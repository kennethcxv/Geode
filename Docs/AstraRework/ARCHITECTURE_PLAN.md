# Whole-shop architecture — measured working plan

Phase 3, 6 September 2026. The selected concepts are the visual target. This plan is being tested against human-scale bodies, operator envelopes, delivery capacity and actual customer routes before detailed asset production. It is not an accepted scene or a claim of final visual parity.

The outer coordinate frame stays at x −6.4…7.0 m, z −2.7…6.0 m so useful world references and owned-item migration have a stable basis. The interior is redesigned: a **6 × 4 m starter unit (24 m²)**, an earned 7.8 × 4.7 m processing/back room, and a 5.6 × 8.7 m expanded showroom with a small connected office/collection alcove. The old all-timber shell, sealed porch, counter serving window and arbitrary hoarding are replaced. Retaining the outer foundation does not retain the old room organization.

| Zone | Proposed bounds and purpose |
|---|---|
| Starter | x −6.4…−0.4, z −2.7…1.3. Installed checkout/management, basic cracking, two receiving positions; no wash, dedicated appraisal, machines, storage wall or free display stock |
| Processing / operations | x −6.4…1.4, z 1.3…6.0. Earned wash/inspection, saw/cracker/lap, four finite loading cells, useful storage and service infrastructure |
| Mature showroom | x 1.4…7.0, z −2.7…6.0. Real street entrance, freestanding checkout with staff access, low island, wall stock and clear browse/queue routes |
| Office / private collection | x −0.4…1.4, z −2.7…1.3. Earned management/collection alcove connected through the back room; no starting office kit |

The starter counter's measured current collider is **2.60 × 0.85 m**, not the initially assumed compact counter. The plan accommodates that real size without shrinking payment devices. It sits across the rear-left of the starter with a cashier lane behind. The cracking bench sits against the right wall with its working side facing the room. Two receiving cells sit near the front-right, over one metre clear of the bench's end. The public entrance is on the front-left. Arrivals and departures have a lane beside the queue.

Planned starter anchors (x,z): counter (−4.45,−0.15), customer (−4.45,−0.90), cashier (−4.45,0.75), bench (−0.90,0.25), operator (−1.95,0.25), receiving (−1.20,−2.05)/(−2.55,−2.05), entrance (−5.60,−2.70), queue (−4.45,−1.65)/(−4.45,−2.40). Door to processing is centered at (−3.30,1.30), clear of bench operation. Exact envelopes and door swing must pass the study; adjust the plan when they do not.

For the processing room, a wet station and inspection run use the west wall; the saw/cracker operate southward from the north/east side. Receiving remains under the real loading threshold. The showroom partition moves west to give the island and wall fixtures usable circulation; the old 4.6 m showroom width cannot comfortably contain both sides of display and a wide island. Primary paths target 1.2 m; one-person staff/operating areas target at least 0.9 m. All actual colliders, drawer/door travel and specimen swing envelopes must be measured.

Implementation stays in the existing pipeline. `WorkshopSceneBuilder` gains a measured study/apply path; it must preserve current gameplay objects, scripts and the repaired Delivery file IDs. A study is only a reversible layout check. Production integration must update scene geometry, ShopPlan, gates, fixture defaults, camera/interaction anchors, receiving and retail route points together. Do not run the old full builder against production or patch serialized YAML.

Required dependency fixes before production Play:

- **Minimum starter selling:** SaleSlot currently requires a separate appraisal station. An opened specimen already has an estimated value, so basic retail can use that estimate without granting a free premium station or falsely marking it appraised. Exact appraisal and calibrated value remain earned. Verify the actual initial sale and unchanged specimen identity.
- **One receiving capacity:** stock crates and equipment currently count independently. The two starter marks represent one shared physical resource. Reject over-capacity purchases before charging, retain pending owned equipment, and never hide or drop older owned items during migration.
- **Earned fixtures:** wash/inspection/storage become purchased and placed equipment. Preserve older careers' ownership and in-progress processing. Room leases open space, not a package of free machines.
- **Save migration:** preserve IDs, lineage, condition, collection, cash, bills and upgrades; migrate fixture/world/crate positions through explicit validated relocation/recovery. Do not load an old career into new walls before this exists. Retain an interrupted cut and any specimen on a moved station.
- **Opening state and entrance:** a physical OPEN/CLOSED sign calls the existing saved admission policy. Both starter and mature doors animate at their true threshold. Closing leaves current customers able to finish and exit.

Before accepting architecture: isolated fresh and copied-career runs, natural keyboard/controller walks, stock/upgrade delivery, positive and negative placement, door/queue/staff/machine clearance, customer close/finish/exit, persistence and native first-person captures. Profile the resulting scene. Asset detail follows this gate; the draft workbench remains parked.

The initial native study found a 44.5 mm bench/partition intersection and a cashier area touching the rear wall. Both were corrected; 19 body envelopes, operator overlap checks and starter capsule routes now pass, including a deliberately blocked-counter negative control. [Evidence](ARCHITECTURE_STUDY_EVIDENCE.json), [overhead](Architecture/plan.png), [eye-height study](Architecture/starter-eye.png). These are geometry-only views with all proposed zones visible; they are not the production game or a correctly gated Day-1 screenshot.
