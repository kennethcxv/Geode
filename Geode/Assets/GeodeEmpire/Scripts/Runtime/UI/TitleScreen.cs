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

        private void Start()
        {
            GameInput.Ensure();
            GameSettings.Current.Apply();
            CursorController.Reset();
            CursorController.EnterMenu();
            Time.timeScale = 1f;
            var doc = GetComponent<UIDocument>();
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

    /// <summary>Builds and slowly turns a museum-grade specimen for the title backdrop.</summary>
    public sealed class TitleHero : MonoBehaviour
    {
        public float TurnSpeed = 9f;
        private Transform _spec;

        private void Start()
        {
            var lib = SpecimenAssetLibrary.Load();
            ulong seed = FindHeroSeed();
            var g = SpecimenGenerator.Generate(seed);
            var go = new GameObject("HeroSpecimen");
            go.transform.SetParent(transform, false);
            var vis = go.AddComponent<SpecimenVisual>();
            vis.Build(g, new SpecimenCondition { Opened = true }, lib);
            vis.SetCrystalsVisible(true);
            var geo = vis.Geometry;
            vis.TopHalf.localRotation = Quaternion.Euler(0f, 0f, 180f);
            vis.TopHalf.localPosition = new Vector3(-geo.MeanEquatorRadius * 2.3f, geo.BottomY + geo.TopY, 0f);
            go.transform.localPosition = new Vector3(0f, -geo.BottomY, 0f);
            float scale = 0.11f / Mathf.Max(0.02f, geo.MaxRadius);
            go.transform.localScale = Vector3.one * Mathf.Clamp(scale, 0.8f, 2.5f);
            _spec = go.transform;
        }

        private static ulong FindHeroSeed()
        {
            for (ulong seed = 90001; seed < 90001 + 20000; seed++)
            {
                var g = SpecimenGenerator.Generate(seed);
                if (g.Tier >= QualityTier.MuseumGrade && (g.Mineral == MineralId.Amethyst || g.Mineral == MineralId.Celestite || g.Mineral == MineralId.Fluorite) && g.Cavity != CavityArchetype.Nodule) return seed;
            }
            return 90001;
        }

        private void Update()
        {
            if (_spec != null) transform.Rotate(0f, TurnSpeed * Time.deltaTime, 0f, Space.World);
        }
    }
}
