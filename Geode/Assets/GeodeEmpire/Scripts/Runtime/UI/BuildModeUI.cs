using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Build;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;

namespace GeodeEmpire.UI
{
    /// <summary>
    /// Build mode's screen: what the shop is (top left), what it costs (below that), what is in hand and what it
    /// does (right), the catalogue along the bottom, and the controls under it. It reads the same panel kit as the
    /// rest of the game so layout editing feels like part of the product rather than a developer tool.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class BuildModeUI : MonoBehaviour
    {
        private UIDocument _doc;
        private VisualElement _root;
        private Label _overviewSize, _overviewDisplay, _overviewStorage, _overviewPlaced;
        private Label _cash, _spent;
        private VisualElement _strip, _tabs;
        private VisualElement _detail;
        private Label _detailName, _detailDesc, _detailStatus;
        private Label _statusLine;
        private string _tab = "";
        private BuildMode _mode;

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
            if (_doc.panelSettings == null) _doc.panelSettings = Resources.Load<PanelSettings>("UI/GeodePanelSettings");
            Build();
        }

        private void Start()
        {
            _mode = BuildMode.Instance;
            if (_mode != null) _mode.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy() { if (_mode != null) _mode.Changed -= Refresh; }

        private void Build()
        {
            _root = _doc.rootVisualElement;
            _root.styleSheets.Add(Resources.Load<StyleSheet>("UI/GeodeUI"));
            _root.AddToClassList("hud-root");
            _root.pickingMode = PickingMode.Ignore;

            // ---- shop overview ------------------------------------------------------------
            var left = UiKit.Box(_root, "card", "build-overview");
            var head = UiKit.Box(left, "row");
            UiKit.Label(head, "BUILD MODE", "section", "grow");
            UiKit.Label(left, "Customise your shop layout", "caption");
            UiKit.Rule(left);
            _overviewSize = UiKit.Kv(left, "Shop size", "—");
            _overviewDisplay = UiKit.Kv(left, "Display capacity", "—");
            _overviewStorage = UiKit.Kv(left, "Storage capacity", "—");
            _overviewPlaced = UiKit.Kv(left, "Fixtures placed", "—");

            UiKit.Rule(left);
            var budget = UiKit.Box(left, "build-budget");
            UiKit.Label(budget, "BUDGET", "section");
            _cash = UiKit.Kv(budget, "Till", "—", "accent");
            _spent = UiKit.Kv(budget, "Fittings owned", "—");
            UiKit.Label(budget, "Better layouts read as a better shop: keep aisles open and let the pieces be seen.", "build-tip");

            // ---- the fixture in hand ------------------------------------------------------
            _detail = UiKit.Box(_root, "card", "build-detail");
            _detailName = UiKit.Label(_detail, "—", "item-title");
            _detailDesc = UiKit.Label(_detail, "", "item-desc");
            UiKit.Rule(_detail);
            _detailStatus = UiKit.Label(_detail, "", "build-status");

            // ---- catalogue ----------------------------------------------------------------
            var bottom = UiKit.Box(_root, "build-bottom");
            _tabs = UiKit.Box(bottom, "row", "build-tabs");
            _strip = UiKit.Box(bottom, "row", "build-strip");
            var rail = UiKit.Box(bottom, "row", "build-rail");
            UiKit.KeyHint(rail, GameInput.Glyph("Rotate"), "Rotate");
            UiKit.KeyHint(rail, GameInput.Glyph("Interact"), "Place");
            UiKit.KeyHint(rail, "Scroll", "Next fitting");
            UiKit.KeyHint(rail, GameInput.Glyph("Build"), "Leave build mode");

            _statusLine = UiKit.Label(_root, "", "build-reason");
            _root.style.display = DisplayStyle.None;
        }

        private void Update()
        {
            if (_mode == null) { _mode = BuildMode.Instance; if (_mode != null) _mode.Changed += Refresh; return; }
            bool on = _mode.Active;
            var want = on ? DisplayStyle.Flex : DisplayStyle.None;
            if (_root.style.display != want) { _root.style.display = want; if (on) Refresh(); }
            if (!on) return;
            _statusLine.text = _mode.CurrentValid ? "" : _mode.CurrentReason;
            _statusLine.EnableInClassList("build-reason-bad", !_mode.CurrentValid);
            _detailStatus.text = _mode.CurrentValid ? "Ready to place." : _mode.CurrentReason;
            _detailStatus.EnableInClassList("warn", !_mode.CurrentValid);
            _detailStatus.EnableInClassList("success", _mode.CurrentValid);
        }

        private void Refresh()
        {
            if (_mode == null || !_mode.Active) return;
            var st = GameSession.Instance != null ? GameSession.Instance.State : null;

            int placed = 0, slots = 0, storage = 0;
            foreach (var f in PlaceableFixture.All)
            {
                if (f == null || !f.Owned) continue;
                if (f.Pose.Placed || f.SitedByDefault) placed++;
                if (f.Category == "DISPLAYS") slots += f.Slots; else if (f.Category == "STORAGE") storage += f.Slots;
            }
            int stage = st != null ? Mathf.Max(1, st.WorkshopStage) : 1;
            _overviewSize.text = stage >= 3 ? "Large" : stage == 2 ? "Medium" : "Small";
            _overviewDisplay.text = slots > 0 ? slots + " slots" : "—";
            _overviewStorage.text = storage > 0 ? storage + " slots" : "—";
            _overviewPlaced.text = placed + " of " + CountOwned();
            _cash.text = st != null ? UiKit.Money(st.Cash) : "—";
            float spent = 0f;
            foreach (var f in PlaceableFixture.All) if (f != null && f.Owned) spent += f.Price;
            _spent.text = UiKit.Money(spent);

            // tabs
            var cats = new List<string>();
            foreach (var f in _mode.Available) if (!cats.Contains(f.Category)) cats.Add(f.Category);
            if (!cats.Contains(_tab)) _tab = cats.Count > 0 ? cats[0] : "";
            _tabs.Clear();
            foreach (var c in cats)
            {
                string cc = c;
                var b = UiKit.Button(_tabs, c, () => { _tab = cc; Refresh(); }, "tab");
                b.EnableInClassList("tab-active", c == _tab);
            }

            // catalogue strip
            _strip.Clear();
            for (int i = 0; i < _mode.Available.Count; i++)
            {
                var f = _mode.Available[i];
                if (f.Category != _tab) continue;
                int idx = i;
                var cell = UiKit.Box(_strip, "build-cell");
                if (f == _mode.Holding) cell.AddToClassList("build-cell-on");
                UiKit.Label(cell, f.DisplayName, "build-cell-name");
                UiKit.Label(cell, f.Pose.Placed ? "placed" : "to site", f.Pose.Placed ? "muted" : "accent");
                cell.RegisterCallback<ClickEvent>(_ => { _mode.Select(idx); });
            }

            var h = _mode.Holding;
            _detailName.text = h != null ? h.DisplayName : "—";
            _detailDesc.text = h != null ? h.Description : "";
            _detail.Clear();
            _detail.Add(_detailName);
            var chip = UiKit.Box(_detail, "row");
            if (h != null)
            {
                UiKit.Label(chip, h.Category, "tag");
                if (!h.Pose.Placed) UiKit.Label(chip, "NOT YET SITED", "tag", "accent");
            }
            _detail.Add(_detailDesc);
            UiKit.Rule(_detail);
            if (h != null)
            {
                UiKit.Kv(_detail, "Size", $"{h.Footprint.x:0.0} m x {h.Footprint.y:0.0} m");
                if (h.Clearance > 0.01f) UiKit.Kv(_detail, "Working space", $"{h.Clearance:0.0} m at the operator side");
                if (h.Slots > 0) UiKit.Kv(_detail, h.Category == "DISPLAYS" ? "Display slots" : "Storage slots", h.Slots.ToString());
                if (h.Price > 0f) UiKit.Kv(_detail, "Cost", UiKit.Money(h.Price), "accent");
            }
            _detail.Add(_detailStatus);
        }

        private int CountOwned()
        {
            int n = 0;
            foreach (var f in PlaceableFixture.All) if (f != null && f.Owned && f.Movable) n++;
            return n;
        }
    }
}
