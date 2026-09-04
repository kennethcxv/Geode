# Reference manifest

Source pack: `Geode/refrences/` — 27 files, 26 unique (`04_07_35 PM (1)` and `04_12_23 PM` are
byte-identical). Ids are assigned alphabetically by filename with the duplicate skipped, matching
the ids used by the earlier fidelity pass so the two passes can be read together.

Two ids were **mislabelled by the earlier pass and are corrected here**: R17 is the office desk
(not a storage room) and R18 is the storage/inventory room (not an office desk).

Evidence for this phase: `Geode/Assets/Output/rebuild/` (`before/`, `after/`, `mX_*/`).

| id | file (`ChatGPT Image Sep 4, 2026, …`) | counterpart | strongest traits | current defect | required change | Blender | layout | state |
|----|------|-------------|------------------|----------------|-----------------|:--:|:--:|-------|
| R01 | 04_07_35 PM (1) | Collection — tablet tab, display cabinet wall | lit cabinet, plates of real rock, rarity chips | cabinet empty on a fresh save | starter collection piece; cabinet lighting | – | – | carried from pass 1 |
| R02 | 04_07_35 PM (2) | Dealer / suppliers — tablet tab, intercom | dealer figure, crate imagery | no dealer figure | out of scope: character art | – | – | carried, deliberate gap |
| R03 | 04_07_36 PM (3) | Inventory list | dense sortable table of stock | no inventory screen | **build the inventory screen** (`I` in the reference key rail) | – | – | **done M7** |
| R04 | 04_07_37 PM (4) | Appraisal card | labelled facts, value set apart | – | keep | – | – | done in pass 1 |
| R05 | 04_07_37 PM (5) | Management laptop | business app over a desk | no laptop | office desk + laptop opens the tablet | ✔ | ✔ | **done M6** |
| R06 | 04_07_37 PM (6) | Showroom | wall shelving full of lit stock, glass island counter with hero pieces and price cards, plants, logo wall, cart panel | shop is empty, same orange boards as the workshop, blank price cards, garish rug | retail identity: darker walls, lit shelving, glass counter, starter stock, logo wall | ✔ | ✔ | **done M7** |
| R07 | 04_09_08 PM | Collection browser | tile grid of real specimens | – | keep | – | – | done in pass 1 |
| R08 | 04_10_10 PM | Upgrades | left list + right detail | – | extend with the new placeable fixtures | – | – | **done M5** |
| R09 | 04_10_36 PM | Geode cracker | industrial press, gauge HUD | bench press, plain | rebuild as a hydraulic press | ✔ | – | **done M2** |
| R10 | 04_11_02 PM | Polishing station | branded lap on a blue plinth | plainer machine, no plinth | machine livery plinth + mat | ✔ | ✔ | **done M2** |
| R11 | 04_11_10 PM | **Build mode** | full-screen layout editor: shop-overview panel, budget, category tabs, item hotbar with prices, green ghost + footprint grid, right-hand item card, rotate/place/cancel/duplicate rail | **no counterpart** | **build the mode** — the centre of this phase | – | ✔ | **done M4** |
| R12 | 04_11_16 PM | Special orders board | pinned commission cards | tablet only | commissions board in the office | ✔ | ✔ | **done M6** |
| R13 | 04_11_24 PM | Packing station | bench with boxes, tape, labels | no counterpart | packing bench in storage (dressing; fulfilment stays V6) | ✔ | ✔ | **done M6** |
| R14 | 04_11_30 PM | New discovery | full-screen reward | – | keep | – | – | done in pass 1 |
| R15 | 04_11_36 PM | End-of-day report | summary panel | no counterpart | carried to V6 backlog — no day-end system exists to report on | – | – | V6 |
| R16 | 04_12_28 PM | **Receiving bay** | open roller door, box truck on a ramp, daylight, crates on pallets, pallet jack, hanging RECEIVING BAY / QUALITY INSPECTION signs, stainless inspection table, labelled bin shelving, stacked pallets | corner behind the counter, no bay, no daylight | build the receiving bay in the back of house | ✔ | ✔ | **done M6** |
| R17 | 04_12_33 PM | **Office desk** | banker's lamp, laptop with the business app, letter trays, clipboard, cork board, mug, specimen tray | no counterpart | office desk in the back of house; the laptop opens the tablet | ✔ | ✔ | **done M6** |
| R18 | 04_12_38 PM | **Storage room** | steel racks of labelled crates, sorting table under a task lamp, INVENTORY STORAGE sign, chalkboard, rolling cart | no counterpart | storage racks in the back of house | ✔ | ✔ | **done M6** |
| R19 | 04_12_43 PM | Checkout | counter, register, customer side | mannequin customer | keep; re-site at the partition | – | ✔ | **done M7** (counter at the partition since M1; queue and browse lines re-verified under the §13 stress test) |
| R20 | 04_12_55 PM | Lap polisher, hands-on | wet lap, grit, hands | no grit or water on show | dressing pass | ✔ | – | **done M2** |
| R21 | 04_13_01 PM | Appraisal bench | labelled specimens in a row | – | keep, extend the row | – | ✔ | **done M2** |
| R22 | 04_13_07 PM | Trim saw | saw on a blue base cabinet | no plinth | machine livery plinth + mat | ✔ | ✔ | **done M2** |
| R23 | 04_13_12 PM | Cracker close-up | A-frame hydraulic press, chrome ram, blue pump body, brass pipework, green lever handles, **yellow maker's plate** | current cracker is a bench device | rebuild the machine; adopt the yellow plate as the machine-sign language | ✔ | – | **done M2** |
| R24 | 04_13_19 PM | Wash station | stainless sink run | – | keep; set it into a cabinet run | ✔ | ✔ | **done M2** |
| R25 | 04_13_25 PM | Inspection bench | rough rocks lined up under a task lamp | benches are bare | line the bench with rough rock | – | ✔ | **done M2** |
| R26 | 04_13_30 PM | **Workshop overview** | machines as islands on blue base cabinets with grey tops, anti-fatigue mats, hanging plaque signs over each station, dark plank walls, polished concrete, black cone pendants on rods, rough rock lined along the inspection bench, showroom visible beyond, key rail with **B Build Mode** and **I Inventory** | everything against the walls, dead centre floor, one orange hue, flat floor, decal signs crowded on one wall, bare benches | the whole of M1–M3 | ✔ | ✔ | **done M1–M3** |

## Rule applied to missing counterparts

§9 forbids marking a reference N/A for convenience. Of the six the earlier pass called
`no counterpart`, five are built in this phase (R03 inventory, R05/R17 office and laptop, R11 build
mode, R13 packing bench, R18 storage). One is carried forward explicitly: **R15 end-of-day report**
has no day-end system behind it, and inventing one is V6 feature work, not fidelity. It is recorded
in the V6 backlog rather than counted as matched.


## State at the end of the phase

Every row above is either **done** in this phase, done in the earlier fidelity pass, or one of the
two deliberate gaps named below. Verification for the whole phase is recorded in
`Docs/VisualRebuild/PLAN.md` section E and the evidence is in `Geode/Assets/Output/rebuild/`.

Two references are still short of their image, and are recorded as such rather than ticked:

- **R02 (dealer / suppliers)** — the reference shows a dealer figure. Character art for a named NPC
  is content work, not fidelity work; the supplier screen itself matches.
- **R15 (end-of-day report)** — there is no day-end system to report on. Inventing one is V6
  feature work. It stays on the V6 backlog.

`REFERENCE_MATCH_CHECKLIST.md` at the repo root is the *earlier* pass's table and is superseded by
this file for everything this phase touched; it also carries the R17/R18 mislabel corrected here.
