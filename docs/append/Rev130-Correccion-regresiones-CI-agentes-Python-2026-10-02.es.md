# Rev130 — Corrección de regresiones CI de agentes Python

**Fecha:** 02-10-2026  
**Versión:** Calradia Forge 25.2.0, sin cambios; `ForgeApi.Version` 13  
**Alcance:** Herramientas de desarrollo, diagnósticos de agentes Python, pruebas y documentación.

## Problema observado y justificación técnica

La ejecución Windows hospedada para el commit `7cb521be249a88a1ef8150d5e74eb7094e06b7ae` instaló correctamente el perfil opcional de Antigravity y completó los gates de compilación/pruebas portables y stateless. Su último paso Python falló tres pruebas de `tests/test_forge_agents.py`: una aserción del reporte offline aún llamaba subagentes activos a cinco roles especialistas configurados, otra esperaba la palabra obsoleta `matched` en la auditoría de paridad, y una fixture de pruebas exitosas esperaba un conteo de assets que el compactor no emitía.

Los logs mostraron que el reporte offline actual dice correctamente `Configured Specialist Roles` y `Cloud-Agent Execution: not used on this route`; la auditoría de paridad dice `100% parity ... and matching Spanish counterparts`. El parser de assets usaba `.*OK`, que no cruza el salto de línea del formato estándar de Python: `Ran N tests ...` seguido por `OK`.

## Solución técnica y decisiones arquitectónicas

El compactor ahora reconoce el formato estándar de éxito de `unittest` en dos líneas y exige la marca explícita `OK`. La regresión comprueba que el resumen conserva el conteo de pruebas de assets. Las pruebas de agentes ahora validan los roles configurados y la ausencia explícita de ejecución cloud en la ruta offline; las aserciones de paridad corresponden a la redacción real y toleran que cambie la cantidad de guías.

No cambiaron el código runtime de Forge, la integración Harmony, las dependencias del producto ni el comportamiento del juego. Esta corrección no implica que los roles configurados se estén ejecutando como subagentes independientes.

## Cambios en activos, código y dependencias

- Actualización de `agents/compactor.py` para reconocer `OK` en la línea posterior a `Ran N tests`.
- Corrección de aserciones obsoletas en `tests/test_forge_agents.py` y nueva regresión en `tests/test_forge_tool_audits.py`.
- Entradas bilingües de changelog. No cambian assets, manifiestos de paquetes, firmas del SDK ni dependencias del juego.

## Validación y límites de la evidencia

- Pasó `tools\\Run-CalradiaForge-Python-Checks.bat --ci --no-pause` tras el arreglo: Ruff, 25 pruebas del pipeline de assets, 5 del pipeline de imágenes, auditorías de herramientas independientes del SDK, pruebas de orquestación offline, launcher CLI y comprobaciones estructurales de sprites/marcador TPAC.
- Antes de la corrección, la suite hospedada opcional de agentes se ejecutó y reportó exactamente las tres aserciones fallidas anteriores; el log de setup confirma la instalación de `google-antigravity 0.1.20` y `google-genai 2.28.0` en el runner.
- No se pudo instalar localmente el perfil `--agents` porque pip devolvió `InvalidChunkLength`. Al anexar esta revisión, está pendiente la repetición hospedada posterior al arreglo.
- Las comprobaciones estáticas prueban límites de código/paquetes solamente. No se afirma decodificación de píxeles TPAC ni validación en vivo de Bannerlord, campaña, batalla o interfaz del juego.

Este apéndice añade evidencia sin reemplazar revisiones anteriores. Conserva los comandos, las rutas, los contratos y la versión del producto.
