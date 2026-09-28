---
name: discrete-event-simulation
description: Discrete-event and time-sliced simulation architecture for Mount & Blade II: Bannerlord campaign systems. Orchestrating Hourly, Daily, and Weekly ticks, state machines, economic equilibrium, and thread-safe event scheduling.
risk: safe
source: Calradia Forge Agent Ecosystem (Apache 2.0)
date_added: 2026-09-28
---

# Discrete Event Simulation: Campaign Time Slicing & Economic State Machines

The campaign world of Mount & Blade II: Bannerlord is an asynchronous, living macro-simulation. Hundreds of lords, thousands of roaming parties, and dozens of settlements interact continuously. Simulating every transaction, rebellion, and movement trajectory every frame is impossible. The TaleWorlds engine employs a **Discrete Event Simulation** architecture organized around temporal tick cadences. A master simulation architect maps systems to their natural temporal rhythm and ensures rock-solid thread safety.

---

## 1. Core Principles

1. **Temporal Cadence Alignment**:
   - Match simulation frequency to the natural timescale of the phenomenon:
     - **Frame Tick (`OnApplicationTick`)**: Player input, Gauntlet UI rendering, camera tracks. **NEVER** run world simulation here.
     - **Hourly Tick (`HourlyTickEvent`)**: Party speed modifications, local patrol state machines, anti-lag hero progression.
     - **Daily Tick (`DailyTickEvent`, `DailyTickParty`, `DailyTickSettlement`)**: Workshop production, settlement food/loyalty decay, caravan trade decisions, tax collection.
     - **Weekly Tick (`WeeklyTickEvent`)**: Diplomatic peace/war evaluation, clan wage payments, kingdom policy consensus.
2. **Main Game Thread Affinity**:
   - TaleWorlds' `Campaign.Current` object graph is **strictly single-threaded**.
   - Any read or mutation of `Hero`, `Settlement`, `MobileParty`, or `Clan` from a background `Task.Run` or threadpool thread triggers race conditions and unhandled memory corruption.
   - Use `GameThreadActionDispatch` whenever external events (IPC, background workers) need to interact with the campaign.
3. **Stateless State Machine Design**:
   - Mod behaviors must remain stateless per `GEMINI.md` Rule B.
   - Discrete state machines should derive their state from the simulation entities (`Hero.Gold`, `Settlement.Loyalty`, `MobileParty.CurrentSettlement`) rather than maintaining persistent custom graph objects in `SyncData`.
4. **Deterministic Event Queuing**:
   - When deferring actions (e.g. caravan delayed by ambush), enqueue lightweight tuples: `(CampaignTime scheduledTime, string partyId, EventType action)` and evaluate only when `CampaignTime.Now >= scheduledTime`.

---

## 2. Capabilities & Scope

### Capabilities
- `cadence-orchestration`: Subscribes and synchronizes systems across Hourly, Daily, and Weekly campaign events.
- `state-machine-modeling`: Implements discrete state transitions (e.g. *Patrolling* $\rightarrow$ *Engaging* $\rightarrow$ *Retreating* $\rightarrow$ *Resupplying*).
- `economic-equilibrium-simulation`: Balances supply, demand, and workshop profit margins without runaway inflation.
- `thread-affinity-enforcement`: Safely marshals asynchronous external telemetry to the main game loop.
- `time-slicing-distribution`: Evenly distributes simulation loads across campaign hours using modulo hashing.

### Scope
- **In Scope**: `CampaignBehaviorBase` subsystems, caravan logistics, bandit spawners, town rebellions, workshop economics.
- **Out of Scope**: Real-time 3D tactical combat (delegate to `game-ai-behavior-trees` and `bannerlord-combat-ai`).

---

## 3. Concrete Simulation Patterns

### Pattern 1: Multi-Cadence Campaign Behavior
Register discrete simulation hooks in `OnGameStart`.

```csharp
public class RegionalMarketSimulationBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        // 1. Hourly: High-frequency party proximity and local path checks
        CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyPartyTick);

        // 2. Daily: Economic production and price equilibrium recalculation
        CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailySettlementTick);

        // 3. Weekly: Macro kingdom inflation adjustments and trade audits
        CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyEconomyTick);
    }

    public override void SyncData(IDataStore dataStore)
    {
        // 100% Stateless: Rule B compliant
    }

    private void OnHourlyPartyTick(MobileParty party)
    {
        if (party.IsCaravan && party.LeaderHero != null)
        {
            // Lightweight hourly route progress
        }
    }

    private void OnDailySettlementTick(Settlement settlement)
    {
        if (settlement.IsTown)
        {
            // Daily workshop throughput and tariff computation
        }
    }

    private void OnWeeklyEconomyTick()
    {
        // Global price normalization across all trade hubs
    }
}
```

### Pattern 2: Time-Sliced State Machine
Distribute discrete state transitions over time.

```csharp
public enum CaravanState { Trading, Fleeing, Ambushed, Restocking }

public static class CaravanStateMachine
{
    public static void Step(MobileParty caravan, int currentHour)
    {
        // Anti-lag: Only step 1/24th of caravans each hour
        if (Math.Abs(caravan.Id.GetHashCode()) % 24 != currentHour)
            return;

        // Derive state purely from simulation facts (Stateless)
        CaravanState currentState = DetermineState(caravan);

        switch (currentState)
        {
            case CaravanState.Trading:
                EvaluateNextTradeDestination(caravan);
                break;
            case CaravanState.Fleeing:
                NavigateTowardsNearestGarrison(caravan);
                break;
            case CaravanState.Restocking:
                PurchaseCommodities(caravan);
                break;
        }
    }

    private static CaravanState DetermineState(MobileParty caravan)
    {
        if (caravan.TargetParty != null && caravan.TargetParty.IsBandit)
            return CaravanState.Fleeing;
        if (caravan.CurrentSettlement != null && caravan.CurrentSettlement.IsTown)
            return CaravanState.Restocking;
        return CaravanState.Trading;
    }
}
```

---

## 4. Sharp Edges & Anti-Patterns

### Edge 1: Executing World Simulation inside `OnApplicationTick`
- **Severity**: CRITICAL
- **Symptom**: Stuttering gameplay on the world map; simulation speed accelerates wildly during fast-forward (1x vs 3x vs 8x speed desync).
- **Root Cause**: `OnApplicationTick` runs per visual frame, not per simulation time step. At high FPS, the economy runs 10x faster than intended.
- **Fix**: Run simulation solely inside `CampaignEvents` hooks (`HourlyTick`, `DailyTick`), which are properly scaled by TaleWorlds' campaign time acceleration.

### Edge 2: Unbounded Party Spawning in Hourly Ticks
- **Severity**: CRITICAL
- **Symptom**: Game slows to a crawl after 50 campaign days; save files grow to hundreds of megabytes.
- **Root Cause**: An hourly spawner adds 2 bandit parties per hour without enforcing a global population ceiling.
- **Fix**: Enforce strict caps: `if (activePartiesCount >= MaxPartiesPerRegion) return;`.

### Edge 3: Calling Campaign APIs from Background Tasks
- **Severity**: HIGH
- **Symptom**: Random, irreproducible crash inside `TaleWorlds.CampaignSystem.Campaign.OnTick()` or `MBObjectManager.GetObject()`.
- **Root Cause**: Background threadpool task reading `Hero.AllAliveHeroes` while the game thread is modifying clan memberships.
- **Fix**: Marshal back to the game thread with `GameThreadActionDispatch.Dispatch(() => { ... })`.

---

## 5. Validation Rules & Verification Checklist

1. [ ] **No Simulation in `OnApplicationTick`**: Confirmed world updates occur only inside `CampaignEvents` listeners.
2. [ ] **Main Game Thread Affinity**: Verified that all `Campaign.Current`, `Settlement`, and `Hero` mutations run on the main thread.
3. [ ] **Population & Entity Bounds**: All procedural spawners enforce strict maximum thresholds.
4. [ ] **Stateless SyncData**: Verified with `tools\verify_stateless_behavior.ps1` that simulation behaviors declare zero `SaveableTypeDefiner`.
5. [ ] **Modulo Time Slicing**: Hourly loops over entities check `Math.Abs(entity.Id.GetHashCode()) % 24 == currentHour`.
