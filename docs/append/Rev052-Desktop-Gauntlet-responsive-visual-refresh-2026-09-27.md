# Rev052 — Responsive Desktop and Gauntlet Visual Refresh

**Date:** 27 September 2026<br>
**Version:** Calradia Forge 25.2.0, unchanged<br>
**Scope:** Standalone Desktop WPF footer and local image resources; Gauntlet workbench header, navigation, Playbook/evidence layout, and structural visual checks.

## Confirmed defects and rationale

At the minimum WPF window size of 980×680 DIPs, the keyboard-shortcut footer could sit too close to the bottom edge. The Gauntlet audit also compared the current prefab against stale icon expectations and did not model how the evidence-focus state changes available workspace width. Several reported sprite overlaps were covered by later opaque panels, while real Playbook and evidence layout conditions needed explicit state-aware checks.

## Implementation

The WPF footer now has a reserved bottom inset and a minimum text height/margin. A render regression checks the footer's actual bounds and fitting across all 13 locales at the minimum window size. The packaged titlebar crop now uses a deterministic Rev072 derivative made from its retained high-resolution local master; only the optimized derivative is embedded.

The Gauntlet layout refines header spacing and binds workspace reservation to Playbook visibility. Evidence focus hides the Playbook and expands evidence while retaining the lower action edge. The eight route buttons use the semantic sprites already registered by the project. The tool catalogue and its 194 routes are unchanged, as are all 56 command bindings.

The visual auditor now evaluates 1220, 1280, 1600, and 1920 viewport widths, normal and detailed Playbook states, and focused evidence. Its overlap model permits a collision only when an opaque later-painted panel covers the affected decoration. The icon validator now matches the current eight navigation sprites and other canonical prefab icons; all 17 generated icon assets and all 30 atlas entries remain validated.

## Validation evidence and limits

- Five baseline and five after WPF render runs each passed 289 cases and 182 layout passes. Harness total medians were 10,953 ms before and 11,474 ms after (+4.8%). Layout-call medians were 3,220.1 ms before and 3,095.3 ms after (-3.9%). The measurements are from the off-screen test harness and do not represent open-app latency. The total median remains within the plan's 10% regression ceiling; no overall speed-up is claimed.
- `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check` passed deterministic comparison. The optimized package is 7,490,852 compressed bytes and 16,473,296 decoded RGBA bytes, within the configured resource budgets.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` built with zero warnings/errors and passed Assets 8/8, Core 338/338, ForgeWeave 73/73, Desktop 63/63, and WPF render 289 cases/182 layout passes.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` passed the decorative-sprite, layout/overlap, and Core gates. Read-only Desktop UIA passed 23/23 checks; it verifies the accessible tree, not visual approval.
- The Client and Modding Kit profiles both built with zero warnings/errors and were deployed using verified backups. The existing installed TPAC was preserved and hash-checked as `69513B5617F026E1F106F6CA6D47CF5C5CE0B5869FFA472E9166B037CA8EDEA6`; it was not replaced. Modding Kit startup reached an `RGL WARNING` dialog listing runtime assemblies. The dialog was left open without interaction, so live Gauntlet rendering and imported-resource appearance remain unverified. No campaign or battle was loaded.

Product version remains 25.2.0. No public API, IPC, routes, commands, permissions, or dependencies changed. ZIPs were not regenerated.
