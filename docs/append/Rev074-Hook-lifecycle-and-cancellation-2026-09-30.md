# Rev074 — Hook lifecycle and host-context safeguards

**Date:** 30 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** SDK, Core, Mod, Desktop, IPC protocol, and documentation. Hardens the Prefix/Postfix service lifecycle and makes operator plan cancellation explicit.

## Observed problem and technical justification

The hook service could be disconnected while active or uncertain hooks remained outside the approved game context, separating SDK state from detours that were still installed. A `Verify` status-reader exception was also not guaranteed to remain visible as a conflict. In addition, the workbench discarded its local plan during snapshot refresh without first invalidating the host's pending token.

The menu gate must identify the game's official screen by type identity rather than a name or prefix that an extension could imitate.

## Technical solution and architectural decisions

Added the optional `IForgeHookServiceDisconnectGuard` capability. `ForgeApi` consults it before replacing or removing the published service. The service rejects the transition when applied, conflicted, or failed hooks remain outside the approved context. `Verify` records status-reader exceptions as `Conflict`; when external removal is verified, the RuntimeDetour handle is disposed before releasing the target reservation.

The gate resolves `TaleWorlds.MountAndBlade.GauntletUI.GauntletInitialScreen` from `TaleWorlds.MountAndBlade.GauntletUI` and compares the exact `Type` object. Resolution failure closes the gate. The operation remains restricted to the game thread, exact main menu, and absence of a campaign, mission, or multiplayer session.

Added `hook-plan-cancel`, which carries only the exact host session and token. WPF keeps the preview until it receives a valid response confirming cancellation; a missing, invalid, cross-session, or negative response retains the plan and stops snapshot refresh. No automatic retry is performed.

## Changes to assets, code, and dependencies

- Added the optional disconnect preflight interface and integrated it with `ForgeApi` lifecycle transitions without adding members to `IForgeRegistry`.
- Corrected the shared `ForgeHookService` compilation for `net472` and `net8.0`; the executable backend remains limited to Bannerlord `net472`.
- Added request/result DTOs and the `hook-plan-cancel` protocol action carrying only session/token, plus asynchronous cancellation state and session validation in the Hook Workbench.
- Updated `docs/SDK.md` and created its `docs/SDK.es.md` counterpart; the English/Spanish SDK reference retains contract 12.
- Registered-target APIs, hook confirmation protocol, product version 25.2.0, and ZIPs remain unchanged.

## Validation and evidence limits

- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"`: Core 384/384 and ForgeWeave 73/73; `net472` and `net8.0` builds had no warnings or errors.
- `cmd.exe /c "tests\CalradiaForge.DetourFixture\Run-DetourFixture.bat --no-pause <nul"`: serial x64 fixture passed unsafe-disconnect rejection, external removal, Verify exceptions, backend exclusion, and `15 → 32 → 15` restoration. The fixture `.exe` was not run directly.
- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause <nul"`: Desktop 65/65; WPF render 292/292 in 13,895 ms of harness time.
- Bannerlord and the Modding Kit were not opened. Serial tests do not prove safety if another thread executes the target during Apply or Revert; the backend remains experimental.

This annex adds evidence without rewriting earlier revisions.
