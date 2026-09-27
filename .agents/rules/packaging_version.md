# Version Packaging Rule

Whenever you finish updating the mod to a new version, bumping the version, or finalizing a release, you **MUST** automatically generate the ZIP files for distribution (Steam/NexusMods) without waiting for the user to ask.

## Steps to follow:
1. Verify the solution compiles cleanly in Release mode (`dotnet build CalradiaForge.sln -c Release`).
2. Run the packaging script:
   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/package.ps1 -Version "<version>"
   ```
3. Verify that the three distribution `.zip` archives were generated in the `artifacts/` folder:
   - `CalradiaForge-Modules-<version>.zip`
   - `CalradiaForge-Source-SDK-<version>.zip`
   - `CalradiaForge-Desktop-<version>.zip`
4. Confirm that the SHA-256 hashes file (`artifacts/package-sha256-<version>.txt`) was generated and provide the archive paths and hashes in your final response.