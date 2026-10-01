---
name: auto-packaging
description: Enforce automatic generation of distribution .zip files at the end of major updates.
trigger: always_on
---

# Auto-Packaging Workflow

This rule is language-independent. It applies to requests and deliveries in English, Spanish and any other language, and to every existing supported interface/resource localization. Package applicable localized resources and verify existing parity; do not silently omit them based on the conversation language. This requirement does not add new supported languages or authorize invented translations.

Whenever a major update is performed, a new version is reached, a milestone or significant batch of code/features is completed, or packaging is explicitly requested, you MUST package the project for distribution before declaring completion. An unchanged version does not waive this requirement. Explicit user instructions not to generate ZIPs for the current request take precedence. Documentation-only maintenance does not independently trigger packaging.

1. **Trigger Condition:** Upon completing an update or feature set, before concluding the goal.
2. **Action:** Execute the workspace's packaging scripts (e.g., `tools\package.ps1`).
3. **Verification:** Require the canonical pipeline to finish successfully, all three current-version ZIPs to exist, the archive audit to pass, and independently computed SHA-256 hashes to match the generated manifest. Use the actual evidence filenames emitted by the script; do not assume a dotted version in audit/hash filenames.
4. **Delivery:** Provide absolute clickable paths to Modules, Source-SDK and Desktop ZIPs and their audit/hash evidence. Commits, local tests and green CI do not prove the archives exist. If packaging fails or a prerequisite is missing, report it as incomplete with the actual error; never silently skip it.
5. **Scope:** Keep archives, staging directories, audit reports and hash manifests in ignored `artifacts/`, outside Git commits. This rule does not authorize a version bump, release publication, push, installation or game launch. Commit intentional project changes at objective completion under the Git workflow.

## FastPackageEngine Principles & Performance Constraints
El pipeline de empaquetado utiliza la arquitectura `FastPackageEngine` para minimizar la latencia de empaquetado y garantizar higiene absoluta:
- **Poda Temprana de Subárboles (Top-Level Pruning):** Descartar carpetas de compilación y control de versiones (`.git`, `bin`, `obj`, `__pycache__`, `artifacts`, `.vs`, `.idea`) en la raíz del árbol antes de recorrer subdirectorios, evitando millones de verificaciones de ruta innecesarias.
- **Staging Cero-Residuos en Memoria:** Copiar y archivar únicamente los archivos explícitamente permitidos (allowlist) o filtrados en memoria, sin realizar copias completas que luego requieran pasadas secundarias de borrado en disco.
- **Compresión Concurrente Multihilo:** Los tres paquetes de distribución (`CalradiaForge-Modules`, `CalradiaForge-Source-SDK` y `CalradiaForge-Desktop`) se empaquetan en paralelo aprovechando múltiples hilos de CPU.
- **Niveles de Compresión:** Usar compresión optimizada (`CompressionLevel.Optimal`) para entregas finales a producción y verificar la generación de hashes SHA-256 e informes de auditoría (`package-audit-<version>.json`).

