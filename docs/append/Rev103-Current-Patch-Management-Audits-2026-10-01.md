# Rev103 — Current patch management audits

**Version:** Calradia Forge 25.2.0; unchanged.

Remote CI on 97e238c passed portable suites and the stateless gate, then failed the optional agent audit because it searched for obsolete _syncLock and direct pointer validation in ForgePatcher. The audit now checks syncLock receipt management, delegation to ForgeDetour.IsTrackedReceipt and ForgeDetour.Verify, Gate-protected tracked records and null installation-pointer rejection. Its report explicitly limits evidence to static management checks, without claiming concurrent-target execution safety.

Five SDK-independent regressions cover the current source and removal of receipt synchronization, integrity delegation, registry synchronization and null-pointer guards. They run from the Python BAT alongside existing suites. The agent stateless tool also uses the BAT gateway and selects the portable build graph on GitHub Actions instead of requiring proprietary engine assemblies.

Local Python CI BAT passed Ruff, 12 asset fixtures, 5 image tests, 5 tool-audit regressions and decorative sprite/icon checks. Remote optional SDK tests remain pending for this correction. The supplied logs ZIP contains the previous inventory encoding failure, already resolved by 97e238c. No application behavior, public API, game runtime, package or product version changed; previous records remain intact.
