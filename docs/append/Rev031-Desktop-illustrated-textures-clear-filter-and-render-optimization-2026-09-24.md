# Rev031 — Desktop illustrated textures, clear filter, and render optimization

**Date:** 2026-09-24  
**Release line:** Calradia Forge 22.0.0  
**Scope:** WPF decorative resources, localized search clearing, and measured render-harness optimization.  
**Distribution:** Source follow-up only; no ZIP or product package regenerated.

## Local illustrated resources

Three original ImageGen masters are now processed by the existing deterministic local pipeline and packaged only as optimized application resources:

- `desktop-titlebar-heraldic-frame-v1.png` — 512 × 32 RGBA derivative for the passive caption frame.
- `desktop-rail-etched-field-v1.png` — 96 × 288 RGBA derivative for the operational navigation rail.
- `desktop-card-corners-botanical-v1.png` — 270 × 90 RGBA derivative for the tool summary card.

The new packaged variants total 92,537 bytes compressed and 273,328 decoded RGBA bytes, within the agreed additional 1 MiB compressed and decoded budgets. They remain local assembly resources. Decorative layers do not receive hit testing; the evidence ledger, command area, editable inputs, and result surfaces remain unobstructed. The existing seal is unchanged. War Table and Parchment Light can show the accents, while High Contrast remains solid.

## Localized search clearing

The operational search now has a compact, localized clear button. A XAML trigger shows it only when the existing filter contains text. Its click clears the filter and increments the existing search-focus request so focus returns to the search field. The control is hosted by the existing window and presentation model; no public ViewModel property, route, command identifier, IPC contract, or product API was added.

The generated UI dictionaries now include the clear label and restore the localized keys already consumed by visible XAML controls, with parity across all 13 supported languages. This preserves the existing display labels for the rail, native file/folder pickers, evidence empty state, title seal, decorative toggle, and window controls.

## Measured harness change

The pre-change, same-machine render-BAT baseline was measured five times at 6.641, 6.730, 6.069, 6.337, and 6.309 seconds; the median was 6.337 seconds. Five final runs measured 6.529, 5.727, 5.481, 5.846, and 6.191 seconds; the median was 5.846 seconds, 7.7% lower than that fresh baseline. Each run passed 272 WPF cases and recorded 148 render/layout passes. The final in-process harness duration median was 5.192 seconds. The prior observed localization-and-scale phase took 2,163.6 ms; the final five-run phase median was 1,058.8 ms. That phase comparison is diagnostic because the former value is one baseline sample, not a five-run phase median.

This improvement is scoped to the test harness: its presentation matrix no longer removes and reapplies the same theme dictionary for every language when the theme is unchanged. It continues applying localization and still renders every language, theme, and supported scale. Harness timing does not claim end-to-end latency in the running product.

## Validation and limits

- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause`: build with zero warnings/errors; Desktop 51/51; WPF render/resource tests 272 cases.
- Five calls to `tools/Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause`: all passed with 148 layout passes per run; final median 5.846 seconds.
- `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check`: deterministic derivatives and packaged resource budget passed.
- The render matrix retained all 194 routes, 13 languages, three themes, and 100%, 125%, 150%, and 200% scale coverage. It additionally exercised localized clear-search visibility, clearing behavior, and keyboard focus restoration.
- The current WPF build was launched by a temporary `.bat` wrapper around the source build output. Read-only UI Automation identified `Calradia Forge 22.0.0`, the search field, command palette, and title-bar controls; no controls were activated.

The new artwork has not been validated in a Bannerlord session. No game, campaign, or battle was started; no ZIP was generated. Product version, public API, IPC, routes, commands, preferences, and game resources remain unchanged.

This entry is append-only and does not revise Rev030 or earlier evidence.
