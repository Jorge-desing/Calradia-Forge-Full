# CODEX.md · OpenAI Codex Instructions for Calradia Forge

This file provides direct project instructions for the OpenAI Codex CLI, OpenAI Operator, and compatible AI agent runtimes operating on the Calradia Forge repository.

See [AGENTS.md](AGENTS.md) for the complete multi-agent specification.

---

## 1. Quick Reference & Core Rules

1. **Anti-Shadowing Constraint (CRITICAL)**:
   - Within `src/CalradiaForge.Mod` and its associated packages, never create a folder, class, or sub-namespace named `Campaign` or `Localization`.
   - These names can shadow `TaleWorlds.CampaignSystem.Campaign` or `TaleWorlds.Localization` when their namespaces are imported, breaking references such as `Campaign.Current`.
   - Approved names: `CampaignBehaviors`, `DataExtensions`, `CampaignExtensions`, and `LocalizationSync`.

2. **Target Framework Rules**:
   - `src/CalradiaForge.Mod`: `.NET Framework 4.7.2` (`net472`).
   - `src/CalradiaForge.Desktop`: `.NET 8.0 Windows` (`net8.0-windows`).
   - `src/CalradiaForge.Core` & `src/CalradiaForge.Sdk`: Multi-targeted `net472;net8.0`.

3. **Stateless CampaignBehaviors**:
   - Zero `SaveableTypeDefiner` inheritance in `src/CalradiaForge.Mod`.
   - Zero `SyncData(IDataStore)` persistence in mod campaign behaviors.
   - Do not serialize engine objects directly.

4. **Preserve Static Source Contracts**:
   - Automated reflection tests in `tests/CalradiaForge.Desktop.Tests` check literal source strings.
   - Never remove or rewrite required contract tokens (e.g. `"Editable Calradia Forge starting point"`).

5. **Dual-Language Documentation Parity**:
   - Technical documentation under `docs/` must have synchronized English (`<TOPIC>.md`) and Spanish (`<TOPIC>.es.md`) counterparts. Do not require bilingual copies of generated artifacts, append-only source records, or non-documentation assets.
   - Preserve the audit's established aliases: `DESKTOP.md` ↔ `ASSEMBLY_WORKBENCH.es.md` and `VALIDATION-<VERSION>.md` ↔ `VALIDACION-<VERSION>.es.md`; architecture and system-design guides use their own `.es.md` counterparts. `tools/Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` must fail if any counterpart is absent.

6. **Project Scope Staging & Zero External Garbage**:
   - At the end of each repository-changing request, objective, or plan, commit all intentional project changes attributable to it; capture the baseline and exclude unrelated pre-existing work. Intermediate steps need no separate commits.
   - Always include all intentional objective changes (code, UI, tests, docs, assets) in the scoped commit. See .agents/rules/git_sync_workflow.md.
   - Standing user authorization requires a scoped commit and normal upstream push at objective completion, without asking again. Honor an explicit no-push/local-only instruction for the current objective. Publication and force-push require separate authorization.
   - **Mandatory after every authorized push:** Verify the remote branch equals the pushed SHA and wait for its applicable Actions/check runs and commit statuses. If they fail, read their logs, correct the task-related cause, validate through BAT, push under the existing authorization and review the replacement SHA. Do not end the objective as complete after push alone, local success, an earlier green commit or a pending check. Report genuine external blockers as unverified. Include the final SHA and matching remote run links. This gate is direct session guidance and does not depend on loading a skill; details are in .agents/rules/git_sync_workflow.md.
   - Strictly exclude external debris: caches (`bin/`, `obj/`, `artifacts/`, `__pycache__/`, `*.pyc`), logs (`*.log`), game saves (`*.sav`), secrets (`.env`), and OS metadata.

### Harmony independence
Forge has no Harmony package, assembly, or distribution dependency. Optional diagnostics may reflect over an exact `0Harmony` runtime already loaded by another component, using verified public query members only; they must not load Harmony or mutate, reorder, or remove third-party patches. This is bounded observational evidence, not a compatibility or coexistence guarantee, complete detection of patch backends, conflict attribution from a shared target alone, or a sandbox/performance bound. Follow Rule F in `AGENTS.md` and `.agents/rules/calradia_forge_architecture.md`.

---

## 2. Command Playbook

### Mandatory Distribution Completion Gate
- Apply this gate in English, Spanish or any other language, independently of the request/response language and the interface/resource localization. Preserve applicable localized resources in packages and check existing localization parity; this does not authorize adding new languages or speculative translations.
- Before completing a release, milestone, significant code/feature update, or explicit packaging request, generate and verify the three current-version distribution ZIPs. An unchanged version, a successful commit, or green CI does not waive this gate.
- Follow [.agents/rules/auto_packaging.md](.agents/rules/auto_packaging.md). Require the canonical package pipeline and archive audit to succeed; compare each ZIP's SHA-256 with the generated manifest. Include absolute clickable ZIP paths and audit/hash evidence in delivery.
- An explicit user instruction not to generate ZIPs for the current request takes precedence. Documentation-only maintenance needs no packaging unless requested. If prerequisites fail or an output is missing, report packaging as incomplete instead of claiming delivery.
- Keep generated archives and packaging evidence in ignored `artifacts/`, outside commits. This gate does not authorize a version bump, publication, remote push, installation, or game launch.

### Build Solution (Release)
```powershell
dotnet build CalradiaForge.sln -c Release -v:minimal
```

### Run Mandatory Statelessness & Anti-Shadowing Verification
```powershell
cmd.exe /c "tools\Verify-CalradiaForge-StatelessBehavior.bat <nul"
```

### Run Full Test Suite (Core, ForgeWeave, Desktop MVVM, WPF Render Tests)
```cmd
cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause <nul"
```

### Run Core Subsystem Tests
```cmd
cmd.exe /c "tools\Run-CalradiaForge-Core-Tests.bat --no-pause <nul"
```

### Run Desktop Subsystem Tests
```powershell
cmd.exe /c "tools\Run-CalradiaForge-Desktop-Tests.bat --no-pause <nul"
```

### Run Desktop Render Subsystem Tests
```cmd
cmd.exe /c "tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause <nul"
```

### Packaging & Release Archives
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\package.ps1
```

### Live In-Game UI Review
- Open the Calradia Forge overlay on a singleplayer screen with `Settings.Hotkey`; the default is `F10`, and pressing it again closes the panel. F10 polling uses one rising-edge gate across `IsKeyPressed`, `IsKeyDown`, and `IsKeyDownImmediate`, rearming only after all three clear; other hotkeys keep the normal `IsKeyPressed` path. Details are in `.agents/rules/bannerlord_input_debug_agent.md` and `.agents/rules/calradia_forge_ui.md`.
- For an authorized live review, build both Client and Modding Kit profiles first, close the Modding Kit completely, launch Bannerlord from Steam with Calradia Forge enabled, and inspect the actual panel. Do not leave the game and Modding Kit open together or ask the user to open the game first.
- Before each source correction, close Bannerlord and the Modding Kit and end/reset Ordenador/Computer Use; keep them closed through edits and builds. After each live check, close Bannerlord and end/reset the Computer Use session before another correction or task completion.
- Treat source audits and Resource Browser import as separate from live rendering and interaction evidence.
- **Campaign Rule Builder interaction evidence:** The selected row, editor fields, and sample preview can represent the mutable rule currently being edited; cycling its event/action changes that same rule live. A visible row or changing label does not prove that `Add rule` added the user's intended rule, that it appeared as an additional row, or that the draft was saved. In this live review, the user reported that the intended new rule never appeared while the active editor row remained visible. Treat the cause as unresolved: confirm the Add command was received, compare visible row count and stable IDs before/after, and verify `Save draft` separately. Never infer an Add click or save from row visibility.

---

## 3. Codex Integration Surfaces

### 3.1 Codex Computer Use Compatibility (Windows 10)
- Compatibility layer located at `CodexCaptureCompat/`.
- Provides an x64 `version.dll` proxy for `codex-computer-use.exe` that bridges the missing Windows 10 `IGraphicsCaptureSession3` border interface.
- Manage and test via:
  ```powershell
  # Check status of compiled artifacts and processes
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action Status

  # Run installation and validation suite
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action Test
  ```

### 3.2 In-Game Codex / Encyclopedia Extender
- C# in-game encyclopedia extension located at `CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender`.
- Methods: `RegisterEntry()`, `UnregisterEntry()`, `GetEntry()`, `GetAllEntries()`, `Search()`, `AddBookmark()`, `RemoveBookmark()`, `IsBookmarked()`, `GetBookmarks()`, `RegisterFilter()`, `Filter()`.

### 3.3 Google Antigravity SDK Autonomous Agents Architecture & Token Compaction
- Optional autonomous-agent integration in `agents/` using the **Google Antigravity SDK** (`google.antigravity`); it is developer tooling, not a Calradia Forge or Bannerlord runtime dependency.
- Configures one coordinator and five specialist roles: `ForgeMasterAgent`, `ForgeArchitectAgent`, `StatelessBehaviorAuditor`, `DesktopWpfSpecialist`, `DocLedgerAgent`, and `BugHunterAgent`.
- Python orchestration uses `CoALAAgentMemory` from `agents/memory.py`; the C# SDK's `ForgeAgentMemory` is a distinct in-game runtime service.
- **Token Compaction (`ForgeTokenCompactor`)**: Tool-specific patterns summarize supported output and retain recognized diagnostic lines; tests cover representative fixtures, not every compiler, test runner, locale, or failure format. Token estimates and reduction ratios are heuristic and input-dependent, not guaranteed targets. Use the original output when exact evidence is required.
- Presets configure selected token thresholds via `--compaction-preset [ultra (8k) | balanced (16k) | deep (32k)]`; `--raw-tools` bypasses distillation. Raw logs are optional and available only when requested, non-empty, and successfully written to `artifacts/agent-runs/`.
- Run the autonomous CLI with `tools\Run-CalradiaForge-Agents.bat [audit|verify|docs|architect|run]`; it always uses the repository `.venv` interpreter and gives setup instructions if that environment is missing.
- Offline mode can invoke selected repository tools locally without the optional SDK; it does not run cloud agents. Online execution requires the SDK, credentials, and offline mode disabled. The `--agents` test profile needs the SDK because it tests SDK configuration; local execution does not prove cloud execution.
- Install the optional profile with `tools\Setup-CalradiaForge-Python.bat --agents --no-pause`; verify with `tools\Run-CalradiaForge-Python-Checks.bat --ci --agents --no-pause`.
- Technical specifications: `docs/AUTONOMOUS_AGENTS.md` and `docs/AUTONOMOUS_AGENTS.es.md`.

---

## 4. Skills Taxonomy & Gateways

When updating agent knowledge, ground lessons in commit diffs and current source, distinguish pending working-tree changes from validated committed behavior, and retain evidence limits. Read `.agents/skills/calradia-forge-dev-workflow/references/recent-commit-lessons.md` for persistence, failure tests, CI and distribution. Historical test counts and timings are not universal thresholds.

For SDK packages, .NET templates and static content, follow
`.agents/rules/developer_onboarding.md` and the `calradia-forge-dotnet` onboarding
reference. Keep package/fixture evidence separate from public publication and live
engine loading or rendering; if a check cannot be observed, report it as pending or
unverified. Consumer modules target net472, resolve licensed TaleWorlds references
locally, and must not redistribute TaleWorlds assemblies or the Forge SDK runtime
already supplied by Forge. ForgeWeave, hooks and whole-method replacement have
distinct contracts; shared-target metadata is a review signal, not proof of a
conflict. At objective completion, consolidate validated lessons in project
specialists and mirror shared instructions in AGENTS.md, CODEX.md and GEMINI.md.
Preserve upstream snapshots and unrelated staged work.
For periodic work that can safely be deferred, use `ForgeTimeSlicer.ShouldProcess`
with a stable entity ID; this does not reduce the cost of scanning the source
collection or guarantee even bucket sizes. Measure the complete callback before
claiming an allocation budget or zero-allocation behavior.

When working with skills in `.agents/skills/`:
- Use `calradia-forge-dotnet` for C# / MSBuild tasks.
- Use `/calradia-forge-docs` for documentation tasks (governed by `.agents/rules/calradia_forge_docs.md`).
- Use `calradia-forge-dev-workflow` for general engineering lifecycle.
- Never overwrite protected skills `using-dotnet`, `superpowers`, and `docs-generator`.

## Specialist Skill Routing

Use the specialist that matches the task after the `calradia-forge-dev-workflow` gateway:
- Crash and lifecycle triage: `.agents/skills/debugging-master/SKILL.md`.
- Measured runtime cost, allocations, and time-slicing: `.agents/skills/performance-hunter/SKILL.md`.
- Tactical combat behavior-tree proposals: `.agents/skills/game-ai-behavior-trees/SKILL.md`.
- Campaign callback cadence and simulation scheduling: `.agents/skills/discrete-event-simulation/SKILL.md`.
- Autonomous-agent routing and output compaction: `.agents/skills/multi-agent-orchestration/SKILL.md`.
- Gauntlet/WPF presentation, accessibility, and F10 integration: `.agents/skills/game-ui-design/SKILL.md`.

---

## 5. Antigravity Modular Rules Index & Compliance

While Google Antigravity discovers `.agents/rules/*.md` automatically via directory walking, OpenAI Codex CLI sessions MUST reference and comply with the domain rules organized in functional clusters:

1. **Architecture & Core**: `calradia_forge_architecture.md`, `bannerlord_architecture.md`, `calradia_forge_sdk.md`, `calradia_forge_forgeweave.md`, `modding_environment.md`.
2. **Bannerlord Gameplay Systems**: `bannerlord_campaign_behavior.md`, `bannerlord_save_system.md`, `bannerlord_shared_patterns.md`, `bannerlord_economy_trade_architecture.md`, `bannerlord_kingdom_diplomacy.md`, `bannerlord_settlement_rebellion.md`, `bannerlord_crime_underworld.md`, `bannerlord_character_development.md`, `bannerlord_clan_succession.md`, `bannerlord_quests_dialogues.md`, `bannerlord_audio_system.md`, `bannerlord_siege_mechanics.md`, `bannerlord_combat_ai_formations.md`, `bannerlord_mobileparty_spawner.md`, `bannerlord_map_visuals.md`, `bannerlord_missionview_hud.md`, `bannerlord_mission_lifecycle.md`, `bannerlord_inventory_barter.md`, `bannerlord_gamemodels_architecture.md`, `bannerlord_items_crafting_architecture.md`, `bannerlord_troop_character_architecture.md`, `bannerlord_xml_overrides.md`, `bannerlord_xml_schemas.md`, `bannerlord_localization.md`.
3. **UI & User Experience**: `calradia_forge_ui.md`, `gauntlet_architecture.md`, `bannerlord_input_debug_agent.md`.
4. **Documentation & Ledger**: `calradia_forge_docs.md`, `docs_generation_workflow.md`.
5. **Verification & Distribution**: `calradia_forge_verification.md`, `distribution_safety.md`.
6. **Codex & Multi-Agent Compatibility**: `codex_compatibility.md`, `development_workflow.md`, `developer_onboarding.md`.
7. **Packaging & Git Sync**: `auto_packaging.md`, `calradia_forge_packaging.md`, `packaging_version.md`, `git_sync_workflow.md`.

*Instruction for Codex:* Before editing C#, XML, WPF, or automation scripts, read the corresponding rule file from `.agents/rules/<rule>.md` to maintain full project compliance.

### Explicit follow-up push authorization
A later user request to push supersedes an earlier no-push instruction for the completed objective. Deliver its scoped commits and requested guide corrections, then review the exact remote SHA and repair task-related CI failures under the same authorization. Do not request permission again or include unrelated concurrent work. Publication and force-push remain outside this authorization. See `.agents/rules/git_sync_workflow.md`.
