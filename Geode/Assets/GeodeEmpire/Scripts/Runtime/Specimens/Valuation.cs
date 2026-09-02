using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GeodeEmpire.Specimens
{
    /// <summary>
    /// Value emerges from visible properties. Tiers are descriptions of the result, not inputs.
    /// </summary>
    public static class Valuation
    {
        public const float ReferenceMassKg = 2.2f;

        public static float FormationFactor(CavityArchetype c) => c switch
        {
            CavityArchetype.Hollow => 1.0f,
            CavityArchetype.ThickWall => 0.85f,
            CavityArchetype.Cathedral => 1.35f,
            CavityArchetype.Pocket => 0.8f,
            CavityArchetype.DoubleChamber => 1.25f,
            _ => 0.9f,
        };

        public static float TraitMultiplier(RareTrait t) => t switch
        {
            RareTrait.GiantCenterpiece => 1.9f,
            RareTrait.DeepCathedral => 1.7f,
            RareTrait.DoubleCavity => 1.5f,
            RareTrait.DenseDruzy => 1.35f,
            RareTrait.ColorZoning => 1.45f,
            RareTrait.HighClarity => 1.5f,
            RareTrait.SecondaryContrast => 1.4f,
            RareTrait.CrystalOnCrystal => 1.6f,
            RareTrait.Phantom => 1.8f,
            RareTrait.PerfectSymmetry => 1.5f,
            RareTrait.MetallicContrast => 1.4f,
            RareTrait.GiantCrystalField => 1.7f,
            _ => 1f,
        };

        /// <summary>0..1 aggregate of the visible quality axes.</summary>
        public static float VisualScore(SpecimenGeology g)
        {
            float scale = g.IsDruzy ? 0.45f + 0.3f * g.CrystalDensity : g.CrystalScale;
            return Mathf.Clamp01(0.30f * scale + 0.18f * g.CrystalDensity + 0.27f * g.Saturation + 0.15f * g.Clarity + 0.10f * g.Symmetry);
        }

        public static float PristineValue(SpecimenGeology g)
        {
            var fam = g.Family;
            float sizeMult = Mathf.Pow(Mathf.Max(0.05f, g.MassKg) / ReferenceMassKg, 0.6f);
            float visual = VisualScore(g);
            float value = 0.45f * fam.ValueMult * sizeMult * Mathf.Exp(visual * 6.2f) * FormationFactor(g.Cavity);
            foreach (var t in g.Traits) value *= TraitMultiplier(t);
            if (g.HasSecondary) value *= 1f + 0.25f * g.SecondaryAmount;
            return Mathf.Round(value);
        }

        /// <summary>Value after damage: damage is a fraction 0..1 of crystal mass lost/broken, plus shell chips.</summary>
        public static float DamagedValue(SpecimenGeology g, float crystalDamageFraction, float shellDamage)
        {
            float d = Mathf.Clamp01(crystalDamageFraction);
            float mult = (1f - d * 0.85f) * (1f - Mathf.Clamp01(shellDamage) * 0.25f);
            return Mathf.Max(1f, Mathf.Round(g.BaseValue * mult));
        }

        public static string TierLabel(QualityTier t) => t switch
        {
            QualityTier.Common => "Common",
            QualityTier.Decent => "Uncommon",
            QualityTier.Good => "Rare",
            QualityTier.Exceptional => "Exceptional",
            QualityTier.MuseumGrade => "Museum Grade",
            _ => "World Class",
        };

        /// <summary>Tier as perceived by the dealer, derived from value relative to the mineral baseline.</summary>
        public static QualityTier TierFromValue(float value)
        {
            if (value < 7f) return QualityTier.Common;
            if (value < 18f) return QualityTier.Decent;
            if (value < 60f) return QualityTier.Good;
            if (value < 240f) return QualityTier.Exceptional;
            if (value < 850f) return QualityTier.MuseumGrade;
            return QualityTier.WorldClass;
        }

        public static string TraitName(RareTrait t) => t switch
        {
            RareTrait.GiantCenterpiece => "Giant centrepiece crystal",
            RareTrait.DeepCathedral => "Cathedral cavity",
            RareTrait.DoubleCavity => "Double chamber",
            RareTrait.DenseDruzy => "Dense druzy carpet",
            RareTrait.ColorZoning => "Colour zoning",
            RareTrait.HighClarity => "Exceptional clarity",
            RareTrait.SecondaryContrast => "Secondary mineral contrast",
            RareTrait.CrystalOnCrystal => "Crystals on crystals",
            RareTrait.Phantom => "Phantom growth",
            RareTrait.PerfectSymmetry => "Remarkable symmetry",
            RareTrait.MetallicContrast => "Metallic contrast",
            RareTrait.GiantCrystalField => "Giant crystal field",
            _ => "",
        };

        /// <summary>Descriptive procedural name from visible properties, e.g. "Deep Violet Amethyst Cathedral".</summary>
        public static string DescriptiveName(SpecimenGeology g)
        {
            var fam = g.Family;
            var sb = new StringBuilder();
            string colorWord = ColorWord(g);
            if (!string.IsNullOrEmpty(colorWord)) sb.Append(colorWord).Append(' ');
            sb.Append(fam.Name);
            sb.Append(' ').Append(FormWord(g));
            return sb.ToString();
        }

        public static string ColorWord(SpecimenGeology g)
        {
            bool strong = g.Saturation > 0.72f;
            bool pale = g.Saturation < 0.35f;
            string palette = g.Palette.Name;
            switch (g.Mineral)
            {
                case MineralId.Amethyst: return strong ? "Deep Violet" : pale ? "Pale Lilac" : "Violet";
                case MineralId.Citrine: return strong ? "Golden" : pale ? "Pale Lemon" : "Honey";
                case MineralId.SmokyQuartz: return palette == "Morion" ? "Black" : strong ? "Dark Smoky" : "Smoky";
                case MineralId.ClearQuartz: return g.Clarity > 0.8f ? "Water-Clear" : palette == "Milky" ? "Milky" : "Clear";
                case MineralId.Fluorite: return palette;
                case MineralId.Agate: return palette == "Cream & Brown" ? "Banded" : palette == "Blue Lace" ? "Blue Lace" : palette == "Carnelian" ? "Carnelian" : "Rose";
                case MineralId.Calcite: return palette == "Iceland" ? "Iceland" : palette == "Peach" ? "Peach" : "Honey";
                case MineralId.Celestite: return strong ? "Sky-Blue" : "Ice-Blue";
                case MineralId.Pyrite: return strong ? "Bright Brass" : "Brassy";
                case MineralId.Aragonite: return palette == "Amber" ? "Amber" : "White";
            }
            return "";
        }

        public static string FormWord(SpecimenGeology g)
        {
            if (g.HasTrait(RareTrait.DeepCathedral) || g.Cavity == CavityArchetype.Cathedral) return "Cathedral";
            if (g.HasTrait(RareTrait.DoubleCavity) || g.Cavity == CavityArchetype.DoubleChamber) return "Double Chamber";
            if (g.HasTrait(RareTrait.GiantCenterpiece)) return "Centrepiece";
            if (g.Cavity == CavityArchetype.Nodule) return "Nodule";
            if (g.IsDruzy) return "Druzy Geode";
            if (g.Mineral == MineralId.Aragonite) return "Spray";
            if (g.Mineral == MineralId.Celestite || g.Mineral == MineralId.Fluorite) return "Cluster";
            if (g.Mineral == MineralId.Pyrite) return "Pocket";
            if (g.Cavity == CavityArchetype.Pocket) return "Pocket";
            return "Geode";
        }

        /// <summary>Short visible-trait bullets for the appraisal card.</summary>
        public static List<string> Highlights(SpecimenGeology g)
        {
            var list = new List<string>();
            if (g.IsDruzy) list.Add("Druzy carpet");
            else if (g.CrystalScale > 0.7f) list.Add("Large crystals");
            else if (g.CrystalScale > 0.45f) list.Add("Medium crystals");
            else list.Add("Small crystals");
            if (g.CrystalDensity > 0.75f) list.Add("Dense growth");
            else if (g.CrystalDensity < 0.35f) list.Add("Sparse growth");
            if (g.Saturation > 0.72f) list.Add("Strong colour");
            else if (g.Saturation < 0.3f) list.Add("Washed-out colour");
            if (g.Clarity > 0.8f && g.Family.Translucency > 0.45f) list.Add("High clarity");
            if (g.Cavity == CavityArchetype.Cathedral) list.Add("Cathedral cavity");
            if (g.Cavity == CavityArchetype.DoubleChamber) list.Add("Double chamber");
            if (g.Cavity == CavityArchetype.Nodule) list.Add("Solid nodule");
            if (g.HasSecondary) list.Add(MineralCatalog.Get(g.Secondary).Name + " inclusions");
            foreach (var t in g.Traits)
            {
                var n = TraitName(t);
                if (!string.IsNullOrEmpty(n) && !list.Contains(n)) list.Add(n);
            }
            return list;
        }
    }
}
