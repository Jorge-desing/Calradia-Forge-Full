---
name: calradia-forge-docs
description: Calradia Forge documentation gateway for technical guides, codemaps, append-only improvement records, DocFX help, and release evidence. Use for documentation requests in this repository.
metadata:
  version: "1.0.0"
  author: "calradia-forge-team"
  category: "documentation"
  hub_skill: "calradia-forge-docs"
  related_skills: ["calradia-forge-codemaps", "calradia-forge-registro-mejoras", "calradia-forge-docfx-pipeline", "calradia-forge-release-validation"]
---

# Calradia Forge Documentation Orchestrator & Governance

Use this dedicated workspace skill whenever creating, updating, or maintaining technical documentation, architectural codemaps, dual-language guides, immutable append-only changelogs, or release validation records in Calradia Forge.

This isolated project skill set contains the current documentation policy and workflow; its linked specialists own the detailed procedures. It does not depend on an upstream generator skill being installed or remaining unchanged. The protected local [`docs-generator`](../docs-generator/SKILL.md) snapshot remains as a backup of source knowledge, while this hub and its specialists are the active project source of truth.

For capability matrices and knowledge handoff, follow the [developer onboarding rule](../../rules/developer_onboarding.md).
Record current-source/commit provenance, actual BAT evidence and pending live gates.
Do not turn a historical test count, fixture result or SDK worktree declaration into
a universal threshold or verified release claim. Specialized procedures belong in
existing skill references; synchronize shared rules across the three root guides.

---

## 1. Subsystem Delegation Architecture

The documentation ecosystem is organized into four specialized domain skills coordinated by this master orchestrator:

| Subdomain | Dedicated Skill | Core Responsibilities |
| :--- | :--- | :--- |
| **Codemaps** | [`calradia-forge-codemaps`](../calradia-forge-codemaps/SKILL.md) | Synchronizes `docs/CODEMAP_ARCHITECTURE.md`, `CODEMAP_CAMPAIGN_BEHAVIORS.md`, and `CODEMAP_SDK_GAMEMODELS.md` against source code. |
| **Registro de Mejoras** | [`calradia-forge-registro-mejoras`](../calradia-forge-registro-mejoras/SKILL.md) | Drafts `docs/append/RevXXX-*.md` annexes, compiles protected DOCX revisions, and appends SHA-256 blocks to `docs/CalradiaForge-Registro-Mejoras.integrity.jsonl`. |
| **DocFX & In-Game Help** | [`calradia-forge-docfx-pipeline`](../calradia-forge-docfx-pipeline/SKILL.md) | Enforces standardized C# XML docstrings, compiles `tools/build_docs.ps1`, and generates runtime help via `tools/generate_in_game_help.py`. |
| **Release Validation** | [`calradia-forge-release-validation`](../calradia-forge-release-validation/SKILL.md) | Generates `docs/VALIDATION-<VERSION>.md` and `docs/VALIDACION-<VERSION>.es.md` with test pass counts, WPF render layout metrics, boundaries of evidence, and archive hashes. |

---

## 2. Core Workflows

### 🏛️ Workflow 1: Codemap Synchronization (`docs/CODEMAP_*.md`)
*Delegates to [`calradia-forge-codemaps`](../calradia-forge-codemaps/SKILL.md)*
1. **Identify Affected Subsystems**:
   - `CODEMAP_ARCHITECTURE.md`: Assembly hierarchy, SubModule lifecycle, Core components, static rules.
   - `CODEMAP_CAMPAIGN_BEHAVIORS.md`: CampaignBehaviorBase lifecycle, dual-registration (`OnGameStart`/`OnCampaignStart`), event catalog, current time-slicing APIs and allocation/performance findings supported by measured scope. Do not state a universal zero-allocation guarantee from source inspection or result-only tests.
   - `CODEMAP_SDK_GAMEMODELS.md`: SDK public APIs (`ForgeData`, `ForgeAgentMemory`, `ForgeCampaignEvents`), GameModel decorator implementations, `ExplainedNumber` conventions.
2. **Update Diagrams & Tables**: Maintain exact symbol names and verify anti-shadowing (`GEMINI.md`).

### 🌐 Workflow 2: Dual-Language Technical Guides (`docs/<TOPIC>.md` & `.es.md`)
1. **Strict Parity**: Every technical document in `docs/` MUST have an English canonical file and the Spanish counterpart recognized by `agents.tools.audit_documentation_parity`. Standard pairs use `<TOPIC>.md` and `<TOPIC>.es.md`; established aliases are `DESKTOP.md` ↔ `ASSEMBLY_WORKBENCH.es.md` and `VALIDATION-<VERSION>.md` ↔ `VALIDACION-<VERSION>.es.md`. Architecture and system-design guides use their own `.es.md` counterparts. Do not rename these legacy pairs to make the audit pass.
2. **Synchronous Changeset**: Whenever an English document is revised, the Spanish translation MUST be updated in the same changeset. Code symbols, CLI commands, and metrics remain verbatim.
3. **Enforced Gate**: Run `tools/Run-CalradiaForge-Python-Checks.bat --ledger --no-pause`; the documentation parity check must fail the runner when any counterpart is missing.

### 📜 Workflow 3: Append-Only "Registro de Mejoras" (DOCX + Hash Chain)
*Delegates to [`calradia-forge-registro-mejoras`](../calradia-forge-registro-mejoras/SKILL.md)*
1. **Draft Annex**: `docs/append/RevXXX-<Topic>[-<date>].[es.]md` with standard structure (Problema observado, Solución técnica, Cambios en activos, Validación y límites).
2. **Compile Revision**: Run `python tools/append_detailed_changelog_revision.py docs/append/RevXXX-<Topic>.md`.
3. **Verify Ledger**: Confirms prefix invariance (text + OOXML C14N), Word read-only protection, and SHA-256 block registration in `docs/CalradiaForge-Registro-Mejoras.integrity.jsonl`.

### 📚 Workflow 4: DocFX API Documentation & In-Game Help Pipeline
*Delegates to [`calradia-forge-docfx-pipeline`](../calradia-forge-docfx-pipeline/SKILL.md)*
1. **XML Docstrings**: Author standardized C# comments with Summary, Lifecycle, Thread Safety, Performance, and Example.
2. **Pipeline Execution**: Execute `tools/build_docs.ps1` and `python tools/generate_in_game_help.py` before packaging.

### 🧪 Workflow 5: Release Validation Evidence (`docs/VALIDATION-<VERSION>.md`)
*Delegates to [`calradia-forge-release-validation`](../calradia-forge-release-validation/SKILL.md)*
1. **Empirical Evidence**: Capture exact test results (Asset fixtures, Core, ForgeWeave, Desktop MVVM, Desktop Render with pass count and layout ms, statelessness).
2. **Boundaries of Evidence**: Declare what was verified and what was not tested (e.g. no live game launch).
3. **Hashes**: Include SHA-256 digests from `artifacts/package-sha256-<version>.txt`.
