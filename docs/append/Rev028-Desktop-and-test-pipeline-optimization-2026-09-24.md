# Rev028 — Desktop and test-pipeline optimization

**Date:** 2026-09-24  
**Release line:** Calradia Forge 22.0.0  
**Scope:** WPF analyzer responsiveness, embedded texture resources, and bounded test orchestration.  
**Distribution:** Source follow-up only; no package changes.

## WPF workbench

The bounded file analyzer already ran away from the WPF dispatcher. This revision also projects its bounded evidence rows and formats the report on that worker task, preserving cancellation checks before and after analysis. The UI thread receives only the completed result object; the report and evidence DTOs remain separate from WPF controls.

The runtime assembly now embeds compact local texture derivatives instead of the full-size authoring PNGs. The deterministic authoring generator crops the two shallow title-bar strips and reduces reserved-margin ornaments to reviewed display dimensions; material backgrounds retain their approved dimensions. The 11-resource texture set is 6,901,545 compressed bytes and 11,489,464 decoded image bytes, compared with 17,864,675 and 64,474,344 for the original set. This reduces packaged texture data by about 61% and decoded bitmap data by about 82%. These figures describe the texture set, not process memory or startup duration. The `--check` generation mode reproduced every derivative byte-for-byte. Full-size artwork remains available as source input and is not embedded in the WPF assembly.

The render-resource test now treats a missing unoptimized pack URI as the expected result and still fails if authoring masters are accidentally embedded. The optimized pack URIs, dimensions, alpha mode, hashes, aggregate packaged size, and decoded-size limits are checked against the runtime resources.

## Test pipeline

The API test replaces its fixed 500 ms wait with a bounded readiness poll (10 ms interval, 2 s maximum), disposes the client, and keeps failures actionable. Across five runs its median fell from 2.72 s to 2.28 s. Asset Batch and Resource Browser tests no longer start a PowerShell child process for every fixture: script entry points can return composable terminating errors, while the BAT wrapper's non-zero error path remains covered. Measured Asset Batch time fell from 14.699 s to 3.446 s; Resource Browser fell from 22.390 s to 3.943 s. The Game Icons validator now parses the prefab once, with a test guarding that invariant. Asset pipeline PNG fixtures use unique temporary directories rather than shared, fixed artifact names.

The main test runner's `--core-only` mode builds the Core and ForgeWeave test dependency graphs instead of the full solution while continuing to execute both suites. One measured run fell from 7.319 s to 6.115 s; the default runner still builds and executes the full selection.

## Validation and limits

The final full `.bat` run built with zero warnings and errors. AssetPipeline, AssetBatchPlan, ResourceBrowser and FBX preflight checks passed; Core passed 232/232, ForgeWeave 31/31, Desktop 42/42 and WPF render 268/268. The full batch took 34.25 s, including a 9.866 s render harness run with 507 layout passes. These are repeatable test-harness observations, not a measurement of WPF runtime input latency. Desktop test/render builds also passed with no compiler warnings or errors. No live Bannerlord game session, campaign, battle, TPAC import, ZIP regeneration, or installed-asset change was performed.

This entry is append-only and does not revise Rev027 or earlier evidence.
