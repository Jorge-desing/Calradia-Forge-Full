# Rev117 — Exact Hook Target Signature Identity for Ordering Diagnostics

**Date:** 2026-10-02
**Version:** Calradia Forge 25.2.0, unchanged
**Scope:** Core hook snapshots, read-only console diagnostics, tests, and bilingual SDK documentation. No hook application behavior or public API signatures changed.

## Observed issue and technical justification

The read-only `cf.hook_order` diagnostic compares registered targets using `ForgeHookSnapshot.TargetMethod`. Earlier identity text could conflate IL methods that shared a declaring type, name, and parameters but differed by return type, or static versus instance dispatch. Required and optional custom modifiers are also part of a CLR method signature and must be represented when distinguishing targets. A false same-target classification could make a declared order reference look like a Forge-known edge when it points at another method.

## Technical solution and architectural decisions

The method identity now includes the declaring type's assembly-qualified identity, closed generic method arguments, parameter and return types, `IsStatic`, `CallingConvention`, and each parameter/return's required and optional custom modifiers. Read-only ordering diagnostics continue to classify only current Forge registrations; they do not infer MonoMod's effective order. The public `ForgeHookSnapshot.TargetMethod` string is diagnostic data whose display grammar may evolve; consumers must treat it as opaque rather than parse it.

Reflection.Emit regressions construct distinct methods that vary only by return type, static/instance calling convention, or a required return custom modifier. They verify that snapshots differ and `cf.hook_order` does not classify a cross-target reference as the same target. Registration and inventory remain inert.

## Changes to assets, code, and dependencies

- Expanded `ForgeHookService.Identity` and added helpers for parameter and custom-modifier identity.
- Added isolated runtime-generated signature tests in `SdkFeaturesTests`.
- Updated the Patch Blueprint and SDK reference guides and the English/Spanish changelogs.
- Product version remains 25.2.0, SDK API remains 13, and MonoMod.RuntimeDetour remains 25.3.6. No third-party dependency or public contract signature was added.

## Validation and evidence limits

- `tools/Run-CalradiaForge-Core-Tests.bat --no-pause`: Core passed 413/413; the serial x64 detour fixture passed through its BAT launcher. The v12 binary-compatibility fixture also built without warnings or errors.
- `tools/Run-CalradiaForge-Desktop-Tests.bat --no-pause`: 65/65 passed.
- `tools/Run-CalradiaForge-ForgeWeave-Tests.bat --no-pause`: 73/73 passed.
- `tools/Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause`: 295/295 cases passed with 308 layout passes. The reported duration is harness time, not application latency.
- `tools/Verify-CalradiaForge-StatelessBehavior.bat`: 4/4 passed.
- `tools/Run-CalradiaForge-Python-Checks.bat --ci --no-pause`: passed.
- The local Release solution build completed with zero warnings and errors through the stateless BAT gate.
- No live Bannerlord or Modding Kit hook application was performed. These identity checks are metadata diagnostics; they do not prove effective runtime dispatch order or concurrent detour safety.

This annex extends the preceding registry without replacing earlier paragraphs. It preserves product version, hook behavior, API signatures, and existing ordering declarations.