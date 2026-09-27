# Rev025 — SDK Contract 8, Bounded Memory, and ForgeWeave Lifecycle

**Date:** September 25, 2026  
**Version:** Calradia Forge 24.0.0, version metadata unchanged in this update  
**Scope:** SDK, Core, and documentation. ForgeWeave subscription lifecycle, `ForgeAgentMemory` quotas, and source package provenance.

## Observed problem and technical justification

The documentation described `ForgeAgentMemory` as unbounded and claimed TTL support in all three tiers. That did not reflect the new contract's capacity limits; unbounded growth could retain agent data for the entire session. An immediate subscription could also run before Forge published its event host, and the documentation lacked a lifecycle owner to unregister and recover the handler after reconnection. The SDK reference still showed contract version 5 and did not distinguish v24.0.0 source from the existing 23.0.0 product ZIPs.

## Technical solution and architectural decisions

The public SDK contract advances to version 8. `ForgeCampaignEvents.SubscribeWeaveWhenAvailable` returns a `ForgeWeaveRegistration` that owns both the pending subscription and active handler. It exposes `WaitingForHost`, `Registered`, `HostUnsupported`, `Failed`, and `Disposed`; it registers on connection, withdraws on disconnect, registers again after reconnection, and stops accepting future connections after `Dispose()`. Registration and active cleanup run synchronously on the host connection thread. A pending handle may be disposed from any thread; disposing an active handle off-thread throws `InvalidOperationException`, leaves the handler active, and records the error so the owner can retry on the correct thread. The immediate `SubscribeWeave` call remains available and throws `InvalidOperationException` when no event host is available or it is called off the connection thread.

`ForgeAgentMemory` allows at most 2,048 distinct IDs shared across its three tiers. Semantic memory stores up to 128 facts per agent and is the only tier with optional TTL. Episodic memory stores 512 episodes per agent and 128 per type; when either limit is exceeded, it evicts the oldest agent-wide episode first and, if needed, the oldest remaining episode of the incoming type. Procedural memory stores up to 128 tasks per agent. Semantic `TryUpsert` and procedural `TryAdd` return `false` when a new key or agent ID exceeds a limit; episodic `TryAdd` returns `false` only when the global limit rejects a new ID, since its tier quotas use FIFO eviction. Legacy counterparts throw `InvalidOperationException` when capacity rejects a write. Existing keys can be updated in Semantic and Procedural memory while their tier limit is full.

The store is thread-safe, process-local, and is not serialized into Bannerlord saves. `ClearAgent` and `ClearAll` release entries. The guide distinguishes the 24.0.0 source from `CalradiaForge-Modules-23.0.0.zip` and `CalradiaForge-Source-SDK-23.0.0.zip`, which were not rebuilt.

## Changes to assets, code, and dependencies

- Synchronizes `docs/sdk-reference.md` and `docs/sdk-reference.es.md`, plus `docs/FORGEWEAVE.md` and `docs/FORGEWEAVE.es.md`.
- Updates `docs/CODEMAP_SDK_GAMEMODELS.md` with SDK contract 8, subscription lifecycle management, and exact memory limits.
- Adds v24.0.0 source notes to the English and Spanish changelogs while preserving earlier entries.
- Adds no dependencies and regenerates no ZIP packages.

## Validation and evidence limits

- Documentation was checked against the source APIs in `ForgeCampaignEvents`, `ForgeAgentMemory`, and `ForgeApi`, and the product version in `Directory.Build.props`.
- `python tools/append_detailed_changelog_revision.py docs/append/Rev025-SDK-v8-bounded-memory-and-weave-lifecycle-2026-09-25.es.md` checks that prior paragraph text, styles, and canonical XML remain an identical prefix and creates the new protected revision.
- `python tools/record_detailed_changelog_integrity.py` verifies the SHA-256 chain and records the new revision.
- As requested, no projects were built and no test suites were run; Bannerlord was not launched. The two 23.0.0 ZIPs remain unchanged, and this update does not claim they contain contract 8 APIs.

This addendum extends the previous revision without replacing its paragraphs. It preserves the commands, paths, public APIs, and product version.
