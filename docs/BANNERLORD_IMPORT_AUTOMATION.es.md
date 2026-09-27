# Asistente de UI para Resource Browser de Bannerlord

`BannerlordImportAutomation` es una herramienta opcional de desarrollo para Windows, separada del módulo de juego de Calradia Forge. Usa FlaUI 5.0.0 para inspeccionar o enviar archivos mediante el Resource Browser que ya esté abierto. No llama a una API de importación del motor no documentada, no compila paquetes TPAC ni verifica los recursos resultantes. El helper mantenido es `BannerlordImportAutomation.csproj`; `BannerlordEditorImport.csproj` y su código se conservan como prototipo de referencia y quedan fuera de la validación normal.

## Modos seguros

Ejecuta `Test-BannerlordImportAutomation.bat` para compilar el helper mantenido y ejecutar pruebas breves de sus políticas sin abrir el Editor. Cubren extensiones admitidas, búsqueda solo en el nivel superior, nombres base duplicados, nombres Unicode y con espacios, carpetas vacías/inexistentes, el límite de 100 archivos, argumentos de destino obligatorios y resolución de rutas de destino exactas/ausentes/ambiguas. El BAT de pruebas no inicia el ejecutable compilado, no se conecta al Editor ni importa recursos.

Usa `Run-BannerlordImportAutomation.bat --dry-run "C:\assets\weapons" "*.fbx"` para validar y listar coincidencias sin acceder a UI Automation. Solo se aceptan `.fbx` y `.png`. La búsqueda es solo del primer nivel y cada lote admite como máximo 100 archivos. Se rechazan patrones que incluyan separadores de directorio.

Usa `Run-BannerlordImportAutomation.bat --inspect` únicamente cuando Resource Browser del Modding Kit ya esté abierto. El modo informa controles expuestos, rutas visibles de elementos del árbol y nombres visibles del menú de importación. No envía clics, teclas, selecciones de archivos ni importaciones. Si el menú contextual no está visible, informa que no encontró una opción de importación expuesta.

## Envío experimental

El comando de envío exige el nombre visible exacto de la carpeta del módulo y una confirmación explícita:

```bat
Run-BannerlordImportAutomation.bat "C:\assets\weapons" "*.fbx" --module-name "CalradiaForge"
```

Primero valida las fuentes y se conecta en modo de solo lectura al Resource Browser abierto. Solo continúa si encuentra exactamente una ruta visible en el árbol con el módulo indicado seguido directamente por `Assets`, por ejemplo `Modules > CalradiaForge > Assets`. Una ruta inexistente, ilegible o ambigua detiene el proceso antes de pedir confirmación. Después muestra el destino resuelto y la lista completa de archivos. Debes escribir `IMPORT` antes de que realice acciones de UI.

El helper detecta nombres base duplicados dentro del lote seleccionado. No enumera los archivos que ya existen en la carpeta `Assets` de destino, así que no puede detectar ni garantizar el resultado de una colisión con un recurso existente. Inspecciona el destino en Resource Browser antes de enviar archivos que podrían existir ya.

Tras confirmar, envía los archivos de uno en uno. Un error de UI, un control ausente/ambiguo o un diálogo que no se cierre detiene el lote; no reintenta automáticamente ningún archivo. Los nombres de botones, el AutomationId `1148` del diálogo y el nombre del proceso observado dependen de la versión/instalación y no constituyen una API estable de TaleWorlds. Usa `--inspect` para revisar lo que expone el Editor instalado.

`SUBMITTED` solo significa que las acciones de UI esperadas terminaron y el diálogo de confirmación se cerró. No demuestra que el recurso se importó, compiló, escribió como TPAC ni cargó en Bannerlord. Confirma el resultado manualmente en Resource Browser y con una herramienta de inspección de paquetes compatible y de solo lectura. No sustituyas el flujo documentado de sprites: ejecuta `SpriteSheetGenerator`, busca los archivos nuevos en Resource Browser, importa la categoría generada y verifica el paquete resultante por separado.

Conserva las carpetas de trabajo `AssetSources` y `Assets` del módulo. Si una distribución omite fuentes de autoría, exclúyelas solo de una copia temporal de empaquetado; no borres las fuentes del proyecto.

El helper usa el nombre de proceso del Editor observado en esta instalación, `TaleWorlds.MountAndBlade.Launcher`; puede variar en otras instalaciones. Nunca inicia el juego ni el Editor. El EXE de Release es una salida local de compilación y no se incluye en los ZIP Modules, Source-SDK ni Desktop.

## Helper FBX experimental independiente

`BannerlordFbxImporter` es un segundo experimento independiente de .NET 8 en la raíz del espacio de trabajo. No es el helper mantenido con FlaUI descrito arriba, no está integrado en Calradia Forge y se excluye de todos los ZIP de distribución. Las instrucciones completas están en [BannerlordFbxImporter/README.es.md](../BannerlordFbxImporter/README.es.md).

Este helper recorre únicamente entradas `.fbx` bajo límites de archivos, carpetas, profundidad, tamaño y cantidad total de entradas. `--dry-run` no usa automatización de UI; `--inspect` es de solo lectura y se limita al proceso indicado; `--submit` exige el nombre exacto del módulo y la confirmación interactiva `IMPORT`. Se detiene ante el primer estado incierto de UI y deja abiertos los diálogos de error. `SUBMITTED` significa que se observó completar la secuencia configurada del diálogo de archivos; no prueba que Resource Browser haya importado o compilado el archivo, generado un TPAC válido o cargado el recurso en el juego.

Conserva `Assets` y `AssetSources` en el árbol de autoría. TaleWorlds documenta sus funciones separadas de edición/fuente y permite filtrarlas de una copia publicada; esa documentación no respalda el supuesto fallo de precedencia de carpetas del PDF. No se adoptan las afirmaciones sobre Enter/stdin, un conteo exacto de modales por LOD ni la explicación interna de huesos hoja. Consulta la [revisión de evidencia](ASSET_AUTOMATION_EVIDENCE.es.md#afirmaciones-del-pdf-de-automatizacion-de-recursos).

### Corrección del estado actual — 2026-09-23

El párrafo anterior conserva el estado de una propuesta previa; no describe la capacidad actual de `BannerlordFbxImporter`. El helper independiente ahora planifica lotes acotados y ofrece inspección de solo lectura limitada al proceso configurado. La ruta `--submit` queda bloqueada deliberadamente antes de pedir confirmación y antes de cualquier acción de UI. Sus cuatro perfiles son `static-mesh`, `rigged-mesh`, `texture-only` y `texture-assign`. Los perfiles de malla aceptan FBX; las extensiones de textura deben configurarse explícitamente a partir de filtros observados en el Editor instalado, y la asignación de texturas exige un mapa completo y explícito. No se han calibrado la pantalla de ajustes, el inventario de materiales, el control final Import ni un perfil verificado; por tanto, no se ha enviado ningún archivo.

La última ejecución de `Test-BannerlordFbxImporter.bat` compiló ambos proyectos sin advertencias ni errores y pasó 32 pruebas cortas; se omitió una prueba de punto de reanálisis porque la cuenta actual de Windows no pudo crear un enlace simbólico. Una captura de solo lectura del Editor encontró anteriormente una ventana Resource Browser y 112 controles, pero no estableció una jerarquía fiable del árbol; una inspección posterior con límite de tiempo se agotó. No se realizó ninguna importación ni se reconstruyeron archivos 22.0.0. Estas declaraciones aplican solo al experimento FBX independiente y no cambian el estado separado del helper FlaUI mantenido.
