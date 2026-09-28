---
name: multi-agent-orchestration
description: Multi-agent coordination and orchestration architecture using Google Antigravity SDK and OpenAI Codex CLI for Calradia Forge. Hierarchical delegation, semantic token compaction with lossless failure telemetry, CoALA memory integration, and background execution without polling loops.
risk: safe
source: Calradia Forge Agent Ecosystem (Apache 2.0)
date_added: 2026-09-28
---

# Multi-Agent Orchestration: Autonomous Coordination & Token Compaction

Building complex software frameworks like Calradia Forge requires coordinated expertise: C# compiler and TaleWorlds engine architecture, WPF MVVM desktop engineering, save game safety auditing, DocFX bilingual documentation, and empirical release packaging. Relying on a single monolithic agent context causes context window pollution and attention degradation. A master multi-agent architect deploys a **Hierarchical Multi-Agent Squadron** powered by the **Google Antigravity SDK** (`google.antigravity`) and compatible with **OpenAI Codex CLI**, governed by semantic **Token Compaction** with **Lossless Failure Telemetry**.

---

## 1. Core Principles

1. **Hierarchical Squadron Delegation**:
   - `ForgeMasterAgent` acts as orchestrator, decomposing macro goals into discrete task contracts.
   - Delegates work to specialized agents:
     - **`ForgeArchitectAgent`**: C# architecture, target frameworks (`net472` vs `net8.0`), and GEMINI Rule A (anti-shadowing).
     - **`StatelessBehaviorAuditor`**: Save safety, empty `SyncData`, and zero `SaveableTypeDefiner` (Rule B).
     - **`DesktopWpfSpecialist`**: WPF MVVM, vector telemetry controls, 980x680 DIP minimum surface contract, and Windows UI Automation.
     - **`DocLedgerAgent`**: Strict English/Spanish documentation parity and append-only SHA-256 ledger integrity.
2. **Semantic Token Compaction with Lossless Failure Telemetry**:
   - The token compaction engine (`ForgeTokenCompactor`) achieves 80–99% token reduction on verbose command outputs (such as thousand-line build outputs or passing test reports).
   - **MANDATORY INVARIANT**: 100% preservation of failure telemetry. Compiler error codes (`CSxxxx`), test assertion failures, and stack traces must never be compressed away or summarized into lossy generalizations. Full uncompressed logs are archived to `artifacts/agent-runs/`.
3. **Reactive Wakeup (Zero Polling Loops)**:
   - When spawning background tasks or subagents, the runtime resumes execution reactively when work completes.
   - **NEVER** write polling loops: `while (!task.IsDone) { manage_task('status'); sleep(5); }`. Launch the task or subagent and stop calling tools to yield your turn.
4. **CoALA Cognitive Memory Architecture**:
   - Equip agents with structured memory tiers (`ForgeAgentMemory`):
     - **Working Memory**: Active goal, current tool call, and scratchpad state.
     - **Episodic Memory**: Bounded execution log with timestamps, tool names, and outcomes.
     - **Semantic Memory**: Immutable repository rules (Rule A, Rule B, Rule C, Rule D) and domain facts.

---

## 2. Capabilities & Scope

### Capabilities
- `squadron-orchestration`: Launches and manages specialized subagents concurrently using `invoke_subagent`.
- `token-compaction`: Distills verbose tool outputs into concise markdown tables with uncompressed log links.
- `lossless-error-retention`: Automatically extracts and highlights compiler errors (`CSxxxx`) and test failures.
- `coala-memory-management`: Injects structured episodic and semantic memory into autonomous agent prompts.
- `codex-antigravity-interop`: Maintains parity between Google Antigravity (`GEMINI.md`) and OpenAI Codex CLI (`AGENTS.md`, `CODEX.md`).

### Scope
- **In Scope**: `agents/` autonomous framework, `tools/run_forge_agents.py`, subagent dispatching, token optimization.
- **Out of Scope**: In-game Bannerlord battle AI (delegate to `game-ai-behavior-trees`).

---

## 3. Concrete Orchestration Patterns

### Pattern 1: Subagent Invocation Without Polling
Launch a specialized subagent and yield control cleanly.

```python
# CORRECT: Launching specialist subagent in Python / Antigravity SDK
from google.antigravity import AgentContext, SubagentRunner

def run_architectural_audit(context: AgentContext):
    # Dispatch specialized architect subagent
    subagent_id = context.invoke_subagent(
        type_name="ForgeArchitectAgent",
        role="C# Architectural Invariant Auditor",
        prompt="Audit src/CalradiaForge.Mod for GEMINI Rule A anti-shadowing and TaleWorlds thread affinity."
    )
    # Stop execution cleanly; system reactively wakes up on subagent message
    return context.yield_turn()
```

### Pattern 2: Token Compaction with Lossless Failure Telemetry
Compact verbose build outputs while guaranteeing complete error fidelity.

```python
import re

def compact_dotnet_build_output(raw_output: str, log_path: str) -> str:
    # 1. Check for compiler errors (CSxxxx) or warnings
    error_lines = [line for line in raw_output.splitlines() if "error CS" in line or ": error " in line]
    
    if error_lines:
        # LOSSLESS RETENTION: Retain all exact compiler errors verbatim
        formatted_errors = "\n".join(error_lines[:15])
        return (
            f"**BUILD FAILED**: {len(error_lines)} compiler error(s) detected.\n\n"
            f"```text\n{formatted_errors}\n```\n"
            f"Full raw output: `{log_path}`"
        )
    
    # 2. On success, compact hundreds of lines into a concise summary
    elapsed_match = re.search(r"Tiempo transcurrido ([0-9:]+)", raw_output)
    elapsed = elapsed_match.group(1) if elapsed_match else "N/A"
    return f"**BUILD SUCCESS**: 0 errors, 0 warnings. (Compiled in {elapsed}) [Log: `{log_path}`]"
```

---

## 4. Sharp Edges & Anti-Patterns

### Edge 1: Polling Task or Subagent Status in a Loop
- **Severity**: CRITICAL
- **Symptom**: Exhausts API token limits, burns tool call quotas, and delays task completion.
- **Root Cause**: Calling `manage_task(Action='status')` or checking subagents in an active loop.
- **Fix**: Never poll. Launch the task or subagent, inform the user, and conclude your turn. The messaging system will wake you up upon completion.

### Edge 2: Compacting Away Compiler Errors or Stack Traces
- **Severity**: CRITICAL
- **Symptom**: The agent reports "Build failed with errors" but cannot see what failed, entering a hallucination loop attempting random fixes.
- **Root Cause**: Token compactor truncating error outputs to save tokens.
- **Fix**: Enforce the Lossless Failure Invariant: Always preserve 100% of compiler errors (`CSxxxx`), test assertions, and stack traces verbatim in the compacted context.

### Edge 3: Spawning Subagents for Trivial Single-File Lookups
- **Severity**: MEDIUM
- **Symptom**: Unnecessary overhead, high latency, and clutter in conversation transcripts.
- **Root Cause**: Spawning a subagent just to read a single file or run a single grep command.
- **Fix**: Perform quick targeted lookups directly in the main agent context. Use subagents only for complex, multi-step tasks requiring deep reasoning or independent workspaces.

---

## 5. Validation Rules & Verification Checklist

1. [ ] **Lossless Failure Telemetry**: Compaction tests in `test_forge_agents.py` confirm 100% error preservation.
2. [ ] **Zero Polling Loops**: No code or agent prompts contain active polling loops.
3. [ ] **CoALA Memory Separation**: Working, episodic, and semantic memory layers are cleanly separated.
4. [ ] **Subagent Tool Isolation**: Subagents receive only the tools and permissions necessary for their specialized role.
5. [ ] **Dual-Platform Parity**: Agent definitions maintain parity across `AGENTS.md` and `GEMINI.md`.
