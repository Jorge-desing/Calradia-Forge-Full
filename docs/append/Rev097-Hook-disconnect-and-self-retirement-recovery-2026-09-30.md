# Rev097 — Recoverable hook disconnect and callback self-retirement

**Date:** September 30, 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Core, Mod, SDK host integration, disposable detour fixture, and bilingual hook documentation.

## Observed problem and technical rationale

The published `TestEngine` exposed `IForgeHookService` but did not forward its optional disconnect guard. `ForgeApi.Disconnect()` could therefore begin host teardown before the inner hook service rejected removal of an active hook. `Runtime.Dispose()` also closed the named-pipe server before asking the SDK to disconnect, removing the workbench recovery path. A separate fixture review found that a Prefix/Postfix callback could revert or dispose its own hook while its current detour invocation still needed MonoMod's original trampoline; disposing the `Hook` immediately invalidated that in-flight call.

## Technical solution and architectural decisions

`TestEngine` now forwards `IForgeHookServiceDisconnectGuard` and keeps an explicit public parameterless constructor alongside the gate-aware constructor. A direct `ForgeApi.Disconnect()` rejected while `Runtime` is still ticking leaves the API published and the pipe available for recovery; logging still persists in `finally`. During actual `Runtime.Dispose()` / `OnSubModuleUnloaded`, the pipe server is closed in `finally`, so pipe-based recovery is not available after unload. That unload-rejection path remains unverified in a live host.

Each applied hook uses a distinct dispatch activation that owns its hook object, typed original invoker, and dispatch route. Dispatch enters that activation before invoking callbacks and exits in `finally`. A revert requested during an active dispatch removes the route for future calls and returns pending without attempting or claiming `Undo`; after dispatches drain, the service rechecks the host gate, then performs and verifies `Undo`, disposes the hook, and releases the target reservation. If the gate has closed, cleanup remains pending and retained for an explicit retry. A failed deferred dispose remains retained and visible as a conflict; a replacement activation cannot overwrite it. Prefix and Postfix self-retirement fixtures check that the in-flight call completes, the callback is not run on later calls, and future target calls use the original method.

The management gate remains the exact Bannerlord main-menu screen on the game thread, with no campaign, mission, or multiplayer session. MonoMod RuntimeDetour synchronization applies to its own chain behavior; this serial fixture does not certify the full host lifecycle or all possible concurrency. The raw `ForgeDetour` backend remains distinct. The named-pipe ACL is limited to the current Windows SID, not the WPF process identity; the checkbox and single-use token are confirmation UX and do not authenticate a same-user client process.

## Code, assets, and dependency changes

The changes are limited to `TestEngine`, runtime teardown ordering, the hook dispatch activation lifecycle, the serial `net472` x64 fixture, and English/Spanish documentation. No new dependency, SDK contract, IPC action, game asset, or package was added. The `TestEngine` parameterless constructor, product version 25.2.0, and `ForgeApi.Version` 12 are preserved.

## Validation and evidence limits

- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` built the net472 and net8.0 Core dependency graphs with zero warnings and errors; Core passed 395/395 and ForgeWeave passed 73/73.
- The launcher built and ran the serial x64 detour fixture through its `.bat` and temporary PowerShell host, including API disconnect rejection/recovery and Prefix/Postfix self-retirement. No test executable was started directly.
- No live `Runtime.Dispose()`/module-unload pipe test, live Bannerlord or Modding Kit session, campaign, battle, or ZIP generation was performed. The fixture does not establish general concurrent-target safety or certify the host lifecycle.

This addendum extends Rev096 without replacing its paragraphs. It preserves existing commands, routes, API version, and product version.
