using UnityEngine;
using UnityEngine.InputSystem;

namespace GeodeEmpire.Core
{
    /// <summary>
    /// Controller rumble, scaled by the vibration setting. Strong pulses win over weak ones while both run; everything
    /// stops on the timer, on pause and when the window loses focus so a pad never buzzes on its own.
    /// </summary>
    public sealed class Haptics : MonoBehaviour
    {
        private static Haptics _i;
        /// <summary>How many pulses actually reached a pad (dev/test).</summary>
        public static int PulseCount { get; private set; }

        private float _until, _low, _high;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _i = null; PulseCount = 0; }

        public static void Pulse(float low, float high, float seconds)
        {
            float v = GameSettings.Current.Vibration;
            var gp = Gamepad.current;
            if (v <= 0.001f || gp == null) return;
            if (_i == null)
            {
                var go = new GameObject("_Haptics");
                DontDestroyOnLoad(go);
                _i = go.AddComponent<Haptics>();
            }
            _i._low = Mathf.Max(_i._low, Mathf.Clamp01(low * v));
            _i._high = Mathf.Max(_i._high, Mathf.Clamp01(high * v));
            _i._until = Mathf.Max(_i._until, Time.unscaledTime + seconds);
            gp.SetMotorSpeeds(_i._low, _i._high);
            PulseCount++;
        }

        public static void Stop()
        {
            if (_i != null) { _i._until = 0f; _i._low = 0f; _i._high = 0f; }
            Gamepad.current?.ResetHaptics();
        }

        private void Update()
        {
            if (_until > 0f && Time.unscaledTime >= _until) Stop();
        }

        private void OnApplicationFocus(bool focus) { if (!focus) Stop(); }
        private void OnDisable() { Stop(); }
    }
}
