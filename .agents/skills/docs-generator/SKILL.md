---
name: docs-generator
description: Protected source snapshot for Calradia Forge documentation. Retained as a backup against upstream changes; active documentation work uses calradia-forge-docs and its project specialists.
metadata:
  version: "2.1.0"
  author: "calradia-forge-team"
---

# Calradia Forge Docs Generator

## Overview

> **Protected source snapshot:** This file is retained so upstream changes cannot erase its Calradia Forge documentation material. For active work, use [`calradia-forge-docs`](../calradia-forge-docs/SKILL.md) and its project specialists; this snapshot is not a runtime dependency.

Master documentation orchestrator tailored specifically to the **Calradia Forge** modding ecosystem. Guides the creation and maintenance of technical documentation, architectural codemaps, dual-language guides, immutable append-only changelog revisions, and release validation reports.

## When to Consult This Preserved Reference

- Keep this source snapshot for recovery and audit. Ordinary documentation requests start with `calradia-forge-docs`, which dispatches to the current project specialists.
- Documentation types:
  - Architecture Codemaps (`docs/CODEMAP_*.md`)
  - Dual-Language Technical Guides (`docs/<TOPIC>.md` and `docs/<TOPIC>.es.md`)
  - Immutable Append-Only Ledger Annexes (`docs/append/RevXXX-*.md`)
  - Release Validation Reports (`docs/VALIDATION-<VERSION>.md` and `.es.md`)
  - DocFX C# XML Docstrings & In-Game Help

---

## Specialized Documentation Skills Delegation

This master orchestrator coordinates four specialized documentation skills:

| Subdomain | Dedicated Skill | Core Responsibilities |
| :--- | :--- | :--- |
| **Codemaps** | [`calradia-forge-codemaps`](../calradia-forge-codemaps/SKILL.md) | Synchronizes `CODEMAP_ARCHITECTURE.md`, `CODEMAP_CAMPAIGN_BEHAVIORS.md`, and `CODEMAP_SDK_GAMEMODELS.md` against source code. |
| **Registro de Mejoras** | [`calradia-forge-registro-mejoras`](../calradia-forge-registro-mejoras/SKILL.md) | Drafts `docs/append/RevXXX-*.md` annexes, compiles protected DOCX revisions, and appends SHA-256 blocks to `docs/CalradiaForge-Registro-Mejoras.integrity.jsonl`. |
| **DocFX & In-Game Help** | [`calradia-forge-docfx-pipeline`](../calradia-forge-docfx-pipeline/SKILL.md) | Enforces standardized C# XML docstrings, compiles `tools/build_docs.ps1`, and generates runtime help via `tools/generate_in_game_help.py`. |
| **Release Validation** | [`calradia-forge-release-validation`](../calradia-forge-release-validation/SKILL.md) | Generates `docs/VALIDATION-<VERSION>.md` and `.es.md` with test pass counts, WPF render layout metrics, boundaries of evidence, and archive hashes. |

---

## Calradia Forge Documentation Workflows

### 🏛️ Workflow 1: Codemap Synchronization (`docs/CODEMAP_*.md`)
*Delegates to [`calradia-forge-codemaps`](../calradia-forge-codemaps/SKILL.md)*

1. **Identify Affected Subsystems**:
   - `CODEMAP_ARCHITECTURE.md`: Module layout, assembly dependencies (`TaleWorlds` → `Core` → `Mod`/`Sdk` → `Desktop`), SubModule lifecycle hooks, Core classes, static rules.
   - `CODEMAP_CAMPAIGN_BEHAVIORS.md`: CampaignBehaviorBase lifecycle, dual-registration (`OnGameStart` / `OnCampaignStart`), event catalog, anti-lag time slicing (modulo-24), zero-allocation loops.
   - `CODEMAP_SDK_GAMEMODELS.md`: SDK public APIs (`ForgeData`, `ForgeAgentMemory`, `ForgeCampaignEvents`), GameModel decorator implementations, `ExplainedNumber` conventions.
2. **Update Diagram & Tables**:
   - Maintain ASCII flowcharts and class tables with exact symbol and assembly names.
   - Ensure zero shadowing of `Campaign` or `Localization` namespaces.

---

### 🌐 Workflow 2: Dual-Language Guides (`docs/<TOPIC>.md` & `.es.md`)

1. **Parity Enforcement**:
   - Every guide in `docs/` MUST exist as a paired pair: English (`<TOPIC>.md`) and Spanish (`<TOPIC>.es.md`).
   - English serves as the canonical technical reference; Spanish maintains identical structure, headers, code blocks, and technical rigor.
2. **Standard Document Layout**:
   - Header with topic title, version context, and summary.
   - Architectural Overview & Diagrams.
   - API Reference & Code Examples (adhering to .NET Framework 4.7.2 for Mod and .NET 8 C# 12 for Desktop).
   - Lifecycle, Thread Safety, and Performance Constraints.
   - Troubleshooting, Gotchas, and Verification Evidence.

---

### 📜 Workflow 3: Append-Only "Registro de Mejoras" (DOCX + Hash Chain)
*Delegates to [`calradia-forge-registro-mejoras`](../calradia-forge-registro-mejoras/SKILL.md)*

1. **Draft Markdown Annex in `docs/append/`**:
   - Path format: `docs/append/RevXXX-<Topic>[-<date>].[es.]md` (e.g. `Rev023-Gauntlet-visual-round3-2026-09-23.md`).
   - Required sections: Metadata (**Fecha**, **Versión**, **Alcance**), Problema observado / Motivación, Solución técnica y arquitectura, Cambios en activos y código, Validación y límites de la evidencia.
2. **Compile Revision**:
   ```bash
   python tools/append_detailed_changelog_revision.py docs/append/RevXXX-<Topic>.md
   ```
3. **Verify Integrity Ledger**:
   - Verifies prefix invariance (text + OOXML C14N), applies read-only protection, creates `docs/CalradiaForge-Registro-Mejoras-RevXXX.docx`, and calls `tools/record_detailed_changelog_integrity.py` to record the SHA-256 block into `docs/CalradiaForge-Registro-Mejoras.integrity.jsonl`.

---

### 📚 Workflow 4: DocFX & In-Game Help Pipeline
*Delegates to [`calradia-forge-docfx-pipeline`](../calradia-forge-docfx-pipeline/SKILL.md)*

1. **Author Standardized XML Comments in C#**:
   - Summary, Remarks, Lifecycle, Thread Safety, Performance, Example code.
2. **Run Documentation Build**:
   ```powershell
   tools/build_docs.ps1
   python tools/generate_in_game_help.py
   ```
   Ensures DocFX site in `docs-site/generated-site` and in-game help strings stay 100% in sync before packaging.

---

### 🧪 Workflow 5: Release Validation Evidence (`docs/VALIDATION-<VERSION>.md`)
*Delegates to [`calradia-forge-release-validation`](../calradia-forge-release-validation/SKILL.md)*

1. **Draft Validation Report (EN & ES)**:
   - `docs/VALIDATION-<VERSION>.md` and `docs/VALIDACION-<VERSION>.es.md`.
2. **Include Exact Empirical Evidence**:
   - Build status (0 warnings, 0 errors).
   - Test suites: Core, ForgeWeave, Desktop MVVM, Desktop Render (passes, ms timing, layout passes), Asset pipeline fixtures.
   - Boundaries of evidence: Clear statement of scope (e.g., headless batch execution vs live game).
   - SHA-256 archive digests from `artifacts/package-sha256-<version>.txt`.
