# Rev041 — Desktop WPF visual polish and hierarchy

**Date:** 26 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Desktop WPF. Parchment texture treatment, rail search and selection, cartographic-board framing, and the empty evidence ledger state.

## Observed problem and technical rationale

Parchment textures were intense enough to compete with text and controls. The active rail route needed an observable selected state, the search field needed a localized hint, the cartographic frame needed predictable reserved space, and the empty evidence ledger needed a clearer visual hierarchy.

## Technical solution and architectural decisions

- Reduced surface/title texture opacity from 0.34/0.32 to 0.11/0.12.
- Made the active navigation route's selected state observable and exposed the localized `Ui.SearchHint` hint in rail search.
- Reserved a 168-DIP cartographic-board column for a 160×90-DIP frame with an 8-DIP margin.
- Enlarged empty-evidence artwork to 42 pixels and its action button to 32 DIP.
- Kept the work presentation-only; no public contracts, IPC, commands, or permissions were intentionally changed.

## Asset, code, and dependency changes

The presentation of existing local WPF resources was adjusted. No dependencies were added and no ZIPs were regenerated. Product version remains 25.2.0.

## Validation and evidence limits

- The latest `tools/Run-CalradiaForge-Tests.bat` Desktop run built with zero warnings/errors, passed Desktop 56/56, and completed 277 WPF render cases with 161 layout passes.
- The harness recorded 7,808 ms overall and 1,824.9 ms in layout calls. These are harness measurements, not latency measurements from the open Desktop application.
- `tools/Test-CalradiaForge-Desktop-Uia.bat` passed 20/20 bounded, read-only checks in `artifacts/desktop-uia-rev059.json`; it inspected the accessibility tree only.
- Live visual approval is not claimed; render-harness results are not a manual inspection of the running application.
- No product version, public contract, or ZIP changed.

This annex extends the preceding protected revision without replacing any existing paragraphs or evidence.
