# Rev055 — Playbook Text Wrapping and Scrollbar Type Guard

**Date:** 27 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Gauntlet detailed Playbook text layout, scroll-flow audit, and exact widget-type validation.

## Observed defect and technical rationale

Some localized Playbook titles, steps, troubleshooting text, and recommended macros could exceed fixed-height rows and be clipped. The widget audit also needed to distinguish the engine's exact `ScrollbarWidget` type from the visually similar but invalid `ScrollBarWidget` spelling; a case-insensitive check could let that typo pass source validation even though Gauntlet widget names are case-sensitive.

## Technical solution and architectural decisions

The PanelViewModel now wraps the existing Playbook and troubleshooting strings at word boundaries for their available text widths without editing the source text, commands, or route catalog. The generated prefab places variable-height text and passive separators in an ordered vertical `ListPanel` with `CoverChildren`, inside a clipped `ScrollablePanel`; the macro action remains the final bounded item reachable through the scrollbar. The structural audit follows the actual ancestor chain to ensure every Playbook text node belongs to that scroll flow and checks the flow's passive-rule order and final macro control. Widget validation now accepts only the exact `ScrollbarWidget` tag and explicitly rejects `ScrollBarWidget`.

## Asset, code, and dependency changes

- Updated `src/CalradiaForge.Mod/PanelViewModel.cs`, `tools/generate_assets.py`, and `modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml` for explicit wrapping and the vertically growing, scrollable Playbook content flow.
- Extended `tools/audit_gauntlet_ui.py` and `tests/CalradiaForge.Tests/AdvancedToolsTests.cs` to check scroll-flow membership, ancestor resolution, ordering, and exact case-sensitive widget spelling.
- No public API, routes, commands, permissions, dependencies, product version, or ZIPs changed.

## Validation evidence and limits

- The first `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` run exposed an outdated decorative-audit expectation for `CoverChildren`. The audit was updated to validate the vertical order of passive rules and the macro endpoint without relaxing layout limits.
- The final BAT run exited with code 0 and reported `Gauntlet visual structural checks and core tests passed`, `RESULT: 338 passed, 0 failed`.
- The F10 session log recorded open/close toggles, but its associated capture did not show the overlay. Therefore, visible Playbook wrapping and placement for this follow-up remain pending live inspection; the structural BAT result does not constitute in-game rendering approval.

Product version remains 25.2.0. This append adds evidence after Rev054 without changing earlier ledger paragraphs.
