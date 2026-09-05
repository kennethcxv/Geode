GEODE EMPIRE — STARTER SHOP, PROGRESSION, UI & VISUAL COHESION REBUILD

Authoritative pre-V6 continuation override for Opus 5

Purpose: Correct the current early-game presentation, progression staging, HUD/UI scale, collection defaults, environment quality, and spatial progression before continuing broader V6 work.

Primary acceptance rule: The actual playable game is the acceptance criterion.

Reference authority: The images in Geode/references and Geode/refrences (if that misspelled folder exists) define the target visual language, composition, UI quality, simulator tone, specimen presentation, and overall polish.

Gameplay authority: Existing Geode systems, saves, specimen identity, provenance, checkout, customer flow, career progression, controller/KBM support, and known-good V5/V6 behavior must be preserved unless this document explicitly requires a structural correction.

0. EXECUTION MODE

PLAN FIRST, THEN EXECUTE IMMEDIATELY

Before making major changes:

Read this entire file.

Review every relevant reference image.

Launch the current game in Play Mode.

Capture the current Day-1 starter experience from natural player camera positions.

Audit:

starter shop footprint;

currently visible rooms/areas;

installed equipment;

collection/display defaults;

HUD scale;

tutorial/prompt scale;

tablet screens;

specimen presentation;

lighting/materials;

player/customer routes;

build/placement logic;

progression state;

save/load behavior.

Write a prioritized execution plan to Docs/StarterRebuild/PLAN.md.

Immediately execute the plan yourself.

Do not stop to ask for approval.

Revise the plan whenever Play Mode observation proves an assumption wrong.

The plan is a working document, not the final deliverable.

1. MISSION

The current game has improved substantially, but the Day-1 experience still feels too large, too finished, too visually spoiled, and too UI-heavy.

This pass must make Geode begin as a small, believable starter operation that visibly grows into the more impressive reference-image business over time.

The player should feel:

“I started with almost nothing. I built this workshop. I chose where the machines went. I earned the showroom. I filled the shelves. I created the collection.”

The game should not feel like:

“I started inside the completed business and the game slowly turns things on.”

This pass must correct that difference.

2. NON-NEGOTIABLE HIGH-LEVEL OUTCOME

By the end of this phase:

Day 1 must feel materially smaller than the current build.

The starter business must have only the minimum believable space and fixtures needed for the opening loop.

Later rooms, displays, machines, storage, showroom features, and premium fixtures must not be fully present from the start.

Progression must be visible in the actual world.

Player ownership and placement must matter.

Collection and showcase spaces must not pretend the player owns specimens they have not discovered.

The HUD must be materially smaller and less intrusive.

Tutorial/prompt UI must stop dominating the screen.

Tablet screens must look like polished simulator UI, not large grey admin panels.

Buying, sourcing, upgrades, collection, and career screens must be visually stronger and easier to understand.

The actual 3D environment must look more polished, deliberate, compact, and premium.

The references should feel like later states of the same game, not unrelated concept art.

3. CORE DESIGN PRINCIPLE — SMALL START, VISIBLE GROWTH

The starter shop should be compact, functional, and intentionally modest.

It should not look unfinished in a bad way.

It should look like:

a small workshop;

a small receiving corner;

one or two core work surfaces;

basic starter storage;

minimal retail capability;

clear space for future expansion.

The player should see obvious potential for growth without already seeing the finished business.

Preferred progression fantasy

Day 1

small workshop

basic bench

basic wash/cleaning

starter storage

first receiving spot

minimal retail or no formal showroom yet

no premium collection gallery

no full late-game office

no advanced machine lineup

Early progression

player earns first meaningful equipment

first additional display/storage appears

first stronger workstation is purchased and placed

more floor/wall area becomes usable

Midgame

powered processing

improved storage

proper retail zone

better checkout

dedicated appraisal / office capability

showroom grows

Late game

mature retail space

premium collection

polished office

advanced machines

upgraded lighting

expanded receiving/storage

prestige displays

exhibition/endgame spaces

The environment should visibly tell the career story.

4. STARTER FOOTPRINT REBUILD

4.1 Day-1 must be significantly smaller

The current starter footprint is too large and exposes too much of the mature business.

Reduce it.

Possible solutions include:

physically smaller initial playable room;

temporary construction walls;

closed shutters/doors;

blocked expansion bays;

unfinished adjacent rooms;

compact initial floorplan with later unlockable extensions;

modular room expansion.

Choose the best architecture for the game.

Do not simply hide finished rooms behind an interaction lock if the player can still see the final business everywhere.

4.2 Mature areas must not visually spoil progression

At Day 1, the player should generally not have full visual access to:

mature showroom;

large private collection;

premium display runs;

advanced storage wing;

complete office;

advanced lap/saw/cracker area;

full-feature checkout;

extensive finished receiving space;

future expansion furniture.

If the architecture requires these spaces to physically exist, they should read as:

closed;

unfinished;

dark;

under construction;

shuttered;

inaccessible;

clearly not yet part of the business.

They should not look finished and merely disabled.

4.3 Expansion should feel earned

Expansion should happen because the player:

reaches a career gate;

buys an upgrade;

opens a room;

places new equipment;

invests in fixtures;

improves the shop.

The environment should change immediately and visibly.

5. EQUIPMENT PRESENCE & WORLD-STATE RULES

5.1 Locked equipment must generally be absent

If the player has not unlocked/purchased a machine, do not place the finished usable machine in the shop by default.

Examples:

geode cracker;

diamond saw;

lap/polishing station;

premium inspection tools;

upgraded storage;

premium display cabinets;

advanced checkout fixtures;

specialty processing equipment.

The preferred flow is:

career unlock;

purchase;

receiving/delivery;

build/placement mode;

valid player placement;

activation;

save/persistence.

5.2 Do not use “covered final machine” as the default solution

A covered or placeholder object can be used only when it makes sense visually and narratively.

Do not fill the starting shop with:

cloth-covered future machines;

locked final fixtures;

inactive finished workstations;

obvious “coming soon” content.

That still spoils the growth fantasy.

5.3 Starter equipment should look starter-grade

Starter equipment may be:

smaller;

older;

more manual;

less efficient;

simpler in construction.

Later equipment should feel visibly better.

6. COLLECTION / SHOWCASE / DISPLAY OWNERSHIP

This is a major current issue.

6.1 Player collection must begin empty

Do not display player-owned geodes, gems, specimens, agates, crystals, or showcase pieces before the player actually earns/discovers/keeps them.

No fake collection progress.

6.2 Empty displays should look intentionally empty

An empty display should feel like:

a future goal;

a clean showcase waiting to be filled;

a meaningful empty slot.

It should not feel broken or forgotten.

Use:

empty risers;

labels/slots;

soft lighting;

tasteful empty stands;

subtle silhouettes/placeholders only if they clearly communicate “not discovered.”

Do not use real specimen models as decorative filler.

6.3 Public retail decoration vs player collection

If a public shop area needs visual decoration, distinguish clearly between:

decorative business-owned stock;

customer-facing merchandise;

the player’s personal collection.

Do not let decorative rocks imply false progression.

6.4 Collection UI

Undiscovered entries should not reveal the real specimen too early unless the design intentionally allows silhouette hints.

Prefer:

obscured silhouette;

empty plate;

iconographic hint;

source clue;

category hint.

The player should feel discovery.

7. SHOWROOM / RETAIL PROGRESSION

The showroom should not begin as a mature, fully stocked rock shop.

7.1 Early retail

small;

sparse;

limited capacity;

believable for a new business.

7.2 Midgame retail

additional shelving;

better lighting;

more display capacity;

improved checkout;

better merchandising.

7.3 Late retail

Late game should resemble the strongest reference images:

premium display runs;

impressive geodes;

curated lighting;

stronger signage;

upgraded checkout;

collector/showcase zones;

prestige feel.

7.4 Stock must be real

Retail shelves should show actual sellable stock from the game.

Do not fill shelves with fake specimens unless explicitly decorative and clearly not inventory.

8. HUD REBUILD — MAKE IT MUCH SMALLER

The current HUD is too large and visually dominant.

This is a hard correction.

8.1 Top-left objective panel

Current issues:

too wide;

too tall;

too much padding;

too much text always visible;

dominates the scene.

Target:

compact objective summary;

only the most important current objective;

secondary details can expand on demand;

tighter typography;

smaller brand badge;

smaller padding.

Consider:

collapsed default state;

expandable details;

progress chips;

concise “Next” line.

8.2 Top-right cash/time/level panel

Current issues:

oversized;

too much dead space;

too large for basic status information.

Target:

compact cash/time/level cluster;

thinner XP bar;

smaller padding;

lower visual weight.

8.3 Bottom control rail

Current issues:

too large;

too much horizontal occupation;

always present with too many controls.

Target:

only contextually important controls;

smaller keycaps;

less spacing;

fade/auto-hide where appropriate;

avoid permanent redundant controls.

8.4 Tutorial / narration banner

Current issues:

too tall;

covers the lower scene;

can visually collide with interaction prompts.

Target:

significantly smaller;

tighter text;

one or two lines when possible;

context-sensitive placement;

fade after acknowledgement;

no stacking with large interaction cards.

8.5 Interaction prompt

Target:

smaller;

closer to crosshair;

concise verb + object;

minimal box;

clean glyph.

8.6 HUD layering

Never allow objective panel + tutorial + interaction + customer notification + status panel to all dominate simultaneously.

Create clear priority rules.

9. TABLET / MANAGEMENT UI REBUILD

The tablet currently functions, but it still looks too much like large grey software panels.

It should feel like a polished simulator management interface.

9.1 Global tablet structure

Improve:

overall spacing;

density;

tab hierarchy;

typography;

borders;

card depth;

selected states;

empty states;

icons;

specimen imagery;

price/status emphasis;

scroll behavior.

Avoid:

giant empty rectangles;

excessive dark-grey slab surfaces;

large unused margins;

repeated identical cards with weak hierarchy.

9.2 Suppliers / ordering UI

This screen must answer quickly:

What am I buying?

How much does it cost?

What is the likely quality?

What is the risk?

How many rocks?

Where does it arrive?

How much storage does it use?

What does it unlock/progress?

Improve:

crate art/iconography;

source identity;

clearer locked state;

stronger button design;

better price emphasis;

“till after purchase” without clutter;

risk/quality chips;

destination/storage readout;

progression requirements.

The player should understand a purchase in seconds.

9.3 Upgrades UI

This is one of the weakest current screens and must be substantially improved.

Current problem:

large grey cards;

generic purple dot icons;

weak visual representation of what is being bought.

Upgrade cards should show:

actual icon / rendered asset / silhouette;

name;

price;

status;

category;

short benefit;

unlock condition;

whether it changes world geometry;

where it will be delivered/placed.

The detail card should show:

a real preview image when possible;

“what it changes”;

physical/world effect;

workflow effect;

purchase status;

placement requirement.

Do not use identical placeholder circles for everything.

9.4 Collection UI

Improve:

specimen cards;

undiscovered state;

category filtering;

rarity;

best value;

largest find;

source/history;

physical collection placement status.

Make discovered specimens feel rewarding.

9.5 Stats / career UI

Avoid giant flat panels.

Prefer:

compact metric groups;

small trend charts if useful;

milestones;

career summary;

notable records;

next goal.

9.6 Career / objective UI

Show:

what matters now;

why it matters;

what unlocks next.

Do not overwhelm with every future objective.

10. WORLD ART / QUALITY CORRECTION

10.1 Current visual problems to address

The current world still shows:

repetitive dark wood;

overly orange task lighting;

empty floor volume;

overly large open space;

flat/placeholder areas;

uneven visual quality;

props that read as utility geometry rather than authored set dressing;

some machine/workstation silhouettes that remain weak.

10.2 Material variety

Introduce believable separation between:

walls;

floor;

counters;

storage;

industrial metal;

painted metal;

rubber;

glass;

wood;

stone;

fabric.

Do not make everything brown/black/orange.

10.3 Lighting

Target:

warm task lighting;

cooler ambient separation;

controlled highlights;

no blown-out specimen displays;

stronger contrast;

clean visibility.

Different zones should feel distinct.

10.4 Prop density

Add only meaningful props.

Do not solve blandness with random clutter.

Every prop should:

support function;

tell business history;

reinforce station purpose;

improve composition.

10.5 Geometry quality

Use Blender where:

proportions are wrong;

silhouettes are too boxy;

shelves/counters look cheap;

fixtures need unique identity;

current meshes hurt reference fidelity.

11. GEODE / SPECIMEN PRESENTATION

The specimens remain the hero.

Improve:

shell silhouette;

crystal depth;

rind;

cavity depth;

cut-face layering;

lighting response;

scale variety;

color control;

readability.

Avoid:

overexposed white specimens;

washed-out retail shelves;

every specimen looking equally bright;

fake neon color;

obvious repeated shapes.

Retail and collection lighting should reveal detail, not erase it.

12. BUILD MODE & PLAYER PLACEMENT

The player should meaningfully place later equipment and fixtures.

12.1 Valid placement requirements

Reject placement if the object:

intersects walls;

intersects floor/ceiling incorrectly;

overlaps fixtures;

blocks doors;

blocks required routes;

blocks customer routes;

blocks checkout;

blocks receiving;

blocks machine interaction;

blocks machine moving clearance;

blocks storage access;

makes an interactable unreachable.

12.2 Placement ghost

Use:

clear footprint;

valid/invalid state;

snapping where useful;

rotation;

distance/clearance visualization if helpful.

12.3 Protected paths

Maintain protected connectivity between:

entrance;

receiving;

workshop;

current stations;

retail;

checkout;

customer route;

office/tablet;

collection;

storage.

12.4 Customer validation

A layout is not valid merely because NavMesh exists.

Actually test:
entrance → browse → choose → queue → checkout → exit

Repeat under multiple layouts.

13. COLLISION / CLIPPING / ANCHOR INTEGRITY

Zero tolerance for obvious visible clipping.

Audit:

signs;

trays;

shelves;

pallets;

pegboards;

lamps;

counters;

Blender props;

wall-mounted fixtures;

machines;

display cabinets;

POS devices;

crates;

buckets;

tools.

Fix:

wall penetration;

z-fighting;

hovering;

embedded props;

wrong pivots;

bad collider bounds.

14. STARTER SAVE / FRESH CAREER ACCEPTANCE TEST

Use a real fresh save.

Verify:

Day-1 business is compact.

Later rooms/features are not visually spoiled.

Premium collection is empty/not yet present.

Later machines are not installed.

Starter loop works.

Player buys first crate.

Receiving works.

Player cleans/inspects/processes first rock.

Player reaches first upgrade.

Upgrade is purchased.

Physical change appears.

Player places equipment where required.

Save/reload preserves it.

Business looks visibly more developed than Day 1.

Capture:

Day-1 baseline;

first expansion;

midgame snapshot;

later premium state.

The difference must be obvious.

15. UI QA

Test at minimum:

1920×1080;

2560×1440;

3840×2160;

normal UI scale;

larger UI scale.

Verify:

no overlap;

no clipped buttons;

no giant panels;

no tablet/HUD collision;

no tutorial/interact collision;

no missing focus;

controller navigation works;

KBM works.

16. CUSTOMER / RETAIL QA

Run repeated customers.

Verify:

entrance;

browse;

purchase selection;

queue;

checkout;

handoff;

exit.

Test:

starter shop;

partially expanded shop;

mature shop.

No shelf or fixture may create jams.

17. PERSISTENCE

Save/load after:

buying equipment;

placing equipment;

expanding room;

moving fixture;

adding collection piece;

changing retail stock.

Verify:

no duplicates;

no missing objects;

no moved equipment resets;

no fake default collection returns;

no progression rollback.

18. REGRESSION PROTECTION

Do not break:

checkout;

specimen identity;

provenance;

economy;

customer sale flow;

processing;

controller;

KBM;

save migration;

Stage 2/3;

existing V6 systems;

tutorial;

rebinding.

Run automated tests after major milestones.

19. REQUIRED WORKFLOW

For each major area:

capture current state;

open relevant reference;

write harsh critique;

implement;

capture;

compare;

reject if still weak;

iterate.

Do not declare success from code alone.

20. FAILURE MODES

Do not:

shrink only the HUD while leaving the starter world too large;

keep late-game machines visible because hiding them is inconvenient;

decorate empty collection spaces with fake player specimens;

use placeholder icons in polished upgrade UI;

preserve giant grey tablet panels;

solve progression with lock icons only;

make the starter shop visually empty in a bad way;

create massive rooms simply to avoid collisions;

let customer navigation degrade;

fake screenshots;

hide defects from the camera;

stop after one pass;

call “improved” the same as “done.”

21. PRIORITY ORDER

Work in this order unless direct observation proves a different dependency:

fresh-save starter-state audit;

starter footprint;

physical progression gating;

collection/showcase ownership defaults;

build/placement integration;

HUD size reduction;

tablet UI;

world composition;

materials/lighting;

specimens;

customer/placement validation;

persistence;

UI render QA;

full fresh-career verification.

Do not spend hours polishing late-game props before the Day-1 structure is correct.

22. DEFINITION OF DONE

This pass is complete only when all of the following are true.

Starter state

Day-1 shop is substantially smaller than the current build.

Day-1 feels intentional, not unfinished.

Mature rooms do not visually spoil progression.

Later machines are not preinstalled.

The player has believable room to grow.

Progression

Unlocks physically change the business.

Purchased machines/fixtures appear only when earned.

Player placement is used where intended.

Early, mid, and late shop states look meaningfully different.

Collection

Player collection starts empty.

No fake decorative player-owned specimens.

Empty collection displays read cleanly.

Collection UI respects discovery.

HUD

Top-left panel materially smaller.

Top-right status materially smaller.

Bottom key rail materially smaller/contextual.

Tutorial banner materially smaller.

Interaction prompts smaller and cleaner.

3D game has significantly more unobstructed screen space.

Tablet

Suppliers looks polished.

Upgrades has real visual identity and meaningful previews.

Collection looks rewarding.

Stats looks designed rather than flat.

No giant dead-space panels.

Controller + KBM navigation both work.

World quality

Environment feels compact, deliberate, and cohesive.

Materials have believable variation.

Lighting is controlled.

Props support function/story.

Weak placeholder assets replaced where necessary.

Specimens remain hero content.

Placement / collision

Invalid overlaps rejected.

Door blocking rejected.

Customer route blocking rejected.

Machine clearance blocking rejected.

No obvious sign/tray/shelf/prop clipping.

Authored default layout passes the same validation standards.

QA

Fresh-save starter loop works.

First upgrade physically changes the shop.

Save/load preserves the new state.

Customer flow succeeds.

UI render QA passes.

Automated tests green.

Side-by-side captures reviewed.

Final result is clearly closer to the references.

If any major box above is false, this phase is not complete.

23. FINAL INSTRUCTION

Take the time required.

Do not optimize for speed.

Optimize for:

a stronger game;

a much better first impression;

a satisfying small-to-large business arc;

clean readable UI;

real ownership;

believable physical progression;

safe player placement;

strong specimen presentation;

polished simulator quality.

Use Unity observation continuously.
Use Blender seriously when needed.
Play the game yourself.
Use screenshots.
Use before/after comparisons.
Use customer stress tests.
Use collision audits.
Use persistence tests.
Use controller and KBM tests.
Use fresh-save progression tests.
Use milestone commits.
Keep origin/main safe.
No force pushes.
No history rewrites.

Do not resume the broader V6 roadmap until this phase genuinely passes its full Definition of Done.

The actual playable game is the acceptance criterion.