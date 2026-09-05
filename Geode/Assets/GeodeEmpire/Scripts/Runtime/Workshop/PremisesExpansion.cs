using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;

namespace GeodeEmpire.Workshop
{
    /// <summary>
    /// The premises themselves are the progression. The business opens in one small unit; the back room and the
    /// shop front are leases the player signs, and each one physically opens a part of the building that was
    /// boarded shut before. Nothing here is a lock over finished geometry: while a lease is unsigned the room's
    /// contents do not exist in the scene and an opaque hoarding stands where the opening will be, so the player
    /// cannot see the business they have not built yet.
    ///
    /// Modelled on <see cref="WorkshopExpansion"/>: roots toggled from the upgrades the save already stores, so
    /// there is nothing new to persist and nothing that can drift out of step with the career.
    /// </summary>
    public sealed class PremisesExpansion : MonoBehaviour
    {
        public static PremisesExpansion Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }

        /// <summary>Everything the back-room lease brings: the bay, the racking, the sorting table, the desk.</summary>
        public GameObject BackRoomRoot;
        /// <summary>The boarding that fills the north opening until that lease is signed.</summary>
        public GameObject BackRoomHoarding;
        /// <summary>Everything the shop-front lease brings: the showroom shell fit-out, the counter, the customers.</summary>
        public GameObject ShopFrontRoot;
        /// <summary>The stud-and-ply hoarding across the workshop at x = -0.4 until that lease is signed.</summary>
        public GameObject ShopFrontHoarding;
        /// <summary>Day-one stand-ins that the leases replace (the kerbside pallet, the temporary sign).</summary>
        public List<GameObject> HideWithBackRoom = new List<GameObject>();
        public List<GameObject> HideWithShopFront = new List<GameObject>();

        /// <summary>Any other root that appears with an upgrade: the shop's sign and fit-out, and whatever follows.</summary>
        [System.Serializable]
        public sealed class Gate
        {
            public string Upgrade;
            public GameObject Root;
        }
        public List<Gate> Gates = new List<Gate>();

        public static bool Owns(string upgradeId)
        {
            var s = GameSession.Instance;
            return s != null && s.State != null && s.State.HasUpgrade(upgradeId);
        }

        public static bool BackRoomOpen => Owns(UpgradeCatalog.BackRoom);
        public static bool ShopFrontOpen => Owns(UpgradeCatalog.ShopFront);

        private void Awake()
        {
            Instance = this;
            Apply();
        }

        private void Start()
        {
            var session = GameSession.Instance;
            if (session == null) return;
            session.Loaded += Apply;
            session.StateChanged += Apply;
            Apply();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            var session = GameSession.Instance;
            if (session != null) { session.Loaded -= Apply; session.StateChanged -= Apply; }
        }

        private void Apply()
        {
            bool back = BackRoomOpen, shop = ShopFrontOpen;
            Set(BackRoomRoot, back);
            Set(BackRoomHoarding, !back);
            Set(ShopFrontRoot, shop);
            Set(ShopFrontHoarding, !shop);
            foreach (var go in HideWithBackRoom) Set(go, !back);
            foreach (var go in HideWithShopFront) Set(go, !shop);
            foreach (var g in Gates) if (g != null) Set(g.Root, Owns(g.Upgrade));
            // a fixture that has just appeared or vanished changes which sale slots are real, and the shop's own
            // StateChanged handler may already have run this frame
            var retail = Retail.RetailShop.Instance;
            if (retail != null) retail.RefreshCapacity();
        }

        private static void Set(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }
    }
}
