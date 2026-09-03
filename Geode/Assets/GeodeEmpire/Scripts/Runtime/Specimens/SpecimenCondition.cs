using System;

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
        /// <summary>How much of the clay coating has been scrubbed off (0 = as delivered, 1 = clean).</summary>
        public float Cleaned;

        public byte DamageAt(int crystalIndex)
        {
            if (CrystalDamage == null || crystalIndex < 0 || crystalIndex >= CrystalDamage.Length) return 0;
            return CrystalDamage[crystalIndex];
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
            };
        }
    }
}
