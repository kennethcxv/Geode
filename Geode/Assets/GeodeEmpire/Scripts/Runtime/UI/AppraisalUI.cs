using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.UI
{
    /// <summary>The specimen card shown while a specimen sits on the scale: what it is, why it is worth that.</summary>
    public sealed class AppraisalUI : MonoBehaviour
    {
        private AppraisalStation _station;
        private VisualElement _card;
        private Label _name, _mineral, _size, _traits, _condition, _value, _record, _hint;

        private void Start()
        {
            _station = FindAnyObjectByType<AppraisalStation>();
            var root = HudController.Instance.GetComponent<UIDocument>().rootVisualElement;
            _card = UiKit.Box(root, "card", "appraisal-card");
            _card.style.position = Position.Absolute;
            _card.style.right = 40;
            _card.style.top = Length.Percent(22);
            _name = UiKit.Label(_card, "", "appraisal-name", "bold");
            _mineral = UiKit.Label(_card, "", "appraisal-line", "muted");
            _size = UiKit.Label(_card, "", "appraisal-line");
            _traits = UiKit.Label(_card, "", "appraisal-line", "accent");
            _traits.style.whiteSpace = WhiteSpace.Normal;
            _condition = UiKit.Label(_card, "", "appraisal-line");
            _value = UiKit.Label(_card, "", "appraisal-value", "bold");
            _record = UiKit.Label(_card, "", "appraisal-record", "medium");
            _hint = UiKit.Label(_card, "", "muted");
            _hint.style.marginTop = 12;
            _hint.style.whiteSpace = WhiteSpace.Normal;
            _card.style.display = DisplayStyle.None;
            if (_station != null)
            {
                _station.Appraised += Show;
                _station.Cleared += Hide;
            }
        }

        private void OnDestroy()
        {
            if (_station != null) { _station.Appraised -= Show; _station.Cleared -= Hide; }
        }

        private bool _wanted;

        /// <summary>The card belongs to free roam: a station view or a menu takes the screen, and it comes back after.</summary>
        private void Update()
        {
            if (_card == null) return;
            // the card reads at the scale; a piece left on it should not follow the player round the workshop
            bool near = _station == null || Camera.main == null || (Camera.main.transform.position - _station.transform.position).sqrMagnitude < 3.4f * 3.4f;
            bool show = _wanted && near && HudController.Instance != null && HudController.Instance.FreeRoam && !CursorController.InMenu;
            var d = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (_card.style.display != d) _card.style.display = d;
        }

        private void Show(SpecimenEntity e)
        {
            var r = e.Record;
            var g = e.Geology;
            var s = GameSession.Instance.State;
            _name.text = r.DisplayName;
            _mineral.text = $"{g.Family.Name}  •  {Valuation.TierLabel(Valuation.TierFromValue(r.AppraisedValue))}";
            _size.text = $"{g.MassKg:F2} kg  •  {g.Size * 200f:F0} cm across  •  {FormationWord(g.Cavity)}";
            _traits.text = string.Join("  •  ", Valuation.Highlights(g));
            float dmg = e.Visual.CrystalDamageFraction();
            string cond = dmg <= 0.001f ? "Condition: clean, no damage" : dmg < 0.12f ? "Condition: minor chipping" : dmg < 0.35f ? "Condition: noticeable damage" : "Condition: heavily damaged";
            if (dmg > 0.001f) cond += $"  (−{Mathf.RoundToInt(dmg * 85f)}%)";
            _condition.text = cond;
            _condition.RemoveFromClassList("danger");
            if (dmg > 0.12f) _condition.AddToClassList("danger");
            _value.text = AppraisalStation.ValueLabel(r);
            var entry = s.GetOrCreateEntry(g.Mineral);
            string rec = "";
            if (entry.Found <= 1) rec = "NEW: first " + g.Family.Name;
            else if (entry.BestSpecimenId == r.Id) rec = "NEW RECORD: best " + g.Family.Name + " so far";
            else if (g.MassKg >= entry.LargestMassKg - 0.001f) rec = "Largest " + g.Family.Name + " so far";
            if (g.Tier >= QualityTier.Exceptional) rec = (rec.Length > 0 ? rec + "\n" : "") + Valuation.TierLabel(g.Tier).ToUpper() + " SPECIMEN";
            _record.text = rec;
            _record.style.display = string.IsNullOrEmpty(rec) ? DisplayStyle.None : DisplayStyle.Flex;
            int displayed = s.DisplayedCount();
            _hint.text = $"Keep it: display cabinet {displayed}/{s.DisplayCapacity}.   Sell it: dealer outbox.";
            _wanted = true;
            _card.style.display = DisplayStyle.Flex;
        }

        private static string FormationWord(CavityArchetype c) => c switch
        {
            CavityArchetype.Hollow => "open cavity",
            CavityArchetype.ThickWall => "thick-walled cavity",
            CavityArchetype.Cathedral => "cathedral cavity",
            CavityArchetype.Pocket => "small pocket",
            CavityArchetype.DoubleChamber => "double chamber",
            _ => "solid nodule",
        };

        private void Hide()
        {
            _wanted = false;
            _card.style.display = DisplayStyle.None;
        }
    }
}
