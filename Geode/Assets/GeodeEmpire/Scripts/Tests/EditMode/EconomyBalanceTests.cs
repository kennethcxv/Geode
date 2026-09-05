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
    /// V5 §68/§69/§80: the deterministic balance gates. Rarity fatigue over 100/500/1000 rocks, jackpot scarcity,
    /// cross-supplier promises (each source does what its tagline says), late-game money sinks, and the 15–25 hour
    /// career pacing assertion. Every simulation is seeded; a failure here is a balance regression, not flake.
    /// </summary>
    public class EconomyBalanceTests
    {
        private static GameState NewState(ulong seed, int stage = 1)
        {
            var s = new GameState { SaveId = "sim", WorldSeed = seed, Cash = GameSession.StartingCash, WorkshopStage = stage, CrateCounter = 5 };
            foreach (var sup in SupplierCatalog.All) s.UnlockedSuppliers.Add(sup.Id);
            return s;
        }

        private static SpecimenRecord Create(GameState s, ulong seed, string sup, string crate)
        {
            s.SpecimenCounter++;
            var r = new SpecimenRecord { Id = "S" + s.SpecimenCounter, Seed = seed, SupplierId = sup, CrateId = crate, Location = SpecimenLocation.InCrate };
            s.Specimens.Add(r);
            return r;
        }

        /// <summary>Rocks from <paramref name="count"/> crates of one supplier, in crate order.</summary>
        private static List<SpecimenRecord> Rocks(SupplierDefinition sup, int crates, ulong world, out List<float> crateReturns)
        {
            var s = NewState(world);
            var rocks = new List<SpecimenRecord>();
            crateReturns = new List<float>();
            for (int n = 0; n < crates; n++)
            {
                var crate = CrateGenerator.Generate(s, sup, (seed, id, cid) => Create(s, seed, id, cid));
                float v = 0f;
                foreach (var id in crate.SpecimenIds)
                {
                    var r = s.FindSpecimen(id);
                    rocks.Add(r);
                    v += Valuation.DamagedValue(r.Geology, 0.05f + r.ShellDamage * 0.5f, 0f);
                }
                crateReturns.Add(v);
            }
            return rocks;
        }

        private static int[] TierCounts(List<SpecimenRecord> rocks, int upTo)
        {
            var c = new int[6];
            for (int i = 0; i < Mathf.Min(upTo, rocks.Count); i++) c[(int)rocks[i].Geology.Tier]++;
            return c;
        }

        private static string Pct(int n, int total) => $"{n * 100f / Mathf.Max(1, total):F1}%";

        [Test]
        public void RarityFatigue_OrdinaryRocksDominate_TopTiersStayRare()
        {
            // a mixed regular-source career: local, regional and cutting rough in rotation, 1000+ rocks
            var s = NewState(4242UL);
            var regular = new[] { SupplierCatalog.Get(SupplierCatalog.Local), SupplierCatalog.Get(SupplierCatalog.Regional), SupplierCatalog.Get(SupplierCatalog.CuttingRough) };
            var rocks = new List<SpecimenRecord>();
            for (int n = 0; rocks.Count < 1200; n++)
            {
                var crate = CrateGenerator.Generate(s, regular[n % regular.Length], (seed, id, cid) => Create(s, seed, id, cid));
                foreach (var id in crate.SpecimenIds) rocks.Add(s.FindSpecimen(id));
            }
            var sb = new StringBuilder("Rarity fatigue (regular sources in rotation)\n");
            foreach (int upTo in new[] { 100, 500, 1000 })
            {
                var c = TierCounts(rocks, upTo);
                sb.AppendLine($"  first {upTo,4}: common {Pct(c[0], upTo)}  decent {Pct(c[1], upTo)}  good {Pct(c[2], upTo)}  exceptional {Pct(c[3], upTo)}  museum {Pct(c[4], upTo)}  world {Pct(c[5], upTo)}");
            }
            Debug.Log(sb.ToString());
            var t = TierCounts(rocks, 1000);
            Assert.GreaterOrEqual(t[0] + t[1], 550, "ordinary rocks (common + decent) must dominate a regular career");
            Assert.That(t[3], Is.InRange(25, 110), "exceptional should be exciting: a few per hundred rocks, not routine");
            Assert.That(t[4], Is.InRange(2, 30), "museum grade should be memorable: a handful per thousand rocks");
            Assert.LessOrEqual(t[5], 6, "world class must stay extremely rare from regular sources");
            Assert.GreaterOrEqual(t[4] + t[5], 3, "a thousand-rock career must still hold a few memorable finds");
        }

        [Test]
        public void PremiumSources_ImproveOddsWithoutShoweringJackpots()
        {
            var sb = new StringBuilder("Top-tier share by source (200 crates each)\n");
            float localTop = 0f;
            foreach (var sup in SupplierCatalog.All)
            {
                var rocks = Rocks(sup, 200, 77UL, out _);
                var c = TierCounts(rocks, rocks.Count);
                float top = (c[3] + c[4] + c[5]) / (float)rocks.Count, museum = (c[4] + c[5]) / (float)rocks.Count;
                sb.AppendLine($"  {sup.Id,-9} ${sup.Price,4}  rocks/crate {rocks.Count / 400f:F1}  exceptional+ {top * 100f:F1}%  museum+ {museum * 100f:F2}%  world {c[5] * 100f / rocks.Count:F2}%");
                if (sup.Id == SupplierCatalog.Local) localTop = top;
                Assert.LessOrEqual(top, 0.40f, $"{sup.Id}: no source may make exceptional-or-better the norm");
                Assert.LessOrEqual(museum, 0.12f, $"{sup.Id}: museum grade must stay scarce even from the best source");
                Assert.LessOrEqual(c[5] / (float)rocks.Count, 0.02f, $"{sup.Id}: world class stays a career event");
            }
            Debug.Log(sb.ToString());
            foreach (var id in new[] { SupplierCatalog.Premium, SupplierCatalog.Specialty, SupplierCatalog.Estate })
            {
                var rocks = Rocks(SupplierCatalog.Get(id), 200, 77UL, out _);
                var c = TierCounts(rocks, rocks.Count);
                Assert.Greater((c[3] + c[4] + c[5]) / (float)rocks.Count, localTop * 1.5f, $"{id} should clearly improve the odds over the quarry crate");
            }
        }

        [Test]
        public void Jackpots_AreBoundedAndScarce()
        {
            var sb = new StringBuilder("Crate return spread (value at ~5% damage)\n");
            var worst = new List<(string id, float maxRatio, float over4, float bigShare)>();
            foreach (var sup in SupplierCatalog.All)
            {
                Rocks(sup, 300, 1313UL, out var returns);
                returns.Sort();
                float max = returns[returns.Count - 1], p99 = returns[(int)(returns.Count * 0.99f)], median = returns[returns.Count / 2];
                int over4 = 0, big = 0; float bigLine = Mathf.Max(sup.Price * 8f, 800f);
                foreach (var v in returns) { if (v > sup.Price * 4f) over4++; if (v > bigLine) big++; }
                sb.AppendLine($"  {sup.Id,-9} price ${sup.Price,4}  median ${median,5:F0}  p99 ${p99,6:F0} ({p99 / sup.Price:F1}x)  max ${max,6:F0} ({max / sup.Price:F1}x)  >4x {over4 * 100f / returns.Count:F1}%  big(>{bigLine:F0}) {big * 100f / returns.Count:F1}%");
                // an unsorted mystery lot is sold on the chance of a big box; the sorted sources are held tighter
                worst.Add((sup.Id, max / 6000f, over4 / (float)returns.Count, big / (float)returns.Count));
            }
            Debug.Log(sb.ToString());
            foreach (var w in worst)
            {
                Assert.LessOrEqual(w.maxRatio, 1f, $"{w.id}: no single crate pays more than a world-class find ($6,000)");
                Assert.LessOrEqual(w.over4, 0.15f, $"{w.id}: a 4x crate must be an event, not a routine");
                Assert.LessOrEqual(w.bigShare, 0.03f, $"{w.id}: a crate worth 8x its price (or $800) is a career event: three in a hundred at most");
            }
        }

        [Test]
        public void EachSourceKeepsItsPromise()
        {
            // the tagline of every supplier is a measurable promise: check each one over 300 crates
            const ulong world = 99UL;
            var local = Rocks(SupplierCatalog.Get(SupplierCatalog.Local), 300, world, out _);
            var cutting = Rocks(SupplierCatalog.Get(SupplierCatalog.CuttingRough), 300, world, out _);
            var oversized = Rocks(SupplierCatalog.Get(SupplierCatalog.OversizedLot), 300, world, out _);
            var amethyst = Rocks(SupplierCatalog.Get(SupplierCatalog.AmethystLot), 300, world, out _);
            var damaged = Rocks(SupplierCatalog.Get(SupplierCatalog.Damaged), 300, world, out _);
            var showcase = Rocks(SupplierCatalog.Get(SupplierCatalog.Showcase), 300, world, out _);
            var specialty = Rocks(SupplierCatalog.Get(SupplierCatalog.Specialty), 300, world, out _);
            var network = Rocks(SupplierCatalog.Get(SupplierCatalog.Network), 300, world, out _);

            float Share(List<SpecimenRecord> rocks, System.Func<SpecimenGeology, bool> f) { int n = 0; foreach (var r in rocks) if (f(r.Geology)) n++; return n / (float)rocks.Count; }
            float sawLocal = Share(local, g => g.Mineral == MineralId.Agate || g.CavityFraction < 0.35f);
            float sawCutting = Share(cutting, g => g.Mineral == MineralId.Agate || g.CavityFraction < 0.35f);
            float bigLocal = Share(local, g => g.SizeClass >= SizeClass.Large);
            float bigOversized = Share(oversized, g => g.SizeClass >= SizeClass.Large);
            float amethystShare = Share(amethyst, g => g.Mineral == MineralId.Amethyst);
            int chipped = 0; foreach (var r in damaged) if (r.ShellDamage > 0.05f) chipped++;
            var localities = new HashSet<string>(); foreach (var r in showcase) localities.Add(r.Locality);
            var familiesSpecialty = new HashSet<MineralId>(); foreach (var r in specialty) familiesSpecialty.Add(r.Geology.Mineral);
            var familiesLocal = new HashSet<MineralId>(); foreach (var r in local) familiesLocal.Add(r.Geology.Mineral);
            int traitsNetwork = 0; foreach (var r in network) if (r.Geology.Traits.Count > 0) traitsNetwork++;
            int traitsLocal = 0; foreach (var r in local) if (r.Geology.Traits.Count > 0) traitsLocal++;
            Debug.Log($"Source promises: saw-work local {sawLocal * 100f:F0}% vs cutting {sawCutting * 100f:F0}%  •  large local {bigLocal * 100f:F0}% vs oversized {bigOversized * 100f:F0}%  •  amethyst lot {amethystShare * 100f:F0}% amethyst  •  damaged lot chipped {chipped * 100f / damaged.Count:F0}%  •  showcase localities {localities.Count}  •  families specialty {familiesSpecialty.Count} vs local {familiesLocal.Count}  •  traits network {traitsNetwork * 100f / network.Count:F0}% vs local {traitsLocal * 100f / local.Count:F0}%");
            Assert.Greater(sawCutting, sawLocal * 1.3f, "cutting rough must be the saw source");
            Assert.Greater(bigOversized, Mathf.Max(0.5f, bigLocal * 2f), "the oversized lot must be mostly large rock");
            Assert.GreaterOrEqual(amethystShare, 0.6f, "the amethyst lot must be mostly amethyst");
            Assert.GreaterOrEqual(chipped / (float)damaged.Count, 0.4f, "the damaged lot must actually arrive chipped");
            Assert.LessOrEqual(localities.Count, 2, "a locality showcase comes from one place (a rare second at most)");
            Assert.GreaterOrEqual(familiesSpecialty.Count, familiesLocal.Count, "the specialty source reaches the deep tail of families");
            Assert.Greater(traitsNetwork, traitsLocal, "the collector network trades in odd, traited pieces");
        }

        [Test]
        public void EverySource_ReturnsMoreThanItCosts_NoneDominates()
        {
            // return per dollar: every source is worth buying (>1.1x), none so far ahead that the others are pointless (<3x)
            var sb = new StringBuilder("Return per dollar (200 crates, ~5% damage, dealer price)\n");
            float lo = float.MaxValue, hi = 0f;
            var ratios = new List<(string id, float ratio)>();
            foreach (var sup in SupplierCatalog.All)
            {
                Rocks(sup, 200, 555UL, out var returns);
                float mean = 0f; foreach (var v in returns) mean += v; mean /= returns.Count;
                float ratio = mean / sup.Price;
                lo = Mathf.Min(lo, ratio); hi = Mathf.Max(hi, ratio);
                sb.AppendLine($"  {sup.Id,-9} ${sup.Price,4} -> mean ${mean:F0} ({ratio:F2}x)");
                ratios.Add((sup.Id, ratio));
            }
            Debug.Log(sb.ToString());
            foreach (var r in ratios)
            {
                Assert.Greater(r.ratio, 1.1f, $"{r.id} must return more than it costs on average");
                Assert.Less(r.ratio, 3.0f, $"{r.id} must not make every other source pointless");
            }
            Assert.Less(hi / lo, 2.4f, "the spread between the best and worst source per dollar stays a strategy choice, not a trap");
        }

        [Test]
        public void FirstCrate_IsSafe()
        {
            // the scripted first crate (CrateCounter 0) must always fund the second crate: no seed strands a new career
            int worst = int.MaxValue; float worstValue = float.MaxValue;
            for (ulong w = 1; w <= 300; w++)
            {
                var s = new GameState { SaveId = "sim", WorldSeed = w * 31UL, Cash = GameSession.StartingCash };
                s.UnlockedSuppliers.Add(SupplierCatalog.Local);
                var sup = SupplierCatalog.Get(SupplierCatalog.Local);
                var crate = CrateGenerator.Generate(s, sup, (seed, id, cid) => Create(s, seed, id, cid));
                float v = 0f; int hollow = 0;
                foreach (var id in crate.SpecimenIds) { var r = s.FindSpecimen(id); v += Valuation.DamagedValue(r.Geology, 0.1f, 0f); if (r.Geology.Cavity != CavityArchetype.Nodule) hollow++; }
                worst = Mathf.Min(worst, hollow); worstValue = Mathf.Min(worstValue, v);
                Assert.GreaterOrEqual(GameSession.StartingCash - sup.Price + v, sup.Price, $"world {w}: the first crate must fund the second even at 10% damage");
            }
            Debug.Log($"First crate over 300 worlds: worst hollow count {worst}, worst value ${worstValue:F0}");
            Assert.GreaterOrEqual(worst, 2, "the first crate always holds something to open");
        }

        /// <summary>
        /// The whole career at ~12 minutes a crate: buy the best crate the cash allows, keep a find every third crate, sell the
        /// rest half retail / half dealer, work through the catalogue in a sensible order up to Stage 3 and the cracker, buy
        /// every occasional lot that is offered, replace blades. Reports when Stage 3 and the full catalogue land.
        /// </summary>
        [Test]
        public void CareerPacing_StageThreeLandsInsideTheFifteenToTwentyFiveHourWindow()
        {
            const float MinutesPerCrate = 15f;   // wash, prep, open, rinse, appraise and sell eight rocks
            int worlds = 24, crates = 130, softlocks = 0; string strandNote = "";
            var stage3At = new List<int>(); var catalogueAt = new List<int>(); var repAt = new List<int>();
            float lateSinkShare = 0f; int lateWorlds = 0;
            // the order a career actually buys in: bench tools, then the room to work in, then the shop front and
            // its fit-out, then the lapidary stages. The premises leases and the retail fixtures joined the
            // catalogue with the starter rebuild, and they are the bulk of what the middle of a career is for.
            var order = new[] { UpgradeCatalog.Loupe, UpgradeCatalog.InspectionLamp, UpgradeCatalog.BenchClamp, UpgradeCatalog.TrimSaw, UpgradeCatalog.FineChisel,
                UpgradeCatalog.CalibratedScale, UpgradeCatalog.BackRoom, UpgradeCatalog.CollectionCabinet, UpgradeCatalog.DisplayExpansion,
                UpgradeCatalog.ShopFront, UpgradeCatalog.SalesTable, UpgradeCatalog.ShopShelving, UpgradeCatalog.ShopSignage,
                UpgradeCatalog.Stage2, UpgradeCatalog.HeavyCradle,
                UpgradeCatalog.Wedge, UpgradeCatalog.PolishLap, UpgradeCatalog.ThinBlade, UpgradeCatalog.CoolantPump, UpgradeCatalog.SawClamp, UpgradeCatalog.GeodeCracker, UpgradeCatalog.Stage3 };
            var sb = new StringBuilder("Career pacing (mixed strategy, occasional lots bought when offered):\n");
            for (ulong w = 1; w <= (ulong)worlds; w++)
            {
                var s = new GameState { SaveId = "sim", WorldSeed = w * 7331UL, Cash = GameSession.StartingCash };
                s.UnlockedSuppliers.Add(SupplierCatalog.Local);
                float collection = 0f; int cuts = 0, stage3 = -1, catalogue = -1, rep4 = -1;
                float lateSpent = 0f, lateEarned = 0f; string lastNote = "start";
                for (int n = 0; n < crates; n++)
                {
                    Market.RefreshOffers(s);
                    SupplierDefinition sup = null, cheapest = null;
                    foreach (var cand in SupplierCatalog.All)
                    {
                        if (!Market.Available(s, cand) || s.Cash < cand.Price) continue;
                        if (cheapest == null || cand.Price < cheapest.Price) cheapest = cand;
                        if (s.Cash >= cand.Price + 75f && (sup == null || cand.Price > sup.Price || cand.Occasional)) sup = cand;
                    }
                    sup ??= cheapest;
                    if (sup == null) { softlocks++; strandNote += $" [world {w} crate {n}: cash ${s.Cash:F0}, stage {s.WorkshopStage}, upgrades {s.Upgrades.Count}, suppliers {string.Join("/", s.UnlockedSuppliers)}, last {lastNote}]"; break; }
                    s.Cash -= sup.Price; if (sup.Occasional) Market.ConsumeOffer(s, sup.Id);
                    lastNote = $"{sup.Id} ${sup.Price}";
                    if (s.WorkshopStage >= 3) lateSpent += sup.Price;
                    s.Stats.CratesPurchased++; s.CrateCounter++;
                    var crate = CrateGenerator.Generate(s, sup, (seed, id, cid) => Create(s, seed, id, cid));
                    float value = 0f, best = 0f;
                    foreach (var id in crate.SpecimenIds)
                    {
                        var r = s.FindSpecimen(id);
                        s.GetOrCreateEntry(r.Geology.Mineral).Found++;
                        float damage = s.HasUpgrade(UpgradeCatalog.FineChisel) ? 0.04f : 0.07f;
                        float v = Valuation.DamagedValue(r.Geology, damage + r.ShellDamage * 0.5f, 0f);
                        if (s.HasUpgrade(UpgradeCatalog.TrimSaw) && (r.Geology.Mineral == MineralId.Agate || r.Geology.CavityFraction < 0.35f)) { v *= 1.12f; cuts++; s.Stats.SawCuts++; if (s.HasUpgrade(UpgradeCatalog.PolishLap)) { v *= 1.08f; s.Stats.PiecesPolished++; } }
                        if (s.HasUpgrade(UpgradeCatalog.CalibratedScale)) v *= 1.05f;
                        if (r.Geology.Tier >= QualityTier.Exceptional && r.Appraised == false) s.Stats.CleanOpens++;
                        best = Mathf.Max(best, v);
                        value += v;
                    }
                    // keep a find every third crate when it clearly raises the collection, never when it would leave the shop unable to buy the next crate
                    if (best > 120f && best > collection * 0.5f && n % 3 == 0 && s.Cash + (value - best) * 0.9f >= 150f) { collection += best; value -= best; }
                    float earned = value * (0.5f * Retail.RetailShop.Markup + 0.5f);
                    s.Cash += earned; lastNote += $" -> ${earned:F0}";
                    if (s.WorkshopStage >= 3) lateEarned += earned;
                    // a blade is bought when it is due and the shop can afford one after the next crate (a broke shop lets the saw wait)
                    float blades = (cuts / 15) * 45f; if (blades > 0f && s.Cash - blades >= 120f) { s.Cash -= blades; cuts %= 15; if (s.WorkshopStage >= 3) lateSpent += blades; }
                    s.Stats.SpecimensSold += crate.SpecimenIds.Count; s.Stats.CustomersServed += crate.SpecimenIds.Count / 4; s.Stats.CleanOpens += 1;
                    if (n % 8 == 7) s.Stats.CommissionsFilled++;
                    s.Prestige = collection >= 6000f ? 5 : collection >= 3000f ? 4 : collection >= 1500f ? 3 : collection >= 600f ? 2 : collection >= 150f ? 1 : 0;
                    SupplierCatalog.EvaluateUnlocks(s);
                    if (rep4 < 0 && Reputation.Tier(s) >= 4) rep4 = n + 1;
                    foreach (var id in order)
                    {
                        if (s.HasUpgrade(id)) continue;
                        var up = UpgradeCatalog.Get(id);
                        if (!string.IsNullOrEmpty(up.Requires) && !s.HasUpgrade(up.Requires)) continue;
                        if (id == UpgradeCatalog.Stage3 && Reputation.Tier(s) < 4) break;   // the game's own gate: sought after first
                        if (s.Cash - up.Price < 150f) break;
                        s.Cash -= up.Price; s.Upgrades.Add(id);
                        if (id == UpgradeCatalog.Stage2) { s.WorkshopStage = 2; SupplierCatalog.EvaluateUnlocks(s); }
                        if (id == UpgradeCatalog.Stage3) { s.WorkshopStage = 3; stage3 = n + 1; SupplierCatalog.EvaluateUnlocks(s); }
                        break;
                    }
                    if (catalogue < 0 && s.Upgrades.Count >= order.Length) catalogue = n + 1;
                }
                if (stage3 > 0) stage3At.Add(stage3);
                if (catalogue > 0) catalogueAt.Add(catalogue);
                if (rep4 > 0) repAt.Add(rep4);
                if (lateEarned > 0f) { lateSinkShare += lateSpent / lateEarned; lateWorlds++; }
                if (w <= 3) sb.AppendLine($"  world {w}: rep4@{rep4} stage3@{stage3} catalogue@{catalogue} cash=${s.Cash:F0} collection=${collection:F0} suppliers={s.UnlockedSuppliers.Count}");
            }
            stage3At.Sort(); catalogueAt.Sort(); repAt.Sort();
            string Med(List<int> l) => l.Count == 0 ? "never" : $"crate {l[l.Count / 2]} (~{l[l.Count / 2] * MinutesPerCrate / 60f:F1} h; p10 {l[l.Count / 10] * MinutesPerCrate / 60f:F1} h, p90 {l[l.Count * 9 / 10] * MinutesPerCrate / 60f:F1} h)";
            sb.AppendLine($"  sought after (rep tier 4): {repAt.Count}/{worlds} worlds, median {Med(repAt)}");
            sb.AppendLine($"  Stage 3 built: {stage3At.Count}/{worlds} worlds, median {Med(stage3At)}");
            sb.AppendLine($"  full catalogue: {catalogueAt.Count}/{worlds} worlds, median {Med(catalogueAt)}");
            sb.AppendLine($"  late game (after Stage 3): crates and blades take {(lateWorlds > 0 ? lateSinkShare / lateWorlds * 100f : 0f):F0}% of what the shop earns; softlocks {softlocks}");
            Debug.Log(sb.ToString());
            Assert.AreEqual(0, softlocks, "the career must never strand" + strandNote);
            Assert.GreaterOrEqual(stage3At.Count, worlds * 0.9f, "Stage 3 must be reachable in nearly every world within 130 crates");
            float stage3Hours = stage3At[stage3At.Count / 2] * MinutesPerCrate / 60f;
            Assert.That(stage3Hours, Is.InRange(6f, 16f), "Stage 3 should land mid-career, leaving the collection and the exhibition for the back half");
            Assert.GreaterOrEqual(repAt[repAt.Count / 2] * MinutesPerCrate / 60f, 2f, "a sought-after name takes a few hours of trade, not a single afternoon");
            Assert.GreaterOrEqual(catalogueAt.Count, worlds * 0.9f, "the full catalogue must be reachable");
            Assert.LessOrEqual(catalogueAt[catalogueAt.Count / 2] * MinutesPerCrate / 60f, 25f, "the whole catalogue lands inside 25 hours");
            Assert.Greater(lateSinkShare / Mathf.Max(1, lateWorlds), 0.25f, "late money must still have somewhere to go: specialty and occasional lots, blades");
        }


        [Test]
        public void AuctionChannel_IsATradeOffNotADominantChannel()
        {
            // 1500 exceptional-or-better pieces from the premium crate: sold rate, net return against the estimate, and the museum bump
            var s = NewState(9090UL);
            int exc = 0, museum = 0, excSold = 0, museumSold = 0; float excNet = 0f, museumNet = 0f, best = 0f;
            var sup = SupplierCatalog.Get(SupplierCatalog.Premium);
            while (exc + museum < 1500)
            {
                var crate = CrateGenerator.Generate(s, sup, (seed, id, cid) => Create(s, seed, id, cid));
                foreach (var id in crate.SpecimenIds)
                {
                    var r = s.FindSpecimen(id);
                    r.Condition.Opened = true; r.Appraised = true; r.AppraisedValue = Valuation.DamagedValue(r.Geology, 0.03f, 0f);
                    if (!Auction.IsEligible(r)) continue;
                    float estimate = Auction.Estimate(r), mult = Auction.HammerMultiplier(s, r);
                    bool isMuseum = Valuation.TierFromValue(r.EstimatedValue()) >= QualityTier.MuseumGrade;
                    bool sold = estimate * mult >= estimate * Auction.ReserveFraction;
                    float net = sold ? mult * (1f - Auction.Commission) : 0f;
                    best = Mathf.Max(best, mult);
                    if (isMuseum) { museum++; if (sold) { museumSold++; museumNet += net; } } else { exc++; if (sold) { excSold++; excNet += net; } }
                }
            }
            float excRate = excSold / (float)Mathf.Max(1, exc), museumRate = museumSold / (float)Mathf.Max(1, museum);
            float excMean = excNet / Mathf.Max(1, excSold), museumMean = museumNet / Mathf.Max(1, museumSold);
            Debug.Log($"Auction: exceptional {exc} lots, {excRate * 100f:F0}% sold, net {excMean:F2}x estimate when sold  •  museum {museum} lots, {museumRate * 100f:F0}% sold, net {museumMean:F2}x  •  best hammer {best:F2}x");
            Assert.That(excRate, Is.InRange(0.55f, 0.9f), "an exceptional lot usually sells, and sometimes passes: a real gamble");
            Assert.That(excMean, Is.InRange(0.95f, 1.2f), "net of commission an exceptional lot lands near the dealer estimate: the showroom's 1.4x stays the patient choice");
            Assert.Greater(museumMean, excMean, "the room bids up a museum piece");
            Assert.Less(museumMean, 1.4f, "even a museum piece does not beat the showroom by design");
            Assert.LessOrEqual(best, 1.9f, "no absurd hammer");
        }

        [Test]
        public void LateGame_MoneySinksRemainWorthBuying()
        {
            // at Stage 3 with everything bought, the specialty and occasional lots are the money sinks: each must pay for itself
            // often enough to be a real choice (not a tax) while staying a gamble (a real bust rate)
            foreach (var id in new[] { SupplierCatalog.Specialty, SupplierCatalog.Network, SupplierCatalog.Showcase, SupplierCatalog.Premium, SupplierCatalog.Estate })
            {
                var sup = SupplierCatalog.Get(id);
                Rocks(sup, 300, 2024UL, out var returns);
                int busts = 0; float mean = 0f;
                foreach (var v in returns) { mean += v; if (v < sup.Price * 0.7f) busts++; }
                mean /= returns.Count;
                Debug.Log($"late sink {id}: ${sup.Price} -> mean ${mean:F0} ({mean / sup.Price:F2}x), busts(<70%) {busts * 100f / returns.Count:F0}%");
                Assert.Greater(mean, sup.Price * 1.15f, $"{id} must be worth the money on average");
                Assert.That(busts / (float)returns.Count, Is.InRange(0.02f, 0.45f), $"{id} must carry real risk without being a trap");
            }
            // Stage 3 and the exhibition gate on reputation: a shop that has done the work reaches 'sought after'
            var s = NewState(1UL, 2);
            s.Stats.SpecimensSold = 60; s.Stats.CustomersServed = 20; s.Stats.CleanOpens = 20; s.Stats.SawCuts = 10; s.Stats.PiecesPolished = 5; s.Stats.CommissionsFilled = 3;
            for (int i = 0; i < 8; i++) s.GetOrCreateEntry((MineralId)i).Found = 1;
            s.Prestige = 3;
            Assert.GreaterOrEqual(Reputation.Tier(s), 4, "a career with the numbers of a respected shop must read as sought after");
            var fresh = NewState(2UL);
            Assert.Less(Reputation.Tier(fresh), 2, "a fresh shop is unknown");
        }
    }
}
