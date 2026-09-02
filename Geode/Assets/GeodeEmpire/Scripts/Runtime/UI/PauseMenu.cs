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

        private VisualElement _dim, _panel, _mainPage, _settingsPage;
        private Button _resume, _settingsBtn, _quitBtn;

        private void Awake() => Instance = this;

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
            _settingsPage = UiKit.Box(_panel);
            BuildSettings(_settingsPage);
            _settingsPage.style.display = DisplayStyle.None;
            _dim.RegisterCallback<NavigationCancelEvent>(e => { if (_settingsPage.style.display == DisplayStyle.Flex) ShowSettings(false); else Close(); e.StopPropagation(); });
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var gp = UnityEngine.InputSystem.Gamepad.current;
            bool pausePressed = (kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.startButton.wasPressedThisFrame);
            bool backPressed = gp != null && gp.buttonEast.wasPressedThisFrame;
            if (!IsOpen)
            {
                if (pausePressed && !CursorController.InMenu && !BenchActive()) Open();
                return;
            }
            if (pausePressed || backPressed)
            {
                if (_settingsPage.style.display == DisplayStyle.Flex) ShowSettings(false);
                else Close();
            }
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
            HudController.Instance?.SetFreeRoamVisible(false);
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
            HudController.Instance?.SetFreeRoamVisible(true);
            GameSettings.Current.Save();
            GameSession.Instance?.FlushSave("pause-close");
        }

        private void ShowSettings(bool show)
        {
            _mainPage.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
            _settingsPage.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show) _settingsPage.Q<Button>()?.Focus(); else _resume.Focus();
            if (!show) GameSettings.Current.Save();
        }

        private void QuitToTitle()
        {
            Time.timeScale = 1f;
            GameSession.Instance?.FlushSave("quit-to-title");
            GameSettings.Current.Save();
            CursorController.Reset();
            CursorController.EnterMenu();
            SceneManager.LoadScene("Title");
        }

        // ---- settings page ------------------------------------------------------------------
        private void BuildSettings(VisualElement page)
        {
            UiKit.Label(page, "SETTINGS", "panel-title", "bold");
            UiKit.Label(page, "Changes apply immediately and persist.", "panel-subtitle");
            var s = GameSettings.Current;
            Slider(page, "Mouse sensitivity", 0.2f, 3f, s.MouseSensitivity, v => { s.MouseSensitivity = v; });
            Slider(page, "Controller sensitivity", 0.2f, 3f, s.GamepadSensitivity, v => { s.GamepadSensitivity = v; });
            Toggle(page, "Invert look Y", s.InvertY, v => { s.InvertY = v; });
            Slider(page, "Field of view", 55f, 95f, s.FieldOfView, v => { s.FieldOfView = Mathf.Round(v); s.Apply(); });
            Slider(page, "Camera shake", 0f, 1.5f, s.CameraShake, v => { s.CameraShake = v; });
            Toggle(page, "Head bob", s.HeadBob, v => { s.HeadBob = v; });
            Slider(page, "Master volume", 0f, 1f, s.MasterVolume, v => { s.MasterVolume = v; s.Apply(); });
            Slider(page, "Effects volume", 0f, 1f, s.SfxVolume, v => { s.SfxVolume = v; });
            Slider(page, "Music volume", 0f, 1f, s.MusicVolume, v => { s.MusicVolume = v; s.Apply(); });
            Slider(page, "Ambience volume", 0f, 1f, s.AmbienceVolume, v => { s.AmbienceVolume = v; s.Apply(); });
            var quality = new DropdownField("Graphics quality", new System.Collections.Generic.List<string> { "Low", "Medium", "High" }, Mathf.Clamp(s.QualityPreset, 0, 2));
            quality.RegisterValueChangedCallback(e => { s.QualityPreset = quality.index; s.Apply(); });
            page.Add(quality);
            Toggle(page, "VSync", s.VSync, v => { s.VSync = v; s.Apply(); });
            var fps = new DropdownField("Frame-rate limit", new System.Collections.Generic.List<string> { "Uncapped", "30", "60", "120" }, s.FrameRateLimit == 30 ? 1 : s.FrameRateLimit == 60 ? 2 : s.FrameRateLimit == 120 ? 3 : 0);
            fps.RegisterValueChangedCallback(e => { s.FrameRateLimit = fps.index == 1 ? 30 : fps.index == 2 ? 60 : fps.index == 3 ? 120 : 0; s.Apply(); });
            page.Add(fps);
            Toggle(page, "Fullscreen", s.Fullscreen, v => { s.Fullscreen = v; s.Apply(); });
            Toggle(page, "Show tutorial hints", s.ShowTutorial, v => { s.ShowTutorial = v; s.Apply(); });
            var back = UiKit.Button(page, "Back", () => ShowSettings(false), "btn-ghost");
            back.style.marginTop = 16;
        }

        private static void Slider(VisualElement parent, string label, float min, float max, float value, System.Action<float> onChange)
        {
            var row = UiKit.Box(parent, "slider-row");
            UiKit.Label(row, label, "slider-label");
            var sl = new Slider(min, max) { value = value };
            sl.RegisterValueChangedCallback(e => onChange(e.newValue));
            row.Add(sl);
            var val = UiKit.Label(row, value.ToString("F2"), "muted");
            val.style.width = 60;
            sl.RegisterValueChangedCallback(e => val.text = e.newValue.ToString("F2"));
        }

        private static void Toggle(VisualElement parent, string label, bool value, System.Action<bool> onChange)
        {
            var row = UiKit.Box(parent, "slider-row");
            UiKit.Label(row, label, "slider-label");
            var t = new UnityEngine.UIElements.Toggle { value = value };
            t.RegisterValueChangedCallback(e => onChange(e.newValue));
            row.Add(t);
        }
    }
}
