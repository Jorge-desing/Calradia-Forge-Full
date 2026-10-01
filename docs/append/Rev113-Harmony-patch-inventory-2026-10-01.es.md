# Rev113 — Inventario determinista de parches Harmony

**Fecha:** 1 de octubre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios; ForgeApi.Version 13  
**Alcance:** Diagnóstico de compatibilidad Harmony de solo lectura en Core, fixture de regresión y validación de distribución.

## Problema observado y justificación técnica

El gate canónico de distribución detectó una aserción fallida en el inventario de parches Harmony de solo lectura. El diagnóstico conservaba el orden arbitrario de enumeración de propietarios del runtime, aunque la regresión esperaba un snapshot estable. La inspección también mostró que la regla para construir nombres de propiedades por tipo de patch generaba nombres inválidos para `Transpiler` y `Finalizer`, por lo que esos tipos podían omitirse del inventario de un runtime Harmony compatible.

## Solución técnica y decisiones arquitectónicas

Los nombres de propietarios ahora se deduplican sin distinguir mayúsculas y se ordenan ordinalmente antes de devolver el snapshot. Los metadatos por tipo usan nombres explícitos para `Prefixes`, `Postfixes`, `Transpilers` y `Finalizers`. El diagnóstico sigue siendo opcional, acotado, de solo lectura y basado en reflexión; no inspecciona detours crudos ni certifica que no exista otro backend de parcheo.

El fixture de regresión ahora proporciona los cuatro tipos y devuelve intencionalmente los propietarios en orden léxico inverso. Comprueba la salida determinista de propietarios, el orden de tipos de patch y el nombre de cada método patch.

## Alcance de fuentes y dependencias

Los cambios se limitan a `HarmonyDiagnostics` y su fixture de regresión de Core. No se añade API, dependencia de runtime ni ruta de mutación; tampoco cambia la versión del SDK o del producto. Rev001–Rev112 permanecen intactas.

## Validación y límites de la evidencia

- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` pasó: Core 405/405 y ForgeWeave 73/73; las compilaciones Release seleccionadas tuvieron 0 advertencias y 0 errores. El fixture aislado de detours x64 también pasó mediante su launcher BAT.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\package.ps1` terminó correctamente después de la corrección. El runner del paquete pasó sus suites seleccionadas, incluidas Desktop 65/65 y WPF con 294 casos de render; el arnés reportó 13.978 ms y 182 pasadas de layout. Ese tiempo corresponde al arnés, no a la latencia interactiva de la aplicación.
- La compilación DocFX terminó con 0 advertencias y 0 errores. La auditoría aprobó los tres archivos y la comparación independiente de SHA-256 coincidió con el manifiesto generado.
- TpacTool no estaba disponible para el análisis profundo y se omitió; el pipeline realizó su comprobación estructural acotada de TPAC. No se afirma una importación en Resource Browser ni render en Bannerlord. No se abrió campaña ni batalla.

Esta revisión añade evidencia a Rev112 sin reemplazar párrafos históricos ni cambiar la versión del producto.
