# Rev124 — Verified knowledge and showcase contracts

**Date:** 2026-10-02  
**Product version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Static content showcase verification, engineering guidance, and SDK evolution documentation.

## Evidence and correction

A source and skill review found that the static content showcase checked localization catalog parity but did not prove that each generated prefab binding had a matching Gauntlet data-source property or that every referenced UI/content key existed in both catalogs. A separate review found unmeasured workload claims in a legacy feature summary and agent handoff: a specific hero count and frame-drop assertion, plus an exact uniform-bucket claim for time slicing.

The showcase generator now scans prefab `@Property` attributes for matching `[DataSourceProperty]` members and scans generated ViewModel and native content XML for referenced localization keys in both English and Spanish. Negative contract fixtures demonstrate rejection of a missing binding annotation and missing keys in either locale. The stale time-slicing recommendations now describe its actual source behavior: deterministic ID hashing, uneven buckets, and full collection traversal by callers. Unsupported workload statements are explicitly identified as unverified.

The debugging and simulation skills distinguish engine-thread affinity from blanket memory-corruption claims, mark illustrative simulation examples as proposals, and require local assembly/source verification before runtime claims. The debugging guide now describes `.cfcrash` as JSON text written under the module folder with only `Timestamp`, `IsTerminating`, and `Exception`; it does not infer provenance or thread affinity from that file. It also keeps the mod's `SyncData` overrides non-persistent and assigns the 64-entry journal/replay bounds to ForgeWeave, separately from the 2,048-agent identity cap. The performance guide scopes `GameThreadActionDispatch.RunOrPost` to its internal call sites rather than promising global dispatch. Root guides now route to all six requested specialist skills. No behavior-tree runtime or universal simulation subsystem is claimed.

## Validation and limits

- `tools\Test-CalradiaForge-ContentShowcase.bat --no-pause` passed after adding the verifier regressions. The generated consumer module built for `net472` without warnings; Core passed 411/411 and ForgeWeave passed 73/73.
- `tools\Validate-CalradiaForge-Skills.bat` passed for the updated debugging and discrete-event-simulation skills.
- `git diff --check` passed. Full repository test, canonical package/hash audit, and protected-record integrity are rerun after this append.
- These checks cover generated source and fixtures. They do not prove Gauntlet loading/rendering, Bannerlord performance, or an in-game frame-time improvement. Bannerlord and Modding Kit were not started.

This entry is append-only and retains product version 25.2.0 and the existing SDK contracts.
