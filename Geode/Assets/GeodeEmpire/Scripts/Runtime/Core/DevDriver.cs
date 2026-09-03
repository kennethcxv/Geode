using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using GeodeEmpire.Player;

namespace GeodeEmpire.Core
{
    /// <summary>
    /// Automated-playtest helper: drives the game through the real Input System path (virtual keyboard,
    /// mouse and gamepad devices) so scripted runs exercise the same code a player does. Editor/eval only.
    /// </summary>
    public sealed class DevDriver : MonoBehaviour
    {
        private static DevDriver _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _instance = null; }
        private Keyboard _kb;
        private Mouse _mouse;
        private Gamepad _pad;
        public string Status = "idle";
        public bool Busy;
        public bool UseGamepad;
        public float LastWalkRemaining;

        public static DevDriver Get()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("_DevDriver");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DevDriver>();
            return _instance;
        }

        private InputSettings.BackgroundBehavior _previousBackgroundBehavior;

        private void Awake()
        {
            // The Editor loses application focus whenever another app (the automation client) is in front.
            // With the default background behaviour the Input System then drops every event from devices that
            // cannot run in the background, which includes these virtual ones, so scripted walks silently stall.
            _previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _kb = InputSystem.AddDevice<Keyboard>("DevKeyboard");
            _mouse = InputSystem.AddDevice<Mouse>("DevMouse");
            _pad = InputSystem.AddDevice<Gamepad>("DevGamepad");
        }

        private void OnDestroy()
        {
            if (_kb != null) InputSystem.RemoveDevice(_kb);
            if (_mouse != null) InputSystem.RemoveDevice(_mouse);
            if (_pad != null) InputSystem.RemoveDevice(_pad);
            if (InputSystem.settings != null) InputSystem.settings.backgroundBehavior = _previousBackgroundBehavior;
        }

        public PlayerInteractor Player => FindAnyObjectByType<PlayerInteractor>();
        public FirstPersonController Controller => FindAnyObjectByType<FirstPersonController>();

        // ---- keyboard / mouse -----------------------------------------------------------------
        public void KeyDown(Key key) { InputSystem.QueueStateEvent(_kb, new KeyboardState(key)); }
        public void KeysDown(params Key[] keys) { InputSystem.QueueStateEvent(_kb, new KeyboardState(keys)); }
        public void KeyUp() { InputSystem.QueueStateEvent(_kb, new KeyboardState()); }

        public Coroutine Tap(Key key, float hold = 0.08f) => StartCoroutine(TapRoutine(key, hold));
        private IEnumerator TapRoutine(Key key, float hold)
        {
            Busy = true;
            KeyDown(key);
            yield return null;
            yield return new WaitForSecondsRealtime(hold);
            KeyUp();
            yield return null;
            Busy = false;
        }

        private bool _leftDown, _rightDown;

        /// <summary>Press or release one button without releasing the other (a tap while inspecting keeps the inspect button held).</summary>
        public void SetMouseButton(int button, bool down)
        {
            if (button == 0) _leftDown = down; else _rightDown = down;
            var st = new MouseState();
            st = st.WithButton(MouseButton.Left, _leftDown);
            st = st.WithButton(MouseButton.Right, _rightDown);
            InputSystem.QueueStateEvent(_mouse, st);
        }

        public Coroutine ClickHold(int button, float seconds) => StartCoroutine(ClickHoldRoutine(button, seconds));
        private IEnumerator ClickHoldRoutine(int button, float seconds)
        {
            Busy = true;
            SetMouseButton(button, true);
            float t = 0f;
            while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
            SetMouseButton(button, false);
            yield return null;
            Busy = false;
        }

        public void MouseDelta(float dx, float dy)
        {
            InputSystem.QueueDeltaStateEvent(_mouse.delta, new Vector2(dx, dy));
        }

        // ---- gamepad ---------------------------------------------------------------------------
        public void PadState(Vector2 leftStick, Vector2 rightStick, float lt, float rt, params GamepadButton[] buttons)
        {
            var st = new GamepadState { leftStick = leftStick, rightStick = rightStick, leftTrigger = lt, rightTrigger = rt };
            foreach (var b in buttons) st = st.WithButton(b, true);
            InputSystem.QueueStateEvent(_pad, st);
        }

        public Coroutine PadTap(GamepadButton button, float hold = 0.08f) => StartCoroutine(PadTapRoutine(button, hold));
        private IEnumerator PadTapRoutine(GamepadButton button, float hold)
        {
            Busy = true;
            PadState(Vector2.zero, Vector2.zero, 0f, 0f, button);
            yield return new WaitForSecondsRealtime(hold);
            PadState(Vector2.zero, Vector2.zero, 0f, 0f);
            yield return null;
            Busy = false;
        }

        // ---- high-level helpers ------------------------------------------------------------------
        /// <summary>Turn the player to look at a world point (sets yaw/pitch directly, like a mouse flick).</summary>
        public void LookAt(Vector3 worldPoint)
        {
            var c = Controller;
            if (c == null) return;
            var camPos = c.CameraPivot.position;
            var dir = worldPoint - camPos;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(dir.y, new Vector2(dir.x, dir.z).magnitude) * Mathf.Rad2Deg;
            c.SetLook(yaw, pitch);
        }

        /// <summary>Walk toward a point using the Move action (virtual keyboard), stopping within tolerance.</summary>
        public Coroutine WalkTo(Vector3 target, float tolerance = 0.35f, float timeout = 12f) => StartCoroutine(WalkRoutine(target, tolerance, timeout));
        /// <summary>Sidesteps taken by walks that stopped making progress (a table edge, a crate corner); cumulative.</summary>
        public int Dodges;

        /// <summary>
        /// Walks toward the target. A controller that stops making progress sidesteps for a moment, alternating sides
        /// and lengthening each time, the way a player nudges round a table: the walk only pushes toward its target.
        /// </summary>
        private IEnumerator WalkRoutine(Vector3 target, float tolerance, float timeout)
        {
            Busy = true;
            Status = "walking";
            var c = Controller;
            float t = 0f, sinceCheck = 0f, dodgeT = 0f;
            int dodges = 0;
            Vector3 checkPos = c != null ? c.transform.position : Vector3.zero;
            while (c != null && t < timeout)
            {
                var flat = target - c.transform.position; flat.y = 0f;
                if (flat.magnitude < tolerance) break;
                LookAt(new Vector3(target.x, c.CameraPivot.position.y, target.z));
                float dt = Time.unscaledDeltaTime;
                if (dodgeT > 0f)
                {
                    dodgeT -= dt;
                    bool left = dodges % 2 == 1;
                    if (UseGamepad) PadState(new Vector2(left ? -0.9f : 0.9f, 0.45f), Vector2.zero, 0f, 0f);
                    else KeysDown(Key.W, left ? Key.A : Key.D);
                }
                else
                {
                    if (UseGamepad) PadState(new Vector2(0f, 1f), Vector2.zero, 0f, 0f); else KeyDown(Key.W);
                    sinceCheck += dt;
                    if (sinceCheck >= 0.5f)
                    {
                        float moved = (c.transform.position - checkPos).magnitude;
                        checkPos = c.transform.position;
                        sinceCheck = 0f;
                        if (moved < 0.05f) { dodges++; Dodges++; dodgeT = 0.45f + 0.25f * dodges; }
                    }
                }
                t += dt;
                yield return null;
            }
            if (UseGamepad) PadState(Vector2.zero, Vector2.zero, 0f, 0f); else KeyUp();
            yield return null;
            if (c != null) { var rem = target - c.transform.position; rem.y = 0f; LastWalkRemaining = rem.magnitude; }
            Status = "idle";
            Busy = false;
        }

        public void Teleport(Vector3 pos, float yaw)
        {
            var c = Controller;
            if (c != null) c.Teleport(pos, yaw);
        }

        /// <summary>
        /// Hero close-up: render the scene from an arbitrary eye point (not the player's camera) to a PNG, with the
        /// main camera's rendering settings, so an asset can be inspected at 30-60 cm from any angle.
        /// </summary>
        public static string CaptureFrom(Vector3 eye, Vector3 lookAt, float fov, string path, int width = 1280, int height = 720)
        {
            var main = Camera.main;
            var go = new GameObject("HeroCam");
            var cam = go.AddComponent<Camera>();
            if (main != null) cam.CopyFrom(main);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.02f;
            go.transform.position = eye;
            go.transform.rotation = Quaternion.LookRotation(lookAt - eye, Vector3.up);
            var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (data == null) data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.FastApproximateAntialiasing;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            cam.targetTexture = null;
            if (Application.isPlaying) { Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(go); }
            else { Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); Object.DestroyImmediate(go); }
            return path;
        }
    }
}
