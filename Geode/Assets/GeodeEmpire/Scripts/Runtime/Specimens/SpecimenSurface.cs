using System;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;

namespace GeodeEmpire.Specimens
{
    /// <summary>What a region of shell can be carrying.</summary>
    public enum ClueKind
    {
        None = 0,
        ThickRind,
        ThinRind,
        IronStaining,
        Weathering,
        Pitting,
        ExposedQuartz,
        Banding,
        CavityOpening,
        PossibleSeam,
        DenseShell,
        FractureLine,
        UnusualTexture,
        SecondaryStaining,
        CrystalShowing,
        ClayFilled,
    }

    /// <summary>How far the player has got with one clue (§5.5).</summary>
    public enum ClueState : byte
    {
        Undiscovered = 0,
        Seen = 1,        // noticed with the naked eye: something is there
        Logged = 2,      // read properly, and written down
    }

    /// <summary>
    /// The shell divided into places. Everything that has to be true of *part* of a rock rather than all of it —
    /// clay that is still on the far side, a chip you have not turned round to, a stain you have looked at —
    /// is indexed by region, so cleaning and inspecting are things you do somewhere rather than things you
    /// do to a number.
    ///
    /// A region is one longitude sector of one latitude band. Sixteen sectors would match the stress model but
    /// is finer than a brush or an eye discriminates; eight around and three up gives twenty-four patches of
    /// roughly a thumb's width on a fist-sized rock, which is the scale the interaction actually works at.
    /// </summary>
    public static class SpecimenSurface
    {
        public const int Longitudes = 8;
        public const int Bands = 3;                 // lower, equator, upper
        public const int Regions = Longitudes * Bands;

        /// <summary>Latitude (normalised -1..1) below which a point is in the lower band, and above which the upper.</summary>
        public const float LowerBand = -0.34f, UpperBand = 0.34f;

        public static int Index(int longitude, int band) =>
            (((longitude % Longitudes) + Longitudes) % Longitudes) + Mathf.Clamp(band, 0, Bands - 1) * Longitudes;

        /// <summary>Which region a specimen-local direction falls in.</summary>
        public static int RegionOf(Vector3 localDir)
        {
            var d = localDir.sqrMagnitude > 1e-8f ? localDir.normalized : Vector3.up;
            float lon = Mathf.Atan2(d.z, d.x);                       // -pi..pi
            int li = Mathf.FloorToInt((lon / (Mathf.PI * 2f) + 0.5f) * Longitudes);
            int band = d.y < LowerBand ? 0 : d.y < UpperBand ? 1 : 2;
            return Index(li, band);
        }

        public static int LongitudeOf(int region) => region % Longitudes;
        public static int BandOf(int region) => region / Longitudes;

        /// <summary>The unit direction at the middle of a region, for pointing a beacon or seating a marker.</summary>
        public static Vector3 DirectionOf(int region)
        {
            float lon = ((LongitudeOf(region) + 0.5f) / Longitudes - 0.5f) * Mathf.PI * 2f;
            float y = BandOf(region) switch { 0 => -0.62f, 1 => 0f, _ => 0.62f };
            float r = Mathf.Sqrt(Mathf.Max(0.0001f, 1f - y * y));
            return new Vector3(Mathf.Cos(lon) * r, y, Mathf.Sin(lon) * r);
        }

        /// <summary>
        /// How strongly a brush landing on <paramref name="hitRegion"/> also works its neighbours. A brush is
        /// wider than a patch, so the edges of adjacent regions come clean too — without this, cleaning leaves
        /// visible square tiles instead of a wiped surface.
        /// </summary>
        public static float Falloff(int hitRegion, int otherRegion)
        {
            if (hitRegion == otherRegion) return 1f;
            int dl = Mathf.Abs(LongitudeOf(hitRegion) - LongitudeOf(otherRegion));
            dl = Mathf.Min(dl, Longitudes - dl);
            int db = Mathf.Abs(BandOf(hitRegion) - BandOf(otherRegion));
            if (dl > 1 || db > 1) return 0f;
            if (dl == 1 && db == 1) return 0.16f;
            return 0.38f;
        }

        // -----------------------------------------------------------------------------------------
        // Clues
        // -----------------------------------------------------------------------------------------

        /// <summary>One thing there is to notice, and where on the rock it is.</summary>
        public readonly struct Clue
        {
            public readonly int Region;
            public readonly ClueKind Kind;
            /// <summary>0..1. Faint clues need the loupe; obvious ones do not.</summary>
            public readonly float Strength;
            public Clue(int region, ClueKind kind, float strength) { Region = region; Kind = kind; Strength = strength; }
            /// <summary>Below this the naked eye will not resolve it: §5.7's reason to own a loupe.</summary>
            public bool NeedsLoupe => Strength < 0.5f;
        }

        /// <summary>
        /// The clues a rock is carrying, derived from its geology and its seed alone, so the same rock always
        /// reads the same way and a reload never moves a stain. §5.6: none of these names the mineral inside.
        /// </summary>
        public static List<Clue> Clues(SpecimenGeology g)
        {
            var list = new List<Clue>(8);
            if (g == null) return list;
            var rng = new SeededRandom(SeededRandom.Combine(g.Seed, 0x5C10E5CAUL));

            void Add(int region, ClueKind kind, float strength) => list.Add(new Clue(region, kind, Mathf.Clamp01(strength)));

            // the seam runs round the equator: it shows in the band where it is best defined
            if (g.SeamQuality > 0.35f)
            {
                int spread = g.SeamQuality > 0.7f ? 3 : g.SeamQuality > 0.5f ? 2 : 1;
                int start = Mathf.FloorToInt(rng.Range(0f, Longitudes - 0.001f));
                for (int i = 0; i < spread; i++)
                    Add(Index(start + i * 2, 1), ClueKind.PossibleSeam, 0.35f + g.SeamQuality * 0.55f);
            }
            // a natural chip is the best exterior clue there is, and it is in exactly one place
            if (g.HasNaturalChip)
            {
                int li = Mathf.FloorToInt(g.ChipLongitude * Longitudes);
                int band = g.ChipLatitude < LowerBand ? 0 : g.ChipLatitude < UpperBand ? 1 : 2;
                int region = Index(li, band);
                Add(region, ClueKind.CavityOpening, 0.72f);
                // and if the cavity is generous, something of the interior is showing through it
                if (g.CavityFraction > 0.45f) Add(region, ClueKind.CrystalShowing, 0.42f);
            }
            if (g.Stain > 0.35f)
            {
                int n = g.Stain > 0.7f ? 3 : 2;
                for (int i = 0; i < n; i++) Add(Mathf.FloorToInt(rng.Range(0f, Regions - 0.001f)), ClueKind.IronStaining, 0.3f + g.Stain * 0.5f);
            }
            if (g.HasSecondary && g.SecondaryAmount > 0.25f)
                Add(Mathf.FloorToInt(rng.Range(0f, Regions - 0.001f)), ClueKind.SecondaryStaining, 0.28f + g.SecondaryAmount * 0.4f);
            if (g.Weathering > 0.5f)
                Add(Mathf.FloorToInt(rng.Range(0f, Regions - 0.001f)), ClueKind.Weathering, 0.3f + g.Weathering * 0.4f);
            if (g.ExteriorHint > 0.3f)
                Add(Mathf.FloorToInt(rng.Range(0f, Regions - 0.001f)), ClueKind.ExposedQuartz, 0.25f + g.ExteriorHint * 0.55f);
            // shell thickness is readable sector by sector: a thin spot is where it will give
            if (g.SectorThickness != null && g.SectorThickness.Length > 0)
            {
                int thinnest = 0, thickest = 0;
                for (int i = 1; i < g.SectorThickness.Length; i++)
                {
                    if (g.SectorThickness[i] < g.SectorThickness[thinnest]) thinnest = i;
                    if (g.SectorThickness[i] > g.SectorThickness[thickest]) thickest = i;
                }
                int per = Mathf.Max(1, g.SectorThickness.Length / Longitudes);
                if (g.SectorThickness[thinnest] < 0.88f) Add(Index(thinnest / per, 1), ClueKind.ThinRind, 0.34f);
                if (g.SectorThickness[thickest] > 1.14f) Add(Index(thickest / per, 1), ClueKind.ThickRind, 0.34f);
            }
            if (g.ShellThickness > 0.26f) Add(Index(Mathf.FloorToInt(rng.Range(0f, Longitudes - 0.001f)), 2), ClueKind.DenseShell, 0.3f);
            switch (g.Texture)
            {
                case ExteriorTexture.Weathered: Add(Mathf.FloorToInt(rng.Range(0f, Regions - 0.001f)), ClueKind.Pitting, 0.45f); break;
                case ExteriorTexture.Banded: Add(Mathf.FloorToInt(rng.Range(0f, Regions - 0.001f)), ClueKind.Banding, 0.5f); break;
                case ExteriorTexture.Volcanic: Add(Mathf.FloorToInt(rng.Range(0f, Regions - 0.001f)), ClueKind.UnusualTexture, 0.44f); break;
                case ExteriorTexture.Coarse: Add(Mathf.FloorToInt(rng.Range(0f, Regions - 0.001f)), ClueKind.Pitting, 0.38f); break;
            }
            if (g.RimRoughness > 0.6f) Add(Index(Mathf.FloorToInt(rng.Range(0f, Longitudes - 0.001f)), 1), ClueKind.FractureLine, 0.3f);
            return list;
        }

        /// <summary>What the player writes down when they have read a clue properly.</summary>
        public static string Describe(ClueKind kind) => kind switch
        {
            ClueKind.ThickRind => "thick rind here",
            ClueKind.ThinRind => "the rind thins here",
            ClueKind.IronStaining => "iron staining",
            ClueKind.Weathering => "weathered face",
            ClueKind.Pitting => "pitted",
            ClueKind.ExposedQuartz => "quartz exposed",
            ClueKind.Banding => "banding in the shell",
            ClueKind.CavityOpening => "an opening into the cavity",
            ClueKind.PossibleSeam => "the seam runs through here",
            ClueKind.DenseShell => "dense, heavy shell",
            ClueKind.FractureLine => "a fracture line",
            ClueKind.UnusualTexture => "unusual texture",
            ClueKind.SecondaryStaining => "a second mineral staining the shell",
            ClueKind.CrystalShowing => "crystal showing through",
            ClueKind.ClayFilled => "clay packed into the pits",
            _ => "",
        };

        /// <summary>The short word the prompt uses before the clue has been read properly.</summary>
        public static string Glimpse(ClueKind kind) => kind switch
        {
            ClueKind.CavityOpening => "a break in the shell",
            ClueKind.CrystalShowing => "something catching the light",
            ClueKind.PossibleSeam => "a line",
            ClueKind.IronStaining or ClueKind.SecondaryStaining => "a discolouration",
            ClueKind.Banding => "a pattern",
            ClueKind.FractureLine => "a mark",
            _ => "something",
        };

        /// <summary>
        /// What a set of read clues suggests. §5.6 is explicit that exterior inspection must never name the
        /// mineral: this reports what the evidence is pointing at, and how firmly.
        /// </summary>
        public static string Reading(IReadOnlyList<Clue> logged)
        {
            if (logged == null || logged.Count == 0) return "";
            bool hollow = false, solid = false, fragile = false, tough = false;
            foreach (var c in logged)
                switch (c.Kind)
                {
                    case ClueKind.CavityOpening: case ClueKind.CrystalShowing: hollow = true; break;
                    case ClueKind.DenseShell: case ClueKind.ThickRind: solid = true; tough = true; break;
                    case ClueKind.ThinRind: case ClueKind.FractureLine: fragile = true; break;
                    case ClueKind.PossibleSeam: break;
                }
            if (hollow && !solid) return "the evidence says hollow";
            if (solid && !hollow) return "the evidence says solid, or a thick wall";
            if (hollow && solid) return "the evidence disagrees with itself";
            if (fragile) return "it should give easily";
            if (tough) return "it will take some work";
            return "not enough to say yet";
        }
    }
}
