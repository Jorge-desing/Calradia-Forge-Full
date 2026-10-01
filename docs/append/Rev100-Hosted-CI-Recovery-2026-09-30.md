# Rev100 — Hosted CI recovery

**Date:** 30 September 2026  
**Version:** Calradia Forge 25.2.0; unchanged  
**Scope:** CI, Python tooling, asset and documentation audits.

## Observed problem and technical rationale

The hosted solution build included Bannerlord-bound projects without licensed TaleWorlds assemblies or a configured GameBin. Core already guards engine-only sources and intentionally supports net472 and net8.0; removing its portable target would break the existing architecture. Auditors also retained assumptions about mutable patch records, a single Desktop simulation file and simultaneously visible Gauntlet workspaces.

## Technical solution and architectural decisions

Hosted CI uses CalradiaForge.Portable.slnf and the existing BAT launcher for portable build and tests. Full local builds retain game integration when GameBin is available. Ledger audits gather all DesktopSimulationViewModels partials and check current immutable snapshots. Decorative overlap analysis excludes only visibility pairs verified from the ViewModel source and supports the current Campaign Rule Builder margin clause; a fixture covers inherited visibility and exclusivity. No framework map or runtime dependency changed in this CI correction.

## Code, assets and dependencies

Updated CI workflows, the test BAT, Python audits and asset fixtures. No proprietary game binaries, caches, test captures or release ZIPs are included in the intended changeset.

## Validation and evidence limits

- Full test BAT: clean Release build, 0 warnings/errors; Core 401/401, ForgeWeave 73/73, Desktop 65/65, WPF render 292/292. The serial x64 detour fixture applied and reverted successfully (15 → 32 → 15).
- Portable test BAT: build, assets, ForgeWeave, Desktop and WPF render passed.
- Python CI BAT: Ruff, 12 asset fixtures, 5 image tests, decorative sprites and icon metadata passed.
- Ledger BAT audits passed before this annex; the appended ledger will be checked again before commit.
- Optional agent dependency installation failed locally because the configured package mirror returned a truncated chunk. That profile is not reported as verified locally.
- Before upload, public Actions history contained 35 failed CI runs, 4 failed ledger runs and 11 failed legacy Conda runs. A push triggers new checks; historical conclusions cannot be rewritten. Remote verification remains pending at this annex.
- No Bannerlord, campaign or battle was launched. Hosted portable validation does not prove game integration or concurrent detour safety.

This annex preserves all previous ledger paragraphs and product version.
