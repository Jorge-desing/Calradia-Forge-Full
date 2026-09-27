# Rev020 — Bound UI Automation worker output draining

**Date:** 2026-09-23  
**Release line:** Calradia Forge 22.0.0  
**Scope:** Standalone experimental `BannerlordFbxImporter`; worker timeout handling and fixture validation.

## Worker timeout correction

The worker now bounds stdout/stderr draining after `WaitForExit(timeout)` to 3 seconds. If either stream has not closed when that drain bound expires, the worker reports `UNKNOWN` rather than waiting indefinitely or treating the operation as successful. A fixture using an incomplete `TaskCompletionSource` was added to exercise the still-pending drain case.

This change could explain the prolonged wait observed in the prior live `--open-import-picker` attempt, but the cause of that attempt was not demonstrated. The fixture verifies the bounded-drain path; it does not establish what occurred inside the Editor.

## Automated validation

The root repeated `Test-BannerlordFbxImporter.bat`. The build completed with 0 warnings and 0 errors; the test result was 59 passed, 0 failed, and 1 skipped because Windows did not permit symlink creation. These automated results do not validate a live UI Automation interaction.

## Live Editor status

No UI attempt was repeated after the prior uncertain outcome. The import selector was not opened or observed, no file was selected or imported, and `Assets` and `AssetSources` were not changed. The live picker/import status therefore remains **UNKNOWN / not verified**.

This is a new append-only record. It does not edit Rev018, Rev019, or earlier revisions, README, DOCX, module assets, or release packages.
