GEODE EMPIRE — FABLE 5.1 AUTONOMOUS VERTICAL-SLICE GOAL
Use this file as the /goal directive for Claude Code / Fable 5.1.

This file is an execution mandate, not a brainstorm. CLAUDE.md defines how you work. GEODE_EMPIRE_FINAL_DESIGN.md defines the authoritative product vision. This file defines the concrete autonomous objective to complete now.

1. MISSION
Build a polished, cohesive, genuinely fun 40–95 minute first-play vertical slice of Geode Empire inside the existing Unity project.

The target experience is a 55–75 minute first-time playthrough, with faster players finishing in roughly 40 minutes and slower/exploratory players taking up to roughly 95 minutes.

This must feel like a small commercial game/demo, not a collection of prototypes.

The player must be able to launch the game from a clean start, understand what to do without developer help, process multiple crates of mystery geodes, experience meaningful progression, discover visibly different specimens, make keep-versus-sell decisions, display favorites, save and resume, and finish the slice wanting to open another crate.

The primary emotional sequence is:

What did I buy? → What is inside? → Oh wow. → How much is this worth? → Do I keep this? → I can afford something better → One more crate.

The game is not a spreadsheet business simulator.
The game is not a generic loot-box simulator.
The game is not a rock-themed menu game.

The physical crack, the reveal, the specimen, the collection choice, and the desire to process one more rock are the product.

2. AUTHORITY ORDER
Before changing gameplay, read these files in full:

CLAUDE.md

GEODE_EMPIRE_FINAL_DESIGN.md

this goal file

When instructions conflict, use this priority:

safety / repository boundaries / explicit user constraints in CLAUDE.md

this file’s concrete vertical-slice execution scope

GEODE_EMPIRE_FINAL_DESIGN.md for broader design intent

Do not expand into post-launch features simply because the design document mentions them.

3. AUTONOMY CONTRACT
Work autonomously until the Definition of Done is satisfied or a genuine blocker requires information only the user can provide.

Do not repeatedly stop to ask:

whether to continue,

whether to implement the next obvious system,

whether to fix errors you introduced,

whether to polish something visibly unfinished,

whether to run tests,

whether to make a reversible project-local change,

whether to iterate after a failed acceptance test.

For ordinary implementation decisions:

inspect → decide → implement → run → observe → fix → retest → polish → verify.

Stop only for:

credentials,

purchases,

external publishing,

destructive actions outside the repository,

irreversible product decisions not answered by the design files,

a hard technical blocker that cannot be reasonably solved locally.

Do not declare completion because code exists.
Do not declare completion because a scene opens.
Do not declare completion because unit tests pass.

Completion requires the game to be playable, visually coherent, repeatedly tested in Unity, and enjoyable enough to satisfy the experiential acceptance criteria below.

4. NON-NEGOTIABLE PRIORITY ORDER
Spend effort in this order:

Crystal/geode visual differentiation

Hammer/chisel feel

Fracture/reveal quality

Crate-processing rhythm

Keep-versus-sell tension

First-hour progression

Workshop presentation

Audio/VFX polish

UI/UX polish

Save integrity

Controller/settings

everything else

If time, complexity, memory, or disk pressure forces a tradeoff, preserve the top of this list.

Never sacrifice the reveal in order to add another menu.

5. HARD SCOPE
Build one polished workshop and one self-contained first-hour progression arc.

Must include:

title/start flow

new game / continue

first-person movement

interaction highlighting/prompts

object pickup, inspection, rotation, and deliberate placement

physical crate purchasing and delivery

crate unpacking

8–12 rocks per normal crate

deterministic procedural specimen generation

8–10 visually distinct initial mineral/formation families

hammer/chisel processing

meaningful strike position / force / accumulated stress

persistent visible damage

satisfying final shell separation/reveal

rapid post-open sorting

appraisal

one clear sell channel

physical limited display cabinet/shelf

meaningful keep-versus-sell decision

3–4 supplier strategies

several useful upgrades

mineral encyclopedia

session statistics

tutorial-by-doing

robust autosave

settings needed for comfortable play

keyboard/mouse

controller support for the full slice

performance appropriate for the M2/8 GB development machine

polished final end-of-slice tease

Do not build unless required by the core slice:

multiplayer/co-op

customer-facing shop simulation

open world

vehicles

mine trips

auctions

deep buyer relationship simulation

broad decorating system

prestige

sandbox

networking

visiting collections

dozens of NPCs

large dialogue systems

survival mechanics

complex geographic simulation

six-instrument inspection laboratory

6. TARGET FIRST-PLAY SESSION
Design and tune the first playthrough around this approximate pacing.

These are targets, not rigid cutscene timers. Adjust based on actual playtesting.

Minute 0–5 — Arrival and immediate agency
The player starts in a visually convincing small lapidary workshop.

Within the first 30 seconds, they can look around and move.

Within the first 90 seconds, they understand:

this is their workshop,

they have limited money,

they buy mystery rock crates,

they crack rocks,

interesting finds can be kept or sold.

Avoid exposition dumps.

The workshop should communicate the fantasy environmentally:

battered workbench,

hammer/chisel station,

empty display cabinet,

package receiving area,

scale/appraisal station,

small computer/tablet/order interface,

boxes, shelves, safety equipment, believable workshop clutter.

Starting cash target: approximately $100–$150, subject to tuning.

The first cheap crate should be affordable immediately.

Minute 3–8 — First crate arrives
The player orders a Local Mixed Crate.

The purchase should feel physical:

purchase confirmation,

short delivery transition or immediate believable arrival,

package appears at receiving area,

player opens the crate,

8–10 exterior rocks are visible.

Do not create real-time waiting.

This moment should produce:

“Okay, which one do I open first?”

Minute 5–20 — First processing session
The first crate teaches the signature interaction through doing.

The player:

selects a rock,

physically picks it up,

rotates/inspects it,

places it at the cracking bench,

grabs/uses the chisel,

positions the chisel,

strikes,

receives visual/audio stress feedback,

rotates/repositions,

opens the specimen.

The first specimen may be slightly tutorial-biased so the player gets a satisfying but not absurd result.

Do not make the first rock the best rock in the slice.

The first crate should contain a deliberate-feeling distribution such as:

several ordinary specimens,

one noticeably attractive specimen,

possibly one unusual formation,

enough variance that the player learns outcomes are not cosmetic recolors.

Tutorial prompts should be short and contextual.

After the player demonstrates a mechanic, stop nagging them about it.

Minute 15–25 — First economic decision
The player learns quick sorting.

Ordinary opened specimens should be easy to put into a sell/ordinary workflow without opening a full-screen UI every time.

At least one specimen from the first crate should plausibly create:

“This is much nicer. Maybe I should keep it.”

Introduce appraisal.

The appraisal should clearly explain visible reasons for value:

mineral

size/weight

color

formation

crystal scale/density

damage

unusual trait

estimated value

Avoid forty-stat spreadsheets.

The player should be able to:

sell ordinary material,

keep a favorite,

physically place the favorite into a display slot.

The empty workshop should now contain a visible personal trophy.

Minute 20–35 — First upgrade and second crate
The first crate should generally provide enough money to afford either:

a useful tool/bench upgrade, or

a more ambitious next crate,

but ideally not both without tradeoff.

The first upgrade should alter actual play, not just show “+5% efficiency.”

Possible early upgrades:

better chisel profile with clearer stress control,

improved bench clamp/support,

brighter inspection light,

basic display expansion,

slightly better appraisal confidence.

Unlock or introduce a Regional Curated Crate.

This crate should not simply be “Local crate but numbers higher.”

It should have:

better quality floor,

less junk,

different formation weighting,

somewhat lower extreme variance than speculative lots.

Minute 30–50 — Mastery starts forming
The second crate is where tutorial becomes gameplay.

The player should begin:

selecting promising rocks based on exterior hints,

changing strike strategy,

understanding damage,

recognizing mineral families without needing every label,

processing ordinary outcomes faster.

The UI should get out of the way.

This section must feel rhythmic.

Target tactile/admin ratio:

At least ~70% of active play time should involve the workshop, rocks, tools, movement, inspection, physical sorting, or display—not menus.

Minute 40–60 — First “I have to keep this” moment
By this point, tune the deterministic session distribution so a normal new player has a reasonable chance of finding a specimen that is:

visibly unusual,

substantially more valuable,

noticeably more beautiful than the baseline.

Do not guarantee a fake “legendary jackpot.”

The player should experience authentic procedural variance, but the demo seed/new-player distribution may be curated enough to demonstrate the product.

The specimen should test the core choice:

Sell it and accelerate progression?

Keep it and occupy scarce display space?

Display capacity should already feel finite.

The collection should visually improve the workshop.

Minute 50–70 — Strategic supplier choice
Introduce a third sourcing option with different risk rather than simply higher level.

Example:

Estate / Mystery Lot
expensive relative to current cash,

incomplete information,

high variance,

can disappoint,

meaningfully fatter upside.

The player now chooses among:

cheap volume,

reliable curated material,

risky high-variance material.

That decision should be understandable without a spreadsheet.

Minute 60–80 — Best crate of the slice
The third major crate/session should be the strongest showcase of:

procedural visual variance,

improved player skill,

tactile rhythm,

collection pressure,

reveal quality.

This is where one of the session’s best potential specimens may occur.

The player should now feel noticeably better at cracking than they did on rock #1.

Damage avoidance should feel earned.

At least one reveal should be good enough that it could plausibly become a trailer/TikTok clip.

Minute 70–95 — Slice climax and “one more” tease
The slice should end naturally after the player has:

processed multiple crates,

made money,

kept multiple specimens,

sold many ordinary specimens,

purchased meaningful upgrades,

unlocked multiple supplier strategies,

improved their collection,

seen several distinct mineral families.

Do not hard-stop immediately after the best reveal.

Give a few minutes to admire/appraise/display/sell.

Then tease the broader game through one or two restrained future hooks:

locked precision saw station,

premium supplier invite,

larger display cabinet,

workshop expansion marker.

Do not implement giant future systems merely for the tease.

The final feeling should be:

“I want to buy the next crate.”

7. CORE CRATE RHYTHM
Normal crates contain roughly 8–12 specimens.

A crate should create a mini narrative:

baseline,

variation,

anticipation,

occasional surprise,

decision.

Avoid streaks where every rock feels equally valuable.

Avoid obvious scripted sequences that destroy mystery.

Use deterministic generation plus controlled new-player weighting where needed.

The player should be able to process ordinary specimens rapidly.

Do not force appraisal on every common rock.

8. PROCEDURAL VISUAL VARIETY — BUILD THIS EARLY
Before deep progression, implement tooling to generate and render at least 200 deterministic specimens.

Create a contact-sheet workflow.

The generator must visibly vary at least:

exterior shape

exterior material/weathering

cavity architecture

mineral family

crystal habit

crystal scale

crystal density

crystal orientation/distribution

palette/saturation

secondary growth

rare centerpiece traits

damage state

Internal numeric variation that cannot be perceived does not count.

Target at least 15–20 clearly recognizable visual outcome families across the initial 8–10 minerals/formation archetypes.

If the contact sheet looks repetitive:

STOP adding economy features and improve the specimen generator.

9. INITIAL MINERAL TARGET
Implement approximately 8–10 visually deep families.

Recommended starting vocabulary:

Clear Quartz

Amethyst

Citrine

Smoky Quartz

Agate / Chalcedony

Calcite

Celestite

Fluorite

Pyrite

one strongly different needle/radial family such as Aragonite

Each family should differ in geometry/material language, not just hue.

Examples:

quartz-like faceted points

cubic fluorite

metallic pyrite

banded agate walls

pale-blue clustered celestite

radial/needle growth

If one family does not read differently enough, replace or redesign it.

10. GEODE GENERATION ARCHITECTURE
Use deterministic seeds and persistent specimen IDs.

Separate:

Geological truth
Generated once from seed:

exterior archetype

mass/scale

shell thickness

cavity archetype

mineral family/families

crystal parameters

rare traits

base value factors

Career state
Persisted separately:

unopened/opened

current stress

crack history

damage

pieces/halves

appraisal state

sold/kept

custom name

display transform

discovery timestamp

Reloading never rerolls geology.

11. FRACTURE SYSTEM
Do not attempt unrestricted scientific fracture simulation if it harms reliability.

Build a robust hybrid system that makes the player believe their actions matter.

The player should perceive relationships between:

strike location

strike angle

force

accumulated stress

existing fracture lines

shell thickness

support/clamp state

Requirements:

strikes produce localized feedback,

stress grows over repeated related hits,

poor hit placement can cause damage,

rotating and working around the shell is advantageous,

a strong player can reduce damage,

opening is not simply “N clicks.”

The final split can use controlled fracture states, masks, prepared shell segments, procedural split geometry, or other robust techniques.

Experience > mathematical purity.

12. DAMAGE
Damage must be visible and persistent.

Possible visible results:

broken tips

missing crystals

cracked cluster

chipped formation

shattered region

damaged symmetry

Damage must affect value.

The player must understand:

“I did that.”

Avoid random invisible penalties.

13. ANTI-SAVE-SCUM INTEGRITY
Career specimen processing must commit.

At minimum:

autosave when a specimen enters an active processing state,

commit meaningful impacts/damage,

persist state after each important fracture transition,

do not let ordinary manual reload restore a pristine known specimen after failed processing,

keep crash recovery fair and robust.

Do not create frustrating save behavior, but do preserve consequence.

14. REVEAL QUALITY BAR
The reveal is the hero feature.

Spend disproportionate polish here.

Combine:

convincing shell separation

rock fragments

dust

excellent impact/final crack audio

subtle camera impulse

subtle time emphasis only for exceptional finds

crystal highlight response

lighting/focus that reveals cavity depth

controller haptics where supported

restrained discovery sting

no gaudy loot-chest particles

Common reveal:

quick

satisfying

grounded

Rare reveal:

visually stronger

slightly more breathing room

still grounded

World-class visual direction:

player stops moving the camera for a second because the specimen itself is beautiful.

15. “CLIP TEST” REQUIREMENT
During development create a repeatable way to stage and capture the reveal.

Produce at least one polished representative reveal that would make sense as a 10–20 second marketing clip.

Judge it without context.

Ask:

is the rock readable?

is the action readable?

is the split readable?

does the crystal interior contrast strongly enough?

does it look like a commercial game rather than a Unity prototype?

If not, improve it before calling the core polished.

16. CRYSTAL MATERIAL QUALITY
URP crystal shaders/materials should emphasize visual appeal and performance.

Use combinations of:

facet-readable normals

Fresnel

controlled transparency/translucency approximations

color depth

internal color variation

roughness variation

reflections/specular response

inclusions/noise where useful

subtle sparkle

secondary mineral contrast

Avoid:

flat emissive candy

excessive bloom

transparent sorting artifacts everywhere

glass that looks invisible

materials that only look good from one camera angle

Test under multiple workshop lighting positions.

17. BLENDER ASSET QUALITY
Use headless Blender Python/bpy through:

./Tools/blender.sh

Create reusable procedural/model-generation scripts under:

Tools/Blender/

Use Blender for high-quality reusable source assets such as:

hammer

chisel variants

workbench

clamps

crates

shelves

display cabinet

appraisal scale/station

workshop props

crystal archetype meshes

exterior rock archetypes

packaging

future saw teaser geometry

Assets should have:

intentional proportions

sensible bevels

readable silhouettes

appropriate topology

correct scale

correct pivots/origins

good normals

UVs when needed

simple LOD/collider strategy where useful

consistent art direction

Do not accept ugly primitives merely because they function.

Do not generate gratuitous high-poly assets.

The workshop must look authored.

18. POLISH STANDARD FOR THE WORKSHOP
The workshop should not feel like a graybox by completion.

It needs:

coherent materials

believable work surfaces

warm/cool lighting hierarchy

focused task lighting at cracking bench

strong specimen lighting at display

believable props

restrained clutter

decals/grime/wear where inexpensive

shadows that ground objects

clear navigation

strong focal points

consistent scale

A screenshot of the workshop should look intentional.

Do not fill space with random generated clutter merely to increase detail.

19. FIRST-PERSON FEEL
Movement and interaction must feel immediately competent.

Implement/tune:

smooth mouse look

configurable sensitivity

reasonable acceleration/deceleration

FOV control

comfortable walking speed

interaction range

crosshair/interaction affordance

object highlighting

pickup weight feel

inspect mode

object rotation

deliberate placement/snap where useful

no uncontrollable physics explosions

The player should feel like they are handling valuable physical objects.

20. TOOL FEEL
Hammer/chisel feedback should include:

readable aiming

satisfying windup/impact timing

different impact intensities

small camera/hand response

clear audio layering

fracture feedback

dust/chips

no excessive screen shake

Make force understandable.

If full analog swing simulation becomes awkward, use a controlled input model that still preserves strike intention and skill.

21. APPRAISAL UX
Appraisal should take seconds, not minutes.

Present a concise hierarchy:

specimen name/mineral

standout visual traits

condition/damage

value

new-discovery/record callouts

Example:

Deep Violet Amethyst Cluster

2.6 kg
Large crystals • Strong saturation • Cathedral cavity
Minor edge damage

Estimated value: $486

NEW: Largest Amethyst

The UI should make the result feel like interpretation of what the player sees, not a random price generator.

22. QUICK SELL / SORT FLOW
Do not interrupt every rock with a modal screen.

Support physical or near-physical rapid sorting.

For example:

ordinary sell tray

appraisal tray

keep/display staging area

At the end of a batch, ordinary sell material can be processed together if that improves pacing.

The player should be able to reach for the next rock quickly.

23. DISPLAY SYSTEM
Start with deliberately scarce display capacity, approximately 8–12 premium slots.

Displayed specimens:

physically remain in workshop

retain exact visual identity

can be picked up/repositioned

have optional concise labels

contribute collection prestige

contribute modest passive value/reputation

appear in encyclopedia records

The display cabinet must make excellent specimens look better than they did on the bench.

Use dedicated lighting.

24. KEEP-VS-SELL ECONOMICS
Keeping a specimen should compete with:

buying next crate

buying upgrade

expanding display capacity

Selling a great piece should feel painful.

Keeping everything should not be optimal.

Tune so at least one first-play specimen creates a legitimate dilemma.

25. SUPPLIERS IN THIS SLICE
Implement 3–4 distinct strategies.

Suggested:

A. Local Quarry Mixed
cheap

high volume

common-heavy

occasional surprising outlier

B. Regional Curated
higher floor

moderate price

more reliable

somewhat lower extreme variance

C. Estate Mystery Lot
limited information

high variance

potentially poor

potentially excellent

D. Premium Dealer (late slice teaser/unlock)
expensive

strong quality floor

visually attractive material

lower chance of junk

not necessarily highest upside

Tune with simulation and playtesting.

Do not make “most expensive available” always optimal.

26. FIRST-HOUR ECONOMY TARGETS
Use actual simulations and playthroughs to tune.

Initial rough targets are only starting points:

starting cash: ~$100–150

first crate: ~$60–90

early ordinary specimens: low but useful value

good first-crate specimen: meaningful enough to consider keeping

first useful upgrade: achievable after roughly one crate

regional crate: requires deliberate reinvestment

estate/mystery crate: a meaningful risk

player should experience at least 2–4 purchasing decisions during the slice

Avoid runaway inflation.

Avoid forcing grind.

The player should progress noticeably in one hour.

27. PROGRESSION IN THE SLICE
The slice should support several meaningful improvements.

Examples:

better chisel

improved support/clamp

inspection light

display expansion

appraisal improvement

supplier unlock

Do not implement twenty upgrades.

Every upgrade must either:

change interaction,

create a new decision,

improve presentation,

unlock a supplier/progression opportunity.

28. SAW POLICY
The lapidary saw is not required to prove the core vertical slice.

After the MUST-HAVE loop is polished and all core gates pass, you may add a small functional saw prototype or polished teaser only if it does not compromise the core.

If implemented:

it is slower than hammer/chisel,

precise,

consumes time/operating cost,

useful for slabs/display faces,

does not dominate hammer processing.

Do not let the saw become the reason the crack remains mediocre.

29. ENCYCLOPEDIA
Track useful discovery information:

mineral family

first discovery

number found

best specimen

largest

highest value

rare traits seen

records

collection percentage for known families

Unknown content should preserve mystery.

Make this pleasant to browse but secondary to workshop play.

30. STATISTICS
Track at least:

crates purchased

rocks processed

specimens opened

money spent

money earned

biggest sale

biggest loss

highest-value kept specimen

largest specimen

most damaged specimen

specimens kept

specimens sold

collection value

mineral discoveries

Use stats for retention and testing.

31. SAVE SYSTEM
Build persistence early enough that later systems use it properly.

Persist:

cash

supplier progression

upgrades

active crates/orders

unopened specimen IDs/seeds

opened specimen IDs/seeds

damage

processing state

appraisals

sold/kept state

display transforms

custom names

encyclopedia

stats

settings

Use:

autosave

backup/recovery save if practical

atomic write/replace pattern

schema/version field

Test:

quit/relaunch

resume mid-progression

kept specimen persistence

damage persistence

deterministic interior persistence

32. TUTORIAL
Teach by action.

Avoid:

long text panels

tutorial videos

ten controls displayed at once

Use contextual prompts.

By the second crate, most tutorial text should be gone.

33. UI ART DIRECTION
Do not ship default Unity-looking UI.

Create a cohesive interface inspired by:

workshop labels

specimen cards

clean utilitarian industrial software

restrained mineral accents

Requirements:

consistent typography

clear hierarchy

spacing discipline

polished hover/focus states

controller navigation

readable at Steam Deck scale

no excessive panels

no tiny text

Menus should feel part of the game world where reasonable, but usability wins over forced diegetic UI.

34. AUDIO
Implement a proper audio architecture.

Even if final bespoke assets are unavailable, create layers/placeholders that can later be replaced.

Critical layers:

hammer swing

hammer/chisel contact

hard/soft impact variation

subtle pre-fracture ticks

stress creaks

final crack transient

rock fragments

dust/debris

crate interaction

object placement

crystal handling

sell/purchase

rare discovery sting

workshop ambience

The final crack must not sound like a generic stock “stone hit.”

35. VFX
Use restrained, grounded VFX:

dust

small chips

crack lines

tiny fragments

subtle crystal glints

reveal highlight

ambient dust motes where inexpensive

Avoid:

confetti

neon rarity beams

giant screen flashes

excessive particles

constant sparkle noise

36. CONTROLLER
The entire required slice must be playable with controller.

Support:

movement/look

interact

pickup/drop

inspect/rotate

chisel positioning

strike

UI navigation

pause/settings

Use controller-friendly prompts.

Do not defer controller until the very end if interaction architecture would need redesign.

37. SETTINGS
At minimum:

mouse sensitivity

controller sensitivity

invert Y

FOV

camera shake intensity

master volume

SFX

music

ambience

resolution/window mode where applicable

VSync

frame-rate limit

graphics quality preset

Ensure settings persist.

38. PERFORMANCE BUDGET
The development machine is an M2 Mac with 8 GB RAM.

Prioritize stable iteration.

Do not leave Blender GUI running.

Blender should run headlessly and exit.

Avoid:

massive texture sets

4K textures by default

extreme mesh density

huge lightmaps

unbounded procedural instances

excessive transparent layers

leaking RenderTextures

per-frame allocations in hot loops

expensive full-scene physics for decorative objects

Prefer:

512–2048 textures depending importance

material reuse

mesh instancing

pooled particles

efficient colliders

LODs where materially helpful

baked/static lighting where appropriate

selective realtime lights

Periodically check memory, frame pacing, and free disk space.

If available disk space approaches 15 GiB, stop asset/build generation and report the issue instead of exhausting disk.

39. GIT DISCIPLINE
Make recoverable commits at meaningful known-good milestones.

Suggested checkpoints:

architecture + clean boot

first-person + interaction

specimen generator/contact-sheet tooling

crack prototype

polished reveal

crate loop

appraisal/sell/display

economy/progression

save/load

UI/audio/VFX polish

controller/settings

final QA

Do not commit broken compilation if avoidable.

Never commit generated Unity Library/Temp/Logs/UserSettings.

Do not perform destructive Git history operations.

40. DEVELOPMENT ORDER
Follow roughly this order unless evidence justifies a change.

Phase 0 — Audit
inspect repo

inspect scene

inspect packages

inspect existing assets/scripts

run baseline

verify Console

create plan internally

Phase 1 — Visual specimen R&D
mineral data model

crystal archetypes

exterior/cavity archetypes

procedural generation

crystal materials

200-specimen rendering/contact sheet

iterate until visual differentiation is convincing

Phase 2 — Hero crack
bench

rock placement

chisel

strike

stress

fracture

damage

reveal

audio/VFX

iterate repeatedly

Phase 3 — Batch rhythm
crate purchase

delivery

unpack

8–12 specimens

rapid next-rock flow

quick sorting

Phase 4 — Value and collection
appraisal

sale

keep

physical display

limited display capacity

collection benefits

Phase 5 — First-hour progression
suppliers

upgrades

economy tuning

tutorial

pacing

Phase 6 — Persistence
save/load

specimen integrity

anti-save-scum state

settings persistence

Phase 7 — Presentation
workshop asset polish

UI

audio

VFX

animation

lighting

Phase 8 — Input/release fundamentals
controller

settings

performance

buildability

Phase 9 — QA
clean-start playthroughs

economy simulation

error fixing

softlock testing

pacing iteration

visual inspection

final report

41. SELF-VERIFICATION LOOP
After each significant phase:

let Unity compile,

inspect Console,

fix errors,

enter Play Mode,

perform the mechanic,

inspect visual result,

inspect interaction feel,

inspect logs,

correct issues,

repeat.

Do not substitute code review for playing the game.

Use screenshots or captured views when helpful.

If independent subagents/verifiers are available, use fresh-context reviewers for:

gameplay usability

visual quality

economy/pacing

save integrity

code architecture

UI/controller accessibility

Do not blindly accept reviewer suggestions. Resolve them against the product thesis.

42. FUN / RETENTION PRINCIPLES
The game should be compelling because of:

curiosity

skill improvement

visual discovery

collection attachment

progression

personal stories

Not because of:

manipulative timers

FOMO

real-money randomness

infinite notification loops

deliberately frustrating scarcity

“Addictive” here means:

the player voluntarily wants one more crate because the core activity is satisfying and the outcomes remain surprising.

43. MOMENT-TO-MOMENT FUN CHECKLIST
During playtesting, ask:

Before opening
do I care what might be inside?

can I make any educated guess?

do I enjoy picking which rock comes next?

During cracking
does position matter?

does force matter?

am I reading the rock?

do impacts feel/sound satisfying?

am I improving?

At reveal
can I immediately see what is different?

is there enough contrast between shell and crystal?

do I want to rotate/inspect it?

would I screenshot a great one?

After reveal
can I move on quickly if it is ordinary?

does appraisal explain visible quality?

does keep/sell create tension?

is the next rock easy to start?

After crate
did the crate feel like a little story?

do I have a favorite?

did I make progress?

do I want another crate?

44. POLISH PASS CHECKLIST
Before calling the slice done, inspect every player-visible system for:

placeholder text

developer labels

debug objects

ugly primitives

missing materials

clipping

floating props

bad pivots

z-fighting

broken normals

inconsistent scale

poor audio levels

harsh transitions

missing focus states

controller dead ends

tutorial spam

excessive camera shake

unreadable UI

low-contrast prompts

repeated specimens

long pauses

dead time

menu friction

softlocks

save failures

Console warnings/errors

Fix obvious problems.

45. CLEAN-START PLAYTEST REQUIREMENT
Perform multiple clean-start runs.

At minimum verify:

Run A — Normal first-time player
Follow prompts, play naturally, keep one attractive find.

Run B — Sell-heavy player
Sell nearly everything and test economy/progression.

Run C — Collector-heavy player
Keep several good specimens and test liquidity/display pressure.

Run D — Careless cracker
Use poor strike strategy and verify damage/consequences.

Run E — Skilled cracker
Use careful strategy and verify improved preservation.

Run F — Save/relaunch
Quit at meaningful checkpoints and resume.

Run G — Controller
Play required loop without keyboard/mouse dependence.

Fix issues found.

46. ECONOMY SIMULATION
Create lightweight simulation/test tooling for crate expected values and progression.

Simulate many crates to inspect:

average return

median return

variance

bad-tail outcomes

good-tail outcomes

extreme outliers

supplier differentiation

time to first upgrade

time to supplier unlock

likelihood of soft-locking financially

The player must not be able to permanently brick a normal first playthrough through ordinary bad luck.

Mystery remains meaningful, but tutorial/early economy should contain safeguards.

47. VISUAL VARIETY TEST
Generate the 200-specimen contact sheet.

Inspect for:

repeated silhouettes

repeated cavity layouts

hue-only variation

crystals intersecting badly

unreadable mineral families

ugly procedural noise

performance outliers

Iterate until the system looks like a collection of distinct specimens rather than recolored procedural clutter.

48. REVEAL TEST
Stage at least:

common reveal

attractive reveal

rare reveal

damaged reveal

All should be satisfying.

Rare should be more memorable without looking like a fantasy chest opening.

49. FAILURE STATES
Handle gracefully:

trying to buy without enough cash

full display

dropping specimen awkwardly

leaving active processing

quitting mid-crack

save interruption

missing optional asset

controller disconnect

opening settings during interaction

specimen stuck/out of bounds

Prefer recovery over forcing restart.

50. ASSET FALLBACK POLICY
If no external production asset is available:

generate a good asset through Blender Python,

create a polished procedural Unity asset/material,

use a high-quality temporary asset with clear replacement architecture.

Do not block the entire game waiting for perfect bespoke art.

But do not call obviously rough placeholders “final polish.”

51. EXTERNAL CONTENT / LICENSING
Do not purchase assets or services without user approval.

Do not import copyrighted/unlicensed content from random sources.

Prefer:

original procedural Blender assets

project-generated textures/materials

Unity-provided licensed resources already available to the project

clearly permissive assets only when licensing is verified

Keep attribution/licensing information if any external permissive asset is intentionally used.

52. REPOSITORY BOUNDARY
Do not intentionally modify files outside:

/Users/kenneth/Documents/GitHub/Geode

except normal tool caches/configuration strictly required by already configured Unity/Blender/Claude tools.

Never delete user documents, messages, photos, unrelated projects, system files, or external repositories.

53. DISK / MEMORY SAFETY
This machine has limited resources.

Periodically run/check:

free disk

Unity memory behavior

runaway Blender outputs

generated contact-sheet assets

temporary builds

Generated diagnostic/contact-sheet artifacts may be cleaned or ignored after useful results are captured.

Preserve source generators and intentional production assets.

Do not create tens of gigabytes of redundant outputs.

54. WHAT “SUPER POLISHED” MEANS
It does not mean maximum polygon count.

It means:

coherent art direction

deliberate composition

satisfying animation/timing

strong materials

excellent lighting

clean UI

responsive controls

high-quality sound layering

consistent visual language

no obvious prototype residue

meaningful detail where the camera actually sees it

beautiful hero objects

disciplined performance

Spend detail on:

geodes/crystals

hands/tools/bench

reveal

display cabinet

crate opening

appraisal presentation

Background clutter receives lower priority.

55. WHAT “FUN” MEANS
A polished but boring simulator fails.

The slice should create:

curiosity before each rock

tactile satisfaction during processing

anticipation as stress builds

surprise at reveal

pride in skilled preservation

attachment to favorites

pain in selling favorites

excitement at better suppliers

desire for another crate

If a system does not strengthen one of these, question it.

56. WHAT “READY” MEANS
Do not finish with:

“The architecture is ready for future implementation.”

Do not finish with:

“The systems are stubbed.”

Do not finish with:

“Here are the next steps.”

Finish only when the vertical slice itself is actually playable.

57. REQUIRED DEFINITION OF DONE
Do not declare the goal complete until all of the following are true.

Boot / onboarding
clean project compiles

no blocking Console errors

game starts from proper title/start flow

new game works

continue works after save exists

player reaches active gameplay without developer intervention

Workshop
workshop is visually coherent and polished

navigation is clear

critical stations are visually readable

assets do not look like a primitive graybox

lighting showcases rocks/crystals

Interaction
movement feels competent

pickup works

inspect/rotate works

placement works

interaction prompts are clean

physics does not routinely explode

Crates
player can buy a crate

crate physically arrives

player can open/unpack it

normal crate contains multiple rocks

multiple crates can be processed in one session

Procedural specimens
deterministic specimen IDs/seeds work

8–10 visually differentiated families exist

200-specimen contact-sheet tooling works

visual differentiation passes inspection

rare traits can create visibly unusual outcomes

Cracking
chisel positioning works

strikes work

force/location meaningfully affect stress

repeated work around the rock matters

final opening is not a fixed-N-click illusion

damage can occur

skill can reduce damage

Reveal
shell separation is readable

interior is immediately visible

common reveal feels satisfying

attractive/rare reveal feels significantly better

audio/VFX are polished enough for a demo

at least one representative reveal is marketing-clip quality

Flow
ordinary specimens can be processed/sorted quickly

player is not forced into long menus after every rock

crate session feels rhythmic

tactile/admin balance is healthy

Appraisal / economy
appraisal works

visible traits affect value

damage affects value

selling works

cash updates correctly

player cannot trivially duplicate/sell specimens twice

early economy avoids normal-play softlocks

Collection
keep works

display works physically

display capacity is limited

displayed specimen persists

displayed specimen looks attractive

keep-versus-sell can create a real tradeoff

Progression
at least 3 supplier strategies are usable/unlocked across the slice

several meaningful upgrades exist

player makes multiple progression choices

progression is noticeable in 40–95 minutes

more expensive source is not always strictly optimal

Persistence
autosave works

reload does not reroll interiors

damage persists

collection persists

cash/progression persists

processing state cannot be trivially save-scummed

backup/recovery strategy exists where practical

Encyclopedia / stats
discoveries record correctly

records update

session/lifetime stats update correctly

Controls / settings
keyboard/mouse can complete required loop

controller can complete required loop

key comfort settings exist and persist

no critical controller navigation dead ends

Performance
game remains workable on the M2/8 GB development machine

no obvious runaway memory behavior

no huge frame-time spikes in ordinary core loop

no catastrophic shader/material issues

disk usage remains safe

QA
multiple clean-start playthroughs completed

save/relaunch tested

careless and careful processing tested

sell-heavy and keep-heavy paths tested

major softlocks fixed

obvious visual bugs fixed

ordinary gameplay leaves no unresolved Console errors

Experience
first-time session naturally lasts approximately 40–95 minutes depending pace

intended target is roughly 55–75 minutes

player experiences multiple crates

player experiences at least one meaningful keep/sell dilemma

player feels progression

player finishes with a visible collection

player is teased with future progression

the strongest outcome is visually memorable

the overall experience feels like one cohesive game

58. FINAL FRESH-PLAYER PASS
When you believe the slice is done:

make a Git checkpoint,

start from a fresh/new save,

play the full intended path as if you have never seen the game,

do not use developer shortcuts,

note friction, dead time, confusion, repetition, ugly assets, poor audio, economy problems, and bugs,

fix the meaningful problems,

repeat until no critical issue remains.

Do not merely perform a scripted automated test and call that a fresh-player pass.

59. FINAL REPORT
Only after the Definition of Done is satisfied, return a concise final report containing:

Completed
What is actually implemented.

How to play
How to launch and the controls.

First-play progression
What a new player experiences across the 40–95 minute slice.

Visual/content systems
Mineral families, specimen variety, Blender-generated assets, workshop polish.

Gameplay systems
Cracking, damage, reveal, crates, appraisal, collection, suppliers, progression.

Verification
Playthroughs, tests, contact-sheet result, Console state, save/load tests, controller test.

Known limitations
Only genuine remaining limitations.

Next best milestone
One tightly scoped next milestone after the slice—not a giant wishlist.

60. EXECUTION COMMAND
Begin now.

Do not spend the first response explaining what you plan to do at length.

Briefly inspect the repository and authoritative design files, establish the current baseline, and start implementing.

Continue autonomously through implementation, testing, debugging, visual iteration, economy tuning, asset generation, and final QA.

Do not stop after making the prototype work. Make the first 40–95 minutes feel like a polished, cohesive, highly compelling commercial demo.

Protect the core: crack → reveal → evaluate → keep/sell → progress → one more crate.

