# Rev074 — Salvaguardas de ciclo de vida de hooks y contexto del anfitrión

**Fecha:** 30 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** SDK, Core, Mod, Desktop, protocolo IPC y documentación. Endurece el ciclo de vida del servicio Prefix/Postfix y hace explícita la cancelación de planes del operador.

## Problema observado y justificación técnica

El servicio de hooks podía desconectarse mientras conservaba hooks activos o inciertos fuera del contexto aprobado de juego, lo que podía separar el estado del SDK de los detours que seguían instalados. La comprobación de Verify tampoco convertía una excepción al leer el estado de RuntimeDetour en un conflicto visible. Además, el workbench descartaba su plan local al actualizar snapshots sin invalidar primero el token pendiente del anfitrión.

La puerta del menú también debe identificar la pantalla oficial del juego por identidad de tipo, no por un nombre o prefijo que una extensión pueda imitar.

## Solución técnica y decisiones arquitectónicas

Se añadió la capacidad opcional `IForgeHookServiceDisconnectGuard`. `ForgeApi` la consulta antes de reemplazar o retirar el servicio publicado. El servicio rechaza la transición si conserva hooks aplicados, en conflicto o fallidos y no se encuentra en el contexto aprobado. `Verify` registra las excepciones de estado como `Conflict`; una retirada externa verificada dispone el handle RuntimeDetour antes de liberar la reserva del destino.

El gate resuelve `TaleWorlds.MountAndBlade.GauntletUI.GauntletInitialScreen` desde `TaleWorlds.MountAndBlade.GauntletUI` y compara el objeto `Type` exacto. Una resolución fallida cierra el gate. El comportamiento continúa restringido al hilo del juego, al menú principal y a la ausencia de campaña, misión y multijugador.

Se agregó `hook-plan-cancel`, que transporta solo la sesión y el token exactos del anfitrión. WPF conserva la vista previa hasta recibir una respuesta válida que confirme la cancelación; una respuesta faltante, inválida, de otra sesión o negativa conserva el plan y detiene la actualización de snapshots. No hay reintentos automáticos.

## Cambios en activos, código y dependencias

- Se añadió la interfaz opcional de preflight de desconexión y se integró a las transiciones de `ForgeApi` sin agregar miembros a `IForgeRegistry`.
- Se corrigió la compilación compartida de `ForgeHookService` para `net472` y `net8.0`; el backend ejecutable sigue limitado a Bannerlord `net472`.
- Se incorporaron request/result DTOs y el protocolo `hook-plan-cancel` con solo sesión/token, además del estado de cancelación asíncrona y validación de sesión en el Hook Workbench.
- Se actualizó `docs/SDK.md` y se creó su contraparte `docs/SDK.es.md`; la referencia SDK en inglés y español conserva el contrato 12.
- No cambian la API de los destinos registrados, el protocolo de confirmación de hooks, la versión de producto 25.2.0 ni los ZIP.

## Validación y límites de la evidencia

- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"`: Core 384/384 y ForgeWeave 73/73; builds `net472` y `net8.0` sin advertencias ni errores.
- `cmd.exe /c "tests\CalradiaForge.DetourFixture\Run-DetourFixture.bat --no-pause <nul"`: fixture serial x64 pasó el rechazo de desconexión insegura, retirada externa, excepciones de Verify, exclusión entre backends y retorno `15 → 32 → 15`. El `.exe` del fixture no se ejecutó directamente.
- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause <nul"`: Desktop 65/65; render WPF 292/292 en 13.895 ms de harness.
- No se abrió Bannerlord ni el Modding Kit. Las pruebas seriales no demuestran seguridad cuando otro hilo ejecuta el destino durante Apply o Revert; el backend sigue siendo experimental.

Este anexo agrega evidencia sin reescribir las revisiones anteriores.
