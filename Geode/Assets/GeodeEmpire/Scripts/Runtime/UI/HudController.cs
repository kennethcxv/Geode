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
        private Label _prompt, _hint, _cash, _cashDelta, _tutorial;
        private PlayerInteractor _player;
        private float _deltaTimer;
        private readonly List<(VisualElement el, float t)> _notes = new List<(VisualElement, float)>();
        private bool _hidden;
        private bool _freeRoam = true;

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

            _prompt = UiKit.Label(hud, "", "prompt", "medium");
            _hint = UiKit.Label(hud, "", "prompt-hint");
            _hint.style.display = DisplayStyle.None;

            var cashCard = UiKit.Box(hud, "card", "cash-card");
            UiKit.Label(cashCard, "CASH", "cash-label");
            _cash = UiKit.Label(cashCard, "$0", "cash-value", "bold");
            _cashDelta = UiKit.Label(cashCard, "", "cash-delta", "medium");

            _notifyStack = UiKit.Box(hud, "notify-stack");
            _tutorialCard = UiKit.Box(hud, "card", "tutorial");
            _tutorial = UiKit.Label(_tutorialCard, "", "");
            _tutorial.style.whiteSpace = WhiteSpace.Normal;
            _tutorialCard.style.display = DisplayStyle.None;

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
                if (s.State != null) OnLoaded();
            }
            Tutorial.Changed += RefreshTutorial;
            GameSettings.Changed += RefreshTutorial;
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
            }
            Tutorial.Changed -= RefreshTutorial;
            GameSettings.Changed -= RefreshTutorial;
        }

        private void OnLoaded()
        {
            _cash.text = UiKit.Money(GameSession.Instance.Cash);
            RefreshTutorial();
        }

        private void OnCash(float cash, float delta)
        {
            _cash.text = UiKit.Money(cash);
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

        /// <summary>Hide crosshair/prompt while a station or menu owns the screen.</summary>
        public void SetFreeRoamVisible(bool visible)
        {
            _freeRoam = visible;
            var d = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _crosshair.style.display = d;
            if (!visible) { _ring.style.display = DisplayStyle.None; _prompt.style.display = DisplayStyle.None; _hint.style.display = DisplayStyle.None; _tutorialCard.style.display = DisplayStyle.None; }
            else { RefreshPrompt(); RefreshTutorial(); }
        }

        private void RefreshPrompt()
        {
            if (_player == null) return;
            if (!_freeRoam)
            {
                _prompt.style.display = DisplayStyle.None;
                _hint.style.display = DisplayStyle.None;
                _ring.style.display = DisplayStyle.None;
                _crosshair.style.display = DisplayStyle.None;
                return;
            }
            bool hasTarget = _player.Target != null;
            _prompt.text = _player.Prompt;
            _prompt.style.display = string.IsNullOrEmpty(_player.Prompt) ? DisplayStyle.None : DisplayStyle.Flex;
            _hint.text = _player.Hint;
            _hint.style.display = string.IsNullOrEmpty(_player.Hint) ? DisplayStyle.None : DisplayStyle.Flex;
            _ring.style.display = hasTarget && !_player.Inspecting ? DisplayStyle.Flex : DisplayStyle.None;
            _crosshair.style.display = _player.Inspecting ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void RefreshTutorial()
        {
            var step = Tutorial.Current;
            if (step == null || !_freeRoam) { _tutorialCard.style.display = DisplayStyle.None; return; }
            _tutorial.text = Tutorial.Format(step.Text);
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
            // keep glyphs current when the player switches devices
            if (Time.frameCount % 30 == 0) { RefreshPrompt(); RefreshTutorial(); }
        }
    }
}
