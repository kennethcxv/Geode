using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.UI;

namespace GeodeEmpire.Build
{
    /// <summary>
    /// Bought equipment arrives in the receiving bay in a crate and stays there until the player sites it. One crate
    /// per pending fixture, labelled; opening it takes the player straight into build mode with that fixture in hand.
    /// This is the middle of §5.3's loop — unlock, buy, receive, place — and the reason a purchase is felt in the room
    /// rather than only in a number.
    /// </summary>
    public sealed class FixtureDelivery : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Slot
        {
            public GameObject Root;
            public WorldLabel Label;
            [System.NonSerialized] public PlaceableFixture Fixture;
        }

        public List<Slot> Slots = new List<Slot>();

        /// <summary>
        /// Crated equipment lands wherever goods-in is today: a corner of the unit before the back room is
        /// leased, the bay under the shutter afterwards. The crates move with it rather than being duplicated.
        /// </summary>
        public Transform KerbAnchor, BayAnchor;
        /// <summary>Local offsets from the live anchor, one per slot.</summary>
        public List<Vector3> SlotOffsets = new List<Vector3>();

        private void Start()
        {
            var s = GameSession.Instance;
            if (s != null) { s.Loaded += Refresh; s.StateChanged += Refresh; }
            Refresh();
        }

        private void OnDestroy()
        {
            var s = GameSession.Instance;
            if (s != null) { s.Loaded -= Refresh; s.StateChanged -= Refresh; }
        }

        public void Refresh()
        {
            var receiving = FindAnyObjectByType<Workshop.ReceivingArea>();
            bool shared = receiving != null && receiving.SharedDeliveries;
            if (shared) receiving.ReceiveEquipment(Slots.Count);
            else SeatCrates();
            var pending = new List<PlaceableFixture>();
            foreach (var f in PlaceableFixture.All)
                if (f != null && f.Owned && f.Movable && !f.SitedByDefault && !f.Pose.Placed && (!shared || f.Pose.Delivered)) pending.Add(f);
            pending.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            for (int i = 0; i < Slots.Count; i++)
            {
                var slot = Slots[i];
                if (slot?.Root == null) continue;
                slot.Fixture = i < pending.Count ? pending[i] : null;
                bool on = slot.Fixture != null;
                if (slot.Root.activeSelf != on) slot.Root.SetActive(on);
                if (on && shared) slot.Root.transform.SetPositionAndRotation(slot.Fixture.Pose.DeliveryPosition, Quaternion.identity);
                if (on && slot.Label != null) slot.Label.Text = slot.Fixture.DisplayName.ToUpperInvariant();
                var crate = slot.Root.GetComponent<DeliveryCrate>();
                if (crate != null) crate.Slot = slot;
            }
        }

        private void SeatCrates()
        {
            var anchor = Workshop.PremisesExpansion.BackRoomOpen && BayAnchor != null ? BayAnchor : KerbAnchor;
            if (anchor == null) return;
            for (int i = 0; i < Slots.Count && i < SlotOffsets.Count; i++)
            {
                var root = Slots[i]?.Root;
                if (root == null) continue;
                root.transform.position = anchor.TransformPoint(SlotOffsets[i]);
                root.transform.rotation = anchor.rotation * Quaternion.Euler(0f, i % 2 == 0 ? -6f : 8f, 0f);
            }
        }
    }
}
