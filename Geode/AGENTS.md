# AGENTS.md

<!-- KitWright Unity managed project skills -->
<!-- KitWright Unity project skill versions: unity-mcp-workflow@1.0.0 -->

# KitWright MCP for Unity Project Guidance

This section is managed by KitWright MCP for Unity. Everything between the begin and end markers is regenerated on each sync; edit outside this block.

## Installed project skills

- `unity-mcp-workflow` v1.0.0 - Efficient workflow for using Unity MCP to edit, import, compile, inspect, and test Unity projects.

## Codex workflow rules

- Prefer project-local KitWright skills under `.codex/skills/`.
- Use `execute_code` as the primary Unity automation tool. For new snippets, include `using KitWright.Editor.Tools.Scripting;`, implement `IKitWrightCommand`, and use `ctx.RegisterObjectCreation` / `RegisterObjectModification` / `DestroyObject` so changes participate in Undo automatically.
- Confirm the Unity project root, active scene, and real object/prefab/asset path before edits. Treat user-provided object names as hints, not paths.
- Inspect Unity objects through MCP before changing user-named scene or prefab targets. Carry the returned `instanceId` into follow-up calls (`find_method=by_id`) instead of re-resolving by name.
- Tool returns are structured JSON (`{success, message, data}` / `{success: false, code, error, data}`). Branch on `code`, not free-form text.
- Set component fields with `set_component_properties` — it picks up `[SerializeField] private` fields and accepts Object references as `{"fileID": <instanceId>}` or `{"assetPath": "Assets/..."}`.
- Read editor state through dedicated tools (`get_selection`, `get_prefab_stage`, `get_tags`, `get_layers`, `get_build_settings`); use `execute_menu_item` before falling back to ad-hoc `execute_code`.
- Never edit `.unity`, `.prefab`, or `.asset` files with shell text tools or patches; use Unity MCP / Editor APIs for scenes, prefabs, and ScriptableObject assets.
- Save only the scene or prefab assets intentionally modified, then read back exact values.
- With default `core` exposure, use the focused workflow tools. With default `full` exposure, prefer specific MCP tools for simple editor operations.
- `execute_code` refreshes the asset database and waits for compilation before running. For other tools that depend on freshly compiled code, still call `request_recompile` after external script edits.
- In `execute_code`, null-guard every lookup and return explicit missing path/object/component messages; do not run self-healing fallback loops.
- For Unity object references, do not use `??=` for lazy rebinding; use explicit `if (field == null) field = Resolve();`.
- After code or resource edits, exit Play Mode if needed, call `request_recompile`, `wait_for_compilation`, then read compilation or console errors.
- `request_recompile` is rejected while Unity is in Play Mode. Call `exit_play_mode` first, then retry.
- After `enter_play_mode`, the HTTP server briefly drops while Unity reloads the domain. Poll `tools/list` or `get_reload_recovery_status` until it responds again before issuing the next tool call.
- If recompilation triggers a domain reload or interrupts a request, treat the result as unknown until `get_reload_recovery_status`, compilation checks, and MCP readback confirm it.
- Avoid changing `Library/`, `Temp/`, `Logs/`, or `obj/`.

## Project

- Project root: `/Users/kenneth/Documents/GitHub/Geode/Geode`
- Product name: `Geode`

## Notes

- Re-run `KitWright > Project Skills` after changing selected skills or platforms.
<!-- /KitWright Unity managed project skills -->

## ASTRA CAPACITY / INTERRUPTION RECOVERY

The active authoritative goal is the full ASTRA PROJECT REWORK + STEAM READINESS master specification.

This work is expected to survive:
- "Selected model is at capacity" failures
- API/server overload
- network interruption
- Codex restart
- context compaction
- Unity domain reload
- Unity MCP timeout
- Blender MCP timeout
- machine sleep/restart
- user absence

These events are interruptions, NEVER completion.

Maintain Docs/AstraRework/PROGRESS.md as the durable recovery state.

Update PROGRESS.md:
- after every meaningful milestone
- before every substantial/high-risk operation
- after discovering an important defect
- after every known-good commit
- before context compaction
- whenever remaining context reaches about 30%

PROGRESS.md must always contain:
- authoritative master-spec path
- active goal
- current branch
- latest known-good commit
- current phase
- completed milestones
- work currently in progress
- exact unfinished operation
- unresolved defects
- modified/uncommitted files
- Unity state
- Blender state
- test results
- player-data isolation state
- generated concept-art paths
- important architectural/design decisions
- exact next actions

Before long or risky implementation work, create a coherent checkpoint whenever practical.

If the model/API returns "Selected model is at capacity":
- do not reinterpret the goal
- do not roll back work
- do not change model automatically
- preserve the current state in PROGRESS.md if execution is still available
- on the next successful request, reread PROGRESS.md and resume the exact interrupted operation

After any Codex restart or context compaction:
1. read GEODE_EMPIRE_ASTRA6_FULL_PROJECT_REWORK_STEAM_READINESS_MASTER_SPEC.md
2. read Docs/AstraRework/PROGRESS.md
3. read Docs/AstraRework/PLAN.md
4. read Geode/AGENTS.md
5. inspect git status/log
6. inspect Unity state
7. inspect Blender state if relevant
8. verify any active QA isolation session
9. resume the exact unfinished operation

Never begin a duplicate test/build because the previous MCP/tool request timed out. First determine whether the underlying process is still running.

Never declare the full rework complete because a Codex turn, context window, capacity allocation, or session ends.

Only the master specification's Definition of Done may terminate the goal.

Also change the compaction policy for this project:

- Do not wait until ~250K tokens.
- When approximately 30% of context remains, finish the current atomic operation, update PROGRESS.md completely, make a clean checkpoint if appropriate, compact, reread the recovery sources above, and immediately continue the same goal.
- Treat compaction as maintenance, never as completion.

## Automated player-data boundary

During the active autonomous Astra rework, keep `AstraQaSession.AutomationGuardEnabled` armed. Its project-specific setting survives Editor and machine restart. It cancels unprepared Play and rejects writes to the real player directory; authorized QA still requires Prepare → exact validation → Enter → exit/Finish → original hash comparison. Verify the guard and any active manifest during recovery. Do not end this guard while autonomous production/QA remains active. The explicit `GeodeEmpire/Astra/End Automation Save Guard` command returns the Editor to normal career use when automation is finished.

Tests must restore the previous `SaveSystem.DirectoryOverride` in `finally`, never unconditionally clear an outer isolation session. After any unexpected Play or protected-file change, record the timestamp, actual hashes and proven field differences, preserve newer player files, and distinguish a proven defect from an unknown initiator.
