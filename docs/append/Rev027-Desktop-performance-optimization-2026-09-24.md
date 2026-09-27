# Rev027 — Desktop performance and test-run optimization

**Date:** 2026-09-24  
**Release line:** Calradia Forge 22.0.0  
**Scope:** WPF workbench collection/search churn, main-rail virtualization, and Desktop test orchestration.  
**Distribution:** Source follow-up only; no package changes.

## WPF workbench

The shell now builds each tool's immutable search text once, skips no-op collection replacements, batches list resets, and reuses group view models while their group remains visible. Pinned and Recent snapshots are cached until their source lists change. The left operational rail uses a recycling virtualized list: the render harness found all 194 tool rows and 11 groups in its data source, while only 10 rows were realized initially and 7 after scrolling to the footer (206 list items including headers and footer). Pinned and Recent retain separate, bounded viewports.

## Test maintenance

The Desktop-only launcher uses `CalradiaForge.Desktop.slnf` to build the two Desktop test projects and their transitive dependencies in one `dotnet build` invocation. It avoids both a solution-wide build and duplicate builds of the shared dependency graph. Source/XAML test inputs and parsed XML are cached for the duration of the unit-test process. The test registry contains 42 active cases; 31 unregistered helpers from obsolete UI contracts were removed after useful assertions were retained in active tests.

The WPF renderer no longer queues an `ApplicationIdle` dispatcher drain for every frame; `UpdateLayout()` runs synchronously on the test's STA thread. Route, localization, theme/scale, accessibility, contrast, empty-state, texture, preference, and viewport checks remain enabled. The complete Desktop-only `.bat` run passed with zero build warnings/errors, 42/42 unit tests, and 268/268 WPF render cases. It completed 507 render/layout passes in 9.822 seconds; the complete batch took 13.974 seconds. The preceding same-turn run took 12.314 seconds for the render harness and 17.874 seconds for the full batch. These are test-harness durations only, not product-runtime performance measurements.

No release version, public API, output package, TPAC, or installed-game asset changed. No campaign or battle was started.

This record is append-only and does not revise Rev021–Rev026 or earlier evidence.
