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
{    /// <summary>Scarce physical display: slots become real trophies, and prestige feeds progression.</summary>
    public sealed class DisplayCabinet : MonoBehaviour
    {
        public List<PlacementZone> Slots = new List<PlacementZone>();
        /// <summary>Per slot: 0 base cabinet, 1 shelf expansion, 2 trophy wall (Stage 2), 3 gallery plinth (Stage 3). Filled by the scene builder.</summary>
        public List<int> SlotTiers = new List<int>();
        public List<string> SlotLabels = new List<string>();
        public Font LabelFont;
        public Material LabelMaterial;   // depth-tested world text; falls back to the font material
        private readonly Dictionary<PlacementZone, UI.WorldLabel> _labels = new Dictionary<PlacementZone, UI.WorldLabel>();

        private void Awake()
        {
            foreach (var s in Slots)
            {
                s.Placed += OnPlaced;
                s.Taken += OnTaken;
            }
        }

        private void Start()
        {
            var session = GameSession.Instance;
            if (session != null)
            {
                session.Loaded += RefreshCapacity;
                session.StateChanged += RefreshCapacity;
                if (session.State != null) RefreshCapacity();
            }
        }

        public void RefreshCapacity()
        {
            var s = GameSession.Instance.State;
            int cap = s != null ? s.DisplayCapacity : 8;
            bool byTier = SlotTiers.Count == Slots.Count;
            for (int i = 0; i < Slots.Count; i++)
            {
                // each group unlocks with its own purchase: the top shelf with the expansion, the trophy wall with Stage 2, the plinths with Stage 3
                bool locked; string hint;
                int tier = byTier ? SlotTiers[i] : 0;
                if (byTier && s != null)
                {
                    locked = tier == 1 ? !s.HasUpgrade(Economy.UpgradeCatalog.DisplayExpansion) : tier == 2 ? s.WorkshopStage < 2 : tier == 3 ? s.WorkshopStage < 3 : false;
                    hint = tier == 1 ? "Locked shelf: buy the Cabinet Shelf Expansion" : tier == 2 ? "The trophy wall comes with the Stage 2 workshop" : tier == 3 ? "The gallery opens with the Stage 3 workshop" : null;
                }
                else { locked = i >= cap; hint = null; }
                Slots[i].Locked = locked && Slots[i].IsEmpty;
                Slots[i].LockedHint = hint;
                string label = SlotLabels.Count == Slots.Count && !string.IsNullOrEmpty(SlotLabels[i]) ? SlotLabels[i] : $"display slot {i + 1}";
                Slots[i].DisplayLabel = locked ? (tier == 3 ? "locked plinth" : tier == 2 ? "locked board" : "locked shelf") : label;
                UpdateLabel(Slots[i]);
            }
        }

        private void OnPlaced(PlacementZone z, SpecimenEntity e)
        {
            var session = GameSession.Instance;
            WorkshopAudio.Play("crystal_chime", e.transform.position, 0.5f);
            session.State.Stats.SpecimensKept = session.State.DisplayedCount();
            float v = e.Record.EstimatedValue();
            if (v > session.State.Stats.HighestValueKept) { session.State.Stats.HighestValueKept = v; session.State.Stats.HighestValueKeptName = e.Record.DisplayName; }
            GameState.Log(e.Record, "displayed", v, z.DisplayLabel);
            RecomputePrestige(session.State);
            Tutorial.Notify("specimen_sorted");
            foreach (var id in SupplierCatalog.EvaluateUnlocks(session.State))
                session.Notify($"New supplier available: {SupplierCatalog.Get(id).Name}", NotificationKind.Discovery);
            UpdateLabel(z);
            session.RaiseStateChanged();
            session.FlushSave("displayed");
        }

        private void OnTaken(PlacementZone z, SpecimenEntity e)
        {
            var session = GameSession.Instance;
            e.Record.Location = SpecimenLocation.World;   // it is leaving the cabinet; the pickup that follows sets Held
            session.State.Stats.SpecimensKept = session.State.DisplayedCount();
            RecomputePrestige(session.State);
            UpdateLabel(z);
            session.RaiseStateChanged();
        }

        public static void RecomputePrestige(GameState s)
        {
            float v = s.CollectionValue();
            int tier = v >= 6000f ? 5 : v >= 3000f ? 4 : v >= 1500f ? 3 : v >= 600f ? 2 : v >= 150f ? 1 : 0;
            s.Prestige = tier;
        }

        private void UpdateLabel(PlacementZone z)
        {
            if (!_labels.TryGetValue(z, out var tm))
            {
                tm = UI.WorldLabel.Create(z.transform, LabelFont, LabelMaterial, 0.024f, new Color(0.12f, 0.1f, 0.08f));
                tm.transform.localPosition = new Vector3(0f, 0.012f, -0.13f);
                tm.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
                _labels[z] = tm;
            }
            var occ = z.First;
            if (occ != null)
            {
                var r = occ.Record;
                string line2 = r.Geology.Family.Name + (!string.IsNullOrEmpty(r.Locality) ? "  •  " + r.Locality : "");
                string line3 = (r.Appraised ? AppraisalStation.ValueLabel(r) : "unappraised") + (r.OpenedAtTicks > 0 ? "  •  " + new System.DateTime(r.OpenedAtTicks).ToString("MMM yyyy") : "");
                tm.Text = (r.Favorite ? "★ " : "") + r.DisplayName + "\n" + line2 + "\n" + line3;
            }
            else tm.Text = "";
        }
    }
}
