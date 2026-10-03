# Rev118 — Documentation parity gate correction

**Date:** 2 October 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Tools, agent tests, documentation rules and skills. Correct English/Spanish counterpart classification and enforce the documentation audit result as a required gate.

## Observed issue and technical rationale

The documentation audit treated English `SYSTEM_DESIGN.md` as a Spanish alias for `ARCHITECTURE.md`. That could report parity while `SYSTEM_DESIGN.es.md` or another actual counterpart was missing. In addition, the ledger runner printed an incomplete parity report without failing. The source tree had 42 English guides once both architecture and system-design documents were counted correctly.

## Technical solution and architectural decisions

The audit now treats architecture and system-design documents as standard English/Spanish pairs and reserves aliases only for the established Desktop and versioned validation records. The ledger runner requires the explicit success marker emitted by the audit. Regression tests exercise complete architecture pairs, a missing system-design counterpart, missing translations through the runner, and the current 42/42 set.

## Changes in assets, code and dependencies

- Removed the false `ARCHITECTURE.md` to `SYSTEM_DESIGN.md` alias and the classification that treated `SYSTEM_DESIGN.md` as Spanish.
- Added regression coverage for the corrected pairing and mandatory runner gate.
- Synchronized the documentation gateway, documentation rule, release-validation skill and the `AGENTS.md`, `CODEX.md` and `GEMINI.md` guidance.
- No product version, SDK API, dependencies, routes, or game behavior changed.

## Validation and evidence limits

- `tools/Run-CalradiaForge-Python-Checks.bat --no-pause`: Ruff passed; Asset Pipeline 25/25; image tests 5/5; agent-tool audit regressions 10/10; sprite/resource structural checks passed.
- `tools/Run-CalradiaForge-Python-Checks.bat --ledger --no-pause`: documentation parity 42/42, zero missing counterparts; the prior protected ledger chain verified through revision 117 before this append.
- `tools/Validate-CalradiaForge-Skills.bat` passed for the modified specialist skills.
- This validates repository documentation and static tooling only; it does not establish live Bannerlord behavior.

This annex extends the previous protected revision without rewriting it. Product version and public contracts remain unchanged.
