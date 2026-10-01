# Rev097 — Recuperación de desconexión y autorretiro de callbacks de hooks

**Fecha:** 30 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Core, integración del host Mod/SDK, fixture desechable de detours y documentación bilingüe de hooks.

## Problema observado y justificación técnica

El `TestEngine` publicado exponía `IForgeHookService`, pero no reenviaba su guard opcional de desconexión. Por ello, `ForgeApi.Disconnect()` podía iniciar el teardown del host antes de que el servicio interno rechazara retirar un hook activo. Además, `Runtime.Dispose()` cerraba el servidor del pipe con nombre antes de solicitar la desconexión al SDK, eliminando la ruta de recuperación del Workbench. Una revisión separada del fixture detectó que un callback Prefix/Postfix podía revertir o liberar su propio hook mientras la invocación actual todavía necesitaba el trampoline original de MonoMod; disponer el objeto `Hook` de inmediato invalidaba esa llamada en curso.

## Solución técnica y decisiones arquitectónicas

`TestEngine` ahora reenvía `IForgeHookServiceDisconnectGuard` y conserva un constructor público explícito sin parámetros junto al constructor que recibe el gate. Una llamada directa a `ForgeApi.Disconnect()` rechazada mientras `Runtime` aún recibe ticks deja la API publicada y el pipe disponible para recuperación; el registro persiste en `finally`. Durante `Runtime.Dispose()` / `OnSubModuleUnloaded` real, el servidor del pipe se cierra en `finally`, por lo que no hay recuperación por pipe después de la descarga. Esa ruta de rechazo durante la descarga sigue sin verificarse en un host en vivo.

Cada hook aplicado usa una activación de dispatch independiente que posee el objeto hook, el invocador tipado del método original y la ruta de dispatch. El dispatch entra en esa activación antes de invocar callbacks y sale mediante `finally`. Si se solicita una reversión durante un dispatch activo, se quita la ruta para llamadas futuras y se devuelve un estado pendiente sin intentar ni declarar `Undo` como verificado. Cuando terminan los dispatches, el servicio vuelve a comprobar el gate del host y después ejecuta y verifica `Undo`, dispone el hook y libera la reserva del destino. Si el gate se cerró, la limpieza queda pendiente y retenida para un reintento explícito. Un fallo de la liberación diferida permanece retenido y visible como conflicto; una activación de reemplazo no puede sobrescribirla. Los fixtures de autorretiro Prefix y Postfix verifican que la llamada en curso termine, que el callback no vuelva a ejecutarse en llamadas posteriores y que las llamadas futuras al destino usen el método original.

El gate de administración sigue limitado a la pantalla exacta del menú principal de Bannerlord en el hilo del juego, sin campaña, misión ni sesión multijugador. La sincronización de MonoMod RuntimeDetour se aplica a su propio comportamiento de cadena; este fixture serial no certifica todo el ciclo de vida del host ni todas las formas posibles de concurrencia. El backend directo `ForgeDetour` es distinto. La ACL del pipe con nombre se limita al SID de Windows actual, no a la identidad del proceso WPF; la casilla y el token de un solo uso son confirmación de interfaz, no autentican a otro proceso del mismo usuario.

## Cambios en activos, código y dependencias

Los cambios se limitan a `TestEngine`, el orden de teardown del runtime, el ciclo de vida de activación del dispatch de hooks, el fixture serial `net472` x64 y la documentación en inglés/español. No se añadieron dependencias, contratos SDK, acciones IPC, recursos del juego ni paquetes. Se conservan el constructor público sin parámetros de `TestEngine`, la versión del producto 25.2.0 y `ForgeApi.Version` 12.

## Validación y límites de la evidencia

- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` compiló los grafos de dependencias Core net472 y net8.0 con cero advertencias y errores; Core pasó 395/395 y ForgeWeave 73/73.
- El launcher compiló y ejecutó el fixture serial x64 mediante su `.bat` y host PowerShell temporal, incluidos el rechazo/recuperación de desconexión de API y el autorretiro Prefix/Postfix. No se inició directamente ningún ejecutable de pruebas.
- No se probó `Runtime.Dispose()`/descarga real del módulo con el pipe vivo; tampoco se inició Bannerlord o Modding Kit, campaña, batalla ni se generaron ZIP. El fixture no establece seguridad general ante llamadas concurrentes al destino ni certifica el ciclo de vida del host.

Este anexo amplía Rev096 sin reemplazar sus párrafos. Conserva los comandos, las rutas, la versión de API y la versión del producto.
