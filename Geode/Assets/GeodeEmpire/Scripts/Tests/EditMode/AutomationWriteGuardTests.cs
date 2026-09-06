using System;
using System.IO;
using GeodeEmpire.Core;
using GeodeEmpire.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GeodeEmpire.Tests
{
    public class AutomationWriteGuardTests
    {
        private string _previousDirectory, _scratch;
        private Action<string> _previousGuard;
        [SetUp]
        public void SetUp()
        {
            _previousDirectory = SaveSystem.DirectoryOverride;
            _previousGuard = SaveSystem.BeforeDirectoryWrite;
            _scratch = Path.Combine(Path.GetTempPath(), "geode-write-guard-" + Guid.NewGuid().ToString("N"));
            SaveSystem.DirectoryOverride = _scratch;
        }
        [TearDown]
        public void TearDown()
        {
            SaveSystem.DirectoryOverride = _previousDirectory;
            SaveSystem.BeforeDirectoryWrite = _previousGuard;
            if (Directory.Exists(_scratch)) Directory.Delete(_scratch, true);
        }
        private static void Refuse(string directory) => throw new IOException("write-guard-negative-control");

        [Test]
        public void ExistingSaveTestFixtureRestoresAnOuterIsolationSession()
        {
            var fixture = new SaveSystemTests();
            try { fixture.Stash(); fixture.SaveThenLoadRoundTrips(); }
            finally { fixture.Restore(); }
            Assert.That(SaveSystem.DirectoryOverride, Is.EqualTo(_scratch));
        }

        [Test]
        public void RefusalPreventsCareerSettingsAndTemporaryFileCreation()
        {
            SaveSystem.BeforeDirectoryWrite = Refuse;
            LogAssert.Expect(LogType.Error, "[SaveSystem] save failed: write-guard-negative-control");
            SaveSystem.Save(new GameState { SaveId = "blocked" });
            LogAssert.Expect(LogType.Error, "[GameSettings] save failed: write-guard-negative-control");
            new GameSettings().Save();
            Assert.That(Directory.Exists(_scratch), Is.False);
        }

        [Test]
        public void RefusalPreservesAllExistingFilesDuringDelete()
        {
            Directory.CreateDirectory(_scratch);
            foreach (string path in new[] { SaveSystem.MainPath, SaveSystem.BackupPath, SaveSystem.TempPath }) File.WriteAllText(path, "untouched");
            SaveSystem.BeforeDirectoryWrite = Refuse;
            LogAssert.Expect(LogType.Error, "[SaveSystem] delete failed: write-guard-negative-control");
            SaveSystem.Delete();
            foreach (string path in new[] { SaveSystem.MainPath, SaveSystem.BackupPath, SaveSystem.TempPath })
                Assert.That(File.ReadAllText(path), Is.EqualTo("untouched"));
        }

        [Test]
        public void RecoveryCanReadBackupButCannotPromoteItAcrossARefusedBoundary()
        {
            Directory.CreateDirectory(_scratch);
            string original = JsonUtility.ToJson(new GameState { SaveId = "recoverable", Cash = 82.5f });
            File.WriteAllText(SaveSystem.BackupPath, original);
            SaveSystem.BeforeDirectoryWrite = Refuse;
            LogAssert.Expect(LogType.Warning, "[SaveSystem] main save unreadable, restored from geode_career.json.bak");
            LogAssert.Expect(LogType.Warning, "[SaveSystem] could not promote recovered save: write-guard-negative-control");
            var loaded = SaveSystem.Load();
            Assert.That(loaded.SaveId, Is.EqualTo("recoverable"));
            Assert.That(loaded.Cash, Is.EqualTo(82.5f));
            Assert.That(File.Exists(SaveSystem.MainPath), Is.False);
            Assert.That(File.ReadAllText(SaveSystem.BackupPath), Is.EqualTo(original));
        }

        [Test]
        public void AllowedIsolatedCareerAndSettingsStillSaveAndReload()
        {
            int writes = 0;
            SaveSystem.BeforeDirectoryWrite = directory => { Assert.That(directory, Is.EqualTo(_scratch)); writes++; };
            SaveSystem.Save(new GameState { SaveId = "allowed", Cash = 125.75f });
            new GameSettings { MouseSensitivity = 1.27f }.Save();
            Assert.That(SaveSystem.Load().Cash, Is.EqualTo(125.75f));
            Assert.That(GameSettings.Load().MouseSensitivity, Is.EqualTo(1.27f));
            Assert.That(writes, Is.EqualTo(2));
        }
    }
}
