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
        /// <summary>The loupe is raised: the held piece stays up in inspect pose without holding the inspect button.</summary>
        public bool LoupeActive { get; private set; }
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

            // inspect held object (held button, or the loupe keeping it raised)
            if (Held != null && (GameInput.InspectHeld || LoupeActive))
            {
                if (!Inspecting) BeginInspect();
                // tap it: a hollow shell rings, a solid nodule thuds. Knowledge, not an answer.
                if (GameInput.StrikePressed && !Held.IsOpened) TapHeld();
                Vector2 look = GameInput.Look;
                float k = GameInput.UsingGamepad ? 180f * Time.deltaTime : 0.35f;
                if (LoupeActive) k *= 0.6f;   // finer control under magnification
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
            _tapNote = "";
            InspectStarted?.Invoke(Held);
        }

        private string _tapNote = "";
        private float _tapKick;

        /// <summary>Knock on the shell: the sound (and a small nudge of the rock) tells hollow from solid, and big from small.</summary>
        private void TapHeld()
        {
            var g = Held.Geology;
            float hollow = Mathf.InverseLerp(0.15f, 0.85f, g.CavityFraction);
            int bank = hollow < 0.33f ? 0 : hollow < 0.66f ? 1 : 2;
            float pitch = Mathf.Lerp(1.25f, 0.75f, Mathf.InverseLerp(0.035f, 0.165f, g.Size));
            GeodeEmpire.Audio.WorkshopAudio.Play("knock_" + bank, Held.transform.position, 0.8f, pitch);
            Haptics.Pulse(0.15f, 0.05f, 0.05f);
            _tapKick = 1f;
            _tapNote = bank == 2 ? "Rings hollow" : bank == 1 ? "Dull ring: some cavity" : "Thuds solid";
            GeodeEmpire.Workshop.Tutorial.Notify("tapped");
            RefreshPrompt();
        }

        /// <summary>What the hands can tell about an unopened rock: size class, weight for its size, coating.</summary>
        public static string HandReading(SpecimenEntity e)
        {
            var g = e.Geology;
            float r = g.Size;
            float solidMass = 4f / 3f * Mathf.PI * r * r * r * g.Axes.x * g.Axes.y * g.Axes.z * 2650f * g.Family.ShellToughness;
            float ratio = g.MassKg / Mathf.Max(0.01f, solidMass);
            string weight = ratio < 0.55f ? "light for its size" : ratio < 0.8f ? "average weight" : "heavy for its size";
            float dirtLeft = e.Visual != null ? e.Visual.DirtRemaining : 0f;
            string dirt = dirtLeft > 0.35f ? "caked in clay" : dirtLeft > 0.08f ? "dusty" : "clean";
            string reading = $"{SpecimenGeology.SizeWord(g.SizeClass)} rock, {weight}, {dirt}";
            // a clean shell shows its seam, chips, staining and any mineral showing through
            if (dirtLeft <= 0.1f)
            {
                var notes = GeodeEmpire.Workshop.Preparation.ShellNotes(g);
                if (notes.Count > 0) reading += "  •  " + string.Join(", ", notes);
            }
            return reading;
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
                p = Held != null && !Held.IsOpened ? HandReading(Held) + (string.IsNullOrEmpty(_tapNote) ? "" : "  •  " + _tapNote) : "";
                h = LoupeActive ? $"{GameInput.Glyph("Look")} turn   {GameInput.Glyph("Loupe")} lower loupe"
                                : $"{GameInput.Glyph("Look")} rotate   {GameInput.Glyph("Inspect")} release" + (LoupeTool.Owned ? $"   {GameInput.Glyph("Loupe")} loupe" : "");
                if (Held != null && !Held.IsOpened) h += $"   {GameInput.Glyph("Strike")} tap";
            }
            else if (Target != null)
            {
                p = $"[{GameInput.Glyph("Interact")}] {Target.GetPrompt(this)}";
                h = Target.GetHint(this) ?? "";
            }
            if (Held != null && !Inspecting)
            {
                string held = $"Hold {GameInput.Glyph("Inspect")} to inspect   {GameInput.Glyph("Drop")} drop" + (LoupeTool.Owned ? $"   {GameInput.Glyph("Loupe")} loupe" : "");
                h = string.IsNullOrEmpty(h) ? held : h + "   " + held;
            }
            if (p != Prompt || h != Hint)
            {
                Prompt = p;
                Hint = h;
                PromptChanged?.Invoke();
            }
        }

        /// <summary>Called by the loupe: raised means the piece stays in inspect pose.</summary>
        public void SetLoupe(bool on)
        {
            LoupeActive = on;
            if (!on && !GameInput.InspectHeld && Inspecting) EndInspect();
            RefreshPrompt();
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
            if (Controller != null) Controller.CarryMassKg = e.Geology.MassKg;
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
            LoupeActive = false;
            if (Controller != null) Controller.CarryMassKg = 0f;
            if (Inspecting) EndInspect();
            e.SetCollidersEnabled(true);
            RefreshPrompt();
        }

        public void Drop()
        {
            if (Held == null) return;
            var e = Held;
            Held = null;
            LoupeActive = false;
            if (Controller != null) Controller.CarryMassKg = 0f;
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
            // under the loupe the piece comes up to the lens (just past it, so the magnified view is of the rock)
            Vector3 targetPos = anchor.position + (Inspecting ? anchor.forward * _inspectZoom : Vector3.zero);
            if (LoupeActive) targetPos += anchor.right * LoupeTool.HeldOffset.x + anchor.up * LoupeTool.HeldOffset.y + anchor.forward * LoupeTool.HeldOffset.z;
            // keep large rocks from clipping the camera; a heavy one is carried lower, against the body
            float pushBack = Mathf.Max(0f, Held.Radius - 0.06f) * (Inspecting ? 1.6f : 0.9f);
            targetPos += anchor.forward * pushBack;
            if (!Inspecting) targetPos += anchor.up * (-Mathf.Max(0f, Held.Radius - 0.07f) * 0.6f);
            _tapKick = Mathf.MoveTowards(_tapKick, 0f, dt * 8f);
            if (_tapKick > 0f) targetPos += anchor.forward * (-0.006f * Mathf.Sin(_tapKick * Mathf.PI));
            float k = 1f - Mathf.Exp(-dt * 16f);
            Held.transform.position = Vector3.Lerp(Held.transform.position, targetPos, k * _heldLerp + (1f - _heldLerp) * 0.5f);
            Held.transform.rotation = Quaternion.Slerp(Held.transform.rotation, targetRot, k);
        }
    }
}
