# Reference-match pass — what changed, screen by screen

Checkpoint before the pass: `1f19c89`. Captures live in `Geode/Assets/Output/refmatch/`
(`before_*` = the state at the start, `after_*` = the current build).

## The shared frame (every reference)

The pack's 26 images share one HUD. It is now built, and it is fed by the career the save
already keeps rather than by a second copy of it (`Runtime/Core/Progression.cs`):

| element | source |
|---|---|
| brand badge, top-left | static |
| standing goals ("Crack N Geodes", "Sell N Specimens", "Earn $N") | `Stats.SpecimensOpened`, `RetailSales + SpecimensSold`, `MoneyEarned` |
| till, top-right | `GameState.Cash` |
| `Day N — H:MM AM` | `Stats.PlayTimeSeconds`, twenty minutes to a shop day, 08:00–20:00 |
| `Empire Level N` + XP bar | money turned over, rock opened, pieces sold, minerals first met |
| key-cap control rail, bottom | `GameInput.Glyph`, so it follows the device |
| interact chip under a thin crosshair | `PlayerInteractor.PromptKey` / `.Prompt` |

The panel kit that goes with it: violet-black grounds, a warmer gold, a vivid money green, a
violet progression accent, 8–10 px radii, rarity chips (common → legendary), key/value rows,
and a rule between sections.

## Environments

- **R26 workshop / R09 R22 R23 R24 stations.** The rooms are boarded, not plastered: a new
  shiplap texture set (`Tools/Blender/gen_textures.py` → `wall_board`) with grooves, grain and
  knots, on retoned concrete, ceiling and wainscot. Every station names itself on a board
  (`HAMMER & CHISEL`, `WASH STATION`, `DIAMOND SAW`, `APPRAISAL`, `LAP STATION`). The pendants
  were tightened into pools and one hangs over each bench, the ambient dropped and re-cooled, and
  the grade (ACES, bloom, vignette, contrast, a warm shadow-to-highlight lean) is now authored on
  every rebuild instead of only when the profile is first created. Stock shelving, crates, a
  draining tray and tools on the pegboard fill the room the way the reference rooms are filled.
- **R24 wash station.** Rebuilt in Blender: a stainless utility sink under a full-width
  splashback, a wall mixer with cross handles and a swan neck over the basin, a spray gun on a
  coiled hose, a drain hose, a draining tray on the rim. It was a blue plastic tub.
- **R06 showroom.** A wine rug with a gold border under the browsing area, dark green display
  felt instead of the near-white gallery felt, a pool over the island table and a shop fill.

## Panels

- **R02 dealer / R08 upgrades.** The tablet is now a list down the left and a detail card down
  the right, with underline tabs, a green till figure in the header and a key rail along the
  foot. Long copy moved out of the rows into the detail card, so the list stays scannable.
- **R01 R07 collection.** A tile grid whose plates are the real rock: `SpecimenThumbnailer`
  builds the family's representative specimen four hundred metres under the floor, frames it,
  renders it with the game's own grade, and hands the texture to the tile — one plate per frame,
  so opening the page never stalls.
- **R04 R21 appraisal.** Labelled facts (origin, weight, crystal type, formation, size, habit,
  saturation, clarity, zoning, tool, strikes, polish, condition) with the valuation set apart in
  a green block, and a rarity chip beside the name.
- **R14 discovery.** A first-of-family or an exceptional grade now stops the screen with the
  piece, its grade and what it is worth, instead of a corner toast.
- **R19 checkout.** The Golf port already carried the counter, POS, reader, drawer and bag; the
  sale card moved onto the same right-hand rail as every other station card so the centre of the
  screen belongs to the crosshair.

## Not attempted, and why

`Docs/RefMatch/MANIFEST.md` marks six references as having no in-game counterpart: a build mode
(R11), a storage room (R17), an inventory list (R03), a packing station (R13), an end-of-day
report (R15) and a management laptop (R05, R18). Those are features, not fidelity, and they stay
on the V6 backlog. What this pass took from them is their visual language, applied to the screens
that exist.

The customer figure (R02, R19) is still the stylised mannequin. A believable human is a character
art job, not a lighting or layout one, and is called out here rather than half-done.
