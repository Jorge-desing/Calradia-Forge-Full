# Rev087 — Archival source reconciliation

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Archival source backfill from the published changelog; no code or runtime changes.

**Archival reconciliation note:** This source annex was reconstructed on 2026-09-30 from the already-published English and Spanish changelog sections. No original source annex for this revision existed in `docs/append/`. This backfill does not modify or replace any pre-existing protected DOCX or integrity record.

## Calradia Forge patch-engine verification follow-up — 2026-09-29 (Rev087)

- Completed the isolated x64 detour fixture through `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat --no-pause`. The launcher builds into a unique temporary directory and removes only that output; the serial fixture verified the target result sequence `15 → 32 → 15` and exact revert.
- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` passed with clean `net472` and `net8.0` builds, 0 warnings and 0 errors, Core 361/361, the isolated native detour fixture, and ForgeWeave 73/73. Regressions include explicit batch rollback, conflicted host reconnection/manual recovery, service ownership, byte-state uncertainty, and reserved `all` command collisions.
- This follow-up updates Rev086's pending test status with the completed BAT evidence. No Bannerlord or Modding Kit runtime session was started. Fixtures invoke targets serially and do not prove safety when another thread may execute a target during a write; the patch backend remains experimental.
- Product version remains 25.2.0; SDK capability version is 11. No ForgeWeave, IPC write route, or distribution ZIP changed.
