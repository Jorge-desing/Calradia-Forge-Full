# Rev137 — Corrección de Errores Multicapa, Resiliencia del Ciclo de Vida y Seguridad en Límites entre Mod, SDK y Core

**Fecha:** 2026-10-04

**Versión:** Calradia Forge 25.2.0, sin cambios; `ForgeApi.Version` 13

**Alcance:** `src/CalradiaForge.Mod/SubModule.cs`, `src/CalradiaForge.Mod/CampaignBehaviors/AgentCognitiveMemoryBehavior.cs`, `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs`, `src/CalradiaForge.Sdk/ForgeAgentMemory.cs`, `src/CalradiaForge.Sdk/ForgeSaveChunker.cs`, `src/CalradiaForge.Sdk/ForgeMissionLifecycleGuard.cs`, `src/CalradiaForge.Sdk/ForgePartySpawner.cs`, `src/CalradiaForge.Core/ForgeWeaveEngine.cs`, `tests/CalradiaForge.Tests/SdkFeaturesTests.cs`, seguridad contra nulos en Gauntlet UI y EventManager en el juego, liberación del manejador UnhandledException en el ciclo de vida, límites en el bucle de desalojo de memoria episódica, prevención de desbordamiento de enteros en el segmentador de guardado, recuperación de inicialización diferida en misiones y registro inmutable de mejoras.

## Problema Observado y Justificación Técnica

Tras la ronda de robustecimiento del canal IPC de escritorio (Rev136), una auditoría integral de resiliencia multicapa a través del runtime del mod (`CalradiaForge.Mod`), los cimientos del SDK (`CalradiaForge.Sdk`) y el núcleo de simulación (`CalradiaForge.Core`) identificó ocho modos de falla en casos límite y vulnerabilidades en condiciones de frontera:
1. En `SubModule.cs:OnSubModuleLoad`, se registraba `AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;` en la carga del módulo, pero `OnSubModuleUnloaded()` nunca desvinculaba dicho manejador. Cuando el mod o el AppDomain se recargaban entre sesiones o durante transiciones del ciclo de vida del motor, el manejador permanecía suscrito, fugando referencias e induciendo invocaciones cruzadas entre ciclos de vida.
2. En `SubModule.cs:OnApplicationTick`, se accedía directamente a `layer.UIContext.EventManager` durante la evaluación del foco de teclado, procesamiento de la tecla Enter, inspección de ID enfocada y navegación. Durante transiciones rápidas de pantalla, descarga de capas o destrucción del contexto Gauntlet, `layer.UIContext` o `layer.UIContext.EventManager` pueden ser nulos, con riesgo de provocar `NullReferenceException` no controlada.
3. En `SubModule.cs:MoveKeyboardFocus`, `RestoreNavigationPaletteFocus` y `FocusFirstWidget`, el acceso a `layer.UIContext.Root` y `layer.UIContext.EventManager` carecía de retornos tempranos defensivos cuando el contexto UIContext se encontraba en estado de desmontaje.
4. En `AgentCognitiveMemoryBehavior.cs` y `ClanCharacterProgressionBehavior.cs`, se invocaba directamente `(int)CampaignTime.Now.ToHours` en `OnHourlyTick()` sin protección contra excepciones. Durante la inicialización de campañas, transiciones de escenas o estados simulados en pruebas, la extracción del tiempo sin capturar podía interrumpir los ticks horarios de la simulación. Adicionalmente, en `AgentCognitiveMemoryBehavior.cs:OnHeroKilled`, `FeudTargetHeroId` se actualizaba con `killerId` sin verificar que `killerId` no fuese nulo ni vacío.
5. En `ForgeAgentMemory.cs:EpisodicMemory.TryAdd`, el bucle de desalojo para episodios tipados (`CountType > MaximumEpisodicEntriesPerType`) invocaba `episodes.RemoveAt(oldestOfType)` sin comprobar que `oldestOfType >= 0` estuviese dentro de los límites de la colección. Si `FindOldestTypeIndex` fallaba o devolvía un índice inválido, `RemoveAt` podía lanzar `ArgumentOutOfRangeException` o desencadenar un bucle infinito. Además, `EpisodicMemory.GetAll` instanciaba una `List<object>` sin capacidad inicial, provocando realocaciones dinámicas en consultas recurrentes.
6. En `ForgeSaveChunker.cs:NeedsChunking`, no se validaban argumentos de `maxChunkSize` no positivos, y `Chunk` calculaba el recuento de fragmentos mediante aritmética de enteros de 32 bits `(data.Length + maxChunkSize - 1) / maxChunkSize`, con riesgo de desbordamiento de enteros si la longitud del contenido se aproximaba a `int.MaxValue`. Del mismo modo, `Reassemble` acumulaba longitudes en un `int` de 32 bits, arriesgando desbordamiento en secuencias extensas.
7. En `ForgeMissionLifecycleGuard.cs:OnTick`, se asignaba `_isInitialized = true;` antes de ejecutar `_deferredInitializer();`. Si el inicializador diferido lanzaba una excepción transitoria, `_isInitialized` quedaba bloqueado en `true`, impidiendo intentos posteriores de reintento y dejando el componente 3D de misión en un estado permanentemente corrupto y no inicializado.
8. En `ForgePartySpawner.cs:ForgePartyBlueprint`, `AddTroop` aceptaba identificadores de tropa en blanco, y `Validate()` no comprobaba que `StartingFood >= 0f`.
9. En `ForgeWeaveEngine.cs:RejectReplay` y `CancelReplay`, la resolución de `Context` desde `services.CurrentContext` cuando `source == null` carecía de salvaguarda ante `services` nulo, arriesgando `NullReferenceException`.

## Solución Técnica y Decisiones Arquitectónicas

1. **Desmontaje Limpio del Ciclo de Vida y Seguridad ante Nulos en EventManager (`SubModule.cs`)**:
   - En `OnSubModuleUnloaded`, se incorporó `AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;` para garantizar la cancelación de la suscripción al descargar el módulo.
   - En `OnApplicationTick`, se aplicó navegación condicional nula en todas las llamadas al manejador de eventos de UIContext: `layer?.UIContext?.EventManager?.FocusedWidget != keyboardControl`, `layer?.UIContext?.EventManager?.ClearFocus()` y `layer?.UIContext?.EventManager?.FocusedWidget?.Id`.
   - En `MoveKeyboardFocus`, `RestoreNavigationPaletteFocus` y `FocusFirstWidget`, se agregaron cláusulas de guarda retornando de inmediato cuando `layer?.UIContext?.Root == null || layer.UIContext.EventManager == null`.
2. **Robustez en Ticks de Simulación de Comportamientos de Campaña (`AgentCognitiveMemoryBehavior.cs`, `ClanCharacterProgressionBehavior.cs`)**:
   - En `OnHourlyTick()`, se envolvió la extracción de `CampaignTime.Now.ToHours` en un bloque `try ... catch` con retorno temprano seguro, protegiendo los ticks de campaña ante fallos transitorios en el reloj del motor.
   - En `AgentCognitiveMemoryBehavior.cs:OnHeroKilled`, se agregó una guarda explícita: `if (!string.IsNullOrEmpty(killerId)) { TryUpdateSemanticFact(leaderId, "FeudTargetHeroId", killerId); }`.
3. **Seguridad en Límites de Memoria Episódica y Optimización de Asignación (`ForgeAgentMemory.cs`)**:
   - En `EpisodicMemory.TryAdd`, se protegió la resolución del índice de desalojo: `if (oldestOfType >= 0 && oldestOfType < episodes.Count) episodes.RemoveAt(oldestOfType); else break;`, erradicando por completo excepciones por índice negativo o bucles infinitos.
   - En `EpisodicMemory.GetAll`, se pre-asignó la capacidad de la lista resultante con `new List<object>(Math.Min(episodes.Count, MaximumEpisodicEntriesPerType))`, eliminando redimensionamientos en el montón.
4. **Protección contra Desbordamiento de Enteros en el Segmentador de Guardado (`ForgeSaveChunker.cs`)**:
   - En `NeedsChunking`, se incorporó la validación `maxChunkSize > 0` para retornar false limpiamente ante tamaños no positivos.
   - En `Chunk`, se promovió el cálculo a aritmética entera de 64 bits: `int chunkCount = (int)(((long)data.Length + maxChunkSize - 1) / maxChunkSize);`.
   - En `Reassemble`, se acumularon las longitudes en `long safeLength`, validando `if (safeLength > int.MaxValue) throw new InvalidOperationException(...)` antes de instanciar `StringBuilder`.
5. **Recuperación de Inicialización Diferida en Misiones (`ForgeMissionLifecycleGuard.cs`)**:
   - Se trasladó `_isInitialized = true;` para ejecutarse estrictamente después de que `_deferredInitializer();` retorne sin lanzar excepciones, garantizando que `IsInitialized` refleje el éxito real de la configuración y permitiendo reintentos en el siguiente tick ante excepciones transitorias.
6. **Validación de Planos en el Generador de Partidas (`ForgePartySpawner.cs`)**:
   - En `AddTroop`, se validaron identificadores con espacios en blanco: `if (string.IsNullOrWhiteSpace(troopCharacterId) || count <= 0) return this;`.
   - En `Validate()`, se agregó la verificación explícita: `if (StartingFood < 0f) errors.Add("Starting food cannot be negative.");`.
7. **Seguridad en Fallback de Contexto en ForgeWeave Engine (`ForgeWeaveEngine.cs`)**:
   - En `RejectReplay` y `CancelReplay`, se blindó la resolución de contexto a `Context = source != null ? source.Context : (services != null ? services.CurrentContext : Context.Any)`.
8. **Batería Automatizada de Verificación Multicapa (`SdkFeaturesTests.cs`)**:
   - Se implementó y registró `TestRev137MultilayerHardeningAndSafety` en `tests/CalradiaForge.Tests/SdkFeaturesTests.cs`, verificando sistemáticamente los límites del segmentador, el reintento del guardia de ciclo de vida, la validación de suministros en partidas y los límites del desalojo episódico.

## Cambios en Activos, Código y Dependencias

- Modificado `src/CalradiaForge.Mod/SubModule.cs`: desvinculación de `UnhandledException` al descargar el módulo, protección contra nulos en `layer?.UIContext?.EventManager` en todo el sondeo de teclado y gestión de foco.
- Modificado `src/CalradiaForge.Mod/CampaignBehaviors/AgentCognitiveMemoryBehavior.cs`: protección en la extracción de `CampaignTime` en `OnHourlyTick`, verificación de `killerId` no vacío para el objetivo de disputa feudal.
- Modificado `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs`: protección en la extracción de `CampaignTime` en `OnHourlyTick`.
- Modificado `src/CalradiaForge.Sdk/ForgeAgentMemory.cs`: delimitación del índice oldestOfType en `EpisodicMemory.TryAdd`, pre-asignación de capacidad en `GetAll`.
- Modificado `src/CalradiaForge.Sdk/ForgeSaveChunker.cs`: validación de `maxChunkSize` positivo, prevención de desbordamiento de 32 bits en `Chunk` y `Reassemble`.
- Modificado `src/CalradiaForge.Sdk/ForgeMissionLifecycleGuard.cs`: asignación de `_isInitialized = true` únicamente tras la finalización exitosa de la inicialización.
- Modificado `src/CalradiaForge.Sdk/ForgePartySpawner.cs`: exclusión de tropas con ID en blanco en `AddTroop`, validación de `StartingFood >= 0f`.
- Modificado `src/CalradiaForge.Core/ForgeWeaveEngine.cs`: blindaje de resolución de contexto ante servicios nulos en `RejectReplay` y `CancelReplay`.
- Modificado `tests/CalradiaForge.Tests/SdkFeaturesTests.cs`: incorporación del caso de prueba exhaustivo `TestRev137MultilayerHardeningAndSafety`.
- Cero cambios incompatibles en los contratos públicos del SDK, cero dependencias externas adicionales y preservación total de los invariantes arquitectónicos.

## Validación y Límites de la Evidencia

- **Compilación Limpia de la Solución**: Verificado `dotnet build CalradiaForge.sln -c Release -v:minimal` compilando con 0 errores y 0 advertencias.
- **Compuerta de Comportamiento Sin Estado**: Verificado `tools\Verify-CalradiaForge-StatelessBehavior.bat` aprobando 4/4 criterios de aceptación (0 SaveableTypeDefiners, SyncData 100% sin estado, anti-sombreo GEMINI, SubModule AddBehavior).
- **Batería de Pruebas Unitarias y Render de Escritorio**: Verificado `tools\Run-CalradiaForge-Desktop-Tests.bat --no-pause` aprobando 71/71 pruebas, y `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause` aprobando 296/296 pruebas de renderizado.
- **Batería de Regresión de Core y SDK**: Verificado `tools\Run-CalradiaForge-Core-Tests.bat <nul` aprobando todas las pruebas de Core, Sdk, Detour y Diagnósticos.
- **Auditorías en Python**: Verificado `tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` confirmando 100% de cumplimiento en olores de código y paridad bilingüe de documentación.
- **Límites de la Evidencia**: Evaluado en entornos de prueba e integración continua sin conexión, empleando simulaciones de las bibliotecas de TaleWorlds donde corresponde; no se ejecutó el binario en vivo de Bannerlord.
