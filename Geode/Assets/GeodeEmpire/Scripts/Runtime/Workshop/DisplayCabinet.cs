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
            for (int i = 0; i < Slots.Count; i++)
            {
                bool locked = i >= cap;
                Slots[i].Locked = locked && Slots[i].IsEmpty;
                Slots[i].DisplayLabel = locked ? "locked shelf" : $"display slot {i + 1}";
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
            if (occ != null) tm.Text = occ.Record.DisplayName + "\n" + (occ.Record.Appraised ? AppraisalStation.ValueLabel(occ.Record) : "unappraised");
            else tm.Text = "";
        }
    }
}
