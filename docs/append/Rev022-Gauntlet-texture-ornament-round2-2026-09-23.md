# Rev022 — Gauntlet decorative texture refinement

**Date:** 2026-09-23  
**Release line:** Calradia Forge 22.0.0  
**Scope:** Original decorative Gauntlet sprite sources and their bounded prefab placements.  
**Distribution:** Source follow-up only; existing ZIPs were not changed or regenerated.

## Decorative texture sources and placement

The second visual round adds three original, reproducibly generated sprite sources: `forge_table_grain` (128 × 64), a subtle event-transparent overlay for header/rail chrome; `forge_map_contours` (128 × 64), restricted to the header ornament zone behind the existing filigree; and `forge_brass_rule` (128 × 16), used as a divider between real sections. Their SpriteData and prefab references are part of the source update. The command deck, editable input, and evidence ledger remain on clean surfaces so texture detail does not compete with actions or technical text.

These project-generated resources add no runtime dependency or external artwork. The existing Game-icons resources and their attributions are unchanged. Decorative placements remain passive and bounded; they do not overlay hit targets or alter navigation, commands, evidence, paging, or game-state protections.

## Validation and release status

The structural validation is pending execution for this revision. The source changes do not establish that the generated atlas or runtime texture package contains these additions. The existing TPAC is stale relative to the source sprite update; Resource Browser import and a live in-game render remain pending. No runtime visual result is claimed. The product remains 22.0.0, and no ZIPs were regenerated.

This record is append-only and does not revise Rev021 or earlier evidence.

## Supplemental append-only validation update — 2026-09-23

The integrated Gauntlet visual-check BAT completed successfully: the Gauntlet auditor reported 0 errors and 0 warnings, and Core passed 232/232 assertions. These are static/source-level checks. Atlas pixel-crop validation, runtime TPAC import, and live in-game rendering remain pending; no runtime visual result is claimed.

## Supplemental append-only atlas and Editor observation — 2026-09-23

The source sprite atlas was generated and synchronized. The decorative-sprite validator confirmed all three source-to-atlas pixel crops. During the brief Editor inspection, Resource Browser showed only `Native` and `SandBox`; the Editor process had been opened without explicit module arguments, so the Calradia Forge module was not available in that view. Filtered UI Automation timed out. No TPAC was imported, and no live in-game render was observed or verified.
