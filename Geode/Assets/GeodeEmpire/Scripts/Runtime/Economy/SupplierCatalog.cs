using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Economy
{
    /// <summary>A sourcing strategy, not a tier: price, count, and how outcomes are distributed.</summary>
    public sealed class SupplierDefinition
    {
        public string Id;
        public string Name;
        public string Tagline;
        public string Description;
        public float Price;
        public int MinRocks, MaxRocks;
        public bool CountHidden;
        public float[] TierWeights;       // Common, Decent, Good, Exceptional, Museum, WorldClass
        public string UnlockHint;
        /// <summary>Named localities this source draws from; a crate picks one (seeded) and its rocks carry it.</summary>
        public string[] Localities = System.Array.Empty<string>();
        /// <summary>Formation bias: the cavity types this source favours (and the share of the crate that follows it).</summary>
        public CavityArchetype[] PreferredCavities;
        public float CavityShare = 0.7f;
        /// <summary>Shell character: thin-shelled (below 1) or thick-shelled (above 1) sources filter their rough by shell thickness.</summary>
        public float ShellBias = 1f;
        /// <summary>Chipped rough: rocks arrive with some shell chipping and a few crystals already broken, at a discount.</summary>
        public float PreDamage;
        /// <summary>A crate never comes in under Price x FloorFactor at dealer prices (0 = no floor): the quarry sells by weight.</summary>
        public float FloorFactor;
        /// <summary>An occasional lot: offered now and then (see SpecialLots), not always on the tablet.</summary>
        public bool Occasional;
        public Color Accent;
        /// <summary>Mineral families this source favours (null = the quarry's natural mix).</summary>
        public MineralId[] PreferredMinerals;
        /// <summary>Fraction of the crate drawn from the preferred families.</summary>
        public float PreferredShare;
        /// <summary>Plain-language expectations shown on the tablet: character, risk, likely minerals, exterior clue.</summary>
        public string Character, Risk, Minerals, Clue;
        /// <summary>Physical size mix of the rough (Small, Medium, Large, Oversized); null = the quarry's natural mix.</summary>
        public float[] SizeWeights;
        /// <summary>Multiplier on the clay coating the rough arrives with (a dealer who pre-cleans ships at 0.2).</summary>
        public float DirtScale = 1f;
        /// <summary>Whether the rough already arrives scrubbed and inspectable.</summary>
        public bool Prepared => DirtScale < 0.35f;

        public string RockCountLabel => CountHidden ? "6–12 rocks (unsorted)" : MinRocks == MaxRocks ? $"{MinRocks} rocks" : $"{MinRocks}–{MaxRocks} rocks";
    }

    public static class SupplierCatalog
    {
        public const string Local = "local";
        public const string Regional = "regional";
        public const string AmethystLot = "amethyst";
        public const string Estate = "estate";
        public const string Premium = "premium";
        public const string CuttingRough = "cutting";
        public const string DesertPocket = "desert";
        public const string OversizedLot = "oversized";
        public const string Network = "network";          // collector network: traded pieces from other collectors, odd and good
        public const string Showcase = "showcase";        // locality showcase: one famous locality, its signature look
        public const string Damaged = "damaged";          // damaged-but-promising: chipped rough at a discount
        public const string Specialty = "specialty";      // late-game specialty source: the rare families, the deep tail

        public static readonly SupplierDefinition[] All =
        {
            new SupplierDefinition
            {
                Id = Local, Name = "Local Quarry Mixed Crate", Tagline = "Cheap volume. Mostly ordinary. Now and then, not.",
                Description = "Unsorted geodes straight off the quarry sorting belt. Great hammer practice and the occasional strange local outlier.",
                Price = 75f, MinRocks = 9, MaxRocks = 10, CountHidden = false, FloorFactor = 1.4f,
                TierWeights = new[] { 0.62f, 0.25f, 0.09f, 0.03f, 0.003f, 0.0003f },
                UnlockHint = "", Accent = new Color(0.75f, 0.62f, 0.42f),
                Character = "Broad common material with a low floor. Volume for the outbox and hammer practice.",
                Risk = "Cheap gamble: most of the crate is ordinary, one piece now and then is not.",
                Minerals = "Quartz, agate and calcite mostly; anything can turn up.",
                Clue = "Small and medium rough, caked in quarry clay. Few surface hints until it is washed.",
                SizeWeights = new[] { 0.42f, 0.5f, 0.08f, 0f }, DirtScale = 1.15f,
                Localities = new[] { "Keokuk beds", "Dugway flats", "Pinnacle Ridge quarry", "the old Marsh pit" },
            },
            new SupplierDefinition
            {
                Id = Regional, Name = "Regional Curated Crate", Tagline = "Hand-picked. Fewer duds, fewer miracles.",
                Description = "A regional dealer pre-sorts these by weight and sound. A better floor and steadier value, but the wild outliers were skimmed off already.",
                Price = 190f, MinRocks = 8, MaxRocks = 8, CountHidden = false,
                TierWeights = new[] { 0.28f, 0.40f, 0.24f, 0.065f, 0.010f, 0.0012f },
                UnlockHint = "Unlocks after your first sale to the dealer.", Accent = new Color(0.45f, 0.65f, 0.55f),
                Character = "Pre-sorted by weight and sound: a reliable floor, steady value, no miracles.",
                Risk = "Low. The duds and the jackpots were both skimmed off before you saw the crate.",
                Minerals = "The full range, weighted toward hollow quartz-family geodes.",
                Clue = "Consistent medium rough, lightly brushed; the dealer's chalk marks the heavier ones.",
                SizeWeights = new[] { 0.1f, 0.78f, 0.12f, 0f }, DirtScale = 0.6f,
            },
            new SupplierDefinition
            {
                Id = AmethystLot, Name = "Amethyst Lot", Tagline = "One mine, one mineral. Purple or bust.",
                Description = "A pallet straight from an amethyst working. Most of it is amethyst; deep-purple cathedrals happen here and almost nowhere else.",
                Price = 230f, MinRocks = 8, MaxRocks = 9, CountHidden = false,
                TierWeights = new[] { 0.36f, 0.32f, 0.2f, 0.09f, 0.015f, 0.002f },
                UnlockHint = "Unlocks after you have opened an amethyst.", Accent = new Color(0.6f, 0.42f, 0.85f),
                PreferredMinerals = new[] { MineralId.Amethyst }, PreferredShare = 0.78f,
                Character = "Little variety, strong colour odds: this is where the cathedrals come from.",
                Risk = "Medium. A pale lot is disappointing; a saturated one pays for the next three crates.",
                Minerals = "Amethyst, with a little clear quartz and calcite from the same seams.",
                Clue = "Round, heavy medium and large rough; purple staining in the pits once the clay is off.",
                SizeWeights = new[] { 0.1f, 0.55f, 0.3f, 0.05f }, DirtScale = 0.9f,
            },
            new SupplierDefinition
            {
                Id = Estate, Name = "Estate Mystery Lot", Tagline = "Somebody's collection, boxed up unsorted. Could be anything.",
                Description = "Unknown provenance, unknown count. Estate lots can be a box of gravel or the best week of your career.",
                Price = 260f, MinRocks = 6, MaxRocks = 12, CountHidden = true,
                TierWeights = new[] { 0.50f, 0.20f, 0.15f, 0.10f, 0.03f, 0.004f },
                UnlockHint = "Unlocks after your second crate, once you have a specimen on display.", Accent = new Color(0.62f, 0.45f, 0.72f),
                PreferredMinerals = new[] { MineralId.Celestite, MineralId.Fluorite, MineralId.Pyrite, MineralId.Aragonite, MineralId.SmokyQuartz, MineralId.Halite, MineralId.Stibnite }, PreferredShare = 0.45f,
                Character = "Somebody's unsorted collection. Odd families, odd combinations, unknown count.",
                Risk = "High. Half a box of gravel is normal; so is the best piece of your month.",
                Minerals = "Skews to the unusual: celestite, fluorite, pyrite, aragonite, smoky quartz.",
                Clue = "Every size from a fist to a head; old labels, mixed matrix colours, some already chipped.",
                SizeWeights = new[] { 0.3f, 0.35f, 0.25f, 0.1f }, DirtScale = 0.8f,
            },
            new SupplierDefinition
            {
                Id = Premium, Name = "Premium Dealer Crate", Tagline = "Invitation only. Display-grade floor.",
                Description = "A dealer who only sells to serious collectors. Expensive, reliable, beautiful, and rarely the biggest upside.",
                Price = 520f, MinRocks = 7, MaxRocks = 7, CountHidden = false,
                TierWeights = new[] { 0.06f, 0.28f, 0.46f, 0.16f, 0.035f, 0.003f },   // V5: a premium lot raises the floor, it does not hand out museum pieces
                UnlockHint = "Invitation arrives when your displayed collection is worth $1,500.", Accent = new Color(0.85f, 0.7f, 0.35f),
                Character = "Display-grade material with a high floor. Beautiful, expensive, rarely the biggest upside.",
                Risk = "Low on junk, capped on jackpots. You pay for certainty.",
                Minerals = "Whatever is showing best that month: saturated quartz family, fluorite, celestite.",
                Clue = "Numbered, wrapped, scrubbed clean and pre-inspected: medium and large rough you can read in the hand.",
                SizeWeights = new[] { 0f, 0.55f, 0.4f, 0.05f }, DirtScale = 0.15f,
            },
            new SupplierDefinition
            {
                Id = CuttingRough, Name = "Cutting Rough Lot", Tagline = "Solid nodules for the saw. Nothing to crack, everything to slice.",
                Description = "Banded agate, malachite and kidney ore sorted for the slab saw: solid rough that the hammer would only shatter, and that a polished face turns into money.",
                Price = 200f, MinRocks = 7, MaxRocks = 8, CountHidden = false,
                TierWeights = new[] { 0.3f, 0.36f, 0.22f, 0.09f, 0.015f, 0.002f },
                UnlockHint = "Unlocks once you own the Trim Saw.", Accent = new Color(0.35f, 0.62f, 0.45f),
                PreferredMinerals = new[] { MineralId.Agate, MineralId.Malachite, MineralId.Hematite, MineralId.Vanadinite, MineralId.Azurite, MineralId.Chalcopyrite }, PreferredShare = 0.8f,
                Character = "Solid, banded, heavy. Saw and polish material: slabs and slices, not cavities.",
                Risk = "Medium. A dull lot cuts plain grey; a good one is bullseye banding all the way through.",
                Minerals = "Agate nodules, malachite crusts, hematite kidney ore.",
                Clue = "Dense medium and large lumps, little clay, green or rust staining where the mineral shows.",
                SizeWeights = new[] { 0.05f, 0.5f, 0.4f, 0.05f }, DirtScale = 0.5f,
            },
            new SupplierDefinition
            {
                Id = DesertPocket, Name = "Desert Pocket Lot", Tagline = "Vug material from the dry country. Small, delicate, sometimes extraordinary.",
                Description = "Pocket rough from lead, pegmatite and garnet workings: wulfenite plates, tourmaline prisms, garnets in schist, selenite blades. Fragile, and worth being careful with.",
                Price = 240f, MinRocks = 6, MaxRocks = 7, CountHidden = false,
                TierWeights = new[] { 0.22f, 0.32f, 0.28f, 0.14f, 0.035f, 0.002f },
                UnlockHint = "Unlocks at collection prestige tier 2.", Accent = new Color(0.85f, 0.5f, 0.28f),
                PreferredMinerals = new[] { MineralId.Wulfenite, MineralId.Tourmaline, MineralId.Garnet, MineralId.Selenite, MineralId.Rhodochrosite, MineralId.Apophyllite, MineralId.Stilbite }, PreferredShare = 0.75f,
                Character = "Small pockets, unusual habits, high fragility: the loupe and the saw earn their keep here.",
                Risk = "Medium-high. Delicate crystals punish a heavy hand; a clean wulfenite pocket pays for the lot.",
                Minerals = "Wulfenite, tourmaline, garnet, selenite; some quartz and calcite.",
                Clue = "Tan and dark rough, small and light; orange or black flecks at the corners.",
                SizeWeights = new[] { 0.45f, 0.45f, 0.1f, 0f }, DirtScale = 0.55f,
            },
            new SupplierDefinition
            {
                Id = OversizedLot, Name = "Oversized Rough Lot", Tagline = "Three boulders. Bring a bigger cradle.",
                Description = "A pallet of oversized geodes straight off the loader. Too big for the basic cradle and the 10-inch saw; with heavy equipment, potentially the best pieces you will ever open.",
                Price = 300f, MinRocks = 3, MaxRocks = 4, CountHidden = false,
                TierWeights = new[] { 0.22f, 0.32f, 0.28f, 0.14f, 0.035f, 0.003f },
                UnlockHint = "Unlocks with the Stage 2 workshop.", Accent = new Color(0.6f, 0.55f, 0.5f),
                Character = "Few pieces, huge mass, big cavities. Everything about them takes longer.",
                Risk = "High effort, high ceiling. Brute-forcing one on the small cradle usually wrecks it.",
                Minerals = "The quarry's own mix, at boulder size: quartz family, agate, calcite, celestite.",
                Clue = "Head-sized rough, caked, seams you can read from across the room.",
                SizeWeights = new[] { 0f, 0f, 0.25f, 0.75f }, DirtScale = 1.1f,
            },
            new SupplierDefinition
            {
                Id = Network, Name = "Collector Network Trade", Tagline = "Other collectors' rough, swapped through the club. Odd, and often good.",
                Description = "Pieces traded between collectors who know what they are letting go of. Unusual families, a decent floor, and now and then a piece somebody should never have parted with.",
                Price = 340f, MinRocks = 5, MaxRocks = 7, CountHidden = false,
                TierWeights = new[] { 0.18f, 0.32f, 0.32f, 0.13f, 0.04f, 0.004f },
                UnlockHint = "Unlocks once the showroom has served 25 customers and something has sold for $200 or more.", Accent = new Color(0.5f, 0.6f, 0.8f),
                PreferredMinerals = new[] { MineralId.Fluorite, MineralId.Celestite, MineralId.Wulfenite, MineralId.Rhodochrosite, MineralId.Apophyllite, MineralId.Azurite, MineralId.Stibnite, MineralId.Chalcopyrite }, PreferredShare = 0.6f,
                Character = "Traded rough from people who know: unusual families and a good floor, priced accordingly.",
                Risk = "Medium. You pay for provenance; the upside is the piece somebody else misjudged.",
                Minerals = "The unusual end: fluorite, celestite, wulfenite, rhodochrosite, apophyllite, azurite.",
                Clue = "Mixed sizes, brushed clean, old collection numbers inked on the shells.",
                SizeWeights = new[] { 0.2f, 0.5f, 0.25f, 0.05f }, DirtScale = 0.3f,
                Localities = new[] { "Rockhound Club swap, Tucson", "a trade from Denver", "a Munich show lot", "the county club auction" },
            },
            new SupplierDefinition
            {
                Id = Showcase, Name = "Locality Showcase: Las Choyas", Tagline = "One famous locality, its own look, offered now and then.",
                Description = "Coconut geodes from the Las Choyas beds: thick grey shells, tight round form, quartz and calcite interiors with the odd surprise. When a pallet comes through, it comes through once.",
                Price = 215f, MinRocks = 7, MaxRocks = 8, CountHidden = false, Occasional = true,
                TierWeights = new[] { 0.28f, 0.34f, 0.25f, 0.1f, 0.018f, 0.002f },
                UnlockHint = "Offered now and then once the collection reaches prestige tier 2.", Accent = new Color(0.72f, 0.58f, 0.4f),
                PreferredMinerals = new[] { MineralId.ClearQuartz, MineralId.Calcite, MineralId.Amethyst, MineralId.Celestite }, PreferredShare = 0.85f,
                PreferredCavities = new[] { CavityArchetype.Hollow, CavityArchetype.ThickWall, CavityArchetype.Cathedral }, CavityShare = 0.85f, ShellBias = 1.25f,
                Character = "One locality's signature: thick shells, round form, hollow interiors. Learn its tells and you can read the crate.",
                Risk = "Medium. Consistent shells, variable interiors; the cathedral ones are the prize.",
                Minerals = "Quartz and calcite, some amethyst and celestite.",
                Clue = "Grey, round, heavy for their size, thick-shelled: they thud, and the seam is faint until washed.",
                SizeWeights = new[] { 0.15f, 0.6f, 0.25f, 0f }, DirtScale = 0.7f,
                Localities = new[] { "Las Choyas, Chihuahua" },
            },
            new SupplierDefinition
            {
                Id = Damaged, Name = "Damaged Rough Lot", Tagline = "Chipped in transit, sold cheap. What is under the chips is anyone's guess.",
                Description = "A pallet that had a bad journey: shells chipped, a few crystals already broken where the rocks banged together. The dealer wants it gone. The rough underneath is the regular grade, sometimes better.",
                Price = 165f, MinRocks = 8, MaxRocks = 9, CountHidden = false, Occasional = true, PreDamage = 0.35f,
                TierWeights = new[] { 0.45f, 0.3f, 0.16f, 0.065f, 0.02f, 0.0015f },   // a discount on regional odds, paid for in chips and pre-cracked seams
                UnlockHint = "Offered now and then after the third crate.", Accent = new Color(0.6f, 0.5f, 0.45f),
                Character = "Regular rough at a third of the price, already knocked about. Damage on the way in, not your fault.",
                Risk = "Medium-low. The damage caps the value of the best pieces; the price makes up for it on the rest.",
                Minerals = "The quarry's own mix.",
                Clue = "Chipped shoulders, bruised rims, a few open cracks you can start from.",
                SizeWeights = new[] { 0.3f, 0.5f, 0.2f, 0f }, DirtScale = 0.9f,
                Localities = new[] { "a pallet dropped at the depot", "the freight claim lot" },
            },
            new SupplierDefinition
            {
                Id = Specialty, Name = "Specialty Mineral Source", Tagline = "The families that do not turn up anywhere else. Priced like it.",
                Description = "A specialist who sources by family, not by weight: stibnite sprays, rhodochrosite, apophyllite, vanadinite. Expensive, thin-shelled, fragile, and where the deep tail of the rarity curve lives.",
                Price = 400f, MinRocks = 5, MaxRocks = 6, CountHidden = false,
                TierWeights = new[] { 0.12f, 0.27f, 0.4f, 0.15f, 0.048f, 0.005f },   // rare families, but exceptional stays a find, not a staple
                UnlockHint = "Unlocks at prestige tier 4 with the Stage 2 workshop.", Accent = new Color(0.8f, 0.45f, 0.6f),
                PreferredMinerals = new[] { MineralId.Stibnite, MineralId.Rhodochrosite, MineralId.Apophyllite, MineralId.Vanadinite, MineralId.Stilbite, MineralId.Wulfenite, MineralId.Halite }, PreferredShare = 0.9f,
                PreferredCavities = new[] { CavityArchetype.Pocket, CavityArchetype.Hollow, CavityArchetype.DoubleChamber }, CavityShare = 0.6f, ShellBias = 0.8f,
                Character = "Rare families, thin shells, the deepest tail in the game. Every rock wants the loupe first.",
                Risk = "High cost, high ceiling, fragile: a heavy hand wastes a $600 crate.",
                Minerals = "Stibnite, rhodochrosite, apophyllite, vanadinite, stilbite, wulfenite, halite.",
                Clue = "Small, light, thin-shelled; metallic or pink flecks at the seam; never wash the halite.",
                SizeWeights = new[] { 0.45f, 0.45f, 0.1f, 0f }, DirtScale = 0.4f,
                Localities = new[] { "Xikuangshan", "Sweet Home Mine", "Poona basalts", "Mibladen", "Searles Lake" },
            },
        };

        public static SupplierDefinition Get(string id)
        {
            foreach (var s in All) if (s.Id == id) return s;
            return null;
        }

        /// <summary>Evaluate unlock rules against the career state; returns newly unlocked supplier ids.</summary>
        public static List<string> EvaluateUnlocks(GameState state)
        {
            var newly = new List<string>();
            if (!state.HasSupplier(Local)) { state.UnlockedSuppliers.Add(Local); }
            if (!state.HasSupplier(Regional) && state.Stats.SpecimensSold > 0) { state.UnlockedSuppliers.Add(Regional); newly.Add(Regional); }
            if (!state.HasSupplier(AmethystLot) && HasOpened(state, MineralId.Amethyst)) { state.UnlockedSuppliers.Add(AmethystLot); newly.Add(AmethystLot); }
            // the gamble is the third strategy: it lands mid-slice, after the player has bought twice and kept something
            if (!state.HasSupplier(Estate) && state.Stats.CratesPurchased >= 2 && state.DisplayedCount() > 0) { state.UnlockedSuppliers.Add(Estate); newly.Add(Estate); }
            if (!state.HasSupplier(Premium) && state.CollectionValue() >= 1500f) { state.UnlockedSuppliers.Add(Premium); newly.Add(Premium); }
            if (!state.HasSupplier(CuttingRough) && state.HasUpgrade(UpgradeCatalog.TrimSaw)) { state.UnlockedSuppliers.Add(CuttingRough); newly.Add(CuttingRough); }
            if (!state.HasSupplier(DesertPocket) && state.Prestige >= 2) { state.UnlockedSuppliers.Add(DesertPocket); newly.Add(DesertPocket); }
            if (!state.HasSupplier(OversizedLot) && state.WorkshopStage >= 2) { state.UnlockedSuppliers.Add(OversizedLot); newly.Add(OversizedLot); }
            if (!state.HasSupplier(Network) && state.Stats.CustomersServed >= 25 && state.Stats.BiggestSale >= 200f) { state.UnlockedSuppliers.Add(Network); newly.Add(Network); }
            if (!state.HasSupplier(Specialty) && state.Prestige >= 4 && state.WorkshopStage >= 2) { state.UnlockedSuppliers.Add(Specialty); newly.Add(Specialty); }
            // occasional lots: unlocked (eligible) by milestone, offered by SpecialLots
            if (!state.HasSupplier(Showcase) && state.Prestige >= 2) { state.UnlockedSuppliers.Add(Showcase); newly.Add(Showcase); }
            if (!state.HasSupplier(Damaged) && state.Stats.CratesPurchased >= 3) { state.UnlockedSuppliers.Add(Damaged); newly.Add(Damaged); }
            return newly;
        }

        private static bool HasOpened(GameState state, MineralId mineral)
        {
            foreach (var s in state.Specimens) if (s.IsOpened && s.Geology.Mineral == mineral) return true;
            return false;
        }
    }
}
