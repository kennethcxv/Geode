# Geode Empire — Project Instructions

Durable development rules for every Claude Code session in this repository.
Read fully before doing any work. These rules override default behavior.

## Repository layout

- Repository root: this directory (`/Users/kenneth/Documents/GitHub/Geode`).
- Unity project root: `./Geode`. Open, build, test and run the Editor from there, never from the repo root.
- Blender tooling: `./Tools`.
  - `Tools/blender.sh` — wrapper that execs the installed Blender binary. Always launch Blender through it.
  - `Tools/Blender/` — reusable Blender Python (bpy) generators and pipeline scripts.
  - `Tools/Blender/smoke_test.py` — reference generator and pipeline smoke test. Keep it working; treat it as the template for new generators.
  - `Tools/Blender/Output/` — scratch output for generators. Git-ignored.
- Editor-only solution/project files (`*.slnx`, `*.csproj`) are generated and git-ignored.

## Engine and toolchain

- Engine: Unity 6.6 (`6000.6.x`) with the Universal Render Pipeline (URP). Do not switch render pipelines.
- Unity CLI (`unity`, installed at `~/.unity/bin/unity`) and the Unity Pipeline package are available.
- Unity MCP (`unity-editor-mcp` tools) is connected to the live Editor.
- Blender 5.2.1 LTS is installed and is an automated asset-generation dependency, not an interactive authoring tool.
- Input: the Unity Input System package (`InputSystem_Actions.inputactions`), not the legacy Input Manager.

### When to use Unity CLI vs Unity MCP

- Use the Unity CLI directly when it is the efficient route: batch operations, builds, running the test runner, project-level commands, anything scriptable that does not need Editor UI state.
- Use Unity MCP when Editor-level inspection or manipulation is appropriate: reading the Console, inspecting the hierarchy and components, editing scenes/prefabs/serialized fields, entering Play Mode, capturing views, running C# in the live Editor.
- Do not hand-edit `.unity`, `.prefab`, or `.asset` YAML when the CLI or MCP can make the change safely.

## Source of truth and verification

- The running Unity Editor is the source of truth for whether gameplay or editor changes actually work. Compiling is not proof; the Editor behaving correctly is.
- Regularly inspect the Unity Console (via MCP) during and after changes. Fix errors and meaningful warnings rather than merely reporting them.
- Verify significant gameplay work in Play Mode before calling it done. Observe behavior, check the Console, then stop Play Mode.
- Before declaring any feature complete, test it. Do not assume code correctness from reading it.
- Prefer fixing root causes over hiding errors (no blanket try/catch, no suppressing warnings, no `#pragma` silencing to make the Console quiet).
- Use the Unity Test Framework for logic that benefits from automated tests; keep tests fast.

## Blender asset pipeline

- Blender is used primarily headlessly through Python/bpy:
  ```sh
  ./Tools/blender.sh --background --python Tools/Blender/<script>.py [-- args]
  ```
- Store reusable generators under `Tools/Blender/`. One script per asset family, parameterized, importable.
- Prefer deterministic procedural generation: fixed seeds, no dependence on wall-clock time or Blender session state, re-runnable with identical output.
- Generated Blender output must not be committed unless it is an intentional production asset. Production assets go under `Geode/Assets/` in an appropriate folder; scratch output stays in `Tools/Blender/Output/`.
- Game-ready exports must be Unity-ready:
  - Scale: 1 Blender unit = 1 meter; `apply_unit_scale=True`, `FBX_SCALE_ALL`.
  - Transforms applied (location, rotation, scale all identity) before export.
  - Sensible pivot: origin at the base for props/buildings, at the center for spinning/floating objects.
  - Orientation: export with `axis_forward='-Z'`, `axis_up='Y'`.
  - Clean topology (no loose verts, no non-manifold artifacts unless intended), correct outward normals, flat vs. smooth shading set deliberately.
  - UVs present for anything that will take a texture or lightmap.
  - Export FBX (see `smoke_test.py` for the exporter call with a native-exporter fallback).
- Do not leave Blender running when it is not required. Headless runs exit on their own; kill any stray Blender process you started.

## Hardware budget: Apple Silicon M2, 8 GB RAM

Development happens on an M2 Mac with only 8 GB of unified memory. Both the workflow and the resulting game must respect that.

- Avoid running heavyweight processes simultaneously (Editor + Blender + a build + a browser is too much). Run Blender jobs sequentially and let them finish before continuing Editor work.
- Avoid unnecessarily large textures (default to ≤ 1024, use ≤ 2048 only with a reason), high polygon counts, dense lightmaps, large baked caches, or big Addressables/AssetBundle builds.
- Prefer lightweight rendering: URP defaults, modest shadow distance/cascades, no unbaked-at-runtime heavy effects, no expensive post-processing stacks by default.
- Prefer procedural, instanced, and reused meshes/materials over many unique assets.
- Keep Editor imports cheap: compress textures, reasonable mesh import settings, no unneeded animation rigs.
- Watch memory use in Play Mode and profile before adding systems that allocate per frame.

## Architecture and scope

- Keep the game modular (clear assemblies/namespaces, small focused MonoBehaviours, ScriptableObjects for data) but do not overengineer hypothetical systems. Build what the current feature needs.
- Keep assets and systems suitable for eventual Windows and macOS desktop/Steam builds: no platform-exclusive APIs without a fallback, keyboard/mouse plus gamepad input, standard desktop resolutions, no mobile-only assumptions.
- Use the PC render pipeline asset for desktop targets; the Mobile RP asset in `Assets/Settings` is template residue and is not a target.

## Git

- Make frequent Git checkpoints after meaningful, known-working milestones (feature works in the Editor, Console is clean, tests pass). Commit with clear messages.
- Never commit Unity `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, build output, IDE files, Blender scratch output, Python caches, or any other generated directory. `.gitignore` already covers these; extend it before adding a new generator or tool that creates output.
- Do not perform destructive Git operations (`reset --hard`, `push --force`, branch deletion, history rewriting, discarding uncommitted work) unless explicitly necessary and confirmed.
- Never modify or delete files outside this repository.

## Working style and autonomy

- For longer tasks, continue autonomously through the normal implementation → verify → debug → fix cycle. Do not stop to ask "should I continue?" between routine phases.
- Stop and ask only for: a genuine product/design decision, a credential requirement, a purchase, an external publishing action, a destructive external action, or a blocker that cannot reasonably be resolved.
- When a feature is done, report what was built, how it was verified (Console state, Play Mode result, tests), and what was committed.

## Project status

- Geode Empire game implementation has not started. The Unity project is a fresh URP template (`SampleScene`, `TutorialInfo` readme files). Do not begin game implementation unless a task explicitly asks for it.
