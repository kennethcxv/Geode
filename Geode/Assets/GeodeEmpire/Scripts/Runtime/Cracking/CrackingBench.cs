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

namespace GeodeEmpire.Cracking
{
    /// <summary>
    /// The signature interaction. Placing a rock on the cradle enters bench view: set the chisel on the shell (it snaps
    /// to the seam when close), wind up and strike, watch the chip and the hairline appear where the chisel stood,
    /// rotate the rock and work around the ring, then split it open with a reveal sequence.
    /// Tools are posed by explicit contact geometry (tip on the shell, hammer face on the chisel cap), never by physics.
    /// </summary>
    public sealed class CrackingBench : MonoBehaviour
    {
        public PlacementZone Cradle;
        public Transform CradleCenter;
        public Transform CameraAnchor;
        public Transform ChiselVisual;
        public Transform HammerVisual;
        public Light TaskLight;
        /// <summary>The basic sandbag ring and the heavy cradle that replaces it once bought.</summary>
        public GameObject CradleVisual, HeavyCradleVisual;
        public float CradleAnchorHeight = 0.055f, HeavyCradleAnchorHeight = 0.075f;

        public bool HasHeavyCradle => GameSession.Instance != null && GameSession.Instance.State != null && UpgradeCatalog.Has(GameSession.Instance.State, UpgradeCatalog.HeavyCradle);

        /// <summary>Show whichever cradle the career owns; the rock on it is re-seated at the right height.</summary>
        public void RefreshCradle()
        {
            bool heavy = HasHeavyCradle;
            if (CradleVisual != null) CradleVisual.SetActive(!heavy);
            if (HeavyCradleVisual != null) HeavyCradleVisual.SetActive(heavy);
            if (CradleCenter != null)
            {
                var p = CradleCenter.localPosition; p.y = heavy ? HeavyCradleAnchorHeight : CradleAnchorHeight; CradleCenter.localPosition = p;
                var occ = Cradle != null ? Cradle.First : null;
                if (occ != null && !Active) Cradle.Place(occ, true);
            }
            if (_model != null) _model.Unstable = _rock != null && _rock.Geology.SizeClass == SizeClass.Oversized && !heavy;
        }

        // tool geometry (metres). Chisel: tip at its origin, cap at +Y ChiselLength. Hammer: origin at the handle
        // bottom, head centre at +Y HammerLen, head is a bar of half-length HammerHeadHalf along local X.
        public float ChiselLength = 0.17f;
        public float HammerLen = 0.312f;
        public float HammerHeadHalf = 0.066f;
        /// <summary>How far the flipped half must land from the rock centre so it clears the cradle ring.</summary>
        public float CradleClearance = 0.145f;

        // bench view tuning (live-editable, code-owned so the scene never pins stale values): where the camera sits
        // around the rock and how the chisel and hammer are held
        [NonSerialized] public float CamToPlayer = 0.68f, CamRight = -0.18f, CamUp = 0.55f;
        [NonSerialized] public float CamDistPerRadius = 3.6f, CamDistBase = 0.17f, CamDistMin = 0.34f, CamDistMax = 0.98f;
        [NonSerialized] public float LookAhead = 0.03f, LookRight = 0.06f, LookUpPerRadius = 0.2f, LookUpBase = 0.03f;
        [NonSerialized] public float ChiselAlongRadial = 0.7f, ChiselLeanRight = 0.4f, ChiselLeanUp = 0.5f, ChiselMinCos = 0.75f;
        [NonSerialized] public float HandleRight = 0.35f, HandleToPlayer = 0.55f;
        [NonSerialized] public float RestAngle = 6f, WindupRange = 58f;
        /// <summary>Re-apply the camera framing (after tuning the fields above in Play Mode).</summary>
        public void Reframe() { if (Active) FrameRock(); }

        public const float ForceTap = 0.28f, ForceCareful = 0.52f, ForceFirm = 0.78f;
        public static string ForceZoneName(float f) => f < ForceTap ? "Tap" : f < ForceCareful ? "Careful" : f < ForceFirm ? "Firm" : "Heavy";

        public bool Active { get; private set; }
        public bool Revealing { get; private set; }
        public bool Opened { get; private set; }
        public SpecimenEntity Rock => _rock;
        public StressModel Model => _model;
        public float Charge => _charge;
        public bool AimValid => _aimValid;
        /// <summary>1 when the chisel sits on the seam, fading to 0 away from it.</summary>
        public float Placement => _placement;
        public bool Swinging => _swinging;
        public Vector2 Cursor => _cursor;
        public bool HasLamp => GameSession.Instance != null && UpgradeCatalog.Has(GameSession.Instance.State, UpgradeCatalog.InspectionLamp);
        public StressModel.StrikeResult LastResult => _lastResult;
        public int DamageEventsThisRock => _damageThisRock;
        public string ResultNote { get; private set; } = "";

        /// <summary>
        /// Test/tutorial hook: viewport position of a seam point. Default is the near-right flank (the working spot a
        /// right-handed player naturally uses: chisel held on the near side, hammer from the right), offset in degrees
        /// around the rock from the point facing the camera, positive toward the player's right.
        /// </summary>
        public Vector2 SeamCursorHint(float azimuthOffsetDeg = 30f)
        {
            if (_rock == null || _cam == null) return new Vector2(0.5f, 0.5f);
            var geo = _rock.Visual.Geometry;
            Vector3 toCam = _cam.transform.position - _rock.transform.position; toCam.y = 0f; toCam.Normalize();
            Vector3 right = -Vector3.Cross(Vector3.up, toCam).normalized;
            float a = azimuthOffsetDeg * Mathf.Deg2Rad;
            Vector3 dir = Mathf.Cos(a) * toCam + Mathf.Sin(a) * right;
            Vector3 p = _rock.transform.position + dir * geo.MeanEquatorRadius * 0.98f;
            var vp = _cam.WorldToViewportPoint(p);
            return new Vector2(vp.x, vp.y);
        }

        /// <summary>Dev: freeze the swing at the moment of contact (for captures) until cleared.</summary>
        public bool HoldAtContact;

        /// <summary>
        /// Dev/clip-test staging: put a specimen of the given seed on the cradle, enter bench view with the ring
        /// almost fully cracked, so one strike (or StageSplit) triggers the reveal. Repeatable for capture.
        /// </summary>
        public SpecimenEntity StageReveal(ulong seed, bool splitNow)
        {
            var session = GameSession.Instance;
            if (Active) Exit();
            var occupant = Cradle.First;
            if (occupant != null) { Cradle.Take(occupant, true); session.Despawn(occupant); }
            // staged rocks are dev props: earlier ones must not pile up in the career (they would be restored on top
            // of each other at the cradle)
            var stale = new List<SpecimenRecord>();
            foreach (var r in session.State.Specimens) if (r.SupplierId == "stage") stale.Add(r);
            foreach (var r in stale) { var old = session.GetEntity(r.Id); if (old != null) session.Despawn(old); session.State.Specimens.Remove(r); }
            var rec = session.CreateSpecimenRecord(seed, "stage", "STAGE");
            rec.Location = Save.SpecimenLocation.World;
            var e = session.Spawn(rec, Cradle.Anchor.position, Quaternion.identity, false);
            Cradle.Place(e, true);
            Enter(e);
            for (int i = 0; i < StressModel.Sectors - 3; i++) _model.Stress[i] = 1f;
            RefreshCrackVisual();
            if (splitNow) StageSplit();
            return e;
        }

        public void StageSplit()
        {
            if (!Active || _rock == null || Opened || Revealing) return;
            for (int i = 0; i < StressModel.Sectors; i++) _model.Stress[i] = Mathf.Max(_model.Stress[i], 1f);
            var result = new StressModel.StrikeResult { Opened = true, CracksTotal = StressModel.Sectors };
            StartCoroutine(RevealRoutine(result));
        }

        /// <summary>Dev: perform one strike at the current aim with the given force, through the real swing.</summary>
        public void StageStrike(float force)
        {
            if (!Active || _rock == null || Opened || Revealing || _swinging || !_aimValid) return;
            StartCoroutine(SwingRoutine(Mathf.Clamp(force, 0.12f, 1f)));
        }

        public void SetCursor(Vector2 viewport) => _cursor = new Vector2(Mathf.Clamp(viewport.x, 0.12f, 0.88f), Mathf.Clamp(viewport.y, 0.12f, 0.9f));

        public event Action Entered;
        public event Action Exited;
        public event Action<StressModel.StrikeResult> Struck;
        public event Action<SpecimenEntity> Revealed;

        private readonly RaycastHit[] _aimHits = new RaycastHit[12];
        private SpecimenEntity _rock;
        private StressModel _model;
        private SeededRandom _rng;
        private Vector2 _cursor = new Vector2(0.5f, 0.55f);
        private bool _aimValid;
        private Vector3 _aimPoint, _aimNormal, _aimRadial, _aimTangent;
        private Vector3 _aimLocalRaw;         // where the ray actually hit, rock-local, before seam snapping
        private float _placement;
        // tool pose frozen at the moment the swing starts, so the hammer lands where the chisel was
        private Vector3 _toolPoint, _toolNormal, _toolRadial, _toolTangent, _toolAxis;
        private float _charge;
        private bool _charging, _swinging;
        private float _swingT, _recoilT = -1f, _swingCharge;
        private float _jolt, _rockKick;
        private Vector3 _rockBasePos;
        private float _rockYaw;
        private int _damageThisRock;
        private StressModel.StrikeResult _lastResult;
        private FirstPersonController _controller;
        private PlayerInteractor _player;
        private Camera _cam;
        private Vector3 _camAnchorHomePos; private Quaternion _camAnchorHomeRot;
        private Vector3 _chiselRestPos, _hammerRestPos;
        private Quaternion _chiselRestRot, _hammerRestRot;
        private float _lightBase;

        private void Awake()
        {
            if (Cradle != null)
            {
                Cradle.Placed += OnPlaced;
                Cradle.Taken += OnTaken;
            }
            if (ChiselVisual != null) { _chiselRestPos = ChiselVisual.position; _chiselRestRot = ChiselVisual.rotation; }
            if (HammerVisual != null) { _hammerRestPos = HammerVisual.position; _hammerRestRot = HammerVisual.rotation; }
            if (TaskLight != null) _lightBase = TaskLight.intensity;
            if (CameraAnchor != null) { _camAnchorHomePos = CameraAnchor.position; _camAnchorHomeRot = CameraAnchor.rotation; }
        }

        private void RestTools()
        {
            if (ChiselVisual != null) { ChiselVisual.gameObject.SetActive(true); ChiselVisual.SetPositionAndRotation(_chiselRestPos, _chiselRestRot); }
            if (HammerVisual != null) { HammerVisual.gameObject.SetActive(true); HammerVisual.SetPositionAndRotation(_hammerRestPos, _hammerRestRot); }
        }

        private void Start()
        {
            _controller = FindAnyObjectByType<FirstPersonController>();
            _player = FindAnyObjectByType<PlayerInteractor>();
            _cam = _controller != null ? _controller.Camera : Camera.main;
            var session = GameSession.Instance;
            if (session != null)
            {
                session.Loaded += RefreshCradle;
                session.StateChanged += RefreshCradle;
                if (session.State != null) RefreshCradle();
            }
        }

        private void OnDestroy()
        {
            var session = GameSession.Instance;
            if (session != null) { session.Loaded -= RefreshCradle; session.StateChanged -= RefreshCradle; }
        }

        private void OnPlaced(PlacementZone zone, SpecimenEntity e)
        {
            WorkshopAudio.Play("rock_place", e.transform.position, 0.9f);
            if (!e.IsOpened)
            {
                Tutorial.Notify("rock_on_bench");
                Enter(e);
            }
        }

        private void OnTaken(PlacementZone zone, SpecimenEntity e)
        {
            if (_rock == e && Active) Exit();
        }

        /// <summary>Re-enter for a rock already sitting on the cradle (e.g. after leaving mid-process or after reload).</summary>
        public void Resume()
        {
            var e = Cradle != null ? Cradle.First : null;
            if (e != null && !e.IsOpened) Enter(e);
        }

        public void Enter(SpecimenEntity e)
        {
            if (Active || e == null) return;
            var session = GameSession.Instance;
            _rock = e;
            _rock.Locked = true;
            _rock.SetStaticCollidable();
            Active = true;
            Opened = false;
            _damageThisRock = 0;
            ResultNote = "";
            var g = e.Geology;
            var fam = g.Family;
            _model = new StressModel
            {
                Toughness = fam.ShellToughness,
                ShellThickness = g.ShellThickness,
                Fragility = fam.Fragility,
                FineChisel = UpgradeCatalog.Has(session.State, UpgradeCatalog.FineChisel),
                Clamped = UpgradeCatalog.Has(session.State, UpgradeCatalog.BenchClamp),
                Wedge = UpgradeCatalog.Has(session.State, UpgradeCatalog.Wedge),
                Radius = e.Visual != null && e.Visual.Geometry != null ? e.Visual.Geometry.MeanEquatorRadius : g.Size,
                SeamQuality = g.SeamQuality,
                SectorThickness = g.SectorThickness,
                Unstable = g.SizeClass == SizeClass.Oversized && !UpgradeCatalog.Has(session.State, UpgradeCatalog.HeavyCradle),
            };
            _model.CopyFrom(e.Record.SectorStress);
            _model.StrikeCount = e.Record.StrikeCount;
            _rng = new SeededRandom(SeededRandom.Combine(e.Record.Seed, (ulong)(e.Record.StrikeCount + 1) * 31UL));
            if (!e.Record.ProcessingStarted)
            {
                e.Record.ProcessingStarted = true;
                session.FlushSave("bench-enter");
            }
            RefreshCrackVisual();
            _rockYaw = 0f;
            _rockBasePos = _rock.transform.position;
            _jolt = 0f; _rockKick = 0f;
            _cursor = new Vector2(0.5f, 0.52f);
            FrameRock();
            EnsureFillLight().enabled = true;
            if (_controller != null) _controller.EnterStationView(CameraAnchor);
            if (_player != null) _player.InputLocked = true;
            if (TaskLight != null) TaskLight.intensity = _lightBase * (HasLamp ? 1.6f : 1f);
            Entered?.Invoke();
        }

        /// <summary>Bench camera: the same viewing direction as the authored anchor, but close enough that the rock fills the view.</summary>
        private void FrameRock()
        {
            if (CameraAnchor == null || _rock == null) return;
            var geo = _rock.Visual.Geometry;
            Vector3 center = CradleCenter.position;
            // steep, slightly to the right: the chisel is driven almost horizontally into the seam on the near side and
            // leans left, the hammer comes from the right, so from up here the strike is seen side-on, not end-on
            Vector3 toPlayer = -transform.forward; toPlayer.y = 0f; toPlayer.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, -toPlayer).normalized;     // the player's right when facing the bench
            Vector3 dir = (toPlayer * CamToPlayer + right * CamRight + Vector3.up * CamUp).normalized;
            float dist = Mathf.Clamp(geo.MaxRadius * CamDistPerRadius + CamDistBase, CamDistMin, CamDistMax);
            Vector3 camPos = center + dir * dist;
            // the chisel and hammer extend toward the player, so look a little that way
            Vector3 lookAt = center + toPlayer * LookAhead + right * LookRight + Vector3.up * (geo.MaxRadius * LookUpPerRadius + LookUpBase);
            CameraAnchor.SetPositionAndRotation(camPos, Quaternion.LookRotation(lookAt - camPos, Vector3.up));
        }

        private Light _fill;

        /// <summary>A soft fill from the player's side so the face being worked is never in the task light's shadow.</summary>
        private Light EnsureFillLight()
        {
            if (_fill != null) return _fill;
            var go = new GameObject("BenchFill");
            go.transform.SetParent(CameraAnchor, false);
            go.transform.localPosition = new Vector3(0.12f, 0.05f, -0.05f);
            _fill = go.AddComponent<Light>();
            _fill.type = LightType.Point;
            _fill.range = 1.4f;
            _fill.intensity = 0.9f;
            _fill.color = new Color(1f, 0.95f, 0.88f);
            _fill.shadows = LightShadows.None;
            return _fill;
        }

        /// <summary>Push the persisted stress and impact marks into the shell shader.</summary>
        private void RefreshCrackVisual()
        {
            if (_rock == null || _rock.Visual == null) return;
            _rock.Visual.SetCrackState(_model != null ? _model.Stress : _rock.Record.SectorStress, _rock.Record.Impacts, HasLamp ? 1f : 0.55f, Opened ? 0.35f : 1f);
        }

        /// <summary>Dev diagnostics: record why the bench was left.</summary>
        public static bool TraceExits;
        public string LastExitReason { get; private set; } = "";

        public void Exit()
        {
            if (!Active || Revealing) return;
            if (TraceExits) { LastExitReason = new System.Diagnostics.StackTrace(1, false).ToString(); Debug.Log("[CrackingBench] Exit\n" + LastExitReason); }
            CursorController.MarkInputConsumed();   // the Back press that leaves the bench must not also pause
            Active = false;
            if (_controller != null) _controller.ExitStationView();
            if (_player != null) _player.InputLocked = false;
            RestTools();
            if (_fill != null) _fill.enabled = false;
            if (_rock != null)
            {
                _rock.Locked = false;
                if (!Opened) _rock.transform.position = _rockBasePos;
            }
            if (TaskLight != null) TaskLight.intensity = _lightBase;
            _charge = 0f; _charging = false; _swinging = false; _recoilT = -1f;
            _rock = null;
            Exited?.Invoke();
            GameSession.Instance?.QueueSave("bench-exit");
        }

        private void Update()
        {
            if (!Active || _rock == null) return;
            float dt = Time.deltaTime;

            if (Revealing) return;

            if (Opened)
            {
                if (GameInput.InteractPressed)
                {
                    var rock = _rock;
                    _player.IgnoreInteractUntilFrame = Time.frameCount + 1;
                    Exit();
                    if (rock.Zone != null) rock.Zone.Take(rock);
                    _player.PickUp(rock);
                    Tutorial.Notify("specimen_picked");
                    WorkshopAudio.Play("rock_pickup", rock.transform.position, 0.7f);
                }
                else if (GameInput.BackPressed) Exit();
                return;
            }

            if (GameInput.BackPressed && !_swinging) { Exit(); return; }

            // rotate the rock on the cradle
            float rot = GameInput.Rotate * 95f * dt + GameInput.Scroll.y * 0.12f;
            if (Mathf.Abs(rot) > 0.0001f && !_swinging)
            {
                _rockYaw += rot;
                _rock.transform.RotateAround(CradleCenter.position, Vector3.up, rot);
                _rockBasePos = _rock.transform.position;
            }

            // the rock takes the hit: a tiny settle into the cradle that springs back
            _rockKick = Mathf.MoveTowards(_rockKick, 0f, dt * 9f);
            _rock.transform.position = _rockBasePos + Vector3.down * (0.0025f * _rockKick);
            _jolt = Mathf.MoveTowards(_jolt, 0f, dt * 11f);

            // virtual cursor
            Vector2 look = GameInput.Look;
            if (GameInput.UsingGamepad) _cursor += look * 0.85f * dt * GameSettings.Current.GamepadSensitivity;
            else _cursor += look * 0.0011f * GameSettings.Current.MouseSensitivity;
            _cursor.x = Mathf.Clamp(_cursor.x, 0.12f, 0.88f);
            _cursor.y = Mathf.Clamp(_cursor.y, 0.12f, 0.9f);

            if (!_swinging) Aim();
            UpdateToolVisuals();

            // wind-up and strike
            if (!_swinging)
            {
                if (GameInput.StrikeHeld && _aimValid)
                {
                    _charging = true;
                    _charge = Mathf.Min(1f, _charge + dt / 0.8f);
                }
                if (_charging && (GameInput.StrikeReleased || !_aimValid && !GameInput.StrikeHeld))
                {
                    float f = Mathf.Max(0.18f, _charge);
                    _charging = false;
                    if (_aimValid) StartCoroutine(SwingRoutine(f));
                    else _charge = 0f;
                }
            }
        }

        /// <summary>
        /// Where the chisel goes: the shell point under the cursor, pulled onto the natural seam when it is close.
        /// The snap makes a deliberate placement easy on any device; strikes far from the seam stay poor.
        /// </summary>
        private void Aim()
        {
            _aimValid = false;
            _placement = 0f;
            var ray = _cam.ViewportPointToRay(new Vector3(_cursor.x, _cursor.y, 0f));
            int n = Physics.RaycastNonAlloc(ray, _aimHits, 3f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            RaycastHit hit = default;
            for (int i = 0; i < n; i++)
            {
                var h = _aimHits[i];
                if (h.collider.attachedRigidbody != _rock.Body) continue;
                if (h.distance < best) { best = h.distance; hit = h; _aimValid = true; }
            }
            if (!_aimValid) return;

            var geo = _rock.Visual.Geometry;
            var rockT = _rock.transform;
            Vector3 local = rockT.InverseTransformPoint(hit.point);
            _aimLocalRaw = local;
            float lon = Mathf.Atan2(local.z, local.x);
            float offset = local.y / Mathf.Max(0.01f, geo.MaxRadius);
            float snap = 1f - Mathf.SmoothStep(0.10f, 0.26f, Mathf.Abs(offset));
            Vector3 aimLocal = local;
            if (snap > 0f)
            {
                // the seam point at this longitude: rim radius and rim jitter interpolated between shell longitudes
                int N = geo.Longitudes;
                float lt = (lon < 0f ? lon + Mathf.PI * 2f : lon) / (Mathf.PI * 2f) * N;
                int l0 = Mathf.FloorToInt(lt) % N, l1 = (l0 + 1) % N;
                float f = lt - Mathf.Floor(lt);
                float r = Mathf.Lerp(geo.Bottom.EquatorOuterRadius[l0], geo.Bottom.EquatorOuterRadius[l1], f);
                float y = Mathf.Lerp(geo.Bottom.EquatorY[l0], geo.Bottom.EquatorY[l1], f);
                var seamLocal = new Vector3(Mathf.Cos(lon) * r, y, Mathf.Sin(lon) * r);
                aimLocal = Vector3.Lerp(local, seamLocal, snap);
            }
            _placement = snap;
            _aimPoint = rockT.TransformPoint(aimLocal);
            _aimNormal = hit.normal;
            // the chisel is driven along the rock's radial at the seam (the bumpy face normal only decides how
            // square the blow lands); the blade lies along the seam tangent
            _aimRadial = rockT.TransformDirection(new Vector3(Mathf.Cos(lon), 0f, Mathf.Sin(lon))).normalized;
            _aimTangent = rockT.TransformDirection(new Vector3(-Mathf.Sin(lon), 0f, Mathf.Cos(lon))).normalized;
        }

        private float WindupAngle(float charge) => RestAngle + WindupRange * charge;

        private void UpdateToolVisuals()
        {
            if (ChiselVisual == null) return;
            if (!_aimValid && !_swinging)
            {
                RestTools();
                return;
            }
            if (!_swinging) { _toolPoint = _aimPoint; _toolNormal = _aimNormal; _toolRadial = _aimRadial; _toolTangent = _aimTangent; }
            if (!ChiselVisual.gameObject.activeSelf) ChiselVisual.gameObject.SetActive(true);

            Vector3 toCam = (_cam.transform.position - _toolPoint).normalized;
            Vector3 camFlat = toCam; camFlat.y = 0f; camFlat.Normalize();
            Vector3 right = -Vector3.Cross(Vector3.up, camFlat).normalized;
            // driven along the seam radial, leaning up and to the player's right so the hammer comes down onto it
            // from the near side; never more than ~40 degrees off the radial so the wedge bites instead of skating
            Vector3 axis = (_toolRadial * ChiselAlongRadial + Vector3.up * ChiselLeanUp + right * ChiselLeanRight).normalized;
            float cosMin = ChiselMinCos;
            float d = Vector3.Dot(axis, _toolRadial);
            if (d < cosMin) axis = Vector3.Slerp(axis, _toolRadial, (cosMin - d) / Mathf.Max(0.01f, 1f - d)).normalized;
            _toolAxis = axis;
            Vector3 tangent = _toolTangent - axis * Vector3.Dot(_toolTangent, axis);
            if (tangent.sqrMagnitude < 1e-4f) tangent = Vector3.Cross(axis, toCam);
            tangent.Normalize();
            var chiselRot = Quaternion.LookRotation(Vector3.Cross(tangent, axis), axis);
            // tip rests on the shell (a hair outside it so the wedge never sinks in), driven a few mm in on impact
            Vector3 tip = _toolPoint + _toolNormal * 0.0012f - axis * (0.0045f * _jolt);
            ChiselVisual.SetPositionAndRotation(tip, chiselRot);

            if (HammerVisual == null) return;
            if (!HammerVisual.gameObject.activeSelf) HammerVisual.gameObject.SetActive(true);
            // hammer in the right hand: the grip sits toward the player and to their right, the head bar lies along the
            // chisel axis, and at the bottom of the swing its lower face sits on the chisel cap
            Vector3 cap = tip + axis * ChiselLength;
            Vector3 handleDir = -(right * HandleRight + camFlat * HandleToPlayer);   // grip -> head
            handleDir -= axis * Vector3.Dot(handleDir, axis);
            handleDir.Normalize();
            Vector3 headContact = cap + axis * (HammerHeadHalf + 0.002f);
            Vector3 grip = headContact - handleDir * HammerLen;
            Vector3 swingAxis = Vector3.Cross(axis, handleDir).normalized;   // positive angle drives the head into the cap
            float angle;
            if (_swinging)
            {
                if (_recoilT >= 0f) angle = Mathf.Lerp(0f, -16f, Mathf.SmoothStep(0f, 1f, _recoilT));
                else angle = Mathf.Lerp(-WindupAngle(_swingCharge), 0f, Mathf.SmoothStep(0f, 1f, _swingT));
            }
            else angle = -WindupAngle(_charge);
            var swing = Quaternion.AngleAxis(angle, swingAxis);
            Vector3 hd = swing * handleDir;
            Vector3 ax = swing * axis;
            // local +X (the domed striking face) points down the chisel axis at the cap; the peen points away
            var hRot = Quaternion.LookRotation(Vector3.Cross(hd, ax), hd);
            HammerVisual.SetPositionAndRotation(grip, hRot);
        }

        private IEnumerator SwingRoutine(float force)
        {
            _swinging = true;
            _swingT = 0f;
            _recoilT = -1f;
            _swingCharge = Mathf.Clamp01(force);
            float dur = Mathf.Lerp(0.10f, 0.15f, force);
            WorkshopAudio.Play("swing", HammerVisual != null ? HammerVisual.position : _toolPoint, 0.18f + 0.5f * force, 0.85f + 0.4f * force);
            while (_swingT < 1f)
            {
                _swingT += Time.deltaTime / dur;
                yield return null;
            }
            _swingT = 1f;
            DoStrike(force);
            // the face stays on the cap for a beat so the contact reads, then the hammer springs back
            yield return new WaitForSeconds(0.05f);
            while (HoldAtContact) { _jolt = 1f; yield return null; }
            _recoilT = 0f;
            while (_recoilT < 1f)
            {
                _recoilT += Time.deltaTime / 0.16f;
                yield return null;
            }
            _charge = 0f;
            _swinging = false;
            _recoilT = -1f;
        }

        private void DoStrike(float force)
        {
            var session = GameSession.Instance;
            var geo = _rock.Visual.Geometry;
            var rockT = _rock.transform;
            Vector3 local = rockT.InverseTransformPoint(_toolPoint);
            float azimuth = Mathf.Atan2(local.z, local.x);
            float planeOffset = Mathf.Clamp(local.y / Mathf.Max(0.01f, geo.MaxRadius), -1f, 1f);
            // how square the blow lands: the chisel axis against the actual face it stands on
            float angle = Mathf.Clamp01(Vector3.Dot(_toolAxis, _toolNormal) * 1.2f);
            var input = new StressModel.StrikeInput { Azimuth = azimuth, PlaneOffset = planeOffset, Force = force, AngleFactor = angle };
            var result = _model.Strike(input, ref _rng);
            _lastResult = result;

            // commit
            var rec = _rock.Record;
            rec.SectorStress = _model.ToArray();
            rec.StrikeCount = _model.StrikeCount;
            session.State.Stats.TotalStrikes++;

            _jolt = Mathf.Clamp01(0.4f + force);
            _rockKick = Mathf.Clamp01(0.3f + force * 0.8f * (result.Wobbled ? 2.2f : 1f));

            // feedback
            float ringFrac = result.CracksTotal / (float)StressModel.Sectors;
            string clip = force < ForceTap ? "tap_light" : force < ForceFirm ? "tap_medium" : "tap_heavy";
            if (result.Wobbled) WorkshopAudio.Play("wood_knock", _rock.transform.position, 0.35f + 0.4f * force, 0.7f);
            if (result.Slipped)
            {
                WorkshopAudio.Play("slip", _toolPoint, 0.8f);
                _controller?.Impulse(0.15f * force);
            }
            else if (result.SurfaceChip)
            {
                // the chisel skated: a scrape, a flake, no ring from the shell
                WorkshopAudio.Play("slip", _toolPoint, 0.45f, 1.25f);
                WorkshopAudio.Play(clip, _toolPoint, 0.35f, 1.15f);
                _controller?.Impulse(0.12f * force);
                EffectsFactory.Instance?.Impact(_toolPoint, _toolNormal, 0.35f);
            }
            else
            {
                float pitch = Mathf.Lerp(1.1f, 0.85f, force) * (result.Placement > 0.6f ? 1f : 1.1f) * (result.Overstrike ? 0.82f : 1f);
                WorkshopAudio.Play(clip, _toolPoint, Mathf.Lerp(0.6f, 1f, force), pitch);
                // hammer face on the chisel cap rings; the stone thud below it is what changes with placement
                WorkshopAudio.Play("chisel_ring", _toolPoint, 0.16f + 0.4f * force, Mathf.Lerp(1.08f, 0.94f, force));
                if (result.NewCrack) { WorkshopAudio.Play("tick", _toolPoint, 0.9f, 0.9f); WorkshopAudio.Play("creak", _toolPoint, 0.35f + 0.4f * ringFrac, 1.1f - 0.25f * ringFrac); }
                else if (result.StressAdded > 0.4f && _rng.Chance(0.35f)) WorkshopAudio.Play("tick", _toolPoint, 0.4f, 1.2f);
                // the crack ran along a weak line: a longer, sharper tick and a second seam burst further round
                if (result.Lucky) { WorkshopAudio.Play("tick", _toolPoint, 0.7f, 0.75f); SeamBurst((result.Sector + (_rng.Chance(0.5f) ? 1 : StressModel.Sectors - 1)) % StressModel.Sectors, geo); }
                // a shell with most of its ring cracked groans under every blow
                if (ringFrac >= 0.5f && !result.NewCrack) WorkshopAudio.Play("creak", _rock.transform.position, 0.25f + 0.35f * ringFrac, 0.8f);
                // near the break the whole shell grinds: a low layer the player learns to listen for
                if (ringFrac >= 0.7f) WorkshopAudio.Play("tension", _rock.transform.position, 0.3f + 0.45f * ringFrac, 0.9f + 0.2f * ringFrac);
                _controller?.Impulse((0.2f + 0.55f * force) * (result.NewCrack ? 1.3f : 1f));
                EffectsFactory.Instance?.Impact(_toolPoint, _toolNormal, force * (result.Placement * 0.6f + 0.4f));
                if (result.NewCrack) SeamBurst(result.Sector, geo);
            }

            // the chip where the chisel stood, persisted so the rock still shows its history after a reload
            AddImpactMark(local, geo, geo.MaxRadius * (0.075f + force * 0.085f), result.Slipped ? 0.3f : result.SurfaceChip ? 0.45f : 0.5f + force * 0.5f);
            RefreshCrackVisual();

            if (result.Damaged && !result.Opened)
            {
                ApplyDamage(result.DamageSeverity, azimuth);
                WorkshopAudio.Play("crystal_break", _toolPoint, 0.5f, 0.85f);
            }

            Tutorial.Notify("first_strike");
            Struck?.Invoke(result);

            if (result.Opened)
            {
                // a burst shell tears crystals off all the way round, not just under the chisel
                if (result.Shattered) { ApplyDamage(0.95f, azimuth); ApplyDamage(0.8f, azimuth + 1.6f); ApplyDamage(0.75f, azimuth + 3.1f); ApplyDamage(0.7f, azimuth + 4.7f); }
                StartCoroutine(RevealRoutine(result));
            }
            else
            {
                // committed within the coalescing window; bench-enter already flushed the anti-reroll state
                session.QueueSave("strike");
            }
        }

        /// <summary>Store a chip in shell surface coordinates (longitude fraction, signed latitude fraction).</summary>
        private void AddImpactMark(Vector3 local, GeodeGeometry geo, float radius, float strength)
        {
            float lon = Mathf.Atan2(local.z, local.x);
            float u = (lon < 0f ? lon + Mathf.PI * 2f : lon) / (Mathf.PI * 2f);
            float len = Mathf.Max(0.001f, local.magnitude);
            float v = Mathf.Asin(Mathf.Clamp(local.y / len, -1f, 1f)) / (Mathf.PI * 0.5f);
            var list = _rock.Record.Impacts;
            list.Add(new Vector4(u, v, radius, strength));
            while (list.Count > SpecimenVisual.MaxImpacts) list.RemoveAt(0);
        }

        /// <summary>Dust and flakes along a seam segment that just opened.</summary>
        private void SeamBurst(int sector, GeodeGeometry geo)
        {
            var fx = EffectsFactory.Instance;
            if (fx == null) return;
            for (int i = 0; i < 3; i++)
            {
                float a = (sector + (i + 0.5f) / 3f) / StressModel.Sectors * Mathf.PI * 2f;
                var dirLocal = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 p = _rock.transform.TransformPoint(dirLocal * geo.MeanEquatorRadius);
                Vector3 nrm = _rock.transform.TransformDirection(dirLocal);
                fx.Impact(p, nrm, 0.25f);
            }
        }

        /// <summary>Break crystals near the strike. Visible later at the reveal: broken tips, missing points.</summary>
        private void ApplyDamage(float severity, float azimuth)
        {
            var geo = _rock.Visual.Geometry;
            var cond = _rock.Record.Condition;
            cond.EnsureSize(geo.Crystals.Count);
            var candidates = new List<(float score, int index)>();
            foreach (var c in geo.Crystals)
            {
                if (cond.DamageAt(c.Index) >= CrystalDamage.Missing) continue;
                float d = Mathf.Abs(Mathf.DeltaAngle(c.Azimuth * Mathf.Rad2Deg, azimuth * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                if (d > 0.6f || c.Latitude > 0.75f) continue;
                if (c.Centerpiece && severity < 0.6f) continue;
                float score = d + c.Latitude * 0.6f - c.Fragility * 0.3f + _rng.Range(0f, 0.25f);
                candidates.Add((score, c.Index));
            }
            if (candidates.Count == 0) return;
            candidates.Sort((a, b) => a.score.CompareTo(b.score));
            // a blow wrecks a patch of the carpet, not one point: scale with how many crystals there are
            int count = Mathf.Clamp(1 + Mathf.RoundToInt(severity * (2f + geo.Crystals.Count * 0.035f)), 1, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                int idx = candidates[i].index;
                byte cur = cond.DamageAt(idx);
                byte next = severity > 0.85f && i == 0 ? CrystalDamage.Missing : severity > 0.6f ? CrystalDamage.Broken : CrystalDamage.Chipped;
                if (cur >= next) next = (byte)Mathf.Min(CrystalDamage.Missing, cur + 1);
                cond.CrystalDamage[idx] = next;
            }
            _rock.Record.DamageEvents++;
            _rock.Record.ShellDamage = Mathf.Clamp01(_rock.Record.ShellDamage + severity * 0.12f);
            _rock.Record.DamageFraction = _rock.Visual.CrystalDamageFraction();
            // the shell shows it too: a ragged chip torn out of the rim beside the strike, bigger the harder the blow
            float rimLon = azimuth + _rng.Range(-0.35f, 0.35f);
            var rimLocal = new Vector3(Mathf.Cos(rimLon), _rng.Range(-0.08f, 0.08f), Mathf.Sin(rimLon)) * geo.MeanEquatorRadius;
            AddImpactMark(rimLocal, geo, geo.MaxRadius * (0.09f + severity * 0.12f), 0.7f + severity * 0.3f);
            _damageThisRock++;
            if (_rock.Record.DamageEvents == 1) GameSession.Instance.State.Stats.SpecimensDamaged++;   // persisted counter: no double count after re-entering
        }

        private IEnumerator RevealRoutine(StressModel.StrikeResult result)
        {
            Revealing = true;
            var session = GameSession.Instance;
            var rec = _rock.Record;
            var vis = _rock.Visual;
            var geo = vis.Geometry;
            var g = _rock.Geology;
            rec.Condition.Opened = true;
            rec.OpenedAtTicks = DateTime.UtcNow.Ticks;
            rec.Location = SpecimenLocation.Bench;
            session.State.Stats.RocksProcessed++;
            // the discovery is part of the same commit as the open: quitting during the animation cannot lose it
            float damage = vis.CrystalDamageFraction();
            rec.DamageFraction = damage;
            session.RecordDiscovery(rec, damage);
            session.FlushSave("opened");

            bool rare = g.Tier >= QualityTier.Exceptional;
            bool attractive = g.Tier >= QualityTier.Good;
            RestTools();
            _rock.transform.position = _rockBasePos;

            // the split
            WorkshopAudio.Play("crack_final", _rock.transform.position, 1f, rare ? 0.92f : 1f);
            WorkshopAudio.Play("fragments", _rock.transform.position, 0.8f);
            _controller?.Impulse(0.9f);
            EffectsFactory.Instance?.Split(_rock.transform.position, geo.MeanEquatorRadius, _cam.transform.forward);
            for (int i = 0; i < StressModel.Sectors; i++) _model.Stress[i] = Mathf.Max(_model.Stress[i], 1f);
            rec.SectorStress = _model.ToArray();
            vis.RebuildCrystals();
            vis.SetCrystalsVisible(true);
            vis.SetCrackState(_model.Stress, rec.Impacts, 0f, 1f);
            _rock.RebuildColliders();
            _rock.SetStaticCollidable();

            // reveal light in the cavity
            var lightGo = new GameObject("RevealLight");
            lightGo.transform.position = _rock.transform.position + Vector3.up * geo.MaxRadius * 0.9f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = geo.MaxRadius * 6f;
            light.color = new Color(0.93f, 0.96f, 1f);      // cool, so crystal colour reads instead of going yellow
            light.intensity = 0f;
            light.shadows = LightShadows.None;

            if (rare)
            {
                Time.timeScale = 0.5f;
                StartCoroutine(RestoreTimeScale(0.9f));
                StartCoroutine(DelayedSting(0.4f));
            }

            // hinge the top half open along the seam, to the player's left, like a book; it tips over the cradle
            // ring and comes to rest on the bench top, so both halves stay in view and nothing overlaps
            var top = vis.TopHalf;
            Vector3 camFlat = _cam.transform.position - _rock.transform.position; camFlat.y = 0f; camFlat.Normalize();
            Vector3 camRight = Vector3.Cross(Vector3.up, camFlat).normalized;   // player's right
            Vector3 fLocal = _rock.transform.InverseTransformDirection(camRight);
            float R = geo.MeanEquatorRadius;
            Vector3 hinge = -fLocal * R;
            Vector3 axis = Vector3.Cross(Vector3.up, fLocal).normalized;
            Vector3 startPos = top.localPosition;
            Quaternion startRot = top.localRotation;
            float landDist = Mathf.Max(2f * R + 0.012f, CradleClearance + geo.MaxRadius);
            Vector3 slide = -fLocal * (landDist - 2f * R);
            Vector3 landLocal = hinge + Quaternion.AngleAxis(178f, axis) * (startPos - hinge) + slide;
            Vector3 landWorld = _rock.transform.TransformPoint(landLocal);
            // rest on whatever lies under the landing spot (bench top), never above or inside it
            float surfaceY = _rock.transform.position.y - _rock.RestHeightOffset(false);
            var downRay = new Ray(landWorld + Vector3.up * 0.3f, Vector3.down);
            float bestSurface = float.MinValue;
            int hits = Physics.RaycastNonAlloc(downRay, _aimHits, 0.7f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits; i++)
            {
                var h = _aimHits[i];
                if (h.collider.attachedRigidbody == _rock.Body) continue;
                if (h.point.y > surfaceY - 0.3f && h.point.y < surfaceY + 0.05f && h.point.y > bestSurface) bestSurface = h.point.y;
            }
            if (bestSurface > float.MinValue) surfaceY = bestSurface;
            // rest the flipped half on its real lowest lump, not its pole
            var landRot = Quaternion.AngleAxis(178f, axis) * startRot;
            float finalLift = (surfaceY - _rock.LowestOfTop(landRot)) - landWorld.y;
            float dur = rare ? 1.5f : 1.15f;
            // dolly the bench camera in to admire the interior, centred between the two halves
            Vector3 camFrom = CameraAnchor.position; Quaternion camFromRot = CameraAnchor.rotation;
            Vector3 mid = _rock.transform.position - camRight * (landDist * 0.5f);
            float camDist = Mathf.Clamp(geo.MaxRadius * 3.6f + 0.1f, 0.28f, 0.6f);
            Vector3 camTo = mid + camFlat * (camDist * 0.7f) + Vector3.up * (camDist * 0.72f);
            Quaternion camToRot = Quaternion.LookRotation((mid + Vector3.up * geo.MaxRadius * 0.15f) - camTo, Vector3.up);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 2.2f);
                float angle = Mathf.Lerp(0f, 178f, e);
                float lift = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * R * 0.28f;
                var rot = Quaternion.AngleAxis(angle, axis);
                Vector3 pos = hinge + rot * (startPos - hinge) + slide * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.35f) / 0.65f)) + Vector3.up * (lift + finalLift * e);
                top.localPosition = pos;
                top.localRotation = rot * startRot;
                light.intensity = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * (rare ? 1.5f : 0.9f);
                float ct = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.15f) / 0.85f));
                CameraAnchor.SetPositionAndRotation(Vector3.Lerp(camFrom, camTo, ct), Quaternion.Slerp(camFromRot, camToRot, ct));
                if (t > 0.35f && t < 0.4f) EffectsFactory.Instance?.Glints(_rock.transform.position + Vector3.up * R * 0.4f, R * 0.7f, attractive ? 18 : 8, SpecimenVisual.ApplySaturation(g.Palette.SurfaceA, g.Saturation));
                yield return null;
            }
            CameraAnchor.SetPositionAndRotation(camTo, camToRot);
            top.localPosition = landLocal + Vector3.up * finalLift;
            top.localRotation = Quaternion.AngleAxis(178f, axis) * startRot;
            WorkshopAudio.Play("rock_place", _rock.transform.TransformPoint(top.localPosition), 0.5f, 0.9f);
            EffectsFactory.Instance?.Impact(_rock.transform.TransformPoint(top.localPosition) + Vector3.down * geo.TopY * 0.9f, Vector3.up, 0.3f);
            _rock.RebuildColliders();
            _rock.SetStaticCollidable();
            StartCoroutine(FadeLight(light, 1.2f));

            ResultNote = BuildResultNote(g, damage, result);
            Opened = true;
            Revealing = false;
            Tutorial.Notify("rock_opened");
            Revealed?.Invoke(_rock);
            session.FlushSave("revealed");
        }

        private static string BuildResultNote(SpecimenGeology g, float damage, StressModel.StrikeResult result)
        {
            string tier = Valuation.TierLabel(Valuation.TierFromValue(Valuation.DamagedValue(g, damage, 0f)));
            string dmg = damage <= 0.001f ? "Clean open" : damage < 0.12f ? "Minor chipping" : damage < 0.35f ? "Noticeable damage" : "Heavy damage";
            if (result.Shattered) dmg = "Shattered open";
            return $"{tier}  •  {dmg}";
        }

        private IEnumerator RestoreTimeScale(float unscaledSeconds)
        {
            yield return new WaitForSecondsRealtime(unscaledSeconds);
            float t = 0f;
            while (t < 1f) { t += Time.unscaledDeltaTime / 0.3f; Time.timeScale = Mathf.Lerp(0.5f, 1f, t); yield return null; }
            Time.timeScale = 1f;
        }

        private IEnumerator DelayedSting(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            WorkshopAudio.Play2D("discovery", 0.55f);
        }

        private static IEnumerator FadeLight(Light l, float dur)
        {
            float start = l.intensity, t = 0f;
            while (t < 1f) { t += Time.deltaTime / dur; if (l == null) yield break; l.intensity = Mathf.Lerp(start, 0f, t); yield return null; }
            if (l != null) Destroy(l.gameObject);
        }
    }
}
