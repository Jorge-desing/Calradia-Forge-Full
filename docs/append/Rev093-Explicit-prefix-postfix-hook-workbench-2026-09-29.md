# Rev093 — Archival source reconciliation

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Archival source backfill from the published changelog; no code or runtime changes.

**Archival reconciliation note:** This source annex was reconstructed on 2026-09-30 from the already-published English and Spanish changelog sections. No original source annex for this revision existed in `docs/append/`. This backfill does not modify or replace any pre-existing protected DOCX or integrity record.

## Calradia Forge explicit Prefix/Postfix hook workbench — 2026-09-29 (Rev093)

- Adds the optional `ForgeApi.Hooks` / `IForgeHookService` capability for explicitly registered Prefix and Postfix hooks through MonoMod.RuntimeDetour 25.3.6 in the Bannerlord `net472` host. Registration remains inert; callbacks execute synchronously on the thread that calls the target. The hook capability is distinct from declarative Patch Blueprint Preflight and from the separate method-replacement patch API.
- Adds guarded Hook Workbench actions in WPF. The operator reviews snapshots, prepares an Apply/Revert plan, checks the confirmation box, and confirms with a single-use token. IPC accepts selected hook IDs and the token only; callbacks, delegates, and executable targets do not cross the pipe. Apply/Revert is restricted to the exact main-menu context on the game thread with no campaign, mission, or multiplayer session. Console commands are `cf.hook_status [owner]`, `cf.hook_apply <id>`, and `cf.hook_revert <id|owner|all>`.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` passed Core 372/372 and ForgeWeave 73/73. Core `net472`/`net8.0` and Mod `net472` builds completed cleanly. The isolated detour fixture passed through `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat`; `DetourFixture.exe` was not executed.
- The fixture is serial and does not prove safety against a concurrent thread executing the target during hook installation or removal; the backend remains experimental. No live Bannerlord or Modding Kit session or asset import was performed. Product version remains 25.2.0; no distribution ZIP was generated or changed.
