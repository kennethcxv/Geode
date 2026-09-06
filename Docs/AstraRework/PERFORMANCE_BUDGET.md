# Performance budget

Provisional production limits, not measured acceptance. Development hardware is an 8 GB Apple M2 Mac. Intended Steam minimum hardware remains to be validated in standalone; the Mac is a constrained development check, not an automatic product minimum. Target is smooth 60 FPS with stable interaction pacing.

| Resource | Initial limit / target | Verification |
|---|---|---|
| Frame time | 16.67 ms target; standalone p95 ≤ 16.67 ms, p99 ≤ 25 ms in representative play | Warmed captures in starter, processing and populated mature layouts; report resolution, quality, hardware and frame distribution |
| Interaction stalls | No avoidable generation/UI main-thread task above 4 ms; no interaction frame above 50 ms | Crate open, first strike, reveal, wash, saw, thumbnail/UI and save markers; exclude tool compilation frames explicitly |
| Scene geometry | ≤ 1.5M visible triangles at normal player camera; aim ≤ 750k in starter | Standalone render statistics and camera captures |
| Hero workstation | 12–25k visible triangles including movable parts; simple bench ≤ 10k | Blender topology / Unity mesh counts and near-view comparison |
| Repeated furniture / props | Shelves 2–5k triangles; small repeated props 100–800; hero handheld tool 2–6k | Aggregate cost at maximum intended stock density |
| Draw calls | Aim ≤ 350 batches / ≤ 150 SetPass at mature normal view | CPU/GPU profiling; shared materials and instancing |
| Materials | 2–4 slots per complex station, 1–2 per repeated prop | Import audit; no cloned materials per stock instance |
| Lighting | At most 4 contributing realtime local lights per visible zone, at most 2 shadowed; one main directional light | Frame debugger; measured shadow atlas and light overlap |
| Transparency | At most 2 meaningful glass/water layers over a hero; no full-room stacked transparent effects | Overdraw capture and GPU timing |
| Geode heroes | One detailed handling hero; target 40–70k triangles, 200–450 visible crystals depending on family | Per-family/size complexity and close-view quality; no density added without aggregate measurement |
| Shelf specimens | Target 4–12k triangles each at shelf distance, 24–48 visible specimens depending on view; LOD/reuse required | Mature stock stress, deterministic identity and LOD-transition captures |
| NPCs | Starter 3 simultaneous customers; prototype later 6–8 only after route/performance gate; 12–20k triangles each at interaction distance | Enter/browse/queue/serve/exit stress with stalls, overlaps, abandonments and frame percentiles |
| Physics | Simple static/compound proxies; no detailed visual mesh used as convex; ≤ 128 vertices per new generic convex proxy unless justified | Cooking logs, proximity/contact checks, Unity profiler. Preserve verified 129-vertex half proxy (254 faces) |
| Active rigidbodies | Aim ≤ 30 awake under normal handling, sleeping stock otherwise | Crate/stock/reveal stress and settling metrics |
| VFX | Small bounded pools; impact/debris normally ≤ 100 particles; no per-strike instantiation or GC | Burst and sustained interaction captures |
| Texture memory | Aim ≤ 512 MB resident art textures in mature scene; common atlases 1–2k, limited 4k heroes only if measured necessary | Platform-format import, mip/streaming audit and standalone memory |
| Process memory | Development standalone target ≤ 2 GB steady, peak ≤ 3 GB during transitions | Warm full-career snapshots; distinguish Unity Editor/Blender overhead |
| CPU generation | Expensive deterministic work off the critical interaction frame; bounded install/upload; no delay used to conceal a hitch | Actual worker/main-thread markers, cancellation/cache ownership and deterministic output tests |

Baseline: full EditMode suite took 847.81 seconds; 134/135 passed. HammerVsSawReport timed out after 371.57 seconds against the 180-second runner limit. Four direct samples (seeds 20000–20003) took 187–255 ms for whole geometry and 496–776 ms for one saw piece in this Editor. Piece construction repeats expensive noise-backed surface queries over 25 × 96 rings with two 18-step searches per point. This is an unresolved production responsiveness concern, not merely a test timeout. Preserve the economic sample/assertions. A temporary floor-reuse/inlining noise trial did not improve its scoped timing and was not committed; the comparison does not certify cross-build performance.

Crate opening baseline included 202.5/99.6/94.8 ms frames after the known MCP compilation frame. PerfProbe's memory delta is not an allocation counter. Replace that evidence with precise markers and warmed standalone captures before acceptance. Do not hide stalls behind lid or fracture delays.

Run heavy Blender generation/rendering, Unity stress/tests and builds sequentially on this machine. Remote concept generation can continue independently. Revisit numerical budgets when actual targets are built; document the evidence for any revision instead of quietly weakening the gate.
