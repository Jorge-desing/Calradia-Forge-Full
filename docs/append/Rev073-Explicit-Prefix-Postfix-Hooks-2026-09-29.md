# Rev073 — Explicit Prefix/Postfix hook service and guarded workbench

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** SDK, Core, Mod, Desktop, dependency notices, and documentation. Adds an optional runtime hook capability and its guarded WPF workbench flow.

## Observed problem and technical justification

Patch Blueprint Preflight is a declarative, read-only description and structural review; it cannot apply a runtime hook. The existing experimental method-replacement API is a separate capability and does not provide the requested Prefix/Postfix lifecycle and operator review flow. Runtime code changes need explicit registration and confirmation and must be restricted to a known game context.

## Technical solution and architectural decisions

Adds the optional `ForgeApi.Hooks` / `IForgeHookService` capability, independent from the declarative blueprint registry and from `ForgeApi.Patches`. The supported executable hook kinds are Prefix and Postfix only, using MonoMod.RuntimeDetour 25.3.6 in the Bannerlord `net472` host. RuntimeDetour is not used by Core `net8.0` or Desktop. Registration is inert; apply, verify, and revert are explicit. Callbacks execute synchronously on the thread invoking the target, and owner names are tracking labels rather than authorization boundaries.

Hook operations are allowed only on Bannerlord's exact main-menu screen, on the game thread, with no campaign, mission, or multiplayer session active. The WPF Hook Workbench reviews snapshots, creates an Apply/Revert plan for selected hook IDs, and requires an explicit confirmation checkbox and a single-use token before committing. IPC transports selected IDs and the token only; delegates, callbacks, `MethodInfo` targets, and executable hook definitions stay in-process. An uncertain or interrupted confirmation must be followed by a fresh snapshot before another plan; the token is not retried. Console commands are `cf.hook_status [owner]`, `cf.hook_apply <id>`, and `cf.hook_revert <id|owner|all>`.

This backend remains experimental. The serial fixture does not establish safety if another thread executes the target while a hook is installed or removed.

## Changes to assets, code, and dependencies

- Added the optional SDK hook contract and built-in host service integration; advanced the SDK capability version as recorded in source.
- Added bounded WPF plan, explicit checkbox/token confirmation, and ID-only IPC actions for hook snapshots, plans, and confirmation.
- Added console status/apply/revert commands and declared MonoMod.RuntimeDetour 25.3.6 with its runtime dependency notices and deployment allowlists for the Bannerlord `net472` host only.
- Updated the English and Spanish patch-blueprint guides to distinguish executable hooks from declarative preflight and method-replacement patches.
- Product version remains 25.2.0. No distribution ZIP was generated or modified.

## Validation and evidence limits

- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: Core 372/372 and ForgeWeave 73/73.
- Core `net472` and `net8.0`, and Mod `net472`, compiled cleanly.
- `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat`: isolated detour fixture passed. The test was not run by directly executing `DetourFixture.exe`.
- The fixture is serial; these results do not prove safety against concurrent target execution during hook installation or removal. No live Bannerlord or Modding Kit session was run, and no Resource Browser import or in-game behavior was verified.

This annex adds evidence to the previous revision without replacing its paragraphs. Existing commands and routes remain unchanged, and the product version remains 25.2.0.
