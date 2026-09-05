# Visual rebuild, world layout & physical integrity — execution plan

Controlling spec: `GEODE_VISUAL_REBUILD_REFERENCE_MATCH.md`.
Safe checkpoint: `fbd9283` — compiles, 68/68 EditMode tests pass.
Baseline captures: `Geode/Assets/Output/rebuild/before/`.
Reference map: `Docs/VisualRebuild/REFERENCE_MANIFEST.md`.

This is a working document. It is revised whenever Unity proves an assumption wrong.

---

## A. What the audit found

### The room (baseline `before/A_workshop_wide.png`, `before/B_workshop_from_door.png`)

Held against R26 (workshop overview) the current workshop fails on composition first and
materials second:

1. **Everything is the same orange.** Wall board, wainscot, benches, beams and ceiling all sit in
   one narrow warm hue at one value. The reference separates dark plank walls, a light polished
   concrete floor and a **blue-grey machine livery** that carries the whole image. There is no cool
   colour anywhere in the current room. This is the single largest failure and §4.4 names it.
2. **Every station is an object pushed against a wall**, leaving a large dead square of floor in
   the middle. The reference puts the powered machines out on the floor as islands with mats and
   walks the player between them.
3. **Every station is the same brown wooden table.** No machine identity, no plinths, no
   base cabinets, no equipment language.
4. **The signs are flat cream decals lying on the boards**, eight of them crowded along one wall
   at one height in a row, several naming stations that are elsewhere or not yet owned. The
   reference hangs thick dark plaques from brackets, one per station, over the station.
5. **The floor is a flat untextured grey-beige** with no reflection and no wear.
6. **The benches are bare.** The reference's inspection bench carries a row of rough rocks — the
   game's hero content, on show, telling the story of the business.
7. **Clutter reads as leftover props**: loose cardboard boxes and a bare pallet on the floor
   rather than stock on racks.
8. **The showroom is empty and identical in material to the workshop** — same orange boards, blank price
   cards on shelves with no stock, a bare felt island, a garish red rug.
9. **Receiving is a corner behind the counter**, with no bay, no depth and no daylight.
10. **The ceiling has a visibly blown-out light** in the wide shot.

### Progression

`SawStation`, `PolishStation` and `CrackerStation` already hide their machine until the upgrade is
owned, so §5.1 is half-satisfied. What is missing is everything after the purchase: the machine
**appears instantly at an authored spot**. There is no delivery, no placement, no player choice,
and therefore no "I built this place". `WorkshopExpansion` toggles two roots; the room does not
grow in any way the player authored.

### Placement

There is no build mode at all (R11 has no counterpart). `PlacementZone` places *specimens* into
slots; nothing places *fixtures*. None of §6 exists: no ghost, no wall/ceiling test, no clearance
volumes, no route graph, no customer-route validation, no persisted fixture transforms.

### Collision

`WorldIntegrityAudit` already measures real collider interpenetration, sunk/floating objects,
unsupported slots and player pinch points — a good base to extend rather than replace. It needs
room-bounds and portal-blocking checks, and it needs to run over the new layout.

---

## B. The new floor plan

One rectangle, three zones, so the shell stays cheap and the composition reads.

    x = -6.4                                    2.4            7.0
      +--------------------------------------+--------------------+  z = 5.2
      |  BACK OF HOUSE                       |                    |
      |  receiving bay (shutter, N wall)     |                    |
      |  storage racks | office desk         |     SHOWROOM       |
      +----[ opening ]-----[ opening ]-------+                    |  z = 2.4
      |                                      |  wall shelving     |
      |            WORKSHOP                  |  island counter    |
      |  wall benches W and N                |  checkout at the   |
      |  machine islands on the floor        |  partition         |
      |                                      |                    |
      +--------------------------------------+--------------------+  z = -3.2
                 workshop door                     shop door

- Building 13.4 x 8.4 m, ceiling 3.2 m (was 10.6 x 5.4 x 3.0).
- Workshop 8.8 x 5.6 m — deep enough for a wide central aisle with machine islands to one side.
- Showroom 4.6 x 8.4 m — a deep shop: enter south, browse the length, queue at the north counter.
- Back of house 8.8 x 2.8 m, open to the workshop through two framed openings.
- **Growth**: at Stage 1 the two openings are boarded over with a hoarding and a STOREROOM sign.
  Stage 2 takes the hoarding down and the room physically doubles.

## C. Milestones

| # | milestone | covers |
|---|-----------|--------|
| M1 | Shell: new floor plan, back-of-house room, hoarding, openings, ceiling, floor material | §8, §3.3 |
| M2 | Station relayout, machine livery, hanging signs, floor mats, rocks on the benches | §4.1, §4.2, R26 |
| M3 | Material and lighting language: kill the uniform orange, cool the ambient, real concrete | §4.3, §4.4 |
| M4 | **Build mode**: ghost, validation, clearance volumes, route graph, customer-route test | §6, §12, R11 |
| M5 | Growth loop: purchase → delivery → player places the machine → persists | §5, §14 |
| M6 | Back of house dressed: receiving bay, storage racks, office desk | R16, R17, R18 |
| M7 | Showroom retail pass, standing stock and the inventory screen | §4.1, R03, R06, R19 |
| M8 | Integrity sweep, customer stress test, persistence, performance, final captures | §7, §13, §15, §17 |

Each milestone ends in a Play Mode capture, a console check and a commit.


---

## D. Decisions taken during execution

**M6 — the boarded hoarding was dropped.** The plan proposed boarding the two back-of-house
openings at Stage 1 and taking the hoarding down at Stage 2. In the Editor it read as a bug rather
than as a building site: a blank board across a framed opening in a room the player can already
see into. The back of house is instead gated by the Stage-2 root, which is what the rest of the
expansion already uses.

**M7 — the shop's standing stock is scenery; the player's stock is not.** R06's showroom is full,
shelf after shelf. Filling the *sale slots* on a fresh save would have handed the player a
business that was already running, which §5 explicitly rejects. So the display walls, the wall
shelves and the island's interior carry the shop's own standing stock (`ShopStock`: real generator
specimens, deterministic seed, no colliders, nothing in the save — the same pattern as `RoughRow`
on the workshop benches), and the six sale slots in the wall case stay empty until the player puts
something in one. The empty case next to full shelves is the goal, stated in the room.

**M7 — the stock generator is biased to the families a shop sells.** A neutral roll returns mostly
pale quartz and calcite; forty of those is a wall of grey, and R06 is a purple shop. `ShopStock`
rerolls the seed until it draws one of fourteen weighted showroom families. The geology generator
itself is untouched.

**M7 — the island's width is a circulation number.** The showroom has 3.96 m of usable floor
between the partition wainscot and the wall case. A customer has to be able to pass a standing
player on either side, so the island counter was rebuilt in Blender at 1.45 m, leaving 1.25 m of
aisle on both sides. The first 1.7 m version failed the §13 stress test.

**M7 — two real navigation defects came out of the §13 test, not out of the layout.**
`HasArrived`'s own comment said "someone (usually the player) standing on the browse point must
never park a customer forever", but `SomeoneStandingNear` only ever checked other customers. And
the player's `NavMeshObstacle` had `carving = false`, so a walker whose path ran through where the
player was standing was pushed sideways into a wall by local avoidance and stalled there — every
time at the same spot, 1.6 m from the door with a complete path. Carving only while stationary
makes a stopped player something the path is planned around. Repositions went 2 → 0 and stuck
recoveries 14–25 → 2 across three runs.

**Left deliberately quiet.** The showroom's west board (the partition north of the staff door)
carries one lit wall shelf at Stage 1 and the third display run at Stage 2: the shop is meant to
visibly gain a wall of stock when the workshop expands.

---

## E. Verification (M8)

Run against `Workshop.unity` as the scene builder produces it, in Play Mode, on the 8 GB M2.

| gate | spec | result |
|------|------|--------|
| Static collider interpenetration | §7 | **0** findings (`WorldIntegrityAudit.StaticOverlaps`) |
| Objects below the floor / floating | §7 | **0** |
| Placement-slot support (a rock can hang off no shelf) | §7 | **0** |
| Clearance and reachability | §7, §8 | **0** — 2979 free cells, 2979 reachable, every interaction zone standable |
| Decor bounds | §7 | 21, all by design: hanging-sign rods in the ceiling, wall-mounted boards in their walls, floor mats under pallet feet |
| User-placed equipment | §12 | **12/12** pass, each refusal naming a reason |
| Customer path: entrance → browse → queue → checkout → exit | §13 | **4 consecutive 5-minute runs green** — repositions 0, stuck events 0, collision loops 0, queue stalls 0, path failures 0 |
| Unlock → purchase → delivery → placement → usable → persists | §14 | pass: 0 machines installed on a fresh save; buying puts a crate in the bay, not a machine in the room; siting it makes the station usable; it survives a reload |
| Persistence, no duplicates | §5.4 | pass: pose and yaw restored exactly, one instance |
| Save/load regression | §16 | `SaveScenarios` and `RetailSaveScenarios` all `ok`, 0 collision overlaps at every reload |
| Automated tests | §16 | **68/68** EditMode |

### Performance (§15)

The showroom's standing stock was the whole story. A dense druse is ~600 crystals combined into
one mesh, near a hundred thousand triangles a rock, and forty of those is four million triangles of
set dressing. `SpecimenVisual.CrystalBudget` keeps the largest 70 for scenery only (anything the
player can pick up, appraise or sell is untouched at 0), and shop stock no longer casts shadows.

| | before | after |
|---|---|---|
| triangles, empty shop | 5,185,444 | **2,795,924** |
| triangles, stocked shop with customers | – | 4,090,578 |
| total allocated | 1,123 MB | **686 MB** (838 MB stocked) |
| graphics driver | 771 MB | **483 MB** |

### Two audit defects the widened coverage found

The clearance grid still described the V5 garage (`x` from −3.6), so the wash station, the
inspection bench and the whole back of house were outside it and reported "unreachable". It now
takes its bounds from `ShopPlan`. With the real room covered, two genuine faults appeared:

- The test capsule treated a 12 cm pallet as a wall, so the receiving bay's own pallet deck read as
  a sealed room. The controller's `stepOffset` is 0.3; anything shorter is walked over.
- **A bucket was parked in the one square metre a player has to stand in to use the sink.** With
  that fixed the wash zone is reachable and every free cell in the building is connected.

### Three authored positions that build mode itself refused

The last check of the phase was the obvious one nobody had run: **does the authored world satisfy
the rules build mode enforces on the player?** Every fixture's default pose was fed to
`PlacementValidator` in the order a player actually buys them. Three failed, and all three were
real:

- **The geode cracker stood 13 cm inside the back-of-house doorway.** M6 took down the hoarding
  that used to hide that opening, which turned it into a route. Moved from z 2.15 to 1.85.
- **A stool was parked in the polishing lap's square metre.** Buy the lap, try to put it where the
  room is drawn for it, and the game says no.
- **The lap was worked from the workshop doorway.** Its clearance faced east onto the aisle, and
  0.85 m east of the lap is the door. Turned round: the operator now stands in the wide aisle west
  of the machine row, and the mat moved with them.

With Stage 2 bought and all three machines sited, the whole audit is clean: 0 static overlaps,
0 floating, 0 unsupported slots, 0 unreachable cells, 0 pinch points.
