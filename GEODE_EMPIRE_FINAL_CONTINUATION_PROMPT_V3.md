GEODE EMPIRE — FABLE 5.1 FINAL CONTINUATION / POLISH / RETAIL EXPANSION DIRECTIVE

Use this as the next prompt in the EXISTING Geode Empire /goal session after usage resets.

Do NOT restart the project, re-scaffold, or rebuild systems that already work.
Continue from the current repository, Git checkpoints, Claude project memory, completed audit, and current Unity state.

Read CLAUDE.md, GEODE_EMPIRE_FINAL_DESIGN.md, and GEODE_EMPIRE_FABLE_GOAL.md as authoritative background.

This V3 continuation prompt is the newest execution directive and explicitly overrides a few earlier scope cuts where stated below. Where this V3 conflicts with an older continuation instruction, V3 wins.
In particular, a lightweight but polished customer-facing retail shop / cashier loop is NOW REQUIRED for this first playable experience.

The core priority is still:

crack → reveal → evaluate → keep/display/sell → retail/business progression → one more crate

The retail layer must support the cracking fantasy, not replace it.

RESUME, DO NOT RESTART

Resume the existing active goal from the exact current state.

First:

inspect Git status and recent commits;

read the current project memory/status notes;

locate and read the completed adversarial geode-slice-code-audit workflow output in full;

establish the latest Unity compile/Console state;

identify all unfinished work from the previous session;

continue implementation immediately.

Do not repeat already-completed setup, Blender smoke tests, contact-sheet work, or core architecture unless a verified bug requires it.

Do not spend a long response explaining a plan.

Use:

inspect → fix → play → observe → improve → retest → commit

until the Definition of Done in the original goal plus the explicit additions below are satisfied.

FIRST PRIORITY — TRIAGE THE COMPLETED ADVERSARIAL AUDIT

The large read-only adversarial audit has already completed.

Read and triage the complete result.

Classify every finding into:

real critical/high,

real medium,

real low,

false positive/not applicable,

intentional design tradeoff.

Then:

fix all real critical/high findings;

fix all real medium findings that materially affect gameplay, saves, economy, physics, controller support, UI, buildability, performance, or maintainability of this slice;

fix low findings when cheap and clearly beneficial;

do not waste time polishing purely theoretical issues with no meaningful player impact.

After fixes:

recompile,

rerun relevant EditMode tests,

rerun affected Play Mode scenarios,

inspect the Console,

make a Git checkpoint.

Do NOT launch another gigantic 80+ agent audit unless a new severe problem genuinely requires it. Use focused reviewers/subagents where useful.

NON-NEGOTIABLE HAMMER + CHISEL / “NAIL” REWORK

This is the most important gameplay improvement in this continuation.

The current cracking must be upgraded until the player can visually understand the rock breaking exactly where they are working it.

The chisel is the “nail-like” tool the player is striking with the hammer.

Required physical fantasy

The player should:

place the rock securely;

aim/place the chisel against the shell;

visibly strike the TOP of the chisel with the hammer;

see a localized chip/crack appear at or near that exact contact point;

hear a localized material response;

rotate the rock;

work around the circumference/seam;

progressively connect crack regions;

make the shell easier to open as the crack network becomes more complete;

finally split the rock along a believable connected fracture path.

The player must not feel like they are hitting an invisible health bar.

Visible progressive fracture

Each meaningful strike should be capable of producing some combination of:

localized shell chip;

tiny stone flakes;

a new hairline crack;

an existing crack extending;

dust;

subtle material darkening/lightening along a fracture;

audio that changes as the shell becomes compromised.

The cracks should remain visible between strikes.

The player should be able to look at the rock and think:

“I have cracked this side. I should rotate it and continue over there.”

Rotation must materially matter

Working around the circumference must be the most reliable technique.

Tune the model so:

hitting one place repeatedly is inefficient and dangerous;

distributed careful strikes connect a fracture ring faster;

a rock that has been worked around most of its circumference becomes noticeably easier to finish;

the final one or two strikes after a nearly-complete fracture network feel powerful and satisfying;

heavy brute force can still open a rock but carries dramatically greater interior damage risk.

The rock should get easier to split because of visible accumulated fracture work, not because a hidden hit counter reached a threshold.

Strike force

Communicate force clearly.

Use a controlled mechanic if necessary, such as:

press/hold/release strength,

timing window,

analog trigger strength,

or another readable model.

The player should be able to intentionally perform:

light tap,

careful strike,

normal strike,

heavy strike.

Light/careful play should be safest but slower.

Heavy play should be fast and dangerous.

Preserve the already-achieved ordering where careful/light technique causes far less damage than repeated heavy strikes.

Chisel placement

Improve chisel placement until it feels deliberate.

Requirements:

chisel tip visually contacts the shell;

chisel cannot visibly float above the rock;

chisel should not visibly pass through the shell;

the shaft should orient naturally away from the contact surface;

reasonable surface snapping / constraint;

readable valid/invalid placement feedback;

controller equivalent that is comfortable.

Hammer contact

The hammer must visually contact the chisel head.

Avoid:

hammer missing the chisel;

hammer clipping halfway through it;

chisel teleporting;

impact VFX appearing at unrelated points.

Use animation constraints, controlled contact poses, or other robust techniques rather than uncontrolled physics if that produces a better result.

Final split

The final split should happen from the developed crack network.

Improve the final opening so:

fracture halves separate from each other without interpenetrating;

their motion relates to the fracture orientation;

the interior becomes immediately readable;

the shell does not phase through the bench, tools, or itself;

crystals do not visibly occupy impossible overlapping space;

pieces settle convincingly.

This interaction is the hero mechanic. Spend disproportionate polish here.

COLLISION / PHASING ZERO-TOLERANCE PASS

Perform a dedicated physical-interaction QA pass.

The current slice must not routinely show objects passing through each other.

The player should almost never see:

rock through bench;

rock through crate;

rock halves through one another;

chisel through rock;

hammer through chisel;

crystals through exterior shell;

specimen through display shelf;

crate through wall;

crate through another crate;

lid through floor/crate/wall;

player through furniture/walls;

NPC/customer through counters/furniture;

purchased specimen through checkout counter;

held object through camera in a distracting way.

Physics strategy

Use a robust mix of:

correct physics layers;

appropriate primitive/convex colliders;

Continuous/Continuous Dynamic collision where genuinely required;

controlled kinematic motion for authored interactions;

overlap checks before placement/spawning;

Physics.ComputePenetration or equivalent validation where useful;

bounds-aware crate packing;

safe snap poses;

collision-safe reveal separation;

fall/out-of-bounds recovery;

post-placement settling.

Do not use expensive detailed mesh collision everywhere.

Use coarse but accurate colliders.

Automated collision validation

Add useful development/test tooling that can detect obvious overlaps for important staged objects.

At minimum test:

unopened crate packing;

delivered crates in receiving bay;

opened crate rocks;

rock on cracking bench;

opened halves;

display slots;

appraisal station;

retail display shelves;

checkout counter;

customer navigation lanes.

Do not claim “no phasing” from visual inspection alone.

Combine automated overlap checks with Play Mode inspection.

RETAIL SHOP / CUSTOMER / CASHIER LOOP — NOW REQUIRED

This requirement OVERRIDES earlier instructions that cut customer-facing retail from the slice.

Add a small, polished mineral retail area integrated into the existing workshop.

Do NOT turn the project into a giant supermarket simulator.

The goal is to let the specimens the player processes become physical merchandise and make the business feel alive.

The player should be able to:

crack a specimen → appraise it → decide personal collection vs for-sale inventory → physically display sale specimens → customers enter → customers browse → customers select specimens → queue → player checks them out → money/reputation increase.

This should run in parallel with the cracking loop where practical.

RETAIL SPACE

Extend or adapt the workshop into a believable small showroom/shop area.

It should include:

entrance door;

modest storefront/showroom zone;

specimen sales shelves/cases;

price labels;

checkout counter;

cash register / POS;

small customer queue area;

tasteful lighting that showcases crystals;

clear separation between private workbench and customer sales area without requiring a huge building.

The existing personal collection/display cabinet should remain distinct from for-sale displays.

A player needs to understand:

PERSONAL COLLECTION = not for sale;

SALES DISPLAY = customers may purchase these.

Use strong environmental readability rather than giant tutorial text.

FOR-SALE SPECIMEN SYSTEM

After appraisal, support at least three meaningful destinations:

Quick dealer sale — immediate convenient cash, lower price;

Personal collection — kept permanently / prestige / scarce display space;

Retail display — potentially better margin, but requires shelf space and waiting for a customer.

This creates a stronger economic choice.

Retail sale price should generally have better upside than instant dealer liquidation, but:

takes time;

consumes display space;

depends on customer interest;

can tie up capital.

Do not make retail always mathematically superior.

CUSTOMER SYSTEM

Implement a restrained but believable customer loop.

Target only a small number of customers simultaneously for performance and clarity.

Customers should:

arrive through the entrance;

walk into the showroom;

browse one or more sale displays;

visibly look at specimens;

evaluate interest;

sometimes leave without buying;

sometimes choose one item;

carry/reserve the chosen specimen;

join the checkout line;

reach the cashier;

complete purchase after the player checks them out;

leave the store.

Customers should not be identical robots.

Give lightweight variation such as:

budget;

preferred mineral family;

preference for color;

preference for large specimens;

preference for pristine condition;

patience.

Do not implement a huge personality/dialogue simulation.

Their choices should be understandable enough that merchandise decisions matter.

CHECKOUT / CASHIER GAMEPLAY

The checkout counter should be an actual player interaction, not an automatic floating-money popup.

When a customer is ready:

they place/present their selected specimen;

the checkout station recognizes the specimen;

show concise item/price information;

player confirms/rings the transaction;

optionally use a simple satisfying register/POS interaction;

payment completes;

sale revenue and retail statistics update;

specimen is permanently transferred out of the player's ownership;

customer leaves;

register gives appropriate visual/audio feedback.

Keep checkout fast.

The game must NOT become repetitive cashier busywork.

Target roughly a few seconds per normal transaction.

Potential simple interactions:

scan/identify item;

press total;

accept payment;

receipt/register confirmation.

Avoid adding ten procedural steps.

SHOP OPERATES AROUND CRACKING

The shop should create parallel activity rather than constantly interrupting cracking.

Customers can browse while the player:

opens crates;

cracks rocks;

appraises;

arranges displays.

Use restrained cues when checkout is needed:

soft register bell;

subtle HUD/status indicator;

visible customer at counter.

Do not spam notifications.

The cracking bench remains the star.

A reasonable first-hour ratio might be:

majority cracking/handling/inspection;

a smaller but meaningful amount of retail/display/checkout.

CUSTOMER PATHING AND COLLISION

Customer navigation must be physically clean.

They must not:

walk through each other;

walk through counters;

walk through crates;

walk through displays;

clip through doors/walls;

stand inside the player;

block essential workshop interactions permanently.

Use navigation appropriate for this small environment.

Include fallback/recovery when a customer cannot reach a target.

Keep customer count low enough to maintain stability on the M2/8 GB machine.

SHOP ECONOMY

Retail should create another strategic layer.

Example logic:

Quick Dealer Sale

instant;

guaranteed;

lower return;

no shelf requirement.

Retail Sale

slower;

potentially 10–35% better return depending on customer fit;

shelf capacity required;

may take several customers;

capital remains tied up.

Personal Collection

no immediate money;

prestige / progression value;

scarce collection slot.

Tune through simulation and actual play.

Do not destroy the existing supplier/economy balance.

RETAIL VISUAL PRESENTATION

For-sale pieces need excellent display.

Each sale display should have:

specimen-friendly lighting;

clean physical shelf/case;

concise price/name card;

visible sold/empty state;

no text intersections;

no specimen clipping.

Customers should visibly orient toward merchandise.

A valuable crystal being browsed by a customer should be a satisfying business moment.

RETAIL SAVE INTEGRITY

Persist:

which specimens are on sale;

exact specimen IDs;

shelf positions;

asking/current prices if applicable;

reserved-by-customer state safely;

active customers only if necessary, otherwise rebuild safely;

pending checkout without duplication;

completed transactions;

retail statistics.

Never allow:

sold specimen to reappear after reload;

one specimen to be sold twice;

displayed item to remain after successful purchase;

revenue duplication via reload;

customer to permanently reserve an item after a crash if the transaction never completed.

LOUPE / MAGNIFYING GLASS — REQUIRED

Add a physical loupe / magnifying inspection tool.

The player should be able to use it on unopened and opened specimens.

It should:

provide a clear magnified view;

reveal tiny exterior mineral hints;

reveal hairline cracks;

reveal small exposed crystals;

help inspect damage;

help distinguish surface texture/banding;

make the rock visually interesting up close.

It must NOT reveal the exact hidden interior.

This adds player knowledge rather than removing mystery.

Make it feel physical and polished.

If appropriate, unlock it as an early upgrade.

Controller use must be comfortable.

BETTER PURCHASING CHOICES — DIFFERENT MATERIAL, DIFFERENT ODDS

Keep the current supplier strategies, but deepen them so purchases are not just “more money = better loot.”

Add or strengthen distinct geological/material crate categories.

Examples:

Local Mixed Quarry

cheap;

broad common material;

low floor;

occasional surprise.

Regional Curated

reliable;

better quality floor;

lower extreme variance.

Amethyst-Focused Lot

higher Amethyst probability;

stronger chance of deep-purple/cathedral outcomes;

less mineral-family variety.

Mixed Mineral Estate Box

uncertain source;

extreme variance;

higher chance of unusual combinations;

meaningful bust chance.

Premium Dealer

expensive;

strong display quality;

lower junk rate;

not necessarily highest jackpot chance.

The player should be learning:

“If I want a certain kind of specimen or risk profile, this is the crate I should buy.”

Display:

price;

expected character in plain language;

reliability/risk;

likely mineral families;

known exterior/source clues.

Do not display exact hidden odds everywhere unless useful for debug.

UI / TEXT / TYPOGRAPHY — MAJOR QUALITY PASS

The current UI quality is not yet at the desired bar.

Perform a complete UI art-direction and usability pass.

Nothing should look like default Unity UI.

Typography

Use a coherent hierarchy:

game title;

section heading;

card heading;

body;

numeric/value;

prompt;

caption.

Fix:

inconsistent font sizes;

cramped line spacing;

weak contrast;

poor text alignment;

text touching borders;

text spilling outside signs/cards;

overly small text;

labels that look like debug text.

Do not share/distribute font files.

Use fonts already available/licensed in the project/system or safe Unity alternatives.

UI visual language

Aim for:

premium workshop instrument + mineral specimen card + restrained industrial interface

Use:

consistent margins;

good spacing;

subtle borders;

controlled shadows;

restrained mineral accents;

excellent hover/focus states;

controller focus clarity;

tasteful transitions.

Required screens/panels

Polish:

title screen;

New Game / Continue / Settings;

supplier tablet;

appraisal card;

pause/settings;

encyclopedia;

statistics;

retail checkout;

retail price cards;

upgrade UI;

tutorial prompts;

end-of-slice presentation.

Inspect each at actual gameplay resolution.

MAIN MENU / BOOT FLOW

Ensure the game’s true launch path is obvious.

Shipping/build scene order must start at:

Title → New Game / Continue → Workshop

Do not rely on the currently open Unity scene.

Add a safe Editor-only convenience such as:

Play From Title, or

configured Play Mode start scene,

if appropriate.

Verify:

launch from Title;

New Game;

Continue with populated save;

Settings;

return/back behavior;

controller navigation.

ROCK / CRYSTAL QUALITY — MAJOR VISUAL PASS

The current procedural system is promising but the final visual quality should be significantly higher.

Review hero rocks at:

unopened hand-inspection distance;

cracking-bench distance;

reveal distance;

appraisal distance;

personal display distance;

retail display distance.

Improve where necessary:

exterior microvariation;

believable stone roughness;

less procedural sameness;

cavity depth;

transition from matrix to crystals;

crystal facet readability;

crystal scale variation;

cluster composition;

secondary minerals;

color zoning;

transparency/translucency readability;

rare centerpiece formations;

believable broken surfaces;

contact shadows;

display lighting.

The best specimen should look like something a player genuinely wants to screenshot.

Do not solve quality by merely increasing polygon counts.

DAMAGE VISUAL QUALITY

Damage must be obvious without a stat card.

Improve visible damage states:

frosted-white fresh fracture faces;

broken crystal tips;

missing crystal stubs;

shattered cluster areas;

chipped shell;

displaced fragments;

damage-specific roughness/surface response.

Appraisal should point to the visible problem.

A player should compare two pieces and immediately recognize which one was processed poorly.

ASSET QUALITY — FULL PASS

The environment has improved, but now perform an intentional quality review of every high-visibility asset.

Priority:

geodes;

crystals;

hammer;

chisel;

cracking cradle/bench;

crate;

appraisal scale/station;

display cabinet;

retail display cases;

checkout register/counter;

loupe;

supplier tablet;

shop signage;

key workshop props.

Use headless Blender Python to improve assets where needed.

Fix:

overly sharp primitive edges;

poor bevels;

generic shapes;

bad proportions;

weak materials;

incorrect pivots;

visible shading artifacts;

obviously repeated clutter;

poor UV/material scale.

Keep performance disciplined.

AUDIO — FULL QUALITY PASS

The current audio architecture should now be judged in actual gameplay.

Improve layering/timing for:

hammer swing;

hammer → chisel impact;

chisel → shell contact;

light/medium/heavy hits;

tiny chips;

crack growth;

near-break tension;

final split;

debris settle;

crystal handling;

crate opening;

loupe equip/use;

appraisal;

display placement;

customer entrance;

customer purchase;

register/POS;

sale completion;

ambient workshop/shop room tone.

The final crack should feel materially different from ordinary hits.

Avoid repetitive exact samples if procedural pitch/variation can help without sounding synthetic.

VFX / FEEDBACK PASS

Add or refine grounded feedback:

localized dust;

tiny chips;

crack growth;

small fragments;

subtle rare crystal glints;

reveal exposure adjustment/focus;

tiny checkout/register feedback;

customer selection feedback;

interaction highlight.

Avoid arcade loot effects.

CONTROLLER SETTINGS — FINISH THE INTERRUPTED WORK

Resume exactly where the previous session stopped.

Known current work included:

tablet D-pad navigation fixed/being verified;

slider controller steps need sensible increments;

cancel/B behavior in nested pause/settings must go back one level rather than closing everything unexpectedly.

Finish and test:

tablet tabs;

supplier list;

Buy buttons;

upgrades;

encyclopedia;

stats;

retail display interactions;

checkout;

pause;

nested settings;

sliders;

toggles;

Back/Cancel;

title screen;

Continue;

loupe;

cracking;

display placement.

No controller dead ends.

Use correct Xbox-style prompt naming where appropriate.

BUILDABILITY — ACTUAL BUILD CHECK

Run a real buildability check.

At minimum build the current macOS development target if feasible on the machine without risking storage.

Verify:

Title is boot scene;

scenes included in correct order;

no Editor-only runtime dependency;

shaders included;

Input System works;

save path works;

no missing generated asset reference;

no build-breaking warning/error;

build launches to Title.

If practical and low-risk, also verify configuration for Windows target, but do not download enormous modules without user approval.

Do not fill the disk with redundant builds.

FINAL CONSOLE / WARNINGS PASS

Clear the Console and perform representative gameplay.

There should be:

zero gameplay errors;

zero recurring exceptions;

no physics warning spam;

no missing-reference spam;

no shader errors;

no repeated navigation warnings;

no save warnings under normal operation.

Distinguish expected diagnostic warnings from real player-facing problems.

Fix normal-play warnings whenever feasible.

FINAL SAVE / RELOAD REGRESSION

After ALL new retail/loupe/collision changes, repeat persistence tests.

Test:

fresh New Game;

first crate;

partially processed rock;

processed damage;

personal display;

retail display;

sold retail specimen;

cash;

supplier unlock;

upgrades;

encyclopedia;

settings;

closing progression;

Continue from Title.

Verify zero duplication or missing specimens.

Test intentional backup recovery again if appropriate.

CLEAN-START FULL PLAYER EXPERIENCE

Perform a true final clean-start acceptance pass.

Do not rely only on scripted debug scenarios.

Use the actual player flow from Title.

Target roughly 40–95 minutes depending pace.

The full arc should now include:

Title / New Game;

buy first crate;

learn inspect/crack;

visible progressive fracture;

rotate rock and work around shell;

reveal;

appraisal;

choose quick sale / personal keep / retail display;

see first customers browse;

complete checkout;

purchase upgrade/loupe;

buy different/better geological crate;

process additional crate(s);

expand collection and retail inventory;

unlock higher-risk supplier;

experience a beautiful rare reveal;

feel display-space/cash tension;

see retail business activity around the cracking loop;

reach closing letter/future tease;

finish wanting another crate.

Measure where time is being spent.

The majority should still be tactile workshop gameplay.

FUN / “ONE MORE CRATE” PASS

Specifically inspect boredom/friction.

Ask:

Is cracking itself enjoyable after rock 10?

Is visible crack progress readable?

Do I naturally rotate the rock?

Does skill reduce damage?

Is the final strike satisfying?

Do ordinary rocks move through quickly?

Are rare pieces meaningfully prettier?

Is the loupe useful without spoiling mystery?

Do different crate types feel strategically different?

Does placing something in the shop feel exciting?

Do customers make the workshop feel alive?

Is checkout fast enough not to become tedious?

Does keeping a piece hurt financially?

Does retail create useful tension with instant selling?

Do I want another crate?

If the answer to an important question is no, iterate.

RETAIL SCOPE SAFETY

Although the customer shop is now required, do NOT explode scope into:

large NPC schedules;

employee hiring;

wages;

complex dialogue;

hundreds of products;

food/drinks;

huge storefront;

customer combat/theft;

deep relationship simulation;

multiplayer;

city simulation.

Build a small, polished, self-contained mineral shop loop that proves the fantasy.

One to several simultaneous customers is enough.

Quality > breadth.

PERFORMANCE AFTER RETAIL

Re-measure performance after customers and shop systems are running.

Keep M2/8 GB viability.

Profile:

customer navigation;

crystal rendering;

display cases;

retail UI;

lighting;

physics;

procedural specimen count;

draw calls;

CPU/GPU frame time;

memory growth.

Pool customers where useful.

Do not keep expensive AI/pathing updates running for invisible/inactive customers.

FINAL VISUAL REVIEW

Before completion, capture and inspect at least:

Title screen;

workshop overview;

receiving bay with crate;

unopened geode in hand;

chisel placement;

visible crack progression;

final reveal;

damaged specimen;

appraisal;

personal collection;

retail sales display;

customer browsing;

customer at checkout;

supplier tablet;

encyclopedia;

rare hero specimen.

Ask of each:

Would this look acceptable on a Steam page screenshot?

If not, improve the visible weakness.

FINAL QUALITY BAR

The result must no longer look like:

Unity prototype;

game-jam UI;

primitives with materials;

procedural tech demo;

placeholder simulator.

It should read as:

a small but unusually polished commercial indie game/demo built around cracking and selling beautiful procedural minerals.

Focus polish where players stare:

rock;

chisel;

hammer;

reveal;

appraisal;

displays;

customer purchase;

UI.

UPDATED DEFINITION OF DONE

The original GEODE_EMPIRE_FABLE_GOAL.md Definition of Done still applies.

Additionally, do NOT declare completion until:

Crack / chisel

chisel visibly touches the rock;

hammer visibly strikes the chisel head;

individual hits create localized visible progress;

crack lines/chips remain visible;

the player is strongly encouraged to rotate the rock;

working around the shell is faster/safer than brute forcing one point;

near-complete fracture makes final opening easier;

final split relates to the worked fracture path;

damage is visibly obvious.

Collision

no common rock/bench phasing;

no shell-half self-intersection during reveal;

no chisel-through-rock presentation;

no hammer-through-chisel presentation;

no crate/wall overlap;

no crate/crate overlap;

no rocks overlapping badly in crates;

no display specimen/shelf overlap;

no customer/counter/wall pathing failures in ordinary use;

automated overlap tests cover key staged systems.

Loupe

functional physical magnifying tool exists;

unopened and opened inspection works;

useful clues are visible;

hidden interior is not spoiled;

keyboard/mouse and controller work.

Purchasing

supplier strategies remain distinct;

at least some crate/material categories target different mineral/risk profiles;

price is not a simple linear “more expensive = always best.”

Retail

player can place appraised specimens on for-sale displays;

sale inventory is physically visible;

customers enter;

customers browse;

customers may select or decline;

customers queue;

checkout works;

player completes transaction;

money/stats update;

purchased specimen is removed permanently;

retail is saved safely;

personal collection is distinct from retail inventory;

quick dealer sale still exists;

retail does not make dealer sale pointless;

checkout is fast;

customer pathing is stable.

UI / presentation

title, tablet, appraisal, retail, settings, encyclopedia and HUD share coherent design;

typography is professional;

no text clipping/overflow;

controller focus states are obvious;

no debug-looking UI remains.

Assets

hero rocks/crystals pass close-up inspection;

hammer/chisel/bench/crate/display/register/loupe assets look authored;

no obviously unfinished hero prop remains.

QA

audit high/medium findings resolved/triaged;

EditMode tests pass;

core Play Mode scenarios pass;

controller run passes;

buildability verified;

final Console normal-play pass is clean;

final save/reload regression passes;

fresh-player progression pass completed;

shop loop tested during normal cracking progression;

performance remains comfortable.

AUTONOMY / SESSION BEHAVIOR

Continue autonomously.

Do not repeatedly ask for permission.

Do not stop after each milestone.

If the context becomes large, allow auto-compaction and continue from project memory/files/Git.

If a session/usage limit is reached, leave the repository at a clean known-good checkpoint with a precise status memory so the next session can resume immediately.

Do not burn millions of tokens on a giant redundant audit after the completed audit has already been triaged.

Spend the remaining reasoning/compute primarily on:

actual gameplay;

visual quality;

physical interaction;

retail integration;

QA;

fixes.



EXECUTION ORDER / QUALITY GATES / COMPUTE DISCIPLINE

Follow this order unless a verified dependency forces a temporary deviation.

The purpose of this section is to prevent the continuation from becoming a wide feature sprint where retail, UI, settings, or content work dilutes the signature crack/reveal.

STAGE 0 — RECOVER THE EXACT CURRENT BASELINE

Before making new changes:

inspect Git status and recent commits;

read the latest Geode Empire project memory/status files;

locate and read the completed adversarial audit output;

inspect the running Unity Editor;

check current compile state and Console;

identify the exact controller/settings change that was interrupted by the previous usage limit;

make sure the working tree is understood before editing.

Do not revert legitimate current work merely to obtain a perfectly clean Git status.

STAGE 1 — AUDIT TRIAGE + INTERRUPTED CONTROLLER/SETTINGS FIX

First:

triage the completed audit;

fix real critical/high findings;

fix meaningful real medium findings;

finish the interrupted controller slider-step and nested Back/B/Escape behavior;

verify the existing tablet/controller navigation again;

get back to a known-good, compiling, test-passing baseline;

commit.

Do not begin the retail expansion while a known serious audit finding or known controller regression is still unresolved.

STAGE 2 — HERO CRACK / FRACTURE / COLLISION QUALITY GATE

Before building the retail shop, bring the signature interaction to the new quality bar.

This gate is passed only when Play Mode inspection shows:

chisel tip visibly contacts the shell;

hammer visibly contacts the chisel head;

impact feedback occurs at the actual contact region;

individual meaningful strikes leave persistent localized chips/cracks;

visible fracture progress makes it obvious where work has already been done;

rotating and working around the shell materially helps;

repeatedly brute-forcing one spot is worse;

a nearly connected fracture path is noticeably easier to finish;

the final split is readable and satisfying;

damage is visibly different from an intact specimen;

the rock, halves, chisel, hammer, cradle, bench, and nearby geometry do not routinely phase through each other.

Do not declare this gate passed from code inspection or unit tests alone.

Capture representative Play Mode views of:

early fracture;

mid-fracture after rotating;

nearly complete fracture;

final split;

careful low-damage result;

careless damaged result.

If this interaction still looks like an invisible hit-point system, continue iterating before moving on.

STAGE 3 — HERO VISUALS / LOUPE / PURCHASING / UI FOUNDATION

After the crack gate passes:

improve close-up rock/crystal quality;

improve visible damage;

implement and test the loupe;

deepen geological/material crate choices;

complete the core UI art-direction foundation that later retail UI will reuse.

Do not create two different UI design languages for workshop systems and retail systems.

STAGE 4 — SMALL POLISHED RETAIL VERTICAL LOOP

Only then add the required shop loop.

Keep the first implementation intentionally bounded:

one integrated showroom/shop zone;

one checkout counter/POS;

a small number of clearly readable for-sale display fixtures;

roughly 1–4 simultaneous customers, adjusted by performance/playtesting;

a small set of lightweight customer preference archetypes;

fast checkout;

no employee simulation;

no large dialogue system;

no giant store-management layer.

Retail must reach a complete loop before adding breadth:

appraised specimen
→ place for sale
→ customer enters
→ browses
→ selects or declines
→ queues
→ player checks out
→ transaction commits
→ specimen leaves ownership
→ money/stats update
→ customer exits.

Play-test this end to end before expanding customer variety.

STAGE 5 — PERSISTENCE / SETTINGS / CONTROLLER / BUILD INTEGRATION

After all gameplay additions are present:

integrate retail/loupe/new fracture state into persistence;

perform duplication and recovery tests;

finish the complete settings matrix;

verify every visible setting actually changes behavior;

verify persistence/reset behavior for every setting;

verify mouse/keyboard;

verify controller;

verify Title / New Game / Continue / pause / return-to-menu;

run the real standalone buildability check;

launch the standalone build from Title if feasible;

clear and re-check the normal-play Console.

STAGE 6 — TRUE FRESH-PLAYER QUALITY PASS

Finally:

create a known-good Git checkpoint;

start from Title on a fresh career;

perform the intended player experience without developer shortcuts;

test normal, sell-heavy, collector-heavy, careless, skilled, save/relaunch, and controller paths;

specifically judge repetition, fun, pacing, visual quality, collisions, customer interruptions, UI friction, and the desire for one more crate;

fix meaningful problems;

repeat until both Definitions of Done are genuinely met.

QUALITY > FEATURE COUNT

If a new feature makes the crack/reveal worse, slower, more confusing, less polished, or less prominent, simplify the new feature.

Do not compensate for weak core gameplay by adding more systems.

OBSERVED PLAY > ASSUMPTIONS

When judging subjective quality:

use the running Unity Editor;

capture screenshots/views;

perform the interaction;

compare before/after where useful;

prefer evidence from actual Play Mode over claims based on code existing.

COMPUTE / AGENT DISCIPLINE

The previous session already completed a very large adversarial audit.

Do not repeat that pattern for ordinary work.

Default to:

the primary Fable agent;

zero to a few focused subagents;

roughly 1–6 targeted agents when parallel review materially helps.

Do not launch dozens of agents merely to polish one feature.

Use a larger workflow only if a genuinely complex unresolved problem clearly benefits from it.

Spend the plan primarily on implementation, Unity observation, Blender asset iteration, focused debugging, and final QA.

CONTEXT / SESSION CONTINUITY

Use the existing project files, Git checkpoints, and project memory as durable state.

Before an unavoidable session/usage stop:

stop at a recoverable point when possible;

save/commit known-good work when appropriate;

write a precise current-status memory;

record unfinished tests and exact next actions.

After compaction or a session reset, resume from that durable state rather than redoing completed work.



FUN, SATISFACTION, AND ANTI-REPETITION — REQUIRED

The game must not merely function. It must feel satisfying, compelling, varied, and replayable.

“Addictive” in this project means:

the player voluntarily wants to process one more rock or buy one more crate because the core loop is enjoyable, surprising, skillful, and rewarding.

Do NOT use manipulative dark patterns, artificial FOMO, real-money randomness, or frustrating timers.

Every 5–10 minutes should contain at least one meaningful change

Across the first 40–95 minutes, avoid long stretches where the player is doing the exact same thing with no new decision or payoff.

Create variation through combinations of:

different rock sizes and exterior clues;

different mineral families;

different cavity shapes;

different crystal habits;

different strike strategies;

different damage risks;

different crate types;

different supplier profiles;

different values;

rare visual traits;

retail customer preferences;

keep-vs-sell-vs-retail decisions;

upgrades that change how the player interacts;

records/new discoveries;

occasional exceptional finds;

evolving workshop/store presentation.

Do not fake variety by simply changing colors or numbers.

Anti-repetition acceptance test

After at least 15–20 rocks, ask:

am I still making real decisions?

do rocks require meaningfully different approaches?

are visual outcomes still surprising?

do I sometimes change my strike strategy?

do I sometimes inspect before cracking?

do I sometimes quick-sell?

do I sometimes keep?

do I sometimes retail-display?

do customer preferences create different merchandising choices?

do new upgrades change how I play?

are some reveals visually memorable?

does the next crate feel meaningfully different from the last?

If too many answers are “no,” redesign the pacing before declaring completion.

SATISFACTION LAYERS

Every important action should have a satisfying response.

Picking up a rock

believable weight and motion;

subtle hand/camera response;

clean inspect transition;

tactile placement sound.

Cracking

clear chisel contact;

convincing hammer impact;

progressive visible chips/cracks;

useful audio changes as stress builds;

subtle dust/debris;

satisfying final break.

Reveal

readable separation;

strong visual contrast;

beautiful crystal lighting;

appropriate rarity emphasis;

short moment to appreciate the specimen.

Appraisal

fast;

visually polished;

value count-up or other restrained satisfying feedback;

record/new-discovery callouts;

visible explanation of why the specimen is valuable.

Displaying

clean snap/placement;

dedicated display lighting;

label/card appears correctly;

prestige/collection feedback.

Retail sale

customer visibly chooses the item;

checkout is quick and tactile;

register/POS feedback;

cash/reputation feedback;

shelf visibly empties;

customer leaves with the purchase.

Purchasing a new crate

satisfying confirmation;

clear risk/reward information;

believable delivery;

crate opening that feels like the start of a new mini-story.

Avoid excessive particle spam and casino-like effects.

MOMENT-TO-MOMENT PACING

Remove dead time.

The player should rarely be forced to:

wait for long animations;

walk repeatedly across unnecessary empty space;

click through redundant confirmations;

reopen the same menu after every specimen;

wait for a customer with nothing else to do;

perform repetitive checkout steps;

repeat tutorial instructions they already know.

Allow the player to stay productive.

While customers browse, they should be able to:

crack rocks;

inspect specimens;

organize displays;

appraise;

order future crates.

Keep the game flowing.

PROGRESSION SHOULD CHANGE THE EXPERIENCE

Upgrades should not primarily be invisible numerical buffs.

Prefer upgrades that create noticeable differences.

Examples:

better chisel = clearer placement or different chisel profile;

improved cradle = easier stable rotation;

loupe = new inspection information;

improved lighting = easier exterior clue reading;

new crate category = different minerals/risk;

additional personal display = collection strategy changes;

additional retail display = more merchandising choices;

better register/POS = faster checkout;

improved appraisal = more precise information;

premium supplier access = new sourcing decisions.

The player should periodically think:

“I can do something now that I could not do 15 minutes ago.”

NEW GAME / CONTINUE / SAVE GAME — FULL PRODUCT QUALITY

Treat the game-state lifecycle as a first-class feature, not an afterthought.

Main menu must include

At minimum:

New Game

Continue

Load Game / Save management if multiple manual saves are supported

Settings

Quit

If the current scope uses one career autosave instead of multiple slots, the UI must clearly communicate that rather than presenting fake save-slot functionality.

New Game

New Game must:

start from the intended starting state;

reset cash, suppliers, upgrades, crates, specimens, displays, retail state, statistics, tutorial state, and progression correctly;

never accidentally inherit old-session data;

show a confirmation if it would overwrite an existing career;

be controller navigable;

transition cleanly into the workshop.

Continue

Continue must:

be disabled or hidden when no save exists;

load the correct latest valid save;

recover from the backup save if the primary is corrupt when possible;

restore the complete world consistently;

never duplicate specimens/cash/customers.

Save Game

If manual saving exists, it must genuinely work.

If the design intentionally uses autosave-only career persistence, do not add a misleading Save button.

If a manual Save button is present:

save immediately and safely;

show a concise confirmation;

use atomic/backup-safe writes;

never permit exploitative rollback of committed cracking damage.

Load Game

If manual/multiple saves are implemented:

display clear save slot information;

date/time;

progression summary;

confirmation before destructive overwrite/delete;

handle missing/corrupt saves gracefully.

Do not overbuild save-slot UI if the slice only needs one robust career.

PAUSE MENU — COMPLETE AND POLISHED

The pause menu must feel release-quality.

Required structure where applicable:

Resume

Save Game or save-status indicator depending on save design

Settings

Controls

Return to Main Menu

Quit Game

Behavior:

pausing truly pauses gameplay systems that should pause;

customer/shop state does not corrupt;

opening/closing pause cannot duplicate input;

Back/B/Escape consistently goes back one level;

no accidental closing of the entire pause tree from nested settings;

focus is always visible with controller;

mouse and controller can switch cleanly.

Test pause/unpause during:

free roam;

holding a rock;

cracking;

appraisal;

retail browsing;

checkout;

customer queue;

crate delivery.

SETTINGS — BEST PRACTICAL SETTINGS MENU

Build the strongest practical settings menu for this game while keeping it reliable and performant.

Every visible setting must actually work and persist after restarting the game.

Do not include decorative settings that do nothing.

Gameplay

Include where applicable:

interaction hold/toggle options;

tutorial prompts on/off or reduced;

camera shake intensity;

controller vibration/haptics intensity;

auto-sprint only if sprint exists;

inspect sensitivity if useful;

cracking assistance / aim assistance only if it improves accessibility without trivializing skill.

Mouse / Keyboard

mouse sensitivity;

invert Y;

key rebinding where supported;

reset controls to defaults.

Controller

look sensitivity;

invert Y;

vibration/haptics;

deadzone where useful;

controller prompt style if feasible;

controller rebinding where supported/practical;

reset controller bindings.

Camera / Accessibility

FOV;

camera shake;

motion reduction;

head bob amount if head bob exists;

crosshair visibility/size if applicable;

interaction highlight intensity if applicable;

UI scale;

subtitle toggle if spoken content exists;

color-independent indicators;

readable/high-contrast mode if practical.

Graphics

At minimum where supported:

display mode: fullscreen / borderless / windowed;

resolution;

VSync;

frame-rate cap;

quality preset;

shadow quality;

anti-aliasing option if exposed safely;

render scale if useful;

texture quality if meaningful;

post-processing toggle/quality if meaningful;

brightness/gamma.

Do not expose settings that break the carefully tuned crystal look unless safe bounds are enforced.

Audio

Separate sliders:

Master

Music

SFX

Ambience

UI

Optional additional slider if useful:

customer/shop sounds

All sliders:

have sensible increments on keyboard/controller;

update in real time where appropriate;

persist;

have usable defaults;

never jump unexpectedly.

Settings UX

Requirements:

clear sections/tabs;

readable labels;

concise descriptions/tooltips where useful;

Apply only where necessary;

Revert confirmation for dangerous video changes;

Reset section / Reset all;

controller-safe navigation;

clear selected/focused states;

no hidden off-screen controls;

no text clipping.

Test every setting individually.

Do not say settings are complete until each visible control demonstrably changes the game as intended.

SETTINGS TEST MATRIX

Create a structured verification pass for settings.

For every visible setting record:

default value;

changed value;

whether runtime behavior visibly changes;

whether it survives scene transitions;

whether it survives save/restart where appropriate;

mouse interaction;

controller interaction;

reset-to-default behavior.

Specifically verify:

sensitivity changes;

FOV changes;

invert Y;

camera shake reduction;

audio sliders;

frame cap;

VSync where testable;

graphics preset;

resolution/display mode where safe;

UI scale;

vibration setting;

control bindings if implemented.

Fix any dead setting.

UI QUALITY BAR — MENUS MUST FEEL COMMERCIAL

Perform another dedicated visual pass on all menu/UI surfaces.

The UI should have:

a coherent grid;

consistent padding;

consistent corner treatment;

consistent heading styles;

high-quality focus/hover/pressed states;

tasteful transitions;

clear selected tab states;

excellent text contrast;

readable currency/numbers;

aligned columns;

no accidental text wrapping;

no placeholder icons;

no debug labels;

no raw enum names shown to players.

Main menu, pause menu, settings, supplier tablet, appraisal, encyclopedia, statistics, retail checkout, price cards, tutorial prompts, and end-of-slice presentation should all look like they belong to the same game.

At final QA, inspect these screens at:

common desktop resolution;

smaller Steam-Deck-like resolution/aspect;

mouse/keyboard;

controller.

SAVE / SETTINGS EDGE-CASE TESTING

Test these cases:

New Game with no existing save;

New Game with an existing save;

Continue with no save;

Continue with valid save;

Continue with intentionally corrupt primary but valid backup;

quit after buying crate;

quit mid-crack;

quit after crack before appraisal;

quit with specimen in personal display;

quit with specimen on retail shelf;

quit while customer has reserved an item;

quit immediately after checkout;

quit after changing settings;

return to main menu and Continue;

start another New Game after prior progression.

No duplication, lost money, lost specimens, or broken settings.

FINAL ANTI-REPETITION PLAYTEST

Before declaring completion, perform a full session specifically looking for repetition.

Track:

number of rocks before cracking feels repetitive;

number of repeated identical prompts;

number of unnecessary menu opens;

average time between meaningful rewards;

number of visually similar consecutive outcomes;

number of times the player has a real choice;

customer/shop interruptions;

travel/walking dead time.

If the loop starts feeling repetitive, improve it through:

stronger rock variation;

better exterior clues;

different crate compositions;

different cracking difficulty/strategy;

more interesting rare traits;

quick-sort improvements;

more meaningful retail/customer preferences;

better upgrade timing;

shorter dead transitions;

occasional record/milestone moments.

Do not add random busywork.

FINAL EXPERIENCE TEST

The finished first-play slice should create a sequence like:

“This rock looks weird.”

“I think this side is thinner.”

“I can see the crack spreading.”

“I should rotate it.”

“One more hit…”

CRACK

“Whoa.”

“This one is actually valuable.”

“Do I keep it?”

“Maybe I can make more if I put it in the shop.”

“That customer actually likes amethyst.”

“Sold.”

“Now I can afford that risky crate.”

“Okay, one more crate.”

If the game does not naturally generate moments like this, continue iterating.

UPDATED FINAL COMPLETION RULE

Do not declare Geode Empire complete until:

the core cracking loop is satisfying;

repeated cracking remains varied;

rock outcomes are visually diverse;

progression creates new decisions;

retail adds life without dominating;

New Game works;

Continue works;

save behavior is robust;

pause behavior is correct;

every visible setting actually functions;

every setting persists where appropriate;

keyboard/mouse navigation works;

controller navigation works;

all major UI looks commercially polished;

there are no obvious collision/phasing issues in normal play;

the final clean-start session is fun enough that the player wants another crate.

The final goal is not “maximum feature count.”

The final goal is:

A polished, responsive, varied, satisfying first-hour game that makes the player want one more rock, one more customer, and one more crate.

EXECUTE

Begin by reading the completed adversarial audit and the current project status.

Then continue.

Do not stop because systems technically work.

The goal is now:

Make Geode Empire’s current first-hour slice feel dramatically better than it does today: better rocks, better cracking, visible fracture progression, better collisions, better UI, better assets, a useful loupe, smarter crate purchasing, and a polished mineral shop where customers browse the exact specimens the player processed and the player checks them out at the register.

Protect the signature fantasy:

buy mystery material → inspect → physically work the shell → SEE IT BREAK WHERE YOU HIT IT → rotate and finish the fracture → reveal something beautiful → appraise → keep / instant-sell / retail-display → customers react and buy → reinvest → one more crate.

Only declare completion after the updated Definition of Done and the original Definition of Done are both genuinely satisfied.