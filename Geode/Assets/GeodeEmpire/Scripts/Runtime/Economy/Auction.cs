using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.Economy
{
    /// <summary>
    /// V5 §62: the auction channel, for genuinely high-end material only. A displayed exceptional-or-better piece is
    /// consigned on the tablet; the courier collects it with the next crate delivery; three crates later the hammer
    /// falls at a seeded price around the estimate (a reserve protects the piece: a lot that does not reach it comes
    /// back to the cabinet). The house takes a commission, so the dealer and the showroom stay real alternatives.
    /// </summary>
    public static class Auction
    {
        public const float Commission = 0.12f;
        public const int ResolveAfterCrates = 3;
        public const float ReserveFraction = 0.85f;
        public const QualityTier MinTier = QualityTier.Exceptional;

        public static bool IsEligible(SpecimenRecord r) => r != null && r.IsOpened && r.Appraised && Valuation.TierFromValue(r.EstimatedValue()) >= MinTier;

        public static string CannotConsign(GameState s, SpecimenRecord r)
        {
            if (r == null) return "Nothing to consign";
            if (r.ConsignedAtCrate > 0 || r.Location == SpecimenLocation.Consigned) return "Already consigned";
            if (!r.IsOpened) return "Only opened pieces go to auction";
            if (!r.Appraised) return "Appraise it first: the house wants an estimate";
            if (Valuation.TierFromValue(r.EstimatedValue()) < MinTier) return "The house only takes exceptional pieces";
            if (r.Favorite) return "A favourite: take the star off it first";
            if (r.Location != SpecimenLocation.DisplaySlot && r.Location != SpecimenLocation.SaleSlot) return "Set it in the cabinet or the showroom first";
            if (Reputation.Tier(s) < 2) return "The house takes consignments from known names: sell a little more first";
            return null;
        }

        public static float Estimate(SpecimenRecord r) => Mathf.Round(r.EstimatedValue());

        /// <summary>The hammer multiplier is decided by the world seed and the piece, so a reload changes nothing.</summary>
        public static float HammerMultiplier(GameState s, SpecimenRecord r)
        {
            var rng = new SeededRandom(SeededRandom.Combine(s.WorldSeed, SeededRandom.HashString("auction:" + r.Id)));
            float u = rng.NextFloat();
            float mult = 0.7f + 0.9f * Mathf.Pow(u, 1.4f);               // 0.7 .. 1.6, most lots a little over the estimate
            if (Valuation.TierFromValue(r.EstimatedValue()) >= QualityTier.MuseumGrade) mult += 0.2f;   // the room bids up a museum piece
            if (r.Certified) mult += 0.05f;                                 // a certificate settles nerves
            return mult;
        }

        public static bool Consign(GameSession session, SpecimenRecord r, out string why)
        {
            why = CannotConsign(session.State, r);
            if (why != null) return false;
            r.ConsignedAtCrate = Mathf.Max(1, session.State.CrateCounter);
            GameState.Log(r, "consigned", Estimate(r), "to the auction house");
            session.Notify($"Consigned: the courier collects {r.DisplayName} with the next delivery", NotificationKind.Success);
            session.QueueSave("consign");
            session.RaiseStateChanged();
            return true;
        }

        public static void Withdraw(GameSession session, SpecimenRecord r)
        {
            if (r == null || r.ConsignedAtCrate <= 0) return;
            r.ConsignedAtCrate = 0;
            session.Notify($"Withdrawn: {r.DisplayName} stays", NotificationKind.Info);
            session.QueueSave("withdraw");
            session.RaiseStateChanged();
        }

        /// <summary>Called with every crate delivery: the courier collects consigned pieces, and lots that are due are hammered.</summary>
        public static void OnDelivery(GameSession session)
        {
            var s = session.State;
            foreach (var r in s.Specimens)
            {
                if (r.ConsignedAtCrate <= 0 || r.Location == SpecimenLocation.Consigned || r.Location == SpecimenLocation.Sold) continue;
                bool listed = false; foreach (var l in s.AuctionLots) if (l.SpecimenId == r.Id) listed = true;
                if (listed) { r.ConsignedAtCrate = 0; continue; }
                var e = session.GetEntity(r.Id);
                if (e != null) session.Despawn(e);
                r.Location = SpecimenLocation.Consigned;
                r.LocationIndex = -1;
                r.ConsignedAtCrate = 0;   // collected: from here the lot list is the record of it
                s.AuctionLots.Add(new AuctionLot { SpecimenId = r.Id, CollectedAtCrate = s.CrateCounter, ResolveAtCrate = s.CrateCounter + ResolveAfterCrates, Estimate = Estimate(r), Reserve = Mathf.Round(Estimate(r) * ReserveFraction) });
                GameState.Log(r, "collected", 0f, "the courier took it to the auction house");
                session.Notify($"The courier took {r.DisplayName} to the auction house", NotificationKind.Info);
            }
            for (int i = s.AuctionLots.Count - 1; i >= 0; i--)
            {
                var lot = s.AuctionLots[i];
                if (s.CrateCounter < lot.ResolveAtCrate) continue;
                var r = s.FindSpecimen(lot.SpecimenId);
                s.AuctionLots.RemoveAt(i);
                if (r == null) continue;
                Resolve(session, lot, r);
            }
        }

        private static void Resolve(GameSession session, AuctionLot lot, SpecimenRecord r)
        {
            var s = session.State;
            float hammer = Mathf.Round(lot.Estimate * HammerMultiplier(s, r));
            r.ConsignedAtCrate = 0;
            if (hammer >= lot.Reserve)
            {
                float net = Mathf.Round(hammer * (1f - Commission));
                r.Location = SpecimenLocation.Sold;
                s.Stats.AuctionsSold++; s.Stats.AuctionRevenue += net; s.Stats.SpecimensSold++;
                if (net > s.Stats.BiggestSale) { s.Stats.BiggestSale = net; s.Stats.BiggestSaleName = r.DisplayName; }
                GameState.Log(r, "auctioned", net, $"hammer {UI.UiKit.Money(hammer)}, {Mathf.RoundToInt(Commission * 100f)}% commission");
                session.AddCash(net, "auction");
                s.PendingLetters.Add(new LetterRecord { Title = "Sold at auction", Body = $"\"{r.DisplayName} went under the hammer at {UI.UiKit.Money(hammer)} against an estimate of {UI.UiKit.Money(lot.Estimate)}. The house's {Mathf.RoundToInt(Commission * 100f)}% comes off; {UI.UiKit.Money(net)} is yours.\"\n\n{Describe(hammer, lot.Estimate)}" });
                session.Notify($"Sold at auction: {r.DisplayName} for {UI.UiKit.Money(net)}", NotificationKind.Success);
            }
            else
            {
                s.Stats.AuctionsPassed++;
                Return(session, r);
                GameState.Log(r, "passed", hammer, "did not reach the reserve; returned");
                s.PendingLetters.Add(new LetterRecord { Title = "Passed at auction", Body = $"\"The bidding on {r.DisplayName} stalled at {UI.UiKit.Money(hammer)}, short of the {UI.UiKit.Money(lot.Reserve)} reserve, so it did not sell. The courier has brought it back.\"\n\nA passed lot is not a lost one: the showroom or the dealer will still take it, and the room may be warmer another season." });
                session.Notify($"Passed at auction: {r.DisplayName} is back", NotificationKind.Warning);
            }
            session.RaiseStateChanged();
            session.QueueSave("auction");
        }

        private static string Describe(float hammer, float estimate)
        {
            float k = hammer / Mathf.Max(1f, estimate);
            return k >= 1.35f ? "Two collectors wanted it and neither would stop." : k >= 1.1f ? "A good room: steady bidding past the estimate." : k >= 0.95f ? "It found its price, about where the estimate put it." : "A quiet room; it cleared the reserve and not much more.";
        }

        /// <summary>A passed lot comes back to a free cabinet slot, or to the floor by the receiving pallets.</summary>
        private static void Return(GameSession session, SpecimenRecord r)
        {
            var cabinet = Object.FindAnyObjectByType<DisplayCabinet>();
            if (cabinet != null)
                foreach (var z in cabinet.Slots)
                {
                    if (z == null || !z.IsEmpty || z.Locked || !z.gameObject.activeInHierarchy) continue;
                    r.Location = SpecimenLocation.World;
                    var e = session.Spawn(r, z.transform.position + Vector3.up * 0.2f, Quaternion.identity, false);
                    if (e == null) break;
                    if (z.RefusalReason(e) == null && z.FitRefusal(e) == null) { z.Place(e, true); return; }
                    session.Despawn(e);
                    break;
                }
            var receiving = Object.FindAnyObjectByType<ReceivingArea>();
            Vector3 spot = receiving != null ? receiving.transform.TransformPoint(new Vector3(-1.5f, 0f, 0.9f)) : Vector3.zero;
            r.Location = SpecimenLocation.World;
            var back = session.Spawn(r, spot + Vector3.up * 0.25f, Quaternion.identity, false);
            if (back != null)
            {
                back.ApplyOpenPose();
                back.SetPose(new Vector3(spot.x, back.RestHeightOffset(true), spot.z), Quaternion.identity);
                back.SetStaticCollidable();
                r.WorldPosition = back.transform.position; r.WorldRotation = back.transform.rotation;
            }
        }

        public static string LotLine(GameState s, AuctionLot lot)
        {
            var r = s.FindSpecimen(lot.SpecimenId);
            int left = Mathf.Max(0, lot.ResolveAtCrate - s.CrateCounter);
            return $"{(r != null ? r.DisplayName : lot.SpecimenId)}  •  estimate {UI.UiKit.Money(lot.Estimate)}, reserve {UI.UiKit.Money(lot.Reserve)}  •  {(left == 0 ? "hammer with the next delivery" : left == 1 ? "hammer after one more crate" : $"hammer after {left} more crates")}";
        }
    }
}
