# Rev118 — Corrección del gate de paridad documental

**Fecha:** 2 de octubre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Herramientas, pruebas de agentes, reglas y skills de documentación. Clasificación correcta de contrapartes inglés/español y cumplimiento obligatorio del resultado de auditoría documental.

## Problema observado y justificación técnica

La auditoría documental trataba `SYSTEM_DESIGN.md`, que está en inglés, como alias en español de `ARCHITECTURE.md`. Por ello podía informar paridad aunque faltara `SYSTEM_DESIGN.es.md` u otra contraparte real. Además, el runner del ledger imprimía un informe de paridad incompleto sin fallar. Al contar correctamente los documentos de arquitectura y diseño de sistemas, el repositorio tenía 42 guías inglesas.

## Solución técnica y decisiones arquitectónicas

La auditoría ahora trata los documentos de arquitectura y diseño de sistemas como pares ingleses/españoles estándar y conserva alias únicamente para Desktop y los registros de validación versionados. El runner del ledger requiere el marcador explícito de éxito que emite la auditoría. Las regresiones cubren pares completos de arquitectura, la ausencia de la contraparte del diseño de sistemas, las traducciones faltantes a través del runner y el conjunto actual de 42/42 documentos.

## Cambios en activos, código y dependencias

- Se eliminó el falso alias `ARCHITECTURE.md` → `SYSTEM_DESIGN.md` y la clasificación de `SYSTEM_DESIGN.md` como archivo español.
- Se añadió cobertura de regresión para el emparejamiento corregido y el gate obligatorio del runner.
- Se sincronizaron el gateway de documentación, la regla documental, la skill de validación de versiones y las instrucciones de `AGENTS.md`, `CODEX.md` y `GEMINI.md`.
- No cambiaron la versión del producto, la API del SDK, las dependencias, las rutas ni el comportamiento del juego.

## Validación y límites de la evidencia

- `tools/Run-CalradiaForge-Python-Checks.bat --no-pause`: Ruff aprobado; Asset Pipeline 25/25; pruebas de imágenes 5/5; regresiones de auditoría de agentes 10/10; comprobaciones estructurales de sprites/recursos aprobadas.
- `tools/Run-CalradiaForge-Python-Checks.bat --ledger --no-pause`: paridad documental 42/42, sin contrapartes faltantes; la cadena del ledger protegido anterior se verificó hasta la revisión 117 antes de este append.
- `tools/Validate-CalradiaForge-Skills.bat` aprobó las skills especialistas modificadas.
- Esto valida documentación y herramientas estáticas del repositorio; no demuestra comportamiento en vivo dentro de Bannerlord.

Este anexo amplía la revisión protegida anterior sin reescribirla. La versión del producto y los contratos públicos no cambian.
