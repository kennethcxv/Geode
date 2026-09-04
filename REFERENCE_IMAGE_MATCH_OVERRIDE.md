# REFERENCE IMAGE MATCH OVERRIDE
## Authoritative visual fidelity pass for Geode before resuming V6

This file defines a focused execution phase that must be completed **before** continuing general V6 work.

---

# 0. MISSION

Pause broader V6 feature work and perform a dedicated **reference-image visual fidelity pass**.

The game already has a set of target reference images provided by the user. These images represent the intended look, feel, framing, UI density, environment quality, material quality, and overall presentation standard for important parts of the game.

Your job is to make the **actual game** match those reference images as closely as practical.

The end result should be that when the user opens the real game and visits the corresponding screen or area, it feels like the same game shown in the reference image.

This is **not** a moodboard exercise.
This is **not** a loose inspiration pass.
This is **not** permission to create fake screenshots that the game cannot actually reproduce.

This is a **real implementation pass** to align the actual game with the visual targets.

---

# 1. REFERENCE LOCATIONS

Inspect all provided visual targets in these folders:

- `Geode/references`
- `Geode/refrences` (if that misspelled path exists)

Treat the full set as the visual target pack for this pass.

If both folders exist, inspect both and deduplicate by actual content rather than filename alone.

---

# 2. PRIMARY OBJECTIVE

Make the real Geode game visually, physically, spatially, and ergonomically match or exceed the reference images.

This includes, where relevant:

- overall art direction
- environment composition
- visual appeal of geodes/specimens
- object/material quality
- scene cleanliness and coherence
- camera angle / FOV / framing
- lighting / mood / contrast
- UI hierarchy
- UI density
- layout spacing
- icon readability
- panel styling
- interaction presentation
- scene polish
- commercial-quality simulator feel

The game should feel like a cohesive product, not a prototype, not a placeholder build, and not an AI-generated approximation.

---

# 3. HARD RULES

## 3.1 Do not resume V6 until this pass is actually complete
No new feature wandering.
No unrelated polish.
No general repo audit.
No “good enough” early exit.

Finish the reference-match pass first.

## 3.2 Improve the real game, not just documentation
The goal is the actual game implementation.

Do not satisfy this task by:
- writing plans only
- generating replacement concept art only
- summarizing differences only
- producing one-off mockups that the game cannot reproduce

## 3.3 Do not blindly rebuild everything
Preserve working systems where possible.
Use the smallest clean structural change needed to achieve the visual target.

However:

## 3.4 Restructure when necessary
If a space, camera setup, room layout, workstation composition, asset arrangement, or prop pipeline is fundamentally preventing fidelity to the reference images, then restructure it.

That includes:
- moving walls
- re-spacing rooms
- rebuilding workstation layouts
- refactoring scene hierarchy
- replacing placeholder props
- remeshing / remodeling specific assets
- rebuilding lighting rigs
- redoing UI composition
- fixing material workflows
- reauthoring environment composition

If Blender is needed, use Blender seriously.

Do not protect a weak current implementation if it blocks the target quality.

## 3.5 Use Blender when needed
If accuracy to the reference images requires asset changes, use Blender deliberately for:
- mesh cleanup
- proportion fixes
- silhouette correction
- workstation redesign
- prop remodeling
- support meshes
- improved composition assets
- laptop or fixture recreation
- UV or material support if needed
- anchor/socket cleanup if applicable

Do not avoid Blender just because it is more work.

## 3.6 No fake “visual parity”
A screen only counts if the playable/buildable/viewable game can actually produce it.

---

# 4. WHAT SUCCESS LOOKS LIKE

This pass is complete only when:

1. Every important reference image has a real in-game equivalent.
2. The in-game equivalent looks convincingly close to the reference.
3. The overall game looks cohesive across all these screens/areas.
4. The geodes/specimens look materially appealing and intentional.
5. The UI looks like a polished simulator UI, not generic placeholder UI.
6. The environments and workstations feel hand-authored, readable, and believable.
7. Side-by-side comparison frames show that the game version now reflects the reference image closely.
8. You have verified the results yourself, not assumed them.

If any major screen still feels obviously worse, flatter, more placeholder-like, or less appealing than its reference, the pass is not done.

---

# 5. EXPECTED REFERENCE CATEGORIES

The reference set may correspond to screens/areas such as:

- main menu
- storefront / exterior
- shop floor
- checkout
- dealer interaction
- collection screen
- collection showroom / gallery
- workshop
- geode cracking station
- cutting station
- cleaning / washing station
- polishing station
- appraisal station
- storage / inventory room
- upgrades screen
- incoming shipment / receiving
- packing / fulfillment
- end-of-day summary
- special orders
- notifications / discoveries / unlock moments
- management desk / office
- laptop or management interface
- museum / showcase spaces

Do not assume this list is exhaustive.
Map the real provided images.

---

# 6. EXECUTION PROTOCOL

Follow this sequence.

## Phase A — Safety checkpoint
Before making substantial changes:

1. verify the project opens and compiles
2. create a clean checkpoint commit
3. push the checkpoint if appropriate
4. note the checkpoint hash in your working log

Do not skip the checkpoint.

---

## Phase B — Build the reference manifest
Create a working manifest for the reference pass.

For every reference image:

- assign it a stable ID
- record its file path
- identify the likely target screen/scene/system
- identify whether it is:
  - environment-first
  - UI-first
  - hybrid environment + UI
  - workstation / interaction
  - reward / notification
- identify the likely in-game counterpart
- note the most important qualities to match

Create a simple table or markdown note for yourself so you can track progress precisely.

Do not operate vaguely from memory.

---

## Phase C — Capture the current state
For every mapped in-game counterpart:

- open the real game
- go to the relevant screen / scene / station
- capture the current real result
- save comparison captures

You need the “before” state.

Do not skip direct visual comparison.

---

## Phase D — Gap analysis
For each reference pair, compare the reference image to the real game on these axes:

### Environment / scene axes
- room shape
- spatial composition
- focal point
- prop selection
- prop scale
- prop placement
- clutter level
- cleanliness
- silhouette quality
- surface breakup
- readability
- depth layering
- background treatment
- lighting direction
- lighting warmth/coolness
- shadow softness
- contrast
- mood
- camera height
- FOV / lens feel
- framing

### Asset / specimen axes
- geode silhouette quality
- cut-face attractiveness
- crystal readability
- material richness
- edge readability
- color variation
- polish level
- believable size variety
- display presentation
- support props
- stand / tray / pedestal quality

### UI axes
- hierarchy
- spacing
- alignment
- panel weight
- panel proportions
- typography feel
- icon clarity
- density
- color system
- contrast
- information grouping
- simulator-game readability
- whether the UI feels too bland or too busy
- whether the UI feels integrated with the scene

### Interaction presentation axes
- how the workstation is framed
- whether the action is immediately readable
- whether the station looks satisfying to use
- whether the important interactables are visually obvious
- whether the sequence feels tactile and polished

Record what is wrong.
Then fix it.

---

# 7. QUALITY BAR — WHAT NOT TO SHIP

Do not stop with any of the following:

- generic Unity-looking UI
- flat panels with weak hierarchy
- random iconography
- geodes that read as dull rocks
- geodes with sloppy crystal faces
- over-detailed noisy clutter
- under-detailed bland rooms
- inconsistent styles from one screen to another
- poor lighting
- placeholder props
- obvious asset mismatch
- weak camera framing
- screens that look “functionally okay” but not attractive
- “AI slop” hyper-detail with no intentional design
- lifeless scenes that feel sterile
- overly plain references copied too literally if they reduce overall appeal
- overly cinematic exaggeration that stops feeling like a simulator

Target:
**more impressive than plain mockups, less fake than glossy AI over-rendering, and clearly like a polished simulator game.**

---

# 8. AUTHORITY ORDER

When deciding what to preserve vs. change, follow this order:

1. **The real reference images**
2. **User-specified quality direction**
3. **Existing strong Geode systems and architecture**
4. **Existing scene layout or asset setup**
5. **Personal convenience**

If the current game conflicts with the reference target, the game should change.

---

# 9. USE OF BLENDER

Use Blender where it materially improves fidelity.

Typical reasons to use Blender include:

- the room layout is fundamentally wrong
- workstation proportions are off
- placeholder assets break the look
- the laptop or workbench needs correct proportions
- existing shop furniture looks cheap
- prop silhouettes are weak
- reference images imply a cleaner or stronger composition
- the current station cannot be made correct through transform tweaks alone

When using Blender:
- keep edits structured and reversible
- export cleanly for Unity
- preserve pivots and scale sanity
- preserve or improve material assignment discipline
- do not leave broken imports
- verify the result in Unity, not just Blender

Do not stop at “looks good in Blender.”
It must work in the game.

---

# 10. UI-SPECIFIC EXPECTATIONS

For screens like collection, dealer, upgrades, inventory, end-of-day, special orders, notifications, and other management UIs:

## 10.1 Match the intended style
The UI should feel:
- clean
- slightly premium
- readable
- tactile
- simulator-appropriate
- visually integrated with the game

## 10.2 Improve beyond blandness
If the current UI is too plain:
- improve panel shapes
- improve spacing
- improve grouping
- improve typographic hierarchy
- improve color contrast
- improve icon consistency
- improve section emphasis
- improve specimen presentation
- improve reward moments
- improve calls to action

## 10.3 Avoid over-design
Do not create:
- unreadable glassmorphism nonsense
- overanimated clutter
- gratuitous ornament
- mobile-game spam feel
- AAA HUD excess that does not fit the simulator

The result should feel polished and intentional.

---

# 11. SPECIMEN / GEODE QUALITY EXPECTATIONS

The user explicitly wants the rocks/geodes/specimens to look more impressive than the bland versions while still looking like part of a game.

That means:

- stronger silhouettes
- clearer crystal interiors
- better edge highlights
- better shape variation
- convincing material transitions
- improved color richness
- nicer presentation on shelves/tables/pedestals
- better lighting on hero pieces
- good readability at gameplay camera distances
- believable variety across small/medium/large examples

Do not let the game’s central objects look cheap.

The geodes are part of the core fantasy.
Treat them like hero content.

---

# 12. IMPORTANT IMPLEMENTATION BEHAVIOR

For each reference image, do the following loop until it is truly close:

1. inspect reference image carefully
2. inspect the current in-game equivalent carefully
3. identify the top 3–10 gaps
4. fix the highest-impact gaps first
5. reopen the game
6. capture again
7. compare side by side
8. continue iterating

Do not do just one pass and move on.
Do not assume a fix worked without checking.

---

# 13. TESTING / VERIFICATION REQUIREMENTS

You must verify with real evidence.

## 13.1 For every important screen/scene
Capture:
- reference image
- current in-game equivalent
- improved in-game equivalent

## 13.2 For every environment/workstation
Verify:
- collisions
- clipping
- camera framing
- close-up appearance
- interaction readability

## 13.3 For every UI-heavy screen
Verify:
- legibility
- spacing
- resolution/scaling
- state consistency
- no overlapping or awkward truncation
- controller + KBM navigation if applicable

## 13.4 For every specimen-heavy screen
Verify:
- shape readability
- material appeal
- lighting quality
- no obvious ugly clipping or broken pivot presentation

## 13.5 For every upgraded area
Verify in the actual game, not just in editor scene view.

---

# 14. DO NOT FALL INTO THESE FAILURE MODES

Do not:

- confuse “more detail” with “better”
- leave the game inconsistent from screen to screen
- only improve one hero shot while the actual normal view still looks weak
- keep outdated placeholder assets because replacing them is inconvenient
- rely on memory of the reference image instead of reopening it
- produce a side-by-side once and stop
- create static beauty shots that hide the actual gameplay view
- leave work half-done and call it a later polish pass
- let one good area justify multiple mediocre ones
- spend time documenting the whole repo

Stay focused on the actual target screens.

---

# 15. FALLBACK / ROLLBACK RULE

Do not remove useful current implementations until the improved replacement is actually better and verified.

If a change makes the screen worse:
- revert or rework it
- do not force the worse version through

The standard is:
**the replacement must be at least as good as the reference target and better than the old version.**

---

# 16. DELIVERABLES FOR THIS PASS

Before declaring this pass complete, produce:

1. a list of all reference images mapped to in-game screens
2. side-by-side comparisons for the important ones
3. the actual implemented game improvements
4. a short completion summary of what changed per screen/area
5. confirmation that V6 was paused during this pass
6. confirmation that V6 may now resume

Keep the summary concise, but the implementation itself must be complete.

---

# 17. DEFINITION OF DONE

This pass is done only if all of the following are true:

- every major reference image has been analyzed
- every major reference image has a real in-game counterpart
- each counterpart has been improved to closely reflect the reference
- environments feel intentionally composed
- UI feels polished and simulator-quality
- geodes/specimens look attractive and central
- collisions/clipping/camera issues have been checked
- the game feels cohesive across all improved screens
- the result does not feel obviously worse than the reference pack
- you have personally verified the result with captures and direct inspection

If any major reference area still feels:
- too bland
- too placeholder-like
- too generic
- too noisy
- too inconsistent
- too different from the reference

then the pass is not done.

---

# 18. FINAL INSTRUCTION

Do this yourself.

Do not spawn broad repo-audit subagents.
Do not waste time documenting the entire project.
Do not resume general V6 work until this reference-image match pass is genuinely complete.

Use Unity seriously.
Use Blender seriously.
Inspect the actual images carefully.
Iterate until the real game matches them closely.

The goal is not to approximate the references.
The goal is to make the actual game feel like those references came from the actual shipped product.