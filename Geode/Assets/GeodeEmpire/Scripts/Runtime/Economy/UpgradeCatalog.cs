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
        /// <summary>A consumable (a fresh blade): bought again and again, never listed as owned.</summary>
        public bool Consumable;
        /// <summary>Only offered once this upgrade is owned.</summary>
        public string Requires;
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
        public const string TrimSaw = "trim_saw";
        public const string SawBlade = "saw_blade";
        public const string ThinBlade = "thin_blade";
        public const string CoolantPump = "coolant_pump";
        public const string SawClamp = "saw_clamp";
        public const string PolishLap = "polish_lap";
        public const string Stage2 = "stage2_workshop";

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
            new UpgradeDefinition { Id = TrimSaw, Name = "Trim Saw", Price = 650f, Order = 5,
                Description = "A 10-inch diamond trim saw with a coolant tray and a carriage vise, under the tarp by the partition.",
                Effect = "A second way to open rock: clamp it, choose the plane, feed it through. Slow, precise, costs blade; cuts flat display faces and slabs, and never shatters a delicate shell." },
            new UpgradeDefinition { Id = SawBlade, Name = "Diamond Blade", Price = 45f, Order = 6, Consumable = true, Requires = TrimSaw,
                Description = "A fresh 10-inch sintered diamond blade.",
                Effect = "Replaces the worn blade. A dull blade cuts slowly and chips the edges of the cut." },
            new UpgradeDefinition { Id = ThinBlade, Name = "Thin-Kerf Blade Profile", Price = 140f, Order = 7, Requires = TrimSaw,
                Description = "A thinner, stiffer blade and a truer arbor.",
                Effect = "Half the kerf: fewer crystals lost along the cut and cleaner faces. Wears a little faster." },
            new UpgradeDefinition { Id = CoolantPump, Name = "Flood Coolant Pump", Price = 110f, Order = 7, Requires = TrimSaw,
                Description = "A recirculating pump flooding the blade instead of the drip feed. The valve still has to be open.",
                Effect = "With the valve open the blade runs cool: much less load when feeding hard, half the chipping, longer blade life." },
            new UpgradeDefinition { Id = SawClamp, Name = "Heavy Vise Jaws", Price = 180f, Order = 8, Requires = TrimSaw,
                Description = "Deeper, stiffer jaws with fresh rubber pads for the carriage vise.",
                Effect = "Tall and awkward rocks stay put under a hard feed instead of shifting and stepping the face." },
            new UpgradeDefinition { Id = PolishLap, Name = "Flat Lap", Price = 420f, Order = 10, Requires = Stage2,
                Description = "A 12-inch flat lap with a diamond pad and a drip feed.",
                Effect = "Polish sawn faces: banded slabs and slices come up glossy and saturated, worth far more. Natural cavities stay as they are." },
            new UpgradeDefinition { Id = Stage2, Name = "Stage 2: Lapidary Workshop", Price = 1400f, Order = 9, Requires = TrimSaw,
                Description = "Contractors, a week of dust, and the room becomes a small professional lapidary shop.",
                Effect = "A lit saw bay with pegboard tooling, a proper polishing corner (the Flat Lap can be fitted), a steel rock rack for nine rough or cut pieces, a trophy wall over the appraisal bench (8 more display slots), a wall shelf in the showroom (4 more sales slots), and access to oversized quarry lots." },
        };

        public static UpgradeDefinition Get(string id)
        {
            foreach (var u in All) if (u.Id == id) return u;
            return null;
        }

        public static bool Has(GameState s, string id) => s != null && s.HasUpgrade(id);
    }
}
