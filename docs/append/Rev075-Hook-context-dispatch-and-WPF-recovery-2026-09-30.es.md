# Rev075 — Distribución de hooks limitada por contexto y recuperación en WPF

**Fecha:** 30 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Distribución de hooks en Core, planificación del host Bannerlord, Hook Workbench de Desktop, pruebas y documentación bilingüe.

## Problema observado y justificación técnica

La puerta del menú principal limitaba Apply y Revert, pero un detour ya instalado aún podía ejecutar sus callbacks personalizados después de que el host saliera de ese contexto. Hook Workbench de WPF tampoco permitía seleccionar hooks `Conflict` o `Failed` para intentar una recuperación explícita, y el plan Apply para varios hooks no avisaba que un hook anterior podía quedar aplicado si fallaba uno posterior.

## Solución técnica y decisiones arquitectónicas

La distribución de hooks ahora vuelve a comprobar la política del host antes de ejecutar cualquiera de los callbacks. Si la política es falsa o produce una excepción, la distribución llama al método original sin modificarlo. El detour permanece instalado hasta que se revierta explícitamente; los callbacks se reanudan cuando vuelve el contexto aprobado del menú principal en el hilo del juego. La gestión Apply/Revert sigue limitada a la pantalla exacta del menú principal; no se supone que un detour instalado desaparezca al salir del menú.

Los validadores de planes del host y WPF ahora permiten intentar Revert explícito en snapshots `Conflict` y `Failed`, además de `Applied`. El workbench muestra una advertencia localizada y permanente sobre el ciclo de vida del hook y otra advertencia del plan: Apply por lote es secuencial y no atómico. La recuperación sigue requiriendo la confirmación normal de un solo uso y el contexto aprobado del host.

## Cambios en activos, código y dependencias

- Se añadió una comprobación de contexto en cada invocación del distribuidor sin cambiar el contrato público de hooks ni la versión de API.
- Se agregó cobertura de fixture serial x64 para callbacks omitidos fuera del gate del host, callbacks reactivados al volver el gate, cancelación de un destino no-void con resultado válido y restauración exacta del destino.
- Se habilitó en WPF la selección para recuperación de estados `Conflict`/`Failed`, se activaron los planes Revert correspondientes en el host y se localizaron las advertencias de ciclo de vida y Apply parcial en los 13 diccionarios de Desktop.
- Se actualizaron la guía de Patch Blueprint en inglés y español y la guía compartida del ciclo de vida Bannerlord, incluida la acción `hook-plan-cancel`.
- La versión del producto permanece en 25.2.0; `ForgeApi.Version` permanece en 12. No se regeneraron paquetes ZIP.

## Validación y límites de la evidencia

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause`: Core 390/390 y ForgeWeave 73/73 pasaron; las compilaciones `net472`, `net8.0` y sus dependencias de prueba informaron cero advertencias y cero errores. El BAT anidado `Run-DetourFixture.bat` completó sus comprobaciones seriales x64 de ciclo de vida, gate de callbacks, Prefix void/no-void, orden de reaplicación, incertidumbre y restauración del parche raw. El ejecutable del fixture no se inició directamente.
- `tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause`: Desktop 65/65 y render/recursos WPF 292/292 pasaron; el harness informó 182 pases de render/layout.
- No se usó una sesión activa de Bannerlord ni del Modding Kit. El éxito del fixture serial no demuestra seguridad de Apply/Revert mientras otro hilo ejecuta el destino; el backend sigue siendo experimental.

Este anexo amplía el registro protegido de mejoras sin reemplazar las revisiones anteriores.
