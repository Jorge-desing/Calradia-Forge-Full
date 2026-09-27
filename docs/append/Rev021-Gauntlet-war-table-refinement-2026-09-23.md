# Rev021 — Tactical Gauntlet war-table refinement

**Date:** 2026-09-23  
**Release line:** Calradia Forge 22.0.0  
**Scope:** In-game Gauntlet panel layout, generated decorative sprite sources, and structural validation.  
**Distribution:** Source follow-up only; the existing ZIPs were not changed or regenerated.

## Gauntlet layout and navigation

The generated prefab now arranges the panel as a compact war table: a header holds the Forge seal, title, session state, and a restrained decorative frame; the eight existing destinations are presented in a vertical left rail; the main area contains the current-section context, status cards, argument/command deck, and an untextured evidence ledger; action and pagination rows remain below the evidence viewport. Existing routes, command bindings, evidence text, and game-state protections are retained. Navigation buttons expose their active state through the existing ViewModel selection properties.

The two editable fields that bind to `@Argument` now have distinct control IDs (`ForgeCommandArgument` and `ForgeAssemblyPath`). Their shared ViewModel property remains unchanged.

## Original decorative sprite sources

`tools/generate_assets.py` deterministically creates two small, low-alpha PNG sources from in-repository geometry: `forge_header_filigree.png` (128 × 128) for the header and `forge_pine_grain.png` (128 × 32) for the rail divider. Their names are registered in `modules/CalradiaForge/GUI/CalradiaForgeSpriteData.xml` and referenced by the generated prefab. These are original project resources; no external artwork, runtime service, or dependency was added.

The PNG sources, SpriteData entries, and prefab references have passed the source-level decorative-sprite validator. This does not demonstrate atlas generation, TPAC creation, or in-game rendering. The Resource Browser atlas/TPAC rebuild and a live visual check remain pending, so the new decorations are not recorded as runtime-verified.

## Validation and release status

`CalradiaForgeTests.bat --core-only --no-pause` completed with 232 passing assertions, and the ForgeWeave suite reported 31 passing tests. `Validate-CalradiaForge-DecorativeSprites.bat --no-pause` passed its structural checks for the PNG sources, SpriteData, and prefab. No live game render or Resource Browser import was verified. The product remains 22.0.0; no ZIP was regenerated.

This record is append-only. It does not revise Rev020 or earlier evidence, and it does not claim that a source PNG is a compiled or loadable Bannerlord texture.

## Follow-up evidence — source atlas generation

After this record was first appended, the official TaleWorlds `SpriteSheetGenerator` was run against an isolated staging copy of the Calradia Forge module. It produced a new source atlas and SpriteData including both decorative sprites. `Validate-CalradiaForge-DecorativeSprites.bat --no-pause` now verifies that the atlas dimensions match metadata and that each decorative sprite's atlas pixel crop exactly matches its source PNG. The source-level and placement checks pass.

The runtime TPAC still predates the regenerated atlas. No Resource Browser import was performed, and no in-game rendering is claimed; runtime texture loading and visual inspection remain pending. Existing backup files were retained. No ZIPs were regenerated.
