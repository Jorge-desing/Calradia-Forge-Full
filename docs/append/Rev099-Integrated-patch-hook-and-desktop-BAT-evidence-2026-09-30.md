# Rev099 — Integrated patch, hook, and Desktop BAT evidence

**Date:** September 30, 2026  
**Scope:** Core, ForgeWeave, disposable detour fixture, and Desktop test/render harness; validation evidence only.

## Verified results

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause` exited 0. Core passed 397/397 and ForgeWeave passed 73/73. The selected `net472` and `net8.0` builds reported zero warnings and zero errors.
- The nested serial x64 detour fixture BAT passed, including unload-inert callbacks and exact `15 → 32 → 15` restoration. `DetourFixture.exe` was not launched directly.
- `tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause` passed the Desktop suite 65/65 and 292 render cases; the build reported zero warnings and zero errors.

This evidence-only addendum records these reported BAT results and makes no additional runtime or version claim.
