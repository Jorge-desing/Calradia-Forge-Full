# Rev125 — Diagnostic evidence and crash-report correction

**Date:** 2026-10-02  
**Product version:** Calradia Forge 25.2.0, unchanged  
**Scope:** JSON crash-report correctness, evidence boundaries, agent documentation and regression coverage.

## Findings and changes

Review found that `SubModule.OnUnhandledException` manually escaped only quotes and line feeds, so a Windows path or other JSON control character could make the `.cfcrash` report invalid. The handler now serializes the same `Timestamp`, `IsTerminating`, and `Exception` fields with the Newtonsoft.Json assembly already referenced by the game module. A regression invokes the production formatter and parses a report containing a Windows path, quotes, CR/LF, a tab and a control character. The Core BAT now invokes the existing F10 edge-gate suite as well, so that test is no longer dormant.

The debugging skill and Desktop crash-analysis copy now describe `.cfcrash` as exception text, with `.dmp` and `.sav` limited to file metadata where applicable. The autonomous-agent guides match the configured `BugHunterAgent` tool list; the CODEX Core test command uses the launcher’s supported non-pausing flag. Historical 25.0.0–25.2.0 validation reports remain unchanged. The SDK evolution guide clarifies that their earlier anti-lag and zero-allocation phrases do not represent a measurement of the complete campaign callback; the synthetic `ProcessBatch` harness does not establish in-game performance.

## Validation and limits

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause` passed with clean `net472` and `net8.0` builds (0 warnings, 0 errors), Core 414/414 and ForgeWeave 73/73. This includes the F10 edge and crash-serialization regressions.
- `tools\Run-CalradiaForge-Desktop-Tests.bat --no-pause` passed 65/65.
- `tools\Validate-CalradiaForge-Skills.bat .agents\skills\debugging-master` reported the skill valid.
- No Bannerlord or Modding Kit process was started. These checks do not verify live F10 delivery or rendering in game.
- Full package/hash audit and protected-record integrity verification are performed after this append.

This entry is append-only; product version 25.2.0 and public SDK contracts are unchanged.
