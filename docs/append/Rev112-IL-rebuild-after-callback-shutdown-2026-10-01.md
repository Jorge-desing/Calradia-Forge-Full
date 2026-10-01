# Rev112 — IL Reconstruction After Runtime Callback Shutdown

**Date:** October 1, 2026  
**Version:** Calradia Forge 25.2.0, unchanged; ForgeApi.Version 13  
**Scope:** Core IL activation reconstruction during callback shutdown, isolated detour regression, and bilingual documentation.

## Observed problem and technical rationale

Stopping runtime callbacks during unload must prevent later gameplay callback dispatch and new management operations. An already-applied IL transformation has a separate lifetime: an external ILHook may rebuild the same target chain while the authorized transformation remains installed. Refusing its reconstruction solely because callbacks were stopped could leave that chain inconsistent with the retained activation.

## Technical solution and architectural decisions

The reconstruction path recognizes the exact retained activation that was explicitly authorized and applied earlier. That activation can reconstruct its already-applied deterministic transformation after `StopCallbacksForUnload`, including when the host gate is false and an external ILHook is added or undone. This preserves chain coherence; it does not resume Prefix/Postfix/Finalizer gameplay callback dispatch or permit a new Apply, Revert, or disconnect outside the approved context. Approved cleanup still restores the original target. An uncertain Undo remains retained and blocked rather than being reported as verified removal.

## Source and dependency scope

The update covers Core hook activation/shutdown interoperability and the existing disposable x64 detour fixture. It adds no executable SDK or IPC payload, no automatic hook discovery/application, and no product or SDK version increment. Rev001–Rev111 remain unchanged.

## Validation and evidence limits

- `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat --no-pause` passed in the isolated x64 serial host with 0 warnings and 0 errors.
- With `StopCallbacksForUnload`, a false host gate, and external ILHook add/undo, the exact retained authorized activation reconstructed values 11 → 15 → 11. Apply, Revert and CanDisconnect remained denied in that context.
- Approved cleanup restored the original value 3. The uncertain-Undo regression remained retained and blocked.
- These results cover the deterministic serial fixture and do not certify general concurrent execution, every external ILHook implementation or every host shutdown lifecycle. Live Bannerlord validation remains unverified; no campaign or battle evidence is claimed.
- This annex claims no distribution ZIP hashes or final archive-audit result. Packaging follows completion of the revision and integrity append.

This revision appends shutdown-interoperability evidence to Rev111 without modifying protected historical revisions.
