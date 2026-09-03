using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Economy
{
    /// <summary>
    /// The occasional side of sourcing and selling: lots that turn up now and then (no timers: an offer stays until it
    /// is bought), and buyers who ask for a particular kind of piece and pay a premium for it through the dealer.
    /// Everything is seeded from the world so a career replays the same offers.
    /// </summary>
    public static class Market
    {
        public const int OfferEveryCrates = 4;
        public const int MaxOpenCommissions = 2;

        /// <summary>After a crate is bought: now and then an occasional lot goes on offer.</summary>
        public static string RefreshOffers(GameState state)
        {
            if (state.CrateCounter < 3 || state.CrateCounter - state.LastOfferCrate < OfferEveryCrates) return null;
            var eligible = new List<SupplierDefinition>();
            foreach (var sup in SupplierCatalog.All)
                if (sup.Occasional && state.HasSupplier(sup.Id) && !state.OfferedLots.Contains(sup.Id)) eligible.Add(sup);
            if (eligible.Count == 0) return null;
            var rng = new SeededRandom(SeededRandom.Combine(state.WorldSeed, 9001UL + (ulong)state.CrateCounter));
            var pick = eligible[rng.Range(0, eligible.Count)];
            state.OfferedLots.Add(pick.Id);
            state.LastOfferCrate = state.CrateCounter;
            return pick.Id;
        }

        /// <summary>Is this source on the tablet right now: always for regular sources, only while offered for occasional ones.</summary>
        public static bool Available(GameState state, SupplierDefinition sup) => state.HasSupplier(sup.Id) && (!sup.Occasional || state.OfferedLots.Contains(sup.Id));

        public static void ConsumeOffer(GameState state, string supplierId) { state.OfferedLots.Remove(supplierId); }

        /// <summary>After a sale: every few sales a buyer writes in with a request, up to two open at once.</summary>
        public static Commission RefreshCommissions(GameState state)
        {
            int milestone = state.Stats.SpecimensSold / 4;
            if (milestone <= state.LastCommissionMilestone || state.Stats.SpecimensSold < 6) return null;
            int open = 0; foreach (var c in state.Commissions) if (!c.Fulfilled) open++;
            if (open >= MaxOpenCommissions) return null;
            state.LastCommissionMilestone = milestone;
            var rng = new SeededRandom(SeededRandom.Combine(state.WorldSeed, 7001UL + (ulong)milestone));
            state.CommissionCounter++;
            // what buyers ask for follows what the player has learned to make: families they have opened, polish once the lap exists
            var known = new List<MineralId>();
            foreach (var e in state.Encyclopedia) if (e.Found > 0) known.Add(e.Mineral);
            bool lap = state.HasUpgrade(UpgradeCatalog.PolishLap);
            bool saw = state.HasUpgrade(UpgradeCatalog.TrimSaw);
            var c2 = new Commission { Id = "K" + state.CommissionCounter, CreatedTicks = System.DateTime.UtcNow.Ticks };
            string[] buyers = { "a collector in Tucson", "the museum shop", "a decorator's studio", "a jeweller", "a science teacher", "a gallery in Denver" };
            c2.Buyer = buyers[rng.Range(0, buyers.Length)];
            int kind = rng.Range(0, lap ? 4 : saw ? 3 : 2);
            switch (kind)
            {
                case 0:   // a family, a grade
                    c2.Mineral = known.Count > 0 ? (int)known[rng.Range(0, known.Count)] : -1;
                    c2.MinTier = (int)QualityTier.Good; c2.WantWhole = true; c2.Premium = 1.7f;
                    c2.Note = $"wants a natural {(c2.Mineral >= 0 ? MineralCatalog.Get((MineralId)c2.Mineral).Name.ToLowerInvariant() : "geode")} at least good grade";
                    break;
                case 1:   // size
                    c2.MinMassKg = rng.Chance(0.5f) ? 1.5f : 2.5f; c2.MinTier = (int)QualityTier.Decent; c2.Premium = 1.6f;
                    c2.Note = $"wants a big one: {c2.MinMassKg:F1} kg or more, decent or better";
                    break;
                case 2:   // sawn
                    c2.WantWhole = false; c2.MinTier = (int)QualityTier.Good; c2.Premium = 1.8f;
                    c2.Note = "wants a sawn face, good grade or better, cavity square to the cut";
                    break;
                default:  // polished
                    c2.WantPolished = true; c2.MinTier = (int)QualityTier.Decent; c2.Premium = 2.0f;
                    c2.Note = "wants a polished piece, decent or better";
                    break;
            }
            state.Commissions.Add(c2);
            return c2;
        }

        /// <summary>Does this piece answer the request?</summary>
        public static bool Matches(Commission c, SpecimenRecord r)
        {
            if (c == null || c.Fulfilled || r == null || !r.IsOpened) return false;
            var g = r.Geology;
            if (c.Mineral >= 0 && (int)g.Mineral != c.Mineral) return false;
            if (c.WantWhole && r.IsPiece) return false;
            if (!c.WantWhole && c.MinTier >= (int)QualityTier.Good && !r.IsPiece && !c.WantPolished && c.MinMassKg <= 0f) return false;   // "a sawn face" asks for a piece
            if (c.WantPolished && r.Polish < 0.9f) return false;
            if (c.MinMassKg > 0f && g.MassKg < c.MinMassKg) return false;
            var tier = Valuation.TierFromValue(r.EstimatedValue());
            if ((int)tier < c.MinTier) return false;
            return true;
        }

        /// <summary>The best open request a piece answers, or null.</summary>
        public static Commission Find(GameState state, SpecimenRecord r)
        {
            Commission best = null;
            foreach (var c in state.Commissions) if (Matches(c, r) && (best == null || c.Premium > best.Premium)) best = c;
            return best;
        }

        public static string Describe(Commission c) => $"{char.ToUpper(c.Buyer[0]) + c.Buyer.Substring(1)} {c.Note}  •  pays ×{c.Premium:F1} the dealer price";
    }
}
