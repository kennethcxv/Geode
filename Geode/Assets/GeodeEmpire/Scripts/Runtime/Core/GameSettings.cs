using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GeodeEmpire.Core
{
    /// <summary>
    /// Player settings, persisted as JSON next to the save file. Every field is bound to a visible control and read by
    /// something in the game; nothing decorative. Apply() pushes the engine-level ones live.
    /// </summary>
    [Serializable]
    public sealed class GameSettings
    {
        public const int CurrentVersion = 2;
        public int Version = CurrentVersion;

        // ---- gameplay
        public bool ShowTutorial = true;
        public float CameraShake = 1f;          // 0..1.5
        public float Vibration = 1f;            // 0..1 controller rumble

        // ---- controls
        public float MouseSensitivity = 1f;     // 0.2..3
        public float GamepadSensitivity = 1f;   // 0.2..3
        public bool InvertY = false;
        public float StickDeadzone = 0.125f;    // 0.05..0.4

        // ---- camera / accessibility
        public float FieldOfView = 70f;         // 55..95
        public float HeadBobAmount = 1f;        // 0..1
        public bool ReducedMotion = false;      // no head bob, no camera shake
        public float UiScale = 1f;              // 0.8..1.4
        public bool CrosshairVisible = true;

        // ---- graphics
        public int DisplayMode = 0;             // 0 fullscreen (borderless window), 1 exclusive fullscreen, 2 windowed
        public int ResolutionWidth = 0;         // 0 = native
        public int ResolutionHeight = 0;
        public bool VSync = true;
        public int FrameRateLimit = 0;          // 0 = uncapped
        public int QualityPreset = 1;           // 0 low, 1 medium, 2 high, 3 custom
        public int ShadowQuality = 1;           // 0 off, 1 medium, 2 high
        public int AntiAliasing = 1;            // 0 off, 1 2x, 2 4x, 3 8x MSAA
        public float RenderScale = 1f;          // 0.7..1.3
        public bool PostProcessing = true;
        public float Brightness = 1f;           // 0.6..1.4

        // ---- audio
        public float MasterVolume = 0.9f;
        public float SfxVolume = 1f;
        public float AmbienceVolume = 0.8f;
        public float UiVolume = 0.8f;
        public float MusicVolume = 0.45f;

        // ---- controls: the Input System's own binding-override JSON (V6 §62)
        public string Bindings = "";

        // ---- v1 fields, kept only so old files migrate
        public bool HeadBob = true;
        public bool Fullscreen = false;

        public static readonly string[] DisplayModeNames = { "Fullscreen", "Exclusive fullscreen", "Windowed" };
        public static readonly string[] QualityNames = { "Low", "Medium", "High", "Custom" };
        public static readonly string[] ShadowNames = { "Off", "Medium", "High" };
        public static readonly string[] AntiAliasingNames = { "Off", "2× MSAA", "4× MSAA", "8× MSAA" };
        public static readonly int[] FrameRateOptions = { 0, 30, 60, 120, 144 };
        public const float BaseExposure = 0.15f;   // the workshop volume's postExposure; brightness offsets from it

        public static event Action Changed;

        /// <summary>What ApplyDisplay last asked the platform for (the Editor's Game view cannot actually change).</summary>
        public static string DisplayApplied { get; private set; } = "";

        private static GameSettings _current;
        private static Volume _brightnessVolume;
        private static ColorAdjustments _brightnessAdj;
        private static PanelSettings _panel;
        private static bool _sceneHooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _current = null; Changed = null; _brightnessVolume = null; _brightnessAdj = null; _panel = null; _sceneHooked = false; DisplayApplied = ""; }

        public static GameSettings Current
        {
            get
            {
                if (_current == null) _current = Load();
                if (!_sceneHooked) { _sceneHooked = true; SceneManager.sceneLoaded += OnSceneLoaded; }
                return _current;
            }
        }

        private static void OnSceneLoaded(Scene s, LoadSceneMode m) { _current?.ApplyCamera(Camera.main); _current?.ApplyUiScale(); }

        public static string FilePath => Path.Combine(GeodeEmpire.Save.SaveSystem.Directory, "settings.json");

        public float EffectiveHeadBob => ReducedMotion ? 0f : HeadBobAmount;
        public float EffectiveShake => ReducedMotion ? 0f : CameraShake;

        public static GameSettings Load()
        {
            GameSettings s = null;
            try
            {
                if (File.Exists(FilePath)) s = JsonUtility.FromJson<GameSettings>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameSettings] failed to load, using defaults: " + e.Message);
            }
            s ??= new GameSettings();
            s.Migrate();
            return s;
        }

        private void Migrate()
        {
            if (Version < 2)
            {
                HeadBobAmount = HeadBob ? 1f : 0f;
                DisplayMode = Fullscreen ? 0 : 2;
                Version = CurrentVersion;
            }
            Clamp();
        }

        public void Clamp()
        {
            CameraShake = Mathf.Clamp(CameraShake, 0f, 1.5f);
            Vibration = Mathf.Clamp01(Vibration);
            MouseSensitivity = Mathf.Clamp(MouseSensitivity, 0.2f, 3f);
            GamepadSensitivity = Mathf.Clamp(GamepadSensitivity, 0.2f, 3f);
            StickDeadzone = Mathf.Clamp(StickDeadzone, 0.05f, 0.4f);
            FieldOfView = Mathf.Clamp(Mathf.Round(FieldOfView), 55f, 95f);
            HeadBobAmount = Mathf.Clamp01(HeadBobAmount);
            UiScale = Mathf.Clamp(UiScale, 0.8f, 1.4f);
            DisplayMode = Mathf.Clamp(DisplayMode, 0, 2);
            QualityPreset = Mathf.Clamp(QualityPreset, 0, 3);
            ShadowQuality = Mathf.Clamp(ShadowQuality, 0, 2);
            AntiAliasing = Mathf.Clamp(AntiAliasing, 0, 3);
            RenderScale = Mathf.Clamp(RenderScale, 0.7f, 1.3f);
            Brightness = Mathf.Clamp(Brightness, 0.6f, 1.4f);
            MasterVolume = Mathf.Clamp01(MasterVolume);
            MusicVolume = Mathf.Clamp01(MusicVolume);
            SfxVolume = Mathf.Clamp01(SfxVolume);
            AmbienceVolume = Mathf.Clamp01(AmbienceVolume);
            UiVolume = Mathf.Clamp01(UiVolume);
            if (Array.IndexOf(FrameRateOptions, FrameRateLimit) < 0) FrameRateLimit = 0;
        }

        /// <summary>Static shorthand so anything holding a reference to a setting can persist it.</summary>
        public static void SaveCurrent() => Current.Save();

        public void Save()
        {
            try
            {
                HeadBob = HeadBobAmount > 0.001f;
                Fullscreen = DisplayMode != 2;
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(this, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("[GameSettings] save failed: " + e.Message);
            }
        }

        // ------------------------------------------------------------------------------------
        // Applying
        // ------------------------------------------------------------------------------------

        /// <summary>Push everything except the display mode/resolution (those go through ApplyDisplay so a bad choice can be reverted).</summary>
        public void Apply()
        {
            Clamp();
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = FrameRateLimit > 0 ? FrameRateLimit : -1;
            int names = QualitySettings.names.Length;
            if (names > 0 && QualitySettings.GetQualityLevel() != names - 1) QualitySettings.SetQualityLevel(names - 1, false);   // the PC URP asset
            ApplyRenderPipeline();
            ApplyCamera(Camera.main);
            ApplyBrightness();
            ApplyUiScale();
            ApplyInput();
            AudioListener.volume = MasterVolume;
            Changed?.Invoke();
        }

        public static UniversalRenderPipelineAsset Pipeline => GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        private void ApplyRenderPipeline()
        {
            var rp = Pipeline;
            if (rp == null) return;
            rp.renderScale = RenderScale;
            rp.msaaSampleCount = AntiAliasing == 0 ? 1 : AntiAliasing == 1 ? 2 : AntiAliasing == 2 ? 4 : 8;
            switch (ShadowQuality)
            {
                case 0: rp.shadowDistance = 0f; break;
                case 1: rp.shadowDistance = 18f; rp.shadowCascadeCount = 2; break;
                default: rp.shadowDistance = 26f; rp.shadowCascadeCount = 3; break;
            }
        }

        /// <summary>A preset is just three graphics choices made at once.</summary>
        public void ApplyPreset(int preset)
        {
            QualityPreset = Mathf.Clamp(preset, 0, 3);
            switch (QualityPreset)
            {
                case 0: ShadowQuality = 1; AntiAliasing = 0; RenderScale = 0.8f; break;
                case 1: ShadowQuality = 1; AntiAliasing = 1; RenderScale = 1f; break;
                case 2: ShadowQuality = 2; AntiAliasing = 2; RenderScale = 1f; break;
            }
        }

        /// <summary>An individual graphics control moved: the preset label follows (Custom unless it still matches one).</summary>
        public void RefreshPresetFromParts()
        {
            if (ShadowQuality == 1 && AntiAliasing == 0 && Mathf.Approximately(RenderScale, 0.8f)) QualityPreset = 0;
            else if (ShadowQuality == 1 && AntiAliasing == 1 && Mathf.Approximately(RenderScale, 1f)) QualityPreset = 1;
            else if (ShadowQuality == 2 && AntiAliasing == 2 && Mathf.Approximately(RenderScale, 1f)) QualityPreset = 2;
            else QualityPreset = 3;
        }

        public void ApplyCamera(Camera cam)
        {
            if (cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = PostProcessing;
        }

        /// <summary>Brightness is an exposure offset on a runtime global volume that outranks the scene volumes.</summary>
        private void ApplyBrightness()
        {
            if (_brightnessVolume == null)
            {
                var go = new GameObject("_BrightnessVolume");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _brightnessVolume = go.AddComponent<Volume>();
                _brightnessVolume.isGlobal = true;
                _brightnessVolume.priority = 100f;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _brightnessAdj = profile.Add<ColorAdjustments>(false);
                _brightnessVolume.sharedProfile = profile;
            }
            _brightnessAdj.postExposure.overrideState = true;
            _brightnessAdj.postExposure.value = BaseExposure + (Brightness - 1f) * 1.6f;
            _brightnessVolume.weight = Mathf.Approximately(Brightness, 1f) ? 0f : 1f;
        }

        public static float CurrentExposure => _brightnessVolume != null && _brightnessVolume.weight > 0f ? _brightnessAdj.postExposure.value : BaseExposure;

        /// <summary>UI scale drives the shared panel's reference resolution: 1.2 makes every screen 20% larger.</summary>
        public void ApplyUiScale()
        {
            if (_panel == null) _panel = Resources.Load<PanelSettings>("UI/GeodePanelSettings");
            if (_panel == null) return;
            var target = new Vector2Int(Mathf.RoundToInt(1920f / UiScale), Mathf.RoundToInt(1080f / UiScale));
            if (_panel.referenceResolution != target) _panel.referenceResolution = target;
        }

        public static Vector2Int PanelReferenceResolution => _panel != null ? _panel.referenceResolution : Vector2Int.zero;

        private void ApplyInput()
        {
            var settings = InputSystem.settings;
            if (settings != null && !Mathf.Approximately(settings.defaultDeadzoneMin, StickDeadzone)) settings.defaultDeadzoneMin = StickDeadzone;
        }

        // ---- display ----------------------------------------------------------------------

        public static bool ExclusiveFullscreenSupported => Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;

        public static List<Vector2Int> ResolutionOptions()
        {
            var list = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (v.x >= 1024 && !list.Contains(v)) list.Add(v);
            }
            if (list.Count == 0) list.Add(new Vector2Int(Display.main.systemWidth, Display.main.systemHeight));
            list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return list;
        }

        public Vector2Int EffectiveResolution()
        {
            if (ResolutionWidth > 0 && ResolutionHeight > 0) return new Vector2Int(ResolutionWidth, ResolutionHeight);
            int w = Display.main.systemWidth, h = Display.main.systemHeight;
            if (DisplayMode == 2) return new Vector2Int(Mathf.RoundToInt(w * 0.8f), Mathf.RoundToInt(h * 0.8f));   // a window that fits the desktop
            return new Vector2Int(w, h);
        }

        public FullScreenMode EffectiveMode => DisplayMode == 1 && ExclusiveFullscreenSupported ? FullScreenMode.ExclusiveFullScreen : DisplayMode == 2 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;

        /// <summary>Display mode and resolution. Separate from Apply() so the settings page can offer a timed revert.</summary>
        public void ApplyDisplay()
        {
            var mode = EffectiveMode;
            var res = EffectiveResolution();
            DisplayApplied = $"{res.x}x{res.y} {mode}";
            if (Application.isEditor) return;   // the Game view is not a window; the standalone player is where this is verified
            if (Screen.fullScreenMode == mode && Screen.width == res.x && Screen.height == res.y) return;
            Screen.SetResolution(res.x, res.y, mode);
        }

        // ---- reset ------------------------------------------------------------------------

        /// <summary>Tabs of the settings page: 0 gameplay, 1 controls, 2 camera, 3 graphics, 4 audio.</summary>
        public void ResetSection(int tab)
        {
            var d = new GameSettings();
            switch (tab)
            {
                case 0: ShowTutorial = d.ShowTutorial; CameraShake = d.CameraShake; Vibration = d.Vibration; break;
                // Controls includes the bindings: "reset section" on this page has to mean the whole page
                case 1: MouseSensitivity = d.MouseSensitivity; GamepadSensitivity = d.GamepadSensitivity; InvertY = d.InvertY; StickDeadzone = d.StickDeadzone; InputBindings.ResetAll(); break;
                case 2: FieldOfView = d.FieldOfView; HeadBobAmount = d.HeadBobAmount; ReducedMotion = d.ReducedMotion; UiScale = d.UiScale; CrosshairVisible = d.CrosshairVisible; break;
                case 3:
                    DisplayMode = d.DisplayMode; ResolutionWidth = d.ResolutionWidth; ResolutionHeight = d.ResolutionHeight; VSync = d.VSync; FrameRateLimit = d.FrameRateLimit;
                    QualityPreset = d.QualityPreset; ShadowQuality = d.ShadowQuality; AntiAliasing = d.AntiAliasing; RenderScale = d.RenderScale; PostProcessing = d.PostProcessing; Brightness = d.Brightness;
                    break;
                case 4: MasterVolume = d.MasterVolume; SfxVolume = d.SfxVolume; AmbienceVolume = d.AmbienceVolume; UiVolume = d.UiVolume; MusicVolume = d.MusicVolume; break;
            }
        }

        public void ResetAll() { for (int i = 0; i < 5; i++) ResetSection(i); }
    }
}
