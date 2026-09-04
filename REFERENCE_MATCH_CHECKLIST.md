# REFERENCE MATCH CHECKLIST

> **Superseded for everything the visual rebuild touched.** This table records the *earlier*
> fidelity pass. The current state of every reference is
> `Docs/VisualRebuild/REFERENCE_MANIFEST.md`, which also corrects the R17/R18 mislabel below (R17
> is the office desk, R18 the storage room). Rows here marked **n/a** for R03, R05, R11, R13, R17
> and R18 have since been built.

State after the fidelity pass. `Docs/RefMatch/MANIFEST.md` maps every reference to its counterpart;
`Docs/RefMatch/PASS1.md` records what changed. Captures: `Geode/Assets/Output/refmatch/`.

Legend: **done** — matched and verified in Play Mode · **partial** — reworked, still short of the
reference in a named way · **n/a** — the game has no such screen (feature work, on the V6 backlog).

| ref | screen | identified | captured | env/layout | camera | light/mat | specimen | UI | collisions | re-captured | close? |
|-----|--------|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|---|
| R01 | Collection (tablet) | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | – | ✔ | **done** |
| R02 | Dealer / suppliers | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | – | ✔ | partial — no dealer figure |
| R03 | Inventory list | ✔ | – | – | – | – | – | – | – | – | **n/a** |
| R04 | Appraisal card | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | **done** |
| R05 | Management laptop | ✔ | – | – | – | – | – | – | – | – | **n/a** |
| R06 | Showroom | ✔ | ✔ | ✔ | ✔ | ✔ | – | ✔ | ✔ | ✔ | partial — cases fill by play |
| R07 | Collection browser | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | – | ✔ | **done** |
| R08 | Upgrades | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | – | ✔ | **done** |
| R09 | Geode cracker | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | partial — bench press, not the reference's industrial cracker |
| R10 | Polishing station | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | partial — motor added, still a plainer machine |
| R11 | Build mode | ✔ | – | – | – | – | – | – | – | – | **n/a** |
| R12 | Special orders board | ✔ | – | – | – | – | – | – | – | – | partial — asks show on the tablet |
| R13 | Packing station | ✔ | – | – | – | – | – | – | – | – | **n/a** |
| R14 | New discovery | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | – | ✔ | **done** |
| R15 | End-of-day report | ✔ | – | – | – | – | – | – | – | – | **n/a** |
| R16 | Receiving bay | ✔ | ✔ | ✔ | ✔ | ✔ | – | ✔ | ✔ | ✔ | partial — bay door built, no truck or yard |
| R17 | Storage room | ✔ | – | – | – | – | – | – | – | – | **n/a** |
| R18 | Office desk | ✔ | – | – | – | – | – | – | – | – | **n/a** |
| R19 | Checkout | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | partial — mannequin customer |
| R20 | Lap polisher | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | partial — no grit blocks or water feed on show |
| R21 | Appraisal bench | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | **done** — reference row added |
| R22 | Trim saw | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | **done** |
| R23 | Cracker close-up | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | partial — no gauges, hopper or caution signage |
| R24 | Wash station | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | **done** — rebuilt in Blender |
| R25 | Inspection bench | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | partial — hand inspect, no bench |
| R26 | Workshop overview | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | **done** |

## Global

- [x] no major screen still feels bland or placeholder-like — the flat grey panels, the plaster
      walls and the colour-chip thumbnails are gone
- [x] no major screen is obviously worse than its reference, among the screens that exist
- [x] the game feels cohesive across all referenced screens — one panel kit, one palette, one
      control rail, one specimen plate treatment
- [ ] **not yet resumed V6** — the seven `n/a` rows are missing features, not fidelity gaps, and
      are the natural next V6 work

## Known short of the reference, deliberately

- The customer is a stylised mannequin (R02, R19). A believable figure is character art.
- The shop cases and the display cabinet fill through play, so a fresh save's showroom is emptier
  than R06 and R08.
- R16's bay now has its roller shutter, guides, box and sill over the pallets, and names itself.
  What it still lacks is the open door, the truck on the ramp and the yard beyond it.
- The geode cracker (R09, R23) and the flat lap (R10, R20) are honest machines that read as
  machines, but they are plainer than the reference's industrial cracker and branded lap. The lap
  gained a finned motor and a maker's plate in this pass; the cracker has not been remodelled.
