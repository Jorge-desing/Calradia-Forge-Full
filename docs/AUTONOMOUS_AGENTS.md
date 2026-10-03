# Calradia Forge Autonomous AI Agents Architecture

This document defines the architecture, subagent hierarchy, tool surfaces, and execution pipelines for the optional Google Antigravity integration in Calradia Forge. The SDK is a developer-tooling extra, not a Forge or Bannerlord runtime dependency.

---

## 1. Overview & Mission

Calradia Forge employs a specialized multi-agent architecture designed to autonomously maintain, audit, develop, and verify the repository across all target frameworks:
- **`src/CalradiaForge.Mod`** (`net472` in-game Bannerlord module).
- **`src/CalradiaForge.Desktop`** (`net8.0-windows` standalone WPF workbench).
- **`src/CalradiaForge.Core` / `Sdk`** (`net472;net8.0` shared core systems).

The system discovers the project skills available under `.agents/skills/`, enforces repository invariants (Rules A, B, C, and D), and supports online execution through the optional Antigravity SDK or local offline tool execution that invokes selected repository tools. The discovered skill set changes as the repository evolves; no fixed count is assumed.

```
                      ┌────────────────────────────┐
                      │      ForgeMasterAgent      │
                      │  (Root Orchestrator / AGY) │
                      └──────────────┬─────────────┘
                                     │
         ┌──────────────────┬────────┴─────────┬──────────────────┐
         ▼                  ▼                  ▼                  ▼
┌──────────────────┐ ┌──────────────┐ ┌──────────────────┐ ┌──────────────┐
│  ForgeArchitect  │ │  Stateless   │ │    DesktopWpf    │ │  DocLedger   │
│      Agent       │ │   Auditor    │ │    Specialist    │ │    Agent     │
│  (C# / Engine)   │ │ (Save Safety)│ │  (WPF / Render)  │ │ (Docs/Ledger)│
└──────────────────┘ └──────────────┘ └──────────────────┘ └──────────────┘
```

The table below is the current role roster: one coordinator and five configured specialist roles, including `BugHunterAgent`. The diagram is illustrative and does not enumerate every specialist.

---

## 2. Multi-Agent Hierarchy & Roles

The configured roster has one coordinator and five specialist roles:

| Agent Name | Primary Domain | Core Invariants Enforced | Bound Tools |
| :--- | :--- | :--- | :--- |
| **`ForgeMasterAgent`** | Root Orchestrator | Task decomposition, safety gates, synthesis | Online: configured repository tools and SDK-configured specialists; offline: selected local repository tools (no local `START_SUBAGENT` tool) |
| **`ForgeArchitectAgent`** | C# & Bannerlord Engine | Rule A Anti-Shadowing (`GEMINI.md`), TFM boundaries, GameModel decorators | `run_dotnet_build`, `inspect_csharp_source` |
| **`StatelessBehaviorAuditor`** | Persistence & Safety | Rule B Statelessness, zero `SaveableTypeDefiner`, stateless `SyncData` | `verify_stateless_behavior`, `inspect_csharp_source` |
| **`DesktopWpfSpecialist`** | WPF Workbench & UI | Rule C Desktop Contracts, container recycling, DirectX aliased edges, UIA | `audit_desktop_contracts`, `run_ui_automation_smoke`, `run_solution_tests` |
| **`DocLedgerAgent`** | Docs & Release Integrity | Rule D Safety, bilingual parity, SHA-256 ledger integrity chain | `audit_documentation_parity`, `audit_ledger_integrity`, `run_package_workflow` |
| **`BugHunterAgent`** | Code Smells & Concurrency | FormattableString interpolation, CAS locks, SyncRoot guards, thread dispatch | `audit_code_smells`, `audit_concurrency_hazards`, `inspect_csharp_source` |

### 2.1 ForgeMasterAgent (Root Orchestrator)
- **Role:** High-level strategic planner and coordinator.
- **System Instructions:** Analyzes natural language instructions, decomposes complex feature requests or bug audits into specialist subtasks, delegates to domain agents, and verifies that all hard invariants pass before concluding.

### 2.2 ForgeArchitectAgent (C# / Bannerlord Engine)
- **Role:** System architect for game assemblies and TaleWorlds engine interactions.
- **Invariants:**
  - Strictly preserves Target Framework isolation (`net472` for game code, `net8.0-windows` for desktop).
  - Enforces **Rule A (GEMINI.md Anti-Shadowing)**: Never allows folders, namespaces, or classes named `Campaign` or `Localization` in `src/CalradiaForge.Mod`.
  - Enforces Decorator GameModel patterns wrapping `_previousModel` with `ExplainedNumber`.

### 2.3 StatelessBehaviorAuditor (Save Safety & CampaignBehavior)
- **Role:** Mod persistence and campaign behavior auditor.
- **Invariants:**
  - Enforces **Rule B (Stateless Campaign Behavior)**: Mod behaviors must remain completely stateless with regard to save games.
  - Zero inheritance from `SaveableTypeDefiner` in `src/CalradiaForge.Mod`.
  - Zero `dataStore.SyncData(...)` serialization calls inside `SyncData(IDataStore dataStore)`.
  - Prohibits direct serialization of transient engine entities (`Hero`, `Settlement`, `MobileParty`).

### 2.4 DesktopWpfSpecialist (WPF Workbench & Render Optimization)
- **Role:** Standalone workbench UI engineer and layout performance auditor.
- **Invariants:**
  - Pure graphic virtualization: `VirtualizingPanel.ScrollUnit="Pixel"`, `VirtualizationMode="Recycling"`, `CacheLength="1,1"`.
  - DirectX aliased edges: `RenderOptions.EdgeMode="Aliased"` and `SnapsToDevicePixels="True"` on 1px dividers.
  - Enforces **Rule C (Desktop Static Contracts)**: Validates static tokens asserted by `tests/CalradiaForge.Desktop.Tests/Program.cs`.
  - Windows UI Automation smoke checks via `tools/Test-CalradiaForge-Desktop-Uia.bat`.

### 2.5 DocLedgerAgent (Bilingual Docs & Ledger Integrity)
- **Role:** Technical writer, ledger auditor, and release gatekeeper.
- **Invariants:**
  - Strict English/Spanish dual-language parity for technical documentation in `docs/`.
  - Verifies the SHA-256 cryptographic hash chain in `docs/CalradiaForge-Registro-Mejoras.integrity.jsonl`.
  - Enforces **Rule D (Distribution Safety)**: Excludes proprietary DLLs, save files, and scripts from release archives.

### 2.6 BugHunterAgent (Code Smells & Concurrency)
- **Role:** Static code smell inspector, thread-safety auditor, and procedural playbook validator.
- **Invariants:**
  - Validates `FormattableString` string substitution invariants in Gauntlet and Desktop.
  - Audits `ConcurrentDictionary` and CAS locks in `ForgeData`, `SyncRoot` in the C# SDK's `ForgeAgentMemory`, and `SemaphoreSlim` in `PipeClient`. This game-runtime memory is distinct from Python's `CoALAAgentMemory` in `agents/memory.py`.
  - Enforces TaleWorlds main-thread marshaling via `GameThreadActionDispatch`.
  - Audits all 8 Gauntlet section playbooks, 8 Desktop remedy trees, and prefab bindings.

---

## 3. Google Antigravity SDK Integration

When the optional `google-antigravity` profile is installed, the autonomous agent integration uses `google.antigravity`:

### 3.1 LocalAgentConfig Configuration
```python
from google.antigravity import LocalAgentConfig, types
from agents.config import ForgeAgentConfig
from agents.subagents import get_subagent_configs
from agents.tools import ALL_REPO_TOOLS

capabilities = types.CapabilitiesConfig(
    enable_subagents=True,
    max_subagent_depth=2,
    allowed_subagents=[sa.name for sa in get_subagent_configs()],
    agent_behavior=types.AgentBehavior.AUTONOMOUS,
    run_command_config=types.RunCommandConfig(enable_sandbox=False),
)

budget_config = types.BudgetConfig(
    max_model_calls=30,
    max_tool_calls=60,
    max_total_tokens=300_000,
)

compaction_config = types.CompactionConfig(
    token_threshold=32_000,  # Deep preset (32k tokens default)
)

config = LocalAgentConfig(
    model="gemini-3.8-flash",
    capabilities=capabilities,
    budget_config=budget_config,
    compaction_config=compaction_config,
    skills_paths=[".agents/skills"],
    tools=ALL_REPO_TOOLS,
    subagents=get_subagent_configs(),
)
```

This configuration sets `enable_sandbox=False`; it does not enable Antigravity command sandboxing or establish process/tool isolation. Do not claim isolation unless a separate OS-level authority boundary has been verified.

### 3.2 Repository Skills Auto-Discovery
The agent exposes the skills directory at `.agents/skills/` through `skills_paths=[".agents/skills"]` when that directory exists. The available skills can change independently of the agent package; examples include `bannerlord-dotnet-artisan`, `calradia-forge-modding`, `agent-memory-systems`, and `calradia-forge-desktop`.

---

## 4. Custom Repository Domain Tools

The repository exposes Python tools wrapping scripts, MSBuild workflows, and architectural checkers; the current functions are listed below:

| Tool Function | Description | Safety / Invariant Checked |
| :--- | :--- | :--- |
| `run_dotnet_build` | Compiles `CalradiaForge.sln` or projects via `dotnet build` | C# compilation, 0 warnings, 0 errors |
| `verify_stateless_behavior` | Executes `tools/Verify-CalradiaForge-StatelessBehavior.bat` | Rule B: Zero SaveableTypeDefiner, stateless SyncData |
| `run_solution_tests` | Executes `tools/Run-CalradiaForge-Tests.bat` | Core, ForgeWeave, Desktop MVVM, and Render tests |
| `run_ui_automation_smoke` | Executes `tools/Test-CalradiaForge-Desktop-Uia.bat` | Windows UI Automation smoke checks against the WPF window |
| `audit_documentation_parity` | Verifies matching English/Spanish files in `docs/` | Conceptual parity across `.md` and `.es.md` |
| `audit_ledger_integrity` | Validates SHA-256 chain in `.integrity.jsonl` | Tamper-evident immutable ledger integrity |
| `inspect_csharp_source` | Static AST scan across `src/` | Rule A: Anti-shadowing; Rule B: Statelessness |
| `audit_desktop_contracts` | Inspects `src/CalradiaForge.Desktop` | Rule C: 6 static contract tokens |
| `audit_section_playbooks` | Audits playbooks, remedy trees, and prefabs | Gauntlet/Desktop macros & ForgePlaybookPanel |
| `audit_code_smells` | Scans for string formatting, hot-path LINQ, etc. | Clean stateless SyncData & zero hot-path LINQ |
| `audit_concurrency_hazards` | Audits thread safety, CAS locks, and SemaphoreSlim | Thread-safe data stores & main thread dispatch |
| `run_package_workflow` | Executes `tools/package.ps1` | Rule D: Clean distribution zips in `artifacts/` |

---

## 5. Unified CLI Runner (`tools/Run-CalradiaForge-Agents.bat`)

A maintained launcher runs `tools/run_forge_agents.py` with the repository `.venv` interpreter. It fails with setup instructions if the environment is missing and does not fall back to a global Python. The CLI supports developers, CI/CD pipelines, and autonomous workflows:

### Basic Invocations
```bat
REM Token compaction benchmark across the currently registered tools
tools\Run-CalradiaForge-Agents.bat compact

REM Full architectural and safety audit
tools\Run-CalradiaForge-Agents.bat audit

REM Code smells, antipatterns, and concurrency hazards audit
tools\Run-CalradiaForge-Agents.bat bughunt

REM Section playbooks, troubleshooting trees, and macro audit
tools\Run-CalradiaForge-Agents.bat playbooks

REM Bilingual docs parity and SHA-256 ledger integrity audit
tools\Run-CalradiaForge-Agents.bat docs

REM Compile solution and inspect C# source rules
tools\Run-CalradiaForge-Agents.bat architect

REM Solution tests and Windows UI Automation smoke check
tools\Run-CalradiaForge-Agents.bat verify

REM Execute arbitrary autonomous task prompt
tools\Run-CalradiaForge-Agents.bat run "Audit persistence safety and verify docs parity"
```

### CLI Flags
- `--offline`: Forces local offline tool execution (no API key required).
- `--verbose` / `-v`: Enables verbose assistant-output streaming.
- `--model <name>`: Overrides model identifier (defaults to `gemini-3.8-flash`).
- `--compaction-preset [ultra|balanced|deep]`: Configures the token threshold (`ultra`: 8k, `balanced`: 16k, `deep`: 32k; default: `deep`).
- `--raw-tools`: Bypasses distillation and outputs uncompressed raw tool logs to the LLM context.
- `--live`: (Used with `compact`) Executes all tools live, including heavy compiler, test, and packaging runs.

The optional SDK package is not required to import the CLI or run local offline tool execution. The online Antigravity route requires the SDK to be importable, credentials to be present, and `offline_mode=False`; the optional `--agents` test profile separately requires the SDK because it tests SDK configuration. Install that profile into the isolated project environment with `tools\Setup-CalradiaForge-Python.bat --agents --no-pause`, then run `tools\Run-CalradiaForge-Python-Checks.bat --ci --agents --no-pause`. Offline mode bypasses cloud execution; it does not prove cloud behavior.

---

## 6. Local Offline Tool Execution

Local offline tool execution runs selected repository functions without cloud API credentials:
- `ForgeAgentOrchestrator` selects its online route only when the optional SDK is importable, credentials are present, and offline mode is disabled. Otherwise it reports local offline tool execution and the reason that route was selected.
- Local offline tool execution calls selected repository tool functions; it does not spawn SDK subagent workers. Its report marks each tool result `PASS`, `FAIL`, or `INDETERMINATE` from explicit output markers. `COMPLETE` is emitted only when every selected tool call returned a report and all verdicts are explicit passes; it is not a claim that unselected checks ran.
- With the SDK importable, credentials present, and offline mode disabled, the orchestrator uses `google.antigravity.Agent` for online execution.

---

## 7. Verification & Automated Testing

The autonomous agent system is verified via automated tests in `tests/test_forge_agents.py`:
```bat
tools\Run-CalradiaForge-Python-Checks.bat --ci --agents --no-pause
```
Test suite coverage:
- Configuration factory, compaction presets, and Google Antigravity SDK config creation.
- Subagent schema validation, system instructions, and tool bindings.
- Repository domain tools execution and semantic output distillation.
- `ForgeTokenCompactor` heuristic token estimates, pattern-based diagnostic retention with representative fixture coverage, and optional artifact logging.
- Orchestrator multi-agent delegation pipeline and local offline tool execution.
- CLI argument parsing and dispatch.

---

## 8. Token Compaction & Context Optimization Engine (`ForgeTokenCompactor`)

To reduce context volume during complex multi-agent workflows, Calradia Forge uses a heuristic semantic distillation engine implemented in `agents/compactor.py`. Distillation is lossy by design; the compact text is a summary, not a substitute for the original output.

```
               Raw Tool Output (Console, MSBuild, Test Suites, UIA)
                                      │
                      ┌───────────────┴───────────────┐
                      ▼                               ▼
       Forensic Persistence                   Semantic Distiller
    (artifacts/agent-runs/*.log)         (Error & Metric Preservation)
                      │                               │
                      └───────────────┬───────────────┘
                                      ▼
                        High-Density Compact Report
                     + Forensic Reference Link for LLM
```

### 8.1 Diagnostic Retention and Evidence Limits
The distillers are designed to retain recognized diagnostic lines in compact summaries, but they do not provide a zero-loss or completeness guarantee. Output formats that do not match a distiller's patterns, capped failure lists, and unexpected tool output can be omitted from the compact text.
1. **Build Diagnostics**: The MSBuild distiller recognizes supported compiler/build error patterns and includes matching lines. This has regression coverage for representative cases; it does not prove that every compiler, MSBuild, locale, or custom-tool diagnostic is captured.
2. **Test Failures**: The test distiller extracts selected failure markers and assertion/error lines, with bounded output. It does not reliably reproduce complete test names, every assertion diff, or full stack traces.
3. **Invariant Diagnostics**: Audit summaries depend on the output format and patterns recognized by each distiller. Use the original tool output to verify full file and line coordinates.
4. **Optional Forensic Logging**: When raw saving is enabled, the output is non-empty, and writing succeeds, the compactor stores the uncompressed text in `artifacts/agent-runs/<timestamp>_<tool>.log` and adds the log path to its summary. The log is the more complete evidence source; its availability is not guaranteed if saving is disabled or a write fails.

### 8.2 Compaction Presets (`CompactionConfig`)
Context window pruning is dynamically controlled via presets:
- **`ultra` (8,000 tokens)**: Designed for resource-constrained environments, small context models, or fast deterministic checks.
- **`balanced` (16,000 tokens)**: Recommended operational balance between conversational history and lean context consumption.
- **`deep` (32,000 tokens, default)**: Expanded deep reasoning context window for intricate multi-agent refactoring sessions and multi-file code reviews.

### 8.3 Reproducible Token-Estimate Comparisons

This guide does not publish fixed token-saving or test-count baselines. `ForgeTokenCompactor` estimates are heuristic and depend on the exact input and distiller version. For a useful comparison, record the repository revision, Python and package versions, tool name, exact fixture or output hash, raw/compacted strings, and both estimates for each run. Treat such values as evidence for that input only, not as universal thresholds or performance guarantees.

---

## 9. CoALA Cognitive Memory Architecture (`agents/memory.py`)

The repository agent runner uses a project-specific, bounded memory model inspired by concepts from **Cognitive Architectures for Language Agents (CoALA)**. This does not claim conformance to a formal CoALA standard or mean that every invocation receives the same semantic context:

```
               ┌────────────────────────────────────────────────┐
               │              CoALAAgentMemory                  │
               └───────────────────────┬────────────────────────┘
                                       │
         ┌─────────────────────────────┼─────────────────────────────┐
         ▼                             ▼                             ▼
┌──────────────────┐         ┌──────────────────┐         ┌──────────────────┐
│ Semantic Memory  │         │  Working Memory  │         │ Episodic Memory  │
│ (Zero Decay /    │         │ (Active Goal /   │         │ (Bounded FIFO /  │
│  Invariants)     │         │  Current Step)   │         │  Error Guarded)  │
└──────────────────┘         └──────────────────┘         └──────────────────┘
```

### 9.1 Semantic Memory (`SemanticRepositoryMemory`)
- **Nature:** Immutable repository rules included when the semantic context is rendered; this is not a guarantee that every agent execution consumes the same context.
- **Contents:**
  - **Rule A (Anti-Shadowing)**: Zero folders, namespaces, or classes named `Campaign` or `Localization`.
  - **Rule B (Statelessness)**: Zero `SaveableTypeDefiner` and clean `SyncData` in `src/CalradiaForge.Mod`.
  - **Rule C (Desktop Contracts)**: Mandatory static source tokens preserved in `src/CalradiaForge.Desktop`.
  - **Rule D (Distribution Safety)**: Exclude engine binaries, user saves, and scripts from release archives.
  - **Engine Thread Affinity**: TaleWorlds entity APIs must be marshaled to the game thread.
  - **Optional Time-Slicing**: Use `ForgeTimeSlicer.ShouldProcess` only for work whose semantics permit deferral. Bucket distribution can be uneven, filtering still scans the input collection, and allocation or duration claims require measurement of the relevant callback.

### 9.2 Working Memory (`WorkingAgentMemory`)
- **Nature:** Short-term execution frame reflecting the immediate task status.
- **State:** Tracks the current high-level objective, active agent persona (`ForgeArchitectAgent`, `BugHunterAgent`, etc.), current step index, active tool name, and last tool result.

### 9.3 Episodic Memory (`EpisodicTrace`)
- **Nature:** Bounded chronological history of executed actions and empirical observations.
- **Trace Record:** Step ID, agent name, action, dense summary, raw tokens, compacted tokens, net tokens saved, compression ratio, error status, and forensic log reference.
- **Error-Aware FIFO Pruning:** The default `max_episodic_traces` value is 32 and can be configured. When the limit is exceeded, pruning prefers the oldest non-error trace among eligible older entries; if none is available, it removes the oldest entry. Error traces are therefore preferred during pruning but are not guaranteed to remain indefinitely, and the compact summary may omit details retained only in an optional forensic log.

