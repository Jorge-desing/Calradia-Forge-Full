# Rev044 — WPF high-contrast rail illustration refinement

**Date:** 27 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Desktop WPF rail artwork and High Contrast rendering checks.

## Observed problem and technical rationale

The Rev063 transparent portrait frame fit the navigation rail, but its dark edge illustration was difficult to distinguish on the High Contrast rail surface. Visibility state alone did not prove that the art was perceptible or that it preserved text contrast.

## Technical solution and architectural decisions

- Created a brighter transparent brass/verdigris engraving as `workbench-heraldic-rail-portrait-rev064.png`, retaining the 887×1774 master aspect and 320×640 optimized presentation size.
- Replaced Rev063 in the Desktop resource manifest and rail template. The Rev063 derivative is retained as a historical/retired variant and is no longer embedded.
- Preserved the existing theme opacity values (War Table 0.18, Parchment Light 0.12, High Contrast 0.24), solid High Contrast surfaces, local resource loading, passive hit testing, and decorative-accent switch.
- Added a render assertion that alpha-composites each visible rail-art pixel over the High Contrast navigation surface. It requires decoration-to-surface contrast of at least 1.5:1 while paper and muted text remain at least 4.5:1.

## Asset, code, and dependency changes

The optimized PNG is 147,309 bytes with SHA-256 `06D98F717E5E38F597D965E266F1D6B59208C9AD662C2ED91E4F20A6E7AF578C`. The packaged PNG inventory is 7,516,909 compressed bytes and 16,473,296 decoded RGBA bytes, within configured limits. No dependencies, public contracts, routes, commands, permissions, product version, or ZIPs changed.

## Validation and evidence limits

- `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check` passed deterministic checks for dimensions, output bytes, and hashes.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts/desktop-visual-rev064-contrast.json` built with zero warnings and errors, passed Desktop 59/59, and passed 281 WPF render cases with 158 layout passes.
- The report recorded 7,802 ms overall and 1,738.8 ms in layout calls. These are render-harness timings, not Desktop interaction latency. High Contrast, Parchment, and minimum-window screenshots were inspected; live application and actual Windows system-DPI appearance remain unverified.
- No ZIP was regenerated and Bannerlord was not launched.

This annex extends the preceding protected revision without replacing prior paragraphs or evidence.
