using UnityEngine;
using GeodeEmpire.Save;

namespace GeodeEmpire.Economy
{
    public sealed class UpgradeDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public string Effect;
        public float Price;
        public int Order;
    }

    /// <summary>Few upgrades, each changing how the game plays rather than a hidden percentage.</summary>
    public static class UpgradeCatalog
    {
        public const string FineChisel = "fine_chisel";
        public const string BenchClamp = "bench_clamp";
        public const string InspectionLamp = "inspection_lamp";
        public const string DisplayExpansion = "display_expansion";
        public const string CalibratedScale = "calibrated_scale";

        public static readonly UpgradeDefinition[] All =
        {
            new UpgradeDefinition { Id = InspectionLamp, Name = "Inspection Lamp", Price = 90f, Order = 0,
                Description = "A bright articulated lamp over the cracking bench.",
                Effect = "Shows the fracture ring and stress build-up clearly while you work, and estimates shell thickness." },
            new UpgradeDefinition { Id = BenchClamp, Name = "Bench Clamp", Price = 110f, Order = 1,
                Description = "A padded clamp that holds the rock dead still on the cradle.",
                Effect = "No more slipping on glancing blows; every strike transfers its full force." },
            new UpgradeDefinition { Id = FineChisel, Name = "Fine Chisel", Price = 140f, Order = 2,
                Description = "A narrow hardened chisel with a precision edge.",
                Effect = "Stress stays focused where you place it, so light taps do real work and crystals near the seam survive." },
            new UpgradeDefinition { Id = CalibratedScale, Name = "Calibrated Dealer Scale", Price = 160f, Order = 3,
                Description = "Certified scale and reference cards the dealer trusts.",
                Effect = "Appraisals show an exact value instead of a range, and documented specimens sell for 5% more." },
            new UpgradeDefinition { Id = DisplayExpansion, Name = "Cabinet Shelf Expansion", Price = 220f, Order = 4,
                Description = "Unlock the top shelf of the display cabinet.",
                Effect = "Four more display slots (8 → 12)." },
        };

        public static UpgradeDefinition Get(string id)
        {
            foreach (var u in All) if (u.Id == id) return u;
            return null;
        }

        public static bool Has(GameState s, string id) => s != null && s.HasUpgrade(id);
    }
}
