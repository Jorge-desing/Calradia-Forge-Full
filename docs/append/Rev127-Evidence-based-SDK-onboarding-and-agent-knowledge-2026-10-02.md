# Rev127 — Evidence-based SDK onboarding and agent knowledge consolidation

**Date:** 2026-10-02  
**Product version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Local SDK/template onboarding, deterministic native-content showcase, bounded patch diagnostics, static Gauntlet example, measured simulation guidance, and project knowledge consolidation.

## Observed objective and design decisions

The project needed a reproducible first-mod path and source-grounded guidance that did not overstate experimental runtime capabilities. The capability matrix distinguishes implemented features, experimental features, and proposals. Existing Harmony-based product surfaces were replaced by Forge-owned bounded diagnostics; optional runtime observation is reflection-only and does not create a Harmony dependency or establish real-mod compatibility. The content example uses verified Native schemas, and factions remain deferred until a schema and case are verified.

## Delivered artifacts

- Added a versioned local `CalradiaForge.Sdk` NuGet package and `.NET new` module template with explicit Forge dependency and locally resolved Bannerlord references. Proprietary TaleWorlds assemblies are not distributed.
- Added deterministic troop/item generators and an installable static content showcase. Maintained BAT workflows check the prefab, ViewModel, localization, manifest, native references, and package output.
- Added bounded, read-only patch diagnostic evidence and a static Gauntlet page prototype. Shared patch targets are review signals only. Forge attributes failures only within callback boundaries it controls; it does not reorder or disable third-party patches.
- Consolidated verified .NET, Bannerlord, SDK, test, documentation, and agent lessons into project specialists and synchronized root gateways. Time-slicing documentation states that filtering still scans its source and makes no unsupported performance claim.

## Validation

- `tools\Run-CalradiaForge-Tests.bat --no-pause`: clean `net472`, `net8.0`, and Desktop builds, zero warnings/errors; Core 415/415; Patch Diagnostics 30/30; ForgeWeave 73/73; Desktop 65/65; 295 WPF render cases and 320 layout/render passes. The 20,903 ms result is harness timing, not application latency.
- `tools\Verify-CalradiaForge-StatelessBehavior.bat`: 4/4 checks passed.
- `tools\Run-CalradiaForge-Python-Checks.bat --ci --no-pause`: Ruff 0.16.9, 25 asset/archive tests, five image tests, 18 agent audit cases, 15 offline orchestration cases, three CLI launcher cases, and Gauntlet/sprite source checks passed. The `--ledger` run verified 42/42 EN/ES pairs.
- All 29 modified skill directories passed `quick_validate.py`; the onboarding knowledge BAT passed root-guide parity/link checks and nine skill validators.
- Isolated onboarding run `20261003T012628Z-f3bebd08` packed and installed SDK/template 25.2.0 locally, generated and restored a consumer module, and compiled it as `net472` against local licensed GameBin references with zero warnings/errors. Its report records stable SDK contract 13 and `workingTreeApiChangesIncluded: false`. Local artifact SHA-256: SDK `2f4aa6863129bd8ea67ae6e7b8b71de643975d962c0e007e02f38ece5f98718c`; template `f9b3a74577e9ae5b9caa7134b37dab8214111396a876fad8e57fd499e2115a64`.
- The content showcase BAT passed deterministic generation, installed Native Items/NPCCharacters schema checks, manifest/reference checks, and a clean generated `net472` module build.

## Evidence limits

No Bannerlord or Modding Kit process was launched for this validation. Game loading/rendering, real Harmony coexistence, public NuGet publication, and Visual Studio/Rider marketplace integration remain unverified. Static Gauntlet checks are not in-game rendering evidence. No runtime performance gain is claimed. The HTML analysis references were preserved as supplied and were not rewritten as technical authority.
