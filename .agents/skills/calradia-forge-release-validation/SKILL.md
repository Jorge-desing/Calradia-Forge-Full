---
name: calradia-forge-release-validation
description: Create empirical Calradia Forge release validation records with bilingual per-run test metrics, WPF harness evidence, scope limits, and package hashes.
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
**Alcance de verificación:** [Alcance exacto] de la compilación Release, suites automatizadas, harness de render WPF, verificación sin estado y auditoría de empaquetado; registrar qué pasos se ejecutaron y cuáles quedaron pendientes.

## Pruebas Superadas / Passed Verification

- `tools/Run-CalradiaForge-Tests.bat --no-pause`: Compilación y suites según el resultado real de esta ejecución; copia los conteos y el estado emitidos, sin sustituirlos por cifras históricas.
  - **Asset Pipeline & TPAC Fixtures**: [resultado y conteo observados en esta ejecución] (describir únicamente las comprobaciones ejecutadas).
  - **CalradiaForge.Tests (Core / Mod / Sdk / Net472)**: [resultado y conteo observados en esta ejecución].
  - **ForgeWeave Event Mesh Tests (Net8.0)**: [resultado y conteo observados en esta ejecución].
  - **Desktop Protocol & MVVM Tests (Net8.0)**: [resultado y conteo observados en esta ejecución].
  - **Desktop WPF Render Tests (Net8.0-windows)**: [resultado y métricas emitidas en esta ejecución]. Reporta tiempos y pases como métricas del harness, no como latencia de la aplicación en runtime.
- `tools/Verify-CalradiaForge-StatelessBehavior.bat`:
  - 0 clases derivadas de `SaveableTypeDefiner`.
  - `SyncData` completamente vacío / sin serialización de estado en behaviors sin estado.
  - 0 infracciones de shadowing con la clase `Campaign` (`GEMINI.md`).
  - Registro correcto de behaviors en `OnGameStart`.
- `tools/Package-CalradiaForge.bat`:
  - Generación y auditoría de los 3 archivos ZIP de distribución oficial.
  - Verificación de ausencia de binarios de motor vanilla (`TaleWorlds.*.dll`), ejecutables app-host (`.exe`), archivos de guardado (`.sav`) y volcados de depuración (`.log`, `.cfcrash`).

## Límites de la Evidencia / Boundaries of Evidence

Es fundamental declarar con transparencia la evidencia que no se obtuvo durante esta ejecución:
- Indicar si *Mount & Blade II: Bannerlord* se inició y qué pantalla se observó; si no se comprobó el juego, la importación en Resource Browser o el render de texturas, marcar cada punto como **pendiente/no verificado**.
- Indicar si se ejecutaron pruebas prolongadas de runtime; no inferir estabilidad o rendimiento de campaña a partir de pruebas de fuente o harness.
- Las pruebas de renderizado WPF pueden ejecutarse en un harness en memoria (`ShowActivated=false`). Informar sus resultados como mediciones del harness, separadas de observaciones de la aplicación abierta y del DPI real de Windows.
- Distinguir las comprobaciones TPAC disponibles (por ejemplo, marcador/cabecera, inventario o estructura) de decodificación de píxeles, importación efectiva y render dentro del juego. No declarar estas últimas verificadas salvo que se hayan observado.

## Huellas Criptográficas Oficiales (SHA-256)

Extraídas de `artifacts/package-sha256-<version>.txt`:
- `CalradiaForge-Modules-<version>.zip`: `[HASH]`
- `CalradiaForge-Source-SDK-<version>.zip`: `[HASH]`
- `CalradiaForge-Desktop-<version>.zip`: `[HASH]`
```

---

## 2. Bilingual Parity Requirement

- `docs/VALIDATION-<VERSION>.md` and its established Spanish alias `docs/VALIDACION-<VERSION>.es.md` MUST be generated simultaneously; do not create a second `.es.md` variant under the English prefix.
- Metrics, numbers, test names, timings, and SHA-256 hashes must match exactly between both files.

---

## 3. Data Gathering Process

1. Run the test suite through its maintained BAT launcher, saving the raw log or capturing stdout:
   ```cmd
   tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause <nul
   ```
2. Record the summary lines and metrics actually emitted by this run. Counts, suite composition, render cases, layout passes, timings, and summary wording can change between executions. If a parser or regular expression extracts them, compare its output with the raw log and do not encode historical totals as expected current values.
3. Inspect `artifacts/package-sha256-<version_clean>.txt` for exact cryptographic hashes.
4. Record whether the package pipeline completed and which archive-audit and hash checks passed; do not infer completion from the presence of old ZIP files.
