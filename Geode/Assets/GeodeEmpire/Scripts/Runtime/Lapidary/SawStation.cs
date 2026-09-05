using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;
using GeodeEmpire.VFX;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.Lapidary
{
    /// <summary>
    /// The 14-inch trim saw: the second way to open rock, operated by hand. The rock is clamped by its near half in a
    /// carriage vise that rides the rails on the operator's side of the blade (the jaws squeeze along the feed axis,
    /// so nothing on the carriage ever crosses the blade plane); it overhangs across the plane and the blade passes
    /// through it alone. Orient it (turn, tilt, slide across), open the coolant valve, commit (the jaw winds shut,
    /// the motor spins up), then feed the carriage by hand and read the load on the machine's meter. The rock has to
    /// pass under the arbor: a rock taller than the blade's pass height (arbor less the flange) cannot be fed through
    /// in any number of passes, so it is turned until it fits, or it waits for a bigger saw. The cut is committed to
    /// the save the moment the clamp closes: a reload resumes the same plane at the same depth. Two pieces come out,
    /// both tied to the parent rock.
    /// </summary>
    public sealed class SawStation : InteractableBehaviour
    {
        public enum Phase { Idle, Orient, Cutting, Done }

        // ---- the machine as an interactable: resume a committed cut left in the clamp -------------------------
        public override bool CanInteract(PlayerInteractor player)
        {
            if (Active || !Owned || player.Held != null || Clamp == null) return false;
            var occ = Clamp.First;
            return occ != null && occ.Record.CutCommitted;
        }

        public override string GetPrompt(PlayerInteractor player)
        {
            var occ = Clamp != null ? Clamp.First : null;
            return occ != null ? $"Resume the cut  ({Mathf.RoundToInt(occ.Record.CutProgress * 100f)}% through)" : "";
        }

        public override string GetHint(PlayerInteractor player) => "The clamp holds the rock exactly where it was";

        public override void Interact(PlayerInteractor player)
        {
            var occ = Clamp != null ? Clamp.First : null;
            if (occ != null && occ.Record.CutCommitted) Enter(occ);
        }

        public PlacementZone Clamp;
        public PlacementZone OutTray;
        public Transform Vise;          // sled + fixed jaw + screw; rides the rails along local X, sits at ViseZ
        public Transform Jaw;           // moving jaw, slides along the vise's X
        public Transform Wheel;         // vise handwheel, turns about X as the jaw travels
        public Transform Blade;         // spins about its local Z
        public Transform Needle;        // load meter needle, turns about its local Z
        public Transform Valve;         // coolant valve lever, quarter turn about its local Y
        public Transform Nozzle;        // where the coolant leaves
        public Transform CameraAnchor;
        public GameObject Teaser;       // the tarp, shown until the saw is bought
        public GameObject Machine;      // everything else, shown once bought
        // Stage 3: the 24-inch slab saw, a second set of parts in the same bay; the trim saw's set is hidden while it is in
        public GameObject LargeMachine;
        public Transform LargeVise, LargeJaw, LargeWheel, LargeBlade, LargeNeedle, LargeValve, LargeNozzle;
        public float LargeBladeRadius = 0.3f, LargeFlangeRadius = 0.045f;
        public bool UsingLarge { get; private set; }
        private GameObject _smallMachine; private Transform _smallVise, _smallJaw, _smallWheel, _smallBlade, _smallNeedle, _smallValve, _smallNozzle;
        private float _smallBladeRadius, _smallFlangeRadius, _smallBladeCenterY;
        public Light TaskLight;

        // machine geometry (station-local metres); see Tools/Blender/gen_props.py saw family
        public float BladeZ = 0.05f;
        public float BladeCenterY = 1.113f;
        public float BladeRadius = 0.178f;
        public float FlangeRadius = 0.032f;
        public float RailTopY = 0.915f;
        public float SledTopY = 0.02f;
        public float FixedJawX = -0.16f;    // vise-local X of the fixed pad face
        public float ViseZ = -0.056f;       // vise centre z: the sled's far edge sits 6 mm short of the blade plane
        public float JawGapOpen = 0.03f;
        public float ScrewLead = 0.004f;    // metres of jaw travel per wheel turn

        [NonSerialized] public float BaseFeed = 0.015f;          // m/s carriage speed at nominal feed: a medium rock is ~30 s
        [NonSerialized] public float FastFeedMult = 1.9f;
        [NonSerialized] public float YawStep = 5f, RollStep = 5f, OffsetStep = 0.003f;
        [NonSerialized] public float Kerf = 0.003f, ThinKerf = 0.0015f;
        [NonSerialized] public Vector3 CamLocalPos = new Vector3(0.44f, 1.42f, -0.52f);
        [NonSerialized] public Vector3 CamLookLocal = new Vector3(0.16f, 0.99f, 0.05f);

        public bool Active { get; private set; }
        public Phase State { get; private set; } = Phase.Idle;
        public SpecimenEntity Rock => _rock;
        public float Yaw => _yaw;
        public float Roll => _roll;
        public float Offset => _offset;
        public float Progress => _progress;
        public float Load => _load;
        public float Overload => Mathf.Max(0f, _load - 1f);
        public bool Feeding => _feeding;
        public bool Committed => State == Phase.Cutting;
        public bool CanRotate => _rock != null && !_rock.IsPiece;
        public string ResultNote { get; private set; } = "";
        public SpecimenEntity PieceA { get; private set; }
        public SpecimenEntity PieceB { get; private set; }
        public int ChipsThisCut { get; private set; }
        public float WearThisCut { get; private set; }
        public float SecondsThisCut { get; private set; }
        public float BladeWear => GameSession.Instance != null && GameSession.Instance.State != null ? GameSession.Instance.State.BladeWear : 0f;
        public bool BladeDull => BladeWear > 0.7f;
        public bool BladeSpent => BladeWear >= 0.999f;
        public bool Owned => GameSession.Instance != null && GameSession.Instance.State != null && UpgradeCatalog.Has(GameSession.Instance.State, UpgradeCatalog.TrimSaw);
        /// <summary>The thin-kerf profile is owned; which blade is on the arbor for this cut is <see cref="ThinForCut"/>.</summary>
        public bool ThinBladeOwned => GameSession.Instance != null && UpgradeCatalog.Has(GameSession.Instance.State, UpgradeCatalog.ThinBlade);
        public bool ThinForCut => _thinForCut;
        /// <summary>The flood pump upgrade: full flow instead of a drip once the valve is open.</summary>
        public bool FloodPump => GameSession.Instance != null && UpgradeCatalog.Has(GameSession.Instance.State, UpgradeCatalog.CoolantPump);
        public bool HeavyJaws => GameSession.Instance != null && UpgradeCatalog.Has(GameSession.Instance.State, UpgradeCatalog.SawClamp);
        /// <summary>The coolant valve: closed until the player opens it (each session), so a dry cut is a choice that shows.</summary>
        public bool CoolantOpen { get; private set; }
        public string CoolantWord => !CoolantOpen ? "closed (dry)" : FloodPump ? "flood" : "drip";
        /// <summary>How well the vise holds this rock, 0..1 (tall narrow rocks shift under a hard feed).</summary>
        public float Grip => _grip;
        /// <summary>The tallest rock the blade passes in one go: arbor height less the flange, above the sled.</summary>
        public float MaxPassHeight => BladeCenterY - FlangeRadius - (RailTopY + SledTopY) - 0.006f;
        public float RockHeight => _ext.size.y;
        /// <summary>In its current pose the rock passes under the arbor flange: a cut can be committed.</summary>
        public bool FitsUnderArbor => _rock != null && (_rock.IsPiece || RockHeight <= MaxPassHeight);
        public float FaceStep => _faceStep;

        [NonSerialized] public bool DevFeed;
        [NonSerialized] public bool DevFast;

        public event Action Entered, Exited, CommittedEvent, Finished;

        private SpecimenEntity _rock;
        private float _yaw, _roll, _offset;
        private float _progress, _load, _rpm, _bladeAngle;
        private bool _feeding, _fast, _thinForCut;
        private float _faceStep;
        private float _carriageX;
        private Bounds _ext;                 // rock hull bounds in the station frame for the current rotation
        private Quaternion _extRot = Quaternion.identity; private bool _extValid;
        private float _grip = 1f, _shiftBuild;
        private Vector3 _viseHome, _jawHome;
        private float _jawT, _wheelAngle, _needleAngle, _valveT;
        private FirstPersonController _controller;
        private PlayerInteractor _player;
        private Camera _cam;
        private AudioSource _motorLoop, _grindLoop, _coolantLoop;
        private float _repeatTimer, _chipTimer, _slurryTimer, _streamTimer;
        private float _lastFeedInputTime;
        private System.Random _cutRng;

        // ------------------------------------------------------------------------------------
        protected override void Awake()
        {
            base.Awake();
            if (Clamp != null)
            {
                Clamp.Placed += OnPlaced;
                Clamp.Taken += OnTaken;
                Clamp.ExtraRefusal = Refusal;
                // looking anywhere at the clamp with a committed cut in it offers to resume
                Clamp.ResumePrompt = occ => occ != null && occ.Record.CutCommitted && !Active ? $"Resume the cut  ({Mathf.RoundToInt(occ.Record.CutProgress * 100f)}% through)" : null;
                Clamp.ResumeAction = occ => { if (occ != null && occ.Record.CutCommitted && !Active) Enter(occ); };
            }
            if (Vise != null) _viseHome = Vise.localPosition;
            if (Jaw != null) _jawHome = Jaw.localPosition;
        }

        private void Start()
        {
            _controller = FindAnyObjectByType<FirstPersonController>();
            _player = FindAnyObjectByType<PlayerInteractor>();
            _cam = _controller != null ? _controller.Camera : Camera.main;
            var session = GameSession.Instance;
            if (session != null)
            {
                session.Loaded += RefreshOwned;
                session.StateChanged += RefreshOwned;
                if (session.State != null) RefreshOwned();
            }
        }

        private void OnDestroy()
        {
            var session = GameSession.Instance;
            if (session != null) { session.Loaded -= RefreshOwned; session.StateChanged -= RefreshOwned; }
            WorkshopAudio.StopLoop(_motorLoop); WorkshopAudio.StopLoop(_grindLoop); WorkshopAudio.StopLoop(_coolantLoop);
        }

        /// <summary>Tarp until bought; the machine afterwards. The clamp only takes rock once the saw exists.</summary>
        public void RefreshOwned()
        {
            bool owned = Owned && GeodeEmpire.Build.PlaceableFixture.SitedFor(this);   // bought is not installed: the player sites it in build mode
            if (Teaser != null) Teaser.SetActive(!owned);
            // which machine is in the bay: the slab saw once Stage 3 is bought
            if (_smallMachine == null) { _smallMachine = Machine; _smallVise = Vise; _smallJaw = Jaw; _smallWheel = Wheel; _smallBlade = Blade; _smallNeedle = Needle; _smallValve = Valve; _smallNozzle = Nozzle; _smallBladeRadius = BladeRadius; _smallFlangeRadius = FlangeRadius; _smallBladeCenterY = BladeCenterY; }
            bool large = LargeMachine != null && WorkshopExpansion.Stage3Active && !Active;
            if (large != UsingLarge || Machine == null)
            {
                UsingLarge = large;
                Machine = large ? LargeMachine : _smallMachine;
                Vise = large ? LargeVise : _smallVise; Jaw = large ? LargeJaw : _smallJaw; Wheel = large ? LargeWheel : _smallWheel; Blade = large ? LargeBlade : _smallBlade;
                Needle = large ? LargeNeedle : _smallNeedle; Valve = large ? LargeValve : _smallValve; Nozzle = large ? LargeNozzle : _smallNozzle;
                BladeRadius = large ? LargeBladeRadius : _smallBladeRadius; FlangeRadius = large ? LargeFlangeRadius : _smallFlangeRadius;
                BladeCenterY = RailTopY + SledTopY + BladeRadius;
                if (Vise != null) _viseHome = Vise.localPosition;
                if (Jaw != null) _jawHome = Jaw.localPosition;
            }
            if (_smallMachine != null) _smallMachine.SetActive(owned && !UsingLarge);
            if (LargeMachine != null) LargeMachine.SetActive(owned && UsingLarge);
            if (Clamp != null) Clamp.Locked = !owned;
            if (OutTray != null) OutTray.Locked = !owned;
            // a rock with a committed cut is held by the clamp until the cut is finished: parked in the vise where
            // the blade left it, not floating at the zone anchor
            var occ = Clamp != null ? Clamp.First : null;
            if (occ != null && occ.Record.CutCommitted && !Active)
            {
                occ.Locked = true; occ.SetStaticCollidable();
                var rec = occ.Record;
                _rock = occ;
                LoadCommitted(rec);
                _carriageX = Mathf.Lerp(CarriageStart, CarriageEnd, Mathf.Clamp01(rec.CutProgress));
                var keep = State; State = Phase.Cutting;
                _jawT = 1f;
                PoseRock();
                State = keep;
                _rock = null;
                occ.Visual.SetCutPreview(rec.CutNormal, rec.CutHeight, Vector3.right, -10f, 0.6f);
            }
        }

        private string Refusal(SpecimenEntity e)
        {
            if (!Owned) return UpgradeCatalog.Has(GameSession.Instance?.State, UpgradeCatalog.TrimSaw)
                ? $"The Trim Saw is still crated in the receiving bay: press {GameInput.Glyph("Build")} to site it"
                : "Buy the Trim Saw on the tablet";
            if (e.IsOpened && !e.IsPiece) return "Split halves cannot be clamped: the saw takes whole rough or sawn pieces";
            if (e.IsPiece && e.Record.Piece.IsSlab && e.Record.Piece.Thickness < 0.012f) return "Too thin to cut again";
            if (!e.IsPiece && LowestHeightOverPoses(e) > MaxPassHeight) return UsingLarge ? "Too tall even for the slab saw's arbor" : "Too tall to pass under the arbor whichever way it lies: slab-saw work (Stage 3)";
            if (BladeSpent) return "The blade is worn out: fit a new one from the tablet";
            return null;
        }

        /// <summary>The lowest hull height the rock can be turned to (a flattened rock lying flat), over a few tilts.</summary>
        public static float LowestHeightOverPoses(SpecimenEntity e)
        {
            float best = float.MaxValue;
            for (int r = -90; r <= 90; r += 30)
                for (int y = 0; y < 180; y += 45)
                {
                    e.HullBoundsFor(Quaternion.AngleAxis(y, Vector3.up) * Quaternion.AngleAxis(r, Vector3.right), out var mn, out var mx);
                    best = Mathf.Min(best, mx.y - mn.y);
                }
            return best;
        }

        private void OnPlaced(PlacementZone zone, SpecimenEntity e)
        {
            WorkshopAudio.Play("rock_place", e.transform.position, 0.8f);
            Enter(e);
        }

        private void OnTaken(PlacementZone zone, SpecimenEntity e)
        {
            if (_rock == e && Active) Exit();
        }

        /// <summary>Re-enter for a rock left in the clamp (reload, or after stepping away).</summary>
        public void Resume()
        {
            var e = Clamp != null ? Clamp.First : null;
            if (e != null) Enter(e);
        }

        private void LoadCommitted(SpecimenRecord rec)
        {
            _yaw = rec.CutYaw; _roll = rec.CutRoll; _offset = rec.CutOffset; _progress = Mathf.Clamp01(rec.CutProgress);
            _faceStep = rec.CutFaceStep;
            _thinForCut = rec.CutThin;
            _kerfNormal = rec.CutNormal; _kerfHeight = rec.CutHeight;
            _extValid = false;
        }

        // ------------------------------------------------------------------------------------
        public void Enter(SpecimenEntity e)
        {
            if (Active || e == null) return;
            var session = GameSession.Instance;
            _enterFrame = Time.frameCount;
            _rock = e;
            _rock.Locked = true;
            _rock.SetStaticCollidable();
            Active = true;
            State = Phase.Orient;
            ResultNote = "";
            PieceA = PieceB = null;
            ChipsThisCut = 0; WearThisCut = 0f; SecondsThisCut = 0f;
            _progress = 0f; _load = 0f; _feeding = false; _shiftBuild = 0f;
            _extValid = false;
            var rec = e.Record;
            if (rec.CutCommitted)
            {
                // resume the committed cut exactly where it stopped
                LoadCommitted(rec);
                _carriageX = Mathf.Lerp(CarriageStart, CarriageEnd, _progress);
                State = Phase.Cutting; _jawT = 1f; StartMotor();
            }
            else
            {
                _yaw = 0f; _roll = 0f; _offset = 0f; _faceStep = 0f;
                _thinForCut = ThinBladeOwned;
                _jawT = 0f;
                if (e.IsPiece) { _roll = 0f; }
                _carriageX = CarriageStart;
            }
            _cutRng = new System.Random((int)(rec.Seed & 0x7FFFFFFF) ^ rec.CutIndex * 7919);
            UpdateGrip();
            PoseRock();
            FrameCamera();
            if (_controller != null) _controller.EnterStationView(CameraAnchor);
            if (_player != null) _player.InputLocked = true;
            if (TaskLight != null) TaskLight.enabled = true;
            UpdatePreview();
            Tutorial.Notify("rock_in_saw");
            Entered?.Invoke();
        }

        public void Exit()
        {
            if (!Active) return;
            _feeding = false; DevFeed = false;
            CursorController.MarkInputConsumed();
            Active = false;
            if (_controller != null) _controller.ExitStationView();
            if (_player != null) _player.InputLocked = false;
            StopMotor();
            bool committed = State == Phase.Cutting;
            if (_rock != null)
            {
                _rock.Locked = committed;   // a committed cut keeps the rock in the clamp
                if (!committed) _rock.Visual.SetCutPreview(Vector3.up, 0f, Vector3.right, -10f, 0f);
            }
            if (!committed) { State = Phase.Idle; OpenJaw(); }
            _rock = null;
            Exited?.Invoke();
            GameSession.Instance?.QueueSave("saw-exit");
        }

        // ---- geometry -----------------------------------------------------------------------
        private float HalfChordAt(float heightAboveSled)
        {
            // the blade circle centred BladeRadius above the sled: half its width at a given height
            float dy = BladeRadius - Mathf.Clamp(heightAboveSled, 0f, 2f * BladeRadius);
            return Mathf.Sqrt(Mathf.Max(0f, BladeRadius * BladeRadius - dy * dy));
        }

        // the rock's leading top corner meets the blade's rim first; the cut is through once its trailing edge has
        // passed the blade's centre line (the rim reaches the sled only there), so the carriage need not travel further
        private float CarriageStart => HalfChordAt(Mathf.Min(RockHeight, 2f * BladeRadius)) - _ext.min.x + 0.025f;
        private float CarriageEnd => -_ext.max.x - 0.015f;
        private float Travel => CarriageStart - CarriageEnd;

        /// <summary>Rock rotation relative to the station: yaw about the vertical, roll about the feed axis. A piece stands on edge, its cut face parallel to the blade.</summary>
        private Quaternion RockLocalRotation()
        {
            if (_rock != null && _rock.IsPiece)
                return Quaternion.AngleAxis(_yaw, Vector3.forward) * Quaternion.FromToRotation(Vector3.up, Vector3.back);
            return Quaternion.AngleAxis(_yaw, Vector3.up) * Quaternion.AngleAxis(_roll, Vector3.right);
        }

        private void EnsureExtents()
        {
            var R = RockLocalRotation();
            if (_extValid && _extRot == R) return;
            _rock.HullBoundsFor(R, out var min, out var max);
            _ext = new Bounds(); _ext.SetMinMax(min, max);
            _extRot = R; _extValid = true;
        }

        private Vector3 RockLocalCenter()
        {
            EnsureExtents();
            return new Vector3(_carriageX, RailTopY + SledTopY - _ext.min.y, BladeZ + _offset);
        }

        /// <summary>The cut plane in the rock's own frame: normal and height.</summary>
        public void PlanInRockFrame(out Vector3 normal, out float height)
        {
            var R = RockLocalRotation();
            normal = (Quaternion.Inverse(R) * Vector3.forward).normalized;
            height = -_offset;
        }

        /// <summary>Feed axis in the rock frame (station +X).</summary>
        private Vector3 FeedAxisInRockFrame() => (Quaternion.Inverse(RockLocalRotation()) * Vector3.right).normalized;
        private Vector3 UpInRockFrame() => (Quaternion.Inverse(RockLocalRotation()) * Vector3.up).normalized;

        private void UpdateGrip()
        {
            if (_rock == null) { _grip = 1f; return; }
            EnsureExtents();
            if (_rock.IsPiece) { _grip = 1f; return; }
            float halfX = _ext.size.x * 0.5f, h = _ext.size.y;
            _grip = Mathf.Clamp01(halfX / Mathf.Max(0.02f, 0.6f * h));
            if (HeavyJaws) _grip = Mathf.Max(_grip, 0.9f);
        }

        private void PoseRock()
        {
            if (_rock == null) return;
            var R = RockLocalRotation();
            var center = RockLocalCenter();
            _rock.SetPose(transform.TransformPoint(center), transform.rotation * R);
            // the vise under the rock's near half: its fixed pad meets the rock's leading (-X) extreme; the moving
            // jaw winds in from the trailing side and closes onto the other extreme
            if (Vise != null) Vise.localPosition = new Vector3(_carriageX + _ext.min.x - FixedJawX, RailTopY, ViseZ);
            if (Jaw != null)
            {
                float gap = Mathf.Lerp(JawGapOpen, 0f, Mathf.SmoothStep(0f, 1f, _jawT));
                Jaw.localPosition = new Vector3(FixedJawX + _ext.size.x + gap, _jawHome.y, _jawHome.z);
            }
            if (Wheel != null) Wheel.localRotation = Quaternion.AngleAxis(_wheelAngle, Vector3.right);
        }

        private void OpenJaw()
        {
            _jawT = 0f;
            if (Jaw != null) Jaw.localPosition = _jawHome;
            if (Vise != null) Vise.localPosition = _viseHome;
        }

        private void FrameCamera()
        {
            if (CameraAnchor == null) return;
            Vector3 pos = transform.TransformPoint(CamLocalPos);
            Vector3 look = transform.TransformPoint(CamLookLocal + new Vector3(0f, Mathf.Max(0f, RockHeight * 0.5f - 0.06f) * 0.5f, 0f));
            CameraAnchor.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
        }

        /// <summary>The blade's leading rim relative to the rock centre, along the feed axis, in rock units.</summary>
        private float Reach => HalfChordAt(RockHeight * 0.5f) - _carriageX;

        private void UpdatePreview()
        {
            if (_rock == null || _rock.Visual == null) return;
            PlanInRockFrame(out var n, out float h);
            var f = FeedAxisInRockFrame();
            var up = UpInRockFrame();
            EnsureExtents();
            bool cutting = State == Phase.Cutting;
            var vis = _rock.Visual;
            if (State == Phase.Orient)
            {
                vis.SetCutPreview(n, h, f, -10f, 1f);
                vis.SetCutMasks(new Vector4(up.x, up.y, up.z, 100f), new Vector4(0f, 1f, 0f, -100f));
            }
            else
            {
                vis.SetCutPreview(_kerfNormal, _kerfHeight, f, cutting ? Reach : 100f, 1f);
                vis.SetCutMasks(new Vector4(up.x, up.y, up.z, 100f), new Vector4(0f, 1f, 0f, -100f));
            }
        }

        private Vector3 _kerfNormal = Vector3.up; private float _kerfHeight;

        /// <summary>What the player is told before committing: seconds and blade wear for this plan.</summary>
        public void Estimate(out float seconds, out float wear, out float cost)
        {
            EnsureExtents();
            float feed = BaseFeed * (BladeDull ? 0.65f : 1f);
            seconds = Travel / feed;
            float tough = _rock != null ? _rock.Geology.Family.ShellToughness : 1f;
            float section = Mathf.Max(0.02f, RockHeight) / 0.13f;
            wear = 0.045f * tough * section * (_thinForCut ? 1.25f : 1f) * CoolantWearMult;
            cost = wear * UpgradeCatalog.Get(UpgradeCatalog.SawBlade).Price;
        }

        private float CoolantWearMult => !CoolantOpen ? 2f : FloodPump ? 0.8f : 1f;
        private float CoolantChipMult => !CoolantOpen ? 1.6f : FloodPump ? 0.5f : 1f;
        private float CoolantLoadMult => !CoolantOpen ? 1.2f : FloodPump ? 0.7f : 1f;

        /// <summary>Dev/test: set the plan directly.</summary>
        public void SetPlan(float yaw, float roll, float offset)
        {
            _yaw = yaw; _roll = roll; _extValid = false;
            EnsureExtents();
            _offset = ClampOffset(offset);
            if (_rock != null && _rock.IsPiece) ClampOffsetToPiece();
            UpdateGrip(); PoseRock(); UpdatePreview();
        }

        /// <summary>Dev/test: the valve, as the player sets it.</summary>
        public void SetCoolant(bool open) { if (CoolantOpen != open) ToggleCoolant(); }

        private float ClampOffset(float o)
        {
            EnsureExtents();
            // the rock's near half must stay in the jaws: at least a couple of centimetres of it on the vise side
            float nearLimit = Mathf.Max(0.005f, -_ext.min.z - 0.02f);
            float farLimit = Mathf.Max(0.005f, _ext.max.z - 0.02f);
            return Mathf.Clamp(o, -farLimit, nearLimit);
        }

        private void ClampOffsetToPiece()
        {
            // a piece is cut parallel to its faces: the plane must fall inside it
            var p = _rock.Record.Piece;
            float lo = p.HasLo ? p.Lo : -_ext.extents.z, hi = p.HasHi ? p.Hi : _ext.extents.z;
            float half = Mathf.Min(_ext.extents.z * 0.85f, (hi - lo) * 0.5f - 0.005f);
            _offset = Mathf.Clamp(_offset, -half, half);
        }

        // ---- coolant ------------------------------------------------------------------------
        public void ToggleCoolant()
        {
            CoolantOpen = !CoolantOpen;
            WorkshopAudio.Play("ui_click", transform.TransformPoint(0.42f, 1.18f, 0.24f), 0.35f, CoolantOpen ? 0.9f : 0.75f);
            if (CoolantOpen && _rpm > 0.3f) WorkshopAudio.Play("splash", NozzlePoint, 0.3f, 1.3f);
            Haptics.Pulse(0.1f, 0.05f, 0.05f);
        }

        private Vector3 NozzlePoint => Nozzle != null ? Nozzle.position : transform.TransformPoint(0f, BladeCenterY + BladeRadius + 0.1f, BladeZ);
        private Vector3 ContactPoint => transform.TransformPoint(new Vector3(HalfChordAt(RockHeight * 0.5f) * 0.9f, RockLocalCenter().y - _ext.extents.y * 0.2f, BladeZ));

        // ------------------------------------------------------------------------------------
        private void Update()
        {
            float dt = Time.deltaTime;
            if (Blade != null && _rpm > 0.01f)
            {
                _bladeAngle += dt * 360f * 18f * _rpm;
                Blade.localRotation = Quaternion.AngleAxis(_bladeAngle, Vector3.forward);
            }
            // the valve lever and the meter needle follow the machine state whether or not anyone is at it
            _valveT = Mathf.MoveTowards(_valveT, CoolantOpen ? 1f : 0f, dt * 4f);
            if (Valve != null) Valve.localRotation = Quaternion.AngleAxis(-90f * _valveT, Vector3.up);
            float wantNeedle = Mathf.Lerp(-55f, 55f, Mathf.Clamp01(_load / 1.4f)) + (_rpm > 0.1f ? Mathf.Sin(Time.time * 37f) * 1.5f * Mathf.Clamp01(_load) : 0f);
            _needleAngle = Mathf.Lerp(_needleAngle, wantNeedle, 1f - Mathf.Exp(-dt * 9f));
            if (Needle != null) Needle.localRotation = Quaternion.AngleAxis(_needleAngle, Vector3.forward);
            if (_rpm > 0.05f)
            {
                // coolant at the nozzle and on the blade whenever the motor runs with the valve open
                _streamTimer -= dt;
                if (CoolantOpen && _streamTimer <= 0f) { _streamTimer = FloodPump ? 0.05f : 0.11f; EffectsFactory.Instance?.CoolantStream(NozzlePoint, FloodPump ? 1f : 0.5f); }
                WorkshopAudio.SetLoop(_coolantLoop, CoolantOpen ? (FloodPump ? 0.32f : 0.16f) * _rpm : 0f, FloodPump ? 1f : 1.25f);
                Haptics.Pulse(0.03f * _rpm, 0f, dt);   // the motor's idle rumble in the pad
            }
            if (!Active) return;
            if (_rock == null && State != Phase.Done) return;
            if (GameInput.DropPressed && State != Phase.Done) ToggleCoolant();
            switch (State)
            {
                case Phase.Orient: UpdateOrient(dt); break;
                case Phase.Cutting: UpdateCutting(dt); break;
                case Phase.Done:
                    if (GameInput.InteractPressed && PieceA != null)
                    {
                        var piece = PieceA;
                        _player.IgnoreInteractUntilFrame = Time.frameCount + 1;
                        Exit();
                        if (piece.Zone != null) piece.Zone.Take(piece);
                        _player.PickUp(piece);
                        WorkshopAudio.Play("rock_pickup", piece.transform.position, 0.7f);
                    }
                    else if (GameInput.BackPressed) Exit();
                    break;
            }
        }

        private bool OrientInputs(float dt, bool allowRotate)
        {
            bool changed = false;
            _repeatTimer -= dt;
            // yaw: Q/E or the bumpers; roll: mouse Y / right stick Y; across: A/D or left stick X. Discrete steps, repeat while held.
            float rot = GameInput.Rotate;
            Vector2 move = GameInput.Move;
            Vector2 look = GameInput.Look;
            float rollIn = GameInput.UsingGamepad ? look.y : (Mathf.Abs(look.y) > 2.5f ? Mathf.Sign(look.y) : 0f);
            bool any = (allowRotate && (Mathf.Abs(rot) > 0.3f || Mathf.Abs(rollIn) > 0.3f)) || Mathf.Abs(move.x) > 0.3f;
            if (any && _repeatTimer <= 0f)
            {
                _repeatTimer = GameInput.UsingGamepad ? 0.14f : 0.09f;
                if (allowRotate && Mathf.Abs(rot) > 0.3f) { _yaw += Mathf.Sign(rot) * YawStep; _extValid = false; changed = true; }
                if (allowRotate && Mathf.Abs(rollIn) > 0.3f && CanRotate) { _roll = Mathf.Clamp(_roll + Mathf.Sign(rollIn) * RollStep, -90f, 90f); _extValid = false; changed = true; }
                if (Mathf.Abs(move.x) > 0.3f)
                {
                    _offset = ClampOffset(_offset + Mathf.Sign(move.x) * OffsetStep);
                    if (_rock.IsPiece) ClampOffsetToPiece();
                    changed = true;
                }
                if (changed) WorkshopAudio.Play("ui_click", _rock.transform.position, 0.15f, 1.4f);
            }
            if (!any) _repeatTimer = 0f;
            return changed;
        }

        private void UpdateOrient(float dt)
        {
            if (GameInput.BackPressed) { Exit(); return; }
            bool changed = OrientInputs(dt, true);
            if (changed) { EnsureExtents(); _offset = ClampOffset(_offset); UpdateGrip(); PoseRock(); UpdatePreview(); }
            // the blade profile for this cut, when there is a choice
            if (GameInput.LoupePressed && ThinBladeOwned) { _thinForCut = !_thinForCut; WorkshopAudio.Play("clamp", Blade != null ? Blade.position : transform.position, 0.4f, 1.3f); }
            // the press that clamped the rock must not also commit the cut; a rock too tall in this pose does not commit
            if (GameInput.InteractPressed && Time.frameCount > _enterFrame + 1)
            {
                if (FitsUnderArbor) Commit();
                else { WorkshopAudio.Play("ui_error", _rock.transform.position, 0.4f); Haptics.Pulse(0.2f, 0.1f, 0.06f); }
            }
        }

        private int _enterFrame;

        /// <summary>The clamp closes and the cut is committed to the save.</summary>
        public void Commit()
        {
            if (!Active || State != Phase.Orient || _rock == null || !FitsUnderArbor) return;
            var session = GameSession.Instance;
            var rec = _rock.Record;
            EnsureExtents();
            PlanInRockFrame(out var n, out float h);
            _kerfNormal = n; _kerfHeight = h;
            rec.CutCommitted = true;
            rec.CutNormal = n; rec.CutHeight = h; rec.CutProgress = 0f;
            rec.CutYaw = _yaw; rec.CutRoll = _roll; rec.CutOffset = _offset;
            rec.CutThin = _thinForCut; rec.CutFaceStep = 0f;
            _faceStep = 0f;
            rec.ProcessingStarted = true;
            State = Phase.Cutting;
            _progress = 0f; _carriageX = CarriageStart;
            StartCoroutine(CloseJaw());
            WorkshopAudio.Play("clamp", _rock.transform.position, 0.9f);
            Haptics.Pulse(0.3f, 0.2f, 0.08f);
            StartMotor();
            UpdatePreview();
            session.FlushSave("saw-commit");
            CommittedEvent?.Invoke();
        }

        private IEnumerator CloseJaw()
        {
            float t = 0f;
            while (t < 1f && State == Phase.Cutting)
            {
                t += Time.deltaTime / 0.7f;
                float prev = _jawT;
                _jawT = Mathf.Clamp01(t);
                _wheelAngle += (JawGapOpen * (_jawT - prev)) / ScrewLead * 360f;
                PoseRock();
                yield return null;
            }
            _jawT = 1f; PoseRock();
        }

        private IEnumerator ReleaseJaw()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.5f;
                float prev = _jawT;
                _jawT = 1f - Mathf.Clamp01(t);
                _wheelAngle -= (JawGapOpen * (prev - _jawT)) / ScrewLead * 360f;
                if (Jaw != null && _rock != null) PoseRock();
                yield return null;
            }
            _jawT = 0f;
        }

        private void StartMotor()
        {
            _rpm = 0f;
            if (_motorLoop == null) _motorLoop = WorkshopAudio.StartLoop("saw_motor", transform.TransformPoint(new Vector3(-0.6f, 1.25f, 0.28f)), 0.001f, 0.5f);
            if (_grindLoop == null) _grindLoop = WorkshopAudio.StartLoop("saw_grind", transform.TransformPoint(new Vector3(0f, BladeCenterY - BladeRadius * 0.5f, BladeZ)), 0.001f, 1f);
            if (_coolantLoop == null) _coolantLoop = WorkshopAudio.StartLoop("coolant_hiss", NozzlePoint, 0.001f, 1f);
            if (TaskLight != null) TaskLight.enabled = true;
        }

        private void StopMotor()
        {
            WorkshopAudio.StopLoop(_motorLoop); _motorLoop = null;
            WorkshopAudio.StopLoop(_grindLoop); _grindLoop = null;
            WorkshopAudio.StopLoop(_coolantLoop); _coolantLoop = null;
            _rpm = 0f;
        }

        /// <summary>
        /// How much stone the blade line meets right now: the vertical line through the rock at the blade's leading
        /// edge, less the cavity lobes it crosses. A hollow geode resists at the shell, drops through the cavity and
        /// resists again on the far shell, the way the pieces will show.
        /// </summary>
        private float MaterialFraction(float reach)
        {
            var g = _rock.Geology;
            EnsureExtents();
            float H = Mathf.Max(0.02f, RockHeight);
            if (g.LobeCenters == null || g.LobeRadii == null || g.LobeCenters.Length == 0 || g.Cavity == CavityArchetype.Nodule) return 1f;
            PlanInRockFrame(out var n, out float h);
            var f = FeedAxisInRockFrame();
            var up = UpInRockFrame();
            Vector3 basePt = n * h + f * reach;
            float cavity = 0f;
            for (int i = 0; i < g.LobeCenters.Length && i < g.LobeRadii.Length; i++)
            {
                Vector3 d = g.LobeCenters[i] - basePt;
                float along = Vector3.Dot(d, up);
                float perp = (d - up * along).magnitude;
                float rr = g.LobeRadii[i];
                if (perp < rr) cavity += 2f * Mathf.Sqrt(rr * rr - perp * perp);
            }
            return Mathf.Clamp(1f - cavity / H, 0.06f, 1f);
        }

        private void UpdateCutting(float dt)
        {
            var session = GameSession.Instance;
            var st = session.State;
            var rec = _rock.Record;
            // spin up
            _rpm = Mathf.MoveTowards(_rpm, 1f - 0.3f * Mathf.Clamp01(_load) - 0.35f * Overload, dt * (_rpm < 0.9f ? 0.9f : 2.5f));
            bool wantFeed = DevFeed || GameInput.StrikeHeld;
            _fast = DevFast || GameInput.SprintHeld;
            _feeding = wantFeed && _rpm > 0.55f && _jawT >= 0.999f;
            float feed = 0f;
            if (_feeding)
            {
                feed = BaseFeed * (_fast ? FastFeedMult : 1f) * (BladeDull ? 0.65f : 1f);
                _lastFeedInputTime = Time.time;
            }
            // the section the blade is in right now: the rim meets the rock at a different x at every height (a
            // circle), so sample a few heights up the rock: how much of its height is under the blade, less cavity
            float reach = Reach;
            var g = _rock.Geology;
            EnsureExtents();
            float passH = RockHeight;
            float cutting = 0f; float matSum = 0f; int inCount = 0;
            const int samples = 5;
            for (int i = 0; i < samples; i++)
            {
                float y = passH * (i + 0.5f) / samples;
                float reachY = HalfChordAt(y) - _carriageX;
                if (reachY > _ext.min.x && reachY < _ext.max.x) { inCount++; matSum += MaterialFraction(reachY); }
            }
            bool inRock = inCount > 0;
            cutting = inCount / (float)samples;
            float matFrac = inRock ? matSum / inCount : 1f;
            float section = Mathf.Clamp(RockHeight / 0.11f, 0.6f, 1.4f);      // a taller section is more stone under the blade
            float density = Mathf.Lerp(0.85f, 1.15f, g.CrystalDensity) ;
            float targetLoad = inRock && feed > 0f ? (feed / BaseFeed) * g.Family.ShellToughness * cutting * matFrac * section * (BladeDull ? 1.35f : 1f) * CoolantLoadMult * 0.8f : 0f;
            _load = Mathf.MoveTowards(_load, targetLoad, dt * (targetLoad > _load ? 2.2f : 3.5f));
            float over = Overload;
            // the carriage moves; a bogged blade stalls it
            float stall = 1f - Mathf.Clamp01(over * 0.8f);
            _carriageX -= feed * stall * dt;
            _progress = Mathf.Clamp01((CarriageStart - _carriageX) / Travel);
            SecondsThisCut += dt;
            // wear: material, hard feeding, a thin blade; coolant helps, dry cutting punishes
            if (inRock && feed > 0f)
            {
                float wear = dt * (feed / BaseFeed) * g.Family.ShellToughness * matFrac * section * 0.0045f * (1f + 2f * over) * (_thinForCut ? 1.25f : 1f) * CoolantWearMult;
                st.BladeWear = Mathf.Clamp01(st.BladeWear + wear);
                st.Stats.BladeWearSpent += wear;
                WearThisCut += wear;
            }
            // overload chips the crystals along the kerf: a crystal-rich section chips more, a cracked shell catches
            // the blade, the flood pump halves it, a thin blade cuts cleaner
            if (over > 0.05f && inRock)
            {
                _chipTimer -= dt;
                if (_chipTimer <= 0f)
                {
                    _chipTimer = 0.35f;
                    float cracked = CrackedNearPlane() ? 1.8f : 1f;
                    float chance = 0.85f * over * CoolantChipMult * (_thinForCut ? 0.75f : 1f) * Mathf.Lerp(0.6f, 1.3f, g.Family.Fragility) * (matFrac < 0.95f ? density : 1f) * cracked;
                    if (_cutRng.NextDouble() < chance) ChipAlongKerf();
                }
            }
            else _chipTimer = 0f;
            // a rock the jaws do not hold well shifts under a sustained hard feed: the plane moves a little, the face steps
            if (over > 0.3f && _grip < 0.85f && inRock)
            {
                _shiftBuild += dt * over * (1f - _grip) * 1.6f;
                if (_shiftBuild >= 1f)
                {
                    _shiftBuild = 0f;
                    float shift = (_cutRng.NextDouble() < 0.5 ? -1f : 1f) * 0.0015f;
                    _offset = ClampOffset(_offset + shift);
                    _faceStep += Mathf.Abs(shift);
                    rec.CutOffset = _offset; rec.CutFaceStep = _faceStep;
                    PlanInRockFrame(out _kerfNormal, out _kerfHeight);   // the blade cuts where the rock now sits
                    rec.CutNormal = _kerfNormal; rec.CutHeight = _kerfHeight;
                    WorkshopAudio.Play("wood_knock", _rock.transform.position, 0.6f, 0.8f);
                    _controller?.Impulse(0.2f);
                    Haptics.Pulse(0.5f, 0.3f, 0.1f);
                    ShiftNote = "the rock shifted in the jaws";
                }
            }
            else _shiftBuild = Mathf.MoveTowards(_shiftBuild, 0f, dt);
            // sound and feel
            float grind = inRock ? Mathf.Clamp01(_load) : 0f;
            WorkshopAudio.SetLoop(_motorLoop, 0.35f + 0.1f * _rpm, Mathf.Lerp(0.55f, 1.05f, _rpm) * (1f - 0.15f * Mathf.Clamp01(over)));
            WorkshopAudio.SetLoop(_grindLoop, grind * (CoolantOpen ? 0.6f : 0.85f), Mathf.Lerp(0.85f, 1.2f, Mathf.Clamp01(_load)) * (1f - 0.2f * Mathf.Clamp01(over)) * (CoolantOpen ? 1f : 1.15f));
            if (_feeding && inRock) Haptics.Pulse(0.12f + 0.4f * Mathf.Clamp01(_load), 0.06f + 0.3f * over, 0.05f);
            if (_controller != null && over > 0.2f) _controller.Impulse(0.05f * over);
            _slurryTimer -= dt;
            if (_feeding && inRock && _slurryTimer <= 0f)
            {
                _slurryTimer = 0.09f;
                float amount = 0.25f + 0.3f * Mathf.Clamp01(_load);
                if (CoolantOpen) EffectsFactory.Instance?.Slurry(ContactPoint, -transform.forward, amount * (FloodPump ? 1.3f : 1f));
                else EffectsFactory.Instance?.Impact(ContactPoint, -transform.forward, amount);   // dry: rock dust
            }
            PoseRock();
            UpdatePreview();
            rec.CutProgress = _progress;
            if (Time.frameCount % 90 == 0) session.QueueSave("saw-progress");
            // through
            if (_progress >= 0.999f) StartCoroutine(FinishCut());
            else if (GameInput.BackPressed && !_feeding)
            {
                // step away mid-cut: the clamp holds, the cut waits
                Exit();
            }
        }

        public string ShiftNote { get; private set; } = "";

        private bool CrackedNearPlane()
        {
            var rec = _rock.Record;
            if (_rock.IsPiece || rec.SectorStress == null || rec.SectorStress.Length == 0) return false;
            // the seam ring runs round the rock's equator; a plane through it crosses every cracked sector it passes
            for (int i = 0; i < rec.SectorStress.Length; i++) if (rec.SectorStress[i] >= 1f) return true;
            return false;
        }

        private void ChipAlongKerf()
        {
            var vis = _rock.Visual;
            var n = _kerfNormal; float h = _kerfHeight;
            var cond = _rock.Record.Condition;
            var geo = vis.Geometry;
            if (geo == null) return;
            cond.EnsureSize(geo.Crystals.Count);
            var near = new List<int>();
            foreach (var c in geo.Crystals)
            {
                float d = Mathf.Abs(Vector3.Dot(c.Position, n) - h);
                if (d < c.Height * 1.2f + 0.006f && cond.DamageAt(c.Index) < CrystalDamage.Broken) near.Add(c.Index);
            }
            if (near.Count == 0) return;
            int count = Mathf.Min(near.Count, 1 + _cutRng.Next(0, 2));
            for (int i = 0; i < count; i++)
            {
                int idx = near[_cutRng.Next(0, near.Count)];
                cond.CrystalDamage[idx] = (byte)Mathf.Min(CrystalDamage.Broken, cond.DamageAt(idx) + 1);
            }
            ChipsThisCut++;
            _rock.Record.DamageEvents++;
            _rock.Record.ShellDamage = Mathf.Clamp01(_rock.Record.ShellDamage + 0.03f);
            WorkshopAudio.Play("crystal_break", _rock.transform.position, 0.35f, 1.1f);
            WorkshopAudio.Play("slip", _rock.transform.position, 0.3f, 0.8f);
        }

        private IEnumerator FinishCut()
        {
            State = Phase.Done;
            _feeding = false;
            var session = GameSession.Instance;
            var parent = _rock.Record;
            var parentEntity = _rock;
            WorkshopAudio.Play("cut_through", _rock.transform.position, 0.9f);
            // the last web of stone parting under the blade: the same layers as a break, but at the saw's balance,
            // so a big dense agate still separates with more weight than a small one (§9.2, §9.3)
            FractureAudio.Break(_rock.transform.position, _rock.Visual, _rock.Geology, FractureAudio.Tool.Saw, false);
            MusicPlayer.Instance?.Duck(2f);
            _controller?.Impulse(0.25f);
            Haptics.Pulse(0.5f, 0.3f, 0.12f);
            float t = 0f;
            while (t < 0.5f) { t += Time.deltaTime; _rpm = Mathf.MoveTowards(_rpm, 0f, Time.deltaTime * 1.4f); WorkshopAudio.SetLoop(_motorLoop, 0.3f * _rpm, Mathf.Lerp(0.55f, 1.05f, _rpm)); WorkshopAudio.SetLoop(_grindLoop, 0f, 1f); yield return null; }
            StopMotor();
            // the two pieces, cut on the committed plane
            var n = _kerfNormal; float h = _kerfHeight;
            float kerf = _thinForCut ? ThinKerf : Kerf;
            PieceShape a, b;
            if (parent.IsPiece)
            {
                var p = parent.Piece;
                float hp = PieceHeightFromOffset(parent, h);
                a = new PieceShape { Normal = p.Normal, Lo = p.Lo, HasLo = p.HasLo, Hi = hp - kerf * 0.5f, HasHi = true };
                b = new PieceShape { Normal = p.Normal, Lo = hp + kerf * 0.5f, HasLo = true, Hi = p.Hi, HasHi = p.HasHi };
            }
            else
            {
                a = PieceShape.Below(n, h - kerf * 0.5f);
                b = PieceShape.Above(n, h + kerf * 0.5f);
            }
            Vector3 parentPos = parentEntity.transform.position; Quaternion parentRot = parentEntity.transform.rotation;
            var (ra, rb) = session.CutSpecimen(parent, a, b, "saw");
            // a stepped face (misaligned second pass, or a rock that shifted) shows in the symmetry and the price
            if (_faceStep > 0.0008f)
            {
                float pen = Mathf.Clamp01(_faceStep / 0.008f);
                ra.PieceSymmetry *= 1f - 0.45f * pen; rb.PieceSymmetry *= 1f - 0.45f * pen;
            }
            _rock = null;
            // the separation: both pieces appear exactly where the rock was, the kerf between them; the far piece,
            // held by nothing now, drops onto the pan before both go to the tray
            var ea = session.Spawn(ra, parentPos, parentRot, false);
            var eb = session.Spawn(rb, parentPos, parentRot, false);
            ea.Visual.SetWet(1f); eb.Visual.SetWet(1f);   // straight off the blade: coolant on the cut faces
            ea.SetStaticCollidable(); eb.SetStaticCollidable();
            var farPiece = eb; var heldPiece = ea;
            StartCoroutine(ReleaseJaw());
            float drop = 0.06f; t = 0f;
            Vector3 fp0 = farPiece.transform.position;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.35f;
                float e = t * t;
                farPiece.SetPose(fp0 + Vector3.down * (drop * Mathf.Clamp01(e)) + transform.forward * (0.012f * Mathf.Clamp01(t)), farPiece.transform.rotation);
                yield return null;
            }
            WorkshopAudio.Play("rock_place", farPiece.transform.position, 0.6f, 0.85f);
            EffectsFactory.Instance?.Slurry(farPiece.transform.position, Vector3.up, 0.3f);
            yield return new WaitForSeconds(0.55f);
            // lay them on the out tray, cut faces up
            var tray = OutTray;
            tray.Place(heldPiece, true);
            tray.Place(farPiece, true);
            PieceA = ea.Record.PristineForSale() >= eb.Record.PristineForSale() ? ea : eb;
            PieceB = PieceA == ea ? eb : ea;
            WorkshopAudio.Play("rock_place", tray.transform.position, 0.7f, 0.95f);
            EffectsFactory.Instance?.Impact(tray.transform.position + Vector3.up * 0.05f, Vector3.up, 0.3f);
            // the camera comes round to the tray
            if (CameraAnchor != null)
            {
                Vector3 from = CameraAnchor.position; Quaternion fromRot = CameraAnchor.rotation;
                Vector3 mid = tray.transform.position + Vector3.up * 0.04f;
                Vector3 to = mid + (transform.forward * -0.5f + transform.right * 0.15f) * 0.75f + Vector3.up * 0.42f;
                Quaternion toRot = Quaternion.LookRotation(mid - to, Vector3.up);
                float tt = 0f;
                while (tt < 1f) { tt += Time.deltaTime / 0.9f; float e = Mathf.SmoothStep(0f, 1f, tt); CameraAnchor.SetPositionAndRotation(Vector3.Lerp(from, to, e), Quaternion.Slerp(fromRot, toRot, e)); yield return null; }
            }
            ResultNote = BuildNote(ra, rb, parent);
            GameState.Log(parent, "cut", 0f, ResultNote);
            GameState.Log(ra, "cut", 0f, "from " + parent.Id); GameState.Log(rb, "cut", 0f, "from " + parent.Id);
            string call = session.ScoreCall(parent);
            if (!string.IsNullOrEmpty(call)) ResultNote += "  •  " + call;
            var stats = session.State.Stats;
            bool rare = ra.Geology.Tier >= QualityTier.Exceptional && !parent.IsPiece && ra.PieceOpening > 0.3f;
            if (rare) WorkshopAudio.Play2D("discovery", 0.5f);
            Tutorial.Notify("saw_cut");
            // a player who grabbed the piece during the camera move has already left the station: no result card then
            if (Active) Finished?.Invoke();
        }

        /// <summary>For a re-cut piece: the plane offset in the vise maps to a height along the piece's own normal.</summary>
        private float PieceHeightFromOffset(SpecimenRecord parent, float h)
        {
            var p = parent.Piece;
            // the piece stands with its primary face (+Y in piece frame) toward -Z, parallel to the blade; the blade
            // plane sits at the offset along +Z, i.e. (faceHeight - offset) along UpNormal
            float faceH = p.HasHi && Vector3.Dot(p.UpNormal, p.Normal) > 0f ? p.Hi : (p.HasLo ? -p.Lo : 0f);
            float upH = faceH - _offset;
            return Vector3.Dot(p.UpNormal, p.Normal) > 0f ? upH : -upH;
        }

        private string BuildNote(SpecimenRecord a, SpecimenRecord b, SpecimenRecord parent)
        {
            string what = a.Geology.Cavity == CavityArchetype.Nodule ? "Two faces of banding" : a.PieceOpening > 0.6f ? "Clean through the cavity" : a.PieceOpening > 0.02f ? "Cavity opened off-centre" : "Missed the cavity";
            string chips = ChipsThisCut == 0 ? "no chipping" : ChipsThisCut == 1 ? "one chipped edge" : $"{ChipsThisCut} chipped edges";
            string step = _faceStep > 0.0008f ? $"  •  stepped face ({_faceStep * 1000f:F0} mm{(string.IsNullOrEmpty(ShiftNote) ? "" : ", " + ShiftNote)})" : "";
            string dry = !CoolantOpen ? "  •  cut dry" : "";
            return $"{what}  •  {chips}{step}{dry}  •  blade {Mathf.RoundToInt(WearThisCut * 100f)}%  •  {SecondsThisCut:F0}s";
        }

        // ---- dev / harness probes ------------------------------------------------------------------
        /// <summary>
        /// Mid-cut integrity probe: whether the blade disc meets the rock's hull at this carriage position (it must,
        /// while the cut progresses), and any penetration between the rock or the vise family and the machine's
        /// static colliders (there must be none).
        /// </summary>
        public string Probe()
        {
            if (_rock == null) return "no rock";
            EnsureExtents();
            var c = RockLocalCenter();
            // blade disc in station frame: centre (0, BladeCenterY, BladeZ), radius BladeRadius; the rock's hull box
            float minX = c.x + _ext.min.x, maxX = c.x + _ext.max.x, minY = c.y + _ext.min.y, maxY = c.y + _ext.max.y, minZ = c.z + _ext.min.z, maxZ = c.z + _ext.max.z;
            bool straddles = minZ < BladeZ && maxZ > BladeZ;
            float nx = Mathf.Clamp(0f, minX, maxX), ny = Mathf.Clamp(BladeCenterY, minY, maxY);
            float dd = (nx - 0f) * (nx - 0f) + (ny - BladeCenterY) * (ny - BladeCenterY);
            bool bladeMeetsRock = straddles && dd < BladeRadius * BladeRadius;
            bool underFlange = maxY <= BladeCenterY - FlangeRadius + 0.001f;
            var sb = new System.Text.StringBuilder();
            sb.Append($"progress={_progress:F2} reach={Reach:F3} bladeMeetsRock={bladeMeetsRock} rockTop={maxY:F3} flangeBottom={BladeCenterY - FlangeRadius:F3} underFlange={underFlange} grip={_grip:F2} coolant={CoolantWord} load={_load:F2}");
            // penetration: rock colliders against the machine's colliders (excluding the vise family and the rock itself)
            var machineCols = Machine != null ? Machine.GetComponentsInChildren<Collider>() : Array.Empty<Collider>();
            var rockCols = _rock.GetComponentsInChildren<Collider>();
            float worst = 0f; string worstName = "";
            foreach (var rc in rockCols)
            {
                if (rc == null || !rc.enabled) continue;
                foreach (var mc in machineCols)
                {
                    if (mc == null || !mc.enabled || mc.isTrigger) continue;
                    if (Vise != null && (mc.transform == Vise || mc.transform.IsChildOf(Vise))) continue;
                    if (Physics.ComputePenetration(rc, rc.transform.position, rc.transform.rotation, mc, mc.transform.position, mc.transform.rotation, out _, out float dist) && dist > worst) { worst = dist; worstName = mc.name; }
                }
            }
            sb.Append($" rockPenetration={worst * 1000f:F1}mm{(worst > 0f ? "(" + worstName + ")" : "")}");
            return sb.ToString();
        }
    }
}
