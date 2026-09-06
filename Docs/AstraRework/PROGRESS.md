# Astra durable recovery state

Updated 2026-09-06 after completed measured architecture study and recovery verification. Interruptions never complete the goal.

## Authority, branch and phase

Authoritative master: `GEODE_EMPIRE_ASTRA6_FULL_PROJECT_REWORK_STEAM_READINESS_MASTER_SPEC.md` at repository root. Active goal: the entire ASTRA FULL PROJECT REWORK + STEAM READINESS, accepted only by the playable game and all master Definitions of Done.

Branch `astra/full-project-rework`. Latest known-good code milestone `6874769a6c349ffb38926d5ec2f39dc6dec39548`, committed and pushed, remote verified. Earlier safety milestone `39463d22b7fe4b0a7b4f35d56e50a355485af914`; integrity/baseline `c506e58`. Main/origin main remains `1632ff25f1178c5dd0a7617e9a6fdcc329530344`. No history rewrite or force push.

Checkout/opening/input P0/P1 regression work is complete at 6874769. Return to master phase order: baseline truth audit and selected art targets → entire architecture/storefront/customer flow → visible assets → geodes/core/audio/UI/progression/performance/full QA. Do not keep polishing checkout or import the parked bench before architecture.

## Exact unfinished operation

No test, build, image generation, Blender batch or Unity coroutine is running. Unity is in Edit Mode with a clean Workshop. All QA isolation finished.

Current atomic work: checkpoint the completed measured architecture study, then integrate the real scene with ownership/receiving/save migration. Phase 1/2 evidence and target concepts are committed/pushed at 86f06ac8f552f6a83215967f73b0056d13cef1a9; remote verified and main unchanged. AstraLayout.cs and the existing partial WorkshopSceneBuilder now create a separate measured AstraLayoutStudy.unity (not in build settings; no gameplay/save systems). Nineteen envelopes pass native body/operator/route checks, including the blocked-counter negative control. The initial 44.5 mm bench/partition and cashier/wall intersections were corrected. Saved scene reload has no missing material references; Workshop was restored clean with its original hash. Native overhead and eye-height renders reviewed, in Architecture/. These are geometry study evidence, not actual game or Day-1 visual acceptance.

Recovery found one incidental M_CaseGlass material blend diff (SrcBlend 5→1). Read-only temporary-material control proved URP SetupMaterialBlendMode derives 1 when preserve-specular is enabled and 5 when disabled. The existing builder wrote 5 while leaving preserve-specular/premultiplied shading enabled. Correcting the builder to use URP's own setup makes the normalized material intentional and consistent; do not manually force a conflicting blend factor back. The exact event that first serialized normalization was not observed. Compilation required the editor-only Universal.Editor assembly reference; validate after reload before committing.

Next substantial operation: extend the existing builder with an in-place apply to a validation copy of Workshop, preserve gameplay and Delivery IDs, and synchronize ShopPlan with a scene-version marker. Implement minimum Day-1 selling, shared finite receiving and explicit safe legacy layout/ownership migration before any isolated Play of that scene. Old saves must not enter new walls without migration. No whole-scene rebuild or production promotion before validation.
Read the current scene/component paths from live MCP before editing. A full pre-change shallow hierarchy is cached in functions store `architectureBefore`; use bounded filtered output. Existing `WorkshopSceneBuilder.Build` replaces the scene and duplicates ShopPlan constants. Prefer extending that pipeline with an in-place architecture apply that preserves existing gameplay/component/Delivery IDs. Do not blindly run the full old builder or patch YAML.

Dependencies now being inspected: ShopPlan, PremisesExpansion/WorkshopExpansion, ReceivingArea/FixtureDelivery, fixture defaults, RetailShop route points and starter counter, SaveSystem migration/player positions. Appraised-only SaleSlot currently prevents Day-1 retail without the installed appraisal station; resolve the minimum-kit dependency explicitly without introducing a free premium inspection station.

## Completed milestones

- Integrity baseline accepted: three DeliveryCrate references rebound to imported standalone script through Unity APIs, file IDs preserved, restart and Play passed. Bounded half proxies passed 18 targeted cases, 256 cooks, crack/reveal/settling/pickup/placement. `Docs/IntegrityGate/REPORT-2026-09-06.md`. Do not undo or repeat without a regression.
- Safety 39463d2: career/settings isolation, checksummed prepare/enter/finish, negative wrong-path refusal, warm/full domain reload and normal Dynamic-frame QA input. Preserve real career, backup and settings.
- Checkout 6874769: persisted open/close; correct inactive-counter lifecycle; abandonment cleanup; atomic drawer-before-first-save; empty drawer preservation; Continue drawer rebind; station action-map preservation and input consumption; exact cents everywhere in checkout.
- Fifty scoped tests passed. Actual 4/5/9/5/Enter entered 45.95 and approved one sale; live/disk cash 200→245.95, exact drawer unchanged, same sold identity left. Conflict retry through real E banked once. Gamepad South scanned with zero movement; East retained unpaid ticket and restored movement. `CHECKOUT_MILESTONE.md` and linked JSON/native `Checkout/` captures. Full controller sale and final visual quality remain later gates.
- Baseline actual first order $120→$45, crate opening to ten rocks, pickup observed. This is not the mandatory whole fresh career.
- Wash audit FINISHED once: S0001-620A, front region clean 1.00/opposite 0.00; scripted yaw sweep left nine dirty regions after 73.3 seconds. Inverted brush and poor basin/HUD presentation proven. `TRUTH_WASH_DIAGNOSTIC.json`. Harness “clean” and “held” screenshot labels are inaccurate; never cite them as success.
- Fixture diagnostic: injected credit, normal purchase/unpack APIs, full route validation. All three actual Delivery objects resolved the correct purchased machine. Saw/cracker defaults cut off a workstation route; lap default lacks standing room. No invalid pose saved. `TRUTH_FIXTURE_PLACEMENT_DIAGNOSTIC.json`. This does not prove no custom placement is possible or pass machine gameplay.
- Five official Steam games / 15 screenshots reviewed; benchmarks written. All 26 unique provided references reviewed (27 files, one duplicate); clickable reference contact sheet/index and critique saved.
- Sixteen original concept outputs complete, copied and viewed. Every master §8 subject has A/B variants. Art direction selected and corrections recorded; concept coverage, 121-FBX + 13-family asset manifest and provisional performance budgets saved. No asset has final Unity acceptance.
- Measured architecture study completed: 24 m² starter, 36.66 m² processing, 48.72 m² showroom, 7.2 m² office. Nineteen body envelopes, native operator overlap/capsule routes, negative control, saved-scene material reload and two native renders. ARCHITECTURE_STUDY_EVIDENCE.json records limits.
- Permanent capacity/interruption rules are saved below and outside KitWright's managed block in `Geode/AGENTS.md`; useful prior instructions preserved.

## Unity, Blender and isolation

Unity 6000.6.0f1, project `/Users/kenneth/Documents/GitHub/Geode/Geode`; MCP responsive. NOT playing, compiling or updating. Workshop clean. No production scene/model changed since integrity baseline; separate study scene added. Glass blend normalization is being made consistent with its URP material builder. Scene SHA256 `ffaf74cd0e9097457ac40e826c1442ce35b901f18096fed27d8402ebc2d125fc`. Original Play options enabled: DisableDomainReload + DisableSceneReload.

Latest QA `8992bb7873e24bff9c3a2bba329d2714` (whole-game-truth-wash-and-machines) FINISHED `2026-09-06T18:06:35.7315710Z`. Override cleared, actual original hashes rechecked equal preparation, Workshop unchanged. Eleven finished sessions in SAVE_ISOLATION_EVIDENCE.json. Current originals:
- career `3ee18e29db190dad365f0254eeb8166c42d33f4cdb39bedda12ee40dcf74032f`
- backup `42f698a6b27e7139b88f980e01874da1ae00846a03112cdb60c5e5d38fc04b63`
- settings `b18f7ce0a4380393f11cec0a10190fdcaab62e359206c404b01ad120bb642340`

Recovery boundary: after prior QA finished at 19:14:39 local, the real career ran/migrated for about eight seconds and saved at 19:14:53–54. Trigger unproven, predates subsequent recovery compile/tests. Same real career/cash/count; newer real files preserved, never rolled back. `PLAYER_DATA_RECOVERY_BOUNDARY.json`. Historical first-nine-session career/backup hash `d45f8931de0d03eb7bba7ccf024b9162fa5f5cd0d2f54f0139a4eb01bc462694` is NOT current player state.

Every Play: Prepare → verify exact career/settings/override and protected hashes → Enter separately. Failed/timed-out preparation prohibits entry. Exit → Finish → actual original hashes and scene check. Never write real `/Users/kenneth/Library/Application Support/DefaultCompany/Geode` files. Standalone needs equivalent explicit isolation.

Blender MCP responsive, 5.2.1 LTS, addon 1.6/protocol 5. Scene `Astra_Bench_Review_01`: bench + seven proxies; original Scene and Astra_Baseline_Bench_Audit preserved. `Tools/blender.sh` tested exit 0. Draft `ArtSource/Blender/Props/prop_workbench.blend` and ignored `Tools/Blender/Output/AstraBench/prop_workbench.fbx`: 4346 vertices/8508 triangles, 1.8×0.749×0.9 m, four material slots/seven boxes. PARKED, not imported or accepted. Later import must update all four material assignments and correct proxies in the existing builder.

## Tests and unresolved defects

Latest scoped job `59eb3483-008a-4a28-9e0f-25c30770ed88`: FINISHED 50/0/0, 3.83 seconds, 19:35:04 local. Earlier 44-case job f491f7b7-1dd6-478c-a491-a14c9d36272c also passed. CheckoutMoney/Flow/RegisterTransaction/Presentation, ShopHours, SaveMigration, SettingsIsolation, StationInput. Compilation clean, no warnings; cached runtime errors absent in last 1800 seconds of the audit.

Full baseline: 135 cases, 134 pass, one P1 timeout ProcessingChoiceTests.HammerVsSawReport (371.57s vs 180s limit), full run 847.81s. BuildPiece 496–776ms vs whole Build 187–255ms; repeated noise-backed surface rays are the lead. A slower inlining trial was discarded. Do not raise timeout, weaken samples or hide hitches with animation delay. Fix in geometry/performance phase.

Current serious whole-game defects: starter has wash/appraisal/storage but no installed checkout; inadequate storefront/zoning; invalid authored machine placements; inverted brush/contact presentation; dirty wash occupant always resumes despite “tap to take” promise; wash Escape can also pause (PauseMenu excludes wash from BenchActive); giant UI/HUD blocks; geometry hitches; NPC mannequins. Full machine outcomes, four-layout customer stress, no-cheat career, full controller/KBM, standalone and performance remain unaccepted.

Audio clips exported read-only from the pure bank; `AUDIO_BASELINE.md`, `Baseline/audio-review.mp3` and index. The tool cannot provide audio input to this model, so perceptual listening is explicitly NOT complete. Source confirms constant nonspatial filtered-noise/60–120Hz ambience. Do not claim audio sounds good/bad or passes from code/RMS. A supported listening review is required later.

## Concepts and design decisions

All image jobs finished; no cells to poll. Originals retained under `/Users/kenneth/.codex/generated_images/01a075ac-c9a6-78b2-bf1d-9f7213c7fd8c/`. `Docs/AstraRework/Concepts/`: day-one-a, day-one-b, storefront-early-retail, cracking-inspection, wash-cracker, saw-lap, midgame-mature, receiving-storage, checkout-laptop, collection-shelves, ui-suppliers-equipment, ui-collection-business, ui-overview-inventory, ui-stats-premises-bills, processing-workshop, ui-owned-inventory (all .png).

Day-1 B: pale plaster, blue steel #2E4D58, cream, restrained oak, concrete, neutral daylight and limited warm task pools. Real entrance and ~1.2m public route, separate staff access, one drawer, modest laptop. All later machines/dedicated inspection/storage earned. Three angled supports touch the actual rock. Shallow wash with connected plumbing. Hydraulic cracker B, saw blade in YZ plane with feed +Y from operator -Y, horizontal lap. Two usable receiving bays; no center cart, giant repeated logos, ornate gold UI or invented data. Inventory B list/detail shows actual owned identities; do not teleport stock through a generated “Move to storage” button.

Historical master, final/Fable/V4/V5/V6, starter/core-loop specs and principal reports read; visual/reference specs fully read. Long checkout handoff has partial technical coverage recorded; its implemented port contract has been audited against the current source and targeted transactions. Old completion claims remain historical.

## Exact next actions

1. Evidence/art-direction checkpoint 86f06ac is committed/pushed and verified. Continue the measured architecture; main remains untouched.
2. Commit the completed measured study and intentional transparent-material builder correction after clean compilation; preserve parked Blender work unstaged. Then extend the existing builder in place on a validation scene copy, preserving Delivery/script IDs. No YAML patches.
3. Implement starter storefront/door/checkout/laptop access, minimal earned kit, open/closed sign, back processing and mature retail routes, and safe save-coordinate/fixture migration. Validate against actual collider envelopes before asset detail.
4. Play isolated fresh/expanded/custom layouts with keyboard/controller, collision/placement/arrival-close-settlement checks and performance capture. Commit a coherent architecture milestone, then proceed through the Blender manifest and every subsequent master phase.

Resource/workflow: 8GB M2; heavy Blender/Unity tests/builds sequential. Use narrow project-log queries, never launcher Editor.log wholesale. Source changes → immediate request_recompile → recovery/wait → error checks. Unknown timeout → inspect underlying process before repeating. Native captures avoid the MCP 512px limit, but true target-resolution captures are still required. Diagnostics with credit/state injection are not primary career acceptance.

## Modified/uncommitted files

86f06ac committed the evidence/planning/concept set. Pending coherent study checkpoint: PROGRESS.md; ARCHITECTURE_PLAN.md, BEFORE/DEPENDENCIES/CHECK text, LAYOUT/STUDY_EVIDENCE JSON, Architecture/ native renders; AstraLayout.cs/meta; WorkshopSceneBuilder.AstraLayout.cs/meta; AstraLayoutStudy.unity/meta; WorkshopSceneBuilder.cs partial declaration and URP transparent setup; GeodeEmpire.Editor.asmdef URP editor reference; M_CaseGlass.mat normalized blend. Parked draft stays unstaged: Tools/Blender/gen_props.py and ArtSource/Blender/Props/prop_workbench.blend. Inspect git status for exact paths before committing.

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
