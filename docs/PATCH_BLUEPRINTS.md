# Patch Blueprint Preflight

Patch Blueprint Preflight is a Forge SDK tool for authors who need to describe a proposed method patch before selecting or invoking a patch framework. It is original Forge functionality: it has no build-time or package dependency on Harmony, MCM, ButterLib, or another mod.

The preflight is read-only. It never applies, removes, reloads, unpatches, reorders, or invokes a patch callback. A resolved target proves only that an author-declared signature matched an assembly already loaded in the current game session. It does not prove that a patch framework can apply the patch or that the game behavior will remain compatible.

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
            // Optional metadata for the callback the author intends to use. Forge does not call it.
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

`MethodReference.From(MethodBase)` and `TypeReference.From(Type)` are supplied to avoid hand-written signatures. For an explicit reference, use the loaded assembly's simple name, exact declaring type full name, exact member name, and every parameter in order. Constructors use `PatchMemberKind.Constructor`; a static constructor also needs `IsStatic=true`.

The provider runs only during an explicit preflight request on Forge's game-thread path. Keep it short, return inert DTOs, observe the cancellation token, and do not retain the request, its services, or live Bannerlord objects after returning. Forge bounds a capture to 100 declarations per provider and 200 total declarations.

## What Forge checks

Forge resolves only assemblies already present in `AppDomain.CurrentDomain`. It does not call `Assembly.Load`, scan arbitrary types, use `AccessTools`, or accept reflection targets from pipe input. Resolution uses:

- an exact loaded assembly simple name;
- `Assembly.GetType` with the exact full type name;
- public/non-public, static/instance members declared by that type;
- the exact declared parameter types, generic arity, optional return type, and optional static assertion.

It reports a structural result for every declaration: resolved, invalid declaration, assembly not loaded, type not found, member not found, duplicate blueprint ID, or ambiguous target. It also records self-referential Before/After declarations as review items. It never chooses a similarly named overload by guesswork or infers a final execution order.

## Viewing results

- The native panel exposes **Patch preflight** beside the Modules and Dependencies actions. It keeps the existing eight-section rail and uses the normal result ledger and paging.
- The desktop application has a separate **Patch preflight** section. It shows a captured report offline and enables **Check patch blueprints** only for a connected server that advertises the `patch-preflight` capability.
- `patch-blueprints` lists registered provider descriptors. `patch-preflight` captures and evaluates declarations. Both are read-only protocol actions.
- Reports preserve the latest explicit preflight capture. Exporting a report does not invoke providers again or repeat any diagnostic capture.

An example provider is in `examples/CalradiaForge.Examples/Examples.cs`. It declares a Bannerlord startup target but does not carry a Harmony attribute, create a Harmony instance, or change the game.
