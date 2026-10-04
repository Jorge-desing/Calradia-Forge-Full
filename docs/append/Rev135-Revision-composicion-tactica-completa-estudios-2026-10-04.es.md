# Rev135 — Revisión exhaustiva de composición táctica en estudios de escritorio

**Fecha:** 2026-10-04

**Versión:** Calradia Forge 25.2.0, sin cambios; `ForgeApi.Version` 13

**Alcance:** `src/CalradiaForge.Desktop/Resources/Views/ToolPageTemplates.xaml`, `tests/CalradiaForge.Desktop.RenderTests/DesktopSimulationServiceTests.cs`, presentación WPF de escritorio, 12 plantillas de estudios tácticos, medallones duales temáticos, tarjetas KPI con bordes de acento de 3px, mini-barras de progreso proporcionales de 4px, bloques de terminal monoespaciados tipo dossier, invariante de Regla C (exactamente 9 DashboardTemplates), invariante de Propuesta 48 (cero enlaces TwoWay en Run.Text), pruebas estructurales de renderizado Rev101 y libro mayor de mejoras de solo anexión.

## Problema observado y justificación técnica

Tras la elevación visual del Hook Workbench y las vistas de presentación en la Rev134, las 12 plantillas de estudios tácticos especializados en `src/CalradiaForge.Desktop/Resources/Views/ToolPageTemplates.xaml` exhibían una jerarquía visual dispar y carecían de una composición temática integral:
1. Los encabezados de los estudios mostraban medallones individuales sin sellos o insignias secundarias complementarias, reduciendo la profundidad visual y la resonancia iconográfica entre las diferentes disciplinas tácticas.
2. Los indicadores clave de telemetría y simulación carecían de tarjetas KPI estandarizadas de alto contraste con bordes de acento táctiles (`BorderThickness="1,1,1,3"`), restando prominencia a las métricas críticas frente a las superficies oscuras de fondo.
3. Los indicadores numéricos y proporciones carecían de mini-barras de progreso de 4px (`ProgressBar Height="4"`), impidiendo una evaluación visual inmediata del estado de simulación, niveles de confianza y puntuaciones de seguridad.
4. Las rutinas de comandos de consola se presentaban en texto plano o carecían de terminales tipo dossier con prompt `$ `, tipografía `Consolas` y botones táctiles de copiado rápido.
5. Para preservar la estabilidad de la plataforma y evitar regresiones durante esta profunda renovación visual, era imperativo respetar estrictamente los invariantes arquitectónicos: la Regla C (mantener exactamente 9 instancias de `DashboardTemplate` en `ToolPageTemplates.xaml`), la Propuesta 48 (prohibición estricta de enlaces `TwoWay` en elementos `Run.Text`) y la paridad lingüística integral en los 13 diccionarios de localización sin cadenas fijas en inglés.

## Solución técnica y decisiones arquitectónicas

1. **Composición de medallón dual temático en los 12 estudios**:
   - Se integraron pares de medallones primarios y secundarios de alta resolución POT en modo de escalado de alta calidad en los encabezados de los 12 estudios tácticos en `ToolPageTemplates.xaml`:
     * `WorkshopSimulator`: Medallón primario `calradia-guild-medallion-rev092.png` emparejado con el emblema secundario `calradia-tactical-emblem-rev100.png`.
     * `KingdomDiplomacy`: Medallón primario `calradia-diplomacy-medallion-rev095.png` emparejado con el sello secundario `calradia-aquila-seal-rev087.png`.
     * `CaravanTrade`: Medallón primario `calradia-trade-sigil-rev096.png` emparejado con el dial secundario `calradia-astrolabe-dial-rev086.png`.
     * `GauntletStudio`: Medallón primario `calradia-gauntlet-sigil-rev098.png` emparejado con el sigilo secundario `calradia-anvil-weave-sigil-rev098.png`.
     * `CampaignStudio`: Medallón primario `calradia-campaign-astrolabe-rev098.png` emparejado con el dial secundario `calradia-astrolabe-dial-rev086.png`.
     * `DeliveryStudio`: Medallón primario `calradia-delivery-seal-rev098.png` emparejado con el sello secundario `imperial-wax-seal-rev085.png`.
     * `DiagnosticsStudio`: Medallón primario `calradia-diagnostics-aegis-rev098.png` emparejado con el ojo secundario `calradia-sentinel-eye-rev096.png`.
     * `LiveSession`: Medallón primario `calradia-pipe-seal-rev096.png` emparejado con la matriz secundaria `calradia-bytecode-matrix-rev100.png`.
     * `AgentMemoryInspector`: Medallón primario `calradia-mind-medallion-rev093.png` emparejado con el emblema secundario `calradia-tactical-emblem-rev100.png`.
     * `CodeSecurityAuditor`: Medallón primario `calradia-cipher-seal-rev095.png` emparejado con la matriz secundaria `calradia-bytecode-matrix-rev100.png`.
     * `ModuleHierarchyValidator`: Medallón primario `calradia-hierarchy-seal-rev095.png` emparejado con el sello secundario `calradia-aquila-seal-rev087.png`.
     * `ComponentGeneratorStudio`: Medallón primario `calradia-mechanism-medallion-rev095.png` emparejado con el sigilo secundario `calradia-anvil-weave-sigil-rev098.png`.
2. **Tarjetas KPI tácticas con bordes de acento de 3px y barras de progreso de 4px**:
   - Se estructuraron las tarjetas de métricas clave con bordes de acento inferiores asimétricos (`BorderThickness="1,1,1,3"`), fondos en `{DynamicResource CoalBrush}` y `{DynamicResource FrameSurfaceSolidBrush}`, y tipografía de alto contraste en `{DynamicResource BrassBrush}` y `{DynamicResource VerdigrisBrush}`.
   - Se incrustaron mini-barras de progreso de 4px (`ProgressBar Height="4"`) con enlace `Mode=OneWay` a métricas numéricas del ViewModel (como `SecurityScore`, `ConfidenceLevel`, `HierarchyDepth`, `ExecutionDurationMs`, `SuccessRate` e `IntegrityRating`).
   - Para `AgentMemoryInspector`, se aprovechó el valor predeterminado de WPF `RangeBase.Maximum=100.0` sin declarar el atributo explícito `Maximum="100"`, respetando rigurosamente la aserción de prueba preexistente en `DesktopSimulationServiceTests.cs:231` (`!memoryTemplate.Contains("Maximum=\"100\"")`).
3. **Bloques de terminal monoespaciados tipo dossier**:
   - Se integraron contenedores tácticos de consola que muestran el prompt `<Run Text="$ " Foreground="{DynamicResource MutedBrush}"/>` seguido de la sintaxis de comando enlazada a `{Binding CuratedConsoleCommands, Mode=OneWay}` con `FontFamily="Consolas"`.
   - Se equipó cada bloque de consola con un botón táctil de copiado enlazado a `{Binding DataContext.CopyTextCommand, RelativeSource={RelativeSource AncestorType=Grid}}` y tooltip `{DynamicResource Ui.CopyCommandToolTip}`.
4. **Cumplimiento de invariantes arquitectónicos**:
   - **Regla C**: Se preservaron exactamente 9 instancias de `DashboardTemplate` en `ToolPageTemplates.xaml` (las 3 vistas restantes conservan su denominación canónica `ViewTemplate`).
   - **Propuesta 48**: Se aplicó estrictamente `Mode=OneWay` en todos los enlaces dinámicos de `Run.Text` en el archivo, garantizando cero enlaces `TwoWay`.
   - **Paridad lingüística**: Se localizaron todas las etiquetas, tooltips y descripciones mediante claves existentes en los 13 diccionarios XML sin introducir cadenas en inglés fijas.
5. **Cobertura de pruebas estructurales y empíricas**:
   - Se implementó y registró `Rev101TacticalStudiosFullCompositionOverhaul()` en `tests/CalradiaForge.Desktop.RenderTests/DesktopSimulationServiceTests.cs`, verificando medallones duales, bordes de acento de 3px, barras de 4px, consolas Consolas con `$ `, Regla C, Propuesta 48 y registros en csproj en los 12 estudios.

## Cambios en activos, código y dependencias

- Se utilizaron las 19 texturas maestras PNG en `src/CalradiaForge.Desktop/Resources/Textures/`: `calradia-guild-medallion-rev092.png`, `calradia-tactical-emblem-rev100.png`, `calradia-diplomacy-medallion-rev095.png`, `calradia-aquila-seal-rev087.png`, `calradia-trade-sigil-rev096.png`, `calradia-astrolabe-dial-rev086.png`, `calradia-gauntlet-sigil-rev098.png`, `calradia-anvil-weave-sigil-rev098.png`, `calradia-campaign-astrolabe-rev098.png`, `calradia-delivery-seal-rev098.png`, `imperial-wax-seal-rev085.png`, `calradia-diagnostics-aegis-rev098.png`, `calradia-sentinel-eye-rev096.png`, `calradia-pipe-seal-rev096.png`, `calradia-bytecode-matrix-rev100.png`, `calradia-mind-medallion-rev093.png`, `calradia-cipher-seal-rev095.png`, `calradia-hierarchy-seal-rev095.png` y `calradia-mechanism-medallion-rev095.png`.
- Se modificaron las definiciones de plantillas en `src/CalradiaForge.Desktop/Resources/Views/ToolPageTemplates.xaml` (485 inserciones, 5 eliminaciones; +480 líneas netas).
- Se modificaron las aserciones de prueba en `tests/CalradiaForge.Desktop.RenderTests/DesktopSimulationServiceTests.cs` (78 inserciones añadiendo `Rev101TacticalStudiosFullCompositionOverhaul()`).
- Ningún contrato público del SDK, protocolo IPC o dependencia externa fue alterado.

## Validación y límites de la evidencia

- `dotnet build CalradiaForge.sln -c Release -v:minimal` finalizó con 0 advertencias y 0 errores.
- `tools\Verify-CalradiaForge-StatelessBehavior.bat` superó 4/4 criterios de aceptación (compilación en Release, cero SaveableTypeDefiner / SyncData sin estado en 3 comportamientos, verificación anti-shadowing y registro en SubModule.OnGameStart).
- `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause` superó 296/296 casos de renderizado WPF en 19.627 ms (320 pasadas de diseño, 8.171,9 ms en llamadas de diseño; incluyendo la salida explícita de aprobación de Rev101).
- `tools\Run-CalradiaForge-Desktop-Tests.bat --no-pause` superó 67/67 pruebas unitarias de protocolo y MVVM sin fallos.
- `tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` verificó 135/135 revisiones en la cadena de hash SHA-256, 42/42 pares de documentación bilingüe en `docs/` y 0 olores de código.
- Límites de la evidencia: La validación se realizó mediante arneses de prueba STA sin interfaz y entornos de prueba unitaria sin conexión; no se ejecutó el juego Bannerlord en vivo ni sesiones de combate en tiempo real.
