# Guía de Calradia Forge Desktop 25.2.0

Esta guía describe el árbol de fuentes 25.2.0 de Calradia Forge Desktop, una mesa de trabajo MVVM independiente que usa CommunityToolkit.Mvvm, MaterialDesignThemes y AsmResolver. El archivo existente `CalradiaForge-Desktop-25.2.0.zip` es una instantánea empaquetada anterior y permanece sin cambios con las modificaciones de código Rev066; vuelve a compilar y empaquetar las fuentes antes de esperar que estos cambios aparezcan en una distribución. Mantén juntos los archivos de cada versión Desktop y no combines ensamblados de versiones distintas.

La herramienta **FBX ASCII Preflight** enumera nombres declarados de modelos, geometrías y materiales, muestra saltos de nombres LOD y registra nodos Camera/Light como evidencia para revisión; sugiere comprobar si la exportación se limitó intencionalmente a objetos seleccionados. El analizador no infiere la intención ni borra nodos. El token de cancelación de Desktop llega al recorrido de carpetas y al análisis línea por línea; el BAT de consola se puede interrumpir desde su ventana. Los recorridos de carpetas usan una búsqueda explícita, omiten puntos de reanálisis tanto en la raíz seleccionada como dentro de ella y respetan la cancelación entre carpetas y entradas. Cada búsqueda se limita a 10.000 carpetas incluida la raíz, profundidad 64, 200.000 entradas del sistema de archivos y el valor solicitado de `MaximumFiles` limitado a 1–2.000 (predeterminado 1.000). Si se alcanza un límite, una ruta no se puede leer o se omite un punto de reanálisis, el resultado se marca truncado con una advertencia `analysis_scan_incomplete`. El análisis de archivos comprimidos compara el tamaño con `MaximumBytesPerFile` antes de crear `ZipArchive` o inspeccionar entradas; el límite predeterminado es 4 MiB y el máximo general de entrada es 16 MiB. Los archivos demasiado grandes producen `archive_size_limit` sin inspeccionar entradas ZIP. Los archivos aceptados inspeccionan hasta `MaximumFiles` entradas e indican como truncadas las omitidas. El FBX binario no es compatible; no resuelve materiales contra recursos del juego y no importa ni compila. Su salida es evidencia estructural de nombres, no un resultado de importación. La herramienta **Gauntlet Sprite Package Auditor** también comprueba IDs y dimensiones de hojas en `SpriteData`, referencias de piezas, límites de sus rectángulos, rutas seguras y dimensiones IHDR de PNG fuente y atlas. Solo lee firma y cabecera de 24 bytes; no decodifica píxeles, valida CRC ni certifica el aspecto gráfico. Para TPAC, lee la cabecera v2 de 36 bytes, limita el conteo declarado a 1–100.000 y comprueba que la tabla de contenido quede dentro del archivo; no analiza entradas ni datos de textura. Una cabecera plausible es evidencia estructural parcial. Usa `tools/Inspect-CalradiaForge-Tpac.bat --validate-only` para el preflight separado de metadatos con `TpacTool.Lib`; el fallo de un lector se informa aparte y no demuestra por sí solo que el archivo esté corrupto.

### Análisis de obsolescencia de API — No disponible
La ruta `ApiDeprecationChecker` se conserva en el catálogo de Desktop para mantener una identidad de navegación estable, pero el análisis de obsolescencia no está disponible hasta que un catálogo de API de TaleWorlds verificado y versionado identifique la versión exacta que cubre. La ruta no clasifica el uso de API como obsoleto ni vigente; su presencia en el catálogo o una salida genérica de análisis no constituye evidencia de compatibilidad. Solo debe habilitarse un análisis útil después de revisar la fuente del catálogo y su cobertura de versiones.

### Evidencia de diagnóstico y análisis de ForgeWeave
Las heurísticas de código fuente de Core enmascaran comentarios y literales de C# antes de comprobar reglas basadas en texto; siguen siendo un análisis textual acotado, no diagnósticos del compilador. La cancelación se respeta al procesar cada archivo de código o localización. Un archivo ilegible o demasiado grande produce un hallazgo localizado y no impide analizar los archivos posteriores. El ledger de Desktop muestra el ID de regla, la ubicación de origen, la evidencia y la recomendación; la exportación conserva esos campos en el valor heredado de detalle. Una respuesta de transporte ForgeWeave recibida correctamente se marca `Unparsed` si la carga falta, está vacía o malformada, o carece de los metadatos obligatorios del snapshot. El transporte permanece como `Received`, el motivo se limita a 240 caracteres y la carga queda disponible para revisión. `Received` y `Unparsed` no significan que la operación se completó ni que el resultado se verificó.

### Diagnósticos de parches
El cliente Desktop usa la acción de protocolo `patch-diagnostics` para mostrar un `ForgePatchDiagnosticsSnapshot` acotado de hooks y parches de reemplazo propiedad de Forge. Su observador externo opcional comprueba la superficie pública estática de consulta esperada `HarmonyLib.Harmony.GetAllPatchedMethods()` y `GetPatchInfo(MethodBase)` en un ensamblado ya cargado llamado `0Harmony`; es una señal diagnóstica acotada del runtime, no una prueba de compatibilidad con una versión o combinación de mods. Forge no carga Harmony ni incorpora su ensamblado como referencia de compilación o distribución. La consulta se ejecuta sincrónicamente dentro del proceso: Forge limita la enumeración de ensamblados y destinos y la salida copiada, pero no puede limitar el tiempo de CPU ni las asignaciones que realicen internamente los métodos de consulta o getters de Harmony. No es un sandbox ni un verificador de autenticidad. El observador es de solo lectura y no detecta otros backends de parches. El alias actual de consola dentro del juego es `cf.patch_diagnostics`; el alias anterior `cf.harmony_summary` quedó retirado.

### Auditor Avanzado de Ensamblados y Guardado (AsmResolver)
Las rutas `SaveTypeDefinerAuditor` y `CampaignNamespaceGuard` admiten binarios `.dll` o `.exe` compilados además de carpetas de código fuente. Mediante AsmResolver, el auditor estático inspecciona los metadatos administrados sin cargar ni ejecutar el ensamblado:
- **GEMINI.md Regla A (Anti-Shadowing):** Comprueba que ningún tipo ni espacio de nombres oculte `TaleWorlds.CampaignSystem.Campaign` o `TaleWorlds.Localization`.
- **Regla B (Contrato de Comportamiento Sin Estado):** Audita clases que heredan de `CampaignBehaviorBase`, garantizando cero atributos `[SaveableField]` o `[SaveableProperty]`.
- **Seguridad de Base ID en SaveableTypeDefiner:** Desensambla el IL del constructor verificando que el identificador base sea $\ge 2.500.000$, evitando colisiones con el motor (0–100.000) u otros mods.
- **Riesgo de Serialización Directa MBGUID:** Alerta ante campos que serializan directamente entidades del motor (`Hero`, `MobileParty`, `Settlement`, `Clan`, `Kingdom`) y recomienda desacoplar mediante `StringId`.
- **Metadatos de Seguridad para Distribución:** Audita los atributos de ensamblado requeridos (`Company`, `Product`, `Description`, `Copyright`) para prevenir falsos positivos en motores antivirus.

### Panel de Telemetría APM y Malla de Eventos ForgeWeave en Vivo
A través de la acción `framework` por named pipes, la ruta `GauntletLivePreview` transforma las capturas del juego en un panel APM táctico:
- **Interruptores de Circuito (Circuit Breakers):** Muestra transiciones de estado en vivo (`[CLOSED]`, `[HALF-OPEN]`, `[OPEN]`) con contadores de fallas e intervalos de recuperación.
- **Percentiles de Latencia:** Calcula y muestra percentiles P50, P95 y P99 junto a conteos de invocación.
- **Histogramas de Distribución:** Representa la dispersión temporal mediante barras ASCII (`[<1ms | 1-5ms | 5-20ms | >20ms]`).
- **Integración con el Libro de Evidencias:** Registra evidencias estructuradas por manejador en el libro interactivo de la interfaz.

### Simulador y Despachador de Eventos (Consola en Vivo)
La ruta `LiveConsole` normaliza automáticamente comandos interactivos hacia acciones de ForgeWeave (`cf.forgeweave.publish`, `cf.forgeweave.unquarantine`, `cf.forgeweave.replay`, `cf.forgeweave.status`, `cf.forgeweave.handlers`, `cf.forgeweave.journal` y `cf.forgeweave.clear`). Permite emitir eventos personalizados o restablecer manejadores en cuarentena directamente desde el escritorio.

### Paleta de Comandos Táctica (Ctrl+K)
La paleta de comandos por teclado incluye filtrado por prefijos tácticos:
- Prefijos admitidos: `>live` (Sesión en vivo), `>diag` (Diagnósticos y seguridad), `>asset` (Recursos y sintetizadores), `>sim` (Simulación y balance), `>econ` (Economía), `>pol` (Política), `>camp` (Campaña), `>comb` (Combate), `>gaunt` (Gauntlet), `>deliv` (Entrega), `>gen` (Generadores), `>rep` (Informes) y `>asm` (Editor e inspector de ensamblados).
- Al pulsar `Enter` se selecciona y activa inmediatamente la primera herramienta coincidente y se cierra la paleta.
- La selección de cualquier herramienta cierra automáticamente el panel superpuesto.

### Estudios Gráficos Interactivos de Simulación y Split Deck (Rev030)
El banco de trabajo táctico evoluciona todos los motores de simulación y diagnóstico desde texto sin formato a visualizadores gráficos WPF de primer nivel, inmediatamente visibles e interactivos:
1. **Árbol Gráfico Interactivo de Progresión de Tropas (`TroopTreeVisualizer` / `ItemBalanceAnalyzer`):**
   - Renderiza árboles de tropas imperiales y líneas dinásticas mediante tarjetas jerárquicas en 6 niveles (T1–T6) con flechas direccionales de mejora.
   - Muestra tarjetas de atributos de combate (Salud, Armadura, Velocidad, Salario, Rol) con insignias de nivel coloreadas y medidores dinámicos.
   - Precarga instantánea con datos canónicos al seleccionar la herramienta y comparador semántico XML (`fileA.xml|fileB.xml`) para detectar entidades añadidas, eliminadas o modificadas.
2. **Estudio Táctico de Audio y Osciloscopio de Onda (`AudioFmodMixerInspector` / `SoundXmlSynthesizer`):**
   - Visualizador acústico en vivo con osciloscopio de forma de onda sobre lienzo gráfico (`Polyline` continua sobre muestras dinámicas de audio).
   - Ecualizador de 5 bandas de frecuencia (`Sub-Bass`, `Bass`, `Mid`, `Presence`, `Brilliance`) con lecturas en decibelios y barras dinámicas de nivel.
   - Vúmetros estéreo de pico dual (`L` / `R`) con umbrales de advertencia de margen dinámico (-1.4 dBFS) conforme a `bannerlord_audio_system.md`.
3. **Simulador Gráfico de Talleres y Economía (`WorkshopEnterpriseSimulator` / `SettlementCalculator`):**
   - Gráfico de barras horizontales comparativas de beneficio entre 7 tipos de talleres (Platería, Herrería, Cervecería, Telar, Carpintería, Alfarería, Prensa de Olivas) con gradientes de color.
   - Indicadores de cuenta regresiva para el período de amortización de capital (41.1 días para Platería óptima) y balance de materias primas.
   - Medidores en tiempo real del equilibrio cívico de lealtad e Índice de Riesgo de Rebelión ($R_{rebellion}$).
4. **Inspector Visual de Memoria Cognitiva CoALA (`SaveInspector` / `ObjectInspector`):**
   - Tarjetas para la arquitectura de 3 niveles: Memoria de Trabajo (tarea actual, intención, formación táctica), Memoria Episódica (flujo cronológico FIFO con marcas de tiempo y relevancia emocional) y Memoria Semántica (red de creencias con barras de progreso de expiración TTL).
   - Medidor de capacidad global que visualiza el uso del límite de 2.048 slots de agentes (`ForgeAgentMemory` SDK).
   - Selector interactivo de héroes para inspección inmediata del estado cognitivo.
5. **Espacio de Trabajo Split Deck Elevado en Cabecera (`[Ctrl+D]`):**
   - Integrado de forma prominente en la barra de cabecera táctica con indicador activo de verdigrís y etiqueta de atajo `[Ctrl+D]`.
   - Lienzo comparativo en paralelo que mantiene la salida, estado y libro de evidencias de la herramienta anclada mientras se opera en el workbench principal.
   - Atajo de teclado directo (`Ctrl+D`), acciones de anclar, limpiar y cerrar con leyendas accesibles.

### Barra de Acciones Tácticas y Botones Operativos Contextuales (Rev031)
El panel de mando táctico evoluciona la botonera de operaciones desde una barra genérica monótona hacia una barra de acciones contextual y diferenciada en las 194 rutas del catálogo:
- **Verbos Semánticos Dinámicos y Acentos de Paleta:** En lugar de reiterar un botón genérico "Run work order", cada ruta proyecta un verbo contextual, icono vectorial y borde de acento según su dominio:
  - *Simulaciones y Balance (`VerdigrisBrush`):* `"⚡ Simular Progresión"`, `"🔬 Analizar Espectro"`, `"📊 Simular Economía"`, `"🧠 Auditar Memoria"`.
  - *Auditorías y Seguridad (`BrassBrush`):* `"🛡️ Auditar Ensamblado"`, `"🔍 Verificar Anti-Shadowing"`, `"📋 Validar Manifiesto"`.
  - *Generadores y Andamiaje (`DeepPineBrush`):* `"⚙️ Generar Andamiaje"`, `"📜 Sintetizar XML"`, `"🔨 Forjar Componente"`.
  - *Sesión en Vivo e IPC (`EmberBrush`):* `"📡 Consultar Telemetría"`, `"⚡ Despachar Evento"`.
- **Botones de Acción Rápida Secundaria:**
  - *Alternador de Escenarios Canónicos (`LoadPresetCommand` / `PresetActionButton`):* Presente en rutas de simulación visual, permitiendo alternar instantáneamente entre escenarios predefinidos (árbol imperial vs. línea noble, muestra de combate vs. fanfarria UI, economías de Marunath vs. Epicrotea, y perfiles cognitivos de Rhagaea vs. Garios vs. Lucon) sin requerir selección manual de archivos.
  - *Limpiador de Entrada (`ClearInputCommand` / `ClearInputButton`):* Botón contextual visible cuando existen parámetros o rutas escritas para vaciar el campo en un clic.
- **Invariantes de Contrato y Accesibilidad:** Preserva todos los identificadores `AutomationProperties.AutomationId` requeridos (`RunWorkOrderButton`, `CancelWorkOrderButton`, `ExportWorkOrderButton`, `BrowseFileButton`, `BrowseFolderButton`) y enlaces MVVM para garantizar compatibilidad total con pruebas de regresión y automatización.

### Personalización Estética de Operaciones y Visualizadores de Dominio (Rev032)
El banco de trabajo eleva la diferenciación visual desde botones contextuales hacia una identidad operacional completa en las 194 rutas del catálogo:
- **Superficies de Identidad Operacional:**
  - *Estandartes Tácticos de Categoría y Lemas:* Cada herramienta renderiza un estandarte de dominio (p. ej. `[ CLR ASSEMBLY & PE METADATA AUDITOR ]`, `[ GEOPOLITICAL DIPLOMACY & SENATE ]`, `[ COMPONENT BLUEPRINT & XML SYNTHESIZER ]`) y un lema doctrinal militar en latín (p. ej. *"Integritas Codicis et Fides Salutis"*, *"Si Vis Pacem, Para Bellum"*, *"Ferrum et Forma ad Victoriam"*).
  - *Acentos Dinámicos de Paleta por Categoría:* Bordes de tarjeta, franjas decorativas y glifos vectoriales heráldicos se enlazan dinámicamente a los pinceles de paleta de la categoría (`VerdigrisBrush`, `BrassBrush`, `DeepPineBrush`, `EmberBrush`, `TemperedSteelBrush`).
  - *Insignias de Dominio de Entrada y Píldoras de Seguridad:* El panel de parámetros muestra insignias explícitas de formato (`PE/CLR BINARY`, `MODULE DIRECTORY`, `XML FRAGMENT`, `IPC PIPE COMMAND`) y píldoras de clasificación de seguridad (`READ-ONLY / ZERO CLR EXECUTION`, `GUARDED MUTATION / ATOMIC BACKUP`, `AIR-GAPPED STATIC SCAN`).
- **5 Nuevos Visualizadores Gráficos de Primer Nivel (Ampliando a 8 Estudios Especializados + 1 Cabina de Mando):**
  1. *Radar de Seguridad PE y Metadatos CLR (`SaveTypeDefinerAuditor` / `CampaignNamespaceGuard` / `AssemblyInspector` / `AssemblyVersionPatchLab`):*
     - Tarjetas de desglose de metadatos CLR (Tipos Declarados, Tabla de Métodos, Referencias de Ensamblado, Conteo de Resolución StringId).
     - 5 barras de cumplimiento de seguridad: Cumplimiento Anti-Shadowing, Garantía de Comportamiento Sin Estado, Base ID en SaveableTypeDefiner ($\ge 2.500.000$), Desacoplamiento StringId de MBGUID y Autenticidad de Metadatos de Ensamblado.
     - 3 preajustes canónicos de auditoría alternables mediante `PresetActionButton`.
  2. *DAG de Topología de Módulos y Jerarquía de Dependencias (`ModConflictMatrix` / `DependencySorter`):*
     - Árbol topológico de dependencias que enlaza módulos nativos (`Native`, `SandBoxCore`, `SandBox`, `StoryMode`) con nodos del mod, mostrando orden de carga, versiones e insignias de conflicto.
     - Métricas topológicas: Módulos Totales, Dependencias Upstream, Puntuación de Riesgo de Conflicto e Índice de Orden de Carga.
     - 3 escenarios predefinidos de dependencias de módulos alternables mediante `PresetActionButton`.
  3. *Barómetro de Diplomacia Geopolítica y Senado (`DiplomaticMatrix` / `WarCasusBelliEngine` / `DynasticSuccessionEvaluator` / `SDK_ForgeDiplomacyEngine`):*
     - Tarjetas de comparación bilateral entre reinos (Imperio vs Vlandia, Battania vs Imperio Occidental, Aserai vs Khuzait, Sturgia vs Imperio del Norte) con medidores de tensión de Casus Belli, balance de tributos y estado de alianzas.
     - Medidor de Consenso de Votación del Senado multifacción (Apoyo %, Oposición %, Abstención %) con indicadores de flujo de tributos.
     - 3 preajustes geopolíticos de conflicto alternables mediante `PresetActionButton`.
  4. *Estudio de Síntesis de Planos de Componentes (`SoundXmlSynthesizer` / `TroopXmlSynthesizer` / `ItemXmlSynthesizer` / `WeaponCraftingForge` / `BrushSynthesizer` / `XmlSnippetForge`):*
     - Flujo de síntesis interactivo en 3 etapas: Validación de Esquema, Composición de Árbol de Entidades XML y Vinculación de Manifiesto.
     - Tarjetas de previsualización estructural con insignias de sintaxis (`ID`, `Type`, `Mesh`, `Tier`, `Modifier`, `Flags`).
     - 3 planos predefinidos alternables mediante `PresetActionButton`.
  5. *Cabina de Vuelo Operacional (`GenericOperationDashboardViewModel`):*
     - Para rutas analíticas y de diagnóstico, provee telemetría operacional estructurada, inspector de parámetros de entrada e indicadores de seguridad de ejecución.

### Ocultación Condicional de Entrada y Dossier Táctico de Comandos (Rev033)
- **Ocultación Dinámica de Barra de Entrada:** En herramientas y estudios donde no se requieren parámetros de archivo (Tool.RequiresInput == false), la caja de texto de entrada se colapsa automáticamente, mostrando una insignia de ruta autónoma y optimizando el espacio visual para las barras de acción y los lienzos interactivos.
- **Dossier Táctico y Referencia de Comandos:** Cada ruta incorpora un dossier táctico integrado (SectionDossierCard) que detalla:
  1. *Contexto Operativo del Dominio:* Propósito, límites de seguridad del motor (sin estado, seguro para subprocesos, sandbox) y flujo de datos.
  2. *Comandos de Consola del Motor:* Comandos in-game asociados de Bannerlord y Gauntlet (cf.*, campaign.*) con botón de copiado rápido.
  3. *Atajos de Teclado del Banco de Trabajo:* Referencia directa de combinaciones de teclas (Ctrl+Enter, Ctrl+D, Ctrl+P, Ctrl+E, Esc, Ctrl+F).
  4. *Invocación CLI sin Cabezal:* Sintaxis exacta de línea de comandos para scripting y CI/CD (CalradiaForge.Desktop.exe --tool <Id>).

### Cambios de Código WPF y Mediciones del Arnés de Render (Rev034)
- **Colecciones Reactivas sin Asignaciones:** `BatchObservableCollection<T>.ReplaceAll` incorpora una sobrecarga especializada para `IReadOnlyList<T>` que ejecuta comparación de igualdad de elementos en el lugar (`EqualityComparer<T>.Default.Equals`), eliminando asignaciones intermedias de matrices cuando los estados de filtrado o navegación permanecen inalterados.
- **Filtrado Directo de Búsqueda y Paleta:** `DesktopShellViewModel.FilterPaletteTools` y `RefreshVisibleTools` sustituyeron cadenas LINQ `.Where(...)` y `.Select(...)` por bucles basados en índices predimensionados, eliminando asignaciones de iteradores y cierres (closures) durante pulsaciones rápidas de teclado en la búsqueda.
- **Predimensionado de Riel Operacional:** `RefreshOperationalRailEntries` precalcula la capacidad exacta de la colección en función de los grupos visibles y el conteo de herramientas activas, evitando ciclos de redimensionado de matrices al poblar las entradas del riel.
- **Eliminación de Boxing en Convertidores:** `PinnedGlyphConverter` evalúa `IList<ToolDefinition>` e `IReadOnlyList<ToolDefinition>` mediante acceso directo por índice, eliminando el boxing de la interfaz `IEnumerator` en las 194 rutas de herramientas en cada pase de renderizado.
- **Predimensionado de Búferes de Simulación:** Los informes de `DesktopSimulationService` (árboles de tropas, memoria de agentes CoALA, formas de onda de audio y modelos económicos a 30 días) inicializan los búferes de `StringBuilder` con 2.048 caracteres, evitando reasignaciones dinámicas en el montículo (heap).
- **Referencia del Arnés de Render fuera de Pantalla:** El tiempo síncrono de llamadas de layout del arnés de DIP fijo cambió de 1.711,5 ms a 1.647,4 ms (-64,1 ms) en 275 casos. La fase de inicialización/primer renderizado del arnés cambió de 1.488,6 ms a 1.391,4 ms (-97,2 ms). Son mediciones del arnés, no del inicio de la aplicación observado por el usuario ni de la latencia interactiva.

### Optimización Multicapa y Precomputación de Metadatos (Rev035)
- **Precomputación de Metadatos en Catálogo de Herramientas (`ToolCatalog` y `ToolDefinition`):**
  - Las 194 rutas de herramientas en `DesktopToolDefinitions` precalculan metadatos visuales y operacionales directamente en el constructor de `ToolDefinition` (`group`, `iconKey`, `categoryBanner`, `categoryMotto`, `categoryAccentBrushKey`, `inputDomainBadge`, `inputFormatHint`, `securityPillText`, `cliSyntax` y `consoleCommands`).
  - Se eliminaron once comprobaciones dinámicas `.StartsWith()` y diez expresiones `switch` por cada lectura de propiedades en enlaces de datos XAML.
  - Sustitución de matrices de comandos ad-hoc por vectores estáticos compartidos e inmutables (`DefaultHotkeys`, `AudioCommands`, `TroopCommands`, `MemoryCommands`, `EconomyCommands`, `AuditCommands`, `ConflictCommands`, `DiplomacyCommands`, `LiveConsoleCommands` y matrices por grupo).
  - Prealmacenamiento en caché del vector de categorías únicas en el constructor de `ToolCatalog`, eliminando asignaciones redundantes en el heap de LINQ `.Distinct().ToArray()` en cada acceso.
  - Reemplazo del escaneo lineal `Array.IndexOf(GroupOrder, group)` en `GroupRank` por una expresión `switch` O(1) con tabla de salto directa.
- **Optimización de Servicios de Análisis y Ensamblados:**
  - `DesktopAnalysisService.Format`: Capacidad predimensionada de `StringBuilder` (`Math.Max(512, count * 256)`), encadenamiento de métodos `.Append()`, emisión directa de números enteros de línea y columna sin crear cadenas temporales, y sustitución de bucles `foreach` por bucles `for` indexados.
  - `DesktopAssemblyService.Inspect` y `Audit`: Reemplazo de cadenas LINQ `.Select().ToArray()` y `string.Join` por búferes de 2 KB predimensionados; sustitución de filtros LINQ en constructores por bucles indexados sobre `type.Methods`; eliminación de `assembly.CustomAttributes.Select().ToHashSet()` mediante un único pase con banderas booleanas para metadatos de distribución.
- **Optimización de Enlaces en el Registro de Evidencias:**
  - `WorkspaceEvidence.Location`: Precomputación del formato de ubicación (`FormatLocation()`) en el constructor asignado a una propiedad de solo lectura (`public string Location { get; }`), evitando formateo reiterado de cadenas durante los ciclos de renderizado y binding de WPF.
- **Referencia Empírica de Navegación y Renderizado WPF:**
  - La navegación entre rutas y comprobación de filtros se aceleró de una base de 2.145 ms a **1.728,66 ms** (-416,3 ms / **19,4 % de mejora**).
  - La suite de pruebas completa (275 casos de renderizado WPF, 54 pruebas MVVM y de protocolo de Desktop, 66 pruebas de ForgeWeave, 239 pruebas de Core, 8 pruebas de pipeline de recursos) finalizó con 100 % de éxito sin regresiones.

### Optimización de ViewModels de Simulación y Estudios Gráficos (Rev036)
- **Precomputación y Caché Estática en ViewModels de Estudios Visuales (`DesktopSimulationViewModels`):**
  - `TroopTreeDashboardViewModel`: Construcción de la jerarquía imperial canónica (12 nodos de tropa, DAG estándar y noble) una única vez en un constructor estático (`CanonicalStandardTreeRoots`, `CanonicalNobleTreeRoots`, `CanonicalHighlightedTroops`, `CanonicalDefaultSelected`, `CanonicalCataphract`). Precomputación de `TroopNodeViewModel.StatSummary` en el constructor, eliminando interpolaciones de cadenas redundantes en cada evaluación de enlace XAML.
  - `AudioStudioDashboardViewModel`: Almacenamiento en caché estática de `DefaultBands` y precomputación de los 24 puntos de la forma de onda acústica (`DefaultWaveform`), eliminando 29 asignaciones de objetos por cada inicialización del estudio.
  - `WorkshopDashboardViewModel`: Precomputación de `WorkshopEnterpriseItemViewModel.BarWidth` en el constructor (`Math.Max(20, normalized * 220.0)`) y vector estático `DefaultEnterprises` para renderizado instantáneo.
  - `AgentMemoryDashboardViewModel`: Caché estática de perfiles de agentes (`DefaultAgents`: Rhagaea, Derthert, Monchug), precomputación de `MemoryFactItem.HasTtl` en el constructor y uso de `Array.Empty<...>()` como respaldo para todas las listas.
  - `CodeSecurityDashboardViewModel`: Sustitución de matrices dinámicas por el vector inmutable `DefaultMetadataStreams`.
  - `ModuleHierarchyDashboardViewModel`: Preasignación de `ScenarioNodes` en los 3 escenarios, eliminando asignaciones en el ciclo de escenarios.
  - `KingdomDiplomacyDashboardViewModel`: Precomputación de `FactionStanceViewModel.TensionText` y `BarWidth` en el constructor, junto con matrices estáticas `ScenarioStances`.
  - `ComponentGeneratorDashboardViewModel`: Preasignación de `ScenarioBlueprints` en los 3 escenarios de planos XML.
- **Precomputación en el Motor de Simulación (`DesktopSimulationService`):**
  - Precomputación de `CanonicalTroopReport`, `CanonicalTroopEvidence`, `CanonicalAudioReport`, `CanonicalAudioEvidence`, `CanonicalEconomyReport` y `CanonicalEconomyEvidence` como campos estáticos de solo lectura. Las ramas síncronas predeterminadas reutilizan esas instancias de informe y evidencia. La ruta completa del workspace WPF también programa una tarea, crea un `WorkspaceExecutionResult` y materializa la evidencia devuelta; no se ha establecido una afirmación de cero asignaciones de extremo a extremo.
  - `BarCache` reutiliza la cadena del medidor cuando `RenderBar` recibe ancho 24. No elimina asignaciones del formato del informe circundante ni de la ruta asíncrona del workspace.
  - Reemplazo de consultas LINQ y matrices temporales en `InspectAgentMemory` por vectores preasignados (`SampleLordAgents`, `EpisodicTypes`) y bucles indexados.
- **Referencia Empírica de Rendimiento y Renderizado WPF:**
  - Pruebas no visuales y contratos acelerados de 812,42 ms a **691,69 ms** (-15 %).
  - La fase de inicialización/primer renderizado del arnés fuera de pantalla cambió de 1.594,45 ms a **1.460,39 ms** (-8,4 %); no mide el inicio de la aplicación observado por el usuario.
  - Navegación entre rutas y filtrado acelerados de 1.853,48 ms a **1.593,84 ms** (-14 %).
  - Tiempo total de la suite de renderizado WPF reducido a **9.792 ms** en los 275 casos de prueba con 100 % de éxito.

### Optimización del Pipeline XAML, Congelación de Recursos y Eliminación de Reflexión (Rev037)
- **Congelación y Caché Estática en Convertidores XAML (`DesktopConverters`):**
  - `GeometryKeyConverter`: Incorpora un diccionario estático `geometryCache` e invoca inmediatamente `geom.Freeze()`. Desvincula las geometrías semánticas del hilo del despachador, permitiendo al compositor de WPF reutilizar glifos vectoriales en memoria nativa con aceleración hardware sin comprobaciones continuas de hilo ni búsquedas recurrentes en el árbol de recursos.
  - `BrushKeyConverter` y `DecorativeAccentVisibilityConverter`: Almacenamiento en caché estática con invalidación atómica coordinada en `DesktopThemeService` (`ApplyDefault()` y `Apply(themeId)`), eliminando traversals repetidos de diccionarios fusionados.
  - `PinnedGlyphConverter`: Incorpora ruta rápida para colecciones vacías (`Count == 0`), evitando bucles de comparación en herramientas sin anclajes activos.
- **Resolución de Enlaces Fallidos en Cabina de Mando (`GenericOperationDashboardViewModel`):**
  - Provisión de propiedades explícitas (`SelectedToolSummary`, `BufferCapacityText`, `FileTraversalText`, `ThreadIsolationText`, `ExecutionBoundsText`) requeridas por las plantillas XAML en más de 180 rutas de herramientas genéricas.
  - Se eliminan por completo las penalizaciones de reflexión interna y trazas de depuración de enlace (`System.Windows.Data`) en cada pase de diseño (`Measure`/`Arrange`).
  - Introducción de `GenericOperationDashboardViewModel.CanonicalInstance` compartida con semántica de copia-en-escritura (copy-on-write) al alternar escenarios, eliminando la asignación reiterada de modelos genéricos.
- **Clasificación O(1) de Estudios Mediante Enum de Byte y Propiedades Booleanas:**
  - `ToolDefinition`: Incorpora `DesktopStudioKind : byte` precalculado en el constructor, clasificando instantáneamente entre los 9 tipos de estudio (`Generic`, `TroopTree`, `AudioMixer`, `Workshop`, `AgentMemory`, `CodeSecurity`, `ModuleHierarchy`, `KingdomDiplomacy`, `ComponentGenerator`).
  - `ToolPageViewModel`: Inicializa propiedades booleanas inmutables `{ get; }` fijas (`IsTroopTreeVisualizer`, `IsAudioMixerInspector`, `IsWorkshopSimulator`, etc.) en el constructor, convirtiendo la activación de vistas especializadas en comprobaciones directas de registro CPU.
- **Izado de Predicados en Búsqueda y Filtrado de Paleta (`DesktopShellViewModel`):**
  - `FilterPaletteTools`: La evaluación del prefijo de paleta (`>live`, `>diag`, `>asset`, `>sim`, etc.) se resuelve una única vez antes de iniciar el bucle sobre las 194 herramientas, asignando delegados estáticos sin asignación de clausura (`static item => ...`) y suprimiendo hasta 2.500 llamadas de comparación de cadenas por pulsación de tecla.
- **Precomputación en Modelos de Grupo de Riel (`WorkbenchViewModels`):**
  - Precomputación de `GroupIconKey = IconForGroup(Name)` en el constructor de `ToolGroupViewModel`, aligerando la carga y alternancia del riel de navegación.
- **Referencia Empírica de Rendimiento y Renderizado WPF:**
  - La fase de inicialización/primer renderizado del arnés fuera de pantalla cambió de 1.751,0 ms a **1.385,7 ms**; no mide la latencia inicial observada por el usuario.
  - Pruebas no visuales y contratos acelerados a **764,6 ms**.
  - La suite de renderizado WPF completa (275 casos de prueba) finalizó en **9.883 ms** con 100 % de casos aprobados (0 fallos).
  - Nodos visuales recorridos reducidos en los snapshots del árbol visual.

### Optimización CoALA de Memoria Cognitiva, Poda Temporal y Verificación UIA (Rev038)
- **Puntuación de Utilidad Compuesta y Decaimiento Temporal MIRIX (`agent-memory-systems`):**
  - Implementación de la fórmula cognitiva MIRIX en `MemoryFactItem`:
    $$Utility = 0.4 \cdot Recency + 0.3 \cdot Frequency + 0.3 \cdot Importance$$
  - Precomputación de `UtilityScore`, `UtilityBadge` (`[U: 0.88]`) y color semántico `UtilityBrushKey` (`VerdigrisBrush` para $U \ge 0.70$, `BrassBrush` para $0.40 \le U < 0.70$, `EmberBrush` para $U < 0.40$) en el constructor. Son valores precalculados del modelo; no se midieron asignaciones de la interacción WPF completa.
  - Enriquecimiento de `AgentProfileViewModel` con contadores atómicos precalculados (`FactCount`, `EpisodicCount`, `ProceduralCount`), estado de decaimiento (`DecayState`) y distintivo textual consolidado `SummaryBadge`.
- **Preasignación de Escenarios de Decaimiento Cognitivo (`ScenarioAgentSets`):**
  - `AgentMemoryDashboardViewModel`: Preasigna tres matrices estáticas inmutables que representan las etapas temporales de los agentes:
    1. *Escenario 0 (Estado Base T=0h):* Recencia completa ($R=1.0$), TTLs activos y alta utilidad.
    2. *Escenario 1 (Decaimiento Temporal T+24h):* Recencia atenuada ($R=0.79$), expiración de TTLs efímeros y recalibración de prioridades dinásticas.
    3. *Escenario 2 (Poda y Consolidación T+72h):* Recencia semivida ($R=0.50$), poda de hechos de baja utilidad, compresión episódica consolidada y reducción de cuota global de memoria de 6.0% a 3.2%.
  - Método `CycleScenario(int index)` y sobrecarga `CycleScenario()` sin asignación heap adicional en tiempo de ejecución.
  - Integración en `WorkspacePageViewModel.CyclePreset()`, permitiendo alternar escenarios cognitivos mediante el botón de acción de preajuste.
- **Harness de Pruebas de Automatización de UI Windows (`calradia-forge-ui-automation`):**
  - Creación de `tools/Test-CalradiaForge-Desktop-Uia.ps1`: harness externo no intrusivo y acotado que descubre la ventana principal de la aplicación mediante `System.Windows.Automation` y verifica 18 controles de accesibilidad (`WindowCloseButton`, `HeaderSealButton`, `CommandPaletteButton`, `LanguageSelector`, `ThemeSelector`, `SplitDeckToggleButton`, `DecorativeAccentsToggle`, `OperationalSearchFilter`, `CategorySelector`, `OperationalRailToolViewport`, `WorkbenchPageScrollViewport`, `RunWorkOrderButton`, `CancelWorkOrderButton`, `ExportWorkOrderButton`, `WorkbenchReportTabs`, etc.).
  - Búsqueda acotada con timeout defensivo, inspección estricta de patrones (`InvokePattern`, `SelectionPattern`, `ScrollPattern`), sin mutaciones de archivos ni estado persistido, y guardado de informe en `artifacts/desktop-uia-smoke.json` (18/18 pruebas superadas).
- **Referencia Empírica de Rendimiento y Renderizado WPF:**
  - Tiempo de llamadas de pase de diseño/layout reducido a **1.382,5 ms** durante la compilación y validación de paquetes.
  - Generación y verificación criptográfica de los tres archivos de distribución v25.0.0.

### Congelación Profunda de Recursos de Tema, Hardware BitmapCache y Consolidación Cognitiva Avanzada (Rev039)
- **Congelación Profunda de Recursos de Temas (`DesktopThemeService`):**
  - Implementación de congelación recursiva (`FreezeDictionaryResources`) en todas las entradas `SolidColorBrush`, `LinearGradientBrush`, `DrawingBrush` y `Color` de los diccionarios de temas activos al aplicar temas (`ApplyDefault` y `Apply(themeId)`).
  - Los recursos congelados eliminan comprobaciones de afinidad de hilo en WPF, permiten uso compartido directo entre subprocesos y suprimen la sobrecarga de notificaciones de cambio de DependencyProperty en miles de elementos visuales renderizados.
- **Hardware BitmapCache en Grupos Vectoriales Estáticos (`MainWindow.xaml`):**
  - Equipamiento de grupos decorativos vectoriales complejos (rejilla `WorkbenchCartographicBoardDecoration` y decoración interna de `HeaderSealButton`) con `<BitmapCache EnableClearType="False" RenderAtScale="1.0" SnapsToDevicePixels="True"/>`.
  - Convierte la rasterización de geometrías complejas en muestreo de texturas GPU durante pasadas de diseño, cambios de tamaño y alternancias de tema.
- **Muestra de Memoria de Agentes y Métricas de Salience:**
  - La ruta WPF presenta perfiles ilustrativos; no es un registro vivo de Bannerlord ni un runtime directo de CoALA. El ciclo de selección evita colecciones temporales, pero la interpolación de resúmenes y la creación de arreglos circundantes sí asignan memoria. No se afirma que toda la ruta de ViewModel/informe tenga cero asignaciones.
  - `AgentMemoryDashboardViewModel`: Exposición de `ActiveClusterOverview` y `ActiveConsolidationRate` enlazados directamente a la tarjeta de cabecera con notificaciones reactivas al seleccionar agentes y alternar escenarios.
- **Verificación Ampliada con Windows UI Automation (`calradia-forge-ui-automation`):**
  - Ampliación de `tools/Test-CalradiaForge-Desktop-Uia.ps1` a 29 comprobaciones en más de 24 nodos de interfaz.
  - Verificación no mutante de navegación por pestañas con `SelectionItemPattern.Select()` (`WorkbenchRawResultTab` -> materialización de `RawResultText` -> restauración de `WorkbenchEvidenceTab`).
  - Verificación no mutante de `InvokePattern.Invoke()` en `SplitDeckToggleButton` (abre, comprueba estado visual y cierra).
  - 29/29 verificaciones superadas en 14.294 ms (0 fallos, 0 advertencias); informe exportado a `artifacts/desktop-uia-smoke.json`.
- **Referencia Empírica de Rendimiento y Renderizado WPF:**
  - Duración de llamadas de diseño reducida a **1.265,9 ms - 1.468,4 ms** (frente a 1.647,4 ms en Rev034 y 1.581 ms en ejecuciones previas).
  - Tiempo total de prueba WPF reducido a **9.226 ms** en los 275 casos de prueba de renderizado.
  - Suite completa de la solución: 726 pasadas, 0 falladas.
  - Empaquetado oficial 25.1.0 generado y verificado criptográficamente.

### Motor Gráfico Puro, Virtualización de Contenedores y Optimización de Composición (Rev040 - v25.2.0)
- **Virtualización Estricta de UI y Desplazamiento Suave por Píxel (`MainWindow.xaml`):**
  - Configuración de `VirtualizingPanel.ScrollUnit="Pixel"`, `VirtualizingPanel.VirtualizationMode="Recycling"` y `VirtualizingPanel.CacheLength="1,1" VirtualizingPanel.CacheLengthUnit="Page"` en `OperationalRailToolViewport` (194 herramientas) y `CommandPaletteToolList`.
  - Conversión de `EvidenceLedgerItems` a virtualización completa con `ScrollViewer.CanContentScroll="True"` y `VirtualizingStackPanel`, eliminando la asignación de nodos del árbol visual al renderizar cientos de registros de evidencias.
  - Activación de precaché fuera de pantalla (margen de 1 página superior e inferior) para desplazamiento suave a 60 fps sin tirones ni presión sobre el recolector de basura.
- **Composición DirectX y Optimización EdgeMode Aliased:**
  - Aplicación de `RenderOptions.EdgeMode="Aliased"` y `SnapsToDevicePixels="True"` a reglas divisorias de 1px, `TitleBarBorder`, `MainCommandHeaderBand` y pestañas de informes, suprimiendo el desenfoque por subpíxeles y la sobrecarga del rasterizador GPU en pantallas de alta densidad.
  - Pre-congelación de pinceles dinámicos en `BrushKeyConverter` y congelación recursiva de `Application.Current.Resources` en `DesktopThemeService.ApplyDefault()`.
- **Referencia Empírica de Rendimiento y Renderizado WPF:**
  - Duración de llamadas de diseño reducida a **1.229,5 ms** (frente a 1.265,9 - 1.468,4 ms en Rev039 y ~1.750 ms en Rev036).
  - Suite completa de pruebas: 726 superadas al 100%; Windows UI Automation 29/29 comprobaciones superadas en 29.365 ms.
  - Versión menor 25.2.0 sincronizada en propiedades, manifiestos y pruebas; empaquetada en `artifacts/` con resúmenes SHA-256.

Selecciona un archivo o carpeta local antes de auditar. Si no hay entrada compatible, la aplicación muestra **No ejecutado** o **No compatible**, sin inventar aprobaciones. Los resultados indican analizador, procedencia, evidencia, límites y recomendación. El selector resuelve los trece idiomas compatibles de Bannerlord; el código, rutas e información técnica se conservan sin traducir.

La aplicación mantiene hasta 64 mediciones breves. La construcción síncrona del shell, el filtrado, la selección de ruta y los cambios de tema o idioma informan duración y asignaciones medidas en el hilo propietario. El análisis y las operaciones de pipe asíncronas conservan la duración, pero muestran los bytes asignados como no medidos: `GC.GetAllocatedBytesForCurrentThread` no puede atribuir el trabajo que cruza `await` o cambia de hilo. Estas muestras no miden toda la latencia entre una entrada y su frame y no representan el rendimiento general del juego. El harness WPF separado mide sus propias llamadas de layout.

Los recursos ilustrados de Desktop se empaquetan solo desde `Resources/Textures/Optimized`. `tools/Prepare-CalradiaForge-Desktop-Textures.bat` los regenera; con `--check` comprueba determinismo byte a byte e inventario exacto. El ornamento de la tarjeta de herramienta conserva la proporción 3:1 de la fuente sin recortar y se prepara a 540×180 píxeles para mostrarse a 270×90 DIP incluso con escala del 200 %. El generador rechaza cambios de proporción. Las variantes retiradas dejan de empaquetarse, pero se conservan sus fuentes de autoría. El render del harness no sustituye la inspección de la ventana WPF en vivo.






### Correcciones de la shell para ventanas estrechas y límites de evidencia (Rev056)
- El rail de navegación ahora tiene un ancho mínimo de 220 DIP, limitado a 276 DIP, y conserva un mínimo de 400 DIP para el área de trabajo. El Split Deck se redujo a 280 DIP para que la composición lateral quepa mejor en la ventana mínima admitida.
- El marco cartográfico de la tarjeta mide 160×90 DIP, en la misma proporción 16:9 que su imagen fuente de 448×252. `Stretch="Uniform"` conserva la imagen sin recorte ni deformación. Se quitaron las capas `BitmapCache` de escala fija de los adornos pasivos del mapa y del sello, evitando ampliar una caché rasterizada a baja resolución.
- Las etiquetas fijas de la shell, las ayudas y los rótulos vacíos/de salida del Split Deck se resuelven desde el diccionario de recursos activo. Los 13 diccionarios de idioma conservan 101 claves coincidentes. Al limpiar contenido fijado, se notifican los cambios derivados del título y la categoría después de vaciar la evidencia, evitando texto obsoleto.
- Cinco ejecuciones seriales del harness de render pasaron 275 casos cada una, con 152 pases de layout por corrida. La mediana total del harness fue 11.335 ms frente a 13.652 ms de línea base (-17,0 %); la mediana de llamadas de layout fue 2.075 ms frente a 2.397 ms (-13,5 %). Las medianas de inicio, navegación/filtro, localización y tema fueron 2.582 ms, 2.042 ms, 2.007 ms y 3.109 ms; cada fase mejoró frente a la línea base. Son tiempos del harness, no latencia interactiva de la aplicación.
- La matriz de escalas rasteriza las capturas a mayor DPI; no emula el layout de Windows con escalado DPI del sistema. El arte decorativo del rail permanece colapsado intencionalmente porque su proporción fuente no cabe en el marco disponible. La inspección en vivo de la ventana/UI Automation sigue pendiente; estas comprobaciones no certifican el render real de escritorio con ajustes de escala del sistema operativo.

### Enriquecimiento Arquitectónico de Estudios Tácticos y Preajustes de Rol (Rev041)
- **Documentación de Estudios e Invariantes de TaleWorlds:**
  - Los 8 ViewModels de estudios tácticos (`TroopTreeDashboardViewModel`, `AudioStudioDashboardViewModel`, `WorkshopDashboardViewModel`, `AgentMemoryDashboardViewModel`, `CodeSecurityDashboardViewModel`, `ModuleHierarchyDashboardViewModel`, `KingdomDiplomacyDashboardViewModel` y `ComponentGeneratorDashboardViewModel`) exponen `StudioDocumentation` detallada, `ArchitecturalInvariants` rigurosas, `StudioCaveat` operativa y comandos `CuratedConsoleCommands`.
  - Las invariantes documentan restricciones del motor TaleWorlds directamente en cada estudio:
    - *TroopTree:* Ranuras de equipamiento, fórmulas salariales por nivel, topología DAG del árbol de mejora.
    - *AudioStudio:* Archivos `.ogg` en minúsculas en `ModuleSounds/`, categorías de mezclador 2D vs 3D, frecuencia de muestreo de 44.1 kHz.
    - *Workshop:* Topes de prosperidad urbana, elasticidad de oferta/demanda, aranceles anti-efecto bola de nieve.
    - *AgentMemory:* Time-slicing por módulo 24 en ticks, comportamiento 100 % sin estado, cero `SaveableTypeDefiner`.
    - *CodeSecurity:* GEMINI Regla A (anti-shadowing), Regla B (sin estado), IDs de `SaveableTypeDefiner` $\ge 2.500.000$.
    - *ModuleHierarchy:* Coincidencia exacta del `<Id>` con la carpeta, ordenamiento topológico de carga, rechazo de ciclos de dependencia.
    - *KingdomDiplomacy:* Resolución de `KingdomDecision`, índices de desgaste bélico, casus belli bilateral.
    - *ComponentGenerator:* Esquemas de elementos raíz XML, regla del doble `SetDialogs()` en misiones.
- **Comandos de Consola Curados y Ejecutables (`StudioConsoleCommand`):**
  - Cada estudio incluye una lista curada de comandos ejecutables con descripciones para la consola del juego (`cf.sim_tactics`, `sound.play`, `cf.quick_state`, `cf.inspect`, `cf.audit`, `cf.modules`, `cf.sim_diplomacy`, `cf.scaffold`).
- **Preajustes de Rol de Modder en el Rail Operativo (`ModderRolePreset`):**
  - Integración de 5 preajustes de rol en `DesktopShellViewModel`:
    - `All`: Rail operativo completo sin filtrar con las 194 herramientas.
    - `Narrative & Dialogues`: Centrado en diseño de misiones, diálogos de héroes, memoria cognitiva, localización y progresión narrativa.
    - `Troop & Combat Artisan`: Centrado en árboles de tropas, tácticas de combate, formaciones IA, armas, armaduras y SFX de audio.
    - `Economy & World Architect`: Centrado en economía de asentamientos, precios comerciales, talleres, callejones criminales, partidas y diplomacia.
    - `Core Dev & Performance`: Centrado en cumplimiento de reglas, SaveableTypeDefiners, GC de memoria, repeticiones ForgeWeave y diagnósticos.
  - Alternancia dinámica mediante `CycleModderRoleCommand` con retroalimentación visual e información contextual en la UI.

Consulta la [guía del banco de trabajo de ensamblados](ASSEMBLY_WORKBENCH.md) para el flujo de previsualización y aplicación dentro del juego. El sitio DocFX local combina las guías en inglés con la documentación XML pública del SDK; genéralo con `tools/build_docs.ps1`.
No copies los archivos Desktop dentro de `Modules`; el módulo del juego funciona sin el banco de trabajo Desktop.

### Shell adaptable y dossier contextual (Rev057)

La ventana WPF sigue alojando el marco nativo, los atajos de teclado globales y `WorkbenchShellView`. La shell ahora se compone de las vistas especializadas `WorkbenchHeaderControl`, `WorkbenchStatusControl`, `WorkbenchNavigationControl`, `WorkbenchWorkspaceControl`, `WorkbenchFooterControl` y `CommandPaletteControl`. Las plantillas especializadas de visualizadores y la plantilla de ruta están en `Resources/Views/ToolPageTemplates.xaml`; `ToolDossierControl` reúne los comandos contextuales, los atajos y la referencia CLI. Los ViewModels conservan el estado y los comandos. Esta separación de presentación no agrega rutas ni cambia contratos públicos del SDK, IPC o el juego.

La cabecera reorganiza sus ajustes y acciones mediante un diseño que admite saltos de línea; el selector de tema mantiene en una sola línea recortada las etiquetas largas como `High Contrast`. El `WrapPanel` de acciones está limitado a su columna de tamaño estrella para que los controles se acomoden dentro del espacio asignado sin ensanchar la shell. `AdaptiveWorkbenchPanel` mantiene la ruta actual, Split Deck y el dossier lateral opcional uno junto a otro solo cuando caben sus anchos mínimos; en otro caso los apila dentro del área de desplazamiento vertical del espacio de trabajo. El ancho mínimo de la ruta proviene de `ToolPageViewModel.MinimumPresentationWidth` (360 DIP para páginas estándar y 520 DIP para visualizadores); los mínimos secundarios son 250 DIP para el deck y 260 DIP para el dossier. La shell conserva una ventana mínima de 980×680 DIP, un rail de navegación de 220–276 DIP y un área de trabajo de al menos 400 DIP. Son restricciones de disposición, no una afirmación de que todos los controles hayan superado una revisión visual en esos tamaños.

El botón accesible `ContextDossierToggleButton` de la cabecera usa la etiqueta localizada `Ui.DossierHeading` y se enlaza a `IsContextDossierOpen`. Un estilo explícito y `ContentPresenter` mantienen visible la etiqueta localizada en vez de dejar que la plantilla implícita de Material Design la oculte; la regresión exige al menos 80 DIP para la etiqueta. El dossier está cerrado inicialmente, sigue la ruta seleccionada y constituye estado de presentación; no se guarda en preferencias. Al cerrarlo, el mismo control permanece disponible como punto de retorno del teclado.

Dos recursos WPF locales optimizados añaden una franja cartográfica al título (`titlebar-cartographic-panorama-rev057.png`, 2172×67 píxeles, 274.555 bytes) y un ornamento discreto para esquinas de tarjetas (`dossier-corner-ornament-rev057.png`, 384×256 píxeles, 35.618 bytes). La suma de estos recursos es 310.173 bytes comprimidos y 975.312 bytes RGBA decodificados. El inventario completo de PNG empaquetados en Desktop suma 7.465.788 bytes comprimidos y 15.921.104 bytes RGBA decodificados. Son adornos pasivos; la detección de clics y el foco de teclado permanecen en los controles. Alto Contraste conserva pinceles sólidos para las superficies. Sus adornos de baja opacidad se configuran por separado, pero la revisión de legibilidad de esta versión sigue pendiente; no se afirma aprobación visual en vivo.

La línea base previa Rev057 consta de cinco informes aprobados del BAT de renderizado, cada uno con 275 casos y 152 pases de layout; las medianas fueron 11.294 ms totales y 2.072,6 ms en llamadas síncronas de layout. Después de los cambios de shell, cinco ejecuciones comparables sin compilación de `tools/Run-CalradiaForge-Tests.bat --desktop-only --skip-build --no-pause --render-output <ruta-json-única>` pasaron 276 casos de render y 161 pases de layout cada una. Sus tiempos totales fueron 8.360, 8.327, 8.403, 7.659 y 8.542 ms (mediana de 8.360 ms, 26,0 % menos que la línea base); las llamadas de layout tardaron 1.759, 1.714,2, 1.747,8, 1.601,6 y 1.732,6 ms (mediana de 1.732,6 ms, 16,4 % menos). Un BAT de Desktop compiló sin advertencias ni errores y pasó Desktop 56/56; una ejecución posterior al ajuste de las etiquetas de informe también pasó Desktop 56/56 y 276 casos/161 pases de layout (8.014 ms en total y 1.632,6 ms de layout). Se conserva toda la cobertura previa y se agregan casos; los tiempos pertenecen al arnés y no miden la latencia de interacción de Desktop.

El launcher acotado y de solo lectura `tools/Test-CalradiaForge-Desktop-Uia.bat` pasó 20/20 comprobaciones del árbol de accesibilidad; el informe es `artifacts/desktop-uia-rev057-final.json`. Identificó la ventana principal del proceso propio y los controles de la shell sin abrir selectores, ejecutar órdenes de trabajo ni cambiar preferencias. UI Automation no equivale a aprobación visual humana. La matriz registra `render-language-raster-scale` para factores 1, 1,25, 1,5 y 2; conserva una ventana lógica de 1360×820 DIP con `LayoutTransform` identidad y rasteriza la salida `RenderTargetBitmap` a la escala solicitada. El informe indica `windowsSystemDpiChanged=false`; no simula el layout con DPI del sistema Windows ni cambia las medidas DIP. Pasó `--check` de texturas. La legibilidad de los adornos de Alto Contraste y la revisión visual con el escalado real del sistema siguen sin verificarse. `--render-output <ruta>` solo selecciona el destino del informe JSON; usa una ruta distinta en cada corrida. No se regeneró ni modificó ningún ZIP.

### Seguimiento del tamaño adaptable de búsquedas y acciones (Rev058)

Los campos de búsqueda del rail y de la paleta de comandos miden 42 DIP de alto y centran verticalmente el texto. El botón de limpieza de 32×32 DIP reserva espacio en el campo para no superponerse a la consulta. En la ventana mínima de 980×680 DIP, los controles de cabecera de paleta de comandos, Split Deck y dossier tienen anchos mínimos de 116, 120 y 126 DIP, respectivamente, y una altura mínima de 36 DIP. Los botones de órdenes de trabajo miden 34 DIP de alto, con anchos acotados y ajuste de línea para mantener las acciones dentro de su columna disponible. Las etiquetas del informe y del dossier conservan plantillas explícitas que muestran su contenido. El harness de render confirmó que las acciones permanecen visibles en las vistas de tamaño mínimo y normal.

La validación usó `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` para compilar sin advertencias ni errores, pasar Desktop 56/56 y realizar una corrida de render WPF con compilación (277 casos, 161 pases de layout, 9.410 ms). Cinco ejecuciones posteriores de render sin compilación pasaron 277 casos y 161 pases de layout cada una. Los valores JSON de `milliseconds` fueron 8.754, 8.928, 8.281, 8.355 y 8.093 ms (mediana de 8.355 ms); los de `renderLayoutMilliseconds` fueron 1.685,9, 1.826,3, 1.652,8, 1.709,0 y 1.507,1 ms (mediana de 1.685,9 ms). Frente a las cinco corridas Rev057 (medianas de 8.360 ms y 1.732,6 ms, con 276 casos/161 pases), la mediana total fue 0,06 % menor y la mediana de layout 2,7 % menor. El rendimiento del harness se mantiene esencialmente estable; esto no demuestra una optimización sustancial ni mide la latencia de la aplicación.

`tools/Test-CalradiaForge-Desktop-Uia.bat` pasó 20/20 comprobaciones de solo lectura en `artifacts/desktop-uia-rev057-controls-final.json`; no manipuló el escalado del sistema. Pasó `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check`. El inventario empaquetado de PNG suma 7.465.788 bytes comprimidos y 15.921.104 bytes RGBA decodificados; incluye los dos adornos Rev057, que suman 310.173 bytes comprimidos y 975.312 bytes RGBA decodificados. La comprobación informó salidas deterministas. Estos resultados no prueban legibilidad visual con DPI real de Windows ni equivalen a aprobación visual humana. No se inició Bannerlord y no cambiaron los ZIP ni la versión del producto.

### Seguimiento de jerarquía visual y moderación de texturas (Rev059)

El tema Parchment ahora muestra las texturas de superficie y título con opacidades de 0,11 y 0,12, en lugar de 0,34 y 0,32, para que la ilustración no compita con los controles ni el texto. La ruta activa del rail de navegación tiene un estado seleccionado observable. La búsqueda del rail expone la indicación localizada `Ui.SearchHint`. El tablero cartográfico reserva una columna de 168 DIP para un marco de 160×90 DIP con un margen de 8 DIP, evitando que el arte interfiera con el área de trabajo. El estado vacío de evidencia usa una ilustración de 42 píxeles y un botón de acción de 32 DIP para dar más presencia a la acción sugerida.

La última ejecución Desktop de `tools/Run-CalradiaForge-Tests.bat` compiló sin advertencias ni errores, pasó Desktop 56/56 y aprobó 277 casos de render WPF con 161 pases de layout. El arnés reportó 7.808 ms en total y 1.824,9 ms en llamadas de layout. Son mediciones del arnés, no de la latencia de interacción de la aplicación abierta; no demuestran que la app sea más rápida en uso. `tools/Test-CalradiaForge-Desktop-Uia.bat` pasó 20/20 comprobaciones acotadas y de solo lectura en `artifacts/desktop-uia-rev059.json`; UI Automation inspeccionó el árbol de accesibilidad y no equivale a aprobación visual. No se afirma aprobación visual en vivo.

La versión del producto permanece en 25.2.0. No se cambió intencionalmente la API pública, el catálogo de rutas ni el comportamiento del banco de trabajo; no se regeneró ningún ZIP.

### Pruebas de render automatizadas que preservan el foco (Rev060)

El arnés de render WPF conecta su hilo STA dedicado a un escritorio privado de Windows antes de crear objetos WPF, HWND o hooks. Una ventana de render puede convertirse en primer plano dentro de ese escritorio privado, pero está separado del escritorio interactivo del usuario. `ForegroundWindowGuard` ahora solo observa eventos en el escritorio privado; nunca llama a `SetForegroundWindow` ni intenta restaurar el foco. La regresión de la barra de título inspecciona bindings, estilos y manejadores sin maximizar, minimizar ni restaurar una ventana nativa.

El ejecutor opcional de smoke UI Automation tampoco tiene una llamada para restaurar el foco. Instala el observador de primer plano filtrado por proceso antes de iniciar la app de solo lectura y falla si observa que la app pasa a primer plano. No se repitió la ejecución UIA en esta revisión; solo se comprobó el análisis sintáctico de PowerShell y que no quedan llamadas para modificar el foco. Esa comprobación estática no equivale a aprobar UIA.

La validación usó dos veces, oculto, el launcher `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output <ruta-json-única>`. Ambas corridas compilaron con cero advertencias/errores, pasaron Desktop 57/57 y render 278/278, y dejaron el HWND de primer plano interactivo en `0x171168` antes y después. La última corrida informó 158 pases de layout, 2.043,3 ms en llamadas de layout y 9.492 ms totales del arnés. El observador del escritorio privado sí vio que el proceso de pruebas pasó a primer plano dentro de ese escritorio aislado, lo cual es esperado; el primer plano interactivo del usuario no cambió. Estas cifras describen el arnés de pruebas, no la latencia de interacción de la aplicación.

### Launcher Desktop que preserva el foco (Rev061)

Al iniciar las pruebas WPF desde el Explorador, usa `wscript.exe tools\Run-CalradiaForge-Desktop-Checks-Hidden.vbs` o abre el archivo VBS. Ejecuta `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` con la ventana de comandos oculta y guarda un log, estado e informe de render únicos en `artifacts/desktop-focus-safe/`. El arnés de render mantiene sus HWND WPF en un escritorio privado de Windows. La comprobación UIA separada se excluye intencionalmente de este launcher silencioso porque su BAT inicia PowerShell; el proceso WPF de UIA solicita ahora `CreateNoWindow`/`Hidden` y registra transiciones de primer plano. Así se distingue una ventana de consola o aviso de seguridad de una ventana de pruebas WPF. El launcher oculto se validó sin una ventana Calradia Forge abierta, por lo que esa ejecución no demuestra por sí sola la conservación del foco cuando la app del usuario está en primer plano.

La última ejecución oculta terminó con código 0: Desktop 59/59 y render WPF 281 casos/158 pases de layout, cero advertencias/errores de compilación y 7.912 ms totales del arnés. El primer plano permaneció en la misma ventana de AvastUI (`0x580296`) antes y después; no había una ventana WPF de Calradia Forge abierta durante esa corrida. Un intento combinado que también invocó el BAT UIA no generó informe UIA y terminó en otro HWND de AvastUI; no se determinó la causa, así que UIA se mantiene fuera del flujo oculto predeterminado.

### Lectura adaptable del estado de ruta (Rev062)

En el tamaño mínimo de ventana de 980×680 DIP, la tarjeta de estado de la aplicación ajusta el nombre largo de una ruta seleccionada a una segunda línea en vez de ocultar su final. El tooltip conserva el valor completo. Las suites Desktop y render WPF por BAT pasaron con cero advertencias/errores de compilación; se mantuvo la cobertura de 281 casos de render y 158 pases de layout.

### Ilustración vertical del rail (Rev063)

El rail de navegación ahora usa la ilustración local transparente `workbench-heraldic-rail-portrait-rev063.png`, ajustada uniformemente detrás del contenido. La antigua franja horizontal del rail se conserva como recurso histórico/de autoría, pero el manifiesto de preparación de texturas la marca como retirada y el proyecto Desktop ya no la incluye. La imagen es pasiva y sigue el ajuste de adornos decorativos. Los tokens de opacidad son 0,18 para War Table, 0,12 para Parchment Light y 0,24 para Alto contraste; las superficies de Alto contraste siguen siendo sólidas.

`tools/Prepare-CalradiaForge-Desktop-Textures.bat` regeneró el derivado optimizado en 320×640 RGBA (126.258 bytes; SHA-256 `DF8C90E403A3B4D5B8476824B72FEFF006C83023165A4924A4CE084EFF362638`) y pasó su comprobación determinista `--check`. El inventario empaquetado suma 7.495.858 bytes comprimidos y 16.473.296 bytes RGBA decodificados, dentro de los límites configurados. `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts/desktop-visual-rev063-opacity/render.json` compiló sin advertencias ni errores, pasó Desktop 59/59 y 281 casos de render WPF con 158 pases de layout. El arnés tardó 7.998 ms en total y 1.779,8 ms en llamadas de layout; son mediciones del arnés, no latencia de la aplicación abierta. Se revisaron capturas renderizadas; la apariencia de la aplicación en vivo y el comportamiento con el DPI real de Windows no se verificaron. La versión del producto sigue en 25.2.0; no cambiaron contratos públicos, rutas, comandos, permisos ni ZIPs.

### Refinamiento del arte del rail en alto contraste (Rev064)

El primer derivado vertical del rail era demasiado oscuro para distinguirse bien de la superficie Alto contraste. Se sustituyó por `workbench-heraldic-rail-portrait-rev064.png`, un grabado transparente generado localmente con bordes más claros de latón y verdigrís. El derivado anterior Rev063 se conserva como histórico y queda excluido del manifiesto de recursos incrustados. War Table, Parchment Light y Alto contraste mantienen sus tokens de opacidad por tema; Alto contraste conserva opacidad 0,24 y superficies sólidas.

El renderizador ahora comprueba el arte del rail ya compuesto con alfa/opacidad sobre la superficie de navegación Alto contraste. Exige al menos 1,5:1 de contraste del adorno con la superficie y conserva al menos 4,5:1 para el texto claro y atenuado. La ilustración sigue siendo pasiva y usa el ajuste decorativo existente. El derivado optimizado es RGBA de 320×640, pesa 147.309 bytes y su SHA-256 es `06D98F717E5E38F597D965E266F1D6B59208C9AD662C2ED91E4F20A6E7AF578C`; el inventario empaquetado suma 7.516.909 bytes comprimidos y 16.473.296 bytes RGBA decodificados.

`tools/Prepare-CalradiaForge-Desktop-Textures.bat --check` pasó la validación determinista. `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts/desktop-visual-rev064-contrast.json` compiló sin advertencias/errores, pasó Desktop 59/59 y 281 casos de render con 158 pases de layout. Su JSON registró 7.802 ms totales y 1.738,8 ms en llamadas de layout; son mediciones del arnés. Se revisaron capturas de Alto contraste, Parchment y ventana mínima. La app en vivo y el DPI real no se comprobaron. La versión sigue en 25.2.0; no cambiaron contratos públicos, rutas, comandos, permisos ni ZIPs.

### Presentación del informe de evidencia (Rev065)

El informe del espacio de trabajo usa pestañas visibles y accesibles por teclado **Evidence** y **Raw Result**, con estados seleccionado, hover y foco. Cada hallazgo aparece en una tarjeta con origen/estado, ID de regla, ubicación de origen, evidencia y recomendación. Cuando el ledger está vacío, conserva activa la acción localizada **Open command palette** junto a una ilustración local pasiva. La suite de render comprueba las etiquetas, el contenido de las tarjetas, el binding de la acción vacía y los límites de layout. La comprobación de hit testing se ejecuta sobre el host WPF fuera de pantalla: llama a `VisualTreeHelper.HitTest` en el centro del botón y comprueba la superficie WPF del botón y sus antecesores visibles mientras la superposición de la paleta está contraída. Esto confirma que el subárbol WPF renderizado expone una superficie de hit testing sin obstrucciones; no simula el puntero del sistema operativo, no envía un clic real ni verifica una ventana en vivo.

### Aclaración del hit testing del informe de evidencia (Rev066)

La afirmación de hit testing del estado vacío de Rev065 se limita al árbol visual WPF fuera de pantalla, como se describe arriba. El artefacto final posterior a la corrección, `artifacts/desktop-visual-report-rev066-review.json`, registra un informe aprobado con Desktop 59/59, 281 casos de render y 158 pases de layout. Esta ejecución incluye la regresión para recomendaciones que solo contienen espacios en blanco. El arnés registró 7.821 ms totales y 1.707,2 ms en llamadas de layout; son mediciones del arnés de pruebas, no latencia de interacción de la aplicación. La compilación terminó con cero advertencias y errores. El comportamiento del puntero del sistema operativo y la aplicación WPF en vivo siguen sin verificarse.

Rev066 es el contador de documentación fuente. El anexo correspondiente del Registro de Mejoras protegido es Rev046; ambos contadores son independientes. La versión del producto sigue en 25.2.0 y no cambiaron la API, las rutas, los comandos, los permisos, las dependencias ni los ZIPs.

### Jerarquía del estado y de comandos (Rev067)

La paleta de comandos es ahora la única acción principal de la cabecera, con superficie de latón, un icono local de búsqueda, un área mínima de 40 DIP y el mismo nombre accesible localizado y `OpenPaletteCommand`. Las cinco tarjetas de orden usan iconos semánticos uniformes en la esquina superior derecha; la tarjeta de herramienta activa ahora tiene una etiqueta localizada y un borde de latón más marcado. La opacidad de los iconos cambia por tema (0,48 en War Table, 0,56 en Parchment Light y 0,82 en Alto contraste); siguen siendo pasivos y se prueban con cada paleta. El botón de exportación de evidencia mide 30 DIP de alto. Los 13 catálogos incluyen la nueva clave `Ui.ActiveTool`.

`cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-visual-rev067-final.json"` compiló sin advertencias ni errores, pasó Desktop 59/59 y 285 casos de render WPF con 158 pases de layout. El informe final del arnés registró 8.681 ms en total y 2.013,7 ms en llamadas de layout; el artefacto de línea base de esta misma sesión tuvo 281 casos/158 pases y registró 10.893 ms/2.564 ms respectivamente. Son tiempos de una sola ejecución del arnés, no latencia de la aplicación ni una afirmación de rendimiento. Se revisaron capturas de War Table y Alto contraste generadas fuera de pantalla; no se inspeccionó una ventana WPF en vivo.

Rev067 es el contador de documentación fuente. Su anexo del Registro de Mejoras protegido es Rev047; ambos contadores son independientes. La versión del producto sigue en 25.2.0; no cambiaron API pública, rutas, comandos, permisos, dependencias ni ZIPs.

### Cabecera compacta y controles de evidencia (Rev068)

En la ventana mínima de 980×680 DIP, la cabecera usa una etiqueta localizada compacta para los adornos, un botón de dossier solo con icono y controles de configuración/acción más estrechos para mantener sus elementos en una sola fila. El interruptor decorativo conserva su nombre accesible localizado completo y su tooltip. El botón del dossier conserva el binding `IsContextDossierOpen` y la etiqueta accesible completa mientras muestra el glifo local de libro. La exportación de evidencia usa un botón compacto de icono de 36×30 DIP con el comando, ID de automatización, nombre accesible localizado y tooltip existentes; el espacio recuperado permite leer el resumen de evidencia en la tarjeta de estado al tamaño mínimo.

Los 13 diccionarios de idioma contienen `Ui.DecorativeAccentsShort`. Las regresiones de render comprueban la posición y visibilidad de la cabecera compacta, el resumen/exportación de evidencia y la accesibilidad del dossier y del interruptor decorativo. Las rutas, comandos, comportamiento de ViewModel, API pública, IPC, versión del producto y ZIPs existentes no cambian.

`cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-visual-rev068-final.json"` pasó la suite Desktop 59/59 y 285 casos de render WPF con 158 pases de layout y cero advertencias/errores de compilación. Esta ejecución única del render registró 13.135 ms totales y 3.072,1 ms en llamadas de layout; la línea base de una ejecución de la misma sesión registró 9.120 ms y 2.033,3 ms. Son tiempos del arnés y la corrida final fue más lenta; no se afirma una mejora de rendimiento. El host de render está fuera de pantalla, por lo que el comportamiento en ventana en vivo, el layout con DPI real del sistema y la entrada del sistema operativo siguen sin verificarse.

Rev068 es el contador de documentación fuente; su anexo protegido del Registro de Mejoras es Rev048. No se inició Bannerlord, ninguna campaña ni batalla, y no se regeneró ni modificó ningún ZIP.

### Ledger en ventana mínima y tratamiento adaptable de identidad (Rev069)

La regresión de 980×680 DIP ahora comprueba que el mensaje localizado y la acción del ledger de evidencia vacío permanezcan dentro del viewport visible del área de trabajo; la acción conserva su altura mínima de activación. El distintivo de identidad de herramienta tiene un ancho acotado y ya no se extiende por toda la tarjeta de resumen: los nombres de categoría y lemas largos permanecen en una línea, se recortan con puntos suspensivos, muestran el valor completo en tooltips y quedan dentro del marco. También se comprueba que la cabecera compacta mantenga una sola fila en el ancho mínimo en los 13 idiomas admitidos. Estos límites mantienen legibles el estado vacío y el ornamento de categoría en un área de trabajo estrecha.

El artefacto final proporcionado `artifacts/desktop-visual-rev069-final.json` pasó con Desktop 59/59, 286 casos de render WPF, 172 pases de layout y cero advertencias/errores de compilación. El JSON registra 11.719 ms de tiempo total del arnés y 3.106,8 ms en llamadas de layout. Son mediciones del arnés, no latencia de la aplicación abierta ni una afirmación de rendimiento. La prueba comprueba presentación local y límites renderizados; no se realizó UI Automation en ejecución ni validación de la app en vivo.

Rev069 es el contador de documentación fuente; su anexo protegido del Registro de Mejoras es Rev049. La versión del producto sigue en 25.2.0, sin cambios en la API pública, las rutas, los comandos, los permisos, IPC, dependencias ni ZIPs.

### Aclaración sobre la cobertura del DPI del sistema Windows (Rev070)

El arnés de render WPF mantiene fijo el DPI de Windows y el layout DIP de la ventana aislada. Un factor de escala de vista previa solo cambia la densidad de salida de `RenderTargetBitmap`; las etiquetas 100 % y 200 % describen densidad raster, no ajustes de escala de pantalla de Windows. El arnés no simula layout con DPI del sistema Windows al 125 %, 150 % o 200 %.

En `artifacts/desktop-visual-rev069-scale-audit.json`, `windows-system-dpi-layout-coverage` aparece explícitamente como `not-simulated`, sin factores de DPI de Windows aplicados y con factores raster de bitmap de vista previa `[1, 2]`. La ejecución proporcionada conserva las comprobaciones existentes de rutas, idiomas, temas e interacciones; no se retiraron casos funcionales. El artefacto registra Desktop 59/59 y 287 casos de render WPF con 172 pases de layout y cero advertencias/errores de compilación. Los 9.321 ms totales y 2.134,7 ms de llamadas de layout son tiempos del arnés, no latencia de la aplicación abierta. El comportamiento con el DPI real de Windows sigue sin verificarse.

Rev070 es el contador de documentación fuente; su anexo protegido del Registro de Mejoras es Rev050. La versión sigue en 25.2.0; no cambiaron API, rutas, comandos, permisos, IPC, dependencias ni ZIPs.

### IPC acotado, exportación segura de informes y rótulos localizados (Rev071)

Desktop lee cada respuesta IPC delimitada por salto de línea de forma incremental y se detiene al alcanzar el límite existente de 32 Mi caracteres UTF-16, antes de acumular una línea ilimitada o intentar deserializarla. La cancelación llega al ciclo de lectura; una respuesta excesiva sigue la ruta existente de desconexión sin continuar leyendo. El formato de transporte y el límite no cambian.

Las exportaciones se ejecutan de forma asíncrona, se preparan en un archivo temporal exclusivo dentro de la carpeta de informes y se publican con un nombre único sin sobrescribir informes anteriores. La cancelación y los errores de escritura devuelven un estado localizado, eliminan el temporal y conservan intactos los informes existentes. Los botones para copiar comandos de consola del dossier tienen IDs de automatización únicos y nombres accesibles localizados que incluyen el comando; el control para copiar la sintaxis CLI tiene un nombre propio y una región viva cortés anuncia el éxito o fallo localizado.

Los rótulos estáticos de interfaz de los visualizadores de árbol de tropas, talleres, seguridad de código, topología de módulos, diplomacia y componentes ahora provienen de los 13 diccionarios de idioma. Los rótulos más largos se ajustan dentro de sus tarjetas. Los nombres de tropas/facciones de ejemplo, comandos, identificadores, cifras y datos técnicos de muestra permanecen sin cambios.

cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-robustness-after-20260927-final.json <nul>" compiló con cero advertencias/errores, pasó Desktop 63/63 y 289 casos de render WPF con 182 pases de layout. El artefacto registra 11.758 ms totales del arnés y 3.483,7 ms en llamadas de layout; son tiempos del arnés, no latencia de la aplicación abierta. tools\Test-CalradiaForge-Desktop-Uia.bat pasó 23/23 comprobaciones de solo lectura; el informe registra ForegroundUnchanged=true y ningún evento de primer plano provocado por el proceso inspeccionado. UIA verifica el árbol accesible de la ventana lanzada, no la aprobación visual; no se inspeccionaron selectores, ejecución de trabajos, cambios de preferencias ni apariencia visual. No se inició una sesión de Bannerlord.

Después de añadir ajuste de línea a las estadísticas traducidas y a los indicadores compactos de las tarjetas estrechas, la ejecución final del BAT volvió a pasar las mismas 63 pruebas Desktop y 289 casos de render con 182 pases de layout. `artifacts/desktop-robustness-after-20260927-final-wrap.json` registra 10.939 ms totales del arnés y 3.120,8 ms en llamadas de layout; siguen siendo tiempos del arnés, no latencia de la aplicación. El BAT de UIA de solo lectura también se volvió a ejecutar y pasó 23/23 controles, incluidos los dos botones para copiar del dossier, con ForegroundUnchanged=true.

Rev071 es el contador de documentación fuente; su anexo protegido del Registro de Mejoras es Rev051. La versión sigue en 25.2.0; no cambiaron API pública, rutas, comandos, permisos, protocolo, dependencias ni ZIPs.

### Aclaración de evidencia — muestra de memoria y asignaciones (02-10-2026)

El inspector de memoria de Desktop usa perfiles de demostración integrados y debe etiquetarlos como muestras, no como telemetría verificada del juego. Los límites mostrados del SDK coinciden con el código de `ForgeAgentMemory`. El ciclo de selección histórico evita colecciones temporales, pero el formato de resúmenes y los arreglos del trabajo de vista/informe circundante sí asignan memoria; ni la inspección del código ni la nota anterior midieron las asignaciones de una interacción WPF completa.
