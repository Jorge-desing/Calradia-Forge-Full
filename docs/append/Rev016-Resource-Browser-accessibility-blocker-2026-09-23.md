# Rev016 — Resource Browser inspected; import controls remain uncalibrated

**Date:** 2026-09-23  
**Release line:** Calradia Forge 22.0.0  
**Scope:** Standalone experimental `BannerlordFbxImporter`; no release package changes.

## Context

The planned next step was a one-file `texture-only` calibration using the project sample `modules/CalradiaForge/GUI/SpriteParts/ui_calradiaforge/calradiaforge_compass.png`. The user explicitly authorized selecting files and sending actions in the Editor. The approved workflow still reserves the final Import action for an action-time confirmation after the target, filename, and settings are presented. Batch submission is gated on observing and manually verifying the sample resource's name and type.

## Observed result

In the first Computer Use attempt, the Editor process and top-level windows named `Resource Browser` and `Edit Mode` were listed, but opening the `Resource Browser` accessibility state failed with the exact error `Accessibility state unavailable`.

After restarting the Computer Use session, visual capture and the AX tree became available. They showed the visible route `Modules > CalradiaForge > Assets > GauntletUI`; the existing texture `ui_calradiaforge_1` was already selected and its inspector was visible. This did not establish the sample import flow. The three toolbar buttons had no accessible names. An AX invocation for `Extras` returned coordinates outside the window, so its action could not be identified through that invocation. A reversible inspection of `Assets/Extras` was performed and dismissed with `Esc`.

The sample `calradiaforge_compass.png` was not selected. The file picker and post-selection import settings were not opened or calibrated. Neither `Save` nor the final `Import` action was activated, no resource was written, and no output or TPAC was observed or verified. Thus, the second session improved read-only visual/AX access to the current Resource Browser contents, but did not make selecting or importing the sample safe. The remaining blocker is unresolved control identification and calibration after user authorization, not lack of authorization and not an import failure reported by the Editor.

## Implementation status

At the time of this revision, `Program.cs` still reports submission as `LOCKED`, and `WindowsEditorAutomation.SubmitFile` returns `UNKNOWN` without sending UI input. Configuration flags do not provide a safe substitute for observed calibration. `AssetBackupService` can produce a bounded SHA-256-verified snapshot of the module's `Assets` tree, but the submission flow does not call it and has no verified destination inventory. Automatic replacement is therefore not available.

The `--inspect` contract remains read-only, and `--dry-run` remains independent of UI Automation. Do not claim calibrated settings, supported texture import, `SUBMITTED`, `OUTPUT_OBSERVED`, `VERIFIED`, or successful TPAC generation based on these inspections.

## Safe continuation gates

1. Identify uniquely named, in-window controls for the exact `Resource Browser`, distinguishing it from `Edit Mode`; stop if a control is unnamed, maps outside the window, the target is ambiguous, or the accessibility provider is unavailable.
2. Inspect the actual import picker, selected-file settings, target route, and final action without assuming that filename filters prove a successful import.
3. Before the sample's final Import action, present its exact target, source path, and observed settings and obtain the operator's explicit `IMPORT` confirmation.
4. Observe the resulting resource in Resource Browser and manually verify the expected name and type before marking the sample `VERIFIED` or enabling a later `texture-only` batch.
5. Keep replacements blocked unless the exact affected resource is identified and its backup is created and hash-verified first. Stop on unexpected dialogs, timeouts, cancellation, or uncertain state; do not retry or close dialogs automatically.

This revision records the blocked attempt and the remaining gates. It does not authorize removing `Assets` or `AssetSources`, starting `SpriteSheetGenerator`, changing release ZIPs, or treating a UI submission as proof of a compiled resource.

## Evidence references

- `BannerlordFbxImporter/Program.cs` — submission remains locked.
- `BannerlordFbxImporter/WindowsEditorAutomation.cs` — `SubmitFile` returns `UNKNOWN` without UI actions.
- `BannerlordFbxImporter/AssetBackupService.cs` — standalone bounded snapshot and hash verification; not integrated with submission.
- `BannerlordFbxImporter/README.md` and `README.es.md` — current state and safe continuation steps.

## Follow-up: persistent evidence and isolated replacement gates

After the Editor inspection, local helper changes added further fail-closed checks without enabling or executing an import. The CLI accepts `--submit` only for `texture-only` and requires `--calibration-sample` to name the unchanged PNG that was manually verified. Before any possible UI interaction, it loads `%LocalAppData%\CalradiaForge\Importer\Calibration\texture-only.json`, validates the versioned schema and SHA-256 integrity value, and matches the current PNG filename, content hash, and size together with the profile, exact module and on-disk `Assets` path, observed resource name and type, configured settings, Editor process, and window title. The SHA-256 value is an integrity checksum, not a digital signature; appsettings flags alone cannot authorize submission.

The new evidence gate does not unlock the UI flow. No calibration evidence has been recorded for `calradiaforge_compass.png`, and even an exact matching record still reaches an explicit `LOCKED` result because the picker/settings/final-Import sequence and live output verification remain unimplemented. No `SUBMITTED`, `OUTPUT_OBSERVED`, or `VERIFIED` state is claimed.

The CLI also now provides an offline `--record-calibration` mode, restricted to `texture-only`, the exact `CalradiaForge` sample filename, and explicit observed resource name/type. It neither scans a batch nor touches UI Automation. It requires the module's direct `Assets` directory and non-empty texture settings from `appsettings.json`, displays these details, and writes the evidence file only after the operator enters the exact word `VERIFIED`. The store checks the PNG signature and records its size/hash. This is an operator-authored manual attestation, not an automated check that the named resource exists in the Editor. Given the recorded session above, the sample was not imported and this command must not be used to record it yet.

Replacement safety is now represented by standalone classifiers and gates: exact resource collision identity, a backup verifier that matches every backing file by set, size, and SHA-256, recognition of only the exact operator-calibrated replacement dialog, and a single-use permit that forbids retries after an uncertain result. These primitives do not themselves click or dismiss dialogs and are not connected to the import automation. Automatic replacement is not operational.

`Test-BannerlordFbxImporter.bat` completed with 48 passed, 0 failed, and 1 skipped because the environment did not allow symlink creation. This validates fixture behavior only; it is not a live import, TPAC, or Editor verification. The runtime state remains unchanged: the sample was not selected, the picker was not opened, and no resource was written.

## Follow-up — context-menu command identified; current retry still blocked

The operator clarified that the import picker is opened from the Resource Browser context menu, not from `Edit Mode`, and supplied a screenshot of the menu on blank space in the `Assets` pane. The screenshot visibly identifies the exact item as `Import new asset`. A new Resource Browser inspection showed the same direct route `Modules > CalradiaForge > Assets` with the `GauntletUI` folder visible.

The current Computer Use attempt did not open the file picker. The first right-click attempt was interrupted by detected user input. Later, the popup could not be captured as one screenshot or read from the accessibility tree. A coordinate action was rejected because it mapped over the overlapping `Edit Mode` window; after raising Resource Browser and refreshing the window, another attempt did not produce an `Open` dialog. The menu label is identified, but the current automation bridge still cannot reliably target the popup. No file was selected, no import settings were changed, no final Import or Save action ran, and no resource was written. The earlier file-picker observations recorded in the README remain historical evidence; this retry adds no new file-type or import-result evidence.

The CLI received a help-message correction so both missing-mode and multiple-mode errors list `--record-calibration`. `Test-BannerlordFbxImporter.bat` then completed with 52 passed, 0 failed, and 1 skipped (symlink creation unavailable); build had 0 warnings and 0 errors. This is fixture-only validation and does not calibrate or verify an Editor import. The sample remains unimported in this attempt and `--submit` remains locked.
