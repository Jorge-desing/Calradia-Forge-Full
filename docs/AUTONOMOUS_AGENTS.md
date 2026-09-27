# Calradia Forge Autonomous AI Agents Architecture

This document defines the architecture, subagent hierarchy, tool surfaces, and execution pipelines for autonomous AI agents in Calradia Forge, powered by the **Google Antigravity SDK** (`google.antigravity`).

---

## 1. Overview & Mission

Calradia Forge employs a specialized multi-agent architecture designed to autonomously maintain, audit, develop, and verify the repository across all target frameworks:
- **`src/CalradiaForge.Mod`** (`net472` in-game Bannerlord module).
- **`src/CalradiaForge.Desktop`** (`net8.0-windows` standalone WPF workbench).
- **`src/CalradiaForge.Core` / `Sdk`** (`net472;net8.0` shared core systems).

The system integrates directly with the 45 domain skills in `.agents/skills/`, enforces repository invariants (Rules A, B, C, and D), and supports both cloud execution via the Gemini Developer API / Vertex AI and deterministic offline simulation for continuous integration.

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

---

## 2. Multi-Agent Hierarchy & Roles

The system is organized into 6 specialized agents:

| Agent Name | Primary Domain | Core Invariants Enforced | Bound Tools |
| :--- | :--- | :--- | :--- |
| **`ForgeMasterAgent`** | Root Orchestrator | Task decomposition, safety gates, synthesis | All tools & subagent delegation (`START_SUBAGENT`) |
| **`ForgeArchitectAgent`** | C# & Bannerlord Engine | Rule A Anti-Shadowing (`GEMINI.md`), TFM boundaries, GameModel decorators | `run_dotnet_build`, `inspect_csharp_source` |
| **`StatelessBehaviorAuditor`** | Persistence & Safety | Rule B Statelessness, zero `SaveableTypeDefiner`, stateless `SyncData` | `verify_stateless_behavior`, `inspect_csharp_source` |
| **`DesktopWpfSpecialist`** | WPF Workbench & UI | Rule C Desktop Contracts, container recycling, DirectX aliased edges, UIA | `audit_desktop_contracts`, `run_ui_automation_smoke`, `run_solution_tests` |
| **`DocLedgerAgent`** | Docs & Release Integrity | Rule D Safety, bilingual parity, SHA-256 ledger integrity chain | `audit_documentation_parity`, `audit_ledger_integrity`, `run_package_workflow` |
| **`BugHunterAgent`** | Code Smells & Concurrency | FormattableString interpolation, CAS locks, SyncRoot guards, thread dispatch | `audit_code_smells`, `audit_concurrency_hazards`, `audit_section_playbooks` |

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
- **Role:** Mod persistence and simulation auditor.
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
  - Windows UI Automation smoke checks via `tools/Test-CalradiaForge-Desktop-Uia.ps1`.

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
  - Audits `ConcurrentDictionary` and CAS locks in `ForgeData`, `SyncRoot` in `ForgeAgentMemory`, and `SemaphoreSlim` in `PipeClient`.
  - Enforces TaleWorlds main-thread marshaling via `GameThreadActionDispatch`.
  - Audits all 8 Gauntlet section playbooks, 8 Desktop remedy trees, and prefab bindings.

---

## 3. Google Antigravity SDK Integration

The autonomous agent suite is built directly on top of `google.antigravity`:

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

### 3.2 Repository Skills Auto-Discovery
The agent automatically imports the 45 specialized skills located in `.agents/skills/` via `skills_paths=[".agents/skills"]`, allowing agents to reference patterns from `bannerlord-dotnet-artisan`, `calradia-forge-modding`, `agent-memory-systems`, and `calradia-forge-desktop`.

---

## 4. Custom Repository Domain Tools

The agents are equipped with 12 custom Python tools wrapping repository scripts, MSBuild workflows, and architectural checkers:

| Tool Function | Description | Safety / Invariant Checked |
| :--- | :--- | :--- |
| `run_dotnet_build` | Compiles `CalradiaForge.sln` or projects via `dotnet build` | C# compilation, 0 warnings, 0 errors |
| `verify_stateless_behavior` | Executes `tools/verify_stateless_behavior.ps1` | Rule B: Zero SaveableTypeDefiner, stateless SyncData |
| `run_solution_tests` | Executes `tools/Run-CalradiaForge-Tests.bat` | Core, ForgeWeave, Desktop MVVM, and Render tests |
| `run_ui_automation_smoke` | Executes `tools/Test-CalradiaForge-Desktop-Uia.ps1` | 29 accessibility checks on live WPF window |
| `audit_documentation_parity` | Verifies matching English/Spanish files in `docs/` | Conceptual parity across `.md` and `.es.md` |
| `audit_ledger_integrity` | Validates SHA-256 chain in `.integrity.jsonl` | Tamper-evident immutable ledger integrity |
| `inspect_csharp_source` | Static AST scan across `src/` | Rule A: Anti-shadowing; Rule B: Statelessness |
| `audit_desktop_contracts` | Inspects `src/CalradiaForge.Desktop` | Rule C: 6 static contract tokens |
| `audit_section_playbooks` | Audits playbooks, remedy trees, and prefabs | Gauntlet/Desktop macros & ForgePlaybookPanel |
| `audit_code_smells` | Scans for string formatting, hot-path LINQ, etc. | Clean stateless SyncData & zero hot-path LINQ |
| `audit_concurrency_hazards` | Audits thread safety, CAS locks, and SemaphoreSlim | Thread-safe data stores & main thread dispatch |
| `run_package_workflow` | Executes `tools/package.ps1` | Rule D: Clean distribution zips in `artifacts/` |

---

## 5. Unified CLI Runner (`tools/run_forge_agents.py`)

A unified CLI runner provides easy execution for developers, CI/CD pipelines, and autonomous workflows:

### Basic Invocations
```bash
# Comprehensive token compaction benchmark across all 12 tools
py -3.12 tools/run_forge_agents.py compact

# Full architectural and safety audit
py -3.12 tools/run_forge_agents.py audit

# Code smells, antipatterns, and concurrency hazards audit
py -3.12 tools/run_forge_agents.py bughunt

# Section playbooks, troubleshooting trees, and macro audit
py -3.12 tools/run_forge_agents.py playbooks

# Bilingual docs parity and SHA-256 ledger integrity audit
py -3.12 tools/run_forge_agents.py docs

# Compile solution and inspect C# source rules
py -3.12 tools/run_forge_agents.py architect

# Solution tests and Windows UI Automation smoke check
py -3.12 tools/run_forge_agents.py verify

# Execute arbitrary autonomous task prompt
py -3.12 tools/run_forge_agents.py run "Audit persistence safety and verify docs parity"
```

### CLI Flags
- `--offline`: Forces deterministic offline simulation mode (no API key required).
- `--verbose` / `-v`: Enables real-time output and thought streaming.
- `--model <name>`: Overrides model identifier (defaults to `gemini-3.8-flash`).
- `--compaction-preset [ultra|balanced|deep]`: Configures context window token threshold (`ultra`: 8k, `balanced`: 16k, `deep`: 32k, default: `deep`).
- `--raw-tools`: Bypasses distillation and outputs uncompressed raw tool logs to the LLM context.
- `--live`: (Used with `compact`) Executes all tools live, including heavy compiler, test, and packaging runs.

---

## 6. Deterministic Offline Simulation Mode

To ensure reproducible testing and CI/CD validation without requiring cloud API credentials:
- When `GEMINI_API_KEY` is not present or `--offline` is specified, `ForgeAgentOrchestrator` automatically runs in **Deterministic Offline Simulation Mode**.
- The orchestrator analyzes the task intent, dispatches work to the registered subagents, executes the required repository domain tools, validates invariants, and produces a complete structured execution report.
- When `GEMINI_API_KEY` is present, the orchestrator connects to the Gemini API using `google.antigravity.Agent` for full autonomous reasoning and subagent delegation.

---

## 7. Verification & Automated Testing

The autonomous agent system is verified via automated tests in `tests/test_forge_agents.py`:
```bash
py -3.12 -m unittest tests/test_forge_agents.py
```
Test suite coverage:
- Configuration factory, compaction presets, and Google Antigravity SDK config creation.
- Subagent schema validation, system instructions, and tool bindings.
- Repository domain tools execution and semantic output distillation.
- `ForgeTokenCompactor` token estimation, lossless compiler error retention, and forensic artifact logging.
- Orchestrator multi-agent delegation pipeline and offline simulation.
- CLI argument parsing and dispatch.

---

## 8. Token Compaction & Context Optimization Engine (`ForgeTokenCompactor`)

To prevent runaway context window bloat during complex multi-agent workflows, Calradia Forge incorporates an aggressive, lossless token compaction engine implemented in `agents/compactor.py`:

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

### 8.1 Preserving Agent Intelligence (Lossless Failure Telemetry)
The token compactor is built on a strict zero-loss guarantee for diagnostic information:
1. **Lossless Compiler Errors**: When `dotnet build` fails, all `error CSxxxx`, `error MSBxxxx`, file paths, line/column coordinates, and error descriptions are 100% preserved. No project restoration noise is passed.
2. **Lossless Test Failures**: When unit or render tests fail, failed test names, exception types, assertion diffs, and stack traces are extracted and preserved verbatim.
3. **Lossless Invariant Diagnostics**: Violations in C# AST scans (Rule A), persistence checks (Rule B), or desktop contracts (Rule C) retain full file and line coordinates.
4. **Forensic Traceability**: Every tool run writes its full uncompressed raw console output to `artifacts/agent-runs/<timestamp>_<tool>.log`. The compacted output embeds the path, allowing any agent or human to inspect the full trace if deeper context is required.

### 8.2 Compaction Presets (`CompactionConfig`)
Context window pruning is dynamically controlled via presets:
- **`ultra` (8,000 tokens)**: Designed for resource-constrained environments, small context models, or fast deterministic checks.
- **`balanced` (16,000 tokens)**: Recommended operational balance between conversational history and lean context consumption.
- **`deep` (32,000 tokens, default)**: Expanded deep reasoning context window for intricate multi-agent refactoring sessions and multi-file code reviews.

### 8.3 Empirical Token Reduction Benchmarks

| Tool Operation | Raw Output Characters | Raw Tokens | Compacted Tokens | Net Savings | Compression Ratio |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`run_dotnet_build`** (Clean Release) | 2,366 chars | ~622 tokens | ~56 tokens | 566 tokens | **87.5% - 91.0%** |
| **`run_solution_tests`** (726 Tests) | 53,222 chars | ~14,005 tokens | ~134 tokens | 13,871 tokens | **99.0%** |
| **`verify_stateless_behavior`** | 3,353 chars | ~882 tokens | ~146 tokens | 702 tokens | **82.8%** |
| **`run_ui_automation_smoke`** (29 Nodes) | 6,840 chars | ~1,800 tokens | ~78 tokens | 1,722 tokens | **95.7%** |
| **`run_package_workflow`** | 3,120 chars | ~183 tokens | ~83 tokens | 100 tokens | **54.6%** |
| **`audit_code_smells`** | 380 chars | ~94 tokens | ~40 tokens | 54 tokens | **57.4%** |
| **`audit_concurrency_hazards`** | 412 chars | ~103 tokens | ~48 tokens | 55 tokens | **53.4%** |
| **`audit_section_playbooks`** | 365 chars | ~91 tokens | ~42 tokens | 49 tokens | **53.8%** |
| **Multi-Agent 12-Tool Suite (`compact`)** | ~8,000 chars | 2,005 tokens | 880 tokens | 1,152 tokens | **57.5% reduction** |

---

## 9. CoALA Cognitive Memory Architecture (`agents/memory.py`)

Calradia Forge implements the **Cognitive Architectures for Language Agents (CoALA)** standard to organize agent cognition across three distinct tiers:

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
- **Nature:** Immutable, zero-decay repository knowledge injected into all agent contexts.
- **Contents:**
  - **Rule A (Anti-Shadowing)**: Zero folders, namespaces, or classes named `Campaign` or `Localization`.
  - **Rule B (Statelessness)**: Zero `SaveableTypeDefiner` and clean `SyncData` in `src/CalradiaForge.Mod`.
  - **Rule C (Desktop Contracts)**: Mandatory static source tokens preserved in `src/CalradiaForge.Desktop`.
  - **Rule D (Distribution Safety)**: Exclude engine binaries, user saves, and scripts from release archives.
  - **Engine Thread Affinity**: TaleWorlds entity APIs must be marshaled to the game thread.
  - **Anti-Lag Time-Slicing**: Modulo-24 hero distribution and zero LINQ queries in simulation ticks.

### 9.2 Working Memory (`WorkingAgentMemory`)
- **Nature:** Short-term execution frame reflecting the immediate task status.
- **State:** Tracks the current high-level objective, active agent persona (`ForgeArchitectAgent`, `BugHunterAgent`, etc.), current step index, active tool name, and last tool result.

### 9.3 Episodic Memory (`EpisodicTrace`)
- **Nature:** Bounded chronological history of executed actions and empirical observations.
- **Trace Record:** Step ID, agent name, action, dense summary, raw tokens, compacted tokens, net tokens saved, compression ratio, error status, and forensic log reference.
- **Error-Preservation FIFO Pruning:** When trace counts exceed the bounded queue size (`max_episodic_traces = 32`), normal execution traces are evicted via FIFO. However, any trace with `has_errors == True` is **strictly protected against eviction**, ensuring that past failures and diagnostic details are never forgotten during multi-step reasoning.

