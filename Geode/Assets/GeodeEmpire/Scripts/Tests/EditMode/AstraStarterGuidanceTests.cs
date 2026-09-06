using System.Linq;
using GeodeEmpire.Build;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;
using GeodeEmpire.Save;
using GeodeEmpire.Workshop;
using NUnit.Framework;
using UnityEngine;

namespace GeodeEmpire.Tests
{
    public class AstraStarterGuidanceTests
    {
        private static GameState Fresh()
        {
            var state = new GameState { SaveId = "astra-guidance-test", Cash = 120f };
            AstraLayoutMigration.InitializeNew(state);
            return state;
        }

        private static void Opening(GameState state)
        {
            foreach (string action in new[] { "moved", "crate_bought", "crate_opened", "rock_picked" })
                Tutorial.RecordAction(state, action);
        }

        [Test]
        public void MinimalKitTeachesCrackingBeforeEquipmentThePlayerDoesNotOwn()
        {
            var state = Fresh();
            Opening(state);
            Assert.That(Tutorial.CurrentFor(state).Id, Is.EqualTo("bench"));
            Tutorial.RecordAction(state, "rock_on_bench");
            Assert.That(state.TutorialDone("wash"), Is.False, "Skipping a station must not erase its later lesson.");
            Tutorial.RecordAction(state, "first_strike");
            Tutorial.RecordAction(state, "rock_opened");
            Tutorial.RecordAction(state, "specimen_picked");
            Assert.That(Tutorial.CurrentFor(state).Id, Is.EqualTo("sort"));
            Tutorial.RecordAction(state, "specimen_sorted");
            Assert.That(Tutorial.CurrentFor(state).Id, Is.EqualTo("ship"));
            Assert.That(state.TutorialDone("rinse"), Is.False);
            Assert.That(state.TutorialDone("appraise"), Is.False);
        }

        [Test]
        public void DeferredWashLessonSurvivesReloadAndWaitsForPlacement()
        {
            var state = Fresh();
            Opening(state);
            Tutorial.RecordAction(state, "rock_on_bench");
            state.Upgrades.Add(UpgradeCatalog.BackRoom);
            state.Upgrades.Add(UpgradeCatalog.WashStation);
            var pose = state.SetFixture("wash_station", new Vector3(-5.95f, 0f, 3f), 90f, false);
            pose.Delivered = true;
            state = SaveSystem.Parse(JsonUtility.ToJson(state));
            Assert.That(Tutorial.CurrentFor(state).Id, Is.EqualTo("strike"), "A purchased parcel is not a usable basin.");
            state.Fixture("wash_station").Placed = true;
            Assert.That(Tutorial.CurrentFor(state).Id, Is.EqualTo("wash"));
            Tutorial.RecordAction(state, "washed");
            Assert.That(state.TutorialDone("wash"), Is.True);
            Assert.That(Tutorial.CurrentFor(state).Id, Is.EqualTo("strike"));
        }

        [TestCase("appraise", "appraisal_station", UpgradeCatalog.AppraisalStation)]
        [TestCase("display", "display_cabinet", UpgradeCatalog.CollectionCabinet)]
        [TestCase("saw", "trim_saw", UpgradeCatalog.TrimSaw)]
        [TestCase("polish", "flat_lap", UpgradeCatalog.PolishLap)]
        public void EquipmentLessonsRequireOwnedAndInstalledFixture(string lesson, string fixture, string upgrade)
        {
            var state = Fresh();
            var step = Tutorial.Steps.Single(x => x.Id == lesson);
            state.SetFixture(fixture, Vector3.zero, 0f, true);
            Assert.That(step.Available(state), Is.False, "Saved coordinates alone do not grant ownership.");
            state.Upgrades.Add(upgrade);
            Assert.That(step.Available(state), Is.True);
            state.Fixture(fixture).Placed = false;
            Assert.That(step.Available(state), Is.False);
        }

        [Test]
        public void StarterGuidanceKeepsTheOpeningLoopAheadOfAccessoryShopping()
        {
            var state = Fresh();
            Assert.That(Progression.NextUnlockShort(state), Is.EqualTo("Order your first local crate"));
            state.Stats.CratesPurchased = 1;
            Assert.That(Progression.NextUnlockShort(state), Is.EqualTo("Open a rock at the cracking bench"));
            state.Stats.SpecimensOpened = 1;
            Assert.That(Progression.NextUnlock(state), Is.EqualTo("Sell your first opened specimen"));
            state.Stats.RetailSales = 1;
            Assert.That(Progression.NextUnlockShort(state), Does.Contain(UpgradeCatalog.Get(UpgradeCatalog.Loupe).Name));
            Assert.That(UpgradeCatalog.Get(UpgradeCatalog.SoftBrush).Requires, Is.EqualTo(UpgradeCatalog.WashStation));
            Assert.That(UpgradeCatalog.Get(UpgradeCatalog.CalibratedScale).Requires, Is.EqualTo(UpgradeCatalog.AppraisalStation));
            Assert.That(UpgradeCatalog.Get(UpgradeCatalog.TrimSaw).Requires, Is.EqualTo(UpgradeCatalog.BackRoom));
        }

        [Test]
        public void PendingPlacementUsesTheSuppliedCareerRatherThanLiveSessionOwnership()
        {
            var go = new GameObject("Guidance fixture witness");
            try
            {
                var fixture = go.AddComponent<PlaceableFixture>();
                fixture.Id = "guidance-test-fixture";
                fixture.RequiresUpgrade = UpgradeCatalog.WashStation;
                var state = Fresh();
                Assert.That(fixture.RequiresPlacementFor(state), Is.False);
                state.Upgrades.Add(UpgradeCatalog.WashStation);
                Assert.That(fixture.RequiresPlacementFor(state), Is.True);
                state.SetFixture(fixture.Id, Vector3.zero, 0f, true);
                Assert.That(fixture.RequiresPlacementFor(state), Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
