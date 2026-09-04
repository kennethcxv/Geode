# GEODE EMPIRE V6 — PRODUCTION ALPHA

## AAA-QUALITY ASSET & MATERIAL OVERHAUL + GEODE HERO REBUILD + PHYSICAL RETAIL + NPCs + TUTORIAL + UI/UX + DEPTH + COHESION + FEEL

Execute **GEODE EMPIRE V6 — PRODUCTION ALPHA** as the authoritative next milestone after the completed V5 checkpoint.

This is a **major autonomous production pass**.

There is **NO TIME LIMIT**.

Do not reduce scope because the work is taking a long time.

Do not lower the quality bar to finish sooner.

Do not declare success because many files changed, tests pass, or systems technically function.

**The actual in-game result is the acceptance criterion.**

Work autonomously until the complete V6 definition of done is genuinely satisfied.

---

# 1. ESTABLISH V6 AS THE AUTHORITATIVE SOURCE OF TRUTH

Before making changes:

1. Read `CLAUDE.md`.
2. Read every authoritative Geode Empire V3/V4/V5 design and implementation document.
3. Read the V5 final report and QA evidence.
4. Inspect the current clean `main` checkpoint.
5. Inspect existing Blender generators/tooling.
6. Inspect Unity scene-generation tooling.
7. Inspect specimen generation.
8. Inspect placement/collision systems.
9. Inspect save/persistence systems.
10. Inspect controller and keyboard/mouse support.
11. Inspect current career progression and Stage 3/endgame.
12. Inspect the actual V5 game in Unity yourself.
13. Perform a first-person visual walkthrough.
14. Capture baseline screenshots of every major area.
15. Read this complete V6 specification into a durable repository document.

Do not rely only on conversation context.

Save the complete V6 brief into the repository so that context compaction, summarization, session continuation or model context loss cannot weaken the requirements.

Maintain a living V6 report containing:

* current phase
* defects discovered
* screenshots
* measurements
* tests added
* experiments
* failed hypotheses
* reverted approaches
* remaining work
* known-good milestone commits
* final acceptance status

---

# 2. V5 IS AN UNTOUCHABLE FUNCTIONAL BASELINE

Preserve all working V5 guarantees.

Do not regress:

* save/load
* persistence
* controller support
* keyboard/mouse support
* specimen identity
* specimen state
* placement validation
* full-footprint validation
* collision
* world-integrity fixes
* cracking
* washing
* sawing
* polishing
* appraisal
* sourcing
* storage
* retail
* career progression
* Stage 2
* Stage 3
* endgame
* existing tests
* deterministic behavior where guaranteed

Improving visuals does not justify breaking mechanics.

Improving mechanics does not justify breaking persistence.

Improving performance does not justify visibly degrading hero assets.

---

# 3. CRITICAL REALITY CHECK — V5 VISUAL QUALITY DID NOT REACH THE REQUIRED BAR

The existing V5 visual pass is **not sufficient**.

Do not treat it as successful simply because Blender files were regenerated or polygon counts increased.

The owner's actual in-engine screenshots show that the game still reads as:

* prototype-quality
* mobile-game-like
* procedurally generated
* visually flat
* overly primitive
* lacking realistic material response
* lacking satisfying geode reveals
* insufficient for close first-person inspection

The screenshots particularly show:

### SAW / WORKSHOP EQUIPMENT

The saw still reads as:

* boxy
* primitive
* dark and visually muddy
* weakly differentiated by material
* mechanically simplified
* lacking believable manufacturing detail

### CLOSED GEODE

The rough geode still reads as:

* soft
* dough-like
* low-detail
* weakly geological
* insufficiently dense/heavy
* too smooth
* weak rind definition

### OPEN GEODE

The opened result still reads as:

* muddy
* shallow
* sparse
* low-detail
* repetitive
* insufficiently crystalline
* unsatisfying

This visual result is **explicitly unacceptable for V6**.

---

# 4. V6 PRIORITY OVERRIDE

Before large content expansion:

## MAKE THE GAME LOOK SIGNIFICANTLY BETTER.

V6 priority order begins with:

1. asset geometry
2. materials
3. geodes
4. fracture/reveal
5. machines
6. lighting/presentation
7. NPC appearance and animation
8. physical interactions

Do not race into later progression features while the game still resembles the current screenshots.

---

# 5. V6 MATERIAL QUALITY OVERHAUL — P0 REQUIREMENT

The current materials are one of the major reasons improved geometry still looks cheap.

Perform a **complete material-quality audit** of everything frequently visible.

Materials should no longer look like simple:

* base-color + roughness values
* flat Unity prototype materials
* overly clean procedural surfaces
* uniformly rough plastic
* painted geometry without physical surface behavior

Every major material needs an intentional physically believable response.

---

# 6. MATERIALS MUST BE PHYSICALLY READABLE

At a glance the player should distinguish:

* rough stone
* fresh fracture
* weathered rind
* polished mineral
* crystal
* stainless steel
* painted steel
* cast iron
* aluminum
* rubber
* plastic
* hardwood
* plywood
* cardboard
* leather
* cloth
* glass
* water
* wet stone
* slurry

Do not make these differences mostly color differences.

They must differ through:

* roughness
* reflectivity
* micro-normal behavior
* surface breakup
* edge response
* wear
* color variation
* specular response
* transparency/transmission where appropriate

---

# 7. PBR MATERIAL REQUIREMENTS

Use physically sensible PBR principles.

Audit:

* albedo values
* roughness
* metallic values
* normal intensity
* micro-normal frequency
* ambient occlusion
* surface masks
* opacity
* transmission
* emissive use
* texture scale

Avoid:

* pure black
* pure white
* extreme metallic values on nonmetals
* mirror-like rough stone
* plastic-looking rock
* glowing crystals without justification
* excessive bloom
* arbitrary gradients used to fake geometry

---

# 8. UV / TEXEL QUALITY

Fix all:

* stretched textures
* visible repetition
* inconsistent texture scale
* giant wood grain
* tiny wood grain
* warped grain
* obvious box projection
* inconsistent texel density
* seams visible from gameplay distance

Hero assets should have intentional UV treatment where needed.

Procedural projection may be used where it produces superior results, but it must not visibly reveal itself.

---

# 9. MATERIAL MICRODETAIL

Hero assets need believable fine surface breakup.

Examples:

### ROCK

* micro-pitting
* rough weathered areas
* mineral staining
* small color irregularities
* fracture grain
* rind variation

### METAL

* subtle machining direction
* brushed metal where appropriate
* cast texture where appropriate
* edge variation
* realistic roughness
* restrained scratches

### WOOD

* believable grain direction
* end grain
* different face grain
* subtle wear
* joint readability

### RUBBER

* soft rough response
* restrained microtexture

Do not exaggerate everything into noisy procedural grunge.

---

# 10. WEAR MUST FOLLOW REAL CONTACT

Do not add random wear uniformly.

Wear should occur where plausible:

* handles
* machine controls
* tray edges
* work surfaces
* tool grips
* corners
* saw table
* vise jaws
* frequently touched areas

Machines should not look abandoned unless the game intends them to.

The goal is **used professional equipment**, not post-apocalyptic scrap.

---

# 11. GEODES ARE STILL THE HERO ASSET

Geodes must receive a far more aggressive rebuild.

They are the reason the player is playing Geode Empire.

The player's emotional payoff depends heavily on:

> What is inside this rock?

The resulting reveal must be beautiful enough to justify the loop.

---

# 12. CLOSED GEODE EXTERIOR REBUILD

Closed geodes should no longer resemble rounded noise spheres.

Improve:

* macro silhouette
* asymmetric profile
* geological mass
* natural flat spots
* weathered ridges
* rind structure
* rock density
* irregular growth
* erosion
* cavities/depressions where appropriate
* fracture history
* mineral staining
* color breakup
* regional texture variation

No obvious repeating ridges.

No pinecone noise.

No smooth dough-ball look.

No uniform procedural displacement.

---

# 13. GEODE MESH RESOLUTION

Do not treat increasing:

`40x14 → 64x20`

as sufficient proof of visual quality.

Use enough geometry to maintain clean silhouettes at close inspection.

Use adaptive/detail-conscious topology where useful.

The standard is visual:

> Can I walk within roughly 30–60 cm of the geode and still believe it is a rock?

If not, keep improving it.

---

# 14. GEODE SHELL / RIND

Make rind thickness physically readable.

Improve:

* shell thickness variation
* weathered exterior
* intermediate matrix
* mineralized rim
* transition to cavity
* fracture-edge layering

The player should be able to visually read:

**outer rock → rind → mineral transition → cavity → crystals**

not:

**brown ball → colored hole**

---

# 15. DECREASE THE BORING ROCK-TO-GEODE RATIO

The owner's feedback is explicit:

> there is too much ordinary rock and not enough geode payoff.

Correct this intelligently.

Do not make every shell unrealistically thin.

Instead tune specimens so that opened geodes more frequently provide visually compelling:

* cavity area
* banding
* crystals
* mineral surfaces

A dramatic specimen should feel dramatic.

---

# 16. OPEN GEODE CAVITY REBUILD

Open cavities need much greater depth.

Build:

* layered cavity walls
* recesses
* shadow pockets
* cavities within cavities where plausible
* mineral transition bands
* complex wall contour
* clustered crystal regions
* partial growth areas
* empty wall variation

Avoid a shallow bowl with crystals stuck onto it.

---

# 17. FRACTURE SURFACE QUALITY

Fresh fracture surfaces should look distinctly different from:

* exterior weathered rind
* polished surfaces
* sawn surfaces

Improve:

* conchoidal character where appropriate
* irregular fracture ridges
* chipped areas
* tiny edge breaks
* fresh color
* roughness response
* mineral exposure

Fracture faces must not look like a painted white disc.

---

# 18. CRYSTAL GEOMETRY QUALITY

Crystals need a major upgrade.

Requirements:

* sharper readable forms
* better bases
* believable termination
* sufficient polygon density
* mineral-specific habits
* scale variation
* orientation variation
* irregular clustering
* occlusion
* secondary growth
* intersection with matrix
* occasional broken crystals
* natural crowding

No floating crystals.

No radial grids.

No uniform rings.

No obvious copy-paste placement.

No sparse random spikes.

---

# 19. CRYSTAL BASE INTEGRATION

A crystal should look like it **grew from the matrix**.

The base should be:

* partially buried
* naturally occluded
* clustered with nearby growth
* integrated into mineral substrate

Do not allow:

> crystal mesh + visible gap + cavity wall

or:

> crystal stabbed through unrelated geometry

---

# 20. MINERAL-SPECIFIC VISUAL IDENTITY

Quartz, amethyst, agate, calcite, garnet, druzy and other supported minerals should have materially distinct visual identities.

Differences should include:

* crystal habit
* transparency
* roughness
* luster
* color zoning
* clustering
* banding
* inclusion patterns
* matrix relationships

Do not create mineral variety simply by changing RGB values.

---

# 21. RARE SPECIMENS MUST LOOK RARE WITHOUT UI

A legendary specimen should visibly outperform an ordinary specimen.

Rare specimens may differ in:

* cavity scale
* crystal density
* crystal clarity
* unusual growth
* color zoning
* multi-mineral structures
* exceptional symmetry
* huge crystals
* unusual banding
* rare formations

If rarity must be read from a label, visual design has failed.

---

# 22. GEODE STATE QUALITY

Every major specimen state must have real visual differences:

* rough
* cleaned
* opened
* damaged
* sawn
* slabbed
* partially polished
* fully polished
* displayed

Do not fake a state entirely with color changes.

---

# 23. POLISHED MATERIAL QUALITY

Polishing should create a convincing transformation.

Polished areas need:

* smoother micro-normal
* lowered roughness
* richer mineral color
* clearer banding
* controlled reflections
* stronger depth

Polish should reveal stone quality rather than simply make the whole object shiny.

---

# 24. WET STONE QUALITY

Washing/sawing should temporarily change surface response.

Wet stone should show:

* darker albedo
* lower roughness
* stronger specular response
* richer color

As appropriate, it should transition back while drying.

---

# 25. CRACKING ANIMATION MUST BE PHYSICALLY CORRECT

The current cracking/opening sequence can phase through the support because the movement direction is wrong.

Completely fix this.

Determine:

* support geometry
* fracture plane
* stationary half
* moving half
* clearance direction
* rotation axis
* resting pose

Use specimen geometry to avoid collisions.

---

# 26. CRACKING SHOULD FEEL SATISFYING

The opening sequence should have beats:

1. tool setup
2. force buildup
3. impact
4. initial fracture
5. small separation
6. rock response
7. cavity reveal
8. piece settling
9. visual/audio payoff

Use appropriate:

* sound
* chips
* dust
* camera feedback
* controller vibration
* lighting/readability

Keep it believable rather than arcade-like.

---

# 27. BLENDER MUST BE USED SERIOUSLY

Do not remain inside procedural Unity geometry simply because modifying it is easier.

Use:

`./Tools/blender.sh`

heavily.

Where procedural generation remains useful, combine it with superior authored/geometric building blocks.

If an asset is fundamentally weak:

**REMODEL IT.**

Do not hide it with material changes.

---

# 28. COMPLETE WORKSHOP HERO-ASSET REBUILD

Audit and improve:

* saw
* saw cabinet
* blade
* arbor
* coolant pan
* splash guards
* rails
* vise
* carriage
* clamps
* motor
* switches
* hoses
* polishing lap
* polish disc
* wash sink
* tap
* scale
* appraisal equipment
* cracking bench
* cradle
* heavy cradle
* hammer
* chisel
* wedge
* loupe
* storage
* displays
* checkout counter
* register

The existing saw screenshot is explicitly below the required standard.

---

# 29. MACHINE INDUSTRIAL DESIGN QUALITY

Machines must look manufactured.

Use:

* realistic dimensions
* proper sheet-metal thickness
* panel gaps
* fasteners
* hinges
* handles
* seams
* supports
* mounting points
* motor housings
* guards
* cables
* hoses
* bearings
* rail interfaces
* hardware

Do not add random detail for noise.

Every visible piece should look like it has a reason to exist.

---

# 30. CURVES MUST ACTUALLY LOOK CURVED

Audit every visible:

* cylinder
* wheel
* handle
* pipe
* hose
* knob
* blade
* pulley
* bucket
* lamp
* crystal
* rounded furniture edge

No obvious radial faceting at normal viewing distance.

Use suitable segment density.

---

# 31. BEVELS / EDGE QUALITY

Perfect 90-degree computer-generated edges often look cheap.

Use physically plausible bevels on:

* metal housings
* wood
* furniture
* counters
* machine panels
* plastic
* tools

But do not make everything pill-shaped.

Preserve intentionally sharp edges.

---

# 32. NORMALS / SMOOTHING

Audit:

* hard edges
* smooth edges
* weighted normals
* Smooth by Angle behavior
* imported split normals

No:

* black shading streaks
* warped panels
* melted edges
* unexpected gradients
* polygon faceting

---

# 33. COLLISION PROXIES MUST FOLLOW NEW GEOMETRY

Every remodeled asset requires collision review.

Use efficient compound colliders where appropriate.

No:

* invisible blocking boxes
* walking through visible solids
* mesh collider artifacts
* collision extending far beyond geometry

---

# 34. WHOLE-WORKSHOP VISUAL AUDIT

After hero assets improve, inspect everything else regularly visible:

* floors
* walls
* trims
* ceilings
* beams
* windows
* doors
* chairs
* shelves
* cabinets
* counters
* lamps
* posters
* switches
* boxes
* pallets
* signs
* packaging
* storage bins

A high-quality saw beside a primitive stool still makes the room look unfinished.

---

# 35. LIGHTING REBUILD

The current scene is often too dark or visually muddy.

Improve:

* key lighting
* fill
* practical lights
* window contribution
* display lighting
* machine readability
* specimen readability
* shadow softness
* exposure
* contrast
* contact shadows
* reflection probes
* ambient occlusion where appropriate

Do not flatten everything with ambient brightness.

Do not bury geometry in darkness.

---

# 36. SHOWROOM / DISPLAY LIGHTING

Display cases should make valuable specimens look desirable.

Use:

* controlled directional light
* accent lights
* reflections
* strong shape readability

Rare specimens should visually pop without looking like glowing loot.

---

# 37. NPC MODEL REBUILD

Current customers remain prototype mannequins.

This is unacceptable for V6.

Improve:

* body proportions
* torso
* shoulders
* arms
* elbows
* wrists
* hands
* fingers
* neck
* head
* face
* hair
* clothing
* shoes

They do not need photoreal MetaHuman quality.

They must look like intentionally designed game characters.

---

# 38. NPC MATERIAL QUALITY

NPCs also need appropriate:

* skin
* hair
* fabric
* shoe
* accessory

materials.

Avoid:

* wax skin
* plastic hair
* painted clothing
* identical material response across everything

---

# 39. NPC LOCOMOTION

Improve:

* walk cycles
* acceleration
* stopping
* turning
* foot planting
* direction changes
* queue movement
* browsing locomotion

No sliding feet.

No mannequin rotation.

---

# 40. NPC IDLE / BODY LANGUAGE

Add restrained natural behavior:

* weight shifting
* looking around
* breathing
* head movement
* inspecting objects
* waiting
* subtle hand/arm movement

Do not make customers hyperactive.

---

# 41. NPC ARM / HAND SYSTEM

Arms and hands need independent control.

Do not mirror both arms automatically.

Support:

* shoulder targeting
* elbow solving
* wrist orientation
* finger closure
* object-specific hand poses
* one-hand carrying
* two-hand carrying

Avoid surrender poses and broken wrists.

---

# 42. NPC PICKUP SYSTEM

When an NPC picks up a specimen:

1. identify a valid grip
2. move within range
3. reach
4. contact
5. close hand
6. transfer ownership
7. attach to grip
8. enter carry/inspection pose

Do not teleport objects from shelves into hands.

---

# 43. OBJECT SCALE MUST REMAIN CONSTANT THROUGH PICKUP

The exact specimen must keep its world size while transitioning:

shelf → hand → counter → packaging → customer.

Do not accidentally change scale through parenting.

---

# 44. NPC LARGE-OBJECT HANDLING

Object weight/size should change the pose.

Tiny specimen:

* one hand

Medium specimen:

* one or two hands

Large geode:

* two hands / tabletop assistance / box

Do not hold a 10 kg geode like a credit card.

---

# 45. CUSTOMER SHOP BEHAVIOR

Customers should naturally:

* enter
* browse
* inspect
* compare
* select
* possibly return items
* queue
* purchase
* receive purchase
* leave

The showroom should feel alive.

---

# 46. CHECKOUT MUST MATCH THE QUALITY OF THE GOLF SIMULATOR SYSTEM

Inspect the prior Golf Simulator checkout history and implementation patterns.

Reproduce the **principles and quality**, not blindly copy engine-specific code.

The Golf system eventually solved:

* stable station framing
* physical customer/payment placement
* card reader positioning
* cash drawer
* change
* payment gestures
* bagging
* customer handoff
* ownership transfer
* carry-grip attachment
* next-customer reset

Achieve the same completeness here.

---

# 47. FULL PHYSICAL CHECKOUT SEQUENCE

A transaction should involve:

1. customer queues
2. customer reaches counter
3. merchandise is staged
4. player rings purchase
5. total displays
6. customer chooses payment
7. customer physically presents payment
8. player accepts payment
9. transaction processes
10. change if required
11. item is packaged
12. package is handed across
13. customer takes ownership
14. customer walks away
15. station resets

No single `[E] Complete Sale` abstraction.

---

# 48. CASH PAYMENT

Cash payment must include:

* visible tender
* natural NPC reach
* physical placement
* cash drawer
* labeled denomination wells
* tender deposit
* change calculation
* physical change handoff

Cash must behave differently from card.

---

# 49. CASH DRAWER

Build a convincing drawer with:

* smooth opening
* dividers
* note wells
* coin wells
* labels
* believable money
* interaction clarity

No flat fake drawer UI if the physical version is practical.

---

# 50. CARD PAYMENT

Build a proper card reader.

Requirements:

* realistic scale
* physical mount
* readable display
* visible total
* status
* coherent buttons
* insert/tap interaction
* correct card dimensions
* card actually entering the slot if inserted

The customer should retain the card until the physical interaction requires release.

---

# 51. CHECKOUT CAMERA

Use a stable authored transaction camera pose.

It should:

* preserve spatial continuity
* show customer
* show counter
* show relevant hardware
* show purchase
* avoid bird's-eye angles
* avoid recomposing unpredictably

The camera must remain stable through transaction completion.

---

# 52. PACKAGING

Use packaging appropriate to specimen size.

### SMALL

bag / small box

### MEDIUM

protective box/bag

### LARGE

strong box/crate or two-handed direct transfer

Never shrink a geode to fit packaging.

---

# 53. NPC PACKAGE HANDOFF

Use the Golf Simulator lesson:

**across → outward/clear counter → downward into receiving hand**

not straight-line interpolation through geometry.

Use full bounds.

The NPC receiving arm must match the grip that ultimately owns the package.

---

# 54. OWNERSHIP IDENTITY MUST REMAIN REAL

Do not fake successful checkout by spawning a replacement package in the customer's hand.

Where applicable verify:

**purchased object → packed object → handed object → carried-out object**

is the same logical purchase identity.

---

# 55. CHECKOUT ITERATION ROUNDS

Do not build checkout once.

Run explicit visual/play rounds.

Capture:

* customer arrival
* item staging
* card presented
* card reader
* cash presented
* drawer
* change
* packaging
* handoff
* customer carry-away
* reset

Continue iterating until no obvious issues remain.

---

# 56. TUTORIAL / FIRST-RUN ONBOARDING

A brand-new player must understand the game without external help.

Teach:

* movement
* camera
* interaction
* pickup
* placement
* first specimen
* cleaning
* cracking
* sawing
* polishing
* appraisal
* storage
* display
* selling
* checkout
* buying
* sourcing
* upgrading
* career progression

---

# 57. TUTORIAL MUST BE INTERACTIVE, NOT A WALL OF TEXT

Use short contextual steps.

Tutorial should:

* wait for player action
* show current key/controller binding
* highlight relevant world object
* acknowledge completion
* advance naturally

Avoid giant tutorial panels.

---

# 58. TUTORIAL ROBUSTNESS

Tutorial must:

* survive save/reload
* be skippable
* be replayable
* never deadlock
* never depend on short human-response timers
* respect remapped controls
* work with controller
* work with keyboard/mouse

---

# 59. FIRST-RUN STRANGER TESTING

Repeatedly run a completely fresh profile.

Ask:

> If I knew absolutely nothing about Geode Empire, would I understand this?

Record:

* blockers
* major confusion
* unnecessary friction
* minor polish

Fix blockers immediately.

---

# 60. COMPLETE UI DESIGN SYSTEM

Rebuild the overall UI presentation.

Define:

* typography
* spacing
* panel style
* borders
* icons
* hover
* focus
* selection
* disabled
* success
* error
* warning
* animation
* sound

The entire game should visually belong to one product.

---

# 61. SETTINGS UI REBUILD

Settings must look polished and actually work.

Audit:

* graphics
* resolution
* window mode
* FOV
* mouse sensitivity
* controller sensitivity
* inversion
* master volume
* music
* SFX
* UI scale
* vibration
* accessibility
* controls/rebinding

Every setting must be tested from interaction → runtime effect → save → reload.

---

# 62. KEY REBINDING

The current known limitation says key rebinding is absent.

Add proper rebinding.

Requirements:

* keyboard
* mouse where appropriate
* controller where appropriate
* conflict handling
* reset
* persistence
* dynamic tutorial/prompts

Never hardcode tutorial text such as `Press E` if the player remapped interact.

---

# 63. INVENTORY UI

Inventory should clearly communicate:

* specimen
* thumbnail
* mineral
* state
* size
* weight
* quality
* rarity
* damage
* appraisal/value
* location

Support:

* sorting
* filtering
* comparison
* controller navigation

But do not let inventory turn physical specimens into teleporting menu icons.

---

# 64. BUYING / SOURCING UI

Make purchasing easy to understand.

Show:

* product
* function
* price
* quantity
* current cash
* requirement
* delivery
* storage impact

Sourcing lots should communicate:

* dealer
* cost
* category
* risk
* expected quality range where appropriate

Do not expose hidden RNG values unnecessarily.

---

# 65. CAREER / OBJECTIVE UI

Make objectives clear without overwhelming the player.

Player should always be able to understand:

* current objective
* why it matters
* what unlocks next

Avoid giant checklists dominating gameplay.

---

# 66. UI RENDER QA

Test at:

* 1920×1080
* 2560×1440
* 3840×2160

Also reasonable UI scaling.

Detect:

* clipping
* overlap
* truncated labels
* unreadable text
* tiny controls
* lost controller focus
* stacked notifications

Use planted negative controls to prove the QA instrument can fail.

---

# 67. SPECIMEN DIVERSITY

Deepen meaningful variation in:

* shell
* rind
* cavity
* crystal habit
* crystal size
* crystal density
* mineral mix
* inclusions
* banding
* fractures
* imperfections
* weathering
* clarity
* color
* luster
* size
* weight

Do not create hundreds of shallow variants.

---

# 68. SPECIMEN-SPECIFIC GAMEPLAY

The specimen itself should affect processing.

Examples:

* fragile druzy requires care
* thick shells require stronger cracking
* cavity position influences cuts
* valuable crystals make poor cuts costly
* different minerals polish differently
* large specimens require larger supports

Player should learn to look at the actual rock.

---

# 69. PROCESSING FEEL

Improve:

* hammering
* chiseling
* cracking
* washing
* brushing
* sawing
* polishing
* appraisal

Focus on:

* timing
* inertia
* resistance
* impact
* alignment
* contact
* feedback

---

# 70. HAND / TOOL CONTACT

Hands should actually line up with tools.

Fix:

* floating grips
* fingers through handles
* tools through hands
* hands through rocks
* tools clipping machines

Use first-person screenshot review.

---

# 71. SAW INTERACTION QUALITY

Saw operation should visibly include:

* clamping
* carriage
* blade
* rotation
* cut progression
* coolant
* workpiece support
* believable sound
* final cut result

No magical state transition.

---

# 72. POLISH INTERACTION QUALITY

Polishing should show:

* disc motion
* contact
* slurry/wetness
* gradual finish improvement
* correct support
* material transformation

---

# 73. WASHING INTERACTION QUALITY

Washing should show:

* running water
* wet stone
* grime removal
* runoff
* appropriate sound
* tool contact

---

# 74. AUDIO PRODUCTION PASS

Add production-quality audio categories for:

* workshop ambience
* retail ambience
* footsteps
* doors
* drawers
* tools
* stone impacts
* cracking
* saw
* saw under load
* water
* polishing
* pickup
* placement
* checkout
* cash
* coins
* card reader
* packaging
* NPCs
* UI

---

# 75. MUSIC

V5 explicitly lists no music as a limitation.

V6 should add a restrained music system appropriate to the game unless a deliberate no-music design proves better through playtesting.

Music must not overwhelm workshop sounds.

Support volume settings.

---

# 76. VFX PASS

Use restrained physically believable:

* rock chips
* dust
* fracture particles
* water
* coolant spray
* slurry
* droplets
* wetness
* polish glints
* subtle crystal sparkle

No arcade loot effects.

---

# 77. CUSTOMER / SHOP LIFE

The shop should not feel empty.

Customer density should evolve sensibly with:

* reputation
* shop quality
* career progress

Avoid excessive crowds that break navigation or realism.

---

# 78. WORLD EVOLUTION

The business should visibly improve through the career.

Changes can include:

* nicer displays
* better machinery
* improved lighting
* better organization
* signage
* packaging
* trophies
* exceptional collection pieces
* storage quality
* retail presentation

Endgame should not visually resemble the starting shop.

---

# 79. CAREER STORYTELLING

Use light systemic/environmental storytelling.

Milestones should feel meaningful:

* first good specimen
* first major sale
* better supplier
* better machine
* first rare geode
* collector attention
* expanding shop
* prestigious collection

Do not turn the game into a dialogue-heavy RPG.

---

# 80. ECONOMY BALANCING

Balance:

* rough acquisition
* processing costs
* sale value
* rarity
* quality
* damage
* upgrades
* machine pricing
* customer budgets
* storage
* sourcing
* reputation

Actually play the economy.

Do not balance exclusively in spreadsheets.

---

# 81. REMOVE DEAD PROGRESSION

Look for:

* grind
* empty waiting
* upgrade gaps
* upgrades that arrive too late
* machines that are rarely affordable
* systems players can skip entirely
* dominant money strategies

The previous V5 note that polishing is rarely affordable in the ten-minute scripted career is a specific balancing signal to investigate.

---

# 82. FAILURE STATES

Support meaningful but fair consequences:

* damaged crystal
* poor crack
* bad cut
* overpolish/mistake where appropriate
* overpaying
* bad source lot
* poor storage decision
* bad sale decision

Do not make mistakes so destructive that the player becomes afraid to experiment.

---

# 83. STAGE-2 RECEIVING SYSTEM

V5 notes that Stage-2 receiving is still essentially the existing pallet grid.

Upgrade it into an actual receiving experience if it still feels placeholder-grade.

Potential flow:

* delivery arrives
* crates/boxes are received
* lot identity is readable
* packages are opened
* specimens are physically staged
* storage decisions matter

Do not leave Stage 2 feeling like a debug spawn grid.

---

# 84. PRIVATE COLLECTION / TROPHY STORY

Implement the useful V5 recommendation if it fits naturally:

collection specimens should retain history such as:

* source lot
* acquisition date
* processing tool
* notable damage
* appraisal history
* sale/display history where relevant

Expose this elegantly through provenance cards or inspection.

The trophy collection should represent the player's career.

---

# 85. CONTROLLER PARITY

Test every major system with controller:

* tutorial
* pickup
* cracking
* saw
* polish
* wash
* appraisal
* inventory
* buying
* settings
* checkout
* cash
* card
* career

Bindings merely existing is insufficient.

---

# 86. KEYBOARD / MOUSE QUALITY

Also verify:

* aiming
* cursor modes
* menus
* first-person movement
* object manipulation
* checkout
* processing

No control mode should feel second-class.

---

# 87. INPUT-FAMILY PROMPTS

Prompts should switch automatically:

keyboard/mouse ↔ controller.

Tutorial must follow the active input device.

---

# 88. SAVE / PERSISTENCE ABUSE TESTING

Save and reload during:

* tutorial
* carrying
* crack sequence
* opened geode
* wash
* saw
* polishing
* display
* sourcing
* customer browsing
* queue
* checkout
* cash
* drawer
* change
* card
* packaging
* handoff
* NPC carry-away
* career unlock
* upgrade

No duplication.

No lost money.

No orphaned specimens.

---

# 89. V5 SAVE MIGRATION

Existing V5 saves should remain usable where reasonably possible.

If migration is required, implement explicit migration logic.

Never silently corrupt an old career.

---

# 90. ZERO-TOLERANCE WORLD INTEGRITY

Continue auditing:

* interpenetration
* floating objects
* sink
* overhang
* clipping
* unsupported specimens
* machine intersection
* NPC overlap
* held-object overlap
* animation penetration
* door intersection
* signage collision

The current screenshots make visual integrity a hard acceptance criterion.

---

# 91. FULL-BOUNDS PLACEMENT

Maintain the V5 full-footprint system.

Test:

* rough
* opened halves
* slabs
* sawn pieces
* small
* medium
* large

Across:

* dealer
* wash
* bench
* saw
* lap
* rack
* display
* trophy
* retail
* receiving

---

# 92. NAVIGATION AUDIT

NPCs and players should not:

* walk through machines
* clip shelving
* intersect counter queues
* block doors permanently
* become trapped

Test navigation after every major scene-layout change.

---

# 93. PERFORMANCE BUDGET

Profile actual builds.

Measure:

* frame time
* GPU
* CPU
* memory
* draw calls
* triangles
* shader count
* material count
* lights
* animation

Do not optimize by guessing.

---

# 94. LOD / CULLING

Use LODs where beneficial for:

* complex geodes
* background props
* crowds

But hero assets near the player must retain visual quality.

---

# 95. MATERIAL / SHADER PERFORMANCE

The material overhaul should not accidentally create hundreds of unnecessary unique shader variants.

Share materials intelligently while preserving variation through supported parameters/textures.

Monitor shader/program count.

---

# 96. NPC PERFORMANCE

Use appropriate:

* animation optimization
* update throttling
* pooling
* LOD
* culling

without causing nearby customers to visibly downgrade.

---

# 97. REAL PLAYER-CAMERA VISUAL EVIDENCE

Do not certify visual quality from Blender renders alone.

Use the actual player camera.

Capture:

* 30 cm
* 60 cm
* normal interaction distance
* room distance

for hero assets.

---

# 98. BLENDER STUDIO RENDERS

Also use Blender studio renders to evaluate:

* topology
* silhouette
* material separation
* surface quality

This provides a neutral environment before Unity lighting is involved.

---

# 99. CONTACT SHEETS

Create contact sheets for:

* geode families
* rarity
* processing states
* machines
* NPCs
* checkout
* UI
* career stages

Compare before/after.

---

# 100. OBSERVE → MEASURE → FIX

Carry forward the best Golf Simulator development principle:

**Do not argue with the observed result.**

When something looks wrong:

1. reproduce
2. measure
3. identify cause
4. instrument if useful
5. add a negative control
6. fix
7. remeasure
8. visually inspect
9. regress

---

# 101. TEST THE TEST

A green audit is weak evidence if it has never demonstrated that it can detect a defect.

Where practical:

* plant a known temporary violation
* confirm the test catches it
* remove the violation
* rerun

Do not leave the temporary violation committed.

---

# 102. REVERT FAILED EXPERIMENTS

If a proposed fix does not improve the measured or visible result:

**revert it.**

Do not accumulate speculative complexity.

---

# 103. AUTOMATED TEST COVERAGE

Expand tests covering:

* V5 regression
* geometry invariants
* geode states
* crack clearance
* placement
* collision
* saw
* polish
* wash
* NPC pickup
* NPC carry
* queue
* checkout
* cash
* card
* packaging
* handoff
* tutorial
* UI
* rebinding
* settings
* inventory
* sourcing
* economy
* career
* save/reload
* migration
* controller

---

# 104. FULL NEW-SAVE PLAYTHROUGHS

Repeatedly start from zero.

Actually play:

* tutorial
* first rock
* wash
* crack
* appraisal
* first sale
* first purchase
* sourcing
* saw
* polish
* displays
* checkout
* upgrades
* Stage 2
* Stage 3
* endgame

Do not rely only on scripted state injection.

---

# 105. MULTIPLE CAREER RUNS

Perform multiple career runs/seeds where useful.

Look for:

* progression dead ends
* unlucky economies
* dominant strategies
* impossible upgrade sequences
* inconsistent tutorials
* rarity anomalies

---

# 106. STANDALONE BUILD TESTING

Do not certify V6 from the editor alone.

Test real standalone builds.

Verify:

* startup
* save path
* first run
* input
* graphics
* tutorial
* checkout
* audio
* controller
* resolution
* career
* quit/reload

---

# 107. STARTUP / BUILD WARNINGS

Investigate the existing informational warnings:

* pre-baked mesh collision off for prop meshes
* Pipeline package lacking runtime config

Determine whether they are harmless.

Fix them if they represent actual production risk.

Do not merely suppress useful warnings.

---

# 108. MUSIC / AUDIO SETTINGS

Since audio scope is increasing, verify:

* master
* music
* SFX
* ambience
* UI

settings persist correctly.

---

# 109. CLEAN GIT DISCIPLINE

Keep `origin/main` safe.

No:

* force push
* history rewrite
* destructive reset of known-good history

Use milestone commits.

---

# 110. V6 MILESTONE STRUCTURE

Suggested known-good milestones:

### V6.0

V5 baseline + authoritative V6 document + baseline screenshots

### V6.1

Material pipeline + geode hero quality

### V6.2

Machines / workshop hero assets

### V6.3

Cracking / processing feel

### V6.4

NPC visual / animation / pickup

### V6.5

Physical checkout

### V6.6

Tutorial / UI / settings / rebinding

### V6.7

Audio / VFX / lighting / presentation

### V6.8

Economy / progression / world evolution

### V6.9

Persistence / performance / controller / standalone

### V6 FINAL

Production Alpha acceptance

Do not create a milestone while knowingly broken.

---

# 111. DO NOT RACE AHEAD

Do not work deeply on Phase 8 while Phase 1 still looks bad.

Required priority:

1. geodes
2. materials
3. machines
4. lighting
5. cracking/reveal
6. NPCs
7. checkout
8. onboarding/UI
9. processing feel
10. audio/VFX
11. economy/career
12. QA/performance

Parallelize only when truly independent.

---

# 112. EXPLICIT OUT-OF-SCOPE ITEMS FOR V6

Do not build:

* multiplayer
* MMO architecture
* VR
* console ports
* gigantic open world
* Steam Workshop
* full modding SDK
* DLC infrastructure
* live service
* dozens of new locations
* giant narrative campaign
* hundreds of shallow minerals
* major localization rollout
* marketing trailers
* store-page production

Those belong later.

---

# 113. V7 / V8 / RELEASE ROADMAP

Maintain the scope boundary:

### V6

Production Alpha quality.

### V7

Content expansion based on actual V6 playtesting.

### V8 / Beta

Accessibility, balancing, broader hardware testing, external-playtest issues, bug eradication.

### Release Candidate

Platform integration, achievements, localization targets, packaging, release requirements, final optimization.

Do not steal future scope at the expense of V6 quality.

---

# 114. FINAL GEODE ACCEPTANCE GATE

Before completion, inspect representative:

* cheap geode
* common geode
* medium geode
* valuable geode
* rare geode
* huge geode
* opened geode
* sawn geode
* damaged geode
* polished geode

from close first-person range.

None may visibly resemble:

* dough
* foam
* a low-poly blob
* a painted ball
* sparse spikes inside a brown shell

---

# 115. FINAL MACHINE ACCEPTANCE GATE

Walk directly up to:

* saw
* crack bench
* wash
* polish
* appraisal
* checkout

Inspect every major visible component.

No obvious:

* crude box
* random cylinder
* unsupported part
* faceting
* flat plastic material
* floating component
* wrong scale
* impossible mechanical relationship

---

# 116. FINAL MATERIAL ACCEPTANCE GATE

From normal gameplay distance, the player must be able to visually identify material categories without relying solely on color.

Stone must look like stone.

Metal must look like metal.

Wood must look like wood.

Crystal must look like crystal.

Polished mineral must look polished.

Wet rock must look wet.

If everything still has the same soft Unity-material response:

**V6 FAILS.**

---

# 117. FINAL NPC ACCEPTANCE GATE

Customers must no longer appear to be test mannequins.

Verify:

* believable silhouettes
* plausible movement
* feet
* hands
* reaches
* pickup
* holding
* payment
* handoff
* walking away

No obviously broken pose may remain.

---

# 118. FINAL CHECKOUT ACCEPTANCE GATE

Manually complete:

### CASH SALE

and

### CARD SALE

from normal gameplay.

Verify:

* customer
* goods
* payment
* drawer/card reader
* packaging
* handoff
* ownership
* carry-away
* reset

No shortcuts.

---

# 119. FINAL TUTORIAL ACCEPTANCE GATE

Start a completely fresh profile.

Use no developer knowledge.

Confirm the tutorial teaches the entire basic loop naturally.

Test keyboard/mouse.

Test controller.

Test save/reload mid-tutorial.

---

# 120. FINAL UI ACCEPTANCE GATE

Inspect:

* settings
* controls
* inventory
* buying
* sourcing
* career
* appraisal
* checkout support UI
* pause
* save/load

Nothing should look like a Unity debug menu or generic mobile UI.

---

# 121. FINAL CAREER ACCEPTANCE GATE

Complete the career from a new save without cheats/debug teleporting.

The progression should be:

* understandable
* rewarding
* balanced
* visually evolving
* mechanically diverse

Stage 3/endgame should feel materially different from the beginning.

---

# 122. FINAL WORLD-INTEGRITY WALK

Walk the entire accessible world.

Inspect:

* floor
* ceiling
* corners
* doors
* signs
* furniture
* machines
* shelves
* specimens
* customer routes
* checkout
* receiving
* trophy/private collection

Fix every obvious visual-integrity defect.

---

# 123. FINAL STANDALONE ACCEPTANCE RUN

Use the real standalone build.

Start fresh.

Play through representative beginning, middle and late game.

No Editor-only shortcuts.

No hidden debug state.

---

# 124. REQUIRED FINAL VISUAL EVIDENCE

Final report must include evidence paths for:

### GEODES

* rough close-up
* opened close-up
* rare close-up
* polished close-up

### MACHINES

* saw
* cracking
* wash
* polish
* appraisal

### NPC

* idle
* walk
* pickup
* inspect
* checkout
* carry-away

### RETAIL

* showroom
* queue
* cash sale
* card sale
* packaging
* handoff

### UI

* tutorial
* inventory
* settings
* buying

### CAREER

* early
* mid
* endgame

---

# 125. REQUIRED FINAL TECHNICAL EVIDENCE

Provide:

* regression test results
* new V6 tests
* world-integrity results
* collision results
* placement results
* UI sweep results
* controller results
* save/persistence results
* career results
* standalone build result
* performance measurements

---

# 126. FINAL HUMAN-QUALITY QUESTION

Ask:

> Could I show these screenshots to someone with no knowledge of this project and have them believe this is a professionally developed PC game in active production?

If the answer is no:

**KEEP WORKING.**

---

# 127. SECOND FINAL QUESTION

Ask:

> Is cracking a geode now genuinely satisfying enough that I want to crack another one immediately just to see what is inside?

If the answer is no:

**KEEP WORKING.**

---

# 128. THIRD FINAL QUESTION

Ask:

> Can I walk directly up to the saw, rock, countertop, NPC and display cabinet without the illusion falling apart?

If the answer is no:

**KEEP WORKING.**

---

# 129. CURRENT SCREENSHOTS ARE THE NEGATIVE BASELINE

The current screenshots showing:

* the dark crude saw
* the dough-like rough geode
* the muddy opened geode

represent the visual result V6 must **materially surpass**.

Do not accept a result that is merely 10% better.

The improvement should be immediately obvious in side-by-side comparison.

A player should not have to be told which screenshot is V6.

---

# 130. FINAL RULE

Do not lower the quality bar because the run becomes expensive, large, slow or difficult.

Do not mistake:

**more polygons**

for:

**better art.**

Do not mistake:

**new shaders**

for:

**better materials.**

Do not mistake:

**tests passing**

for:

**good gameplay.**

Do not mistake:

**procedural complexity**

for:

**believable geology.**

Do not mistake:

**NPC navigation working**

for:

**good characters.**

Do not mistake:

**a sale completing**

for:

**good checkout gameplay.**

Do not mistake:

**a tutorial panel appearing**

for:

**good onboarding.**

Do not mistake:

**a Blender export succeeding**

for:

**a good in-game asset.**

Use Blender seriously.

Use Unity observation continuously.

Play the game yourself.

Inspect from close range.

Use screenshots.

Use contact sheets.

Measure geometry.

Audit materials.

Test collisions.

Test placement.

Test persistence.

Test controller.

Test keyboard/mouse.

Test UI.

Test checkout.

Test new-player onboarding.

Build standalone versions.

Keep clean known-good milestone commits.

And continue iterating until **Geode Empire genuinely looks and feels like a production-quality PC game instead of the prototype shown in the current screenshots.**

**Work autonomously until the complete V6 definition of done is genuinely satisfied. Do not lower the quality bar to finish faster.**
