using System.Collections;
using UnityEngine;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Retail
{
    /// <summary>
    /// The checkout station (V6 §47): the register on the counter runs a physical transaction step by step from a
    /// fixed counter camera. The customer stages the piece, the player rings it up (the screen shows the total), the
    /// customer presents cash or a card, the player takes the cash to the drawer and hands back change, or readies the
    /// reader and the card goes into the slot; the sale commits; the player packs the piece (bag, box, or two hands)
    /// and hands it across the counter into the customer's hand; the customer leaves with the same object; the
    /// station resets. One press per physical step, never a menu.
    /// </summary>
    public sealed class CheckoutRegister : InteractableBehaviour
    {
        public enum Step { Idle, RungUp, CashPresented, CashInHand, CashDeposited, ChangeInHand, CardPresented, CardInserted, Processing, Approved, Paid, Packed, HandingOver, Done }

        public RetailShop Shop;
        public Transform Drawer;                 // slides out along its local -Z
        public MeshRenderer Screen;              // the register body; slot 1 is the screen
        public MeshRenderer ReaderScreen;        // the card reader body; slot 1 is its display
        public Transform CameraAnchor, TenderPoint, HandoffMid, ReaderSlot, DrawerNotes, DrawerChange, PlayerHand;
        public Transform RegisterLabelPoint, ReaderLabelPoint;
        public GameObject CardPrefab, NotesPrefab, BagPrefab, BoxPrefab;
        public Material[] CardMaterials, NotesMaterials, BagMaterials, BoxMaterials;
        public Font LabelFont;
        public Material LabelMaterial;
        public float CameraFov = 58f;          // the counter view is wider than a bench station: customer, screen, drawer and counter in one frame

        public Step Stage { get; private set; }
        public bool Active { get; private set; }
        public bool Busy { get; private set; }
        public bool RungUp => Stage != Step.Idle;
        public Customer Current => Shop != null ? Shop.AtCounter : null;
        public Customer Transacting => _customer;
        public float Price { get; private set; }
        public float Tendered { get; private set; }
        public float Change { get; private set; }
        public Customer.Payment Method { get; private set; }
        /// <summary>The next physical step, as the prompt shows it.</summary>
        public string StatusLine { get; private set; } = "";
        public string PackageWord { get; private set; } = "";

        private Customer _customer;
        private SpecimenEntity _piece;
        private GameObject _notes, _change, _card, _package;
        private FirstPersonController _controller;
        private PlayerInteractor _player;
        private UI.WorldLabel _registerLabel, _readerLabel;
        private float _drawerTarget, _drawerOpen, _drawerVel, _screenGlow, _readerGlow;
        private Vector3 _drawerHome;
        private MaterialPropertyBlock _mpb;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        protected override void Awake()
        {
            base.Awake();
            if (Drawer != null) _drawerHome = Drawer.localPosition;
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            _controller = FindAnyObjectByType<FirstPersonController>();
            _player = FindAnyObjectByType<PlayerInteractor>();
            if (LabelFont == null && Shop != null) { LabelFont = Shop.LabelFont; LabelMaterial = Shop.LabelMaterial; }
            if (LabelFont != null)
            {
                if (RegisterLabelPoint != null) { _registerLabel = UI.WorldLabel.Create(RegisterLabelPoint, LabelFont, LabelMaterial, 0.026f, new Color(0.02f, 0.07f, 0.04f), "RegisterText"); _registerLabel.Text = "READY"; }
                if (ReaderLabelPoint != null) { _readerLabel = UI.WorldLabel.Create(ReaderLabelPoint, LabelFont, LabelMaterial, 0.012f, new Color(0.92f, 0.97f, 1f), "ReaderText"); _readerLabel.Text = "READY"; }
            }
        }

        public override bool CanInteract(PlayerInteractor player)
        {
            if (Shop == null || player.Held != null) return false;
            if (Stage != Step.Idle) return true;
            return Shop.AtCounter != null && Shop.AtCounter.Wanted != null;
        }

        public override string GetPrompt(PlayerInteractor player)
        {
            if (Stage == Step.Idle)
            {
                var c = Current;
                if (c == null || c.Wanted == null) return "";
                return $"Ring up {c.Wanted.Record.DisplayName}  {UI.UiKit.Money(c.Wanted.Record.AskingPrice)}";
            }
            if (Active) return "";                                   // the HUD strip carries the step while the counter view is up
            return string.IsNullOrEmpty(StatusLine) ? "Checkout" : StatusLine;
        }

        public override string GetHint(PlayerInteractor player) => Stage == Step.Idle ? "The register reads the tag; the sale then runs step by step from behind the counter" : null;

        public override void Interact(PlayerInteractor player)
        {
            if (!Active) Enter();
            if (Stage == Step.Idle) RingUp();
            else Advance();
        }

        private void Enter()
        {
            if (Active) return;
            Active = true;
            if (_controller != null && CameraAnchor != null) _controller.EnterStationView(CameraAnchor, CameraFov);
            if (_player != null) { _player.InputLocked = true; _player.IgnoreInteractUntilFrame = Time.frameCount + 1; }
        }

        /// <summary>Leave the counter camera; a transaction in progress keeps its state and resumes on the next press.</summary>
        public void Exit()
        {
            if (!Active) return;
            Active = false;
            if (_controller != null) _controller.ExitStationView();
            if (_player != null) _player.InputLocked = false;
        }

        // ------------------------------------------------------------------------------------------------------
        // the sequence
        // ------------------------------------------------------------------------------------------------------
        private void RingUp()
        {
            var c = Current;
            if (c == null || c.Wanted == null) return;
            _customer = c;
            _piece = c.Wanted;
            Price = _piece.Record.AskingPrice > 0f ? _piece.Record.AskingPrice : RetailShop.AskingPrice(_piece.Record);
            Method = c.Method;
            Stage = Step.RungUp;
            _screenGlow = 1f;
            SetRegister($"TOTAL {UI.UiKit.Money(Price)}");
            StatusLine = "The customer is paying...";
            WorkshopAudio.Play("register_beep", transform.position, 0.6f);
            StartCoroutine(CustomerPresents());
        }

        private IEnumerator CustomerPresents()
        {
            Busy = true;
            yield return new WaitForSeconds(0.55f);
            if (_customer == null) { Busy = false; Reset(); yield break; }
            if (Method == Customer.Payment.Cash)
            {
                Tendered = TenderFor(Price);
                Change = Tendered - Price;
                _notes = Spawn(NotesPrefab, NotesMaterials, _customer.HandPoint, "Cash");
                _customer.Reach(true);
                yield return new WaitForSeconds(0.35f);
                yield return Move(_notes.transform, TenderPoint, 0.7f, 0.08f);
                _customer.Reach(false);
                Stage = Step.CashPresented;
                SetRegister($"TOTAL {UI.UiKit.Money(Price)}\nCASH {UI.UiKit.Money(Tendered)}");
                StatusLine = $"Take the {UI.UiKit.Money(Tendered)}";
                _customer.SayLine(Change > 0f ? $"Here's {UI.UiKit.Money(Tendered)}." : "Exact, I think.");
            }
            else
            {
                _card = Spawn(CardPrefab, CardMaterials, _customer.HandPoint, "Card");
                _card.transform.localRotation = Quaternion.Euler(-60f, 0f, 0f);
                _card.transform.localPosition = new Vector3(0.02f, 0.0f, 0.04f);
                _customer.Reach(true);
                Stage = Step.CardPresented;
                SetRegister($"TOTAL {UI.UiKit.Money(Price)}\nCARD");
                SetReader($"{UI.UiKit.Money(Price)}\nREADY");
                StatusLine = "Ready the card reader";
                _customer.SayLine("Card, please.");
            }
            Busy = false;
        }

        /// <summary>The next physical step (the Interact press while the station is active).</summary>
        public void Advance()
        {
            if (Busy) return;
            switch (Stage)
            {
                case Step.CashPresented: StartCoroutine(TakeCash()); break;
                case Step.CashInHand: StartCoroutine(DepositCash()); break;
                case Step.CashDeposited: StartCoroutine(TakeChange()); break;
                case Step.ChangeInHand: StartCoroutine(HandChange()); break;
                case Step.CardPresented: StartCoroutine(CardFlow()); break;
                case Step.Paid: StartCoroutine(Pack()); break;
                case Step.Packed: StartCoroutine(HandOver()); break;
            }
        }

        private IEnumerator TakeCash()
        {
            Busy = true;
            yield return Move(_notes.transform, PlayerHand, 0.35f, 0.05f);
            Stage = Step.CashInHand;
            _drawerTarget = 1f;
            WorkshopAudio.Play("register", transform.position, 0.5f);
            StatusLine = $"Put the {UI.UiKit.Money(Tendered)} in the drawer";
            Busy = false;
        }

        private IEnumerator DepositCash()
        {
            Busy = true;
            yield return Move(_notes.transform, DrawerNotes, 0.4f, 0.06f);
            _notes.transform.localRotation = Quaternion.identity;
            Stage = Step.CashDeposited;
            WorkshopAudio.Play("rock_place", Drawer != null ? Drawer.position : transform.position, 0.25f, 1.4f);
            if (Change > 0.01f)
            {
                _change = Spawn(NotesPrefab, NotesMaterials, DrawerChange, "Change");
                _change.transform.localScale = new Vector3(0.9f, 0.9f, 0.6f);
                SetRegister($"CASH {UI.UiKit.Money(Tendered)}\nCHANGE {UI.UiKit.Money(Change)}");
                StatusLine = $"Take {UI.UiKit.Money(Change)} change";
                Busy = false;
            }
            else
            {
                Busy = false;
                CommitPayment();
            }
        }

        private IEnumerator TakeChange()
        {
            Busy = true;
            yield return Move(_change.transform, PlayerHand, 0.35f, 0.05f);
            Stage = Step.ChangeInHand;
            StatusLine = $"Hand {UI.UiKit.Money(Change)} to the {_customer.Archetype.Name.ToLower()}";
            Busy = false;
        }

        private IEnumerator HandChange()
        {
            Busy = true;
            _customer.Receive(true);
            yield return new WaitForSeconds(0.3f);
            yield return Move(_change.transform, _customer.HandPoint, 0.6f, 0.08f);
            yield return new WaitForSeconds(0.5f);
            _customer.Receive(false);
            Destroy(_change); _change = null;      // pocketed
            Busy = false;
            CommitPayment();
        }

        private IEnumerator CardFlow()
        {
            Busy = true;
            SetReader($"{UI.UiKit.Money(Price)}\nINSERT CARD");
            _readerGlow = 1f;
            WorkshopAudio.Play("register_beep", ReaderSlot != null ? ReaderSlot.position : transform.position, 0.4f, 1.3f);
            StatusLine = "Waiting for the card...";
            yield return new WaitForSeconds(0.4f);
            yield return Move(_card.transform, ReaderSlot, 0.55f, 0.03f);
            Stage = Step.CardInserted;
            _customer.Reach(false);
            SetReader("PROCESSING");
            Stage = Step.Processing;
            yield return new WaitForSeconds(1.3f);
            SetReader($"{UI.UiKit.Money(Price)}\nAPPROVED");
            Stage = Step.Approved;
            WorkshopAudio.Play("register_beep", ReaderSlot != null ? ReaderSlot.position : transform.position, 0.5f, 1.6f);
            yield return new WaitForSeconds(0.6f);
            _customer.Reach(true);
            yield return Move(_card.transform, _customer.HandPoint, 0.5f, 0.03f);
            yield return new WaitForSeconds(0.4f);
            _customer.Reach(false);
            Destroy(_card); _card = null;          // back in the wallet
            _readerGlow = 0.3f;
            Busy = false;
            CommitPayment();
        }

        private void CommitPayment()
        {
            if (_customer == null || _piece == null) { Reset(); return; }
            if (!Shop.CompleteSale(_customer, false)) { Reset(); return; }
            Stage = Step.Paid;
            _drawerTarget = 0f;
            SetRegister($"PAID {UI.UiKit.Money(Price)}");
            SetReader("READY");
            PackageWord = PackageFor(_piece.Geology.SizeClass);
            StatusLine = PackageWord == "piece" ? $"Lift the {_piece.Record.DisplayName} across" : $"Pack the {_piece.Record.DisplayName} in a {PackageWord}";
            WorkshopAudio.Play2D("register", 0.8f);
            WorkshopAudio.Play2D("crystal_chime", 0.3f, 1.25f);
        }

        private static string PackageFor(SizeClass size) => size == SizeClass.Small ? "bag" : size == SizeClass.Medium ? "box" : "piece";

        private IEnumerator Pack()
        {
            Busy = true;
            var size = _piece.Geology.SizeClass;
            // however it was displayed, the geode closes back up (lid on) to travel: one body in the package or in the hands
            _piece.SetPhysics(false);
            _piece.SetCollidersEnabled(false);
            _piece.Locked = true;
            _piece.ApplyStoredPose();
            var foot = _piece.FootprintFor(DisplayPose.Closed);   // specimen-local, closed
            if (size == SizeClass.Small || size == SizeClass.Medium)
            {
                bool bag = size == SizeClass.Small;
                var prefab = bag ? BagPrefab : BoxPrefab;
                float footY = _piece.transform.position.y - _piece.RestHeightOffset(_piece.IsOpened);
                _package = Spawn(prefab, bag ? BagMaterials : BoxMaterials, null, bag ? "Bag" : "Box");
                Quaternion packRot = Quaternion.LookRotation(-Vector3.Cross(Vector3.up, transform.right), Vector3.up);
                _package.transform.SetPositionAndRotation(new Vector3(_piece.transform.position.x, footY, _piece.transform.position.z), packRot);
                // the package is sized to the piece, never the other way round (§52): the piece's long side runs along the
                // package's wide side (package-local X), with a little room all round
                bool turn = foot.size.z > foot.size.x;
                float w = turn ? foot.size.z : foot.size.x, dpt = turn ? foot.size.x : foot.size.z, hgt = foot.size.y;
                float s = bag ? Mathf.Max(1f, w / 0.17f, dpt / 0.105f, hgt / 0.26f) : Mathf.Max(1f, w / 0.26f, dpt / 0.20f, hgt / 0.14f);
                _package.transform.localScale = Vector3.one * s;
                foreach (var col in _package.GetComponentsInChildren<Collider>()) col.enabled = false;
                // the piece goes into it: the same entity, parented inside, standing centred on the package floor
                Quaternion pieceRot = packRot * Quaternion.Euler(0f, turn ? 90f : 0f, 0f);
                Vector3 inside = _package.transform.position + Vector3.up * (0.012f * s + _piece.RestHeightOffset(_piece.IsOpened)) - pieceRot * new Vector3(foot.center.x, 0f, foot.center.z);
                yield return Lift(_piece.transform, _piece.transform.position + Vector3.up * 0.12f, inside, 0.55f, pieceRot);
                _piece.transform.SetParent(_package.transform, true);
            }
            Stage = Step.Packed;
            StatusLine = PackageWord == "piece" ? $"Hand the {_piece.Record.DisplayName} to the {_customer.Archetype.Name.ToLower()}" : $"Hand the {PackageWord} to the {_customer.Archetype.Name.ToLower()}";
            WorkshopAudio.Play("rock_place", _piece.transform.position, 0.4f, 1.2f);
            Busy = false;
        }

        private IEnumerator HandOver()
        {
            Busy = true;
            Stage = Step.HandingOver;
            StatusLine = "Handing it across...";
            _customer.Receive(true, _package, _piece);
            Transform carried = _package != null ? _package.transform : _piece.transform;
            // across the counter to its customer-side edge, outward and clear of it, then down into the receiving hand (§53)
            Vector3 p0 = carried.position;
            Vector3 edge = HandoffMid != null ? HandoffMid.position : p0;
            Vector3 outward = HandoffMid != null ? HandoffMid.forward : (edge - p0).normalized;
            float clear = 0.1f + (_package != null ? _package.transform.localScale.x * 0.08f : _piece.Radius);
            Vector3 p1 = edge + Vector3.up * 0.06f;
            Vector3 p2 = edge + outward * clear + Vector3.up * 0.04f;
            yield return Lift(carried, p0, p1, 0.5f);
            yield return Lift(carried, p1, p2, 0.35f);
            // the hands settle as the arms come up: aim at the hold pose where it is each frame, so ownership changes without a jump
            float t = 0f; Vector3 from = p2; Quaternion fromRot = carried.rotation;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.45f;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                _customer.HoldPose(out var hp, out var hr);
                carried.SetPositionAndRotation(Vector3.Lerp(from, hp, e), Quaternion.Slerp(fromRot, hr, e));
                yield return null;
            }
            _customer.TakeOwnership(_piece, _package);
            _package = null;
            Stage = Step.Done;
            SetRegister("PAID\nTHANK YOU");
            StatusLine = "";
            WorkshopAudio.Play("rock_pickup", carried.position, 0.4f, 1.05f);
            yield return new WaitForSeconds(0.9f);
            Busy = false;
            Reset();
            Exit();
        }

        /// <summary>Harness: run the transaction to completion from the current step at the given pace.</summary>
        public IEnumerator CompleteFromHere(float pace)
        {
            float guard = 0f;
            while (Stage != Step.Idle && guard < 60f)
            {
                if (!Busy) Advance();
                yield return new WaitForSeconds(pace);
                guard += pace;
            }
        }

        private void Reset()
        {
            Stage = Step.Idle;
            StatusLine = "";
            PackageWord = "";
            _customer = null; _piece = null;
            if (_notes != null) { Destroy(_notes); _notes = null; }
            if (_change != null) { Destroy(_change); _change = null; }
            if (_card != null) { Destroy(_card); _card = null; }
            if (_package != null) { Destroy(_package); _package = null; }
            _drawerTarget = 0f;
            _screenGlow = 0.25f;
            _readerGlow = 0.3f;
            SetRegister("READY");
            SetReader("READY");
            Busy = false;
        }

        // ------------------------------------------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------------------------------------------
        /// <summary>Notes tendered for a price: twenties on top, a ten or five to finish, never less than the price.</summary>
        public static float TenderFor(float price)
        {
            float p = Mathf.Ceil(price / 5f) * 5f;
            if (p <= 10f) return p;
            float twenties = Mathf.Floor(p / 20f) * 20f;
            float rest = p - twenties;
            if (rest <= 0.01f) return twenties;
            return rest <= 10f ? twenties + 10f : twenties + 20f;
        }

        private GameObject Spawn(GameObject prefab, Material[] mats, Transform parent, string name)
        {
            GameObject go;
            if (prefab != null) go = Instantiate(prefab);
            else { go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.transform.localScale = new Vector3(0.1f, 0.01f, 0.06f); Destroy(go.GetComponent<Collider>()); }
            go.name = name;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (mats == null || mats.Length == 0) continue;
                var arr = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < arr.Length; i++) arr[i] = mats[Mathf.Min(i, mats.Length - 1)];
                r.sharedMaterials = arr;
            }
            foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = false;
            if (parent != null) { go.transform.SetParent(parent, false); go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; }
            return go;
        }

        private IEnumerator Move(Transform t, Transform target, float dur, float arc)
        {
            if (t == null) yield break;
            t.SetParent(null, true);
            Vector3 from = t.position; Quaternion fromRot = t.rotation;
            float k = 0f;
            while (k < 1f)
            {
                k += Time.deltaTime / dur;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k));
                Vector3 to = target != null ? target.position : from;
                t.position = Vector3.Lerp(from, to, e) + Vector3.up * (Mathf.Sin(e * Mathf.PI) * arc);
                if (target != null) t.rotation = Quaternion.Slerp(fromRot, target.rotation, e);
                yield return null;
            }
            if (target != null) { t.SetParent(target, true); t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity; }
        }

        private static IEnumerator Lift(Transform t, Vector3 from, Vector3 to, float dur, Quaternion? toRot = null)
        {
            float k = 0f;
            Quaternion fromRot = t.rotation;
            while (k < 1f)
            {
                k += Time.deltaTime / dur;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k));
                t.position = Vector3.Lerp(from, to, e) + Vector3.up * (Mathf.Sin(e * Mathf.PI) * 0.03f);
                if (toRot.HasValue) t.rotation = Quaternion.Slerp(fromRot, toRot.Value, e);
                yield return null;
            }
            t.position = to;
            if (toRot.HasValue) t.rotation = toRot.Value;
        }

        private void SetRegister(string text) { if (_registerLabel != null) _registerLabel.Text = text; }
        private void SetReader(string text) { if (_readerLabel != null) _readerLabel.Text = text; }

        private void Update()
        {
            float dt = Time.deltaTime;
            // a customer who gave up mid-transaction (before the money) takes the station back to idle; so does a world
            // cleared under a running sale (new game or load while the register is mid-sequence)
            if (Stage != Step.Idle)
            {
                bool worldGone = _customer == null || _piece == null;
                bool gaveUp = !worldGone && Stage < Step.Paid && (_customer.State == Customer.Phase.Leaving || _customer.State == Customer.Phase.Done);
                if (worldGone || gaveUp)
                {
                    StopAllCoroutines();
                    if (worldGone) Exit();
                    Reset();
                }
            }
            if (Active)
            {
                if (GameInput.InteractPressed && !Busy && Stage != Step.Idle) Advance();
                if (GameInput.BackPressed) Exit();
            }
            // the drawer kicks out fast and eases back
            _drawerVel = Mathf.Lerp(_drawerVel, (_drawerTarget - _drawerOpen) * 18f, 1f - Mathf.Exp(-dt * 14f));
            _drawerOpen = Mathf.Clamp01(_drawerOpen + _drawerVel * dt);
            if (Drawer != null) Drawer.localPosition = _drawerHome + Vector3.back * (0.13f * _drawerOpen);
            // screens: lit while a sale runs, a soft glow otherwise
            float wantGlow = Stage != Step.Idle ? 1f : 0.25f;
            _screenGlow = Mathf.Lerp(_screenGlow, wantGlow, 1f - Mathf.Exp(-dt * 8f));
            if (Screen != null)
            {
                Screen.GetPropertyBlock(_mpb, 1);
                _mpb.SetColor(EmissionId, new Color(0.55f, 0.85f, 0.62f) * (0.18f + 0.5f * _screenGlow));
                Screen.SetPropertyBlock(_mpb, 1);
            }
            if (ReaderScreen != null)
            {
                ReaderScreen.GetPropertyBlock(_mpb, 1);
                _mpb.SetColor(EmissionId, new Color(0.25f, 0.45f, 0.8f) * (0.15f + 0.45f * _readerGlow));
                ReaderScreen.SetPropertyBlock(_mpb, 1);
            }
        }
    }
}
