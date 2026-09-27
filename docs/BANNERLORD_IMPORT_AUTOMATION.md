# Bannerlord Resource Browser UI helper

`BannerlordImportAutomation` is an optional Windows development helper, separate from the Calradia Forge game module. It uses FlaUI 5.0.0 to inspect or submit files through the already-open Resource Browser. It does not call an undocumented engine import API, compile TPAC packages, or verify the resulting game assets. The maintained helper is `BannerlordImportAutomation.csproj`; `BannerlordEditorImport.csproj` and its source are retained as a reference prototype and are not part of normal validation.

## Safe modes

Run `Test-BannerlordImportAutomation.bat` to build the maintained helper and run its short, no-Editor policy fixtures. They cover supported extensions, top-level-only matching, duplicate basenames, Unicode and spaced filenames, empty/missing folders, the 100-file boundary, required destination arguments, and exact/missing/ambiguous destination-path matching. The test BAT does not launch the built helper executable, attach to the Editor, or import resources.

Use `Run-BannerlordImportAutomation.bat --dry-run "C:\assets\weapons" "*.fbx"` to validate and list matching files without accessing UI Automation. `*.fbx` and `*.png` are the only accepted extensions. The search is top-level only and each batch is limited to 100 files. A pattern containing a directory separator is rejected.

Use `Run-BannerlordImportAutomation.bat --inspect` only when the Modding Kit Resource Browser is already open. This mode reports exposed controls, visible tree-item paths, and visible import menu names. It sends no clicks, keystrokes, file selections, or imports. If the context menu is not already visible, it reports that no import item is exposed.

## Experimental submission mode

The submission command requires an exact visible module folder name and an explicit confirmation:

```bat
Run-BannerlordImportAutomation.bat "C:\assets\weapons" "*.fbx" --module-name "CalradiaForge"
```

The helper first validates the source set and attaches read-only to the open Resource Browser. It proceeds only when exactly one visible tree path matches the supplied module directly followed by `Assets`, for example `Modules > CalradiaForge > Assets`. A missing, unreadable, or ambiguous path fails closed before asking for confirmation. The helper then displays the resolved destination and the complete filename list. Typing `IMPORT` is required before it performs UI actions.

The helper detects duplicate base names inside the selected batch. It does not inventory files already present in the destination `Assets` folder, so it cannot detect or guarantee the outcome of a name collision with an existing asset. Inspect the destination in Resource Browser before submitting files that may already exist.

After confirmation, files are submitted one at a time. A UI error, missing/ambiguous control, or dialog that does not close stops the batch; no file is retried automatically. The button labels, file-dialog AutomationId `1148`, and current process name are installation/version-specific observations, not a stable TaleWorlds API. Use `--inspect` to review what the installed Editor currently exposes.

`SUBMITTED` means only that the expected UI actions completed and the confirmation dialog closed. It does not prove that the asset was imported, compiled, written as a TPAC, or loaded by Bannerlord. Confirm the result manually in Resource Browser and with a compatible read-only package inspection tool. Do not use the helper as a substitute for the documented sprite workflow: run `SpriteSheetGenerator`, scan the new files in Resource Browser, import the generated category, then verify the resulting package separately.

Keep the module's `AssetSources` and `Assets` working folders. If a release excludes authoring sources, exclude them only from a disposable staging copy; do not delete the project sources.

The helper targets the Editor process name observed on this installation, `TaleWorlds.MountAndBlade.Launcher`; this is not guaranteed for other installations. It never launches the game or Editor. The Release EXE is a local build output and is not part of the Modules, Source-SDK, or Desktop archives.

## Separate experimental FBX helper

`BannerlordFbxImporter` is a second, standalone .NET 8 experiment under the workspace root. It is not the maintained FlaUI helper described above, is not integrated into Calradia Forge, and is excluded from all distribution archives. Its full operating instructions are in [BannerlordFbxImporter/README.md](../BannerlordFbxImporter/README.md).

This helper recursively scans only `.fbx` inputs under bounded file, directory, depth, size, and total-entry limits. `--dry-run` performs no UI automation; `--inspect` is read-only and process-scoped; `--submit` requires the exact module name and interactive `IMPORT` confirmation. It stops at the first uncertain UI state and leaves error dialogs untouched. `SUBMITTED` means the configured file-dialog UI sequence was observed to finish; it does not prove that Resource Browser imported or compiled the file, produced a valid TPAC, or loaded the asset in game.

Keep `Assets` and `AssetSources` in the authoring tree. TaleWorlds describes their separate editable/source roles and permits filtering them from a published copy; that documentation does not support the PDF's claimed directory-precedence crash. The PDF's Enter/stdin lock claim, exact LOD modal count, and detailed leaf-bone engine explanation are not adopted. See [the evidence review](ASSET_AUTOMATION_EVIDENCE.md#claims-in-the-18-page-resource-automation-pdf) for the source-by-source assessment.

### Current status correction — 2026-09-23

The preceding paragraph records an earlier proposed workflow and is not the current capability of `BannerlordFbxImporter`. The standalone helper now plans bounded batches and offers process-scoped, read-only inspection; its `--submit` path is deliberately locked before confirmation and before any UI action. Its four profiles are `static-mesh`, `rigged-mesh`, `texture-only`, and `texture-assign`. Mesh profiles accept FBX; texture extensions must be explicitly configured from filters observed in the installed Editor, and texture assignment requires a complete explicit map. No settings screen, material inventory, final Import control, or verified profile has been calibrated, so no file has been submitted.

The latest `Test-BannerlordFbxImporter.bat` run built both projects with zero warnings and errors and passed 32 short tests; one Windows reparse-point fixture was skipped because the current account could not create a symbolic link. A read-only Editor snapshot previously found a Resource Browser window and 112 controls, but did not establish a trustworthy tree ancestry; a later bounded inspection timed out. No import was performed and no 22.0.0 archive was rebuilt. These statements apply only to the standalone FBX experiment; they do not change the maintained FlaUI helper's separate status above.
