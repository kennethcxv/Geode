# Astra durable recovery state

Updated 2026-09-07 00:57 local after QA23 root-cause evidence and isolation cleanup. Interruptions never complete the goal.

## Authority, branch and phase

Authoritative master: `GEODE_EMPIRE_ASTRA6_FULL_PROJECT_REWORK_STEAM_READINESS_MASTER_SPEC.md` at repository root. Active goal: the entire ASTRA FULL PROJECT REWORK + STEAM READINESS, accepted only by the playable game and all master Definitions of Done.

Branch `astra/full-project-rework`. Latest LOCAL tested machine-gating checkpoint `7df7e3956e2c5b99ca33001a05e91f6b07541aa7` follows QA-incomplete architecture checkpoint `12ef41003e818c223bcf80bc829a63ab5b1724bf`. Both NOT pushed; full architecture remains unaccepted. Latest verified safety commit `01deb7d6475d230029a5e17d3f57771f6026fdab` committed/pushed and remote verified; main unchanged. Measured-study checkpoint `a364f7809b75a2a1e8db887373dccff418289a5f`. Latest known-good gameplay code milestone `6874769a6c349ffb38926d5ec2f39dc6dec39548`, committed and pushed, remote verified. Earlier safety milestone `39463d22b7fe4b0a7b4f35d56e50a355485af914`; integrity/baseline `c506e58`. Main/origin main remains `1632ff25f1178c5dd0a7617e9a6fdcc329530344`. No history rewrite or force push.

Checkout/opening/input P0/P1 regression work is complete at 6874769. Return to master phase order: baseline truth audit and selected art targets → entire architecture/storefront/customer flow → visible assets → geodes/core/audio/UI/progression/performance/full QA. Do not keep polishing checkout or import the parked bench before architecture.

## Exact unfinished operation

QA23 `237a3e02af324739b0cfe8486e6e7d41` FINISHED2026-09-06T22:56:16.128638Z. All23sessionsfinished, overridecleared, guardTRUE, candidateEditModeclean. Actual3originalhashes/productionWorkshop recheckedunchanged. No input/test/build/image/Blender/coroutine/tooljob running. TemporaryPlaynavmodifier/voxel/door changes reverted natively:0leafmodifiers,doorenabled/closed,surfacevoxeloverridefalse. Compilationclean. ParkedBlenderunchanged.

EXACT NEXT: production fix for PROVEN architecture P1 navigation and repeated-placement autosave, then isolated regression. Three navcauses: closed animated door permanentlybakedsolid; defaultsurfacevoxel.1667closesnarrowentry; moving/gatedfixtures have no navupdate/proxy. Fixwith doorleafNavMeshModifier(ignoreFromBuild), .05m surfacevoxels, moving/gatedgeometryexcludedfromstaticbake with carvingfootprintproxies followingfixtures/hoardings/countergates. Keep physicalcolliders. QueueSave onEVERY successful BuildMode.TryPlace, independentof completedtutorial. Do not callnavigation/architecturePASS until fresh/expanded/customlayout and livecustomercontrols pass.

Causal QA23 evidence: playermovedaway samplevalid butoutside→door/counterpathsPartial. OpenleafonlystillPartial. RebuildomittingleafdefaultvoxelstillPartial14.62ms. Omitleaf+.05voxels→Complete29.19ms. Negativefinevoxel+closedleafincluded→Partial. TemporaryNavModifieronleafphysicalcolliderretained+.05→Complete23.38ms. EarlynavReadvoxeloutputreadglobalsettings; finalGetBuildSettingsreadshowsactualsurface.05. Actualnormalwashmove(-5.75,0,3)90→(-1.5,0,3)270 valid:oldcentrecontinuednonwalkable/newsolidcentrecontinuedwalkable. NofixtureNavObstacle. Afteridle, diskstilloldpose/liveatnewpose; buildlessonalreadydone. TryPlaceonlyqueuesindirectlyviaTutorial.Notify. ARCHITECTURE_NAVIGATION_ROOT_CAUSE.json.

TemporarycorrectednavigationPlay: explicit synthetic opened unappraised seeds2/4 acceptednormalstarterSaleSlotsasking4.95/1.95. Oversizedseed1337correctlyrefusedsupportboundsandretainedindealertray. Customersnaturallybrowsed/selected/queued. ClosingnormalSetOpen refusednewarrival; existingcheckout diagnostic harnesscompletedone4.95sale,cash2310→2314.95,sameS0002-0002sold/despawned. Twoothersabandonedwhileautomationwaited; final0customers/0stuck/0reposition/0pathfailure. This is targetednavigation/closingevidence, NOT primarymanualcareer/fullinputcustomeracceptance.

Starterguidance/source NOW nativeverified QA23: freshcash120/counteronly/HUD Build your business/Order your firstlocalcrate; brushrequiresWashStation/sawBackRoom. Deferred wash/appraisal/collection/saw/lap lessons waitforowned+sited; implicitcompletiondoesnoteraseunavailablelessons. Futurewashsurvivesreload test. Equipmentshownowned-needs-placement. Catalogue leasearea/capacity/worldeffects correctedwithoutpricebalancepass. Eightguidance+fourreceiving testsPASS12/0/0,.7s jobdca32dbf-71e5-4216-aeed-97bd24e104bc. Initial11/1 failure was testfixturemissingSaveId; fixedvalidinput, originalreportretained.

QA23 equipment diagnosticcash3000, normalBackRoom550+Wash140→2310. Deliveredwashparcelat(-4.15,.12,4.47)typedDeliveryCratecorrect;Bodyabsent. Parcel.Interactselectedwash. ProductionnegativeTryPlaceovergoods-inrefused. RealcontrollerSouthconfirmedvalidsnappedpreview(-5.75,0,3)90 with explicitlycontrolledcamera/temporarilylockedlook. Savedposeexactreload,lessonavailable,4freecells/no parcels. ExitAPIusedbecausefocuspreventedEast; controllerrestored. FirstevidenceserializerfailedonVector3AFTERpurchase; nativeonlyreadrecoveredresult, purchasesneverduplicated. ARCHITECTURE_STARTER_GUIDANCE_EQUIPMENT.json.

Practical lighting QA22 `643cf00f633e4088bc7a909d63e09ed7` FINISHED22:19:31.351678Z. Sevenbattens/3shadowed; backroomprocessing+office,shopfrontshowroom. Fresh2active/mature7active, 15Bodies. Gallery3spotsnowonlycentralshadowed. Normalcontrollerpurchase/open120→45,C001nineoriginalspecimens at(-1.2,.12,-2.05), wholecareerexactreload/9entities. Precise laterpickupwasfocuslimited,noPASSclaimed. Use controller-crate-open-confirmed.png; earliercontroller-crate-open.png wasclosed. Nativecamera978×519 vsGameView1956×1080 at2x, NOTFHDperformance/publiccaptureacceptance. SingleEditorsnapshots fresh9stockCPU12.14/GPU5.82ms,mature5.50/5.27ms. Lightingreadabilityimproved, olddarkbrownwarmfixturesstillbelowconcept; finalBlenderlamps/materials/shelf-lightconsolidationPhase4. ARCHITECTURE_PRACTICAL_LIGHTING.json.

Allcandidate one-shots ALREADYRUN andsaved: CreateAstraWorkshopCandidate, FinishAstraCandidatePrimaryLayout, ConfigureAstraCandidateMinimumKit, AttachAstraCandidateOpenSigns, ConfigureAstraCandidateLateFixtures, ConfigureAstraCandidateMachineBodies, ConfigureAstraCandidatePracticalLighting. Neverrerun. Separate save_scene afterbuilder Undoresolveddeferreddirty. Candidate NOTinbuildsettings, Workshopunchanged.

Prior verifiedPhase3cases: QA16exactlegacy18records/17live/oneSold/oneWash/twoopencrates/3stations meaningfulfields/cash/bills/rentunchanged, fullrepeatloadexact. QA17–20all15relocations/recoveryidentity+collection+naturalhalfpose/nooverlap/retailaskingreset and37%sawcut preserved. CausalstalelockbeforeRebuildfixedbyRefreshOwned inApplyVisibility; controllednormal/stale-lock loadsPass; temporarylogsremoved. QA21machineBody/small-large/sharedtrayoutput/gatingPass; 15placementsvalid,256staticcolliders37crossfixture/fixedscenerypairs,0unsupported/0overlap>1mm. All91zoneIDs/3DeliveryIDs166829470/1990628459/616842567preserved. Do not redo absentnewregression. Reports LEGACY/RECOVERY/SAW_RESTORE/MACHINE_GATING inDocs/AstraRework.

ProtectedSHA256: career7741f17c948a3fc20d053867ca9ba726dc42b6807a7442824f2646b6d4a58946; backup3ee18e29db190dad365f0254eeb8166c42d33f4cdb39bedda12ee40dcf74032f; settingsb18f7ce0a4380393f11cec0a10190fdcaab62e359206c404b01ad120bb642340. Workshopffaf74cd0e9097457ac40e826c1442ce35b901f18096fed27d8402ebc2d125fc. DisableDomainReload+DisableSceneReloadunchanged. No newplayersafetyincident. Historicalreal21:13:25writeinitiatorUNPROVEN,newerfilespreserved.

CaptureonlyUnityoffscreenwindows. GenericbundleactivationcanfocuswrongEditor; pinnedport9015lastPID81300. Focuschangeswithconcurrentuserinput; neverinferinputsuccessfromscheduling. No desktopcapture. SourceeditsrequireExit/Finishfirst; allcurrentlystopped.

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

Unity6000.6.0f1 MCP responsive, correct Geode project. Edit Mode, clean saved Workshop_AstraCandidate; not compiling/updating. Candidate startup enabled, production Workshop unchanged. No build settings promotion. Original Play options DisableDomainReload+DisableSceneReload. Twenty-three finished isolation sessions, override cleared. Automation guard persists TRUE. Protected real hashes above, never replace with historical hashes.

Recovery boundary: after prior QA finished at 19:14:39 local, the real career ran/migrated for about eight seconds and saved at 19:14:53–54. Trigger unproven, predates subsequent recovery compile/tests. Same real career/cash/count; newer real files preserved, never rolled back. `PLAYER_DATA_RECOVERY_BOUNDARY.json`. Historical first-nine-session career/backup hash `d45f8931de0d03eb7bba7ccf024b9162fa5f5cd0d2f54f0139a4eb01bc462694` is NOT current player state.

Every Play: Prepare → verify exact career/settings/override and protected hashes → Enter separately. Failed/timed-out preparation prohibits entry. Exit → Finish → actual original hashes and scene check. Never write real `/Users/kenneth/Library/Application Support/DefaultCompany/Geode` files. Standalone needs equivalent explicit isolation.

Blender MCP responsive, 5.2.1 LTS, addon 1.6/protocol 5. Scene `Astra_Bench_Review_01`: bench + seven proxies; original Scene and Astra_Baseline_Bench_Audit preserved. `Tools/blender.sh` tested exit 0. Draft `ArtSource/Blender/Props/prop_workbench.blend` and ignored `Tools/Blender/Output/AstraBench/prop_workbench.fbx`: 4346 vertices/8508 triangles, 1.8×0.749×0.9 m, four material slots/seven boxes. PARKED, not imported or accepted. Later import must update all four material assignments and correct proxies in the existing builder.

## Tests and unresolved defects

Latest scoped job `59eb3483-008a-4a28-9e0f-25c30770ed88`: FINISHED 50/0/0, 3.83 seconds, 19:35:04 local. Earlier 44-case job f491f7b7-1dd6-478c-a491-a14c9d36272c also passed. CheckoutMoney/Flow/RegisterTransaction/Presentation, ShopHours, SaveMigration, SettingsIsolation, StationInput. Compilation clean, no warnings; cached runtime errors absent in last 1800 seconds of the audit.

Full baseline: 135 cases, 134 pass, one P1 timeout ProcessingChoiceTests.HammerVsSawReport (371.57s vs 180s limit), full run 847.81s. BuildPiece 496–776ms vs whole Build 187–255ms; repeated noise-backed surface rays are the lead. A slower inlining trial was discarded. Do not raise timeout, weaken samples or hide hitches with animation delay. Fix in geometry/performance phase.

Original production Workshop still has wrong starter kit/architecture; candidate fixes are unpromoted. Current whole-game defects: misleading starter station guidance; inverted brush/contact presentation; dirty wash occupant always resumes despite “tap to take” promise; wash Escape can also pause (PauseMenu excludes wash from BenchActive); giant UI/HUD blocks; geometry hitches; NPC mannequins. Full machine outcomes, four-layout customer stress, no-cheat career, full controller/KBM, standalone and performance remain unaccepted.

Audio clips exported read-only from the pure bank; `AUDIO_BASELINE.md`, `Baseline/audio-review.mp3` and index. The tool cannot provide audio input to this model, so perceptual listening is explicitly NOT complete. Source confirms constant nonspatial filtered-noise/60–120Hz ambience. Do not claim audio sounds good/bad or passes from code/RMS. A supported listening review is required later.

## Concepts and design decisions

All image jobs finished; no cells to poll. Originals retained under `/Users/kenneth/.codex/generated_images/01a075ac-c9a6-78b2-bf1d-9f7213c7fd8c/`. `Docs/AstraRework/Concepts/`: day-one-a, day-one-b, storefront-early-retail, cracking-inspection, wash-cracker, saw-lap, midgame-mature, receiving-storage, checkout-laptop, collection-shelves, ui-suppliers-equipment, ui-collection-business, ui-overview-inventory, ui-stats-premises-bills, processing-workshop, ui-owned-inventory (all .png).

Day-1 B: pale plaster, blue steel #2E4D58, cream, restrained oak, concrete, neutral daylight and limited warm task pools. Real entrance and ~1.2m public route, separate staff access, one drawer, modest laptop. All later machines/dedicated inspection/storage earned. Three angled supports touch the actual rock. Shallow wash with connected plumbing. Hydraulic cracker B, saw blade in YZ plane with feed +Y from operator -Y, horizontal lap. Two usable receiving bays; no center cart, giant repeated logos, ornate gold UI or invented data. Inventory B list/detail shows actual owned identities; do not teleport stock through a generated “Move to storage” button.

Historical master, final/Fable/V4/V5/V6, starter/core-loop specs and principal reports read; visual/reference specs fully read. Long checkout handoff has partial technical coverage recorded; its implemented port contract has been audited against the current source and targeted transactions. Old completion claims remain historical.

## Exact next actions

1. Fix proven entrance/coarse-voxel/dynamic-fixture navigation and repeat-placement autosave defects, then isolated before/after controls and natural customer routes. Starter guidance/lighting are locally verified.
2. Native isolated fresh guidance + actual finite delivery/unpack/player placement, positive/negative route, natural customer arrival/door/open-close/exit and minimal selling checks. Fix actual Phase3 blockers, avoid subsystem polishing.
3. Commit coherent accepted architecture and promote candidate through Unity preserving references. Continue entire Phase4 Blender manifest, then all remaining master phases. Final art/geodes/audio/UI/customers/economy/performance/fullQA are not accepted. Main remains safe.

Resource/workflow:8GB M2; heavy Blender/Unity tests/builds sequential. Narrow project logs, never broad process arguments. Source edits → immediate request_recompile → wait/recover → errors. Timeout → inspect underlying process before retry. Diagnostics with injected states are not the primary no-cheat career. Native Game captures use explicit window dimensions, focus=false.

## Modified/uncommitted files

Since local7df7e39: lighting builder/candidate, runtime Tutorial/Progression/UpgradeCatalog/HudController/TabletUI/PlaceableFixture/GameSession guidance and ownership copy, new AstraStarterGuidanceTests+meta, lighting/guidance/nav-root-cause/test/isolation evidence and PROGRESS. About to create local partial checkpoint before navigation fix; do not promote/push as accepted architecture. Parked/unrelated: M_Felt.mat legacy _Color sync to unchanged _BaseColor, Tools/Blender/gen_props.py, ArtSource/Blender/Props/prop_workbench.blend. Do not blindly stage parked work or patch Unity YAML. No asset import/Blender acceptance. Candidate serialization whitespace is left as Unity saved it; source/doc diff checks required before commit. Do not push/promote QA-incomplete architecture as accepted.

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
