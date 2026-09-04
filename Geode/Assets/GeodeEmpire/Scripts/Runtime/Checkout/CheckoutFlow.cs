using System;
using System.Collections.Generic;
using System.Linq;

namespace GeodeEmpire.Checkout
{
    /// <summary>The 30 player-visible checkout states, in order. Ported from Golf's src/sim/registerFlow.js.</summary>
    public enum CheckoutState
    {
        CustomerApproaching, CustomerPlacingProducts, WaitingForCashier, EnteringCashierMode, WaitingForScan,
        ProductHeld, ProductScanning, ProductScanned, AllProductsScanned, ChoosingPayment,
        CardPresented, CardInsertReady, CardInserting, CardAmountEntry, CardProcessing, CardApproved, CardDeclined,
        CashPresented, CashAccepted, DrawerOpening, DepositingCash, SelectingChange, GivingChange,
        PaymentComplete, ReceiptPrinting, Bagging, BagHandoff, CustomerLeaving, TransactionComplete, Recovery,
    }

    /// <summary>Which safe checkpoint a recovery may resume from; the resolver picks the target, never the caller.</summary>
    public enum RecoveryResolver { Fixed, ScanProgress, CardAuthorization, PaymentCheckpoint, BagHandoffCheckpoint, SaleBankCheckpoint, StoredTarget }

    public sealed class CheckoutStateSpec
    {
        public CheckoutState Id;
        public string Phase, Branch, CameraPose, PosState, Prompt, TimeoutReason, RecoveryCheckpoint;
        public string[] Audio = Array.Empty<string>();
        public float? TimeoutSeconds;                     // null: this state waits on a human and must not be policed
        public CheckoutState? RecoveryResume;
        public RecoveryResolver Resolver = RecoveryResolver.Fixed;
        public CheckoutState[] NextStates = Array.Empty<CheckoutState>();
    }

    /// <summary>
    /// The physical flow contract: what the player sees, which camera holds it, what may be pressed, how long the
    /// state may last and where a failure resumes. It owns no money and no meshes — the transaction owns the money
    /// (RegisterTransaction), and the two are deliberately separate. Every time they were coupled in Golf, a renderer
    /// bug became a money bug.
    /// </summary>
    public static class CheckoutFlowContract
    {
        private static readonly CheckoutState[] RecoveryResumeStates =
        {
            CheckoutState.CustomerApproaching, CheckoutState.CustomerPlacingProducts, CheckoutState.WaitingForCashier,
            CheckoutState.WaitingForScan, CheckoutState.AllProductsScanned, CheckoutState.ChoosingPayment,
            CheckoutState.CardPresented, CheckoutState.CardInsertReady, CheckoutState.CardAmountEntry,
            CheckoutState.CashPresented, CheckoutState.CashAccepted, CheckoutState.PaymentComplete,
            CheckoutState.ReceiptPrinting, CheckoutState.Bagging, CheckoutState.CustomerLeaving, CheckoutState.TransactionComplete,
        };

        private static readonly Dictionary<CheckoutState, CheckoutStateSpec> Specs = Build();

        public static IReadOnlyDictionary<CheckoutState, CheckoutStateSpec> All => Specs;
        public static CheckoutStateSpec Spec(CheckoutState s) => Specs[s];

        private static Dictionary<CheckoutState, CheckoutStateSpec> Build()
        {
            var d = new Dictionary<CheckoutState, CheckoutStateSpec>();
            void S(CheckoutState id, string phase, string branch, string camera, string pos, string prompt,
                   float? timeout, string timeoutReason, CheckoutState? resume, string checkpoint, string[] audio,
                   CheckoutState[] next, RecoveryResolver resolver = RecoveryResolver.Fixed)
                => d[id] = new CheckoutStateSpec
                {
                    Id = id, Phase = phase, Branch = branch, CameraPose = camera, PosState = pos, Prompt = prompt,
                    TimeoutSeconds = timeout, TimeoutReason = timeoutReason, RecoveryResume = resume,
                    RecoveryCheckpoint = checkpoint, Audio = audio, NextStates = next, Resolver = resolver,
                };
            var N = Array.Empty<string>();

            S(CheckoutState.CustomerApproaching, "customer-arrival", "shared", "world-first-person", "waiting", "A customer is approaching the register.",
              45f, "Customer navigation did not reach the register.", CheckoutState.CustomerApproaching, "reserved-order",
              new[] { "customer_step" }, new[] { CheckoutState.CustomerPlacingProducts });
            S(CheckoutState.CustomerPlacingProducts, "customer-arrival", "shared", "checkout-wide", "products-ready", "Customer is placing products.",
              30f, "Sequential product placement stopped progressing.", CheckoutState.CustomerPlacingProducts, "placed-product-uids",
              new[] { "rock_place" }, new[] { CheckoutState.WaitingForCashier });
            S(CheckoutState.WaitingForCashier, "customer-arrival", "shared", "world-first-person", "products-ready", "Work the register.",
              180f, "Customer waited too long without cashier interaction.", CheckoutState.WaitingForCashier, "staged-order",
              new[] { "counter_bell" }, new[] { CheckoutState.EnteringCashierMode });
            S(CheckoutState.EnteringCashierMode, "cashier-entry", "shared", "cashier-wide", "products-ready", "Preparing register...",
              4f, "Cashier camera or input capture failed to settle.", CheckoutState.WaitingForCashier, "staged-order",
              new[] { "station_enter" }, new[] { CheckoutState.WaitingForScan });
            S(CheckoutState.WaitingForScan, "scanning", "shared", "cashier-wide", "scanning", "Ring up each piece and bag it.",
              null, "The player may pause or step away safely between product scans.", CheckoutState.WaitingForScan, "scan-progress",
              N, new[] { CheckoutState.ProductHeld }, RecoveryResolver.ScanProgress);
            S(CheckoutState.ProductHeld, "scanning", "shared", "scanner-support", "scanning", "Ringing up item...",
              4f, "A clicked product did not begin moving to the bag.", CheckoutState.WaitingForScan, "scan-progress",
              new[] { "rock_pickup" }, new[] { CheckoutState.WaitingForScan, CheckoutState.ProductScanning });
            S(CheckoutState.ProductScanning, "scanning", "shared", "cashier-wide", "scanning", "Item added to the bag.",
              4f, "The product did not reach the bag.", CheckoutState.WaitingForScan, "scan-progress",
              new[] { "register_beep" }, new[] { CheckoutState.ProductHeld, CheckoutState.ProductScanned });
            S(CheckoutState.ProductScanned, "scanning", "shared", "cashier-wide", "scanning", "Item added to the order and bag.",
              4f, "A bagged product did not settle.", CheckoutState.WaitingForScan, "scan-progress",
              new[] { "bag_rustle" }, new[] { CheckoutState.WaitingForScan, CheckoutState.AllProductsScanned }, RecoveryResolver.ScanProgress);
            S(CheckoutState.AllProductsScanned, "scanning", "shared", "cashier-pos", "all-items-scanned", "All items bagged. Preparing payment...",
              5f, "Automatic payment selection did not begin.", CheckoutState.AllProductsScanned, "all-items-scanned",
              new[] { "ui_click" }, new[] { CheckoutState.ChoosingPayment });
            S(CheckoutState.ChoosingPayment, "payment", "shared", "cashier-wide", "select-payment", "Waiting for payment method.",
              20f, "Customer payment presentation did not start.", CheckoutState.ChoosingPayment, "all-items-scanned",
              new[] { "ui_click" }, new[] { CheckoutState.CardPresented, CheckoutState.CashPresented });

            S(CheckoutState.CardPresented, "payment", "card", "card-presentation", "card-payment", "The customer is presenting their card.",
              4f, "The presented card did not reach its insertion path.", CheckoutState.CardPresented, "card-unapproved",
              new[] { "card_present" }, new[] { CheckoutState.CardInsertReady, CheckoutState.ChoosingPayment, CheckoutState.AllProductsScanned });
            // waits on the player: a machine-speed watchdog here killed sales mid-flight in Golf's 2026-08-03 playtest
            S(CheckoutState.CardInsertReady, "payment", "card", "card-reader-focus", "card-payment", "Take the card and insert it.",
              null, "The offer waits safely until the player takes the card.", CheckoutState.CardPresented, "card-unapproved",
              new[] { "reader_ready" }, new[] { CheckoutState.CardInserting, CheckoutState.ChoosingPayment, CheckoutState.AllProductsScanned });
            S(CheckoutState.CardInserting, "payment", "card", "card-reader-focus", "card-payment", "Inserting card...",
              4f, "The automatic card insertion stopped progressing.", CheckoutState.CardPresented, "card-unapproved",
              new[] { "card_insert" }, new[] { CheckoutState.CardAmountEntry, CheckoutState.ChoosingPayment, CheckoutState.AllProductsScanned });
            S(CheckoutState.CardAmountEntry, "payment", "card", "card-reader-focus", "card-payment", "Type the exact total, then press OK.",
              null, "The terminal waits safely for deliberate amount entry.", CheckoutState.CardPresented, "card-unapproved",
              new[] { "keypad_tap" }, new[] { CheckoutState.CardProcessing, CheckoutState.ChoosingPayment, CheckoutState.AllProductsScanned });
            S(CheckoutState.CardProcessing, "payment", "card", "card-reader-focus", "card-payment", "Processing card...",
              12f, "Card authorization exceeded its response window.", CheckoutState.CardPresented, "card-authorization",
              new[] { "card_processing" }, new[] { CheckoutState.CardApproved, CheckoutState.CardDeclined }, RecoveryResolver.CardAuthorization);
            S(CheckoutState.CardApproved, "payment", "card", "cashier-wide", "payment-complete", "Approved.",
              4f, "Approved-card feedback did not advance.", CheckoutState.PaymentComplete, "card-authorization",
              new[] { "card_approved" }, new[] { CheckoutState.PaymentComplete }, RecoveryResolver.CardAuthorization);
            S(CheckoutState.CardDeclined, "payment", "card", "card-presentation", "card-payment", "Declined. Try another card or use cash.",
              null, "A decline remains recoverable until the player chooses retry or cash.", CheckoutState.CardPresented, "card-unapproved",
              new[] { "card_declined" }, new[] { CheckoutState.CardPresented, CheckoutState.ChoosingPayment });

            S(CheckoutState.CashPresented, "payment", "cash", "cash-presentation", "cash-payment", "Take the cash the customer laid down.",
              null, "Customer-held cash remains a safe one-click target until accepted.", CheckoutState.CashPresented, "cash-unaccepted",
              new[] { "cash_present" }, new[] { CheckoutState.CashAccepted });
            S(CheckoutState.CashAccepted, "payment", "cash", "drawer-preparation", "cash-payment", "Cash received. Opening drawer...",
              5f, "The drawer did not begin opening after cash acceptance.", CheckoutState.CashAccepted, "cash-accepted-local",
              new[] { "cash_pickup" }, new[] { CheckoutState.DrawerOpening });
            S(CheckoutState.DrawerOpening, "payment", "cash", "open-drawer-focus", "cash-payment", "Opening drawer...",
              5f, "Drawer animation did not reach its open stop.", CheckoutState.CashAccepted, "cash-accepted-local",
              new[] { "drawer_unlock", "drawer_open" }, new[] { CheckoutState.DepositingCash });
            S(CheckoutState.DepositingCash, "payment", "cash", "open-drawer-focus", "cash-payment", "Stowing received cash...",
              8f, "Automatic tender deposit stopped progressing.", CheckoutState.CashAccepted, "cash-accepted-local",
              new[] { "notes_down", "coins_down" }, new[] { CheckoutState.SelectingChange });
            S(CheckoutState.SelectingChange, "payment", "cash", "change-selection-focus", "change-selection", "Count the change, then confirm.",
              null, "Change selection is deliberate and survives pause, blur, or alt-tab.", CheckoutState.CashAccepted, "cash-accepted-local",
              new[] { "bill_handle", "coin_handle" }, new[] { CheckoutState.GivingChange });
            S(CheckoutState.GivingChange, "payment", "cash", "change-handoff-focus", "change-selection", "Handing change to the customer...",
              20f, "Customer change handoff did not complete.", CheckoutState.CashAccepted, "cash-accepted-local",
              new[] { "change_handoff", "drawer_close" }, new[] { CheckoutState.PaymentComplete });

            S(CheckoutState.PaymentComplete, "fulfilment", "shared", "cashier-wide", "payment-complete", "Payment complete.",
              5f, "Payment completion did not finish.", CheckoutState.PaymentComplete, "payment-authorized",
              new[] { "register" }, new[] { CheckoutState.ReceiptPrinting }, RecoveryResolver.PaymentCheckpoint);
            // Geode files its paperwork in the specimen's own history; no paper exists, so this is a beat, not a printer
            S(CheckoutState.ReceiptPrinting, "fulfilment", "shared", "cashier-wide", "payment-complete", "Closing the sale...",
              15f, "Sale close did not complete.", CheckoutState.ReceiptPrinting, "payment-authorized",
              N, new[] { CheckoutState.Bagging }, RecoveryResolver.PaymentCheckpoint);
            S(CheckoutState.Bagging, "fulfilment", "shared", "bagging-focus", "bag-transfer", "Packing the order...",
              15f, "Automatic order packing stopped progressing.", CheckoutState.Bagging, "payment-authorized",
              new[] { "bag_item" }, new[] { CheckoutState.BagHandoff }, RecoveryResolver.PaymentCheckpoint);
            S(CheckoutState.BagHandoff, "fulfilment", "shared", "bag-handoff-focus", "bag-transfer", "Handing the order to the customer...",
              20f, "Bag handoff did not reach the customer target.", CheckoutState.Bagging, "payment-authorized",
              new[] { "bag_handoff" }, new[] { CheckoutState.Bagging, CheckoutState.CustomerLeaving }, RecoveryResolver.BagHandoffCheckpoint);
            S(CheckoutState.CustomerLeaving, "fulfilment", "shared", "cashier-wide", "complete", "Transaction complete.",
              45f, "Paid customer could not leave the checkout zone.", CheckoutState.Bagging, "sale-bank-checkpoint",
              new[] { "crystal_chime" }, new[] { CheckoutState.TransactionComplete }, RecoveryResolver.SaleBankCheckpoint);
            S(CheckoutState.TransactionComplete, "fulfilment", "shared", "world-first-person", "complete", "Sale complete.",
              null, "Terminal state has no timeout.", CheckoutState.TransactionComplete, "sale-banked",
              new[] { "station_leave" }, Array.Empty<CheckoutState>());
            S(CheckoutState.Recovery, "recovery", "shared", "recovery-safe-pose", "recovery", "Restoring checkout...",
              null, "Recovery cleanup is synchronous; an explicit void is available if its checkpoint cannot validate.",
              null, "stored-resume-state", new[] { "ui_error" }, RecoveryResumeStates, RecoveryResolver.StoredTarget);
            return d;
        }

        /// <summary>The legal edges out of a state. Every non-terminal state can enter Recovery; Recovery resumes only a checkpoint.</summary>
        public static IEnumerable<CheckoutState> Transitions(CheckoutState from)
        {
            var spec = Specs[from];
            if (from == CheckoutState.Recovery) return spec.NextStates;
            if (from == CheckoutState.TransactionComplete) return spec.NextStates;
            return spec.NextStates.Concat(new[] { CheckoutState.Recovery });
        }

        public static bool CanTransition(CheckoutState from, CheckoutState to) => Transitions(from).Contains(to);

        /// <summary>
        /// The build-time guarantee: 30 states, no self-transitions, every non-terminal state exposes Recovery, and
        /// only the states that wait on a human are allowed to have no watchdog.
        /// </summary>
        public static List<string> Validate()
        {
            var problems = new List<string>();
            var all = (CheckoutState[])Enum.GetValues(typeof(CheckoutState));
            if (all.Length != 30) problems.Add($"expected 30 states, found {all.Length}");
            var humanWaits = new HashSet<CheckoutState>
            {
                CheckoutState.WaitingForScan, CheckoutState.CardInsertReady, CheckoutState.CardAmountEntry,
                CheckoutState.CardDeclined, CheckoutState.CashPresented, CheckoutState.SelectingChange,
                CheckoutState.TransactionComplete, CheckoutState.Recovery,
            };
            foreach (var s in all)
            {
                if (!Specs.ContainsKey(s)) { problems.Add($"{s} has no spec"); continue; }
                var spec = Specs[s];
                var next = Transitions(s).ToList();
                if (next.Contains(s)) problems.Add($"{s} transitions to itself");
                if (s != CheckoutState.TransactionComplete && s != CheckoutState.Recovery && !next.Contains(CheckoutState.Recovery))
                    problems.Add($"{s} does not expose the Recovery edge");
                if (spec.TimeoutSeconds == null && !humanWaits.Contains(s)) problems.Add($"{s} has no watchdog but does not wait on a human");
                if (spec.TimeoutSeconds != null && humanWaits.Contains(s)) problems.Add($"{s} waits on a human but carries a watchdog");
                if (string.IsNullOrEmpty(spec.Prompt)) problems.Add($"{s} has no prompt");
                if (spec.RecoveryResume != null && !RecoveryResumeStates.Contains(spec.RecoveryResume.Value) && s != CheckoutState.Recovery)
                    problems.Add($"{s} resumes {spec.RecoveryResume} which is not a safe checkpoint");
            }
            return problems;
        }
    }

    /// <summary>Facts the recovery resolver reads; it never invents an approval and never lets go of a banked sale.</summary>
    public struct CheckoutFacts
    {
        public bool PaymentAuthorized, CashAccepted, SaleBanked, BagOwned, AnyScanned, AllScanned;
    }

    /// <summary>The live flow: where the checkout is, how long it has been there, and where a failure resumes.</summary>
    [Serializable]
    public sealed class CheckoutFlow
    {
        public CheckoutState Current = CheckoutState.CustomerApproaching;
        public float EnteredAt;
        public string LastReason = "created";
        public CheckoutState? RecoveryResume;
        public string RecoveryCheckpoint;
        public List<string> History = new List<string>();

        public CheckoutStateSpec Spec => CheckoutFlowContract.Spec(Current);

        public bool To(CheckoutState next, float now, string reason)
        {
            if (next == Current) return false;
            if (!CheckoutFlowContract.CanTransition(Current, next)) return false;
            History.Add($"{Current}->{next} ({reason})");
            if (History.Count > 64) History.RemoveAt(0);
            Current = next;
            EnteredAt = now;
            LastReason = reason;
            return true;
        }

        public bool TimedOut(float now)
        {
            var t = Spec.TimeoutSeconds;
            return t != null && now - EnteredAt > t.Value;
        }

        /// <summary>Enter recovery, choosing the resume checkpoint by the failing state's own resolver.</summary>
        public void EnterRecovery(float now, string reason, CheckoutFacts facts)
        {
            RecoveryResume = ResolveResumeTarget(Current, facts);
            RecoveryCheckpoint = Spec.RecoveryCheckpoint;
            To(CheckoutState.Recovery, now, reason);
        }

        public static CheckoutState ResolveResumeTarget(CheckoutState failing, CheckoutFacts facts)
        {
            var spec = CheckoutFlowContract.Spec(failing);
            switch (spec.Resolver)
            {
                case RecoveryResolver.ScanProgress:
                    return facts.AllScanned ? CheckoutState.AllProductsScanned : CheckoutState.WaitingForScan;
                case RecoveryResolver.CardAuthorization:
                    return facts.PaymentAuthorized ? CheckoutState.PaymentComplete : CheckoutState.CardPresented;
                case RecoveryResolver.PaymentCheckpoint:
                    return facts.PaymentAuthorized || facts.CashAccepted ? CheckoutState.PaymentComplete : CheckoutState.ChoosingPayment;
                case RecoveryResolver.BagHandoffCheckpoint:
                    return facts.BagOwned ? CheckoutState.CustomerLeaving : CheckoutState.Bagging;
                case RecoveryResolver.SaleBankCheckpoint:
                    return facts.SaleBanked ? CheckoutState.CustomerLeaving : CheckoutState.Bagging;
                case RecoveryResolver.StoredTarget:
                    return CheckoutState.WaitingForScan;
                default:
                    return spec.RecoveryResume ?? CheckoutState.WaitingForScan;
            }
        }

        /// <summary>Resume the one validated checkpoint recovery chose. Any other target is refused.</summary>
        public bool Resume(float now, CheckoutState target, string reason)
        {
            if (Current != CheckoutState.Recovery) return false;
            if (RecoveryResume == null || RecoveryResume.Value != target) return false;
            bool ok = To(target, now, reason);
            if (ok) { RecoveryResume = null; RecoveryCheckpoint = null; }
            return ok;
        }

        /// <summary>
        /// The escape hatch: an UNAUTHORIZED checkout may drop back to a safe scan point. An authorized one is refused
        /// and must reconcile — "never invent an approval" does not require "never let go".
        /// </summary>
        public bool AbandonRecovery(float now, CheckoutFacts facts)
        {
            if (Current != CheckoutState.Recovery) return false;
            if (facts.PaymentAuthorized || facts.CashAccepted || facts.SaleBanked) return false;
            var target = facts.AllScanned ? CheckoutState.AllProductsScanned : CheckoutState.WaitingForScan;
            RecoveryResume = target;
            return Resume(now, target, "abandoned recovery");
        }
    }
}
