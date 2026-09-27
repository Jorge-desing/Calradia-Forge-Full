# Rev011 — endurecimiento del importador FBX experimental

## Decisión y alcance

Se mantuvo `BannerlordFbxImporter` como utilidad experimental independiente de Calradia Forge, en versión de producto 22.0.0. No se modificó el helper canónico FlaUI `BannerlordImportAutomation` ni se regeneraron los ZIP de distribución.

El PDF recibido se evaluó como material de referencia. Se cotejaron sus afirmaciones con documentación oficial de TaleWorlds, documentación del proyecto UIExtenderEx, documentación de Microsoft y una guía comunitaria de exportación FBX. Se conservaron como guía práctica las recomendaciones con respaldo parcial y se descartaron como contratos del motor las conclusiones causales no demostradas.

## Evidencia y guía de recursos

- La documentación de TaleWorlds confirma las funciones diferenciadas de `Assets`, `AssetSources` y `AssetPackages`, y permite omitir carpetas de autoría al publicar. No confirma que su coexistencia haga que el motor ignore paquetes ni que una carpeta vacía cause el fallo de inicio atribuido a War Sails. La herramienta no borra esas carpetas; cualquier filtrado se limita a una copia temporal de publicación.
- La guía oficial de sprites confirma que `SpriteSheetGenerator.exe` crea `Assets`, `AssetSources` y `SpriteData.xml`, y que después se usa Resource Browser para escanear e importar. No especifica un protocolo de Enter por stdin ni un bloqueo de PNG que el helper pueda resolver. Por tanto, el helper FBX no inicia ni controla el generador de sprites.
- La guía comunitaria recomienda desactivar Blender `Add Leaf Bones` y muestra un síntoma en Model Viewer. La explicación interna del motor expuesta por el PDF no está respaldada por documentación oficial; la recomendación queda como comprobación manual, sin un bloqueo automatizado.
- TaleWorlds documenta que `_notused` hace que el motor ignore el esqueleto cuando se importan activos relacionados sin importar ese esqueleto. No documenta la transferencia de pesos al esqueleto maestro que afirma el PDF.
- TaleWorlds indica que los materiales referenciados en FBX no se crean durante la importación y que deben existir materiales con el mismo nombre. No se encontró respaldo para una ventana por LOD ni para la fórmula de 18 advertencias.
- Microsoft documenta eventos UI Automation y patrones de ventana. Eso no prueba que las ventanas de error de Resource Browser puedan cerrarse de forma segura ni que el lote pueda continuar después. La decisión acordada se conserva: detener el lote y dejar la ventana abierta.
- La documentación propia de UIExtenderEx describe AutoGens y su desactivación global para permitir parches de XML. Es una capacidad de modificación de interfaz Gauntlet y no una API de importación de activos; no se incorpora al helper.

## Cambios del helper y verificación

- El helper ya no puede reportar `SUBMITTED` cuando UI Automation falla al enumerar ventanas, cuando el diálogo configurado sigue existiendo, cuando el proceso presenta otra ventana visible, cuando la ventana principal no responde o cuando se detecta una ventana anidada.
- El recorrido recursivo ahora limita también las entradas totales del sistema de archivos a 100.000 y restringe las advertencias individuales de puntos de reanálisis, además de los límites previos de 100 FBX, 10.000 carpetas, profundidad 32, 256 MiB por archivo y 2 GiB por lote.
- Se reemplazó el README que describía IDs inventados, cierre automático de diálogos, reintentos y verificación TPAC. Se añadieron guías inglesa y española que describen los modos reales, el proceso acotado y el resultado limitado.
- `Test-BannerlordFbxImporter.bat` compiló Helper y pruebas con 0 advertencias y 0 errores y pasó 57 aserciones breves, incluyendo enumeración incompleta, diálogos persistentes, modales visibles/anidados, respuesta de la ventana principal y límite de entradas.
- No se inició el Editor, no se ejecutó una importación real y no se observó un TPAC resultante. Los selectores UI siguen sin valores confirmados. No se reconstruyeron los paquetes 22.0.0.

## Referencias

[Roles de carpetas y publicación de TaleWorlds](https://moddocs.bannerlord.com/asset-management/asset-types/overriding_assets/) · [Generación e importación de sprites](https://moddocs.bannerlord.com/asset-management/generating_and_loading_ui_sprite_sheets/) · [Convenciones de LOD, materiales y esqueletos](https://moddocs.bannerlord.com/asset-management/asset-types/asset_naming_conventions/) · [Exportación FBX comunitaria](https://docs.bannerlordmodding.lt/3d/export_to_fbx/) · [Documentación UIExtenderEx](https://uiextenderex.butr.link/) · [UI Automation WindowOpenedEvent](https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.windowpattern.windowopenedevent?view=windowsdesktop-10.0)
