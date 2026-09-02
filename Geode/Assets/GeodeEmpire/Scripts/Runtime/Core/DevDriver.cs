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

        public static DevDriver Get()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("_DevDriver");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DevDriver>();
            return _instance;
        }

        private void Awake()
        {
            _kb = InputSystem.AddDevice<Keyboard>("DevKeyboard");
            _mouse = InputSystem.AddDevice<Mouse>("DevMouse");
            _pad = InputSystem.AddDevice<Gamepad>("DevGamepad");
        }

        private void OnDestroy()
        {
            if (_kb != null) InputSystem.RemoveDevice(_kb);
            if (_mouse != null) InputSystem.RemoveDevice(_mouse);
            if (_pad != null) InputSystem.RemoveDevice(_pad);
        }

        public PlayerInteractor Player => FindAnyObjectByType<PlayerInteractor>();
        public FirstPersonController Controller => FindAnyObjectByType<FirstPersonController>();

        // ---- keyboard / mouse -----------------------------------------------------------------
        public void KeyDown(Key key) { InputSystem.QueueStateEvent(_kb, new KeyboardState(key)); }
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

        public void SetMouseButton(int button, bool down)
        {
            var st = new MouseState();
            st = st.WithButton(button == 0 ? MouseButton.Left : MouseButton.Right, down);
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
        private IEnumerator WalkRoutine(Vector3 target, float tolerance, float timeout)
        {
            Busy = true;
            Status = "walking";
            var c = Controller;
            float t = 0f;
            while (c != null && t < timeout)
            {
                var flat = target - c.transform.position; flat.y = 0f;
                if (flat.magnitude < tolerance) break;
                LookAt(new Vector3(target.x, c.CameraPivot.position.y, target.z));
                if (UseGamepad) PadState(new Vector2(0f, 1f), Vector2.zero, 0f, 0f); else KeyDown(Key.W);
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (UseGamepad) PadState(Vector2.zero, Vector2.zero, 0f, 0f); else KeyUp();
            yield return null;
            Status = "idle";
            Busy = false;
        }

        public void Teleport(Vector3 pos, float yaw)
        {
            var c = Controller;
            if (c != null) c.Teleport(pos, yaw);
        }
    }
}
