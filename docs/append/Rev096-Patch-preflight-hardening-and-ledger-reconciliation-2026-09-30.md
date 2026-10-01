# Rev096 — Patch preflight hardening and protected-ledger reconciliation

**Date:** 30 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Explicit patch and hook preflight, disposable detour fixture, validation scripts, and append-only documentation registry.

## Changes

- Hardened ForgeDetour preflight before any executable-memory access. It rejects P/Invoke, InternalCall, and varargs methods; checks implicit instance-receiver compatibility; compares required and optional custom modifiers on return and parameter signatures; and rejects targets reserved by the hook service.
- Added Core regressions for those invalid target/replacement combinations and for hook reservations against direct and batch detour paths. Rejected cases assert that the memory adapter performs no reads, protection changes, writes, or instruction-cache flushes, and that no patch receipt remains.
- Fixed the disposable fixture BAT failure branch so a failed build reliably returns a nonzero exit code. The fixture remains a net472 x64 library hosted by the BAT through 64-bit Windows PowerShell; the launcher rejects an emitted fixture apphost. No fixture EXE is started directly.
- Corrected the patch snapshot code-smell audit to check the immutable snapshot path actually used by cf.patch_status, without requiring a live MethodInfo dereference.

## Validation

- tools/Run-CalradiaForge-Tests.bat --core-only --no-pause <nul passed: net472 and net8.0 builds had zero warnings and errors, Core passed 395/395, ForgeWeave passed 73/73, and the serial x64 detour fixture passed through its BAT host.
- The fixture tests serial behavior and exact restoration. It does not prove safety if another thread is executing a target during code modification; the backend remains experimental and is not a sandbox for extension code.
- No Bannerlord or Modding Kit session was started. Product version remains 25.2.0, ForgeApi.Version remains 12, and no distribution ZIP was generated.

## Dependency security and registry history

- A review of public MonoMod RuntimeDetour project security pages and package information did not establish a published independent security audit as of this date. This is not evidence that a private or unindexed audit does not exist. RuntimeDetour executes hooks in-process; accept only trusted extension code. Its documented serialization of hook-chain mutations does not certify every host lifecycle or make arbitrary concurrent native-code modification safe. References: [RuntimeDetour usage](https://monomod.dev/docs/RuntimeDetour/Usage.html), [chain hot-patching](https://monomod.dev/docs/RuntimeDetour/implementation/ChainHotPatching.html), [project security advisories](https://github.com/MonoMod/MonoMod/security/advisories), and [security policy](https://github.com/MonoMod/MonoMod/security/policy).
- The protected DOCX and SHA-256 integrity chain were reconciled through Rev095 before this append. Source annexes Rev077–Rev095 and the protected DOCX sequence are now contiguous. Rev086–Rev094 source annexes are archival backfills transcribed from the already-published bilingual changelog sections; earlier protected revisions 001–076 were preserved. This entry supersedes Rev095's now-stale note that the ledger was still at Rev076 and reconciliation remained pending.
- The repository Python audit also reports unrelated existing documentation/playbook findings; those are not represented as passing or fixed by this patch-engine revision.