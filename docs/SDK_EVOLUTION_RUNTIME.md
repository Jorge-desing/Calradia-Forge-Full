# Runtime evolution and interoperability evidence

This note records the capability boundary verified in source revision `59789ad` (2026-10-01). It is evidence guidance, not a promise that uncommitted working-tree changes have passed the same review or validation.

The parallel hook delivery was subsequently committed as `d8c0def` with SDK contract
13. It is a separate change, not an API increment made by the onboarding work.
The earlier combined implementation passed the integrated Core BAT and an 11-case
isolated BAT fixture. After assembly identity, public-surface, and exact-signature
hardening, the isolated fixture passed 21/21. Neither fixture establishes live
Bannerlord coexistence or concurrent native patch safety.

## Runtime capabilities are separate

| Capability | Verified behavior | Limit |
|---|---|---|
| ForgeWeave | Cooperative handlers for callbacks emitted by Forge's Bannerlord adapter. Failures are timed, isolated, and may be quarantined. | It does not intercept arbitrary methods, load mod assemblies, or emit IL. |
| Patch Blueprint Preflight | Resolves inert declarations against assemblies already loaded in the session. | It does not apply, order, or prove the safety of a patch. |
| `ForgeApi.Patches` / `ForgePatcher` | Explicit, experimental full-method replacement with verification and revert records. | The raw writer does not stop other threads or decode/relocate overwritten instructions. Do not present it as safe for concurrent live targets or as a general third-party patch-framework replacement. |
| `ForgeApi.Hooks` | The API 13 contract adds an explicit `Finalizer` callback alongside Prefix/Postfix. It runs after the callback/original path, including failures, while the host callback gate permits it; it can preserve, replace, or suppress a pending exception, subject to return-value validation. | Hooks are synchronous on the target caller's thread. Registration is inert and Apply/Revert are explicit host operations under a strict game-thread/main-menu gate. A throwing Finalizer preserves both failures in an `AggregateException`; serial fixtures do not prove safety for concurrent target execution. |
| Host-local IL transpiler | The Bannerlord `net472` host has a separate `ForgeHookService.RegisterTranspiler` entry point using MonoMod's `ILContext.Manipulator` and the common hook handle/lifecycle. | The SDK exposes the ordinary registration lifecycle, snapshots, and a `HasTranspiler` metadata flag, but not this registration entry point or the manipulator/`ILContext`; IPC never carries the delegate or IL context. Although initial application and management are host-gated, MonoMod can reconstruct an active IL transformation on another caller's thread until explicit reversion. Manipulators must be repeatable and use only supplied IL and stable configuration; they must never access live game state. No concurrent-safety claim is established. |
| `ForgePatchDiagnostics` | Bounded snapshot of Forge-owned hooks/replacements; optional reflection adapter queries the public API from a caller-supplied `0Harmony` assembly only if already loaded. No Harmony build or distribution dependency. | External inspection is intended to read state, but synchronous queries and public getters execute in-process and may do internal work; bounds cover enumeration and copied output, not Harmony's CPU or allocations. This is not a sandbox, cannot identify every other backend, and cannot prove a destructive conflict. |

The historical cited source revision had `ForgeApi.Version = 12`; the current source checkout has `ForgeApi.Version = 13`. Check runtime optional capabilities directly; do not infer that a service is implemented solely from a compile-time version constant. Any later contract or hook additions must be audited and tested separately before documentation or product claims change.

## Evidence categories for patch diagnostics

`ForgePatchDiagnosticsSnapshot` separates Forge-owned hook/replacement records from the optional `ExternalRuntime` observation. The outer snapshot includes capture time, bounded notes and a host-controlled stale flag; the external section can report `NotRequested`, `NotLoaded`, `Observed`, `Incomplete` or `Unsupported` and carries its runtime identity and bounded targets/owners/observations. The optional adapter only considers a caller-supplied, already-loaded assembly whose simple name is `0Harmony`, then reflects public query members. Its limits bound assembly/target enumeration and copied output; they do not bound CPU time or allocations performed inside synchronous Harmony calls or public getters. Those calls run in-process, so this adapter is not a sandbox and does not promise zero side effects. Interpret evidence conservatively:

- **Observed:** the Forge registries returned their copied records; separately, an external observation is present only when the optional adapter queried the expected public `0Harmony` query surface on an already-loaded assembly. This is a bounded runtime diagnostic signal, not release/mod compatibility evidence.
- **Review:** multiple external owners report the same target. Shared ownership is a review signal, not proof that hooks are incompatible or that the target is currently detoured by every owner.
- **Inconclusive:** the optional runtime was not loaded, unsupported, incomplete, or returned no target metadata. An empty external snapshot is never evidence that other runtimes or native detours are absent. `ConflictCount` summarizes Forge-owned record states; it is not an automatic cross-framework conflict verdict.

`Runtime` marks cached patch-diagnostics and patch-preflight evidence stale during its initial context setup and when it later observes a change in the `Campaign.Current` or `Mission.Current` object references. This is a context-change signal, not a wall-clock TTL. The isolated external-adapter fixture covers capture timestamps and freshness notes, but it does not drive Bannerlord's `Runtime` through a real campaign/mission transition. No existing game-state mock provides that transition, so `IsStale` behavior on context change remains pending an engine-backed regression or live validation.

Patch diagnostics must never automatically reorder, disable, or revert third-party patches. A future conflict triage needs stable method identity, explicit evidence provenance and freshness, and tests against actual interop behavior before any stronger label is justified. Runtime detour changes outside Forge's registries may be invisible to the optional read-only adapter.

## Diagnostics and staged evolution

Attribute failures only to Forge-owned handlers or hooks for which Forge has an ID, target, execution stage, and captured exception. Do not claim universal crash interception or promise source line attribution when symbols are unavailable. Keep callback work and diagnostic storage bounded on the game thread.

Recommended order: reconcile the committed SDK surface against pending changes; retain read-only observations and uncertainty labels; validate interop in isolated fixtures; only then consider user-facing review policies. Keep raw executable-memory replacement opt-in and experimental until concurrent execution and instruction-boundary safety are demonstrated. High-level builders and IDE templates can progress independently of this runtime boundary.
