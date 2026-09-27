---
name: calradia-forge-architecture
description: Enforces Calradia Forge specific architectural patterns, assembly dependencies, and integration patterns across the modular Bannerlord mod system.
trigger: always_on
---

# Calradia Forge Architecture Rules

## 1. Assembly Dependency Hierarchy

### Required Dependency Order
```
TaleWorlds.* (Native Engine)
    ↓
CalradiaForge.Core (Framework Layer)
    ↓                    ↓
CalradiaForge.Mod   CalradiaForge.Sdk
    ↓                    ↓
CalradiaForge.Desktop (Companion Application)
```

### Forbidden Dependencies
- **CalradiaForge.Mod** must NOT reference CalradiaForge.Desktop
- **CalradiaForge.Sdk** must NOT reference CalradiaForge.Mod
- **CalradiaForge.Core** must NOT reference any other CalradiaForge assemblies
- **CalradiaForge.Desktop** must NOT reference TaleWorlds.* directly (only through Sdk)

### Enforcement
```csharp
// CalradiaForge.Mod.csproj - CORRECT
<ItemGroup>
  <Reference Include="TaleWorlds.CampaignSystem" />
  <Reference Include="CalradiaForge.Core" />
  <Reference Include="CalradiaForge.Sdk" />
</ItemGroup>

// CalradiaForge.Mod.csproj - FORBIDDEN
<ItemGroup>
  <Reference Include="CalradiaForge.Desktop" /> // ❌ Wrong direction
</ItemGroup>
```

## 2. SubModule Lifecycle Constraints

### Constructor Safety Rule
```csharp
// ✅ CORRECT - Zero entity access
public ClanCharacterProgressionBehavior()
{
    // Only initialize primitive counters
    _lifeCycleEventsProcessed = 0;
}

// ❌ WRONG - Entity access crashes on module load
public unsafeConstructor()
{
    var hero = Hero.MainHero; // Campaign.Current not ready!
}
```

### Registration Order
```csharp
protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
{
    if (gameStarterObject is CampaignGameStarter campaignStarter)
    {
        // 1. Auto-register all [AutoRegisterBehavior] classes first
        CalradiaForge.Core.CampaignExtensions.ForgeBehaviorLoader.RegisterAll(campaignStarter);
        
        // 2. Manual registration for specific behaviors
        campaignStarter.AddBehavior(new ClanCharacterProgressionBehavior());
    }
}
```

### Campaign Start Registration
```csharp
public override void OnCampaignStart(Game game, object starterObject)
{
    runtime?.NotifyCampaignStarted();
    if (starterObject is CampaignGameStarter campaignStarter)
    {
        // Register data behaviors that need full campaign state
        campaignStarter.AddBehavior(new DataBehavior());
    }
}
```

## 3. Namespace Architecture Rules

### Approved Namespace Patterns
```
CalradiaForge.Mod.CampaignBehaviors     // CampaignBehaviorBase implementations
CalradiaForge.Mod.Commands             // Console/commands
CalradiaForge.Core.CampaignExtensions  // Behavior registration utilities
CalradiaForge.Core.Diagnostics         // Logging systems
CalradiaForge.Core.SDK                 // SDK interfaces
CalradiaForge.Sdk.Builders             // Builder pattern implementations
CalradiaForge.Sdk.Services             // SDK service implementations
```

### Forbidden Namespace Patterns
```
CalradiaForge.Mod.Campaign             // ❌ Shadows TaleWorlds.CampaignSystem.Campaign
CalradiaForge.Mod.Localization         // ❌ Shadows TaleWorlds.Localization
CalradiaForge.Core.Campaign            // ❌ Shadows TaleWorlds.CampaignSystem.Campaign
```

### Enforcement
The `ModRuleAuditor.GEMINI_CAMPAIGN_SHADOWING` rule automatically detects:
- Folders named `Campaign`
- Namespaces ending in `.Campaign`
- Classes named `Campaign`

## 4. Auto-Registration Pattern

### Attribute Requirements
```csharp
[AutoRegisterBehavior]
public class CustomBehavior : CampaignBehaviorBase
{
    // Must have parameterless constructor
    public CustomBehavior() { }
}
```

### Assembly Scanning Rules
```csharp
// ForgeBehaviorLoader only scans assemblies that:
// 1. Reference CalradiaForge.Core
// 2. Are not dynamic assemblies
// 3. Contain CampaignBehaviorBase subclasses
// 4. Have [AutoRegisterBehavior] attribute
```

### Discovery Flow
```
AppDomain.GetAssemblies()
    ↓
Filter: References CalradiaForge.Core
    ↓
GetTypes()
    ↓
Filter: IsClass && !IsAbstract && IsSubclassOf(CampaignBehaviorBase)
    ↓
Filter: Has [AutoRegisterBehavior] attribute
    ↓
Activator.CreateInstance (parameterless ctor required)
    ↓
campaignStarter.AddBehavior
```

## 5. SDK Service Lifecycle

### Service Clearing Requirements
```csharp
protected override void OnSubModuleUnloaded()
{
    try
    {
        // Clear all SDK services in reverse initialization order
        CalradiaForge.Sdk.ForgeCampaignEvents.ClearSubscribers();
        CalradiaForge.Sdk.CampaignVariableInspector.ClearTrackedVariables();
        CalradiaForge.Sdk.CampaignVariableInspector.ClearSnapshotListeners();
        CalradiaForge.Sdk.ForgeData.ClearAll();
        CalradiaForge.Sdk.ForgeAgentMemory.ClearAll();
        CalradiaForge.Sdk.ForgeUI.Clear();
        CalradiaForge.Sdk.ForgeDetour.UnpatchAll();
    }
    finally
    {
        runtime?.Dispose();
        runtime = null;
    }
}
```

### Service Scope Rules
| Service | Scope | Clear Trigger |
|---------|-------|---------------|
| ForgeData | Campaign | OnSubModuleUnloaded |
| ForgeAgentMemory | Mission | OnMissionEnd |
| ForgeCampaignEvents | Global | OnSubModuleUnloaded |
| ForgeUI | Global | OnSubModuleUnloaded |
| ForgeDetour | Global | OnSubModuleUnloaded |

## 6. Runtime Bridge Pattern

### ITestServices Implementation
```csharp
public class Runtime : ITestServices
{
    // Bridge external tool calls to SDK services
    public void SetValue(string key, object value)
    {
        ForgeData.SetValue(key, value);
    }
    
    public object GetValue(string key, object defaultValue)
    {
        return ForgeData.GetValue(key, defaultValue);
    }
    
    public void NotifyAgentCreated(int agentIndex)
    {
        ForgeAgentMemory.RememberAgent(agentIndex, new AgentData(agentIndex));
    }
}
```

### Extension Startup
```csharp
// In ExtensionStartup.Connect
ExtensionStartup.Connect(TestEngine engine)
{
    var runtime = new Runtime();
    engine.RegisterService(runtime);
}
```

## 7. Crash Recovery Architecture

### Crash Dump Format
```json
{
  "Timestamp": "2026-09-20T15:30:45.1234567",
  "IsTerminating": true,
  "Exception": "System.NullReferenceException: Object reference not set..."
}
```

### Crash Handler Registration
```csharp
protected override void OnSubModuleLoad()
{
    AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
}

private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    if (e.ExceptionObject is Exception ex)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var path = Path.Combine(basePath, "Modules", "CalradiaForge", $"crash_{timestamp}.cfcrash");
        File.WriteAllText(path, JsonSerializer.Serialize(new CrashDump(ex)));
    }
}
```

### Crash File Naming
- Pattern: `crash_YYYYMMDD_HHMMSS.cfcrash`
- Location: `Modules/CalradiaForge/`
- Format: JSON with timestamp, termination flag, exception details

## 8. Gauntlet UI Architecture

### Panel Lifecycle
```csharp
void Open()
{
    // 1. Check if already open
    if (layer != null) return;
    
    // 2. Refresh language
    runtime.RefreshGameLanguage();
    
    // 3. Get active screen
    owner = ScreenManager.TopScreen;
    
    // 4. Create ViewModel and Layer
    vm = new PanelViewModel(runtime, Close);
    layer = new GauntletLayer("CalradiaForge", 500, true);
    
    // 5. Load and attach
    layer.UIContext.BrushFactory.LoadBrushFile("CalradiaForge");
    layer.LoadMovie("CalradiaForge", vm);
    layer.IsFocusLayer = true;
    owner.AddLayer(layer);
}

void Close()
{
    // 1. Detach references first (prevents reentry issues)
    var closingLayer = layer;
    var closingOwner = owner;
    var closingViewModel = vm;
    layer = null;
    owner = null;
    vm = null;
    
    // 2. Safe cleanup
    if (!closingLayer.IsFinalized && closingOwner != null)
    {
        closingLayer.InputRestrictions.ResetInputRestrictions();
        ScreenManager.TryLoseFocus(closingLayer);
        if (closingOwner.HasLayer(closingLayer))
            closingOwner.RemoveLayer(closingLayer);
    }
    
    // 3. Finalize ViewModel
    closingViewModel?.OnFinalize();
}
```

### Keyboard Navigation Rules
- Tab: Move focus forward/backward (with Shift)
- Enter: Execute focused button
- Escape: Close panel or toggle key help
- Ctrl+F: Focus argument field
- Ctrl+Enter: Refresh
- Ctrl+L: Clear output
- Ctrl+K: Toggle key help
- Ctrl+W: Toggle live watch
- Ctrl+D1-D9: Switch categories

## 9. Module Distribution Structure

### Required Directory Layout
```
modules/CalradiaForge/
├── SubModule.xml                  # Module manifest
├── bin/Win64_Shipping_Client/    # Compiled DLLs
│   ├── CalradiaForge.Mod.dll
│   ├── CalradiaForge.Core.dll
│   └── CalradiaForge.Sdk.dll
├── ModuleData/                    # XML data files (optional)
└── GUI/                           # Gauntlet UI prefabs (optional)
    └── Prefabs/
```

### SubModule.xml Requirements
```xml
<Module>
  <Id value="CalradiaForge" />
  <Name value="Calradia Forge" />
  <Version value="1.0.0" />
  <SingleplayerModule value="true" />
  <DependedModules>
    <DependedModule Id="Native" />
    <DependedModule Id="SandBoxCore" />
    <DependedModule Id="Sandbox" />
    <DependedModule Id="StoryMode" />
  </DependedModules>
  <SubModules>
    <SubModule>
      <Name value="CalradiaForge" />
      <DLLName value="CalradiaForge.Mod.dll" />
      <SubModuleClassType value="CalradiaForge.Mod.SubModule" />
    </SubModule>
  </SubModules>
</Module>
```

## 10. Verification Architecture

### Build Verification Steps
```powershell
# 1. Compilation
dotnet build CalradiaForge.sln -c Release

# 2. Statelessness check
tools/verify_stateless_behavior.ps1

# 3. Distribution safety
tools/validate_dlls.ps1
```

### Stateless Behavior Verification
```python
# tools/verify_stateless_behavior.py checks:
# 1. Release build compiles
# 2. Zero SaveableTypeDefiner inheritance
# 3. Empty SyncData in CampaignBehaviors
# 4. Zero "Campaign" folder/namespace/class names
# 5. SubModule.AddBehavior registration present
```

### Test Infrastructure
```
tests/CalradiaForge.Tests/
├── ClanCharacterProgressionTests.cs
│   ├── TestAntiShadowingRule
│   ├── TestSubModuleRegistration
│   └── TestStatelessBehavior
└── (Additional test suites)
```

## 11. ModRuleAuditor Integration

### Rule Enforcement
```csharp
// Runtime audit of third-party mods
var result = ModRuleAuditor.Audit(modDirectory);
if (!result.Passed)
{
    foreach (var finding in result.Findings)
    {
        ForgeLogger.PrintError($"[{finding.RuleId}] {finding.Description}");
    }
}
```

### Built-in Rules
| Rule ID | Severity | Check |
|---------|----------|-------|
| GEMINI_CAMPAIGN_SHADOWING | Error | Namespace/folder/class named "Campaign" |
| SAVEABLE_BASE_ID_COLLISION | Error | SaveableTypeDefiner base ID < 2,500,000 |
| QUEST_DOUBLE_SET_DIALOGS | Error | QuestBase missing double SetDialogs() |
| MISSION_MESH_ONINIT_DEFERRED | Warning | Mesh manipulation in OnInit() without guard |
| AUDIO_INVALID_CATEGORY | Error | Invalid sound category in XML |
| TROOP_NON_INTEGER_AGE | Error | Fractional age in NPCCharacter |
| GAUNTLET_WATERMARK_EVENT_BLOCK | Error | Watermark missing event blocking |

## 12. Configuration Management

### ForgeConfig Structure
```csharp
public class ForgeConfig
{
    public string Hotkey { get; set; } = "F10";
    public bool EnableLogging { get; set; } = true;
    public int LogVerbosity { get; set; } = 1;
}
```

### Settings Location
- Path: `Modules/CalradiaForge/settings.json`
- Auto-created on first run
- Language mirrors BannerlordConfig.Language

## 13. InternalsVisibleTo Pattern

### Test Assembly Access
```csharp
// At top of SubModule.cs
[assembly: InternalsVisibleTo("CalradiaForge.Tests")]

// Allows tests to access internal members
[Test]
public void TestInternalMethod()
{
    var behavior = new ClanCharacterProgressionBehavior();
    // Can access internal methods/members
}
```

## 14. Core Framework Components

### Critical Core Classes
| Class | Purpose | Assembly |
|-------|---------|----------|
| ForgeBehaviorLoader | Auto-registration of behaviors | Core |
| ModRuleAuditor | Static analysis validation | Core |
| ForgeLogger | Logging system | Core |
| ForgeConfig | Configuration management | Core |
| ForgeBootstrapper | Harmony patch initialization | Core |
| ForgeWeaveEngine | Patch management | Core |

### Core Extension Points
```csharp
// Custom behavior registration
[AutoRegisterBehavior]
public class CustomBehavior : CampaignBehaviorBase { }

// Custom rule audit
public class CustomRuleAuditor
{
    public static RuleAuditResult AuditCustom(string modDirectory)
    {
        // Custom validation logic
    }
}
```

## 15. Documentation Requirements

### Required Documentation Ecosystem
```
docs/
├── CODEMAP_ARCHITECTURE.md           # Overall architecture & assembly dependencies
├── CODEMAP_CAMPAIGN_BEHAVIORS.md     # Event catalog & anti-lag time-slicing
├── CODEMAP_SDK_GAMEMODELS.md         # SDK public contracts & decorator models
├── <TOPIC>.md / <TOPIC>.es.md        # Technical guides with strict bilingual parity
├── append/RevXXX-*.md                # Append-only Markdown annexes
├── CalradiaForge-Registro-Mejoras-Rev*.docx # Read-only protected Word documents
├── CalradiaForge-Registro-Mejoras.integrity.jsonl # Cryptographic SHA-256 hash chain
└── VALIDATION-<VERSION>.md / .es.md  # Empirical test run evidence & package hashes
```

### Specialized Documentation Skills & Tooling
- **`calradia-forge-codemaps`**: Audits and synchronizes `docs/CODEMAP_*.md` against source code.
- **`calradia-forge-registro-mejoras`**: Appends historical revisions via `tools/append_detailed_changelog_revision.py` and verifies `integrity.jsonl`.
- **`calradia-forge-docfx-pipeline`**: Builds DocFX static site (`tools/build_docs.ps1`) and generates in-game help (`tools/generate_in_game_help.py`).
- **`calradia-forge-release-validation`**: Documents empirical test suite metrics and package SHA-256 hashes.
- **Master Orchestrator**: [`calradia-forge-docs`](../skills/calradia-forge-docs/SKILL.md) coordinates the documentation specialists.

### Code Documentation Standards
```csharp
/// <summary>
/// [Brief description of class/method]
/// 
/// [Detailed explanation of purpose and behavior]
/// 
/// Lifecycle: [When created/destroyed]
/// Thread Safety: [Thread safety guarantees]
/// Performance: [Performance characteristics]
/// 
/// Example:
/// <code>
/// // Usage example
/// </code>
/// </summary>
```

## 16. Performance Architecture

### Anti-Lag Requirements
1. **Modulo-24 Time Slicing**: Required for bulk entity processing
2. **Zero GC Allocations**: Avoid LINQ in tick handlers
3. **Squared Distance**: Use DistanceSquared() instead of Math.Sqrt()
4. **Interlocked Counters**: Thread-safe telemetry without locks

### Performance Monitoring
```csharp
// SubModule tick timing
protected override void OnApplicationTick(float dt)
{
    var callbackStarted = Stopwatch.GetTimestamp();
    try
    {
        runtime.Tick(dt);
    }
    finally
    {
        runtime?.RecordCallbackTime(
            (Stopwatch.GetTimestamp() - callbackStarted) * 1000.0 / Stopwatch.Frequency
        );
    }
}
```

## 17. Security & Safety Rules

### Crash Recovery
- UnhandledException handler in OnSubModuleLoad
- JSON crash dumps with timestamps
- Graceful degradation on errors

### Initialization Safety
- Zero entity access in behavior constructors
- Session deferral for world scanning
- Null guards on all entity references
- Main thread only for Campaign APIs

### Memory Safety
- No static behavior references
- Non-serialized listeners only
- StringId for entity references
- Proper disposal in OnSubModuleUnloaded

## 18. Integration Testing Rules

### Integration Test Pattern
```csharp
[Test]
public void TestSubModuleRegistration()
{
    var submoduleContent = File.ReadAllText("src/CalradiaForge.Mod/SubModule.cs");
    Assert.IsTrue(submoduleContent.Contains("campaignStarter.AddBehavior"));
    Assert.IsTrue(submoduleContent.Contains("ClanCharacterProgressionBehavior"));
}

[Test]
public void TestAntiShadowingRule()
{
    var modDir = "src/CalradiaForge.Mod";
    var hasCampaignNamespace = Directory.GetFiles(modDir, "*.cs", SearchOption.AllDirectories)
        .Any(f => File.ReadAllText(f).Contains("namespace.*Campaign"));
    Assert.IsFalse(hasCampaignNamespace, "Found forbidden Campaign namespace");
}
```

## 19. Version Management

### Version Bumping
- Use `bump_version.py` for automated version updates
- Update SubModule.xml version
- Update AssemblyInfo.cs version
- Update CHANGELOG.md

### Version Format
```
Major.Minor.Patch (e.g., 1.0.0)
- Major: Breaking changes
- Minor: New features (backward compatible)
- Patch: Bug fixes (backward compatible)
```

## 20. Distribution Safety

### Script Exclusion
```
# tools/verify_stateless_behavior.ps1
# Exclude tools/ directory from distribution check
if (!scriptFile.Replace('\\', '/').Contains("/tools/"))
{
    // Warn about script files in distribution
}
```

### DLL Validation
```powershell
# tools/validate_dlls.ps1
# Verify correct DLLs in bin/ directory
# Check dependencies are present
# Validate version numbers
```

## 21. Desktop Workbench Architecture & Modernization

### TFM Separation & Modern C# Features
- **Framework Separation:** `src/CalradiaForge.Mod` and `src/CalradiaForge.Sdk` target `.NET Framework 4.7.2` (for game engine compatibility), while `src/CalradiaForge.Desktop` targets `.NET 8.0-windows`.
- **C# 12 Idioms in Desktop:** Use C# 12 primary constructors, collection expressions (`[]`), switch expressions, list patterns, and target-typed `new()` to keep WPF view models and services clean, readable, and low-allocation.
- **KISS & MVVM Boundaries:** Desktop services must never reference WPF controls (`System.Windows.Controls`). Always preserve separation between Presentation ViewModels and pure Services.

### Static Source-Contract Preservation
- **Source Inspection Tests:** The test suite (`tests/CalradiaForge.Desktop.Tests/Program.cs` and `DesktopAssemblyServiceTests.cs`) performs static assertions by reading raw `.cs` files with `File.ReadAllText`.
- **Inviolable Tokens & Phrases:** When refactoring, reducing code, or cleaning syntax, you MUST preserve all verbatim contract strings and tokens:
  - `!service.Contains("System.Windows")` (Ensures zero UI dependencies in DesktopWorkspaceService)
  - `"State-changing execution is unavailable from this guarded desktop route."`
  - `"Editable Calradia Forge starting point"`
  - `"not a game-wide performance attribution"`
  - `"desktop-preferences.json"`
  - `File.Move(temporary, path, true)` (Atomic file updates)
  - `MaximumMeasurements = 64` (Bounded telemetry queue)
  - `Take(128)` (Bounded evidence collection)
- **Compilation vs Assertion:** Code may compile with 0 errors, but removing or rewriting these exact tokens will break the Desktop test suite. Always run `tools/Run-CalradiaForge-Desktop-Tests.bat` after any refactor.

### C# Member Name Shadowing (`Path` vs `System.IO.Path`)
- **Shadowing Hazard:** If a class declares a property or member named `Path` (e.g., `DesktopPreferenceService.Path`), any unqualified call to `Path.Combine` resolves to `this.Path.Combine`, triggering compiler error CS0236 ("A field initializer cannot reference the non-static field, method, or property") or CS0118.
- **Enforcement:** Always explicitly qualify IO path operations as `System.IO.Path.Combine(...)` inside classes that declare a `Path` property or parameter.

### WPF `IMultiValueConverter.ConvertBack` Signature
- **Interface Contract:** In WPF (`System.Windows.Data`), `IMultiValueConverter.ConvertBack` requires the signature:
  ```csharp
  public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
  ```
- **CS0738 Prevention:** Returning `object` instead of `object[]` violates the interface implementation and results in CS0738.

## 22. ForgeWeave Event Mesh Integration

- ForgeWeave provides decoupled, additive event registration and dynamic Gauntlet UI discovery across modules.
- Complete rules for execution budgets, replay registries, fault isolation, and handler quarantine are defined in `.agents/rules/calradia_forge_forgeweave.md`.

## 23. TPAC Resource & Asset Validation

- TpacTool was removed after frequent reader errors. Do not reinstall it or use its legacy reader as an import, deployment, or release gate; parser failures do not establish TPAC corruption.
- For a Gauntlet UI deployment, preserve the imported Steam runtime TPAC, record and compare its SHA-256 before and after deployment, then verify the referenced sprites in the running game. Header or file-presence checks do not decode the texture payload or prove rendering.

## Summary of Critical Rules

1. **Assembly Hierarchy**: Respect dependency direction (Core → Mod/Sdk → Desktop)
2. **Constructor Safety**: Zero entity access in behavior constructors
3. **Anti-Shadowing**: Never use "Campaign" or "Localization" in names
4. **Auto-Registration**: Use [AutoRegisterBehavior] with parameterless ctor
5. **Service Lifecycle**: Clear SDK services in OnSubModuleUnloaded
6. **Runtime Bridge**: Implement ITestServices for external integration
7. **Crash Recovery**: Register UnhandledException handler
8. **Panel Lifecycle**: Detach references before cleanup
9. **Module Structure**: Follow standard Bannerlord module layout
10. **Verification**: Run verify_stateless_behavior.ps1/.py before commit
11. **ModRuleAuditor**: Use built-in rules for validation
12. **Configuration**: Store settings in modules/CalradiaForge/settings.json
13. **InternalsVisibleTo**: Enable test assembly access
14. **Performance**: Apply anti-lag patterns (modulo-24, no LINQ in ticks)
15. **Documentation**: Maintain CODEMAP_*.md files
16. **Security**: Implement crash recovery and initialization safety
17. **Testing**: Write integration tests for critical paths
18. **Version Management**: Use automated bump scripts
19. **Distribution Safety**: Exclude tools/ from distribution
20. **Statelessness**: Zero SaveableTypeDefiner in stateless behaviors
21. **.NET Routing & Simplicity**: Start with `calradia-forge-dotnet`, which contains the required TFM, routing, and KISS rules; use `bannerlord-dotnet-artisan` for game code. Installed general .NET guidance is optional, and the protected local `using-dotnet` snapshot is not a runtime dependency. Enforce TFM separation (`net472` module vs `net8.0-windows` Desktop).
22. **Desktop Source-Contract Invariants**: Preserve static reflection/inspection tokens verbatim in Desktop WPF services and view models
23. **Member Qualification (`System.IO.Path`)**: Explicitly qualify `System.IO.Path` when classes expose a `Path` property
24. **ForgeWeave Event Mesh Integrity**: Enforce bounded execution budgets, isolated replays, zero-allocation handler health tracking, and Gauntlet UI dynamic discovery
