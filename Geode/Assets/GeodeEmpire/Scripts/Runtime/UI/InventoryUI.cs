using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.UI
{
    /// <summary>
    /// The inventory screen (R03): everything the business owns, stacked by kind, over the room the player is
    /// standing in. The reference key rail advertises it on I, so it is a first-class screen rather than a tab
    /// buried in the tablet: what have I got, what is it worth, and where is it.
    /// </summary>
    public sealed class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }
        public bool IsOpen { get; private set; }

        private enum Cat { All, Rough, Geodes, Slabs, Polished, OnSale, Collection }
        private enum Sort { Name, Qty, Quality, Value }

        /// <summary>One line of the list: every piece of the same kind, quality and condition counted together.</summary>
        private sealed class Stack
        {
            public string Name, Category, Condition;
            public int Tier, Count;
            public float Value;
            public int ConditionRank;
            public readonly List<SpecimenRecord> Members = new List<SpecimenRecord>();
        }

        private GameSession _s;
        private VisualElement _panel, _chipRow, _rows, _tabRow, _hudBrand, _retailChip;
        private Label _totalItems, _totalValue, _subtitle, _empty;
        private TextField _search;
        private Cat _cat = Cat.All;
        private Sort _sort = Sort.Value;
        private bool _descending = true;
        private int _tab;                     // 0 = items, 1 = where it all is
        private int _selected;
        private readonly List<Button> _chips = new List<Button>();
        private readonly List<Button> _tabs = new List<Button>();
        private readonly List<Label> _headers = new List<Label>();
        private readonly List<Stack> _stacks = new List<Stack>();
        private string _expanded;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            _s = GameSession.Instance;
            var hud = HudController.Instance;
            if (hud == null) { enabled = false; return; }
            var root = hud.GetComponent<UIDocument>().rootVisualElement;

            _panel = UiKit.Box(root, "card", "inv-panel");
            _panel.style.display = DisplayStyle.None;

            var brand = UiKit.Box(_panel, "panel-brand");
            UiKit.Box(brand, "brand-gem");
            UiKit.Label(brand, "GEODE EMPIRE", "panel-brandname");
            UiKit.Label(_panel, "Inventory", "page-title");
            _subtitle = UiKit.Label(_panel, "", "page-sub");
            _subtitle.style.whiteSpace = WhiteSpace.Normal;

            _tabRow = UiKit.Box(_panel, "tab-row", "inv-tabs");
            string[] tabNames = { "INVENTORY", "WHERE IT IS" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int idx = i;
                var b = new Button(() => { _tab = idx; Refresh(); }) { text = tabNames[i] };
                b.AddToClassList("tab");
                _tabRow.Add(b);
                _tabs.Add(b);
            }

            _chipRow = UiKit.Box(_panel, "inv-chips");
            foreach (Cat c in Enum.GetValues(typeof(Cat)))
            {
                var cc = c;
                var b = new Button(() => { _cat = cc; _expanded = null; Refresh(); }) { text = CatName(c) };
                b.AddToClassList("inv-chip");
                _chipRow.Add(b);
                _chips.Add(b);
            }

            var filters = UiKit.Box(_panel, "inv-filters");
            UiKit.Label(filters, "FILTERS", "section");
            _search = new TextField { value = "" };
            _search.AddToClassList("inv-search");
            var input = _search.Q(TextField.textInputUssName);
            if (input != null) input.style.backgroundColor = Color.clear;
            _search.RegisterValueChangedCallback(_ => { _expanded = null; Refresh(); });
            filters.Add(_search);
            UiKit.Label(filters, "Search items", "muted", "inv-search-hint");

            var head = UiKit.Box(_panel, "inv-thead");
            AddHeader(head, "ITEM", "inv-col-item", Sort.Name);
            AddHeader(head, "CATEGORY", "inv-col-cat", Sort.Name);
            AddHeader(head, "QTY", "inv-col-qty", Sort.Qty);
            AddHeader(head, "QUALITY", "inv-col-qual", Sort.Quality);
            AddHeader(head, "CONDITION", "inv-col-cond", Sort.Name);
            AddHeader(head, "VALUE", "inv-col-val", Sort.Value);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("inv-scroll");
            _panel.Add(scroll);
            _rows = scroll.contentContainer;
            _empty = UiKit.Label(_rows, "", "muted", "inv-empty");

            var foot = UiKit.Box(_panel, "inv-foot");
            _totalItems = UiKit.Label(foot, "Total Items: 0", "caption");
            UiKit.Box(foot, "grow");
            _totalValue = UiKit.Label(foot, "Total Value: $0", "caption", "accent");

            _hudBrand = root.Q<VisualElement>(className: "brand");
            _retailChip = root.Q<VisualElement>(className: "retail-chip");
            var rail = UiKit.Box(_panel, "keybar", "inv-rail");
            UiKit.KeyHint(rail, GameInput.Glyph("Interact"), "Expand stack");
            UiKit.KeyHint(rail, GameInput.Glyph("Inventory"), "Close");
            UiKit.KeyHint(rail, GameInput.Glyph("Back"), "Back");

            if (_s != null) _s.StateChanged += OnStateChanged;
        }

        private void OnStateChanged() { if (IsOpen) Refresh(); }

        private void AddHeader(VisualElement parent, string text, string col, Sort sort)
        {
            var l = UiKit.Label(parent, text, "inv-th", col);
            l.pickingMode = PickingMode.Position;
            l.RegisterCallback<ClickEvent>(_ =>
            {
                if (_sort == sort) _descending = !_descending; else { _sort = sort; _descending = sort != Sort.Name; }
                WorkshopAudio.Play2D("ui_click", 0.3f);
                Refresh();
            });
            _headers.Add(l);
        }

        private static string CatName(Cat c) => c switch
        {
            Cat.All => "All Items",
            Cat.Rough => "Rough",
            Cat.Geodes => "Geodes",
            Cat.Slabs => "Slabs",
            Cat.Polished => "Polished",
            Cat.OnSale => "For Sale",
            _ => "Collection",
        };

        private void Update()
        {
            if (!IsOpen)
            {
                if (GameInput.InventoryPressed && !CursorController.InMenu && CanOpen()) Open();
                return;
            }
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var gp = UnityEngine.InputSystem.Gamepad.current;
            if (GameInput.InventoryPressed || (kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame)) Close();
        }

        /// <summary>The inventory is a free-roam screen: it never opens over a workstation or build mode.</summary>
        private bool CanOpen()
        {
            var hud = HudController.Instance;
            if (hud == null || !hud.FreeRoam) return false;
            var build = Build.BuildMode.Instance;
            return build == null || !build.Active;
        }

        public void Open()
        {
            if (IsOpen || _panel == null) return;
            IsOpen = true;
            _panel.style.display = DisplayStyle.Flex;
            if (_hudBrand != null) _hudBrand.style.display = DisplayStyle.None;
            if (_retailChip != null) _retailChip.style.visibility = Visibility.Hidden;
            CursorController.EnterMenu();
            HudController.Instance.SetFreeRoamVisible(false);
            WorkshopAudio.Play2D("ui_click", 0.4f);
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _panel.style.display = DisplayStyle.None;
            if (_hudBrand != null) _hudBrand.style.display = DisplayStyle.Flex;
            if (_retailChip != null) _retailChip.style.visibility = Visibility.Visible;
            CursorController.ExitMenu();
            HudController.Instance.SetFreeRoamVisible(true);
            WorkshopAudio.Play2D("ui_click", 0.3f, 0.9f);
        }

        // ---- the data -----------------------------------------------------------------------------

        private static bool Counts(SpecimenRecord r) =>
            r.Location != SpecimenLocation.Sold && r.Location != SpecimenLocation.Discarded && r.Location != SpecimenLocation.Cut;

        private static bool InCategory(SpecimenRecord r, Cat c) => c switch
        {
            Cat.All => true,
            Cat.Rough => !r.IsPiece && !r.IsOpened,
            Cat.Geodes => !r.IsPiece && r.IsOpened,
            Cat.Slabs => r.IsPiece && r.Polish < 0.45f,
            Cat.Polished => r.IsPiece && r.Polish >= 0.45f,
            Cat.OnSale => r.Location == SpecimenLocation.SaleSlot,
            _ => r.Location == SpecimenLocation.DisplaySlot,
        };

        private static string CategoryOf(SpecimenRecord r) =>
            r.IsPiece ? (r.Polish >= 0.45f ? "Polished" : "Slabs") : r.IsOpened ? "Geodes" : "Rough";

        /// <summary>Physical condition, the way the reference's list reads it: how much of the piece survived.</summary>
        private static void ConditionOf(SpecimenRecord r, out string word, out int rank)
        {
            float damage = Mathf.Max(r.DamageFraction, r.ShellDamage);
            if (!r.IsOpened && !r.IsPiece && r.Condition != null && r.Condition.Cleaned < 0.4f) { word = "Uncleaned"; rank = 1; return; }
            if (damage < 0.02f) { word = "Excellent"; rank = 4; return; }
            if (damage < 0.12f) { word = "Good"; rank = 3; return; }
            if (damage < 0.3f) { word = "Fair"; rank = 2; return; }
            word = "Damaged"; rank = 0;
        }

        private static string LocationName(SpecimenLocation l) => l switch
        {
            SpecimenLocation.InCrate => "Unopened crates",
            SpecimenLocation.World => "Loose in the workshop",
            SpecimenLocation.Held => "In hand",
            SpecimenLocation.Bench => "On the cracking bench",
            SpecimenLocation.SellTray => "In the sell tray",
            SpecimenLocation.AppraisalStation => "At the appraisal bench",
            SpecimenLocation.DisplaySlot => "In the collection",
            SpecimenLocation.SaleSlot => "On sale in the showroom",
            SpecimenLocation.WashTub => "In the wash tub",
            SpecimenLocation.Consigned => "With the auction house",
            SpecimenLocation.Saw => "Clamped in the saw",
            SpecimenLocation.Lap => "On the polishing lap",
            SpecimenLocation.Rack => "On the rock rack",
            SpecimenLocation.Cracker => "On the cracker",
            _ => l.ToString(),
        };

        private void Rebuild()
        {
            _stacks.Clear();
            if (_s == null || _s.State == null) return;
            string q = _search != null ? _search.value.Trim() : "";
            var byKey = new Dictionary<string, Stack>();
            foreach (var r in _s.State.Specimens)
            {
                if (r == null || !Counts(r) || !InCategory(r, _cat)) continue;
                string name = r.DisplayName;
                if (q.Length > 0 && name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                float value = r.EstimatedValue();
                int tier = (int)Valuation.TierFromValue(value);
                ConditionOf(r, out string cond, out int rank);
                string key = name + "|" + tier + "|" + cond;
                if (!byKey.TryGetValue(key, out var st))
                {
                    st = new Stack { Name = name, Category = CategoryOf(r), Condition = cond, ConditionRank = rank, Tier = tier };
                    byKey[key] = st;
                    _stacks.Add(st);
                }
                st.Count++;
                st.Value += value;
                st.Members.Add(r);
            }
            int dir = _descending ? -1 : 1;
            _stacks.Sort((a, b) => _sort switch
            {
                Sort.Qty => dir * a.Count.CompareTo(b.Count),
                Sort.Quality => dir * (a.Tier != b.Tier ? a.Tier.CompareTo(b.Tier) : a.Value.CompareTo(b.Value)),
                Sort.Value => dir * a.Value.CompareTo(b.Value),
                _ => dir * -string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase),
            });
        }

        // ---- drawing ------------------------------------------------------------------------------

        private void Refresh()
        {
            if (_panel == null) return;
            for (int i = 0; i < _tabs.Count; i++) _tabs[i].EnableInClassList("tab-active", i == _tab);
            for (int i = 0; i < _chips.Count; i++) _chips[i].EnableInClassList("inv-chip-on", (int)_cat == i);
            _chipRow.style.display = _tab == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _panel.Q<VisualElement>(className: "inv-thead").style.display = _tab == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _rows.Clear();
            _rows.Add(_empty);
            if (_tab == 0) DrawItems(); else DrawLocations();
        }

        private void DrawItems()
        {
            Rebuild();
            int items = 0;
            float value = 0f;
            foreach (var st in _stacks) { items += st.Count; value += st.Value; }
            _subtitle.text = "Every piece the business owns, stacked by kind. Sold and sawn-up pieces have left the books.";
            _totalItems.text = $"Total Items: {items}";
            _totalValue.text = $"Total Value: {UiKit.Money(value)}";
            _empty.text = _stacks.Count == 0
                ? (_s != null && _s.State != null && _s.State.Specimens.Count > 0 ? "Nothing here matches that filter." : "Nothing yet. Order a crate from the tablet and start cracking.")
                : "";
            _empty.style.display = _stacks.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var st in _stacks)
            {
                var row = UiKit.Box(_rows, "inv-row");
                var plate = UiKit.Box(row, "inv-plate");
                if (st.Members.Count > 0) SpecimenThumbnailer.Instance.Specimen(plate, st.Members[0], SpecimenThumbnailer.Ground);
                UiKit.Label(row, st.Name, "inv-cell", "inv-col-item");
                UiKit.Label(row, st.Category, "inv-cell", "inv-col-cat", "muted");
                UiKit.Label(row, st.Count.ToString(), "inv-cell", "inv-col-qty");
                Stars(row, st.Tier);
                var cond = UiKit.Box(row, "inv-cell", "inv-col-cond", "inv-cond");
                var dot = UiKit.Box(cond, "inv-dot");
                dot.style.backgroundColor = st.ConditionRank >= 4 ? new Color(0.36f, 0.86f, 0.52f)
                    : st.ConditionRank >= 3 ? new Color(0.62f, 0.82f, 0.42f)
                    : st.ConditionRank >= 2 ? new Color(0.92f, 0.76f, 0.34f) : new Color(0.92f, 0.42f, 0.34f);
                UiKit.Label(cond, st.Condition, "muted");
                UiKit.Label(row, UiKit.Money(st.Value), "inv-cell", "inv-col-val", "accent");
                bool open = _expanded == st.Name + st.Tier + st.Condition;
                row.EnableInClassList("inv-row-on", open);
                row.pickingMode = PickingMode.Position;
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    _expanded = open ? null : st.Name + st.Tier + st.Condition;
                    WorkshopAudio.Play2D("ui_click", 0.3f);
                    Refresh();
                });
                if (!open) continue;
                // a stack opens into the pieces themselves: where each one is, and what it is worth on its own
                foreach (var r in st.Members)
                {
                    var sub = UiKit.Box(_rows, "inv-subrow");
                    UiKit.Label(sub, r.Favorite ? "★" : "·", "inv-bullet");
                    UiKit.Label(sub, LocationName(r.Location), "inv-cell", "inv-col-item", "muted");
                    UiKit.Label(sub, r.Appraised ? "Appraised" : "Not appraised", "inv-cell", "inv-col-cat", "muted");
                    UiKit.Label(sub, $"{r.Geology.MassKg:0.00} kg", "inv-cell", "inv-col-qty2", "muted");
                    UiKit.Label(sub, UiKit.Money(r.EstimatedValue()), "inv-cell", "inv-col-val", "muted");
                }
            }
        }

        private void DrawLocations()
        {
            _subtitle.text = "Where the stock physically is right now. A piece is only worth something where a customer can reach it.";
            var counts = new Dictionary<SpecimenLocation, (int n, float v)>();
            int items = 0;
            float value = 0f;
            if (_s != null && _s.State != null)
                foreach (var r in _s.State.Specimens)
                {
                    if (r == null || !Counts(r)) continue;
                    counts.TryGetValue(r.Location, out var e);
                    float v = r.EstimatedValue();
                    counts[r.Location] = (e.n + 1, e.v + v);
                    items++; value += v;
                }
            _totalItems.text = $"Total Items: {items}";
            _totalValue.text = $"Total Value: {UiKit.Money(value)}";
            _empty.text = counts.Count == 0 ? "Nothing in stock anywhere yet." : "";
            _empty.style.display = counts.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            var keys = new List<SpecimenLocation>(counts.Keys);
            keys.Sort((a, b) => counts[b].v.CompareTo(counts[a].v));
            foreach (var k in keys)
            {
                var row = UiKit.Box(_rows, "inv-row");
                UiKit.Label(row, LocationName(k), "inv-cell", "inv-col-item");
                UiKit.Label(row, counts[k].n + (counts[k].n == 1 ? " piece" : " pieces"), "inv-cell", "inv-col-cat", "muted");
                UiKit.Label(row, UiKit.Money(counts[k].v), "inv-cell", "inv-col-val", "accent");
            }
            if (_s != null && _s.State != null)
            {
                int crates = 0;
                foreach (var c in _s.State.Crates) if (c != null && c.Delivered && !c.Opened) crates++;
                if (crates > 0)
                {
                    var row = UiKit.Box(_rows, "inv-row");
                    UiKit.Label(row, "Sealed crates on the floor", "inv-cell", "inv-col-item");
                    UiKit.Label(row, crates + (crates == 1 ? " crate" : " crates"), "inv-cell", "inv-col-cat", "muted");
                    UiKit.Label(row, "—", "inv-cell", "inv-col-val", "muted");
                }
            }
        }

        private static void Stars(VisualElement parent, int tier)
        {
            var box = UiKit.Box(parent, "inv-cell", "inv-col-qual", "inv-stars");
            for (int i = 0; i < 5; i++)
            {
                var l = UiKit.Label(box, i <= tier ? "★" : "☆", "inv-star");
                if (i <= tier) l.AddToClassList("inv-star-on");
            }
        }
    }
}
