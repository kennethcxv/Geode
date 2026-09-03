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
    /// The trim saw: the second way to open rock. Clamp a rock in the carriage vise, turn it and slide it across
    /// until the chalk line shows the cut you want, commit (the clamp closes, the motor spins up), then feed the
    /// carriage into the blade. Feed too hard through thick stone and the blade bogs, chips the edges of the cut and
    /// wears. The cut is committed to the save the moment the clamp closes: a reload resumes the same plane at the
    /// same depth. Two pieces come out, both tied to the parent rock.
    /// </summary>
    public sealed class SawStation : MonoBehaviour
    {
        public enum Phase { Idle, Orient, Cutting, Done }

        public PlacementZone Clamp;
        public PlacementZone OutTray;
        public Transform Vise;          // sled + fixed jaw; rides the rails along local X and shifts across in Z
        public Transform Jaw;           // moving jaw, slides along the vise's Z
        public Transform Blade;         // spins about its local Z
        public Transform CameraAnchor;
        public GameObject Teaser;       // the tarp, shown until the saw is bought
        public GameObject Machine;      // everything else, shown once bought
        public Light TaskLight;

        // station geometry (metres, station-local): blade plane z, axle height and radius, carriage travel, rail top
        public float BladeZ = 0.05f;
        public float BladeCenterY = 1.06f;
        public float BladeRadius = 0.125f;
        public float RailTopY = 0.915f;
        public float CarriageStartX = 0.42f;
        /// <summary>Fixed jaw pad face, vise-local z; the rock's near side rests against it.</summary>
        public float FixedJawZ = -0.102f;
        public float SledTopY = 0.02f;

        // tuning (code-owned)
        [NonSerialized] public float BaseFeed = 0.02f;           // m/s carriage speed at nominal feed
        [NonSerialized] public float FastFeedMult = 1.9f;
        [NonSerialized] public float YawStep = 5f, RollStep = 5f, OffsetStep = 0.003f;
        [NonSerialized] public float Kerf = 0.003f, ThinKerf = 0.0015f;
        [NonSerialized] public Vector3 CamLocalPos = new Vector3(0.46f, 1.3f, -0.5f);
        [NonSerialized] public Vector3 CamLookLocal = new Vector3(0.2f, 1.0f, 0.05f);

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
        public bool ThinBlade => GameSession.Instance != null && UpgradeCatalog.Has(GameSession.Instance.State, UpgradeCatalog.ThinBlade);
        public bool Coolant => GameSession.Instance != null && UpgradeCatalog.Has(GameSession.Instance.State, UpgradeCatalog.CoolantPump);
        /// <summary>Dev/test: feed without holding the button.</summary>
        [NonSerialized] public bool DevFeed;
        [NonSerialized] public bool DevFast;

        public event Action Entered, Exited, CommittedEvent, Finished;

        private SpecimenEntity _rock;
        private float _yaw, _roll, _offset;
        private float _progress, _load, _rpm, _bladeAngle;
        private bool _feeding, _fast;
        private float _carriageX;
        private float _rockRadius, _rockHalfWidth;
        private Vector3 _viseHome, _jawHome;
        private FirstPersonController _controller;
        private PlayerInteractor _player;
        private Camera _cam;
        private AudioSource _motorLoop, _grindLoop;
        private float _repeatTimer, _chipTimer, _slurryTimer;
        private float _lastFeedInputTime;
        private System.Random _cutRng;

        // ------------------------------------------------------------------------------------
        private void Awake()
        {
            if (Clamp != null)
            {
                Clamp.Placed += OnPlaced;
                Clamp.Taken += OnTaken;
                Clamp.ExtraRefusal = Refusal;
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
            WorkshopAudio.StopLoop(_motorLoop); WorkshopAudio.StopLoop(_grindLoop);
        }

        /// <summary>Tarp until bought; the machine afterwards. The clamp only takes rock once the saw exists.</summary>
        public void RefreshOwned()
        {
            bool owned = Owned;
            if (Teaser != null) Teaser.SetActive(!owned);
            if (Machine != null) Machine.SetActive(owned);
            if (Clamp != null) Clamp.Locked = !owned;
            if (OutTray != null) OutTray.Locked = !owned;
        }

        private string Refusal(SpecimenEntity e)
        {
            if (!Owned) return "Under the tarp: buy the Trim Saw on the tablet";
            if (e.IsOpened && !e.IsPiece) return "Split halves cannot be clamped: the saw takes whole rough or sawn pieces";
            if (e.IsPiece && e.Record.Piece.IsSlab && e.Record.Piece.Thickness < 0.012f) return "Too thin to cut again";
            if (e.Geology.SizeClass == SizeClass.Oversized || e.Radius > BladeRadius * 0.98f) return "Too big for a 10-inch blade";
            if (BladeSpent) return "The blade is worn out: fit a new one from the tablet";
            return null;
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

        // ------------------------------------------------------------------------------------
        public void Enter(SpecimenEntity e)
        {
            if (Active || e == null) return;
            var session = GameSession.Instance;
            _rock = e;
            _rock.Locked = true;
            _rock.SetStaticCollidable();
            Active = true;
            State = Phase.Orient;
            ResultNote = "";
            PieceA = PieceB = null;
            ChipsThisCut = 0; WearThisCut = 0f; SecondsThisCut = 0f;
            _rockRadius = e.Visual != null && e.Visual.Geometry != null ? e.Visual.Geometry.MaxRadius : e.Geology.Size * 1.2f;
            _rockHalfWidth = _rockRadius;
            _progress = 0f; _load = 0f; _feeding = false;
            _carriageX = CarriageStartForRock;
            var rec = e.Record;
            if (rec.CutCommitted)
            {
                // resume the committed cut exactly where it stopped
                _yaw = rec.CutYaw; _roll = rec.CutRoll; _offset = rec.CutOffset; _progress = Mathf.Clamp01(rec.CutProgress);
                _carriageX = Mathf.Lerp(CarriageStartForRock, CarriageEndX, _progress);
                State = Phase.Cutting;
                StartMotor();
            }
            else
            {
                _yaw = 0f; _roll = 0f; _offset = 0f;
                if (e.IsPiece) { _roll = 0f; }
            }
            _cutRng = new System.Random((int)(rec.Seed & 0x7FFFFFFF) ^ rec.CutIndex * 7919);
            PoseRock();
            FrameCamera();
            if (_controller != null) _controller.EnterStationView(CameraAnchor);
            if (_player != null) _player.InputLocked = true;
            if (TaskLight != null) TaskLight.enabled = true;
            UpdatePreview(State == Phase.Orient ? 1f : 1f);
            Tutorial.Notify("rock_in_saw");
            Entered?.Invoke();
        }

        public void Exit()
        {
            if (!Active || State == Phase.Cutting && _feeding) return;
            CursorController.MarkInputConsumed();
            Active = false;
            if (_controller != null) _controller.ExitStationView();
            if (_player != null) _player.InputLocked = false;
            StopMotor();
            if (_rock != null)
            {
                _rock.Locked = false;
                if (State != Phase.Cutting) _rock.Visual.SetCutPreview(Vector3.up, 0f, Vector3.right, -10f, 0f);
            }
            if (State != Phase.Cutting) { State = Phase.Idle; OpenJaw(); }
            _rock = null;
            Exited?.Invoke();
            GameSession.Instance?.QueueSave("saw-exit");
        }

        private float CarriageEndX => -BladeRadius - _rockRadius - 0.012f;
        /// <summary>The carriage starts just short of the blade for this rock, not at the far end of the rails.</summary>
        private float CarriageStartForRock => Mathf.Min(CarriageStartX, BladeRadius + _rockRadius + 0.03f);

        // ------------------------------------------------------------------------------------
        // The plan: where the rock sits in the vise and, from that, the plane through it
        // ------------------------------------------------------------------------------------
        /// <summary>Rock rotation relative to the station: yaw about the vertical, roll about the feed axis. Pieces ride flat on the jaw.</summary>
        private Quaternion RockLocalRotation()
        {
            if (_rock != null && _rock.IsPiece)
            {
                // the flat face against the fixed jaw (which faces +Z): the piece's up (+Y, its cut face) points -Z
                return Quaternion.AngleAxis(_yaw, Vector3.forward) * Quaternion.FromToRotation(Vector3.up, Vector3.back);
            }
            return Quaternion.AngleAxis(_yaw, Vector3.up) * Quaternion.AngleAxis(_roll, Vector3.right);
        }

        private Vector3 RockLocalCenter()
        {
            float y = RailTopY + SledTopY + Mathf.Max(_rockRadius, 0.04f) + 0.004f;
            return new Vector3(_carriageX, y, BladeZ + _offset);
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

        private void PoseRock()
        {
            if (_rock == null) return;
            var R = RockLocalRotation();
            var center = RockLocalCenter();
            _rock.SetPose(transform.TransformPoint(center), transform.rotation * R);
            // vise under the rock, shifted across so the fixed jaw meets the rock's near side; the moving jaw closes the gap
            if (Vise != null)
            {
                float viseZ = center.z - _rockHalfWidth - FixedJawZ;
                Vise.localPosition = new Vector3(_carriageX, RailTopY, viseZ);
            }
            if (Jaw != null)
            {
                bool closed = State == Phase.Cutting || State == Phase.Done;
                float gap = closed ? 0f : 0.03f;
                Jaw.localPosition = new Vector3(_jawHome.x, _jawHome.y, FixedJawZ + 0.008f + _rockHalfWidth * 2f + gap);
            }
        }

        private void OpenJaw()
        {
            if (Jaw != null) Jaw.localPosition = _jawHome;
            if (Vise != null) Vise.localPosition = _viseHome;
        }

        private void FrameCamera()
        {
            if (CameraAnchor == null) return;
            Vector3 pos = transform.TransformPoint(CamLocalPos);
            Vector3 look = transform.TransformPoint(CamLookLocal + new Vector3(0f, Mathf.Max(0f, _rockRadius - 0.06f) * 0.5f, 0f));
            CameraAnchor.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
        }

        private void UpdatePreview(float show)
        {
            if (_rock == null || _rock.Visual == null) return;
            PlanInRockFrame(out var n, out float h);
            var f = FeedAxisInRockFrame();
            // the blade's leading rim relative to the rock centre, along the feed axis, in rock units
            float reach = BladeRadius - _carriageX;
            _rock.Visual.SetCutPreview(n, h, f, State == Phase.Orient ? -10f : reach, show);
        }

        /// <summary>What the player is told before committing: seconds and blade wear for this plan.</summary>
        public void Estimate(out float seconds, out float wear, out float cost)
        {
            float travel = CarriageStartForRock - CarriageEndX;
            float feed = BaseFeed * (BladeDull ? 0.65f : 1f);
            seconds = travel / feed;
            float tough = _rock != null ? _rock.Geology.Family.ShellToughness : 1f;
            wear = 0.045f * tough * (_rockRadius / 0.065f) * (ThinBlade ? 1.25f : 1f) * (Coolant ? 0.8f : 1f);
            cost = wear * UpgradeCatalog.Get(UpgradeCatalog.SawBlade).Price;
        }

        /// <summary>Dev/test: set the plan directly.</summary>
        public void SetPlan(float yaw, float roll, float offset)
        {
            _yaw = yaw; _roll = roll; _offset = Mathf.Clamp(offset, -_rockRadius * 0.85f, _rockRadius * 0.85f);
            if (_rock != null && _rock.IsPiece) ClampOffsetToPiece();
            PoseRock(); UpdatePreview(1f);
        }

        private void ClampOffsetToPiece()
        {
            // a piece is cut parallel to its faces: the plane must fall inside it
            var p = _rock.Record.Piece;
            float lo = p.HasLo ? p.Lo : -_rockRadius, hi = p.HasHi ? p.Hi : _rockRadius;
            // rock-frame height = -offset along the piece's normal... the piece frame has its cut face up, so heights map
            // through the piece rotation; keep it simple: limit the offset to the piece's thickness
            float half = Mathf.Min(_rockRadius * 0.85f, (hi - lo) * 0.5f - 0.005f);
            _offset = Mathf.Clamp(_offset, -half, half);
        }

        // ------------------------------------------------------------------------------------
        private void Update()
        {
            if (Blade != null && _rpm > 0.01f)
            {
                _bladeAngle += Time.deltaTime * 360f * 18f * _rpm;
                Blade.localRotation = Quaternion.AngleAxis(_bladeAngle, Vector3.forward);
            }
            if (!Active) return;
            if (_rock == null && State != Phase.Done) return;
            float dt = Time.deltaTime;
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

        private void UpdateOrient(float dt)
        {
            if (GameInput.BackPressed) { Exit(); return; }
            bool changed = false;
            _repeatTimer -= dt;
            // yaw: Q/E or the bumpers; roll: mouse Y / right stick Y; across: A/D or left stick X. Discrete steps, repeat while held.
            float rot = GameInput.Rotate;
            Vector2 move = GameInput.Move;
            Vector2 look = GameInput.Look;
            float rollIn = GameInput.UsingGamepad ? look.y : (Mathf.Abs(look.y) > 2.5f ? Mathf.Sign(look.y) : 0f);
            bool any = Mathf.Abs(rot) > 0.3f || Mathf.Abs(move.x) > 0.3f || Mathf.Abs(rollIn) > 0.3f;
            if (any && _repeatTimer <= 0f)
            {
                _repeatTimer = GameInput.UsingGamepad ? 0.14f : 0.09f;
                if (Mathf.Abs(rot) > 0.3f) { _yaw += Mathf.Sign(rot) * YawStep; changed = true; }
                if (Mathf.Abs(rollIn) > 0.3f && CanRotate) { _roll = Mathf.Clamp(_roll + Mathf.Sign(rollIn) * RollStep, -90f, 90f); changed = true; }
                if (Mathf.Abs(move.x) > 0.3f)
                {
                    _offset = Mathf.Clamp(_offset + Mathf.Sign(move.x) * OffsetStep, -_rockRadius * 0.85f, _rockRadius * 0.85f);
                    if (_rock.IsPiece) ClampOffsetToPiece();
                    changed = true;
                }
                if (changed) WorkshopAudio.Play("ui_click", _rock.transform.position, 0.15f, 1.4f);
            }
            if (!any) _repeatTimer = 0f;
            if (changed) { PoseRock(); UpdatePreview(1f); }
            if (GameInput.InteractPressed) Commit();
        }

        /// <summary>The clamp closes and the cut is committed to the save.</summary>
        public void Commit()
        {
            if (!Active || State != Phase.Orient || _rock == null) return;
            var session = GameSession.Instance;
            var rec = _rock.Record;
            PlanInRockFrame(out var n, out float h);
            rec.CutCommitted = true;
            rec.CutNormal = n; rec.CutHeight = h; rec.CutProgress = 0f;
            rec.CutYaw = _yaw; rec.CutRoll = _roll; rec.CutOffset = _offset;
            rec.ProcessingStarted = true;
            State = Phase.Cutting;
            _progress = 0f; _carriageX = CarriageStartForRock;
            PoseRock();
            WorkshopAudio.Play("clamp", _rock.transform.position, 0.9f);
            Haptics.Pulse(0.3f, 0.2f, 0.08f);
            StartMotor();
            UpdatePreview(1f);
            session.FlushSave("saw-commit");
            CommittedEvent?.Invoke();
        }

        private void StartMotor()
        {
            _rpm = 0f;
            if (_motorLoop == null) _motorLoop = WorkshopAudio.StartLoop("saw_motor", transform.TransformPoint(new Vector3(-0.35f, 1.0f, 0.15f)), 0.001f, 0.5f);
            if (_grindLoop == null) _grindLoop = WorkshopAudio.StartLoop("saw_grind", transform.TransformPoint(new Vector3(0f, BladeCenterY - BladeRadius * 0.5f, BladeZ)), 0.001f, 1f);
            if (TaskLight != null) TaskLight.enabled = true;
        }

        private void StopMotor()
        {
            WorkshopAudio.StopLoop(_motorLoop); _motorLoop = null;
            WorkshopAudio.StopLoop(_grindLoop); _grindLoop = null;
            _rpm = 0f;
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
            _feeding = wantFeed && _rpm > 0.55f;
            float feed = 0f;
            if (_feeding)
            {
                feed = BaseFeed * (_fast ? FastFeedMult : 1f) * (BladeDull ? 0.65f : 1f);
                _lastFeedInputTime = Time.time;
            }
            // the section the blade is in right now: shell is hard, the cavity is air
            float reach = BladeRadius - _carriageX;               // rock-frame distance along the feed axis
            var g = _rock.Geology;
            float cavR = g.Size * g.CavityFraction;
            bool inRock = reach > -_rockRadius && reach < _rockRadius;
            float chord = inRock ? Mathf.Sqrt(Mathf.Max(0f, _rockRadius * _rockRadius - reach * reach)) / _rockRadius : 0f;
            float matFrac = Mathf.Abs(reach) < cavR * 0.9f && _offset * _offset < cavR * cavR * 0.8f ? Mathf.Clamp01(g.ShellThickness * 2.4f) : 1f;
            float targetLoad = inRock && feed > 0f ? (feed / BaseFeed) * g.Family.ShellToughness * chord * matFrac * (BladeDull ? 1.35f : 1f) * (Coolant ? 0.7f : 1f) * 0.85f : 0f;
            _load = Mathf.MoveTowards(_load, targetLoad, dt * (targetLoad > _load ? 2.2f : 3.5f));
            float over = Overload;
            // the carriage moves; a bogged blade stalls it
            float stall = 1f - Mathf.Clamp01(over * 0.8f);
            _carriageX -= feed * stall * dt;
            float travel = CarriageStartForRock - CarriageEndX;
            _progress = Mathf.Clamp01((CarriageStartForRock - _carriageX) / travel);
            SecondsThisCut += dt;
            // wear: material, hard feeding, a thin blade; coolant helps
            if (inRock && feed > 0f)
            {
                float wear = dt * (feed / BaseFeed) * g.Family.ShellToughness * matFrac * 0.0045f * (1f + 2f * over) * (ThinBlade ? 1.25f : 1f) * (Coolant ? 0.8f : 1f);
                st.BladeWear = Mathf.Clamp01(st.BladeWear + wear);
                st.Stats.BladeWearSpent += wear;
                WearThisCut += wear;
            }
            // overload chips the crystals along the kerf; the coolant pump halves it, a thin blade cuts cleaner
            if (over > 0.05f && inRock)
            {
                _chipTimer -= dt;
                if (_chipTimer <= 0f)
                {
                    _chipTimer = 0.35f;
                    float chance = 0.85f * over * (Coolant ? 0.5f : 1f) * (ThinBlade ? 0.75f : 1f) * Mathf.Lerp(0.6f, 1.3f, g.Family.Fragility);
                    if (_cutRng.NextDouble() < chance) ChipAlongKerf();
                }
            }
            else _chipTimer = 0f;
            // sound and feel
            float grind = inRock ? Mathf.Clamp01(_load) : 0f;
            WorkshopAudio.SetLoop(_motorLoop, 0.35f + 0.1f * _rpm, Mathf.Lerp(0.55f, 1.05f, _rpm) * (1f - 0.15f * Mathf.Clamp01(over)));
            WorkshopAudio.SetLoop(_grindLoop, grind * 0.6f, Mathf.Lerp(0.85f, 1.2f, Mathf.Clamp01(_load)) * (1f - 0.2f * Mathf.Clamp01(over)));
            if (_feeding && inRock) Haptics.Pulse(0.12f + 0.4f * Mathf.Clamp01(_load), 0.06f + 0.3f * over, 0.05f);
            if (_controller != null && over > 0.2f) _controller.Impulse(0.05f * over);
            _slurryTimer -= dt;
            if (_feeding && inRock && _slurryTimer <= 0f)
            {
                _slurryTimer = 0.09f;
                Vector3 contact = transform.TransformPoint(new Vector3(BladeRadius * 0.92f, RockLocalCenter().y - _rockRadius * 0.15f, BladeZ));
                EffectsFactory.Instance?.Impact(contact, transform.forward * -1f, 0.25f + 0.3f * Mathf.Clamp01(_load));
            }
            PoseRock();
            UpdatePreview(1f);
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

        private void ChipAlongKerf()
        {
            var vis = _rock.Visual;
            PlanInRockFrame(out var n, out float h);
            // crystals near the plane (rock-frame heights differ from piece-frame for whole rocks: use the geometry list)
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
            _controller?.Impulse(0.25f);
            Haptics.Pulse(0.5f, 0.3f, 0.12f);
            float t = 0f;
            while (t < 0.5f) { t += Time.deltaTime; _rpm = Mathf.MoveTowards(_rpm, 0f, Time.deltaTime * 1.4f); WorkshopAudio.SetLoop(_motorLoop, 0.3f * _rpm, Mathf.Lerp(0.55f, 1.05f, _rpm)); WorkshopAudio.SetLoop(_grindLoop, 0f, 1f); yield return null; }
            StopMotor();
            // the two pieces
            PlanInRockFrame(out var n, out float h);
            float kerf = ThinBlade ? ThinKerf : Kerf;
            PieceShape a, b;
            if (parent.IsPiece)
            {
                var p = parent.Piece;
                // the plane in the parent piece's own frame is parallel to its faces: convert the offset to a height along its normal
                float hp = PieceHeightFromOffset(parent, h);
                a = new PieceShape { Normal = p.Normal, Lo = p.Lo, HasLo = p.HasLo, Hi = hp - kerf * 0.5f, HasHi = true };
                b = new PieceShape { Normal = p.Normal, Lo = hp + kerf * 0.5f, HasLo = true, Hi = p.Hi, HasHi = p.HasHi };
            }
            else
            {
                a = PieceShape.Below(n, h - kerf * 0.5f);
                b = PieceShape.Above(n, h + kerf * 0.5f);
            }
            var (ra, rb) = session.CutSpecimen(parent, a, b, "saw");
            _rock = null;
            // lay them on the out tray, cut faces up
            var tray = OutTray;
            var ea = session.Spawn(ra, tray.transform.position, Quaternion.identity, false);
            var eb = session.Spawn(rb, tray.transform.position, Quaternion.identity, false);
            tray.Place(ea, true);
            tray.Place(eb, true);
            PieceA = ea.Record.PristineForSale() >= eb.Record.PristineForSale() ? ea : eb;
            PieceB = PieceA == ea ? eb : ea;
            OpenJaw();
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
            var stats = session.State.Stats;
            bool rare = ra.Geology.Tier >= QualityTier.Exceptional && !parent.IsPiece && ra.PieceOpening > 0.3f;
            if (rare) WorkshopAudio.Play2D("discovery", 0.5f);
            Tutorial.Notify("saw_cut");
            Finished?.Invoke();
        }

        /// <summary>For a re-cut piece: the plane offset in the vise maps to a height along the piece's own normal.</summary>
        private float PieceHeightFromOffset(SpecimenRecord parent, float h)
        {
            var p = parent.Piece;
            // the piece rests with its primary face (+Y in piece frame) against the fixed jaw, i.e. facing -Z; the blade plane
            // sits at the offset along +Z, so it is (faceHeight - offset) along UpNormal, i.e. UpNormal height = faceH - offset
            float faceH = p.HasHi && Vector3.Dot(p.UpNormal, p.Normal) > 0f ? p.Hi : (p.HasLo ? -p.Lo : 0f);
            float upH = faceH - _offset;   // height along UpNormal
            return Vector3.Dot(p.UpNormal, p.Normal) > 0f ? upH : -upH;
        }

        private string BuildNote(SpecimenRecord a, SpecimenRecord b, SpecimenRecord parent)
        {
            float va = a.PristineForSale(), vb = b.PristineForSale();
            string what = a.Geology.Cavity == CavityArchetype.Nodule ? "Two faces of banding" : a.PieceOpening > 0.6f ? "Clean through the cavity" : a.PieceOpening > 0.02f ? "Cavity opened off-centre" : "Missed the cavity";
            string chips = ChipsThisCut == 0 ? "no chipping" : ChipsThisCut == 1 ? "one chipped edge" : $"{ChipsThisCut} chipped edges";
            return $"{what}  •  {chips}  •  blade {Mathf.RoundToInt(WearThisCut * 100f)}%  •  {SecondsThisCut:F0}s";
        }
    }
}
