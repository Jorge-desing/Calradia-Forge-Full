# Bannerlord FBX and texture importer experiment

`BannerlordFbxImporter` is a standalone .NET 8 Windows experiment. It is separate from the Calradia Forge game module and the maintained FlaUI helper, and it is not included in any release ZIP. The current implementation supports bounded local planning, read-only Resource Browser inspection, and a picker-only UIA mode. **Submission is deliberately locked** until file selection, settings, the complete resource inventory, the final Import action, and one manually verified sample can be calibrated.

## Build and short tests

Build the helper with the .NET 8 SDK and Windows Desktop targeting pack:

```powershell
dotnet build .\BannerlordFbxImporter.csproj --configuration Release
```

Run its fixture suite through `Test-BannerlordFbxImporter.bat`. The BAT builds the test project and invokes the managed test DLL with `dotnet`; it does not start the helper app or the Editor. Tests cover profile parsing, source limits, explicit texture maps, FBX advisory parsing, target-path rejection, backup verification, state transitions, stale inputs, and stop-on-uncertain results.

## Profiles and local planning

Choose a profile explicitly for every batch:

- `static-mesh` and `rigged-mesh` scan `.fbx` only. Their material text scan is advisory; it does not validate imported settings, bone data, skin weights, or Editor material registration.
- `texture-only` scans only the file extensions listed under `profiles.texture-only.supportedExtensions` in `appsettings.json`. That list must come from the file filters observed in the actual Editor; it is empty by default. No format is asserted to be supported by TaleWorlds documentation.
- `texture-assign` uses the same observed-extension rule and requires a `--texture-map` with one explicit assignment for every scanned file. The map does not execute an assignment in the Editor.

Example local plan for a mesh batch:

```powershell
dotnet .\bin\Release\net8.0-windows\BannerlordFbxImporter.dll --dry-run --profile static-mesh --source-folder "D:\Mod Assets\Armor" --materials-manifest .\materials_manifest.example.json
```

Example texture assignment map:

```json
[
  { "sourceFile": "armor_n.png", "textureName": "armor_n", "materialName": "armor_mat", "slot": "Normal" }
]
```

```powershell
dotnet .\bin\Release\net8.0-windows\BannerlordFbxImporter.dll --dry-run --profile texture-assign --source-folder "D:\Mod Assets\Armor" --config .\appsettings.json --texture-map .\texture-map.json
```

Every scan is bounded to 100 files, depth 32, 10,000 directories, 100,000 entries, 256 MiB per file, and 2 GiB per batch. It rejects repeated case-insensitive basenames and skips reparse points. A mesh batch must use `.fbx`; texture formats are read only from the selected profile configuration. `--dry-run` never attaches to UI Automation or writes to the module.

FBX preflight reads bounded ASCII text only. Binary, malformed, or oversized FBX input produces `Not available` evidence and requires manual review; the preflight is not a reason to claim import success. A missing or invalid material manifest is also reported as unavailable evidence.

## Read-only Resource Browser inspection

Open the Modding Kit and Resource Browser manually, then run:

```powershell
dotnet .\bin\Release\net8.0-windows\BannerlordFbxImporter.dll --inspect --config .\appsettings.json
```

`--inspect` attaches only to `editorProcessName` and the exact visible top-level window title in `resourceBrowserWindowTitle` from `appsettings.json`. It distinguishes `Resource Browser` from other windows such as `Edit Mode`, even when they share a process, and rejects zero or multiple matches. It sends no clicks, selections, keystrokes, or dialog commands. UI Automation runs in a disposable worker with a 45-second default limit. If a provider blocks or its hierarchy cannot be established, inspection reports failure or `UNKNOWN`; the Editor is not closed, retried, or modified. A flat list of `TreeItem` names is not proof of a `Modules > module > Assets` route.

On 2026-09-23, a direct read-only Computer Use snapshot uniquely identified the visible `Resource Browser` window while `Edit Mode` was also open in the same process. Its breadcrumb showed `Modules/CalradiaForge/Assets/GauntletUI/`; the selected resource was `ui_calradiaforge_1`, whose source is `Modules/CalradiaForge/AssetSources/GauntletUI/ui_calradiaforge_1.png` (2048 × 256, BGRA8). The Texture Inspector displayed the current resource type `Albedo (DXT1/DXT5 - RGBA_8)`, plus Albedo HQ, Normal, Specular, HDR, and Heightmap choices and texture flags. These are observations of an existing atlas resource, not proof that they are defaults for a new import. The Resource Browser filter list contains asset classes such as `Texture`; it does not show filename extensions. The individual `calradiaforge_compass.png` file was not listed as a Resource Browser asset.

Later that session, right-clicking blank space in the Resource Browser's `Assets` pane showed `Import new asset`. Selecting that menu item opened a separate Windows `Open` dialog titled `Abrir`, owned by the Editor process. Its `File type` list showed `All (*.tif;*.psd;*.dds;*.bmp;*.tga;*.png;*.hdr;*.exr;*.trf;*.fbx;*.stsdk)` and individual filters for each listed extension. The texture profiles record only the observed raster filters (`.tif`, `.psd`, `.dds`, `.bmp`, `.tga`, `.png`, `.hdr`, `.exr`) for local batch planning. This proves that the picker exposes those filename filters; it does **not** prove the importer will accept, process, or create a valid resource from a selected file. No file was selected; the dialog was canceled; no final `Import` or `Save` action was invoked, and no module asset files were changed. The dialog came from a Resource Browser context-menu command; no separate file-picker command was found in the `Edit Mode` menu.

The helper's separate `--inspect` UI Automation attempt timed out at its 45-second limit and returned `UNKNOWN`; it has not been retried. That timeout does not erase the manual screenshot/accessibility observations above, but the helper still cannot calibrate the settings shown after selecting a new file, the complete resource inventory, or the final Import action. PNG is present in the picker's filename filter; actual PNG import and output remain unverified. Submission remains locked.

## Locked submission and evidence states

The `--submit` command is restricted to `--profile texture-only` and requires `--calibration-sample <verified-png>` plus the exact module name. Before any possible UI interaction, it requires persistent evidence at `%LocalAppData%\CalradiaForge\Importer\Calibration\texture-only.json`. The evidence reader validates schema and SHA-256 integrity, then matches the current sample's filename, content hash, and size; profile; module and on-disk `Assets` path; observed resource name and type; configured settings; Editor process; and window title. SHA-256 here is an integrity check, not a signed attestation. `appsettings.json` calibration flags or selectors alone never authorize submission.

Even matching persistent evidence does not enable an import: the command still exits `LOCKED` before interacting with UI because the file-picker/settings/final-Import sequence and live output verification have not been implemented and verified. It does not ask for `IMPORT`, click controls, or claim `SUBMITTED`. A changed sample, different module/path/process/window/settings, invalid record, or non-`texture-only` profile fails closed. Editing appsettings flags does not bypass these checks.

The workflow model defines `PLANNED`, `TARGET_READY`, `SETTINGS_READY`, `SUBMITTED`, `IMPORTING`, `OUTPUT_OBSERVED`, `VERIFIED`, `STOPPED`, and `UNKNOWN`. No import-related state is inferred from a closed file dialog or a process title. `SUBMITTED` is reserved for a confirmed invocation of the final Import button; `VERIFIED` requires observing the expected resource name and type in Resource Browser. A Model Viewer review is separate. Until calibration exists, actual submission, output observation, verification, and visual review remain not run.

The backup utility can create a bounded SHA-256-verified snapshot of a module's direct `Assets` folder under `%LocalAppData%\CalradiaForge\Importer\Backups`. Separate replacement-safety primitives now classify exact resource collisions, verify that the backup hashes and file set match the exact affected resource, recognize only a fully calibrated replacement dialog, and issue a single-use permit that forbids retries. These checks do not click or dismiss the dialog, and they are not connected to the actual import flow. Automatic replacement is therefore not operational; no replacement may be claimed or attempted through this helper yet.

### Latest calibration attempt — 2026-09-23

The operator explicitly authorized selecting files and sending Editor actions. An initial Computer Use attempt listed the Editor process and the `Resource Browser` and `Edit Mode` windows, but opening the Resource Browser accessibility state failed with `Accessibility state unavailable`. After restarting the Computer Use session, visual capture and the AX tree became available. They showed the route `Modules > CalradiaForge > Assets > GauntletUI` and the previously selected texture `ui_calradiaforge_1` with its inspector. This is an observation of the existing resource, not the sample.

The three toolbar buttons had no accessible names. An AX invocation for `Extras` returned coordinates outside the window, so its intended action could not be identified from that result. A reversible inspection of `Assets/Extras` was dismissed with `Esc`. The sample `calradiaforge_compass.png` was not selected; no file picker or import-settings flow was opened; neither `Save` nor final `Import` was activated; and no resource was written. The sample is therefore still uncalibrated. UI inspection partially recovered, but the picker and import controls remain inaccessible or unidentified; this is a tooling/calibration blocker, not a missing user authorization.

The helper's `--submit` path remains locked in the current source. Do not enable it by changing configuration flags alone. Resume calibration only when the exact Resource Browser and its controls can be inspected reliably; stop on an ambiguous window, unavailable accessibility tree, timeout, or unexpected dialog. The sample's final Import action still requires showing the exact target, filename, and settings and obtaining the operator's explicit `IMPORT` confirmation. A later `texture-only` batch can be considered only after that sample's expected name and type are observed and manually verified in the Editor. No TPAC creation or successful import is claimed here.

### Local submission and replacement gates — 2026-09-23

The CLI now limits `--submit` to `texture-only` and requires the unchanged, manually verified PNG through `--calibration-sample`. It reads `%LocalAppData%\CalradiaForge\Importer\Calibration\texture-only.json` and checks schema, SHA-256 integrity, sample name/hash/size, profile, module, exact `Assets` path, observed resource name/type, settings, process, and window title before any possible UI access. Appsettings flags alone are insufficient. No calibration record has been created from the Resource Browser inspection described above, and the final code path remains locked even if a record matches because the real import sequence and output verification are still absent.

After—and only after—you personally observe the sample imported in Resource Browser and verify its exact name, type, and settings, you may record that manual attestation with:

```powershell
dotnet .\bin\Release\net8.0-windows\BannerlordFbxImporter.dll --record-calibration --profile texture-only --module-name "CalradiaForge" --calibration-sample ".\modules\CalradiaForge\GUI\SpriteParts\ui_calradiaforge\calradiaforge_compass.png" --observed-resource-name "<exact-resource-name>" --observed-resource-type "<exact-resource-type>" --config .\appsettings.json
```

This command does not scan a batch or inspect/interact with the Editor. It requires a direct module `Assets` path and non-empty observed texture settings in `appsettings.json`, displays the values to be recorded, and writes the local evidence only if the operator types exactly `VERIFIED`. It hashes and validates the PNG when writing. This is a manual attestation, not an automatic observation or verification. Do not run it for the current sample until the import has actually been observed and its name and type confirmed; recording an attestation still does not unlock `--submit`.

Replacement checks were added as isolated safety primitives: exact collision classification, per-resource backup file-set and SHA-256 verification, exact calibrated-dialog recognition, and a single-use permit with no retry after uncertainty. The import automation does not invoke these primitives, so automatic replacement is not available. `Test-BannerlordFbxImporter.bat` completed with 48 passed, 0 failed, and 1 skipped because the environment denied symlink creation; these are fixture tests and do not verify a live Editor import.

Keep `Assets` and `AssetSources` intact. TaleWorlds describes `Assets` as editable TPAC metadata, `AssetSources` as imported source files, and `AssetPackages` as read-only client output; its documentation does not establish the claimed fatal precedence rule for empty authoring folders. Do not remove directories to address a crash based on that claim.

`Add Leaf Bones` is a manual export checklist item only. The helper cannot read that Blender option from FBX. TaleWorlds' `_notused` naming rule tells the engine to ignore a skeleton for the documented related-asset import case; it is not evidence of automatic weight transfer.

The SpriteSheetGenerator workflow remains separate and manual. TaleWorlds documents the console command `resource.show_resource_browser`; it does not document a redirected-stdin Enter handshake or a PNG-lock lifecycle. This helper never launches the generator or sends it input.

## Sources

- [TaleWorlds asset naming conventions](https://moddocs.bannerlord.com/asset-management/asset-types/asset_naming_conventions/)
- [TaleWorlds asset folder roles and publishing](https://moddocs.bannerlord.com/asset-management/asset-types/overriding_assets/)
- [TaleWorlds sprite-sheet workflow](https://moddocs.bannerlord.com/asset-management/generating_and_loading_ui_sprite_sheets/)
- [Microsoft Process standard output behavior](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput?view=net-10.0)

### Latest retry — 2026-09-23

The operator clarified that the import picker is opened from the Resource Browser context menu, not from the separate Edit Mode window, and supplied a screenshot showing the exact `Import new asset` entry in the blank area of the `Assets` pane. A fresh read-only inspection again showed `Modules > CalradiaForge > Assets` with `GauntletUI` as the only visible child. The context-menu item is identified by the operator-provided screenshot.

The current Computer Use bridge could not complete the selection: it reported user input during the first right-click attempt, then its popup capture returned multiple screenshot regions with no accessibility entries for the menu. A coordinate attempt initially mapped over the overlapping `Edit Mode` window; after raising Resource Browser and refreshing, no file dialog appeared. No file was selected, no final Import or Save action ran, and no module resource changed. The remaining blocker is reliable interaction with the context-menu popup, not uncertainty about the command name. `--submit` remains locked.

After the CLI mode-help wording was updated to include `--record-calibration`, `Test-BannerlordFbxImporter.bat` rebuilt successfully with 0 warnings and 0 errors: 52 passed, 0 failed, 1 skipped because Windows denied symlink creation. These are automated fixture tests, not a live import or output verification.

### Picker-only UIA mode and latest live attempt — 2026-09-23

Use the new picker-only mode with:

```powershell
dotnet .\bin\Release\net8.0-windows\BannerlordFbxImporter.dll --open-import-picker --config .\appsettings.json
```

The exact `Import new asset` context-menu entry must already be visible in the configured Resource Browser before this command runs. The mode does **not** right-click, create, or reopen the context menu. It only invokes one uniquely exposed, visible, enabled UI Automation `MenuItem` whose exact name is `Import new asset`, process ID matches the configured Editor, and window title matches the configured Resource Browser. If the item is absent, duplicated, disabled, or does not expose the required invoke pattern, the operation fails closed without a fallback action.

Even when UIA invokes that menu item and prints `PICKER_MENU_INVOKED`, this means only that the menu item was invoked; it does not prove that the Windows picker appeared. The mode stops before selecting a file and sends no final `Import` or `Save` action. It cannot report a created or verified resource. `--submit` remains locked.

A real UI invocation was attempted, remained without a response for more than 60 seconds, and was interrupted. No file picker was visible or verified, no file was selected, and no import or resource change was observed. The latest `Test-BannerlordFbxImporter.bat` result was 58 passed, 0 failed, 1 skipped; these fixture tests do not verify the live UIA invocation or an import.

### Worker pipe-drain fix — follow-up on 2026-09-23

The UI worker's stdout/stderr drain was corrected with a 3-second close limit; if either pipe does not close within that bound, the worker result is `UNKNOWN`. This could be related to the earlier live hang, but the cause was not established or tested. No UI retry was made because the Editor state was uncertain. The file picker and any import remain unverified, and `--submit` is still locked. The latest `Test-BannerlordFbxImporter.bat` run had a clean build and reported 59 passed, 0 failed, 1 skipped; it does not establish the cause of the live hang or verify an import.
