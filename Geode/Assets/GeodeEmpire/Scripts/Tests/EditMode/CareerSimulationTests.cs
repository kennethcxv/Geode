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
    /// §19: rent, power and water were added on top of the old economy, and §19 is explicit that this is not
    /// allowed to stand without a balance pass. These run a deterministic career per archetype (§19.2) with the
    /// operating costs switched on, and assert §19.1's outcomes on the result rather than on intuition.
    ///
    /// The simulation is deliberately coarse — a day is a crate, some processing and some selling — because what
    /// is being measured is whether the money works over weeks, not whether one strike lands. Everything that
    /// decides money (crate generation, valuation, the ledger, upgrade prices) is the shipping code.
    /// </summary>
    public class CareerSimulationTests
    {
        /// <summary>How a player behaves. Everything here is a dial a real player turns by playing differently.</summary>
        private sealed class Archetype
        {
            public string Name;
            /// <summary>Cash kept back rather than spent on stock or floor.</summary>
            public float Float = 120f;
            /// <summary>Fraction of pieces sold over the counter rather than to the dealer.</summary>
            public float RetailShare = 0.5f;
            /// <summary>Crystal damage the player does on the way in: skill, near enough.</summary>
            public float Damage = 0.08f;
            /// <summary>Multiplier on how eagerly leases are signed (1 = as soon as affordable with the float).</summary>
            public float ExpandEagerness = 1f;
            /// <summary>Cap on how dear a crate they will buy.</summary>
            public float MaxCratePrice = 9999f;
            /// <summary>Quality luck applied to every piece.</summary>
            public float Luck = 1f;
        }

        private sealed class Result
        {
            public string Name;
            public float Cash, MinCash, Earned, Spent, BillsPaid;
            public int Days, Missed, LatestFee, CratesBought, Pieces, Advances;
            public int BackRoomDay = -1, ShopFrontDay = -1;
            public float Rent, Power, Water;
            public bool WentBust;
            public float NetWorth => Cash;
            public float ProfitPerDay => Days > 0 ? (Earned - Spent) / Days : 0f;
            public float BillShare => Earned > 0.01f ? BillsPaid / Earned : 0f;
            public readonly StringBuilder Trace = new StringBuilder();
            public override string ToString() =>
                $"{Name,-18} cash {Cash,8:F0}  min {MinCash,7:F0}  earned {Earned,8:F0}  bills {BillsPaid,7:F0} ({BillShare * 100f,4:F0}% of takings)"
                + $"  missed {Missed}  advances {Advances}  crates {CratesBought}  backroom d{(BackRoomDay < 0 ? "-" : BackRoomDay.ToString())}"
                + $"  shopfront d{(ShopFrontDay < 0 ? "-" : ShopFrontDay.ToString())}  pieces {Pieces}";
        }

        private static GameState NewState(ulong seed)
        {
            var s = new GameState { SaveId = "sim", WorldSeed = seed, Cash = GameSession.StartingCash };
            s.UnlockedSuppliers.Add(SupplierCatalog.Local);
            s.Bills.NextBillDay = Ledger.FirstBillDay;
            return s;
        }

        private static SpecimenRecord Create(GameState s, ulong seed, string sup, string crate)
        {
            s.SpecimenCounter++;
            var r = new SpecimenRecord { Id = "S" + s.SpecimenCounter, Seed = seed, SupplierId = sup, CrateId = crate, Location = SpecimenLocation.InCrate };
            s.Specimens.Add(r);
            return r;
        }

        /// <summary>
        /// The float is what a player builds up to, not a floor they hold from day one — nobody sits on $260 of
        /// working capital with $120 in the till. It scales with what they actually have.
        /// </summary>
        private static float Reserve(GameState s, Archetype a) => Mathf.Min(a.Float, s.Cash * 0.30f);

        /// <summary>
        /// The best crate this player is willing and able to buy today, or null. With nothing left to work on the
        /// float goes out of the window: stock is the business, and a reserve you will not spend on the last crate
        /// is how a career freezes at $103 with $124 owed and never moves again.
        /// </summary>
        private static SupplierDefinition PickSupplier(GameState s, Archetype a, bool hasStock)
        {
            float reserve = hasStock ? Reserve(s, a) : 0f;
            SupplierDefinition best = null;
            foreach (var sup in SupplierCatalog.All)
            {
                if (sup.Occasional || !s.HasSupplier(sup.Id)) continue;
                if (sup.Price > a.MaxCratePrice) continue;
                if (s.Cash - sup.Price < reserve) continue;
                if (best == null || sup.Price > best.Price) best = sup;
            }
            return best;
        }

        private static Result Run(Archetype a, ulong seed, int days)
        {
            var s = NewState(seed);
            var rng = new SeededRandom(seed ^ 0x9E3779B97F4A7C15UL);
            var res = new Result { Name = a.Name, Days = days, MinCash = s.Cash };
            var stock = new List<SpecimenRecord>();

            for (int day = 1; day <= days; day++)
            {
                s.Stats.PlayTimeSeconds = (day - 1) * Progression.DaySeconds;

                // --- the solvency backstop the game itself has (GameSession.CheckSolvency) -------
                // Without it a career deadlocks: no cash, so no crate, so no stock, so no earnings, so no cash.
                // That is precisely the spiral §19.1 forbids, and the dealer advance is what forbids it.
                float cheapest = SupplierCatalog.Get(SupplierCatalog.Local).Price;
                if (s.Cash < cheapest && stock.Count == 0)
                {
                    float advance = Mathf.Ceil(cheapest - s.Cash);
                    s.Cash += advance;
                    s.Stats.DealerAdvances++;
                    res.Advances++;
                }

                // --- buy stock -------------------------------------------------------------------
                var sup = PickSupplier(s, a, stock.Count > 0);
                if (sup != null && !(Ledger.PremiumSourcingBlocked(s) && sup.Id != SupplierCatalog.Local))
                {
                    s.Cash -= sup.Price; s.Stats.MoneySpent += sup.Price; res.Spent += sup.Price; res.CratesBought++;
                    var crate = CrateGenerator.Generate(s, sup, (sd, id, cid) => Create(s, sd, id, cid));
                    foreach (var id in crate.SpecimenIds) { var r = s.FindSpecimen(id); if (r != null) stock.Add(r); }
                }

                // --- process and sell ------------------------------------------------------------
                // a day is worth a handful of pieces either way; the split is what the archetype decides
                int worked = Mathf.Min(stock.Count, 4);
                for (int i = 0; i < worked; i++)
                {
                    var r = stock[0]; stock.RemoveAt(0);
                    r.Condition.Opened = true;
                    float value = Valuation.DamagedValue(r.Geology, a.Damage, 0f) * a.Luck;
                    r.Appraised = true; r.AppraisedValue = value;
                    bool retail = rng.NextFloat() < a.RetailShare;
                    // the counter pays the markup but not every piece finds a buyer; the dealer always takes it
                    float take = retail && rng.NextFloat() < 0.62f ? value * RetailShop.Markup
                               : retail ? 0f                                   // still on the shelf at the end of the day
                               : value;
                    if (take <= 0f) { stock.Add(r); continue; }
                    s.Cash += take; s.Stats.MoneyEarned += take; res.Earned += take; res.Pieces++;
                    // the machines that did the work drew power and water while they did it
                    if (s.HasUpgrade(UpgradeCatalog.TrimSaw)) s.Bills.ElectricityUnits += Ledger.DrawPerMinute(UpgradeCatalog.TrimSaw) * 3f;
                    if (s.HasUpgrade(UpgradeCatalog.PolishLap)) s.Bills.ElectricityUnits += Ledger.DrawPerMinute(UpgradeCatalog.PolishLap) * 2f;
                    s.Bills.WaterLitres += Ledger.BasinLitresPerMinute * 1.2f;
                }

                // --- spend on the business -------------------------------------------------------
                TryBuy(s, a, res, day, UpgradeCatalog.CounterTable);
                if (a.RetailShare > 0.2f) TryBuy(s, a, res, day, UpgradeCatalog.Loupe);
                foreach (var id in new[] { UpgradeCatalog.BackRoom, UpgradeCatalog.ShopFront })
                {
                    var def = UpgradeCatalog.Get(id);
                    if (s.HasUpgrade(id) || Ledger.ExpansionBlocked(s)) continue;
                    if (!string.IsNullOrEmpty(def.Requires) && !s.HasUpgrade(def.Requires)) continue;
                    // eagerness decides how much clear water they want over the price before signing
                    float wanted = def.Price + Reserve(s, a) / Mathf.Max(0.05f, a.ExpandEagerness);
                    if (s.Cash < wanted) continue;
                    s.Cash -= def.Price; s.Stats.MoneySpent += def.Price; res.Spent += def.Price;
                    s.Upgrades.Add(id);
                    if (id == UpgradeCatalog.BackRoom) res.BackRoomDay = day; else res.ShopFrontDay = day;
                }

                // --- the bill --------------------------------------------------------------------
                if (day >= s.Bills.NextBillDay && !Ledger.Due(s))
                {
                    res.Rent += Ledger.RentPerPeriod(s);
                    res.Power += Ledger.ElectricityCost(s);
                    res.Water += Ledger.WaterCost(s);
                    Ledger.IssueBill(s, day);
                }
                if (Ledger.Due(s))
                {
                    if (s.Cash >= s.Bills.Outstanding)
                    {
                        float owed = s.Bills.Outstanding;
                        s.Cash -= owed; s.Stats.MoneySpent += owed; res.Spent += owed; res.BillsPaid += owed;
                        s.Bills.Outstanding = 0f; s.Bills.LateFees = 0f; s.Bills.MissedPayments = 0; s.Bills.TotalPaid += owed;
                    }
                    else if (Ledger.PastGrace(s, day) && !s.Bills.FeeAppliedForThisBill)
                    {
                        Ledger.ApplyLateFee(s);
                        res.Missed++;
                        res.LatestFee = day;
                    }
                }

                res.Trace.Append($"d{day} cash {s.Cash:F0} stock {stock.Count} owed {s.Bills.Outstanding:F0}; ");
                res.MinCash = Mathf.Min(res.MinCash, s.Cash);
                if (s.Cash < 0f) res.WentBust = true;
            }
            res.Cash = s.Cash;
            return res;
        }

        private static void TryBuy(GameState s, Archetype a, Result res, int day, string id)
        {
            if (s.HasUpgrade(id)) return;
            var def = UpgradeCatalog.Get(id);
            if (def == null || s.Cash - def.Price < Reserve(s, a)) return;
            s.Cash -= def.Price; s.Stats.MoneySpent += def.Price; res.Spent += def.Price;
            s.Upgrades.Add(id);
        }

        private static readonly Archetype[] Archetypes =
        {
            new Archetype { Name = "conservative",  Float = 260f, RetailShare = 0.35f, Damage = 0.06f, ExpandEagerness = 0.5f, MaxCratePrice = 120f },
            new Archetype { Name = "average",       Float = 140f, RetailShare = 0.50f, Damage = 0.10f, ExpandEagerness = 1.0f },
            new Archetype { Name = "aggressive",    Float =  60f, RetailShare = 0.55f, Damage = 0.14f, ExpandEagerness = 3.0f },
            new Archetype { Name = "slow expander", Float = 300f, RetailShare = 0.45f, Damage = 0.09f, ExpandEagerness = 0.15f },
            new Archetype { Name = "dealer-heavy",  Float = 150f, RetailShare = 0.00f, Damage = 0.09f, ExpandEagerness = 0.8f },
            new Archetype { Name = "retail-heavy",  Float = 150f, RetailShare = 1.00f, Damage = 0.09f, ExpandEagerness = 0.8f },
            new Archetype { Name = "unlucky",       Float = 150f, RetailShare = 0.50f, Damage = 0.26f, ExpandEagerness = 0.8f, Luck = 0.55f },
        };

        private static List<Result> RunAll(int days = 30)
        {
            var all = new List<Result>();
            foreach (var a in Archetypes)
            {
                // three seeds each, and the median career is the one reported: one lucky run proves nothing
                var runs = new List<Result>();
                for (ulong k = 1; k <= 3; k++) runs.Add(Run(a, 8675309UL * k + 17UL, days));
                runs.Sort((x, y) => x.Cash.CompareTo(y.Cash));
                all.Add(runs[1]);
            }
            return all;
        }

        [Test]
        public void CareerReport()
        {
            var sb = new StringBuilder("Careers over 30 days, median of 3 seeds, with rent/power/water charged\n");
            foreach (var r in RunAll()) { sb.AppendLine(r.ToString()); if (r.Name == "dealer-heavy" || r.Name == "average") sb.AppendLine("   " + r.Trace); }
            Debug.Log(sb.ToString());
            Assert.Pass();
        }

        [Test]
        public void Nobody_goes_bust_and_nobody_spirals()
        {
            foreach (var r in RunAll())
            {
                Assert.IsFalse(r.WentBust, $"{r.Name} went bankrupt: {r}");
                Assert.GreaterOrEqual(r.MinCash, 0f, $"{r.Name} dipped below zero: {r}");
                // §19.1: no unavoidable spiral — a career that missed bills has to be able to climb back out
                Assert.Greater(r.Cash, 0f, $"{r.Name} ended broke: {r}");
            }
        }

        [Test]
        public void Day_one_is_survivable_on_the_starting_float()
        {
            foreach (var a in Archetypes)
            {
                var r = Run(a, 4242UL, 1);
                Assert.GreaterOrEqual(r.MinCash, 0f, $"{a.Name} could not survive day one: {r}");
                Assert.IsFalse(Ledger.Due(NewState(1)), "nothing is owed before anything has been earned");
            }
            // and the first bill genuinely lands after the first day, not on it
            Assert.GreaterOrEqual(Ledger.FirstBillDay, 2);
        }

        [Test]
        public void Rent_is_pressure_not_punishment()
        {
            foreach (var r in RunAll())
            {
                if (r.Earned < 1f) continue;
                Assert.Greater(r.BillShare, 0.05f, $"{r.Name} barely notices the bills ({r.BillShare * 100f:F1}% of takings) — no pressure at all");
                // the unlucky career is deliberately squeezed: fixed costs against poor rock is what a bad month is,
                // and what matters there is that it survives it, not that it feels comfortable
                float ceiling = r.Name == "unlucky" ? 0.70f : 0.40f;
                Assert.Less(r.BillShare, ceiling, $"{r.Name} spends {r.BillShare * 100f:F0}% of takings on bills — that is punishment, not pressure");
            }
        }

        [Test]
        public void Selling_over_the_counter_pays_better_than_the_dealer()
        {
            Result dealer = null, retail = null;
            foreach (var r in RunAll()) { if (r.Name == "dealer-heavy") dealer = r; if (r.Name == "retail-heavy") retail = r; }
            Assert.IsNotNull(dealer); Assert.IsNotNull(retail);
            // §19.1 "early customer sales matter": the honest comparison is per piece sold, because the counter's
            // cost is time — a piece can sit on the shelf for days — not margin. Gross depends on throughput.
            float dealerPer = dealer.Earned / Mathf.Max(1, dealer.Pieces);
            float retailPer = retail.Earned / Mathf.Max(1, retail.Pieces);
            Assert.Greater(retailPer, dealerPer * 1.15f,
                $"a counter sale has to be worth the wait\n  dealer {dealerPer:F2}/piece  {dealer}\n  retail {retailPer:F2}/piece  {retail}");
            // ...and §19.1 "dealer remains useful": shipping everything must stay a viable way to run the business
            Assert.Greater(dealer.Cash, 0f, $"the dealer route has to remain survivable\n  {dealer}");
            Assert.Greater(dealer.Earned, retail.Earned * 0.6f,
                $"the dealer must stay worth using\n  {dealer}\n  {retail}");
        }

        [Test]
        public void Expansion_is_reachable_and_its_timing_is_a_real_decision()
        {
            var all = RunAll(60);
            Result fast = null, slow = null;
            int expanded = 0;
            foreach (var r in all)
            {
                if (r.BackRoomDay > 0) expanded++;
                if (r.Name == "aggressive") fast = r;
                if (r.Name == "slow expander") slow = r;
            }
            Assert.Greater(expanded, 0, "§16: nobody could afford the back room in two months — expansion is unreachable\n  "
                                        + string.Join("\n  ", all));
            Assert.IsNotNull(fast); Assert.IsNotNull(slow);
            Assert.IsTrue(fast.BackRoomDay < 0 || slow.BackRoomDay < 0 || fast.BackRoomDay < slow.BackRoomDay,
                          $"eagerness has to change when the lease is signed\n  {fast}\n  {slow}");
            // neither may be strictly dominant: if expanding early were free money, or ruinous, there is no decision
            float ratio = fast.Cash / Mathf.Max(1f, slow.Cash);
            Assert.Greater(ratio, 0.25f, $"expanding early is ruinous\n  {fast}\n  {slow}");
            Assert.Less(ratio, 4.0f, $"expanding early is free money\n  {fast}\n  {slow}");
        }

        [Test]
        public void Bad_luck_costs_a_career_without_ending_it()
        {
            Result unlucky = null, average = null;
            foreach (var r in RunAll()) { if (r.Name == "unlucky") unlucky = r; if (r.Name == "average") average = r; }
            Assert.IsNotNull(unlucky); Assert.IsNotNull(average);
            Assert.Less(unlucky.Cash, average.Cash, "poor rock and heavy damage have to cost something");
            Assert.Greater(unlucky.Cash, 0f, $"§19.1: no unavoidable bankruptcy spiral\n  {unlucky}");
        }

        [Test]
        public void There_is_no_infinite_profit_exploit()
        {
            // Three months of the most aggressive policy. Per-day ratios are useless here — a 30-day figure can
            // sit either side of zero — so this measures the shape of the curve: takings must grow with the days
            // worked, but not faster than the days worked by more than a modest factor.
            var a = Archetypes[2];
            var thirty = Run(a, 991UL, 30);
            var ninety = Run(a, 991UL, 90);
            Assert.Greater(ninety.Earned, thirty.Earned, "a working business should still grow");
            float perDay30 = thirty.Earned / 30f, perDay90 = ninety.Earned / 90f;
            Assert.Less(perDay90, perDay30 * 3f,
                $"takings per day are compounding away: 30d {perDay30:F0}/day, 90d {perDay90:F0}/day");
            // and the till itself must not run away either
            Assert.Less(ninety.Cash, Mathf.Max(400f, thirty.Cash) * 12f,
                $"cash is compounding away: 30d {thirty.Cash:F0}, 90d {ninety.Cash:F0}");
        }
    }
}
