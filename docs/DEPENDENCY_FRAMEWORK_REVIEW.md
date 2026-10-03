# Dependency framework review

This review defines the current extension path for Calradia Forge. ForgeWeave is the primary framework for new cooperative tools. Patch diagnostics provide a neutral Forge-owned snapshot; an optional reflection observer checks for an expected public query surface on an already-loaded assembly named `0Harmony`. That check is a limited runtime diagnostic signal, not a Harmony compatibility test. Harmony remains neither a Forge dependency nor the recommended author workflow.

## ForgeWeave: the current framework

ForgeWeave is an original, standalone event framework owned by Calradia Forge. It runs extension handlers only from Forge adapter callbacks that correspond to known Bannerlord lifecycle points. It does not discover arbitrary methods, emit IL, replace game callbacks, load assemblies, or require a third-party mod framework.

An author registers one `IForgeEventHandler` through `ForgeApi.Events`. Its subscription declares a globally unique descriptor, event kind, priority, optional same-event/same-priority ordering references, access level, an exact-match scalar filter, failure limit and, for `Pulse`, a minimum interval. Forge exposes copied scalar event data only; handlers receive neither live Bannerlord objects nor Forge test services.

| Author need | ForgeWeave behavior | Deliberate boundary |
| --- | --- | --- |
| Run work after a known host lifecycle point | Dispatches explicit Forge-ready, screen, context, campaign, mission, agent and bounded pulse events | Does not intercept arbitrary methods |
| Coordinate independently authored handlers | Computes priority and same-priority `Before`/`After` order deterministically | Missing, cross-event, cross-priority, self-referential and cyclic constraints are blocked rather than inferred |
| Restrict state changes | Applies descriptor context, test-mode and copied-campaign safeguards to write-capable handlers | Does not grant a general game-object or service capability |
| Keep one faulty extension from hiding others | Isolates exceptions, records failures and quarantines handlers after their declared bounded limit | Cannot forcibly terminate arbitrary synchronous C# code |
| Investigate extension behavior | Captures copied handler health, timings, event counts, findings and a bounded dispatch journal in the game, desktop companion, JSON and HTML reports | Does not attribute whole-game performance or a third-party failure to an extension automatically |
| Verify a known lifecycle event again | Replays one retained, copied host event only after handler opt-in and exact-context checks | Does not inject arbitrary payloads, intercept methods or bypass writer gates |

These are concrete operational properties for cooperative tools. They are not a claim that ForgeWeave replaces a patch library for every use case. An author who must alter an arbitrary third-party method needs a separate, explicit integration design and validation for that method.

## Safety and execution model

The Bannerlord adapter queues scalar lifecycle observations and drains a bounded number on the game thread. The queue, copied data, ordering references, journal and finding collections all have bounded capacity. A `Pulse` subscription must declare an interval from 250 to 60,000 milliseconds; Forge's host pulse and the individual subscription interval both limit delivery.

ForgeWeave checks context and access before invoking a handler. `Observe` and diagnostic work can run in their declared valid contexts. `CampaignWrite` and `MissionWrite` use the existing test-mode and campaign-copy gates. `StopPropagation` ends later ForgeWeave handler delivery for that one event; it never cancels, replaces or blocks Bannerlord's original callback.

Replay Lab retains only completed host event evidence: sequence, kind, logical context, timing, copied scalar payload and the original outcome. `Disabled` is the default replay mode. `ObserveOnly` is for read-only handlers; `Live` stays subject to the same access, test-mode and copied-campaign checks as a first delivery. A replay receives its own sequence and names its source through `ForgeEvent.SourceSequence`; Forge never records a replay as a new source. Missing/stale records, context mismatch, lack of an opted-in handler and writer-gate failures are explicit outcomes, not implicit retries.

The `framework` protocol action returns a copied `ForgeWeaveSnapshot`, `event-journal` returns its bounded recent dispatches, and `replay` accepts one retained sequence in a connected game. The registry, pipe and desktop row selection never accept arbitrary event data. The in-game Framework action and desktop Framework section consume the same data, so an author can inspect declared order, blocked status, failure counts, quarantine, replay evidence and timing without adding a diagnostic dependency. See [FORGEWEAVE.md](FORGEWEAVE.md) and [SDK.md](SDK.md) for the API and limits.

## Existing SDK paths

The legacy SDK v1 registry still accepts tests, commands and diagnostic providers. Shared libraries provide typed, versioned services with ownership and lifetime checks. ForgeWeave filters and handlers are additive in SDK v3: they do not change those existing contracts, and their IDs share the global SDK identifier space.

Patch Blueprint Preflight remains a separate, read-only authoring aid. `IPatchBlueprintProvider` supplies inert declarations with exact assembly, type, member and parameter metadata. Forge resolves them only against already loaded assemblies, reports structural problems and never applies a declaration, calls its callback, infers final execution order, or changes a patch. This makes a preflight useful without making ForgeWeave a patch engine. See [PATCH_BLUEPRINTS.md](PATCH_BLUEPRINTS.md).

## Patch diagnostics and optional external observation

`ForgePatchDiagnostics` returns a bounded `ForgePatchDiagnosticsSnapshot` with Forge-owned hooks and replacement-patch records, counts and notes. The `patch-diagnostics` protocol action is the current read-only entry point. Its optional external observer searches caller-supplied loaded assemblies for the expected `0Harmony` assembly identity, resolves `HarmonyLib.Harmony` from that same assembly, and checks for the public static `GetAllPatchedMethods()` query returning `IEnumerable` and `GetPatchInfo(MethodBase)` query returning a value. This expected query surface is not a promise of compatibility with a Harmony release or mod combination. The observer does not load or distribute Harmony and creates no compile-time dependency. External observations include limited target identity, owners, patch categories, declared ordering metadata and patch-method identity. They are review evidence only: a shared target is not proof of a conflict, and an owner string is not automatically a Bannerlord module ID.

The observer is optional and observational: it never patches, unpatches or reorders third-party code. It cannot see other patching backends or prove compatibility or coexistence. If the expected assembly, type or query surface is absent, Forge reports that observation without changing its own behavior. This diagnostic signal is bounded at the assembly/target enumeration and copied-output layers; external queries and public getters still execute synchronously in-process and are not sandboxed. The 0.4.0 native Atlas evidence in [VALIDATION.md](VALIDATION.md) is historical and does not validate ForgeWeave.

## Quality and validation boundaries

ForgeWeave should be evaluated through reproducible checks for registration, ordered dispatch, invalid constraints, access/context gates, copied data, pulse bounds, propagation, exception isolation, quarantine, telemetry, opt-in replay, context mismatch, writer-gate rejection, replay-loop prevention and report serialization. A short native session can verify that the host advertises `framework`, `event-journal` and `replay`, returns a snapshot, replays the read-only `ForgeReady` example, opens the panel and exits normally. It does not establish campaign, mission, compatibility or performance behavior.

No novelty, universal superiority or replacement claim is established by this review. ForgeWeave's value is its declared lifecycle boundary, deterministic cooperative scheduling, permission checks, containment and shared diagnostics. Compatibility with a particular mod combination requires its own evidence.

## Historical references

- [Harmony query and patch API](https://harmony.pardeike.net/api/HarmonyLib.Harmony.html)
- [Patches metadata](https://harmony.pardeike.net/api/HarmonyLib.Patches.html)
- [Individual patch metadata](https://harmony.pardeike.net/api/HarmonyLib.Patch.html)

These upstream references provide context for Harmony's query and patch metadata APIs and the historical Atlas. The current optional observer is tested with an isolated fixture that exposes the expected `0Harmony` identity and public query surface; this does not verify an installed Harmony release, a particular mod combination, or runtime coexistence. The adapter remains optional and creates no Forge dependency.
