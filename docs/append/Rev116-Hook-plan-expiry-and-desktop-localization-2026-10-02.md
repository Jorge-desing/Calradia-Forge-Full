# Rev116 — Hook Plan Expiry and Desktop Localization Integrity

**Date:** 2026-10-02
**Version:** Calradia Forge 25.2.0, unchanged; SDK API 13
**Scope:** Core runtime plan expiry, Desktop WPF localization generation and status projection, test tooling, and bilingual documentation.

## Observed issue and technical justification

The plan confirmation gate accepted a token at the exact `ExpiresAtUtc` boundary because expiry used a strict greater-than comparison. Separately, the WPF resource generator rewrote full dictionaries from a 73-key hard-coded subset even though the committed dictionaries contained 248 entries, silently removing 175 UI strings per language during regeneration. Hook Workbench status text also retained localized strings from the previous language after the user changed the Desktop language.

## Technical solution and architectural decisions

Plan expiry now uses an inclusive boundary and a regression verifies that a plan is invalid at `now == ExpiresAtUtc`. The Desktop generator reads an explicit extension catalog with translations for the full 13-language set, preserves the prior 248 keys, and adds a localized language-change message. Duplicate JSON keys and duplicate generated resource keys are rejected; the localization audit checks exact extension values and locale coverage. Hook Workbench stores a resolver for UI-owned status messages and refreshes it when localization changes. Raw host errors and diagnostic detail remain unmodified.

## Code, assets, and dependency changes

The change updates the expiry comparison and Core regression, Desktop localization/status view models, the 13 generated WPF dictionaries, `localization/desktop-extensions.json`, native Hook Workbench `Cancel` localization and its generator/audit path, Python BAT checks, SDK and shared-library documentation, and bilingual changelog/ledger sources. Existing WPF translation values were preserved; no product version, SDK API, command, route, hook behavior, or runtime dependency changed.

## Validation and evidence limits

- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` passed Desktop 65/65 and WPF 295 render cases / 308 layout passes.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` passed Core 414/414, the serial x64 hook fixture (including Finalizer and ILHook cases), ForgeWeave 73/73, Desktop 65/65, and WPF 295/295 render cases / 308 layout passes; build output reported 0 warnings and 0 errors.
- `tools/Run-CalradiaForge-Python-Checks.bat --ci --no-pause` passed asset, localization, sprite, and icon checks; `tools/Verify-CalradiaForge-StatelessBehavior.bat` passed all four acceptance stages.
- The WPF timings measure the render harness, not application interaction latency. No Bannerlord, campaign, battle, or live Resource Browser import was opened or verified.
- Canonical distribution packaging and its archive hashes are a separate completion gate and are recorded in the delivery report.

This annex extends the protected record without rewriting earlier revisions.
