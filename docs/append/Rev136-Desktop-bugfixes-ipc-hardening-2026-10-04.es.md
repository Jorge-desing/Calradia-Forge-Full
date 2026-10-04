# Rev136 — Robustecimiento del canal IPC Desktop, resiliencia en diff semántico XML y tolerancia a fallos en preferencias

**Fecha:** 2026-10-04

**Versión:** Calradia Forge 25.2.0, sin cambios; `ForgeApi.Version` 13

**Alcance:** `src/CalradiaForge.Desktop/PipeClient.cs`, `src/CalradiaForge.Desktop/Services/DesktopSessionService.cs`, `src/CalradiaForge.Desktop/Services/DesktopWorkspaceService.cs`, `src/CalradiaForge.Desktop/Services/DesktopSimulationService.cs`, `src/CalradiaForge.Desktop/Services/DesktopLocalizationService.cs`, `src/CalradiaForge.Desktop/Services/DesktopThemeService.cs`, `src/CalradiaForge.Desktop/Services/DesktopPreferenceService.cs`, `src/CalradiaForge.Desktop/Services/DesktopAssemblyService.cs`, `tests/CalradiaForge.Desktop.Tests/Program.cs`, manejo de excepciones y robustez de IPC en Desktop WPF, tolerancia a identificadores duplicados en diff semántico XML, saneamiento del estado de preferencias, seguridad en búsqueda de paletas de temas y registro inmutable de mejoras.

## Problema observado y justificación técnica

Tras las sucesivas rondas de artesanía visual y composición táctica en el banco de trabajo Desktop, una auditoría exhaustiva del manejo de errores en tiempo de ejecución y los servicios de frontera identificó ocho modos de falla en casos límite y vulnerabilidades de excepción:
1. En `PipeClient.cs:SendCore`, si una conexión por canalización con nombre devolvía una cadena vacía, carga corrupta o el literal `null`, `Json.Deserialize<Response>(line)` generaba `null`, provocando una excepción no controlada `NullReferenceException` al desreferenciar `response.Id`. De manera similar, en `ConnectCore`, un `response.Data` nulo o vacío causaba que `Json.Deserialize<string[]>` arrojara `ArgumentNullException`.
2. En `DesktopSessionService.cs:SendAsync`, las solicitudes nulas no se validaban antes de la invocación, y las excepciones de red o tiempo de espera lanzadas por `pipe.Send` no restablecían `LastRoundTripLatencyMs`, dejando métricas de latencia residuales de operaciones exitosas previas.
3. En `DesktopWorkspaceService.cs:QueryLiveAsync`, las consultas en vivo asumían respuestas de sesión no nulas, desreferenciando directamente `response.Success` y careciendo de un resultado estructurado de respaldo ante cargas nulas.
4. En `DesktopSimulationService.cs:PerformTroopXmlDiff`, el visor de diferencias semánticas XML empleaba `.ToDictionary(e => e.Attribute("id").Value, ...)` sobre los documentos XML base y objetivo. En los XML de datos de juego de Bannerlord que contienen identificadores duplicados (p. ej. variantes repetidas de tropas, ramas de mejora anidadas o ítems multiforma), `.ToDictionary` arrojaba `ArgumentException: An item with the same key has already been added`, interrumpiendo la ejecución del comparador.
5. En `DesktopSimulationService.cs:RenderBar`, los cálculos de porcentaje visual carecían de defensas contra `double.IsNaN` y `double.IsInfinity`, valores que pueden resultar de divisiones por cero en escenarios sin calibrar.
6. En `DesktopLocalizationService.cs:Apply`, la instanciación de un `ResourceDictionary` de idioma desde URIs relativas carecía de captura de excepciones, arriesgando caídas de XAML/IO si un diccionario de recursos fallaba al cargarse.
7. En `DesktopThemeService.cs:Apply`, la resolución del índice de inserción de paleta utilizaba `Enumerable.Range(...).First(index => IsPaletteDictionary(...))`, lo que podía arrojar `InvalidOperationException: Sequence contains no matching element` ante mutaciones concurrentes de la colección.
8. En `DesktopPreferenceService.cs:Save`, los estados nulos o en blanco no se saneaban antes de la serialización JSON, arriesgando la escritura de `null` o cargas inválidas en `desktop-preferences.json`.
9. En `DesktopAssemblyService.cs:Inspect`, los ensamblados administrados sin metadatos de versión explícitos podían arrojar `NullReferenceException` al invocar `assembly.Version.ToString()`.

## Solución técnica y decisiones arquitectónicas

1. **Robustecimiento del canal IPC y protección contra cargas nulas (`PipeClient.cs`)**:
   - En `SendCore`, se incorporó una guardia explícita inmediatamente tras la deserialización JSON: `if (response == null) throw new IOException("Received null or malformed response payload from named pipe.");`.
   - En `ConnectCore`, se añadieron comprobaciones de nulidad y espacios en blanco al procesar las capacidades publicitadas desde `response.Data`: `var capabilitiesData = !string.IsNullOrWhiteSpace(response.Data) ? Json.Deserialize<string[]>(response.Data) : null; Capabilities = new HashSet<string>(capabilitiesData ?? [], StringComparer.OrdinalIgnoreCase);`.
2. **Seguridad en servicio de sesión y reinicio de latencia (`DesktopSessionService.cs`)**:
   - Se añadió `if (request == null) throw new ArgumentNullException(nameof(request));` en `SendAsync`.
   - Se envolvió `pipe.Send` en un bloque `try ... catch` que reinicia explícitamente `LastRoundTripLatencyMs = null` antes de relanzar la excepción, asegurando que fallos transitorios o tiempos de espera no filtren métricas obsoletas.
   - Se limpió `LastRoundTripLatencyMs = null` en todos los manejadores de captura de `ConnectAsync` y `ReconnectAsync`.
3. **Resultado estructurado de respaldo en consultas de espacio de trabajo (`DesktopWorkspaceService.cs`)**:
   - En `QueryLiveAsync`, se introdujo una guardia que devuelve un `WorkspaceExecutionResult` de fallo estructurado (`Status = "Failed"`) si `response == null`, evitando desreferencias nulas y preservando intacto el contrato de línea verbatim (`if (response.Success && tool.PipeAction == "framework")`).
4. **Tolerancia a IDs XML duplicados con semántica de última definición gana (`DesktopSimulationService.cs`)**:
   - Se reemplazaron las llamadas frágiles a `.ToDictionary(...)` por bucles seguros con asignación indexada (`elemsA[idAttr.Value] = el;`), respetando la semántica nativa de Bannerlord donde la última definición prevalece, sin arrojar excepciones por claves duplicadas.
   - Se blindó `RenderBar` con `if (double.IsNaN(percentage) || double.IsInfinity(percentage)) percentage = 0.0;`.
5. **Protección de respaldo en localización y temas (`DesktopLocalizationService.cs`, `DesktopThemeService.cs`)**:
   - Se envolvió la carga del `ResourceDictionary` en `DesktopLocalizationService.cs` dentro de un bloque `try ... catch`, degradando automáticamente a `englishFallback` y `"en"` ante cualquier fallo.
   - Se sustituyó `Enumerable.First(...)` en `DesktopThemeService.cs` por un bucle indexado con valor seguro por defecto `Math.Min(1, dictionaries.Count)`.
6. **Saneamiento de estado de preferencias e inspección de ensamblados (`DesktopPreferenceService.cs`, `DesktopAssemblyService.cs`)**:
   - En `DesktopPreferenceService.cs:Save`, se saneó el estado entrante para garantizar `ThemeId` no nulo (por defecto `"war-table"`) y `LanguageCode` no nulo (por defecto `"en"`), preservando estrictamente el token atómico `File.Move(temporary, path, true)`.
   - En `DesktopAssemblyService.cs:Inspect`, se aplicó navegación condicional contra nulos: `assembly.Version?.ToString() ?? "0.0.0.0"`.
7. **Verificación empírica y conjunto de pruebas unitarias (`Program.cs`)**:
   - Se crearon y registraron cuatro nuevos casos de prueba automatizados en `tests/CalradiaForge.Desktop.Tests/Program.cs`:
     * `NullResponsePayload`: Verifica que `PipeClient` rechace respuestas nulas o malformadas en canalizaciones reales arrojando `IOException` controlada.
     * `XmlDiffDuplicateIds`: Verifica que `DesktopSimulationService.SimulateTroopTree` tolere IDs duplicados entre archivos XML de comparación.
     * `PreferenceNullSanitization`: Verifica que `DesktopPreferenceService` sanee estados nulos y cadenas vacías a valores por defecto al guardar.
     * `SessionSendNullAndErrorLatency`: Verifica que `DesktopSessionService.SendAsync` rechace solicitudes nulas con `ArgumentNullException` y no registre latencia espuria.

## Cambios en activos, código y dependencias

- Se modificó `src/CalradiaForge.Desktop/PipeClient.cs`: protección de `SendCore` contra deserialización nula y `ConnectCore` contra datos de capacidades nulos.
- Se modificó `src/CalradiaForge.Desktop/Services/DesktopSessionService.cs`: validación de `request != null` y reinicio de `LastRoundTripLatencyMs` ante errores o desconexión.
- Se modificó `src/CalradiaForge.Desktop/Services/DesktopWorkspaceService.cs`: protección de `QueryLiveAsync` contra respuestas IPC nulas.
- Se modificó `src/CalradiaForge.Desktop/Services/DesktopSimulationService.cs`: sustitución de `.ToDictionary` por asignación indexada en `PerformTroopXmlDiff`, y contención de valores no finitos en `RenderBar`.
- Se modificó `src/CalradiaForge.Desktop/Services/DesktopLocalizationService.cs`: encapsulación de la carga de diccionarios en un bloque `try ... catch` con degradación a inglés.
- Se modificó `src/CalradiaForge.Desktop/Services/DesktopThemeService.cs`: sustitución de la búsqueda con `.First(...)` por un bucle indexado seguro.
- Se modificó `src/CalradiaForge.Desktop/Services/DesktopPreferenceService.cs`: saneamiento de campos antes de serializar.
- Se modificó `src/CalradiaForge.Desktop/Services/DesktopAssemblyService.cs`: operador condicional de nulo en `assembly.Version`.
- Se modificó `tests/CalradiaForge.Desktop.Tests/Program.cs`: adición de 4 nuevos casos de prueba (+102 líneas), ampliando la batería de pruebas de escritorio de 67 a 71 casos.
- Cero cambios incompatibles en contratos públicos del SDK, cero nuevas dependencias externas y todos los tokens de contrato estático preservados al 100%.

## Validación y límites de la evidencia

- `dotnet build CalradiaForge.sln -c Release -v:minimal` compiló con 0 advertencias y 0 errores.
- `tools\Verify-CalradiaForge-StatelessBehavior.bat` superó los 4/4 criterios de aceptación (compilación Release, cero SaveableTypeDefiner / SyncData sin estado en 3 behaviors, anti-shadowing y registro en SubModule.OnGameStart).
- `tools\Run-CalradiaForge-Desktop-Tests.bat --no-pause` superó 71/71 pruebas unitarias, de protocolo, catálogo de herramientas y robustez sin fallos.
- `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause` superó 296/296 casos de renderizado WPF en 19,807 ms (320 pasadas de layout, 8,283.1 ms en llamadas de layout).
- `tools\Run-CalradiaForge-Core-Tests.bat --no-pause` superó 420/420 pruebas unitarias y 30/30 pruebas del fixture de diagnóstico de parches.
- `tools\Run-CalradiaForge-ForgeWeave-Tests.bat --no-pause` superó 73/73 pruebas de eventos y repetición.
- `tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` validó la paridad documental completa (42/42 pares) y verificaciones estáticas de código sin antipatrones.
