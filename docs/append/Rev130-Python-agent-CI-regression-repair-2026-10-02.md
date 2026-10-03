# Rev130 — Python agent CI regression repair

**Date:** 2026-10-02  
**Product version:** Calradia Forge 25.2.0, unchanged; `ForgeApi.Version` 13  
**Scope:** Development tooling, Python agent diagnostics, tests, and documentation.

## Observed issue and technical rationale

The hosted Windows run for commit `7cb521be249a88a1ef8150d5e74eb7094e06b7ae` successfully installed the optional Antigravity profile and completed the portable build/test and stateless gates. Its final Python step failed three tests in `tests/test_forge_agents.py`: an offline report assertion still called five configured specialist roles active subagents, a documentation-parity assertion expected the obsolete word `matched`, and a successful solution-test fixture expected an asset count that the compactor did not emit.

The logs showed that the current offline report accurately says `Configured Specialist Roles` and `Cloud-Agent Execution: not used on this route`; the parity audit says `100% parity ... and matching Spanish counterparts`. The asset parser used `.*OK`, which did not cross the newline in Python's standard `Ran N tests ...` then `OK` output.

## Technical solution and architectural decisions

The compactor now parses the standard two-line `unittest` success format while requiring the explicit `OK` marker. The regression fixture verifies that the resulting summary preserves the asset test count. Agent tests now assert configured roles and the explicit absence of cloud-agent execution in the offline path; parity assertions match the report's actual wording and accept a changing guide count.

No runtime Forge code, Harmony integration, product dependency, or game behavior changed. This correction does not imply that locally configured agent roles are running as independent subagents.

## Changes to assets, code, and dependencies

- Updated `agents/compactor.py` to recognize `OK` on the next line after `Ran N tests`.
- Corrected stale assertions in `tests/test_forge_agents.py` and added a base-tooling regression in `tests/test_forge_tool_audits.py`.
- Added bilingual changelog entries. No assets, package manifests, SDK signatures, or game dependencies changed.

## Validation and evidence limits

- `tools\\Run-CalradiaForge-Python-Checks.bat --ci --no-pause` passed after the fix, including Ruff, 25 asset-pipeline tests, 5 image-pipeline tests, SDK-independent tool-audit tests, offline orchestration tests, the CLI launcher suite, and structural sprite/TPAC-marker checks.
- Before the correction, the hosted optional agent suite ran and reported exactly the three assertion failures above; its setup log confirms that `google-antigravity 0.1.20` and `google-genai 2.28.0` were installed successfully on the runner.
- The local `--agents` profile could not be installed because pip returned `InvalidChunkLength`. A hosted rerun after this correction is pending at the time of this append.
- Static checks confirm source/package boundaries only. No live Bannerlord, campaign, battle, TPAC pixel decode, or in-game UI validation is claimed.

This appendix adds evidence without replacing previous revisions. It preserves existing commands, routes, contracts, and product version.
