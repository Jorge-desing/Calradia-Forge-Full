# Rev122 — Localization parity and portable archive hashing

**Date:** 2 October 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Spanish SDK source catalog and release packaging hash generation.

## Observed issue and technical rationale

The canonical packaging pipeline stopped during native UI/catalog generation because two translated Spanish help strings used Spanish text as the catalog key instead of the corresponding English source keys. After that was corrected, the pipeline reached archive auditing but the current Windows PowerShell runspace could not resolve `Get-FileHash`, so it exited before writing its SHA-256 manifest.

## Technical solution and architectural decisions

- Updated only the two Spanish catalog keys to match their English source strings while preserving the existing Spanish translations.
- Replaced the package script's command lookup with streaming `System.Security.Cryptography.SHA256` hashing over each completed archive and deterministic disposal of the algorithm and file stream.
- Kept archives, audit JSON, and the hash manifest under ignored `artifacts/`; no version bump, publication, or repository staging of generated archives.

## Changes in assets, code and dependencies

Changed `localization/es.xml`, `tools/package.ps1`, and the bilingual changelog/appendix. The package workflow remains PowerShell-based behind `tools/Package-CalradiaForge.bat`; no runtime dependencies changed.

## Validation and evidence limits

- The final `tools\Package-CalradiaForge.bat` pipeline passed with clean build output, Core 411/411, ForgeWeave 73/73, Desktop 65/65, WPF 295 cases/308 layout-render passes, deterministic showcase generation/XSD, DocFX, and the archive audit.
- All three version 25.2.0 archives were present in `artifacts/`; independent streaming SHA-256 calculations matched the generated manifest.
- WPF values are harness results only. No live Bannerlord session, campaign, battle, or external publication was performed.

Product remains 25.2.0; `ForgeApi.Version` remains 13.
