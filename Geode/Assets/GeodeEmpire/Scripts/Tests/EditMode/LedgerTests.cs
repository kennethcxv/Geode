using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Economy;
using GeodeEmpire.Save;

namespace GeodeEmpire.Tests.EditMode
{
    /// <summary>
    /// Rent, utilities and what happens when they are not paid. §17.7 is explicit that late payment must be
    /// recoverable and must never softlock a career, so that is what most of these check.
    /// </summary>
    public sealed class LedgerTests
    {
        private static GameState Fresh()
        {
            var s = new GameState { Cash = 120f };
            s.Bills.NextBillDay = Ledger.FirstBillDay;
            return s;
        }

        [Test]
        public void RentGrowsWithTheFloorLeased()
        {
            var s = Fresh();
            float starter = Ledger.RentPerPeriod(s);
            s.Upgrades.Add(UpgradeCatalog.BackRoom);
            float withBack = Ledger.RentPerPeriod(s);
            s.Upgrades.Add(UpgradeCatalog.ShopFront);
            float withShop = Ledger.RentPerPeriod(s);
            Assert.Greater(withBack, starter, "more floor, more rent");
            Assert.Greater(withShop, withBack);
            Assert.Greater(Ledger.LeasedAreaM2(s), 100f, "three rooms is a lot of floor");
        }

        [Test]
        public void NothingIsOwedOnDayOne()
        {
            var s = Fresh();
            Assert.IsFalse(Ledger.Due(s));
            Assert.GreaterOrEqual(s.Bills.NextBillDay, Ledger.FirstBillDay,
                "a business must trade for a few days before its first bill");
        }

        [Test]
        public void AStarterBillIsAffordableFromOneCrateOfWork()
        {
            // §19.1: rent creates pressure, not punishment. The opening bill has to be payable from ordinary trading.
            var s = Fresh();
            float bill = Ledger.Total(s);
            Assert.Less(bill, 130f, $"the day-4 bill of {bill:F2} is too steep for a business that started on $120");
            Assert.Greater(bill, 60f, "and it has to be worth noticing");
        }

        [Test]
        public void MetersBecomeMoney()
        {
            var s = Fresh();
            float quiet = Ledger.Total(s);
            s.Bills.ElectricityUnits = 40f;
            s.Bills.WaterLitres = 900f;
            float busy = Ledger.Total(s);
            Assert.Greater(busy, quiet, "a week of sawing and washing must cost more than a week of neither");
        }

        [Test]
        public void IssuingABillDoesNotTakeTheMoney()
        {
            // §17.6: no silent deduction, ever
            var s = Fresh();
            float before = s.Cash;
            Ledger.IssueBill(s, Ledger.FirstBillDay);
            Assert.AreEqual(before, s.Cash, "issuing a bill must not touch the till");
            Assert.IsTrue(Ledger.Due(s));
            Assert.Greater(s.Bills.Outstanding, 0f);
            Assert.AreEqual(Ledger.FirstBillDay + 1, s.Bills.DueDay);
            Assert.AreEqual(Ledger.FirstBillDay + Ledger.PeriodDays, s.Bills.NextBillDay);
        }

        [Test]
        public void IssuingABillResetsTheMeters()
        {
            var s = Fresh();
            s.Bills.ElectricityUnits = 30f;
            s.Bills.WaterLitres = 400f;
            Ledger.IssueBill(s, 4);
            Assert.AreEqual(0f, s.Bills.ElectricityUnits, "the next period starts from zero");
            Assert.AreEqual(0f, s.Bills.WaterLitres);
            Assert.IsNotEmpty(s.Bills.LastLines, "the player must be able to see what they were charged for");
        }

        [Test]
        public void LateFeeArrivesOnlyAfterTheGracePeriod()
        {
            var s = Fresh();
            Ledger.IssueBill(s, 4);
            int due = s.Bills.DueDay;
            Assert.IsFalse(Ledger.Overdue(s, due), "not late on the day it is due");
            Assert.IsTrue(Ledger.Overdue(s, due + 1));
            Assert.IsFalse(Ledger.PastGrace(s, due + Ledger.GraceDays), "the grace period is real");
            Assert.IsTrue(Ledger.PastGrace(s, due + Ledger.GraceDays + 1));
        }

        [Test]
        public void MissingBillsBlocksExpansionButNeverTheCareer()
        {
            var s = Fresh();
            Ledger.IssueBill(s, 4);
            Assert.IsFalse(Ledger.ExpansionBlocked(s));
            Ledger.ApplyLateFee(s);
            Assert.IsFalse(Ledger.ExpansionBlocked(s), "one missed bill is a warning");
            Ledger.ApplyLateFee(s);
            Assert.IsTrue(Ledger.ExpansionBlocked(s), "two is a consequence");
            Ledger.ApplyLateFee(s);
            Assert.IsTrue(Ledger.PremiumSourcingBlocked(s));
            // §17.7 and §28: recoverable. Nothing here can end a career.
            s.Bills.MissedPayments = 0;
            Assert.IsFalse(Ledger.ExpansionBlocked(s));
            Assert.IsFalse(Ledger.PremiumSourcingBlocked(s));
        }

        [Test]
        public void LateFeesCompoundGentlyRatherThanRunningAway()
        {
            var s = Fresh();
            Ledger.IssueBill(s, 4);
            float start = s.Bills.Outstanding;
            for (int i = 0; i < 6; i++) Ledger.ApplyLateFee(s);
            Assert.Less(s.Bills.Outstanding, start * 2f,
                "six missed periods must not double the debt: that is the bankruptcy spiral §19.1 rules out");
        }

        [Test]
        public void TheBreakdownAddsUp()
        {
            var s = Fresh();
            s.Upgrades.Add(UpgradeCatalog.TrimSaw);
            s.Bills.ElectricityUnits = 12f;
            s.Bills.WaterLitres = 220f;
            float sum = 0f;
            foreach (var l in Ledger.Breakdown(s)) sum += l.Amount;
            Assert.AreEqual(Ledger.Total(s), sum, 0.02f, "the lines the player is shown must be the bill");
        }

        [Test]
        public void OnlyMachineryOwnedIsServiced()
        {
            var s = Fresh();
            Assert.AreEqual(0f, Ledger.MaintenancePerPeriod(s), "nothing to service on day one");
            s.Upgrades.Add(UpgradeCatalog.TrimSaw);
            Assert.Greater(Ledger.MaintenancePerPeriod(s), 0f);
        }

        [Test]
        public void RunningMachinesCostMoreThanOwningThem()
        {
            // §19.1: advanced machines cost more to operate
            Assert.Greater(Ledger.DrawPerMinute(UpgradeCatalog.TrimSaw), Ledger.DrawPerMinute(UpgradeCatalog.GeodeCracker));
            Assert.Greater(Ledger.DrawPerMinute(UpgradeCatalog.PolishLap), Ledger.DrawPerMinute(UpgradeCatalog.InspectionLamp));
            Assert.AreEqual(0f, Ledger.DrawPerMinute("hand_tool_that_is_not_powered"));
        }
    }
}
