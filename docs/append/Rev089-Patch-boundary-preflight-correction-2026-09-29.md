# Rev089 — Archival source reconciliation

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Archival source backfill from the published changelog; no code or runtime changes.

**Archival reconciliation note:** This source annex was reconstructed on 2026-09-30 from the already-published English and Spanish changelog sections. No original source annex for this revision existed in `docs/append/`. This backfill does not modify or replace any pre-existing protected DOCX or integrity record.

## Calradia Forge patch-boundary preflight correction — 2026-09-29 (Rev089)

- Moves page-span rejection in direct and batch patch routes ahead of any original-byte read or registry reservation. A rejected batch validates every span before reading or reserving any target, so the same IDs and targets remain retryable.
- Adds public-route regressions for direct `Patch` and `ForgePatcher.ApplyAll`, verifying unchanged read/protect/write/flush counts, empty receipts after rejection, and successful reuse under a permitted synthetic page size.
- The latest Core BAT run passed 363/363 managed regressions and compiled the disposable `net472` x64 fixture with zero warnings or errors. The serial native fixture then blocked and was stopped; the latest overall BAT run and native smoke are therefore not reported as passing. The fixture BAT now bounds its child-process wait.
- This follow-up supersedes Rev088's fixture-pass statement for the latest tree only; it preserves Rev088 as the record of the earlier successful run. No Bannerlord or Modding Kit session was started. The detour backend remains experimental and has no concurrent-execution safety guarantee. Product version remains 25.2.0, `ForgeApi.Version` remains 11, and ZIPs remain unchanged.
