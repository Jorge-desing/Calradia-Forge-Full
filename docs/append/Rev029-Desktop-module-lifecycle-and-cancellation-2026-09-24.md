# Rev029 — Desktop cancellation, assembly transactions, and game-thread lifecycle

**Date:** 2026-09-24  
**Release line:** Calradia Forge 22.0.0  
**Scope:** Desktop named-pipe cancellation, safe assembly-copy transactions, and game-module extension lifecycle.  
**Distribution:** Source follow-up only; no package changes.

## Desktop cancellation and named-pipe behavior

Cancellation now flows from Desktop session operations through named-pipe connect, reconnect, and request/response work. Connect, reconnect, and response waits remain bounded. A caller cancellation is reported as cancellation, distinct from a timeout or a disconnected endpoint. When an in-flight request is cancelled or times out after transmission begins, the pipe is disconnected so a late response cannot be mistaken for the next request's response. Local named-pipe tests cover these cancellation paths.

## Assembly workbench copy safety

Inspection and version-patch operations accept cancellation while hashing, reading metadata, creating the output, and staging the backup. Inspection hashes the source before and after metadata parsing and stops if the source changed. Backups are copied in bounded chunks while computing SHA-256; the staged backup is committed only after its digest matches the inspected input. The patched assembly is written to a separate temporary output, reopened and validated, and committed only after the last cancellation check.

Cleanup is limited to files created by the current operation. If another process creates the output path before commit, the workbench preserves that competing file. Failed operations remove only a committed output or backup whose hash still matches the data created by this operation. Core and Desktop regression cases exercise pre-commit cancellation, cancellation after backup staging, and an output-path race.

## Game-thread dispatch and queue accounting

Extension page teardown is dispatched to Bannerlord's game thread before it changes Gauntlet state. Trusted completion and teardown callbacks are not silently lost when the bounded extension-request queue is full. The queue's extension-request capacity uses atomic accounting instead of repeatedly counting a concurrent queue, with concurrent producer tests checking that the configured bound remains exact.

## Validation and limits

The complete validation was launched with `tools/Run-CalradiaForge-Tests.bat --no-pause`; no test executable was invoked directly. The solution build completed with zero warnings and errors. AssetPipeline, AssetBatchPlan, ResourceBrowser, TPAC inventory comparison, and FBX preflight checks passed. Core passed 239/239, ForgeWeave 31/31, Desktop 50/50, and the WPF render harness passed 268 cases. The render harness recorded 507 layout passes in 11.334 seconds. Repository code-review and quality scripts reported zero findings for source and tests.

These checks do not measure input latency in the running WPF application or Bannerlord. No game session, campaign, or battle was started. No installed assets, TPAC files, ZIP packages, public API, or release version were changed.

This entry is append-only and does not revise Rev028 or earlier evidence.
