# Starter shop, progression, UI & visual cohesion — plan

Controlling spec: `GEODE_STARTER_SHOP_PROGRESSION_UI_REBUILD.md` (repo root).
Baseline captures: `Geode/Assets/Output/starter/before/`. Evidence for each milestone lands in
`Geode/Assets/Output/starter/mN_*/`.

This plan supersedes `Docs/VisualRebuild/PLAN.md` wherever the two disagree. The visual-rebuild pass
matched reference R06/R26 **literally**, and that is the root of most of what is wrong below: those
images are captioned **Day 12 · $2,310 · Empire Level 3**. They are a mid-game state, and the last
pass built them as the *starting* state.

---

## A. Audit of the fresh save (2026-09-05, commit 180f4ec)

Method: `NewGame()` in Play Mode, then captures from standing eye height at four natural positions
plus a screen capture of the HUD and all four tablet tabs. Numbers below are measured, not estimated.

### A1. The Day-1 business is the finished business

Fresh save: `cash=120, upgrades=0, encyclopedia=0`. What the player can walk to on that save:

| area | size | state on Day 1 |
|------|------|----------------|
| workshop | 8.8 × 5.9 m = 51.9 m² | wash, inspection bench, hammer & chisel bench, storage shelf, dealer outbox, display cabinet |
| showroom | 4.6 × 8.7 m = 40.1 m² | **complete**: logo wall, two lit display walls, glass island counter, checkout + POS, plants, pendant globes |
| back of house | 8.8 × 2.8 m = 24.6 m² | **complete**: office desk + laptop, storage racks, packing bench, sorting table, receiving bay + roller shutter |
| **total** | **116.6 m²** | |

Against §4.2's list of what must *not* be visually accessible on Day 1 — mature showroom, premium
display runs, advanced storage wing, complete office, full-feature checkout, extensive finished
receiving space — the current build fails on **all six**.

The machines are the one thing already right: `trim_saw`, `geode_cracker` and `flat_lap` are
`PlaceableFixture`s gated behind their upgrades with their bodies hidden, and they are delivered as
crates and sited by the player. That mechanism (M5 of the last pass) is sound and is what the rest of
this plan reuses.

### A2. 41 fake specimens claim progression the player has not made

`ShopStock` — which I added last pass to match R06 — builds **41 procedurally generated specimens**
across 9 components (two display walls, the glass island top and its cased stock, wall shelving) and
they are all present on a save with $120 and nothing cracked. §6.1, §6.3 and §7.4 forbid exactly
this. The private-collection cabinet is correctly empty (`0/8`), so the offence is entirely retail.

### A3. HUD occupies a third of the screen

Measured off `before/hud.png` (1280×720) against reference R26 (1672×941):

| element | current | reference | over by |
|---------|---------|-----------|---------|
| objective card | 355 × 301 px = **27.7% × 41.8%** | 290 × 230 = 17.3% × 24.4% | 1.6× wide, **1.7× tall** |
| status card | 350 × 105 = **27.3% × 14.6%** | 250 × 145 = 15.0% × 15.4% | 1.8× wide |
| key rail | 5 hints, spans 742 px | 4 hints, ~390 px | 1.9× |
| tutorial banner | 665 × 58, `max-width: 860px` (64%) | none in frame | — |

The objective card's height is not typography, it is content: a three-line `Next:` prose block that
the reference does not have. The status card's width is dead space — the reference right-aligns the
same four facts in 250 px.

### A4. The tablet is grey slabs with placeholder dots

- **Upgrades** (`before/t_upgrades.png`): every row carries the *same* purple circle. §9.3 names this
  defect literally. No category, no preview, no statement of what changes in the world, no
  destination. The detail pane is a grey rectangle.
- **Collection** (`before/t_collection.png`): four large empty grey tiles reading "Undiscovered", and
  a **completely empty** detail rectangle — the single worst instance of §9.1's "giant empty
  rectangles". No filters, rarity, sort or category tabs; R07 has all four.
- **Suppliers**: the best of the four (M6/§64 fixed the fact ordering) but still flat-grey rows with
  a beige dot for crate art.
- **Stats**: two flat columns of label/value.

### A5. World art

Compared with R26 the room is much darker and browner. The reference has light warm timber with
visible framing, polished light concrete, **blue machine base cabinets with stainless tops**, and a
bright ambient that the warm pendants sit on top of. The current room is near-black brown board on
every surface with hot orange pools under each pendant and no ambient separation between zones.

---

## B. Architecture decision — three leases, not one room with locks

§4.1 offers a menu; §4.2 rules out "hide the finished rooms behind a lock". So the premises
themselves become the progression:

```
        x=-6.4          x=-0.4      x=2.4                x=7.0
 z=6.0  ┌───────────────────────────────┬──────────────────┐
        │  BACK ROOM  (lease 2)         │                  │
 z=3.2  ├───────────────┬───────────────┤   SHOP FRONT     │
        │               │  workshop     │   (lease 3)      │
        │  THE UNIT     │  east half    │                  │
        │  (day 1)      │  (lease 2)    │                  │
 z=-2.7 └───────────────┴───────────────┴──────────────────┘
                     hoarding        partition
```

- **Day 1 — "the unit"**: `x ∈ [-6.4, -0.4] × z ∈ [-2.7, 3.2]` = **35.4 m², 30% of today's
  116.6 m²**. A stud-and-board hoarding closes it at x = -0.4 and the cross wall closes it at
  z = 3.2. Neither is see-through: the mature business is not merely locked, it is not visible.
- **Lease 2 "Back Room"** — opens the cross wall *and* the hoarding's north half: storage racks,
  sorting table, office desk, packing bench, the real receiving bay and its roller shutter.
- **Lease 3 "Shop Front"** — the hoarding comes down entirely, the showroom shell becomes real, the
  street door works and customers start arriving. It arrives **bare**: a plain counter, one shelf
  unit, no logo wall, no glass island, no plants, no lit display runs.
- Retail fit-out then becomes individual purchases that each add geometry.

Day-1 sales run through the **dealer outbox**, which already exists in the workshop and already pays
appraised value. That is the believable small-operation opening: consign to a dealer until you can
afford a shop front.

Day-1 receiving is a **kerbside pallet inside the unit**; the four-cell bay under the shutter is part
of lease 2.

Implementation: one `PremisesExpansion` component modelled on the existing `WorkshopExpansion` —
roots toggled from owned upgrades, wired to `GameSession.Loaded` / `StateChanged`, nothing new
persisted beyond the upgrade ids the save already stores.

---

## C. Prioritised milestones

Order follows §21. Each ends with a Play-Mode capture, the console clean, EditMode green, and a commit.

| # | milestone | §  | done when |
|---|-----------|----|-----------|
| **M1** | **Premises architecture**: `PremisesExpansion`, the hoarding, the three leases in the upgrade catalogue, room-aware `ShopPlan`, receiving on Day 1 | 4, 5 | fresh save reaches 35.4 m² and cannot see or enter the showroom or back room |
| **M2** | **Ownership defaults**: delete the 41 scenery specimens; display runs carry *real* retail slots; empty slots read as intentional (riser + label + soft light) | 6, 7 | fresh save shows zero specimens the player does not own |
| **M3** | **Physical progression**: each lease and each retail fixture appears on purchase, is sited by the player where it should be, and survives save/reload | 5, 12, 17 | buy → deliver → place → reload, three times over |
| **M4** | **HUD reduction** to the reference proportions, with priority rules for overlapping layers | 8 | objective ≤18% × ≤26%, status ≤16% wide, banner ≤2 lines, rail contextual |
| **M5** | **Tablet rebuild**: baked preview thumbnails per upgrade, real specimen art and filters in Collection, designed Stats, stronger Suppliers | 9 | no placeholder dot, no empty rectangle, all four tabs at the reference's density |
| **M6** | **World art**: lighter timber, ambient/task separation, machine livery, material variety, prop meaning | 10, 11 | side-by-side against R26 is closer on materials, light and composition |
| **M7** | **Validation**: placement rules vs. the new rooms, authored-layout audit, customer flow in all three shop states | 12, 13, 16 | audits 0 findings; customers complete in starter/partial/mature |
| **M8** | **Acceptance**: fresh-career run to the third lease, four-state capture set, UI render QA, full test suite | 14, 15, 18, 22 | every §22 box true |

## D. Decisions taken (append as they are made)

- **D1.** R06 and R26 are **Day-12 targets**, not Day-1 targets. The reference pack is the *late*
  state of this game; matching it at hour zero is what produced the current failure. Every reference
  is now read as "what the business grows into".
- **D2.** Day-1 area **35.4 m²** (30% of the current build). Chosen so the whole opening loop —
  receive, wash, inspect, crack, store, consign — fits without any room feeling padded, and so the
  hoarding lands on a wall line the existing collision and route grids already understand.
- **D3.** Fake retail stock is **deleted, not hidden**. §7.4 allows decorative stock only when it is
  clearly not inventory; on a rock-shop shelf nothing reads as "not inventory".

- **D4.** The **display cabinet is bought, not given**. The day-one unit has no wall long enough for a 1.24 m
  cabinet — measured, not guessed — and that is the honest answer: a new business does not own a private gallery.
  `DisplayCapacity` therefore starts at **0** and the Collection Cabinet grants 8.
- **D5.** The island counter and the two shelving runs are `PlaceableFixture`s behind their own upgrades, so §5.1
  holds for retail as well as for machines. `RetailShop.RefreshCapacity` now counts only slots whose fixture is
  actually standing, instead of locking by list index — with fixtures bought in any order, index order was the
  scene builder's, not the player's.
- **D6.** `MeshFactory.Box` keyed its generated mesh asset by **name alone**, so every box called `Board`, `Frame`,
  `Rod1` or `Bracket` in the scene shared one mesh — whichever was written last — while its collider (set per
  object) stayed correct. Hung sign boards were rendering at other signs' widths and their rods at other signs'
  lengths. The key is now name + size; 204 stale single-name assets were deleted.

## E. Verification log (append per milestone)

### M1–M2 — premises, ownership, and the HUD reduction

**Starter footprint.** Day 1 is the workshop west of the hoarding: `x ∈ [-6.4, 0.6] × z ∈ [-2.7, 3.2]` =
**41.3 m², 35% of the 116.6 m² the player used to start with**. The back room (+24.6 m²) and the shop front
(+50.7 m²) are leases. The hoarding is stud-and-OSB with its frame on the workshop side, noggins, a diagonal
brace, hazard tape and a "UNIT 2 — NOT LET" notice; the two north openings are boarded the same way. Nothing
mature is visible from inside the unit.

**Ownership.** `ShopStock` is deleted. The 41 procedurally generated specimens that stood in the showroom on a
fresh save are gone; every shelf position in the shop is now a real `SaleSlot` on a felt riser, so an empty shop
reads as a shop waiting for stock. Evidence: `starter/m1/e_bareshop.png` (leased, unfitted) and
`f_fittedshop.png` (fitted, unstocked).

**Integrity.** `WorldIntegrityAudit` on **both** states:

| state | static overlaps | decor bounds | floor/floating | placement support | clearance |
|-------|:--:|:--:|:--:|:--:|:--:|
| day 1 (sealed unit) | 0 | 0 | 0 | 0 | 0 (free 3661, reachable 1116) |
| fully leased and fitted | 0 | 0 | 0 | 0 | 0 (free 3218, **reachable 3218**) |

Getting there fixed real defects: the pallet jack 145 mm inside the west wall, the cork board 18 mm into the
partition, bay pallets standing on their own mat, sale-slot anchors floating 32–91 mm over risers that had no
colliders to rest on, and D6's shared-mesh bug.

**HUD (§8).** Measured on a 1280×720 capture against reference R26:

| element | before | after | reference |
|---------|--------|-------|-----------|
| objective card | 27.7% × 41.8% | **17.0% × 29.3%** | 17.3% × 24.4% |
| status card | 27.3% wide | **15.5% wide** | 15.0% |
| key rail | 5 hints, 58% of width | **3 contextual hints, 24%** | 4 hints |
| tutorial banner | 52% wide | **35% wide** | not in frame |

The objective card's height was content, not type: a three-line `Next:` prose block. `Progression.NextUnlockShort`
replaces it with one line. The rail now shows Inspect only with something in hand, Build only with a movable
fixture owned, and Inventory only with stock.


### M3–M6 — progression, tablet, world art

**Physical progression.** `Playtest.RunStarterAcceptance()` walks §14 against a real `NewGame()` and reports
**pass=32 fail=0**. It checks, in order: neither lease signed and both hoardings up; no retail shop running;
the reachable floor; that `trim_saw`, `geode_cracker`, `flat_lap`, `shop_island`, `display_wall_a`,
`display_wall_b` and `display_cabinet` are all unowned with their bodies out of the scene; that the collection,
the encyclopedia and the world are empty of specimens; then buys the back room, buys the cabinet, finds it
crated on the workshop floor, sweeps the leased rooms for a legal pose and sites it, checks the body appears,
checks placement is refused in a doorway and in the unleased showroom, reloads and checks the pose, the
capacity and the empty collection all survive, and finally signs the shop-front lease and checks the shop
starts serving.

**Reachable floor**, measured on the audit's own 15 cm grid, is the number that says "small start":

| state | standing room |
|-------|--------------|
| day 1 | **24.7 m²** |
| both leases | **75.6 m²** |

**Two real defects surfaced by writing that test.** `DisplayCabinet` sat east of the hoarding, so the
reparenting pass had filed a *player-owned* fixture inside the sealed shop root — buying it on day one put it
in a room that was switched off. Fixtures carrying a `PlaceableFixture` are now never filed under a lease.
And `RetailShop.Instance` stayed set after its root was disabled, so a sealed showroom still answered as a
running shop; it is released in `OnDisable` now.

**Tablet.** Upgrades, Collection and Stats rebuilt — see the commit. `UpgradeIconBaker` renders the actual prop
behind each of the 23 upgrades into `Resources/UI/Upgrades`; the contact sheet is
`Assets/Output/starter/upgrade_icons.png`.

**World art.** Ambient raised and cooled, pendants down from 4.0 to 2.7 with a wider falloff, board/ceiling/floor
lightened, the dado repainted as painted board, and the wash station set into a blue base cabinet with a
stainless apron. Before/after: `starter/before/a_start.png` vs `starter/m6/a_room.png`.

**UI render QA (§15).** 1920/2560/3840 × UI scale 1.0/1.4 × four screens: **pass=28 fail=0 findings=0**, with all
four planted faults still caught. Two real findings were fixed on the way: the XP readout at 9 px and the
objective card's "Next" line at 10.5 px, both under the 11 px floor, caused by the HUD reduction.

**Four-state captures (§14).** `Assets/Output/starter/states/`: `day1`, `back`, `shop` (leased, bare),
`fitted` (island, shelving, sign, still unstocked).

### Workflow note

A `.uss` edit does not reach the running game until something forces an asset refresh; `unity command recompile`
and `AssetDatabase.ImportAsset` both failed to do it, and only a C# recompile did. Two UI QA runs reported
stale font sizes before this was spotted.

### Regression (§18)

- Retail cycle in the fully fitted shop (island and both shelving runs sited through build mode): **4 customers
  served, $1,033.80, 0 collision overlaps**.
- World-integrity audit **0/0/0/0/0** on both the sealed unit and the leased shop.
- Starter acceptance **32/32**.
- UI render QA **28/28, 0 findings**.

### M5 remainder — Suppliers (§9.2)

The last placeholder dot in the tablet was the supplier swatch. `UpgradeIconBaker` now also bakes four crate
builds — plain, curated, premium (lid off, a boxed piece beside it) and bulk (a pallet of two) — and a supplier
row carries the one matching what turns up on its pallet, with its accent as a pip. The detail card leads with
the same picture. The screen's subtitle now says where the delivery actually lands, which is the goods-in pallet
in the workshop until the back room is leased.

Re-ran after: UI render QA **28/28, 0 findings**. EditMode **68/68** — the career-pacing test needed its
purchase order extended with the premises leases and the retail fit-out, which are now most of what the middle
of a career is spent on.
