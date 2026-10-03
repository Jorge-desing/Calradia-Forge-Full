# Rev131 — Python unittest summary parser

**Date:** 2026-10-02

**Product version:** Calradia Forge 25.2.0, unchanged; `ForgeApi.Version` 13

**Scope:** Development tooling, Python output summarization, regression tests, and documentation.

## Observed issue and technical rationale

The final review of the Rev130 CI repair found that its regression fixture used `Ran 8 tests ...` immediately followed by `OK`. Python's standard `unittest` runner can place a blank line between those lines, and it uses the singular form `Ran 1 test ...` for one test. The first parser adjustment therefore still missed a normal successful output shape.

## Technical solution and architectural decisions

The solution-test summary parser accepts one or more line breaks and horizontal indentation before the explicit `OK` success marker. It recognizes both `test` and `tests` in the runner summary. Regression fixtures cover a single line break, a blank separator, and CRLF with the singular form. Existing success/error classification remains unchanged.

## Changes to assets, code, and dependencies

- Updated `agents/compactor.py` to parse the observed Python runner output variants.
- Expanded `tests/test_forge_tool_audits.py` to guard each line-ending and singular/plural shape.
- Added bilingual changelog entries and this append-only revision. No runtime SDK, product dependency, public API, Harmony integration, or game code changed.

## Validation and evidence limits

- `tools\Run-CalradiaForge-Python-Checks.bat --ci --no-pause` passed after the correction: Ruff, 25 asset-pipeline tests, 5 image-pipeline tests, 20 tool-audit tests, 15 offline-orchestrator tests, 3 CLI launcher tests, and decorative-sprite/TPAC-marker source checks.
- `tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` verified bilingual parity across 42 document pairs and the 130-record chain before this new append; the append launcher and integrity verifier will validate Rev131.
- The optional `--agents` suite cannot run locally because the Antigravity profile is unavailable; a prior local pip attempt ended with `InvalidChunkLength`. The hosted post-fix run remains pending.
- No live Bannerlord, campaign, battle, TPAC pixel decoding, or in-game rendering is claimed.

This annex adds a new revision without rewriting Rev130 or earlier records. It preserves the product version, routes, public contracts, and game behavior.
