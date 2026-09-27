# Calradia Forge 21.0.0 validation

Maintenance validation run: 2026-09-22. The release version remains 21.0.0. No campaign, battle, endurance run, or prolonged performance capture was used.

## Passed on the current workspace

- `tools/Run-CalradiaForge-Tests.bat --no-pause` built the solution with 0 warnings and 0 errors, then ran the suites through their batch entry points: Core 231/231, ForgeWeave 31/31, Desktop 39/39, and WPF render/resources 263 cases in 14.024 seconds.
- Resource Browser fixtures passed dry-run, staged-hash, backup, module-ID, missing/empty/invalid-package, and read-only inspection checks. Invalid and truncated TPAC markers are rejected; fixture coverage also checks missing SpriteData categories, malformed metadata, and atlas-count mismatches.
- `tools/Inspect-CalradiaForge-Tpac.bat --validate-only --no-pause` checked the source package: 538 bytes, `TPAC` marker, SHA-256 `7DAC28A71B23C00D28F59F5E110B62F17692F2D82910F05A185DC713F5A579E8`. This reads the four-byte marker and hashes the file; it does not parse or certify the payload.
- `python tools/validate_game_icon_assets.py --require-tpac` validated nine attributed icons, source atlas metadata, and the present runtime package. The existing Modules archive contains the same 538-byte TPAC with the same marker and hash.
- `python tools/audit_localization.py` validated all 13 native and Desktop catalogs: 195 native keys per language and 193 Desktop keys per language.
- `python tools/audit_package.py --version 21.0.0` verified the three existing archives, manifests, XML, ZIP integrity, required files, and exclusions. Evidence is in `artifacts/package-audit-2100.json`; hashes are in `artifacts/package-sha256-2100.txt`.

## Limits and release state

- This maintenance pass did not launch Bannerlord or TpacTool. The 21.0.0 changelog records the earlier short Modding Kit menu check; it was not repeated here. No campaign or battle was loaded.
- The local TpacTool is 0.4.0. Upstream describes that release as targeting Bannerlord 1.8.0 beta; compatibility with the project's 1.4.8 target is unverified. It is an optional viewer/exporter, not an importer or TPAC packer.
- The three 21.0.0 archives are the already-built release artifacts. They passed the archive audit, but they do not include the new unreleased analyzer and inspection-script refinements from this maintenance pass. Regenerate archives after assigning the next release version before distributing these changes.
