using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Cracking;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Workshop
{
    /// <summary>
    /// Specimen-specific preparation: what a rock asks for before it is opened, derived from its geology and condition
    /// rather than a universal place-and-press flow. Pure functions so the rules are testable.
    /// </summary>
    public static class Preparation
    {
        /// <summary>Clay past this much of the shell hides the seam at the bench.</summary>
        public const float SeamHiddenDirt = 0.5f;

        /// <summary>0 = the seam is hidden under clay, 1 = the shell reads clean.</summary>
        public static float Cleanliness(float dirtRemaining) => 1f - Mathf.Clamp01(dirtRemaining / SeamHiddenDirt);

        /// <summary>A natural chip close to the seam is a ready-made start for a crack: its sector, or -1.</summary>
        public static int ChipSector(SpecimenGeology g)
        {
            if (g == null || !g.HasNaturalChip || Mathf.Abs(g.ChipLatitude) > 0.3f) return -1;
            return StressModel.SectorOf(g.ChipLongitude * Mathf.PI * 2f);
        }

        /// <summary>Pre-stress a fresh rock's chip sector: the shell is already started there.</summary>
        public const float ChipStartStress = 0.5f;

        /// <summary>Height above the lowest point where a sandbag ring touches a rock.</summary>
        public const float RingContactHeight = 0.03f;

        /// <summary>
        /// Seat quality from how the hull stands: wide and low on the ring is firm, tall and narrow rocks. A rock much
        /// smaller than the ring drops into the hollow and is held all round; a boulder overhanging the small ring is
        /// held far below its mass and never sits well. The bench clamp holds any rock down.
        /// </summary>
        public static float Stability(float hullHeight, float baseHalfWidth, float rockRadius, float ringRadius, bool oversizedOnSmallRing, bool clamped)
        {
            float stance = Mathf.Clamp01(baseHalfWidth / Mathf.Max(0.02f, 0.4f * hullHeight));
            float s = stance;
            float sizeToRing = rockRadius / Mathf.Max(0.02f, ringRadius);
            if (sizeToRing < 0.7f) s = Mathf.Max(s, 0.9f);
            if (oversizedOnSmallRing) s = Mathf.Min(s, 0.2f);
            else if (sizeToRing > 1.6f) s = Mathf.Min(s, 0.55f);
            if (clamped) s = Mathf.Max(s, 0.85f);
            return Mathf.Clamp01(s);
        }

        public static string SeatWord(float stability) => stability >= 0.8f ? "firm" : stability >= StressModel.UnstableBelow ? "uneven" : "rocking";

        /// <summary>What a clean shell tells the hand: seam, chip, staining, mineral showing through.</summary>
        public static List<string> ShellNotes(SpecimenGeology g)
        {
            var notes = new List<string>();
            if (g == null) return notes;
            if (g.SeamQuality > 0.7f) notes.Add("clean seam right round");
            else if (g.SeamQuality < 0.4f) notes.Add("ragged seam");
            if (g.HasNaturalChip) notes.Add(Mathf.Abs(g.ChipLatitude) <= 0.3f ? "natural chip at the seam" : "chipped shoulder");
            if (g.Stain > 0.5f) notes.Add("iron-stained");
            if (g.ExteriorHint > 0.4f) notes.Add("colour showing through");
            notes.Add(SpecimenGeology.TextureWord(g.Texture));
            return notes;
        }
    }
}
