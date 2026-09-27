# Resource Browser: observación del selector de archivos

## Hallazgo de interfaz

El 23-09-2026, en la ventana `Resource Browser` del proceso `TaleWorlds.MountAndBlade.Launcher`, se abrió el menú contextual desde un área vacía del panel de `Assets`. El menú mostró la opción `Import new asset`. Al elegirla se abrió una ventana de diálogo de Windows separada, titulada `Abrir`, perteneciente al mismo proceso del Editor.

## Filtros observados

El diálogo mostraba el filtro combinado `All (*.tif;*.psd;*.dds;*.bmp;*.tga;*.png;*.hdr;*.exr;*.trf;*.fbx;*.stsdk)` y filtros individuales para cada patrón, incluidos `*.png` y `*.fbx`. Los perfiles locales `texture-only` y `texture-assign` registran únicamente los formatos de imagen visibles: `.tif`, `.psd`, `.dds`, `.bmp`, `.tga`, `.png`, `.hdr` y `.exr`.

El filtro solo demuestra que el selector ofrece esos patrones de nombre. No demuestra que seleccionar un archivo termine con una importación correcta, qué tipo o ajustes resulten, ni qué recurso se genere. No se seleccionó archivo, no se abrió la pantalla de ajustes de una textura nueva, no se confirmó la importación y no se comprobó una salida. `Assets` y `AssetSources` quedaron intactas; no se ejecutó `Import` ni `Save`. La ventana `Abrir` se cerró con `Cancelar`.

## Calibración y envío

La observación se limita al comando del menú contextual del Resource Browser y al filtro del selector. No se encontró en `Edit Mode` un selector separado documentado u observado; la opción probada se lanzó desde Resource Browser. Los ajustes posteriores a seleccionar un archivo, el inventario completo de recursos, la ruta de reemplazo y el botón final de importación continúan sin calibrar. `editorCalibrationReviewed`, `calibrationReviewed` y `resourceInventoryComplete` permanecen desactivados. `--submit` sigue bloqueado y la inspección de `--inspect` del helper sigue limitada a lectura del Resource Browser.

## Reconciliación append-only de Rev014

Existen dos notas fuente Markdown con el prefijo `Rev014`: `Rev014-Resource-Browser-icon-calibration-2026-09-23.md` documentó un intento anterior del helper que terminó en `UNKNOWN`; `Rev014-Resource-Browser-texture-observation-2026-09-23.md` conservó una observación posterior del atlas existente. El registro DOCX protegido `CalradiaForge-Registro-Mejoras-Rev014.docx` contiene una sola revisión Rev014 y la observación posterior del atlas. No se reescribieron esas notas ni el DOCX; esta nota Rev015 aclara la secuencia sin cambiar el historial.

La cifra de pruebas citada en la nota fuente Rev014 cuenta una prueba sintética del estado `TimedOut`; no verifica que un proceso UIA real se quede bloqueado, expire y se recupere. La suite automatizada sigue ejecutándose fuera del Editor y no importa archivos.

## Resultado

El selector separado ya se observó de forma segura y su filtro de nombres quedó registrado. La importación real de un PNG, la pantalla de ajustes, la generación/verificación de salida y la calibración aprobada permanecen pendientes de una validación manual controlada. No se generaron ZIP nuevos y la versión del producto permanece en `22.0.0`.
