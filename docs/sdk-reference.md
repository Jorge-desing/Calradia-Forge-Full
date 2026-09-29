# Calradia Forge SDK Reference — 25.0.0

SDK Contract Version: **11**
Product source version: **25.0.0**  
SDK targets: `net472` and `net8.0`; the in-game module remains `net472`.

> This reference describes the v25.0.0 source tree. Existing distribution ZIPs were not regenerated for this source update; their contents do not establish SDK contract 11 availability.

## 1. Auto registration

Use `ForgeApi.AutoRegister` from the module startup hook to discover supported SDK components:

```csharp
public class MySubModule : MBSubModuleBase
{
    protected override void OnSubModuleLoad()
    {
        ForgeApi.AutoRegister(Assembly.GetExecutingAssembly(), "MyModId");
    }
}
```

Auto-registration discovers supported tests, commands, diagnostic providers and Forge event handlers in the supplied assembly.

`AutoRegisterWithReport(assembly, moduleId)` returns an immutable `ForgeAutoRegisterReport` with the scan state, scanned-type/candidate/registered/failure counts and at most 64 bounded failure details. It continues registering independent types when another type fails; it does not roll back registrations that already succeeded. `HostUnavailable` means no registry was connected. Partial type-load failures are included when reflection can still provide loadable types. The legacy `AutoRegister` remains a `void` wrapper and logs bounded failures through `ForgeApi.Logger`.

## 2. ForgeWeave subscriptions and module lifetime

`ForgeCampaignEvents.SubscribeWeaveWhenAvailable` accepts the same arguments and defaults as `SubscribeWeave`, and returns a `ForgeWeaveRegistration` that owns the availability callback and active handler registration. Keep the registration for the module lifetime and dispose it when the module unloads:

```csharp
private ForgeWeaveRegistration _readyRegistration;

protected override void OnSubModuleLoad()
{
    _readyRegistration = ForgeCampaignEvents.SubscribeWeaveWhenAvailable(
        "my_module.startup_observer",
        ForgeEventKind.ForgeReady,
        OnForgeReady,
        access: ForgeEventAccess.Observe);
}

protected override void OnSubModuleUnloaded()
{
    _readyRegistration?.Dispose();
    _readyRegistration = null;
}

private void OnForgeReady(ForgeEvent @event)
{
    // Handle the event on Forge's normal dispatch path.
}
```

The registration checks for the event host atomically. It registers synchronously when Forge is already available, waits when it is not, removes its handler and returns to `WaitingForHost` when Forge disconnects, then registers again after reconnection. Registration and unregistration run on the host connection thread. A pending registration can be disposed from any thread; once a handler is active, `Dispose()` must run on its registration thread. An off-thread call throws `InvalidOperationException`, leaves the handle registered, and records the error so the owner can retry on the correct thread. If host-provided unregistration itself throws, the registration retains the active registry reference and reports `Failed` with the error; a later host transition or a same-thread `Dispose()` can retry the withdrawal. Reentrant reconnects are generation-checked so an older callback cannot overwrite state established by the newer connection, and cleanup errors from an older generation do not replace the newer state.

`IForgeEventRegistry.Register` does not guarantee that a throwing host made no change. If registration throws, the managed handle enters `Failed` and stops automatically registering on later connections because the handler may already exist. The SDK does not attempt rollback: `Unregister(IForgeEventHandler)` is ID-based and could remove a different handler after a duplicate-ID failure. `Error` explains that host state may be uncertain. Dispose the handle, inspect the host for the subscription ID, resolve any leftover registration, then create a new handle. Disposing a handle with an unknown partial registration preserves the diagnostic; it only withdraws registrations the SDK can identify safely. `SubscribeWeave(...)` is also subject to the host's registration behavior, so inspect the host before retrying after an exception.

During `Dispose()`, reentrant availability callbacks are ignored until cleanup finishes. If cleanup throws, the handle remains retryable; retry disposal on the registration thread. Do not assume a failed withdrawal succeeded; inspect `State` and `Error`.

| `ForgeWeaveRegistrationState` | Meaning |
|---|---|
| `WaitingForHost` | No supported event host is currently connected. |
| `Registered` | The handler is registered with the active Forge event host. |
| `HostUnsupported` | A host is present but does not support this registration path. |
| `Failed` | Registration or cleanup failed; inspect `Error` before retrying or recreating the handle. A registration exception can leave host state uncertain. |
| `Disposed` | The owner disposed the registration; no later connection will register it. |

`ForgeWeaveRegistration.State`, `.Error` and `.Handler` expose the current state, latest failure, and active `IForgeEventHandler` when available. `SubscribeWeave` remains the immediate-registration option and throws `InvalidOperationException` when `ForgeApi.Events` is unavailable or the call is off the host connection thread. Release its returned handler with `UnsubscribeWeave(handler)` or `UnsubscribeWeave(handlerId)` on the host connection thread.

For handlers implemented as `IForgeEventHandler` classes, `ForgeApi.RegisterWhenAvailable` and `ForgeApi.UnregisterWhenAvailable` remain available as an atomic module-availability pair. Managed delivery validates the captured registry generation; unregister skips queued delivery and waits for any callback already in progress. Callbacks run synchronously on the registration caller when a host is already connected, or on the connection caller otherwise, outside the SDK-wide availability lock; self-unregistration is supported. Do not block callbacks waiting for other threads that may register or unregister managed availability callbacks. See [ForgeWeave](FORGEWEAVE.md) for event data, access gates, ordering, replay, and dispatch limits.

Managed availability callbacks execute outside the SDK lock. Delivery checks both subscriber lifetime and connection generation; if a callback reenters connection lifecycle code and supersedes the registry, the rest of that stale availability snapshot is skipped. Unregistering a managed callback before its turn also prevents its delivery.

`SharedServiceMonitor<T>.Changed` keeps a typed handler snapshot that is rebuilt when subscribers change. Notifications iterate that snapshot in subscription order, preserving per-handler exception isolation without rebuilding a delegate invocation list on every service change.

## 3. ForgeAgentMemory — bounded in-memory tiers

`ForgeAgentMemory` is a thread-safe, process-local store for agent facts, experiences and tactics. It does not serialize to Bannerlord saves. `ClearAll()` clears every tier; `ClearAgent(agentId)` clears that ID across all tiers. Each tier also exposes its own `ClearAgent` operation.

All three tiers share a maximum of **2,048 distinct agent IDs**. The count is the union of IDs with data in any tier. A new ID is rejected across all tiers once that limit is reached; existing IDs can still update entries within their tier limits.

`SemanticMemory`, `EpisodicMemory`, and `ProceduralMemory` remain publicly constructible for source compatibility, but their instances share the same process-wide backing store. Constructing another tier object cannot create an uncounted copy of the memory, bypass the global agent quota, or leave data behind after `ForgeAgentMemory.ClearAgent`/`ClearAll`.

| Tier | Exact capacity | At capacity | TTL |
|---|---|---|---|
| Semantic facts | 128 facts per agent | Existing keys can be updated. A new fact is rejected. | Optional per fact; this is the only tier with TTL. |
| Episodic experiences | 512 episodes per agent and 128 per `(agentId, type)` | Oldest episodes for the agent are evicted FIFO to satisfy the total cap; when the incoming type is full, its oldest episode is also evicted as needed. | None. |
| Procedural tasks | 128 tasks per agent | Existing tasks can be updated. A new task is rejected. | None. |

The public limits are exposed as `ForgeAgentMemory.MaximumAgents`, `MaximumSemanticEntriesPerAgent`, `MaximumEpisodicEntriesPerAgent`, `MaximumEpisodicEntriesPerType` and `MaximumProceduralEntriesPerAgent`. Use the `Try*` methods when a capacity rejection is an expected outcome. Semantic `TryUpsert` and procedural `TryAdd` return `false` when a new key or agent ID exceeds a limit; existing keys can still be updated. Episodic `TryAdd` returns `false` only when the global agent limit rejects a new ID. Episode FIFO eviction is an accepted write and does not make `TryAdd` fail. The legacy `Upsert`/`Add` methods keep their signatures and throw `InvalidOperationException` if capacity rejects a write.

```csharp
bool storedFact = ForgeAgentMemory.Semantic.TryUpsert(
    agentId, "faction_loyalty", "empire", TimeSpan.FromHours(24));

bool storedEpisode = ForgeAgentMemory.Episodic.TryAdd(
    agentId, "battle", new { Outcome = "victory" });

bool storedTactic = ForgeAgentMemory.Procedural.TryAdd(
    agentId, "cavalry_charge", new { Preferred = true });

string faction = ForgeAgentMemory.Semantic.Get<string>(agentId, "faction_loyalty");
IReadOnlyList<string> facts = ForgeAgentMemory.Semantic.GetKeys(agentId);
List<object> battles = ForgeAgentMemory.Episodic.GetAll(agentId, "battle");
int episodeCount = ForgeAgentMemory.Episodic.TotalCount(agentId);
```

Semantic TTL begins at `Upsert`/`TryUpsert` time and uses UTC. Expired facts return the default value and are removed when read with `Get<T>` or enumerated with `GetKeys`. Omit the TTL to keep a fact until it is overwritten or cleared. Zero and negative TTL values are already expired. TTL arithmetic treats a non-positive duration as immediately expired without adding it to the current timestamp; an unrepresentably large positive duration saturates at `DateTimeOffset.MaxValue` rather than overflowing. Episodic and procedural data never expire automatically.

`Semantic.GetResult<T>(agentId, key)` and `Procedural.GetResult<T>(agentId, task)` return immutable `ForgeMemoryReadResult<T>` values with `Found`, `Missing`, or `TypeMismatch`; Semantic reads can also return `Expired`. A semantic read still removes expired facts, matching `Get<T>` cleanup behavior. Procedural entries have no TTL and never return `Expired`. Existing `Get<T>` signatures and default-on-missing behavior are unchanged.

## 4. ForgeLocalApi — local REST server

`ForgeLocalApi` exposes a lightweight HTTP server for local tools:

```csharp
var api = new ForgeLocalApi();
api.Start("http://localhost:59999/");
// ...
api.Stop();
api.Dispose(); // or use 'using'
```

| Endpoint | Response |
|---|---|
| `GET /info` | Server version, current `ForgeApi.Version` under `sdk`, and status. |
| `GET /status` | Running status. |
| `GET /agents` | Aggregate agent-memory statistics JSON. It preserves the `agents` array as empty and returns only `statistics.agentCount`, `semanticEntries`, `episodicEntries`, and `proceduralEntries`; agent IDs, keys, and stored payloads are not returned. |

## 5. ForgeCampaignEvents compatibility bridge

The legacy `Subscribe(ForgeEvent, Action<object[]>)` bridge isolates callback exceptions and logs them through `ForgeApi.Logger`. Pair it with `Unsubscribe(eventType, callback)` or `ClearSubscribers()` to release static callbacks. ForgeWeave-specific delegate subscriptions should use `SubscribeWeaveWhenAvailable` when the module must survive host disconnect/reconnect; its returned `ForgeWeaveRegistration` is the lifecycle owner.

## 6. Explicit experimental method replacement

The optional `IForgePatchService` capability is available as `ForgeApi.Patches`. It is deliberately separate from `IForgeRegistry`, so adding patch lifecycle support does not require existing registry implementations to add methods. `ForgeApi.Version` is a compile-time constant; use `ForgeApi.Patches != null` to determine whether the connected host actually supplies this capability. `IForgePatchServiceLifecycle` is a second optional capability for hosts that can reuse the same service instance after reconnect; it is separate so existing patch-service implementations remain source-compatible.

Use an explicit stable patch ID and owner label to install a one-for-one method replacement:

```csharp
var patches = ForgeApi.Patches;
if (patches == null)
    throw new InvalidOperationException("The connected Forge host does not provide patch service support.");

IForgePatchHandle handle = patches.ApplyMethodReplacement(
    "my_module.hero_name",
    "MyModule",
    typeof(Hero).GetMethod("GetName"),
    typeof(MyPatch).GetMethod("PatchedGetName"));

ForgePatchVerification verification = handle.Verify();
ForgePatchRevertResult result = handle.Revert();
// Dispose is an idempotent best-effort revert; keep the handle for lifecycle and diagnostics.
```

The handle exposes an immutable `Snapshot`, `Verify()`, and `Revert()`. `Dispose()` requests best-effort reversion and is safe to call more than once. `IForgePatchService.GetSnapshots(owner)` returns immutable snapshots; `Verify(patchId)`, `Revert(patchId)`, `RevertOwner(owner)` and `RevertAll()` provide explicit status and cleanup operations. Results report `Applied`, `Reverted`, `Conflict` or `Failed`; an owner is a tracking label, not an authorization boundary. When the service disconnects it stops accepting new patches and attempts to revert its patches in reverse application order. Existing handles remain queryable/releasable after disconnection. On reconnect, the built-in service reopens only when every prior record and original byte image is verified as reverted and no target detour remains tracked; conflicts or uncertain records keep it closed.

Reversion is allowed only when the target bytes still match the exact jump bytes installed by Forge. If another component changed the target, Forge records a conflict and leaves the foreign bytes untouched. Page-protection restoration and `FlushInstructionCache` are checked; a failure is reported rather than treated as a successful patch or revert. A write that crosses a system page boundary is rejected before `VirtualProtect`, because this backend restores one original protection value for the written span.

This low-level writer remains experimental. Neither executable-page protection changes nor instruction-cache flushing coordinate other threads that may be executing the target. The current backend does not claim safe concurrent hot patching; use a disposable, isolated fixture and ensure no thread can execute the method while code is changed. Do not use this as a production patch framework. The owner field is descriptive only. Preflight hook declarations such as `Transpiler` and `Finalizer` are not implemented by the method-replacement backend.

`ForgeLivePatcher` and the older detour utilities remain compatibility surfaces. `ApplyDetour(original, replacement)` uses Forge's low-level replacement path; `ApplyPatch`/`RevertPatch` only raise their corresponding request events for a registered consumer. Forge does not provide Harmony or automatically turn `Prefix`, `Postfix`, `Transpiler` or `Finalizer` declarations into runtime hooks. Prefer `ForgeApi.Patches` when explicit ownership, verification and reversion are required.

## 7. ForgeApi contract version

Contract 11 adds the optional patch-service surface. Because the version is a compile-time constant embedded in the consuming assembly, check the optional capability at runtime instead of using a version comparison for this feature:

```csharp
if (ForgeApi.Patches == null)
    throw new InvalidOperationException("The connected host does not provide the patch service.");
```

| Contract | Product source | Relevant addition |
|---|---|---|
| 11 | 25.0.0 | Optional `IForgePatchService` with explicit method-replacement handles, verification and conflict-aware reversion. |
| 10 | 25.0.0 | Explicit safe-save outcomes, bounded auto-registration reports, typed Semantic/Procedural memory reads, and generation-safe availability delivery. |
| 9 | 24.0.0 | Owner-scoped `ForgeModelRegistry` registrations; modifier conditions execute on a registry snapshot outside its lock. |
| 8 | 24.0.0 | Managed ForgeWeave availability registration and bounded `ForgeAgentMemory`; TTL is limited to Semantic memory. |
| 7 | 23.0.0 | Shared-service resolution, transactional publication and monitoring. |

`ForgeData.RemoveForgeData<T>(entity)` also removes the entity's outer SDK data entry when the last typed value is removed. It checks the current dictionary under its per-entity lock before detaching it, so concurrent SDK insertions are not lost.

The `/agents` endpoint is an aggregate-only diagnostic. Counts are computed from the in-memory tiers; it deliberately does not expose agent IDs, semantic keys, episodic types, or arbitrary stored objects.

`ForgeModelRegistry.GetModifiers(category)` returns a cached, immutable snapshot containing only the requested category. Registrations, replacements, and removals invalidate the cache; snapshots already returned to callers remain stable. `Evaluate` reuses the category snapshot and invokes modifier conditions outside the registry lock. These are internal allocation and traversal improvements; registration order and calculation behavior are unchanged.

## 8. ModSettings

```csharp
var settings = ModSettings.Register("MyMod", new MySettings { Volume = 1.0f });
settings.Volume = 0.8f;
ModSettings.Save("MyMod", settings);
var current = ModSettings.Get<MySettings>("MyMod");
```

`Register` and `Save` treat `modId` as a single file-name component, not a path. They reject empty IDs, directory separators, rooted/drive paths, alternate-stream syntax, invalid file-name characters and control characters before reading or writing. The resolved JSON path is canonicalized and checked to remain beneath the `My Documents/Mount and Blade II Bannerlord/Configs/ModSettings` directory. Safe names, including spaces and Unicode, are preserved. `Get<T>` only reads the in-memory cache and does not resolve a file path.

`TrySave<T>` returns a `ModSettingsSaveResult` with `Saved`, `SerializerUnavailable`, `SerializationFailed`, or `StorageFailed`. It rejects null settings before serialization, writes to a same-directory temporary file, then moves/replaces the destination atomically. The cache is updated only after the file commit; serialization or storage failure preserves the previous file and cached value. Mod IDs use case-insensitive cache and commit identity on Windows. User serializers and deserializers run outside the per-mod commit lock; if a newer reentrant or concurrent operation commits while `Register<T>` is loading, the older load does not overwrite it. `Save<T>` keeps its signature and logs failed results. Without a serializer, `Register<T>` still returns supplied defaults but does not create a placeholder file that could imply they were persisted.
