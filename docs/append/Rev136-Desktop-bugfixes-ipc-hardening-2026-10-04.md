# Rev136 — Desktop IPC Channel Hardening, Semantic XML Diff Resilience, and Preference Fallback Robustness

**Date:** 2026-10-04

**Version:** Calradia Forge 25.2.0, unchanged; `ForgeApi.Version` 13

**Scope:** `src/CalradiaForge.Desktop/PipeClient.cs`, `src/CalradiaForge.Desktop/Services/DesktopSessionService.cs`, `src/CalradiaForge.Desktop/Services/DesktopWorkspaceService.cs`, `src/CalradiaForge.Desktop/Services/DesktopSimulationService.cs`, `src/CalradiaForge.Desktop/Services/DesktopLocalizationService.cs`, `src/CalradiaForge.Desktop/Services/DesktopThemeService.cs`, `src/CalradiaForge.Desktop/Services/DesktopPreferenceService.cs`, `src/CalradiaForge.Desktop/Services/DesktopAssemblyService.cs`, `tests/CalradiaForge.Desktop.Tests/Program.cs`, Desktop WPF error handling and IPC channel robustness, semantic XML diff duplicate ID tolerance, preference state sanitization, theme palette lookup safety, and append-only improvement ledger.

## Observed Problem and Technical Rationale

Following extensive UI craftsmanship passes and tactical visual composition improvements, an end-to-end audit of runtime error handling and boundary services across the Desktop workbench identified eight edge-case failure modes and exception vulnerabilities:
1. In `PipeClient.cs:SendCore`, if a named pipe connection returned an empty string, corrupted payload, or literal `null`, `Json.Deserialize<Response>(line)` produced `null`, resulting in an unhandled `NullReferenceException` when dereferencing `response.Id`. Similarly, in `ConnectCore`, empty or null `response.Data` caused `Json.Deserialize<string[]>` to throw `ArgumentNullException`.
2. In `DesktopSessionService.cs:SendAsync`, null requests were not checked prior to invocation, and network or timeout exceptions thrown by `pipe.Send` failed to reset `LastRoundTripLatencyMs`, leaving stale latency figures from earlier successful operations.
3. In `DesktopWorkspaceService.cs:QueryLiveAsync`, live queries assumed non-null session responses, dereferencing `response.Success` directly and lacking a structured fallback result when transport yielded null.
4. In `DesktopSimulationService.cs:PerformTroopXmlDiff`, the semantic XML diff viewer utilized `.ToDictionary(e => e.Attribute("id").Value, ...)` against base and target XML documents. In Bannerlord game data XMLs containing duplicate identifiers (e.g. repeated troop variants, nested upgrade branches, or multiform items), `.ToDictionary` threw `ArgumentException: An item with the same key has already been added`, causing the diff viewer to abort execution.
5. In `DesktopSimulationService.cs:RenderBar`, visual percentage calculations lacked defense against `double.IsNaN` and `double.IsInfinity`, which can result from division by zero in uncalibrated scenarios.
6. In `DesktopLocalizationService.cs:Apply`, instantiating a custom language `ResourceDictionary` from relative pack URIs lacked exception handling, risking unhandled XAML/IO exceptions if a language dictionary failed to load.
7. In `DesktopThemeService.cs:Apply`, resolving the palette insertion slot used `Enumerable.Range(...).First(index => IsPaletteDictionary(...))`, which could throw `InvalidOperationException: Sequence contains no matching element` if collections mutated concurrently.
8. In `DesktopPreferenceService.cs:Save`, null or blank state objects were not sanitized prior to JSON serialization, risking writing `null` or invalid payloads to `desktop-preferences.json`.
9. In `DesktopAssemblyService.cs:Inspect`, unversioned managed assemblies lacking explicit version metadata could throw `NullReferenceException` when invoking `assembly.Version.ToString()`.

## Technical Solution and Architectural Decisions

1. **IPC Channel Hardening & Null Payload Protection (`PipeClient.cs`)**:
   - In `SendCore`, inserted an explicit null check immediately following JSON deserialization: `if (response == null) throw new IOException("Received null or malformed response payload from named pipe.");`.
   - In `ConnectCore`, added null and whitespace guards when parsing advertised capabilities from `response.Data`: `var capabilitiesData = !string.IsNullOrWhiteSpace(response.Data) ? Json.Deserialize<string[]>(response.Data) : null; Capabilities = new HashSet<string>(capabilitiesData ?? [], StringComparer.OrdinalIgnoreCase);`.
2. **Session Service Safety & Latency Reset (`DesktopSessionService.cs`)**:
   - Added `if (request == null) throw new ArgumentNullException(nameof(request));` in `SendAsync`.
   - Wrapped `pipe.Send` inside a `try ... catch` block that explicitly resets `LastRoundTripLatencyMs = null` before rethrowing, ensuring transient failures or timeouts do not leak stale latency metrics.
   - Cleared `LastRoundTripLatencyMs = null` across all catch handlers in `ConnectAsync` and `ReconnectAsync`.
3. **Workspace Query Fallback Result (`DesktopWorkspaceService.cs`)**:
   - In `QueryLiveAsync`, introduced an explicit guard returning a structured failure `WorkspaceExecutionResult` (`Status = "Failed"`) if `response == null`, preventing null dereference while preserving verbatim line contracts (`if (response.Success && tool.PipeAction == "framework")`).
4. **Duplicate XML ID Tolerance with Last-Loaded-Wins Semantics (`DesktopSimulationService.cs`)**:
   - Replaced fragile `.ToDictionary(...)` calls with robust dictionary loops utilizing indexer assignment (`elemsA[idAttr.Value] = el;`), honoring Bannerlord's native last-loaded-wins semantics without throwing duplicate key exceptions.
   - Hardened `RenderBar` with `if (double.IsNaN(percentage) || double.IsInfinity(percentage)) percentage = 0.0;`.
5. **Localization & Theme Fallback Protection (`DesktopLocalizationService.cs`, `DesktopThemeService.cs`)**:
   - Enclosed `ResourceDictionary` creation in `DesktopLocalizationService.cs` within a `try ... catch` block, automatically falling back to `englishFallback` and `"en"` upon error.
   - Replaced `Enumerable.First(...)` in `DesktopThemeService.cs` with an indexed loop defaulting safely to `Math.Min(1, dictionaries.Count)`.
6. **Preference State Sanitization & Assembly Inspection (`DesktopPreferenceService.cs`, `DesktopAssemblyService.cs`)**:
   - In `DesktopPreferenceService.cs:Save`, sanitized incoming state to guarantee non-null `ThemeId` (defaulting to `"war-table"`) and non-null `LanguageCode` (defaulting to `"en"`), while strictly preserving the atomic replacement token `File.Move(temporary, path, true)`.
   - In `DesktopAssemblyService.cs:Inspect`, applied null-conditional navigation: `assembly.Version?.ToString() ?? "0.0.0.0"`.
7. **Empirical Verification & Unit Test Suite (`Program.cs`)**:
   - Authored and registered four new automated test cases in `tests/CalradiaForge.Desktop.Tests/Program.cs`:
     * `NullResponsePayload`: Verifies `PipeClient` rejects null or malformed response payloads on real named pipes, throwing controlled `IOException`.
     * `XmlDiffDuplicateIds`: Verifies `DesktopSimulationService.SimulateTroopTree` tolerates duplicate entity IDs across comparison XML files.
     * `PreferenceNullSanitization`: Verifies `DesktopPreferenceService` sanitizes null states and empty strings to default values upon save.
     * `SessionSendNullAndErrorLatency`: Verifies `DesktopSessionService.SendAsync` rejects null requests with `ArgumentNullException` and leaves latency unrecorded.

## Asset, Code, and Dependency Changes

- Modified `src/CalradiaForge.Desktop/PipeClient.cs`: guarded `SendCore` against null deserialization and `ConnectCore` against null capabilities data.
- Modified `src/CalradiaForge.Desktop/Services/DesktopSessionService.cs`: validated `request != null` and cleared `LastRoundTripLatencyMs` on error and disconnect.
- Modified `src/CalradiaForge.Desktop/Services/DesktopWorkspaceService.cs`: guarded `QueryLiveAsync` against null IPC responses.
- Modified `src/CalradiaForge.Desktop/Services/DesktopSimulationService.cs`: replaced `.ToDictionary` with indexer assignment in `PerformTroopXmlDiff`, and clamped non-finite values in `RenderBar`.
- Modified `src/CalradiaForge.Desktop/Services/DesktopLocalizationService.cs`: wrapped dictionary loading in a fallback `try ... catch` block.
- Modified `src/CalradiaForge.Desktop/Services/DesktopThemeService.cs`: replaced `.First(...)` index lookup with a safe loop.
- Modified `src/CalradiaForge.Desktop/Services/DesktopPreferenceService.cs`: sanitized state fields before serializing.
- Modified `src/CalradiaForge.Desktop/Services/DesktopAssemblyService.cs`: applied null-conditional operator on `assembly.Version`.
- Modified `tests/CalradiaForge.Desktop.Tests/Program.cs`: added 4 new unit test cases (+102 lines), expanding the desktop test battery from 67 to 71 tests.
- Zero breaking changes to public SDK contracts, zero added external dependencies, and all static contract tokens preserved.

## Validation and Evidence Boundaries

- `dotnet build CalradiaForge.sln -c Release -v:minimal` compiled with 0 warnings and 0 errors.
- `tools\Verify-CalradiaForge-StatelessBehavior.bat` passed 4/4 acceptance criteria (Release build, zero SaveableTypeDefiner / stateless SyncData across 3 behaviors, anti-shadowing verification, and SubModule.OnGameStart registration).
- `tools\Run-CalradiaForge-Desktop-Tests.bat --no-pause` passed 71/71 protocol, tool catalog, MVVM, and robustness tests with zero failures.
- `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause` passed 296/296 WPF render cases in 19,807 ms (320 layout passes, 8,283.1 ms in layout calls).
- `tools\Run-CalradiaForge-Core-Tests.bat --no-pause` passed 420/420 unit tests and 30/30 patch diagnostic fixture tests.
- `tools\Run-CalradiaForge-ForgeWeave-Tests.bat --no-pause` passed 73/73 event and replay tests.
- `tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` passed all documentation parity audits (42/42 pairs) and static code smell checks.
