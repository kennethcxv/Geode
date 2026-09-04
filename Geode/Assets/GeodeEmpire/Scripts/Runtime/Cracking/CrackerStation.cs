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
    /// The geode cracker: a chain splitter in the Stage-2 workshop, the third way to open rock. The rock rests across
    /// two rubber rails, a chain of hardened links is laid round its seam (the chain is built along the rock's own
    /// seam ring, so it sits on the shell wherever the seam runs), the lever takes up the slack, and pumping it
    /// squeezes the ring until the shell lets go all the way round at once. It is slower to set than a hammer and
    /// takes nothing bigger than the chain, but a level, well-seated rock splits clean with far less crystal damage
    /// than careless hammering. A rock set with its seam off level rides the chain up the shell and slips under
    /// pressure; a thin shell squeezed too hard shatters.
    /// </summary>
    public sealed class CrackerStation : InteractableBehaviour
    {
        public enum Phase { Idle, Seat, Tighten, Pressure, Splitting, Done }

        public PlacementZone Bed;
        public Transform BedCenter;
        public Transform Lever;          // pumps about its local Y (+X bar rises)
        public Transform GaugeNeedle;    // turns about its local Z
        public Transform Anchor;         // where the chain's tails meet the drum
        public Transform CameraAnchor;
        public GameObject Machine;
        public Material ChainMaterial;

        public float RailCrestY = 0.94f;
        public float MaxRockRadius = 0.11f;
        [NonSerialized] public float TiltSpeed = 45f, TiltLimit = 35f;
        [NonSerialized] public float PressureRate = 0.32f;      // per second with the lever pumping
        [NonSerialized] public float TightenSeconds = 1.3f;
        [NonSerialized] public Vector3 CamLocalPos = new Vector3(0.36f, 1.42f, -0.62f);
        [NonSerialized] public Vector3 CamLookLocal = new Vector3(0.02f, 0.98f, 0f);

        public bool Active { get; private set; }
        public Phase State { get; private set; } = Phase.Idle;
        public SpecimenEntity Rock => _rock;
        public float Pressure => _pressure;
        public float SplitPressure => _splitPressure;
        public float TiltAngle => _rock != null ? Vector3.Angle(_rock.transform.up, Vector3.up) : 0f;
        public float Tighten => _tighten;
        public string ResultNote { get; private set; } = "";
        public string Note { get; private set; } = "";
        public int Slips { get; private set; }
        public bool Owned => GameSession.Instance != null && GameSession.Instance.State != null && UpgradeCatalog.Has(GameSession.Instance.State, UpgradeCatalog.GeodeCracker);
        /// <summary>How the seam sits under the chain: level is right, past ~8 degrees it starts to ride, past ~20 it slips.</summary>
        public string AlignmentWord => TiltAngle < 8f ? "level" : TiltAngle < 20f ? "off level" : "well off level";

        public event Action Entered, Exited, Revealed;

        private SpecimenEntity _rock;
        private float _pressure, _splitPressure, _tighten, _leverAngle, _needleAngle;
        private Quaternion _rockRotBase = Quaternion.identity;
        private Vector3 _rockBasePos;
        private float _pumpPhase, _creakTimer, _rockKick;
        private SeededRandom _rng;
        private FirstPersonController _controller;
        private PlayerInteractor _player;
        private Camera _cam;
        private GameObject _chain, _tails;
        private Mesh _chainMesh, _tailMesh;
        private AudioSource _strainLoop;
        private int _enterFrame;

        // ------------------------------------------------------------------------------------
        protected override void Awake()
        {
            base.Awake();
            if (Bed != null)
            {
                Bed.Placed += OnPlaced;
                Bed.Taken += OnTaken;
                Bed.ExtraRefusal = Refusal;
                Bed.ResumePrompt = occ => occ != null && !occ.IsOpened && !Active ? "Set the chain  (E)" : null;
                Bed.ResumeAction = occ => { if (occ != null && !occ.IsOpened && !Active) Enter(occ); };
            }
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
            WorkshopAudio.StopLoop(_strainLoop);
            if (_chainMesh != null) Destroy(_chainMesh);
            if (_tailMesh != null) Destroy(_tailMesh);
        }

        public void RefreshOwned()
        {
            bool owned = Owned;
            if (Machine != null) Machine.SetActive(owned);
            if (Bed != null) Bed.Locked = !owned;
        }

        private string Refusal(SpecimenEntity e)
        {
            if (!Owned) return "Buy the Geode Cracker on the tablet";
            if (e.IsOpened) return "Already open";
            if (e.IsPiece) return "The chain wants a whole rock";
            if (e.Radius > MaxRockRadius) return "The chain is too short for a rock that size";
            return null;
        }

        public override bool CanInteract(PlayerInteractor player) => false;
        public override string GetPrompt(PlayerInteractor player) => "";
        public override void Interact(PlayerInteractor player) { }

        private void OnPlaced(PlacementZone zone, SpecimenEntity e)
        {
            WorkshopAudio.Play("rock_place", e.transform.position, 0.8f);
            if (!e.IsOpened) Enter(e);
        }

        private void OnTaken(PlacementZone zone, SpecimenEntity e)
        {
            if (_rock == e && Active) Exit();
        }

        public void Resume()
        {
            var e = Bed != null ? Bed.First : null;
            if (e != null && !e.IsOpened) Enter(e);
        }

        // ------------------------------------------------------------------------------------
        public void Enter(SpecimenEntity e)
        {
            if (Active || e == null) return;
            var session = GameSession.Instance;
            _enterFrame = Time.frameCount;
            _rock = e; _rock.Locked = true; _rock.SetStaticCollidable();
            Active = true; State = Phase.Seat;
            ResultNote = ""; Note = ""; Slips = 0;
            _pressure = 0f; _tighten = 0f; _pumpPhase = 0f; _rockKick = 0f;
            var g = e.Geology;
            // what it takes to part this shell all the way round: thick, tough, well-seamed shells resist; the chain
            // squeezes every sector at once, so the average thickness counts, and the seam quality helps a lot
            float thick = Mathf.InverseLerp(0.08f, 0.5f, g.ShellThickness);
            float seam = Mathf.Lerp(1.25f, 0.8f, g.SeamQuality);
            float size = Mathf.Lerp(0.75f, 1.2f, Mathf.InverseLerp(0.035f, 0.11f, e.Radius));
            _splitPressure = Mathf.Clamp(0.35f + 0.5f * thick, 0.3f, 0.95f) * Mathf.Clamp(g.Family.ShellToughness, 0.6f, 1.5f) * seam * size;
            _splitPressure = Mathf.Clamp(_splitPressure, 0.25f, 1.6f);
            _rng = new SeededRandom(SeededRandom.Combine(e.Record.Seed, 977UL));
            if (!e.Record.ProcessingStarted) { e.Record.ProcessingStarted = true; session.FlushSave("cracker-enter"); }
            _rockRotBase = _rock.transform.rotation;
            ReseatRock();
            BuildChain();
            FrameCamera();
            if (_controller != null) _controller.EnterStationView(CameraAnchor);
            if (_player != null) _player.InputLocked = true;
            _rock.Visual.SetCrackState(_rock.Record.SectorStress, _rock.Record.Impacts, 1f, 1f);
            Tutorial.Notify("rock_in_cracker");
            Entered?.Invoke();
        }

        public void Exit()
        {
            if (!Active || State == Phase.Splitting) return;
            CursorController.MarkInputConsumed();
            Active = false;
            if (_controller != null) _controller.ExitStationView();
            if (_player != null) _player.InputLocked = false;
            WorkshopAudio.StopLoop(_strainLoop); _strainLoop = null;
            if (_rock != null)
            {
                _rock.Locked = false;
                if (State != Phase.Done) { _rock.transform.rotation = _rockRotBase; _rock.transform.position = _rockBasePos; }
            }
            ClearChain();
            _pressure = 0f; _tighten = 0f;
            if (State != Phase.Done) State = Phase.Idle;
            _rock = null;
            Exited?.Invoke();
            GameSession.Instance?.QueueSave("cracker-exit");
        }

        private void FrameCamera()
        {
            if (CameraAnchor == null) return;
            Vector3 pos = transform.TransformPoint(CamLocalPos);
            Vector3 look = transform.TransformPoint(CamLookLocal + Vector3.up * (_rock != null ? _rock.Radius * 0.5f : 0f));
            CameraAnchor.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
        }

        /// <summary>The rock rests on the rails on its real lowest lump, centred between them.</summary>
        private void ReseatRock()
        {
            Vector3 c = BedCenter != null ? BedCenter.position : transform.TransformPoint(0f, RailCrestY, 0f);
            _rock.transform.position = new Vector3(c.x, c.y - _rock.LowestPointOffset(_rock.transform.rotation), c.z);
            _rockBasePos = _rock.transform.position;
        }

        private void Tilt(Vector2 input)
        {
            if (_rock == null || _cam == null) return;
            Vector3 camFlat = _cam.transform.forward; camFlat.y = 0f; camFlat.Normalize();
            Vector3 camRight = Vector3.Cross(Vector3.up, camFlat).normalized;
            var delta = Quaternion.AngleAxis(input.y * TiltSpeed, camRight) * Quaternion.AngleAxis(-input.x * TiltSpeed, camFlat);
            var next = delta * _rockRotBase;
            if (Vector3.Angle(next * Vector3.up, Vector3.up) > TiltLimit) return;
            _rockRotBase = next; _rock.transform.rotation = next;
            ReseatRock();
        }

        // ---- the chain: a run of links along the rock's own seam ring, plus two tails up to the drum ----
        private void BuildChain()
        {
            ClearChain();
            if (_rock == null || _rock.Visual == null || _rock.Visual.Geometry == null) return;
            var geo = _rock.Visual.Geometry;
            int N = geo.Longitudes;
            var pts = new List<Vector3>(N + 1);
            for (int i = 0; i <= N; i++)
            {
                int k = i % N;
                float a = k / (float)N * Mathf.PI * 2f;
                float r = geo.Bottom.EquatorOuterRadius[k] + 0.007f;
                pts.Add(new Vector3(Mathf.Cos(a) * r, geo.Bottom.EquatorY[k], Mathf.Sin(a) * r));
            }
            _chainMesh = TubeMesh(pts, 0.0065f, 10, true, 0.02f);
            _chain = new GameObject("Chain");
            _chain.transform.SetParent(_rock.transform, false);
            _chain.AddComponent<MeshFilter>().sharedMesh = _chainMesh;
            var mr = _chain.AddComponent<MeshRenderer>();
            mr.sharedMaterial = ChainMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _tails = new GameObject("ChainTails");
            _tails.transform.SetParent(transform, false);
            _tails.AddComponent<MeshFilter>();
            var tr = _tails.AddComponent<MeshRenderer>();
            tr.sharedMaterial = ChainMaterial;
            UpdateTails();
        }

        private void ClearChain()
        {
            if (_chain != null) Destroy(_chain);
            if (_tails != null) Destroy(_tails);
            if (_chainMesh != null) Destroy(_chainMesh);
            if (_tailMesh != null) Destroy(_tailMesh);
            _chain = null; _tails = null; _chainMesh = null; _tailMesh = null;
        }

        /// <summary>Two runs from the back of the ring up to the anchor drum: slack sags, a tightened chain is straight.</summary>
        private void UpdateTails()
        {
            if (_tails == null || _rock == null || Anchor == null) return;
            var geo = _rock.Visual.Geometry;
            // the ring's rearmost point (toward the post, station +Z) in station space
            Vector3 best = Vector3.zero; float bestZ = float.MinValue;
            int N = geo.Longitudes;
            for (int k = 0; k < N; k++)
            {
                float a = k / (float)N * Mathf.PI * 2f;
                float r = geo.Bottom.EquatorOuterRadius[k] + 0.007f;
                Vector3 w = _rock.transform.TransformPoint(new Vector3(Mathf.Cos(a) * r, geo.Bottom.EquatorY[k], Mathf.Sin(a) * r));
                Vector3 l = transform.InverseTransformPoint(w);
                if (l.z > bestZ) { bestZ = l.z; best = l; }
            }
            Vector3 drum = transform.InverseTransformPoint(Anchor.position);
            float slack = (1f - Mathf.Clamp01(_tighten)) * 0.05f;
            var pts = new List<Vector3>(12);
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                Vector3 p = Vector3.Lerp(best, drum, t);
                p.y -= Mathf.Sin(t * Mathf.PI) * slack;
                pts.Add(p);
            }
            if (_tailMesh != null) Destroy(_tailMesh);
            _tailMesh = TubeMesh(pts, 0.006f, 8, false, 0.02f);
            _tails.GetComponent<MeshFilter>().sharedMesh = _tailMesh;
        }

        /// <summary>A tube along a polyline with a link pattern in the vertex colour (dark every other link).</summary>
        private static Mesh TubeMesh(List<Vector3> pts, float radius, int segs, bool closed, float linkLen)
        {
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var cols = new List<Color>(); var tris = new List<int>();
            int n = pts.Count;
            float dist = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = pts[i];
                Vector3 tan = (i < n - 1 ? pts[i + 1] - p : p - pts[i - 1]).normalized;
                if (i > 0) dist += (p - pts[i - 1]).magnitude;
                Vector3 side = Vector3.Cross(tan, Vector3.up); if (side.sqrMagnitude < 1e-5f) side = Vector3.Cross(tan, Vector3.right);
                side.Normalize();
                Vector3 up = Vector3.Cross(side, tan).normalized;
                bool dark = ((int)(dist / linkLen)) % 2 == 0;
                float rr = radius * (dark ? 0.85f : 1.05f);
                for (int s = 0; s < segs; s++)
                {
                    float a = s / (float)segs * Mathf.PI * 2f;
                    Vector3 nrm = Mathf.Cos(a) * side + Mathf.Sin(a) * up;
                    verts.Add(p + nrm * rr); norms.Add(nrm); cols.Add(dark ? new Color(0.35f, 0.35f, 0.37f) : Color.white);
                }
            }
            for (int i = 0; i < n - 1; i++)
                for (int s = 0; s < segs; s++)
                {
                    int a = i * segs + s, b = i * segs + (s + 1) % segs, c = (i + 1) * segs + s, d = (i + 1) * segs + (s + 1) % segs;
                    tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d);
                }
            var m = new Mesh { name = "Chain" };
            m.SetVertices(verts); m.SetNormals(norms); m.SetColors(cols); m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        // ------------------------------------------------------------------------------------
        private void Update()
        {
            float dt = Time.deltaTime;
            // the lever and the needle
            float wantLever = State == Phase.Tighten ? Mathf.Lerp(0f, -25f, Mathf.PingPong(_pumpPhase, 1f)) : State == Phase.Pressure ? Mathf.Lerp(0f, -30f, Mathf.PingPong(_pumpPhase, 1f)) : 0f;
            _leverAngle = Mathf.Lerp(_leverAngle, wantLever, 1f - Mathf.Exp(-dt * 12f));
            if (Lever != null) Lever.localRotation = Quaternion.AngleAxis(_leverAngle, Vector3.up);
            float wantNeedle = Mathf.Lerp(-60f, 60f, Mathf.Clamp01(_pressure / 1.6f));
            _needleAngle = Mathf.Lerp(_needleAngle, wantNeedle, 1f - Mathf.Exp(-dt * 8f));
            if (GaugeNeedle != null) GaugeNeedle.localRotation = Quaternion.AngleAxis(_needleAngle, Vector3.forward);
            if (!Active || _rock == null) return;
            _rockKick = Mathf.MoveTowards(_rockKick, 0f, dt * 8f);
            _rock.transform.position = _rockBasePos + Vector3.down * (0.002f * _rockKick);
            switch (State)
            {
                case Phase.Seat: UpdateSeat(dt); break;
                case Phase.Tighten: UpdateTighten(dt); break;
                case Phase.Pressure: UpdatePressure(dt); break;
                case Phase.Done:
                    if (GameInput.InteractPressed && Time.frameCount > _enterFrame + 1)
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
                    break;
            }
        }

        private void UpdateSeat(float dt)
        {
            if (GameInput.BackPressed) { Exit(); return; }
            float rot = GameInput.Rotate * 90f * dt + GameInput.Scroll.y * 0.12f;
            bool changed = false;
            if (Mathf.Abs(rot) > 0.0001f)
            {
                _rock.transform.rotation = _rockRotBase;
                _rock.transform.RotateAround(_rockBasePos, Vector3.up, rot);
                _rockRotBase = _rock.transform.rotation; changed = true;
            }
            Vector2 mv = GameInput.Move;
            if (mv.sqrMagnitude > 0.09f) { Tilt(mv * dt); changed = true; }
            if (changed) UpdateTails();
            if (GameInput.InteractPressed && Time.frameCount > _enterFrame + 1) { State = Phase.Tighten; _pumpPhase = 0f; Note = ""; }
        }

        private void UpdateTighten(float dt)
        {
            if (GameInput.BackPressed) { Exit(); return; }
            // hold to pump the slack out; let go and it stays where it is
            if (GameInput.InteractHeld)
            {
                _pumpPhase += dt * 3f;
                float before = _tighten;
                _tighten = Mathf.Clamp01(_tighten + dt / TightenSeconds);
                if (Mathf.FloorToInt(before * 4f) != Mathf.FloorToInt(_tighten * 4f)) { WorkshopAudio.Play("clamp", Anchor != null ? Anchor.position : transform.position, 0.5f, 1.2f + 0.2f * _tighten); Haptics.Pulse(0.15f, 0.1f, 0.05f); }
                UpdateTails();
                if (_tighten >= 1f)
                {
                    State = Phase.Pressure; _pumpPhase = 0f;
                    WorkshopAudio.Play("tick", _rock.transform.position, 0.6f, 0.8f);
                    if (_strainLoop == null) _strainLoop = WorkshopAudio.StartLoop("saw_grind", _rock.transform.position, 0.001f, 0.4f);
                }
            }
            else _pumpPhase = 0f;
        }

        private void UpdatePressure(float dt)
        {
            if (GameInput.BackPressed && _pressure < 0.05f) { Exit(); return; }
            var g = _rock.Geology;
            bool pumping = GameInput.StrikeHeld || GameInput.InteractHeld;
            if (pumping)
            {
                _pumpPhase += dt * 2.6f;
                _pressure = Mathf.Min(1.6f, _pressure + dt * PressureRate);
            }
            else { _pumpPhase = 0f; _pressure = Mathf.MoveTowards(_pressure, _pressure * 0.98f, dt * 0.02f); }   // a ratchet: it holds
            float frac = _pressure / _splitPressure;
            // the shell answers: the stress ring rises evenly (thin sectors first), groans, then lets go
            var stress = _rock.Record.SectorStress;
            if (stress == null || stress.Length != StressModel.Sectors) { stress = new float[StressModel.Sectors]; _rock.Record.SectorStress = stress; }
            for (int i = 0; i < StressModel.Sectors; i++)
            {
                float t = g.SectorThicknessAt(i);
                stress[i] = Mathf.Max(stress[i], Mathf.Clamp01(frac * (1.15f / Mathf.Max(0.6f, t)) * 0.92f));
            }
            _rock.Visual.SetCrackState(stress, _rock.Record.Impacts, 1f, 1f);
            WorkshopAudio.SetLoop(_strainLoop, Mathf.Clamp01(frac) * 0.25f, 0.55f + 0.5f * Mathf.Clamp01(frac));
            _creakTimer -= dt;
            if (pumping && _creakTimer <= 0f)
            {
                _creakTimer = Mathf.Lerp(0.6f, 0.18f, Mathf.Clamp01(frac));
                if (frac > 0.35f) WorkshopAudio.Play("creak", _rock.transform.position, 0.25f + 0.5f * Mathf.Clamp01(frac), 0.85f + 0.3f * frac);
                if (frac > 0.7f) WorkshopAudio.Play("tension", _rock.transform.position, 0.4f * Mathf.Clamp01(frac), 0.9f + 0.2f * frac);
                if (frac > 0.5f && _rng.Chance(0.5f)) WorkshopAudio.Play("tick", _rock.transform.position, 0.3f, 1.1f);
                Haptics.Pulse(0.1f + 0.5f * Mathf.Clamp01(frac), 0.05f + 0.2f * Mathf.Clamp01(frac), 0.08f);
                _rockKick = 0.6f;
            }
            // a chain on a seam that is not level rides up the shell under load and lets go all at once
            float tilt = TiltAngle;
            if (tilt > 8f && frac > Mathf.Lerp(0.95f, 0.25f, Mathf.InverseLerp(8f, 25f, tilt)))
            {
                Slip();
                return;
            }
            if (frac >= 1f) StartCoroutine(Split(frac));
        }

        private void Slip()
        {
            Slips++;
            _pressure = 0f; _tighten = 0f; State = Phase.Tighten; _pumpPhase = 0f;
            WorkshopAudio.StopLoop(_strainLoop); _strainLoop = null;
            WorkshopAudio.Play("slip", _rock.transform.position, 0.9f);
            WorkshopAudio.Play("wood_knock", _rock.transform.position, 0.5f, 0.75f);
            Haptics.Pulse(0.7f, 0.4f, 0.12f);
            _controller?.Impulse(0.25f);
            // the rock jumps in the rails
            _rock.transform.rotation = _rockRotBase;
            _rock.transform.RotateAround(_rockBasePos, Vector3.up, _rng.Range(-14f, 14f));
            _rockRotBase = _rock.transform.rotation;
            _rockKick = 1f;
            // a slipped chain chips the shell where it rode
            var rec = _rock.Record;
            float lon = _rng.NextFloat();
            rec.Impacts.Add(new Vector4(lon, _rng.Range(-0.15f, 0.15f), _rock.Radius * 0.12f, 0.6f));
            rec.ShellDamage = Mathf.Clamp01(rec.ShellDamage + 0.02f);
            _rock.Visual.SetCrackState(rec.SectorStress, rec.Impacts, 1f, 1f);
            UpdateTails();
            Note = "the chain rode up off the seam and let go: level the rock and take up the slack again";
        }

        private IEnumerator Split(float frac)
        {
            State = Phase.Splitting;
            var session = GameSession.Instance;
            var rec = _rock.Record;
            var vis = _rock.Visual;
            var geo = vis.Geometry;
            var g = _rock.Geology;
            WorkshopAudio.StopLoop(_strainLoop); _strainLoop = null;
            // damage: a shell taken cleanly at its split pressure keeps its crystals; a thin shell forced past it, or a
            // slipped-and-retried rock, loses some near the seam
            float over = Mathf.Clamp01((_pressure - _splitPressure) / 0.4f);
            float thin = Mathf.InverseLerp(0.25f, 0.08f, g.ShellThickness);
            float severity = Mathf.Clamp01(over * (0.4f + 0.6f * thin) + Slips * 0.12f);
            bool shattered = over > 0.6f && thin > 0.6f;
            if (shattered) severity = Mathf.Max(severity, 0.75f);
            if (severity > 0.05f) DamageNearSeam(severity);
            for (int i = 0; i < StressModel.Sectors; i++) rec.SectorStress[i] = 1f;
            rec.Condition.Opened = true;
            rec.OpenedAtTicks = DateTime.UtcNow.Ticks;
            rec.ProcessedBy = "cracker";
            rec.StrikeCount += 0;
            session.State.Stats.RocksProcessed++;
            float damage = vis.CrystalDamageFraction();
            rec.DamageFraction = damage;
            session.RecordDiscovery(rec, damage);
            session.FlushSave("cracker-opened");
            bool rare = g.Tier >= QualityTier.Exceptional;
            WorkshopAudio.Play("crack_final", _rock.transform.position, 1f, rare ? 0.92f : 1.05f);
            MusicPlayer.Instance?.Duck(rare ? 4f : 2f);
            WorkshopAudio.Play("fragments", _rock.transform.position, 0.6f);
            _controller?.Impulse(0.7f);
            Haptics.Pulse(0.9f, 0.6f, 0.2f);
            EffectsFactory.Instance?.Split(_rock.transform.position, geo.MeanEquatorRadius, _cam.transform.forward);
            ClearChain();
            vis.RebuildCrystals();
            vis.SetCrystalsVisible(true);
            vis.SetCrackState(rec.SectorStress, rec.Impacts, 0f, 1f);
            _rock.RebuildColliders();
            _rock.SetStaticCollidable();
            // the chain has parted the halves along the seam: the top lifts off and comes to rest beside the bottom on
            // the base plate, to the operator's left, where the plate is clear of the post
            var top = vis.TopHalf;
            Vector3 startPos = top.localPosition; Quaternion startRot = top.localRotation;
            _rock.TopPoseFor(DisplayPose.SideBySide, out var landLocal, out var landRot);
            // the side-by-side pose lies along the rock's -X: turn the rock so that lands to the operator's left (station -X)
            Vector3 landWorldDir = _rock.transform.TransformDirection(new Vector3(-1f, 0f, 0f));
            Vector3 wantDir = -transform.right;
            float turn = Vector3.SignedAngle(landWorldDir, wantDir, Vector3.up);
            _rock.transform.RotateAround(_rockBasePos, Vector3.up, turn);
            _rockRotBase = _rock.transform.rotation;
            // both halves rest on the plate: the bottom half sits where it did, the top lands beside it. The rails run
            // across the plate 8 cm either side of centre: a half that lands over a rail rests on the rail's crest
            float plateY = transform.TransformPoint(0f, RailCrestY - 0.04f, 0f).y;
            Vector3 landWorld = _rock.transform.TransformPoint(landLocal);
            float landX = transform.InverseTransformPoint(landWorld).x;
            float halfW = geo.MaxRadius;
            bool overRail = Mathf.Abs(Mathf.Abs(landX) - 0.08f) < halfW + 0.02f;
            float restY = overRail ? transform.TransformPoint(0f, RailCrestY, 0f).y : plateY;
            float lift = (restY - _rock.LowestOfTop(landRot)) - landWorld.y;
            var light = new GameObject("RevealLight").AddComponent<Light>();
            light.transform.position = _rock.transform.position + Vector3.up * geo.MaxRadius;
            light.type = LightType.Point; light.range = geo.MaxRadius * 6f; light.color = new Color(0.93f, 0.96f, 1f); light.intensity = 0f; light.shadows = LightShadows.None;
            Vector3 camFrom = CameraAnchor.position; Quaternion camFromRot = CameraAnchor.rotation;
            Vector3 mid = _rock.transform.position + (landWorld - _rock.transform.position) * 0.5f;
            Vector3 camTo = mid + (-transform.forward) * 0.42f + Vector3.up * 0.4f;
            Quaternion camToRot = Quaternion.LookRotation((mid + Vector3.up * geo.MaxRadius * 0.15f) - camTo, Vector3.up);
            float t = 0f; float dur = rare ? 1.4f : 1.0f;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 2.2f);
                float hop = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * geo.MeanEquatorRadius * 0.6f;
                top.localPosition = Vector3.Lerp(startPos, landLocal + Vector3.up * lift, e) + Vector3.up * hop;
                top.localRotation = Quaternion.Slerp(startRot, landRot, e);
                light.intensity = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * (rare ? 1.5f : 0.9f);
                float ct = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.1f) / 0.9f));
                CameraAnchor.SetPositionAndRotation(Vector3.Lerp(camFrom, camTo, ct), Quaternion.Slerp(camFromRot, camToRot, ct));
                if (t > 0.3f && t < 0.36f) EffectsFactory.Instance?.Glints(_rock.transform.position + Vector3.up * geo.MeanEquatorRadius * 0.4f, geo.MeanEquatorRadius * 0.7f, g.Tier >= QualityTier.Good ? 18 : 8, SpecimenVisual.ApplySaturation(g.Palette.SurfaceA, g.Saturation));
                yield return null;
            }
            top.localPosition = landLocal + Vector3.up * lift; top.localRotation = landRot;
            WorkshopAudio.Play("rock_place", _rock.transform.TransformPoint(top.localPosition), 0.5f, 0.9f);
            _rock.RebuildColliders(); _rock.SetStaticCollidable();
            _rock.CommitOpenPose();
            StartCoroutine(FadeLight(light, 1.2f));
            string tier = Valuation.TierLabel(Valuation.TierFromValue(Valuation.DamagedValue(g, damage, 0f)));
            string dmg = shattered ? "Shattered: forced past what the shell would take" : damage <= 0.001f ? "Clean split" : damage < 0.12f ? "Minor chipping" : damage < 0.35f ? "Noticeable damage" : "Heavy damage";
            ResultNote = $"{tier}  •  {dmg}  •  {_pressure / _splitPressure * 100f:F0}% of the shell's limit" + (Slips > 0 ? $"  •  slipped {Slips}x" : "");
            string call = session.ScoreCall(rec);
            if (!string.IsNullOrEmpty(call)) ResultNote += "  •  " + call;
            session.State.Stats.RocksCracked++;
            if (rare) WorkshopAudio.Play2D("discovery", 0.55f);
            State = Phase.Done;
            Tutorial.Notify("rock_opened");
            Revealed?.Invoke();
            session.FlushSave("cracker-revealed");
        }

        private void DamageNearSeam(float severity)
        {
            var geo = _rock.Visual.Geometry;
            var cond = _rock.Record.Condition;
            cond.EnsureSize(geo.Crystals.Count);
            var near = new List<CrystalInstance>();
            foreach (var c in geo.Crystals) if (c.Latitude < 0.45f && cond.DamageAt(c.Index) < CrystalDamage.Missing) near.Add(c);
            if (near.Count == 0) return;
            int count = Mathf.Clamp(Mathf.RoundToInt(severity * (2f + near.Count * 0.12f)), 1, near.Count);
            for (int i = 0; i < count; i++)
            {
                var c = near[_rng.Range(0, near.Count)];
                byte next = severity > 0.7f && i == 0 ? CrystalDamage.Missing : severity > 0.45f ? CrystalDamage.Broken : CrystalDamage.Chipped;
                if (cond.DamageAt(c.Index) < next) cond.CrystalDamage[c.Index] = next;
            }
            _rock.Record.DamageEvents++;
            _rock.Record.ShellDamage = Mathf.Clamp01(_rock.Record.ShellDamage + severity * 0.1f);
            if (_rock.Record.DamageEvents == 1) GameSession.Instance.State.Stats.SpecimensDamaged++;
        }

        private static IEnumerator FadeLight(Light l, float dur)
        {
            float start = l.intensity, t = 0f;
            while (t < 1f) { t += Time.deltaTime / dur; if (l == null) yield break; l.intensity = Mathf.Lerp(start, 0f, t); yield return null; }
            if (l != null) Destroy(l.gameObject);
        }

        // ---- dev / harness ----
        public void DevSeat(float yawDeg, float tiltDeg)
        {
            if (!Active || _rock == null) return;
            _rock.transform.rotation = Quaternion.AngleAxis(tiltDeg, transform.right) * Quaternion.AngleAxis(yawDeg, Vector3.up) * Quaternion.identity;
            _rockRotBase = _rock.transform.rotation; ReseatRock(); UpdateTails();
        }
        public void DevTighten() { if (State == Phase.Seat) { State = Phase.Tighten; } _tighten = 1f; if (State == Phase.Tighten) { State = Phase.Pressure; if (_strainLoop == null) _strainLoop = WorkshopAudio.StartLoop("saw_grind", _rock.transform.position, 0.001f, 0.4f); } UpdateTails(); }
        public bool DevPump;
    }
}
