# Rev076 — Recuperación del ciclo de hooks y parches

**Fecha:** 30 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Ciclo de conexión del SDK, servicios de hooks y parches de Core, fixture serial de detours y documentación.

## Problema observado y justificación técnica

La puerta de contexto se comprobaba una sola vez al entrar a la distribución del hook, por lo que una transición síncrona dentro de Prefix o del destino podía dejar que etapas posteriores ejecutaran callbacks fuera del contexto aprobado del menú principal. `Dispose()` también descartaba resultados de reversión fallida. El teardown del SDK podía despublicar el servicio de parches con conflictos pendientes, y una reconexión entrante fallida podía dejar publicado pero cerrado al host anterior.

## Solución técnica y decisiones arquitectónicas

La distribución vuelve a comprobar la puerta del host después de Prefix y antes de Postfix. Si se cierra después de Prefix, Forge invoca el destino original con los argumentos originales; si se cierra mientras se ejecuta el destino, devuelve su resultado sin ejecutar Postfix. Estas comprobaciones no suspenden hilos ni vuelven seguras las escrituras concurrentes.

`Dispose()` de los handles de hooks y parches ahora lanza una excepción cuando no puede confirmar la reversión; quien necesite diagnósticos estructurados puede llamar directamente a `Revert()`. Un conflicto de parche solo se puede recuperar después de que el llamador restaure los bytes a la imagen exacta instalada por Forge o a la imagen original. `ForgeApi.Disconnect()` conserva publicado `Patches` mientras haya registros sin resolver. Si falla la reconexión del ciclo de vida del host entrante después de una limpieza correcta del host anterior, Forge intenta reabrir el ciclo anterior y las aplicaciones directas; una restauración fallida se informa mediante una excepción agregada.

## Cambios en activos, código y dependencias

- Se añadieron comprobaciones fail-closed entre Prefix, el destino original y Postfix.
- `Dispose()` de los handles de hooks/parches ahora informa las reversiones que no pudo confirmar.
- Se conserva la capacidad opcional de parches durante desconexiones con conflictos y se permite recuperar `Conflict` solo con bytes exactos, sin sobrescribir bytes ajenos.
- Se añadió rollback si falla la reconexión del ciclo de vida del host entrante y regresiones para estos casos.
- Se actualizaron la guía Patch Blueprint en inglés y español, las skills de ciclo de vida, los changelogs y este registro append-only. La versión del producto permanece en 25.2.0 y `ForgeApi.Version` en 12. No se reconstruyeron ZIP.

## Validación y límites de la evidencia

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause`: Core 393/393 y ForgeWeave 73/73 pasaron; la compilación informó cero advertencias y cero errores. El `Run-DetourFixture.bat` anidado pasó las comprobaciones seriales x64 de Prefix/Postfix, cambio de contexto, fallo de disposición visible, restauración y ciclo de vida. No se inició directamente el ejecutable de la fixture.
- `tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause`: Desktop 65/65 y render/recursos WPF 292/292 pasaron con 182 pases de layout/render.
- `git diff --check` pasó. No se usó una sesión real de Bannerlord ni del Modding Kit. Las fixtures seriales no demuestran seguridad cuando otro hilo ejecuta el destino; el backend sigue siendo experimental.

Este anexo amplía el registro protegido de mejoras sin reemplazar las revisiones anteriores.
