# Rev068 — Patch-engine page safety and reconnect follow-up

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** SDK, Core, patch preflight, regression tests, and bilingual SDK documentation.

## Observed problem and technical rationale

The executable-byte writer changes protection for the 13-byte detour span and restores one protection value returned for its first address. A span crossing a system page boundary could therefore restore the first page's value over a neighboring page. Lifecycle review also found that disconnecting and reconnecting the same registry left its built-in patch service permanently closed. Finally, generic parameter names alone were not a precise identity: a declaring-type parameter and a method parameter can both be named `T` while representing different signature slots.

## Technical solution and architectural decisions

- Reject null addresses, nonpositive lengths, range overflow, and any write crossing a system page boundary before calling `VirtualProtect`. A write ending exactly at the page boundary remains supported.
- Add the separate optional `IForgePatchServiceLifecycle` interface instead of adding a required method to `IForgePatchService`, so existing service implementations remain compatible. The built-in service refreshes all records and resumes only when every record is `Reverted`, every original byte snapshot exists and matches, and no target remains tracked. Connection invokes this capability before publishing the incoming registry or reopening the shared detour gate.
- Represent generic parameter references with owner-kind/position tokens: `!0` for a declaring-type parameter and `!!0` for a method parameter. Preflight compares these tokens instead of accepting matching names.
- Regressions cover a one-page write, a rejected cross-page write with no protect/write/flush calls, safe same-instance reconnect, repeated connect, conflict-blocked reconnect, and generic parameter scope mismatch.

## Changes to code and dependencies

Changes are limited to detour range validation, the optional SDK lifecycle capability and its built-in forwarding implementation, preflight generic signature identity, tests, and matching English/Spanish patch-blueprint and SDK guidance. No runtime dependency, `IForgeRegistry` member, IPC write action, ForgeWeave behavior, game version, product version, or distribution archive changed. `ForgeApi.Version` remains 11.

## Validation and evidence limits

- `cmd.exe /c "tools\Run-CalradiaForge-Core-Tests.bat -NonInteractive <nul"` passed: Core 363/363 and the disposable x64 fixture confirmed the serial result `15 → 32 → 15` with exact revert.
- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` passed with clean `net472` and `net8.0` builds, 0 warnings, 0 errors, Core 363/363, the serial x64 detour fixture, and ForgeWeave 73/73.
- One earlier integrated attempt reported a Core failure that did not recur in the isolated Core run or the subsequent full integrated retry. The final accepted integrated result is the passing retry.
- No Bannerlord or Modding Kit runtime session was started. Fixtures call the target serially around writes; they do not prove safety when another thread may execute the method during a code write. The detour backend remains experimental.

This appendix supplements Rev067 without modifying earlier revision paragraphs. Product version remains 25.2.0; SDK capability version remains 11; ZIPs remain unchanged.
