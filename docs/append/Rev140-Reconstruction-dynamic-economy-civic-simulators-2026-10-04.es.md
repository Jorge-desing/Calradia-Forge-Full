# Rev140 — Simuladores Dinámicos de Economía, Equilibrio Cívico, Submundo y Sucesión Dinástica

**Fecha:** 2026-10-04  
**Versión:** 25.2.0  
**Alcance:** Motores de Simulación Dinámica (`CalradiaForge.Desktop`, `CalradiaForge.Sdk`), Enrutamiento Táctico en Desktop (`DesktopWorkspaceService`), Pruebas de Regresión Matemática (`CalradiaForge.Tests`, `CalradiaForge.Desktop.Tests`) y Gobernanza de Invariantes.

---

### Problema observado y justificación técnica

En estricta observancia de la directriz del usuario ("No realices funciones o pruebas que no se implementan realmente o sin sustento, o que solo muestra texto aparentando funcionalidad" / Directriz "Anti-Mock"):
1. **Salidas Estáticas en Simulaciones Económicas de Desktop:** En `CalradiaForge.Desktop`, el método `DesktopSimulationService.SimulateEconomyAndCampaign` devolvía un informe estático hardcodeado (`CanonicalEconomyReport`) y evidencias estáticas fijas (`CanonicalEconomyEvidence`) simulando un resultado congelado para el asentamiento de Marunath.
2. **Enrutamiento Indiscriminado de Herramientas Tácticas:** En `DesktopWorkspaceService.cs`, cuatro herramientas distintas del catálogo de la aplicación de escritorio (`WorkshopEnterpriseSimulator`, `SettlementCalculator`, `UnderworldCrimeSimulator` y `DynasticSuccessionEvaluator`) estaban agrupadas en una única bifurcación condicional (`if (tool.Id == "WorkshopEnterpriseSimulator" || ...)`), despachando a la misma salida estática en lugar de computar sus dinámicas respectivas.
3. **Desaprovechamiento de los Motores Matemáticos del SDK:** El SDK (`CalradiaForge.Sdk`) ya contaba con implementaciones deterministas y tipadas de equilibrio de mercado y retorno de inversión (`ForgeTradeSimulator`), trayectorias de lealtad y riesgo de rebelión (`ForgeSettlementSystem`) y rendimiento de callejones clandestinos y contrabando (`ForgeUnderworldSystem`), pero la aplicación de escritorio no las consumía dinámicamente en tiempo de ejecución.
4. **Ausencia de Cobertura de Pruebas Cuantitativas en el SDK:** No existían pruebas automatizadas con límites numéricos estrictos que verificaran la elasticidad de precios ante superávit y escasez en `ForgeTradeSimulator`, la activación de rebeliones inminentes ante lealtad crítica en `ForgeSettlementSystem`, ni la amortiguación del delito por presencia de la guardia en `ForgeUnderworldSystem`.

---

### Solución técnica y decisiones arquitectónicas

1. **Reconstrucción Integral de Métodos de Simulación Dinámica en Desktop (`DesktopSimulationService`):**
   - `SimulateWorkshopEconomics(string input, CancellationToken cancellation)`: Parsea parámetros de asentamiento, capital inicial, días de horizonte y factor de demanda. Invoca `ForgeTradeSimulator.SimulateWorkshopRoi` para los 7 tipos estándar de talleres de Calradia (`Silversmith`, `Smithy`, `Brewery`, `Weaver`, `WoodWorkshop`, `Pottery`, `Tannery`), calcula el margen neto diario, el período de amortización exacto y la producción global, generando una gráfica ASCII dinámica de trayectoria de beneficios y 4 evidencias auditables `WorkspaceEvidence`.
   - `SimulateSettlementCivicEquilibrium(string input, CancellationToken cancellation)`: Parsea lealtad, guarnición, milicia, superávit de alimentos, impuestos, corrupción y afinidad cultural. Invoca `ForgeSettlementSystem.CalculateDailyLoyaltyDelta`, `CalculateDailySecurityDelta` y `EvaluateRebellionRisk`. Proyecta la lealtad a 30 días y emite 4 evidencias auditables de estabilidad y riesgo de insurrección.
   - `SimulateUnderworldCrime(string input, CancellationToken cancellation)`: Parsea número de matones, prosperidad del asentamiento, nivel de seguridad y operaciones de contrabando. Invoca `ForgeUnderworldSystem.CalculateAlleyDailyYield`, `CalculateSmugglingMargin` y `CalculateCrimeDecay`, calculando la supresión de la guardia urbana y la deriva neta de notoriedad criminal con 4 evidencias auditables.
   - `SimulateDynasticSuccession(string input, CancellationToken cancellation)`: Modela la sucesión dinástica evaluando legitimidad de linaje (35%), competencia táctica (25%), rasgos de carácter (20%) y madurez vital (20%). Clasifica a los pretendientes, determina al heredero designado y proyecta el índice de estabilidad del clan y riesgo de rivalidad sucesoria.
   - `SimulateEconomyAndCampaign(string input, CancellationToken cancellation)`: Reconstruido dinámicamente como la composición unificada de `SimulateWorkshopEconomics` y `SimulateSettlementCivicEquilibrium`, preservando la compatibilidad retroactiva con 0 datos hardcodeados.
2. **Enrutamiento Dedicado en `DesktopWorkspaceService`:**
   - Desacopladas las cuatro herramientas en ramas condicionales independientes:
     - `WorkshopEnterpriseSimulator` -> `SimulateWorkshopEconomics`.
     - `SettlementCalculator` -> `SimulateSettlementCivicEquilibrium`.
     - `UnderworldCrimeSimulator` -> `SimulateUnderworldCrime`.
     - `DynasticSuccessionEvaluator` -> `SimulateDynasticSuccession`.
3. **Pruebas Automatizadas Rigurosas y Deterministas:**
   - En `tests/CalradiaForge.Tests/SdkFeaturesTests.cs`, se incorporó `TestRev140DynamicEconomicAndCivicSimulations`, validando rigurosamente `ForgeTradeSimulator` (elasticidad de precios, superávit vs déficit crítico, solvencia de talleres), `ForgeSettlementSystem` (derivas de lealtad, penalizaciones por hambruna, umbral de rebelión inminente cuando lealtad <= 25 y milicia > 2x guarnición) y `ForgeUnderworldSystem` (extorsión de callejones, márgenes de contrabando con evasión arancelaria y decaimiento por seguridad).
   - En `tests/CalradiaForge.Desktop.Tests/Program.cs`, se incorporó `DynamicSimulationToolsExecution`, comprobando que las 4 herramientas se ejecutan en estado `Simulated`, responden matemáticamente a variaciones de parámetros (capital, días, lealtad, matones, nombre de clan) y emiten evidencias auditables verificadas.

---

### Cambios en activos y código

- `src/CalradiaForge.Desktop/Services/DesktopSimulationService.cs`: Implementación de `SimulateWorkshopEconomics`, `SimulateSettlementCivicEquilibrium`, `SimulateUnderworldCrime`, `SimulateDynasticSuccession`, utilidades de parseo de parámetros (`ExtractParameter`, `ExtractInt`, `ExtractFloat`, `ExtractBool`) y composición dinámica de `SimulateEconomyAndCampaign`.
- `src/CalradiaForge.Desktop/Services/DesktopWorkspaceService.cs`: Despacho individualizado de `WorkshopEnterpriseSimulator`, `SettlementCalculator`, `UnderworldCrimeSimulator` y `DynasticSuccessionEvaluator`.
- `tests/CalradiaForge.Tests/SdkFeaturesTests.cs`: Registro y desarrollo de la prueba matemática `TestRev140DynamicEconomicAndCivicSimulations`.
- `tests/CalradiaForge.Desktop.Tests/Program.cs`: Registro y desarrollo de la prueba de integración de herramientas `DynamicSimulationToolsExecution`.

---

### Validación y límites de la evidencia

- **Compilación de la solución:** `dotnet build CalradiaForge.sln -c Release -v:minimal` ejecutada con 0 errores y 0 advertencias.
- **Compuerta sin estado:** `tools\Verify-CalradiaForge-StatelessBehavior.bat` superó los 4/4 criterios de aceptación.
- **Pruebas de Desktop:** `tools\Run-CalradiaForge-Desktop-Tests.bat --no-pause` superó 74/74 pruebas (0 fallos).
- **Pruebas de Core y SDK:** `tools\Run-CalradiaForge-Core-Tests.bat` superó 424 pruebas de SDK, DetourFixture (10 etapas) y Patch Diagnostics (30/30).
- **Pruebas de Renderizado:** `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause` superó 296 pruebas de render (320 pases de layout).
- **Límites de la evidencia:** Todas las pruebas se ejecutaron en el entorno headless local offline. No se inició una sesión de juego interactiva con TaleWorlds Bannerlord ni se conectó a servidores de telemetría remotos.
