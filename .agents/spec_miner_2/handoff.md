# Specification Handoff Report — Spec Miner 2: Architecture, Lifecycle, & Verification Specifications

**Date**: 2026-09-20  
**Agent**: Spec Miner 2  
**Target Module**: `CalradiaForge.Mod` (`src/CalradiaForge.Mod/`)  
**Assigned Scope**: Architectural rules, Engine Initialization Crash Constraint, Statelessness guarantees, SubModule registration pipeline, and Programmatic Acceptance Verification methods.

---

## 1. Observation

### 1.1 Project Architecture & Build System
- **Solution File**: `CalradiaForge.sln` at `c:\Users\Alex\Documents\Mod Desarrolladores\CalradiaForge.sln`.
- **Target Project**: `src/CalradiaForge.Mod/CalradiaForge.Mod.csproj` targets `net472` and references `$(GameBin)\TaleWorlds*.dll` and `CalradiaForge.Core`.
- **Build Verification**: Command `dotnet build CalradiaForge.sln -c Release` completed with exit code 0 (`0 Advertencia(s)`, `0 Errores`).
- **Existing Behaviors**: `src/CalradiaForge.Mod/CampaignBehaviors/DataBehavior.cs` is the only behavior currently residing in `CalradiaForge.Mod`. It inherits from `CampaignBehaviorBase` and implements `SyncData(IDataStore dataStore)`.
- **Existing SubModule**: `src/CalradiaForge.Mod/SubModule.cs` inherits from `MBSubModuleBase`.
  - In `OnGameStart(Game game, IGameStarter gameStarterObject)` (lines 56–64):
    ```csharp
    base.OnGameStart(game, gameStarterObject);
    if (gameStarterObject is TaleWorlds.CampaignSystem.CampaignGameStarter campaignStarter)
    {
        CalradiaForge.Core.CampaignExtensions.ForgeBehaviorLoader.RegisterAll(campaignStarter);
    }
    ```
  - In `OnCampaignStart(Game game, object starterObject)` (lines 66–73):
    ```csharp
    runtime?.NotifyCampaignStarted();
    if (starterObject is TaleWorlds.CampaignSystem.CampaignGameStarter campaignStarter)
    {
        campaignStarter.AddBehavior(new CalradiaForge.Mod.DataExtensions.DataBehavior());
    }
    ```

### 1.2 Anti-Shadowing Rule
- **Rule Source**: `GEMINI.md`:
  > "CRITICAL: Never name a folder, sub-namespace, or class `Campaign` within this project. Doing so shadows the `TaleWorlds.CampaignSystem.Campaign` class when `using TaleWorlds.CampaignSystem;` is active, breaking compilation for properties like `Campaign.Current`. Use alternatives like `CampaignBehaviors`, `DataExtensions`, or `CampaignExtensions`."
- **Additional Rule Sources**: `.agents/rules/bannerlord_architecture.md`, `.agents/rules/bannerlord_campaign_behavior.md`, `.agents/rules/bannerlord_clan_succession.md`.
- **Current Repository Status**: Zero folders or namespaces named `Campaign` exist in `src/CalradiaForge.Mod/`. Folder is `CampaignBehaviors/` and namespace is `CalradiaForge.Mod.CampaignBehaviors` (or `CalradiaForge.Mod.DataExtensions`).

### 1.3 Engine Initialization Crash Constraint
- **Rule Source**: `.agents/rules/bannerlord_mission_lifecycle.md` and `.agents/rules/bannerlord_campaign_behavior.md`.
- **Observed Lifecycle Rules**:
  - In 3D scenes / missions: Meshes, skeletons, and complex physics cannot be modified in `OnInit()`. Deferral to `OnTick` via `_initialized` boolean flag.
  - In Campaign simulation: During `MBSubModuleBase.OnGameStart` and `CampaignBehaviorBase.RegisterEvents()`, engine entity managers (`Hero.AllAliveHeroes`, `Clan.All`, `Settlement.All`, `MobileParty.All`, `MBObjectManager.Instance`) are not yet fully populated or stable. Querying or mutating them during registration causes low-level C++ access violation crashes or null reference exceptions.
  - Session activation event: `CampaignEvents.OnSessionLaunchedEvent` is the designated engine hook fired once all settlements, heroes, clans, and parties are loaded and ready.

### 1.4 Stateless Requirement & Save Safety
- **Rule Source**: `.agents/rules/bannerlord_save_system.md` and `ORIGINAL_REQUEST.md`.
- **Observed Constraints**:
  - Custom classes inheriting from `TaleWorlds.SaveSystem.SaveableTypeDefiner` inject custom assembly types into `.sav` binary stream headers. If the mod is removed or types are modified, saves crash.
  - The request mandates that the new behavior run statelessly without requiring custom save data serialization.
  - Scan of `src/CalradiaForge.Mod/` confirmed 0 classes currently inherit from `SaveableTypeDefiner`.

---

## 2. Logic Chain

### 2.1 Compiler Type Shadowing Mechanics
1. When a C# file has `using TaleWorlds.CampaignSystem;` and contains references to `Campaign.Current`, the C# Roslyn compiler resolves identifiers starting from the most local namespace.
2. If any parent namespace or sibling symbol matches `Campaign` (e.g., `namespace CalradiaForge.Mod.Campaign`), `Campaign` resolves to that namespace or class rather than `TaleWorlds.CampaignSystem.Campaign`.
3. This triggers compiler errors `CS0118` ("'Campaign' is a namespace but is used like a type") or `CS0234` ("'Current' does not exist in namespace 'CalradiaForge.Mod.Campaign'").
4. **Resolution**: All behavior classes must be placed in `src/CalradiaForge.Mod/CampaignBehaviors/` and declared within `namespace CalradiaForge.Mod.CampaignBehaviors`. The class name must never be `Campaign` (e.g. `ClanProgressionBehavior` or `ClanCharacterProgressionBehavior`).

### 2.2 Lifecycle & Engine Initialization Crash Prevention
1. In Bannerlord, `MBSubModuleBase.OnGameStart(Game game, IGameStarter gameStarterObject)` executes while the campaign game state is being constructed.
2. The engine immediately invokes `CampaignBehaviorBase.RegisterEvents()` on all registered behaviors.
3. If `RegisterEvents()` attempts to inspect or mutate heroes (`Hero.AllAliveHeroes`), clans (`Clan.All`), or spawn parties, the underlying TaleWorlds collections may be null or mid-deserialization.
4. **Deferral Pattern**:
   - `RegisterEvents()` must only attach event listeners using `CampaignEvents.[EventName].AddNonSerializedListener(this, ...)`.
   - Any initial world scan or session setup must be hooked to `CampaignEvents.OnSessionLaunchedEvent`.
   - Periodic evaluations must hook into `CampaignEvents.DailyTickHero`, `DailyTickClan`, or `HourlyTick` and verify `hero != null && hero.IsActive`.
   - To prevent stutter during daily pulses ("Midnight Freeze"), hero processing must use hash-modulo slicing across hours: `(hero.StringId.GetHashCode() & 0x7FFFFFFF) % 24 == currentHour`.

### 2.3 Stateless Architecture Guarantee
1. To ensure 100% save compatibility and zero uninstallation risk, the behavior must persist zero custom fields in save files.
2. `SaveableTypeDefiner`: The assembly must contain zero subclasses of `SaveableTypeDefiner`.
3. `SyncData(IDataStore dataStore)`: The override must be a no-op:
   ```csharp
   public override void SyncData(IDataStore dataStore)
   {
       // Deliberately empty: This behavior is 100% stateless.
       // All metrics and decisions derive dynamically from vanilla engine state.
   }
   ```
4. Deriving progression statelessly:
   - Level & XP: `hero.Level`, `hero.HeroDeveloper.TotalXp`, `hero.HeroDeveloper.UnspentFocusPoints`, `hero.HeroDeveloper.UnspentAttributePoints`.
   - Skills & Perks: `hero.GetSkillValue(...)`, `hero.HeroDeveloper.GetPerkValue(...)`.
   - Clan state: `clan.Tier`, `clan.Renown`, `clan.Influence`, `clan.Leader`, `clan.Lords`, `clan.Companions`.
   - Succession & Family: `clan.Leader`, `hero.Spouse`, `hero.Children`, `hero.Father`, `hero.Mother`.
   - When an event triggers (e.g. `HeroLevelledUp`, `HeroGainedSkill`, `ClanTierIncrease`), the behavior applies vanilla mutations via TaleWorlds actions (e.g. `GainRenownAction.Apply`, `ChangeClanLeaderAction.Apply`, `heroDeveloper.AddSkillXp`). These actions mutate vanilla fields that TaleWorlds natively saves. The mod behavior itself holds and saves zero state.

### 2.4 SubModule Registration Architecture
1. Bannerlord provides two hooks in `MBSubModuleBase`: `OnGameStart(Game game, IGameStarter gameStarterObject)` and `OnCampaignStart(Game game, object starterObject)`.
2. `OnCampaignStart` is only called when launching a brand new campaign. It is bypassed when loading existing save files.
3. `OnGameStart` is called in both new campaign creation and existing save game loads.
4. Casting `gameStarterObject` via `if (gameStarterObject is CampaignGameStarter campaignStarter)` ensures execution only occurs in campaign modes (and safely ignores custom battles).
5. Calling `campaignStarter.AddBehavior(new CalradiaForge.Mod.CampaignBehaviors.ClanProgressionBehavior())` inside `SubModule.OnGameStart` ensures registration in all campaign sessions.
6. The test script can inspect `SubModule.cs` and verify `campaignStarter.AddBehavior(...)` is invoked inside `OnGameStart`.

---

## 3. Caveats

1. **Vanilla Entity Mutation vs Mod State**: Statelessness applies to the mod's behavior class. When the behavior calls TaleWorlds methods (e.g., `hero.HeroDeveloper.AddSkillXp` or `clan.AddRenown`), it modifies native Bannerlord entities. These native modifications are saved by TaleWorlds' own save system, which is safe and expected.
2. **Event Signature Compatibility**: TaleWorlds `CampaignEvents` delegate signatures vary slightly across minor game versions (e.g., e1.7.x vs v1.0.x vs v1.2.x). Delegate parameters must match the assemblies referenced in `CalradiaForge.Mod.csproj` (`$(GameBin)\TaleWorlds*.dll`).
3. **Threading**: TaleWorlds Campaign System is strictly single-threaded. Never dispatch campaign event reactions or entity lookups onto background threads (`Task.Run`).

---

## 4. Conclusion

The Stateless Clan & Character CampaignBehavior can be cleanly implemented and validated under the following exact architecture:
1. **Location & Namespace**: `src/CalradiaForge.Mod/CampaignBehaviors/ClanProgressionBehavior.cs` in `namespace CalradiaForge.Mod.CampaignBehaviors`.
2. **Anti-Shadowing**: Strictly avoid the identifier `Campaign` for any folder, namespace, or class.
3. **Engine Crash Guard**: Zero entity lookups or logic in constructor or `RegisterEvents()`. All subscriptions must use `CampaignEvents.[Event].AddNonSerializedListener(this, ...)`.
4. **Zero Save State**: Zero `SaveableTypeDefiner` classes and an empty `SyncData(IDataStore dataStore)` method.
5. **Registration**: Explicitly registered via `campaignStarter.AddBehavior(new ClanProgressionBehavior())` inside `SubModule.OnGameStart`.
6. **Programmatic Verification**: Automated via Python and PowerShell verification scripts testing compilation, AST/regex statelessness, anti-shadowing, and registration.

---

## 5. Verification Method

### 5.1 Programmatic Acceptance Verification Specifications

The acceptance criteria require programmatic scripts to verify:
1. `CalradiaForge.sln` compiles successfully in Release mode.
2. No classes inherit from `SaveableTypeDefiner` and `SyncData` contains no synced fields.
3. SubModule registers the new behavior via `AddBehavior()` in `OnGameStart`.
4. Anti-shadowing compliance (no folder, namespace, or class named `Campaign`).

#### Python Verification Script Specification (`tools/verify_stateless_behavior.py`)

```python
#!/usr/bin/env python3
"""
Stateless CampaignBehavior & Architecture Acceptance Verifier
Validates compilation, statelessness, anti-shadowing, and SubModule registration.
"""
import os
import re
import subprocess
import sys

def check_compilation():
    print("[1/4] Verifying CalradiaForge.sln compiles in Release mode...")
    cmd = ["dotnet", "build", "CalradiaForge.sln", "-c", "Release", "-v:minimal"]
    res = subprocess.run(cmd, capture_output=True, text=True)
    if res.returncode != 0:
        print(f"FAILED: Compilation error:\n{res.stdout}\n{res.stderr}")
        return False
    print("  ✓ CalradiaForge.sln compiled successfully with 0 errors.")
    return True

def check_statelessness(mod_dir):
    print("[2/4] Verifying statelessness: 0 SaveableTypeDefiner & 0 SyncData fields...")
    # 1. No classes inherit from SaveableTypeDefiner
    violations = []
    for root, _, files in os.walk(mod_dir):
        for f in files:
            if f.endswith(".cs"):
                p = os.path.join(root, f)
                with open(p, "r", encoding="utf-8", errors="ignore") as src:
                    for line_no, line in enumerate(src, 1):
                        if re.search(r":\s*SaveableTypeDefiner\b", line):
                            violations.append(f"{p}:{line_no} inherits from SaveableTypeDefiner")
    if violations:
        print(f"FAILED: Found SaveableTypeDefiner inheritance:\n" + "\n".join(violations))
        return False

    # 2. Check SyncData in CampaignBehaviors (excluding legacy DataBehavior.cs)
    cb_dir = os.path.join(mod_dir, "CampaignBehaviors")
    for root, _, files in os.walk(cb_dir):
        for f in files:
            if f.endswith(".cs") and f != "DataBehavior.cs":
                p = os.path.join(root, f)
                with open(p, "r", encoding="utf-8", errors="ignore") as src:
                    content = src.read()
                    # Find SyncData method
                    match = re.search(r"public\s+override\s+void\s+SyncData\s*\(\s*IDataStore\s+(\w+)\s*\)\s*\{([^}]*)\}", content)
                    if not match:
                        print(f"FAILED: Could not find SyncData(IDataStore) override in {p}")
                        return False
                    body = match.group(2)
                    param_name = match.group(1)
                    if re.search(rf"\b{param_name}\.SyncData\b", body):
                        print(f"FAILED: SyncData in {p} calls dataStore.SyncData: {body.strip()}")
                        return False
                    if re.search(r"\[\s*Saveable(Field|Property)", content):
                        print(f"FAILED: Found [SaveableField] or [SaveableProperty] in {p}")
                        return False
    print("  ✓ Zero SaveableTypeDefiner and zero SyncData serialization confirmed.")
    return True

def check_anti_shadowing(mod_dir):
    print("[3/4] Verifying GEMINI.md Anti-Shadowing constraints...")
    # Check forbidden folder names
    for root, dirs, _ in os.walk(mod_dir):
        for d in dirs:
            if d.lower() == "campaign":
                print(f"FAILED: Forbidden folder named '{d}' found at {os.path.join(root, d)}")
                return False

    # Check forbidden namespaces and class names
    for root, _, files in os.walk(mod_dir):
        for f in files:
            if f.endswith(".cs"):
                p = os.path.join(root, f)
                with open(p, "r", encoding="utf-8", errors="ignore") as src:
                    for line_no, line in enumerate(src, 1):
                        if re.search(r"namespace\s+.*\.Campaign(\s*;|\s*\{|$)", line) or re.search(r"namespace\s+Campaign(\s*;|\s*\{|$)", line):
                            print(f"FAILED: Forbidden namespace ending in 'Campaign' at {p}:{line_no}")
                            return False
                        if re.search(r"\bclass\s+Campaign\b", line):
                            print(f"FAILED: Forbidden class named 'Campaign' at {p}:{line_no}")
                            return False
    print("  ✓ Anti-shadowing verified: no folder, namespace, or class named 'Campaign'.")
    return True

def check_submodule_registration(mod_dir):
    print("[4/4] Verifying MBSubModuleBase.OnGameStart registration...")
    submodule_path = os.path.join(mod_dir, "SubModule.cs")
    if not os.path.exists(submodule_path):
        print(f"FAILED: SubModule.cs not found at {submodule_path}")
        return False

    with open(submodule_path, "r", encoding="utf-8") as f:
        content = f.read()

    match = re.search(r"protected\s+override\s+void\s+OnGameStart\s*\([^)]*\)\s*\{([\s\S]*?)\n\s*\}", content)
    if not match:
        print("FAILED: OnGameStart method not found in SubModule.cs")
        return False

    body = match.group(1)
    if not re.search(r"campaignStarter\.AddBehavior\s*\(\s*new\s+[\w\.]*Behavior\s*\(\s*\)\s*\);", body):
        print(f"FAILED: campaignStarter.AddBehavior(...) not found in SubModule.OnGameStart:\n{body}")
        return False

    print("  ✓ SubModule properly registers CampaignBehavior in OnGameStart via AddBehavior().")
    return True

def main():
    workspace = os.path.abspath(os.path.dirname(__file__) + "/..")
    mod_dir = os.path.join(workspace, "src", "CalradiaForge.Mod")
    
    success = (
        check_compilation() and
        check_statelessness(mod_dir) and
        check_anti_shadowing(mod_dir) and
        check_submodule_registration(mod_dir)
    )
    if success:
        print("\nSUCCESS: All architectural and acceptance criteria passed!")
        sys.exit(0)
    else:
        print("\nFAILURE: One or more acceptance criteria failed.")
        sys.exit(1)

if __name__ == "__main__":
    main()
```

#### PowerShell Verification Script Specification (`tools/verify_stateless_behavior.ps1`)

```powershell
param(
    [string]$Workspace = (Get-Location).Path
)
$ErrorActionPreference = 'Stop'
Write-Host "=== Programmatic Acceptance Verification ===" -ForegroundColor Cyan

# 1. Compilation Verification
Write-Host "[1/4] Checking Release build of CalradiaForge.sln..." -ForegroundColor Yellow
dotnet build CalradiaForge.sln -c Release -v:minimal
if ($LASTEXITCODE -ne 0) {
    Write-Error "Release build failed."
    exit 1
}
Write-Host "  ✓ Build successful." -ForegroundColor Green

# 2. Statelessness Check
Write-Host "[2/4] Checking for zero SaveableTypeDefiner and stateless SyncData..." -ForegroundColor Yellow
$modDir = Join-Path $Workspace "src/CalradiaForge.Mod"
$saveableTypes = Get-ChildItem -Path $modDir -Recurse -Filter *.cs | Select-String -Pattern ':\s*SaveableTypeDefiner\b'
if ($saveableTypes) {
    Write-Error "Found SaveableTypeDefiner inheritance: $($saveableTypes | Out-String)"
    exit 1
}

$behaviorFiles = Get-ChildItem -Path (Join-Path $modDir "CampaignBehaviors") -Filter *.cs | Where-Object { $_.Name -ne "DataBehavior.cs" }
foreach ($file in $behaviorFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match 'public\s+override\s+void\s+SyncData\s*\(\s*IDataStore\s+(\w+)\s*\)\s*\{([^}]*)\}') {
        $paramName = $Matches[1]
        $body = $Matches[2]
        if ($body -match "$paramName\.SyncData") {
            Write-Error "SyncData in $($file.Name) contains dataStore.SyncData call: $body"
            exit 1
        }
    } else {
        Write-Error "SyncData override missing in $($file.Name)"
        exit 1
    }
    if ($content -match '\[\s*Saveable(Field|Property)') {
        Write-Error "Found SaveableField or SaveableProperty in $($file.Name)"
        exit 1
    }
}
Write-Host "  ✓ Zero SaveableTypeDefiner and zero SyncData fields verified." -ForegroundColor Green

# 3. Anti-Shadowing Check
Write-Host "[3/4] Checking Anti-Shadowing (GEMINI.md)..." -ForegroundColor Yellow
$badFolders = Get-ChildItem -Path $modDir -Recurse -Directory | Where-Object { $_.Name -eq "Campaign" }
if ($badFolders) {
    Write-Error "Forbidden folder 'Campaign' found: $($badFolders.FullName)"
    exit 1
}
$badNamespaces = Get-ChildItem -Path $modDir -Recurse -Filter *.cs | Select-String -Pattern 'namespace\s+.*\.Campaign(\s*;|\s*\{|$)'
if ($badNamespaces) {
    Write-Error "Forbidden namespace ending in Campaign found: $($badNamespaces | Out-String)"
    exit 1
}
Write-Host "  ✓ Anti-shadowing verified." -ForegroundColor Green

# 4. SubModule Registration Check
Write-Host "[4/4] Checking SubModule.OnGameStart registration..." -ForegroundColor Yellow
$subModuleFile = Join-Path $modDir "SubModule.cs"
$subContent = Get-Content $subModuleFile -Raw
if ($subContent -match 'protected\s+override\s+void\s+OnGameStart\s*\([^)]*\)\s*\{([\s\S]*?)\n\s*\}') {
    $methodBody = $Matches[1]
    if ($methodBody -notmatch 'campaignStarter\.AddBehavior\s*\(\s*new\s+[\w\.]*Behavior\s*\(\s*\)\s*\);') {
        Write-Error "campaignStarter.AddBehavior(...) call not found in SubModule.OnGameStart"
        exit 1
    }
} else {
    Write-Error "OnGameStart not found in SubModule.cs"
    exit 1
}
Write-Host "  ✓ SubModule properly registers CampaignBehavior in OnGameStart." -ForegroundColor Green

Write-Host "`nAll acceptance criteria verified successfully!" -ForegroundColor Cyan
```

---

## Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Architecture | Anti-Shadowing Naming Rule | Prohibits naming any folder, sub-namespace, or class `Campaign` to prevent compiler symbol collision with `TaleWorlds.CampaignSystem.Campaign`. | Folder names, namespace declarations, class declarations | Clean compilation of `Campaign.Current` | Compiler error `CS0118` or `CS0234` | `GEMINI.md`, `bannerlord_architecture.md` |
| 2 | Lifecycle | Declarative `RegisterEvents()` | `RegisterEvents()` is solely for attaching non-serialized listeners; zero domain logic or entity querying permitted during registration. | `CampaignEvents.*.AddNonSerializedListener(this, delegate)` | Registered delegate handles in `MbEvent` | N/A (memory safe) | `bannerlord_campaign_behavior.md` |
| 3 | Lifecycle | Session Launch Deferral Hook | `CampaignEvents.OnSessionLaunchedEvent` fires when world map, settlements, heroes, and parties are fully constructed and safe to inspect. | `Action<CampaignGameStarter>` delegate | Callback invoked after entity initialization completes | Prevents engine startup null pointers and access violations | `bannerlord_campaign_behavior.md`, `bannerlord_mission_lifecycle.md` |
| 4 | Performance | Modulo Bucket Time-Slicing | Throttles daily/hourly hero evaluation across 24 campaign hours using hash modulus to prevent the "Midnight Freeze" stutter. | `(hero.StringId.GetHashCode() & 0x7FFFFFFF) % 24 == currentHour` | 1/24th hero workload processed per hour | Throttling eliminates UI freeze | `bannerlord_campaign_behavior.md` |
| 5 | Save System | Stateless `SyncData` Pattern | Guarantees zero mod save footprint by keeping `SyncData(IDataStore dataStore)` completely empty. | `IDataStore dataStore` | `void` (no `dataStore.SyncData()` calls) | Completely eliminates save corruption and uninstallation crashes | `bannerlord_save_system.md`, `ORIGINAL_REQUEST.md` |
| 6 | Save System | Zero `SaveableTypeDefiner` | Omits all `SaveableTypeDefiner` inheritance and `[SaveableField]` decorations, preventing custom type registration in TaleWorlds binary headers. | Source code classes | No custom types registered with engine serializer | Eliminates binary assembly deserialization errors on mod removal | `bannerlord_save_system.md`, `ORIGINAL_REQUEST.md` |
| 7 | Lifecycle | `MBSubModuleBase.OnGameStart` Registration | Registers the campaign behavior on game startup for both new campaign games and loaded save games via `CampaignGameStarter.AddBehavior`. | `Game game, IGameStarter gameStarterObject` | `campaignStarter.AddBehavior(new Behavior())` | Custom battles safely ignored via type guard (`gameStarterObject is CampaignGameStarter`) | `bannerlord_architecture.md`, `SubModule.cs` |
| 8 | Progression | Vanilla State Derivation | Derives hero progression and clan status directly from vanilla properties (`HeroDeveloper`, `Clan.Tier`, `clan.Renown`) rather than custom mod dictionaries. | Vanilla engine objects (`Hero`, `Clan`) | Computed thresholds and vanilla actions (`GainRenownAction`, `AddSkillXp`) | Null-coalescing guards (`hero != null && hero.IsActive`) prevent crashes on deceased heroes | `bannerlord_character_development.md`, `bannerlord_clan_succession.md` |
| 9 | Verification | Automated Acceptance Scripting | Programmatic Python & PowerShell verification checking Release build, AST/regex statelessness, anti-shadowing, and SubModule registration. | Source code files, solution file | Exit code 0 (pass) or 1 (fail) with diagnostic log | Pinpoints exact file and line of any architectural violation | `tools/build.ps1`, `CalradiaForge.Tests` |

---

## Edge Cases

| # | Feature | Input | Observed Behavior |
|---|---------|-------|-------------------|
| 1 | Anti-Shadowing | Namespace declared as `CalradiaForge.Mod.Campaign` | Compiler attempts to resolve `Campaign.Current` as `CalradiaForge.Mod.Campaign.Current`, failing with CS0118/CS0234. |
| 2 | Anti-Shadowing | Folder named `src/CalradiaForge.Mod/Campaign/` | IDE / Roslyn auto-generates namespace `CalradiaForge.Mod.Campaign`, re-introducing the collision even if not intended. |
| 3 | Engine Lifecycle | Querying `Hero.AllAliveHeroes` inside `RegisterEvents()` | Collections may be unallocated or mid-hydration; causes `NullReferenceException` or native crash during game load. |
| 4 | Engine Lifecycle | Spawning companion or party in `OnGameStart()` | Game world map and party lists are not yet constructed; crashes engine with native memory fault. |
| 5 | Statelessness | Saving custom `Dictionary<string, int>` in `SyncData` | Injects type into save stream; if user later removes the mod, loading the save throws binary deserialization crash. |
| 6 | SubModule Registration | Registering behavior in `OnCampaignStart` only | Behavior is registered when clicking "New Campaign", but NEVER registered when clicking "Saved Game / Load Campaign". |
| 7 | SubModule Registration | Running Custom Battle or Multiplayer | `gameStarterObject` is `BasicGameStarter`, not `CampaignGameStarter`. Type-check gracefully skips campaign registration without error. |
| 8 | Anti-Lag / Ticks | Iterating all heroes in `CampaignEvents.DailyTick` without filter | Synchronous iteration over 1,500+ heroes causes frame drop ("Midnight Freeze"). Must use modulo hour slicing. |
| 9 | Hero Reference Safety | Hero dies or is executed mid-campaign | Hero state transitions to dead; accessing active party roles or developers without `hero.IsActive && hero.IsAlive` guard can throw null references. |
