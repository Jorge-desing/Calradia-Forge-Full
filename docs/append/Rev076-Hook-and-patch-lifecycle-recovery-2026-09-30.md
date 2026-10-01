# Rev076 — Hook and patch lifecycle recovery

**Date:** 30 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** SDK connection lifecycle, Core hook and patch services, serial detour fixture, and documentation.

## Observed problem and technical justification

The context guard was checked only once at hook-dispatch entry, so a synchronous Prefix or target transition could leave later callback stages running outside the approved main-menu context. Handle disposal also discarded unsuccessful revert results. SDK teardown could unpublish a patch service with unresolved conflicts, and a failed incoming lifecycle reconnect could leave the previous host published but closed.

## Technical solution and architectural decisions

Hook dispatch now rechecks the host gate after Prefix and before Postfix. If the gate closes after Prefix, Forge invokes the original target with the original arguments; if it closes while the target runs, Forge returns the target result without running Postfix. These checks do not provide thread quiescence or concurrent-write safety.

Hook and patch handle disposal now throws when it cannot confirm a revert; callers needing structured diagnostics can call `Revert()` directly. Patch conflicts remain recoverable only after a caller restores bytes to the exact Forge-installed or original image. `ForgeApi.Disconnect()` keeps `Patches` published while unresolved patch records remain. If the incoming host lifecycle fails after a clean prior-host teardown, Forge attempts to reopen the prior lifecycle and direct applications; rollback failure is surfaced as an aggregate exception.

## Changes to assets, code, and dependencies

- Added fail-closed gate checks between Prefix, the original target, and Postfix.
- Made hook/patch handle disposal surface unsuccessful reversion.
- Preserved the optional patch capability through unresolved disconnects and made exact-byte Conflict recovery possible without overwriting foreign bytes.
- Added rollback for incoming host lifecycle reconnect failure and regressions for all of the above.
- Updated the English and Spanish Patch Blueprint guide, lifecycle skills, changelogs, and this append-only record. Product version remains 25.2.0 and `ForgeApi.Version` remains 12. No ZIPs were rebuilt.

## Validation and evidence limits

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause`: Core 393/393 and ForgeWeave 73/73 passed; build reported zero warnings and zero errors. The nested `Run-DetourFixture.bat` passed serial x64 Prefix/Postfix, context-transition, visible-disposal-failure, patch restoration, and lifecycle checks. No fixture executable was launched directly.
- `tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause`: Desktop 65/65 and WPF render/resource checks 292/292 passed with 182 layout/render passes.
- `git diff --check` passed. No live Bannerlord or Modding Kit session was used. Serial fixtures do not prove safety while another thread executes the target; the backend remains experimental.

This annex extends the protected improvement record without replacing earlier revisions.
