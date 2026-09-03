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
        public const string Loupe = "loupe";
        public const string SalesTable = "sales_table";
        public const string HeavyCradle = "heavy_cradle";
        public const string Wedge = "wedge";

        public static readonly UpgradeDefinition[] All =
        {
            new UpgradeDefinition { Id = Loupe, Name = "Jeweller's Loupe", Price = 45f, Order = -1,
                Description = "A folding 10x brass loupe for reading rock up close.",
                Effect = "Hold a rock and raise the loupe: exposed mineral, banding, hairline cracks and chips come into focus. It never shows what is inside." },
            new UpgradeDefinition { Id = InspectionLamp, Name = "Inspection Lamp", Price = 110f, Order = 0,
                Description = "A bright articulated lamp over the cracking bench.",
                Effect = "Shows the fracture ring and stress build-up clearly while you work, and estimates shell thickness." },
            new UpgradeDefinition { Id = BenchClamp, Name = "Bench Clamp", Price = 140f, Order = 1,
                Description = "A padded clamp that holds the rock dead still on the cradle.",
                Effect = "No more slipping on glancing blows; every strike transfers its full force." },
            new UpgradeDefinition { Id = FineChisel, Name = "Fine Chisel", Price = 180f, Order = 2,
                Description = "A narrow hardened chisel with a precision edge.",
                Effect = "Stress stays focused where you place it, so light taps do real work and crystals near the seam survive." },
            new UpgradeDefinition { Id = CalibratedScale, Name = "Calibrated Dealer Scale", Price = 200f, Order = 3,
                Description = "Certified scale and reference cards the dealer trusts.",
                Effect = "Appraisals show an exact value instead of a range, and documented specimens sell for 5% more." },
            new UpgradeDefinition { Id = DisplayExpansion, Name = "Cabinet Shelf Expansion", Price = 260f, Order = 4,
                Description = "Unlock the top shelf of the display cabinet.",
                Effect = "Four more display slots (8 → 12)." },
            new UpgradeDefinition { Id = SalesTable, Name = "Showroom Island Table", Price = 160f, Order = 3,
                Description = "A felt-topped island table in the middle of the shop.",
                Effect = "Four more sales slots (6 → 10), where browsing customers see them first." },
            new UpgradeDefinition { Id = Wedge, Name = "Splitting Wedge & Lump Hammer", Price = 130f, Order = 2,
                Description = "A hardened wedge and a heavier hammer for big rough.",
                Effect = "Drives a far stronger crack into large and oversized rocks. Too much for thin shells: it goes straight through into the crystals." },
            new UpgradeDefinition { Id = HeavyCradle, Name = "Heavy Cradle", Price = 240f, Order = 2,
                Description = "A wide sandbag ring on a steel plate with three padded posts.",
                Effect = "Oversized rough sits dead still instead of rocking under every blow: no more skidded strikes, full force into the seam." },
        };

        public static UpgradeDefinition Get(string id)
        {
            foreach (var u in All) if (u.Id == id) return u;
            return null;
        }

        public static bool Has(GameState s, string id) => s != null && s.HasUpgrade(id);
    }
}
