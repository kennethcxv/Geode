using System;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Interaction;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Player
{
    /// <summary>
    /// Looks for interactables under the crosshair, carries one specimen at a time, supports inspect-rotate,
    /// deliberate placement into zones, and dropping.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        public Camera Cam;
        public FirstPersonController Controller;
        public float Range = 2.6f;
        public LayerMask Mask = Physics.DefaultRaycastLayers;
        public Transform HoldAnchor;
        public Transform InspectAnchor;

        public SpecimenEntity Held { get; private set; }
        public IInteractable Target { get; private set; }
        public string Prompt { get; private set; } = "";
        public string Hint { get; private set; } = "";
        public bool Inspecting { get; private set; }
        /// <summary>Set by stations/UI to suspend free-roam interaction.</summary>
        public bool InputLocked;
        /// <summary>Frame until which interact presses are ignored (a station consumed the press).</summary>
        public int IgnoreInteractUntilFrame;

        public event Action PromptChanged;
        public event Action<SpecimenEntity> PickedUp;
        public event Action<SpecimenEntity> Dropped;
        public event Action<SpecimenEntity> InspectStarted;

        private Quaternion _inspectRot = Quaternion.identity;
        private float _inspectZoom;
        private IInteractable _lastHighlighted;
        private float _heldLerp;

        private void Awake()
        {
            if (Cam == null) Cam = GetComponentInChildren<Camera>();
            if (Controller == null) Controller = GetComponent<FirstPersonController>();
            gameObject.layer = 2; // Ignore Raycast: never hit our own capsule
            Mask = Physics.DefaultRaycastLayers;
            if (HoldAnchor == null)
            {
                HoldAnchor = new GameObject("HoldAnchor").transform;
                HoldAnchor.SetParent(Cam.transform, false);
                HoldAnchor.localPosition = new Vector3(0.30f, -0.24f, 0.62f);
            }
            if (InspectAnchor == null)
            {
                InspectAnchor = new GameObject("InspectAnchor").transform;
                InspectAnchor.SetParent(Cam.transform, false);
                InspectAnchor.localPosition = new Vector3(0f, -0.05f, 0.42f);
            }
        }

        private void Update()
        {
            if (InputLocked || !GameInput.GameplayEnabled)
            {
                SetTarget(null);
                if (Inspecting) EndInspect();
                return;
            }

            // inspect held object
            if (Held != null && GameInput.InspectHeld)
            {
                if (!Inspecting) BeginInspect();
                Vector2 look = GameInput.Look;
                float k = GameInput.UsingGamepad ? 180f * Time.deltaTime : 0.35f;
                _inspectRot = Quaternion.AngleAxis(-look.x * k, Vector3.up) * Quaternion.AngleAxis(look.y * k, Vector3.right) * _inspectRot;
                _inspectZoom = Mathf.Clamp(_inspectZoom + GameInput.Scroll.y * 0.0006f + GameInput.Rotate * Time.deltaTime * 0.2f, -0.12f, 0.16f);
            }
            else if (Inspecting)
            {
                EndInspect();
            }

            if (!Inspecting)
            {
                SetTarget(FindTarget());
                if (Target != null && GameInput.InteractPressed && Time.frameCount > IgnoreInteractUntilFrame && Target.CanInteract(this))
                {
                    Target.Interact(this);
                    RefreshPrompt();
                }
            }
            else SetTarget(null);

            if (Held != null && GameInput.DropPressed) Drop();
            // prompts are rebuilt when what they describe changes, and every few frames for text that ticks on its own
            bool changed = !ReferenceEquals(Target, _promptTarget) || Held != _promptHeld || Inspecting != _promptInspecting || GameInput.Scheme != _promptScheme;
            if (changed || (Time.frameCount + _promptPhase) % 8 == 0) RefreshPrompt();
        }

        private IInteractable _promptTarget;
        private SpecimenEntity _promptHeld;
        private bool _promptInspecting;
        private ControlScheme _promptScheme;
        private readonly int _promptPhase = 3;

        private readonly RaycastHit[] _hits = new RaycastHit[16];

        /// <summary>
        /// Nearest interactable under the crosshair. Thin solid geometry (shelf lips, tray walls) may sit just in
        /// front of a placement zone, so an interactable slightly behind the first solid hit still counts.
        /// </summary>
        private IInteractable FindTarget()
        {
            var ray = new Ray(Cam.transform.position, Cam.transform.forward);
            int n = Physics.RaycastNonAlloc(ray, _hits, Range, Mask, QueryTriggerInteraction.Collide);
            if (n == 0) return null;
            System.Array.Sort(_hits, 0, n, RaycastDistanceComparer.Instance);
            float firstSolid = float.MaxValue;
            IInteractable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var h = _hits[i];
                var inter = h.collider.GetComponentInParent<IInteractable>();
                if (inter is SpecimenEntity se && se == Held) inter = null;
                // something interactable that currently refuses interaction (an opened crate still holding rocks, a
                // full tray) is just geometry: it must not shadow the rock lying right behind its rim
                if (inter != null && !inter.CanInteract(this)) inter = null;
                if (inter != null)
                {
                    if (h.distance <= firstSolid + 0.4f && h.distance < bestDist) { best = inter; bestDist = h.distance; }
                }
                else if (!h.collider.isTrigger && h.distance < firstSolid)
                {
                    firstSolid = h.distance;
                }
                if (h.distance > firstSolid + 0.4f) break;
            }
            return best;
        }

        private sealed class RaycastDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly RaycastDistanceComparer Instance = new RaycastDistanceComparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }

        private void BeginInspect()
        {
            Inspecting = true;
            Controller.LookEnabled = false;
            Controller.MovementEnabled = false;
            _inspectRot = Quaternion.identity;
            _inspectZoom = 0f;
            InspectStarted?.Invoke(Held);
        }

        private void EndInspect()
        {
            Inspecting = false;
            Controller.LookEnabled = true;
            Controller.MovementEnabled = true;
        }

        private void SetTarget(IInteractable t)
        {
            if (t != null && !t.CanInteract(this)) t = null;
            if (ReferenceEquals(t, Target)) return;
            if (_lastHighlighted != null) { try { _lastHighlighted.SetHighlight(false); } catch { } }
            Target = t;
            _lastHighlighted = t;
            if (t != null) t.SetHighlight(true);
        }

        private void RefreshPrompt()
        {
            _promptTarget = Target; _promptHeld = Held; _promptInspecting = Inspecting; _promptScheme = GameInput.Scheme;
            string p = "", h = "";
            if (Inspecting)
            {
                p = "";
                h = $"{GameInput.Glyph("Look")} rotate   {GameInput.Glyph("Inspect")} release";
            }
            else if (Target != null)
            {
                p = $"[{GameInput.Glyph("Interact")}] {Target.GetPrompt(this)}";
                h = Target.GetHint(this) ?? "";
            }
            if (Held != null && !Inspecting)
            {
                string held = $"Hold {GameInput.Glyph("Inspect")} to inspect   {GameInput.Glyph("Drop")} drop";
                h = string.IsNullOrEmpty(h) ? held : h + "   " + held;
            }
            if (p != Prompt || h != Hint)
            {
                Prompt = p;
                Hint = h;
                PromptChanged?.Invoke();
            }
        }

        public void PickUp(SpecimenEntity e)
        {
            if (e == null || Held != null) return;
            if (e.Zone != null) e.Zone.Take(e);
            if (e.IsOpened && !e.Record.HasOpenPose) e.CommitOpenPose();   // leaving the bench: freeze the opened layout on the rock's own base plane
            Held = e;
            e.Locked = false;
            e.SetPhysics(false);
            e.SetCollidersEnabled(false);
            e.transform.SetParent(null, true);
            e.Record.Location = SpecimenLocation.Held;
            _heldLerp = 0f;
            _inspectRot = Quaternion.identity;
            PickedUp?.Invoke(e);
            GeodeEmpire.Audio.WorkshopAudio.Play("rock_pickup", e.transform.position, 0.6f);
            GeodeEmpire.Workshop.Tutorial.Notify(e.IsOpened ? "specimen_picked" : "rock_picked");
            RefreshPrompt();
        }

        /// <summary>Hand the held specimen to a zone (zone handles placement).</summary>
        public void ReleaseHeld()
        {
            if (Held == null) return;
            var e = Held;
            Held = null;
            if (Inspecting) EndInspect();
            e.SetCollidersEnabled(true);
            RefreshPrompt();
        }

        public void Drop()
        {
            if (Held == null) return;
            var e = Held;
            Held = null;
            if (Inspecting) EndInspect();
            e.transform.SetParent(null, true);
            e.SetCollidersEnabled(true);
            e.SetPhysics(true);
            e.Body.linearVelocity = Cam.transform.forward * 0.9f + Vector3.down * 0.3f;
            e.Body.angularVelocity = UnityEngine.Random.insideUnitSphere * 0.4f;
            e.Record.Location = SpecimenLocation.World;
            Dropped?.Invoke(e);
            RefreshPrompt();
        }

        private void LateUpdate()
        {
            if (Held == null) return;
            float dt = Time.deltaTime;
            _heldLerp = Mathf.Min(1f, _heldLerp + dt * 5f);
            var anchor = Inspecting ? InspectAnchor : HoldAnchor;
            Quaternion baseRot = Held.IsOpened ? Quaternion.Euler(-62f, 18f, 0f) : Quaternion.Euler(18f, 32f, 8f);
            Quaternion targetRot = anchor.rotation * (Inspecting ? _inspectRot * Quaternion.Euler(Held.IsOpened ? -70f : 10f, 0f, 0f) : baseRot);
            Vector3 targetPos = anchor.position + (Inspecting ? anchor.forward * _inspectZoom : Vector3.zero);
            // keep large rocks from clipping the camera
            float pushBack = Mathf.Max(0f, Held.Radius - 0.06f) * (Inspecting ? 1.6f : 0.9f);
            targetPos += anchor.forward * pushBack;
            float k = 1f - Mathf.Exp(-dt * 16f);
            Held.transform.position = Vector3.Lerp(Held.transform.position, targetPos, k * _heldLerp + (1f - _heldLerp) * 0.5f);
            Held.transform.rotation = Quaternion.Slerp(Held.transform.rotation, targetRot, k);
        }
    }
}
