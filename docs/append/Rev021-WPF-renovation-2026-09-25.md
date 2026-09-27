# Rev021 — WPF workbench renovation and measured performance round

**Date:** 2026-09-25  
**Release line:** Calradia Forge 22.0.0 (unchanged)  
**Scope:** Desktop WPF shell composition, keyboard-accessible report navigation, local decorative resources, and measured render-harness optimization.  
**Distribution:** Source follow-up only; no ZIP or game package is in scope.

## Approved WPF workbench composition

This round recomposes the full Desktop shell around a compact header and context area, responsive operational navigation, a larger workbench, and combined report navigation that remains accessible from the keyboard. The change stays within the standalone WPF Desktop application and preserves its MVVM structure and existing contracts.

The workbench retains all 194 routes and existing commands, all 13 language catalogs, the War Table, Parchment Light, and High Contrast themes, and the 100%, 125%, 150%, and 200% scale coverage. The round does not reduce route or render coverage to improve timing.

## Local decorative resources

The approved artwork set consists of three local ImageGen authoring masters: `workbench-cartographic-board-v1.png`, `workbench-heraldic-rail-band-v1.png`, and `workbench-heraldic-shield-v1.png`. The existing deterministic texture pipeline is to produce optimized application resources from these masters. The decorations remain passive and do not receive input or hit testing. War Table and Parchment Light may use them; High Contrast remains a solid presentation.

The masters are local authoring inputs for application-local resources; the approved design adds no runtime generation or network dependency. Resource generation, derivative integrity, and packaged-size checks are pending final results.

## Measured performance work

Only bottlenecks identified by measurement are to be changed. The comparison uses five same-machine runs of the Desktop render BAT before and after the changes, with the existing coverage retained. Render-harness duration is a harness measurement, not end-to-end input latency in a running WPF session.

The pre-change baseline attempt recorded four of five runs passing and one synthetic focus-assertion flake. This is a baseline finding, not a passing five-run baseline. No baseline median or performance conclusion is recorded here; the final comparison remains pending.

## Validation status and limits

- The Desktop build and unit tests are pending final results and must run through repository `.bat` entry points.
- The WPF render/resource suite and retained route, language, theme, and scale coverage are pending final results.
- The five-run same-machine pre/post render-BAT comparison is pending; timing, medians, layout counts, and any change in harness performance will be added from the final report.
- Live WPF window observation and visual review are pending. Harness results alone do not establish live-window behavior.

No Bannerlord session or game test is part of this Desktop-only round, and no ZIP is to be regenerated.

This note records the approved scope; validation results remain pending until the final report is supplied.

## Subsequent High Contrast clarification — 2026-09-25

The user later clarified the High Contrast texture policy: show decorative textures when they remain legible. This updates the design direction recorded above without changing the solid-surface rule. High Contrast may display only separate, passive, low-opacity ornamental overlays where foreground contrast and control legibility are preserved; surface brushes and work areas remain solid, and the solid presentation is the fallback whenever legibility is uncertain. This is a design decision, not evidence that the overlays have been rendered or validated; implementation and render verification remain pending.

## Final validation results — 2026-09-25

- The Desktop build completed with zero warnings and zero errors. The Desktop suite passed **51/51** tests. The WPF render suite passed **273 cases** with **150 layout passes**, retaining the documented routes, languages, themes, and scale matrix.
- Five render-harness BAT runs measured **5,260, 5,680, 5,162, 5,338, and 5,205 ms**; the median was **5,260 ms**. This is **7.8% lower** than the historical Rev030 five-run median of **5,703 ms**. The fresh Rev032 reference is incomplete (four valid runs out of five; median **5,303.5 ms**), against which the current median is only **0.8% lower**. The valid fresh-reference phase comparison shows route-navigation and filter checks improving from **1,133.2 ms** to **810.3 ms** (**28.5% faster**); other phases were mixed, so this is not a uniform speedup. These values measure the test harness, not interaction latency in the running application.
- Decorative texture determinism checks passed. The prepared texture package measured **7,278,592 compressed bytes** and **15,778,992 decoded bytes**. The render harness verified the High Contrast legibility behavior; solid work surfaces remain in place.
- Live-window UI Automation and visual inspection remain pending. Harness rendering does not establish behavior or appearance in a live WPF session.

These results supersede the earlier pending test and harness-comparison statements in this append-only record. They do not change the approved scope, application version, or distribution status.

## Screenshot defect follow-up — 2026-09-25

- The title bar previously drew a second heraldic frame with a centered compass over the botanical strip. The redundant overlay is removed; the full-source botanical band remains passive and the render regression confirms it stays within the title columns, clear of minimize, maximize, and close controls.
- The tool-summary artwork is constrained to a dedicated 270-DIP right-hand column with uniform scaling, so it cannot overlap title or status text. Rail favorite buttons use a reserved 28-DIP column and a centered 26-DIP control; the render checks confirm each visible button and wrapped label remains inside its row.
- The final BAT validation for this follow-up built with zero warnings/errors, passed Desktop 51/51 and WPF render 273/273 with 150 layout passes. Two render harness runs measured 4,804 ms and 5,200 ms. These are harness durations only, not a before/after benchmark or live application latency measurements.
- High Contrast, War Table, and default harness screenshots were reviewed after the corrections. Live-window inspection remains pending; no Bannerlord session, game, campaign, or battle was started.
