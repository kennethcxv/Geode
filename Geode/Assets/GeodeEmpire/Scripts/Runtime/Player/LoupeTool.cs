using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;

namespace GeodeEmpire.Player
{
    /// <summary>
    /// The jeweller's loupe: with a specimen in hand, raise it and the piece is held up to the lens. The lens
    /// magnifies the opaque scene behind it and the shell shader brings its exterior clues up (exposed mineral,
    /// banding, hairline cracks, chips). Nothing about the interior is revealed. Requires the Loupe upgrade.
    /// </summary>
    public sealed class LoupeTool : MonoBehaviour
    {
        public Transform Loupe;
        public PlayerInteractor Player;
        public float Magnification = 2.2f;
        [NonSerialized] public Vector3 RaisedLocalPos = new Vector3(0.032f, -0.142f, 0.2f);
        [NonSerialized] public Vector3 RaisedLocalEuler = new Vector3(0f, 0f, 6f);
        [NonSerialized] public Vector3 LoweredLocalPos = new Vector3(0.09f, -0.28f, 0.22f);
        [NonSerialized] public Vector3 LoweredLocalEuler = new Vector3(40f, 20f, 30f);
        /// <summary>Where the held piece sits (camera-local offset from the inspect anchor) while the loupe is up: centred behind the lens.</summary>
        [NonSerialized] public static Vector3 HeldOffset = new Vector3(0.032f, -0.03f, -0.05f);

        public bool Active { get; private set; }

        private static readonly int LensCenterId = Shader.PropertyToID("_LensCenter");
        private static readonly int MagnifyId = Shader.PropertyToID("_Magnify");
        private static readonly int LoupeBoostId = Shader.PropertyToID("_LoupeBoost");
        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private float _raise;
        private Camera _cam;
        private Transform _lensPoint;
        private bool _opaqueTextureWasOn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Shader.SetGlobalFloat(Shader.PropertyToID("_LoupeBoost"), 0f); }

        public static bool Owned => GameSession.Instance != null && GameSession.Instance.State != null && GameSession.Instance.State.HasUpgrade(UpgradeCatalog.Loupe);

        private void Awake()
        {
            if (Player == null) Player = GetComponent<PlayerInteractor>();
            _cam = Player != null ? Player.Cam : GetComponentInChildren<Camera>();
            if (Loupe != null)
            {
                _renderers = Loupe.GetComponentsInChildren<Renderer>(true);
                _lensPoint = new GameObject("LensPoint").transform;
                _lensPoint.SetParent(Loupe, false);
                _lensPoint.localPosition = new Vector3(0f, 0.112f, -0.007f);
            }
            _mpb = new MaterialPropertyBlock();
            Shader.SetGlobalFloat(LoupeBoostId, 0f);
        }

        private void OnDisable()
        {
            if (Active) SetActive(false);
        }

        private void Update()
        {
            if (Player == null || Loupe == null) return;
            bool canUse = Player.Held != null && !Player.InputLocked && GameInput.GameplayEnabled;
            if (Active && !canUse) SetActive(false);
            if (canUse && GameInput.LoupePressed)
            {
                if (!Owned)
                {
                    GameSession.Instance?.Notify("You need a loupe. The tablet sells a jeweller's loupe.", NotificationKind.Warning);
                    WorkshopAudio.Play2D("ui_error", 0.35f);
                }
                else SetActive(!Active);
            }

            float target = Active ? 1f : 0f;
            _raise = Mathf.MoveTowards(_raise, target, Time.deltaTime * 4.5f);
            bool visible = _raise > 0.001f;
            if (Loupe.gameObject.activeSelf != visible) Loupe.gameObject.SetActive(visible);
            if (!visible) return;
            float e = Mathf.SmoothStep(0f, 1f, _raise);
            Loupe.localPosition = Vector3.Lerp(LoweredLocalPos, RaisedLocalPos, e);
            Loupe.localRotation = Quaternion.Slerp(Quaternion.Euler(LoweredLocalEuler), Quaternion.Euler(RaisedLocalEuler), e);
            // the lens shader magnifies about the lens centre on screen
            if (_cam != null && _lensPoint != null && _renderers != null)
            {
                var vp = _cam.WorldToViewportPoint(_lensPoint.position);
                foreach (var r in _renderers)
                {
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetVector(LensCenterId, new Vector4(vp.x, vp.y, 0f, 0f));
                    _mpb.SetFloat(MagnifyId, Mathf.Lerp(1f, Magnification, e));
                    r.SetPropertyBlock(_mpb);
                }
            }
            Shader.SetGlobalFloat(LoupeBoostId, e);
        }

        private void SetActive(bool on)
        {
            if (Active == on) return;
            Active = on;
            // the lens samples the camera's opaque texture: only pay for the copy while the loupe is up
            var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (rp != null)
            {
                if (on) { _opaqueTextureWasOn = rp.supportsCameraOpaqueTexture; rp.supportsCameraOpaqueTexture = true; }
                else rp.supportsCameraOpaqueTexture = _opaqueTextureWasOn;
            }
            if (Player != null) Player.SetLoupe(on);
            WorkshopAudio.Play2D(on ? "loupe_up" : "loupe_down", 0.5f);
            if (!on) Shader.SetGlobalFloat(LoupeBoostId, 0f);
        }
    }
}
