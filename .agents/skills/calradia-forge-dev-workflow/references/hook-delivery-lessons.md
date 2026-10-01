# Explicit hooks and scoped delivery lessons

Use when maintaining runtime hooks, their WPF/Gauntlet workbench, serial benchmarks, or distribution in a shared dirty checkout. Grounded in commits `d8c0def` and `4541754`; inspect current implementation before applying these lessons.

## Backend boundaries

- SDK API 13 adds optional Finalizer metadata while preserving prior constructors. Probe `ForgeApi.Hooks` at runtime; the compile-time version constant is not a capability check. MonoMod 25.3.6 and `ILContext.Manipulator` belong to Core `net472`; keep SDK, Core `net8.0`, Desktop and IPC independent of MonoMod types.
- Registration is inert. Read-only preflight cannot run callbacks or IL manipulators. Finalizer preserves unchanged pending exceptions through `ExceptionDispatchInfo`, can explicitly replace/suppress them, validates a suppressed return value, and aggregates a throwing Finalizer with an existing failure. Postfix runs after successful earlier phases. Retain the older fallback policy when no Finalizer exists.
- Initial Apply/Revert management uses the host main-menu/game-thread gate. Verified IL application authorizes reconstruction of that exact retained activation during external chain rebuilds, even on another caller thread or after callback shutdown. Exact ownership and absence of uncertain Undo remain required. Reconstruction does not authorize new management or gameplay callbacks. Manipulators must use supplied IL and stable configuration, tolerate repeated execution, and avoid live game state.
- A failed IL Undo may leave transformed code installed despite `IsApplied=false`. Retain the handle, conflict and destination reservation; require host restart for unresolved cleanup. Byte verification applies to raw detours; hook/ILHook state verification has different evidence limits.

Authoritative sources: `src/CalradiaForge.Core/ForgeHookService.cs`, `ForgeHookService.Transpiler.cs`, `src/CalradiaForge.Sdk/ForgeHookContracts.cs`, and `docs/PATCH_BLUEPRINTS.md` / `.es.md`.

## Workbench and console

- WPF sends selected registered IDs and single-use plan tokens. Gauntlet shares the runtime planner directly. Confirm on the host with current session, context, expiry and selected snapshots; never accept executable callbacks, arbitrary targets or IL over IPC/UI.
- Console Revert filters Applied, Conflict and Failed records before preparing a plan. Inactive records alone produce no mutation plan.
- Preserve complete selection when filtering the list; disclose hidden selected IDs in the confirmation preview. Bound long owner/target rows with wrapping and scrolling instead of clipping the confirmation surface. Keep localization parity.
- Use `tools/Test-CalradiaForge-HookUtility.bat` for argument/isolated transport coverage and `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat` for the disposable serial x64 fixture. Transport simulation does not validate the live Bannerlord host lifecycle.

## Measurement and live evidence

- The fixture BAT accepts `--benchmark --no-pause`. Measure direct and hooked calls, Apply/Revert, first-call and warm samples separately. Include Finalizer and IL-only scenarios; inspect timing units and allocation columns before reporting.
- Re-run measurements if relevant source changed. Historical ns/call, allocations and suite counts are observations rather than performance budgets. Serial fixtures never prove safe modification during concurrent target execution.
- Keep source tests, package integrity and live menu validation separate. The latest hook delivery did not observe a successful live main-menu mutation. Record unavailable native-window access or an unsuccessful F10 attempt as pending evidence, not approval.

## Shared-checkout delivery

- Capture baseline status and review file/hunk ownership. A correct build of a worktree containing concurrent work is not proof that the scoped commit builds. Materialize a reviewed index tree into a unique ignored snapshot, then validate and package there. Keep the original worktree intact.
- The canonical package script derives its workspace from its script location. Supply its local Python environment and licensed game paths without committing them; run tests through BAT. Do not reuse output from a different snapshot as evidence for the selected tree.
- Audit all three ZIPs and independently compare SHA-256 with that snapshot's emitted manifest. Inspect archive content for unintended concurrent additions; the generic safety audit does not identify task ownership. Record whether packaged source matches the delivered source, allowing explicit non-packaged bookkeeping differences.
- Append evidence without changing prior DOCX revisions or annexes. Discover the next ledger revision from current state; never assume a revision number in a shared checkout. A ledger pass proves its protected chain, not gameplay validation.
- Follow the Git rule for explicit push authorization. Commit the complete owned objective, push only when requested, and review remote branch SHA, Actions, checks and statuses for that SHA. Repair task-related failures under the existing authorization. Record green remote checks in the response without creating an evidence-only commit loop.
