using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Checkout;

namespace GeodeEmpire.Tests
{
    /// <summary>
    /// The checkout's domain rules, ported from Golf's headless suite. These run without a scene: the transaction and
    /// the flow know nothing about meshes, which is the whole point of keeping them separate from the station.
    /// </summary>
    public sealed class CheckoutMoneyTests
    {
        [TestCase(45.95f, "$45.95")]
        [TestCase(7.05f, "$7.05")]
        [TestCase(0.01f, "$0.01")]
        [TestCase(-0.01f, "-$0.01")]
        [TestCase(1234f, "$1,234.00")]
        [TestCase(0f, "$0.00")]
        [SetCulture("de-DE")]
        public void CheckoutAmountsKeepExactCentsRegardlessOfEditorLocale(float amount, string displayed)
        {
            Assert.AreEqual(displayed, Money.Format(amount));
        }

        [Test]
        public void CurrencyIsIntegerCents()
        {
            // 0.1 * 3 is 0.30000000000000004 in float; a till that balances on paper must balance in code
            var s = new MoneyStack();
            s.Add(0.1f, 3);
            Assert.AreEqual(30, s.TotalCents);
            Assert.AreEqual(0.30f, s.Total, 1e-6f);
        }

        [Test]
        public void MakeChangeIsGreedyOverTheCanonicalSet()
        {
            var s = Money.MakeChange(68.37f);
            Assert.AreEqual(1, s.CountOf(50f));
            Assert.AreEqual(0, s.CountOf(20f));
            Assert.AreEqual(1, s.CountOf(10f));
            Assert.AreEqual(1, s.CountOf(5f));
            Assert.AreEqual(3, s.CountOf(1f));
            Assert.AreEqual(0, s.CountOf(0.5f));
            Assert.AreEqual(1, s.CountOf(0.25f));
            Assert.AreEqual(1, s.CountOf(0.1f));
            Assert.AreEqual(0, s.CountOf(0.05f));
            Assert.AreEqual(2, s.CountOf(0.01f));
            Assert.AreEqual(6837, s.TotalCents);
        }

        [Test]
        public void BoundedChangeSolvesWhereGreedyWouldFail()
        {
            // no fives in the till: $5 has to come back as singles, which a greedy pick over the canonical set misses
            var drawer = new MoneyStack();
            drawer.Add(10f, 2);
            drawer.Add(1f, 6);
            var plan = Money.MakeChangeFrom(drawer, 5f);
            Assert.IsNotNull(plan, "the drawer can make $5 from singles");
            Assert.AreEqual(500, plan.TotalCents);
            Assert.AreEqual(0, plan.CountOf(5f));
            Assert.AreEqual(5, plan.CountOf(1f));
        }

        [Test]
        public void BoundedChangeReturnsNullWhenTheTillCannotMakeIt()
        {
            var drawer = new MoneyStack();
            drawer.Add(20f, 2);
            Assert.IsNull(Money.MakeChangeFrom(drawer, 5f), "two twenties cannot make five");
        }

        [Test]
        public void TakingMoreThanASlotHoldsFails()
        {
            var s = new MoneyStack();
            s.Add(1f, 2);
            Assert.IsTrue(s.Take(1f, 2));
            Assert.IsFalse(s.Take(1f), "the slot is empty");
        }

        [Test]
        public void OpeningFloatCanBreakAFifty()
        {
            var drawer = Money.NewDrawer();
            Assert.IsNotNull(Money.MakeChangeFrom(drawer, 45f), "a till that cannot break a fifty stalls the queue");
            Assert.AreEqual(37275, drawer.TotalCents);
        }

        [Test]
        public void CustomersPayWithLargeCoinsOrRoundUp()
        {
            // .95 endings: either notes plus quarters and dimes, or the next clean note. Never a fistful of pennies.
            for (int seed = 0; seed < 40; seed++)
            {
                var tender = Money.CustomerTender(33.95f, new System.Random(seed));
                Assert.GreaterOrEqual(tender.TotalCents, 3395, "a customer never hands over less than the price");
                Assert.AreEqual(0, tender.CountOf(0.01f), "nobody counts out pennies at a counter");
            }
        }

        [Test]
        public void PayableInLargeCoinsRejectsShrapnel()
        {
            Assert.IsTrue(Money.PayableInLargeCoins(95));   // quarters and dimes
            Assert.IsTrue(Money.PayableInLargeCoins(50));
            Assert.IsFalse(Money.PayableInLargeCoins(96));  // needs a penny
            Assert.IsFalse(Money.PayableInLargeCoins(3));
        }

        [Test]
        public void DrawerMigrationNeverInventsOrDestroysValue()
        {
            var legacy = new MoneyStack();
            legacy.Add(20f, 4);
            legacy.Add(1f, 30);
            legacy.Add(0.1f, 40);
            int before = legacy.TotalCents;
            var migrated = Money.MigrateDrawer(legacy);
            Assert.AreEqual(before, migrated.TotalCents);
        }
    }

    public sealed class CheckoutFlowTests
    {
        [Test]
        public void TheContractHoldsThirtyStates()
        {
            var problems = CheckoutFlowContract.Validate();
            Assert.IsEmpty(problems, string.Join(" | ", problems));
            Assert.AreEqual(30, CheckoutFlowContract.All.Count);
        }

        [Test]
        public void EveryNonTerminalStateExposesRecovery()
        {
            foreach (CheckoutState s in System.Enum.GetValues(typeof(CheckoutState)))
            {
                if (s == CheckoutState.TransactionComplete || s == CheckoutState.Recovery) continue;
                Assert.IsTrue(CheckoutFlowContract.CanTransition(s, CheckoutState.Recovery), $"{s} cannot recover");
            }
        }

        [Test]
        public void StatesThatWaitOnAHumanCarryNoWatchdog()
        {
            // a 4 s watchdog on the card offer killed sales mid-flight for any player who took a beat
            foreach (var s in new[] { CheckoutState.CardInsertReady, CheckoutState.CardAmountEntry, CheckoutState.SelectingChange, CheckoutState.WaitingForScan, CheckoutState.CardDeclined, CheckoutState.CashPresented })
                Assert.IsNull(CheckoutFlowContract.Spec(s).TimeoutSeconds, $"{s} waits on a human");
        }

        [Test]
        public void RecoveryResumesOnlyItsOwnCheckpoint()
        {
            var flow = new CheckoutFlow { Current = CheckoutState.WaitingForScan };
            flow.EnterRecovery(10f, "test", new CheckoutFacts { AllScanned = false });
            Assert.AreEqual(CheckoutState.Recovery, flow.Current);
            Assert.IsFalse(flow.Resume(11f, CheckoutState.PaymentComplete, "wrong target"), "recovery may not resume anywhere it likes");
            Assert.IsTrue(flow.Resume(11f, CheckoutState.WaitingForScan, "resume"));
        }

        [Test]
        public void RecoveryNeverInventsAnApproval()
        {
            var flow = new CheckoutFlow { Current = CheckoutState.CardProcessing };
            flow.EnterRecovery(5f, "lost the animation", new CheckoutFacts { PaymentAuthorized = false });
            Assert.AreEqual(CheckoutState.CardPresented, flow.RecoveryResume);
            var authorized = new CheckoutFlow { Current = CheckoutState.CardProcessing };
            authorized.EnterRecovery(5f, "lost the animation", new CheckoutFacts { PaymentAuthorized = true });
            Assert.AreEqual(CheckoutState.PaymentComplete, authorized.RecoveryResume);
        }

        [Test]
        public void AnAuthorizedCheckoutCannotBeAbandoned()
        {
            var flow = new CheckoutFlow { Current = CheckoutState.PaymentComplete };
            flow.EnterRecovery(5f, "stuck", new CheckoutFacts { PaymentAuthorized = true });
            Assert.IsFalse(flow.AbandonRecovery(6f, new CheckoutFacts { PaymentAuthorized = true }), "a paid sale must reconcile, not vanish");
            var unpaid = new CheckoutFlow { Current = CheckoutState.WaitingForScan };
            unpaid.EnterRecovery(5f, "stuck", new CheckoutFacts());
            Assert.IsTrue(unpaid.AbandonRecovery(6f, new CheckoutFacts()), "an unpaid one may let go");
        }

        [Test]
        public void NoStateTransitionsToItself()
        {
            foreach (CheckoutState s in System.Enum.GetValues(typeof(CheckoutState)))
                CollectionAssert.DoesNotContain(CheckoutFlowContract.Transitions(s), s, $"{s} loops on itself");
        }
    }

    public sealed class RegisterTransactionTests
    {
        private static RegisterTransaction Ticket(params float[] prices)
        {
            var lines = new List<TicketLine>();
            for (int i = 0; i < prices.Length; i++) lines.Add(new TicketLine { Uid = "S" + i, Name = "Piece " + i, Price = prices[i] });
            return RegisterTransaction.Create(lines, PaymentMethod.Cash, new System.Random(7), "A collector");
        }

        [Test]
        public void PaymentRefusesWhileAnythingIsUnrung()
        {
            var tx = Ticket(4.95f, 9.95f);
            Assert.IsFalse(tx.RequestPayment().Ok, "payment cannot start with goods unrung");
            tx.ScanItem("S0");
            Assert.IsFalse(tx.RequestPayment().Ok);
            tx.ScanItem("S1");
            Assert.IsTrue(tx.RequestPayment().Ok);
            Assert.AreEqual(TxStage.CashTender, tx.Stage);
        }

        [Test]
        public void AnItemRingsUpExactlyOnce()
        {
            var tx = Ticket(4.95f);
            Assert.IsTrue(tx.ScanItem("S0").Ok);
            Assert.IsFalse(tx.ScanItem("S0").Ok, "an item cannot be rung up twice");
            Assert.IsFalse(tx.ScanItem("nope").Ok, "an item that is not on the order cannot be rung up");
        }

        [Test]
        public void TheTerminalOpensAtZeroAndRefusesTheWrongAmount()
        {
            var tx = Ticket(12.95f);
            tx.ScanItem("S0");
            tx.Prefer = PaymentMethod.Card;
            tx.RequestPayment();
            tx.PresentCard();
            var res = tx.InsertCard();
            Assert.IsTrue(res.Ok);
            Assert.AreEqual(0, tx.CardEntryCents, "the reader opens empty: keying the amount is the interaction");
            foreach (char ch in "1200") tx.EnterCardDigit(ch - '0');
            Assert.IsFalse(tx.SubmitCardAmount().Ok, "12.00 is not the total");
            Assert.AreEqual("AMOUNT MUST MATCH TOTAL", tx.CardEntryError);
            tx.ClearCardAmount();
            foreach (char ch in "1295") tx.EnterCardDigit(ch - '0');
            Assert.IsTrue(tx.SubmitCardAmount().Ok);
            Assert.IsTrue(tx.RunCard().Ok);
            Assert.AreEqual("approved", tx.CardResult);
        }

        [Test]
        public void ACardRunCanBePulledOnlyBeforeItIsSubmitted()
        {
            var tx = Ticket(5.95f);
            tx.ScanItem("S0");
            tx.Prefer = PaymentMethod.Card;
            tx.RequestPayment();
            tx.PresentCard();
            Assert.IsTrue(tx.AbandonCardBeforeSubmit().Ok, "the cashier may pull the run at the reader");
            Assert.AreEqual(TxStage.Scanning, tx.Stage);
            Assert.IsTrue(tx.Find("S0").Scanned, "the basket stays intact");

            tx.RequestPayment();
            tx.PresentCard();
            tx.InsertCard();
            foreach (char ch in "595") tx.EnterCardDigit(ch - '0');
            tx.SubmitCardAmount();
            Assert.IsFalse(tx.AbandonCardBeforeSubmit().Ok, "never once authorization is in flight");
        }

        [Test]
        public void TheCustomerIsNeverUnderPaidAndTheCourtesyIsCapped()
        {
            var tx = Ticket(6.95f);
            tx.ScanItem("S0");
            tx.RequestPayment();
            tx.Tendered = Money.MakeChange(20f);
            tx.AcceptCash();
            var drawer = Money.NewDrawer();
            tx.OpenDrawer();
            tx.DepositTendered(drawer);
            Assert.AreEqual(13.05f, tx.ChangeDue, 0.001f);

            tx.TakeFromDrawer(drawer, 10f);
            Assert.IsFalse(tx.HandOverChange().Ok, "short change is refused");
            tx.TakeFromDrawer(drawer, 1f); tx.TakeFromDrawer(drawer, 1f); tx.TakeFromDrawer(drawer, 1f);
            tx.TakeFromDrawer(drawer, 0.05f);
            Assert.AreEqual(ChangeState.Exact, tx.ChangeGivingState(out int delta));
            Assert.AreEqual(0, delta);
            Assert.IsTrue(tx.HandOverChange().Ok);
            Assert.AreEqual(0f, tx.Lost, 0.001f);
        }

        [Test]
        public void OverpayingIsAllowedUpToFiveDollarsAndNoFurther()
        {
            var tx = Ticket(1.95f);
            tx.ScanItem("S0");
            tx.RequestPayment();
            tx.Tendered = Money.MakeChange(5f);
            tx.AcceptCash();
            var drawer = Money.NewDrawer();
            tx.OpenDrawer();
            tx.DepositTendered(drawer);
            for (int i = 0; i < 9; i++) tx.TakeFromDrawer(drawer, 1f);   // $9 against $3.05 due: $5.95 over
            Assert.AreEqual(ChangeState.Excess, tx.ChangeGivingState(out _));
            Assert.IsFalse(tx.HandOverChange().Ok, "more than the $5 courtesy is refused");
            for (int i = 0; i < 5; i++) tx.ReturnToDrawer(drawer, 1f);   // $4 against $3.05: 95c over, inside the courtesy
            Assert.AreEqual(ChangeState.Over, tx.ChangeGivingState(out _));
            Assert.IsTrue(tx.HandOverChange().Ok);
            Assert.AreEqual(0.95f, tx.Lost, 0.001f, "the courtesy books as a loss to the till");
        }

        [Test]
        public void TheSaleWillNotBankUnlessTheDrawerBalances()
        {
            var tx = Ticket(4.95f);
            tx.ScanItem("S0");
            tx.RequestPayment();
            tx.Tendered = Money.MakeChange(5f);
            tx.AcceptCash();
            var drawer = Money.NewDrawer();
            tx.OpenDrawer();
            tx.DepositTendered(drawer);
            tx.TakeFromDrawer(drawer, 0.05f);
            tx.HandOverChange();
            var commit = tx.CommitFor(drawer);
            Assert.IsTrue(commit.Ok, commit.Reason);
            Assert.AreEqual(4.95f, commit.Cash, 0.001f);

            // someone else moved the till between accepting and banking
            var meddled = drawer.Copy();
            meddled.Add(20f);
            Assert.IsFalse(tx.CommitFor(meddled).Ok, "the drawer changed before this sale could bank");
        }

        [Test]
        public void AVoidedSaleCostsNothing()
        {
            var tx = Ticket(9.95f);
            tx.ScanItem("S0");
            tx.RequestPayment();
            tx.Tendered = Money.MakeChange(10f);
            tx.AcceptCash();
            var drawer = Money.NewDrawer();
            int before = drawer.TotalCents;
            tx.OpenDrawer();
            tx.DepositTendered(drawer);
            tx.Void();
            Assert.AreEqual(TxStage.Voided, tx.Stage);
            Assert.AreEqual(before, drawer.TotalCents, "the persistent drawer is never touched until the sale banks");
        }

        [Test]
        public void CashRecoveryReplaysFromTheAcceptedCheckpoint()
        {
            var tx = Ticket(7.95f);
            tx.ScanItem("S0");
            tx.RequestPayment();
            tx.Tendered = Money.MakeChange(10f);
            tx.AcceptCash();
            var drawer = Money.NewDrawer();
            tx.OpenDrawer();
            tx.DepositTendered(drawer);
            tx.TakeFromDrawer(drawer, 1f);
            int persistentBefore = drawer.TotalCents;
            Assert.IsTrue(tx.RecoverCashAcceptedCheckpoint(drawer).Ok);
            Assert.AreEqual(TxStage.CashDrawer, tx.Stage);
            Assert.IsFalse(tx.Deposited, "the deposit replays");
            Assert.AreEqual(10f, tx.Tendered.Total, 0.001f, "the tender comes back exactly as accepted");
            Assert.IsTrue(tx.Hand.Empty);
            Assert.AreEqual(persistentBefore, drawer.TotalCents, "recovery copies the persistent drawer, never mutates it");
        }

        [Test]
        public void GoodsCannotBeHandedOverBeforeTheyArePacked()
        {
            var tx = Ticket(2.95f);
            tx.ScanItem("S0");
            tx.RequestPayment();
            tx.Tendered = Money.MakeChange(5f);
            tx.AcceptCash();
            var drawer = Money.NewDrawer();
            tx.OpenDrawer();
            tx.DepositTendered(drawer);
            tx.TakeFromDrawer(drawer, 1f); tx.TakeFromDrawer(drawer, 1f); tx.TakeFromDrawer(drawer, 0.05f);
            tx.HandOverChange();
            tx.CloseSale();
            Assert.IsFalse(tx.HandOverGoods().Ok, "nothing is packed yet");
            tx.BagItem("S0");
            Assert.IsTrue(tx.HandOverGoods().Ok);
            Assert.IsTrue(tx.CanComplete);
        }
    }

    public sealed class CheckoutPresentationTests
    {
        private static CheckoutPresentation.BagInterior Bag() => new CheckoutPresentation.BagInterior
        {
            HalfX = 0.125f, HalfMouth = 0.126f, HalfDepth = 0.07f, Centre = new Vector3(0f, 0.14f, 0f),
        };

        [Test]
        public void ABodyTooDeepToLieDownStandsOnItsLongestAxis()
        {
            var plan = CheckoutPresentation.BagFit(new Vector3(0.06f, 0.06f, 0.09f), Bag());
            Assert.IsTrue(plan.StandUp);
            Assert.AreEqual(2, plan.Axis, "the longest axis goes up the mouth");
        }

        [Test]
        public void AFittingBodyLiesDownAndStaysInsideTheWalls()
        {
            var interior = Bag();
            var plan = CheckoutPresentation.BagFit(new Vector3(0.05f, 0.05f, 0.05f), interior);
            Assert.IsFalse(plan.StandUp);
            var p = CheckoutPresentation.BagPlacement(plan, interior, 0);
            Assert.LessOrEqual(Mathf.Abs(p.x) + plan.Half.x, interior.HalfX + 1e-4f, "the clamp must not invert when the body is wide");
        }

        [Test]
        public void AnOversizeBodyIsNeverShovedThroughBothWalls()
        {
            // the fault this replaced: clamp(v, -(half - body), half - body) inverts once body > half
            var interior = Bag();
            var plan = CheckoutPresentation.BagFit(new Vector3(0.30f, 0.05f, 0.05f), interior);
            var p = CheckoutPresentation.BagPlacement(plan, interior, 0);
            Assert.IsTrue(plan.StandUp);
            Assert.AreEqual(0f, p.x, 1e-4f, "a standing body sits on the centre line");
        }

        [Test]
        public void MoneyPlacementIsDeterministic()
        {
            var meta = new DrawerWellContract { WellW = 0.0572f, WellD = 0.176f, MaxPieces = 12, Spacing = 0.0016f, HingeDrop = 0.039f };
            var a = DrawerMoneyLayout.BillLayout(meta, 7, 20f);
            var b = DrawerMoneyLayout.BillLayout(meta, 7, 20f);
            for (int i = 0; i < a.Length; i++) Assert.AreEqual(a[i].Offset, b[i].Offset, "the till must not reshuffle itself behind the player");
        }

        [Test]
        public void NotesStayInsideTheirWell()
        {
            var meta = new DrawerWellContract { WellW = 0.0572f, WellD = 0.176f, WallH = 0.044f, MaxPieces = 12, Spacing = 0.0016f, HingeDrop = 0.039f };
            foreach (var piece in DrawerMoneyLayout.BillLayout(meta, 12, 5f))
            {
                Assert.Less(Mathf.Abs(piece.Offset.x), meta.WellW * 0.5f);
                Assert.Less(Mathf.Abs(piece.Offset.z), meta.WellD * 0.5f);
                Assert.Less(piece.Offset.y, meta.WallH);
            }
        }

        [Test]
        public void CoinsStayInsideTheirWell()
        {
            var meta = new DrawerWellContract { WellW = 0.0572f, WellD = 0.136f, WallH = 0.028f, MaxPieces = 30, PileH = 0.0032f, Coin = true };
            float r = DrawerMoneyLayout.CoinDiameter(0.25f) * 0.5f;
            foreach (var piece in DrawerMoneyLayout.CoinLayout(meta, 30, r, 0.002f, 0.25f))
            {
                Assert.LessOrEqual(Mathf.Abs(piece.Offset.x) + r, meta.WellW * 0.5f + 1e-3f);
                Assert.LessOrEqual(Mathf.Abs(piece.Offset.z) + r, meta.WellD * 0.5f + 1e-3f);
            }
        }

        [Test]
        public void TheQuarterLivesInTheFourthCoinWell()
        {
            // the kit labelled its fourth well 20 because a sheet authored a 20-unit piece; the quarter is the real coin
            Assert.AreEqual("20", DrawerMoneyLayout.WellKey(0.25f));
            Assert.AreEqual("50", DrawerMoneyLayout.WellKey(0.5f));
            Assert.AreEqual("20", DrawerMoneyLayout.WellKey(20f).Replace("20", "20"));
            Assert.AreEqual("cash_coin_05_sheet01", DrawerMoneyLayout.AssetStem(0.05f, true));
            Assert.AreEqual("cash_coin_05", DrawerMoneyLayout.AssetStem(0.05f, false));
            Assert.AreEqual("cash_bill_20", DrawerMoneyLayout.AssetStem(20f, false));
        }
    }
}
