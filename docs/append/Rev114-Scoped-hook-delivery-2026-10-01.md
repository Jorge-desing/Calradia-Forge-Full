# Rev114 — Scoped hook delivery validation — 2026-10-01

The Finalizer/ILHook objective was validated from the staged Git tree in an isolated local snapshot, excluding concurrent SDK/onboarding changes. The snapshot build passed with zero warnings and errors. BAT validation passed Core 405/405, ForgeWeave 73/73, Desktop 65/65, 294 WPF cases with 182 layout passes, Asset Pipeline 20/20, stateless acceptance 4/4 and Gauntlet structural checks. Hook Utility passed 29 argument checks and 22 isolated transport cases. The WPF harness took 14,026 ms; this is not application interaction latency.

A fresh serial BAT benchmark included five samples per scenario: Finalizer median 183.0 ns/call and 136.31 B/call; IL-only median 17.8 ns/call and zero measured allocated bytes. These fixture measurements do not certify concurrent modification safety or live game behavior.

The canonical packaging pipeline generated and audited all three 25.2.0 archives from that scoped snapshot. Independent SHA-256 verification matched its manifest, and the Source-SDK archive excluded concurrent ContentShowcase, HarmonyDiagnostics test-project and SDK evolution additions. Final procedural guides distinguish initial gated application from reconstruction of an already verified owned IL activation; uncertain Undo never authorizes reconstruction.

Live main-menu hook application remains unverified because native-window control is unavailable in the current Computer Use API. No campaign, battle or TaleWorlds target was used. Product version remains 25.2.0, SDK API 13 and MonoMod 25.3.6. Delivery requires a scoped local commit without push; unrelated work remains unstaged.
