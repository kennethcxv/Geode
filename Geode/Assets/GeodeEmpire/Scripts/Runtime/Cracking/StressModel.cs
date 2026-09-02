using System;
using UnityEngine;
using GeodeEmpire.Core;

namespace GeodeEmpire.Cracking
{
    /// <summary>
    /// Simplified but legible fracture model: a ring of sectors around the natural seam accumulates stress.
    /// Placement (distance from the seam), angle, force, tool focus, clamping and existing cracks all matter.
    /// Pure C#: unit-testable, and the whole thing serialises as a float[] on the specimen record.
    /// </summary>
    public sealed class StressModel
    {
        public const int Sectors = 16;
        public const float OpenFraction = 0.72f;

        public readonly float[] Stress = new float[Sectors];
        public float Toughness = 1f;          // matrix toughness (family)
        public float ShellThickness = 0.2f;   // fraction of radius
        public float Fragility = 0.5f;        // crystal breakage susceptibility
        public bool FineChisel;
        public bool Clamped;
        public int StrikeCount;

        public struct StrikeInput
        {
            public float Azimuth;        // radians around the rock's up axis, rock-local
            public float PlaneOffset;    // signed distance from the seam / rock radius (-1..1)
            public float Force;          // 0.15..1
            public float AngleFactor;    // 1 = square to the surface, 0 = glancing
        }

        public struct StrikeResult
        {
            public int Sector;
            public float StressAdded;
            public float Placement;      // 0..1 seam accuracy
            public bool Slipped;
            public bool NewCrack;
            public bool Propagated;      // extended an existing crack
            public int CracksTotal;
            public bool Opened;
            public bool Shattered;       // opened by brute force (extra damage)
            public bool Overstrike;      // hammered an already-cracked sector
            public float DamageChance;
            public bool Damaged;
            public float DamageSeverity;
            public float Quality;        // 0..1 how good this strike was (for feedback/tutorial)
        }

        public bool IsCracked(int sector) => Stress[sector] >= 1f;

        public int CrackedCount()
        {
            int n = 0;
            for (int i = 0; i < Sectors; i++) if (Stress[i] >= 1f) n++;
            return n;
        }

        public float TotalStress()
        {
            float t = 0f;
            for (int i = 0; i < Sectors; i++) t += Mathf.Min(Stress[i], 1.2f);
            return t;
        }

        /// <summary>0..1 progress toward opening, for the bench meter.</summary>
        public float Progress()
        {
            return Mathf.Clamp01(TotalStress() / (Sectors * OpenFraction));
        }

        public static int SectorOf(float azimuth)
        {
            float a = azimuth / (Mathf.PI * 2f);
            a -= Mathf.Floor(a);
            return Mathf.Clamp((int)(a * Sectors), 0, Sectors - 1);
        }

        public StrikeResult Strike(StrikeInput s, ref SeededRandom rng)
        {
            var r = new StrikeResult();
            StrikeCount++;
            int sector = SectorOf(s.Azimuth);
            r.Sector = sector;
            float force = Mathf.Clamp(s.Force, 0.12f, 1f);
            float placement = Mathf.Exp(-(s.PlaneOffset * s.PlaneOffset) / (0.32f * 0.32f));
            r.Placement = placement;
            float angle = Mathf.Clamp01(s.AngleFactor);

            // glancing heavy blows on an unclamped rock can skid off
            if (!Clamped && angle < 0.5f && force > 0.55f && rng.Chance(0.55f))
            {
                r.Slipped = true;
                r.StressAdded = 0f;
                r.DamageChance = 0.12f * force * Fragility;
                r.Damaged = rng.Chance(r.DamageChance);
                r.DamageSeverity = r.Damaged ? force * 0.5f : 0f;
                r.Quality = 0f;
                r.CracksTotal = CrackedCount();
                return r;
            }

            bool wasCracked = Stress[sector] >= 1f;
            float thicknessMult = Mathf.Lerp(1.35f, 0.75f, Mathf.InverseLerp(0.08f, 0.5f, ShellThickness));
            float toolMult = FineChisel ? 1.18f : 1f;
            float clampMult = Clamped ? 1.1f : 1f;
            float baseStress = 1.9f * force * (0.4f + 0.6f * angle) * placement * thicknessMult * toolMult * clampMult / Mathf.Max(0.5f, Toughness);

            // existing fracture lines guide the crack
            int left = (sector + Sectors - 1) % Sectors, right = (sector + 1) % Sectors;
            bool neighborCracked = Stress[left] >= 1f || Stress[right] >= 1f;
            if (neighborCracked && !wasCracked) baseStress *= 1.3f;

            float spread = FineChisel ? 0.28f : 0.45f;
            if (wasCracked)
            {
                // energy goes past the open crack into the neighbours, but mostly wasted
                r.Overstrike = force > 0.45f;
                Stress[left] += baseStress * spread * 0.8f;
                Stress[right] += baseStress * spread * 0.8f;
                r.StressAdded = baseStress * spread * 1.6f;
            }
            else
            {
                Stress[sector] += baseStress;
                Stress[left] += baseStress * spread;
                Stress[right] += baseStress * spread;
                r.StressAdded = baseStress * (1f + 2f * spread);
            }
            r.NewCrack = !wasCracked && Stress[sector] >= 1f;
            r.Propagated = r.NewCrack && neighborCracked;
            int cracks = CrackedCount();
            r.CracksTotal = cracks;

            // damage: heavy force, off-seam hits, thin shells and overstrikes hurt crystals
            float thin = Mathf.InverseLerp(0.3f, 0.08f, ShellThickness);
            float chance = Fragility * force * force * 0.5f * (1f + 0.9f * (1f - placement)) * (1f + 0.5f * thin);
            if (r.Overstrike) chance *= 1.7f;
            if (FineChisel) chance *= 0.7f;
            if (force < 0.35f) chance *= 0.2f;
            else if (force < 0.55f) chance *= 0.55f;
            r.DamageChance = Mathf.Clamp01(chance);
            r.Damaged = rng.Chance(r.DamageChance);
            r.DamageSeverity = r.Damaged ? Mathf.Clamp01(force * rng.Range(0.55f, 1.05f) * (r.Overstrike ? 1.2f : 1f)) : 0f;

            // opening: enough of the ring is cracked, or the shell is simply overwhelmed
            bool ringOpen = cracks >= Mathf.CeilToInt(Sectors * OpenFraction) && (r.NewCrack || wasCracked || cracks >= Sectors - 1);
            bool overwhelmed = TotalStress() >= Sectors * 1.05f;
            if (ringOpen || overwhelmed)
            {
                r.Opened = true;
                r.Shattered = overwhelmed && !ringOpen;
                if (r.Shattered) { r.Damaged = true; r.DamageSeverity = Mathf.Max(r.DamageSeverity, 0.8f); }
            }
            r.Quality = Mathf.Clamp01(placement * (0.5f + 0.5f * angle) * (wasCracked ? 0.2f : 1f) * (force > 0.85f ? 0.7f : 1f));
            return r;
        }

        public void CopyFrom(float[] saved)
        {
            if (saved == null) return;
            for (int i = 0; i < Sectors && i < saved.Length; i++) Stress[i] = saved[i];
        }

        public float[] ToArray()
        {
            var a = new float[Sectors];
            Array.Copy(Stress, a, Sectors);
            return a;
        }
    }
}
