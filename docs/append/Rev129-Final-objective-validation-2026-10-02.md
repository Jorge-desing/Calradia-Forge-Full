# Rev129 — Final SDK evolution validation and evidence boundaries

**Date:** 2026-10-02  
**Product version:** Calradia Forge 25.2.0, unchanged; `ForgeApi.Version` 13  
**Scope:** SDK, Core, Desktop/Gauntlet static resources, developer tools, examples, documentation, and project-agent knowledge.

## Observed issue and technical rationale

The evolution roadmap had to be reconciled against implemented source rather than aspirational descriptions. The local developer start path, native content generation, patch diagnostics, and simulation helpers have distinct maturity and evidence boundaries. Generated language resources also needed to retain source-language text consistently, while the documentation and agent instructions needed to state only what the current code and repeatable checks support.

## Technical solution and architectural decisions

The local onboarding flow builds a versioned SDK package and `.NET new` module template, installs them in an isolated template hive, generates a consumer, restores it, and compiles it against licensed local GameBin references. The generated content showcase remains static and deterministic; it does not claim runtime entity creation or Gauntlet rendering.

`ForgePatchDiagnostics` reports bounded Forge-owned hook/replacement evidence and can optionally query a compatible public surface on an exact `0Harmony` assembly that is already loaded. Forge does not reference, load, distribute, patch, unpatch, or reorder Harmony. The external query runs synchronously in-process: output bounds do not sandbox or time-bound Harmony's internal CPU, allocations, or effects, and shared targets are review signals rather than conflict verdicts. The tests use fixtures and do not prove coexistence with real third-party mods.

Simulation guidance now reflects `ForgeTimeSlicer` behavior: stable IDs select deterministic buckets, but the input collection is still traversed. No performance gain is claimed without measuring the complete callback. Gauntlet resource guidance and four category-help labels use generated, localized sources across the 13 existing languages; product/API version and public contracts are unchanged.

## Changes to assets, code, and dependencies

The objective adds the local SDK/template packaging and isolated smoke workflow, a deterministic troop/item content showcase and verifier, bounded patch-diagnostic adapter/tests, Python tool-environment support, localized category guidance, and evidence-backed updates to rules, specialist skills, and the root `AGENTS.md`, `CODEX.md`, and `GEMINI.md` guides. It adds no required Harmony dependency and ships no proprietary TaleWorlds assemblies. Product version remains 25.2.0 and `ForgeApi.Version` remains 13.

## Validation and evidence limits

- `tools\Run-CalradiaForge-Tests.bat --no-pause`: clean `net472`, `net8.0`, and Desktop builds; zero warnings/errors; Core 416/416, ForgeWeave 73/73, Desktop 65/65, and 295 WPF render cases with 320 layout/render passes. Render time was 19,288 ms in the harness only.
- `tests\CalradiaForge.PatchDiagnostics.Tests.bat --no-pause`: 30/30 passed.
- `tools\Test-CalradiaForge-Developer-Onboarding.bat`: isolated SDK/template package installation, consumer generation, restore, and `net472` build passed with zero warnings against local licensed GameBin references. `tools\Test-CalradiaForge-ContentShowcase.bat` and `tools\Verify-CalradiaForge-StatelessBehavior.bat` passed.
- `tools\Run-CalradiaForge-Python-Checks.bat --ci --no-pause` passed. The optional `--agents` setup was attempted separately but pip failed with `InvalidChunkLength`; the dependency-backed optional profile remains unverified. `--ledger` verified all 42/42 maintained English/Spanish technical-document pairs; this new bilingual appendix is recorded separately in the protected improvement ledger.
- All 29 modified skill directories passed `tools\Validate-CalradiaForge-Skills.bat`; `tools\Validate-CalradiaForge-Onboarding-Knowledge.bat` passed. Generated runtime resources matched all 52 new label translations (13 languages × 4 labels).
- The TPAC deep parser was skipped because `TpacTool.Lib.dll` or its local Native fixture was unavailable. Bannerlord and Modding Kit were not launched. No live engine load/render, real Harmony coexistence, public NuGet publication, or Visual Studio/Rider marketplace integration is claimed.

This appendix adds evidence without replacing previous revisions. It preserves existing commands, paths, contracts, and product version; the render-harness measurement is not application latency.
