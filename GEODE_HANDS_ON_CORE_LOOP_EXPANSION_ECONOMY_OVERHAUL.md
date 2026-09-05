GEODE EMPIRE — HANDS-ON CORE LOOP, SHOP EXPANSION, OPERATING COSTS & RESPONSIVENESS OVERHAUL

Authoritative pre-V6 continuation specification for Opus 5

Status: This document temporarily overrides normal V6 work.
Primary acceptance criterion: The actual playable game is the acceptance criterion.
Execution model: Audit → plan → implement → play → measure → compare → reject/fix → verify → commit.
Do not resume broader V6 work until every applicable Definition-of-Done gate in this document genuinely passes.

0. WHY THIS PASS EXISTS

Recent hands-on playtesting exposed a core design problem:

Geode currently contains many strong systems, but several of the most important interactions still skip over the physical simulator fantasy.

Examples:

inspection can become “place rock down and receive information”;

washing can become “press E and the specimen becomes clean”;

cracking can hitch at the most important reward moment;

discovery/record notifications can interrupt the player too aggressively;

receiving can allow overlapping crates;

tutorial guidance can point to an area instead of the exact required object;

the shop needs a stronger small-business expansion arc;

operating a larger business should have recurring costs;

early retail should exist before the mature showroom;

the player should remember doing tasks manually before buying equipment that makes them easier.

This pass must correct those issues at the system/design level, not just patch individual symptoms.

1. CORE GAMEPLAY PHILOSOPHY

Geode should be a hands-on simulator where progression moves the player from:

manual skill → improved tools → assisted workflow → professional machinery → mature business

The player should learn each process before automation reduces effort.

The fundamental rule is:

Every repeated physical step must reveal information, require skill, create anticipation, change the outcome, or deliver satisfaction. If it does none of these, simplify or automate it.

Do not replace meaningful gameplay with one-button solutions.

2. EXECUTION MODE

2.1 Read everything first

Before major implementation:

Read this full document.

Read relevant V6 sections.

Review the current fresh-save game in Play Mode.

Review relevant reference images in:

Geode/references

Geode/refrences if present.

Inspect the existing:

inspection systems;

washing/cleaning systems;

specimen manipulation;

hammer/chisel;

fracture/reveal pipeline;

discovery notifications;

receiving/crate placement;

tutorial beacon targeting;

customer/retail systems;

shop expansion/premises systems;

economy;

save architecture;

tablet/HUD;

audio;

performance instrumentation.

2.2 Write the plan

Create:

Docs/CoreLoopOverhaul/PLAN.md

The plan must include:

current-state findings;

root causes;

dependencies;

milestone order;

acceptance tests;

likely Blender work;

likely Unity runtime work;

performance risks;

save migration risks.

2.3 Then execute immediately

Do not stop after planning.

Do not wait for user approval between milestones.

Revise the plan if direct Play Mode evidence proves an assumption wrong.

3. SAFE CHECKPOINT

Before structural changes:

ensure project compiles;

run the current automated suite;

capture baseline performance around cracking;

capture baseline screenshots/videos of inspection, washing, cracking, receiving, retail, HUD;

create a clean milestone commit;

push if appropriate;

record the checkpoint hash in Docs/CoreLoopOverhaul/PLAN.md.

No force pushes.
No history rewrites.

4. STARTER EXPERIENCE — MANUAL FIRST

The opening hours must feel small, tactile, and under-equipped in a good way.

4.1 Day-1 starter tools

The starter player should have only what is needed for the manual loop:

hammer;

chisel;

simple workbench;

hand magnifying glass / loupe;

simple light source;

manual washing setup;

minimal receiving/drop area;

minimal storage;

minimal sale/display capability;

basic dealer access;

basic customer retail path if legitimate stock is available.

4.2 Do not start with advanced equipment

Do not install the following at Day 1 unless the current design has a very specific justified exception:

powered washer;

geode cracker;

diamond saw;

lap/polisher;

advanced inspection machine;

premium checkout;

mature showroom;

premium display runs;

private collection gallery;

advanced storage;

high-end office.

4.3 Manual first, automation later

Progression should feel like:

Starter

inspect by hand;

wash by hand;

crack by hammer/chisel;

sell directly;

tiny retail capacity.

Early upgrades

better loupe;

brighter inspection lamp;

better cleaning tools;

better storage;

basic dedicated sink/wash station;

better starter display.

Midgame

cracker;

powered wash assistance;

better appraisal tools;

larger retail area;

better checkout;

first shop expansion.

Later

saw;

lap/polishing;

advanced inspection equipment;

premium retail;

larger expansion;

high-value sourcing;

collection/gallery;

advanced business tools.

The player should feel they earned convenience.

5. HANDS-ON INSPECTION REBUILD

Inspection must become a real interaction.

5.1 Current problem

Do not allow the core inspection loop to become:

place specimen → press button → receive complete information

That is too abstract for the game’s core fantasy.

5.2 Starter inspection interaction

The player should:

pick up the specimen;

place it on or hold it near an inspection surface;

rotate it freely;

use a magnifying glass/loupe;

inspect specific visible regions;

identify surface evidence;

optionally tap/listen;

form a prediction.

5.3 Magnifying glass

Implement a usable physical magnifier.

Requirements:

works in first-person;

can be moved over the specimen;

visually magnifies the surface;

remains readable without severe distortion;

respects correct depth/focus;

works with controller and mouse;

does not clip badly;

can be put away quickly;

does not reveal hidden information automatically.

5.4 Surface observations

Inspection should reveal clues, not answers.

Possible observations:

thick rind;

thin rind;

iron staining;

weathering;

pits;

exposed quartz;

banding;

visible cavity opening;

possible seam;

dense shell;

fragile fracture line;

unusual texture;

secondary mineral staining;

partial crystal exposure;

mud/clay obscuring detail.

5.5 Clue discovery model

Prefer a model where observations become available because the player actually looks at the relevant area.

A clue can be:

undiscovered;

seen but not logged;

logged;

interpreted with confidence.

Do not show all clues globally by default.

5.6 Predictions

Allow the player to form a hypothesis from evidence.

The game should support:

observe → infer → choose process → reveal → learn

The system should never guarantee the exact mineral from basic exterior inspection.

5.7 Inspection upgrades

Later upgrades can include:

better loupe;

articulated inspection light;

digital scale;

calipers / dimensions;

improved acoustic/tap tool;

UV light if appropriate;

macro camera;

microscope;

assisted observation logging;

automated measurement;

more precise appraisal tools.

Upgrades should:

reduce uncertainty;

reduce manual effort;

improve speed;

improve consistency.

They should not invalidate the manual starter gameplay.

6. TRUE 360° SPECIMEN MANIPULATION

Current manipulation must support genuine multi-axis inspection.

6.1 Requirements

The player must be able to rotate a specimen through:

yaw;

pitch;

roll;

full continuous 360° orientation.

They must be able to inspect:

top;

bottom;

sides;

openings;

rind;

fracture areas;

cut faces.

6.2 Interaction quality

Use robust orientation handling.

Avoid:

one-axis product-viewer rotation;

gimbal-lock behavior;

sudden flipping;

camera clipping;

drift;

unusable large-specimen movement.

Support:

mouse;

controller;

reset orientation;

optional gentle inertia;

optional fine-control mode.

6.3 Test matrix

Test:

small specimen;

medium specimen;

large specimen;

irregular specimen;

opened specimen;

saw-cut specimen;

heavy specimen.

7. HANDS-ON WASHING REBUILD

Washing must become physical gameplay.

7.1 Current problem

Do not allow:

press E → specimen globally becomes clean

That skips the entire cleaning fantasy.

7.2 Starter cleaning setup

The early player should use a simple manual setup such as:

basin;

bucket;

utility sink;

basic running water;

sponge;

cloth;

soft brush.

No powered washer at the start.

7.3 Spatial dirt model

A specimen should have dirt distributed across actual regions.

Requirements:

dirt exists spatially;

one side can remain dirty while another is clean;

cleaning progress is visible;

missed spots remain;

dirt can obscure inspection clues;

the player must rotate the specimen to finish cleaning.

The implementation does not need absurd physical simulation, but it must feel spatially real.

7.4 Cleaning interaction

The player should:

wet the specimen;

rotate it;

locate dirty areas;

rub/brush;

see dirt reduce where contacted;

rinse;

inspect missed spots;

complete once sufficiently clean.

7.5 Skill / care

Cleaning should reward care.

Potential outcomes:

careful cleaning preserves fragile surfaces;

careless aggressive brushing on delicate exposed material can cause minor damage;

overpressure can be risky in appropriate cases;

ordinary normal cleaning should not feel unfairly destructive.

7.6 Cleaning time target

Do not make washing tedious.

A normal manual clean should be short and satisfying.

More difficult specimens can justify more work.

7.7 Washing upgrades

Progression can include:

better brushes;

better nozzle;

better basin;

improved sink;

adjustable pressure;

holding fixture;

powered assistance;

delicate-mode cleaning;

faster drying;

improved drainage.

Progression should feel like:

manual → improved manual → assisted → professional

7.8 Physical station quality

The wash station must:

face the player correctly;

not face a wall;

have sensible reach;

have correct plumbing/fixture orientation;

have clear working space;

have no clipping;

look good in first-person.

Use Blender if the workstation itself is geometrically weak.

8. HAMMER + CHISEL — STARTER HERO INTERACTION

Hammer and chisel should be one of the most satisfying early interactions.

8.1 Improve physical feel

Improve:

aiming;

chisel placement;

strike timing;

hand/tool movement;

hit reactions;

small chips;

dust;

rock micro-movement;

fracture buildup;

visual feedback;

audio feedback.

8.2 Good strike vs bad strike

The player should learn what a good strike feels/sounds like.

Good hit

Use:

believable metal-on-stone transient;

subtle resonance;

clear but non-arcade confirmation;

stronger fracture response.

Neutral/bad hit

Use physically different:

duller impact;

reduced resonance;

weaker fracture response.

Do not use cartoon “success” effects.

9. BREAK / FRACTURE AUDIO REBUILD

The current break sound needs a serious pass.

9.1 Layered fracture audio

Build the sound from multiple layers where appropriate:

tool impact;

crack onset;

rock stress;

major split;

chips/debris;

piece contact;

settling.

9.2 Variation

Avoid repeated identical audio.

Use appropriate:

sample variation;

slight pitch variation;

size-dependent response;

material-dependent response;

different balance for hammer/cracker/saw.

9.3 Test

Test:

small;

medium;

large;

dense;

hollow;

hammer;

cracker;

saw separation.

10. FRACTURE / REVEAL RESPONSIVENESS

The crack/reveal moment is one of the most important moments in the game.

It must not visibly hitch.

10.1 Instrument the pipeline

Measure frame times around:

final strike;

fracture threshold;

result generation;

mesh creation;

crystal setup;

physics spawn;

material creation;

VFX;

audio;

appraisal;

discovery registration;

thumbnail generation;

save operations;

UI creation;

notification queue;

collection update.

Do not guess.

10.2 Separate critical from noncritical work

Critical:

immediate physical fracture;

audio response;

visible piece separation;

first readable interior frame.

Noncritical:

thumbnail rendering;

history logging;

long metadata calculations;

collection UI refresh;

save serialization;

record comparison;

analytics/debug bookkeeping.

Defer noncritical work when safe.

10.3 Likely optimization strategies

Where appropriate:

precompute deterministic outcome data;

prewarm materials/shaders;

pool result pieces;

pool VFX;

avoid one-frame allocations;

cache reusable geometry/material data;

move thumbnail capture later;

defer save;

avoid synchronous disk writes;

budget crystal setup;

avoid instantiating excessive unique materials;

stage expensive operations over frames where this does not affect correctness.

10.4 Acceptance target

The player should feel:

final strike → immediate crack response

No noticeable frozen pause before the geode reacts.

11. CRACK / OPENING ANIMATION REBUILD

Do not let opening feel like:

a teleport;

a hinge;

a book opening;

a scripted slide;

an abrupt pop.

Preferred sequence:

impact → micro-fracture response → separation → pieces clear → gravity → bounce/settle → interior readable

11.1 Requirements

no interpenetrating halves;

no sudden freeze;

no long dead delay;

no camera losing the result;

no pieces flying unrealistically;

no clipping through workbench/machine.

Use natural physics where appropriate, but keep the reveal readable.

12. DISCOVERY / RARE / RECORD PRESENTATION

Current notification behavior must be more restrained and intentional.

12.1 Notification hierarchy

Tier 1 — routine

Examples:

normal new mineral;

small value improvement;

ordinary collection update.

Presentation:

small;

nonblocking;

short;

can queue quietly.

Tier 2 — meaningful

Examples:

genuinely new family;

rare variant;

significant quality milestone.

Presentation:

medium;

brief;

polished;

does not steal control for long.

Tier 3 — exceptional

Examples:

extraordinary rarity;

major personal record;

major career milestone.

Presentation:

larger;

rare;

celebratory;

worth interrupting for briefly.

12.2 Rate limiting

Do not allow:

stacked giant notifications;

repeated record popups for trivial early values;

multiple full-screen cards in sequence;

new-game spam.

Use:

significance thresholds;

queueing;

cooldown/rate limit;

combine related events where appropriate.

12.3 Reveal timing

The notification should not cause the crack hitch.

The physical result comes first.

Then the UI celebrates it.

13. TUTORIAL BEACON ACCURACY

Tutorial guidance must target the exact required runtime object.

13.1 Current issue

Do not point to:

approximate room coordinates;

general area;

stale authored positions.

13.2 Semantic targeting

Prefer:

Tutorial step → semantic target ID → current runtime Transform/anchor

Examples:

hammer;

chisel;

crate;

exact dirty spot tool;

wash brush;

dealer box;

receiving slot;

specific UI control;

exact machine interaction anchor.

13.3 Player-placed equipment

The beacon must continue to work after:

player moves equipment;

save/load;

room expansion;

layout changes.

13.4 Offscreen guidance

When target is offscreen:

directional chevron;

distance if useful;

no misleading center-screen pointer.

Test every tutorial step from a fresh save.

14. RECEIVING / MULTIPLE CRATE COLLISION

Crates must never spawn into each other.

14.1 Receiving slots

Use real capacity/slot logic.

Each crate needs:

bounds;

valid receiving slot;

collision-safe placement;

clear spacing.

14.2 Capacity

If receiving is full:

prevent order;

warn before purchase;

queue future delivery;

or require expansion.

Do not silently overlap crates.

14.3 Physical rules

No:

overlapping crates;

wall penetration;

pallet penetration;

impossible stacks;

crates spawning inside player/fixture.

15. EARLY CUSTOMERS & RETAIL

Customers should be part of the business fantasy before the mature showroom.

15.1 Early retail

Once the player has a legitimate item for sale, they should be able to receive customers.

Starter retail can be:

one small display;

one table;

one small shelf;

limited capacity;

simple checkout/payment setup.

15.2 Progression

Later upgrades increase:

display capacity;

customer traffic;

customer budgets;

reputation;

merchandising;

checkout sophistication;

floor area;

sales conversion.

The fantasy:

process rock → put it up for sale → customer may buy it

should exist early.

15.3 Customer testing

Verify:

enter;

browse;

select;

queue;

checkout;

receive purchase;

exit.

At:

starter state;

first expansion;

mature state.

16. SHOP EXPANSION SYSTEM

The business must physically expand.

16.1 Expansion structure

Use the current premises/lease foundation and turn it into meaningful player-facing progression.

Possible sequence:

Unit A — starter workshop

tiny;

manual tools;

minimal receiving;

minimal retail.

Expansion 1

additional work/storage bay.

Expansion 2

dedicated storefront/showroom.

Expansion 3

processing/back-of-house.

Expansion 4

premium collection/gallery/office.

The exact sequence should follow the real design.

16.2 Expansion requirements

Expansion can require:

deposit;

reputation;

career milestone;

build-out fee;

increased rent;

electrical upgrade;

water/plumbing upgrade;

construction/fit-out time.

16.3 Physical transformation

When the player expands:

hoarding/door/wall changes;

new space opens;

new placement grid becomes available;

new lighting/utilities activate;

furnishings are still placed/earned;

rent increases;

operating costs increase.

Expansion must be visible.

16.4 Strategic choice

Expansion should create a decision:

Can I afford the larger space, and will it generate enough value to justify the higher operating costs?

17. RENT, UTILITIES & OPERATING COSTS

Add recurring business expenses.

Keep them meaningful but understandable.

17.1 Rent

Rent should depend on:

current leased area;

unit tier;

expansion state.

Larger business = larger rent.

17.2 Electricity

Approximate meaningful usage from:

saw;

lap;

powered washer;

advanced inspection tools;

showroom lighting;

other powered equipment.

Do not over-simulate individual watts unless useful.

17.3 Water

Driven by:

washing;

powered wash;

cleaning activity.

17.4 Optional costs

Only add if they improve decisions:

maintenance;

insurance;

waste disposal;

shipping;

card fees;

equipment service.

Do not turn the game into accounting software.

17.5 Billing cadence

Choose a cadence that fits the game’s day length.

The player should always know:

current rent;

estimated utilities;

due date;

last bill;

current operating cost trend.

17.6 Payment

Do not silently remove large amounts.

Provide:

upcoming bill notice;

breakdown;

due date;

payment confirmation.

17.7 Failure to pay

Use graduated consequences.

Possible:

warning;

grace period;

late fee;

blocked expansion/order tier;

reputation/credit consequences;

lease pressure only after repeated failure.

Do not instantly softlock or destroy the save.

18. BUSINESS / BILLS UI

Add a polished management view.

Possible tablet sections:

PREMISES

current unit;

usable square meters;

next expansion;

deposit;

fit-out cost;

new rent;

requirements.

BILLS

rent;

electricity;

water;

maintenance;

total;

next due date;

previous bill;

estimated next bill.

OPERATING COSTS

cost per day/week;

recent change;

major cost drivers.

Use the current reference-style UI language.

Do not create a giant empty grey panel.

19. ECONOMY REBALANCE

New expenses require a real balance pass.

Do not just add rent/utilities on top of the old economy.

19.1 Desired outcomes

Day 1 survivable;

early customer sales matter;

dealer remains useful;

rent creates pressure but not punishment;

expansion timing matters;

machine ROI matters;

advanced machines cost more to operate;

higher capability can create greater profit;

no unavoidable bankruptcy spiral;

no infinite-profit exploit.

19.2 Simulations

Run multiple deterministic simulations for:

conservative player;

average player;

aggressive expansion;

slow expansion;

dealer-heavy;

retail-heavy;

unlucky specimen quality.

Also run at least one genuine fresh-save path.

20. WORLD / ART QUALITY PASS

While implementing these systems, improve the environments they touch.

20.1 Focus areas

starter workshop;

receiving;

inspection;

washing;

hammer/chisel bench;

first retail setup;

expansion thresholds/hoarding;

bills/premises UI.

20.2 Improve

layout;

proportions;

materials;

lighting;

signage;

plumbing;

electrical details;

tools;

storage;

safety items;

purposeful set dressing.

20.3 Do not over-clutter

Every prop should support:

function;

story;

readability;

composition.

Use Blender when the mesh quality is the limiting factor.

21. CORE AUDIO PASS

Improve audio around the hands-on loop.

At minimum:

magnifier handling if appropriate;

specimen handling;

brush;

sponge;

running water;

rinse;

good hit;

bad hit;

chisel strike;

crack onset;

fracture;

debris;

settling;

discovery notification;

rare reveal;

record;

customer/shop feedback;

bill/management feedback where appropriate.

Volume-match the set.

Avoid piercing UI sounds.

22. PERFORMANCE / RESPONSIVENESS PASS

Measure and fix visible hitches around:

specimen pickup;

magnifier activation;

360° manipulation;

dirt updates;

washing;

final strike;

fracture;

reveal;

discovery UI;

thumbnail capture;

save;

multiple crates;

customer spawn;

expansion activation.

Do not guess.

Record before/after numbers in:
Docs/CoreLoopOverhaul/PERFORMANCE.md

23. PERSISTENCE

Save/load must preserve:

shop expansion state;

lease state;

rent schedule;

utility/accounting state;

placed equipment;

moved equipment;

receiving queue;

specimen dirt state where relevant;

inspection observations;

collection discoveries;

customer/shop state already supported.

No:

duplicate bills;

duplicate equipment;

reset expansions;

reset rent date;

fake cleaned specimens;

lost observations.

24. CONTROLLER + KBM

Every new physical interaction must work with both.

Test:

magnifier;

specimen rotation;

brushing;

washing;

hammer/chisel;

expansion UI;

bills UI;

tutorial targets;

crate receiving;

retail.

No mouse-only escape hatch.

25. MILESTONE ORDER

Unless direct dependencies prove otherwise:

M1 — Baseline + instrumentation

checkpoint;

capture;

crack profiling.

M2 — 360° specimen handling

robust manipulation;

controller + KBM.

M3 — hands-on inspection

magnifier;

clues;

prediction.

M4 — hands-on washing

spatial dirt;

manual tools;

upgrades.

M5 — hammer/chisel + audio

impact feel;

good/bad hit;

fracture sound.

M6 — reveal performance + animation

eliminate hitch;

better split/settle.

M7 — notification hierarchy

discovery;

rare;

records;

rate limit.

M8 — tutorial targeting

semantic runtime anchors.

M9 — receiving capacity

multi-crate safe placement.

M10 — early retail

starter customer sales.

M11 — shop expansion

lease/premises;

physical growth.

M12 — rent/utilities

bills;

operating costs.

M13 — economy rebalance

simulations;

fresh career.

M14 — final world/art/audio polish

touched areas.

M15 — full regression

controller;

KBM;

persistence;

customers;

standalone;

final captures.

26. REQUIRED ACCEPTANCE TESTS

26.1 Starter state

small starter unit;

hammer/chisel present;

magnifier present;

manual cleaning present;

no washer;

no cracker;

no saw;

no lap;

limited retail.

26.2 Inspection

true 360° rotation;

magnifier works;

specific surface regions inspectable;

clues discovered through observation;

no complete automatic answer dump;

controller works;

KBM works.

26.3 Washing

spatial dirt;

cleaning affects contacted regions;

opposite side remains dirty until cleaned;

missed dirt visible;

manual wash works;

upgrade speeds/improves it;

no one-button global clean.

26.4 Hammer/crack

good hit feels better;

bad hit distinguishable;

fracture sound improved;

immediate final-hit response;

split animation believable;

no visible crack hitch.

26.5 Notifications

routine discoveries unobtrusive;

meaningful discoveries polished;

exceptional discoveries rare;

record spam eliminated;

no stacking giant popups.

26.6 Tutorial

exact hammer target;

exact chisel target;

exact wash tool;

exact crate;

exact dealer target;

follows player-placed equipment;

survives save/load.

26.7 Receiving

order multiple crates;

no overlap;

finite capacity;

full receiving handled correctly.

26.8 Early customers

legal item can be displayed early;

customer enters;

browses;

buys;

checkout completes;

customer exits.

26.9 Expansion

first expansion purchasable;

physical space changes;

new placement area opens;

rent increases;

save/load preserves expansion.

26.10 Bills

rent visible;

electricity visible;

water visible;

due date visible;

warning before payment;

no silent unfair charge;

late-payment path safe.

26.11 Economy

fresh game viable;

average player viable;

expansion not always immediate optimum;

expansion not impossible;

machine operating cost meaningful;

no unavoidable bankruptcy spiral.

27. REGRESSION PROTECTION

Do not break:

exact specimen identity;

provenance;

checkout;

customer handoff;

build mode;

placement validation;

customer navigation;

dealer;

sourcing;

career;

Stage 2;

Stage 3;

tutorial;

key rebinding;

controller;

KBM;

save migration;

existing V6 systems.

Run the full automated suite after every major milestone.

28. FAILURE MODES — DO NOT ACCEPT THESE

Do not:

make inspection a fancy UI wrapper around instant information;

make washing a progress bar disconnected from actual dirty regions;

let magnifier behave like a scanner that reveals hidden contents;

use fake 360° rotation that only spins one axis;

hide crack lag behind a longer animation;

move expensive work into another visible hitch;

spam discoveries;

let trivial early values trigger record celebrations;

let tutorial beacons use stale world coordinates;

allow crates to share a spawn transform;

reserve all customer retail for late game;

add rent without rebalancing income;

add utilities that are impossible to understand;

punish late payment with immediate softlock;

make expansion purely a menu number with no world change;

add random clutter and call it visual polish;

break controller support;

mark automated tests green while the interaction still feels bad.

29. EVIDENCE REQUIRED BEFORE COMPLETION

Provide:

baseline captures;

final captures;

crack performance before/after;

hands-on inspection capture;

magnifier capture;

360° manipulation proof;

dirty/partially clean/fully clean specimen captures;

hammer/chisel proof;

fracture/reveal proof;

notification hierarchy captures;

multi-crate receiving proof;

early customer retail proof;

starter-shop capture;

first-expansion capture;

later-shop capture;

premises/bills UI capture;

rent/utilities verification;

save/load proof;

controller proof;

automated test results;

customer stress results;

clean milestone commits.

30. DEFINITION OF DONE

This phase is complete only when all applicable boxes are genuinely true.

Hands-on core loop

inspection is physical;

magnifying glass works;

inspection reveals clues rather than answers;

specimen manipulation is true multi-axis 360°;

manual washing is physical;

dirt is spatial;

manual cleaning is required early;

cleaning upgrades improve the workflow;

starter game begins without washer;

hammer/chisel feel substantially better.

Crack/reveal

good strike sound improved;

fracture sound improved;

final strike responds immediately;

visible crack hitch eliminated or reduced below perceptible concern;

split animation is believable;

pieces settle correctly;

reveal remains readable.

Notifications

routine discoveries are nonblocking;

rare discoveries are special;

record presentation is meaningful;

notification spam eliminated;

queue/rate limiting works.

Tutorial

beacons point to exact runtime targets;

moved equipment remains correctly targeted;

save/load maintains target correctness.

Receiving

multiple crates never overlap;

receiving capacity is real;

full capacity has a clear behavior.

Retail

customers can participate early;

starter retail works;

mature retail remains an upgrade;

full customer loop works.

Expansion

shop can physically expand;

expansion costs money;

larger space visibly changes world;

player gains more usable placement area;

expansion persists.

Operating costs

rent exists;

electricity exists;

water exists;

costs are understandable;

due dates work;

payment warnings work;

failure-to-pay path is recoverable;

economy has been rebalanced.

Quality

touched environments improved;

core-loop audio improved;

performance measured;

no major interaction hitch;

controller works;

KBM works;

save/load works;

automated tests green;

real fresh-save playthrough completed;

actual Play Mode evidence supports completion.

If a box is false, keep working.

31. FINAL EXECUTION RULE

Take the time required.

Do not optimize for finishing quickly.

Do not stop after planning.

Do not solve this with superficial UI.

Actually:

pick up rocks;

rotate them;

inspect them;

use the magnifier;

find clues;

wash them;

clean specific dirt;

strike them;

listen to good and bad hits;

crack them;

measure the hitch;

watch the reveal;

trigger discoveries;

order several crates;

run customers;

expand the business;

pay rent/utilities;

save/reload;

test controller;

test KBM;

compare natural gameplay captures to the references.

Use Unity observation continuously.
Use Blender seriously where needed.
Use deterministic tests.
Use performance instrumentation.
Use screenshots/contact sheets where useful.
Use milestone commits.
Keep origin/main safe.
No force pushes.
No history rewrites.

Only after the full Definition of Done genuinely passes should you resume the broader V6 specification.

The actual playable game is the acceptance criterion.