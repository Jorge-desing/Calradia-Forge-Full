# Rev123 — SDK builder safety, shared guide parity, and local template validation

**Date:** 2026-10-02  
**Product version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Documentation, rules, SDK onboarding. Root AI-agent guidance and the SDK evolution evidence trail.

## Observed issue and technical rationale

A read-only review found that `AGENTS.md` and `CODEX.md` prohibited both `Campaign` and `Localization` names in the mod scope, while `GEMINI.md` prohibited only `Campaign`. The SDK evolution guide also described its evidence trail using only the initial Rev115–Rev116 entries, although follow-up work was appended through Rev122.

The earlier onboarding report verified portable template generation and package restoration, but did not compile the generated consumer against a local licensed Bannerlord `GameBin`. Source-level local-reference metadata therefore needed a stronger, separately stated check.

A targeted builder review also found that a sparse, very large equipment variation index grew a list across every intervening index, and a duplicate upgrade target could be rejected after the two-branch limit was reached.

## Technical correction and decisions

The three root guides now use the same anti-shadowing names, scope, rationale, and approved alternatives. The English and Spanish SDK evolution guides point to the initial evidence and subsequent Rev119–Rev123 entries. `ForgeTroopBuilder` now stores only declared nonnegative equipment variation keys in a sorted dictionary, retaining ascending output without allocating sparse gaps. It rejects negative variation indexes explicitly. It now returns successfully for an existing upgrade target before checking the two-distinct-target limit; a third distinct target remains rejected. The public method signatures, TFM, product version and XML order for normal variation indexes are unchanged.

The developer-onboarding BAT was run with the local Bannerlord root. It generated and restored the consumer template and compiled its `net472` module using the local GameBin references. The successful build reported zero warnings and zero errors. This is a local compile check only; it does not establish Visual Studio/Rider UI integration or in-game loading/rendering.

## Assets, code, and dependencies

Updated `GEMINI.md`, `CODEX.md`, `docs/SDK_EVOLUTION.md`, `docs/SDK_EVOLUTION.es.md`, the bilingual changelog, and `ForgeTroopBuilder` with focused regressions. No dependency, game binary, public API signature, or package manifest was changed.

## Validation and evidence limits

- `tools/Test-CalradiaForge-Developer-Onboarding.bat --game-path "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord" --no-pause` passed; generated module restore and local GameBin build succeeded with zero warnings/errors.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` passed after the builder fix: Core 411/411 and ForgeWeave 73/73, with net472/net8 builds at zero warnings/errors. Regression coverage includes `int.MaxValue` sparse indexing, ascending output, negative rejection, idempotent duplicates at the branch limit, and continued third-branch rejection.
- The complete BAT suite had already passed before this builder fix: Core 411/411, ForgeWeave 73/73, Desktop 65/65, WPF render 295/295, with zero build warnings/errors. The final package pipeline is rerun after this append.
- Python CI checks, skill validation, onboarding knowledge checks, source-package audit, and the canonical package/hash audit passed during this objective; current integrity/parity checks are rerun after this append.
- No Bannerlord or Modding Kit process was started for the local compile. No live rendering or IDE wizard behavior is claimed.

This appendix extends the prior protected record without rewriting earlier revisions. It retains the product version 25.2.0 and all public SDK contracts.
