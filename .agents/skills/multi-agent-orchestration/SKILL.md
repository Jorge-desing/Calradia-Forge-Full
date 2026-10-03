---
name: multi-agent-orchestration
description: "Multi-agent coordination for Calradia Forge using Google Antigravity SDK and OpenAI Codex CLI. Covers task contracts, tool-specific output summaries, raw-log review, CoALA memory integration, and host-specific task waiting without busy polling."
metadata:
  risk: safe
  source: Calradia Forge Agent Ecosystem (Apache 2.0)
  date_added: "2026-09-28"
---

# Multi-Agent Orchestration: Autonomous Coordination & Output Summaries

Building complex software frameworks like Calradia Forge requires coordinated expertise: C# compiler and TaleWorlds engine architecture, WPF MVVM desktop engineering, save game safety auditing, DocFX bilingual documentation, and empirical release packaging. Relying on a single monolithic agent context can cause context window pollution and attention degradation. A multi-agent orchestrator decomposes tasks into bounded contracts and may use the repository's Google Antigravity SDK (`google.antigravity`) or Codex agent facilities. The output compactor provides heuristic token estimates and tool-specific summaries; it is not a universal lossless telemetry guarantee.

---

## 1. Core Principles

1. **Hierarchical Squadron Delegation**:
   - `ForgeMasterAgent` acts as orchestrator, decomposing macro goals into discrete task contracts.
   - The repository configures five specialist roles; local offline tool execution calls repository tools sequentially and does not create Antigravity SDK workers. Only describe workers as spawned when the selected online runtime reports that execution.
   - The specialist roster is:
     - **`ForgeArchitectAgent`**: C# architecture, target frameworks (`net472` vs `net8.0`), and GEMINI Rule A (anti-shadowing).
     - **`StatelessBehaviorAuditor`**: Save safety, empty `SyncData`, and zero `SaveableTypeDefiner` (Rule B).
     - **`DesktopWpfSpecialist`**: WPF MVVM, vector telemetry controls, 980x680 DIP minimum surface contract, and Windows UI Automation.
     - **`DocLedgerAgent`**: Strict English/Spanish documentation parity and append-only SHA-256 ledger integrity.
     - **`BugHunterAgent`**: C# code-smell and concurrency-hazard audits.
2. **Bounded Output Summaries with Raw-Log Review**:
   - `ForgeTokenCompactor` selects a distiller by tool name and extracts recognized diagnostics and summary metrics. Its token counts use a character/word heuristic, not the target model's tokenizer.
   - Evidence is fixture-specific. `test_distill_dotnet_build_success` checks greater than 70% estimated reduction for one synthetic 40-project output. `test_distill_dotnet_build_preserves_compiler_errors` checks two sample compiler diagnostics. These tests do not establish a general compression range or preservation of every diagnostic format.
   - `test_distill_solution_tests_failure_preserves_stack_traces` checks the failure status, a `FAIL` line, and an assertion line; despite its name, it does not assert that the stack-frame line is present in the compacted text. Do not claim universal preservation of assertions, compiler messages, stack traces, or other failure telemetry.
   - Raw output is written to `artifacts/agent-runs/` when `save_raw=True` and the write succeeds; callers can disable it. For an incident requiring complete details, use raw mode/`force_raw=True` or inspect the returned forensic log when available. A compact summary or `has_errors` flag is not a substitute for reviewing the raw evidence.
3. **Host-Specific Event-Driven Waiting (No Polling Loops)**:
   - For Codex subagent tasks, use the collaboration mailbox/wait facilities; that host signals new messages and task completion. This does not establish an equivalent wake-up or yield API for Google Antigravity. Verify the installed SDK's versioned documentation before describing its task lifecycle.
   - Codex collaboration agents are managed by the Codex host; they are separate from the repository's `tools/Run-CalradiaForge-Agents.bat` runner. That runner selects the Antigravity SDK path only when its prerequisites are present, otherwise it executes repository tools locally. A Codex agent being active does not prove an Antigravity worker was created, and local offline tool execution is not subagent delegation.
   - **NEVER** write active polling loops such as `while (!task.IsDone) { manage_task('status'); sleep(5); }`. Use the task and messaging facilities supported by the selected host, and report the execution route that actually ran.
4. **CoALA-inspired project memory model**:
   - Python agent orchestration uses the project-specific `CoALAAgentMemory` (`agents/memory.py`) with bounded, structured memory tiers; this is conceptually inspired by CoALA and does not claim full framework conformance:
     - **Working Memory**: Active goal, current tool call, and scratchpad state.
     - **Episodic Memory**: Bounded execution log with timestamps, tool names, and outcomes.
     - **Semantic Memory**: Immutable tuple of repository rules (Rule A, Rule B, Rule C, Rule D) and domain facts, injected only on paths that request the rendered memory context.
   - `ForgeAgentMemory` is a separate C# Bannerlord/SDK runtime service; do not conflate it with the Python orchestrator's CoALA memory.
   - Do not attach a previous tool's `last_stats` to a later raw-tool result. Use stats returned for that invocation or record the verdict without compaction savings.

---

## 2. Capabilities & Scope

### Capabilities
- `squadron-orchestration`: Configures specialist roles for the optional online Antigravity integration; local offline tool execution calls repository tools and must be reported separately from online agent execution.
- `tool-output-summarization`: Produces heuristic token statistics and summaries for recognized tool formats.
- `failure-triage`: Uses extracted error indicators to prioritize review; extraction is tool-specific and must not be described as complete or lossless.
- `coala-memory-management`: Injects structured episodic and semantic memory into autonomous agent prompts.
- `codex-antigravity-interop`: Maintains parity between Google Antigravity (`GEMINI.md`) and OpenAI Codex CLI (`AGENTS.md`, `CODEX.md`).

### Scope
- **In Scope**: `agents/` autonomous framework, `tools/run_forge_agents.py`, subagent dispatching, token optimization.
- **Out of Scope**: In-game Bannerlord battle AI (delegate to `game-ai-behavior-trees`).

---

## 3. Concrete Orchestration Patterns

### Pattern 1: External SDK API Sketch (Unverified; Not Repository Code)
The following is conceptual pseudocode for an external SDK shape only. `AgentContext`, `SubagentRunner`, `invoke_subagent`, and `yield_turn` are not verified against the repository's pinned optional dependency and are not used by `agents/orchestrator.py`. Do not copy this block as executable code or cite it as evidence of current SDK behavior. Check the installed package's versioned documentation before using any corresponding API.

```text
# EXTERNAL PSEUDOCODE ONLY — API symbols are unverified.
from google.antigravity import AgentContext, SubagentRunner

def run_architectural_audit(context: AgentContext):
    # Dispatch specialized architect subagent
    subagent_id = context.invoke_subagent(
        type_name="ForgeArchitectAgent",
        role="C# Architectural Invariant Auditor",
        prompt="Audit src/CalradiaForge.Mod for GEMINI Rule A anti-shadowing and TaleWorlds thread affinity."
    )
    # This external SDK yield/wake-up behavior is unverified; do not rely on it.
    return context.yield_turn()
```

The current local implementation is observable in `agents/orchestrator.py`: it chooses an online `Agent` path only when the SDK, credentials, and non-offline configuration are all present. Otherwise it runs repository tool functions locally; this fallback is not an SDK subagent dispatch.

### Pattern 2: Tool-Specific Summary and Raw Evidence
Compact only when a concise view is useful, and preserve a route to raw evidence. Summaries must not be treated as complete diagnostic records.

```python
from agents.compactor import ForgeTokenCompactor

def inspect_build_output(raw_output: str) -> str:
    raw_text, stats = ForgeTokenCompactor.distill(
        "run_dotnet_build",
        raw_output,
        save_raw=True,
        force_raw=True,
    )
    # In raw mode, inspect raw_text directly. Do not infer failure state from
    # stats.has_errors: the bypass returns original text without classifying it.
    assert raw_text == raw_output
    if stats.log_file:
        print(f"Forensic copy: {stats.log_file}")
    return raw_text
```

For compacted operation, call `distill` without `force_raw`, treat the result as a summary, and inspect raw output whenever diagnostic completeness matters. A returned log path is available only when saving was requested and the write succeeded.

---

## 4. Sharp Edges & Anti-Patterns

### Edge 1: Polling Task or Subagent Status in a Loop
- **Severity**: CRITICAL
- **Symptom**: Repeated status checks can consume tool calls and add coordination overhead without advancing the task.
- **Root Cause**: The orchestration assumes every host provides the same asynchronous wait or wake-up behavior.
- **Fix**: In Codex, use its collaboration mailbox and wait facilities; those can surface messages or task completion while the caller is waiting. This guarantee is specific to the Codex runtime. Do not claim Google Antigravity automatically resumes an agent on completion unless the installed, versioned SDK documentation confirms that behavior. For Antigravity, follow only the lifecycle and waiting mechanisms documented for the installed SDK; report unsupported or unverified behavior explicitly.

### Edge 2: Treating a Summary as a Complete Failure Record
- **Severity**: CRITICAL
- **Symptom**: An agent acts on an incomplete diagnostic excerpt or cannot see the failing stack frame.
- **Root Cause**: A tool-specific distiller recognizes only selected line formats and may cap or omit details; fixture success does not prove coverage of other output shapes.
- **Fix**: Review raw output or its forensic log for diagnosis-critical failures. Use raw mode when complete text is required, and add a fixture for any newly supported output format before documenting its behavior.

### Edge 3: Spawning Subagents for Trivial Single-File Lookups
- **Severity**: MEDIUM
- **Symptom**: Unnecessary overhead, high latency, and clutter in conversation transcripts.
- **Root Cause**: Spawning a subagent just to read a single file or run a single grep command.
- **Fix**: Perform quick targeted lookups directly in the main agent context. Use subagents only for complex, multi-step tasks requiring deep reasoning or independent workspaces.

---

## 5. Validation Rules & Verification Checklist

1. [ ] **Fixture-Bounded Evidence**: State exactly which sample formats and assertions are covered; do not generalize fixture results into universal guarantees.
2. [ ] **Raw Failure Path**: Confirm how the caller obtains original output (`force_raw`/raw mode or a successfully returned `log_file`) before relying on compacted diagnostics.
3. [ ] **Estimated Metrics**: Label token counts/reduction as heuristic estimates, not model-tokenizer measurements.
4. [ ] **Zero Polling Loops**: No code or agent prompts contain active polling loops.
5. [ ] **CoALA Memory Separation**: Working, episodic, and semantic memory layers are cleanly separated.
6. [ ] **Execution Route & Authority**: Report the route actually taken. The current SDK configuration sets `enable_sandbox=False`, so do not claim command sandboxing or tool isolation without a separate verified authority boundary.
7. [ ] **Dual-Platform Parity**: Agent definitions maintain parity across `AGENTS.md` and `GEMINI.md`.
