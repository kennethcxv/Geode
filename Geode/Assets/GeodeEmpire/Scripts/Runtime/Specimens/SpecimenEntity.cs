using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;
using GeodeEmpire.Save;

namespace GeodeEmpire.Specimens
{
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

        public static SpecimenEntity Create(SpecimenRecord record, SpecimenAssetLibrary lib)
        {
            var go = new GameObject("Specimen_" + record.Id);
            go.layer = LayerMask.NameToLayer("Default");
            var e = go.AddComponent<SpecimenEntity>();
            e.Record = record;
            e.Visual = go.AddComponent<SpecimenVisual>();
            e.Visual.Build(record.Geology, record.Condition, lib);
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

        public void RebuildColliders()
        {
            foreach (var c in _colliders) if (c != null) Destroy(c);
            _colliders.Clear();
            AddHalfCollider(Visual.BottomHalf);
            AddHalfCollider(Visual.TopHalf);
        }

        private void AddHalfCollider(Transform half)
        {
            if (half == null) return;
            var mf = half.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;
            var mc = half.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = true;
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

        /// <summary>How far above a surface the pivot must sit so the rock rests on it.</summary>
        public float RestHeightOffset(bool cavityUp)
        {
            if (Visual == null || Visual.Geometry == null) return 0.05f;
            if (IsOpened)
            {
                // opened: bottom half sits on its exterior with the cut face up; pivot is the fracture plane
                return -Visual.Geometry.BottomY;
            }
            return -Visual.Geometry.BottomY;
        }

        /// <summary>Arrange halves for an opened specimen: top half hinged open beside the bottom half.</summary>
        public void ApplyOpenPose()
        {
            if (Visual == null || Visual.TopHalf == null) return;
            if (IsOpened)
            {
                Visual.SetCrystalsVisible(true);
                var geo = Visual.Geometry;
                float r = geo.MeanEquatorRadius;
                // lie the top half next to the bottom half, cavity up (rotated 180 around Z, shifted along -X)
                Visual.TopHalf.localRotation = Quaternion.Euler(0f, 0f, 180f);
                Visual.TopHalf.localPosition = new Vector3(-r * 2.15f, geo.TopY * 0f + (-geo.BottomY + geo.TopY) * 0f, 0f);
                // its lowest point must touch the same surface: after flipping, its pole (TopY) is lowest
                Visual.TopHalf.localPosition = new Vector3(-r * 2.15f, geo.BottomY + geo.TopY, 0f);
            }
            else
            {
                Visual.SetCrystalsVisible(false);
                Visual.TopHalf.localRotation = Quaternion.identity;
                Visual.TopHalf.localPosition = Vector3.zero;
            }
        }

        public void SetHighlight(bool on) => Visual?.SetHighlight(on ? 1f : 0f);

        public bool CanInteract(PlayerInteractor player) => !Locked && player.Held == null;

        public string GetPrompt(PlayerInteractor player) => IsOpened ? $"Pick up {Record.DisplayName}" : $"Pick up rock  {Geology.MassKg:F1} kg";

        public string GetHint(PlayerInteractor player) => null;

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
