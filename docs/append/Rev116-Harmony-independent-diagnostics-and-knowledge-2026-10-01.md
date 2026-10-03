# Rev116 — Harmony-independent diagnostics and knowledge consolidation — 1 October 2026

**Date:** 1 October 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** SDK/Core, Desktop protocol, patch diagnostics, and agent guidance. The `ForgeApi` contract is unchanged.

## Observed problem and technical rationale

Harmony diagnostics had grown into a public Core surface, a report member, and a protocol/UI action. This blurred the boundary between optional compatibility observation and a dependency, and could imply coexistence guarantees that the observations do not establish. The onboarding knowledge also needed to distinguish verified capabilities from proposals such as IDE UI integration, hot reload, and in-game behavior.

## Technical solution and architectural decisions

- Retired the `HarmonyDiagnostics` public API and DTOs, `SessionReport.Harmony`, and the `harmony` action. Consumers must migrate to `ForgePatchDiagnostics`, `SessionReport.PatchDiagnostics`, and `patch-diagnostics`.
- The replacement retains Forge-owned diagnostics for hooks and whole-method replacement. Optional external observation uses reflection over an exact `0Harmony` assembly supplied by the caller and already loaded, and only over verified public query members. Forge adds no Harmony references, packages, or binaries; it does not load the assembly or change third-party patch state.
- `ForgeProtocol.Version` advances to 2 because the action and its models were retired; `EnvelopeVersion` remains 1. `ForgeApi.Version` remains 13 and the product remains 25.2.0.
- Limits bound enumeration and Forge-produced output. Partial results are marked incomplete. This reflection is not a sandbox: the cost and side effects inside synchronous third-party getters are not bounded. A shared target is a review signal, not a conclusive conflict or coexistence detection.
- Consolidated instructions and skills distinguish implemented capabilities, local evidence, and unverified proposals. The CLI template and static showcase do not establish Visual Studio/Rider UI integration, public NuGet publication, hot reload, or Bannerlord loading/rendering.

## Changes to assets, code, and dependencies

Updated the diagnostic and protocol migration surface together with the bilingual guide, changelogs, and agent guidance. No Harmony dependency was added, and `ForgeApi` was not changed; the project target frameworks and product version remain as before. The existing interactive analysis report is outside this revision and remains untouched.

## Validation and evidence limits

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause`: `net472` and `net8.0` builds with 0 warnings and 0 errors; Core 403/403 and ForgeWeave 73/73.
- `tests\CalradiaForge.PatchDiagnostics.Tests.bat --no-pause`: 27/27 focused diagnostic tests. These are fixtures; they do not run or certify Harmony coexistence in Bannerlord.
- BAT validation of local SDK/template onboarding and the static content showcase ran before the final diagnostics-only edits. The local smoke does not equal IDE UI integration or publication to a public feed.
- Bannerlord, a campaign, and a battle were not opened; hot reload and Gauntlet rendering in the engine were not verified. This revision did not generate distribution ZIPs.

This annex is paired with the Spanish source and records the same evidence and limits. It does not replace or rewrite earlier paragraphs in the protected record.
