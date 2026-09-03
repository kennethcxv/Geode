using System;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;

namespace GeodeEmpire.Specimens
{
    public enum CavityArchetype { Hollow = 0, ThickWall = 1, Cathedral = 2, Pocket = 3, DoubleChamber = 4, Nodule = 5 }
    public enum ExteriorArchetype { Rounded = 0, Lumpy = 1, Flattened = 2, Knobbly = 3, Angular = 4 }
    public enum QualityTier { Common = 0, Decent = 1, Good = 2, Exceptional = 3, MuseumGrade = 4, WorldClass = 5 }
    /// <summary>Physical size class: decides handling, crate packing, tool suitability and how long the shell takes to work.</summary>
    public enum SizeClass { Small = 0, Medium = 1, Large = 2, Oversized = 3 }
    /// <summary>How the outside of the rough reads: matrix grain, weathered rind, layered skin, or a dark vesicular volcanic crust.</summary>
    public enum ExteriorTexture { Coarse = 0, Weathered = 1, Banded = 2, Volcanic = 3 }

    public enum RareTrait
    {
        None = 0, GiantCenterpiece = 1, DeepCathedral = 2, DoubleCavity = 3, DenseDruzy = 4, ColorZoning = 5,
        HighClarity = 6, SecondaryContrast = 7, CrystalOnCrystal = 8, Phantom = 9, PerfectSymmetry = 10,
        MetallicContrast = 11, GiantCrystalField = 12,
    }

    /// <summary>
    /// Geological truth of one specimen. Generated ONCE from the seed and never mutated.
    /// Everything the player can see or that affects value is derived from these fields.
    /// </summary>
    [Serializable]
    public sealed class SpecimenGeology
    {
        public ulong Seed;
        public float QualityRoll;          // 0..1 underlying quality
        public QualityTier Tier;
        public MineralId Mineral;
        public bool HasSecondary;
        public MineralId Secondary;
        public float SecondaryAmount;      // fraction of crystals replaced/overgrown
        public int PaletteIndex;
        public CavityArchetype Cavity;
        public ExteriorArchetype Exterior;
        public SizeClass SizeClass;
        public ExteriorTexture Texture;
        public float Size;                 // mean radius in metres
        public Vector3 Axes;               // ellipsoid semi-axis multipliers (x,y,z), mean ~1
        public float ShellThickness;       // fraction of radius (min wall)
        public float CavityFraction;       // main lobe radius / size
        public Vector3[] LobeCenters;      // cavity lobes, specimen-local (metres)
        public float[] LobeRadii;          // metres
        public float CrystalScale;         // 0..1 (family maps to actual size)
        public float CrystalDensity;       // 0..1
        public float Saturation;           // 0..1
        public float Clarity;              // 0..1
        public float Symmetry;             // 0..1 (visual regularity of crystal sizes/orientation)
        public float HueShift;             // -1..1 within palette
        public float Zoning;               // 0..1
        public float ExteriorRoughness;    // lump amplitude
        public float Weathering;           // 0..1 exterior colour desaturation/dirt
        public float ExteriorHint;         // 0..1 exposed mineral hint on the outside
        public int ExteriorTone;           // palette index for matrix colour
        public float RimRoughness;
        public float MassKg;
        public float BandOffset;           // banding phase for agate walls
        /// <summary>Clay/dust coating on the rough (0 = arrives clean). Washed off at the tub; hides the shell's clues until then.</summary>
        public float Dirt;
        /// <summary>Iron staining streaks on the shell: an exterior character trait, stronger on weathered rough.</summary>
        public float Stain;
        /// <summary>How well defined the natural seam is (1 = a clean equatorial parting, 0.3 = barely a line): the hammer follows it.</summary>
        public float SeamQuality;
        /// <summary>Per-sector shell thickness multiplier around the seam (0.75..1.3): thin spots crack first, thick ones resist.</summary>
        public float[] SectorThickness = Array.Empty<float>();
        /// <summary>A natural chip on the shell exposing a glimpse of the interior (the best exterior clue there is), or none.</summary>
        public bool HasNaturalChip;
        public float ChipLongitude;        // fraction around the rock
        public float ChipLatitude;         // signed fraction, 0 at the seam
        public List<RareTrait> Traits = new List<RareTrait>();
        public float BaseValue;            // pristine appraised value ($)

        public bool HasTrait(RareTrait t) => Traits.Contains(t);
        public MineralFamily Family => MineralCatalog.Get(Mineral);
        public MineralFamily SecondaryFamily => HasSecondary ? MineralCatalog.Get(Secondary) : null;
        public MineralPalette Palette => Family.Palettes[Mathf.Clamp(PaletteIndex, 0, Family.Palettes.Length - 1)];

        /// <summary>True when the crystals are small enough that the family renders as a druzy carpet.</summary>
        public bool IsDruzy => Family.DruzyCapable && CrystalScale < 0.22f;

        public string SeedString => Seed.ToString("X16");

        public float SectorThicknessAt(int sector) => SectorThickness != null && sector >= 0 && sector < SectorThickness.Length ? SectorThickness[sector] : 1f;

        public static string SizeWord(SizeClass c) => c switch
        {
            SizeClass.Small => "small",
            SizeClass.Medium => "medium",
            SizeClass.Large => "large",
            _ => "oversized",
        };

        public static string TextureWord(ExteriorTexture t) => t switch
        {
            ExteriorTexture.Coarse => "coarse matrix",
            ExteriorTexture.Weathered => "weathered rind",
            ExteriorTexture.Banded => "layered skin",
            _ => "volcanic crust",
        };
    }

    /// <summary>Pure deterministic generator: same seed always yields identical geology.</summary>
    public static class SpecimenGenerator
    {
        public const int Version = 2;
        public const int SeamSectors = 16;

        // Tier bands over the quality roll (used both for generation and for supplier rejection sampling).
        public static readonly float[] TierUpper = { 0.35f, 0.58f, 0.78f, 0.90f, 0.97f, 1.0001f };
        public static readonly float[] NeutralTierWeights = { 0.58f, 0.24f, 0.12f, 0.045f, 0.012f, 0.003f };
        /// <summary>Neutral size mix; suppliers reshape it by rejection sampling.</summary>
        public static readonly float[] NeutralSizeWeights = { 0.3f, 0.48f, 0.17f, 0.05f };
        /// <summary>Mean-radius bands per size class (metres).</summary>
        public static readonly float[] SizeLower = { 0.034f, 0.05f, 0.08f, 0.115f };
        public static readonly float[] SizeUpper = { 0.05f, 0.08f, 0.115f, 0.165f };

        public static QualityTier TierFor(float q)
        {
            for (int i = 0; i < TierUpper.Length; i++) if (q < TierUpper[i]) return (QualityTier)i;
            return QualityTier.WorldClass;
        }

        public static float TierLower(QualityTier t) => t == QualityTier.Common ? 0f : TierUpper[(int)t - 1];

        public static SizeClass SizeClassFor(float radius)
        {
            for (int i = 0; i < SizeUpper.Length - 1; i++) if (radius < SizeUpper[i]) return (SizeClass)i;
            return SizeClass.Oversized;
        }

        public static SpecimenGeology Generate(ulong seed)
        {
            var rng = new SeededRandom(seed);
            var g = new SpecimenGeology { Seed = seed };

            // --- quality roll: neutral long-tail distribution ---------------------------------
            int tier = rng.PickWeighted(NeutralTierWeights);
            float lo = TierLower((QualityTier)tier), hi = TierUpper[tier];
            g.QualityRoll = Mathf.Clamp01(rng.Range(lo, Mathf.Min(hi, 1f)));
            g.Tier = TierFor(g.QualityRoll);
            float q = g.QualityRoll;

            // --- mineral ------------------------------------------------------------------------
            var families = MineralCatalog.All;
            var weights = new float[families.Count];
            for (int i = 0; i < families.Count; i++) weights[i] = families[i].BaseFrequency;
            var fam = families[rng.PickWeighted(weights)];
            g.Mineral = fam.Id;
            g.PaletteIndex = rng.PickWeighted(fam.PaletteWeights);
            g.HueShift = rng.Range(-1f, 1f);

            // --- size class and exterior --------------------------------------------------------
            g.SizeClass = (SizeClass)rng.PickWeighted(NeutralSizeWeights);
            int sc = (int)g.SizeClass;
            // within its band a better specimen tends to be a fuller one, never enough to cross into the next class
            float within = Mathf.Lerp(Mathf.Pow(rng.NextFloat(), 1.2f), 0.55f + 0.45f * rng.NextFloat(), q * 0.5f);
            g.Size = Mathf.Lerp(SizeLower[sc], SizeUpper[sc] - 0.001f, within);
            g.Exterior = (ExteriorArchetype)rng.PickWeighted(new[] { 0.3f, 0.25f, 0.15f, 0.18f, 0.12f });
            float ax = rng.Range(0.85f, 1.2f), ay = rng.Range(0.8f, 1.1f), az = rng.Range(0.85f, 1.2f);
            if (g.Exterior == ExteriorArchetype.Flattened) ay *= 0.68f;
            float meanAxis = (ax + ay + az) / 3f;
            g.Axes = new Vector3(ax / meanAxis, ay / meanAxis, az / meanAxis);
            g.ExteriorRoughness = g.Exterior switch
            {
                ExteriorArchetype.Rounded => rng.Range(0.045f, 0.08f),
                ExteriorArchetype.Lumpy => rng.Range(0.1f, 0.18f),
                ExteriorArchetype.Flattened => rng.Range(0.05f, 0.1f),
                ExteriorArchetype.Knobbly => rng.Range(0.07f, 0.12f),
                _ => rng.Range(0.09f, 0.15f),
            };
            g.Weathering = rng.Range(0.1f, 0.9f);
            g.ExteriorHint = rng.Chance(0.35f) ? rng.Range(0.3f, 1f) : rng.Range(0f, 0.15f);
            g.ExteriorTone = rng.Range(0, 4);
            g.RimRoughness = rng.Range(0.02f, 0.06f);
            g.BandOffset = rng.NextFloat();
            // exterior texture family: the family's habit and the weathering decide it
            float[] texW = fam.Id == MineralId.Agate ? new[] { 0.25f, 0.3f, 0.4f, 0.05f }
                : g.Weathering > 0.6f ? new[] { 0.25f, 0.5f, 0.1f, 0.15f }
                : new[] { 0.45f, 0.2f, 0.1f, 0.25f };
            g.Texture = (ExteriorTexture)rng.PickWeighted(texW);
            g.Dirt = Mathf.Clamp01(rng.Range(0.05f, 0.95f) * Mathf.Lerp(0.6f, 1.1f, g.Weathering));
            g.Stain = rng.Chance(0.4f + 0.3f * g.Weathering) ? rng.Range(0.25f, 1f) : rng.Range(0f, 0.12f);
            g.SeamQuality = Mathf.Clamp01(rng.Range(0.25f, 1f) * 0.7f + (g.Exterior == ExteriorArchetype.Angular ? 0.05f : 0.3f) * rng.NextFloat());
            g.SectorThickness = new float[SeamSectors];
            {
                // a smooth ring of thickness variation: two harmonics so thin and thick spots come in patches, not noise
                float p1 = rng.Range(0f, Mathf.PI * 2f), p2 = rng.Range(0f, Mathf.PI * 2f);
                float a1 = rng.Range(0.05f, 0.2f), a2 = rng.Range(0.02f, 0.12f);
                for (int i = 0; i < SeamSectors; i++)
                {
                    float a = i / (float)SeamSectors * Mathf.PI * 2f;
                    g.SectorThickness[i] = Mathf.Clamp(1f + a1 * Mathf.Sin(a + p1) + a2 * Mathf.Sin(2f * a + p2) + rng.Range(-0.04f, 0.04f), 0.75f, 1.3f);
                }
            }
            g.HasNaturalChip = rng.Chance(0.12f + 0.15f * g.Weathering);
            g.ChipLongitude = rng.NextFloat();
            g.ChipLatitude = rng.Range(-0.55f, 0.55f);

            // --- cavity -------------------------------------------------------------------------
            if (fam.Id == MineralId.Agate)
                g.Cavity = (CavityArchetype)rng.PickWeighted(new[] { 0.12f, 0.28f, 0.02f, 0.05f, 0.03f, 0.5f });
            else
                g.Cavity = (CavityArchetype)rng.PickWeighted(new[] { 0.42f, 0.28f, 0.06f + 0.1f * q, 0.14f, 0.07f, 0.03f });

            g.ShellThickness = g.Cavity switch
            {
                CavityArchetype.Hollow => rng.Range(0.09f, 0.17f),
                CavityArchetype.ThickWall => rng.Range(0.22f, 0.34f),
                CavityArchetype.Cathedral => rng.Range(0.11f, 0.18f),
                CavityArchetype.Pocket => rng.Range(0.26f, 0.38f),
                CavityArchetype.DoubleChamber => rng.Range(0.13f, 0.22f),
                _ => rng.Range(0.4f, 0.55f),
            };
            g.CavityFraction = g.Cavity switch
            {
                CavityArchetype.Hollow => rng.Range(0.74f, 0.88f),
                CavityArchetype.ThickWall => rng.Range(0.52f, 0.66f),
                CavityArchetype.Cathedral => rng.Range(0.62f, 0.76f),
                CavityArchetype.Pocket => rng.Range(0.38f, 0.5f),
                CavityArchetype.DoubleChamber => rng.Range(0.5f, 0.62f),
                _ => rng.Range(0.1f, 0.2f),
            };

            // --- crystal parameters ----------------------------------------------------------
            // quality drives the visible axes hard: a poor rock is pale, cloudy and small-crystalled; a fine one is
            // saturated, clear and well grown. The residual noise keeps two rocks of one tier from matching.
            float qNoise = Mathf.Clamp01(q + rng.Gaussian() * 0.06f);
            // each axis keeps its own roll so one fine rock is all colour and another all crystal, but quality sets
            // most of every axis: a common rock cannot roll a museum colour, a museum rock cannot roll grey
            g.CrystalScale = Mathf.Clamp01(Mathf.Lerp(0.05f, 0.95f, Mathf.Pow(rng.NextFloat(), 1.3f)) * 0.38f + qNoise * 0.62f);
            g.CrystalDensity = Mathf.Clamp01(rng.Range(0.2f, 1f) * 0.4f + qNoise * 0.6f);
            g.Saturation = Mathf.Clamp01(rng.Range(0.1f, 1f) * 0.3f + qNoise * 0.7f);
            g.Clarity = Mathf.Clamp01(rng.Range(0.1f, 1f) * 0.35f + qNoise * 0.65f);
            g.Symmetry = Mathf.Clamp01(rng.Range(0f, 1f) * 0.45f + qNoise * 0.55f);
            g.Zoning = Mathf.Clamp01(fam.ZoningBase * rng.Range(0.3f, 1.7f) * Mathf.Lerp(0.6f, 1.3f, qNoise));

            // --- secondary mineral -------------------------------------------------------------
            if (fam.SecondaryOptions != null && fam.SecondaryOptions.Length > 0 && rng.Chance(fam.SecondaryChance + 0.15f * q))
            {
                g.HasSecondary = true;
                g.Secondary = rng.Pick(fam.SecondaryOptions);
                g.SecondaryAmount = rng.Range(0.08f, 0.3f);
            }

            // --- rare traits --------------------------------------------------------------------
            int traitCount = 0;
            if (q >= 0.97f) traitCount = 2 + (rng.Chance(0.5f) ? 1 : 0);
            else if (q >= 0.9f) traitCount = 1 + (rng.Chance(0.6f) ? 1 : 0);
            else if (q >= 0.78f) traitCount = 1;
            else if (q >= 0.58f) traitCount = rng.Chance(0.3f) ? 1 : 0;
            else traitCount = rng.Chance(0.04f) ? 1 : 0;

            var pool = new List<RareTrait>();
            bool quartzFamily = fam.Id == MineralId.ClearQuartz || fam.Id == MineralId.Amethyst || fam.Id == MineralId.Citrine || fam.Id == MineralId.SmokyQuartz;
            bool nodule = g.Cavity == CavityArchetype.Nodule;
            if (!nodule && fam.CenterpieceChance > 0.2f) pool.Add(RareTrait.GiantCenterpiece);
            if (!nodule) pool.Add(RareTrait.DeepCathedral);
            if (!nodule) pool.Add(RareTrait.DoubleCavity);
            if (fam.DruzyCapable) pool.Add(RareTrait.DenseDruzy);
            if (quartzFamily || fam.Id == MineralId.Fluorite) pool.Add(RareTrait.ColorZoning);
            if (fam.Translucency > 0.45f) pool.Add(RareTrait.HighClarity);
            if (g.HasSecondary) pool.Add(RareTrait.SecondaryContrast);
            if (!nodule) pool.Add(RareTrait.CrystalOnCrystal);
            if (quartzFamily) pool.Add(RareTrait.Phantom);
            if (!nodule) pool.Add(RareTrait.PerfectSymmetry);
            if (g.HasSecondary && g.Secondary == MineralId.Pyrite || fam.Id == MineralId.Pyrite) pool.Add(RareTrait.MetallicContrast);
            if (!nodule && fam.Placement != PlacementStyle.Banded) pool.Add(RareTrait.GiantCrystalField);

            for (int i = 0; i < traitCount && pool.Count > 0; i++)
            {
                var t = rng.Pick(pool);
                pool.Remove(t);
                g.Traits.Add(t);
                ApplyTrait(g, t, ref rng);
            }

            // --- cavity lobes --------------------------------------------------------------------
            BuildLobes(g, ref rng);

            // --- mass ----------------------------------------------------------------------------
            float r = g.Size;
            float volume = 4f / 3f * Mathf.PI * r * r * r * g.Axes.x * g.Axes.y * g.Axes.z;
            float cav = g.CavityFraction;
            float hollow = Mathf.Clamp01(cav * cav * cav);            // cavity volume fraction
            float density = 2650f * fam.ShellToughness;              // kg/m3 (chalcedony ~2.6)
            g.MassKg = volume * density * (1f - hollow * 0.92f);

            g.BaseValue = Valuation.PristineValue(g);
            return g;
        }

        private static void ApplyTrait(SpecimenGeology g, RareTrait t, ref SeededRandom rng)
        {
            switch (t)
            {
                case RareTrait.GiantCenterpiece: g.CrystalScale = Mathf.Max(g.CrystalScale, 0.45f); break;
                case RareTrait.DeepCathedral: g.Cavity = CavityArchetype.Cathedral; g.CavityFraction = Mathf.Max(g.CavityFraction, 0.62f); g.ShellThickness = Mathf.Min(g.ShellThickness, 0.2f); break;
                case RareTrait.DoubleCavity: g.Cavity = CavityArchetype.DoubleChamber; g.CavityFraction = Mathf.Max(g.CavityFraction, 0.45f); break;
                case RareTrait.DenseDruzy: g.CrystalScale = Mathf.Min(g.CrystalScale, 0.16f); g.CrystalDensity = 1f; g.Saturation = Mathf.Max(g.Saturation, 0.6f); break;
                case RareTrait.ColorZoning: g.Zoning = 1f; g.Saturation = Mathf.Max(g.Saturation, 0.7f); break;
                case RareTrait.HighClarity: g.Clarity = 1f; break;
                case RareTrait.SecondaryContrast: g.SecondaryAmount = Mathf.Max(g.SecondaryAmount, 0.35f); break;
                case RareTrait.CrystalOnCrystal: g.CrystalScale = Mathf.Max(g.CrystalScale, 0.5f); if (!g.HasSecondary) { g.HasSecondary = true; g.Secondary = g.Family.SecondaryOptions[0]; g.SecondaryAmount = 0.25f; } break;
                case RareTrait.Phantom: g.Zoning = Mathf.Max(g.Zoning, 0.8f); g.Clarity = Mathf.Max(g.Clarity, 0.7f); break;
                case RareTrait.PerfectSymmetry: g.Symmetry = 1f; g.CrystalDensity = Mathf.Max(g.CrystalDensity, 0.7f); break;
                case RareTrait.MetallicContrast: if (g.Mineral != MineralId.Pyrite) { g.HasSecondary = true; g.Secondary = MineralId.Pyrite; g.SecondaryAmount = Mathf.Max(g.SecondaryAmount, 0.3f); } break;
                case RareTrait.GiantCrystalField: g.CrystalScale = Mathf.Max(g.CrystalScale, 0.82f); g.CrystalDensity = Mathf.Max(g.CrystalDensity, 0.55f); break;
            }
        }

        private static void BuildLobes(SpecimenGeology g, ref SeededRandom rng)
        {
            float r = g.Size * g.CavityFraction;
            float R = g.Size;
            var centers = new List<Vector3>();
            var radii = new List<float>();
            // Main lobe: biased below the fracture plane so the bottom (display) half is the deeper one.
            float yOff = -Mathf.Min(rng.Range(0.02f, 0.12f) * R, 0.5f * r);
            centers.Add(new Vector3(0f, yOff, 0f));
            radii.Add(r);
            switch (g.Cavity)
            {
                case CavityArchetype.Cathedral:
                    centers.Add(new Vector3(0f, yOff - 0.32f * R, 0f));
                    radii.Add(r * 0.82f);
                    break;
                case CavityArchetype.DoubleChamber:
                {
                    float a = rng.Range(0f, Mathf.PI * 2f);
                    var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (0.42f * R);
                    centers.Add(d + new Vector3(0f, yOff * 0.5f, 0f));
                    radii.Add(r * 0.75f);
                    centers.Add(-d + new Vector3(0f, yOff * 0.5f, 0f));
                    radii.Add(r * 0.7f);
                    break;
                }
                case CavityArchetype.Pocket:
                {
                    var off = rng.InsideUnitCircle() * (0.22f * R);
                    centers[0] = new Vector3(off.x, yOff, off.y);
                    break;
                }
            }
            g.LobeCenters = centers.ToArray();
            g.LobeRadii = radii.ToArray();
        }
    }
}
