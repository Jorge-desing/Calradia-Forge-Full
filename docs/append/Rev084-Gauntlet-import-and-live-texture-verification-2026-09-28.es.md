# Rev084 — Corrección de importación y verificación en vivo de texturas Gauntlet

**Fecha:** 28 de septiembre de 2026
**Versión:** Calradia Forge 25.2.0, sin cambios
**Alcance:** Importación del atlas en Resource Browser, recopilación del TPAC fuente y una observación de Gauntlet dentro del juego.

## Problema observado y justificación técnica

Rev083 atribuyó incorrectamente la importación del atlas a la acción **Save** del inspector de texturas. Esa acción solo guarda los ajustes de importación. El hash instalado registrado después de esa acción no demostraba que se hubiera importado el atlas actual, y todavía no se había inspeccionado el panel dentro del juego. Rev083 también indicó que Computer Use se había detenido con Escape; el usuario aclaró que lo cerró para reducir el consumo de recursos.

## Solución técnica y decisiones arquitectónicas

La importación real se completó mediante el flujo de selección de archivos de Resource Browser. El atlas seleccionado produjo un diálogo que nombraba el archivo existente `ui_calradiaforge_1.png` y preguntaba si debía reemplazarse y actualizar los recursos correspondientes. Se eligió **Update**. Después se cerró la ventana temporal `Importing file` y Resource Browser volvió a mostrar `ui_calradiaforge_1` en `Modules > CalradiaForge > Assets > GauntletUI` como textura. El inspector informó datos de origen 4096×512 `B8G8R8A8` y una textura de ejecución 4096×512 DXT5 con 13 niveles mip.

Después de esta verificación en Resource Browser, el TPAC instalado se copió a un archivo temporal; se comprobó el SHA-256 de esa copia antes de reemplazar el TPAC fuente del espacio de trabajo. El resultado se volvió a comprobar. Los archivos previos instalados y fuente permanecen en el directorio de respaldo Rev082. No se usó TpacTool.

## Cambios en activos, código y dependencias

El TPAC instalado después de la importación tiene 539 bytes y SHA-256 `1506C5EAECD4BFC752678C6E6CDB3FA87C4A7A51D8B3B7846B15715276D571EF`. La copia temporal y el TPAC fuente final del espacio de trabajo tienen el mismo hash y tamaño. El respaldo instalado anterior a la importación conserva `69513B5617F026E1F106F6CA6D47CF5C5CE0B5869FFA472E9166B037CA8EDEA6`; el respaldo del TPAC fuente anterior conserva `8619B64C386D61364A0599574C70C7B0F0F4149CF406EADE77BEE726D726D5E5`.

El producto permanece en 25.2.0. No cambian código, API, rutas, comandos, permisos, dependencias ni ZIPs.

## Validación y límites de la evidencia

- `tools/Deploy-CalradiaForge-ToGame.bat --no-pause` compiló y desplegó los perfiles Client y Modding Kit. Ambas compilaciones informaron 0 advertencias y 0 errores. El registro de despliegue guardó y preservó el TPAC de ejecución importado con SHA-256 `1506C5EAECD4BFC752678C6E6CDB3FA87C4A7A51D8B3B7846B15715276D571EF`.
- La configuración del launcher de Steam tenía `CalradiaForge` seleccionado. Bannerlord 1.4.8 llegó al menú principal. Una apertura con F10 mostró el panel Calradia Forge 25.2.0; los ornamentos de cabecera y rail se renderizaron desde la textura importada. No se observó una aserción ni un marcador de textura ausente. El panel y el juego se cerraron normalmente. No se inició campaña, batalla ni ejecución de pruebas.
- La acción Save anterior, por sí sola, no importó el atlas; sí lo hizo la operación **Update** del flujo de selección de archivos. El hash `131E623077708032C76790324F31EDC7862BDC6D5ACA79350B4A03C241A0529E` registrado en Rev083 fue una observación intermedia, no evidencia de una importación del atlas completada. El hash de la importación completada es el indicado arriba.
- El usuario cerró la sesión de Computer Use anterior para reducir el consumo de recursos; no se detuvo con Escape.
- No se usó TpacTool. Esta comprobación en vivo confirma únicamente el render observado del panel en el menú principal; no valida campañas, batallas ni todos los estados Gauntlet.

Este anexo corrige la interpretación de evidencia de Rev083 sin editar ni reemplazar ese anexo histórico. Agrega evidencia y conserva la versión 25.2.0 y los contratos públicos existentes.
