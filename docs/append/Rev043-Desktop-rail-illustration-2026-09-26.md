# Rev043 — WPF portrait rail illustration

**Date:** 26 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Desktop WPF navigation-rail artwork and per-theme presentation.

## Observed problem and technical rationale

The former rail decoration was a wide landscape image that did not fit the tall navigation surface and was not used by the current rail template. The navigation rail needed a portrait composition that could sit behind its controls without taking hit tests or competing with text.

## Technical solution and architectural decisions

- Added a locally authored transparent portrait field-journal frame, optimized from its 887×1774 master to a 320×640 RGBA WPF resource.
- Replaced the obsolete landscape resource in the project manifest and rail template. The historical source remains in the texture source directory; the preparation manifest identifies its old optimized derivative as retired.
- Kept `Stretch=Uniform`, high-quality bitmap sampling, the existing decorative-accent visibility binding, and passive input/focus settings.
- Added theme opacity tokens: War Table 0.18, Parchment Light 0.12, and High Contrast 0.24. High Contrast surface brushes remain solid.

## Asset, code, and dependency changes

The optimized PNG is 126,258 bytes with SHA-256 `DF8C90E403A3B4D5B8476824B72FEFF006C83023165A4924A4CE084EFF362638`. The project resource inventory is 7,495,858 compressed bytes and 16,473,296 decoded RGBA bytes, within the configured limits. No dependency, route, public API, command, permission, product version, or ZIP changed.

## Validation and evidence limits

- `tools/Prepare-CalradiaForge-Desktop-Textures.bat` and `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check` passed, including deterministic output validation.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts/desktop-visual-rev063-opacity/render.json` built with zero warnings and errors, passed Desktop 59/59, and passed 281 render cases with 158 layout passes.
- The render harness reported 7,998 ms total and 1,779.8 ms in layout calls. These are harness measurements, not application-interaction latency. Render screenshots were inspected, but this is not a live application review or verification under actual Windows system DPI.
- No ZIP was regenerated and no game was launched.

This annex extends the preceding protected revision without replacing prior paragraphs or evidence.
