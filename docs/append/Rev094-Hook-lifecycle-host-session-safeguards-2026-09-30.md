# Rev094 — Archival source reconciliation

**Date:** 30 September 2026  
**Version:** Calradia Forge 25.2.0, unchanged  
**Scope:** Archival source backfill from the published changelog; no code or runtime changes.

**Archival reconciliation note:** This source annex was reconstructed on 2026-09-30 from the already-published English and Spanish changelog sections. No original source annex for this revision existed in `docs/append/`. This backfill does not modify or replace any pre-existing protected DOCX or integrity record.

## Calradia Forge hook lifecycle and host-session safeguards — 2026-09-30 (Rev094)

- Adds an optional disconnect guard so SDK replacement or disconnection cannot silently orphan active or uncertain hooks when the approved game-thread/menu context is unavailable. Verification exceptions remain visible as conflicts, and handles for hooks removed externally are disposed before the service reports a clean reverted state.
- Tightens the Bannerlord menu gate to exact CLR identity for the engine's `GauntletInitialScreen`, resolved from the game Gauntlet assembly at runtime. If the official type cannot be resolved, hook mutations fail closed; name-only lookalikes are rejected.
- Adds `hook-plan-cancel`, which carries only the host session and preview token. Cancellation is bound to that exact pair; WPF retains the preview when cancellation is unconfirmed and stops snapshot refresh instead of discarding local state. English and Spanish SDK protocol documentation now describes the action.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: Core 384/384 and ForgeWeave 73/73 passed; the relevant `net472`, `net8.0`, and Mod `net472` builds completed with 0 warnings and 0 errors. The isolated `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat` passed guarded disconnect, external removal, uncertain verification, and exact `15 → 32 → 15` restoration.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause`: Desktop 65/65 and 292 WPF render cases passed. The render harness reported 13,895 ms total; this is harness timing, not observed application interaction latency.
- No live Bannerlord or Modding Kit session was started. The fixture remains serial and does not establish safety while another thread executes a target during Apply or Revert. Product version remains 25.2.0 and no ZIP was regenerated or modified.
