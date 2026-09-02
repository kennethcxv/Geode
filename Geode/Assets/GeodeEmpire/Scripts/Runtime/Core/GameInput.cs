using UnityEngine;
using UnityEngine.InputSystem;

namespace GeodeEmpire.Core
{
    public enum ControlScheme { KeyboardMouse, Gamepad }

    /// <summary>
    /// Thin static wrapper over the project-wide Input System actions (Assets/InputSystem_Actions.inputactions).
    /// Also tracks which device the player used last so prompts can show the right glyph.
    /// </summary>
    public static class GameInput
    {
        private static InputActionMap _player;
        private static InputAction _move, _look, _interact, _strike, _inspect, _drop, _rotate, _scroll, _sprint, _back, _pause, _tablet;
        private static bool _ready;
        private static double _lastGamepadTime = -1, _lastKbmTime = 0;

        public static ControlScheme Scheme { get; private set; } = ControlScheme.KeyboardMouse;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _ready = false; _player = null; _lastGamepadTime = -1; _lastKbmTime = 0; Scheme = ControlScheme.KeyboardMouse;
        }
        public static bool UsingGamepad => Scheme == ControlScheme.Gamepad;

        public static void Ensure()
        {
            if (_ready) return;
            var asset = InputSystem.actions;
            if (asset == null)
            {
                Debug.LogError("[GameInput] No project-wide InputActionAsset assigned.");
                return;
            }
            _player = asset.FindActionMap("Player", true);
            _move = _player.FindAction("Move", true);
            _look = _player.FindAction("Look", true);
            _interact = _player.FindAction("Interact", true);
            _strike = _player.FindAction("Strike", true);
            _inspect = _player.FindAction("Inspect", true);
            _drop = _player.FindAction("Drop", true);
            _rotate = _player.FindAction("Rotate", true);
            _scroll = _player.FindAction("Scroll", true);
            _sprint = _player.FindAction("Sprint", true);
            _back = _player.FindAction("Back", true);
            _pause = _player.FindAction("Pause", true);
            _tablet = _player.FindAction("Tablet", true);
            _player.Enable();
            var ui = asset.FindActionMap("UI");
            ui?.Enable();
            _ready = true;
        }

        /// <summary>Call once per frame from the player to keep the control scheme current.</summary>
        public static void Tick()
        {
            if (!_ready) return;
            var gp = Gamepad.current;
            if (gp != null)
            {
                if (gp.leftStick.ReadValue().sqrMagnitude > 0.04f || gp.rightStick.ReadValue().sqrMagnitude > 0.04f ||
                    gp.buttonSouth.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame ||
                    gp.buttonNorth.wasPressedThisFrame || gp.rightTrigger.ReadValue() > 0.2f || gp.leftTrigger.ReadValue() > 0.2f ||
                    gp.startButton.wasPressedThisFrame || gp.selectButton.wasPressedThisFrame || gp.leftShoulder.wasPressedThisFrame ||
                    gp.rightShoulder.wasPressedThisFrame || gp.dpad.ReadValue().sqrMagnitude > 0.1f)
                    _lastGamepadTime = Time.unscaledTimeAsDouble;
            }
            var kb = Keyboard.current;
            var ms = Mouse.current;
            if ((kb != null && kb.anyKey.wasPressedThisFrame) || (ms != null && (ms.delta.ReadValue().sqrMagnitude > 4f || ms.leftButton.wasPressedThisFrame || ms.rightButton.wasPressedThisFrame)))
                _lastKbmTime = Time.unscaledTimeAsDouble;
            Scheme = _lastGamepadTime > _lastKbmTime ? ControlScheme.Gamepad : ControlScheme.KeyboardMouse;
        }

        public static void SetGameplayEnabled(bool enabled)
        {
            if (!_ready) return;
            if (enabled) _player.Enable(); else _player.Disable();
        }

        public static bool GameplayEnabled => _ready && _player.enabled;

        public static Vector2 Move => _ready ? _move.ReadValue<Vector2>() : Vector2.zero;
        public static Vector2 Look => _ready ? _look.ReadValue<Vector2>() : Vector2.zero;
        public static float Rotate => _ready ? _rotate.ReadValue<float>() : 0f;
        public static Vector2 Scroll => _ready ? _scroll.ReadValue<Vector2>() : Vector2.zero;
        public static bool SprintHeld => _ready && _sprint.IsPressed();
        public static bool InteractPressed => _ready && _interact.WasPressedThisFrame();
        public static bool StrikePressed => _ready && _strike.WasPressedThisFrame();
        public static bool StrikeHeld => _ready && _strike.IsPressed();
        public static bool StrikeReleased => _ready && _strike.WasReleasedThisFrame();
        public static bool InspectHeld => _ready && _inspect.IsPressed();
        public static bool InspectPressed => _ready && _inspect.WasPressedThisFrame();
        public static bool DropPressed => _ready && _drop.WasPressedThisFrame();
        public static bool BackPressed => _ready && _back.WasPressedThisFrame();
        public static bool PausePressed => _ready && _pause.WasPressedThisFrame();
        public static bool TabletPressed => _ready && _tablet.WasPressedThisFrame();

        /// <summary>Human-readable glyph for an action in the current scheme.</summary>
        public static string Glyph(string action)
        {
            bool gp = UsingGamepad;
            switch (action)
            {
                case "Interact": return gp ? "A" : "E";
                case "Strike": return gp ? "RT" : "LMB";
                case "Inspect": return gp ? "LT" : "RMB";
                case "Drop": return gp ? "X" : "G";
                case "Rotate": return gp ? "LB/RB" : "Q/R";
                case "Back": return gp ? "B" : "Esc";
                case "Pause": return gp ? "Start" : "Esc";
                case "Tablet": return gp ? "Select" : "Tab";
                case "Move": return gp ? "L-Stick" : "WASD";
                case "Look": return gp ? "R-Stick" : "Mouse";
                case "Sprint": return gp ? "L3" : "Shift";
            }
            return action;
        }
    }
}
