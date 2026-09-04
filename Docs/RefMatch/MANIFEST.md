# Reference-image manifest

Source pack: `Geode/refrences/` (27 files, 26 unique — `04_07_35 PM (1)` and `04_12_23 PM` are byte-identical).
Stable ids are assigned alphabetically by filename; thumbnails live in the session scratchpad.

| id | reference file | kind | in-game counterpart | exists? |
|----|----------------|------|---------------------|---------|
| R01 | 04_07_35 PM (1) | hybrid | Collection — tablet tab 2, plus the display cabinet wall | partial |
| R02 | 04_07_35 PM (2) | hybrid | Dealer / suppliers — tablet tab 0 + the dealer intercom | partial (no dealer figure) |
| R03 | 04_07_36 PM (3) | UI-first | Inventory / storage list | **no counterpart** |
| R04 | 04_07_37 PM (4) | workstation | Appraisal station — `AppraisalStation` + `AppraisalUI` | yes |
| R05 | 04_07_37 PM (5) | hybrid | Management laptop / business overview | **no counterpart** |
| R06 | 04_07_37 PM (6) | environment | Showroom / shop floor — `RetailShop` + display cases | yes |
| R07 | 04_09_08 PM | UI-first | Collection browser (full screen) | partial (tablet tab) |
| R08 | 04_10_10 PM | hybrid | Upgrades — tablet tab 1, over the showroom | yes |
| R09 | 04_10_36 PM | workstation | Geode cracker — `CrackerStation` + `CrackerHud` | yes |
| R10 | 04_11_02 PM | workstation | Polishing station — `PolishStation` | yes |
| R11 | 04_11_10 PM | hybrid | Build mode / shop layout editing | **no counterpart** |
| R12 | 04_11_16 PM | hybrid | Special orders board — commissions exist in `GameState` | partial (no board) |
| R13 | 04_11_24 PM | workstation | Packing / order fulfilment | **no counterpart** |
| R14 | 04_11_30 PM | reward | New discovery moment — `GameSession.RecordDiscovery` | partial (toast only) |
| R15 | 04_11_36 PM | UI-first | End-of-day report | **no counterpart** |
| R16 | 04_12_28 PM | environment | Receiving bay — `ReceivingArea` pallets | partial (no bay) |
| R17 | 04_12_33 PM | environment | Storage room | **no counterpart** |
| R18 | 04_12_38 PM | environment | Office desk with laptop | **no counterpart** |
| R19 | 04_12_43 PM | workstation | Checkout counter — `CheckoutStation` (Golf port) | yes |
| R20 | 04_12_55 PM | workstation | Lap polisher, hands-on | yes |
| R21 | 04_13_01 PM | workstation | Appraisal bench with labelled specimens | yes |
| R22 | 04_13_07 PM | workstation | Trim saw — `SawStation` + `SawHud` | yes |
| R23 | 04_13_12 PM | workstation | Geode cracker, machine close-up | yes |
| R24 | 04_13_19 PM | workstation | Wash station — `WashStation` | yes |
| R25 | 04_13_25 PM | workstation | Inspection & analysis bench | partial (hand inspect only) |
| R26 | 04_13_30 PM | environment | Workshop overview | yes |

## What this pass covers

Screens marked **yes** or **partial** are in scope: they have a real counterpart the player can reach,
and the work is to make that counterpart look like its reference.

Screens marked **no counterpart** describe systems the game has not built (a build mode, a storage
room, an end-of-day report, a packing station, a management laptop, an inventory list). Those are
feature work, not a fidelity pass, and they stay on the V6 backlog. What this pass takes from them
is their *visual language* — panel weight, type hierarchy, spacing, the key-cap control rail, the
rarity chips, the left-rail/detail-card split — and applies it to the screens that do exist, so the
game reads as one product.
