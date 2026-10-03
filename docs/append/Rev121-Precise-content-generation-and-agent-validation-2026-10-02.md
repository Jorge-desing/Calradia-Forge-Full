# Rev121 — Precise content generation and evidence-based agent validation

**Date:** 2 October 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** SDK item generation, showcase regression coverage, agent validation guidance and final verification.

## Observed issue and technical rationale

`ForgeItemBuilder` rounded weights to one decimal place and accepted non-finite `double` values, which cannot be represented by the Native Items `xs:decimal` field. A first precision fix also removed the legacy `.0` suffix from integral values; the showcase BAT exposed that compatibility regression before it was corrected.

## Technical solution and architectural decisions

- Serialize weights with invariant XML-decimal formatting that retains fractional precision and at least one fractional digit; reject `NaN` and infinities at the builder boundary.
- Reject Banner generation until a version-specific Native Items component schema can be verified rather than emit an unsupported structure.
- Align the agent execution guide with actual local offline tool execution, and make release validation use maintained BAT launchers with run-specific test counts and explicit harness/live-runtime evidence boundaries.

## Changes in assets, code and dependencies

Updated `ForgeItemBuilder`, SDK tests, the release-validation and agent-orchestration skills, bilingual agent guides, and this append-only record. Product version, `ForgeApi.Version`, public package dependencies and runtime targets remain unchanged.

## Validation and evidence limits

- `tools\Run-CalradiaForge-Tests.bat --no-pause`: clean `net472`, `net8.0` and Desktop builds with 0 warnings/errors; Core 411/411, ForgeWeave 73/73, Desktop 65/65, and WPF 295 cases/308 layout-render passes. The 16,366 ms render time is harness time, not application latency.
- `tools\Test-CalradiaForge-ContentShowcase.bat --no-pause`: deterministic output, local XSD checks, `net472` module build, Core 411/411 and ForgeWeave 73/73 passed. The generated showcase XML hashes did not change.
- `tools\Test-CalradiaForge-Developer-Onboarding.bat --portable-only --no-pause`: package, isolated template installation, generated-module restore/build and manifest checks passed. Portable mode does not prove GameBin integration or in-game loading.
- `tools\Verify-CalradiaForge-StatelessBehavior.bat`: all four checks passed with 0 build warnings/errors.
- A static adversarial review found no Harmony package/assembly dependency or runtime loading path; this does not establish third-party coexistence.
- No live Bannerlord session, campaign or battle was started. In-game load/render and public NuGet/IDE publication remain unverified.

The product remains at 25.2.0 and `ForgeApi.Version` remains 13.
