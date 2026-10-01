# Rev071 — Explicit Forge patch documentation and validation follow-up

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Gauntlet copy, architecture maps, 13 native language catalogs, BAT validation.

## Documentation and localization corrections

- The Gauntlet Panel distinguishes the read-only Harmony Atlas from Patch Blueprint Preflight and Forge's explicit replacement backend. Preflight structurally checks declared target and callback references against assemblies already loaded by the host; it does not load assemblies, invoke callback code, apply hooks, or prove that a native replacement can be installed.
- Architecture maps now match the public `ForgeDetour` surface: `Patch(MethodInfo original, MethodInfo replacement)`, `Unpatch(MethodInfo original)`, and `UnpatchAll()`. They no longer describe ForgeDetour as Harmony management or depict startup as an implicit patch scan.
- The language resource generator now includes the existing translated Gauntlet Page Blueprint title, description, and Page title strings, along with the corrected patch-engine UI copy. All 13 generated native catalogs contain 710 matching keys; the localization audit reports valid parity.

## Validation evidence

- `tools/Run-CalradiaForge-Tests.bat --no-pause` completed successfully. The full solution built with 0 warnings and 0 errors. Core reported 369/369; the serial x64 fixture returned `15 → 32 → 15` and verified exact restoration; ForgeWeave reported 73/73; Desktop reported 63/63; and the WPF renderer passed 292 cases.
- The fixture and managed tests run through BAT launchers. The fixture calls its target serially; these results do not prove safety if another thread can execute a target during a memory write. The detour backend remains experimental and has no concurrent-execution guarantee.
- No Bannerlord or Modding Kit session, campaign, or battle was started. No package ZIP was regenerated. Product version is 25.2.0 and `ForgeApi.Version` is 11.

This append extends the existing protected register. Earlier revisions remain unchanged.
