GEODE EMPIRE — VISUAL REBUILD, REFERENCE MATCH, WORLD LAYOUT & PHYSICAL INTEGRITY OVERRIDE

Authoritative pre-V6-resume specification for Opus 5

Status: This phase overrides normal V6 work until complete.
Primary standard: The actual playable game is the acceptance criterion.
Visual authority: The provided reference images define the target art direction, composition, UI quality, lighting, specimen presentation, and simulator feel.
Gameplay authority: Existing Geode systems, progression, economy, provenance, saves, customer logic, and V6 design remain authoritative unless this specification explicitly requires a structural correction.

0. EXECUTION MODE

PLAN BEFORE MAJOR IMPLEMENTATION — BUT DO NOT STOP AFTER PLANNING

Before making large structural changes:

Inspect all reference images in:

Geode/references

Geode/refrences if it exists.

Inspect the actual current game in Play Mode from normal player camera positions.

Inspect the current workshop/shop hierarchy, prefabs, Blender sources, colliders, placement logic, unlock logic, navigation/customer paths, lighting, materials, UI, and save/persistence hooks relevant to this pass.

Produce a concrete prioritized implementation plan in:

Docs/VisualRebuild/PLAN.md

Immediately execute that plan yourself.

Do not stop after planning and do not wait for user approval.

Revise the plan whenever direct Unity observation proves an assumption wrong.

The plan is a working execution document, not the final deliverable.

1. MISSION

Before resuming broader V6 work, rebuild and correct Geode's core playable spaces so the game:

visually resembles the strongest provided reference images;

feels like a polished commercial simulator rather than an early prototype;

has intentional, believable room composition;

presents geodes/specimens as hero content;

has cohesive simulator-grade UI;

uses authored materials, lighting, props, and workstations;

grows physically as the player's business progresses;

does not contain late-game equipment before it is unlocked/purchased/placed;

lets the player meaningfully expand and arrange the business;

never allows furniture/equipment to block required paths;

never allows props, trays, signs, equipment, shelves, or decorative meshes to phase through walls, floors, counters, machines, or each other;

preserves customer circulation and workstation access;

remains fully usable with customers, keyboard/mouse, controller, saves, and career progression.

This is not a screenshot-faking task.

The real playable world must produce the quality shown by the references.

2. QUALITY TARGET

Target the middle ground represented by the preferred references:

more impressive and polished than the plain early mockups;

less over-rendered, noisy, or "AI concept art" than the most cinematic references;

clearly achievable as a real Unity simulator;

warm, premium, tactile, and grounded;

authored rather than procedurally cluttered;

readable in normal gameplay;

believable under close first-person inspection.

The result should look like a polished simulator game on Steam, not:

a student blockout;

a generic Unity scene;

an asset-store room with unrelated props;

a glossy fake concept render;

a mobile-game UI;

or a beautiful screenshot that collapses when the player moves two meters.

3. HARD OVERRIDE RULES

3.1 Do not resume normal V6 until this passes

This phase must complete its full Definition of Done before returning to the paused V6 roadmap.

3.2 The real game must improve

Do not satisfy this with:

plans only;

documentation only;

concept art only;

static mockups;

editor-only beauty shots;

screenshots that depend on hidden staging not present during actual play.

3.3 Do not protect weak existing layouts

If the room, partition, shelf placement, counter position, workstation spacing, display arrangement, or customer route is fundamentally poor, change it.

You may:

move walls;

resize rooms;

move or replace shelves;

redesign station zones;

widen aisles;

relocate checkout;

rework receiving/storage;

rebuild furniture;

move doors or openings when necessary;

change lighting rigs;

restructure scene hierarchy;

replace low-quality assets.

Do not preserve a bad layout merely because it already exists.

3.4 Use Blender seriously

Use Blender when geometry is the limiting factor.

Appropriate Blender work includes:

new/rebuilt workstations;

counters;

shelving;

cabinets;

racks;

wash fixtures;

machine housings;

covers;

signs;

trays;

support props;

wall/ceiling architectural pieces;

display furniture;

checkout furniture;

storage furniture;

collision-friendly simplified proxy meshes where useful.

Verify every Blender result in Unity.

3.5 Preserve systems unless structural change is required

This is not permission to replace working economy, specimen identity, save, provenance, or customer systems without cause.

Prefer:

thin adapters;

better presentation;

better layout;

better geometry;

stronger placement rules;

stronger validation.

4. CURRENT VISUAL DEFECTS — TREAT AS REAL FAILURES

The current game still exhibits several prototype-level weaknesses.

4.1 Environment composition

Current weaknesses include:

too much empty/dead floor;

stations distributed as "objects against walls";

weak focal hierarchy;

inconsistent space between workshop, retail, private collection, checkout, receiving, and storage;

low sense of progression in the physical room;

insufficient visual storytelling;

awkward route composition;

large objects that can interfere with navigation.

Correct the room as a designed business, not a container for systems.

4.2 Asset consistency

Current weak areas include:

boxy shelving;

basic stools/benches/pallets;

sparse hammer/chisel areas;

generic display cases;

visual mismatch between some newer assets and older placeholder furniture;

props that look placed for functionality rather than believable use.

Replace or rebuild assets where necessary.

4.3 Materials

Correct:

overly uniform wood;

flat floors;

weak material separation;

generic metals;

insufficient roughness variation;

weak edge response;

cheap-looking surfaces;

objects that visually merge into walls/floor.

4.4 Lighting

Correct:

overly orange global illumination;

flat ambient response;

blown-out task lamps;

weak focal hierarchy;

poor retail display illumination;

inconsistent exposure;

insufficient separation between workshop, retail, office, and collection zones.

4.5 Geodes/specimens

The rocks are the game's hero content.

Improve:

silhouettes;

interior depth;

crystal readability;

rind-to-cavity transitions;

cut-face realism;

family differentiation;

color richness without artificial neon;

display lighting;

thumbnail quality;

scale variety;

specimen stands/presentation.

4.6 UI

Improve:

hierarchy;

spacing;

card density;

tab clarity;

typography;

icons;

data grouping;

selected states;

CTA emphasis;

use of specimen imagery;

simulator-style polish.

Avoid generic large grey panels wherever richer presentation is justified.

5. PHYSICAL BUSINESS GROWTH — NON-NEGOTIABLE

The business must physically evolve through progression.

The starting shop should look like an early business, not a fully equipped endgame facility with features merely disabled.

5.1 Locked equipment must not already exist in the world

If equipment is not yet unlocked and purchased, it should generally not be physically installed in the player's usable shop.

Examples:

diamond saw;

geode cracker;

advanced lap/polishing equipment;

premium display cases;

expanded storage racks;

advanced checkout/retail fixtures;

additional specialist benches;

later-career furniture/equipment.

Do not show the finished machine sitting in place with a lock icon unless there is a specific authored reason in the design.

Preferred progression:

player unlocks access;

player purchases the equipment/fixture;

delivery/receiving occurs if appropriate;

player enters placement/build mode;

player chooses a valid location;

object is physically placed;

the business visibly changes.

The player should feel:

"I built this place."

5.2 Early game should be intentionally modest

Do not make early-game screenshots visually bad, but the operation should clearly have room to grow.

Use:

empty usable wall/floor zones;

simpler starter benches;

fewer displays;

modest storage;

fewer machines;

believable open space intended for expansion.

The early shop should feel deliberately small, not unfinished.

5.3 Upgrades should alter the world

When the player buys:

a machine;

shelving;

display cases;

storage;

lighting;

decor;

an expansion;

the change should be visible and meaningful.

Avoid upgrade systems that only change numbers while the environment remains identical.

5.4 Persistence

Placed/unlocked/purchased equipment must persist correctly.

Saving/loading must restore:

purchase state;

unlock state;

world placement;

orientation;

valid relationships/anchors;

machine state where already supported.

No duplicate equipment after reload.

6. PLAYER PLACEMENT / BUILD MODE — HARD SAFETY RULES

The player may design portions of the shop, but the game must never allow physically invalid layouts.

6.1 Invalid placement must be rejected

A player must not be able to place an object if it:

intersects a wall;

intersects the floor incorrectly;

penetrates the ceiling;

overlaps another fixture beyond permitted contact;

blocks a required doorway;

blocks a room portal;

blocks access to another section of the business;

blocks receiving;

blocks checkout;

blocks the private collection;

blocks a required workstation;

traps the player;

traps customers;

cuts the customer route to checkout;

makes a required interactable unreachable;

causes impossible clearance for a machine's moving parts;

blocks drawer/door/tray movement;

overlaps an NPC queue position;

blocks emergency/recovery routing used by the game.

If invalid:

show a clear invalid-placement state;

prevent confirmation;

explain the reason where useful.

6.2 Placement preview

Use a coherent placement ghost:

valid = visibly valid;

invalid = visibly invalid;

snapped orientation when appropriate;

clear footprint/bounds;

no misleading preview that becomes invalid after placement.

6.3 Clearance volumes

Important fixtures should have explicit clearance volumes in addition to their visual collider.

Examples:

saw working zone;

geode cracker interaction zone;

wash station player zone;

lap operating zone;

checkout cashier/player zone;

customer side of checkout;

drawers/trays;

cabinet doors;

delivery/receiving zone;

collection/display interaction zone.

These volumes are not necessarily solid player colliders; they can be validation volumes used by build mode.

6.4 Required route graph

Maintain a protected route graph through the business.

At minimum validate access between:

player spawn/main entrance;

receiving;

wash;

starter processing bench;

every currently unlocked required processing station;

retail floor;

checkout;

customer entrance/exit;

private collection when unlocked;

office/tablet;

storage when unlocked.

A placement that breaks required connectivity is invalid.

6.5 Customer navigation

Do not rely only on "NavMesh still exists."

After meaningful placement/layout changes:

spawn customers;

have them enter;

browse;

approach displays;

queue;

checkout;

depart.

A bookshelf that technically leaves a tiny NavMesh sliver but functionally blocks or jams customers is a failure.

7. COLLISION & INTERSECTION INTEGRITY

This is a zero-tolerance category for obvious defects.

7.1 Known failure types to eliminate

Examples include:

bookshelf blocking passage to the customer side;

signs inside walls;

trays cutting through cabinets;

shelves intersecting partitions;

machines clipping benches;

props half buried in furniture;

pallet pieces intersecting walls;

lamps intersecting signs;

collection cases penetrating nearby geometry;

machine covers overlapping active parts;

wall-mounted items floating away from the wall;

counter equipment sitting partly inside the countertop.

7.2 Colliders must reflect the visuals

For important objects:

collider shape should approximate real visible shape;

avoid giant invisible boxes;

avoid tiny colliders that allow obvious clipping;

use compound/simple colliders when better than one crude box;

use mesh colliders selectively only when justified;

maintain reasonable performance.

7.3 Surface anchoring

Wall and counter props should use robust placement logic/anchors.

For:

signs;

trays;

pegboards;

lamps;

dispensers;

wall tools;

screens;

registers;

POS devices;

shelves;

framed art;

verify:

surface normal alignment;

depth offset;

bounds;

no z-fighting;

no penetration;

no hovering.

7.4 Automated world audit

Create or strengthen an Editor/runtime audit that checks core authored layouts for:

intersecting bounds;

severe collider overlap;

objects outside intended room bounds;

objects embedded in walls/floors/ceilings;

required stations with blocked interaction anchors;

required portals blocked by static layout objects.

Allow explicit ignore/whitelist rules only for intentional intersections.

Do not "fix" the audit by suppressing legitimate failures.

8. WORKSHOP / SHOP LAYOUT DESIGN

Treat the room as a real floor plan.

8.1 Zones should read clearly

Establish coherent zones such as:

receiving;

rough storage;

wash/cleaning;

manual cracking/hammer;

powered processing;

appraisal/inspection;

retail display;

checkout;

private collection;

office/tablet;

future expansion zones.

The exact architecture can change if a better arrangement serves the game.

8.2 Avoid corridor blockers

Never place:

bookshelves;

display cases;

machines;

pallets;

crates;

cabinets;

decor;

in a way that visually or physically blocks a major route.

8.3 Sightlines

Important stations and progression additions should be visually discoverable without every object competing for attention.

Use:

signage;

lighting;

architecture;

open sightlines;

intentional framing.

8.4 Room scale

Do not make rooms oversized merely to avoid collision issues.

Aim for:

believable workshop dimensions;

adequate aisle widths;

comfortable first-person navigation;

room for customers to pass each other where necessary;

enough growth space for upgrades.

9. REFERENCE IMAGE MAPPING

Create:
Docs/VisualRebuild/REFERENCE_MANIFEST.md

For every reference:

reference ID;

file path;

intended game counterpart;

strongest visual traits;

current defects;

required changes;

whether Blender is needed;

whether layout restructuring is needed;

completion state;

evidence capture path.

Do not silently mark a reference "N/A" because the feature is inconvenient.

If a reference represents a feature that belongs in V6 but does not exist yet:

record it as a V6-required missing counterpart;

either implement the smallest real counterpart required by this phase when necessary for visual cohesion;

or explicitly carry it forward in the paused V6 backlog without claiming the full reference set is matched.

10. IMPLEMENTATION PHASES

Phase A — Audit + plan

Inspect:

all references;

current Play Mode;

current layout;

colliders;

NavMesh/customer paths;

unlock state;

machine placement;

UI;

specimens;

lighting/materials;

performance risks.

Write Docs/VisualRebuild/PLAN.md.

Phase B — Safe checkpoint

compile;

tests;

clean known-good commit;

push if appropriate;

record hash.

Phase C — Structural floor-plan pass

Before polishing tiny props:

fix room layout;

remove route blockers;

establish progression-ready expansion zones;

correct checkout/customer circulation;

correct receiving/storage flow;

protect portals and interaction areas.

Phase D — Progression-world pass

remove premature equipment;

ensure locked/purchased state maps to physical presence;

integrate placement/build flow where required;

make progression physically visible.

Phase E — Placement/collision system hardening

placement ghost;

overlap validation;

wall/floor/ceiling validation;

clearance volumes;

route connectivity;

customer navigation validation;

persistent placed transforms.

Phase F — Blender/asset rebuild

Rebuild weak geometry after the structural plan is known.

Phase G — Materials + lighting

Author a cohesive material/lighting language after layout stabilizes.

Phase H — Geode/specimen hero pass

Ensure central specimens match the target quality.

Phase I — UI/HUD/tablet

Bring UI to reference quality and keep it integrated with real gameplay state.

Phase J — Screen-by-screen reference iteration

For each mapped reference:

open reference;

capture real game;

compare;

identify highest-impact mismatch;

fix;

capture again;

reject if still clearly inferior;

repeat.

Phase K — Physical integrity sweep

Walk the whole game in first person:

edges;

corners;

behind machines;

under shelves;

through portals;

customer side;

receiving;

checkout;

collection;

storage.

Look for:

clipping;

unreachable areas;

bad colliders;

floating props;

phasing signs/trays;

route blockers.

Phase L — Customer stress test

Use repeated customers and multiple layouts.

Verify:

entering;

browsing;

choosing specimens;

queuing;

checkout;

departure;

no shelf-induced route deadlock.

Phase M — Persistence

Save/reload after:

purchasing equipment;

placing equipment;

moving equipment;

changing layout;

unlocking expansion;

customer/retail activity.

Phase N — Final visual QA

Capture normal player views, not staged beauty views only.

11. VISUAL VERIFICATION RULE

For every major screen/zone:

Reference → current capture → critique → implementation → new capture → comparison → acceptance/rejection.

Do not rely on memory.
Do not assume a code change improved the image.
If the observed result is bad, keep working.

Do not argue with the observed result.

12. USER-PLACED EQUIPMENT ACCEPTANCE TESTS

Test at least:

place a new workstation in open space — valid;

push it into wall — invalid;

overlap existing workstation — invalid;

block a doorway — invalid;

block customer path to checkout — invalid;

block access to wash/saw/cracker — invalid;

block a drawer or machine clearance zone — invalid;

rotate near a corner — correctly validated;

place beside a wall without penetration — valid;

save/reload placement — preserved;

multiple furniture placements — navigation remains valid;

relocate/remove where design allows — world updates cleanly;

no duplicated colliders/ghosts after reload.

13. CUSTOMER PATH ACCEPTANCE TEST

Create a test layout with several player-placeable fixtures.

Then run customers through:

entrance;

two or more browsing areas;

selected specimen;

queue;

checkout;

exit.

Repeat enough times to expose jams.

If a bookshelf/display/cabinet can block progression or strand customers, build mode is not finished.

14. UNLOCK / BUSINESS-GROWTH ACCEPTANCE TEST

From a fresh save:

inspect the starting shop;

verify later equipment is not already installed;

earn/unlock a machine;

purchase it;

receive it if applicable;

place it;

verify new station becomes usable;

verify progression/save state;

continue with another upgrade;

confirm the shop visibly grows.

The difference between Day 1 and later career should be visible in the actual room.

15. PERFORMANCE

Do not solve visual fidelity by making the build unusable.

Profile after major passes.

Watch:

triangle count;

draw calls;

crystal complexity;

shadow-casting lights;

realtime lights;

transparent materials;

overdraw;

collider complexity;

NavMesh rebuild cost;

placement validation cost.

Prefer deterministic validation and cached bounds over expensive per-frame brute force when possible.

16. REGRESSION RULES

This pass must not break:

existing checkout;

exact specimen identity;

provenance;

economy;

saves;

controller;

KBM;

career;

customer purchase flow;

processing;

Stage 2/3 systems already established;

V5/V6 known-good behavior.

Run automated tests after major structural milestones.

17. REQUIRED EVIDENCE

Before completion, provide:

reference manifest;

implementation plan;

before/after captures;

side-by-side comparisons for major references;

fresh-save early-shop capture;

progressed-shop capture showing physical growth;

placement-valid capture;

placement-invalid capture;

customer circulation proof;

collision/world audit results;

tests;

save/load placement proof;

final Play Mode captures;

clean milestone commits.

18. FAILURE MODES — DO NOT DO THESE

Do not:

decorate a broken layout;

keep a blocking bookshelf because "NavMesh technically works";

pre-place late equipment and merely disable interaction;

permit furniture through walls;

allow trays/signs to clip because they are decorative;

hide clipping by changing camera position;

solve everything with huge colliders;

solve everything with giant empty rooms;

use Blender assets without testing their pivots/scale/colliders;

mark screens complete from code inspection;

accept one good screenshot when nearby angles are broken;

call the reference pass complete while major reference counterparts are missing;

let automated tests override an obviously bad visual result.

19. DEFINITION OF DONE

This phase is complete only when all of the following are true:

Visual

Core gameplay screenshots feel like the same game as the strongest references.

Workshop, retail, collection, receiving, and checkout have deliberate composition.

Materials and lighting are cohesive and commercial-quality.

Geodes/specimens visibly carry the premise.

UI feels polished, readable, and consistent.

No major centerpiece still screams placeholder.

Layout

Required player routes are open.

Customer routes are open.

No bookshelf/display/machine blocks required circulation.

All unlocked stations are reachable.

Checkout and customer side are usable.

Receiving and collection zones are reachable.

Progression

Late equipment is not prematurely installed.

Unlock/purchase/placement visibly grows the business.

Fresh-save shop and progressed shop look meaningfully different.

Placed upgrades persist.

Placement

Invalid overlaps are rejected.

Wall/floor/ceiling penetration is rejected.

Doorway/portal blocking is rejected.

Required route blocking is rejected.

Customer-route blocking is rejected.

Machine clearance blocking is rejected.

Valid placements remain easy and intuitive.

Collision

No obvious signs/trays/shelves/machines phase through nearby geometry.

Important colliders match the visible object reasonably.

Close-up first-person inspection reveals no major collision embarrassment.

Authored-world overlap audit is clean except documented intentional intersections.

Gameplay integrity

Checkout still works.

Processing still works.

Customers still browse/queue/buy/leave.

KBM works.

Controller works.

Save/load works.

Automated regression suite is green.

Verification

Every major reference has been examined.

Every applicable reference has a real in-game counterpart.

Side-by-side captures were actually reviewed.

Weak results were rejected and iterated.

Final Play Mode captures look good from natural camera angles.

Clean milestone committed/pushed.

If any major condition above is false, the phase is not done.

20. FINAL INSTRUCTION

Take the time required.

Do not optimize for finishing quickly.

Optimize for:

a better game;

a coherent physical business;

satisfying visible progression;

safe player-authored layouts;

reliable NPC circulation;

correct collision/placement;

strong specimen presentation;

cohesive simulator-grade visual quality.

Use Unity observation continuously.
Use Blender seriously.
Play interactions yourself.
Use deterministic tests, screenshots, contact sheets where useful, collision audits, placement audits, customer stress tests, persistence tests, controller tests, and standalone verification.

Keep origin/main safe with clean known-good milestone commits.
No force pushes.
No history rewrites.

Do not resume the paused V6 specification until this phase genuinely satisfies the full Definition of Done.

The actual game is the acceptance criterion.