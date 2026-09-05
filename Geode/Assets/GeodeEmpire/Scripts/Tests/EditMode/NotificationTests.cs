using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Tests
{
    /// <summary>
    /// §12: the game is allowed to shout, but it has to earn it. These pin the thresholds, because the failure
    /// they exist to stop — six full cards from one starter crate, a record popup for a fifty-pence improvement —
    /// only shows up in a real career, long after the code that caused it looked reasonable.
    /// </summary>
    public class NotificationTests
    {
        /// <summary>Queue and cooldowns are static: each test starts from a fresh career, not the last test's.</summary>
        [SetUp] public void Reset() => Notifications.Reset();

        private static SpecimenGeology Geo(QualityTier tier)
        {
            var fam = MineralCatalog.All[0];
            return new SpecimenGeology { Mineral = fam.Id, Tier = tier };
        }
        private static Note Classify(QualityTier tier, bool first, float value, float previousBest)
            => Notifications.Classify(new SpecimenRecord(), Geo(tier), "Amethyst", first, value, previousBest);

        [Test]
        public void An_ordinary_find_is_routine()
        {
            Assert.AreEqual(NoteTier.Routine, Classify(QualityTier.Common, false, 12f, 40f).Tier);
            Assert.AreEqual(NoteTier.Routine, Classify(QualityTier.Decent, false, 20f, 40f).Tier);
        }

        [Test]
        public void A_first_of_family_is_meaningful_not_exceptional()
        {
            var n = Classify(QualityTier.Common, true, 15f, 0f);
            Assert.AreEqual(NoteTier.Meaningful, n.Tier);
            StringAssert.Contains("First", n.Headline);
        }

        [Test]
        public void Only_extraordinary_rarity_is_exceptional()
        {
            Assert.AreEqual(NoteTier.Meaningful, Classify(QualityTier.Exceptional, false, 200f, 500f).Tier);
            Assert.AreEqual(NoteTier.Exceptional, Classify(QualityTier.MuseumGrade, false, 200f, 500f).Tier);
            Assert.AreEqual(NoteTier.Exceptional, Classify(QualityTier.WorldClass, false, 200f, 500f).Tier);
        }

        [Test]
        public void A_trivial_early_record_says_nothing()
        {
            // the second rock of a career beating the first by a pound is not news (§12.2)
            Assert.AreEqual(NoteTier.Routine, Classify(QualityTier.Common, false, 9f, 8f).Tier,
                "under the floor: no record note at all");
            Assert.AreEqual(NoteTier.Routine, Classify(QualityTier.Common, false, 44f, 40f).Tier,
                "over the floor but only a 10% improvement: still not news");
            Assert.AreEqual(NoteTier.Meaningful, Classify(QualityTier.Common, false, 60f, 40f).Tier,
                "a real improvement over a real value is worth a line");
        }

        [Test]
        public void A_record_far_past_the_old_one_is_a_career_moment()
        {
            Assert.AreEqual(NoteTier.Exceptional, Classify(QualityTier.Good, false, 400f, 100f).Tier);
        }

        [Test]
        public void A_first_find_that_is_also_a_record_is_one_note_not_two()
        {
            var n = Classify(QualityTier.Good, true, 400f, 100f);
            Assert.AreEqual(NoteTier.Exceptional, n.Tier);
            Assert.IsNotNull(n.Detail, "the second fact rides on the same card");
        }

        [Test]
        public void A_crate_of_first_finds_does_not_produce_a_card_each()
        {
            var seen = new System.Collections.Generic.List<Note>();
            void Watch(Note n) => seen.Add(n);
            Notifications.Present += Watch;
            try
            {
                for (int i = 0; i < 6; i++)
                {
                    Notifications.Post(new Note(NoteTier.Meaningful, "First mineral " + i));
                    Notifications.Tick();
                }
                int cards = 0;
                foreach (var n in seen) if (n.Tier != NoteTier.Routine) cards++;
                Assert.LessOrEqual(cards, Notifications.BurstLimit,
                    $"six first-finds in one crate produced {cards} cards; §12.2 forbids the stack");
                Assert.AreEqual(6, seen.Count, "nothing is dropped: the rest arrive as quiet lines");
            }
            finally { Notifications.Present -= Watch; }
        }

        [Test]
        public void Two_world_class_finds_in_a_row_do_not_both_interrupt()
        {
            var seen = new System.Collections.Generic.List<Note>();
            void Watch(Note n) => seen.Add(n);
            Notifications.Present += Watch;
            try
            {
                Notifications.Post(new Note(NoteTier.Exceptional, "one"));
                Notifications.Tick();
                Notifications.Post(new Note(NoteTier.Exceptional, "two"));
                Notifications.Tick();
                int big = 0;
                foreach (var n in seen) if (n.Tier == NoteTier.Exceptional) big++;
                Assert.AreEqual(1, big, "the second is demoted by the cooldown, not stacked on the first");
            }
            finally { Notifications.Present -= Watch; }
        }

        [Test]
        public void Nothing_is_presented_from_inside_Post_for_a_queued_card()
        {
            // §12.3: the queue is drained on Tick, so a card can never land inside the frame the rock breaks on
            var seen = 0;
            void Watch(Note n) => seen++;
            Notifications.Present += Watch;
            try
            {
                Notifications.Post(new Note(NoteTier.Exceptional, "a"));   // first one goes straight through
                Notifications.Post(new Note(NoteTier.Exceptional, "b"));
                Notifications.Post(new Note(NoteTier.Exceptional, "c"));
                Assert.LessOrEqual(seen, 2, "at most the card being shown plus a demoted line, never all three");
            }
            finally { Notifications.Present -= Watch; }
        }
    }
}
