# Rev111 — Hook Revert Eligibility, Confirmation Visibility and IL Rebuild Recovery

**Date:** October 1, 2026  
**Version:** Calradia Forge 25.2.0, unchanged; ForgeApi.Version 13  
**Scope:** Console hook planning, Desktop confirmation visibility, Core IL reconstruction, read-only diagnostics compatibility, tests and bilingual documentation.

## Observed problem and technical rationale

A console Revert selection by owner or `all` included Registered and Reverted records alongside active hooks. The strict runtime planner rejected that mixed selection, preventing otherwise eligible removal from being planned. Desktop filters could also hide retained selections without making their count clear, and a long confirmation preview was difficult to review within its bounded surface. An applied IL transformation needed to survive later MonoMod chain reconstruction outside the menu without permitting new hook management there. The public read-only `HarmonyDiagnostics` compatibility surface also needed restoration for existing consumers.

## Technical changes and architectural decisions

- Console `cf.hook_revert` resolves only registered IDs/owners and filters the selection to Applied, Conflict or Failed snapshots before preparing a plan. If no eligible records remain, it reports an explicit empty outcome without preparing or confirming an operation. Runtime state checks, selected-ID scope and the separate single-use confirmation remain enforced.
- The Desktop workbench displays hidden selection counts and wraps the confirmation preview within a bounded review area. These changes make retained selection and the proposed operation visible without applying hooks automatically.
- IL reconstruction uses the applied activation's scope. A later chain rebuild can reconstruct an explicitly applied transformation outside the menu; new Apply/Revert management remains blocked there. Registration and preflight remain inert, and IL manipulators remain local trusted code rather than SDK/IPC executable payloads.
- The public `HarmonyDiagnostics` compatibility surface is restored as a read-only diagnostic API. It does not apply, remove or reorder patches, and it does not add an executable Harmony patching backend.

## Source and dependency scope

The changes affect console selection in `ForgeCommands.cs`, Core hook activation/rebuild handling and diagnostics, Desktop confirmation presentation, and focused regressions. Product version stays 25.2.0 and SDK API stays 13. This annex does not amend historical Rev001–Rev110 or claim new dependency certification.

## Validation and evidence limits

- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` passed Core 405/405 and ForgeWeave 73/73 with clean selected builds. Retained output: `artifacts/hook-rev111-core.txt`.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` passed Desktop 65/65 and WPF 294 render cases, with 182 render/layout passes and a console harness summary of 14,410 ms. Retained output: `artifacts/hook-rev111-desktop.txt`. This harness duration is not interactive application latency.
- `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat --no-pause` passed in the isolated x64 serial host, including the IL rebuild regression 11 → 15 → 11 while management remained blocked outside the approved menu. These serial fixtures do not certify safety while another thread executes a target during detour changes.
- Live Bannerlord hook mutation and Resource Browser mutation remain unverified. No campaign or battle was used for this validation. Passing managed tests and isolated fixtures do not establish in-game rendering, native import or live host-lifecycle correctness.
- The three current-version distribution ZIPs will be regenerated after this revision and its integrity append. This annex asserts no final ZIP hashes or archive-audit result.

This revision appends evidence to Rev110 while preserving prior protected revisions.
