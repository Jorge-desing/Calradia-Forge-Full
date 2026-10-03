# Rev116 — Identidad del diagnóstico de orden de hooks y regresión del inventario

**Fecha:** 2 de octubre de 2026
**Versión:** Calradia Forge 25.2.0, sin cambios
**Alcance:** Core, Mod, pruebas, documentación y validación de distribución. No cambian el comportamiento de hooks ni la API pública.

## Problema observado y justificación técnica

El informe de solo lectura `cf.hook_order` comparaba una cadena descriptiva del método de destino. Esa cadena omitía la identidad del ensamblado declarante y los argumentos genéricos cerrados, por lo que un método genérico y otro no genérico con el mismo nombre y parámetros podían aparecer como el mismo destino. Una prueba relacionada buscaba IDs con subcadenas; al agregar referencias `Before` y `After`, esos IDs también aparecían en el texto de metadatos y se confundían con registros devueltos por el inventario.

## Solución técnica y decisiones arquitectónicas

La identidad de cada destino de hook ahora incluye la identidad del ensamblado del tipo declarante, los argumentos genéricos cerrados del método y los tipos de parámetros con identidad de ensamblado. El informe de orden sigue comparando declaraciones contra el inventario actual y no afirma calcular el orden efectivo de dispatch de MonoMod. La regresión de consola comprueba prefijos de líneas completas de registros y señala qué aserción de filtro falló, en vez de ocultarlo en una condición compuesta. El registro, el inventario y el diagnóstico de orden no aplican hooks.

## Cambios en activos, código y dependencias

- Se actualizó `ForgeHookService.Identity` para distinguir métodos genéricos y no genéricos, así como destinos homónimos de ensamblados distintos.
- Se agregó una regresión con un par genérico/no genérico de igual nombre y una referencia `Before` a otro destino.
- Se corrigieron las pruebas del inventario para separar líneas de registros de IDs citados en metadatos `Before`/`After`.
- Se actualizaron las guías Patch Blueprint y los changelogs en inglés y español. Se conservan versión de producto, API SDK 13, MonoMod.RuntimeDetour 25.3.6, contratos públicos y comportamiento del juego.

## Validación y límites de la evidencia

- `dotnet build CalradiaForge.sln -c Release -v:minimal`: aprobado con cero advertencias y errores.
- `tools/Run-CalradiaForge-Core-Tests.bat --no-pause`: Core pasó 413/413; el fixture serial x64 de hooks pasó mediante `Run-DetourFixture.bat`.
- `tools/Run-CalradiaForge-Desktop-Tests.bat --no-pause`: 65/65 aprobadas.
- `tools/Run-CalradiaForge-ForgeWeave-Tests.bat --no-pause`: 73/73 aprobadas.
- `tools/Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause`: 295 casos de render aprobados; el tiempo corresponde al arnés, no a la latencia de la aplicación.
- `tools/Verify-CalradiaForge-StatelessBehavior.bat`: 4/4 aprobadas.
- `tools/Run-CalradiaForge-Python-Checks.bat --ci --no-pause`: aprobó comprobaciones de recursos, estructura Gauntlet y assets.
- `tools/package.ps1 -SkipTests`: generó y auditó los tres paquetes; la comparación SHA-256 independiente coincidió con el manifiesto generado en ese empaquetado.
- No se aplicaron hooks en vivo en Bannerlord ni en Modding Kit. Los resultados seriales del fixture no establecen que el parcheo sea seguro mientras otro hilo ejecuta el destino.

Este anexo agrega evidencia sin reemplazar revisiones anteriores. Conserva los comandos, las rutas, los contratos API y la versión del producto.
