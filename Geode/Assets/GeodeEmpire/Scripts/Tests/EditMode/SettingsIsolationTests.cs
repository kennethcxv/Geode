using System;
using System.IO;
using GeodeEmpire.Core;
using GeodeEmpire.Save;
using NUnit.Framework;

namespace GeodeEmpire.Tests
{
    public class SettingsIsolationTests
    {
        [Test]
        public void IsolatedSettingsSaveAndReloadUseTheCareerDirectory()
        {
            string previous = SaveSystem.DirectoryOverride;
            string scratch = Path.Combine(Path.GetTempPath(), "geode-settings-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                SaveSystem.DirectoryOverride = scratch;
                // Assert the destination before writing: a regression must never write to the real settings file.
                Assert.That(GameSettings.FilePath, Is.EqualTo(Path.Combine(scratch, "settings.json")));
                new GameSettings { MouseSensitivity = 1.73f, MasterVolume = 0.37f }.Save();
                var restored = GameSettings.Load();
                Assert.That(restored.MouseSensitivity, Is.EqualTo(1.73f).Within(0.0001f));
                Assert.That(restored.MasterVolume, Is.EqualTo(0.37f).Within(0.0001f));
            }
            finally
            {
                SaveSystem.DirectoryOverride = previous;
                if (Directory.Exists(scratch)) Directory.Delete(scratch, true);
            }
        }
    }
}
