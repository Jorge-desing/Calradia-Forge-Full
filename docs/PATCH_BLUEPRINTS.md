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

- The native panel exposes **Patch preflight** beside the Modules and Dependencies actions. It keeps the existing eight-section rail and uses the normal result ledger and paging.
- The desktop application has a separate **Patch preflight** section. It shows a captured report offline and enables **Check patch blueprints** only for a connected server that advertises the `patch-preflight` capability.
- `patch-blueprints` lists registered provider descriptors. `patch-preflight` captures and evaluates declarations. Both are read-only protocol actions.
- `cf.patch_status [owner]` is a read-only game-console query. `cf.patch_revert <id|owner|all>` is an explicit state-changing console command; it is not exposed as a write-capable IPC action. The native Patch Preflight panel and `patch-blueprints`/`patch-preflight` protocol actions remain read-only.
- Reports preserve the latest explicit preflight capture. Exporting a report does not invoke providers again or repeat any diagnostic capture.

An example provider is in `examples/CalradiaForge.Examples/Examples.cs`. It declares a Bannerlord startup target but does not apply a detour or change the game. See also the [SDK reference](sdk-reference.md) and the [Spanish version](PATCH_BLUEPRINTS.es.md).

## Explicit runtime Prefix/Postfix hooks

`ForgeApi.Hooks` is the separate optional `IForgeHookService` capability for executable runtime hooks. It is distinct from `IPatchBlueprintRegistry` and `PatchBlueprint`: those describe declarations for read-only preflight and do not install anything. It is also distinct from the experimental `ForgeApi.Patches` method-replacement API. The current executable hook backend supports only Prefix and Postfix through `MonoMod.RuntimeDetour` 25.3.6 in the Bannerlord `net472` host. It is not used by Core's `net8.0` target or Desktop. Other hook kinds, including Transpiler and Finalizer, remain descriptive declarations and cannot be applied by this backend.

Hook registration is inert. A registered definition keeps its target and delegates in-process; a hook is installed only by an explicit Apply operation and can be verified or explicitly reverted. Owner strings are tracking labels, not authorization boundaries. Prefix and Postfix callbacks execute synchronously on the same thread that invokes the target method, so callback latency and thread-affinity requirements are inherited from that caller. Dispatch rechecks the host's approved game-thread main-menu gate for each invocation; if it is false or throws, Forge skips both callbacks and calls the original target. The detour itself remains installed after leaving the menu and callbacks resume if the approved context returns, so explicitly revert before leaving when that is the intended lifecycle.

The host permits hook changes only on Bannerlord's exact main-menu screen, on the game thread, with no campaign, mission, or multiplayer session active. The WPF Hook Workbench reads snapshots, creates an Apply/Revert plan for selected hook IDs, and requires an explicit checkbox plus the plan's single-use confirmation token before committing. Batch Apply is sequential, not atomic: earlier hooks can remain applied when a later hook fails, so reconcile snapshots and explicitly revert what should be removed. Conflict and Failed snapshots can also be selected for a verified Revert attempt from the menu. IPC carries hook IDs for selection and the confirmation token; it never carries callbacks, delegates, `MethodInfo` targets, or executable hook definitions. The available protocol actions are `hook-snapshots`, `hook-apply-plan`, `hook-apply-confirm`, `hook-revert-plan`, `hook-revert-confirm`, and `hook-plan-cancel`. If a confirmation response is interrupted or uncertain, refresh snapshots before making another plan; do not retry the consumed token.

The named-pipe ACL is scoped to the current Windows user SID; it does not authenticate that the client is the WPF process. The checkbox and single-use token provide the normal Workbench confirmation flow, but are not a process-identity boundary against another process running as the same user. Treat same-SID local clients as trusted unless a separate host-verifiable client-authorization mechanism is added.

The dependency is pinned to MonoMod.RuntimeDetour 25.3.6, but repository and public advisory checks did not establish a published independent security audit as of 2026-09-30; this is not evidence that a private or unindexed audit does not exist. RuntimeDetour is an in-process code-execution capability, not a sandbox. Only use hooks from trusted extensions. The upstream [RuntimeDetour guide](https://monomod.dev/docs/RuntimeDetour/Usage.html) describes synchronization for changes to its detour chain, while [chain hot-patching notes](https://monomod.dev/docs/RuntimeDetour/implementation/ChainHotPatching.html) describe limits and edge cases; neither proves safety for every host lifecycle or for this experimental backend. See the upstream [security advisories](https://github.com/MonoMod/MonoMod/security/advisories) and [security policy](https://github.com/MonoMod/MonoMod/security/policy).

The game console exposes `cf.hook_status [owner]`, `cf.hook_apply <id>`, and `cf.hook_revert <id|owner|all>`. These explicit runtime commands are separate from the read-only Patch Blueprint Preflight surface. The built-in host additionally enforces the main-menu/game-thread context at apply and revert time.

The isolated detour fixture is serial. Its passing result and managed tests do not prove safety if another thread is executing a target while a hook is applied or reverted; this backend remains experimental. No live Bannerlord or Modding Kit session was used to validate this hook work, and no asset import was performed.

### Hook benchmark and measurement limits

Run `tools/Benchmark-CalradiaForge-Hooks.bat` to build the disposable `net472` fixture and invoke it through the fixture BAT in a temporary 64-bit Windows PowerShell host. The benchmark is serial and measures three local scenarios—Prefix, Postfix, and Prefix+Postfix—with no-op callbacks. For each scenario it prepares the target method before timing, performs 5,000 warm-up calls, then records five samples of 25,000 calls for the direct path and the hooked path. Its CSV output includes total milliseconds, nanoseconds per call, allocated bytes and bytes per call, Gen0 collections, and a checksum; summaries report p50/p95 for time and bytes per call. It also records the explicit Apply/Revert operation durations and the first call after Apply. Allocation values come from the isolated host's `AppDomain` counters.

One earlier single-run baseline, before the proposed optimization, showed direct-call p50 around 3.6–3.7 ns/call with 0 B/call, and no-op hooked-call p50 around 656–774 ns/call with about 401 B/call. Treat these as harness measurements and an indicative baseline, not as a repeated benchmark result or evidence of a later improvement. The fixture does not measure Bannerlord latency, frame time, UI responsiveness, or game-thread impact. It is not a concurrency-safety test: no other thread executes the target during mutation, and these measurements cannot establish safety for live code replacement or prove safety when calls are in flight.

### Lifecycle recovery and callback gates

Hook dispatch checks the host context before callbacks, again after Prefix, and again before Postfix. If the gate closes after Prefix but before the original method, Forge discards the Prefix argument edits/cancellation and calls the original target with its original arguments. If the original method leaves the approved context, Forge returns its result without invoking Postfix. This is a per-stage policy check, not thread quiescence or a guarantee against concurrent code writes.

Hook and patch handle `Dispose()` request a revert and throw `InvalidOperationException` when the service cannot confirm success. Use `Revert()` directly when the caller needs the structured result. `Conflict` recovery never overwrites foreign bytes: the patch service can resume a revert only after the current bytes exactly match Forge's recorded installed image or the verified original image.

During `ForgeApi.Disconnect()` or host replacement, an unresolved patch keeps `ForgeApi.Patches` published for inspection and explicit recovery. When an incoming host lifecycle fails to reconnect after a clean replacement teardown, Forge attempts to reopen the previous host and resumes direct patch applications only if that rollback succeeds. A failed rollback is surfaced as an aggregate exception; refresh snapshots and resolve the retained host before retrying.

The built-in `TestEngine` forwards the optional hook disconnect guard through the published host. `Runtime.Dispose()` asks `ForgeApi.Disconnect()` to complete before disposing its named-pipe server; if the guard rejects teardown, the API remains published and the pipe remains available for recovery. The serial fixture covers API-level rejection followed by an approved revert and clean disconnect. It does not exercise a live `Runtime.Dispose()` or prove pipe survival during a real module unload.
