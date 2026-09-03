using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Workshop
{    /// <summary>The single sell channel: drop opened specimens in the outbox, call the dealer.</summary>
    public sealed class SellOutbox : MonoBehaviour
    {
        public PlacementZone Tray;
        public event Action<float, int> Shipped;

        private void Awake()
        {
            if (Tray != null) Tray.Placed += (z, e) =>
            {
                WorkshopAudio.Play("rock_place", e.transform.position, 0.6f);
                Tutorial.Notify("specimen_sorted");
            };
        }

        public int Count => Tray != null ? Tray.Occupants.Count : 0;

        public float EstimateTotal()
        {
            float total = 0f;
            foreach (var e in Tray.Occupants) total += SaleValue(e);
            return total;
        }

        /// <summary>What the dealer pays, or what a buyer with a standing request pays for a piece that answers it.</summary>
        public static float SaleValueWithCommissions(GameState state, SpecimenEntity e, out Commission filled)
        {
            filled = Market.Find(state, e.Record);
            float v = SaleValue(e);
            return filled != null ? Mathf.Round(v * filled.Premium) : v;
        }

        public static float SaleValue(SpecimenEntity e)
        {
            var s = GameSession.Instance;
            float v = e.Record.EstimatedValue();
            if (e.Record.Appraised && UpgradeCatalog.Has(s.State, UpgradeCatalog.CalibratedScale)) v *= 1.05f;
            int prestige = s.State.Prestige;
            v *= 1f + 0.02f * prestige;
            return Mathf.Round(v);
        }

        public void Ship()
        {
            var session = GameSession.Instance;
            if (Tray == null || Tray.Occupants.Count == 0) return;
            var items = new List<SpecimenEntity>(Tray.Occupants);
            float total = 0f;
            foreach (var e in items)
            {
                if (e.Record.Location == SpecimenLocation.Sold) continue; // no double sales
                float v = SaleValueWithCommissions(session.State, e, out var filled);
                total += v;
                e.Record.Location = SpecimenLocation.Sold;
                if (filled != null)
                {
                    filled.Fulfilled = true;
                    session.State.Stats.CommissionsFilled++;
                    session.State.Stats.CommissionRevenue += v;
                    GameState.Log(e.Record, "commission", v, "to " + filled.Buyer);
                    session.Notify($"{char.ToUpper(filled.Buyer[0]) + filled.Buyer.Substring(1)} took {e.Record.DisplayName} for {UI.UiKit.Money(v)}", NotificationKind.Discovery);
                }
                else GameState.Log(e.Record, "dealer", v, "sold to the dealer");
                session.State.Stats.SpecimensSold++;
                if (v > session.State.Stats.BiggestSale) { session.State.Stats.BiggestSale = v; session.State.Stats.BiggestSaleName = e.Record.DisplayName; }
                session.Despawn(e);
            }
            Tray.Occupants.Clear();
            session.AddCash(total, "sale");
            WorkshopAudio.Play2D("ui_sell", 0.8f);
            session.Notify($"Sold {items.Count} piece{(items.Count == 1 ? "" : "s")} for {UI.UiKit.Money(total)}", NotificationKind.Success);
            Tutorial.Notify("shipped");
            foreach (var id in SupplierCatalog.EvaluateUnlocks(session.State))
                session.Notify($"New supplier available: {SupplierCatalog.Get(id).Name}", NotificationKind.Discovery);
            var ask = Market.RefreshCommissions(session.State);
            if (ask != null) session.Notify("A buyer wrote in: " + Market.Describe(ask), NotificationKind.Discovery);
            session.RaiseStateChanged();
            session.CheckSolvency();
            session.FlushSave("sold");
            Shipped?.Invoke(total, items.Count);
        }
    }
}
