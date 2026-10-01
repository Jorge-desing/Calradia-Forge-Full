# Rev104 — Hook backend review and completion audit

**Date:** 1 October 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Review of SDK, Core, Mod, Desktop, runtime dependencies, and test evidence. No new executable hook kinds or distribution packages.

## Observed problem and technical justification

The requested audited detour backend needs a defined evidence boundary. The user accepted a documented code/dependency review rather than requiring a published independent security audit. This review is scoped to Forge's integration; it is not an exhaustive security audit of MonoMod or a certification of concurrent native execution.

The full master BAT passed, but a subsequent Core BAT run returned 400/401: the concurrent case-alias settings-save test reported an unsuccessful save. Its previous assertion discarded the typed outcome and underlying exception. The cause remains unconfirmed; passing later executions does not resolve that observation. Eight further Core repetitions passed, then another run failed in the separate Atomic settings replacement test with an IOException from File.Replace: Windows could not remove the file being replaced. This evidence broadens the investigation to the storage boundary; it does not identify a responsible external process.

## Technical solution and architectural decisions

- `ForgeBootstrapper.InitializeGlobalPatches` remains an obsolete no-op. `SubModule.OnSubModuleLoad` does not scan or apply patches. Explicit `ForgePatcher.ApplyAll` resolves the supplied assembly before committing a batch; legacy adapters use shared target reservations and generation-aware receipts.
- `ForgeDetour` checks architecture, signatures, page boundaries, installed bytes, protection changes, and instruction-cache flushing. Reversion refuses foreign bytes. Simulated Win32 failures and serial disposable native fixtures cover recovery without executing a target concurrently with code writes.
- `ForgeHookService.Register` is inert. Prefix/Postfix use pinned MonoMod.RuntimeDetour 25.3.6 only under `NETFRAMEWORK`; Core net8.0 and Desktop do not install this backend. Finalizer and Transpiler remain unsupported executable kinds.
- The integration retains the exact original invoker during dispatch, defers self-removal until that serial invocation finishes, retains uncertain handles and target reservations, and makes retained callbacks inert during unload. Shared reservations prevent mixing raw detours and managed hooks on a Forge-owned target.
- Optional SDK capabilities remain separate from registry implementers. `ForgeApi.Version` is 12 after the hook extension; availability is determined by `ForgeApi.Patches` and `ForgeApi.Hooks`, not the compile-time constant alone. Snapshots copy their state, owner names are labels, and handles preserve explicit verification/recovery.
- Runtime mutation requires the game thread, the exact official main-menu CLR type, and no campaign, mission, or multiplayer context. WPF selects registered IDs only. IPC plans are bounded to 32 IDs, expire after 60 seconds, bind to session/service/screen/context epoch, and consume the matching token before mutation checks. Unknown outcomes require reconciliation; bulk application is sequential and can be partial.
- The workbench uses an explicit checkbox and confirmation action; cancellation matches session and token. Console status/apply/revert and the fixture/benchmark BAT utilities cover the requested operator surfaces. The same-user pipe ACL is not proof of WPF process identity.

## Dependency review

`THIRD_PARTY_NOTICES.md` records pinned package versions, source commits, MIT notices, and the bundled Iced notice. The Core project conditions RuntimeDetour on net472; packaging uses an explicit runtime dependency allowlist. No dependency version was changed during this review.

The upstream RuntimeDetour usage guide describes chain ordering, original delegates, and disposal. Its synchronization claims are limited to that chain and do not establish arbitrary host-lifecycle safety. On this date, GitHub's upstream security pages displayed no SECURITY.md policy and no published advisories. Neither observation establishes absence of vulnerabilities or an independent audit. Sources: https://monomod.dev/docs/RuntimeDetour/Usage.html ; https://github.com/MonoMod/MonoMod/security/policy ; https://github.com/MonoMod/MonoMod/security/advisories .

## Changes to code and records

The concurrent settings-save assertion now includes the failed save index, typed state, bounded failure reason, and internal exception as its cause, including HRESULT. The atomic JSON replacement assertion also records HRESULT and the retained language when an IOException occurs. It retains the requirement that every scheduled alias save succeed and that the final cache match the committed file. No retry, exception suppression, production storage change, or reduced coverage was added.

This English annex and its Spanish counterpart document the review and its outstanding observation. Previous ledger revisions are preserved. No Bannerlord process, Modding Kit, campaign, or battle was started; no ZIP was regenerated.

## Validation and evidence limits

- `tools/Run-CalradiaForge-Tests.bat --no-pause`: clean solution build with zero warnings/errors; all selected suites passed, including Desktop 65/65 and WPF render 293 cases with 182 layout passes. The render harness reported 14,643 ms, which is not application latency.
- `tools/Verify-CalradiaForge-StatelessBehavior.bat`: 4/4 architectural gates passed.
- `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat --no-pause`: Prefix/Postfix, ordering, serial self-removal, void cancellation, typed invoker, context changes, unload/disconnect recovery, uncertain status, backend exclusion, and exact raw restoration passed. A library was hosted through BAT; no DetourFixture.exe was launched.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` after the diagnostic change: Core 401/401 and ForgeWeave 73/73 passed. Additional Core repetitions are diagnostic evidence, not a cure for the earlier intermittent save failure.
- `tools/Benchmark-CalradiaForge-Hooks.bat`: five samples per scenario. Warm no-op hook medians were 230.3 ns/call (Prefix), 192.3 ns/call (Postfix), and 252.6 ns/call (both); approximate AppDomain allocation counters reported 160.56, 128.45, and 160.56 bytes/call. These are serial microbenchmarks, not game-frame latency or zero-allocation claims.
- The settings-save failure remains unresolved until the diagnostic captures a reproducible cause. This annex does not claim that the full objective is complete. Hosted CI checks must be reviewed for the exact SHA after this change is pushed.

This revision adds review evidence without replacing earlier paragraphs or claiming live game verification.
