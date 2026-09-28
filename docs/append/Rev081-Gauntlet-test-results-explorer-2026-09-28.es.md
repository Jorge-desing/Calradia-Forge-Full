# Rev081 — Explorador de resultados de pruebas Gauntlet

**Fecha:** 28 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Mod y recursos de localización. Presentación estructurada de las respuestas existentes de ejecución de pruebas Gauntlet.

## Necesidad observada y justificación técnica

El área Tests presenta actualmente la salida de ejecución en el ledger de texto compartido. Aunque conserva los diagnósticos, dificulta revisar un resultado de un lote junto con su contexto de ejecución y el resultado de limpieza. El explorador separa la navegación y el detalle sin añadir otra forma de ejecutar pruebas.

## Solución técnica y decisiones arquitectónicas

La vista de resultados, limitada a la vida del panel, consume la respuesta estructuralmente válida más reciente de la acción existente `run` o `run-batch`. El parser conserva como máximo 51 registros como límite defensivo en una lista desplazable, con ID técnico, estado y duración. El `TestEngine` actual acepta lotes de 1 a 50 pruebas y rechaza lotes mayores, por lo que una respuesta válida de lote contiene como máximo 50 registros. Al seleccionar un registro, presenta semilla, contexto, `StartedAt`, pasos, error y error de limpieza. Los valores técnicos se conservan literalmente; solo se localizan las etiquetas de interfaz y el estado vacío. Los casos fallidos permanecen identificados como fallidos.

Abrir la vista y seleccionar una fila son operaciones de presentación y nunca invocan ni repiten una prueba. Los resultados sobreviven a la navegación entre áreas y se liberan al cerrar el panel. La vista no reemplaza ni modifica la salida original del ledger, `Argument`, el historial de comandos, el filtro de salida ni la comparación de salidas.

## Cambios en activos, código y dependencias

La guía de diseño de Gauntlet y el changelog bilingüe describen la lista estructurada de resultados y la superficie de detalle. La función reutiliza las respuestas de pruebas existentes y los patrones de binding de Gauntlet; no introduce rutas, comandos, API pública, cambios de protocolo ni dependencias externas. La versión del producto sigue en 25.2.0.

## Validación y límites de la evidencia

- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` pasó las auditorías de sprites y prefab/disposición con 0 errores y 0 avisos; su suite Core pasó 343/343.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` pasó las compilaciones `net472` y `net8.0` limpias, Core 343/343 y ForgeWeave 73/73. Pasaron los bindings estructurales y los 13 recursos de idioma generados.
- Pasó la regresión existente de flanco ascendente F10 y ciclo de vida/telemetría de GauntletLayer. Solo comprueba la estructura del código, no la entrada en vivo; la aserción nativa anterior y el overlay dentro del juego siguen sin verificarse, y no se justificó cambiar `SubModule.cs`.
- No se realizó una inspección en vivo de Bannerlord para este registro. El TPAC instalado es anterior al atlas fuente actual, así que la importación y el render dentro del juego siguen pendientes.

Este anexo añade evidencia a la revisión anterior sin reemplazar sus párrafos.
