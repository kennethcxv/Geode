using System;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Save;

namespace GeodeEmpire.Economy
{
    /// <summary>One line on a bill.</summary>
    public readonly struct BillLine
    {
        public readonly string Label, Detail;
        public readonly float Amount;
        public BillLine(string label, float amount, string detail = null) { Label = label; Amount = amount; Detail = detail; }
    }

    /// <summary>
    /// What it costs to keep the doors open. Rent scales with the floor the business has leased, the meters
    /// count what the equipment actually used, and the bill arrives on a known day with a breakdown — never as
    /// a silent deduction (§17.6).
    ///
    /// Kept deliberately small: rent, electricity, water and, once there is machinery worth servicing,
    /// maintenance. §17.4 warns against turning the game into accounting software, so nothing else is modelled.
    /// </summary>
    public static class Ledger
    {
        /// <summary>Days between bills. A shop day is twenty minutes, so this is about an hour of play.</summary>
        public const int PeriodDays = 3;
        /// <summary>Days after the due date before a late fee is added.</summary>
        public const int GraceDays = 2;
        public const float LateFeeRate = 0.08f;
        /// <summary>Bills start once the business is past its first day, so nothing is owed before anything is earned.</summary>
        public const int FirstBillDay = 4;

        // ---- rent (§17.1) --------------------------------------------------------------------
        /// <summary>
        /// Rent per period for the starter unit alone. §19: simulated across seven archetypes, 96 took 44-53% of
        /// every career's takings and nobody could reach the $550 back room inside a month — that is punishment,
        /// not pressure. The starter unit is one small workshop; the expansions are where the rent bites.
        /// </summary>
        public const float UnitRent = 48f;
        public const float BackRoomRent = 74f;
        public const float ShopFrontRent = 138f;

        public static float RentPerPeriod(GameState s)
        {
            if (s == null) return 0f;
            float r = UnitRent;
            if (s.HasUpgrade(UpgradeCatalog.BackRoom)) r += BackRoomRent;
            if (s.HasUpgrade(UpgradeCatalog.ShopFront)) r += ShopFrontRent;
            if (s.HasUpgrade(UpgradeCatalog.Stage3)) r += 90f;      // the specialist lapidary is more floor again
            return r;
        }

        /// <summary>Usable floor, square metres, so the premises page can show what the rent is buying.</summary>
        public static float LeasedAreaM2(GameState s)
        {
            if (s == null) return 0f;
            float a = 41.3f;
            if (s.HasUpgrade(UpgradeCatalog.BackRoom)) a += 34.2f;
            if (s.HasUpgrade(UpgradeCatalog.ShopFront)) a += 41.1f;
            return a;
        }

        // ---- electricity (§17.2) -------------------------------------------------------------
        /// <summary>Price per unit (kWh). Round numbers: the player should be able to do this in their head.</summary>
        public const float PricePerUnit = 0.34f;
        /// <summary>Standing charge per period, whatever is plugged in.</summary>
        public const float ElectricityStanding = 7f;

        /// <summary>What a running machine draws, in units per minute of actual use.</summary>
        public static float DrawPerMinute(string upgradeId) => upgradeId switch
        {
            UpgradeCatalog.TrimSaw => 0.055f,
            UpgradeCatalog.PolishLap => 0.042f,
            UpgradeCatalog.GeodeCracker => 0.020f,
            UpgradeCatalog.CoolantPump => 0.012f,
            UpgradeCatalog.InspectionLamp => 0.002f,
            UpgradeCatalog.UvLamp => 0.003f,
            _ => 0f,
        };

        /// <summary>Lighting a showroom all day is the other real draw, and it is owned rather than used.</summary>
        public static float LightingUnitsPerPeriod(GameState s)
        {
            if (s == null) return 0f;
            float u = 4.2f;                                        // the workshop's own pendants
            if (s.HasUpgrade(UpgradeCatalog.BackRoom)) u += 2.6f;
            if (s.HasUpgrade(UpgradeCatalog.ShopFront)) u += 7.4f;  // a lit shop front is the expensive one
            if (s.HasUpgrade(UpgradeCatalog.ShopSignage)) u += 2.1f;
            return u;
        }

        // ---- water (§17.3) -------------------------------------------------------------------
        /// <summary>Price per hundred litres.</summary>
        public const float PricePerHundredLitres = 0.42f;
        public const float WaterStanding = 4f;
        /// <summary>Litres a minute at the basin, and again with the nozzle running.</summary>
        public const float BasinLitresPerMinute = 7.5f;
        public const float NozzleLitresPerMinute = 13f;

        // ---- maintenance (§17.4) -------------------------------------------------------------
        /// <summary>Servicing, once there is machinery worth servicing. Nothing to pay before there is.</summary>
        public static float MaintenancePerPeriod(GameState s)
        {
            if (s == null) return 0f;
            float m = 0f;
            if (s.HasUpgrade(UpgradeCatalog.TrimSaw)) m += 9f;
            if (s.HasUpgrade(UpgradeCatalog.PolishLap)) m += 8f;
            if (s.HasUpgrade(UpgradeCatalog.GeodeCracker)) m += 6f;
            if (s.HasUpgrade(UpgradeCatalog.UtilitySink)) m += 3f;
            return m;
        }

        // ---- the bill ------------------------------------------------------------------------
        public static float ElectricityCost(GameState s)
        {
            if (s == null) return 0f;
            float units = s.Bills.ElectricityUnits + LightingUnitsPerPeriod(s);
            return ElectricityStanding + units * PricePerUnit;
        }

        public static float WaterCost(GameState s)
        {
            if (s == null) return 0f;
            return WaterStanding + s.Bills.WaterLitres / 100f * PricePerHundredLitres;
        }

        public static List<BillLine> Breakdown(GameState s)
        {
            var lines = new List<BillLine>(5);
            if (s == null) return lines;
            lines.Add(new BillLine("Rent", RentPerPeriod(s), $"{LeasedAreaM2(s):F0} m² leased"));
            lines.Add(new BillLine("Electricity", ElectricityCost(s),
                $"{s.Bills.ElectricityUnits + LightingUnitsPerPeriod(s):F1} units at {PricePerUnit:0.00}"));
            lines.Add(new BillLine("Water", WaterCost(s), $"{s.Bills.WaterLitres:F0} litres"));
            float maint = MaintenancePerPeriod(s);
            if (maint > 0f) lines.Add(new BillLine("Equipment service", maint, "cover on the machines you own"));
            if (s.Bills.LateFees > 0.005f) lines.Add(new BillLine("Late fee", s.Bills.LateFees, "carried over"));
            return lines;
        }

        public static float Total(GameState s)
        {
            float t = 0f;
            foreach (var l in Breakdown(s)) t += l.Amount;
            return Mathf.Round(t * 100f) / 100f;
        }

        /// <summary>What is on the meters right now, before the period ends: the estimate §17.5 asks for.</summary>
        public static float EstimatedNext(GameState s) => Total(s);

        /// <summary>Cost per day at the current rate, for the operating-costs page (§18).</summary>
        public static float PerDay(GameState s) => Total(s) / PeriodDays;

        public static bool Due(GameState s) => s != null && s.Bills.Outstanding > 0.005f;

        /// <summary>Days until the bill lands, or since it was due (negative when overdue).</summary>
        public static int DaysUntilDue(GameState s, int today)
        {
            if (s == null) return 0;
            return (Due(s) ? s.Bills.DueDay : s.Bills.NextBillDay) - today;
        }

        public static bool Overdue(GameState s, int today) => Due(s) && today > s.Bills.DueDay;
        public static bool PastGrace(GameState s, int today) => Due(s) && today > s.Bills.DueDay + GraceDays;

        /// <summary>
        /// Close the period: the meters become a bill, the bill gets a due date, and the meters reset. Nothing is
        /// taken from the till here — §17.6 is explicit that the money moves only when the player pays.
        /// </summary>
        public static void IssueBill(GameState s, int today)
        {
            if (s == null) return;
            float amount = Total(s);
            s.Bills.LastBillAmount = amount;
            s.Bills.LastBillDay = today;
            s.Bills.Outstanding += amount;
            s.Bills.DueDay = today + 1;
            s.Bills.NextBillDay = today + PeriodDays;
            s.Bills.LastLines.Clear();
            foreach (var l in Breakdown(s)) s.Bills.LastLines.Add(l.Label + "|" + l.Amount.ToString("F2") + "|" + (l.Detail ?? ""));
            s.Bills.ElectricityUnits = 0f;
            s.Bills.WaterLitres = 0f;
            s.Bills.LateFees = 0f;
        }

        /// <summary>A late fee, once, when the grace period runs out (§17.7).</summary>
        public static float ApplyLateFee(GameState s)
        {
            if (s == null || s.Bills.Outstanding <= 0.005f) return 0f;
            float fee = Mathf.Round(s.Bills.Outstanding * LateFeeRate * 100f) / 100f;
            s.Bills.Outstanding += fee;
            s.Bills.LateFees += fee;
            s.Bills.MissedPayments++;
            s.Bills.FeeAppliedForThisBill = true;
            return fee;
        }

        /// <summary>
        /// Graduated consequences, never a softlock (§17.7). Two missed bills and the landlord stops approving new
        /// floor; three and the good suppliers want cash up front. Paying up clears it — the career always recovers.
        /// </summary>
        public static bool ExpansionBlocked(GameState s) => s != null && s.Bills.MissedPayments >= 2;
        public static bool PremiumSourcingBlocked(GameState s) => s != null && s.Bills.MissedPayments >= 3;

        public static string StandingWarning(GameState s, int today)
        {
            if (s == null || !Due(s)) return null;
            if (PastGrace(s, today)) return $"{UI.UiKit.Money(s.Bills.Outstanding)} overdue. A late fee has been added and new floor is on hold until it is paid.";
            if (Overdue(s, today)) return $"{UI.UiKit.Money(s.Bills.Outstanding)} is overdue. You have {s.Bills.DueDay + GraceDays - today} days before a late fee.";
            return $"{UI.UiKit.Money(s.Bills.Outstanding)} due on day {s.Bills.DueDay}.";
        }
    }
}
