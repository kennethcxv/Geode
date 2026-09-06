# Geode Empire art direction

Selected production direction, 6 September 2026. Generated images are original design references; none is evidence of the playable game's visual quality. The direction is a modest street-facing mineral shop with practical blue steel workshop equipment, pale plaster, restrained oak, and carefully presented natural specimens. Every family still requires Blender and Unity comparison before acceptance. Measurements and physical constraints below override errors in generated imagery.

## Selected targets and critiques

| Concept | Selection | What to carry into production | What to correct or reject |
|---|---|---|---|
| [Day 1 A](Concepts/day-one-a.png) | Palette alternative | Pale walls, meaningful material contrast, useful task lighting | Reject the invented receiving cradles and rigid U-shaped rock holder; prove cashier access |
| [Day 1 B](Concepts/day-one-b.png) | Primary interior direction | Cream checkout, blue bench, plain concrete floor, a real street connection, sparse starter kit | Receiving deck must leave the door swing and approach clear; do not reproduce ambiguous OPEN/CLOSED lettering |
| [Storefront / early retail](Concepts/storefront-early-retail.png) | Store A; early retail A with revised access | Broad display window, left entrance, cream fascia with dark mineral mark, shallow useful display shelving | Captioned dimensions are design intentions, not measured drawings. Leave a real cashier entrance and a continuous 1.2 m public route; no shelf crowds the threshold |
| [Cracking / inspection](Concepts/cracking-inspection.png) | Cracking A frame; inspection B equipment language | Bolted steel frame, cross-brace, thick replaceable oak top, low shelf, excellent hammer/chisel silhouettes, later earned loupe/light/scale | Cracking A's rock is between the pads instead of visibly supported by them. Reject that contact arrangement. Fit three low angled rubber supports to the actual specimen; confirm physics contact. Inspection station is not installed on Day 1 |

| [Wash / cracker](Concepts/wash-cracker.png) | Wash A; hydraulic cracker B | Shallow stainless basin, connected plumbing, supported rock; opposing press jaws and readable hydraulic mechanism | The manual screw alternative's pressure gauge has no credible connection: reject it. Water sits below the working rock; do not conceal support contact |
| [Saw / lap](Concepts/saw-lap.png) | Cabinet saw A; horizontal lap B | Compact enclosed machinery, accessible controls and coolant management | The saw carriage must feed in the blade plane. In Blender: vertical YZ blade, operator at -Y, feed toward +Y. Reject ambiguous rails or an unclamped specimen |
| [Midgame / mature](Concepts/midgame-mature.png) | Mature B circulation | Low cases, central display with clear routes, practical private collection | Stock density requires shared materials and LOD. Do not block the entrance with a hero plinth |
| [Receiving / storage](Concepts/receiving-storage.png) | Receiving A, corrected to two bays | Finite floor marks beside storage and away from door/bench | A draws four marks despite a two-bay brief; B misaligns packages. Model exactly usable capacity, with larger equipment packages accounted for |
| [Checkout / laptop](Concepts/checkout-laptop.png) | Cream checkout A, laptop A | Oak service surface, modest 13-inch management device, clear staff access | A duplicates the cash drawer: build exactly one. The later office desk must be earned |
| [Collection / shelving](Concepts/collection-shelves.png) | Private case A, early shelf A | Sparse owned specimens, later glass cases, distinct private and sale displays | Concept stock is illustrative, never free inventory on a new save |
| [Suppliers / equipment UI](Concepts/ui-suppliers-equipment.png) | A navigation and master/detail structure | Pale work area, blue sidebar, clear focus, useful product image and delivery facts | Correct invented prices, unlocks and quantities from actual game data. Disable unaffordable purchases visibly. Capacity is a shared physical limit, not an unexplained per-product ratio |
| [Collection / business UI](Concepts/ui-collection-business.png) | Collection A grid/detail; Business A closure notice | Mineral thumbnails, ownership/discovery distinction, current customer count and explicit opening action | Reject invented tabs, bank balances, dates and statistics unsupported by the career. Keep navigation consistent with Suppliers. Closing text must say the shop is already closed to new arrivals while customers finish |

| [Overview / empty inventory](Concepts/ui-overview-inventory.png) | Overview A; honest empty inventory state | Compact useful next action, cash and physical capacity; zero stock really means zero | Avoid ornate gold/serif drift and decorative invented statistics; preserve the common blue/sans system |
| [Stats / premises / bills](Concepts/ui-stats-premises-bills.png) | Compact Stats B and Premises A plan | Legible labelled metrics, real room diagram, due date and actionable bill breakdown | Reconcile totals from the actual ledger; remove duplicate utility categories and invented prices; use metric area |
| [Processing workshop](Concepts/processing-workshop.png) | A wet/dry separation | Clear main aisle, coherent blue machinery, connected plumbing, visible door to retail | Reject B's cart blocking the center, excessive loose props, extra saw-like machines and drill-press ambiguity. Keep floor rough enough to read as concrete; lighten walls and use neutral task exposure |
| [Owned inventory](Concepts/ui-owned-inventory.png) | B compact list/detail; A useful collection-style alternative | Exact item/location/state, actual thumbnail, processing history, $45.95 precision and clear focus | Remove invented instant “Move to storage” action: finding a specimen must not teleport it. Generated dimensions/quality/value are illustrative; the real record is authoritative |

All master section 8 subject groups now have at least two original variants. [CONCEPT_COVERAGE.md](CONCEPT_COVERAGE.md) maps every requirement to its board and selected target. Small maker plates belong on machinery; do not repeat a giant game logo on every prop. The design direction is selected; individual assets and final routes remain unaccepted until their Unity gates pass.

## Visual system

Architecture uses ordinary shop construction: 2.7–3.0 m ceilings, 1.05–1.15 m clear entrance, shallow window reveals, plaster over masonry, simple skirting, painted timber/metal frames, and a street threshold that customers cross visibly. Start around 24 m² and earn connected processing and retail space. Avoid a large pre-furnished warehouse. Stage and measure the layout before detailed mesh production.

Colour balance is roughly 55% pale mineral neutrals, 20% muted blue steel, 15% natural wood, 10% dark rubber/metal and specimen colour. Initial sRGB swatches: plaster #D5CDBD, cream enamel #DDD8C9, steel blue #2E4D58, charcoal #272D30, oak #9A7149, concrete #99988E. These are art targets, not instructions to multiply all light by a warm tint. Minerals carry the richest colour.

Use mostly neutral daylight at the storefront, soft ambient fill, and limited warm practical light around work surfaces. Rough shells must remain readable without bleaching the rind; mineral facets should reflect the environment without emission. Capture the same specimen under task, retail and neutral inspection light. Shadows provide contact and shape; they are budgeted per zone.

Materials describe use: oak grain follows boards; steel has a restrained satin finish with wear at handles and fasteners; rubber is dark and rough; stainless steel belongs around water and cutting; cream enamel distinguishes customer-facing furniture. Put fine wear in textures, not thousands of tiny displaced faces. Repeated props share an atlas or tiling material. Glass is restricted to useful windows, lenses, cases and checkout surfaces.

Signage uses a simple original faceted-nodule mark and legible geometric lettering. Important signs are readable from the actual interaction distance. OPEN and CLOSED each have one unambiguous face/state. Back-room signs identify real doors; delivery marks identify actual usable capacity. Avoid decorative words over nonexistent functions.

Workstations share feet, frame gauge, fastener scale, handles and labels, while each has an identifiable working mechanism. The working side faces a usable 0.9–1.1 m operator area. Surfaces sit around 0.88–0.94 m high. Tools and rocks need hand, camera and swing clearance. Round visual bevels do not justify complex collision meshes.

Retail fixtures stay low enough to preserve sightlines. Use sparse, carefully spaced specimens on a mix of blue steel/oak shelves and later cream/glass showcases. Customer paths, browse positions and staff access are part of the fixture design. Every price card belongs to a real sale item. Private collection displays have a visibly different purpose from sale shelves.

The management laptop is a believable 13–14 inch machine in the checkout kit. UI should feel like a small business application: pale panels, dark legible type, blue navigation, mineral thumbnails, aligned currency, compact labelled metrics and a clear primary action. Supplier, equipment, inventory, collection, bills and statistics pages share the same spacing and controls. Controller focus is obvious, and the minimum readable scale is measured in real captures.

## Visual progression

| Stage | Installed environment and focal point |
|---|---|
| Day 1 | Small checkout/laptop, basic hammer/chisel bench, empty finite receiving area. Modest, clean, mostly empty. No installed wash, dedicated inspection, machines, advanced storage or showroom |
| Early | The player's first stocked display and purchased tools; a few repaired/added fixtures make ownership visible |
| Mid | Opened back room, coherent processing row with earned wash/inspection/cracker/saw/lap, separated receiving and storage, improved public circulation |
| Mature | Expanded mineral showroom and private collection, stronger retail lighting, premium cases and hero specimens; practical workshop remains credible and connected |

For every stage, acceptance requires a natural first-person walk and a comparison of the actual player camera with the selected target. A flattering Blender render alone is insufficient.
