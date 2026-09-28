# Rev080 — Bounded Gauntlet Output Comparison

**Date:** 28 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Mod and localization resources. Gauntlet output presentation and bilingual UI guidance.

## Observed need and technical justification

The live-output filter can narrow the current display, but it cannot retain one result as a reference while a later run produces new output. The existing object-snapshot comparison serves a different workflow and must remain independent. Developers need to review two complete text outputs without replacing the original output, tool argument, command history or retained evidence.

## Technical solution and architectural decisions

Add an explicit panel-lifetime output baseline, a two-column comparison view and an explicit baseline-clear action. Preserve identical lines and align insertions and deletions into paired rows. The existing case-insensitive filter searches both original sides and keeps a pair if either side matches. Wrapping and pagination stay synchronized at 16 visual rows per page. An active comparison refreshes when new output arrives; area navigation preserves the baseline. Clearing the current output exits comparison view while retaining the baseline. Comparison is bounded to 256 Ki characters and 4,096 lines per side, with a maximum of 8,192 aligned rows, 500,000 search steps and 262,144 trace cells. Exceeding a configured input or work bound produces a localized unavailable state and no partial comparison; an oversized attempt to pin a new baseline does not change it.

## Asset, code and dependency changes

The Gauntlet panel adds localized actions and baseline/current headings. Source and generated game-language resources, their regeneration key list, and the English/Spanish UI design guide describe the output-only workflow. The existing object-snapshot commands and contracts remain separate. No external runtime dependency or product-version change is introduced.

## Validation and evidence limits

- Source/catalog parity, visual structure and runtime tests are pending; this annex does not claim those checks passed.
- No live Bannerlord inspection or in-game comparison has been performed as part of this documentation update.
- The feature does not alter the raw output, tool argument, command history, clipboard/export payloads or retained evidence.

This annex records the bounded output-comparison behavior without replacing earlier revisions.
