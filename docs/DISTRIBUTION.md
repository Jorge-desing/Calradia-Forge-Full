# Distribution preparation

`Directory.Build.props` is the source of the product version, currently `25.2.0`. The canonical packaging pipeline reads that value and rejects a conflicting version. A successful run creates these local archives under `artifacts/`:

- `CalradiaForge-Modules-25.2.0.zip`: the main module plus the Examples, Price Provider, Price Consumer, and Content Showcase modules. It contains the game-module assemblies and their packaged data, not the Desktop application.
- `CalradiaForge-Desktop-25.2.0.zip`: the optional WPF workbench, its managed dependencies, runtime configuration, notices, attribution, and `Desktop/Run-CalradiaForge-Desktop.bat`. It has no app-host `.exe` and requires the .NET 8 Windows Desktop Runtime on the target PC. Its root `README.md` is copied from `docs/DESKTOP.md`.
- `CalradiaForge-Source-SDK-25.2.0.zip`: source, examples, tests, templates, localization, assets, documentation, generated DocFX site, and the .NET Framework SDK assembly/XML reference under `SDK/`. It does not include compiled game-module output or the published Desktop application.

The archives do not package TaleWorlds assemblies, Harmony runtime DLLs, user saves, the repository `.venv`, or Python site packages. The package archive checks also reject unsafe/duplicate entries and development scripts; the sole script exception is the Desktop runtime launcher named above. The Desktop archive is separate from Bannerlord's `Modules` folder. A package audit or hash does not establish an antivirus vendor verdict, code-signing validation, or acceptance by a hosting service.

## Build and verify local archives

If the repository Python environment has not been prepared, run `tools\Setup-CalradiaForge-Python.bat --no-pause`. Then run the canonical release entry point from any directory:

```bat
tools\Package-CalradiaForge.bat
```

The BAT delegates to `tools/package.ps1`; it does not publish or upload the result. With its default options the pipeline restores and builds the solution, runs the Content Showcase and full test BATs, builds the documentation and in-game help, validates generated game icons and TPAC structure, checks module XML, stages the three archives, and applies ZIP-entry checks. The TPAC reader's default check is structural; it does not decode texture pixels or prove that the game imported/rendered the resource. A successful pipeline then runs the local archive audit and writes:

- `artifacts/package-audit-2520.json`, with per-archive metadata including entry counts and SHA-256 values.
- `artifacts/package-sha256-2520.txt`, with SHA-256 values for the three archive files.

The audit checks required archive content and roots, duplicate/unsafe paths, ZIP integrity, module manifest versions, packaged XML, excluded game assemblies and save data, absence of app-host executables and disallowed scripts, SDK-template files, and required icon attribution. These reports describe the exact local archives inspected. Compare the manifest hashes with the files you intend to distribute; neither report proves that Bannerlord loaded the module or that an external platform accepted an upload.

## External distribution and runtime limits

For Steam Workshop, consult the [official TaleWorlds publishing workflow](https://moddocs.bannerlord.com/steam-workshop/uploading_updating_mod/). The local package pipeline does not export a client module through the Modding Kit, create or update a Workshop item, upload files, or verify Workshop installation. Those steps and an actual game-load check are separate from ZIP construction, audit, and hashing.

For Nexus or another host, use the Modules archive as a manual-install package and offer the Source-SDK and Desktop archives separately where appropriate. The package script does not upload files or accept publisher terms. Choose the license, credits, and redistribution permissions before publishing. The source archive contains documentation drafts; their presence does not mean that a store listing has been published.
