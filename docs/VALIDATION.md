# Calradia Forge 14.4.0 validation evidence — 2026-09-22

Historical 14.4.0 results follow. Current hotfix evidence is in [VALIDATION-14.4.1.md](VALIDATION-14.4.1.md). The 14.4.0 source checks did not catch the real WPF startup binding failure; 14.4.1 adds a real window/template rendering gate and fixes that failure.

Validation was intentionally short. No endurance testing, prolonged performance sampling, campaign or battle session, state-changing test, save modification, or network operation was run.

## Automated checks

- `tools/build.ps1` completed with a Release build at **0 warnings** and **0 errors**.
- `CalradiaForge.Tests`: **220 passed, 0 failed** with the installed-module scan; the portable batch wrapper also passed its short suite with the scan skipped.
- `CalradiaForge.ForgeWeave.Tests`: **24 passed, 0 failed**, including replay opt-in, context and writer gates, replay-loop prevention, quarantine, failure isolation, reports, and bounded retained evidence.
- `CalradiaForge.Desktop.Tests`: **34 passed, 0 failed**, covering named-pipe protocol/reconnect, routed MVVM pages, cancellation/disposal, session transport, dynamic resource use, language-key parity, and bounded measurements.
- `tools/generate_desktop_resources.py`, `tools/audit_localization.py`, and `tools/desktop_visual_matrix.py` passed. Native files have UTF-8 BOM and **169** English-parity entries in each of 13 languages. Desktop has 13 catalogs with 169 keys and 13 WPF dictionaries with 19 UI keys. The matrix records **52 structural cases** for 13 languages at 100%, 125%, 150%, and 200%; it does not claim rendered visual inspection. Evidence: `artifacts/localization-audit-1440.json`, `artifacts/desktop-visual-matrix-1440.json`, and `artifacts/language-regeneration-1440.json`.
- `tools/Run-ShortTests.bat -SkipInstalledScan` was executed successfully. It is a Windows convenience wrapper that preserves the PowerShell exit code and output; `tools/build.ps1` remains the canonical CI and packaging command.

## Packaging

`tools/package.ps1 -Version 14.4.0` created exactly:

- `artifacts/CalradiaForge-Modules-14.4.0.zip`
- `artifacts/CalradiaForge-Source-SDK-14.4.0.zip`
- `artifacts/CalradiaForge-Desktop-14.4.0.zip`

The archive audit passed for expected content, XML, versions, hashes, duplicate and unsafe paths, absent saves, absent TaleWorlds assemblies, and absent app-host executables. The Desktop archive includes `Desktop/Run-CalradiaForge-Desktop.bat`, which launches the published DLL through the installed .NET 8 Windows Desktop Runtime. Evidence: `artifacts/package-audit-1440.json` and `artifacts/package-sha256-1440.txt`. Temporary staging folders were removed after the audit.

## Native preflight and Steam probe

The audited Modules archive was deployed over the four existing Forge module directories. `tools/Test-BannerlordPreflight.ps1 -Version 14.4.0` then confirmed the Native 1.4.8 game surface, deployed `v14.4.0` manifests, and the selected launcher order. It did not alter launcher data, Steam, BLSE, module order, or saves. Evidence: `artifacts/bannerlord-preflight-1440.json`.

`tools/launch_bannerlord.ps1 -Version 14.4.0` started the official launcher through `steam://rungameid/261550` and detected its process. It was closed through the standard Windows system-close command after the short probe. The available computer-use surface exposed no native application windows, so the launcher controls could not be used to enter the main menu. Native panel open/close, Framework journal, `ForgeReady` replay, and Desktop capability negotiation remain pending an interactive short Steam session. Evidence: `artifacts/bannerlord-steam-probe-1440.json`.
