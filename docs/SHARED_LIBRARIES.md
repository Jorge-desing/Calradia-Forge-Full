# Shared libraries for mod authors

`ForgeApi.Libraries` is a registry for explicitly named, versioned services shared between Bannerlord modules. Forge creates it before the `ForgeApi.Available` notification and invalidates it when Forge disconnects. It is additive to the legacy SDK v1 test/command registry and remains available from SDK v3; it does not replace Bannerlord's module loader and does not require or load Harmony. Forge offers a separate optional, read-only diagnostic observer for an exact Harmony runtime already loaded by another component; this is not part of shared services or a patching workflow. The current source declares `ForgeApi.Version` 13, including optional patch and runtime-hook capabilities; this does not change the `ForgeApi.Libraries` contract. The SDK targets `net472` and `net8.0`; the game module targets `net472`. Registry operations run on the registry's creating thread (the game thread in Forge).

## Game-model modifiers and owner lifetime

`ForgeModelRegistry` can evaluate registered modifiers without holding its internal lock while user-supplied `Condition` predicates run. Each evaluation uses a stable snapshot: registering or removing a modifier from a predicate does not change the modifiers already selected for that evaluation, and a predicate can reenter the registry without deadlocking it.

For module-owned modifiers, use `BeginOwnerScope(ownerId)` and retain the returned `ForgeModelRegistrationScope` for the module lifetime:

```csharp
private ForgeModelRegistrationScope _modelScope;

void RegisterModels(ForgeModelRegistry registry)
{
    _modelScope = registry.BeginOwnerScope("MyMod");
    _modelScope.Register(new ForgeModelModifier(
        "my_mod.party_speed", ForgeGameModelCategory.PartySpeed, "MyMod",
        "My party speed adjustment", 0.0f, 0.05f));
}

void OnModuleUnloaded()
{
    _modelScope?.Dispose();
    _modelScope = null;
}
```

The modifier's `SourceModule` must match the scope owner exactly. Disposing the scope removes only the registration generations still owned by that scope; if another registration has replaced an ID since then, disposal leaves the replacement intact. Legacy `Register` and ID-based `Unregister` remain available and keep their registry-wide behavior.

## Provider and consumer

Publish a C# interface in a small contracts assembly referenced by both mods. The provider distributes that contracts DLL once; the consumer references the same assembly with `Private=false` and declares the provider as a required module in `SubModule.xml`. Both modules depend on CalradiaForge. Do not distribute a second Forge SDK DLL.

```csharp
// Provider module, in its ForgeApi.RegisterWhenAvailable callback on the game update thread.
library = ForgeApi.Libraries.OpenModule("MyEconomy");
registration = library.Provide<IPriceCalculator>(
    "prices", new Version(1, 0), new PriceCalculator());

// Consumer module, after the provider has loaded.
library = ForgeApi.Libraries.OpenModule("MyTradeMod");
prices = library.Require<IPriceCalculator>(
    "MyEconomy", "prices", new Version(1, 0));
int total = prices.Use(service => service.CalculateTotal(7, 3));
```

Keep each module's `ModuleLibrary` in a field and dispose it in `OnSubModuleUnloaded`. The disposable returned by `Provide` can withdraw just that service. Forge releases registrations, but the provider owns disposal of its implementation and other resources.

During `OnSubModuleLoad`, call `ForgeApi.RegisterWhenAvailable(Register)` and call `ForgeApi.UnregisterWhenAvailable(Register)` during unload. Registration atomically checks the current registry and subscribes for a later connection, so module loading cannot miss availability. Forge initializes the SDK on its first application update; the module-loading thread is not necessarily the update thread. The included provider and consumer `SubModule` classes demonstrate this lifecycle. Access libraries only after availability, from game updates or Forge commands/tests. Callbacks run synchronously on the thread performing registration against a connected host or the connection itself, outside the SDK-wide availability lock. Each managed delivery is guarded against stale connection generations and subscription removal; unregistering before a queued callback's turn skips it, while unregister waits for a callback already in progress to finish. A callback may unregister itself. Keep callbacks short, and do not block them waiting for other threads that may register or unregister managed availability callbacks.

Availability invokes a snapshot of every subscriber in registration order. A failed callback does not prevent later callbacks from running. Connect then throws an `AggregateException` containing callback failures; the game host records it. Successful registrations remain available. This is notification isolation, not transactional rollback: an extension that partially registers before failing must clean up its own resources. Reconnecting creates a fresh library registry and invalidates handles from the previous connection.

## Compatibility, diagnostics, and lifetime

- IDs are case-insensitive. Duplicate module IDs or service IDs within a provider are rejected; no provider is silently selected or overwritten.
- The requested interface must be the exact published CLR contract type, including its assembly identity. Publish interfaces rather than concrete implementation classes.
- Provider and minimum versions must have the same major version; the provider must be at least the requested version. Omitted build/revision components normalize to zero. Major version zero is rejected. Authors remain responsible for honoring their compatibility promises.
- `Require<T>` resolves only a provider and service that are already registered. It does not load a module, search for another provider, coerce a contract, or bypass launcher order. Missing or incompatible dependencies fail immediately with `InvalidOperationException`; callers should use the diagnostic context to correct configuration and then retry during the normal availability lifecycle.
- A missing-provider diagnostic identifies the requested provider module. Confirm the exact module ID, that the provider is installed/enabled, and that the consumer's `SubModule.xml` declares the provider dependency. Both modules must also depend on CalradiaForge.
- A missing-service diagnostic identifies the provider and service ID. Confirm that the provider calls `Provide<T>` with the same service ID from its availability callback and that the consumer's load order follows the provider.
- A contract-mismatch diagnostic identifies the provider/service and the CLR contract expected by the consumer versus the contract published by the provider. Confirm that both projects reference the same contracts project/assembly identity, and package one shared contract DLL with the provider rather than a private consumer copy.
- A version diagnostic identifies the provider/service and the published and requested versions. Align the provider's published version and the consumer's minimum requirement: majors must match and the published version must meet the requested minimum. Do not lower the requirement to hide an incompatible breaking change.
- Handles bind to one registration generation. After withdrawal, provider unload, consumer unload, or registry disconnect, a handle fails instead of silently switching implementations. Resolve a new handle after the provider is registered again.
- Registration, resolution, use, and active disposal occur on the registry's creating thread; in Forge this is the game thread. Provider exceptions propagate to the consumer. The test runner can record them when calls occur inside a registered test.

## Optional resolution and change monitoring

`ModuleLibrary.Resolve<T>(providerModule, serviceId, minimumVersion)` performs the same explicit provider, service, contract-identity, and version checks as `Require<T>`, but returns an immutable `SharedServiceResolution<T>` for lookup outcomes instead of throwing for an unavailable or incompatible service. Its `SharedServiceResolutionStatus` values are `Available`, `ProviderMissing`, `ServiceMissing`, `ContractMismatch`, and `IncompatibleVersion`; `Diagnostic` carries the corresponding correction context, and `IsAvailable` is a convenience check. `TryGetService(out SharedService<T> service)` returns a handle only when the result is available. Invalid arguments, thread-affinity violations, and use after disposal remain programmer/lifecycle errors and still throw. `Require<T>` remains fail-fast and preserves its existing exception type and behavior.

Use `ModuleLibrary.Watch<T>(providerModule, serviceId, minimumVersion)` when a consumer needs to react to later changes rather than poll. The returned `SharedServiceMonitor<T>.Current` is the initial resolution snapshot; creating the monitor does not invoke `Changed`. Later service availability and withdrawal update `Current` and notify `Changed` with immutable `SharedServiceChangedEventArgs<T>.Previous` and `.Current` transition snapshots. Withdrawing and then publishing the same provider/service/version creates a new registration generation and is reported as a change; the registry has no in-place replacement operation. Disposing a provider module while the consumer remains active changes the resolution to a missing provider. Notifications are synchronous on the registry thread and run after the corresponding registry state change is complete. Handler failures are isolated and aggregated in `LastNotificationError` for the latest transition; a successful transition clears that property. One failing handler does not prevent other handlers from receiving the change. A mutation made by a `Changed` handler takes effect synchronously in the registry and refreshes monitor snapshots immediately; only callback delivery is queued while a notification pass is active, preventing recursive dispatch. Consequently, an event's `Previous` and `Current` snapshots may be stale by the time later handlers run. Read `monitor.Current` or call `Resolve<T>` to obtain the latest registry state; a handle from an older registration can already be invalid after withdrawal. Dispose the monitor when it is no longer needed; consumer unload detaches its subscriptions. Disconnecting the global registry silently detaches and disposes its monitors without delivering a final `Changed` notification.

## Atomic publication batches

For providers that expose related services, `ModuleLibrary.BeginPublication()` creates a `SharedServiceBatch`. Add each service with `Add<T>(serviceId, apiVersion, implementation)`, then call `Commit()`. Staged services remain invisible until commit. Commit validates the complete set before changing registry state; an invalid entry or collision leaves the registry without a partially published batch. Observers are notified only after the entire batch has become visible.

The batch object is also the lifetime lease. Disposing it before commit discards the staged entries; disposing it after commit withdraws all registrations in that batch as one state change. Keep the committed batch in a provider-owned field until module unload. Disposal withdraws registrations but does not take ownership of, or dispose, the implementation objects; the provider remains responsible for their resources. Existing `Provide<T>` and its per-service `IDisposable` handle remain supported for independent registrations.

The callback API checks lifetimes before calling the provider. It is not a C# sandbox: callbacks must not retain the implementation, return it for later use, or access game objects from background tasks. An already executing callback is not forcibly interrupted. Shared services are normal mod code; they do not acquire testing permission automatically. Use the existing test/command mutation declarations when exposing state-changing actions through the workbench.

## Buildable provider/consumer example

Three projects demonstrate independently compiled modules:

1. `CalradiaForge.PriceContracts`: shared `IPriceCalculator` interface.
2. `CalradiaForge.PriceProvider`: publishes the `prices` API at 1.0 and checks invalid/overflowing integer totals.
3. `CalradiaForge.PriceConsumer`: resolves that interface and registers `examples.shared_prices`, a read-only test expecting 7 × 3 = 21.

Install the optional `CalradiaForgePriceProvider` and `CalradiaForgePriceConsumer` module folders. Load Forge, Provider, then Consumer. The consumer manifest declares the provider dependency. The contract DLL is packaged only with Provider; the consumer project reference uses `Private=false`, and no second copy belongs in the consumer folder. Do not copy another Forge SDK DLL. Run `examples.shared_prices` from Tests and inspect/export its recorded steps through the existing workflow. This is a small dependency example, not a replacement economy or a claim of a novel pricing algorithm.

The Core test suite exercises the real provider and consumer from separate assemblies, including contract identity, registration and withdrawal, version compatibility failures, missing dependencies, lifecycle invalidation, and example manifests/package layout. Run it through `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`; do not launch test executables directly. Native validation is recorded separately in `VALIDATION.md`.
