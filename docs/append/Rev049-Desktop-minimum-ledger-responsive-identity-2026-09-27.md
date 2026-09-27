# Rev049 — Minimum-window evidence ledger and responsive tool identity

**Date:** 27 September 2026<br>
**Version:** Calradia Forge 25.2.0, unchanged<br>
**Scope:** Standalone Desktop WPF minimum-window layout and render regressions.

## Observed issue and technical rationale

At the 980×680-DIP minimum, the empty evidence-ledger message/action and long tool-category identity text needed explicit bounds checks. Without them, a narrow workspace could clip the empty-state action or let decorative category text stretch the summary card.

## Technical solution and decisions

The render coverage now verifies that the empty-ledger message and action remain within the visible work-area viewport and that the action retains its minimum activation height. The tool identity badge is bounded; long category banners and mottos stay on one line, trim with an ellipsis, provide their complete text in tooltips, and remain inside the badge frame. A separate assertion checks that the compact header remains a single row at minimum width across all 13 supported locales. Existing local-artwork and passive-decoration assertions remain part of the suite.

No API, route, command, permission, IPC contract, runtime dependency, or product behavior was changed. Product version remains 25.2.0 and package ZIPs were not regenerated.

## Assets, code, and dependencies

- Updated the minimum-window render checks for the empty evidence ledger, tool identity badge, long labels, and header row across supported locales.
- No new art assets or runtime dependencies were added by this follow-up.
- Updated the bilingual Desktop documentation and source changelogs append-only.

## Validation and evidence limits

- The supplied final BAT artifact `artifacts/desktop-visual-rev069-final.json` reports zero build warnings/errors, Desktop 59/59, WPF render 286/286, and 172 layout passes.
- That JSON records 11,719 ms total harness time and 3,106.8 ms in layout calls. These are harness measurements, not open-app latency or a performance claim.
- The render artifact covers local presentation and rendered bounds. No runtime UI Automation or live-app validation was performed. This appendix task did not rerun tests.

This appendix adds evidence to the previous revision without replacing its paragraphs. Source changelog Rev069 and protected Register appendix Rev049 are independent counters.
