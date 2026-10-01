# Rev110 — Release Archive Guard and Evidence Reconciliation

**Date:** October 1, 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Release staging/auditing, hook-workbench evidence records, tests, and documentation.

## Why this revision was needed

The Rev109 integrity chain and append-only documents were valid, but an independent audit found that the previous Source-SDK ZIP could contain development scripts and an installed `node_modules` tree. The retained Rev109 render JSON also differed from its console summary by 3 ms, and the archived suite/utility logs predated the last test additions. The historical Rev109 record remains unchanged; this revision records fresh evidence and closes the distribution-policy gap.

## Changes and verification

- `tools/package.ps1` now removes `.bat` and `.ps1` files from module and source staging and prunes `node_modules` at any depth. The only permitted packaged script is the exact `Desktop/Run-CalradiaForge-Desktop.bat` launcher.
- `tools/audit_package.py` rejects every other `.bat` or `.ps1` archive entry and any path containing a `node_modules` segment. `tests/CalradiaForge.AssetPipeline.Tests.py` has regressions for script rejection, the exact Desktop exception, nested dependency directories, and ZIP payload rejection.
- `cmd.exe /c "tests\\CalradiaForge.AssetPipeline.Tests.bat --no-pause <nul"` exited 0 with 20/20 tests.
- `cmd.exe /c "tools\\Test-CalradiaForge-HookUtility.bat"` exited 0. The retained output `artifacts/hook-utility-rev110.txt` records 29 argument cases and 22 transport cases. Token rejection is simulated by an isolated fixture; this does not verify Runtime expiry, session, screen, or menu state.
- `cmd.exe /c "tools\\Run-CalradiaForge-Tests.bat --skip-build --no-pause --render-output artifacts\\hook-rev110-render.json <nul"` exited 0: Core 404/404, ForgeWeave 73/73, Desktop 65/65, and WPF render 294 cases. The console summary reported 13,183 ms; the structured JSON recorded 13,187 ms. Both record 182 render/layout passes and 5,827.1 ms in layout calls. The distinct values are retained as separate readouts rather than silently choosing one. Harness timing is not interactive Desktop latency.

## Boundaries

The distribution staging filter and synthetic archive-audit tests passed; the canonical three-archive package gate must still be run against this revision before release outputs are accepted. No ZIP hash or archive-audit result is asserted by this annex. Live Bannerlord hook mutation remains unverified; no Apply/Verify/Revert action was issued, and no campaign or battle was opened. Serial fixtures do not establish safety while another thread executes a target during detour installation or removal. Product version remains 25.2.0 and `ForgeApi.Version` remains 13.

This revision appends evidence and does not modify Rev001–Rev109.
