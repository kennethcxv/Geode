using System.Collections.Generic;
using UnityEngine;

namespace GeodeEmpire.Specimens
{
    public enum MineralId
    {
        ClearQuartz = 0, Amethyst = 1, Citrine = 2, SmokyQuartz = 3, Agate = 4,
        Calcite = 5, Celestite = 6, Fluorite = 7, Pyrite = 8, Aragonite = 9,
        // V4 families: each a different habit and material response, not a recolour
        Malachite = 10, Selenite = 11, Wulfenite = 12, Garnet = 13, Hematite = 14, Tourmaline = 15,
    }

    /// <summary>Indices match the FBX archetypes exported by Tools/Blender/gen_crystals.py.</summary>
    public enum CrystalArchetype
    {
        QuartzPoint = 0, QuartzStubby = 1, QuartzCluster = 2, Cube = 3, Octahedron = 4, Rhomb = 5,
        Dogtooth = 6, Nailhead = 7, Blade = 8, Needle = 9, Pyritohedron = 10, DruzyTile = 11,
        Botryoidal = 12, AragoniteSpray = 13,
        TabularPlate = 14, Dodecahedron = 15, TrigonalPrism = 16, Fishtail = 17,
    }

    public enum PlacementStyle { Carpet, Clustered, Scattered, Embedded, Sprays, Banded }

    /// <summary>A named colour palette a family can roll between (fluorite/agate vary a lot).</summary>
    public sealed class MineralPalette
    {
        public string Name;
        public Color SurfaceA, SurfaceB, DeepA, DeepB, Zone;
        public Color BandA, BandB;

        public MineralPalette(string name, Color surfaceA, Color surfaceB, Color deepA, Color deepB, Color zone,
            Color? bandA = null, Color? bandB = null)
        {
            Name = name;
            SurfaceA = surfaceA; SurfaceB = surfaceB; DeepA = deepA; DeepB = deepB; Zone = zone;
            BandA = bandA ?? new Color(0.82f, 0.8f, 0.78f);
            BandB = bandB ?? new Color(0.5f, 0.48f, 0.46f);
        }
    }

    /// <summary>Static, data-driven description of one mineral/formation family.</summary>
    public sealed class MineralFamily
    {
        public MineralId Id;
        public string Name;
        public string Description;
        public CrystalArchetype[] Archetypes;
        public float[] ArchetypeWeights;
        public PlacementStyle Placement;
        public float ScaleMin, ScaleMax;          // crystal height / cavity radius across crystalScale 0..1
        public float DensityMin, DensityMax;      // fill probability per surface cell
        public float TiltDeg;
        public float ElongationMin, ElongationMax;
        public MineralPalette[] Palettes;
        public float[] PaletteWeights;
        public float Translucency, Metallic, Smoothness, Sparkle, Rim, ZoningBase, Inclusions;
        public Color CavityWall;
        public float BandStrength, BandFrequency;
        public float ValueMult;
        public float Fragility;
        public float ShellToughness;
        public MineralId[] SecondaryOptions;
        public float SecondaryChance;
        public int BaseFrequency;
        public bool DruzyCapable;
        public float CenterpieceChance;
        /// <summary>Cavity archetype weights (Hollow, ThickWall, Cathedral, Pocket, DoubleChamber, Nodule); null = the default mix.</summary>
        public float[] CavityWeights;
        /// <summary>Preferred matrix tone index (-1 = any): dark host rock for iron and copper minerals, tan for desert vugs.</summary>
        public int MatrixToneBias = -1;
        /// <summary>Extra iron staining on the outside (0..1): a real exterior clue for the iron-rich families.</summary>
        public float StainBias;
        /// <summary>How the family's own colour shows through the shell as an exterior hint (multiplier).</summary>
        public float HintBias = 1f;
        /// <summary>Shown in the encyclopedia and on source cards: where it comes from and what to look for.</summary>
        public string FieldNote;
    }

    public static class MineralCatalog
    {
        private static MineralFamily[] _all;
        private static Dictionary<MineralId, MineralFamily> _byId;

        public static IReadOnlyList<MineralFamily> All
        {
            get { EnsureBuilt(); return _all; }
        }

        public static MineralFamily Get(MineralId id)
        {
            EnsureBuilt();
            return _byId[id];
        }

        private static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

        private static void EnsureBuilt()
        {
            if (_all != null) return;
            var list = new List<MineralFamily>
            {
                new MineralFamily
                {
                    Id = MineralId.ClearQuartz, Name = "Clear Quartz",
                    Description = "Transparent prismatic points. The baseline geode: honest, glassy, and occasionally water-clear.",
                    Archetypes = new[] { CrystalArchetype.QuartzPoint, CrystalArchetype.QuartzStubby, CrystalArchetype.QuartzCluster },
                    ArchetypeWeights = new[] { 0.6f, 0.25f, 0.15f },
                    Placement = PlacementStyle.Carpet, ScaleMin = 0.24f, ScaleMax = 0.75f, DensityMin = 0.6f, DensityMax = 1.0f,
                    TiltDeg = 24f, ElongationMin = 0.85f, ElongationMax = 1.4f,
                    Palettes = new[]
                    {
                        new MineralPalette("Water Clear", C(0.94f, 0.96f, 0.99f), C(0.88f, 0.92f, 0.97f), C(0.72f, 0.78f, 0.88f), C(0.6f, 0.68f, 0.82f), C(1f, 1f, 1f)),
                        new MineralPalette("Milky", C(0.96f, 0.95f, 0.93f), C(0.9f, 0.88f, 0.85f), C(0.82f, 0.8f, 0.76f), C(0.7f, 0.68f, 0.64f), C(0.98f, 0.97f, 0.95f)),
                    },
                    PaletteWeights = new[] { 0.65f, 0.35f },
                    Translucency = 0.85f, Metallic = 0f, Smoothness = 0.95f, Sparkle = 0.7f, Rim = 0.7f, ZoningBase = 0.15f, Inclusions = 0.25f,
                    CavityWall = C(0.62f, 0.6f, 0.56f), BandStrength = 0.22f, BandFrequency = 10f,
                    ValueMult = 1.0f, Fragility = 0.5f, ShellToughness = 1.0f,
                    SecondaryOptions = new[] { MineralId.Pyrite, MineralId.Calcite }, SecondaryChance = 0.16f,
                    BaseFrequency = 16, DruzyCapable = true, CenterpieceChance = 0.5f,
                },
                new MineralFamily
                {
                    Id = MineralId.Amethyst, Name = "Amethyst",
                    Description = "Purple quartz. Saturation and tip zoning vary from pale lilac to deep grape.",
                    Archetypes = new[] { CrystalArchetype.QuartzPoint, CrystalArchetype.QuartzStubby, CrystalArchetype.QuartzCluster },
                    ArchetypeWeights = new[] { 0.55f, 0.3f, 0.15f },
                    Placement = PlacementStyle.Carpet, ScaleMin = 0.22f, ScaleMax = 0.7f, DensityMin = 0.65f, DensityMax = 1.0f,
                    TiltDeg = 20f, ElongationMin = 0.8f, ElongationMax = 1.3f,
                    Palettes = new[]
                    {
                        new MineralPalette("Grape", C(0.6f, 0.4f, 0.86f), C(0.45f, 0.24f, 0.72f), C(0.32f, 0.1f, 0.58f), C(0.18f, 0.04f, 0.38f), C(0.26f, 0.06f, 0.48f),
                            C(0.78f, 0.76f, 0.72f), C(0.5f, 0.5f, 0.5f)),
                        new MineralPalette("Lilac", C(0.78f, 0.66f, 0.92f), C(0.68f, 0.55f, 0.88f), C(0.55f, 0.38f, 0.78f), C(0.42f, 0.26f, 0.66f), C(0.5f, 0.3f, 0.75f),
                            C(0.8f, 0.78f, 0.75f), C(0.55f, 0.55f, 0.55f)),
                    },
                    PaletteWeights = new[] { 0.6f, 0.4f },
                    Translucency = 0.62f, Metallic = 0f, Smoothness = 0.94f, Sparkle = 0.6f, Rim = 0.6f, ZoningBase = 0.45f, Inclusions = 0.2f,
                    CavityWall = C(0.56f, 0.54f, 0.51f), BandStrength = 0.28f, BandFrequency = 12f,
                    ValueMult = 1.5f, Fragility = 0.5f, ShellToughness = 1.05f,
                    SecondaryOptions = new[] { MineralId.Calcite, MineralId.Pyrite }, SecondaryChance = 0.2f,
                    BaseFrequency = 16, DruzyCapable = true, CenterpieceChance = 0.6f,
                },
                new MineralFamily
                {
                    Id = MineralId.Citrine, Name = "Citrine",
                    Description = "Warm yellow to orange quartz. Rarer than amethyst; strong colour is prized.",
                    Archetypes = new[] { CrystalArchetype.QuartzPoint, CrystalArchetype.QuartzStubby, CrystalArchetype.QuartzCluster },
                    ArchetypeWeights = new[] { 0.5f, 0.35f, 0.15f },
                    Placement = PlacementStyle.Carpet, ScaleMin = 0.24f, ScaleMax = 0.7f, DensityMin = 0.6f, DensityMax = 1.0f,
                    TiltDeg = 22f, ElongationMin = 0.8f, ElongationMax = 1.3f,
                    Palettes = new[]
                    {
                        new MineralPalette("Honey", C(0.96f, 0.78f, 0.38f), C(0.92f, 0.66f, 0.24f), C(0.72f, 0.42f, 0.08f), C(0.55f, 0.28f, 0.04f), C(0.85f, 0.5f, 0.1f)),
                        new MineralPalette("Pale Lemon", C(0.98f, 0.92f, 0.62f), C(0.95f, 0.85f, 0.5f), C(0.85f, 0.7f, 0.3f), C(0.7f, 0.55f, 0.2f), C(0.9f, 0.75f, 0.35f)),
                    },
                    PaletteWeights = new[] { 0.55f, 0.45f },
                    Translucency = 0.7f, Metallic = 0f, Smoothness = 0.94f, Sparkle = 0.65f, Rim = 0.6f, ZoningBase = 0.3f, Inclusions = 0.2f,
                    CavityWall = C(0.58f, 0.54f, 0.48f), BandStrength = 0.25f, BandFrequency = 10f,
                    ValueMult = 1.6f, Fragility = 0.5f, ShellToughness = 1.0f,
                    SecondaryOptions = new[] { MineralId.Calcite }, SecondaryChance = 0.12f,
                    BaseFrequency = 8, DruzyCapable = true, CenterpieceChance = 0.5f,
                },
                new MineralFamily
                {
                    Id = MineralId.SmokyQuartz, Name = "Smoky Quartz",
                    Description = "Dark translucent quartz, brown to near-black. Large points read beautifully against pale matrix.",
                    Archetypes = new[] { CrystalArchetype.QuartzPoint, CrystalArchetype.QuartzStubby, CrystalArchetype.QuartzCluster },
                    ArchetypeWeights = new[] { 0.65f, 0.2f, 0.15f },
                    Placement = PlacementStyle.Carpet, ScaleMin = 0.28f, ScaleMax = 0.82f, DensityMin = 0.35f, DensityMax = 0.85f,
                    TiltDeg = 26f, ElongationMin = 1.0f, ElongationMax = 1.6f,
                    Palettes = new[]
                    {
                        new MineralPalette("Smoke", C(0.5f, 0.44f, 0.38f), C(0.36f, 0.3f, 0.26f), C(0.16f, 0.12f, 0.1f), C(0.08f, 0.06f, 0.05f), C(0.12f, 0.09f, 0.07f)),
                        new MineralPalette("Morion", C(0.3f, 0.27f, 0.25f), C(0.2f, 0.18f, 0.17f), C(0.07f, 0.06f, 0.06f), C(0.03f, 0.03f, 0.03f), C(0.05f, 0.04f, 0.04f)),
                    },
                    PaletteWeights = new[] { 0.7f, 0.3f },
                    Translucency = 0.5f, Metallic = 0f, Smoothness = 0.95f, Sparkle = 0.55f, Rim = 0.8f, ZoningBase = 0.3f, Inclusions = 0.15f,
                    CavityWall = C(0.7f, 0.67f, 0.62f), BandStrength = 0.2f, BandFrequency = 9f,
                    ValueMult = 1.2f, Fragility = 0.45f, ShellToughness = 1.0f,
                    SecondaryOptions = new[] { MineralId.ClearQuartz, MineralId.Calcite }, SecondaryChance = 0.15f,
                    BaseFrequency = 8, DruzyCapable = false, CenterpieceChance = 0.55f,
                },
                new MineralFamily
                {
                    Id = MineralId.Agate, Name = "Agate",
                    Description = "Banded chalcedony. The value is in the walls: concentric bands, botryoidal bubbles and a small druzy heart.",
                    Archetypes = new[] { CrystalArchetype.DruzyTile, CrystalArchetype.Botryoidal, CrystalArchetype.QuartzPoint },
                    ArchetypeWeights = new[] { 0.55f, 0.35f, 0.1f },
                    Placement = PlacementStyle.Banded, ScaleMin = 0.08f, ScaleMax = 0.3f, DensityMin = 0.5f, DensityMax = 0.95f,
                    TiltDeg = 10f, ElongationMin = 0.9f, ElongationMax = 1.1f,
                    Palettes = new[]
                    {
                        new MineralPalette("Cream & Brown", C(0.93f, 0.92f, 0.9f), C(0.85f, 0.83f, 0.8f), C(0.75f, 0.72f, 0.7f), C(0.62f, 0.6f, 0.58f), C(0.95f, 0.94f, 0.92f),
                            C(0.9f, 0.82f, 0.7f), C(0.5f, 0.36f, 0.26f)),
                        new MineralPalette("Blue Lace", C(0.9f, 0.93f, 0.97f), C(0.82f, 0.86f, 0.92f), C(0.7f, 0.76f, 0.85f), C(0.55f, 0.62f, 0.75f), C(0.95f, 0.96f, 0.98f),
                            C(0.86f, 0.9f, 0.95f), C(0.4f, 0.5f, 0.62f)),
                        new MineralPalette("Carnelian", C(0.95f, 0.85f, 0.75f), C(0.9f, 0.75f, 0.62f), C(0.8f, 0.55f, 0.4f), C(0.7f, 0.4f, 0.25f), C(0.96f, 0.9f, 0.82f),
                            C(0.92f, 0.62f, 0.38f), C(0.55f, 0.22f, 0.12f)),
                        new MineralPalette("Pink & Grey", C(0.95f, 0.9f, 0.9f), C(0.88f, 0.82f, 0.82f), C(0.78f, 0.7f, 0.7f), C(0.65f, 0.58f, 0.58f), C(0.96f, 0.93f, 0.93f),
                            C(0.9f, 0.78f, 0.8f), C(0.45f, 0.42f, 0.43f)),
                    },
                    PaletteWeights = new[] { 0.4f, 0.2f, 0.25f, 0.15f },
                    Translucency = 0.35f, Metallic = 0f, Smoothness = 0.85f, Sparkle = 0.5f, Rim = 0.4f, ZoningBase = 0.1f, Inclusions = 0.3f,
                    CavityWall = C(0.86f, 0.82f, 0.76f), BandStrength = 1.0f, BandFrequency = 16f,
                    ValueMult = 1.1f, Fragility = 0.3f, ShellToughness = 1.3f,
                    SecondaryOptions = new[] { MineralId.ClearQuartz, MineralId.Amethyst }, SecondaryChance = 0.22f,
                    BaseFrequency = 14, DruzyCapable = true, CenterpieceChance = 0.1f,
                },
                new MineralFamily
                {
                    Id = MineralId.Calcite, Name = "Calcite",
                    Description = "Rhombs, dogtooth points and nailheads. Softer, waxier and cheaper than quartz, but a great many habits.",
                    Archetypes = new[] { CrystalArchetype.Rhomb, CrystalArchetype.Dogtooth, CrystalArchetype.Nailhead },
                    ArchetypeWeights = new[] { 0.4f, 0.35f, 0.25f },
                    Placement = PlacementStyle.Scattered, ScaleMin = 0.28f, ScaleMax = 0.9f, DensityMin = 0.15f, DensityMax = 0.5f,
                    TiltDeg = 30f, ElongationMin = 0.8f, ElongationMax = 1.25f,
                    Palettes = new[]
                    {
                        new MineralPalette("Honey", C(0.97f, 0.9f, 0.72f), C(0.95f, 0.82f, 0.55f), C(0.85f, 0.65f, 0.35f), C(0.72f, 0.5f, 0.22f), C(0.9f, 0.72f, 0.4f)),
                        new MineralPalette("Iceland", C(0.96f, 0.96f, 0.94f), C(0.9f, 0.9f, 0.88f), C(0.8f, 0.8f, 0.78f), C(0.68f, 0.68f, 0.66f), C(0.95f, 0.95f, 0.93f)),
                        new MineralPalette("Peach", C(0.98f, 0.86f, 0.78f), C(0.95f, 0.76f, 0.66f), C(0.88f, 0.6f, 0.48f), C(0.76f, 0.45f, 0.35f), C(0.92f, 0.68f, 0.55f)),
                    },
                    PaletteWeights = new[] { 0.45f, 0.35f, 0.2f },
                    Translucency = 0.55f, Metallic = 0f, Smoothness = 0.8f, Sparkle = 0.3f, Rim = 0.5f, ZoningBase = 0.2f, Inclusions = 0.4f,
                    CavityWall = C(0.5f, 0.43f, 0.36f), BandStrength = 0.25f, BandFrequency = 8f,
                    ValueMult = 0.8f, Fragility = 0.7f, ShellToughness = 0.85f,
                    SecondaryOptions = new[] { MineralId.Pyrite, MineralId.ClearQuartz }, SecondaryChance = 0.15f,
                    BaseFrequency = 10, DruzyCapable = false, CenterpieceChance = 0.45f,
                },
                new MineralFamily
                {
                    Id = MineralId.Celestite, Name = "Celestite",
                    Description = "Pale sky-blue blades in clusters on tan matrix. Delicate: it chips if you get greedy with the hammer.",
                    Archetypes = new[] { CrystalArchetype.Blade, CrystalArchetype.QuartzPoint },
                    ArchetypeWeights = new[] { 0.75f, 0.25f },
                    Placement = PlacementStyle.Clustered, ScaleMin = 0.3f, ScaleMax = 0.88f, DensityMin = 0.35f, DensityMax = 0.85f,
                    TiltDeg = 34f, ElongationMin = 1.0f, ElongationMax = 1.7f,
                    Palettes = new[]
                    {
                        new MineralPalette("Sky", C(0.72f, 0.84f, 0.97f), C(0.58f, 0.72f, 0.92f), C(0.36f, 0.52f, 0.82f), C(0.25f, 0.4f, 0.7f), C(0.45f, 0.6f, 0.9f)),
                        new MineralPalette("Ice Blue", C(0.86f, 0.92f, 0.98f), C(0.78f, 0.86f, 0.95f), C(0.6f, 0.72f, 0.88f), C(0.48f, 0.6f, 0.8f), C(0.7f, 0.8f, 0.95f)),
                    },
                    PaletteWeights = new[] { 0.6f, 0.4f },
                    Translucency = 0.75f, Metallic = 0f, Smoothness = 0.92f, Sparkle = 0.55f, Rim = 0.7f, ZoningBase = 0.2f, Inclusions = 0.2f,
                    CavityWall = C(0.56f, 0.5f, 0.42f), BandStrength = 0.3f, BandFrequency = 8f,
                    ValueMult = 1.6f, Fragility = 0.8f, ShellToughness = 0.9f,
                    SecondaryOptions = new[] { MineralId.Calcite }, SecondaryChance = 0.2f,
                    BaseFrequency = 7, DruzyCapable = false, CenterpieceChance = 0.5f,
                },
                new MineralFamily
                {
                    Id = MineralId.Fluorite, Name = "Fluorite",
                    Description = "Cubes and octahedra in purple, green, blue or yellow. Stepped cubic geometry reads unlike any quartz.",
                    Archetypes = new[] { CrystalArchetype.Cube, CrystalArchetype.Octahedron },
                    ArchetypeWeights = new[] { 0.62f, 0.38f },
                    Placement = PlacementStyle.Clustered, ScaleMin = 0.28f, ScaleMax = 0.8f, DensityMin = 0.3f, DensityMax = 0.75f,
                    TiltDeg = 40f, ElongationMin = 0.9f, ElongationMax = 1.1f,
                    Palettes = new[]
                    {
                        new MineralPalette("Purple", C(0.62f, 0.38f, 0.82f), C(0.5f, 0.28f, 0.72f), C(0.35f, 0.12f, 0.55f), C(0.22f, 0.06f, 0.4f), C(0.28f, 0.1f, 0.5f)),
                        new MineralPalette("Green", C(0.45f, 0.8f, 0.58f), C(0.35f, 0.7f, 0.5f), C(0.15f, 0.5f, 0.3f), C(0.08f, 0.35f, 0.2f), C(0.12f, 0.42f, 0.25f)),
                        new MineralPalette("Blue", C(0.4f, 0.6f, 0.9f), C(0.3f, 0.5f, 0.85f), C(0.12f, 0.3f, 0.65f), C(0.06f, 0.2f, 0.5f), C(0.1f, 0.25f, 0.6f)),
                        new MineralPalette("Yellow", C(0.9f, 0.82f, 0.4f), C(0.85f, 0.75f, 0.3f), C(0.65f, 0.55f, 0.15f), C(0.5f, 0.4f, 0.08f), C(0.6f, 0.5f, 0.1f)),
                    },
                    PaletteWeights = new[] { 0.4f, 0.3f, 0.15f, 0.15f },
                    Translucency = 0.8f, Metallic = 0f, Smoothness = 0.93f, Sparkle = 0.5f, Rim = 0.6f, ZoningBase = 0.5f, Inclusions = 0.15f,
                    CavityWall = C(0.5f, 0.47f, 0.44f), BandStrength = 0.25f, BandFrequency = 8f,
                    ValueMult = 1.5f, Fragility = 0.6f, ShellToughness = 0.95f,
                    SecondaryOptions = new[] { MineralId.Pyrite, MineralId.ClearQuartz }, SecondaryChance = 0.25f,
                    BaseFrequency = 7, DruzyCapable = false, CenterpieceChance = 0.55f,
                },
                new MineralFamily
                {
                    Id = MineralId.Pyrite, Name = "Pyrite",
                    Description = "Brassy metallic cubes and pyritohedra embedded in dark matrix. Fool's gold, real shine.",
                    Archetypes = new[] { CrystalArchetype.Pyritohedron, CrystalArchetype.Cube },
                    ArchetypeWeights = new[] { 0.5f, 0.5f },
                    Placement = PlacementStyle.Embedded, ScaleMin = 0.22f, ScaleMax = 0.7f, DensityMin = 0.25f, DensityMax = 0.65f,
                    TiltDeg = 60f, ElongationMin = 0.95f, ElongationMax = 1.05f,
                    Palettes = new[]
                    {
                        new MineralPalette("Brass", C(0.88f, 0.74f, 0.42f), C(0.78f, 0.62f, 0.32f), C(0.7f, 0.55f, 0.28f), C(0.6f, 0.45f, 0.2f), C(0.9f, 0.78f, 0.5f)),
                        new MineralPalette("Dark Brass", C(0.72f, 0.6f, 0.36f), C(0.62f, 0.5f, 0.28f), C(0.5f, 0.4f, 0.22f), C(0.4f, 0.3f, 0.15f), C(0.75f, 0.64f, 0.4f)),
                    },
                    PaletteWeights = new[] { 0.6f, 0.4f },
                    Translucency = 0f, Metallic = 0.95f, Smoothness = 0.78f, Sparkle = 0.45f, Rim = 0.2f, ZoningBase = 0f, Inclusions = 0.1f,
                    CavityWall = C(0.3f, 0.28f, 0.26f), BandStrength = 0.2f, BandFrequency = 8f,
                    ValueMult = 1.3f, Fragility = 0.35f, ShellToughness = 1.1f,
                    SecondaryOptions = new[] { MineralId.ClearQuartz, MineralId.Calcite }, SecondaryChance = 0.28f,
                    BaseFrequency = 6, DruzyCapable = false, CenterpieceChance = 0.4f,
                },
                new MineralFamily
                {
                    Id = MineralId.Aragonite, Name = "Aragonite",
                    Description = "Radiating needle sprays. Fragile, airy and instantly recognisable: nothing else in the crate looks like it.",
                    Archetypes = new[] { CrystalArchetype.AragoniteSpray, CrystalArchetype.Needle },
                    ArchetypeWeights = new[] { 0.7f, 0.3f },
                    Placement = PlacementStyle.Sprays, ScaleMin = 0.35f, ScaleMax = 0.95f, DensityMin = 0.2f, DensityMax = 0.55f,
                    TiltDeg = 30f, ElongationMin = 0.9f, ElongationMax = 1.3f,
                    Palettes = new[]
                    {
                        new MineralPalette("Bone", C(0.96f, 0.94f, 0.88f), C(0.9f, 0.84f, 0.72f), C(0.82f, 0.72f, 0.56f), C(0.7f, 0.6f, 0.45f), C(0.95f, 0.92f, 0.85f)),
                        new MineralPalette("Amber", C(0.95f, 0.85f, 0.6f), C(0.9f, 0.75f, 0.45f), C(0.78f, 0.58f, 0.3f), C(0.62f, 0.45f, 0.2f), C(0.9f, 0.78f, 0.5f)),
                    },
                    PaletteWeights = new[] { 0.6f, 0.4f },
                    Translucency = 0.5f, Metallic = 0f, Smoothness = 0.85f, Sparkle = 0.4f, Rim = 0.5f, ZoningBase = 0.15f, Inclusions = 0.25f,
                    CavityWall = C(0.55f, 0.46f, 0.36f), BandStrength = 0.3f, BandFrequency = 8f,
                    ValueMult = 1.25f, Fragility = 0.9f, ShellToughness = 0.85f,
                    SecondaryOptions = new[] { MineralId.Calcite }, SecondaryChance = 0.1f,
                    BaseFrequency = 4, DruzyCapable = false, CenterpieceChance = 0.35f,
                },
                // ---- V4 families ----------------------------------------------------------------------
                new MineralFamily
                {
                    Id = MineralId.Malachite, Name = "Malachite",
                    Description = "Botryoidal green crusts with concentric banding. The cut and polished face is the prize: bullseye rings in two greens.",
                    FieldNote = "Copper country. Dark, heavy, green-stained rough; almost always solid, so it wants the saw.",
                    Archetypes = new[] { CrystalArchetype.Botryoidal, CrystalArchetype.DruzyTile },
                    ArchetypeWeights = new[] { 0.85f, 0.15f },
                    Placement = PlacementStyle.Banded, ScaleMin = 0.1f, ScaleMax = 0.32f, DensityMin = 0.55f, DensityMax = 0.95f,
                    TiltDeg = 8f, ElongationMin = 0.9f, ElongationMax = 1.1f,
                    Palettes = new[]
                    {
                        new MineralPalette("Deep Green", C(0.18f, 0.52f, 0.32f), C(0.1f, 0.4f, 0.24f), C(0.05f, 0.26f, 0.15f), C(0.03f, 0.18f, 0.1f), C(0.08f, 0.32f, 0.18f),
                            C(0.3f, 0.6f, 0.4f), C(0.05f, 0.22f, 0.12f)),
                        new MineralPalette("Bright Green", C(0.3f, 0.7f, 0.42f), C(0.2f, 0.58f, 0.34f), C(0.1f, 0.4f, 0.22f), C(0.06f, 0.28f, 0.15f), C(0.12f, 0.45f, 0.25f),
                            C(0.4f, 0.7f, 0.48f), C(0.08f, 0.28f, 0.15f)),
                    },
                    PaletteWeights = new[] { 0.6f, 0.4f },
                    Translucency = 0.12f, Metallic = 0f, Smoothness = 0.72f, Sparkle = 0.15f, Rim = 0.3f, ZoningBase = 0.05f, Inclusions = 0.15f,
                    CavityWall = C(0.2f, 0.42f, 0.28f), BandStrength = 1.0f, BandFrequency = 22f,
                    ValueMult = 1.7f, Fragility = 0.5f, ShellToughness = 0.8f,
                    SecondaryOptions = new[] { MineralId.Calcite, MineralId.ClearQuartz }, SecondaryChance = 0.1f,
                    BaseFrequency = 5, DruzyCapable = true, CenterpieceChance = 0.05f,
                    CavityWeights = new[] { 0.05f, 0.3f, 0.02f, 0.08f, 0.02f, 0.53f },
                    MatrixToneBias = 3, StainBias = 0.2f, HintBias = 1.6f,
                },
                new MineralFamily
                {
                    Id = MineralId.Selenite, Name = "Selenite",
                    Description = "Glassy gypsum blades and swallowtail twins. Water-clear to amber, satin-soft, and the most fragile thing on the bench.",
                    FieldNote = "Clay beds and gypsum caves. Light for its size, often a pale, powdery shell; a fingernail scratches it.",
                    Archetypes = new[] { CrystalArchetype.Fishtail, CrystalArchetype.Blade },
                    ArchetypeWeights = new[] { 0.55f, 0.45f },
                    Placement = PlacementStyle.Sprays, ScaleMin = 0.4f, ScaleMax = 1.0f, DensityMin = 0.2f, DensityMax = 0.6f,
                    TiltDeg = 28f, ElongationMin = 1.0f, ElongationMax = 1.6f,
                    Palettes = new[]
                    {
                        new MineralPalette("Water Clear", C(0.96f, 0.96f, 0.93f), C(0.9f, 0.9f, 0.86f), C(0.78f, 0.78f, 0.72f), C(0.66f, 0.66f, 0.6f), C(0.95f, 0.95f, 0.92f)),
                        new MineralPalette("Amber", C(0.95f, 0.82f, 0.55f), C(0.9f, 0.72f, 0.42f), C(0.78f, 0.55f, 0.25f), C(0.62f, 0.42f, 0.18f), C(0.88f, 0.7f, 0.4f)),
                    },
                    PaletteWeights = new[] { 0.65f, 0.35f },
                    Translucency = 0.9f, Metallic = 0f, Smoothness = 0.82f, Sparkle = 0.35f, Rim = 0.75f, ZoningBase = 0.1f, Inclusions = 0.3f,
                    CavityWall = C(0.72f, 0.66f, 0.56f), BandStrength = 0.2f, BandFrequency = 7f,
                    ValueMult = 1.2f, Fragility = 0.95f, ShellToughness = 0.7f,
                    SecondaryOptions = new[] { MineralId.Calcite }, SecondaryChance = 0.12f,
                    BaseFrequency = 5, DruzyCapable = false, CenterpieceChance = 0.45f,
                    CavityWeights = new[] { 0.45f, 0.2f, 0.15f, 0.12f, 0.08f, 0f },
                    MatrixToneBias = 2, StainBias = 0f, HintBias = 0.7f,
                },
                new MineralFamily
                {
                    Id = MineralId.Wulfenite, Name = "Wulfenite",
                    Description = "Square, paper-thin orange plates with an adamantine flash, scattered on tan matrix. Rare, and prized when the colour is red.",
                    FieldNote = "Desert lead vugs. Small tan rough with a pocket; look for an orange fleck on the outside.",
                    Archetypes = new[] { CrystalArchetype.TabularPlate },
                    ArchetypeWeights = new[] { 1f },
                    Placement = PlacementStyle.Scattered, ScaleMin = 0.22f, ScaleMax = 0.62f, DensityMin = 0.15f, DensityMax = 0.5f,
                    TiltDeg = 45f, ElongationMin = 0.9f, ElongationMax = 1.15f,
                    Palettes = new[]
                    {
                        new MineralPalette("Orange", C(0.98f, 0.58f, 0.16f), C(0.94f, 0.48f, 0.1f), C(0.78f, 0.34f, 0.05f), C(0.6f, 0.24f, 0.03f), C(0.9f, 0.4f, 0.06f)),
                        new MineralPalette("Red-Orange", C(0.92f, 0.36f, 0.1f), C(0.85f, 0.28f, 0.06f), C(0.65f, 0.16f, 0.03f), C(0.5f, 0.1f, 0.02f), C(0.8f, 0.2f, 0.04f)),
                        new MineralPalette("Butterscotch", C(0.96f, 0.74f, 0.34f), C(0.92f, 0.64f, 0.24f), C(0.75f, 0.48f, 0.12f), C(0.58f, 0.35f, 0.08f), C(0.86f, 0.55f, 0.15f)),
                    },
                    PaletteWeights = new[] { 0.5f, 0.2f, 0.3f },
                    Translucency = 0.55f, Metallic = 0f, Smoothness = 0.96f, Sparkle = 0.95f, Rim = 0.8f, ZoningBase = 0.15f, Inclusions = 0.1f,
                    CavityWall = C(0.62f, 0.52f, 0.38f), BandStrength = 0.15f, BandFrequency = 8f,
                    ValueMult = 2.2f, Fragility = 0.85f, ShellToughness = 0.9f,
                    SecondaryOptions = new[] { MineralId.Calcite }, SecondaryChance = 0.18f,
                    BaseFrequency = 3, DruzyCapable = false, CenterpieceChance = 0.3f,
                    CavityWeights = new[] { 0.2f, 0.35f, 0.03f, 0.37f, 0.05f, 0f },
                    MatrixToneBias = 2, StainBias = 0.1f, HintBias = 1.4f,
                },
                new MineralFamily
                {
                    Id = MineralId.Garnet, Name = "Garnet",
                    Description = "Dodecahedra of deep red set into dark host rock. Tough, glassy, and read as jewels at any distance.",
                    FieldNote = "Schist and skarn: dark, gritty, heavy rough. Broken corners sometimes show a red glint.",
                    Archetypes = new[] { CrystalArchetype.Dodecahedron },
                    ArchetypeWeights = new[] { 1f },
                    Placement = PlacementStyle.Embedded, ScaleMin = 0.22f, ScaleMax = 0.6f, DensityMin = 0.2f, DensityMax = 0.6f,
                    TiltDeg = 60f, ElongationMin = 0.95f, ElongationMax = 1.05f,
                    Palettes = new[]
                    {
                        new MineralPalette("Almandine", C(0.55f, 0.1f, 0.14f), C(0.42f, 0.06f, 0.1f), C(0.25f, 0.03f, 0.06f), C(0.15f, 0.02f, 0.04f), C(0.3f, 0.04f, 0.08f)),
                        new MineralPalette("Spessartine", C(0.85f, 0.36f, 0.12f), C(0.72f, 0.26f, 0.08f), C(0.5f, 0.14f, 0.04f), C(0.35f, 0.08f, 0.02f), C(0.6f, 0.18f, 0.05f)),
                    },
                    PaletteWeights = new[] { 0.7f, 0.3f },
                    Translucency = 0.3f, Metallic = 0f, Smoothness = 0.92f, Sparkle = 0.55f, Rim = 0.55f, ZoningBase = 0.1f, Inclusions = 0.2f,
                    CavityWall = C(0.26f, 0.24f, 0.23f), BandStrength = 0.1f, BandFrequency = 8f,
                    ValueMult = 1.4f, Fragility = 0.3f, ShellToughness = 1.2f,
                    SecondaryOptions = new[] { MineralId.ClearQuartz, MineralId.Pyrite }, SecondaryChance = 0.2f,
                    BaseFrequency = 5, DruzyCapable = false, CenterpieceChance = 0.4f,
                    CavityWeights = new[] { 0.15f, 0.4f, 0.03f, 0.35f, 0.07f, 0f },
                    MatrixToneBias = 3, StainBias = 0.15f, HintBias = 1.2f,
                },
                new MineralFamily
                {
                    Id = MineralId.Hematite, Name = "Hematite",
                    Description = "Kidney ore: botryoidal black-grey with a steel sheen, or bright specular plates. Heavy as lead and reads metallic, never glassy.",
                    FieldNote = "Iron country. Rust-stained, very heavy for its size; a red streak in the pits gives it away.",
                    Archetypes = new[] { CrystalArchetype.Botryoidal, CrystalArchetype.DruzyTile },
                    ArchetypeWeights = new[] { 0.8f, 0.2f },
                    Placement = PlacementStyle.Carpet, ScaleMin = 0.1f, ScaleMax = 0.3f, DensityMin = 0.6f, DensityMax = 1.0f,
                    TiltDeg = 8f, ElongationMin = 0.9f, ElongationMax = 1.1f,
                    Palettes = new[]
                    {
                        new MineralPalette("Kidney Ore", C(0.3f, 0.28f, 0.3f), C(0.22f, 0.2f, 0.22f), C(0.12f, 0.1f, 0.12f), C(0.06f, 0.05f, 0.06f), C(0.16f, 0.13f, 0.15f),
                            C(0.42f, 0.2f, 0.14f), C(0.18f, 0.09f, 0.07f)),
                        new MineralPalette("Specular", C(0.52f, 0.52f, 0.56f), C(0.4f, 0.4f, 0.45f), C(0.22f, 0.22f, 0.26f), C(0.12f, 0.12f, 0.15f), C(0.3f, 0.3f, 0.34f),
                            C(0.4f, 0.2f, 0.15f), C(0.16f, 0.09f, 0.08f)),
                    },
                    PaletteWeights = new[] { 0.65f, 0.35f },
                    Translucency = 0f, Metallic = 0.85f, Smoothness = 0.8f, Sparkle = 0.4f, Rim = 0.25f, ZoningBase = 0f, Inclusions = 0.05f,
                    CavityWall = C(0.28f, 0.2f, 0.18f), BandStrength = 0.35f, BandFrequency = 14f,
                    ValueMult = 1.3f, Fragility = 0.25f, ShellToughness = 1.35f,
                    SecondaryOptions = new[] { MineralId.ClearQuartz, MineralId.Calcite }, SecondaryChance = 0.22f,
                    BaseFrequency = 4, DruzyCapable = true, CenterpieceChance = 0.05f,
                    CavityWeights = new[] { 0.35f, 0.35f, 0.05f, 0.1f, 0.05f, 0.1f },
                    MatrixToneBias = 3, StainBias = 0.7f, HintBias = 0.8f,
                },
                new MineralFamily
                {
                    Id = MineralId.Tourmaline, Name = "Tourmaline",
                    Description = "Striated three-sided prisms: jet-black schorl mostly, now and then green or pink. Sits in pale pegmatite and looks like nothing else.",
                    FieldNote = "Pegmatite: pale, coarse, feldspar-flecked rough with a black needle showing at a corner if you are lucky.",
                    Archetypes = new[] { CrystalArchetype.TrigonalPrism },
                    ArchetypeWeights = new[] { 1f },
                    Placement = PlacementStyle.Scattered, ScaleMin = 0.35f, ScaleMax = 0.95f, DensityMin = 0.15f, DensityMax = 0.5f,
                    TiltDeg = 35f, ElongationMin = 1.0f, ElongationMax = 1.4f,
                    Palettes = new[]
                    {
                        new MineralPalette("Schorl", C(0.12f, 0.11f, 0.12f), C(0.08f, 0.08f, 0.09f), C(0.04f, 0.04f, 0.05f), C(0.02f, 0.02f, 0.03f), C(0.06f, 0.06f, 0.07f)),
                        new MineralPalette("Verdelite", C(0.2f, 0.55f, 0.35f), C(0.14f, 0.45f, 0.28f), C(0.06f, 0.3f, 0.16f), C(0.03f, 0.2f, 0.1f), C(0.08f, 0.35f, 0.18f)),
                        new MineralPalette("Rubellite", C(0.85f, 0.3f, 0.5f), C(0.75f, 0.2f, 0.4f), C(0.55f, 0.1f, 0.28f), C(0.4f, 0.05f, 0.2f), C(0.65f, 0.12f, 0.32f)),
                    },
                    PaletteWeights = new[] { 0.6f, 0.22f, 0.18f },
                    Translucency = 0.35f, Metallic = 0f, Smoothness = 0.9f, Sparkle = 0.4f, Rim = 0.6f, ZoningBase = 0.35f, Inclusions = 0.15f,
                    CavityWall = C(0.8f, 0.76f, 0.7f), BandStrength = 0.1f, BandFrequency = 8f,
                    ValueMult = 1.9f, Fragility = 0.6f, ShellToughness = 1.1f,
                    SecondaryOptions = new[] { MineralId.ClearQuartz }, SecondaryChance = 0.3f,
                    BaseFrequency = 4, DruzyCapable = false, CenterpieceChance = 0.5f,
                    CavityWeights = new[] { 0.3f, 0.3f, 0.05f, 0.3f, 0.05f, 0f },
                    MatrixToneBias = 2, StainBias = 0f, HintBias = 1.1f,
                },
            };
            _all = list.ToArray();
            _byId = new Dictionary<MineralId, MineralFamily>();
            foreach (var f in _all) _byId[f.Id] = f;
        }
    }
}
