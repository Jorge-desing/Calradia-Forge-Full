# Rev085 — Resource Browser atlas re-import confirmation

**Date:** 28 September 2026
**Version:** Calradia Forge 25.2.0, unchanged
**Scope:** Repeat import of the generated Gauntlet atlas and synchronization of its source TPAC.

## User report and verification

The user repeated the atlas import to ensure the latest texture package was present. The Resource Browser had returned to its normal state with no pending confirmation or import-progress window. Its Texture Inspector continued to show `ui_calradiaforge_1` under `Modules > CalradiaForge > Assets > GauntletUI` as a texture. The source details were 4096×512 `B8G8R8A8`; runtime details were 4096×512 DXT5 with 13 mip levels.

The import workflow is selecting the generated `ui_calradiaforge_1.png` and confirming **Update** in Resource Browser. The Texture Inspector **Save** button only stores import settings and does not perform the import. The repeated import completed without changing the package hash recorded after the completed import.

## TPAC evidence and retained backups

The installed Steam module package and the workspace source package both contain 539 bytes and have SHA-256 `8899A48A407591ADA53573EFC0DD699EA47D2A30F32A0C12C1875003F7993047`. The installed package is under `Mount & Blade II Bannerlord/Modules/CalradiaForge/Assets/GauntletUI/`; the workspace package is `modules/CalradiaForge/Assets/GauntletUI/ui_calradiaforge_1_tex.tpac`. The source atlas used for the import is 4096×512 and has SHA-256 `63E94707F5119CB3025DB30748BC0A63EC5349FF3CA3367B9FC3FB154D24C372`.

Backups remain outside the repository in `%LOCALAPPDATA%/CalradiaForge/resource-browser-backups/`. The pre-import installed package is preserved with SHA-256 `81B828AE0718F05E8565EEAA94327B6CC6C9CDB365ADD89F724891E31B829BBA`; the pre-import workspace source package is preserved with SHA-256 `1506C5EAECD4BFC752678C6E6CDB3FA87C4A7A51D8B3B7846B15715276D571EF`. A separate post-import installed backup was verified against the final `8899A48A...93047` hash.

TpacTool was not used. Hash and file-size equality establishes that the installed and source package files match; Resource Browser's loaded inspector establishes the observed resource name, type, dimensions, runtime format, and mip count. Neither fact by itself proves a fresh in-game render. No new F10 or live-game rendering observation was made after this re-import, so the latest in-game render check remains pending.

## Scope and compatibility

This revision records the repeated import and package synchronization. Product version remains 25.2.0; no API, route, command, permission, dependency, or ZIP changed.
