# Rev117 — Identidad exacta de firma de destinos hook para diagnósticos de orden

**Fecha:** 2 de octubre de 2026
**Versión:** Calradia Forge 25.2.0, sin cambios
**Alcance:** Snapshots de hooks en Core, diagnósticos de consola de solo lectura, pruebas y documentación bilingüe del SDK. No cambia la aplicación de hooks ni las firmas de API públicas.

## Problema observado y justificación técnica

El diagnóstico de solo lectura `cf.hook_order` compara los destinos registrados mediante `ForgeHookSnapshot.TargetMethod`. La identidad anterior podía confundir métodos IL con el mismo tipo declarante, nombre y parámetros pero distinto tipo de retorno o despacho estático/de instancia. Los modificadores personalizados requeridos y opcionales también forman parte de una firma de método CLR y deben representarse al distinguir destinos. Una clasificación falsa como mismo destino podría presentar una referencia de orden declarada como arista Forge conocida cuando apunta a otro método.

## Solución técnica y decisiones arquitectónicas

La identidad del método ahora incluye la identidad AssemblyQualified del tipo declarante, los argumentos genéricos cerrados, tipos de parámetros y retorno, `IsStatic`, `CallingConvention` y los modificadores personalizados requeridos y opcionales de cada parámetro y del retorno. El diagnóstico de orden de solo lectura sigue clasificando únicamente registros Forge actuales; no infiere el orden efectivo de MonoMod. La cadena pública `ForgeHookSnapshot.TargetMethod` es información diagnóstica cuyo formato puede evolucionar; los consumidores deben tratarla como opaca y no analizarla.

Las regresiones Reflection.Emit construyen métodos distintos que solo varían en el tipo de retorno, la convención estático/instancia o un modificador requerido del retorno. Comprueban que los snapshots sean distintos y que `cf.hook_order` no clasifique una referencia a otro destino como el mismo método. El registro y el inventario siguen siendo inertes.

## Cambios en activos, código y dependencias

- Se amplió `ForgeHookService.Identity` y se añadieron helpers para identidad de parámetros y modificadores personalizados.
- Se añadieron pruebas aisladas con firmas generadas en tiempo de ejecución a `SdkFeaturesTests`.
- Se actualizaron las guías Patch Blueprint y referencia del SDK, además de los changelogs en inglés y español.
- La versión del producto permanece en 25.2.0, la API SDK en 13 y MonoMod.RuntimeDetour en 25.3.6. No se agregó ninguna dependencia externa ni firma de contrato pública.

## Validación y límites de la evidencia

- `tools/Run-CalradiaForge-Core-Tests.bat --no-pause`: Core pasó 413/413; el fixture serial x64 de detours pasó mediante su BAT. El fixture de compatibilidad binaria v12 también compiló sin advertencias ni errores.
- `tools/Run-CalradiaForge-Desktop-Tests.bat --no-pause`: pasaron 65/65.
- `tools/Run-CalradiaForge-ForgeWeave-Tests.bat --no-pause`: pasaron 73/73.
- `tools/Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause`: pasaron 295/295 casos con 308 pases de layout. El tiempo reportado corresponde al arnés, no a la latencia de la aplicación.
- `tools/Verify-CalradiaForge-StatelessBehavior.bat`: pasaron 4/4.
- `tools/Run-CalradiaForge-Python-Checks.bat --ci --no-pause`: pasó.
- La compilación local Release de la solución terminó con cero advertencias y errores mediante el gate BAT de stateless.
- No se aplicaron hooks en vivo en Bannerlord ni Modding Kit. Estas comprobaciones de identidad son diagnósticos de metadatos; no prueban el orden efectivo de despacho ni la seguridad concurrente de detours.

Este anexo amplía el registro anterior sin sustituir párrafos previos. Conserva versión, comportamiento de hooks, firmas de API y declaraciones de orden existentes.