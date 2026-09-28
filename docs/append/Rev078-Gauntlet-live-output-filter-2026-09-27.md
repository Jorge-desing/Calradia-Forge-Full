# Rev078 — Gauntlet live output filter

**Date:** 27 September 2026
**Version:** Calradia Forge 25.2.0, unchanged
**Scope:** Add a presentation-only text filter for the current native tool output.

## Intended behavior

The output query is independent of the tool argument and command history. It filters output lines case-insensitively, remains in memory while the panel is open across area navigation, and applies to newly produced output. Matching lines continue through the existing wrapping and pagination behavior. An empty query shows the unfiltered output, and a localized state explains when the query has no matches. The original output and retained evidence remain unchanged.

## Validation

- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: passed; clean `net472` build with zero warnings, Core 340/340, ForgeWeave 73/73.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause`: passed sprite-resource checks, prefab/layout audit, localized generated-resource checks, and the Core suite. The audit enforces a usable minimum width for the filter at all reference viewports.
- The filter translations were checked against all 13 generated Bannerlord language resources. A whitespace-only query is treated consistently as empty.
- F10 review was static only: no concrete defect was found in `SubModule.cs` or its telemetry ordering; the existing test checks source structure rather than simulated input.

No Bannerlord session was launched. Resource Browser import, TPAC refresh, live rendering and in-game filter interaction remain unverified; the visual launcher reports that the installed TPAC predates the source atlas. Static and automated checks are not evidence of runtime Gauntlet behavior.
