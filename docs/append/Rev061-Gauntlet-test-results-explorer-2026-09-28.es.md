# Rev061 — Explorador de resultados de pruebas Gauntlet

**Fecha:** 28 de septiembre de 2026
**Versión:** Calradia Forge 25.2.0, sin cambios
**Alcance:** Mod, prefab Gauntlet generado, recursos de localización, pruebas y documentación.

## Necesidad observada y justificación técnica

Antes, el área Tests exponía la salida de ejecución únicamente mediante el ledger de texto compartido. Los desarrolladores necesitaban una manera de solo lectura para revisar el estado y los detalles de ejecución/limpieza de un caso sin analizar todo el payload ni repetir una prueba accidentalmente.

## Solución técnica y decisiones arquitectónicas

El ViewModel del panel conserva la respuesta estructuralmente válida más reciente de la acción existente `run` o `run-batch` y presenta los casos en una lista desplazable y un panel de detalle. Abrir la vista o seleccionar un caso son acciones de presentación. La salida original del ledger, el argumento, el historial de comandos, el filtro y la comparación de salidas permanecen intactos. Los resultados sobreviven a la navegación entre áreas y se liberan al cerrar el panel. El parser de presentación impone un límite defensivo de 51 registros; el `TestEngine` actual acepta lotes de 1 a 50 casos y rechaza los mayores.

La auditoría trata el modal como cerrado en la disposición base y valida por separado su geometría abierta en cada viewport. Comprueba los límites del modal, paneles desplazables, separación de lista/detalle, bindings y superficies pasivas. El modelo de disposición horizontal ahora asigna el espacio restante a los hijos estirables de una pila y divide el espacio entre varias columnas estirables. El resumen y el encabezado de detalle se alinean arriba para permanecer en sus franjas previstas.

## Cambios en activos, código y dependencias

El prefab Gauntlet generado añade la vista modal de lista y detalle, con etiquetas localizadas en los 13 recursos de idioma generados. La función reutiliza las respuestas existentes y los patrones `MBBindingList`; no añade rutas, comandos, API pública, cambios de protocolo ni dependencias. No se requirió modificar `SubModule.cs`. Pasó la regresión existente de flanco ascendente F10 y ciclo de vida/telemetría de GauntletLayer; esta no simula entrada nativa ni diagnostica la aserción anterior del juego.

## Validación y límites de la evidencia

- `python tools/generate_assets.py --gauntlet-only`: terminó correctamente.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause`: pasaron la revisión de sprites, los recursos generados y la auditoría estructural/de disposición Gauntlet con 0 errores y 0 avisos; Core pasó 343/343.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: pasaron las compilaciones limpias `net472` y `net8.0`, Core 343/343 y ForgeWeave 73/73.
- El TPAC instalado es anterior al atlas fuente actual. No se reemplazó ningún TPAC, no se importó desde Resource Browser y el render y el comportamiento F10 en vivo siguen pendientes. No se inició campaña ni batalla.

Este anexo añade evidencia a la revisión protegida anterior sin reescribir su contenido. La versión del producto sigue en 25.2.0; no se generó ningún ZIP.
