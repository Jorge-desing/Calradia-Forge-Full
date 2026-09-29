# Rev067 — Explicit patch-engine verification follow-up

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** SDK, Core, Mod, and test tooling. Regression coverage for explicit detour application, exact-byte rollback, and host/service lifecycle.

## Observed problem and technical rationale

The Rev066 patch-engine implementation had not yet been exercised by its disposable native fixture or the integrated BAT suite. In addition, lifecycle review identified two cases that needed direct regression coverage: preserving a prior host when reconnect is blocked by foreign target bytes, and distinguishing the reserved `all` command from a patch owner as well as a patch ID.

## Technical solution and architectural decisions

- The x64 detour fixture launcher builds into a unique temporary output directory. It refuses an existing directory and cleans only files in the directory it created, avoiding the delayed visibility of the project-local executable that caused the earlier launcher failure.
- Regression coverage confirms that a conflicted host remains published for manual resolution, its disconnected service rejects new applications, restoring the recorded installed bytes permits explicit handle reversion, and a later host connection then succeeds.
- Console coverage rejects `all` when it collides with an owner, an ID, or both. Batch application rollback, uncertain write status, shared service/direct registry behavior, and preflight callback resolution remain covered.

## Changes to assets, code, and dependencies

The source changes are limited to test-fixture launch behavior and regression tests. No runtime dependency, game module asset, distribution archive, ForgeWeave behavior, or product version changed. `ForgeApi.Version` remains 11.

## Validation and evidence limits

- `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat --no-pause` completed successfully: clean x64 `net472` build and serial target result `15 → 32 → 15` with exact revert.
- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` completed with clean `net472` and `net8.0` builds, 0 warnings, 0 errors, Core 361/361, the isolated native detour fixture passing, and ForgeWeave 73/73.
- No Bannerlord or Modding Kit runtime session was started. The fixture serializes target calls around patch writes; it does not prove safety for concurrent execution. The detour backend remains experimental and does not coordinate other threads or relocate overwritten instructions.

This appendix adds verification evidence after Rev066 without changing earlier paragraphs. Product version remains 25.2.0; ZIPs remain unchanged.
