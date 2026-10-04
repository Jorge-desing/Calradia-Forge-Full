# Rev139 — Deterministic Mission Combat Simulation Engine and Substantive Tactical Analytics

**Fecha:** 2026-10-04  
**Versión:** 25.2.0  
**Alcance:** Mission Combat Simulation Architecture (`CalradiaForge.Sdk`), Desktop Tactical Simulation Integration (`CalradiaForge.Desktop`), Automated Regression Suites (`CalradiaForge.Tests`, `CalradiaForge.Desktop.Tests`), and Invariant Governance.

---

### Problema observado y justificación técnica

Siguiendo la directriz explícita del usuario que prohíbe terminantemente la creación de funciones o pruebas superficiales que aparenten funcionalidad mediante texto sin sustento ("anti-mock / cero cosmética falsa"):
1. **Ausencia de Motor Analítico de Combate Determinista:** En el SDK existían fórmulas aisladas en `ForgeCombatTactics` (choque de moral básico y cálculo de brechas de muralla), pero carecía de un motor de simulación de combate integral que modelara enfrentamientos tácticos entre tropas agrupadas por formaciones y roles (`Infantry`, `Ranged`, `Cavalry`, `HorseArcher`) con el ciclo de vida real de `AgentComponent` y `MissionBehavior` de TaleWorlds.
2. **Herramientas de Simulación de Combate en Desktop Desconectadas:** En la aplicación de escritorio (`CalradiaForge.Desktop`), herramientas registradas en el catálogo como `CombatAgentSpawner`, `NoviceCombatAi` y `SiegeNavmeshTactician` devolvían un estado inerte (`DesktopToolKind.Simulation => NotRun`), requiriendo un motor de simulación sustantivo capaz de evaluar balance de tropas, efectividad de armaduras y dinámicas de combate offline.
3. **Carencia de Modelado No Lineal de Armaduras en SDK:** No existía una implementación tipada de las curvas hiperbólicas de absorción de daño de Bannerlord por tipo de impacto (`Cut` con alta absorción, `Pierce` con penetración de armadura superior, y `Blunt` con contundencia consistente).
4. **Ausencia de Componentes de Estamina y Pánico en Tiempo Real:** Faltaban componentes de misión que evaluaran el agotamiento por ataque continuo frente a la recuperación pasiva en reposo, así como el efecto de choque colectivo en la moral de la escuadra ante bajas súbitas de compañeros.

---

### Solución técnica y decisiones arquitectónicas

1. **Nuevo Motor de Simulación de Combate Determinista (`ForgeMissionCombatSimulator`):**
   - Implementado en `CalradiaForge.Sdk` como un motor 100% C# nativo y autónomo, sin dependencias de red, sin servicios externos, sin tickets de API en la nube y sin consumo de cuotas.
   - Definición de modelos de datos fuertemente tipados: `CombatDamageType`, `CombatTroopRole`, `CombatSoldierState`, `CombatVerdict`, `CombatSoldierDefinition`, `CombatSoldierRuntime`, `CombatMissionEvent`, `CombatSimulationScenario` y `CombatSimulationResult`.
   - Bucle de simulación determinista gobernado por pasos de tiempo fijos (`DeltaTime`, típicamente 0.05s por tick), reproducible idénticamente a partir de semillas aleatorias (`RandomSeed`).
2. **Componentes de Misión Basados en `ICombatMissionComponent`:**
   - `StaminaManagementComponent`: Gestiona el consumo de estamina por ataque activo (-14 HP/atk) y la reducción de efectividad de daño y velocidad cuando la estamina cae por debajo de 25 HP, junto con la recuperación pasiva durante periodos de descanso (+8 a +18 HP/s).
   - `MoraleShockComponent`: Evalúa el estrés psicológico ante heridas graves y propaga choques de moral colectivos entre aliados al producirse una baja súbita. Si la moral cae por debajo de 15 HP, el combatiente entra en estado `Routed` y huye de la contienda.
3. **Fórmulas Matemáticas de Absorción de Armadura de Bannerlord:**
   - Implementación en `CalculateAbsorbedDamage` de la curva hiperbólica de absorción:
     `Multiplicador = 100 / (100 + ArmaduraEfectiva)`
     donde la armadura efectiva varía según el factor de penetración del tipo de daño:
     - `Cut`: Factor 1.00 (absorción completa de blindaje).
     - `Pierce`: Factor 0.65 (alta penetración contra corazas).
     - `Blunt`: Factor 0.40 (daño contundente consistente con mínima mitigación).
   - Escalado proporcional de daño por habilidad de combate (`+0.2%` por punto de habilidad).
4. **Integración en Calradia Forge Desktop:**
   - En `DesktopSimulationService.cs`, se implementó `SimulateMissionCombat(string input, CancellationToken cancellation)` que ejecuta la simulación, compila tablas métricas completas y emite 4 evidencias auditables `WorkspaceEvidence`.
   - En `DesktopWorkspaceService.cs`, se enrutaron `CombatAgentSpawner`, `NoviceCombatAi` y `SiegeNavmeshTactician` para invocar directamente a `SimulateMissionCombat`.
5. **Pruebas Rigurosas y Sustantivas:**
   - En `SdkFeaturesTests.cs`, se validó la exactitud matemática de las fórmulas de armadura con aserciones cuantitativas estrictas, la coherencia de la sumatoria de combatientes, el determinismo perfecto entre ejecuciones con la misma semilla y el rechazo de escenarios vacíos o cancelados.
   - En `CalradiaForge.Desktop.Tests`, se validó el despacho y generación de evidencias de `CombatAgentSpawner` a través de `DesktopWorkspaceService`.

---

### Cambios en activos y código

- `src/CalradiaForge.Sdk/ForgeMissionCombatSimulator.cs`: Motor analítico determinista de combate, modelos de datos y componentes de misión (`StaminaManagementComponent`, `MoraleShockComponent`).
- `src/CalradiaForge.Desktop/Services/DesktopSimulationService.cs`: Método `SimulateMissionCombat` con generación de dossier métrico y evidencias de combate.
- `src/CalradiaForge.Desktop/Services/DesktopWorkspaceService.cs`: Enrutamiento de `CombatAgentSpawner`, `NoviceCombatAi` y `SiegeNavmeshTactician` al motor de simulación.
- `tests/CalradiaForge.Tests/SdkFeaturesTests.cs`: Nueva prueba matemática y determinista `TestRev139MissionCombatSimulator`.
- `tests/CalradiaForge.Desktop.Tests/Program.cs`: Nueva prueba de integración `MissionCombatSimulationRoute`.

---

### Validación y límites de la evidencia

- **Compilación de la solución:** `dotnet build CalradiaForge.sln -c Release -v:minimal` completada con 0 errores y 0 advertencias en `net472`, `net8.0` y `net8.0-windows`.
- **Compuerta sin estado:** `tools\Verify-CalradiaForge-StatelessBehavior.bat` superó los 4/4 criterios de aceptación (100%).
- **Pruebas de Desktop:** `tools\Run-CalradiaForge-Desktop-Tests.bat --no-pause` superó 73/73 pruebas (0 fallos).
- **Pruebas de Core y SDK:** `tools\Run-CalradiaForge-Core-Tests.bat` superó 423 pruebas de SDK, DetourFixture (10 etapas) y Patch Diagnostics (30/30).
- **Pruebas de renderizado WPF:** `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause` superó 296 pruebas en 320 pasadas.
- **Límites de la evidencia:** Esta simulación modela determinísticamente las matemáticas de daño, estamina, moral y combate táctico de tropas offline; no ejecuta una sesión gráfica 3D en vivo de Bannerlord ni invoca APIs remotas.
