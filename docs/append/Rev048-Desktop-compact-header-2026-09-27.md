# Rev048 — Compact Desktop header and evidence summary

**Date:** 27 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Standalone Desktop WPF header, context dossier toggle, evidence-summary card, and off-screen render assertions.

## Observed issue and technical rationale

At the 980×680-DIP minimum, the header's wide labels and secondary actions crowded its single-row layout. The dossier label and evidence-export text also consumed width needed by settings and the retained-evidence summary.

## Technical solution and decisions

The visible decorative-accent label now uses the compact localized `Ui.DecorativeAccentsShort` key while preserving the complete accessible name and tooltip. The dossier toggle presents a local book glyph instead of a visible text label while retaining its existing binding, toggle behavior, automation ID, accessible name, and tooltip. The evidence export action presents a passive local icon in a compact 36×30-DIP button and retains its existing command and accessibility contract. The reclaimed width keeps the evidence summary readable in the compact status card.

The render harness checks minimum-window header placement, evidence-summary/export dimensions, and dossier/decorative-toggle accessibility. All 13 locale catalogs provide the short decorative label. No route, command, public API, IPC contract, product version, or ZIP changed.

## Assets, code, and dependencies

- Updated the existing WPF header/status presentation and render assertions; added the short label to the 13 localized resource dictionaries.
- No image assets, runtime dependencies, or new application capabilities were added.
- Product source remains 25.2.0; no package ZIP was regenerated or modified.

## Validation and evidence limits

- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-visual-rev068-final.json"` passed with zero build warnings/errors, Desktop 59/59, and WPF render 285/285 with 158 layout passes.
- The final single-run render artifact recorded 13,135 ms total and 3,072.1 ms in layout calls. The same-session single-run baseline recorded 9,120 ms and 2,033.3 ms. These are harness timings, not application-interaction latency; the final run was slower and no performance improvement is claimed.
- The render harness uses an off-screen WPF host. Live-window behavior, Windows system-DPI layout, and operating-system pointer input remain unverified. Bannerlord, campaigns, and battles were not started.

This appendix adds evidence to the previous revision without replacing its paragraphs. Source changelog Rev068 and protected Register appendix Rev048 are independent counters.
