using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Tests
{
    /// <summary>
    /// Lightweight economy simulation: crate returns per supplier, variance, tails, and a naive
    /// sell-everything progression to check the first hour cannot soft-lock through ordinary bad luck.
    /// </summary>
    public class EconomySimulationTests
    {
        private static GameState NewState(ulong seed)
        {
            var s = new GameState { SaveId = "sim", WorldSeed = seed, Cash = GameSession.StartingCash };
            s.UnlockedSuppliers.Add(SupplierCatalog.Local);
            return s;
        }

        private static float CrateValue(GameState s, CrateRecord c, float damage = 0.05f)
        {
            float v = 0f;
            foreach (var id in c.SpecimenIds)
            {
                var r = s.FindSpecimen(id);
                v += Valuation.DamagedValue(r.Geology, damage, 0f);
            }
            return v;
        }

        private static SpecimenRecord Create(GameState s, ulong seed, string sup, string crate)
        {
            s.SpecimenCounter++;
            var r = new SpecimenRecord { Id = "S" + s.SpecimenCounter, Seed = seed, SupplierId = sup, CrateId = crate, Location = SpecimenLocation.InCrate };
            s.Specimens.Add(r);
            return r;
        }

        [Test]
        public void SupplierReturnsReport()
        {
            var sb = new StringBuilder("Crate returns (value at sale, ~5% damage)\n");
            foreach (var sup in SupplierCatalog.All)
            {
                var returns = new List<float>();
                int busts = 0, jackpots = 0;
                for (ulong w = 1; w <= 400; w++)
                {
                    var s = NewState(w * 104729UL);
                    s.CrateCounter = 5; // skip the curated first-crate script
                    var crate = CrateGenerator.Generate(s, sup, (seed, id, cid) => Create(s, seed, id, cid));
                    float v = CrateValue(s, crate);
                    returns.Add(v);
                    if (v < sup.Price * 0.6f) busts++;
                    if (v > sup.Price * 4f) jackpots++;
                }
                returns.Sort();
                float mean = 0f; foreach (var v in returns) mean += v; mean /= returns.Count;
                sb.AppendLine($"  {sup.Id,-9} price={sup.Price,5}  mean={mean,7:F0}  median={returns[returns.Count / 2],7:F0}  p10={returns[returns.Count / 10],6:F0}  p90={returns[returns.Count * 9 / 10],7:F0}  max={returns[returns.Count - 1],7:F0}  busts(<60%)={busts * 100f / returns.Count:F0}%  jackpots(>4x)={jackpots * 100f / returns.Count:F0}%");
                Assert.Greater(mean, sup.Price * 1.15f, $"{sup.Id} should return more than its price on average");
            }
            Debug.Log(sb.ToString());
        }

        [Test]
        public void FirstCrateIsTeachingPaced()
        {
            int stuck = 0;
            var sb = new StringBuilder("First crate (curated):\n");
            for (ulong w = 1; w <= 200; w++)
            {
                var s = NewState(w * 7919UL);
                var sup = SupplierCatalog.Get(SupplierCatalog.Local);
                var crate = CrateGenerator.Generate(s, sup, (seed, id, cid) => Create(s, seed, id, cid));
                float v = CrateValue(s, crate);
                if (v + (GameSession.StartingCash - sup.Price) < sup.Price + 30f) stuck++;   // could not afford another crate after selling everything
                if (w <= 5)
                {
                    sb.Append($"  world {w}: total ${v:F0} ->");
                    foreach (var id in crate.SpecimenIds) { var g = s.FindSpecimen(id).Geology; sb.Append($" {g.Mineral}/{g.Tier}/${g.BaseValue}"); }
                    sb.AppendLine();
                }
            }
            sb.AppendLine($"  worlds where first crate cannot fund the next one: {stuck}/200");
            Debug.Log(sb.ToString());
            Assert.Less(stuck, 4, "first crate must not soft-lock normal players");
        }

        [Test]
        public void SellEverythingProgression()
        {
            // naive player: always buy the cheapest affordable crate, sell everything, buy upgrades when affordable
            var sb = new StringBuilder("Sell-everything progression over 8 crates:\n");
            int softlocks = 0;
            float avgCashAfter4 = 0f;
            for (ulong w = 1; w <= 100; w++)
            {
                var s = NewState(w * 31337UL);
                var log = new StringBuilder();
                for (int crateN = 0; crateN < 8; crateN++)
                {
                    var sup = SupplierCatalog.Get(s.HasSupplier(SupplierCatalog.Regional) && s.Cash >= 190f + 100f ? SupplierCatalog.Regional : SupplierCatalog.Local);
                    if (s.Cash < sup.Price) { softlocks++; break; }
                    s.Cash -= sup.Price;
                    var crate = CrateGenerator.Generate(s, sup, (seed, id, cid) => Create(s, seed, id, cid));
                    float v = CrateValue(s, crate, 0.08f);
                    s.Cash += v;
                    s.Stats.SpecimensSold += crate.SpecimenIds.Count;
                    SupplierCatalog.EvaluateUnlocks(s);
                    foreach (var up in UpgradeCatalog.All)
                        if (!s.HasUpgrade(up.Id) && s.Cash - up.Price >= 120f) { s.Cash -= up.Price; s.Upgrades.Add(up.Id); }
                    if (crateN == 3) avgCashAfter4 += s.Cash;
                    if (w <= 3) log.Append($" [{sup.Id[0]} +{v:F0} -> ${s.Cash:F0} up={s.Upgrades.Count}]");
                }
                if (w <= 3) sb.AppendLine("  world " + w + ":" + log);
            }
            sb.AppendLine($"  softlocks: {softlocks}/100   avg cash after 4 crates (after upgrades): ${avgCashAfter4 / 100f:F0}");
            Debug.Log(sb.ToString());
            Assert.Less(softlocks, 3);
        }
    }
}
