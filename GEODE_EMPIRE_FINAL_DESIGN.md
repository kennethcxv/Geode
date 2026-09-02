# GEODE EMPIRE — Final Product Vision & Vertical-Slice Design

> **Purpose:** This document is the authoritative game-design brief for Claude/Fable when working on Geode Empire. `CLAUDE.md` governs development workflow and tooling; this file governs product intent, gameplay priorities, scope, and validation gates.
>
> **Critical rule:** Do **not** interpret this document as permission to build the entire final game at once. The full vision exists so architecture and decisions do not paint the project into a corner. Development must prove the core experience first, then expand only after validation gates pass.

---

## 0. Product thesis and build philosophy

**Geode Empire is not a business simulator with geode cracking attached. It is a tactile geode-processing game with just enough business and collection systems to give every crack stakes.**

The fantasy is simple:

> Buy a crate of mystery rocks. Work through them physically at the bench. Most are ordinary. Some are good. Very rarely, one makes you stop and stare. Then decide whether that specimen belongs in your collection or should fund the next step of your operation.

The emotional loop is:

> **What did I buy? → What is inside? → Oh wow. → How much is this worth? → Do I really want to sell this? → One more crate.**

The project must be built in this order:

1. **Prove visual variety.**
2. **Prove the crack/reveal.**
3. **Prove a 10–12 geode processing session is fun.**
4. **Prove keep-vs-sell creates tension.**
5. **Only then build broader progression.**

No amount of economy, UI, progression, or content rescues a weak crack/reveal.

---

# PHASE GATES

## Gate A — The 200 Specimen Contact-Sheet Test

Before deep economy work, generate at least **200 deterministic specimens** across the initial mineral set and render them under identical lighting.

Create one or more contact sheets.

The test passes only if a viewer can recognize substantially more than a handful of repeated looks. The goal is not 200 unique silhouettes; the goal is a convincing visual vocabulary with **at least ~15–20 clearly distinguishable visual families/formation outcomes** before rare-trait combinations are counted.

If the 200 renders collapse into “purple crystals, white crystals, blue crystals, orange crystals,” stop and improve generation/rendering before continuing.

The player must perceive variation without reading stat text.

---

## Gate B — The Reveal Clip Test

Create a polished **10–20 second clip** showing:

1. positioning a chisel,
2. one or two strong impacts,
3. the fracture completing,
4. shell separation,
5. the crystal interior becoming visible.

The clip must communicate the fantasy **with no explanation and preferably even with sound muted**.

Then test the clip externally before building large systems.

If the reveal is not visually magnetic, improve it before expanding scope.

---

## Gate C — The Twelve-Geode Test

A player should be able to receive a crate of roughly **8–12 rocks/geodes** and spend **15–25 minutes** processing them with minimal menu interruption.

The tactile-to-admin ratio must favor tactile play.

Target experience:

- process several specimens in a row,
- quickly place ordinary outcomes aside,
- occasionally pause for appraisal or a meaningful find,
- finish the crate feeling tempted to buy another.

A session must not become:

> 40 seconds of rock interaction → 3 minutes of menus → repeat.

---

## Gate D — The Keep-It Test

At least occasionally, the player should stare at a meaningful decision:

> **SELL — $2,840**
>
> **KEEP — occupies one of only 12 premium display spaces, increases collection prestige, and may produce long-term value**

If selling or keeping is always obvious, the economy/collection relationship has failed.

---

## Gate E — The One-More-Crate Test

At the end of a 30–45 minute session, the player should naturally feel:

> “I can afford one more crate. I want to see what is in it.”

That desire is more important than the raw number of systems implemented.

---

# FINAL DESIGN

## 1. High-level concept

Geode Empire is a **first-person geode cracking, lapidary, mineral collecting, and light business-progression simulator**.

The player begins with almost no money, a small workshop, a hammer, a chisel, a basic scale/appraisal setup, and access to cheap mixed crates.

They:

- buy mystery crates,
- receive physical packages,
- inspect rocks,
- process them by hand,
- reveal procedural interiors,
- appraise results,
- keep or sell specimens,
- display favorites,
- improve equipment,
- unlock new suppliers,
- and gradually build a prestigious mineral collection and processing operation.

The final fantasy can become large, but the **core game always remains processing rocks and discovering what is inside**.

---

## 2. Core pillars

Every system must strengthen at least one of these pillars:

1. **Tactile processing** — cracking/cutting feels physical, skillful, and satisfying.
2. **Discovery** — outcomes are uncertain enough to create anticipation.
3. **Collection** — rare finds create attachment and long-term memory.
4. **Progression** — money, tools, suppliers, and workshop growth give the discoveries stakes.
5. **Shareability** — exceptional finds produce screenshots/clips worth showing other people.

If a proposed feature strengthens none of these, it should probably be cut.

---

## 3. The primary gameplay loop

The primary loop is crate-based:

> **Buy Crate → Receive → Unpack → Inspect → Process Several Specimens → Reveal → Quick Sort → Appraise Interesting Finds → Keep/Sell → Upgrade/Restock → Buy Another Crate**

The important change is **batching**.

The player does not normally order one geode, wait, process it, return to a menu, and repeat.

They buy a **crate** containing a session’s worth of material.

This creates rhythm and contrast:

- common,
- common,
- decent,
- common,
- strange,
- common,
- excellent.

The excellent specimen feels better because the player just handled the baseline personally.

---

## 4. Crate-based purchasing

Crates are the default purchase unit.

A crate can contain roughly **8–12 rocks** depending on supplier, rock size, and price.

Crates differ by:

- average cost per rock,
- reliability,
- variance,
- mineral probability,
- average specimen size,
- unknown/visible information,
- chance of unusual material,
- source reputation.

Crates must create buying decisions rather than a simple “always buy the highest tier” ladder.

---

## 5. Supplier philosophy — variance versus reliability

Suppliers are **different strategies**, not strict tiers where each one dominates the previous one.

Initial examples:

### Local Quarry Mixed Crate
- cheapest,
- mostly common,
- imperfect exterior information,
- occasional strange local outlier,
- good for volume and hammer practice.

### Regional Curated Crate
- moderate price,
- better average quality,
- lower chance of total junk,
- less extreme upside than speculative lots.

### Premium Dealer Crate
- expensive,
- high quality floor,
- lower variance,
- good for players who want reliable value and display-grade specimens.

### Estate / Mystery Lot
- limited availability,
- uncertain provenance,
- high variance,
- can be terrible,
- can contain extraordinarily rare outcomes.

The player should sometimes prefer a cheaper or riskier source even after becoming wealthy.

---

## 6. Procedural specimen identity

Every unopened rock receives a **unique persistent specimen ID** and a **deterministic generation seed**.

The seed determines the geological opportunity.

The specimen ID tracks the actual career-state object:

- unopened,
- inspected,
- struck,
- fractured,
- damaged,
- opened,
- cut,
- polished,
- appraised,
- displayed,
- sold.

Reloading must never reroll what is inside.

---

## 7. Anti-save-scumming and processing integrity

Deterministic interiors plus damage require explicit save integrity.

The career mode should use **committed processing state**:

- autosave when a specimen is placed into an active processing fixture,
- autosave when the first meaningful strike/cut begins,
- persist accumulated stress/damage after committed impacts,
- journal important state transitions immediately,
- do not allow normal manual-save rollback while a specimen is in an active processing session,
- crash recovery restores the committed physical state rather than an untouched pre-processing copy.

The goal is not to punish players for crashes. The goal is to make physical mistakes meaningful.

A “practice/sandbox” context, if ever added, can behave differently and should not affect career progression.

---

## 8. Perceptible procedural variance

The generator can track many internal properties, but player-visible variety should concentrate on axes people actually perceive.

The most important visible axes are:

1. **color/palette**,
2. **crystal scale**,
3. **crystal density**,
4. **cavity/formation shape**,
5. **surface structure / habit**,
6. **secondary mineral contrast**,
7. **damage/condition**,
8. **exceptional centerpiece features**.

Stats that cannot be seen should not carry too much design weight.

---

## 9. Initial mineral set — depth before breadth

The vertical slice should contain roughly **8–10 deeply differentiated mineral/formation families**, not 20 shallow recolors.

A strong initial set could include:

1. **Clear Quartz** — transparent/prismatic points.
2. **Amethyst** — purple quartz with saturation and zoning variation.
3. **Citrine** — warm yellow/orange quartz.
4. **Smoky Quartz** — dark translucent points.
5. **Agate / Chalcedony** — banded walls, smooth microcrystalline layers, druzy transitions.
6. **Calcite** — different crystal habit, softer/cleavage-like visual identity.
7. **Celestite** — pale blue clustered interior.
8. **Fluorite** — cubic/stepped geometry that immediately reads differently.
9. **Pyrite** — metallic gold/brassy cubes or clusters.
10. **Aragonite or another needle/branching family** — visually distinct radial/needle formations.

The exact list may change if contact-sheet testing proves a family does not look distinct enough.

Adding minerals is easy later. Making the first ten unforgettable is more important.

---

## 10. Exterior generation

Unopened rocks vary in:

- size,
- mass,
- silhouette,
- roundness,
- irregularity,
- matrix material,
- surface color,
- weathering,
- dirt/coating,
- exposed mineral hints,
- shell thickness,
- fractures,
- inclusions visible from outside.

The exterior should occasionally provide useful clues without revealing the answer.

---

## 11. Interior generation

The interior may contain:

- one or more cavities,
- wall banding,
- druzy surfaces,
- clustered crystals,
- centerpiece crystals,
- secondary mineral growth,
- varied crystal orientation,
- varied spacing,
- layered growth,
- color gradients,
- clarity variation,
- inclusions,
- unusual cavity geometry.

The system should favor **recognizable formation archetypes plus controlled procedural variation**, rather than unrestricted noise that creates visually messy results.

---

## 12. Rare traits

Rare traits must produce visible differences.

Examples:

- giant centerpiece crystal,
- exceptionally deep cavity,
- cathedral-like opening,
- double cavity,
- connected chambers,
- dense druzy carpet,
- rare color zoning,
- unusually transparent crystals,
- contrasting secondary mineral growth,
- crystals growing on larger crystals,
- phantom/inclusion-like structures,
- extreme symmetry,
- extreme asymmetry with beautiful composition,
- unusually large crystal field,
- rare metallic/mineral contrast.

The rule is:

> **If the rare trait would not make someone take a screenshot, question whether it should exist.**

---

## 13. Rarity emerges from properties

Avoid treating rarity as merely a colored loot tier.

A specimen becomes extraordinary because its actual generated properties are extraordinary.

Internal labels may still classify outcomes for UI/achievements, for example:

- Common
- Uncommon
- Rare
- Exceptional
- Museum Grade
- World Class

But those labels are descriptions of the underlying specimen, not the thing that generates it.

---

## 14. Long-tail discovery distribution

Most rocks should be ordinary.

Some should be genuinely good.

A few should be exceptional.

Extremely rarely, the player should find something they may never see again.

The best outcomes must not merely be “common specimen × 100 value.”

They should look different enough that another experienced player might ask:

> “What formation is that?”

---

## 15. Mystery before opening

Players should be able to make educated guesses from unopened material.

Early inspection is physical and simple:

- weight,
- silhouette,
- exterior clues,
- visible cracks,
- surface mineralization,
- tapping sound,
- supplier description.

Later inspection may include **at most two major dedicated tools in the first substantial release**, such as:

- loupe / inspection light,
- UV light or precision scale/measurement station.

Do not build a six-instrument laboratory before the crack works.

Inspection should improve decisions without eliminating uncertainty.

---

## 16. First-person interaction

The player should naturally be able to:

- walk,
- look,
- pick up objects,
- rotate/inspect them,
- place them deliberately,
- use benches/fixtures,
- open boxes/crates,
- grab tools,
- manipulate chisels,
- operate machines,
- move specimens to storage/display.

Physics must feel **controlled and weighty**, not like a slapstick physics sandbox.

---

## 17. Hammer-and-chisel core gameplay

This is the signature mechanic and remains useful for the entire game.

The player:

1. places the rock in a stable work position,
2. rotates it,
3. chooses a strike region,
4. places/aims the chisel,
5. chooses force/timing,
6. strikes,
7. reads sound/visual fracture feedback,
8. rotates/repositions,
9. works around the shell,
10. opens the specimen.

It must not feel like:

> “Hit rock five times until animation plays.”

---

## 18. Stress and fracture model

The rock maintains a simplified but believable internal state.

Relevant inputs can include:

- strike location,
- strike angle,
- strike force,
- accumulated stress,
- previous fracture lines,
- shell thickness,
- material hardness/toughness,
- tool condition/quality,
- clamp/support configuration.

The engineering does **not** need mathematically perfect rock fracture simulation.

Player experience beats simulation purity.

---

## 19. Damage system

Poor technique can cause visible, persistent damage:

- chipped tips,
- broken crystals,
- shattered clusters,
- fractured edges,
- damaged symmetry,
- cracked polished surfaces,
- split specimen halves,
- crushed cavity regions.

Damage reduces value because the player can see what was lost.

A hidden “-7% quality” number without a visible consequence is not enough.

---

## 20. Hammer economy role

Hammer/chisel is **not a starter tool to obsolete**.

It remains:

- fast,
- inexpensive,
- high-throughput,
- physically satisfying,
- suitable for mixed-crate volume,
- capable of excellent results in skilled hands,
- but riskier on delicate/valuable material.

Experienced players should still choose the hammer at hour 100.

---

## 21. The reveal

The reveal is the single most important moment in the game.

When a shell finally opens, the game should combine grounded effects such as:

- believable shell separation,
- small stone fragments,
- dust,
- sharp fracture audio,
- subtle camera impulse,
- crystal sparkle/highlight response,
- lighting that naturally exposes the cavity,
- brief focus shift,
- controller vibration,
- restrained rarity-specific sound/visual emphasis.

Common finds remain grounded.

Legendary finds can receive stronger presentation, but never turn into fantasy loot-chest fireworks.

---

## 22. Audio is a core mechanic

Audio communicates both satisfaction and information.

Important sounds include:

- hammer contact,
- chisel contact,
- different shell/material impacts,
- subtle fracture progression,
- the final crack,
- rock-on-bench movement,
- crystals lightly contacting surfaces,
- crate/box opening,
- clamps,
- drawers/cabinets,
- saw motor,
- blade contact,
- coolant/water,
- UI/purchase/sale cues,
- ambient workshop room tone.

The **final crack sound** deserves production-quality treatment.

Placeholder audio is acceptable during development, but the release version should budget for high-quality custom/curated sound.

---

## 23. Crystal rendering

The crystals are the product.

Rendering should prioritize:

- readable facets,
- strong silhouette,
- controlled translucency,
- Fresnel response,
- roughness variation,
- internal depth approximation,
- color gradients/zoning,
- inclusions,
- reflection highlights,
- subtle sparkle,
- beautiful response under workshop lighting.

Do not chase physically perfect refraction if a cheaper shader produces a more attractive and stable result.

Every rare specimen should photograph beautifully.

---

## 24. Visual style

Aim for **grounded realism with slightly heightened readability**.

The workshop should feel believable, but important objects and crystal interiors must read clearly.

Avoid:

- muddy realism,
- noisy surfaces everywhere,
- excessive bloom,
- magical treasure effects,
- overly glossy plastic rocks.

The visual hierarchy should make a player instantly understand where the specimen and tools are.

---

## 25. Quick-sort workflow

To keep processing sessions flowing, ordinary specimens should not require a full menu ritual.

After opening, the player can physically place results into simple zones/trays such as:

- **Sell pile / ordinary output**,
- **Inspect/appraise**,
- **Keep/display candidate**.

Interesting specimens get attention.

Ordinary specimens move through the system quickly.

This keeps the tactile-to-admin ratio healthy.

---

## 26. Appraisal

Appraisal should be satisfying but concise.

The vertical slice can evaluate:

- mineral family,
- weight,
- overall dimensions,
- color/saturation,
- clarity/translucency,
- crystal scale/density,
- formation quality,
- condition/damage,
- exceptional traits,
- estimated value.

The appraisal UI should highlight what the player can actually see.

Do not bury the player in forty geological stats.

---

## 27. Appraisal knowledge versus automation

Early appraisal can provide an estimate.

As the player progresses, better equipment/knowledge can improve confidence or reveal hidden descriptors.

The system should reward learning, but the vertical slice does not need a complicated identification laboratory.

---

## 28. Keep versus sell

This is one of the game’s fundamental decisions.

### Sell
- immediate cash,
- more crates,
- tools,
- upgrades,
- working capital.

### Keep
- consumes scarce display space,
- increases collection prestige,
- can unlock supplier/reputation thresholds,
- can generate modest passive collection income/patronage,
- remains a permanent trophy,
- may become more valuable strategically than immediate cash.

Keeping must be an **investment competing with liquidity**, not merely sentimental self-punishment.

---

## 29. Scarce display space

Display capacity is intentionally limited.

The player might begin with roughly **8–12 meaningful display slots**.

A great new specimen can force a hard choice:

- replace an older favorite,
- sell something you once treasured,
- spend heavily to expand display capacity,
- or sell the new discovery.

This prevents late-game “keep everything” behavior.

---

## 30. The physical collection

The collection is not merely a menu.

Displayed specimens physically exist in the workshop.

They can be:

- placed,
- rotated,
- labeled,
- lit,
- rearranged,
- compared.

The workshop should gradually become a visible history of the playthrough.

> **The room becomes part of the save file.**

---

## 31. Collection benefits

Displayed specimens can contribute:

- collection prestige,
- supplier trust/reputation,
- modest passive visitor/patronage income,
- milestone unlocks,
- encyclopedia records,
- achievement progress.

The passive benefit should be meaningful enough to justify keeping pieces, but never replace active processing as the primary money source.

---

## 32. Single sell channel for the vertical slice

The vertical slice should use **one clear primary sell channel**.

Do not build six buyer types immediately.

A simple dealer/market can pay based on appraised value.

The design goal is to prove:

- processing,
- value,
- collection tension,
- progression.

Complex buyer relationships come later only if the core loop earns them.

---

## 33. Future buyer relationships

Post-slice/full-game buyer systems may include:

- collectors,
- museums,
- jewelers,
- dealers,
- institutions.

But they must not reduce to “click whoever pays the most.”

Future buyers need constraints such as:

- limited demand,
- cooldowns,
- specialty preferences,
- relationship/reputation effects,
- consequences for selling museum-grade pieces into destructive use cases.

This system is **not required to prove the core game**.

---

## 34. Economy philosophy

The economy should create decisions, not guaranteed exponential growth.

Expected value may rise with player progression, but:

- crates can disappoint,
- speculative lots can produce losses,
- premium supply may be safer but less explosive,
- keeping valuable specimens ties up capital,
- tools and display expansion compete for money.

The player should occasionally feel financially stretched even after a great find.

---

## 35. Starting economy

A good tutorial start is roughly:

> **Here is $100.**

The player can afford a cheap crate and basic operations.

They should reach their first meaningful upgrade quickly enough to understand the progression fantasy, while still having to make choices.

---

## 36. Tool progression

Tool progression should unlock different interaction possibilities rather than only numerical buffs.

Examples:

### Early
- basic hammer,
- basic chisel,
- simple bench/support,
- basic scale/appraisal.

### Mid
- better chisel profiles,
- clamps,
- inspection light/loupe,
- improved work surface,
- small lapidary saw.

### Later
- precision saw,
- polishing equipment,
- better specimen preparation tools,
- advanced appraisal/inspection.

Do not build the entire endgame equipment catalog in the first slice.

---

## 37. Lapidary saw — parallel tool, not upgrade ladder

The saw must **not obsolete hammer/chisel**.

Saw identity:

- slower,
- precise,
- requires clamping/setup,
- consumes blades/coolant/time,
- ideal for slabs, display faces, and controlled cuts,
- safer in some contexts but not universally more profitable.

Hammer identity:

- fast,
- low operating cost,
- risky,
- high throughput,
- best for many natural cavity openings.

Players should switch between them based on the specimen and goal.

The saw is **SHOULD HAVE after the slice proves the crack**.

---

## 38. Cutting gameplay

When implemented, saw gameplay includes:

- specimen orientation,
- clamping,
- cut plane positioning,
- feed rate,
- coolant/water,
- blade progress,
- potential damage,
- resulting halves/slabs.

Cut location affects presentation and value.

Use robust hybrid geometry/reveal techniques instead of fragile arbitrary runtime CSG if needed.

---

## 39. Polishing

Polishing is a later supporting system.

It can improve:

- surface clarity,
- shine,
- presentation,
- display quality,
- sale value.

It should involve some physical interaction, but it must never become a mandatory grind between every reveal and every sale.

**SHOULD HAVE after the core slice, not a prerequisite for proving the game.**

---

## 40. Packaging and delivery

Crate delivery is a smaller anticipation beat before processing.

Purchases physically arrive as:

- boxes,
- wooden crates,
- larger shipments later.

The player opens the package and sees the exterior rocks before knowing the interiors.

Delivery should be fast enough that it does not interrupt session rhythm.

No unnecessary real-time waiting.

---

## 41. Inventory and storage

Inventory distinguishes:

- unopened rocks,
- opened/unappraised specimens,
- appraised specimens,
- displayed specimens,
- tools/supplies,
- pending crate orders.

Prefer physical storage where it improves immersion.

Do not turn storage into tedious warehouse management.

---

## 42. Encyclopedia and statistics

This is relatively cheap and strong for retention.

Track:

- mineral families discovered,
- formation archetypes,
- rare traits,
- best specimen per family,
- largest specimen,
- highest-value specimen,
- total crates processed,
- specimens opened,
- specimens damaged,
- specimens kept,
- specimens sold,
- biggest profit,
- biggest loss,
- lifetime collection value,
- rarest discovery.

The encyclopedia should celebrate discovery without implying every procedural combination is meant to be completed.

---

## 43. Procedural specimen names

Exceptional specimens may receive descriptive names generated from real visible properties, for example:

> Deep-Purple Amethyst Cathedral

or

> Double-Cavity Celestite Cluster

The player may optionally rename kept specimens.

Player names persist with the specimen.

---

## 44. Tutorial by doing

Avoid a large tutorial wall of text.

Teach through the first crate:

1. here is your starting cash,
2. order a cheap crate,
3. package arrives,
4. open it,
5. pick up a rock,
6. inspect it,
7. place it on the bench,
8. position chisel,
9. strike,
10. open it,
11. quick-sort it,
12. appraise a promising find,
13. sell or keep,
14. purchase the first upgrade.

Prompts disappear once learned.

---

## 45. Workshop progression

The workshop may visually evolve, but progression is staged and restrained.

### Stage 1 — Tiny workshop/garage
The entire vertical slice can live here.

### Stage 2 — Improved workshop
More bench space, better display capacity, saw area.

### Stage 3 — Professional lapidary studio
Later production scope.

Further showroom/museum-scale expansion is long-term vision, not slice scope.

---

## 46. Workshop customization — cut from core scope

Do **not** build a broad flooring/walls/furniture/sign/decorating system for v1.

It has high asset cost and weak contribution to the central fantasy.

Allow only **functional/visual progression** tied to:

- equipment,
- display capacity,
- workshop stages.

Deep freeform customization can be reconsidered after launch if players clearly want it.

---

## 47. Customer-facing shop — cut from v1

Do not turn Geode Empire into cashier simulator.

A physical retail customer loop is **not part of the vertical slice or initial core release target**.

If ever added later, it must support the cracking loop rather than replacing it.

---

## 48. Reputation

Keep reputation simple at first.

Reputation can derive from:

- quality of displayed collection,
- major discoveries,
- progression milestones.

It can unlock:

- better supplier access,
- rare crate opportunities,
- display/workshop upgrades.

Avoid a complex reputation simulation until the core game is proven.

---

## 49. Rotating supplier opportunities

After the core slice, suppliers can periodically offer unusual lots:

- estate crate,
- regional shipment,
- oversized lot,
- premium dealer special,
- mystery liquidation.

These create return-visit rhythm without becoming live-service pressure.

No real-money timers, premium currencies, or predatory mechanics.

---

## 50. No real-money loot boxes

Mystery rocks use **in-game money only**.

There is no:

- premium currency,
- paid loot box,
- real-money reroll,
- cash-based gambling mechanic.

The mystery purchase is the game mechanic itself.

---

## 51. Player skill versus RNG

The design formula is:

> **RNG determines the opportunity.**
>
> **Knowledge determines what you buy.**
>
> **Skill determines how well you process it.**
>
> **Judgment determines what you keep, sell, and invest in.**

This is the line between a tactile discovery game and a slot machine.

---

## 52. Performance target

The development machine is an **Apple Silicon M2 Mac with 8 GB RAM**, so the project must be efficient from the beginning.

Prefer:

- URP,
- modest texture sizes,
- reusable materials,
- instancing,
- sensible mesh budgets,
- pooled effects,
- limited simultaneous heavyweight simulations,
- headless Blender generation,
- predictable memory use.

Long-term desktop targets:

- Windows Steam,
- macOS Steam,
- Steam Deck-quality performance where feasible.

Do not build the game around hardware far beyond the actual development environment.

---

## 53. Blender asset-generation philosophy

Blender is primarily an automated asset factory controlled through Python/bpy.

Use it for:

- rock base meshes,
- crystal archetype meshes,
- tool props,
- workbench props,
- shelves/cabinets,
- simple workshop architecture,
- LOD/collider helpers,
- deterministic procedural source assets.

Prefer reproducible generators where practical.

Unity owns runtime gameplay/procedural composition.

---

## 54. Runtime procedural-generation philosophy

Do not generate every possible final specimen as a unique Blender asset.

Use Blender to create high-quality building blocks.

Use Unity/C# to combine them at runtime based on deterministic specimen data:

- exterior deformation,
- scale,
- material,
- cavity archetype,
- mineral family,
- crystal density,
- crystal scale,
- color,
- placement,
- rare traits,
- damage.

This provides much more variety per asset.

---

## 55. Technical philosophy

Do not implement technically impressive systems players cannot perceive.

Examples:

- No requirement for academically perfect arbitrary rock fracture.
- No need for physically perfect optical crystal refraction.
- No reason to simulate every grain of dust.

Use hybrid techniques if they produce:

- believable cracks,
- believable cuts,
- persistent damage,
- beautiful interiors,
- reliable performance.

**Player experience beats engineering purity.**

---

## 56. Save system

Persistence is mandatory and must be robust.

Store:

- money,
- progression,
- supplier unlocks,
- equipment,
- crate/pending-order state,
- every specimen ID,
- every specimen seed,
- processing state,
- damage,
- appraisal result,
- display placement,
- custom names,
- encyclopedia,
- statistics,
- settings.

Use atomic/backup-safe techniques where practical.

A one-in-a-million specimen must not vanish because of a save bug.

---

## 57. Collection integrity

Specimens are persistent entities, not disposable UI entries.

A kept specimen should retain:

- unique ID,
- seed,
- generated properties,
- damage history,
- processing history,
- appraisal history,
- player name,
- display transform.

Collection integrity has higher priority than convenience features.

---

## 58. Settings and accessibility

Release-quality fundamentals include:

- remappable keyboard controls,
- controller support/rebinding,
- mouse sensitivity,
- invert Y,
- FOV,
- camera shake amount,
- motion reduction,
- brightness,
- scalable UI,
- graphics presets,
- resolution/window mode,
- frame-rate limit,
- VSync,
- master/music/SFX/ambience sliders,
- subtitle support where spoken content exists,
- color-independent UI cues where relevant.

For the vertical slice, prioritize the subset needed for comfortable playtesting, but architecture should not block the rest.

---

## 59. Controller and Steam Deck

The interaction model must be designed for:

- keyboard/mouse,
- Xbox-style controller,
- PlayStation-style controller,
- Steam Deck controls.

Do not create critical mechanics that only work with tiny mouse movements unless a controller equivalent exists.

Steam Deck support is a target, not a reason to compromise the PC experience.

---

## 60. Photo mode

A full photo mode is **SHOULD HAVE after the slice proves out**.

The product should eventually support excellent specimen sharing through:

- UI hide,
- controlled rotation,
- focus/DOF,
- lighting/background options,
- high-resolution screenshots.

But do not build this before the reveal itself looks good in normal gameplay.

---

## 61. Achievements

Achievements should reinforce unusual or memorable play rather than only grind.

Examples:

- **First Crack** — open the first geode.
- **Keeper** — keep a specimen that would fund a major upgrade.
- **Jackpot** — find something worth 100× its effective cost.
- **Handle With Care** — preserve an exceptionally delicate specimen.
- **Museum Piece** — discover a museum-grade outcome.
- **Against Better Judgment** — keep an extraordinarily valuable specimen instead of selling it.

Achievements are not required to validate the first crack/reveal prototype.

---

## 62. Contracts — post-slice

Optional contracts may eventually provide structured goals such as:

- find a high-clarity amethyst,
- provide a specimen above a weight threshold,
- produce a low-damage display piece,
- provide a specific mineral family.

They are a later progression layer.

Do not use contracts to compensate for a repetitive core loop.

---

## 63. Auctions — post-launch / late production

Auctions can eventually provide high-stakes speculative purchasing.

They should expose incomplete information and real opportunity cost.

But simulated auctions, bidding AI, specialized UI, and buyer logic are **not part of the initial proof**.

Estate/mystery lots can provide the same high-variance fantasy much more cheaply in early development.

---

## 64. Geographic origins — later knowledge layer

Origins can eventually affect:

- exterior appearance,
- mineral probabilities,
- formation archetypes,
- size distributions,
- rare traits.

Players could gradually learn source patterns.

This is valuable long-term, but not needed to prove the vertical slice.

---

## 65. Co-op — post-launch major update

Do **not** build co-op into the initial release unless the solo game is already excellent and technical validation clearly justifies it.

The problem is that one rock naturally creates one active job.

Co-op only works if the game intentionally supports **parallel work**, such as:

- one player cracks while another sorts/appraises,
- one prepares the next specimen,
- one handles incoming crates,
- one reorganizes display/storage,
- one operates a different processing station.

The recommended strategy is:

> **Prove and ship the solo loop first. Add co-op as a major free update with parallel work deliberately designed around it.**

---

## 66. Features explicitly cut from initial scope

Do not spend initial production time on:

- broad workshop customization,
- customer-facing retail shop simulation,
- six+ inspection instruments,
- prestige/reset loops,
- sandbox career duplication,
- seed sharing in career,
- mine/quarry trips,
- visiting other players’ collections,
- multiplayer/co-op,
- large open worlds,
- vehicles,
- NPC schedules,
- combat,
- survival meters.

These features may be reconsidered later, but they are **not allowed to distract from proving the crack, reveal, variety, collection tension, and crate rhythm.**

---

## 67. Vertical-slice scope — MUST HAVE

The first serious playable vertical slice should contain:

1. one polished workshop,
2. first-person movement/interaction,
3. crate purchasing,
4. physical crate delivery/unpacking,
5. 8–10 deeply differentiated mineral families,
6. deterministic specimen IDs/seeds,
7. 200-specimen contact-sheet validation tooling,
8. hammer/chisel processing,
9. stress/fracture model,
10. visible damage,
11. polished reveal,
12. excellent crack audio placeholder architecture,
13. quick-sort workflow,
14. appraisal,
15. one sell channel,
16. scarce physical display cabinet,
17. keep-vs-sell tension,
18. simple reputation/display benefit,
19. 3–4 supplier strategies,
20. basic progression/upgrades,
21. encyclopedia/statistics,
22. robust autosave/specimen integrity,
23. tutorial by doing,
24. essential settings,
25. keyboard/mouse + controller playability,
26. stable performance on the M2/8 GB development machine.

The target is a **30–60 minute Steam-demo-quality slice**, not the entire empire fantasy.

---

## 68. Should-have features after the slice proves out

Add only after the core gates pass:

- lapidary saw as a parallel tool,
- polishing,
- more mineral families,
- more formation archetypes,
- rotating supplier opportunities,
- workshop Stage 2/3 visual progression,
- specimen renaming,
- achievements,
- photo mode,
- richer reputation progression,
- additional display formats.

Each addition should be evaluated by whether it strengthens cracking, discovery, collection, progression, or sharing.

---

## 69. Post-launch / expansion vision

Potential later additions:

- auctions,
- deeper buyer relationships,
- contracts,
- geographic origin knowledge,
- museum/gallery endgame,
- major new mineral families,
- co-op with deliberately parallel work,
- optional advanced photo/collection-sharing features.

Do not architect the current game around speculative networking or expansion requirements at the cost of simplicity.

---

## 70. Final identity and non-negotiable decision rule

The finished game should feel like a combination of:

- tactile simulator,
- treasure hunt,
- collecting game,
- light business progression,
- physical trophy room,
- social discovery generator.

It should **not** become:

- a spreadsheet business sim,
- a cashier game,
- a generic loot-box simulator,
- a physics sandbox,
- a massive open-world game,
- a feature checklist.

The final design rule is:

> **When forced to choose, prioritize the crack, the reveal, the specimen, and the desire to open one more crate.**

If a feature makes those better, consider it.

If a feature distracts from them, delay or cut it.

---

# CLAUDE / FABLE IMPLEMENTATION DIRECTIVE

When using this document during development:

1. **Do not attempt to implement all 70 sections in one pass.**
2. Treat Sections 67–69 as the scope authority.
3. Build validation tooling early, especially the 200-specimen contact-sheet generator.
4. Do not deepen economy/progression until the reveal and twelve-geode session are fun.
5. Prefer data-driven architecture for minerals, suppliers, traits, and values.
6. Prefer robust hybrid visual/geometry solutions over fragile technically perfect ones.
7. Verify gameplay in the running Unity Editor.
8. Use Blender headlessly through Python/bpy for reusable asset-generation tooling.
9. Keep generated assets and runtime systems efficient enough for the 8 GB M2 development machine.
10. Preserve specimen/save integrity from the beginning.
11. Test the tactile-to-admin ratio in actual play sessions.
12. Do not claim variety is good based on random-property counts; verify visually with contact sheets.
13. Do not claim the reveal is good because effects exist; verify it visually and through play.
14. Do not build post-launch systems to compensate for an unproven core loop.
15. After each major milestone, ask: **Does this make opening the next crate more desirable?**

---

# DEFINITION OF A SUCCESSFUL VERTICAL SLICE

The slice is successful when a new player can launch the game and, without developer explanation:

- buy a crate,
- receive and open it,
- pick up multiple mystery rocks,
- process them smoothly in sequence,
- feel meaningful physical control over cracking,
- understand when they damaged something,
- experience clearly different mineral/formation outcomes,
- occasionally encounter a specimen that visually stands out,
- appraise interesting finds,
- sell ordinary results quickly,
- keep a meaningful favorite,
- feel limited by display space,
- use profits to buy a meaningful upgrade or different supplier crate,
- save/reload without rerolling or losing specimen state,
- play comfortably on keyboard/mouse or controller,
- complete a 30–60 minute session without major bugs or repetitive menu friction,
- and finish wanting to process another crate.

The slice is **not** successful merely because all systems technically function.

It is successful when the crack/reveal rhythm is enjoyable enough that the player wants another rock before the game asks them to.