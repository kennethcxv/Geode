using System.Collections.Generic;
using GeodeEmpire.Save;
using UnityEngine;

namespace GeodeEmpire.Build
{
    /// <summary>Saved occupancy of the shared stock/equipment receiving cells, including deliveries still falling.</summary>
    public static class ReceivingManifest
    {
        public static bool Occupied(GameState state, Vector3 point, float radius)
        {
            if (state == null) return false;
            foreach (var crate in state.Crates)
                if (crate != null && crate.Delivered && Near(crate.Position, point, radius)) return true;
            foreach (var fixture in state.Fixtures)
                if (fixture != null && fixture.Delivered && !fixture.Placed && Near(fixture.DeliveryPosition, point, radius)) return true;
            return false;
        }

        private static bool Near(Vector3 a, Vector3 b, float radius)
        {
            a.y = b.y = 0f;
            return (a - b).sqrMagnitude < radius * radius;
        }

        /// <summary>Reserve before showing a parcel. A reload preserves its cell; overflow stays owned and pending.</summary>
        public static bool TryReceive(GameState state, string id, IEnumerable<Vector3> cells, float radius)
        {
            if (state == null || string.IsNullOrEmpty(id)) return false;
            var pose = state.Fixture(id);
            if (pose != null && (pose.Placed || pose.Delivered)) return true;
            foreach (var point in cells)
            {
                if (Occupied(state, point, radius)) continue;
                if (pose == null) pose = state.SetFixture(id, Vector3.zero, 0f, false);
                pose.DeliveryPosition = point;
                pose.Delivered = true;
                return true;
            }
            return false;
        }
    }
}
