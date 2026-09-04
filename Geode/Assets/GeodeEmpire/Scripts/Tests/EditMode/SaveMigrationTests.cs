using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Economy;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Tests
{
    /// <summary>
    /// V5 §72: a V4 save (version 1, none of the V5 fields) loads into the current version with every V5 field at a
    /// sensible default, the V4 pieces and cut lineage intact, and saves back at the current version.
    /// </summary>
    public class SaveMigrationTests
    {
        /// <summary>A V4-shaped file: today's serialiser with every V5-only key stripped, the way a V4 build wrote it.</summary>
        private static string V4Fixture()
        {
            var s = new GameState { SaveId = "v4-fixture", WorldSeed = 4242UL, Cash = 812f, CrateCounter = 9, SpecimenCounter = 3, WorkshopStage = 2, DisplayCapacity = 16 };
            s.Upgrades.Add(UpgradeCatalog.TrimSaw); s.Upgrades.Add(UpgradeCatalog.Stage2);
            s.UnlockedSuppliers.Add(SupplierCatalog.Local); s.UnlockedSuppliers.Add(SupplierCatalog.Regional);
            var parent = new SpecimenRecord { Id = "S0001-1111", Seed = 0x1111UL, SupplierId = "local", CrateId = "C001", Location = SpecimenLocation.Cut, CutCommitted = true };
            parent.Condition.Opened = true;
            var a = new SpecimenRecord { Id = "S0001-1111-A", Seed = 0x1111UL, SupplierId = "local", CrateId = "C001", Location = SpecimenLocation.DisplaySlot, LocationIndex = 2, IsPiece = true, ParentId = parent.Id, CutIndex = 0, Appraised = true, AppraisedValue = 140f, Polish = 0.95f };
            a.Condition.Opened = true;
            var b = new SpecimenRecord { Id = "S0001-1111-B", Seed = 0x1111UL, SupplierId = "local", CrateId = "C001", Location = SpecimenLocation.SaleSlot, LocationIndex = 1, IsPiece = true, ParentId = parent.Id, CutIndex = 1, Appraised = true, AppraisedValue = 60f, AskingPrice = 84f };
            b.Condition.Opened = true;
            var rough = new SpecimenRecord { Id = "S0003-2222", Seed = 0x2222UL, SupplierId = "regional", CrateId = "C002", Location = SpecimenLocation.World, WorldPosition = new Vector3(-1f, 0.1f, 0.5f) };
            s.Specimens.Add(parent); s.Specimens.Add(a); s.Specimens.Add(b); s.Specimens.Add(rough);
            s.Stats.CratesPurchased = 9; s.Stats.SpecimensSold = 40; s.Stats.SawCuts = 6;
            string json = JsonUtility.ToJson(s, false);
            // V5-only keys (records and state): a V4 file never wrote them
            foreach (var key in new[] { "Locality", "AcquiredAtTicks", "AcquisitionCost", "OriginalMassKg", "Favorite", "Certified", "Fluorescence", "Predicted", "PredictedHollow", "PredictedTier", "History", "CutThin", "CutFaceStep", "ConsignedAtCrate", "CustomName",
                                        "OfferedLots", "LastOfferCrate", "Commissions", "AuctionLots", "PendingLetters", "CommissionCounter", "LastCommissionMilestone", "ExhibitionInviteShown", "ExhibitionCompletedTicks", "ExhibitionsHeld", "ExhibitedIds" })
                json = StripKey(json, key);
            json = Regex.Replace(json, "\"Version\":\\d+", "\"Version\":1");
            return json;
        }

        /// <summary>Remove a top-level or nested key with a scalar, array or object value from compact JSON.</summary>
        private static string StripKey(string json, string key)
        {
            // scalar / string values
            json = Regex.Replace(json, "\"" + key + "\":(\"(?:[^\"\\\\]|\\\\.)*\"|-?[0-9.eE+-]+|true|false|null),?", "");
            // arrays and objects (no nesting of the same kind inside, which holds for these keys)
            json = Regex.Replace(json, "\"" + key + "\":\\[[^\\[\\]]*(?:\\[[^\\[\\]]*\\][^\\[\\]]*)*\\],?", "");
            json = Regex.Replace(json, "\"" + key + "\":\\{[^{}]*(?:\\{[^{}]*\\}[^{}]*)*\\},?", "");
            json = json.Replace(",}", "}").Replace(",]", "]");
            return json;
        }

        [Test]
        public void V4Save_LoadsWithV5Defaults_AndLineageIntact()
        {
            string json = V4Fixture();
            Assert.IsFalse(json.Contains("\"History\""), "the fixture must not carry V5 keys");
            Assert.IsFalse(json.Contains("\"AuctionLots\""), "the fixture must not carry V5 keys");
            var s = SaveSystem.Parse(json);
            Assert.IsNotNull(s, "a V4 file must parse");
            Assert.AreEqual(GameState.CurrentVersion, s.Version, "migrated to the current version");
            Assert.AreEqual("v4-fixture", s.SaveId);
            Assert.AreEqual(4, s.Specimens.Count);
            Assert.IsNotNull(s.AuctionLots); Assert.IsNotNull(s.PendingLetters); Assert.IsNotNull(s.OfferedLots); Assert.IsNotNull(s.Commissions); Assert.IsNotNull(s.ExhibitedIds);
            foreach (var r in s.Specimens)
            {
                Assert.IsNotNull(r.History, r.Id + " history list");
                Assert.IsNotNull(r.Condition, r.Id + " condition");
                Assert.AreEqual(-1, r.PredictedTier, r.Id + " no call on record");
                Assert.AreEqual(0, r.ConsignedAtCrate, r.Id + " not consigned");
                Assert.IsFalse(r.Favorite); Assert.IsFalse(r.Certified);
                Assert.IsNotNull(r.Geology, r.Id + " geology regenerates from the seed");
            }
            var parent = s.FindSpecimen("S0001-1111"); var a = s.FindSpecimen("S0001-1111-A"); var b = s.FindSpecimen("S0001-1111-B");
            Assert.AreEqual(SpecimenLocation.Cut, parent.Location, "the cut parent stays a lineage root");
            Assert.IsTrue(a.IsPiece && b.IsPiece && a.ParentId == parent.Id && b.ParentId == parent.Id, "pieces keep their lineage");
            Assert.AreEqual(SpecimenLocation.DisplaySlot, a.Location); Assert.AreEqual(2, a.LocationIndex);
            Assert.AreEqual(SpecimenLocation.SaleSlot, b.Location); Assert.AreEqual(84f, b.AskingPrice, 0.01f);
            Assert.AreEqual(0.95f, a.Polish, 0.001f, "polish survives");
            Assert.AreEqual(2, s.WorkshopStage); Assert.AreEqual(16, s.DisplayCapacity);
            Assert.IsTrue(s.HasUpgrade(UpgradeCatalog.TrimSaw) && s.HasUpgrade(UpgradeCatalog.Stage2));
            Assert.AreEqual(CrateGenerator.DefaultLocalities("regional")[0], s.FindSpecimen("S0003-2222").Locality, "a V4 rock gets its source's default locality");
            Assert.AreEqual("the local quarry", s.FindSpecimen("S0001-1111").Locality, "a quarry rock is from the quarry");
            Assert.IsTrue(a.DisplayName.Length > 0, "a piece still has a name");
            // and it saves back at the current version, round-tripping the V5 defaults
            string again = JsonUtility.ToJson(s);
            var s2 = SaveSystem.Parse(again);
            Assert.AreEqual(GameState.CurrentVersion, s2.Version);
            Assert.AreEqual(4, s2.Specimens.Count);
        }

        [Test]
        public void CurrentSave_RoundTripsEveryV5Field()
        {
            var s = new GameState { SaveId = "v5", WorldSeed = 7UL, Cash = 100f, CrateCounter = 4 };
            var r = new SpecimenRecord { Id = "S0001-AAAA", Seed = 0xAAAAUL, SupplierId = "regional", Locality = "Tabasco Mine", Favorite = true, Certified = true, Fluorescence = "glows green", Predicted = true, PredictedHollow = true, PredictedTier = 3, CustomName = "The Green Lantern", ConsignedAtCrate = 4, Location = SpecimenLocation.DisplaySlot };
            r.Condition.Opened = true; r.Condition.Rinsed = true;
            r.History.Add(new SpecimenEvent { Kind = "displayed", Value = 300f, Note = "display slot 1" });
            s.Specimens.Add(r);
            s.AuctionLots.Add(new AuctionLot { SpecimenId = r.Id, CollectedAtCrate = 4, ResolveAtCrate = 7, Estimate = 300f, Reserve = 255f });
            s.PendingLetters.Add(new LetterRecord { Title = "Sold at auction", Body = "..." });
            s.OfferedLots.Add(SupplierCatalog.Showcase);
            s.ExhibitedIds.Add(r.Id); s.ExhibitionsHeld = 1;
            var back = SaveSystem.Parse(JsonUtility.ToJson(s));
            var rb = back.FindSpecimen(r.Id);
            Assert.AreEqual("Tabasco Mine", rb.Locality); Assert.IsTrue(rb.Favorite && rb.Certified && rb.Predicted && rb.PredictedHollow); Assert.AreEqual(3, rb.PredictedTier);
            Assert.AreEqual("The Green Lantern", rb.CustomName); Assert.AreEqual("The Green Lantern", rb.DisplayName); Assert.AreEqual(4, rb.ConsignedAtCrate);
            Assert.IsTrue(rb.Condition.Rinsed); Assert.AreEqual(1, rb.History.Count); Assert.AreEqual("displayed", rb.History[0].Kind);
            Assert.AreEqual(1, back.AuctionLots.Count); Assert.AreEqual(7, back.AuctionLots[0].ResolveAtCrate);
            Assert.AreEqual(1, back.PendingLetters.Count); Assert.AreEqual(1, back.OfferedLots.Count); Assert.AreEqual(1, back.ExhibitedIds.Count); Assert.AreEqual(1, back.ExhibitionsHeld);
        }
    }
}
