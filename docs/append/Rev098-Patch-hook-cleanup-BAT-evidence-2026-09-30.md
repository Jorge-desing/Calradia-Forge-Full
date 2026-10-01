# Rev098 — Patch and hook cleanup BAT evidence

**Date:** September 30, 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Core, ForgeWeave, and the disposable detour fixture; validation evidence only.

## Verified results

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause` completed successfully. The Core and ForgeWeave builds and test suites passed, with zero build warnings and zero build errors.
- `tests\CalradiaForge.DetourFixture\Run-DetourFixture.bat --no-pause` completed successfully. The fixture build reported zero warnings and zero errors, and the fixture passed through its BAT-hosted execution path. `DetourFixture.exe` was not launched directly.

This evidence-only addendum records the reported BAT results for the current patch and hook cleanup changes. It does not add results or claims beyond those commands.
