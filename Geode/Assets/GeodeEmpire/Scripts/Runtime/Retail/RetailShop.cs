using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;
using GeodeEmpire.Interaction;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.Retail
{
    /// <summary>
    /// The small showroom: sale fixtures the player stocks with appraised specimens, customers who walk in, browse,
    /// pick something (or not), queue and pay at the counter. Runs alongside the cracking loop; the player is only
    /// needed for the checkout itself. Reservations are runtime-only: a save never carries a half-finished sale.
    /// </summary>
    public sealed class RetailShop : MonoBehaviour
    {
        public static RetailShop Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }

        public List<PlacementZone> SaleSlots = new List<PlacementZone>();
        public List<Transform> PriceCards = new List<Transform>();
        public Transform OutsidePoint, DoorPoint, CounterCustomerPoint, CounterItemPoint;
        public List<Transform> QueuePoints = new List<Transform>();
        public List<Transform> BrowsePoints = new List<Transform>();   // one per sale slot, where a customer stands to look
        public GameObject CustomerTemplate;
        public Transform DoorLeaf;
        public Font LabelFont;
        public Material LabelMaterial;
        public NavMeshSurface Navigation;

        /// <summary>Retail markup over the appraised (dealer) value. A customer with the right taste pays it; the dealer never does.</summary>
        public const float Markup = 1.4f;
        public const int MaxCustomers = 3;

        public event Action Changed;
        public event Action<Customer> CustomerArrivedAtCounter;
        public event Action<Customer, SpecimenRecord, float> SaleCompleted;

        private readonly List<Customer> _customers = new List<Customer>();
        private readonly Dictionary<PlacementZone, UI.WorldLabel> _labels = new Dictionary<PlacementZone, UI.WorldLabel>();
        private readonly Queue<Customer> _queue = new Queue<Customer>();
        private readonly Dictionary<PlacementZone, Customer> _browseClaims = new Dictionary<PlacementZone, Customer>();

        /// <summary>Navigation health counters, read by the retail stress test.</summary>
        public sealed class NavMetrics { public int StuckRecoveries, Repositions, PathFailures; public float LastSaleSeconds, SaleSecondsTotal; public int SalesTimed; }
        private float _counterSince;
        [System.NonSerialized] public NavMetrics Metrics = new NavMetrics();
        private float _nextSpawnIn = 25f;
        private int _customerCounter;
        private float _doorOpen;
        private Quaternion _doorClosedRot;

        public IReadOnlyList<Customer> Customers => _customers;
        public int QueueLength => _queue.Count;
        public Customer AtCounter { get; private set; }

        private void Awake()
        {
            Instance = this;
            foreach (var s in SaleSlots)
            {
                s.Placed += OnPlaced;
                s.Taken += OnTaken;
            }
            if (DoorLeaf != null) _doorClosedRot = DoorLeaf.localRotation;
        }

        private void Start()
        {
            // the room is generated, so the walkable surface is baked here, once, from the static geometry
            if (Navigation != null) Navigation.BuildNavMesh();
            var session = GameSession.Instance;
            if (session != null)
            {
                session.Loaded += OnLoaded;
                session.StateChanged += RefreshCapacity;
                if (session.State != null) OnLoaded();
            }
        }

        private void OnDestroy()
        {
            var session = GameSession.Instance;
            if (session != null) { session.Loaded -= OnLoaded; session.StateChanged -= RefreshCapacity; }
            if (Instance == this) Instance = null;
        }

        private void OnLoaded()
        {
            // customers are never persisted; whatever was reserved when the game closed is simply back on its shelf
            foreach (var c in _customers) if (c != null) Destroy(c.gameObject);
            _customers.Clear();
            _queue.Clear();
            AtCounter = null;
            _browseClaims.Clear();
            _nextSpawnIn = 30f;
            RefreshCapacity();
        }

        public void RefreshCapacity()
        {
            var s = GameSession.Instance != null ? GameSession.Instance.State : null;
            int cap = s != null ? s.SaleCapacity : 6;
            for (int i = 0; i < SaleSlots.Count; i++)
            {
                bool locked = i >= cap;
                SaleSlots[i].Locked = locked && SaleSlots[i].IsEmpty;
                SaleSlots[i].DisplayLabel = locked ? "locked sales table" : $"sales slot {i + 1}";
                UpdateLabel(SaleSlots[i]);
            }
        }

        // ---- stock ---------------------------------------------------------------------------
        public static float AskingPrice(SpecimenRecord r) => Mathf.Round(r.EstimatedValue() * Markup * (r.Certified ? 1.1f : 1f));

        private void OnPlaced(PlacementZone z, SpecimenEntity e)
        {
            var session = GameSession.Instance;
            e.Record.AskingPrice = AskingPrice(e.Record);
            WorkshopAudio.Play("crystal_chime", e.transform.position, 0.45f, 1.1f);
            Tutorial.Notify("specimen_sorted");
            Tutorial.Notify("for_sale");
            UpdateLabel(z);
            session.RaiseStateChanged();
            session.FlushSave("for-sale");
            Changed?.Invoke();
        }

        private void OnTaken(PlacementZone z, SpecimenEntity e)
        {
            var session = GameSession.Instance;
            e.Record.AskingPrice = 0f;
            e.Record.Location = SpecimenLocation.World;
            foreach (var c in _customers) if (c != null && c.Wanted == e) c.ItemGone();
            UpdateLabel(z);
            session.RaiseStateChanged();
            Changed?.Invoke();
        }

        /// <summary>Specimens currently on sale and not reserved by another customer.</summary>
        public List<SpecimenEntity> Available(Customer forCustomer)
        {
            var list = new List<SpecimenEntity>();
            foreach (var s in SaleSlots)
            {
                var e = s.First;
                if (e == null || e.Record.Location != SpecimenLocation.SaleSlot) continue;
                bool reserved = false;
                foreach (var c in _customers) if (c != null && c != forCustomer && c.Wanted == e) reserved = true;
                if (!reserved) list.Add(e);
            }
            return list;
        }

        public PlacementZone SlotOf(SpecimenEntity e) => e != null ? e.Zone : null;

        // ---- browse spots: one person per fixture, so nobody stands inside anybody else ---------------------------
        public bool ClaimBrowse(PlacementZone slot, Customer c)
        {
            if (slot == null) return false;
            if (_browseClaims.TryGetValue(slot, out var other) && other != null && other != c && other.State != Customer.Phase.Done) return false;
            _browseClaims[slot] = c;
            return true;
        }

        public Customer BrowseClaimedBy(PlacementZone slot) => slot != null && _browseClaims.TryGetValue(slot, out var c) && c != null ? c : null;

        public void ReleaseBrowse(Customer c)
        {
            List<PlacementZone> mine = null;
            foreach (var kv in _browseClaims) if (kv.Value == c || kv.Value == null) (mine ??= new List<PlacementZone>()).Add(kv.Key);
            if (mine != null) foreach (var k in mine) _browseClaims.Remove(k);
        }

        /// <summary>Typical asking price of what is on the shelves (median), or a modest default for an empty shop.</summary>
        public float StockAnchorPrice()
        {
            var prices = new List<float>();
            foreach (var s in SaleSlots)
            {
                var e = s.First;
                if (e != null && e.Record.Location == SpecimenLocation.SaleSlot) prices.Add(e.Record.AskingPrice > 0f ? e.Record.AskingPrice : AskingPrice(e.Record));
            }
            if (prices.Count == 0) return 90f;
            prices.Sort();
            float median = prices.Count % 2 == 1 ? prices[prices.Count / 2] : 0.5f * (prices[prices.Count / 2 - 1] + prices[prices.Count / 2]);
            // the window's best piece pulls the crowd up: a $110 celestite among $10 quartz still finds its buyer sometimes
            return Mathf.Lerp(median, prices[prices.Count - 1], 0.35f);
        }

        public Transform BrowsePointFor(PlacementZone slot)
        {
            int i = SaleSlots.IndexOf(slot);
            return i >= 0 && i < BrowsePoints.Count ? BrowsePoints[i] : null;
        }

        private void UpdateLabel(PlacementZone z)
        {
            int i = SaleSlots.IndexOf(z);
            if (i < 0 || i >= PriceCards.Count || PriceCards[i] == null) return;
            var card = PriceCards[i];
            if (!_labels.TryGetValue(z, out var tm))
            {
                // sibling of the scaled card prop, placed by hand on its printed face
                tm = UI.WorldLabel.Create(card.parent, LabelFont, LabelMaterial, 0.021f * Mathf.Max(0.1f, card.lossyScale.x), new Color(0.14f, 0.11f, 0.08f), "PriceText");
                tm.transform.SetPositionAndRotation(card.TransformPoint(new Vector3(0f, 0.052f, -0.014f)), card.rotation * Quaternion.Euler(-15f, 0f, 0f));
                tm.LineSpacing = 1.05f;
                _labels[z] = tm;
            }
            var occ = z.First;
            bool reserved = false;
            if (occ != null) foreach (var c in _customers) if (c != null && c.Wanted == occ) reserved = true;
            if (occ != null) tm.Text = ShortName(occ.Record.DisplayName) + "\n" + UI.UiKit.Money(occ.Record.AskingPrice) + (reserved ? "\nRESERVED" : "");
            else tm.Text = z.Locked ? "" : "FOR SALE\n—";
            card.gameObject.SetActive(!z.Locked);
        }

        private static string ShortName(string name) => name.Length > 22 ? name.Substring(0, 21) + "…" : name;

        public void RefreshLabels() { foreach (var s in SaleSlots) UpdateLabel(s); }

        // ---- customers -----------------------------------------------------------------------
        private void Update()
        {
            var session = GameSession.Instance;
            if (session == null || session.State == null) return;
            if (CursorController.InMenu) return;   // paused menus already freeze time; letters/tablet do not: hold the clock
            _nextSpawnIn -= Time.deltaTime;
            if (_nextSpawnIn <= 0f)
            {
                int forSale = session.State.ForSaleCount();
                if (_customers.Count < MaxCustomers && (forSale > 0 || _customers.Count == 0))
                {
                    SpawnCustomer();
                    // a stocked shop draws people; an empty one gets the odd browser who leaves again
                    _nextSpawnIn = forSale > 0 ? UnityEngine.Random.Range(38f, 70f) : UnityEngine.Random.Range(140f, 220f);
                }
                else _nextSpawnIn = 12f;
            }
            // door swings for anyone near the threshold
            bool nearDoor = false;
            if (DoorPoint != null)
                foreach (var c in _customers) if (c != null && (c.transform.position - DoorPoint.position).sqrMagnitude < 1.4f * 1.4f) nearDoor = true;
            _doorOpen = Mathf.MoveTowards(_doorOpen, nearDoor ? 1f : 0f, Time.deltaTime * 1.6f);
            if (DoorLeaf != null) DoorLeaf.localRotation = _doorClosedRot * Quaternion.Euler(0f, -95f * Mathf.SmoothStep(0f, 1f, _doorOpen), 0f);
        }

        /// <summary>Dev/test: bring a customer in right now.</summary>
        public Customer SpawnNow() { SpawnCustomer(); return _customers.Count > 0 ? _customers[_customers.Count - 1] : null; }

        private void SpawnCustomer()
        {
            if (CustomerTemplate == null || OutsidePoint == null) return;
            var go = Instantiate(CustomerTemplate, OutsidePoint.position, OutsidePoint.rotation);
            go.name = "Customer_" + (++_customerCounter);
            go.SetActive(true);
            var c = go.GetComponent<Customer>();
            if (c == null) c = go.AddComponent<Customer>();
            c.Init(this, _customerCounter);
            _customers.Add(c);
            WorkshopAudio.Play("shop_bell", DoorPoint != null ? DoorPoint.position : transform.position, 0.6f);
            Changed?.Invoke();
        }

        public void Remove(Customer c)
        {
            _customers.Remove(c);
            ReleaseBrowse(c);
            if (AtCounter == c) AtCounter = null;
            RefreshLabels();
            Changed?.Invoke();
        }

        /// <summary>A customer with an item joins the line; returns their position index (0 = at the counter).</summary>
        public int JoinQueue(Customer c)
        {
            if (!_queue.Contains(c)) _queue.Enqueue(c);
            RefreshLabels();
            Changed?.Invoke();
            return QueueIndex(c);
        }

        public int QueueIndex(Customer c)
        {
            int i = 0;
            foreach (var q in _queue) { if (q == c) return i; i++; }
            return -1;
        }

        public Transform QueuePoint(int index)
        {
            if (index <= 0) return CounterCustomerPoint;
            int qi = Mathf.Min(index - 1, QueuePoints.Count - 1);
            return qi >= 0 ? QueuePoints[qi] : CounterCustomerPoint;
        }

        public void LeaveQueue(Customer c)
        {
            if (!_queue.Contains(c)) return;
            var rest = new List<Customer>(_queue);
            rest.Remove(c);
            _queue.Clear();
            foreach (var r in rest) _queue.Enqueue(r);
            if (AtCounter == c) AtCounter = null;
            Changed?.Invoke();
        }

        public void ArrivedAtCounter(Customer c)
        {
            _counterSince = Time.time;
            if (AtCounter == c) return;
            AtCounter = c;
            // the bell and the HUD chip carry this; a toast on top of the chip said the same thing twice
            WorkshopAudio.Play("counter_bell", CounterItemPoint != null ? CounterItemPoint.position : transform.position, 0.7f);
            CustomerArrivedAtCounter?.Invoke(c);
            Changed?.Invoke();
        }

        /// <summary>The player rang it up and took the money: the specimen leaves the career for good.</summary>
        public bool CompleteSale(Customer c) => CompleteSale(c, true);

        /// <summary>handOver=false: the money changes hands but the piece stays on the counter for the checkout to pack and pass across.</summary>
        public bool CompleteSale(Customer c, bool handOver)
        {
            var session = GameSession.Instance;
            if (c == null || c.Wanted == null || AtCounter != c) return false;
            var e = c.Wanted;
            var rec = e.Record;
            if (rec.Location == SpecimenLocation.Sold) return false;   // never twice
            float price = rec.AskingPrice > 0f ? rec.AskingPrice : AskingPrice(rec);
            rec.Location = SpecimenLocation.Sold;
            rec.AskingPrice = 0f;
            GameState.Log(rec, "sold", price, "to " + c.Archetype.Name + " at the counter");
            var st = session.State.Stats;
            st.RetailSales++;
            st.RetailRevenue += price;
            st.SpecimensSold++;
            st.CustomersServed++;
            if (price > st.BiggestRetailSale) { st.BiggestRetailSale = price; st.BiggestRetailSaleName = rec.DisplayName; }
            if (price > st.BiggestSale) { st.BiggestSale = price; st.BiggestSaleName = rec.DisplayName; }
            session.AddCash(price, "retail");
            if (handOver) { LeaveQueue(c); c.Paid(e); }   // the piece leaves with the buyer; it is despawned at the door
            else c.AwaitHandover();                        // the checkout packs it and hands it across; the queue moves once they leave
            Metrics.LastSaleSeconds = Time.time - _counterSince;
            Metrics.SaleSecondsTotal += Metrics.LastSaleSeconds; Metrics.SalesTimed++;
            WorkshopAudio.Play2D("register", 0.8f);
            WorkshopAudio.Play2D("crystal_chime", 0.3f, 1.25f);
            session.Notify($"Sold {rec.DisplayName} for {UI.UiKit.Money(price)}", NotificationKind.Success);
            Tutorial.Notify("checkout");
            foreach (var id in SupplierCatalog.EvaluateUnlocks(session.State))
                session.Notify($"New supplier available: {SupplierCatalog.Get(id).Name}", NotificationKind.Discovery);
            var ask = Market.RefreshCommissions(session.State);
            if (ask != null) session.Notify("A buyer wrote in: " + Market.Describe(ask), NotificationKind.Discovery);
            session.RaiseStateChanged();
            session.CheckSolvency();
            session.FlushSave("retail-sold");
            SaleCompleted?.Invoke(c, rec, price);
            Changed?.Invoke();
            return true;
        }

        public void CustomerLeftEmptyHanded()
        {
            var session = GameSession.Instance;
            if (session != null && session.State != null) session.State.Stats.CustomersLeftEmptyHanded++;
        }
    }
}
