# AGENTS.md · Calradia Forge Agent Conventions & Codex Playbook

This document defines repository conventions, architecture constraints, validation workflows, and operational boundaries for AI coding assistants, autonomous agents, and OpenAI Codex CLI operating in this workspace.

---

## 1. Project Mission & Architecture

Calradia Forge is an advanced modding framework, live developer tooling suite, and standalone assembly workbench for **Mount & Blade II: Bannerlord**.

### Assembly & Target Framework Map

| Project | Target Framework | Primary Purpose | Key Constraints |
| :--- | :--- | :--- | :--- |
| `src/CalradiaForge.Mod` | `net472` | In-game Bannerlord module (`MBSubModuleBase`, Gauntlet UI panels, CampaignBehaviors) | Requires TaleWorlds assemblies; game-thread affinity; stateless behaviors. |
| `src/CalradiaForge.Core` | `net472;net8.0` | Core SDK, behavioral systems, telemetry, rule auditors, UI helpers | Zero TaleWorlds hard dependency in net8.0 mode; thread-safe registries. |
| `src/CalradiaForge.Sdk` | `net472;net8.0` | Public SDK surfaces, game-runtime cognitive memory (`ForgeAgentMemory`), API bridges | Clean decoupled contracts; no engine entity direct references. `ForgeAgentMemory` is distinct from Python orchestration's `CoALAAgentMemory`. |
| `src/CalradiaForge.Desktop` | `net8.0-windows` | Standalone desktop workbench (WPF MVVM, IPC pipe client, offline analyzer) | Strict UI thread isolation; static source contracts; tactical palette. |
| `CodexCaptureCompat` | Native C++17 / x64 | Windows 10 Computer Use screenshot compatibility layer (`version.dll`) | Local DLL proxy for `codex-computer-use.exe`; preserves system exports. |

---

## 2. Invariant Rules & Hard Constraints (NEVER VIOLATE)

### Rule A: Anti-Shadowing Constraint (GEMINI.md)
- **CRITICAL**: Never name a folder, sub-namespace, or class `Campaign` or `Localization` within `src/CalradiaForge.Mod` or associated packages.
- **Reason**: Doing so shadows `TaleWorlds.CampaignSystem.Campaign` or `TaleWorlds.Localization` when using directives are active, causing catastrophic compilation breakage across the solution (e.g. `Campaign.Current`).
- **Approved Names**: Use `CampaignBehaviors`, `DataExtensions`, `CampaignExtensions`, or `LocalizationSync`.

### Rule B: Stateless Campaign Behavior Contract
- Mod behaviors (e.g. `ClanCharacterProgressionBehavior`) must remain completely stateless regarding save persistence.
- **Zero SaveableTypeDefiner**: Never inherit from `SaveableTypeDefiner` in `src/CalradiaForge.Mod`.
- **Stateless SyncData**: Keep `SyncData(IDataStore dataStore)` completely free of `dataStore.SyncData(...)` serialization calls.
- **No Transient Objects**: Never serialize engine entities (`Hero`, `Settlement`, `MobileParty`) directly; resolve by `StringId` if transient references are needed.

### Rule C: Desktop Static Source Contracts
- Automated desktop contract tests (`tests/CalradiaForge.Desktop.Tests/Program.cs`) verify architectural rules via static source analysis (`File.ReadAllText`).
- Never delete or alter required contract tokens such as:
  - `"Editable Calradia Forge starting point"`
  - `"State-changing execution is unavailable from this guarded desktop route."`
  - `MaximumMeasurements = 64`
  - `Take(128)`
  - `File.Move(temporary, path, true)`
  - `"desktop-preferences.json"`

### Rule D: Distribution Safety Guidelines
- Never package raw proprietary game engine DLLs (`TaleWorlds.*.dll`) or user save files in release archives.
- Never include development/test batch scripts (`.bat`) or PowerShell scripts (`.ps1`) in user-facing distribution ZIPs. The sole runtime-launcher exception is `Desktop/Run-CalradiaForge-Desktop.bat` in the Desktop archive, required by its app-host-free distribution; do not generalize this exception to other scripts.
- Strip NTFS `:Zone.Identifier` alternate data streams from assemblies before distribution.

### Rule E: Project Scope Staging & Zero External Garbage
- **Chat-Scoped Commit**: At the end of each request, objective, or plan that changes repository files, commit all intentional project changes attributable to that complete request/objective/plan, including changes made by delegated agents. Intermediate steps do not need separate commits. Capture the working-tree baseline first; do not absorb unrelated pre-existing work. See .agents/rules/git_sync_workflow.md.
- **Full Objective Scope**: At objective completion, stage all intentional project modifications, features, tests, documentation, annexes, and assets attributable to that objective. Never omit valid task changes under the mistaken assumption that they are "external".
- **Default Commit and Push**: Standing user authorization requires a scoped commit and normal push to the configured upstream at the end of every repository-changing objective. Ask no additional permission. Honor an explicit no-push/local-only instruction for the current objective. Release publication and force-push require separate authorization.
- **Mandatory Post-Push Completion Gate**: After every authorized push, verify the remote branch matches the exact pushed SHA and inspect all applicable GitHub Actions, check runs and commit statuses for that SHA. Keep working until the checks finish. On failure, retrieve the logs, correct the task-related cause, validate locally through BAT, push the correction under the existing authorization, and review the new SHA again. Do not report completion merely because push succeeded or local tests passed. Report a genuine external blocker or inaccessible check explicitly as unverified; never call it passing. Include the final SHA and matching run links in the response. This rule applies directly in every session, even when no skill or modular rule is loaded. See .agents/rules/git_sync_workflow.md.
- **Strictly Prohibited (Zero External Garbage)**: Never stage or commit files external to the project:
  1. Build artifacts and caches (`bin/`, `obj/`, `artifacts/`, `.vs/`, `.idea/`, `__pycache__/`, `*.pyc`).
  2. Crash logs, dump files, and traces (`*.log`, `*.cfcrash`).
  3. Proprietary game engine binaries (`TaleWorlds.*.dll`) or user game saves (`*.sav`).
  4. Secrets, credentials, or environment files (`.env`, tokens).
  5. OS metadata (`Thumbs.db`, `.DS_Store`, `desktop.ini`).
- **Pre-commit Audit**: Always verify `git status --porcelain` to confirm that all staged items belong to the project and zero external garbage or caches are included.

### Rule F: Harmony Independence and Optional Observation
- Do not add Harmony/`0Harmony` package references, compile-time or runtime assembly references, bundled binaries, or a required Harmony installation to any Forge product.
- Optional diagnostics may inspect an exact, already-loaded `0Harmony` runtime through Forge-owned reflection over verified public query members. They must not load Harmony, patch/unpatch/reorder third-party code, or make Forge behavior depend on Harmony being present.
- Treat observations as bounded review evidence only. They do not prove coexistence, detect every patch backend, attribute a conflict from a shared target alone, or sandbox/bound Harmony's own synchronous CPU and memory work. See `.agents/rules/calradia_forge_architecture.md` and `docs/DEPENDENCY_FRAMEWORK_REVIEW.md`.

---

## 3. Essential Commands & Verification Playbook

### Mandatory Distribution Completion Gate
- This gate applies regardless of the language of the request, response, interface or packaged resources: English, Spanish and any other supported language. Do not omit applicable localized resources from the distribution; validate existing localization parity without inventing new translations or supported languages.
- At completion of a release, milestone, significant code/feature update, or an explicit packaging request, generate the three current-version distribution ZIPs before reporting completion. This applies even when the product version is unchanged; a commit or green CI is not a substitute for packaging.
- Follow [.agents/rules/auto_packaging.md](.agents/rules/auto_packaging.md): run the canonical packaging pipeline, require its archive audit to pass, and verify the SHA-256 of each output against the generated hash manifest. Report clickable absolute paths to all three ZIPs and the audit/hash evidence.
- Preserve an explicit user instruction not to generate ZIPs for the current request. Documentation-only maintenance does not independently trigger packaging unless requested. Do not silently skip packaging: report a failed prerequisite or missing archive as incomplete.
- Keep release ZIPs, staging trees, audit reports, and hash manifests in ignored `artifacts/`; never stage them in Git. Do not bump the version, publish a release, push, install, or launch the game merely to satisfy this gate. Commit intentional source/rule/documentation changes under Rule E.

Always execute commands with non-interactive flags and appropriate working directories.

### Build
```powershell
dotnet build CalradiaForge.sln -c Release -v:minimal
```

### Stateless Behavior & Acceptance Verification (Mandatory Gate)
```powershell
cmd.exe /c "tools\Verify-CalradiaForge-StatelessBehavior.bat <nul"
```

### Full Test Suite (Core, ForgeWeave, Desktop MVVM, WPF Render Tests)
```cmd
cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause <nul"
```

### Unit Test Execution by Subsystem
- **Core Tests**: `cmd.exe /c "tools\Run-CalradiaForge-Core-Tests.bat --no-pause <nul"`
- **Desktop MVVM & Protocol Tests**: `cmd.exe /c "tools\Run-CalradiaForge-Desktop-Tests.bat --no-pause <nul"`
- **Desktop Live Render Tests**: `cmd.exe /c "tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause <nul"`

### Release Packaging
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

## 4. Codex & Agent Compatibility Surfaces

### 4.1 OpenAI Codex CLI & Multi-Agent Standard
- This `AGENTS.md` and matching `CODEX.md` serve as the single source of truth for OpenAI Codex CLI and multi-agent systems.
- Project metadata and commands are registered in `.codex/config.json`.
- The rule file `.agents/rules/codex_compatibility.md` governs synchronization across agent environments.

### 4.2 Windows 10 Codex Computer Use Compatibility (`CodexCaptureCompat`)
- The workspace includes a dedicated Windows 10 compatibility layer for Codex Computer Use (`codex-computer-use.exe`).
- Resolves the missing `IGraphicsCaptureSession3` border interface by proxying `version.dll` and redirecting `FrameArrived` callbacks to Windows threadpool MTA worker threads.
- Managed via `tools/Manage-CodexCaptureCompat.ps1`:
  - Audit status: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action Status`
  - Run verification tests: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action Test`
  - Preview installation: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action WhatIf -HelperPath <PathToExe>`

### 4.3 In-Game Codex / Encyclopedia Extender (`ForgeEncyclopediaExtender`)
- In-game Bannerlord Encyclopedia / Codex extension API is implemented in `CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender`.
- Allows registering custom entries, categories, tags, dynamic filters, and quick bookmarks.
- Internal collection access is synchronized, but entry objects remain mutable and query methods may allocate result lists. Do not claim whole-API thread safety or zero allocations without measuring the specific usage path.

### 4.4 Google Antigravity SDK Autonomous Agents Architecture & Token Compaction
- The repository has an optional autonomous-agent integration in `agents/` using the **Google Antigravity SDK** (`google.antigravity`). It is developer tooling, not a Calradia Forge or Bannerlord runtime dependency.
- The configured roster is one coordinator plus five specialist roles: `ForgeMasterAgent`, `ForgeArchitectAgent`, `StatelessBehaviorAuditor`, `DesktopWpfSpecialist`, `DocLedgerAgent`, and `BugHunterAgent`.
- Python orchestration uses `CoALAAgentMemory` in `agents/memory.py`; the C# SDK's `ForgeAgentMemory` is a separate in-game runtime service.
- **Token Compaction (`ForgeTokenCompactor`)**: Tool-specific patterns summarize supported output and retain recognized diagnostic lines; tests cover representative fixtures, not every compiler, test runner, locale, or failure format. Token estimates and reduction ratios are heuristic and input-dependent, not guaranteed targets. Use the original output when exact evidence is required.
- Presets configure selected token thresholds via `--compaction-preset [ultra (8k) | balanced (16k) | deep (32k)]`; `--raw-tools` bypasses distillation. Raw logs are optional and available only when requested, non-empty, and successfully written to `artifacts/agent-runs/`.
- Run the autonomous CLI through `tools\Run-CalradiaForge-Agents.bat` with subcommands such as `audit`, `verify`, `docs`, `architect`, or `run "<prompt>"`; the launcher uses the repository `.venv` interpreter and never falls back to a global Python.
- Offline mode can invoke selected repository tools locally without the optional SDK; it does not run cloud agents. Online Antigravity execution requires the SDK, credentials, and offline mode disabled. The `--agents` test profile requires the SDK because it tests SDK configuration; local execution does not prove cloud execution.
- Install the optional profile with `tools\Setup-CalradiaForge-Python.bat --agents --no-pause`; it installs only into the repository `.venv`. Verify it with `tools\Run-CalradiaForge-Python-Checks.bat --ci --agents --no-pause`.
- Detailed architecture and usage guidelines: `docs/AUTONOMOUS_AGENTS.md` and `docs/AUTONOMOUS_AGENTS.es.md`.

---

## 5. Documentation Architecture & Dual-Language Parity

Documentation workflows are coordinated by the `/calradia-forge-docs` command and skill orchestrator (governed by `.agents/rules/calradia_forge_docs.md` and `.agents/rules/docs_generation_workflow.md`).
All technical documentation under `docs/` must maintain strict conceptual parity between English and Spanish:
- English source: `docs/<TOPIC>.md`
- Spanish counterpart: `docs/<TOPIC>.es.md`
- Never modify an English document without synchronizing its Spanish counterpart in the same changeset.
- Established parity aliases are `DESKTOP.md` ↔ `ASSEMBLY_WORKBENCH.es.md` and `VALIDATION-<VERSION>.md` ↔ `VALIDACION-<VERSION>.es.md`; architecture and system-design guides use their own `.es.md` counterparts. Require `tools/Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` to fail when a counterpart is missing.
- Major releases and architectural revisions are recorded in the immutable append-only ledger ("Registro de Mejoras", `docs/CalradiaForge-Registro-Mejoras-Rev*.docx`) backed by a SHA-256 cryptographic hash chain in `docs/CalradiaForge-Registro-Mejoras.integrity.jsonl`.

---

## 6. Skills Taxonomy & Gateway Routing

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

The repository houses specialized agent skills organized into functional clusters in `.agents/skills/`:
- **Primary Gateways**:
  - `calradia-forge-dotnet`: First step for all .NET, C#, and MSBuild tasks.
  - `calradia-forge-docs`: First step for documentation, codemaps, and ledger updates.
  - `calradia-forge-dev-workflow`: First step for general task routing and QA.
- **Protected Upstream Copies**:
  - `using-dotnet`, `superpowers`, and `docs-generator` are preserved local copies containing project adaptations. Never overwrite them with raw upstream updates.

## Specialist Skill Routing

Use the specialist that matches the task after the `calradia-forge-dev-workflow` gateway:
- Crash and lifecycle triage: `.agents/skills/debugging-master/SKILL.md`.
- Measured runtime cost, allocations, and time-slicing: `.agents/skills/performance-hunter/SKILL.md`.
- Tactical combat behavior-tree proposals: `.agents/skills/game-ai-behavior-trees/SKILL.md`.
- Campaign callback cadence and simulation scheduling: `.agents/skills/discrete-event-simulation/SKILL.md`.
- Autonomous-agent routing and output compaction: `.agents/skills/multi-agent-orchestration/SKILL.md`.
- Gauntlet/WPF presentation, accessibility, and F10 integration: `.agents/skills/game-ui-design/SKILL.md`.

---

## 7. Rules Taxonomy & Domain Gateway Routing

In addition to root invariants, the repository maintains modular rules in `.agents/rules/` automatically discovered by Antigravity and indexed for OpenAI Codex CLI across functional clusters:

| Cluster | Key Rule Files (`.agents/rules/`) | Primary Scope & Invariants |
| :--- | :--- | :--- |
| **Core Architecture & Engine** | `calradia_forge_architecture.md`<br>`bannerlord_architecture.md`<br>`calradia_forge_sdk.md`<br>`calradia_forge_forgeweave.md`<br>`modding_environment.md` | Target frameworks (net472 vs net8.0), assembly isolation, SubModule lifecycle, and ForgeWeave event pipelines. |
| **Bannerlord Game Systems** | `bannerlord_campaign_behavior.md`<br>`bannerlord_save_system.md`<br>`bannerlord_shared_patterns.md`<br>`bannerlord_economy_trade_architecture.md`<br>`bannerlord_kingdom_diplomacy.md`<br>`bannerlord_settlement_rebellion.md`<br>`bannerlord_crime_underworld.md`<br>`bannerlord_character_development.md`<br>`bannerlord_clan_succession.md`<br>`bannerlord_quests_dialogues.md`<br>`bannerlord_audio_system.md`<br>`bannerlord_siege_mechanics.md`<br>`bannerlord_combat_ai_formations.md`<br>`bannerlord_mobileparty_spawner.md`<br>`bannerlord_map_visuals.md`<br>`bannerlord_missionview_hud.md`<br>`bannerlord_mission_lifecycle.md`<br>`bannerlord_inventory_barter.md`<br>`bannerlord_gamemodels_architecture.md`<br>`bannerlord_items_crafting_architecture.md`<br>`bannerlord_troop_character_architecture.md`<br>`bannerlord_xml_overrides.md`<br>`bannerlord_xml_schemas.md`<br>`bannerlord_localization.md` | Stateless behaviors, zero SaveableTypeDefiner, modulo-24 time slicing, decorator models, audio categories, XML merging rules, and localization encoding. |
| **UI & Presentation** | `calradia_forge_ui.md`<br>`gauntlet_architecture.md`<br>`bannerlord_input_debug_agent.md` | Gauntlet UI XML layout, F10 hotkey polling with rising-edge fallback, Desktop WPF MVVM, theme dictionaries, and accessibility. |
| **Documentation & Ledger** | `calradia_forge_docs.md`<br>`docs_generation_workflow.md` | Strict English/Spanish parity (`docs/<TOPIC>.md` $\leftrightarrow$ `.es.md`), DocFX compilation, and SHA-256 append-only ledger integrity. |
| **Verification & Distribution** | `calradia_forge_verification.md`<br>`distribution_safety.md` | Build verification, 5-stage test suite, static source reflection contracts, and exclusion of game DLLs/scripts/zone streams. |
| **Codex & Multi-Agent** | `codex_compatibility.md`<br>`development_workflow.md`<br>`developer_onboarding.md` | Cross-agent parity, evidence-grounded SDK/template/content onboarding, Windows capture compatibility and agent coordination. |
| **Packaging & Git Workflow** | `auto_packaging.md`<br>`calradia_forge_packaging.md`<br>`packaging_version.md`<br>`git_sync_workflow.md` | Automated ZIP packaging (`FastPackageEngine`), version synchronization, chat-scoped commits at objective completion, and explicit GitHub synchronization. |

### Explicit follow-up push authorization
A later user request to push supersedes an earlier no-push instruction for the completed objective. Deliver its scoped commits and requested guide corrections, then review the exact remote SHA and repair task-related CI failures under the same authorization. Do not request permission again or include unrelated concurrent work. Publication and force-push remain outside this authorization. See `.agents/rules/git_sync_workflow.md`.
