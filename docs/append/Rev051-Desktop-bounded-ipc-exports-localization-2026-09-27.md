# Rev051 — Bounded Desktop IPC, safe report exports, and localized studio labels

**Date:** 27 September 2026<br>
**Version:** Calradia Forge 25.2.0, unchanged<br>
**Scope:** Standalone Desktop WPF: bounded IPC response reading, report export, accessible copy controls, and visualizer localization.

## Observed issue and technical rationale

The Desktop client previously relied on a whole-line read for newline-delimited IPC responses. A peer could make the client accumulate a response before the existing size limit was checked. Report writes also needed predictable behavior under concurrent exports and failures, and dossier copy actions needed distinct accessible names and announced outcomes. Static visualizer labels remained embedded in XAML despite the 13-language resource catalogs.

## Technical solution and decisions

`BoundedLineReader` reads incrementally with a bounded buffer, observes cancellation, and enforces the existing 32 Mi UTF-16-character limit while accumulating the line. An over-limit response fails before deserialization and follows the existing disconnect path; the wire format and limit remain unchanged.

`DesktopReportExportService` performs exports asynchronously. It stages each report in an exclusive temporary file in the destination directory, publishes to a unique path without overwriting existing output, and removes temporary files on cancellation or failure. Localized status is returned to the view model while retained evidence remains intact.

Dossier copy controls use unique automation IDs; each console-command accessible name includes its command, the CLI control has its own localized name, and a polite live region reports localized success or failure. Remaining static labels in troop, workshop, code-security, module-topology, diplomacy, and component visualizers now come from all 13 resource dictionaries. Longer labels wrap; example names, command strings, identifiers, and numerical/technical sample values are preserved.

## Assets, code, and dependencies

- Updated bounded IPC reading and asynchronous no-overwrite report export for Desktop.
- Added localized visualizer labels and regression coverage for 13-catalog parity, static-label resource wiring, route rendering, and dossier controls.
- No new dependencies, public API, route, command, permission, IPC protocol field, or distribution package was added or changed. Product version remains 25.2.0; ZIPs were not regenerated.

## Validation and evidence limits

- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-robustness-after-20260927-final.json <nul"` built with zero warnings/errors, passed Desktop 63/63, and passed 289 WPF render cases with 182 layout passes.
- The render artifact records 11,758 ms total harness time and 3,483.7 ms in layout calls. These measure the harness, not latency in the open application.
- `cmd.exe /d /c "tools\Test-CalradiaForge-Desktop-Uia.bat -OutputPath artifacts\desktop-robustness-after-20260927-final-uia.json -TimeoutSeconds 30 <nul"` passed 23/23 read-only UIA checks and records `ForegroundUnchanged=true`, `ForegroundChangedByOwnedProcess=false`, and no observed foreground event from the owned process.
- UIA inspected the launched main window's accessible tree only. It did not exercise file/folder pickers, work execution, preference changes, or visual approval. No Bannerlord session, campaign, or battle was started.

This appendix adds evidence to the previous revision without replacing its paragraphs. Source changelog Rev071 and protected Register appendix Rev051 are independent counters.
