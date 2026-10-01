# Rev108 — Finalizer, Transpiler and guarded hook utilities

**Date:** 1 October 2026  
**Version:** Calradia Forge 25.2.0, unchanged; SDK compile-time API 13  
**Scope:** SDK, Core net472 backend, console/BAT utilities, WPF, Gauntlet and bilingual guidance.

## Observed problem and technical justification

Prefix/Postfix did not provide explicit exception handling or local IL authoring. Hook management needed consistent registered-ID selection, diagnostics and confirmation across existing interfaces. A failed MonoMod IL Undo can clear IsApplied before reconstruction succeeds, so that flag alone cannot certify restoration.

## Technical solution and architecture decisions

Finalizer receives the pending exception after earlier phases, preserves it through ExceptionDispatchInfo by default, and supports explicit replacement or suppression with return-type validation. A throwing Finalizer aggregates an existing failure. Hooks without Finalizer preserve the previous callback failure policy.

Transpiler uses pinned MonoMod.RuntimeDetour 25.3.6 ILHook through a Core-only net472 ILContext adapter. Registration/preflight remain inert; SDK/Desktop do not reference MonoMod. Installed IL remains active outside the menu until Undo. Management remains explicit and restricted to the approved game-thread main-menu context.

Failed IL Undo retains conflict, handle and target reservation and requires host restart. Shutdown blocks new applications and runtime callbacks; only synchronous owned Undo under the management lock, caller-thread scope and approved context can rebuild remaining manipulators. Runtime.Dispose always closes the pipe in finally; API-level recovery is not proof of pipe survival after module unloading.

## Changes in code, interfaces and dependencies

Immutable snapshots expose optional Finalizer/Transpiler metadata without removing existing constructors. Console supports owner/target/type inventory, verification and JSON export. Typed BAT requests have bounded IDs, payloads and deadlines and do not retry confirmations. WPF provides filters, selection verification and export; Gauntlet uses the local plan coordinator rather than IPC. Both preserve exact metadata/session matching, expiring single-use plans and explicit confirmation. New labels preserve parity across thirteen languages. Filtered-empty inventory now has a distinct WPF message.

## Validation and evidence limits

- tools/Run-CalradiaForge-Tests.bat --no-pause passed: Core 403, ForgeWeave 73, Desktop 65 and WPF 294; builds reported zero warnings/errors. Final render: 12,508 ms with 182 layout passes, not observed game latency.
- tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause passed structural/decorative checks and nested Core/serial fixture validation.
- tools/Test-CalradiaForge-HookUtility.bat passed 29 argument cases and fourteen isolated transport cases, including malformed versions, IDs, UTF-8, bounds and timeout.
- Serial x64 BAT-hosted fixtures cover Finalizer errors, valid/invalid suppression, replacement and aggregation; IL ordering, rebuild/coexistence, failed Apply, failed Undo reservation retention, clean unload, disconnect and raw collisions. No test EXE was launched directly and target calls were not concurrent with writes.
- Five benchmark BAT executions passed. Median of per-run warm medians: Prefix 242.1 ns/168.43 bytes, Postfix 189.8/136.31, both 256.8/168.43, Finalizer 156.0/136.31, IL-only 17.9/0.00. Allocation counters describe the serial AppDomain harness. Historical single-run figures are not a comparable performance baseline; no speedup or general zero-allocation claim is made.
- Client and Modding Kit profiles built and deployed with installed-file backups. The existing imported TPAC was backed up with verified SHA-256 and preserved unchanged. Steam reached the main menu; Computer Use observed F10 opening Forge 25.2.0 and navigation to Framework at 1920x1080. No campaign or battle was opened. The native console did not open with the tested chords, so owned-target live hook application/reversion and live Gauntlet hook controls remain pending. Bannerlord was closed and Computer Use reset afterward.
- Review documents Forge's integration and dependency boundaries; it is not an independent security audit or proof of safe mutation during concurrent target execution. Backend remains experimental.

This annex appends evidence without changing earlier protected revisions. Distribution is recorded separately after the canonical three-ZIP audit and SHA-256 gate; remote publication is not authorized.
