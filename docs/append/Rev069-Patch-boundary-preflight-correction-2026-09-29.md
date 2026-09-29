# Rev069 — Patch-boundary preflight correction

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** SDK detour preflight, batch registration, regression tests, and fixture BAT.

## Issue found during review

The page-boundary guard originally ran in the low-level writer after a direct patch or batch member had reserved its registry receipt. Its exception therefore left a failed record behind even though no executable memory had been changed. The stale record blocked reuse of that target or patch ID. In a batch, a later cross-page span could also be discovered after the earlier batch members had already been read or reserved.

## Correction

- Direct `Patch` now validates the 13-byte target span under the registry gate, after the host, target, and ID checks but before reading the original bytes or reserving state.
- `PatchBatch` validates every member's page span before reading any original bytes, copying receipts, or publishing registry entries. The low-level writer retains its guard as defense in depth.
- Regression tests use a deterministic synthetic page-size seam through public direct and batch entry points. They verify unchanged fake-adapter read/protect/write/flush counts, no tracked snapshots or legacy receipts, and successful reuse/revert with the same targets and IDs once a permissive page size is set.
- The disposable fixture BAT now bounds the child-process wait so a blocked native smoke does not hold the test launcher indefinitely.

## Validation and evidence limits

- `tools/Run-CalradiaForge-Core-Tests.bat -NonInteractive` passed the managed Core suite: 363/363. Its disposable x64 fixture built for `net472` with zero warnings and zero errors.
- The latest serial native fixture blocked after launch and was stopped. Therefore the latest Core BAT as a whole and the native smoke are not reported as passing; the fixture timeout behavior has not been established as a successful smoke result. The new managed regressions validate the no-write page-rejection state cleanup, not native detour execution.
- No Bannerlord or Modding Kit session was started. The backend remains experimental and does not guarantee safety while another thread may execute a target during a code write.
- This entry corrects the test-status statement for the latest tree only. Rev068 and earlier append-only records remain unchanged. Product version is 25.2.0, `ForgeApi.Version` is 11, and distribution ZIPs are unchanged.
