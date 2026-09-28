# Rev076 — Illustrated Heraldic Gauntlet Renewal

**Date:** 27 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Gauntlet visual assets, generated prefab, structural audit, and bounded F10 evidence.

## Observed goal and technical rationale

The panel's header identity gap and navigation rail used earlier material/cartographic artwork. This round replaces those two presentation layers with original heraldic illustrations while keeping the existing eight-area navigation and its controls readable. The work also records the current evidence around the user's historical F10 assertion without asserting an unverified cause or repair.

## Technical solution and architectural decisions

The existing passive widget IDs now reference `forge_heraldic_header_v2` and `forge_heraldic_rail_v2`. The header retains its reserved 220×42-DIP identity-gap rectangle. The rail uses a centered, fixed 128-DIP width and a 256-DIP maximum height; its available height shrinks with the supported viewport. The new art remains a non-interactive underlay and the audit enforces alpha limits of 88/255 for the header and 64/255 for rail guidance text.

The deterministic preparation flow and official source-atlas generation produced a 4096×512 atlas with 32 sprite registrations: 17 game icons and 15 decorative sprites. The Gauntlet structural audit checks the selected sprite IDs, alpha caps, passive flags, centered rail geometry and containment across its supported viewport profiles.

## Asset, code, and dependency changes

- Added local ImageGen authoring and prepared artwork for the heraldic header and rail; updated the asset preparation inputs, generated prefab, `SpriteData`, and structural-audit expectations.
- The installed TPAC was not replaced. It predates the new source atlas; Resource Browser import and in-game rendering remain pending.
- No API, routes, commands, permissions, product version, dependencies, or ZIPs changed.

## Validation evidence and limits

- `tools/Prepare-CalradiaForge-ImageGenTextures.bat --check --no-pause`, `tools/Test-CalradiaForge-DecorativeTextureDeterminism.bat --no-pause`, and `tools/Validate-CalradiaForge-DecorativeSprites.bat --no-pause` passed.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` passed with 0 structural-audit errors and 0 warnings; its Core suite passed 339/339.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` passed Core 339/339 and ForgeWeave 73/73. The Release solution build completed with zero warnings.
- The F10 edge/layer lifecycle telemetry regression passed. A read-only comparison found identical source and installed prefab SHA-256 `5741D7571C254B6487477D601F0CFE016AA254A1DFC6BD6795766274543B2F45`; both contain the engine-canonical `<ScrollbarWidget>` and no `<ScrollBarWidget>` spelling. A persisted prior session log records opening/loading and closing the panel. This is not a fresh reproduction of the historical assertion, and its exact cause and current resolution remain unverified.
- Bannerlord was not launched. Import and live visual approval are pending; structural source checks do not establish in-game rendering.

This appendix adds evidence after Rev055 without replacing prior registry paragraphs. Product version remains 25.2.0.
