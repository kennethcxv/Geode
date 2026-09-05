using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Audio;

namespace GeodeEmpire.Tests
{
    /// <summary>
    /// §9.2/§9.3: the break must actually differ by size, material and tool. These assert the layer plan rather
    /// than "an audio call happened", so a future refactor that quietly flattens the balance back to one sample
    /// for every rock fails here instead of shipping.
    /// </summary>
    public class FractureAudioTests
    {
        private const float Small = FractureAudio.SmallRadius, Large = FractureAudio.LargeRadius;
        private const float Soft = 0.6f, Tough = 1.4f;

        private static FractureLayer Layer(List<FractureLayer> plan, string cue)
        {
            foreach (var l in plan) if (l.Cue == cue) return l;
            Assert.Fail("plan has no '" + cue + "' layer: " + string.Join(", ", plan));
            return default;
        }
        private static bool Has(List<FractureLayer> plan, string cue)
        {
            foreach (var l in plan) if (l.Cue == cue) return true;
            return false;
        }

        [Test]
        public void Break_is_layered_in_time_not_one_shot()
        {
            var plan = FractureAudio.Plan(0.07f, 1f, FractureAudio.Tool.Hammer, false);
            Assert.GreaterOrEqual(plan.Count, 5, "§9.1 wants tool impact, onset, split, debris and settling");
            // the layers must be spread over the break, not stacked on frame zero
            float first = float.MaxValue, last = float.MinValue;
            foreach (var l in plan) { first = Mathf.Min(first, l.Delay); last = Mathf.Max(last, l.Delay); }
            Assert.AreEqual(0f, first, 1e-4f, "something has to land on the blow itself");
            Assert.Greater(last, 0.2f, "the settling layer must trail the break");
        }

        [Test]
        public void Bigger_rocks_break_lower_and_louder()
        {
            var small = FractureAudio.Plan(Small, 1f, FractureAudio.Tool.Hammer, false);
            var large = FractureAudio.Plan(Large, 1f, FractureAudio.Tool.Hammer, false);
            Assert.Less(Layer(large, "stone_split").Pitch, Layer(small, "stone_split").Pitch, "a large rock splits lower");
            Assert.Greater(Layer(large, "stone_split").Volume, Layer(small, "stone_split").Volume, "and with more mass behind it");
            Assert.Less(Layer(large, "debris_settle").Pitch, Layer(small, "debris_settle").Pitch);
        }

        [Test]
        public void Tougher_shells_snap_brighter()
        {
            var soft = FractureAudio.Plan(0.07f, Soft, FractureAudio.Tool.Hammer, false);
            var tough = FractureAudio.Plan(0.07f, Tough, FractureAudio.Tool.Hammer, false);
            Assert.Greater(Layer(tough, "crack_onset").Pitch, Layer(soft, "crack_onset").Pitch);
            Assert.Greater(Layer(tough, "fragments").Pitch, Layer(soft, "fragments").Pitch);
        }

        [Test]
        public void Only_a_tough_shell_rings_on_after_the_break()
        {
            Assert.IsTrue(Has(FractureAudio.Plan(0.07f, Tough, FractureAudio.Tool.Hammer, false), "tension"));
            Assert.IsFalse(Has(FractureAudio.Plan(0.07f, Soft, FractureAudio.Tool.Hammer, false), "tension"));
            Assert.IsFalse(Has(FractureAudio.Plan(0.07f, Tough, FractureAudio.Tool.Saw, false), "tension"),
                "a saw parts a face; it does not leave a shell ringing");
        }

        [Test]
        public void The_three_tools_are_balanced_differently()
        {
            var hammer = FractureAudio.Plan(0.07f, 1f, FractureAudio.Tool.Hammer, false);
            var cracker = FractureAudio.Plan(0.07f, 1f, FractureAudio.Tool.Cracker, false);
            var saw = FractureAudio.Plan(0.07f, 1f, FractureAudio.Tool.Saw, false);
            // the press has no metal transient in front of it, and the saw barely any
            Assert.Less(Layer(cracker, "crack_onset").Volume, Layer(hammer, "crack_onset").Volume);
            Assert.Less(Layer(saw, "crack_onset").Volume, Layer(cracker, "crack_onset").Volume);
            // but the press puts more mass through the split than the hammer does
            Assert.Greater(Layer(cracker, "stone_split").Volume, Layer(hammer, "stone_split").Volume);
            // and a cut throws far less off the rock than a break
            Assert.Less(Layer(saw, "fragments").Volume, Layer(hammer, "fragments").Volume * 0.5f);
        }

        [Test]
        public void A_shattered_shell_throws_more_debris()
        {
            var clean = FractureAudio.Plan(0.07f, 1f, FractureAudio.Tool.Hammer, false);
            var burst = FractureAudio.Plan(0.07f, 1f, FractureAudio.Tool.Hammer, false, shattered: true);
            Assert.Greater(Layer(burst, "fragments").Volume, Layer(clean, "fragments").Volume);
            Assert.Greater(Layer(burst, "debris_settle").Volume, Layer(clean, "debris_settle").Volume);
        }

        [Test]
        public void Every_cue_the_plan_names_exists_in_the_bank()
        {
            var seen = new HashSet<string>();
            foreach (var tool in new[] { FractureAudio.Tool.Hammer, FractureAudio.Tool.Cracker, FractureAudio.Tool.Saw })
            foreach (var t in new[] { Soft, Tough })
            foreach (var l in FractureAudio.Plan(0.07f, t, tool, false)) seen.Add(l.Cue);
            foreach (var cue in seen)
                Assert.IsNotNull(WorkshopAudio.GetClip(cue), "no clip generated for '" + cue + "'");
        }

        /// <summary>§8.2: the bad hit has to be a different sound, not a quieter one.</summary>
        [Test]
        public void The_dead_hit_is_spectrally_duller_than_a_good_one()
        {
            float live = Brightness(WorkshopAudio.GetClip("tap_medium"));
            float dead = Brightness(WorkshopAudio.GetClip("tap_dead"));
            Assert.Greater(live, dead * 1.5f,
                $"tap_dead must be audibly duller, not just quieter (bright: live {live:0.000} vs dead {dead:0.000})");
        }

        [Test]
        public void The_onset_is_short_and_bright_and_the_split_is_long_and_low()
        {
            var onset = WorkshopAudio.GetClip("crack_onset");
            var split = WorkshopAudio.GetClip("stone_split");
            Assert.Less(onset.length, 0.2f, "the onset is the first fibre giving, not the break");
            Assert.Greater(split.length, 0.5f, "the split carries the body of the break");
            Assert.Greater(Brightness(onset), Brightness(split) * 1.5f);
        }

        /// <summary>Zero-crossing rate: a cheap, stable stand-in for spectral centroid.</summary>
        private static float Brightness(AudioClip clip)
        {
            Assert.IsNotNull(clip);
            var d = new float[clip.samples * clip.channels];
            clip.GetData(d, 0);
            int crossings = 0, counted = 0;
            for (int i = 1; i < d.Length; i++)
            {
                if (Mathf.Abs(d[i]) < 0.004f && Mathf.Abs(d[i - 1]) < 0.004f) continue;   // ignore the silent tail
                counted++;
                if ((d[i] < 0f) != (d[i - 1] < 0f)) crossings++;
            }
            return counted > 0 ? crossings / (float)counted : 0f;
        }
    }
}
