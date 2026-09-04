using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.UI
{
    /// <summary>Suppliers, upgrades, collection and statistics on the workshop tablet.</summary>
    public sealed class TabletUI : MonoBehaviour
    {
        public static TabletUI Instance { get; private set; }
        public bool IsOpen { get; private set; }
        public int CurrentTab => _tab;
        public string FocusedText => UiKit.FocusedText(_panel);

        private VisualElement _root, _dim, _panel, _content, _detail;
        private string _detailKey;   // which row the detail card is showing, so a rebuild keeps it
        private Label _cash, _subtitle;
        private readonly List<Button> _tabs = new List<Button>();
        private int _tab;
        private GameSession _s;

        private void Awake() => Instance = this;

        private void Start()
        {
            _s = GameSession.Instance;
            var hud = HudController.Instance;
            _root = hud.GetComponent<UIDocument>().rootVisualElement;
            _dim = UiKit.Box(_root, "panel-dim");
            _dim.style.display = DisplayStyle.None;
            _panel = UiKit.Box(_dim, "panel");
            _panel.style.width = 1500;
            _panel.style.height = 880;
            var header = UiKit.Box(_panel, "panel-head");
            var brand = UiKit.Box(header, "panel-brand");
            UiKit.Box(brand, "brand-gem");
            UiKit.Label(brand, "GEODE EMPIRE", "panel-brandname");
            var titleBox = UiKit.Box(header, "grow");
            UiKit.Label(titleBox, "WORKSHOP TABLET", "page-title");
            _subtitle = UiKit.Label(titleBox, "", "page-sub");
            _cash = UiKit.Label(header, "$0", "status-cash");
            _cash.style.fontSize = 34;
            var close = UiKit.Button(header, "Close", Close, "btn-ghost");
            close.style.marginLeft = 22;
            var tabRow = UiKit.Box(_panel, "tab-row");
            string[] names = { "Suppliers", "Upgrades", "Collection", "Stats" };
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i;
                var b = new Button(() => SelectTab(idx)) { text = names[i] };
                b.AddToClassList("tab");
                b.RegisterCallback<FocusInEvent>(_ => { if (_tab != idx) SelectTab(idx); });   // d-pad left/right switches tabs directly
                tabRow.Add(b);
                _tabs.Add(b);
            }
            var body = UiKit.Box(_panel, "panel-body");
            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.AddToClassList("panel-list");
            body.Add(_scroll);
            _content = _scroll.contentContainer;
            // the pack's management screens always keep a detail card down the right edge: the list stays
            // scannable and everything long about the highlighted row lives over here
            // the detail card scrolls: a long supplier read-out must not squash its own rows
            var detailScroll = new ScrollView(ScrollViewMode.Vertical);
            detailScroll.AddToClassList("panel-detail");
            body.Add(detailScroll);
            _detail = detailScroll.contentContainer;
            var foot = UiKit.Box(_panel, "panel-foot");
            UiKit.KeyHint(foot, GameInput.Glyph("Move"), "Navigate");
            UiKit.KeyHint(foot, GameInput.Glyph("Interact"), "Select");
            UiKit.KeyHint(foot, GameInput.Glyph("Back"), "Close");
            // Controller navigation: spatial navigation never steps from the tab row into the scroll view, so route
            // it explicitly. Down enters the list, Up/Down walk the buttons, Left/Right in the list switch tabs.
            _panel.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);

            OrderTablet.Opened += Open;
            _s.StateChanged += Refresh;
            _s.CashChanged += (c, d) => { if (IsOpen) Refresh(); };
            _dim.RegisterCallback<NavigationCancelEvent>(e => { Close(); e.StopPropagation(); });
        }

        private void OnDestroy()
        {
            OrderTablet.Opened -= Open;
            if (_s != null) _s.StateChanged -= Refresh;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!IsOpen)
            {
                if (GameInput.TabletPressed && !CursorController.InMenu && !(FindAnyObjectByType<Cracking.CrackingBench>()?.Active ?? false)) Open();
                return;
            }
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var gp = UnityEngine.InputSystem.Gamepad.current;
            bool back = (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)) || (gp != null && (gp.buttonEast.wasPressedThisFrame || gp.selectButton.wasPressedThisFrame));
            if (back) Close();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            CursorController.EnterMenu();
            HudController.Instance.SetFreeRoamVisible(false);
            _dim.style.display = DisplayStyle.Flex;
            SelectTab(_tab);
            WorkshopAudio.Play2D("ui_click", 0.4f);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _dim.style.display = DisplayStyle.None;
            CursorController.ExitMenu();
            HudController.Instance.SetFreeRoamVisible(true);
            WorkshopAudio.Play2D("ui_click", 0.3f, 0.9f);
        }

        private ScrollView _scroll;

        private List<Button> ContentButtons()
        {
            var list = new List<Button>();
            _content.Query<Button>().ForEach(b => { if (b.enabledInHierarchy && b.resolvedStyle.display != DisplayStyle.None) list.Add(b); });
            return list;
        }

        private void OnNavigationMove(NavigationMoveEvent e)
        {
            if (!IsOpen) return;
            var focused = _root.panel?.focusController?.focusedElement as VisualElement;
            var buttons = ContentButtons();
            int idx = focused is Button fb ? buttons.IndexOf(fb) : -1;
            bool onTab = focused is Button tb && _tabs.Contains(tb);
            Button target = null;
            switch (e.direction)
            {
                case NavigationMoveEvent.Direction.Down:
                    if (onTab || focused == null) { if (buttons.Count > 0) target = buttons[0]; }
                    else if (idx >= 0 && idx < buttons.Count - 1) target = buttons[idx + 1];
                    else return;
                    break;
                case NavigationMoveEvent.Direction.Up:
                    if (idx > 0) target = buttons[idx - 1];
                    else if (idx == 0) target = _tabs[_tab];
                    else return;
                    break;
                case NavigationMoveEvent.Direction.Left:
                case NavigationMoveEvent.Direction.Right:
                    if (idx < 0) return;                    // on the tab row the default ring already moves between tabs
                    SelectTab((_tab + (e.direction == NavigationMoveEvent.Direction.Right ? 1 : _tabs.Count - 1)) % _tabs.Count);
                    e.StopPropagation();
                    _root.panel.focusController.IgnoreEvent(e);
                    return;
                default:
                    return;
            }
            if (target == null) return;
            target.Focus();
            if (buttons.Contains(target)) _scroll.ScrollTo(target);
            e.StopPropagation();
            _root.panel.focusController.IgnoreEvent(e);
        }

        /// <summary>Dev/QA: show a tab directly.</summary>
        public void ShowTab(int i) => SelectTab(i);

        private void SelectTab(int i)
        {
            _tab = i;
            for (int k = 0; k < _tabs.Count; k++)
            {
                if (k == i) _tabs[k].AddToClassList("tab-active"); else _tabs[k].RemoveFromClassList("tab-active");
            }
            Refresh();
            _tabs[i].Focus();
        }

        private void Refresh()
        {
            if (!IsOpen || _s.State == null) return;
            _cash.text = UiKit.Money(_s.State.Cash);
            // a purchase rebuilds the list; keep a controller user's place instead of dropping focus
            var focused = _root.panel?.focusController?.focusedElement as Button;
            int keep = focused != null ? ContentButtons().IndexOf(focused) : -1;
            _content.Clear();
            _detail.Clear();
            _detailKey = null;
            switch (_tab)
            {
                case 0: BuildSuppliers(); break;
                case 1: BuildUpgrades(); break;
                case 2: BuildCollection(); break;
                default: BuildStats(); break;
            }
            if (keep >= 0)
            {
                // the new buttons are not laid out yet, so focusing them now is a no-op: do it on the next panel update
                int idx = keep;
                _panel.schedule.Execute(() =>
                {
                    if (!IsOpen) return;
                    var buttons = ContentButtons();
                    if (buttons.Count > 0) { var b = buttons[Mathf.Min(idx, buttons.Count - 1)]; b.Focus(); _scroll.ScrollTo(b); }
                    else _tabs[_tab].Focus();
                });
            }
        }

        // ---- Suppliers -----------------------------------------------------------------------
        private void BuildSuppliers()
        {
            _subtitle.text = "Order mystery crates. Delivery is immediate, to the pallet by the door.";
            var st = _s.State;
            int pallet = _s.Crates.Count;
            if (pallet >= 3) UiKit.Label(_content, $"The receiving pallet is getting full ({pallet} crates). Open or break down a crate before ordering more.", "muted");
            var openAsks = new List<Commission>();
            foreach (var c in st.Commissions) if (!c.Fulfilled) openAsks.Add(c);
            if (openAsks.Count > 0)
            {
                UiKit.Label(_content, "BUYERS ASKING", "section");
                foreach (var c in openAsks)
                {
                    var ask = UiKit.Box(_content, "item-card");
                    ask.style.borderLeftWidth = 4; ask.style.borderLeftColor = new Color(0.85f, 0.7f, 0.35f);
                    UiKit.Label(ask, Market.Describe(c), "item-title", "medium");
                    UiKit.Label(ask, "Put a piece that fits in the dealer outbox: the intercom sends it to them at their price. No hurry; the request stands.", "item-sub");
                }
            }
            foreach (var sup in SupplierCatalog.All)
            {
                bool unlocked = st.HasSupplier(sup.Id);
                if (sup.Occasional && !unlocked) continue;                      // occasional lots are not advertised before they exist
                if (sup.Occasional && !Market.Available(st, sup)) continue;     // ... and only show while on offer
                bool premiumTease = sup.Id == SupplierCatalog.Premium && !unlocked;
                var card = UiKit.Box(_content, "row-card");
                card.style.borderLeftColor = unlocked ? sup.Accent : new Color(0.3f, 0.3f, 0.3f);
                var row = UiKit.Box(card, "row");
                row.style.alignItems = Align.Center;
                var sw = UiKit.Box(row, "swatch");
                sw.style.backgroundColor = unlocked ? sup.Accent * 0.32f : new Color(0.16f, 0.16f, 0.17f);
                UiKit.Box(sw, "swatch-core").style.backgroundColor = unlocked ? sup.Accent : new Color(0.24f, 0.24f, 0.26f);
                var text = UiKit.Box(row, "grow");
                UiKit.Label(text, sup.Name, "row-title");
                UiKit.Label(text, sup.Tagline, "row-sub");
                var tags = UiKit.Box(text, "row");
                tags.style.marginTop = 6;
                UiKit.Label(tags, sup.RockCountLabel.ToUpper(), "tag");
                UiKit.Label(tags, VarianceTag(sup), "tag");
                if (sup.Occasional) UiKit.Label(tags, "ON OFFER", "tag");
                if (!unlocked) UiKit.Label(tags, "LOCKED", "tag", "tag-locked");
                var side = UiKit.Box(row);
                side.style.alignItems = Align.FlexEnd;
                side.style.minWidth = 168;
                UiKit.Label(side, UiKit.Money(sup.Price), "row-price");
                var supplier = sup;
                void Detail() => SupplierDetail(supplier, unlocked, premiumTease);
                card.RegisterCallback<PointerEnterEvent>(_ => Detail());
                if (unlocked)
                {
                    bool afford = _s.CanAfford(sup.Price);
                    var buy = UiKit.Button(side, afford ? "Order crate" : $"{UiKit.Money(sup.Price - st.Cash)} more", () => Buy(supplier), afford ? "btn-primary" : "");
                    buy.style.marginTop = 8;
                    buy.SetEnabled(afford && pallet < 4);
                    buy.RegisterCallback<FocusInEvent>(_ => Detail());
                }
                else
                {
                    var look = UiKit.Button(side, "Details", Detail, "btn-ghost");
                    look.style.marginTop = 8;
                    look.RegisterCallback<FocusInEvent>(_ => Detail());
                }
                if (_detailKey == null) { _detailKey = sup.Id; Detail(); }
            }
        }

        /// <summary>The right-hand card: everything long about the highlighted supplier.</summary>
        private void SupplierDetail(SupplierDefinition sup, bool unlocked, bool premiumTease)
        {
            _detailKey = sup.Id;
            _detail.Clear();
            var head = UiKit.Box(_detail, "row");
            head.style.alignItems = Align.Center;
            var sw = UiKit.Box(head, "swatch");
            sw.style.backgroundColor = unlocked ? sup.Accent * 0.32f : new Color(0.16f, 0.16f, 0.17f);
            UiKit.Box(sw, "swatch-core").style.backgroundColor = unlocked ? sup.Accent : new Color(0.24f, 0.24f, 0.26f);
            var ht = UiKit.Box(head, "grow");
            UiKit.Label(ht, sup.Name, "detail-title");
            UiKit.Label(ht, sup.Tagline, "detail-sub");
            UiKit.Rule(_detail);
            var desc = UiKit.Label(_detail, unlocked ? sup.Description : sup.UnlockHint, "detail-note");
            desc.style.marginTop = 0;
            if (unlocked)
            {
                UiKit.Rule(_detail);
                void Line(string k, string v)
                {
                    UiKit.Label(_detail, k.ToUpper(), "caption").style.marginTop = 8;
                    UiKit.Label(_detail, v, "detail-note").style.marginTop = 1;
                }
                Line("Expect", sup.Character);
                Line("Risk", sup.Risk);
                Line("Minerals", sup.Minerals);
                Line("Look for", sup.Clue);
            }
            else if (premiumTease)
            {
                UiKit.Rule(_detail);
                UiKit.Kv(_detail, "Collection value", UiKit.Money(_s.State.CollectionValue()) + " / $1,500");
            }
            UiKit.Rule(_detail);
            UiKit.Kv(_detail, "Crate price", UiKit.Money(sup.Price), "accent");
            UiKit.Kv(_detail, "Rocks", sup.RockCountLabel);
            UiKit.Kv(_detail, "Character", VarianceTag(sup));
        }

        private static string VarianceTag(SupplierDefinition sup)
        {
            return sup.Id switch
            {
                SupplierCatalog.Local => "HIGH VARIANCE",
                SupplierCatalog.Regional => "RELIABLE",
                SupplierCatalog.AmethystLot => "FOCUSED",
                SupplierCatalog.Estate => "GAMBLE",
                SupplierCatalog.CuttingRough => "SAW MATERIAL",
                SupplierCatalog.DesertPocket => "DELICATE",
                SupplierCatalog.OversizedLot => "HEAVY",
                SupplierCatalog.Network => "TRADED",
                SupplierCatalog.Showcase => "ONE LOCALITY",
                SupplierCatalog.Damaged => "CHIPPED, CHEAP",
                SupplierCatalog.Specialty => "RARE FAMILIES",
                _ => "DISPLAY GRADE",
            };
        }

        private void Buy(SupplierDefinition sup)
        {
            if (_s.BuyCrate(sup.Id, out string err))
            {
                Refresh();
            }
            else
            {
                _s.Notify(err, NotificationKind.Warning);
                WorkshopAudio.Play2D("ui_error", 0.5f);
            }
        }

        // ---- Upgrades ------------------------------------------------------------------------
        private void BuildUpgrades()
        {
            _subtitle.text = "Bench and workshop upgrades. Each one changes how you work, not just a number.";
            var st = _s.State;
            var list = new List<UpgradeDefinition>(UpgradeCatalog.All);
            list.Sort((a, b) => a.Order.CompareTo(b.Order));
            foreach (var up in list)
            {
                bool owned = st.HasUpgrade(up.Id);
                var card = UiKit.Box(_content, "row-card");
                card.style.borderLeftColor = owned ? new Color(0.31f, 0.84f, 0.5f) : new Color(0.55f, 0.36f, 0.96f);
                var row = UiKit.Box(card, "row");
                row.style.alignItems = Align.Center;
                var sw = UiKit.Box(row, "swatch");
                sw.style.backgroundColor = owned ? new Color(0.1f, 0.22f, 0.15f) : new Color(0.15f, 0.12f, 0.23f);
                UiKit.Box(sw, "swatch-core").style.backgroundColor = owned ? new Color(0.31f, 0.84f, 0.5f) : new Color(0.55f, 0.36f, 0.96f);
                var text = UiKit.Box(row, "grow");
                UiKit.Label(text, up.Name, "row-title");
                UiKit.Label(text, up.Description, "row-sub");
                var side = UiKit.Box(row);
                side.style.alignItems = Align.FlexEnd;
                side.style.minWidth = 168;
                var upgrade = up;
                void Detail() => UpgradeDetail(upgrade, owned);
                card.RegisterCallback<PointerEnterEvent>(_ => Detail());
                if (owned) UiKit.Label(side, "INSTALLED", "tag", "tag-owned");
                else
                {
                    if (up.Id == UpgradeCatalog.SawBlade && st.HasUpgrade(UpgradeCatalog.TrimSaw))
                        UiKit.Label(side, $"Blade wear {st.BladeWear * 100f:F0}%", "row-sub", st.BladeWear >= 0.75f ? "warn" : "muted");
                    else if (up.Id == UpgradeCatalog.Stage2 && st.WorkshopStage < 2)
                        UiKit.Label(side, "WORKSHOP EXPANSION", "tag");
                    UiKit.Label(side, UiKit.Money(up.Price), "row-price");
                    bool can = _s.CanBuyUpgrade(up.Id, out string why);
                    var buy = UiKit.Button(side, can ? (up.Consumable ? "Replace" : "Purchase") : why, () => BuyUpgrade(upgrade), can ? "btn-primary" : "");
                    buy.style.marginTop = 8;
                    buy.SetEnabled(can);
                    buy.RegisterCallback<FocusInEvent>(_ => Detail());
                }
                if (_detailKey == null) { _detailKey = up.Id; Detail(); }
            }
        }

        private void UpgradeDetail(UpgradeDefinition up, bool owned)
        {
            _detailKey = up.Id;
            _detail.Clear();
            var head = UiKit.Box(_detail, "row");
            head.style.alignItems = Align.Center;
            var sw = UiKit.Box(head, "swatch");
            sw.style.backgroundColor = owned ? new Color(0.1f, 0.22f, 0.15f) : new Color(0.15f, 0.12f, 0.23f);
            UiKit.Box(sw, "swatch-core").style.backgroundColor = owned ? new Color(0.31f, 0.84f, 0.5f) : new Color(0.55f, 0.36f, 0.96f);
            var ht = UiKit.Box(head, "grow");
            UiKit.Label(ht, up.Name, "detail-title");
            UiKit.Label(ht, owned ? "Installed" : "Available", "detail-sub");
            UiKit.Rule(_detail);
            UiKit.Label(_detail, up.Description, "detail-note").style.marginTop = 0;
            UiKit.Label(_detail, "WHAT IT CHANGES", "caption").style.marginTop = 12;
            UiKit.Label(_detail, up.Effect, "detail-note").style.marginTop = 1;
            UiKit.Rule(_detail);
            UiKit.Kv(_detail, "Price", UiKit.Money(up.Price), "accent");
            UiKit.Kv(_detail, "Status", owned ? "Installed" : (_s.CanBuyUpgrade(up.Id, out string why) ? "Ready to buy" : why),
                     owned ? "success" : null);
        }

        /// <summary>Right-hand card for the highlighted mineral family.</summary>
        private void FamilyDetail(MineralFamily fam, EncyclopediaEntry entry, bool found)
        {
            _detailKey = fam.Name;
            _detail.Clear();
            var pal = fam.Palettes[0];
            var plate = UiKit.Box(_detail, "detail-plate");
            if (found) SpecimenThumbnailer.Instance.Family(plate, fam.Id, SpecimenThumbnailer.Ground);
            else plate.style.backgroundColor = new Color(0.115f, 0.11f, 0.125f);
            UiKit.Label(_detail, found ? fam.Name : "Undiscovered", "detail-title").style.marginTop = 12;
            UiKit.Label(_detail, found ? fam.Description : "Crack more rocks to learn what this is.", "detail-note").style.marginTop = 4;
            if (!found) return;
            if (!string.IsNullOrEmpty(fam.FieldNote))
            {
                UiKit.Label(_detail, "FIELD NOTE", "caption").style.marginTop = 12;
                UiKit.Label(_detail, fam.FieldNote, "detail-note").style.marginTop = 1;
            }
            UiKit.Rule(_detail);
            UiKit.Kv(_detail, "Found", entry.Found.ToString());
            UiKit.Kv(_detail, "Best value", UiKit.Money(entry.BestValue), "accent");
            UiKit.Kv(_detail, "Largest", $"{entry.LargestMassKg:F1} kg");
            if (entry.CavitiesSeen.Count > 0) UiKit.Kv(_detail, "Formations", string.Join(", ", entry.CavitiesSeen));
        }

        private void BuyUpgrade(UpgradeDefinition up)
        {
            if (_s.BuyUpgrade(up.Id, out string err)) Refresh();
            else { _s.Notify(err, NotificationKind.Warning); WorkshopAudio.Play2D("ui_error", 0.5f); }
        }

        // ---- Collection ----------------------------------------------------------------------
        private void BuildCollection()
        {
            var st = _s.State;
            int displayed = st.DisplayedCount();
            _subtitle.text = $"Display cabinet {displayed}/{st.DisplayCapacity}  •  Collection value {UiKit.Money(st.CollectionValue())}  •  Prestige tier {st.Prestige}";
            var grid = UiKit.Box(_content, "row");
            grid.style.flexWrap = Wrap.Wrap;
            int known = 0;
            foreach (var fam in MineralCatalog.All)
            {
                EncyclopediaEntry entry = null;
                foreach (var e in st.Encyclopedia) if (e.Mineral == fam.Id) entry = e;
                bool found = entry != null && entry.Found > 0;
                if (found) known++;
                var pal = fam.Palettes[0];
                // the pack's collection grid: a specimen plate on top, the name and family under it, the value on the line below
                var tile = UiKit.Box(grid, "tile");
                var plate = UiKit.Box(tile, "tile-plate");
                if (found) SpecimenThumbnailer.Instance.Family(plate, fam.Id, SpecimenThumbnailer.Ground);
                else plate.style.backgroundColor = new Color(0.115f, 0.11f, 0.125f);
                var body = UiKit.Box(tile, "tile-body");
                UiKit.Label(body, found ? fam.Name : "Undiscovered", "row-title");
                UiKit.Label(body, found ? fam.Description : "Crack more rocks to learn what this is.", "row-sub");
                if (found)
                {
                    var foot = UiKit.Box(body, "row");
                    foot.style.marginTop = 8;
                    foot.style.alignItems = Align.Center;
                    int tier = entry.BestValue >= 1200f ? 4 : entry.BestValue >= 600f ? 3 : entry.BestValue >= 250f ? 2 : entry.BestValue >= 90f ? 1 : 0;
                    UiKit.Rarity(foot, tier);
                    var spacer = UiKit.Box(foot, "grow");
                    UiKit.Label(foot, UiKit.Money(entry.BestValue), "row-price");
                    UiKit.Label(body, $"Found {entry.Found}  \u2022  largest {entry.LargestMassKg:F1} kg", "row-sub");
                    if (entry.TraitsSeen.Count > 0)
                    {
                        var traits = new List<string>();
                        foreach (var t in entry.TraitsSeen) if (System.Enum.TryParse<RareTrait>(t, out var rt)) traits.Add(Valuation.TraitName(rt));
                        UiKit.Label(body, "Traits: " + string.Join(", ", traits), "row-sub");
                    }
                }
                var famRef = fam; var entryRef = entry; bool foundRef = found;
                tile.RegisterCallback<PointerEnterEvent>(_ => FamilyDetail(famRef, entryRef, foundRef));
                if (_detailKey == null && found) { _detailKey = fam.Name; FamilyDetail(famRef, entryRef, true); }
            }
            UiKit.Label(_content, $"{known} of {MineralCatalog.All.Count} mineral families discovered", "muted");

            // the career's conclusion: where the exhibition stands, and the button to open it
            UiKit.Label(_content, $"CURATOR'S EXHIBITION  •  standing: {Reputation.Word(st)}", "section");
            foreach (var ax in Exhibition.Axes(st))
            {
                var row = UiKit.Box(_content, "stat-row");
                var kl = UiKit.Label(row, (ax.Met ? "✓  " : "○  ") + ax.Title, ax.Met ? "item-sub" : "item-title"); kl.style.flexGrow = 1;
                UiKit.Label(row, ax.Detail, "muted");
            }
            if (st.ExhibitionsHeld > 0) UiKit.Label(_content, $"Held {st.ExhibitionsHeld} time{(st.ExhibitionsHeld == 1 ? "" : "s")}, last on {new System.DateTime(st.ExhibitionCompletedTicks).ToString("d MMM yyyy")}.", "muted");
            var director = ExhibitionDirector.Instance;
            if (Exhibition.Eligible(st) && director != null)
            {
                int on = director.PlinthCount(st);
                var open = UiKit.Button(_content, on >= 3 ? "Open the exhibition" : $"Set three pieces on the gallery plinths ({on} of 3)", () => { if (on >= 3) { Close(); director.Open(); } }, on >= 3 ? "btn-primary" : "");
                open.SetEnabled(on >= 3);
            }
            else UiKit.Label(_content, Reputation.NextStep(st), "muted");

            // collection goals: what the cabinet is working toward
            UiKit.Label(_content, $"COLLECTION GOALS  •  {CollectionGoals.DoneCount(st)} of {CollectionGoals.All.Length}", "section");
            foreach (var g in CollectionGoals.All)
            {
                var p = g.Progress(st);
                bool done = p.have >= p.need;
                var row = UiKit.Box(_content, "stat-row");
                var kl = UiKit.Label(row, (done ? "✓  " : "○  ") + g.Title, done ? "item-sub" : "item-title");
                kl.style.flexGrow = 1;
                UiKit.Label(row, done ? "done" : (p.need > 1 ? $"{p.have} / {p.need}" : ""), "muted");
                if (!done) UiKit.Label(_content, g.Detail, "muted").style.marginLeft = 24;
            }

            // lots with the auction house
            if (st.AuctionLots.Count > 0)
            {
                UiKit.Label(_content, "AT AUCTION", "section");
                foreach (var lot in st.AuctionLots) { var ll = UiKit.Label(_content, Auction.LotLine(st, lot), "item-sub"); ll.style.whiteSpace = WhiteSpace.Normal; ll.style.marginLeft = 24; }
            }

            // the pieces on display, grouped by kind, with their short histories; a star marks a favourite (never sold by mistake)
            var kept = new List<SpecimenRecord>();
            foreach (var r in st.Specimens) if (r.Location == SpecimenLocation.DisplaySlot) kept.Add(r);
            if (kept.Count > 0)
            {
                kept.Sort((a, b) => b.EstimatedValue().CompareTo(a.EstimatedValue()));
                var groups = new (string title, System.Func<SpecimenRecord, bool> pick)[]
                {
                    ("NATURAL SPLITS", r => !r.IsPiece), ("SAWN", r => r.IsPiece && r.Polish < 0.9f), ("POLISHED", r => r.IsPiece && r.Polish >= 0.9f),
                };
                foreach (var (title, pick) in groups)
                {
                    bool any = false;
                    foreach (var r in kept) if (pick(r)) { any = true; break; }
                    if (!any) continue;
                    UiKit.Label(_content, "ON DISPLAY  •  " + title, "section");
                    foreach (var r in kept)
                    {
                        if (!pick(r)) continue;
                        var card = UiKit.Box(_content, "item-card");
                        var row = UiKit.Box(card, "row"); row.style.alignItems = Align.Center;
                        var text = UiKit.Box(row, "grow");
                        UiKit.Label(text, $"{(r.Favorite ? "★ " : "")}{r.LocationIndex + 1}.  {r.DisplayName}" + (r.ConsignedAtCrate > 0 ? "   (consigned: the courier collects it with the next delivery)" : ""), "item-title", "medium");
                        UiKit.Label(text, Provenance(r), "item-sub");
                        string hist = HistoryText(r, 5);
                        if (hist.Length > 0) { var hl = UiKit.Label(text, hist, "muted"); hl.style.whiteSpace = WhiteSpace.Normal; }
                        var side = UiKit.Box(row); side.style.alignItems = Align.FlexEnd; side.style.minWidth = 150;
                        var rec = r;
                        var fav = UiKit.Button(side, rec.Favorite ? "Unstar" : "★ Favourite", () => { rec.Favorite = !rec.Favorite; _s.QueueSave("favorite"); _s.RaiseStateChanged(); Refresh(); }, "");
                        fav.style.marginTop = 4;
                        // a name of your own for a kept piece (the card, the label and the exhibition all use it)
                        var nameField = new TextField { value = rec.CustomName ?? "", maxLength = 40 };
                        nameField.style.display = DisplayStyle.None; nameField.style.marginTop = 6; nameField.style.width = 300;
                        card.Add(nameField);
                        var nameBtn = UiKit.Button(side, string.IsNullOrEmpty(rec.CustomName) ? "Name it" : "Rename", () =>
                        {
                            bool open = nameField.style.display == DisplayStyle.Flex;
                            nameField.style.display = open ? DisplayStyle.None : DisplayStyle.Flex;
                            if (!open) nameField.Focus();
                        }, "");
                        nameBtn.style.marginTop = 4;
                        // the auction: exceptional pieces only, a reserve under them, the house's cut on top
                        if (Auction.IsEligible(rec) || rec.ConsignedAtCrate > 0)
                        {
                            string cannot = rec.ConsignedAtCrate > 0 ? null : Auction.CannotConsign(st, rec);
                            var auc = UiKit.Button(side, rec.ConsignedAtCrate > 0 ? "Withdraw from auction" : cannot ?? $"Consign  •  est. {UiKit.Money(Auction.Estimate(rec))}", () =>
                            {
                                if (rec.ConsignedAtCrate > 0) { Auction.Withdraw(_s, rec); Refresh(); return; }
                                if (Auction.Consign(_s, rec, out string why)) Refresh(); else _s.Notify(why, NotificationKind.Warning);
                            }, "");
                            auc.style.marginTop = 4;
                            auc.SetEnabled(rec.ConsignedAtCrate > 0 || cannot == null);
                        }
                        nameField.RegisterCallback<KeyDownEvent>(ev =>
                        {
                            if (ev.keyCode != KeyCode.Return && ev.keyCode != KeyCode.KeypadEnter) return;
                            string v = nameField.value.Trim();
                            rec.CustomName = v.Length > 0 ? v : null;
                            _s.QueueSave("rename"); _s.RaiseStateChanged(); Refresh();
                        });
                    }
                }
            }
        }

        /// <summary>One line of history: where it came from, what opened it, what was done to it, what it is worth.</summary>
        public static string Provenance(SpecimenRecord r, bool brief = false)
        {
            var parts = new List<string>();
            if (brief)
            {
                // the card's footer: where it came from and when; the tablet has the rest
                if (!string.IsNullOrEmpty(r.Locality)) parts.Add(r.Locality);
                var supB = !string.IsNullOrEmpty(r.SupplierId) ? SupplierCatalog.Get(r.SupplierId) : null;
                if (supB != null) parts.Add(supB.Name + (!string.IsNullOrEmpty(r.CrateId) ? $" (lot {r.CrateId})" : ""));
                if (r.DiscoveredAtTicks > 0) parts.Add("found " + new System.DateTime(r.DiscoveredAtTicks).ToString("d MMM yyyy"));
                return string.Join("  •  ", parts);
            }
            var sup = !string.IsNullOrEmpty(r.SupplierId) ? SupplierCatalog.Get(r.SupplierId) : null;
            if (sup != null) parts.Add(sup.Name + (!string.IsNullOrEmpty(r.CrateId) ? $" (lot {r.CrateId})" : ""));
            string tool = r.IsPiece ? "trim saw" : r.ProcessedBy == "hammer" ? "hammer and chisel" : r.ProcessedBy;
            if (!string.IsNullOrEmpty(tool)) parts.Add("opened with the " + tool);
            if (r.DamageFraction > 0.005f) parts.Add($"{r.DamageFraction * 100f:F0}% crystal damage"); else if (r.IsOpened) parts.Add("no damage");
            if (r.Polish > 0.5f) parts.Add("polished");
            parts.Add(r.Appraised ? "appraised " + UiKit.Money(r.AppraisedValue) : "unappraised");
            if (!string.IsNullOrEmpty(r.Locality)) parts.Insert(0, r.Locality);
            if (r.OriginalMassKg > 0.01f && r.IsPiece) parts.Add($"from a {r.OriginalMassKg:F1} kg rock");
            if (r.AcquisitionCost > 0.001f) parts.Add($"cost {UiKit.Money(r.AcquisitionCost)} in the crate");
            if (r.Predicted) parts.Add("called " + Player.PlayerInteractor.CallWord(r));
            if (r.DiscoveredAtTicks > 0) parts.Add("found " + new System.DateTime(r.DiscoveredAtTicks).ToString("d MMM yyyy"));
            else if (r.OpenedAtTicks > 0) parts.Add("opened " + new System.DateTime(r.OpenedAtTicks).ToString("d MMM yyyy"));
            return string.Join("  •  ", parts);
        }

        /// <summary>The specimen's life, newest last, as short dated lines.</summary>
        public static string HistoryText(SpecimenRecord r, int max = 8)
        {
            if (r == null || r.History == null || r.History.Count == 0) return "";
            var lines = new List<string>();
            int start = Mathf.Max(0, r.History.Count - max);
            for (int i = start; i < r.History.Count; i++)
            {
                var ev = r.History[i];
                string when = new System.DateTime(ev.Ticks).ToString("d MMM");
                string val = ev.Value > 0.001f ? " " + UiKit.Money(ev.Value) : "";
                lines.Add($"{when}  {ev.Kind}{val}{(string.IsNullOrEmpty(ev.Note) ? "" : "  •  " + ev.Note)}");
            }
            return string.Join("\n", lines);
        }

        // ---- Stats ----------------------------------------------------------------------------
        private void BuildStats()
        {
            _subtitle.text = "Career statistics";
            var st = _s.State.Stats;
            void Row(string k, string v)
            {
                var r = UiKit.Box(_content, "stat-row");
                UiKit.Label(r, k, "stat-key");
                UiKit.Label(r, v, "stat-val", "medium");
            }
            Row("Play time", System.TimeSpan.FromSeconds(st.PlayTimeSeconds).ToString(@"h\:mm\:ss"));
            Row("Crates purchased", st.CratesPurchased.ToString());
            Row("Rocks processed", st.RocksProcessed.ToString());
            Row("Specimens opened", st.SpecimensOpened.ToString());
            Row("Clean opens", st.CleanOpens.ToString());
            Row("Specimens damaged", st.SpecimensDamaged.ToString());
            Row("Total strikes", st.TotalStrikes.ToString());
            Row("Money spent", UiKit.Money(st.MoneySpent));
            Row("Money earned", UiKit.Money(st.MoneyEarned));
            Row("Biggest sale", st.BiggestSale > 0 ? $"{UiKit.Money(st.BiggestSale)}  ({st.BiggestSaleName})" : "—");
            Row("Highest value kept", st.HighestValueKept > 0 ? $"{UiKit.Money(st.HighestValueKept)}  ({st.HighestValueKeptName})" : "—");
            Row("Largest specimen", st.LargestSpecimenKg > 0 ? $"{st.LargestSpecimenKg:F2} kg  ({st.LargestSpecimenName})" : "—");
            Row("Most damaged", st.MostDamagedFraction > 0 ? $"{st.MostDamagedFraction * 100f:F0}%  ({st.MostDamagedName})" : "—");
            Row("Rocks washed", st.RocksWashed.ToString());
            Row("Rocks split on the cracker", st.RocksCracked.ToString());
            Row("Calls made in the hand", st.PredictionsMade.ToString());
            if (st.PredictionsMade > 0) Row("Hollow or solid called right", $"{st.HollowCallsRight} of {st.PredictionsMade} ({Mathf.RoundToInt(100f * st.HollowCallsRight / st.PredictionsMade)}%)");
            if (st.PredictionsMade > 0) Row("Grade called within one tier", $"{st.TierCallsRight} of {st.PredictionsMade}");
            Row("Saw cuts / slabs", $"{st.SawCuts} / {st.SlabsCut}");
            Row("Best saw result", st.HighestValueSawResult > 0 ? $"{UiKit.Money(st.HighestValueSawResult)}  ({st.HighestValueSawResultName})" : "—");
            Row("Best hammer result", st.HighestValueHammerResult > 0 ? $"{UiKit.Money(st.HighestValueHammerResult)}  ({st.HighestValueHammerResultName})" : "—");
            Row("Largest slab face", st.LargestSlabFaceCm2 > 0 ? $"{st.LargestSlabFaceCm2:F0} cm²  ({st.LargestSlabName})" : "—");
            Row("Pieces polished", st.PiecesPolished.ToString());
            Row("Best polished piece", st.BestPolishedValue > 0 ? $"{UiKit.Money(st.BestPolishedValue)}  ({st.BestPolishedName})" : "—");
            Row("Specimens kept", _s.State.DisplayedCount().ToString());
            Row("Specimens sold", st.SpecimensSold.ToString());
            Row("Retail sales", st.RetailSales > 0 ? $"{st.RetailSales}  ({UiKit.Money(st.RetailRevenue)})" : "0");
            Row("Best retail sale", st.BiggestRetailSale > 0 ? $"{UiKit.Money(st.BiggestRetailSale)}  ({st.BiggestRetailSaleName})" : "—");
            Row("Customers served / left empty-handed", $"{st.CustomersServed} / {st.CustomersLeftEmptyHanded}");
            Row("On sale now", _s.State.ForSaleCount().ToString());
            Row("Collection value", UiKit.Money(_s.State.CollectionValue()));
            Row("Mineral families discovered", _s.State.Encyclopedia.Count + " / " + MineralCatalog.All.Count);
        }
    }
}
