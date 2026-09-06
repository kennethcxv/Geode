using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Save;

namespace GeodeEmpire.Workshop
{
    /// <summary>
    /// Where purchased crates land. Goods-in has a fixed number of pallet spaces and a crate goes in one of them
    /// or the order is refused; nothing is ever put down on top of something already standing there.
    ///
    /// §14 exists because the previous version could not say no: it tried three stack heights over a single kerb
    /// cell, rejected all of them by its own occupancy test, then fell through to an unconditional
    /// <c>return</c> of the ground cell — so three crates bought on day one landed on one transform, gap 0.000 m.
    /// </summary>
    public sealed class ReceivingArea : MonoBehaviour
    {
        public Vector2 Footprint = new Vector2(1.1f, 0.7f);

        /// <summary>
        /// Where deliveries land today. Before the back room is leased there is no bay, only the corner of the
        /// unit; signing the lease moves goods-in to the pallets under the shutter. The area moves rather than
        /// being duplicated, so a crate already on the floor stays where the player left it.
        /// </summary>
        public Transform KerbAnchor, BayAnchor;

        /// <summary>Astra's measured receiving marks are shared by stock and equipment. Legacy scenes retain their old layout.</summary>
        public bool SharedDeliveries;
        public Vector3[] StarterCells = System.Array.Empty<Vector3>();
        public Vector3[] BayCells = System.Array.Empty<Vector3>();

        /// <summary>A crate and its lid need this much floor; two crates closer than this are touching.</summary>
        public const float SlotRadius = 0.62f;

        private Transform ActiveAnchor => PremisesExpansion.BackRoomOpen && BayAnchor != null ? BayAnchor : KerbAnchor;

        /// <summary>
        /// Two spaces at the kerb: enough to have one open and one waiting, and no more. The second used to sit
        /// 1.32 m due west of the first, which put it 162 mm inside the workbench — the collision audit caught it
        /// the first time two crates were ordered on one save, and a first move at -0.95 caught a stool. It sits
        /// south-west of the pallets now, with clearance to the bench behind it and to the trade counter in front
        /// (which is switched off on a fresh save, so a clearance cast alone does not see it).
        /// </summary>
        private static readonly Vector3[] KerbCells =
        {
            new Vector3(0f, 0.12f, 0f), new Vector3(-1.35f, 0.12f, -1.05f),
        };

        /// <summary>Four pallet cells in the bay, front row first, each 1.2 x 0.8 m so a crate and its lid fit.</summary>
        private static readonly Vector3[] Cells =
        {
            new Vector3(-0.6f, 0.12f, 0.4f), new Vector3(0.6f, 0.12f, 0.4f),
            new Vector3(-0.6f, 0.12f, -0.4f), new Vector3(0.6f, 0.12f, -0.4f),
        };

        /// <summary>Stage 3: a third pallet in the row north of the first four.</summary>
        private static readonly Vector3[] Stage3Cells = { new Vector3(0f, 0.12f, 1.23f) };

        private IEnumerable<Vector3> ActiveCells()
        {
            if (!PremisesExpansion.BackRoomOpen)
            {
                foreach (var c in StarterCells.Length > 0 ? StarterCells : KerbCells) yield return c;
                yield break;
            }
            foreach (var c in BayCells.Length > 0 ? BayCells : Cells) yield return c;
            if (BayCells.Length > 0) yield break;
            if (WorkshopExpansion.Stage3Active) foreach (var c in Stage3Cells) yield return c;
        }

        /// <summary>Delivery points are read off whichever anchor is live, not off this transform.</summary>
        private Vector3 Point(Vector3 cell)
        {
            var a = ActiveAnchor;
            return a != null ? a.TransformPoint(cell) : transform.TransformPoint(cell);
        }

        /// <summary>How many crates goods-in can hold at once, here and now.</summary>
        public int Capacity
        {
            get { int n = 0; foreach (var _ in ActiveCells()) n++; return n; }
        }

        /// <summary>Spaces with nothing standing in them.</summary>
        public int FreeSlots
        {
            get
            {
                int n = 0;
                foreach (var cell in ActiveCells()) if (!Occupied(Point(cell))) n++;
                return n;
            }
        }

        public bool HasSpace => FreeSlots > 0;

        /// <summary>Why an order cannot be taken right now, or null when it can.</summary>
        public string RefusalReason()
        {
            if (HasSpace) return null;
            return PremisesExpansion.BackRoomOpen
                ? $"Goods-in is full ({Capacity} pallets). Open or break down a crate first."
                : $"There is only room for {Capacity} crates in the corner. Open one, or lease the back room for a proper goods-in bay.";
        }

        /// <summary>Is a crate (or a crate on its way down) already standing here?</summary>
        private bool Occupied(Vector3 spot)
        {
            var session = GameSession.Instance;
            if (session == null) return false;
            if (SharedDeliveries) return Build.ReceivingManifest.Occupied(session.State, spot, SlotRadius);
            foreach (var c in session.Crates.Values)
            {
                if (c == null) continue;
                var d = c.transform.position - spot;
                d.y = 0f;
                if (d.sqrMagnitude < SlotRadius * SlotRadius) return true;
            }
            return false;
        }

        /// <summary>Only physical fixtures need receiving space; tool fittings and leases do not.</summary>
        public string EquipmentRefusal(string upgradeId)
        {
            if (!SharedDeliveries) return null;
            int parcels = 0, waiting = 0;
            foreach (var fixture in Build.PlaceableFixture.All)
            {
                if (fixture == null || !fixture.Movable || fixture.SitedByDefault) continue;
                if (fixture.RequiresUpgrade == upgradeId) parcels++;
                if (fixture.Owned && !fixture.Pose.Placed) waiting++;
            }
            if (parcels == 0) return null;
            var delivery = FindAnyObjectByType<Build.FixtureDelivery>();
            int availableParcels = delivery != null ? Mathf.Max(0, delivery.Slots.Count - waiting) : 0;
            return parcels <= FreeSlots && parcels <= availableParcels ? null
                : $"This delivery needs {parcels} receiving space(s). Unpack and place equipment or break down an empty crate first.";
        }

        /// <summary>Receive older pending ownership without losing overflow or overlapping a stock crate.</summary>
        public void ReceiveEquipment(int visibleParcelCapacity)
        {
            if (!SharedDeliveries) return;
            var session = GameSession.Instance;
            if (session?.State == null) return;
            var pending = new List<Build.PlaceableFixture>();
            foreach (var fixture in Build.PlaceableFixture.All)
                if (fixture != null && fixture.Owned && fixture.Movable && !fixture.SitedByDefault && !fixture.Pose.Placed)
                    pending.Add(fixture);
            // Purchase order, then stable fixture ID for a multiple-parcel upgrade. Never depend on Unity find order.
            pending.Sort((a, b) => {
                int c = session.State.Upgrades.IndexOf(a.RequiresUpgrade).CompareTo(session.State.Upgrades.IndexOf(b.RequiresUpgrade));
                return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id);
            });
            int received = 0;
            foreach (var fixture in pending) if (fixture.Pose.Delivered) received++;
            foreach (var fixture in pending)
            {
                if (fixture.Pose.Delivered || received >= visibleParcelCapacity) continue;
                if (!Build.ReceivingManifest.TryReceive(session.State, fixture.Id, Slots(), SlotRadius)) continue;
                received++;
                session.QueueSave("equipment-received");
            }
        }

        /// <summary>Restore already-owned stock/recovery parcels as finite spaces become available; never buys or rerolls stock.</summary>
        public void ReceiveWaitingCrates()
        {
            var session = GameSession.Instance;
            if (!SharedDeliveries || session == null || !session.IsLoaded || session.State == null) return;
            foreach (var crate in session.State.Crates)
            {
                if (crate.Delivered) continue;
                var point = FreeSpot();
                if (point == null) break;
                crate.Position = point.Value;
                crate.Rotation = Quaternion.identity;
                crate.Delivered = true;
                session.RestoreDeliveredCrate(crate);
                session.QueueSave("waiting-stock-received");
            }
        }

        /// <summary>The first free space, or null when goods-in is full. Never guesses.</summary>
        public Vector3? FreeSpot()
        {
            foreach (var cell in ActiveCells())
            {
                var spot = Point(cell);
                if (!Occupied(spot)) return spot;
            }
            return null;
        }

        /// <summary>Every space, free or not — for the audit and for drawing the bay out.</summary>
        public IEnumerable<Vector3> Slots()
        {
            foreach (var cell in ActiveCells()) yield return Point(cell);
        }

        /// <summary>
        /// Land a crate in a free space. Returns false rather than putting it on top of another one; callers are
        /// expected to have checked <see cref="HasSpace"/> before taking the player's money.
        /// </summary>
        public bool Deliver(CrateRecord crate)
        {
            var session = GameSession.Instance;
            var spot = FreeSpot();
            if (spot == null)
            {
                Debug.LogWarning("[ReceivingArea] delivery refused: no free pallet space");
                return false;
            }
            crate.Position = spot.Value;
            crate.Rotation = (ActiveAnchor != null ? ActiveAnchor.rotation : transform.rotation) * Quaternion.Euler(0f, Random.Range(-8f, 8f), 0f);
            crate.Delivered = true;
            var ce = CrateEntity.Create(crate, session);
            ce.transform.SetPositionAndRotation(spot.Value + Vector3.up * 1.3f, crate.Rotation);
            StartCoroutine(Drop(ce.transform, spot.Value, crate.Rotation));
            return true;
        }

        private IEnumerator Drop(Transform t, Vector3 target, Quaternion rot)
        {
            float v = 0f, y = t.position.y;
            while (y > target.y)
            {
                v += 9.81f * Time.deltaTime * 1.4f;
                y -= v * Time.deltaTime;
                if (t == null) yield break;
                t.position = new Vector3(target.x, Mathf.Max(y, target.y), target.z);
                yield return null;
            }
            if (t == null) yield break;
            t.SetPositionAndRotation(target, rot);
            WorkshopAudio.Play("thud", target, 1f);
            VFX.EffectsFactory.Instance?.Impact(target + Vector3.up * 0.02f, Vector3.up, 0.9f);
            GameSession.Instance.QueueSave("delivered");
        }
    }
}
