# Hands-on core loop, shop expansion and operating costs — plan

Working document for `GEODE_HANDS_ON_CORE_LOOP_EXPANSION_ECONOMY_OVERHAUL.md`.
Written after auditing the running fresh-save game, not from reading the code alone.

**Safe checkpoint (§3): `cf825de`** — compiles clean, EditMode 68/68 green, baseline captures in
`Geode/Output/captures/coreloop/baseline/`, baseline crack profile below. No force pushes, no history rewrites.

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
| M2 | 360° handling: roll, fine mode, inertia, reset | `PlayerInteractor.TurnHeld` — verified in Play Mode, KBM + pad |
| M3 | Look-to-discover clues, loupe gating, clay blocks reading | `SpecimenSurface` + `ReadSurface`; 11 EditMode tests |
| M4 | Spatial washing proven one-sided | `ONE FACE: front region 23 clean=1.00  opposite region 19 clean=0.00 (spatial=YES)` |
| M4 | Full wash from the basin view | `washed in 73.1s: dirt=0.12 cleaned=0.69 dirtyPatches=8`, overlaps>4mm: 0 |
| M6 | Reveal hitch removed | worst frames 189/325/232 ms -> 31.6/55.5/<33 ms; idle control 39–57 ms |
| M9 | Receiving refuses instead of overlapping | `RefusalReason()` before spend; crate gap >= 0.62 m |
| M12 | Ledger model + version-3 migration | 12 EditMode tests; old saves load with 0 owed |
| — | Editor responsiveness restored | ping 30 s timeout -> 1.2 s after moving 1,620 scratch PNGs out of `Assets/` |
| — | EditMode suite | **91/91** (68 + 11 SurfaceTests + 12 LedgerTests) |
| M5 | Good blow vs bad blow reachable in play | `STRIKE FEEL: live=5 (mean q 0.92) dead=1 (q 0.06)` — both branches fire |
| M5 | Break scales by size, material and tool | 9 EditMode tests on `FractureAudio.Plan`; `audio-crack` span 0.09 ms |
| M7 | Notification tiers restrained | 3 rocks -> 2 cards + 1 quiet line; old build popped "Best X so far" on the ordinary one |
| M8 | Every reachable tutorial step points at something | fresh save 14 resolved / 0 dead; crate open 16 resolved / 0 dead |
| M10 | Day-one retail, no leases | enter -> browse -> queue -> counter -> checkout -> exit, cash 350 -> 380.95, shopfront=False throughout |
| M11 | Arrears block expansion and premium sourcing | `LedgerTests`; starter acceptance back to 35/35 |
| M12 | Bills UI and metering | bill $121.78 shown line by line, Pay clears it; a minute of saw+lap+cracker = 0.117 units, 7.5 l |
| M13 | Seven archetype careers, 30-90 days | bills 44-53% -> 22-27% of takings; counter $29.50/piece vs dealer $19.60 |
| M13 | A genuine fresh-save career | 15.2 min, 16 opened, 10 sold, 12 families, dup/orphans 0, Console clean |
| M14 | Audio set complete and level matched | 45 cues exist, peak spread inside 8x, UI quieter than a hammer tap; `scrub_dry` and `scrape` had been silent |
| M15 | Persistence | closed crate / mid-animation / opened / held all reload; `maxMove=0.000 missing=0`, `atOrigin=0` |
| M15 | Controller | tablet tabs, purchase, pause, settings tabs, sliders, toggles, fullscreen confirm/revert, back to gameplay |
| M15 | Customer stress | 30 spawned, 24 served (80%), 0 stuck / loops / stalls / path failures, 0 overlaps, 60-82 fps |
| M15 | UI render QA | 28/28 pass, 0 findings at 1080p/1440p/4K and 1.0x/1.4x (was 6 fail / 24 findings) |
| M15 | EditMode suite | **121/121** |
| DoD | Several crates, capacity, refusal | goods-in takes exactly its 2, refuses in words, closest 1.710 m, refused order costs $0 |
| DoD | Expansion is physical and persists | hoarding down, floor 1096 -> 1707 cells, rent 48 -> 122, lease and ledger survive a reload |
| DoD | A bill arrives, is paid, is remembered | $141.34 with a 3-line breakdown and a due date; paid exactly; nothing owed after reload |
| DoD | Phase acceptance, with the counter standing | **20/20, 0 collision overlaps** |

## J. Definition of Done (§30)

Every box in §30 that a running game can prove has been proved in Play Mode and is listed above. Two
carry a caveat rather than a tick:

- **"performance measured"** — measured and written down (`PERFORMANCE.md`), but the figures were taken on
  a machine deep into swap. The idle control run reads 2,417 ms worst frame while doing nothing at all, so
  the numbers describe the operating system as much as the game. They need re-taking with memory free.
- **"no major interaction hitch"** — the crack hitch is gone (189-325 ms -> 29.7-55.5), which was the
  defect §10 was written about. A 6.8-10.9 MB allocation still lands on the frame after the discovery card
  is presented; it is recorded as outstanding rather than claimed as fixed.

