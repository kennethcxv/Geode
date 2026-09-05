using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Audio
{
    /// <summary>One scheduled layer of a break: which cue, how far behind the blow, and at what level and pitch.</summary>
    public readonly struct FractureLayer
    {
        public readonly string Cue;
        public readonly float Delay, Volume, Pitch;
        public FractureLayer(string cue, float delay, float volume, float pitch)
        { Cue = cue; Delay = delay; Volume = volume; Pitch = pitch; }
        public override string ToString() => $"{Cue}@{Delay:0.000}s v{Volume:0.00} p{Pitch:0.00}";
    }

    /// <summary>
    /// The break, as a sequence rather than a sample (§9.1). A rock coming apart is a tool impact, then the first
    /// fibre giving, then the mass of the shell separating, then chips, then grit settling — and those arrive over
    /// about a second and a half, not on one frame.
    ///
    /// <see cref="Plan"/> is deliberately pure: no Unity audio, no randomness, so the three things §9.2 says a
    /// player must be able to hear — how big the rock is, what it is made of, which tool opened it — can be
    /// asserted in a test instead of taken on trust. The old code played `crack_final` and `fragments` together at
    /// a fixed pitch for every rock in the game, so a fist-sized calcite nodule and a large agate were identical.
    /// </summary>
    public static class FractureAudio
    {
        public enum Tool { Hammer, Cracker, Saw }

        /// <summary>Radius range the size scaling is spread across, in metres.</summary>
        public const float SmallRadius = 0.03f, LargeRadius = 0.13f;

        /// <summary>
        /// The layers a break of this rock, on this tool, should play. <paramref name="radius"/> is the mean
        /// equator radius in metres and <paramref name="toughness"/> the shell toughness from the mineral
        /// catalogue (roughly 0.5 soft .. 1.5 tough).
        /// </summary>
        public static List<FractureLayer> Plan(float radius, float toughness, Tool tool, bool rare, bool shattered = false)
        {
            // size: a small nodule cracks high and short, a large one low and long
            float size = Mathf.InverseLerp(SmallRadius, LargeRadius, radius);      // 0 small .. 1 large
            float sizePitch = Mathf.Lerp(1.28f, 0.80f, size);
            float sizeGain = Mathf.Lerp(0.70f, 1.10f, size);

            // material: a tough shell snaps brightly and rings on; a soft one gives duller and shorter
            float tough = Mathf.Clamp01((toughness - 0.5f) / 1.0f);                // 0 soft .. 1 tough
            float bright = Mathf.Lerp(0.90f, 1.14f, tough);

            // tool balance: the hammer puts a metal transient in front, the cracker is press and mass with no ring
            // at all, and the saw does not fracture — it parts the last web of an already-cut face
            float onset = tool == Tool.Saw ? 0.25f : tool == Tool.Cracker ? 0.55f : 1f;
            float split = tool == Tool.Saw ? 0.45f : tool == Tool.Cracker ? 1.10f : 1f;
            float debris = tool == Tool.Saw ? 0.35f : tool == Tool.Cracker ? 0.75f : 1f;

            // a shell burst by brute force throws far more off it than one that opened along its seam
            if (shattered) { debris *= 1.5f; split *= 1.12f; }

            var layers = new List<FractureLayer>(6)
            {
                // the first thing to let go, on the frame of the blow
                new FractureLayer("crack_onset", 0f, 0.75f * onset * Mathf.Lerp(1f, 0.82f, size), sizePitch * bright),
                // the mass of the shell parting, a couple of frames behind it
                new FractureLayer("stone_split", 0.018f, 1.0f * split * sizeGain, sizePitch * Mathf.Lerp(1.03f, 0.96f, tough)),
                // the old body layer, kept as low weight under the split rather than carrying the cue itself
                new FractureLayer("crack_final", 0.026f, 0.55f * split * sizeGain, (rare ? 0.92f : 1f) * sizePitch),
                // chips and flakes, just after the faces separate
                new FractureLayer("fragments", 0.07f, 0.70f * debris * Mathf.Lerp(0.85f, 1.05f, size), Mathf.Lerp(1.10f, 0.94f, size) * bright),
                // grit settling on the bench for the next second and a half
                new FractureLayer("debris_settle", 0.30f, 0.55f * debris, Mathf.Lerp(1.06f, 0.92f, size)),
            };
            // a tough shell keeps ringing a moment after it opens; a soft one does not, and a saw never does
            if (tough > 0.45f && tool != Tool.Saw)
                layers.Add(new FractureLayer("tension", 0.05f, 0.22f * tough, 1.05f + 0.15f * (1f - size)));
            return layers;
        }

        public static void Break(Vector3 pos, float radius, float toughness, Tool tool, bool rare, bool shattered = false)
        {
            foreach (var l in Plan(radius, toughness, tool, rare, shattered))
                WorkshopAudio.PlayDelayed(l.Cue, pos, l.Delay, l.Volume, l.Pitch);
        }

        /// <summary>The same call, taking the rock so callers do not each re-derive size and toughness.</summary>
        public static void Break(Vector3 pos, SpecimenVisual vis, SpecimenGeology geology, Tool tool, bool rare, bool shattered = false)
            => Break(pos, RadiusOf(vis), ToughnessOf(geology), tool, rare, shattered);

        public static float RadiusOf(SpecimenVisual vis)
            => vis != null && vis.Geometry != null ? vis.Geometry.MeanEquatorRadius : 0.07f;

        public static float ToughnessOf(SpecimenGeology geology)
        {
            if (geology == null) return 1f;
            var fam = MineralCatalog.Get(geology.Mineral);
            return fam != null ? fam.ShellToughness : 1f;
        }
    }
}
