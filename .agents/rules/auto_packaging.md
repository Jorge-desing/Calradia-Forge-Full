---
name: auto-packaging
description: Enforce automatic generation of distribution .zip files at the end of major updates.
trigger: always_on
---

# Auto-Packaging Workflow

Whenever a major update is performed, a new version is reached, or a significant batch of features is completed, you MUST automatically package the project for distribution.

1. **Trigger Condition:** Upon completing an update or feature set, before concluding the goal.
2. **Action:** Execute the workspace's packaging scripts (e.g., `tools\package.ps1`).
3. **Delivery:** Clearly state in your summary response that the ZIP files were generated and provide their exact output paths.

## FastPackageEngine Principles & Performance Constraints
El pipeline de empaquetado utiliza la arquitectura `FastPackageEngine` para minimizar la latencia de empaquetado y garantizar higiene absoluta:
- **Poda Temprana de Subárboles (Top-Level Pruning):** Descartar carpetas de compilación y control de versiones (`.git`, `bin`, `obj`, `__pycache__`, `artifacts`, `.vs`, `.idea`) en la raíz del árbol antes de recorrer subdirectorios, evitando millones de verificaciones de ruta innecesarias.
- **Staging Cero-Residuos en Memoria:** Copiar y archivar únicamente los archivos explícitamente permitidos (allowlist) o filtrados en memoria, sin realizar copias completas que luego requieran pasadas secundarias de borrado en disco.
- **Compresión Concurrente Multihilo:** Los tres paquetes de distribución (`CalradiaForge-Modules`, `CalradiaForge-Source-SDK` y `CalradiaForge-Desktop`) se empaquetan en paralelo aprovechando múltiples hilos de CPU.
- **Niveles de Compresión:** Usar compresión optimizada (`CompressionLevel.Optimal`) para entregas finales a producción y verificar la generación de hashes SHA-256 e informes de auditoría (`package-audit-<version>.json`).

