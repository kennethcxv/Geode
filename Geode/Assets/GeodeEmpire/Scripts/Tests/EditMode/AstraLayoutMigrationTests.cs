using System.Linq;
using GeodeEmpire.Build;
using GeodeEmpire.Economy;
using GeodeEmpire.Save;
using NUnit.Framework;
using UnityEngine;

namespace GeodeEmpire.Tests
{
    public class AstraLayoutMigrationTests
    {
        [Test]
        public void FreshCareerHasOnlyEssentialCounterAndRemainsMinimalAfterReload()
        {
            var state = new GameState { SaveId = "astra-fresh", Cash = 120f };
            AstraLayoutMigration.InitializeNew(state);
            var loaded = SaveSystem.Parse(JsonUtility.ToJson(state));
            Assert.That(loaded.Upgrades, Is.EquivalentTo(new[] { UpgradeCatalog.CounterTable }));
            Assert.That(loaded.Fixtures, Is.Empty);
            Assert.That(loaded.DisplayCapacity, Is.Zero);
            Assert.That(loaded.LayoutRevision, Is.EqualTo(AstraWorkshop.Revision));
            Assert.That(loaded.LayoutRentCredit, Is.Zero);
            Assert.That(loaded.Cash, Is.EqualTo(120f));
            Assert.That(Ledger.LeasedAreaM2(loaded), Is.EqualTo(24f));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(3)]
        public void ReplacementRightsNeverRaiseRentOrAlterCashAndExistingBills(int stage)
        {
            var state = new GameState { SaveId = "legacy-rights", Cash = 319.95f, WorkshopStage = stage };
            if (stage >= 2) state.Upgrades.Add(UpgradeCatalog.Stage2);
            if (stage >= 3) state.Upgrades.Add(UpgradeCatalog.Stage3);
            state.Bills.Outstanding = 68.15f; state.Bills.NextBillDay = 7; state.Bills.DueDay = 5;
            state.Bills.WaterLitres = 13.8f; state.Bills.ElectricityUnits = 2.75f;
            state.Bills.LastLines.Add("Rent|48.00|original unit");
            string bills = JsonUtility.ToJson(state.Bills);
            float rent = Ledger.RentPerPeriod(state);
            AstraLayoutMigration.PreserveLegacyRights(state);
            Assert.That(Ledger.RentPerPeriod(state), Is.EqualTo(rent));
            Assert.That(state.Cash, Is.EqualTo(319.95f));
            Assert.That(JsonUtility.ToJson(state.Bills), Is.EqualTo(bills));
            Assert.That(state.HasUpgrade(UpgradeCatalog.WashStation), Is.True);
            Assert.That(state.HasUpgrade(UpgradeCatalog.AppraisalStation), Is.True);
            Assert.That(state.HasUpgrade(UpgradeCatalog.StorageShelf), Is.True);
            Assert.That(state.HasUpgrade(UpgradeCatalog.BackRoom), Is.True);
            Assert.That(state.HasUpgrade(UpgradeCatalog.ShopFront), Is.EqualTo(stage >= 2));
            string once = JsonUtility.ToJson(state);
            AstraLayoutMigration.PreserveLegacyRights(state);
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(once), "Interruption before revision commit must not grant credit twice.");
        }

        [Test]
        public void PreviouslyLeasedRoomsReceiveNoCreditOrLostOwnership()
        {
            var state = new GameState { SaveId = "paid-leases", WorkshopStage = 2 };
            state.Upgrades.AddRange(new[] { UpgradeCatalog.BackRoom, UpgradeCatalog.ShopFront, UpgradeCatalog.TrimSaw, UpgradeCatalog.Stage2 });
            AstraLayoutMigration.PreserveLegacyRights(state);
            Assert.That(state.LayoutRentCredit, Is.Zero);
            Assert.That(state.HasUpgrade(UpgradeCatalog.TrimSaw), Is.True);
            Assert.That(state.Upgrades.Distinct().Count(), Is.EqualTo(state.Upgrades.Count));
        }

        [Test]
        public void RecoveryPreservesEveryNonPhysicalFieldAndCollectionValueAcrossReload()
        {
            var state = new GameState { SaveId = "recovery-identity" };
            var specimen = new SpecimenRecord { Id = "S0093-ABCD", Seed = 893UL, CrateId = "ORIGINAL-LOT",
                SupplierId = "local", Locality = "Original locality", Location = SpecimenLocation.DisplaySlot,
                LocationIndex = 14, Favorite = true, Appraised = true, AppraisedValue = 127.95f,
                CustomName = "Keep this piece", IsPiece = true, ParentId = "S0001-ABCD", CutIndex = 3,
                CutCommitted = true, CutNormal = new Vector3(.3f, .6f, .7f), CutProgress = .37f, CutYaw = 27f,
                ShellDamage = .12f, DamageFraction = .07f, StrikeCount = 19,
                WorldPosition = new Vector3(6f, 1f, 1f), WorldRotation = Quaternion.Euler(15f, 28f, 5f) };
            specimen.Condition.Opened = true;
            specimen.Condition.RegionClean = new byte[] { 12, 89, 255 };
            specimen.History.Add(new SpecimenEvent { Kind = "opened", Ticks = 638000000000000000L, Note = "Original history" });
            state.Specimens.Add(specimen);
            string before = JsonUtility.ToJson(specimen);
            var originalPosition = specimen.WorldPosition;
            var parcel = AstraLayoutMigration.Pack(state, specimen);
            Assert.That(AstraLayoutMigration.Pack(state, specimen), Is.SameAs(parcel));
            Assert.That(state.Crates.Count, Is.EqualTo(1));
            Assert.That(state.CollectionValue(), Is.EqualTo(127.95f));
            Assert.That(state.DisplayedCount(), Is.EqualTo(1));
            var loaded = SaveSystem.Parse(JsonUtility.ToJson(state));
            var recovered = loaded.FindSpecimen(specimen.Id);
            Assert.That(recovered.IsInside(loaded.FindCrate(parcel.Id)), Is.True);
            Assert.That(loaded.FindCrate(parcel.Id).Delivered, Is.False);
            Assert.That(loaded.FindCrate(parcel.Id).SpecimenIds, Is.EquivalentTo(new[] { specimen.Id }));
            recovered.RecoveryCrateId = null; recovered.WorldPosition = originalPosition;
            Assert.That(JsonUtility.ToJson(recovered), Is.EqualTo(before), "No identity, lineage, processing, condition or history field may change.");
        }

        [Test]
        public void StockQueuePreservesDeliveredAndWaitingCratesAndTheirOriginalContents()
        {
            var state = new GameState { SaveId = "old-crates" };
            foreach (bool delivered in new[] { true, false })
            {
                string id = delivered ? "delivered" : "waiting";
                var crate = new CrateRecord { Id = id, Delivered = delivered, Opened = delivered, Seed = 817UL, PricePaid = 75f };
                crate.SpecimenIds.Add("S-" + id); state.Crates.Add(crate);
                state.Specimens.Add(new SpecimenRecord { Id = "S-" + id, CrateId = id, Seed = 1337UL,
                    Location = SpecimenLocation.InCrate, WorldPosition = Vector3.one });
            }
            AstraLayoutMigration.QueueOldCrates(state);
            Assert.That(state.Crates.Count, Is.EqualTo(2));
            Assert.That(state.Specimens.Count, Is.EqualTo(2));
            Assert.That(state.Crates.All(c => !c.Delivered && c.Position == Vector3.zero && c.PricePaid == 75f), Is.True);
            Assert.That(state.FindCrate("delivered").Opened, Is.True);
            Assert.That(state.Specimens.All(r => r.Seed == 1337UL && r.IsInside(state.FindCrate(r.CrateId))), Is.True);
        }

        [Test]
        public void RecoveringAgainDoesNotReuseAnEmptyParcelStillOnTheFloor()
        {
            var state = new GameState { SaveId = "recovery-repeat" };
            var specimen = new SpecimenRecord { Id = "S1", Location = SpecimenLocation.World, CrateId = "provenance" };
            state.Specimens.Add(specimen);
            var first = AstraLayoutMigration.Pack(state, specimen);
            specimen.RecoveryCrateId = null; specimen.Location = SpecimenLocation.Held;
            var second = AstraLayoutMigration.Pack(state, specimen);
            Assert.That(first.Id, Is.Not.EqualTo(second.Id));
            Assert.That(specimen.IsInside(first), Is.False);
            Assert.That(specimen.IsInside(second), Is.True);
            Assert.That(specimen.CrateId, Is.EqualTo("provenance"));
        }

        [TestCase(SpecimenLocation.Sold)]
        [TestCase(SpecimenLocation.Discarded)]
        [TestCase(SpecimenLocation.Cut)]
        [TestCase(SpecimenLocation.Consigned)]
        public void RecoveryNeverDuplicatesStockThatHasLeftThePhysicalInventory(SpecimenLocation location)
        {
            var state = new GameState();
            Assert.Throws<System.ArgumentException>(() => AstraLayoutMigration.Pack(state, new SpecimenRecord { Id = "gone", Location = location }));
            Assert.That(state.Crates, Is.Empty);
        }
    }
}
