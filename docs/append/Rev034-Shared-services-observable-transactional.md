# Rev034 — Observable and transactional shared services

**Date:** 2026-09-25  
**Release line:** Calradia Forge 23.0.0; SDK API version 7  
**Scope:** SDK shared-service resolution, grouped publication, and lifecycle observation.  
**Distribution:** Source follow-up only; no ZIP or product package regenerated.

## Problem and technical rationale

The shared-service registry offered a fail-fast `Require<T>` lookup and one-at-a-time `Provide<T>` registrations. Consumers that needed optional dependencies had to catch exceptions or implement their own polling, while providers exposing related services could make them visible one at a time and manage multiple registration handles. These patterns made optional integrations and all-or-nothing service groups harder to express consistently.

## Technical solution and architectural decisions

`ForgeApi.Version` advances from 6 to 7 while the product remains on 23.0.0. `ModuleLibrary.Resolve<T>` returns an immutable `SharedServiceResolution<T>` with a status, diagnostic context, and `TryGetService`; lookup states distinguish availability, missing provider, missing service, CLR contract mismatch, and incompatible version. Invalid arguments, thread-affinity violations, and disposed-object use remain exceptional. Existing `Require<T>` remains fail-fast and preserves its exception type and semantics.

`ModuleLibrary.BeginPublication()` creates a `SharedServiceBatch`. Providers stage entries with `Add<T>`, then publish the complete group with `Commit()`. Validation completes before the registry changes, preventing partial visibility when a staged entry is invalid or collides. The batch object is the lifetime lease: disposing before commit discards staged work; disposing after commit withdraws the full group. It withdraws registrations but does not own or dispose provider implementations. Existing individual `Provide<T>` handles remain available.

`ModuleLibrary.Watch<T>` returns a `SharedServiceMonitor<T>` whose `Current` value is the initial snapshot; monitor construction does not raise `Changed`. Later availability and withdrawal update the snapshot and notify observers. Withdrawal followed by publication creates a new registration generation; there is no in-place replacement operation. Notifications are synchronous on the registry thread after each complete state change. `Changed` event arguments are immutable transition snapshots and may be stale by the time later handlers run; `monitor.Current` and `Resolve<T>` report the latest state, and a previously obtained handle may already be invalid after withdrawal. A registry mutation inside a `Changed` handler takes effect synchronously and refreshes monitor snapshots immediately; only delivery of resulting callbacks is queued while a notification pass is active to prevent recursive dispatch. Handler exceptions are isolated, retained through `LastNotificationError`, and do not stop other observers. Consumer unload detaches watches; global registry disconnect silently detaches and disposes them without a final event.

## Code, assets, and dependencies

The change adds SDK shared-service API types and members only. The SDK remains compatible with `net472`; no TaleWorlds assembly, IPC contract, third-party dependency, module command, or game behavior is added. The provider/consumer example and service guidance describe the additive APIs without changing shared contract identity or the existing single-provider packaging rule.

## Validation and evidence limits

- `cmd /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"`: build completed with 0 warnings and 0 errors; Core passed 263/263 and ForgeWeave passed 43/43.
- The change is intended to retain tests for resolution outcomes, fail-fast `Require<T>`, atomic visibility and rollback-free failure, registration collisions and lifetime, watcher snapshots and transitions, callback isolation, reentrancy, thread affinity, provider/consumer integration, manifest dependency, and single-copy contract packaging.
- The test suites ran through the `.bat` launcher; no test `.exe` or `.dll` was started directly. Bannerlord, campaigns, and battles were not launched. No ZIP was generated.

This appendix records the approved API v7 addition without replacing earlier ledger paragraphs or changing product version 23.0.0.
