"""
Unit and integration tests for Calradia Forge Autonomous Agents System.

Validates:
1. Google Antigravity SDK configuration factory.
2. Subagent definitions, system instructions, and tool bindings.
3. Repository domain tools execution.
4. ForgeTokenCompactor semantic output distillation, token metrics, and forensic logging.
5. Retention of representative matching diagnostic lines during tool distillation.
6. ForgeAgentOrchestrator multi-agent delegation pipeline.
7. CLI runner command dispatch.
"""

from __future__ import annotations

import argparse
import unittest
from pathlib import Path

from agents.config import (
    COMPACTION_PRESET_BALANCED,
    COMPACTION_PRESET_DEEP,
    COMPACTION_PRESET_ULTRA,
    COMPACTION_PRESETS,
    ForgeAgentConfig,
    create_forge_agent_config,
    create_sdk_agent_config,
    get_repo_root,
    get_skills_path,
)
from agents.compactor import (
    CompactionStats,
    ForgeTokenCompactor,
    estimate_tokens,
    save_raw_log,
)
from agents.memory import (
    CoALAAgentMemory,
    EpisodicTrace,
    SemanticRepositoryMemory,
    WorkingAgentMemory,
)
from agents.orchestrator import ForgeAgentOrchestrator
from agents.subagents import (
    ARCHITECT_AGENT_NAME,
    BUG_HUNTER_NAME,
    DESKTOP_WPF_NAME,
    DOC_LEDGER_NAME,
    MASTER_AGENT_NAME,
    STATELESS_AUDITOR_NAME,
    get_architect_instructions,
    get_bug_hunter_instructions,
    get_desktop_wpf_instructions,
    get_doc_ledger_instructions,
    get_master_instructions,
    get_stateless_auditor_instructions,
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
)
from tools.run_forge_agents import cmd_compact


class TestForgeAgentConfig(unittest.TestCase):
    """Tests configuration and Google Antigravity SDK factory."""

    def test_repo_and_skills_paths_exist(self) -> None:
        repo_root = get_repo_root()
        skills_path = get_skills_path()
        self.assertTrue(repo_root.exists(), f"Repo root does not exist: {repo_root}")
        self.assertTrue(skills_path.exists(), f"Skills path does not exist: {skills_path}")
        self.assertTrue((repo_root / "CalradiaForge.sln").exists())

    def test_default_config_values(self) -> None:
        cfg = create_forge_agent_config()
        self.assertEqual(cfg.model, "gemini-3.8-flash")
        self.assertEqual(cfg.max_subagent_depth, 2)
        self.assertEqual(cfg.max_model_calls, 30)
        self.assertEqual(cfg.max_tool_calls, 60)
        self.assertEqual(cfg.max_total_tokens, 300_000)
        self.assertEqual(cfg.compaction_preset, COMPACTION_PRESET_DEEP)
        self.assertEqual(cfg.token_threshold, 32_000)
        self.assertFalse(cfg.raw_tools)

    def test_compaction_presets(self) -> None:
        ultra = create_forge_agent_config(compaction_preset=COMPACTION_PRESET_ULTRA)
        self.assertEqual(ultra.token_threshold, 8_000)

        balanced = create_forge_agent_config(compaction_preset=COMPACTION_PRESET_BALANCED)
        self.assertEqual(balanced.token_threshold, 16_000)

        deep = create_forge_agent_config(compaction_preset=COMPACTION_PRESET_DEEP)
        self.assertEqual(deep.token_threshold, 32_000)

    def test_create_sdk_agent_config(self) -> None:
        cfg = create_forge_agent_config()
        subagents = get_subagent_configs()
        sdk_config = create_sdk_agent_config(
            forge_config=cfg,
            tools=ALL_REPO_TOOLS,
            subagents=subagents,
            system_instructions=get_master_instructions(),
        )
        self.assertIsNotNone(sdk_config)
        self.assertEqual(sdk_config.model, "gemini-3.8-flash")
        self.assertEqual(len(sdk_config.subagents), 5)
        self.assertTrue(len(sdk_config.tools) >= 10)
        self.assertTrue(sdk_config.capabilities.enable_subagents)
        self.assertEqual(sdk_config.capabilities.max_subagent_depth, 2)
        self.assertEqual(sdk_config.compaction_config.token_threshold, 32_000)


class TestForgeSubagents(unittest.TestCase):
    """Tests subagents schema and system instructions."""

    def test_subagent_count_and_names(self) -> None:
        subagents = get_subagent_configs()
        self.assertEqual(len(subagents), 5)
        names = [sa.name for sa in subagents]
        self.assertIn(ARCHITECT_AGENT_NAME, names)
        self.assertIn(STATELESS_AUDITOR_NAME, names)
        self.assertIn(DESKTOP_WPF_NAME, names)
        self.assertIn(DOC_LEDGER_NAME, names)
        self.assertIn(BUG_HUNTER_NAME, names)

    def test_instructions_contain_critical_invariants(self) -> None:
        arch_inst = get_architect_instructions()
        self.assertIn("GEMINI.md", arch_inst)
        self.assertIn("Anti-Shadowing", arch_inst)
        self.assertIn("net472", arch_inst)
        self.assertIn("net8.0-windows", arch_inst)

        state_inst = get_stateless_auditor_instructions()
        self.assertIn("INVARIANT RULE B", state_inst)
        self.assertIn("SaveableTypeDefiner", state_inst)
        self.assertIn("SyncData", state_inst)

        wpf_inst = get_desktop_wpf_instructions()
        self.assertIn("VirtualizationMode", wpf_inst)
        self.assertIn("ScrollUnit='Pixel'", wpf_inst)
        self.assertIn("EdgeMode='Aliased'", wpf_inst)
        self.assertIn("RULE C", wpf_inst)

        doc_inst = get_doc_ledger_instructions()
        self.assertIn("Dual-Language Parity", doc_inst)
        self.assertIn("Registro de Mejoras", doc_inst)
        self.assertIn("SHA-256", doc_inst)

        bug_inst = get_bug_hunter_instructions()
        self.assertIn("BugHunterAgent", bug_inst)
        self.assertIn("Code Smell", bug_inst)
        self.assertIn("Concurrency Safety", bug_inst)
        self.assertIn("Rule B", bug_inst)


class TestForgeTools(unittest.TestCase):
    """Tests custom repository tools execution."""

    def test_inspect_csharp_source(self) -> None:
        report = inspect_csharp_source()
        self.assertIn("C# Source Inspection PASSED", report)
        self.assertNotIn("FAILED", report)

    def test_audit_desktop_contracts(self) -> None:
        report = audit_desktop_contracts()
        self.assertIn("Desktop Static Contracts PASSED", report)
        self.assertIn("All 6 mandatory contract tokens verified", report)

    def test_audit_documentation_parity(self) -> None:
        report = audit_documentation_parity()
        self.assertIn("Documentation Parity Audit", report)
        self.assertIn("matched", report)

    def test_audit_ledger_integrity(self) -> None:
        report = audit_ledger_integrity()
        self.assertIn("Ledger Integrity Verification PASSED:", report)
        self.assertIn("Hash Chain Status: 100% Valid", report)

    def test_audit_code_smells(self) -> None:
        report = audit_code_smells()
        self.assertIn("Code Smells", report)
        self.assertIn("PASSED", report)
        self.assertNotIn("FAILED", report)
        raw_report = audit_code_smells(raw=True)
        self.assertIn("Console commands: 100% of ForgeCommands defend against null args.", raw_report)
        self.assertIn("Cognitive memory: Semantic relation facts accurately track GetRelation and LastRelationDelta.", raw_report)
        self.assertIn("Assembly inspection: 100% null-safe assembly reference and version formatting.", raw_report)

    def test_audit_concurrency_hazards(self) -> None:
        report = audit_concurrency_hazards()
        self.assertIn("Concurrency Hazards Audit", report)
        self.assertIn("PASSED", report)
        self.assertNotIn("FAILED", report)
        raw_report = audit_concurrency_hazards(raw=True)
        self.assertIn("GameLocalization: ConcurrentDictionary guarantees thread-safe token caching across threads.", raw_report)


class TestForgeTokenCompactor(unittest.TestCase):
    """Tests representative distillation patterns, diagnostic retention, and optional logging."""

    def setUp(self) -> None:
        ForgeTokenCompactor.reset_history()
        ForgeTokenCompactor.raw_mode = False

    def test_estimate_tokens(self) -> None:
        self.assertEqual(estimate_tokens(""), 0)
        self.assertEqual(estimate_tokens("   "), 0)
        tokens = estimate_tokens("Hello world this is a test string for token estimation.")
        self.assertTrue(10 <= tokens <= 25)

    def test_save_raw_log(self) -> None:
        content = "Line 1: Raw output\nLine 2: Important data\nLine 3: End of log."
        log_rel_path = save_raw_log("unit_test_tool", content)
        repo_root = get_repo_root()
        full_path = repo_root / log_rel_path
        self.assertTrue(full_path.exists(), f"Log file does not exist: {full_path}")
        self.assertEqual(full_path.read_text(encoding="utf-8"), content)

    def test_distill_dotnet_build_success(self) -> None:
        fake_build_lines = ["Build SUCCESS for CalradiaForge.sln [Release]:"]
        for i in range(40):
            fake_build_lines.append(f"  CalradiaForge.Project{i} -> bin/Release/net8.0/Project{i}.dll")
        fake_build_lines.extend([
            "Compilación correcta.",
            "    0 Advertencia(s)",
            "    0 Errores",
            "Tiempo transcurrido 00:00:06.45",
        ])
        raw_build = "\n".join(fake_build_lines)

        distilled, stats = ForgeTokenCompactor.distill("run_dotnet_build", raw_build, save_raw=False)

        self.assertFalse(stats.has_errors)
        self.assertEqual(stats.error_count, 0)
        self.assertIn("Build SUCCESS for CalradiaForge.sln [Release]: 0 errors, 0 warnings", distilled)
        self.assertIn("in 00:00:06.45", distilled)
        self.assertTrue(stats.compression_ratio > 0.70, f"Expected >70% reduction, got {stats.compression_ratio:.1%}")

    def test_distill_dotnet_build_preserves_compiler_errors(self) -> None:
        """Verifies matching compiler diagnostics are retained for a representative build fixture."""
        failed_lines = [
            "Build FAILED for CalradiaForge.sln [Release]:",
            "  Restoring packages...",
            "src/CalradiaForge.Mod/SubModule.cs(42,15): error CS0246: The type or namespace name 'InvalidModel' could not be found",
            "src/CalradiaForge.Core/ForgeLogger.cs(99,8): error CS0103: The name 'unresolvedIdentifier' does not exist in the current context",
            "Compilación con errores.",
            "    0 Advertencia(s)",
            "    2 Errores",
            "Tiempo transcurrido 00:00:03.12",
        ]
        raw_failed = "\n".join(failed_lines)

        distilled, stats = ForgeTokenCompactor.distill("run_dotnet_build", raw_failed, save_raw=False)

        self.assertTrue(stats.has_errors)
        self.assertEqual(stats.error_count, 2)
        # Verify both compiler errors are present verbatim in distilled output
        self.assertIn("SubModule.cs(42,15): error CS0246", distilled)
        self.assertIn("InvalidModel", distilled)
        self.assertIn("ForgeLogger.cs(99,8): error CS0103", distilled)
        self.assertIn("unresolvedIdentifier", distilled)

    def test_distill_solution_tests_success(self) -> None:
        raw_tests = (
            "Ran 8 tests in 0.066s\nOK\n"
            "[Core] Running SDK, ForgeWeave, and module tests through the batch launcher...\n"
            "CalradiaForge.Tests: 239 passed\n"
            "RESULT: 239 passed, 0 failed\n"
            "[ForgeWeave] Running bounded event and replay tests via dotnet and this batch launcher...\n"
            "RESULT: 71 passed, 0 failed\n"
            "[Desktop] Running protocol, tool catalog, and MVVM tests via dotnet and this batch launcher...\n"
            "RESULT: 54 passed, 0 failed\n"
            "PASS 275 WPF render cases; 16917 ms. No game session or tool execution.\n"
            "PERF 150 render/layout passes, 2311.7 ms in those calls; visual-tree snapshots 89 builds / 1282 hits / 119410 visited nodes.\n"
            "All selected Calradia Forge test suites passed."
        )
        distilled, stats = ForgeTokenCompactor.distill("run_solution_tests", raw_tests, save_raw=False)

        self.assertFalse(stats.has_errors)
        self.assertIn("Solution Test Suite PASSED", distilled)
        self.assertIn("Asset Pipeline: 8 tests passed", distilled)
        self.assertIn("Core Systems: 239 passed (0 failed)", distilled)
        self.assertIn("ForgeWeave: 71 passed (0 failed)", distilled)
        self.assertIn("Desktop MVVM: 54 passed (0 failed)", distilled)
        self.assertIn("Desktop Render: 275 cases passed (16917 ms harness time)", distilled)
        self.assertIn("150 layout passes", distilled)
        self.assertIn("119,410 nodes visited", distilled)

    def test_distill_solution_tests_does_not_shift_missing_suite_result(self) -> None:
        raw_tests = (
            "[Core] Running selected tests...\nRESULT: 30 passed, 0 failed\n"
            "[ForgeWeave] Running selected tests...\n"
            "[Desktop] Running selected tests...\nRESULT: 12 passed, 0 failed\n"
            "All selected Calradia Forge test suites passed."
        )
        distilled, stats = ForgeTokenCompactor.distill("run_solution_tests", raw_tests, save_raw=False)

        self.assertFalse(stats.has_errors)
        self.assertIn("Core Systems: 30 passed (0 failed)", distilled)
        self.assertNotIn("ForgeWeave:", distilled)
        self.assertIn("Desktop MVVM: 12 passed (0 failed)", distilled)

    def test_distill_solution_tests_does_not_invent_suite_counts(self) -> None:
        raw_tests = "All selected Calradia Forge test suites passed."
        distilled, stats = ForgeTokenCompactor.distill("run_solution_tests", raw_tests, save_raw=False)

        self.assertFalse(stats.has_errors)
        self.assertIn("Solution Test Suite PASSED for the suites selected by the launcher", distilled)
        self.assertNotIn("Asset Pipeline:", distilled)
        self.assertNotIn("Core Systems:", distilled)
        self.assertNotIn("ForgeWeave:", distilled)
        self.assertNotIn("Desktop", distilled)

    def test_distill_solution_tests_without_overall_status_is_indeterminate(self) -> None:
        raw_tests = "CalradiaForge.Tests: 12 passed\nRESULT: 12 passed, 0 failed"
        distilled, stats = ForgeTokenCompactor.distill("run_solution_tests", raw_tests, save_raw=False)

        self.assertTrue(stats.has_errors)
        self.assertEqual(0, stats.error_count)
        self.assertEqual("INDETERMINATE", stats.status)
        self.assertIn("INDETERMINATE:", stats.summary_line())
        self.assertIn("INDETERMINATE", distilled)
        self.assertNotIn("Solution Test Suite PASSED", distilled)

    def test_distill_solution_tests_failure_preserves_stack_traces(self) -> None:
        raw_failed_tests = (
            "RESULT: 70 passed, 1 failed\n"
            "FAIL ForgeWeave circuit breaker failed to transition to HalfOpen\n"
            "Assert.AreEqual failed. Expected:<HalfOpen>. Actual:<Closed>.\n"
            "   at CalradiaForge.ForgeWeave.Tests.CircuitBreakerTests.TestTransition() in C:\\src\\CircuitBreakerTests.cs:line 85\n"
            "Calradia Forge test run failed with exit code 1."
        )
        distilled, stats = ForgeTokenCompactor.distill("run_solution_tests", raw_failed_tests, save_raw=False)

        self.assertTrue(stats.has_errors)
        self.assertIn("Solution Test Suite FAILED", distilled)
        self.assertIn("FAIL ForgeWeave circuit breaker", distilled)
        self.assertIn("Assert.AreEqual failed", distilled)

    def test_distill_stateless_behavior_preserves_failure(self) -> None:
        raw_state_fail = (
            "=== Stateless CampaignBehavior Acceptance Verification ===\n"
            "[1/4] Checking Release build of CalradiaForge.sln... [OK]\n"
            "[2/4] Checking for zero SaveableTypeDefiner and stateless SyncData...\n"
            "  [FAIL] Prohibited SaveableTypeDefiner inheritance found in MyCorruptBehavior.cs:line 24\n"
        )
        distilled, stats = ForgeTokenCompactor.distill("verify_stateless_behavior", raw_state_fail, save_raw=False)

        self.assertTrue(stats.has_errors)
        self.assertIn("Stateless Campaign Behavior Verification FAILED", distilled)
        self.assertIn("Prohibited SaveableTypeDefiner inheritance", distilled)

    def test_compactor_raw_mode_bypass(self) -> None:
        raw_text = "This is a raw text that should never be compressed when raw_mode is enabled."
        ForgeTokenCompactor.raw_mode = True
        distilled, stats = ForgeTokenCompactor.distill("test_tool", raw_text, save_raw=False)

        self.assertEqual(distilled, raw_text)
        self.assertEqual(stats.tokens_saved, 0)
        self.assertEqual(stats.compression_ratio, 0.0)

    def test_compactor_cumulative_telemetry(self) -> None:
        ForgeTokenCompactor.reset_history()
        self.assertEqual(len(ForgeTokenCompactor.history), 0)

        # Distill two tool outputs
        long_out = "Build SUCCESS for CalradiaForge.sln [Release]:\n" + "\n".join([f"line {i}" for i in range(100)])
        ForgeTokenCompactor.distill("run_dotnet_build", long_out, save_raw=False)
        ForgeTokenCompactor.distill("inspect_csharp_source", "C# Source Inspection PASSED: All 426 C# files adhere to rules.", save_raw=False)

        self.assertEqual(len(ForgeTokenCompactor.history), 2)
        summary = ForgeTokenCompactor.get_cumulative_summary()
        self.assertIn("Token Compaction Cumulative Telemetry (2 tool calls)", summary)
        self.assertIn("Total Raw Context:", summary)
        self.assertIn("Total Compacted Context:", summary)
        self.assertIn("Net Tokens Saved:", summary)

    def test_distill_code_smells_pass_and_fail(self) -> None:
        raw_pass = (
            "Code Smells Audit PASSED:\n"
            "  - Zero string substitution bugs (e.g. proper FormattableString in interpolation).\n"
            "  - Stateless SyncData verified across all campaign behaviors.\n"
            "  - Zero hot-path LINQ queries in simulation ticks.\n"
            "  - Documented exception handling across all catch blocks."
        )
        distilled_pass, stats_pass = ForgeTokenCompactor.distill("audit_code_smells", raw_pass, save_raw=False)
        self.assertFalse(stats_pass.has_errors)
        self.assertIn("Code Smells Audit PASSED", distilled_pass)
        self.assertTrue(stats_pass.tokens_saved >= 0)

        raw_fail = (
            "Code Smells Audit FAILED:\n"
            "  - String substitution bug in SubModule.cs:line 88: string.Format with missing argument.\n"
            "  - Forbidden LINQ query in tick loop: ClanProgression.cs:line 120."
        )
        distilled_fail, stats_fail = ForgeTokenCompactor.distill("audit_code_smells", raw_fail, save_raw=False)
        self.assertTrue(stats_fail.has_errors)
        self.assertEqual(stats_fail.error_count, 2)
        self.assertIn("Code Smells Audit FAILED (2 issues detected)", distilled_fail)
        self.assertIn("String substitution bug in SubModule.cs", distilled_fail)

    def test_distill_concurrency_hazards_pass_and_fail(self) -> None:
        raw_pass = (
            "Concurrency Hazards Audit PASSED:\n"
            "  - ForgeData: ConcurrentDictionary with CAS locks guarantees safe multi-threaded access.\n"
            "  - ForgeAgentMemory: Global SyncRoot guards all agent registrations.\n"
            "  - Desktop PipeClient: SemaphoreSlim gate ensures thread-safe IPC streaming.\n"
            "  - Game Thread Dispatch: Engine calls are strictly marshaled via GameThreadActionDispatch."
        )
        distilled_pass, stats_pass = ForgeTokenCompactor.distill("audit_concurrency_hazards", raw_pass, save_raw=False)
        self.assertFalse(stats_pass.has_errors)
        self.assertIn("Concurrency Hazards Audit PASSED", distilled_pass)

        raw_fail = (
            "Concurrency Hazards Audit FAILED:\n"
            "  - PipeClient.cs does not guard IPC requests with SemaphoreSlim."
        )
        distilled_fail, stats_fail = ForgeTokenCompactor.distill("audit_concurrency_hazards", raw_fail, save_raw=False)
        self.assertTrue(stats_fail.has_errors)
        self.assertEqual(stats_fail.error_count, 1)
        self.assertIn("Concurrency Hazards Audit FAILED (1 hazards detected)", distilled_fail)
        self.assertIn("PipeClient.cs does not guard IPC requests", distilled_fail)

    def test_distill_section_playbooks_pass_and_fail(self) -> None:
        raw_pass = (
            "Section Playbooks Audit PASSED:\n"
            "  - Gauntlet Section Playbooks: 8/8 registered\n"
            "  - Desktop Tactical Studio Trees: 8/8 verified\n"
            "  - ForgePlaybookPanel Prefab: 100% bound"
        )
        distilled_pass, stats_pass = ForgeTokenCompactor.distill("audit_section_playbooks", raw_pass, save_raw=False)
        self.assertFalse(stats_pass.has_errors)
        self.assertIn("Section Playbooks Audit PASSED", distilled_pass)

        raw_fail = (
            "Section Playbooks Audit FAILED:\n"
            "  - Missing Gauntlet section playbook for: KingdomDiplomacy"
        )
        distilled_fail, stats_fail = ForgeTokenCompactor.distill("audit_section_playbooks", raw_fail, save_raw=False)
        self.assertTrue(stats_fail.has_errors)
        self.assertEqual(stats_fail.error_count, 1)
        self.assertIn("Section Playbooks Audit FAILED", distilled_fail)
        self.assertIn("Missing Gauntlet section playbook", distilled_fail)


class TestCoALAAgentMemory(unittest.TestCase):
    """Tests CoALA multi-tiered cognitive memory system."""

    def test_semantic_memory_invariants(self) -> None:
        sem = SemanticRepositoryMemory.get_semantic_context()
        self.assertIsInstance(SemanticRepositoryMemory.INVARIANTS, tuple)
        with self.assertRaises(TypeError):
            SemanticRepositoryMemory.INVARIANTS[0] = "mutable"
        self.assertIn("Rule A (Anti-Shadowing)", sem)
        self.assertIn("Rule B (Statelessness)", sem)
        self.assertIn("Rule C (Desktop Contracts)", sem)
        self.assertIn("Rule D (Distribution Safety)", sem)
        self.assertIn("Single-Threaded Engine", sem)
        self.assertIn("Optional Time-Slicing", sem)
        self.assertIn("buckets can be uneven", sem)
        self.assertIn("filtering still scans the full collection", sem)
        self.assertIn("Measure the complete callback", sem)

    def test_working_memory_lifecycle(self) -> None:
        wm = WorkingAgentMemory(objective="Perform architecture audit")
        self.assertEqual(wm.objective, "Perform architecture audit")
        self.assertEqual(wm.current_step, 0)

        wm.update("ForgeArchitectAgent", "inspect_csharp_source", "Inspected 525 files")
        self.assertEqual(wm.current_step, 1)
        self.assertEqual(wm.active_agent, "ForgeArchitectAgent")
        self.assertEqual(wm.active_tool, "inspect_csharp_source")

        ctx = wm.get_working_context()
        self.assertIn("Goal: Perform architecture audit", ctx)
        self.assertIn("Active Agent: ForgeArchitectAgent (Step #1)", ctx)
        self.assertIn("Active Tool: inspect_csharp_source", ctx)

    def test_episodic_traces_and_error_preservation(self) -> None:
        memory = CoALAAgentMemory(objective="FIFO test", max_episodic_traces=5)

        # Add 4 normal steps
        for i in range(4):
            memory.record_step(f"Agent{i}", f"Action{i}", f"Summary{i}")

        self.assertEqual(len(memory.episodic), 4)

        # Add step with error
        err_stats = CompactionStats(
            tool_name="failing_tool",
            raw_tokens=200,
            compacted_tokens=50,
            tokens_saved=150,
            compression_ratio=0.75,
            has_errors=True,
            error_count=1,
        )
        memory.record_step("BugHunterAgent", "audit_code_smells", "Found syntax bug", stats=err_stats)
        self.assertEqual(len(memory.episodic), 5)
        self.assertTrue(any(t.has_errors for t in memory.episodic))

        # Add 3 more normal steps (exceeding max_episodic_traces = 5)
        for i in range(5, 8):
            memory.record_step(f"Agent{i}", f"Action{i}", f"Summary{i}")

        # Size should remain bounded
        self.assertEqual(len(memory.episodic), 5)
        # CRITICAL: The error trace MUST NOT have been evicted!
        has_error_trace = any(t.action == "audit_code_smells" and t.has_errors for t in memory.episodic)
        self.assertTrue(has_error_trace, "Error trace was pruned prematurely from episodic memory")

    def test_coala_render_context(self) -> None:
        memory = CoALAAgentMemory(objective="End-to-end task")
        memory.record_step(
            agent_name="StatelessAuditor",
            action="verify_stateless_behavior",
            summary="All 4 criteria satisfied",
            stats=CompactionStats(
                tool_name="verify_stateless_behavior",
                raw_tokens=800,
                compacted_tokens=150,
                tokens_saved=650,
                compression_ratio=0.8125,
                has_errors=False,
            ),
        )
        rendered = memory.render_context()
        self.assertIn("Core Semantic Invariants", rendered)
        self.assertIn("Working Memory", rendered)
        self.assertIn("Episodic Memory (1 recent execution traces)", rendered)
        self.assertIn("StatelessAuditor -> verify_stateless_behavior (OK)", rendered)
        self.assertIn("Saved 650 tokens, -81.2%", rendered)
        self.assertIn("Net tokens saved in this episode: 650 (-81.2%)", rendered)


class TestForgeOrchestrator(unittest.TestCase):
    """Tests multi-agent orchestration execution and delegation."""

    def test_offline_orchestrator_execution(self) -> None:
        config = ForgeAgentConfig(offline_mode=True, verbose=False)
        orch = ForgeAgentOrchestrator(config)
        report = orch.run_task_sync("Audit stateless behaviors, bug hunter smells, and verify documentation parity")

        self.assertIn(f"=== {MASTER_AGENT_NAME} Multi-Agent Orchestration Report ===", report)
        self.assertIn("Active Subagents: 5 registered", report)
        self.assertIn("--- Orchestration & Delegation Steps ---", report)
        self.assertIn("Stateless_Behavior", report)
        self.assertIn("Docs_Parity", report)
        self.assertIn("Code_Smells", report)
        self.assertIn("Concurrency_Hazards", report)
        self.assertIn("Status: COMPLETE.", report)

    def test_offline_orchestrator_memory_integration(self) -> None:
        config = ForgeAgentConfig(offline_mode=True, verbose=False)
        orch = ForgeAgentOrchestrator(config)
        report = orch.run_task_sync("Audit stateless behaviors")

        self.assertTrue(len(orch.memory.episodic) >= 1)
        self.assertEqual(orch.memory.working.objective, "Audit stateless behaviors")
        self.assertIn("--- CoALA Cognitive Memory & Token Telemetry ---", report)
        self.assertIn("Core Semantic Invariants", report)

    def test_cmd_compact_execution(self) -> None:
        args = argparse.Namespace(
            raw_tools=False,
            compaction_preset=COMPACTION_PRESET_DEEP,
            live=False,
            model="gemini-3.8-flash",
            offline=True,
            verbose=False,
        )
        exit_code = cmd_compact(args)
        self.assertEqual(exit_code, 0)


if __name__ == "__main__":
    unittest.main()
