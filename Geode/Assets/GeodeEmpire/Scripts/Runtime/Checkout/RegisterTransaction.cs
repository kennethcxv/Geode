using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeodeEmpire.Checkout
{
    /// <summary>What is legally true about the money. Ported from Golf's tx.stage machine (src/sim/register.js).</summary>
    public enum TxStage { Scanning, Payment, CardPresent, CardReady, CardEntry, CardBusy, CardDeclined, CashTender, CashDrawer, Closing, Bagging, Done, Voided }

    public enum PaymentMethod { None, Cash, Card }

    public enum ChangeState { Short, Exact, Over, Excess }

    public struct TxResult
    {
        public bool Ok;
        public string Reason;
        public static TxResult Good => new TxResult { Ok = true };
        public static TxResult Fail(string reason) => new TxResult { Ok = false, Reason = reason };
    }

    /// <summary>One line on the ticket: the specimen itself, by identity, at the price it was on the shelf for.</summary>
    [Serializable]
    public sealed class TicketLine
    {
        public string Uid;            // SpecimenRecord.Id — the identity that walks out of the shop
        public string Name;
        public float Price;
        public bool Scanned;
        public bool Bagged;
    }

    /// <summary>
    /// The transaction a player works with their hands. This module is the whole truth of a sale and knows nothing
    /// about meshes: every rule the counter enforces (an item is rung up exactly once, payment cannot start with
    /// goods unrung, money moves only when payment succeeds) is a method here, so it can be hammered headlessly.
    ///
    /// Geode differences from the Golf original, deliberately: no sales tax (the career has none), no service lines,
    /// and no write-ahead settlement log — a Geode sale banks through GameSession/RetailShop, which already writes the
    /// career atomically (temp + backup) and marks the specimen Sold exactly once by identity.
    /// </summary>
    [Serializable]
    public sealed class RegisterTransaction
    {
        public List<TicketLine> Items = new List<TicketLine>();
        public TxStage Stage = TxStage.Scanning;
        public PaymentMethod Method = PaymentMethod.None;
        public PaymentMethod Prefer = PaymentMethod.None;

        // card
        public int CardEntryCents;
        public string CardEntryDigits = "";
        public string CardEntryError;
        public string CardResult;          // approved | declined | timeout | cancelled | null
        public int CardsTried;
        public int CardAttempts;

        // cash
        public MoneyStack Tendered = new MoneyStack();
        public MoneyStack AcceptedTender;
        public float TenderedTotal;
        public MoneyStack DrawerStart, DrawerPending;
        public MoneyStack Hand = new MoneyStack();
        public bool DrawerOpen, Deposited;
        public float ChangeGiven = -1f;
        public float Lost;

        public bool Banked;
        public string CustomerName = "A customer";

        [NonSerialized] public System.Random Rng = new System.Random(12345);

        /// <summary>The $5 courtesy is the only slack there is: the customer can never be under-paid, not by a cent.</summary>
        public const int MaxExtraChangeCents = 500;

        public static RegisterTransaction Create(IEnumerable<TicketLine> lines, PaymentMethod prefer, System.Random rng, string customerName)
        {
            var tx = new RegisterTransaction { Prefer = prefer, Rng = rng ?? new System.Random(1), CustomerName = customerName ?? "A customer" };
            foreach (var l in lines) tx.Items.Add(l);
            return tx;
        }

        // ---- ringing up ------------------------------------------------------------------------------------
        public TxResult ScanItem(string uid)
        {
            var item = Find(uid);
            if (item == null) return TxResult.Fail("That is not on this order.");
            if (item.Scanned) return TxResult.Fail("Already rung up.");
            item.Scanned = true;
            return TxResult.Good;
        }

        public TxResult BagScannedItem(string uid)
        {
            var item = Find(uid);
            if (item == null) return TxResult.Fail("That is not on this order.");
            if (!item.Scanned) return TxResult.Fail("Ring it up first.");
            item.Bagged = true;
            return TxResult.Good;
        }

        public TicketLine Find(string uid) => Items.Find(i => i.Uid == uid);
        public int UnscannedCount { get { int n = 0; foreach (var i in Items) if (!i.Scanned) n++; return n; } }
        public bool AllScanned => UnscannedCount == 0 && Items.Count > 0;
        public bool AllBagged { get { foreach (var i in Items) if (!i.Bagged) return false; return Items.Count > 0; } }

        public float Subtotal { get { float t = 0f; foreach (var i in Items) t += i.Price; return Money.Round(t); } }
        public float Total => Subtotal;
        public float CashTotal => Total;      // no rounding rule: Geode prices already land on the cent
        public float Due => Total;

        // ---- payment ---------------------------------------------------------------------------------------
        public TxResult RequestPayment()
        {
            if (Stage != TxStage.Scanning) return TxResult.Fail("Payment already started.");
            if (Items.Count == 0) return TxResult.Fail("Nothing to ring up.");
            if (UnscannedCount > 0) return TxResult.Fail($"Still {UnscannedCount} to ring up.");
            Method = Prefer != PaymentMethod.None ? Prefer : (Rng.NextDouble() < 0.5 ? PaymentMethod.Card : PaymentMethod.Cash);
            if (Method == PaymentMethod.Cash)
            {
                if (Money.Cents(CashTotal) == 0)
                {
                    Tendered = new MoneyStack(); AcceptedTender = new MoneyStack(); TenderedTotal = 0f;
                    Deposited = true; ChangeGiven = 0f; Lost = 0f; Stage = TxStage.Closing;
                }
                else Stage = TxStage.CashTender;
            }
            else Stage = TxStage.CardPresent;
            return TxResult.Good;
        }

        // ---- card ------------------------------------------------------------------------------------------
        public TxResult PresentCard()
        {
            if (Stage != TxStage.CardPresent) return TxResult.Fail("No card asked for.");
            Stage = TxStage.CardReady;
            CardsTried = Mathf.Max(1, CardsTried);
            return TxResult.Good;
        }

        /// <summary>
        /// THE TERMINAL STARTS AT 0.00. It used to prefill the exact total and leave the cashier to press Confirm,
        /// which made keying the amount — the one act that makes a card sale feel like operating a till — optional and
        /// usually skipped. Submit still refuses anything but the exact total.
        /// </summary>
        public TxResult InsertCard()
        {
            if (Stage != TxStage.CardReady)
                return TxResult.Fail(Stage == TxStage.CardDeclined ? "Use a different card." : "No card is ready to insert.");
            CardEntryCents = 0; CardEntryDigits = ""; CardEntryError = null;
            Stage = TxStage.CardEntry;
            return TxResult.Good;
        }

        public float CardEnteredAmount => Money.Dollars(Mathf.Max(0, CardEntryCents));

        public TxResult EnterCardDigit(int digit)
        {
            if (Stage != TxStage.CardEntry) return TxResult.Fail("The terminal is not accepting an amount.");
            if (digit < 0 || digit > 9) return TxResult.Fail("Enter one keypad digit.");
            string digits = CardEntryDigits + digit;
            if (digits.Length > 8) return TxResult.Fail("That amount is too large.");
            CardEntryDigits = digits;
            CardEntryCents = int.Parse(digits);
            CardEntryError = null;
            return TxResult.Good;
        }

        public TxResult BackspaceCardAmount()
        {
            if (Stage != TxStage.CardEntry) return TxResult.Fail("The terminal is not accepting an amount.");
            CardEntryDigits = CardEntryDigits.Length > 0 ? CardEntryDigits.Substring(0, CardEntryDigits.Length - 1) : "";
            CardEntryCents = CardEntryDigits.Length > 0 ? int.Parse(CardEntryDigits) : 0;
            CardEntryError = null;
            return TxResult.Good;
        }

        public TxResult ClearCardAmount()
        {
            if (Stage != TxStage.CardEntry) return TxResult.Fail("The terminal is not accepting an amount.");
            CardEntryCents = 0; CardEntryDigits = ""; CardEntryError = null;
            return TxResult.Good;
        }

        public TxResult SubmitCardAmount()
        {
            if (Stage != TxStage.CardEntry) return TxResult.Fail("The terminal is not accepting an amount.");
            int expected = Money.Cents(Total);
            bool empty = CardEntryDigits.Length == 0;
            if (empty || CardEntryCents != expected)
            {
                CardEntryError = empty ? "ENTER AMOUNT" : "AMOUNT MUST MATCH TOTAL";
                return TxResult.Fail(empty ? "Enter the transaction total." : "Entered amount must match the transaction total.");
            }
            CardEntryError = null;
            Stage = TxStage.CardBusy;
            return TxResult.Good;
        }

        /// <summary>Normal play approves deterministically after an exact entry; force values keep decline/timeout testable.</summary>
        public TxResult RunCard(string force = null, bool timeout = false)
        {
            if (Stage != TxStage.CardBusy)
                return TxResult.Fail(Stage == TxStage.CardDeclined ? "Declined - they need another card." : "No card presented.");
            CardAttempts++;
            if (timeout) { CardResult = "timeout"; Stage = TxStage.CardDeclined; return TxResult.Good; }
            if (force == "declined") { CardResult = "declined"; Stage = TxStage.CardDeclined; return TxResult.Good; }
            CardResult = "approved";
            Stage = TxStage.Closing;
            return TxResult.Good;
        }

        public TxResult RetryCard()
        {
            if (Stage != TxStage.CardDeclined) return TxResult.Fail("Nothing to retry.");
            CardsTried++; CardEntryCents = 0; CardEntryDigits = ""; CardEntryError = null;
            Stage = TxStage.CardReady;
            return TxResult.Good;
        }

        public TxResult CancelCard()
        {
            if (Stage != TxStage.CardPresent && Stage != TxStage.CardReady && Stage != TxStage.CardEntry
                && Stage != TxStage.CardBusy && Stage != TxStage.CardDeclined) return TxResult.Fail("No card payment running.");
            CardResult = "cancelled"; CardEntryCents = 0; CardEntryDigits = ""; CardEntryError = null;
            Method = PaymentMethod.None;
            Stage = TxStage.Payment;
            return TxResult.Good;
        }

        /// <summary>
        /// The cashier pulls the run at the reader's X before the amount is submitted: the basket stays intact and every
        /// item stays rung up. Never legal once authorization is in flight or resolved, so it cannot double-settle.
        /// </summary>
        public TxResult AbandonCardBeforeSubmit()
        {
            if (Method != PaymentMethod.Card) return TxResult.Fail("No card run to pull.");
            if (Stage != TxStage.CardPresent && Stage != TxStage.CardReady && Stage != TxStage.CardEntry)
                return TxResult.Fail("The card can only be pulled before the amount is submitted.");
            CardResult = null; CardEntryCents = 0; CardEntryDigits = ""; CardEntryError = null;
            Method = PaymentMethod.None;
            Stage = TxStage.Scanning;
            return TxResult.Good;
        }

        /// <summary>A lost in-flight animation rolls back to the presentation without inventing an approval or a decline.</summary>
        public TxResult RecoverUnresolvedCardAuthorization()
        {
            if (CardResult == "approved") return TxResult.Fail("That authorization already resolved.");
            if (Stage != TxStage.CardBusy) return TxResult.Fail("No card run is in flight.");
            Stage = TxStage.CardPresent;
            return TxResult.Good;
        }

        public TxResult PayCashInstead()
        {
            if (Stage != TxStage.Payment) return TxResult.Fail("Not at the payment choice.");
            Method = PaymentMethod.Cash;
            Stage = TxStage.CashTender;
            return TxResult.Good;
        }

        // ---- cash ------------------------------------------------------------------------------------------
        public MoneyStack CustomerCash()
        {
            Tendered = Money.CustomerTender(CashTotal, Rng);
            return Tendered;
        }

        public TxResult AcceptCash()
        {
            if (Stage != TxStage.CashTender) return TxResult.Fail("No cash offered.");
            if (Tendered == null || Tendered.Empty) return TxResult.Fail("They have not counted it out yet.");
            AcceptedTender = Tendered.Copy();
            TenderedTotal = Tendered.Total;   // remembered as a NUMBER: the pieces are about to be dismantled into the till
            Stage = TxStage.CashDrawer;
            return TxResult.Good;
        }

        /// <summary>Restore the last safe cash checkpoint from the transaction-local journal. The persistent drawer is copied, never mutated.</summary>
        public TxResult RecoverCashAcceptedCheckpoint(MoneyStack persistentDrawer)
        {
            if (Method != PaymentMethod.Cash || Banked) return TxResult.Fail("That cash transaction is past the recoverable checkpoint.");
            if (AcceptedTender == null || AcceptedTender.Empty) return TxResult.Fail("The accepted tender checkpoint is missing.");
            var baseline = DrawerStart != null ? DrawerStart.Copy() : (persistentDrawer != null ? persistentDrawer.Copy() : Money.NewDrawer());
            Tendered = AcceptedTender.Copy();
            DrawerStart = baseline.Copy();
            DrawerPending = baseline.Copy();
            DrawerOpen = false; Deposited = false;
            Hand = new MoneyStack();
            ChangeGiven = -1f; Lost = 0f;
            Stage = TxStage.CashDrawer;
            return TxResult.Good;
        }

        private MoneyStack LocalDrawer(MoneyStack persistent)
        {
            if (DrawerStart == null)
            {
                DrawerStart = persistent != null ? persistent.Copy() : Money.NewDrawer();
                DrawerPending = DrawerStart.Copy();
            }
            return DrawerPending;
        }

        public MoneyStack DrawerContents(MoneyStack persistent) => DrawerPending ?? (persistent != null ? persistent.Copy() : Money.NewDrawer());

        public TxResult OpenDrawer()
        {
            if (Stage != TxStage.CashDrawer) return TxResult.Fail("The drawer stays shut.");
            DrawerOpen = true;
            return TxResult.Good;
        }

        public TxResult CloseDrawer() { DrawerOpen = false; return TxResult.Good; }

        public TxResult DepositPiece(MoneyStack persistent, float denom)
        {
            if (!DrawerOpen) return TxResult.Fail("Open the drawer first.");
            if (!Tendered.Take(denom)) return TxResult.Fail("They did not give you one of those.");
            LocalDrawer(persistent).Add(denom);
            Deposited = Tendered.Empty;
            return TxResult.Good;
        }

        public TxResult DepositTendered(MoneyStack persistent)
        {
            if (!DrawerOpen) return TxResult.Fail("Open the drawer first.");
            if (Deposited) return TxResult.Fail("Already put away.");
            if (Tendered == null || Tendered.Empty) return TxResult.Fail("Nothing to put away.");
            for (int i = 0; i < Money.Denoms.Length; i++)
                for (int n = Tendered[i]; n > 0; n--) DepositPiece(persistent, Money.Denoms[i]);
            return TxResult.Good;
        }

        public TxResult TakeFromDrawer(MoneyStack persistent, float denom)
        {
            if (!DrawerOpen) return TxResult.Fail("Open the drawer first.");
            var working = LocalDrawer(persistent);
            if (!working.Take(denom)) return TxResult.Fail("None left in that slot.");
            Hand.Add(denom);
            return TxResult.Good;
        }

        public TxResult ReturnToDrawer(MoneyStack persistent, float denom)
        {
            if (!Hand.Take(denom)) return TxResult.Fail("Not holding one of those.");
            LocalDrawer(persistent).Add(denom);
            return TxResult.Good;
        }

        public float HandTotal => Hand.Total;
        public float ChangeDue => Method != PaymentMethod.Cash ? 0f : Money.Round(Mathf.Max(0f, (TenderedTotal > 0f ? TenderedTotal : Tendered.Total) - CashTotal));

        public ChangeState ChangeGivingState(out int deltaCents)
        {
            int required = Money.Cents(ChangeDue);
            int giving = Money.Cents(HandTotal);
            deltaCents = giving - required;
            if (deltaCents < 0) return Checkout.ChangeState.Short;
            if (deltaCents == 0) return Checkout.ChangeState.Exact;
            return deltaCents <= MaxExtraChangeCents ? Checkout.ChangeState.Over : Checkout.ChangeState.Excess;
        }

        public TxResult HandOverChange()
        {
            if (Stage != TxStage.CashDrawer) return TxResult.Fail("No change to give.");
            if (!Deposited) return TxResult.Fail("Put their money in the till first.");
            ChangeGivingState(out int delta);
            if (delta < 0) return TxResult.Fail("Not enough - count it again.");
            if (delta > MaxExtraChangeCents) return TxResult.Fail("Too much - count it again.");
            Lost = Money.Dollars(delta);
            ChangeGiven = Money.Round(HandTotal);
            Hand = new MoneyStack();
            DrawerOpen = false;
            Stage = TxStage.Closing;
            return TxResult.Good;
        }

        // ---- closing and packing ---------------------------------------------------------------------------
        public TxResult CloseSale()
        {
            if (Stage != TxStage.Closing) return TxResult.Fail("The payment is not finished.");
            Stage = TxStage.Bagging;
            return TxResult.Good;
        }

        public TxResult BagItem(string uid)
        {
            if (Stage != TxStage.Bagging) return TxResult.Fail("Not packing yet.");
            var item = Find(uid);
            if (item == null) return TxResult.Fail("That is not on this order.");
            item.Bagged = true;
            return TxResult.Good;
        }

        public TxResult HandOverGoods()
        {
            if (Stage != TxStage.Bagging) return TxResult.Fail("Nothing to hand over.");
            if (!AllBagged) return TxResult.Fail("Pack everything first.");
            Stage = TxStage.Done;
            return TxResult.Good;
        }

        public bool CanComplete => Stage == TxStage.Done && AllBagged;

        public void Void()
        {
            Stage = TxStage.Voided;
            DrawerStart = null; DrawerPending = null;   // the local stacks are discarded: a voided sale costs nothing
            Hand = new MoneyStack();
            DrawerOpen = false;
        }

        /// <summary>
        /// What the persistent drawer must become for this sale to bank, and the guard that refuses when the
        /// arithmetic disagrees. Ported from Golf's drawerCommitFor: if the till does not balance, the sale does not
        /// bank. Full stop.
        /// </summary>
        public struct DrawerCommit { public bool Ok; public string Reason; public float Cash; public MoneyStack Contents, Before; public bool AlreadyCommitted; }

        public DrawerCommit CommitFor(MoneyStack persistent)
        {
            if (Method != PaymentMethod.Card && Method != PaymentMethod.Cash)
                return new DrawerCommit { Ok = false, Reason = "No payment method." };
            if (Method == PaymentMethod.Card) return new DrawerCommit { Ok = true, Cash = Due };
            if (!Deposited || DrawerStart == null || DrawerPending == null)
                return new DrawerCommit { Ok = false, Reason = "The cash has not been secured in the drawer." };
            if (!Hand.Empty || DrawerOpen)
                return new DrawerCommit { Ok = false, Reason = "Finish the change and close the drawer first." };
            var current = persistent ?? DrawerStart;
            bool already = current.Same(DrawerPending);
            if (!current.Same(DrawerStart) && !already)
                return new DrawerCommit { Ok = false, Reason = "The drawer changed before this sale could bank." };
            float cash = Money.Round(DrawerPending.Total - DrawerStart.Total);
            float expected = Money.Round(Due - Lost);
            if (Money.Cents(cash) != Money.Cents(expected))
                return new DrawerCommit { Ok = false, Reason = "The drawer does not balance with this transaction." };
            return new DrawerCommit { Ok = true, Cash = cash, Contents = DrawerPending.Copy(), Before = DrawerStart.Copy(), AlreadyCommitted = already };
        }
    }
}
