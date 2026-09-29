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

Each blueprint must declare a patch callback reference. `MethodReference.From(MethodBase)` and `TypeReference.From(Type)` are supplied to avoid hand-written signatures. For an explicit reference, use the loaded assembly's simple name, exact declaring type full name, exact member name, and every parameter in order. Constructors use `PatchMemberKind.Constructor`; a static constructor also needs `IsStatic=true`.

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

This low-level backend requires a 64-bit process, remains experimental, and does not guarantee safety when another thread is executing the target while its machine code is being replaced. Writes crossing a system page boundary are rejected before protection changes because the implementation restores one previous protection value per span. Restoring page protections and calling `FlushInstructionCache` are required memory-management steps, but they do not suspend threads, decode instruction boundaries, or make concurrent execution safe. Do not use it in production or claim thread-safe hot patching. Keep the target quiescent in an isolated disposable fixture, and stop if target identity, bytes, page protections, or restoration state is uncertain. Blueprint hook kinds that the method-replacement backend does not implement remain declarations only.

## Viewing results

- The native panel exposes **Patch preflight** beside the Modules and Dependencies actions. It keeps the existing eight-section rail and uses the normal result ledger and paging.
- The desktop application has a separate **Patch preflight** section. It shows a captured report offline and enables **Check patch blueprints** only for a connected server that advertises the `patch-preflight` capability.
- `patch-blueprints` lists registered provider descriptors. `patch-preflight` captures and evaluates declarations. Both are read-only protocol actions.
- `cf.patch_status [owner]` is a read-only game-console query. `cf.patch_revert <id|owner|all>` is an explicit state-changing console command; it is not exposed as a write-capable IPC action. The native Patch Preflight panel and `patch-blueprints`/`patch-preflight` protocol actions remain read-only.
- Reports preserve the latest explicit preflight capture. Exporting a report does not invoke providers again or repeat any diagnostic capture.

An example provider is in `examples/CalradiaForge.Examples/Examples.cs`. It declares a Bannerlord startup target but does not apply a detour or change the game. See also the [SDK reference](sdk-reference.md) and the [Spanish version](PATCH_BLUEPRINTS.es.md).
