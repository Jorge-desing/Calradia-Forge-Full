# Rev081 — Gauntlet Test Results Explorer

**Date:** 28 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Mod and localization resources. Structured presentation of existing Gauntlet test-run responses.

## Observed need and technical justification

The Tests area currently presents execution output in the shared text ledger. That preserves diagnostic text but makes it difficult to inspect one result in a batch alongside its execution context and cleanup outcome. The explorer separates navigation and detail without adding another way to execute tests.

## Technical solution and architectural decisions

The panel-lifetime results view consumes the latest structurally valid response from the existing `run` or `run-batch` action. Its parser defensively retains at most 51 records in a scrollable list, showing technical ID, status and duration. The current `TestEngine` accepts batches of 1–50 tests and rejects larger batches, so valid engine batch responses contain no more than 50 records. Selecting a record displays its seed, context, `StartedAt`, steps, error and cleanup error. The raw technical values remain verbatim; only interface labels and the empty state are localized. Failed cases remain visibly failed.

Opening the view and selecting a row are presentation-only operations and never invoke or repeat a test. Results survive area navigation and are released when the panel closes. The view does not replace or mutate raw ledger output, `Argument`, command history, output filtering or output comparison.

## Asset, code and dependency changes

The Gauntlet UI design guide and bilingual changelog describe the structured result list and detail surface. The feature reuses the existing test responses and Gauntlet binding conventions; it introduces no route, command, public API, protocol change or external dependency. Product version remains 25.2.0.

## Validation and evidence limits

- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` passed sprite and prefab/layout audits with 0 errors and 0 warnings; its Core suite passed 343/343.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` passed clean `net472` and `net8.0` builds, Core 343/343 and ForgeWeave 73/73. Structural bindings and all 13 generated language resources passed.
- The existing F10 rising-edge and GauntletLayer lifecycle telemetry regression passed. It checks source structure only, not live input; the earlier native assertion and in-game overlay remain unverified, and no `SubModule.cs` change was indicated.
- No live Bannerlord inspection was performed for this record. The installed TPAC predates the current source atlas, so import and in-game rendering remain pending.

This annex adds evidence to the preceding revision without replacing earlier paragraphs.
