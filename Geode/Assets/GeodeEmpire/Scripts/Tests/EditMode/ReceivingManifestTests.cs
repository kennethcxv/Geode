using GeodeEmpire.Build;
using GeodeEmpire.Save;
using NUnit.Framework;
using UnityEngine;

namespace GeodeEmpire.Tests
{
    public class ReceivingManifestTests
    {
        private static readonly Vector3[] Cells = { new Vector3(-1.2f, .12f, -2.05f), new Vector3(-2.55f, .12f, -2.05f) };

        [Test]
        public void FallingStockReservesItsDestinationBeforeEquipmentArrives()
        {
            var state = new GameState { SaveId = "shared-bay" };
            state.Crates.Add(new CrateRecord { Id = "stock", Delivered = true, Position = Cells[0] });
            Assert.That(ReceivingManifest.TryReceive(state, "saw", Cells, .62f), Is.True);
            Assert.That(state.Fixture("saw").DeliveryPosition, Is.EqualTo(Cells[1]));
            Assert.That(ReceivingManifest.TryReceive(state, "lap", Cells, .62f), Is.False);
            Assert.That(state.Fixture("lap"), Is.Null);
            Assert.That(state.FindCrate("stock"), Is.Not.Null);
        }

        [Test]
        public void PlacingEquipmentReleasesOnlyItsCellAndDoesNotMoveAnotherParcel()
        {
            var state = new GameState { SaveId = "release-bay" };
            Assert.That(ReceivingManifest.TryReceive(state, "saw", Cells, .62f), Is.True);
            Assert.That(ReceivingManifest.TryReceive(state, "lap", Cells, .62f), Is.True);
            var lapPosition = state.Fixture("lap").DeliveryPosition;
            state.SetFixture("saw", new Vector3(0f, 0f, 5f), 90f, true);
            Assert.That(ReceivingManifest.TryReceive(state, "washer", Cells, .62f), Is.True);
            Assert.That(state.Fixture("washer").DeliveryPosition, Is.EqualTo(Cells[0]));
            Assert.That(state.Fixture("lap").DeliveryPosition, Is.EqualTo(lapPosition));
            Assert.That(state.Fixture("saw").Position, Is.EqualTo(new Vector3(0f, 0f, 5f)));
        }

        [Test]
        public void ReloadRetainsParcelReservationAndOverflowOwnershipWithoutDuplicates()
        {
            var state = new GameState { SaveId = "resume-bay", Cash = 97.25f };
            state.Upgrades.AddRange(new[] { "saw", "lap", "washer" });
            ReceivingManifest.TryReceive(state, "saw", Cells, .62f);
            ReceivingManifest.TryReceive(state, "lap", Cells, .62f);
            var restored = SaveSystem.Parse(JsonUtility.ToJson(state));
            Assert.That(ReceivingManifest.TryReceive(restored, "lap", Cells, .62f), Is.True);
            Assert.That(ReceivingManifest.TryReceive(restored, "washer", Cells, .62f), Is.False);
            Assert.That(restored.Fixtures.Count, Is.EqualTo(2));
            Assert.That(restored.HasUpgrade("washer"), Is.True);
            Assert.That(restored.Cash, Is.EqualTo(97.25f));
            Assert.That(restored.Fixture("lap").DeliveryPosition, Is.EqualTo(Cells[1]));
        }

        [Test]
        public void LegacyPendingFixtureIsReceivedWithoutChangingItsSavedPlacementCandidate()
        {
            var state = SaveSystem.Parse("{\"Version\":4,\"SaveId\":\"legacy\",\"Fixtures\":[{\"Id\":\"saw\",\"Position\":{\"x\":1,\"y\":0,\"z\":2},\"Yaw\":270,\"Placed\":false}]}");
            Assert.That(ReceivingManifest.TryReceive(state, "saw", Cells, .62f), Is.True);
            Assert.That(state.Fixture("saw").Position, Is.EqualTo(new Vector3(1f, 0f, 2f)));
            Assert.That(state.Fixture("saw").Yaw, Is.EqualTo(270f));
            Assert.That(state.Fixture("saw").Placed, Is.False);
        }
    }
}
