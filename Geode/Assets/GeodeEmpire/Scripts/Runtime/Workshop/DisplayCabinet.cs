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
        private readonly Dictionary<PlacementZone, TextMesh> _labels = new Dictionary<PlacementZone, TextMesh>();

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
            float v = e.Record.Appraised ? e.Record.AppraisedValue : e.Geology.BaseValue;
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
            session.State.Stats.SpecimensKept = Mathf.Max(0, session.State.DisplayedCount() - 1);
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
                var go = new GameObject("Label");
                go.transform.SetParent(z.transform, false);
                go.transform.localPosition = new Vector3(0f, 0.012f, -0.13f);
                go.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
                tm = go.AddComponent<TextMesh>();
                tm.characterSize = 0.012f;
                tm.fontSize = 48;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = new Color(0.12f, 0.1f, 0.08f);
                if (LabelFont != null) { tm.font = LabelFont; go.GetComponent<MeshRenderer>().sharedMaterial = LabelFont.material; }
                _labels[z] = tm;
            }
            var occ = z.First;
            if (occ != null) tm.text = occ.Record.DisplayName + "\n" + (occ.Record.Appraised ? AppraisalStation.ValueLabel(occ.Record) : "unappraised");
            else tm.text = z.Locked ? "" : "";
        }
    }
}
