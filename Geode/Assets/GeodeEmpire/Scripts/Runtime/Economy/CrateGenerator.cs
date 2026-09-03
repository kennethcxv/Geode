using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Economy
{
    /// <summary>
    /// Builds crate contents deterministically from the world seed + crate counter. Suppliers shape the
    /// distribution by rejection-sampling specimen seeds; the geology of any seed is still pure.
    /// </summary>
    public static class CrateGenerator
    {
        public const int MaxAttemptsPerRock = 400;

        public static CrateRecord Generate(GameState state, SupplierDefinition sup, System.Func<ulong, string, string, SpecimenRecord> createRecord)
        {
            state.CrateCounter++;
            ulong crateSeed = SeededRandom.Combine(state.WorldSeed, (ulong)state.CrateCounter * 7919UL + 17UL);
            var rng = new SeededRandom(crateSeed);
            var crate = new CrateRecord
            {
                Id = $"C{state.CrateCounter:D3}",
                SupplierId = sup.Id,
                Seed = crateSeed,
                PricePaid = sup.Price,
            };
            int count = rng.Range(sup.MinRocks, sup.MaxRocks + 1);
            var targets = BuildTierTargets(state, sup, count, ref rng);

            var mineralCounts = new Dictionary<MineralId, int>();
            for (int i = 0; i < count; i++)
            {
                var target = targets[i];
                ulong chosen = 0;
                SpecimenGeology chosenGeo = null;
                // a focused source draws most of its rocks from its preferred families
                bool wantPreferred = sup.PreferredMinerals != null && sup.PreferredMinerals.Length > 0 && rng.Chance(sup.PreferredShare);
                for (int attempt = 0; attempt < MaxAttemptsPerRock; attempt++)
                {
                    ulong seed = rng.NextULong();
                    var g = SpecimenGenerator.Generate(seed);
                    if (g.Tier != target) continue;
                    if (wantPreferred && attempt < MaxAttemptsPerRock - 40 && System.Array.IndexOf(sup.PreferredMinerals, g.Mineral) < 0) continue;
                    // keep crates varied: at most 3 of one family, and the curated first crate avoids repeats
                    int have = mineralCounts.GetValueOrDefault(g.Mineral);
                    int limit = state.CrateCounter == 1 ? 2 : 3;
                    if (wantPreferred) limit = 12;   // a focused lot is allowed to repeat its mineral
                    if (have >= limit && attempt < MaxAttemptsPerRock - 20) continue;
                    chosen = seed;
                    chosenGeo = g;
                    break;
                }
                if (chosenGeo == null)
                {
                    chosen = rng.NextULong();
                    chosenGeo = SpecimenGenerator.Generate(chosen);
                }
                mineralCounts[chosenGeo.Mineral] = mineralCounts.GetValueOrDefault(chosenGeo.Mineral) + 1;
                var rec = createRecord(chosen, sup.Id, crate.Id);
                crate.SpecimenIds.Add(rec.Id);
            }
            state.Crates.Add(crate);
            return crate;
        }

        /// <summary>Tier per rock. The first local crate is deliberately paced; later crates follow the supplier table with light new-player safeguards.</summary>
        public static QualityTier[] BuildTierTargets(GameState state, SupplierDefinition sup, int count, ref SeededRandom rng)
        {
            var t = new QualityTier[count];
            if (state.CrateCounter == 1 && sup.Id == SupplierCatalog.Local)
            {
                // teaching crate: mostly ordinary, one clearly nicer piece, a couple of decent ones
                var script = new List<QualityTier> { QualityTier.Common, QualityTier.Common, QualityTier.Decent, QualityTier.Common, QualityTier.Good,
                    QualityTier.Common, QualityTier.Decent, QualityTier.Common, QualityTier.Common, QualityTier.Decent };
                for (int i = 0; i < count; i++) t[i] = script[i % script.Count];
                // shuffle the middle so the good one is not always in the same slot, but never first
                for (int i = count - 1; i > 1; i--) { int j = rng.Range(1, i + 1); (t[i], t[j]) = (t[j], t[i]); }
                if (t[0] != QualityTier.Common) { for (int i = 1; i < count; i++) if (t[i] == QualityTier.Common) { (t[0], t[i]) = (t[i], t[0]); break; } }
                return t;
            }
            for (int i = 0; i < count; i++) t[i] = (QualityTier)rng.PickWeighted(sup.TierWeights);

            // new-player safeguards on the early crates: no total bust, and a guaranteed standout by crate 3
            bool anyGood = false;
            foreach (var x in t) if (x >= QualityTier.Good) anyGood = true;
            if (state.CrateCounter <= 3 && !anyGood) t[rng.Range(0, count)] = QualityTier.Good;
            if (state.CrateCounter == 3 && !HasFoundTier(state, QualityTier.Exceptional))
            {
                bool hasExc = false;
                foreach (var x in t) if (x >= QualityTier.Exceptional) hasExc = true;
                if (!hasExc) t[rng.Range(0, count)] = QualityTier.Exceptional;
            }
            return t;
        }

        private static bool HasFoundTier(GameState state, QualityTier tier)
        {
            foreach (var s in state.Specimens) if (s.IsOpened && s.Geology.Tier >= tier) return true;
            return false;
        }
    }
}
