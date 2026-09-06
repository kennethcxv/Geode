using System;
using UnityEngine;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Workshop
{
    /// <summary>
    /// The wash basin. Put a caked rock in the water, take up the brush and work the clay off it — a patch at a
    /// time, where the bristles actually go. The far side stays filthy until you turn the rock round, and the
    /// clay hides the shell's clues until it is off, so this is where a rock stops being a lump and starts
    /// being evidence.
    ///
    /// §7.1 rules out the shape this used to have: hold one button, a number fills, the whole rock is clean.
    /// Dirt is now per region (<see cref="SpecimenSurface"/>), the brush has to reach a region to clean it,
    /// and a dry brush is slower and rougher than a wet one, which is what makes dipping worth doing.
    /// </summary>
    public sealed class WashStation : InteractableBehaviour
    {
        public PlacementZone Tub;
        public Transform Brush;
        public Transform WaterSurface;
        public Transform CameraAnchor;

        /// <summary>Seconds of wet bristles on one patch to take it from fully caked to clean.</summary>
        public float ScrubSeconds = 1.15f;
        /// <summary>Below this much clay a patch counts as done.</summary>
        public const float CleanEnough = 0.06f;
        /// <summary>How long a full brush of water lasts under load.</summary>
        public const float WetSeconds = 5.5f;

        public bool Active { get; private set; }
        public SpecimenEntity Current => Tub != null ? Tub.First : null;
        /// <summary>0..1 water in the bristles: full straight out of the basin, falling as it works.</summary>
        public float BrushWet { get; private set; }
        private AudioSource _water;
        private float _waterLoud;
        public bool Scrubbing { get; private set; }
        public bool BrushOnRock { get; private set; }
        public Vector2 Cursor => _cursor;
        /// <summary>Patches still carrying clay.</summary>
        public int DirtyRegions { get; private set; }
        public string Note { get; private set; } = "";

        public event Action Entered;
        public event Action Exited;

        // owned tools (§7.7)
        public static bool SoftBrush => Owns(UpgradeCatalog.SoftBrush);
        public static bool Nozzle => Owns(UpgradeCatalog.WashNozzle);
        public static bool Sink => Owns(UpgradeCatalog.UtilitySink);
        private static bool Owns(string id)
        {
            var s = GameSession.Instance;
            return s != null && s.State != null && s.State.HasUpgrade(id);
        }

        private Vector3 _brushRestPos;
        private Quaternion _brushRestRot;
        private Vector2 _cursor = new Vector2(0.5f, 0.55f);
        private PlayerInteractor _player;
        private FirstPersonController _controller;
        private Camera _cam;
        private readonly RaycastHit[] _hits = new RaycastHit[12];
        private Vector3 _contact, _contactNormal;
        private int _contactRegion = -1;
        private float _rockYaw, _rockPitch;
        private Quaternion _rockRestRot = Quaternion.identity;
        private Vector3 _rockRestPos;
        private float _stroke, _nextScrubSound, _dipCue;
        private bool _announcedClean;

        protected override void Awake()
        {
            base.Awake();
            if (Brush != null) { _brushRestPos = Brush.localPosition; _brushRestRot = Brush.localRotation; }
            if (Tub != null)
            {
                Tub.Placed += OnPlaced;
                Tub.Taken += (z, e) => { if (Active) Exit(); };
                Tub.ResumePrompt = occ => occ != null && !Active && Dirty(occ) ? "Wash the rock  (tap to take it out)" : null;
                Tub.ResumeAction = occ => { if (occ != null && Dirty(occ)) Enter(occ); };
            }
        }

        private static bool Dirty(SpecimenEntity e) => e != null && e.Visual != null && e.Visual.DirtRemaining > CleanEnough;

        // ------------------------------------------------------------------------------------------
        // Placing a rock in the water
        // ------------------------------------------------------------------------------------------
        private void OnPlaced(PlacementZone z, SpecimenEntity e)
        {
            WorkshopAudio.Play("splash", e.transform.position, 0.7f);
            VFX.EffectsFactory.Instance?.Impact(e.transform.position + Vector3.up * 0.02f, Vector3.up, 0.2f);
            if (e.Visual != null) e.Visual.SetWet(1f);
            e.Record.Condition.EnsureRegions();
            Tutorial.Notify("rock_in_tub");
            // a freshly opened rock: the dust of the break rinses off and the interior comes up in full colour
            if (e.IsOpened && !e.IsPiece && e.Record.Condition != null && !e.Record.Condition.Rinsed)
            {
                e.Record.Condition.Rinsed = true;
                GameState.Log(e.Record, "rinsed");
                if (e.Visual != null) e.Visual.RefreshCondition();
                WorkshopAudio.Play("splash", e.transform.position, 0.5f, 1.25f);
                VFX.EffectsFactory.Instance?.Glints(e.transform.position + Vector3.up * (e.Radius * 0.5f), e.Radius * 0.9f, 14, Color.white);
                var session = GameSession.Instance;
                if (session != null)
                {
                    session.State.Stats.RocksWashed++;
                    session.Notify("Rinsed: the dust is off and the colour comes up.", NotificationKind.Success);
                    session.QueueSave("rinsed");
                }
                Tutorial.Notify("rinsed");
            }
        }

        public override bool CanInteract(PlayerInteractor player) => !Active && player.Held == null && Dirty(Current);
        public override string GetPrompt(PlayerInteractor player) => Active || !Dirty(Current) ? "" : "Wash the rock";
        public override string GetHint(PlayerInteractor player) =>
            Active || !Dirty(Current) ? null : "Clay off, clues on: the seam, any staining and mineral showing through";
        public override void Interact(PlayerInteractor player) { if (Dirty(Current)) Enter(Current); }

        // ------------------------------------------------------------------------------------------
        // The basin, close up
        // ------------------------------------------------------------------------------------------
        public void Enter(SpecimenEntity e)
        {
            if (Active || e == null) return;
            _player = FindAnyObjectByType<PlayerInteractor>();
            _controller = _player != null ? _player.GetComponent<FirstPersonController>() : null;
            _cam = _controller != null && _controller.Camera != null ? _controller.Camera : Camera.main;
            Active = true;
            _announcedClean = false;
            _cursor = new Vector2(0.5f, 0.56f);
            _rockYaw = 0f; _rockPitch = 0f;
            BrushWet = 1f;
            e.Locked = true;
            e.Record.Condition.EnsureRegions();
            _rockRestPos = e.transform.position;
            _rockRestRot = e.transform.rotation;
            CountDirty(e);
            MeasureBrush();
            FrameRock(e);
            if (CameraAnchor != null && _controller != null) _controller.EnterStationView(CameraAnchor);
            if (_player != null) _player.InputLocked = true;
            Note = "";
            // §21: the tap runs while the player is at the basin, so the station is not silent between strokes
            if (_water == null && WaterSurface != null)
                _water = WorkshopAudio.StartLoop("water_run", WaterSurface.position, 0f, Nozzle ? 1.06f : 1f);
            Entered?.Invoke();
        }

        /// <summary>
        /// Put the working view where a person washing a rock would have their head: over it, on the side they are
        /// standing, close enough to see a patch of clay and far enough to see the whole rock turn. Framed from the
        /// rock rather than from a fixed offset, so a pebble and a two-kilo lump both fill the view sensibly.
        /// </summary>
        private void FrameRock(SpecimenEntity e)
        {
            if (CameraAnchor == null) return;
            Vector3 rock = e.transform.position;
            // which side is the player on? flattened, so the camera never ends up under the worktop
            Vector3 side = _player != null ? _player.transform.position - rock : Vector3.forward;
            side.y = 0f;
            if (side.sqrMagnitude < 0.01f) side = Vector3.forward;
            side.Normalize();
            float r = Mathf.Max(0.035f, e.Radius);
            float dist = Mathf.Clamp(r * 4.6f + 0.085f, 0.20f, 0.52f);
            Vector3 pos = rock + side * (dist * 0.72f) + Vector3.up * (dist * 0.78f);
            CameraAnchor.SetPositionAndRotation(pos, Quaternion.LookRotation(rock - pos, Vector3.up));
        }

        public void Exit()
        {
            if (!Active) return;
            CursorController.MarkInputConsumed();
            Active = false;
            Scrubbing = false;
            if (_water != null) { WorkshopAudio.StopLoop(_water); _water = null; }
            RestBrush();
            if (_controller != null) _controller.ExitStationView();
            if (_player != null) _player.InputLocked = false;
            var e = Current;
            if (e != null)
            {
                e.Locked = false;
                e.SetPose(_rockRestPos, _rockRestRot);
            }
            GameSession.Instance?.QueueSave("wash-exit");
            Exited?.Invoke();
        }

        private void CountDirty(SpecimenEntity e)
        {
            int n = 0;
            if (e != null && e.Visual != null)
                for (int r = 0; r < SpecimenSurface.Regions; r++)
                    if (e.Visual.DirtAt(r) > CleanEnough) n++;
            DirtyRegions = n;
        }

        /// <summary>Where the water is: dip below this and the brush fills.</summary>
        private float WaterY => WaterSurface != null ? WaterSurface.position.y : transform.position.y + 0.1f;

        private void Update()
        {
            var e = Current;
            if (!Active || e == null) { if (!Active) RestBrush(); return; }
            if (GameInput.BackPressed || GameInput.InventoryPressed) { Exit(); return; }
            float dt = Time.deltaTime;

            // the brush is moved with the same virtual cursor the bench uses, so mouse and stick both work
            Vector2 look = GameInput.Look;
            if (GameInput.UsingGamepad) _cursor += look * 0.85f * dt * GameSettings.Current.GamepadSensitivity;
            else _cursor += look * 0.0011f * GameSettings.Current.MouseSensitivity;
            _cursor.x = Mathf.Clamp(_cursor.x, 0.1f, 0.9f);
            _cursor.y = Mathf.Clamp(_cursor.y, 0.08f, 0.92f);

            // turning the rock is how the far side gets reached at all (§7.3)
            float turn = GameInput.Rotate;
            if (Mathf.Abs(turn) > 0.01f)
            {
                _rockYaw += turn * 95f * dt;
            }
            // and tipping it, so the bottom is reachable too
            if (GameInput.StrikeHeld) _rockPitch = Mathf.MoveTowards(_rockPitch, 150f, 110f * dt);
            else _rockPitch = Mathf.MoveTowards(_rockPitch, 0f, 110f * dt);
            e.SetPose(_rockRestPos, Quaternion.AngleAxis(_rockPitch, Vector3.right) * Quaternion.AngleAxis(_rockYaw, Vector3.up) * _rockRestRot);

            AimBrush(e);
            bool wantScrub = GameInput.InteractHeld && BrushOnRock;
            Scrubbing = wantScrub;

            if (!BrushOnRock && _contact.y < WaterY + 0.02f)
            {
                // bristles in the water: the brush fills again
                float before = BrushWet;
                BrushWet = Mathf.MoveTowards(BrushWet, 1f, dt * (Nozzle ? 2.4f : 1.3f));
                _waterLoud = 1f;                                   // the brush is in the stream
                GameSession.Instance?.MeterWater(Nozzle ? Economy.Ledger.NozzleLitresPerMinute : Economy.Ledger.BasinLitresPerMinute, dt);
                if (BrushWet > 0.3f && before <= 0.3f) WorkshopAudio.Play("splash", _contact, 0.35f, 1.35f);
            }

            if (Scrubbing) Scrub(e, dt);
            else _stroke = Mathf.MoveTowards(_stroke, 0f, dt * 3f);

            MoveBrush(e, dt);
            Advice(e);
        }

        /// <summary>Where the bristles are: the shell point under the cursor, or the water below it.</summary>
        /// <summary>Diagnostic: what the brush ray actually did last frame.</summary>
        public string AimDebug { get; private set; } = "";

        private void AimBrush(SpecimenEntity e)
        {
            BrushOnRock = false;
            _contactRegion = -1;
            if (_cam == null) { AimDebug = "no camera"; return; }
            var ray = _cam.ViewportPointToRay(new Vector3(_cursor.x, _cursor.y, 0f));
            int n = Physics.RaycastNonAlloc(ray, _hits, 3f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var h = _hits[i];
                if (h.collider.attachedRigidbody != e.Body && h.collider.GetComponentInParent<SpecimenEntity>() != e) continue;
                if (h.distance < best) { best = h.distance; _contact = h.point; _contactNormal = h.normal; BrushOnRock = true; }
            }
            if (BrushOnRock)
            {
                var local = e.transform.InverseTransformPoint(_contact);
                _contactRegion = SpecimenSurface.RegionOf(local);
                AimDebug = $"on rock region {_contactRegion}";
                return;
            }
            AimDebug = $"cam={_cam.name} hits={n} " + (n > 0 ? _hits[0].collider.name + "@" + _hits[0].distance.ToString("F2") : "nothing")
                     + $" rockAt={e.transform.position.ToString("F2")} cursor={_cursor}";
            // not on the rock: put the bristles where the ray meets the water, so dipping is a place you can go
            var plane = new Plane(Vector3.up, new Vector3(0f, WaterY, 0f));
            _contact = plane.Raycast(ray, out float d) ? ray.GetPoint(d) : ray.GetPoint(0.6f);
            _contactNormal = Vector3.up;
        }

        private void Scrub(SpecimenEntity e, float dt)
        {
            if (_contactRegion < 0) return;
            var cond = e.Record.Condition;
            cond.EnsureRegions();
            // a wet brush cuts clay; a dry one mostly pushes dust around and is rough on the shell
            float wetness = Mathf.Lerp(0.28f, 1f, BrushWet);
            float tool = SoftBrush ? 1.35f : 1f;
            float rate = dt / ScrubSeconds * wetness * tool;
            for (int r = 0; r < SpecimenSurface.Regions; r++)
            {
                float share = SpecimenSurface.Falloff(_contactRegion, r);
                if (share <= 0f) continue;
                float clean = cond.CleanAt(r);
                if (clean >= 1f) continue;
                cond.SetCleanAt(r, clean + rate * share);
            }
            BrushWet = Mathf.Max(0f, BrushWet - dt / WetSeconds);
            e.Visual.SetWet(Mathf.Max(0.35f, BrushWet));
            e.Visual.RefreshCondition();
            _stroke += dt * (6.5f + BrushWet * 2f);

            // the tap is always on at the basin; it opens up when the brush goes under it
            _waterLoud = Mathf.MoveTowards(_waterLoud, 0f, dt * 2.2f);
            if (_water != null)
                WorkshopAudio.SetLoop(_water, Mathf.Lerp(0.12f, 0.42f, _waterLoud) * WorkshopAudio.SfxVolume,
                                      Mathf.Lerp(0.97f, 1.05f, _waterLoud));
            _nextScrubSound -= dt;
            if (_nextScrubSound <= 0f)
            {
                _nextScrubSound = BrushWet > 0.25f ? 0.3f : 0.42f;
                // §21: a soft brush on a wet rock is a sponge, not bristles; a dry brush drags whatever it is
                string wipe = BrushWet <= 0.25f ? "scrub_dry" : SoftBrush ? "sponge" : "scrub";
                WorkshopAudio.Play(wipe, _contact, BrushWet > 0.25f ? 0.55f : 0.4f,
                                   0.9f + 0.2f * Mathf.Sin(_stroke));
                if (UnityEngine.Random.value < 0.5f)
                    VFX.EffectsFactory.Instance?.Impact(_contact + _contactNormal * 0.005f, _contactNormal, BrushWet > 0.25f ? 0.14f : 0.09f);
                Haptics.Pulse(0.08f, 0.02f, 0.06f);
            }

            // §7.5: careless work has a cost, but only careless work. A dry brush driven over exposed crystal
            // is the one way to spoil a rock here, and the game says so before it happens.
            if (BrushWet < 0.12f && !e.IsOpened) MaybeScuff(e);

            CountDirty(e);
            if (DirtyRegions == 0 && !_announcedClean) Finish(e);
        }

        private float _scuffTimer;

        private void MaybeScuff(SpecimenEntity e)
        {
            _scuffTimer += Time.deltaTime;
            if (_scuffTimer < 1.6f) return;
            _scuffTimer = 0f;
            var rec = e.Record;
            if (rec.ShellDamage > 0.28f) return;              // never grinds a rock away: it stops well short
            rec.ShellDamage = Mathf.Clamp01(rec.ShellDamage + 0.035f);
            WorkshopAudio.Play("scrape", _contact, 0.5f, 0.85f);
            Note = "Dry bristles are scouring the shell — dip the brush.";
        }

        private void Finish(SpecimenEntity e)
        {
            _announcedClean = true;
            var session = GameSession.Instance;
            WorkshopAudio.Play("splash", e.transform.position, 0.5f, 1.15f);
            WorkshopAudio.Play("ui_click", e.transform.position, 0.4f, 1.3f);
            if (session != null)
            {
                session.State.Stats.RocksWashed++;
                GameState.Log(e.Record, "washed");
                session.Notify("Clean. Turn it over and read the shell before you decide how to open it.", NotificationKind.Info);
                session.QueueSave("washed");
            }
            Tutorial.Notify("washed");
            Note = "Clean.";
        }

        /// <summary>Short, factual guidance: which way to turn, and whether the brush needs dipping.</summary>
        private void Advice(SpecimenEntity e)
        {
            if (_announcedClean) { Note = "Clean — take it out."; return; }
            if (BrushWet < 0.15f) { Note = "The brush is dry. Dip it in the water."; return; }
            if (Scrubbing) { Note = ""; return; }
            var (region, dirt) = e.Record.Condition.DirtiestRegion();
            if (region < 0 || dirt <= CleanEnough) { Note = ""; return; }
            // is the dirtiest patch facing the player, or round the back?
            var world = e.transform.TransformDirection(SpecimenSurface.DirectionOf(region));
            var toCam = _cam != null ? (_cam.transform.position - e.transform.position).normalized : Vector3.back;
            float facing = Vector3.Dot(world.normalized, toCam);
            Note = facing > 0.25f ? DirtyRegions + " patches left" : "There is still clay round the other side — turn it.";
        }

        /// <summary>Half the brush's longest side, measured once, so its head can be seated on the shell.</summary>
        private float _brushReach = 0.06f;

        private void MeasureBrush()
        {
            if (Brush == null) return;
            var rs = Brush.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            _brushReach = Mathf.Max(0.02f, Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z)));
        }

        /// <summary>
        /// The brush comes in over the player's shoulder with its head on the rock and its handle going back out of
        /// shot — held at arm's length like a tool, not floated between the eye and the thing being cleaned.
        /// </summary>
        private void MoveBrush(SpecimenEntity e, float dt)
        {
            if (Brush == null || _cam == null) return;
            Vector3 target = _contact + _contactNormal * 0.012f;
            if (Scrubbing)
            {
                var tangent = Vector3.Cross(_contactNormal, Vector3.up);
                if (tangent.sqrMagnitude < 1e-4f) tangent = Vector3.right;
                target += tangent.normalized * (Mathf.Sin(_stroke) * 0.014f);
            }
            // the handle runs up and to the right, away from the camera, so the rock is never behind the brush
            var away = (_cam.transform.right * 0.55f + _cam.transform.up * 0.72f - _cam.transform.forward * 0.42f).normalized;
            Brush.position = Vector3.Lerp(Brush.position, target + away * _brushReach, 1f - Mathf.Exp(-dt * 22f));
            // bristles down onto the shell, handle up and back
            var look = Quaternion.LookRotation(away, _cam.transform.up) * Quaternion.Euler(-90f, 0f, 0f);
            if (Scrubbing) look *= Quaternion.Euler(Mathf.Sin(_stroke) * 10f, 0f, Mathf.Cos(_stroke * 0.8f) * 8f);
            Brush.rotation = Quaternion.Slerp(Brush.rotation, look, 1f - Mathf.Exp(-dt * 16f));
        }

        private void RestBrush()
        {
            if (Brush == null) return;
            Brush.localPosition = _brushRestPos;
            Brush.localRotation = _brushRestRot;
        }

        // ------------------------------------------------------------------------------------------
        // Test seam: the scripted playtests drive the same code the player does
        // ------------------------------------------------------------------------------------------
        public void SetCursor(Vector2 viewport) => _cursor = new Vector2(Mathf.Clamp(viewport.x, 0.1f, 0.9f), Mathf.Clamp(viewport.y, 0.08f, 0.92f));
        public void TurnRock(float degrees) => _rockYaw += degrees;
        public int ContactRegion => _contactRegion;
        public void FillBrush() => BrushWet = 1f;
    }
}
