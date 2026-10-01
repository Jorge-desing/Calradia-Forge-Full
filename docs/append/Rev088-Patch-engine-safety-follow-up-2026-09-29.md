# Rev088 — Archival source reconciliation

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Archival source backfill from the published changelog; no code or runtime changes.

**Archival reconciliation note:** This source annex was reconstructed on 2026-09-30 from the already-published English and Spanish changelog sections. No original source annex for this revision existed in `docs/append/`. This backfill does not modify or replace any pre-existing protected DOCX or integrity record.

## Calradia Forge patch-engine safety follow-up — 2026-09-29 (Rev088)

- Reject detour writes that cross a system page boundary before changing memory protection; add boundary and no-write regressions. `TypeReference.From` and Patch Preflight now preserve and compare generic parameter owner/position (`!0` versus `!!0`) so same-name type and method parameters cannot alias.
- Add the separate optional `IForgePatchServiceLifecycle` capability to reopen a disconnected built-in patch service only after each prior record and original bytes are verified reverted with no tracked target. Keeping it separate avoids adding a required member to existing `IForgePatchService` implementations.
- The integrated BAT retry passed clean `net472`/`net8.0` builds with 0 warnings/errors, Core 363/363, the serial x64 detour fixture (`15 → 32 → 15`, exact revert), and ForgeWeave 73/73. An earlier integrated attempt reported one non-reproducible Core failure; the isolated Core BAT and subsequent integrated run both passed.
- No Bannerlord or Modding Kit runtime session was started. The serial fixture does not establish safety against concurrent execution during a code write; the detour backend remains experimental. Product version remains 25.2.0, `ForgeApi.Version` remains 11, and ForgeWeave, IPC write routes, and distribution ZIPs are unchanged.
