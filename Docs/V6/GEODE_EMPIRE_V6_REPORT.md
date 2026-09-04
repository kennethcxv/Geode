# Geode Empire V6 — Living Report

Authoritative brief: `GEODE_EMPIRE_V6_PRODUCTION_ALPHA.md` (repo root). V5 baseline: commit `5a79e07` ("Complete V5 final QA"), report in `Docs/GEODE_EMPIRE_V5_FINAL_REPORT.md`.

## Current phase
- V6.0: V5 baseline + authoritative V6 document + baseline screenshots (this milestone).
- Next: V6.1 material pipeline + geode hero quality (priority order from §111: geodes, materials, machines, lighting, cracking/reveal, NPCs, checkout, onboarding/UI, processing feel, audio/VFX, economy/career, QA/performance).

## Baseline (negative) screenshots
- `Docs/V6/baseline/` — player-camera captures of the V5 saw, rough geode, opened geode, bench, wash, polish, appraisal, checkout and showroom at 30 cm / 60 cm / interaction / room distance. These are the pictures V6 must unmistakably surpass (§129).

## Defects discovered
- (V5 baseline, from the owner's screenshots and the captures above) boxy dark saw; dough-like rough geode; muddy, shallow, sparse opened geode; colour-only material differences; mannequin customers; abstract checkout.

## Measurements
- Editor perf at a fixed 1080p game view during the retail stress: ~850 draw calls, 150-180 set-pass calls, 5.7 M triangles, 8.5 M vertices, 730 MB allocated / 1.77 GB reserved, 16-33 fps (M2, 8 GB).

## Tests added
- (none yet in V6; V5 suite 35/35 is the regression floor)

## Experiments / failed hypotheses / reverts
- (none yet)

## Known-good milestone commits
- V6.0: (this commit)

## Remaining work
- Everything from V6.1 onward per the brief.

## Final acceptance status
- Not started.
