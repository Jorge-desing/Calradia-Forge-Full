# Calradia Forge 22.0.0

22.0.0 adds a full static sprite-pipeline audit to the existing Gauntlet asset diagnostics. It checks `SpriteData` sheet references and bounds, safe part paths, source PNG header dimensions, and atlas dimensions before the existing limited TPAC marker check. It also retains the Gauntlet UI extension catalog, in-game assembly workbench, offline SDK help, and separate Desktop workbench. See [short validation](docs/VALIDATION-22.0.0.md) and [changelog](docs/CHANGELOG.md).

Calradia Forge is a standalone developer suite for **Mount & Blade II: Bannerlord 1.4.8**. It provides an in-game Forge panel, a separate Windows desktop workbench, and the ForgeWeave SDK. English is the authoritative source language; Spanish documentation is maintained separately.

Forge does not require Harmony, MCM, ButterLib, a network service, or another runtime mod framework. ForgeWeave is its supported cooperative extension path. It dispatches only from Forge-owned official lifecycle callbacks; it is not a general method-interception library.

## Downloads

Release 22.0.0 has exactly three archives:

- `CalradiaForge-Modules-22.0.0.zip` — runtime modules, required AsmResolver runtime dependencies, attributed game icon assets, and third-party notices.
- `CalradiaForge-Source-SDK-22.0.0.zip` — source, SDK, examples, DocFX configuration and generated static site, documentation, and developer material.
- `CalradiaForge-Desktop-22.0.0.zip` — the optional .NET 8 desktop application, without an app-host EXE.

**Release hold:** a follow-up read-only parse found that the 538-byte Gauntlet TPAC inside the 22.0.0 Modules archive is not readable by the supplied `TpacTool.Lib` metadata reader. The ZIP exists, but its in-game icon texture is not verified; do not publish it as a working icon release until the atlas is imported again through Resource Browser and the package preflight passes. See [icon asset workflow](docs/GAME_ICON_ASSETS.md) and [follow-up validation](docs/VALIDATION-22.0.0.md).

## Install

1. Extract `CalradiaForge` from the Modules archive into Bannerlord's `Modules` folder, then enable it after Native and SandBoxCore in the official launcher.
2. Install the optional examples only when you intend to use them.
3. Extract the Desktop archive outside the game directory. Install the .NET 8 Windows Desktop Runtime, then start it with `dotnet Desktop\CalradiaForge.Desktop.dll`.
4. Run `tools/Test-BannerlordPreflight.ps1` before a local probe. It reads the game surface, deployed manifest, selected launcher order, and BLSE presence without changing Steam, BLSE, launcher settings, or saves. `tools/launch_bannerlord.ps1` then starts the short probe through Steam (`steam://rungameid/261550`), never by launching `Bannerlord.exe` directly.

## Verified tooling

Desktop actions that audit, scan, validate, check, guard, parse, verify, or inspect a supported file type run bounded local analyzers. Results state whether they are verified file evidence, structural analysis, template output, or unavailable. Inputs that are absent or unsupported report **Not run** or **Unsupported**; they never claim a fabricated pass.

Supported analysis includes manifests, dependencies, DLL declarations, XML, localization, Gauntlet input safety, C# campaign/save/mission/quest rules, audio/troop/item structures, watchdog logs, crash metadata, and ZIP safety. Reports retain analyzer identity, source locations, severity, evidence, recommendations, limits, and run metadata.

ForgeWeave Replay Lab retains bounded copies of Forge events. A replay accepts one retained sequence only, requires handler opt-in, matches the current context exactly, retains the existing test and copied-campaign writer gates, and never records a replay as a new source. It is controlled event verification, not arbitrary callback injection.

The native panel follows Bannerlord's active text language automatically. Desktop uses an internal MVVM shell, an explicit reviewed tool catalog, cancellation-aware work orders, and a WPF `ContentControl` that releases inactive pages. English is the authoritative resource source, with equivalent dynamic catalogs for all thirteen Bannerlord desktop languages. Code identifiers, paths, and raw evidence remain untranslated.

Short desktop measurements record duration and current-thread allocations for language changes, tool changes, local analysis, and named-pipe operations. They are observations of those desktop operations, never a claim about whole-game performance.

See [ForgeWeave documentation](docs/FORGEWEAVE.md), [Gauntlet extension guide](docs/GAME_UI_EXTENSIONS.md), [assembly workbench](docs/ASSEMBLY_WORKBENCH.md), [desktop documentation](docs/DESKTOP.md), [system design](docs/SYSTEM_DESIGN.md), and [validation evidence](docs/VALIDATION-22.0.0.md).

The Desktop workbench now includes **FBX ASCII Preflight** under Assets. It and [`tools/Inspect-CalradiaForge-Fbx.bat`](tools/Inspect-CalradiaForge-Fbx.bat) use bounded, read-only analysis to list model, geometry, and material declarations, report LOD naming hints, and surface Camera/Light nodes for human review. Desktop forwards cancellation into its analyzer; the console BAT can be interrupted from its window. Neither parses binary FBX, validates geometry or resolves materials against game assets, nor imports resources. See the [asset automation evidence review](docs/ASSET_AUTOMATION_EVIDENCE.md) for limits and the verified workflow.

For the optional, experimental Resource Browser UI helper, see [Bannerlord Import Automation](docs/BANNERLORD_IMPORT_AUTOMATION.md). It is not a module dependency and its UI import controls remain unverified across Editor versions.






