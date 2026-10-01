# Rev086 — Archival source reconciliation

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Archival source backfill from the published changelog; no code or runtime changes.

**Archival reconciliation note:** This source annex was reconstructed on 2026-09-30 from the already-published English and Spanish changelog sections. No original source annex for this revision existed in `docs/append/`. This backfill does not modify or replace any pre-existing protected DOCX or integrity record.

## Calradia Forge 25.2.0 patch-engine source follow-up — 2026-09-29 (Rev086)

- Removes implicit patch scanning from module startup; the legacy `InitializeGlobalPatches()` entry remains an obsolete no-op. `ForgePatcher.ApplyAll(assembly)` is explicit and validates the supplied patch batch before writing. Patch Preflight remains read-only and resolves declared targets/callbacks, duplicate IDs, conflicts, and ordering without loading assemblies or invoking callbacks.
- Adds optional `IForgePatchService` through `ForgeApi.Patches`, advancing `ForgeApi.Version` from 10 to 11 without changing `IForgeRegistry` implementers. `ForgeDetour`, `MethodSwapper`, and patch records share one write/verification path. Reversion checks exact bytes, reports foreign modifications as conflicts, and requests instruction-cache flushing while checking executable-page protection changes. Adds `cf.patch_status [owner]` and `cf.patch_revert <id|owner|all>`; Patch Preflight over IPC remains read-only.
- The backend remains experimental: it does not suspend threads, decode or relocate overwritten instructions, or guarantee safety while a target method is executing. No Bannerlord or Modding Kit runtime test was performed for this entry. The disposable x64 fixture and its BAT integration are present, but BAT execution/results and full regression results remain pending; no test pass is claimed here.
- Product version remains 25.2.0; ForgeWeave and distribution ZIPs are unchanged.
