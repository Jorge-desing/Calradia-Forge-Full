# Rev115 — Developer onboarding and installable content showcase — 2026-10-01

Product: Calradia Forge 25.2.0, unchanged.
Scope: SDK onboarding, the developer template, static content generation, and bounded Harmony diagnostics guidance.

## Changes

- Added an installable content showcase that deterministically generates a static item, troop definition, module manifest, localized read-only Gauntlet extension page, and ViewModel. It reuses Native horse_whip presentation assets and does not insert the troop into a party template or troop tree.
- Aligned novice content generation with the SDK builders and corrected generated troop equipment rosters to use the game schema's EquipmentRoster element. Existing builder signatures and supported item options are retained.
- Added SDK/template onboarding guidance, validation and packaging workflows, and project-specific skill references. The guidance explains the dual target-framework boundary and keeps Bannerlord runtime constraints separate from the Desktop workbench.
- Documented bounded, read-only Harmony inventory as a compatibility signal. It reports bounded provenance/freshness and incomplete-query state; its isolated fake-query fixture does not prove coexistence with a live Harmony runtime.
- This onboarding/showcase work adds no public SDK API. The isolated package smoke report records source HEAD d8c0def6ab3604ded8600bacd81db50cd581bd32, SDK contract 13 and product 25.2.0, with explicit builder/project overlays. The earlier contract-12 smoke is historical only; contract 13 came from separate hook work, not this onboarding objective.

## Validation evidence and limits

- `tools\Test-CalradiaForge-ContentShowcase.bat --no-pause` passed deterministic generation, installed-game Items.xsd and NPCCharacters.xsd checks, manifest/native-reference/static-page checks, and a net472 showcase-module build with zero warnings and errors. The same run passed Core 405/405 and ForgeWeave 73/73. XSD SHA-256 values used by the verifier: Items.xsd E57D50EA6B0C6FD8356F4A26AAB7593B410242C6F4C457E4F1A4226961B9E266; NPCCharacters.xsd 912DDCB8BECB1315D9A579C81C109C420FD0391FFCF0D8AB53B49DEFE0B2A687.
- The five-sample synthetic time-slicer harness reported medians of 1.945 ms (128 entities × 128 repetitions), 0.9962 ms (2,048 × 8), and 0.2476 ms (16,384 × 1), with zero measured synchronous thread allocations in that fixture. Repetition counts differ, so these values are not comparative speedups or Bannerlord frame timings; no optimization was applied from them.
- Onboarding validation passed root-guide parity, local documentation links, Ruff and nine skill checks. The isolated template-hive smoke created local SDK/template packages, generated and validated a module, and compiled it against installed net472 game references with zero warnings/errors. Report: artifacts/sdk-evolution/onboarding/20261001T184300Z-5a8bfd73/source-package-report.json. It records SDK package SHA-256 78c6362f07f9a6049a63b05febc375904f650530c0ac02c397c12f823e81307e and template package SHA-256 95d3f897362504fc01d63403f343d197ad827208e4a8d981487ac193c61b11f7. This verifies local NuGet/template smoke artifacts, not distribution ZIPs, IDE UI integration, or live Bannerlord loading/rendering.
- The integrated BAT pipeline passed Core 405/405, ForgeWeave 73/73, Desktop 65/65, and 294 WPF render cases with 182 layout passes. Its 17,341 ms render value is harness time, not application interaction latency. Stateless acceptance passed 4/4; Asset Pipeline passed 23/23; the isolated Harmony diagnostics fixture passed 11/11. That fixture is not a live Harmony coexistence test.
- The independent source template declares its own module version, `v1.0.0`; the audit exception is scoped to that exact template manifest, and distributable Forge modules still require product version `25.2.0`. Local .nupkg hashes in this entry do not certify the three distribution ZIPs; the canonical package gate validates those archives separately.
- No IDE UI, live Gauntlet rendering, in-game behavior, campaign, or battle was verified.

The protected improvement-record DOCX and its integrity chain are handled separately; earlier revisions remain unchanged.
