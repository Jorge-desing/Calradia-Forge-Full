# Experimento de importación FBX y texturas para Bannerlord

`BannerlordFbxImporter` es un experimento independiente para .NET 8 y Windows. Está separado del módulo de juego de Calradia Forge y del helper mantenido con FlaUI; no se incluye en ningún ZIP de distribución. La implementación actual permite planificar lotes locales con límites, inspeccionar Resource Browser en modo de solo lectura y usar un modo UIA que solo solicita el selector. **El envío está bloqueado deliberadamente** hasta calibrar la selección de archivos, los ajustes, el inventario completo, la acción final Import y un ejemplo verificado manualmente.

## Compilación y pruebas breves

Compila con .NET 8 SDK y el paquete de destino Windows Desktop:

```powershell
dotnet build .\BannerlordFbxImporter.csproj --configuration Release
```

Ejecuta las pruebas mediante `Test-BannerlordFbxImporter.bat`. El BAT compila el proyecto de pruebas e invoca el DLL administrado con `dotnet`; no inicia la aplicación auxiliar ni el Editor. Las pruebas cubren perfiles, límites de entrada, mapas explícitos de texturas, análisis consultivo de FBX, rechazo de rutas inseguras, verificación de copias de respaldo, estados, cambios en fuentes y detención ante resultados inciertos.

## Perfiles y planificación local

Elige un perfil para cada lote:

- `static-mesh` y `rigged-mesh` recorren únicamente `.fbx`. El análisis de texto de materiales es consultivo; no valida ajustes de importación, huesos, pesos ni el registro de materiales del Editor.
- `texture-only` solo recorre las extensiones indicadas en `profiles.texture-only.supportedExtensions` de `appsettings.json`. Esa lista debe proceder de los filtros observados en el Editor real; queda vacía por defecto. La documentación de TaleWorlds no confirma formatos de textura admitidos por este flujo.
- `texture-assign` usa la misma regla de extensiones observadas y requiere `--texture-map` con una asignación explícita para cada archivo. El mapa no ejecuta ninguna asignación en el Editor.

Ejemplo de planificación local para modelos:

```powershell
dotnet .\bin\Release\net8.0-windows\BannerlordFbxImporter.dll --dry-run --profile static-mesh --source-folder "D:\Mod Assets\Armor" --materials-manifest .\materials_manifest.example.json
```

Ejemplo de mapa para asignar texturas:

```json
[
  { "sourceFile": "armor_n.png", "textureName": "armor_n", "materialName": "armor_mat", "slot": "Normal" }
]
```

```powershell
dotnet .\bin\Release\net8.0-windows\BannerlordFbxImporter.dll --dry-run --profile texture-assign --source-folder "D:\Mod Assets\Armor" --config .\appsettings.json --texture-map .\texture-map.json
```

Cada recorrido tiene límites de 100 archivos, profundidad 32, 10.000 carpetas, 100.000 entradas, 256 MiB por archivo y 2 GiB por lote. Rechaza nombres base duplicados sin distinguir mayúsculas y omite puntos de reanálisis. Los perfiles de malla solo aceptan `.fbx`; las extensiones de texturas provienen únicamente de la configuración del perfil. `--dry-run` nunca se conecta a UI Automation ni escribe en el módulo.

El preflight FBX solo lee texto ASCII con límites. Los FBX binarios, malformados o demasiado grandes producen evidencia `Not available` y requieren revisión manual; el preflight no demuestra que un recurso se haya importado. Un manifiesto de materiales ausente o inválido también queda como evidencia no disponible.

## Inspección de Resource Browser sin escritura

Abre manualmente el Modding Kit y Resource Browser y ejecuta:

```powershell
dotnet .\bin\Release\net8.0-windows\BannerlordFbxImporter.dll --inspect --config .\appsettings.json
```

`--inspect` se conecta únicamente a `editorProcessName` y al título exacto de ventana visible indicado por `resourceBrowserWindowTitle` en `appsettings.json`. Distingue `Resource Browser` de otras ventanas como `Edit Mode`, aunque compartan proceso, y rechaza cero o varias coincidencias. No envía clics, selecciones, teclas ni comandos de diálogo. UI Automation corre en un proceso auxiliar desechable con un límite predeterminado de 45 segundos. Si el proveedor se bloquea o no se puede establecer la jerarquía, la inspección informa fallo o `UNKNOWN`; no cierra, reintenta ni modifica el Editor. Una lista plana de nombres `TreeItem` no demuestra una ruta `Modules > módulo > Assets`.

El 23-09-2026, una captura directa de solo lectura con Computer Use identificó de forma única la ventana visible `Resource Browser`, mientras `Edit Mode` también estaba abierto en el mismo proceso. La ruta visible era `Modules/CalradiaForge/Assets/GauntletUI/`; el recurso seleccionado fue `ui_calradiaforge_1`, cuya fuente es `Modules/CalradiaForge/AssetSources/GauntletUI/ui_calradiaforge_1.png` (2048 × 256, BGRA8). Texture Inspector mostró el tipo actual `Albedo (DXT1/DXT5 - RGBA_8)`, además de las opciones Albedo HQ, Normal, Specular, HDR y Heightmap, y varios indicadores de textura. Son datos observados de un atlas existente; no demuestran que sean los valores predeterminados de una importación nueva. La lista de filtros de Resource Browser incluye clases de recurso como `Texture`, pero no muestra extensiones de archivo. El PNG individual `calradiaforge_compass.png` no aparecía como recurso en la lista.

Más tarde, un clic derecho en el espacio vacío del panel `Assets` de Resource Browser mostró `Import new asset`. Al seleccionar esa opción se abrió un diálogo de Windows independiente titulado `Abrir`, propiedad del proceso del Editor. La lista `Tipo de archivo` mostró `All (*.tif;*.psd;*.dds;*.bmp;*.tga;*.png;*.hdr;*.exr;*.trf;*.fbx;*.stsdk)` y filtros individuales para cada extensión. Los perfiles locales de textura registran solo los filtros de imagen observados (`.tif`, `.psd`, `.dds`, `.bmp`, `.tga`, `.png`, `.hdr`, `.exr`). Esto confirma que el selector ofrece esos filtros de nombre de archivo; **no** demuestra que el importador acepte, procese o genere un recurso válido a partir del archivo seleccionado. No se eligió ningún archivo; el diálogo se canceló; no se ejecutó la acción final `Import` ni `Save` y no se alteraron recursos del módulo. El diálogo se abrió desde un comando contextual de Resource Browser; no se encontró un selector separado en el menú de `Edit Mode`.

El intento separado de UI Automation de `--inspect` agotó su límite de 45 segundos y devolvió `UNKNOWN`; no se ha repetido. El tiempo agotado no invalida las observaciones manuales anteriores, pero el helper aún no puede calibrar los ajustes posteriores a elegir un archivo, el inventario completo ni el botón final Import. PNG aparece en el filtro de nombre del selector; la importación real y la salida siguen sin verificarse. El envío continúa bloqueado.

## Envío bloqueado y estados de evidencia

El comando `--submit` está restringido al perfil `--profile texture-only` y exige `--calibration-sample <png-verificado>` además del nombre exacto del módulo. Antes de cualquier posible interacción con la interfaz, requiere evidencia persistente en `%LocalAppData%\CalradiaForge\Importer\Calibration\texture-only.json`. El lector valida el esquema y la integridad SHA-256; después coteja el nombre, hash de contenido y tamaño actuales de la muestra, el perfil, módulo y ruta local exacta de `Assets`, nombre y tipo observados del recurso, ajustes configurados, proceso del Editor y título de ventana. SHA-256 es aquí una comprobación de integridad, no una atestación firmada. Los indicadores o selectores de calibración de `appsettings.json` por sí solos nunca autorizan un envío.

Incluso si la evidencia persistente coincide, la importación sigue deshabilitada: el comando termina en `LOCKED` antes de interactuar con la interfaz porque todavía no se implementaron ni verificaron la secuencia real del selector, ajustes y botón final `Import`, ni la observación de salida. No pide `IMPORT`, no pulsa controles ni afirma `SUBMITTED`. Una muestra cambiada, otro módulo/ruta/proceso/ventana/ajustes, un registro inválido o un perfil distinto de `texture-only` hacen que falle de forma cerrada. Cambiar indicadores de appsettings no evita estas comprobaciones.

El modelo define los estados `PLANNED`, `TARGET_READY`, `SETTINGS_READY`, `SUBMITTED`, `IMPORTING`, `OUTPUT_OBSERVED`, `VERIFIED`, `STOPPED` y `UNKNOWN`. Ningún estado de importación se deduce de cerrar un selector de archivos ni del título del proceso. `SUBMITTED` se reserva para una invocación confirmada del botón final Import; `VERIFIED` exige observar el nombre y tipo del recurso esperado en Resource Browser. La revisión con Model Viewer se registra por separado. Mientras no exista calibración, el envío, la observación de salida, la verificación y la revisión visual siguen sin ejecutarse.

El servicio de copias puede crear una instantánea acotada de la carpeta `Assets` directa del módulo, verificada con SHA-256, bajo `%LocalAppData%\CalradiaForge\Importer\Backups`. Por separado, ya existen componentes de seguridad para clasificar colisiones con identidad exacta del recurso, verificar que los hashes y el conjunto de archivos de respaldo correspondan exactamente al recurso afectado, reconocer solo un diálogo de reemplazo totalmente calibrado y emitir un permiso de un solo uso que prohíbe reintentos. Estas comprobaciones no pulsan ni cierran el diálogo y no están conectadas al flujo real de importación. Por tanto, el reemplazo automático no está operativo y este helper todavía no puede afirmar ni ejecutar un reemplazo.

### Último intento de calibración — 2026-09-23

El operador autorizó expresamente seleccionar archivos y enviar acciones al Editor. En el primer intento, Computer Use enumeró el proceso del Editor y las ventanas `Resource Browser` y `Edit Mode`, pero falló al abrir el estado de accesibilidad de Resource Browser con `Accessibility state unavailable`. Tras reiniciar la sesión de Computer Use, estuvieron disponibles la captura visual y el árbol AX. Mostraron la ruta `Modules > CalradiaForge > Assets > GauntletUI` y la textura previamente seleccionada `ui_calradiaforge_1` con su inspector. Esto solo acredita la observación del recurso existente, no de la muestra.

Los tres botones de la barra no tenían nombres accesibles. Una invocación AX de `Extras` devolvió coordenadas fuera de la ventana, por lo que ese resultado no permitió identificar la acción. Se hizo una inspección reversible de `Assets/Extras` y se cerró con `Esc`. No se seleccionó `calradiaforge_compass.png`; no se abrió el selector de archivos ni el flujo de ajustes de importación; no se activó `Save` ni el `Import` final; y no se escribió ningún recurso. Por tanto, la muestra sigue sin calibrar. La inspección visual se recuperó parcialmente, pero el selector y los controles de importación siguen inaccesibles o sin identificar; es un bloqueo técnico/de calibración, no falta de autorización del usuario.

La ruta `--submit` del código actual del helper continúa bloqueada. No la habilites cambiando solo indicadores de configuración. Reanuda la calibración cuando sea posible inspeccionar de forma fiable Resource Browser y sus controles exactos; detente ante una ventana ambigua, un árbol de accesibilidad no disponible, un timeout o un diálogo inesperado. La acción final `Import` para la muestra todavía requiere mostrar el destino, nombre de archivo y ajustes exactos, y obtener la confirmación explícita `IMPORT` del operador. Solo se podrá considerar un lote posterior `texture-only` cuando el nombre y tipo esperados de esa muestra se observen y verifiquen manualmente en el Editor. Aquí no se afirma que se haya creado un TPAC ni que la importación haya tenido éxito.

### Compuertas locales de envío y reemplazo — 2026-09-23

El CLI ahora limita `--submit` a `texture-only` y exige el PNG intacto y verificado manualmente mediante `--calibration-sample`. Lee `%LocalAppData%\CalradiaForge\Importer\Calibration\texture-only.json` y coteja esquema, integridad SHA-256, nombre/hash/tamaño de la muestra, perfil, módulo, ruta exacta de `Assets`, nombre/tipo observado del recurso, ajustes, proceso y título de ventana antes de cualquier posible acceso a la interfaz. Los indicadores de appsettings no bastan. La inspección descrita arriba no produjo un registro de calibración y la ruta final sigue bloqueada incluso con evidencia coincidente porque faltan la secuencia real de importación y la verificación de salida.

Solo después de observar personalmente que la muestra se importó en Resource Browser y verificar su nombre, tipo y ajustes exactos, puedes registrar esa atestación manual:

```powershell
dotnet .\bin\Release\net8.0-windows\BannerlordFbxImporter.dll --record-calibration --profile texture-only --module-name "CalradiaForge" --calibration-sample ".\modules\CalradiaForge\GUI\SpriteParts\ui_calradiaforge\calradiaforge_compass.png" --observed-resource-name "<nombre-exacto-del-recurso>" --observed-resource-type "<tipo-exacto-del-recurso>" --config .\appsettings.json
```

Este comando no recorre un lote ni inspecciona/interactúa con el Editor. Exige la ruta directa a `Assets` del módulo y ajustes de textura observados y no vacíos en `appsettings.json`; muestra los valores y solo escribe evidencia local si el operador teclea exactamente `VERIFIED`. Al guardar, valida y calcula el hash del PNG. Es una atestación manual, no una observación ni verificación automática. No lo ejecutes para la muestra actual hasta observar la importación y confirmar el nombre y el tipo; registrar la atestación tampoco desbloquea `--submit`.

Se añadieron componentes de seguridad aislados para colisiones exactas, verificación de conjunto de archivos y hashes del respaldo por recurso, reconocimiento del diálogo calibrado y permiso de un solo uso sin reintento tras incertidumbre. La automatización de importación no los invoca, por lo que el reemplazo automático no está disponible. `Test-BannerlordFbxImporter.bat` terminó con 48 pruebas aprobadas, 0 fallidas y 1 omitida porque el entorno no permitió crear un symlink; son pruebas de fixtures, no validación de una importación en el Editor real.

Conserva intactas `Assets` y `AssetSources`. TaleWorlds define `Assets` como metadatos TPAC editables, `AssetSources` como archivos fuente importados y `AssetPackages` como salida cliente de solo lectura; la documentación no demuestra la regla fatal de precedencia alegada para carpetas de autoría vacías. No borres directorios para intentar corregir un fallo basándote en esa afirmación.

`Add Leaf Bones` solo es una recomendación manual de exportación. El helper no puede leer esa opción de Blender desde FBX. La regla `_notused` de TaleWorlds indica ignorar un esqueleto en el caso documentado de importar activos relacionados; no demuestra una transferencia automática de pesos.

El flujo de SpriteSheetGenerator sigue separado y es manual. TaleWorlds documenta el comando de consola `resource.show_resource_browser`, pero no un protocolo de Enter por stdin redirigido ni un ciclo de bloqueo de PNG. Este helper nunca inicia el generador ni le envía entrada.

## Fuentes

- [Convenciones de nombres de TaleWorlds](https://moddocs.bannerlord.com/asset-management/asset-types/asset_naming_conventions/)
- [Funciones de carpetas y publicación de TaleWorlds](https://moddocs.bannerlord.com/asset-management/asset-types/overriding_assets/)
- [Flujo de hojas de sprites de TaleWorlds](https://moddocs.bannerlord.com/asset-management/generating_and_loading_ui_sprite_sheets/)
- [Comportamiento de salida estándar de Process en Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput?view=net-10.0)

### Último intento — 2026-09-23

El operador aclaró que el selector de importación se abre desde el menú contextual de Resource Browser, no desde la ventana separada de Edit Mode, y envió una captura donde se ve la opción exacta `Import new asset` en un espacio vacío del panel `Assets`. Una nueva inspección de solo lectura volvió a mostrar `Modules > CalradiaForge > Assets`, con `GauntletUI` como único elemento visible. La opción del menú queda identificada por la captura proporcionada por el operador.

El puente actual de Computer Use no pudo completar la selección: informó entrada del usuario durante el primer intento de clic derecho y después la captura del menú emergente devolvió varias regiones sin elementos de accesibilidad para el menú. Un intento por coordenadas se mapeó inicialmente sobre la ventana superpuesta `Edit Mode`; tras traer Resource Browser al frente y refrescar, no apareció ningún selector de archivos. No se eligió ningún archivo, no se ejecutó Import ni Save y no cambió ningún recurso del módulo. El bloqueo pendiente es interactuar de forma fiable con el menú contextual, no identificar el nombre del comando. `--submit` sigue bloqueado.

Tras actualizar el mensaje de ayuda del CLI para incluir `--record-calibration`, `Test-BannerlordFbxImporter.bat` compiló correctamente con 0 advertencias y 0 errores: 52 pruebas aprobadas, 0 fallidas y 1 omitida porque Windows rechazó la creación de symlinks. Son pruebas automatizadas de fixtures; no verifican una importación real ni la salida del Editor.

### Modo UIA solo para abrir el selector y último intento real — 2026-09-23

Usa el nuevo modo solo para abrir el selector con:

```powershell
dotnet .\bin\Release\net8.0-windows\BannerlordFbxImporter.dll --open-import-picker --config .\appsettings.json
```

La opción exacta `Import new asset` del menú contextual debe estar ya visible en el Resource Browser configurado antes de ejecutar el comando. El modo **no** hace clic derecho ni crea o vuelve a abrir el menú contextual. Solo invoca un único elemento UI Automation `MenuItem` visible y habilitado cuyo nombre exacto sea `Import new asset`, cuyo PID coincida con el Editor configurado y cuyo título de ventana coincida con el Resource Browser configurado. Si falta, está duplicado o deshabilitado, o no ofrece el patrón de invocación requerido, la operación falla de forma segura y no usa una acción alternativa.

Aunque UIA invoque el elemento y el programa imprima `PICKER_MENU_INVOKED`, eso solo significa que se invocó el elemento del menú; no demuestra que apareciera el selector de Windows. El modo se detiene antes de elegir un archivo y no ejecuta la acción final `Import` ni `Save`. No puede informar que un recurso se creó o verificó. `--submit` permanece bloqueado.

Se intentó una invocación real de la interfaz; permaneció sin respuesta durante más de 60 segundos y se interrumpió. No se vio ni verificó ningún selector de archivos, no se eligió un archivo y no se observó importación ni cambio de recursos. El resultado más reciente de `Test-BannerlordFbxImporter.bat` fue 58 aprobadas, 0 fallidas y 1 omitida; estas pruebas de fixtures no verifican la invocación UIA en vivo ni una importación.

### Corrección del drenaje de pipes del worker — seguimiento del 2026-09-23

Se corrigió el drenaje de stdout/stderr del worker con un límite de 3 segundos para el cierre; si alguna pipe no cierra dentro de ese límite, el resultado del worker es `UNKNOWN`. Esto podría estar relacionado con el cuelgue real anterior, pero no se comprobó ni probó como causa. No se repitió la interacción con la interfaz porque el estado del Editor era incierto. El selector y cualquier importación siguen sin verificarse, y `--submit` continúa bloqueado. La última ejecución de `Test-BannerlordFbxImporter.bat` tuvo una compilación limpia y reportó 59 aprobadas, 0 fallidas y 1 omitida; no establece la causa del cuelgue real ni verifica una importación.
