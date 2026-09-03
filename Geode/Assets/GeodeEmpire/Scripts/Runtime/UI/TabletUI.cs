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

        private VisualElement _root, _dim, _panel, _content;
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
            _panel.style.width = 1120;
            _panel.style.height = 720;
            var header = UiKit.Box(_panel, "row");
            header.style.alignItems = Align.Center;
            var titleBox = UiKit.Box(header, "grow");
            UiKit.Label(titleBox, "WORKSHOP TABLET", "panel-title", "bold");
            _subtitle = UiKit.Label(titleBox, "", "panel-subtitle");
            _cash = UiKit.Label(header, "$0", "price", "bold");
            _cash.style.fontSize = 30;
            var close = UiKit.Button(header, "Close", Close, "btn-ghost");
            close.style.marginLeft = 20;
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
            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.AddToClassList("grow");
            _panel.Add(_scroll);
            _content = _scroll.contentContainer;
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
            foreach (var sup in SupplierCatalog.All)
            {
                bool unlocked = st.HasSupplier(sup.Id);
                bool premiumTease = sup.Id == SupplierCatalog.Premium && !unlocked;
                var card = UiKit.Box(_content, "item-card");
                card.style.borderLeftWidth = 4;
                card.style.borderLeftColor = unlocked ? sup.Accent : new Color(0.3f, 0.3f, 0.3f);
                var row = UiKit.Box(card, "row");
                row.style.alignItems = Align.Center;
                var text = UiKit.Box(row, "grow");
                UiKit.Label(text, sup.Name, "item-title", "medium");
                UiKit.Label(text, sup.Tagline, "item-sub");
                var tags = UiKit.Box(text, "row");
                tags.style.marginTop = 8;
                UiKit.Label(tags, sup.RockCountLabel.ToUpper(), "tag");
                UiKit.Label(tags, VarianceTag(sup), "tag");
                if (!unlocked) UiKit.Label(tags, "LOCKED", "tag", "tag-locked");
                var desc = UiKit.Label(text, unlocked ? sup.Description : sup.UnlockHint, "item-desc");
                if (unlocked)
                {
                    void Line(string k, string v) { var r2 = UiKit.Box(text, "row"); r2.style.marginTop = 4; var kl = UiKit.Label(r2, k, "item-sub"); kl.style.width = 92; var vl = UiKit.Label(r2, v, "item-sub"); vl.style.whiteSpace = WhiteSpace.Normal; vl.style.flexShrink = 1; }
                    Line("Expect", sup.Character);
                    Line("Risk", sup.Risk);
                    Line("Minerals", sup.Minerals);
                    Line("Look for", sup.Clue);
                }
                var side = UiKit.Box(row);
                side.style.alignItems = Align.FlexEnd;
                side.style.minWidth = 180;
                UiKit.Label(side, UiKit.Money(sup.Price), "price", "bold");
                if (unlocked)
                {
                    bool afford = _s.CanAfford(sup.Price);
                    var buy = UiKit.Button(side, afford ? "Order crate" : "Not enough cash", () => Buy(sup), afford ? "btn-primary" : "");
                    buy.style.marginTop = 8;
                    buy.SetEnabled(afford && pallet < 4);
                }
                else if (premiumTease)
                {
                    UiKit.Label(side, $"Collection value {UiKit.Money(st.CollectionValue())} / $1,500", "muted");
                }
            }
        }

        private static string VarianceTag(SupplierDefinition sup)
        {
            return sup.Id switch
            {
                SupplierCatalog.Local => "HIGH VARIANCE",
                SupplierCatalog.Regional => "RELIABLE",
                SupplierCatalog.AmethystLot => "FOCUSED",
                SupplierCatalog.Estate => "GAMBLE",
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
                var card = UiKit.Box(_content, "item-card");
                var row = UiKit.Box(card, "row");
                row.style.alignItems = Align.Center;
                var text = UiKit.Box(row, "grow");
                UiKit.Label(text, up.Name, "item-title", "medium");
                UiKit.Label(text, up.Description, "item-sub");
                UiKit.Label(text, up.Effect, "item-desc");
                var side = UiKit.Box(row);
                side.style.alignItems = Align.FlexEnd;
                side.style.minWidth = 180;
                if (owned) UiKit.Label(side, "INSTALLED", "tag", "tag-owned");
                else
                {
                    UiKit.Label(side, UiKit.Money(up.Price), "price", "bold");
                    bool can = _s.CanBuyUpgrade(up.Id, out string why);
                    var buy = UiKit.Button(side, can ? "Buy" : why, () => BuyUpgrade(up), can ? "btn-primary" : "");
                    buy.style.marginTop = 8;
                    buy.SetEnabled(can);
                }
            }
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
                var card = UiKit.Box(grid, "item-card");
                card.style.width = 330;
                card.style.marginRight = 10;
                var pal = fam.Palettes[0];
                card.style.borderLeftWidth = 4;
                card.style.borderLeftColor = found ? pal.SurfaceA : new Color(0.25f, 0.25f, 0.25f);
                UiKit.Label(card, found ? fam.Name : "Undiscovered", "item-title", "medium");
                if (found)
                {
                    UiKit.Label(card, fam.Description, "item-desc");
                    UiKit.Label(card, $"Found {entry.Found}  •  Best {UiKit.Money(entry.BestValue)}  •  Largest {entry.LargestMassKg:F1} kg", "item-sub");
                    if (entry.TraitsSeen.Count > 0)
                    {
                        var traits = new List<string>();
                        foreach (var t in entry.TraitsSeen) if (System.Enum.TryParse<RareTrait>(t, out var rt)) traits.Add(Valuation.TraitName(rt));
                        UiKit.Label(card, "Traits seen: " + string.Join(", ", traits), "item-sub");
                    }
                    UiKit.Label(card, "Formations: " + string.Join(", ", entry.CavitiesSeen), "item-sub");
                }
                else
                {
                    UiKit.Label(card, "Crack more rocks to learn what this is.", "item-sub");
                }
            }
            UiKit.Label(_content, $"{known} of {MineralCatalog.All.Count} mineral families discovered", "muted");
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
            Row("Specimens kept", _s.State.DisplayedCount().ToString());
            Row("Specimens sold", st.SpecimensSold.ToString());
            Row("Collection value", UiKit.Money(_s.State.CollectionValue()));
            Row("Mineral families discovered", _s.State.Encyclopedia.Count + " / " + MineralCatalog.All.Count);
        }
    }
}
