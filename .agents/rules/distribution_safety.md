---
name: distribution-safety
description: Guidelines to prevent antivirus false positives and automated quarantines when distributing mods and apps.
trigger: always_on
---

# Distribution Safety Guidelines

When packaging and distributing applications (e.g., for Nexus Mods, Steam) or building .NET Desktop executables, adhere to these rules to prevent false positive malware flags and automated quarantines:

1. **Assembly Metadata:** Always include `<Company>`, `<Product>`, `<Description>`, and `<Copyright>` tags in the `.csproj` or `Directory.Build.props` for .NET executable projects. Generic executables without this metadata are aggressively flagged by Avast, Windows Defender, and VirusTotal.
2. **Exclude Build Scripts:** Never include shell scripts, batch files (`.bat`), or PowerShell scripts (`.ps1`) in user-facing distribution archives unless strictly required. Modding sites like Nexus Mods will automatically quarantine archives containing these extensions.
3. **AppHost False Positives:** If an unsigned .NET Desktop app `.exe` is still being falsely flagged by antivirus engines despite having Assembly Metadata, completely disable the executable generation by setting `<UseAppHost>false</UseAppHost>` in the `.csproj`. Distribute the app as a framework-dependent `.dll` and provide a simple `.bat` script (e.g., `start "" dotnet MyApp.dll`) for users to launch it.
4. **Mark of the Web (`Zone.Identifier`) & Dynamic Assembly Loading (`0x80131515`):** Files downloaded from the internet or unpacked from zip archives frequently receive the NTFS `:Zone.Identifier` alternate data stream. In .NET Framework and PowerShell, dynamic loading APIs (like `[System.Reflection.Assembly]::LoadFrom()`) fail with `0x80131515` (`NotSupportedException: An attempt was made to load an assembly from a network location...`). Packaging and distribution workflows must ensure dependencies are unblocked via `Unblock-File` or stripped of zone streams prior to archiving, and automated preflight tools should verify stream absence.
5. **Exclusion of Working Backups and Development Residue:** Distribution packages must strictly exclude working backup files (e.g., `*.bak.*`, `*.backup`, `*~`), scratch files, local debug dumps (`*.log`, `*.cfcrash`), and build intermediate artifacts (`obj/`, `bin/Debug/`). Shipping development residue bloats archives and triggers heuristic antivirus warnings.
6. **Exclusion of Game Engine DLLs and User State:** Never include first-party game engine assemblies (`TaleWorlds.*.dll`) or user save files (`*.sav`) inside mod or desktop distribution packages. Packaging proprietary engine binaries violates publisher license agreements and risks severe version-mismatch crashes on end-user setups. Distribute only mod-owned compiled assemblies and declared assets.
