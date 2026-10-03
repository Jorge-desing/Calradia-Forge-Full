# Rev119 — Evidence-based SDK evolution and agent knowledge consolidation

**Date:** 2 October 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Local developer onboarding, generated content example, Forge-owned patch diagnostics, time-slicing guidance, bilingual documentation, and project agent rules/skills.

## Observed issue and technical rationale

The planned evolution combined current capabilities with ideas that still require runtime or ecosystem evidence. Several performance descriptions also risked implying uniform bucket distribution or reduced work even though `ForgeTimeSlicer` still visits the source collection. Harmony support needed to remain optional, without restoring a dependency or treating a shared target as proof of conflict.

## Technical solution and architectural decisions

- Added a locally buildable `CalradiaForge.Sdk` package and `.NET new` module template targeting `net472`, with game references resolved locally and no TaleWorlds assemblies distributed.
- Added deterministic troop/item generation and an installable content-showcase fixture, plus a source-level static Gauntlet page example. Factions and live-engine UI claims remain deferred until a verified schema/runtime case exists.
- Replaced the retired Harmony report surface with Forge-owned hook/patch diagnostics. Optional compatibility inspects only a compatible exact `0Harmony` assembly already supplied as loaded; it is request-triggered reflection only and does not load, patch, unpatch, or reorder Harmony.
- Corrected stable-ID scheduling advice across code, tests, maps, skills, and agent guides. Stable buckets can vary in size; filtering still scans the entire input collection in O(N), and no general performance improvement is claimed.
- Synchronized bilingual technical guides and common agent instructions; specialist procedures remain in the relevant skills. The protected ledger is appended without rewriting earlier records.

## Validation and evidence limits

- `tools/Run-CalradiaForge-Tests.bat --no-pause`: clean builds (0 warnings/errors), Core 410/410, ForgeWeave 73/73, Desktop 65/65, WPF render 295 cases and 308 layout/render passes. The 24,476 ms WPF number is harness time, not live application latency.
- `tools/Verify-CalradiaForge-StatelessBehavior.bat --portable`: all four acceptance criteria passed.
- `tools/Run-CalradiaForge-Python-Checks.bat --no-pause`: Ruff and maintained Python, image, asset, and structural checks passed.
- `tools/Test-CalradiaForge-Developer-Onboarding.bat --no-pause`: local SDK/template package, isolated template installation, generated `net472` module restore/build all passed with zero build warnings/errors.
- `tools/Test-CalradiaForge-ContentShowcase.bat --no-pause`: deterministic content checks, local schema validation, module build, synthetic benchmark, Core and ForgeWeave checks passed. Benchmark figures describe its synthetic harness only.
- `tools/Validate-CalradiaForge-Skills.bat` validated 26 modified specialist directories. Documentation parity was 42/42 before this append; the protected chain is verified after appending Rev119.
- No live Bannerlord session, campaign, battle, real Harmony installation/coexistence, public NuGet publication, or IDE marketplace integration was tested. Optional Antigravity dependency installation remains unverified because its package mirror previously returned `InvalidChunkLength`.

Product version remains 25.2.0; `ForgeApi.Version` remains 13, the request envelope remains v1, and patch-diagnostics protocol remains v2. The results above are source/build/test evidence, not proof of in-game rendering, performance, or third-party coexistence.
