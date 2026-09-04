using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;
using GeodeEmpire.Retail;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Checkout
{
    /// <summary>
    /// The physical checkout: the counter, its hardware, and the two machines that drive them — the transaction (what
    /// is legally true about the money) and the flow (what is physically happening). They stay separate on purpose;
    /// every time Golf coupled them, a renderer bug became a money bug.
    ///
    /// The station owns no career state. A sale banks through Geode's own RetailShop, which marks the specimen Sold by
    /// identity exactly once and writes the career atomically; the till's own drawer contents live in the save beside
    /// it and only move when the drawer arithmetic balances.
    /// </summary>
    public sealed class CheckoutStation : MonoBehaviour, IInteractable
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

        // ---- live state -------------------------------------------------------------------------------------
        // runtime only: a serialized transaction or flow would come back from the scene asset as a live half-sale
        [NonSerialized] private CheckoutFlow _flow = new CheckoutFlow();
        [NonSerialized] private RegisterTransaction _tx;
        public CheckoutFlow Flow => _flow;
        public RegisterTransaction Tx => _tx;
        public bool Active { get; private set; }
        public bool Busy { get; private set; }
        public string StatusLine { get; private set; } = "";
        /// <summary>Breadcrumb of the last physical milestone, so a stalled sequence names itself.</summary>
        public string Trace { get; private set; } = "idle";
        private void Mark(string where) => Trace = where;
        public CheckoutState State => Flow.Current;
        public MoneyStack Drawer => _drawer;
        public CheckoutTarget Hovered { get; private set; }

        private CheckoutScreens _screens;
        private CheckoutMoneyRig _money;
        private CheckoutBagRig _bagRig;
        private Customer _customer;
        private readonly List<SpecimenEntity> _pieces = new List<SpecimenEntity>();
        private MoneyStack _drawer;
        private GameObject _card;
        private FirstPersonController _controller;
        private PlayerInteractor _player;
        private float _drawerTarget;
        private Vector3 _trayHome;
        private bool _trayHomeSet;
        private readonly List<CheckoutTarget> _targets = new List<CheckoutTarget>();
        private int _cycleIndex = -1;
        private float _busyClock;
        private int _ticketNumber;

        public float DrawerOpen { get; private set; }

        // ------------------------------------------------------------------------------------------------------
        // lifecycle
        // ------------------------------------------------------------------------------------------------------
        private void Awake()
        {
            CacheTrayHome();
            _screens = gameObject.AddComponent<CheckoutScreens>();
            _money = gameObject.AddComponent<CheckoutMoneyRig>();
            _bagRig = gameObject.AddComponent<CheckoutBagRig>();
            _money.Library = Library; _money.DrawerRig = DrawerRig; _money.Counter = Counter;
            _bagRig.Library = Library; _bagRig.Counter = Counter;
        }

        private void Start()
        {
            _screens.Build(MonitorRig, TerminalRig, CustomerDisplayRig);
            _screens.ShowIdle();
            _drawer = LoadDrawer();
            _money.RefreshDrawer(_drawer);
            if (Shop != null) Shop.CustomerArrivedAtCounter += OnCustomerArrived;
            AttachWellTargets();
            AttachKeyTargets();
        }

        private void OnDestroy()
        {
            if (Shop != null) Shop.CustomerArrivedAtCounter -= OnCustomerArrived;
        }

        private void CacheTrayHome()
        {
            if (_trayHomeSet || DrawerRig == null || DrawerRig.Tray == null) return;
            _trayHome = DrawerRig.Tray.localPosition;
            _trayHomeSet = true;
        }

        private MoneyStack LoadDrawer()
        {
            var state = GameSession.Instance != null ? GameSession.Instance.State : null;
            if (state == null) return Money.NewDrawer();
            if (state.CashDrawer == null || state.CashDrawer.Pieces == 0) state.CashDrawer = Money.NewDrawer();
            else state.CashDrawer = Money.MigrateDrawer(state.CashDrawer);
            return state.CashDrawer;
        }

        // ------------------------------------------------------------------------------------------------------
        // the customer arrives and stages their goods
        // ------------------------------------------------------------------------------------------------------
        private void OnCustomerArrived(Customer c)
        {
            if (c == null || c.Wanted == null) return;
            if (Tx != null && Flow.Current != CheckoutState.TransactionComplete) return;
            Begin(c);
        }

        public void Begin(Customer c)
        {
            _customer = c;
            _pieces.Clear();
            _pieces.Add(c.Wanted);
            var session = GameSession.Instance;
            _ticketNumber = session != null && session.State != null ? session.State.Stats.RetailSales + 1 : 1;
            var lines = new List<TicketLine>();
            foreach (var p in _pieces)
                lines.Add(new TicketLine
                {
                    Uid = p.Record.Id,
                    Name = p.Record.DisplayName,
                    Price = p.Record.AskingPrice > 0f ? p.Record.AskingPrice : RetailShop.AskingPrice(p.Record),
                });
            var rng = new System.Random(unchecked((int)(c.Wanted.Record.Seed ^ 0x9E3779B9UL)));
            _tx = RegisterTransaction.Create(lines, c.Method == Customer.Payment.Cash ? PaymentMethod.Cash : PaymentMethod.Card, rng, c.Archetype.Name);
            _flow = new CheckoutFlow { Current = CheckoutState.CustomerApproaching, EnteredAt = Time.time };
            Flow.To(CheckoutState.CustomerPlacingProducts, Time.time, "customer at the counter");
            StatusLine = "";
            StartCoroutine(StageGoods());
        }

        /// <summary>At most one product starts or finishes placement per call: even a long frame cannot teleport an order onto the counter.</summary>
        private IEnumerator StageGoods()
        {
            for (int i = 0; i < _pieces.Count; i++)
            {
                var piece = _pieces[i];
                if (piece == null) continue;
                piece.SetPhysics(false);
                piece.SetCollidersEnabled(false);
                piece.Locked = true;
                Vector3 from = piece.transform.position;
                Vector3 to = StagePose(i, out Quaternion rot);
                yield return Arc(piece.transform, from, to, rot, Layout.ProductPlaceSeconds);
                WorkshopAudio.Play("rock_place", to, 0.55f);
                AttachPieceTarget(piece);
            }
            Flow.To(CheckoutState.WaitingForCashier, Time.time, "goods staged");
            _screens.ShowTransaction(Tx, "products-ready", "Press to work the register.", _ticketNumber);
        }

        private Vector3 StagePose(int index, out Quaternion rot)
        {
            var r = Layout.Staging;
            float t = _pieces.Count <= 1 ? 0.5f : index / (float)(_pieces.Count - 1);
            float x = Mathf.Lerp(r.MinX + 0.08f, r.MaxX - 0.08f, t);
            var local = new Vector3(x, Layout.TopY, r.CentreZ);
            var piece = _pieces[index];
            float lift = piece != null ? piece.RestHeightOffset(piece.IsOpened) : 0f;
            rot = Counter.rotation;
            return Counter.TransformPoint(local) + Vector3.up * lift;
        }

        private static IEnumerator Arc(Transform t, Vector3 from, Vector3 to, Quaternion rot, float duration, float arc = 0.10f)
        {
            float k = 0f;
            Quaternion fromRot = t.rotation;
            while (k < 1f)
            {
                k += Time.deltaTime / Mathf.Max(0.01f, duration);
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k));
                t.SetPositionAndRotation(Vector3.Lerp(from, to, e) + Vector3.up * (Mathf.Sin(e * Mathf.PI) * arc),
                                         Quaternion.Slerp(fromRot, rot, e));
                yield return null;
            }
            t.SetPositionAndRotation(to, rot);
        }

        // ------------------------------------------------------------------------------------------------------
        // entering and leaving the station
        // ------------------------------------------------------------------------------------------------------
        public bool CanInteract(PlayerInteractor player) => !Active && Tx != null && Flow.Current == CheckoutState.WaitingForCashier;

        public string GetPrompt(PlayerInteractor player) => Active ? "" : "Work the register";

        public string GetHint(PlayerInteractor player) => Tx != null ? $"{Tx.CustomerName} is waiting" : "";

        public void Interact(PlayerInteractor player)
        {
            if (!CanInteract(player)) return;
            _player = player;
            Enter();
        }

        /// <summary>The register block lights when the player can take it; the rest of the counter stays quiet.</summary>
        public void SetHighlight(bool on)
        {
            if (MonitorRig == null) return;
            _stationHighlight ??= new MaterialPropertyBlock();
            foreach (var r in MonitorRig.GetComponentsInChildren<Renderer>())
            {
                for (int i = 0; i < r.sharedMaterials.Length; i++)
                {
                    r.GetPropertyBlock(_stationHighlight, i);
                    _stationHighlight.SetColor(HighlightId, on ? new Color(0.16f, 0.28f, 0.15f) : Color.black);
                    r.SetPropertyBlock(_stationHighlight, i);
                }
            }
        }

        private MaterialPropertyBlock _stationHighlight;
        private static readonly int HighlightId = Shader.PropertyToID("_EmissionColor");

        public void Enter()
        {
            if (Active) return;
            if (Flow.Current == CheckoutState.CustomerPlacingProducts) Flow.To(CheckoutState.WaitingForCashier, Time.time, "goods already down");
            Active = true;
            _controller = FindAnyObjectByType<FirstPersonController>();
            if (_controller != null) _controller.EnterStationView(WorkingCamera, Layout.WorkingFov);
            if (_player == null) _player = FindAnyObjectByType<PlayerInteractor>();
            if (_player != null) _player.InputLocked = true;   // the world prompt must not float over the counter
            CursorController.EnterMenu();
            WorkshopAudio.Play2D("ui_click", 0.5f);
            Flow.To(CheckoutState.EnteringCashierMode, Time.time, "player took the register");
            Flow.To(CheckoutState.WaitingForScan, Time.time, "station ready");
            StatusLine = "Ring up the order";
            RefreshScreens();
        }

        public void Exit()
        {
            if (!Active) return;
            Active = false;
            if (_controller != null) _controller.ExitStationView();
            if (_player != null) _player.InputLocked = false;
            CursorController.ExitMenu();
            SetHovered(null);
        }

        // ------------------------------------------------------------------------------------------------------
        // targets and picking
        // ------------------------------------------------------------------------------------------------------
        private void AttachPieceTarget(SpecimenEntity piece)
        {
            var t = CheckoutTarget.Attach(piece.gameObject, CheckoutTargetKind.Piece, piece.Record.Id,
                                          boxSize: Vector3.one * Mathf.Max(0.08f, piece.Radius * 2.2f));
            Register(t);
        }

        private void AttachWellTargets()
        {
            if (DrawerRig == null) return;
            foreach (var well in DrawerRig.Wells)
            {
                if (well.Socket == null) continue;
                float denom = well.Coin ? WellDenom(well.Denomination) : float.Parse(well.Denomination);
                var go = new GameObject($"Well_{well.Denomination}");
                go.transform.SetParent(well.Socket, false);
                var t = CheckoutTarget.Attach(go, CheckoutTargetKind.DrawerWell, well.Denomination, denom,
                                              boxSize: new Vector3(well.WellW, 0.05f, well.WellD),
                                              boxCentre: new Vector3(0f, 0.02f, 0f));
                Register(t);
            }
        }

        /// <summary>The kit's fourth coin well is labelled 20 because a sheet authored one; the quarter is the real coin and lives there.</summary>
        private static float WellDenom(string key) => key == "20" ? 0.25f : int.Parse(key) / 100f;

        private void AttachKeyTargets()
        {
            if (TerminalRig == null) return;
            foreach (var r in TerminalRig.Refs)
            {
                string action = TerminalKeyAction(r.Name);
                if (action == null || r.Target == null) continue;
                var t = CheckoutTarget.Attach(r.Target.gameObject, CheckoutTargetKind.TerminalKey, action,
                                              boxSize: new Vector3(0.024f, 0.014f, 0.006f));
                Register(t);
            }
        }

        /// <summary>The whole key mapping, kept in one place so the picker, the keyboard and the tests read one table.</summary>
        public static string TerminalKeyAction(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName)) return null;
            const string prefix = "Terminal_Key_";
            if (nodeName.StartsWith(prefix) && nodeName.Length == prefix.Length + 1 && char.IsDigit(nodeName[prefix.Length]))
                return "digit:" + nodeName[prefix.Length];
            if (nodeName == "Terminal_ConfirmButton") return "confirm";
            if (nodeName == "Terminal_BackButton") return "backspace";
            if (nodeName == "Terminal_CancelButton") return "clear";
            return null;
        }

        private void Register(CheckoutTarget t)
        {
            if (t != null && !_targets.Contains(t)) _targets.Add(t);
        }

        private void SetHovered(CheckoutTarget t)
        {
            if (Hovered == t) return;
            if (Hovered != null) Hovered.SetHighlight(false);
            Hovered = t;
            if (Hovered != null) Hovered.SetHighlight(true);
        }

        private bool Live(CheckoutTarget t)
        {
            if (t == null || !t.isActiveAndEnabled || Tx == null) return false;
            switch (t.Kind)
            {
                case CheckoutTargetKind.Piece: return Tx.Stage == TxStage.Scanning && Tx.Find(t.Payload) is { Scanned: false };
                case CheckoutTargetKind.Tender: return Tx.Stage == TxStage.CashTender;
                case CheckoutTargetKind.DrawerWell: return Tx.Stage == TxStage.CashDrawer && Tx.Deposited && DrawerOpen > 0.6f;
                case CheckoutTargetKind.TerminalKey: return Tx.Stage == TxStage.CardEntry;
                case CheckoutTargetKind.Card: return Tx.Stage == TxStage.CardReady;
                default: return false;
            }
        }

        private void UpdatePicking()
        {
            if (!Active || Busy) { SetHovered(null); return; }
            var cam = Camera.main;
            if (cam == null) return;
            if (GameInput.UsingGamepad)
            {
                var live = new List<CheckoutTarget>();
                foreach (var t in _targets) if (Live(t)) live.Add(t);
                if (live.Count == 0) { SetHovered(null); _cycleIndex = -1; return; }
                float axis = GameInput.Move.x;
                if (Mathf.Abs(axis) > 0.6f && Time.time - _busyClock > 0.25f)
                {
                    _busyClock = Time.time;
                    _cycleIndex = (_cycleIndex + (axis > 0f ? 1 : live.Count - 1) + live.Count) % live.Count;
                }
                if (_cycleIndex < 0) _cycleIndex = 0;
                SetHovered(live[Mathf.Clamp(_cycleIndex, 0, live.Count - 1)]);
                return;
            }
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return;
            var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            CheckoutTarget best = null;
            float bestDist = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(ray, 6f, ~0, QueryTriggerInteraction.Collide))
            {
                var t = hit.collider.GetComponentInParent<CheckoutTarget>();
                if (t == null || !Live(t) || hit.distance >= bestDist) continue;
                best = t; bestDist = hit.distance;
            }
            SetHovered(best);
        }

        /// <summary>When nothing is pointed at, the obvious next thing is what the press means.</summary>
        private CheckoutTarget ObviousTarget()
        {
            if (Tx == null) return null;
            CheckoutTarget first = null;
            bool ambiguous = false;
            foreach (var t in _targets)
            {
                if (!Live(t) || t.Kind == CheckoutTargetKind.DrawerWell || t.Kind == CheckoutTargetKind.TerminalKey) continue;
                if (first == null) { first = t; continue; }
                // a laid-out handful of notes is one thing you take, however many pieces it is drawn from
                if (t.Kind != first.Kind) ambiguous = true;
                else if (t.Kind != CheckoutTargetKind.Tender) ambiguous = true;
            }
            return ambiguous ? null : first;
        }

        // ------------------------------------------------------------------------------------------------------
        // frame
        // ------------------------------------------------------------------------------------------------------
        private void Update()
        {
            float dt = Time.deltaTime;
            AnimateDrawer(dt);
            if (!Active) return;
            UpdatePicking();
            if (Busy) return;

            if (GameInput.BackPressed) { Exit(); return; }
            if (GameInput.InteractPressed || (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame))
                Activate(Hovered ?? ObviousTarget());
            HandleKeyboard();
            CheckWatchdog();
        }

        private void HandleKeyboard()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || Tx == null) return;
            if (Tx.Stage == TxStage.CardEntry)
            {
                for (int d = 0; d <= 9; d++)
                    if (kb[(UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.Digit1 + (d == 0 ? 9 : d - 1))].wasPressedThisFrame)
                        Keypad("digit:" + d);
                if (kb.backspaceKey.wasPressedThisFrame) Keypad("backspace");
                if (kb.deleteKey.wasPressedThisFrame) Keypad("clear");
                if (kb.enterKey.wasPressedThisFrame) Keypad("confirm");
            }
            if (Tx.Stage == TxStage.CashDrawer && Tx.Deposited)
            {
                if (kb.zKey.wasPressedThisFrame) UndoChange();
                if (kb.spaceKey.wasPressedThisFrame) ConfirmChange();
            }
        }

        private void CheckWatchdog()
        {
            if (Tx == null || Busy) return;
            if (!Flow.TimedOut(Time.time)) return;
            var facts = Facts();
            Flow.EnterRecovery(Time.time, Flow.Spec.TimeoutReason, facts);
            StatusLine = "Restoring the counter...";
            RefreshScreens();
            var target = Flow.RecoveryResume ?? CheckoutState.WaitingForScan;
            Flow.Resume(Time.time, target, "recovered");
        }

        /// <summary>Every transition the station asks for must be legal; a refusal is a contract break worth hearing about.</summary>
        private void Go(CheckoutState next, string reason)
        {
            if (!Flow.To(next, Time.time, reason))
                Debug.LogWarning($"[Checkout] illegal transition {Flow.Current} -> {next} ({reason})");
        }

        private CheckoutFacts Facts() => new CheckoutFacts
        {
            PaymentAuthorized = Tx != null && Tx.CardResult == "approved",
            CashAccepted = Tx != null && Tx.AcceptedTender != null && !Tx.AcceptedTender.Empty,
            SaleBanked = Tx != null && Tx.Banked,
            BagOwned = _bagRig != null && _bagRig.Bag == null && Tx != null && Tx.Stage == TxStage.Done,
            AnyScanned = Tx != null && Tx.UnscannedCount < Tx.Items.Count,
            AllScanned = Tx != null && Tx.AllScanned,
        };

        /// <summary>Inspection only: hold the drawer at a given openness so its contents can be photographed.</summary>
        public void SetDrawerOpenForInspection(float open)
        {
            _drawerTarget = Mathf.Clamp01(open);
            DrawerOpen = _drawerTarget;
            PlaceTray();
        }

        /// <summary>
        /// The tray slides out toward the CASHIER. The direction is taken from the counter, not from an axis of the
        /// tray: the kit's own node carries a baked axis conversion, and assuming its local forward slid the drawer
        /// straight up through the counter top.
        /// </summary>
        private void PlaceTray()
        {
            CacheTrayHome();
            if (DrawerRig == null || DrawerRig.Tray == null || Counter == null) return;
            var parent = DrawerRig.Tray.parent != null ? DrawerRig.Tray.parent : DrawerRig.transform;
            Vector3 outward = parent.InverseTransformDirection(Counter.forward);
            float travel = Layout != null ? Layout.DrawerTravel : DrawerRig.TrayTravel;
            DrawerRig.Tray.localPosition = _trayHome + outward.normalized * (travel * DrawerOpen);
        }

        private void AnimateDrawer(float dt)
        {
            float speed = _drawerTarget > DrawerOpen ? Layout.DrawerOpenSpeed : Layout.DrawerCloseSpeed;
            DrawerOpen = Mathf.MoveTowards(DrawerOpen, _drawerTarget, speed * dt * 0.55f);
            PlaceTray();
        }

        // ------------------------------------------------------------------------------------------------------
        // the sequence
        // ------------------------------------------------------------------------------------------------------
        private void Activate(CheckoutTarget t)
        {
            if (t == null || Tx == null) return;
            switch (t.Kind)
            {
                case CheckoutTargetKind.Piece: StartCoroutine(RingUp(t.Payload)); break;
                case CheckoutTargetKind.Tender: StartCoroutine(TakeCash()); break;
                case CheckoutTargetKind.Card: StartCoroutine(InsertCard()); break;
                case CheckoutTargetKind.DrawerWell: TakeChangePiece(t.Denom); break;
                case CheckoutTargetKind.TerminalKey: Keypad(t.Payload); break;
            }
        }

        /// <summary>
        /// ONE FORGIVING PRESS OWNS THE WHOLE GESTURE. The piece slides along the counter into the bag's mouth; the
        /// register rings at the middle of the slide, where the barcode would have crossed, never on the press itself.
        /// </summary>
        private IEnumerator RingUp(string uid)
        {
            var line = Tx.Find(uid);
            var piece = _pieces.Find(p => p != null && p.Record.Id == uid);
            if (line == null || piece == null || line.Scanned) yield break;
            Busy = true;
            Flow.To(CheckoutState.ProductHeld, Time.time, "picked up");
            WorkshopAudio.Play("rock_pickup", piece.transform.position, 0.5f);
            bool bagged = CheckoutBagRig.ShouldBag(piece);
            if (bagged && _bagRig.Bag == null) _bagRig.Lay(new Vector3(Layout.Bagging.CentreX, Layout.TopY, Layout.Bagging.CentreZ));
            Flow.To(CheckoutState.ProductScanning, Time.time, "sliding to the bag");

            Vector3 from = piece.transform.position;
            Vector3 to = bagged
                ? _bagRig.Bag.transform.TransformPoint(_bagRig.PlacementFor(piece))
                : Counter.TransformPoint(new Vector3(Layout.ScannedStaging.CentreX, Layout.TopY + piece.RestHeightOffset(piece.IsOpened), Layout.ScannedStaging.CentreZ));
            float k = 0f;
            bool rang = false;
            while (k < 1f)
            {
                k += Time.deltaTime / Layout.SlideDuration;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k));
                piece.transform.position = Vector3.Lerp(from, to, e) + Vector3.up * (Mathf.Sin(e * Mathf.PI) * 0.05f);
                if (!rang && e >= 0.5f)
                {
                    rang = true;
                    Tx.ScanItem(uid);
                    Tx.BagScannedItem(uid);
                    WorkshopAudio.Play2D("register_beep", 0.55f);
                    RefreshScreens();
                }
                yield return null;
            }
            piece.transform.position = to;
            if (bagged) piece.transform.SetParent(_bagRig.Bag.transform, true);
            WorkshopAudio.Play(bagged ? "crate_open" : "rock_place", to, 0.4f, 1.2f);
            var target = piece.GetComponent<CheckoutTarget>();
            if (target != null) target.SetHighlight(false);
            Flow.To(CheckoutState.ProductScanned, Time.time, "in the bag");
            Busy = false;

            if (Tx.AllScanned)
            {
                Flow.To(CheckoutState.AllProductsScanned, Time.time, "everything rung up");
                RefreshScreens();
                yield return new WaitForSeconds(Layout.AutoPaymentHold);
                StartCoroutine(BeginPayment());
            }
            else
            {
                Flow.To(CheckoutState.WaitingForScan, Time.time, "next item");
                RefreshScreens();
            }
        }

        private IEnumerator BeginPayment()
        {
            Busy = true;
            Flow.To(CheckoutState.ChoosingPayment, Time.time, "asking for payment");
            var res = Tx.RequestPayment();
            if (!res.Ok) { StatusLine = res.Reason; Busy = false; yield break; }
            RefreshScreens();
            yield return new WaitForSeconds(0.35f);
            if (Tx.Method == PaymentMethod.Cash) yield return PresentCash();
            else yield return PresentCard();
            Busy = false;
        }

        // ---- cash ------------------------------------------------------------------------------------------
        private IEnumerator PresentCash()
        {
            Tx.CustomerCash();
            Flow.To(CheckoutState.CashPresented, Time.time, "customer counts it out");
            StatusLine = $"Take the {UI.UiKit.Money(Tx.Tendered.Total)}";
            if (_customer != null) _customer.Reach(true);
            yield return new WaitForSeconds(0.5f);
            _money.ShowTender(Tx.Tendered, new Vector3(Layout.CustomerTender.CentreX, Layout.TopY, Layout.CustomerTender.CentreZ));
            foreach (var go in _money.TenderPieces)
                Register(CheckoutTarget.Attach(go, CheckoutTargetKind.Tender, "", boxSize: new Vector3(0.18f, 0.05f, 0.10f)));
            WorkshopAudio.Play("rock_place", TenderPoint.position, 0.35f, 1.4f);
            if (_customer != null) _customer.Reach(false);
            RefreshScreens();
        }

        private IEnumerator TakeCash()
        {
            if (Tx.Stage != TxStage.CashTender) yield break;
            Busy = true;
            Mark("takeCash:accept");
            var res = Tx.AcceptCash();
            if (!res.Ok) { StatusLine = res.Reason; Busy = false; yield break; }
            Flow.To(CheckoutState.CashAccepted, Time.time, "cash taken");
            WorkshopAudio.Play2D("rock_pickup", 0.5f, 1.2f);
            _money.ClearTender();
            RefreshScreens();
            yield return new WaitForSeconds(0.25f);

            Mark("takeCash:opening");
            Flow.To(CheckoutState.DrawerOpening, Time.time, "opening the drawer");
            Tx.OpenDrawer();
            _drawerTarget = 1f;
            WorkshopAudio.Play("register", DrawerRig.transform.position, 0.6f);
            if (_controller != null) _controller.EnterStationView(DrawerCamera, Layout.DrawerFov);
            float openGuard = 0f;
            while (DrawerOpen < 0.98f && openGuard < 4f) { openGuard += Time.deltaTime; yield return null; }
            Mark($"takeCash:opened({DrawerOpen:F2})");

            Flow.To(CheckoutState.DepositingCash, Time.time, "stowing the cash");
            StatusLine = "Putting it in the till";
            Mark("takeCash:depositing");
            var dep = Tx.DepositTendered(_drawer);
            Mark($"takeCash:deposited ok={dep.Ok} {dep.Reason}");
            _money.RefreshDrawer(Tx.DrawerContents(_drawer));
            Mark("takeCash:drawerDrawn");
            WorkshopAudio.Play("register", DrawerRig.transform.position, 0.35f, 1.3f);
            yield return new WaitForSeconds(0.35f);

            // even an exact payment goes through the change step: the flow contract has no edge that skips it, and the
            // player still closes the drawer themselves
            Mark("takeCash:selecting");
            Go(CheckoutState.SelectingChange, "counting the change");
            StatusLine = Money.Cents(Tx.ChangeDue) == 0 ? "Exact - close the drawer" : $"Count {UI.UiKit.Money(Tx.ChangeDue)} change";
            RefreshScreens();
            Busy = false;
        }

        private void TakeChangePiece(float denom)
        {
            if (Tx == null || Tx.Stage != TxStage.CashDrawer || !Tx.Deposited) return;
            var res = Tx.TakeFromDrawer(_drawer, denom);
            if (!res.Ok) { StatusLine = res.Reason; WorkshopAudio.Play2D("ui_error", 0.4f); return; }
            WorkshopAudio.Play2D(Money.IsBill(denom) ? "wood_knock" : "tick", 0.35f, Money.IsBill(denom) ? 1.6f : 1.2f);
            _money.RefreshDrawer(Tx.DrawerContents(_drawer));
            _money.ShowChange(Tx.Hand, new Vector3(Layout.ChangeHandoff.CentreX, Layout.TopY, Layout.ChangeHandoff.CentreZ));
            var state = Tx.ChangeGivingState(out _);
            StatusLine = state == ChangeState.Exact ? "Exact - hand it across" : $"Counting {UI.UiKit.Money(Tx.HandTotal)}";
            RefreshScreens();
        }

        private void UndoChange()
        {
            if (Tx == null || Tx.Hand.Empty) return;
            for (int i = Money.Denoms.Length - 1; i >= 0; i--)
            {
                if (Tx.Hand[i] <= 0) continue;
                Tx.ReturnToDrawer(_drawer, Money.Denoms[i]);
                break;
            }
            WorkshopAudio.Play2D("tick", 0.3f, 0.9f);
            _money.RefreshDrawer(Tx.DrawerContents(_drawer));
            _money.ShowChange(Tx.Hand, new Vector3(Layout.ChangeHandoff.CentreX, Layout.TopY, Layout.ChangeHandoff.CentreZ));
            RefreshScreens();
        }

        /// <summary>Confirm the counted change from an input press (Space, the POS Done button, or the controller).</summary>
        public void ConfirmChangeFromInput() => ConfirmChange();

        private void ConfirmChange()
        {
            if (Tx == null || Busy || Tx.Stage != TxStage.CashDrawer || !Tx.Deposited) return;
            StartCoroutine(GiveChange());
        }

        private IEnumerator GiveChange()
        {
            Busy = true;
            Mark("giveChange:start");
            var res = Tx.HandOverChange();
            if (!res.Ok)
            {
                StatusLine = res.Reason;
                WorkshopAudio.Play2D("ui_error", 0.45f);
                Busy = false;
                yield break;
            }
            Flow.To(CheckoutState.GivingChange, Time.time, "handing the change across");
            StatusLine = "";
            if (_customer != null) _customer.Receive(true);
            var pieces = new List<GameObject>(_money.ChangePieces);
            Vector3 hand = _customer != null && _customer.HandPoint != null ? _customer.HandPoint.position : Counter.TransformPoint(new Vector3(0f, Layout.TopY, -0.5f));
            float k = 0f;
            var starts = new List<Vector3>();
            foreach (var p in pieces) starts.Add(p != null ? p.transform.position : hand);
            while (k < 1f)
            {
                k += Time.deltaTime / 0.55f;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k));
                var target = _customer != null && _customer.HandPoint != null ? _customer.HandPoint.position : hand;
                for (int i = 0; i < pieces.Count; i++)
                    if (pieces[i] != null) pieces[i].transform.position = Vector3.Lerp(starts[i], target, e) + Vector3.up * (Mathf.Sin(e * Mathf.PI) * 0.06f);
                yield return null;
            }
            _money.ClearChange();
            if (_customer != null) _customer.Receive(false);
            WorkshopAudio.Play2D("tick", 0.4f, 1.1f);
            _drawerTarget = 0f;
            Tx.CloseDrawer();
            WorkshopAudio.Play("register", DrawerRig.transform.position, 0.45f, 0.9f);
            if (_controller != null) _controller.EnterStationView(WorkingCamera, Layout.WorkingFov);
            yield return new WaitForSeconds(0.3f);
            Busy = false;
            StartCoroutine(Settle());
        }

        // ---- card ------------------------------------------------------------------------------------------
        private IEnumerator PresentCard()
        {
            Tx.PresentCard();
            Flow.To(CheckoutState.CardPresented, Time.time, "customer offers a card");
            StatusLine = "Take the card";
            if (_customer != null) _customer.Reach(true);
            _card = Library.Instantiate("payment_card", Counter);
            _card.name = "Card";
            Register(CheckoutTarget.Attach(_card, CheckoutTargetKind.Card, "", boxSize: new Vector3(0.09f, 0.03f, 0.06f)));
            yield return new WaitForSeconds(0.35f);
            Flow.To(CheckoutState.CardInsertReady, Time.time, "card is out");
            if (_controller != null) _controller.EnterStationView(CardCamera, Layout.CardFov);
            RefreshScreens();
            _screens.ShowTerminal("READY", "", "Insert the card");
        }

        private void Update_CardHold()
        {
            if (_card == null || _customer == null || Tx == null) return;
            if (Tx.Stage != TxStage.CardPresent && Tx.Stage != TxStage.CardReady) return;
            // re-assert the offer every frame: the customer controller would otherwise drop the arm, and the card with it
            var hand = _customer.HandPoint != null ? _customer.HandPoint : _customer.transform;
            _card.transform.position = hand.position + Vector3.up * 0.02f;
            _card.transform.rotation = Quaternion.LookRotation(Counter.forward, Vector3.up) * Quaternion.Euler(CardHeldPitch, 0f, 0f);
        }

        private const float CardHeldPitch = 35.5f;   // 0.62 rad: the angle a card is actually held out at

        private IEnumerator InsertCard()
        {
            if (Tx.Stage != TxStage.CardReady) yield break;
            Busy = true;
            if (_customer != null) _customer.Reach(false);
            Flow.To(CheckoutState.CardInserting, Time.time, "inserting");
            var slot = TerminalRig.Find("CARD_INSERT_SOCKET") ?? TerminalRig.transform;
            Vector3 from = _card.transform.position;
            Quaternion fromRot = _card.transform.rotation;
            Vector3 to = slot.position;
            Quaternion toRot = slot.rotation;
            float k = 0f;
            while (k < 1f)
            {
                k += Time.deltaTime / Layout.CardInsertTime;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k));
                _card.transform.SetPositionAndRotation(Vector3.Lerp(from, to, e), Quaternion.Slerp(fromRot, toRot, e));
                yield return null;
            }
            _card.transform.SetParent(slot, true);
            WorkshopAudio.Play("wood_knock", slot.position, 0.35f, 1.5f);
            var res = Tx.InsertCard();
            if (!res.Ok) { StatusLine = res.Reason; Busy = false; yield break; }
            Flow.To(CheckoutState.CardAmountEntry, Time.time, "terminal open");
            StatusLine = "Key the total, then OK";
            _screens.ShowTerminal("ENTER AMOUNT", "0.00", "Type the total, press OK");
            RefreshScreens();
            Busy = false;
        }

        private void Keypad(string action)
        {
            if (Tx == null || Tx.Stage != TxStage.CardEntry || Busy) return;
            WorkshopAudio.Play2D("ui_click", 0.35f, 1.3f);
            if (action.StartsWith("digit:")) Tx.EnterCardDigit(action[6] - '0');
            else if (action == "backspace") Tx.BackspaceCardAmount();
            else if (action == "clear") Tx.ClearCardAmount();
            else if (action == "confirm")
            {
                var res = Tx.SubmitCardAmount();
                if (!res.Ok)
                {
                    _screens.ShowTerminal(Tx.CardEntryError ?? "ERROR", Amount(), "Try again", new Color(1f, 0.5f, 0.45f));
                    WorkshopAudio.Play2D("ui_error", 0.4f);
                    return;
                }
                StartCoroutine(RunCard());
                return;
            }
            _screens.ShowTerminal("ENTER AMOUNT", Amount(), "Type the total, press OK");
        }

        private string Amount() => Tx.CardEntryDigits.Length == 0 ? "0.00" : (Tx.CardEntryCents / 100f).ToString("0.00");

        private IEnumerator RunCard()
        {
            Busy = true;
            Flow.To(CheckoutState.CardProcessing, Time.time, "authorising");
            StatusLine = "Processing...";
            float t = 0f;
            while (t < Layout.CardAuthTime)
            {
                t += Time.deltaTime;
                int dots = CheckoutPresentation.TerminalBusyDotPhase(t, Layout.TerminalBusyDotHz) + 1;
                _screens.ShowTerminal("PROCESSING", new string('.', dots), "Do not remove the card", new Color(0.9f, 0.85f, 0.5f));
                yield return null;
            }
            Tx.RunCard();
            Flow.To(CheckoutState.CardApproved, Time.time, "approved");
            _screens.ShowTerminal("APPROVED", UI.UiKit.Money(Tx.Total), "Remove the card", new Color(0.45f, 0.95f, 0.6f));
            WorkshopAudio.Play2D("crystal_chime", 0.4f, 1.3f);
            yield return new WaitForSeconds(0.5f);
            if (_card != null) { Destroy(_card); _card = null; }
            if (_controller != null) _controller.EnterStationView(WorkingCamera, Layout.WorkingFov);
            Busy = false;
            StartCoroutine(Settle());
        }

        // ---- settle, pack, hand over ------------------------------------------------------------------------
        private IEnumerator Settle()
        {
            Busy = true;
            Mark("settle:start");
            Flow.To(CheckoutState.PaymentComplete, Time.time, "payment complete");
            StatusLine = "";
            RefreshScreens();
            Bank();
            yield return new WaitForSeconds(0.2f);
            Flow.To(CheckoutState.ReceiptPrinting, Time.time, "closing the sale");
            Tx.CloseSale();
            Flow.To(CheckoutState.Bagging, Time.time, "packing");
            yield return new WaitForSeconds(0.25f);
            foreach (var line in Tx.Items) Tx.BagItem(line.Uid);
            Tx.HandOverGoods();
            Flow.To(CheckoutState.BagHandoff, Time.time, "handing it across");
            yield return HandOver();
            Busy = false;
        }

        /// <summary>The sale banks exactly once, through the career's own books, and the till's contents move with it.</summary>
        private void Bank()
        {
            if (Tx == null || Tx.Banked) return;
            var commit = Tx.CommitFor(_drawer);
            if (!commit.Ok)
            {
                StatusLine = commit.Reason;
                Debug.LogWarning("[Checkout] " + commit.Reason);
                return;
            }
            var session = GameSession.Instance;
            if (Shop != null && _customer != null) Shop.CompleteSale(_customer, false);
            if (commit.Contents != null && session != null && session.State != null)
            {
                session.State.CashDrawer = commit.Contents;
                _drawer = session.State.CashDrawer;
                _money.RefreshDrawer(_drawer);
            }
            if (Tx.Lost > 0f && session != null) session.AddCash(-Tx.Lost, "cash-over-short");
            Tx.Banked = true;
        }

        private IEnumerator HandOver()
        {
            Mark("handover:start");
            Transform carried = _bagRig.Bag != null ? _bagRig.Bag.transform : (_pieces.Count > 0 && _pieces[0] != null ? _pieces[0].transform : null);
            if (carried == null) yield break;
            bool twoHands = _bagRig.Bag == null;
            if (_customer != null) _customer.Receive(true, _bagRig.Bag, twoHands ? _pieces[0] : null);
            carried.SetParent(null, true);
            Vector3 from = carried.position;
            Quaternion fromRot = carried.rotation;
            float k = 0f;
            while (k < 1f)
            {
                k += Time.deltaTime / Layout.BagDeliverTime;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k));
                Vector3 hp = from;
                Quaternion hr = fromRot;
                if (_customer != null) _customer.HoldPose(out hp, out hr);
                carried.SetPositionAndRotation(Vector3.Lerp(from, hp, e) + Vector3.up * (Mathf.Sin(e * Mathf.PI) * 0.05f),
                                               Quaternion.Slerp(fromRot, hr, e));
                yield return null;
            }
            WorkshopAudio.Play("rock_pickup", carried.position, 0.4f, 1.05f);
            if (_customer != null) _customer.TakeOwnership(_pieces.Count > 0 ? _pieces[0] : null, _bagRig.Bag);
            _bagRig.Release();   // the carrier belongs to the customer now; the station must not destroy it
            Flow.To(CheckoutState.CustomerLeaving, Time.time, "customer has their order");
            _screens.ShowTransaction(Tx, "complete", "", _ticketNumber);
            yield return new WaitForSeconds(Layout.BagCustomerHold);
            Flow.To(CheckoutState.TransactionComplete, Time.time, "done");
            ResetStation();
        }

        /// <summary>Put the station back to idle. Not named Reset: MonoBehaviour owns that message and the editor calls it on add.</summary>
        public void ResetStation()
        {
            _tx = null;
            _customer = null;
            _pieces.Clear();
            _targets.RemoveAll(t => t == null);
            _money.ClearTender();
            _money.ClearChange();
            if (_card != null) { Destroy(_card); _card = null; }
            _bagRig.Clear();
            _drawerTarget = 0f;
            StatusLine = "";
            Busy = false;
            _screens.ShowIdle();
            Exit();
        }

        private void RefreshScreens()
        {
            if (Tx == null) { _screens.ShowIdle(); return; }
            _screens.ShowTransaction(Tx, PosState(), StatusLine, _ticketNumber);
        }

        private string PosState()
        {
            switch (Flow.Current)
            {
                case CheckoutState.CustomerPlacingProducts:
                case CheckoutState.WaitingForCashier: return "products-ready";
                case CheckoutState.WaitingForScan:
                case CheckoutState.ProductHeld:
                case CheckoutState.ProductScanning:
                case CheckoutState.ProductScanned: return "scanning";
                case CheckoutState.AllProductsScanned: return "all-items-scanned";
                case CheckoutState.ChoosingPayment: return "select-payment";
                case CheckoutState.CardPresented:
                case CheckoutState.CardInsertReady:
                case CheckoutState.CardInserting:
                case CheckoutState.CardAmountEntry:
                case CheckoutState.CardProcessing:
                case CheckoutState.CardApproved: return "card-payment";
                case CheckoutState.CashPresented:
                case CheckoutState.CashAccepted:
                case CheckoutState.DrawerOpening:
                case CheckoutState.DepositingCash: return "cash-payment";
                case CheckoutState.SelectingChange:
                case CheckoutState.GivingChange: return "change-selection";
                case CheckoutState.PaymentComplete:
                case CheckoutState.ReceiptPrinting: return "payment-complete";
                case CheckoutState.Bagging:
                case CheckoutState.BagHandoff: return "bag-transfer";
                case CheckoutState.CustomerLeaving:
                case CheckoutState.TransactionComplete: return "complete";
                case CheckoutState.Recovery: return "recovery";
                default: return "waiting";
            }
        }

        // ------------------------------------------------------------------------------------------------------
        // harness: the same physical actions a player takes, chosen deterministically
        // ------------------------------------------------------------------------------------------------------
        /// <summary>
        /// The button press, whatever pressed it: mouse, [E] or the gamepad's south face. With something under the
        /// pointer or the cycle, that is what is acted on; with nothing, the obvious next thing is what the press means.
        /// </summary>
        public bool PressInteract()
        {
            if (Busy || Tx == null) return false;
            var target = Hovered ?? ObviousTarget();
            if (target == null) return false;
            Activate(target);
            return true;
        }

        /// <summary>Move the highlight to the next live pickable. This is the controller's pointer.</summary>
        public bool CycleTarget(int direction)
        {
            var live = new List<CheckoutTarget>();
            foreach (var t in _targets) if (Live(t)) live.Add(t);
            if (live.Count == 0) { SetHovered(null); _cycleIndex = -1; return false; }
            _cycleIndex = ((_cycleIndex < 0 ? 0 : _cycleIndex + direction) % live.Count + live.Count) % live.Count;
            SetHovered(live[_cycleIndex]);
            return true;
        }

        /// <summary>Take the next physical action the counter is waiting for. Returns false while an animation owns the step.</summary>
        public bool HarnessStep()
        {
            if (Busy || Tx == null) return false;
            switch (Tx.Stage)
            {
                case TxStage.Scanning:
                    foreach (var line in Tx.Items)
                        if (!line.Scanned) { StartCoroutine(RingUp(line.Uid)); return true; }
                    return false;
                case TxStage.CashTender:
                    StartCoroutine(TakeCash());
                    return true;
                case TxStage.CashDrawer:
                    if (!Tx.Deposited) return false;
                    int remaining = Money.Cents(Tx.ChangeDue) - Money.Cents(Tx.HandTotal);
                    if (remaining <= 0) { ConfirmChange(); return true; }
                    var plan = Money.MakeChangeFrom(Tx.DrawerContents(_drawer), Money.Dollars(remaining));
                    if (plan == null) { ConfirmChange(); return true; }
                    for (int i = 0; i < Money.Denoms.Length; i++)
                        if (plan[i] > 0) { TakeChangePiece(Money.Denoms[i]); return true; }
                    return false;
                case TxStage.CardReady:
                    StartCoroutine(InsertCard());
                    return true;
                case TxStage.CardEntry:
                    int want = Money.Cents(Tx.Total);
                    string digits = want.ToString();
                    if (Tx.CardEntryDigits.Length < digits.Length) { Keypad("digit:" + digits[Tx.CardEntryDigits.Length]); return true; }
                    Keypad("confirm");
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Harness: run the transaction to completion from wherever it stands.</summary>
        public IEnumerator CompleteFromHere(float pace)
        {
            float guard = 0f;
            while (Tx != null && Flow.Current != CheckoutState.TransactionComplete && guard < 90f)
            {
                HarnessStep();
                yield return new WaitForSeconds(pace);
                guard += pace;
            }
        }

        private void LateUpdate() => Update_CardHold();
    }
}
