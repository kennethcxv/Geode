using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.UI
{
    /// <summary>Title flow: New Game / Continue / Settings / Quit over a lit hero specimen.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TitleScreen : MonoBehaviour
    {
        private VisualElement _root, _menu, _confirm;
        private Button _continue, _newGame, _settings, _quit;
        private static TitleScreen _instance;

        /// <summary>Put controller focus back on the title menu after the settings panel closes.</summary>
        public static void RefocusMenu()
        {
            if (_instance != null && _instance._settings != null) _instance._settings.Focus();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Start()
        {
            _instance = this;
            GameInput.Ensure();
            GameSettings.Current.Apply();
            CursorController.Reset();
            CursorController.EnterMenu();
            Time.timeScale = 1f;
            var doc = GetComponent<UIDocument>();
            if (doc.panelSettings == null) doc.panelSettings = Resources.Load<PanelSettings>("UI/GeodePanelSettings");
            _root = doc.rootVisualElement;
            _root.Clear();
            var ss = Resources.Load<StyleSheet>("UI/GeodeUI");
            if (ss != null) _root.styleSheets.Add(ss);

            var layer = UiKit.Box(_root, "hud-root");
            var left = UiKit.Box(layer);
            left.style.position = Position.Absolute;
            left.style.left = 110; left.style.top = Length.Percent(24);
            var title = UiKit.Label(left, "GEODE EMPIRE", "bold");
            title.style.fontSize = 74;
            title.style.letterSpacing = 6;
            var sub = UiKit.Label(left, "Buy the crate. Crack the rock. Decide what you keep.", "muted");
            sub.style.fontSize = 22;
            sub.style.marginBottom = 40;
            _menu = UiKit.Box(left);
            _menu.style.width = 340;
            bool hasSave = SaveSystem.Exists();
            if (hasSave)
            {
                _continue = UiKit.Button(_menu, "Continue", Continue, "btn-primary");
                _continue.style.marginBottom = 10;
            }
            _newGame = UiKit.Button(_menu, "New Game", NewGame, hasSave ? "" : "btn-primary");
            _newGame.style.marginBottom = 10;
            _settings = UiKit.Button(_menu, "Settings", () => PauseMenu.Instance?.Open());
            _settings.style.marginBottom = 10;
            _quit = UiKit.Button(_menu, "Quit", () => Application.Quit(), "btn-ghost");
            var ver = UiKit.Label(layer, "Vertical slice  •  keyboard/mouse or controller", "muted");
            ver.style.position = Position.Absolute; ver.style.left = 110; ver.style.bottom = 40;

            _confirm = UiKit.Box(_root, "panel-dim");
            var panel = UiKit.Box(_confirm, "panel");
            UiKit.Label(panel, "Start a new workshop?", "panel-title", "bold");
            UiKit.Label(panel, "Your current workshop, collection and cash will be erased.", "panel-subtitle");
            var row = UiKit.Box(panel, "row");
            var yes = UiKit.Button(row, "Erase and start over", StartNew, "btn-primary");
            yes.style.marginRight = 10;
            UiKit.Button(row, "Cancel", () => { _confirm.style.display = DisplayStyle.None; _newGame.Focus(); }, "btn-ghost");
            _confirm.style.display = DisplayStyle.None;
            (hasSave ? _continue : _newGame).Focus();
        }

        private void Continue()
        {
            WorkshopAudio.Play2D("ui_click", 0.5f);
            GameSession.PendingStart = SessionStartMode.Continue;
            SceneManager.LoadScene("Workshop");
        }

        private void NewGame()
        {
            WorkshopAudio.Play2D("ui_click", 0.5f);
            if (SaveSystem.Exists()) { _confirm.style.display = DisplayStyle.Flex; _confirm.Q<Button>()?.Focus(); }
            else StartNew();
        }

        private void StartNew()
        {
            SaveSystem.Delete();
            GameSession.PendingStart = SessionStartMode.NewGame;
            SceneManager.LoadScene("Workshop");
        }

        private void Update()
        {
            var gp = UnityEngine.InputSystem.Gamepad.current;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (_confirm.style.display == DisplayStyle.Flex && ((gp != null && gp.buttonEast.wasPressedThisFrame) || (kb != null && kb.escapeKey.wasPressedThisFrame)))
                _confirm.style.display = DisplayStyle.None;
        }
    }
}
