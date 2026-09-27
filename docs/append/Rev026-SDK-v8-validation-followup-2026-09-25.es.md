# Rev026 — seguimiento de validación del SDK 8

**Fecha:** 25 de septiembre de 2026  
**Versión:** Calradia Forge 24.0.0, sin cambios  
**Alcance:** SDK, pruebas Core, pruebas ForgeWeave, auditoría de bindings Gauntlet y evidencia de validación.

## Problema observado y justificación técnica

Después de registrar en Rev025 el código fuente del SDK 8 y su documentación, el launcher de pruebas Core detectó un fallo en la auditoría de bindings del prefab Gauntlet. La auditoría atribuía todos los bindings `Command.Click` y `@property` al `PanelViewModel`, aunque Gauntlet resuelve los bindings dentro del `ItemTemplate` de un `ListPanel` contra el ViewModel de cada elemento de la colección. En particular, `ExecuteLoad` pertenece a `CommandHistoryItemVM`.

## Solución técnica y decisiones arquitectónicas

El auditor de pruebas ahora resuelve los elementos dentro de un `ItemTemplate` al tipo de elemento genérico de la propiedad `ListPanel.DataSource` más cercana en `PanelViewModel`. Comprueba las propiedades y comandos de la plantilla contra ese dueño, y los controles fuera de plantillas contra el ViewModel del panel. Así refleja el contexto real de binding sin cambiar el código de ejecución ni las API públicas.

## Cambios en activos, código y dependencias

- Corrige `tests/CalradiaForge.Tests/NativeEvidencePanelTests.cs` para que los bindings de plantillas se validen contra sus ViewModels de elemento.
- Corrige el codemap actual del SDK de `ForgeApi.Version = 7` a la versión 8.
- No altera activos de módulos, dependencias de paquetes, ZIP ni comportamiento del producto.

## Validación y límites de la evidencia

- `cmd /c tools\\Run-CalradiaForge-Tests.bat --core-only --no-pause` terminó correctamente.
- Las compilaciones seleccionadas `net472` y `net8.0` informaron cero advertencias y cero errores.
- Core pasó **275/275** pruebas; ForgeWeave pasó **52/52** pruebas.
- No se inició Bannerlord. Este seguimiento no afirma comportamiento dentro del juego, validación de paquetes ni contenidos de ZIP.

Este anexo amplía la revisión anterior sin reemplazar sus párrafos. Mantiene los comandos, las rutas, la versión del producto y los contratos SDK existentes.
