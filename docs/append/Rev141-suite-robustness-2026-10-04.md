# Rev141 — Suite Robustness Corrections

**Date:** 2026-10-04

**Version:** Calradia Forge 25.2.0; Forge API 13 unchanged

**Scope:** Mod IPC and unload lifecycle, SDK availability notifications, campaign scheduling, and WPF localization/accessibility.

## Observed problem and technical rationale

The line-oriented named-pipe server used an unbounded `ReadLineAsync` before checking the 65,536-character request limit, so an incomplete or oversized request could keep accumulating data. Several unload operations were grouped behind cleanup paths where an exception could prevent later finalization. The legacy SDK availability callback list could continue notifying subscribers after a callback synchronously replaced the connected provider. The campaign time-slicer also needed to fail closed when the engine clock could not be read. WPF had untranslated cycle/favorites labels and a Ping action without explicit accessible naming/help.

## Technical solution and architectural decisions

- Added an incremental UTF-8 line reader with a 65,536 UTF-16-character limit, cancellation, and a 15-second incomplete-line deadline. It preserves LF, CR, and CRLF line boundaries, and disconnects on timeout or malformed/oversized requests before JSON deserialization.
- Isolated module and runtime unload steps. Failures are reported and do not stop later event unsubscription, SDK data cleanup, IPC disposal, or session-log persistence scheduling.
- Revalidated the connected Forge API generation before and after each legacy availability callback; remaining callbacks stop when a callback disconnects or replaces that generation.
- Made campaign scheduling return false when the engine clock cannot be read.
- Added localized WPF labels and explicit UI Automation names/help for the cycle indicator, favorites filter, and Ping button without changing commands or AutomationIds.

No public API, IPC schema, routes, permissions, dependencies, product version, API version, or target frameworks changed.

## Code, assets, and tests

- `src/CalradiaForge.Mod/PipeServer.cs`, `Runtime.cs`, `SubModule.cs`, and `CampaignBehaviors/ClanCharacterProgressionBehavior.cs` implement bounded transport and independent cleanup/fail-closed scheduling.
- `src/CalradiaForge.Sdk/Contracts.cs` stops stale-generation notifications.
- WPF presentation resources and all 13 locale dictionaries provide accessible localized labels.
- Regression coverage includes IPC size boundaries, delimiters, cancellation, timeout/reconnect, independent cleanup after exceptions, inaccessible campaign clock, stale-generation reentrancy, locale parity, and UI Automation properties.

## Validation and evidence limits

- `tools/Run-CalradiaForge-Tests.bat --no-pause`: solution build succeeded with 0 warnings and 0 errors; Core 436/436, ForgeWeave 74/74, Desktop 75/75, and 296 WPF render cases with 320 layout/render passes passed.
- `tools/Verify-CalradiaForge-StatelessBehavior.bat`: 4/4 checks passed.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause`: structural audit passed with 0 errors and 0 warnings; this is source/fixture validation, not live-game validation.
- `tools/Test-CalradiaForge-Desktop-Uia.bat`: read-only inspection passed for 23/23 observed records in its dedicated test process.
- The 9,962.7 ms render/layout figure is from the WPF harness (22,060 ms total render suite), not observed application latency. Bannerlord was not launched; game behavior remains unverified.

This appendix adds evidence without replacing earlier revisions. All prior DOCX content and integrity records remain append-only.
