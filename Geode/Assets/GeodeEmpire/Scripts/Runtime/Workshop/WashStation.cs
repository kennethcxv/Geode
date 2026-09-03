using UnityEngine;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Workshop
{
    /// <summary>
    /// The wash tub: dunk a dirty rock, hold the brush on it and the clay comes off in patches, high points first.
    /// A few seconds per rock, skippable, and the only way to read a quarry rock's seam, staining and mineral hints
    /// before deciding how to open it.
    /// </summary>
    public sealed class WashStation : InteractableBehaviour
    {
        public PlacementZone Tub;
        public Transform Brush;
        public Transform WaterSurface;
        /// <summary>Seconds of scrubbing to take a fully caked rock to clean.</summary>
        public float ScrubSeconds = 3.2f;

        public bool Scrubbing { get; private set; }
        public SpecimenEntity Current => Tub != null ? Tub.First : null;

        private Vector3 _brushRestPos;
        private Quaternion _brushRestRot;
        private float _stroke, _nextScrubSound, _rockYaw;
        private PlayerInteractor _player;

        protected override void Awake()
        {
            base.Awake();
            if (Brush != null) { _brushRestPos = Brush.localPosition; _brushRestRot = Brush.localRotation; }
            if (Tub != null)
            {
                Tub.Placed += OnPlaced;
                Tub.Taken += (z, e) => { Scrubbing = false; RestBrush(); };
                // the press on a dirty rock in the tub scrubs it (hold); a tap takes it back out
                Tub.ResumePrompt = occ => occ != null && Dirt(occ) > 0.02f && !Scrubbing ? "Scrub the rock  (hold  •  tap to take)" : null;
                Tub.ResumeAction = occ => { if (occ != null && Dirt(occ) > 0.02f) BeginScrub(); };
            }
        }

        private float _pressTime;
        private float _progressSincePress;

        private void BeginScrub()
        {
            _player = FindAnyObjectByType<PlayerInteractor>();
            Scrubbing = true;
            _nextScrubSound = 0f;
            _pressTime = Time.time;
            _progressSincePress = 0f;
        }

        private void OnPlaced(PlacementZone z, SpecimenEntity e)
        {
            WorkshopAudio.Play("splash", e.transform.position, 0.7f);
            VFX.EffectsFactory.Instance?.Impact(e.transform.position + Vector3.up * 0.02f, Vector3.up, 0.2f);
            if (e.Visual != null) e.Visual.SetWet(1f);   // dunked: darker and glossy until it dries
            Tutorial.Notify("rock_in_tub");
        }

        private float Dirt(SpecimenEntity e) => e != null && e.Visual != null ? e.Visual.DirtRemaining : 0f;

        public override bool CanInteract(PlayerInteractor player)
        {
            var e = Current;
            return e != null && player.Held == null && Dirt(e) > 0.02f;
        }

        public override string GetPrompt(PlayerInteractor player)
        {
            var e = Current;
            if (e == null) return "";
            float dirt = Dirt(e);
            return Scrubbing ? $"Scrubbing  {Mathf.RoundToInt((1f - dirt / Mathf.Max(0.01f, e.Geology.Dirt)) * 100f)}%" : "Scrub the rock  (hold)";
        }

        public override string GetHint(PlayerInteractor player) => Scrubbing ? null : "Clay off, clues on: the seam, any staining and mineral showing through";

        public override void Interact(PlayerInteractor player)
        {
            if (Current == null) return;
            _player = player;
            BeginScrub();
        }

        private void Update()
        {
            var e = Current;
            if (!Scrubbing || e == null) { if (Brush != null && !Scrubbing) RestBrush(); return; }
            if (!GameInput.InteractHeld || (_player != null && _player.Held != null))
            {
                Scrubbing = false;
                RestBrush();
                // a quick tap was a "take", not a scrub
                if (Time.time - _pressTime < 0.28f && _progressSincePress < 0.02f && _player != null && _player.Held == null)
                {
                    Tub.Take(e);
                    _player.PickUp(e);
                }
                return;
            }
            float dt = Time.deltaTime;
            var cond = e.Record.Condition;
            float before = e.Visual.DirtRemaining;
            cond.Cleaned = Mathf.Clamp01(cond.Cleaned + dt / ScrubSeconds);
            e.Visual.SetWet(1f);
            _progressSincePress += dt / ScrubSeconds;
            e.Visual.RefreshCondition();
            // the rock turns under the brush so the whole shell gets done
            _rockYaw += dt * 70f;
            var anchor = Tub.Anchor != null ? Tub.Anchor : Tub.transform;
            e.SetPose(e.transform.position, anchor.rotation * Quaternion.Euler(0f, _rockYaw, 0f));
            // brush strokes across the top of the rock
            _stroke += dt * 6.5f;
            if (Brush != null)
            {
                float r = e.Radius;
                Vector3 top = e.transform.position + Vector3.up * (r * 0.75f);
                Vector3 side = anchor.right * (Mathf.Sin(_stroke) * r * 0.55f) + anchor.forward * (Mathf.Cos(_stroke * 0.7f) * r * 0.3f);
                Brush.position = top + side + Vector3.up * 0.012f;
                Brush.rotation = Quaternion.LookRotation(anchor.forward, Vector3.up) * Quaternion.Euler(Mathf.Sin(_stroke) * 14f, 0f, Mathf.Cos(_stroke) * 10f);
            }
            _nextScrubSound -= dt;
            if (_nextScrubSound <= 0f)
            {
                _nextScrubSound = 0.36f;
                WorkshopAudio.Play("scrub", e.transform.position, 0.55f, 0.9f + 0.2f * Mathf.Sin(_stroke));
                if (Random.value < 0.4f) VFX.EffectsFactory.Instance?.Impact(e.transform.position + Vector3.up * (e.Radius * 0.5f), Vector3.up, 0.15f);
                Haptics.Pulse(0.08f, 0.02f, 0.06f);
            }
            if (e.Visual.DirtRemaining <= 0.02f && before > 0.02f)
            {
                Scrubbing = false;
                RestBrush();
                WorkshopAudio.Play("splash", e.transform.position, 0.5f, 1.15f);
                WorkshopAudio.Play("ui_click", e.transform.position, 0.4f, 1.3f);
                GameSession.Instance?.Notify("Clean. Turn it over and read the shell before you decide how to open it.", NotificationKind.Info);
                Tutorial.Notify("washed");
                GameSession.Instance?.QueueSave("washed");
            }
        }

        private void RestBrush()
        {
            if (Brush == null) return;
            Brush.localPosition = _brushRestPos;
            Brush.localRotation = _brushRestRot;
        }
    }
}
