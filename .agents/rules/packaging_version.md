# Version Packaging Rule

Whenever you finish updating the mod to a new version, bumping the version, or finalizing a release, you **MUST** automatically generate the ZIP files for distribution (Steam/NexusMods) without waiting for the user to ask.

The same completion gate applies to significant code/feature milestones at the existing version, as defined in [auto_packaging.md](auto_packaging.md), AGENTS.md and CODEX.md. Honor an explicit no-ZIP instruction for the current request. Do not bump the version solely to package, publish automatically, or stage generated archives in Git.

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
4. Require the archive audit to pass and verify freshly computed hashes against `artifacts/package-sha256-<version-without-dots>.txt` (for example, `package-sha256-2520.txt`). The audit uses the same version-without-dots suffix. Provide clickable absolute archive and evidence paths in the final response. Missing outputs or failed prerequisites leave packaging incomplete; a commit or green CI does not replace this gate.
