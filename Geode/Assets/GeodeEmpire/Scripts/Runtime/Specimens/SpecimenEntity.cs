using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;
using GeodeEmpire.Save;

namespace GeodeEmpire.Specimens
{
    /// <summary>How an opened geode's halves are arranged where it sits.</summary>
    public enum DisplayPose { Natural, Closed, SideBySide, Clamshell }

    /// <summary>
    /// The physical specimen in the workshop: record + visual + physics. One per SpecimenRecord that is in the world.
    /// </summary>
    public sealed class SpecimenEntity : MonoBehaviour, IInteractable
    {
        public SpecimenRecord Record { get; private set; }
        public SpecimenVisual Visual { get; private set; }
        public Rigidbody Body { get; private set; }
        public PlacementZone Zone;
        public bool Locked;

        private readonly List<Collider> _colliders = new List<Collider>();
        private float _settleTimer;
        private bool _physicsOn;

        public string Id => Record.Id;
        public SpecimenGeology Geology => Record.Geology;
        public bool IsOpened => Record.IsOpened;
        public float Radius => Visual != null && Visual.Geometry != null ? Visual.Geometry.MaxRadius : 0.06f;

        public string ShortName => IsOpened ? Record.DisplayName : $"rock ({Geology.MassKg:F1} kg)";
        public bool IsPiece => Record.IsPiece;

        public static SpecimenEntity Create(SpecimenRecord record, SpecimenAssetLibrary lib)
        {
            var go = new GameObject("Specimen_" + record.Id);
            go.layer = LayerMask.NameToLayer("Default");
            var e = go.AddComponent<SpecimenEntity>();
            e.Record = record;
            e.Visual = go.AddComponent<SpecimenVisual>();
            if (record.IsPiece) e.Visual.Build(record.Geology, record.Condition, lib, record.Piece, record.Polish);
            else e.Visual.Build(record.Geology, record.Condition, lib);
            // a worked rock shows its chips and cracks wherever it is, not only on the bench; a sawn piece has no seam
            e.Visual.SetCrackState(record.SectorStress, record.Impacts, record.IsPiece ? 0f : 0.55f, record.IsOpened ? 0.35f : 1f);
            e.Body = go.AddComponent<Rigidbody>();
            e.Body.mass = Mathf.Clamp(record.Geology.MassKg, 0.2f, 6f);
            e.Body.interpolation = RigidbodyInterpolation.None;
            e.Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            e.Body.linearDamping = 0.3f;
            e.Body.angularDamping = 1.5f;
            e.Body.maxAngularVelocity = 12f;
            e.RebuildColliders();
            e.SetPhysics(false);
            e.ApplyOpenPose();
            return e;
        }

        /// <summary>
        /// Fit the two hull colliders. Cheap to call: if the colliders already carry the right meshes it does
        /// nothing, because a MeshCollider follows its own transform and a rock opening does not change the shape
        /// of either half. Destroying and re-adding them made PhysX re-cook both convex hulls, which cost 89-298 ms
        /// of the frame it landed on — the last real stall left after the reveal.
        /// </summary>
        public void RebuildColliders()
        {
            if (Visual != null && _colliders.Count > 0)
            {
                bool ok = true;
                foreach (var c in _colliders)
                    if (!(c is MeshCollider mc) || (mc.sharedMesh != Visual.BottomColliderMesh && mc.sharedMesh != Visual.TopColliderMesh)) { ok = false; break; }
                int want = (Visual.BottomHalf != null && Visual.BottomColliderMesh != null ? 1 : 0)
                         + (Visual.TopHalf != null && Visual.TopColliderMesh != null ? 1 : 0);
                if (ok && _colliders.Count == want) return;
            }
            foreach (var c in _colliders) if (c != null) Destroy(c);
            _colliders.Clear();
            AddHalfCollider(Visual.BottomHalf, Visual.BottomColliderMesh);
            AddHalfCollider(Visual.TopHalf, Visual.TopColliderMesh);
        }

        private void AddHalfCollider(Transform half, Mesh colliderMesh)
        {
            if (half == null || colliderMesh == null) return;
            var mc = half.gameObject.AddComponent<MeshCollider>();
            // Unity initially assigns the MeshFilter's visual shell. Clear it before enabling convex cooking.
            mc.sharedMesh = null;
            // the generated hulls are already clean and welded; asking PhysX to do it again is the expensive part
            mc.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation
                              | MeshColliderCookingOptions.UseFastMidphase;
            mc.convex = true;
            mc.sharedMesh = colliderMesh;
            mc.material = null;
            _colliders.Add(mc);
        }

        /// <summary>Move a (kinematic) specimen without the physics pose lagging behind the transform.</summary>
        public void SetPose(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            if (Body != null)
            {
                Body.position = position;
                Body.rotation = rotation;
            }
        }

        public void SetPhysics(bool on)
        {
            _physicsOn = on;
            if (Body != null)
            {
                Body.interpolation = on ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
                Body.isKinematic = !on;
                if (on)
                {
                    Body.position = transform.position;
                    Body.rotation = transform.rotation;
                    Body.WakeUp();
                    _settleTimer = 0f;
                }
            }
            foreach (var c in _colliders) if (c != null) c.enabled = on || Zone != null || Locked;
        }

        /// <summary>Colliders active but no dynamics (placed on a station).</summary>
        public void SetStaticCollidable()
        {
            if (Body != null) Body.isKinematic = true;
            _physicsOn = false;
            foreach (var c in _colliders) if (c != null) c.enabled = true;
        }

        public void SetCollidersEnabled(bool on)
        {
            foreach (var c in _colliders) if (c != null) c.enabled = on;
        }

        /// <summary>
        /// Lowest point of the (closed) rock under a given rotation, relative to its pivot: how far the pivot must sit
        /// above a surface so a tilted rock rests on it instead of sinking in. Uses the collider hulls, so it agrees
        /// with what physics and the collision audit see.
        /// </summary>
        public float LowestPointOffset(Quaternion rotation)
        {
            float lowest = Mathf.Min(LowestOf(Visual != null ? Visual.BottomColliderMesh : null, rotation), IsOpened ? float.MaxValue : LowestOf(Visual != null ? Visual.TopColliderMesh : null, rotation));
            return lowest == float.MaxValue ? -0.05f : lowest;
        }

        /// <summary>Lowest hull vertex of the top half under a local rotation (negative: below the half's pivot).</summary>
        public float LowestOfTop(Quaternion localRotation) => LowestOf(Visual != null ? Visual.TopColliderMesh : null, localRotation);

        private static float LowestOf(Mesh m, Quaternion rotation)
        {
            if (m == null) return float.MaxValue;
            float lowest = float.MaxValue;
            var verts = m.vertices;
            for (int i = 0; i < verts.Length; i++) { float y = (rotation * verts[i]).y; if (y < lowest) lowest = y; }
            return lowest;
        }

        private float _bottomLowest = float.NaN;

        /// <summary>
        /// How the closed rock stands under a rotation: hull height, and the half-width of its base where a cradle ring
        /// touches it (the narrower horizontal extent of the hull vertices in a band just above the lowest point).
        /// </summary>
        public void SupportProfile(Quaternion rotation, float bandHeight, out float height, out float baseHalfWidth)
        {
            SupportProfileOf(Visual != null ? Visual.BottomColliderMesh : null, IsOpened || Visual == null ? null : Visual.TopColliderMesh, rotation, bandHeight, out height, out baseHalfWidth);
        }

        /// <summary>Axis-aligned bounds of the hull (collider meshes) under a rotation, about the pivot: what a vise or a cradle sees.</summary>
        public void HullBoundsFor(Quaternion rotation, out Vector3 min, out Vector3 max)
        {
            var lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue); var hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            void Span(Mesh m)
            {
                if (m == null) return;
                var vs = m.vertices;
                for (int i = 0; i < vs.Length; i++) { var p = rotation * vs[i]; lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
            }
            Span(Visual != null ? Visual.BottomColliderMesh : null);
            if (!IsOpened && Visual != null) Span(Visual.TopColliderMesh);
            if (lo.x == float.MaxValue) { float r = Radius; lo = new Vector3(-r, -r, -r); hi = new Vector3(r, r, r); }
            min = lo; max = hi;
        }

        public static void SupportProfileOf(Mesh bottom, Mesh top, Quaternion rotation, float bandHeight, out float height, out float baseHalfWidth)
        {
            height = 0.1f; baseHalfWidth = 0.05f;
            var vb = bottom != null ? bottom.vertices : null;
            var vt = top != null ? top.vertices : null;
            if ((vb == null || vb.Length == 0) && (vt == null || vt.Length == 0)) return;
            float lowest = float.MaxValue, highest = float.MinValue;
            void Span(Vector3[] vs) { if (vs == null) return; for (int i = 0; i < vs.Length; i++) { float y = (rotation * vs[i]).y; if (y < lowest) lowest = y; if (y > highest) highest = y; } }
            Span(vb); Span(vt);
            height = Mathf.Max(0.01f, highest - lowest);
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            float lo = lowest + bandHeight - 0.008f, hi = lowest + bandHeight + 0.008f;
            void Band(Vector3[] vs)
            {
                if (vs == null) return;
                for (int i = 0; i < vs.Length; i++)
                {
                    var p = rotation * vs[i];
                    if (p.y < lo || p.y > hi) continue;
                    if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x; if (p.z < minZ) minZ = p.z; if (p.z > maxZ) maxZ = p.z;
                }
            }
            Band(vb); Band(vt);
            if (minX == float.MaxValue) { baseHalfWidth = 0.004f; return; }   // nothing at that height: a point of contact
            baseHalfWidth = 0.5f * Mathf.Min(maxX - minX, maxZ - minZ);
        }

        /// <summary>
        /// How far above a surface the pivot must sit so the rock rests on it: the bottom hull's real lowest lump, not
        /// the pole, so nothing sinks into cradles, trays or shelves.
        /// </summary>
        public float RestHeightOffset(bool cavityUp)
        {
            if (Visual == null || Visual.Geometry == null) return 0.05f;
            if (float.IsNaN(_bottomLowest)) _bottomLowest = LowestOf(Visual.BottomColliderMesh, Quaternion.identity);
            return _bottomLowest == float.MaxValue ? -Visual.Geometry.BottomY : -_bottomLowest;
        }

        /// <summary>Arrange halves for an opened specimen: top half hinged open beside the bottom half.</summary>
        public void ApplyOpenPose()
        {
            if (Visual == null) return;
            if (Visual.TopHalf == null) { Visual.SetCrystalsVisible(true); return; }   // a sawn piece is one body
            if (IsOpened)
            {
                Visual.SetCrystalsVisible(true);
                if (TopPoseFor(DisplayPose.Natural, out var position, out var rotation))
                {
                    Visual.TopHalf.localRotation = rotation;
                    Visual.TopHalf.localPosition = position;
                }
            }
            else
            {
                Visual.SetCrystalsVisible(false);
                Visual.TopHalf.localRotation = Quaternion.identity;
                Visual.TopHalf.localPosition = Vector3.zero;
            }
        }

        /// <summary>Storage pose: an opened geode closes back up (lid on, crystals kept visible) so it takes one rock's room on a rack.</summary>
        public void ApplyStoredPose()
        {
            if (Visual == null || Visual.TopHalf == null || !IsOpened) return;
            Visual.TopHalf.localRotation = Quaternion.identity;
            Visual.TopHalf.localPosition = Vector3.zero;
        }

        /// <summary>
        /// Where the top half sits (specimen-local) in a display pose; false for a closed rock or a sawn piece (one body).
        /// Natural is the layout the reveal left (or side by side); Clamshell props the top half up behind the bottom half,
        /// cavity toward -Z (the front of a shelf), so a big geode takes 2r of shelf width instead of 4r.
        /// </summary>
        public bool TopPoseFor(DisplayPose pose, out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero; rot = Quaternion.identity;
            if (Visual == null || Visual.TopHalf == null || Visual.Geometry == null || !IsOpened) return false;
            float r = Visual.Geometry.MeanEquatorRadius;
            switch (pose)
            {
                case DisplayPose.Closed:
                    return true;
                case DisplayPose.Clamshell:
                    rot = Quaternion.Euler(-72f, 0f, 0f) * Quaternion.Euler(0f, 0f, 180f);
                    pos = new Vector3(0f, -RestHeightOffset(false) - LowestOfTop(rot), r * 1.02f + 0.012f);
                    break;
                case DisplayPose.Natural when Record.HasOpenPose:
                    pos = Record.OpenTopLocalPos; rot = Record.OpenTopLocalRot;
                    break;
                default:
                    rot = Quaternion.Euler(0f, 0f, 180f);
                    pos = new Vector3(-r * 2.15f, -RestHeightOffset(false) - LowestOfTop(rot), 0f);
                    break;
            }
            SeparateTopPose(ref pos, rot);
            return true;
        }

        /// <summary>Distance between half pivots that leaves a small gap along the given specimen-local direction.</summary>
        public static float HalfSeparation(Mesh bottom, Mesh top, Quaternion topRotation, Vector3 direction)
        {
            if (bottom == null || top == null) return 0f;
            direction.Normalize();
            float bottomExtent = float.MinValue, topExtent = float.MinValue;
            foreach (var p in bottom.vertices) bottomExtent = Mathf.Max(bottomExtent, Vector3.Dot(p, direction));
            foreach (var p in top.vertices) topExtent = Mathf.Max(topExtent, Vector3.Dot(topRotation * p, -direction));
            return bottomExtent + topExtent + 0.004f;
        }

        private void SeparateTopPose(ref Vector3 position, Quaternion rotation)
        {
            // Mean radius misses large knobs. Keep the requested layout and base height, but move the lid
            // far enough sideways that the actual hulls cannot intersect, including poses from older saves.
            var direction = new Vector3(position.x, 0f, position.z);
            float distance = direction.magnitude;
            if (distance < 0.0001f) return; // an intentionally closed pose
            float minimum = HalfSeparation(Visual.BottomColliderMesh, Visual.TopColliderMesh, rotation, direction);
            if (distance < minimum) position += direction / distance * (minimum - distance);
        }

        public void ApplyPose(DisplayPose pose)
        {
            if (Visual == null) return;
            if (Visual.TopHalf == null) { Visual.SetCrystalsVisible(true); return; }
            if (!IsOpened) { ApplyOpenPose(); return; }
            Visual.SetCrystalsVisible(true);
            if (TopPoseFor(pose, out var p, out var q)) { Visual.TopHalf.localRotation = q; Visual.TopHalf.localPosition = p; }
        }

        /// <summary>Specimen-local bounds of the collider hulls in a pose: the footprint a surface has to support (pivot at the origin).</summary>
        public Bounds FootprintFor(DisplayPose pose)
        {
            if (Visual == null) return new Bounds(Vector3.zero, Vector3.one * 0.1f);
            var b = new Bounds(Vector3.zero, Vector3.zero);
            bool any = false;
            void Add(Mesh m, Vector3 p, Quaternion q)
            {
                if (m == null) return;
                var verts = m.vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    var v = q * verts[i] + p;
                    if (!any) { b = new Bounds(v, Vector3.zero); any = true; } else b.Encapsulate(v);
                }
            }
            Add(Visual.BottomColliderMesh, Vector3.zero, Quaternion.identity);
            if (Visual.TopColliderMesh != null)
            {
                if (TopPoseFor(pose, out var p, out var q)) Add(Visual.TopColliderMesh, p, q);
                else Add(Visual.TopColliderMesh, Vector3.zero, Quaternion.identity);
            }
            return b;
        }

        /// <summary>
        /// Freeze the flipped top half where the reveal left it, but resting on the bottom half's own base plane, so the
        /// pose is right on every surface the specimen is later set on (the bench drops it onto the bench top instead).
        /// </summary>
        public void CommitOpenPose()
        {
            if (Visual == null || Visual.TopHalf == null || !IsOpened) return;
            var p = Visual.TopHalf.localPosition;
            p.y = -RestHeightOffset(false) - LowestOfTop(Visual.TopHalf.localRotation);
            SeparateTopPose(ref p, Visual.TopHalf.localRotation);
            Visual.TopHalf.localPosition = p;
            Record.OpenTopLocalPos = p;
            Record.OpenTopLocalRot = Visual.TopHalf.localRotation;
            Record.HasOpenPose = true;
        }

        public void SetHighlight(bool on) => Visual?.SetHighlight(on ? 1f : 0f);

        public bool CanInteract(PlayerInteractor player) => !Locked && player.Held == null;

        public string GetPrompt(PlayerInteractor player) => IsOpened ? $"Pick up {Record.DisplayName}" : $"Pick up rock  {Geology.MassKg:F1} kg";

        /// <summary>A piece on a shelf reads like its label: what it is, what it is worth, where it came from.</summary>
        public string GetHint(PlayerInteractor player)
        {
            if (!IsOpened || Zone == null || (Zone.Kind != ZoneKind.DisplaySlot && Zone.Kind != ZoneKind.SaleSlot)) return null;
            var g = Geology;
            string line = $"{g.Family.Name}  •  {Specimens.Valuation.TierLabel(Specimens.Valuation.TierFromValue(Record.EstimatedValue()))}  •  {UI.UiKit.Money(Record.EstimatedValue())}";
            string prov = UI.TabletUI.Provenance(Record, true);
            return prov.Length > 0 ? line + "  •  " + prov : line;
        }

        public void Interact(PlayerInteractor player)
        {
            if (Locked) return;
            if (Zone != null) Zone.Take(this);
            player.PickUp(this);
        }

        private void FixedUpdate()
        {
            if (!_physicsOn || Body == null || Body.isKinematic) return;
            if (Body.linearVelocity.sqrMagnitude < 0.0004f && Body.angularVelocity.sqrMagnitude < 0.001f)
            {
                _settleTimer += Time.fixedDeltaTime;
                if (_settleTimer > 0.5f)
                {
                    Record.WorldPosition = transform.position;
                    Record.WorldRotation = transform.rotation;
                }
            }
            else _settleTimer = 0f;
            // out-of-bounds recovery: never let a specimen vanish through the floor
            if (transform.position.y < -2f)
            {
                Body.linearVelocity = Vector3.zero;
                transform.position = new Vector3(Mathf.Clamp(transform.position.x, -3f, 3f), 1.2f, Mathf.Clamp(transform.position.z, -2f, 2f));
            }
        }

        public void SyncRecordTransform()
        {
            Record.WorldPosition = transform.position;
            Record.WorldRotation = transform.rotation;
        }
    }
}
