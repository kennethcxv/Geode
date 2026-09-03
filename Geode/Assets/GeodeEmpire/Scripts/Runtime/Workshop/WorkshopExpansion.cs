using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;

namespace GeodeEmpire.Workshop
{
    /// <summary>
    /// The Stage-2 lapidary workshop: one purchase that visibly changes the room. Everything Stage-2 adds lives under
    /// <see cref="Stage2Root"/> (inactive until bought); a few Stage-1 stand-ins (cardboard storage, the jar shelf)
    /// are hidden once it is in. Capacities are stored on the save; this only toggles the scene.
    /// </summary>
    public sealed class WorkshopExpansion : MonoBehaviour
    {
        public const int Stage2DisplaySlots = 8;
        public const int Stage2SaleSlots = 4;
        public const int Stage2RackSlots = 9;

        public static WorkshopExpansion Instance { get; private set; }

        public GameObject Stage2Root;
        public List<GameObject> HideAtStage2 = new List<GameObject>();

        public static bool Stage2Active => GameSession.Instance != null && GameSession.Instance.State != null && GameSession.Instance.State.WorkshopStage >= 2;

        private void Awake()
        {
            Instance = this;
            Apply(false);
        }

        private void Start()
        {
            var session = GameSession.Instance;
            if (session != null)
            {
                session.Loaded += OnRefresh;
                session.StateChanged += OnRefresh;
                if (session.State != null) OnRefresh();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            var session = GameSession.Instance;
            if (session != null) { session.Loaded -= OnRefresh; session.StateChanged -= OnRefresh; }
        }

        private void OnRefresh() => Apply(Stage2Active);

        private void Apply(bool stage2)
        {
            if (Stage2Root != null && Stage2Root.activeSelf != stage2) Stage2Root.SetActive(stage2);
            foreach (var go in HideAtStage2)
                if (go != null && go.activeSelf == stage2) go.SetActive(!stage2);
        }
    }
}
