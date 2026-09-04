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
        private Label _name, _mineral, _traits, _value, _record, _hint, _why, _call, _prov, _rarity;
        private VisualElement _facts, _valueBox;

        private void Start()
        {
            _station = FindAnyObjectByType<AppraisalStation>();
            var root = HudController.Instance.GetComponent<UIDocument>().rootVisualElement;
            // the reference pack's specimen card: name and rarity at the top, a run of labelled facts,
            // then the valuation set apart in green, then the prose
            _card = UiKit.Box(root, "detail", "appraisal-card");
            var head = UiKit.Box(_card, "row");
            head.style.alignItems = Align.FlexStart;
            var headText = UiKit.Box(head, "grow");
            headText.style.flexShrink = 1;
            _name = UiKit.Label(headText, "", "detail-title");
            _mineral = UiKit.Label(headText, "", "detail-sub");
            _rarity = UiKit.Rarity(head, 0);
            _rarity.style.flexShrink = 0;
            _rarity.style.marginLeft = 10;
            UiKit.Rule(_card);
            // the body scrolls inside the card: a long read-out must never squash its own rows
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("card-scroll");
            _card.Add(scroll);
            var body = scroll.contentContainer;
            _facts = UiKit.Box(body, "facts");
            _traits = UiKit.Label(body, "", "detail-note", "accent");
            _valueBox = UiKit.Box(body, "value-block");
            UiKit.Label(_valueBox, "ESTIMATED VALUE", "caption");
            _value = UiKit.Label(_valueBox, "", "appraisal-value");
            _why = UiKit.Label(_valueBox, "", "detail-note");
            _why.style.marginTop = 6;
            _call = UiKit.Label(body, "", "detail-note", "accent");
            _record = UiKit.Label(body, "", "appraisal-record", "medium");
            UiKit.Rule(body);
            _prov = UiKit.Label(body, "", "detail-note", "muted");
            _prov.style.marginTop = 0;
            _hint = UiKit.Label(body, "", "detail-note", "muted");
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
            _mineral.text = g.Family.Name;
            var tier = Valuation.TierFromValue(r.AppraisedValue);
            int chip = Mathf.Clamp((int)tier - 1, 0, 4);
            _rarity.text = Valuation.TierLabel(tier).ToUpper();
            foreach (var c in new[] { "rarity-common", "rarity-uncommon", "rarity-rare", "rarity-epic", "rarity-legendary" }) _rarity.RemoveFromClassList(c);
            _rarity.AddToClassList(new[] { "rarity-common", "rarity-uncommon", "rarity-rare", "rarity-epic", "rarity-legendary" }[chip]);

            _facts.Clear();
            if (!string.IsNullOrEmpty(r.Locality)) UiKit.Kv(_facts, "Origin", r.Locality);
            UiKit.Kv(_facts, "Weight", $"{g.MassKg:F2} kg");
            UiKit.Kv(_facts, "Crystal type", g.Family.Name);
            UiKit.Kv(_facts, "Formation", FormationWord(g.Cavity));
            UiKit.Kv(_facts, "Size", $"{g.Size * 200f:F0} cm across");
            UiKit.Kv(_facts, "Habit", Valuation.HabitWord(g));
            UiKit.Kv(_facts, "Saturation", Valuation.SaturationWord(g.Saturation));
            UiKit.Kv(_facts, "Clarity", Valuation.ClarityWord(g.Clarity));
            UiKit.Kv(_facts, "Zoning", Valuation.ZoningWord(g.Zoning));
            string tool = r.IsPiece ? "trim saw" : r.ProcessedBy == "hammer" ? "hammer and chisel" : r.ProcessedBy == "cracker" ? "geode cracker" : r.ProcessedBy;
            UiKit.Kv(_facts, "Opened with", string.IsNullOrEmpty(tool) ? (r.IsOpened ? "opened" : "unopened") : tool);
            if (r.StrikeCount > 0) UiKit.Kv(_facts, "Strikes", r.StrikeCount.ToString());
            if (r.Polish > 0.02f) UiKit.Kv(_facts, "Polish", Mathf.RoundToInt(r.Polish * 100f) + "%");
            if (r.Certified) UiKit.Kv(_facts, "UV", r.Fluorescence);
            float dmg = e.Visual.CrystalDamageFraction();
            string cond = dmg <= 0.001f ? "Clean, no damage" : dmg < 0.12f ? "Minor chipping" : dmg < 0.35f ? "Noticeable damage" : "Heavily damaged";
            if (dmg > 0.001f) cond += $"  (-{Mathf.RoundToInt(dmg * 85f)}%)";
            UiKit.Kv(_facts, "Condition", cond, dmg > 0.12f ? "danger" : "success");

            _why.text = string.Join("\n", Valuation.Explain(r));
            _call.text = r.Predicted ? PredictionLine(r) : "";
            _call.style.display = r.Predicted ? DisplayStyle.Flex : DisplayStyle.None;
            _prov.text = TabletUI.Provenance(r, true);
            _traits.text = string.Join("  \u2022  ", Valuation.Highlights(g));
            _traits.style.display = string.IsNullOrEmpty(_traits.text) ? DisplayStyle.None : DisplayStyle.Flex;
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
            _hint.text = $"Keep it: display cabinet {displayed}/{s.DisplayCapacity}.   Sell it: dealer outbox." + (Economy.Auction.IsEligible(r) ? "   Exceptional: the auction house takes consignments from the tablet." : "");
            _wanted = true;
            _card.style.display = DisplayStyle.Flex;
        }

        /// <summary>The call the player made in the hand against what came out.</summary>
        public static string PredictionLine(SpecimenRecord r)
        {
            var g = r.Geology;
            bool hollow = g.Cavity != CavityArchetype.Nodule;
            string hollowCall = r.PredictedHollow ? "hollow" : "solid";
            bool hollowRight = r.PredictedHollow == hollow;
            string tierCall = r.PredictedTier >= 0 ? Valuation.TierLabel((QualityTier)r.PredictedTier).ToLowerInvariant() : "";
            bool tierRight = r.PredictedTier >= 0 && Mathf.Abs(r.PredictedTier - (int)g.Tier) <= 1;
            string s = $"You called it {hollowCall}" + (tierCall.Length > 0 ? $", {tierCall}" : "") + ": ";
            s += hollowRight && (r.PredictedTier < 0 || tierRight) ? "right" : hollowRight ? "hollow was right, the grade was off" : "it was " + (hollow ? "hollow" : "solid");
            return s;
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
