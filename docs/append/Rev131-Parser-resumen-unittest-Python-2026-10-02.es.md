# Rev131 — Parser del resumen unittest de Python

**Fecha:** 02-10-2026

**Versión:** Calradia Forge 25.2.0, sin cambios; `ForgeApi.Version` 13

**Alcance:** Herramientas de desarrollo, resumen de salida Python, pruebas de regresión y documentación.

## Problema observado y justificación técnica

La revisión final de la corrección CI Rev130 encontró que su fixture usaba `Ran 8 tests ...` seguido inmediatamente por `OK`. El `unittest` estándar de Python puede insertar una línea vacía entre ambas líneas y emplea `Ran 1 test ...` cuando solo hay una prueba. Por ello, el primer ajuste del parser todavía omitía una salida exitosa normal.

## Solución técnica y decisiones arquitectónicas

El parser del resumen de pruebas acepta uno o más saltos de línea y espacios horizontales antes del marcador explícito de éxito `OK`. Reconoce `test` y `tests` en el resumen del ejecutor. Las fixtures cubren un salto simple, un separador vacío y CRLF con la forma singular. La clasificación existente de éxito y error no cambia.

## Cambios en activos, código y dependencias

- Se actualizó `agents/compactor.py` para analizar las variantes observadas de salida del ejecutor Python.
- Se ampliaron las pruebas de `tests/test_forge_tool_audits.py` para cubrir cada forma de salto y número gramatical.
- Se agregaron entradas bilingües al changelog y esta revisión append-only. No cambió el SDK runtime, dependencias del producto, API pública, integración Harmony ni código del juego.

## Validación y límites de la evidencia

- Pasó `tools\Run-CalradiaForge-Python-Checks.bat --ci --no-pause` después del cambio: Ruff, 25 pruebas de assets, 5 de imágenes, 20 auditorías de herramientas, 15 pruebas del orquestador offline, 3 del launcher CLI y comprobaciones estructurales de sprites/marcador TPAC.
- `tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` verificó la paridad bilingüe de 42 pares documentales y la cadena de 130 registros antes de este nuevo apéndice; el launcher de anexado y el verificador de integridad validarán Rev131.
- La suite opcional `--agents` no está disponible localmente porque falta el perfil Antigravity; un intento previo de pip terminó con `InvalidChunkLength`. La ejecución hospedada posterior al arreglo sigue pendiente.
- No se afirma validación en Bannerlord, campaña, batalla, decodificación de píxeles TPAC ni renderizado dentro del juego.

Este apéndice añade una revisión nueva sin reescribir Rev130 ni los registros anteriores. Conserva la versión del producto, las rutas, los contratos públicos y el comportamiento del juego.
