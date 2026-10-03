# Rev128 — Validation evidence reconciliation and showcase contracts

**Date:** 2026-10-02  
**Product version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Static showcase verification, validation evidence, SDK evolution documentation, and append-only records.

## Finding

Rev127 reports a final integrated run with Core 415/415, Patch Diagnostics 30/30, 320 WPF layout/render passes, and 20,903 ms. The checkout's latest retained integrated log, `artifacts/sdk-evolution/post-final-reviewed-20261002.log`, records a passing run with Core 410/410, Patch Diagnostics 28/28, ForgeWeave 73/73, Desktop 65/65, and 295 WPF render cases with 308 layout/render passes. Its render duration is 24,476 ms. No retained log was found that reproduces Rev127's exact totals and duration.

## Correction and evidence boundary

This discrepancy is a retention and independent-reproducibility gap; it does not establish that the later reported run failed or did not occur. Rev127 is preserved unchanged. This revision identifies the retained log as the independently inspectable result and labels the higher Rev127 totals as reported but not independently reproduced from this checkout. The `ForgeTimeSlicer.ProcessBatch` benchmark remains a synthetic harness measurement; it does not measure a complete campaign callback or performance inside Bannerlord.

The content-showcase generator now verifies that EN/SP `language_data.xml` entries use the expected `xml_path`, that each path stays inside its locale directory and resolves to an existing file, and that the close button is focusable with its text bound to `@CloseLabel`. Negative checks cover malformed locale paths, missing files, focus state, and localized label binding. The retained showcase BAT result validates these source contracts, generated content, schemas, deterministic output, and a generated module build; it does not prove live Gauntlet rendering.

The retained integrated log reports successful local suites and clean builds. No in-game runtime or render result is inferred from those checks.

## Validation

- Inspected `artifacts/sdk-evolution/post-final-reviewed-20261002.log` and confirmed its suite, render-case, layout-pass, and duration summaries.
- Ran `tools\Test-CalradiaForge-ContentShowcase.bat --no-pause` after adding the static negative regressions; see the captured log in `artifacts/sdk-evolution/showcase-rev128.log`.
- Preserved Rev127 and appended this clarification as Rev128 without rewriting earlier appendices.
- Verify the document hash chain with `tools\Append-CalradiaForge-Improvement-Record.bat --verify` after compiling the protected record.
