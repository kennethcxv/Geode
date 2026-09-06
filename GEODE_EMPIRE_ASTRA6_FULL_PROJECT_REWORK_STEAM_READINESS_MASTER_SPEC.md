GEODE EMPIRE — GPT-6 ASTRA FULL PROJECT REWORK, ART DIRECTION, STEAM-READINESS & PRODUCTION MASTER SPEC

Authoritative end-to-end rebuild specification for Geode Empire

Model: GPT-6 Astra, high/max reasoning

Primary acceptance rule: The actual playable game is the acceptance criterion.

Core project rule: Prior claims such as “V4 complete,” “V5 complete,” “V6 complete,” “100% complete,” or “production ready” are historical claims only. They are not proof. Every major system, visual target, gameplay loop, and quality gate must be re-verified against the current repository, live Unity project, Blender assets, Play Mode behavior, fresh-save progression, controller/KBM support, performance, persistence, and standalone builds.

Quality target: A coherent, visually attractive, mechanically satisfying, commercial first-person rock-shop / workshop simulator that is credible to show on Steam. The target is not “functional prototype with lots of features.” The target is a game that looks, sounds, feels, and behaves like a polished simulator product.

Available production tools: Unity MCP (KitWright), Blender MCP, repository shell/CLI, Tools/blender.sh, Codex image generation, Git, automated tests, screenshots, profiling, standalone builds, controller/KBM input, deterministic asset-generation tooling.

Art-production philosophy: Use GPT-6 Astra’s image-generation + Blender + Unity vision loop aggressively:

generate target concept → model/rebuild in Blender → texture/material pass → export → inspect in Unity → capture screenshot → compare against concept → iterate until close → profile → keep only if performance remains acceptable.

0. PURPOSE OF THIS MASTER SPEC

Geode Empire has undergone many passes, milestone documents, and “completion” claims. Some systems are genuinely strong. Others are partially implemented, visually weak, mechanically awkward, inconsistent with the intended design, or only “complete” according to tests/documents rather than actual player experience.

This specification instructs Astra to take ownership of the entire project and perform a truth-based rework.

The goals are:

determine what is actually complete;

determine what is merely functional;

determine what is broken, ugly, awkward, missing, or prototype-quality;

establish an original target art direction using image generation;

redesign the shop and its architecture;

redesign every visible player-facing asset in Blender;

redesign the rocks/geodes as hero content;

rebuild the most important interactions;

rebuild weak UI;

rebuild weak audio, ambience, VFX, and animation;

improve progression, customer flow, business operation, shop opening/closing, inventory, equipment purchase/placement, and expansion;

keep the result performant;

verify all historical promises;

leave the project in a coherent state that is genuinely close to Steam-ready.

1. NON-NEGOTIABLE EXECUTION CONTRACT

This is a project-wide production rework.

Do NOT treat this as:

a narrow bug fix;

a “finish the remaining V6 tasks” pass;

a checklist-closing exercise;

a documentation pass;

a code-only refactor;

a simple visual reskin;

a one-shot environment makeover;

a single screenshot beautification pass.

You must operate as if taking over the project as lead engineer + technical artist + gameplay designer + QA owner.

You are expected to use:

live Unity observation;

live Blender scene inspection;

Blender asset production;

Codex image generation;

generated texture/reference workflows;

performance profiling;

gameplay testing;

controller testing;

KBM testing;

customer stress testing;

persistence testing;

screenshot comparison;

deterministic contact sheets;

standalone builds;

Git milestones.

Do not stop because the run becomes long.

Do not lower the bar to finish quickly.

2. CURRENT VERIFIED INTEGRITY BASELINE — PRESERVE

A targeted Unity integrity gate was completed immediately before this pass.

Treat the following as known-good technical baseline unless later testing proves a real regression.

2.1 DeliveryCrate integrity — PASS

A real serialized script-reference defect was found.

The three authored Delivery objects under:

Stations/FixtureDelivery

previously referenced an embedded/scene-local MonoScript instead of a proper imported script asset.

The repair:

moved DeliveryCrate into its own script file;

rebound all three existing authored components through Unity serialization APIs;

preserved existing GameObject/component file IDs;

verified before/during/after Play;

verified after a full Editor restart.

Do not undo this repair.

2.2 Broken-half collider integrity — PASS

The fractured geode half collision path was repaired.

The previous high-detail visual geometry exceeded Unity/PhysX convex limits and could trigger partial-hull fallback.

The current corrected path:

uses bounded simplified collision meshes;

keeps visual fracture geometry independent from collision geometry;

clears the MeshFilter-assigned visual mesh before convex cooking;

keeps opened halves separated using actual collider extents;

passed targeted regression and runtime validation.

Verified:

18/18 targeted integrity/preparation tests;

256 representative half meshes cooked without the production warning;

crack/reveal;

pickup;

placement;

settling;

no overlap above tolerance;

no meaningful measured performance regression.

Do not revert to detailed visual shells for convex colliders.

2.3 Toolchain — PASS

Verified:

Unity MCP: PASS

Blender MCP: PASS

Blender CLI via Tools/blender.sh: PASS

Unity compilation: clean

project ready for Astra production work: YES

2.4 Player-data safety rule

A prior automation mistake briefly entered Play Mode before the intended save-isolation setup had successfully activated.

The current career was restored byte-for-byte, but an older rolling backup was rotated and could not be recovered.

Therefore, from this point forward:

BEFORE any automated Play Mode operation that can mutate player data:

checksum all current player save/settings/backup files;

create a temporary isolated save directory;

copy necessary test inputs into that directory;

set SaveSystem.DirectoryOverride;

verify the active override path exactly;

if the setup code fails to compile, times out, or returns the wrong path, DO NOT ENTER PLAY MODE;

run the Play Mode test;

exit Play Mode;

restore DirectoryOverride;

verify original player-data checksums;

do not rotate, overwrite, rename, or delete real player backup files during automated QA.

No exceptions.

3. HISTORICAL DOCUMENTS — REQUIREMENTS, NOT PROOF

Read the important historical documents and milestone reports.

At minimum:

CLAUDE.md

AGENTS.md

GEODE_EMPIRE_FINAL_DESIGN.md

GEODE_EMPIRE_FABLE_GOAL.md

V4 specifications/reports

V5 specifications/reports

GEODE_EMPIRE_V6_PRODUCTION_ALPHA.md

checkout handoff/spec

visual rebuild specs

reference-match specs

starter shop / progression / UI rebuild specs

hands-on core loop / expansion / economy specs

current Docs/ plans/reports

relevant Git history

current tests

Use them to answer:

What did prior agents promise?

What systems were supposed to exist?

What quality bar was claimed?

What screenshots/tests were used as evidence?

Which milestones were declared complete?

Which promises remain visible in current code/game?

Which claims have degraded due to later changes?

Never treat the old completion declaration as acceptance.

4. PROJECT TRUTH MATRIX — REQUIRED

Create:

Docs/AstraRework/PROJECT_TRUTH_MATRIX.md

For every significant feature or quality area, record:

Area

Prior claim

Current implementation

Play Mode reality

Visual quality

Interaction quality

Persistence

Controller

KBM

Tests

Performance

Steam-ready?

Required action

Allowed result states:

VERIFIED

PARTIAL

BROKEN

MISSING

PROTOTYPE

NEEDS REWORK

DEFERRED WITH EXPLICIT JUSTIFICATION

Do not mark VERIFIED because:

code exists;

a unit test passes;

a report says “done”;

a placeholder object technically functions;

the feature exists behind debug controls;

the UI has a button.

Verify the player-facing result.

5. WHOLE-GAME AUDIT SCOPE

Audit the entire game.

5.1 Core specimen loop

Audit:

crate purchasing;

crate delivery;

crate opening;

rock pickup;

carrying;

dropping;

physics;

manual washing;

spatial dirt;

inspection;

magnifier;

true multi-axis manipulation;

clues;

prediction;

hammer/chisel;

correct strike feedback;

incorrect strike feedback;

cracker;

diamond saw;

lap/polish;

reveal;

appraisal;

collection;

dealer selling;

customer selling;

checkout.

5.2 Business systems

Audit:

Day-1 state;

starter equipment;

equipment progression;

unlocking;

purchasing;

delivery;

placement;

build mode;

shop expansion;

leases;

rent;

utilities;

bills;

open/close business state;

inventory;

storage;

receiving capacity;

sourcing;

customer traffic;

reputation/progression;

economy;

operating costs;

profitability.

5.3 World / architecture

Audit:

exterior storefront;

customer entrance;

customer exit;

windows/doors;

business sign;

OPEN/CLOSED indication;

retail floor;

checkout;

processing;

receiving;

storage;

private collection;

office/management;

laptop/workstation;

aisles;

sightlines;

floor;

ceiling;

walls;

thresholds;

signs;

shelving;

furniture;

props;

materials;

lighting;

plumbing;

electrical;

colliders;

NavMesh;

player routes;

customer routes;

machine clearance;

placement bounds.

5.4 UI

Audit:

HUD;

interaction prompts;

tutorial;

notification queue;

discovery;

rare/record presentation;

Suppliers;

Upgrades;

Collection;

Business;

Stats;

Inventory;

Premises;

Bills;

Career;

Settings;

Rebinding;

checkout UI.

5.5 Production quality

Audit:

geode exteriors;

geode interiors;

rind;

matrix;

cavity geometry;

crystal morphology;

cut faces;

wet state;

display lighting;

all workstations;

all furniture;

all props;

customers/NPCs;

animations;

ambient audio;

SFX;

VFX;

performance;

saves;

migration;

controller;

KBM;

standalone.

6. STEAM-READINESS RESEARCH

Research current successful first-person shop/workshop simulator games and the quality standards expected by Steam players.

Create:

Docs/AstraRework/STEAM_SIMULATOR_BENCHMARKS.md

Study patterns such as:

storefront readability;

shop entrance;

open/close state;

customer arrival;

aisle width;

checkout location;

management workstation;

customer browsing;

workstation zoning;

progression from tiny shop to mature business;

visual density;

lighting;

environmental storytelling;

prop density;

UI readability;

interaction feedback;

sound design;

frame pacing;

tutorial restraint;

upgrade presentation.

Do NOT copy:

another game's exact layout;

assets;

UI;

branding;

characters;

proprietary designs.

Extract principles and create an original Geode Empire solution.

7. IMAGE-GENERATED ART DIRECTION — REQUIRED

Before broadly rebuilding the world, use the Codex image-generation skill/tooling to generate original target concept images for Geode Empire.

If the image-generation skill/tool instructions are not already loaded:

discover them;

read them;

use them correctly.

Do not rely solely on verbal art direction.

8. CONCEPT ART SET — REQUIRED

Generate several concept variants for:

Day-1 starter shop/workshop

exterior storefront

customer entrance

early retail floor

midgame expanded shop

mature late-game mineral/geode showroom

processing workshop

hammer/chisel cracking area

inspection station

wash station

cracker station

diamond saw station

lap/polish station

receiving/storage

checkout

management laptop/workstation

private collection/gallery

geode display shelving

tablet UI

Suppliers UI

Upgrades UI

Collection UI

Business UI

Stats UI

Premises/Bills UI

Generate contact sheets where useful.

9. TARGET ART DIRECTION

The selected target should feel like:

a modern commercial simulator;

believable in human scale;

physically grounded;

slightly stylized for readability;

warm but not monochrome;

authentic mineral/rock-shop character;

authentic workshop infrastructure;

attractive customer-facing retail;

visually impressive geodes;

practical lighting;

clear progression from humble to mature.

Use material variety:

wood

concrete

plaster/drywall

painted metal

stainless steel

glass

rubber

ceramic

plastic

cardboard

fabric

stone

mineral/crystal surfaces

Avoid:

all-brown rooms;

all-orange lighting;

giant empty warehouse spaces;

fantasy crystal palace;

neon arcade look;

sterile laboratory look;

cheap mobile-game proportions;

repeated prefab staging;

over-rendered concept art that cannot run in real-time.

Day 1 must remain modest.

Late game may become visually impressive.

10. SELECT A COHERENT VISUAL SYSTEM

Critique generated concepts.

Choose a direction that supports:

movement;

customers;

workstations;

player placement;

future expansions;

visual clarity;

performance.

Create:

Docs/AstraRework/ART_DIRECTION.md

Document:

architecture;

palette;

lighting;

materials;

signage;

prop language;

workstation language;

retail language;

UI language;

geode presentation;

Day-1 → early → mid → late visual evolution.

11. REQUIRED CONCEPT-TO-UNITY LOOP

For every major asset/space:

generated concept / Geode reference
→ current Unity screenshot
→ written critique
→ Blender inspection
→ Blender rebuild
→ Blender render
→ self-critique
→ Unity import
→ Play Mode screenshot
→ concept comparison
→ performance check
→ accept or iterate again

Never stop at:
“looks good in Blender.”

Unity Play Mode is final authority.

12. PERFORMANCE — 60 FPS IS A DESIGN CONSTRAINT

The target visual quality must remain performant.

Goal:

smooth 60 FPS in the intended Steam configuration;

stable frame pacing;

no severe interaction spikes;

no large GC hitches;

no major scene-load stalls caused by avoidable asset design.

Use the current Mac as a constrained development test environment, but do not automatically define it as final minimum hardware.

Create:

Docs/AstraRework/PERFORMANCE_BUDGET.md

Define budgets for:

triangles;

hero assets;

repeated props;

draw calls;

materials;

realtime lights;

shadowed lights;

transparent surfaces;

geode crystal counts;

shelf specimen counts;

NPC counts;

colliders;

VFX;

texture memory;

CPU generation time.

Profile standalone, not just Editor.

13. REDESIGN THE ENTIRE SHOP — REQUIRED

The current shop should be treated as architecturally unacceptable until re-proven.

Redesign it from first principles.

The game must actually look and function like a shop.

14. STOREFRONT / ENTRANCE

Implement or redesign:

storefront identity;

customer entrance;

customer exit;

front door;

window treatment where appropriate;

signage;

believable exterior or vestibule if useful;

open/closed indicator;

arrival point;

visual connection between entrance and retail floor.

Customers should not appear to teleport into a generic workshop.

15. OPEN / CLOSE BUSINESS SYSTEM

Add or finish a clear OPEN/CLOSED loop.

Player should be able to:

open shop;

communicate OPEN state;

start customer traffic;

stop new customer entry;

finish current customers;

close business.

Use:

sign;

door;

lighting;

audio cue;

UI cue;

where appropriate.

Do not make opening/closing tedious.

16. SHOP ZONING

Create coherent zones.

Customer-facing

entrance

retail displays

browsing aisles

checkout

premium showcase later

Processing

cracking

inspection

washing

cracker

saw

lap

Operations

receiving

storage

packaging

laptop/management

Progression

future lease/expansion areas

collection/gallery later

Do not place objects merely because there is floor space.

17. FIX CURRENT POSITIONING PROBLEMS

Explicitly fix:

rock crate/box sitting inches from cracking bench;

workstation crowding;

washing station facing the wall;

cleaning interaction oriented away from player;

awkward station cameras;

props clipping walls;

signs clipping walls;

shelves with no useful route;

checkout in an implausible location;

random dead space;

poor aisle widths;

objects with no interaction clearance.

Use real-world dimensions.

18. DAY-1 SHOP — EXTREMELY MINIMAL

At fresh start, player should have only the smallest essential business kit.

Primary installed Day-1 pieces:

checkout

basic hammer/chisel cracking station

initial crate/receiving capability

minimal management access

A simple starter laptop/terminal may be part of the essential management/checkout kit so inventory/orders are accessible.

Everything else should be acquired and placed later unless a basic handheld tool is absolutely necessary for the opening loop.

Do NOT preinstall:

wash station

premium inspection station

cracker

saw

lap

mature shelving

premium displays

collection gallery

advanced storage

large office

mature showroom

If early inspection/cleaning is required before stations are purchased, use minimal handheld/manual tools.

19. EQUIPMENT PROGRESSION

Preferred flow:

unlock
→ purchase
→ delivery
→ unpack
→ build mode
→ player placement
→ validation
→ activation
→ persistence

The player should feel:

“I built this place.”

20. SHOP EXPANSION

Preserve existing lease/expansion foundations but redesign them to fit the final shop architecture.

Expansion should:

open real space;

change walls/doors/hoarding;

change customer routes;

increase usable placement area;

increase rent;

increase utilities where appropriate;

unlock meaningful business capability.

Do not reduce expansion to a menu number.

21. RENT / UTILITIES / BILLS

Preserve and re-verify:

rent;

electricity;

water;

bills;

due dates;

readable cost breakdown;

recoverable late-payment path.

Rebalance after final shop/equipment progression exists.

22. MANAGEMENT LAPTOP / WORKSTATION

Add or redesign a believable physical laptop or management workstation.

It should support:

inventory;

suppliers;

upgrades;

business;

stats;

premises;

bills;

collection where appropriate.

Requirements:

physical asset;

believable dimensions;

readable screen;

correct placement;

no clipping;

controller + KBM;

final UI design language.

Use Blender.

23. EVERY PLAYER-FACING ASSET MUST BE REDESIGNED OR MATERIALY RE-AUTHORED

Audit every visible player-facing production asset.

For this pass, default assumption:

Every visible player-facing asset should be materially redesigned/re-authored to fit the Astra target art direction.

Do not retain an asset simply because it is technically functional.

An asset may remain substantially unchanged only if Astra explicitly proves:

it already matches the chosen concept direction;

it survives first-person close inspection;

it is visually coherent with the rebuilt set;

its materials/geometry/colliders are production quality.

Otherwise rebuild it.

24. ASSET REWORK MANIFEST

Create:

Docs/AstraRework/ASSET_REWORK_MANIFEST.md

For each visible asset:

Asset

Current quality

Problems

Target

Blender source

Rebuild plan

Material plan

Collider plan

Unity verification

Status

Include at minimum:

storefront

doors

windows

signs

walls

floors

ceiling

trim

crates

shelves

retail fixtures

counters

checkout counter

POS

terminal

laptop

stools/chairs

workbenches

hammer

chisel

cracking cradle

wash station

brush

sinks/buckets

inspection tools

magnifier

cracker

saw

lap

storage

collection displays

lamps

packaging

receiving

pallets

dealer/outbox

customer-facing props

back-of-house props

25. BLENDER WORKFLOW

Use both Blender paths.

Live Blender MCP

Use for:

visual inspection;

proportions;

geometry iteration;

materials;

camera work;

live modeling;

viewport captures.

Tools/blender.sh / Blender Python

Use for:

deterministic generation;

repeatable modeling;

exports;

collision proxies;

procedural geodes;

crystal generation;

contact sheets;

validation;

batch production.

Do not create a parallel pipeline unless absolutely required.

26. ORIGINAL ASSETS FIRST

Default to original project-created assets.

Prefer:

Astra-authored Blender models;

Astra-generated original textures/material source images;

procedural Geode geometry;

internally produced signs/labels.

Do not solve the rework by importing random asset packs.

External assets require:

clear necessity;

license verification;

visual compatibility;

explicit record in the asset manifest.

27. IMAGE-GENERATED TEXTURE WORKFLOW

Use image generation when useful for original texture/source imagery.

Possible:

wood variation;

concrete/plaster;

painted metal wear;

geological posters;

labels;

packaging;

signage;

cardboard;

flooring;

wall art.

Verify:

real-world scale;

tiling;

UVs;

roughness;

normals;

color balance.

Do not map arbitrary generated art directly onto geometry without material cleanup.

28. GEODES ARE THE HERO — FULL REDESIGN

Rocks/geodes require one of the deepest passes in the entire game.

The player will inspect them from inches away.

Do not accept procedural quantity as quality.

29. GEODE EXTERIORS

Rework:

silhouette;

asymmetry;

knobbles;

shell thickness;

rind;

weathering;

pits;

cracks;

iron staining;

clay;

exposed mineral hints;

roughness;

micro-normal response.

Avoid:

smooth blobs;

repeated silhouettes;

obvious procedural noise;

plastic surfaces;

stretched textures.

30. GEODE INTERIORS

Rework:

cavity shape;

rind-to-cavity transition;

matrix;

banding;

crystal attachment;

crystal growth direction;

size distribution;

cavity depth;

void variation;

crystal density;

imperfections;

cut boundaries.

Avoid:

floating crystals;

crystals penetrating shell;

identical radial carpets;

neon glow;

flat interior surfaces.

31. CRYSTAL MORPHOLOGY

Families must differ in geometry, not only color.

Vary:

crystal habit;

termination;

width/length;

cluster organization;

translucency;

inclusions;

roughness;

color distribution;

specular behavior.

32. LIGHTING RESPONSE OF ROCKS

Test rocks under:

normal shop lighting;

inspection light;

wet state;

retail display lighting;

collection lighting;

close directional light.

Interiors should look beautiful when illuminated without:

blown highlights;

emissive cheating;

washed-out white crystals.

33. GEODE CONTACT SHEETS

For every family create deterministic review sheets:

rough exterior;

cleaned exterior;

opened;

cut;

polished where appropriate;

retail display;

collection display.

Reject:

duplicates;

weak silhouettes;

repeated cavities;

implausible crystals;

flat interiors;

overexposure;

procedural repetition.

34. CRACKING BENCH — FULL PHYSICAL REWORK

The cracking bench must operate on the real specimen.

Eliminate:

floating rock;

thin-air manipulation;

invisible repositioning;

detached product-viewer behavior.

35. ROCK MUST PHYSICALLY REST ON CRACKING SUPPORT

When placed:

visible support contact must match collision;

rock cannot hover;

rock cannot sink;

irregular rocks need believable support;

rock must remain readable.

Redesign cradle/anvil/support in Blender if required.

36. ACTUAL PICKUP / REPOSITIONING AT CRACKING BENCH

Player must be able to:

pick up the actual specimen;

rotate it;

move it;

reposition it;

set it back down;

choose strike orientation.

Do not merely rotate a virtual proxy.

Exact specimen identity must remain unchanged.

37. HAMMER / CHISEL HERO PASS

Rework:

hammer model;

chisel model;

hand/tool alignment;

chisel placement;

strike timing;

impact animation;

dust/chips;

rock micro-movement;

sound;

haptics;

fracture anticipation.

This interaction should be trailer-worthy.

38. CORRECT-HIT FEEDBACK — REPLACE

Current correct-place hit sound/effect is unacceptable.

Research physically plausible:

hammer on chisel;

chisel on stone;

resonant rock;

dull rock.

Create a satisfying but realistic feedback language.

Do not use:

arcade ding;

piercing click;

fake success chime.

39. ROCK BREAK SOUND — REBUILD

Use layered fracture audio:

impact;

stress;

initial crack;

major split;

chips/debris;

half contact;

settling.

Different sizes/materials should vary.

40. ROCK BREAK ANIMATION — REBUILD

Avoid:

pop apart;

teleport;

book hinge;

long pause;

clipping;

abrupt freeze.

Target:

final impact
→ immediate stress/crack
→ separation
→ gravity
→ contact/bounce
→ settle
→ readable reveal

Do not hide performance lag behind an animation delay.

41. CRACK / REVEAL PERFORMANCE

Preserve prior optimization.

Profile:

final strike;

geometry;

colliders;

crystals;

physics;

VFX;

audio;

thumbnail generation;

save;

collection refresh;

UI.

Physical crack must respond immediately.

42. WASHING — COMPLETE REDESIGN

Explicit known current problems:

brush orientation wrong/upside-down;

brush may obscure rock;

rock too deep in water;

poor visibility;

station facing wall;

station presentation weak.

Rebuild it.

43. WASH STATION FACES PLAYER

Active interaction side must face player.

Camera, hand reach, brush contact, sink geometry, and rock placement must agree.

Do not point active workflow into the wall.

44. WATER / ROCK VISIBILITY

Rock must remain clearly visible while washing.

Water should:

wet/submerge realistically;

not hide the entire specimen;

preserve contrast;

support rinse.

Use believable basin depth.

45. BRUSH REBUILD

Fix/rebuild:

brush model;

handle orientation;

bristle orientation;

scale;

pivot;

grip;

contact point;

animation.

Bristles must touch the rock correctly.

46. WASH ANIMATION / VFX

Improve:

hand motion;

brush stroke;

bristle contact;

water movement;

dirt reduction;

wetness;

rinse;

droplets/splash.

Spatial dirt remains tied to contact.

No magical global wash.

47. INSPECTION RECHECK

Re-audit current manual inspection.

Keep strong parts.

Rework anything that still feels like:

floating product viewer;

instant answer dump;

awkward camera;

UI-driven analysis instead of visual observation.

Magnifier should feel like a real tool.

48. MANAGEMENT / INVENTORY

Through the management laptop/workstation, player should clearly understand:

rough rocks;

opened rocks;

processed pieces;

retail stock;

collection pieces;

incoming crates;

storage capacity.

Avoid spreadsheet busywork.

49. REDESIGN MAJOR MANAGEMENT UI

Treat these as full redesign candidates:

Suppliers

Upgrades

Collection

Business

Stats

Inventory

Premises

Bills

Current functionality is not sufficient.

50. UI ART DIRECTION

Use generated concepts.

Target:

compact;

premium;

simulator-like;

readable;

information-dense;

tactile;

consistent with shop identity.

Avoid:

giant grey slabs;

empty dead space;

placeholder dots;

debug/admin look;

tiny unreadable text.

51. SUPPLIERS UI

Clearly communicate:

source;

crate image;

price;

rock count;

likely quality/character;

risk;

storage impact;

receiving capacity;

arrival;

affordability.

52. UPGRADES UI

Each upgrade shows:

real render/icon/silhouette;

name;

cost;

benefit;

prerequisite;

world effect;

delivery/placement behavior;

operating-cost impact.

No generic placeholder dots.

53. COLLECTION UI

Make collection rewarding.

Use real owned/discovered specimens only.

Show:

actual thumbnail;

family;

rarity;

source;

value;

notable traits;

processing history;

provenance.

No fake default gems.

54. BUSINESS / STATS / BILLS

Business UI should show:

cash;

revenue;

expenses;

rent;

utilities;

profit;

dealer sales;

customer sales;

inventory value;

expansion state.

Stats should be compact.

Bills/Premises should be understandable and actionable.

55. HUD

Re-review final HUD.

It should be:

small;

quiet;

contextual;

readable.

World dominates the screen.

Do not stack multiple giant overlays.

56. AMBIENT AUDIO — FULL REWORK

Current background ambience reportedly sounds like continuous AC.

Replace it.

Create a believable layered shop soundscape.

Possible subtle layers:

room tone;

distant exterior traffic/parking if appropriate;

occasional building creak;

localized electrical hum near powered equipment;

customer movement when open;

door/open-sign cues;

packaging/shop activity.

Do not fill entire game with constant HVAC.

Use silence and dynamic range.

57. ZONE / STATE AUDIO

Where useful vary ambience by:

storefront;

workshop;

storage;

processing;

shop open/closed;

customer presence;

machine state.

58. FULL SFX AUDIT

Audit/rebuild sounds for:

pickup/place;

crate;

water;

brush;

rinse;

hammer;

chisel;

correct hit;

bad hit;

fracture;

debris;

saw;

cracker;

lap;

cash;

coins;

drawer;

card;

bag;

customers;

door;

UI;

discovery;

rare reveal;

bills;

expansion.

Aim for:

pleasant;

realistic;

satisfying;

non-harsh.

Do not preserve weak sound because it is already wired.

59. VFX — FULL AUDIT

Audit/rebuild:

impact;

correct strike;

bad strike;

dust;

chips;

fracture;

water;

wetness;

brush contact;

rinse;

saw coolant;

slurry;

polish;

reveal;

discovery;

rare find.

Avoid:

magical glow;

arcade effects;

transparent particle spam;

performance-heavy excess.

60. ANIMATION — FULL AUDIT

Review:

rock pickup;

rock placement;

brush;

hammer;

chisel;

crack;

cracker;

saw;

lap;

cash drawer;

card;

bills/cash;

bagging;

customer browse;

queue;

handoff;

door;

shop open/close;

equipment delivery/unpacking.

Fix:

clipping;

floatiness;

wrong pivots;

timing mismatch;

robotic motion;

impossible hand/object relationships.

61. CUSTOMERS / NPCS

Audit:

model quality;

scale;

clothing;

material quality;

walk;

idle;

browse;

queue;

checkout;

carry;

exit;

pathing.

Customers must not look like placeholder mannequins.

Do not scope-creep into a huge NPC RPG system.

62. CUSTOMER FLOW

Required:

entrance → browse → select → queue → checkout → exit

Shop open/closed state controls entry.

No teleport-feeling retail.

63. COLLISION / PLACEMENT

After redesign, re-run placement validation.

Reject:

wall intersection;

door blockage;

customer path blockage;

checkout blockage;

station clearance blockage;

machine moving-part blockage;

receiving blockage;

unreachable interactable;

player trap.

64. AUTHORED WORLD COLLISION AUDIT

Default authored layout follows same standards as player placement.

Zero tolerance for:

signs inside walls;

trays through cabinets;

shelves through partitions;

floating props;

machine penetration;

z-fighting;

giant invisible colliders;

undersized colliders.

65. ECONOMY / PROGRESSION REBALANCE

Major layout and equipment changes invalidate old balance.

Rebalance:

starting cash;

crate prices;

dealer payout;

customer payout;

equipment cost;

expansion cost;

rent;

utilities;

storage;

operating costs.

66. FRESH-SAVE PROGRESSION TARGET

Desired progression feel:

Day 1

tiny shop;

cracking;

first crates;

checkout;

management laptop/terminal;

basic selling.

Early

handheld/manual analysis;

simple retail/storage improvement;

cleaning capability;

customer growth.

Mid

improved wash/inspection;

cracker;

first expansion;

better retail/storage.

Later

saw;

lap;

mature processing;

mature showroom;

collection/gallery;

higher expenses;

premium sourcing.

Exact timing may change through playtesting.

Principle is fixed:

start tiny; build everything.

67. TUTORIAL

Update tutorial to match redesigned:

layout;

equipment;

laptop;

shop opening;

purchasing;

placement;

customers;

expansion.

Beacons point to exact runtime objects.

68. INPUT

Everything supports:

keyboard/mouse;

controller.

No mouse-only management step.

No stale hardcoded glyphs.

Preserve rebinding.

69. PERFORMANCE AFTER EVERY MAJOR PASS

Profile after:

architecture;

asset replacement;

geode rebuild;

lighting;

NPC work;

VFX.

Do not wait until the end.

Do not accept a prettier 30 FPS version.

70. LOD / REUSE / INSTANCING

Use budgets appropriate for:

hero assets;

workstations;

repeated props;

shelf geodes;

collection geodes;

NPCs.

Use:

LOD;

shared materials;

instancing;

shadow budgeting;

reduced distant geode detail;

pooled VFX;

collision proxies.

71. SAVE / MIGRATION

Protect:

specimen identity;

collection;

inventory;

equipment placement;

expansion;

bills;

progression.

If redesigned geometry invalidates old world coordinates:

migrate;

relocate safely;

recover;

never silently delete important owned objects.

72. TESTING PHILOSOPHY

Use:

automated tests;

Play Mode observation;

screenshots;

performance profiling;

controller;

KBM;

persistence;

customer stress;

standalone.

No category replaces the others.

73. FRESH-SAVE FULL CAREER — MANDATORY

Before Steam-readiness passes, play a real fresh career through the major progression arc.

Primary acceptance run uses no cheats.

Record:

confusion;

boredom;

bad pacing;

weak art;

bad sound;

awkward interactions;

balance walls;

UI friction;

clipping;

performance.

Fix serious issues.

74. CUSTOMER STRESS

Run in:

starter shop;

expanded shop;

mature shop;

player-customized layout.

Record:

spawned;

served;

abandoned;

queue stalls;

path failures;

recovery reposition;

overlaps;

checkout failures.

Repeated recovery is a defect.

75. STANDALONE BUILD

Produce/test standalone.

Verify:

launch;

new game;

load;

controller;

KBM;

audio;

visuals;

customers;

checkout;

cracking;

washing;

laptop;

save/relaunch;

performance.

Editor-only success is insufficient.

76. FINAL SCREENSHOT SET

Capture public-quality frames:

storefront

Day-1 shop

cracking bench

rock physically resting on support

washing

inspection

reveal

geode macro close-up

early retail

mature retail

checkout

management laptop

Suppliers UI

Upgrades UI

Collection UI

expanded shop

private collection

active customer scene

If a screenshot screams “prototype,” fix it.

77. CONCEPT PARITY LOOP

For every final screenshot compare against:

selected Astra-generated concept;

relevant existing reference image.

Do not require pixel identity.

Require comparable:

composition quality;

material quality;

lighting;

scale;

density;

production feel.

If in-game output is materially worse, keep iterating.

78. STEAM-READINESS MATRIX

Create:

Docs/AstraRework/STEAM_READINESS_MATRIX.md

Rate:

VISUAL

AUDIO

GAMEPLAY

UX

PERFORMANCE

STABILITY

PROGRESSION

CONTROLLER

SAVE

STORE PRESENTATION

Use only:

PASS

CONDITIONAL

FAIL

No hidden “almost.”

79. BUG SEVERITY

P0

crash

save corruption

hardlock

lost/duplicated specimen

impossible progression

P1

major UI overlap

severe clipping

customer deadlock

unusable station

major hitch

broken checkout

controller blocker

persistence failure

P2

minor polish

small audio/animation issue

copy inconsistency

Steam-readiness requires:

zero known P0

zero known P1

80. ASTRA MAY ADD MISSING FEATURES — WITH DISCIPLINE

Astra may add a feature not explicitly listed here only when direct research/playtesting shows it is necessary for:

shop believability;

core-loop quality;

progression clarity;

player comprehension;

commercial presentation.

Before adding:

record problem;

proposed feature;

necessity;

scope;

acceptance criterion.

Do not scope-creep into:

multiplayer;

giant open world;

deep employee management;

huge dialogue trees;

unrelated crafting;

dozens of filler systems.

81. PHASE ORDER

Phase 0 — safety checkpoint

preserve integrity fixes

protect player data

clean checkpoint/branch

Phase 1 — truth audit

old requirements vs actual game

Steam baseline

Phase 2 — research + image-generated art direction

benchmarks

concepts

chosen target

Phase 3 — shop architecture

storefront

entrance

zoning

checkout

open/close

progression footprint

Phase 4 — all visible asset re-authoring

manifest

Blender rebuilds

materials

props

Phase 5 — geode hero rework

exteriors

interiors

crystals

lighting

contact sheets

Phase 6 — core interactions

cracking

physical specimen support

washing

inspection

break/reveal

Phase 7 — audio/VFX/animation

hero loop polish

Phase 8 — laptop/UI

inventory

suppliers

upgrades

collection

business

stats

bills

Phase 9 — customers/retail

entrance

browse

checkout

exit

open/close

Phase 10 — progression/economy

Day 1

equipment

placement

expansion

operating costs

Phase 11 — performance

60 FPS

frame pacing

memory

Phase 12 — full QA

fresh career

customer stress

controller/KBM

saves

standalone

Phase 13 — visual parity

concepts vs Unity

references vs Unity

iterate

Phase 14 — Steam-readiness report

final matrix

evidence

screenshots

commits

82. GIT SAFETY

This pass is broad.

Prefer:

dedicated Astra rework branch/worktree from known-good baseline;

coherent milestone commits;

verified pushes;

merge only known-good milestones into main.

If direct-main policy is required:

checkpoint first;

keep every milestone independently revertible;

never push broken state.

No force pushes.
No history rewrites.

Keep origin/main safe.

83. REQUIRED DOCUMENTS

Maintain:

Docs/AstraRework/PROJECT_TRUTH_MATRIX.md

Docs/AstraRework/PLAN.md

Docs/AstraRework/STEAM_SIMULATOR_BENCHMARKS.md

Docs/AstraRework/ART_DIRECTION.md

Docs/AstraRework/ASSET_REWORK_MANIFEST.md

Docs/AstraRework/PERFORMANCE_BUDGET.md

Docs/AstraRework/STEAM_READINESS_MATRIX.md

Docs/AstraRework/FINAL_REPORT.md

Documentation is evidence/planning.

It is not acceptance.

84. DEFINITION OF DONE — SHOP

real storefront/customer entrance

believable exit

open/close state

checkout location makes sense

customer flow makes sense

receiving/storage makes sense

processing zones make sense

workstations face player

no arbitrary crate crowding

Day 1 tiny

later equipment absent until purchased

player places upgrades

expansion visibly changes shop

final layout approaches generated concepts

85. DEFINITION OF DONE — ASSETS

every visible player-facing asset audited

every weak/inconsistent player-facing asset rebuilt or materially redesigned

hero assets match unified art direction

no obvious prototype geometry in hero spaces

believable dimensions

correct pivots

correct normals

coherent materials

appropriate colliders

Unity first-person verification passed

86. DEFINITION OF DONE — GEODES

exteriors high quality

interiors high quality

crystal morphology genuinely varied

family identity not color-only

rind/cavity transitions believable

no floating/interpenetrating crystals

cut faces coherent

wet/inspection/display lighting works

contact sheets reviewed

procedural repetition reduced

hero rocks survive close inspection

performance budget maintained

87. DEFINITION OF DONE — CRACKING

rock physically rests on support

no thin-air manipulation

actual specimen can be picked up/repositioned

strike placement feels physical

correct-hit sound/VFX satisfying

poor-hit feedback distinct

fracture audio satisfying

break animation believable

no crack hitch

halves settle correctly

pickup after reveal works

88. DEFINITION OF DONE — WASHING

station faces player

rock visible

water depth sensible

brush correctly oriented

brush scale believable

bristles contact rock

animation convincing

dirt removal spatial

wetness/rinse readable

no one-button magic clean

controller + KBM usable

89. DEFINITION OF DONE — AUDIO/VFX/ANIMATION

ambience no longer resembles constant AC

open/closed and zone ambience believable

correct-hit sound good

poor-hit sound good

fracture sound good

washing sound good

machine audio good

checkout audio good

VFX realistic/restrained

no weak placeholder hero sounds

animations do not clip/float

volume matching consistent

90. DEFINITION OF DONE — UI

physical management laptop/workstation exists

inventory understandable

Suppliers redesigned

Upgrades redesigned

Collection redesigned

Business redesigned

Stats redesigned

Premises/Bills coherent

HUD compact

controller navigation

KBM

no giant grey placeholder panels

UI matches final art direction

91. DEFINITION OF DONE — CUSTOMERS

customers use real entrance

open/close controls entry

browse works

queue works

checkout works

exit works

no repeated jams

NPC visuals acceptable

starter/mid/late layouts all work

92. DEFINITION OF DONE — BUSINESS

minimal Day-1 progression

equipment earned

equipment delivered

equipment player-placed

inventory works

shop expands

rent works

utilities work

bills understandable

economy rebalanced

no unavoidable bankruptcy

progression visibly changes shop

93. DEFINITION OF DONE — TECHNICAL

no known P0

no known P1

full automated suite green

integrity suite green

customer stress green

collision/placement audits green

save/load green

migration green

controller green

KBM green

standalone green

documented performance target met

no major frame hitch

player-data isolation verified

94. DEFINITION OF DONE — VISUAL / STEAM READINESS

original target concepts created

art direction documented

Unity screenshots iterated against concepts

existing references compared

storefront credible

Day-1 credible

workshop credible

geode close-up credible

retail credible

checkout credible

UI credible

mature shop credible

60 FPS target maintained in documented target configuration

game no longer visually reads as prototype

95. FINAL ACCEPTANCE RUN

Before declaring this project rework complete:

verify clean known-good project state;

verify isolated test save;

compile;

run full tests;

start real fresh save;

play opening manually;

receive crate;

crack rock;

physically reposition rock;

inspect;

wash after unlock;

sell;

open shop;

serve customers;

complete checkout;

use management laptop;

order upgrade;

receive upgrade;

place upgrade;

expand shop;

pay bills;

use cracker;

use saw;

use lap where suitable;

build collection;

save;

reload;

test controller;

test KBM;

stress customers;

profile;

build standalone;

smoke standalone;

capture final screenshots;

compare to Astra concepts;

compare to Geode references;

fix obvious remaining defects;

rerun affected tests;

update Steam-readiness matrix;

commit/push verified result.

96. FINAL REPORT

Create:

Docs/AstraRework/FINAL_REPORT.md

Include:

which prior milestone claims proved true;

which were partial;

which were false;

major shop redesign summary;

asset rebuild summary;

geode rebuild summary;

audio/VFX/animation summary;

UI summary;

customer/retail summary;

progression/economy summary;

performance results;

customer stress;

save/migration;

controller/KBM;

standalone;

final screenshot index;

Steam-readiness matrix;

remaining P2 issues;

exact milestone commits.

Do not hide failed requirements.

97. COMPLETION DECLARATION

Do not say:

“V4 100%”

“V5 100%”

“V6 100%”

“Steam ready”

because an old checklist says so.

You may declare:

ASTRA FULL PROJECT REWORK COMPLETE — STEAM-READINESS GATE PASSED

only when:

project truth audit is complete;

zero known P0/P1 remain;

shop and assets pass visual review;

geodes pass hero-quality review;

audio/VFX/animation pass;

UI passes;

fresh career passes;

customer stress passes;

save/migration passes;

controller/KBM pass;

standalone passes;

performance meets the documented target;

real in-game screenshots are materially close to selected Astra-generated concept targets and the Geode reference quality;

verified work is safely committed/pushed.

If any are false:

KEEP WORKING.

98. FINAL EXECUTION RULE

Take the time required.

Do research.

Generate concept images.

Use image generation for original texture/source imagery when useful.

Model seriously in Blender.

Rebuild assets, do not decorate prototypes.

Inspect Unity continuously.

Play the real interactions.

Listen to every important sound.

Watch every important animation.

Profile performance.

Use contact sheets.

Use screenshots.

Use customer stress tests.

Use save/load.

Use controller.

Use KBM.

Use standalone builds.

Do not optimize for finishing quickly.

Do not preserve weak work because a previous agent called it complete.

Do not accept a Blender asset until it works in Unity.

Do not accept a functional interaction until it feels good.

Do not accept a screenshot if it visibly falls short of the selected concept.

Do not accept a prettier scene if performance collapses.

Keep origin/main safe.

No force pushes.

No history rewrites.

The actual playable game is the acceptance criterion.