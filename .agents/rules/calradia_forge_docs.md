---
name: calradia-forge-docs
description: Enforce invocation, delegation workflows, and quality standards for the /calradia-forge-docs skill and command across Calradia Forge documentation.
trigger: always_on
---

# Calradia Forge Documentation Rules (`/calradia-forge-docs`)

When creating, maintaining, or auditing documentation in Calradia Forge, you MUST use the `/calradia-forge-docs` command and skill orchestrator. This rule establishes operational boundaries, trigger criteria, and mandatory invariants.

---

## 1. Trigger Conditions & Invocation Protocol

### Explicit Invocation
- Whenever the user executes the `/calradia-forge-docs` slash command or requests documentation, codemap updates, changelog revisions, or validation evidence.
- Replaces legacy or upstream `docs-generator` commands. The local copy `docs-generator` is a preserved backup snapshot only.

### Autonomous Phase-Gate Triggers
- **Post-Feature / Refactor Phase**: After any architectural change, behavior addition, SDK API modification, or UI extension, you must invoke `/calradia-forge-docs` to synchronize codemaps and technical guides.
- **Pre-Packaging Gate**: Before executing `tools/package.ps1`, `/calradia-forge-docs` ensures DocFX static documentation, in-game help resources, and release validation files are compiled and verified.

---

## 2. Gateway Delegation Architecture

`/calradia-forge-docs` acts as the master documentation orchestrator. It delegates specialized workflows to domain-specific skills:

```
                  /calradia-forge-docs (Hub Orchestrator)
                                    │
    ┌──────────────────────┬────────┴──────────────┬────────────────────────┐
    ▼                      ▼                       ▼                        ▼
calradia-forge-codemaps  calradia-forge-        calradia-forge-          calradia-forge-
 (Canonical Codemaps:    registro-mejoras        docfx-pipeline           release-validation
  Arch, Behaviors, SDK)   (Append-Only DOCX &     (C# XML Docstrings,      (VALIDATION-<VER>,
                           SHA-256 Ledger)         DocFX Site & Help)       Empirical Metrics)
```

### Delegation Matrix

| Task Type | Specialist Skill | Target Artifacts | Validation Command |
| :--- | :--- | :--- | :--- |
| **System Codemaps** | `calradia-forge-codemaps` | `docs/CODEMAP_*.md` | Consistency review against C# types and lifecycle. |
| **Bilingual Guides** | `/calradia-forge-docs` (Direct) | `docs/<TOPIC>.md` & `.es.md` | Verification of synchronous bilingual parity. |
| **Append-Only Ledger** | `calradia-forge-registro-mejoras` | `docs/append/RevXXX-*.md`<br>`docs/CalradiaForge-Registro-Mejoras-Rev*.docx` | `python tools/append_detailed_changelog_revision.py docs/append/RevXXX-*.md` |
| **API Docs & Help** | `calradia-forge-docfx-pipeline` | `docs-site/generated-site`<br>`Modules/CalradiaForge/ModuleData/Languages/` | `powershell -File tools/build_docs.ps1`<br>`python tools/generate_in_game_help.py` |
| **Release Validation** | `calradia-forge-release-validation` | `docs/VALIDATION-<VERSION>.md`<br>`docs/VALIDACION-<VERSION>.es.md` | Test metrics extraction (Core, ForgeWeave, Desktop MVVM, RenderTests layout ms). |

---

## 3. Mandatory Documentation Invariants

### Invariant A: Strict Bilingual Parity (EN / ES)
- Every technical markdown guide in `docs/` MUST exist as an English canonical document (`docs/<TOPIC>.md`) and an identical Spanish translation (`docs/<TOPIC>.es.md`).
- Both files MUST be modified within the same changeset. Never create or edit one without updating the other.
- **Untranslated Elements**: Code symbols, method names, CLI flags, JSON keys, file paths, and benchmark numbers must remain verbatim.

### Invariant B: Ledger Immutability & Cryptographic Integrity
- Historical revisions in `docs/CalradiaForge-Registro-Mejoras-Rev*.docx` and `docs/CalradiaForge-Registro-Mejoras.integrity.jsonl` are write-once, read-only append-only.
- **NEVER** edit, re-format, or re-hash past revisions (`Rev001` through the preceding revision).
- Always draft a new annex in `docs/append/RevXXX-<Topic>[-<date>].[es.]md` and compile via `tools/append_detailed_changelog_revision.py`. The tool strictly enforces OOXML C14N prefix invariance and read-only protection.

### Invariant C: Anti-Shadowing & Statelessness in Code Examples
- Documentation code examples for `CalradiaForge.Mod` MUST adhere to `GEMINI.md` Rule A: never show folders, namespaces, or classes named `Campaign` or `Localization`.
- Behavior examples MUST adhere to Rule B: never show `SaveableTypeDefiner` inheritance or `SyncData` serialization for mod behaviors.

### Invariant D: DocFX & Help Preflight Gate
- In-game help XML files and DocFX site output must never be committed as out-of-sync or broken stubs.
- `tools/package.ps1` mandates that `build_docs.ps1` and `generate_in_game_help.py` finish with exit code 0 before generating distribution archives.

### Invariant E: Local Markdown Planning (`RULE[user_global]`)
- Feature planning, milestone tracking, and task checklists must reside directly within the repository in Markdown (`task.md` or `PROJECT_PLAN.md`). Do not rely on external cloud task trackers.

---

## 4. Step-by-Step Workflow for `/calradia-forge-docs`

When `/calradia-forge-docs` is invoked:
1. **Analyze Change Scope**:
   - If C# architecture, lifecycles, or public SDK surfaces changed → update corresponding `docs/CODEMAP_*.md`.
   - If user-facing features or system design changed → update `docs/<TOPIC>.md` and `docs/<TOPIC>.es.md`.
   - If a significant batch of improvements or milestone was completed → draft `docs/append/RevXXX-*.md` and run the append tool.
   - If public APIs or in-game commands were modified → verify XML docstrings and run `build_docs.ps1` / `generate_in_game_help.py`.
   - If concluding a release → create `docs/VALIDATION-<VERSION>.md` and `.es.md`.
2. **Perform Updates in Synchronous Changeset**:
   - Apply edits ensuring bilingual parity.
3. **Audit & Validate**:
   - Run documentation build scripts and verify hash integrity chains if ledger was updated.
