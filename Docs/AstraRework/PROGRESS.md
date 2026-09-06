# Astra durable recovery state

Updated 2026-09-06 after capacity interruption and live recovery inspection. Interruptions do not terminate the goal.

## Authority and current phase

Authoritative master: `GEODE_EMPIRE_ASTRA6_FULL_PROJECT_REWORK_STEAM_READINESS_MASTER_SPEC.md` at repository root. Active goal: the entire ASTRA FULL PROJECT REWORK + STEAM READINESS, accepted only by the playable game and every master Definition of Done. Latest user steering: finish the interrupted checkout/input P0/P1 milestone, commit it, then immediately return to Phase 1 truth audit → Phase 2 benchmarking/concepts/final art direction → Phase 3 entire architecture/storefront/customer flow. Do not polish checkout beyond serious defects. Park the draft bench until the architecture phase permits asset production.

Branch `astra/full-project-rework`. Latest committed/pushed known-good milestone: `39463d22b7fe4b0a7b4f35d56e50a355485af914` (isolated career/settings QA and reliable input scheduling). Integrity/baseline checkpoint `c506e58`. Main/origin main remains `1632ff25f1178c5dd0a7617e9a6fdcc329530344`. No force push, history rewrite, or unverified main changes.

## Exact unfinished operation

Checkout/opening/input regression milestone is verified and ready to commit. Remaining cents P1 fixed: POS, customer display, cash/change and approval show integer-cent amounts to two decimals; terminal entry uses invariant decimal punctuation. Fifty scoped tests passed. Actual 4/5/9/5/Enter produced 45.95, approval 45.95, one sale, cash 200→245.95 on disk/live, unchanged exact drawer, and the same sold specimen left. `CHECKOUT_CENTS_EVIDENCE.json` and tracked `Checkout/` screenshots. No known P0/P1 remains in this bounded regression change; broader architecture/geometry/UI P1s remain open in the master audit.

Next atomic operation: commit/push this coherent checkout/opening/input plus recovery-rule milestone on astra/full-project-rework, then immediately resume Phase 1 truth audit and Phase 2 benchmark/concept completion. Do not resume asset production or keep polishing checkout. No test/build/Blender batch/image request is running.

## Completed milestones and evidence

- Integrity baseline remains accepted: three DeliveryCrate components rebound to imported standalone script via Unity APIs, IDs preserved; clean restart and Play checks passed. Bounded half collision proxies passed 18 tests, 256 cooks, crack/settle/pickup/placement and separation checks. `Docs/IntegrityGate/REPORT-2026-09-06.md` is the evidence. Do not undo or repeat without a regression.
- Safety milestone committed/pushed: career + settings follow isolated save directory; wrong-path refusal, warm/full domain reload and original file preservation verified. `AstraQaInput` schedules bounded events on normal Dynamic updates; MCP's immediate manual update can consume action edges. Do not confuse that tool behavior with gameplay.
- Baseline first crate bought with actual input (cash 120→45), opened to ten rocks, S0006-B157 picked up. This is partial opening evidence, not the mandatory complete fresh career. Baseline screenshots: `Docs/AstraRework/Baseline/` and native `Geode/Output/AstraQA/`.
- Opening-hours code (uncommitted) adds persisted ShopOpen, closed fresh/migrated v3 careers, admission guards and Business controls. Actual KBM opened and controller closed; existing customers remained and finished; new arrivals refused; UI count updated live. `OPENING_HOURS_CARD_DIAGNOSTIC.txt`.
- Checkout defects proven/fixed: inactive counter subscription after lease; stale unpaid ticket after customer abandonment; drawer assigned after first save; empty drawer minted at reload; checkout disabled its own action map. Source fixes compiled and scoped tests passed. See `CHECKOUT_ABANDONMENT_DIAGNOSTIC.json`, `CHECKOUT_CASH_ORDERING_DIAGNOSTIC.json`, `CHECKOUT_ATOMIC_SAVE_EVIDENCE.json`, and `CHECKOUT_INPUT_RECOVERY_EVIDENCE.json`.
- Atomic first cash save: live/disk cash 245.95, exact denominations 418.70; subsequent card/reload cash 291.90, drawer unchanged; same specimen sold/left, no leftovers. Unpaid abandonment preserved cash/stock then allowed another sale. Controlled drawer conflict refused banking; restoring injected cent + actual E performed once, banked once, and handed over the same specimen. Gamepad South scanned one item with zero player displacement; East exited retaining unpaid ticket and restoring normal movement.
- Permanent capacity/interruption rules saved below and outside KitWright managed block in `Geode/AGENTS.md`; existing useful instructions preserved. No automatic model switching.

## Unity, Blender and player-data isolation

Unity 6000.6.0f1, project `/Users/kenneth/Documents/GitHub/Geode/Geode`; MCP responsive. STOPPED, not compiling/updating, Workshop loaded and clean. Workshop SHA256 `ffaf74cd0e9097457ac40e826c1442ce35b901f18096fed27d8402ebc2d125fc`. No production scene/model changed since integrity baseline. Original Play options: enabled, DisableDomainReload + DisableSceneReload; preserve after diagnostics. Current compile clean; no cached runtime errors over the verified session.

Latest QA `a784c4555121468f9dc595c33290809f` (checkout-exact-cents-card-input) FINISHED 2026-09-06T17:40:11.8691930Z. Active manifest and save override cleared; Workshop clean. Ten finished sessions indexed in SAVE_ISOLATION_EVIDENCE.json. Rechecked actual original hashes after exit: all equal preparation. Current main SHA256 `3ee18e29db190dad365f0254eeb8166c42d33f4cdb39bedda12ee40dcf74032f`; backup `42f698a6b27e7139b88f980e01874da1ae00846a03112cdb60c5e5d38fc04b63`; settings `b18f7ce0a4380393f11cec0a10190fdcaab62e359206c404b01ad120bb642340`.

Recovery boundary: between prior QA Finish at 19:14:39 local and this new preparation, real career was loaded/migrated and saved at 19:14:53–54; same career/cash/count, ~8 more seconds of play. Trigger unknown, predates this recovery's compile/tests. Preserved newer files; no rollback. `PLAYER_DATA_RECOVERY_BOUNDARY.json` records exact differences. Prior nine-session career/backup baseline was `d45f8931de0d03eb7bba7ccf024b9162fa5f5cd0d2f54f0139a4eb01bc462694`; never confuse that historical checksum with current player state.

Every automated Play: Prepare → verify exact save/settings paths and hashes → Enter separately; after exit verify Finish and actual original hashes. Failed/timed-out preparation prohibits entry. Never write real `/Users/kenneth/Library/Application Support/DefaultCompany/Geode` files. Preserve settings, backup and career byte-for-byte within each session.

Blender MCP responsive, 5.2.1 LTS, addon 1.6/protocol 5. Scene `Astra_Bench_Review_01`, eight objects (bench + seven proxies); original Scene and Astra_Baseline_Bench_Audit preserved. CLI `Tools/blender.sh` available and tested exit 0. A draft blue-steel/oak bench was generated before latest phase-order steering: `ArtSource/Blender/Props/prop_workbench.blend`, staged FBX `Tools/Blender/Output/AstraBench/prop_workbench.fbx`. 4346 vertices / 8508 triangles, 1.8×0.749×0.9 m, four material slots, seven box proxies. Reviewed in Blender only, NOT imported/accepted in Unity. Park it. Future import must update all four material arrays and recreate seven boxes through existing builder/API, not just overwrite FBX.

## Test results and unresolved defects

Latest targeted job `59eb3483-008a-4a28-9e0f-25c30770ed88`: FINISHED, 50 passed / 0 failed / 0 skipped, 3.83 s, 19:35:04 local. Earlier 44-case job f491f7b7-1dd6-478c-a491-a14c9d36272c also passed. Classes CheckoutMoney/Flow/RegisterTransaction/Presentation, ShopHours, SaveMigration, SettingsIsolation, StationInput. Compilation clean. Actual keyboard/gamepad proof recovered after interruption; do not restart this finished job.

Full baseline EditMode: 135 cases, 134 passed, one P1 timeout `ProcessingChoiceTests.HammerVsSawReport` (371.57 s / 180 s limit), full run 847.81 s. `BASELINE_TEST_RESULTS.json`. Do not increase timeout or weaken samples as the fix. Measured BuildPiece 496–776 ms versus Build 187–255 ms; repeated noisy shape ray queries are the lead. A noise inlining trial was slower and discarded. Structural work belongs to the appropriate geometry/performance pass; do not postpone architecture indefinitely for it.

Checkout/opening/input scoped P0/P1 defects now resolved and verified; full checkout visual/whole-career acceptance remains later work. Whole-game outstanding: major geometry hitch; Day 1 over-equipped but no installed starter checkout; weak architecture/storefront, all-brown/orange lighting, UI slabs and overlapping HUD; weak brush/wash orientation and physical support; NPC mannequins; complete fresh career, full controller/KBM, customer stress, audio listening and standalone/performance not yet accepted. Read truth matrix for evidence limits. Full Steam readiness is NOT achieved.

## Concepts and design decisions

All image-generation calls finished; no cells to poll. Original outputs retained under `/Users/kenneth/.codex/generated_images/01a075ac-c9a6-78b2-bf1d-9f7213c7fd8c/`. Copied/reviewed targets under `Docs/AstraRework/Concepts/`: day-one-a.png, day-one-b.png, storefront-early-retail.png, cracking-inspection.png, wash-cracker.png, saw-lap.png, midgame-mature.png, receiving-storage.png, checkout-laptop.png, collection-shelves.png, ui-suppliers-equipment.png, ui-collection-business.png, ui-overview-inventory.png, ui-stats-premises-bills.png.

Draft `ART_DIRECTION.md`: Day-1 B pale plaster, blue steel, cream checkout, oak and concrete, neutral daylight with restrained warm pools. Modest ~24 m² concept is a target, not measured layout. Real customer entrance and clear ~1.2 m routes; no preinstalled later machines. One cash drawer; 13-inch basic management laptop. Three low angled rubber supports must physically contact rock; reject floating concept specimens. Wash shallow enough to see rock, connected plumbing. Hydraulic cracker B (reject nonsensical screw gauge); saw blade YZ plane normal X, feed +Y from operator -Y; horizontal lap. Receiving exactly two usable bays, not generated four marks. Reject universal giant branding, ornate/gold UI drift, invented financial data/imperial units. Final art selection and complete required variant coverage still pending; inventory board lacks an owned-stock detail variant.

Asset manifest indexes 121 FBX plus 13 procedural/world families; no asset is final accepted. Performance budgets provisional, standalone 60 FPS unproven. Initial official Steam benchmarks and screenshots exist; complete broader comparative analysis. Finish provided reference pack contact sheet (27 files / 26 unique) and remaining bounded historical reads. Master, final design, Fable, V4/V5/V6 specs, starter/core-loop specs and principal reports were read; visual/reference specs and long checkout handoff still have explicitly tracked partial coverage.

## Exact next actions

1. Review/stage the intentional checkout/opening/input, regression evidence and permanent recovery-rule files; commit/push the known-good milestone and update this file with its hash.
2. Isolation is finished, compile/tests/input proof passed, scene unchanged. Leave unrelated art drafts separate unless checkpointed explicitly; do not rerun finished gates without a new regression.
3. Immediately finish Phase 1 truth audit and Steam baseline, recording unobserved cases honestly. Finish Phase 2 official simulator benchmarking, reference review, missing original target variants, and final art selection.
4. Redesign the entire shop/storefront/entrance/zones/expansion/customer routes (Phase 3), then execute the Blender manifest (Phase 4). Continue every remaining master phase through real career, full QA, standalone, performance and visual parity. No broad bench import before architecture.

Resource/workflow: 8 GB M2; heavy jobs sequential. Never read launcher Editor.log wholesale (may contain credential); use project Geode/Logs/Editor.log with narrow filters. No shell edits to Unity serialized assets, Library, Temp, Logs or obj. Source changes → immediate request_recompile → recovery/wait → clean compilation. Tool timeout → inspect underlying job/process before starting another. Native screenshots avoid MCP default 512 px limit. Primary acceptance career uses no state injection or debug teleporting; diagnostics are explicitly labelled.

## Modified/uncommitted files at checkpoint

```text
 M Docs/AstraRework/PROGRESS.md
 M Docs/AstraRework/SAVE_ISOLATION_EVIDENCE.json
 M Geode/AGENTS.md
 M Geode/Assets/GeodeEmpire/Scripts/Editor/AstraQaInput.cs
 M Geode/Assets/GeodeEmpire/Scripts/Runtime/Checkout/CheckoutStation.cs
 M Geode/Assets/GeodeEmpire/Scripts/Runtime/Core/CursorController.cs
 M Geode/Assets/GeodeEmpire/Scripts/Runtime/Core/Playtest.cs
 M Geode/Assets/GeodeEmpire/Scripts/Runtime/Retail/RetailShop.cs
 M Geode/Assets/GeodeEmpire/Scripts/Runtime/Save/GameState.cs
 M Geode/Assets/GeodeEmpire/Scripts/Runtime/Save/SaveSystem.cs
 M Geode/Assets/GeodeEmpire/Scripts/Runtime/UI/TabletUI.cs
 M Tools/Blender/gen_props.py
?? ArtSource/
?? Docs/AstraRework/ART_DIRECTION.md
?? Docs/AstraRework/ASSET_REWORK_MANIFEST.md
?? Docs/AstraRework/CHECKOUT_ABANDONMENT_DIAGNOSTIC.json
?? Docs/AstraRework/CHECKOUT_ATOMIC_SAVE_EVIDENCE.json
?? Docs/AstraRework/CHECKOUT_CASH_ORDERING_DIAGNOSTIC.json
?? Docs/AstraRework/CHECKOUT_INPUT_RECOVERY_EVIDENCE.json
?? Docs/AstraRework/Concepts/checkout-laptop.png
?? Docs/AstraRework/Concepts/collection-shelves.png
?? Docs/AstraRework/Concepts/cracking-inspection.png
?? Docs/AstraRework/Concepts/midgame-mature.png
?? Docs/AstraRework/Concepts/receiving-storage.png
?? Docs/AstraRework/Concepts/saw-lap.png
?? Docs/AstraRework/Concepts/storefront-early-retail.png
?? Docs/AstraRework/Concepts/ui-collection-business.png
?? Docs/AstraRework/Concepts/ui-overview-inventory.png
?? Docs/AstraRework/Concepts/ui-stats-premises-bills.png
?? Docs/AstraRework/Concepts/ui-suppliers-equipment.png
?? Docs/AstraRework/Concepts/wash-cracker.png
?? Docs/AstraRework/OPENING_HOURS_CARD_DIAGNOSTIC.txt
?? Docs/AstraRework/PERFORMANCE_BUDGET.md
?? Geode/Assets/GeodeEmpire/Scripts/Tests/EditMode/ShopHoursTests.cs
?? Geode/Assets/GeodeEmpire/Scripts/Tests/EditMode/ShopHoursTests.cs.meta
?? Geode/Assets/GeodeEmpire/Scripts/Tests/EditMode/StationInputTests.cs
?? Geode/Assets/GeodeEmpire/Scripts/Tests/EditMode/StationInputTests.cs.meta
```

## ASTRA CAPACITY / INTERRUPTION RECOVERY

The active authoritative goal is the full ASTRA PROJECT REWORK + STEAM READINESS master specification.

This work is expected to survive:
- "Selected model is at capacity" failures
- API/server overload
- network interruption
- Codex restart
- context compaction
- Unity domain reload
- Unity MCP timeout
- Blender MCP timeout
- machine sleep/restart
- user absence

These events are interruptions, NEVER completion.

Maintain Docs/AstraRework/PROGRESS.md as the durable recovery state.

Update PROGRESS.md:
- after every meaningful milestone
- before every substantial/high-risk operation
- after discovering an important defect
- after every known-good commit
- before context compaction
- whenever remaining context reaches about 30%

PROGRESS.md must always contain:
- authoritative master-spec path
- active goal
- current branch
- latest known-good commit
- current phase
- completed milestones
- work currently in progress
- exact unfinished operation
- unresolved defects
- modified/uncommitted files
- Unity state
- Blender state
- test results
- player-data isolation state
- generated concept-art paths
- important architectural/design decisions
- exact next actions

Before long or risky implementation work, create a coherent checkpoint whenever practical.

If the model/API returns "Selected model is at capacity":
- do not reinterpret the goal
- do not roll back work
- do not change model automatically
- preserve the current state in PROGRESS.md if execution is still available
- on the next successful request, reread PROGRESS.md and resume the exact interrupted operation

After any Codex restart or context compaction:
1. read GEODE_EMPIRE_ASTRA6_FULL_PROJECT_REWORK_STEAM_READINESS_MASTER_SPEC.md
2. read Docs/AstraRework/PROGRESS.md
3. read Docs/AstraRework/PLAN.md
4. read Geode/AGENTS.md
5. inspect git status/log
6. inspect Unity state
7. inspect Blender state if relevant
8. verify any active QA isolation session
9. resume the exact unfinished operation

Never begin a duplicate test/build because the previous MCP/tool request timed out. First determine whether the underlying process is still running.

Never declare the full rework complete because a Codex turn, context window, capacity allocation, or session ends.

Only the master specification's Definition of Done may terminate the goal.

Also change the compaction policy for this project:

- Do not wait until ~250K tokens.
- When approximately 30% of context remains, finish the current atomic operation, update PROGRESS.md completely, make a clean checkpoint if appropriate, compact, reread the recovery sources above, and immediately continue the same goal.
- Treat compaction as maintenance, never as completion.
