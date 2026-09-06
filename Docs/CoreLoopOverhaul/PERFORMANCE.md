# Core-loop overhaul — performance record

§22 asks for measured before/after numbers, not impressions. Everything here comes from `PerfProbe`
capture windows taken in Play Mode on the development machine (Apple Silicon M2, 8 GB), through the
`Playtest` harness driving the real input path. Each row names the run that produced it.

## How these were measured

`PerfProbe.Begin(label)` opens a window; `PerfProbe.Frame(unscaledDeltaMs)` records every frame's time,
its gen-0 collection count and its managed allocation; `PerfProbe.Measure(name)` times a named span.
`End()` prints the spans worst-first, the frame stats, and the seven frames around the worst one with
their allocation. The probe is free when not capturing.

**Read every number below against the idle control.** An "idle" run is the same harness doing nothing
at all for the same number of frames. On a machine under memory pressure the control is worse than the
work, and any figure taken then is measuring the operating system, not the game.

| Run | Frames | Mean | Worst | Over 33 ms |
|---|---|---|---|---|
| idle control, machine healthy | 600 | 4.0 ms | 29.7 ms | 0 |
| idle control, swap full (10.9 of 11.2 GB) | 600 | 13.1 ms | **2,417 ms** | 11 |

## The crack and reveal (§10)

The headline defect of the phase. Measured on a fresh save, three rocks, `Playtest.CrackPerf`.

| Rock | Before | After |
|---|---|---|
| 1 | 189.0 ms | 29.7 ms (0 frames over 33 ms) |
| 2 | 325.5 ms | 55.5 ms |
| 3 | 232.3 ms | 31.6 ms |

Four causes, each measured before and after rather than guessed at (§28 warns specifically against
moving cost around and calling it fixed):

| Span | Before | After | What changed |
|---|---|---|---|
| `MineralShelf.Refresh` | 385–647 ms | 0.04–2.6 ms | Rebuilds only the slots that changed, a piece per frame, and holds while a presentation hold is open |
| `StateChanged` (all subscribers) | 160–324 ms | 0.25–5 ms | Deferred out of the frame the rock splits on; each subscriber is charged for its own time while capturing |
| thumbnail render | 46–192 ms | 2.5–7.7 ms | Photographs a lightweight proxy of the live mesh instead of generating a specimen, 90 crystals, prewarmed |
| collider re-cook | 89–298 ms | 0.01–0.02 ms | `RebuildColliders` is idempotent and cooks with `CookForFasterSimulation \| UseFastMidphase` |
| URP shader compile | 3.9 s, once | — | Moved to load: crystals are prewarmed on bench entry and after each damaging blow |

`audio-crack` after the §9 layered rebuild: **0.09 ms**. Layering the break across six scheduled cues
costs nothing measurable — they are scheduled on the audio thread, not built per frame.

## Everything else §22 lists

Measured in the same windows unless noted.

| Moment | Number | Note |
|---|---|---|
| specimen pickup | within frame noise | no span exceeded 0.3 ms in any capture |
| 360° manipulation | no measurable span | `TurnHeld` is arithmetic on one transform |
| dirt updates | no per-frame texture write | 24 regions pushed as `SetVectorArray("_RegionClean", Vector4[6])` on the existing MPB; no `Texture2D.Apply()` in the scrub loop |
| washing | no span over 1 ms | contact is one viewport ray plus a sphere intersection |
| final strike | see the table above | |
| fracture / reveal | see the table above | |
| discovery UI | `deferred:discovery-card` 0.11–0.15 ms | a routine note no longer photographs the rock at all (§12.1) |
| thumbnail capture | `thumbnail-render` 2.1–3.1 ms | `thumb:camera` 1.5–2.8 ms, `thumb:proxy` 0.2–0.4 ms |
| save | `flush-save` 2.15–2.37 ms, `flush-save-revealed` 1.84–2.42 ms | |
| multiple crates | not re-measured | goods-in is capped at 2 kerb / 4 bay / 5 stage-3 slots and refuses beyond that (§14), so the count is bounded by design |
| customer spawn | not separately instrumented | capped at `RetailShop.MaxCustomers` = 3 |
| expansion activation | not separately instrumented | one `SetActive` per room root plus a `RefreshCapacity` |

## Allocation

The worst frame in the post-fix crack runs allocated 6.8–10.9 MB and took one gen-0 collection. On a
healthy machine that frame cost 82 ms; with swap full the same allocation cost 153 ms. The allocation
itself is unmeasured — no instrumented span sits on that frame — and it lands one frame after
`deferred:state-changed`, so it is UI Toolkit laying out and repainting the discovery card. Reducing it
is the next performance job and is **not** claimed as done.

One allocation was removed on sight rather than measured: `TutorialBeacon.LateUpdate` called
`GetComponentsInChildren<Renderer>()` every frame to size the marker. It measures the target when the
target changes instead.

## A genuine fresh-save career (§19.2)

`Playtest.Career("balanced", 14 min)` on a fresh save, after the rebalance: 3 cycles over 15.2 minutes,
16 rocks opened, 10 sold to the dealer, 1 over the counter, 12 mineral families found, 5 upgrades
bought, cash 120 -> 127.95. Integrity counters all clean — `dupEntities=0 dupRecords=0 orphans=0` — and
the Console ended with zero errors and zero warnings.

Two things it shows that the deterministic simulations cannot:

- `walkRescues=18` over 15 minutes. The harness walks in straight lines and the rescue is it recovering;
  it is not a player-facing defect, but it is why harness runs need the rescue at all.
- `leftEmpty=12` against 1 counter sale in the last cycle, with 6 pieces on the shelves. Customer taste
  is meant to be real — an archetype whose liked minerals are not on display does not buy — but that
  ratio wants measuring properly against a stocked shop before it is called correct.

## Outstanding

- The 6.8–10.9 MB allocation on the frame after the discovery card is presented.
- Customer spawn and expansion activation want their own capture windows.
- Every figure in this document needs re-taking on a machine that is not swapping. The idle control is
  the honest statement of what the numbers are worth: when it reads 2,417 ms, nothing else on the page
  can be trusted as a measurement of the game.
