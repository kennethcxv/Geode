using System;
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
    /// The flat lap: set a sawn piece face-down on the spinning platen, hold the button to press it on and move it
    /// about, and the frosted saw face comes up glossy. A few seconds per piece; only cut faces take a polish, a
    /// natural cavity is left alone. Progress is saved as it goes and never has to be redone.
    /// </summary>
    public sealed class PolishStation : InteractableBehaviour
    {
        public PlacementZone Lap;
        public Transform Platen;
        public GameObject Machine;
        public GameObject Before;       // the corner as it was before the lap arrived
        public float PlatenY = 0.786f;  // top of the pad, station-local
        public float PlatenRadius = 0.148f;

        [NonSerialized] public float SecondsToPolish = 6.5f;
        [NonSerialized] public float SweepRadius = 0.07f;
        /// <summary>Firm pressure (sprint held): faster, but soft or fragile stone chips at the edge if it is kept up.</summary>
        public bool Pressing { get; private set; }
        public float Heat => _heat;
        public string LastNote { get; private set; } = "";
        private float _heat, _pressTotal;
        private Vector2 _sweepVel;

        /// <summary>How fast a family takes a polish (hard stone is slow) and how high its gloss goes (hard stone shines).</summary>
        public static float PolishRate(MineralFamily fam) => 1f / Mathf.Lerp(0.75f, 1.4f, Mathf.InverseLerp(0.6f, 1.5f, fam.ShellToughness));
        public static float GlossCeiling(MineralFamily fam) => Mathf.Lerp(0.78f, 1f, Mathf.InverseLerp(0.6f, 1.5f, fam.ShellToughness));
        public static bool NeedsCare(MineralFamily fam) => fam.Fragility > 0.6f || fam.ShellToughness < 0.8f;

        public bool Polishing { get; private set; }
        public SpecimenEntity Current => Lap != null ? Lap.First : null;
        public bool Owned => GameSession.Instance != null && GameSession.Instance.State != null && UpgradeCatalog.Has(GameSession.Instance.State, UpgradeCatalog.PolishLap);
        public float Rpm => _rpm;
        public float LastSweep => _sweep;

        private float _rpm, _platenAngle, _sweep, _sweepPhase, _staticTime, _contactTimer, _saveTimer;
        private Vector2 _sweepPos;
        private AudioSource _motorLoop, _contactLoop;
        private PlayerInteractor _player;
        private FirstPersonController _controller;

        protected override void Awake()
        {
            base.Awake();
            if (Lap != null)
            {
                Lap.Placed += OnPlaced;
                Lap.Taken += OnTaken;
                Lap.ExtraRefusal = Refusal;
                // the press on an unpolished piece polishes it (hold); a tap takes it off the pad
                Lap.ResumePrompt = occ => occ != null && Owned && occ.Record.Polish < 0.98f && !Polishing ? $"Polish the face  (hold  •  tap to take)  {Mathf.RoundToInt(occ.Record.Polish * 100f)}%" : null;
                Lap.ResumeAction = occ => { if (occ != null && Owned && occ.Record.Polish < 0.98f) Begin(FindAnyObjectByType<PlayerInteractor>()); };
            }
        }

        private float _pressTime, _progressSincePress;

        private void Begin(PlayerInteractor player)
        {
            _player = player;
            Polishing = true;
            _staticTime = 0f;
            _pressTime = Time.time;
            _progressSincePress = 0f;
            if (_motorLoop == null) _motorLoop = WorkshopAudio.StartLoop("lap_motor", transform.TransformPoint(new Vector3(0f, 0.5f, 0f)), 0.001f, 0.7f);
            if (_contactLoop == null) _contactLoop = WorkshopAudio.StartLoop("lap_contact", transform.TransformPoint(new Vector3(0f, PlatenY, 0f)), 0.001f, 1f);
            if (_controller != null) { _controller.LookEnabled = false; _controller.MovementEnabled = false; }
        }

        private void Start()
        {
            _player = FindAnyObjectByType<PlayerInteractor>();
            _controller = FindAnyObjectByType<FirstPersonController>();
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
            WorkshopAudio.StopLoop(_motorLoop); WorkshopAudio.StopLoop(_contactLoop);
        }

        public void RefreshOwned()
        {
            bool owned = Owned;
            if (Machine != null) Machine.SetActive(owned);
            if (Before != null) Before.SetActive(!owned);
            if (Lap != null) Lap.Locked = !owned;
            var occ = Current;
            if (occ != null && !Polishing) Seat(occ);
        }

        private string Refusal(SpecimenEntity e)
        {
            if (!Owned) return "Buy the Flat Lap on the tablet";
            if (!e.IsPiece) return "Only a sawn face takes a polish: a natural split is left as it is";
            if (e.Record.Polish >= 0.98f) return "Already polished";
            return null;
        }

        /// <summary>Face-down on the pad: the piece frame's +Y (its cut face) flipped onto the platen.</summary>
        private void Seat(SpecimenEntity e)
        {
            if (e == null || e.Visual == null || e.Visual.Geometry == null) return;
            float face = float.IsNaN(e.Visual.Geometry.ClipTopY) ? 0f : e.Visual.Geometry.ClipTopY;
            var flip = transform.rotation * Quaternion.Euler(180f, (float)((e.Record.Seed >> 8) % 360), 0f);
            Vector3 center = transform.TransformPoint(new Vector3(_sweepPos.x, PlatenY + face + 0.0015f, _sweepPos.y));
            e.SetPose(center, flip);
            e.SetStaticCollidable();
        }

        private void OnPlaced(PlacementZone zone, SpecimenEntity e)
        {
            _sweepPos = Vector2.zero;
            Seat(e);
            WorkshopAudio.Play("rock_place", e.transform.position, 0.6f, 1.05f);
            Tutorial.Notify("piece_on_lap");
        }

        private void OnTaken(PlacementZone zone, SpecimenEntity e)
        {
            StopPolishing();
        }

        public override bool CanInteract(PlayerInteractor player)
        {
            var e = Current;
            return Owned && e != null && player.Held == null && e.Record.Polish < 0.98f;
        }

        private float _noteUntil;

        public override string GetPrompt(PlayerInteractor player)
        {
            var e = Current;
            if (e == null) return "";
            if (Polishing)
            {
                string note = Time.time < _noteUntil ? "  •  " + LastNote : _staticTime > 1.2f ? "  •  keep it moving" : Pressing ? "  •  pressing" : "";
                return $"Polishing  {Mathf.RoundToInt(e.Record.Polish * 100f)}%{note}";
            }
            var fam = e.Geology.Family;
            string care = NeedsCare(fam) ? "  •  light hand" : "";
            return $"Polish the face  (hold)  {Mathf.RoundToInt(e.Record.Polish * 100f)}%{care}";
        }

        public override string GetHint(PlayerInteractor player)
        {
            var e = Current;
            var fam = e != null ? e.Geology.Family : null;
            if (Polishing) return $"{GameInput.Glyph("Look")} sweep the piece across the pad   {GameInput.Glyph("Sprint")} press harder" + (fam != null && NeedsCare(fam) ? " (this stone chips if pushed)" : "");
            if (fam == null) return "";
            float rate = PolishRate(fam);
            string speed = rate < 0.85f ? "hard stone: a slow polish, a deep shine" : rate > 1.15f ? "soft stone: a quick polish" : "takes a polish at an ordinary rate";
            return $"Hold the piece on the spinning pad and keep it moving; the frost goes, the colour comes up. {char.ToUpper(speed[0]) + speed.Substring(1)}.";
        }

        public override void Interact(PlayerInteractor player)
        {
            if (Current == null) return;
            Begin(player);
        }

        private void StopPolishing()
        {
            if (!Polishing) return;
            Polishing = false;
            if (_controller != null) { _controller.LookEnabled = true; _controller.MovementEnabled = true; }
            GameSession.Instance?.QueueSave("polish");
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            // the platen spins up while polishing and winds down after
            _rpm = Mathf.MoveTowards(_rpm, Polishing ? 1f : 0f, dt * (Polishing ? 1.2f : 0.8f));
            if (Platen != null && _rpm > 0.01f)
            {
                _platenAngle += dt * 360f * 3.5f * _rpm;
                Platen.localRotation = Quaternion.AngleAxis(_platenAngle, Vector3.up);
            }
            WorkshopAudio.SetLoop(_motorLoop, 0.3f * _rpm, Mathf.Lerp(0.6f, 1f, _rpm));
            if (!Polishing) { WorkshopAudio.SetLoop(_contactLoop, 0f, 1f); if (_rpm <= 0.01f) { WorkshopAudio.StopLoop(_motorLoop); _motorLoop = null; WorkshopAudio.StopLoop(_contactLoop); _contactLoop = null; } return; }
            var e = Current;
            if (e == null || !GameInput.InteractHeld || (_player != null && _player.Held != null))
            {
                bool tap = e != null && Time.time - _pressTime < 0.28f && _progressSincePress < 0.02f && _player != null && _player.Held == null;
                StopPolishing();
                if (tap) { Lap.Take(e); _player.PickUp(e); }
                return;
            }
            // sweep: the look input slides the piece around the pad, against the drag of the spinning pad, which
            // pulls the piece round with it (a lag on the hand, and a tangential tug that grows with the speed)
            Vector2 look = GameInput.Look;
            Vector2 delta = GameInput.UsingGamepad ? look * 0.12f * dt : look * 0.0006f;
            Vector2 tangent = new Vector2(-_sweepPos.y, _sweepPos.x).normalized * (0.018f * _rpm);
            _sweepVel = Vector2.Lerp(_sweepVel, (delta / Mathf.Max(dt, 1e-4f)) + tangent, 1f - Mathf.Exp(-dt * 9f));
            var next = Vector2.ClampMagnitude(_sweepPos + _sweepVel * dt, SweepRadius);
            float moved = (next - _sweepPos).magnitude;
            _sweepPos = next;
            _sweep = Mathf.Lerp(_sweep, Mathf.Clamp01(moved / (0.09f * dt + 1e-4f)), dt * 6f);
            _staticTime = moved < 0.0005f ? _staticTime + dt : 0f;
            var fam = e.Geology.Family;
            Pressing = GameInput.SprintHeld;
            // the pad only bites once it is up to speed; moving the piece polishes evenly and faster; pressing harder
            // is faster still, but heats the face, and soft or fragile stone chips at its edge when pushed
            float rate = _rpm * (0.55f + 0.65f * _sweep) * (Pressing ? 1.6f : 1f) * PolishRate(fam) / SecondsToPolish;
            var rec = e.Record;
            float before = rec.Polish;
            rec.Polish = Mathf.Clamp01(rec.Polish + rate * dt);
            _progressSincePress += rate * dt;
            // a piece held still on one spot for long dwells a little: nothing lost, just slower
            if (_staticTime > 2f) rec.Polish = Mathf.Clamp01(rec.Polish - dt * 0.01f);
            _heat = Mathf.Clamp01(_heat + dt * (Pressing ? 0.28f : 0.06f) * _rpm - dt * 0.12f * (1f - _rpm * 0.5f));
            if (Pressing && NeedsCare(fam) && _rpm > 0.5f)
            {
                _pressTotal += dt;
                if (_pressTotal > 1.6f)
                {
                    _pressTotal = 0f;
                    rec.ShellDamage = Mathf.Clamp01(rec.ShellDamage + 0.03f);
                    rec.DamageEvents++;
                    WorkshopAudio.Play("crystal_break", e.transform.position, 0.3f, 1.25f);
                    Haptics.Pulse(0.4f, 0.2f, 0.08f);
                    LastNote = fam.Fragility > 0.6f ? "the edge chipped: fragile stone wants a light hand" : "the edge chipped: soft stone wants a light hand";
                    _noteUntil = Time.time + 3f;
                }
            }
            else _pressTotal = Mathf.MoveTowards(_pressTotal, 0f, dt);
            if (_heat > 0.85f && _rpm > 0.5f) { LastNote = "the face is heating: ease off or keep it moving"; _noteUntil = Time.time + 1f; }
            e.Visual.SetPolish(rec.Polish);
            e.Visual.SetWet(1f);   // the drip feed keeps the face wet while it is on the pad
            // a slow orbit of the piece on the pad (visual), plus whatever the player sweeps
            _sweepPhase += dt * 2.2f;
            Seat(e);
            WorkshopAudio.SetLoop(_contactLoop, 0.45f * _rpm, Mathf.Lerp(0.9f, 1.15f, _sweep));
            Haptics.Pulse(0.08f + 0.1f * _rpm, 0.03f, 0.05f);
            _contactTimer -= dt;
            if (_contactTimer <= 0f && _rpm > 0.5f)
            {
                _contactTimer = 0.12f;
                Vector3 edge = e.transform.position + transform.right * (e.Radius * 0.8f) + Vector3.down * 0.005f;
                EffectsFactory.Instance?.Slurry(edge, transform.right, 0.12f + 0.2f * _sweep);
            }
            Haptics.Pulse(0.04f * _rpm + (Pressing ? 0.06f : 0f), 0f, dt);
            _saveTimer -= dt;
            if (_saveTimer <= 0f) { _saveTimer = 2f; GameSession.Instance?.QueueSave("polish"); }
            if (rec.Polish >= 0.98f && before < 0.98f) Finish(e);
        }

        private void Finish(SpecimenEntity e)
        {
            var session = GameSession.Instance;
            var rec = e.Record;
            rec.Polish = 1f;
            e.Visual.SetPolish(1f);
            if (rec.Appraised) rec.AppraisedValue = rec.PristineForSale();   // the price card follows the finish
            var st = session.State.Stats;
            st.PiecesPolished++;
            float v = rec.PristineForSale();
            if (v > st.BestPolishedValue) { st.BestPolishedValue = v; st.BestPolishedName = rec.DisplayName; }
            StopPolishing();
            WorkshopAudio.Play("crystal_chime", e.transform.position, 0.6f, 1.2f);
            WorkshopAudio.Play("ui_click", e.transform.position, 0.4f, 1.3f);
            session.Notify($"Polished: {rec.DisplayName}", NotificationKind.Success);
            Tutorial.Notify("polished");
            session.RaiseStateChanged();
            session.FlushSave("polished");
        }
    }
}
