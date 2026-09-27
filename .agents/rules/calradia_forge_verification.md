---
name: calradia-forge-verification
description: Defines verification, testing, and quality assurance patterns specific to Calradia Forge mod development including build automation, static analysis, and acceptance testing.
trigger: always_on
---

# Calradia Forge Verification & Testing Rules

## 1. Build Verification Pipeline

### Release Build Requirement
```powershell
# All verification requires Release build
dotnet build CalradiaForge.sln -c Release -v:minimal

# Exit on compilation errors
if ($LASTEXITCODE -ne 0) {
    Write-Error "Release build failed"
    exit 1
}
```

### Build Configuration
- **Configuration**: Release only (no Debug verification)
- **Verbosity**: Minimal (for clean output)
- **Target Framework**: .NET Framework 4.7.2
- **Output**: bin/Release/net472/

### Build Success Criteria
- Zero compilation errors
- Zero compilation warnings (preferred)
- All DLLs generated in correct locations
- Assembly versions match SubModule.xml

## 2. Stateless Behavior Verification

### Verification Script Requirements
```powershell
# tools/verify_stateless_behavior.ps1 checks:
1. Compilation (Release build)
2. Statelessness (0 SaveableTypeDefiner, empty SyncData)
3. Anti-shadowing (0 "Campaign" names)
4. SubModule registration (AddBehavior present)
```

### Statelessness Checks

#### SaveableTypeDefiner Check
```powershell
# Scan all .cs files in src/CalradiaForge.Mod
$saveableTypes = Get-ChildItem -Path $modDir -Recurse -Filter *.cs | 
    Select-String -Pattern ':\s*SaveableTypeDefiner\b'

# Must be zero violations
if ($saveableTypes) {
    Write-Error "Found SaveableTypeDefiner inheritance"
    exit 1
}
```

#### SyncData Check
```powershell
# Check CampaignBehaviors (excluding DataBehavior.cs)
$behaviorFiles = Get-ChildItem -Path $cbDir -Filter *.cs | 
    Where-Object { $_.Name -ne "DataBehavior.cs" }

foreach ($file in $behaviorFiles) {
    $content = Get-Content $file.FullName -Raw
    
    # Must have SyncData override
    if ($content -match 'public\s+override\s+void\s+SyncData') {
        $paramName = $Matches[1]
        $body = $Matches[2]
        
        # Must NOT call dataStore.SyncData
        if ($body -match "$paramName\s*\.\s*SyncData\b") {
            Write-Error "SyncData contains dataStore.SyncData call"
            exit 1
        }
    }
    
    # Must NOT have [SaveableField] or [SaveableProperty]
    if ($content -match '\[\s*Saveable(Field|Property)') {
        Write-Error "Found [SaveableField] or [SaveableProperty]"
        exit 1
    }
}
```

### Anti-Shadowing Checks

#### Folder Check
```powershell
# Check for forbidden "Campaign" folder
$badFolders = Get-ChildItem -Path $modDir -Recurse -Directory | 
    Where-Object { $_.Name -eq "Campaign" }

if ($badFolders) {
    Write-Error "Forbidden folder 'Campaign' found"
    exit 1
}
```

#### Namespace Check
```powershell
# Check for forbidden namespace ending in "Campaign"
$badNamespaces = Get-ChildItem -Path $modDir -Recurse -Filter *.cs | 
    Select-String -Pattern '\bnamespace\s+([A-Za-z0-9_\.]*\.)?Campaign\b(\s*;|\s*\{|$)'

if ($badNamespaces) {
    Write-Error "Forbidden namespace ending in Campaign found"
    exit 1
}
```

#### Class Name Check
```powershell
# Check for forbidden class named "Campaign"
$badClasses = Get-ChildItem -Path $modDir -Recurse -Filter *.cs | 
    Select-String -Pattern '\bclass\s+Campaign\b'

if ($badClasses) {
    Write-Error "Forbidden class named 'Campaign' found"
    exit 1
}
```

### SubModule Registration Check
```powershell
# Check SubModule.cs for AddBehavior call
$subContent = Get-Content $subModuleFile -Raw

if ($subContent -match 'protected\s+override\s+void\s+OnGameStart') {
    $methodBody = $Matches[1]
    
    # Must have campaignStarter.AddBehavior call
    $hasAddBehavior = ($methodBody -match 'campaignStarter\.AddBehavior') -or
                      ($methodBody -match 'AddBehavior')
    
    if (-not $hasAddBehavior) {
        Write-Error "campaignStarter.AddBehavior not found in OnGameStart"
        exit 1
    }
}
```

## 3. Cross-Platform Verification

### Python Verification Script
```python
#!/usr/bin/env python3
# tools/verify_stateless_behavior.py

# Same 4 checks as PowerShell but cross-platform
def check_compilation(workspace):
    cmd = ["dotnet", "build", "CalradiaForge.sln", "-c", "Release", "-v:minimal"]
    res = subprocess.run(cmd, cwd=workspace, capture_output=True, text=True)
    return res.returncode == 0

def check_statelessness(mod_dir):
    # Scan for SaveableTypeDefiner
    # Check SyncData methods
    # Check for [SaveableField] attributes
    pass

def check_anti_shadowing(mod_dir):
    # Check folders, namespaces, classes
    pass

def check_submodule_registration(mod_dir):
    # Check AddBehavior in OnGameStart
    pass
```

### Platform Compatibility
- **Windows**: Use `verify_stateless_behavior.ps1`
- **Linux/Mac**: Use `verify_stateless_behavior.py`
- **CI/CD**: Prefer Python for cross-platform consistency

## 4. Unit Testing Architecture

### Test Structure
```
tests/CalradiaForge.Tests/
├── ClanCharacterProgressionTests.cs
├── ArchitectureTests.cs
├── ModRuleAuditorTests.cs
└── (Additional test suites)
```

### Test Categories

#### Anti-Shadowing Tests
```csharp
[Test]
public void TestAntiShadowingRule()
{
    var modDir = "src/CalradiaForge.Mod";
    var csFiles = Directory.GetFiles(modDir, "*.cs", SearchOption.AllDirectories);
    
    foreach (var file in csFiles)
    {
        var content = File.ReadAllText(file);
        
        // Check for forbidden namespace
        Assert.IsFalse(
            Regex.IsMatch(content, @"\bnamespace\s+([A-Za-z0-9_\.]*\.)?Campaign\b"),
            $"Found forbidden Campaign namespace in {Path.GetFileName(file)}"
        );
        
        // Check for forbidden class
        Assert.IsFalse(
            Regex.IsMatch(content, @"\bclass\s+Campaign\b"),
            $"Found forbidden class named Campaign in {Path.GetFileName(file)}"
        );
    }
}
```

#### SubModule Registration Tests
```csharp
[Test]
public void TestSubModuleRegistration()
{
    var submodulePath = "src/CalradiaForge.Mod/SubModule.cs";
    var content = File.ReadAllText(submodulePath);
    
    // Check for AddBehavior call
    Assert.IsTrue(
        content.Contains("campaignStarter.AddBehavior"),
        "SubModule.OnGameStart must call campaignStarter.AddBehavior"
    );
    
    // Check for specific behavior registration
    Assert.IsTrue(
        content.Contains("ClanCharacterProgressionBehavior"),
        "SubModule must register ClanCharacterProgressionBehavior"
    );
}
```

#### Statelessness Tests
```csharp
[Test]
public void TestStatelessBehavior()
{
    var behaviorDir = "src/CalradiaForge.Mod/CampaignBehaviors";
    var behaviorFiles = Directory.GetFiles(behaviorDir, "*.cs")
        .Where(f => !f.EndsWith("DataBehavior.cs"));
    
    foreach (var file in behaviorFiles)
    {
        var content = File.ReadAllText(file);
        
        // Check for SaveableTypeDefiner inheritance
        Assert.IsFalse(
            Regex.IsMatch(content, @":\s*SaveableTypeDefiner\b"),
            $"{Path.GetFileName(file)} must not inherit from SaveableTypeDefiner"
        );
        
        // Check for empty SyncData
        var syncDataMatch = Regex.Match(
            content, 
            @"public\s+override\s+void\s+SyncData\s*\([^)]*\)\s*\{([^}]*)\}"
        );
        
        if (syncDataMatch.Success)
        {
            var body = syncDataMatch.Groups[1].Value.Trim();
            Assert.IsTrue(
                string.IsNullOrWhiteSpace(body) || body == "// Stateless",
                $"{Path.GetFileName(file)} SyncData must be empty for stateless behavior"
            );
        }
    }
}
```

#### ModRuleAuditor Tests
```csharp
[Test]
public void TestModRuleAuditorCampaignShadowing()
{
    var testDir = CreateTestDirectory();
    CreateTestFile(testDir, "Test.cs", "namespace Test.Campaign { }");
    
    var result = ModRuleAuditor.Audit(testDir);
    
    Assert.IsFalse(result.Passed);
    Assert.IsTrue(
        result.Findings.Any(f => f.RuleId == "GEMINI_CAMPAIGN_SHADOWING"),
        "Should detect Campaign namespace shadowing"
    );
}
```

## 5. Integration Testing

### Integration Test Pattern
```csharp
[TestFixture]
public class IntegrationTests
{
    private string _testWorkspace;
    
    [SetUp]
    public void Setup()
    {
        _testWorkspace = CreateTestWorkspace();
    }
    
    [TearDown]
    public void TearDown()
    {
        CleanupTestWorkspace(_testWorkspace);
    }
    
    [Test]
    public void TestFullBuildPipeline()
    {
        // 1. Build solution
        var buildResult = RunDotnetBuild(_testWorkspace);
        Assert.IsTrue(buildResult.Success, "Build should succeed");
        
        // 2. Run verification scripts
        var verifyResult = RunVerificationScript(_testWorkspace);
        Assert.IsTrue(verifyResult.Success, "Verification should pass");
        
        // 3. Run unit tests
        var testResult = RunDotnetTest(_testWorkspace);
        Assert.IsTrue(testResult.Success, "Unit tests should pass");
    }
}
```

## 6. Static Analysis Integration

### ModRuleAuditor in CI/CD
```csharp
// Program.cs for static analysis tool
var modDirectory = args[0];
var result = ModRuleAuditor.Audit(modDirectory);

if (!result.Passed)
{
    Console.WriteLine("MOD RULE AUDIT FAILED");
    foreach (var finding in result.Findings.Where(f => f.Severity == "Error"))
    {
        Console.WriteLine($"[{finding.RuleId}] {finding.FilePath}: {finding.Description}");
    }
    Environment.Exit(1);
}

Console.WriteLine("MOD RULE AUDIT PASSED");
```

### Custom Rule Registration
```csharp
// Extend ModRuleAuditor with custom rules
public static class CustomRuleAuditor
{
    public static RuleAuditResult AuditCustom(string modDirectory)
    {
        var result = new RuleAuditResult { TargetDirectory = modDirectory };
        
        // Custom validation logic
        foreach (var file in Directory.EnumerateFiles(modDirectory, "*.cs", SearchOption.AllDirectories))
        {
            // Check for custom patterns
            if (HasCustomViolation(file))
            {
                result.Findings.Add(new RuleFinding
                {
                    RuleId = "CUSTOM_RULE",
                    Severity = "Error",
                    FilePath = file,
                    Description = "Custom rule violation",
                    Recommendation = "Fix the custom issue"
                });
            }
        }
        
        return result;
    }
}
```

## 7. Pre-Commit Verification

### Git Hook Pattern
```bash
#!/bin/bash
# .git/hooks/pre-commit

echo "Running pre-commit verification..."

# Build verification
dotnet build CalradiaForge.sln -c Release -v:minimal
if [ $? -ne 0 ]; then
    echo "Build failed. Commit aborted."
    exit 1
fi

# Stateless behavior verification
powershell -File tools/verify_stateless_behavior.ps1
if [ $? -ne 0 ]; then
    echo "Verification failed. Commit aborted."
    exit 1
fi

echo "Pre-commit verification passed."
```

### Installation
```bash
# Copy pre-commit hook to .git/hooks/
cp tools/pre-commit .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

## 8. Continuous Integration

### GitHub Actions Pattern
```yaml
name: Verification

on: [push, pull_request]

jobs:
  verify:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '4.7.2'
      
      - name: Build Solution
        run: dotnet build CalradiaForge.sln -c Release
      
      - name: Run Verification
        run: powershell -File tools/verify_stateless_behavior.ps1
      
      - name: Run Unit Tests
        run: dotnet test tests/CalradiaForge.Tests/CalradiaForge.Tests.csproj
```

### Azure DevOps Pattern
```yaml
trigger:
- main

pool:
  vmImage: 'windows-latest'

steps:
- task: DotNetCoreCLI@2
  displayName: 'Build Solution'
  inputs:
    command: 'build'
    projects: 'CalradiaForge.sln'
    arguments: '-c Release'

- task: PowerShell@2
  displayName: 'Run Verification'
  inputs:
    filePath: 'tools/verify_stateless_behavior.ps1'

- task: DotNetCoreCLI@2
  displayName: 'Run Unit Tests'
  inputs:
    command: 'test'
    projects: 'tests/CalradiaForge.Tests/CalradiaForge.Tests.csproj'
```

## 9. Distribution Verification

### DLL Validation
```powershell
# tools/validate_dlls.ps1
$modulePath = "modules/CalradiaForge/bin/Win64_Shipping_Client"

$requiredDlls = @(
    "CalradiaForge.Mod.dll",
    "CalradiaForge.Core.dll",
    "CalradiaForge.Sdk.dll"
)

foreach ($dll in $requiredDlls)
{
    $dllPath = Join-Path $modulePath $dll
    if (-not (Test-Path $dllPath))
    {
        Write-Error "Required DLL missing: $dll"
        exit 1
    }
    
    # Check DLL version
    $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dllPath)
    Write-Host "✓ $dll (v$($versionInfo.FileVersion))"
}
```

### Module Structure Validation
```powershell
# Validate required module structure
$requiredPaths = @(
    "modules/CalradiaForge/SubModule.xml",
    "modules/CalradiaForge/bin/Win64_Shipping_Client"
)

foreach ($path in $requiredPaths)
{
    if (-not (Test-Path $path))
    {
        Write-Error "Required path missing: $path"
        exit 1
    }
}
```

### Script Exclusion Check
```powershell
# Ensure no scripts in distribution
$modulePath = "modules/CalradiaForge"
$scripts = Get-ChildItem -Path $modulePath -Recurse -Include "*.ps1", "*.bat", "*.sh"

if ($scripts)
{
    Write-Warning "Found scripts in distribution (should be excluded)"
    foreach ($script in $scripts)
    {
        Write-Host "  $($script.FullName)"
    }
}
```

## 10. Test Data Management

### Test Workspace Creation
```csharp
private string CreateTestWorkspace()
{
    var tempPath = Path.Combine(Path.GetTempPath(), $"calradia_forge_test_{Guid.NewGuid()}");
    Directory.CreateDirectory(tempPath);
    
    // Copy minimal structure
    Directory.CreateDirectory(Path.Combine(tempPath, "src", "CalradiaForge.Mod"));
    Directory.CreateDirectory(Path.Combine(tempPath, "src", "CalradiaForge.Core"));
    
    return tempPath;
}
```

### Test Data Cleanup
```csharp
private void CleanupTestWorkspace(string path)
{
    if (Directory.Exists(path))
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Log warning but don't fail test
            Console.WriteLine($"Warning: Could not delete test workspace: {path}");
        }
    }
}
```

## 11. Performance Testing

### Tick Performance Test
```csharp
[Test]
public void TestHourlyTickPerformance()
{
    var behavior = new ClanCharacterProgressionBehavior();
    var stopwatch = Stopwatch.StartNew();
    
    // Simulate 1000 hourly ticks
    for (int i = 0; i < 1000; i++)
    {
        behavior.OnHourlyTick();
    }
    
    stopwatch.Stop();
    var avgMs = stopwatch.ElapsedMilliseconds / 1000.0;
    
    Assert.Less(avgMs, 1.0, "Average hourly tick should be < 1ms");
}
```

### Memory Allocation Test
```csharp
[Test]
public void TestMemoryAllocation()
{
    var behavior = new ClanCharacterProgressionBehavior();
    var memoryBefore = GC.GetTotalMemory(true);
    
    // Execute event handlers
    for (int i = 0; i < 100; i++)
    {
        behavior.OnHeroCreated(Hero.MainHero, true);
    }
    
    var memoryAfter = GC.GetTotalMemory(true);
    var allocated = memoryAfter - memoryBefore;
    
    Assert.Less(allocated, 1024 * 1024, "Should allocate < 1MB for 100 events");
}
```

## 12. Coverage Requirements

### Minimum Coverage Targets
- **Critical Paths**: 90%+ coverage
- **Event Handlers**: 80%+ coverage
- **Utility Methods**: 70%+ coverage
- **Overall**: 75%+ coverage

### Coverage Exclusions
```xml
<!-- Coverlet.runsettings -->
<ModulePaths>
  <Exclude>
    <!-- Exclude test assemblies -->
    <ModulePath>.*Tests\.dll$</ModulePath>
    
    <!-- Exclude generated code -->
    <ModulePath>.*\.obj\\.*</ModulePath>
  </Exclude>
</ModulePaths>

<Functions>
  <Exclude>
    <!-- Exclude property getters/setters -->
    <Function>.*\.get_.*</Function>
    <Function>.*\.set_.*</Function>
  </Exclude>
</Functions>
```

## 13. Documentation Testing

### Code Documentation Verification
```csharp
[Test]
public void TestPublicApiDocumentation()
{
    var assembly = Assembly.LoadFrom("CalradiaForge.Mod.dll");
    var publicTypes = assembly.GetTypes()
        .Where(t => t.IsPublic && !t.IsValueType);
    
    foreach (var type in publicTypes)
    {
        var xmlDoc = type.GetDocumentation();
        Assert.IsNotNull(xmlDoc, $"Type {type.Name} missing XML documentation");
        
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            var methodDoc = method.GetDocumentation();
            Assert.IsNotNull(methodDoc, $"Method {type.Name}.{method.Name} missing XML documentation");
        }
    }
}
```

### Documentation Consistency Check
```python
# docs/verify_docs.py
def verify_code_doc_consistency():
    # Check that CODEMAP_*.md files match actual code structure
    code_structure = analyze_code_structure()
    doc_structure = parse_code_map_files()
    
    differences = compare_structures(code_structure, doc_structure)
    
    if differences:
        print("Documentation inconsistencies found:")
        for diff in differences:
            print(f"  {diff}")
        return False
    
    return True
```

## 14. Error Reporting

### Verification Error Format
```powershell
# Standardized error reporting
Write-Host "=======================================================" -ForegroundColor Red
Write-Host "VERIFICATION FAILED" -ForegroundColor Red
Write-Host "=======================================================" -ForegroundColor Red
Write-Host ""
Write-Host "Failed Check: [Check Name]" -ForegroundColor Yellow
Write-Host "File: [File Path]" -ForegroundColor Yellow
Write-Host "Line: [Line Number]" -ForegroundColor Yellow
Write-Host "Issue: [Description of issue]" -ForegroundColor Yellow
Write-Host "Recommendation: [How to fix]" -ForegroundColor Yellow
Write-Host ""
Write-Host "Run with -Verbose for more details." -ForegroundColor Gray
```

### Success Reporting
```powershell
# Standardized success reporting
Write-Host "=======================================================" -ForegroundColor Green
Write-Host "VERIFICATION SUCCESSFUL" -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green
Write-Host ""
Write-Host "✓ Build: Release compilation successful" -ForegroundColor Green
Write-Host "✓ Statelessness: Zero SaveableTypeDefiner, empty SyncData" -ForegroundColor Green
Write-Host "✓ Anti-Shadowing: Zero Campaign names detected" -ForegroundColor Green
Write-Host "✓ Registration: SubModule.AddBehavior present" -ForegroundColor Green
Write-Host ""
Write-Host "All acceptance criteria passed." -ForegroundColor Cyan
```

## 15. Verification Workflow

### Recommended Development Workflow
```bash
# 1. Make code changes
# 2. Build solution
dotnet build CalradiaForge.sln -c Release

# 3. Run verification
powershell -File tools/verify_stateless_behavior.ps1

# 4. Run unit tests
dotnet test tests/CalradiaForge.Tests/

# 5. (Optional) Run full test suite
dotnet test --configuration Release

# 6. Commit (pre-commit hook will verify again)
git commit -m "Description"
```

### Pre-Release Checklist
```
□ Release build compiles successfully
□ verify_stateless_behavior.ps1 passes
□ Unit tests pass (75%+ coverage)
□ Integration tests pass
□ Documentation is up to date
□ SubModule.xml version updated
□ CHANGELOG.md updated
□ Distribution package validated
□ No scripts in module distribution
□ All required DLLs present
```

## 16. Headless & Piped PowerShell Execution

### Background & Hang Prevention
When batch files (`.bat`) or automation wrappers invoke PowerShell in background environments, CI/CD runners, or piped agent subshells, standard input can stall the PowerShell host indefinitely if interactive prompts are expected.

### Mandatory Invocation Pattern
All `.bat` runners that invoke PowerShell scripts MUST specify `-NoProfile`, `-NonInteractive`, and redirect standard input from `NUL` via `<nul`:

```cmd
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%SCRIPT_DIR%tools\Run-CalradiaForge-Tests.ps1" %* <nul
```

### Script Requirements
- Always include `<nul` at the end of the batch command line when launching PowerShell.
- Ensure PowerShell scripts do not invoke `Read-Host` or interactive pauses when running with `--no-pause` or in automated contexts.
- Pass explicit exit codes (`exit $LASTEXITCODE`) from PowerShell back to the calling batch wrapper.

## 17. Zone.Identifier (Mark of the Web) Verification

### Problem Description
Files downloaded from the internet or extracted from web zip archives retain the NTFS `:Zone.Identifier` alternate data stream. When .NET Framework 4.7.2 or PowerShell 5.1 attempts to inspect or dynamically load assemblies with this stream via `[System.Reflection.Assembly]::LoadFrom()`, the CLR aborts with HRESULT `0x80131515` (`NotSupportedException: An attempt was made to load an assembly from a network location...`).

### Preflight Unblocking Pattern
Test runners, analyzers, and preflight tools that examine external binaries (such as `TpacTool` dependencies) must check for and unblock alternate data streams before execution:

```powershell
$toolDir = Join-Path $workspace "TpacTool\bin"
if (Test-Path $toolDir) {
    Get-ChildItem -Path $toolDir -Recurse -Filter *.dll | ForEach-Object {
        Unblock-File -Path $_.FullName -ErrorAction SilentlyContinue
    }
}
```

## 18. Full Test Suite Battery & Runner Hierarchy

Calradia Forge maintains four distinct test suites comprising 612 automated test cases. All suites run via dedicated batch wrappers that invoke PowerShell safely:

| Suite Name | Batch Launcher | Scope & Target | Count |
| :--- | :--- | :--- | :--- |
| **Core** | `tools/Run-CalradiaForge-Core-Tests.bat` | Framework lifecycle, time-slicing, campaign behaviors, and stateless checks | 250 tests |
| **ForgeWeave** | `tools/Run-CalradiaForge-ForgeWeave-Tests.bat` | Event mesh, replay registry, execution budgets, quarantine isolation, Gauntlet UI discovery | 37 tests |
| **Desktop** | `tools/Run-CalradiaForge-Desktop-Tests.bat` | Named pipe protocol, MVVM commands, input pickers, preferences, and static source contracts | 51 tests |
| **Desktop Render** | `tools/Run-CalradiaForge-Desktop-Render-Tests.bat` | Live WPF visual tree instantiation, theme dictionaries, 13-language matrix, and layout scaling | 274 tests |
| **Unified Runner** | `tools/Run-CalradiaForge-Tests.bat` | Executes all 4 suites sequentially with clean summary reporting | 612 tests |

### CI/Batch Invocation
To execute without interactive prompts in scripts or automated pipelines:
```cmd
tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause
```

## 19. TPAC Resource Inspection & Structural Validation

### Header & Segment Verification
- Bannerlord TPAC texture packages (`.tpac`) contain binary headers, chunk tables, and asset descriptors.
- During packaging and release preflights, `tools/Inspect-CalradiaForge-Tpac.ps1` runs with `-StructuralOnly` by default:
  ```powershell
  & tools/Inspect-CalradiaForge-Tpac.ps1 -ModulePath modules/CalradiaForge -ToolDirectory TpacTool/bin -ValidateOnly -StructuralOnly
  ```
- **Structural Mode vs Full Metadata:** `-StructuralOnly` verifies file magic, version, segment counts, and offset tables without attempting full GUID resolution, allowing stub textures and unbaked development packages to pass release verification without corrupting pipeline execution.

## 20. Static Source-Code Contract Verification

### Test-Driven Architectural Enforcement
- In addition to runtime assertions, test suites (specifically `tests/CalradiaForge.Desktop.Tests/Program.cs`) verify architectural boundaries by scanning C# source files directly via `File.ReadAllText`.
- **Contract Rules:**
  - Services must not take dependencies on UI frameworks (`!service.Contains("System.Windows")`).
  - Guarded route messages, atomic file swap tokens (`File.Move(temporary, path, true)`), and queue limits (`MaximumMeasurements = 64`) must remain verbatim.
  - Refactoring must preserve these contract markers or the static test suite will reject the build.

## Summary of Verification Rules

1. **Release Build Only**: Verification requires Release configuration
2. **Four-Check Verification**: Compilation, Statelessness, Anti-Shadowing, Registration
3. **Cross-Platform Scripts**: PowerShell (Windows) and Python (Linux/Mac)
4. **Unit Test Coverage**: Minimum 75% overall, 90% for critical paths
5. **Pre-Commit Hooks**: Automated verification before commits
6. **CI/CD Integration**: GitHub Actions / Azure DevOps pipelines
7. **Distribution Validation**: DLL and structure checks before packaging
8. **Performance Testing**: Tick performance and memory allocation tests
9. **Documentation Testing**: Verify XML documentation and consistency
10. **Error Reporting**: Standardized error/success reporting format
11. **Test Data Management**: Safe workspace creation and cleanup
12. **Static Analysis**: ModRuleAuditor integration in CI/CD
13. **Integration Testing**: Full pipeline testing from build to test
14. **Custom Rules**: Extensible rule system for custom validation
15. **Pre-Release Checklist**: Comprehensive verification before release
16. **Headless Execution**: Use `-NoProfile -NonInteractive` and `<nul` redirection to prevent stdin deadlocks
17. **Zone.Identifier Safety**: Unblock external assemblies before dynamic loading to avoid `0x80131515`
18. **Four-Suite Battery**: Always verify all 612 tests (Core 250, ForgeWeave 37, Desktop 51, Render 274)
19. **TPAC Structural Validation**: Use `-StructuralOnly` during release preflights to validate asset package headers safely
20. **Static Source Contracts**: Preserve architectural inspection tokens verbatim in source code

