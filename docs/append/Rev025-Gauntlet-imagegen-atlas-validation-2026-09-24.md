# Rev025 — Gauntlet ImageGen atlas validation

**Date:** 2026-09-24  
**Release line:** Calradia Forge 22.0.0  
**Scope:** Source Gauntlet textures, prefab validation, and test evidence.  
**Distribution:** Source follow-up only; no TPAC or ZIP changes.

## Completed source pipeline

The four generated material masters were prepared reproducibly as RGBA8 sprites: `forge_dark_wood` (128×64), `forge_inkwash` (128×64), `forge_patina_brass` (128×16), and `forge_pine_felt` (128×32). The official TaleWorlds SpriteSheetGenerator rebuilt the source atlas and `CalradiaForgeSpriteData.xml` in an isolated module staging directory. The resulting atlas is 2048×128; SpriteData registers all 13 parts in the always-loaded category, with the declared sprite bounds validated. Previous source sprites, prefab, atlas, and metadata were backed up and hash-checked before replacement.

The decorative PNG validator passed and reported SpriteData registration as current. The Gauntlet structural auditor passed with zero errors and zero warnings, including passive decoration, control/evidence protection, and layout-intersection checks. The solution compiled with zero warnings and zero errors. Batch-launched Core tests passed 232/232 and ForgeWeave tests passed 31/31. The Gauntlet visual-check batch completed successfully after the test assembly was rebuilt.

## Runtime boundary

The installed source TPAC predates the new atlas. It was not replaced or imported through Resource Browser, and no in-game render was observed. Runtime texture appearance remains pending manual Resource Browser import and a brief menu-screen inspection. This record does not claim a verified TPAC or runtime rendering result. The product remains 22.0.0; no ZIPs were generated.

This record is append-only and does not revise Rev021–Rev024 or earlier evidence.
