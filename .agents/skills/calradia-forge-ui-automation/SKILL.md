---
name: calradia-forge-ui-automation
description: Design, implement, or diagnose a bounded Windows UI Automation smoke check for Calradia Forge's .NET 8 WPF Desktop window. Use for native-window discovery, accessible controls, UIA patterns, bounded waits, and failure evidence; not for Gauntlet, Resource Browser, FBX import, or in-process render tests.
---

# Calradia Forge UI Automation

Use this skill only for an external Windows UI Automation check of `src/CalradiaForge.Desktop/`. Read [calradia-forge-desktop](../calradia-forge-desktop/SKILL.md), [Desktop documentation](../../../docs/DESKTOP.md), and the current launcher and test entry points before changing the workflow.

## Scope and safety

- Keep the smoke check separate from the existing in-process WPF render/resource suite. It checks the launched window's UI Automation surface; it does not replace layout, theme, localization, or application logic tests.
- Use the Windows UI Automation client surface for WPF. Do not use browser automation, Playwright, or Puppeteer for this native window.
- Scope discovery to the specific Desktop process started by the check and its main window. Do not attach to or terminate an already-running user instance, search the whole desktop tree, launch Bannerlord or the Editor, or inject into another process.
- Limit interactions to safe shell inspection. Do not open file pickers, invoke analyzer/work-order commands, submit paths, modify files, or change persisted language, theme, or other preferences.
- Never show a test window on the interactive desktop. The opt-in UIA process must use an invisible, off-screen, non-activating window (`Opacity=0`, `ShowInTaskbar=false`, `ShowActivated=false`, and `WS_EX_NOACTIVATE`) before it is shown. Verify its native window rectangle lies outside the virtual screen before querying controls; a UIA `IsOffscreen` value alone is not sufficient because it may report `false` for a transparent HWND outside the screen.
- Install a passive foreground-event observer before launching the owned process. Fail if that process takes foreground; never call `SetForegroundWindow`, restore focus, or accept a run solely because the final foreground handle matches.
- Suppress modal startup dialogs only for the read-only UIA process; preserve normal interactive startup errors. Keep the observer read-only and limited to the process started by the runner.
- If the process, window, or target control cannot be identified unambiguously, stop and report the check as incomplete. Do not compensate with keyboard shortcuts or guessed clicks.

## Locate and inspect controls

- Identify controls by both `AutomationId` and `ControlType`, scoped to the expected parent. An `AutomationId` is only guaranteed unique among siblings.
- Read the localized `Name` in the active UI language. Prefer stable IDs for cross-language identity; assert a displayed name only when the expected language is known. Do not assume English labels across all thirteen catalogs.
- Record `IsEnabled`, `IsOffscreen`, and the native virtual-screen bounds for the owned window. Check only relevant supported control patterns, such as `Invoke`, `Value`, `Selection`, or `Scroll`; do not invoke a pattern just to prove it exists.
- Keep tree traversal bounded to the main window and the few shell controls under review. Record the process ID, window title, control type, automation ID, accessible name, and supported patterns needed to explain a result.

## Waits and failure evidence

- Wait for window and control conditions with UI Automation events or bounded condition polling. Define a deadline; do not use fixed sleeps as the success condition.
- On timeout or failure, retain the queried UIA properties and a focused subtree dump. Capture a screenshot when the environment supports it. Store temporary evidence separately from source assets and include enough detail to reproduce the check.
- Report exactly what was observed, the active language, whether the check passed or timed out, and what was not examined. A successful smoke check is not a full accessibility audit, visual approval, game validation, or proof that work orders behave correctly.
- If the app cannot be launched or the UIA client is unavailable, report **Not run** with the reason; do not infer success from a process exit code alone.

## Adding or maintaining a harness

- Keep any UIA runner opt-in and distinct from the regular unit/render suites until its launch and cleanup behavior is stable. It must clean up only the process it started and must not rely on Bannerlord installation state or game saves.
- Prefer existing Windows/.NET facilities and project conventions. Add a dependency only when the required UIA behavior cannot be implemented with the available platform APIs.
- Keep checks short and read-only. A shell smoke check should verify the real top-level window and selected accessibility properties, not drive a complete workflow or mutate user data.
