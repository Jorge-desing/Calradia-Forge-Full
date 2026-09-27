# Catálogo de Módulos del SDK (v8.0.0)

## Politics
- [x] ForgeKingdomManager
- [x] ForgeClanManager
- [x] ForgeRebellionSystem
- [x] ForgePolicyEnforcer
- [x] ForgeVassalRelations
- [x] ForgeDiplomacyEngine
- [x] ForgeMarriageArranger
- [x] ForgeHeirDesignator
- [x] ForgeElectionRigger
- [x] ForgeTreasonSystem
- [x] ForgeCivilWarTrigger
- [x] ForgeAllianceBuilder
- [x] ForgeTruceNegotiator
- [x] ForgeCasusBelli
- [x] ForgeSpyNetwork
- [x] ForgeAssassinationPlot
- [x] ForgeInfluenceMarket
- [x] ForgeRenownTracker
- [x] ForgeTitleGranter
- [x] ForgeFactionSplitter
- [x] ForgeNobleCourt

## Campaign
- [x] ForgeQuestManager
- [x] ForgeWeatherController
- [x] ForgeTimeManipulator
- [x] ForgeReligionSystem
- [x] ForgeTraitManager
- [x] ForgeBanditController
- [x] ForgeHideoutSpawner
- [x] ForgeMercenaryHiring
- [x] ForgeVillageHearthManager
- [x] ForgeLoyaltyModifier
- [x] ForgeSecurityModifier
- [x] ForgeWoundRateController
- [x] ForgeNotableSpawner
- [x] ForgeCaravanGuardManager
- [x] ForgeRandomEventTrigger
- [x] ForgePlagueSimulator
- [x] ForgeBanditInvasion
- [x] ForgeBountyHunting
- [x] ForgeSlaveTrade
- [x] ForgeTournamentGenerator
- [x] ForgeCustomSettlementBuilder
- [x] ForgeNavalTravel
- [x] ForgeCampingSystem
- [x] ForgeHuntingSystem
- [x] ForgeForagingSystem
- [x] ForgeCompanionSpawner

## Economy
- [x] ForgeTradeManager
- [x] ForgeCaravanController
- [x] ForgeWorkshopManager
- [x] ForgeMarketFluctuation
- [x] ForgeTaxesController
- [x] ForgeSmugglingSystem
- [x] ForgeBlackMarket
- [x] ForgeLoanSystem
- [x] ForgeBankSystem
- [x] ForgeInvestmentTracker
- [x] ForgeResourceDepletion
- [x] ForgeInflationController
- [x] ForgeTradeRouteOptimizer
- [x] ForgeMerchantGuilds
- [x] ForgeCurrencyExchange
- [x] ForgePricePegging
- [x] ForgeBribeManager
- [x] ForgeEconomicCrisis
- [x] ForgeProsperityBooster
- [x] ForgeFamineSimulator
- [x] ForgeSupplyChainManager

## Combat
- [x] ForgeFormationController
- [x] ForgeSiegeEngineManager
- [x] ForgeDamageModifier
- [x] ForgeArmorPenetration
- [x] ForgeWeaponBreakage
- [x] ForgeMoraleShock
- [x] ForgeFleeLogic
- [x] ForgeCavalryCharge
- [x] ForgePikemanBrace
- [x] ForgeArcherVolley
- [x] ForgeFriendlyFireAvoidance
- [x] ForgeWeatherCombatMod
- [x] ForgeNightVisionPenalties
- [x] ForgeBleedEffect
- [x] ForgePoisonEffect
- [x] ForgeStunEffect
- [x] ForgeCleaveStrike
- [x] ForgeHeadshotBonus
- [x] ForgeDismountChance
- [x] ForgeShieldBash
- [x] ForgeExecutionMoves
- [x] ForgeAmbushTactics
- [x] ForgeLootingBehavior
- [x] ForgeBodyguardAssignment
- [x] ForgeDuellingSystem
- [x] ForgeTargetPriority
- [x] ForgeWeaponSwapLogic
- [x] ForgeFormationSpacing

## UI
- [x] ForgeFloatingDamage
- [x] ForgeCustomCrosshair
- [x] ForgeMinimapOverlay
- [x] ForgeHealthBars
- [x] ForgeCombatCompass
- [x] ForgeAdvancedKillfeed
- [x] ForgeInventorySort
- [x] ForgePartyFilter
- [x] ForgeTroopTreeViewer
- [x] ForgeEncyclopediaExtender
- [x] ForgeDialogueOptionsUI
- [x] ForgeTradeProfitUI
- [x] ForgeKingdomOverviewUI
- [x] ForgeClanRolesUI
- [x] ForgeSiegeHUD
- [x] ForgeTournamentBracketUI
- [x] ForgeWeaponStatsUI
- [x] ForgeCharacterEditorExtra
- [x] ForgeMapBordersUI
- [x] ForgeArmyMoraleUI
- [x] ForgeGarrisonManagerUI
- [x] ForgeWorkshopStatsUI
- [x] ForgeSettlementIcons
- [x] ForgePrisonerRansomUI
- [x] ForgeRelationshipBarsUI
- [x] ForgeSkillTrackerUI
- [x] ForgeGoldTrackerUI
- [x] ForgeInfluenceGainUI
- [x] ForgeRenownGainUI
- [x] ForgeFoodConsumptionUI

## Calradia Forge v12.0.0 Major Update
- [x] Task 265: ForgeSaveChunker for >30KB save payload corruption protection
- [x] Task 266: ForgeMissionLogicBuilder with deferred _isInitialized lifecycle safety
- [x] Task 267: ForgeDetour memory unpatching and instruction restoration
- [x] Task 268: ForgeUI registry clearing and lifecycle cleanup
- [x] Task 269: Desktop UI simplification (group 46 tools into 5 subcategories, recursive search, compact header)
- [x] Task 270: Game UI improvements (versioning, context empty-state visibility, action parity)
- [x] Task 271: Suite version bump to 12.0.0 and distribution auto-packaging

## Ronda de Optimización y Corrección Integral (Mod, Core/SDK, Desktop)
- [x] Fase 0: Medición de línea base de rendimiento (Benchmark WPF Render y Core)
- [x] Fase 1: Optimización y corrección en Game Module (CalradiaForge.Mod)
  - [x] Inspector.cs: Búsqueda y captura zero-allocation sin LINQ ni concatenaciones intermedias
  - [x] SubModule.cs: Optimización de OnApplicationTick y hotkeys con banderas en caché
- [x] Fase 2: Optimización, resiliencia y concurrencia en Core y SDK (CalradiaForge.Core & CalradiaForge.Sdk)
  - [x] ForgeAgentMemory.cs: Bloqueos de sincronización thread-safe y límite acotado de episodios
  - [x] ForgeWeaveEngine.cs: Subscriptions preasignado sin LINQ y despacho optimizado
  - [x] ForgeEncyclopediaExtender.cs: Indexación rápida y consultas seguras
- [x] Fase 3: Optimización del Workbench de Escritorio (CalradiaForge.Desktop)
  - [x] DesktopShellViewModel.cs: RefreshVisibleTools y SynchronizeGroupExpansionSubscriptions sin LINQ
  - [x] Preservación de contratos estáticos (Rule C)
- [x] Fase 4: Verificación exhaustiva, auditoría y benchmarks comparativos
  - [x] Pruebas unitarias Core (251+ tests)
  - [x] Pruebas Desktop y contrato MVVM (51 tests)
  - [x] Benchmark WPF Render tests (medición comparativa ms y nodos)
  - [x] Verificación de statelessness (verify_stateless_behavior.ps1)
- [x] Fase 5: Documentación y empaquetado de distribución
  - [x] Actualización de Codemaps (docs/CODEMAP_*.md)
  - [x] Empaquetado automático de distribución (tools/package.ps1)

## ForgeWeave Innovación Novedosa (Custom Events, Circuit Breaker, Histogram/Percentiles)
- [x] Tarea FW-1: Contratos del SDK (ForgeEventKind.Custom, Topic, ForgeCircuitBreakerPolicy, IForgeEventRegistry.PublishCustom)
- [x] Tarea FW-2: Motor ForgeWeaveEngine (Despacho de Custom Events, matching de Topics con wildcard/prefix)
- [x] Tarea FW-3: Auto-recuperación Circuit Breaker (estados Closed -> Open -> Half-Open con backoff exponencial)
- [x] Tarea FW-4: Telemetría APM de latencia de cero asignación (Buffer circular de 64 muestras, P50/P95/P99, buckets de histograma)
- [x] Tarea FW-5: Pruebas unitarias exhaustivas en CalradiaForge.ForgeWeave.Tests y CalradiaForge.Tests
- [x] Tarea FW-6: Documentación bilingüe con /calradia-forge-docs (docs/FORGEWEAVE.md y .es.md) y Codemaps
- [x] Tarea FW-7: Verificación completa de suite y empaquetado de distribución (package.ps1)

## Calradia Forge SDK 23.0.0 — servicios compartidos observables y transaccionales
- [x] Añadir `ModuleLibrary.Resolve<T>` con estados explícitos, diagnóstico inmutable y acceso opcional al handle; mantener `Require<T>` fail-fast.
- [x] Añadir `SharedServiceBatch` con publicación de grupo atómica, colisiones validadas antes de publicar y lease de baja conjunto.
- [x] Añadir `SharedServiceMonitor<T>` con snapshot inicial, generaciones, callbacks síncronos aislados y desconexión silenciosa.
- [x] Cubrir estados, versiones, identidad de contrato, afinidad de hilo, errores, lifecycle, reentrancia y proveedor/consumidor entre ensamblados reales.
- [x] Incrementar `ForgeApi.Version` a 7 sin cambiar la versión del producto ni contratos anteriores; conservar `net472` y sin dependencias TaleWorlds nuevas.
- [x] Actualizar guías EN/ES, changelogs y codemap; anexar Rev023 y aclaración Rev024 al libro mayor protegido sin reescribir revisiones previas.
- [x] Validar con `cmd /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"`: 0 advertencias/errores, Core 263/263 y ForgeWeave 43/43.
- [x] Verificar cadena de integridad DOCX: 24 registros verificados, ninguno pendiente; no iniciar el juego ni generar ZIP.

## Paquete de mejoras de interfaz Gauntlet dentro del juego — 2026-09-25
- [x] Revisar los contratos de Gauntlet, la composición fija y las ocho rutas principales.
- [x] Añadir catálogo SDK buscable con selección, estado activo, watch de telemetría y panel de atajos.
- [x] Añadir acceso rápido al historial de comandos, navegación temporal y toast en la banda de estado.
- [x] Corregir el auditor para tamaños StretchToParent, contenedores Children repetidos y contexto de ItemTemplate.
- [x] Pasar la auditoría estructural de Gauntlet en los cuatro perfiles de ventana.
- [x] Validar el árbol actual: build Release con 0 advertencias/errores, verificación stateless y suite completa aprobada (Assets/Core/ForgeWeave/Desktop/WPF Render).
- [x] Revisar los siete textos nuevos en los 13 catálogos de idioma, con BOM/XML válidos y EN/SP comprobados.
- [x] Revisar el render en Bannerlord y la interacción con el catálogo SDK cuando una instancia del juego esté disponible.

## Calradia Forge 24.0.0 — SDK v8 con suscripciones administradas y memoria acotada
- [x] Añadir suscripciones ForgeWeave desechables que esperen conexiones/reconexiones, expongan su estado y permitan cancelación o baja explícita; hacer que `SubscribeWeave` falle explícitamente si el host no está disponible.
- [x] Limitar ForgeAgentMemory a 2.048 IDs globales; 128 hechos semánticos, 512 episodios totales (128 por tipo) y 128 entradas procedimentales por agente; añadir resultados `Try...` y `ClearAgent`.
- [x] Incrementar metadatos de fuente y contratos a producto 24.0.0 / `ForgeApi.Version = 8`, conservando los TFM y sin regenerar ZIPs.
- [x] Actualizar documentación EN/ES, codemap, changelog append-only y anexar Rev025 al libro protegido con integridad verificada.
- [x] Ejecutar `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: compilación `net472`/`net8.0` con 0 advertencias/errores, Core 275/275 y ForgeWeave 52/52.
- [x] Corregir el auditor de bindings Gauntlet para resolver comandos de `ItemTemplate` contra el ViewModel del elemento declarado por `ListPanel.DataSource`.
- [x] Verificar el codemap actual en contrato SDK 8; no generar ZIP ni iniciar Bannerlord.

## Calradia Forge 24.0.0 — ronda amplia de corrección SDK, API y Core
- [x] Releer reglas del repositorio y skills de .NET/Bannerlord; confirmar TFMs `net472;net8.0` y ausencia de referencias TaleWorlds en el target `net8.0`.
- [x] Ejecutar línea base con `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: compilación seleccionada sin advertencias/errores y suites Core/ForgeWeave aprobadas.
- [x] Actualizar `docs/sdk-reference.md` y `.es.md` con los límites reales de TTL, la contención de rutas `ModSettings` y la recuperación reintentable/reentrante de bajas ForgeWeave.
- [x] Anexar Rev039 bilingüe a `docs/CHANGELOG.md` y `.es.md` con las correcciones SDK/Core y declarar pendiente la validación posterior a los cambios mediante `.bat`.
- [x] Actualizar `docs/SYSTEM_DESIGN.md` y su par `.es.md`, más las secciones EN/ES de `docs/DESKTOP.md`, con límites del caminador y preflight ZIP; anexar Rev040 EN/ES sin modificar Rev039.
- [x] Revisar y corregir defectos reproducibles de ForgeAgentMemory, con pruebas límite y temporales.
- [x] Revisar el ciclo de vida, atomicidad, afinidad de hilo y diagnósticos de las APIs públicas SDK/ForgeWeave y agregar regresiones, incluidos los efectos inciertos de `Register` y la disposición reentrante.
- [x] Auditar Core por errores de corrección y robustez con cambios mínimos trazados al flujo real: aliases exactos, recorridos acotados, lectura de bytes del flujo abierto y preflight de ZIP limitado.
- [x] Ejecutar la validación final exclusivamente por `.bat`; `net472`/`net8.0` sin advertencias ni errores, Core 295/295 y ForgeWeave 65/65.
- [x] Actualizar documentación y codemap; anexar Rev044–Rev045 a los changelogs bilingües y Rev029–Rev031 al registro protegido. La cadena SHA-256 verificó 31 revisiones consecutivas con prefijos DOCX preservados.

## Mejoras Novedosas a la Aplicación Desktop WPF (2026-09-25)
- [x] Tarea WPF-1: Auditor Avanzado de Ensamblados y Guardado con AsmResolver (`DesktopAssemblyService.Audit`, rutas `SaveTypeDefinerAuditor` y `CampaignNamespaceGuard` para PE `.dll`/`.exe` verificando Anti-Shadowing, comportamiento sin estado, base IDs >= 2.500.000, serialización MBGUID y metadatos de distribución).
- [x] Tarea WPF-2: Panel de Telemetría APM y Malla de Eventos ForgeWeave en Vivo (`DesktopWorkspaceService.QueryLiveAsync` con acción `framework`, decodificación de `ForgeWeaveSnapshot`, tabla con estados de Circuit Breaker, percentiles P50/P95/P99, barras ASCII de histograma y proyección al libro de evidencias).
- [x] Tarea WPF-3: Simulador y Despachador de Eventos ForgeWeave (`LiveConsole` con normalización automática de comandos `publish`, `unquarantine`, `replay`, `status`, `handlers`, `journal`, `clear` hacia `cf.forgeweave.*`).
- [x] Tarea WPF-4: Paleta de Comandos y Navegación Rápida Ctrl+K (`DesktopShellViewModel.PaletteFilter` con filtrado por prefijos tácticos `>live`, `>diag`, `>asset`, `>sim`, etc., `SelectFirstPaletteToolCommand` con tecla `Enter` y cierre automático del panel).
- [x] Tarea WPF-5: Verificación integral de suites de pruebas (Core 263/263, ForgeWeave 43/43, Desktop MVVM 52/52, Desktop Render 274/274 y aceptación stateless).
- [x] Tarea WPF-6: Documentación bilingüe con estricta paridad EN/ES (`docs/DESKTOP.md`, `docs/ASSEMBLY_WORKBENCH.md` y `.es.md`).
- [x] Tarea WPF-7: Auto-empaquetado de distribución con `tools/package.ps1` per `.agents/rules/auto_packaging.md`.

## Innovaciones Avanzadas a la App Desktop WPF — Simulación Headless y Split Deck (Rev029)
- [x] Innovación 1: Visualizador Interactivo de Progresión de Tropas y Diff Semántico XML (DesktopSimulationService.SimulateTroopTree enrutado desde TroopTreeVisualizer e ItemBalanceAnalyzer). Genera DAG acíclico de 6 niveles, balance estadístico, elasticidad salarial y diff semántico entre archivos XML (pathA|pathB).
- [x] Innovación 2: Inspector Visual de Memoria Cognitiva de Agentes CoALA (DesktopSimulationService.InspectAgentMemory enrutado desde SaveInspector / ObjectInspector). Mide cuota global de 2.048 slots con barra de capacidad, audita los tres niveles (Semántico con TTL, Episódico con límite FIFO 512/128, y Procedimental).
- [x] Innovación 3: Estudio Táctico de Audio y Previsualizador de Formas de Onda (DesktopSimulationService.AuditAudioWaveform enrutado desde AudioFmodMixerInspector y SoundXmlSynthesizer). Conforme a annerlord_audio_system.md, analiza cabeceras RIFF WAVE, dBFS de pico (-1.4 dBFS), RMS, envolvente ASCII y espectro en 5 bandas.
- [x] Innovación 4: Simulador Headless de Campaña y Economía de Talleres (DesktopSimulationService.SimulateEconomyAndCampaign enrutado desde WorkshopEnterpriseSimulator y SettlementCalculator). Simulación a 30 días de 7 tipos de talleres, amortización de capital, equilibrio de insumos y cálculo del Índice de Riesgo de Rebelión ({rebellion}$).
- [x] Innovación 5: Modo Split-Screen / Vista Dividida y Pestañas Fijables (Split Deck con Ctrl+D). Permite anclar herramientas en un panel lateral (PinnedDeckCard) con retención de salida bruta, estado y evidencias sin romper contratos estáticos ni duplicar WorkbenchReportTabs.
- [x] Verificación de Pruebas: Creación y ejecución de DesktopSimulationServiceTests cubriendo las 5 innovaciones; 52 pruebas de contrato Desktop superadas; 275 casos de RenderTests superados; suite completa (Run-CalradiaForge-Tests.bat) y script erify_stateless_behavior.ps1 superados con 0 errores.
- [x] Documentación Bilingüe: Actualización sincrónica de docs/DESKTOP.md (secciones EN y ES con estricta paridad conceptual).
- [x] Auto-Packaging: Ejecución de 	ools\\package.ps1 y reporte de hashes SHA-256.

## Seguimiento de auditoría Core — validador de módulos (2026-09-26)
- [x] Acotar el recorrido de `ModuleValidator` por entradas, carpetas, profundidad y archivos XML; omitir puntos de reanálisis y reportar análisis parciales.
- [x] Usar pilas explícitas para el árbol XML y el grafo de dependencias; conservar la detección de incompatibilidades declaradas.
- [x] Añadir regresiones para profundidad e incompatibilidades y ejecutar `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` con cero advertencias/errores, Core 297/297 y ForgeWeave 65/65.
- [x] Actualizar la guía bilingüe, changelog append-only Rev046 y anexar Rev032 al registro protegido; cadena de integridad verificada durante el anexado.

## Paquete de interfaz Gauntlet y despliegue Steam (2026-09-26)
- [x] Importar `ui_calradiaforge_1.png` desde el panel central del Resource Browser; confirmar el diálogo de actualización y observar `ui_calradiaforge_1` en el inspector.
- [x] Documentar el acceso al menú con F10 predeterminado y la revisión en vivo autorizada en reglas y skills de Gauntlet/Resource Browser.
- [x] Integrar catálogo SDK, ayuda de teclado, historial de comandos y toast en el prefab generado; conservar los contratos del ViewModel y auditor geométrico.
- [x] Sincronizar todos los textos de interfaz con los catálogos de idioma.
- [x] Validar assets y prefab, compilar Release sin advertencias/errores y pasar los gates obligatorios: statelessness, Core 297/297, ForgeWeave 65/65, Desktop 52/52, WPF Render 275 casos y Asset Pipeline.
- [x] Preservar el TPAC importado de Steam con hash SHA-256 antes y después del despliegue; TpacTool no se usa como gate.
- [x] Desplegar el módulo actualizado a la instalación de Steam; verificar prefab instalado y respaldos de archivos reemplazados.
- [x] Observar render e interacción en Bannerlord; F10 abre el panel, el catálogo SDK filtra herramientas y la vista se cierra correctamente.

## Lecciones operativas de Gauntlet y corrección de F10 (2026-09-26)
- [x] Registrar el diagnóstico de F10 (`IsKeyPressed` y `IsKeyDown` falsos con `IsKeyDownImmediate` verdadero) y el fallback por flanco ascendente, con rearme al soltar la tecla.
- [x] Documentar el menú **Import New Assets** desde un clic derecho en el área vacía central de **Assets**, no desde el árbol izquierdo; distinguir el selector de archivos de una importación confirmada.
- [x] Dejar el orden Editor/juego y el cierre de Bannerlord y Ordenador entre correcciones en reglas y skills de UI.
- [x] Sincronizar el procedimiento en `AGENTS.md`, `CODEX.md`, `GEMINI.md`, las reglas de UI/entrada y las skills de Gauntlet/Resource Browser; alinear el límite de evidencia en la skill FBX y revisar enlaces internos.

## Optimización Medida del Workbench Desktop WPF (Rev034)
- [x] Fase 1: Optimización de colecciones reactivas y hot paths en `DesktopShellViewModel` (`FilterPaletteTools` sin LINQ/slicing, predimensionamiento de listas de grupos y entradas de rail, eliminación de asignaciones intermedias).
- [x] Fase 2: Optimización de conversores XAML (`DesktopConverters.cs`) eliminando boxing de enumeradores en `PinnedGlyphConverter` y caching de recursos.
- [x] Fase 3: Modernización C# 12 MVVM en `WorkspacePageViewModel` y `DesktopSimulationViewModels` (constructores primarios, expresiones de colección, switch expressions concisas).
- [x] Fase 4: Optimización en generadores de informes y servicios (`DesktopSimulationService.cs` y `DesktopWorkspaceService.cs` preasignando `StringBuilder` y evitando LINQ en colecciones de evidencia).
- [x] Fase 5: Verificación exhaustiva de contratos (Rule C intacta, 52 pruebas MVVM/protocolo, verificación stateless 4/4).
- [x] Fase 6: Medición empírica comparativa antes vs después en la suite de 275 tests de renderizado WPF.
- [x] Fase 7: Documentación bilingüe (`docs/DESKTOP.md`), registro de aprendizajes (`/learn`) y auto-empaquetado (`tools/package.ps1`).


## Mejoras Gauntlet — paleta global y navegación por teclado (2026-09-26)
- [x] Revisar las reglas y skills aplicables; confirmar el módulo net472, el F10 corregido y el enrutamiento Gauntlet existente.
- [x] Agregar paleta global para rutas integradas y todos los destinos SDK, con operaciones de vista seguras.
- [x] Persistir hasta 32 favoritos y 10 rutas recientes en JSON versionado, usando IDs estables y escritura atómica.
- [x] Separar el foco de teclado de la ruta activa; cubrir la búsqueda de paleta y los campos visibles del banco de ensamblados.
- [x] Localizar las nuevas cadenas en los 13 catálogos existentes y mantener EN/SP revisados.
- [x] Ejecutar build, verificaciones requeridas y auditorías Gauntlet/localización.
- [x] Desplegar a Steam con simulación previa, respaldo y verificación de integridad del TPAC.
- [x] Verificar en vivo F10, apertura de la paleta con Ctrl+P físico y los estados de búsqueda con resultados y sin coincidencias.
- [ ] Completar en vivo la selección de una ruta integrada y un destino SDK, la navegación solo con teclado, Escape y la persistencia de favoritos/recientes después de reiniciar.

## Optimización Integral de 4 Pilares del Workbench Desktop WPF (Rev037)
- [ ] Pilar 1: Conversores XAML con Caché y Congelado de Geometries y Brushes en `DesktopConverters.cs` e invalidación en `DesktopThemeService.cs`.
- [ ] Pilar 2: Resolución de Bindings en `GenericOperationDashboardViewModel` (`DesktopSimulationViewModels.cs`) eliminando fallos de reflexión en layout passes y compartición de instancia canónica.
- [ ] Pilar 3: Clasificación O(1) de `StudioKind` en `ToolDefinition` (`ToolCatalog.cs`) y precomputación de propiedades booleanas `{ get; }` en `ToolPageViewModel` (`WorkspacePageViewModel.cs`).
- [ ] Pilar 4: Optimización de clasificación de prefijos en la paleta de comandos Ctrl+K (`DesktopShellViewModel.cs`) y precomputación de `GroupIconKey` en `ToolGroupViewModel` (`WorkbenchViewModels.cs`).
- [ ] Verificación de Suites: Compilación `net8.0-windows` sin advertencias/errores, 54 pruebas Desktop MVVM/contratos, 275 tests de renderizado WPF con medición de rendimiento comparativa y verificación stateless 4/4.
- [ ] Documentación bilingüe (`docs/DESKTOP.md`), registro de aprendizajes (`/learn` en `learning_proposal.md`) y auto-empaquetado (`tools/package.ps1`).

## Correcciones visuales y de disposición WPF — 2026-09-26 (Rev056)
- [x] Corregir el ancho del rail y reducir Split Deck para que la página siga utilizable en la ventana mínima; añadir un caso de layout con Split Deck activo.
- [x] Ajustar el marco del mapa a 160×90 DIP para su fuente 448×252, conservar `Stretch="Uniform"` y quitar cachés bitmap decorativas de escala fija.
- [x] Localizar rótulos fijos, ayudas y estados vacíos/fijados del shell en los 13 diccionarios de idioma; cubrir cambios de etiquetas derivadas al limpiar Split Deck.
- [x] Validar con `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause <nul"`: build sin advertencias/errores y Desktop MVVM 55/55.
- [x] Comparar cinco ejecuciones del render `.bat`: 275 casos y 152 pases de layout en cada una; mediana 11.335 ms frente a 13.652 ms de baseline, y mediana de layout 2.075 ms frente a 2.397 ms. Estas son métricas del harness.
- [x] Actualizar esta guía y los changelogs EN/ES; anexar Rev037, el resultado final Rev038 y la aclaración de pruebas Rev039 al registro protegido, conservando los DOCX previos y verificando la cadena completa hasta Rev039.

## Gran Ronda de Corrección de Errores, Agente BugHunter y Optimización de Empaquetado (Rev057)
- [x] Corregir formato de cadenas y variables en `AgentCognitiveMemoryBehavior.cs` y añadir salvaguarda nula para memorias episódicas de parientes.
- [x] Refactorizar `DataBehavior.cs` a 100% stateless (vaciar `SyncData`, mover a `OnGameStart`, limpiar en eventos de sesión).
- [x] Eliminar exclusión de omisión en `tools/verify_stateless_behavior.ps1` (verificación 4/4 completa sobre todos los comportamientos sin excepciones).
- [x] Documentar bloques catch en `HarmonyDiagnostics.cs`, `PanelViewModel.cs` y `AssemblyWorkbenchService.cs`.
- [x] Implementar `BugHunterAgent` en `agents/subagents.py` y `agents/orchestrator.py` con herramientas `audit_code_smells` y `audit_concurrency_hazards`.
- [x] Añadir subcomando `bughunt` en `tools/run_forge_agents.py` y suite de pruebas unitarias en `tests/test_forge_agents.py` (22/22 pasadas).
- [x] Optimizar notablemente `tools/package.ps1` con compresión .NET `ZipFile::CreateFromDirectory` (12.5x más rápida), streaming de árboles de ficheros con `EnumerateFiles` y caché de directorios, validación XML en streaming con `XmlReader`, y flags `-SkipTests` y `-Quick` (reducción de 2m30s a 34s).
- [x] Superar todas las puertas de verificación (Core, ForgeWeave, Desktop MVVM, RenderTests, UI Automation 20/20, statelessness 4/4 y verify 5/5 subagentes).
- [x] Desplegar a Steam mediante `tools/deploy_to_game.ps1`.
- [x] Sincronizar codemaps de arquitectura y comportamientos (`CODEMAP_ARCHITECTURE.md`, `CODEMAP_CAMPAIGN_BEHAVIORS.md`) y redactar Propuesta 18 en `learning_proposal.md`.

## Gran Ronda de Compactación de Tokens Multi-Agente & Memoria CoALA (Rev058)
- [ ] Implementar destiladores semánticos dedicados para `audit_code_smells`, `audit_concurrency_hazards` y `audit_section_playbooks` en `agents/compactor.py`.
- [ ] Optimizar `_distill_generic` para no expandir tokens en textos cortos y registrar las 12 herramientas en `ForgeTokenCompactor._DISTILLERS`.
- [ ] Configurar el preset `deep` (32,768 tokens) como predeterminado en `CompactionPreset`.
- [ ] Implementar arquitectura de memoria cognitiva CoALA (`AgentMemoryContext`: Working, Episodic, Semantic) para historial de agentes en `agents/orchestrator.py`.
- [ ] Implementar subcomando CLI `compact` en `tools/run_forge_agents.py` para benchmarking exhaustivo de compactación sobre las 12 herramientas.
- [ ] Añadir suite de pruebas unitarias en `tests/test_forge_agents.py` para nuevos destiladores, memoria CoALA y subcomando `compact`.
- [ ] Ejecutar `py -3.12 tools/run_forge_agents.py compact` y verificar telemetría de compresión sin pérdidas.
- [ ] Ejecutar suites de verificación completas (`test_forge_agents.py`, `dotnet build`, `verify_stateless_behavior.ps1`).
- [ ] Redactar Propuesta 19 en `learning_proposal.md` y sincronizar `docs/AUTONOMOUS_AGENTS.md` y `docs/AUTONOMOUS_AGENTS.es.md`.
