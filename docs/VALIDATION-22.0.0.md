# Calradia Forge 22.0.0 validation

Validation run: 2026-09-22. Checks were short and local. Bannerlord and TpacTool were not launched; no campaign, battle, endurance test, or prolonged performance sample was used.

## Passed

- `tools/Run-CalradiaForge-Tests.bat --no-pause`: build with 0 warnings and 0 errors; Core 231/231; ForgeWeave 31/31; Desktop 39/39; WPF render/resources 263 cases in 14.569 seconds.
- The batch runner's asset pipeline fixtures passed 6/6: PNG signature/IHDR read, malformed header rejection, release sprite-path acceptance, timestamped working-backup rejection, truthful Resource Browser instructions, and rejection of an unverified TPAC-readiness claim.
- `python tools/validate_game_icon_assets.py --require-tpac` validated all 9 attributed icon parts, `SpriteData`, source-part references, coordinates, PNG dimensions, the 2048×256 atlas, and runtime TPAC presence.
- `python tools/audit_localization.py` validated 13 native catalogs at 195 keys per language and Desktop catalogs at 193 keys per language.
- `tools/Inspect-CalradiaForge-Tpac.bat --validate-only --no-pause` was run against source and Steam-installed module packages. Both are 538 bytes with SHA-256 `7DAC28A71B23C00D28F59F5E110B62F17692F2D82910F05A185DC713F5A579E8`; both fail the TpacTool metadata reader with `Byte array for Guid must be exactly 16 bytes long`.
- Release packaging regenerates DocFX and contextual help, rebuilds icon resources, runs the test BAT, and audits the three archives for versions, XML, ZIP integrity, required files, duplicate/unsafe paths, timestamped backups, TaleWorlds assemblies, saves, and app-host EXEs.

## Limits

- PNG analysis reads only the signature and IHDR dimensions. It does not decode pixels, validate CRCs, or assess icon appearance.
- Desktop's TPAC finding checks the 36-byte v2 fixed header, a bounded declared asset count, and the table-of-contents range only. The separate `.bat` preflight parses bounded asset metadata, but the current source and installed files fail it. TpacTool does not decode texture payloads or prove Bannerlord 1.4.8 compatibility or visual rendering.
- No live Resource Browser or in-game menu check was repeated for this release.

## Post-release TPAC correction (2026-09-22)

A follow-up read-only check used the locally supplied `TpacTool.Lib.dll` 0.1.0 parser. It successfully read Native's `_shared.tpac` from this Bannerlord installation and enumerated 64 metadata assets. The 538-byte Calradia Forge sprite package instead fails while reading an asset GUID. This proves only that the file is not readable by this parser; the parser does not certify texture payloads or rendering. The 22.0.0 Modules archive contains that same package, so do not publish it as a verified working icon build.

The Resource Browser collection script now requires this metadata parse before copying a TPAC, and `tools/package.ps1` stops before archive creation when parsing fails. The short `.bat` regression suite passed with a known Native package, rejected a marker-only fake TPAC, and confirmed rejected replacements preserve the existing source package. A fresh Resource Browser import remains required before the module packages can be rebuilt. Bannerlord stayed closed; no new native UI check was performed.

## Generated-resource guidance correction (2026-09-22)

The icon generator previously rewrote `GUI/SpriteParts/README.md` with an unsupported claim that the runtime TPAC was included. It now documents the distinct atlas-generation and Resource Browser import stages, requires `Metadata validation: PASS` before collection or packaging, and states that metadata inspection does not decode textures or prove rendering. The resource validator checks this generated guidance, with positive and negative fixtures. The Resource Browser and asset-pipeline BAT suites passed after regeneration. The source and installed TPAC remain unreadable by the available metadata parser, so this documentation fix does not clear the release hold.

The inspection BAT also gained `--installed`, with `BANNERLORD_GAME_DIR` support for non-default Steam paths; a fixture verified it against a known Native TPAC. The assets-only BAT build and both resource fixture suites passed after this addition. The complete short test BAT was rerun after the guidance change: build 0 warnings/0 errors, Core 231/231, ForgeWeave 31/31, Desktop 39/39, asset fixtures 6/6, and 263 WPF render/resource cases in 14.569 seconds. The game and editor remained closed.

## Theme contrast correction (2026-09-22)

The seal monogram now uses a theme-specific `LogoTextBrush` chosen to contrast with its shield fill. ComboBox fields and popup items now use the active input, text, and highlight resources; keyboard focus uses the existing focus token. The Desktop-only BAT run passed with 0 warnings/0 errors, 39/39 MVVM/protocol tests, and 263 WPF render cases in 19.572 seconds. The render suite now checks monogram contrast and verifies selector foreground/background colors against every theme's palette.

## TPAC header diagnostics (2026-09-22)

The local asset analyzer now reads the complete 36-byte TPAC v2 fixed header instead of treating the four-byte marker as the only structural signal. Fixtures cover a plausible bounded entry count, a truncated fixed header, a zero count, an excessive count, and an out-of-file table. A plausible header still does not certify asset metadata or texture payloads; the finding points to the separate read-only TpacTool metadata preflight. The source and installed Calradia Forge TPACs remain unreadable by the available parser, so no release package was rebuilt and no in-game validation was claimed.

The short `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` run rebuilt the solution with 0 warnings/errors and passed Core 231/231 plus ForgeWeave 31/31. The read-only installed-module preflight still exits with code 1 and reports `Byte array for Guid must be exactly 16 bytes long`; this expected failure confirms the release blocker remains.

The read-only preflight reports the reader failure separately from the header/table result and includes the parser reason, without a PowerShell stack trace. It does not infer corruption or instruct an unconditional reimport. The Resource Browser BAT regression passed its malformed-metadata case and confirmed rejected collection leaves the existing source TPAC hash unchanged.

## Follow-up format-scope correction (2026-09-22)

The v2 analyzer now stops before reading version-specific fields when the package declares another format version and reports that case as unsupported. The 36-byte v2 range checks are limited to structural evidence. The official Bannerlord sprite workflow documents atlas generation followed by Resource Browser import; this project found no public C# FBX batch-import API or documented `.meta` sidecar contract in the installed managed assemblies or official asset instructions, so it does not generate speculative metadata files.

## Short source follow-up validation (2026-09-22)

The solution rebuilt with 0 warnings and 0 errors. The `.bat` suites passed: Core 231/231, ForgeWeave 31/31, Desktop 40/40, WPF render/resources 264 cases in 15.921 seconds, Asset Pipeline 7/7, and Resource Browser staging/parser fixtures. Desktop checks include file/folder picker selection, cancellation preserving the previous input, and disabling pickers after page disposal. The installed-module TPAC preflight remains intentionally nonzero: v2 header/table bounds pass, while TpacTool metadata parsing fails with `Byte array for Guid must be exactly 16 bytes long`; the file was not modified. No archives were rebuilt, and Bannerlord remained closed.

`tests/CalradiaForge.ResourceBrowser.Tests.bat --no-pause` passed its staging and parser fixtures, including concise failure output and source-hash preservation. `tests/CalradiaForge.AssetPipeline.Tests.bat --no-pause` passed 7/7 fixtures. `tools/Build-CalradiaForge-UiAssets.bat --dry-run --no-pause` validated nine attributed icons and the 2048×256 atlas; the official generator was not launched.

## Read-only FBX preflight follow-up (2026-09-23)

`tests/CalradiaForge.FbxPreflight.Tests.bat` passed its short fixture run. It verifies ASCII declaration extraction, choosing geometry declarations over paired model nodes, LOD advisories, material-name inventory, binary and malformed input reporting, per-file limits, early directory-discovery limits, report/input path boundaries, unchanged source hashes, and invocation through the shipped `.bat`.

The Desktop Assets tool uses the shared Core analyzer; its report preserves `Structural` provenance rather than labeling naming hints as verified imports. The complete short `tools/Run-CalradiaForge-Tests.bat --no-pause` run rebuilt the solution with 0 warnings/errors and passed Core 232/232, ForgeWeave 31/31, Desktop 41/41, and 265 WPF render/resource cases in 17.163 seconds. The master BAT also passed the Asset Pipeline fixtures (7/7), TPAC inventory comparison, Resource Browser staging/parser fixtures, Asset Batch Plan fixtures, and the FBX command-line fixtures. Core/Desktop fixtures cover cooperative cancellation, Camera/Light review hints, source preservation, and binary/malformed/limit cases. No FBX import, editor, compiler, or Bannerlord process was run. The existing 22.0.0 archives remain unchanged.

After bounding the CLI Camera/Light examples to sixteen names while retaining exact observed totals, `tests/CalradiaForge.FbxPreflight.Tests.bat` passed again, including its 20-camera truncation fixture. `tests/CalradiaForge.AssetPipeline.Tests.bat --no-pause` also passed 7/7 after the documentation update.

## TPAC inventory timestamp and strict-reader follow-up (2026-09-23)

`tests/CalradiaForge.TpacInventoryCompare.Tests.bat` passed after the diff began preserving each input report's `createdUtc` as normalized `capturedUtc`, printing both capture times in the CLI, and rejecting invalid or timezone-less timestamps. Fixtures verify that an explicit `+02:00` offset normalizes to UTC and that the values survive JSON export. Follow-up hostile-input fixtures also cover missing/null nested fields, malformed unsigned values, missing/zero/out-of-range table-of-contents lengths, trailing LF/CRLF, the unknown `-00:00` offset, scalar `assets` values, and source lengths just above the 128 MiB reader limit. The inventory test `.bat` now forwards `-RequireReader` and `-ToolDirectory`; the strict missing-reader case returns a nonzero result instead of being counted as a portable skip.

The default `tests/CalradiaForge.TpacInventory.Tests.bat` reported `SKIP`: Windows blocked the locally downloaded `TpacTool.Lib.dll` with `0x80131515`. Its real metadata-reader behavior therefore remains unverified in this run. The strict mode was separately checked against that blocked reader and returned the expected nonzero diagnostic. No TPAC, downloaded tool file, release archive, or game installation file was changed, and no archive was rebuilt.
