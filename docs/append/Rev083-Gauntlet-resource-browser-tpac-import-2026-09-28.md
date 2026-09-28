# Rev083 — Resource Browser verification of the Gauntlet atlas TPAC

**Date:** 28 September 2026
**Version:** Calradia Forge 25.2.0, unchanged
**Scope:** Gauntlet atlas import and installed resource verification.

## Observed issue and technical justification

Rev082 recorded the current Gauntlet atlas and its Resource Browser import as pending. The installed module contained an older `ui_calradiaforge_1_tex.tpac`, so the regenerated source atlas could not yet be treated as the texture package available to the installed module. Earlier notes show that generic asset-source selection is distinct from the SpriteSheetGenerator atlas import workflow.

## Technical solution and architectural decisions

The documented atlas workflow was followed in the Resource Browser: select the existing `ui_calradiaforge_1` texture resource in `Modules > CalradiaForge > Assets > GauntletUI`, preserve the displayed import settings, and save the generated atlas resource. The Resource Browser was then refreshed and the selected resource was loaded again in the texture inspector. TpacTool was not used because its installed reader is obsolete and unreliable for this package.

## Changes to assets, code, and dependencies

The installed `Assets/GauntletUI/ui_calradiaforge_1_tex.tpac` changed from SHA-256 `69513B5617F026E1F106F6CA6D47CF5C5CE0B5869FFA472E9166B037CA8EDEA6` (539 bytes) to `131E623077708032C76790324F31EDC7862BDC6D5ACA79350B4A03C241A0529E` (539 bytes). The original installed package is preserved at `C:\Users\Alex\AppData\Local\CalradiaForge\resource-browser-backups\Rev082-20260928-073823-248\installed-ui_calradiaforge_1_tex.tpac`; its hash was rechecked after import. The source TPAC backup is also preserved and rechecked.

The workspace source TPAC remains unchanged at SHA-256 `8619B64C386D61364A0599574C70C7B0F0F4149CF406EADE77BEE726D726D5E5` (538 bytes). It was not collected from the installed module: the existing collection helper gates that operation through the legacy TpacTool metadata reader, which was deliberately not trusted or run. No source code, product version, or ZIP package changed.

## Validation and limits of evidence

- The Resource Browser texture inspector displayed resource `ui_calradiaforge_1` as a texture. Source details were 4096×512, `B8G8R8A8`; runtime details were 4096×512, DXT5, with 13 mip levels. These details were visible again after saving and refreshing the browser.
- The Resource Browser Save action was used once with the existing Albedo/DXT5 RGBA, no-resize, mipmap-enabled settings. No confirmation dialog appeared. SHA-256 checks confirmed that the pre-import installed and source backups remained unchanged.
- The resulting TPAC import is verified in Resource Browser by the loaded resource name, type, and dimensions. This does not verify the full Gauntlet panel composition or visual appearance in a running game. No campaign, battle, or F10 test was performed for this import.
- The subsequent Computer Use session was stopped by the operator with Escape; no further UI actions were taken.
- The TpacTool reader was not used. The installed package is retained as the verified import, while source collection remains pending until a trustworthy validation path is available.

This annex records additional evidence without replacing earlier revision text. The product remains 25.2.0; public APIs, routes, commands, permissions, and ZIP packages are unchanged.
