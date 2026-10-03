---
name: discrete-event-simulation
description: Design guidance for scheduling Bannerlord campaign work against engine callbacks, modeling state transitions and measuring game-thread costs. This is not a general Forge simulation runtime.
metadata:
  risk: safe
  source: Calradia Forge Agent Ecosystem (Apache 2.0)
  date_added: "2026-09-28"
---

# Discrete Event Simulation: Campaign Time Slicing & Economic State Machines

Bannerlord exposes campaign callbacks at engine-defined cadences, which can help a mod align work with the event that owns it. This skill offers design guidance for those callbacks; it does not assert a formal engine-wide Discrete Event Simulation architecture or fixed world-population counts. Confirm callback names, delegate signatures, lifecycle, and thread-affinity against the local TaleWorlds assemblies, then measure a representative scenario. Cadence alignment alone does not guarantee thread safety or performance.

---

## 1. Core Principles

1. **Separate Application Updates, Rendering, and Campaign Cadence**:
   - The current module overrides `SubModule.OnApplicationTick(float dt)` for application-update work, including its runtime/input pump. The source does not establish this method as a Gauntlet render callback. Keep update/input and rendering claims separate; verify any engine render hook against the local TaleWorlds assemblies before naming it.
   - Do not schedule world simulation merely because an application update or UI render occurred. Select a campaign event whose scope and cadence match the work, and verify its event name, delegate signature, lifecycle, and behavior for the target game version.
   - Event names verified in current source include `CampaignEvents.HourlyTickEvent`, `CampaignEvents.HourlyTickPartyEvent`, `CampaignEvents.DailyTickHeroEvent`, `CampaignEvents.DailyTickClanEvent`, `CampaignEvents.DailyTickEvent`, and `CampaignEvents.WeeklyTickEvent` in `ClanCharacterProgressionBehavior.RegisterEvents`; the generated `ForgeNoviceHub` settlement scaffold uses `CampaignEvents.DailyTickSettlementEvent`. These references verify source registrations or scaffolds, not every engine-version cadence assumption.
   - Hourly, daily, and weekly workloads below are candidate design examples, not guaranteed game mechanics or shipped Forge behavior. Stable-ID bucket selection can reduce selected work, but the callback still scans its input collection.
2. **Main Game Thread Affinity**:
   - Treat campaign and mission entities as game-thread-affine unless the specific TaleWorlds API documents otherwise.
   - Reading or mutating campaign entities on a worker thread may violate that assumption and cause races, exceptions, or stale state; the exact failure is API- and lifecycle-dependent.
   - Marshal work through a dispatcher explicitly provided by the host. This repository's `GameThreadActionDispatch.RunOrPost` is an internal helper in `CalradiaForge.Mod`, not a public SDK API.
3. **Stateless State Machine Design**:
   - Mod behaviors must remain stateless per `GEMINI.md` Rule B.
   - Derive state from current engine data and stable identifiers rather than persisting mutable engine objects or speculative graph state through `SyncData`.
4. **Deterministic Event Queuing**:
   - When deferring actions (e.g. caravan delayed by ambush), enqueue lightweight tuples: `(CampaignTime scheduledTime, string partyId, EventType action)` and evaluate only when `CampaignTime.Now >= scheduledTime`.

---

## 2. Capabilities & Scope

### Guidance topics (not a unified Forge simulation subsystem)
- `cadence-orchestration`: Guides design reviews for Hourly, Daily, and Weekly campaign event subscriptions; verify each event and delegate signature against the local game assemblies.
- `state-machine-modeling`: Proposes discrete state-transition designs (e.g. patrol, engage, retreat, and resupply); Forge does not ship a general state-machine runtime here.
- `economic-equilibrium-simulation`: Provides design guidance for supply, demand, and workshop-profit scenarios; it does not implement or guarantee economic balance.
- `thread-affinity-enforcement`: Reviews host-specific ways to marshal asynchronous requests; Forge's module-local queue is not a public SDK dispatcher.
- `stable-id-bucket-selection`: Documents the implemented `ForgeTimeSlicer` helper and its selected consumers. Selection is deterministic by stable ID; bucket populations may be uneven, and filtering a full collection still scans it.

### Scope
- **In Scope**: `CampaignBehaviorBase` subsystems, caravan logistics, bandit spawners, town rebellions, workshop economics.
- **Out of Scope**: Real-time 3D tactical combat (delegate to `game-ai-behavior-trees` and `bannerlord-combat-ai`).

---

## 3. Illustrative Simulation Patterns

### Pattern 1: Illustrative Multi-Cadence Campaign Behavior
The following sketch communicates a design shape only. It is not a verified Forge subsystem or a compile-ready integration. Check every event name, delegate signature, cadence, and lifecycle point against the local Bannerlord assemblies before using it; the repository does not validate this complete economic simulation.

```csharp
public class RegionalMarketSimulationBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        // These event names are present in the current project source.
        // Verify their engine signatures and lifecycle for the target version.
        CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyPartyTick);
        CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailySettlementTick);
        CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyEconomyTick);
    }

    public override void SyncData(IDataStore dataStore)
    {
        // 100% Stateless: Rule B compliant
    }

    private void OnHourlyPartyTick(MobileParty party)
    {
        // Candidate work scoped to the event argument; no render work here.
    }

    private void OnDailySettlementTick(Settlement settlement)
    {
        // Candidate work scoped to the event argument.
    }

    private void OnWeeklyEconomyTick() { /* Candidate lower-frequency review work. */ }
}
```

### Pattern 2: Time-Sliced State Machine
Distribute discrete state transitions over time.

```csharp
public static class OptionalHeroWork
{
    public static bool ShouldProcess(Hero hero, int currentHour)
    {
        if (hero == null || string.IsNullOrEmpty(hero.StringId)) return false;

        // Stable-ID selection; bucket populations can vary between hours.
        return CalradiaForge.Sdk.ForgeTimeSlicer.ShouldProcess(hero.StringId, currentHour);
    }
}
```

---

## 4. Sharp Edges & Anti-Patterns

### Edge 1: Executing World Simulation inside `OnApplicationTick`
- **Severity**: CRITICAL
- **Symptom**: Repeated world work may contribute to update cost or occur at a different cadence from the campaign event the logic is intended to follow; the effect depends on the engine callback and workload.
- **Root Cause**: Application updates and campaign events are different callback paths. The current source does not establish `OnApplicationTick` as a Gauntlet render callback or provide a universal frames-to-campaign-time ratio.
- **Fix**: Use a verified `CampaignEvents` callback for work that belongs to campaign time. Measure the actual workload; do not infer a speed multiplier from frame rate alone.

### Edge 2: Unbounded Party Spawning in Hourly Ticks
- **Severity**: CRITICAL
- **Symptom**: An unbounded spawner can increase entity count and may raise update, persistence, or save costs over time; the impact depends on the spawned objects and their lifecycle.
- **Root Cause**: Spawn work has no verified population bound, cleanup path, or ownership tracking.
- **Fix**: Define and test explicit population and cleanup policies for the feature. Measure entity count and save impact in a representative scenario instead of assuming a fixed growth rate.

### Edge 3: Calling Campaign APIs from Background Tasks
- **Severity**: HIGH
- **Symptom**: Background access to mutable game state can produce races, stale reads, exceptions, or other lifecycle-dependent failures.
- **Root Cause**: Campaign entity access is assumed to be game-thread-affine unless the specific engine API documents otherwise.
- **Fix**: Use the owning host's verified main-thread queue. The Forge module uses
  `GameThreadActionDispatch.RunOrPost` with runtime queue callbacks; this is not a
  public SDK `Dispatch` API. Verify queue availability and lifecycle before posting.

---

## 5. Validation Rules & Verification Checklist

1. [ ] **Update vs. Rendering vs. Campaign Work**: Do not describe `SubModule.OnApplicationTick(float dt)` as Gauntlet rendering. Keep campaign simulation out of this application-update callback and use event names/signatures verified for the target engine version.
2. [ ] **Main Game Thread Affinity**: Verified that all `Campaign.Current`, `Settlement`, and `Hero` mutations run on the main thread.
3. [ ] **Population & Entity Bounds**: All procedural spawners enforce strict maximum thresholds.
4. [ ] **Stateless SyncData**: Verified with `tools\Verify-CalradiaForge-StatelessBehavior.bat` that simulation behaviors declare zero `SaveableTypeDefiner`.
5. [ ] **Measured Time Slicing**: Use `ForgeTimeSlicer.ShouldProcess` for stable IDs
   and normalized hours when distributing deferrable work. Filtering after a full
   scan still visits O(N) entities; measure traversal and selected processing separately.
   Do not defer actions that must react within their current campaign event.

## Current repository evidence (2026-10-02)

In the working-tree snapshot for the module manifest version `v25.2.0`,
`src/CalradiaForge.Sdk/ForgeTimeSlicer.cs` implements a manual stable-ID hash,
positive bucket normalization, and indexed-list and enumerable `ProcessBatch`
overloads. Null or empty IDs map to bucket zero; non-positive or single-bucket
counts select all work. With multiple buckets, both batch overloads still visit
the full input and call the ID selector for every non-null entry before choosing
which processor callbacks to invoke. Time slicing reduces selected processing,
not input traversal; custom enumerators, selectors, processors, and campaign API
calls may allocate or dominate cost.

Current call sites include hourly hero maintenance in
`src/CalradiaForge.Mod/CampaignBehaviors/AgentCognitiveMemoryBehavior.cs` and
hourly hero/party selection in `ClanCharacterProgressionBehavior.cs`. The hero
callbacks first enumerate `Hero.AllAliveHeroes`, so they retain O(N) traversal.
The latter's weekly clan assessment is a separate full `Clan.All` scan and is
not bucketed. Do not infer that every event handler is time-sliced.

Event registration is not proof that every advertised campaign feature has an
implemented gameplay effect. In `ClanCharacterProgressionBehavior`, some
callbacks log a notification while others only increment session counters or
contain comment-only assessment branches (for example, the daily hero and
hourly party checks). `ClanCharacterProgressionTests` covers registration,
statelessness, guards, call sites and selected helper behavior; it does not
verify campaign outcomes in a live save.

`tests/CalradiaForge.Tests/SdkFeaturesTests.cs` exercises null/empty IDs,
single-bucket fallback, 24-hour repeatability, large positive-hour normalization
and selector/processor call counts proving the full scan. The
`AgentCognitiveMemoryTests` and `ClanCharacterProgressionTests` also check
selected source call sites. These are deterministic helper/source-contract
checks, not measurements of callback duration or proof of runtime behavior in
Bannerlord. No representative in-game time-slicing benchmark is established by
these checks; report harness results separately and leave game performance
unverified until measured in a controlled live scenario.
