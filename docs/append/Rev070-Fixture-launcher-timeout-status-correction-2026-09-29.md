# Rev070 — Fixture launcher timeout status correction

**Date:** 29 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Disposable x64 fixture launch and latest patch-engine test evidence.

## Correction to Rev069

Rev069 said the fixture BAT bounded its child-process wait. A PowerShell inline timeout wrapper was tested through the BAT, but it did not return control reliably while the native child remained blocked. The wrapper was removed and the BAT restored to direct child-process launch. No timeout guarantee is now claimed.

## Current evidence

- The latest managed Core BAT output passed 363/363 regressions; the isolated `net472` x64 fixture compiled with zero warnings and errors.
- The native fixture stalled after launch and was stopped by interrupting only its disposable BAT test run. Therefore the latest Core BAT as a whole and native smoke are not approved or reported as passing.
- Earlier serial fixture success remains evidence for the earlier tree recorded in Rev088. It does not establish behavior of the latest tree.
- The page-boundary preflight regressions continue to verify the no-read/no-write rejection path and registry cleanup through the managed fake adapter. No Bannerlord or Modding Kit runtime was started.

This append-only correction supersedes Rev069's timeout claim without changing that historical document. Product version remains 25.2.0, `ForgeApi.Version` remains 11, and distribution ZIPs are unchanged. The experimental detour backend still does not guarantee safety while another thread executes a target during a code write.
