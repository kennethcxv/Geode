using System;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Core
{
    /// <summary>How loudly the game is allowed to react to something (§12.1).</summary>
    public enum NoteTier
    {
        /// <summary>A normal find, a small improvement. A line in the corner, gone in a moment.</summary>
        Routine = 1,
        /// <summary>A genuinely new family, a rare variant, a real quality milestone. A short, polished card.</summary>
        Meaningful = 2,
        /// <summary>Extraordinary rarity or a major personal record. Rare enough to be worth interrupting for.</summary>
        Exceptional = 3,
    }

    public readonly struct Note
    {
        public readonly NoteTier Tier;
        public readonly string Headline, Detail;
        public readonly SpecimenRecord Specimen;
        public Note(NoteTier tier, string headline, string detail = null, SpecimenRecord specimen = null)
        { Tier = tier; Headline = headline; Detail = detail; Specimen = specimen; }
        public bool Valid => !string.IsNullOrEmpty(Headline);
        public override string ToString() => $"[{Tier}] {Headline}" + (Detail != null ? " — " + Detail : "");
    }

    /// <summary>
    /// What the game is allowed to shout about, and how often (§12).
    ///
    /// The old behaviour had two shapes and no queue: the same full card announced a first find and a world-class
    /// one, "Best X so far" fired for any improvement at all including a fifty-pence one on the second rock of
    /// the career, and two rocks opened back to back simply overwrote each other's card. Opening a starter crate
    /// produced six big cards in a row.
    ///
    /// This is the referee. <see cref="Classify"/> is pure so the thresholds can be tested rather than eyeballed,
    /// and everything else here is queue and cooldown: one card on screen at a time, a cooldown per tier, and a
    /// tier that decays when the same tier keeps arriving, so a crate full of first-finds ends up as one card and
    /// a handful of quiet lines instead of six interruptions.
    /// </summary>
    public static class Notifications
    {
        // ---- significance thresholds (§12.2) -------------------------------------------------
        /// <summary>A personal best under this is not worth a line: early records are noise, not news.</summary>
        public const float RecordFloor = 30f;
        /// <summary>...and it has to beat the old best by this much to count as an improvement.</summary>
        public const float RecordFactor = 1.2f;
        /// <summary>A record this far past the old one is a career moment rather than a better one.</summary>
        public const float MajorRecordFactor = 2.5f;

        // ---- rate limits (§12.2) -------------------------------------------------------------
        public const float ExceptionalCooldown = 40f;
        public const float MeaningfulCooldown = 6f;
        /// <summary>Card time on screen, by tier.</summary>
        public const float MeaningfulSeconds = 3.2f, ExceptionalSeconds = 5.5f;
        /// <summary>Meaningful cards inside this window before the rest of the burst drops to routine lines.</summary>
        public const int BurstLimit = 2;
        public const float BurstWindow = 20f;

        /// <summary>Raised with the note that should be presented now. The HUD decides what each tier looks like.</summary>
        public static event Action<Note> Present;

        private static readonly List<Note> _queue = new List<Note>(8);
        private static float _busyUntil, _lastExceptional = -999f, _lastMeaningful = -999f;
        private static readonly List<float> _recentMeaningful = new List<float>(8);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Present = null; Reset(); }

        /// <summary>
        /// Back to a fresh career's worth of history: no queue, no cooldowns, no burst. Subscribers are kept —
        /// clearing them belongs to domain reload, not to "start counting again".
        /// </summary>
        public static void Reset()
        {
            _queue.Clear(); _recentMeaningful.Clear();
            _busyUntil = 0f; _lastExceptional = -999f; _lastMeaningful = -999f;
        }

        /// <summary>Queue depth, for tests and diagnostics.</summary>
        public static int Pending => _queue.Count;

        /// <summary>
        /// What a rock coming open deserves. Pure: no time, no queue, no side effects — <paramref name="now"/>
        /// only decides tone, not whether the note is allowed through (that is <see cref="Post"/>'s job).
        /// </summary>
        public static Note Classify(SpecimenRecord r, SpecimenGeology g, string familyName,
                                    bool firstOfFamily, float value, float previousBest)
        {
            if (g == null) return default;
            bool beatsFloor = value >= RecordFloor;
            bool record = previousBest > 0f && value > previousBest * RecordFactor && beatsFloor;
            bool majorRecord = previousBest > 0f && value > previousBest * MajorRecordFactor && beatsFloor;

            // tier 3: extraordinary rarity, or a record that is a career moment rather than a better afternoon
            if (g.Tier >= QualityTier.MuseumGrade)
                return new Note(NoteTier.Exceptional, Specimens.Valuation.TierLabel(g.Tier) + " " + familyName,
                    firstOfFamily ? "the first you have ever opened" : "a piece worth keeping", r);
            if (majorRecord)
                return new Note(NoteTier.Exceptional, "New record: " + familyName,
                    UI.UiKit.Money(value) + ", well past your old best", r);

            // tier 2: a genuinely new family, a rare variant, a real milestone. One note, not two: a first find
            // that is also a record says both things on one card rather than queueing a second (§12.2 combine).
            if (firstOfFamily)
                return new Note(NoteTier.Meaningful, "First " + familyName,
                    g.Tier >= QualityTier.Good ? "and a good one" : null, r);
            if (g.Tier >= QualityTier.Exceptional)
                return new Note(NoteTier.Meaningful, Specimens.Valuation.TierLabel(g.Tier) + " " + familyName,
                    record ? "your best yet" : null, r);
            if (record)
                return new Note(NoteTier.Meaningful, "Best " + familyName + " so far", UI.UiKit.Money(value), r);

            // tier 1: everything else. A found line, no card.
            return new Note(NoteTier.Routine, familyName, Specimens.Valuation.TierLabel(g.Tier), r);
        }

        /// <summary>
        /// Offer a note. Cooldowns and burst limits may quietly demote it a tier or drop it; nothing here blocks,
        /// and nothing is ever presented from inside this call (§12.3: the rock breaks first).
        /// </summary>
        public static void Post(Note note)
        {
            if (!note.Valid) return;
            float now = Time.unscaledTime;
            var tier = note.Tier;

            if (tier == NoteTier.Exceptional && now - _lastExceptional < ExceptionalCooldown)
                tier = NoteTier.Meaningful;                       // two world-class finds in a row: the second is a card

            if (tier == NoteTier.Meaningful)
            {
                _recentMeaningful.RemoveAll(t => now - t > BurstWindow);
                if (now - _lastMeaningful < MeaningfulCooldown || _recentMeaningful.Count >= BurstLimit)
                    tier = NoteTier.Routine;                      // a crate full of first finds is one card, then lines
            }

            var final = tier == note.Tier ? note : new Note(tier, note.Headline, note.Detail, note.Specimen);
            if (final.Tier == NoteTier.Routine) { Deliver(final, now); return; }
            _queue.Add(final);
            Drain(now);
        }

        /// <summary>Called once a frame by the session so queued cards get their turn.</summary>
        public static void Tick() => Drain(Time.unscaledTime);

        private static void Drain(float now)
        {
            if (_queue.Count == 0 || now < _busyUntil) return;
            // the loudest thing waiting goes first, so a world-class find is not stuck behind three first-finds
            int best = 0;
            for (int i = 1; i < _queue.Count; i++) if (_queue[i].Tier > _queue[best].Tier) best = i;
            var n = _queue[best];
            _queue.RemoveAt(best);
            _busyUntil = now + (n.Tier == NoteTier.Exceptional ? ExceptionalSeconds : MeaningfulSeconds);
            Deliver(n, now);
        }

        private static void Deliver(Note n, float now)
        {
            if (n.Tier == NoteTier.Exceptional) _lastExceptional = now;
            if (n.Tier == NoteTier.Meaningful) { _lastMeaningful = now; _recentMeaningful.Add(now); }
            Present?.Invoke(n);
        }
    }
}
