# Rev054 — Responsive Detailed Playbook and Scroll Layout

**Date:** 27 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Gauntlet detailed Playbook placement, action-row fit, contextual panels, and scrollbar structural audit.

## Observed defect and technical rationale

The detailed Playbook occupied the same right-side region as action controls and rows, causing the panel to cover or compete with active content. Long Playbook material also lacked a bounded, adaptive scroll surface. The existing key-help panel could appear at the same time, compounding the overlap.

## Technical solution and architectural decisions

The detailed Playbook now sits below the context cards and reserves a right-side column so it cannot cover the active controls or rows. In detailed mode, the affected action buttons contract to 100 DIPs to preserve the usable workspace. The Playbook's content receives an adaptive-height scroll surface. Key Help and Playbook visibility are mutually exclusive, preventing both contextual panels from claiming the same column. The Gauntlet audit now checks the scrollbar contract across six scrollable surfaces, including the new Playbook surface.

## Asset, code, and dependency changes

- Updated the generated Gauntlet prefab and generator for the reserved Playbook column, detailed-mode button sizing, adaptive Playbook scrolling, and exclusive Key Help/Playbook visibility.
- Extended the structural audit to include all six scrollbar surfaces; no new atlas asset or dependency was required.
- No public API, route, command, permission, product version, or ZIP changed.

## Validation evidence and limits

- The full `tools/Run-CalradiaForge-Tests.bat` run passed; Desktop passed 63/63 and WPF render passed 289 cases.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat` passed with zero Gauntlet visual-audit errors and zero warnings, including the six scrollbar contracts.
- Live in-game validation of this additional Playbook adjustment is pending confirmation; source/build checks do not establish its rendered placement or interaction in Bannerlord. No campaign or battle was loaded for this check.
- This is a follow-up to Rev053 and adds evidence without replacing its paragraphs or claims.

Product version remains 25.2.0. Earlier ledger paragraphs and revisions remain unchanged.
