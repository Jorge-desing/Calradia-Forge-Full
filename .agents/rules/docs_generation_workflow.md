---
name: docs-generation-workflow
description: Enforce Calradia Forge's native documentation architecture, Codemaps synchronization, bilingual parity, append-only cryptographic ledger (Registro de Mejoras), DocFX pipeline, and release validation.
trigger: always_on
---

# Calradia Forge Documentation Architecture & Workflow

Whenever creating, updating, or maintaining technical documentation, specifications, codemaps, or release records in Calradia Forge, use the `/calradia-forge-docs` command and skill orchestrator (governed by [calradia_forge_docs.md](calradia_forge_docs.md)). Start with [`calradia-forge-docs`](../skills/calradia-forge-docs/SKILL.md); the protected local [`docs-generator`](../skills/docs-generator/SKILL.md) copy remains intact as a source backup and is not a workflow dependency.

---

## 1. Documentation Ecosystem Overview (`docs/`)

The `docs/` directory is the single source of truth for technical, architectural, and user-facing documentation. `/calradia-forge-docs` coordinates its five subsystems and their specialist skills:

1. **Canonical Codemaps (`docs/CODEMAP_*.md`)** → Managed via [`calradia-forge-codemaps`](../skills/calradia-forge-codemaps/SKILL.md)
2. **Dual-Language Technical Guides & References (`<TOPIC>.md` and `<TOPIC>.es.md`)**
3. **Immutable Append-Only Ledger ("Registro de Mejoras": DOCX + Hash Chain)** → Managed via [`calradia-forge-registro-mejoras`](../skills/calradia-forge-registro-mejoras/SKILL.md)
4. **DocFX API Reference & In-Game Help Pipeline** → Managed via [`calradia-forge-docfx-pipeline`](../skills/calradia-forge-docfx-pipeline/SKILL.md)
5. **Release Validation Evidence (`VALIDATION-<VERSION>.md` and `VALIDACION-<VERSION>.es.md`)** → Managed via [`calradia-forge-release-validation`](../skills/calradia-forge-release-validation/SKILL.md)

---

## 2. Canonical Codemaps (`docs/CODEMAP_*.md`)

Codemaps maintain architectural alignment between source code and documentation. Whenever components, behaviors, models, or core patterns are added or modified, the corresponding Codemap MUST be updated:

- **`docs/CODEMAP_ARCHITECTURE.md`**:
  - Assembly hierarchy: `TaleWorlds.*` → `CalradiaForge.Core` → `CalradiaForge.Mod` / `CalradiaForge.Sdk` → `CalradiaForge.Desktop`.
  - SubModule lifecycle pipeline: ASCII sequence across `OnSubModuleLoad`, `OnGameStart`, `OnCampaignStart`, `OnApplicationTick`, and `OnSubModuleUnloaded`.
  - Core components: `ForgeBehaviorLoader`, `ModRuleAuditor`, `ForgeWeaveEngine`, `ForgeConfig`, `ForgeLogger`, and `ForgeBootstrapper`.
  - GEMINI anti-shadowing rules: Zero folders, sub-namespaces, or classes named `Campaign` or `Localization`.
- **`docs/CODEMAP_CAMPAIGN_BEHAVIORS.md`**:
  - `CampaignBehaviorBase` lifecycle and dual-registration patterns (`OnGameStart` for simulation listeners, `OnCampaignStart` for world state).
  - Complete event catalog and subscription signatures (`CampaignEvents`).
  - Anti-lag time-slicing (modulo-24: `hero.Id.GetHashCode() % 24 == currentHour`) and zero GC allocations in tick loops.
  - Save system safety rules and `SaveableTypeDefiner` base ID allocation ($\ge 2,500,000$).
- **`docs/CODEMAP_SDK_GAMEMODELS.md`**:
  - SDK public service contracts (`ForgeData`, `ForgeAgentMemory`, `ForgeCampaignEvents`, `ForgeUI`, `ForgeDetour`).
  - Decorator pattern implementations for vanilla GameModels wrapping `_previousModel`.
  - `ExplainedNumber` bonus/penalty conventions and registration in `OnGameStart`.

---

## 3. Dual-Language Technical Guides (`.md` and `.es.md`)

All technical specifications, feature overviews, design documents, and user guides in `docs/` MUST maintain strict conceptual parity between English (canonical source) and Spanish:

- **Paired File Structure**:
  - English: `docs/<TOPIC>.md`
  - Spanish: `docs/<TOPIC>.es.md`
- **Established Pairs**:
  - `ARCHITECTURE.md` / `SYSTEM_DESIGN.md` (English base)
  - `FORGEWEAVE.md` / `FORGEWEAVE.es.md`
  - `ASSET_AUTOMATION_EVIDENCE.md` / `ASSET_AUTOMATION_EVIDENCE.es.md`
  - `BANNERLORD_IMPORT_AUTOMATION.md` / `BANNERLORD_IMPORT_AUTOMATION.es.md`
  - `GAME_ICON_ASSETS.md` / `GAME_ICON_ASSETS.es.md`
  - `GAME_UI_EXTENSIONS.md` / `GAME_UI_EXTENSIONS.es.md`
  - `PATCH_BLUEPRINTS.md` / `PATCH_BLUEPRINTS.es.md`
  - `UI_DESIGN.md` / `UI_DESIGN.es.md`
  - `DESKTOP.md` / `ASSEMBLY_WORKBENCH.es.md`
  - `TESTING.md` / `TESTING.es.md`
  - `STORE_DESCRIPTION.md` / `STORE_DESCRIPTION.es.md`
  - `CHANGELOG.md` / `CHANGELOG.es.md`
  - `CODEX_COMPATIBILITY.md` / `CODEX_COMPATIBILITY.es.md`
  - `VALIDATION-<VERSION>.md` / `VALIDACION-<VERSION>.es.md`
- **Synchronization Rules**:
  - Whenever an English document is created or revised, the corresponding Spanish translation must be updated in the same changeset.
  - Code identifiers, CLI flags, JSON keys, file paths, and benchmark numbers must remain verbatim and untranslated.

---

## 4. Immutable Append-Only Ledger ("Registro de Mejoras")

For major milestones, architectural overhauls, visual passes, or significant batches of improvements, the project maintains an immutable, cryptographically verifiable ledger in Microsoft Word DOCX format.

### Append-Only Workflow:
1. **Draft Markdown Annex in `docs/append/`**:
   - File naming: `docs/append/RevXXX-<Topic>[-<date>].[es.]md` (e.g., `Rev023-Gauntlet-visual-round3-2026-09-23.md`).
   - Structure:
     - Level 1 Heading: `# RevXXX — [Title]`
     - Metadata block: **Fecha**, **Versión**, **Alcance**.
     - Sections: Problema observado y justificación técnica, Solución técnica y decisiones arquitectónicas, Cambios en activos y código, Validación y límites de la evidencia.
2. **Execute Append Tool**:
   ```bash
   python tools/append_detailed_changelog_revision.py docs/append/RevXXX-<Topic>.md
   ```
3. **Integrity Guarantees Enforced by the Tool**:
   - **Prefix Invariance**: Never alters an earlier revision. It verifies that every paragraph from `CalradiaForge-Registro-Mejoras-Rev(XXX-1).docx` remains an identical prefix (both plain text/style checks and canonical OOXML C14N XML element comparison).
   - **Read-Only Protection**: Injects Word document protection (`w:documentProtection edit="readOnly" enforcement="1"`).
   - **Lockfile Concurrency Protection**: Uses `docs/.CalradiaForge-Registro-Mejoras.append.lock` to prevent concurrent modifications.
   - **Cryptographic Hash Chain**: Automatically invokes `tools/record_detailed_changelog_integrity.py` to record the SHA-256 digest linked to the previous block hash in `docs/CalradiaForge-Registro-Mejoras.integrity.jsonl`.

---

## 5. DocFX API Documentation & In-Game Help Pipeline

### C# Code Documentation Standards:
Every public class, interface, method, and property must include standardized XML docstrings:
```csharp
/// <summary>
/// [Brief description of class/method]
/// 
/// [Detailed explanation of purpose and behavior]
/// 
/// Lifecycle: [When created/destroyed]
/// Thread Safety: [Thread safety guarantees]
/// Performance: [Performance characteristics]
/// 
/// Example:
/// <code>
/// // Usage example
/// </code>
/// </summary>
```

### Automation & Tooling:
- **Build DocFX Static Site**:
  ```powershell
  tools/build_docs.ps1
  ```
  Generates static documentation in `docs-site/generated-site`, packaged in the SDK release zip (`CalradiaForge-Source-SDK-<version>.zip`).
- **Generate In-Game Help**:
  ```bash
  python tools/generate_in_game_help.py
  ```
  Extracts XML docstrings into localized game help resources for Gauntlet UI and Desktop.
- **Packaging Gate**: `tools/package.ps1` mandates that `build_docs.ps1` and `generate_in_game_help.py` execute successfully before creating release archives.

---

## 6. Release Validation Evidence (`VALIDATION-<VERSION>.md`)

Upon concluding a version release or major update:
- Generate `docs/VALIDATION-<VERSION>.md` and `docs/VALIDACION-<VERSION>.es.md`.
- Include:
  - Exact date, commit/version number, and test environment limits.
  - Complete test breakdown: Core tests, ForgeWeave tests, Desktop MVVM/protocol tests, WPF Render/Layout pass metrics (number of passes, ms timings, nodes visited), Asset pipeline fixtures, and Statelessness verification.
  - Boundaries of evidence: Explicitly state what was verified (e.g. offline unit/integration/render tests) and what was not tested (e.g. no live Bannerlord game launch, no endurance battle runs).
  - SHA-256 package hashes from `artifacts/package-sha256-<version>.txt`.

---

## 7. Local Markdown Planning & Vibe Coding (`RULE[user_global]`)

- Keep all task planning, atomic feature checklists, and active milestone notes directly inside the repository in Markdown (`task.md`, `PROJECT_PLAN.md`).
- Prioritize open source, free, local file structures over paid external project management tools or cloud APIs.
