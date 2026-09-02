using UnityEngine;
using GeodeEmpire.Core;

namespace GeodeEmpire.Player
{
    /// <summary>
    /// Grounded first-person movement with smooth look, subtle head bob and a station-view camera blend
    /// (the camera glides to a fixed anchor while the player works at a bench).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        public Transform CameraPivot;
        public Camera Camera;
        public float WalkSpeed = 2.4f;
        public float SprintSpeed = 3.8f;
        public float Acceleration = 14f;
        public float Gravity = -18f;
        public float PitchLimit = 85f;
        public bool MovementEnabled = true;
        public bool LookEnabled = true;
        public Vector3 SpawnPoint = new Vector3(0f, 0.08f, 0f);
        public float SpawnYaw;

        private CharacterController _cc;
        private float _yaw, _pitch;
        private Vector3 _velocity;
        private float _verticalVelocity;
        private float _bobTime;
        private Transform _stationAnchor;
        private float _stationBlend;
        private Vector3 _camLocalPos;
        private Quaternion _camLocalRot;

        private float _shakeAmp;
        private float _shakeSeed;

        /// <summary>Small camera kick (strike impact, crate landing). Scaled by the camera-shake setting.</summary>
        public void Impulse(float strength)
        {
            _shakeAmp = Mathf.Min(1.5f, _shakeAmp + strength);
            _shakeSeed = UnityEngine.Random.value * 100f;
        }

        public bool InStationView => _stationAnchor != null;
        public float StationBlend => _stationBlend;
        public float Yaw => _yaw;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (CameraPivot == null) CameraPivot = transform.Find("CameraPivot");
            if (Camera == null) Camera = GetComponentInChildren<Camera>();
            _yaw = transform.eulerAngles.y;
            _camLocalPos = Camera.transform.localPosition;
            _camLocalRot = Camera.transform.localRotation;
            GameInput.Ensure();
        }

        private void OnEnable()
        {
            ApplySettings();
            GameSettings.Changed += ApplySettings;
        }

        private void OnDisable() => GameSettings.Changed -= ApplySettings;

        private void ApplySettings()
        {
            if (Camera != null) Camera.fieldOfView = GameSettings.Current.FieldOfView;
        }

        public void Teleport(Vector3 position, float yaw)
        {
            _cc.enabled = false;
            transform.position = position;
            _yaw = yaw;
            _pitch = 0f;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            CameraPivot.localRotation = Quaternion.identity;
            _cc.enabled = true;
        }

        /// <summary>Directly set look angles (used by scripted playtests and station framing).</summary>
        public void SetLook(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = Mathf.Clamp(pitch, -PitchLimit, PitchLimit);
        }

        public void EnterStationView(Transform anchor)
        {
            _stationAnchor = anchor;
            MovementEnabled = false;
            LookEnabled = false;
        }

        public void ExitStationView()
        {
            _stationAnchor = null;
            MovementEnabled = true;
            LookEnabled = true;
        }

        private void Update()
        {
            GameInput.Tick();
            var settings = GameSettings.Current;
            float dt = Time.deltaTime;

            if (LookEnabled && GameInput.GameplayEnabled)
            {
                Vector2 look = GameInput.Look;
                float dx, dy;
                if (GameInput.UsingGamepad)
                {
                    dx = look.x * 160f * settings.GamepadSensitivity * dt;
                    dy = look.y * 120f * settings.GamepadSensitivity * dt;
                }
                else
                {
                    dx = look.x * 0.075f * settings.MouseSensitivity;
                    dy = look.y * 0.075f * settings.MouseSensitivity;
                }
                if (settings.InvertY) dy = -dy;
                _yaw += dx;
                _pitch = Mathf.Clamp(_pitch - dy, -PitchLimit, PitchLimit);
            }
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            CameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            Vector2 move = MovementEnabled && GameInput.GameplayEnabled ? GameInput.Move : Vector2.zero;
            if (move.sqrMagnitude > 1f) move.Normalize();
            float speed = GameInput.SprintHeld ? SprintSpeed : WalkSpeed;
            Vector3 wish = (transform.forward * move.y + transform.right * move.x) * speed;
            _velocity = Vector3.MoveTowards(_velocity, wish, Acceleration * dt);

            // safety net: never let the player fall out of the workshop
            if (transform.position.y < -3f) { Teleport(SpawnPoint, SpawnYaw); _verticalVelocity = 0f; }
            if (_cc.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += Gravity * dt;
            var delta = _velocity * dt + Vector3.up * _verticalVelocity * dt;
            if (_cc.enabled) _cc.Move(delta);

            // head bob
            float planar = new Vector2(_velocity.x, _velocity.z).magnitude;
            if (settings.HeadBob && planar > 0.2f && _cc.isGrounded)
            {
                _bobTime += dt * (4.5f + planar * 1.2f);
            }
            float bobAmp = settings.HeadBob ? Mathf.Clamp01(planar / WalkSpeed) * 0.02f : 0f;
            Vector3 bob = new Vector3(Mathf.Sin(_bobTime * 0.5f) * bobAmp * 0.5f, Mathf.Abs(Mathf.Sin(_bobTime)) * bobAmp, 0f);
            CameraPivot.localPosition = Vector3.Lerp(CameraPivot.localPosition, new Vector3(0f, 1.62f, 0f) + bob, 10f * dt);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            _stationBlend = Mathf.MoveTowards(_stationBlend, _stationAnchor != null ? 1f : 0f, dt * 2.6f);
            float t = Mathf.SmoothStep(0f, 1f, _stationBlend);
            Vector3 homePos = CameraPivot.TransformPoint(_camLocalPos);
            Quaternion homeRot = CameraPivot.rotation * _camLocalRot;
            if (_stationAnchor != null || _stationBlend > 0f)
            {
                Vector3 targetPos = _stationAnchor != null ? _stationAnchor.position : Camera.transform.position;
                Quaternion targetRot = _stationAnchor != null ? _stationAnchor.rotation : Camera.transform.rotation;
                Camera.transform.SetPositionAndRotation(Vector3.Lerp(homePos, targetPos, t), Quaternion.Slerp(homeRot, targetRot, t));
            }
            else
            {
                Camera.transform.SetPositionAndRotation(homePos, homeRot);
            }
            if (_shakeAmp > 0.001f)
            {
                float amp = _shakeAmp * GameSettings.Current.CameraShake;
                float tt = Time.unscaledTime * 38f + _shakeSeed;
                var kick = new Vector3(Mathf.Sin(tt) * 0.9f, Mathf.Sin(tt * 1.3f + 1f) * 0.6f, Mathf.Sin(tt * 0.7f + 2f) * 0.35f) * amp * 0.9f;
                Camera.transform.rotation = Camera.transform.rotation * Quaternion.Euler(kick);
                Camera.transform.position += Camera.transform.up * (-amp * 0.004f);
                _shakeAmp = Mathf.MoveTowards(_shakeAmp, 0f, dt * 4.5f);
            }
        }
    }
}
