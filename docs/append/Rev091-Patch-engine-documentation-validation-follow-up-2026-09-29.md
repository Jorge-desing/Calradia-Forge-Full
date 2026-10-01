# Rev091 — Archival source reconciliation

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Archival source backfill from the published changelog; no code or runtime changes.

**Archival reconciliation note:** This source annex was reconstructed on 2026-09-30 from the already-published English and Spanish changelog sections. No original source annex for this revision existed in `docs/append/`. This backfill does not modify or replace any pre-existing protected DOCX or integrity record.

## Calradia Forge patch-engine documentation and validation follow-up — 2026-09-29 (Rev091)

- Clarifies the Gauntlet copy and architecture maps: Patch Blueprint Preflight is a read-only structural review of declared target and callback references against already-loaded assemblies. It neither applies patches nor invokes callback code, and its resolution does not prove a native replacement can be installed. The Harmony Atlas is a separate read-only inventory of Harmony patches already present in loaded assemblies.
- Corrects the `ForgeDetour` reference to its actual `MethodInfo` replacement API and experimental scope. The module lifecycle and diagrams now show that startup does not scan or apply patches and that preflight is separate from explicit replacement requests.
- Regenerates all 13 native language catalogs with 710 matching keys. Localization audit passed, including the Gauntlet Page Blueprint label, description, and Page title.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` passed: full solution build with 0 warnings and 0 errors; Core 369/369; isolated serial x64 detour fixture `15 → 32 → 15` with exact revert; ForgeWeave 73/73; Desktop 63/63; and 292 WPF render cases. No Bannerlord or Modding Kit session was started.
- The fixture invokes the target serially and does not establish safety while another thread may execute a target during a memory write. The detour backend remains experimental. Product version remains 25.2.0, `ForgeApi.Version` remains 11, and no IPC write surface, ForgeWeave behavior, or distribution ZIP changed.
