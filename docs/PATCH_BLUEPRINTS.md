# Patch Blueprint Preflight

Patch Blueprint Preflight is a Forge SDK tool for describing and reviewing a proposed method patch before applying anything. It is original Forge functionality and has no build-time or package dependency on Harmony, MCM, ButterLib, or another mod. The declared hook vocabulary (`Prefix`, `Postfix`, `Transpiler`, `Finalizer`) is descriptive metadata; it does not mean Forge implements every hook kind.

Preflight is read-only. It invokes a registered provider's `Describe` method only for an explicit capture, but never loads an assembly, applies or removes a patch, or invokes the declared patch callback. A resolved target proves only that the declaration matches a method in an assembly already loaded in the current game session. It does not prove that an executable detour can be installed or that the game behavior will remain compatible.

Preflight and patch application are separate capabilities. Preflight can report a declaration without enabling or applying it. Runtime application is opt-in and experimental; it is not part of module startup and does not happen merely because an assembly contains a `[ForgePatch]` attribute. The legacy `ForgeBootstrapper.InitializeGlobalPatches()` entry point is obsolete and does nothing. `ForgePatcher.ApplyAll(assembly)` remains an explicit request and validates the whole discovered batch before attempting any write.

## Register a blueprint provider

Use the additive registry when Forge becomes available. A blueprint provider must use `ChangesState=false`; Forge rejects a provider that claims to change state.

```csharp
using System.Collections.Generic;
using System.Reflection;
using CalradiaForge.Sdk;

public sealed class MyPatchBlueprints : IPatchBlueprintProvider
{
    public Descriptor Descriptor => new Descriptor {
        Id = "my_module.patch_blueprints",
        Module = "MyModule",
        Name = "My patch blueprints",
        Context = Context.Any,
        ChangesState = false
    };

    public IEnumerable<PatchBlueprint> Describe(PatchBlueprintRequest request)
    {
        request.Cancellation.ThrowIfCancellationRequested();
        yield return new PatchBlueprint {
            Id = "my_module.startup_prefix",
            Name = "Startup screen prefix",
            Hook = PatchHookKind.Prefix,
            Target = new MethodReference {
                AssemblyName = "TaleWorlds.MountAndBlade",
                DeclaringType = "TaleWorlds.MountAndBlade.MBSubModuleBase",
                MemberName = "OnBeforeInitialModuleScreenSetAsRoot",
                ParameterTypes = new List<TypeReference>(),
                IsStatic = false
            },
            // Required callback reference. Preflight resolves it but never invokes it.
            PatchMethod = MethodReference.From(typeof(MyPatchBlueprints).GetMethod(
                nameof(DeclaredCallback), BindingFlags.Public | BindingFlags.Static)),
            Priority = 400,
            Before = new List<string> { "another.owner.id" },
            Rationale = "Document the intended startup hook before implementation."
        };
    }

    public static void DeclaredCallback() { }
}

// In a ForgeApi.RegisterWhenAvailable callback:
ForgeApi.PatchBlueprints?.Register(new MyPatchBlueprints());
```

Each blueprint must declare a patch callback reference. `MethodReference.From(MethodBase)` and `TypeReference.From(Type)` are supplied to avoid hand-written signatures. For an explicit reference, use the loaded assembly's simple name, exact declaring type full name, exact member name, and every parameter in order. Every concrete parameter and return `TypeReference` must include both its exact full type name and simple assembly name; an omitted assembly is invalid and is never treated as a wildcard. Generic parameter references use `!n` for the declaring type and `!!n` for the method, scoped to the declaring assembly. `MethodReference.From` normalizes a constructed generic method (including one on a constructed generic declaring type) to its open declaration so that it can round-trip through preflight. Constructors use `PatchMemberKind.Constructor`; a static constructor also needs `IsStatic=true`.

The provider runs only during an explicit preflight request on Forge's game-thread path. Keep it short, return inert DTOs, observe the cancellation token, and do not retain the request, its services, or live Bannerlord objects after returning. Forge bounds a capture to 100 declarations per provider and 200 total declarations.

## What Forge checks

Forge resolves only assemblies already present in `AppDomain.CurrentDomain`. It does not call `Assembly.Load`, scan arbitrary types, use `AccessTools`, or accept reflection targets from pipe input. Resolution uses:

- an exact loaded assembly simple name;
- `Assembly.GetType` with the exact full type name;
- public/non-public, static/instance members declared by that type;
- the exact declared parameter types, generic arity, optional return type, and optional static assertion.

It reports a structural result for every declaration, including exact target or callback resolution, invalid signatures, assembly/type/member not found, duplicate IDs, conflicting target declarations, and ambiguous overloads. It checks generic arity and method shape rather than selecting a similarly named overload by guesswork. Generic type parameters use owner-kind/position tokens (`!0` for a declaring-type parameter and `!!0` for a method parameter), preventing same-name parameters from aliasing. Ordering references are checked for missing IDs, self-references and cycles; preflight reports the declared constraints but never infers a final runtime order.

## Experimental runtime detours

The separate `ForgeApi.Patches` capability provides explicit method-replacement requests and lifecycle handles. Check the optional capability at runtime; `ForgeApi.Version` is a compile-time constant and is not a substitute for checking whether a host supplies that interface. The additional `IForgePatchServiceLifecycle` is optional, preserving existing `IForgePatchService` implementations. Owners and patch IDs are labels for tracking, not security boundaries. Disconnect stops accepting new requests and attempts to revert Forge-owned patches in reverse application order. A handle may still be queried or disposed after disconnect. The built-in service reopens on reconnect only after every prior record and original byte image is verified as reverted and no target detour remains tracked. Reversion succeeds only when the current target bytes still match the exact bytes installed by Forge; if another component changed them, Forge reports a conflict and leaves those bytes untouched.

This low-level backend requires a 64-bit process, remains experimental, and does not guarantee safety when another thread is executing the target while its machine code is being replaced. Writes crossing a system page boundary are rejected before protection changes because the implementation restores one previous protection value per span. Restoring page protections and calling `FlushInstructionCache` are required memory-management steps, but they do not suspend threads, decode instruction boundaries, or make concurrent execution safe. Do not use it in production or claim thread-safe hot patching. Keep the target quiescent in an isolated disposable fixture, and stop if target identity, bytes, page protections, or restoration state is uncertain. The fixture is built as a `net472` library and invoked only through `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat`, which loads it into a temporary x64 Windows PowerShell process; the test does not create or launch `CalradiaForge.DetourFixture.exe`. Blueprint hook kinds that the method-replacement backend does not implement remain declarations only.

## Viewing results

- The native panel exposes **Patch preflight** beside the Modules and Dependencies actions. Hook Workbench is a conditional action surface inside that route; it is shown only while Patch Preflight is selected and the command deck is available. It is not a global overlay and must not hide or replace the section rail or evidence ledger. It uses the normal result ledger and paging.
- The desktop application has a separate **Patch preflight** section. It shows a captured report offline and enables **Check patch blueprints** only for a connected server that advertises the `patch-preflight` capability.
- `patch-blueprints` lists registered provider descriptors. `patch-preflight` captures and evaluates declarations. Both are read-only protocol actions.
- `cf.patch_status [owner]` is a read-only game-console query. `cf.patch_revert <id|owner|all>` is an explicit state-changing console command; it is not exposed as a write-capable IPC action. The native Patch Preflight panel and `patch-blueprints`/`patch-preflight` protocol actions remain read-only.
- Reports preserve the latest explicit preflight capture. Exporting a report does not invoke providers again or repeat any diagnostic capture.

An example provider is in `examples/CalradiaForge.Examples/Examples.cs`. It declares a Bannerlord startup target but does not apply a detour or change the game. See also the [SDK reference](sdk-reference.md) and the [Spanish version](PATCH_BLUEPRINTS.es.md).

## Explicit runtime hooks and IL transpilers

`ForgeApi.Hooks` is the separate optional `IForgeHookService` capability for executable runtime hooks. It is distinct from `IPatchBlueprintRegistry` and `PatchBlueprint`: those describe declarations for read-only preflight and do not install anything. It is also distinct from the experimental `ForgeApi.Patches` method-replacement API. SDK API 13 adds Finalizer callbacks to the Prefix/Postfix contract. The Bannerlord `net472` backend uses `MonoMod.RuntimeDetour` 25.3.6 for these callbacks and a separate Core-only `ILHook` adapter for local Transpiler registration. The executable backend is absent from Core's `net8.0` target and Desktop. The SDK has no MonoMod types or package dependency; blueprint declarations still do not install executable hooks.

Hook registration is inert. A registered definition keeps its target and delegates in-process; a hook is installed only by an explicit Apply operation and can be verified or explicitly reverted. Owner strings are tracking labels, not authorization boundaries. Prefix, Postfix, and Finalizer callbacks execute synchronously on the same thread that invokes the target method, so callback latency and thread-affinity requirements are inherited from that caller. Dispatch rechecks the host's approved game-thread main-menu gate for each invocation; if it is false or throws, Forge skips runtime callbacks and calls the target without the Forge callback wrapper. The detour itself remains installed after leaving the menu and callbacks resume if the approved context returns, so explicitly revert before leaving when that is the intended lifecycle.

### Finalizer failure handling

Set `ForgeHookDefinition.Finalizer` to a `ForgeHookCallback` and inspect `ForgeHookInvocation.Exception`. With a Finalizer registered, Forge captures failures from Prefix, the original target, or Postfix and runs the Finalizer after that path while the host gate permits callbacks. If Prefix throws, dispatch stops before calling the original target; Finalizer receives that Prefix exception. Postfix runs only after a successful Prefix/original path. An unchanged pending exception is rethrown with `ExceptionDispatchInfo`, preserving its identity and original stack. Assign another exception to replace it, or assign null to suppress it; a successful non-void return requires a `Result` compatible with the target return type. An invalid Finalizer result throws `InvalidOperationException`, retaining the pending failure as its inner exception. If Finalizer throws while an invocation failure is pending, Forge throws `AggregateException` containing both; if no failure was pending, the Finalizer exception propagates directly.

Hooks without Finalizer retain their existing policy: a throwing Prefix falls back to the original arguments and call, a throwing Postfix preserves the original result, and an original-method failure propagates without running Postfix. Every Finalizer also passes the host callback gate; leaving the approved context or disabling callbacks during unload can skip it. It is therefore not an unconditional cleanup guarantee. Snapshots expose `HasFinalizer`; existing snapshot constructor signatures remain available.

### Local IL transpilers

Only `CalradiaForge.Core` compiled for `net472` exposes `ForgeHookService.RegisterTranspiler(ForgeHookDefinition metadata, MonoMod.Cil.ILContext.Manipulator manipulator)`. Metadata supplies the ID, owner, managed target and ordering, and must have no Prefix, Postfix or Finalizer callbacks. Registration returns the normal `IForgeHookHandle` without running the manipulator; Apply, Verify, Revert and disconnect use the shared hook lifecycle and target reservation. Snapshots expose `HasTranspiler`. Neither `ILContext` nor executable delegates cross SDK contracts or IPC.

The manipulator executes during Apply and IL-chain rebuilds, including rebuilds triggered by another `ILHook`; it must tolerate repeated execution. Registration and Patch Blueprint Preflight do not execute it. Initial Apply, Revert management, and reconstruction before an activation has completed an explicitly authorized Apply require the approved host context. After verified Apply, MonoMod may invoke that exact owned activation again for a chain rebuild on another caller's thread, after the host context changes, or after runtime callbacks stop, while the activation remains owned and Undo is not uncertain. This reconstruction is not a new Apply or permission to manage hooks. During unload, a previously applied activation can still be needed for external chain reconstruction; an activation without prior Apply authorization may rebuild only during synchronous owned Undo under the management lock and approved caller thread/context. Reentrant management during the manipulator is blocked. Unlike runtime callbacks, a transformed method body remains active when the caller leaves the menu: invocation has no per-call gate for the installed IL changes. Explicit Undo/Revert removes that transformation; stopping callbacks does not restore the body. Use the menu gate for management and check snapshots after failures instead of assuming the body has been restored.

The host permits hook changes only on Bannerlord's exact main-menu screen, on the game thread, with no campaign, mission, or multiplayer session active. The WPF Hook Workbench reads snapshots, creates an Apply/Revert plan for selected hook IDs, and requires an explicit checkbox plus the plan's single-use confirmation token before committing. Batch Apply is sequential, not atomic: earlier hooks can remain applied when a later hook fails, so reconcile snapshots and explicitly revert what should be removed. Conflict and Failed snapshots can also be selected for a verified Revert attempt from the menu. IPC carries hook IDs for selection and the confirmation token; it never carries callbacks, delegates, `MethodInfo` targets, or executable hook definitions. The available protocol actions are `hook-snapshots`, `hook-verify`, `hook-apply-plan`, `hook-apply-confirm`, `hook-revert-plan`, `hook-revert-confirm`, and `hook-plan-cancel`. If a confirmation response is interrupted or uncertain, refresh snapshots before making another plan; do not retry the consumed token.

The named-pipe ACL is scoped to the current Windows user SID; it does not authenticate that the client is the WPF process. The checkbox and single-use token provide the normal Workbench confirmation flow, but are not a process-identity boundary against another process running as the same user. Treat same-SID local clients as trusted unless a separate host-verifiable client-authorization mechanism is added.

The dependency is pinned to MonoMod.RuntimeDetour 25.3.6, but repository and public advisory checks did not establish a published independent security audit as of 2026-09-30; this is not evidence that a private or unindexed audit does not exist. RuntimeDetour is an in-process code-execution capability, not a sandbox. Only use hooks from trusted extensions. The upstream [RuntimeDetour guide](https://monomod.dev/docs/RuntimeDetour/Usage.html) describes synchronization for changes to its detour chain, while [chain hot-patching notes](https://monomod.dev/docs/RuntimeDetour/implementation/ChainHotPatching.html) describe limits and edge cases; neither proves safety for every host lifecycle or for this experimental backend. See the upstream [security advisories](https://github.com/MonoMod/MonoMod/security/advisories) and [security policy](https://github.com/MonoMod/MonoMod/security/policy).

The game console exposes `cf.hook_status [owner]`, `cf.hook_apply <id>`, `cf.hook_revert <id|owner|all>`, and `cf.hook_confirm <apply|revert> <token>`. Apply and Revert only prepare a server-issued preview; they do not mutate hooks. Review the listed IDs, owners, targets, operation, session, expiry, and single-use token, then explicitly confirm that exact plan with `cf.hook_confirm`. The token expires after 60 seconds and is consumed before mutable preconditions are checked. Confirmation revalidates the exact host session, current main-menu screen and game thread, hook service, and every selected registration/state. A denied, expired, or uncertain confirmation must not be retried; refresh status and prepare a new plan. Revert accepts a registered ID, owner, or `all`, while Apply accepts one registered ID. These explicit runtime commands are separate from the read-only Patch Blueprint Preflight surface. The built-in host enforces the same main-menu/game-thread context for planning and confirmation.

The optional in-game owned-target fixture uses `cf.hook_fixture register` to register `cf.fixture.callback` (Prefix/Postfix/Finalizer) and `cf.fixture.il` without applying either. In the approved main menu, `cf.hook_fixture run` invokes only its own managed target and reports its value, callback deltas and IL rebuild count. Expected values are 10 with neither hook, 11 with callbacks only, 20 with IL only, and 21 with both. To change fixture hooks in the console, prepare with `cf.hook_apply <id>` or `cf.hook_revert <id|owner|all>`, review the preview, then run the exact `cf.hook_confirm <operation> <token>` command printed by the plan. Repeat the fixture run and prepare/confirm a Revert plan to inspect restoration. These are fixture expectations and source behavior, not a recorded live Bannerlord validation result.

The isolated detour fixture is serial. Its passing result and managed tests do not prove safety if another thread is executing a target while a hook is applied or reverted; this backend remains experimental. No live Bannerlord or Modding Kit session was used to validate this hook work, and no asset import was performed.

### Hook benchmark and measurement limits

Run `tools/Benchmark-CalradiaForge-Hooks.bat` to build the disposable `net472` fixture and invoke it through the fixture BAT in a temporary 64-bit Windows PowerShell host. The benchmark is serial and measures five local scenarios: Prefix, Postfix, Prefix+Postfix and Finalizer with no-op callbacks, plus IL-only with a result-preserving inserted Nop and no runtime callbacks. For each scenario it prepares the target method before timing, performs 5,000 warm-up calls, then records five samples of 25,000 calls for the direct path and the hooked path. Its CSV output includes total milliseconds, nanoseconds per call, allocated bytes and bytes per call, Gen0 collections, and a checksum; summaries report p50/p95 for time and bytes per call. It also records the explicit Apply/Revert operation durations and the first call after Apply. Allocation values come from the isolated host's `AppDomain` counters.

One earlier single-run baseline, before the proposed optimization, showed direct-call p50 around 3.6–3.7 ns/call with 0 B/call, and no-op hooked-call p50 around 656–774 ns/call with about 401 B/call. Treat these as harness measurements and an indicative baseline, not as a repeated benchmark result or evidence of a later improvement. The fixture does not measure Bannerlord latency, frame time, UI responsiveness, or game-thread impact. It is not a concurrency-safety test: no other thread executes the target during mutation, and these measurements cannot establish safety for live code replacement or prove safety when calls are in flight.

### Lifecycle recovery and callback gates

Hook dispatch checks the host context before callbacks, again after Prefix, and again before Postfix. If the gate closes after Prefix but before the original method, Forge discards the Prefix argument edits/cancellation and calls the original target with its original arguments. If the original method leaves the approved context, Forge returns its result without invoking Postfix. This is a per-stage policy check, not thread quiescence or a guarantee against concurrent code writes.

Hook and patch handle `Dispose()` request a revert and throw `InvalidOperationException` when the service cannot confirm success. Use `Revert()` directly when the caller needs the structured result. `Conflict` recovery never overwrites foreign bytes: the patch service can resume a revert only after the current bytes exactly match Forge's recorded installed image or the verified original image.

During `ForgeApi.Disconnect()` or host replacement, an unresolved patch keeps `ForgeApi.Patches` published for inspection and explicit recovery. When an incoming host lifecycle fails to reconnect after a clean replacement teardown, Forge attempts to reopen the previous host and resumes direct patch applications only if that rollback succeeds. A failed rollback is surfaced as an aggregate exception; refresh snapshots and resolve the retained host before retrying.

The built-in `TestEngine` forwards the optional hook disconnect guard through the published host. `Runtime.Dispose()` disables callbacks and new applications, requests `ForgeApi.DisconnectForUnload()`, and always disposes its named-pipe server in `finally`. A rejected API-level disconnect may retain the published capability; real module unload does not preserve the pipe for recovery. The serial fixture covers API-level rejection followed by an approved revert and clean disconnect, not live module unloading.

### Inventory and verification utilities

`cf.hook_status [owner|-] [target|-] [prefix|postfix|finalizer|transpiler]` filters metadata without applying hooks. `cf.hook_verify <id|owner|all>` checks backend lifecycle state; it does not certify native bytes or concurrent safety. `cf.hook_export` returns JSON to console output. WPF provides owner/target/type filters, retains hidden selections and exports snapshots/results. Gauntlet selects registered IDs only, displays the full plan and requires explicit approval; it calls the runtime coordinator locally without IPC.

`tools/Invoke-CalradiaForge-Hooks.bat` accepts typed Status, Verify, ApplyPlan, RevertPlan, ConfirmApply, ConfirmRevert and Cancel operations with a positive ProcessId. Selections contain 1–32 exact IDs through HookIds or HookIdsJson (a JSON string array), never both. Confirmation requires Token and the Confirm switch; cancellation requires Session and Token. No code, IL or arbitrary target is accepted. ValidateOnly checks arguments without IPC. Limits are 3 seconds for connection/write, up to 15 seconds for reading and 65,536 response bytes; there are no automatic retries. Reconcile uncertain outcomes through status before making another plan.

A failed IL Undo can clear the backend IsApplied marker before the rebuild finishes, leaving the transformed body installed. Forge retains the conflict, handle and reservation and requires host restart; a second revert cannot certify restoration from that marker alone. Shutdown closes runtime callbacks and new applications. An activation that completed explicit Apply may still be invoked for external chain reconstruction while it remains owned and Undo is not uncertain, even after callback shutdown or when the host gate is false. A never-authorized activation may rebuild only during synchronous owned Undo under the management lock and approved caller thread/context. These rules preserve the installed chain; they do not certify general concurrent target safety.
