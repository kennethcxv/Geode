using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.Economy
{
    /// <summary>
    /// The career's conclusion: a curated exhibition of the workshop's best pieces on the gallery plinths, opened when
    /// the career has proved itself across sourcing, natural and lapidary work, size, the collection, the workshop and
    /// the trade's regard. The save carries on afterwards; the exhibition can be held again with better pieces.
    /// </summary>
    public static class Exhibition
    {
        public sealed class Axis { public string Title; public bool Met; public string Detail; }

        public static List<Axis> Axes(GameState s)
        {
            var axes = new List<Axis>();
            var displayed = new List<SpecimenRecord>();
            foreach (var r in s.Specimens) if (r.Location == SpecimenLocation.DisplaySlot) displayed.Add(r);
            var sources = new HashSet<string>();
            foreach (var r in s.Specimens) if (!string.IsNullOrEmpty(r.SupplierId)) sources.Add(r.SupplierId);
            bool natural = false, lapidary = false, large = false;
            foreach (var r in displayed)
            {
                if (!r.IsPiece && Valuation.TierFromValue(r.EstimatedValue()) >= QualityTier.Exceptional) natural = true;
                if (r.IsPiece && r.Polish >= 0.9f) lapidary = true;
                if (r.Geology.MassKg >= 2.5f) large = true;
            }
            axes.Add(new Axis { Title = "Sourcing", Met = sources.Count >= 6, Detail = $"rock from {sources.Count} of 6 sources" });
            axes.Add(new Axis { Title = "Natural specimen", Met = natural, Detail = natural ? "an exceptional natural split on display" : "an exceptional natural split on display: not yet" });
            axes.Add(new Axis { Title = "Saw and polish", Met = lapidary, Detail = lapidary ? "a polished piece on display" : "a polished piece on display: not yet" });
            axes.Add(new Axis { Title = "Large specimen", Met = large, Detail = large ? "a piece over 2.5 kg on display" : "a piece over 2.5 kg on display: not yet" });
            axes.Add(new Axis { Title = "Collection", Met = CollectionGoals.DoneCount(s) >= 6, Detail = $"{CollectionGoals.DoneCount(s)} of 10 collection goals (6 needed)" });
            axes.Add(new Axis { Title = "Workshop", Met = s.WorkshopStage >= 3, Detail = s.WorkshopStage >= 3 ? "Stage 3 specialist workshop" : "Stage 3 workshop: not yet" });
            axes.Add(new Axis { Title = "Reputation", Met = Reputation.Tier(s) >= 4, Detail = $"{Reputation.Word(s)} (sought after needed)" });
            return axes;
        }

        public static bool Eligible(GameState s) { foreach (var a in Axes(s)) if (!a.Met) return false; return true; }

        /// <summary>The pieces standing on the gallery plinths (the last three display slots at Stage 3).</summary>
        public static List<SpecimenRecord> OnPlinths(GameState s, int firstPlinthSlot)
        {
            var list = new List<SpecimenRecord>();
            foreach (var r in s.Specimens) if (r.Location == SpecimenLocation.DisplaySlot && r.LocationIndex >= firstPlinthSlot) list.Add(r);
            list.Sort((a, b) => a.LocationIndex.CompareTo(b.LocationIndex));
            return list;
        }

        public static string Summary(GameState s)
        {
            var st = s.Stats;
            float hours = st.PlayTimeSeconds / 3600f;
            return $"{st.CratesPurchased} crates  •  {st.SpecimensOpened} rocks opened ({st.CleanOpens} clean)  •  {st.SawCuts} saw cuts, {st.PiecesPolished} polished  •  {st.RocksCracked} split on the cracker\n" +
                   $"{st.SpecimensSold} pieces sold, {st.CustomersServed} customers served, {st.CommissionsFilled} requests filled  •  biggest sale {UI.UiKit.Money(st.BiggestSale)} ({st.BiggestSaleName})\n" +
                   $"{s.Encyclopedia.Count} families found  •  collection worth {UI.UiKit.Money(s.CollectionValue())}  •  {Reputation.Word(s).ToLowerInvariant()} in the trade  •  {hours:F1} hours";
        }
    }
}
