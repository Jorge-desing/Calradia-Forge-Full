# Rev075 — Context-gated hook dispatch and WPF recovery

**Date:** 30 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Core hook dispatch, Bannerlord host planning, Desktop Hook Workbench, tests, and bilingual documentation.

## Observed problem and technical justification

The main-menu gate restricted Apply and Revert, but an already installed detour could still invoke its custom callbacks after the host left that context. The WPF workbench also could not select `Conflict` or `Failed` hooks for an explicit recovery attempt, and a multi-hook Apply plan did not disclose that an earlier hook may remain applied if a later hook fails.

## Technical solution and architectural decisions

Hook dispatch now rechecks the same host policy before running either callback. If the policy is false or throws, dispatch calls the original target unchanged. The detour remains installed until explicitly reverted; callbacks resume when the approved game-thread main-menu context returns. Apply/Revert management remains restricted to the exact main-menu screen and is not treated as a guarantee that an installed detour disappears when leaving the menu.

The host and WPF plan validators now allow explicit Revert attempts for `Conflict` and `Failed` snapshots, in addition to `Applied`. The workbench shows a persistent localized hook-lifetime warning and a plan warning that batch Apply is sequential and non-atomic. Recovery still requires the normal single-use plan confirmation and the approved host context.

## Changes to assets, code, and dependencies

- Added a per-invocation context guard to the callback dispatcher without changing the public hook contract or API version.
- Added serial x64 fixture coverage for callbacks skipped outside the host gate, restored callbacks when the gate reopens, valid-result cancellation of a non-void target, and exact target restoration.
- Added WPF recovery selection for `Conflict`/`Failed`, enabled matching host-side revert plans, and localized the lifetime and partial-Apply warnings in all 13 Desktop dictionaries.
- Updated the English and Spanish Patch Blueprint guide and the shared Bannerlord lifecycle guidance, including the `hook-plan-cancel` action.
- Product version remains 25.2.0; `ForgeApi.Version` remains 12. No distribution ZIPs were regenerated.

## Validation and evidence limits

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause`: Core 390/390 and ForgeWeave 73/73 passed; `net472`, `net8.0`, and test dependency builds reported 0 warnings and 0 errors. The nested `Run-DetourFixture.bat` completed its serial x64 lifecycle, callback-gate, void/non-void Prefix, reapply-order, uncertainty, and raw patch restoration checks. The fixture executable was not launched directly.
- `tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause`: Desktop 65/65 and WPF render/resource checks 292/292 passed; the harness reported 182 render/layout passes.
- No live Bannerlord or Modding Kit session was used. Serial fixture success does not prove safe Apply/Revert while another thread executes the target; the backend remains experimental.

This annex extends the protected improvement record without replacing earlier revisions.
