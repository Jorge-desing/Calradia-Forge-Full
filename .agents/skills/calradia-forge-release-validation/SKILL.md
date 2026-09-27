---
name: calradia-forge-release-validation
description: Create and maintain official release validation records (VALIDATION-<VERSION>.md and VALIDACION-<VERSION>.es.md) capturing empirical test runs, WPF render metrics, evidence boundaries, and package SHA-256 digests in Calradia Forge.
metadata:
  version: "1.0.0"
  author: "calradia-forge-team"
---

# Calradia Forge Release Validation Skill

Use this skill whenever concluding a version release, milestone update, or packaging pass to author the official empirical validation records:
- English: `docs/VALIDATION-<VERSION>.md`
- Spanish: `docs/VALIDACION-<VERSION>.es.md`

---

## 1. Structure of a Validation Record

Every validation record represents an immutable, empirical audit of the exact build and test execution performed prior to shipping.

### Standard Markdown Layout:

```markdown
# Calradia Forge <VERSION> Validation / Validación de Calradia Forge <VERSION>

**Fecha de ejecución:** [YYYY-MM-DD]  
**Entorno de pruebas:** Windows [Version], .NET SDK [Version], Visual Studio Build Tools, PowerShell / cmd.  
**Alcance de verificación:** Compilación Release, batería completa de pruebas automatizadas, pruebas de renderizado WPF, verificación sin estado y auditoría de empaquetado.

## Pruebas Superadas / Passed Verification

- `tools/Run-CalradiaForge-Tests.bat --no-pause`: Compilación exitosa con 0 errores y 0 advertencias.
  - **Asset Pipeline & TPAC Fixtures**: [X/X passed] (Lectura IHDR, validación de atlas 2048x256, inspección estructural de cabecera v2).
  - **CalradiaForge.Tests (Core / Mod / Sdk / Net472)**: [X/X passed] (Verificación de listeners no serializados, ciclo de vida SubModule, mitigación de lag modulo-24, scoring dinástico).
  - **ForgeWeave Event Mesh Tests (Net8.0)**: [X/X passed] (Aislamiento de fallos, cuarentena, presupuestos de ejecución, replay registry, políticas Gauntlet UI).
  - **Desktop Protocol & MVVM Tests (Net8.0)**: [X/X passed] (PipeClient named-pipe roundtrips, atomic preferences, 13 idiomas, cancelación de comandos).
  - **Desktop WPF Render Tests (Net8.0-windows)**: [X/X passed] ([P] pases de render/layout, [M] ms en llamadas sincrónicas, [N] nodos de árbol visual visitados).
- `tools/verify_stateless_behavior.ps1`:
  - 0 clases derivadas de `SaveableTypeDefiner`.
  - `SyncData` completamente vacío / sin serialización de estado en behaviors sin estado.
  - 0 infracciones de shadowing con la clase `Campaign` (`GEMINI.md`).
  - Registro correcto de behaviors en `OnGameStart`.
- `tools/package.ps1`:
  - Generación y auditoría de los 3 archivos ZIP de distribución oficial.
  - Verificación de ausencia de binarios de motor vanilla (`TaleWorlds.*.dll`), ejecutables app-host (`.exe`), archivos de guardado (`.sav`) y volcados de depuración (`.log`, `.cfcrash`).

## Límites de la Evidencia / Boundaries of Evidence

Es fundamental declarar con total transparencia qué aspectos **NO** fueron probados durante la sesión:
- El juego *Mount & Blade II: Bannerlord* **NO fue ejecutado en vivo**.
- No se realizaron batallas personalizadas de rendimiento prolongado ni campañas de resistencia de varios años de simulación continua.
- Las pruebas de renderizado WPF se ejecutaron en el harness de pruebas en memoria (`ShowActivated=false`), no en una sesión interactiva humana con monitores físicos múltiples.
- La verificación de TPAC es estructural a nivel de cabecera y tabla de contenidos; no decodifica los píxeles DXT5 de las texturas de juego.

## Huellas Criptográficas Oficiales (SHA-256)

Extraídas de `artifacts/package-sha256-<version>.txt`:
- `CalradiaForge-Modules-<version>.zip`: `[HASH]`
- `CalradiaForge-Source-SDK-<version>.zip`: `[HASH]`
- `CalradiaForge-Desktop-<version>.zip`: `[HASH]`
```

---

## 2. Bilingual Parity Requirement

- Both `VALIDATION-<VERSION>.md` and `VALIDACION-<VERSION>.es.md` MUST be generated simultaneously.
- Metrics, numbers, test names, timings, and SHA-256 hashes must match exactly between both files.

---

## 3. Data Gathering Process

1. Run the test suite saving the raw log or capture stdout:
   ```cmd
   tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause <nul
   ```
2. Extract metrics from the summary lines:
   - "RESULT: 250 passed, 0 failed"
   - "RESULT: 37 passed, 0 failed"
   - "RESULT: 51 passed, 0 failed"
   - "PASS 274 WPF render cases; [ms] ms."
   - "PERF [passes] render/layout passes, [ms] ms in those calls; visual-tree snapshots [builds] builds / [hits] hits / [nodes] visited nodes."
3. Inspect `artifacts/package-sha256-<version_clean>.txt` for exact cryptographic hashes.
4. Commit both validation documents to `docs/`.
