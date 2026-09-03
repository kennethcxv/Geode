using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GeodeEmpire.Save;

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

        /// <summary>Reference display radius (m): a medium hollow geode's cavity.</summary>
        public const float ReferenceDisplayRadius = 0.052f;

        /// <summary>
        /// The size a buyer sees: the cavity (crystal field) of a geode, the banded face of a nodule. A heavy
        /// thick-walled rock with a fist-sized pocket is worth a fist-sized pocket, however much it weighs.
        /// </summary>
        public static float DisplayRadius(SpecimenGeology g)
        {
            float cav = g.Cavity == CavityArchetype.Nodule ? Mathf.Max(g.CavityFraction, 0.55f) : g.CavityFraction;
            return g.Size * cav;
        }

        public static float PristineValue(SpecimenGeology g)
        {
            var fam = g.Family;
            float sizeMult = Mathf.Pow(Mathf.Max(0.01f, DisplayRadius(g)) / ReferenceDisplayRadius, 1.6f);
            float visual = VisualScore(g);
            // quality now sets most of every visible axis, so the curve is steeper than V3's: an ordinary common
            // is a few dollars, a world-class piece a thousand or more
            float value = 0.76f * fam.ValueMult * sizeMult * Mathf.Exp(visual * 6.5f) * FormationFactor(g.Cavity);
            foreach (var t in g.Traits) value *= TraitMultiplier(t);
            if (g.HasSecondary) value *= 1f + 0.25f * g.SecondaryAmount;
            return Mathf.Round(value);
        }

        /// <summary>Words for the 0..1 visible axes, as the appraiser would put them.</summary>
        public static string SaturationWord(float v) => v > 0.8f ? "intense colour" : v > 0.6f ? "strong colour" : v > 0.4f ? "moderate colour" : v > 0.22f ? "pale" : "washed out";
        public static string ClarityWord(float v) => v > 0.8f ? "glassy" : v > 0.6f ? "clear" : v > 0.4f ? "slightly cloudy" : v > 0.22f ? "milky" : "opaque";
        public static string ZoningWord(float v) => v > 0.65f ? "strong colour zoning" : v > 0.35f ? "some zoning" : "even colour";
        public static string HabitWord(SpecimenGeology g)
        {
            float mm = g.Family.Placement == PlacementStyle.Carpet && g.IsDruzy ? 1f : Mathf.Lerp(g.Family.ScaleMin, g.Family.ScaleMax, g.CrystalScale) * g.Size * g.CavityFraction * 1000f;
            string size = g.IsDruzy ? "druzy, a sugar of points" : mm < 5f ? "fine points" : mm < 14f ? "points a finger-width" : mm < 26f ? "large points" : "very large points";
            return $"{ArchetypeWord(g.Family.Archetypes[0])}, {size}";
        }
        private static string ArchetypeWord(CrystalArchetype a)
        {
            // CamelCase enum names read as words: QuartzPoint -> quartz point
            var sb = new StringBuilder();
            foreach (char ch in a.ToString().Replace("Crystal", "")) { if (char.IsUpper(ch) && sb.Length > 0) sb.Append(' '); sb.Append(char.ToLowerInvariant(ch)); }
            return sb.ToString();
        }

        /// <summary>
        /// The appraisal, line by line: every factor that moved the value from the family's baseline, with its
        /// multiplier, so the price on the card is explained by what can be seen on the piece.
        /// </summary>
        public static List<string> Explain(SpecimenRecord r)
        {
            var g = r.Geology;
            var fam = g.Family;
            var lines = new List<string>();
            if (r.IsPiece)
            {
                lines.Add($"Sawn piece of a {fam.Name.ToLowerInvariant()} worth {UiMoney(g.BaseValue)} whole");
                if (g.Cavity == CavityArchetype.Nodule) lines.Add($"Banded face: {Mathf.RoundToInt(r.PieceFaceArea * 100f)}% of the full section" + (r.Piece.IsSlab ? "  •  slab, patterned both sides ×1.15" : ""));
                else if (r.PieceOpening <= 0.02f) lines.Add("Missed the cavity: a lump of rind");
                else lines.Add($"Kept {Mathf.RoundToInt(r.PieceRetained * 100f)}% of the crystal field  •  cavity opened {Mathf.RoundToInt(r.PieceOpening * 100f)}%  •  symmetry {Mathf.RoundToInt(r.PieceSymmetry * 100f)}%");
                if (r.Polish > 0.02f) lines.Add($"Polish {Mathf.RoundToInt(r.Polish * 100f)}%: ×{(g.Cavity == CavityArchetype.Nodule ? 1f + 0.85f * r.Polish : 1f + 0.12f * r.Polish):F2}");
                if (r.CutFaceStep > 0.0008f) lines.Add($"Stepped face ({r.CutFaceStep * 1000f:F0} mm)");
            }
            else
            {
                lines.Add($"{fam.Name}: family baseline ×{fam.ValueMult:F2}");
                float sizeMult = Mathf.Pow(Mathf.Max(0.01f, DisplayRadius(g)) / ReferenceDisplayRadius, 1.6f);
                lines.Add($"{(g.Cavity == CavityArchetype.Nodule ? "Banded section" : "Crystal field")} {DisplayRadius(g) * 200f:F0} cm across: ×{sizeMult:F2}");
                lines.Add($"Quality (points, fill, colour, clarity, symmetry) {Mathf.RoundToInt(VisualScore(g) * 100f)}/100: ×{Mathf.Exp(VisualScore(g) * 6.5f):F1}");
                float f = FormationFactor(g.Cavity);
                if (Mathf.Abs(f - 1f) > 0.01f) lines.Add($"{FormationName(g.Cavity)}: ×{f:F2}");
                foreach (var t in g.Traits) lines.Add($"{TraitName(t)}: ×{TraitMultiplier(t):F2}");
                if (g.HasSecondary) lines.Add($"{g.SecondaryFamily.Name} on {fam.Name.ToLowerInvariant()}: ×{1f + 0.25f * g.SecondaryAmount:F2}");
            }
            float d = Mathf.Clamp01(r.DamageFraction);
            if (d > 0.005f) lines.Add($"Crystal damage {Mathf.RoundToInt(d * 100f)}%: −{Mathf.RoundToInt(d * 85f)}%");
            if (r.ShellDamage > 0.02f) lines.Add($"Shell chipping: −{Mathf.RoundToInt(Mathf.Clamp01(r.ShellDamage) * 25f)}%");
            return lines;
        }

        private static string UiMoney(float v) => "$" + Mathf.RoundToInt(v).ToString("N0");
        public static string FormationName(CavityArchetype c) => c switch
        {
            CavityArchetype.Hollow => "Open cavity", CavityArchetype.ThickWall => "Thick-walled cavity", CavityArchetype.Cathedral => "Cathedral cavity",
            CavityArchetype.Pocket => "Small pocket", CavityArchetype.DoubleChamber => "Double chamber", _ => "Solid nodule",
        };

        /// <summary>Value after damage: damage is a fraction 0..1 of crystal mass lost/broken, plus shell chips.</summary>
        public static float DamagedValue(SpecimenGeology g, float crystalDamageFraction, float shellDamage)
        {
            float d = Mathf.Clamp01(crystalDamageFraction);
            float mult = (1f - d * 0.85f) * (1f - Mathf.Clamp01(shellDamage) * 0.25f);
            return Mathf.Max(1f, Mathf.Round(g.BaseValue * mult));
        }

        /// <summary>
        /// Value of a sawn piece. A crystal geode's half is worth what it kept of the field and how much of the cavity the
        /// face opens; a banded nodule's face is the product itself, and a polish makes it. A cut that missed the cavity
        /// is a lump of rind. Two centre cuts of a geode add up to about one natural split; the saw buys certainty and
        /// two separately saleable pieces, not more value.
        /// </summary>
        public static float PieceValue(SpecimenGeology g, PieceShape piece, float retained, float opening, float symmetry, float faceArea, float polish, float crystalDamageFraction, float shellDamage)
        {
            float basev = g.BaseValue;
            float value;
            bool nodule = g.Cavity == CavityArchetype.Nodule;
            float thin = piece.IsSlab && piece.Thickness < 0.009f ? 0.7f : 1f;   // a wafer chips in the hand
            if (nodule)
            {
                // the banded face: area and finish
                value = basev * (0.2f + 0.6f * Mathf.Clamp01(faceArea)) * (1f + 0.85f * Mathf.Clamp01(polish)) * thin;
                if (piece.IsSlab) value *= 1.15f;   // a slab shows the pattern on both sides
            }
            else if (opening <= 0.02f)
            {
                value = basev * 0.04f * Mathf.Clamp01(faceArea) * (1f + 0.3f * polish) + 1f;   // rind
            }
            else
            {
                value = basev * Mathf.Pow(Mathf.Clamp01(retained), 0.85f) * (0.35f + 0.65f * Mathf.Clamp01(opening)) * (0.8f + 0.2f * Mathf.Clamp01(symmetry)) * (1f + 0.12f * Mathf.Clamp01(polish)) * thin;
            }
            float d = Mathf.Clamp01(crystalDamageFraction);
            value *= (1f - d * 0.85f) * (1f - Mathf.Clamp01(shellDamage) * 0.25f);
            return Mathf.Max(1f, Mathf.Round(value));
        }

        public static string PieceWord(SpecimenGeology g, PieceShape piece, float polish, float opening)
        {
            bool polished = polish > 0.5f;
            if (g.Cavity == CavityArchetype.Nodule) return piece.IsSlab ? (polished ? "Polished Slab" : "Slab") : (polished ? "Polished Slice" : "Sawn Slice");
            if (opening <= 0.02f) return "Rind Cut";
            if (piece.IsSlab) return polished ? "Polished Slab" : "Slab";
            return polished ? "Polished Half" : "Sawn Half";
        }

        /// <summary>"Deep Violet Amethyst Sawn Half", "Banded Agate Polished Slab".</summary>
        public static string PieceName(SpecimenGeology g, PieceShape piece, float polish, float opening)
        {
            var fam = g.Family;
            string colorWord = ColorWord(g);
            string famName = fam.Name;
            if (fam.Id == MineralId.ClearQuartz && !string.IsNullOrEmpty(colorWord)) famName = "Quartz";
            if (!string.IsNullOrEmpty(colorWord) && famName.StartsWith(colorWord, System.StringComparison.OrdinalIgnoreCase)) colorWord = null;
            return (string.IsNullOrEmpty(colorWord) ? "" : colorWord + " ") + famName + " " + PieceWord(g, piece, polish, opening);
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
            if (value < 9f) return QualityTier.Common;
            if (value < 28f) return QualityTier.Decent;
            if (value < 90f) return QualityTier.Good;
            if (value < 320f) return QualityTier.Exceptional;
            if (value < 1000f) return QualityTier.MuseumGrade;
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
            string famName = fam.Name;
            if (fam.Id == MineralId.ClearQuartz && !string.IsNullOrEmpty(colorWord)) famName = "Quartz";
            // "Smoky Smoky Quartz": the family name already carries the word
            if (!string.IsNullOrEmpty(colorWord) && famName.StartsWith(colorWord, System.StringComparison.OrdinalIgnoreCase)) colorWord = null;
            if (!string.IsNullOrEmpty(colorWord)) sb.Append(colorWord).Append(' ');
            sb.Append(famName);
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
                case MineralId.ClearQuartz: return g.Clarity > 0.8f ? "Water-Clear" : palette == "Milky" ? "Milky" : "";
                case MineralId.Fluorite: return palette;
                case MineralId.Agate: return palette == "Cream & Brown" ? "Banded" : palette == "Blue Lace" ? "Blue Lace" : palette == "Carnelian" ? "Carnelian" : "Rose";
                case MineralId.Calcite: return palette == "Iceland" ? "Iceland" : palette == "Peach" ? "Peach" : "Honey";
                case MineralId.Celestite: return strong ? "Sky-Blue" : "Ice-Blue";
                case MineralId.Pyrite: return strong ? "Bright Brass" : "Brassy";
                case MineralId.Aragonite: return palette == "Amber" ? "Amber" : "White";
                case MineralId.Malachite: return palette == "Bright Green" ? "Bright Green" : strong ? "Deep Green" : "Green";
                case MineralId.Selenite: return palette == "Amber" ? "Amber" : g.Clarity > 0.75f ? "Water-Clear" : "Satin";
                case MineralId.Wulfenite: return palette == "Red-Orange" ? "Red" : palette == "Butterscotch" ? "Butterscotch" : "Orange";
                case MineralId.Garnet: return palette == "Spessartine" ? "Orange" : strong ? "Deep Red" : "Red";
                case MineralId.Hematite: return palette == "Specular" ? "Specular" : "Black";
                case MineralId.Tourmaline: return palette == "Verdelite" ? "Green" : palette == "Rubellite" ? "Pink" : "Black";
                case MineralId.Vanadinite: return palette == "Orange" ? "Orange" : strong ? "Blood-Red" : "Red";
                case MineralId.Azurite: return palette == "Electric" || strong ? "Electric Blue" : "Deep Blue";
                case MineralId.Stibnite: return "Steel";
                case MineralId.Rhodochrosite: return palette == "Raspberry" ? "Raspberry" : "Rose";
                case MineralId.Apophyllite: return palette == "Green" ? "Mint" : g.Clarity > 0.75f ? "Water-Clear" : "Pearly";
                case MineralId.Chalcopyrite: return palette == "Peacock" ? "Peacock" : "Brassy";
                case MineralId.Stilbite: return palette == "White" ? "White" : "Salmon";
                case MineralId.Halite: return palette == "Blue" ? "Blue" : palette == "Clear" ? "Clear" : "Pink";
            }
            return "";
        }

        public static string FormWord(SpecimenGeology g)
        {
            if (g.HasTrait(RareTrait.DeepCathedral) || g.Cavity == CavityArchetype.Cathedral) return "Cathedral";
            if (g.HasTrait(RareTrait.DoubleCavity) || g.Cavity == CavityArchetype.DoubleChamber) return "Double Chamber";
            if (g.HasTrait(RareTrait.GiantCenterpiece)) return "Centrepiece";
            if (g.Cavity == CavityArchetype.Nodule) return g.Mineral == MineralId.Malachite ? "Botryoidal Mass" : "Nodule";
            if (g.IsDruzy) return "Druzy Geode";
            if (g.Mineral == MineralId.Aragonite) return "Spray";
            if (g.Mineral == MineralId.Celestite || g.Mineral == MineralId.Fluorite) return "Cluster";
            if (g.Mineral == MineralId.Pyrite || g.Mineral == MineralId.Wulfenite) return "Pocket";
            if (g.Mineral == MineralId.Malachite) return "Crust";
            if (g.Mineral == MineralId.Selenite) return g.CrystalScale > 0.6f ? "Blades" : "Cluster";
            if (g.Mineral == MineralId.Garnet) return "in Matrix";
            if (g.Mineral == MineralId.Hematite) return g.Palette.Name == "Specular" ? "Specularite" : "Kidney Ore";
            if (g.Mineral == MineralId.Tourmaline) return g.CrystalScale > 0.6f ? "Prisms" : "in Pegmatite";
            if (g.Mineral == MineralId.Vanadinite) return "Barrels";
            if (g.Mineral == MineralId.Azurite) return "Rosettes";
            if (g.Mineral == MineralId.Stibnite) return "Sprays";
            if (g.Mineral == MineralId.Rhodochrosite) return g.Cavity == CavityArchetype.Nodule ? "Banded Nodule" : "Crust";
            if (g.Mineral == MineralId.Apophyllite) return "Cluster";
            if (g.Mineral == MineralId.Chalcopyrite) return "in Matrix";
            if (g.Mineral == MineralId.Stilbite) return "Sheaves";
            if (g.Mineral == MineralId.Halite) return "Hoppers";
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
