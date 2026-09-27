# Rev050 — Clarify Windows system-DPI render coverage

**Date:** 27 September 2026<br>
**Version:** Calradia Forge 25.2.0, unchanged<br>
**Scope:** Standalone Desktop WPF render-harness evidence and scale-coverage documentation.

## Observed issue and technical rationale

The render harness produces bitmap previews labeled at 100% and 200%, while its isolated WPF host keeps the same fixed Windows DPI and DIP layout. Those preview labels could be misread as validation at Windows display scaling settings, including 125%, 150%, or 200% system DPI.

## Technical solution and decisions

The documentation now distinguishes bitmap raster density from operating-system DPI. `RenderTargetBitmap` preview factors `[1, 2]` change output pixel density only; they do not change the host window's DPI, DIP dimensions, or WPF layout scale. The supplied scale-audit JSON explicitly reports `windows-system-dpi-layout-coverage` as `not-simulated`, with `appliedWindowsDpiScaleFactors` empty. Therefore Windows system-DPI layout at 125%, 150%, and 200% remains unverified.

The audit retains the existing functional route, locale, theme, and interaction cases; no functional coverage was removed. This correction narrows the evidence claim rather than changing the harness or app.

## Assets, code, and dependencies

- Added an explanatory Rev070 section to the English and Spanish Desktop guides and source changelogs.
- No application code, test implementation, assets, routes, APIs, or dependencies changed in this documentation follow-up.
- Product version remains 25.2.0; ZIPs were not regenerated.

## Validation and evidence limits

- The supplied `artifacts/desktop-visual-rev069-scale-audit.json` reports `passed: true`, Desktop 59/59, 287 WPF render cases, 172 layout passes, and zero build warnings/errors.
- The artifact records 9,321 ms total harness time and 2,134.7 ms in layout calls. These are harness measurements, not open-app latency.
- The JSON marks `windows-system-dpi-layout-coverage` as `not-simulated`; bitmap raster factors `[1, 2]` do not constitute Windows DPI testing. No tests were run during this documentation-only update, and no live app/DPI validation was performed.

This appendix adds evidence to the previous revision without replacing its paragraphs. Source changelog Rev070 and protected Register appendix Rev050 are independent counters.
