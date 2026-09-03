using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;

namespace GeodeEmpire.UI
{
    /// <summary>Pause menu with the comfort/graphics/audio settings needed for playtesting.</summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }
        public bool IsOpen { get; private set; }
        public bool ShowSettingsOnly;   // used by the title screen
        public bool SettingsVisible => _settingsPage != null && _settingsPage.style.display == DisplayStyle.Flex;
        public SettingsPage Settings => _settings;
        public string FocusedText => UiKit.FocusedText(_panel);

        private VisualElement _dim, _panel, _mainPage, _settingsPage;
        private SettingsPage _settings;
        private Button _resume, _settingsBtn, _quitBtn;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            var root = GetComponentInParent<UIDocument>()?.rootVisualElement ?? HudController.Instance.GetComponent<UIDocument>().rootVisualElement;
            _dim = UiKit.Box(root, "panel-dim");
            _dim.style.display = DisplayStyle.None;
            _panel = UiKit.Box(_dim, "panel");
            _panel.style.width = 720;
            _mainPage = UiKit.Box(_panel);
            UiKit.Label(_mainPage, "PAUSED", "panel-title", "bold");
            UiKit.Label(_mainPage, "Progress is saved automatically.", "panel-subtitle");
            _resume = UiKit.Button(_mainPage, "Resume", Close, "btn-primary");
            _resume.style.marginBottom = 10;
            _settingsBtn = UiKit.Button(_mainPage, "Settings", () => ShowSettings(true));
            _settingsBtn.style.marginBottom = 10;
            _quitBtn = UiKit.Button(_mainPage, "Save and quit to title", QuitToTitle, "btn-ghost");
            _settings = new SettingsPage(_panel, LeaveSettings);
            _settingsPage = _settings.Root;
            _settingsPage.style.display = DisplayStyle.None;
            _dim.RegisterCallback<NavigationCancelEvent>(e =>
            {
                e.StopPropagation();
                if (_cancelHandledFrame == Time.frameCount) return;      // Update() already consumed this press
                _cancelHandledFrame = Time.frameCount;
                if (SettingsVisible) { if (!_settings.HandleCancel()) LeaveSettings(); } else Close();
            });
        }

        // One physical press can arrive twice: as the UI Cancel navigation event and as the raw device poll below.
        private int _cancelHandledFrame = -1;

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var gp = UnityEngine.InputSystem.Gamepad.current;
            bool pausePressed = (kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.startButton.wasPressedThisFrame);
            bool backPressed = gp != null && gp.buttonEast.wasPressedThisFrame;
            if (!IsOpen)
            {
                // the same Escape/B that just closed the tablet, a letter or the bench must not also open the pause menu
                if (pausePressed && !CursorController.InMenu && !CursorController.InputConsumedThisFrame && !BenchActive()) Open();
                return;
            }
            if (SettingsVisible)
            {
                _settings.Tick(Time.unscaledDeltaTime);
                // shoulder buttons (or Q/E) hop between tabs from anywhere on the page
                bool prev = (gp != null && gp.leftShoulder.wasPressedThisFrame) || (kb != null && kb.qKey.wasPressedThisFrame);
                bool next = (gp != null && gp.rightShoulder.wasPressedThisFrame) || (kb != null && kb.eKey.wasPressedThisFrame);
                if (prev) _settings.SwitchTab(-1); else if (next) _settings.SwitchTab(1);
            }
            if ((pausePressed || backPressed) && _cancelHandledFrame != Time.frameCount)
            {
                _cancelHandledFrame = Time.frameCount;
                if (SettingsVisible) { if (!_settings.HandleCancel()) LeaveSettings(); }
                else Close();
            }
        }

        /// <summary>Back out of the settings page: to the pause page in the workshop, all the way out on the title screen.</summary>
        private void LeaveSettings()
        {
            if (ShowSettingsOnly) Close(); else ShowSettings(false);
        }

        private static bool BenchActive()
        {
            var b = FindAnyObjectByType<Cracking.CrackingBench>();
            return b != null && b.Active;
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            CursorController.EnterMenu();
            var hud = HudController.Instance;
            if (hud != null) hud.SetFreeRoamVisible(false);
            _dim.style.display = DisplayStyle.Flex;
            ShowSettings(ShowSettingsOnly);
            Time.timeScale = 0f;
            WorkshopAudio.Play2D("ui_click", 0.4f);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Time.timeScale = 1f;
            _dim.style.display = DisplayStyle.None;
            CursorController.ExitMenu();
            var hud = HudController.Instance;
            if (hud != null) hud.SetFreeRoamVisible(true);
            GameSettings.Current.Save();
            var session = GameSession.Instance;
            if (session != null) session.FlushSave("pause-close");
            if (ShowSettingsOnly) TitleScreen.RefocusMenu();
        }

        private void ShowSettings(bool show)
        {
            _mainPage.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
            _settingsPage.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            // controller users land on the first control, not on the Back button at the bottom; the page was
            // display:none a moment ago, so focus it once the panel has laid it out
            if (show) { _settings.Select(0, false); _settings.FocusFirstControl(); }
            else { _resume.Focus(); _panel.schedule.Execute(() => { if (IsOpen) _resume.Focus(); }); }
            if (!show) GameSettings.Current.Save();
        }

        private void QuitToTitle()
        {
            Time.timeScale = 1f;
            var session = GameSession.Instance;
            if (session != null) session.FlushSave("quit-to-title");
            GameSettings.Current.Save();
            CursorController.Reset();
            CursorController.EnterMenu();
            SceneManager.LoadScene("Title");
        }
    }
}
