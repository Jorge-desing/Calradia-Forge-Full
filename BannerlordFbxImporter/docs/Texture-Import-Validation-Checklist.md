# Resource Browser PNG calibration and texture-only checklist

This checklist separates what was observed in the Editor from what is supported by official documentation. It is a procedure, not evidence that an import succeeded.

## Evidence boundary

- **Observed in Resource Browser on 2026-09-23:** the visible route was `Modules/CalradiaForge/Assets/GauntletUI/`; the existing resource `ui_calradiaforge_1` had a Texture Inspector showing `Albedo (DXT1/DXT5 - RGBA_8)` and other texture choices. These values describe that existing atlas only; they are not defaults for a new import.
- **Observed in the file picker:** the `All` filename filter listed `.tif`, `.psd`, `.dds`, `.bmp`, `.tga`, `.png`, `.hdr`, `.exr`, `.trf`, `.fbx`, and `.stsdk`, with individual filters. The local texture-only profile lists the observed image extensions `.tif`, `.psd`, `.dds`, `.bmp`, `.tga`, `.png`, `.hdr`, and `.exr` for planning. **A filename filter is not proof that a file is importable, that import will succeed, or that a valid resource will be produced.**
- **Official documentation:** TaleWorlds says textures can be imported through the Asset Browser and assigned to materials through the Material Editor. Its separate sprite-sheet guide describes adding sprites, generating sprite sheets, then scanning/importing the category from Resource Browser. Do not treat the sprite-sheet workflow as proof of generic PNG-import behavior.
- **Not yet observed or verified:** settings shown after selecting `calradiaforge_compass.png`, the final import action, the resulting resource, its exact name/type, and a successful texture import. The documented helper currently returns `LOCKED` before UI interaction; `--inspect` previously ended as `UNKNOWN` on timeout.

## One-PNG calibration

Use `modules/CalradiaForge/GUI/SpriteParts/ui_calradiaforge/calradiaforge_compass.png` as the proposed sample. Keep both `Assets` and `AssetSources` intact.

1. **Preflight:** confirm the sample exists and is the intended PNG. In Resource Browser, confirm the exact Editor window, module, and destination folder. The prior `GauntletUI` breadcrumb is an observation, not an instruction to use that folder. Stop if the destination is ambiguous or a same-name resource exists; do not overwrite or infer a backup is ready.
2. **Open the picker:** in the `Assets` pane, the observed context-menu item is `Import new asset`. Select the sample only after confirming the picker is owned by the Editor and the displayed path/name are correct. Record the filename filter shown; do not label its extensions “supported formats.”
3. **Inspect settings before submission:** record the destination, proposed resource name, resource type, and every visible import setting with its displayed value. In particular, do not copy the existing atlas's Albedo/compression setting as a presumed default. Stop before the final action if any required setting or target cannot be read reliably.
4. **Submit only from a known state:** record `TARGET_READY` once the exact target is established and `SETTINGS_READY` once settings are visible and reviewed. Record `SUBMITTED` only after the final `Import` action is actually invoked, then `IMPORTING` while the Editor is processing. An unexpected dialog, UI Automation timeout, or uncertain state means `STOPPED` or `UNKNOWN`; leave the Editor as-is and do not retry automatically.
5. **Observe and verify:** after processing, locate the new resource in the intended folder. Record `OUTPUT_OBSERVED` only when it appears. Record `VERIFIED` only after checking and recording the exact resource name and type in Resource Browser; also record the final settings and outcome. If the resource does not appear or its identity/type is unclear, do not call the import verified. Model Viewer review is a separate check.

## Texture-only batches

- Keep batch planning bounded by the helper's configured limits. Accept only extensions explicitly configured for the texture-only profile, but describe them as **observed picker filters**, not verified importable formats.
- Require the single-PNG calibration above to reach `VERIFIED` before treating its settings or result as a precedent for later files. Review every proposed source path, destination, resource name, and settings; stop on duplicate names, target collisions, unreadable settings, unexpected dialogs, or an uncertain result. Preserve `Assets` and `AssetSources`.
- For every item, retain the state sequence and evidence separately: `PLANNED` → `TARGET_READY` → `SETTINGS_READY` → `SUBMITTED` → `IMPORTING` → `OUTPUT_OBSERVED` → `VERIFIED`. A stop or timeout must be recorded as `STOPPED` or `UNKNOWN`, never upgraded to success based on a closed dialog or an output file alone.
- Current implementation status takes precedence over this procedure: the helper's `--submit` path is still locked and the actual picker/settings/import/output-verification sequence is not implemented. Do not claim a batch has run until that implementation and an Editor observation establish it.

## References

- TaleWorlds, [Textures](https://moddocs.bannerlord.com/asset-management/asset-types/textures/): Asset Browser import and Material Editor assignment.
- TaleWorlds, [Generating and Loading UI Sprite Sheets](https://moddocs.bannerlord.com/asset-management/generating_and_loading_ui_sprite_sheets/): sprite-sheet generation and its distinct Resource Browser scan/import sequence.
- Project observations and current helper status: [`README.md`](../README.md), [`README.es.md`](../README.es.md), sections “Read-only Resource Browser inspection” / “Inspección de Resource Browser sin escritura” and “Locked submission and evidence states” / “Envío bloqueado y estados de evidencia”.
