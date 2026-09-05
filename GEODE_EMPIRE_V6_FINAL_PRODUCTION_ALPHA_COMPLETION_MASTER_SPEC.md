GEODE EMPIRE V6 — FINAL PRODUCTION ALPHA COMPLETION MASTER SPEC

Authoritative completion specification for Opus 5

Primary acceptance rule: The actual playable game is the acceptance criterion.

This document becomes the controlling V6 completion specification after the completed visual rebuild, starter-shop/progression rebuild, hands-on core-loop overhaul, physical checkout port, tutorial/rebinding work, UI QA, shop expansion, and business-economy work.

Preserve completed known-good systems unless later testing exposes a real regression. Resume from the first genuinely incomplete V6 requirement, beginning with specimen diversity and specimen-specific gameplay, then continue autonomously through every remaining V6 gate.

0. MISSION

Geode Empire must leave V6 as a coherent Production Alpha, not as a collection of individually functional systems.

The player must be able to:

Receive → Clean → Inspect → Predict → Choose Process → Open/Cut → Reveal → Verify/Appraise → Keep/Sell/Retail → Upgrade → Expand → Manage Costs → Repeat

while the business visibly grows from a tiny starter operation into the mature shop shown by the strongest reference images.

The central fantasy remains:

WHAT IS INSIDE THIS ROCK?

Every important system should strengthen that question.

V6 is complete only when:

specimens are meaningfully diverse;

specimen properties affect gameplay decisions;

hands-on processing is satisfying;

progression visibly changes the shop;

customers and checkout work reliably;

the economy supports rent, utilities, sourcing, equipment and expansion;

UI is compact and production-quality;

world art survives close first-person inspection;

performance is responsive;

controller + KBM both work;

saves persist correctly;

a real fresh career and standalone build pass.

1. SOURCE AUTHORITY

When requirements conflict, use this order:

Actual observed playable result.

This master spec.

GEODE_EMPIRE_V6_PRODUCTION_ALPHA.md.

Completed override specs:

visual rebuild;

starter-shop/progression/UI rebuild;

hands-on core-loop/expansion/economy overhaul;

checkout handoff.

Reference images in Geode/references and Geode/refrences if present.

Older V5/V4 docs.

Do not resurrect an older behavior that a newer completed pass intentionally replaced.

2. KNOWN-GOOD BASELINE — DO NOT REOPEN WITHOUT EVIDENCE

Treat these as locked unless later V6 testing exposes a real defect.

2.1 Starter shop

Preserve:

compact Day-1 footprint;

future areas absent/closed instead of visually complete;

machines/fixtures earned instead of preinstalled;

player placement;

empty collection defaults;

visible business growth.

2.2 Hands-on loop

Preserve:

manual inspection;

magnifier/loupe;

true specimen manipulation;

spatial dirt;

manual washing;

manual-first progression;

hammer/chisel foundation;

crack/reveal performance improvements;

restrained discovery notifications.

2.3 Business systems

Preserve:

leases/expansion;

rent;

electricity;

water;

bills/ledger;

finite receiving;

early retail.

2.4 Checkout

Preserve:

customer approach;

item staging;

cash;

change;

card;

drawer;

packaging;

handoff;

exact specimen identity;

controller support;

persistence.

2.5 UI / input

Preserve:

tutorial beacon system;

live input glyphs;

rebinding;

settings;

prior UI render QA;

compact HUD direction;

tablet framework.

If a regression appears, fix it and rerun that system's acceptance tests. Otherwise keep moving.

3. EXECUTION MODE

Before new implementation:

Read this entire file.

Read only the genuinely incomplete parts of GEODE_EMPIRE_V6_PRODUCTION_ALPHA.md.

Inspect current git status and origin/main.

Run the complete current automated suite.

Launch a fresh save.

Capture representative current-state screenshots.

Create Docs/V6Final/PLAN.md.

Record current test counts, performance, and known remaining gaps.

Immediately execute the plan without waiting for approval.

For every milestone:

observe → diagnose → implement → play → capture → compare → test → commit → push

Do not declare success from code alone.

4. PRIORITY ORDER

Use this order unless an actual dependency requires adjustment:

Specimen diversity.

Specimen-specific gameplay.

Sourcing/provenance/rarity depth.

Processing outcome depth.

Customer/retail depth.

Checkout final verification.

Career/economy final balance.

Tutorial and UI final pass.

Audio/VFX/processing feel.

Environment/NPC/art final pass.

Persistence/controller/KBM.

Performance.

Full fresh-save career.

Standalone build.

Final screenshots/contact sheets/report.

Do not start V7 content before this closes.

5. SPECIMEN DIVERSITY — NEXT MAJOR PRIORITY

Specimen diversity is not satisfied by hidden seed differences or different colors.

A player looking at a table of rocks should believe they are different physical specimens.

Vary meaningfully:

silhouette;

asymmetry;

elongation;

roundness;

shell thickness;

shell roughness;

weathering;

pits;

stains;

openings;

seams;

apparent density;

exposed mineral clues;

cavity position;

cavity size;

banding;

crystal scale;

crystal density;

crystal orientation;

crystal morphology;

matrix;

translucency;

cut-face structure;

impurity;

damage;

size.

Avoid:

same blob / different material;

same cavity / different color;

neon palette swaps;

repeated crystal arrangements;

obvious procedural tiling.

6. FAMILY IDENTITY

Each mineral family should have recognizable tendencies without becoming deterministic.

A skilled player should gradually learn:

visual tendencies;

source tendencies;

rind tendencies;

acoustic tendencies;

processing suitability;

value tendencies.

But exterior inspection should never trivially reveal exact contents.

7. INTRA-FAMILY VARIATION

Within one family, create meaningful variance in:

overall geometry;

crystal form;

cavity percentage;

rind;

impurity;

quality;

damage;

cut suitability;

polish potential;

value.

Reject families where ten generated rocks look like minor rotations of the same asset.

8. CONTACT-SHEET QA

For every family, generate deterministic representative sets.

Capture:

unopened;

washed exterior;

opened;

cut;

polished where applicable;

retail lighting;

collection lighting.

Build contact sheets and inspect them.

Reject specimens with:

duplicate silhouettes;

repeated cavity shapes;

flat interiors;

blown highlights;

floating crystals;

impossible crystal penetration;

ugly shell/cavity seams;

UV/material stretching;

toy-like colors;

weak hero quality.

Do not rely on memory. Compare images side by side.

9. HERO SPECIMEN BAR

A hero specimen must survive close first-person inspection.

Audit:

shell silhouette;

rind depth;

cavity depth;

shell/interior transition;

crystal attachment;

cut-face continuity;

visible holes;

clipping;

normals;

z-fighting;

material response.

Use Blender seriously when runtime procedural geometry is the limiting factor.

Do not hide bad geometry with lighting.

10. SPECIMEN-SPECIFIC GAMEPLAY

Different specimens must cause different player decisions.

Specimen properties should influence:

cleaning;

inspection;

prediction;

hammer/chisel;

cracker;

saw;

polishing;

appraisal;

keeping/selling.

Do not let all rocks reduce to the same interaction sequence followed by a different dollar value.

11. EXTERIOR CLUES

Possible exterior observations:

thick rind;

thin rind;

iron staining;

weathering;

pits;

exposed quartz;

banding;

visible cavity;

fracture seam;

dense shell;

fragile line;

unusual texture;

secondary mineral staining;

partial crystal exposure;

hollow/dense acoustic response.

Clues should be probabilistic evidence.

Use the hands-on inspection system already built.

12. OBSERVE → INFER → CHOOSE

The intended mental loop is:

Observe evidence → form hypothesis → choose processing method → reveal → learn

Inspection should change confidence, not reveal an answer sheet.

After reveal, the player should learn whether their interpretation was good.

13. CLEANING DIFFERENCES

Different rocks may have:

light dust;

heavy clay;

stubborn deposits;

iron-rich mud;

brittle exposed material;

fragile seams.

Cleaning can expose new clues.

Do not make every specimen require identical scrubbing time.

Do not turn cleaning into grind.

14. HAMMER / CHISEL DIFFERENCES

Specimen properties can affect:

useful strike areas;

required force;

number of strikes;

resonance;

fracture propagation;

risk of damage;

interior preservation.

Good hit and poor hit should remain physically distinguishable.

15. CRACKER DIFFERENCES

The cracker should be advantageous for some specimens and risky/suboptimal for others.

Consider:

shape stability;

shell thickness;

asymmetry;

brittle crystal;

value preservation.

No machine should be universally best.

16. SAW DIFFERENCES

Saw gameplay should reward:

orientation;

cavity prediction;

symmetry;

specimen size;

rind thickness;

slab potential;

material-loss consideration;

polish potential.

Meaningful options should include:

split;

cut;

preserve rough;

sell unopened.

17. POLISH DIFFERENCES

Only physically suitable surfaces should polish.

Good candidates:

flat cut face;

banded agate surface;

appropriate matrix.

Bad candidate:

open crystal cavity.

Polishing can improve:

presentation;

value;

collection appeal.

18. PROCESSING HISTORY

Store meaningful processing history:

cleaned;

observed clues;

tools used;

hammer/cracker/saw path;

cut orientation;

damage;

polish;

appraisal.

Use it in provenance and collection.

19. PROCESSING OUTCOME

Player decisions should affect:

damage;

symmetry;

material loss;

quality;

sale value;

customer appeal;

collection appeal.

The exact same geological specimen processed differently should not always produce an identical economic/presentation result.

20. PROVENANCE DEPTH

Track:

source;

crate;

purchase date;

processing path;

tools;

damage;

appraisal;

collection;

sale destination;

notable records.

Use provenance meaningfully in:

appraisal;

collection;

customer interest where appropriate;

records.

21. RARITY

Rarity must remain genuinely rare.

Avoid:

constant rare popups;

rarity being just color;

meaningless tier inflation.

Rare specimens should be unusual through some combination of:

mineral family;

morphology;

size;

quality;

structure;

provenance;

combination;

preservation.

Use the notification hierarchy already built.

22. REVEAL

Reveal remains the emotional peak.

Required:

immediate final-hit response;

no visible freeze;

readable separation;

believable settling;

good sound;

visible interior before UI takes over.

For exceptional finds, increase presentation carefully without removing control for too long.

23. APPRAISAL

Appraisal should explain why a specimen is worth what it is.

Consider:

family;

rarity;

size;

crystal quality;

damage;

method;

symmetry;

polish;

provenance;

presentation.

Do not dump hidden formulas at the player.

Teach the value logic through readable factors.

24. SOURCING

Different sources should have distinct economic/geological identities.

Potential dimensions:

family distribution;

quality;

dirt;

damage;

rarity;

size;

shipping;

price;

provenance appeal.

No one crate/source should dominate all progression.

25. SUPPLIERS UI

The Suppliers page must make a decision understandable in seconds.

Show clearly:

price;

source;

rock count;

source character;

quality/risk;

receiving destination;

free slots;

affordability;

progression requirements.

Use real crate/source visuals or strong authored iconography.

No generic empty cards.

26. RECEIVING

Preserve finite receiving.

Test:

multiple simultaneous orders;

starter capacity;

expanded capacity;

full capacity;

no crate overlap;

no wall/pallet penetration;

all crates reachable;

persistence.

27. CUSTOMERS

Add enough customer variety to make retail feel alive.

Vary:

budget;

patience;

browsing duration;

specimen preference;

rarity preference;

size preference;

willingness to buy.

Avoid every customer behaving identically.

28. CUSTOMER VISUAL QUALITY

Review NPCs in real first-person gameplay.

Improve when necessary:

proportions;

clothing;

materials;

walk;

idle;

browse pose;

queue pose;

checkout pose;

carry/departure.

They should not look like prototype mannequins.

Do not scope-creep into a huge character customization system.

29. CUSTOMER NAVIGATION

Test:

Day-1 shop;

first expansion;

player-customized layout;

mature showroom.

Required path:

entrance → browse multiple areas → select → queue → checkout → exit

Repeated recovery teleports are a failure, not success.

30. EARLY RETAIL

Preserve early selling.

Once the player has a legitimate sale item:

they can display it;

a customer can enter;

browse;

purchase;

checkout;

exit.

Mature showroom traffic is progression, not a prerequisite for retail existing at all.

31. MERCHANDISING

Retail displays must use actual inventory.

Improve:

price cards;

spacing;

lighting;

empty states;

customer reach;

restocking.

No fake player-owned decorative sale stock.

32. CHECKOUT FINAL VERIFICATION

Re-verify the known-good physical checkout after all remaining changes.

Test:

customer approach;

staging;

scan/bag;

cash;

exact tender;

change;

drawer;

card;

processing/approval;

package;

handoff;

exact specimen identity;

customer departure.

Run repeated customers.

33. CHECKOUT FEEL

Review:

input latency;

camera stability;

cash readability;

coin readability;

drawer motion;

keypad;

terminal;

bag placement;

item clipping;

audio;

haptics.

Fix any first-person awkwardness.

34. SHOP EXPANSION

Preserve and final-test all physical expansions.

Each expansion must:

visibly change the world;

open usable area;

change placement boundaries;

increase rent;

unlock meaningful capability;

persist.

Do not reduce expansion to a menu number.

35. RENT / UTILITIES / BILLS

Preserve:

rent;

electricity;

water;

billing cadence;

notices;

recoverable late-payment path.

Player must always understand:

amount;

category;

due date;

cause.

No mysterious deductions.

36. ECONOMY FINAL BALANCE

After specimen/source/customer work, rebalance the economy.

Review:

starting cash;

crate prices;

dealer payouts;

retail payouts;

customer budgets;

equipment cost;

operating cost;

rent;

utilities;

expansion;

storage;

progression.

37. ECONOMY TARGET

The player should experience:

meaningful scarcity;

survivable Day 1;

useful dealer;

useful retail;

meaningful equipment ROI;

meaningful expansion timing;

meaningful operating costs;

exciting rare finds.

Avoid:

unavoidable bankruptcy;

trivial infinite profit;

one dominant source;

one dominant processing method;

always-expand-immediately strategy.

38. CAREER SIMULATIONS

Run deterministic careers representing:

cautious;

average;

aggressive;

dealer-heavy;

retail-heavy;

machine-heavy;

expansion-heavy;

unlucky;

lucky.

Record:

first upgrade;

first machine;

first expansion;

showroom;

late-game stability.

39. CAREER OBJECTIVES

Objectives should communicate:

what to do;

why;

what unlocks next.

Do not show the player every future objective at once.

Keep active goals focused.

40. TUTORIAL FINAL PASS

Run from fresh save.

Ensure tutorial teaches current reality:

receiving;

handling;

washing;

inspection;

prediction;

hammer/chisel;

appraisal;

selling;

early customers;

build mode;

upgrades;

expansion;

bills.

No tutorial should reference removed starter equipment or stale room coordinates.

41. TUTORIAL TARGETS

Verify exact runtime targeting for:

hammer;

chisel;

magnifier;

brush/wash;

crate;

dealer;

display;

checkout;

build-mode target;

tablet controls.

Targets must follow player-placed/moved equipment and survive save/load.

42. HUD FINAL PASS

The HUD should remain materially smaller than the old version.

Check:

top-left objective card;

top-right cash/time/level;

XP;

bottom rail;

interaction prompt;

tutorial banner;

notifications.

The 3D world must dominate the screen.

Never let multiple large overlays stack.

43. TABLET FINAL PASS

Review:

Suppliers;

Upgrades;

Collection;

Stats;

Premises;

Bills;

Career.

Required:

strong hierarchy;

compact density;

clear tabs;

real preview art;

useful empty states;

controller navigation;

KBM navigation;

no giant grey placeholder slabs.

44. UPGRADES UI

Each upgrade should show:

real icon/render/silhouette;

name;

cost;

benefit;

prerequisites;

physical world effect;

delivery/placement behavior;

operating-cost impact if applicable.

No identical placeholder circles.

45. COLLECTION

Collection must:

start empty;

show only genuine discoveries/owned specimens;

respect exact identity;

show provenance;

show value/size/rarity;

not use fake decorative owned specimens.

46. COLLECTION PRESENTATION

Discovered specimen entries should feel rewarding.

Use:

actual thumbnail;

family;

rarity;

source;

processing history;

value;

notable traits.

Undiscovered content should remain meaningfully unknown.

47. PRIVATE COLLECTION

If available in V6:

must feel earned;

start empty;

allow real owned specimens;

preserve exact identity;

have intentional empty states.

No default fake showcase gems.

48. STATS

Stats should be useful and compact.

Potential metrics:

rocks processed;

revenue;

profit;

expenses;

highest-value find;

rarest find;

dealer sales;

customer sales;

expansions;

margins.

Avoid giant flat empty cards.

49. WORLD EVOLUTION

Capture and compare:

Day 1.

first upgrade.

first expansion.

midgame.

mature showroom.

late collection.

The player's career story must be visually obvious.

50. ENVIRONMENT ART FINAL SWEEP

Walk every accessible area in first person.

Inspect:

floors;

ceilings;

walls;

corners;

doors;

shutters;

hoarding;

workstations;

shelves;

signs;

plumbing;

electrical;

storage;

checkout;

collection.

Fix anything still reading as:

prototype;

oversized;

floating;

too empty;

randomly cluttered;

clipped;

visually inconsistent.

51. MATERIALS

Maintain clear material identity for:

concrete;

wood;

painted metal;

stainless;

glass;

rubber;

plastic;

stone;

fabric;

crystal.

Avoid the shop becoming uniformly brown/orange.

52. LIGHTING

Target:

warm task lighting;

restrained ambient;

cool/warm separation;

readable interiors;

no blown specimen highlights;

no black unusable corners;

no overwhelming orange cast.

Use lighting to guide the player.

53. BLENDER QUALITY BAR

If geometry is the limiting factor, use Blender.

Do not decorate a prototype mesh that should be replaced.

Prioritize hero assets:

inspection;

wash;

hammer/chisel;

cracker;

saw;

lap;

checkout;

display;

collection;

receiving.

54. SIGNAGE / PROPS

Signs:

correct alignment;

no penetration;

consistent typography/material;

useful navigation.

Props:

purposeful;

functional;

believable.

Good:

tools;

geological references;

safety equipment;

packaging;

labels;

cleaning supplies;

work notes.

Bad:

random clutter;

duplicated fake rocks;

objects that impede play.

55. AUDIO MASTER PASS

Review:

ambience;

footsteps;

handling;

magnifier/tool handling;

washing;

brush;

water;

chisel;

good hit;

poor hit;

fracture;

debris;

cracker;

saw;

lap;

reveal;

UI;

customer;

checkout;

cash;

card;

discovery;

bills;

expansion.

Volume-match the set.

Avoid piercing UI sounds and repetitive samples.

56. VFX MASTER PASS

Review:

dust;

chips;

water;

wetness;

crack;

coolant;

polish;

reveal;

rare discovery.

Avoid:

arcade glow;

huge transparent particle spam;

performance-heavy unnecessary effects.

57. PROCESSING FEEL

Machines must feel mechanically different.

Hammer:

manual;

skillful;

risky.

Cracker:

controlled pressure;

clean split.

Saw:

powered;

precise;

deliberate feed/coolant.

Lap:

finishing;

material removal;

progressive polish.

Do not reuse the same interaction pattern with different props.

58. MACHINE ANIMATION / CLEARANCE

Review:

clamps;

handles;

moving parts;

saw feed;

cracker motion;

lap rotation;

covers;

coolant.

No:

clipping;

impossible pivots;

weightless motion;

blocked working volume.

Re-run placement clearance.

59. BUILD MODE FINAL PASS

Verify:

valid/invalid ghost;

bounds;

rotation;

snapping;

wall collision;

fixture collision;

doorway protection;

route protection;

machine clearance;

remove/relocate;

save/load;

expansion boundaries.

60. AUTHORED WORLD COLLISION AUDIT

Apply the same standards to authored/default objects.

Zero tolerance for:

signs in walls;

trays through cabinets;

shelves through partitions;

embedded machines;

floating props;

z-fighting;

bad collider sizes.

Intentional overlaps require explicit justification.

61. PERFORMANCE PROFILING

Profile representative states:

Day 1;

active washing;

inspection;

final crack/reveal;

saw;

mature showroom;

multiple customers;

checkout;

tablet;

save;

expansion.

Measure:

frame time;

GC;

memory;

triangles;

draw calls;

realtime lights;

transparency;

collider cost;

NavMesh/repath;

thumbnails;

save spikes.

62. PERFORMANCE PRIORITIES

No visible hitch on:

crack/reveal;

specimen pickup;

tablet open;

customer spawn;

crate delivery;

save;

shop expansion.

Avoid repeated:

specimen rebuild;

material instantiation;

shelf regeneration;

collider cooking;

thumbnail duplication.

63. SPECIMEN PERFORMANCE BUDGET

Hero specimens may be expensive.

Background/retail specimens must use appropriate budgets.

Use:

crystal budgets;

LOD;

reduced distant geometry;

material sharing;

cached data.

A full showroom must remain performant.

64. SAVE / PERSISTENCE FINAL PASS

Test:

exact specimen identity;

dirt;

clues;

processing history;

collection;

sale stock;

shop placement;

expansion;

machines;

rent;

utilities;

bill schedule;

crate orders;

receiving;

tutorial;

settings;

bindings.

No:

duplicates;

resets;

fake collection repopulation;

missing machines;

lost bills;

moved fixtures returning to defaults.

65. SAVE MIGRATION

If schema changed:

migrate older V6 saves;

preserve progression;

preserve specimens;

preserve collection;

do not duplicate;

do not unexpectedly reposition everything.

66. CONTROLLER FULL-CAREER TEST

Run:

movement;

pickup;

specimen rotation;

magnifier;

washing;

hammer/chisel;

all machines;

tablet;

build mode;

checkout;

bills;

collection;

settings/rebinding.

No mouse-only step.

67. KBM FULL-CAREER TEST

Run same path with keyboard/mouse.

Verify:

no hardcoded glyph errors;

fine manipulation;

no input conflict;

UI usable.

68. REBINDING REGRESSION

Verify live binding updates in:

HUD;

tutorial;

interaction prompts;

checkout;

build mode;

tablet.

Bindings persist after relaunch.

69. UI RENDER QA

Run at minimum:

1920×1080;

2560×1440;

3840×2160;

supported laptop/taller aspect;

normal UI scale;

larger UI scale.

Verify:

no clipping;

no overlap;

no offscreen close controls;

scroll containers intentional;

no HUD/tablet collision.

Use planted negative controls where useful to confirm the QA instrument still detects failures.

70. CUSTOMER STRESS

Run extended retail stress.

Record:

spawned;

served;

abandoned;

queue stalls;

recovery reposition events;

collision overlaps;

checkout failures;

path failures.

Repeated recovery teleport/reposition means the layout/navigation still has a defect.

71. FRESH-SAVE CAREER — MANDATORY

Play a genuine fresh career without cheats for the primary run.

Verify sequence:

tiny starter shop;

first crate;

manual wash;

inspection;

prediction;

hammer/chisel;

appraisal;

dealer;

early customer sale;

first upgrade;

first placed equipment;

first expansion;

first bill;

powered processing;

mature retail;

collection;

rare find;

late progression.

Record friction and fix serious problems.

72. CAREER PACING QUESTIONS

During fresh career ask:

Is any step boring?

Is any cost unexplained?

Is the player stuck waiting?

Is early income sufficient?

Is expansion exciting?

Is a machine upgrade meaningful?

Does a rare find matter?

Does collection feel earned?

Does the shop visibly evolve?

Do not ignore bad pacing just because no hard bug occurs.

73. STANDALONE BUILD

Produce a real standalone build.

Test:

launch;

new save;

load;

controller;

KBM;

resolution;

audio;

performance;

reveal;

customers;

checkout;

save/relaunch;

quit.

Do not rely on Editor-only success.

74. FIRST-PERSON VISUAL QA

Walk slowly through every accessible space.

Look:

up;

down;

behind furniture;

behind machines;

underneath benches;

at thresholds;

at ceilings;

at wall joins;

at collection;

at checkout;

at displays.

Fix obvious defects.

Curious players must not instantly find broken geometry.

75. REFERENCE COMPARISON LOOP

For each category:

reference → current capture → critical written comparison → implementation → new capture → accept/reject

Categories:

Day-1 workshop;

inspection;

washing;

cracking;

saw;

lap;

receiving;

retail;

checkout;

collection;

appraisal;

tablet;

mature shop.

Target:

same quality class;

coherent shared art direction;

better fit to gameplay than concept art.

76. PUBLIC-SCREENSHOT BAR

Before V6 completion, capture:

Day-1 shop.

inspection.

manual washing.

hammer/chisel.

reveal.

saw.

retail.

checkout.

first expansion.

mature showroom.

collection.

tablet.

rare specimen.

If one still screams “prototype,” fix it.

77. BUG SEVERITY

P0

crash;

save corruption;

progression hardlock;

lost/duplicated specimen;

checkout impossible.

P1

repeated customer jam;

persistence failure;

unusable machine;

severe clipping;

major UI overlap;

major hitch.

P2

minor art/audio/copy polish issue.

V6 may not ship its Production Alpha declaration with known P0/P1 defects.

78. AUTOMATED TESTS

Extend tests where valuable for:

specimen invariants;

surface/clue logic;

dirt;

economy;

billing;

sourcing;

placement;

persistence;

checkout;

UI bounds.

Automation supports hands-on QA. It does not replace it.

79. REPO SAFETY

Use:

clean milestone commits;

descriptive messages;

verified pushes.

Do not:

force push;

rewrite history;

leave broken main;

commit huge unnecessary generated garbage.

Keep origin/main at known-good milestones.

80. V6 SCOPE BOUNDARY

V6 is Production Alpha.

It should prove:

complete game loop;

career arc;

scalable specimen system;

physical interactions;

business growth;

strong art direction;

reliable technical foundation.

Do not scope-creep into:

multiplayer;

giant open world;

employee-management sim;

massive dialogue system;

dozens of filler mineral families;

deep accounting;

V7 breadth.

81. V7 HANDOFF

Only after V6 truly completes, create:

Docs/V7/BACKLOG.md

Carry forward:

additional sources;

additional specimen families;

content breadth;

optional advanced customer variation;

further late-game breadth;

playtest-driven balance.

Do not move unfinished V6 requirements into V7 to claim completion.

82. DEFINITION OF DONE — SPECIMENS

families visually distinct;

intra-family variation convincing;

no same-blob/different-color effect;

rind/cavity transitions hold up close;

crystal morphology varies;

contact sheets reviewed;

weak outputs rejected;

hero specimens survive close inspection;

showroom specimen budgets perform.

83. DEFINITION OF DONE — SPECIMEN GAMEPLAY

clues meaningful;

clues probabilistic;

cleaning reveals evidence;

processing choice depends on specimen;

hammer/cracker/saw tradeoffs exist;

polish suitability physical;

processing decisions affect outcome;

appraisal explains value;

provenance preserved.

84. DEFINITION OF DONE — SOURCING / RARITY

sources distinct;

crate choices meaningful;

no dominant source;

receiving safe;

rarity actually rare;

rare specimens meaningful;

notification presentation restrained.

85. DEFINITION OF DONE — CUSTOMERS / RETAIL

early retail works;

customer variety exists;

starter navigation works;

expanded navigation works;

mature navigation works;

queue works;

checkout works;

exit works;

no normal-layout recovery spam;

customer art acceptable.

86. DEFINITION OF DONE — CHECKOUT

cash;

exact tender/change;

card;

drawer;

packaging;

handoff;

exact specimen identity;

repeated customers;

controller;

KBM;

persistence.

87. DEFINITION OF DONE — BUSINESS

tiny starter shop;

visible expansion;

machines earned;

placement meaningful;

rent works;

utilities work;

bills understandable;

expansion economic decision;

economy balanced;

no unavoidable bankruptcy spiral.

88. DEFINITION OF DONE — UI

HUD compact;

tutorial compact;

Suppliers polished;

Upgrades polished;

Collection polished;

Stats polished;

Premises/Bills polished;

controller navigation;

KBM navigation;

render QA green.

89. DEFINITION OF DONE — WORLD / ART

starter workshop polished;

mature showroom polished;

hero workstations polished;

materials cohesive;

lighting controlled;

no major placeholder hero assets;

no obvious clipping;

authored collision audit clean;

first-person sweep clean;

screenshot quality acceptable.

90. DEFINITION OF DONE — AUDIO / VFX

core impacts satisfying;

fracture satisfying;

wash/water convincing;

machines distinct;

checkout clear;

UI restrained;

VFX readable;

reveal responsive.

91. DEFINITION OF DONE — TECHNICAL

full automated suite green;

no known P0/P1;

customer stress green;

placement audit green;

persistence green;

migration green;

controller green;

KBM green;

performance acceptable;

standalone green.

92. DEFINITION OF DONE — CAREER

fresh career completed;

no progression deadlock;

first hour engaging;

upgrades meaningful;

expansion rewarding;

bills manageable;

midgame coherent;

late progression reachable;

collection earned;

rare discovery satisfying.

93. FINAL ACCEPTANCE RUN

Before declaring V6 complete:

sync latest known-good;

compile;

run all tests;

start fresh save;

play opening manually;

clean multiple specimens;

inspect multiple specimens;

use magnifier;

use hammer;

use cracker;

use saw;

use lap where appropriate;

make dealer sale;

make customer sale;

complete physical checkout;

order multiple crates;

expand shop;

pay bills;

place/move equipment;

save;

reload;

verify collection;

verify provenance;

stress customers;

test controller;

test KBM;

test rebinding;

run UI render QA;

run collision/placement audit;

profile representative scenes;

build standalone;

smoke standalone;

capture final screenshots;

compare to references;

fix remaining obvious defects;

rerun affected tests;

commit;

push.

Only then can V6 be declared complete.

94. FINAL REPORT

Create:

Docs/V6Final/FINAL_REPORT.md

Include:

milestone summary;

commit list;

automated test results;

specimen/contact-sheet findings;

customer stress;

fresh-career summary;

economy findings;

performance findings;

controller/KBM;

persistence/migration;

standalone result;

screenshot index;

known P2 issues;

complete V6 Definition-of-Done checklist.

Do not hide unresolved P0/P1 issues.

95. FAILURE MODES

Do not:

add data fields and call specimen gameplay deep;

create palette swaps and call them diversity;

make all processing methods equivalent;

let a rare tag replace actual rarity;

hide performance hitches behind animations;

rely on recovery teleport to “pass” navigation;

re-populate collection with fake rocks;

re-enlarge the HUD;

make giant grey tablet pages;

reopen completed architecture without evidence;

postpone true V6 defects to V7;

optimize for finishing quickly.

96. QUALITY BAR

A successful V6 should allow a player to:

launch Geode Empire, begin in a believable tiny rented workshop, receive rough rocks, physically clean and inspect them, form predictions, choose processing methods, experience varied and convincing reveals, understand why specimens have value, sell to a dealer or real customers, complete physical checkout, expand the premises, pay operating costs, buy better equipment, build a genuine collection, and reach a mature rock shop — without obvious prototype logic, broken progression, intrusive UI, major clipping, repetitive specimens, or visible interaction hitches.

That is the bar.

97. FINAL EXECUTION RULE

Work autonomously until the full V6 Production Alpha genuinely passes.

Use:

Unity observation continuously;

Blender seriously;

Play Mode;

deterministic tests;

contact sheets;

screenshots;

comparison captures;

collision audits;

placement tests;

customer stress;

persistence tests;

controller tests;

KBM tests;

fresh-save career;

performance instrumentation;

standalone builds.

Keep origin/main safe.

Use clean known-good milestone commits.

Push verified milestones.

No force pushes.
No history rewrites.

Do not lower the quality bar because the run is long.

The actual playable game is the acceptance criterion.

98. COMPLETION DECLARATION

The phrase:

V6 PRODUCTION ALPHA COMPLETE

may only be used when:

every applicable gate above passes;

the full fresh-career run succeeds;

the standalone succeeds;

no known P0/P1 defects remain;

final screenshots withstand critical review;

all verified work is committed and pushed.

If any of those are false:

KEEP WORKING.