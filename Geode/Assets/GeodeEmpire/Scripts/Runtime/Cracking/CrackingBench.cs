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
    /// The signature interaction. Placing a rock on the cradle enters bench view: aim the chisel on the shell,
    /// wind up and strike, rotate the rock, read the cracks, and finally split it open with a reveal sequence.
    /// </summary>
    public sealed class CrackingBench : MonoBehaviour
    {
        public PlacementZone Cradle;
        public Transform CradleCenter;
        public Transform CameraAnchor;
        public Transform ChiselVisual;
        public Transform HammerVisual;
        public Light TaskLight;

        public bool Active { get; private set; }
        public bool Revealing { get; private set; }
        public bool Opened { get; private set; }
        public SpecimenEntity Rock => _rock;
        public StressModel Model => _model;
        public float Charge => _charge;
        public bool AimValid => _aimValid;
        public Vector2 Cursor => _cursor;
        public bool HasLamp => GameSession.Instance != null && UpgradeCatalog.Has(GameSession.Instance.State, UpgradeCatalog.InspectionLamp);
        public StressModel.StrikeResult LastResult => _lastResult;
        public int DamageEventsThisRock => _damageThisRock;
        public string ResultNote { get; private set; } = "";

        /// <summary>Test/tutorial hook: viewport position of the seam point facing the camera.</summary>
        public Vector2 SeamCursorHint()
        {
            if (_rock == null || _cam == null) return new Vector2(0.5f, 0.5f);
            var geo = _rock.Visual.Geometry;
            Vector3 toCam = _cam.transform.position - _rock.transform.position; toCam.y = 0f; toCam.Normalize();
            Vector3 p = _rock.transform.position + toCam * geo.MeanEquatorRadius * 0.98f;
            var vp = _cam.WorldToViewportPoint(p);
            return new Vector2(vp.x, vp.y);
        }

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
            var rec = session.CreateSpecimenRecord(seed, "stage", "STAGE");
            rec.Location = Save.SpecimenLocation.World;
            var e = session.Spawn(rec, Cradle.Anchor.position, Quaternion.identity, false);
            Cradle.Place(e, true);
            Enter(e);
            for (int i = 0; i < StressModel.Sectors - 3; i++) _model.Stress[i] = 1f;
            _ribbon?.Refresh(_model, HasLamp);
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

        public void SetCursor(Vector2 viewport) => _cursor = new Vector2(Mathf.Clamp(viewport.x, 0.12f, 0.88f), Mathf.Clamp(viewport.y, 0.12f, 0.9f));

        public event Action Entered;
        public event Action Exited;
        public event Action<StressModel.StrikeResult> Struck;
        public event Action<SpecimenEntity> Revealed;

        private SpecimenEntity _rock;
        private StressModel _model;
        private CrackRibbon _ribbon;
        private SeededRandom _rng;
        private Vector2 _cursor = new Vector2(0.5f, 0.55f);
        private bool _aimValid;
        private Vector3 _aimPoint, _aimNormal;
        private float _charge;
        private bool _charging, _swinging;
        private float _swingT;
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
            };
            _model.CopyFrom(e.Record.SectorStress);
            _model.StrikeCount = e.Record.StrikeCount;
            _rng = new SeededRandom(SeededRandom.Combine(e.Record.Seed, (ulong)(e.Record.StrikeCount + 1) * 31UL));
            if (!e.Record.ProcessingStarted)
            {
                e.Record.ProcessingStarted = true;
                session.FlushSave("bench-enter");
            }
            _ribbon = CrackRibbon.Attach(e, session.Library.CrackMaterial);
            _ribbon.Refresh(_model, HasLamp);
            _rockYaw = 0f;
            _cursor = new Vector2(0.5f, 0.52f);
            if (CameraAnchor != null) CameraAnchor.SetPositionAndRotation(_camAnchorHomePos, _camAnchorHomeRot);
            if (_controller != null) _controller.EnterStationView(CameraAnchor);
            if (_player != null) _player.InputLocked = true;
            if (TaskLight != null) TaskLight.intensity = _lightBase * (HasLamp ? 1.6f : 1f);
            Entered?.Invoke();
        }

        /// <summary>Dev diagnostics: record why the bench was left.</summary>
        public static bool TraceExits;
        public string LastExitReason { get; private set; } = "";

        public void Exit()
        {
            if (!Active || Revealing) return;
            LastExitReason = new System.Diagnostics.StackTrace(1, false).ToString();
            if (TraceExits) Debug.Log("[CrackingBench] Exit\n" + LastExitReason);
            Active = false;
            if (_controller != null) _controller.ExitStationView();
            if (_player != null) _player.InputLocked = false;
            RestTools();
            if (_ribbon != null && !Opened) { Destroy(_ribbon.gameObject); }
            _ribbon = null;
            if (_rock != null) _rock.Locked = false;
            if (TaskLight != null) TaskLight.intensity = _lightBase;
            _charge = 0f; _charging = false; _swinging = false;
            var r = _rock;
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
            }

            // virtual cursor
            Vector2 look = GameInput.Look;
            if (GameInput.UsingGamepad) _cursor += look * 0.85f * dt * GameSettings.Current.GamepadSensitivity;
            else _cursor += look * 0.0011f * GameSettings.Current.MouseSensitivity;
            _cursor.x = Mathf.Clamp(_cursor.x, 0.12f, 0.88f);
            _cursor.y = Mathf.Clamp(_cursor.y, 0.12f, 0.9f);

            // aim on the rock surface
            _aimValid = false;
            var ray = _cam.ViewportPointToRay(new Vector3(_cursor.x, _cursor.y, 0f));
            var hits = Physics.RaycastAll(ray, 3f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            foreach (var h in hits)
            {
                var se = h.collider.GetComponentInParent<SpecimenEntity>();
                if (se != _rock) continue;
                if (h.distance < best) { best = h.distance; _aimPoint = h.point; _aimNormal = h.normal; _aimValid = true; }
            }
            UpdateToolVisuals(dt);

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

        private void UpdateToolVisuals(float dt)
        {
            if (ChiselVisual == null) return;
            if (!_aimValid)
            {
                RestTools();
                return;
            }
            Vector3 toCam = (_cam.transform.position - _aimPoint).normalized;
            Vector3 axis = (_aimNormal * 0.75f + toCam * 0.45f + Vector3.up * 0.35f).normalized;
            var rot = Quaternion.FromToRotation(Vector3.up, axis);
            ChiselVisual.SetPositionAndRotation(_aimPoint, rot * Quaternion.Euler(0f, 35f, 0f));
            if (HammerVisual != null)
            {
                // hammer held to the right of the chisel: head above the chisel cap, handle out to the side.
                // Wind-up raises the head around the grip; the swing brings it down onto the cap.
                const float chiselLen = 0.215f, hammerLen = 0.315f;
                float swingDrop = _swinging ? Mathf.SmoothStep(0f, 1f, _swingT) : 0f;
                Vector3 headPos = _aimPoint + axis * chiselLen;
                Vector3 side = Vector3.Cross(axis, toCam).normalized;   // roughly the player's right
                Vector3 handleDir = -side;
                Vector3 grip = headPos + axis * 0.012f - handleDir * hammerLen;
                Vector3 swingAxis = Vector3.Cross(axis, handleDir).normalized;
                float raise = Mathf.Lerp(-(18f + 40f * _charge), 4f, swingDrop);
                var swing = Quaternion.AngleAxis(raise, swingAxis);
                Vector3 hd = swing * handleDir;
                Vector3 ax = swing * axis;
                var hRot = Quaternion.LookRotation(Vector3.Cross(ax, hd), hd);
                HammerVisual.SetPositionAndRotation(grip, hRot);
            }
        }

        private IEnumerator SwingRoutine(float force)
        {
            _swinging = true;
            _swingT = 0f;
            float dur = Mathf.Lerp(0.09f, 0.14f, force);
            while (_swingT < 1f)
            {
                _swingT += Time.deltaTime / dur;
                yield return null;
            }
            _swingT = 1f;
            DoStrike(force);
            yield return new WaitForSeconds(0.08f);
            _charge = 0f;
            _swinging = false;
        }

        private void DoStrike(float force)
        {
            var session = GameSession.Instance;
            var geo = _rock.Visual.Geometry;
            Vector3 local = _rock.transform.InverseTransformPoint(_aimPoint);
            float azimuth = Mathf.Atan2(local.z, local.x);
            float planeOffset = Mathf.Clamp(local.y / Mathf.Max(0.01f, geo.MaxRadius), -1f, 1f);
            Vector3 toCam = (_cam.transform.position - _aimPoint).normalized;
            float angle = Mathf.Clamp01(Vector3.Dot(_aimNormal, toCam) * 1.15f);
            var input = new StressModel.StrikeInput { Azimuth = azimuth, PlaneOffset = planeOffset, Force = force, AngleFactor = angle };
            var result = _model.Strike(input, ref _rng);
            _lastResult = result;

            // commit
            var rec = _rock.Record;
            rec.SectorStress = _model.ToArray();
            rec.StrikeCount = _model.StrikeCount;
            session.State.Stats.TotalStrikes++;

            // feedback
            string clip = force < 0.4f ? "tap_light" : force < 0.75f ? "tap_medium" : "tap_heavy";
            if (result.Slipped)
            {
                WorkshopAudio.Play("slip", _aimPoint, 0.8f);
                _controller?.Impulse(0.15f * force);
            }
            else
            {
                WorkshopAudio.Play(clip, _aimPoint, Mathf.Lerp(0.6f, 1f, force), Mathf.Lerp(1.1f, 0.85f, force) * (result.Placement > 0.6f ? 1f : 1.12f));
                if (result.NewCrack) { WorkshopAudio.Play("tick", _aimPoint, 0.9f, 0.9f); WorkshopAudio.Play("creak", _aimPoint, 0.35f, 1.1f); }
                else if (result.StressAdded > 0.4f && _rng.Chance(0.35f)) WorkshopAudio.Play("tick", _aimPoint, 0.4f, 1.2f);
                _controller?.Impulse((0.2f + 0.55f * force) * (result.NewCrack ? 1.3f : 1f));
                EffectsFactory.Instance?.Impact(_aimPoint, _aimNormal, force * (result.Placement * 0.6f + 0.4f));
            }
            var bottomLocal = _rock.Visual.BottomHalf.InverseTransformPoint(_aimPoint);
            var bottomNormal = _rock.Visual.BottomHalf.InverseTransformDirection(_aimNormal);
            _ribbon?.AddImpactMark(bottomLocal, bottomNormal, geo.MaxRadius * (0.04f + force * 0.05f), 0.35f + force * 0.4f);
            _ribbon?.Refresh(_model, HasLamp);

            if (result.Damaged && !result.Opened)
            {
                ApplyDamage(result.DamageSeverity, azimuth);
                WorkshopAudio.Play("crystal_break", _aimPoint, 0.5f, 0.85f);
            }

            Tutorial.Notify("first_strike");
            Struck?.Invoke(result);

            if (result.Opened)
            {
                if (result.Shattered) { ApplyDamage(0.9f, azimuth); ApplyDamage(0.7f, azimuth + 2.5f); }
                StartCoroutine(RevealRoutine(result));
            }
            else
            {
                session.FlushSave("strike");
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
            int count = Mathf.Clamp(1 + Mathf.RoundToInt(severity * 3f), 1, candidates.Count);
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
            _damageThisRock++;
            GameSession.Instance.State.Stats.SpecimensDamaged += _damageThisRock == 1 ? 1 : 0;
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
            session.FlushSave("opened");

            bool rare = g.Tier >= QualityTier.Exceptional;
            bool attractive = g.Tier >= QualityTier.Good;
            RestTools();

            // the split
            WorkshopAudio.Play("crack_final", _rock.transform.position, 1f, rare ? 0.92f : 1f);
            WorkshopAudio.Play("fragments", _rock.transform.position, 0.8f);
            _controller?.Impulse(0.9f);
            EffectsFactory.Instance?.Split(_rock.transform.position, geo.MeanEquatorRadius, _cam.transform.forward);
            if (_ribbon != null)
            {
                for (int i = 0; i < StressModel.Sectors; i++) _model.Stress[i] = Mathf.Max(_model.Stress[i], 1f);
                _ribbon.Refresh(_model, false);
            }
            vis.RebuildCrystals();
            vis.SetCrystalsVisible(true);
            _rock.RebuildColliders();
            _rock.SetStaticCollidable();

            // reveal light in the cavity
            var lightGo = new GameObject("RevealLight");
            lightGo.transform.position = _rock.transform.position + Vector3.up * geo.MaxRadius * 0.9f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = geo.MaxRadius * 6f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 0f;
            light.shadows = LightShadows.None;

            if (rare)
            {
                Time.timeScale = 0.5f;
                StartCoroutine(RestoreTimeScale(0.9f));
                StartCoroutine(DelayedSting(0.4f));
            }

            // hinge the top half open sideways (to the player's left) so both halves stay in view like an open book
            var top = vis.TopHalf;
            Vector3 camFlat = _cam.transform.position - _rock.transform.position; camFlat.y = 0f; camFlat.Normalize();
            Vector3 camRight = Vector3.Cross(Vector3.up, camFlat).normalized;   // player's right
            Vector3 fLocal = _rock.transform.InverseTransformDirection(camRight);
            float R = geo.MeanEquatorRadius;
            Vector3 hinge = -fLocal * R;
            Vector3 axis = Vector3.Cross(Vector3.up, fLocal).normalized;
            Vector3 startPos = top.localPosition;
            Quaternion startRot = top.localRotation;
            // where the flipped half will come to rest: on whatever surface lies under its landing spot
            Vector3 landLocal = hinge + Quaternion.AngleAxis(178f, axis) * (startPos - hinge);
            Vector3 landWorld = _rock.transform.TransformPoint(landLocal);
            float surfaceY = _rock.transform.position.y + geo.BottomY;
            var downRay = new Ray(landWorld + Vector3.up * 0.25f, Vector3.down);
            foreach (var h in Physics.RaycastAll(downRay, 0.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.GetComponentInParent<SpecimenEntity>() == _rock) continue;
                if (h.point.y > surfaceY - 0.3f) { surfaceY = Mathf.Max(surfaceY - 0.3f, h.point.y); break; }
            }
            float finalLift = (surfaceY + geo.TopY) - _rock.transform.TransformPoint(landLocal).y;
            float dur = rare ? 1.5f : 1.15f;
            // dolly the bench camera in to admire the interior
            Vector3 camFrom = CameraAnchor.position; Quaternion camFromRot = CameraAnchor.rotation;
            Vector3 camTo = _rock.transform.position - camRight * geo.MaxRadius * 1.1f + camFlat * (geo.MaxRadius * 1.6f + 0.04f) + Vector3.up * (geo.MaxRadius * 1.9f + 0.03f);
            Quaternion camToRot = Quaternion.LookRotation((_rock.transform.position - camRight * geo.MaxRadius * 1.1f + Vector3.up * geo.MaxRadius * 0.2f) - camTo, Vector3.up);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 2.2f);
                float angle = Mathf.Lerp(0f, 178f, e);
                float lift = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * R * 0.28f;
                var rot = Quaternion.AngleAxis(angle, axis);
                Vector3 pos = hinge + rot * (startPos - hinge) + Vector3.up * (lift + finalLift * e);
                top.localPosition = pos;
                top.localRotation = rot * startRot;
                light.intensity = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * (rare ? 2.4f : 1.5f);
                float ct = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.15f) / 0.85f));
                CameraAnchor.SetPositionAndRotation(Vector3.Lerp(camFrom, camTo, ct), Quaternion.Slerp(camFromRot, camToRot, ct));
                if (t > 0.35f && t < 0.4f) EffectsFactory.Instance?.Glints(_rock.transform.position + Vector3.up * R * 0.4f, R * 0.7f, attractive ? 18 : 8, SpecimenVisual.ApplySaturation(g.Palette.SurfaceA, g.Saturation));
                yield return null;
            }
            CameraAnchor.SetPositionAndRotation(camTo, camToRot);
            top.localPosition = hinge + Quaternion.AngleAxis(178f, axis) * (startPos - hinge) + Vector3.up * finalLift;
            top.localRotation = Quaternion.AngleAxis(178f, axis) * startRot;
            WorkshopAudio.Play("rock_place", _rock.transform.TransformPoint(top.localPosition), 0.5f, 0.9f);
            _rock.RebuildColliders();
            _rock.SetStaticCollidable();
            StartCoroutine(FadeLight(light, 1.2f));
            if (_ribbon != null) StartCoroutine(FadeRibbon(_ribbon, 0.8f));
            _ribbon = null;

            float damage = vis.CrystalDamageFraction();
            session.RecordDiscovery(rec, damage);
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

        private static IEnumerator FadeRibbon(CrackRibbon r, float dur)
        {
            float t = 0f;
            var rends = r.GetComponentsInChildren<Renderer>();
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                if (r == null) yield break;
                yield return null;
            }
            if (r != null) Destroy(r.gameObject);
        }
    }
}
