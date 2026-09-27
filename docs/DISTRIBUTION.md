# Distribution preparation

The build produces three separate downloads:

- `CalradiaForge-Modules-1.1.0.zip`: the main mod and optional Examples, Price Provider and Price Consumer modules. Extract the desired module folders into Bannerlord's Modules directory. No EXE files or desktop runtime are included.
- `CalradiaForge-Desktop-1.1.0.zip`: the optional Windows application and its installation instructions. Keep this as a separate download; never install it into Modules.
- `CalradiaForge-Source-SDK-1.1.0.zip`: SDK, source and documentation for developers. It contains no installable game-module output, EXE files or compiled desktop application. Desktop source remains available for rebuilding.

None includes TaleWorlds binaries. The mod runs independently of the desktop application and has no required Harmony, MCM, ButterLib or other mod-framework dependency. Separation does not guarantee acceptance by a hosting service or security scanner.

The Desktop archive includes bilingual antivirus-reporting instructions in its README (source: DESKTOP.md). Keep the exact release binaries when investigating an alert. A suspected false-positive report should identify the affected file's SHA256, not just its filename or the enclosing ZIP. No Avast approval or code-signing certificate is included in this preview.

These are local developer-preview packages. Do not describe them as runtime-certified until the native checks in VALIDATION.md are completed. STORE_DESCRIPTION.md and STORE_DESCRIPTION.es.md are editable listing drafts, not published pages.

After building and packaging, run `python tools/audit_package.py --version 1.1.0`. This compares module and desktop assemblies, manifests, the native prefab and documentation with local outputs; checks ZIP integrity; and rejects game DLLs, save files, unsafe archive paths and bundled Harmony/MCM/ButterLib framework DLLs in module/developer archives. Its hash report identifies the exact archives checked. Run it again after regenerating either archive.

For Steam Workshop, follow the [official TaleWorlds publishing workflow](https://moddocs.bannerlord.com/steam-workshop/uploading_updating_mod/), including exporting the selected module through the modding toolkit for client use before uploading. The local ZIP does not prove that toolkit publishing or Workshop installation works. The official uploader uses a module folder and listing metadata; upload the main module and optional examples as appropriate separate items, with the examples depending on the main item. No Workshop item IDs have been created by this project.

For Nexus, use the Modules archive for manual installation and the developer archive as an optional source/SDK download. Distribute the desktop application separately where executable downloads are permitted. Choose the license, credits and distribution permissions before publishing. This project has not uploaded files or accepted publisher terms on your behalf.
