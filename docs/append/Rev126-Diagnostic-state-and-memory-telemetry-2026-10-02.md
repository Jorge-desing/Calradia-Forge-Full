# Rev126 — Diagnostic state and accepted memory telemetry

**Date:** 2026-10-02  
**Product version:** Calradia Forge 25.2.0, unchanged  
**Scope:** SDK and Core diagnostic semantics, bounded campaign-memory telemetry, Desktop simulation evidence, and project-agent guidance.

## Observed issue and technical justification

The external patch inspector used the `Truncated` flag for some collection failures, although no output had necessarily been clipped. The campaign memory behavior incremented telemetry even when the SDK rejected a write after reaching its global agent quota. The time-slicing helper had no explicit test for null or empty IDs, and the Desktop memory-inspection regression still expected an older live-runtime/verified contract although the service returns an illustrative sample.

## Technical solution and architectural decisions

The inspector now marks output as truncated only when an actual output/collection bound clips data; query, enumeration and skipped-assembly failures remain incomplete evidence. Campaign-memory counters advance only after `TryAdd`/`TryUpsert` accepts the write. Tests record that `ForgeTimeSlicer` maps null and empty IDs to bucket zero and that callers should provide stable non-empty IDs; no runtime behavior changed. The Desktop render test now checks the existing CoALA-inspired sample wording and `Sample` evidence status rather than claiming a live registry snapshot.

## Changes to assets, code and dependencies

Updated the bounded patch-inspection result handling, campaign memory write telemetry, regression coverage for SDK time-slicing and memory quotas, Desktop simulation test expectations, and specialist knowledge for debugging, performance, simulation, UI and agent coordination. No new runtime dependency, public API, product-version change, package publication or TaleWorlds integration was introduced.

## Validation and evidence limits

- `tools\Run-CalradiaForge-Tests.bat --no-pause` passed: clean `net472`, `net8.0` and Desktop builds (0 warnings, 0 errors); Core 415/415; Patch Diagnostics 28/28; ForgeWeave 73/73; Desktop 65/65; WPF render 295 cases and 308 layout/render passes.
- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause` passed during the focused time-slicer and memory changes. The isolated patch diagnostics BAT passed 28/28.
- Render durations and layout-call durations are harness measurements, not latency of the open application. No Bannerlord process, campaign, battle, live third-party runtime, IDE integration or public NuGet publication was used to establish behavior.

This appendix adds evidence to the previous revision without replacing its paragraphs. Product version 25.2.0, public SDK contracts and existing module routes remain unchanged.
