using System;
using System.IO;
using UnityEngine;

namespace GeodeEmpire.Core
{
    /// <summary>Player comfort/graphics/audio settings, persisted as JSON next to the save file.</summary>
    [Serializable]
    public sealed class GameSettings
    {
        public int Version = 1;
        public float MouseSensitivity = 1f;
        public float GamepadSensitivity = 1f;
        public bool InvertY = false;
        public float FieldOfView = 70f;
        public float CameraShake = 1f;
        public float MasterVolume = 0.9f;
        public float SfxVolume = 1f;
        public float MusicVolume = 0.6f;
        public float AmbienceVolume = 0.8f;
        public bool VSync = true;
        public int FrameRateLimit = 0;      // 0 = uncapped (vsync governs)
        public int QualityPreset = 1;       // 0 low, 1 medium, 2 high
        public bool Fullscreen = false;
        public bool HeadBob = true;
        public bool ShowTutorial = true;

        public static event Action Changed;

        private static GameSettings _current;
        public static GameSettings Current
        {
            get
            {
                if (_current == null) _current = Load();
                return _current;
            }
        }

        public static string FilePath => Path.Combine(Application.persistentDataPath, "settings.json");

        public static GameSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var s = JsonUtility.FromJson<GameSettings>(File.ReadAllText(FilePath));
                    if (s != null) return s;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameSettings] failed to load, using defaults: " + e.Message);
            }
            return new GameSettings();
        }

        public void Save()
        {
            try
            {
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

        /// <summary>Push graphics-level settings to the engine and notify listeners.</summary>
        public void Apply()
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = FrameRateLimit > 0 ? FrameRateLimit : -1;
            int names = QualitySettings.names.Length;
            if (names > 0) QualitySettings.SetQualityLevel(Mathf.Clamp(names - 1, 0, names - 1), false); // PC preset (URP asset), see ApplyRenderScale
            ApplyRenderScale();
            if (Screen.fullScreen != Fullscreen) Screen.fullScreen = Fullscreen;
            AudioListener.volume = MasterVolume;
            Changed?.Invoke();
        }

        private void ApplyRenderScale()
        {
            var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (rp == null) return;
            switch (QualityPreset)
            {
                case 0: rp.renderScale = 0.8f; rp.shadowDistance = 12f; rp.msaaSampleCount = 1; break;
                case 1: rp.renderScale = 1f; rp.shadowDistance = 18f; rp.msaaSampleCount = 2; break;
                default: rp.renderScale = 1f; rp.shadowDistance = 25f; rp.msaaSampleCount = 4; break;
            }
        }
    }
}
