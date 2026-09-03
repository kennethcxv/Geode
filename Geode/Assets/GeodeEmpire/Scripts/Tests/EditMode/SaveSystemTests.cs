using System.IO;
using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Tests
{
    /// <summary>A one-in-a-million specimen must survive a bad write: atomic replace + backup recovery.</summary>
    public class SaveSystemTests
    {
        private string _scratch;

        [SetUp]
        public void Stash()
        {
            // never the real career: every test runs against its own scratch folder
            _scratch = Path.Combine(Path.GetTempPath(), "GeodeEmpireTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_scratch);
            SaveSystem.DirectoryOverride = _scratch;
            SaveSystem.Delete();
        }

        [TearDown]
        public void Restore()
        {
            SaveSystem.Delete();
            SaveSystem.DirectoryOverride = null;
            try { Directory.Delete(_scratch, true); } catch (System.Exception) { }
        }

        private static GameState Sample()
        {
            var s = new GameState { SaveId = "test-" + System.Guid.NewGuid().ToString("N"), WorldSeed = 42, Cash = 321f };
            var r = new SpecimenRecord { Id = "S1", Seed = 999, Location = SpecimenLocation.DisplaySlot, LocationIndex = 3, Appraised = true, AppraisedValue = 1234f, CustomName = "My Rock" };
            r.Condition.Opened = true;
            r.Condition.CrystalDamage = new byte[] { 0, 1, 2, 3 };
            r.SectorStress = new float[] { 0.5f, 1f, 0.2f };
            s.Specimens.Add(r);
            return s;
        }

        [Test]
        public void SaveThenLoadRoundTrips()
        {
            var s = Sample();
            SaveSystem.Save(s);
            var loaded = SaveSystem.Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(s.SaveId, loaded.SaveId);
            Assert.AreEqual(321f, loaded.Cash);
            var r = loaded.FindSpecimen("S1");
            Assert.AreEqual(999UL, r.Seed);
            Assert.AreEqual(SpecimenLocation.DisplaySlot, r.Location);
            Assert.AreEqual(3, r.LocationIndex);
            Assert.AreEqual(1234f, r.AppraisedValue);
            Assert.AreEqual("My Rock", r.CustomName);
            CollectionAssert.AreEqual(new byte[] { 0, 1, 2, 3 }, r.Condition.CrystalDamage);
            Assert.AreEqual(1f, r.SectorStress[1]);
            Assert.AreEqual(SpecimenGenerator.Generate(999).BaseValue, r.Geology.BaseValue, "geology regenerates identically from the seed");
        }

        [Test]
        public void CorruptMainFileFallsBackToBackup()
        {
            var first = Sample();
            SaveSystem.Save(first);
            var second = Sample();
            second.Cash = 999f;
            SaveSystem.Save(second);                       // first becomes .bak
            File.WriteAllText(SaveSystem.MainPath, "{ this is not json");
            var loaded = SaveSystem.Load();
            Assert.IsNotNull(loaded, "backup must be used when the main save is unreadable");
            Assert.AreEqual(first.SaveId, loaded.SaveId);
        }

        [Test]
        public void SecondSaveKeepsPreviousAsBackup()
        {
            SaveSystem.Save(Sample());
            SaveSystem.Save(Sample());
            Assert.IsTrue(File.Exists(SaveSystem.BackupPath));
            Assert.IsFalse(File.Exists(SaveSystem.TempPath), "temp file must not linger");
        }
    }
}
