# Rev105 — Bounded recovery for unchanged atomic replacement failures

**Date:** 1 October 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** SDK settings persistence, Core JSON persistence, and regressions following the hook completion audit.

## Observed problem and technical justification

Rev104 retained an unresolved storage observation. The tenth diagnostic Core run captured `HRESULT=80070497` in `ModSettings.TrySave`, identifying Win32 `ERROR_UNABLE_TO_REMOVE_REPLACED` (1175). The separately observed JSON replacement failure had the same Windows error text. This identifies the failed storage operation, not the process responsible for it.

Microsoft's ReplaceFileW contract states that error 1175 leaves both files under their original names. Errors 1176 and 1177 can have different outcomes and must not be treated as the same recoverable state. Source: https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-replacefilew .

## Technical solution and architectural decisions

An internal shared `AtomicFileReplacement` helper repeats only HRESULT 80070497, only while both staged and destination files exist, and at most three times after the initial attempt. Delays are 20, 40, and 60 ms, with a maximum requested wait of 120 ms. It preserves `File.Replace`; there is no delete/move fallback or retry of uncertain outcomes. Persistent errors retain and propagate the original exception. Caller cleanup and cache-after-commit ordering remain unchanged.

Core `Json.Save` and SDK `ModSettings.TrySave` use this helper. Serializers still run outside the per-mod commit lock. The bounded waits occur during a failing commit; normal successful saves add no delay. This does not promise recovery from persistent filesystem locks, access denial, other processes, or power loss.

## Changes to code and records

- Added the internal helper without changing public API or SDK capability version.
- Added a deterministic regression with injected replacement and delay operations, real staged files, and checks for success after transient refusals, persistent refusal budget, exact exception retention, original/staged content preservation, missing source, and no retry for errors 32, 5, 1176, and 1177.
- Preserved the existing concurrent case-alias acceptance test and strengthened diagnostics with HRESULT. The JSON assertion preserves the original exception even if inspecting retained content fails.
- Rev104 remains immutable and records the earlier uncertainty. This bilingual annex records the subsequently captured cause and its bounded correction. No game session, TPAC, package, ZIP, or product version change.

## Validation and evidence limits

`tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` compiled with zero warnings/errors and passed Core 402/402, ForgeWeave 73/73, and the isolated serial detour/hook fixture. All test execution remains through BAT, without directly starting a fixture executable. Deterministic fault injection proves the retry decision and bounds; successful filesystem runs do not establish which external actor caused the earlier error or guarantee against every future storage failure.

The backend review accepted by the user is recorded in Rev104. Experimental hook and raw-detour concurrency limits remain unchanged. Remote validation must be checked against the exact pushed SHA before reporting delivery.
