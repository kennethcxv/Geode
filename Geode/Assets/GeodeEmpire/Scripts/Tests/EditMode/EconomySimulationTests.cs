using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;
using GeodeEmpire.Retail;
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

        /// <summary>
        /// A mixed-strategy midgame: buy what the cash allows, keep the best find when it is worth keeping, sell the
        /// rest (part retail at the markup, part to the dealer), buy upgrades in a sensible order, replace blades.
        /// Reports when the saw and Stage 2 become affordable in crates and rough hours (~12 min per crate).
        /// </summary>
        [Test]
        public void MidgameProgressionReport()
        {
            const float MinutesPerCrate = 12f;
            var sawAt = new List<int>(); var stageAt = new List<int>(); var lapAt = new List<int>();
            int worlds = 60, crates = 40, softlocks = 0;
            float collectionEnd = 0f, cashEnd = 0f; int prestigeEnd = 0;
            var order = new[] { UpgradeCatalog.Loupe, UpgradeCatalog.InspectionLamp, UpgradeCatalog.BenchClamp, UpgradeCatalog.TrimSaw, UpgradeCatalog.FineChisel,
                UpgradeCatalog.CalibratedScale, UpgradeCatalog.SalesTable, UpgradeCatalog.DisplayExpansion, UpgradeCatalog.Stage2, UpgradeCatalog.HeavyCradle,
                UpgradeCatalog.Wedge, UpgradeCatalog.PolishLap, UpgradeCatalog.ThinBlade, UpgradeCatalog.CoolantPump };
            var sb = new StringBuilder("Midgame progression (mixed strategy):\n");
            for (ulong w = 1; w <= (ulong)worlds; w++)
            {
                var s = NewState(w * 7331UL);
                var rng = new System.Random((int)w);
                float collection = 0f; int cuts = 0;
                int saw = -1, stage = -1, lap = -1;
                for (int n = 0; n < crates; n++)
                {
                    // the best crate the cash allows, keeping the cheapest crate's price in reserve
                    SupplierDefinition sup = null, cheapest = null;
                    foreach (var cand in SupplierCatalog.All)
                    {
                        if (!s.HasSupplier(cand.Id) || s.Cash < cand.Price) continue;
                        if (cheapest == null || cand.Price < cheapest.Price) cheapest = cand;
                        if (s.Cash >= cand.Price + 75f && (sup == null || cand.Price > sup.Price)) sup = cand;
                    }
                    sup ??= cheapest;
                    if (sup == null) { softlocks++; break; }
                    s.Cash -= sup.Price;
                    s.Stats.CratesPurchased++;
                    var crate = CrateGenerator.Generate(s, sup, (seed, id, cid) => Create(s, seed, id, cid));
                    // process: saw owners cut nodules and agate for a small premium; polished faces add a little more
                    float value = 0f, best = 0f;
                    foreach (var id in crate.SpecimenIds)
                    {
                        var r = s.FindSpecimen(id);
                        float damage = s.HasUpgrade(UpgradeCatalog.FineChisel) ? 0.04f : 0.07f;
                        float v = Valuation.DamagedValue(r.Geology, damage, 0f);
                        if (s.HasUpgrade(UpgradeCatalog.TrimSaw) && (r.Geology.Mineral == MineralId.Agate || r.Geology.CavityFraction < 0.35f)) { v *= 1.12f; cuts++; if (s.HasUpgrade(UpgradeCatalog.PolishLap)) v *= 1.08f; }
                        if (s.HasUpgrade(UpgradeCatalog.CalibratedScale)) v *= 1.05f;
                        best = Mathf.Max(best, v);
                        value += v;
                    }
                    // keep the best when it beats what is already on the shelves (collection value drives prestige)
                    // keep a find only when it clearly raises the collection, and not every crate: cash comes first for a mixed player
                    if (best > 120f && best > collection * 0.5f && n % 3 == 0) { collection += best; value -= best; }
                    // half sells retail at the markup, the rest goes to the dealer
                    s.Cash += value * (0.5f * Retail.RetailShop.Markup + 0.5f);
                    s.Cash -= (cuts / 15) * 45f; cuts %= 15;   // a blade every fifteen cuts
                    s.Stats.SpecimensSold += crate.SpecimenIds.Count;
                    s.Prestige = collection >= 6000f ? 5 : collection >= 3000f ? 4 : collection >= 1500f ? 3 : collection >= 600f ? 2 : collection >= 150f ? 1 : 0;
                    SupplierCatalog.EvaluateUnlocks(s);
                    foreach (var id in order)
                    {
                        if (s.HasUpgrade(id)) continue;
                        var up = UpgradeCatalog.Get(id);
                        if (!string.IsNullOrEmpty(up.Requires) && !s.HasUpgrade(up.Requires)) continue;
                        if (s.Cash - up.Price < 150f) break;   // one purchase at a time, in order, keeping a crate's worth
                        s.Cash -= up.Price; s.Upgrades.Add(id);
                        if (id == UpgradeCatalog.TrimSaw) saw = n + 1;
                        if (id == UpgradeCatalog.Stage2) { stage = n + 1; s.WorkshopStage = 2; SupplierCatalog.EvaluateUnlocks(s); }
                        if (id == UpgradeCatalog.PolishLap) lap = n + 1;
                        break;
                    }
                }
                if (saw > 0) sawAt.Add(saw);
                if (stage > 0) stageAt.Add(stage);
                if (lap > 0) lapAt.Add(lap);
                collectionEnd += collection; cashEnd += s.Cash; prestigeEnd += s.Prestige;
                if (w <= 3) sb.AppendLine($"  world {w}: saw@{saw} stage2@{stage} lap@{lap} cash=${s.Cash:F0} collection=${collection:F0} prestige={s.Prestige} suppliers={string.Join(",", s.UnlockedSuppliers)}");
            }
            sawAt.Sort(); stageAt.Sort(); lapAt.Sort();
            string Med(List<int> l) => l.Count == 0 ? "never" : $"crate {l[l.Count / 2]} (~{l[l.Count / 2] * MinutesPerCrate / 60f:F1} h; p10 {l[l.Count / 10] * MinutesPerCrate / 60f:F1} h, p90 {l[l.Count * 9 / 10] * MinutesPerCrate / 60f:F1} h)";
            sb.AppendLine($"  saw affordable: {sawAt.Count}/{worlds} worlds, median {Med(sawAt)}");
            sb.AppendLine($"  Stage 2 built: {stageAt.Count}/{worlds} worlds, median {Med(stageAt)}");
            sb.AppendLine($"  flat lap: {lapAt.Count}/{worlds} worlds, median {Med(lapAt)}");
            sb.AppendLine($"  after {crates} crates: avg cash ${cashEnd / worlds:F0}, avg collection ${collectionEnd / worlds:F0}, avg prestige {prestigeEnd / (float)worlds:F1}, softlocks {softlocks}");
            Debug.Log(sb.ToString());
            Assert.AreEqual(0, softlocks, "a mixed strategy must never strand the career");
            Assert.GreaterOrEqual(sawAt.Count, worlds * 0.95f, "the saw must be reachable in nearly every world");
            Assert.That(sawAt[sawAt.Count / 2] * MinutesPerCrate / 60f, Is.InRange(0.8f, 3.0f), "saw should land in hours 1-3");
            Assert.GreaterOrEqual(stageAt.Count, worlds * 0.9f, "Stage 2 must be reachable in nearly every world within 40 crates");
            Assert.That(stageAt[stageAt.Count / 2] * MinutesPerCrate / 60f, Is.InRange(2.5f, 7.0f), "Stage 2 should land in hours 3-7");
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
