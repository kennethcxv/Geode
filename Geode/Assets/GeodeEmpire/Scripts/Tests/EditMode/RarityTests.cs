using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Economy;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Tests
{
    /// <summary>
    /// V5 rarity gate: ordinary sourcing must keep the top tiers genuinely scarce, and premium sources may raise the
    /// floor without turning crates into jackpots. Reports tier frequencies per supplier over thousands of rocks.
    /// </summary>
    public class RarityTests
    {
        private static GameState NewState(ulong seed)
        {
            var s = new GameState { SaveId = "rarity", WorldSeed = seed, Cash = 100000f };
            foreach (var sup in SupplierCatalog.All) if (!s.UnlockedSuppliers.Contains(sup.Id)) s.UnlockedSuppliers.Add(sup.Id);
            s.WorkshopStage = 2;
            return s;
        }

        private static SpecimenRecord Create(GameState s, ulong seed, string sup, string crate)
        {
            s.SpecimenCounter++;
            var r = new SpecimenRecord { Id = "S" + s.SpecimenCounter, Seed = seed, SupplierId = sup, CrateId = crate, Location = SpecimenLocation.InCrate };
            s.Specimens.Add(r);
            return r;
        }

        [Test]
        public void RarityDistributionReport()
        {
            // neutral generator draw: the base tier weights
            var counts = new int[6];
            int n = 6000;
            for (ulong seed = 1; seed <= (ulong)n; seed++) counts[(int)SpecimenGenerator.Generate(seed * 7919UL + 13UL).Tier]++;
            var sb = new StringBuilder("Rarity (neutral draw, " + n + " rocks): ");
            for (int t = 0; t < 6; t++) sb.Append($"{(QualityTier)t}={100f * counts[t] / n:F2}%  ");
            sb.AppendLine();
            float common = 100f * counts[0] / n, exceptional = 100f * counts[3] / n, museum = 100f * counts[4] / n, world = 100f * counts[5] / n;
            var failures = new List<string>();
            if (common < 52f || common > 72f) failures.Add($"neutral Common {common:F1}% outside 52-72");
            if (exceptional < 1f || exceptional > 5f) failures.Add($"neutral Exceptional {exceptional:F2}% outside 1-5");
            if (museum < 0.15f || museum > 1.6f) failures.Add($"neutral Museum {museum:F2}% outside 0.15-1.6");
            if (world >= 0.5f) failures.Add($"neutral World Class {world:F2}% >= 0.5");

            // per supplier: what a crate actually delivers after the lot's own selection
            sb.AppendLine("Per supplier (400 crates each):");
            foreach (var sup in SupplierCatalog.All)
            {
                var c = new int[6]; int total = 0; var fams = new HashSet<MineralId>();
                for (ulong w = 1; w <= 400; w++)
                {
                    var s = NewState(w * 104729UL + 7UL);
                    s.CrateCounter = 5;
                    var crate = CrateGenerator.Generate(s, sup, (seed, id, cid) => Create(s, seed, id, cid));
                    foreach (var id in crate.SpecimenIds)
                    {
                        var g = s.FindSpecimen(id).Geology;
                        c[(int)g.Tier]++; total++; fams.Add(g.Mineral);
                    }
                }
                float ex = 100f * c[3] / total, mu = 100f * c[4] / total, wc = 100f * c[5] / total;
                sb.AppendLine($"  {sup.Id,-9} rocks={total,5} families={fams.Count,2}  Common={100f * c[0] / total,5:F1}% Decent={100f * c[1] / total,5:F1}% Good={100f * c[2] / total,5:F1}% Exc={ex,5:F2}% Museum={mu,5:F2}% World={wc,5:F2}%");
                if (mu + wc >= 5f) failures.Add($"{sup.Id}: museum+world {mu + wc:F2}% is routine (>= 5%)");
                if (ex >= 20f) failures.Add($"{sup.Id}: exceptional {ex:F1}% is a staple (>= 20%)");
            }
            Debug.Log(sb.ToString());
            Assert.That(failures, Is.Empty, string.Join("; ", failures));
        }
    }
}
