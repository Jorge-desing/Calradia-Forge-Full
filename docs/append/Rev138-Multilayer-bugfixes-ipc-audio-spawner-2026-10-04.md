# Rev138 — Multilayer Bug Fixes, IPC Contract Alignment, Audio Path Hardening, and Party Spawner Safety

**Fecha:** 2026-10-04  
**Versión:** 25.2.0  
**Alcance:** Desktop IPC Transport (`CalradiaForge.Desktop`), Mod Runtime Server (`CalradiaForge.Mod`), Audio & Campaign Variables SDK (`CalradiaForge.Sdk`), Analysis Engine (`CalradiaForge.Core`), Agent Memory (`agents/memory.py`), and Automated Regression Suites (`CalradiaForge.Desktop.Tests`, `CalradiaForge.Tests`).

---

### Problema observado y justificación técnica

Durante la auditoría exhaustiva y ronda de pruebas sobre los subsistemas de integración entre el cliente de escritorio y el runtime en tiempo de ejecución del juego, se detectaron discrepancias de protocolo y vulnerabilidades de frontera:
1. **Desalineación de Acción IPC de Memoria de Agentes:** `DesktopSessionService.QueryAgentMemoryAsync` despachaba la acción `"query-agent-memory"`, mientras que el servidor integrado del mod en `Runtime.cs` únicamente escuchaba `"agent-memory-query"`, provocando que las consultas individuales de memoria de héroes desde el escritorio recibieran errores de acción desconocida.
2. **Acción IPC de Variables de Campaña no Manejada:** `DesktopSessionService.QueryVariableAsync` invocaba `"query-variable"`, pero `Runtime.cs` carecía de un manejador en su bloque de despacho, arrojando excepciones `ArgumentException("Unknown action")`.
3. **Falta de Extracción Segura de Variables:** `CampaignVariableInspector` carecía de un método de consulta individual seguro contra excepciones para variables específicas registradas por mods.
4. **Vulnerabilidad de Salto de Directorio y Caracteres Inválidos en Audio:** `ForgeAudioBuilder` y `ForgeAudioInspector` no validaban la presencia de secuencias de salto de directorio (`..`) en las rutas relativas declaradas en `module_sounds.xml`. Adicionalmente, `Path.GetExtension(path)` en `ForgeAudioInspector` arrojaba excepciones `ArgumentException` no controladas ante rutas que contenían caracteres no permitidos en el sistema de archivos de Windows, abortando la auditoría completa en lugar de aislar el hallazgo.
5. **Auditoría de Audio en Catálogo de Análisis:** `ForgeAnalysisCatalog` no detectaba nombres de sonido duplicados ni rutas vacías dentro de `module_sounds.xml`.
6. **Riesgo de Desbordamiento y Nulidad en Planos de Partidas:** `ForgePartyBlueprint` no protegía la sumatoria de tropas contra desbordamientos de 64 bits en escenarios de generación masiva ni validaba cadenas de espacios en blanco en `HomeSettlementStringId`.
7. **Fuga de Wait-Handles en PipeClient:** La primitiva de sincronización `SemaphoreSlim` en `PipeClient` no se disponía explícitamente al invocar `Dispose()`.
8. **Colapso de Cola en Memoria de Agentes:** Si se inicializaba `CoALAAgentMemory` con `max_episodic_traces <= 0`, la cola FIFO colapsaba en cada paso.

---

### Solución técnica y decisiones arquitectónicas

1. **Alineación Bidireccional de Protocolo IPC:**
   - En `DesktopSessionService.cs`, se actualizó la solicitud a `"agent-memory-query"`.
   - En `Runtime.cs`, se añadió `case "query-agent-memory":` como alias transparente sobre `case "agent-memory-query":`.
   - En `Runtime.cs`, se implementó el bloque `case "query-variable":` que resuelve claves de variables de campaña mediante `CampaignVariableInspector.GetVariable`.
2. **Acceso Seguro a Variables en SDK:**
   - Se añadió `CampaignVariableInspector.GetVariable(string key)` que evalúa el delegado getter de forma segura contra excepciones y retorna `null` ante entradas no válidas.
3. **Saneamiento Defensivo de Audio:**
   - En `ForgeAudioBuilder.AddSound` y `Validate()`, se verifica que las rutas relativas no contengan secuencias de salto de directorio (`..`).
   - En `ForgeAudioInspector.AuditSoundManifest()`, se encapsuló la extracción de extensiones en un bloque `try/catch` y se agregaron validaciones de salto de directorio y de nombres duplicados.
   - En `ForgeAnalysisCatalog.cs`, se añadió seguimiento mediante `HashSet<string>` para reportar sonidos duplicados y se validaron rutas vacías (`audio_path_empty`).
4. **Resiliencia en Planos de Partidas:**
   - `ForgePartyBlueprint.Validate()` acumula tropas en una variable `long` de 64 bits, validando cotas positivas y límites máximos, e inspecciona `HomeSettlementStringId` contra espacios en blanco.
5. **Disposición de Recursos en Transporte:**
   - `PipeClient.Dispose()` ejecuta `gate.Dispose()` junto al cierre del stream y tubería.
   - Se añadieron guardas de argumento nulo en `PipeClient.Send` y `PipeClient.QueryAgentMemory`.
6. **Robustez en CoALA Agent Memory:**
   - `CoALAAgentMemory` acota `max_episodic_traces = max(1, int(max_episodic_traces))` para prevenir colapso de la memoria episódica.

---

### Cambios en activos y código

- `src/CalradiaForge.Desktop/PipeClient.cs`: Guardas de nulidad en `Send`, validación de `agentId` en `QueryAgentMemory`, dispose de `gate`.
- `src/CalradiaForge.Desktop/Services/DesktopSessionService.cs`: Acción `"agent-memory-query"` alineada con el servidor del juego.
- `src/CalradiaForge.Mod/Runtime.cs`: Soporte para `"query-variable"` y alias para `"query-agent-memory"`.
- `src/CalradiaForge.Sdk/CampaignVariableInspector.cs`: Nuevo método seguro `GetVariable(string key)`.
- `src/CalradiaForge.Sdk/ForgeAudioBuilder.cs`: Validación contra secuencias de salto de directorio (`..`).
- `src/CalradiaForge.Sdk/ForgeAudioInspector.cs`: Extracción protegida de extensiones ante caracteres inválidos y reporte de nombres duplicados.
- `src/CalradiaForge.Core/ForgeAnalysisCatalog.cs`: Detección de sonidos duplicados y rutas vacías en `module_sounds`.
- `src/CalradiaForge.Sdk/ForgePartySpawner.cs`: Sumatoria segura de 64 bits y validación de `HomeSettlementStringId`.
- `agents/memory.py`: Guardia de cota inferior en `max_episodic_traces`.
- `tests/CalradiaForge.Desktop.Tests/Program.cs`: Nueva prueba `Desktop PipeClient and SessionService enforce boundary checks and agent memory argument safety`.
- `tests/CalradiaForge.Tests/SdkFeaturesTests.cs`: Nueva prueba `Rev138 Audio, variable inspector, and party spawner hardening`.

---

### Validación y límites de la evidencia

- **Compilación en Release:** `dotnet build CalradiaForge.sln -c Release -v:minimal` completado con 0 errores y 0 advertencias.
- **Compuerta sin estado (Regla B):** `Verify-CalradiaForge-StatelessBehavior.bat` superó 4/4 criterios con 0 tipos serializables en mod.
- **Desktop Tests:** `Run-CalradiaForge-Desktop-Tests.bat` completado con 72 pruebas aprobadas (100%).
- **Master Test Battery:** `Run-CalradiaForge-Tests.bat` completó 25 pruebas de assets, 72 pruebas desktop y 296 pruebas de renderizado (320 pasadas de layout).
- **Core & SDK Test Suite:** `Run-CalradiaForge-Core-Tests.bat` completó 422 pruebas de SDK/Core, 10 etapas de DetourFixture y 30 pruebas de diagnóstico de parches (100%).
- **Límites:** Las pruebas verifican contratos de código fuente, serialización IPC fuera de línea y validadores estáticos; no constituyen una prueba de sesión en vivo dentro del ejecutable de TaleWorlds Bannerlord.
