# Rev024 — Gauntlet material-texture replacement

**Date:** 2026-09-24  
**Release line:** Calradia Forge 22.0.0  
**Scope:** Native Gauntlet decorative textures and their reproducible sprite processing.  
**Distribution:** Source follow-up only; no ZIP or TPAC changes.

## ImageGen material masters

This round replaces the geometric decoration set with four ImageGen-created material masters: pine wood, ink wash, aged brass, and pine felt. Local deterministic processing converts the approved masters to atlas-ready sprites with fixed dimensions, transparency, and color treatment. The geometric map-contour, rosette, stitched, and other line-art motifs are retired from the visual design; the result uses material texture rather than repeated geometric marks. ImageGen is used during authoring only. The game has no runtime image-generation or network dependency.

The textures are reserved for interface chrome: the header, navigation rail, outer framing, and real section dividers. Evidence text and technical output, editable fields, and command controls keep clean, high-contrast surfaces. Decorative widgets remain passive, preserve control hit targets, and do not obscure keyboard focus. The eight routes, commands, labels, permissions, public API, and behavior remain unchanged.

## Validation and release status

Texture-generation and sprite-reference checks, atlas regeneration, Resource Browser import, and live in-game rendering are pending confirmation. No test, atlas, TPAC, or runtime rendering result is claimed by this entry. The product remains 22.0.0; no TPAC was replaced and no ZIPs were generated.

This record is append-only and does not revise Rev021–Rev023 or earlier evidence.
