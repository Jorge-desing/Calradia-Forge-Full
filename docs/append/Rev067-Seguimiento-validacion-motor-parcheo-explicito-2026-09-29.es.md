# Rev067 — Seguimiento de validación del motor de parcheo explícito

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** SDK, Core, Mod y herramientas de prueba. Regresiones para aplicar detours explícitos, reversión de bytes exactos y ciclo de vida del host/servicio.

## Problema observado y justificación técnica

La implementación del motor de parcheo de Rev066 aún no se había ejercitado con su fixture nativo desechable ni con la suite BAT integrada. Además, la revisión del ciclo de vida identificó dos casos que necesitaban regresión directa: conservar el host anterior cuando una reconexión se bloquea por bytes ajenos en el destino, y distinguir el comando reservado `all` tanto de un propietario como de un ID de parche.

## Solución técnica y decisiones arquitectónicas

- El launcher del fixture de detour x64 compila en una carpeta temporal única. Rechaza una carpeta ya existente y elimina solo los archivos de la carpeta que creó, evitando la visibilidad tardía del ejecutable local del proyecto que causó el fallo anterior del launcher.
- La cobertura confirma que un host en conflicto permanece publicado para resolución manual, que su servicio desconectado rechaza aplicaciones nuevas, que restaurar los bytes instalados registrados permite revertir explícitamente el handle y que después se puede conectar un host nuevo.
- La cobertura de consola rechaza `all` si coincide con un propietario, un ID o ambos. También siguen cubiertos la reversión de aplicación por lotes, el estado de escritura incierto, el registro compartido de servicio/detour directo y la resolución del callback en preflight.

## Cambios en activos, código y dependencias

Los cambios fuente se limitan al launcher del fixture y las regresiones. No cambia ninguna dependencia de ejecución, recurso del módulo, archivo de distribución, comportamiento de ForgeWeave ni versión del producto. `ForgeApi.Version` permanece en 11.

## Validación y límites de la evidencia

- `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat --no-pause` terminó correctamente: compilación x64 `net472` limpia y resultado de destino en serie `15 → 32 → 15` con reversión exacta.
- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` terminó con compilaciones `net472` y `net8.0` limpias, cero advertencias, cero errores, Core 361/361, el fixture aislado de detour nativo aprobado y ForgeWeave 73/73.
- No se inició Bannerlord ni el Modding Kit. El fixture serializa las llamadas al destino alrededor de las escrituras del parche; no demuestra seguridad frente a ejecución concurrente. El backend sigue siendo experimental y no coordina otros hilos ni reubica instrucciones sobrescritas.

Este anexo agrega evidencia después de Rev066 sin cambiar párrafos anteriores. La versión sigue en 25.2.0 y los ZIP permanecen sin cambios.
