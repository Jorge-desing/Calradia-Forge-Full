# ForgeWeave

This page documents the Calradia Forge 25.0.0 source and SDK contract version 10. Existing distribution ZIPs were not regenerated and do not attest to the v25.0.0 source APIs.

ForgeWeave is Calradia Forge's standalone extension framework. It is a cooperative event system, not a method-patching library: it does not discover methods, emit IL, replace callbacks, or load another mod framework.

It gives authors a documented place to run small extension handlers from Bannerlord callbacks that Forge already owns. Each handler declares its identity, context, access level, priority, ordering constraints, failure limit and, for pulses, a minimum interval. Forge records health and timing for every handler without treating an overlap as a conflict.

## Why use it

ForgeWeave addresses operational concerns that a general patch mechanism leaves to every author:

- deterministic ordering with explicit, inspectable `Before` and `After` declarations;
- context and state-change gates shared with Forge's test mode and campaign-copy protection;
- per-handler exception isolation and automatic quarantine after repeated failures;
- bounded scalar event data, rather than live campaign, mission or agent objects;
- declarative exact-match filters over copied scalar data, so handlers can target one host variant without unsafe predicates;
- opt-in execution budgets that report slow handlers without aborting or quarantining arbitrary extension code;
- per-handler timing, event counts, health status and a bounded dispatch journal;
- JSON, HTML, in-game and desktop inspection through the same `framework` snapshot.

It does not claim to replace a patching library where an author truly needs to modify an arbitrary third-party method. Use it for cooperative work that can be expressed through official host lifecycle events.

## Register a handler

Reference `CalradiaForge.Sdk.dll`, declare `CalradiaForge` as a required module, and register with Forge's atomic availability helper. `ForgeApi.Events` is assigned before its callback runs.

```csharp
using System.Collections.Generic;
using CalradiaForge.Sdk;

public sealed class StartupObserver : IForgeEventHandler
{
    public ForgeEventSubscription Subscription => new ForgeEventSubscription
    {
        Descriptor = new Descriptor
        {
            Id = "my_module.startup_observer",
            Module = "MyModule",
            Name = "Startup observer",
            Context = Context.Any,
            ChangesState = false
        },
        Event = ForgeEventKind.ForgeReady,
        Priority = 100,
        Access = ForgeEventAccess.Observe,
        Filter = new ForgeEventFilter
        {
            RequiredData = new Dictionary<string, string> { ["suite"] = "0.7.1" }
        },
        // Disabled is the default. This read-only handler explicitly permits
        // a controlled replay of an event Forge retained.
        ReplayMode = ForgeReplayMode.Live
    };

    public void Handle(ForgeEvent @event)
    {
        @event.Cancellation.ThrowIfCancellationRequested();
        var suite = @event.Data.TryGetValue("suite", out var value) ? value : "unknown";
        // Consume copied scalar metadata only. Do not retain the event after this callback.
    }
}

// In the extension's MBSubModuleBase:
protected override void OnSubModuleLoad() => ForgeApi.RegisterWhenAvailable(Register);
protected override void OnSubModuleUnloaded() => ForgeApi.UnregisterWhenAvailable(Register);
void Register(IForgeRegistry registry) => ForgeApi.Events?.Register(new StartupObserver());
```

`RegisterWhenAvailable` atomically checks the current connection and records the callback for a later connection, so module loading cannot miss Forge between a null check and an event subscription. IDs are global across Forge tests, commands, diagnostics, patch blueprints and ForgeWeave handlers.

The included [`CalradiaForge.Examples`](../examples/CalradiaForge.Examples/Examples.cs) module registers `examples.forgeweave.ready`, a read-only `ForgeReady` observer.

### Managed delegate subscription lifecycle (SDK contract 9)

For a delegate-based handler, `ForgeCampaignEvents.SubscribeWeaveWhenAvailable` returns a `ForgeWeaveRegistration`. The handle owns both the pending availability callback and any active ForgeWeave handler, so store it for the owning module's lifetime and call `Dispose()` from `OnSubModuleUnloaded`:

```csharp
private ForgeWeaveRegistration _registration;

protected override void OnSubModuleLoad()
{
    _registration = ForgeCampaignEvents.SubscribeWeaveWhenAvailable(
        "my_module.startup_observer",
        ForgeEventKind.ForgeReady,
        OnForgeReady,
        access: ForgeEventAccess.Observe);
}

protected override void OnSubModuleUnloaded()
{
    _registration?.Dispose();
    _registration = null;
}

private void OnForgeReady(ForgeEvent @event) { }
```

The helper atomically checks host availability and registers synchronously if Forge is connected. Otherwise it waits without losing a connection that arrives during module startup. A disconnect unregisters the active handler and returns the handle to `WaitingForHost`; a later connection registers it again. Registration callbacks run synchronously on the host connection thread. `Dispose()` prevents later registrations and releases the active handler. A pending handle may be disposed from any thread; when the handler is active, dispose it on the thread that registered it. An off-thread call throws `InvalidOperationException`, leaves the registration active, and records the error so the owner can retry on the correct thread.

| `ForgeWeaveRegistrationState` | Meaning |
|---|---|
| `WaitingForHost` | Waiting for a supported event host. |
| `Registered` | Handler is active with the current host. |
| `HostUnsupported` | A host is connected but does not support this registration path. |
| `Failed` | Registration failed; inspect `Error`. |
| `Disposed` | The registration owner released the handle. |

`ForgeWeaveRegistration.State`, `.Error` and `.Handler` expose its current state, latest failure, and active `IForgeEventHandler` when one exists. `SubscribeWeave` remains an immediate-registration API and throws `InvalidOperationException` if `ForgeApi.Events` is unavailable or the call is off the host connection thread; use `UnsubscribeWeave` on the connection thread to release its returned handler. The managed handle tracks availability and reconnects, while the immediate call requires an already connected host.

## Host events

Forge delivers these events only when its own Bannerlord adapter receives the corresponding official callback. The framework has no polling thread and does not create its own game loop.

| Event | Host source | Scalar data examples |
| --- | --- | --- |
| `ForgeReady` | First Forge application update after SDK registration | `suite`, `target` |
| `InitialScreenReady` | `OnBeforeInitialModuleScreenSetAsRoot` | `context` |
| `ContextEntering` | Forge detects a campaign/mission identity transition | `context` |
| `ContextLeaving` | Forge detects a campaign/mission identity transition | `from`, `to`; the event context remains `from` |
| `CampaignStarted` | `OnCampaignStart` | `context` |
| `GameLoaded` | `OnGameLoaded` | `context` |
| `MissionInitialized` | `OnMissionBehaviorInitialize` | `context` |
| `MissionEnded` | `MissionLogic.OnEndMission` / `EventObserver` | `context` |
| `AgentCreated` / `AgentRemoved` | Forge's mission observer | `agent_index` |
| `Pulse` | Bounded application-update pulse | `host_interval_ms` |
| `Custom` | Programmatic mod-to-mod event publishing via `ForgeApi.PublishCustomEvent` | Arbitrary custom payload |

The adapter queues these events and drains at most 16 per Forge application update. This keeps extension dispatch on Forge's normal game-thread path. A queue is bounded to 128 entries; when it fills, the oldest queued event is discarded and Forge records a warning.

`ForgeEvent.Data` is a read-only copy with at most 32 pairs. Keys are capped at 64 characters and values at 512. The event does not expose `ITestServices`, live objects, or a general game-object resolver.

### Custom Mod-to-Mod Event Mesh

In addition to host lifecycle callbacks, ForgeWeave provides a decoupled mod-to-mod event mesh via `ForgeEventKind.Custom`. Any module referencing the SDK can publish custom events across mods without direct assembly coupling.

- **Publishing Events:**
  Call `ForgeApi.PublishCustomEvent(topic, data)` or `IForgeEventRegistry.PublishCustom(topic, data)`. The call validates the topic and queues the event for dispatch on the main game thread.
- **Topic Matching Semantics:**
  Subscribers declare their target topic pattern in `ForgeEventSubscription.Topic`:
  - **Exact Match:** `Topic = "economy.trade.caravan_arrived"` matches only dispatches for that exact topic.
  - **Prefix Wildcard:** `Topic = "economy.*"` matches any topic starting with `"economy."` (e.g., `"economy.trade"`, `"economy.market.crisis"`).
  - **Catch-All:** `Topic = "*"` (or omitted/null) matches all custom events across the mesh.
- **Queueing & Safety:**
  Custom events share Forge's bounded dispatch queue (up to 128 queued events, draining at most 16 per game frame). Payloads are validated against scalar constraints (up to 32 pairs, keys <= 64 chars, values <= 512 chars).

### Dynamic Unregistration

Extensions can dynamically remove registered handlers at runtime using `IForgeEventRegistry.Unregister(string id)` or `IForgeEventRegistry.Unregister(IForgeEventHandler handler)`. Dynamic unregistration cleanly frees the registered ID slot and handler slot without restarting the session or affecting other extensions.

### Declarative event filters

Set `ForgeEventSubscription.Filter.RequiredData` when a handler should receive only events whose copied scalar payload contains exact key/value pairs. Set `ForgeEventSubscription.Filter.ExcludedData` for declarative negative matching: if any excluded key/value pair is present in the payload, the event is immediately filtered out. An empty filter matches every payload. Forge validates and copies both maps when the handler is registered; each map accepts at most eight pairs, caps keys at 64 characters and values at 512, and uses ordinal equality. A non-matching event is recorded as `Filtered` with a bounded outcome and does not invoke, authorize or count as a handler failure. Filters are evaluated again against the retained copied payload during replay, so a replay cannot bypass the declaration.

Set `BudgetMilliseconds` to a value from 1 to 5,000 to make slow callbacks visible. The default `ForgeBudgetPolicy.Warn` records an `OverBudget` outcome and timing evidence after the callback returns; `Ignore` leaves the callback timing available but does not count an overrun. A budget is diagnostic only: Forge cannot safely interrupt arbitrary C# code and never treats an overrun as a handler failure or quarantine trigger. Handler and event health records track `MinMilliseconds`, `MeanMilliseconds`, and `MaxMilliseconds` execution timing.

## Replay Lab

Replay Lab is controlled event verification, not method interception. Forge retains at most 64 **host** event records after their dispatch returns. A record contains its source sequence, event kind, logical context, timing, copied scalar payload and original dispatch outcome. It never retains a game object, service or a replay as another replay source.

Replay is disabled for every handler by default. A handler must explicitly set `ForgeEventSubscription.ReplayMode`:

- `Disabled` receives no replay.
- `ObserveOnly` is for read-only verification handlers.
- `Live` permits an intentional replay, but grants no additional access: the current game context must exactly match the retained record, and any `CampaignWrite` or `MissionWrite` handler still passes Forge's existing context, test-mode and copied-campaign gates.

On a replay, `ForgeEvent.IsReplay` is `true` and `ForgeEvent.SourceSequence` identifies the retained host event. The replay has its own dispatch sequence. Handlers can use these fields to avoid duplicate external work. A rejected source, stale/missing sequence, context mismatch, unavailable opted-in handler or writer-gate failure is recorded as a replay outcome; Forge does not retry it automatically.

`ForgeApi.Replays.Records` returns detached copies of the retained records. `ForgeApi.Replays.Replay(sequence, services, cancellation)` accepts only one retained sequence, so an extension cannot inject an arbitrary payload or callback. The local `replay` pipe action follows the same rule. In the native Framework workbench, enter the retained sequence in **Search / argument** and use **Replay**. In the desktop workbench, select an evidence row and run its replay command. Both reports show retained evidence, replay status, timing and handler outcomes.

## Pulse handlers

Only a `Pulse` handler may specify `MinimumIntervalMilliseconds`; it must be between 250 and 60,000. All other event kinds require zero. Forge's host pulse is already limited to roughly 250 ms, and each pulse handler gets an additional independent due-time check.

```csharp
Event = ForgeEventKind.Pulse,
MinimumIntervalMilliseconds = 1000,
Access = ForgeEventAccess.Diagnostics
```

This prevents an accidental handler from running once per frame. A skipped handler records that it is waiting for its interval; it is not an error.

## Ordering and permissions

Higher `Priority` values run first. `Before` and `After` can refine the order only when both handlers have the same event and priority. Use complete, globally unique handler IDs such as `my_module.cache_refresh`.

Forge blocks a handler rather than guessing when a declared target is missing, belongs to another event, has a different priority, references itself, or participates in an ordering cycle. Independent handlers still run. The Framework view and reports show the blocking reason and finding.

`ForgeEventAccess` is explicit:

- `Observe` and `Diagnostics` require `Descriptor.ChangesState=false`.
- `CampaignWrite` requires `ChangesState=true` and `Context.Campaign`.
- `MissionWrite` requires `ChangesState=true` and `Context.Mission`.

State-changing handlers pass through the same session test-mode and campaign-copy checks as Forge tests. The gate makes Forge-provided dispatch accountable; it cannot prevent unrelated code from directly using Bannerlord APIs outside Forge.

## Failures, stopping and health

Forge invokes every eligible handler independently. An exception records a bounded warning, updates that handler's health and does not stop later handlers. Dispatch plans for non-`Custom` event kinds are reused until handler registration changes; `Custom` plans remain per-dispatch because their open-ended topics must not create an unbounded cache. Registration and unregistration invalidate retained plans. `Snapshot()` also reuses each current finite-event plan while copying its current findings into the report; custom-topic planning remains uncached. Snapshot health, journal, replay and event data are still copied from current state.

### Smart Circuit Breaker with Auto-Recovery

Handlers can configure their resilience policy via `ForgeEventSubscription.CircuitBreakerPolicy`:

- **`ForgeCircuitBreakerPolicy.AutoRecover` (Default):**
  When a handler reaches its `FailureLimit` (1 to 5 consecutive exceptions, default: 3), the circuit transitions from `Closed` to `Open` (quarantined). While `Open`, all incoming events are safely skipped.
  Once `CircuitBreakerCooldownSeconds` (default: 10s, configurable between 1s and 300s) elapses, the next dispatch transitions the circuit to a probationary `HalfOpen` state and executes a single test probe:
  - **Probe Succeeded:** The circuit transitions back to `Closed`, failure counters reset to 0, quarantine is lifted, and cooldown resets to its baseline.
  - **Probe Failed:** The circuit transitions back to `Open`, the handler is re-quarantined, and an exponential backoff doubles the cooldown interval (e.g., 10s → 20s → 40s → maximum 60s).
- **`ForgeCircuitBreakerPolicy.PermanentQuarantine`:**
  Preserves hard-quarantine behavior: once `FailureLimit` is reached, the handler remains quarantined indefinitely until explicitly reset by an operator or on session reload.

Quarantined handlers can also be manually unquarantined at any time individually, by module (`ResetModuleQuarantines`), or globally (`ResetAllQuarantines`).

### Bounded APM Latency Telemetry

ForgeWeave tracks Application Performance Monitoring (APM) metrics per handler. Dispatch recording writes into a preallocated 64-sample buffer; percentile reporting copies and sorts that bounded sample window on demand, so the reporting path is not allocation-free:

- **Rolling Sample Window:** A fixed 64-element circular buffer (`double[64]`) records execution latency in milliseconds.
- **Latency Percentiles:** Calculates P50 (median), P95, and P99 latency percentiles on demand.
- **Histogram Distribution Buckets:**
  - `< 1.0 ms`: Sub-millisecond executions (`BucketUnder1Ms`)
  - `1.0 ms - 5.0 ms`: Fast operations (`Bucket1To5Ms`)
  - `5.0 ms - 20.0 ms`: Moderate operations (`Bucket5To20Ms`)
  - `> 20.0 ms`: Heavy operations (`BucketOver20Ms`)
- All metrics are exposed in `ForgeWeaveHandlerHealth`, developer console inspect tools, and the Desktop Workbench.

`ForgeEvent.StopPropagation(reason)` stops later **ForgeWeave** handlers for the current event only. It never cancels, replaces or blocks Bannerlord's original callback.

`ForgeWeaveSnapshot` includes handler status, declared order, timing totals/mean/minimum/maximum, failures, event health, findings, retained replay evidence, recent replay outcomes and the 64 most recent dispatch records. The named-pipe actions are:

- `framework` — full `ForgeWeaveSnapshot`.
- `event-journal` — the bounded `RecentDispatches` list.
- `replay` — replays one retained source sequence in the live game and returns its guarded outcome.
- `forgeweave-unquarantine` — restores quarantined handlers (`id`, `all`, or `module:<name>`).
- `forgeweave-clear` — clears `journal`, `replays`, or `all` bounded history.

### Developer Console Commands

ForgeWeave exposes native in-game console commands (accessible via `ALT + ~`):

- `cf.forgeweave.status` — overview of registered, active, blocked, and quarantined handlers.
- `cf.forgeweave.handlers [module]` — lists registered handlers and their execution metrics.
- `cf.forgeweave.handler <id>` — detailed inspection of one specific handler, including Topic, Circuit State (`Closed`, `Open`, `HalfOpen`), cooldown backoff status, P50/P95/P99 latency percentiles, and histogram distribution. Percentile reporting copies and sorts the bounded sample window.
- `cf.forgeweave.publish <topic> [key=value ...]` — publishes a custom mod-to-mod event into the ForgeWeave event mesh.
- `cf.forgeweave.unquarantine <id|all|module:name>` — resets quarantine on handlers.
- `cf.forgeweave.replay <sequence>` — replays a retained event sequence in-game.
- `cf.forgeweave.journal [limit]` — displays recent event dispatches with timing and outcomes.
- `cf.forgeweave.clear [journal|replays|all]` — clears bounded journal or replay history.

The native Modules area exposes **Framework** and the desktop companion has a dedicated **Framework** section. Reports capture a current in-memory snapshot without reflection or game-object traversal.

## Limits and lifecycle

ForgeWeave accepts at most 256 handlers and at most 32 ordering references per handler. It limits ordering IDs and error text before exposing them to UI or reports. Handler code remains synchronous and should be brief; Forge can isolate failures but cannot safely terminate arbitrary C# code mid-execution.

ForgeWeave Replay Lab and declarative filters are SDK v3 additive surfaces. Existing test, command, diagnostic, shared-library and Patch Blueprint consumers remain source-compatible. It requires no third-party mod-framework dependency.
