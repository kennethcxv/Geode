# CHECKOUT / CASHIER PORT HANDOFF — Golf Empire → Geode Empire

Written for the Claude that will reproduce this checkout inside
`~/Documents/GitHub/Geode/Geode` (Unity 6, `GeodeEmpire.Runtime` /
`GeodeEmpire.Editor` / `GeodeEmpire.Tests.EditMode`).

Everything below describes code and assets that **exist on disk today** in
`~/Documents/GitHub/Golf-Simulator`. Nothing here is a redesign, a proposal, or
a reconstruction from memory. Line references are to the working tree at commit
`ab3850c4`.

---

## 0. READ THIS FIRST — the two projects are not the same runtime

The request was phrased in Unity terms (prefabs, FBX, `.meta`, GUIDs). That
vocabulary does not apply to the source project, and pretending it does would
send you looking for files that have never existed.

**Golf Empire is not a Unity project.**

| | Golf Empire (source) | Geode Empire (target) |
|---|---|---|
| Runtime | Electron 39 + Chromium, `main.cjs` | Unity 6 player |
| Language | ES modules, plain JavaScript | C# (`Assembly-CSharp`, `GeodeEmpire.Runtime`) |
| Renderer | three.js r185, **no bundler** — `index.html` import map resolves `three` | URP / Unity SRP |
| 3D assets | `.glb` (glTF binary), authored in `.blend` by Python build scripts | `.fbx`/`.prefab`/`.asset` + `.meta` |
| Asset identity | **file path string**, resolved at load; `Assets/MANIFEST.md` says which copy ships | GUID in a `.meta` sidecar |
| Prefabs | none — every prop is `merch.instantiateKit('<stem>')`, a `clone(true)` of a cached parsed GLB `Object3D` | real `.prefab` assets |
| Scene | built procedurally every load from `src/data/shopLayout.js` | `.unity` scene files |
| Tests | `node --test` over 528 `tests/*.test.js` + Electron QA drivers | Unity Test Framework |

Consequences you must plan around, stated once here and referenced later:

- **There are zero `.meta` files and zero GUIDs to preserve.** Section 19 covers
  what actually plays the role GUIDs play here, and what you must create on the
  Unity side.
- **There are zero `.fbx` and zero `.prefab` files.** Section 8/10/18 give exact
  `.blend` → `.glb` → `vendor/models/` paths and the *node names inside the
  GLBs*, which are the real contract.
- The port is therefore **a rewrite of the presentation layer and a
  transliteration of the simulation layer.** The simulation layer is ~9,000
  lines of pure, DOM-free, three.js-free JavaScript with 528 tests behind it.
  That is the part worth carrying across nearly line-for-line. The 10,861-line
  three.js presentation module is the part you re-author in C# against the same
  contracts.

Sections 16, 17 and 22 are the ones that matter most for you. Section 23 is the
ordered plan.

---

## 1. ALL CHECKOUT-RELATED SOURCE FILES

### 1.1 Simulation layer — pure, no three.js, no DOM (`src/sim/`)

These are the authority. All of them are `node --test`-able headlessly.

| File | Lines | What it does |
|---|---:|---|
| `src/sim/register.js` | 2350 | **The transaction.** Currency in integer cents, `createTx`, scan, subtotal/discount/tax/total, payment start, the whole card sub-machine, the whole cash sub-machine, drawer stacks, change window, receipt, bagging, `canComplete`, `voidTx`, `completeSale` (the only place money moves), `segmentHitsBox` (swept scan volume test). |
| `src/sim/checkout.js` | 968 | **Stock in flight.** `pickFromShelf` / `returnToShelf` / `consumeHeld` / `consumeHeldBatch` — the `shop.held` ledger that makes "in a shopper's hands" a real, saved location. `recoverCheckout` (what a reload does). `checkoutSale` — the headless "direct" sale path used by tutorials/QA/legacy. Payment helpers `cashTender`, `startPayment`, `giveChange`, `processCard` (legacy simple path). |
| `src/sim/checkoutSettlement.js` | 3171 | **The write-ahead log.** A checkout spans inventory lots, the drawer, ledger entries, tax liability, sales analytics, transaction history and customer history — JS cannot make those one transaction. This module persists a settlement record *before* the first irreversible write and reconciles it afterwards. `preparePendingCheckout` / `reconcilePendingCheckout` / quarantine + release. |
| `src/sim/registerFlow.js` | 823 | **The player-visible state machine contract.** 30 states, their camera pose, player animation, customer animation, POS state, prompt, audio cue list, completion condition, timeout and recovery path — as data. Owns no money, no meshes. |
| `src/sim/checkoutCashContract.js` | 43 | `checkoutPaymentContract(ticket)` — a persisted ticket must describe a payment a player could physically have performed. `MAX_EXTRA_CHANGE_CENTS = 500`. |
| `src/sim/checkoutPreferences.js` | 73 | Accessibility prefs under `state.uiPrefs.checkout`: large text/targets, reduced camera motion, faster animations, automatic exact change, confirm cash purchase. |
| `src/sim/customerSimulation.js` | 1332 | The serializable clubhouse population: 28 `CUSTOMER_STATE` values, arrivals, queue slots + patience, product reservation, checkout start/fail/complete marks, recovery, despawn. |
| `src/sim/customerBasket.js` | 71 | `CARRY` categories (basket / hand / two-hand / special / hanger), `carryCategory(sku)`, unit lifecycle labels (`shopping → staged → sold/abandoned`). |
| `src/sim/paymentBag.js` | 107 | Cash-or-card drawn from a **balanced shuffled bag** (5 cash + 5 card per batch), persisted in `state.shop.paymentBag`. One draw per customer, for life. |
| `src/sim/cardSwipe.js` | 50 | `judgeSwipe(samples)` — normalized top-to-bottom swipe gesture judge. |
| `src/sim/barcode.js` | 47 | `barcodeFor(skuId, price)` (an 11-digit body — 6 from a FNV hash of the SKU, 5 from the price in cents — plus a mod-10 check digit), `judgeBarcodeRead(...)`, `BARCODE_FACING_DOT = -0.35`. |
| `src/sim/salesTax.js` | 467 | Jurisdiction rate, `salesTaxOn`, `SALES_TAX_LINE`, liability accrual. |
| `src/sim/inventoryLifecycle.js` | 1568 | Lot-level inventory with `INVENTORY_STAGE` buckets and an idempotent operation journal (`referenceId` keyed). Checkout moves `CUSTOMER_HELD → SOLD` through it. |
| `src/sim/reservationCheckIn.js` | — | `attachGreenFeeToTx` merges a `service:green-fee` line into a live retail ticket; `finalizeReservationCheckIn`. |
| `src/sim/economy.js` | — | `postLedgerEntry`, `preflightLedgerEntry`, `preflightOutcome`, `recordOutcome`. All checkout money lands here. |
| `src/sim/shop.js` | — | `initShop` (the `state.shop` shape), `priceFor`, `shelfCapacity`, `skuDisplayIsPlaced`. |
| `src/sim/state.js` | — | `newGame`, `serialize`/`deserialize`/`snapshot`, and the load-time repair that can quarantine a torn checkout journal. |

### 1.2 Presentation layer — three.js (`src/render3d/`)

| File | Lines | What it does |
|---|---:|---|
| `src/render3d/clubhouse/simplifiedRegisterMode.js` | **10861** | The entire playable register. Camera poses, workspaces, product meshes, drawer rig + money instancing, card mesh + terminal keypad, bag rig, POS canvas, pointer/keyboard input, the watchdog, GPU pre-warm, resource ledgers, QA hooks. `createRegisterMode(B)` returns the ~90-member API listed at line 10549. |
| `src/render3d/clubhouse/frontDeskMonitorUi.js` | 1259 | The 1024×640 POS canvas: tabs (Check In / Checkout / Tee Sheet), item rows, totals, cash screen, hotspot registry, overlap/truncation audit. |
| `src/render3d/clubhouse/checkoutPaymentPresentation.js` | 163 | Pure layout math: presented tender fan on the counter, counted-change flat pile, change bundle, handoff points, customer card point. |
| `src/render3d/clubhouse/checkoutScanPresentation.js` | 125 | Barcode bit pattern (EAN-style), scan choreography phases/timings, `scannerReadFacts` ray math. |
| `src/render3d/clubhouse/cashierPresentation.js` | 77 | Pure routing: which camera view, which hand pose, which pick size — for every frame of the flow. |
| `src/render3d/clubhouse/cashierHands.js` | 277 | Procedural first-person cashier arms, 22 named poses. **Suppressed in the shipping simplified register** (props animate themselves) but retained. |
| `src/render3d/clubhouse/drawerMoneyLayout.js` | 89 | Deterministic bill stack / coin mound placement inside an authored well contract. |
| `src/render3d/clubhouse/customerPaidBag.js` | 364 | Bag ownership transfer to the customer, carry sync, and a retry ledger so a failed WebGL disposal cannot leak. |
| `src/render3d/clubhouse/customerFlow.js` | 173 | Pure: organic order planning, sequential placement controller, impatient beat, `checkoutStagingPose`. |
| `src/render3d/clubhouse/catalogProductVisual.js` | 1028 | Per-SKU checkout descriptor: model stem, size, barcode surface, grip mode, `separateHandoff` (oversize). `catalogCheckoutLayout` for counter staging. |
| `src/render3d/clubhouse/customers.js` | 1628 | Customer actors: movement, animation mode selection, carried item meshes, counter poses. |
| `src/render3d/clubhouse/registerItemResources.js` | 124 | Identity-based WebGL resource ownership for register-minted meshes. |
| `src/render3d/clubhouse/registerCameraPoses.js` | 70 | Card handoff / terminal / fulfilment camera poses derived in the front-desk frame. |
| `src/render3d/clubhouse/fixtures.js` (lines 1960–2085) | — | Places the physical kit on the counter from `REGISTER` datums. |
| `src/render3d/clubhouse/merch.js` (lines 456–575) | — | The `KIT` list and the GLB loader that populates `kit:<stem>` prototypes. |
| `src/render3d/clubhouse.js` | 14700+ | Owns the *person*: approach, queue, placement, `handPlacedItemsToRegister`, `onCustomerPaid`, departure, and the `register.update(dt)` call in the frame loop (line 13895). |
| `src/render3d/characterAsset.js` | — | Procedural articulated customer figure; `setMode('Carry'|'Checkout'|'PayCash'|'PayCard'|'ReceiveBag'|...)`, `hand('L'|'R')`, `carryGrip('L')`. |

### 1.3 Data (`src/data/`)

| File | What it contributes |
|---|---|
| `src/data/shopLayout.js` (1521) | `FRONT_DESK_FRAME`, `COUNTER`, `COUNTER_TOP`, `COUNTER_WORK_TOP`, **`REGISTER`** (every prop pose and every workspace rect), `queueSlot(i)`, `frontDeskPoint/Vector/LocalPoint/Pose`. |
| `src/data/shopItems.js` | `SHOP_CATALOG` — id, cat, tier, name, cost, msrp, `lb`, `fragile`, `form`. |
| `src/data/paymentCards.js` | `PAYMENT_CARDS` — network/issuer/tier/mark rows; deliberately non-trademarked. |
| `src/data/productPackaging.js` | Packed freight dimensions reused by `catalogProductVisual`. |
| `src/data/salesTax.js` / `src/sim/salesTax.js` | Jurisdiction table. |
| `src/data/fixtureSlots.js` | Shelf capacity per SKU. |

### 1.4 UI / audio / entry points

| File | Role |
|---|---|
| `src/core/audio.js` | All checkout SFX. Procedural WebAudio + sampled fallbacks. Imports `BILLS` from `sim/register.js` so audio and the till cannot disagree about what a `5` is. |
| `src/ui/registerGuidance.js` (133) | The player-facing hint strings per stage. |
| `src/ui/laptop.js` (4082) | The Fairway Office DOM interface projected onto the laptop glass. |
| `src/core/laptopRig.js` (114) | Pure laptop geometry + screen-corner solve. |
| `src/main.js` | Boots the app, routes pointer/keyboard into the register, drives the laptop DOM alignment, exposes `window.__fw` for QA. |

---

## 2. THE CHECKOUT STATE MACHINE / TRANSACTION SEQUENCE

There are **two** state machines and they are deliberately separate. Port both.

### 2.1 The domain stage machine (`tx.stage`, in `src/sim/register.js`)

`createTx()` starts at `'scanning'`. Legal stages, and the verb that moves each:

```
scanning
  ├─ scanItem(tx, uid)            item.scanned = true   (stays in scanning)
  ├─ bagScannedItem(tx, uid)      item.bagged  = true   (simplified loop; stays in scanning)
  └─ requestPayment(tx)  ── refuses while unscannedCount(tx) > 0
        │  method = tx.prefer ?? coin flip
        ├─ cash & cashTotalOf(tx) === 0 ───────────────────────────► receipt
        ├─ cash ──────────────────────────────────────────────────► cash-tender
        └─ card ──────────────────────────────────────────────────► card-present

card-present  ── presentCard()          ──► card-ready
card-ready    ── insertCard()           ──► card-entry     (entry opens at 0.00)
              ── abandonCardBeforeSubmit() ──► scanning
card-entry    ── enterCardDigit/backspace/clear
              ── submitCardAmount()     ──► card-busy      (refuses ≠ exact total)
              ── abandonCardBeforeSubmit() ──► scanning
card-busy     ── runCard()              ──► receipt | card-declined
              ── recoverUnresolvedCardAuthorization() ──► card-present
card-declined ── retryCard()            ──► card-ready
              ── cancelCard()           ──► payment
payment       ── payCashInstead()       ──► cash-tender

cash-tender   ── customerCash(tx) builds tx.tendered
              ── acceptCash(tx)         ──► cash-drawer   (snapshots acceptedTender)
cash-drawer   ── openDrawer / depositPiece × n / depositTendered
              ── takeFromDrawer / returnToDrawer  (builds tx.hand)
              ── handOverChange(tx)     ──► receipt       (enforces the change window)
              ── recoverCashAcceptedCheckpoint(tx, drawer) ──► cash-drawer (safe replay)

receipt       ── printReceipt(tx)  → takeReceipt(tx) ──► bagging
bagging       ── packReceipt(tx); bagItem(tx, uid) × n
              ── handOverGoods(tx)      ──► done          (requires allBagged + receiptPacked)
done          ── completeSale(state, tx, who, opts)  ── THE ONLY PLACE MONEY MOVES
voided        ── voidTx(tx)              terminal, no money moved, drawer stacks discarded
```

`canComplete(tx)` is the single guard on banking:
`stage === 'done' && receiptPrinted && receiptPacked && allBagged(tx)`.

`allBagged` ignores **service lines** (`skuId` starting `service:`) — a tee time
is not something you put in a bag, so a combined ticket could otherwise never be
handed over.

### 2.2 The physical flow contract (`src/sim/registerFlow.js`)

30 states, exported in order as `CHECKOUT_STATE_ORDER`:

```
CustomerApproaching → CustomerPlacingProducts → WaitingForCashier
 → EnteringCashierMode → WaitingForScan
 → ProductHeld → ProductScanning → ProductScanned   (loop per item)
 → AllProductsScanned → ChoosingPayment
   ├─ CARD:  CardPresented → CardInsertReady → CardInserting → CardAmountEntry
   │         → CardProcessing → CardApproved | CardDeclined
   └─ CASH:  CashPresented → CashAccepted → DrawerOpening → DepositingCash
             → SelectingChange → GivingChange
 → PaymentComplete → ReceiptPrinting → Bagging → BagHandoff
 → CustomerLeaving → TransactionComplete
                                                    (+ Recovery, from anywhere)
```

Every state carries, **as data**, exactly these fields (validated by
`validateCheckoutContract()`, which also asserts the count is 30):

`id, phase, branch, entryAction, allowedInput[], camera{pose,transition,lookControl},
playerAnimation, customerAnimation, uiState{posState,prompt}, audio[],
completionCondition, timeout{seconds,action,reason}, recoveryPath{resumeState,
checkpoint,action,resolver}, nextStates[]`

Hard rules encoded in that module — **carry these verbatim**:

- Every non-terminal state exposes the `Recovery` edge. `validateCheckoutContract`
  fails the build if one does not.
- No state may transition to itself.
- `Recovery` may resume **only** the checkpoint chosen when recovery began
  (`flow.recovery.resumeState`), selected by `resolveCheckoutRecoveryTarget` from
  one of five resolvers: `scan-progress`, `card-authorization`,
  `payment-checkpoint`, `bag-handoff-checkpoint`, `sale-bank-checkpoint`,
  `stored-target`.
- `abandonCheckoutRecovery` is the escape hatch: an **unauthorized** checkout may
  drop back to `WaitingForScan`/`AllProductsScanned`; an authorized one is
  refused and must reconcile. (Added after a 2026-08-03 playtest where the
  register sat dead in Recovery with a customer standing at it.)
- Three states deliberately have **no timeout** because they wait on a human:
  `CardInsertReady`, `CardAmountEntry`, `SelectingChange`, plus `WaitingForScan`
  and `CardDeclined`. Everything else has a watchdog (4 s–180 s).
- `SIMPLIFIED_REGISTER_WATCHDOG_STATES` in `simplifiedRegisterMode.js` is the
  subset the renderer actively polices. `CardInsertReady` is explicitly absent.

The two machines are coupled only at the renderer: `simplifiedRegisterMode`
calls `flowTo(state, reason)` alongside the domain verb, and `tx.checkoutFlow`
holds the flow object so it survives on the transaction.

---

## 3. CASH FLOW

### 3.1 Currency

`src/sim/register.js` lines 12–52. **Money is integer cents internally.** The
comment states the reason: a drawer holds hundreds of dimes and `0.1 * 300` is
`30.000000000000004`, which makes a till that balances on paper fail in code.

```js
export const BILLS = [50, 20, 10, 5, 1];
export const COINS = [0.5, 0.25, 0.1, 0.05, 0.01];
export const DENOMS = [...BILLS, ...COINS];
```

The **quarter is canonical**. An earlier build shipped a 20¢ piece because asset
Sheet 02 happened to author one; the drawer then labelled its fourth well "20¢",
a denomination that does not exist. `migrateLegacyQuarterStack` retires it by
exchanging each 0.2 for two dimes (identical value, no invention).

### 3.2 Tender — what a customer actually hands over

`customerCash(tx)` (line 640). Two behaviours, deliberately:

1. **Notes for the dollars, coins for the cents** (≈55% when the cents are
   payable in large coins) so the change comes back in whole dollars. This is the
   commoner real-shop move and it is what puts coins on the desk.
2. **Round up to the next note** (step = 50 if due > 100, 20 if > 40, 10 if > 15,
   else 5), optionally plus exact quarters (35% when cents % 25 === 0).

`payableInLargeCoins(cents)` gates (1): quarters and dimes, nickels only to
finish a five. Nobody counts out ninety-six cents in pennies at a counter. An
audit on 2026-08-07 found the unrestricted branch put sub-quarter shrapnel in
34% of all cash tenders.

The pieces are materialised by `makeChange(amount)` — greedy over the unlimited
canonical set.

### 3.3 The drawer

Opening float, `newDrawer()` (line 621):

```
$50 × 2   $20 × 5   $10 × 8   $5 × 10   $1 × 25
50¢ × 16  25¢ × 25  10¢ × 20   5¢ × 20   1¢ × 50
```

Stack primitives: `stackTotal`, `stackCount`, `addToStack`, `takeFromStack`.

**Transaction-local working copy.** `localDrawer(tx, drawer)` snapshots
`tx.drawerStart` and `tx.drawerPending` on first touch. `state.shop.drawer`
remains the opening float until `completeSale` atomically replaces it. A voided
or reloaded sale therefore costs nothing — `voidTx` just discards the local
stacks. `drawerContents(tx, drawer)` is the read path for drawing.

Deposit: `openDrawer(tx)` → `depositPiece(tx, drawer, denom)` one piece at a
time (or `depositTendered` for the whole handful). `tx.deposited` becomes true
when `stackCount(tx.tendered) === 0`.

`makeChangeFrom(drawer, amount)` is **bounded** change: a dynamic-programming
solve over what is really in each slot, because a greedy pick can fail when a
slot is temporarily empty even though exact change exists. Returns `null` when
the till genuinely cannot make it — Relaxed mode has to know that before it
promises a correct-change highlight it cannot honour.

`migrateDrawer(drawer)` rebalances pre-cent-accurate saves into half-dollars and
pennies **without creating or destroying value** (it verifies the cent total is
unchanged and returns the un-migrated stack if not).

### 3.4 Change

```js
changeDue(tx) = max(0, tenderedTotal − cashTotalOf(tx))
```

`tx.tenderedTotal` is captured as a **number** at `acceptCash`, because the
`tendered` stack is about to be dismantled piece by piece into the till; reading
change off a stack being dismantled would walk it down to zero.

The player builds `tx.hand` with `takeFromDrawer` / `returnToDrawer`.
`changeGivingState(tx)` classifies:

| delta (cents) | state | `handOverChange` result |
|---|---|---|
| < 0 | `short` | refused — "Not enough - count it again." |
| = 0 | `exact` | accepted, `lost = 0` |
| 0 < d ≤ 500 | `over` | accepted, `tx.lost = d/100` (till short by the courtesy) |
| > 500 | `excess` | refused — "Too much - count it again." |

`MAX_EXTRA_CHANGE_CENTS = 500` lives in `checkoutCashContract.js` and is
re-exported from `register.js`. **The customer can never be under-paid, not even
by a cent, in any difficulty mode.** The $5 courtesy overage is the only slack,
and it books to the `cashOverShort` ledger line as an expense.

`handOverChange` sets `tx.changeGiven`, clears `tx.hand`, closes the drawer, and
moves to `receipt`.

### 3.5 Handoff (visual)

- `presentedTenderLayout(denoms, anchor)` — the customer **lays the money on the
  counter** at `REGISTER.customerTender` (a point on the *customer* half, clearly
  right of the bag mouth and clearly left of the change pile). Notes fan flat,
  climbing only paper thickness; coins sit flat at the near edge.
- `selectedChangeLayout(denoms, handoff, counterTop)` — counted change piles
  **flat on the bare counter** at `REGISTER.changeHandoff` (0.38 × 0.20
  footprint, left of the monitor and clear of it). The authored green handoff
  tray prop was deleted on 2026-07-30.
- `changeBundleLayout(denoms)` — once confirmed, every piece becomes one
  physical handful with local offsets inside a desk-oriented carrier.
- `changeHandoffPoint(hand)` — the customer's grip, offset so fingers pinch an
  edge rather than occupy the prop centre.

### 3.6 Drawer commit (`drawerCommitFor`, line 1019)

Refuses to bank unless: cash is deposited, both local stacks exist, the hand is
empty, the drawer is closed, `state.shop.drawer` still equals `tx.drawerStart`
(or already equals `tx.drawerPending` — the idempotent replay case), and

```
stackTotal(drawerPending) − stackTotal(drawerStart) === dueOf(tx) − tx.lost
```

If that arithmetic disagrees, the sale does not bank. Full stop.

---

## 4. CARD FLOW

### 4.1 The reader is a device, not a yes/no

`presentCard` → `insertCard` → `card-entry` → `submitCardAmount` → `runCard`.

**`insertCard` opens the terminal at 0.00.** The comment at line 385 records
why: it used to prefill the exact total and leave the cashier to press Confirm,
which made keying the amount — the one act that makes a card sale feel like
operating a till — optional and usually skipped. `submitCardAmount` still
refuses anything that is not the exact total, so this moved work to the player
without loosening the check.

Entry errors are terminal-screen strings, not exceptions:
- empty entry → `ENTER AMOUNT`
- wrong amount → `AMOUNT MUST MATCH TOTAL`

Keypad verbs: `enterCardDigit(tx, 0-9)` (max 8 digits), `backspaceCardAmount`,
`clearCardAmount`, `submitCardAmount`. `cardEnteredAmount(tx)` reads it back.

### 4.2 Processing and approval

```js
const DECLINE_CHANCE = 0;   // production
```

Normal gameplay **approves deterministically** after an exact entry. The
probability shape is retained so tests can force outcomes:
`runCard(tx, { timeout: true })` → `timeout`; `{ force: 'declined' }` →
`card-declined`; `{ force: 'approved' }` → `receipt`.

`retryCard` requires a *different* card (`tx.cardsTried += 1`) and returns to
`card-ready`. `cancelCard` drops all the way to `payment` so they can pay cash.

### 4.3 Abandoning a card run

`abandonCardBeforeSubmit(tx)` — the cashier pulls the run at the reader's X.
Legal **only** from `card-present`, `card-ready`, `card-entry`. Never from
`card-busy` (in flight) or after a result. The basket stays intact and every
item stays scanned; the sale drops back to `scanning`, where the payment choice
re-opens. This can never double-settle or strand a paid customer.

`recoverUnresolvedCardAuthorization(tx)` — a renderer watchdog may lose an
in-flight terminal animation before `runCard` produced a result. This rolls
`card-busy` back to `card-present` **without inventing an approval or a
decline** and without touching attempt counters. Explicitly refused once
`cardResult === 'approved'`.

### 4.4 The physical card

- Geometry: ISO/IEC 7810 ID-1 — `CARD_WIDTH 0.086`, `CARD_HEIGHT 0.054`,
  `CARD_THICKNESS 0.0014` m. `CARD_HELD_PITCH = 0.62` rad.
- Asset: `vendor/models/checkout/payment_card.glb` (authored 0.0856 × 0.054).
- Face art: painted per design from `src/data/paymentCards.js` into a cached
  `CanvasTexture` (`createPaymentCardTextureCache`). Five marks are supported:
  `chevrons`, `ring-bar`, `lattice`, `wave`, `keystone`. **Names and marks are
  deliberately coined and unlike real networks**; `tests/payment-card-variants.test.js`
  holds a refusal list of real network/issuer names and fails the build if one
  appears.
- The card rides the customer's articulated carry grip. `updateCard(dt)`
  re-asserts `poseCustomerForCheckout('PayCard')` every frame while the offer is
  held out, because the customer controller's own `Stage` pose would otherwise
  drop the arm — and the grip-parented card with it, out of sight behind the
  counter. (The register updates *after* `customers.js` in the frame loop.)
- Insertion is automatic once the player clicks the offered card
  (`acceptPresentedCard`), taking `CARD_INSERT_TIME = 0.72` s into the authored
  `CARD_INSERT_SOCKET` / `ANCHOR_ChipSlot`.

### 4.5 The swipe judge (retained, not the live path)

`src/sim/cardSwipe.js` `judgeSwipe(samples)` — normalized top-to-bottom, with
`START_MAX 0.4`, `END_MIN 0.8`, `REVERSAL 0.2`, `MIN_SEC 0.05`, `MAX_SEC 1.6`
and seven message codes. `register.js` also carries `evaluateCardSwipe(samples,
opts)`, a counter-local X/Z judge kept for compatibility with existing checkout
evidence. **The shipping interaction is chip insert + keypad, not swipe.** Port
the judges only if you want the swipe interaction; otherwise they are dead code.

---

## 5. ITEM FLOW

### 5.1 Shelf → customer

`pickFromShelf(state, skuId, uid)` (`src/sim/checkout.js:69`):

1. Refuse if `inv.shelf <= 0` or the display is stored (`skuDisplayIsPlaced`).
2. Refuse if the UID is already held **or was ever used before**
   (`state.shop.inventoryLifecycle.operations['customer-pick:<uid>']`). A
   physical unit identity is single-use; the operation journal is persisted
   across reloads, so reusing a renderer-local UID would replay the old lot
   movement.
3. `moveInventory(SHELF → CUSTOMER_HELD, qty 1, referenceId 'customer-pick:<uid>')`.
4. `inv.shelf -= 1`; push `{uid, skuId}` onto `state.shop.held`; remember the
   lot allocations.

`returnToShelf` reverses it, filling the fixture first and overflowing into
`RESERVE` (back stock) rather than silently deleting at the capacity cap.

`consumeHeld` / `consumeHeldBatch` are the **only** legal way a held unit
vanishes: `CUSTOMER_HELD → SOLD`.

### 5.2 Customer → counter (staging)

Owned by `clubhouse.js:updateCustomerPlacement` + `customerFlow.js`.

- `createSequentialPlacement(cart)` — **at most one product may start or finish
  placement per call.** Even a long frame cannot teleport the rest of the order
  onto the counter behind the player's back.
- `PRODUCT_PLACE_SECONDS = 0.58` per item.
- Target poses come from `catalogCheckoutLayout(items, REGISTER.staging,
  COUNTER_TOP + 0.012, register.counterBagKeepOut() || REGISTER.bagging)`.
  Oversize (`separateHandoff`) descriptors get the "large" lane; compact ones
  fill the customer-side row.
- The mesh is `interior.attach(mesh)`ed from the customer's wrist (preserving
  world pose), then **rescaled to world-true authored size** — the carried mesh
  inherits the character body scale (0.87–0.99) and without this the goods
  landed at the customer's size and popped ~9% bigger when the register rebuilt
  them (measured: world 0.9186 → 1.0, popRatio 1.089).
- Arc: eased position lerp plus `sin(π·p) × 0.10` lift; rotations relax to the
  pose.
- On completion → `checkoutPhase = 'waiting'`, flow advances to
  `WaitingForCashier`.

`handPlacedItemsToRegister(c)` then removes the customer's proxy meshes and calls
`register.begin(c)`; on failure it restores the exact counter poses rather than
letting a transient busy till delete visible goods.

### 5.3 Ring-up (`bagProduct`, `simplifiedRegisterMode.js:7278`)

**One forgiving click owns the whole gesture.** There is no drag, no wheel
puzzle, no hidden second click.

```
click → flow: WaitingForScan → ProductHeld → ProductScanning
      → sfx('productPickup')
      → scanMotion = one lateral SLIDE along the counter, SLIDE_DURATION = 0.55 s
      → mid-slide: commitScanMotion() — scanItem + bagScannedItem + POS row + beep
      → destination: the bag mouth (pushed 0.34 × BAG_COUNTER_SCALE *into* the
        cavity so the interior shell swallows the good at FULL SIZE — what ends
        its visibility is the bag being around it, not a shrink)
      → flow: ProductScanned → WaitingForScan | AllProductsScanned
```

Both the staging strip and the bag sit on the counter's centre seam, so the run
is one lateral slide with no cross-counter drift and no climb. The register beep
**is** the scan — the POS line and success cue belong to the validated
barcode-contact edge inside `commitScanMotion`, never to the click itself.

The five-phase pickup → barcode-alignment → reader-pass → bag-arc choreography
(`CHECKOUT_SCAN_TIMING` in `checkoutScanPresentation.js`) still exists and is
still exercised by the legacy adapter, but the 2026-07-30 playtest rejected it as
ceremony. `checkoutScanPresentation.js` remains the authority for barcode bits
and `scannerReadFacts` ray math.

### 5.4 The swept scan volume (legacy drag path)

`REGISTER.scan` is a box: local x −0.48…0.08, z −0.18…0.24, **y 1.06…1.34
absolute**. An item counts as scanned when its barcode passes **through** it,
tested by `segmentHitsBox(p0, p1, box)` — a slab-method swept segment test, not
a point test. The comment at `register.js:2333` states the reason: at 60 fps a
fast mouse flick carries the barcode a third of a yard between frames, clean over
a 0.56 yd volume and out the far side, never once sampled inside it. A point test
would miss that scan, the item would land in the bag unscanned, and the register
would refuse payment while the player swore they scanned it.

### 5.5 Ownership transfer

Nothing about the goods is *owned* by the customer until the sale banks.

`clubhouse.js:onCustomerPaid(c, transaction)` is called by the register through
`cust.onPaid` **after** `completeSale` returns ok:

1. `transferCustomerPaidOwnership(c)` — authoritative ownership + route release.
2. `leaveReview(c, true)`, `clearCustomerItemMeshes(c)`, `beginPendingDesk(c)`.
3. The bag: `c.checkoutHandoffBag` (the physical counter bag the player handed
   over) if present, else a fresh `merch.instantiateKit('shopping_bag', {scale:
   0.86})`, else a legacy instantiate, else a procedural box.
4. `attachPaidBagToCustomer(c, bag, {productionBag, carryTarget})` where
   `carryTarget = char.carryGrip('L') || char.hand('L')`. The dedicated grip is a
   **scale-independent sibling** of the hand mesh, so the upright carrier follows
   the carry point without inheriting the hand ellipsoid's non-uniform scale —
   gravity keeps the bag vertical while the articulated arm moves.
5. `c.bagAcceptanceHold = PAID_BAG_ACCEPTANCE_HOLD_SEC` (1.4 s) and
   `c.bagAcceptanceYaw` preserve the handoff camera's orientation for the
   ownership beat; normal locomotion resumes when the hold expires.
6. `attachOversizePurchaseVisuals(c, transaction)` — clubs, umbrellas and stand
   bags are carried, not bagged (see §7).
7. `char.setMode('ReceiveBag')`.

**No receipt rides in the bag.** Round 7 (2026-07-31) removed the receipt from
the whole flow ("please completely remove the receipt"); the sim still files its
durable paperwork inside `beginAutomaticReceipt`, but no paper exists and
`fixtures.js` no longer places a printer.

---

## 6. CUSTOMER INTERACTION

### 6.1 Approach

`src/sim/customerSimulation.js` owns the persistent lifecycle. 28 states in
`CUSTOMER_STATE`; the checkout-relevant run is:

```
BROWSING → MOVING_TO_DISPLAY → INSPECTING_PRODUCT → SELECTING_PRODUCT
        → CARRYING_PRODUCT → MOVING_TO_QUEUE → WAITING_IN_QUEUE
        → MOVING_TO_REGISTER → STAGING_PRODUCTS → WAITING_FOR_CASHIER
        → PAYING → RECEIVING_BAG_AND_RECEIPT → LEAVING → EXITING → DESPAWNED
```

`MAX_ACTIVE_CUSTOMERS = 12`, `MAX_SERVICE_QUEUE = 6`.

Order size: `organicOrderSize(rng)` → 2–4 (`ORGANIC_ORDER_MIN/MAX`).
`planOrganicOrder(fixtures, inventory, rng)` consumes counts from a local copy
while planning so a shopper never plans four copies of a line that had one on
display; distinct fixtures are preferred before revisiting. **Fewer than two
available units makes it a browse-only visit** — a stock shortage must not
silently fall back to the retired one-item checkout path.

### 6.2 Queue

`queueSlot(i)` in `shopLayout.js:1197` — `queueBase + queueStep × i`, with a v2
overflow pocket past `V2_QUEUE.lineSlots` so the line does not pierce the west
wall.

- `QUEUE_ADVANCE_CLEARANCE = 0.95` — `queueSlotIsClear(slot, bodies, clearance)`
  and `queueAdvanceSlot(held, wanted, isClear)` gate stepping forward.
- `QUEUE_NEVER_ABANDON_DEPTH = 2` — the first two positions never give up.
- `QUEUE_TOTAL_WAIT_MULTIPLIER = 3`, `stepPreServiceWait(prev, dt, queueIndex,
  {serving})`, `queueGiveUp(clocks, queueIndex, patienceFull)`,
  `queuePositionMayAbandon(queueIndex)`.
- `createCustomerImpatientBeat(1.25 s)` gives a customer who runs out of patience
  a deterministic, visual-only restrained reaction before cleanup — they do not
  vanish on the threshold frame.

### 6.3 Hand / arm animation and grips

The customer is a **procedural articulated figure** (`characterAsset.js`), not a
rigged GLB. The comment at the top records why: "the rigged GLB path exported
broken skins twice (DEV_LOG 2026-07-09 asset session)".

Animation is mode-driven: `char.setMode(mode)` with the checkout-relevant modes
`Walk`, `Idle`, `Carry`, `Checkout`, `PayCash`, `PayCard`, `Stage`,
`ReceiveBag`. `char.hand('L'|'R')` returns the wrist object;
`char.carryGrip('L')` returns the scale-independent carry point.

`characterYawToward(fromX, fromZ, toX, toZ)` is the only facing math — the rig is
authored facing local **+Z** (eyes, nose, polo placket and shoe toes are all on
that side), and keeping the math in one place stops a caller adding a legacy
180° correction and making an actor walk backwards.

Cash presentation timing (`customers.js:1168`): the arm extends, the tender lands
(~0.9 s covers the fly-in), the arm comes back, and the customer waits with hands
settled. **A card stays held out.**

### 6.4 Carry poses (`src/sim/customerBasket.js`)

```js
CARRY = { BASKET: 'basket-compatible', HAND: 'hand-carried',
          TWO_HAND: 'two-hand-carry', SPECIAL: 'special-checkout-delivery',
          HANGER: 'clothing-on-hanger' }
```

`carryCategory(sku)`:
- `bag1` → SPECIAL
- `jacket2` → HANGER
- `umb1` → HAND
- `cat === 'clubs'` or `lb >= 4` → TWO_HAND
- otherwise → BASKET

`visibleBasketSlots(units, capacity = 3)` caps what reads in a hand basket.

### 6.5 Cashier hands (the player's own)

`cashierHands.js` builds stylised first-person arms that solve each wrist toward
a world-space target every frame. `CASHIER_POSES` has 22 entries (`idle`,
`reach`, `pick-small`, `pick-medium`, `rotate`, `scan`, `place-item`,
`accept-bill`, `accept-coin`, `deposit-bill`, `deposit-coin`, `select-change`,
`hold-change`, `give-change`, `hold-card`, `swipe-card`, `collect-receipt`,
`open-bag`, `bag-item`, `add-receipt`, `hand-bag`), each `{curl, spread}`.
Only the last `VIEWMODEL_FOREARM = 0.19` m of forearm renders, so counter depth
cannot turn a normal gesture into a long tube.

`cashierHandPoseForFrame({...})` in `cashierPresentation.js` is the pure routing
function that selects a pose from the frame's facts.

**In the shipping simplified register these arms are intentionally suppressed** —
the header comment says so: "Checkout props animate directly; detached
first-person cashier hands are intentionally suppressed to match the supplied
simulator reference and keep the shared counter readable." Port the module if you
want them; do not enable them by default.

### 6.6 Departure

`CustomerLeaving` banks the sale exactly once, releases the register reservation
and navigates the customer away with the bag (45 s watchdog). `char.setMode
('ReceiveBag')`, the 1.4 s acceptance hold, then normal route locomotion out
through `LEAVING → EXITING → DESPAWNED`.

---

## 7. PACKAGING LOGIC

### 7.1 Bag vs. carry — the size rule

The rule is **per-SKU authored data**, not runtime geometry inference.
`src/render3d/clubhouse/catalogProductVisual.js` `VISUALS` table, one row per
SKU, built by `visual(kind, options)`:

```js
separateHandoff: !!options.oversize
gripMode:        options.grip || 'small'      // 'small' | 'medium' | 'two-hand'
size:            [x, y, z] metres
barcodeSurface:  'package-back' | 'package-side' | 'hang-tag' | 'apparel-tag' | 'club-tag'
```

`oversize: true` (⇒ `separateHandoff`) is set on exactly these retail SKUs:

| SKU | Size (m) | Why |
|---|---|---|
| `driver1/2/3` | ~1.14–1.18 long | club |
| `irons1/2` | ~1.02–1.04 long | club set |
| `putter1/2/3` | ~0.94–0.98 long | club |
| `wedge1/2` | ~0.96–0.98 long | club |
| `umb1` | 0.84 long | umbrella |
| `bag1`, `bag3` | 0.72–0.74 long | stand bag |

Everything else bags. `shoe1/shoe3` are `grip: 'medium'` but **not** oversize —
they go in the bag as the authored Fairhollow retail carton.

Freight-only descriptors (`vac1`, `desk1`, `counter1`, `rug1`, `lounge1`, …) are
also `oversize`, but they never enter a customer basket; they exist so opened
delivery boxes are truthful.

### 7.2 Where an oversize item goes

`bagProduct` (line 7291) branches on `mesh.userData.catalogVisual.separateHandoff`:

```js
const oversizePoint = frontDeskPoint(-1.02 + max(0, oversizeCount − 1) * 0.07, -0.12);
destination     = separateHandoff ? oversizePoint : bagMouth;
destinationKind = separateHandoff ? 'oversize'    : 'bag';
toQuaternion    = separateHandoff ? frontDeskQuaternion(-0.9, PI*0.6, 0.4)
                                  : frontDeskQuaternion(0, 0, itemRoll);
```

`mesh.userData.checkoutVisualState = 'oversize-set-aside'`. At handoff,
`beginBagDeliveryOrRelease` collects them into
`cust.checkoutHandoffOversizeProducts` and they attach to the customer as carried
goods alongside the bag. Handoff for these is the one route that still asks for a
drag (`settleBaggingProduct`, drop radius 0.50 vs `BAG_REACH = 0.34`).

### 7.3 Inside the bag — the geometry that was got wrong twice

`bagFitPlan(body, interior)` and `bagPlacementFor(plan, interior, opts)` at
`simplifiedRegisterMode.js:282–315`. Both pure and exported, because the fault
they replace was unreachable from a headless test.

The bug (`FOUND_FALSE.md` "The bag — 2 appearances", row 2): the previous clamp
was

```js
clamp(v, -(halfX - bodyHalfX), halfX - bodyHalfX)
```

which **inverts its own bounds** the moment `bodyHalfX > halfX`. A long item was
shoved sideways by its own overflow and cut through the paper on *both* walls at
once.

The replacement is a design answer, not a clamp:

```js
fitsLying = hx <= interior.halfX && hy <= interior.halfMouth && hz <= interior.halfDepth
if (fitsLying) → lie down, two-column stack (index % 2), layerStep 0.075
else           → rotate the LONGEST axis onto the mouth axis, stand it on the
                 interior floor, centre it across the width.
                 The only thing it can then overflow is the one opening the bag has.
```

Row 3 of that ledger entry is still open: the owner reported "big items still
stick out", and the standing decision is that a body too big for the bag is a
**design** answer (it is not bagged — see §7.2) rather than a geometry one.

### 7.4 The bag as a presentation object

`CHECKOUT_BAG_PRESENTATION` (line 439). Every number here is derived, and the
comment block records what each derivation replaced:

```js
pitch: -PI/2, roll: -PI/2          // maps bag +Y (mouth) → desk +X,
                                   // bag +Z (printed face + rope handle) → world UP,
                                   // bag +X (width) → desk +Z, toward the cashier
scale:       1.35                  // above life size on purpose; between the POS's
                                   // own 1.55 and the reader's 1.85 draw scales
flatten:     0.55                  // a laid paper bag's gusset collapses
counterLift: 0.116 * 0.55 * 1.35 + 0.003    // DERIVED half-thickness + 3 mm seat
itemRoll:    0
```

`counterLift` is derived rather than baked specifically because a previous size
change landed the bag's flank *through* the counter top, and the earlier baked
0.101 only passed on a 4 mm tolerance.

The interior liner is darkened by `applyKraftBagStyle` — when it was painted the
same kraft as the outside, the mouth read as a flat seam and a playtest called
the laid bag "a fallen box". **What must read is the opening.**

Bag anchors consumed from the GLB: `ANCHOR_BagContents` (contents volume centre),
`ANCHOR_BagHandoff` (falling back to `ANCHOR_BagHandleFront`),
`ANCHOR_BagHandleFront` / `ANCHOR_BagHandleBack`, `ANCHOR_BagDrop`,
`ANCHOR_ReceiptPocket`, `BAG_ITEM_SOCKET_01..n`, `BAG_PICKUP_SOCKET`.

### 7.5 There is no box-packing at checkout

Boxes (`src/sim/boxPlacement.js`, `deliveryBoxVisual.js`) are the **inbound**
delivery system, not checkout packaging. Do not port them as part of this.

---

## 8. CHECKOUT COUNTER SETUP

### 8.1 The frame

Everything on and around the desk is expressed in the **front-desk local frame**,
never in world coordinates. `src/data/shopLayout.js`:

```js
FRONT_DESK_FRAME = deriveFrontDeskFrame(CLUBHOUSE_LAYOUT_VARIANT)
   → { x, z, ry, frontLength, frontDepth, counterTop, counterWorkTop,
       returnCollisionWidth, returnStaffExtent }

frontDeskPoint(localX, localZ)      // local → world (yards)
frontDeskVector(localX, localZ)     // local → world direction
frontDeskLocalPoint(worldX, worldZ) // world → local
frontDeskPose(localX, localZ, localRy)
```

Key absolutes: `COUNTER_TOP = FRONT_DESK_FRAME.counterTop` (1.055 interior-local
y), `COUNTER_WORK_TOP` is 155 mm lower (the staff work surface), interior floor
`0.3`.

**A layout fact that bites:** `FRONT_DESK_FRAME.frontLength` is 4.2 m — that is
the greybox slab the colliders and the queue are measured against. But
`pineHillsV2Interior.mountHeroCounter` hides the slab and instantiates
`hero_counter` **raw at its authored size**, which is 2.388 m end to end.
Anything placed *on* the desk must fit inside
`HERO_COUNTER_DRAWN_HALF_LENGTH_M = 1.194`, not inside the slab. Two props (the
laptop and the ledger book) were placed against the slab and hung in mid air off
opposite ends.

### 8.2 `REGISTER` — every pose and rect

From `shopLayout.js:1248–1360`. All in the front-desk local frame.

**Devices (poses, seated on `COUNTER_TOP`):**

| Key | local x | local z | ry | Notes |
|---|---:|---:|---|---|
| `monitor` | 0.30 | 0.24 | 0 | POS. Placed at scale 1.0, presented at `POS_HARDWARE_SCALE = 1.55` |
| `cardterm` | 0.10 | −0.16 | 0 | card reader, reachable by **both** sides |
| `scanner` | −0.20 | 0.02 | π+0.22 | **suppressed in the simplified loop** |
| `printer` | 0.14 | 0.36 | π−0.06 | **data only** — no device is placed |
| `custdisplay` | 0.70 | −0.10 | π | turned toward the queue, scale 1.15 |
| `bag` | −1.05 | 0.06 | 0 | the carrier's **closed base** (model origin) |
| `bagstand` | 1.02 | 0.30 | 0 | spare folded carriers |
| `divider` | 0.98 | −0.15 | 0 | |
| `impulse` | 0.86 | −0.34 | 0 | |

**Drawer** (under the counter, below the POS, slides toward staff):

```js
drawer: { ...frontDeskPoint(0.52, depthHalf − 0.06),
          y: 0.86, w: 0.46, d: 0.40,
          travel: 0.44, travelX, travelZ }   // travel vector = frontDeskVector(0, 0.44)
```

Travel is 0.44, not 0.34: at 0.34 the bill row sat half-hidden under the top and
reads/clicks went to the coin row in front. The staff corridor keeps 0.71 yd with
it open (the player capsule is 0.68).

**Workspace rects:**

| Key | Rect (local) | Purpose |
|---|---|---|
| `staging` | x −0.74…−0.10, z −0.16…−0.01 | where the customer sets goods down (customer half, hugging the centre seam) |
| `scannedStaging` | x −1.28…−0.62, z −0.12…0.04 | scanned goods stay visible until payment completes |
| `bagging` | x −1.22…−0.82, z 0.02…0.26 | the laid carrier's footprint |
| `changeHandoff` | point(−0.10, 0.30), w 0.38, d 0.20 | flat counted-change pile, **left of and clear of the monitor** |
| `customerTender` | point(−0.38, −0.22), w 0.26, d 0.18 | where the customer's own cash lands, on their half |
| `scan` | x −0.48…0.08, z −0.18…0.24, y 1.06…1.34 | the swept scan volume |
| `stand` | `frontDeskPoint(0.00, 0.90)` | the cashier's standing datum |

These were **derived against two reach circles**, not eyeballed: the player
stands at (2.80, 5.10) and reaches 1.55 yd; the customer stands at the queue head
(1.60, 3.05) and leans 1.6 yd over the counter. `tests/checkout-space.test.js`
holds it open — the first cut put the staging tray a 1.68 yd stretch away and the
test caught it.

### 8.3 Counter asset paths

| Layer | Path |
|---|---|
| Blender source | `Assets/checkout/source/checkout_counter.blend` |
| Build script | `tools/blender/build_checkout_assets.py` (counter section ~line 950–1045) |
| Canonical GLB | `Assets/checkout/glb/checkout_counter.glb` (737,636 bytes) |
| Runtime GLB | `vendor/models/checkout/checkout_counter.glb` *(generated — see §18)* |
| Hero variant | `hero_counter` in the `pine-hills-v2` interior, mounted raw |

Authored counter anchors (from `build_checkout_assets.py`):

```
ANCHOR_POS              (-0.62, -0.12, 1.015)  equipment
ANCHOR_DrawerHousing    (-0.66, -0.39, 0.690)  equipment
ANCHOR_Scanner          ( 0.15,  0.020, 1.015) equipment
ANCHOR_CardReader       (-0.68,  0.290, 1.015) equipment, rot z = π
ANCHOR_ReceiptPrinter   ( 0.60, -0.20, 1.015)  equipment
ANCHOR_Bag              ( 1.02, -0.10, 1.015)  equipment
ANCHOR_Staging          (-0.10,  0.155, 1.025) surface, 0.78 × 0.40 m
ANCHOR_Bagging          ( 0.94, -0.120, 1.025) surface, 0.72 × 0.38 m
ANCHOR_StaffStand       ( 0.05, -1.05, 0)      standing_position
ANCHOR_CustomerStand    (-0.62,  1.05, 0)      standing_position, rot z = π
ANCHOR_StaffEntrance    ( 1.85, -0.50, 0)      opening, clear_width 0.90 m
```

Colliders are authored as meshes named `COL_*`; `merch.instantiateKit` hides any
node whose name starts with `COL_` and leaves it in the graph. Runtime navigation
authority is **analytic layout**, not GLB collision
(`runtimeNavigationAuthority: "ANALYTIC_LAYOUT"`, `activateGlbCollision: false`
in the asset manifests).

### 8.4 Materials / textures

The checkout kit ships with **baked materials, kept exactly as authored** — no
slot remap. `merch.js` line 456: "These are the finished hero assets for the
TCG-style register: baked materials are kept as authored (no slot remap),
collision proxies hidden."

Textures live in `Assets/checkout/textures/` (62 PNGs). The counter-relevant
ones: `CounterBlack.png`, `CreamPanel.png`, `OakSlat.png`, `BrushedAlu.png`,
`MatteBlackMetal.png`, `MidPlastic.png`, `CharcoalPlastic.png`, `TrayGray.png`,
`KraftPaper.png` (the bag), `BagArtwork.png`, `ReceiptPaper.png`,
`PaymentCard.png`, `Bill_{1,5,10,20,50}.png`, `Coin_{01,05,10,20,25,50}.png` and
their `_N` normal maps, `Coin_05_sheet01.png` + `_N`.

**Textures are embedded in the GLBs.** `gltfCache.js` interns them across files
via `internTextures` / `sharedTexturePool` so a clone shares decoded images. KTX2
is deliberately rejected (`TEXTURE_MEMORY_POLICY.md §3`) and the KTX2 loader is
left detached so a compressed asset throws a legible parse error rather than
failing inside a transcoder worker.

### 8.5 Moving parts on/around the counter

| Part | Node | Motion |
|---|---|---|
| Cash drawer tray | `CashDrawer_Tray` (kit) / `DrawerSlide` (legacy) | slides along local −Z; `DRAWER_OPEN_SPEED = 3.2`, `DRAWER_CLOSE_SPEED = 2.4`; authored clip `DrawerSlide_OpenHoldClose`, keys at frames 1/9/24/58/76 between `(0,0,0.020)` and `(0,−0.34,0.020)` |
| Bill retaining clips | `BillClipPivot_{denom}` | hinge on X; rest = paddle on slot floor, slerped up by `clipFillRatio(meta, count)` to ride the top of the note stack |
| Drawer latch | `DrawerLockPivot` / `DrawerLockTongue` | hinge on Z |
| Bag handles | `BagHandleFront` / `BagHandleBack` pivots | follow the carry grip on handoff |
| Card | `payment_card.glb` root | presented → grip → chip slot |
| Terminal | `payment_terminal.glb` root | parks in the device bay, **floats up to the player's face at working scale during card entry** |

### 8.6 The device bay (`CHECKOUT_TERMINAL_BAY`, line 351)

The counter's staff-facing front edge carries a dark-framed niche with a bright
white glowing back panel; the card reader plus a small pin pad park inside it.
Every number is measured, and the comments record the measurement:

```js
localX: -0.04, width: 0.56, height: 0.21,
reach: 0.19,        // 0.115 was too shallow to LEAN in; the parked reader sweeps 0.153
belowTop: 0.115,    // 0.145 fell almost entirely below the working frame's bottom edge
seatPitch: -0.32,   // at -0.5 the reader swept 0.161 in a 0.15 alcove, face 0.023 proud
parkScale: 0.42,    // full-size at rest towered out of the tray; grows back at the face
seatDepthFrac: 0.55,
pinPadOffsetX: 0.17
```

---

## 9. LAPTOP

**Important scoping note:** the laptop is *not* part of the checkout. It is the
management interface — the "Fairway Office" — and it lives on the same reception
worktop. Port it separately. It is included here because it was asked for and
because it shares the counter and the camera-seat pattern.

### 9.1 There is no laptop model file

The laptop is **built procedurally in three.js** at `src/render3d/clubhouse.js:2518–2630`
from the pure geometry in `src/core/laptopRig.js`. There is no `.glb`, no
`.blend`, no prefab.

(There *is* an unrelated asset called `asset_066_office_laptop_desk` — that is the
**desk**, not the machine. Paths: `asset_sources/blender/assets_51_100/sheet_07/asset_066_office_laptop_desk.blend`
→ `Assets/assets_51_100/glb/sheet_07/asset_066_office_laptop_desk.glb`
→ `vendor/models/assets_51_100/sheet_07/asset_066_office_laptop_desk.glb`.
Sockets: `SOCKET_Laptop`, `SOCKET_ChairPlacement`, `SOCKET_Lamp`,
`SOCKET_CableGrommet`, `SOCKET_OfficeProp_01`, `SOCKET_PLACEMENT`.
1.6 × 0.76 × 0.8 m, `runtimeScale: METERS_TO_YARDS`.)

### 9.2 The rig (`src/core/laptopRig.js`)

One convention, and everything follows: **local −Z is the seat.** The player sits
there and looks toward +Z. So `trackpad.z < keyboard.z < hingeZ`, and the open
screen's normal points back at −Z.

```js
LAPTOP = {
  deck:     { w: 0.390, d: 0.270, t: 0.020 },   // 14.0" × 9.7"
  lid:      { w: 0.390, d: 0.270, t: 0.014 },
  screen:   { w: 0.3628, h: 0.2268 },           // 15.4" diagonal, 16:10
  bezel:    0.013,
  hingeZ:   0.135, hingeY: 0.021,
  lidOpen:  1.87,                               // ~107°, reclined
  keyboard: { z: 0.035, w: 0.345, d: 0.125 },
  trackpad: { z: -0.088, w: 0.115, d: 0.072 },
  led:      { x: 0.168, z: -0.128 },
  foot:     { rTop: 0.006, rBottom: 0.007, h: 0.0035, y: 0.0002, inset: 0.022 },
  baseDrop: 0.0035/2 - 0.0002,
}
```

`baseDrop` exists because the group origin is the desk top as far as a caller is
concerned, but the lowest drawn point is the underside of the rubber feet
1.55 mm below it. A caller setting `position.y` to a surface height sinks the
laptop by exactly that. **That mismatch was the seating bug.**

Pure functions: `forwardLocal()`, `hingeAxisLocal()`, `lidTipLocal(angle)`,
`screenNormalLocal(angle)`, `screenCornersLocal(angle)` — the four glass corners
in the order a seated player reads them (TL, TR, BR, BL).

**The 16:10 is not cosmetic.** The interface is a 1024×640 DOM — exactly 16:10 —
mapped corner-to-corner onto the glass. Let the panel drift to 16:9 and every
glyph is stretched 11% wide.

### 9.3 Materials (all procedural)

- Body: `MeshStandardMaterial({color: 0x9aa1a8, roughness: 0.35, metalness: 0.75})`;
  dark variant `0x62676d`.
- Keyboard: a 280 × 104 `CanvasTexture` keycap grid (rows of 14/14/13/12/9 plus a
  spacebar), sRGB, inset into the deck.
- Trackpad: `0x83898f`, roughness 0.3, metalness 0.45.
- Feet: `0x1b1d20`, roughness 0.95.
- Screen: a `CanvasTexture` whose content is the screen-state machine below.

### 9.4 Screen logic

```
'off' → 'boot' → 'live'   (the DOM is projected on the glass)
              ↘ 'desk'    (nobody sitting: a lock screen — crest, club, clock)
```

- `'live'` paints **a flat sheet of the interface's own paper colour**, nothing
  else. This is deliberate: the canvas used to paint a full desktop (green
  wallpaper, Supplier / Pro Shop / Tee Sheet tiles) *while the real interface was
  projected on top* — two interfaces on one screen, readable through the gaps
  around the misaligned DOM, which is exactly why it read as a popup.
- `'desk'` is information, not navigation. Nothing on it to click.

**The alignment bug and its fix (`LAPTOP.md`).** `alignLaptopUi()` ran twice on a
`setTimeout` while the camera was still easing and the lid still swinging, so the
`matrix3d` always described where the screen *had been* — permanently one
alignment behind. **It aligns every frame now.** Four projections and a 3×3 solve
is nothing, and a transform that is never cached can never be stale. Seated
drift: 0 px on all four corners, at every interface scale.

**The lens.** Walk mode is 66° FOV. To fill 80% of the frame with a 15" panel
through a 66° lens you must sit 8 inches from it, at which point the keycaps are
enormous and the top bezel clips. The seated camera gets a **34° lens** and its
own near plane: same 80% coverage, eye lands 17.1 inches from the glass.
`tests/laptop-seat.test.js` pins both the 70–85% coverage band and the lens.

**Boot bar.** `BOOT_BAR_STORE_KEY = 'golfempire:laptop:open-ms'` in
`localStorage`, clamped to 220–9000 ms, nominal 480 ms, updated after every open
and remembered across sessions. One straight ramp covering 97%; the last 3% is
only claimed by `finishBoot()` when `main.js` says the interface is actually up.
Past the estimate it creeps (a bar that stops reads as hung) but through a small
enough remainder that it cannot become the old sprint-then-stall.

### 9.5 Scripts tied to it

| File | Role |
|---|---|
| `src/core/laptopRig.js` | pure geometry + screen-corner solve |
| `src/render3d/clubhouse.js` (2510–2930) | meshes, screen canvas, lid animation, `office.laptopObject`, `office.laptop`, screen-corner projection |
| `src/ui/laptop.js` (4082) | the whole Fairway Office DOM interface |
| `src/ui/laptopSearch.js` (269) | search index over its pages |
| `src/main.js` | per-frame `alignLaptopUi()`, seated camera, boot choreography |
| `LAPTOP.md`, `LAPTOP_UI_MANIFEST.md` | the design record |
| `tests/laptop-rig.test.js`, `tests/laptop-seat.test.js`, `tests/laptop-pages.test.js`, `tests/laptop-search-index.test.js` | |

Placement: `laptop.position.set(FRONT_DESK.laptop.x, COUNTER_WORK_TOP +
LAPTOP.baseDrop, FRONT_DESK.laptop.z)`, `rotation.y = FRONT_DESK.laptop.ry`.
It stands on the **staff work top**, 155 mm below the customer bar.
Visibility is gated by `facilityInstalled(state, 'laptop')`.

---

## 10. REGISTER / CARD READER / CASH DRAWER

All three are `.glb` files in the same kit. `merch.js:KIT` loads all of them into
`kit:<stem>` prototypes; `merch.instantiateKit(stem, {scale})` returns
`proto.clone(true)` with `castShadow = true`, `receiveShadow = false`, and any
`COL_*` node hidden.

### 10.1 POS monitor

| | |
|---|---|
| Blend | `Assets/checkout/source/pos_monitor.blend` |
| Canonical GLB | `Assets/checkout/glb/pos_monitor.glb` (701,780 B) |
| Runtime GLB | `vendor/models/checkout/pos_monitor.glb` |
| Placement | `placeKit('pos_monitor', REGISTER.monitor, {scale: 1.0})` at `COUNTER_TOP`, `rotation.y = 0` |
| Screen node | **`POS_Screen`** — a dedicated face with clean 0..1 UVs |
| Screen plane | `POS_PLANE_W = 0.34`, `POS_PLANE_H = 0.2125`, presented at `POS_HARDWARE_SCALE = 1.55` |

`registerMode.attachScreen(reg)` hangs its **own** canvas plane onto the
`POS_Screen` face. It does *not* assign a material to the kit mesh — the comment
in `fixtures.js` is explicit: "NOT `slotMesh(...).material = screenMaterial`".

Canvas: 1024 × 640 (`SCREEN_W`/`SCREEN_H`, matching
`FRONT_DESK_MONITOR_WIDTH/HEIGHT`).

### 10.2 Payment terminal / card reader

| | |
|---|---|
| Blend | `Assets/checkout/source/payment_terminal.blend` |
| Canonical GLB | `Assets/checkout/glb/payment_terminal.glb` (499,612 B) |
| Runtime GLB | `vendor/models/checkout/payment_terminal.glb` |
| Placement | `placeKit('payment_terminal', REGISTER.cardterm, {scale: 1.0})`; `registerMode.attachTerm(term)` |
| Authored width | 0.100 m |

**Physical keys are real meshes.** `checkoutTerminalKeyAction(name)` is the whole
mapping, kept pure so the driver, the press raycast and the tests read one table:

```js
/^(?:Terminal_|t_glyph_)Key_(\d)$/            → `digit:${n}`
/^(?:Terminal_|t_glyph_)(Confirm|Cancel|Back)Button$/
      Confirm → 'confirm'   Back → 'backspace'   Cancel → 'clear'
```

`CHECKOUT_TERMINAL_KEY_ROLES`: red **X** cancels the entry, yellow **⌫**
backspaces, green **OK** enters. The kit authored the confirm cap with a
seven-segment "O" (a second zero on the pad) and the backspace cap with a bare
"−", so **the runtime redraws those three faces**.

Terminal screen: its own canvas (`TERM_CANVAS_W/H`), with exactly one screen
affordance — the cancel-the-run **X** at `TERM_X_BOX = {x0: W−70, y0: 12, x1:
W−14, y1: 68}`. The on-screen keypad was removed on 2026-07-29; presses land on
the GLB meshes.

Busy indicator: `TERMINAL_BUSY_DOT_HZ = 3`, `terminalBusyDotPhase(elapsed) =
floor(elapsed*3) % 3`, advanced by `advanceTerminalBusyDots`.

Authored anchors: `ANCHOR_Screen` (0.068 × 0.044 m, pitched −9°),
`ANCHOR_Contactless`, `ANCHOR_ChipSlot` (clear width 0.096),
`ANCHOR_CardReady` (`insertion_u: 0.0`), `ANCHOR_CardInserted`
(`insertion_u: 1.0`), `ANCHOR_CardGrip`, plus kit sockets `CARD_INSERT_SOCKET`
and `NFC_TAP_SOCKET`.

Timings: `CARD_TIME = 1.15` s authorization, `CARD_INSERT_TIME = 0.72` s,
`AUTO_PAYMENT_HOLD = 0.38` s.

### 10.3 Cash drawer

| | |
|---|---|
| Blend | `Assets/checkout/source/cash_drawer.blend` |
| Canonical GLB | `Assets/checkout/glb/cash_drawer.glb` (992,640 B) |
| Runtime GLB | `vendor/models/checkout/cash_drawer.glb` |
| Placement | `model.position.set(0, -0.045, 0.10 - 0.41*kitScale)` inside `drawerGroup`, which sits at the counter's staff face |
| Slide node | **`CashDrawer_Tray`** (kit) or `DrawerSlide` (legacy build) |

**Money socket remap.** On load the runtime re-derives every denomination slot
from the kit's authored sockets so hotspots, labels and stacks land in the real
wells:

```js
BILLS  → `BILL_${denom}_SOCKET`
COINS  → `COIN_${cents2}_SOCKET`
0.25   → ['COIN_25_SOCKET', 'COIN_20_SOCKET']   // the quarter took over the 4th well
```

Each socket also carries its compartment's **placement contract** as authored
extras, scaled into world units once:

| Field | Bill default | Coin default |
|---|---|---|
| `well_w` | 0.070 | 0.070 |
| `well_d` | 0.250 | 0.185 |
| `wall_h` | 0.053 | 0.034 |
| `max_pieces` | 12 | 30 |
| `spacing` | 0.0019 | — |
| `hinge_drop` | 0.047 | — |
| `pile_h` | — | 0.0039 |
| `clip` | node name of the retaining clip | — |

Fallback slots (used only when the GLB has not loaded):
`SLOT[denom] = {x: -0.164 + i*0.082, y: 0.101|0.095, z: 0.095|-0.098}`.

Real-world footprints used for fitting (metres, pre-kit-scale):

```js
BILL_FOOTPRINT = { 1:[0.122,0.054], 5:[0.132,0.057], 10:[0.142,0.061],
                   20:[0.156,0.066], 50:[0.156,0.066] }
COIN_BLANK     = { 0.01:0.018, 0.05:0.021, 0.1:0.024, 0.25:0.026, 0.5:0.030 }
MONEY_KIT_SCALE = 1.0     // money is authored in exact real-world metres
```

Placement math is `drawerMoneyLayout.js`:
- `billFit(meta, len, wid)` → scale to 94% of interior depth × 92% of interior width.
- `billLayout(meta, count, denom)` → tidy stack with deterministic human jitter
  (±2 mm slide, ±0.04 rad skew) via `scramble(denom, i, salt)`, a `sin`-hash so
  a save/reload or a re-open never reshuffles the till behind the player.
- `clipFillRatio(meta, count)` → 0 = clip resting on the floor, 1 = level with
  its hinge.
- `coinLayout(meta, count, coinR, coinT, denom)` → golden-angle scattered mound,
  layers pulled toward the centre by 30% each, every piece clamped inside the
  well.
- `fillState(count, maxPieces)` → `empty | low | moderate | full` at 0 / 25% / 75%.

Drawer well labels are a **tested contract**, not a screenshot:
`checkoutDrawerSlotLabels()` returns `{bills: ['$1','$5','$10','$20','$50'],
coins: ['1¢','5¢','10¢','25¢','50¢']}`.

### 10.4 Money assets

`checkoutMoneyAssetStem(denom, from)`:

```js
BILLS      → `cash_bill_${denom}`
0.05 from 'tender' → 'cash_coin_05_sheet01'    // the larger Sheet-01 coin
otherwise  → `cash_coin_${pad2(denom*100)}`
```

Visual routing only — both five-unit variants keep the same logical denomination
in transaction and save data. The larger coin appears in incoming customer
tender; the smaller Sheet-02 coin stays in the drawer and in selected change.

Files: `cash_bill_{1,5,10,20,50}.glb`, `cash_coin_{01,05,10,20,25,50}.glb`,
`cash_coin_05_sheet01.glb`, `cash_handoff_stack.glb`.
`cash_coin_20.glb` stays loaded **for save-migration visuals only**.

GPU pre-warm: `checkoutMoneyGpuPrewarmStems`, `checkoutPaymentGpuPrewarmStems`,
`checkoutPaymentCardGpuPrewarmVariantIds`, `checkoutTexturePrewarmPlan`,
`cashGpuPrewarmReleaseReady` / `ShouldRelease`, `shouldPrewarmDrawerCoin`.

### 10.5 Other kit members touched by checkout

| Stem | Status |
|---|---|
| `barcode_scanner` | placed **only** for the legacy adapter (`B.register.simplified ? null : …`) |
| `receipt_printer` | loaded but **never placed** — round 7 removed the receipt |
| `loose_receipt` | loaded, unused in the shipping flow |
| `shopping_bag` | the paid carrier handed to the customer (scale 0.86) |
| `customer_display` | placed at scale 1.15, faces the queue |
| `scannable_product_box` | generic scannable prop |

Static batching: `batchRigidVisualsByPbrResponse(hardwareVisualRoot,
hardwareModels, {name: 'CheckoutHardwareRigidPbrBatch', excludeNames:
['Scanner_Window','Scanner_LED','Scanner_CashierLED']})`.

---

## 11. AUDIO AND VFX

### 11.1 Audio

`src/core/audio.js` — original procedural WebAudio design with sampled variants
where a recording exists. It **imports `BILLS` from `sim/register.js`** so the
register and the audio can never disagree about whether a 5 is paper or metal.
`paperOrCoin()` picks among a cue's variants by title regex.

Cue names actually fired by `simplifiedRegisterMode.js` (33):

```
stationEnter  stationLeave  uiTick  thunk  doorbell
productPickup  scanSuccess  scanInvalid  posAdd
cardTap  cardInsert  cardProcessing  cardApproved  cardDeclined  cardOut
cashPresent  cashPickup  cashRunStart  cashRunStop
billHandle  coinHandle  notesDown  coinsDown
drawerUnlock  drawerOpen  drawerOpenSequence  drawerClose
changeSelect  changeHandoff
keypadTap  bagItem  bagHandoff  checkoutComplete
```

The full declared set in `audio.js` also includes `scannerActivate`, `cardMove`,
`cardSwipe`, `billDeposit`, `coinDeposit`, `receiptPrint`, `receiptTear`,
`bagOpen`, `bagRustle`.

`registerFlow.js` additionally names the **intended** cue list per state as data
(e.g. `CashPresented: ['cash-count','cash-handoff']`, `DrawerOpening:
['drawer-unlock','drawer-open']`, `ReceiptPrinting: ['receipt-printer',
'paper-feed','paper-tear']`). Those are contract names, not function names; the
renderer maps them.

Design notes worth carrying:
- `scanBeep()` — dry A6 square pip (1760→1680 Hz, 75 ms, peak 0.028, LP 4200) with
  a quieter fifth above (2637→2489 Hz at +12 ms).
- The till has a **bell** on the sale, as a separate cue from the drawer slide,
  "because a real till rings on the sale, not on every drawer".
- `DRAWER_GAIN` was reduced after playtest 5 item 5: measured on
  `tools/qa/electron-money-cue-graph.js`, `drawerOpen` peaked at 0.592 and
  `drawerClose` at 0.591 while `checkoutComplete` — the sound that marks the
  outcome — was quieter. The loudest thing in the checkout was the furniture.
- `checkoutCueAllowed(name, minGap)` + `checkoutCueLastAt` debounce rapid repeats.

### 11.2 VFX

There is **no particle system and no post-effect specific to checkout.** The
"effects" are all material/geometry state:

- Hover: `setGrabOutline(mesh)` — a shell outline. Bright-green rim for grabbable
  payment (the customer's offered card/cash); brass box for counter goods.
- `setHoverCursor(bool)` and `showTip(text, event)` / `hideTip()`.
- Terminal busy dots (3 Hz), approval/decline screen states.
- The device bay's lit back panel (an emissive material).
- `activeRegisterGtaoOverride` — a scoped boolean that holds the player's exact
  prior GTAO setting on register entry and restores it on every exit, because the
  fixed close cameras make the whole active register the measured allocation
  hotspot. Workspace transitions must **not** recapture or release the scope.
- `suppressInteriorSunShadows(model)` on the drawer.
- `checkoutMoneyGpuPrewarm*` — shaders compiled before the drawer opens so the
  first coin does not hitch.

---

## 12. UI / PROMPTS / CAMERA

### 12.1 The POS screen (`frontDeskMonitorUi.js`)

1024 × 640 canvas, drawn every state change. `createFrontDeskMonitorUi(canvas)`
returns `{draw(model), hotspots(), ...}`.

Three tabs at y 92, height 52, width 184: **Check In** (x 24), **Checkout**
(x 224), **Tee Sheet** (x 424). Hotspot ids `tab-check-in`, `tab-checkout`,
`tab-tee-sheet`.

`STAGE_COPY` — headline + subline per POS state:

| posState | Headline | Subline |
|---|---|---|
| `waiting` | WAITING FOR CUSTOMER | The register is ready for the next transaction. |
| `products-ready` | PRODUCTS READY | Click each product to drop it in the bag. |
| `scanning` | BAGGING | Click each product to drop it in the bag. |
| `all-items-scanned` | ALL ITEMS SCANNED | The customer is confirming how they will pay. |
| `select-payment` | PAYMENT CONFIRMED | Opening the selected payment workspace automatically. |
| `card-payment` | CARD PAYMENT | Insert the customer card into the chip reader. |
| `cash-payment` | CASH PAYMENT | Click the presented cash to take it. |
| `change-selection` | SELECT CHANGE | Count change from the drawer: exact, or up to $5.00 over. |
| `payment-complete` | PAYMENT COMPLETE | Payment was accepted successfully. |
| `receipt-delivering` | RECEIPT TO CUSTOMER | The receipt is being handed across. |
| `bag-transfer` | BAG TO CUSTOMER | The customer is taking their bag. |
| `ready-to-finalize` | READY TO FINALIZE | The receipt and bag are on their way across the counter. |
| `complete` | TRANSACTION COMPLETE | The customer has been served. |

Palette (`COLORS`): cream `#f4eddb`, paper `#fffaf0`, green `#173f35`,
greenSoft `#28584a`, sage `#a8b9a4`, sagePale `#dce4d6`, charcoal `#272b29`,
muted `#667069`, brass `#b58a42`, brassPale `#e5d2a8`, white `#fffdf8`,
danger `#9b443d`, dangerPale `#efd8d2`, success `#2f7257`, successPale `#d6e8dc`,
line `#c8c7b8`.

The module keeps its own **audit** state — `MONITOR_TRUNCATIONS`,
`MONITOR_OVERLAPS`, `MONITOR_AUDIT_STATS`, `monitorAuditRectSnapshot()`,
`resetMonitorAudit()`. `tests/front-desk-monitor-overlaps.test.js` and
`front-desk-monitor-fits.test.js` run a rect-overlap sweep over every screen
**with its own planted-overlap control** — because a previous sweep passed while
all three checkout fixtures omitted `items`, so it only ever rendered "Waiting
for products" (`FOUND_FALSE.md` row: 2 appearances).

During the cash count the POS carries the orange Received / Total / Change /
Giving screen **directly above the open drawer**, and it carries the Undo / Clear
buttons. There are no stand-in panels — the real monitor stays present in every
workspace.

### 12.2 Camera

Four **workspaces**: `monitor`, `scan`, `card`, `cash`.
Three **pose keys** (`poseKey()`): `cash`, `checkin`, `overview`.

**The camera holds still.** Playtest 2026-07-30: "there is too much movement
going on… it makes the player dizzy." The fulfilment pan, the receipt close-up
and the card-terminal cut are all gone. Scanning, payment presentation, printing
and the handovers share **one working frame**; what needs attention comes *to*
the player (the reader lifts to the face for card entry). The only camera moves
left are the top-down drawer view and the check-in glass.

Eye and lens (all exported constants, all derived):

```js
CHECKOUT_STAFF_FLOOR_Y            = 0.30
CHECKOUT_STANDING_EYE_ABOVE_FLOOR = 1.62   // the walking player's own eye height
CHECKOUT_EYE_ABOVE_COUNTER        = 0.56   // what is actually authored
CHECKOUT_WORKING_EYE_Y            = COUNTER_TOP + 0.56
CHECKOUT_CUSTOMER_SHOULDER_Y      = 1.34
CHECKOUT_CUSTOMER_HANDS_Y         = 0.95
CHECKOUT_WORKING_FOV              = 54
CHECKOUT_WORKING_GLANCE_SCALE     = 0.34
CHECKOUT_CASH_GLANCE_SCALE        = 0.30
```

The history matters, because it explains why the eye is pinned to the *work*
rather than the *floor*: pinning it to the floor was right in principle and wrong
in this room — a standing eye 1.62 above the floor sits 0.865 above **this**
counter, whose top is only 0.755 off the staff floor (~0.69 m where a real shop
counter is 0.90–1.00 m). The playtest read that as "still too high up" even
though it was physically honest. Raising the desk would be the deeper fix and
would ripple through every fixture, collider and reach on it.

FOV went 48.5 → 54: at 48.5 the only way to fit ~2 yd of kit was from a yard and
a half back, and from there a standing eye sees the counter's front apron across
the bottom third instead of the counter *top*.

`checkoutLookScale(workspaceName, shiftKey)` — how much the cursor leans the
view:

```js
'scan' | 'monitor' → shiftKey ? 0.34 : 0     // still by default; Shift to glance
'cash'             → 0.30                    // barely leans: at full lean the POS
                                             // cash summary above the drawer left frame
otherwise          → 1
```

Derived poses, not authored ones:
- `derivedCheckinPose()` solves from the live screen quad — eye on the screen's
  forward normal at the centre's own height, standoff for `CHECKIN_FRAC_H = 0.60`
  frame share. The old authored pose sat the eye at 1.26 looking 14.5° **up** at
  the POS ("watching the screen from below the desk").
- The cash pose is derived from the **open drawer's bounding box**, walking a
  probe camera until every denomination label is readable. The authored preset
  sat the till in the frame's bottom-right where the labels went subpixel.
- `registerCameraPoses.js` supplies `cardHandoffPose`, `cardTerminalPose`,
  `fulfillmentHandoffPose` in the front-desk frame, so relocating or rotating
  reception cannot make a working camera look back at the former counter.

### 12.3 Prompts

`src/ui/registerGuidance.js` holds the player-facing hint strings.
`register.hint()` is read by `main.js:5654`. `register.checkoutStatus()` and
`register.checkoutInstruction()` expose the words on the screen so a check can
read them rather than screenshot them.

---

## 13. INPUT / CONTROLLER BEHAVIOUR

Mouse + keyboard only. There is no gamepad path.

### 13.1 Mouse

`onDown(event)`:

| Button | Context | Behaviour |
|---|---|---|
| **2 (right)** | `cash` workspace over an open drawer | `retractChangeFromSlot` — takes one of that well's denomination back off the counted pile |
| **2** | otherwise | `leave()` — **unless `cardTerminalLocked()`**, which would be a second way to abandon a running payment |
| **0 (left)** | `monitor` | POS hotspot → `handleMonitorAction`; else pick: item → `bagProduct`/`startBaggingProductDrag` (and swing to `scan`); bag → `startBagHandoffDrag`; else `handleCashPick` |
| **0** | `card` | terminal **X** → `cancelCardAtTerminal`; offered card at `card-ready` → `acceptPresentedCard`; terminal key mesh → `handleTerminalKey`; at `card-declined`, terminal body → `retryDeclinedCard` |
| **0** | `scan` | item → `bagProduct` |
| **0** | `cash` | POS Undo/Clear hotspot; bag → `startBagHandoffDrag`; else `handleCashPick` |

`onMove(event)` always calls `updateLookTarget(event)` — **the cursor's screen
position leans the head** (left edge looks left, top looks up), eased so it feels
like a neck. Then per workspace it sets the grab outline, the tooltip and the
hover cursor.

`onUp(event)` settles whichever drag is live: `money` → `settleTenderDrag`,
`bag` → `settleBagHandoff`, scanning `item` → `settleDraggedScan`, else
`settleBaggingProduct`.

`onWheel(deltaY, shiftKey)` → `rotateHeldProduct`.

### 13.2 Keyboard (`onKey(key)`)

| Key | Behaviour |
|---|---|
| `Escape` | If dragging → `recoverInput`. **If `cardTerminalLocked()` → swallowed** (the reader is modal; only its on-screen X leaves). Else back out one level: workspace → monitor → clear selected walk-in → clear selected reservation → tab home → `leave()` |
| `0`–`9` | at `card-entry` → `handleTerminalKey('digit:n')` |
| `Backspace` | at `card-entry` → backspace |
| `Enter` | at `card-entry` → confirm |
| `Z` / `z` | at `cash-drawer` && deposited → `undoLastChange()` |
| `Space` | at `cash-drawer` && deposited → `confirmChange()` (identical to the POS Done button) |

**There are deliberately no letter shortcuts for the cash flow.** `S` is a
walking key now that the player moves freely at the till; the presented pile takes
the click.

### 13.3 Pointer lock

`enter()` records `restorePointerLock = !!document.pointerLockElement`, exits
pointer lock, adds `document.body.classList.add('register-mode')`, and stashes
`previousFov`. `leave()` restores the FOV and re-requests pointer lock on a
`setTimeout(0)` if the document has focus, catching rejection silently (browsers
may require the next direct user gesture; `main.js` keeps click-to-look
available).

`enter()` also calls `B.setDirtReveal?.(0)` and `B.walk?.clearKeys?.()` — the
walk update stops while the till is up, so its per-frame zeroing cannot clear a
reveal lit at the moment of entry, and a keyup delivered while the walk is frozen
would otherwise be lost and relight the reveal on exit.

---

## 14. SAVE / PERSISTENCE

### 14.1 What lives in `state.shop`

From `initShop(state)` (`src/sim/shop.js:569`), checkout-relevant fields:

```js
held: []                          // [{uid, skuId}] — units in a shopper's hands
drawer                            // the persisted cash stack (added by state.js:397 / begin())
transactionHistory: []            // completed tickets, newest first
nextTransactionNo: 1              // durable monotonic ticket number
nextTransactionId: 1              // legacy direct-sale sequence
nextHeldId                        // anonymous held-unit sequence
salesLive: {units, revenue}
salesToday: {}                    // per-SKU units since last day close
salesWindow: []                   // last seven closed days
paymentBag: []                    // the balanced cash/card bag
paymentBagStats: {assigned, recent}
pendingCheckouts: {}              // THE WAL — present even when empty
checkoutSettlementReceipts: {}
checkoutSettlementReceiptKeys: []
checkoutProjectionIds: {}
pendingCheckoutsQuarantine        // {active, releasedBy, ...}
inventoryLifecycle                // lots + operation journal
customerSimulation                // active customers with their carts
log: []                           // flavour text, bounded to 8
```

Preferences live under `state.uiPrefs.checkout` so they travel with manual saves,
autosaves and empire snapshots.

`pendingCheckouts` is **intentionally present even when empty**, so load can
distinguish a clean journal from a missing or torn one.

### 14.2 The write-ahead settlement log

`src/sim/checkoutSettlement.js`. `CHECKOUT_SETTLEMENT_VERSION = 1`,
`MAX_PENDING_CHECKOUTS = 1` (absolute analytics and drawer targets assume one
register settlement owns the commit boundary at a time),
`MAX_SETTLEMENT_RECEIPTS = 2000`.

`preparePendingCheckout(state, plan)` persists, **before the first irreversible
write**:

```
settlementId, ticketNumber, ticketKey, alternateTicketKeys, ticketDraft,
inventory (the exact lot allocations), drawer {before, after},
postings[] (ledger specs), projections[] (sales/tax with before AND after),
outcomeSpec, reservationTarget
```

`reconcilePendingCheckout(state, settlementId)` then runs, in this exact order:

```
PREFLIGHT (every potentially failing projection, before any core mutation)
  validatePlan → inventory → drawer → ticket → postings → projections
  → outcome → reservationTarget → customerEvent → settlementReceipt
COMMIT (forward-only replay of operation-keyed writes)
  applyInventory → applyDrawer → applyPostings → applyProjections
  → recordOutcome → appendTicket
TAILS (each may return pendingTail: true and be retried later)
  applyReservationTarget → reconcileCustomerVisitEvents → applySettlementReceipt
CLEANUP
  delete projection operation keys; delete pending[settlementId]
```

Everything is keyed by an idempotency string, so a replay after a crash or a
reload finishes rather than duplicating:

```
checkout:<txId>:sale
checkout:<txId>:salestax
checkout:<txId>:cogs
checkout:<txId>:cash-over-short
checkout:<txId>:completed              (outcome)
checkout:<txId>:sales-projection
checkout:<txId>:tax-projection
checkout:<txId>:customer-visit
checkout-sale-batch:v2:{json}          (inventory)
```

### 14.3 The rule about history rows

Stated twice in the code, once in `checkout.js` and once in `register.js:1920`:

> Only the WAL can prove that stock, books, sales/tax projections, outcome and
> ticket crossed the same commit boundary. **A history row alone is a derived
> projection and may be forged or torn from those authorities.** Genuine
> interrupted work always retains its pending settlement until all tails
> complete; a row with no WAL is therefore a closed duplicate, never permission
> to mark a fresh transaction banked.

So `completeSale` on a transaction whose ticket already exists but whose WAL is
gone returns `{ok: false, already: true}` — it does **not** report success.

### 14.4 Quarantine

`checkoutWalIsQuarantined(state)`, `quarantineCheckoutWal(...)`,
`releaseCheckoutWalQuarantine(state, {acknowledgedBy})`,
`checkoutWalQuarantineAcknowledged(rawShop)`.

While quarantined: `checkoutSale` refuses, `recoverCheckout` refuses to return
held stock (it would double-credit against a settlement that still owns it), and
the player sees `t('checkout.integrityUnavailable')`.

The comment at line 56 records a real bug worth knowing before you port this: the
owner asked whether the load-time repair re-latches on every boot. **It did.**
`state.js:1961` discards the shop authorities and quarantines, while
`classifyCheckoutJournalCoherence` derives "unsafe" partly from the *ledger* — an
orphan bank posting with no replay checkpoint. The release rewrites
`shop.pendingCheckouts`; it cannot rewrite financial history. So the evidence
that trips the check outlives every repair. "The key turned, and the door
relocked." `checkoutWalQuarantineAcknowledged` is the one narrow thing a release
can leave behind that the next boot will believe.

### 14.5 Reload behaviour

`recoverCheckout(state)` is what a load does with `shop.held`:

- Skips UIDs owned by a **persisted customer's cart** (`customerSimulation.active`)
  — those are real simulation entities whose cart survives with them.
- Skips UIDs owned by a **pending settlement** — a prepared checkout is an
  irreversible commit decision; never return those exact goods to the shelf while
  the durable settlement still owns them.
- Everything else goes back: shelf first, back-stock on overflow.
- **Idempotent**: loading twice does not mint a second copy.
- No money moved, so none is unwound.

---

## 15. ERROR AND RECOVERY BEHAVIOUR

### 15.1 Customer leaves / abandons

`clubhouse.js:10798`: `if (register.getCustomer() === c) { register.abandon();
register.leave(); }`. `abandon()` calls `voidTx(tx)` if unbanked, clears physical
meshes, nulls `tx`/`cust`, returns to the monitor workspace.
`registerMode holds no authority over stock — the shelf is credited right here`
by `clubhouse.js` calling `returnToShelf`.

Queue patience: `queueGiveUp` + `createCustomerImpatientBeat(1.25 s)`.
`markCheckoutFailed(state, customer, reason)` records it on the customer.

Timeout: `WaitingForCashier` has a 180 s watchdog; there is also a separate
`waiting-customer-watchdog` at `clubhouse.js:10624` that leaves or recovers
input.

### 15.2 Interrupted payment

| Interruption | Handler |
|---|---|
| Terminal animation lost mid-authorization | `recoverUnresolvedCardAuthorization(tx)` → back to `card-present`, no approval invented, counters untouched. Refused once approved. |
| Player pulls the card before submit | `abandonCardBeforeSubmit(tx)` → back to `scanning`, basket intact. Refused from `card-busy` onward. |
| Cash animation fails after acceptance | `recoverCashAcceptedCheckpoint(tx, drawer)` → restores from `tx.acceptedTender` and `tx.drawerStart`, closes the visual drawer, replays. **Copies the persistent drawer, never mutates it.** Refused once `banked` or `receiptPrinted`. |
| Any state exceeds its timeout | `checkoutStateTimedOut(flow, nowMs)` → `recoverTimedOutCheckout` → `Recovery` with a resolved resume target |
| Recovery cannot reconcile | `abandonCheckoutRecovery(flow, {facts})` — allowed only when `!facts.paymentAuthorized` |

The renderer's watchdog (`checkoutWatchdogDiagnostics()`) reports
`managedStates`, `events`, `postBankFailures`, `postBankRecoveries`,
`cashRecoveryPending`.

### 15.3 Missing item

- `scanItem` on an unknown uid → `{ok: false, reason: 'That is not on this order.'}`
- `judgeBarcodeRead` codes: `missing`, `wrong-customer`, `duplicate`,
  `outside-zone`, `orientation`, each with a player-facing string in `BARCODE_MSG`.
- `checkoutSale` / `completeSale` refuse if any line is no longer in `shop.held`:
  "Every checkout item must still be held by this customer."
- `consumeHeldBatch` validates the **whole set before removing anything** —
  duplicate UID, ambiguous UID, SKU mismatch and incomplete lot provenance are all
  caught before the first splice. It then splices from the end so indices stay
  stable.

### 15.4 Reload mid-sale

Covered in §14.5. The invariant: **a transaction that is voided, abandoned, or
reloaded out from under us costs the player nothing and earns them nothing.**

### 15.5 Failed transaction / conflict results

`completeSale` and `checkoutSale` return typed failures, not exceptions:

```
{ok:false, reason}                       ordinary refusal, player-readable
{ok:false, conflict:true, ...}           identity belongs to a different ticket
{ok:false, duplicate:true, transactionId} already banked
{ok:false, already:true, ...}            ticket exists, WAL gone → closed duplicate
{ok:false, pending:true, ...}            a durable projection tail is outstanding
{ok:false, quarantined:true, ...}        the journal is quarantined
```

`diagnostic` carries the engineer-facing detail; `reason` is
`t('checkout.integrityUnavailable')` for everything the player should not be
asked to interpret.

QA fault injection is built in: `qaFaultAfterInventory`, `qaFaultAfterCoreCommit`
options on both, and `register.debugFailNextBankHelperReturn()`.

---

## 16. DEPENDENCIES ON GOLF-SPECIFIC SYSTEMS

Every coupling, and how deep it runs.

| # | Golf system | Where checkout touches it | Depth |
|---|---|---|---|
| 1 | **`state` object** (`src/sim/state.js`) | `state.shop`, `state.ledger`, `state.cash`, `state.clock.minutes`, `state.property.id`, `state.seed`, `state.mode`, `state.uiPrefs`, `state.salesTax` | **Total.** Every sim function takes `state` first. |
| 2 | **Ledger / economy** (`src/sim/economy.js`) | `preflightLedgerEntry`, `postLedgerEntry`, `preflightOutcome`, `recordOutcome`. Line keys: `shopSales`, `salesTax`, `costOfGoods`, `cashOverShort`, `greenFees` | **Total.** Checkout does not move money itself; it posts. |
| 3 | **Inventory lifecycle** (`src/sim/inventoryLifecycle.js`) | `INVENTORY_STAGE.{SHELF,CUSTOMER_HELD,SOLD,RESERVE}`, `moveInventory`, lot allocations, the operation journal | **Total.** Lot-level provenance is a hard requirement of the WAL. |
| 4 | **SKU catalog** (`src/data/shopItems.js`) | `skuById(id).{name, cost, msrp, cat, tier, lb, fragile, form}` | High — `cost` drives COGS, `cat`/`lb` drive carry category. |
| 5 | **Shop pricing** (`src/sim/shop.js`) | `priceFor(sku, markup, memberTier)`, `shelfCapacity`, `skuDisplayIsPlaced` | Medium. |
| 6 | **Sales tax** (`src/sim/salesTax.js`) | `salesTaxRate(state)`, `salesTaxOn`, `SALES_TAX_LINE`, `ensureSalesTax`, `taxJurisdictionLabel` | High — the rate is frozen onto the ticket at creation. |
| 7 | **Customer identity** (`src/sim/customerIdentity.js`) | `preflightCustomerVisitEvent`, `reconcileCustomerVisitEvents` | Medium — a visit event rides the ticket as a tail. |
| 8 | **Customer simulation** (`src/sim/customerSimulation.js`) | who is at the counter, the queue, the cart, patience | High. |
| 9 | **Reservations / tee times** (`src/sim/reservations.js`, `reservationCheckIn.js`) | `service:green-fee` lines on a retail ticket; `dueForCheckIn`, `daySheet`, `fmtSlot` | **Golf-only.** Delete or replace wholesale. |
| 10 | **Shop layout** (`src/data/shopLayout.js`) | every pose, rect, queue slot and frame | Total, but it is pure data. |
| 11 | **Clubhouse variant system** | `pine-hills-v2` selects the layout, the hero counter, the queue geometry | Golf-only. |
| 12 | **i18n** (`src/core/i18n.js`) | `t('checkout.integrityUnavailable')`, `t('checkout.*')`, `t('customer.historyUnavailable')`, `t('ledger.integrityUnavailable')` | Low. |
| 13 | **`merch` asset service** (`clubhouse/merch.js`) | `instantiateKit`, `instantiate`, `onReady`, `bake`, `hasKit` | Total, on the renderer side. |
| 14 | **Character asset** (`characterAsset.js`) | `setMode`, `hand`, `carryGrip` | High, on the renderer side. |
| 15 | **Audio** (`src/core/audio.js`) | 33 cue names | Medium. |
| 16 | **Fault guard** (`src/core/faultGuard.js`) | `reportFault` | Low. |
| 17 | **Tutorial** (`src/sim/tutorial.js`) | `triggerContextTutorial(state, 'checkout')`, `tutorialFlag(state, 'checkoutCompleted')` | Low. |
| 18 | **Reviews / reputation** | `leaveReview(c, true)` on paid | Low. |
| 19 | **Shop progression / tiers** | `unlockedTier` gates which SKUs exist | Low. |
| 20 | **Golf domain vocabulary throughout** | `greenFees`, `teeTime`, `clubhouse`, `proShop`, `golfer`, `caddie`, SKU ids like `driver1`, `balls2` | Cosmetic but pervasive — rename at the data layer, not in logic. |

---

## 17. WHAT THE GEODE VERSION MUST PROVIDE FOR EACH DEPENDENCY

One row per §16 item. This is the interface list to build first.

| # | Geode must provide | Minimum contract |
|---|---|---|
| 1 | `GameState` root (a serializable C# object graph, **not** MonoBehaviours) | `Shop`, `Ledger`, `decimal Cash`, `Clock.Minutes`, `Property.Id`, `int Seed`, `DifficultyMode`, `UiPrefs`, `SalesTax`. Must round-trip through JSON identically — the WAL depends on it. |
| 2 | `ILedger` | `Preflight(LedgerEntrySpec) → PreflightResult`, `Post(spec) → Entry`, `PreflightOutcome`, `RecordOutcome`. **Idempotency by `idempotencyKey` is mandatory**, not optional. Line keys become Geode's own: `retailSales`, `salesTax`, `costOfGoods`, `cashOverShort`, and whatever replaces `greenFees` (appraisals? cutting jobs?). |
| 3 | `IInventoryLifecycle` | Lot-level stock with stages `Shelf`, `CustomerHeld`, `Sold`, `Reserve`, and `Move(from, to, sku, qty, allocations, referenceId, reason)` that is **idempotent on `referenceId`** and returns the exact lot allocations it consumed. Without lot allocations the WAL's conservation checks cannot be ported. |
| 4 | `ISpecimenCatalog` (Geode's `MineralCatalog` / `SpecimenAssetLibrary` already exist) | `ById(id) → {Name, Cost, Msrp, Category, Tier, WeightLb, Fragile, Form}`. `Cost` is required for COGS. |
| 5 | `IPricing` | `PriceFor(sku, markup, memberTier) → decimal`, `ShelfCapacity(sku)`, `DisplayIsPlaced(sku)`. |
| 6 | `ISalesTax` | `Rate(state) → decimal`, `TaxOn(amount, rate) → decimal` (must round identically), a `SalesTaxLine` key, and a liability accumulator `{Collected, Owed, TaxableSales}`. |
| 7 | `ICustomerHistory` | `PreflightVisitEvent(spec)`, `ReconcileVisitEvents(tickets)`. Can be a no-op stub for v1 — the ticket carries `customerVisitEvent` + `customerVisitRecorded: false` and the tail simply never runs. |
| 8 | `ICustomerSimulation` | Serializable customer entities with an explicit state enum, a cart of `{uid, skuId, price}`, queue index, patience clocks, and `MarkCheckoutStarted/Failed/Completed`. Geode's `DealerIntercom` / `ReceivingArea` are the nearest existing shapes. |
| 9 | **Service lines — replace, do not port.** | Keep the *mechanism* (`SERVICE_LINE_PREFIX = "service:"`, untaxed, no COGS, not baggable, own revenue line, exactly one booking per ticket) and give it a Geode meaning — a commissioned cut, an appraisal fee, a consignment payout. If Geode has no such concept in v1, keep the prefix logic and ship zero service SKUs; `goodsLinesOf`/`serviceLinesOf` then degenerate safely. |
| 10 | `CounterLayout` ScriptableObject | The direct analogue of `REGISTER`. Author it as a `ScriptableObject` with the same fields (device poses, workspace rects, drawer travel) expressed in a **counter-local frame** with the same `LocalToWorld` / `WorldToLocal` helpers. Do **not** bake world coordinates. |
| 11 | Variant system | Optional. If Geode has one shop layout, delete the variant indirection and keep a single `CounterLayout` asset. |
| 12 | `ILocalization` | `T(key)` returning a string. A `Dictionary<string,string>` is enough. |
| 13 | **Prefab library** replaces `merch` | `IPropLibrary.Instantiate(string stem, float scale) → GameObject`, backed by `Addressables` or a `ScriptableObject` registry mapping stem → prefab. Must hide any child named `COL_*`, set `castShadow`, and support an `OnReady` callback for deferred loads. |
| 14 | `ICustomerRig` | `SetMode(CustomerMode)`, `Hand(Side) → Transform`, `CarryGrip(Side) → Transform`. Geode may use a real rigged humanoid + Animator (Golf could not, for exporter reasons that do not apply to Unity). **The carry grip must be a scale-independent sibling of the hand bone** — that is not an implementation detail, it is what keeps the bag vertical. |
| 15 | `IAudio` | `Play(string cue)` over the 33 cue names. A `ScriptableObject` cue table with per-cue variants and a min-gap debounce reproduces `checkoutCueAllowed`. |
| 16 | `IFaultReporter` | `Report(string, Exception)`. |
| 17–19 | Tutorial / review / progression hooks | All optional callbacks. Make them `Action?` fields the register invokes defensively (Golf wraps every one in `try {} catch {}` — a broken flourish must never strand a durable ticket). |
| 20 | Naming | Rename at the data layer only. Every identifier in the sim modules should be domain-neutral in Geode: `RetailTicket`, `CounterTransaction`, `SpecimenSku`. |

---

## 18. EXACT ASSET DEPENDENCY GRAPH

### 18.1 The three-stage pipeline

```
tools/blender/build_checkout_assets.py    (or build_checkout_kit.py,
tools/blender/build_checkout_products.py   build_checkout_kit.py for the TCG kit)
        │  blender --background --python <script>
        ▼
Assets/checkout/source/<stem>.blend        ← authored source, git-lfs tracked
        │  (the same script exports)
        ▼
Assets/checkout/glb/<stem>.glb             ← CANONICAL, git-lfs tracked
        │  node tools/build-vendor-models.mjs
        │  driven by tools/vendor-models.manifest.json  {from, to}
        ▼
vendor/models/checkout/<stem>.glb          ← RUNTIME, **gitignored / generated**
        │  merch.js: loader.load(`vendor/models/checkout/${name}.glb`)
        ▼
protos.set(`kit:${name}`, gltf.scene)      ← the "prefab"
        │  merch.instantiateKit(name, {scale})  →  proto.clone(true)
        ▼
scene graph
```

**`vendor/models/` files listed in `tools/vendor-models.manifest.json` are
GENERATED.** Edit the `Assets/` source, never the vendor copy. The other 413
files in `vendor/models/` are direct Blender exports and *are* tracked. Run
`node tools/build-vendor-models.mjs` after pulling; `--check` is part of
`npm run gate`.

### 18.2 The complete required file list for checkout

Copy **all** of these or the register will silently lose props (a missing model
does not wedge the shop — `merch.js` swallows the load error — it just does not
show, which is the worst possible failure mode for a port).

**Kit GLBs — `Assets/checkout/glb/` → `vendor/models/checkout/`**

*Required by the register itself:*
```
checkout_counter.glb        pos_monitor.glb          payment_terminal.glb
cash_drawer.glb             payment_card.glb         shopping_bag.glb
customer_display.glb        barcode_scanner.glb      receipt_printer.glb
loose_receipt.glb           scannable_product_box.glb
cash_bill_1.glb   cash_bill_5.glb   cash_bill_10.glb
cash_bill_20.glb  cash_bill_50.glb
cash_coin_01.glb  cash_coin_05.glb  cash_coin_05_sheet01.glb
cash_coin_10.glb  cash_coin_20.glb  cash_coin_25.glb  cash_coin_50.glb
cash_handoff_stack.glb
```

*Loaded by the same `KIT` list (retail fixtures — needed only if you port the
shop floor too):* `apparel_wall.glb`, `apparel_wall_display.glb`, `hat_wall.glb`,
`accessory_slatwall.glb`, `club_rack.glb`, `putter_rack.glb`, `bag_display.glb`,
`shoe_wall.glb`, `ball_shelf.glb`, `snack_shelf.glb`, `rangefinder_display.glb`,
`merch_table.glb`, `retail_gondola.glb`, `apparel_table.glb`,
`stock_shelving.glb`, `storage_tote_{olive,slate,charcoal,stone}.glb`,
`lounge_armchair.glb`, `lounge_coffee_table.glb`, `lounge_side_table.glb`,
`office_desk.glb`, `office_chair.glb`, `filing_cabinet.glb`.

**Product GLBs — `vendor/models/clubhouse/`** — one per `VISUALS` row that names
a `model`. Verified against disk:

```
checkout_product_driver          checkout_product_iron_set
checkout_product_putter          checkout_product_wedge
checkout_product_ball_carton     checkout_product_glove
checkout_product_folded_polo     checkout_product_folded_bottom
checkout_product_folded_jacket   checkout_product_cap
checkout_product_visor           checkout_product_sock_pair
checkout_product_shoe_box        checkout_product_shoe_pair
checkout_product_tee_pouch       checkout_product_towel_roll
checkout_product_marker_blister  checkout_product_divot_tool_card
checkout_product_rangefinder     checkout_product_eyewear_case
checkout_product_bottle          checkout_product_beverage_can
checkout_product_snack_pouch     checkout_product_snack_bar
checkout_product_scorecard       checkout_product_umbrella
checkout_product_stand_bag
provisions_fairway_spring_water  provisions_bunker_bites_chips
```

Also present on disk but **not** referenced by any live `VISUALS` row:
`checkout_product_hanging_polo`, `checkout_product_hanging_jacket`,
`checkout_product_headcover`, `checkout_product_staging_tray`.
The staging tray and the change-handoff tray are **deleted from the flow** —
nothing instantiates or bakes them (`fixtures.js:1973`).

Freight-only descriptors (never in a customer basket, needed only if you port
deliveries): `delivery_fixture_product_{vacuum,plant,poster,events_board,pendant}`,
`packed_product_rug1`, `packed_product_lounge1`.

**Textures — `Assets/checkout/textures/` (62 PNGs, embedded in the GLBs).**
The checkout-critical ones: `Bill_{1,5,10,20,50}.png`,
`Coin_{01,05,10,20,25,50}.png` + `_N`, `Coin_05_sheet01.png` + `_N`,
`PaymentCard.png`, `KraftPaper.png`, `BagArtwork.png`, `ReceiptPaper.png`,
`CounterBlack.png`, `CreamPanel.png`, `BrushedAlu.png`, `MatteBlackMetal.png`,
`MidPlastic.png`, `CharcoalPlastic.png`, `TrayGray.png`, `OakSlat.png`,
`BoxBarcode.png`, `ProductBoxAtlas.png`, `ShoeBoxArt.png`, `OpticBox.png`.

**Blender sources — `Assets/checkout/source/*.blend`** (51 files, one per GLB
plus `checkout_assembled_preview.blend`).

**Build scripts** — `tools/blender/build_checkout_assets.py`,
`build_checkout_kit.py`, `build_checkout_products.py`,
`assemble_checkout_preview.py`.

**Previews** — `Assets/checkout/previews/*.png` (reference contact sheets).

### 18.3 The node names are the real contract

Because there are no GUIDs, **the string names of nodes inside each GLB are what
the runtime binds to.** These must survive the port, either as GameObject names
or as an explicit serialized reference on a component. Complete list of names the
runtime looks up by string:

| GLB | Node names read at runtime |
|---|---|
| `pos_monitor` | `POS_Screen` |
| `payment_terminal` | `Terminal_Key_0`…`Terminal_Key_9`, `Terminal_ConfirmButton`, `Terminal_CancelButton`, `Terminal_BackButton` (and the `t_glyph_` prefixed variants), `CARD_INSERT_SOCKET`, `NFC_TAP_SOCKET`, `ANCHOR_Screen`, `ANCHOR_ChipSlot`, `ANCHOR_CardReady`, `ANCHOR_CardInserted`, `ANCHOR_CardGrip`, `ANCHOR_Contactless` |
| `cash_drawer` | `CashDrawer_Tray` (or legacy `DrawerSlide`), `BILL_{1,5,10,20,50}_SOCKET`, `COIN_{01,05,10,25,50}_SOCKET` (+ legacy `COIN_20_SOCKET`), each socket's `userData` extras (`well_w`, `well_d`, `wall_h`, `max_pieces`, `spacing_m`, `hinge_drop_m`, `pile_h_m`, `clip`), the clip node named by `clip`, `ANCHOR_DrawerClosed`, `ANCHOR_DrawerOpen`, `ANCHOR_DrawerPull`, `ANCHOR_CashDeposit`, `COL_DrawerHousing`, `COL_DrawerSlide` |
| `shopping_bag` | `ANCHOR_BagContents`, `ANCHOR_BagHandoff`, `ANCHOR_BagHandleFront`, `ANCHOR_BagHandleBack`, `ANCHOR_BagDrop`, `ANCHOR_ReceiptPocket`, `BAG_ITEM_SOCKET_01…`, `BAG_PICKUP_SOCKET` |
| `barcode_scanner` | `Scanner_Window`, `Scanner_LED`, `Scanner_CashierLED`, `ANCHOR_ScanZone`, `ANCHOR_ScanNormal`, `ANCHOR_BeamEmitter`, `ANCHOR_ScannerGrip` |
| `receipt_printer` | `ANCHOR_PaperRoll`, `ANCHOR_ReceiptFeed`, `ANCHOR_Tear`, `ANCHOR_ReceiptPickup`, `RECEIPT_OUTPUT_SOCKET`, `RECEIPT_TEAR_SOCKET`, `RECEIPT_PICKUP_SOCKET` |
| `checkout_counter` | the 11 `ANCHOR_*` listed in §8.3, `COL_*` |
| product GLBs | `ANCHOR_ProductBarcode` or `BARCODE_AREA`, grip anchors, `PRODUCT_GRAB_SOCKET` |
| any | prefix `COL_` → hidden collision proxy |

### 18.4 The manifest

`Assets/MANIFEST.md`, generated by `npm run assets:manifest`, **never
hand-edited**. Every asset the game may load, with `SHIPPING` / `NOT WIRED` /
`SUPERSEDED` derived from the loaders in `src/`. Read it before picking an asset
by filename — three hero versions once held the same ten names and nothing on
disk said which was live.

`Assets/_archive/` holds dead assets; `Assets/_archive/ARCHIVED.json` declares
them, and referencing either their archived path **or their former path** fails
the suite (`tests/archived-assets-are-not-referenced.test.js`).

---

## 19. `.meta` FILES AND GUID RELATIONSHIPS

**There are none in Golf Empire.** Zero `.meta` files, zero GUIDs.

What plays the same role, and what you must therefore construct on the Unity
side:

| Golf mechanism | What it guarantees | Unity equivalent you must build |
|---|---|---|
| Literal path string in `merch.js:KIT` and `loader.load('vendor/models/checkout/<stem>.glb')` | which file loads | An `AssetReference` / Addressables key, or a `ScriptableObject` registry mapping `stem → prefab`. **Do not rely on `Resources.Load` by path** — that reintroduces exactly the fragility Golf documents. |
| `tools/vendor-models.manifest.json` `{from, to}` pairs | which canonical file becomes which runtime file | An Editor import step, or simply commit the FBX/prefab once and drop the two-stage pipeline. |
| `Assets/MANIFEST.md` + `npm run assets:check` | which of several same-named copies is live | A generated `AssetManifest.asset` + an EditMode test that fails when a prefab is referenced but not registered. |
| `Assets/_archive/ARCHIVED.json` + `tests/archived-assets-are-not-referenced.test.js` | dead assets stay dead | An EditMode test scanning for references to a deny-list of GUIDs. |
| **Node names inside the GLB** (§18.3) | the runtime can find `POS_Screen`, `BILL_20_SOCKET`, … | This is the one that genuinely maps onto GUIDs. **Do not keep string lookups in Unity.** Author a `CheckoutRigReferences` MonoBehaviour on each prefab with serialized `Transform` fields for every anchor and socket, populated once at import time by a `GeodeModelPostprocessor` (Geode already has `Assets/GeodeEmpire/Scripts/Editor/GeodeModelPostprocessor.cs`). Then the GUID + fileID of those references becomes the durable binding, and a renamed node breaks at import instead of silently at runtime. |
| Socket `userData` extras on drawer sockets | the well placement contract | Serialize a `DrawerWellContract` struct on the same component (`wellWidth`, `wellDepth`, `wallHeight`, `maxPieces`, `spacing`, `hingeDrop`, `pileHeight`, `clip: Transform`). |
| `git-lfs` on `*.glb`, `*.blend`, `*.hdr`, `*.exr`, `Assets` images (`.gitattributes`) | the repo stays clonable | Same — Geode should LFS-track `*.fbx`, `*.blend`, and large textures. Golf's history was **not** rewritten; LFS is forward-only. |

**The one hard rule:** when you import the GLBs into Unity, import each one once,
let Unity mint its `.meta`, and **never move or rename a file afterwards outside
the Editor.** Golf's path-string binding tolerates a move (fix the string);
Unity's GUID binding does not tolerate a move that loses the `.meta`.

---

## 20. TESTS AND HARNESSES

### 20.1 The gate

```
npm run gate
```
= lint ratchet (frozen at the owner-reviewed baseline, shrink-only)
→ asset-manifest check → vendor-models build check
→ full suite (528 test files, includes the glTF validation gate over all runtime GLBs)
→ golden-image capture + diff (Electron, `pine-hills-v2`)
→ the golden one-pixel control.

Individual pieces:
```
npm run lint
npm test                                   # node --test over tests/
npm run golden                             # capture + diff
npm run golden:control                     # the planted-difference control
npm run golden:accept                      # rebaseline after an INTENDED visual change
npm run assets:check
node tools/build-vendor-models.mjs --check
node tools/validate-gltf.mjs <path>
```

### 20.2 Headless domain tests (`node --test`)

The checkout-relevant files, grouped:

**Transaction core**
`checkout.test.js`, `register-flow.test.js`, `register-complete.test.js`,
`register-integrity.test.js`, `checkout-atomicity.test.js`,
`checkout-payment.test.js`, `checkout-economic-binding.test.js`,
`one-visit-one-payment.test.js`, `sales-tax.test.js`

**Cash**
`register-cash-confirmation.test.js`, `register-cash-timing.test.js`,
`register-change-tolerance.test.js`, `register-money-visuals.test.js`,
`money-sounds.test.js`, `checkout-workspace-trays.test.js`

**Card**
`register-card-abort.test.js`, `reader-physical-keys.test.js`,
`checkout-payment-presentation.test.js`, `pay-gestures-differ.test.js`

**Settlement / persistence**
`checkout-settlement-authority.test.js`, `checkout-settlement-recovery.test.js`,
`checkout-direct-settlement-recovery.test.js`,
`checkout-wal-quarantine-release.test.js`, `checkout-inventory-replay.test.js`,
`empire-checkout-persistence.test.js`, `save-stability.test.js`,
`inventory-conservation.test.js`, `customer-visit-publication-atomicity.test.js`

**Recovery**
`customer-checkout-recovery.test.js`, `register-abandon.test.js`,
`register-watchdog-recovery.test.js`, `register-lifecycle-stress.test.js`,
`checkout-scan-presentation.test.js`

**Customer / queue**
`customer-simulation.test.js`, `customer-checkout-flow.test.js`,
`customer-basket.test.js`, `walk-in-queue-truth.test.js`,
`staff-pass-through.test.js`

**Bag / packaging**
`bag-drop-nothing-shrinks.test.js`, `bag-leaves-in-their-hand.test.js`,
`checkout-bag-does-not-block.test.js`, `counter-goods-clear-the-bag.test.js`,
`customer-paid-bag.test.js`, `carried-goods-visual.test.js`,
`counter-item-overlap.test.js`

**Space / layout**
`checkout-space.test.js`, `pine-hills-v2-layout.test.js`,
`prop-collision-contract.test.js`, `register-camera-poses.test.js`,
`register-placement-preview.test.js`

**Screen**
`front-desk-monitor-ui.test.js`, `front-desk-monitor-fits.test.js`,
`front-desk-monitor-overlaps.test.js`, `checkout-display-brand.test.js`,
`no-price-tags.test.js`

**Assets**
`checkout-kit-assets.test.js`, `checkout-kit-runtime-mirror.test.js`,
`gltf-validation-gate.test.js`, `proshop-assets.test.js`,
`shared-texture-assets.test.js`, `register-checkout-texture-prewarm.test.js`

**Preferences / accessibility**
`checkout-preferences.test.js`,
`simplified-register-recovery-accessibility-driver.test.js`

### 20.3 In-game (Electron) QA drivers

The rule from `CLAUDE.md`: **Electron only for game evidence.**

```bash
node tools/qa/run-electron.cjs <driver> --clubhouse=pine-hills-v2
VIDEO_DIR=qa/<name> node tools/qa/run-electron.cjs <driver> --clubhouse=pine-hills-v2
```

Then extract frames with the ffmpeg tile pattern in `tools/qa/clip-frames.mjs`
and **look at the frames**. Never report a number about a clip you have not
looked at.

Key checkout drivers:
```
tools/qa/simplified-register-acceptance.mjs        end-to-end sale
tools/qa/simplified-register-queue-acceptance.mjs  queue drains
tools/qa/simplified-register-save-reload.mjs       reload matrix
tools/qa/simplified-register-lifecycle-stress.mjs
tools/qa/simplified-register-product-matrix.mjs    every SKU through the till
tools/qa/simplified-register-performance.mjs
tools/qa/simplified-register-recovery-accessibility.mjs
tools/qa/register-acceptance-driver.mjs
tools/qa/drawer-end-to-end.js                      drawer open→deposit→change→close
tools/qa/checkout-bag-handoff-path.js
tools/qa/checkout-card-lockout.js                  the reader is modal
tools/qa/checkout-card-spike-probe.js
tools/qa/checkout-reader-geometry.js
tools/qa/checkout-queue-exodus.js
tools/qa/checkout-monitor-layout.js
tools/qa/checkout-terminal-canvas-hotpath.js
tools/qa/electron-b2-one-visit-one-payment.js
tools/qa/electron-b3-queue-drains.js
tools/qa/electron-g7-cash-gesture.js
tools/qa/electron-e2-card-pinch.js
tools/qa/electron-money-cue-graph.js               audio levels, measured
tools/qa/bag-presentation-shots.js                 bag size vs POS vs reader
tools/qa/steam-release-checkout-performance.js
```

### 20.4 The QA discipline (`.claude/skills/golf-qa/`) — carry this over

- **Every instrument gets a negative control.** A green suite is not evidence.
- An invariant that does not launch the game certifies nothing.
- **Every fix gets a check you have watched FAIL on the unfixed build.**
- Before claiming a previously-found-false item done, write down what the new
  check measures and how it differs from the check that passed last time. If it
  is a number of the same kind, it is not a new check.
- For anything that **moves**, a clip is the standard.

The eight failure shapes in `Designs/ProShop/FOUND_FALSE.md` (two populations,
zero call sites, right object wrong variable, two selectors, shipped disabled,
visible but not painted, wrong runtime, counted the numerator) are the checklist
to run a Geode check against before believing it.

---

## 21. KNOWN BUGS AND EDGE CASES

Carried from `KNOWN_ISSUES.md`, `FOUND_FALSE.md`, and inline code comments.

### 21.1 Open

| # | Issue | Status |
|---|---|---|
| 1 | **Oversize goods still visibly stick out of the bag.** `FOUND_FALSE.md` "The bag" row 3. The standing decision is that a body too big for the bag is a *design* answer (§7.2 — it is not bagged) rather than a geometry one. Goal 22 has not re-measured. | Open, by decision |
| 2 | **The WAL quarantine can re-latch on every boot.** The load-time repair discards shop authorities and quarantines, while the coherence classifier derives "unsafe" partly from the *ledger*. A release rewrites `shop.pendingCheckouts` but cannot rewrite financial history, so the tripping evidence outlives every repair. `checkoutWalQuarantineAcknowledged` is the narrow fix; probe at `tools/qa/node/p0-relatch-probe.mjs`. | Mitigated, not eliminated |
| 3 | `DECLINE_CHANCE = 0` — **card declines never occur in normal play.** The whole `CardDeclined` branch, `retryCard`, `cardsTried` and the decline audio are reachable only through `force`. | Deliberate; decide for Geode |
| 4 | **The scanner and printer are loaded but not placed.** `barcode_scanner` only for the legacy adapter; `receipt_printer` never. `REGISTER.printer` remains as data because the placeable catalog's socket map and the frame tests key off it. Dead weight in a port. | Deliberate |
| 5 | **`cash_coin_20.glb` is a live load for save-migration visuals only.** The denomination does not exist. | Deliberate |
| 6 | **`cashierHands.js` is fully implemented and fully suppressed.** 277 lines, 22 poses, never rendered in the shipping loop. | Deliberate |
| 7 | The hero counter's drawn length (2.388 m) disagrees with the collider slab (4.2 m). Props authored against the slab hang in mid-air. | Documented, guarded by `HERO_COUNTER_DRAWN_HALF_LENGTH_M` |
| 8 | `state.js` load repair may reset `shop.pendingCheckouts` to `{}` on a malformed journal, which is data loss for a genuinely interrupted sale. | Accepted; the alternative is refusing to load |

### 21.2 Fixed, but the failure mode will recur if you re-derive the code

These are the ones most likely to be reintroduced by a careless port.

| Bug | The trap |
|---|---|
| **The inverted bag clamp** — `clamp(v, -(h-b), h-b)` inverts when the body exceeds the bag, driving the item through *both* walls. | Any "clamp inside bounds" written without checking `b > h`. |
| **Bag sank through the counter** — the model origin is its base, so a laid bag put half its flank below y=0. Fixed by deriving `counterLift` from the flatten factor and scale. | Baking a lift constant instead of deriving it. The baked 0.101 passed only on a 4 mm tolerance. |
| **Goods popped ~9% on the last placement** — carried meshes inherit the character body scale (0.87–0.99); `attach()` preserves it. Fixed by rescaling to world-true authored size the moment the mesh leaves the hand. | Any reparent that preserves world transform across a scaled hierarchy. |
| **The card fell behind the counter** — the customer controller's `Stage` pose dropped the arm every frame before its sim state reached `PAYING`, taking the grip-parented card with it. Fixed by re-asserting `PayCard` in `updateCard`, which runs *after* `customers.js`. | Frame-order coupling between two controllers writing the same transform. |
| **`CardInsertReady` killed sales mid-flight** — a 4 s watchdog was left behind when the card route changed from timer-inserted to "waits for the player to click". Any player who took a beat lost the sale. | A watchdog on a state that waits on a human. |
| **Recovery was a dead end** — the flow could sit in Recovery forever because every other transition was refused, with a customer standing at the register. Fixed by `abandonCheckoutRecovery`. | "Never invent an approval" does not require "never let go". |
| **`deskHitTargets` / `deskAction` mapped `h.action`** on hotspot records that carry `id`, producing an array of `undefined` filtered to empty — indistinguishable from an empty screen. The same mistake was made twice in one object. | Reading a field name you assumed rather than the one `addHotspot` stores. |
| **The hotspot list survived the register closing**, so with the player nowhere near the desk every row still read as on-screen and hit-testable. Fixed by gating `deskAction` on `active`. | State that outlives the thing it describes. |
| **The 20¢ coin** — asset Sheet 02 authored one, so the drawer labelled its fourth well with a denomination that does not exist in the reference till. | Letting an asset define a domain fact. |
| **The point-in-box scan test** tunnelled on fast drags. Fixed by `segmentHitsBox`. | Any per-frame containment test against a fast-moving object. |
| **`alignLaptopUi` on a `setTimeout`** was permanently one alignment behind. | Caching a transform that depends on an in-flight animation. |
| **The monitor overlap sweep passed** because all three fixtures omitted `items`, so it only ever rendered "Waiting for products". | A sweep with no planted-positive control. |

---

## 22. WHAT TO COPY EXACTLY vs. WHAT TO ADAPT

### 22.1 Copy exactly — transliterate, do not redesign

These are pure, tested, and their edge cases were paid for in playtests.

| Source | Why |
|---|---|
| `src/sim/register.js` — currency, stacks, `makeChange`, `makeChangeFrom`, `customerCash`, `payableInLargeCoins`, `changeGivingState`, `drawerCommitFor`, the whole stage machine, `segmentHitsBox` | Integer-cent arithmetic and the bounded-change DP are not things to re-derive. `makeChangeFrom` in particular: greedy is wrong and the code says why. |
| `src/sim/registerFlow.js` — all 30 states **as data**, `validateCheckoutContract`, `resolveCheckoutRecoveryTarget`, `abandonCheckoutRecovery` | This is the spec. Port it as a `ScriptableObject` or a static table and keep the validator as an EditMode test. |
| `src/sim/checkoutCashContract.js` | 43 lines; the persisted-money invariant. |
| `src/sim/checkoutSettlement.js` — the prepare/preflight/commit/tails ORDER and every idempotency key shape | The ordering is the whole point. Reordering it reintroduces torn commits. |
| `src/sim/checkout.js` — `consumeHeldBatch` validation, `recoverCheckout` skip rules | The "validate the whole set before removing anything" pattern and the two skip sets. |
| `src/sim/paymentBag.js` | 107 lines; the balanced bag. |
| `src/sim/barcode.js`, `src/sim/cardSwipe.js` | Pure judges. |
| `src/render3d/clubhouse/drawerMoneyLayout.js` | Pure, deterministic, unit-agnostic. Drops straight into C#. |
| `src/render3d/clubhouse/checkoutPaymentPresentation.js` | Pure layout math. |
| `src/render3d/clubhouse/checkoutScanPresentation.js` | Pure barcode + ray math. |
| `src/render3d/clubhouse/cashierPresentation.js` | Pure routing. |
| `bagFitPlan` / `bagPlacementFor` (from `simplifiedRegisterMode.js`) | Extract them; they are already pure and exported for exactly this reason. |
| `src/sim/customerFlow.js` (in `render3d/clubhouse/`) | Pure order planning + sequential placement. |
| `src/sim/customerBasket.js` | Carry categories. |
| **The derived constants**, verbatim: `MAX_EXTRA_CHANGE_CENTS 500`, `CHECKOUT_EYE_ABOVE_COUNTER 0.56`, `CHECKOUT_WORKING_FOV 54`, `SLIDE_DURATION 0.55`, `CARD_INSERT_TIME 0.72`, `CARD_TIME 1.15`, `BAG_DELIVER_TIME 0.78`, `BAG_CUSTOMER_HOLD 1.25`, `PAID_BAG_ACCEPTANCE_HOLD_SEC 1.4`, `DRAWER_OPEN_SPEED 3.2`, `DRAWER_CLOSE_SPEED 2.4`, `PRODUCT_PLACE_SECONDS 0.58`, `TERMINAL_BUSY_DOT_HZ 3` | Each was set by a specific playtest note recorded in the comment above it. |

### 22.2 Adapt

| Source | How |
|---|---|
| `simplifiedRegisterMode.js` (10,861 lines) | **Re-author in C#.** Split it — Golf's own comments admit it is too big. Suggested split: `CheckoutController` (flow + verbs), `CheckoutCameraRig`, `CheckoutInput`, `DrawerRig`, `CardRig`, `BagRig`, `ProductRig`, `PosScreen`, `CheckoutWatchdog`, `CheckoutResources`. |
| `frontDeskMonitorUi.js` | Re-author as UI Toolkit or a RenderTexture Canvas. **Keep `STAGE_COPY` verbatim** and keep the hotspot registry + overlap/truncation audit — the audit caught a real shipped defect. |
| `catalogProductVisual.js` `VISUALS` | Becomes a `ScriptableObject` per SKU with the same fields. Keep `separateHandoff` as the oversize rule. |
| `shopLayout.js` `REGISTER` | Becomes a `CounterLayout` ScriptableObject. Keep the local frame and the derivation comments. |
| `customerSimulation.js` | Adapt the state list to Geode's customer concept; keep the queue clearance/patience/abandon-depth logic. |
| `characterAsset.js` | Replace with a rigged humanoid + Animator. Preserve the `SetMode` surface and the scale-independent carry grip. |
| `cashierHands.js` | Optional. If Geode wants visible cashier hands, port `CASHIER_POSES` and `cashierHandPoseForFrame`; otherwise skip. |
| `audio.js` | Re-author with Unity AudioSources / FMOD. Keep the 33 cue names, the debounce, and the drawer-vs-outcome loudness relationship. |
| The laptop | Separate work item. `laptopRig.js` ports directly; the DOM interface becomes UI Toolkit on a RenderTexture. |

### 22.3 Do not port

| Source | Why |
|---|---|
| `evaluateCardSwipe` + `cardSwipe.js` | The shipping interaction is chip + keypad. Dead unless you want swipe. |
| Receipt printer, `loose_receipt`, receipt animation | Removed from the flow by owner instruction on 2026-07-31. |
| `barcode_scanner` placement, the five-phase scan choreography | Rejected as ceremony 2026-07-30. `barcodeFor` and `judgeBarcodeRead` stay. |
| `checkout_product_staging_tray`, the change handoff tray | Deleted 2026-07-30. Goods rest on the bare counter; change piles flat. |
| Reservations / tee times / green fees | Golf-only. Keep the service-line *mechanism* (§17 row 9). |
| The `pine-hills-v2` variant machinery | One layout is enough. |
| `gltfCache.js`, `sharedTexturePool.js`, `ktx2Support.js`, `staticSubtreeBatch.js`, `rigidVisualBatch.js`, `registerItemResources.js`, `createCardMeshResourceLedger`, `createPaidBagResourceLedger`, the GPU prewarm system | All of it exists because three.js has manual `dispose()` and no bundler. Unity's asset pipeline, GPU instancing and SRP Batcher replace every one. **Do not port manual resource ledgers into C#.** |
| `checkoutSale` (the "direct" path in `checkout.js`) | A second, legacy, card-only settlement path kept for tutorials/QA/old saves. One settlement path is better. Port only `completeSale`. |

---

## PORTING PLAN FOR GEODE EMPIRE

Ordered. Each phase ends with something runnable and something tested. Do not
start a phase until the previous one's gate passes.

### Phase 0 — Ground truth (½ day)

1. Read `src/sim/register.js`, `src/sim/registerFlow.js`,
   `src/sim/checkoutCashContract.js` end to end. They are 3,200 lines and they
   are the whole design.
2. Read `Designs/ProShop/FOUND_FALSE.md` (439 lines) and this document's §21.
3. Run the Golf gate once so you know what green looks like:
   `npm run gate`.
4. Run one Electron driver and **watch it**:
   `node tools/qa/run-electron.cjs tools/qa/simplified-register-acceptance.mjs --clubhouse=pine-hills-v2`.
   You cannot port an interaction you have not seen.

**Exit:** you can describe the 30-state flow and the change window from memory.

### Phase 1 — Port the domain, headless, with no Unity scene (2–3 days)

Create `Assets/GeodeEmpire/Scripts/Runtime/Checkout/` and transliterate, in this
order, because each depends only on those above it:

1. `Money.cs` — integer cents, `Bills`, `Coins`, `Denoms`, `RoundCash`,
   `MakeChange`, `StackTotal/Count/Add/Take`, `MakeChangeFrom` (the DP),
   `MigrateDrawer`, `MigrateLegacyQuarterStack`.
2. `CheckoutCashContract.cs` — `MaxExtraChangeCents = 500`, `PaymentContract(ticket)`.
3. `CheckoutFlow.cs` — the 30 states as a static table, `Transitions`,
   `ValidateContract()`, `Create/Transition/Resume/AbandonRecovery/TimedOut`,
   `ResolveRecoveryTarget`.
4. `RegisterTransaction.cs` — `CreateTx`, scan/bag, subtotal→total (goods vs
   service split), `RequestPayment`, the card verbs, the cash verbs,
   `ChangeGivingState`, `HandOverChange`, receipt, bagging, `CanComplete`,
   `VoidTx`, `SegmentHitsBox`.
5. `PaymentBag.cs`, `Barcode.cs`, `CustomerBasket.cs`.
6. `CheckoutSettlement.cs` — the WAL. **Port the preflight/commit/tails order
   exactly** (§14.2) and every idempotency key string.
7. `HeldStock.cs` — `PickFromShelf`, `ReturnToShelf`, `ConsumeHeldBatch`,
   `RecoverCheckout`.

Alongside, stub the §17 interfaces: `ILedger`, `IInventoryLifecycle`,
`ISpecimenCatalog`, `IPricing`, `ISalesTax`, `ILocalization`. Real
implementations can come later; the domain only needs the contracts.

**Port the tests as you go, one file at a time**, into
`GeodeEmpire.Tests.EditMode`. Start with `checkout.test.js`,
`register-complete.test.js`, `register-flow.test.js`,
`register-change-tolerance.test.js`, `checkout-atomicity.test.js`,
`checkout-settlement-recovery.test.js`, `inventory-conservation.test.js`.

**Exit gate:** EditMode suite green, including a save→reload→recover round trip
and a deliberately torn settlement that reconciles. `ValidateContract()` returns
30 states with every Recovery edge present.

### Phase 2 — Assets in, no interaction (1–2 days)

1. Copy `Assets/checkout/glb/*.glb` (the 24 register-critical files from §18.2)
   into `Geode/Assets/GeodeEmpire/Art/Checkout/`. Let Unity import and mint
   `.meta` files. **Commit the `.meta` files immediately.**
2. Extend `GeodeModelPostprocessor.cs` to, on import of anything under that
   folder:
   - hide/disable every `COL_*` child and move it to a collision layer;
   - find every `ANCHOR_*` / `*_SOCKET` child and write it into a
     `CheckoutRigReferences` component as a serialized `Transform` field;
   - read the drawer sockets' glTF `extras` into a `DrawerWellContract` struct
     (§10.3 table);
   - **fail the import with a clear message if a required name is missing.**
     That is the whole point of doing it at import time.
3. Make prefabs from the imported models. Build a `CheckoutPropLibrary`
   ScriptableObject mapping stem → prefab (the `merch.instantiateKit` replacement).
4. Author `CounterLayout.asset` from §8.2, in counter-local space, with
   `LocalToWorld`/`WorldToLocal` helpers matching `frontDeskPoint`/`frontDeskLocalPoint`.
5. Place the counter, POS, terminal, drawer, customer display and bag from that
   asset in a test scene.

**Exit gate:** an EditMode test that loads every checkout prefab and asserts each
required anchor/socket reference is non-null. A play-mode screenshot of the
counter at the derived working pose (§12.2) beside the Golf reference.

### Phase 3 — The static presentation (2–3 days)

1. `PosScreen` — RenderTexture, 1024×640, `STAGE_COPY` verbatim, the three tabs,
   the hotspot registry, and **port the overlap/truncation audit with its
   planted-overlap control**.
2. `CheckoutCameraRig` — the three pose keys, `CheckoutLookScale`, the derived
   check-in pose (from the live screen quad) and the derived cash pose (from the
   open drawer's bounds). Do not author these as fixed transforms; the derivation
   is what makes them survive a moved counter.
3. `DrawerRig` — tray slide on the authored axis, `DrawerMoneyLayout` (ported in
   Phase 1) instancing bills and coins into the wells, clip rotation from
   `ClipFillRatio`. Use GPU instancing for the money.
4. `BagRig` — `ChecoutBagPresentation` constants, the flatten group, the darkened
   liner, the anchors.
5. `CardRig` — card mesh, the painted face texture cache from `PAYMENT_CARDS`,
   the chip socket.
6. `TerminalRig` — the physical key meshes via `CheckoutTerminalKeyAction`, the
   three redrawn caps, the terminal screen RenderTexture with the X box, the
   busy dots.

**Exit gate:** a driven scene that steps the flow programmatically through all 30
states and screenshots each one. Compare against Golf's own golden captures.

### Phase 4 — Interaction (3–4 days)

1. `CheckoutInput` — the exact mouse and key table from §13. Right-click retracts
   change over an open drawer; the reader is modal against Escape; Space confirms
   change; Z undoes.
2. `CheckoutController` — the four workspaces, `Begin`, `Enter`, `Leave`,
   `Abandon`, `Update(dt)`, and `FlowTo(state, reason)` beside every domain verb.
3. `BagProduct` — **one forgiving click**, the 0.55 s lateral slide, commit at
   mid-slide, oversize branch to the set-aside point.
4. The card route: click the offered card → 0.72 s insert → keypad entry → OK →
   1.15 s authorize → approved.
5. The cash route: click the presented pile → drawer opens → auto-deposit →
   select change from wells → Done/Space → handoff → drawer closes.
6. `CheckoutWatchdog` — the `SIMPLIFIED_REGISTER_WATCHDOG_STATES` subset only.
   **Do not add a watchdog to a state that waits on a human.**

**Exit gate:** a PlayMode test that completes a three-item cash sale and a
three-item card sale with real simulated input, and a recorded clip of each that
you have watched frame by frame.

### Phase 5 — The customer (2–3 days)

1. `CustomerSimulation` — the state enum, arrivals, `QueueSlot(i)`, clearance
   0.95, `NeverAbandonDepth 2`, patience, the 1.25 s impatient beat.
2. `SequentialPlacement` — **one product per call**, 0.58 s each, the eased arc
   with the `sin(π·p) × 0.10` lift, world-true rescale on hand-off from the wrist.
3. `CustomerRig` — modes, `Hand(side)`, `CarryGrip(side)` as a scale-independent
   sibling.
4. `OnCustomerPaid` — bag attach, 1.4 s acceptance hold with preserved yaw,
   oversize carried goods, `ReceiveBag`, departure.

**Exit gate:** a clip of a customer approaching, queueing, placing three goods
one at a time, paying, taking the bag and walking out — watched, with the frame
that proves each beat named by timestamp.

### Phase 6 — Audio, prefs, polish (1–2 days)

1. The 33 cues, with the debounce and the drawer-quieter-than-outcome mix.
2. `CheckoutPreferences` — the five flags, `AnimationDelta`, `MonitorAccessibility`,
   `ShouldAutoConfirmExactChange`.
3. Hover outlines, tooltips, cursor states.
4. The device bay's lit panel; the terminal's float-to-face.

### Phase 7 — The gate (1 day)

Stand up Geode's equivalent of `npm run gate`:

```
dotnet format / analyzers (ratcheted, shrink-only)
→ asset manifest check (every referenced prefab is registered)
→ EditMode suite
→ PlayMode suite
→ golden screenshot capture + diff
→ a planted one-pixel control that MUST fail
```

The control is not optional. `npm run golden:control` exists in Golf because a
golden suite with no control cannot distinguish "nothing changed" from "the
capture is broken".

### Phase 8 — The laptop (separate, 2–3 days)

Only after checkout is green. `laptopRig.js` ports directly (§9.2); the interface
becomes UI Toolkit on a RenderTexture with the **per-frame** corner solve, the
34° seated lens, and the persisted boot-bar estimate.

---

### Three things that will decide whether this port degrades the checkout

1. **Do not collapse the two state machines.** `tx.stage` (what is legally true
   about the money) and `CheckoutFlow` (what is physically happening) are
   separate on purpose. Every time they were coupled, a renderer bug became a
   money bug.

2. **Do not simplify the settlement WAL.** It looks like over-engineering until
   you see the bug list it exists for. If Unity's serialization makes a true
   transaction possible, use it — but then delete the WAL deliberately and
   replace its guarantees, do not just drop it.

3. **Do not re-derive the constants.** Every magic number in §22.1 has a comment
   above it naming the playtest that set it. Copy the number and copy the
   comment. A "cleaner" value is a regression waiting for a playtest to find.
