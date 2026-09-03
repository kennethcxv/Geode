GEODE EMPIRE — V5 V2

FULL CAREER, SPECIMEN MASTERY, HANDS-ON PROCESSING & VISUAL FIDELITY ALPHA

Fable 5.1 / Claude Code Autonomous Production Directive

Use this ONLY after GEODE_EMPIRE_V4_PROCESSING_DEPTH_MIDGAME_ALPHA_V2.md is genuinely complete and its Definition of Done passes.

V3 proved the first hour.
V4 proves the processing foundation and 4–8 hour midgame.
V5 V2 must turn Geode Empire into a feature-complete single-player career alpha with:

a polished 15–25+ hour career,

final hands-on rock processing,

substantially higher visual fidelity,

stronger specimen uniqueness and scarcity,

richer sourcing/selling,

a complete UI/UX pass,

Stage-3 progression,

a career conclusion,

and a reason to keep playing afterward.

This should be the LAST large feature-development milestone before a later V6 beta/release-candidate milestone.

0. AUTHORITY / CONTINUITY

Before changing anything, read in full:

CLAUDE.md

GEODE_EMPIRE_FINAL_DESIGN.md

GEODE_EMPIRE_FABLE_GOAL.md

GEODE_EMPIRE_FINAL_CONTINUATION_PROMPT_V3.md

GEODE_EMPIRE_V4_PROCESSING_DEPTH_MIDGAME_ALPHA_V2.md

latest project memory/status notes

V4 final completion report

Git status / recent commits / configured remote

Authority:

CLAUDE.md

this V5 V2 directive

V4 quality gates

V3 quality gates

final design

Do not restart or re-scaffold proven systems.
Do not replace stable systems just because another architecture looks cleaner.

1. V5 V2 PRODUCT MISSION

Build a feature-complete single-player career alpha with a first-career target of roughly:

15–25+ hours

and endless continuation afterward.

The player fantasy should become:

“I can tell rocks apart.”

“I know what clues matter.”

“I know which tool to use.”

“I physically prepare and process the rock.”

“My skill changes the result.”

“A truly rare specimen is actually rare.”

“When I finally find one, I can SEE why it is special.”

“My workshop, shop and collection show my growing expertise.”

“I want one more rock.”

2. V4 REGRESSION GATE

Before new V5 work, verify:

compile clean

normal Console clean

Title

New Game

Continue

settings

controller

cleaning

hand inspection

tap/loupe

hammer/chisel

small/medium/large/oversized handling

saw

cut-plane consequences

piece lineage

polishing

retail

customer navigation

checkout

collection

source lots

Stage-2 progression

save/reload

standalone build

Fix regressions first.

Commit/push:
Verify V4 baseline before V5

Do not rerun a giant redundant audit.

3. V5 V2 TOP PRIORITIES

Priority order:

VISUAL FIDELITY OF ROCKS / TOOLS / MACHINES / WORKSHOP ASSETS

HANDS-ON PROCESSING QUALITY

SPECIMEN UNIQUENESS + RARITY SCARCITY

SAW INTERACTION DEPTH

VARIABLE ROCK PREPARATION

HAMMER / CHISEL FEEL

COLLISION / CONTACT QUALITY

INSPECTION / VERIFICATION

UI / UX

SOURCING / ECONOMY

COLLECTION / RETAIL / BUYERS

STAGE-3 / ENDGAME

FULL CAREER QA

Do not jump into late-career systems while the stones, saw and tools still look low quality.

4. GRAPHICS / ASSET FIDELITY IS A HARD QUALITY GATE

V5 V2 must perform a serious visual-quality pass on ALL important first-person assets.

The target is:

commercial simulator-game quality

not:

prototype geometry with acceptable functionality.

The player will spend large amounts of time looking closely at:

unopened rough

opened geodes

crystal fields

sawn faces

polished faces

hammer

chisel

wedge

saw

clamp

blade

coolant system

wash tub

brush

loupe

appraisal tools

geode cracker if added

display fixtures

register

major workshop equipment

These must hold up at close range.

5. BLENDER IS THE PRIMARY ASSET-QUALITY WORKFLOW

For asset work, use headless Blender through:

./Tools/blender.sh

Use deterministic scripts where practical.

Do NOT accept crude geometry because it is faster to code.

For every important asset:

inspect the current mesh

identify silhouette/detail/material weaknesses

rebuild or improve it in Blender

export correctly

import in Unity

inspect at game camera distance

inspect close-up

inspect from multiple angles

test collisions / interaction

only then accept it

If a mesh looks like a placeholder, improve it.

6. DO NOT BLINDLY MAXIMIZE POLYGON COUNT

Higher quality does NOT mean “maximum polygons everywhere.”

Use geometry where it visibly improves:

silhouette

curved surfaces

edge quality

tool heads

handles

blade housing

clamps

machine controls

crystal morphology

rock shape

close-up props

Use:

bevels

weighted normals

smooth shading where appropriate

proper hard edges

normal/detail maps

efficient topology

Preserve performance.

The rule is:

Spend polygons where the player can see them.

7. HERO-ASSET QUALITY TARGET

The following are HERO ASSETS and require a dedicated close-up pass:

rocks / geodes

crystals

hammer

chisel

wedge / heavy hammer

trim saw

blade

saw clamp / vise

saw carriage

coolant nozzle/tray

polishing lap

loupe

appraisal bench

geode cracker if implemented

These should have:

coherent real-world proportions

believable construction

appropriate edge bevels

correct normals

clean topology

no accidental protrusions

no unexplained rods

no floating parts

no intersecting components

no obviously stretched textures

no malformed pivots

no wrong-scale handles

no placeholder cylinders where real shape matters

8. ASSET SANITY AUDIT

Before accepting any hero asset, check:

scale

dimensions

orientation

pivot/origin

local axes

normals

smoothing

UVs

material slots

mesh bounds

collider

interaction point

attachment points

animation axes

camera-facing presentation

Visually rotate the asset 360°.

If something looks like:

random rod

floating wedge

detached handle

clipped blade

impossible hinge

support bar through another mesh

arbitrary primitive stuck to the side

fix it.

Do not excuse visually broken geometry as “background detail” if the player can see it.

# HARD GATE — FULL WORLD, ASSET, COLLISION & GEOMETRY REBUILD

V5 must not preserve the current prototype/mobile-game visual quality.

The current game has strong systems, but many visible assets, specimen interiors, machines, furniture, workshop props, signs, fixtures, surfaces and environmental elements still look like prototype geometry. V5 must aggressively replace, rebuild, remodel and re-author these assets rather than merely recoloring, rescaling, adding shaders, or placing more props around them.

THIS IS A RELEASE-QUALITY VISUAL AND PHYSICAL-INTEGRITY GATE.

V5 is NOT complete while the workshop still reads visually as a low-budget/mobile game or while obvious clipping, phasing, floating, intersection, unsupported placement, low-poly faceting or malformed geometry remains.

## 1. BLENDER REBUILD IS MANDATORY

Return heavily to Blender.

Do not treat the existing meshes as sacred.

For EVERY important visible asset:

1. Inspect it closely in Unity.
2. Identify whether the geometry itself is good enough.
3. If not, open/rebuild/remodel it in Blender.
4. Fix silhouette, dimensions, bevels, topology, normals, smoothing, pivots and material separation.
5. Re-export.
6. Inspect it again in Unity from first-person gameplay distance.
7. Test collisions and interactions.
8. Reject it and iterate again if it still looks like prototype geometry.

Do not solve poor geometry primarily through materials.

A good shader on a poor mesh is still a poor asset.

Hero assets should receive as much geometry and Blender attention as necessary while remaining performance-conscious.

This particularly applies to:

- unopened geodes
- broken geode shells
- geode interiors
- crystal formations
- cavity walls
- fracture surfaces
- saw-cut surfaces
- polished surfaces
- hammer
- chisel
- splitting wedge
- cracking bench
- workbench
- lapidary saw
- saw carriage
- clamp and vise
- blade
- coolant hardware
- flat lap
- wash station
- loupe
- scale/appraisal equipment
- receiving crates
- storage racks
- dealer outbox
- display cabinets
- private collection area
- trophy wall
- shelving
- chairs
- tables
- doors
- signs
- counters
- lamps
- wall fixtures
- workshop architectural trim
- every frequently viewed prop

## 2. REMOVE THE "MOBILE GAME" LOOK

The final V5 workshop should visually read as a believable PC/console-quality lapidary workshop and mineral business.

Reject:

- visibly primitive cubes/cylinders used as final assets
- crude silhouettes
- oversized bevels
- toy-like proportions
- overly saturated/simple materials
- uniformly clean surfaces
- flat-looking crystal cavities
- repetitive procedural shapes
- obvious low-poly faceting
- polygon streaks across curved surfaces
- visible normal/smoothing errors
- stretched textures
- obvious tiling
- fake-looking metal
- plastic-looking stone
- perfectly sharp machine edges
- perfectly clean machinery
- floating details
- arbitrary rods/bars sticking through objects
- geometry created only because it was convenient to script

Curved objects must look curved.

If polygon edges, radial streaks, triangular shading patterns or low-resolution facets are visible from normal gameplay distance, increase geometry quality and/or repair topology/normals.

Use appropriate Blender techniques such as:

- proper topology
- sufficient radial/segment resolution
- bevels
- support loops
- Shade Smooth / Smooth by Angle where appropriate
- corrected/custom/weighted normals where appropriate
- subdivision where justified
- hard edges where mechanically appropriate
- UV cleanup
- realistic material separation
- correct scale
- realistic proportions

Do not hide bad topology with excessive smoothing.

## 3. GEODES AND CRYSTAL INTERIORS REQUIRE A MAJOR QUALITY REBUILD

The geodes are the star of Geode Empire.

Their interiors must receive disproportionate effort.

Current procedural interiors must be critically reviewed and rebuilt wherever required.

A high-quality opened geode should have:

- believable shell thickness
- irregular natural cavity walls
- convincing matrix transition
- realistic rind
- fracture texture
- crystals that visibly emerge from / attach to the matrix
- no floating crystals
- no crystals visibly passing through unrelated crystals
- no obvious circular placement rings
- no evenly spaced procedural repetition
- varied crystal size
- varied direction
- clustering
- occlusion
- secondary growth
- natural empty spaces
- convincing cavity depth
- family-specific geological structure
- realistic cut/fracture boundaries
- convincing crystal-to-rock contact
- correct translucency / roughness / metallic response for the mineral
- sufficient geometry for close first-person inspection

Exceptional specimens must look exceptional BEFORE the UI tells the player they are valuable.

The reveal should work visually without requiring a rarity border.

Perform close-up visual QA on multiple examples of every major mineral family.

## 4. WORKBENCH AND MACHINES MUST BE COMPLETELY RE-EVALUATED

The cracking bench/workbench and lapidary equipment are seen constantly.

They cannot remain prototype-quality.

Rebuild them in Blender if necessary.

They should have believable:

- construction
- thickness
- joinery
- fasteners
- feet
- supports
- handles
- hinges
- clamps
- moving parts
- material transitions
- wear
- contact surfaces
- clear functional purpose

Nothing should look like an arbitrary collection of primitives.

A player should be able to look at the machine and understand how it could physically function.

## 5. ZERO-TOLERANCE INTERPENETRATION / PHASING PASS

The current build contains examples such as:

- a chair intersecting the private collection area
- the dealer outbox sign intersecting/phasing through a door
- specimens placed in the dealer outbox partly hanging outside the wooden platform

These are examples of V5-blocking defects.

Search the ENTIRE playable workshop for similar issues.

No intentionally placed static object may visibly occupy the same physical space as another unrelated solid object.

Fix:

- furniture through walls
- furniture through other furniture
- signs through doors/walls
- legs through floors
- props floating above surfaces
- props embedded below surfaces
- shelves intersecting walls incorrectly
- lights intersecting ceilings
- doors clipping signage
- machines intersecting architecture
- crates intersecting pallets
- cabinets intersecting trim
- chairs clipping collection fixtures
- geometry crossing walkways
- decorative objects crossing interactive zones

Do not simply move one known example.

Perform a systematic workshop-wide audit.

## 6. PLACEMENT ZONES MUST PHYSICALLY SUPPORT THEIR CONTENT

Every placement interaction must result in believable physical placement.

The dealer outbox is currently capable of placing a geode partly on the wooden platform and partly outside it. This is unacceptable.

For EVERY placement zone:

- derive usable placement bounds from the actual supporting surface
- account for specimen footprint/radius, not only its pivot
- account for cut halves/slabs having different bounds
- prevent placement outside the support area
- prevent overlapping occupants
- prevent penetration into walls/rails/signs
- prevent objects floating above the support
- prevent objects sinking through it
- ensure large rocks only occupy positions that physically fit
- reject placement with a clear explanation when no valid position exists

Test placement using:

- small rough
- medium rough
- large rough
- oversized rough where allowed
- opened halves
- sawn halves
- slabs
- polished slabs

Test all appropriate zones including:

- dealer outbox
- appraisal/scale
- wash tub
- cracking bench
- saw clamp
- saw output tray
- polishing station
- storage rack
- display cabinet
- trophy/private collection areas
- retail shelves
- receiving area

A placement zone is not correct merely because the specimen's pivot lies inside it.

THE ENTIRE SPECIMEN BOUNDS MUST BE PHYSICALLY SUPPORTED.

## 7. COLLIDERS MUST MATCH WHAT THE PLAYER SEES

Do not use enormous coarse box colliders around complex hero objects if they create visibly incorrect interactions.

Use efficient compound colliders where necessary.

Verify:

- workbench edges
- shelves
- cabinets
- saw
- clamp
- lap
- wash station
- crates
- doors
- counters
- display furniture
- collection fixtures
- major environmental props

The player should not:

- walk through visible solids
- collide with invisible space far from geometry
- become wedged between props
- push rocks through furniture
- place objects through another collider
- see held specimens clip deeply through machinery

Interactive contact should visually agree with collision/contact logic.

## 8. AUTOMATED + VISUAL COLLISION AUDIT

Extend the existing collision audit tooling.

Run automated checks for:

- static renderer/mesh bounds intersections
- placement-zone containment
- invalid world-space overlaps
- objects below floors
- objects outside supporting surfaces
- inaccessible interaction points
- blocked doors/passages
- navigation clearance
- specimen overlap after placement
- oversized-object clearance

But DO NOT trust automated bounds checks alone.

Some valid meshes have overlapping bounds and some visually broken arrangements may pass a bounds test.

Therefore every major area also requires human-style visual inspection through Unity screenshots/captures and actual first-person movement.

## 9. FULL WORKSHOP VISUAL WALKTHROUGH

Before V5 can pass:

Perform a slow first-person visual walkthrough of the entire accessible workshop.

Inspect:

- floor level
- eye level
- shelves
- corners
- doorways
- underneath/behind major fixtures
- receiving area
- cracking station
- saw area
- wash area
- polishing area
- appraisal area
- retail area
- dealer area
- private collection
- trophy wall
- storage
- Stage-2 additions

Inspect from multiple angles.

Capture screenshots of every major area.

Zoom into suspicious geometry.

Fix every obvious defect found.

Then repeat the walkthrough.

Do not stop after one pass.

## 10. HERO-ASSET CLOSE-UP GATE

Capture close-up screenshots for:

- unopened rough
- opened geode interior
- exceptional geode
- damaged geode
- cut half
- slab
- polished slab
- hammer
- chisel
- cracking workbench
- saw
- saw clamp + specimen
- flat lap
- wash station
- appraisal equipment
- display cabinet
- dealer outbox
- private collection
- Stage-2 workshop

Review these at full resolution.

If an asset visibly contains:

- low-poly streaks
- faceting
- broken normals
- ugly triangulation
- clipping
- primitive-looking construction
- visibly incorrect material response
- unrealistic proportions
- floating/intersecting components

it fails V5 and must be rebuilt/fixed.

## 11. V5 WORLD INTEGRITY DEFINITION OF DONE

V5 may NOT be marked complete until:

- the workshop no longer visually reads as a mobile/prototype environment
- hero assets have received serious Blender remodeling
- geode interiors are substantially more convincing than V4
- workbench and machines have materially better geometry
- obvious low-poly streaking/faceting is gone
- no obvious static-object interpenetration remains
- no signs/furniture visibly phase through architecture
- dealer outbox and other placement zones fully support objects
- specimen placement respects complete physical bounds
- visible colliders agree with geometry
- first-person traversal has no major snag/wedge points
- major areas pass repeated visual walkthroughs
- hero assets pass close-up screenshot review
- automated collision/placement audits are clean
- keyboard/controller interactions remain functional after remodeling
- saves and progression remain intact after asset replacement

Do not waive this gate because the underlying gameplay works.

A system that functions but looks unfinished is not V5-complete.

V5 should be the milestone where Geode Empire stops looking like a sophisticated prototype and begins looking like a serious commercial PC game.

9. COLLIDER QUALITY FOR ASSETS

Never use an inaccurate collider if it ruins first-person interaction.

For major tools/machines:

visual blade aligns with cutting plane

clamp collider matches jaws

rock rests on actual support

hammer contacts chisel head

chisel contacts shell

saw blade intersects the intended kerf

specimen does not hover

specimen does not sink

hands/tools do not phase through machine housing

pieces do not launch or clip after cut

Prefer simple compound colliders when they accurately approximate the asset.

Do not use needlessly expensive MeshColliders everywhere.

10. FINAL ROCK LIFECYCLE

Definitive flow:

RECEIVE
→ TRIAGE
→ CLEAN IF NEEDED
→ INSPECT
→ OPTIONAL PREDICTION
→ CHOOSE PROCESS
→ SPECIMEN-SPECIFIC PREP
→ SECURE / POSITION
→ PROCESS
→ IMMEDIATE REVEAL
→ QUICK POST-CLEAN / RINSE IF USEFUL
→ VERIFY
→ OPTIONAL SECONDARY PROCESS
→ APPRAISE
→ KEEP / DEALER / RETAIL / SPECIAL BUYER / AUCTION
→ CATALOG / DISPLAY
→ NEXT ROCK

Not every rock uses every step.

11. EVERY PHYSICAL STEP MUST EARN ITS PLACE

Every physical step must:

reveal information

require judgment

require skill

alter outcome

build anticipation

create satisfying sensory feedback

create attachment

or support a meaningful economic decision

If it does none of those:

shorten it

automate it

or remove it

No chores for their own sake.

12. SPECIMEN-SPECIFIC PREPARATION

Preparation must vary by specimen.

Do NOT create one universal:

place rock → press button → cut

flow.

Different rocks may need different preparation based on:

size

weight

shape

flatness

shell texture

seam quality

fragility

cavity suspicion

hardness/toughness

existing cracks

dirt

natural chip

source material

desired output

Examples:

small clean geode

inspect

position

crack

dirty estate rock

wash

inspect

tap

choose seam

crack/saw

angular rock

identify stable support face

shim/support if necessary

clamp carefully

large heavy rock

use heavy cradle

stronger support

more secure clamp

oversized rock

heavy equipment

larger clamp/saw/cracker

possibly mark and re-seat multiple times

valuable agate/nodule

wash

inspect band direction

orient for strongest cross-section

choose cut plane

clamp

saw

polish

13. PREP MUST BE PHYSICAL BUT NOT TEDIOUS

Prep interactions can include:

rotate rock

choose rest orientation

place support pad

close clamp

position guide

select blade/profile where meaningful

mark cut

adjust coolant

confirm clearance

Target:

a few seconds for ordinary material,
longer only for valuable/large/special specimens.

Do not make every rock a setup simulator.

14. SPECIMEN VISUAL UNIQUENESS

Every generated specimen should strongly differ perceptually.

Same-family specimens should vary in:

silhouette

size

axes

rind

texture

weathering

dirt

staining

natural chips

shell thickness

cavity geometry

chamber count

crystal count

crystal size

crystal density

centerpiece placement

clarity

saturation

hue

zoning

inclusions

secondary mineral

rare traits

damage

cut orientation

polish

Two rocks of the same mineral family should look related, not duplicated.

15. SAME-FAMILY QUALITY TEST

For every major family, stage:

low

average

good

exceptional

museum/world-class

damaged

under identical lighting.

Ask:

can I tell which is better?

can I see why?

does the rare one look rare?

does damage look visible?

are colors and crystal geometry actually different?

If not, improve generator/materials/geometry.

16. RARITY MUST BE GENUINELY SCARCE

High-end specimens should NOT appear constantly.

The player should not become numb to:

Exceptional

Museum Grade

World Class

These need meaningful scarcity.

Exact economy balance should be validated with simulations, but initial design intent:

Common / ordinary:

majority of rocks

Decent:

frequent

Good:

uncommon

Exceptional:

clearly rare

Museum Grade:

very rare

World Class:

extremely rare

A normal early player should NOT expect a museum/world-class rock every crate.

17. RARITY TARGET GUIDANCE

Do not blindly hardcode these exact numbers without simulation, but use as an initial sanity target for BASE/ordinary sourcing:

Common: ~55–70%

Decent: ~20–30%

Good: ~7–14%

Exceptional: ~1–4%

Museum Grade: ~0.2–1%

World Class: ~0.02–0.2%

Premium/targeted late-game sources can improve odds, but should NEVER make top-tier pieces routine.

Even premium sources should mostly improve:

floor

consistency

family targeting

cleanliness

clue quality

rather than turning every crate into jackpots.

18. RARE DOES NOT MEAN COLOR BORDER

Rare specimens must visibly differ BEFORE UI confirmation.

Use:

unusual cavity architecture

enormous centerpiece

exceptional clarity

intense believable saturation

strong zoning

secondary mineral combinations

rare habits

extraordinary banding

pristine terminations

symmetry

dramatic asymmetry

large preserved internal field

uncommon source/formation

No fantasy glow.
No casino beams.

19. SPECIMEN CONTENT TARGET

By V5 end, aim for roughly:

20–28 strong mineral/formation families/archetypes total

only if quality supports it.

Add ~6–10 excellent archetypes rather than many recolors.

20. STONE MATERIAL QUALITY

Improve:

micro-roughness

shell pores

chipped surfaces

rind depth

stain breakup

clay/dust

wetness

sawn texture

polishing

fracture frost

crystal translucency

crystal inclusions

metallic response

edge highlights

cavity depth

Avoid:

plastic

flat uniform color

obvious texture tiling

excessive perfect smoothness

21. WET / DRY

Fresh wash:

temporarily darker

richer

slight water sheen

subtle beads/drips

Fresh saw:

coolant/slurry

wet cut face

Dry:

returns toward normal

Polished:

stays permanently higher finish

Wetness does not change permanent appraisal value.

22. CRYSTAL GEOMETRY QUALITY

For hero mineral families, improve crystal meshes in Blender.

Use family-appropriate:

prisms

terminations

cubes

blades

needles

sprays

clusters

botryoidal forms

overgrowth

Avoid crude repeated cones.

Use enough geometry for clean close-ups while maintaining performance.

23. CRYSTAL PLACEMENT QUALITY

Avoid:

rings

perfect grids

equal spacing

identical hero crystals

repeated centerpiece angles

obvious seed patterns

Placement must follow cavity shape and formation style.

24. HANDS-ON HAMMER / CHISEL

Hammer/chisel remains first-class forever.

The player must physically:

place chisel

choose seam region

choose force

strike

read result

rotate

reposition

continue fracture

Different rocks respond differently.

Inputs interact with:

shell thickness

seam quality

support

size

toughness

prior cracks

strike placement

angle

force

Skill strongly influences results, but geology matters.

25. HAMMER FEEL

Improve:

contact animation

wind-up

strike acceleration

impact pause

recoil

camera micro-response

controller vibration

sound

chisel ring

rock thud

crack propagation

debris

dust

visible chip

A strike should FEEL like energy moved into the rock.

26. HAMMER FAILURE STATES

Possible:

weak bite

slip

surface chip

local crack

lucky propagation

overstrike

internal damage

crystal break

no useful progress

Do not make failure random.

The player should understand why most failures happened.

27. SAW IS A HERO INTERACTION, NOT A BUTTON

V5 V2 must treat the saw as one of the game’s signature interactions.

Do NOT allow:

put rock in → click Cut → canned animation → two pieces

as the final implementation.

The player must physically operate the saw.

28. SAW INTERACTION LOOP

A good saw workflow:

inspect the specimen

wash if needed

decide desired result

place rock on saw carriage

rotate/orient rock

move/select cut plane

position clamp jaws

secure rock

verify blade clearance

start motor

enable/verify coolant

physically feed carriage / specimen through blade

FEEL changing resistance

hear motor load change

watch kerf grow

manage feed rate

finish cut

motor unloads

piece separates

rinse / reveal cut face

inspect result

Not every step needs a separate button.
The whole sequence must feel physical.

29. SAW CUT SHOULD VARY BY ROCK

Every specimen should not saw identically.

Saw behavior may depend on:

size

cross-sectional area

shell toughness

crystal density

cavity

matrix

blade wear

blade profile

clamp stability

coolant

cut angle

feed rate

existing cracks

Examples:

dense / thick material

higher resistance

deeper motor load

slower safe feed

hollow geode

shell resistance

sudden load drop through cavity

resistance again on far shell

crystal-rich region

risk of chipping near blade path

large rock

stronger clamp

slower feed

larger blade requirement

bad orientation

unstable or poor face

worse final presentation

30. SAW RESISTANCE / FEEDBACK

The player should FEEL the saw cutting.

Use coordinated feedback:

Audio

motor idle

motor load

abrasive grind

coolant hiss

pitch/load variation

breakthrough sound

piece drop/rest sound

Visual

moving blade

coolant stream

wet kerf

slurry

cut dust/sludge appropriate to wet saw

growing cut line

subtle machine vibration

visible carriage movement

Haptics

light motor vibration

stronger resistance under load

small changes through density

breakthrough release

Motion

carriage feels resistant

feed rate changes

not simply fixed-speed animation

31. SAW FEED RATE MATTERS

The player should control feed.

Too slow:

safe

time cost

little downside

Good:

efficient

clean

Too fast:

motor bog

blade wear

vibration

chipping

worse face

possible clamp movement if poorly secured

Do not make this twitch-game precise.

Use a broad skill window.

32. SAW MOTOR LOAD

Create a clear but non-arcade way to read load.

Possible:

motor pitch

sound strain

vibration

subtle analog needle/current meter on machine

coolant/slurry behavior

Avoid giant HUD “OVERHEAT” bars unless absolutely necessary.

Prefer machine feedback.

33. SAW BLADE / KERF

Cut must correspond exactly to blade plane.

The blade should visibly pass through the rock.

The resulting pieces must match:

plane

orientation

kerf loss

cavity exposure

crystal truncation

lineage

No mismatch between animation and resulting geometry.

34. SAW COLLISION GATE

Zero tolerance for common saw failures:

blade visually misses rock while cut progresses

blade clips housing

clamp intersects rock

rock floats above carriage

rock sinks through support

piece intersects blade after separation

player holds rock through running blade

piece launches through machine

cut halves overlap

coolant appears from nowhere

saw carriage phases through housing

Fix visually, not just numerically.

35. SAW ASSET QUALITY GATE

The saw must receive a dedicated Blender rebuild/improvement pass.

Check:

housing silhouette

blade size/thickness

blade guard

arbor

carriage

vise/clamp

rails

tray

coolant nozzle

motor housing

controls

feet/base

cable/plumbing details where visible

Do not add random decorative rods.

Every visible part should have a believable function.

36. SAW MATERIAL QUALITY

Use distinct believable materials:

painted/powder-coated metal

bare steel

rubber

plastic controls

diamond blade

wet stone

water/coolant

stained tray

worn work surface

Avoid one generic metal material across everything.

37. SAW ANIMATION / MACHINE MOTION

Moving parts must move around correct pivots.

Examples:

blade spins around arbor

clamp jaw translates/rotates correctly

carriage follows rail

knobs rotate

lever moves

coolant starts from nozzle

cut pieces remain physically plausible

No detached motion.

38. SAW PREP CHANGES BY SPECIMEN

Examples:

ROUND GEODE

cradle/soft jaws

choose stable rotational orientation

ANGULAR NODULE

choose flat support

align bands

LARGE ROCK

use heavy clamp

ensure blade clearance

OVERSIZED ROCK

larger saw or alternate processing

FRAGILE CRYSTAL-RICH ROCK

avoid cutting through exposed pocket

slower feed

better coolant

HIGH-VALUE AGATE

carefully choose display face

possibly mark multiple candidate cuts before committing

39. CUT CHOICE MUST MATTER

A cut can:

hit cavity center

skim cavity

miss cavity

expose best banding

create beautiful symmetrical face

cut through centerpiece

create slab

create rind

reduce value

improve display potential

The player should learn cut strategy.

40. LARGE / OVERSIZED MATERIAL

Preserve meaningful equipment progression.

Possible:

heavy cradle

splitting wedge

geode cracker

larger slab saw

stronger clamp

Avoid arbitrary UI locks when physical limitation can communicate it.

41. THIRD PROCESSING STRATEGY

If V4 is stable, add a geode cracker / soil-pipe-cutter-inspired splitter.

Identity:

controlled natural split

pressure around circumference

lower crystal damage than careless hammering

preserves natural-looking halves

slower setup

limited by size/equipment

less flexible than saw

not guaranteed perfect

42. GEODE CRACKER HANDS-ON LOOP

Player:

place rock

orient

wrap/position chain/wheels

align

tighten

increase pressure

listen/watch stress

re-seat if necessary

split

No one-button automation.

43. POLISHING HANDS-ON LOOP

Polishing should also be physical but concise.

For suitable flat faces:

place face on lap

start machine

apply controlled pressure/movement

maintain contact

observe finish improve

stop when satisfied

Avoid:

holding button for 30 seconds with no judgment.

Different materials can:

polish at different rates

require different care

show different visual payoff

44. POLISHING FEEDBACK

Use:

lap motor sound

contact sound

wet surface

visible scratch reduction

saturation/clarity increase

haptics

subtle resistance

Do not polish natural crystal points.

45. POST-OPEN CLEAN / RINSE

Use this as reveal enhancement, not a chore.

Preferred:
process completes
→ first glimpse
→ quick rinse/clear slurry/dust
→ final beauty view

46. INSPECTION GAME

Inspection uses combinations of:

size

weight

exterior texture

locality

supplier

seam

stain

natural chip

loupe

tap resonance

optional UV/inspection light

known source tendencies

Never reveal exact hidden rolls.

47. TAP IS A CLUE, NOT A SCANNER

Use qualitative feedback:

dense thud

muted response

slight resonance

clear hollow resonance

Rock geometry can mislead it.

48. OPTIONAL PREDICTION / FIELD NOTES

If useful:

likely family

likely hollow/solid

expected quality

chosen processing method

After verification:

compare prediction

track mastery stats

No harsh punishment.

49. VERIFICATION / APPRAISAL

Confirm:

mineral/family

locality

size/weight

cavity type

crystal habit

saturation

clarity

zoning

secondary minerals

rare traits

condition

processing method

processing damage

polish

value

Explain value with visible reasons.

50. HIGH-END VERIFICATION

For exceptional pieces, optional advanced verification may use:

premium light

precision scale

calipers

magnification

UV

No lab simulator.

51. SPECIMEN PROVENANCE

Important pieces retain:

unique ID

source

lot

acquisition

original size/mass

cleaning

inspection/prediction

processing method

cut lineage

damage

polish

appraisal

display/sale history

custom name

52. SPECIMEN CARD / UI

Create premium specimen presentation.

Show:

name

family

source

size

weight

traits

condition

process history

value

custom name

optional 3D preview

Do not hide the specimen behind UI.

53. UI / UX MAJOR POLISH

Review:

Title

New Game / Continue

pause

settings

tablet

suppliers

upgrades

collection

specimen cards

appraisal

hammer HUD

wash

saw

geode cracker

polishing

retail

checkout

special buyers

auction

endgame

Target:
cohesive premium industrial/lapidary simulator.

54. UI PRINCIPLES

Readable.
Restrained.
Controller-friendly.
Scalable.
Consistent.

Avoid:

default Unity feel

giant cards

neon rarity colors

tiny text

center-screen clutter

excessive icons

mobile-game style

55. PROCESSING UI SHOULD BE PHYSICAL-FIRST

For saw/hammer/polish/cracker:
prefer:

physical machine indicators

audio

motion

haptics

tool feedback

over:

giant health bars

progress bars

arcade meters

Use HUD only when physical communication is insufficient.

56. AUDIO / HAPTICS ARE PART OF GAMEPLAY

Treat audio as mechanical information.

Important sounds:

rock handling

tap resonance

brush

water

hammer

chisel

crack

crystal break

saw motor idle

saw load

blade grind

coolant

breakthrough

slab placement

lap motor

polish contact

geode cracker pressure

checkout

rare reveal

Controller haptics should reinforce:

impact

resistance

motor vibration

breakthrough

pressure release

57. MUSIC / AMBIENCE

If music is absent:
add restrained adaptive/generative music.

Keep tool sounds dominant.

Independent slider.

58. COLLECTION / GALLERY

Improve:

lighting

mounts

labels

large specimen displays

slab displays

natural vs polished

browsing

favorites

custom names

best-piece records

Curated, not decorating sandbox.

59. SOURCING

Target roughly:

8–12 meaningful source/locality profiles

Each differs in:

families

sizes

cleanliness

shell character

formations

risk

rarity tail

consistency

price

processing suitability

60. SUPPLIER PROGRESSION

Possible ladder:

local quarry

regional dealer

focused source

estate

premium dealer

oversized supplier

collector network

specialty late-game source

Unlock via meaningful milestones, not generic XP.

61. SPECIAL LOTS

Use occasional:

oversized lot

amethyst-focused

agate nodules

estate mystery

premium show material

locality showcase

damaged-but-promising lot

No FOMO timers.

62. SELLING STRATEGIES

DEALER

instant

lower price

RETAIL

higher price

slower

preferences/capacity matter

SPECIAL BUYER / COMMISSION

targeted demand

premium

AUCTION

only for genuinely high-end material if stable

No dominant channel.

63. NPC / CUSTOMER QUALITY

Preserve V4 navigation quality.

Improve:

body/head/clothing variation

walk

turn

idle

browse

queue

checkout reaction

rare specimen interest

Do not let customer art consume V5.

64. CHECKOUT POLISH

Target:
3–8 seconds normal transaction.

Clear:

customer

specimen

price

payment

ownership transfer

exit

queue advance

Strong sound and motion.

65. WORKSHOP STAGE 3

If Stage 2 is strong, add Stage 3:

established specialist lapidary / specimen dealer

Possible:

heavy processing bay

improved saw

better inspection

premium appraisal

larger curated display

upgraded showroom

improved receiving

Physical changes, not hidden modifiers.

66. CAREER / REPUTATION

Use restrained mastery/reputation to unlock:

suppliers

buyers

Stage 3

special lots

endgame

No generic XP grind.

67. ENDGAME

Recommended:
MASTER COLLECTION / CURATOR EXHIBITION.

Require meaningful mastery across:

sourcing

natural specimen

saw/polished specimen

large specimen

collection

workshop

reputation

Player chooses best pieces.
Present beautifully.
Conclude career.

Continue save afterward.

68. ECONOMY / RARITY SIMULATION

Run large deterministic simulations.

Verify:

rarity frequency

value distribution

source differences

premium supplier balance

jackpot scarcity

first crate safety

upgrade affordability

late-game money use

no softlocks

Do not tune rarity by feel alone.

69. RARITY FATIGUE TEST

Simulate/observe:

100 rocks

500 rocks

1,000+ rocks

Ask:

are exceptional rocks still exciting?

are museum/world-class still memorable?

does premium sourcing help without trivializing rarity?

If top tiers appear too often, reduce them.

70. ANTI-REPETITION

At 10+ hours use:

specimen uniqueness

source differences

size

prep differences

inspection clues

processing strategies

special lots

rare traits

collection goals

buyer demand

equipment progression

Never chores.

71. ONE-MORE-ROCK TEST

After each rock, create curiosity through:

suspicious next rock

new source

saved oversized piece

collection gap

buyer request

near-upgrade goal

unusual surface clue

No manipulative timers.

72. SAVE / MIGRATION

Persist:

V4 pieces

cut lineage

polish

prep state where necessary

predictions

new suppliers

Stage 3

buyers

auction

endgame

custom names

No duplication.
No resurrected parent rocks.
No missing children.

73. CRASH RECOVERY

Test interruption during:

wash

hammer

saw setup

saw mid-cut

cut completion

polish

cracker

appraisal

checkout

auction

endgame selection

Recovery must be deterministic/fair.

74. COLLISION QUALITY GATE

Zero tolerance for frequent visible collisions among:

rock/support

rock/clamp

blade/rock

tool/rock

hammer/chisel

chisel/shell

cut pieces

saw housing

carriage

wash tub

lap

displays

customers

counter

player-held item/camera

Automated audits + visual Play Mode inspection.

75. PERFORMANCE

Visual fidelity must coexist with performance.

Profile:

Stage 3

full displays

high-crystal specimen

saw

water/coolant

polished shaders

customers

UI

audio

Use LOD/detail strategy where helpful.

Preserve the M2 / 8 GB development floor where practical.

76. GRAPHICS ACCEPTANCE GATE

Do not pass the visual phase until:

rocks look materially better than V4

saw no longer looks low-poly/placeholder

hammer/chisel look credible close-up

hero tools have clean geometry

no obvious random protrusions

no broken normals

no floating parts

crystals hold up close

materials are distinct/believable

wet/dry/polished/cut/fracture states read

screenshot-quality rare specimens exist

77. SAW ACCEPTANCE GATE

Do not call saw complete until:

player physically operates it

orientation matters

clamp matters

cut plane matters

blade visibly matches cut plane

resistance varies by specimen

feed rate matters

audio load responds

haptics respond

coolant works

kerf grows visibly

breakthrough feels different

pieces match cut

collisions are clean

saves/lineage work

controller works

repeated rocks do not feel identical

78. PREP VARIATION GATE

Test:

small round geode

medium angular rock

large rough

oversized rough

fragile crystal-rich

banded agate/nodule

dirty estate rock

premium prepared rock

The prep sequence should NOT be identical for all.

79. RARITY ACCEPTANCE GATE

Across simulated careers:

ordinary rocks dominate

Good is welcome

Exceptional is exciting

Museum Grade is memorable

World Class is extremely rare

premium sources improve strategy without showering jackpots

80. UI ACCEPTANCE GATE

Do not pass until:

coherent art direction

commercial appearance

controller focus excellent

1080p/1440p/4K practical checks

specimen remains visible

supplier differences readable

appraisal premium

processing UI restrained

no obvious default Unity widgets

81. CAREER FUN GATE

At 10–15 hours ask:

do rocks still look different?

are rare rocks still rare?

do I inspect differently?

do I prep differently?

do I use multiple tools?

does the saw feel physical?

does hammer remain fun?

does skill matter?

is UI helping rather than cluttering?

do I care about collection?

do I want one more crate?

If several answers are no, iterate.

82. V5 PHASE ORDER

PHASE 0 — V4 regression
PHASE 1 — hero asset / Blender / graphics overhaul
PHASE 2 — specimen visual quality / rarity
PHASE 3 — hands-on rock lifecycle / prep
PHASE 4 — saw interaction finalization
PHASE 5 — hammer / cracker / polishing refinement
PHASE 6 — verification / provenance
PHASE 7 — sourcing / selling / collection
PHASE 8 — Stage 3 / endgame
PHASE 9 — UI / audio / NPC presentation
PHASE 10 — economy / rarity / pacing
PHASE 11 — persistence / controller / standalone
PHASE 12 — full career QA

Do not skip Phase 1.

83. GIT / GITHUB

GitHub persistence required.

At major known-good milestones:

compile

relevant tests

visual check

inspect staged files

commit

push current branch to origin

Suggested commits:

Verify V4 baseline before V5

Upgrade hero assets and specimen rendering

Complete specimen rarity and uniqueness pass

Add specimen-specific preparation workflow

Complete interactive lapidary saw

Refine hands-on processing feedback

Complete verification and provenance

Expand sourcing and selling career

Build Stage 3 and endgame

Complete V5 UI and presentation polish

Balance rarity and career economy

Complete V5 final QA

Never force-push.
Never rewrite published history.
Never commit credentials or Unity caches.

84. RABBIT-HOLE RULE

For low-value cosmetic/tooling problems:
after ~20–30 minutes without convergence:

simplify

choose robust solution

record limitation

continue

Do NOT shortcut:

save corruption

duplication

build failure

core processing

serious visual defects in hero assets

saw mismatch

controller softlock

collision failures

navigation deadlocks

85. COMPUTE DISCIPLINE

Prefer:

primary Fable agent

0–6 focused subagents

targeted review

Do not launch another giant redundant audit.

Spend compute on:

modeling

interaction

observation

testing

visual comparisons

balancing

86. USAGE-WINDOW CONTINUITY

If a usage window ends:

finish current quality gate

test

commit

push

write exact memory/status

record next action

Do not partially start many later phases.

87. CLEAN-START CAREERS

Run:

A. balanced
B. hammer specialist
C. saw specialist
D. collector
E. retailer
F. dealer-heavy
G. careless/bad-luck
H. oversized specialist
I. controller-only
J. save/relaunch interruption

Use simulation for long pacing plus real observed Play Mode.

88. FINAL STANDALONE TEST

Verify in build:

Title

New Game

Continue

first crate

cleaning

inspection/tap/loupe

specimen-specific prep

hammer

saw

oversized

cracker if added

polish

appraisal

retail

checkout

collection

source progression

Stage 2

Stage 3

endgame

controller

settings

save/reload

No build-only shader/serialization failures.

89. V5 V2 DEFINITION OF DONE

V5 V2 is complete only when all are true:

Visual fidelity

hero assets materially improved

saw is close-up credible

tools are close-up credible

rocks/crystals materially improved

no common random protrusions / floating geometry

asset pivots/normals/materials/colliders correct

rare pieces screenshot-worthy

Specimens

same-family uniqueness obvious

stats visibly affect appearance

quality visually legible

top rarity genuinely scarce

roughly 20–28 strong families/archetypes if quality supports it

Rock lifecycle

receive→inspect→prep→process→verify→disposition is coherent

not every rock uses identical prep

no chore overload

Saw

fully hands-on

variable by rock

physical feed

load/resistance feedback

coolant

kerf

cut-plane accuracy

collision-safe

audio/haptics strong

Hammer

tactile

geology dependent

visible cause/effect

viable forever

Other processing

oversized meaningful

polishing physical/contextual

cracker/equivalent third strategy if it improves the game

Verification

appraisal explains visible value

provenance meaningful

optional prediction supports mastery

UI/UX

full polish pass

coherent visual language

physical-first processing feedback

controller strong

resolution scaling practical

Economy

15–25 hour career viable

rarity distribution believable

jackpots not routine

no softlock

late money useful

Collection / sourcing / selling

collection motivates

sources learned

suppliers progress

selling strategies differ

special high-end channel bounded

Stage 3 / endgame

meaningful career conclusion

game continues afterward

Persistence

no duplication

lineage intact

crash recovery fair

Performance/build

development floor healthy

standalone passes

Console clean in normal play

Fun

processing feels physical

rare finds feel rare

skill feels real

stones look desirable

player wants one more rock

90. V5 MUST NOT BECOME

Do NOT add:

multiplayer

co-op

open-world quarry

vehicles

employee management

giant dialogue RPG

factory automation

huge decoration sandbox

live-service timers

battle pass

prestige reset

combat

Not needed for Geode Empire 1.0.

91. FINAL REPORT

Report:

Completed

Actual systems.

Visual fidelity

Assets rebuilt/improved, Blender work, specimen rendering.

Processing

Prep, hammer, saw, oversized, polish, cracker.

Rarity

Distribution and simulation results.

Rock lifecycle

Definitive player flow.

Career

Sources, buyers, Stage 3, endgame.

UI / audio / NPC

Major polish.

Persistence

Lineage/save/recovery.

Verification

Tests, Play Mode, controller, performance, standalone.

GitHub

Branch:
Final commit:
Remote:
Push status:
Working tree:

Known limitations

Only real remaining limitations.

V6 recommendation

Release-candidate/beta:

real-player testing

Steam

compatibility

accessibility

achievements/cloud

localization

optimization

legal/credits

release QA

Do not propose another giant feature expansion unless playtesting proves a fundamental hole.

92. EXECUTE

Execute autonomously.

Do not stop because something technically works.

Do not accept low-quality hero assets.
Do not accept a low-quality saw.
Do not accept repeated rocks.
Do not accept routine high-tier loot.
Do not accept one-button processing.
Do not accept inaccurate visible contact.
Do not accept collisions that undermine the fantasy.

Use Blender seriously.
Look closely at every hero asset.
Play every processing method.
Listen to the machine.
Feel the haptics.
Watch the cut happen.
Inspect the resulting stone.
Run rarity simulations.
Protect the save.
Push known-good milestones to GitHub.

Final product thesis:

Every rock looks like its own physical specimen.

The player reads it.

The player prepares it.

The player chooses the right tool.

The player physically performs the process.

The stone pushes back differently depending on what it is.

Skill changes what survives.

Rare means rare.

Great specimens look great before the UI says so.

The tools and machines feel real, responsive and satisfying.

The workshop becomes the record of the player's expertise.

Then the player wants one more rock.

Continue until the V5 V2 Definition of Done is genuinely satisfied.