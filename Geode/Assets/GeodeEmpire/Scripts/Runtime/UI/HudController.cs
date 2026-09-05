using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Core;
using GeodeEmpire.Player;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.UI
{
    /// <summary>Crosshair, interaction prompt, cash, tutorial hint and notification toasts.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudController : MonoBehaviour
    {
        public static HudController Instance { get; private set; }

        private UIDocument _doc;
        private VisualElement _root, _crosshair, _ring, _notifyStack, _tutorialCard;
        private VisualElement _promptChip, _goalsCard, _statusCard, _keybar, _xpFill;
        private Label _goalWhy;
        private Label _prompt, _hint, _cash, _cashDelta, _tutorial, _tutorialTick, _promptKey, _day, _level, _xpText, _goalHead;
        private readonly List<(VisualElement box, Label text, Label num)> _goalRows = new List<(VisualElement, Label, Label)>();
        private int _lastLevel = -1;
        private VisualElement _discovery, _discPlate;
        private Label _discKind, _discName, _discSub, _discValue, _discRarity;
        private float _discTimer;
        private PlayerInteractor _player;
        private float _deltaTimer;
        private readonly List<(VisualElement el, float t)> _notes = new List<(VisualElement, float)>();
        private bool _hidden;
        private bool _freeRoam = true;
        private string _tutorialDoneText;
        private float _tutorialDoneTimer;

        private void Awake()
        {
            Instance = this;
            _doc = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_doc.panelSettings == null) _doc.panelSettings = Resources.Load<PanelSettings>("UI/GeodePanelSettings");
            _root = _doc.rootVisualElement;
            _root.Clear();
            var ss = Resources.Load<StyleSheet>("UI/GeodeUI");
            if (ss != null && !_root.styleSheets.Contains(ss)) _root.styleSheets.Add(ss);
            var hud = UiKit.Box(_root, "hud-root");
            hud.pickingMode = PickingMode.Ignore;

            _ring = UiKit.Box(hud, "crosshair-ring");
            _ring.style.left = Length.Percent(50); _ring.style.top = Length.Percent(50);
            _ring.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            _ring.style.display = DisplayStyle.None;
            _crosshair = UiKit.Box(hud, "crosshair");
            _crosshair.style.left = Length.Percent(50); _crosshair.style.top = Length.Percent(50);
            _crosshair.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            // a thin cross reads on both a bright bench and a dark corner where a dot disappears
            var bh = UiKit.Box(_crosshair, "crosshair-bar");
            bh.style.left = -7; bh.style.top = 1; bh.style.width = 15; bh.style.height = 1;
            var bv = UiKit.Box(_crosshair, "crosshair-bar");
            bv.style.left = 1; bv.style.top = -7; bv.style.width = 1; bv.style.height = 15;

            // the call to act sits just under the crosshair as a key cap and a verb
            _promptChip = UiKit.Box(hud, "interact-chip");
            _promptKey = UiKit.Label(_promptChip, "E", "keycap");
            _prompt = UiKit.Label(_promptChip, "", "keyhint-label");
            _prompt.style.fontSize = 15;
            _promptChip.style.display = DisplayStyle.None;
            _hint = UiKit.Label(hud, "", "prompt-hint");
            _hint.style.display = DisplayStyle.None;

            BuildBrand(hud);
            BuildStatus(hud);
            BuildKeybar(hud);
            BuildDiscovery(hud);

            _notifyStack = UiKit.Box(hud, "notify-stack");
            _tutorialCard = UiKit.Box(hud, "card", "tutorial");
            _tutorial = UiKit.Label(_tutorialCard, "", "");
            _tutorial.style.whiteSpace = WhiteSpace.Normal;
            _tutorialTick = UiKit.Label(_tutorialCard, "", "tutorial-tick");
            _tutorialTick.style.display = DisplayStyle.None;
            _tutorialCard.style.display = DisplayStyle.None;

        }

        /// <summary>Top-left: who you are and what the shop is working towards.</summary>
        private void BuildBrand(VisualElement hud)
        {
            var brand = UiKit.Box(hud, "brand");
            UiKit.Box(brand, "brand-gem");
            UiKit.Label(brand, "GEODE EMPIRE", "brand-name");

            _goalsCard = UiKit.Box(hud, "goals");
            _goalHead = UiKit.Label(_goalsCard, "Expand the Business", "goals-title");
            _goalWhy = UiKit.Label(_goalsCard, "", "goal-why");
            _goalWhy.style.whiteSpace = WhiteSpace.Normal;
            var head = UiKit.Box(_goalsCard, "goal-row");
            UiKit.Label(head, "\u2605", "goal-star");
            var headText = UiKit.Label(head, "", "goal-text", "goal-head");
            _goalRows.Add((null, headText, null));
            for (int i = 0; i < 3; i++)
            {
                var row = UiKit.Box(_goalsCard, "goal-row");
                var box = UiKit.Box(row, "goal-box");
                var text = UiKit.Label(row, "", "goal-text");
                var num = UiKit.Label(row, "", "goal-num");
                _goalRows.Add((box, text, num));
            }
        }

        /// <summary>Top-right: the till, the clock and how far the empire has come.</summary>
        private void BuildStatus(VisualElement hud)
        {
            _statusCard = UiKit.Box(hud, "status");
            var cashRow = UiKit.Box(_statusCard, "row");
            cashRow.style.alignItems = Align.Center;
            _cashDelta = UiKit.Label(cashRow, "", "cash-delta", "medium");
            _cashDelta.style.marginRight = 10;
            _cash = UiKit.Label(cashRow, "$0", "status-cash");
            _day = UiKit.Label(_statusCard, "Day 1 - 8:00 AM", "status-day");
            _level = UiKit.Label(_statusCard, "Empire Level 1", "status-level");
            var track = UiKit.Box(_statusCard, "xp-track");
            _xpFill = UiKit.Box(track, "xp-fill");
            _xpText = UiKit.Label(track, "0 / 1,000 XP", "xp-text");
        }

        /// <summary>Bottom rail: the controls that are always live, in the pack's key-cap style.</summary>
        private void BuildKeybar(VisualElement hud)
        {
            _keybar = UiKit.Box(hud, "keybar");
            UiKit.KeyHint(_keybar, GameInput.Glyph("Tablet"), "Tablet");
            UiKit.KeyHint(_keybar, GameInput.Glyph("Inspect"), "Inspect");
            UiKit.KeyHint(_keybar, GameInput.Glyph("Build"), "Build Mode");
            UiKit.KeyHint(_keybar, GameInput.Glyph("Inventory"), "Inventory");
            UiKit.KeyHint(_keybar, GameInput.Glyph("Pause"), "Menu");
        }

        /// <summary>The moment a rock gives up something worth stopping for: the piece, its grade and what it is worth.</summary>
        private void BuildDiscovery(VisualElement hud)
        {
            _discovery = UiKit.Box(hud, "discovery");
            _discKind = UiKit.Label(_discovery, "NEW DISCOVERY", "discovery-kind");
            _discPlate = UiKit.Box(_discovery, "discovery-plate");
            var row = UiKit.Box(_discovery, "row");
            row.style.alignItems = Align.Center;
            row.style.marginTop = 12;
            var text = UiKit.Box(row, "grow");
            _discName = UiKit.Label(text, "", "detail-title");
            _discSub = UiKit.Label(text, "", "detail-sub");
            _discRarity = UiKit.Rarity(row, 0);
            _discValue = UiKit.Label(_discovery, "", "appraisal-value");
            _discovery.style.display = DisplayStyle.None;
        }

        private void OnDiscovered(Save.SpecimenRecord r, string kind)
        {
            if (r == null || _discovery == null) return;
            _discKind.text = kind.ToUpper();
            _discName.text = r.DisplayName;
            _discSub.text = r.Geology.Family.Name;
            int tier = Mathf.Clamp((int)r.Geology.Tier - 1, 0, 4);
            foreach (var c in new[] { "rarity-common", "rarity-uncommon", "rarity-rare", "rarity-epic", "rarity-legendary" }) _discRarity.RemoveFromClassList(c);
            _discRarity.AddToClassList(new[] { "rarity-common", "rarity-uncommon", "rarity-rare", "rarity-epic", "rarity-legendary" }[tier]);
            _discRarity.text = Specimens.Valuation.TierLabel(r.Geology.Tier).ToUpper();
            _discValue.text = UiKit.Money(r.PristineForSale());
            SpecimenThumbnailer.Instance.Specimen(_discPlate, r, SpecimenThumbnailer.Ground);
            _discovery.style.display = DisplayStyle.Flex;
            _discovery.style.opacity = 1f;
            _discTimer = 5.5f;
            Audio.WorkshopAudio.Play2D("ui_sell", 0.55f, 1.05f);
        }

        private void RefreshKeybar()
        {
            var caps = _keybar.Query<Label>(className: "keycap").ToList();
            string[] wanted = { GameInput.Glyph("Tablet"), GameInput.Glyph("Inspect"), GameInput.Glyph("Build"), GameInput.Glyph("Inventory"), GameInput.Glyph("Pause") };
            for (int i = 0; i < caps.Count && i < wanted.Length; i++) caps[i].text = wanted[i];
        }

        /// <summary>Day, clock, level, experience and the standing goals, all read off the career.</summary>
        private void RefreshStatus()
        {
            var st = GameSession.Instance != null ? GameSession.Instance.State : null;
            if (st == null) return;
            _day.text = "Day " + Progression.Day(st) + "  -  " + Progression.Clock(st);
            var (level, into, span) = Progression.LevelProgress(st);
            _level.text = "Empire Level " + level;
            _xpFill.style.width = Length.Percent(Mathf.Clamp01(span <= 0 ? 0f : into / (float)span) * 100f);
            _xpText.text = into.ToString("N0") + " / " + span.ToString("N0") + " XP";
            if (level != _lastLevel)
            {
                if (_lastLevel > 0) GameSession.Instance.Notify("Empire Level " + level, NotificationKind.Discovery);
                _lastLevel = level;
            }

            _goalHead.text = "Expand the Business";
            // §65: the goals are the measure; this says what is on the other side of them
            string next = Progression.NextUnlock(st);
            _goalWhy.text = string.IsNullOrEmpty(next) ? "" : "Next: " + next;
            _goalWhy.style.display = string.IsNullOrEmpty(next) ? DisplayStyle.None : DisplayStyle.Flex;
            var goals = Progression.Goals(st);
            _goalRows[0].text.text = Progression.GoalHeader(st);
            for (int i = 0; i < 3; i++)
            {
                var (box, text, num) = _goalRows[i + 1];
                if (i >= goals.Length) { text.text = ""; num.text = ""; continue; }
                text.text = goals[i].Label;
                num.text = goals[i].Progress;
                if (goals[i].Done) box.AddToClassList("goal-box-done"); else box.RemoveFromClassList("goal-box-done");
            }
        }

        private void Start()
        {
            _player = FindAnyObjectByType<PlayerInteractor>();
            if (_player != null) _player.PromptChanged += RefreshPrompt;
            var s = GameSession.Instance;
            if (s != null)
            {
                s.CashChanged += OnCash;
                s.Notified += Notify;
                s.Loaded += OnLoaded;
                s.StateChanged += RefreshTutorial;
                s.Discovered += OnDiscovered;
                if (s.State != null) OnLoaded();
            }
            Tutorial.Changed += RefreshTutorial;
            Tutorial.Completed += OnTutorialStepDone;
            GameSettings.Changed += OnSettingsChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDisable()
        {
            if (_player != null) _player.PromptChanged -= RefreshPrompt;
            var s = GameSession.Instance;
            if (s != null)
            {
                s.CashChanged -= OnCash;
                s.Notified -= Notify;
                s.Loaded -= OnLoaded;
                s.StateChanged -= RefreshTutorial;
                s.Discovered -= OnDiscovered;
            }
            Tutorial.Changed -= RefreshTutorial;
            Tutorial.Completed -= OnTutorialStepDone;
            GameSettings.Changed -= OnSettingsChanged;
        }

        private void OnLoaded()
        {
            _cash.text = UiKit.Money(GameSession.Instance.Cash);
            _lastLevel = -1;
            RefreshStatus();
            RefreshTutorial();
        }

        private void OnCash(float cash, float delta)
        {
            _cash.text = UiKit.Money(cash);
            RefreshStatus();
            _cashDelta.text = (delta >= 0 ? "+" : "") + UiKit.Money(delta);
            _cashDelta.RemoveFromClassList("success");
            _cashDelta.RemoveFromClassList("danger");
            _cashDelta.AddToClassList(delta >= 0 ? "success" : "danger");
            _deltaTimer = 2.5f;
        }

        public void SetHidden(bool hidden)
        {
            _hidden = hidden;
            _root.style.display = hidden ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public bool CrosshairShown => _crosshair != null && _crosshair.style.display == DisplayStyle.Flex;

        private void OnSettingsChanged()
        {
            RefreshTutorial();
            if (_freeRoam) SetFreeRoamVisible(true);
        }

        /// <summary>True while the player is free-roaming (no station view or menu owns the screen).</summary>
        public bool FreeRoam => _freeRoam;

        /// <summary>Hide crosshair/prompt while a station or menu owns the screen.</summary>
        public void SetFreeRoamVisible(bool visible)
        {
            _freeRoam = visible;
            var d = visible && GameSettings.Current.CrosshairVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _crosshair.style.display = d;
            // the goal card and the control rail belong to walking around; a station or menu draws its own
            var chrome = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _goalsCard.style.display = chrome;
            _keybar.style.display = chrome;
            if (!visible) { _ring.style.display = DisplayStyle.None; _promptChip.style.display = DisplayStyle.None; _hint.style.display = DisplayStyle.None; _tutorialCard.style.display = DisplayStyle.None; }
            else { RefreshPrompt(); RefreshTutorial(); }
        }

        private void RefreshPrompt()
        {
            if (_player == null) return;
            if (!_freeRoam)
            {
                _promptChip.style.display = DisplayStyle.None;
                _hint.style.display = DisplayStyle.None;
                _ring.style.display = DisplayStyle.None;
                _crosshair.style.display = DisplayStyle.None;
                return;
            }
            bool hasTarget = _player.Target != null;
            _prompt.text = _player.Prompt;
            _promptKey.text = _player.PromptKey;
            _promptKey.style.display = string.IsNullOrEmpty(_player.PromptKey) ? DisplayStyle.None : DisplayStyle.Flex;
            _promptChip.style.display = string.IsNullOrEmpty(_player.Prompt) ? DisplayStyle.None : DisplayStyle.Flex;
            _hint.text = _player.Hint;
            // one bottom-centre message at a time: a tutorial step outranks the control hint (V5 §54, no centre-screen clutter)
            _hint.style.display = string.IsNullOrEmpty(_player.Hint) || Tutorial.Current != null ? DisplayStyle.None : DisplayStyle.Flex;
            _ring.style.display = hasTarget && !_player.Inspecting ? DisplayStyle.Flex : DisplayStyle.None;
            _crosshair.style.display = _player.Inspecting || !GameSettings.Current.CrosshairVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>A finished step is acknowledged for a couple of seconds before the next one takes the card.</summary>
        private void OnTutorialStepDone(Tutorial.Step step)
        {
            if (step == null || string.IsNullOrEmpty(step.Done)) return;
            _tutorialDoneText = step.Done;
            _tutorialDoneTimer = 2.2f;
            RefreshTutorial();
        }

        private void RefreshTutorial()
        {
            var step = Tutorial.Current;
            bool ack = _tutorialDoneTimer > 0f && !string.IsNullOrEmpty(_tutorialDoneText);
            if ((step == null && !ack) || !_freeRoam) { _tutorialCard.style.display = DisplayStyle.None; return; }
            if (ack)
            {
                _tutorial.text = "✓  " + _tutorialDoneText;
                _tutorialTick.text = step != null ? "next: " + Tutorial.Format(step.Text) : "That is the whole loop. The rest is yours.";
                _tutorialTick.style.whiteSpace = WhiteSpace.Normal;
                _tutorialTick.style.display = DisplayStyle.Flex;
            }
            else
            {
                _tutorial.text = Tutorial.Format(step.Text);
                _tutorialTick.style.display = DisplayStyle.None;
            }
            _tutorialCard.EnableInClassList("tutorial-done", ack);
            _tutorialCard.style.display = DisplayStyle.Flex;
        }

        public void Notify(string text, NotificationKind kind)
        {
            var el = UiKit.Box(_notifyStack, "card", "notify");
            switch (kind)
            {
                case NotificationKind.Success: el.AddToClassList("notify-success"); break;
                case NotificationKind.Warning: el.AddToClassList("notify-warning"); break;
                case NotificationKind.Discovery: el.AddToClassList("notify-discovery"); break;
            }
            var l = UiKit.Label(el, text, "");
            l.style.whiteSpace = WhiteSpace.Normal;
            _notes.Add((el, kind == NotificationKind.Discovery ? 7f : 4.5f));
            while (_notes.Count > 4) { _notes[0].el.RemoveFromHierarchy(); _notes.RemoveAt(0); }
        }

        private void Update()
        {
            if (_discTimer > 0f)
            {
                _discTimer -= Time.deltaTime;
                _discovery.style.opacity = Mathf.Clamp01(_discTimer * 1.4f);
                if (_discTimer <= 0f) _discovery.style.display = DisplayStyle.None;
            }
            if (_deltaTimer > 0f)
            {
                _deltaTimer -= Time.deltaTime;
                _cashDelta.style.opacity = Mathf.Clamp01(_deltaTimer);
                if (_deltaTimer <= 0f) _cashDelta.text = "";
            }
            for (int i = _notes.Count - 1; i >= 0; i--)
            {
                var (el, t) = _notes[i];
                t -= Time.deltaTime;
                el.style.opacity = Mathf.Clamp01(t * 1.5f);
                if (t <= 0f) { el.RemoveFromHierarchy(); _notes.RemoveAt(i); }
                else _notes[i] = (el, t);
            }
            // keep glyphs current when the player switches devices, and the clock honest as the day runs
            if (_tutorialDoneTimer > 0f)
            {
                _tutorialDoneTimer -= Time.unscaledDeltaTime;
                if (_tutorialDoneTimer <= 0f) { _tutorialDoneText = null; RefreshTutorial(); }
            }
            if (Time.frameCount % 30 == 0) { RefreshPrompt(); RefreshTutorial(); RefreshKeybar(); RefreshStatus(); }
        }
    }
}
