# Calibración observacional del perfil texture-only

El 23 de septiembre de 2026 se inspeccionó de forma manual y de solo lectura la ventana `Resource Browser` del Modding Kit. La calibración queda incompleta: las observaciones ayudan a describir un atlas existente, pero no confirman el filtro de extensiones para una importación nueva ni habilitan `--submit`.

## Ventana y destino observados

La ventana visible se identificó por el título exacto `Resource Browser`. `Edit Mode` estaba abierta al mismo tiempo y pertenecía al mismo proceso, por lo que el título exacto permite distinguir ambas. El breadcrumb mostrado en el navegador era `Modules/CalradiaForge/Assets/GauntletUI/`.

## Textura y controles visibles

El recurso seleccionado era `ui_calradiaforge_1`, con fuente `Modules/CalradiaForge/AssetSources/GauntletUI/ui_calradiaforge_1.png`. El inspector indicaba 2048 × 256, formato B8G8R8A8; los datos de ejecución indicaban 2048 × 256, 12 mipmaps y compresión DXT5. El tipo actual visible era `Albedo (DXT1/DXT5 - RGBA_8)`; el selector también mostraba `Albedo_HQ`, `Normal`, `Specular`, `HDR` y `Heightmap`.

La franja `Filters` enumeraba clases del Resource Browser, incluida `Texture`; no mostraba extensiones de nombres de archivo. Por eso no se deduce de esa casilla que el selector de importación acepte PNG. El archivo individual `calradiaforge_compass.png` existe en los recursos del proyecto, pero no aparecía en la lista visible del navegador. El recurso observado es el atlas `ui_calradiaforge_1`, no el PNG individual.

## Límites y protección

No se abrió un selector de archivos porque las acciones disponibles en la barra aparecían sin nombres accesibles y no fue posible identificar con certeza cuál escanea o importa. No se ejecutó `Import`, no se pulsó `Save`, no se cerró ninguna ventana y no se alteraron `Assets` ni `AssetSources`. Las opciones observadas pertenecen a una textura ya existente; no se presentan como ajustes predeterminados de una importación nueva.

Una ejecución previa del helper `--inspect` agotó el límite de UI Automation de 45 segundos y terminó como `UNKNOWN`. No se reintentó. La inspección visual accesible de esta revisión no convierte ese intento en una calibración interna del helper. El filtro real del selector, la pantalla de ajustes de importación, el inventario completo y el botón final siguen sin calibrarse; `calibrationReviewed` y `--submit` permanecen desactivados. Tampoco se afirma que el recurso haya sido importado en esta sesión.

La guía de TaleWorlds indica que los recursos se importan desde Resource Browser y que la textura puede revisarse en Texture Inspector, pero esta inspección no verificó un nuevo envío: [Resource Editors](https://moddocs.bannerlord.com/editor/resource-editors/), [Implementing Flora](https://moddocs.bannerlord.com/asset-management/implementing_flora/).

La suite `Test-BannerlordFbxImporter.bat` terminó con compilación correcta, 0 advertencias, 0 errores, 36 pruebas aprobadas, 0 fallidas y 1 omitida porque Windows no permitió crear el enlace simbólico de la prueba. Las pruebas incluyeron selección del Resource Browser frente a `Edit Mode`, destino ambiguo, límite de espera y rechazo de acciones de envío por el worker de solo lectura.
