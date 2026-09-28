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
- [x] Implementar destiladores semánticos dedicados para `audit_code_smells`, `audit_concurrency_hazards` y `audit_section_playbooks` en `agents/compactor.py`.
- [x] Optimizar `_distill_generic` para no expandir tokens en textos cortos y registrar las 12 herramientas en `ForgeTokenCompactor._DISTILLERS`.
- [x] Configurar el preset `deep` (32,768 tokens) como predeterminado en `CompactionPreset`.
- [x] Implementar arquitectura de memoria cognitiva CoALA (`AgentMemoryContext`: Working, Episodic, Semantic) para historial de agentes en `agents/orchestrator.py`.
- [x] Implementar subcomando CLI `compact` en `tools/run_forge_agents.py` para benchmarking exhaustivo de compactación sobre las 12 herramientas.
- [x] Añadir suite de pruebas unitarias en `tests/test_forge_agents.py` para nuevos destiladores, memoria CoALA y subcomando `compact`.
- [x] Ejecutar `py -3.12 tools/run_forge_agents.py compact` y verificar telemetría de compresión sin pérdidas.
- [x] Ejecutar suites de verificación completas (`test_forge_agents.py`, `dotnet build`, `verify_stateless_behavior.ps1`).
- [x] Redactar Propuesta 19 en `learning_proposal.md` y sincronizar `docs/AUTONOMOUS_AGENTS.md` y `docs/AUTONOMOUS_AGENTS.es.md`.

## Optimización de Empaquetado Multihilo y Compresión Concurrente (Rev059)
- [x] Implementar clase compilada en C# `FastPackageEngine` en `tools/package.ps1` con `FastTreeCopy`, `CopyModuleDirectory` y `CompressParallel`.
- [x] Poda de directorios en nivel superior (`bin`, `obj`, `__pycache__`, `api`), reduciendo tiempo de staging a 43 ms (>99% de reducción).
- [x] Filtrado preventivo en memoria de residuos (`.bak.*`, `.cfcrash`, `.log`, `.backup`, `.pdb`, `.exe`) sin pasadas de borrado sobre disco.
- [x] Compresión concurrente paralela de los 3 archivos ZIP (`Parallel.Invoke`) reduciendo tiempo al archivo más pesado.
- [x] Compresión adaptativa (`Optimal` en release, `Fastest` con `-Quick`).
- [x] Consolidación de build MSBuild (`dotnet build CalradiaForge.sln -c Release --no-restore -v:minimal` en un solo pase).
- [x] Superar pruebas de integridad y empaquetado: 3 paquetes ZIP verificados con `audit_package.py`, `Test-ZipEntries` y hashes SHA-256.
- [x] Redactar Propuesta 20 en `learning_proposal.md`.

## Gran Ronda de Corrección de Errores al Mod y Expansión de BugHunterAgent (Rev060)
- [x] Blindar todas las llamadas a `vm` en `SubModule.cs` (`OnApplicationTick`, `GetKeyboardFocusLabel`, `SyncNavigationPaletteFocus`) ante referencias nulas.
- [x] Migrar la caché estática de `GameLocalization.cs` a `ConcurrentDictionary<string, string>` para concurrencia segura multihilo.
- [x] Estandarizar la comprobación `args == null || args.Count < N` en todas las funciones de consola de `ForgeCommands.cs`.
- [x] Corregir la actualización de hechos semánticos en `AgentCognitiveMemoryBehavior.cs` guardando la relación real (`hero1.GetRelation(hero2)`) y el delta en `LastRelationDelta`.
- [x] Fortalecer `AssemblyWorkbenchService.cs` con salvaguardas nulas ante referencias de ensamblados ofuscadas o anómalas.
- [x] Expandir `BugHunterAgent` en `agents/tools.py` (`audit_code_smells`, `audit_concurrency_hazards`) y añadir tests en `tests/test_forge_agents.py`.
- [x] Ejecutar suites de verificación completas (`dotnet build`, `verify_stateless_behavior.ps1`, `Run-CalradiaForge-Tests.bat`, `run_forge_agents.py bughunt` y `verify`).
- [x] Redactar Propuesta 21 en `learning_proposal.md`, sincronizar codemaps y generar paquetes de distribución con `tools/package.ps1`.

## Compatibilidad Universal de Reglas de Antigravity con OpenAI Codex CLI (Rev061)
- [x] Actualizar `.codex/config.json` a versión `25.2.0`, declarar `"rulesPath": ".agents/rules"` y `"rulesDirectory": ".agents/rules"`.
- [x] Añadir Sección 5 "Cross-Tool Rule Discovery & Parity Protocol" en `.agents/rules/codex_compatibility.md`.
- [x] Añadir Sección 7 "Rules Taxonomy & Domain Gateway Routing" en `AGENTS.md` (43 reglas en 7 clusters).
- [x] Añadir Sección 5 "Antigravity Modular Rules Index & Compliance" en `CODEX.md`.
- [x] Sincronizar documentación bilingüe en `docs/CODEX_COMPATIBILITY.md` y `docs/CODEX_COMPATIBILITY.es.md`.
- [x] Redactar Propuesta 22 en `learning_proposal.md` y artefacto `codex_antigravity_rules_proposal.md`.
- [x] Superar pruebas de verificación y compilar suite de pruebas.

## Gran Ronda de Corrección de Errores Integral y Blindaje Defensivo (Rev062)
- [x] Blindar llamadas a `vm` en `SubModule.cs` ante referencias nulas (`vm?.ExecuteHistoryPrevious()`, `vm?.ExecuteHistoryNext()`).
- [x] Fortalecer `Runtime.cs` (`Handle`): guarda obligatoria `if (s == null) throw new ArgumentNullException(nameof(s))`, normalización de claves en `case "pin"`, resolución segura de `DeclaringType` en métodos dinámicos de `case "patcher"`, y escape seguro de caracteres JSON en `case "agent-memory-query"`.
- [x] Corregir `SnapshotComparer.cs`: guarda preventiva `if (before == null) return "Before snapshot is missing"` para evitar `NullReferenceException`.
- [x] Blindar `PipeServer.cs`: manejo seguro `p.Request?.Id` y validación nula de `execute` en `Process()`.
- [x] Corregir paridad de traducciones alemanas en `Strings.de.xaml` (`Viz.Diplomacy.MonarchLabel` = `HERRSCHER`) y resolver CS0246 en `RenderTests/Program.cs` (`using System.Windows.Documents`).
- [x] Expandir `BugHunterAgent` en `agents/tools.py` con 4 nuevos inspectores estáticos (llamadas a `vm`, guardas de `Request`, guardas de `SnapshotComparer` y seguridad de `PipeServer`).
- [x] Superar 100% de las suites de prueba: 31/31 pruebas unitarias de agentes (`test_forge_agents.py`), 239 Core, 73 ForgeWeave, 63 Desktop MVVM, 289 WPF Render/Layout, y 4/4 en `verify_stateless_behavior.ps1`.
- [x] Generar paquetes de distribución release 25.2.0 (`Modules`, `Source-SDK`, `Desktop`) con `tools/package.ps1`.

## Gran Ronda de Corrección de Errores, Concurrencia de Patcher y Robustez de UI (Rev063)
- [x] Blindaje de concurrencia y seguridad de memoria en `src/CalradiaForge.Sdk/Patcher/ForgePatcher.cs` (`_syncLock` en `appliedPatches`, clonación segura en `RevertAll`, y verificación segura de punteros JIT en `PatchRecord.IsIntact()` contra `IntPtr.Zero` y excepciones de acceso).
- [x] Blindaje de formateo en `src/CalradiaForge.Mod/Commands/ForgeCommands.cs` (`ListPatches` y `VerifyIntegrity` para métodos dinámicos sin `DeclaringType`).
- [x] Blindaje ante referencias nulas de categoría en `src/CalradiaForge.Mod/PanelViewModel.cs` (`(currentCategory ?? "overview").ToUpperInvariant()` en `MacroActionHint` y `ExecuteCategoryHelp`).
- [x] Expansión de `BugHunterAgent` en `agents/tools.py` (`audit_concurrency_hazards` con verificación de `ForgePatcher`, y `audit_code_smells` con guardas de UI y comandos).
- [x] Añadir pruebas unitarias de regresión en `tests/test_forge_agents.py`.
- [x] Verificación completa de compilación (`dotnet build CalradiaForge.sln -c Release` 0 errores / 0 advertencias).
- [x] Ejecución de suite de pruebas: stateless behavior (4/4), pruebas completas (Core, ForgeWeave, Desktop MVVM, RenderTests), y pruebas unitarias de agentes (31+ pasadas).
- [x] Generación automática de paquetes de distribución vía `tools/package.ps1` con `FastPackageEngine`.
- [x] Redacción de Propuesta 24 en `learning_proposal.md`.
- [x] Sincronización y push a GitHub (`main`) siguiendo `RULE[git_sync_workflow.md]`.

## Gran Ronda de Mejoras Visuales a la Interfaz Gauntlet In-Game y Telemetría Táctica (Rev064)
- [x] Corrección de la geometría de Viewport en `src/CalradiaForge.Mod/PanelViewModel.cs` (`EvidenceHeight` 482f en modo Focus para cumplir la regla estricta de 702f con margen inferior).
- [x] Enriquecimiento de telemetría en tiempo real en encabezado táctico (`src/CalradiaForge.Mod/PanelViewModel.cs`): badges para estado de memoria CoALA, despachos ForgeWeave y rol activo de modder con colores dinámicos.
- [x] Sincronización y armonización de iconos de navegación semánticos en `tools/generate_assets.py` y `tools/validate_game_icon_assets.py`.
- [x] Preservación del contrato de scrollbar de evidencia de alto contraste sin `ImageWidget` en `ForgeEvidenceFrame`.
- [x] Refinamiento estético del panel de playbooks tácticos procedimentales (`ForgePlaybookPanel`) y comandos curados (`ForgeCategoryCommandsPanel`).
- [x] Verificación de compilación limpia de la solución (.NET Framework 4.7.2 y .NET 8).
- [x] Verificación de 4/4 criterios sin estado (`verify_stateless_behavior.ps1`).
- [x] Ejecución de suite completa de pruebas de la solución (`Run-CalradiaForge-Tests.bat`): 338 Core, 73 ForgeWeave, 63 Desktop, 289 WPF Render.
- [x] Ejecución de suite multi-agente (`test_forge_agents.py` 31/31 pasadas).
- [x] Generación automática de paquetes de distribución (`package.ps1`) con `FastPackageEngine`.
- [x] Redacción de Propuesta 25 en `learning_proposal.md` y formalización de la regla de Staging Selectivo en `.agents/rules/git_sync_workflow.md`, `AGENTS.md` y `CODEX.md`.
- [x] Sincronización selectiva y push a GitHub (`main`) siguiendo la nueva regla estricta de staging aislado.

## Ajuste de Reglas de Staging y Sincronización Completa del Proyecto (Rev065)
- [x] Ajustar la definición de la Regla E (`AGENTS.md`), Regla 6 (`CODEX.md`) y `.agents/rules/git_sync_workflow.md` para garantizar que todos los cambios intencionales del proyecto se suban a GitHub, definiendo con precisión "no externos" como exclusión estricta de residuos temporales, cachés (`__pycache__`, `*.pyc`), volcados de depuración y secretos.
- [x] Actualizar `.gitignore` con exclusiones automáticas de `__pycache__/` y `*.pyc`.
- [x] Preparar (staging) y comitear todas las mejoras del proyecto pendientes: actualización de Desktop WPF (panorama rev072, footer y shell), anexos y registros inmutables DOCX del changelog (Rev052 a Rev055), pruebas de contratos Gauntlet (`AdvancedToolsTests.cs`), herramientas de auditoría y assets.
- [x] Validar 100% de la suite de pruebas: compilación limpia en Release, 4/4 comportamientos sin estado, 338 Core, 73 ForgeWeave, 63 Desktop, 289 WPF Render y 31/31 agentes.
- [x] Sincronizar y pushear a GitHub (`main`).

## Workflows de GitHub Actions para CI/CD, Empaquetado y Auditoría de Integridad (Rev066)
- [x] Eliminar workflow genérico roto `python-package-conda.yml` (que dependía de `environment.yml` inexistente).
- [x] Crear workflow de Integración Continua (`.github/workflows/ci.yml`) en `windows-latest`: compilación .NET 8 / Framework 4.7.2, validación de comportamientos sin estado (4/4), suite completa de pruebas (Core 338, ForgeWeave 73, Desktop 63, RenderTests 289), verificación de sprites/iconos Gauntlet y suite multi-agente.
- [x] Crear pipeline de empaquetado y distribución (`.github/workflows/release-packaging.yml`): empaquetado multihilo FastPackageEngine (`package.ps1`), auditoría de seguridad y ausencia de DLLs de TaleWorlds, subida de artefactos y publicación automática de GitHub Releases en tags `v*`.
- [x] Crear workflow de auditoría de documentación e integridad criptográfica (`.github/workflows/ledger-docs-integrity.yml`): verificación de cadena SHA-256 de 55 revisiones del Registro de Mejoras, paridad bilingüe EN/ES, playbooks de Gauntlet y detección de code smells.
- [x] Sincronizar y pushear a GitHub (`main`) bajo la Regla E ajustada.

## Purga de Archivos Residuales, Logs Temporales y Scripts Obsoletos (Rev067)
- [x] Auditoría exhaustiva y preservación incondicional de los 176 archivos ZIP históricos de releases en `artifacts/`.
- [x] Preservación de metadatos críticos de auditoría (`package-audit-*.json`, `package-sha256-*.txt`, `localization-audit-*.json`) y copias de seguridad de TPAC.
- [x] Eliminación de 511 archivos y carpetas residuales (~145 MB) en `artifacts/` (directorios temporales de pruebas `desktop-focus-safe`, `visual-stage`, `agent-runs`, `docx-render`, `ui-restore`, logs huérfanos).
- [x] Eliminación completa del directorio temporal obsoleto `tmp/` (129 archivos entre renders PNG antiguos, logs y volcados de prueba).
- [x] Eliminación de scripts scratch, ejecutables de prueba temporales (`test_hash*.exe`, `test_hash*.cs`), volcados JSON sueltos y capturas de pantalla de recorte en la raíz del repositorio.
- [x] Desvinculación de git y purga física de todos los directorios `__pycache__` y archivos de bytecode compilado `*.pyc`.
- [x] Verificación de paso completo de los controles de calidad: compilación limpia en Release (0 errores / 0 advertencias), verificación de comportamiento sin estado (4/4), suite de pruebas de la solución (Core 338, ForgeWeave 73, Desktop 63, WPF Render 289) y pruebas de agentes de Antigravity (31/31).
- [x] Sincronización y push a GitHub (`main`) bajo la Regla E.

## Gran Ronda de Mejoras Visuales a los 8 Visualizadores y Estudios de Dominio WPF (Rev068)
- [x] Refinamiento estético y enriquecimiento gráfico del Visualizador de Jerarquía de Tropas (`TroopTreeVisualizerDashboardTemplate`): medidores visuales de HP/vitalidad, medidores de armadura con insignia de escudo, badges de rol marcial y encuadre noble dorado.
- [x] Refinamiento del Estudio de Audio Táctico (`AudioMixerInspectorDashboardTemplate`): chasis de osciloscopio táctico con retícula de decibelios, espectro de 5 bandas con etiquetas audibles y vúmetros estéreo duales (L/R) calibrados.
- [x] Enriquecimiento del Simulador de Empresas y Equilibrio Cívico (`WorkshopSimulatorDashboardTemplate`): barras de rentabilidad comparativa con desglose de coste/beneficio, insignia de máximo rendimiento y medidores gráficos de prosperidad, lealtad y riesgo de rebelión.
- [x] Perfeccionamiento del Inspector de Memoria Cognitiva CoALA (`AgentMemoryInspectorDashboardTemplate`): tarjetas de hechos semánticos con barras TTL, línea de tiempo episódica con pips temporales y formato de reglas procedimentales en bloques tácticos legibles.
- [x] Refinamiento del Radar de Seguridad CLR y Metadatos PE (`CodeSecurityAuditorDashboardTemplate`): sellos heráldicos de conformidad, badges de reglas y fichas de verificación de tokens.
- [x] Armonización del Visualizador de Topología de Módulos (`ModuleHierarchyValidatorDashboardTemplate`): pipeline de carga Native a CalradiaForge con flechas de progresión marcial y contadores de dependencias.
- [x] Enriquecimiento del Barómetro Geopolítico y del Senado (`KingdomDiplomacyStudioDashboardTemplate`): barómetros de tensión entre reinos con código de color dinámico, medidor de consenso senatorial y balance militar.
- [x] Pulido del Estudio de Síntesis de Planos de Componentes (`ComponentGeneratorStudioDashboardTemplate`) y Flight Deck de Operaciones Genéricas (`GenericOperationOverviewDashboardTemplate`).
- [x] Verificación de compilación limpia de la solución en Release (0 errores / 0 advertencias).
- [x] Verificación de 4/4 criterios sin estado (`verify_stateless_behavior.ps1`).
- [x] Ejecución de la suite de pruebas de Desktop MVVM (63 pruebas) y suite completa de Render WPF (289 casos en 3 temas y 4 escalas).
- [x] Ejecución de verificación UI Automation de la ventana WPF.
- [x] Sincronización y push a GitHub (`main`) bajo la Regla E.

## Gran Ronda de Mejoras Visuales al Shell Global, Navegación, Dossier y Footer WPF (Rev069)
- [x] Fase 1: Refinamiento del Encabezado Táctico (`WorkbenchHeaderControl.xaml`) y Barra de Navegación (`WorkbenchNavigationControl.xaml`, `App.xaml`).
- [x] Fase 2: Perfeccionamiento del Centro de Acción, Tool Dossier (`ToolDossierControl.xaml`) y Split Deck (`WorkbenchWorkspaceControl.xaml`).
- [x] Fase 3: Modernización de la Cinta de Estado Táctica Inferior (`WorkbenchFooterControl.xaml`) y Modal de Paleta de Comandos (`CommandPaletteControl.xaml`).
- [x] Fase 4: Armonización de Paletas y Materiales Tácticos (`TacticalPalette.xaml`, Parchment, HighContrast) y Flight Deck Genérico (`ToolPageTemplates.xaml`).
- [x] Fase 5: Verificación de compilación limpia de la solución en Release (0 errores / 0 advertencias).
- [x] Fase 6: Verificación de 4/4 criterios sin estado (`verify_stateless_behavior.ps1`).
- [x] Fase 7: Pruebas de contrato Desktop MVVM (63 pruebas) y suite de Renderizado WPF (289 casos).
- [x] Fase 8: Benchmarking de rendimiento profundo con harness de renderizado en 5 pasadas comparativas.
- [x] Fase 9: Suite de pruebas de agentes y UI Automation (`test_forge_agents.py` 31/31 pasadas).
- [x] Fase 10: Documentación de aprendizajes (`learning_proposal.md`), sincronización y push a GitHub (`main`) bajo la Regla E.

## Gran Ronda de Mejoras de Funcionalidad del Banco de Trabajo Desktop WPF (Rev070)
- [x] Fase 1: Interactividad en Estudios Tácticos de Dominio (`DesktopSimulationViewModels.cs` y `ToolPageTemplates.xaml`): filtros de tropa, simulación de bajas, cálculo económico reactivo de talleres, decay y búsqueda en memoria CoALA, presets acústicos y modulador de tensión diplomática.
- [x] Fase 2: Motor de Exportación Multiformato y Telemetría IPC (`DesktopReportExportService.cs`, `DesktopSessionService.cs`, `WorkspacePageViewModel.cs`): exportación atómica Markdown y JSON estructurado, medición de latencia ping IPC y consultas en caliente.
- [x] Fase 3: Gestión Avanzada del Catálogo y Filtros en el Shell (`DesktopShellViewModel.cs`): filtro rápido por favoritos, filtro por ToolKind y contador dinámico de herramientas visibles.
- [x] Fase 4: Verificación exhaustiva (compilación Release 0 errores, stateless 4/4, Desktop MVVM 63 pruebas, WPF Render 289 pruebas, Antigravity Agents 31 pruebas y UI Automation smoke).
- [x] Fase 5: Registro de aprendizaje (Propuesta 27 en `learning_proposal.md`), sincronización y push a GitHub (`main`) bajo la Regla E.

## Gran Ronda de Mejoras de Funcionalidad Táctica, Topología de Módulos y Telemetría IPC en Tiempo Real (Rev071)
- [x] Fase 1: Interactividad en los 3 Estudios de Dominio Restantes (`DesktopSimulationViewModels.cs` y `ToolPageTemplates.xaml`): auditor de seguridad CLR interactivo con alternador de escaneo forense, simulación de orden de carga de módulos DAG con detección de conflictos, y síntesis interactiva de planos XML con vista previa en vivo y copiado.
- [x] Fase 2: Telemetría IPC en Vivo en Shell y Footer (`DesktopSessionService.cs`, `DesktopShellViewModel.cs` y `WorkbenchFooterControl.xaml`): monitor de latencia en tiempo real, comando de ping/heartbeat reactivo y estado de conexión online/standalone.
- [x] Fase 3: Cableado y Robustez XAML (`ToolPageTemplates.xaml`): alineación de enlaces de datos, botones de acción interactiva y garantía estricta de `Mode=OneWay` en elementos `<Run>`.
- [x] Fase 4: Verificación exhaustiva (compilación Release 0 errores, stateless 4/4, Desktop MVVM 63 pruebas, WPF Render 289 pruebas, Antigravity Agents 31 pruebas y UI Automation smoke).
- [x] Fase 5: Registro de aprendizaje (Propuesta 28 en `learning_proposal.md`), actualización de codemaps, sincronización y push a GitHub (`main`) bajo la Regla E.

## Gran Ronda de Mejoras de Consolidación CoALA, Barómetro Senatorial y Flight Deck Interactivo (Rev072)
- [x] Fase 1: Motor de Consolidación Cognitiva CoALA y Filtro por Categorías (`DesktopSimulationViewModels.cs`): comando `ConsolidateMemoriesCommand` (ciclo de sueño, poda de hechos expirados y cálculo de cohesión), filtro por categorías `SelectedCategoryFilter` con comando `FilterCategoryCommand`, e inyección de hechos personalizados `AddSemanticFactCommand`.
- [x] Fase 2: Simulación de Votación Senatorial y Acciones Diplomáticas (`DesktopSimulationViewModels.cs`): aliases completos en `FactionStanceViewModel` y `KingdomDiplomacyDashboardViewModel`, comando de votación senatorial interactivo `SimulateSenateVoteCommand` con cálculo de influencia de clanes nobles y decretos, y comandos de acción en caliente `DeclareWarCommand` y `ProposePeaceTreatyCommand`.
- [x] Fase 3: Flight Deck Operacional Interactivo para más de 180 Herramientas (`DesktopSimulationViewModels.cs`): comando `RunDiagnosticScanCommand`, progreso en tiempo real `ScanProgressPercentage`, colección observable de hallazgos `DiagnosticFindings` y comando `ClearFindingsCommand`.
- [x] Fase 4: Modificadores de Pericias de Capitán en Árbol de Tropas (`DesktopSimulationViewModels.cs`): comando `ToggleCaptainPerksCommand`, propiedad `CaptainPerksActive` y bonificaciones de cohesión y soldada.
- [x] Fase 5: Cableado en Plantillas XAML (`ToolPageTemplates.xaml`): integración de controles interactivos para consolidación de memoria, votación senatorial, acciones de guerra/paz, diagnóstico en flight deck genérico y pericias de capitán con `Mode=OneWay` estricto en `<Run>`.
- [x] Fase 6: Pruebas unitarias de simulación en `DesktopSimulationServiceTests.cs` y verificación exhaustiva de 6 capas (compilación Release, stateless 4/4, Core 73, Desktop 63, Render 289, Agentes 31 y UI Automation).
- [x] Fase 7: Empaquetado automático con `FastPackageEngine` (`package.ps1`), registro de Propuesta 29 en `learning_proposal.md`, sincronización y push a GitHub (`main`) bajo la Regla E.




## Gran Ronda de Gráficas Vectoriales Interactivas y Telemetría Multidimensional en WPF (Rev073)
- [x] Fase 1: Suite de Controles Vectoriales Directos (`ForgeChartControls.cs`): Implementación nativa de `ForgeSparkline` (línea y área con gradiente suave), `ForgeRadarChart` (poligonal n-dimensional con cuadrícula concéntrica regular y soporte multicapa) y `ForgeRingGauge` (reloj de anillo angular de 270° con umbral dinámico de alerta y tipografía centrada), todos derivados de `FrameworkElement` con `DrawingContext.DrawGeometry()` y geometrías congeladas (`Freeze()`) para 0 sobrecoste en el árbol visual.
- [x] Fase 2: Modelos de Vista y Telemetría Multidimensional (`DesktopSimulationViewModels.cs`):
  - Árbol de Tropas (`TroopTreeDashboardViewModel`): 5 ejes ortogonales normalizados `CombatRadarAxes` [Armor, Damage, Speed, Cohesion, CostEff], `SelectedTroopRadarValues` reactivo y superposición comparativa `CaptainPerksRadarValues`.
  - Simulador de Empresas (`WorkshopDashboardViewModel`): Trayectoria acumulada de beneficios a 30 días `CumulativeProfitTrajectory` y medidor radial de riesgo de rebelión cívica `RebellionRiskGaugeValue` con umbral de 45%.
  - Diplomacia Geopolítica (`KingdomDiplomacyDashboardViewModel`): Medidores radiales duales `SenateConsensusGaugeValue` y `WarRiskGaugeValue`, y radar geopolítico pentagonal `DiplomaticRadarAxes` y `FactionPowerRadarValues`.
  - Memoria Cognitiva CoALA (`AgentMemoryDashboardViewModel`): Curva analítica horaria de decaimiento de utilidad $U(t) = 0.4R(t) + 0.3F + 0.3I$ a 24 intervalos `MemoryDecayTrajectory` y medidor radial de ocupación de slots `MemorySlotUtilizationGaugeValue`.
  - Flight Deck de Operaciones Genéricas (`GenericOperationDashboardViewModel`): Histograma de latencia de despacho IPC `ExecutionLatencyTrajectory` a 16 muestras y medidor radial de búfer de 64 MB `BufferUtilizationGaugeValue`.
- [x] Fase 3: Integración y Cableado XAML en Estudios Tácticos (`ToolPageTemplates.xaml`):
  - Inserción de `ForgeRadarChart` pentagonal en columna táctica de `TroopTreeVisualizerDashboardTemplate`.
  - Inserción de `ForgeSparkline` de rentabilidad acumulada y `ForgeRingGauge` de riesgo cívico en `WorkshopSimulatorDashboardTemplate`.
  - Inserción de `ForgeRingGauge` de slots y `ForgeSparkline` de decaimiento cognitivo en `AgentMemoryInspectorDashboardTemplate`.
  - Inserción de `ForgeRadarChart` de poder geopolítico y medidores duales `ForgeRingGauge` en `KingdomDiplomacyStudioDashboardTemplate`.
  - Inserción de `ForgeRingGauge` de búfer y `ForgeSparkline` de latencia IPC en `GenericOperationOverviewDashboardTemplate`.
  - Preservación inviolable de los 37 tokens de localización verificados por `VisualizerStaticLabelLocalization` y purga de duplicados de cierre XAML.
- [x] Fase 4: Pruebas Unitarias de Telemetría Gráfica en `DesktopSimulationServiceTests.cs`: Comprobación exhaustiva de cotas [0.0, 1.0] y [0, 100], colecciones no nulas y mutaciones reactivas ante selección de tropas, pericias de capitán, costes económicos, decretos de guerra y ciclos de escenario.
- [x] Fase 5: Verificación Exhaustiva de 6 Capas:
  - Compilación de la solución `CalradiaForge.sln` en Release (0 errores / 0 advertencias).
  - Verificación de comportamiento sin estado `verify_stateless_behavior.ps1` (4/4 criterios cumplidos).
  - Suite de pruebas de la solución (`Run-CalradiaForge-Tests.bat`): Core & ForgeWeave 73 pasadas, Desktop MVVM 63 pasadas, WPF Render 289 pasadas en < 13.5s.
  - Suite de agentes autónomos Antigravity (`test_forge_agents.py`): 31 pruebas pasadas en 25.1s.
  - Verificación Windows UI Automation smoke (`Test-CalradiaForge-Desktop-Uia.ps1`): 23/23 registros conformes.
- [x] Fase 6: Empaquetado Automático con `FastPackageEngine` (`package.ps1`): Generación de los 3 archivos ZIP de distribución en `artifacts/` (`CalradiaForge-Modules-25.2.0.zip`, `CalradiaForge-Source-SDK-25.2.0.zip`, `CalradiaForge-Desktop-25.2.0.zip`).
- [x] Fase 7: Registro de Aprendizaje (Propuesta 30 en `learning_proposal.md`), Sincronización y Push a GitHub (`main`) bajo la Regla E.

## Integración de 8 Skills Élite para Agentes y Explorador de Resultados Gauntlet (Rev081)
- [x] Fase 1: Investigación y Selección de Skills Élite: Análisis de repositorios abiertos para desarrollo en Bannerlord y Antigravity, seleccionando 8 skills en 4 pilares:
  1. Rendimiento & Depuración: `debugging-master`, `performance-hunter`
  2. IA de Juego & Simulación: `game-ai-behavior-trees`, `discrete-event-simulation`
  3. Multi-Agente & Testing: `multi-agent-orchestration`, `test-architect`
  4. UI/UX de Juego & Audio: `game-ui-design`, `game-audio`
- [x] Fase 2: Especialización Completa de las 8 Skills en `.agents/skills/`:
  - `debugging-master/SKILL.md`: Triaje de fallos nativos (0xC0000005), regla de los 10 minutos y análisis de volcados `.cfcrash`.
  - `performance-hunter/SKILL.md`: Zero GC allocs en ticks de simulación, time-slicing modulo-24 y enumeradores estructurados.
  - `game-ai-behavior-trees/SKILL.md`: Diseño de nodos de árboles de comportamiento, patrón Blackboard y ciclo de vida de `AgentComponent` / `MissionLogic`.
  - `discrete-event-simulation/SKILL.md`: Cadencias horarias/diarias/semanales, afinidad de hilo de juego y máquinas de estados deterministas.
  - `multi-agent-orchestration/SKILL.md`: Coordinación de Google Antigravity SDK y OpenAI Codex CLI con compactación de tokens sin pérdida de telemetría de fallos.
  - `test-architect/SKILL.md`: Pirámide de verificación de 5 fases, contratos estáticos de C#/XAML, pruebas de renderizado WPF headless y smoke UIA.
  - `game-ui-design/SKILL.md`: Prefabs Gauntlet XML con `[DataSourceProperty]`, paleta táctica WPF y sondeo de flanco ascendente en F10.
  - `game-audio/SKILL.md`: Directorio `ModuleSounds/`, esquema `module_sounds.xml`, categorías de mezcla `ui`/`mission_combat` y reproducción 2D/3D.
- [x] Fase 3: Gobernanza y Taxonomía:
  - Actualización de la tabla de enrutamiento y categorías en `docs/SKILLS_TAXONOMY.md`.
  - Registro de la Propuesta 35 en `learning_proposal.md`.
- [x] Fase 4: Implementación del Explorador de Resultados Gauntlet (Rev081):
  - Modelos `TestResultItemVM` y analizador acotado `TestResultExplorerParser` (51 registros) en `PanelViewModel.cs`.
  - Prefab Gauntlet `CalradiaForge.xml` con `ListPanel` desplazable, `ItemTemplate` y panel de detalle contextual.
  - Sincronización de cadenas en 13 idiomas e inclusión de oclusión en `Validate-CalradiaForge-DecorativeSprites.py`.
- [x] Fase 5: Verificación Integral de 6 Capas:
  - Compilación Release de `CalradiaForge.sln`: 0 advertencias, 0 errores.
  - Verificación stateless (`verify_stateless_behavior.ps1`): 4/4 criterios superados.
  - Suite de pruebas de la solución (`Run-CalradiaForge-Tests.bat`): 343 Core, 73 ForgeWeave, 63 Desktop, 289 WPF Render (182 pasadas, 193.796 nodos) superados al 100%.
  - Suite de agentes autónomos (`test_forge_agents.py`): 31/31 pruebas superadas al 100% con 58.6% de reducción en compactación de tokens.
  - Validación de sprites decorativos (`Validate-CalradiaForge-DecorativeSprites.py`): 100% aprobado.
- [x] Fase 6: Empaquetado Automático con `FastPackageEngine` (`package.ps1`): 3 archivos ZIP generados en `artifacts/` (`CalradiaForge-Modules-25.2.0.zip`, `CalradiaForge-Source-SDK-25.2.0.zip`, `CalradiaForge-Desktop-25.2.0.zip`).
- [x] Fase 7: Sincronización Git y Push a GitHub (`main`): Commit `9e8f0be` sincronizado exitosamente con el repositorio remoto.

## Suite de Generación de Imágenes de Alta Calidad y Pipeline de Activos de Juego (Rev082)
- [x] Fase 1: Creación de la Skill Especializada `.agents/skills/high-quality-image-generation/SKILL.md`:
  - Frontmatter con nombre, descripción detallada y disparadores contextuales.
  - Arquitectura de Prompts de 9 Dimensiones (sujeto histórico, materiales, iluminación, ópticas 85mm, atmósfera medieval).
  - Dialectos específicos por modelo: Flux.1 Dev/Pro, Midjourney v6.1, Gemini (`generate_image`), DALL-E 3.
  - Catálogo de arquetipos: Iconos de armas/objetos 512x512, heráldica de reinos, texturas de pergamino UI y banners promocionales 1920x1080 / 4K.
- [x] Fase 2: Desarrollo de la Utilidad de Procesamiento `tools/process_high_quality_asset.py`:
  - CLI Python con Pillow para extracción de canal alfa, recorte inteligente y transparencia.
  - Escalado estricto Power-of-Two (POT: 256, 512, 1024, 2048, 4096) para compatibilidad con TaleWorlds TPAC y GPU mipmaps.
  - Dilatación de bordes (color bleed/padding) para evitar bleeding en Gauntlet UI.
  - Normalización de espacio de color sRGB 8-bit y exportación de informe JSON de metadatos.
- [x] Fase 3: Pruebas Unitarias Automatizadas en `tests/test_high_quality_image_pipeline.py`:
  - Verificación de procesamiento de canal alfa, escalado POT, padding de bordes y metadatos JSON.
- [x] Fase 4: Gobernanza, Taxonomía y Aprendizaje:
  - Actualización de `docs/SKILLS_TAXONOMY.md` con la nueva skill en el clúster de UI & Activos Creativos.
  - Registro de la Propuesta 36 en `learning_proposal.md`.
- [x] Fase 5: Verificación Integral del Repositorio:
  - Compilación Release de `CalradiaForge.sln`: 0 advertencias, 0 errores.
  - Verificación stateless (`verify_stateless_behavior.ps1`): 4/4 criterios superados.
  - Suite de pruebas de la solución (`Run-CalradiaForge-Tests.bat`): 343 Core, 73 ForgeWeave, 63 Desktop, 289 WPF Render superados al 100%.
  - Suite de agentes autónomos (`test_forge_agents.py`): 31/31 pruebas superadas al 100%.
  - Ejecución de pruebas unitarias del pipeline de imágenes (`test_high_quality_image_pipeline.py`): 5/5 pruebas superadas al 100%.
- [x] Fase 6: Empaquetado Automático con `FastPackageEngine` (`package.ps1`): 3 archivos ZIP generados en `artifacts/` (`CalradiaForge-Modules-25.2.0.zip`, `CalradiaForge-Source-SDK-25.2.0.zip`, `CalradiaForge-Desktop-25.2.0.zip`).
- [x] Fase 7: Sincronización y Push a GitHub (`main`) bajo la Regla E.

## Gran Ronda de Mejoras Gráficas y Visuales a la App Desktop WPF (Rev083)
- [x] Fase 1: Generación y Procesamiento de Activos Tácticos de Alta Fidelidad con `high-quality-image-generation`
  - [x] Generar imagen de escudo heráldico medieval con `generate_image` (Arquetipo 2).
  - [x] Generar textura de pergamino y mapa cartográfico con `generate_image` (Arquetipo 3).
  - [x] Procesar activos con `tools/process_high_quality_asset.py` (POT 512/1024, alfa, sangrado de color, metadatos JSON).
  - [x] Integrar texturas en `src/CalradiaForge.Desktop/Resources/Textures/` y pinceles en `TacticalPalette.xaml` / `TacticalPalette.Parchment.xaml`.
- [x] Fase 2: Expansión del Motor Vectorial Directo en `ForgeChartControls.cs`
  - [x] Implementar `ForgeBarChart` con soporte de barras horizontales/verticales, etiquetas, valores, tooltips y geometrías congeladas.
  - [x] Implementar `ForgeHeatmapGrid` con interpolación de color cálido táctico y tooltips dinámicos.
- [x] Fase 3: Enriquecimiento de Modelos de Vista en `DesktopSimulationViewModels.cs`
  - [x] Exponer datos de barras para combate (`WeaponDamageBreakdownBars`), comercio (`CommodityProfitMarginBars`), tropas (`TroopStatDistributionBars`), talleres (`WorkshopEconomicBreakdownBars`), diplomacia (`KingdomPowerComparisonBars`) y flight deck (`OperationThroughputBars`).
  - [x] Cablear comandos reactivos de refresco.
- [x] Fase 4: Integración y Cableado XAML en `ToolPageTemplates.xaml`
  - [x] Insertar nuevos gráficos vectoriales en paneles manteniendo exactamente 9 `*DashboardTemplate`.
  - [x] Garantizar disposición fluida a 980x680 DIP sin recortes y `Mode=OneWay` en elementos `<Run>`.
- [x] Fase 5: Sincronización de Localización Multilingüe
  - [x] Verificar conformidad con `VisualizerStaticLabelLocalization` y paridad de 13 idiomas en diccionarios de recursos.
- [x] Fase 6: Pruebas y Verificación Integral de 6 Capas
  - [x] Añadir pruebas en `DesktopSimulationServiceTests.cs` para bar charts y heatmaps.
  - [x] Validar compilación Release (0 errores), stateless (4/4), Core & ForgeWeave (73), Desktop MVVM (63), Render (292), Agentes (31) y Pipeline de Imágenes (5).
  - [x] Ejecutar smoke test Windows UI Automation (23/23 registros observados).
- [x] Fase 7: Registro de Aprendizaje, Empaquetado y Sincronización Git
  - [x] Redactar Propuesta 37 en `learning_proposal.md` (`/learn`).
  - [x] Empaquetar distribución con `tools/package.ps1` (3 archivos ZIP generados en `artifacts/`).
  - [x] Sincronizar y push a GitHub (`main`) bajo la Regla E.

## Gran Ronda de Mejoras Gráficas, Visuales y Telemetría Vectorial WPF (Rev084)
- [x] Fase 1: Generación y Procesamiento de Activos Tácticos HQ (`high-quality-image-generation`)
  - [x] Generar activos tácticos medievales con `generate_image` (placa dial metálica y cinta de rango imperial).
  - [x] Procesar activos con `tools/process_high_quality_asset.py` (POT 512, alfa, dilatación de bordes 2px, sRGB, metadatos JSON).
  - [x] Integrar texturas en `Resources/Textures/` y pinceles congelados en temas tácticos.
- [x] Fase 2: Expansión de Controles de Telemetría Vectorial Directa (`ForgeChartControls.cs`)
  - [x] Implementar `ForgeRadarChart` con N ejes poligonales, anillos concéntricos, polígono translúcido, marcadores y geometrías congeladas (.Freeze()).
  - [x] Implementar `ForgeArcGauge` con arco de fondo, arco de progreso redondeado, texto formateado y unidades con coste cero en reposo.
- [x] Fase 3: Enriquecimiento de Modelos de Vista en `DesktopSimulationViewModels.cs`
  - [x] Exponer propiedades de radar para tropas (`TroopAttributesRadar`) y combate (`CombatTacticalRadar`).
  - [x] Exponer medidores de arco para diplomacia, comercio, talleres, tropas y saturación del flight deck.
- [x] Fase 4: Micro-Interacciones y Estilos de Feedback Táctico
  - [x] Estilo interactivo de tarjetas con resplandor en hover (`ForgeInteractiveCardStyle`) y chips de filtro (`ForgeFilterChipStyle`).
  - [x] Animación de pulso cíclico suave en `SessionConnectionIndicator` (Connected/Connecting).
  - [x] Garantizar presentación estricta en una sola línea horizontal en `WorkbenchHeaderControl.xaml` a 980 DIP.
- [x] Fase 5: Integración XAML en `ToolPageTemplates.xaml`
  - [x] Integrar `ForgeRadarChart` y `ForgeArcGauge` en los paneles manteniendo exactamente el contrato de 9 `*DashboardTemplate`.
  - [x] Asegurar resolución mínima 980x680 DIP y enlaces `Mode=OneWay` en elementos `<Run>`.
- [x] Fase 6: Pruebas y Verificación Integral de 6 Capas
  - [x] Actualizar pruebas de renderizado en `DesktopSimulationServiceTests.cs`.
  - [x] Validar compilación Release (0 errores), stateless (4/4), Core & ForgeWeave, Desktop MVVM (63), Render (WPF 292), Agentes (31) y Pipeline de Imágenes (5).
  - [x] Ejecutar smoke test Windows UI Automation (23/23 registros observados).
- [x] Fase 7: Registro de Aprendizaje, Empaquetado y Sincronización Git
  - [x] Redactar Propuesta 38 en `learning_proposal.md` (`/learn`).
  - [x] Empaquetar distribución con `tools/package.ps1` (3 archivos ZIP generados en `artifacts/`).
  - [x] Sincronizar y push a GitHub (`main`) bajo la Regla E.

## Gran Ronda de Mejoras Gráficas, Visuales y Telemetría Vectorial WPF (Rev085)
- [x] Fase 1: Generación y Procesamiento de Activos Tácticos HQ (`high-quality-image-generation`)
  - [x] Generar sello de cera heráldico imperial con script determinista procedural (`high-quality-image-generation` sin llamar `generate_image`).
  - [x] Procesar activo con `tools/process_high_quality_asset.py` (POT 512, canal alfa limpio, dilatación de 2px, sRGB).
  - [x] Integrar textura en `src/CalradiaForge.Desktop/Resources/Textures/` y registrar en `tools/prepare_desktop_textures.py` en `PRESERVED_UNPACKAGED_VARIANTS`.
- [x] Fase 2: Expansión de Controles de Telemetría Vectorial Directa (`ForgeChartControls.cs`)
  - [x] Implementar `ForgeAreaChart` con degradado vertical translúcido, línea de cresta, cuadrícula militar de ejes y geometrías congeladas (.Freeze()).
  - [x] Implementar `ForgeStepProgress` con nodos secuenciales, conectores vectoriales y badges de estado (`DONE`, `ACTIVE`, `PENDING`).
- [x] Fase 3: Enriquecimiento de Modelos de Vista en `DesktopSimulationViewModels.cs`
  - [x] Exponer propiedades de pipeline y trayectorias de área en `GenericOperationDashboardViewModel`, `GauntletStudioDashboardViewModel`, `CampaignStudioDashboardViewModel`, `DeliveryStudioDashboardViewModel` y `DiagnosticsStudioDashboardViewModel`.
- [x] Fase 4: Personalización Diferenciada en XAML (`ToolPageTemplates.xaml` & `WorkbenchWorkspaceControl.xaml`)
  - [x] Integrar `ForgeStepProgress` y `ForgeAreaChart` en las vistas genéricas (`GenericOperation`, `GauntletStudio`, `CampaignStudio`, `DeliveryStudio`, `DiagnosticsStudio`).
  - [x] Integrar sello imperial en `ContextDossierPanel` como distintivo de seguridad certificada.
  - [x] Preservar exactamente el límite de 9 plantillas `*DashboardTemplate` y las dimensiones mínimas 980x680 DIP.
- [x] Fase 5: Pruebas y Verificación Integral de 6 Capas
  - [x] Actualizar pruebas de renderizado en `DesktopSimulationServiceTests.cs`.
  - [x] Validar compilación Release (0 errores), stateless (4/4), Core & ForgeWeave, Desktop MVVM (63), Render (WPF 292), Agentes (31) y Pipeline de Imágenes (5).
  - [x] Ejecutar smoke test Windows UI Automation (23/23 registros observados).
- [x] Fase 6: Registro de Aprendizaje, Empaquetado y Sincronización Git
  - [x] Redactar Propuesta 39 en `learning_proposal.md` (`/learn`).
  - [x] Empaquetar distribución con `tools/package.ps1` (3 archivos ZIP generados en `artifacts/`).
  - [ ] Sincronizar y push a GitHub (`main`) bajo la Regla E.


