# Rev047 — WPF status and command hierarchy refinement

**Date:** 27 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Standalone Desktop WPF header, work-order status cards, theme resources, and off-screen render coverage.

## Observed issue and technical rationale

The header gave the command palette roughly the same visual weight as secondary controls, while the active-tool status card was the only status card without a clear localized label. Evidence export also used a shorter target than the primary header action. Status icon opacity was fixed rather than tuned for the three palettes, so High Contrast did not use the stronger treatment selected for decorative details.

## Technical solution and decisions

The existing command-palette action now uses a brass primary surface, a local search glyph, and a 40-DIP minimum activation height while retaining its original automation ID, localized accessible name, command, and route behavior. The five status cards have consistent local semantic glyphs; the active-tool card has a localized kicker and stronger brass border. A per-theme `Token.StatusIconOpacity` keeps War Table subdued, raises Parchment Light slightly, and makes the glyphs clearly visible in High Contrast. The icons are explicitly non-hit-testable and non-focusable. The evidence-export target is 30 DIP high.

The render harness now checks the localized labels, primary action size/name/icon, passive behavior, and the configured icon opacity for each theme. All 13 resource catalogs contain `Ui.ActiveTool`.

## Assets, code, and dependencies

- Updated the Desktop header and status control XAML, theme dictionaries, the localized resource catalogs, and render assertions.
- No image assets or dependencies were added. Existing local Game-icon geometry is reused.
- Product version remains 25.2.0. No public API, route, command, permission, IPC contract, or ZIP changed.

## Validation and evidence limits

- Baseline BAT: `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-visual-rev067-baseline.json"` — build clean; 59 Desktop tests; 281 render cases; 158 layout passes; 10,893 ms overall and 2,564 ms in layout calls.
- Final BAT: `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-visual-rev067-final.json"` — build with zero warnings/errors; Desktop 59/59; 285 render cases; 158 layout passes; 8,681 ms overall and 2,013.7 ms in layout calls.
- These are one-run harness measurements, not open-application interaction latency or a performance claim. Off-screen War Table and High Contrast screenshots were reviewed; the live WPF application, OS input, and actual Windows DPI were not tested.
- No Bannerlord session, campaign, or battle was started.

This appendix adds evidence to the previous revision without replacing its paragraphs. Source changelog Rev067 and protected Register appendix Rev047 are independent counters.
