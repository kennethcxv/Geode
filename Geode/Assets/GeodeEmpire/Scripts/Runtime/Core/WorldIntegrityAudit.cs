using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GeodeEmpire.Interaction;
using GeodeEmpire.Specimens;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.Core
{
    /// <summary>
    /// Dev/QA: the static world-integrity audit V5 asks for. Measures real collider interpenetration between every
    /// pair of fixed objects in the workshop (walls, furniture, machines, props), objects sunk below the floor,
    /// objects floating with nothing under or beside them, placement-zone slots whose full footprint is not
    /// supported by a surface, and player clearance (reachability of every station and pinch points narrower than
    /// the player). Bounds tests alone pass visibly broken arrangements, so this uses Physics.ComputePenetration on
    /// the actual colliders and rays against the actual surfaces; the visual walkthrough still has the last word.
    /// </summary>
    public static class WorldIntegrityAudit
    {
        public const float PenetrationTolerance = 0.006f;   // 6 mm: contact noise below, real interpenetration above
        public const float PlayerRadius = 0.3f;

        /// <summary>Room-shell boxes overlap each other by design (corners, trim over walls); pairs inside this set are skipped.</summary>
        private static bool IsShell(Transform t)
        {
            string n = t.name;
            return n.StartsWith("Wall") || n == "Floor" || n == "FloorShop" || n == "ShopFloor" || n == "Ceiling" || n.StartsWith("Wainscot") || n.StartsWith("Skirt")
                || n.StartsWith("Partition") || n.StartsWith("Porch") || n.StartsWith("Beam") || n == "DoorLintel" || n == "Pipe" || n.EndsWith("Mat");
        }

        private static bool IsDynamic(Collider c)
        {
            return c.GetComponentInParent<SpecimenEntity>() != null || c.GetComponentInParent<CrateEntity>() != null
                || c.GetComponentInParent<CharacterController>() != null || c.GetComponentInParent<Retail.Customer>() != null
                || c.GetComponentInParent<Retail.RetailShop>() != null && c.transform.root.name == "CustomerTemplate";
        }

        /// <summary>The fixture an object belongs to: the child of Stations/Environment/Stage2 it hangs off, so a cradle on its bench is one fixture.</summary>
        private static Transform Fixture(Transform t)
        {
            var cur = t;
            while (cur.parent != null)
            {
                string pn = cur.parent.name;
                if (pn == "Stations" || pn == "Environment" || pn == "Stage2" || pn == "RetailShop" || pn == "WorkshopExpansion") return cur;
                cur = cur.parent;
            }
            return cur;
        }

        private static string Name(Component c)
        {
            var f = Fixture(c.transform);
            return f == c.transform ? c.name : f.name + "/" + c.name;
        }

        public struct Finding
        {
            public string Kind, A, B;
            public float Amount;
            public override string ToString() => $"{Kind}: {A}{(B != null ? " <> " + B : "")} ({Amount * 1000f:F0} mm)";
        }

        // -----------------------------------------------------------------------------------------------------
        public static List<Finding> StaticOverlaps()
        {
            var results = new List<Finding>();
            var all = new List<Collider>();
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
                if (c.enabled && !c.isTrigger && !IsDynamic(c)) all.Add(c);
            Physics.SyncTransforms();
            for (int i = 0; i < all.Count; i++)
            {
                var a = all[i];
                var ba = a.bounds;
                for (int j = i + 1; j < all.Count; j++)
                {
                    var b = all[j];
                    if (!ba.Intersects(b.bounds)) continue;
                    var fa = Fixture(a.transform); var fb = Fixture(b.transform);
                    if (fa == fb) continue;                                   // parts of one fixture touch by design
                    if (IsShell(fa) && IsShell(fb)) continue;                 // the room shell overlaps itself at corners
                    if (!Physics.ComputePenetration(a, a.transform.position, a.transform.rotation, b, b.transform.position, b.transform.rotation, out _, out float depth)) continue;
                    if (depth > PenetrationTolerance) results.Add(new Finding { Kind = "overlap", A = Name(a), B = Name(b), Amount = depth });
                }
            }
            results.Sort((x, y) => y.Amount.CompareTo(x.Amount));
            return results;
        }

        /// <summary>Renderers without colliders (signs, lamps, jars, posters): every mesh vertex is tested for lying inside a
        /// solid collider of another fixture (Collider.ClosestPoint), so a tilted broom or a hanging lamp is judged by its
        /// real surface, not by an axis-aligned box around it.</summary>
        public static List<Finding> DecorOverlaps()
        {
            var results = new List<Finding>();
            var cols = new List<Collider>();
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
                if (c.enabled && !c.isTrigger && !IsDynamic(c) && (!(c is MeshCollider mc) || mc.convex)) cols.Add(c);
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                if (r.GetComponentInParent<Collider>() != null) continue;
                if (r.GetComponentInParent<SpecimenEntity>() != null || r.GetComponentInParent<CrateEntity>() != null || r.GetComponentInParent<Retail.Customer>() != null) continue;
                if (r.name == "Bulb" || r.name.Contains("Text") || r.name == "Print" || r.name == "Screen") continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var rb = r.bounds;
                var fr = Fixture(r.transform);
                // in Play Mode a static-batched renderer points at the combined scene mesh (not readable, already in world space):
                // judge those by the corners and centre of their world bounds instead of their vertices
                bool readable = mf.sharedMesh.isReadable;
                Vector3[] verts = readable ? mf.sharedMesh.vertices : new[] { rb.center, new Vector3(rb.min.x, rb.min.y, rb.min.z), new Vector3(rb.max.x, rb.min.y, rb.min.z), new Vector3(rb.min.x, rb.max.y, rb.min.z), new Vector3(rb.max.x, rb.max.y, rb.min.z), new Vector3(rb.min.x, rb.min.y, rb.max.z), new Vector3(rb.max.x, rb.min.y, rb.max.z), new Vector3(rb.min.x, rb.max.y, rb.max.z), new Vector3(rb.max.x, rb.max.y, rb.max.z) };
                int step = Mathf.Max(1, verts.Length / 1500);
                foreach (var c in cols)
                {
                    if (Fixture(c.transform) == fr) continue;
                    if (!rb.Intersects(c.bounds)) continue;
                    int inside = 0; float worst = 0f;
                    var cb = c.bounds;
                    for (int i = 0; i < verts.Length; i += step)
                    {
                        var w = readable ? r.transform.TransformPoint(verts[i]) : verts[i];
                        if (!cb.Contains(w)) continue;
                        var cp = c.ClosestPoint(w);
                        if ((cp - w).sqrMagnitude < 1e-8f)
                        {
                            inside++;
                            // depth: how far to the nearest face of the collider's bounds (good enough for boxes)
                            float d = Mathf.Min(Mathf.Min(w.x - cb.min.x, cb.max.x - w.x), Mathf.Min(Mathf.Min(w.y - cb.min.y, cb.max.y - w.y), Mathf.Min(w.z - cb.min.z, cb.max.z - w.z)));
                            worst = Mathf.Max(worst, d);
                        }
                    }
                    if (inside > 0 && worst > 0.006f) results.Add(new Finding { Kind = "decor-inside", A = Name(r) + $" ({inside} verts)", B = Name(c), Amount = worst });
                }
            }
            results.Sort((x, y) => y.Amount.CompareTo(x.Amount));
            return results;
        }

        /// <summary>Anything whose renderer bounds dip below the floor slab, and anything that touches nothing at all:
        /// no surface within 5 cm under its lowest point and no other object's collider or renderer within 3 cm of its
        /// bounds (a jar on a collider-less wall shelf counts as supported by the shelf's renderer).</summary>
        public static List<Finding> FloorAndFloating()
        {
            var results = new List<Finding>();
            var all = new List<Renderer>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            {
                if (r.GetComponentInParent<SpecimenEntity>() != null || r.GetComponentInParent<CrateEntity>() != null || r.GetComponentInParent<Retail.Customer>() != null) continue;
                if (r is ParticleSystemRenderer) continue;
                all.Add(r);
            }
            foreach (var r in all)
            {
                var f = Fixture(r.transform);
                if (IsShell(f)) continue;
                if (r.name.Contains("Text") || r.name == "Bulb" || r.name == "Print" || r.name == "Screen" || r.name == "Knob") continue;
                var b = r.bounds;
                if (b.min.y < -0.012f) results.Add(new Finding { Kind = "below-floor", A = Name(r), Amount = -b.min.y });
                if (b.min.y <= 0.02f || b.size.magnitude < 0.08f) continue;
                var origin = new Vector3(b.center.x, b.min.y + 0.01f, b.center.z);
                bool under = Physics.Raycast(origin, Vector3.down, out var hit, 0.06f, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(r.transform);
                if (under) continue;
                bool touching = false;
                var grown = new Bounds(b.center, b.size + Vector3.one * 0.06f);
                int n = Physics.OverlapBoxNonAlloc(b.center, b.extents + Vector3.one * 0.03f, _overlapHits, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n && !touching; i++) if (!_overlapHits[i].transform.IsChildOf(r.transform) && !IsDynamic(_overlapHits[i])) touching = true;
                for (int i = 0; i < all.Count && !touching; i++)
                {
                    var o = all[i];
                    if (o == r || o.transform.IsChildOf(r.transform)) continue;   // a parent's renderer (the shelf under a jar) counts as support
                    if (grown.Intersects(o.bounds)) touching = true;
                }
                if (!touching) results.Add(new Finding { Kind = "floating", A = Name(r), Amount = b.min.y });
            }
            return results;
        }

        /// <summary>
        /// Every placement slot: the largest specimen the zone can take is stood on the slot and its footprint ring is
        /// rayed down; a ring point that finds no support within 4 cm of the slot's surface means a rock could hang
        /// over the edge of the tray/shelf/platform. Radii: rough up to 0.13 (large), oversized 0.2 on the heavy cradle.
        /// </summary>
        public static List<Finding> PlacementSupport()
        {
            var results = new List<Finding>();
            foreach (var z in Object.FindObjectsByType<PlacementZone>(FindObjectsInactive.Exclude))
            {
                // a station still under its cover (Stage-1 tarp / boxes) has no surface to offer yet
                var saw = z.GetComponentInParent<Lapidary.SawStation>(); if (saw != null && saw.Machine != null && !saw.Machine.activeInHierarchy) continue;
                var lap = z.GetComponentInParent<Lapidary.PolishStation>(); if (lap != null && lap.Machine != null && !lap.Machine.activeInHierarchy) continue;
                var cr = z.GetComponentInParent<Cracking.CrackerStation>(); if (cr != null && cr.Machine != null && !cr.Machine.activeInHierarchy) continue;
                var anchor = z.Anchor != null ? z.Anchor : z.transform;
                float hx = z.SupportHalfSize.x, hz = z.SupportHalfSize.y;
                int slots = z.Packed ? 1 : Mathf.Max(1, z.Capacity);
                for (int i = 0; i < slots; i++)
                {
                    var centre = anchor.TransformPoint(z.SlotLocalOffset(i));
                    var top = centre + Vector3.up * 0.25f;
                    if (!SurfaceBelow(top, 0.5f, out float surface))
                    {
                        results.Add(new Finding { Kind = "slot-no-surface", A = Name(z) + "#" + i, Amount = 0f });
                        continue;
                    }
                    float aboveOk = z.Kind == ZoneKind.Cradle || z.Kind == ZoneKind.Cracker ? 0.06f : 0.03f;   // a rock sits up on the sandbag ring / the cracker's chain
                    if (centre.y - surface > aboveOk || centre.y - surface < -0.005f) results.Add(new Finding { Kind = "slot-height", A = $"{Name(z)}#{i} anchor {centre.y - surface:F3} m above its surface", Amount = Mathf.Abs(centre.y - surface) });
                    if (z.Kind == ZoneKind.Saw || z.Kind == ZoneKind.Cradle || z.Kind == ZoneKind.Cracker) continue;   // held by jaws / nestled in a sandbag ring / cupped by the chain
                    int unsupported = 0;
                    // the support rectangle's corners and edge midpoints, in the anchor's frame
                    var ring = new[] { new Vector3(-hx, 0, -hz), new Vector3(0, 0, -hz), new Vector3(hx, 0, -hz), new Vector3(hx, 0, 0), new Vector3(hx, 0, hz), new Vector3(0, 0, hz), new Vector3(-hx, 0, hz), new Vector3(-hx, 0, 0) };
                    foreach (var o in ring)
                    {
                        var p = anchor.TransformPoint(z.SlotLocalOffset(i) + o * 0.97f) + Vector3.up * 0.25f;
                        bool ok = SurfaceBelow(p, 0.5f, out float y) && Mathf.Abs(y - surface) < 0.04f;
                        if (!ok) unsupported++;
                    }
                    if (unsupported > 0) results.Add(new Finding { Kind = "slot-overhang", A = $"{Name(z)}#{i} support {hx * 2:F2}x{hz * 2:F2} ({unsupported}/8 edge points off the surface)", Amount = Mathf.Max(hx, hz) });
                }
            }
            return results;
        }

        private static readonly RaycastHit[] _rayHits = new RaycastHit[16];
        private static readonly Collider[] _overlapHits = new Collider[32];

        /// <summary>Nearest solid, non-specimen surface under a point (rocks sitting in a slot are not the slot's surface).</summary>
        private static bool SurfaceBelow(Vector3 from, float maxDist, out float y)
        {
            y = 0f;
            int n = Physics.RaycastNonAlloc(from, Vector3.down, _rayHits, maxDist, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var h = _rayHits[i];
                if (h.collider.GetComponentInParent<SpecimenEntity>() != null || h.collider.GetComponentInParent<CrateEntity>() != null) continue;
                if (h.distance < best) { best = h.distance; y = h.point.y; }
            }
            return best < float.MaxValue;
        }

        /// <summary>
        /// Player clearance: a 15 cm grid over the floor, a cell is free when a player capsule fits there. Flood-fill
        /// from the player start; every placement zone / station needs a free, connected cell within 1.1 m of its
        /// interaction point. Pinch points: free corridor cells narrower than 0.6 m in both axes.
        /// </summary>
        public static List<Finding> Clearance(out int freeCells, out int reachableCells)
        {
            var results = new List<Finding>();
            const float cell = 0.15f;
            float x0 = -3.6f, x1 = 7.0f, z0 = -2.7f, z1 = 2.7f;
            int nx = Mathf.CeilToInt((x1 - x0) / cell), nz = Mathf.CeilToInt((z1 - z0) / cell);
            var free = new bool[nx, nz];
            freeCells = 0;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    var p = new Vector3(x0 + (i + 0.5f) * cell, 0f, z0 + (j + 0.5f) * cell);
                    // the static world only: a rock or crate on the floor, or the player, is not a wall
                    int hits = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * (PlayerRadius + 0.02f), p + Vector3.up * (1.8f - PlayerRadius), PlayerRadius, _overlapHits, ~0, QueryTriggerInteraction.Ignore);
                    bool blocked = false;
                    for (int k = 0; k < hits && !blocked; k++)
                    {
                        var c = _overlapHits[k];
                        if (c is CharacterController || c.GetComponentInParent<SpecimenEntity>() != null || c.GetComponentInParent<CrateEntity>() != null || c.GetComponentInParent<CharacterController>() != null) continue;
                        blocked = true;
                    }
                    free[i, j] = !blocked;
                    if (!blocked) freeCells++;
                }
            // flood fill from the player start
            var start = GameObject.Find("PlayerStart");
            var sp = start != null ? start.transform.position : new Vector3(-0.3f, 0f, -0.6f);
            var reach = new bool[nx, nz];
            var q = new Queue<(int, int)>();
            int si = Mathf.Clamp(Mathf.FloorToInt((sp.x - x0) / cell), 0, nx - 1), sj = Mathf.Clamp(Mathf.FloorToInt((sp.z - z0) / cell), 0, nz - 1);
            if (!free[si, sj])
            {
                // nudge to the nearest free cell
                for (int rr = 1; rr < 6 && !free[si, sj]; rr++)
                    for (int di = -rr; di <= rr && !free[si, sj]; di++)
                        for (int dj = -rr; dj <= rr && !free[si, sj]; dj++)
                        {
                            int ii = si + di, jj = sj + dj;
                            if (ii >= 0 && jj >= 0 && ii < nx && jj < nz && free[ii, jj]) { si = ii; sj = jj; }
                        }
            }
            reach[si, sj] = true; q.Enqueue((si, sj));
            reachableCells = 0;
            while (q.Count > 0)
            {
                var (i, j) = q.Dequeue();
                reachableCells++;
                foreach (var (di, dj) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int ii = i + di, jj = j + dj;
                    if (ii < 0 || jj < 0 || ii >= nx || jj >= nz || !free[ii, jj] || reach[ii, jj]) continue;
                    reach[ii, jj] = true; q.Enqueue((ii, jj));
                }
            }
            // stations reachable?
            foreach (var z in Object.FindObjectsByType<PlacementZone>(FindObjectsInactive.Exclude))
            {
                var p = z.transform.position;
                bool ok = false;
                for (int i = 0; i < nx && !ok; i++)
                    for (int j = 0; j < nz && !ok; j++)
                    {
                        if (!reach[i, j]) continue;
                        var c = new Vector3(x0 + (i + 0.5f) * cell, 0f, z0 + (j + 0.5f) * cell);
                        float dx = c.x - p.x, dz = c.z - p.z;
                        if (dx * dx + dz * dz < 1.1f * 1.1f) ok = true;
                    }
                if (!ok) results.Add(new Finding { Kind = "unreachable", A = Name(z), Amount = 0f });
            }
            // pinch points: a reachable cell whose free run is < 4 cells (0.6 m) along X and along Z
            int pinches = 0;
            for (int i = 1; i < nx - 1; i++)
                for (int j = 1; j < nz - 1; j++)
                {
                    if (!reach[i, j]) continue;
                    int runX = 1, runZ = 1;
                    for (int k = i - 1; k >= 0 && free[k, j]; k--) runX++;
                    for (int k = i + 1; k < nx && free[k, j]; k++) runX++;
                    for (int k = j - 1; k >= 0 && free[i, k]; k--) runZ++;
                    for (int k = j + 1; k < nz && free[i, k]; k++) runZ++;
                    if (runX < 4 && runZ < 4)
                    {
                        pinches++;
                        if (pinches <= 6) results.Add(new Finding { Kind = "pinch", A = $"cell ({x0 + (i + 0.5f) * cell:F2}, {z0 + (j + 0.5f) * cell:F2}) runX={runX} runZ={runZ}", Amount = Mathf.Min(runX, runZ) * cell });
                    }
                }
            if (pinches > 6) results.Add(new Finding { Kind = "pinch", A = $"... {pinches - 6} more pinch cells", Amount = 0f });
            return results;
        }

        public static string Report(string tag = "")
        {
            var sb = new StringBuilder();
            sb.Append($"[WorldIntegrity{(string.IsNullOrEmpty(tag) ? "" : " " + tag)}]");
            void Section(string name, List<Finding> list, int max = 25)
            {
                sb.Append($"\n {name}: {list.Count}");
                int shown = 0;
                foreach (var f in list) { if (shown++ >= max) { sb.Append("\n   ..."); break; } sb.Append("\n   ").Append(f); }
            }
            Section("static overlaps", StaticOverlaps());
            Section("decor bounds", DecorOverlaps());
            Section("floor/floating", FloorAndFloating());
            Section("placement support", PlacementSupport(), 40);
            var cl = Clearance(out int freeCells, out int reach);
            Section($"clearance (free {freeCells}, reachable {reach})", cl);
            return sb.ToString();
        }
    }
}
