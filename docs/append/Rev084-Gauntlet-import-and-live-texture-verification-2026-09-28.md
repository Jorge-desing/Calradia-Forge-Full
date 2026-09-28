# Rev084 — Corrected atlas import and live Gauntlet texture verification

**Date:** 28 September 2026
**Version:** Calradia Forge 25.2.0, unchanged
**Scope:** Resource Browser atlas import, TPAC source collection, and one in-game Gauntlet observation.

## Observed issue and technical justification

Rev083 incorrectly attributed atlas import to the Texture Inspector **Save** action. That action only saves import settings. The installed TPAC hash recorded after that action was not proof that the current atlas had been imported, and the in-game panel had not yet been inspected. Rev083 also said Computer Use had been stopped with Escape; the user clarified that they closed it to reduce resource use.

## Technical solution and architectural decisions

The actual import was completed through Resource Browser's file-selection workflow. The selected atlas file produced a prompt naming the existing `ui_calradiaforge_1.png` and asking whether to replace it and update corresponding assets. **Update** was selected. The transient `Importing file` window then closed, and Resource Browser again displayed `ui_calradiaforge_1` in `Modules > CalradiaForge > Assets > GauntletUI` as a texture. The inspector reported 4096×512 `B8G8R8A8` source data and a 4096×512 DXT5 runtime texture with 13 mip levels.

After this Resource Browser verification, the installed TPAC was copied to a staging file, whose SHA-256 was checked before replacing the workspace source TPAC. The result was checked again. The previous installed and source files remain preserved in the separate Rev082 backup directory. TpacTool was not used.

## Changes to assets, code, and dependencies

The post-import installed TPAC is 539 bytes with SHA-256 `1506C5EAECD4BFC752678C6E6CDB3FA87C4A7A51D8B3B7846B15715276D571EF`. The staged collection and final workspace source TPAC have the same hash and size. The pre-import installed backup remains `69513B5617F026E1F106F6CA6D47CF5C5CE0B5869FFA472E9166B037CA8EDEA6`; the pre-import source backup remains `8619B64C386D61364A0599574C70C7B0F0F4149CF406EADE77BEE726D726D5E5`.

The product remains 25.2.0. No code, API, route, command, permission, dependency, or ZIP changed.

## Validation and limits of evidence

- `tools/Deploy-CalradiaForge-ToGame.bat --no-pause` built and deployed the Client and Modding Kit profiles. Both builds reported 0 warnings and 0 errors. The deploy log recorded and preserved the imported runtime TPAC at SHA-256 `1506C5EAECD4BFC752678C6E6CDB3FA87C4A7A51D8B3B7846B15715276D571EF`.
- Steam launcher configuration had `CalradiaForge` selected. Bannerlord 1.4.8 reached the main menu. One F10 opening displayed the Calradia Forge 25.2.0 panel; its header and rail ornaments rendered from the imported texture. No assertion or missing-texture placeholder was observed. The panel and game were closed normally. No campaign, battle, or test run was started.
- The previous Save-only action did not import the atlas; the file-selection **Update** operation did. The installed `131E623077708032C76790324F31EDC7862BDC6D5ACA79350B4A03C241A0529E` hash recorded in Rev083 was an intermediate observation, not evidence of a completed atlas import. The completed import hash is the value above.
- The user closed the earlier Computer Use session to reduce resource consumption; it was not stopped by Escape.
- TpacTool was not used. This live check confirms the observed main-menu panel rendering only; it does not validate campaigns, battles, or every Gauntlet state.

This annex corrects the evidence interpretation in Rev083 without editing or replacing that historical annex. It appends evidence while retaining product version 25.2.0 and all existing public contracts.
