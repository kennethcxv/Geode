using GeodeEmpire.Save;
using NUnit.Framework;
using UnityEngine;

namespace GeodeEmpire.Tests
{
    public class ShopHoursTests
    {
        [Test]
        public void OldCareerResumesClosedAndRetainsStockFixturesCashAndBills()
        {
            const string json = "{\"Version\":3,\"SaveId\":\"old-shop\",\"Cash\":149.25,"
                + "\"Specimens\":[{\"Id\":\"owned-quartz\"}],"
                + "\"Fixtures\":[{\"Id\":\"trade_counter\",\"Placed\":true,\"Yaw\":90}],"
                + "\"Bills\":{\"Outstanding\":87.5,\"DueDay\":12}}";
            var state = SaveSystem.Parse(json);
            Assert.That(state, Is.Not.Null);
            Assert.That(state.Version, Is.EqualTo(GameState.CurrentVersion));
            Assert.That(state.ShopOpen, Is.False);
            Assert.That(state.Cash, Is.EqualTo(149.25f));
            Assert.That(state.FindSpecimen("owned-quartz"), Is.Not.Null);
            Assert.That(state.Fixture("trade_counter").Placed, Is.True);
            Assert.That(state.Fixture("trade_counter").Yaw, Is.EqualTo(90f));
            Assert.That(state.Bills.Outstanding, Is.EqualTo(87.5f));
            Assert.That(state.Bills.DueDay, Is.EqualTo(12));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CurrentCareerRetainsTheOwnersOpeningChoice(bool open)
        {
            var state = new GameState { SaveId = "hours-roundtrip", ShopOpen = open };
            var restored = SaveSystem.Parse(JsonUtility.ToJson(state));
            Assert.That(restored.ShopOpen, Is.EqualTo(open));
        }
    }
}
