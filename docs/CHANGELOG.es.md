# Lista de cambios

## Viewport WPF aislado del runner — 2026-09-30 (Registro Rev101)

- El fixture de render amplía únicamente los límites de seguimiento de su HWND aislado para que una pantalla alojada menor no limite la matriz fija de 1360×820 DIP. Una regresión ejercita límites nativos de 1024×768; la aplicación y la configuración de pantalla permanecen intactas. El BAT Desktop aprobó 65/65 y render WPF 293/293 con 182 pases de layout y compilación limpia. El primer workflow del registro subido aprobó; CI compiló y aprobó Desktop, pero expuso esta limitación del host de render. Las comprobaciones remotas de esta corrección siguen pendientes.

## Recuperación de CI alojada — 30-09-2026 (Registro Rev100)

- La CI alojada compila destinos portables mediante `CalradiaForge.Portable.slnf` y el launcher BAT; la integración local de Bannerlord conserva `net472` y requiere ensamblados TaleWorlds licenciados mediante `GameBin`. Core mantiene sus dos destinos. Las auditorías del registro reconocen los ViewModels Desktop divididos y los snapshots inmutables de parches. La auditoría de solapamientos respeta la visibilidad mutuamente excluyente comprobada en fuentes y la condición actual del margen de evidencia. Pasaron las pruebas locales Core 401/401, ForgeWeave 73/73, Desktop 65/65 y render WPF 292/292; las verificaciones Python pasaron 12 fixtures de assets, 5 pruebas de imágenes, Ruff y auditorías de sprites/iconos. La instalación opcional de agentes encontró una respuesta truncada del mirror local. Los nuevos checks GitHub quedan pendientes en esta revisión; las ejecuciones históricas fallidas permanecen en Actions.

## Seguimiento de fuentes 25.2.0 — 30 de septiembre de 2026 (Rev076 del registro)

- `Dispose()` de los handles de hooks y parches informa las reversiones fallidas en vez de descartar el resultado. La distribución de hooks vuelve a comprobar el contexto aprobado después de Prefix y antes de Postfix; conserva los argumentos originales si la puerta se cierra antes del destino y omite callbacks posteriores si el destino sale del contexto. Forge mantiene `Patches` publicado mientras queden registros por recuperar, solo permite revertir `Conflict` cuando se verifican los bytes instalados/originales registrados y restaura el ciclo de vida del host anterior si falla la reconexión del entrante. La validación por BAT pasó Core 393/393, ForgeWeave 73/73, el fixture serial x64, Desktop 65/65 y render/recursos WPF 292/292 con 182 pases de layout. No se inició directamente el EXE del fixture, no se abrió Bannerlord, campaña o batalla y no se regeneraron ZIP.

## Seguimiento de fuentes 25.2.0 — 30 de septiembre de 2026 (Rev075)

- La distribución de hooks ahora comprueba en cada invocación el gate exacto del menú principal aprobado en el hilo del juego. Fuera de ese contexto omite los callbacks personalizados y llama al destino original, mientras el detour permanece instalado hasta revertirse explícitamente. WPF permite preparar reversiones de recuperación para snapshots `Conflict` y `Failed`, y la confirmación avisa que Apply por lote es secuencial y puede dejar hooks anteriores aplicados si falla uno posterior. Los 13 idiomas de Desktop incluyen las advertencias de ciclo de vida y Apply parcial. La validación por BAT pasó: Core 390/390, ForgeWeave 73/73, fixture detour serial x64, Desktop 65/65 y render/recursos WPF 292/292 con 182 pases de layout; las compilaciones tuvieron cero advertencias/errores. No se hizo una comprobación con Bannerlord en vivo; los fixtures seriales no prueban seguridad con llamadas concurrentes al destino; `ForgeApi.Version` sigue en 12, el producto en 25.2.0 y no se regeneraron ZIPs.

## Seguimiento de fuentes 23.0.0 — 25 de septiembre de 2026 (Rev033)

- Refuerza los diagnósticos del proveedor compartido y conserva el contrato fail-fast existente de `Require<T>` y los tipos de excepción. Los fallos por proveedor ausente, servicio ausente, identidad del contrato CLR y versión incluyen ahora el contexto de proveedor/servicio, el contrato o las versiones esperadas y publicadas cuando corresponde, y una ruta para corregir la configuración. Amplía la cobertura de integración del ciclo de vida proveedor/consumidor, la identidad del contrato compartido entre ensamblados, el rechazo de versiones, los handles invalidados, las dependencias de manifiesto y el empaquetado del DLL de contratos una sola vez con el proveedor. `ForgeApi.Version` permanece en 6; las firmas públicas, `net472`, el orden de carga y las reglas del hilo del juego no cambian. `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` compiló con cero advertencias/errores y pasó Core 252/252 y ForgeWeave 37/37; el host BAT ejecutó la biblioteca Core dentro del proceso. No se inició directamente ningún ejecutable de pruebas, no se crearon ZIP ni se inició Bannerlord.

## Seguimiento de fuentes 23.0.0 — 25 de septiembre de 2026 (Rev034)

- Amplía los servicios compartidos con resultados opcionales de `ModuleLibrary.Resolve<T>` que distinguen servicio disponible, proveedor ausente, servicio ausente, contrato incompatible y versión incompatible, y conserva `Require<T>` fail-fast y su contrato de excepciones. Añade la publicación transaccional `SharedServiceBatch` mediante `BeginPublication`, `Add` y `Commit`; el lote confirmado permanece como lease del ciclo de vida y retirarlo elimina el grupo completo. Añade `SharedServiceMonitor<T>` mediante `Watch<T>`, con una instantánea inicial `Current` y notificaciones `Changed` sincrónicas en el hilo del registro para los cambios posteriores. Los argumentos de `Changed` son snapshots de transición inmutables y pueden estar desactualizados cuando se ejecuten manejadores posteriores; consulta `monitor.Current` o llama a `Resolve<T>` para obtener el estado actual, ya que un handle puede haber quedado invalidado al retirar el servicio. Una mutación del registro dentro de un manejador `Changed` surte efecto de inmediato; solo se encola la entrega de callbacks para evitar el despacho recursivo. Los errores de los manejadores se aíslan y quedan disponibles en `LastNotificationError`. La desconexión libera silenciosamente los monitores. `ForgeApi.Version` aumenta de 6 a 7; la versión del producto permanece en 23.0.0, los destinos del SDK siguen siendo `net472` y `net8.0` y la integración del módulo del juego sigue en `net472`; no cambian IPC ni el comportamiento del módulo del juego. `cmd /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` terminó con compilación de 0 advertencias/0 errores, Core 263 pasadas/0 fallidas y ForgeWeave 43 pasadas/0 fallidas. No se inició directamente ningún ejecutable de pruebas, no se generaron ZIP ni se inició Bannerlord.

## Seguimiento de fuentes 22.0.0 — 25 de septiembre de 2026 (Rev032)

- Rev032 reorganiza la mesa de trabajo WPF independiente con una cabecera compacta, navegación adaptable y un informe accesible por teclado, conservando las 194 rutas, los comandos y permisos existentes, los contratos MVVM/IPC, los 13 idiomas y los tres temas. Añade ilustraciones cartográficas y heráldicas locales optimizadas al marco pasivo de War Table y Parchment Light; Alto contraste permanece sólido y las superficies de entrada, evidencia y comandos siguen limpias. La validación queda pendiente: comparación de cinco corridas del harness de render en el mismo equipo, suites Desktop/render por BAT sin reducir cobertura e inspección WPF de solo lectura mediante el launcher. Los tiempos del harness no son latencia de la aplicación; aquí no se afirma una mejora de rendimiento ni una inspección en vivo aprobada. No cambian API pública, IPC, versión, ZIP ni recursos de Bannerlord.

## Seguimiento de fuentes 22.0.0 — 24 de septiembre de 2026 (Rev031)

- Rev031 añade tres texturas WPF originales, locales y deterministas para el marco superior, el rail operativo y las esquinas de las tarjetas. Sus derivados optimizados agregan 92.537 bytes comprimidos y 273.328 bytes RGBA decodificados; los adornos son pasivos, y el ledger de evidencia, los campos editables, los comandos y las superficies de Alto contraste permanecen limpios. El campo de búsqueda incorpora una acción localizada para limpiar el texto, visible solo cuando hay contenido; limpia el filtro existente y devuelve el foco sin ampliar la API pública. El harness de render evita volver a aplicar un tema que no cambió durante la matriz de idiomas. El lote completo de Desktop compiló con cero advertencias/errores y pasó Desktop 51/51 y WPF 272 casos con 148 pases de layout; la validación determinista de texturas pasó. Cinco ejecuciones finales del BAT de render dieron una mediana de 5,846 s frente a 6,337 s en la línea base recién medida antes del cambio (7,7% menos). Una comprobación de solo lectura mediante el launcher expuso la ventana WPF y sus controles principales por UI Automation. Son comprobaciones del harness y del inicio del shell, no validación de juego. No cambian la versión, la API pública, IPC, ZIP ni sesiones, campañas o batallas de Bannerlord.

## Seguimiento de fuentes 22.0.0 — 24 de septiembre de 2026 (Rev030)

- Rev030 enfoca las mediciones de respuesta WPF en el arranque, filtrado, selección de rutas y operaciones asíncronas. Distingue las asignaciones síncronas por hilo del trabajo async: conserva su duración y muestra los bytes asignados como no disponibles. Al vaciar el filtro se libera la página seleccionada y las páginas liberadas no conservan resultados asíncronos tardíos. La suite comprueba rutas, iconos y enlaces de las 194 rutas, con layout completo para estados representativos, 13 idiomas, tres temas, textos largos, evidencias y escalas de 100–200%. Desktop pasó 51/51; render y recursos WPF pasaron 271 casos con 146 pases de layout representativos; la compilación tuvo cero advertencias/errores. Cinco corridas del BAT de render: 5,675, 5,677, 5,749, 5,703 y 5,738 s (mediana 5,703 s; 46,0% por debajo de la referencia de 10,559 s). Son tiempos del harness, no latencia de la aplicación en vivo; una muestra sintética de selección superó 16,7 ms. La inspección UI Automation de solo lectura no se ejecutó porque la política de la herramienta rechazó el comando aislado de lanzamiento/inspección. Todas las pruebas usaron `.bat`, sin lanzar binarios de prueba directamente. No cambian API, IPC, versión, recursos del juego ni ZIP.

## Seguimiento de fuentes 22.0.0 — 24 de septiembre de 2026 (Rev029)

- Rev029 refuerza la cancelación y las transacciones de archivos en Desktop y el ciclo de vida de extensiones: las peticiones por named pipe distinguen cancelación de timeout y desconectan el canal si se interrumpe una respuesta; los respaldos de ensamblados se copian y hashean en bloques acotados, la salida temporal se valida antes de confirmarse y la limpieza conserva archivos creados por procesos concurrentes. El cierre de páginas Gauntlet se despacha al hilo del juego; la capacidad de la cola usa contabilidad atómica y las tareas confiables de limpieza no se descartan al llenarse la cola. La suite completa por `.bat` pasó: compilación con cero advertencias/errores, Core 239/239, ForgeWeave 31/31, Desktop 50/50, render WPF con 268 casos y todas las comprobaciones de assets/preflight; los scripts de revisión de fuente y pruebas no reportaron hallazgos. El harness registró 507 pases en 11,334 s; esto no mide la latencia de la aplicación en vivo. No cambiaron la versión, API pública, ZIP, TPAC, recursos instalados ni se inició juego, campaña o batalla.

## Seguimiento de fuentes 22.0.0 — 24 de septiembre de 2026 (Rev028)

- Rev028 traslada al trabajo en segundo plano la proyección de evidencias y el formato del informe del analizador acotado, conservando la cancelación. Las derivaciones locales de texturas WPF ahora se generan de forma determinista: se empaquetan 6.901.545 bytes comprimidos y 11.489.464 bytes de imagen decodificada, frente a 17.864.675 y 64.474.344 del conjunto original a resolución completa. Las fuentes de autoría quedan fuera de los recursos de la aplicación. El mantenimiento de pruebas elimina lanzamientos de PowerShell por fixture, evita analizar dos veces el prefab y coloca los PNG de prueba en directorios temporales aislados. La mediana de cinco ejecuciones de la prueba API bajó de 2,72 s a 2,28 s; Asset Batch pasó de 14,699 s a 3,446 s y Resource Browser de 22,390 s a 3,943 s. El runner focalizado `--core-only` conserva ambas suites, Core y ForgeWeave, y midió 7,319 s antes y 6,115 s después en una corrida. La batería completa compiló sin advertencias ni errores y pasó AssetPipeline, AssetBatchPlan, ResourceBrowser, preflight FBX, Core 232/232, ForgeWeave 31/31, Desktop 42/42 y render WPF 268/268 en 34,25 s; el harness de render tardó 9,866 s en 507 pases de layout. Son mediciones de pruebas y empaquetado, no de latencia de la aplicación en ejecución. No cambiaron la versión, API, ZIP, TPAC ni recursos instalados; no se inició campaña ni batalla.

## Seguimiento de fuentes 22.0.0 — 24 de septiembre de 2026 (Rev027)

- Rev027 reduce el trabajo repetido del escritorio: almacena el texto de búsqueda de cada herramienta inmutable, agrupa las notificaciones de las colecciones, reutiliza los modelos de grupos visibles y conserva los snapshots de favoritos y recientes. El rail operativo virtualiza y recicla sus filas, por lo que no materializa como controles las 194 rutas del catálogo. Se retiraron helpers huérfanos y no registrados de las pruebas de contratos; sus comprobaciones útiles quedaron en casos activos. El runner de solo Desktop compila ambos proyectos de prueba y sus dependencias mediante un único filtro de solución, en vez de ejecutar dos compilaciones separadas o compilar toda la solución. El harness WPF ya no vacía el Dispatcher con prioridad ApplicationIdle en cada frame; el layout síncrono conserva toda la matriz. La última ejecución por `.bat` compiló sin advertencias ni errores, pasó las pruebas Desktop 42/42 y las pruebas de render WPF 268/268; completó 507 pases de render/layout en 9,822 s y la corrida Desktop completa tardó 13,974 s. Es un resultado del harness, no una medición del rendimiento en ejecución del producto. No se modificaron versión, API pública, ZIP ni TPAC; no se inició campaña ni batalla.

## Seguimiento de fuentes 22.0.0 — 24 de septiembre de 2026 (Rev026)

- Rev026 completa la ronda visual de Desktop y Gauntlet sin cambiar la versión, los IDs de ruta, la API pública ni las preferencias guardadas. WPF verifica ahora 11 texturas empaquetadas localmente, marcas específicas para Parchment/War Table, una acción localizada para el estado vacío de evidencia, controles de título accesibles, estados de ComboBox adaptados al tema y un rail virtualizado con reciclaje. El BAT de solo Desktop compiló sin advertencias ni errores; las pruebas Desktop pasaron 42/42 y la suite WPF 268 casos, incluidos los 194 recorridos actuales, 13 idiomas a cuatro escalas DPI y tres temas a cuatro escalas. El render tardó 10,047 s: menos que los 12,3 s de la corrida anterior inmediata, aunque aún supera la referencia previa de 8,66 s. Las comprobaciones fuente de Gauntlet reportan preparación determinista de texturas, SpriteData vigente, auditoría limpia del prefab y Core 232/232. El TPAC instalado sigue siendo anterior al atlas fuente; la importación por Resource Browser y el render en el juego continúan pendientes. No se escribieron TPAC ni ZIP.

## Seguimiento de fuentes 22.0.0 — 24 de septiembre de 2026 (Rev025)

- Rev025 registra el canal de recursos ImageGen completado: cuatro sprites materiales deterministas, un atlas fuente de 2048×128 generado por TaleWorlds, SpriteData vigente con las 13 partes declaradas dentro de límites y respaldos verificados antes del reemplazo. Pasaron las auditorías de sprites y diseño Gauntlet; la solución compiló sin advertencias ni errores, Core pasó 232/232, ForgeWeave 31/31 y la suite visual por lotes pasó tras reconstruir las pruebas obsoletas. El TPAC de ejecución sigue siendo anterior; la importación con Resource Browser y el render en vivo continúan pendientes. No se generaron ZIP.

## Seguimiento de fuentes 22.0.0 — 24 de septiembre de 2026

- Rev024 sustituye el conjunto de adornos geométricos de Gauntlet por cuatro maestros materiales de ImageGen —madera de pino, aguada de tinta, latón envejecido y fieltro de pino— procesados local y determinísticamente como sprites listos para el atlas. Se limitan al marco visual; la evidencia, los campos editables y los comandos permanecen sobre superficies limpias. No cambian las rutas ni el comportamiento. Las pruebas de sprites, la regeneración del atlas, la importación con Resource Browser y el renderizado en vivo siguen pendientes; no se modificaron TPAC ni ZIPs.

## Seguimiento de fuentes 22.0.0 — 23 de septiembre de 2026 (no incluido en los ZIP existentes)

- Seguimiento de Rev021: se ejecutó el generador oficial de sprites de TaleWorlds sobre una copia aislada de preparación y se regeneraron el atlas fuente y SpriteData con las dos decoraciones originales. El validador confirma que los recortes de píxeles del atlas coinciden exactamente, que las dimensiones y AlwaysLoad coinciden con los metadatos y que la ubicación en el prefab es segura. El TPAC existente todavía es anterior al atlas; la importación desde Resource Browser y la inspección dentro del juego siguen pendientes. No se regeneraron ZIP.
- Rev022 añade tres fuentes Gauntlet originales y deterministas: `forge_table_grain` (128 × 64) para el marco pasivo de cabecera/rail, `forge_map_contours` (128 × 64) detrás del filigranado existente de la cabecera y `forge_brass_rule` (128 × 16) como separador de una sección real. Las superficies de entrada, comandos y evidencia permanecen limpias. La validación estructural está pendiente; el TPAC existente está obsoleto respecto de las fuentes, la importación desde Resource Browser y el render en vivo siguen pendientes, y no cambian los ZIP ni la versión.
- Rev023 registra una tercera ronda visual para Gauntlet con `forge_heraldic_corner` (128 × 32), que sustituye a `forge_header_filigree`, y una textura de borde tejido. La sustitución conserva el atlas de 2048 × 256 y mantiene `forge_pine_grain`; también se refuerza la jerarquía de navegación, comandos, estados y foco de teclado. La decoración es pasiva y se mantiene lejos de la evidencia y las superficies editables; las ocho áreas, la API, el comportamiento y la versión no cambian. Aún no se registran las comprobaciones estructurales ni la generación del atlas para esta revisión; la importación TPAC y el render en vivo siguen pendientes. No se regeneraron ZIP.

- Añade el `BannerlordFbxImporter` independiente y experimental con cuatro perfiles explícitos, recorrido recursivo acotado, rechazo de nombres base duplicados, extensiones de textura configuradas, mapas explícitos de asignación, evidencia consultiva de materiales FBX ASCII y un servicio independiente de respaldo verificado por hash. Mantiene `--submit` bloqueado antes de la confirmación y la UI porque aún no se calibraron la ruta del Resource Browser, los ajustes, el inventario de materiales, el control final Import ni ejemplos verificados. `Test-BannerlordFbxImporter.bat` compiló sin errores y pasó 32 pruebas; se omitió una prueba de enlace simbólico porque Windows denegó su creación. La inspección de solo lectura del Editor no fue concluyente; no hubo importación real ni reconstrucción de ZIP.
- Se confirmó en la guía de sprites de TaleWorlds que `Alt + backtick` seguido de `resource.show_resource_browser` abre Resource Browser. El `Enter` de ese procedimiento envía el comando de consola; no demuestra que SpriteSheetGenerator espere entrada estándar redirigida. El helper FBX sigue sin ejecutar el generador. Antes de cada envío se vuelven a comprobar tamaño y fecha del FBX y se mantiene un bloqueo compartido de solo lectura durante la secuencia UI; la comprobación de respuesta de ventana usa una sonda `WM_NULL` con límite de tiempo. Las pruebas cortas pasan con 59 aserciones; no se importó desde el Editor ni se reconstruyeron ZIP.
- Se revisó la propuesta adicional de automatización C#: las instrucciones oficiales de consola no verifican tiempos de `SetForegroundWindow`/`SendKeys` ni que sea seguro cerrar errores UIA. No se integran; los diálogos inesperados quedan abiertos y detienen el lote.
- Revisión de fallo seguro: los errores al enumerar ventanas de UI Automation y los modales visibles/anidados ya no pueden confundirse con el cierre del diálogo ni producir `SUBMITTED`. Se limitó la cantidad total de entradas recorridas y se acotaron las advertencias de puntos de reanálisis. Se reemplazó el README obsoleto del helper y se clasificaron las afirmaciones del PDF con evidencia por fuente; no se importó desde el Editor ni se reconstruyeron ZIP.
- Endurecimiento posterior: las rutas ilegibles del árbol de Resource Browser y las carpetas `Assets` anidadas ahora fallan de forma segura; los fixtures de regresión pasan con `Test-BannerlordImportAutomation.bat` (ambas compilaciones: cero advertencias/errores). Las colisiones con nombres existentes en destino requieren revisión manual en Resource Browser, ya que el helper no inventaría los recursos instalados. La UI del Editor y el envío real siguen sin verificarse porque el entorno denegó iniciar el ejecutable del helper.
- Endurece el helper FlaUI mantenido de Resource Browser: solo fuentes `.fbx`/`.png` del primer nivel, tope de 100 archivos, destino explícito por nombre visible del módulo, resolución de ruta única `módulo > Assets` y detención ante estado UI incierto, sin reintentos. Añade pruebas sin abrir el Editor y marca el proyecto duplicado antiguo como referencia. No se reconstruyeron los ZIP 22.0.0 existentes; `SUBMITTED` no afirma compilación TPAC ni éxito en tiempo de ejecución.
- Corrige la auditoría estructural TPAC para usar la cabecera v2 de 36 bytes y limitar el rango de la tabla; las versiones desconocidas se marcan como no compatibles sin leer campos propios de v2.
- Añade selectores MVVM de archivo y carpeta para herramientas Desktop que requieren una entrada local, con etiquetas traducidas y conservación de la entrada al cancelar.
- Añade una comparación de solo lectura entre inventarios TPAC de TpacTool, empareja activos por GUID y exporta altas, bajas y cambios sin sobrescribir las entradas.
- Añade `Plan-CalradiaForge-AssetBatch.bat`, un inventario acotado y de solo lectura del árbol de fuentes: calcula hashes de candidatos conocidos, marca extensiones no verificadas, señala nombres base repetidos para revisión y exporta JSON atómico con respaldo al reemplazar. No genera sidecars `.meta`, no invoca compiladores ni afirma que una importación haya terminado.
- Actualiza el informe del planificador a esquema v4 con disponibilidad explícita del generador, etapas manuales o bloqueadas, lectura acotada y segura de `SpriteData`, mapeo atlas/categoría/hoja y observaciones breves de duración y memoria del proceso. Su alcance es explícito; asignaciones, rendimiento del generador/juego y reducción de cierres siguen sin medirse. Las rutas TPAC predichas siguen sin verificarse; el planificador nunca inicia el generador ni Resource Browser.
- Añade una revisión de evidencia de la infografía de automatización de recursos generada por IA. Conserva el flujo documentado de sprites e importación y excluye transformaciones `.meta`/`.fbs` no admitidas, flags de compilación supuestos, redirección de stdin, inyección Roslyn y cifras de rendimiento/cierres sin sustento.
- Añade mediciones breves observadas de la duración del inventario y del pico de memoria de vida del proceso PowerShell, con alcance explícito; asignaciones, generador, juego y cierres siguen sin medirse.
- Integra el punto de entrada C# proporcionado para `BannerlordImportAutomation`, documenta la inspección de solo lectura y los límites del envío experimental optativo, y registra los avisos MIT de FlaUI para Source-SDK. Su prueba BAT ahora compila y pasa sin abrir el Editor.
- Aclara que el flujo Bannerlord inspeccionado no documenta un importador FBX masivo en C# ni un contrato de sidecars `.meta`. El flujo existente para atlas sigue siendo SpriteSheetGenerator más importación en Resource Browser.
- Añade una revisión de las fuentes, afirmación por afirmación, del informe de automatización de recursos. Confirma carpetas documentadas, nombres LOD/materiales, atlas UI y NativeTextureExporter; marca como no verificados o no respaldados los flags CLI supuestos del generador, la importación FBX con Reflection/Harmony, los ejes universales, el diseño binario TPAC y los umbrales de memoria del editor.
- Conserva intactos los TPAC fuente e instalado de Steam. Sus límites de cabecera y tabla pasan, pero el lector TpacTool disponible no puede leer el GUID del activo; los metadatos siguen sin verificarse. Estas correcciones aún no se empaquetaron en los archivos existentes.
- Añade el analizador acotado y de solo lectura FBX ASCII a la sección Assets de Desktop, además del `.bat` para consola. Informa indicios de nombres LOD, nombres de materiales declarados y archivos binarios/no compatibles sin importar recursos. Los ZIP 22.0.0 existentes no se reconstruyeron.
- Verifica la infografía/PDF de recursos actualizada 1.2 contra las guías de nombres y sprites de TaleWorlds y la guía comunitaria de importación FBX. El analizador Core de Desktop respeta la cancelación durante el recorrido/análisis; el BAT de consola se puede interrumpir desde su ventana. Ambos reportan nodos Camera/Light declarados para revisión humana. No reescriben FBX, convierten ejes con reglas supuestas, usan flags ocultos del generador ni automatizan la importación en el editor. Los ZIP no se reconstruyeron.
- Mejora la trazabilidad de la comparación de inventarios TpacTool: normaliza a UTC la fecha de captura de cada informe, la conserva en el JSON anterior/posterior, la muestra en el CLI `.bat` y la valida antes de comparar. Añade un modo estricto optativo para que la falta del lector o su bloqueo por Windows no se confundan con una auditoría local completa. Los ZIP 22.0.0 existentes no se reconstruyeron.
- Endurece la validación del diff TPAC contra campos anidados ausentes/nulos, enteros sin signo inválidos, tablas v2 vacías o fuera del archivo, saltos de línea finales y el desplazamiento desconocido `-00:00` de RFC 3339. Los fixtures exigen rechazos con mensajes accionables; los ZIP existentes siguen intactos.
- Alinea la validación de informes externos con el contrato del lector: `assets` debe ser un arreglo JSON y el tamaño declarado no puede superar 128 MiB. Los fixtures rechazan un objeto escalar y un tamaño un byte por encima del límite; Windows aún bloquea el comportamiento real del lector.
- Refina el panel Gauntlet nativo como una mesa de guerra, con rail vertical para las ocho rutas, contexto de sección, tarjetas de estado, command deck, ledger de evidencia sin textura y filas inferiores de acciones y paginación. Conserva las rutas, los comandos, la propiedad del ViewModel y las protecciones del estado del juego; asigna IDs de control únicos a los dos campos `@Argument`. Añade PNG fuente decorativos, deterministas y de baja opacidad (`forge_header_filigree` y `forge_pine_grain`) y registra sus referencias en SpriteData y el prefab. Pasan el validador estructural de sprites fuente y las pruebas breves de Core/ForgeWeave; siguen pendientes la reconstrucción de atlas/TPAC en Resource Browser y la inspección de renderizado en vivo. No cambia la versión ni se reconstruyen los ZIP.

## 22.0.0 — 22 de septiembre de 2026 — auditoría estructural de sprites Gauntlet

- Amplía el analizador de recursos para validar tamaños e IDs de hojas de `SpriteData`, referencias, rutas de origen seguras y rectángulos de piezas dentro de los límites del atlas.
- Añade comprobaciones acotadas de firma e IHDR PNG para piezas y atlas generados, con hallazgos explícitos por archivos ausentes, cabeceras incorrectas y dimensiones distintas. No decodifica píxeles ni verifica CRC de PNG.
- Extiende el validador reproducible de recursos Game-icons con controles de referencias y dimensiones, conservando atribuciones y recursos generados.
- Refuerza la evidencia de preparación TPAC: el analizador lee la cabecera fija v2 de 36 bytes y rechaza cabeceras truncadas, conteos cero/excesivos y rangos de tabla fuera del archivo. Las otras versiones se marcan como no compatibles sin aplicar offsets v2.
- Aclara que una cabecera fija plausible no valida el contenido; el hallazgo dirige al preflight opcional de metadatos TpacTool y a los pasos de recuperación de Bannerlord Resource Browser.
- Convierte los fallos del preflight en diagnósticos breves con hash y acción de recuperación; la recolección detenida no modifica el TPAC fuente y conserva el motivo del parser sin volcar una traza de PowerShell.
- Mantiene la evidencia TPAC limitada a la estructura acotada de cabecera/tabla y al preflight SHA-256; TpacTool sigue siendo una herramienta de inspección opcional que no se redistribuye.
- Excluye respaldos temporales con marca de tiempo de los ZIP Modules y Source-SDK y hace que la auditoría rechace su inclusión accidental.
- Añade fixtures de sprites válidos e inválidos y genera las tres distribuciones 22.0.0 por separado.

## 21.0.1 — 22 de septiembre de 2026 — diagnóstico de sprites Gauntlet y preflight TPAC

- Refuerza el auditor de sprites Gauntlet: compara las categorías de carga permanente y `SpriteData.xml` con el número de atlas generados; además distingue paquetes ausentes, vacíos, ilegibles, con marcador inválido y con marcador TPAC. La coincidencia de cuatro bytes se informa como evidencia estructural limitada, nunca como prueba de que el contenido sea válido o cargable.
- Añade el preflight de solo lectura `Inspect-CalradiaForge-Tpac.bat`, que muestra tamaño y SHA-256 antes de abrir opcionalmente TpacTool local. Avisa que la versión upstream 0.4.0 apunta a Bannerlord 1.8.0 beta y su uso con este proyecto 1.4.8 no está verificado; no redistribuye TpacTool.
- Añade fixtures para marcadores válidos e incorrectos y corrige la guía de recursos para reflejar el TPAC importado en el módulo fuente y el paquete de distribución.
- Actualiza metadatos de versión y manifiestos a 21.0.1; conserva las funciones de 21.0.0 y las tres distribuciones separadas: Modules, Source-SDK y Desktop.
- Regenera y audita los tres ZIP 21.0.1 después de pasar las suites breves por `.bat`, DocFX/ayuda/iconos, localización nativa y Desktop, y el preflight TPAC de solo lectura.

## 21.0.0 — 22 de septiembre de 2026 — extensiones Gauntlet, ensamblados dentro del juego y ayuda contextual

- Añade al SDK los metadatos públicos `[ForgeUiPage]` y `[ForgeUiCommand]`, registro limitado al ensamblado indicado por `ForgeApi.AutoRegister`, validación de prefabs propios y enlaces `Command.Click`, aislamiento de errores por página, limpieza por propietario y una capa Gauntlet independiente abierta desde Extensions.
- Comprueba contextos de página y comandos al abrir; las declaraciones que modifican estado requieren modo de pruebas y, en campaña, confirmación de una copia. Las solicitudes de apertura y cierre se ponen en cola para el hilo del juego. Los atributos no aíslan el código de extensiones.
- Integra AsmResolver 6.0.1 en el módulo net472 de Bannerlord para inspeccionar metadatos PE/.NET acotados en segundo plano. Añade vista previa/aplicación explícita de cambio de versión, rechaza entradas firmadas y mixtas, preserva el original, crea respaldo y salida separados, registra hashes y valida el resultado.
- Añade al módulo SVG originales atribuidos de Game-icons.net, PNG transparentes para sprite parts, metadatos, una categoría de carga permanente y el TPAC de ejecución generado por Resource Browser. Una comprobación breve y directa del menú del Modding Kit en Bannerlord 1.4.8 confirmó que el prefab muestra sus iconos semánticos sin el aviso de textura ausente.
- Añade `Build-CalradiaForge-UiAssets.bat` para ejecutar y validar el generador oficial de sprites de Bannerlord para un módulo, junto con **Gauntlet Sprite Package Auditor** en Desktop para detectar atlas fuente ausentes, TPAC faltantes, salidas vacías y límites de análisis.
- Añade `Prepare-CalradiaForge-ResourceBrowser.bat` para validar y preparar el atlas y sus metadatos en el módulo instalado, y luego recoger el TPAC compilado en el árbol fuente. Verifica hashes, respalda archivos reemplazados y avisa si difieren las versiones sin copiar DLL ni manifiestos. El TPAC importado forma parte del módulo fuente 21.0.0.
- Amplía el hallazgo del auditor de sprites de Desktop con los pasos de preparar/importar/recoger y la regla explícita de separar DLL y manifiestos si las versiones difieren.
- Refina el escritorio y las definiciones Gauntlet nativas con iconos de Game-icons con significado: escritorio agrupa Crear, Inspeccionar y Verificar en la cabecera, asigna un icono de área a cada herramienta y marca evidencia y estados con símbolos correspondientes. La comprobación breve dentro del juego confirmó que Gauntlet encuentra la categoría importada.
- Mueve los pinceles completos de cada tema de escritorio al diccionario de paleta propio y cambia de tema sustituyendo el diccionario entero. Corrige colores de texto obsoletos y elimina diccionarios duplicados de paleta e idioma al alternar varias veces.
- Mantiene Recientes limitado a doce entradas en un viewport desplazable independiente y añade comprobaciones WPF de colores efectivos, disponibilidad de iconos en todas las rutas y unicidad de diccionarios.
- Añade generación de API con DocFX al compilar y resúmenes de ayuda offline en los 13 idiomas derivados de la documentación XML del SDK.
- Actualiza el empaquetado de módulos para incluir dependencias de AsmResolver, licencias, atribución de iconos y fuentes de sprite; actualiza auditorías y scripts de release para 21.0.0.
- Añade pruebas enfocadas al registro UI y al banco de ensamblados. Ejecuta un arranque breve y directo del Modding Kit, abre y cierra Forge en el menú principal y confirma los nueve iconos; no incluye campañas, batallas ni pruebas largas.

## 20.0.0 — 2026-09-22 — iconos vectoriales, páginas declarativas y herramientas de ensamblados

- Sustituye iconos dependientes de fuentes y categorías con emoji por geometrías WPF locales y recoloreables. El encabezado adapta el contenido y recorta el título antes de solaparlo.
- Incluye SVG seleccionados de Game-icons.net, generación reproducible de geometrías y atribución por icono, artista, fuente, licencia CC BY 3.0 y modificaciones.
- Conecta `[ForgeUiPage]` y `[ForgeUiCommand]` a un catálogo validado una sola vez: regiones, claves, ranuras, enlaces `ICommand` y cancelación. XAML conserva el diseño y el catálogo no recorre controles.
- Añade inspección de metadatos .NET con AsmResolver 6.0.1 solo en Desktop; el ensamblado inspeccionado no se ejecuta ni se carga en el CLR.
- Añade un editor optativo en dos pasos: la vista previa no escribe archivos; aplicar crea otra salida y respaldo `.source.bak`, registra hashes SHA-256 y valida el PE. Rechaza ensamblados firmados.
- Añade DocFX 2.77.0 como herramienta fijada para generar un sitio estático local desde Markdown en inglés y comentarios XML del SDK.
- Retira dependencias de símbolos WPF UI y MahApps que ya no se utilizan. AsmResolver queda solo en Desktop; los módulos mantienen sus dependencias actuales.
- Agrega pruebas WPF breves para inspección, vista previa sin escritura, copia/respaldo, versión, conservación del original y rechazo de ensamblados inválidos. No se ejecutaron pruebas largas, campañas ni batallas.

## 19.0.0 — 22 de septiembre de 2026 — integración de toolkits para Desktop

- Añadidos adaptadores fijados de CommunityToolkit.Mvvm 8.4.2 para estado observable, comandos parametrizados y trabajo asíncrono cancelable, conservando los contratos de páginas enrutadas.
- Añadidos recursos de MaterialDesignThemes.Wpf 5.3.2, símbolos y controles de WPF UI 4.3.0 y glifos Material de MahApps.Metro.IconPacks 6.2.1 en Desktop. Las cuatro librerías solo afectan Desktop; los módulos de Bannerlord siguen siendo independientes.
- Sustituidos los glifos de ejemplo de cabecera y rail por controles respaldados por los toolkits, manteniendo la paleta táctica, los viewports limitados de recientes/fijadas, el foco de teclado y la evidencia.
- Fijadas las versiones de paquetes y documentadas las licencias, URL y atribuciones transitivas en `THIRD_PARTY_NOTICES.md`.
- Publicados los tres ZIP de 19.0.0 después de comprobaciones cortas de compilación, MVVM, recursos WPF, localización y paquetes. No se ejecutaron campañas, batallas, resistencia ni pruebas largas del juego.

## 18.0.0 — 22 de septiembre de 2026 — rail operativo por secciones

- Reorganizado el rail izquierdo de Desktop en secciones contraíbles explícitas con glifos, contadores, estado de expansión, desplazamiento independiente y separación clara entre fijadas, recientes y áreas de trabajo.
- Añadidos búsqueda y filtro por área sin recorrer el árbol visual; cada ruta conserva identificador, comando, entrada, compuerta y resultado de evidencia.
- Añadido un viewport acotado para las secciones y encabezados visibles por teclado para mantener navegable el catálogo entre 100 y 200%.
- Añadidos catálogos de recursos de 54 claves con ayuda localizada para desplazar secciones en los 13 idiomas de Desktop; identificadores, rutas, JSON y evidencia técnica permanecen sin traducir.
- Corregida la clasificación de herramientas de Política, Economía y Combate, conservando temas, IPC, ForgeWeave, evidencia y liberación de páginas.
- Publicados los tres ZIP auditados de 18.0.0 tras las comprobaciones cortas de Core, ForgeWeave, Desktop, WPF, localización y paquetes. No se ejecutaron campañas, batallas, resistencia ni muestras largas de rendimiento.

## 17.0.0 — 22 de septiembre de 2026 — Mesa de guerra MVVM táctica

- Sustituidas las líneas arbitrarias del encabezado por marcas vectoriales derivadas de Lucide: mapa, brújula, espadas, bandera, objetivo, fijado, recientes y escudo. Las geometrías quedan acotadas a sus secciones y conservan la lectura entre 100 y 200%; la atribución está en `THIRD_PARTY_NOTICES.md`.
- Añadidos tres temas reversibles para Desktop: Mesa de guerra (predeterminado), Pergamino claro y Alto contraste. El tema y el idioma se guardan de forma atómica en `%LocalAppData%\CalradiaForge`; una preferencia dañada se recupera sin tocar las partidas.
- Añadidos nombres localizados, descripciones de contraste, errores y ayuda del selector en los 13 diccionarios de Desktop. Cambiar de tema conserva página, comando, recientes, favoritas y evidencia retenida.
- Ampliadas las marcas vectoriales sin dependencias derivadas de Lucide para tema, conexión, evidencia, advertencia, fijado y recientes; la atribución permanece en `THIRD_PARTY_NOTICES.md`.
- Ampliado el render WPF a 20 rutas representativas con 3 temas y 4 escalas, conservando la pasada completa de 190 rutas con Mesa de guerra.
- Rediseñado Desktop como un shell MVVM sin dependencias, con páginas bajo demanda, grupos, favoritas, recientes, paleta de comandos, estado de sesión, command deck y ledger de evidencia limitado.
- Añadidos los atajos `Ctrl+K`, `Ctrl+F`, `Ctrl+1…9`, `Ctrl+Enter` y `Esc`; al cambiar de herramienta se libera la página anterior y la evidencia queda retenida en el shell.
- Reorganizada la jerarquía WPF en cinco zonas contenidas y tokens tácticos dinámicos. El sello centrado y las reglas de evidencia no atraviesan texto técnico ni controles.
- Extendidos los 13 catálogos de recursos con claves del banco de trabajo y la paleta; identificadores, rutas, código generado y evidencia sin procesar permanecen sin traducir.
- Limitados Favoritas y Recientes a viewports independientes con desplazamiento automático, contadores visibles, estados vacíos, foco de teclado y un máximo de 12 recientes para que el rail no crezca indefinidamente.
- Añadidas marcas vectoriales sin dependencias derivadas de Lucide para el historial reciente; la atribución ISC está en `THIRD_PARTY_NOTICES.md`.
- Actualizados metadatos, preflight de Steam, documentación, recursos nativos, pruebas y los tres archivos ZIP auditados de 17.0.0. La validación sigue siendo corta y no ejecuta campañas, batallas, resistencia ni muestras largas.

## 14.4.1 — 22 de septiembre de 2026 — corrección de arranque y enfoque de evidencia

- Corregido el cierre al iniciar: el campo de resultado ahora usa un enlace de solo lectura hacia RawResult, conservando selección de texto y actualizaciones.
- Incorporada una prueba real de ventanas y plantillas WPF: 190 rutas y 52 combinaciones de idioma/escala. El empaquetado se detiene si falla.
- Agregadas cuatro tarjetas tácticas al panel del juego: contexto, modo de pruebas, evidencia retenida y pruebas registradas.
- Nuevo botón Enfocar evidencia / Mostrar herramientas: amplía el informe y su texto sin perder sección, página ni argumento. Incluye encabezado y contador de páginas; una regla limpia reemplaza los trazos aleatorios.
- Framework y Extensiones vuelven a tener navegación visible. Los argumentos se actualizan al escribir y los dos nuevos controles tienen traducción a los 13 idiomas.
- Se conservan los tres ZIP separados y el lanzador BAT. El lanzador del proyecto usa la carpeta Desktop publicada, evitando compilaciones Debug antiguas.

## 14.4.0 — 21 de septiembre de 2026 — recursos dinámicos y transporte de sesión

- Se regeneran los 13 diccionarios WPF de Forge desde las claves en inglés. El cambio de idioma sustituye solo el diccionario de Forge y deja intactas evidencia, rutas e identificadores.
- Los colores y estados de controles restantes pasan a recursos dinámicos; los encabezados de plantillas usan recursos parametrizados.
- Se agrega transporte cancelable y medido para conexión y reconexión por named pipe. La validación incluye paridad de recursos y matriz estructural de 13 idiomas por cuatro escalas.

## 14.3.0 — 21 de septiembre de 2026 — mesa de trabajo de escritorio enrutada

- La aplicación activa deja de descubrir herramientas desde el TreeView y de cambiar visibilidad por cascadas. Usa 190 definiciones revisadas, páginas temporales enrutadas, comandos cancelables y plantillas WPF ContentControl.
- Análisis, generación, consultas de sesión, mediciones breves, navegación y exportación se separan en servicios sin controles WPF.
- Las salidas generadas se identifican como plantillas editables; rutas no compatibles, desconectadas, que modifican estado o sin entrada muestran límites explícitos.
- La superficie táctica se rehízo con un sello de mando centrado y sin reglas decorativas que crucen controles o texto.

## 14.1.0 — 21 de septiembre de 2026 — shell interno MVVM de comandos

- Se agregan `ObservableObject`, comandos síncronos, comandos asíncronos cancelables, estado visible y un catálogo único de herramientas sin dependencias externas.
- Todos los identificadores de navegación pasan por el catálogo y la ruta de orden de trabajo seleccionada. La orden activa se carga mediante una plantilla `ContentControl` de WPF mientras las implementaciones verificadas compatibles conservan su ruta establecida.
- ForgeWeave conserva su búfer de 128 registros, el presupuesto de 16 eventos por despacho, la protección de replay, las compuertas de escritura y el aislamiento de fallos.

## 14.0.0 — 21 de septiembre de 2026 — fiabilidad y herramientas verificadas

- Se unificó el valor de versión en ensamblados, manifiestos, escritorio, informes, documentación, pruebas y nombres de archivos.
- Se añadió el contrato de análisis, el registro limitado de `IForgeAnalyzer` para el SDK, evidencia de archivo/línea, procedencia y estados explícitos `No ejecutado` y `No compatible`.
- Las auditorías visibles de escritorio ahora analizan archivos locales para módulos, XML, idiomas, Gauntlet, reglas C#, recursos, registros, metadatos de fallos y archivos ZIP.
- Los servicios de tiempo de ejecución que no existen informan capacidad y devuelven errores accionables; ya no usan resultados vacíos simulados.
- Se renovó la mesa táctica y el encabezado nativo con un sello de mando centrado y reglas acotadas; se eliminaron rombos, flechas y líneas que cruzaban títulos.
- El escritorio resuelve los trece idiomas de Bannerlord desde catálogo y los recursos nativos se regeneran en UTF-8 con BOM e IDs estables.
- El empaquetado genera solamente Modules, Source-SDK y Desktop. Desktop se publica sin ejecutable anfitrión EXE.
- Se añadió una prevalidación de Bannerlord de solo lectura y una ruta de inicio mediante Steam, sin reescribir Steam, BLSE, configuración ni partidas.

## 0.7.6 — 16 de septiembre de 2026 — intersección exacta del disco de mando

- El disco de mando queda exactamente en la intersección 34/34 de las líneas táctica horizontal y vertical del rombo.
- La suite de regresión de escritorio pasa de 10 a 24 casos cortos de transporte, reconexión, errores, localización, escalado, superficies de evidencia y selección de Replay Lab; la matriz visual cubre todos los idiomas soportados por Bannerlord y las escalas 100 % y 200 %.

## 0.7.5 — 16 de septiembre de 2026 — corrección óptica del disco de mando

- El disco de mando del escritorio se desplaza media unidad de diseño a la izquierda y abajo después del redondeo del trazo del escudo; corrige la desviación óptica observada a alta densidad de píxeles y conserva la retícula táctica común.
- Se repiten comprobaciones acotadas de ForgeWeave, Core/SDK, escritorio, recursos generados y paquetes; no se incluyen pruebas de resistencia ni muestreo prolongado dentro del juego.

## 0.7.4 — 16 de septiembre de 2026 — navegación táctica a gran escala

- La identidad de navegación de escritorio usa ahora la misma escala de cromo compacto que la cabecera; su etiqueta de sección y regla de formación aumentan y se redistribuyen con la opción de texto al 200 %.
- Se refina el disco de mando de la marca con diez unidades y centro compartido en la retícula 34/34 del rombo, para mantener un centro óptico claro a alta densidad de píxeles.
- La regla de formación sigue dentro de la columna de navegación escalada y conserva su función decorativa, sin recibir interacciones.

## 0.7.3 — 16 de septiembre de 2026 — alineación de identidad táctica

- Se reconstruye la marca del escritorio sobre un lienzo vectorial único de 68 unidades: el rombo de mando, la retícula y el punto de latón comparten el centro exacto; la cinta CF conserva su campo inferior independiente.
- El bloque completo de identidad escala con el cromo compacto al 200 %, junto con una cabecera de altura correspondiente, para mantener el sello alineado con el título y fuera del área de trabajo.
- Se sustituyen las diagonales anteriores por una regla de despliegue limitada: segmentos separados, tres marcadores de formación y remates que leen como notación de mapa táctico sin cruzar identidad, título o placa de sesión.
- La decoración de extremos de la barra lateral pasa a ser una regla de formación contenida, con marcadores y remates visibles al 100 % y al 200 %.

## 0.7.2 — 16 de septiembre de 2026 — renovación táctica de la interfaz

- Se sustituye el rombo CF saturado por un escudo heráldico centrado, un rombo de mando, un punto de latón y una cinta CF independiente.
- La cabecera usa una única línea de batalla sobria detrás del marco y los rombos de los extremos de navegación quedan alineados con su regla.
- Se conservan el foco de teclado, el cambio de idioma, el escalado al 200 % y los tokens carbón, pino, verdín y latón.

## 0.7.1 — 16 de septiembre de 2026 — catálogos de idiomas del escritorio

- El selector de la aplicación de escritorio ahora incluye los 13 idiomas presentes en los paquetes instalados de Bannerlord: inglés, español latinoamericano, portugués brasileño, alemán, francés, italiano, polaco, ruso, turco, chino simplificado, chino tradicional, japonés y coreano.
- Core integra catálogos de escritorio generados a partir de las traducciones revisadas del menú nativo y conserva el inglés como respaldo explícito para el texto técnico sin traducción revisada.
- Se añaden comprobaciones de cobertura para cada idioma y se conserva el flujo de código en inglés, la interfaz táctica escalable y el paquete separado de escritorio.

## 0.7.0 — 16 de septiembre de 2026 — filtros de eventos ForgeWeave y SDK v3

1. Se añade `ForgeEventFilter.RequiredData`, un filtro declarativo de coincidencia exacta sobre datos escalares copiados del anfitrión. Un filtro vacío coincide con todos los eventos; una discrepancia se registra como `Filtered` y no invoca ni autoriza el manejador.
2. El mismo filtro se aplica al despacho vivo y a Replay Lab. Forge copia la declaración al registrarla, la muestra en la salud de Framework, el panel nativo y los informes HTML, y la limita a ocho pares, claves de 64 caracteres y valores de 512.
3. Se añaden presupuestos de ejecución por manejador de 1 a 5.000 ms. `Warn` registra evidencia `OverBudget` sin abortar ni poner en cuarentena código de extensión; `Ignore` conserva la duración sin crear un hallazgo de presupuesto.
4. Se agregan pruebas focalizadas del SDK/Core para copias inmutables, coincidencias, discrepancias, replay filtrado, excesos de presupuesto y declaraciones malformadas o demasiado grandes.
5. Se actualizan el contrato público del SDK a v3 y la vista preliminar a 0.7.0, junto con el ejemplo incluido, manifiestos, documentación y archivos separados de publicación.

## 0.6.5 — 16 de septiembre de 2026 — decoración limpia de interfaces

1. Se sustituyen los trazos decorativos dispersos de la cabecera nativa por una composición fija de cuatro zonas: sello Forge, identidad, placa de sección/contexto y placa de compilación.
2. Se centra el sello CF sobre un eje común, con inserto, monograma, regla superior y subrayado de latón medidos para conservar claridad en tamaños pequeños.
3. Se añaden reglas alineadas de latón y verdín, placas interiores y espaciado controlado, conservando la barra de ocho secciones y el control Cerrar.
4. Todos los widgets decorativos son pasivos y quedan dentro de límites definidos para que ninguna línea cruce el logo, el texto, la versión o el área de clic.
5. Se sincronizan manifiestos, ensamblados, recursos nativos generados, metadata, documentación y los tres ZIP separados de 0.6.5.

## 0.6.0 — 15 de septiembre de 2026 — ForgeWeave Replay Lab y mesa táctica

1. Se añade Replay Lab, una función de verificación controlada de eventos para ForgeWeave. Forge conserva evidencia acotada y copiada de eventos del anfitrión y puede repetir una sola secuencia seleccionada; no acepta datos inyectados, no registra un replay como nueva fuente y no reintenta un rechazo automáticamente.
2. Se agregan la aceptación explícita `ForgeReplayMode` por manejador, `ForgeEvent.IsReplay`, `SourceSequence` y el registro aditivo `ForgeApi.Replays`. `Disabled` sigue siendo el valor inicial; el contexto actual debe coincidir exactamente y los manejadores de escritura conservan las protecciones de descriptor, modo de pruebas y copia de campaña.
3. Se añade la acción de pipe `replay`, junto con estado y motivo de rechazo, resultados copiados por manejador, tiempos, registros de origen y resultados recientes en la instantánea Framework, diario de eventos e informes JSON y HTML.
4. El Marco nativo acepta una secuencia conservada en su campo de argumento existente, y la tabla Framework de escritorio repite la fila de evidencia seleccionada.
5. Ambas interfaces se actualizan como una mesa táctica: se conservan las ocho secciones nativas mientras Marco se vuelve un área de trabajo principal; las tiras de orden de trabajo, mazos de comandos y filas de libro mayor usan carbón, pino, verde templado, latón, verdín y brasa. La cabecera nativa ahora tiene un solo sello Forge, una placa alineada de orden/contexto y una placa de compilación separada; se eliminan los trazos pseudoaleatorios.
6. ForgeWeave sigue independiente de Harmony, MCM, ButterLib y recursos externos. Replay Lab no es intercepción de métodos ni afirma sustituir de forma universal a un framework de parches.
7. Se añaden regresiones focalizadas de replay, informes, protocolo y selección de escritorio, y solo se ejecutan la suite corta y una sesión breve de menú principal. Las comprobaciones de campaña, batalla, cambios de estado y resistencia quedan fuera de esta versión.

## 0.5.0 — 15 de septiembre de 2026 — framework cooperativo ForgeWeave

1. Se añade ForgeWeave, el framework original e independiente de Calradia Forge para extensiones cooperativas. Usa solo ciclos de vida que Forge ya posee; no descubre métodos, emite IL, reemplaza callbacks ni carga otro framework de mods.
2. Los autores cuentan con eventos explícitos de `ForgeReady`, pantalla, contexto, campaña, misión, agentes y `Pulse` acotado, con datos escalares copiados en lugar de objetos vivos de Bannerlord o servicios de pruebas de Forge.
3. La ejecución de manejadores es determinista e inspeccionable: prioridad y restricciones `Before` y `After` del mismo evento y prioridad forman un plan explícito. Las declaraciones ausentes, entre eventos, entre prioridades, autorreferentes o cíclicas se bloquean con hallazgos, sin adivinar un orden.
4. Los manejadores que pueden escribir reutilizan las protecciones de contexto, modo de pruebas y copia de campaña de Forge. Cada error se aísla por manejador, los fallos repetidos generan cuarentena y se conserva un historial acotado de tiempos, salud de eventos y despachos.
5. Se añaden las acciones de protocolo `framework` y `event-journal`, la acción Marco en el panel nativo, la sección Marco en la aplicación de escritorio y la misma instantánea copiada de ForgeWeave en informes JSON y HTML.
6. Se añade un manejador de ejemplo de solo lectura para `ForgeReady`, documentación de ForgeWeave primero en inglés y documentación complementaria en español para autores de extensiones.
7. El Atlas de parches de Harmony opcional deja de ser el flujo principal del panel y de la aplicación. Permanece como diagnóstico histórico aislado, acotado y solo de lectura cuando otro módulo seleccionado ya aporta un runtime compatible; ForgeWeave no lo usa ni lo requiere.
8. La prevalidación de Patch Blueprint sigue independiente de observación de Harmony. Revisa estructuralmente declaraciones inertes de autores y nunca aplica, quita ni reordena un parche.
9. Se agregan las ayudas atómicas `RegisterWhenAvailable` y `UnregisterWhenAvailable` del SDK para que la carga de un módulo no pierda Forge entre comprobar el registro y suscribirse después a disponibilidad.
10. `ContextLeaving` conserva el contexto lógico de salida, por lo que los manejadores elegibles de limpieza y solo lectura se ejecutan contra el contexto que abandonan mientras se mantienen las protecciones existentes para escritura.

## 0.4.0 — 15 de septiembre de 2026

1. Calradia Forge se mantiene independiente: el mod no requiere ni referencia Harmony, MCM, ButterLib u otro framework de mods, y la auditoría rechaza esas DLL de los ZIP de módulos y desarrolladores.
2. Se añade el Atlas de parches de Harmony, opcional y solo de lectura. Si otro módulo ya cargó una API compatible, muestra de forma limitada el método original, propietarios, Prefix/Postfix/Transpiler/Finalizer, prioridad declarada, reglas before/after, índice y método de parche. La ausencia o incompatibilidad de Harmony no afecta Forge; el Atlas no carga, agrega, quita ni reordena parches.
3. El Atlas aparece en Módulos/Dependencias del panel, el flujo en vivo de escritorio, la negociación del protocolo, los informes sin conexión y la exportación HTML. Los destinos compartidos se presentan como candidatos de revisión, no conflictos confirmados.
4. Un error de una extensión al anunciarse ya no cierra el ciclo de interfaz: Forge conserva registros sanos y guarda una advertencia.
5. Se completa la superficie declarada de acciones del ayudante de inspección y se mejora el contraste de selección por teclado en el panel nativo.
6. Nuevas pruebas cubren ausencia o API incompatible de Harmony, sobrecargas, propietarios, orden, filtros, límites, metadatos defectuosos, escape HTML y tolerancia a errores de extensiones.

## 0.3.0 — 14 de septiembre de 2026

1. Rediseño de ambas interfaces con verde oscuro, marcos dorados, superficies de pergamino y ornamentos originales reproducibles.
2. Aplicación de escritorio con navegación permanente, estado de conexión visible, acciones por sección y resultados que conservan su espacio al aumentar el texto. Los resultados vacíos incluyen instrucciones y conservan el JSON original en Datos sin procesar.
3. Panel del juego con barra lateral de ocho secciones, botones planos y estilos propios. El resumen separa contexto, modo de pruebas, pruebas registradas y datos de sesión. Los diagnósticos usan texto sin sombras pesadas. La barra se dibuja delante y los contenedores decorativos no reciben clics. El recorrido con Tab omite controles y grupos ocultos o desactivados. La búsqueda comienza vacía. El clic de Tests en el carril final instalado quedó registrado en la validación nativa.
4. Presentación legible de resultados de pruebas y mensajes conocidos, conservando intactos los identificadores, rutas y datos técnicos. Se amplían las traducciones de ayuda para registros vacíos.
5. Comprobaciones automáticas de catálogos, identificadores de traducción, manifiestos, enlaces entre el panel y su código e invariantes del diseño de entrada nativo.
6. Versión 0.3.0 sincronizada en ensamblados, informes, interfaces y manifiestos. Nuevo icono de Windows y portadas actualizadas.
7. ZIP separados para módulos, aplicación de escritorio y código fuente con SDK. El ejecutable de escritorio solo se incluye en su paquete independiente.

El inglés sigue siendo el idioma de origen. El panel utiliza automáticamente el idioma de Bannerlord y recurre al inglés cuando falta una traducción. Esta versión conserva las bibliotecas compartidas y las herramientas del SDK incorporadas en 0.2.0; no afirma sustituir Harmony.

Consulta VALIDATION.md para distinguir las pruebas automáticas, la ejecución dentro del juego, la confirmación visual del usuario y las comprobaciones pendientes. El historial completo en inglés está en CHANGELOG.md.



## Seguimiento de fuentes 22.0.0 — 25 de septiembre de 2026 (Rev033)

- Rev033 registra el ajuste de Alto contraste posterior a Rev032. El tema conserva sólidos sus pinceles de superficie y permite mostrar superposiciones decorativas pasivas cuando los acentos decorativos están activados; el enlace de visibilidad WPF sigue condicionándolas por la opción de acentos y el tema seleccionado. Las ilustraciones fuente usan baja opacidad y no reciben hit testing. Aún no se ha confirmado la legibilidad renderizada en toda la composición y matriz de escalas; la validación final de render y en vivo sigue pendiente. Esta entrada no afirma que se hayan ejecutado pruebas ni una comprobación dentro del juego.

## Seguimiento de fuentes 22.0.0 — 25 de septiembre de 2026 (Rev034)

- Se ajusta Alto contraste para mostrar texturas decorativas pasivas solo cuando la opción de acentos lo permite; los fondos y pinceles de superficie del tema siguen siendo sólidos. El motivo heráldico de la barra superior tiene ahora una altura explícita de 2 DIP para evitar que se colapse por falta de tamaño deseado.
- La validación por archivos `.bat` terminó con compilación sin advertencias, pruebas Desktop 51/51 y render WPF con 273 casos y 150 pases de layout. La prueba de determinismo de texturas pasó; el paquete mide 7.278.592 bytes comprimidos y 15.778.992 bytes decodificados.
- Cinco mediciones del render harness dieron 5.260, 5.680, 5.162, 5.338 y 5.205 ms, con mediana de 5.260 ms. Son tiempos del harness, no mediciones de latencia de la aplicación en ejecución. Frente a la mediana histórica completa de Rev030 (5.703 ms), el valor actual es 7,8 % menor. La referencia reciente Rev032 solo tiene cuatro ejecuciones válidas de cinco y una mediana de 5.303,5 ms; frente a esa referencia incompleta, la diferencia actual es de apenas 0,8 %.
- La fase del harness para navegación y filtros de rutas bajó de una mediana de 1.133,2 ms a 810,3 ms (28,5 %). Las demás fases tuvieron resultados mixtos, por lo que esta revisión no afirma una mejora uniforme del rendimiento.
- La inspección de la ventana WPF en vivo mediante UI Automation sigue pendiente; las capturas del harness no la sustituyen.

## Seguimiento de fuentes 22.0.0 — 25 de septiembre de 2026 (Rev035)

- Se corrigieron tres defectos de composición señalados en las capturas. La barra superior ya no superpone un segundo marco heráldico centrado sobre la franja botánica; así se elimina la brújula duplicada y se mantiene la decoración dentro del área del título, fuera de los botones de ventana.
- La ilustración del resumen de herramienta ocupa ahora una columna derecha propia de 270 DIP y conserva su proporción. Ya no se dibuja sobre el título, el propósito ni el estado. La acción de favorito del rail cuenta con una columna fija de 28 DIP y un botón centrado de 26 DIP, para que la estrella permanezca visible junto a títulos multilínea.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` compiló sin advertencias ni errores, pasó Desktop 51/51 y render WPF 273/273 con 150 pases de layout. Se revisaron visualmente las vistas previas de Alto contraste, Mesa de guerra y el tema predeterminado generadas por el harness. Dos ejecuciones del render tardaron 4,804 y 5,200 segundos. Son tiempos del harness, no latencia de interacción ni validación de la ventana en vivo.
- La inspección WPF en vivo mediante UI Automation sigue pendiente. No se afirma ningún cambio de juego, campaña, batalla, ZIP, API o versión.

## Seguimiento de fuentes 22.0.0 — 25 de septiembre de 2026 (Rev036)

- Se regeneró el ornamento botánico de la tarjeta de herramienta desde la fuente de autoría completa con proporción 3:1, a 540×180 píxeles para su espacio de 270×90 DIP. Así dispone de píxeles suficientes hasta la escala WPF del 200 %. El derivado anterior medía 270×90 y recortaba la imagen original antes de deformar su proporción. La tarjeta, la franja botánica superior, el tablero cartográfico y el rail solicitan ahora muestreo de alta calidad; las ilustraciones siguen siendo pasivas y quedan fuera de textos y controles.
- El generador determinista rechaza cambios de proporción y comprueba que el inventario optimizado sea exacto. Se retiraron tres derivados sin referencias de uso del paquete de ejecución, conservando sus fuentes de autoría. Las texturas optimizadas ocupan 7.202.682 bytes comprimidos y 15.334.592 bytes RGBA decodificados: 75.910 y 444.400 bytes menos, respectivamente, que la medición del paquete en Rev034.
- El subtítulo localizado de la cabecera se distribuye en varias líneas en vez de acabar en puntos suspensivos y ofrece el texto íntegro como ayuda emergente. La matriz de render comprueba que permanece dentro de su grupo. Pasó `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check`; `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` compiló sin advertencias ni errores y pasó Desktop 51/51 y render WPF 273/273 con 150 pases de layout. La ejecución final del harness tardó 4.886 ms; esa duración no mide la latencia de la aplicación abierta.
- Se revisaron las vistas previas del harness a escala normal y del 200 %, incluidas Mesa de guerra y Alto contraste. La inspección de una ventana WPF en vivo sigue pendiente. No se alteraron versión, ZIP, recursos del juego, API ni sesión de Bannerlord.

## Seguimiento de fuentes 24.0.0 — 25 de septiembre de 2026 (Rev037)

- Actualiza el contrato público del SDK a la versión 8 con el registro administrado de delegados mediante `ForgeCampaignEvents.SubscribeWeaveWhenAvailable`. Su `ForgeWeaveRegistration` administra la devolución de disponibilidad y el manejador activo, informa `WaitingForHost`, `Registered`, `HostUnsupported`, `Failed` o `Disposed`, anula el registro al desconectarse el anfitrión, se registra otra vez al reconectarse y deja de registrar después de `Dispose()`. El registro es sincrónico en el hilo de conexión del anfitrión; liberar un objeto activo desde otro hilo lanza una excepción y lo deja activo para reintentar en el hilo correcto. `SubscribeWeave` conserva el registro inmediato y lanza `InvalidOperationException` si el anfitrión de eventos Forge no está disponible o la llamada se hace desde otro hilo.
- Acota `ForgeAgentMemory` a 2,048 IDs de agente distintos compartidos entre los niveles, 128 hechos semánticos por agente, 512 entradas episódicas por agente y 128 por tipo, y 128 tareas procedurales por agente. El desbordamiento episódico expulsa las entradas más antiguas mediante FIFO; los datos semánticos y procedurales existentes se pueden actualizar, pero las entradas nuevas se rechazan al llegar a la capacidad. Solo los hechos semánticos admiten TTL. `TryUpsert`/`TryAdd` informan los rechazos por capacidad; los métodos de escritura heredados lanzan `InvalidOperationException` cuando se rechaza una escritura.
- Actualiza las referencias del SDK y ForgeWeave y el codemap del SDK para el árbol de código fuente v24.0.0. Los archivos existentes `CalradiaForge-Modules-23.0.0.zip` y `CalradiaForge-Source-SDK-23.0.0.zip` permanecen sin cambios y no se regeneraron; no se afirma que contengan el producto v24.0.0 ni se declaran hashes de paquetes de esa versión. No se ejecutaron compilaciones, suites de pruebas ni Bannerlord para esta actualización documental y del registro.

## Seguimiento de fuentes 24.0.0 — 25 de septiembre de 2026 (Rev038)

- Validación posterior a Rev037: `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` compiló las dependencias seleccionadas `net472` y `net8.0` sin advertencias ni errores; después pasaron Core 275/275 y ForgeWeave 52/52.
- Se corrigió el auditor de bindings Gauntlet para que resuelva los comandos `Command.Click` y las propiedades `@...` dentro de `ItemTemplate` contra el ViewModel del elemento declarado por `ListPanel.DataSource`. Así comprueba `CommandHistoryItemVM.ExecuteLoad` y `ToolItemVM.ExecuteSelect` en sus propietarios reales; no cambió el comportamiento del producto ni la superficie pública del SDK.
- Se actualizó el codemap actual al contrato SDK 8. No se inició Bannerlord ni se generaron o modificaron ZIPs.

## Seguimiento de fuentes 24.0.0 — 25 de septiembre de 2026 (Rev039)

- Se endureció el cálculo del TTL semántico de `ForgeAgentMemory`: las duraciones cero y negativas vencen de inmediato sin provocar subdesbordamiento de marcas de tiempo; las duraciones positivas muy grandes se limitan a `DateTimeOffset.MaxValue` en vez de desbordarse.
- Se limitaron los IDs usados por `ModSettings.Register` y `Save` a un solo componente válido de nombre de archivo y se canonicalizó la ruta resultante para mantenerla bajo el directorio ModSettings. Se rechazan antes del acceso a archivos los IDs vacíos, separadores, rutas absolutas o con unidad, sintaxis de flujos alternativos, caracteres no válidos y caracteres de control; se conservan los nombres seguros con espacios y Unicode.
- Se hicieron reintentables los fallos al retirar manejadores ForgeWeave administrados. `ForgeWeaveRegistration` conserva el registro activo cuando falla la anulación del anfitrión, expone el error mediante `State`/`Error` y permite reintentar ante una transición posterior o con `Dispose()` en el mismo hilo. La comparación de generaciones evita que una devolución anterior reentrante sobrescriba el estado de una reconexión más nueva.
- Se corrigió el despacho de analizadores Core para que los IDs desconocidos permanezcan como `Unsupported`, en vez de asignarlos heurísticamente por una palabra clave del identificador. El analizador de archivos comprimidos ahora cuenta solo las entradas realmente inspeccionadas hasta `MaximumFiles` e indica como truncado el contenido omitido.
- Se actualizaron las referencias del SDK en inglés y español. La validación final coordinada mediante `.bat` está pendiente; esta entrada no afirma que las pruebas automatizadas posteriores a los cambios hayan pasado. El código fuente del producto permanece en 24.0.0 y el contrato SDK en 8; no se generaron ni modificaron ZIPs.

## Seguimiento de fuentes 24.0.0 — 25 de septiembre de 2026 (Rev040)

- Se amplió la evidencia de auditoría de Core con límites para recorrer carpetas: hasta 10.000 carpetas descubiertas, incluida la raíz seleccionada, profundidad 64 y 200.000 entradas del sistema de archivos por búsqueda. `MaximumFiles` tiene un valor predeterminado de 1.000 y se limita a 1–2.000. El caminador comprueba la cancelación durante el recorrido, omite puntos de reanálisis en la raíz y dentro de ella, e informa `analysis_scan_incomplete` junto con `Truncated` al alcanzar un límite o encontrar rutas omitidas/no legibles.
- Se añadió un límite de tamaño de archivo comprimido antes de construir `ZipArchive` e inspeccionar las entradas del directorio ZIP. Un archivo demasiado grande se informa como `archive_size_limit` sin inspeccionar entradas; los archivos aceptados inspeccionan hasta `MaximumFiles` entradas y marcan las omitidas como truncadas. Este límite no significa que el formato del archivo comprimido se haya validado por completo.
- Se actualizaron las guías bilingües de Diseño del Sistema y Desktop. Rev039 conserva el registro de sus hallazgos originales sobre TTL, `ModSettings`, ForgeWeave y despacho de analizadores; estos límites adicionales del caminador y del tamaño de archivos comprimidos surgieron durante la continuación de la auditoría de Core y se registran aquí sin editar Rev039.
- La validación final coordinada mediante `.bat` sigue pendiente; esta entrada no afirma que las pruebas automatizadas posteriores a los cambios hayan pasado. El código fuente del producto permanece en 24.0.0, el contrato SDK en 8 y no se regeneraron ni modificaron los ZIP existentes.

## Seguimiento de fuentes 24.0.0 — 25 de septiembre de 2026 (Rev041)

- Se cerró la ronda de endurecimiento de SDK/Core con correcciones de límites para la aritmética TTL semántica, los IDs seguros de `ModSettings`, las bajas ForgeWeave reintentables y reentrantes, los aliases exactos de analizadores, el recorrido acotado de carpetas y los límites de tamaño/entradas de archivos comprimidos. Las firmas públicas, el contrato SDK 8, la versión 24.0.0 y los TFM no cambian.
- La validación final mediante `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` terminó con código 0: las compilaciones seleccionadas `net472` y `net8.0` tuvieron cero advertencias y errores; Core pasó 286/286 y ForgeWeave 56/56. El launcher `.bat` alojó la biblioteca de pruebas en PowerShell; no se inició directamente ningún ejecutable de pruebas.
- No se inició Bannerlord ni se afirma haber validado el comportamiento dentro del juego. Los ZIP existentes no se regeneraron ni modificaron.

## Seguimiento de fuentes 24.0.0 — 25 de septiembre de 2026 (Rev042)

- Una segunda revisión cerró tres casos límite: todos los objetos construibles públicamente de `ForgeAgentMemory` comparten ahora el almacenamiento acotado del proceso; un `ForgeWeave.Register` reentrante que termina después de una conexión nueva retira el manejador obsoleto y conserva la limpieza fallida para poder reintentarla; y las lecturas de texto aplican el máximo de bytes al flujo abierto aunque el archivo crezca después de comprobar su tamaño.
- El preflight Core de ZIP ahora lee el registro final acotado del directorio central antes de crear `ZipArchive`. Se omiten directorios ilegibles o multidisco, así como archivos que declaran más de 10.000 entradas, antes de materializar objetos; los archivos aceptados mantienen el límite de entradas inspeccionadas por solicitud y la evidencia de truncamiento.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` pasó después de estas correcciones: cero advertencias/errores de compilación, Core 288/288 y ForgeWeave 58/58. Las pruebas siguen alojadas por el launcher; no se ejecutó directamente ningún ejecutable de pruebas.
- El código fuente permanece en 24.0.0 y el contrato SDK en 8. No se verificaron Bannerlord ni el comportamiento en vivo; los ZIP no se generaron ni modificaron.

## Seguimiento de fuentes 24.0.0 — 25 de septiembre de 2026 (Rev043)

- Se hizo segura la iteración de limpiezas ForgeWeave obsoletas ante callbacks anidados que modifican la lista de bajas pendientes: ahora se recorre una instantánea y se omiten registros que un callback reentrante ya retiró. Se añadió una regresión con dos registros obsoletos cuya baja falló y una reconexión anidada durante el reintento.
- La validación final mediante `.bat` después de esta corrección pasó con cero advertencias/errores, Core 289/289 y ForgeWeave 59/59. El código fuente permanece en 24.0.0, el contrato SDK en 8 y los ZIP existentes no se tocaron. Bannerlord no se inició.

## Seguimiento de fuentes 24.0.0 — 26 de septiembre de 2026 (Rev044)

- Se reforzó ForgeWeave ante tres fallos reentrantes del ciclo de vida: `Dispose()` ahora ignora devoluciones de disponibilidad mientras anula el registro; los errores de limpieza de generaciones antiguas ya no reemplazan el estado de una conexión más nueva; y toda excepción de `IForgeEventRegistry.Register` se considera potencialmente parcial, porque el contrato no garantiza rollback y `Unregister(IForgeEventHandler)` elimina por ID. El handle administrado falla de forma cerrada tras esa excepción, conserva los registros conocidos de una conexión nueva y mantiene el diagnóstico al disponerlo, sin intentar una baja insegura por ID.
- Se añadieron regresiones para el registro parcial, errores de limpieza obsoleta durante reconexiones anidadas y reemplazo de anfitrión desde `Unregister` durante `Dispose`. La referencia SDK inglesa y española, el codemap SDK y los comentarios de la interfaz describen este límite de recuperación.
- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` terminó con código 0: las compilaciones seleccionadas `net472` y `net8.0` informaron cero advertencias y errores; Core pasó 294/294 y ForgeWeave 64/64. El launcher alojó las pruebas; no se inició directamente ningún ejecutable de pruebas.
- La versión del producto permanece en 24.0.0 y `ForgeApi.Version` en 8. No se inició Bannerlord ni se afirma validación en vivo; tampoco se generaron o modificaron los ZIP existentes.

## Seguimiento de fuentes 24.0.0 — 26 de septiembre de 2026 (Rev045)

- Se cerró una brecha de disposición anidada en ForgeWeave: si un callback de limpieza del anfitrión dispone el registro mientras una transición exterior sigue ejecutándose, el callback exterior vuelve a comprobar `Disposed` al regresar del código del anfitrión y se detiene antes de intentar otro registro.
- Se añadió una regresión donde la disposición ocurre durante la limpieza de un anfitrión obsoleto pendiente y el anfitrión disparador está configurado para lanzar después de registrar; el anfitrión disparador queda intacto, se retira el manejador conocido y el registro permanece dispuesto.
- La ejecución final del `.bat` pasó con cero advertencias/errores, Core 295/295 y ForgeWeave 65/65. El código permanece en 24.0.0, el contrato SDK en 8; no se inició directamente ningún ejecutable de pruebas, Bannerlord no se inició y los ZIP no se generaron ni modificaron.

## Seguimiento de fuentes 24.0.0 — 26 de septiembre de 2026 (Rev046)

- Se cerró un recorrido sin límites en el validador independiente de `SubModule.xml`: las raíces de módulos y los análisis XML anidados ahora limitan el recorrido a 10.000 carpetas, profundidad 64, 200.000 entradas del sistema de archivos y 10.000 archivos XML adicionales. Se omiten puntos de reanálisis y los análisis parciales informan `analysis_scan_incomplete`.
- Los recorridos de árboles XML y detección de ciclos de dependencias ahora usan pilas explícitas; también se restauró el aviso de incompatibilidad declarada cuando el módulo en conflicto está presente. Se añadieron regresiones para el límite de profundidad y las incompatibilidades.
- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` pasó sin advertencias ni errores: Core 297/297 y ForgeWeave 65/65. El launcher alojó las pruebas; no se inició directamente ningún ejecutable de pruebas. Bannerlord no se inició y los ZIP no se generaron ni modificaron.

## Seguimiento de fuentes 24.0.0 — 26 de septiembre de 2026 (Rev047)

- `ForgeApi.Version` avanza de 8 a 9. `ForgeModelRegistry.Evaluate` evalúa las condiciones de modificadores desde una instantánea estable y fuera del bloqueo del registro; el nuevo scope desechable `BeginOwnerScope(ownerId)` retira solo las generaciones vigentes que le pertenecen, mientras `Register` y `Unregister` heredados siguen disponibles.
- `ForgeData.RemoveForgeData<T>` ahora separa la entrada externa de la entidad después de quitar su último valor tipado, sin descartar inserciones simultáneas del SDK. `GET /agents` informa conteos agregados reales de agentes y de los tres niveles de memoria, conserva el campo `agents` vacío y no expone identificadores, claves ni datos almacenados.
- Las operaciones públicas de `Core.Helpers` que aún no están soportadas ahora lanzan un `NotSupportedException` claro que indica que no se realizó ninguna acción, en lugar de aparentar éxito. `ModRuleAuditor`, con recorrido acotado, trata los análisis incompletos, entradas ilegibles y XML inválido como errores; ignora comentarios al comprobar intervalos numéricos Pulse de al menos 50 ms; solo confía en scripts bajo el directorio `tools` de nivel superior del repositorio; y expone los IDs de analizadores como colección inmutable.
- Se actualizaron las referencias bilingües del SDK y los servicios compartidos. Se está ajustando la aserción Gauntlet de pruebas para los IDs existentes `ForgeArgument` y `ForgeAssemblyPath`; el prefab no cambia.
- La validación posterior a los cambios mediante `.bat` está pendiente y no se afirma como aprobada. El código fuente permanece en 24.0.0; SDK y Core conservan `net472`/`net8.0` según su configuración. Los ZIP existentes no se regeneraron ni modificaron y Bannerlord no se inició; no se afirma comportamiento verificado en el motor.

## Seguimiento de fuentes 24.0.0 — 26 de septiembre de 2026 (Rev048)

- Registra la finalización del ajuste de la aserción Gauntlet de pruebas de Rev047 para los IDs existentes `ForgeArgument` y `ForgeAssemblyPath`; el prefab permanece intacto. El codemap actual y las guías de ForgeWeave y Diseño del Sistema identifican el contrato SDK 9 y documentan los registros de modelos por propietario, la auditoría acotada que falla de forma segura y la diferencia entre el umbral estático Pulse (50 ms) y el mínimo de ForgeWeave en ejecución (250 ms).
- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` pasó con código 0. Las compilaciones seleccionadas `net472` y `net8.0` informaron cero advertencias y errores; Core pasó 301/301 y ForgeWeave 65/65. Las pruebas se ejecutaron mediante el launcher `.bat`; no se invocó directamente ningún ejecutable de pruebas.
- El código fuente permanece en 24.0.0 y `ForgeApi.Version` es 9. Bannerlord no se inició, por lo que no se afirma validación en vivo de los helpers que dependen del motor. Los ZIP existentes no se regeneraron ni modificaron.

## Seguimiento de fuentes 24.0.0 — 26 de septiembre de 2026 (Rev049)

- Se redujo el trabajo de consultas del SDK: `ForgeModelRegistry.GetModifiers` ahora filtra bajo el bloqueo del registro y devuelve un resultado de categoría independiente, sin copiar la lista completa en cada consulta. El arnés sintético con 1.024 modificadores y 128 consultas midió 15,647 ms antes y 0,57–0,62 ms después; es una microprueba del arnés, no latencia por fotograma del juego.
- Se sustituyeron el reordenamiento repetido de la lista disponible y la eliminación frontal en ForgeWeave por un montículo binario mínimo, ordenado por ID ordinal sin distinguir mayúsculas. Las regresiones conservan el orden determinista existente entre prioridades iguales. En el arnés de 200 manejadores y 12 despachos, las mediciones observadas bajaron de 25,965–35,696 ms antes a 4,848–6,319 ms después; estos valores no miden duración de callbacks dentro del juego.
- Se redujeron asignaciones por solicitud en `ForgeLocalApi`: el despacho reutiliza un callback almacenado en vez de capturar un cierre y `/status` reutiliza su búfer UTF-8 estático y privado. La latencia de 32 solicitudes locales no mejoró (349,213 ms antes y 355,539 ms después), por lo que no se afirma mayor rendimiento de respuesta; el cambio solo elimina asignaciones evitables.
- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` terminó con código 0: las compilaciones seleccionadas `net472` y `net8.0` tuvieron cero advertencias y errores; Core pasó 303/303 y ForgeWeave 66/66. Las pruebas se ejecutaron mediante el launcher `.bat`.
- El código fuente permanece en 24.0.0 y el contrato SDK en 9. No cambiaron firmas de API, TFM, comportamiento del módulo ni ZIPs. Bannerlord no se inició; no se midió rendimiento en el motor.

## Seguimiento de fuentes 24.0.0 — 26 de septiembre de 2026 (Rev050)

- Se conservó el ID de ruta Desktop `ApiDeprecationChecker` y se actualizaron su nombre visible, propósito, estado de disponibilidad y dossier en los 13 idiomas compatibles para indicar que el análisis de obsolescencia no está disponible hasta contar con un catálogo de API de TaleWorlds verificado, versionado y con cobertura de versión explícita. La ruta ya no presenta la disponibilidad genérica de un analizador como evidencia de obsolescencia.
- Se actualizaron el generador de diccionarios de recursos de Desktop y las notificaciones al cambiar de idioma, para que el texto localizado de la ruta y su entrada de búsqueda sigan el idioma seleccionado. Se documentaron los límites de evidencia en las secciones inglesa y española de la guía Desktop.
- Las pruebas funcionales posteriores al cambio y la verificación de la interfaz en vivo quedan pendientes; esta entrada no afirma que hayan pasado. El código fuente permanece en 24.0.0, la documentación/contratos públicos del SDK y los IDs de ruta no cambian, y los ZIP existentes no se regeneraron ni modificaron.

## Seguimiento de fuentes 24.0.0 — 26 de septiembre de 2026 (Rev051)

- Las heurísticas de código fuente de Core ahora enmascaran comentarios y literales de C# antes de evaluar reglas textuales, respetan la cancelación durante el procesamiento de archivos individuales y generan hallazgos localizados si un archivo de código o localización es ilegible o supera el límite, sin detener el análisis acotado de los demás. `ApiDeprecationChecker` conserva su ID de ruta, pero devuelve `Unsupported` hasta disponer de un catálogo de obsolescencias de TaleWorlds verificado y versionado.
- El ledger de Desktop y sus exportaciones conservan ID de regla, ubicación de origen, evidencia y recomendación. Toda respuesta satisfactoria de la acción ForgeWeave, incluida una carga vacía, pasa por la validación del snapshot; las respuestas malformadas o incompletas distinguen transporte `Received` y snapshot `Unparsed`, conservan la carga y limitan el motivo del diagnóstico a 240 caracteres.
- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --no-pause <nul"` pasó: la compilación de la solución terminó con cero advertencias y errores; Core 306/306, ForgeWeave 66/66, Desktop 54/54 y 275 casos del arnés de render WPF pasaron. Las suites BAT de recursos y planes de importación también pasaron; se omitió el lector TPAC profundo opcional porque no estaba disponible su biblioteca/fixture local. Todas las pruebas se iniciaron mediante archivos `.bat`.
- El código fuente permanece en 24.0.0 y el contrato SDK en 9. No cambiaron API/SDK públicos, rutas ni permisos. Bannerlord no se inició y no se afirma comportamiento dentro del juego; los ZIP existentes no se regeneraron ni modificaron.

## Seguimiento de fuentes 24.0.0 — 26 de septiembre de 2026 (Rev052)

- `ForgeModelRegistry` ahora reutiliza instantáneas inmutables por categoría y evalúa solo la categoría solicitada; las instantáneas permanecen estables tras registros posteriores y los valores de enum arbitrarios no hacen crecer la caché. ForgeWeave reutiliza planes de despacho para los tipos finitos de evento distintos de `Custom` y los invalida tras cambios de registro exitosos; los planes por tema `Custom` siguen sin almacenarse en caché.
- El análisis de Core ahora propaga la cancelación por las lecturas acotadas, el análisis XML, los ciclos de analizadores y la inspección de módulos, conservando sin cambios la firma pública síncrona del validador. Se añadieron regresiones de cancelación cooperativa y análisis de fuentes acotado.
- Cinco ejecuciones de `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` pasaron: en cada una, las compilaciones seleccionadas `net472`/`net8.0` tuvieron cero advertencias y errores; Core pasó 312/312 y ForgeWeave 68/68. Las pruebas se iniciaron únicamente mediante el archivo `.bat`.
- En esos cinco recorridos del arnés, la mediana de `GetModifiers` para 1.024 modificadores y 128 consultas bajó de 0,590 ms a 0,004 ms; la medición Core de 200 manejadores y 12 despachos bajó de 5,789 ms a 2,167 ms; y ForgeWeave independiente bajó de 6,763 ms a 2,018 ms. Son mediciones del arnés, no latencia por fotograma de Bannerlord ni de la aplicación en vivo. `Evaluate`, las estadísticas de memoria y los análisis de fuentes no tienen una línea base anterior comparable y se registran solo como mediciones. No se afirma mejora en los tiempos ruidosos de `/status`.
- El código fuente permanece en 24.0.0, el contrato SDK en 9 y la API pública no cambia. Bannerlord no se inició y no se midió el comportamiento del motor. Los ZIP existentes no se generaron ni modificaron.

## Actualización del código fuente 25.0.0 — 26 de septiembre de 2026 (Rev053)

- Se actualizan a 25.0.0 los metadatos del código fuente y los manifiestos de módulos, y `ForgeApi.Version` pasa a 10. SDK y Core conservan los destinos `net472` y `net8.0`; se mantienen las firmas de los métodos públicos existentes.
- Se agrega `ModSettings.TrySave<T>` con resultados explícitos para serializador ausente, error de serialización y error de almacenamiento. Escribe un temporal en el mismo directorio y confirma el archivo antes de cambiar la caché; los fallos conservan el archivo y la caché anteriores. `Register<T>` sigue devolviendo los valores predeterminados sin crear un archivo marcador si no hay serializador; `Save<T>` conserva su firma y registra los errores.
- Se agrega `ForgeApi.AutoRegisterWithReport` con conteos y detalles de error acotados, registro parcial de tipos independientes y un wrapper `AutoRegister` compatible que registra errores. La memoria Semantic distingue `Found`, `Missing`, `Expired` y `TypeMismatch`; Procedural ofrece los estados aplicables a datos sin TTL.
- La entrega administrada de `RegisterWhenAvailable` comprueba la vigencia del suscriptor y la generación de conexión; omite el resto de una instantánea de disponibilidad obsoleta después de cambios reentrantes del ciclo de vida.
- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` pasó: las compilaciones seleccionadas `net472` y `net8.0` tuvieron cero advertencias y errores; Core pasó 316/316 y ForgeWeave 69/69. Las pruebas se iniciaron únicamente mediante el archivo `.bat`.
- Bannerlord no se inició y no se generaron ni modificaron ZIPs; no se afirma comportamiento verificado dentro del juego.

## Seguimiento del código fuente 25.0.0 — 26 de septiembre de 2026 (Rev054)

- Cierra la carrera de baja de disponibilidad administrada con compuertas de entrega por suscriptor. Se omite un callback pendiente retirado antes de su turno; `UnregisterWhenAvailable` espera que termine uno ya iniciado, mientras los callbacks siguen fuera del bloqueo global de disponibilidad y pueden darse de baja a sí mismos.
- Rechaza ajustes nulos antes de que `TrySave<T>` prepare o confirme un archivo, conservando el archivo y el objeto en caché anteriores. Documenta la identidad de ajustes de Windows sin distinción de mayúsculas y la protección que impide que una carga de registro antigua reentrante o concurrente sobrescriba una confirmación más reciente.
- Se añadieron pruebas deterministas para el callback retirado que ya estaba en la instantánea de `Connect`, la espera de un callback en curso, la baja de sí mismo y la preservación ante un guardado nulo. El launcher `.bat` pasó con cero advertencias/errores de compilación, Core 318/318 y ForgeWeave 71/71.
- Este seguimiento corresponde a la revisión protegida Rev035; la secuencia del changelog de fuentes es independiente. Bannerlord no se inició y no se generaron ni modificaron ZIPs.

## Seguimiento del código fuente 25.0.0 — 26 de septiembre de 2026 (Rev055)

- ForgeWeave reutiliza los planes de eventos finitos al producir los hallazgos de `Snapshot()`; los temas `Custom` siguen sin almacenarse en caché y los datos de salud e informe se copian desde el estado actual. En cinco mediciones del mismo equipo mediante `.bat`, la mediana de 8 snapshots con 256 manejadores y 64 registros retenidos bajó de 60,870 ms a 26,504 ms (56,4 %); la mediana del arnés independiente pasó de 94,258 ms a 67,380 ms (28,5 %). Son tiempos del arnés, no latencia dentro del juego. Se midió `GetHandlerHealth` y quedó intacto porque los resultados no mostraron un cuello de botella estable.
- `SharedServiceMonitor<T>.Changed` ahora reutiliza una matriz tipada de suscriptores, reconstruida solo al agregar o quitar manejadores. En cinco ejecuciones, la mediana de un lote de notificaciones a 1.000 monitores mejoró de 80,041 ms a 65,324 ms (18,4 %); el caso de 100 monitores permaneció prácticamente igual (4,558 ms frente a 4,585 ms). Se conservan el orden, el aislamiento de callbacks, la reentrada y la afinidad de hilo.
- Se evaluó y revirtió una ruta que transfería la lista de resultados de `Evaluate`: los perfiles densos incondicional y mixto mejoraron solo 7,3 % y 3,4 %, por debajo del umbral del 10 %, mientras que un perfil amplio con 127 modificadores empeoró. El perfil denso con condiciones verdaderas mejoró, pero el resultado no fue uniforme. El constructor público sigue copiando defensivamente las listas de quien lo llama. Las mediciones de memoria y resolución de servicios no justificaron cambiar esas implementaciones.
- Se corrigieron la documentación de telemetría ForgeWeave y el nombre de una prueba: el despacho escribe muestras en un búfer acotado preasignado, mientras que el cálculo de percentiles copia y ordena una cantidad limitada de datos bajo demanda. No se afirma que los informes no generen asignaciones.
- Pasaron cinco corridas de línea base y cinco posteriores de `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"`. Tras revertir la prueba de `Evaluate`, la ejecución final del launcher pasó con cero advertencias/errores en las compilaciones seleccionadas `net472` y `net8.0`, Core 323/323 y ForgeWeave 73/73. Todas las pruebas se iniciaron mediante el archivo `.bat`.
- Este seguimiento del código fuente se registró en la revisión protegida Rev036; la numeración del changelog de fuentes y del DOCX es independiente.
- El código fuente permanece en 25.0.0 y `ForgeApi.Version` en 10; no cambian API pública, destinos ni comportamiento del juego. Bannerlord no se inició y los ZIP existentes no se generaron ni modificaron.

## Seguimiento de fuentes Desktop 25.2.0 — 26 de septiembre de 2026 (Rev056)

- Se corrigió la composición del banco de trabajo en ventanas estrechas: el rail de navegación tiene un mínimo de 220 DIP (limitado a 276 DIP), el área de trabajo conserva un mínimo de 400 DIP y el panel Split Deck mide 280 DIP. Se añadió cobertura del Split Deck en la ventana mínima.
- El marco cartográfico ahora coincide con la proporción de su fuente (160×90 DIP para 448×252 píxeles), conserva la presentación uniforme sin recorte y ya no usa cachés bitmap de escala fija en los adornos pasivos del mapa y el sello. Se localizaron las etiquetas fijas de la shell, ayudas, alternativas vacías/fijadas y tooltips en los 13 diccionarios de idioma, que conservan 101 claves coincidentes. Al limpiar Split Deck, se notifican los cambios derivados de título y categoría después de vaciar la evidencia.
- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause <nul"` terminó con cero advertencias/errores de compilación y Desktop MVVM 55/55. Cinco ejecuciones seriales de `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause --output <report.json> <nul` guardaron informes con sufijos `-01` a `-05`; cada una pasó 275 casos, con 152 pases de layout.
- En las cinco ejecuciones finales del harness, la mediana total fue 11.335 ms frente a 13.652 ms de línea base (-17,0 %); la mediana de llamadas de layout fue 2.075 ms frente a 2.397 ms (-13,5 %). Las medianas de inicio, navegación/filtro, localización y tema fueron 2.582 ms, 2.042 ms, 2.007 ms y 3.109 ms; todas quedaron por debajo de sus medianas de línea base. Son mediciones del harness, no latencia interactiva de Desktop. La matriz de escalas rasteriza capturas y no simula el layout con DPI del sistema Windows.
- El arte decorativo del rail permanece colapsado intencionalmente porque su proporción fuente no cabe en el marco disponible. La inspección en vivo de WPF/UI Automation sigue pendiente; no se afirma aprobación visual en vivo. La versión del producto permanece en 25.2.0 y no se regeneraron ni modificaron ZIPs.
## Seguimiento de fuentes Desktop 25.2.0 — 26 de septiembre de 2026 (Rev057)

- Se reorganizó la shell WPF en controles de presentación especializados; `MainWindow` conserva el rol de ventana y marco, y las plantillas de página pasan a `Resources/Views/ToolPageTemplates.xaml`. `AdaptiveWorkbenchPanel` coloca la ruta seleccionada, Split Deck opcional y dossier contextual en paralelo cuando caben sus anchos mínimos; de lo contrario los apila en el área desplazable del espacio de trabajo. Un control accesible y localizado abre el dossier opcional; su estado no se persiste.
- Se mantienen el catálogo de rutas, el rail, los atajos, el estado MVVM, los contratos SDK/IPC y la versión del producto. Se agregaron dos texturas WPF locales y pasivas optimizadas: una franja de título de 2172×67 y un ornamento de esquina de tarjeta de 256×171. Alto Contraste conserva superficies sólidas; la legibilidad de los adornos aún requiere revisión.
- La validación Rev057 está pendiente: la línea base previa de cinco ejecuciones de render pasó 275 casos y 152 pases de layout por ejecución, con medianas de 11.286 ms en total y 2.072,6 ms en llamadas síncronas de layout. Son tiempos del arnés, no latencia de la aplicación. Las suites BAT posteriores y la UI Automation de solo lectura todavía no están aprobadas ni registradas. No se regeneró ni modificó ningún ZIP.
- El selector de tema ahora recorta etiquetas largas como High Contrast en una sola línea. El launcher maestro también acepta --render-output <ruta>; solo redirige el informe JSON y no altera la geometría de la previsualización. Entre 100–200 %, el arnés rasteriza a la escala solicitada y conserva la ventana lógica de 1360×820 DIP con transformación de layout identidad; no modifica el DPI de Windows ni las medidas DIP.

## Seguimiento de fuentes Desktop 25.2.0 — 26 de septiembre de 2026 (Rev058)

- Se corrigieron las dimensiones compactas de búsquedas y acciones: las búsquedas del rail y de la paleta de comandos miden 42 DIP de alto y centran el texto; el botón de limpieza de 32×32 DIP reserva el espacio necesario para que no se superponga a la consulta. En la ventana mínima de 980×680 DIP, los botones de cabecera de la paleta, Split Deck y dossier usan anchos mínimos de 116, 120 y 126 DIP y altura mínima de 36 DIP. Los botones de órdenes de trabajo miden 34 DIP de alto, con anchos acotados y ajuste de línea. El harness confirmó que las acciones permanecen visibles en las ventanas mínima y normal.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` compiló con cero advertencias/errores, pasó Desktop 56/56 y completó una corrida de render con compilación con 277 casos/161 pases de layout en 9.410 ms. Cinco corridas posteriores sin compilación, cada una con un informe JSON distinto (`artifacts/desktop-render-rev057-controls-repeat-01..05-20260926.json`), pasaron 277 casos/161 pases. Las muestras exactas `milliseconds` fueron 8.754, 8.928, 8.281, 8.355 y 8.093 ms (mediana 8.355 ms); las muestras `renderLayoutMilliseconds` fueron 1.685,9, 1.826,3, 1.652,8, 1.709,0 y 1.507,1 ms (mediana 1.685,9 ms).
- Frente al grupo Rev057 final2 de cinco corridas (276 casos/161 pases de layout; medianas de 8.360 ms totales y 1.732,6 ms de layout), la mediana total cambió -0,06 % y la de layout -2,7 %. El resultado es esencialmente estable y no demuestra una mejora sustancial de rendimiento. Son tiempos del harness, no latencia de interacción de la aplicación.
- `tools/Test-CalradiaForge-Desktop-Uia.bat` pasó 20/20 comprobaciones acotadas y de solo lectura en `artifacts/desktop-uia-rev057-controls-final.json`. Pasó `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check`; el inventario empaquetado de PNG suma 7.465.788 bytes comprimidos y 15.921.104 bytes RGBA decodificados, incluidos los dos adornos Rev057 (310.173 bytes comprimidos y 975.312 bytes RGBA decodificados). Los registros de escala rasterizada no cambian el DPI de Windows ni simulan el layout con DPI del sistema; no se afirma aprobación visual humana. No se inició Bannerlord, no se regeneró ningún ZIP y la versión 25.2.0 y los contratos públicos permanecen sin cambios.

## Pulido visual de Desktop 25.2.0 — 26 de septiembre de 2026 (Rev059)

- Se redujo la opacidad de las texturas de superficie y título del tema Parchment de 0,34/0,32 a 0,11/0,12 para mantener el grano decorativo detrás del contenido. El rail de navegación expone una selección activa visible y observable; el campo de búsqueda muestra la indicación localizada `Ui.SearchHint`.
- Se reservó una columna de 168 DIP para el tablero cartográfico, con marco de 160×90 DIP y margen de 8 DIP. La ilustración del estado vacío de evidencia ahora mide 42 píxeles y su botón de acción 32 DIP.
- `tools/Run-CalradiaForge-Tests.bat` completó la compilación Desktop sin advertencias ni errores, pasó Desktop 56/56 y aprobó 277 casos de render WPF con 161 pases de layout. El arnés registró 7.808 ms en total y 1.824,9 ms en llamadas de layout; son métricas del arnés, no latencia de la aplicación.
- `tools/Test-CalradiaForge-Desktop-Uia.bat` pasó 20/20 comprobaciones acotadas y de solo lectura en `artifacts/desktop-uia-rev059.json`; valida el árbol de accesibilidad, no la apariencia visual. La versión del producto permanece en 25.2.0. No se afirma aprobación visual en vivo y no se regeneró ningún ZIP.

### Ejecución de pruebas WPF que preserva el foco (Rev060)

- Se trasladaron los HWND del render WPF a un escritorio privado de Windows antes de crear objetos WPF o hooks. El guard solo registra eventos de primer plano de ese escritorio y ya no llama a `SetForegroundWindow`.
- Las comprobaciones interactivas nativas de minimizar/maximizar/restaurar se sustituyeron por inspecciones de bindings, estilos y manejadores. Se eliminó la restauración explícita del foco del ejecutor UIA opcional; ahora informa fallo si observa que su app pasa a primer plano.
- Dos ejecuciones ocultas de `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output <ruta-json-única>` compilaron con cero advertencias/errores, pasaron Desktop 57/57 y render 278/278, y conservaron el HWND interactivo `0x171168`. El último render informó 158 pases de layout y 9.492 ms totales. UIA no se volvió a ejecutar; no se afirma que haya pasado.
- El render solo apareció en primer plano dentro de su escritorio privado separado. La versión del producto sigue en 25.2.0; no cambiaron contratos públicos, rutas ni ZIPs.

### Launcher Desktop que preserva el foco (Rev061) — 26/09/2026

- Se agregó `tools/Run-CalradiaForge-Desktop-Checks-Hidden.vbs` para ejecutar las suites existentes Desktop/render mediante `.bat`, sin ventana de comandos visible y con log, estado e informe únicos por corrida. La comprobación UIA opcional con PowerShell permanece separada de este flujo silencioso.
- El proceso UIA ahora solicita `CreateNoWindow` y `Hidden`; su observador pasivo registra las transiciones recientes de primer plano. Nunca activa ni restaura una ventana.
- Se conservan la versión 25.2.0 y todo el comportamiento de la app/API. Esta revisión comprobó el foco cuando no había una ventana WPF de Calradia Forge abierta; queda pendiente verificarlo con la app del usuario en primer plano durante la prueba.
- El launcher oculto terminó con código 0: Desktop 59/59, 281 casos de render WPF, 158 pases de layout, cero advertencias/errores de compilación y 7.912 ms totales del arnés. El HWND `0x580296` (AvastUI) se mantuvo igual antes y después. Un intento combinado con UIA no produjo informe y terminó en otro HWND de AvastUI; se desconoce la causa, por lo que UIA sigue excluida de este launcher.

### Lectura adaptable del estado de ruta (Rev062) — 26/09/2026

- La tarjeta de estado de la aplicación ahora ajusta los nombres largos de ruta seleccionada a una segunda línea en el mínimo de 980×680 DIP, en vez de truncarlos en una sola línea. El tooltip conserva el valor completo; las demás tarjetas y el comportamiento de las rutas no cambian.
- La suite Desktop por `.bat` pasó 59/59 y la suite de render WPF pasó 281/281 con 158 pases de layout. La compilación terminó con cero advertencias y errores; el informe de render registró 8.415 ms. Es tiempo del arnés, no latencia de interacción de la aplicación.
- La versión del producto permanece en 25.2.0; no cambiaron la API, las rutas, los comandos, los permisos ni los ZIP.

### Ilustración vertical del rail (Rev063) — 26/09/2026

- Se reemplazó la decoración horizontal sin uso del rail por una ilustración vertical transparente generada localmente, optimizada a 320×640 RGBA y ajustada uniformemente detrás de la navegación. El derivado retirado queda excluido del manifiesto de recursos Desktop; la fuente se conserva.
- Se agregaron tokens de opacidad por tema para War Table, Parchment Light y Alto contraste. La visibilidad decorativa sigue usando el ajuste existente; las superficies de Alto contraste siguen siendo sólidas y la ilustración no recibe entrada ni foco de teclado.
- Pasaron la generación de texturas y la comprobación determinista `--check`. La validación Desktop por `.bat` compiló sin advertencias/errores, pasó 59/59 pruebas Desktop y 281 casos de render WPF/158 pases de layout. El arnés reportó 7.998 ms totales y 1.779,8 ms en llamadas de layout; esos valores no representan latencia de interacción de la aplicación. Se revisaron capturas renderizadas; la apariencia en vivo y con DPI real del sistema siguen sin verificarse.
- La versión del producto sigue en 25.2.0; no cambiaron API pública, rutas, comandos, permisos ni ZIPs.

### Refinamiento de la ilustración del rail para alto contraste (Rev064) — 27/09/2026

- Se reemplazó el derivado Rev063, demasiado oscuro, por un grabado transparente más claro en latón y verdigrís, conservando la composición vertical y el comportamiento pasivo. Rev063 permanece como histórico y se excluye de los recursos incrustados.
- Se agregó una regresión de render para Alto contraste que mide la visibilidad del adorno después de componer alfa sobre la superficie sólida del rail y comprueba que el texto claro/atenuado conserve al menos 4,5:1 frente a cada píxel visible del arte.
- Pasó la validación determinista de texturas. La compilación BAT Desktop terminó sin advertencias/errores; Desktop pasó 59/59 y render 281 casos/158 pases de layout. El informe registró 7.802 ms totales y 1.738,8 ms en llamadas de layout. Son mediciones del arnés; las capturas no equivalen a aprobación de la app en vivo/DPI.
- La versión del producto sigue en 25.2.0; no cambiaron API pública, rutas, comandos, permisos ni ZIPs.

### Refinamiento de la superficie del informe de evidencia (Rev065) — 27/09/2026

- Se diferenciaron visualmente las pestañas Evidence y Raw Result con encabezados localizados, iconos, conteo de evidencias y estados seleccionado, hover y foco. Cada hallazgo ahora usa una tarjeta legible con origen/estado, ID de regla, ubicación, evidencia y recomendación. La acción del ledger vacío sigue siendo interactiva junto a su ilustración local pasiva y ejecuta el comando de paleta existente.
- Se agregaron regresiones de render para las etiquetas del informe, el contexto de hallazgos, el binding e hit testing de la acción vacía y los límites de layout. `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-visual-report-rev065-fix1.json"` compiló con cero advertencias/errores, pasó Desktop 59/59 y las comprobaciones de render/recursos WPF 281/281 con 158 pases de layout. El informe registró 7.856 ms totales y 1.705,8 ms en llamadas de layout; son mediciones del arnés, no latencia de la app abierta.
- No se inspeccionó la aplicación en vivo. La versión del producto sigue en 25.2.0; no cambian API pública, rutas, comandos, permisos, dependencias, recursos ni ZIPs.

### Alcance del hit testing de evidencia fuera de pantalla (Rev066 fuente) — 27/09/2026

- Se aclaró que el hit testing de la acción del ledger vacío se ejecuta en el host de render WPF fuera de pantalla mediante `VisualTreeHelper.HitTest` en el centro del botón, y comprueba su superficie WPF y sus antecesores visibles mientras la superposición de la paleta está contraída. Esto valida únicamente el subárbol visual renderizado; no simula la entrada del puntero del sistema operativo ni un clic en una ventana en vivo.
- El artefacto final posterior a la corrección `artifacts/desktop-visual-report-rev066-review.json` pasó con Desktop 59/59, 281 casos de render y 158 pases de layout, incluida la regresión de recomendaciones que solo contienen espacios en blanco. La compilación terminó con cero advertencias/errores. El JSON registra 7.821 ms totales y 1.707,2 ms en llamadas de layout; ninguno representa latencia de interacción de la aplicación.
- El comportamiento del puntero del sistema operativo y de WPF en vivo sigue sin verificarse. Rev066 del changelog fuente es independiente del anexo Rev046 del Registro de Mejoras protegido. La versión del producto sigue en 25.2.0; no cambiaron API pública, rutas, comandos, permisos, dependencias ni ZIPs.

### Jerarquía del estado y de comandos (Rev067) — 27/09/2026

- Se destacó la acción existente de la paleta en la cabecera con una superficie primaria de latón y un icono local pasivo de búsqueda, conservando su nombre accesible y binding de comando. Se añadieron iconos semánticos pasivos uniformes y una etiqueta localizada para la herramienta activa en las cinco tarjetas de estado; la opacidad varía por tema y es mayor en Alto contraste. El botón de exportación de evidencia aumentó a 30 DIP de alto.
- Se agregó `Ui.ActiveTool` a los 13 catálogos y se ampliaron las pruebas de render para etiquetas localizadas, hit testing y foco de los iconos pasivos, opacidad por tema y tamaño/accesibilidad de la acción principal.
- La compilación BAT Desktop terminó sin advertencias/errores; Desktop pasó 59/59 y el render WPF pasó 285 casos con 158 pases de layout. El artefacto final registra 8.681 ms totales/2.013,7 ms en llamadas de layout; la línea base de esta sesión registró 10.893 ms/2.564 ms con 281 casos/158 pases. Son tiempos de una sola ejecución del arnés y no prueban el rendimiento de la app abierta. Se revisaron capturas fuera de pantalla de War Table y Alto contraste; no se probó la app en vivo.
- La versión del producto sigue en 25.2.0; no cambiaron API, rutas, comandos, permisos, dependencias ni ZIPs.

### Cabecera Desktop compacta y resumen de evidencia (Rev068) — 27/09/2026

- Se compactó la cabecera WPF para el mínimo de 980×680 DIP: se acortó la etiqueta visible de adornos sin cambiar su nombre accesible localizado completo ni su tooltip, la acción del dossier ahora muestra un glifo de libro con nombre accesible y se redujeron los anchos de controles para mantener la fila compacta.
- El botón de exportación de evidencia ahora muestra un icono local pasivo en un botón de 36×30 DIP. Conserva el comando, ID de automatización, nombre accesible y tooltip; el ancho recuperado permite leer el resumen de evidencia retenida en la tarjeta de estado.
- Se agregaron aserciones de render para la posición de cabecera en tamaño mínimo, la visibilidad del resumen, la geometría compacta de exportación y la accesibilidad del dossier y del interruptor decorativo. `Ui.DecorativeAccentsShort` está en los 13 idiomas.
- La compilación BAT Desktop terminó sin advertencias/errores; Desktop pasó 59/59 y el render WPF 285/285 con 158 pases de layout. La ejecución final única registró 13.135 ms totales/3.072,1 ms en llamadas de layout frente a 9.120 ms/2.033,3 ms de la línea base de una ejecución. Son mediciones del arnés; la corrida final fue más lenta y no se afirma una mejora de rendimiento. El comportamiento WPF en vivo y el layout con DPI real siguen sin verificarse.
- La versión del producto sigue en 25.2.0; no cambiaron API, rutas, comandos, permisos, dependencias ni ZIPs.

### Ledger en ventana mínima y disposición adaptable de identidad (Rev069) — 27/09/2026

- Se agregó cobertura de render a 980×680 DIP que confirma que el mensaje y la acción del ledger de evidencia vacío permanecen en el viewport visible del área de trabajo, y que la acción conserva su altura mínima de activación. El distintivo de identidad de herramienta tiene ancho acotado; los nombres de categoría y lemas largos permanecen en una línea, se recortan con puntos suspensivos, muestran el valor completo en tooltips y quedan dentro del marco.
- Se agregó una aserción para la cabecera de una sola fila y ancho mínimo en los 13 idiomas admitidos. La suite de render conserva las comprobaciones existentes de recursos ilustrados locales y adornos pasivos; este seguimiento no agrega recursos ni dependencias de ejecución.
- El artefacto BAT final proporcionado registra cero advertencias/errores de compilación, Desktop 59/59 y render WPF 286/286 con 172 pases de layout. Su JSON registra 11.719 ms de tiempo total del arnés y 3.106,8 ms en llamadas de layout. Son mediciones del arnés, no latencia de la aplicación abierta ni una afirmación de rendimiento. No se realizó UI Automation en ejecución ni validación de la app en vivo.
- La versión del producto sigue en 25.2.0; no cambiaron API pública, rutas, comandos, permisos, IPC, dependencias ni ZIPs.

### Alcance de auditoría del DPI del sistema Windows (Rev070) — 27/09/2026

- Se aclaró que el arnés de render WPF mantiene fijo el DPI/layout DIP de Windows del host aislado. Los valores de vista previa 100 % y 200 % solo cambian la densidad raster de salida de `RenderTargetBitmap`; no simulan layout con DPI del sistema Windows al 125 %, 150 % o 200 %.
- El artefacto proporcionado `artifacts/desktop-visual-rev069-scale-audit.json` marca `windows-system-dpi-layout-coverage` como `not-simulated`, sin factores de DPI de Windows aplicados y con factores raster de bitmap de vista previa `[1, 2]`. Se conservó la cobertura funcional existente de rutas, idiomas, temas e interacciones.
- La ejecución proporcionada registra Desktop 59/59, 287 casos de render WPF, 172 pases de layout y cero advertencias/errores de compilación. Los tiempos JSON del arnés son 9.321 ms totales y 2.134,7 ms en llamadas de layout; no representan latencia de la app abierta. El comportamiento con el DPI real de Windows sigue sin verificarse. No se ejecutaron pruebas en este añadido documental.
- La versión sigue en 25.2.0; no cambiaron API, rutas, comandos, permisos, IPC, dependencias ni ZIPs.

### Refuerzo de IPC, exportación de informes y localización de estudios Desktop (Rev071) — 27/09/2026

- Se limitaron de forma incremental las lecturas IPC delimitadas por salto de línea al límite existente de 32 Mi caracteres UTF-16; se respeta la cancelación y las respuestas excesivas se desconectan antes de deserializar.
- Las exportaciones ahora escriben de forma asíncrona a un temporal exclusivo y se publican con un nombre único sin sobrescritura, con estado de cancelación/error y limpieza del temporal.
- Se añadieron nombres accesibles específicos por comando y feedback localizado al copiar desde el dossier; también se localizaron los rótulos estáticos restantes de visualizadores en los 13 catálogos, conservando valores de muestra e identificadores técnicos.
- La compilación BAT de Desktop terminó con cero advertencias/errores; Desktop pasó 63/63 y el render WPF pasó 289 casos con 182 pases de layout. El artefacto registra 11.758 ms totales del arnés y 3.483,7 ms en llamadas de layout; ninguno representa la latencia de la app abierta.
- Tras añadir ajuste de línea a las estadísticas traducidas y a los indicadores compactos de las tarjetas estrechas, la ejecución final del BAT volvió a pasar la misma cobertura; su artefacto separado registra 10.939 ms totales y 3.120,8 ms en llamadas de layout, mediciones exclusivas del arnés. La repetición de UIA en solo lectura pasó 23/23 controles, incluidos los botones de copia del dossier, y conservó la ventana en primer plano.
- El BAT de UIA en solo lectura pasó 23/23 controles y registró ForegroundUnchanged=true; no ejercitó selectores, ejecución de trabajos, cambios de preferencias ni aprobación visual. No se inició una sesión de Bannerlord.
- La versión sigue en 25.2.0; no cambiaron API pública, rutas, comandos, permisos, protocolo, dependencias ni ZIPs.

### Pie Desktop adaptable y ajuste del panel Gauntlet (Rev072) — 27/09/2026

- El pie de atajos WPF ahora permanece dentro del viewport mínimo de 980×680 DIP, con un margen inferior acotado y comprobaciones de ajuste del texto en los 13 idiomas. Se sustituyó el recorte de cabecera empaquetado Rev057 por el panorama cartográfico determinista Rev072 de mayor resolución; el original de alta resolución no se incluye en el ensamblado.
- Se ajustaron el espaciado de la cabecera Gauntlet, la disposición al enfocar evidencia y la reserva del Playbook. El Playbook libera el margen derecho del espacio de trabajo y se oculta al enfocar la evidencia. Las ocho áreas usan sprites semánticos; se conservan las 194 rutas de herramientas y los 56 bindings de comando.
- El auditor visual ahora modela anchos de 1220/1280/1600/1920, estados de evidencia normal/detallado/enfocado y la oclusión comprobada por el panel opaco. Se actualizó la lista de iconos para reflejar el prefab, manteniendo requeridos los 17 iconos generados y el inventario de atlas de 30 sprites.
- El render Desktop conservó 289 casos y 182 pases de layout en cada una de cinco corridas anteriores y cinco posteriores. Las medianas del arnés fueron 10.953 ms antes y 11.474 ms después (+4,8 %); las medianas de llamadas de layout fueron 3.220,1 ms y 3.095,3 ms (-3,9 %). Son mediciones del arnés de render, no latencia de la aplicación abierta; no se afirma una mejora de rendimiento global. La comprobación `--check` de texturas fue determinista y el paquete permaneció dentro de sus presupuestos.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` terminó sin advertencias ni errores de compilación: Assets 8/8, Core 338/338, ForgeWeave 73/73, Desktop 63/63 y render WPF con 289 casos/182 pases de layout. `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` también pasó. UIA Desktop de solo lectura pasó 23/23 comprobaciones.
- Los perfiles Client y Modding Kit se compilaron y desplegaron con respaldos de archivos verificados. Se conservó el TPAC instalado con SHA-256 `69513B5617F026E1F106F6CA6D47CF5C5CE0B5869FFA472E9166B037CA8EDEA6`. El inicio del Modding Kit se detuvo en un cuadro `RGL WARNING`, que se dejó abierto; por ello el renderizado en vivo del panel sigue sin verificarse. No se cargó campaña ni batalla.
- La versión sigue en 25.2.0; no cambiaron API pública, rutas, comandos, permisos, dependencias ni ZIPs.

### Corrección del bloqueo F10 por la barra de Gauntlet (Rev073) — 27/09/2026

- Se corrigió la estructura generada de `ForgeEvidenceScroll` después de que la ruta F10 del menú principal produjera una aserción de widget de barra integrado de TaleWorlds y una violación de acceso en `ScrollablePanel.UpdateScrollablePanel`. El panel ahora resuelve su referencia relativa a un `ScrollbarWidget` vertical hermano con un control identificado correspondiente.
- Se amplió la auditoría estructural para exigir el contrato de barra en los cinco paneles desplazables y se ajustó la regresión de la superficie de evidencia para permitir únicamente la imagen del control de la barra.
- Las comprobaciones BAT de Core/ForgeWeave pasaron 333/333 y 73/73 sin advertencias ni errores de compilación; el BAT visual de Gauntlet pasó sin errores ni advertencias de auditoría y Core 338/338. Los perfiles Client y Modding Kit se compilaron y desplegaron con respaldos verificados; se conservó el TPAC existente.
- La validación en vivo en el menú principal para un jugador, iniciado desde Steam, confirmó que F10 abre el panel de ocho áreas y una segunda pulsación lo cierra sin la aserción ni el fallo. No se cargó campaña ni batalla. Esta comprobación no valida el comportamiento del Modding Kit en ejecución.
- La versión sigue en 25.2.0; no cambiaron API pública, rutas, comandos, permisos, dependencias, TPAC ni ZIPs.

### Playbook detallado adaptable y disposición desplazable (Rev074) — 27/09/2026

- Se movió el Playbook detallado debajo de las tarjetas de contexto y se reservó una columna derecha para que no cubra controles ni filas activas. En modo detallado, los botones de acción se reducen a 100 DIP y el contenido del Playbook ahora usa desplazamiento con altura adaptable.
- Se hizo que Ayuda de teclas y Playbook sean excluyentes y se extendió a seis superficies desplazables la auditoría de barras Gauntlet.
- Pasó el BAT completo de pruebas, incluidas Desktop 63/63 y 289 casos del render WPF; el BAT de comprobaciones visuales Gauntlet pasó con cero errores y cero advertencias de auditoría visual.
- La validación en vivo de esta disposición y desplazamiento adicional sigue pendiente de confirmación; las comprobaciones BAT no demuestran el comportamiento renderizado dentro de Bannerlord.
- La versión sigue en 25.2.0; no cambiaron API pública, rutas, comandos, permisos, dependencias ni ZIPs.

### Ajuste de texto del Playbook y validación exacta de la barra (Rev075) — 27/09/2026

- Se añadió ajuste en límites de palabra para el texto traducido del Playbook detallado y de solución de problemas, conservando los textos fuente, rutas y comandos. El Playbook generado usa un flujo vertical recortado con `CoverChildren` y deja la acción de macro como último elemento desplazable y alcanzable.
- La auditoría Gauntlet ahora distingue el tipo nativo `ScrollbarWidget` por coincidencia exacta y rechaza explícitamente la escritura inválida `ScrollBarWidget`; también comprueba la cadena de ancestros del flujo y el orden del contenido del Playbook.
- Tras corregir una expectativa obsoleta de la auditoría decorativa sobre `CoverChildren`, sin relajar los límites de disposición, `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` terminó con código 0 e informó 338 aprobadas y 0 fallidas.
- El registro de la sesión F10 indicó alternancia de apertura y cierre, pero la captura asociada no mostró el overlay. La aprobación visual en vivo de este seguimiento del Playbook sigue pendiente.
- La versión sigue en 25.2.0; no cambiaron API pública, rutas, comandos, permisos, dependencias ni ZIPs.

### Renovación heráldica ilustrada de Gauntlet y evidencia acotada de F10 (Rev076) — 27/09/2026

- Se sustituyeron las ilustraciones del espacio de identidad de la cabecera y del fondo del rail de navegación por dos sprites heráldicos originales y locales: `forge_heraldic_header_v2` (256×48, alfa máximo 88/255) y `forge_heraldic_rail_v2` (128×256, alfa máximo 64/255). Los IDs de widgets existentes siguen siendo pasivos; el arte del rail se centra a su ancho nativo de 128 DIP y limita su altura al espacio disponible del viewport.
- Se regeneraron el atlas fuente y `SpriteData` mediante el canal de recursos del proyecto. El atlas mide 4096×512 y contiene 32 registros (17 iconos de juego y 15 sprites decorativos). La auditoría estructural ahora comprueba las referencias, los límites alfa y la geometría del rail en los perfiles de viewport admitidos.
- Pasaron `tools/Prepare-CalradiaForge-ImageGenTextures.bat --check --no-pause`, `tools/Test-CalradiaForge-DecorativeTextureDeterminism.bat --no-pause` y `tools/Validate-CalradiaForge-DecorativeSprites.bat --no-pause`. `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` pasó con 0 errores/advertencias de auditoría y Core 339/339; `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` pasó Core 339/339 y ForgeWeave 73/73. La compilación Release de la solución terminó sin advertencias.
- El TPAC instalado se conservó y es anterior a este atlas; la importación por Resource Browser y la inspección visual en el juego siguen pendientes. No se inició el juego en esta ronda.
- La evidencia de F10 se limita al código y diagnósticos persistidos: los SHA-256 del prefab fuente e instalado coinciden (`5741D7571C254B6487477D601F0CFE016AA254A1DFC6BD6795766274543B2F45`) y ambos usan la escritura canónica `ScrollbarWidget`. El registro de una sesión anterior indica apertura/carga y cierre, y pasó la regresión de alternancia F10/telemetría de capa. No se reprodujo nuevamente el crash; la causa exacta de la aserción histórica y si está resuelta siguen sin verificarse.
- La versión sigue en 25.2.0; no cambiaron API pública, rutas, comandos, permisos, dependencias, TPAC ni ZIPs.

### Cronología de hashes del prefab F10 y límites de la instrumentación (Rev077) — 27/09/2026

- Aclara que el hash coincidente del prefab fuente e instalado anotado en Rev076, `5741D7571C254B6487477D601F0CFE016AA254A1DFC6BD6795766274543B2F45`, se midió antes de regenerar la fuente. El prefab fuente regenerado ahora tiene `C7BDE061D57F142797DF7B046BC93437E53049351A9D1C1821F6326CDC3BAC31`; el prefab instalado conserva el hash anterior porque los recursos fuente nuevos no se importaron ni desplegaron. Ambos usan `<ScrollbarWidget>` de forma canónica y omiten `<ScrollBarWidget>`; esto no determina la causa ni la resolución de la aserción histórica de F10.
- `SubModule.Open()` ahora registra hitos ordenados de carga y finalización de la carga del archivo de brushes y la película del panel. La regresión comprueba estáticamente el orden del código fuente; no es una captura de telemetría en tiempo de ejecución ni reproduce la aserción. La causa/resolución, la importación en Resource Browser y el render en vivo siguen sin verificarse o pendientes.
- La versión sigue en 25.2.0; esta aclaración no cambia API, rutas, comandos, permisos, TPAC ni ZIPs.

### Explorador de resultados de pruebas Gauntlet (Rev081) — 28/09/2026

- Se añadió un inspector, limitado a la vida del panel, para la respuesta estructuralmente válida más reciente de `run` o `run-batch`. Una lista desplazable muestra el ID, el estado y la duración; al seleccionar un resultado se muestran su semilla, contexto, `StartedAt`, pasos, error y error de limpieza. El parser conserva como máximo 51 registros como límite defensivo de presentación; el `TestEngine` actual acepta lotes de 1 a 50 pruebas y rechaza los mayores.
- Abrir el inspector o seleccionar una fila es una acción de solo lectura y no ejecuta ni repite pruebas. Los estados de prueba fallidos se conservan explícitos. No cambian la salida original del ledger, los argumentos, el historial, el filtrado ni la comparación de salidas; no se añade una ruta, comando, API pública o superficie de protocolo.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` pasó las auditorías de sprites y prefab/disposición con 0 errores y 0 avisos; su suite Core pasó 343/343. `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` pasó las compilaciones `net472` y `net8.0` limpias, Core 343/343 y ForgeWeave 73/73. Pasaron los bindings estructurales y los 13 recursos de idioma generados.
- Pasó la regresión existente de flanco ascendente F10 y ciclo de vida/telemetría de GauntletLayer. Es una comprobación de estructura de código fuente, no una prueba de entrada en vivo; la aserción nativa anterior y el comportamiento del overlay en el juego siguen sin verificarse, y no fue necesario modificar `SubModule.cs`.
- La inspección dentro del juego sigue pendiente. El TPAC instalado es anterior al atlas fuente actual; las comprobaciones de fuente no demuestran la importación del TPAC ni el render de Gauntlet en vivo.
- La versión del producto sigue en 25.2.0; no se regenera ningún ZIP.

### Correcciones de disposición Gauntlet y revisión de fuente F10 (Rev082) — 28/09/2026

- Se reajustó el ledger normal de evidencia para reservar al menos 160 DIP en el viewport de auditoría 1280×720. Se separaron las celdas de estado y duración del explorador de resultados, se limitó el texto a cada columna y se exigieron al menos 6 DIP de separación.
- Se retiraron de la generación activa de sprites los ocho adornos heredados `forge_header_*_v1`, creados a 128×64 y dibujados a 24×12. Sus copias master, preparadas y de SpriteParts se conservan en `assets/gauntlet-imagegen/archive/2026-09-28/` con un manifiesto SHA-256. Se añadieron tres masters locales de ImageGen para la tela cartográfica, el rail heráldico vertical y el tratamiento transparente de cabecera; la comprobación del flujo de preparación de texturas es determinista.
- Se revisaron el flanco ascendente existente de F10 y la ruta de telemetría de `GauntletLayer`. La regresión de fuente no requirió cambios en `SubModule.cs`; esto no demuestra entrada en vivo ni resuelve la aserción nativa histórica.
- Pasó `tools/Prepare-CalradiaForge-ImageGenTextures.bat --check --no-pause`. La ejecución reportada de Core/ForgeWeave pasó Core 343/343 y ForgeWeave 73/73 sin advertencias de compilación. La auditoría visual Gauntlet completa y la regeneración del atlas fuente siguen pendientes para esta revisión.
- La importación por Resource Browser y el render Gauntlet en vivo siguen pendientes; ninguna comprobación actual de fuente demuestra esos resultados. En esta revisión no se observó F10 en vivo ni se cargó campaña o batalla.
- La versión del producto sigue en 25.2.0; no cambian API pública, rutas, comandos, permisos ni ZIPs.

### Importación del TPAC del atlas Gauntlet verificada en Resource Browser (Rev083) — 28/09/2026

- Se importó el atlas generado `ui_calradiaforge_1` mediante Resource Browser con los ajustes de textura existentes. Tras guardar y actualizar, el inspector cargó el recurso como textura de 4096×512; los detalles de ejecución mostraron DXT5 y 13 niveles mip.
- El `ui_calradiaforge_1_tex.tpac` instalado cambió del SHA-256 `69513B5617F026E1F106F6CA6D47CF5C5CE0B5869FFA472E9166B037CA8EDEA6` a `131E623077708032C76790324F31EDC7862BDC6D5ACA79350B4A03C241A0529E`. Se volvieron a comprobar y conservar los respaldos previos del TPAC instalado y fuente.
- El TPAC fuente del espacio de trabajo conserva su hash anterior porque la herramienta de recopilación depende del lector obsoleto de TpacTool; no se usó TpacTool. La importación de Resource Browser está verificada; el render del panel Gauntlet en vivo sigue sin verificarse.
- La versión del producto sigue en 25.2.0; no cambian API, rutas, comandos, permisos ni ZIPs.

### Corrección de importación del atlas en Resource Browser y verificación Gauntlet en vivo (Rev084) — 28/09/2026

- Se corrigió la evidencia de Rev083: **Save** en el inspector de texturas guarda los ajustes de importación; la importación se realizó al seleccionar el atlas y confirmar **Update** en Resource Browser. El navegador volvió a cargar `ui_calradiaforge_1` como textura de 4096×512 (DXT5 en ejecución, 13 niveles mip).
- El TPAC importado y el TPAC fuente recopilado tienen 539 bytes y SHA-256 `1506C5EAECD4BFC752678C6E6CDB3FA87C4A7A51D8B3B7846B15715276D571EF`. Se conservaron y verificaron por hash los respaldos anteriores de instalación y fuente. El valor intermedio `131E6230...` de Rev083 no probaba la importación completada; no se usó TpacTool.
- Tras compilar Client y Modding Kit sin advertencias ni errores, una observación con F10 en el menú principal de Bannerlord mostró el panel y los adornos de textura de cabecera y rail. No apareció una aserción ni un marcador de textura ausente. No se inició campaña, batalla ni prueba.
- Se corrige la referencia de Rev083 a Escape: el usuario cerró Computer Use para reducir consumo de recursos. La versión 25.2.0, API, rutas, comandos, permisos y ZIPs siguen sin cambios.

### Confirmación de reimportación del atlas en Resource Browser (Rev085) — 28/09/2026

- El usuario repitió la importación mediante Resource Browser. El inspector cargado mostró `ui_calradiaforge_1` como textura 4096×512, con origen `B8G8R8A8` y formato de ejecución DXT5 con 13 niveles mip. La importación se realiza seleccionando el archivo y confirmando **Update**; **Save** en el inspector solo guarda los ajustes.
- El TPAC instalado de Steam y el TPAC fuente del espacio de trabajo tienen 539 bytes y coinciden en el SHA-256 `8899A48A407591ADA53573EFC0DD699EA47D2A30F32A0C12C1875003F7993047`. Los respaldos previos y posteriores permanecen fuera del repositorio y se verificaron por hash. No se usó TpacTool.
- No hubo una nueva observación del render en juego ni de F10 después de repetir la importación; el render en vivo sigue pendiente. La versión permanece en 25.2.0; no cambian API, rutas, comandos, permisos, dependencias ni ZIPs.

## Seguimiento del motor de parcheo de Calradia Forge 25.2.0 — 29/09/2026 (Rev086)

- Se elimina el escaneo implícito de parches al iniciar el módulo; la entrada heredada `InitializeGlobalPatches()` permanece como no-op obsoleto. `ForgePatcher.ApplyAll(assembly)` solo se ejecuta de forma explícita y valida el lote suministrado antes de escribir. Patch Preflight sigue siendo de solo lectura y resuelve destinos y callbacks declarados, IDs duplicados, conflictos y orden sin cargar ensamblados ni invocar callbacks.
- Se agrega `IForgePatchService` opcional mediante `ForgeApi.Patches`; `ForgeApi.Version` avanza de 10 a 11 sin cambiar los implementadores de `IForgeRegistry`. `ForgeDetour`, `MethodSwapper` y los registros de parches comparten una ruta de escritura y verificación. La reversión comprueba bytes exactos, informa como conflicto las modificaciones ajenas y solicita vaciar la caché de instrucciones mientras comprueba los cambios de protección de páginas ejecutables. Se agregan `cf.patch_status [owner]` y `cf.patch_revert <id|owner|all>`; Patch Preflight por IPC continúa siendo de solo lectura.
- El backend sigue siendo experimental: no suspende hilos, no decodifica ni reubica instrucciones sobrescritas y no garantiza seguridad mientras se ejecuta el método objetivo. Esta entrada no incluye una prueba en tiempo de ejecución de Bannerlord ni del Modding Kit. El fixture desechable x64 y su integración con BAT ya están presentes, pero la ejecución y los resultados por BAT y la regresión completa siguen pendientes; aquí no se afirma ningún resultado aprobado.
- La versión del producto permanece en 25.2.0; ForgeWeave y los ZIP de distribución no cambian.

## Seguimiento de validación del motor de parcheo de Calradia Forge — 29/09/2026 (Rev087)

- Se completó el fixture aislado de detour x64 mediante `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat --no-pause`. El launcher compila en una carpeta temporal única y elimina solo esa salida; el fixture serial verificó la secuencia `15 → 32 → 15` y la reversión exacta.
- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` pasó con compilaciones `net472` y `net8.0` limpias, cero advertencias y cero errores, Core 361/361, el fixture nativo aislado y ForgeWeave 73/73. Las regresiones incluyen reversión de lote explícito, reconexión con conflicto y recuperación manual, propiedad del servicio, incertidumbre de bytes y colisiones del comando reservado `all`.
- Este seguimiento actualiza el estado pendiente de pruebas de Rev086 con evidencia BAT ya completada. No se inició una sesión de Bannerlord ni del Modding Kit. Los fixtures llaman los destinos en serie y no demuestran seguridad si otro hilo pudiera ejecutarlos durante una escritura; el backend sigue siendo experimental.
- La versión permanece en 25.2.0; la capacidad del SDK usa la versión 11. No cambian ForgeWeave, las rutas de escritura IPC ni los ZIP de distribución.

## Seguimiento de seguridad del motor de parcheo de Calradia Forge — 29/09/2026 (Rev088)

- Rechaza antes de cambiar la protección de memoria las escrituras de detour que crucen el límite de una página del sistema; agrega regresiones de frontera y ausencia de escritura. `TypeReference.From` y Patch Preflight conservan y comparan el propietario/posición de parámetros genéricos (`!0` frente a `!!0`) para impedir que parámetros de tipo y método con el mismo nombre se confundan.
- Agrega la capacidad opcional separada `IForgePatchServiceLifecycle`, que reabre un servicio integrado desconectado únicamente si cada registro anterior y sus bytes originales se verifican como revertidos y no queda un destino rastreado. Mantenerla separada evita exigir nuevos miembros a implementaciones existentes de `IForgePatchService`.
- El reintento integrado por BAT pasó compilaciones limpias `net472`/`net8.0`, 0 advertencias y errores, Core 363/363, el fixture serial de detour x64 (`15 → 32 → 15`, reversión exacta) y ForgeWeave 73/73. Un intento integrado anterior informó un fallo Core que no pudo reproducirse; pasaron tanto el BAT Core aislado como la ejecución integrada posterior.
- No se inició Bannerlord ni el Modding Kit. El fixture serial no demuestra seguridad frente a ejecución concurrente durante la escritura de código; el backend de detour sigue siendo experimental. La versión permanece en 25.2.0, `ForgeApi.Version` en 11, y no cambian ForgeWeave, las rutas de escritura IPC ni los ZIP de distribución.

## Corrección del preflight de límites de página del motor de parcheo — 29/09/2026 (Rev089)

- Mueve el rechazo de tramos por límite de página en las rutas de parche directo y por lote antes de leer bytes originales o reservar registros. Un lote rechazado valida todos sus tramos antes de leer o reservar cualquier destino, por lo que sus mismos IDs y destinos quedan disponibles para reintentar.
- Agrega regresiones por las rutas públicas `Patch` y `ForgePatcher.ApplyAll`, que verifican que no cambien los contadores de lectura/protección/escritura/flush, que no queden recibos tras el rechazo y que se puedan reutilizar los destinos con un tamaño de página sintético permitido.
- La ejecución más reciente del BAT Core pasó 363/363 regresiones administradas y compiló el fixture desechable `net472` x64 con cero advertencias o errores. Después, el fixture nativo serial se bloqueó y se detuvo; por ello, no se informa como aprobada la ejecución global más reciente del BAT ni el smoke nativo. El BAT del fixture ahora limita la espera del proceso hijo.
- Este seguimiento reemplaza únicamente para el árbol más reciente la afirmación de fixture aprobado en Rev088; conserva Rev088 como registro de la ejecución aprobada anterior. No se inició Bannerlord ni el Modding Kit. El backend de detour sigue siendo experimental y no garantiza seguridad ante ejecución concurrente. La versión permanece en 25.2.0, `ForgeApi.Version` en 11 y los ZIP no cambian.

## Corrección de estado del launcher del fixture Calradia Forge — 29/09/2026 (Rev090)

- El wrapper PowerShell de timeout para el fixture nativo desechable no devolvió el control de forma fiable y se retiró. El BAT del fixture volvió a lanzar directamente el proceso hijo; no se afirma que haya un timeout garantizado.
- El smoke nativo actual sigue bloqueado/sin verificar. Las regresiones administradas Core más recientes pasan 363/363 y el fixture x64 compila limpiamente, pero el BAT Core no termina porque el proceso nativo hijo se bloquea. La evidencia anterior de fixture aprobado permanece limitada al árbol previo, registrado en Rev088.
- Esta entrada corrige únicamente la afirmación de timeout de Rev089. No hubo sesión del juego/Modding Kit ni cambio de ZIP; la versión permanece en 25.2.0 y el motor de parcheo sigue siendo experimental.

## Seguimiento documental y de validación del motor de parches — 29/09/2026 (Rev091)

- Aclara el texto de Gauntlet y los mapas de arquitectura: Patch Blueprint Preflight es una revisión estructural de solo lectura de las referencias declaradas para destino y callback frente a ensamblados ya cargados. No aplica parches ni ejecuta callbacks, y resolver referencias no demuestra que se pueda instalar un reemplazo nativo. Harmony Atlas es un inventario de solo lectura independiente de los parches Harmony presentes en ensamblados ya cargados.
- Corrige la referencia de `ForgeDetour` para mostrar su API real de reemplazo mediante `MethodInfo` y su alcance experimental. El ciclo de inicio y los diagramas ahora reflejan que el módulo no busca ni aplica parches al iniciar, y que el preflight está separado de las solicitudes explícitas de reemplazo.
- Regenera los 13 catálogos nativos con 710 claves coincidentes. La auditoría de localización pasó, incluidas las etiquetas Gauntlet Page Blueprint, su descripción y Page title.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` pasó: compilación completa sin advertencias ni errores; Core 369/369; fixture serial x64 de detour `15 → 32 → 15` con reversión exacta; ForgeWeave 73/73; Desktop 63/63; y 292 casos de render WPF. No se inició Bannerlord ni el Modding Kit.
- El fixture invoca el destino de forma serial y no demuestra seguridad mientras otro hilo pueda ejecutar un destino durante una escritura de memoria. El backend de detour sigue siendo experimental. La versión permanece en 25.2.0, `ForgeApi.Version` en 11, y no cambian la superficie de escritura IPC, el comportamiento de ForgeWeave ni los ZIP de distribución.

## Corrección del host BAT del fixture de detour y localización del panel — 29/09/2026 (Rev092)

- El fixture desechable de detour ahora se compila como una biblioteca `net472` x64 y se ejecuta únicamente mediante `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat`, cargada en un proceso temporal x64 de Windows PowerShell. Ya no crea ni inicia `CalradiaForge.DetourFixture.exe`; el BAT comprueba que no se haya emitido un apphost. La prueba permanece alineada con el runtime de Bannerlord. Un ensayo bajo el JIT de .NET 8 no detectó el reemplazo en la etapa 4 y se descartó, sin reportarlo como fixture aprobado.
- Corrige el texto del juego para dejar claro que Patch Blueprint Preflight es estructural y de solo lectura, que Harmony Atlas sigue siendo un inventario separado de solo lectura y que la guía de ForgeWeave Replay no implica que el preflight aplique detours. Siete claves de copy ya están traducidas en los 13 catálogos nativos.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` pasó la compilación completa con 0 advertencias y 0 errores; ForgeWeave 73/73; Desktop 63/63; y 292 casos de render WPF. El fixture serial x64, alojado por el BAT, devolvió `15 → 32 → 15` con restauración exacta. Cada uno de los 13 catálogos nativos contiene 737 claves y la auditoría de localización es válida.
- El fixture sigue siendo serial y no demuestra seguridad ante ejecución concurrente durante escrituras de código máquina. No se inició Bannerlord ni el Modding Kit, y no se invocó la generación de paquetes en esta corrección. La versión permanece en 25.2.0 y `ForgeApi.Version` en 11.

## Workbench de hooks Prefix/Postfix explícitos de Calradia Forge — 29/09/2026 (Rev093)

- Agrega la capacidad opcional `ForgeApi.Hooks` / `IForgeHookService` para registrar explícitamente hooks Prefix y Postfix mediante MonoMod.RuntimeDetour 25.3.6 en el host Bannerlord `net472`. El registro no instala hooks; los callbacks se ejecutan sincrónicamente en el hilo que llama al destino. Esta capacidad es independiente de la prevalidación declarativa de Patch Blueprints y de la API separada de reemplazo de métodos.
- Agrega acciones protegidas del Hook Workbench en WPF. El operador revisa los snapshots, prepara un plan Apply/Revert, marca la casilla de confirmación y confirma con un token de un solo uso. IPC acepta solo los IDs de hooks seleccionados y el token; los callbacks, delegates y destinos ejecutables no cruzan el pipe. Apply/Revert se limita al contexto exacto del menú principal, en el hilo del juego y sin campaña, misión ni sesión multijugador activa. Los comandos de consola son `cf.hook_status [owner]`, `cf.hook_apply <id>` y `cf.hook_revert <id|owner|all>`.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` pasó Core 372/372 y ForgeWeave 73/73. Las compilaciones Core `net472`/`net8.0` y Mod `net472` terminaron limpiamente. El fixture aislado de detours pasó mediante `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat`; no se ejecutó `DetourFixture.exe`.
- El fixture es serial y no demuestra seguridad frente a un hilo concurrente que ejecute el destino durante la instalación o retirada del hook; el backend sigue siendo experimental. No se realizó una sesión real de Bannerlord o Modding Kit ni una importación de recursos. La versión permanece en 25.2.0; no se generaron ni modificaron ZIP de distribución.

## Salvaguardas de ciclo de vida de hooks y contexto del anfitrión — 30/09/2026 (Rev094)

- Agrega una protección opcional de desconexión para impedir que reemplazar o desconectar el servicio SDK deje hooks activos o inciertos sin administrar cuando el contexto aprobado de menú e hilo del juego no está disponible. Las excepciones de verificación quedan visibles como conflictos, y los handles de hooks retirados externamente se liberan antes de informar una reversión limpia.
- Restringe la puerta del menú de Bannerlord a la identidad CLR exacta de `GauntletInitialScreen`, resuelta en ejecución desde el ensamblado Gauntlet del juego. Si el tipo oficial no se puede resolver, las mutaciones se rechazan; no se aceptan imitaciones basadas solo en el nombre.
- Agrega `hook-plan-cancel`, que solo transporta la sesión del anfitrión y el token de la vista previa. La cancelación queda ligada a ese par exacto; WPF conserva el plan si no puede confirmarla y detiene la actualización de snapshots en vez de descartar el estado local. La documentación del protocolo SDK en inglés y español describe esta acción.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: Core 384/384 y ForgeWeave 73/73 pasaron; las compilaciones pertinentes `net472`, `net8.0` y Mod `net472` terminaron con 0 advertencias y 0 errores. `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat` pasó desconexión protegida, retirada externa, verificación incierta y restauración exacta `15 → 32 → 15`.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause`: Desktop 65/65 y 292 casos de render WPF pasaron. El harness de render informó 13.895 ms en total; es tiempo del harness, no latencia de interacción observada en la aplicación.
- No se inició Bannerlord ni el Modding Kit. El fixture sigue siendo serial y no demuestra seguridad mientras otro hilo ejecuta el destino durante Apply o Revert. La versión permanece en 25.2.0 y no se regeneraron ni modificaron ZIP.

## Invocador tipado de distribución de hooks y benchmark medido — 30/09/2026 (Rev095)

- Sustituye las llamadas normales `Delegate.DynamicInvoke` de Prefix/Postfix por un invocador tipado compilado una vez por registro, almacena metadatos de parámetros, reutiliza los argumentos del callback y clona los originales solo cuando Prefix lo requiere. La validación por BAT pasó Core 394/394, ForgeWeave 73/73, Desktop 65/65 y el fixture desechable de detours `net472` x64; la compilación del fixture mediante BAT informó cero advertencias/errores y sus pruebas de invocador tipado, orden, IDs duplicados, restauración y excepciones pasaron. No se inició directamente ningún EXE del fixture.
- Una corrida BAT de benchmark previa al cambio, con cinco muestras internas, registró p50 de 773,1 ns para Prefix, 656,0 ns para Postfix y 657,9 ns para ambos. La mediana de cinco corridas BAT posteriores fue 218,5 ns, 159,5 ns y 218,6 ns, respectivamente: reducciones aproximadas de 71,7%, 75,7% y 66,8%. Se compara una corrida base con la mediana de cinco corridas posteriores; no es un estudio equivalente de cinco corridas antes y cinco después. Son mediciones del fixture aislado, no de latencia ni tiempo de cuadro de Bannerlord.
- El backend de hooks continúa siendo experimental y el fixture serial no garantiza seguridad si otro hilo ejecuta el destino durante la modificación. No hubo sesión de Bannerlord/Modding Kit ni generación de ZIP. El changelog y anexo fuente avanzan a Rev095, mientras que el DOCX/integridad protegidos permanecen en Rev076 a la espera de reconciliar los anexos Rev086–Rev092 faltantes; no se ejecutaron el generador DOCX ni el registrador.

## Endurecimiento del preflight y reconciliación del registro protegido — 30/09/2026 (Rev096)

- Se endurece el preflight de ForgeDetour antes de acceder a memoria ejecutable: rechaza P/Invoke, InternalCall y varargs; valida receptores implícitos de instancia y modificadores de firma requeridos/opcionales; y bloquea destinos reservados por el servicio de hooks. Las regresiones Core comprueban que los rechazos no lean ni modifiquen memoria, no cambien protecciones ni vacíen la caché, y no dejen recibos.
- Se corrige el código de salida del BAT cuando falla la compilación del fixture desechable y se valida la ruta real de snapshots inmutables en la auditoría de code smells. El fixture net472 x64 permanece alojado por BAT; no se inicia directamente su EXE.
- tools/Run-CalradiaForge-Tests.bat --core-only --no-pause <nul pasó con compilaciones net472/net8.0 limpias, Core 395/395, ForgeWeave 73/73 y fixture serial x64. Estas pruebas no demuestran seguridad ante ejecución concurrente del destino; el backend sigue siendo experimental.
- El DOCX protegido y la cadena de integridad se reconciliaron hasta Rev095. Los anexos fuente Rev086–Rev094 se reconstruyeron como respaldos a partir del changelog bilingüe publicado; las revisiones protegidas 001–076 siguen intactas. Esta entrada corrige la nota obsoleta de reconciliación pendiente en Rev095 sin editarla.
- La revisión de fuentes públicas no comprobó una auditoría independiente publicada para MonoMod RuntimeDetour; esto no descarta trabajo privado o no indexado. Los hooks ejecutan código de extensiones confiable dentro del proceso. No se inició Bannerlord/Modding Kit ni se generaron ZIP; la versión 25.2.0 y ForgeApi.Version 12 permanecen sin cambios.

## Recuperación de desconexión de hooks y callbacks que se retiran — 30/09/2026 (Rev097)

- El `TestEngine` integrado ahora reenvía el guard opcional de desconexión de hooks y conserva su constructor público sin parámetros. Una llamada directa a `ForgeApi.Disconnect()` rechazada mientras `Runtime` aún recibe ticks conserva la ruta de recuperación publicada. Un `Runtime.Dispose()` / `OnSubModuleUnloaded` real cierra el pipe en `finally`; no hay recuperación por pipe después de la descarga y esa ruta sigue sin verificarse en un host en vivo.
- El ciclo de vida del dispatch conserva el trampoline de MonoMod de una llamada actual si su callback revierte o libera su propio handle: una solicitud durante un dispatch activo queda pendiente sin intentar ni declarar `Undo` como verificado; al terminar los dispatches, el servicio vuelve a comprobar el gate del host y después verifica `Undo`, dispone el hook y libera la reserva del destino. Las llamadas posteriores usan el destino original. Un fallo de limpieza diferida permanece visible como conflicto. Esto no amplía el gate exacto de menú principal/hilo del juego ni certifica concurrencia arbitraria de hooks.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause <nul` compiló net472/net8.0 con cero advertencias/errores, pasó Core 395/395 y ForgeWeave 73/73, y ejecutó el fixture serial x64 mediante su host BAT/PowerShell. No se inició directamente ningún EXE de pruebas.
- La documentación ahora aclara que la ACL del pipe por SID no autentica que el cliente sea el proceso WPF; su casilla/token es el flujo normal de confirmación, no una frontera de identidad frente a otro proceso del mismo usuario. No se afirma haber probado una descarga real del módulo, una sesión de Bannerlord, campaña, batalla, una auditoría independiente de MonoMod ni la generación de ZIP. La versión 25.2.0 y `ForgeApi.Version` 12 permanecen sin cambios.

## Evidencia BAT de limpieza de parches y hooks — 30/09/2026 (Rev098)

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause` terminó correctamente. Pasaron las compilaciones y las suites de Core y ForgeWeave, con cero advertencias y cero errores de compilación.
- `tests\CalradiaForge.DetourFixture\Run-DetourFixture.bat --no-pause` terminó correctamente. La compilación del fixture no tuvo advertencias ni errores, y el fixture pasó mediante su ruta alojada por BAT; no se inició directamente `DetourFixture.exe`.
- Esta entrada registra únicamente evidencia BAT de los cambios actuales de limpieza de parches y hooks. No agrega afirmaciones más allá de los comandos comunicados. La versión del producto permanece en 25.2.0.

## Evidencia BAT integrada de parches, hooks y Desktop — 30/09/2026 (Rev099)

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause` terminó con código 0: pasaron Core 397/397 y ForgeWeave 73/73; las compilaciones seleccionadas `net472` y `net8.0` informaron cero advertencias y errores.
- El BAT anidado del fixture serial x64 de detours pasó con callbacks inertes durante la descarga y restauración exacta `15 → 32 → 15`. No se inició directamente `DetourFixture.exe`.
- `tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause` pasó Desktop 65/65 y 292 casos de render; la compilación informó cero advertencias y errores.
- Esta entrada registra únicamente la evidencia BAT integrada comunicada para esta ronda de validación.
