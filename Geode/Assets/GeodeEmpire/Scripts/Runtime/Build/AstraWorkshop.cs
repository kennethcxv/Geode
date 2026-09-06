using UnityEngine;

namespace GeodeEmpire.Build
{
    /// <summary>Identifies the authored Astra floor plan. The measured study and legacy Workshop do not carry it.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class AstraWorkshop : MonoBehaviour
    {
        public const int Revision = 1;
        private static AstraWorkshop _instance;
        private static bool _resolved;
        public static bool Active
        {
            get
            {
                if (!_resolved) { _instance = FindAnyObjectByType<AstraWorkshop>(); _resolved = true; }
                return _instance != null && _instance.isActiveAndEnabled;
            }
        }
        private void OnEnable() { _instance = this; _resolved = true; }
        private void OnDisable() { if (_instance == this) _instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _instance = null; _resolved = false; }
    }
}
