using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;

namespace GeodeEmpire.Build
{
    /// <summary>
    /// Puts every placeable fixture where the save says it stands, and keeps a bought-but-unsited machine out of
    /// the room until the player has chosen a spot for it. Nothing is instantiated: the fixtures are scene objects
    /// whose body is hidden until they are both owned and placed.
    /// </summary>
    public sealed class FixtureWorld : MonoBehaviour
    {
        public static FixtureWorld Instance { get; private set; }

        private readonly HashSet<string> _announced = new HashSet<string>();

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            // includes fixtures inside an expansion root that has not been switched on yet
            PlaceableFixture.Rescan();
            var s = GameSession.Instance;
            if (s != null) { s.Loaded += Apply; s.StateChanged += Apply; }
            Apply();
        }

        private void OnDisable()
        {
            var s = GameSession.Instance;
            if (s != null) { s.Loaded -= Apply; s.StateChanged -= Apply; }
        }

        public void Apply()
        {
            var st = GameSession.Instance != null ? GameSession.Instance.State : null;
            if (st == null) return;
            foreach (var f in PlaceableFixture.All)
            {
                if (f == null) continue;
                var pose = st.Fixture(f.Id);
                if (pose != null && pose.Placed) f.ApplyPose(pose);
                else f.transform.SetPositionAndRotation(f.DefaultPosition, Quaternion.Euler(0f, f.DefaultYaw, 0f));
                if (f.Body != null)
                {
                    // a machine exists in the room only once it is owned and sited; before that the floor is clear
                    bool show = f.Owned && (pose == null ? !f.Movable || f.SitedByDefault : pose.Placed);
                    if (f.Body.activeSelf != show) f.Body.SetActive(show);
                }
                // a purchase the player has not sited yet is waiting in a crate: say so once
                bool pending = f.Owned && f.Movable && !f.SitedByDefault && (pose == null || !pose.Placed);
                if (pending && _announced.Add(f.Id))
                    GameSession.Instance?.Notify($"{f.DisplayName} is waiting for placement. Unpack its parcel in goods-in; clear a receiving space if the delivery is waiting.", NotificationKind.Info);
                else if (!pending) _announced.Remove(f.Id);
            }
            PlacementValidator.InvalidateMask();
        }
    }
}
