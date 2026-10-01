# Rev113 — Deterministic Harmony Patch Inventory

**Date:** October 1, 2026  
**Version:** Calradia Forge 25.2.0, unchanged; ForgeApi.Version 13  
**Scope:** Read-only Core Harmony compatibility diagnostics, regression fixture, packaging validation.

## Observed problem and technical rationale

The canonical release gate exposed a failing assertion in the read-only Harmony patch inventory. The diagnostic preserved the runtime's arbitrary owner enumeration order even though its regression expected a stable snapshot. Inspection also showed that the property-name rule used for patch kinds formed invalid names for `Transpiler` and `Finalizer`, so those kinds could be omitted from a compatible Harmony runtime's inventory.

## Technical solution and architectural decisions

Owner names are now deduplicated case-insensitively and sorted ordinally before returning a snapshot. Patch-kind metadata uses explicit property names for `Prefixes`, `Postfixes`, `Transpilers`, and `Finalizers`. The diagnostic remains reflection-based, read-only, bounded, and optional; it does not inspect raw detours or certify that another patching backend is absent.

The regression fixture now provides all four patch kinds and intentionally returns owners in reverse lexical order. It verifies deterministic owner output, patch-kind ordering, and each patch method name.

## Source and dependency scope

Changes are limited to `HarmonyDiagnostics` and its Core regression fixture. There is no new API, runtime dependency, mutation path, SDK version, or product version change. Rev001–Rev112 remain unchanged.

## Validation and evidence limits

- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` passed: Core 405/405 and ForgeWeave 73/73; selected Release builds had 0 warnings and 0 errors. The isolated x64 detour fixture also passed through its BAT launcher.
- The canonical `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\package.ps1` completed successfully after the correction. The package runner passed its selected suites, including Desktop 65/65 and WPF 294 render cases; the render harness reported 13,978 ms and 182 layout passes. This is harness timing, not interactive app latency.
- DocFX build succeeded with 0 warnings and 0 errors. Package audit verified all three archives; independent SHA-256 comparison matched the generated manifest.
- The TpacTool deep parser was unavailable and skipped; the package pipeline performed its bounded structural TPAC check. No Resource Browser import or live Bannerlord rendering is claimed. No campaign or battle was opened.

This revision appends evidence to Rev112 without replacing historical paragraphs or changing the product version.
