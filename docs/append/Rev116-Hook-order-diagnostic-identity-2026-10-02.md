# Rev116 — Hook Order Diagnostic Identity and Inventory Regression

**Date:** 2 October 2026
**Version:** Calradia Forge 25.2.0, unchanged
**Scope:** Core, Mod, tests, documentation, distribution validation. The runtime hook behavior and public API are unchanged.

## Observed problem and technical rationale

The read-only `cf.hook_order` report compared the display string for each target method. That string omitted declaring assembly identity and closed generic method arguments, so a generic method and a non-generic method with the same name and parameter list could be reported as the same target. A related test used substring checks for hook IDs; after the test declared `Before` and `After` references, those IDs also appeared in metadata text and were mistaken for returned inventory records.

## Technical solution and architecture decisions

Hook target identities now include the declaring type's assembly-qualified identity, the method's closed generic arguments, and assembly-qualified parameter types. The ordering report still compares declarations against the current inventory only and does not claim to calculate MonoMod's effective dispatch order. The console regression checks full inventory-record prefixes, and now reports which filter assertion failed instead of obscuring it in one compound condition. No hook is applied by registration, inventory, or ordering diagnostics.

## Changes to assets, code, and dependencies

- Updated `ForgeHookService.Identity` to distinguish generic and non-generic methods and similarly named targets from different assemblies.
- Added a regression using a generic/non-generic same-name pair and a cross-target `Before` reference.
- Corrected hook inventory tests to distinguish record lines from IDs mentioned in `Before`/`After` metadata.
- Updated the English and Spanish Patch Blueprint guides and changelogs. Product version, SDK API 13, MonoMod.RuntimeDetour 25.3.6, public contracts, and game behavior remain unchanged.

## Validation and evidence limits

- `dotnet build CalradiaForge.sln -c Release -v:minimal`: passed with zero warnings and errors.
- `tools/Run-CalradiaForge-Core-Tests.bat --no-pause`: Core passed 413/413; the serial x64 hook fixture passed through `Run-DetourFixture.bat`.
- `tools/Run-CalradiaForge-Desktop-Tests.bat --no-pause`: 65/65 passed.
- `tools/Run-CalradiaForge-ForgeWeave-Tests.bat --no-pause`: 73/73 passed.
- `tools/Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause`: 295 render cases passed; harness time is not application latency.
- `tools/Verify-CalradiaForge-StatelessBehavior.bat`: passed 4/4.
- `tools/Run-CalradiaForge-Python-Checks.bat --ci --no-pause`: passed source asset, Gauntlet structure, and resource checks.
- `tools/package.ps1 -SkipTests`: produced and audited all three distribution archives; independent SHA-256 checks matched the emitted manifest at packaging time.
- No live Bannerlord or Modding Kit hook application was performed. Serial fixture results do not establish safe patching while another thread executes the target.

This annex adds evidence without replacing prior revisions. It preserves the existing commands, routes, API contracts, and product version.
