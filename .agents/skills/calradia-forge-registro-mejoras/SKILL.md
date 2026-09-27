---
name: calradia-forge-registro-mejoras
description: Draft, compile, and record immutable append-only changelog revisions to the Calradia Forge "Registro de Mejoras" with Word read-only protection and cryptographic SHA-256 hash chain verification.
metadata:
  version: "1.0.0"
  author: "calradia-forge-team"
---

# Calradia Forge Registro de Mejoras Skill

Use this skill whenever documenting major feature batches, UI/UX renovations, architectural overhauls, or milestone releases into the immutable, cryptographically verifiable **Registro de Mejoras**.

---

## 1. Core Principles of the Ledger

1. **Strict Immutability & Append-Only:**
   - Previous revisions (`CalradiaForge-Registro-Mejoras-Rev001.docx` through `RevXXX.docx`) are never edited, overwritten, or modified.
   - Every new revision is created by taking the previous DOCX, verifying that every single paragraph and style remains an exact, identical prefix, and appending a new appendix.
2. **Canonical OOXML C14N Validation:**
   - In addition to text/style checking, the compiler performs an XML canonicalization (`c14n`) check on body paragraph elements (`<w:p>`) to guarantee zero subtle metadata or styling tampering.
3. **Word Read-Only Protection:**
   - The compiled document is locked using Word's non-password read-only enforcement (`w:documentProtection edit="readOnly" enforcement="1"`).
4. **Cryptographic SHA-256 Hash Chain:**
   - Every revision is recorded sequentially in `docs/CalradiaForge-Registro-Mejoras.integrity.jsonl`.
   - Each record contains the file name, the file's SHA-256 digest, the hash of the previous record (blockchain-style linking), a UTC timestamp, and a canonical record digest.

---

## 2. Step-by-Step Append Workflow

### Step 1: Discover Next Revision Number
Inspect existing files in `docs/`:
```powershell
python -c "import re, pathlib; docs = pathlib.Path('docs'); revs = [int(m.group(1)) for p in docs.glob('CalradiaForge-Registro-Mejoras-Rev*.docx') if (m := re.fullmatch(r'CalradiaForge-Registro-Mejoras-Rev(\d{3})\.docx', p.name))]; print(f'Next revision: Rev{max(revs) + 1:03d}')"
```

### Step 2: Draft Markdown Annex in `docs/append/`
Create a new file following the naming convention:
`docs/append/RevXXX-<Topic>[-<date>].[es.]md` (e.g., `docs/append/Rev023-Gauntlet-visual-round3-2026-09-23.md`).

#### Required Structure:
```markdown
# RevXXX — [Descriptive Title in Spanish]

**Fecha:** [DD de mes de YYYY]  
**Versión:** Calradia Forge [X.Y.Z], [sin cambios | con incremento de versión]  
**Alcance:** [Mod / Desktop / Sdk / Core / Assets / Tools / Docs]. [Specific subsystems affected]

## Problema observado y justificación técnica
[Detailed description of what problem occurred, why it needed fixing or optimization, including symptoms and repro]

## Solución técnica y decisiones arquitectónicas
[Technical explanation of the solution: algorithms, data structures, zero GC allocation guarantees, anti-lag time-slicing, styling rules, or thread safety]

## Cambios en activos, código y dependencias
[Specific files modified, functions changed, resources removed/optimized, byte budget deltas]

## Validación y límites de la evidencia
- [Exact commands executed: e.g. tools/Run-CalradiaForge-Tests.bat]
- [Test suites passed, test counts: e.g. 51 passed Desktop, 250 passed Core]
- [Performance timings and layout pass counts: e.g. 150 layout passes, 883.5 ms]
- [Explicit boundary of evidence: e.g. "Bannerlord remained closed; no live endurance battle was executed"]

Este anexo añade evidencia a la revisión anterior sin reemplazar sus párrafos. Mantiene los comandos, las rutas, MVVM, las API públicas y la versión del producto.
```

### Step 3: Execute Compilation Tool
Run the append tool:
```bash
python tools/append_detailed_changelog_revision.py docs/append/RevXXX-<Topic>.md
```

#### What the Tool Does Automatically:
1. Acquires lock `docs/.CalradiaForge-Registro-Mejoras.append.lock`.
2. Validates that there are no gaps in the existing revision sequence.
3. Loads `CalradiaForge-Registro-Mejoras-Rev(XXX-1).docx` and appends the Markdown content.
4. Verifies paragraph prefix matching and OOXML C14N equality against the previous version.
5. Injects Word read-only protection into `word/settings.xml`.
6. Saves `docs/CalradiaForge-Registro-Mejoras-RevXXX.docx` and marks it read-only on disk (`chmod S_IREAD`).
7. Invokes `tools/record_detailed_changelog_integrity.py` which computes the SHA-256 digest and appends a verified entry to `docs/CalradiaForge-Registro-Mejoras.integrity.jsonl`.
8. Releases the lock.

### Step 4: Verify Hash Chain
Verify the integrity ledger:
```bash
python tools/record_detailed_changelog_integrity.py
```
Should output JSON reporting all verified records with zero errors.

---

## 3. Common Error Scenarios & Troubleshooting

- **Lock Collision (`.integrity.lock` or `.append.lock`):** If a previous run crashed or was aborted, verify that no Python process is active, inspect the lock file, and delete it if stale.
- **Prefix Invariance Failure:** Occurs if python-docx altered an existing table, style, or paragraph during document loading. Never use generic word automation; strictly rely on `append_detailed_changelog_revision.py`.
- **Holes in Revisions:** All numbers from `Rev001` to `RevXXX` must exist contiguously. If a number is missing, the tool will refuse to execute to avoid ambiguous hash chains.
