# Calradia Forge Release Packaging Architecture

Whenever a new version, milestone, or significant update is reached, you MUST automatically run the release packaging workflow and verify all distribution artifacts.

## 1. Release Packaging Command
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\package.ps1 -Version "<version>"
```
*(If `-Version` is omitted, `package.ps1` automatically detects the shared release version defined in `Directory.Build.props`)*.

## 2. Generated Distribution Archives
The packaging script produces three verified release archives in `artifacts/`:

1. **`CalradiaForge-Modules-<version>.zip`**
   - Contains the game modules ready for extraction into Bannerlord's `Modules/` directory:
     - `CalradiaForge/` (Core runtime, Mod, and Sdk assemblies in `bin/Win64_Shipping_Client/`, XML data, GUI assets)
     - `CalradiaForgeExamples/` (Example behaviors and components)
     - `CalradiaForgePriceProvider/` (Contract provider example)
     - `CalradiaForgePriceConsumer/` (Contract consumer example)
   - Verified SubModule manifests and third-party notices.

2. **`CalradiaForge-Source-SDK-<version>.zip`**
   - Clean source snapshot (excluding `bin/`, `obj/`, `.git/`, temporary files, and debug residue).
   - Generated DocFX static documentation site in `docs-site/generated-site`.
   - Standalone `CalradiaForge.Sdk.dll` and `CalradiaForge.Sdk.xml` for third-party mod developer integration.

3. **`CalradiaForge-Desktop-<version>.zip`**
   - Framework-dependent .NET 8 WPF Desktop workbench.
   - Published with `<UseAppHost>false</UseAppHost>` to prevent antivirus heuristics and false positive quarantine flags.
   - Includes `Run-CalradiaForge-Desktop.bat` launcher, attribution, and `README.md`.

## 3. Manifests & Integrity Evidence
Upon successful packaging, the following audit files are generated in `artifacts/`:
- **`package-sha256-<version>.txt`**: Cryptographic SHA-256 hashes of all three archives.
- **`package-audit-<version>.json`**: Automated audit report confirming archive hygiene, entry counts, and path safety.

## 4. Distribution Safety & Hygiene Checks
Every package must satisfy the following strict validation checks before release:
- **Zero AppHost EXEs**: The Desktop archive must never contain an executable (`.exe`). It must be launched via `Run-CalradiaForge-Desktop.bat` invoking `dotnet CalradiaForge.Desktop.dll`.
- **Zero Proprietary Engine Binaries**: No `TaleWorlds.*.dll` assemblies may be packaged.
- **Zero Debug / Runtime Dumps**: No `.cfcrash`, `.log`, `.backup`, or `*.bak.*` files may be included.
- **Zero Save State**: No `.sav` files or save-backup directories may be packaged.
- **TPAC Evidence**: Do not require TpacTool or its legacy reader script; the user removed TpacTool after frequent reader errors. Preserve the imported Steam runtime TPAC when deploying module changes, record its hash before deployment, and confirm that the game renders the referenced sprites. File presence or a parser result alone does not prove payload decoding or visual correctness.
