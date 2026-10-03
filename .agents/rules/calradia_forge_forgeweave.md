# Calradia Forge - ForgeWeave Architecture & Event Mesh Rules

ForgeWeave is Calradia Forge's cooperative lifecycle-event framework. It dispatches declared events through Forge-owned host callbacks; it is not a method-patching or detour orchestrator. Gauntlet page registration, executable hooks, and experimental whole-method replacement are separate integration surfaces with distinct contracts and validation. ForgeWeave provides bounded event data and handler-level controls, but it cannot sandbox arbitrary synchronous extension code or guarantee that the game process will remain stable.

## 1. Event Mesh & Registry Hierarchy

### Additive Event Registry
- Events are declared and registered additively through `ForgeApi` and `ForgeWeaveEngine`.
- **Host Generation**: Managed handler registrations must follow the current `ForgeApi` host generation across connect, disconnect, and reconnect. Stale callbacks must not replace or remove a newer host's registration; do not imply hot-reload support unless it is separately implemented and tested.
- **Lifecycle Events**: The engine dispatches only declared event kinds raised by its host adapters (including `GameLoaded`, `MissionEnded`, and `ContextLeaving`). Handler order is determined by priority and valid same-event, same-priority `Before`/`After` constraints; this does not order other mods' callbacks or patch systems.

### Replay Registry & Host Isolation
- **Host-Bound Replays**: The replay registry is exposed **exclusively** through the connected host (`IForgeWeaveHost`).
- **No Self-Replay**: Replay events must never record another replay as its source (`isReplay` guard). Replay evidence must survive report normalization and be safely rendered.
- **Opt-In Replay Handlers**: Replay dispatches only invoke handlers that explicitly opt in to replay processing. Handlers must reject retained events outside their original logical context.

### Interoperability Boundaries
- ForgeWeave is cooperative event dispatch from Forge-owned host callbacks; it is not Harmony compatibility, method interception, or conflict resolution.
- Keep Patch Blueprint Preflight (declaration-only), explicitly applied runtime hooks, and experimental whole-method replacement as distinct capabilities with separate validation and lifecycle rules. See `docs/DEPENDENCY_FRAMEWORK_REVIEW.md` and `docs/PATCH_BLUEPRINTS.md`.
- Optional Harmony observation may use Forge-owned reflection over a caller-supplied, already-loaded `0Harmony` assembly only. Never add a Harmony reference/package, load it, or modify third-party patches. A shared target or owner label is a review signal, not proof of conflict or module attribution; an observation does not establish compatibility or coexistence.
- Attribute an extension failure to a ForgeWeave handler only when Forge captures an exception from that handler's invocation. Stack frames, shared targets, and external owner metadata alone do not prove causation; diagnostics must never automatically reorder, unpatch, or disable third-party code.

## 2. Deterministic Ordering & Dependency Graphs

### Priority and Dependency Resolution
- Handlers specify an execution priority (`Priority`) and optional `Before`/`After` references.
- **Deterministic Resolution**: Higher-priority handlers run first. `Before`/`After` only refine order among handlers registered for the same event and priority.
- **Invalid Ordering**: Missing, cross-event, cross-priority, self-referential, or cyclic references are reported as blocked declarations; do not infer ordering or describe these checks as method-patch conflict resolution.

## 3. Performance and Allocation Guidance

### Deferrable Work and Pulse Throttling
- **Pulse Throttle**: Pulse handlers must declare a throttle; time-slicing is not a substitute. The current `ForgeWeaveEngine` rejects intervals below 250 ms. Check the engine and analyzer sources if this contract changes.
- **Stable Time Slicing**: For periodic work that can safely be deferred, use `ForgeTimeSlicer.ShouldProcess` with a stable entity ID. It assigns a deterministic bucket but does not guarantee even bucket sizes. Filtering during a full collection scan still costs O(N).
- **Allocation-Aware Hot Paths**:
  - Avoid LINQ (`.Select()`, `.Where()`, `.ToList()`) and unnecessary materialization inside tick, hourly, or pulse dispatch loops.
  - Prefer pre-sized collections, indexed loops, and atomic/interlocked flags where measurement and ownership semantics support them.
  - Do not claim zero allocations for telemetry, health tracking, or dispatch without measuring the complete synchronous path, including handlers and callbacks.

### Bounded Execution Budgets
- Handlers declare an execution budget with `BudgetMilliseconds` and `BudgetPolicy`.
- An overrun is measured and recorded after the synchronous handler returns; it does not preempt, cancel, or quarantine that handler by itself. A long-running or non-returning callback can still delay the game thread.
- Oversized or malformed event declarations must be validated and rejected at registration time.

## 4. Fault Isolation & Dynamic Quarantine

### Exception Containment
- Ordinary managed exceptions thrown by an invoked handler are caught, recorded, and isolated so eligible later ForgeWeave handlers can continue, subject to cancellation and propagation rules.
- This does not terminate a handler that hangs or consumes excessive resources, sandbox arbitrary code, or guarantee recovery from process-fatal, native, or runtime failures. Do not promise that handler isolation prevents every game crash.

### Circuit Breaker and Quarantine
- **Failure Threshold**: Repeated consecutive handler exceptions up to the subscription's declared `FailureLimit` open its circuit. An unsuccessful half-open recovery probe also reopens the circuit.
- **Budget Evidence**: A declared execution-budget overrun is measured and reported according to `BudgetPolicy`; it does not by itself quarantine the handler. Do not claim that a slow handler is automatically stopped or quarantined solely for exceeding a budget.
- **Dynamic Unregistration**: Handlers can be unregistered dynamically by string identifier (`HandlerId`) or object instance.
- **Quarantine Management**: Quarantined handlers can be inspected, cleared, or reset globally or on a per-module basis.

## 5. Event Filtering & Data Safety

### Filtering Capabilities
- Event subscriptions support both positive criteria and `ExcludedData` negative criteria to avoid invoking handlers when unwanted parameters are present.
- Oversized or malformed filter specifications must be rejected at registration.

### Scalar Data Copying & Engine Reference Safety
- **Scalar Isolation**: Event payloads are copied into bounded string key/value pairs. Encode numeric or Boolean values as explicit strings when needed; do not expose arbitrary objects or engine references.
- **No Transient Engine Handles**: Never pass raw engine pointers or transient mission agents across async or replay boundaries. Use `StringId` or entity identifiers, and resolve them against `MBObjectManager` when required.
- **Context Preservation**: During `ContextLeaving` events, the logical source context must be preserved so handlers can perform stateful cleanup safely.

## 6. Simulation Gates & Campaign Safety

### Campaign Writer Gates
- Handlers that modify campaign state ("Writers") **MUST** enforce the campaign test gate (`IsInTestMode == true`) and operate against a sandboxed or explicitly gated campaign copy.
- Unsafe writer declarations and unthrottled Pulse declarations are rejected by policy.

## 7. Separate Gauntlet UI Integration

Gauntlet page discovery, prefab validation, ownership, and command policy are separate from ForgeWeave event dispatch. Keep their rules in [`.agents/rules/gauntlet_architecture.md`](gauntlet_architecture.md); do not duplicate UI guarantees here or describe ForgeWeave as a UI discovery framework.
