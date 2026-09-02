using UnityEngine;

namespace GeodeEmpire.Core
{
    /// <summary>Single place that decides whether the cursor is locked (gameplay) or free (menus).</summary>
    public static class CursorController
    {
        private static int _menuDepth;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _menuDepth = 0; }

        public static bool InMenu => _menuDepth > 0;

        public static void EnterMenu()
        {
            _menuDepth++;
            Apply();
        }

        public static void ExitMenu()
        {
            _menuDepth = Mathf.Max(0, _menuDepth - 1);
            Apply();
        }

        public static void Reset()
        {
            _menuDepth = 0;
            Apply();
        }

        public static void Apply()
        {
            bool menu = _menuDepth > 0;
            Cursor.lockState = menu ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = menu;
            GameInput.SetGameplayEnabled(!menu);
        }
    }
}
