---
name: test-architect
description: Comprehensive multi-stage test architecture and verification strategy for Bannerlord mods and Calradia Forge. Orchestrating pure unit tests, static C# and XAML contract testing, headless WPF layout/render passes, Windows UI Automation, and non-interactive batch runners.
metadata:
  risk: safe
  source: Calradia Forge Agent Ecosystem (Apache 2.0)
  date_added: "2026-09-28"
---

# Test Architect: Multi-Stage Verification & Static Contract Testing

Testing complex game mods and developer workbenches is notoriously difficult. Relying exclusively on manual testing requires launching the full game from Steam, waiting 60+ seconds through splash screens, and manually setting up campaign conditions. When tests break, developers guess at which layer failed. A master test architect implements a **5-Stage Verification Pyramid** combining static source contract analysis, pure unit tests, headless WPF layout verification, out-of-process Windows UI Automation, and automated distribution gates.

---

For source-backed persistence, failure-test, CI and distribution details, read [recent-commit lessons](../calradia-forge-dev-workflow/references/recent-commit-lessons.md).

## 1. Core Principles

1. **The 5-Stage Verification Pyramid**:
   - **Stage 1: Static Source Contracts**: Regex and AST scans verifying architectural rules (GEMINI Rule A anti-shadowing, Rule B statelessness, Rule C required tokens) in milliseconds.
   - **Stage 2: Pure Unit Tests**: Logic tests for Core SDK, behavioral math, time slicers, and event dispatchers without engine dependencies.
   - **Stage 3: Headless WPF Render & Layout Tests**: In-process layout passes measuring element measurements, visual tree node counts, and minimum surface constraints (980x680 DIP).
   - **Stage 4: Windows UI Automation (UIA) Smoke Checks**: Out-of-process accessibility tree inspection verifying native control discovery without stealing user focus.
   - **Stage 5: Packaging & Distribution Hygiene Gate**: Preflight verification ensuring release archives exclude proprietary engine DLLs, scripts, and debug logs.
2. **Deterministic Mocks (Zero Game Boot Requirement)**:
   - Mod core logic must be testable completely offline without booting `Bannerlord.exe`.
   - Decouple entity references using string identifiers or lightweight data interfaces.
3. **Strict Non-Interactive Execution (`<nul`)**:
   - Automated scripts must **never pause** or prompt for user keystrokes (`press any key to continue...`).
   - Pipe `<nul` into batch files and use `-NonInteractive` on PowerShell invocations to prevent headless CI deadlocks.
4. **Static Token & Regex Invariance**:
   - Automated tests in `tests/CalradiaForge.Desktop.Tests/Program.cs` check source tokens via `File.ReadAllText`.
   - Never alter or remove protected contract tokens (e.g. required template regex counts, guarded desktop route messages).

5. **Failure Must Be Observed**:
   - Following `4c5b16c`, capture the exception in `catch`, then assert after that block that the expected exception occurred and retains its reference identity. A conditional assertion inside `catch` passes silently if no exception is thrown.
   - The atomic replacement fixture (`3f7170c`) covers the exact `IOException.HResult == 0x80070497` (Win32 1175) refusal: at most four attempts and delays of 20/40/60 ms, only while both files exist. Verify persistent refusal propagation, unchanged destination and retained staged file. Errors 1176/1177 and a missing staged source are not retried. The helper keeps `File.Replace`; these tests do not establish all persistence or filesystem failure outcomes.
   - `tools/Setup-CalradiaForge-Python.bat` uses `pip --timeout 120 --retries 5` for requirement downloads. Treat this as bounded download retry configuration; do not infer complete response-body delivery or a working optional runtime from it.

6. **Distribution and Remote Evidence**:
   - At release, milestone, significant feature completion or explicit packaging requests, require the three current-version ZIPs even when the version is unchanged; honor an explicit no-ZIP instruction. Documentation-only maintenance does not independently trigger packaging. This applies in every conversation/interface language and includes existing localized resources without inventing translations.
   - Keep archives, staging, audit reports and hash manifests in ignored `artifacts/`; require a passing archive audit and independently matching SHA-256 hashes. Use emitted filenames: `25.2.0` maps to `package-audit-2520.json` and `package-sha256-2520.txt`. Tests or green CI are not archive evidence.
   - A scoped objective commit does not authorize a push. After an explicitly authorized push, verify the remote branch at the exact pushed SHA and await applicable Actions/check runs/commit statuses for that SHA. Retrieve failing logs, validate task-related corrections through BAT and review the next pushed SHA under the existing authorization. Pending/inaccessible checks remain unverified; link the matching final runs.

---

## 2. Capabilities & Scope

### Capabilities
- `test-pyramid-orchestration`: Sequences and executes the complete 5-stage verification suite.
- `static-contract-auditing`: Validates repository constraints via regex pattern matching against source code.
- `headless-layout-testing`: Measures WPF layout passes, visual tree snapshots, and viewport bounds without rendering windows.
- `uia-smoke-verification`: Discovers and asserts accessibility properties across workbench windows.
- `ci-runner-hardening`: Ensures batch scripts execute with non-interactive flags and clean return codes.

### Scope
- **In Scope**: Solution test suites (`CalradiaForge.Tests`, `CalradiaForge.ForgeWeave.Tests`, `CalradiaForge.Desktop.Tests`, `CalradiaForge.Desktop.RenderTests`), UIA smoke scripts, packaging preflight.
- **Out of Scope**: In-game human visual inspection (requires authorized live review per `AGENTS.md`).

---

## 3. Concrete Test Architecture Patterns

### Pattern 1: Static Source Contract Testing
Verify architectural invariants via static file scanning before compiling or executing tests.

```csharp
[Fact]
public void VerifyDesktopPageTemplatesStaticContract()
{
    var templatePath = Path.Combine(SolutionRoot, "src/CalradiaForge.Desktop/Resources/Views/ToolPageTemplates.xaml");
    string content = File.ReadAllText(templatePath);

    // Rule C Invariant: Exactly 9 templates named [A-Za-z]+DashboardTemplate
    var matches = Regex.Matches(content, "<DataTemplate x:Key=\"[A-Za-z]+DashboardTemplate\"");
    Assert.True(matches.Count == 9, 
        $"Static contract violation: Expected exactly 9 DashboardTemplates, found {matches.Count}. " +
        "New studio templates must use the 'ViewTemplate' suffix to preserve static contracts.");
}
```

### Pattern 2: Non-Interactive Batch Script Launcher
Construct batch files that never hang in headless automated execution.

```cmd
@echo off
setlocal
:: Ensure child consoles are hidden and input is detached
cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --skip-build --no-pause <nul"
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

call tools\Verify-CalradiaForge-StatelessBehavior.bat
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

exit /b 0
```

### Pattern 3: Headless Visual Tree Metric Sampling
Validate WPF rendering performance and visual tree node limits in-process.

```csharp
public static void AssertLayoutPerformance(Visual visualRoot, int maxVisitedNodes, double maxLayoutMs)
{
    var sw = Stopwatch.StartNew();
    int visitedNodes = 0;

    void Traverse(DependencyObject current)
    {
        visitedNodes++;
        int count = VisualTreeHelper.GetChildrenCount(current);
        for (int i = 0; i < count; i++)
        {
            Traverse(VisualTreeHelper.GetChild(current, i));
        }
    }

    Traverse(visualRoot);
    sw.Stop();

    Assert.True(visitedNodes <= maxVisitedNodes, $"Visual tree bloat: {visitedNodes} nodes visited (Max: {maxVisitedNodes}).");
    Assert.True(sw.Elapsed.TotalMilliseconds <= maxLayoutMs, $"Layout latency spike: {sw.Elapsed.TotalMilliseconds:0.0} ms (Max: {maxLayoutMs} ms).");
}
```

---

## 4. Sharp Edges & Anti-Patterns

### Edge 1: Interactive Prompts in Test Scripts (`pause`)
- **Severity**: CRITICAL
- **Symptom**: Automated agent or CI runner hangs indefinitely at 100% timeout limit.
- **Root Cause**: A batch file contains `pause` or prompts for user confirmation.
- **Fix**: Always invoke batch files with `--no-pause <nul` and pass `-NonInteractive` to PowerShell.

### Edge 2: Breaking Protected Regex Tokens in View Templates
- **Severity**: HIGH
- **Symptom**: `CalradiaForge.Desktop.Tests` fails with a contract assertion failure immediately upon build.
- **Root Cause**: Naming a new data template `DiagnosticsStudioDashboardTemplate` instead of `DiagnosticsStudioViewTemplate`, violating the strict count of 9.
- **Fix**: Adhere strictly to the template naming convention: use `ViewTemplate` for studio extensions.

### Edge 3: Testing Mod Logic with Live Engine Dependencies
- **Severity**: HIGH
- **Symptom**: Tests crash with `TypeInitializationException` in `TaleWorlds.MountAndBlade.MBGlobals` or complain that native engine libraries cannot be loaded.
- **Root Cause**: Unit tests attempting to instantiate TaleWorlds game singletons (`Campaign.Current`, `Mission.Current`) outside the game process.
- **Fix**: Isolate core logic into `CalradiaForge.Core` and `CalradiaForge.Sdk`, mocking game interfaces for unit tests.

---

## 5. Validation Rules & Verification Checklist

1. [ ] **Non-Interactive Batch Gate**: All test batch files accept `--no-pause` and pipe `<nul`.
2. [ ] **Static Contract Fidelity**: Automated tests verify all 6 mandatory desktop contract tokens.
3. [ ] **Stateless Verification Gate**: The stateless BAT gate passes all checks emitted by the current run.
4. [ ] **Headless Render Pass Verification**: WPF render tests retain required coverage and report actual case counts, layout passes and scoped timings; compare equivalent before/after runs rather than enforcing a historical count or duration.
5. [ ] **Zero External Test Residue**: Keep diagnostic evidence in ignored `artifacts/`; exclude logs, dumps and generated captures from commits and distribution.
