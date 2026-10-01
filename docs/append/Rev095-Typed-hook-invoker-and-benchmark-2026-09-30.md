# Rev095 — Typed hook invoker and dispatch benchmark

**Date:** 30 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Core runtime Prefix/Postfix hook dispatch, isolated detour fixture, benchmark tooling, and documentation.

## Observed problem and technical justification

The normal hook dispatch path called `Delegate.DynamicInvoke` for every callback. That reflection-based invocation added per-call overhead and allocations to Prefix/Postfix execution on the target's calling thread. The change was limited to this measured dispatch path; it does not claim to improve Bannerlord frame time or the full game integration.

## Technical solution and architectural decisions

At registration, Core now caches the delegate parameter types and compiles a strongly typed original-delegate invoker with `DynamicMethod`. The generated invoker performs the required argument casts/unboxing and return boxing without `DynamicInvoke` on the normal path. Dispatch reuses the callback argument array and clones the original arguments only when a Prefix needs them. The dynamic-invocation fallback remains only for an unexpected missing dispatch entry. The compiled invoker is cleared when the entry is removed.

The hook lifecycle and explicit Apply/Revert contract are unchanged. Prefix and Postfix still execute synchronously on the target caller's thread. The backend remains experimental: these serial fixtures do not suspend other threads or prove that a target is quiescent during code modification.

## Changes to assets, code, and dependencies

- Added a typed callback invoker to `src/CalradiaForge.Core/ForgeHookService.cs`, caching registration metadata and avoiding `Delegate.DynamicInvoke` in normal dispatch.
- Extended the isolated `net472` detour fixture for callback ordering, duplicate IDs, instance targets, return-value changes, `void` callbacks, and exception propagation.
- Added `tools/Benchmark-CalradiaForge-Hooks.bat` and the fixture's opt-in `--benchmark` mode. The benchmark reports direct and hooked calls, elapsed time, AppDomain allocation counters, Gen0 collections, Apply/Revert duration, and first-call cost.
- Updated the Patch Blueprint guide in both languages and recorded the third-party notices for the pinned hook dependencies.
- Product version remains 25.2.0 and `ForgeApi.Version` remains 12. Public API, IPC, game routes, and ZIPs were not changed or regenerated.

## Validation and evidence limits

- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: Core 394/394 and ForgeWeave 73/73 passed; builds reported zero warnings and zero errors.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause`: Desktop 65/65 passed; build reported zero warnings.
- `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat`: the disposable `net472` x64 fixture built with zero warnings and errors and passed typed-invoker, callback-ordering, duplicate-ID, restoration, and exception checks. No fixture `.exe` was started directly.
- `tools/Benchmark-CalradiaForge-Hooks.bat`: one pre-change BAT run recorded five within-run samples; p50 was 773.1 ns / 401.08 B per call for Prefix, 656.0 ns / 401.33 B for Postfix, and 657.9 ns / 401.08 B for both. Five complete post-change BAT runs produced median per-run p50 values of 218.5 ns / 160.5 B, 159.5 ns / 128.45 B, and 218.6 ns / 160.56 B, respectively: approximately 71.7%, 75.7%, and 66.8% lower p50 time. This compares one baseline run containing five samples with the median across five post-change BAT runs; it is not a matched five-run-before/five-run-after study. These timings and allocation counters measure the isolated `net472` fixture/harness, not Bannerlord, frame time, or live application latency.
- No live Bannerlord or Modding Kit hook session was measured. The serial fixture does not establish thread-quiescence or safety while another thread executes the target; the backend remains experimental.
- The source annex and changelog are recorded as Rev095, but the protected DOCX and integrity chain remain at Rev076. Reconciliation is pending: append sources for Rev086–Rev092 are missing. The DOCX generator and integrity registrar were deliberately not run, so this entry is not yet present in the protected registry.

This annex documents the measured hook-dispatch optimization while preserving the prior revision history. Protected-ledger registration remains pending reconciliation of the missing revisions.
