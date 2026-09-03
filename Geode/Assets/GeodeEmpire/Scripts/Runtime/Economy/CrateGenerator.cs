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
        public const int MaxAttemptsPerRock = 1600;

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
            // the lot comes from one place: a named locality the rocks carry for the rest of their lives
            var locs = sup.Localities != null && sup.Localities.Length > 0 ? sup.Localities : DefaultLocalities(sup.Id);
            crate.Locality = locs[rng.Range(0, locs.Length)];

            var mineralCounts = new Dictionary<MineralId, int>();
            var sizeWeights = sup.SizeWeights ?? SpecimenGenerator.NeutralSizeWeights;
            for (int i = 0; i < count; i++)
            {
                var target = targets[i];
                // the source's physical size mix, and the teaching crate keeps its first rock hand-sized
                var sizeTarget = (SizeClass)rng.PickWeighted(sizeWeights);
                // the teaching crate keeps its first rock and its good pieces hand-sized: a fist-sized cavity to learn on
                if (state.CrateCounter == 1 && (i == 0 || target >= QualityTier.Good)) sizeTarget = SizeClass.Medium;
                ulong chosen = 0;
                SpecimenGeology chosenGeo = null;
                // a focused source draws most of its rocks from its preferred families
                bool wantPreferred = sup.PreferredMinerals != null && sup.PreferredMinerals.Length > 0 && rng.Chance(sup.PreferredShare);
                bool wantCavity = sup.PreferredCavities != null && sup.PreferredCavities.Length > 0 && rng.Chance(sup.CavityShare);
                for (int attempt = 0; attempt < MaxAttemptsPerRock; attempt++)
                {
                    ulong seed = rng.NextULong();
                    var g = SpecimenGenerator.Generate(seed);
                    if (g.Tier != target) continue;
                    if (g.SizeClass != sizeTarget && attempt < MaxAttemptsPerRock - 200) continue;
                    if (wantPreferred && attempt < MaxAttemptsPerRock - 40 && System.Array.IndexOf(sup.PreferredMinerals, g.Mineral) < 0) continue;
                    // a source's formations and shell character: a locality of thick-shelled coconuts, a specialist's thin pockets
                    if (wantCavity && attempt < MaxAttemptsPerRock - 60 && System.Array.IndexOf(sup.PreferredCavities, g.Cavity) < 0) continue;
                    if (sup.ShellBias > 1.05f && attempt < MaxAttemptsPerRock - 80 && g.ShellThickness < 0.18f) continue;
                    if (sup.ShellBias < 0.95f && attempt < MaxAttemptsPerRock - 80 && g.ShellThickness > 0.22f) continue;
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
                rec.Locality = crate.Locality;
                rec.AcquiredAtTicks = System.DateTime.UtcNow.Ticks;
                rec.AcquisitionCost = Mathf.Round(sup.Price / Mathf.Max(1, count) * 100f) / 100f;
                rec.OriginalMassKg = chosenGeo.MassKg;
                GameState.Log(rec, "acquired", rec.AcquisitionCost, sup.Name + ", " + crate.Locality);
                if (sup.PreDamage > 0.001f && rng.Chance(0.8f))
                {
                    // knocked about in transit: bruised shell, and an open crack or two the chisel can start from
                    rec.ShellDamage = Mathf.Clamp01(sup.PreDamage * rng.Range(0.4f, 1f));
                    rec.SectorStress = new float[SpecimenGenerator.SeamSectors];
                    int cracks = rng.Range(1, 3);
                    for (int c = 0; c < cracks; c++) rec.SectorStress[rng.Range(0, SpecimenGenerator.SeamSectors)] = rng.Range(0.45f, 0.75f);
                    rec.Impacts.Add(new Vector4(rng.NextFloat(), rng.Range(-0.3f, 0.3f), chosenGeo.Size * 0.15f, 0.7f));
                    GameState.Log(rec, "damaged", 0f, "chipped in transit");
                }
                crate.SpecimenIds.Add(rec.Id);
            }
            state.Crates.Add(crate);
            return crate;
        }

        /// <summary>Localities for sources that do not name their own.</summary>
        public static string[] DefaultLocalities(string supplierId) => supplierId switch
        {
            SupplierCatalog.Regional => new[] { "Brazilian import lot", "Chihuahua nodule beds", "Tabasco Mine", "Rio Grande do Sul" },
            SupplierCatalog.AmethystLot => new[] { "Artigas, Uruguay", "Ametista do Sul", "Thunder Bay" },
            SupplierCatalog.Estate => new[] { "an estate in Tucson", "a rockhound's garage, Quartzsite", "a retired cutter's shop, Franklin", "a club collection, Boise" },
            SupplierCatalog.Premium => new[] { "Dugway, Utah", "Coyamito Ranch", "Las Choyas", "Keokuk, Iowa" },
            SupplierCatalog.CuttingRough => new[] { "Botswana nodule lot", "Condor agate field", "Brazilian rough" },
            SupplierCatalog.DesertPocket => new[] { "Erongo pockets", "Thomas Range", "Las Choyas vugs" },
            SupplierCatalog.OversizedLot => new[] { "Rio Grande do Sul cathedral field", "Uruguayan basalt flow" },
            _ => new[] { "the local quarry" },
        };

        /// <summary>Tier per rock. The first local crate is deliberately paced; later crates follow the supplier table with light new-player safeguards.</summary>
        public static QualityTier[] BuildTierTargets(GameState state, SupplierDefinition sup, int count, ref SeededRandom rng)
        {
            var t = new QualityTier[count];
            if (state.CrateCounter == 1 && sup.Id == SupplierCatalog.Local)
            {
                // teaching crate: mostly ordinary, two clearly nicer pieces, a few decent ones: it always funds the next crate
                var script = new List<QualityTier> { QualityTier.Common, QualityTier.Common, QualityTier.Decent, QualityTier.Common, QualityTier.Good,
                    QualityTier.Common, QualityTier.Decent, QualityTier.Good, QualityTier.Common, QualityTier.Decent };
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
