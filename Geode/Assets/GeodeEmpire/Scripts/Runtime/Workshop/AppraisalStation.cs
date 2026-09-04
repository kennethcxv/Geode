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
{    /// <summary>Weigh a specimen; the card explains why it is worth what it is worth.</summary>
    public sealed class AppraisalStation : MonoBehaviour
    {
        public PlacementZone Scale;
        public Light UvLight;    // Stage 3: the longwave lamp over the scale, lit while an exceptional piece is verified
        public SpecimenEntity Current { get; private set; }
        public bool Weighing { get; private set; }
        public event Action<SpecimenEntity> Appraised;
        public event Action Cleared;

        private void Awake()
        {
            if (Scale != null) { Scale.Placed += OnPlaced; Scale.Taken += OnTaken; }
        }

        private void OnPlaced(PlacementZone z, SpecimenEntity e)
        {
            WorkshopAudio.Play("rock_place", e.transform.position, 0.7f);
            StartCoroutine(Weigh(e));
        }

        private void OnTaken(PlacementZone z, SpecimenEntity e)
        {
            Current = null;
            Cleared?.Invoke();
        }

        private IEnumerator Weigh(SpecimenEntity e)
        {
            Weighing = true;
            Current = e;
            yield return new WaitForSeconds(0.35f);
            WorkshopAudio.Play("tick", e.transform.position, 0.5f, 1.4f);
            yield return new WaitForSeconds(0.45f);
            if (Current != e) { Weighing = false; yield break; }
            var session = GameSession.Instance;
            float damage = e.Visual.CrystalDamageFraction();
            e.Record.DamageFraction = damage;
            float value = e.Record.PristineForSale();   // a sawn piece is valued as a piece, a split rock as a rock
            bool first = !e.Record.Appraised;
            e.Record.Appraised = true;
            e.Record.AppraisedValue = value;
            // Stage 3: the UV lamp goes on over anything exceptional; fluorescence is noted and the piece is certified
            if (WorkshopExpansion.Stage3Active && e.Geology.Tier >= QualityTier.Exceptional && !e.Record.Certified)
            {
                if (UvLight != null) UvLight.enabled = true;
                yield return new WaitForSeconds(0.9f);
                if (UvLight != null) UvLight.enabled = false;
                if (Current != e) { Weighing = false; yield break; }
                e.Record.Fluorescence = FluorescenceWord(e.Geology.Mineral);
                e.Record.Certified = true;
                GameState.Log(e.Record, "certified", 0f, "verified under UV: " + e.Record.Fluorescence);
                WorkshopAudio.Play("crystal_chime", e.transform.position, 0.4f, 1.3f);
            }
            if (first) GameState.Log(e.Record, "appraised", value);
            WorkshopAudio.Play("ui_click", e.transform.position, 0.5f, 1.2f);
            if (first) Tutorial.Notify("appraised");
            session.RaiseStateChanged();
            session.QueueSave("appraised");
            Weighing = false;
            Appraised?.Invoke(e);
        }

        /// <summary>Longwave UV response by family, as the appraiser writes it.</summary>
        public static string FluorescenceWord(MineralId m) => m switch
        {
            MineralId.Calcite => "glows red-orange",
            MineralId.Fluorite => "glows blue-violet",
            MineralId.Apophyllite => "faint green glow",
            MineralId.Aragonite => "glows cream",
            MineralId.Wulfenite => "dull orange glow",
            MineralId.Selenite => "faint blue-white glow",
            MineralId.Halite => "faint orange glow",
            _ => "inert under UV",
        };

        public static string ValueLabel(SpecimenRecord r)
        {
            var s = GameSession.Instance;
            bool exact = s != null && UpgradeCatalog.Has(s.State, UpgradeCatalog.CalibratedScale);
            if (exact) return UI.UiKit.Money(r.AppraisedValue);
            float lo = Mathf.Round(r.AppraisedValue * 0.86f), hi = Mathf.Round(r.AppraisedValue * 1.14f);
            return $"{UI.UiKit.Money(lo)} – {UI.UiKit.Money(hi)}";
        }
    }
}
