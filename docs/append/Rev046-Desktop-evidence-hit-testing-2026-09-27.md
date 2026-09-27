# Rev046 — Precise scope for WPF evidence hit testing

**Date:** 27 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Desktop WPF evidence report and off-screen render assertions.

## Observed issue and technical rationale

The Rev065 documentation described the empty-ledger action as having passed “hit testing” without saying that the assertion runs in an off-screen render host. That wording could be read as evidence of operating-system pointer input or a successful click in the live application. The regression checks only the WPF visual hit surface and its visible ancestors.

## Clarification and validation boundary

- The render harness calls `VisualTreeHelper.HitTest` at the center of the empty-ledger button, verifies that the hit visual belongs to the button subtree, checks visibility and `IsHitTestVisible` on the button's ancestors, and confirms that the command-palette overlay is collapsed.
- The action remains bound to the existing `OpenPaletteCommand`; the adjacent illustration is passive. No route, command, API, permission, dependency, or runtime behavior changed.
- This is off-screen WPF visual-tree evidence. It does not inject operating-system pointer events, perform a real click, or establish behavior in a live WPF window.

## Validation and evidence

- Final post-fix artifact: `artifacts/desktop-visual-report-rev066-review.json` (`passed: true`).
- Build completed with zero warnings and errors; Desktop tests passed 59/59; the render report contains 281 cases and 158 layout passes, including the whitespace-only recommendation regression.
- The JSON records 7,821 ms overall and 1,707.2 ms in layout calls. These are harness measurements, not application interaction latency.
- Operating-system pointer behavior and live WPF behavior remain unverified.

Source changelog Rev066 and protected Register of Improvements appendix Rev046 are independent counters. This appendix records the next protected-ledger revision without changing earlier evidence. Product version remains 25.2.0; existing ZIPs remain unchanged.
