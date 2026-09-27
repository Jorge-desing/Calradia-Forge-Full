# Rev053 — Gauntlet F10 Scrollbar Crash Correction

**Date:** 27 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Gauntlet prefab generation, scrollbar structure audit, and runtime validation at the singleplayer main menu.

## Observed defect and technical rationale

Pressing F10 in the main menu first produced a TaleWorlds Gauntlet assertion stating that `CreateBuiltinWidget(ScrollBarWidget)` could not find the requested built-in widget. Retrying exposed an access violation in `ScrollablePanel.UpdateScrollablePanel(Single)`. Inspection found that the generated Evidence scroll panel did not follow the native prefab contract: its scrollbar reference and nested widget structure did not resolve to a valid sibling scrollbar with a matching handle.

## Technical solution and architectural decisions

The generated prefab now binds `ForgeEvidenceScroll` to the sibling `ForgeEvidenceScrollBar` through the relative `VerticalScrollbar="..\\ForgeEvidenceScrollBar"` reference. The scrollbar is a `ScrollbarWidget` sibling with `AlignmentAxis="Vertical"`, a `Handle` property, and a matching identified handle child. The generator applies the same structural contract to all five scrollable surfaces. The Gauntlet auditor verifies the five-panel inventory, relative references, sibling placement, vertical axis, handle ownership, and absence of nested scrollbar controls. The evidence-surface regression permits only the scrollbar's own handle image while continuing to reject decorative sprites in the ledger.

## Asset, code, and dependency changes

- Updated `tools/generate_assets.py` and `modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml` to emit the native-compatible scrollbar arrangement.
- Strengthened `tools/audit_gauntlet_ui.py` and adjusted `tests/CalradiaForge.Tests/AdvancedToolsTests.cs` for the scrollbar handle exception.
- No public API, route, command, permission, dependency, or product version changed. No ZIP was generated and no TPAC was replaced.

## Validation evidence and limits

- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` compiled with zero warnings/errors and passed Core 333/333 and ForgeWeave 73/73.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` passed the decorative asset and Gauntlet structural audits with zero errors and zero warnings; its Core suite passed 338/338.
- `tools/Deploy-CalradiaForge-ToGame.bat --no-pause` built and deployed the Client and Modding Kit profiles with verified file backups. The existing installed TPAC was preserved.
- In a Steam-launched Bannerlord session with Calradia Forge v25.2.0 enabled, F10 opened the panel at the singleplayer main menu and a second F10 closed it. The eight navigation areas rendered, and the game remained running without the assertion or access violation. No campaign or battle was loaded.
- This live check confirms the tested main-menu path only; it does not establish Modding Kit runtime behavior or validate unrelated scroll-panel states under all game screens.

Product version remains 25.2.0. Earlier ledger paragraphs and revisions remain unchanged.
