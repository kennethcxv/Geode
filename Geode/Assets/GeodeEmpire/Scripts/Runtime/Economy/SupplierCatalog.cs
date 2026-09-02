using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Save;

namespace GeodeEmpire.Economy
{
    /// <summary>A sourcing strategy, not a tier: price, count, and how outcomes are distributed.</summary>
    public sealed class SupplierDefinition
    {
        public string Id;
        public string Name;
        public string Tagline;
        public string Description;
        public float Price;
        public int MinRocks, MaxRocks;
        public bool CountHidden;
        public float[] TierWeights;       // Common, Decent, Good, Exceptional, Museum, WorldClass
        public string UnlockHint;
        public Color Accent;

        public string RockCountLabel => CountHidden ? "6–12 rocks (unsorted)" : MinRocks == MaxRocks ? $"{MinRocks} rocks" : $"{MinRocks}–{MaxRocks} rocks";
    }

    public static class SupplierCatalog
    {
        public const string Local = "local";
        public const string Regional = "regional";
        public const string Estate = "estate";
        public const string Premium = "premium";

        public static readonly SupplierDefinition[] All =
        {
            new SupplierDefinition
            {
                Id = Local, Name = "Local Quarry Mixed Crate", Tagline = "Cheap volume. Mostly ordinary. Now and then, not.",
                Description = "Unsorted geodes straight off the quarry sorting belt. Great hammer practice and the occasional strange local outlier.",
                Price = 75f, MinRocks = 9, MaxRocks = 10, CountHidden = false,
                TierWeights = new[] { 0.62f, 0.24f, 0.10f, 0.03f, 0.008f, 0.002f },
                UnlockHint = "", Accent = new Color(0.75f, 0.62f, 0.42f),
            },
            new SupplierDefinition
            {
                Id = Regional, Name = "Regional Curated Crate", Tagline = "Hand-picked. Fewer duds, fewer miracles.",
                Description = "A regional dealer pre-sorts these by weight and sound. A better floor and steadier value, but the wild outliers were skimmed off already.",
                Price = 190f, MinRocks = 8, MaxRocks = 8, CountHidden = false,
                TierWeights = new[] { 0.30f, 0.40f, 0.22f, 0.065f, 0.013f, 0.002f },
                UnlockHint = "Unlocks after your first sale to the dealer.", Accent = new Color(0.45f, 0.65f, 0.55f),
            },
            new SupplierDefinition
            {
                Id = Estate, Name = "Estate Mystery Lot", Tagline = "Somebody's collection, boxed up unsorted. Could be anything.",
                Description = "Unknown provenance, unknown count. Estate lots can be a box of gravel or the best week of your career.",
                Price = 260f, MinRocks = 6, MaxRocks = 12, CountHidden = true,
                TierWeights = new[] { 0.50f, 0.20f, 0.15f, 0.10f, 0.035f, 0.015f },
                UnlockHint = "Unlocks once you have a specimen on display.", Accent = new Color(0.62f, 0.45f, 0.72f),
            },
            new SupplierDefinition
            {
                Id = Premium, Name = "Premium Dealer Crate", Tagline = "Invitation only. Display-grade floor.",
                Description = "A dealer who only sells to serious collectors. Expensive, reliable, beautiful, and rarely the biggest upside.",
                Price = 650f, MinRocks = 6, MaxRocks = 6, CountHidden = false,
                TierWeights = new[] { 0.05f, 0.30f, 0.42f, 0.18f, 0.045f, 0.005f },
                UnlockHint = "Invitation arrives when your displayed collection is worth $1,500.", Accent = new Color(0.85f, 0.7f, 0.35f),
            },
        };

        public static SupplierDefinition Get(string id)
        {
            foreach (var s in All) if (s.Id == id) return s;
            return null;
        }

        /// <summary>Evaluate unlock rules against the career state; returns newly unlocked supplier ids.</summary>
        public static List<string> EvaluateUnlocks(GameState state)
        {
            var newly = new List<string>();
            if (!state.HasSupplier(Local)) { state.UnlockedSuppliers.Add(Local); }
            if (!state.HasSupplier(Regional) && state.Stats.SpecimensSold > 0) { state.UnlockedSuppliers.Add(Regional); newly.Add(Regional); }
            if (!state.HasSupplier(Estate) && state.DisplayedCount() > 0) { state.UnlockedSuppliers.Add(Estate); newly.Add(Estate); }
            if (!state.HasSupplier(Premium) && state.CollectionValue() >= 1500f) { state.UnlockedSuppliers.Add(Premium); newly.Add(Premium); }
            return newly;
        }
    }
}
