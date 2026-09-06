using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Interaction;
using GeodeEmpire.Retail;
using GeodeEmpire.Specimens;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.Build
{
    /// <summary>
    /// Everything §6 of the rebuild spec asks of a placement, measured against the real world rather than trusted.
    /// A layout is rejected if the fixture leaves the room, cuts into the shell, overlaps another fixture or its
    /// working space, stands in a doorway, buries an interaction point or a queue position, or breaks the route
    /// graph that connects the player's spawn to every station, the counter, receiving and the collection.
    ///
    /// The route test is a flood fill over the real floor with the player's own radius, not a NavMesh query: a
    /// bookshelf that leaves a NavMesh sliver but functionally jams the aisle has to fail, and it does.
    /// </summary>
    public static class PlacementValidator
    {
        public const float Cell = 0.25f;
        public const float PlayerRadius = 0.29f;
        /// <summary>Contact this shallow is a fixture standing against a wall, not cutting into it.</summary>
        public const float Tolerance = 0.012f;

        private static readonly Collider[] _hits = new Collider[64];
        private static readonly RaycastHit[] _rays = new RaycastHit[16];

        public struct Result
        {
            public bool Valid;
            public string Reason;
            public static Result Ok => new Result { Valid = true, Reason = "" };
            public static Result No(string why) => new Result { Valid = false, Reason = why };
        }

        // ---------------------------------------------------------------------------------------------
        public static Result Check(PlaceableFixture f, Vector3 pos, float yaw, bool routeCheck = true)
        {
            if (f == null) return Result.No("nothing to place");
            f.BodyBox(pos, yaw, out var centre, out var half, out var rot);

            // 1. the right room, and inside the building
            var room = ShopPlan.RoomAt(pos);
            if (room == Room.None) return Result.No("outside the building");
            if (!ShopPlan.Leased(room)) return Result.No(room == Room.Showroom ? "the shop front is not leased yet" : "the back room is not leased yet");
            bool allowed = false;
            foreach (var r in f.AllowedRooms) if (r == room) { allowed = true; break; }
            if (!allowed) return Result.No(room == Room.Showroom ? "not in the showroom" : room == Room.BackOfHouse ? "not in the back of house" : "not in the workshop");
            foreach (var c in Corners(centre, half, rot))
            {
                var cr = ShopPlan.RoomAt(c);
                if (cr == Room.None) return Result.No("it would go through the wall");
                if (cr != room) return Result.No("it crosses into the next room");
                if (!ShopPlan.Open(c)) return Result.No("it would go through the hoarding");
            }

            // 2. the floor really is under all of it, and the ceiling is above it
            foreach (var c in Corners(centre, half, rot))
                if (!FloorUnder(c, f)) return Result.No("no floor under it");
            if (pos.y + f.Height > ShopPlan.Height - 0.05f) return Result.No("it would go through the ceiling");

            // 3. doorways, openings and the counter's serving space stay clear
            foreach (var p in ShopPlan.Portals)
                if (BoxTouchesAabb(centre, half, rot, p)) return Result.No("it blocks a doorway");
            var receiving = Object.FindAnyObjectByType<ReceivingArea>();
            if (receiving != null && receiving.SharedDeliveries)
                foreach (var point in receiving.Slots())
                    if (BoxTouchesAabb(centre, half, rot, new Bounds(point, new Vector3(1.2f, 2f, .8f))))
                        return Result.No("keep the marked receiving spaces clear");

            // 4. nothing solid where the body goes: walls, shell, other fixtures, machines
            int n = Physics.OverlapBoxNonAlloc(centre, half - Vector3.one * Tolerance, _hits, rot, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = _hits[i];
                if (c == null || Ignorable(c, f)) continue;
                return Result.No(IsShell(c.transform) ? "it cuts into the wall" : "it overlaps the " + FixtureName(c));
            }

            // 5. its own working space, and everyone else's
            if (f.Clearance > 0.01f)
            {
                f.ClearanceBox(pos, yaw, out var cc, out var ch, out var cr);
                foreach (var p in ShopPlan.Portals)
                    if (BoxTouchesAabb(cc, ch, cr, p)) return Result.No("no room to stand at it without blocking a doorway");
                int m = Physics.OverlapBoxNonAlloc(cc, ch - Vector3.one * 0.05f, _hits, cr, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < m; i++)
                {
                    var c = _hits[i];
                    if (c == null || Ignorable(c, f) || IsFloor(c.transform)) continue;
                    return Result.No("no room to stand and work at it");
                }
            }
            foreach (var other in PlaceableFixture.All)
            {
                if (other == null || other == f || !other.isActiveAndEnabled || !other.Sited
                    || (other.Body != null && !other.Body.activeInHierarchy) || other.Clearance <= 0.01f) continue;
                var op = other.Pose;
                other.ClearanceBox(op.Position, op.Yaw, out var oc, out var oh, out var orot);
                if (BoxesOverlap(centre, half, rot, oc, oh, orot)) return Result.No("it blocks the working space at the " + other.DisplayName.ToLowerInvariant());
            }

            // 6. interaction points, queue positions and browsing spots must stay reachable
            // a fixture never buries its own anchors: those move with it
            foreach (var a in Anchors())
                if (!Owns(f, a.t) && PointInBox(a.p, centre, half, rot, 0.05f)) return Result.No("it buries something the player has to reach");
            foreach (var a in CustomerPoints())
                if (!Owns(f, a.t) && PointInBox(a.p, centre, half, rot, 0.2f)) return Result.No("it stands where a customer has to stand");

            // 7. everything must still be walkable from everything else
            if (routeCheck && !RouteHolds(f, centre, half, rot, out string blocked)) return Result.No(blocked);
            return Result.Ok;
        }

        /// <summary>Floor under a corner: anything the fixture itself owns, and anything above ankle height, is not floor.</summary>
        private static bool FloorUnder(Vector3 c, PlaceableFixture f)
        {
            int n = Physics.RaycastNonAlloc(new Ray(new Vector3(c.x, 1.9f, c.z), Vector3.down), _rays, 3.5f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (_rays[i].point.y > 0.06f) continue;
                if (f != null && _rays[i].collider.transform.IsChildOf(f.transform)) continue;
                return true;
            }
            return false;
        }

        // ---------------------------------------------------------------------------------------------
        private static bool Ignorable(Collider c, PlaceableFixture f)
        {
            if (c.isTrigger || !c.enabled) return false || c.isTrigger;
            if (f != null && c.transform.IsChildOf(f.transform)) return true;
            if (c.GetComponentInParent<SpecimenEntity>() != null) return true;
            if (c.GetComponentInParent<CrateEntity>() != null) return true;
            if (c.GetComponentInParent<CharacterController>() != null) return true;
            if (c.GetComponentInParent<Customer>() != null) return true;
            return IsFloor(c.transform);
        }

        private static bool Owns(PlaceableFixture f, Transform t) => f != null && t != null && t.IsChildOf(f.transform);

        private static bool IsFloor(Transform t)
        {
            string n = t.name;
            return n == "Floor" || n == "FloorShop" || n == "ShopFloor" || n == "BayApron" || n == "Porch" || n.EndsWith("Mat");
        }

        public static bool IsShell(Transform t)
        {
            string n = t.name;
            return n.StartsWith("Wall") || n.StartsWith("Partition") || n.StartsWith("Back") || n.StartsWith("Wainscot")
                || n.StartsWith("Skirt") || n == "Ceiling" || n.StartsWith("Beam") || n == "DoorLintel";
        }

        private static string FixtureName(Collider c)
        {
            var pf = c.GetComponentInParent<PlaceableFixture>();
            if (pf != null) return pf.DisplayName.ToLowerInvariant();
            var t = c.transform;
            while (t.parent != null && t.parent.name != "Stations" && t.parent.name != "Environment") t = t.parent;
            return t.name.ToLowerInvariant();
        }

        private static IEnumerable<Vector3> Corners(Vector3 centre, Vector3 half, Quaternion rot)
        {
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    yield return centre + rot * new Vector3(sx * (half.x - 0.02f), 0f, sz * (half.z - 0.02f));
        }

        private static bool PointInBox(Vector3 p, Vector3 centre, Vector3 half, Quaternion rot, float margin)
        {
            var l = Quaternion.Inverse(rot) * (p - centre);
            return Mathf.Abs(l.x) < half.x + margin && Mathf.Abs(l.z) < half.z + margin;
        }

        /// <summary>Separating-axis test in the plane: both boxes are upright, so only x/z matter.</summary>
        private static bool BoxesOverlap(Vector3 c0, Vector3 h0, Quaternion r0, Vector3 c1, Vector3 h1, Quaternion r1)
        {
            Vector3[] axes = { r0 * Vector3.right, r0 * Vector3.forward, r1 * Vector3.right, r1 * Vector3.forward };
            var d = c1 - c0; d.y = 0f;
            foreach (var a in axes)
            {
                float p0 = h0.x * Mathf.Abs(Vector3.Dot(a, r0 * Vector3.right)) + h0.z * Mathf.Abs(Vector3.Dot(a, r0 * Vector3.forward));
                float p1 = h1.x * Mathf.Abs(Vector3.Dot(a, r1 * Vector3.right)) + h1.z * Mathf.Abs(Vector3.Dot(a, r1 * Vector3.forward));
                if (Mathf.Abs(Vector3.Dot(d, a)) > p0 + p1 - 0.02f) return false;
            }
            return true;
        }

        private static bool BoxTouchesAabb(Vector3 centre, Vector3 half, Quaternion rot, Bounds b)
            => BoxesOverlap(centre, half, rot, b.center, b.extents, Quaternion.identity);

        // ---------------------------------------------------------------------------------------------
        private static readonly List<(Vector3 p, Transform t)> _anchors = new List<(Vector3, Transform)>();
        private static readonly List<(Vector3 p, Transform t)> _customer = new List<(Vector3, Transform)>();
        private static int _anchorFrame = -1;

        private static void Gather()
        {
            if (_anchorFrame == Time.frameCount) return;
            _anchorFrame = Time.frameCount;
            _anchors.Clear(); _customer.Clear();
            foreach (var z in Object.FindObjectsByType<PlacementZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (z.Locked) continue;
                _anchors.Add((z.Anchor != null ? z.Anchor.position : z.transform.position, z.transform));
            }
            foreach (var t in Object.FindObjectsByType<OrderTablet>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) _anchors.Add((t.transform.position, t.transform));
            foreach (var r in Object.FindObjectsByType<ReceivingArea>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) _anchors.Add((r.transform.position, r.transform));
            // Only the route customers can actually walk today. The shop object is alive from day one now (§15.1),
            // and reserving a corridor to a counter that is not standing closed the whole starter unit to
            // placement: 415 of 3,708 candidate spots were refused for cutting off a route to nothing.
            var shop = Object.FindAnyObjectByType<RetailShop>();
            if (shop != null && shop.Trading)
            {
                void Live(Transform t) { if (t != null && t.gameObject.activeInHierarchy) _customer.Add((t.position, t)); }
                foreach (var q in shop.QueuePoints) Live(q);
                foreach (var b in shop.BrowsePoints) Live(b);
                Live(shop.CounterCustomerPoint);
                Live(shop.DoorPoint);
            }
        }

        private static List<(Vector3 p, Transform t)> Anchors() { Gather(); return _anchors; }
        private static List<(Vector3 p, Transform t)> CustomerPoints() { Gather(); return _customer; }

        // ---------------------------------------------------------------------------------------------
        private static bool[] _walkable;
        private static int _gw, _gh, _maskFrame = -10000;

        /// <summary>Force the walkable mask to be measured again (after a fixture moves).</summary>
        public static void InvalidateMask() { _maskFrame = -10000; _anchorFrame = -1; }

        private static void EnsureMask()
        {
            if (Time.frameCount - _maskFrame < 30 && _walkable != null) return;
            _maskFrame = Time.frameCount;
            _gw = Mathf.CeilToInt((ShopPlan.XMax - ShopPlan.XMin) / Cell);
            _gh = Mathf.CeilToInt((ShopPlan.ZMax - ShopPlan.ZMin) / Cell);
            if (_walkable == null || _walkable.Length != _gw * _gh) _walkable = new bool[_gw * _gh];
            for (int j = 0; j < _gh; j++)
                for (int i = 0; i < _gw; i++)
                    _walkable[j * _gw + i] = Free(CellCentre(i, j), null);
        }

        private static Vector3 CellCentre(int i, int j) => new Vector3(ShopPlan.XMin + (i + 0.5f) * Cell, 0f, ShopPlan.ZMin + (j + 0.5f) * Cell);

        /// <summary>A cell the player fits through: clear at shin and chest height, with floor under it.</summary>
        private static bool Free(Vector3 p, PlaceableFixture ignore)
        {
            int n = Physics.OverlapSphereNonAlloc(p + Vector3.up * 0.35f, PlayerRadius, _hits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (!Skip(_hits[i], ignore)) return false;
            n = Physics.OverlapSphereNonAlloc(p + Vector3.up * 1.25f, PlayerRadius, _hits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (!Skip(_hits[i], ignore)) return false;
            return true;
        }

        private static bool Skip(Collider c, PlaceableFixture ignore)
        {
            if (c == null || c.isTrigger || !c.enabled) return true;
            if (IsFloor(c.transform)) return true;
            if (ignore != null && c.transform.IsChildOf(ignore.transform)) return true;
            if (c.GetComponentInParent<SpecimenEntity>() != null) return true;
            if (c.GetComponentInParent<CharacterController>() != null) return true;
            if (c.GetComponentInParent<Customer>() != null) return true;
            return false;
        }

        /// <summary>
        /// Flood fill from the player's own position over the walkable mask, with the candidate fixture added as a
        /// blocker. Every station anchor, queue position and browsing spot must still be reached.
        /// </summary>
        private static bool RouteHolds(PlaceableFixture f, Vector3 centre, Vector3 half, Quaternion rot, out string reason)
        {
            reason = "";
            EnsureMask();
            var open = new bool[_gw * _gh];
            for (int j = 0; j < _gh; j++)
                for (int i = 0; i < _gw; i++)
                {
                    int k = j * _gw + i;
                    if (!_walkable[k]) { open[k] = false; continue; }
                    var p = CellCentre(i, j);
                    // the fixture's own footprint, grown by the player's radius, is not walkable
                    open[k] = !PointInBox(p, centre, half, rot, PlayerRadius);
                    // nor is its old position walkable again yet — the mask still has it there, which is safe
                }
            // seed from the player, falling back to the workshop door
            var player = Object.FindAnyObjectByType<CharacterController>();
            var seed = player != null ? player.transform.position : new Vector3(ShopPlan.WorkDoorX, 0f, ShopPlan.ZMin + 0.6f);
            if (!Seed(open, seed, out int si, out int sj)) { reason = "cannot find a way in"; return false; }

            var q = new Queue<int>();
            var seen = new bool[_gw * _gh];
            int start = sj * _gw + si;
            seen[start] = true; q.Enqueue(start);
            while (q.Count > 0)
            {
                int k = q.Dequeue();
                int i = k % _gw, j = k / _gw;
                for (int d = 0; d < 4; d++)
                {
                    int ni = i + (d == 0 ? 1 : d == 1 ? -1 : 0), nj = j + (d == 2 ? 1 : d == 3 ? -1 : 0);
                    if (ni < 0 || nj < 0 || ni >= _gw || nj >= _gh) continue;
                    int nk = nj * _gw + ni;
                    if (seen[nk] || !open[nk]) continue;
                    seen[nk] = true; q.Enqueue(nk);
                }
            }
            foreach (var a in Anchors())
                if (!Reached(seen, open, a.p)) { reason = "it cuts off the way to a workstation"; return false; }
            foreach (var a in CustomerPoints())
                if (!Reached(seen, open, a.p)) { reason = "it cuts off a customer's route to the counter"; return false; }
            return true;
        }

        private static bool Seed(bool[] open, Vector3 p, out int si, out int sj)
        {
            si = Mathf.Clamp(Mathf.FloorToInt((p.x - ShopPlan.XMin) / Cell), 0, _gw - 1);
            sj = Mathf.Clamp(Mathf.FloorToInt((p.z - ShopPlan.ZMin) / Cell), 0, _gh - 1);
            if (open[sj * _gw + si]) return true;
            for (int r = 1; r <= 6; r++)
                for (int dj = -r; dj <= r; dj++)
                    for (int di = -r; di <= r; di++)
                    {
                        int i = si + di, j = sj + dj;
                        if (i < 0 || j < 0 || i >= _gw || j >= _gh) continue;
                        if (open[j * _gw + i]) { si = i; sj = j; return true; }
                    }
            return false;
        }

        /// <summary>An anchor is reached if any walkable cell within arm's length of it was flooded.</summary>
        private static bool Reached(bool[] seen, bool[] open, Vector3 p)
        {
            int ci = Mathf.FloorToInt((p.x - ShopPlan.XMin) / Cell), cj = Mathf.FloorToInt((p.z - ShopPlan.ZMin) / Cell);
            const int R = 5;   // 1.25 m: standing distance from a bench top or a shelf
            for (int dj = -R; dj <= R; dj++)
                for (int di = -R; di <= R; di++)
                {
                    int i = ci + di, j = cj + dj;
                    if (i < 0 || j < 0 || i >= _gw || j >= _gh) continue;
                    if (seen[j * _gw + i]) return true;
                }
            // an anchor that was already unreachable before this placement is not this placement's fault
            for (int dj = -R; dj <= R; dj++)
                for (int di = -R; di <= R; di++)
                {
                    int i = ci + di, j = cj + dj;
                    if (i < 0 || j < 0 || i >= _gw || j >= _gh) continue;
                    if (open[j * _gw + i]) return false;
                }
            return true;
        }
    }
}
