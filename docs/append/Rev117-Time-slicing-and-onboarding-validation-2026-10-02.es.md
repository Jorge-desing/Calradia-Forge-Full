# Rev117 — Casos límite de time-slicing y validación de incorporación

**Fecha:** 2 de octubre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** SDK, Core, Mod, Tools y documentación. Agrupación de trabajo con desbordamiento seguro, etiquetas generadas de Gauntlet, generación de recursos de localización, evidencia de diagnóstico opcional de patches externos y consolidación de guías de agentes.

## Problema observado y justificación técnica

La normalización de grupos horarios estables podía desbordarse con cantidades positivas grandes de grupos. El compositor de páginas Gauntlet ya exponía una etiqueta localizada en el ViewModel enlazado, pero no la mostraba sobre los campos editables generados. El texto de ayuda sobre trabajo periódico estaba en los catálogos fuente, pero la cobertura de los recursos generados reveló que una prueba exigía incorrectamente que la guía también fuera una clave de control del menú nativo. Los informes sin snapshot de diagnóstico externo también debían distinguir «no capturado» de «runtime no cargado».

## Solución técnica y decisiones arquitectónicas

La normalización de grupos horarios ahora admite todo el rango positivo de `int` y comparte la ruta entre ambos overloads de `ProcessBatch`. Esto no reduce el costo de recorrer y calcular el hash de cada ID de entidad, no garantiza cargas uniformes por grupo y no vuelve apropiado el aplazamiento para callbacks sensibles a la latencia. El compositor generado de Gauntlet muestra el `Label` localizado existente sin cambiar rutas, comandos, bindings ni API pública. La regresión de localización valida los catálogos fuente y recursos de juego generados, en lugar de exigir que un párrafo de ayuda general pertenezca al catálogo separado de controles del menú. La ausencia de un snapshot externo se informa como `NotCaptured`; no se infiere que el runtime esté ausente. Las skills y las tres guías principales de agentes expresan los límites actuales de evidencia para time-slicing, mediciones, observación Harmony e incorporación local del SDK.

## Cambios en activos, código y dependencias

- Se endureció la normalización de `ForgeTimeSlicer` y se agregaron regresiones de desbordamiento para `ShouldProcess` y ambos overloads de `ProcessBatch`.
- Se renderiza la propiedad existente `Label` de cada componente Gauntlet y se conserva el contrato del campo editable.
- Se corrigió el generador de recursos para textos de panel independientes y se quitó una aserción incorrecta sobre el menú nativo de las pruebas de localización.
- Se mantuvo la independencia de Harmony: Forge no referencia, carga, empaqueta ni modifica Harmony; la reflexión opcional solo observa un ensamblado ya cargado que recibe el llamador.
- Se actualizaron las guías bilingües de evolución SDK con el último informe aislado de paquetes/plantilla y sus hashes; también las reglas y skills especialistas, sin alterar revisiones anteriores del ledger.
- No cambiaron la versión del producto, la versión de API pública del SDK, los TFM ni la dependencia de TaleWorlds.

## Validación y límites de la evidencia

- `tools/Run-CalradiaForge-Tests.bat --no-pause`: compilación completa, cero advertencias/errores; Core 404/404, ForgeWeave 73/73, Desktop 65/65, 295 casos WPF y 308 pasadas de layout/render (16.098 ms en el arnés).
- `tests/CalradiaForge.PatchDiagnostics.Tests.bat --no-pause`: 28/28.
- `tools/Verify-CalradiaForge-StatelessBehavior.bat`: aprobado.
- `tools/Test-CalradiaForge-Developer-Onboarding.bat --no-pause`: pasaron smoke de paquetes SDK y plantilla, instalación aislada, generación/restauración/compilación del módulo; informe `artifacts/sdk-evolution/onboarding/20261002T033001Z-00c3d2e2/source-package-report.json`.
- `tools/Test-CalradiaForge-ContentShowcase.bat --no-pause`: pasaron generación determinista, esquema/manifiesto, compilación del proyecto y comprobaciones Core y ForgeWeave.
- `tools/Run-CalradiaForge-Python-Checks.bat --no-pause`: pasaron herramientas Python base, Ruff, 25 fixtures de assets, cinco fixtures de imagen, cinco pruebas offline de utilidades de agentes y auditorías estructurales Gauntlet. El perfil opcional Antigravity no se instaló: pip devolvió `InvalidChunkLength`.
- Las cinco skills especialistas modificadas pasaron `tools/Validate-CalradiaForge-Skills.bat`; la auditoría de conocimiento de incorporación indicó cero problemas.
- Los tiempos del arnés no son latencia de callbacks en Bannerlord ni de la aplicación. No se verificaron una sesión real de Bannerlord, el análisis profundo de TPAC ni el render Gauntlet dentro del juego. La prueba del diagnóstico externo usa un fixture de ensamblado sintético y no prueba coexistencia real con Harmony.

Este anexo añade evidencia a la revisión anterior sin reemplazar su texto. Las rutas, los comandos, las API públicas y la versión del producto permanecen sin cambios.
