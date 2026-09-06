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
        /// <summary>The key the prompt is asking for, drawn as its own cap; empty when the line is not a call to act.</summary>
        public string PromptKey { get; private set; } = "";
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
                // the prompt has to go with the target: without this the last world prompt hangs over a station view
                if (!string.IsNullOrEmpty(Prompt)) RefreshPrompt();
                if (Inspecting) EndInspect();
                return;
            }

            // inspect held object (held button, or the loupe keeping it raised)
            if (Held != null && (GameInput.InspectHeld || LoupeActive))
            {
                if (!Inspecting) BeginInspect();
                // tap it: a hollow shell rings, a solid nodule thuds. Knowledge, not an answer.
                if (GameInput.StrikePressed && !Held.IsOpened) TapHeld();
                // call it: what the hands and the ear say it is, written down before the rock is opened
                if (GameInput.DropPressed && !Held.IsOpened) CycleCall();
                TurnHeld();
                ReadSurface();
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

            if (Held != null && GameInput.DropPressed && !(Inspecting && !Held.IsOpened)) Drop();
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
            // what the ear actually gets (V5 §47): a thick shell muffles a cavity, clay deadens the whole rock, so the
            // tap can mislead; the four grades are dense thud, muted response, slight resonance, clear hollow ring
            float thick = Mathf.InverseLerp(0.18f, 0.30f, g.ShellThickness);
            float dirt = Held.Visual != null ? Mathf.Clamp01(Held.Visual.DirtRemaining) : 0f;
            float heard = hollow * (1f - 0.45f * thick) * (1f - 0.35f * dirt);
            int grade = heard < 0.2f ? 0 : heard < 0.45f ? 1 : heard < 0.7f ? 2 : 3;
            int bank = grade == 0 ? 0 : grade == 3 ? 2 : 1;
            float pitch = Mathf.Lerp(1.25f, 0.75f, Mathf.InverseLerp(0.035f, 0.165f, g.Size)) * (grade == 2 ? 1.06f : 1f);
            GeodeEmpire.Audio.WorkshopAudio.Play("knock_" + bank, Held.transform.position, grade == 0 ? 0.9f : 0.8f, pitch);
            Haptics.Pulse(grade == 0 ? 0.22f : 0.15f, 0.05f, 0.05f);
            _tapKick = 1f;
            _tapNote = grade switch { 0 => "Dense thud", 1 => "Muted response", 2 => "Slight resonance", _ => "Clear hollow ring" };
            if (dirt > 0.35f && grade < 3) _tapNote += " (through the clay)";
            GeodeEmpire.Workshop.Tutorial.Notify("tapped");
            RefreshPrompt();
        }

        /// <summary>The calls a player can make in the hand, in the order the drop key cycles them.</summary>
        private static readonly (bool hollow, int tier, string word)[] Calls =
        {
            (true, 0, "hollow, ordinary"), (true, 2, "hollow, a rare one"), (true, 3, "hollow, exceptional"), (false, -1, "solid nodule"),
        };

        private void CycleCall()
        {
            var r = Held.Record;
            int idx = -1;
            if (r.Predicted) for (int i = 0; i < Calls.Length; i++) if (Calls[i].hollow == r.PredictedHollow && Calls[i].tier == r.PredictedTier) { idx = i; break; }
            idx = (idx + 1) % (Calls.Length + 1);
            if (idx == Calls.Length) { r.Predicted = false; r.PredictedTier = -1; }
            else
            {
                var c = Calls[idx];
                if (!r.Predicted) { GameSession.Instance.State.Stats.PredictionsMade++; GameState.Log(r, "predicted", 0f, c.word); }
                r.Predicted = true; r.PredictedHollow = c.hollow; r.PredictedTier = c.tier;
            }
            GeodeEmpire.Audio.WorkshopAudio.Play("ui_click", Held.transform.position, 0.3f, 1.2f);
            GameSession.Instance.QueueSave("call");
            RefreshPrompt();
        }

        public static string CallWord(SpecimenRecord r)
        {
            if (r == null || !r.Predicted) return "";
            foreach (var c in Calls) if (c.hollow == r.PredictedHollow && c.tier == r.PredictedTier) return c.word;
            return r.PredictedHollow ? "hollow" : "solid";
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
            if (Economy.UpgradeCatalog.Has(GameSession.Instance != null ? GameSession.Instance.State : null, Economy.UpgradeCatalog.Calipers))
                reading += $"  •  {g.Size * 200f:F0} mm across";
            if (e.Record != null && !string.IsNullOrEmpty(e.Record.Locality)) reading += $"  •  from {e.Record.Locality}";
            // §5.5: what the shell says is what the player has actually looked at and read, not everything the
            // generator knows. Turning the rock over and going round it is the only way to fill this line in.
            var logged = Logged(e);
            if (logged.Count > 0)
            {
                var seen = new System.Collections.Generic.List<string>();
                foreach (var c in logged)
                {
                    string word = Specimens.SpecimenSurface.Describe(c.Kind);
                    if (!seen.Contains(word)) seen.Add(word);
                }
                reading += "  •  " + string.Join(", ", seen);
                string verdict = Specimens.SpecimenSurface.Reading(logged);
                if (!string.IsNullOrEmpty(verdict)) reading += "  •  " + verdict;
            }
            return reading;
        }

        // -------------------------------------------------------------------------------------
        // Turning it over (§6)
        // -------------------------------------------------------------------------------------
        private Vector3 _spin;              // residual angular velocity, screen axes, deg/s
        private float _rollHeld;

        /// <summary>
        /// Yaw, pitch and roll, accumulated about screen axes rather than the object's own, so it behaves like a
        /// rock in two hands: no axis is privileged, there is no pole to get stuck at, and turning it upside down
        /// does not invert the controls. Roll is the third axis §6.1 asks for; sprint is the fine-control modifier
        /// and the drop key resets it square.
        /// </summary>
        private void TurnHeld()
        {
            float dt = Time.deltaTime;
            Vector2 look = GameInput.Look;
            float k = GameInput.UsingGamepad ? 180f : 21f;
            if (LoupeActive) k *= 0.55f;                       // finer under magnification
            if (GameInput.SprintHeld) k *= 0.35f;              // §6.2 fine-control mode
            // the rotate axis rolls the piece about the view direction while inspecting; it zooms when it is not needed
            float roll = GameInput.Rotate;
            _rollHeld = Mathf.MoveTowards(_rollHeld, Mathf.Abs(roll) > 0.02f ? 1f : 0f, dt * 6f);
            float yaw = -look.x * k * (GameInput.UsingGamepad ? dt : 1f / 60f);
            float pitch = look.y * k * (GameInput.UsingGamepad ? dt : 1f / 60f);
            float rollDeg = -roll * 105f * dt * (GameInput.SprintHeld ? 0.35f : 1f);
            Apply(yaw, pitch, rollDeg);
            // gentle inertia: a flick keeps turning for a moment and settles, so the piece feels like it has mass
            if (Mathf.Abs(yaw) > 0.001f || Mathf.Abs(pitch) > 0.001f)
                _spin = Vector3.Lerp(_spin, new Vector3(pitch, yaw, 0f) / Mathf.Max(dt, 0.0001f) * 0.06f, 0.5f);
            else if (_spin.sqrMagnitude > 0.01f)
            {
                Apply(_spin.y * dt, _spin.x * dt, 0f);
                _spin = Vector3.MoveTowards(_spin, Vector3.zero, dt * (Held != null ? 220f : 400f));
            }
            // square it up again: an escape from any orientation, on both devices
            if (GameInput.InteractPressed) { _inspectRot = Quaternion.identity; _spin = Vector3.zero; }
            if (!LoupeActive && Mathf.Abs(roll) <= 0.02f)
                _inspectZoom = Mathf.Clamp(_inspectZoom + GameInput.Scroll.y * 0.0006f, -0.12f, 0.16f);
        }

        private void Apply(float yawDeg, float pitchDeg, float rollDeg)
        {
            if (Mathf.Abs(yawDeg) < 1e-5f && Mathf.Abs(pitchDeg) < 1e-5f && Mathf.Abs(rollDeg) < 1e-5f) return;
            var delta = Quaternion.AngleAxis(yawDeg, Vector3.up)
                      * Quaternion.AngleAxis(pitchDeg, Vector3.right)
                      * Quaternion.AngleAxis(rollDeg, Vector3.forward);
            _inspectRot = delta * _inspectRot;
        }

        // -------------------------------------------------------------------------------------
        // Reading the shell (§5.5): clues are found by looking at the place they are
        // -------------------------------------------------------------------------------------
        private System.Collections.Generic.List<Specimens.SpecimenSurface.Clue> _clues;
        private string _cluesFor = "";
        private int _lookingAt = -1;
        private float _dwell;
        /// <summary>What the eye is on right now, for the prompt.</summary>
        public string SurfaceNote { get; private set; } = "";

        private System.Collections.Generic.List<Specimens.SpecimenSurface.Clue> CluesFor(SpecimenEntity e)
        {
            if (e == null) return null;
            if (_clues == null || _cluesFor != e.Id)
            {
                _clues = Specimens.SpecimenSurface.Clues(e.Geology);
                _cluesFor = e.Id;
            }
            return _clues;
        }

        /// <summary>
        /// A ray through the middle of the view finds the patch of shell being looked at. Hold your eye on a patch
        /// carrying something and it registers; the loupe is what turns "there is a mark here" into a reading, and
        /// clay still on that patch hides it completely.
        /// </summary>
        private void ReadSurface()
        {
            SurfaceNote = "";
            var e = Held;
            if (e == null || e.IsOpened || e.Visual == null) { _lookingAt = -1; _dwell = 0f; return; }
            var clues = CluesFor(e);
            if (clues == null || clues.Count == 0) return;

            // the held piece sits right in front of the camera: intersect the view ray with its hull
            var origin = Cam.transform.position;
            var dir = Cam.transform.forward;
            var centre = e.transform.position;
            var toCentre = centre - origin;
            float along = Vector3.Dot(toCentre, dir);
            if (along <= 0f) { _lookingAt = -1; _dwell = 0f; return; }
            float radius = Mathf.Max(0.01f, e.Radius);
            var closest = origin + dir * along;
            float off = (closest - centre).magnitude;
            if (off > radius) { _lookingAt = -1; _dwell = 0f; return; }
            // the front surface point under the crosshair
            float depth = Mathf.Sqrt(Mathf.Max(0f, radius * radius - off * off));
            var surface = closest - dir * depth;
            int region = Specimens.SpecimenSurface.RegionOf(e.transform.InverseTransformPoint(surface));

            if (region != _lookingAt) { _lookingAt = region; _dwell = 0f; }
            _dwell += Time.deltaTime;

            var cond = e.Record.Condition;
            float clay = e.Visual.DirtAt(region);
            for (int i = 0; i < clues.Count; i++)
            {
                var c = clues[i];
                if (c.Region != region) continue;
                if (clay > 0.22f) { SurfaceNote = "clay covers this face"; continue; }
                var state = cond.ClueAt(i);
                if (state == Specimens.ClueState.Logged)
                {
                    if (string.IsNullOrEmpty(SurfaceNote)) SurfaceNote = Specimens.SpecimenSurface.Describe(c.Kind);
                    continue;
                }
                bool canLog = !c.NeedsLoupe || LoupeActive;
                if (_dwell > 0.3f && state == Specimens.ClueState.Undiscovered)
                {
                    cond.SetClue(i, Specimens.ClueState.Seen);
                    GeodeEmpire.Audio.WorkshopAudio.Play2D("ui_click", 0.18f, 1.5f);
                }
                if (_dwell > (LoupeActive ? 0.55f : 0.9f) && canLog && cond.ClueAt(i) < Specimens.ClueState.Logged)
                {
                    cond.SetClue(i, Specimens.ClueState.Logged);
                    Save.GameState.Log(e.Record, "observed", 0f, Specimens.SpecimenSurface.Describe(c.Kind));
                    GameSession.Instance?.QueueSave("observed");
                    GeodeEmpire.Audio.WorkshopAudio.Play2D("ui_click", 0.3f, 1.15f);
                    GeodeEmpire.Workshop.Tutorial.Notify("observed");
                }
                SurfaceNote = cond.ClueAt(i) >= Specimens.ClueState.Logged
                    ? Specimens.SpecimenSurface.Describe(c.Kind)
                    : Specimens.SpecimenSurface.Glimpse(c.Kind) + (canLog ? "  (keep looking)" : "  (too fine to read — use the loupe)");
            }
        }

        /// <summary>What has actually been read off this rock, in the order it was found.</summary>
        public static System.Collections.Generic.List<Specimens.SpecimenSurface.Clue> Logged(SpecimenEntity e)
        {
            var found = new System.Collections.Generic.List<Specimens.SpecimenSurface.Clue>();
            if (e == null || e.Record == null) return found;
            var clues = Specimens.SpecimenSurface.Clues(e.Geology);
            for (int i = 0; i < clues.Count; i++)
                if (e.Record.Condition.ClueAt(i) >= Specimens.ClueState.Logged) found.Add(clues[i]);
            return found;
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
            string p = "", h = "", key = "";
            if (Inspecting)
            {
                p = Held != null && !Held.IsOpened ? HandReading(Held) + (string.IsNullOrEmpty(_tapNote) ? "" : "  •  " + _tapNote) + (Held.Record.Predicted ? "  •  your call: " + CallWord(Held.Record) : "") : "";
                if (!string.IsNullOrEmpty(SurfaceNote))
                    p += "\n<size=17><color=#E8C88A>" + SurfaceNote + "</color></size>";
                h = LoupeActive ? $"{GameInput.Glyph("Look")} turn   {GameInput.Glyph("Rotate")} roll   {GameInput.Glyph("Loupe")} lower loupe"
                                : $"{GameInput.Glyph("Look")} turn   {GameInput.Glyph("Rotate")} roll   {GameInput.Glyph("Inspect")} release" + (LoupeTool.Owned ? $"   {GameInput.Glyph("Loupe")} loupe" : "");
                if (Held != null && !Held.IsOpened) h += $"   {GameInput.Glyph("Strike")} tap   {GameInput.Glyph("Drop")} call it   {GameInput.Glyph("Interact")} square it up";
            }
            else if (Target != null)
            {
                string tp = Target.GetPrompt(this);
                p = tp ?? "";
                if (!string.IsNullOrEmpty(p)) key = GameInput.Glyph("Interact");
                h = Target.GetHint(this) ?? "";
                // a piece on a shelf (the crosshair finds the slot, or the rock itself): its label sits under the prompt, small, where the eye already is
                string label = Target is SpecimenEntity se ? se.GetHint(this)
                    : Target is PlacementZone pz && (pz.Kind == ZoneKind.DisplaySlot || pz.Kind == ZoneKind.SaleSlot) && pz.Occupants.Count == 1 ? pz.Occupants[0].GetHint(this) : null;
                if (!string.IsNullOrEmpty(label)) { p += "\n<size=16><color=#CFC7B8>" + label + "</color></size>"; if (h == label) h = ""; }
            }
            if (Held != null && !Inspecting)
            {
                string held = $"Hold {GameInput.Glyph("Inspect")} to inspect   {GameInput.Glyph("Drop")} drop" + (LoupeTool.Owned ? $"   {GameInput.Glyph("Loupe")} loupe" : "");
                h = string.IsNullOrEmpty(h) ? held : h + "   " + held;
            }
            if (p != Prompt || h != Hint || key != PromptKey)
            {
                Prompt = p;
                Hint = h;
                PromptKey = key;
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
            bool recovering = e.Record.InRecovery;
            var recoveredFrom = e.Record.Location;
            if (e.Zone != null) e.Zone.Take(e);
            // A recovery parcel has no PlacementZone.Take to restore the opened layout from its packing pose.
            if (recovering && e.IsOpened && !e.IsPiece) e.ApplyPose(DisplayPose.Natural);
            if (e.IsOpened && !e.Record.HasOpenPose) e.CommitOpenPose();   // leaving the bench: freeze the opened layout on the rock's own base plane
            Held = e;
            e.Locked = false;
            e.SetPhysics(false);
            e.SetCollidersEnabled(false);
            e.transform.SetParent(null, true);
            e.Record.Location = SpecimenLocation.Held;
            e.Record.RecoveryCrateId = null;
            if (recovering)
            {
                // Match taking stock from its original fixture; packing preserves its designation until this pickup.
                if (recoveredFrom == SpecimenLocation.SaleSlot) e.Record.AskingPrice = 0f;
                var session = GameSession.Instance;
                if (session != null && session.State != null)
                {
                    if (recoveredFrom == SpecimenLocation.DisplaySlot)
                    {
                        session.State.Stats.SpecimensKept = session.State.DisplayedCount();
                        Workshop.DisplayCabinet.RecomputePrestige(session.State);
                    }
                    session.RaiseStateChanged();
                    session.QueueSave("recovery-picked");
                }
            }
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
