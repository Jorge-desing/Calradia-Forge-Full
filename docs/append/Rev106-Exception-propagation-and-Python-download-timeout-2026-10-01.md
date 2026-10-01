# Rev106 — Exception propagation and Python download timeout

**Date:** 1 October 2026
**Version:** Calradia Forge 25.2.0, unchanged
**Scope:** Completion-review regressions and Python bootstrap resilience.

## Observed problem

The final review found that the atomic-replacement regression checked exception identity only inside catch handlers. A helper that incorrectly swallowed a failure could therefore satisfy the remaining assertions. Separately, GitHub documentation run 36832115292 failed before its audit while downloading the lxml 6.1.3 Windows wheel: pip reported a read timeout from files.pythonhosted.org after 3.1 of 4.0 MB. This is a download failure, not a ledger validation result.

## Correction

The persistent-error, non-retryable-error and missing-source cases now require the caught instance to equal the injected exception after each call. Production replacement behavior is unchanged. Both Python setup profiles set pip's socket timeout to 120 seconds and its connection retry limit to five. This does not guarantee recovery of an interrupted response body or an unavailable package service; installation failures still propagate.

## Validation and limits

The Core BAT passed 402/402, ForgeWeave 73/73, and the isolated serial detour/hook fixture after strengthening the assertions. Python setup and the ledger BAT are checked separately before pushing. Remote checks must complete for the correction's exact SHA before delivery. No API, dependencies, product version, game resources or ZIPs change; previous records remain immutable.
