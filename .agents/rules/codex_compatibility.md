# Codex & Multi-Agent Compatibility Guidelines

This rule establishes cross-tool compatibility standards between Google Antigravity, Gemini-based agents, and the OpenAI Codex CLI ecosystem operating within Calradia Forge.

---

## 1. Specification Synchronization (`GEMINI.md`, `AGENTS.md`, `CODEX.md`)

To ensure seamless handoffs and consistent behavior across AI coding assistants:
- **`GEMINI.md`**: Primary root rule file discovered by Antigravity / Gemini agents.
- **`AGENTS.md`**: Universal multi-agent and OpenAI Codex CLI instruction manifest.
- **`CODEX.md`**: Standard target for `codex` command-line sessions.
- **`.codex/config.json`**: Machine-readable configuration and command matrix for Codex.

Whenever core architectural constraints (anti-shadowing, target frameworks, verification commands, packaging rules) are updated, the changes **MUST** be mirrored across `GEMINI.md`, `AGENTS.md`, and `CODEX.md`.

---

## 2. Invariant Rules Enforced Across All Agents

1. **Anti-Shadowing Constraint**:
   - Never name any folder, namespace, or class `Campaign` or `Localization`.
   - Shadowing causes C# compiler collisions with `TaleWorlds.CampaignSystem.Campaign` and `TaleWorlds.Localization`.
2. **Stateless CampaignBehaviors**:
   - Zero `SaveableTypeDefiner` inheritance in `src/CalradiaForge.Mod`.
   - Zero `SyncData(IDataStore)` persistence in mod campaign behaviors.
3. **Dual-Language Documentation Parity**:
   - Every file created or updated in `docs/` must maintain exact conceptual parity between English (`docs/<TOPIC>.md`) and Spanish (`docs/<TOPIC>.es.md`).
4. **Preservation of Static Source Contracts**:
   - In `src/CalradiaForge.Desktop`, never remove or rename string literals verified by static reflection tests (e.g. `"Editable Calradia Forge starting point"`).

---

## 3. Windows 10 Codex Computer Use Compatibility (`CodexCaptureCompat`)

The workspace contains a dedicated native compatibility layer for Codex Computer Use (`codex-computer-use.exe`):
- **Location**: `CodexCaptureCompat/capture-compat/`
- **Core Mechanism**: A local x64 `version.dll` proxy that provides missing `IGraphicsCaptureSession3` border handling and dispatches `FrameArrived` callbacks onto Windows threadpool MTA worker threads.
- **Unified Management Script**: Agents and developers must use `tools/Manage-CodexCaptureCompat.ps1`:
  - `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action Status`
  - `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action Test`
  - `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action WhatIf -HelperPath <Path>`

---

## 4. In-Engine Calradia Codex / Encyclopedia Extender

For in-game codex and lore lookup mechanics:
- Use `CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender` to register custom encyclopedia entries, categories, tags, dynamic filters, and quick bookmarks.
- All methods are thread-safe and allocation-bounded.

---

## 5. Cross-Tool Rule Discovery & Parity Protocol

To guarantee that OpenAI Codex CLI enforces the same operational guardrails as Google Antigravity:
1. **Rule Discovery Mirroring**:
   - Google Antigravity discovers `.agents/rules/*.md` automatically via directory traversal.
   - OpenAI Codex CLI accesses rules via `.codex/config.json` (`"rulesPath": ".agents/rules"`) and the canonical Rules Taxonomy in `AGENTS.md` (Section 7) and `CODEX.md` (Section 3).
2. **Rule Modification Invariant**:
   - Whenever a rule in `.agents/rules/` is added, renamed, or modified, its reference and gateway cluster in `AGENTS.md` and `CODEX.md` MUST be synchronized in the same changeset.
3. **Mandatory Rule Consultation for Codex**:
   - Codex sessions modifying C# code, XML data, UI assets, packaging scripts, or git synchronization MUST consult the domain-specific rule before proposing or applying changes.

