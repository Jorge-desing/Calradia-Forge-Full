# SDK v3

For typed, versioned APIs shared between modules, see [Shared libraries](SHARED_LIBRARIES.md). `ForgeApi.Libraries` is separate from the test/command registration interface.

Reference `CalradiaForge.Sdk.dll` and declare `CalradiaForge` as a required module loaded before your extension. Do not ship a second copy of the SDK DLL in your extension folder. The SDK targets .NET Framework 4.7.2 for game modules and .NET 8 for external tests. Forge itself has no required dependency on Harmony, MCM, ButterLib, or another mod framework.

Register extensions from your submodule's load callback with Forge's atomic availability helper:

```csharp
using CalradiaForge.Sdk;

protected override void OnSubModuleLoad() => ForgeApi.RegisterWhenAvailable(Register);
protected override void OnSubModuleUnloaded() => ForgeApi.UnregisterWhenAvailable(Register);
void Register(IForgeRegistry registry) => registry.Register(new MyTest());
```

`RegisterWhenAvailable` checks the current registry and subscribes as one operation, so it cannot miss a connection between a null check and a later event subscription. Keep the matching `UnregisterWhenAvailable` call on unload. The legacy `Available` event remains for source compatibility. Extensions must use globally unique IDs such as `my_module.inventory_check`. Forge invokes every availability subscriber even if another subscriber throws, retains successful registrations, and records the failed callback as a startup warning. An extension must still catch and handle its own recoverable errors.

## Contracts

- `ITestCase`: `Prepare`, `Execute`, `Verify`, `Cleanup`. Cleanup is attempted even after preparation failure or cancellation. Keep cleanup idempotent and tolerate partial preparation.
- `ICommand`: one bounded operation with an argument string.
- `IDiagnosticProvider`: returns `Finding` records; provider exceptions become diagnostic warnings.
- `Descriptor`: `Id`, `Module`, `Name`, `Context` and `ChangesState`. Context is Any, Campaign or Mission. State changes require testing mode, plus campaign-copy confirmation whenever a campaign is active.
- `TestExecution`: services, cancellation token, seeded `Random`, recorded steps and `Verify(condition, description)`.

## ForgeWeave extension framework

`IForgeEventRegistry` is an additive SDK contract for cooperative handlers. Register an `IForgeEventHandler` from a `ForgeApi.RegisterWhenAvailable` callback; Forge assigns `Events` before it invokes that callback, while preserving the existing `IForgeRegistry` contract for tests, commands and diagnostics.

A handler exposes one `ForgeEventSubscription`: a globally unique `Descriptor`, a `ForgeEventKind`, priority, optional same-event/same-priority `Before` and `After` IDs, `ForgeEventAccess`, a bounded exact-match `ForgeEventFilter`, `FailureLimit` and, only for `Pulse`, `MinimumIntervalMilliseconds`. The first valid pulse interval is 250 ms; the maximum is 60,000 ms. Other event kinds must use zero.

`ForgeEventFilter.RequiredData` lets an author subscribe to copied scalar metadata without writing a predicate in every handler. An empty map matches every event. Forge accepts at most eight key/value pairs, caps keys at 64 characters and values at 512, copies the declaration at registration, and compares with ordinal equality. A mismatch produces a `Filtered` handler outcome and never reaches authorization or user code. The same rule applies to live dispatch and Replay Lab.

`BudgetMilliseconds` gives a handler a short, visible execution budget from 0 to 5,000 ms. Zero disables it. With `ForgeBudgetPolicy.Warn`, an overrun is reported as `OverBudget` in the handler outcome and snapshot while the callback is allowed to finish; `Ignore` keeps the measurement out of budget findings. Forge never aborts arbitrary extension code or quarantines a handler solely for being slow.

ForgeWeave receives only callbacks explicitly raised by Forge's Bannerlord adapter: `ForgeReady`, `InitialScreenReady`, context changes, campaign start, mission initialization, agent creation/removal and a bounded `Pulse`. `ContextLeaving` preserves its source context even after the host has moved into the next context. `ForgeEvent` exposes a sequence, timestamp, logical context, cancellation token, duration and a read-only copied `Data` dictionary. It does not expose `ITestServices`, a game-object resolver or live Bannerlord objects.

Higher priorities run first. Before/After constraints refine equal priorities only. Forge blocks missing, cross-event, cross-priority, self-referential and cyclic declarations instead of inferring an order; unrelated handlers continue. Handlers that throw are isolated and quarantined after their bounded failure limit. `CampaignWrite` and `MissionWrite` handlers use the same descriptor context, test-mode and campaign-copy checks as Forge tests. `StopPropagation` only stops later ForgeWeave handlers for the same event; it never cancels a game callback.

### Replay Lab

`IForgeReplayRegistry` is another additive contract exposed as `ForgeApi.Replays`. It enumerates detached `ForgeReplayRecord` copies retained by the host and accepts only their source sequence in `Replay`. It does not accept an event kind, context or payload from an extension.

Set `ForgeEventSubscription.ReplayMode` explicitly when a handler may receive a replay. The default is `Disabled`; `ObserveOnly` is for read-only verification; `Live` permits a guarded live replay. Replays always require an exact current-context match. They do not bypass descriptor access, test mode or copied-campaign checks. During a replay, `ForgeEvent.IsReplay` is `true` and `SourceSequence` names the retained host event. Forge never retains a replay as a new source.

`ForgeReplayResult` and the snapshot contain the status, rejection reason, timing and copied handler outcomes. Use them for controlled event verification. They are not a generic patch, callback injection or method-interception API. See [ForgeWeave](FORGEWEAVE.md) for the registration example, limits and UI behavior.

The `framework` protocol action returns `ForgeWeaveSnapshot`; `event-journal` returns its bounded recent dispatch records; `replay` asks the live game to replay one retained source sequence. See [ForgeWeave](FORGEWEAVE.md) for a complete registration example and limits.

## Patch Blueprint Preflight

`IPatchBlueprintProvider` is an additive, read-only SDK contract for declaring an intended patch target. Register it with `ForgeApi.PatchBlueprints?.Register(provider)` after Forge is connected. It is separate from `IForgeRegistry` so existing SDK v1 test, command and diagnostic consumers remain compatible.

The provider has a `Descriptor` and returns `PatchBlueprint` records from `Describe(PatchBlueprintRequest)`. Set `Descriptor.ChangesState=false`; Forge rejects state-changing blueprint providers. A blueprint includes a stable ID, hook vocabulary (`Prefix`, `Postfix`, `Transpiler`, `Finalizer`), exact `MethodReference` target, required callback reference, declared priority, before/after owner IDs and rationale. `MethodReference.From(MethodBase)` and `TypeReference.From(Type)` capture a signature without any Harmony type. `ForgeApi.Version` is 13; patch and runtime-hook services remain optional host capabilities, so check `ForgeApi.Patches` or `ForgeApi.Hooks` directly before use.

Forge executes the provider only for an explicit preflight on the game thread, copies the returned DTOs, and bounds capture size. Do not apply a patch in `Describe`, retain game objects, or start background work. Forge validates the declared target only against already loaded assemblies, records self-referential ordering declarations for review, and never infers final execution order. See [Patch Blueprint Preflight](PATCH_BLUEPRINTS.md) for a complete example and the resolution rules.

Run game operations on the calling game thread. Do not retain live game objects across sessions or spawn background tasks that access them. Long-running extension code must cooperate with cancellation; Forge is not a sandbox and cannot safely abort arbitrary C# code.

Tests are synchronous and should finish quickly. Transport requests time out after 15 seconds and request cancellation. A timed-out operation may still be completing cleanup; inspect the recorded result before retrying any state-changing action.

The two complete examples are in `examples/CalradiaForge.Examples/Examples.cs`. They obtain `IGameLaboratory` from `ITestServices.GetService`; this game adapter lives in the mod assembly. Their automated tests use a fake laboratory and do not establish native engine correctness.

SDK API 13 exposes optional `ForgeHookDefinition.Finalizer`, mutable invocation `Exception`, and snapshot `HasFinalizer`/`HasTranspiler` flags while preserving earlier constructor signatures. Prefix/Postfix/Finalizer registration uses `IForgeHookService`; local IL transpilers use the Core-only `net472` `ForgeHookService.RegisterTranspiler` adapter. The SDK contains no MonoMod contract types. See [runtime hook policies](PATCH_BLUEPRINTS.md#explicit-runtime-hooks-and-il-transpilers) before registering executable code: Finalizers pass the host callback gate, while an applied IL transformation remains active outside the menu until explicitly reverted.

## Local protocol

Connect to `CalradiaForge-{BannerlordProcessId}` using a duplex Windows named pipe. The ACL permits only the current Windows user. No TCP port or external service is used.

Each UTF-8 line is a JSON `Request`: `Version=1`, `Id`, `Action`, `Argument`, `Seed`. Responses echo version and request ID, with `Success`, `Error` and `Data`. Structured `Data` is itself a JSON string. Start with `hello` to negotiate capabilities. Its string list includes `protocol:1`, suite and target labels, then every supported action. Requests are limited to 64 KiB; desktop responses to 32 MiB.

Actions: `hello`, `summary`, `scan`, `modules`, `dependencies`, `diagnostics`, `logs`, `inspect`, `pin`, `compare`, `snapshots`, `unpin`, `tests`, `commands`, `command`, `test-mode`, `confirm-copy`, `run`, `run-batch`, `metrics`, `framework`, `event-journal`, `replay`, `harmony`, `patch-blueprints`, `patch-preflight`, `hook-snapshots`, `hook-apply-plan`, `hook-apply-confirm`, `hook-revert-plan`, `hook-revert-confirm`, `hook-plan-cancel`, `report`, `export`, `panel-open`, `panel-close`, `language`, `agent-memory`.

`language` is a read-only compatibility query returning the game's current language identifier. It does not change either interface's language. `run-batch` accepts up to 50 comma-separated test IDs, validates the batch before executing, and stops on its first failed test. `snapshots` lists pinned objects; `unpin` takes the same exact `Type|ID` key used by `pin`. `dependencies` returns a suggested order from the latest completed module scan and does not edit the launcher. `panel-open` and `panel-close` control the native panel.

`framework` returns current ForgeWeave handler health, declared order, context/access data, timing, failures, quarantines, event counters, retained replay sources and recent replay outcomes. `event-journal` returns only the recent bounded history. `replay` takes one retained source sequence and runs it on the game thread; it does not retry automatically and preserves every existing writer gate. `framework` and `event-journal` are read-only and do not traverse game objects; `replay` is a guarded verification action whose outcome can be rejected without altering the session.

`harmony` remains a historical bounded, read-only diagnostic endpoint only when a compatible Harmony API is already loaded by the game or another selected module. It is not used by ForgeWeave and is not part of the recommended extension path.

`patch-blueprints` lists registered blueprint-provider descriptors. `patch-preflight` captures their declarations and evaluates exact targets. It does not require or invoke a patch runtime, and it applies no patch.

`hook-snapshots` is read-only. Apply and Revert use separate plan and confirmation actions with a short-lived single-use token; `hook-plan-cancel` carries only the host session and preview token and invalidates the preview only when both match. Apply/Revert operate only on previously registered hook IDs and do not accept arbitrary target methods or callbacks. The Bannerlord host still enforces its game-thread and exact main-menu gate.

`command` arguments use `command.id|argument`. `inspect` uses `Type|filter`; pin and compare require exact IDs. Reconnect explicitly after the game exits or transport fails; failed requests are never automatically replayed.
