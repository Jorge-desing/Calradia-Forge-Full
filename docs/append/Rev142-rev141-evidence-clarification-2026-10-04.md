# Rev142 — Rev141 Accessibility and Evidence Clarification

**Date:** 2026-10-04

**Version:** Calradia Forge 25.2.0; Forge API 13 unchanged

**Scope:** Documentation erratum for WPF accessibility wording and repeatable validation evidence. No product source or behavior changes.

## Observed issue and rationale

The protected Rev141 appendix broadly grouped the localized cycle hint with controls receiving new UI Automation names/help. Current XAML shows that the cycle hint is localized text; the favorites toggle has state-specific localized automation name/help, and the footer Ping button has localized name/help while retaining its command and AutomationId. The original Rev141 statement therefore needs a precise interpretation without editing its protected document.

Some retained Rev141 artifacts also came from earlier reruns. A final integrated run was captured separately so current test counts and render-harness measurements can be tied to one named log and JSON report.

## Technical clarification and decisions

- Rev141 localized the cycle hint and favorites label. It added state-aware accessible name/help and tooltip to the favorites filter, and accessible name/help to Ping. It did not add separate UI Automation properties to the cycle hint itself.
- The final integrated run is `artifacts/rev141-full-suite-final4.txt`; its WPF render report is `artifacts/desktop-render-rev141-final4.json`. The BAT console reports 25,994 ms for its render suite; the JSON report records 25,997 ms total and 11,405 ms in 320 render/layout passes. These are harness measurements, not application latency.
- Because Rev141 and its hash-chain entry are already protected, this clarification is appended as Rev142; prior document paragraphs and hashes are not changed.

## Files and evidence

- Updated the shared wording in `AGENTS.md`, `CODEX.md`, and `GEMINI.md`, and the WPF accessibility guidance in `.agents/skills/calradia-forge-desktop/SKILL.md`.
- `tools/Run-CalradiaForge-Tests.bat --no-pause`: 0 warnings/errors, Core 436/436, ForgeWeave 74/74, Desktop 75/75, WPF 296 cases and 320 layout/render passes passed.
- `tools/Test-CalradiaForge-Desktop-Uia.bat`: read-only inspection passed 23/23 observed records. This is structural UI Automation evidence, not visual approval.
- Bannerlord was not launched. No live in-game behavior is claimed. Product/API versions, contracts, dependencies, routes, permissions, and target frameworks remain unchanged.

This appendix supplements Rev141 without replacing it or any earlier protected revision.
