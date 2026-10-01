# Rev109 — Finalizer, IL Transpiler, and Explicit Hook Confirmation

**Date:** October 1, 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** SDK, Core, Mod, Gauntlet, Desktop, tests, documentation, and release validation.

## Observed problem and technical justification

The experimental hook workbench needed explicit Finalizer and IL-transpiler behavior, while the console Apply/Revert commands did not share the preview-and-confirmation path used by WPF and Gauntlet. An independent review confirmed that console commands could call the service directly. The intended contract requires local callbacks, explicit host-controlled mutation, recoverable verification, and no assertion that serial fixtures make in-process patching safe during concurrent target execution.

## Technical solution and architectural decisions

- The optional SDK hook capability now includes Finalizer metadata and invocation exception state. A pending Prefix, original-method, or Postfix exception is rethrown with `ExceptionDispatchInfo` unless Finalizer explicitly replaces or suppresses it. Suppressing an exception for a non-void target requires a result assignable to its return type. An invalid Finalizer result throws `InvalidOperationException`; a Finalizer failure is propagated directly when no prior failure exists and aggregated with a pending failure otherwise. Postfix still runs only after preceding phases complete successfully; hooks without Finalizer retain their prior failure behavior.
- With Finalizer installed, a Prefix callback exception stops dispatch before the original target is called; Finalizer receives that exception. This is covered by the serial fixture and is distinct from the legacy Prefix/Postfix path, where a Prefix failure falls back to the original call.
- The Bannerlord `net472` Core adapter uses MonoMod.RuntimeDetour 25.3.6 `ILHook` for local `ILContext.Manipulator` delegates. SDK, Desktop, IPC, and Patch Blueprint preflight do not receive or execute transpiler delegates. A manipulator can be invoked during Apply and again when MonoMod rebuilds the IL chain, so it must tolerate repeated execution. IL transforms are experimental, in-process, and can remain effective outside the menu after installation until explicitly undone.
- Hooks and ILHooks share destination reservations, snapshots, lifecycle checks, and recoverable conflict handling. Apply/Revert remains limited to the exact approved main-menu context and game thread. Owner strings are labels, not authorization; the named-pipe SID ACL is not process identity authentication.
- Console Apply/Revert is now a two-phase flow. `cf.hook_apply <id>` and `cf.hook_revert <id|owner|all>` request a preview only. The console displays the selected registered IDs, owners, targets, session, expiry, and one-use token. `cf.hook_confirm <apply|revert> <token>` sends the matching confirmation action to the Runtime, which consumes and validates the token and rechecks the exact session, screen/thread context, hook service, and snapshots. No callback, IL body, or arbitrary target is accepted over IPC.

## Code, assets, dependencies, and validation

- `ForgeApi.Version` is 13; public hook capability remains optional and separate from registry contracts. MonoMod 25.3.6 remains confined to the Bannerlord `net472` backend; no package version or product version was changed.
- WPF and Gauntlet Hook Workbench labels/help were synchronized across the supported language catalogs. The console utility was documented in English and Spanish. Existing Blueprint preflight stays read-only.
- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --no-pause --render-output artifacts\hook-final-render.json <nul"` exited 0: Release build had 0 warnings and 0 errors; asset fixtures 12/12; Core 404/404; ForgeWeave 73/73; Desktop 65/65; WPF render 294 cases. The final render harness reported 13,960 ms total, 182 render/layout passes, and 6,154.6 ms in layout calls. These are harness measurements, not live Desktop interaction latency.
- The integrated launcher hosted the serial x64 detour fixture through its BAT path. It passed Finalizer failures, suppression/replacement, IL transform/order/rebuild/coexistence, guarded lifecycle, conflict recovery, and exact restoration; `DetourFixture.exe` was not launched directly.
- `cmd.exe /c "tools\Test-CalradiaForge-HookUtility.bat"` exited 0 with 29 argument-validation cases and 22 isolated transport cases. The pipe fixture verifies request mapping/serialization and simulated invalid/expired-token responses; it does not invoke Bannerlord Runtime's token clock, session, screen, or context validation. `cmd.exe /c "tools\Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause <nul"` exited 0 with source-sprite/SpriteData/prefab structural checks and nested serial fixture passing. `cmd.exe /c "tools\Verify-CalradiaForge-StatelessBehavior.bat <nul"` exited 0 with all four acceptance gates and a clean build.
- The TPAC deep parser was skipped because `TpacTool.Lib.dll` or its local Native fixture was unavailable. Source and structural checks do not prove Resource Browser import or in-game rendering.

## Evidence limits

The live attempt reached the Bannerlord main menu, but pressing F10 once did not show the Calradia Forge panel. The WPF Hook Workbench reported that hook operations were unavailable in the current game context. No hook was applied, verified, or reverted in the live host; no campaign or battle was opened. Live menu operation and a registered in-game fixture remain pending. Passing BAT fixtures are serial and do not prove safety if another thread executes the target during patch installation or removal. The backend remains experimental.

This annex adds evidence without replacing previous revisions, public contracts, command routes, or the product version.
