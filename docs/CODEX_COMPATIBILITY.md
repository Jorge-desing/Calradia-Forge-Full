# Codex Compatibility & Multi-Agent Architecture

This document outlines the architecture, tool integration, and operational guidelines for OpenAI Codex CLI and multi-agent systems within Calradia Forge.

---

## 1. Overview & Ecosystem Alignment

Calradia Forge is engineered to be fully operable by diverse autonomous AI agents and pair-programming assistants. To achieve cross-tool parity between Google Antigravity / Gemini and OpenAI Codex, the repository provides multi-surface configuration and runtime adapters.

```text
┌──────────────────────────────────────────────────────────────┐
│                    Developer / Agent CLI                     │
├──────────────────────────────┬───────────────────────────────┤
│    Google Antigravity        │       OpenAI Codex CLI        │
│   (GEMINI.md, .agents/)      │  (AGENTS.md, CODEX.md, .codex)│
└──────────────┬───────────────┴───────────────┬───────────────┘
               │                               │
               ▼                               ▼
┌──────────────────────────────────────────────────────────────┐
│                  Calradia Forge Workspace                    │
├──────────────────────────────────────────────────────────────┤
│  • Anti-Shadowing & Statelessness Invariant Rules            │
│  • Multi-Targeted Solution (net472 / net8.0-windows)         │
│  • Automated Verification & 5-Stage Test Suite               │
│  • CodexCaptureCompat (Windows 10 Computer Use Layer)        │
│  • ForgeEncyclopediaExtender (In-Game Codex API)             │
└──────────────────────────────────────────────────────────────┘
```

---

## 2. Configuration Surfaces

### 2.1 Instruction Manifests & Rules Interoperability (`AGENTS.md`, `CODEX.md`, `.codex/config.json`)
- **`AGENTS.md`**: Universal multi-agent specification defining architectural layers, anti-shadowing constraints, statelessness invariants, command playbooks, skills taxonomy, and the 43-rules gateway taxonomy.
- **`CODEX.md`**: Direct entry point for OpenAI Codex CLI sessions with quick-reference commands, skills routing, and modular rules index.
- **`.codex/config.json`**: Machine-readable project metadata (version 25.2.0), test runner commands, target framework mapping, and `"rulesPath": ".agents/rules"`.
- **Modular Rules Discovery Protocol**: While Google Antigravity discovers `.agents/rules/*.md` automatically, Codex CLI uses the shared taxonomy in `AGENTS.md` (Section 7) and `CODEX.md` (Section 5) to enforce identical architectural, gameplay, UI, documentation, and packaging invariants.

### 2.2 Invariant Rules Enforced
- **Anti-Shadowing Constraint**: Never create any folder, namespace, or class named `Campaign` or `Localization` in game assemblies. Collisions with `TaleWorlds.CampaignSystem.Campaign` break `Campaign.Current`.
- **Stateless CampaignBehaviors**: Zero `SaveableTypeDefiner` inheritance and zero `SyncData(IDataStore)` persistence in `src/CalradiaForge.Mod` behaviors.
- **Desktop Static Contracts**: Automated reflection tests verify immutable strings in `src/CalradiaForge.Desktop`.

---

## 3. Windows 10 Codex Computer Use Compatibility (`CodexCaptureCompat`)

### 3.1 Background & Technical Need
OpenAI Operator and Codex Computer Use (`codex-computer-use.exe`) utilize Windows Graphics Capture (WGC). On Windows 10:
1. The `IGraphicsCaptureSession3` interface (specifically `IsBorderRequired`) is not available natively.
2. Synchronous image conversion inside WGC callbacks can induce deadlocks.

### 3.2 Solution Architecture
The workspace incorporates `CodexCaptureCompat`:
- **Proxy DLL (`version.dll`)**: Intercepts `RoGetActivationFactory` for `Direct3D11CaptureFramePool`.
- **Border Compatibility**: Returns a compatible interface when `IGraphicsCaptureSession3` is queried, safely bypassing missing system APIs.
- **MTA Callback Dispatch**: Offloads `FrameArrived` callbacks onto Windows threadpool worker threads, eliminating frame ingestion deadlocks.

### 3.3 Management via `tools/Manage-CodexCaptureCompat.ps1`

```powershell
# Check dist binaries and running helper process
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action Status

# Run test suite
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action Test

# Preview installation to target helper
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action WhatIf -HelperPath C:\Path\To\codex-computer-use.exe
```

---

## 4. In-Game Calradia Codex / Encyclopedia Extender

For in-game lore, troop rosters, and settlement lookup:
- **Class**: `CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender`
- **Features**:
  - `RegisterEntry(EncyclopediaEntry)`: Adds modded lore or entity definitions to the codex.
  - `Search(query, category)`: High-performance substring search across names, tags, and descriptions.
  - `AddBookmark(id)` / `IsBookmarked(id)` / `GetBookmarks()`: Player bookmarking system.
  - `RegisterFilter(category, tag, predicate)` / `Filter(category, tag)`: Dynamic categorization and facet filtering.
- **Thread Safety**: Fully synchronized via internal monitor locks; safe for concurrent game-thread and worker-thread queries.

---

## 5. Verification Commands

```powershell
# 1. Statelessness Acceptance
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\verify_stateless_behavior.ps1

# 2. Comprehensive Test Suite
cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause <nul"

# 3. Codex Capture Compat Verification
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action Test
```
