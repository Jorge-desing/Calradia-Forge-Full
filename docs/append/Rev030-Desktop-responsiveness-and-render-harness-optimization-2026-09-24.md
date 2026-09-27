# Rev030 — Desktop responsiveness and render-harness optimization

**Date:** 2026-09-24  
**Release line:** Calradia Forge 22.0.0  
**Scope:** WPF responsiveness instrumentation, asynchronous metric correctness, and representative render coverage.  
**Distribution:** Source follow-up only; no package changes.

## WPF operation measurements

This revision focuses measurement on startup, tool filtering, route selection, and asynchronous work so optimization can follow observed costs instead of assumptions. The frame-time reference for flagging a slow interaction is 16.7 ms at 60 Hz. Harness timings are kept distinct from latency measured in the running application.

Allocation counters from `GC.GetAllocatedBytesForCurrentThread` are thread-local. They can describe a synchronous scope on the same thread, but they cannot accurately describe an operation that crosses `await` and may resume on another thread. Asynchronous measurements therefore retain elapsed duration and report allocated bytes as unavailable; synchronous measurements may continue to report bytes.

## Render-harness coverage

The tool catalog contains 194 routes that share a workbench page template. The render suite continues to validate every route, icon, binding, selection, and release path, while full WPF layout is reserved for representative tool kinds and scenarios that exercise distinct visual states. The retained render matrix covers long labels, empty and populated evidence, themes, supported languages, and 100–200% scale factors. This removes redundant layout work without reducing route-level contract checks.

The harness records startup, filtering, navigation, asynchronous operations, and render duration separately. Filter refresh measured p50 0.08 ms and p95/max 9.28 ms, with no samples at or above 16.7 ms. Route selection measured p50 0.62 ms and p95 1.16 ms; one of 194 selections crossed 16.7 ms (maximum 23.46 ms), so the threshold remains a diagnostic flag rather than an app-wide latency claim. These synthetic measurements do not represent end-to-end latency in a running user session.

Five render-BAT runs completed in 5.675, 5.677, 5.749, 5.703, and 5.738 seconds. Their 5.703-second median is below the approved 9.0-second target and 46.0% below the 10.559-second baseline. The latest verified run built with zero warnings/errors, passed Desktop 51/51 and 271 WPF render/resource cases, and used 146 representative layout passes. The test report explicitly scopes timings to the harness; it is not a live product-latency measurement.

## Regression and cleanup scope

Regression coverage includes fast and empty filtering, route navigation and page disposal, asynchronous duration/allocation semantics, cancellation, and ledger states. Clearing the filter now releases the selected page and clears its content; a disposed page ignores late async success, cancellation, or error completion. No additional production-code deletion was made without a verified unused-code finding. Any future source removal requires checking C# and XAML references, dynamic resources, reflection, tests, and packaging before deletion. The planned WPF UI Automation smoke check was not run: the tool policy rejected the isolated launch/inspection command, so no current shell-window result is claimed.

## Validation and limits

The build, Desktop tests, render tests, 13-language parity, three-theme scale matrix, and five-run comparison passed through repository `.bat` files. The harness covers scales 100%, 125%, 150%, and 200%; the route, language, and theme suites passed. No test `.exe` or `.dll` was launched directly. UI Automation smoke is not run, so startup/window identification in a separate process remains unverified. Runtime responsiveness remains distinct from harness timings. The application stays on `net8.0-windows`; public API, IPC, product version, game resources, ZIPs, and Bannerlord runtime behavior are unchanged.

This entry is append-only and does not revise Rev029 or earlier evidence.
