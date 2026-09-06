using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using GeodeEmpire.Core;
using GeodeEmpire.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeodeEmpire.EditorTools
{
    /// <summary>Explicit, isolated automation sessions. Never writes to the player's real data directory.</summary>
    [InitializeOnLoad]
    public static class AstraQaSession
    {
        private const string ActiveKey = "GeodeEmpire.AstraQA.ActiveManifest";
        private const string LastKey = "GeodeEmpire.AstraQA.LastManifest";

        [Serializable]
        public sealed class ProtectedFile
        {
            public string name;
            public string sha256;
        }

        [Serializable]
        public sealed class Session
        {
            public string label;
            public string manifestPath;
            public string originalDirectory;
            public string isolatedDirectory;
            public string phase;
            public string startedUtc;
            public string endedUtc;
            public bool copiedCareer;
            public bool originalDataUnchanged;
            public ProtectedFile[] originals;
            public string[] failures;
        }

        static AstraQaSession()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            // SessionState survives a domain reload. Reinstate isolation before runtime initialization.
            try
            {
                var session = Read(ActiveKey);
                if (session != null) SaveSystem.DirectoryOverride = session.isolatedDirectory;
            }
            catch (Exception e)
            {
                EditorApplication.isPlaying = false;
                Debug.LogError("[Astra QA] Cannot restore isolation: " + e.Message);
            }
        }

        public static Session Prepare(string label, bool copyCareer = false)
        {
            RequireStopped();
            if (Read(ActiveKey) != null || !string.IsNullOrEmpty(SaveSystem.DirectoryOverride))
                throw new InvalidOperationException("Finish the existing QA/save override before preparing another session.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("QA requires clean scenes: " + SceneManager.GetSceneAt(i).path);

            string run = Path.GetFullPath(Path.Combine(Application.dataPath, "../Output/AstraQA", Guid.NewGuid().ToString("N")));
            var session = new Session
            {
                label = label,
                manifestPath = Path.Combine(run, "session.json"),
                originalDirectory = Path.GetFullPath(Application.persistentDataPath),
                isolatedDirectory = Path.Combine(run, "player-data"),
                phase = "Prepared",
                startedUtc = DateTime.UtcNow.ToString("O"),
                copiedCareer = copyCareer,
                failures = Array.Empty<string>()
            };
            session.originals = Snapshot(session.originalDirectory);
            string backup = Path.Combine(run, "originals");
            Directory.CreateDirectory(backup);
            Directory.CreateDirectory(session.isolatedDirectory);
            foreach (var file in session.originals)
            {
                string source = Path.Combine(session.originalDirectory, file.name);
                string copy = Path.Combine(backup, file.name);
                File.Copy(source, copy, false);
                if (Hash(copy) != file.sha256) throw new IOException("Backup verification failed: " + file.name);
                if (copyCareer || file.name.StartsWith("settings.json", StringComparison.Ordinal))
                    File.Copy(copy, Path.Combine(session.isolatedDirectory, file.name), false);
            }
            Write(session);
            SessionState.SetString(ActiveKey, session.manifestPath);
            SessionState.SetString(LastKey, session.manifestPath);
            SaveSystem.DirectoryOverride = session.isolatedDirectory;
            ValidatePrepared();
            return session;
        }

        /// <summary>Only enter after Prepare succeeds. Rechecks paths and original hashes in the same call as entry.</summary>
        public static Session Enter()
        {
            RequireStopped();
            ValidatePrepared();
            var session = Read(ActiveKey);
            EditorApplication.isPlaying = true;
            return session;
        }

        public static void ValidatePrepared()
        {
            var session = Read(ActiveKey);
            if (session == null) throw new InvalidOperationException("No prepared isolated QA session. Play entry refused.");
            if (!Directory.Exists(session.isolatedDirectory)
                || SaveSystem.DirectoryOverride != session.isolatedDirectory
                || SaveSystem.MainPath != Path.Combine(session.isolatedDirectory, SaveSystem.FileName)
                || GameSettings.FilePath != Path.Combine(session.isolatedDirectory, "settings.json"))
                throw new InvalidOperationException("Career/settings paths do not exactly match the prepared isolation directory. Play entry refused.");
            var failures = CompareOriginals(session);
            if (failures.Length != 0)
                throw new InvalidOperationException("Original player data changed: " + string.Join(", ", failures));
        }

        public static Session Status() => Read(ActiveKey) ?? Read(LastKey);

        /// <summary>Also called automatically after leaving Play, so interruption cannot leave the override active.</summary>
        public static Session Finish()
        {
            RequireStopped();
            var session = Read(ActiveKey);
            if (session == null) return Read(LastKey);
            SaveSystem.DirectoryOverride = null;
            session.failures = CompareOriginals(session);
            session.originalDataUnchanged = session.failures.Length == 0;
            session.phase = session.originalDataUnchanged ? "Finished" : "FailedPreservation";
            session.endedUtc = DateTime.UtcNow.ToString("O");
            Write(session);
            SessionState.EraseString(ActiveKey);
            if (!session.originalDataUnchanged)
                Debug.LogError("[Astra QA] Player-data preservation failed: " + string.Join(", ", session.failures));
            return session;
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (string.IsNullOrEmpty(SessionState.GetString(ActiveKey, ""))) return;
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                try { ValidatePrepared(); }
                catch (Exception e) { EditorApplication.isPlaying = false; Debug.LogError("[Astra QA] " + e.Message); }
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                var session = Read(ActiveKey);
                session.phase = "Playing";
                Write(session);
            }
            else if (state == PlayModeStateChange.EnteredEditMode) Finish();
        }

        private static void RequireStopped()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Wait until the Editor has stopped playing, compiling, and importing.");
        }

        private static Session Read(string key)
        {
            string path = SessionState.GetString(key, "");
            if (string.IsNullOrEmpty(path)) return null;
            var session = JsonUtility.FromJson<Session>(File.ReadAllText(path));
            if (session == null || string.IsNullOrEmpty(session.isolatedDirectory))
                throw new InvalidDataException("Invalid QA manifest: " + path);
            return session;
        }

        private static void Write(Session session) => File.WriteAllText(session.manifestPath, JsonUtility.ToJson(session, true));

        private static ProtectedFile[] Snapshot(string directory)
        {
            if (!Directory.Exists(directory)) return Array.Empty<ProtectedFile>();
            return Directory.GetFiles(directory).Where(p =>
                Path.GetFileName(p).StartsWith(SaveSystem.FileName, StringComparison.Ordinal)
                || Path.GetFileName(p).StartsWith("settings.json", StringComparison.Ordinal))
                .OrderBy(Path.GetFileName, StringComparer.Ordinal)
                .Select(p => new ProtectedFile { name = Path.GetFileName(p), sha256 = Hash(p) }).ToArray();
        }

        private static string[] CompareOriginals(Session session)
        {
            var current = Snapshot(session.originalDirectory).ToDictionary(f => f.name, f => f.sha256);
            var expected = session.originals.ToDictionary(f => f.name, f => f.sha256);
            return current.Keys.Union(expected.Keys).Where(name => !current.TryGetValue(name, out var hash)
                || !expected.TryGetValue(name, out var original) || hash != original).OrderBy(x => x).ToArray();
        }

        private static string Hash(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
