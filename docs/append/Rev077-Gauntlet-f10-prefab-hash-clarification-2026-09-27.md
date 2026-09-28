# Rev077 — F10 prefab hash chronology and instrumentation boundary

**Date:** 27 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Append-only clarification of Rev076's prefab comparison and the limits of F10 evidence.

## Hash chronology

Rev076 recorded a pre-regeneration comparison in which the source and installed `GUI/Prefabs/CalradiaForge.xml` both had SHA-256 `5741D7571C254B6487477D601F0CFE016AA254A1DFC6BD6795766274543B2F45`. After the heraldic visual renewal regenerated the source prefab, the source hash became `C7BDE061D57F142797DF7B046BC93437E53049351A9D1C1821F6326CDC3BAC31`; the installed prefab remains at `5741D7571C254B6487477D601F0CFE016AA254A1DFC6BD6795766274543B2F45` because the new resources were not imported or deployed. The current difference identifies source and installed revisions that are not in sync; it does not by itself indicate file corruption.

Both current source and installed prefabs use the engine-canonical `<ScrollbarWidget>` spelling and do not use `<ScrollBarWidget>`. This observation does not identify or rule out the exact cause of the historical F10 assertion, and it does not establish that the crash has been resolved.

## F10 lifecycle instrumentation

`SubModule.Open()` now records the ordered milestones `Loading panel brush file.`, `Panel brush file loaded.`, `Loading panel movie.`, and `Panel movie loaded.`. The static regression in `AdvancedToolsTests` checks that these milestones, layer attachment, and focus operations occur in the expected source order. Although the strings are emitted by the runtime code, this static source-order assertion is not a runtime telemetry capture and does not reproduce the historical assertion.

No new in-game run, F10 crash reproduction, TPAC import, or live render inspection is claimed here. The historical cause and resolution remain unverified; Resource Browser import and live rendering remain pending. This addendum clarifies chronology and evidence limits without rewriting Rev076 or Rev056.
