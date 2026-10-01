# Rev072 — Detour fixture BAT host and patch copy correction

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Detour fixture launcher, Gauntlet patch guidance, native language catalogs.

## Corrections

- The isolated detour fixture is a `net472` x64 library. `Run-DetourFixture.bat` builds it and loads its test entry point into a disposable x64 Windows PowerShell process. The fixture no longer emits or starts `CalradiaForge.DetourFixture.exe`, and the BAT fails closed if an apphost appears.
- A trial conversion to a .NET 8 executable DLL was rejected: under that JIT, the serial fixture did not observe the detour at stage 4. The test returned to `net472`, matching the Bannerlord runtime. The failed trial is not counted as passing evidence.
- Gauntlet copy now separates read-only Patch Blueprint Preflight, the historical read-only Harmony Atlas, and ForgeWeave replay. Seven corrected copy keys are translated in all 13 native catalogs.

## Validation evidence

- The fixture BAT completed all six stages in a disposable x64 host, returned `15 → 32 → 15`, and verified exact revert.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` built cleanly with 0 warnings and 0 errors. ForgeWeave passed 73/73, Desktop 63/63, and WPF rendering passed 292 cases. The latest render measured 14,399 ms in the harness; this is not application interaction latency.
- The localization regeneration reported 13 catalogs with 737 keys each. The generated localization audit reported `valid: true`.
- No Bannerlord or Modding Kit session was started. No package generation was invoked in this correction pass. The serial fixture does not establish concurrent-execution safety; the backend remains experimental. `ForgeApi.Version` stays 11.
