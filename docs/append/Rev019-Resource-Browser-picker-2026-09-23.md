# Rev019 — Import picker command attempt remains unverified

**Date:** 2026-09-23  
**Release line:** Calradia Forge 22.0.0  
**Scope:** Standalone experimental `BannerlordFbxImporter`; implementation and validation status for opening the Resource Browser import picker.

## Code change and safety limits

The helper now has an `--open-import-picker` path. It searches for and invokes the exact `MenuItem` for the import picker only when that item is already exposed and visible to UI Automation. The helper does not open the Resource Browser context menu to reveal the item. This command does not select a file or submit an import. The final `--submit` path remains blocked.

## Automated validation

The root run of `Test-BannerlordFbxImporter.bat` rebuilt the helper with 0 warnings and 0 errors, then reported 58 passed, 0 failed, and 1 skipped. The skipped case was symlink creation, which Windows did not permit. These fixture results do not establish that the live Editor command opened a picker.

## Live attempt and outcome

A real `dotnet run ... --open-import-picker` attempt produced no response for more than 60 seconds and was interrupted by the root operator. This entry records that observation only; it does not claim that the helper's programmed timeout fired or reported an error.

Computer Use did not observe an import selector. After interruption, no helper process or temporary file remained. No file was selected, no Import action was sent, and `Assets` and `AssetSources` were unchanged. Because the live action did not yield an observable result, its status is **UNKNOWN / not verified**.

## Continuation boundary

The menu-item invocation and its behavior remain unverified in the live Resource Browser. The automated build and fixture results are positive but do not change that status. No import or generated resource is claimed.

This is a new append-only record. It does not edit Rev018 or any earlier revision, README, DOCX, module assets, or release packages.
