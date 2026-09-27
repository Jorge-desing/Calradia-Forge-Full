# Calradia Forge third-party notices

Calradia Forge 22.0.0 targets Bannerlord 1.4.8. The Modules archive includes the static .NET metadata reader and its runtime dependencies because the in-game Assembly Workbench uses them. The Desktop archive remains separate and contains only Desktop runtime dependencies. DocFX is a build tool and is not required by Bannerlord or Desktop at runtime.

## Runtime packages in Modules and Desktop

| Package | Version | License | Use | Source |
| --- | --- | --- | --- | --- |
| AsmResolver.DotNet | 6.0.1 | MIT | Read PE/.NET metadata and write the explicitly requested copy-only assembly version transform. | https://github.com/Washi1337/AsmResolver |
| AsmResolver.PE | 6.0.1 | MIT | PE metadata dependency of AsmResolver.DotNet. | https://github.com/Washi1337/AsmResolver |
| AsmResolver.PE.File | 6.0.1 | MIT | PE file dependency of AsmResolver.DotNet. | https://github.com/Washi1337/AsmResolver |
| AsmResolver | 6.0.1 | MIT | Core metadata dependency of AsmResolver.DotNet. | https://github.com/Washi1337/AsmResolver |
| MonoMod.Backports | 1.1.2 | MIT | Runtime compatibility dependency of AsmResolver. | https://github.com/MonoMod/MonoMod.Backports |
| MonoMod.ILHelpers | 1.1.0 | MIT | Runtime compatibility dependency of AsmResolver. | https://github.com/MonoMod/MonoMod |
| System.ValueTuple | 4.5.0 | MIT | .NET Framework compatibility dependency. | https://github.com/dotnet/corefx/blob/master/LICENSE.TXT |
| IsExternalInit | 1.0.3 | MIT | Compile-time compatibility dependency; no runtime assembly is shipped. | https://www.nuget.org/packages/IsExternalInit/ |

AsmResolver parses the selected file as data on a worker thread. It does not load or execute that file. Version editing is opt-in, rejects signed and mixed-mode assemblies, preserves the original, creates a separate output and source backup, records hashes, and reopens the result for validation. It is not an IL editor or a compatibility/safety verdict.

## Desktop packages

| Package | Version | License | Source |
| --- | --- | --- | --- |
| CommunityToolkit.Mvvm | 8.4.2 | MIT | https://github.com/CommunityToolkit/dotnet |
| MaterialDesignThemes | 5.3.2 | MIT | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit |
| DocFX (build tool only) | 2.77.0 | MIT | https://github.com/dotnet/docfx |

The Desktop project also references AsmResolver.DotNet 6.0.1 for its separate offline assembly workbench. MaterialDesignThemes includes its upstream asset notices in the Desktop distribution. Desktop is published without an app-host `.exe`.

## Optional local Editor UI automation helper

| Package | Version | License | Use | Source |
| --- | --- | --- | --- | --- |
| FlaUI.Core | 5.0.0 | MIT | UI Automation client for the optional Resource Browser helper. | https://github.com/FlaUI/FlaUI |
| FlaUI.UIA3 | 5.0.0 | MIT | Windows UIA3 provider used by the optional Resource Browser helper. | https://github.com/FlaUI/FlaUI |

These packages are referenced only by the local `BannerlordImportAutomation.csproj` development helper. They are not runtime dependencies and are not included in Modules or Desktop; the Source-SDK contains the project file and source, not the built helper executable. See the upstream [license](https://github.com/FlaUI/FlaUI/blob/main/LICENSE.txt).

## Lucide icons

The dependency-free vector marks in `src/CalradiaForge.Desktop/Resources/TacticalIcons.xaml` are adapted from Lucide `clock-3`, `pin`, `plug`, `shield-check`, `alert-triangle`, `search`, `contrast`, `map`, `compass`, `target`, `shield`, `swords`, and `flag` icons. They are embedded as WPF geometries; no Lucide package or runtime asset is bundled. Lucide Icons and Contributors, ISC License: https://github.com/lucide-icons/lucide/blob/main/LICENSE

## Game-icons.net resources

The 17 selected tactical icons below come from the [game-icons/icons](https://github.com/game-icons/icons) project. Each source SVG is bundled and converted into a recolorable WPF `Geometry` and a transparent 96×96 RGBA Gauntlet sprite part. The black background is removed during conversion; the source files remain available in the package. All 17 individual source pages identify the listed author and mark the icon CC BY 3.0. The [CC BY 3.0 license](https://creativecommons.org/licenses/by/3.0/) allows sharing and adaptation with attribution, a license link, and an indication of changes. These records are included in `src/CalradiaForge.Desktop/Resources/GameIcons/ATTRIBUTION.json`; the same records are copied to the Modules and Desktop archives. `tools/generate_game_icons.py` creates the attribution files and icon assets. No artwork is fetched at runtime.

| Icon | Artist | Source | License | Changes |
| --- | --- | --- | --- | --- |
| `archery-target` | Lorc | [source](https://game-icons.net/1x1/lorc/archery-target.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `compass` | Lorc | [source](https://game-icons.net/1x1/lorc/compass.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `crossed-swords` | Lorc | [source](https://game-icons.net/1x1/lorc/crossed-swords.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `gear-hammer` | Lorc | [source](https://game-icons.net/1x1/lorc/gear-hammer.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `gears` | Lorc | [source](https://game-icons.net/1x1/lorc/gears.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `knight-banner` | Delapouite | [source](https://game-icons.net/1x1/delapouite/knight-banner.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `magnifying-glass` | Lorc | [source](https://game-icons.net/1x1/lorc/magnifying-glass.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `scroll-unfurled` | Lorc | [source](https://game-icons.net/1x1/lorc/scroll-unfurled.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `stopwatch` | Lorc | [source](https://game-icons.net/1x1/lorc/stopwatch.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `open-book` | Lorc | [source](https://game-icons.net/1x1/lorc/open-book.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `puzzle` | Delapouite | [source](https://game-icons.net/1x1/delapouite/puzzle.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `files` | Delapouite | [source](https://game-icons.net/1x1/delapouite/files.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `eye-target` | Delapouite | [source](https://game-icons.net/1x1/delapouite/eye-target.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `test-tubes` | Lorc | [source](https://game-icons.net/1x1/lorc/test-tubes.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `histogram` | Delapouite | [source](https://game-icons.net/1x1/delapouite/histogram.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `anvil` | Lorc | [source](https://game-icons.net/1x1/lorc/anvil.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |
| `plug` | Delapouite | [source](https://game-icons.net/1x1/delapouite/plug.html) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Removed black background; converted foreground to recolorable WPF geometry and transparent 96×96 RGBA sprite. |

The packaged `GUI/SpriteParts/Config.xml` marks the generated sprites as always loaded. The native prefab uses the icons as semantic marks for its navigation and context cards. Bannerlord's SpriteSheetGenerator and Resource Browser must create the engine texture package (TPAC) before the icons can render in game.

## Optional local TPAC viewer

TpacTool 0.4.0 is an optional, user-supplied viewer/exporter referenced by `tools/Inspect-CalradiaForge-Tpac.bat`; its executable and dependencies are not copied into any release archive. The upstream project is MIT-licensed: https://github.com/szszss/TpacTool and https://github.com/szszss/TpacTool/blob/master/LICENSE. Upstream release 0.4.0 targets Bannerlord 1.8.0 beta, so compatibility with this project's Bannerlord 1.4.8 target is unverified. TpacTool does not import sprites or create TPAC files; use Bannerlord Resource Browser for that workflow.

The optional local `TpacTool.Lib.dll` 0.1.0 from the same MIT project is used by the Resource Browser collection and release-preflight scripts to parse package headers and asset metadata. It is not shipped in Modules, Source-SDK, or Desktop. Payload decoding and rendering checks remain outside this reader's scope.
