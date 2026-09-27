# Rev045 — Clearer WPF evidence report surface

**Date:** 27 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Desktop WPF report tabs, evidence ledger presentation, and render regressions.

## Observed problem and technical rationale

The Evidence and Raw Result views had no clear visible tab hierarchy, making the selected report surface difficult to discover. Populated findings were presented as undifferentiated text, while the empty-ledger call to action was visually present but placed under a parent with hit testing disabled, so it could not receive pointer input.

## Technical solution and architectural decisions

- Added an explicit themed `TabControl` template and `TabItem` style for the report. The localized Evidence and Raw Result headers now include semantic icons, selected/hover/focus states, and a live evidence count.
- Reworked evidence rows as clean cards that separate source/status, rule ID, location, evidence text, and recommendation. No new view-model or command contract was introduced.
- Kept the empty-ledger illustration passive, but restored hit testing on its parent so the localized button can invoke the existing `OpenPaletteCommand`.
- Expanded WPF render assertions for localized tab labels, realistic populated evidence, CTA binding/hit testing, and bounds. The off-screen render harness is not a live-window review.

## Asset, code, and dependency changes

Updated `src/CalradiaForge.Desktop/App.xaml`, `src/CalradiaForge.Desktop/Resources/Views/ToolPageTemplates.xaml`, and `tests/CalradiaForge.Desktop.RenderTests/Program.cs`. No new texture, dependency, public API, route, command, permission, or packaged asset was added. The existing 25.2.0 ZIP was left unchanged.

## Validation and evidence limits

- Ran `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-visual-report-rev065-fix1.json"`.
- Build completed with zero warnings and errors; Desktop passed 59/59; WPF render/resource checks passed 281/281 with 158 layout passes.
- The JSON report recorded 7,856 ms overall and 1,705.8 ms in synchronous layout calls. These are harness measurements, not open-application latency.
- Render preview `artifacts/desktop-visual-report-rev065-fix1.png` was reviewed. No live application inspection was performed.

This annex extends the preceding protected revision without replacing earlier paragraphs or evidence.
