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

---

## 2. Command Playbook

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
