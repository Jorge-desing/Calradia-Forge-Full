# Rev061 — Gauntlet Test Results Explorer

**Date:** 28 September 2026
**Version:** Calradia Forge 25.2.0, unchanged
**Scope:** Mod, generated Gauntlet prefab, localization resources, tests and documentation.

## Observed need and technical justification

The Tests area previously exposed run output only through the shared text ledger. Developers needed a read-only way to inspect a case's status and execution/cleanup details without parsing the full payload or accidentally repeating a test.

## Technical solution and architectural decisions

The panel ViewModel retains the latest structurally valid response from the existing `run` or `run-batch` action and presents its cases in a scrollable list plus a detail panel. Selecting or opening the view is presentation-only. The raw ledger output, argument, command history, output filter and comparison remain unchanged. Results survive section navigation and are released with the panel. The presentation parser has a defensive limit of 51 records; the current `TestEngine` accepts 1–50 batch cases and rejects larger batches.

The audit treats the modal as closed in the base layout and independently validates its open geometry at each viewport. It checks the modal bounds, scroll panels, list/detail separation, bindings and passive surfaces. The horizontal layout model now allocates remaining space to stretched stack children and divides space between multiple stretched columns. The summary and detail heading are top-aligned so they stay in their intended bands.

## Asset, code and dependency changes

The generated Gauntlet prefab adds the modal list/detail view, with localized labels in the 13 generated language resources. The feature reuses existing responses and `MBBindingList` patterns and adds no route, command, public API, protocol change or dependency. No `SubModule.cs` change was required. The existing F10 rising-edge and GauntletLayer lifecycle telemetry source regression passed; it does not simulate native input or diagnose the earlier game assertion.

## Validation and evidence limits

- `python tools/generate_assets.py --gauntlet-only`: completed successfully.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause`: passed sprite checks, generated-resource checks and Gauntlet structural/layout audit with 0 errors and 0 warnings; Core passed 343/343.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: passed clean `net472` and `net8.0` builds, Core 343/343 and ForgeWeave 73/73.
- The installed TPAC predates the current source atlas. No TPAC was replaced, Resource Browser import was not performed and live rendering/F10 behavior remains pending. No campaign or battle was started.

This annex adds evidence to the previous protected revision without rewriting its content. Product version remains 25.2.0; no ZIP was generated.
