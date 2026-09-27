# Rev017 — Context menu confirmed; picker remains inaccessible

**Date:** 2026-09-23  
**Release line:** Calradia Forge 22.0.0  
**Scope:** Standalone experimental `BannerlordFbxImporter`; no release package changes.

## New operator clarification

The operator clarified that the earlier Computer Use interruption occurred because they switched away from the Editor to write a chat message. It was not an action within the Editor. The operator's screenshot shows the Resource Browser context menu after right-clicking blank space in the `Assets` pane, with the menu item `Import new asset`.

## Current observation

The exact top-level `Resource Browser` window was reacquired in the Bannerlord editor process. Its accessibility tree and screenshot showed the route `Modules > CalradiaForge > Assets` and the existing `GauntletUI` folder. The menu command name is therefore identified from the operator's screenshot, and the active browser destination is independently visible.

After raising the Resource Browser, renewed attempts to open the context menu in the blank resource area did not produce a verifiable picker. During the popup attempt, the Computer Use bridge reported that it could not expose a single screenshot region or an accessibility state for the window. A keyboard attempt using the menu's last-item navigation did not open a file dialog. The file picker was not observed in this attempt.

## Import and implementation status

`calradiaforge_compass.png` was not selected. No texture settings were inspected or changed. No final `Import` or `Save` action was activated, no resource was written, and no output or TPAC was observed. `--submit` remains locked; `WindowsEditorAutomation.SubmitFile` still returns `UNKNOWN` without sending clicks or keys. The UI inspection helper has no implemented import-click sequence.

`Test-BannerlordFbxImporter.bat` completed with 52 passed, 0 failed, and 1 skipped because Windows did not permit symlink creation. This fixture-only result does not establish a live import, calibrated settings, or output verification.

## Continuation gate

Continue only when the context menu and the resulting file picker can be inspected in the exact Resource Browser window. If the bridge cannot uniquely expose the `Import new asset` item and picker controls, stop and ask the operator to open the picker manually. Before any final Import action, present the exact target, source file, and observed settings and obtain the operator's explicit `IMPORT` confirmation. A later texture-only batch remains disabled until the sample is observed and its resource name and type are manually verified.

This entry appends the new clarification and attempt. It does not change any earlier revision, authorize a final import, modify `Assets` or `AssetSources`, or change release packages.
