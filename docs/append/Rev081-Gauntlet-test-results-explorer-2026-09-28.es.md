# Rev081 — Explorador de resultados de pruebas Gauntlet

**Fecha:** 28 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Mod y recursos de localización. Presentación estructurada de las respuestas existentes de ejecución de pruebas Gauntlet.

## Necesidad observada y justificación técnica

El área Tests presenta actualmente la salida de ejecución en el ledger de texto compartido. Aunque conserva los diagnósticos, dificulta revisar un resultado de un lote junto con su contexto de ejecución y el resultado de limpieza. El explorador separa la navegación y el detalle sin añadir otra forma de ejecutar pruebas.

## Solución técnica y decisiones arquitectónicas

La vista de resultados, limitada a la vida del panel, consume la respuesta estructuralmente válida más reciente de la acción existente `run` o `run-batch`. Muestra hasta 51 registros en una lista desplazable, con ID técnico, estado y duración. Al seleccionar un registro, presenta semilla, contexto, `StartedAt`, pasos, error y error de limpieza. Los valores técnicos se conservan literalmente; solo se localizan las etiquetas de interfaz y el estado vacío. Los casos fallidos permanecen identificados como fallidos.

Abrir la vista y seleccionar una fila son operaciones de presentación y nunca invocan ni repiten una prueba. Los resultados sobreviven a la navegación entre áreas y se liberan al cerrar el panel. La vista no reemplaza ni modifica la salida original del ledger, `Argument`, el historial de comandos, el filtro de salida ni la comparación de salidas.

## Cambios en activos, código y dependencias

La guía de diseño de Gauntlet y el changelog bilingüe describen la lista estructurada de resultados y la superficie de detalle. La función reutiliza las respuestas de pruebas existentes y los patrones de binding de Gauntlet; no introduce rutas, comandos, API pública, cambios de protocolo ni dependencias externas. La versión del producto sigue en 25.2.0.

## Validación y límites de la evidencia

- La integración de fuente, la paridad de recursos localizados, las comprobaciones estructurales y las pruebas de ejecución de esta función siguen pendientes; no se ejecutó ningún comando de pruebas para este anexo.
- No se realizó una inspección en vivo de Bannerlord para este registro. La apertura de la vista y la selección de resultados no se han confirmado dentro del juego.
- Este anexo registra el comportamiento de la función sin afirmar validaciones aún no observadas.

Este anexo añade evidencia a la revisión anterior sin reemplazar sus párrafos.
