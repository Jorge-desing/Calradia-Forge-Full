# Rev026 — Desktop and Gauntlet visual round

**Date:** 2026-09-24  
**Release line:** Calradia Forge 22.0.0  
**Scope:** Desktop WPF themes, textures, navigation, and source-side Gauntlet validation.  
**Distribution:** Source follow-up only; no TPAC or ZIP changes.

## Desktop WPF

Desktop now uses locally packaged illustrated materials for War Table and Parchment Light; High Contrast keeps solid surfaces. The custom title bar exposes localized, named minimize, maximize, restore, and close controls. Its seal remains an accessible button bound to the existing command-palette command. A session-only decorative-accents toggle gates passive art, while the empty-evidence ledger presents localized copy, a passive illustration, and an action that opens the existing palette. ComboBox selected/focus states are theme-aware, and the main rail uses a recycling `VirtualizingStackPanel` while retaining every catalog route and separate Pinned/Recent history viewports.

The WPF resource check found 11 packaged local PNGs, including the Parchment-only `parchment-field-journal-ornament-v1.png` (1254×1254 RGBA; SHA-256 `B6EBC6EECA36266E53B53DE9BB372C07A655DF946B42B9D570714E417B163AD3`). The High Contrast and decorative-toggle checks confirmed solid themed surfaces and hidden decorative layers where required.

`tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` built the Desktop test dependency graphs with zero warnings and errors. Desktop unit tests passed 42/42; the WPF render suite passed 268 cases: 194 catalog routes, 13 languages across 100%, 125%, 150%, and 200% DPI, and three themes across the same four scales with 20 representative routes per theme/scale. The suite also checked the materialized footer and its independent history viewports, ComboBox contrast, title-bar accessibility/state transitions, empty-ledger layout, and unchanged user preferences. The full run took 10.047 seconds, with 507 render/layout passes and 3151.5 ms spent in `UpdateLayout`. Removing the central ApplicationIdle dispatcher drain improved the immediately preceding 12.3-second run, though this remains 1.387 seconds slower than the earlier 8.66-second baseline. This is WPF harness timing, not product runtime performance. Current previews and results are in `artifacts/desktop-render-tests-en-100-current.png`, `artifacts/desktop-render-tests-en-200-current.png`, `artifacts/desktop-render-tests-war-table.png`, `artifacts/desktop-render-tests-parchment.png`, `artifacts/desktop-render-tests-high-contrast.png`, and `artifacts/desktop-render-tests.json`.

## Gauntlet source validation

The source-side validator passed four prepared PNG contracts: cloth at 128×64 with alpha 22–24, overlay at 256×48 with alpha 0–112, brass rule at 128×16 with alpha 82–88, and felt at 128×32 with alpha 37–40. Texture preparation was deterministic, produced identical hashes, and left its sources unchanged. SpriteData was reported current, and the prefab structural audit reported zero errors and zero warnings. Core tests passed 232/232.

## Runtime boundary

The installed TPAC predates the source atlas. No TPAC replacement, Resource Browser import, or in-game render was performed; those checks remain pending. No campaign or battle was launched. Calradia Forge remains 22.0.0, and no ZIPs were generated.

This record is append-only and does not revise Rev021–Rev025 or earlier evidence.
