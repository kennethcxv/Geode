using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Economy
{
    /// <summary>Curated goals the collection can work toward: each is a question of the specimens on display, answered from the save.</summary>
    public static class CollectionGoals
    {
        public sealed class Goal
        {
            public string Id, Title, Detail;
            public System.Func<GameState, (int have, int need)> Progress;
            public bool Done(GameState s) { var p = Progress(s); return p.have >= p.need; }
        }

        private static List<SpecimenRecord> Displayed(GameState s)
        {
            var list = new List<SpecimenRecord>();
            foreach (var r in s.Specimens) if (r.Location == SpecimenLocation.DisplaySlot) list.Add(r);
            return list;
        }

        public static readonly Goal[] All =
        {
            new Goal { Id = "families6", Title = "Six families on display", Detail = "Six different mineral families in the cabinet at once.",
                Progress = s => { var fams = new HashSet<MineralId>(); foreach (var r in Displayed(s)) fams.Add(r.Geology.Mineral); return (fams.Count, 6); } },
            new Goal { Id = "cathedral", Title = "A cathedral", Detail = "A cathedral-cavity geode on display.",
                Progress = s => { int n = 0; foreach (var r in Displayed(s)) if (r.Geology.Cavity == CavityArchetype.Cathedral) n++; return (Mathf.Min(n, 1), 1); } },
            new Goal { Id = "polished", Title = "A polished face", Detail = "A polished sawn piece on display.",
                Progress = s => { int n = 0; foreach (var r in Displayed(s)) if (r.IsPiece && r.Polish > 0.9f) n++; return (Mathf.Min(n, 1), 1); } },
            new Goal { Id = "big", Title = "Something heavy", Detail = "A specimen over 2.5 kg on display.",
                Progress = s => { int n = 0; foreach (var r in Displayed(s)) if (r.Geology.MassKg >= 2.5f) n++; return (Mathf.Min(n, 1), 1); } },
            new Goal { Id = "exceptional", Title = "Exceptional or better", Detail = "A specimen appraised at exceptional grade or above, kept.",
                Progress = s => { int n = 0; foreach (var r in Displayed(s)) if (Valuation.TierFromValue(r.EstimatedValue()) >= QualityTier.Exceptional) n++; return (Mathf.Min(n, 1), 1); } },
            new Goal { Id = "traits3", Title = "Three rare traits", Detail = "Three different rare traits across the display.",
                Progress = s => { var t = new HashSet<RareTrait>(); foreach (var r in Displayed(s)) foreach (var tr in r.Geology.Traits) t.Add(tr); return (t.Count, 3); } },
            new Goal { Id = "sources4", Title = "Four sources", Detail = "Displayed pieces from four different suppliers.",
                Progress = s => { var sup = new HashSet<string>(); foreach (var r in Displayed(s)) if (!string.IsNullOrEmpty(r.SupplierId)) sup.Add(r.SupplierId); return (sup.Count, 4); } },
            new Goal { Id = "clean5", Title = "Five clean opens on show", Detail = "Five displayed specimens with no crystal damage.",
                Progress = s => { int n = 0; foreach (var r in Displayed(s)) if (r.DamageFraction < 0.005f && r.IsOpened) n++; return (n, 5); } },
            new Goal { Id = "value5000", Title = "A $5,000 collection", Detail = "The display cabinet worth $5,000 together.",
                Progress = s => (Mathf.Min(Mathf.RoundToInt(s.CollectionValue()), 5000), 5000) },
            new Goal { Id = "museum", Title = "A museum piece", Detail = "A museum-grade specimen, kept.",
                Progress = s => { int n = 0; foreach (var r in Displayed(s)) if (r.Geology.Tier >= QualityTier.MuseumGrade) n++; return (Mathf.Min(n, 1), 1); } },
        };

        public static int DoneCount(GameState s) { int n = 0; foreach (var g in All) if (g.Done(s)) n++; return n; }

        /// <summary>The nearest unmet goal, as a hint after a rock: what the collection is missing.</summary>
        public static string NearestGap(GameState s)
        {
            Goal best = null; float bestFrac = -1f;
            foreach (var g in All)
            {
                if (g.Done(s)) continue;
                var p = g.Progress(s);
                float frac = p.need > 0 ? p.have / (float)p.need : 0f;
                if (frac > bestFrac) { bestFrac = frac; best = g; }
            }
            return best != null ? best.Title.ToLowerInvariant() : "";
        }
    }
}
