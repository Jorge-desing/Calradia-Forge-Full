# Calradia Forge 21.0.1 validation

Validation run: 2026-09-22. This was a short maintenance/release pass. Bannerlord was not launched, and no campaign, battle, endurance run, or prolonged performance capture was used.

## Passed

- `tools/Run-CalradiaForge-Tests.bat --no-pause` passed after the version update: build with 0 warnings and 0 errors; Core 231/231; ForgeWeave 31/31; Desktop 39/39; WPF render/resources 263 cases in 14.293 seconds.
- The release packager regenerated the DocFX site and 24 contextual help strings for 13 languages, regenerated nine attributed icon sprite parts, and validated the 2048×256 atlas and runtime TPAC.
- `python tools/audit_localization.py` validated all 13 native catalogs at 195 keys per language and the Desktop catalogs at 193 keys per language.
- `tools/Inspect-CalradiaForge-Tpac.bat --validate-only --no-pause` checked the source package: 538 bytes, `TPAC` marker, SHA-256 `7DAC28A71B23C00D28F59F5E110B62F17692F2D82910F05A185DC713F5A579E8`. This verifies presence, size, marker, and hash only; it does not parse or certify the payload.
- `python tools/audit_package.py --version 21.0.1` passed for the three release archives. The audit checks package contents and ZIP integrity, module and assembly versions, XML, duplicate and unsafe paths, excluded TaleWorlds binaries, save absence, and absence of app-host `.exe` files. The audit JSON and SHA-256 list are written under `artifacts/` in the workspace.

## Limits

- This pass did not repeat a live Resource Browser or Bannerlord menu check. The 21.0.0 changelog records an earlier brief visual check; it is not evidence from this 21.0.1 pass.
- The local TpacTool is an optional user-supplied viewer/exporter. The preflight does not invoke it and does not claim compatibility with Bannerlord 1.4.8.
- No test executable was started directly; the repository's `.bat` runners hosted the test libraries.
