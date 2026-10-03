"""
Multi-agent orchestration engine for Calradia Forge.

Coordinates the Google Antigravity Agent runtime with specialized subagents,
custom repository tools, and a local offline tool-execution route.
"""

from __future__ import annotations

import asyncio
import re
import sys
from typing import Any, Dict, List, Optional

from agents.compactor import CompactionStats, estimate_tokens
from agents.config import (
    ForgeAgentConfig,
    create_forge_agent_config,
    create_sdk_agent_config,
)
from agents.memory import CoALAAgentMemory
from agents.subagents import (
    ARCHITECT_AGENT_NAME,
    BUG_HUNTER_NAME,
    DESKTOP_WPF_NAME,
    DOC_LEDGER_NAME,
    MASTER_AGENT_NAME,
    STATELESS_AUDITOR_NAME,
    get_master_instructions,
    get_subagent_configs,
)
from agents.tools import (
    ALL_REPO_TOOLS,
    audit_code_smells,
    audit_concurrency_hazards,
    audit_desktop_contracts,
    audit_documentation_parity,
    audit_ledger_integrity,
    inspect_csharp_source,
    run_dotnet_build,
    run_package_workflow,
    run_solution_tests,
    run_ui_automation_smoke,
    verify_stateless_behavior,
)

try:
    from google.antigravity import Agent
    SDK_AVAILABLE = True
except ImportError:
    SDK_AVAILABLE = False
    Agent = None


class ForgeAgentOrchestrator:
    """Orchestrator for Calradia Forge autonomous agents."""

    def __init__(self, config: Optional[ForgeAgentConfig] = None) -> None:
        self.config = config or create_forge_agent_config()
        self.tools = ALL_REPO_TOOLS
        self.subagents = get_subagent_configs()
        self.memory = CoALAAgentMemory(
            objective="",
            context_token_budget=self.config.token_threshold,
        )

    async def run_task(self, prompt: str) -> str:
        """Executes a task via Google Antigravity Agent or local repository tools."""
        self.memory.working.objective = prompt
        # Use cloud Agent if credentials exist and offline mode is not forced
        if SDK_AVAILABLE and self.config.has_api_credentials and not self.config.offline_mode:
            return await self._run_online_agent(prompt)

        if self.config.offline_mode:
            reason = "offline mode was explicitly requested"
        elif not SDK_AVAILABLE:
            reason = "the optional Antigravity SDK is unavailable"
        else:
            reason = "API credentials are unavailable"
        return await self._run_local_tool_execution(prompt, route_reason=reason)

    def run_task_sync(self, prompt: str) -> str:
        """Synchronous wrapper for run_task."""
        return asyncio.run(self.run_task(prompt))

    async def _run_online_agent(self, prompt: str) -> str:
        """Executes the task using the live Google Antigravity SDK Agent."""
        augmented_instructions = (
            get_master_instructions()
            + "\n\n"
            + self.memory.render_context()
        )
        sdk_config = create_sdk_agent_config(
            forge_config=self.config,
            tools=self.tools,
            subagents=self.subagents,
            system_instructions=augmented_instructions,
        )

        output_chunks: List[str] = []
        async with Agent(config=sdk_config) as agent:
            response = await agent.chat(prompt)
            async for chunk in response:
                if self.config.verbose:
                    print(chunk, end="", flush=True)
                output_chunks.append(chunk)

        full_output = "".join(output_chunks).strip()
        if not full_output:
            full_output = await response.text()

        first_line = full_output.splitlines()[0] if full_output else "Completed online task"
        self.memory.record_step(
            agent_name=MASTER_AGENT_NAME,
            action="online_chat",
            summary=first_line[:120],
        )
        return full_output

    async def _run_local_tool_execution(self, prompt: str, route_reason: Optional[str] = None) -> str:
        """Runs selected repository tools locally and reports their observed verdicts."""
        p_lower = prompt.lower()
        reports: Dict[str, str] = {}
        delegations: List[str] = []
        checks: List[tuple[str, str, bool, CompactionStats]] = []

        def run_check(
            label: str,
            action: Any,
            success_markers: tuple[str, ...],
            raw_output: bool = True,
        ) -> str:
            """Run one local tool and accept only its explicit, recognized verdict."""
            try:
                report = action(raw=raw_output)
            except Exception as ex:
                report = f"Exception while executing {label}: {type(ex).__name__}: {ex}"
                returned = False
            else:
                returned = True
            verdict = _classify_tool_report(report, success_markers)
            stored_summary = report.splitlines()[0] if report else ""
            estimated_tokens = estimate_tokens(stored_summary)
            status = {"PASS": "OK", "FAIL": "FAIL", "INDETERMINATE": "INDETERMINATE"}[verdict]
            stats = CompactionStats(
                tool_name=label,
                raw_tokens=estimated_tokens,
                compacted_tokens=estimated_tokens,
                tokens_saved=0,
                compression_ratio=0.0,
                has_errors=verdict != "PASS",
                error_count=int(verdict == "FAIL"),
                status=status,
            )
            checks.append((label, verdict, returned, stats))
            return report

        def asks_for(*terms: str) -> bool:
            return any(re.search(rf"\b{re.escape(term)}\b", p_lower) for term in terms)

        is_audit = asks_for("audit", "stateless", "rule", "shadow", "check", "inspect")
        is_docs = asks_for("doc", "docs", "documentation", "parity", "ledger", "registro", "hash")
        is_ui_automation = asks_for("uia") or bool(re.search(r"\bui\s+automation\b", p_lower))
        is_test = asks_for("test", "tests", "testing", "render", "layout") or (
            asks_for("smoke") and not is_ui_automation
        )
        is_compile = asks_for("build", "compile", "csproj", "sln")
        is_build = is_compile or asks_for("architect", "architecture")
        is_package = asks_for("package", "release", "zip", "distribution", "dist")
        is_package_action = is_package and not any(
            w in p_lower for w in ["audit", "inspect", "review", "check"]
        )
        is_bughunt = any(w in p_lower for w in ["bug", "smell", "hazard", "concurrency", "hunter"])

        # If generic or comprehensive request, trigger standard audit & verification suite
        if not any([is_audit, is_docs, is_test, is_build, is_package, is_bughunt, is_ui_automation]):
            is_audit = True
            is_docs = True

        # 1. ForgeArchitectAgent delegation
        if is_build or is_audit:
            delegations.append(f"Delegating to {ARCHITECT_AGENT_NAME} for C# architecture and anti-shadowing check...")
            rep = run_check(
                "C# source inspection",
                inspect_csharp_source,
                ("C# Source Inspection PASSED",),
            )
            reports["Architect_Inspection"] = rep
            self.memory.record_step(ARCHITECT_AGENT_NAME, "inspect_csharp_source", rep.splitlines()[0] if rep else "Inspected C# source", checks[-1][3])

        if is_compile:
            delegations.append(f"Delegating to {ARCHITECT_AGENT_NAME} for the requested solution build...")
            rep = run_check(
                "solution build",
                run_dotnet_build,
                ("Build SUCCESS",),
                raw_output=False,
            )
            reports["Solution_Build"] = rep
            self.memory.record_step(ARCHITECT_AGENT_NAME, "run_dotnet_build", rep.splitlines()[0] if rep else "Built the solution", checks[-1][3])

        # 2. StatelessBehaviorAuditor delegation
        if is_audit:
            delegations.append(f"Delegating to {STATELESS_AUDITOR_NAME} for stateless persistence verification...")
            rep = run_check(
                "stateless behavior verification",
                verify_stateless_behavior,
                ("Stateless Behavior Verification [PASS]", "Stateless Behavior Verification [PASSED]"),
            )
            reports["Stateless_Behavior"] = rep
            self.memory.record_step(STATELESS_AUDITOR_NAME, "verify_stateless_behavior", rep.splitlines()[0] if rep else "Verified stateless behavior", checks[-1][3])

        # 3. DesktopWpfSpecialist delegation
        if is_test or is_ui_automation or is_audit:
            delegations.append(f"Delegating to {DESKTOP_WPF_NAME} for Desktop static contracts audit...")
            rep = run_check(
                "Desktop static contracts",
                audit_desktop_contracts,
                ("Desktop Static Contracts PASSED",),
            )
            reports["Desktop_Contracts"] = rep
            self.memory.record_step(DESKTOP_WPF_NAME, "audit_desktop_contracts", rep.splitlines()[0] if rep else "Audited desktop contracts", checks[-1][3])

        if is_test:
            delegations.append(f"Delegating to {DESKTOP_WPF_NAME} for the requested test suites...")
            rep = run_check(
                "solution test suites",
                run_solution_tests,
                ("Solution Test Suite PASSED",),
                raw_output=False,
            )
            reports["Solution_Tests"] = rep
            self.memory.record_step(DESKTOP_WPF_NAME, "run_solution_tests", rep.splitlines()[0] if rep else "Ran solution test suites", checks[-1][3])

        if is_ui_automation:
            delegations.append(f"Delegating to {DESKTOP_WPF_NAME} for the requested read-only UI Automation inspection...")
            rep = run_check(
                "Desktop UI Automation",
                run_ui_automation_smoke,
                ("Windows UI Automation Smoke PASSED",),
                raw_output=False,
            )
            reports["Desktop_UIA"] = rep
            self.memory.record_step(DESKTOP_WPF_NAME, "run_ui_automation_smoke", rep.splitlines()[0] if rep else "Inspected Desktop UI Automation", checks[-1][3])

        # 4. DocLedgerAgent delegation
        if is_docs or is_package:
            delegations.append(f"Delegating to {DOC_LEDGER_NAME} for documentation parity and ledger integrity...")
            rep_p = run_check(
                "documentation parity",
                audit_documentation_parity,
                ("All English technical documents have synchronized Spanish counterparts!",),
            )
            reports["Docs_Parity"] = rep_p
            self.memory.record_step(DOC_LEDGER_NAME, "audit_documentation_parity", rep_p.splitlines()[0] if rep_p else "Audited docs parity", checks[-1][3])

            rep_l = run_check(
                "improvement ledger integrity",
                audit_ledger_integrity,
                ("Ledger Integrity Verification PASSED",),
            )
            reports["Ledger_Integrity"] = rep_l
            self.memory.record_step(DOC_LEDGER_NAME, "audit_ledger_integrity", rep_l.splitlines()[0] if rep_l else "Audited ledger integrity", checks[-1][3])

        if is_package_action:
            delegations.append(f"Delegating to {DOC_LEDGER_NAME} for the explicitly requested release packaging workflow...")
            rep = run_check(
                "release packaging workflow",
                run_package_workflow,
                ("Package Workflow SUCCESS",),
                raw_output=False,
            )
            reports["Package_Workflow"] = rep
            self.memory.record_step(DOC_LEDGER_NAME, "run_package_workflow", rep.splitlines()[0] if rep else "Ran the package workflow", checks[-1][3])

        # 5. BugHunterAgent delegation
        if is_bughunt or is_audit:
            delegations.append(f"Delegating to {BUG_HUNTER_NAME} for code smells and concurrency hazards audit...")
            rep_s = run_check(
                "code smell audit",
                audit_code_smells,
                ("Code Smells & Antipatterns Audit PASSED",),
            )
            reports["Code_Smells"] = rep_s
            self.memory.record_step(BUG_HUNTER_NAME, "audit_code_smells", rep_s.splitlines()[0] if rep_s else "Audited code smells", checks[-1][3])

            rep_c = run_check(
                "concurrency hazards audit",
                audit_concurrency_hazards,
                ("Concurrency Hazards Audit PASSED",),
            )
            reports["Concurrency_Hazards"] = rep_c
            self.memory.record_step(BUG_HUNTER_NAME, "audit_concurrency_hazards", rep_c.splitlines()[0] if rep_c else "Audited concurrency hazards", checks[-1][3])

        all_returned = bool(checks) and all(returned for _, _, returned, _ in checks)
        outcomes = [verdict for _, verdict, _, _ in checks]
        if outcomes and all(verdict == "PASS" for verdict in outcomes):
            validation_status = "PASS"
        elif any(verdict == "FAIL" for verdict in outcomes):
            validation_status = "FAIL"
        else:
            validation_status = "INDETERMINATE"
        execution_status = "COMPLETE" if all_returned else "INCOMPLETE"

        # Format synthesized output
        lines = [
            f"=== {MASTER_AGENT_NAME} Multi-Agent Orchestration Report ===",
            f"Task: \"{prompt}\"",
            "Execution Mode: Local Offline Tool Execution",
            f"Execution Route Reason: {route_reason or 'offline local tool execution was selected by the caller'}.",
            f"Configured Specialist Roles: {len(self.subagents)} ({ARCHITECT_AGENT_NAME}, {STATELESS_AUDITOR_NAME}, {DESKTOP_WPF_NAME}, {DOC_LEDGER_NAME}, {BUG_HUNTER_NAME}).",
            "Tool Execution: repository-tool calls run locally in this process.",
            "Cloud-Agent Execution: not used on this route.",
            "",
            "--- Orchestration & Delegation Steps ---",
        ]
        for d in delegations:
            lines.append(f"  -> {d}")
        lines.append("")
        lines.append("--- Specialist Subagent Evidence & Findings ---")

        for title, content in reports.items():
            lines.append(f"\n[{title}]")
            for subline in content.splitlines():
                lines.append(f"  {subline}")

        lines.extend(["", "--- Observed Tool Verdicts ---"])
        for label, verdict, returned, _ in checks:
            suffix = "" if returned else " (tool call raised an exception)"
            lines.append(f"  [{verdict}] {label}{suffix}")

        lines.extend([
            "",
            "--- CoALA Cognitive Memory & Token Telemetry ---",
            self.memory.render_context(),
            "",
            "--- Master Synthesis & Conclusion ---",
            f"Execution Status: {execution_status} ({sum(returned for _, _, returned, _ in checks)}/{len(checks)} tool calls returned reports).",
            f"Validation Status: {validation_status} (only explicit tool verdicts count as passing).",
            "Conclusion: This route reports repository-tool outputs and does not imply unexecuted checks passed.",
            f"Status: {'COMPLETE.' if execution_status == 'COMPLETE' and validation_status == 'PASS' else 'FAILED.' if validation_status == 'FAIL' else 'INDETERMINATE.'}"
        ])

        return "\n".join(lines)


def _classify_tool_report(report: str, success_markers: tuple[str, ...]) -> str:
    """Classify only explicit tool output; an unrecognized report is not a pass."""
    normalized = report.casefold()
    if re.search(r"(?m)^.*\bFAILED\b.*$", report):
        return "FAIL"
    if re.search(r"(?m)^.*\bFAIL(?:ED)?\s*(?:\(|:|\]).*$", report):
        return "FAIL"
    if re.search(r"\berror\s*:|\bexception while\b", normalized):
        return "FAIL"
    failed_count = re.search(
        r"(?im)^\s*(?:RESULT\b[^\r\n]*?\b(\d+)\s+failed\b|Failed:\s*(\d+)\b)",
        report,
    )
    if failed_count is not None and int(failed_count.group(1) or failed_count.group(2)) > 0:
        return "FAIL"
    missing_match = re.search(r"missing spanish translations:\s*(\d+)", normalized)
    if missing_match is not None and int(missing_match.group(1)) > 0:
        return "FAIL"
    if any(marker.casefold() in normalized for marker in success_markers):
        if "synchronized spanish counterparts" in success_markers[0].casefold():
            if missing_match is None or int(missing_match.group(1)) != 0:
                return "INDETERMINATE"
        return "PASS"
    return "INDETERMINATE"
