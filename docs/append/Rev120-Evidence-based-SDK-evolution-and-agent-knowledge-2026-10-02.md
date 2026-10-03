# Rev120 — Evidence-based SDK evolution and agent knowledge consolidation

**Date:** 2 October 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** SDK, Core diagnostics, developer onboarding, content showcase, agent tools, documentation and project rules.

## Observed issue and technical rationale

The evolution work needed to distinguish shipped capabilities from proposals, provide a reproducible local starting point, and avoid an external Harmony requirement. Existing time-slicing guidance also needed to describe its actual scan cost and uneven bucket populations rather than imply a general performance gain.

## Technical solution and architectural decisions

- Added a locally packable `CalradiaForge.Sdk` package and `.NET new` module template targeting `net472`; consumers resolve TaleWorlds references locally and do not redistribute game assemblies.
- Added deterministic troop/item generation and an installable source showcase with a static Gauntlet page. Factions and live-engine behavior remain deferred until their schemas and runtime are verified.
- Replaced the retired Harmony snapshot surface with Forge-owned patch diagnostics. Optional observation uses bounded reflection only over an exact compatible `0Harmony` assembly already loaded by the host and only when requested; Forge does not load or mutate Harmony.
- Updated stable-ID time-slicing guidance and project agent instructions to state that bucketing may be uneven and filtering still scans the input collection.

## Changes in assets, code and dependencies

The SDK packaging/template workflow, showcase module and generators, bounded diagnostic inspector, validation tests, bilingual technical guides, and specialist agent guidance were added or synchronized. Product dependencies remain independent of Harmony; no proprietary TaleWorlds assemblies are packaged.

## Validation and evidence limits

- `tools\Run-CalradiaForge-Tests.bat --no-pause`: clean `net472`, `net8.0` and Desktop builds; at the original Rev120 validation point, Core 410/410, ForgeWeave 73/73, Desktop 65/65, and WPF 295 cases/308 layout-render passes. WPF time is harness time only.
- `tools\Verify-CalradiaForge-StatelessBehavior.bat --portable`: all four architectural acceptance checks passed.
- Python/Ruff, asset checks, onboarding/template smoke and content-showcase smoke passed through maintained BAT launchers.
- Optional Antigravity dependency installation was not verified because the package mirror previously returned `InvalidChunkLength`.
- No live Bannerlord session, campaign, battle, real Harmony runtime/coexistence, public NuGet publication, or IDE marketplace integration was tested.

This appendix preserves the existing changelog account of the original Rev120 validation. The later precision/finite-weight regression and its updated final test totals are recorded in Rev121.
