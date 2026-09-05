# Geode Empire V6 — Living Report

Authoritative brief: `GEODE_EMPIRE_V6_PRODUCTION_ALPHA.md` (repo root). V5 baseline: commit `5a79e07` ("Complete V5 final QA"), report in `Docs/GEODE_EMPIRE_V5_FINAL_REPORT.md`.

## Current phase
- V6.1 material pipeline + geode hero quality (in progress). V6.0 (`1e923ff`) holds the baseline; V6.1a (`07186f9`) delivered the generated PBR pipeline (Tools/Blender/gen_textures.py, 16 tileable families, import rules, set-aware workshop materials).
- V6.1 plan (from a five-agent code map + synthesis, executed in order): S1 specimen tiles + Blender review renders; S2 GeodeShell triplanar detail normal/mask (done, first pass); S3 five-layer rim profile (done, first pass); S4 sawn/polished/wet/dust responses + SSAO keyword (partly done); S5 exterior macro asymmetry, resting flat spots, pits (done, first pass); S6 satellite cavity lobes, deeper display half, thinner sectors (lobes + depth done); S7 conchoidal rim ripples/terraces, more rim rings (ripples done); S8 clustered buried crystal growth, no floating/crossing tips (clustering + scale done; containment pending); S9 crystal archetype remodel + LOD budgets in Blender; S10 per-mineral crystal identity (luster/F0, transmission, zoning, inclusions); S11 SpecimenVisual hygiene and perf gating; S12 machine remodel scaffolding (worn sets, hard-surface helpers, saw pilot); S13 acceptance gate (geode matrix, tests, standalone, report).

## Baseline (negative) screenshots
- `Docs/V6/baseline/` — player-camera captures of the V5 saw, rough geode, opened geode, bench, wash, polish, appraisal, checkout and showroom at 30 cm / 60 cm / interaction / room distance. These are the pictures V6 must unmistakably surpass (§129).
- `Docs/V6/v61/` — the same four hero seeds (amethyst 7D1, vanadinite ACC, agate E53, rhodochrosite 8BF) after V6.1b, in four states each: quarry-dirty, washed, opened dusty, rinsed (two angles). Compare `v5_b_open_7D1_a.jpg` (grey chunks on a flat lavender floor, heavy dark rim) with `v61_b_open_7D1_a.jpg` (purple points with pale bases over a purple druse floor inside a grey chalcedony ring with fine wavy bands), and `v5_b_rough_7D1_b.jpg` (dough) with `v61_b_rough_8BF_b.jpg` (pitted, knobbly rind).

## V6.1b — geode hero pass (2026-09-04)
- Materials/tiles: `gen_textures.py` gained `worley_vec` (offset to the nearest seed), a botryoidal `rind_weathered` (spherical knobs at two scales, chalky dome tops, dark creases, sharp pits with dry rims, sparse hairline cracks) and a new `druse` tile (a mosaic of six-sided terminations at random heights and turns, glassy, near-white; the shell shader tints it). Normal strengths are now physical: `tan(slope) = 2 * strength * height-per-pixel`, so rind 16 / fracture 10 / cavity 8 / druse 18 (the old 1.0-2.2 gave about four degrees of tilt, which is why V5 relief read as clay).
- Shell shader: druse detail set blended under carpets by `_CavityDruzy`; the cut face is chalcedony-dominant (thin exterior skin, weathered matrix rind 0.06-0.32, grey-blue chalcedony with coarse + fine wavy bands, mineralised inner edge), sawn faces share the layering; rock flour is a pale powder in the low ground; seam / lip / guide widths scale with the rock radius; exterior mineral hints are a tint, not paint; pits come from the rind tile's occlusion; a `DepthNormals` pass (SSAO's normals prepass) in both geode shaders; the triplanar frame fixed (x plane samples `zy`, u mirrored on back faces, tangent x flipped); `_GeodeDebug` modes 1-5 (albedo / tile albedo / y-plane uv / blend weights / normal).
- Crystal shader: body keeps more of the surface tint (no more black glass), pale milky bases under zoning, ambient scatter, lighter dust, DepthNormals pass, glints from the smooth noise channel.
- Mesh: 96x30 rings per half, 12 rim rings (13,632 triangles per half shell); displacement band-limited to about six ring samples per noise cycle (lump 1.3-2.2, bump 2.0-2.4, billow 1.8-2.4 with squared domes, pits 2.2); satellite lobes etc. from V6.1a kept.
- Asset builder wires every detail set from `Textures/Generated` (`WireDetailSet`); the legacy `T_Rock` crack lines softened.
- Harness: `hero_bench2.sh <seeds>` captures dirty / washed / opened-dusty / rinsed (sets `Condition.Cleaned = 1` and `Condition.Rinsed = true` then `RefreshCondition()`); `diag_streak*.sh` capture debug modes at 2560x1440 and crop the rock.

## V6.1c — crystal habits and carpets (S9, 2026-09-04)
- `gen_crystals.py`: prism striations dropped from the meshes (the shader's `_Striation` carries them), so a quartz point is 70 faces / 136 triangles instead of 178 faces; the botryoidal tile went from 3,546 to 906 faces; a new `crystal_quartz_termination` habit (short buried prism, tall six-face termination with alternating steep/shallow faces) = `CrystalArchetype.QuartzTermination` (25 archetypes, library rebuilt).
- Placement (`GeodeMeshBuilder.PlaceCrystals`): carpets sample a 56x20 cell grid (others 40x14), points 0.56 of the family scale, size variance 0.78-1.28 with 4% giants x1.3-1.7 and 20% runts, quartz points on the fringe swap to terminations (75% on the fringe, 25% in the cores), tilt <= 12 degrees, spacing 0.36 so points touch, fill probability x1.15; burial capped by the local wall thickness (`Cell.Thickness`). The hero amethyst went from 384 loose chunks to 788 packed points at fewer triangles than before.
- Crystal shader: every light-keyed term (glints, transmission, rim, the new cloudy scatter fill) now accumulates over the additional lights through `LIGHT_LOOP_BEGIN/END` (the bench lamp is an additional light; the sun never reaches the bench), milky bodies keep their hue, the pale-base fade sits only in the bottom 40% of a point.
- Shell: the druse floor is satin (smoothness 0.62 under carpets) and paler/less saturated than the points, milkier for cloudy specimens (`SpecimenVisual` blends the druse colour toward white by clarity).
- Perf with the 788-point amethyst opened in the player's view at the bench (Editor, fixed 1080p): 287 draw calls, 58 set-pass calls, 1.50 M triangles across all passes, 637 MB allocated, 56-59 fps.
- Captures: `Docs/V6/v61/v61c_*.jpg` (amethyst 7D1 opened two angles + dusty, rhodochrosite, the cloudy thin-shelled amethyst 2B77E opened two angles + rough).

## V6.1d — family-wide review (2026-09-04)
- `ContactSheetGenerator.FamilySheetsAll()` rendered all 24 families (Geode/Output/family_*_interior.png, git-ignored). Findings: the quartz carpets, pyrite, citrine, agate and rhodochrosite held; scattered / clustered / spray families (fluorite, calcite, celestite, stilbite, halite, chalcopyrite) sat as a few hero-sized crystals in bare bowls; malachite's rounded growths rendered white; every sheet was pale.
- Causes and fixes: the sheet renderer built opened rocks unrinsed (the 55% rock-flour film on every cell) — it now rinses opened cells; the cloudy-milk blend and the pale-base fade applied to opaque minerals — both gated by `_Translucency`; scattered / clustered vugs now sample the fine 56x20 grid at half the hero size with 0.9x density, and every non-carpet vug gets a pale quartz druse lining (`CavityDruzyAmount` 0.55 scattered / 0.5 sprays / 0.35+ clustered, `SpecimenVisual` tints the lining toward quartz white).
- Balance consequence: the hammer-versus-saw test dropped to 7 hammer wins in 120 because two sawn halves valued at 1.11x the whole rock (the `PieceValue` retained-crystal exponent 0.85 rewarded any split) and small uniform points no longer lose anything to the kerf. Fixed at the model: `BuildPiece` marks points rooted within 4 mm + 0.6 footprint of the cut plane as ruined (40% weight), `PieceValue` is linear in retained weight with an 8% face premium (opening x symmetry) in place of the exponent. Result: base $66, hammer $66, saw centre $70; saw better on 23 rocks, hammer on 16 (big-crystal families), the rest within 5%.
- Saw review (fresh game, saw bought, `saw_views.sh`): no longer the V5 green box, but every part wears the same olive paint (motor, hood, brackets, vise, cabinet), no wear or coolant staining, a flat blade with an outlined rim, a capsule motor, a thick banana hood. That is the V6.2 brief: a worn-paint material (vertex-colour wear masks baked in Blender, paint-to-metal edge wear, grime in cavities, coolant staining), distinct part materials (cast iron, aluminium, stainless, plastic, rubber), and geometry passes on the blade (plate, gullets, label), motor (fins, fan cover, terminal box, nameplate), hood (sheet-metal guard), vise (serrated pads), cables and hoses.

## V6.2 — machines (in progress, 2026-09-04)
- V6.2a done (this commit): the wear bake (long edges subdivided to 4.5 cm cells so panels get interior vertices; convex edge-ness from an 8 degree threshold over a 25 degree span so 3-segment bevels count; recesses from concave edges plus a 14-ray BVH occlusion cast; up-facing in blue; exported linear), `GeodeEmpire/WornSurface`, the `Worn(...)` materials, the saw remodel, blade segments and a single label ring, `M_Coolant` slurry, the saw bay lamp at 2.2, the cracker lever's grip on its bar (it floated beside the head), the lap pan floor lifted 3 mm off the cabinet top (coplanar faces z-fought into a camouflage blotch), all 60 props regenerated with the bake. Captures: `Docs/V6/v62/v61_saw_*.jpg` (before) against `v62a_saw_*.jpg`, plus `v62a_cracker_*`, `v62a_lap_*`, `v62a_wash_front.jpg`.
- Plan: (1) `Tools/Blender/hq.bake_wear` bakes per-vertex wear masks into every prop (R convex edge-ness after subdividing long edges so panels have interior vertices, G recesses from concave edges + a 14-ray BVH occlusion cast, B up-facing; exported as linear vertex colour); (2) `GeodeEmpire/WornSurface` shader: paint or cast tile over a bare-metal tile, edge wear and chips, grime in recesses, streak staining down the sides, dust on tops, tangent-space normals, DepthNormals pass; (3) scene builder `Worn(...)` materials `M_MachinePaint` (olive paint over steel), `M_MachineIron` (cast iron), `M_MachineAlu` (aluminium), plus `M_Nameplate` and `M_BladeLabel`; (4) the trim / slab saw remodelled in `gen_props`: cast-iron motor with fins, end-bell bolts, a slotted fan cowl, terminal box, cable gland and cable run, a nameplate, cast pillow block with a grease nipple and webbed pedestal, belt on the pulleys, an aluminium sheet guard (3 mm walls, cheeks, a viewing window, bracket bolts), a plastic switch box, panel trim lines and a cabinet nameplate; blade with 24 diamond segments and gullets plus a printed label ring; aluminium sled on the vise. (5) Then the cracker, polish lap, wash tub and register on the same materials.

## V6.3 — fracture and reveal motion (2026-09-04)
- Frames through a staged split (`reveal_frames.sh`, Output/captures/reveal) showed the V5 defects exactly as the brief describes them: the top half swung about its rim with a fixed 0.28 R lift, so the dome swept through the cradle cushion at a quarter second; it snapped from closed to moving; it stopped dead on landing; the reveal light and the task lamp blew the cut face to white.
- `CrackingBench.RevealRoutine` now: a 15% "give" beat (millimetres and three degrees along the seam) before the flip; a per-frame hold that keeps the half's true lowest point (`LowestOfTop`) above the seam plane while it is still over the cradle, fading as the slide carries it clear so it can drop to the bench; a small toss; a damped settle about the contact edge on landing (five degrees, 0.42 s); the reveal light at 0.5 / 0.9 (rare). Captures: `Docs/V6/v63/`.

## V6.4 — lighting and presentation (2026-09-04)
- Ambient lowered and cooled (sky 0.36/0.39/0.46, equator 0.33/0.30/0.27, ground 0.14/0.125/0.105) so the lamps carry the room; pendants at 2.5 with a lit enamel inner shade (`M_ShadeInner`, warm emissive). Before/after overviews in `Docs/V6/v64/`.
- The locked saw's placeholder (a flat green rounded box: the "fridge") rebuilt as a canvas dust cover: a loft that follows the machine under it (cabinet, guard hump, motor peak), wrinkles, a pleated skirt to the floor, a rope and a paper tag; a new `canvas` weave tile (plain weave, slubs, faded khaki, grime in the folds).

## V6.5 — customers (2026-09-04)
- `gen_props.customer_parts` rebuilt: a lofted torso with shoulder caps, collar, placket and buttons; a pelvis with belt and buckle; thighs with knees and shins with cuffed trouser legs, shoes and soles; upper arms with elbows and forearms with cuffs, hands and thumbs; a skull with a jaw, neck, ears, eyes, brows, nose and mouth; hair (short / long), cap, beanie and coat variants. Slots: jacket / trousers / skin / hair-and-dark. Limb segments export flat with world pivots (FBX child transforms arrive in the root's frame) and `WorkshopMaterials.AssignCustomerMaterials` parents shins under thighs and forearms under upper arms, then assigns cloth (felt / canvas normals, archetype colour untinted), skin and hair materials by part name.
- `Customer.Animate`: knees bend as the trailing leg comes through (42 degrees at full gait), elbows carry a resting bend that opens with the swing and closes at the chin or when carrying; the hand point moved to the forearm.
- Two dead ends caught by the four-sided diagnostic capture: `hq.uv_sphere` sits on its centre point (every joint and the skull rode one radius high until a centred wrapper replaced it), and the figure's front really is -Z (a first reading of the diagnostic argued the opposite and was reverted). Captures: `Docs/V6/v65/`.

## V6.6 — the checkout, ported from Golf Simulator (2026-09-04)
- The whole checkout is now a port of the proven Golf Simulator checkout rather than a Geode-grown one. Its kit
  (counter, POS monitor, card terminal, cash drawer, payment card, shopping bag, customer display, bills and coins)
  is converted from the authored GLBs by `Tools/Blender/import_golf_checkout.py` and built into prefabs with
  serialized anchor/socket references by `CheckoutKitBuilder`. The domain — integer-cent money, the bounded
  drawer-change solve, the 30-state physical contract with its recovery rules, the transaction's card and cash
  sub-machines, the change window, the deterministic money placement and the bag fit — is transliterated into
  `Scripts/Runtime/Checkout`. Golf's economy, inventory lifecycle, tax, customer history and write-ahead settlement
  log were deliberately not ported: Geode's own career already banks a sale atomically and marks the specimen Sold
  by identity exactly once.
- Shelf prices now end on the .95. A whole-dollar till never sees a coin, and counting change out of the drawer is
  half of what a checkout is.
- The record, with Golf's own reference frames beside the Geode ones, is `Docs/V6/checkout-port/README.md`.
- Verified: cash and card sales across small, medium and large specimens (cash banked, the till moves by the same
  amount on cash and not at all on card, the record Sold, the customer and entity gone, the station idle, nothing
  left on the counter); three customers back to back; a whole sale worked with nothing but the interact button and
  the target cycle (the controller path); save integrity for stock, reserved and sold pieces; a close-up pass over
  every prop. 23 new EditMode tests, suite green at 68.

## V6.7 — tutorial and first-run onboarding (§56-59, 2026-09-05)
- The tutorial existed and taught nineteen steps, but two of §57's four requirements were declared and never
  implemented: `Tutorial.Step.Target` was a field nothing read ("so the beacon can point at it" — there was no
  beacon), and `Tutorial.Completed` was raised with nobody subscribed, so a finished step vanished without
  acknowledgement.
- **`TutorialBeacon`** points at the object the current step is about. A ring sits over it while it is on screen;
  off screen the ring becomes a chevron pinned to an inset ellipse, pointing the way to turn. Both carry the
  distance, because "the tablet" means nothing until you know it is four metres behind you. It is drawn on the HUD
  panel rather than in the world: no new shader, nothing to occlude, and it reads the same in a dark corner as
  under a lamp. `Resolve()` maps each step's target key to a live object and re-resolves a few times a second, so a
  crate that is delivered mid-step or a machine that is sited is picked up without a restart. The tablet key
  deliberately prefers the workshop's own tablet over the office laptop: they open the same screen, but sending a
  first-run player to the back of house on step two teaches the wrong room.
- **Completion acknowledgement**: the hint card turns green for two seconds with the step's own `Done` line and the
  next step underneath, then hands the card over.
- **Two new steps** for the systems the visual rebuild added: `build` (only offered once something bought is still
  crated — `PlaceableFixture.AnyCratedFor`) and `inventory`. `BuildMode.TryPlace` and `InventoryUI.Open` notify
  them. Both hint texts carry the live binding through `Tutorial.Format`, so a remapped key reads correctly (§58).
- **A real bug the first-run pass found (§59).** `Playtest.FetchRock` clamped the walker to `x >= -3.1, z <= 2.25`
  — the V5 garage. M1 had moved the west wall to -6.4 and put the receiving bay north of z 3.2, so every rock
  delivered to the bay left the harness standing three metres away aiming at nothing: `could not pick`,
  `crackall processed=0`, then a retail cycle with an empty shelf and four customers leaving empty-handed. Clamped
  to `ShopPlan` instead. The same fresh run now opens 9 rocks instead of 2, keeps 2, and makes a retail sale.
- Verified: fresh save -> tutorial from step one, beacon on and off screen, acknowledgement, `RunFreshPlayer`
  end to end (9 opened, 6 families, 1 retail sale, controller menu sweep green, 0 collision overlaps), 68/68
  EditMode. Captures: `Geode/Output/captures/rebuild/v67/`.

## V6.8 — settings audit and key rebinding (§61-62, 2026-09-05)
- §61's audit found the settings model already complete — graphics, resolution, window mode, FOV, both
  sensitivities, inversion, every volume, UI scale, vibration and the accessibility set are all bound to a control
  and applied live. What was missing was the last line of §61's own list: **controls/rebinding**.
- The Controls page's BINDINGS card was a hardcoded string table. It could not be edited, and it was already wrong
  — it never listed Build mode or the inventory. `GameInput.Glyph` was a hardcoded switch too, so §62's
  "never hardcode `Press E` if the player remapped interact" was violated by construction.
- **`InputBindings`**: the project asset is the source of truth; `Display()` asks it what an action is bound to on a
  given control scheme, overrides included, and shortens the Input System's correct-but-long names to something a
  key cap can hold ("Left Stick Press" -> "L3", and a composite's longest run of single-character parts collapses,
  so Move reads "WASD" rather than "W/A/S/D/Up/Left/Down/Right"). `Glyph` now calls it, so a remap moves every
  prompt, key rail, tutorial line and station hint with it; the old table survives only as the pre-load fallback.
- **Rebinding**: `PerformInteractiveRebinding` per action per scheme, with the gameplay map disabled while listening
  (or the very press being captured also fires the action it is replacing), mouse position and delta excluded, and
  Escape / Start to cancel. **Conflicts**: one control, one action — the action that had it gives it up and the page
  says which, because silently sharing a key is the worst of the three options. **Reset** per action and for all,
  and "Reset section" on the Controls page now means the whole page, bindings included.
- **Persistence**: the Input System's own override JSON rides in `settings.json` (`GameSettings.Bindings`) and is
  applied in `GameInput.Ensure` before anything asks for a glyph.
- Verified by extending `SettingsMatrix` with eight rows that drive the real path — start a rebind, queue a real
  device event, check the binding, the prompt and the tutorial text all moved, take the same control with another
  action and check the first is unbound and named, rebind on the pad and check the keyboard side is untouched, save,
  reload from disk, reset. **37/37 settings rows pass, 0 fail**, covering §61's "interaction -> runtime effect ->
  save -> reload" for every setting in its audit. 68/68 EditMode. Captures: `Geode/Output/captures/rebuild/v62/`.

## V6.9 — buying, sourcing and the objective card (§64-65, 2026-09-05)
- **§64 audit.** The suppliers screen already carried the dealer, the price, the rock count, the character chip, the
  lock state, the risk, the expected quality range and the minerals to look for. Three things were wrong or absent:
  - the header said crates arrive "to the pallet by the door", which has not been true since M6 built the receiving
    bay — an ordered crate lands in the back of house, and the game had been telling the player the wrong room;
  - **storage impact** was missing from §64's list entirely;
  - and the numbers that decide a purchase were **below the fold**. The detail card scrolls, and price, rock count
    and character sat under four paragraphs of flavour.
- The detail card now leads with the decision: price, what the till looks like afterwards, rocks, character, where
  it is delivered, how many crates are already waiting unopened, and how many display and sale slots are free —
  with a warning line when there are none. The prose follows.
- **§65.** The goals card said what to do (three standing goals and the level they add up to) but never what was on
  the other side of them. `Progression.NextUnlock` names the nearest thing the player can act on: the cheapest
  available upgrade if they can already afford it, otherwise the cheapest supplier still behind a condition with
  that condition spelled out, otherwise the cheapest upgrade and the shortfall. One clipped line under the header,
  because §65 also says not to let the card dominate the screen.
- Verified in Play Mode on a fresh save and on a progressed one; 68/68 EditMode. Captures:
  `Geode/Output/captures/rebuild/v64/` and `v65/`.

## V6.10 — UI render QA (§66, 2026-09-05)
- `UiRenderAudit` measures the interface instead of looking at it: every visible element on the game's own panel is
  walked and judged against the faults §66 names — off screen, clipped, truncated, unreadable, too small to hit,
  overlapping another card, focus lost with a menu open, notifications stacked. `Playtest.RunUiRenderQa` lays the
  panel out into a render texture at 1920, 2560 and 3840 and runs the audit over four screens at two interface
  scales, so each pass is a real layout rather than a proxy for one.
- §66 asks that the instrument be proved able to fail. `PlantNegatives` breaks four things on purpose — a 4 px
  label, an element hanging off the left edge, a long label in a 40 px box, an 8 px button — and the harness
  requires each to be caught by name before it trusts a single pass.
- **Three faults in the instrument, found by running it.** The checkout's POS monitor and customer display are
  world-space screens on their own panels: 20 cm of glass on a counter, where a screen-pixel font rule is
  meaningless — they are skipped now. The physical-pixel sum was inverted (it divided by the reference resolution
  instead of scaling layout units up to the real screen). And content scrolled out of a list was reported as
  clipped, which is what a scroll view is for: the walk now carries the nearest scrolling viewport as the rect an
  element is allowed to draw in, and 70 findings on one healthy page went to none.
- **Two real faults in the game.** The tablet panel was a fixed 1500x880, and at 1.4x interface scale the reference
  resolution is 1371x771 — it hung off both edges; it is clamped to the panel now. And at that scale the tablet's
  Close button landed squarely on the HUD's status card. The tablet carries its own cash readout in its header, so
  the status card stands down for it (a side panel like the inventory leaves it up, the way R03 shows it).
- Result: **28/28 pass, 0 findings** over three resolutions, two interface scales and four screens, with all four
  negative controls caught. 68/68 EditMode. Captures: `Geode/Output/captures/rebuild/v66/`.

## Defects discovered
- (V5 baseline, from the owner's screenshots and the captures above) boxy dark saw; dough-like rough geode; muddy, shallow, sparse opened geode; colour-only material differences; mannequin customers; abstract checkout.
- V6.1b root causes behind the "dough" and "fur":
  1. `M_GeodeShell` had no `_RindAlbedo` assigned (the tile's colour breakup never reached the exterior). Fixed by wiring in `AssetLibraryBuilder`.
  2. Tile normal strength was about 8x too weak (see the formula above).
  3. The hero bench opened rocks under the full dust film and staged them clay-caked, so every crystal capture was grey; the bench now captures the washed and rinsed states too.
  4. Mesh noise above ~2.5 cycles per unit aliased into diagonal ridges along the quad splits (the `|n|` billow creases and a finer knob octave made it worse). Band-limited.
  5. Triplanar x-plane UVs were transposed and back faces unmirrored, so knobs lit as dents on some faces.
  6. The rocks had no `DepthNormals` pass, so SSAO (source DepthNormals) never saw them.
  7. The main "fur": `pits`, cavity glitter and crystal sparkle thresholded the noise texture's per-texel channel, and with `anisotropicFiltering = ForceEnable` white noise filters into streaks along the foreshortened axis. Every surface feature now uses the tile or a smooth noise channel.

## Measurements
- Editor perf at a fixed 1080p game view during the retail stress: ~850 draw calls, 150-180 set-pass calls, 5.7 M triangles, 8.5 M vertices, 730 MB allocated / 1.77 GB reserved, 16-33 fps (M2, 8 GB).
- V6.1b: shell 13,632 triangles per half; crystals per hero rock 84 (ACC) / 172 (E53) / 384 (7D1) / 578 (8BF, druzy); tile generation 7 s for four 1024 sets; EditMode 35/35 (twice, before and after the mesh change).

## Tests added
- V6.6 added 23 EditMode tests (checkout). Suite is 68 and green through V6.10. In-game harnesses: 37/37 settings rows, 28/28 UI render QA passes with 4 proven negative controls, 12/12 placement, 5 customer stress runs.

## Experiments / failed hypotheses / reverts
- A finer knob octave on the mesh (`b3` at 4.6x the billow frequency) was added and removed the same session: it aliased on the 96-ring grid.
- Softening `T_Rock`'s crack lines and moving SSAO off the rocks were both tried as streak fixes before the anisotropic-noise cause was found; the first is kept (harmless, slightly cleaner coarse rinds), the second was never needed (source was already DepthNormals; the missing pass was the defect).

## Known-good milestone commits
- V6.0 `1e923ff`, V6.1a `07186f9`, V6.1b `b720789` (material pipeline + geode hero pass), V6.1c `927efe1` (crystal habits and carpets), V6.1d `47b0053` (family-wide review fixes), V6.2a `9b37bec` (worn machines, saw remodel), V6.3 `94f12d7` (reveal motion), V6.4 `27f0db9` (lighting and the saw cover), V6.5 (this commit): customers.

## Remaining work
- V6.6 checkout and V6.7 tutorial are complete (see above). The inventory UI (§63) was built during the visual
  rebuild phase (`InventoryUI`, R03), §61/§62 (V6.8), §64/§65 (V6.9) and §66 (V6.10) are done. Next: §67
  specimen diversity and §68 specimen-specific gameplay, then §69 onward.
- V6.1 remainder: S8 tilt toward clusters, S6 per-direction wall thinning, S7 terraces, S10 luster classes for the non-quartz habits, S11 SpecimenVisual hygiene + perf gate (**partly done**: `SpecimenVisual.CrystalBudget` gives scenery a crystal budget, which took the stocked showroom from 5.19 M to 2.80 M triangles — see `Docs/VisualRebuild/PLAN.md` E), S12 machine scaffolding, S13 acceptance gate (RunGeodeGate matrix over all 24 families, standalone, report). Known visual nits: the agate face's fracture relief is strong, the staged seam frost is still chalky at full stress, non-quartz carpets (calcite, fluorite) still use the V5 habits at V5 sizes.
- Then V6.2 machines .. V6.9 and the FINAL acceptance per the brief.

## Final acceptance status
- Not started.
