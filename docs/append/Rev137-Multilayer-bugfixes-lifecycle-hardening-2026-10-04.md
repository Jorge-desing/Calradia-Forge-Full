# Rev137 — Multilayer Bug Fixes, Lifecycle Resilience, and Boundary Safety Across Mod, SDK, and Core

**Date:** 2026-10-04

**Version:** Calradia Forge 25.2.0, unchanged; `ForgeApi.Version` 13

**Scope:** `src/CalradiaForge.Mod/SubModule.cs`, `src/CalradiaForge.Mod/CampaignBehaviors/AgentCognitiveMemoryBehavior.cs`, `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs`, `src/CalradiaForge.Sdk/ForgeAgentMemory.cs`, `src/CalradiaForge.Sdk/ForgeSaveChunker.cs`, `src/CalradiaForge.Sdk/ForgeMissionLifecycleGuard.cs`, `src/CalradiaForge.Sdk/ForgePartySpawner.cs`, `src/CalradiaForge.Core/ForgeWeaveEngine.cs`, `tests/CalradiaForge.Tests/SdkFeaturesTests.cs`, in-game Gauntlet input and event manager null safety, lifecycle unhandled exception teardown, episodic memory eviction bounds, save chunker integer overflow protection, mission lifecycle deferred initialization recovery, and append-only improvement ledger.

## Observed Problem and Technical Rationale

Following the Desktop IPC channel hardening pass (Rev136), a comprehensive multilayer resilience audit across the Mod Runtime (`CalradiaForge.Mod`), SDK Foundation (`CalradiaForge.Sdk`), and Simulation Core (`CalradiaForge.Core`) identified eight edge-case failure modes and boundary vulnerabilities:
1. In `SubModule.cs:OnSubModuleLoad`, `AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;` was registered on module load, but `OnSubModuleUnloaded()` never detached the handler. When the mod or AppDomain reloads across play sessions or during in-engine lifecycle transitions, the unhandled exception handler remained registered, leaking references and causing cross-lifecycle invocations.
2. In `SubModule.cs:OnApplicationTick`, `layer.UIContext.EventManager` was accessed directly during keyboard focus evaluation, enter key handling, focused ID inspection, and keyboard navigation. During fast screen transitions, layer unloading, or Gauntlet context destruction, `layer.UIContext` or `layer.UIContext.EventManager` can be null, risking unhandled `NullReferenceException`.
3. In `SubModule.cs:MoveKeyboardFocus`, `RestoreNavigationPaletteFocus`, and `FocusFirstWidget`, access to `layer.UIContext.Root` and `layer.UIContext.EventManager` lacked defensive early returns when the UIContext was in a teardown state.
4. In `AgentCognitiveMemoryBehavior.cs` and `ClanCharacterProgressionBehavior.cs`, `(int)CampaignTime.Now.ToHours` was invoked directly in `OnHourlyTick()` without exception protection. During campaign initialization, scene transitions, or uncalibrated mock states, unhandled time extraction could interrupt hourly simulation ticks. Additionally, in `AgentCognitiveMemoryBehavior.cs:OnHeroKilled`, `FeudTargetHeroId` was updated with `killerId` without validating that `killerId` was non-empty.
5. In `ForgeAgentMemory.cs:EpisodicMemory.TryAdd`, the eviction loop for typed episodes (`CountType > MaximumEpisodicEntriesPerType`) invoked `episodes.RemoveAt(oldestOfType)` without checking that `oldestOfType >= 0` and within collection bounds. If `FindOldestTypeIndex` failed or returned an invalid index, `RemoveAt` could throw `ArgumentOutOfRangeException` or trigger an infinite loop. Furthermore, `EpisodicMemory.GetAll` instantiated an un-sized `List<object>`, causing dynamic memory allocations on repeatedly queried episodic types.
6. In `ForgeSaveChunker.cs:NeedsChunking`, non-positive `maxChunkSize` arguments were not validated, and `Chunk` calculated chunk counts via 32-bit integer arithmetic `(data.Length + maxChunkSize - 1) / maxChunkSize`, risking integer overflow if payload lengths approached `int.MaxValue`. Similarly, `Reassemble` accumulated lengths in a 32-bit `int`, risking overflow on large chunk sequences.
7. In `ForgeMissionLifecycleGuard.cs:OnTick`, `_isInitialized = true;` was assigned before executing `_deferredInitializer();`. If the deferred initializer threw a transient exception, `_isInitialized` remained stuck at `true`, preventing subsequent retry attempts and leaving the 3D mission component in a permanently corrupted, uninitialized state.
8. In `ForgePartySpawner.cs:ForgePartyBlueprint`, `AddTroop` accepted whitespace troop IDs, and `Validate()` did not verify that `StartingFood >= 0f`.
9. In `ForgeWeaveEngine.cs:RejectReplay` and `CancelReplay`, resolving `Context` from `services.CurrentContext` when `source == null` lacked a fallback guard against null `services`, risking `NullReferenceException`.

## Technical Solution and Architectural Decisions

1. **SubModule Lifecycle Teardown & EventManager Null Safety (`SubModule.cs`)**:
   - In `OnSubModuleUnloaded`, added `AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;` to guarantee clean unregistration upon module teardown.
   - In `OnApplicationTick`, applied null-conditional navigation across all UIContext event manager calls: `layer?.UIContext?.EventManager?.FocusedWidget != keyboardControl`, `layer?.UIContext?.EventManager?.ClearFocus()`, and `layer?.UIContext?.EventManager?.FocusedWidget?.Id`.
   - In `MoveKeyboardFocus`, `RestoreNavigationPaletteFocus`, and `FocusFirstWidget`, introduced guard clauses returning immediately when `layer?.UIContext?.Root == null || layer.UIContext.EventManager == null`.
2. **Campaign Behavior Simulation Tick Robustness (`AgentCognitiveMemoryBehavior.cs`, `ClanCharacterProgressionBehavior.cs`)**:
   - In `OnHourlyTick()`, wrapped `CampaignTime.Now.ToHours` extraction in a `try ... catch` block with safe early return, protecting campaign ticks against transient engine time faults.
   - In `AgentCognitiveMemoryBehavior.cs:OnHeroKilled`, added an explicit guard: `if (!string.IsNullOrEmpty(killerId)) { TryUpdateSemanticFact(leaderId, "FeudTargetHeroId", killerId); }`.
3. **Episodic Memory Bounds Safety and Allocation Optimization (`ForgeAgentMemory.cs`)**:
   - In `EpisodicMemory.TryAdd`, guarded eviction index resolution: `if (oldestOfType >= 0 && oldestOfType < episodes.Count) episodes.RemoveAt(oldestOfType); else break;`, completely eliminating the possibility of negative-index exceptions or unbounded loops.
   - In `EpisodicMemory.GetAll`, pre-allocated the result list with `new List<object>(Math.Min(episodes.Count, MaximumEpisodicEntriesPerType))`, eliminating repeated heap reallocations.
4. **Save Chunker Integer Overflow Protection (`ForgeSaveChunker.cs`)**:
   - In `NeedsChunking`, added `maxChunkSize > 0` validation to ensure non-positive sizes return false cleanly.
   - In `Chunk`, promoted calculation to 64-bit integer arithmetic: `int chunkCount = (int)(((long)data.Length + maxChunkSize - 1) / maxChunkSize);`.
   - In `Reassemble`, accumulated lengths in `long safeLength`, validating `if (safeLength > int.MaxValue) throw new InvalidOperationException(...)` before instantiating `StringBuilder`.
5. **Mission Lifecycle Deferred Initialization Recovery (`ForgeMissionLifecycleGuard.cs`)**:
   - Moved `_isInitialized = true;` to execute strictly after `_deferredInitializer();` returns without throwing, ensuring `IsInitialized` reflects genuine setup success and allowing subsequent tick retry if transient exceptions occur.
6. **Party Spawner Blueprint Validation (`ForgePartySpawner.cs`)**:
   - In `AddTroop`, guarded against whitespace IDs: `if (string.IsNullOrWhiteSpace(troopCharacterId) || count <= 0) return this;`.
   - In `Validate()`, added an explicit check: `if (StartingFood < 0f) errors.Add("Starting food cannot be negative.");`.
7. **ForgeWeave Engine Context Fallback Safety (`ForgeWeaveEngine.cs`)**:
   - In `RejectReplay` and `CancelReplay`, hardened Context resolution to `Context = source != null ? source.Context : (services != null ? services.CurrentContext : Context.Any)`.
8. **Automated Multilayer Verification Suite (`SdkFeaturesTests.cs`)**:
   - Authored and registered `TestRev137MultilayerHardeningAndSafety` in `tests/CalradiaForge.Tests/SdkFeaturesTests.cs`, systematically verifying chunker boundaries, lifecycle guard retry behavior, party blueprint food validation, and episodic eviction bounds.

## Asset, Code, and Dependency Changes

- Modified `src/CalradiaForge.Mod/SubModule.cs`: detached `UnhandledException` on unload, guarded `layer?.UIContext?.EventManager` across all keyboard polling and focus routines.
- Modified `src/CalradiaForge.Mod/CampaignBehaviors/AgentCognitiveMemoryBehavior.cs`: protected `CampaignTime` extraction in `OnHourlyTick`, guarded non-empty `killerId` for feud target.
- Modified `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs`: protected `CampaignTime` extraction in `OnHourlyTick`.
- Modified `src/CalradiaForge.Sdk/ForgeAgentMemory.cs`: bounded oldestOfType index in `EpisodicMemory.TryAdd`, pre-sized `GetAll` list.
- Modified `src/CalradiaForge.Sdk/ForgeSaveChunker.cs`: guarded positive `maxChunkSize`, prevented 32-bit overflow in `Chunk` and `Reassemble`.
- Modified `src/CalradiaForge.Sdk/ForgeMissionLifecycleGuard.cs`: assigned `_isInitialized = true` only after successful initialization.
- Modified `src/CalradiaForge.Sdk/ForgePartySpawner.cs`: guarded whitespace troop IDs in `AddTroop`, added `StartingFood >= 0f` validation.
- Modified `src/CalradiaForge.Core/ForgeWeaveEngine.cs`: hardened Context resolution against null services in `RejectReplay` and `CancelReplay`.
- Modified `tests/CalradiaForge.Tests/SdkFeaturesTests.cs`: added comprehensive `TestRev137MultilayerHardeningAndSafety` test case.
- Zero breaking changes to public SDK contracts, zero added external dependencies, and all architectural invariants preserved.

## Validation and Evidence Limits

- **Clean Solution Build**: Verified `dotnet build CalradiaForge.sln -c Release -v:minimal` compiling with 0 errors and 0 warnings.
- **Stateless Behavior Gate**: Verified `tools\Verify-CalradiaForge-StatelessBehavior.bat` passing 4/4 acceptance criteria (0 SaveableTypeDefiners, 100% stateless SyncData, GEMINI anti-shadowing, SubModule AddBehavior).
- **Desktop Unit & Render Suite**: Verified `tools\Run-CalradiaForge-Desktop-Tests.bat --no-pause` passing 71/71 tests, and `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause` passing 296/296 render tests.
- **Core & Sdk Regression Suite**: Verified `tools\Run-CalradiaForge-Core-Tests.bat <nul` passing all Core, Sdk, Detour, and Diagnostics fixtures.
- **Python Audits**: Verified `tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` confirming 100% code smell compliance and bilingual documentation parity.
- **Evidence Boundaries**: Tested in offline CI/test harnesses with mocked TaleWorlds libraries where appropriate; no live Bannerlord executable launch was performed.
