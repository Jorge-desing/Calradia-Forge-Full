# Calradia Forge - ForgeWeave Architecture & Event Mesh Rules

ForgeWeave is the reactive event mesh, dynamic patch orchestrator, and Gauntlet UI discovery framework for Calradia Forge. It provides decoupled, safe, and deterministic event propagation across mod modules without relying on unmanaged detours or destabilizing the Bannerlord simulation loop.

## 1. Event Mesh & Registry Hierarchy

### Additive Event Registry
- Events are declared and registered additively through `ForgeApi` and `ForgeWeaveEngine`.
- **Atomic Availability**: `ForgeApi` availability and handler registrations must remain atomic across pipe reconnects or hot-reloads.
- **Lifecycle Events**: The engine dispatches host lifecycle events (`GameLoaded`, `MissionEnded`, `ContextLeaving`) with deterministic order.

### Replay Registry & Host Isolation
- **Host-Bound Replays**: The replay registry is exposed **exclusively** through the connected host (`IForgeWeaveHost`).
- **No Self-Replay**: Replay events must never record another replay as its source (`isReplay` guard). Replay evidence must survive report normalization and be safely rendered.
- **Opt-In Replay Handlers**: Replay dispatches only invoke handlers that explicitly opt in to replay processing. Handlers must reject retained events outside their original logical context.

## 2. Deterministic Ordering & Dependency Graphs

### Priority and Dependency Resolution
- Handlers specify an execution priority (`Priority`) and optional dependencies (`DependsOn`).
- **Deterministic Resolution**: Handlers with equal priority and explicit dependencies are sorted deterministically.
- **Cyclic Ordering Rejection**: Cyclic dependencies must be detected and rejected immediately during registration rather than guessing execution order or entering infinite loops.

## 3. Performance & Zero-Allocation Constraints

### Modulo-24 and Pulse Frequency Rules
- **No Per-Frame Pulses**: Handlers subscribed to `Pulse` events **CANNOT** run every frame. They must specify a minimum throttle interval (`MinMilliseconds`, typically $\ge 50$ms) or integrate with modulo-24 time slicing.
- **Zero GC Allocation in Hot Paths**:
  - Telemetry counters, health monitoring (`MinMilliseconds` tracking), and dispatch loops must avoid heap allocations.
  - Do NOT use LINQ (`.Select()`, `.Where()`, `.ToList()`) inside tick, hourly, or pulse dispatch loops.
  - Use pre-allocated arrays, indexed `for` loops, and atomic/interlocked flags for status monitoring.

### Bounded Execution Budgets
- Handlers declare an execution budget (`ExecutionBudgetMs`).
- Overruns are measured and recorded in telemetry without aborting the broader engine or campaign simulation.
- Oversized or malformed event declarations must be validated and rejected at registration time.

## 4. Fault Isolation & Dynamic Quarantine

### Exception Containment
- Errors in third-party or custom handlers are caught and isolated in `ForgeWeaveEngine`. An unhandled exception in one handler must never abort subsequent framework handlers or crash the game.

### Automated Quarantine System
- **Quarantine Threshold**: A handler that repeatedly throws exceptions or exceeds critical execution thresholds is automatically placed in quarantine.
- **Dynamic Unregistration**: Handlers can be unregistered dynamically by string identifier (`HandlerId`) or object instance.
- **Quarantine Management**: Quarantined handlers can be inspected, cleared, or reset globally or on a per-module basis.

## 5. Event Filtering & Data Safety

### Filtering Capabilities
- Event subscriptions support both positive criteria and `ExcludedData` negative criteria to avoid invoking handlers when unwanted parameters are present.
- Oversized or malformed filter specifications must be rejected at registration.

### Scalar Data Copying & Engine Reference Safety
- **Scalar Isolation**: Only bounded scalar event data (integers, strings, floats, booleans) should be copied and passed to event arguments.
- **No Transient Engine Handles**: Never pass raw engine pointers or transient mission agents across async or replay boundaries. Use `StringId` or entity identifiers, and resolve them against `MBObjectManager` when required.
- **Context Preservation**: During `ContextLeaving` events, the logical source context must be preserved so handlers can perform stateful cleanup safely.

## 6. Simulation Gates & Campaign Safety

### Campaign Writer Gates
- Handlers that modify campaign state ("Writers") **MUST** enforce the campaign test gate (`IsInTestMode == true`) and operate against a sandboxed or explicitly gated campaign copy.
- Unsafe writer declarations and unthrottled Pulse declarations are rejected by policy.

## 7. Gauntlet UI Dynamic Discovery & Policy

### Dynamic UI Discovery Requirements
- Gauntlet UI registration requires matching owned `ViewModel` instances with valid `.xml` Gauntlet prefabs.
- **Validation Rules**:
  - Reject ViewModels that do not inherit from `TaleWorlds.Library.ViewModel`.
  - Reject UI registrations missing required command bindings.
  - Reject duplicate command identifiers and duplicate page keys.
  - Discard orphaned UI registrations when an owning module unloads.
- **Policy Enforcement**:
  - Gauntlet UI policy strictly enforces page and command context.
  - UI action commands that trigger state-modifying work require test mode and a campaign copy for writer safety.
