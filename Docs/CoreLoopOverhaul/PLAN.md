# Hands-on core loop, shop expansion and operating costs — plan

Working document for `GEODE_HANDS_ON_CORE_LOOP_EXPANSION_ECONOMY_OVERHAUL.md`.
Written after auditing the running fresh-save game, not from reading the code alone.

**Safe checkpoint (§3): `cf825de`** — compiles clean, EditMode 68/68 green, baseline captures in
`Geode/Assets/Output/coreloop/baseline/`, baseline crack profile below. No force pushes, no history rewrites.

---

## A. Current-state findings

Everything here was reproduced in Play Mode on a fresh save, with the figure or capture that proves it.

### A1. The crack hitch is real and large — and it is not where I would have guessed

`PerfProbe` (new, `Runtime/Core/PerfProbe.cs`) held a capture window across the final strike and the whole
reveal, for three rocks on a fresh save:

| rock | worst frame | second-worst | what was in them |
|---|---|---|---|
| 1 | 189.0 ms | 161.0 ms | `record-discovery` 127 ms, then `thumbnail-render` 140 ms |
| 2 | 325.5 ms | 162.4 ms | `thumbnail-render` 166 ms, `record-discovery` 108 ms, `rebuild-crystals` 27 ms |
| 3 | 232.3 ms | 212.5 ms | `rebuild-crystals` 119 ms, `thumbnail-render` 157 ms, `record-discovery` 79 ms |

Two consecutive frozen frames, every time, at the exact moment the rock opens: **300–440 ms of stall**.

Sub-instrumenting named the real culprits:

- **`disc:statechanged` — 48–62 ms.** `RecordDiscovery` ends with `StateChanged?.Invoke()`, so every subscriber
  (HUD, tablet pages, retail, fixtures) rebuilds inside the crack frame.
- **`thumb:build` — 47–92 ms.** `SpecimenThumbnailer.Render` constructs a *complete second SpecimenVisual* —
  full geode mesh and crystals — purely to photograph it. The camera render itself is only **3–6 ms**.
  The rock it is photographing is standing on the bench, already built.
- **`rebuild-crystals` — 2.4–95 ms.** Interior crystal meshes are generated at the instant of the split,
  although they are fully determined by the seed the moment the rock exists.
- `flush-save` — 1.8–4.5 ms on a fresh save. Small now; it is a full serialise + write + *read back and
  re-parse* + two renames, and it scales with career length (a mature save is 68 KB).

**Root cause:** everything the game wants to *say* about the discovery is executed synchronously inside the
frame where the rock is supposed to physically break. §10.2's split between critical and noncritical work
has not been made.

### A2. Receiving overlaps crates — provably, not theoretically

Bought three crates on a fresh save and printed their transforms:

```
pair 1-2 gap=0.000 at (-0.30, 1.42, 2.45) / (-0.30, 1.42, 2.45)
pair 1-3 gap=0.000 at (-0.30, 1.42, 2.45) / (-0.30, 1.42, 2.45)
pair 2-3 gap=0.000 at (-0.30, 1.42, 2.45) / (-0.30, 1.42, 2.45)
```

Three crates at one transform. `baseline/receiving_overlap.png` shows the result: crates interpenetrating,
their rocks hanging in mid-air.

**Root cause,** in `ReceivingArea.NextSpot()`: before the back room is leased there is exactly **one** kerb
cell. The function tries three stack levels 0.44 m apart, but its occupancy test rejects a spot when a crate
is within `Mathf.Abs(y - spot.y) < 1.6f` — which every stack level is. So all three are "occupied", the loop
falls through, and the function `return`s the ground cell unconditionally, on top of whatever is already
there. Meanwhile `BuyCrate` permits four crates before any bay exists. Crate size is never consulted.

### A3. There is no retail at all on Day 1

`RetailShop.Instance` is `NONE` on a fresh save. The shop root is inactive until the Shop Front lease is
signed, and `OnDisable` correctly releases the static — so **no customer can exist** until the player has
paid Back Room ($550) + Shop Front ($1,200), and a sales fixture on top.

The previous phase was right to delete the premature mature showroom, but in doing so it pushed *all* retail
past $1,750. §15.1 wants customers as soon as there is a legitimate item for sale; §28 forbids reserving
retail for late game. The fantasy "process rock → put it up for sale → someone buys it" must exist in hour one.

### A4. Washing is a hold-to-fill timer, not cleaning

`SpecimenCondition.Cleaned` is a single float 0..1. `WashStation.Update` does
`cond.Cleaned += dt / ScrubSeconds` while the button is held, spins the rock on a fixed yaw so it *looks*
worked, and calls it clean at 98%. There is no spatial dirt: one side cannot be clean while another is dirty,
nothing is missed, and where the brush actually is has no effect. This is exactly §7.1's forbidden shape.

### A5. Inspection reveals answers, not clues

`PlayerInteractor.HandReading` composes the whole read-out at once — size, weight, coating, locality, and
then `Preparation.ShellNotes(g)` which dumps *every* shell note the geology has (seam quality, chip, staining,
colour showing through, texture) the moment dirt drops below 0.1. Looking at a particular part of the rock
changes nothing. The loupe (`LoupeTool`) is a genuine magnifier with a real lens shader — good, and worth
keeping — but it gates nothing and discovers nothing.

### A6. Rotation is two-axis

`_inspectRot = AngleAxis(-look.x, up) * AngleAxis(look.y, right) * _inspectRot` — pre-multiplied world-axis
accumulation, so it is a proper arcball and does *not* gimbal-lock. But there is **no roll**, no reset, no
fine mode, no inertia, and orientation is thrown away on every release. §6.1 asks for yaw, pitch and roll.

### A7. Discovery presentation has two shapes and no queue

`RecordDiscovery` fires the big card for `firstOfFamily` — which, in the opening hours, is *almost every
rock* — and for Exceptional tier. `HudController.OnDiscovered` overwrites whatever card is showing, with no
queue and no rate limit. `Notify` stacks toasts capped at four, no de-duplication, no significance threshold.
§12 wants three tiers with rate limiting.

### A8. Tutorial beacons point at roots, not at the thing

`TutorialBeacon.Resolve` already looks up live components (`Find<WashStation>()`, `Find<CrackingBench>()`),
so §13.2's semantic targeting is half-built and survives moves and reloads. What it returns is the **station
root transform** — usually the base of a machine — not the cradle you must put the rock on, the tub you must
dunk it in, or the scale pan. There are no `hammer` or `chisel` targets at all, and the "pick up a rock" step
points at the crate rather than the rock.

### A9. Operating costs do not exist

No rent, electricity, water, bills, due dates or operating-cost pressure anywhere in `GameState`,
`UpgradeCatalog` or the tablet. Expansion is a menu purchase that changes the world (good — `PremisesExpansion`
already does the physical transformation) but costs nothing to hold afterwards.

### A10. World/art notes from the baseline captures

`baseline/room_overview.png`: the unit reads as one continuous timber brown — walls, hoarding, benches, crates
and pallets all share a tone. The floor is flat grey. There is a large empty floor plate to the east. The wash
station is the only station wearing machine livery.

---

## B. Root causes, grouped

1. **No critical/noncritical split.** (A1) Presentation, bookkeeping and persistence all run inside the
   physical event's frame.
2. **Scalar state where spatial state is needed.** (A4, A5) Cleanliness and clue knowledge are single numbers
   attached to a specimen, so no interaction can be local to a place on the rock.
3. **Gating by lease where gating by capability was meant.** (A3) Retail is bound to the *showroom room*
   rather than to *owning something to sell it from*.
4. **Placement logic that returns a fallback instead of failing.** (A2) `NextSpot` cannot say "no room".
5. **Targets resolved to objects rather than to affordances.** (A8)
6. **A missing system.** (A9)

---

## C. Dependencies

```
M1 baseline/instrumentation ──┬── M6 reveal performance
                              └── M14 art / M15 regression
M2 360° handling ── M3 inspection clues ── M4 spatial washing
                                   │              │
                                   └──────────────┴── M5 hammer feel, M9 receiving
M10 early retail ── M11 expansion ── M12 rent/utilities ── M13 economy rebalance
M7 notifications (independent)   M8 tutorial targeting (needs M2–M5 anchors)
```

Spatial dirt (M4) must land before inspection clues (M3) can be *obscured* by dirt, but M3's clue model must
exist first for dirt to have something to hide — so they are built together, dirt-model first.

Rent (M12) cannot be balanced before expansion (M11) sets the areas it prices, and M13 must follow both.

## D. Milestone order

Follows §25 except where the audit proved a different order cheaper:

- **M1** Baseline + instrumentation. `PerfProbe`, crack profile, captures, checkpoint. *(done)*
- **M2** True 360° handling: roll, reset, fine mode, inertia, persistence of orientation; controller + KBM.
- **M3** Hands-on inspection: per-region clue model, look-to-discover, loupe gating, prediction from evidence.
- **M4** Spatial washing: per-region dirt, brush contact, rotate-to-finish, dirt hides clues, wash upgrades.
- **M5** Hammer/chisel feel + layered fracture audio.
- **M6** Reveal performance: pre-warm crystals, defer StateChanged, photograph the real rock, async save.
- **M7** Notification hierarchy: three tiers, thresholds, queue, rate limit.
- **M8** Tutorial targeting: affordance anchors, exact tools, offscreen guidance.
- **M9** Receiving: real slots, real capacity, refuse rather than overlap.
- **M10** Early retail: a starter counter the player buys cheap, customers in hour one.
- **M11** Shop expansion as a priced, physical, persistent progression.
- **M12** Rent, electricity, water, bills UI, due dates, graduated late path.
- **M13** Economy rebalance with deterministic simulations across player archetypes.
- **M14** World/art/audio polish on everything touched.
- **M15** Full regression: controller, KBM, persistence, customer stress, fresh career, final captures.

## E. Likely Blender work

- Cleaning tools: brush, sponge, cloth — hand-held, correct scale, pivot at the grip.
- A proper utility sink/basin with plumbing that reads in first person (§7.8), replacing the current tub if
  its geometry is the limiting factor.
- Starter retail counter/table for M10.
- Meter cabinet / consumer unit and a water meter as the physical anchor for bills (§20.2 electrical detail).
- Steel-framed bench legs, carried over from the gap recorded at the end of the previous phase.

Runs stay headless and sequential (8 GB budget): never while the Editor is doing heavy work.

## F. Likely Unity runtime work

New: `SpecimenSurface` (region model shared by dirt and clues), `Observation`/`ClueCatalog`,
`CleaningTool`, `Notifications` (tiering + queue), `Ledger`/`Bills` (rent, utilities, due dates),
`StarterRetail`, receiving slot model. Modified: `PlayerInteractor`, `WashStation`, `SpecimenVisual`,
`SpecimenCondition`, `CrackingBench`, `GameSession`, `HudController`, `TabletUI`, `TutorialBeacon`,
`ReceivingArea`, `UpgradeCatalog`, `GameState`, `WorkshopSceneBuilder`.

## G. Performance risks

- Per-region dirt must not become a per-frame texture write. Budget: a small fixed region count with a
  shader parameter block, no `Texture2D.Apply()` in the hold loop.
- Pre-warming crystals moves cost to bench-entry — which must not itself become a new hitch. Measure it.
- Customers arriving early means more agents alive for longer on an 8 GB machine. Cap and measure.
- Bills must not tick per frame; evaluate on day boundaries.

Every one of these gets a before/after number in `Docs/CoreLoopOverhaul/PERFORMANCE.md`. No guessing.

## H. Save migration risks

`GameState.CurrentVersion` is 2 and there is no migration switch — `SaveMigrationTests` exists and must stay
green. New fields (surface state, observations, ledger, receiving slots, expansion state) must all default to
a sane value on an old save: an existing career must load with zero bills owed, a sensible next due date, its
specimens fully clean rather than suddenly filthy, and its already-known rocks not re-notified as discoveries.
Bump to version 3 with an explicit migration and a test per field.

---

## I. Verification log

*(filled in as each milestone lands: what was measured, in what state, with which capture)*

| # | What | Evidence |
|---|---|---|
| M1 | Baseline profile, 3 rocks, fresh save | worst frames 189.0 / 325.5 / 232.3 ms — see A1 |
| M1 | Crate overlap reproduced | three crates, gap 0.000 m — see A2 |
| M1 | No Day-1 retail reproduced | `RetailShop.Instance == NONE` |
| M1 | EditMode suite | 68/68 |
