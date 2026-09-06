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
        private Retail.RetailShop _retailShop;
        private int _businessCustomerCount = -1;

        private void Awake() => Instance = this;

        private void Start()
        {
            _s = GameSession.Instance;
            var hud = HudController.Instance;
            _root = hud.GetComponent<UIDocument>().rootVisualElement;
            _dim = UiKit.Box(_root, "panel-dim");
            _dim.style.display = DisplayStyle.None;
            _panel = UiKit.Box(_dim, "panel");
            // a fixed 1500x880 hung off both edges once the interface scale shrank the reference resolution
            // below it (1.4x gives 1371x771): clamp to the panel and let the body scroll
            _panel.style.width = 1500;
            _panel.style.height = 880;
            _panel.style.maxWidth = Length.Percent(96);
            _panel.style.maxHeight = Length.Percent(94);
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
            string[] names = { "Suppliers", "Upgrades", "Collection", "Business", "Stats" };
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
            _retailShop = Retail.RetailShop.Instance;
            if (_retailShop != null) _retailShop.Changed += OnRetailChanged;
            _s.CashChanged += (c, d) => { if (IsOpen) Refresh(); };
            _dim.RegisterCallback<NavigationCancelEvent>(e => { Close(); e.StopPropagation(); });
        }

        private void OnDestroy()
        {
            OrderTablet.Opened -= Open;
            if (_s != null) _s.StateChanged -= Refresh;
            if (_retailShop != null) _retailShop.Changed -= OnRetailChanged;
            if (Instance == this) Instance = null;
        }

        private void OnRetailChanged()
        {
            if (IsOpen && _tab == 3 && _retailShop != null && _retailShop.Customers.Count != _businessCustomerCount)
                Refresh();
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
            HudController.Instance.SetStatusVisible(false);
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
            HudController.Instance.SetStatusVisible(true);
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
                case 3: BuildBusiness(); BusinessDetail(); break;
                default: BuildStats(); StatsDetail(); break;
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
            _subtitle.text = Workshop.PremisesExpansion.BackRoomOpen
                ? "Order mystery crates. Delivery is immediate, onto the pallets in the receiving bay."
                : "Order mystery crates. Delivery is immediate, onto the goods-in pallet in the corner of the workshop.";
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
                CrateTile(row, sup, unlocked, 82f);
                var text = UiKit.Box(row, "upg-text");
                UiKit.Label(text, sup.Name, "row-title");
                UiKit.Label(text, sup.Tagline, "row-sub");
                var tags = UiKit.Box(text, "row");
                tags.style.marginTop = 6;
                UiKit.Label(tags, sup.RockCountLabel.ToUpper(), "tag");
                UiKit.Label(tags, VarianceTag(sup), "tag");
                if (sup.Occasional) UiKit.Label(tags, "ON OFFER", "tag");
                if (!unlocked) UiKit.Label(tags, "LOCKED", "tag", "tag-locked");
                var side = UiKit.Box(row, "upg-side");
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
            var plate = CrateTile(_detail, sup, unlocked, 999f);
            plate.style.width = Length.Percent(100);
            plate.style.height = 150;
            var head = UiKit.Box(_detail, "row");
            head.style.alignItems = Align.Center;
            head.style.marginTop = 10;
            var ht = UiKit.Box(head, "grow");
            UiKit.Label(ht, sup.Name, "detail-title");
            UiKit.Label(ht, sup.Tagline, "detail-sub");
            // §64: the numbers that decide the purchase come first. The card scrolls, and price, rocks and what
            // the till looks like afterwards were below the fold under four paragraphs of flavour.
            UiKit.Rule(_detail);
            var st = _s.State;
            UiKit.Kv(_detail, "Crate price", UiKit.Money(sup.Price), "accent");
            UiKit.Kv(_detail, "Till after", UiKit.Money(st.Cash - sup.Price), st.Cash >= sup.Price ? "" : "danger");
            UiKit.Kv(_detail, "Rocks", sup.RockCountLabel);
            UiKit.Kv(_detail, "Character", VarianceTag(sup));
            UiKit.Kv(_detail, "Delivered to", "the receiving bay, at once");
            int waiting = UnopenedCrates();
            if (waiting > 0) UiKit.Kv(_detail, "Crates already waiting", waiting.ToString(), "warn");
            int free = FreeShelfSpace();
            UiKit.Kv(_detail, "Free display and sale slots", free.ToString(), free > 0 ? "" : "warn");
            if (free == 0)
                UiKit.Label(_detail, "Every shelf and sales slot is full. Rocks keep coming, but nothing new goes on show until you sell something.", "build-tip");

            UiKit.Rule(_detail);
            var desc = UiKit.Label(_detail, unlocked ? sup.Description : sup.UnlockHint, "detail-note");
            desc.style.marginTop = 0;
            if (unlocked)
            {
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
        }

        /// <summary>Crates delivered and not yet emptied: the honest answer to "how much is already waiting".</summary>
        private int UnopenedCrates()
        {
            int n = 0;
            foreach (var c in _s.State.Crates) if (c != null && c.Delivered && !c.Opened) n++;
            return n;
        }

        /// <summary>Sale and display slots the player owns and has not filled.</summary>
        private int FreeShelfSpace()
        {
            var st = _s.State;
            int used = 0;
            foreach (var r in st.Specimens)
            {
                if (r == null) continue;
                if (r.Location == Save.SpecimenLocation.SaleSlot || r.Location == Save.SpecimenLocation.DisplaySlot) used++;
            }
            return Mathf.Max(0, st.SaleCapacity + st.DisplayCapacity - used);
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
        /// <summary>
        /// The baked preview for an upgrade (see UpgradeIconBaker). §9.3: an upgrade card has to show the thing
        /// being bought, not a coloured dot repeated twenty-three times.
        /// </summary>
        private static readonly Dictionary<string, Texture2D> _upgradeIcons = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Texture2D> _crateIcons = new Dictionary<string, Texture2D>();

        /// <summary>
        /// §9.2: what turns up on the pallet. Four builds rather than twelve near-identical crates \u2014 what
        /// separates one supplier from another on this screen is its accent, its chips and its price, and what the
        /// picture has to carry is the shape of the delivery.
        /// </summary>
        private static string CrateArt(SupplierDefinition sup)
        {
            if (sup.Occasional || sup.Price >= 700f) return "premium";
            if (sup.RockCountLabel != null && sup.RockCountLabel.Contains("12")) return "bulk";
            if (sup.Price >= 260f) return "bulk";
            return sup.Price >= 150f ? "curated" : "plain";
        }

        private static VisualElement CrateTile(VisualElement parent, SupplierDefinition sup, bool unlocked, float size)
        {
            var tile = UiKit.Box(parent, "upg-tile");
            tile.style.width = size;
            tile.style.height = size * 0.78f;
            string art = CrateArt(sup);
            if (!_crateIcons.TryGetValue(art, out var tex)) { tex = Resources.Load<Texture2D>("UI/Crates/" + art); _crateIcons[art] = tex; }
            if (tex != null)
            {
                tile.style.backgroundImage = new StyleBackground(tex);
                tile.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                if (!unlocked) tile.style.unityBackgroundImageTintColor = new Color(0.42f, 0.41f, 0.45f);
            }
            var pip = UiKit.Box(tile, "crate-pip");
            pip.style.backgroundColor = unlocked ? sup.Accent : new Color(0.30f, 0.30f, 0.33f);
            if (!unlocked) UiKit.Label(tile, "\u25A0", "upg-lock");
            return tile;
        }

        private static Texture2D UpgradeIcon(string id)
        {
            if (_upgradeIcons.TryGetValue(id, out var t)) return t;
            t = Resources.Load<Texture2D>("UI/Upgrades/" + id);
            _upgradeIcons[id] = t;
            return t;
        }

        /// <summary>A framed preview tile; falls back to the category initial when no image was baked.</summary>
        private static VisualElement IconTile(VisualElement parent, UpgradeDefinition up, bool owned, bool locked, float size)
        {
            var tile = UiKit.Box(parent, "upg-tile");
            tile.style.width = size;
            tile.style.height = size * 0.78f;
            var tex = UpgradeIcon(up.Id);
            if (tex != null)
            {
                tile.style.backgroundImage = new StyleBackground(tex);
                tile.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }
            else UiKit.Label(tile, string.IsNullOrEmpty(up.Category) ? "?" : up.Category.Substring(0, 1), "upg-tile-letter");
            if (locked) UiKit.Label(tile, "\u25A0", "upg-lock");
            else if (owned) UiKit.Label(tile, "\u2713", "upg-owned-tick");
            return tile;
        }

        private void BuildUpgrades()
        {
            _subtitle.text = "What the business owns. A purchase changes the room or the way you work \u2014 never only a number.";
            var st = _s.State;
            var list = new List<UpgradeDefinition>(UpgradeCatalog.All);
            list.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Price.CompareTo(b.Price));

            // grouped by what part of the business they are, in the order the career meets them
            string[] order = { "PREMISES", "MACHINES", "BENCH", "RETAIL", "DISPLAYS", "STORAGE" };
            foreach (var cat in order)
            {
                var inCat = new List<UpgradeDefinition>();
                foreach (var u in list) if ((u.Category ?? "BENCH") == cat) inCat.Add(u);
                if (inCat.Count == 0) continue;
                int owns = 0;
                foreach (var u in inCat) if (!u.Consumable && st.HasUpgrade(u.Id)) owns++;
                int buyable = 0;
                foreach (var u in inCat) if (!u.Consumable) buyable++;
                var head = UiKit.Box(_content, "row");
                head.style.alignItems = Align.Center;
                head.style.marginTop = 14;
                UiKit.Label(head, cat, "section").style.marginTop = 0;
                UiKit.Box(head, "grow");
                UiKit.Label(head, owns + " / " + buyable + " owned", "row-sub");
                foreach (var up in inCat) UpgradeCard(up, st);
            }
        }

        private void UpgradeCard(UpgradeDefinition up, Save.GameState st)
        {
            bool owned = !up.Consumable && st.HasUpgrade(up.Id);
            bool locked = !string.IsNullOrEmpty(up.Requires) && !st.HasUpgrade(up.Requires);
            var card = UiKit.Box(_content, "row-card", "upg-card");
            card.style.borderLeftColor = owned ? new Color(0.31f, 0.84f, 0.5f)
                                       : locked ? new Color(0.34f, 0.32f, 0.38f)
                                       : new Color(0.55f, 0.36f, 0.96f);
            var row = UiKit.Box(card, "row");
            row.style.alignItems = Align.Center;
            IconTile(row, up, owned, locked, 86f);
            var text = UiKit.Box(row, "upg-text");
            UiKit.Label(text, up.Name, "row-title");
            UiKit.Label(text, up.Description, "row-sub");
            // the two facts that decide a purchase at a glance: what it does, and whether it changes the room
            var chips = UiKit.Box(text, "chip-row");
            if (!string.IsNullOrEmpty(up.Effect)) UiKit.Label(chips, Chip(up.Effect), "chip", "chip-effect");
            if (ChangesTheRoom(up)) UiKit.Label(chips, "CHANGES THE ROOM", "chip", "chip-world");
            if (NeedsSiting(up)) UiKit.Label(chips, "YOU PLACE IT", "chip", "chip-place");

            var side = UiKit.Box(row, "upg-side");
            var upgrade = up;
            void Detail() => UpgradeDetail(upgrade, owned);
            card.RegisterCallback<PointerEnterEvent>(_ => Detail());
            if (owned) UiKit.Label(side, "INSTALLED", "tag", "tag-owned");
            else
            {
                if (up.Id == UpgradeCatalog.SawBlade && st.HasUpgrade(UpgradeCatalog.TrimSaw))
                    UiKit.Label(side, $"Blade wear {st.BladeWear * 100f:F0}%", "row-sub", st.BladeWear >= 0.75f ? "warn" : "muted");
                UiKit.Label(side, UiKit.Money(up.Price), "row-price");
                if (locked)
                {
                    var req = UpgradeCatalog.Get(up.Requires);
                    UiKit.Label(side, "Needs " + (req != null ? req.Name : up.Requires), "row-sub", "muted");
                }
                else
                {
                    bool can = _s.CanBuyUpgrade(up.Id, out string why);
                    var buy = UiKit.Button(side, can ? (up.Consumable ? "Replace" : "Purchase") : why, () => BuyUpgrade(upgrade), can ? "btn-primary" : "");
                    buy.style.marginTop = 8;
                    buy.SetEnabled(can);
                    buy.RegisterCallback<FocusInEvent>(_ => Detail());
                }
            }
            if (_detailKey == null) { _detailKey = up.Id; Detail(); }
        }

        /// <summary>Whether buying this puts something new in the world, rather than changing a rule.</summary>
        private static bool ChangesTheRoom(UpgradeDefinition up)
            => !string.IsNullOrEmpty(up.WorldChange) && !up.WorldChange.StartsWith("Goes on your belt");

        /// <summary>Whether the player has to choose where it goes.</summary>
        private static bool NeedsSiting(UpgradeDefinition up)
        {
            foreach (var f in Build.PlaceableFixture.All)
                if (f != null && f.RequiresUpgrade == up.Id && f.Movable && !f.SitedByDefault) return true;
            return false;
        }

        /// <summary>The headline of an effect: the first clause, so a chip stays a chip.</summary>
        private static string Chip(string text)
        {
            int stop = text.IndexOfAny(new[] { '.', ':', ';' });
            string s = stop > 6 ? text.Substring(0, stop) : text;
            if (s.Length > 46) { int cut = s.LastIndexOf(' ', 45); s = s.Substring(0, cut > 20 ? cut : 45) + "\u2026"; }
            return s;
        }

        private void UpgradeDetail(UpgradeDefinition up, bool owned)
        {
            _detailKey = up.Id;
            _detail.Clear();
            var st = _s.State;
            bool locked = !string.IsNullOrEmpty(up.Requires) && !st.HasUpgrade(up.Requires);
            var plate = UiKit.Box(_detail, "detail-plate");
            var tex = UpgradeIcon(up.Id);
            if (tex != null)
            {
                plate.style.backgroundImage = new StyleBackground(tex);
                plate.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }
            var head = UiKit.Box(_detail, "row");
            head.style.alignItems = Align.Center;
            head.style.marginTop = 10;
            var ht = UiKit.Box(head, "grow");
            UiKit.Label(ht, up.Name, "detail-title");
            UiKit.Label(ht, up.Category ?? "BENCH", "detail-sub");
            UiKit.Label(head, owned ? "INSTALLED" : locked ? "LOCKED" : "AVAILABLE", "tag", owned ? "tag-owned" : null);
            UiKit.Rule(_detail);
            UiKit.Label(_detail, up.Description, "detail-note").style.marginTop = 0;
            UiKit.Label(_detail, "WHAT IT CHANGES", "caption").style.marginTop = 12;
            UiKit.Label(_detail, up.Effect, "detail-note").style.marginTop = 1;
            if (!string.IsNullOrEmpty(up.WorldChange))
            {
                UiKit.Label(_detail, "IN THE WORKSHOP", "caption").style.marginTop = 12;
                UiKit.Label(_detail, up.WorldChange, "detail-note").style.marginTop = 1;
            }
            UiKit.Rule(_detail);
            UiKit.Kv(_detail, "Price", UiKit.Money(up.Price), "accent");
            UiKit.Kv(_detail, "Till after", UiKit.Money(Mathf.Max(0f, st.Cash - up.Price)), st.Cash >= up.Price ? null : "warn");
            if (locked)
            {
                var req = UpgradeCatalog.Get(up.Requires);
                UiKit.Kv(_detail, "Requires", req != null ? req.Name : up.Requires, "warn");
            }
            UiKit.Kv(_detail, "Status", owned ? "Installed" : locked ? "Not yet available" : (_s.CanBuyUpgrade(up.Id, out string why) ? "Ready to buy" : why),
                     owned ? "success" : null);
        }

        /// <summary>One figure in the collection's summary strip.</summary>
        private static void Metric(VisualElement parent, string label, string value, string valueClass)
        {
            var cell = UiKit.Box(parent, "metric");
            UiKit.Label(cell, label, "caption").style.marginTop = 0;
            UiKit.Label(cell, value, valueClass == null ? "metric-value" : "metric-value", valueClass);
        }

        /// <summary>
        /// What the right-hand card says before anything has been found. §9.1 forbids the giant empty rectangle
        /// this used to be, and a first-time player deserves to be told how discovery works.
        /// </summary>
        private void CollectionEmptyDetail()
        {
            _detailKey = "empty";
            _detail.Clear();
            UiKit.Label(_detail, "Nothing found yet", "detail-title");
            UiKit.Label(_detail, "Every mineral family", "detail-sub");
            UiKit.Rule(_detail);
            UiKit.Label(_detail, "A family joins the collection the first time you open a rock and find it inside. "
                              + "Until then it stands here as a silhouette \u2014 you can see there is something to find, "
                              + "not what it is.", "detail-note").style.marginTop = 0;
            UiKit.Label(_detail, "HOW TO FIND ONE", "caption").style.marginTop = 12;
            UiKit.Label(_detail, "Order a crate on the Suppliers tab, wash the rock, and open it at the cracking bench. "
                              + "Different quarries hold different families, so the ones you meet depend on where you buy.",
                        "detail-note").style.marginTop = 1;
            UiKit.Rule(_detail);
            UiKit.Kv(_detail, "Families", "0 / " + MineralCatalog.All.Count);
            UiKit.Kv(_detail, "Collection value", UiKit.Money(0f));
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
            _subtitle.text = st.DisplayCapacity > 0
                ? $"Display cabinet {displayed}/{st.DisplayCapacity}  •  Collection value {UiKit.Money(st.CollectionValue())}  •  Prestige tier {st.Prestige}"
                : "No display cabinet yet. Buy one on the Upgrades tab and the pieces you keep go behind glass.";

            // §9.4: the collection is a record of what has been found, so it leads with the record, not with a
            // grid of empty plates. Every figure here is read off the encyclopedia the save already keeps.
            int foundFamilies = 0; float best = 0f; string bestName = null; float heaviest = 0f; string heaviestName = null; int finds = 0;
            foreach (var fam in MineralCatalog.All)
            {
                EncyclopediaEntry e = null;
                foreach (var x in st.Encyclopedia) if (x.Mineral == fam.Id) e = x;
                if (e == null || e.Found <= 0) continue;
                foundFamilies++; finds += e.Found;
                if (e.BestValue > best) { best = e.BestValue; bestName = fam.Name; }
                if (e.LargestMassKg > heaviest) { heaviest = e.LargestMassKg; heaviestName = fam.Name; }
            }
            var summary = UiKit.Box(_content, "coll-summary");
            Metric(summary, "FAMILIES", foundFamilies + " / " + MineralCatalog.All.Count, foundFamilies > 0 ? "accent" : "muted");
            Metric(summary, "PIECES FOUND", finds.ToString("N0"), finds > 0 ? null : "muted");
            Metric(summary, "BEST PIECE", best > 0f ? UiKit.Money(best) : "\u2014", best > 0f ? "success" : "muted");
            Metric(summary, "LARGEST", heaviest > 0f ? heaviest.ToString("F1") + " kg" : "\u2014", heaviest > 0f ? null : "muted");
            Metric(summary, "ON DISPLAY", displayed + " / " + st.DisplayCapacity, displayed > 0 ? null : "muted");
            if (bestName != null) UiKit.Label(_content, $"Best so far: {bestName}. Heaviest: {heaviestName}.", "muted");

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
                SpecimenThumbnailer.Instance.Family(plate, fam.Id, SpecimenThumbnailer.Ground);
                if (!found)
                {
                    // §6.4: an obscured silhouette says "not found yet"; an empty rectangle says "broken"
                    plate.style.unityBackgroundImageTintColor = new Color(0.115f, 0.105f, 0.145f);
                    plate.style.backgroundColor = new Color(0.10f, 0.096f, 0.115f);
                    var q = UiKit.Label(plate, "?", "tile-unknown");
                }
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
            if (_detailKey == null) CollectionEmptyDetail();
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
        /// <summary>The right-hand card on the statistics page: where the career stands, at a glance.</summary>
        private void StatsDetail()
        {
            var st = _s.State;
            _detail.Clear();
            var (level, into, span) = Core.Progression.LevelProgress(st);
            UiKit.Label(_detail, "THE CAREER SO FAR", "caption");
            UiKit.Label(_detail, "Empire Level " + level, "detail-title").style.marginTop = 4;
            UiKit.Label(_detail, into.ToString("N0") + " / " + span.ToString("N0") + " XP toward level " + (level + 1), "detail-sub");
            UiKit.Rule(_detail);
            UiKit.Kv(_detail, "Day", Core.Progression.Day(st).ToString());
            UiKit.Kv(_detail, "In the till", UiKit.Money(st.Cash), "success");
            UiKit.Kv(_detail, "Turned over", UiKit.Money(st.Stats.MoneyEarned));
            UiKit.Kv(_detail, "Rock opened", st.Stats.SpecimensOpened.ToString());
            UiKit.Kv(_detail, "Families met", st.Encyclopedia.Count + " / " + MineralCatalog.All.Count);
            UiKit.Kv(_detail, "On display", st.DisplayedCount() + " / " + st.DisplayCapacity);
            if (!string.IsNullOrEmpty(st.Stats.BiggestSaleName))
            {
                UiKit.Rule(_detail);
                UiKit.Label(_detail, "BEST SALE", "caption");
                UiKit.Label(_detail, st.Stats.BiggestSaleName, "detail-note").style.marginTop = 1;
                UiKit.Label(_detail, UiKit.Money(st.Stats.BiggestSale), "row-price").style.marginTop = 2;
            }
        }

        /// <summary>
        /// §18: what the business costs to run. Three sections, in the same metric-group language as the career
        /// page rather than a grey slab — the premises the rent is buying, the bill itself broken down, and what
        /// a day of it costs. Everything comes from the Ledger, so the page cannot drift from what is charged.
        /// </summary>
        private void BuildBusiness()
        {
            var s = _s.State;
            int today = Progression.Day(s);
            var b = s.Bills;
            bool due = Economy.Ledger.Due(s);
            _subtitle.text = due
                ? $"{UiKit.Money(b.Outstanding)} outstanding, due day {b.DueDay}."
                : $"Rent, power and water. Next bill on day {b.NextBillDay}.";

            void Group(string title, params (string k, string v, string cls)[] cells)
            {
                UiKit.Label(_content, title, "section");
                var strip = UiKit.Box(_content, "coll-summary");
                foreach (var (k, v, cls) in cells) Metric(strip, k, v, cls);
            }

            // ---- OPENING HOURS -------------------------------------------------------------------
            var shop = Retail.RetailShop.Instance;
            int inside = shop != null ? shop.Customers.Count : 0;
            _businessCustomerCount = inside;
            Group("OPENING HOURS", ("SHOP", s.ShopOpen ? "OPEN" : "CLOSED", s.ShopOpen ? "success" : "muted"),
                ("CUSTOMERS INSIDE", inside.ToString(), null));
            UiKit.Label(_content, s.ShopOpen ? "New customers can enter while the checkout is in place."
                : inside > 0 ? "New arrivals are stopped. Customers inside can finish shopping and paying."
                : "Open when you are ready to welcome customers.", "muted").style.whiteSpace = WhiteSpace.Normal;
            var hoursButton = UiKit.Button(_content, s.ShopOpen ? "Close shop" : "Open shop", () =>
            {
                if (shop != null && !shop.SetOpen(!s.ShopOpen, out string error) && error != null)
                    _s.Notify(error, NotificationKind.Warning);
            }, "btn-primary");
            hoursButton.name = "shop-hours-toggle";
            hoursButton.SetEnabled(shop != null && (s.ShopOpen || shop.Trading));
            if (shop != null && !shop.Trading && !s.ShopOpen)
                UiKit.Label(_content, "Place the checkout to open your shop.", "muted");

            // ---- PREMISES ------------------------------------------------------------------------
            string unit = Workshop.PremisesExpansion.ShopFrontOpen ? "Unit 1 + back room + shop front"
                        : Workshop.PremisesExpansion.BackRoomOpen ? "Unit 1 + back room"
                        : "Unit 1 only";
            var next = NextPremises(s);
            Group("PREMISES",
                ("LEASED", unit, "accent"),
                ("USABLE FLOOR", $"{Economy.Ledger.LeasedAreaM2(s):F0} m\u00b2", null),
                ("RENT A PERIOD", UiKit.Money(Economy.Ledger.RentPerPeriod(s)), null),
                ("NEXT UNIT", next != null ? next.Name : "All of it is yours", next != null ? null : "muted"),
                ("TO TAKE IT ON", next != null ? UiKit.Money(next.Price) : "\u2014", next != null && s.Cash >= next.Price ? "success" : "muted"));
            if (next != null)
            {
                string block = Economy.Ledger.ExpansionBlocked(s)
                    ? "The landlord will not approve new floor while a bill is outstanding."
                    : !string.IsNullOrEmpty(next.Requires) && !s.HasUpgrade(next.Requires)
                        ? "First: " + Economy.UpgradeCatalog.Get(next.Requires).Name
                        : null;
                if (block != null) UiKit.Label(_content, block, "muted").style.whiteSpace = WhiteSpace.Normal;
                UiKit.Label(_content, $"Rent would be {UiKit.Money(Economy.Ledger.RentPerPeriod(s) + RentDeltaFor(next.Id))} a period once signed — {UiKit.Money(RentDeltaFor(next.Id))} more than now.", "muted")
                    .style.whiteSpace = WhiteSpace.Normal;
            }

            // ---- BILLS ---------------------------------------------------------------------------
            UiKit.Label(_content, due ? "THIS BILL" : "ON THE METERS", "section");
            if (due && b.LastLines.Count > 0)
            {
                // the bill that was issued, not an estimate: IssueBill zeroes the meters, so re-running Breakdown
                // here would show the player next period's charges under the heading of the one they owe
                foreach (var raw in b.LastLines)
                {
                    var parts = raw.Split('|');
                    string label = parts.Length > 0 ? parts[0] : raw;
                    string amount = parts.Length > 1 && float.TryParse(parts[1], out float a) ? UiKit.Money(a) : "";
                    string detail = parts.Length > 2 ? parts[2] : null;
                    UiKit.Kv(_content, label + (!string.IsNullOrEmpty(detail) ? "  \u2014  " + detail : ""), amount);
                }
                if (b.LateFees > 0.005f) UiKit.Kv(_content, "Late fee", UiKit.Money(b.LateFees));
                UiKit.Kv(_content, "Outstanding", UiKit.Money(b.Outstanding));
            }
            else
            {
                foreach (var l in Economy.Ledger.Breakdown(s))
                    UiKit.Kv(_content, l.Label + (l.Detail != null ? "  \u2014  " + l.Detail : ""), UiKit.Money(l.Amount));
                UiKit.Kv(_content, "Estimated next bill", UiKit.Money(Economy.Ledger.Total(s)));
            }
            if (due)
            {
                var pay = UiKit.Button(_content, $"Pay {UiKit.Money(b.Outstanding)}", () =>
                {
                    if (!_s.PayBill(out string err) && err != null) _s.Notify(err, NotificationKind.Warning);
                    Refresh();
                }, s.Cash >= b.Outstanding ? "btn-primary" : "btn-ghost");
                pay.style.marginTop = 10;
                int daysLeft = b.DueDay + Economy.Ledger.GraceDays - today;
                UiKit.Label(_content, Economy.Ledger.Overdue(s, today)
                    ? (daysLeft > 0 ? $"Overdue. {daysLeft} day{(daysLeft == 1 ? "" : "s")} before a late fee." : "Overdue. A late fee has been added.")
                    : $"Due on day {b.DueDay}. Nothing is taken from the till until you pay it.", "muted")
                    .style.whiteSpace = WhiteSpace.Normal;
            }

            // ---- OPERATING COSTS -----------------------------------------------------------------
            float perDay = Economy.Ledger.PerDay(s);
            Group("OPERATING COSTS",
                ("A DAY", UiKit.Money(perDay), null),
                ("A WEEK", UiKit.Money(perDay * 7f), null),
                ("LAST BILL", b.LastBillAmount > 0f ? UiKit.Money(b.LastBillAmount) : "\u2014", b.LastBillAmount > 0f ? null : "muted"),
                ("PAID SO FAR", UiKit.Money(b.TotalPaid), b.TotalPaid > 0f ? "success" : "muted"),
                ("MISSED", b.MissedPayments.ToString(), b.MissedPayments > 0 ? "warn" : "muted"));

            // biggest drivers, so the player knows what to switch off rather than just that it is expensive
            var drivers = new List<(string what, float cost)>
            {
                ("Rent", Economy.Ledger.RentPerPeriod(s)),
                ("Electricity", Economy.Ledger.ElectricityCost(s)),
                ("Water", Economy.Ledger.WaterCost(s)),
                ("Equipment service", Economy.Ledger.MaintenancePerPeriod(s)),
            };
            drivers.Sort((x, y) => y.cost.CompareTo(x.cost));
            float total = Mathf.Max(0.01f, Economy.Ledger.Total(s));
            UiKit.Label(_content, "WHERE IT GOES", "section");
            foreach (var d in drivers)
            {
                if (d.cost <= 0.005f) continue;
                UiKit.Kv(_content, d.what, $"{UiKit.Money(d.cost)}   ({100f * d.cost / total:F0}%)");
            }
        }

        /// <summary>The premises lease the player could take on next, or null when they hold them all.</summary>
        private static Economy.UpgradeDefinition NextPremises(Save.GameState s)
        {
            foreach (var id in new[] { Economy.UpgradeCatalog.BackRoom, Economy.UpgradeCatalog.ShopFront, Economy.UpgradeCatalog.Stage3 })
                if (!s.HasUpgrade(id)) return Economy.UpgradeCatalog.Get(id);
            return null;
        }

        private static float RentDeltaFor(string id)
            => id == Economy.UpgradeCatalog.BackRoom ? Economy.Ledger.BackRoomRent
             : id == Economy.UpgradeCatalog.ShopFront ? Economy.Ledger.ShopFrontRent
             : 90f;

        private void BusinessDetail()
        {
            var s = _s.State;
            UiKit.Label(_detail, "HOW BILLING WORKS", "section");
            UiKit.Label(_detail,
                $"A bill lands every {Economy.Ledger.PeriodDays} days with a breakdown, and nothing leaves the till "
                + "until you pay it here. Rent goes up with the floor you lease. Electricity is a standing charge "
                + "plus what the machines and the lights actually used; water is the basin and the nozzle. "
                + $"Miss a bill by more than {Economy.Ledger.GraceDays} days and a "
                + $"{Economy.Ledger.LateFeeRate * 100f:F0}% fee is added — two missed bills and the landlord stops "
                + "approving new floor, three and the good suppliers want cash up front. Paying up clears all of it.",
                "muted").style.whiteSpace = WhiteSpace.Normal;
            var warn = Economy.Ledger.StandingWarning(s, Progression.Day(s));
            if (warn != null)
            {
                var w = UiKit.Label(_detail, warn, "warn");
                w.style.whiteSpace = WhiteSpace.Normal;
                w.style.marginTop = 10;
            }
        }

        private void BuildStats()
        {
            _subtitle.text = "The career so far \u2014 what has been opened, sold, kept and learned.";
            var st = _s.State.Stats;
            var save = _s.State;

            // §9.5: compact metric groups and notable records, not one long flat column of label/value
            void Group(string title, params (string k, string v, string cls)[] cells)
            {
                UiKit.Label(_content, title, "section");
                var strip = UiKit.Box(_content, "coll-summary");
                foreach (var (k, v, cls) in cells) Metric(strip, k, v, cls);
            }

            Group("THE BUSINESS",
                ("DAY", Progression.Day(save).ToString(), null),
                ("AT THE BENCH", System.TimeSpan.FromSeconds(st.PlayTimeSeconds).ToString(@"h\:mm\:ss"), null),
                ("TURNED OVER", UiKit.Money(st.MoneyEarned), st.MoneyEarned > 0f ? "success" : "muted"),
                ("SPENT", UiKit.Money(st.MoneySpent), null),
                ("IN THE TILL", UiKit.Money(save.Cash), "accent"));

            Group("AT THE BENCH",
                ("CRATES", st.CratesPurchased.ToString(), null),
                ("ROCKS OPENED", st.SpecimensOpened.ToString(), null),
                ("CLEAN OPENS", st.SpecimensOpened > 0 ? $"{st.CleanOpens}  ({Mathf.RoundToInt(100f * st.CleanOpens / Mathf.Max(1, st.SpecimensOpened))}%)" : "\u2014", st.CleanOpens > 0 ? "success" : "muted"),
                ("DAMAGED", st.SpecimensDamaged.ToString(), st.SpecimensDamaged > 0 ? "warn" : "muted"),
                ("STRIKES", st.TotalStrikes.ToString("N0"), null),
                ("WASHED", st.RocksWashed.ToString(), null));

            if (st.SawCuts > 0 || st.PiecesPolished > 0 || st.RocksCracked > 0)
                Group("PROCESSING",
                    ("SAW CUTS", st.SawCuts.ToString(), null),
                    ("SLABS", st.SlabsCut.ToString(), null),
                    ("POLISHED", st.PiecesPolished.ToString(), null),
                    ("CRACKER", st.RocksCracked.ToString(), null),
                    ("LARGEST FACE", st.LargestSlabFaceCm2 > 0 ? $"{st.LargestSlabFaceCm2:F0} cm\u00b2" : "\u2014", st.LargestSlabFaceCm2 > 0 ? null : "muted"));

            Group("THE SHOP",
                ("SOLD", st.SpecimensSold.ToString(), null),
                ("OVER THE COUNTER", st.RetailSales.ToString(), null),
                ("COUNTER TAKINGS", UiKit.Money(st.RetailRevenue), st.RetailRevenue > 0f ? "success" : "muted"),
                ("SERVED", st.CustomersServed.ToString(), null),
                ("LEFT EMPTY", st.CustomersLeftEmptyHanded.ToString(), st.CustomersLeftEmptyHanded > 0 ? "warn" : "muted"),
                ("ON SALE NOW", save.ForSaleCount().ToString(), null));

            if (st.PredictionsMade > 0)
                Group("THE HAND",
                    ("CALLS MADE", st.PredictionsMade.ToString(), null),
                    ("HOLLOW OR SOLID", $"{Mathf.RoundToInt(100f * st.HollowCallsRight / st.PredictionsMade)}%", st.HollowCallsRight * 2 >= st.PredictionsMade ? "success" : "warn"),
                    ("GRADE WITHIN ONE", $"{st.TierCallsRight} of {st.PredictionsMade}", null));

            // notable records: the pieces worth remembering, named
            UiKit.Label(_content, "RECORDS", "section");
            void Record(string k, float value, string name, bool money = true, string unit = null)
            {
                var r = UiKit.Box(_content, "stat-row");
                var kl = UiKit.Label(r, k, "stat-key"); kl.style.flexGrow = 1;
                if (value <= 0f) { UiKit.Label(r, "not yet", "muted"); return; }
                UiKit.Label(r, string.IsNullOrEmpty(name) ? "\u2014" : name, "row-sub");
                UiKit.Label(r, money ? UiKit.Money(value) : value.ToString("F2") + (unit ?? ""), "stat-val", "medium");
            }
            Record("Biggest sale", st.BiggestSale, st.BiggestSaleName);
            Record("Best retail sale", st.BiggestRetailSale, st.BiggestRetailSaleName);
            Record("Finest piece kept", st.HighestValueKept, st.HighestValueKeptName);
            Record("Largest specimen", st.LargestSpecimenKg, st.LargestSpecimenName, false, " kg");
            Record("Best from the saw", st.HighestValueSawResult, st.HighestValueSawResultName);
            Record("Best from the hammer", st.HighestValueHammerResult, st.HighestValueHammerResultName);
            Record("Best polished", st.BestPolishedValue, st.BestPolishedName);

            // and what the career is working towards next
            UiKit.Label(_content, "NEXT", "section");
            var goals = Progression.Goals(save);
            foreach (var g in goals)
            {
                var r = UiKit.Box(_content, "stat-row");
                var kl = UiKit.Label(r, (g.Done ? "\u2713  " : "\u25cb  ") + g.Label, g.Done ? "item-sub" : "item-title"); kl.style.flexGrow = 1;
                UiKit.Label(r, g.Progress, g.Done ? "success" : "muted");
            }
            string next = Progression.NextUnlock(save);
            if (!string.IsNullOrEmpty(next)) UiKit.Label(_content, next, "muted");
        }
    }
}
