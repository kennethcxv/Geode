using System;
using System.Collections.Generic;
using System.Linq;
using GeodeEmpire.Core;
using GeodeEmpire.Interaction;
using GeodeEmpire.Save;
using GeodeEmpire.Workshop;
using UnityEngine;

namespace GeodeEmpire.Build
{
    /// <summary>Applies room/fixture state before stock reconstruction, and recovers stock displaced by a lease change.</summary>
    public static class AstraWorldLayout
    {
        private static readonly HashSet<string> LegacyInstalled = new HashSet<string>
        { "wash_station", "appraisal_station", "storage_shelf", "rock_rack", "main_display_case",
            "display_wall_c", "gallery_plinths", "specialist_display_case" };

        public static void ApplyVisibility()
        {
            UnityEngine.Object.FindAnyObjectByType<PremisesExpansion>()?.Apply();
            UnityEngine.Object.FindAnyObjectByType<WorkshopExpansion>()?.Refresh();
            UnityEngine.Object.FindAnyObjectByType<FixtureWorld>()?.Apply();
            // Gates that are attachments must be applied after the containing purchased body.
            UnityEngine.Object.FindAnyObjectByType<PremisesExpansion>()?.Apply();
            // Saved station occupants must see this career's availability before reconstruction, even when
            // Start/Loaded have not run yet or a previous career left the runtime zone locked.
            foreach (var fixture in PlaceableFixture.All)
            {
                if (fixture == null) continue;
                if (fixture.TryGetComponent<Lapidary.SawStation>(out var saw)) saw.RefreshOwned();
                if (fixture.TryGetComponent<Lapidary.PolishStation>(out var lap)) lap.RefreshOwned();
                if (fixture.TryGetComponent<Cracking.CrackerStation>(out var cracker)) cracker.RefreshOwned();
            }
            UnityEngine.Object.FindAnyObjectByType<DisplayCabinet>(FindObjectsInactive.Include)?.RefreshCapacity();
            UnityEngine.Object.FindAnyObjectByType<Retail.RetailShop>()?.RefreshCapacity();
            Physics.SyncTransforms();
            PlacementValidator.InvalidateMask();
        }

        public static void MigrateLegacy(GameSession session)
        {
            var state = session.State;
            if (state.LayoutRevision >= AstraWorkshop.Revision) return;
            AstraLayoutMigration.PreserveLegacyRights(state);
            PlaceableFixture.Rescan();
            var fixtures = PlaceableFixture.All.OrderByDescending(f => HasOccupant(f, state)).ThenBy(f => f.Id, StringComparer.Ordinal).ToArray();
            var originals = new Dictionary<string, FixturePose>();
            foreach (var f in fixtures)
            {
                var old = state.Fixture(f.Id);
                if (old != null) originals[f.Id] = new FixturePose { Id = old.Id, Position = old.Position, Yaw = old.Yaw, Placed = old.Placed };
                if (HasOccupant(f, state))
                {
                    AstraLayoutMigration.Grant(state, f.RequiresUpgrade);
                    if (f.AllowedRooms.Contains(Room.Showroom)) AstraLayoutMigration.GrantRoom(state, Economy.UpgradeCatalog.ShopFront);
                }
                var pending = state.SetFixture(f.Id, f.DefaultPosition, f.DefaultYaw, false);
                pending.Delivered = false;
            }
            ApplyVisibility();
            int relocated = 0, waiting = 0;
            foreach (var f in fixtures)
            {
                if (!f.Owned || f.SitedByDefault || !f.Movable) continue;
                originals.TryGetValue(f.Id, out var old);
                bool installed = (old != null && old.Placed) || LegacyInstalled.Contains(f.Id) || HasOccupant(f, state);
                if (!installed) continue;
                bool placed = old != null && old.Placed && TryPlace(f, old.Position, old.Yaw, state);
                if (!placed) placed = TryPlace(f, f.DefaultPosition, f.DefaultYaw, state);
                if (!placed) placed = FindSafePosition(f, state);
                if (placed) relocated++; else waiting++;
            }
            AstraLayoutMigration.QueueOldCrates(state);
            int packed = 0;
            foreach (var r in state.Specimens)
                if (!r.InRecovery && (r.Location == SpecimenLocation.World || r.Location == SpecimenLocation.Held
                    || (r.Location == SpecimenLocation.InCrate && state.FindCrate(r.CrateId) == null)))
                { AstraLayoutMigration.Pack(state, r); packed++; }
            state.LayoutRevision = AstraWorkshop.Revision;
            state.PendingLetters.Add(new LetterRecord { Title = "Your workshop has moved",
                Body = "Your equipment, collection and stock are still yours. Replacement room access carries no extra rent. "
                    + relocated + " fixtures have safe positions; " + waiting + " wait for placement. " + packed
                    + " loose pieces were packed individually. Recovery parcels arrive in goods-in as space clears. "
                    + "Take each real piece from its parcel to put it back to work; its provenance and processing progress are unchanged." });
            ApplyVisibility();
        }

        private static bool HasOccupant(PlaceableFixture fixture, GameState state)
        {
            foreach (var zone in fixture.GetComponentsInChildren<PlacementZone>(true))
            {
                var location = zone.LocationFor();
                if (location == SpecimenLocation.World) continue;
                foreach (var r in state.Specimens)
                    if (!r.InRecovery && r.Location == location && (!zone.IsIndexedSlot || r.LocationIndex == zone.SlotIndex)) return true;
            }
            return false;
        }

        private static bool TryPlace(PlaceableFixture fixture, Vector3 point, float yaw, GameState state)
        {
            fixture.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, yaw, 0f));
            Physics.SyncTransforms(); PlacementValidator.InvalidateMask();
            if (!PlacementValidator.Check(fixture, point, yaw).Valid) return false;
            state.SetFixture(fixture.Id, point, yaw, true);
            if (fixture.Body != null) fixture.Body.SetActive(true);
            Physics.SyncTransforms(); PlacementValidator.InvalidateMask();
            return true;
        }

        // A bounded relocation search, only for a one-time incompatible save migration. Normal placement never guesses.
        private static bool FindSafePosition(PlaceableFixture fixture, GameState state)
        {
            int routeAttempts = 0;
            foreach (float yaw in new[] { fixture.DefaultYaw, fixture.DefaultYaw + 90f, fixture.DefaultYaw + 180f, fixture.DefaultYaw + 270f })
                for (float z = ShopPlan.ZMin + .5f; z < ShopPlan.ZMax; z += .5f)
                    for (float x = ShopPlan.XMin + .5f; x < ShopPlan.XMax; x += .5f)
                    {
                        var point = new Vector3(x, 0f, z);
                        if (!fixture.AllowedRooms.Contains(ShopPlan.RoomAt(point))) continue;
                        fixture.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, yaw, 0f));
                        Physics.SyncTransforms();
                        if (!PlacementValidator.Check(fixture, point, yaw, false).Valid) continue;
                        if (TryPlace(fixture, point, yaw, state)) return true;
                        if (++routeAttempts >= 24) return false;
                    }
            return false;
        }

        public static void Refresh(GameSession session)
        {
            if (!AstraWorkshop.Active || session.State == null) return;
            ApplyVisibility();
            int recovered = 0;
            foreach (var entity in session.Entities.Values.ToArray())
            {
                if (entity == null || entity.Zone == null || entity.Zone.gameObject.activeInHierarchy) continue;
                var record = entity.Record;
                session.Despawn(entity);
                AstraLayoutMigration.Pack(session.State, record);
                recovered++;
            }
            var receiving = UnityEngine.Object.FindAnyObjectByType<ReceivingArea>();
            receiving?.ReceiveWaitingCrates();
            if (recovered > 0)
            {
                session.Notify(recovered + " pieces from the retired fixture are packed for recovery in goods-in.");
                session.QueueSave("stock-recovery");
            }
        }
    }
}
