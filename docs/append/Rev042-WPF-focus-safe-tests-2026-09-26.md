# Rev042 — WPF render tests isolated from user focus

**Date:** 26 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Desktop WPF test infrastructure. Isolated render desktop and read-only UI Automation focus policy.

## Observed problem and technical rationale

Repeated WPF render runs were reported to move focus away from the user's Desktop window. The render harness created native WPF windows, and its foreground guard also retained a last-resort `SetForegroundWindow` restoration path. The optional UI Automation launcher had a similar explicit restoration branch. Even a guarded restoration is an active foreground mutation and makes the test behavior harder to trust.

## Technical solution and architectural decisions

- The render harness creates a dedicated STA worker thread, attaches it to a uniquely named private Windows desktop with `CreateDesktopW` and `SetThreadDesktop`, and only then initializes WPF, windows, or hooks.
- `ForegroundWindowGuard` is now passive and process-scoped. It records when test HWNDs become foreground on the private desktop, but contains no foreground mutation or restoration call.
- The native title-bar state test now checks visibility bindings, styles, and handler presence without maximizing, minimizing, or restoring a window.
- The opt-in UIA runner keeps its prelaunch foreground observer and `WS_EX_NOACTIVATE` check, but no longer calls `SetForegroundWindow`; an observed foreground transition is a failure and is reported without restoration.

## Asset, code, and dependency changes

Updated the render-test foreground guard and runner plus the UIA smoke runner. No production UI behavior, public API, dependency, product version, or ZIP changed. The isolated desktop handle is process-scoped and released by Windows when the test process exits.

## Validation and evidence limits

- Ran the hidden launcher `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output <unique-json-path>` twice. Both builds had zero warnings and errors; Desktop passed 57/57 and WPF render passed 278/278.
- On both runs the interactive foreground HWND was `0x171168` before and after. The final report records 158 layout passes, 2,043.3 ms in layout calls, and 9,492 ms total harness time.
- The render observer saw the test process become foreground only on its unique private desktop. This is an expected internal event and is isolated from the user's interactive desktop.
- The UIA runner passed a PowerShell parse check and a static check for removed focus restoration. Its UIA smoke was not rerun, so no post-change UIA pass is claimed.
- No live visual inspection, game launch, or application-interaction latency measurement is claimed.

This annex adds evidence to the previous protected revision without replacing earlier paragraphs or records.
