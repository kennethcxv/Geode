# Astra execution plan

Authority: [full rework master specification](../../GEODE_EMPIRE_ASTRA6_FULL_PROJECT_REWORK_STEAM_READINESS_MASTER_SPEC.md). The entire playable game is the deliverable. This plan is active; no phase-wide acceptance or Steam-readiness declaration has been made.

## Checkpoint and safety

- Work on `astra/full-project-rework`; local main remains at `1632ff2`. Baseline checkpoint `c506e58` preserves the repaired DeliveryCrate references and fractured-half collision proxies, the incoming project state, and previously ignored runtime building source. Never rewrite history or push an unverified change to main.
- Every automated Play session starts with `AstraQaSession.Prepare`, exact career/settings path validation, and `AstraQaSession.Enter`. Preparation failure stops entry. Each exit must restore the directory override and prove the original career, settings, and rolling backup are unchanged. Use copied careers for migration tests; never run destructive tests against the originals.
- Scene/prefab/asset changes go through Unity APIs with Undo and targeted saves. External source edits are followed immediately by import/recompile, recovery, and compilation checks. Preserve original Play Mode options after diagnostic experiments.
- Heavy Blender production, Unity stress/tests, and standalone builds run sequentially on the 8 GB M2 development machine.

## Current findings and immediate work

The baseline full EditMode run completed 135 cases: **134 passed, one timed out** (`ProcessingChoiceTests.HammerVsSawReport`, 371.57 seconds against a 180-second limit). Keep the sample and economic assertions meaningful; profile its repeated piece generation before changing implementation. Increasing a timeout alone would not resolve game responsiveness.

The settings isolation fix passed its unit test, a deliberately wrong-path Play refusal, and a real Play save/exit cycle. The original three protected files remained byte-identical and Workshop stayed clean. The helper also restored its override across a full domain reload.

The fresh scene visibly contains the wash, appraisal and storage setup but lacks the master specification's installed starter checkout. It remains uniformly dark timber/orange light. The tablet works through real input but its large grey list/detail surfaces and clipped benefit chips are below the requested target. The actual first purchase and crate opening were performed through gameplay input; screenshots are in `Geode/Output/AstraQA/`.

Validation tools also need scrutiny: the MCP screenshot default caps even an explicitly requested large image at 512 pixels, so use native captures or deliberately configure its limit. MCP's immediate manual InputSystem update can consume action edges before gameplay sees them. Scheduling input on normal Dynamic updates opens/navigates/purchases correctly. The first warm-reload movement failure requires a controlled repeat before attributing it to game code. Exclude tool-compilation frames from frame-time samples.

Immediate sequence:

1. Resume the exact interrupted checkout/input milestone from PROGRESS.md. The safety milestone 39463d2 is already committed/pushed; 44 scoped tests and actual keyboard retry/gamepad scan/exit passed. Finish the remaining exact-cents display defect, verify its affected behavior, and commit the coherent known-good checkout/opening/input work. Do not repeat completed integrity/checkout checks without a regression.
2. Immediately return to the master phase order: complete historical requirement extraction and live truth audit across all matrix areas. Mark unobserved claims as partial; code and old reports do not establish acceptance.
3. Finish Steam simulator benchmarking, provided reference review, all required original concept variants, final art selection and budgets.
4. Redesign the entire architecture/storefront/entrance/zoning/customer flow before proceeding through the Blender asset manifest. The unimported draft bench is parked. Address the measured geometry timeout in the appropriate geometry/performance pass; do not hide it with a higher timeout or let it derail the phase order.

PROGRESS.md is the durable interruption/recovery state. Update after meaningful milestones and before substantial/risky work, commits or compaction. At about 30% remaining context, finish the atomic operation, checkpoint, compact where supported, reread the prescribed recovery sources and continue the same goal. Capacity failures, tool timeouts and restarts are interruptions, never acceptance.

## Production order and exit evidence

| Phase | Work | Evidence required before acceptance |
|---|---|---|
| 0 Safety | Integrity preservation, branch, isolated career/settings/backups, reliable QA entry and exit | Clean compile, typed Delivery readback, collision tests, negative isolation control, checksum preservation, verified commit |
| 1 Truth audit | Every historical promise against source, saved assets and live game; Steam simulator baseline | Completed truth matrix with specific evidence and prioritized defects; representative beginning/middle/late captures; current test/performance baseline |
| 2 Art direction | Original variants for shop stages, storefront, entrance, each workstation, receiving/storage, checkout, laptop, collection, shelves and management pages | Reviewed concept/reference contact sheets, selected targets, written critiques, asset manifest, performance budget |
| 3 Architecture | Tiny starter storefront and entrance, meaningful exit, checkout, player open/close, routes, receiving and expansion footprint | Natural first-person walk; minimum starter equipment; existing customers finish after closing; route and placement controls; save/load migration |
| 4 Visible assets | Reauthor weak furniture, architecture, industrial equipment, tools, props, NPCs, materials and lighting | Blender source and studio review per family, correct scale/UV/normals/pivots/proxies, Unity near/medium/room captures compared with targets |
| 5 Geode heroes | Family-specific exterior/rind/cavity/crystals, attached growth, cut/polish/wet/damage presentation | Deterministic per-family/state sheets; explicit containment and intersection tests; 30–60 cm inspection; hero/background cost measurements |
| 6 Physical loop | Real cradle support and pickup/reposition, manual inspection and orientation, spatial brushing, immediate fracture and settling | Actual KBM/controller interactions, negative placement controls, contact and separation measurements, crack timeline and frame pacing |
| 7 Sound and motion | Replace weak ambience/impacts/fracture, grounded material/size variants, machine sound, VFX and physical animation | Listened-to clips and live captures, sound-level comparisons, correct wet/debris/contact behavior, measured cost |
| 8 Laptop and UI | Physical management laptop, inventory, suppliers, upgrades, collection, business, stats, premises and bills | Coherent selected UI concepts, readable real data, actual input navigation/rebinding, supported resolution/scaling sweep, persistence |
| 9 Retail | Human customers, enter/browse/select/queue/pay/package/handoff/exit, stock identity, open/close | Cash/card repeated sales through input, exact identity, starter/expanded/mature/custom-layout stress with served/abandoned/stall/recovery/overlap counts |
| 10 Career | Manual-first economy, equipment unlock→buy→deliver→unpack→place→activate, expansion and operating costs | Genuine fresh career, meaningful ROI/pacing and fair recovery, deterministic strategy simulations, interruption/migration checks |
| 11 Performance | Stable 60 FPS target on documented intended Steam configuration; frame pacing and resource budgets | Representative standalone captures with CPU/GPU/frame percentiles, spikes, GC, memory, render/physics/NPC counts; no masking hitches with delays |
| 12 Full QA | Full regression, controller/KBM, persistence, placement, stress, standalone | All automated tests green, no known P0/P1, real fresh-career completion, standalone launch/new/load/play/save/relaunch, all required state matrices |
| 13 Visual parity | Every selected concept/reference against the actual player camera | Reviewed side-by-side evidence for all major spaces, stages, machines, geodes and UI; rejected weak results iterated |
| 14 Readiness | Final matrix, evidence index, report, verified commits/pushes | Every master DoD satisfied; no category hidden behind an “almost”; final report accurately states hardware and remaining P2 limits |

For each production asset/area: **concept/reference → Blender → Unity Play → screenshot → critique → iterate → profile**. Build scripts alone do not pass a visual gate. Runtime geometry may remain where it is the right system, but does not excuse weak form or unchecked containment.

## Acceptance discipline

Keep the primary career run free of cash/unlock injection and debug teleporting. Targeted fixtures and state injection may support stress or regression tests, and must be labelled as such. Record the purchase/item/save identity and the player action that produced each result. Re-test affected systems after each change; run the complete suite at major milestones. Do not count repeated NPC recovery as successful navigation. Do not add unrelated systems to avoid fixing the observed game.

Final readiness requires zero known P0/P1 defects and every applicable gate in the master specification. The active goal stays open until that is true.
