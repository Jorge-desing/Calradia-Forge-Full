# Rev066 — Explicit experimental patch lifecycle

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** SDK, Core, Mod, documentation. Explicit patch activation, read-only preflight, and reversible detour ownership.

## Observed problem and technical rationale

Patch application must be an explicit operator or caller decision. A module-startup scan obscures when code is modified and can apply declarations before their targets and callbacks have been reviewed. Preflight must report whether declarations resolve without itself loading arbitrary assemblies, invoking callbacks, or changing executable memory. Reversal must preserve changes made by another component instead of blindly restoring a cached prologue.

The implementation remains an experimental development facility. `VirtualProtect` and `FlushInstructionCache` do not prevent another thread from executing a method while its bytes are being changed, and the current backend does not decode or relocate overwritten machine instructions.

## Technical solution and architectural decisions

- `SubModule.OnSubModuleLoad()` no longer invokes a global patch scan. `ForgeBootstrapper.InitializeGlobalPatches()` remains only as an obsolete compatibility no-op. `ForgePatcher.ApplyAll(assembly)` is explicit and validates the complete declared batch before attempting writes.
- Patch Preflight is read-only. It resolves exact target and callback signatures, checks generic arity, duplicate IDs, target collisions, order references and cycles against assemblies already supplied to the operation. It does not load assemblies, invoke callbacks, or apply hooks; declarations for hook kinds unsupported by the backend remain declarations only.
- The SDK adds optional `IForgePatchService` via `ForgeApi.Patches` rather than extending `IForgeRegistry`. `ForgeApi.Version` advances from 10 to 11. The service exposes owner-labelled explicit replacement, immutable snapshots, verification, and idempotent handles for reverting an ID, owner, or all owned patches. An owner string is attribution, not a security boundary.
- `ForgeDetour`, `MethodSwapper`, and patch records use a common detour/write path. The backend checks x64 architecture and supported method signatures, changes executable-page protection for the write, flushes the instruction cache, and verifies the recorded bytes. Revert restores the original bytes only when the current bytes exactly match Forge's installed bytes; foreign bytes are recorded as a conflict and left untouched. Failure handling attempts restoration and records an uncertain/failure state if restoration cannot be verified.
- Disconnect rejects new applications and reverts this service's patches in reverse application order. Handles remain available for state queries and cleanup. Prefix/postfix request events are notifications, not an implicit patching backend. Console commands `cf.patch_status [owner]` and `cf.patch_revert <id|owner|all>` report or explicitly revert patch records; Patch Preflight over IPC remains read-only.

## Changes to assets, code, and dependencies

The source changes are in the SDK patch contracts and detour adapters, Core preflight and service implementation, Mod startup/console commands, and the paired patch-blueprint and SDK-reference documents. No Harmony, MonoMod, Iced, or other patching dependency is introduced. ForgeWeave and the product release version are unchanged.

## Validation and evidence limits

- No automated tests or builds were run while preparing this documentation entry. Required `.bat` validation and disposable x64 fixture coverage remain pending; no passing test count is claimed.
- No Bannerlord or Modding Kit runtime test was run. The source-level change and fake-memory checks, if run later, cannot establish safety against execution concurrent with a memory write.
- The backend does not suspend or coordinate target threads and does not decode or relocate instruction boundaries. It must remain experimental and must not be represented as safe for live methods with possible concurrent callers.

This annex extends the previous registry revision without replacing its paragraphs. Product version remains 25.2.0; the optional SDK capability is versioned at `ForgeApi.Version` 11, while `IForgeRegistry` implementers, ForgeWeave, and distribution ZIPs are unchanged.
