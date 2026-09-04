# Geode Empire V5 — Final Report

## Completed
- Phase 0 `64bd494` V4 baseline verified. Phase 1 `1956919`/`1950c20` hero assets, Blender `hq.py` + 49-prop `gen_props.py`, SSAO, world-integrity audit, full-footprint placement. Phase 2 `143ab6f` specimen rarity and uniqueness (16 families, traits, localities). Phase 3 `1d446f2` specimen-specific preparation (stability, tilt, clamp, clay, chips, rind, rinse). Phase 4 `8db64c2` interactive lapidary saw (14-inch trim saw, vise, wheel, coolant valve, single-pass rule, kerf, pieces). Phase 5 `a49c2d6` hands-on processing feedback (ease-in swing, hit-stop, crack sweep, weak bite / no progress / internal damage causes, polish care, wash rinse). Phase 6 `60a41f1` verification and provenance (call-before-open, UV certification, Stage-2 cracker, history log, museum labels). Phase 7 `87b879b` sourcing and selling career (12 suppliers incl. occasional lots, commissions, reputation, collection goals, favourites, customer variants, register drawer). Phase 8 `533d5e9` Stage 3 and endgame (slab saw, UV lamp, third pallet, gallery plinths, second case, exhibition with camera pass and summary, music bed). Phase 9 `8775db4` UI and presentation polish (physical-first bench panel, centre-slot rule, sectioned specimen card, shelf labels, custom names, 14 px minimum type, resolution sweep, auction channel). Phase 10 `f359e27` rarity and career economy balance (eight deterministic balance gates, quarry crate value floor, world-class made a career event, damaged lot repriced, one-more-rock cues). Phase 11 `dde7597` persistence, controller and standalone (save version 2 with V4 migration, exhibition reset on load, controller pass over the collection page, standalone build).
- Phase 12 closure (this commit): four-grade tap that a thick shell or clay can mislead, locality in the hand reading, a bench shim under a rocking rock, a standalone harness hook (`-geode-run`), ten clean-start careers, retail stress with perf samples, this report.

## Visual fidelity
- Hero assets rebuilt in Blender through `Tools/blender.sh`: `hq.py` (bevels, lathes, tubes, lofts, weighted normals, collision proxies) and `gen_props.py` (49 props: trim saw with blade/vise/wheel/needle/valve/nozzle, 24-inch slab saw, cracker with chain and lever, lump hammer, fine chisel, wedge, register with sliding drawer, plinths, UV lamp, customer part variants, furniture and fixtures). Geode meshes 64x20 rings with billow rind, lobed cavities, conchoidal fracture rims and patchy crystal density; crystals from `gen_crystals.py` (24 habits). Shaders: `GeodeShell` (dust, kerf masks, edge bruises, wet, clue tint) and `GeodeCrystal` (dust, damage stubs, polish gloss).
- Judged against the owner's screenshots this is still not the production bar: the saw reads boxy and dark, rough geodes read soft, opened cavities read shallow and sparse. V6 treats these as the negative baseline (see V6 recommendation).

## Processing
- Bench: seat quality from the hull's stance (tilt with Move, clamp as an act, shim on Drop), clay hides the seam and cushions blows, natural chips are starters, rind texture crumbles, force zones read on the panel, wind-up and hit-stop, crack sweep along the seam, failure causes named (off seam, glancing, unstable, clay, thick shell, light, heavy, thin shell, overstrike, wedge).
- Saw: clamp with the wheel, yaw/roll/offset plan, single-pass arbor rule with tilt-to-fit and a refusal for rocks that cannot pass, coolant valve (dry/drip/flood wear and chip rates), load from the sampled lobes on the machine's needle, grip and shift, kerf that grows, two pieces with lineage; the Stage-3 slab saw passes 25 cm.
- Cracker (Stage 2): seat, take up the chain, squeeze, slip when misaligned, split along the seam with lower crystal damage.
- Polish: rate, gloss ceiling and care per family, pressing, heat, edge chips, wet slurry. Wash: scrub and a post-open rinse that clears dust for the beauty view.

## Rarity
- Regular sources over 1,000 rocks: common 42%, decent 33%, good 17%, exceptional 6%, museum 1.3%, world class 0.2% (world-class weights cut to 0.03-0.2% per rock on regular sources, 0.3-0.5% premium). Premium sources raise exceptional-or-better to 14-20% without museum grade exceeding 4.4%. Jackpot crates (8x price or $800) under 3% for every source; the biggest single crate stays under a world-class find.

## Rock lifecycle
- Receive (locality, lot, acquisition cost, original mass) -> wash -> inspect in hand (size, weight, clay, shell notes, locality, tap with four grades) -> optional call (hollow/solid, tier) -> prepare (tilt, shim, clamp) -> hammer / saw / cracker -> reveal with dust -> rinse -> appraise (look, condition, process, value reasons, call scored, UV certification at Stage 3) -> keep (cabinet, trophy wall, plinths, favourites, custom names) / dealer outbox / showroom / commission / auction -> catalogue with a full event history.

## Career
- Suppliers: local, regional, amethyst, estate, premium, cutting, desert, oversized, network, showcase (occasional), damaged (occasional), specialty; unlock rules on sales, prestige, stage and reputation. Commissions every four sales (two open), occasional lots every four crates. Reputation score and tiers gate Stage 3 and the auction house. Ten collection goals with met/near cues. Stage 2 (cracker, trophy wall, rack, shop shelf) and Stage 3 (slab saw, UV lamp, third pallet, gallery, second case). Endgame: the Curator's Exhibition across seven axes, three pieces on the plinths, a camera pass and summary; the save carries on.
- Pacing (15 min a crate): Stage 3 lands mid-career, the full catalogue inside 25 hours, late crates and blades still take a quarter of late income; no strand across 24 simulated worlds thanks to the quarry crate floor.

## UI, audio, NPC
- HUD: physical-first panels (no bars), one bottom-centre message at a time, shelf labels under the prompt, sectioned specimen card, 14 px minimum type, verified at 1080p/1440p/4K. Tablet: suppliers with variance tags and "$N more", upgrades, collection (goals, favourites, names, consign, lots), stats, exhibition button. Letters for invitations and auction results.
- Audio: synthesised bank incl. coolant hiss, slab place, cracker creak/tension, music pads (calm/work) with a slider. NPC: customer part variants (hair, cap, beanie, coat), rare-specimen reaction, carried-out purchases, register drawer and screen motion.

## Persistence
- Save version 2 with `SaveSystem.Parse` + `Migrate` (V4 files gain locality, acquisition ticks, original mass and non-null V5 lists; lineage intact); consigned pieces skipped by the loader; exhibition director resets on load; mid-pass exhibition reload, mid-lot auction reload and saw/wash/bench/appraisal interruption scenarios verified; SaveMigrationTests and SaveSystemTests green.

## Verification
- EditMode: 35/35 (economy simulation, economy balance incl. auction, rarity, processing choice, preparation, prep matrix, save system, save migration, specimen generator, stress model).
- Play Mode harnesses (all logged, Console clean): RunPrepRock, RunSawCut (incl. tall/dry), RunCracker, RunCallTest, RunMarket, RunStage3 (audit 0/0/0, exhibition held, mid-pass reload), RunAuction (consign/withdraw/collect/reload/sale/pass/return, collision audits 0), RunUiSweep at 1080p/1440p/4K, RunControllerMenus (collection buttons reachable on the pad).
- Careers A-J (clean start, 7-8 min each): all ten ok, zero duplicate entities/records, zero orphans, zero exceptions.
  - mixed: 10.9 min, opened 19, sold 14, upgrades 7, stage 0, reloads 0, rescues 1, dup/orphans 0/0/0, ok
  - hammer: 11.3 min, opened 18, sold 16, upgrades 0, stage 0, reloads 0, rescues 0, dup/orphans 0/0/0, ok
  - saw: 14.6 min, opened 20, sold 12, upgrades 4, stage 2, reloads 0, rescues 3, dup/orphans 0/0/0, ok
  - collector: 10.7 min, opened 19, sold 17, upgrades 0, stage 0, reloads 0, rescues 0, dup/orphans 0/0/0, ok
  - seller: 12.2 min, opened 19, sold 18, upgrades 0, stage 0, reloads 0, rescues 0, dup/orphans 0/0/0, ok
  - poorsaw: 12.1 min, opened 17, sold 15, upgrades 2, stage 2, reloads 0, rescues 1, dup/orphans 0/0/0, ok
  - saveheavy: 11.3 min, opened 19, sold 17, upgrades 2, stage 2, reloads 2, rescues 0, dup/orphans 0/0/0, ok
  - controller: 11.3 min, opened 19, sold 17, upgrades 7, stage 0, reloads 0, rescues 1, dup/orphans 0/0/0, ok
  - careless: 7.3 min, opened 10, sold 8, upgrades 0, stage 0, reloads 0, rescues 0, dup/orphans 0/0/0, ok
  - mixedlean: 11.7 min, opened 19, sold 13, upgrades 0, stage 0, reloads 0, rescues 0, dup/orphans 0/0/0, ok
- Retail stress 16 min: 60 spawned / 40 served, 0 queue stalls, 0 path failures, longest counter wait 3.2 s, collision audit 0; 3 soft overlap loops (>2.5 s) where a leaving customer squeezes past browsers at the wall case, 1 stuck event (4.0 s, self-recovered). 8-minute rerun after moving the island table: 32/24 served, 0 stuck, 0 stalls, collision audit 0, 3 loops at the same lane (V6 navigation item).
- Perf sample (Editor, retail stress): about 830-870 draw calls, 150-180 set-pass calls, 5.6-5.7 M triangles, 8.5 M vertices, 730 MB allocated / 1.77 GB reserved, 16-33 fps in the Editor at a fixed 1080p game view (the standalone runs lighter; V6 needs LOD and instancing for the specimen meshes).
- Standalone macOS build: 102 MB, 0 errors, 2 informational warnings (pre-baked mesh collision, Pipeline runtime config); boots Title and Continue; with `-geode-new -geode-run RunStage3` the real build ran the whole Stage 3 routine (world audit 0/0/0/0, slab-saw cut, UV certification, plinths, invitation, mid-pass reload, exhibition held, save/reload 0 mismatches) and `-geode-run RunCallTest` (clay-muffled tap, call scored on the card); captures at 2940x1912 show the card clear of the tutorial card.

## GitHub
- Branch: main
- Final commit: `5a79e07` Complete V5 final QA (V5 milestones: 64bd494, 1956919, 1950c20, 143ab6f, 1d446f2, 8db64c2, a49c2d6, 60a41f1, 87b879b, 533d5e9, 8775db4, f359e27, dde7597, 5a79e07)
- Remote: https://github.com/kennethcxv/Geode.git
- Push status: pushed; origin/main == 5a79e07
- Working tree: clean apart from the untracked `GEODE_EMPIRE_V6_PRODUCTION_ALPHA.md` (the V6 brief, committed at the start of V6)

## Known limitations
- Visual quality is below the owner's bar (the V6 negative baseline): saw and machines read boxy and dark, rough geodes soft, opened cavities shallow and sparse, materials mostly colour-driven.
- Slabs display flat on the shelf (no upright easel); calipers are not a tool (the card carries size); no LOD strategy yet (the scene stays inside the M2 budget as measured).
- Custom names are keyboard-only (the pad opens the field; typing needs a keyboard).
- Harness walker occasionally wedges on the stool or wash stand and teleports (logged as walk rescues); not a player-facing issue.
- Audio remains synthesised, not recorded.

## V6 recommendation
- Follow `GEODE_EMPIRE_V6_PRODUCTION_ALPHA.md` in its priority order: geodes and materials first (Blender rebuild of rind, cavity, fracture, crystal habits; PBR material families with normals and microdetail), machines remodelled with manufactured detail, lighting rebuild, then the physically correct cracking sequence, NPC rebuild, Golf-Simulator-quality checkout, tutorial and UI system, audio/VFX, economy and world evolution, persistence/performance/standalone QA.
