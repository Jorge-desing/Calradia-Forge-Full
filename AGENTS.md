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
| `src/CalradiaForge.Sdk` | `net472;net8.0` | Public SDK surfaces, cognitive memory (`ForgeAgentMemory`), API bridges | Clean decoupled contracts; no engine entity direct references. |
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
- Never include batch scripts (`.bat`) or PowerShell scripts (`.ps1`) in user-facing distribution zips.
- Strip NTFS `:Zone.Identifier` alternate data streams from assemblies before distribution.

---

## 3. Essential Commands & Verification Playbook

Always execute commands with non-interactive flags and appropriate working directories.

### Build
```powershell
dotnet build CalradiaForge.sln -c Release -v:minimal
```

### Stateless Behavior & Acceptance Verification (Mandatory Gate)
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\verify_stateless_behavior.ps1
```

### Full Test Suite (Core, ForgeWeave, Desktop MVVM, WPF Render Tests)
```cmd
cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause <nul"
```

### Unit Test Execution by Subsystem
- **Core Tests**: `cmd.exe /c "tools\Run-CalradiaForge-Core-Tests.bat -NonInteractive <nul"`
- **Desktop MVVM & Protocol Tests**: `dotnet run --project tests/CalradiaForge.Desktop.Tests/CalradiaForge.Desktop.Tests.csproj -c Release`
- **Desktop Live Render Tests**: `dotnet run --project tests/CalradiaForge.Desktop.RenderTests/CalradiaForge.Desktop.RenderTests.csproj -c Release`

### Release Packaging
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\package.ps1
```

### Live In-Game UI Review
- Open the Calradia Forge overlay on a singleplayer screen with `Settings.Hotkey`; the default is `F10`, and pressing it again closes the panel. F10 polling needs an F10-only rising-edge fallback using `IsKeyDown`/`IsKeyDownImmediate` when `IsKeyPressed` misses the key; details are in `.agents/rules/bannerlord_input_debug_agent.md` and `.agents/rules/calradia_forge_ui.md`.
- For an authorized live review, build both Client and Modding Kit profiles first, close the Modding Kit completely, launch Bannerlord from Steam with Calradia Forge enabled, and inspect the actual panel. Do not leave the game and Modding Kit open together or ask the user to open the game first.
- Before each source correction, close Bannerlord and the Modding Kit and end/reset Ordenador/Computer Use; keep them closed through edits and builds. After each live check, close Bannerlord and end/reset the Computer Use session before another correction or task completion.
- Treat source audits and Resource Browser import as separate from live rendering and interaction evidence.

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
- Thread-safe and zero-allocation friendly.

### 4.4 Google Antigravity SDK Autonomous Agents Architecture & Token Compaction
- The repository houses a specialized multi-agent autonomous framework in `agents/` powered by the **Google Antigravity SDK** (`google.antigravity`).
- Coordinates 5 specialized agents: `ForgeMasterAgent` (orchestrator), `ForgeArchitectAgent` (C# architecture and GEMINI anti-shadowing), `StatelessBehaviorAuditor` (save safety and stateless SyncData), `DesktopWpfSpecialist` (WPF MVVM, container virtualization, DirectX aliasing, and UI Automation), and `DocLedgerAgent` (bilingual docs parity and SHA-256 ledger integrity).
- **Token Compaction Engine (`ForgeTokenCompactor`)**: Semantic output distillation achieving 80-99% token reduction while strictly guaranteeing **lossless failure telemetry** (100% preservation of compiler errors `CSxxxx`, test assertions, and stack traces). Uncompressed console outputs are saved to `artifacts/agent-runs/`.
- Adaptive presets via `--compaction-preset [ultra (8k) | balanced (16k) | deep (32k)]`, with optional raw bypass via `--raw-tools`.
- Invoked via `tools/run_forge_agents.py` with subcommands: `audit`, `verify`, `docs`, `architect`, and `run "<prompt>"`.
- Verified via `py -3.12 -m unittest tests/test_forge_agents.py` with deterministic offline simulation mode when no API key is set.
- Detailed architecture and usage guidelines: `docs/AUTONOMOUS_AGENTS.md` and `docs/AUTONOMOUS_AGENTS.es.md`.

---

## 5. Documentation Architecture & Dual-Language Parity

Documentation workflows are coordinated by the `/calradia-forge-docs` command and skill orchestrator (governed by `.agents/rules/calradia_forge_docs.md` and `.agents/rules/docs_generation_workflow.md`).
All technical documentation under `docs/` must maintain strict conceptual parity between English and Spanish:
- English source: `docs/<TOPIC>.md`
- Spanish counterpart: `docs/<TOPIC>.es.md`
- Never modify an English document without synchronizing its Spanish counterpart in the same changeset.
- Major releases and architectural revisions are recorded in the immutable append-only ledger ("Registro de Mejoras", `docs/CalradiaForge-Registro-Mejoras-Rev*.docx`) backed by a SHA-256 cryptographic hash chain in `docs/CalradiaForge-Registro-Mejoras.integrity.jsonl`.

---

## 6. Skills Taxonomy & Gateway Routing

The repository houses 45 specialized agent skills organized into 7 functional clusters in `.agents/skills/`:
- **Primary Gateways**:
  - `calradia-forge-dotnet`: First step for all .NET, C#, and MSBuild tasks.
  - `calradia-forge-docs`: First step for documentation, codemaps, and ledger updates.
  - `calradia-forge-dev-workflow`: First step for general task routing and QA.
- **Protected Upstream Copies**:
  - `using-dotnet`, `superpowers`, and `docs-generator` are preserved local copies containing project adaptations. Never overwrite them with raw upstream updates.
