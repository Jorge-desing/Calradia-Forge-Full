# Gauntlet ImageGen texture masters

This directory contains local authoring images for the Calradia Forge Gauntlet UI. Current source masters are prepared deterministically by `tools/Prepare-CalradiaForge-ImageGenTextures.bat`; the script converts the ImageGen RGB/RGBA source into bounded RGBA8 atlas sprites and applies each configured alpha cap once. The source files are not runtime dependencies. Game-icons remain separately attributed and unchanged.

The two large material masters are full-coverage RGB images. Their prepared sprites therefore use a uniformly low alpha (up to the cap) over a solid Gauntlet base surface; the pipeline does not derive cut-out alpha from luminance. The heraldic overlay retains its authored transparency.

| Current master | Prepared sprite | Prepared size | Maximum alpha | Intended placement | Master SHA-256 |
| --- | --- | ---: | ---: | --- | --- |
| `forge_war_table_cloth_v2.png` | `forge_war_table_cloth_v2.png` | 1024×128 | 24/255 | Panoramic header surface | `A4C19F089FC3879FC4D4A3990D9171D306A47848B5C57F73B328C85FABE06F47` |
| `forge_rail_cartographic_field_v1.png` | `forge_rail_cartographic_field_v1.png` | 256×256 | 36/255 | Cartographic navigation-rail field | `479A16D70CDF76677D02958287E4F042CCCD8695986E4C8B8368E474E46CC728` |
| `heraldic_field_journal_overlay_v1.png` | `forge_heraldic_overlay.png` | 256×48 | 112/255 | Passive header ornament | `70E619A85CB15BB87BA8E87EE67BFEAEF28FB2F458C5559DA8431E4B71189E20` |
| `aged_brass_patina.png` | `forge_patina_brass.png` | 128×16 | 88/255 | Thin passive divider rules | `AA7782E9DB476D773F934AB05321CB359BF7D7713345D53129A6216216FA11DA` |
| `pine_felt.png` | `forge_pine_felt.png` | 128×32 | 40/255 | Card-edge and rail trim | `6D39A23E36912169D1C7FE988971864B8CF7C262E9708010C74B96EAC70B2316` |

## Current route header ornament masters

ImageGen produced one transparent master for each primary route. The exact prompts are recorded in [`prompts.md`](prompts.md), and the source-master SHA-256 values below are also stored in [`SHA256SUMS.txt`](SHA256SUMS.txt). Each source is prepared with the same deterministic crop and alpha-cap pipeline as the material masters; the runtime crop is RGBA8 at 128×64 with a maximum alpha of 112/255.

| Route | Source master | Prepared sprite | Runtime placement | Master SHA-256 |
| --- | --- | --- | --- | --- |
| Summary | `forge_header_summary_v1.png` (1774×887 RGBA) | `forge_header_summary_v1.png` (128×64) | x=256, y=114, 24×12; `IsSummaryActive` | `09799A47B4F53A499DCFBE00E698AC23A2915902D0A79DDF361165419191EF17` |
| Modules | `forge_header_modules_v1.png` (1774×887 RGBA) | `forge_header_modules_v1.png` (128×64) | x=256, y=114, 24×12; `IsModulesActive` | `9E25D780607FE385AF5387691642CB857992CF91CD5EB2135C3A75CAA1315B40` |
| Logs | `forge_header_logs_v1.png` (1774×887 RGBA) | `forge_header_logs_v1.png` (128×64) | x=256, y=114, 24×12; `IsLogsActive` | `4F8F7C367379B906C16B6279D4B95561E8B4CD9312AF6ABD2E164E8A65FC34EC` |
| Inspector | `forge_header_inspector_v1.png` (1774×887 RGBA) | `forge_header_inspector_v1.png` (128×64) | x=256, y=114, 24×12; `IsInspectorActive` | `27626009E8D5246AC0E5EE6B9BFD060B9AE83C90DA46EDA42C295934E12D3487` |
| Tests | `forge_header_tests_v1.png` (1774×887 RGBA) | `forge_header_tests_v1.png` (128×64) | x=256, y=114, 24×12; `IsTestsActive` | `C050E7641A4CE3A0729AD606488DC5E351057FBED7DDF4C75BD4797BC5D1D7EE` |
| Metrics | `forge_header_metrics_v1.png` (1774×887 RGBA) | `forge_header_metrics_v1.png` (128×64) | x=256, y=114, 24×12; `IsMetricsActive` | `8FDC889BB8F78A0C56EADE027EC5561986F5F671A191057F7790B10A85D87EA4` |
| Framework | `forge_header_framework_v1.png` (1774×887 RGBA) | `forge_header_framework_v1.png` (128×64) | x=256, y=114, 24×12; `IsFrameworkActive` | `13225DEDF61E3465DFE560DE2C58C7F4138088E26A16029BE74731A791C901FC` |
| Extensions | `forge_header_extensions_v1.png` (1774×887 RGBA) | `forge_header_extensions_v1.png` (128×64) | x=256, y=114, 24×12; `IsExtensionsActive` | `9EBDA29EBB2ADF306AAC42FCD53FED6F96DFD5215397B1F3FA1E9721EE4BD71` |

The ornaments are passive image layers. They are visible only on their matching primary route and are hidden on auxiliary and SDK destinations; they never receive pointer or keyboard input. The page title begins at x=282 to retain a two-pixel gap around the 24-pixel ornament.

`--check` performs two isolated preparations and compares output hashes without modifying `prepared/`; `--prepare` backs up any changed output and publishes the deterministic crops there. SpriteParts synchronization and atlas/SpriteData generation are separate steps. The official `SpriteSheetGenerator` owns atlas packing and SpriteData rectangles; until it is run and its output passes validation, registration and live rendering remain pending. Do not edit the atlas or SpriteData by hand.

## Archived first-round source masters

Superseded source artwork and prepared/runtime PNGs are retained under `archive/2026-09-25/` with verified SHA-256 records in `SHA256SUMS.txt`. They are historical inputs only and are not in the current preparation set. The historical prompts below describe that earlier direction.

## Historical generation prompts

**Pine woodgrain** — “A tileable dark pine-stained wood grain to replace geometric UI textures. Flat material swatch, even organic flowing grain, seamless on all edges, no focal feature; restrained hand-finished wood, matte, very low contrast, charcoal black and deep pine green. Texture only: no symbols, icons, border, seams, grids, map lines, geometry, diamonds, repeated motifs, high-contrast marks, or watermark.”

**Inkwash vellum** — “A seamless dark ink-washed vellum texture with organic age and pigment variation, replacing geometric map-line decoration. Flat front-facing swatch with diffuse clouded pigment and soft paper fibers, even, repeatable edges, no central emblem; matte, low contrast, deep pine charcoal, muted blue-green ink, old olive and smoke. No map lines, symbols, route marks, lettering, icons, geometry, grids, border, objects, high contrast, or watermark.”

**Aged brass patina** — “A seamless antique brass and verdigris surface texture to replace geometric woven, rosette, and ornamental UI patterns. Flat material swatch for thin border strips; evenly distributed brushed and softly tarnished texture, no focal emblem; matte, subdued highlights, low contrast, dark aged brass, muted olive-gold, deep green patina, charcoal shadows. No engraved lines, rosettes, rivets, repeating geometry, grids, symbols, letters, icons, borders, objects, or watermark.”

**Pine felt** — “Dark pine-green aged wool felt with naturally irregular fibers, replacing a geometric woven UI pattern. Flat front-facing material sample with uniform organic fibers and soft wear, no focal detail; matte, even, very low contrast, deep pine green, charcoal and muted verdigris. Seamless; no repeating motif, herringbone, diamonds, grid, stitching, symbols, lettering, emblem, trim, objects, bright marks, or watermark.”

## Prepared sprite contract

| Sprite | Size | Maximum alpha | Intended placement |
| --- | ---: | ---: | --- |
| `forge_war_table_cloth_v2.png` | 1024×128 | 24/255 | Panoramic header background |
| `forge_rail_cartographic_field_v1.png` | 256×256 | 36/255 | Navigation-rail material field |
| `forge_heraldic_overlay.png` | 256×48 | 112/255 | Passive header ornament with authored transparent margins |
| `forge_patina_brass.png` | 128×16 | 88/255 | Divider gaps outside controls and evidence |
| `forge_pine_felt.png` | 128×32 | 40/255 | Rail and briefing-card edge strips |

The masters are not used at runtime. Only the RGBA8 prepared sprites are registered in `SpriteParts/Config.xml` and packed by the official generator. The current registration count and atlas rectangles must be read from generated `CalradiaForgeSpriteData.xml`; do not hand-edit either generated file. Previous `forge_dark_wood`, `forge_inkwash`, and `forge_war_table_cloth` prepared sources are historical and must not reappear in the live sprite set.

## Desktop WPF companion textures

The WPF application packages its own local texture copies under `src/CalradiaForge.Desktop/Resources/Textures`; it does not read the Gauntlet atlas. The War Table and Parchment Light surfaces use the illustrated cloth and cartographic paper masters listed in `visual-round-prompts.md`. The custom WPF caption bar uses the following additional masters:

| Master | WPF packaged resource | Dimensions / alpha | SHA-256 | Placement |
| --- | --- | --- | --- | --- |
| `titlebar_embroidered_cloth_v1.png` | `titlebar-embroidered-cloth.png` | 2048×768, RGB | `722B23CE06322F2E7E2746BDFA539678E65F245B4E385F76F639E69D2DA3B155` | Low-opacity, narrow caption strip in War Table; parchment uses its own paper surface |
| `titlebar_heraldic_compass_v1.png` | `titlebar-heraldic-corner.png` | 1254×1254, RGBA, alpha 0–255 | `BEF2AAE01BD0D95B10D8E87884D1E4E7B8110B25A5D4A2045BD984569F44A0BC` | Small passive left-end caption medallion; hidden in High Contrast |

The title-bar buttons remain separate native WPF controls with localized accessible names, keyboard focus, and themed glyphs. Texture masters are authoring assets; only the WPF resources and prepared Gauntlet crops are packaged into their respective applications.

## Additional WPF header identity assets

| Master | Packaged resource | Dimensions / alpha | SHA-256 | Placement |
| --- | --- | --- | --- | --- |
| `header_heraldic_compass_v2.png` | `header-heraldic-compass-v2.png` | 1254×1254, RGBA, alpha 0–255 | `29D8F76FF79367405BE95E5CA1468190AC01E484A8B5F172ACC59D03EB9F1836` | Main header brand; keyboard-accessible button opens the existing command palette |
| `header_corner_engraving_v1.png` | `header-corner-engraving-v1.png` | 2172×724, RGBA, alpha 0–255 | `D61A91D9E9B0186E464E6CD451CACDD4F3EEB5239C3E1798048594495A9550CF` | Passive low-opacity header corners; center remains clear |
| `tool_card_top_corners_v1.png` | `tool-card-top-corners-v1.png` | 2172×724, RGBA, alpha 0–255 | `4801E3E10269A7D49E358891D673039EC3BECAE6095F92EAA10DA092493A3F74` | Passive top-corner accents on ToolPage title sections; inputs and evidence remain clear |

The exact prompts and source master records live in `visual-round-prompts.md`. The WPF image layers do not accept pointer input. High Contrast uses solid fills and a simple text fallback for the mark.

## WPF empty evidence ledger illustration

The Desktop-only empty-ledger artwork is a separate local WPF resource; it is not part of the Gauntlet atlas. It is shown only when the evidence ledger has no entries. The illustration itself is a passive, non-hit-testable image overlay and is hidden in High Contrast and whenever decorative accents are disabled. Any adjacent CTA remains a separate localized control; where the UI integration is enabled, it invokes the existing command-palette action rather than making the artwork interactive.

| Source master | WPF packaged resource | Dimensions / alpha | SHA-256 | Placement |
| --- | --- | --- | --- | --- |
| `C:/Users/Alex/.codex/generated_images/01a0729a-9f57-7173-9f44-d03d3737b50f/exec-5f692a76-c349-4d03-ab53-8cf8deb29e73.png` | `src/CalradiaForge.Desktop/Resources/Textures/evidence-ledger-empty-v1.png` | 1254×1254, RGBA, alpha 0–255 | `69C6CA93CEFCE9615D9F73D924236F042668909D64564DB39F9AABF83B9F5B2C` | Low-emphasis empty-ledger illustration beside localized empty-state text |

The source master and packaged resource were checked to have matching SHA-256 hashes. The exact generation prompt is appended to `visual-round-prompts.md`.
