# Rev026 — SDK v8 validation follow-up

**Date:** September 25, 2026  
**Version:** Calradia Forge 24.0.0, unchanged  
**Scope:** SDK, Core tests, ForgeWeave tests, Gauntlet binding audit, and validation evidence.

## Observed issue and technical justification

After the SDK v8 source and documentation addendum was recorded as Rev025, the required Core-only test launcher exposed one failure in the Gauntlet prefab binding audit. The audit treated every `Command.Click` and `@property` binding as if it belonged to `PanelViewModel`, although Gauntlet resolves bindings inside a `ListPanel` `ItemTemplate` against each collection item's ViewModel. In particular, `ExecuteLoad` belongs to `CommandHistoryItemVM`.

## Technical solution and decisions

The test auditor now resolves an element inside an `ItemTemplate` to the generic item type of the nearest `ListPanel.DataSource` property on `PanelViewModel`. It checks template properties and commands against that owner and checks controls outside templates against the panel ViewModel. This reflects the actual binding context without changing runtime code or public APIs.

## Changes to assets, code, and dependencies

- Corrects `tests/CalradiaForge.Tests/NativeEvidencePanelTests.cs` so item-template bindings are checked against their real item ViewModels.
- Corrects the current SDK codemap from `ForgeApi.Version = 7` to version 8.
- Does not alter module assets, package dependencies, ZIP archives, or product behavior.

## Validation and evidence limits

- `cmd /c tools\\Run-CalradiaForge-Tests.bat --core-only --no-pause` completed successfully.
- The selected `net472` and `net8.0` builds reported zero warnings and zero errors.
- Core passed **275/275** tests; ForgeWeave passed **52/52** tests.
- Bannerlord was not launched. No in-game behavior, package validation, or ZIP contents are claimed by this follow-up.

This addendum extends the previous revision without replacing its paragraphs. It preserves existing commands, paths, product version, and SDK contracts.
