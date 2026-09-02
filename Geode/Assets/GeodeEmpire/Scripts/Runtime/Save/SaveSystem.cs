using System;
using System.IO;
using UnityEngine;

namespace GeodeEmpire.Save
{
    /// <summary>
    /// Atomic JSON persistence with a rolling backup. Write to .tmp, keep previous as .bak, then move into place,
    /// so a crash mid-write never destroys a career.
    /// </summary>
    public static class SaveSystem
    {
        public const string FileName = "geode_career.json";

        public static string Directory => Application.persistentDataPath;
        public static string MainPath => Path.Combine(Directory, FileName);
        public static string BackupPath => MainPath + ".bak";
        public static string TempPath => MainPath + ".tmp";

        public static event Action<GameState> Saved;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Saved = null; }

        public static bool Exists() => File.Exists(MainPath) || File.Exists(BackupPath);

        public static void Save(GameState state)
        {
            if (state == null) return;
            state.LastSavedTicks = DateTime.UtcNow.Ticks;
            state.Version = GameState.CurrentVersion;
            string json = JsonUtility.ToJson(state, false);
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                File.WriteAllText(TempPath, json);
                // sanity: re-read what we wrote before replacing the live save
                var check = JsonUtility.FromJson<GameState>(File.ReadAllText(TempPath));
                if (check == null || check.SaveId != state.SaveId) throw new IOException("save verification failed");
                if (File.Exists(MainPath))
                {
                    if (File.Exists(BackupPath)) File.Delete(BackupPath);
                    File.Move(MainPath, BackupPath);
                }
                File.Move(TempPath, MainPath);
                Saved?.Invoke(state);
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] save failed: " + e.Message);
            }
        }

        public static GameState Load()
        {
            var s = TryLoad(MainPath);
            if (s != null) return s;
            s = TryLoad(BackupPath);
            if (s != null) Debug.LogWarning("[SaveSystem] main save unreadable, restored from backup");
            return s;
        }

        private static GameState TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var s = JsonUtility.FromJson<GameState>(File.ReadAllText(path));
                if (s == null || string.IsNullOrEmpty(s.SaveId)) return null;
                Migrate(s);
                return s;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] could not read {path}: {e.Message}");
                return null;
            }
        }

        private static void Migrate(GameState s)
        {
            if (s.Version < 1) s.Version = 1;
            if (s.DisplayCapacity <= 0) s.DisplayCapacity = 8;
            foreach (var r in s.Specimens)
            {
                r.Condition ??= new GeodeEmpire.Specimens.SpecimenCondition();
                r.SectorStress ??= Array.Empty<float>();
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(MainPath)) File.Delete(MainPath);
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
                if (File.Exists(TempPath)) File.Delete(TempPath);
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] delete failed: " + e.Message);
            }
        }
    }
}
