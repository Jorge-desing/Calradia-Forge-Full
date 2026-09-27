# Rev012 — comando oficial del Resource Browser y estabilidad de entradas

## Verificación de afirmaciones

La guía oficial de TaleWorlds confirma dos datos concretos: `SpriteSheetGenerator.exe` crea `Assets`, `AssetSources` y `SpriteData.xml`; y desde el menú principal se abre la consola con `Alt + backtick`, se escribe `resource.show_resource_browser` y se pulsa Enter para enviar ese comando. El Enter documentado pertenece al ingreso del comando de consola. La guía no afirma que el generador espere una nueva línea por `stdin`, mantenga los PNG bloqueados hasta recibirla o necesite `RedirectStandardInput`.

La documentación de carpetas asigna funciones distintas a `Assets` (TPAC editables), `AssetSources` (archivos de origen) y `AssetPackages` (TPAC de solo lectura para cliente). También indica que las carpetas de autoría pueden filtrarse antes de publicar y que los módulos editables pueden distribuir `Assets` y `AssetSources`. No documenta que su coexistencia —vacía o no— anule `AssetPackages`, ni demuestra que sea la causa de un fallo de War Sails. Se conserva la recomendación de no borrar las carpetas del árbol de autoría; cualquier preparación de distribución debe usar el flujo oficial o una copia de staging.

El texto pegado repite las afirmaciones sobre stdin, prioridad de directorios vacíos y cierre UIA de modales. También propone automatizar la consola con `SetForegroundWindow`, `SendKeys` y pausas fijas. Solo el comando y los pasos manuales para abrir Resource Browser están en la guía oficial; no se documentan dichas pausas, la secuencia automatizada de teclas ni que cerrar errores UIA y continuar sea una operación segura. Ese código no se integra. El helper se limita al proceso explícitamente configurado y deja errores o diálogos inesperados abiertos para inspección.

## Cambio del helper

Cada `AssetFile` conserva tamaño y fecha UTC observados al escanear. Justo antes de cualquier acción UI para ese archivo, `AssetFileLease` vuelve a comprobar que siga siendo un archivo regular, con la ruta, tamaño y fecha esperados. Mantiene un `FileStream` de solo lectura compartible únicamente con lectores hasta que termina el proceso auxiliar, de modo que las escrituras, renombrados o sustituciones normales de Windows no puedan alterar entradas ya entregadas durante el lote. Si la verificación falla, el lote se detiene sin interacción para ese archivo. Esta protección no afirma resistencia frente a un actor con capacidad de cambiar la ruta antes de adquirir el handle y no sustituye una prueba de importación real.

La comprobación que permite concluir `SUBMITTED` ahora envía `WM_NULL`, sin mutar la interfaz, y espera como máximo un segundo la respuesta de la ventana principal. Una ventana que no responde deja el resultado como `STOPPED`.

## Pruebas y límites

`Test-BannerlordFbxImporter.bat` compiló el helper y el arnés con cero advertencias y cero errores. Pasaron 59 aserciones breves, incluidas detección de cambios entre escaneo y envío y rechazo de escrituras mientras la entrada está bajo lease. No se abrió el Editor ni se ejecutó una importación real; no se observó ni validó un TPAC. La versión continúa en 22.0.0 y los ZIP no se regeneraron.

## Fuentes

[Generación de hojas de sprites e invocación de Resource Browser — TaleWorlds](https://moddocs.bannerlord.com/asset-management/generating_and_loading_ui_sprite_sheets/) · [Roles de Assets, AssetSources y AssetPackages — TaleWorlds](https://moddocs.bannerlord.com/asset-management/asset-types/overriding_assets/)
