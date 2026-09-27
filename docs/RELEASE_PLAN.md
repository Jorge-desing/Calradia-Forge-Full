# Calradia Forge 22.0.0 release

## Scope

22.0.0 is an English-first developer preview for Bannerlord Native 1.4.8 and .NET Framework 4.7.2. It strengthens the existing Gauntlet asset analyzer with structural validation of the Game-icons sprite pipeline. The 21.0.0 extension SDK, static assembly workbench, offline help, Desktop app, and ForgeWeave behavior remain the supported release surface.

## Sprite validation

For a selected module with generated sprite metadata, the analyzer verifies category and sheet IDs/dimensions, duplicate or missing sheet references, safe sprite-part paths, source-part existence, positive dimensions and coordinates, and part rectangles within the declared sheet. It reads the 24-byte PNG signature/IHDR header to compare source-part and atlas dimensions. It does not decode pixels, inspect image color/alpha data, validate PNG CRCs, or certify visual quality. The TPAC path still checks file presence, non-empty length, and the four-byte `TPAC` marker only; it does not parse the package payload.

`tools/validate_game_icon_assets.py` applies corresponding reference and dimension checks to the shipped Game-icons assets. `tools/Inspect-CalradiaForge-Tpac.bat` remains a read-only size, header, and SHA-256 preflight. TpacTool stays locally supplied and is not redistributed; its compatibility with the 1.4.8 target is unverified.

## Packaging

The release contains exactly `CalradiaForge-Modules-22.0.0.zip`, `CalradiaForge-Source-SDK-22.0.0.zip`, and `CalradiaForge-Desktop-22.0.0.zip`. The packager omits timestamped Resource Browser backups and the archive audit rejects them. Modules includes runtime assets, icon attribution, notices, and required game-module dependencies. Source-SDK includes source, SDK, examples, DocFX configuration/site, docs and development tools. Desktop remains separate and has no app-host `.exe`.

## Validation

Run the short batch-based build/tests, Python icon-resource audit, native localization audit, TPAC preflight, DocFX, and ZIP/package audits. Report source checks separately from a native Bannerlord session. Do not load a campaign or battle, run endurance tests, or capture prolonged performance samples.
