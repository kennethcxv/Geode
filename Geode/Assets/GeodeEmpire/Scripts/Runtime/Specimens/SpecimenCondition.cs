using System;
using UnityEngine;

namespace GeodeEmpire.Specimens
{
    /// <summary>Per-crystal damage level.</summary>
    public static class CrystalDamage
    {
        public const byte Intact = 0;
        public const byte Chipped = 1;
        public const byte Broken = 2;
        public const byte Missing = 3;
    }

    /// <summary>
    /// The visual/career condition of a specimen: separate from geology so reloads never reroll
    /// the interior but do restore exactly what the player did to it.
    /// </summary>
    [Serializable]
    public sealed class SpecimenCondition
    {
        public bool Opened;
        public byte[] CrystalDamage = Array.Empty<byte>();
        public float ShellChipping;   // 0..1 exterior/rim chipping
        /// <summary>
        /// How much of the clay coating has been scrubbed off, averaged over the whole shell (0 = as delivered,
        /// 1 = clean). Kept as the summary figure everything outside cleaning reads; the truth is in
        /// <see cref="RegionClean"/>, and this is recomputed from it.
        /// </summary>
        public float Cleaned;

        /// <summary>
        /// How clean each patch of shell is, 0..255 per region (see <see cref="SpecimenSurface"/>). Empty on a
        /// rock that has never been touched, and on a save written before cleaning was spatial — in both cases
        /// <see cref="Cleaned"/> stands in for the lot, so an old career's rocks are exactly as clean as they were.
        /// </summary>
        public byte[] RegionClean = Array.Empty<byte>();

        /// <summary>How far the player has got with each clue on the shell, indexed as this rock's clue list.</summary>
        public byte[] ClueState = Array.Empty<byte>();
        /// <summary>An opened rock's interior is dusty from the break until it is rinsed in the tub; sawn pieces come off the blade rinsed.</summary>
        public bool Rinsed;

        public byte DamageAt(int crystalIndex)
        {
            if (CrystalDamage == null || crystalIndex < 0 || crystalIndex >= CrystalDamage.Length) return 0;
            return CrystalDamage[crystalIndex];
        }

        /// <summary>Cleanliness of one region, falling back to the whole-rock figure for an untouched or migrated rock.</summary>
        public float CleanAt(int region)
        {
            if (RegionClean == null || RegionClean.Length == 0) return Cleaned;
            if (region < 0 || region >= RegionClean.Length) return Cleaned;
            return RegionClean[region] / 255f;
        }

        /// <summary>Start tracking cleanliness per patch, seeded from wherever the rock had got to as a whole.</summary>
        public void EnsureRegions()
        {
            if (RegionClean != null && RegionClean.Length == SpecimenSurface.Regions) return;
            var arr = new byte[SpecimenSurface.Regions];
            byte from = (byte)Mathf.RoundToInt(Mathf.Clamp01(Cleaned) * 255f);
            for (int i = 0; i < arr.Length; i++) arr[i] = i < (RegionClean?.Length ?? 0) ? RegionClean[i] : from;
            RegionClean = arr;
        }

        public void SetCleanAt(int region, float value)
        {
            EnsureRegions();
            if (region < 0 || region >= RegionClean.Length) return;
            RegionClean[region] = (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
            float sum = 0f;
            for (int i = 0; i < RegionClean.Length; i++) sum += RegionClean[i];
            Cleaned = sum / (255f * RegionClean.Length);
        }

        /// <summary>The dirtiest patch left, and how dirty it is: what "you have missed a bit" means.</summary>
        public (int region, float dirt) DirtiestRegion()
        {
            if (RegionClean == null || RegionClean.Length == 0) return (-1, 1f - Mathf.Clamp01(Cleaned));
            int worst = 0;
            for (int i = 1; i < RegionClean.Length; i++) if (RegionClean[i] < RegionClean[worst]) worst = i;
            return (worst, 1f - RegionClean[worst] / 255f);
        }

        public GeodeEmpire.Specimens.ClueState ClueAt(int index)
        {
            if (ClueState == null || index < 0 || index >= ClueState.Length) return Specimens.ClueState.Undiscovered;
            return (ClueState)ClueState[index];
        }

        public void SetClue(int index, ClueState state)
        {
            if (index < 0) return;
            if (ClueState == null || ClueState.Length <= index)
            {
                var arr = new byte[index + 1];
                if (ClueState != null) Array.Copy(ClueState, arr, ClueState.Length);
                ClueState = arr;
            }
            if (ClueState[index] < (byte)state) ClueState[index] = (byte)state;
        }

        public void EnsureSize(int count)
        {
            if (CrystalDamage == null || CrystalDamage.Length < count)
            {
                var arr = new byte[count];
                if (CrystalDamage != null) Array.Copy(CrystalDamage, arr, CrystalDamage.Length);
                CrystalDamage = arr;
            }
        }

        public SpecimenCondition Clone()
        {
            return new SpecimenCondition
            {
                Opened = Opened,
                CrystalDamage = CrystalDamage == null ? Array.Empty<byte>() : (byte[])CrystalDamage.Clone(),
                ShellChipping = ShellChipping,
                Cleaned = Cleaned,
                RegionClean = RegionClean == null ? Array.Empty<byte>() : (byte[])RegionClean.Clone(),
                ClueState = ClueState == null ? Array.Empty<byte>() : (byte[])ClueState.Clone(),
                Rinsed = Rinsed,
            };
        }
    }
}
