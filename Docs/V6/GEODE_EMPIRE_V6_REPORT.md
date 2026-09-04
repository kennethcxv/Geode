# Geode Empire V6 — Living Report

Authoritative brief: `GEODE_EMPIRE_V6_PRODUCTION_ALPHA.md` (repo root). V5 baseline: commit `5a79e07` ("Complete V5 final QA"), report in `Docs/GEODE_EMPIRE_V5_FINAL_REPORT.md`.

## Current phase
- V6.1 material pipeline + geode hero quality (in progress). V6.0 (`1e923ff`) holds the baseline; V6.1a (`07186f9`) delivered the generated PBR pipeline (Tools/Blender/gen_textures.py, 16 tileable families, import rules, set-aware workshop materials).
- V6.1 plan (from a five-agent code map + synthesis, executed in order): S1 specimen tiles + Blender review renders; S2 GeodeShell triplanar detail normal/mask (done, first pass); S3 five-layer rim profile (done, first pass); S4 sawn/polished/wet/dust responses + SSAO keyword (partly done); S5 exterior macro asymmetry, resting flat spots, pits (done, first pass); S6 satellite cavity lobes, deeper display half, thinner sectors (lobes + depth done); S7 conchoidal rim ripples/terraces, more rim rings (ripples done); S8 clustered buried crystal growth, no floating/crossing tips (clustering + scale done; containment pending); S9 crystal archetype remodel + LOD budgets in Blender; S10 per-mineral crystal identity (luster/F0, transmission, zoning, inclusions); S11 SpecimenVisual hygiene and perf gating; S12 machine remodel scaffolding (worn sets, hard-surface helpers, saw pilot); S13 acceptance gate (geode matrix, tests, standalone, report).

## Baseline (negative) screenshots
- `Docs/V6/baseline/` — player-camera captures of the V5 saw, rough geode, opened geode, bench, wash, polish, appraisal, checkout and showroom at 30 cm / 60 cm / interaction / room distance. These are the pictures V6 must unmistakably surpass (§129).
- `Docs/V6/v61/` — the same four hero seeds (amethyst 7D1, vanadinite ACC, agate E53, rhodochrosite 8BF) after V6.1b, in four states each: quarry-dirty, washed, opened dusty, rinsed (two angles). Compare `v5_b_open_7D1_a.jpg` (grey chunks on a flat lavender floor, heavy dark rim) with `v61_b_open_7D1_a.jpg` (purple points with pale bases over a purple druse floor inside a grey chalcedony ring with fine wavy bands), and `v5_b_rough_7D1_b.jpg` (dough) with `v61_b_rough_8BF_b.jpg` (pitted, knobbly rind).

## V6.1b — geode hero pass (2026-09-04)
- Materials/tiles: `gen_textures.py` gained `worley_vec` (offset to the nearest seed), a botryoidal `rind_weathered` (spherical knobs at two scales, chalky dome tops, dark creases, sharp pits with dry rims, sparse hairline cracks) and a new `druse` tile (a mosaic of six-sided terminations at random heights and turns, glassy, near-white; the shell shader tints it). Normal strengths are now physical: `tan(slope) = 2 * strength * height-per-pixel`, so rind 16 / fracture 10 / cavity 8 / druse 18 (the old 1.0-2.2 gave about four degrees of tilt, which is why V5 relief read as clay).
- Shell shader: druse detail set blended under carpets by `_CavityDruzy`; the cut face is chalcedony-dominant (thin exterior skin, weathered matrix rind 0.06-0.32, grey-blue chalcedony with coarse + fine wavy bands, mineralised inner edge), sawn faces share the layering; rock flour is a pale powder in the low ground; seam / lip / guide widths scale with the rock radius; exterior mineral hints are a tint, not paint; pits come from the rind tile's occlusion; a `DepthNormals` pass (SSAO's normals prepass) in both geode shaders; the triplanar frame fixed (x plane samples `zy`, u mirrored on back faces, tangent x flipped); `_GeodeDebug` modes 1-5 (albedo / tile albedo / y-plane uv / blend weights / normal).
- Crystal shader: body keeps more of the surface tint (no more black glass), pale milky bases under zoning, ambient scatter, lighter dust, DepthNormals pass, glints from the smooth noise channel.
- Mesh: 96x30 rings per half, 12 rim rings (13,632 triangles per half shell); displacement band-limited to about six ring samples per noise cycle (lump 1.3-2.2, bump 2.0-2.4, billow 1.8-2.4 with squared domes, pits 2.2); satellite lobes etc. from V6.1a kept.
- Asset builder wires every detail set from `Textures/Generated` (`WireDetailSet`); the legacy `T_Rock` crack lines softened.
- Harness: `hero_bench2.sh <seeds>` captures dirty / washed / opened-dusty / rinsed (sets `Condition.Cleaned = 1` and `Condition.Rinsed = true` then `RefreshCondition()`); `diag_streak*.sh` capture debug modes at 2560x1440 and crop the rock.

## V6.1c — crystal habits and carpets (S9, 2026-09-04)
- `gen_crystals.py`: prism striations dropped from the meshes (the shader's `_Striation` carries them), so a quartz point is 70 faces / 136 triangles instead of 178 faces; the botryoidal tile went from 3,546 to 906 faces; a new `crystal_quartz_termination` habit (short buried prism, tall six-face termination with alternating steep/shallow faces) = `CrystalArchetype.QuartzTermination` (25 archetypes, library rebuilt).
- Placement (`GeodeMeshBuilder.PlaceCrystals`): carpets sample a 56x20 cell grid (others 40x14), points 0.56 of the family scale, size variance 0.78-1.28 with 4% giants x1.3-1.7 and 20% runts, quartz points on the fringe swap to terminations (75% on the fringe, 25% in the cores), tilt <= 12 degrees, spacing 0.36 so points touch, fill probability x1.15; burial capped by the local wall thickness (`Cell.Thickness`). The hero amethyst went from 384 loose chunks to 788 packed points at fewer triangles than before.
- Crystal shader: every light-keyed term (glints, transmission, rim, the new cloudy scatter fill) now accumulates over the additional lights through `LIGHT_LOOP_BEGIN/END` (the bench lamp is an additional light; the sun never reaches the bench), milky bodies keep their hue, the pale-base fade sits only in the bottom 40% of a point.
- Shell: the druse floor is satin (smoothness 0.62 under carpets) and paler/less saturated than the points, milkier for cloudy specimens (`SpecimenVisual` blends the druse colour toward white by clarity).
- Perf with the 788-point amethyst opened in the player's view at the bench (Editor, fixed 1080p): 287 draw calls, 58 set-pass calls, 1.50 M triangles across all passes, 637 MB allocated, 56-59 fps.
- Captures: `Docs/V6/v61/v61c_*.jpg` (amethyst 7D1 opened two angles + dusty, rhodochrosite, the cloudy thin-shelled amethyst 2B77E opened two angles + rough).

## Defects discovered
- (V5 baseline, from the owner's screenshots and the captures above) boxy dark saw; dough-like rough geode; muddy, shallow, sparse opened geode; colour-only material differences; mannequin customers; abstract checkout.
- V6.1b root causes behind the "dough" and "fur":
  1. `M_GeodeShell` had no `_RindAlbedo` assigned (the tile's colour breakup never reached the exterior). Fixed by wiring in `AssetLibraryBuilder`.
  2. Tile normal strength was about 8x too weak (see the formula above).
  3. The hero bench opened rocks under the full dust film and staged them clay-caked, so every crystal capture was grey; the bench now captures the washed and rinsed states too.
  4. Mesh noise above ~2.5 cycles per unit aliased into diagonal ridges along the quad splits (the `|n|` billow creases and a finer knob octave made it worse). Band-limited.
  5. Triplanar x-plane UVs were transposed and back faces unmirrored, so knobs lit as dents on some faces.
  6. The rocks had no `DepthNormals` pass, so SSAO (source DepthNormals) never saw them.
  7. The main "fur": `pits`, cavity glitter and crystal sparkle thresholded the noise texture's per-texel channel, and with `anisotropicFiltering = ForceEnable` white noise filters into streaks along the foreshortened axis. Every surface feature now uses the tile or a smooth noise channel.

## Measurements
- Editor perf at a fixed 1080p game view during the retail stress: ~850 draw calls, 150-180 set-pass calls, 5.7 M triangles, 8.5 M vertices, 730 MB allocated / 1.77 GB reserved, 16-33 fps (M2, 8 GB).
- V6.1b: shell 13,632 triangles per half; crystals per hero rock 84 (ACC) / 172 (E53) / 384 (7D1) / 578 (8BF, druzy); tile generation 7 s for four 1024 sets; EditMode 35/35 (twice, before and after the mesh change).

## Tests added
- (none yet in V6; V5 suite 35/35 is the regression floor)

## Experiments / failed hypotheses / reverts
- A finer knob octave on the mesh (`b3` at 4.6x the billow frequency) was added and removed the same session: it aliased on the 96-ring grid.
- Softening `T_Rock`'s crack lines and moving SSAO off the rocks were both tried as streak fixes before the anisotropic-noise cause was found; the first is kept (harmless, slightly cleaner coarse rinds), the second was never needed (source was already DepthNormals; the missing pass was the defect).

## Known-good milestone commits
- V6.0 `1e923ff`, V6.1a `07186f9`, V6.1b `b720789` (material pipeline + geode hero pass), V6.1c (this commit): crystal habits and carpets.

## Remaining work
- V6.1 remainder: S8 tilt toward clusters, S6 per-direction wall thinning, S7 terraces, S10 luster classes for the non-quartz habits, S11 SpecimenVisual hygiene + perf gate (LOD for opened rocks on shelves), S12 machine scaffolding, S13 acceptance gate (RunGeodeGate matrix over all 24 families, standalone, report). Known visual nits: the agate face's fracture relief is strong, the staged seam frost is still chalky at full stress, non-quartz carpets (calcite, fluorite) still use the V5 habits at V5 sizes.
- Then V6.2 machines .. V6.9 and the FINAL acceptance per the brief.

## Final acceptance status
- Not started.
