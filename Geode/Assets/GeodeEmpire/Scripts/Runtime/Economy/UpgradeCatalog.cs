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
        /// <summary>Tablet grouping: PREMISES, BENCH, MACHINES, RETAIL, DISPLAYS, STORAGE.</summary>
        public string Category = "BENCH";
        /// <summary>
        /// What appears in the world and where it lands, in one line. §9.3: the player must be able to tell a
        /// purchase that changes geometry from one that changes a number, before they spend the money.
        /// </summary>
        public string WorldChange;
        /// <summary>Baked preview texture under Resources/UI/Upgrades. Empty falls back to the category mark.</summary>
        public string Icon;
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
        public const string GeodeCracker = "geode_cracker";
        public const string PolishLap = "polish_lap";
        public const string CollectionCabinet = "collection_cabinet";
        public const string BackRoom = "premises_backroom";
        public const string ShopFront = "premises_shopfront";
        public const string ShopShelving = "retail_shelving";
        public const string ShopSignage = "retail_signage";
        public const string Stage2 = "stage2_workshop";
        public const string Stage3 = "stage3_workshop";

        public static readonly UpgradeDefinition[] All =
        {
            new UpgradeDefinition { Id = Loupe, Category = "BENCH", Name = "Jeweller's Loupe", Price = 45f, Order = -1,
                Description = "A folding 10x brass loupe for reading rock up close.",
                Effect = "Hold a rock and raise the loupe: exposed mineral, banding, hairline cracks and chips come into focus. It never shows what is inside.",
                WorldChange = "Goes on your belt. Nothing is built." },
            new UpgradeDefinition { Id = InspectionLamp, Category = "BENCH", Name = "Inspection Lamp", Price = 110f, Order = 0,
                Description = "A bright articulated lamp over the cracking bench.",
                Effect = "Shows the fracture ring and stress build-up clearly while you work, and estimates shell thickness.",
                WorldChange = "An articulated lamp is fitted over the cracking bench." },
            new UpgradeDefinition { Id = BenchClamp, Category = "BENCH", Name = "Bench Clamp", Price = 140f, Order = 1,
                Description = "A padded clamp that holds the rock dead still on the cradle.",
                Effect = "No more slipping on glancing blows; every strike transfers its full force.",
                WorldChange = "A clamp is fitted to the bench cradle." },
            new UpgradeDefinition { Id = FineChisel, Category = "BENCH", Name = "Fine Chisel", Price = 180f, Order = 2,
                Description = "A narrow hardened chisel with a precision edge.",
                Effect = "Stress stays focused where you place it, so light taps do real work and crystals near the seam survive.",
                WorldChange = "Joins the tools on the pegboard." },
            new UpgradeDefinition { Id = CalibratedScale, Category = "BENCH", Name = "Calibrated Dealer Scale", Price = 200f, Order = 3,
                Description = "Certified scale and reference cards the dealer trusts.",
                Effect = "Appraisals show an exact value instead of a range, and documented specimens sell for 5% more.",
                WorldChange = "A certified scale replaces the one on the inspection bench." },
            new UpgradeDefinition { Id = DisplayExpansion, Category = "DISPLAYS", Name = "Cabinet Shelf Expansion", Price = 260f, Order = 4, Requires = CollectionCabinet,
                Description = "Unlock the top shelf of the display cabinet.",
                Effect = "Four more display slots (8 → 12).",
                WorldChange = "The cabinet\u2019s top shelf is glazed and lit." },
            new UpgradeDefinition { Id = SalesTable, Category = "RETAIL", Name = "Showroom Island Counter", Price = 520f, Order = 8, Requires = ShopFront,
                Description = "A glazed island counter for the middle of the shop, with felt risers at each end.",
                Effect = "Twelve more sale slots: four on the glass, two hero risers and six behind the glazing, all where a browsing customer meets them first.",
                WorldChange = "A glazed island counter is delivered. You choose where it stands in the showroom." },
            new UpgradeDefinition { Id = Wedge, Category = "BENCH", Name = "Splitting Wedge & Lump Hammer", Price = 130f, Order = 2,
                Description = "A hardened wedge and a heavier hammer for big rough.",
                Effect = "Drives a far stronger crack into large and oversized rocks. Too much for thin shells: it goes straight through into the crystals.",
                WorldChange = "A wedge and lump hammer join the pegboard." },
            new UpgradeDefinition { Id = HeavyCradle, Category = "BENCH", Name = "Heavy Cradle", Price = 240f, Order = 2,
                Description = "A wide sandbag ring on a steel plate with three padded posts.",
                Effect = "Oversized rough sits dead still instead of rocking under every blow: no more skidded strikes, full force into the seam.",
                WorldChange = "Replaces the cradle on the cracking bench." },
            new UpgradeDefinition { Id = TrimSaw, Category = "MACHINES", Name = "Trim Saw", Price = 650f, Order = 5,
                Description = "A 10-inch diamond trim saw with a coolant tray and a carriage vise, under the tarp by the partition.",
                Effect = "A second way to open rock: clamp it, choose the plane, feed it through. Slow, precise, costs blade; cuts flat display faces and slabs, and never shatters a delicate shell.",
                WorldChange = "Delivered crated. You site the saw on the workshop floor." },
            new UpgradeDefinition { Id = SawBlade, Category = "MACHINES", Name = "Diamond Blade", Price = 45f, Order = 6, Consumable = true, Requires = TrimSaw,
                Description = "A fresh 10-inch sintered diamond blade.",
                Effect = "Replaces the worn blade. A dull blade cuts slowly and chips the edges of the cut.",
                WorldChange = "Fitted to the saw at once." },
            new UpgradeDefinition { Id = ThinBlade, Category = "MACHINES", Name = "Thin-Kerf Blade Profile", Price = 140f, Order = 7, Requires = TrimSaw,
                Description = "A thinner, stiffer blade and a truer arbor.",
                Effect = "Half the kerf: fewer crystals lost along the cut and cleaner faces. Wears a little faster.",
                WorldChange = "Fitted to the saw at once." },
            new UpgradeDefinition { Id = CoolantPump, Category = "MACHINES", Name = "Flood Coolant Pump", Price = 110f, Order = 7, Requires = TrimSaw,
                Description = "A recirculating pump flooding the blade instead of the drip feed. The valve still has to be open.",
                Effect = "With the valve open the blade runs cool: much less load when feeding hard, half the chipping, longer blade life.",
                WorldChange = "A pump and hose are fitted to the saw." },
            new UpgradeDefinition { Id = SawClamp, Category = "MACHINES", Name = "Heavy Vise Jaws", Price = 180f, Order = 8, Requires = TrimSaw,
                Description = "Deeper, stiffer jaws with fresh rubber pads for the carriage vise.",
                Effect = "Tall and awkward rocks stay put under a hard feed instead of shifting and stepping the face.",
                WorldChange = "New jaws are fitted to the carriage vise." },
            new UpgradeDefinition { Id = GeodeCracker, Category = "MACHINES", Name = "Geode Cracker", Price = 380f, Order = 10, Requires = Stage2,
                Description = "A chain splitter on a stand: a hardened chain round the seam, a ratchet lever to squeeze it.",
                Effect = "A third way to open rock: slow to set but a level, well-seated geode splits clean all the way round with far less crystal damage than hammering. Takes nothing over 11 cm.",
                WorldChange = "Delivered crated. You site the press on the workshop floor." },
            new UpgradeDefinition { Id = PolishLap, Category = "MACHINES", Name = "Flat Lap", Price = 420f, Order = 10, Requires = Stage2,
                Description = "A 12-inch flat lap with a diamond pad and a drip feed.",
                Effect = "Polish sawn faces: banded slabs and slices come up glossy and saturated, worth far more. Natural cavities stay as they are.",
                WorldChange = "Delivered crated. You site the lap on the workshop floor." },
            new UpgradeDefinition { Id = Stage3, Category = "PREMISES", Name = "Stage 3: Specialist Lapidary", Price = 3200f, Order = 12, Requires = Stage2,
                Description = "The established specialist: a 24-inch slab saw in the bay, a UV verification lamp at the scale, a gallery of lit plinths and a second case in the showroom, a bigger receiving bay.",
                Effect = "The slab saw passes rock up to 25 cm; exceptional pieces are verified and certified; three gallery plinths and six more sales slots; six crates on the pallets. Needs a respected name in the trade.",
                WorldChange = "The bay is rebuilt for the slab saw and the showroom gains a gallery run." },
            new UpgradeDefinition { Id = CollectionCabinet, Category = "DISPLAYS", Name = "Collection Cabinet", Price = 240f, Order = 3,
                Description = "A glazed cabinet with a lit shelf, for the pieces you decide not to sell.",
                Effect = "Eight slots for a private collection. A piece put here leaves the business for good, and the room is better for it.",
                WorldChange = "Delivered flat-packed. You choose which wall it goes against." },
            new UpgradeDefinition { Id = BackRoom, Category = "PREMISES", Name = "Back Room Lease", Price = 550f, Order = 4,
                Description = "The landlord holds the keys to the room behind the workshop. Take it on and the hoarding across the north wall comes down.",
                Effect = "A proper goods-in bay under the roller shutter with four pallets, steel storage racking, a sorting table, and a desk to run the business from.",
                WorldChange = "The boarded opening in the north wall is opened up. The back room is fitted out at once \u2014 nothing to site." },
            new UpgradeDefinition { Id = ShopFront, Category = "PREMISES", Name = "Shop Front Lease", Price = 1200f, Order = 8, Requires = BackRoom,
                Description = "The empty unit next door faces the street. Sign for it and the hoarding across the workshop comes down.",
                Effect = "A showroom the public can walk into: the street door opens, a checkout counter goes in, and customers start arriving. It comes bare \u2014 fitting it out is on you.",
                WorldChange = "The hoarding is removed and the showroom shell is opened. Counter and till are fitted; the shelving and signage are separate." },
            new UpgradeDefinition { Id = ShopShelving, Category = "RETAIL", Name = "Wall Display Shelving", Price = 780f, Order = 8, Requires = ShopFront,
                Description = "Two lit shelving runs against the showroom walls, with a strip light under every shelf.",
                Effect = "Twelve more sale slots, each lit well enough that a customer stops at it. Empty shelves do not sell.",
                WorldChange = "Two shelving units are delivered crated. You site each one against a showroom wall." },
            new UpgradeDefinition { Id = ShopSignage, Category = "RETAIL", Name = "Sign & Fit-Out", Price = 640f, Order = 9, Requires = ShopFront,
                Description = "The shop\u2019s name in lit letters over the back wall, glass pendants down the room, and something green in the corners.",
                Effect = "The showroom stops looking like a leased unit. Customers browse longer, and browse further in.",
                WorldChange = "The sign, pendant globes and planters are fitted on purchase \u2014 nothing to site." },
            new UpgradeDefinition { Id = Stage2, Category = "PREMISES", Name = "Stage 2: Lapidary Workshop", Price = 1400f, Order = 9, Requires = TrimSaw,
                Description = "Contractors, a week of dust, and the room becomes a small professional lapidary shop.",
                Effect = "A lit saw bay with pegboard tooling, a proper polishing corner (the Flat Lap can be fitted), a steel rock rack for nine rough or cut pieces, a trophy wall over the appraisal bench (8 more display slots), a wall shelf in the showroom (4 more sales slots), and access to oversized quarry lots.",
                WorldChange = "Contractors fit out the workshop: saw bay, polishing corner, rock rack and trophy wall." },
        };

        public static UpgradeDefinition Get(string id)
        {
            foreach (var u in All) if (u.Id == id) return u;
            return null;
        }

        public static bool Has(GameState s, string id) => s != null && s.HasUpgrade(id);
    }
}
