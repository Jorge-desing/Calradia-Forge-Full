# Rev117 — Time-slicing edge cases and onboarding validation

**Date:** 2 October 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** SDK, Core, Mod, Tools, Documentation. Overflow-safe work bucketing, generated Gauntlet field labels, localization resource generation, optional external-patch diagnostic evidence, and consolidated agent guidance.

## Observed issue and technical rationale

Stable hour-bucket normalization could overflow for large positive bucket counts. The Gauntlet page composer already had a localized label on its bound view model but did not render it above generated editable fields. The periodic-work help localization existed in source catalogs, but generated resource coverage exposed a mistaken test assumption that general help text must also be a native-menu control key. Reports without an external diagnostic snapshot also needed to distinguish “not captured” from “runtime not loaded.”

## Technical solution and architectural decisions

Time-slice normalization now handles the full positive `int` bucket range and shares its normalization path with both `ProcessBatch` overloads. This does not reduce the cost of scanning and hashing every entity ID, guarantee even bucket loads, or make deferred work appropriate for latency-sensitive callbacks. The generated Gauntlet composer displays the existing localized `Label` without changing routes, commands, bindings or public APIs. The localization regression validates the source catalogs and generated game resources instead of requiring a generic help paragraph in the separate menu-control catalog. An absent external snapshot is reported as `NotCaptured`; no claim of runtime absence is inferred. Skills and the three root agent guides state the current evidence limits for time slicing, measurements, Harmony observation and local SDK onboarding.

## Changes in assets, code and dependencies

- Hardened `ForgeTimeSlicer` normalization and added overflow regressions for `ShouldProcess` and both `ProcessBatch` overloads.
- Rendered the existing Gauntlet component `Label` and retained its editable binding contract.
- Corrected the standalone panel-localization resource generator and removed the incorrect native-menu assertion from localization tests.
- Preserved Harmony independence: Forge does not reference, load, bundle or mutate Harmony; optional reflection observes a caller-supplied already-loaded assembly only.
- Refreshed bilingual SDK evolution evidence with the latest isolated package/template smoke report and hashes; updated agent rules and specialist skills without altering prior ledger entries.
- No product version, public SDK API version, target framework or TaleWorlds dependency changed.

## Validation and evidence limits

- `tools/Run-CalradiaForge-Tests.bat --no-pause`: full build, zero warnings/errors; Core 404/404, ForgeWeave 73/73, Desktop 65/65, WPF 295 render cases and 308 layout/render passes (16,098 ms in the harness).
- `tests/CalradiaForge.PatchDiagnostics.Tests.bat --no-pause`: 28/28.
- `tools/Verify-CalradiaForge-StatelessBehavior.bat`: passed.
- `tools/Test-CalradiaForge-Developer-Onboarding.bat --no-pause`: SDK and template package smoke, isolated template install, module generation/restore/build passed; report `artifacts/sdk-evolution/onboarding/20261002T033001Z-00c3d2e2/source-package-report.json`.
- `tools/Test-CalradiaForge-ContentShowcase.bat --no-pause`: deterministic generation, schema/manifest checks, project build, Core and ForgeWeave checks passed.
- `tools/Run-CalradiaForge-Python-Checks.bat --no-pause`: Python base tooling, Ruff, 25 asset fixtures, five image fixtures, five offline agent utility tests and Gauntlet asset structural checks passed. The separate optional Antigravity profile did not install: pip returned `InvalidChunkLength`.
- Five modified specialist skills passed `tools/Validate-CalradiaForge-Skills.bat`; onboarding knowledge audit reported zero issues.
- Harness timings are not Bannerlord tick or application latency. No live Bannerlord session, TPAC deep parsing or in-game Gauntlet rendering was verified. The external diagnostic test uses a synthetic assembly fixture and does not prove real Harmony coexistence.

This annex adds evidence to the preceding revision without replacing its text. Existing routes, commands, public APIs and product version remain unchanged.
