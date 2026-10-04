"""
CoALA-inspired memory model for Calradia Forge autonomous agents.

Provides project-specific memory tiers inspired by Cognitive Architectures for
Language Agents (CoALA); it does not claim formal conformance to a standard:
1. Semantic Memory:
   - Invariant repository rules (GEMINI Anti-Shadowing Rule A, Stateless Behavior Rule B,
     Desktop Static Contracts Rule C, Distribution Safety Rule D).
   - Zero decay; included when the orchestration path requests semantic context.
     It is not guaranteed to be injected into every agent invocation.
2. Episodic Memory:
   - Timestamped record of past tool executions, agent handoffs, forensic log links,
     and empirical performance metrics.
   - Bounded FIFO quota with graceful summarization when approaching token budgets.
3. Working Memory:
   - Short-term workspace holding current goal, active subagent role, and immediate tool results.
"""

from __future__ import annotations

import time
from dataclasses import dataclass, field
from datetime import datetime
from typing import Any, Dict, List, Optional, Tuple

from agents.compactor import CompactionStats, estimate_tokens


@dataclass
class EpisodicTrace:
    """Tool trace where status is displayed and has_errors only prioritizes retention."""

    step_id: int
    agent_name: str
    action: str
    summary: str
    raw_tokens: int
    compacted_tokens: int
    tokens_saved: int
    compression_ratio: float
    has_errors: bool  # Retention signal; `status` distinguishes FAIL from INDETERMINATE.
    log_file: Optional[str] = None
    timestamp: str = field(default_factory=lambda: datetime.now().strftime("%Y-%m-%d %H:%M:%S"))
    status: Optional[str] = field(default=None, repr=False)

    def to_compact_string(self) -> str:
        """Renders high-density episodic trace for prompt injection."""
        status = self.status or ("FAIL" if self.has_errors else "OK")
        ratio_pct = f"{self.compression_ratio * 100:.1f}%"
        log_ref = f" [Log: {self.log_file}]" if self.log_file else ""
        return (
            f"- [{self.timestamp}] {self.agent_name} -> {self.action} ({status}): "
            f"{self.summary} (Saved {self.tokens_saved} tokens, -{ratio_pct}){log_ref}"
        )


class SemanticRepositoryMemory:
    """Immutable semantic memory containing Calradia Forge core constraints and rules."""

    INVARIANTS: Tuple[str, ...] = (
        "Rule A (Anti-Shadowing): Zero folders, namespaces, or classes named 'Campaign' or 'Localization'.",
        "Rule B (Statelessness): Zero SaveableTypeDefiner and empty SyncData in src/CalradiaForge.Mod.",
        "Rule C (Desktop Contracts): Mandatory source tokens preserved in src/CalradiaForge.Desktop.",
        "Rule D (Distribution Safety): Exclude raw game DLLs, user saves, and batch/powershell scripts.",
        "Single-Threaded Engine: All Bannerlord Campaign/Mission entity calls must execute on main thread.",
        "Optional Time-Slicing: Use ForgeTimeSlicer.ShouldProcess with stable IDs only when work may be deferred; buckets can be uneven and filtering still scans the full collection. Measure the complete callback before making allocation or latency claims.",
    )

    @classmethod
    def get_semantic_context(cls) -> str:
        """Returns the canonical semantic invariants formatted for agent context injection."""
        lines = ["=== Core Semantic Invariants (Repository Constraints) ==="]
        for inv in cls.INVARIANTS:
            lines.append(f"  • {inv}")
        return "\n".join(lines)


class WorkingAgentMemory:
    """Short-term working memory representing active task execution."""

    def __init__(self, objective: str = "", active_agent: str = "ForgeMasterAgent") -> None:
        self.objective: str = objective
        self.active_agent: str = active_agent
        self.current_step: int = 0
        self.active_tool: Optional[str] = None
        self.last_output: str = ""

    def update(self, active_agent: str, active_tool: Optional[str], last_output: str) -> None:
        self.current_step += 1
        self.active_agent = active_agent
        self.active_tool = active_tool
        self.last_output = last_output

    def get_working_context(self) -> str:
        lines = [
            "=== Working Memory (Active Execution Frame) ===",
            f"  Goal: {self.objective}",
            f"  Active Agent: {self.active_agent} (Step #{self.current_step})",
        ]
        if self.active_tool:
            lines.append(f"  Active Tool: {self.active_tool}")
        return "\n".join(lines)


class CoALAAgentMemory:
    """Project-specific multi-tiered memory model inspired by CoALA."""

    def __init__(
        self,
        objective: str = "",
        max_episodic_traces: int = 32,
        context_token_budget: int = 32_000,
    ) -> None:
        self.semantic = SemanticRepositoryMemory()
        self.working = WorkingAgentMemory(objective=objective)
        self.episodic: List[EpisodicTrace] = []
        self.max_episodic_traces: int = max(1, int(max_episodic_traces))
        self.context_token_budget: int = context_token_budget

    def record_step(
        self,
        agent_name: str,
        action: str,
        summary: str,
        stats: Optional[CompactionStats] = None,
    ) -> EpisodicTrace:
        """Records a step in episodic memory, maintaining FIFO bounds."""
        step_id = len(self.episodic) + 1
        raw = stats.raw_tokens if stats else estimate_tokens(summary)
        compact = stats.compacted_tokens if stats else raw
        saved = stats.tokens_saved if stats else 0
        ratio = stats.compression_ratio if stats else 0.0
        status = stats.status if stats else None
        has_errors = stats.has_errors if stats else False
        if status not in {"OK", "FAIL", "INDETERMINATE"}:
            status = "FAIL" if has_errors else "OK"
        log_file = stats.log_file if stats else None

        trace = EpisodicTrace(
            step_id=step_id,
            agent_name=agent_name,
            action=action,
            summary=summary,
            raw_tokens=raw,
            compacted_tokens=compact,
            tokens_saved=saved,
            compression_ratio=ratio,
            has_errors=has_errors,
            log_file=log_file,
            status=status,
        )

        self.episodic.append(trace)

        # Enforce the FIFO bound, preferring to retain attention-worthy traces.
        if len(self.episodic) > self.max_episodic_traces:
            # First prune the oldest non-error outside the five newest traces,
            # then any older success if failures occupy that prefix. Only when
            # every retained trace needs attention must the oldest be evicted.
            prune_idx = next(
                (i for i, trace in enumerate(self.episodic[:-5]) if not trace.has_errors),
                None,
            )
            if prune_idx is None:
                prune_idx = next(
                    (i for i, trace in enumerate(self.episodic) if not trace.has_errors),
                    None,
                )
            self.episodic.pop(0 if prune_idx is None else prune_idx)

        self.working.update(agent_name, action, summary)
        return trace

    def render_context(self) -> str:
        """Renders the consolidated memory structure within the configured token budget."""
        sections = [
            self.semantic.get_semantic_context(),
            "",
            self.working.get_working_context(),
            "",
            f"=== Episodic Memory ({len(self.episodic)} recent execution traces) ===",
        ]

        if not self.episodic:
            sections.append("  (No prior episodic traces recorded)")
        else:
            for trace in self.episodic:
                sections.append("  " + trace.to_compact_string())

        total_saved = sum(t.tokens_saved for t in self.episodic)
        total_raw = sum(t.raw_tokens for t in self.episodic)
        ratio = (total_saved / total_raw * 100) if total_raw > 0 else 0.0
        sections.append(f"\n[Telemetry: Net tokens saved in this episode: {total_saved:,} (-{ratio:.1f}%)]")

        return "\n".join(sections)
