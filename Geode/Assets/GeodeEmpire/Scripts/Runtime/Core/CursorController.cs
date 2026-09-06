using UnityEngine;

namespace GeodeEmpire.Core
{
    /// <summary>Single place that decides whether the cursor is locked (gameplay) or free (menus).</summary>
    public static class CursorController
    {
        private static int _menuDepth;
        private static int _stationDepth;

        /// <summary>Frame on which a menu/station consumed a Back/Escape press, so no other consumer re-reads the same press.</summary>
        public static int LastConsumedFrame { get; private set; } = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _menuDepth = 0; _stationDepth = 0; LastConsumedFrame = -1; }

        public static void MarkInputConsumed() => LastConsumedFrame = Time.frameCount;
        public static bool InputConsumedThisFrame => LastConsumedFrame == Time.frameCount;

        public static bool InMenu => _menuDepth > 0;
        public static bool StationControlsActive => _stationDepth > 0 && _menuDepth == _stationDepth;

        public static void EnterMenu(bool stationControls = false)
        {
            _menuDepth++;
            if (stationControls) _stationDepth++;
            Apply();
        }

        public static void ExitMenu(bool stationControls = false)
        {
            _menuDepth = Mathf.Max(0, _menuDepth - 1);
            if (stationControls) _stationDepth = Mathf.Max(0, _stationDepth - 1);
            MarkInputConsumed();
            Apply();
        }

        public static void Reset()
        {
            _menuDepth = 0;
            _stationDepth = 0;
            Apply();
        }

        public static void Apply()
        {
            bool menu = _menuDepth > 0;
            Cursor.lockState = menu ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = menu;
            // A physical counter needs the player's remapped Interact/Back/navigation actions with a free
            // cursor. Its station camera locks locomotion; an ordinary menu above it blocks those actions.
            GameInput.SetGameplayEnabled(!menu || StationControlsActive);
        }
    }
}
