# Rev092 — Archival source reconciliation

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Archival source backfill from the published changelog; no code or runtime changes.

**Archival reconciliation note:** This source annex was reconstructed on 2026-09-30 from the already-published English and Spanish changelog sections. No original source annex for this revision existed in `docs/append/`. This backfill does not modify or replace any pre-existing protected DOCX or integrity record.

## Detour fixture BAT-host correction and patch-copy localization — 2026-09-29 (Rev092)

- The disposable detour fixture now builds as a `net472` x64 library and runs only through `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat`, loaded into a temporary x64 Windows PowerShell process. It no longer creates or starts `CalradiaForge.DetourFixture.exe`; the BAT checks that no fixture apphost was emitted. The test remains aligned with the Bannerlord runtime. A trial under the .NET 8 JIT failed to observe the replacement at stage 4 and was discarded rather than treated as a passing fixture.
- Corrects in-game copy so Patch Blueprint Preflight is clearly structural and read-only, Harmony Atlas remains a separate read-only inventory, and ForgeWeave replay guidance does not imply that preflight applies detours. Seven copy keys now have translations across all 13 native catalogs.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` passed the full build with 0 warnings and 0 errors; ForgeWeave 73/73; Desktop 63/63; and 292 WPF render cases. The BAT-hosted serial x64 fixture returned `15 → 32 → 15` with exact restoration. The 13 native catalogs each contain 737 keys and the localization audit is valid.
- The fixture remains serial and does not establish safety against concurrent execution during machine-code writes. No Bannerlord or Modding Kit session was started, and no package generation was invoked in this correction pass. Product version remains 25.2.0 and `ForgeApi.Version` remains 11.
