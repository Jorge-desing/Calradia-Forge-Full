# CODEX.md · OpenAI Codex Instructions for Calradia Forge

This file provides direct project instructions for the OpenAI Codex CLI, OpenAI Operator, and compatible AI agent runtimes operating on the Calradia Forge repository.

See [AGENTS.md](AGENTS.md) for the complete multi-agent specification.

---

## 1. Quick Reference & Core Rules

1. **Anti-Shadowing Constraint (CRITICAL)**:
   - NEVER create a folder, class, or sub-namespace named `Campaign` or `Localization`.
   - Shadowing `TaleWorlds.CampaignSystem.Campaign` causes C# compilation failures with `Campaign.Current`.
   - Approved names: `CampaignBehaviors`, `DataExtensions`, `CampaignExtensions`.

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
   - Every file under `docs/` must have both English (`<TOPIC>.md`) and Spanish (`<TOPIC>.es.md`) versions.

6. **Project Scope Staging & Zero External Garbage**:
   - At the end of each repository-changing request, objective, or plan, commit all intentional project changes attributable to it; capture the baseline and exclude unrelated pre-existing work. Intermediate steps need no separate commits.
   - Always include all intentional objective changes (code, UI, tests, docs, assets) in the scoped commit. See .agents/rules/git_sync_workflow.md.
   - A commit does not authorize a remote push; push only when explicitly requested.
   - **Mandatory after every authorized push:** Verify the remote branch equals the pushed SHA and wait for its applicable Actions/check runs and commit statuses. If they fail, read their logs, correct the task-related cause, validate through BAT, push under the existing authorization and review the replacement SHA. Do not end the objective as complete after push alone, local success, an earlier green commit or a pending check. Report genuine external blockers as unverified. Include the final SHA and matching remote run links. This gate is direct session guidance and does not depend on loading a skill; details are in .agents/rules/git_sync_workflow.md.
   - Strictly exclude external debris: caches (`bin/`, `obj/`, `artifacts/`, `__pycache__/`, `*.pyc`), logs (`*.log`), game saves (`*.sav`), secrets (`.env`), and OS metadata.

---

## 2. Command Playbook

### Mandatory Distribution Completion Gate
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
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\verify_stateless_behavior.ps1
```

### Run Full Test Suite (Core, ForgeWeave, Desktop MVVM, WPF Render Tests)
```cmd
cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause <nul"
```

### Run Core Subsystem Tests
```cmd
cmd.exe /c "tools\Run-CalradiaForge-Core-Tests.bat -NonInteractive <nul"
```

### Run Desktop Subsystem Tests
```powershell
dotnet run --project tests/CalradiaForge.Desktop.Tests/CalradiaForge.Desktop.Tests.csproj -c Release
```

### Packaging & Release Archives
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\package.ps1
```

### Live In-Game UI Review
- Open the Calradia Forge overlay on a singleplayer screen with `Settings.Hotkey`; the default is `F10`, and pressing it again closes the panel. F10 polling needs an F10-only rising-edge fallback using `IsKeyDown`/`IsKeyDownImmediate` when `IsKeyPressed` misses the key; details are in `.agents/rules/bannerlord_input_debug_agent.md` and `.agents/rules/calradia_forge_ui.md`.
- For an authorized live review, build both Client and Modding Kit profiles first, close the Modding Kit completely, launch Bannerlord from Steam with Calradia Forge enabled, and inspect the actual panel. Do not leave the game and Modding Kit open together or ask the user to open the game first.
- Before each source correction, close Bannerlord and the Modding Kit and end/reset Ordenador/Computer Use; keep them closed through edits and builds. After each live check, close Bannerlord and end/reset the Computer Use session before another correction or task completion.
- Treat source audits and Resource Browser import as separate from live rendering and interaction evidence.

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
- Multi-agent autonomous framework in `agents/` powered by the **Google Antigravity SDK** (`google.antigravity`).
- Coordinates 5 specialized agents: `ForgeMasterAgent`, `ForgeArchitectAgent`, `StatelessBehaviorAuditor`, `DesktopWpfSpecialist`, and `DocLedgerAgent`.
- **Token Compaction Engine (`ForgeTokenCompactor`)**: Semantic output distillation achieving 80-99% token reduction while strictly guaranteeing lossless failure telemetry (compiler errors `CSxxxx`, test assertions, stack traces). Raw outputs saved to `artifacts/agent-runs/`.
- Adaptive presets via `--compaction-preset [ultra (8k) | balanced (16k) | deep (32k)]`, with optional raw bypass via `--raw-tools`.
- CLI runner: `python tools/run_forge_agents.py [audit|verify|docs|architect|run]`.
- Verified via `py -3.12 -m unittest tests/test_forge_agents.py` with offline simulation fallback.
- Technical specifications: `docs/AUTONOMOUS_AGENTS.md` and `docs/AUTONOMOUS_AGENTS.es.md`.

---

## 4. Skills Taxonomy & Gateways

When working with skills in `.agents/skills/`:
- Use `calradia-forge-dotnet` for C# / MSBuild tasks.
- Use `/calradia-forge-docs` for documentation tasks (governed by `.agents/rules/calradia_forge_docs.md`).
- Use `calradia-forge-dev-workflow` for general engineering lifecycle.
- Never overwrite protected skills `using-dotnet`, `superpowers`, and `docs-generator`.

---

## 5. Antigravity Modular Rules Index & Compliance

While Google Antigravity discovers `.agents/rules/*.md` automatically via directory walking, OpenAI Codex CLI sessions MUST reference and comply with these 43 domain rules organized in 7 functional clusters:

1. **Architecture & Core**: `calradia_forge_architecture.md`, `bannerlord_architecture.md`, `calradia_forge_sdk.md`, `calradia_forge_forgeweave.md`, `modding_environment.md`.
2. **Bannerlord Gameplay Systems**: `bannerlord_campaign_behavior.md`, `bannerlord_save_system.md`, `bannerlord_shared_patterns.md`, `bannerlord_economy_trade_architecture.md`, `bannerlord_kingdom_diplomacy.md`, `bannerlord_settlement_rebellion.md`, `bannerlord_crime_underworld.md`, `bannerlord_character_development.md`, `bannerlord_clan_succession.md`, `bannerlord_quests_dialogues.md`, `bannerlord_audio_system.md`, `bannerlord_siege_mechanics.md`, `bannerlord_combat_ai_formations.md`, `bannerlord_mobileparty_spawner.md`, `bannerlord_map_visuals.md`, `bannerlord_missionview_hud.md`, `bannerlord_mission_lifecycle.md`, `bannerlord_inventory_barter.md`, `bannerlord_gamemodels_architecture.md`, `bannerlord_items_crafting_architecture.md`, `bannerlord_troop_character_architecture.md`, `bannerlord_xml_overrides.md`, `bannerlord_xml_schemas.md`, `bannerlord_localization.md`.
3. **UI & User Experience**: `calradia_forge_ui.md`, `gauntlet_architecture.md`, `bannerlord_input_debug_agent.md`.
4. **Documentation & Ledger**: `calradia_forge_docs.md`, `docs_generation_workflow.md`.
5. **Verification & Distribution**: `calradia_forge_verification.md`, `distribution_safety.md`.
6. **Codex & Multi-Agent Compatibility**: `codex_compatibility.md`, `development_workflow.md`.
7. **Packaging & Git Sync**: `auto_packaging.md`, `calradia_forge_packaging.md`, `packaging_version.md`, `git_sync_workflow.md`.

*Instruction for Codex:* Before editing C#, XML, WPF, or automation scripts, read the corresponding rule file from `.agents/rules/<rule>.md` to maintain full project compliance.
