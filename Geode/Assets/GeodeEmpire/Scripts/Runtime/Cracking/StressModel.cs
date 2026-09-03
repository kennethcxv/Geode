using System;
using UnityEngine;
using GeodeEmpire.Core;

namespace GeodeEmpire.Cracking
{
    /// <summary>
    /// Simplified but legible fracture model: a ring of sectors around the natural seam accumulates stress.
    /// Placement (distance from the seam), angle, force, tool focus, clamping and existing cracks all matter, and so
    /// does the geology: how big the rock is, how well its seam is defined, how thick the shell runs in each sector.
    /// A correct-looking strike does not always bite the same way, but skill shifts the odds hard.
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
        /// <summary>Splitting wedge and lump hammer: a heavier bite on large rough, too much for thin shells.</summary>
        public bool Wedge;
        /// <summary>Rock too big for the cradle it sits on: it rocks under every blow.</summary>
        public bool Unstable;
        /// <summary>Mean radius (m). Bigger shells need more work all the way round.</summary>
        public float Radius = 0.065f;
        /// <summary>How well defined the natural seam is (0.3..1): a poor seam wastes part of every strike.</summary>
        public float SeamQuality = 1f;
        /// <summary>Per-sector shell thickness multiplier (0.75..1.3); null = uniform.</summary>
        public float[] SectorThickness;
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
            /// <summary>The chisel skated and only took a flake off the shell: a chip mark, little progress.</summary>
            public bool SurfaceChip;
            /// <summary>The crack ran further than the blow deserved: the geology gave way along a weak line.</summary>
            public bool Lucky;
            /// <summary>The rock shifted on an undersized cradle.</summary>
            public bool Wobbled;
            /// <summary>Thickness multiplier of the sector that was hit (for feedback).</summary>
            public float SectorThick;
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

        public float ThicknessAt(int sector) => SectorThickness != null && sector >= 0 && sector < SectorThickness.Length ? SectorThickness[sector] : 1f;

        /// <summary>How much harder this shell is to work than a medium rock, from its size alone (1 = medium).</summary>
        public float SizeEffort => 1f / Mathf.Lerp(1.3f, 0.5f, Mathf.InverseLerp(0.035f, 0.165f, Radius));

        public StrikeResult Strike(StrikeInput s, ref SeededRandom rng)
        {
            var r = new StrikeResult();
            StrikeCount++;
            int sector = SectorOf(s.Azimuth);
            r.Sector = sector;
            r.SectorThick = ThicknessAt(sector);
            float force = Mathf.Clamp(s.Force, 0.12f, 1f);
            float placement = Mathf.Exp(-(s.PlaneOffset * s.PlaneOffset) / (0.32f * 0.32f));
            r.Placement = placement;
            float angle = Mathf.Clamp01(s.AngleFactor);
            float seam = Mathf.Lerp(0.8f, 1f, Mathf.Clamp01(SeamQuality));

            // an oversized rock on the small cradle shifts under the blow: part of the energy goes into the wobble,
            // and a hard swing can skid off entirely
            if (Unstable && force > 0.3f)
            {
                r.Wobbled = true;
                if (force > 0.55f && rng.Chance(0.3f))
                {
                    r.Slipped = true;
                    r.StressAdded = 0f;
                    r.DamageChance = 0.2f * force * Fragility;
                    r.Damaged = rng.Chance(r.DamageChance);
                    r.DamageSeverity = r.Damaged ? force * 0.6f : 0f;
                    r.Quality = 0f;
                    r.CracksTotal = CrackedCount();
                    return r;
                }
            }

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
            float localThickness = ShellThickness * r.SectorThick;
            float thicknessMult = Mathf.Lerp(1.35f, 0.75f, Mathf.InverseLerp(0.08f, 0.5f, localThickness));
            float toolMult = FineChisel ? 1.18f : 1f;
            bool big = Radius >= 0.08f;
            if (Wedge && big) toolMult *= 1.35f;
            float clampMult = Clamped ? 1.1f : 1f;
            float sizeMult = 1f / SizeEffort;
            if (Unstable) sizeMult *= 0.6f;
            float baseStress = 1.9f * force * (0.4f + 0.6f * angle) * placement * seam * thicknessMult * toolMult * clampMult * sizeMult / Mathf.Max(0.5f, Toughness);

            // geology answers the blow: the same strike bites a little differently every time
            float bite = rng.Range(0.78f, 1.22f);
            // a soft, badly angled tap can skate and just take a flake off the shell
            if (!wasCracked && angle < 0.55f && force < 0.5f && rng.Chance(0.15f))
            {
                r.SurfaceChip = true;
                bite *= 0.3f;
            }
            baseStress *= bite;

            // existing fracture lines guide the crack, and a ring that is mostly open pulls the rest apart:
            // the last few segments crack under far less than the first ones did
            int left = (sector + Sectors - 1) % Sectors, right = (sector + 1) % Sectors;
            bool neighborCracked = Stress[left] >= 1f || Stress[right] >= 1f;
            if (neighborCracked && !wasCracked) baseStress *= 1.3f;
            // now and then the crack finds a weak line and runs further than the blow deserved
            if (neighborCracked && !wasCracked && !r.SurfaceChip && rng.Chance(0.1f + 0.08f * seam)) { r.Lucky = true; baseStress *= 1.7f; }
            float ringFrac = CrackedCount() / (float)Sectors;
            if (!wasCracked) baseStress *= 1f + 1.1f * ringFrac * ringFrac;

            float spread = FineChisel ? 0.28f : 0.45f;
            if (r.Lucky) spread += 0.25f;
            if (wasCracked)
            {
                // energy goes past the open crack into the neighbours, but mostly wasted
                r.Overstrike = force > 0.45f;
                Stress[left] += baseStress * spread * 0.5f;
                Stress[right] += baseStress * spread * 0.5f;
                Stress[sector] += baseStress * 0.25f;  // keeps accumulating toward a brute-force shatter
                r.StressAdded = baseStress * spread;
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
            float thin = Mathf.InverseLerp(0.3f, 0.08f, localThickness);
            float chance = Fragility * force * force * force * 0.6f * (1f + 0.9f * (1f - placement)) * (1f + 0.5f * thin);
            if (r.Overstrike) chance *= 1.7f;
            if (FineChisel) chance *= 0.7f;
            if (Wedge && big) chance *= 1f + 0.6f * thin;   // the wedge drives too deep into a thin shell
            if (Unstable) chance *= 1.4f;
            if (r.SurfaceChip) chance *= 0.25f;
            if (force < 0.35f) chance *= 0.1f;
            else if (force < 0.55f) chance *= 0.45f;
            r.DamageChance = Mathf.Clamp01(chance);
            r.Damaged = rng.Chance(r.DamageChance);
            r.DamageSeverity = r.Damaged ? Mathf.Clamp01(force * rng.Range(0.55f, 1.05f) * (r.Overstrike ? 1.2f : 1f)) : 0f;

            // opening: enough of the ring is cracked, or the shell is simply overwhelmed
            bool ringOpen = cracks >= Mathf.CeilToInt(Sectors * OpenFraction) && (r.NewCrack || wasCracked || cracks >= Sectors - 1);
            // brute force: a sector hammered far past cracking, or a heavy blow on an already stressed shell
            float maxStress = 0f;
            for (int i = 0; i < Sectors; i++) maxStress = Mathf.Max(maxStress, Stress[i]);
            // pounding one spot eventually bursts the shell, but it takes at least as many blows as working the ring
            float burst = (6f + 4f * Mathf.Clamp(Toughness, 0.6f, 1.5f)) * Mathf.Lerp(0.85f, 1.4f, Mathf.InverseLerp(0.035f, 0.165f, Radius));
            bool overwhelmed = !ringOpen && (maxStress >= burst || (force > 0.75f && TotalStress() >= Sectors * 1.1f));
            if (ringOpen || overwhelmed)
            {
                r.Opened = true;
                r.Shattered = overwhelmed && !ringOpen;
                if (r.Shattered) { r.Damaged = true; r.DamageSeverity = Mathf.Max(r.DamageSeverity, 0.8f); }
            }
            r.Quality = Mathf.Clamp01(placement * (0.5f + 0.5f * angle) * (wasCracked ? 0.2f : 1f) * (force > 0.85f ? 0.7f : 1f) * (r.SurfaceChip ? 0.3f : 1f));
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
