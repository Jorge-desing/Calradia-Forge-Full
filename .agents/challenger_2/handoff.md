# Empirical Challenge Report — Challenger 2: Defensive Boundary Conditions & Null Safety

**Target Subject**: `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs`  
**Test Suite**: `tests/CalradiaForge.Tests/ClanCharacterProgressionTests.cs`  
**Test Binary**: `.\tests\CalradiaForge.Tests\bin\Release\net472\CalradiaForge.Tests.exe`  
**Verdict**: **REQUEST_CHANGES**

---

## 1. Observation

Direct empirical test execution of the solution via `.\tests\CalradiaForge.Tests\bin\Release\net472\CalradiaForge.Tests.exe` revealed **4 unhandled fatal exceptions (`NullReferenceException`)** when evaluating defensive boundary conditions in `ClanCharacterProgressionBehavior.cs`:

### Test Suite Execution Output
```
Command: .\tests\CalradiaForge.Tests\bin\Release\net472\CalradiaForge.Tests.exe
Result: 202 passed, 4 failed (Exit Code 1)
```

### 1.1 Verbatim Failure 1: Un-guarded `mother.Clan` in `OnGivenBirth`
- **Location**: `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs:line 566`
- **Method**: `private void OnGivenBirth(Hero mother, List<Hero> aliveChildren, int stillbornCount)`
- **Verbatim Error**:
  ```
  [CRASH REPRODUCED] in OnGivenBirth:
  System.NullReferenceException: Referencia a objeto no establecida como instancia de un objeto.
     en CalradiaForge.Mod.CampaignBehaviors.ClanCharacterProgressionBehavior.OnGivenBirth(Hero mother, List`1 aliveChildren, Int32 stillbornCount) en C:\Users\Alex\Documents\Mod Desarrolladores\src\CalradiaForge.Mod\CampaignBehaviors\ClanCharacterProgressionBehavior.cs:línea 566
  FAIL ClanProgression: Empirical Stress - Empty and null aliveChildren in OnGivenBirth
  ```
- **Code Quote** (Lines 563–569):
  ```csharp
  for (int i = 0; i < aliveChildren.Count; i++)
  {
      Hero child = aliveChildren[i];
      if (child != null && mother.Clan == Clan.PlayerClan)
      {
          ForgeLogger.PrintSuccess($"Joyous tidings! A healthy child, {child.Name}, was born to {mother.Name}.");
      }
  }
  ```
- **Observed Behavior**: If `mother.Clan == null` (unaligned notables, wanderers, bandits) or when `Campaign.Current == null`, evaluating `mother.Clan == Clan.PlayerClan` causes the engine to query `Clan.PlayerClan`, which invokes `Campaign.Current.PlayerClan`, throwing an unhandled `NullReferenceException`.

### 1.2 Verbatim Failure 2: Un-guarded `Clan.PlayerClan` in `OnClanDestroyed`
- **Location**: `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs:line 343`
- **Method**: `private void OnClanDestroyed(Clan clan)`
- **Verbatim Error**:
  ```
  [CRASH REPRODUCED] in OnClanDestroyed:
  System.NullReferenceException: Referencia a objeto no establecida como instancia de un objeto.
     en CalradiaForge.Mod.CampaignBehaviors.ClanCharacterProgressionBehavior.OnClanDestroyed(Clan clan) en C:\Users\Alex\Documents\Mod Desarrolladores\src\CalradiaForge.Mod\CampaignBehaviors\ClanCharacterProgressionBehavior.cs:línea 343
  FAIL ClanProgression: Empirical Stress - OnClanDestroyed null safety when Campaign inactive
  ```
- **Code Quote** (Lines 340–346):
  ```csharp
  if (clan == null) return;
  Interlocked.Increment(ref _clanEventsProcessed);

  if (clan == Clan.PlayerClan)
  {
      ForgeLogger.PrintError("The player dynasty has been extinguished!");
  }
  ```
- **Observed Behavior**: Passing any non-null clan instance directly triggers `clan == Clan.PlayerClan`. In TaleWorlds, `Clan.PlayerClan` executes `Campaign.Current.PlayerClan`. When `Campaign.Current` is uninitialized or null (e.g., scene transitions, custom battles, or outside active campaign sessions), this throws a fatal `NullReferenceException`.

### 1.3 Verbatim Failure 3: Fatal Crash in Public Static `ShouldProcessInCurrentHour`
- **Location**: `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs:line 158`
- **Method**: `public static bool ShouldProcessInCurrentHour(string stringId)`
- **Verbatim Error**:
  ```
  [CRASH REPRODUCED] in ShouldProcessInCurrentHour("party_123"):
  System.NullReferenceException: Referencia a objeto no establecida como instancia de un objeto.
     en CalradiaForge.Mod.CampaignBehaviors.ClanCharacterProgressionBehavior.ShouldProcessInCurrentHour(String stringId) en C:\Users\Alex\Documents\Mod Desarrolladores\src\CalradiaForge.Mod\CampaignBehaviors\ClanCharacterProgressionBehavior.cs:línea 158
  FAIL ClanProgression: Empirical Stress - ShouldProcessInCurrentHour with non-empty string
  ```
- **Code Quote** (Lines 156–161):
  ```csharp
  public static bool ShouldProcessInCurrentHour(string stringId)
  {
      if (string.IsNullOrEmpty(stringId)) return false;
      int entityHash = stringId.GetHashCode() & 0x7FFFFFFF;
      int currentHour = (int)CampaignTime.Now.ToHours % 24;
      return (entityHash % 24) == currentHour;
  }
  ```
- **Observed Behavior**: When passed any non-empty string, line 159 unconditionally invokes `CampaignTime.Now`. In TaleWorlds, `CampaignTime.Now` executes `Campaign.Current.MapTimeTracker.Now`. When `Campaign.Current == null`, this throws `NullReferenceException`.

### 1.4 Verbatim Failure 4: Fatal Crash in Public Interface Property `ActiveTrackedHeroesCount`
- **Location**: `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs:line 34`
- **Property**: `public int ActiveTrackedHeroesCount => Hero.AllAliveHeroes != null ? Hero.AllAliveHeroes.Count : 0;`
- **Verbatim Error**:
  ```
  [CRASH REPRODUCED] in ActiveTrackedHeroesCount getter:
  System.NullReferenceException: Referencia a objeto no establecida como instancia de un objeto.
     en CalradiaForge.Mod.CampaignBehaviors.ClanCharacterProgressionBehavior.get_ActiveTrackedHeroesCount() en C:\Users\Alex\Documents\Mod Desarrolladores\src\CalradiaForge.Mod\CampaignBehaviors\ClanCharacterProgressionBehavior.cs:línea 34
  FAIL ClanProgression: Empirical Stress - ActiveTrackedHeroesCount null safety when Campaign inactive
  ```
- **Observed Behavior**: The author wrote `Hero.AllAliveHeroes != null ? ... : 0`, assuming `Hero.AllAliveHeroes` could be safely checked for null. However, the TaleWorlds property getter `Hero.AllAliveHeroes` internally executes `Campaign.Current.AliveHeroes`. When `Campaign.Current == null`, evaluating `Hero.AllAliveHeroes` throws an unhandled `NullReferenceException`.

---

## 2. Logic Chain

1. **Contract Requirement**:
   `ORIGINAL_REQUEST.md` (R1 & R2) and `PROJECT.md` mandate that the CampaignBehavior operate statelessly, defer complex queries to avoid engine initialization crashes, and handle edge cases cleanly without crashing on null or boundary values.
2. **Defensive Guard Inconsistency**:
   In several methods, the author appropriately implemented defensive checks:
   - Line 189: `if (hero.Clan != null && hero.Clan == Clan.PlayerClan)`
   - Line 214: `if (!isCaptive && hero.Clan != null && hero.Clan == Clan.PlayerClan)`
   - Line 240: `if (victim.Clan != null && victim.Clan == Clan.PlayerClan)`
   - Line 278: `if (hero.Clan != null && hero.Clan == Clan.PlayerClan)`
   - Line 367: `if (clan != null && clan == Clan.PlayerClan)`
   - Line 602: `if (shouldNotify && hero.Clan != null && hero.Clan == Clan.PlayerClan)`
   - Line 615: `if (hero.Clan != null && hero.Clan == Clan.PlayerClan)`
3. **The Omission & Mechanism of Failure**:
   In contrast, the author omitted null checks on the left operand in:
   - Line 566: `if (child != null && mother.Clan == Clan.PlayerClan)` (missing `mother.Clan != null`)
   - Line 503: `if (newGovernor != null && town.OwnerClan == Clan.PlayerClan)` (missing `town.OwnerClan != null`)
   - Line 533: `if (firstHero.Clan == Clan.PlayerClan || secondHero.Clan == Clan.PlayerClan)` (missing `Clan != null`)
   - Line 586: `if (shouldNotify && hero.Clan == Clan.PlayerClan)` (missing `hero.Clan != null`)
   - Line 768: `if (hero.Clan == Clan.PlayerClan && ...)` (missing `hero.Clan != null`)
   When the left operand is null or when `Campaign.Current` is null, C# evaluates the right-hand side `Clan.PlayerClan`.
4. **TaleWorlds Engine Internal Mechanics**:
   Inspection of `TaleWorlds.CampaignSystem.dll` MSIL confirms:
   - `Clan.get_PlayerClan`: `call Campaign.get_Current; callvirt Campaign.get_PlayerClan`
   - `Hero.get_MainHero`: `call Campaign.get_Current; callvirt Campaign.get_MainHero`
   - `Hero.get_AllAliveHeroes`: `call Campaign.get_Current; callvirt Campaign.get_AliveHeroes`
   - `Clan.get_All`: `call Campaign.get_Current; callvirt Campaign.get_Clans`
   - `CampaignTime.get_Now`: `call Campaign.get_Current; callvirt Campaign.get_MapTimeTracker; callvirt MapTimeTracker.get_Now`
   Because each of these static engine properties unconditionally dereferences `Campaign.Current`, querying them when `Campaign.Current == null` causes a fatal engine crash.
5. **Impact on Robustness**:
   `ShouldProcessInCurrentHour` and `ActiveTrackedHeroesCount` are public APIs declared in the behavior's contract. Calling either outside a live campaign tick will crash the entire mod assembly.

---

## 3. Caveats

- **Passed Boundary Conditions**:
  The following boundary conditions were stress-tested and confirmed robust:
  - Null victim and killer in `BeforeHeroKilled` and `OnHeroKilled` (PASS).
  - Null oldLeader and newLeader in `OnClanLeaderChanged` (PASS).
  - Null `HeroDeveloper` in `OnHeroGainedSkill`, `OnHeroLevelledUp`, `OnDailyTickHero`, and `EvaluateHeroProgression` (PASS).
  - Null `LeaderHero` on mobile parties during party ticks (PASS).
  - Null candidate in `DynasticSuccessionScore(Hero candidate)` (PASS).
- **Scope Limitation**:
  Per the strict **Review-only** constraint, Challenger 2 did not modify `ClanCharacterProgressionBehavior.cs`. The implementation fixes must be applied by Worker 1.

---

## 4. Conclusion & Required Changes

**Verdict**: **REQUEST_CHANGES**

Worker 1 must update `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs` with the following mitigations:

1. **Fix `ActiveTrackedHeroesCount` (Line 34)**:
   ```csharp
   public int ActiveTrackedHeroesCount => (Campaign.Current != null && Hero.AllAliveHeroes != null) ? Hero.AllAliveHeroes.Count : 0;
   ```

2. **Fix `ShouldProcessInCurrentHour` (Lines 156–161)**:
   ```csharp
   public static bool ShouldProcessInCurrentHour(string stringId)
   {
       if (string.IsNullOrEmpty(stringId) || Campaign.Current == null) return false;
       int entityHash = stringId.GetHashCode() & 0x7FFFFFFF;
       int currentHour = (int)CampaignTime.Now.ToHours % 24;
       return (entityHash % 24) == currentHour;
   }
   ```

3. **Fix `OnClanDestroyed`, `OnClanTierIncrease`, `OnClanChangedKingdom`, `OnClanDefected`, `OnRulingClanChanged` (Lines 343, 354, 426, 439, 452)**:
   ```csharp
   if (Campaign.Current != null && clan == Clan.PlayerClan)
   ```

4. **Fix `OnGovernorChanged` (Line 503)**:
   ```csharp
   if (newGovernor != null && town.OwnerClan != null && Campaign.Current != null && town.OwnerClan == Clan.PlayerClan)
   ```

5. **Fix `OnBeforeHeroesMarried` (Line 533)**:
   ```csharp
   if (Campaign.Current != null && ((firstHero.Clan != null && firstHero.Clan == Clan.PlayerClan) || (secondHero.Clan != null && secondHero.Clan == Clan.PlayerClan)))
   ```

6. **Fix `OnGivenBirth` (Line 566)**:
   ```csharp
   if (child != null && mother.Clan != null && Campaign.Current != null && mother.Clan == Clan.PlayerClan)
   ```

7. **Fix `OnHeroGainedSkill` (Line 586)**:
   ```csharp
   if (shouldNotify && hero.Clan != null && Campaign.Current != null && hero.Clan == Clan.PlayerClan)
   ```

8. **Fix `EvaluateHeroProgression` (Line 768)**:
   ```csharp
   if (hero.Clan != null && Campaign.Current != null && hero.Clan == Clan.PlayerClan && (unspentFocus > 0 || unspentAttr > 0))
   ```

9. **Fix `OnHourlyTick` (Line 702) & `OnWeeklyTick` (Line 737)**:
   Ensure `if (Campaign.Current == null) return;` is checked before querying `Hero.AllAliveHeroes` or `Clan.All`.

---

## 5. Verification Method

To independently reproduce and verify these findings:

1. Build the test suite:
   ```powershell
   dotnet build tests/CalradiaForge.Tests/CalradiaForge.Tests.csproj -c Release
   ```
2. Execute the test runner:
   ```powershell
   .\tests\CalradiaForge.Tests\bin\Release\net472\CalradiaForge.Tests.exe
   ```
3. Observe the 4 explicit test failures:
   - `FAIL ClanProgression: Empirical Stress - Empty and null aliveChildren in OnGivenBirth`
   - `FAIL ClanProgression: Empirical Stress - OnClanDestroyed null safety when Campaign inactive`
   - `FAIL ClanProgression: Empirical Stress - ShouldProcessInCurrentHour with non-empty string`
   - `FAIL ClanProgression: Empirical Stress - ActiveTrackedHeroesCount null safety when Campaign inactive`
