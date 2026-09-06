using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GeodeEmpire.EditorTools
{
    /// <summary>Bounded real input events for isolated QA, delivered on gameplay's normal input update.</summary>
    [InitializeOnLoad]
    public static class AstraQaInput
    {
        private static Action _tick;
        private static Action _release;
        private static Gamepad _gamepad;

        static AstraQaInput()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Cancel();
                if (state == PlayModeStateChange.EnteredEditMode && _gamepad != null)
                {
                    if (_gamepad.added) InputSystem.RemoveDevice(_gamepad);
                    _gamepad = null;
                }
            };
        }

        public static bool Busy => _tick != null;

        public static void TapKey(string key) => HoldKey(key, 0.05f);

        public static void HoldKey(string key, float seconds)
        {
            RequireSession(seconds);
            if (!Enum.TryParse(key, true, out Key parsed) || parsed == Key.None)
                throw new ArgumentException("Unknown Input System key: " + key);
            var keyboard = Keyboard.current;
            if (keyboard == null) throw new InvalidOperationException("No keyboard device is available.");
            void Write(float value)
            {
                if (!keyboard.added) return;
                using (StateEvent.From(keyboard, out var e))
                {
                    keyboard[parsed].WriteValueIntoEvent(value, e);
                    InputSystem.QueueEvent(e);
                }
            }
            Schedule(seconds, () => Write(1f), () => Write(0f));
        }

        public static void HoldGamepad(Vector2 move, Vector2 look, float seconds)
        {
            RequireSession(seconds);
            if (_gamepad == null || !_gamepad.added)
                _gamepad = InputSystem.AddDevice<Gamepad>("AstraQaGamepad");
            var device = _gamepad;
            var state = new GamepadState
            {
                leftStick = Vector2.ClampMagnitude(move, 1f),
                rightStick = Vector2.ClampMagnitude(look, 1f)
            };
            Schedule(seconds, () => InputSystem.QueueStateEvent(device, state),
                () => { if (device.added) InputSystem.QueueStateEvent(device, new GamepadState()); });
        }

        public static void Cancel()
        {
            if (_tick != null) InputSystem.onBeforeUpdate -= _tick;
            _tick = null;
            _release?.Invoke();
            _release = null;
        }

        private static void RequireSession(float seconds)
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("QA input requires Play Mode.");
            AstraQaSession.ValidatePrepared();
            if (Busy) throw new InvalidOperationException("Wait for the pending QA input to release.");
            if (float.IsNaN(seconds) || seconds < 0.01f || seconds > 10f)
                throw new ArgumentOutOfRangeException(nameof(seconds), "QA holds must last 0.01–10 seconds.");
        }

        private static void Schedule(float seconds, Action press, Action release)
        {
            int warmup = 2;
            double deadline = -1;
            _release = release;
            _tick = () =>
            {
                if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
                // Tool compilation can stall an Editor frame. Do not apply look/movement using that old delta.
                if (warmup-- > 0) return;
                if (deadline < 0)
                {
                    deadline = Time.unscaledTimeAsDouble + seconds;
                    press();
                }
                else if (Time.unscaledTimeAsDouble >= deadline) Cancel();
            };
            InputSystem.onBeforeUpdate += _tick;
        }
    }
}
