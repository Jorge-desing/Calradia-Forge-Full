# Calradia Forge 19.0.0 — short validation

Validation is deliberately bounded. It exercises the desktop workbench and packaging without loading a campaign or battle, changing a save, or collecting a long performance sample.

## Desktop

- `CalradiaForge.Desktop.RenderTests`: 190 explicit War Table routes, 20 representative routes across War Table/Parchment Light/High Contrast and 100/125/150/200% scales, plus 13 languages × 4 scale factors, with actual WPF template instantiation and read-only raw-result binding.
- The shell uses an explicit catalog and `ContentControl`; page selection creates a `WorkbenchPageViewModel`, disposes the previous page, and retains only evidence DTOs.
- The left rail renders the catalog through ordered, collapsible `ToolGroupViewModel` sections with counts, semantic glyphs, expansion retention, independent scroll, and separate bounded Pinned/Recent viewports.
- Keyboard commands are bound in XAML (`Ctrl+K`, `Ctrl+F`, `Ctrl+Enter`, `Esc`). Favorites and recent tools are bounded in the shell; no visual-tree traversal is used.
- Theme and language preferences use atomic replacement under the user data folder. Unknown or malformed values fall back to War Table and English.
- CommunityToolkit.Mvvm 8.4.2 drives the observable/command compatibility layer; MaterialDesignThemes 5.3.2, WPF UI 4.3.0, and MahApps.Metro.IconPacks 6.2.1 are instantiated in the WPF presentation surface and recorded in `THIRD_PARTY_NOTICES.md`.

## Automated suite

The short build runs Core, ForgeWeave, Desktop structural, WPF render, native-resource and archive checks. Missing or unsupported analyzer input is reported as **Not run** or **Unsupported**. The suite does not claim a native game result when the Steam surface is unavailable.

## Packaging

The release contains exactly:

- `CalradiaForge-Modules-19.0.0.zip`
- `CalradiaForge-Source-SDK-19.0.0.zip`
- `CalradiaForge-Desktop-19.0.0.zip`

Desktop includes `Run-CalradiaForge-Desktop.bat` and no app-host executable. The archive audit also requires the published CommunityToolkit.Mvvm, MaterialDesignThemes.Wpf, Wpf.Ui, and MahApps.Metro.IconPacks assemblies. Manifests, assemblies, XML, hashes, archive paths, absent saves and excluded game DLLs are audited.

## Native boundary

`Test-BannerlordPreflight.ps1` checks the installed Steam game surface and deployed module version. `launch_bannerlord.ps1` uses `steam://rungameid/261550` only after preflight. If the available computer-use surface cannot expose a native window, Framework inspection, `ForgeReady` replay and capability negotiation remain pending rather than being reported as passed.

No campaign, battle, endurance run, or prolonged native probe was executed for this release.


