# Rev079 — Gauntlet filter review hardening

**Date:** 27 September 2026
**Version:** Calradia Forge 25.2.0, unchanged
**Scope:** Resolve two static review findings from the live-output filter round and record the F10 review boundary.

## Findings and corrections

- The evidence heading shared a fixed 205-DIP row with the filter while binding to the active route name. Long SDK titles could be clipped or approach the filter. It now uses a short localized `Evidence` label; the active route title remains in the workspace header. Source and generated game catalogs contain the label in all 13 supported languages.
- `SubModule.Open()` constructed `GauntletLayer` before its cleanup `try`. A constructor exception after creating the panel ViewModel could leave that partial ViewModel unfinalized. Layer construction is now inside the guarded region, and `Close()` finalizes the ViewModel even when no layer was assigned. A source-level regression protects this path.
- Static review did not establish a cause for the historical F10 assertion. The hotkey test still checks source ordering and does not simulate Bannerlord input, native assertions, or Gauntlet rendering. No game session was launched and no F10 reproduction was attempted.

## Validation and evidence limits

- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: passed; `net472` and `net8.0` builds reported zero warnings and errors, Core 340/340, ForgeWeave 73/73.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause`: passed; sprite validation and Gauntlet structural/layout audit reported 0 errors and 0 warnings; Core passed 340/340.
- The source atlas and SpriteData checks are static. The installed TPAC predates the source atlas; import, TPAC refresh, and live Gauntlet rendering remain pending.
- The F10 lifecycle review found that session telemetry is buffered before periodic persistence, so an abrupt native termination may omit the final breadcrumbs. This was recorded as an evidence limitation, not changed without a runtime-verifiable cause.
