# Rev090 — Archival source reconciliation

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Archival source backfill from the published changelog; no code or runtime changes.

**Archival reconciliation note:** This source annex was reconstructed on 2026-09-30 from the already-published English and Spanish changelog sections. No original source annex for this revision existed in `docs/append/`. This backfill does not modify or replace any pre-existing protected DOCX or integrity record.

## Calradia Forge fixture-launcher status correction — 2026-09-29 (Rev090)

- A PowerShell timeout wrapper around the disposable native fixture did not return control reliably and was removed. The fixture BAT is restored to direct child-process launch; no timeout guarantee is claimed.
- The current native smoke remains blocked/unverified. The latest managed Core regressions pass 363/363 and the x64 fixture compiles cleanly, but the Core BAT does not complete because its native child stalls. Earlier successful fixture evidence remains scoped to the earlier tree recorded in Rev088.
- This entry corrects only the timeout statement in Rev089. No module/game session or ZIP change occurred; product version remains 25.2.0 and the patch backend remains experimental.
