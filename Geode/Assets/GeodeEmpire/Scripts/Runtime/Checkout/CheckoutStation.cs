using UnityEngine;
using GeodeEmpire.Retail;

namespace GeodeEmpire.Checkout
{
    /// <summary>
    /// The physical checkout: the counter, its hardware, and the two machines that drive them — the transaction
    /// (what is legally true about the money) and the flow (what is physically happening). They stay separate on
    /// purpose; every time Golf coupled them, a renderer bug became a money bug.
    ///
    /// This component owns only the station. Money banks through Geode's own career (GameSession / RetailShop), and
    /// the specimen that walks out is the same entity that stood on the shelf.
    /// </summary>
    public sealed class CheckoutStation : MonoBehaviour
    {
        [Header("Authoring")]
        public RetailShop Shop;
        public CounterLayout Layout;
        public CheckoutPropLibrary Library;
        public Transform Counter;

        [Header("Kit")]
        public CheckoutRig CounterRig, MonitorRig, TerminalRig, DrawerRig, CustomerDisplayRig;

        [Header("Anchors (counter-local)")]
        public Transform StagingPoint, ScannedPoint, BagPoint, TenderPoint, ChangePoint, StaffStandPoint, CustomerStandPoint;

        [Header("Cameras")]
        public Transform WorkingCamera, DrawerCamera, CardCamera;

        /// <summary>0 shut, 1 fully out. The tray slides along the drawer's own authored axis.</summary>
        public float DrawerOpen { get; private set; }
        private float _drawerTarget, _drawerVel;
        private Vector3 _trayHome;
        private bool _trayHomeSet;

        private void Awake() => CacheTrayHome();

        private void CacheTrayHome()
        {
            if (_trayHomeSet || DrawerRig == null || DrawerRig.Tray == null) return;
            _trayHome = DrawerRig.Tray.localPosition;
            _trayHomeSet = true;
        }

        public void SetDrawerTarget(float open) => _drawerTarget = Mathf.Clamp01(open);

        private void Update()
        {
            float dt = Time.deltaTime;
            // the drawer kicks out fast and eases back (Golf: open 3.2, close 2.4)
            float speed = _drawerTarget > DrawerOpen ? (Layout != null ? Layout.DrawerOpenSpeed : 3.2f)
                                                     : (Layout != null ? Layout.DrawerCloseSpeed : 2.4f);
            DrawerOpen = Mathf.MoveTowards(DrawerOpen, _drawerTarget, speed * dt * 0.55f);
            if (DrawerRig != null && DrawerRig.Tray != null)
            {
                CacheTrayHome();
                float travel = Layout != null ? Layout.DrawerTravel : DrawerRig.TrayTravel;
                DrawerRig.Tray.localPosition = _trayHome + Vector3.forward * (travel * DrawerOpen);   // out toward the staff side, coins leading
            }
        }
    }
}
