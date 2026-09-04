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
| M7 | Showroom retail pass and starter stock | §4.1, R06 |
| M8 | Integrity sweep, customer stress test, persistence, performance, final captures | §7, §13, §15, §17 |

Each milestone ends in a Play Mode capture, a console check and a commit.
