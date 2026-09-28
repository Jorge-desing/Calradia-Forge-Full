# Rev082 — Illustrated Gauntlet layout corrections and F10 source review

**Date:** 28 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Gauntlet panel, generated texture sources, layout auditing, and existing F10 lifecycle diagnostics.

## Observed need and technical justification

The 1280×720 viewport audit showed that the normal evidence ledger retained only 102 DIPs, leaving technical output unnecessarily compressed. The test-results explorer also allowed status and duration values to touch or overlap when a status was long. Eight decorative route-header sources were authored at 128×64 but rendered at 24×12, where most of their detail was lost. The historical F10 assertion already had prior source corrections; this revision rechecked the existing input-edge and layer telemetry rather than treating those changes as proof that the assertion was resolved.

## Technical solution and architectural decisions

The normal ledger origin was raised to reserve at least 160 DIPs for its body at the 1280×720 audit profile. The result explorer keeps status and duration in independently clipped cells with at least 6 DIPs of clearance, including localized header-width measurement. The eight undersized header ornaments were removed from active sprite generation, while their master, prepared, and SpriteParts copies were archived with a SHA-256 manifest. Existing semantic route icons and native Gauntlet focus indicators continue to communicate route state.

Three ImageGen masters were selected using the project's `high-quality-image-generation` art direction: dark cartographic cloth, a vertically composed heraldic rail textile, and a transparent heraldic header treatment. Their originals remain in `assets/gauntlet-imagegen/` and the deterministic local preparation workflow derives fixed-size, alpha-bounded Gauntlet sprites. WPF artwork is not reused. `SubModule.cs` was reviewed without changing it; the F10-only rising-edge fallback and `GauntletLayer` lifecycle telemetry remain the existing diagnostic path.

## Asset, code and dependency changes

| ImageGen master | Dimensions and mode | SHA-256 |
| --- | --- | --- |
| `forge_war_table_cloth_v3_master.png` | 2172×724 RGB | `32206FCD23A74379412DAC373CB26984553A6404CB9A732495D10700C2ADF552` |
| `forge_heraldic_rail_v4_master.png` | 887×1774 RGB | `9EA66B7F9F91828539D637EC2EE939E61BD7A8B66238CF7426C8420EE7F80FCF` |
| `forge_heraldic_header_v3_master.png` | 2048×768 RGBA, alpha 0–254 | `FA854C634D051EBD9D98909DD9F332963FD09FA78A628B69FFE6DFF78CAC0083` |

The product remains 25.2.0. The eight legacy ornaments are retained under `assets/gauntlet-imagegen/archive/2026-09-28/` with `SHA256SUMS.txt`; removing them from active generation does not delete historical art. This work changes no public API, route, command, permission, dependency, or ZIP.

## Validation and evidence limits

- `tools/Prepare-CalradiaForge-ImageGenTextures.bat --check --no-pause` passed the preparation determinism check.
- The reported `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` run passed Core 343/343 and ForgeWeave 73/73 with no build warnings. The existing F10 edge/layer lifecycle regression is source-level evidence only.
- The Gauntlet visual audit and source-atlas regeneration were still pending when this annex was drafted. Resource Browser import and live Gauntlet rendering remain pending; source checks do not establish runtime behavior.
- No live F10 observation, campaign, or battle was performed for this revision.

This annex adds evidence to the preceding revision without replacing earlier paragraphs.
