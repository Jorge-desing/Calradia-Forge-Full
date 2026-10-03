"""Static tool regressions, runnable without optional cloud SDK dependencies."""
import contextlib
import io
import runpy
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from agents.compactor import ForgeTokenCompactor
from agents.memory import CoALAAgentMemory, SemanticRepositoryMemory
from agents.tools import (
    _patch_concurrency_issues,
    audit_code_smells,
    audit_concurrency_hazards,
    audit_documentation_parity,
)


class PatchManagementAuditTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        sdk = Path(__file__).resolve().parents[1] / "src" / "CalradiaForge.Sdk"
        cls.patcher = (sdk / "Patcher" / "ForgePatcher.cs").read_text(encoding="utf-8")
        cls.detour = (sdk / "ForgeDetour.cs").read_text(encoding="utf-8")

    def test_current_management_checks_pass_with_execution_limit(self):
        self.assertEqual([], _patch_concurrency_issues(self.patcher, self.detour))
        report = audit_concurrency_hazards(raw=True)
        self.assertIn("PASSED", report)
        self.assertIn("do not establish safety", report)

    def test_missing_receipt_lock_is_rejected(self):
        source = self.patcher.replace("lock (syncLock)", "lock (otherGate)")
        self.assertTrue(any("syncLock" in issue for issue in _patch_concurrency_issues(source, self.detour)))

    def test_missing_integrity_delegation_is_rejected(self):
        source = self.patcher.replace("ForgeDetour.Verify(", "Unverified(")
        self.assertTrue(any("integrity" in issue for issue in _patch_concurrency_issues(source, self.detour)))

    def test_missing_pointer_guard_is_rejected(self):
        source = self.detour.replace("address == IntPtr.Zero || replacementAddress == IntPtr.Zero", "false")
        self.assertTrue(any("null target" in issue for issue in _patch_concurrency_issues(self.patcher, source)))

    def test_missing_tracked_registry_lock_is_rejected(self):
        source = self.detour.replace("lock (Gate)", "lock (otherGate)")
        self.assertTrue(any("Gate" in issue for issue in _patch_concurrency_issues(self.patcher, source)))


class AgentMemoryContractTests(unittest.TestCase):
    def test_semantic_repository_constraints_are_immutable(self):
        self.assertIsInstance(SemanticRepositoryMemory.INVARIANTS, tuple)
        with self.assertRaises(TypeError):
            SemanticRepositoryMemory.INVARIANTS[0] = "mutable"
        context = SemanticRepositoryMemory.get_semantic_context()
        self.assertIn("Core Semantic Invariants", context)
        self.assertIn("Rule A (Anti-Shadowing)", context)


class DocumentationParityGateTests(unittest.TestCase):
    @staticmethod
    def run_audit_runner(parity_report):
        runner_path = Path(__file__).resolve().parents[1] / "tools" / "Run-CalradiaForge-Ledger-Audits.py"
        with patch("agents.tools.audit_ledger_integrity", return_value="Ledger Integrity Verification PASSED"), \
             patch("agents.tools.audit_section_playbooks", return_value="Section Playbooks PASSED"), \
             patch("agents.tools.audit_code_smells", return_value="Code Smells PASSED"), \
             patch("agents.tools.audit_documentation_parity", return_value=parity_report), \
             contextlib.redirect_stdout(io.StringIO()):
            module = runpy.run_path(str(runner_path), run_name="ledger_audit_runner_test")
            return module["main"]()

    def test_missing_spanish_counterpart_fails_ledger_gate(self):
        result = self.run_audit_runner("Documentation Parity Audit: 40/41 matched, 1 lacking Spanish translation")
        self.assertEqual(1, result)

    def test_complete_documentation_parity_passes_ledger_gate(self):
        result = self.run_audit_runner(
            "All English technical documents have synchronized Spanish counterparts!"
        )
        self.assertEqual(0, result)

    def test_current_documentation_pairs_are_complete(self):
        report = audit_documentation_parity(raw=True)
        self.assertIn("Missing Spanish translations: 0", report)
        self.assertIn("All English technical documents have synchronized Spanish counterparts!", report)

    def test_english_architecture_and_system_design_each_need_their_own_spanish_pair(self):
        with tempfile.TemporaryDirectory() as temporary:
            docs = Path(temporary) / "docs"
            docs.mkdir()
            for name in (
                "ARCHITECTURE.md",
                "ARCHITECTURE.es.md",
                "SYSTEM_DESIGN.md",
                "SYSTEM_DESIGN.es.md",
            ):
                (docs / name).write_text("# fixture\n", encoding="utf-8")

            with patch("agents.tools._get_repo_root", return_value=Path(temporary)):
                report = audit_documentation_parity(raw=True)

            self.assertIn("Total English docs audited: 2", report)
            self.assertIn("Total Spanish counterparts matched: 2", report)
            self.assertIn("Missing Spanish translations: 0", report)

    def test_system_design_cannot_be_misclassified_as_spanish_architecture(self):
        with tempfile.TemporaryDirectory() as temporary:
            docs = Path(temporary) / "docs"
            docs.mkdir()
            for name in ("ARCHITECTURE.md", "ARCHITECTURE.es.md", "SYSTEM_DESIGN.md"):
                (docs / name).write_text("# fixture\n", encoding="utf-8")

            with patch("agents.tools._get_repo_root", return_value=Path(temporary)):
                report = audit_documentation_parity(raw=True)

            self.assertIn("Total English docs audited: 2", report)
            self.assertIn("Files lacking Spanish translation:", report)
            self.assertIn("docs/SYSTEM_DESIGN.md", report)


class CodeSmellsReportTests(unittest.TestCase):
    def test_linq_check_is_reported_as_static_not_as_an_allocation_measurement(self):
        report = audit_code_smells(raw=True)

        self.assertIn("Static LINQ scan:", report)
        self.assertIn("(not an allocation measurement)", report)
        self.assertNotIn("Hot path allocations:", report)
        self.assertNotIn("zero LINQ queries", report)


class CompactorOutputEvidenceTests(unittest.TestCase):
    def test_unittest_asset_count_is_preserved_when_ok_is_on_the_next_line(self):
        outputs = (
            (8, "Ran 8 tests in 0.066s\nOK\nAll selected Calradia Forge test suites passed."),
            (8, "Ran 8 tests in 0.066s\n\nOK\nAll selected Calradia Forge test suites passed."),
            (1, "Ran 1 test in 0.066s\r\n\r\nOK\r\nAll selected Calradia Forge test suites passed."),
        )

        for expected_count, raw_output in outputs:
            with self.subTest(raw_output=raw_output):
                report, stats = ForgeTokenCompactor.distill(
                    "run_solution_tests",
                    raw_output,
                    save_raw=False,
                )

                self.assertFalse(stats.has_errors)
                self.assertIn(f"Asset Pipeline: {expected_count} tests passed", report)

    def test_success_marker_without_suite_metrics_does_not_invent_counts(self):
        report, stats = ForgeTokenCompactor.distill(
            "run_solution_tests",
            "All selected Calradia Forge test suites passed.",
            save_raw=False,
        )

        self.assertFalse(stats.has_errors)
        self.assertIn("Solution Test Suite PASSED for the suites selected by the launcher", report)
        self.assertNotIn("Asset Pipeline:", report)
        self.assertNotIn("Core Systems:", report)
        self.assertNotIn("ForgeWeave:", report)
        self.assertNotIn("Desktop", report)

    def test_missing_overall_status_is_reported_as_indeterminate(self):
        report, stats = ForgeTokenCompactor.distill(
            "run_solution_tests",
            "CalradiaForge.Tests: 12 passed\nRESULT: 12 passed, 0 failed",
            save_raw=False,
        )

        self.assertTrue(stats.has_errors)  # Retention signal; error_count remains zero.
        self.assertIn("INDETERMINATE", report)
        self.assertNotIn("Solution Test Suite PASSED", report)

        self.assertEqual("INDETERMINATE", stats.status)
        self.assertIn("INDETERMINATE:", stats.summary_line())
        self.assertEqual(0, stats.error_count)

        memory = CoALAAgentMemory(max_episodic_traces=6)
        indeterminate = memory.record_step("ForgeMasterAgent", "run_solution_tests", report, stats)
        self.assertEqual("INDETERMINATE", indeterminate.status)
        self.assertTrue(indeterminate.has_errors)  # Retention signal, not a failed result.
        self.assertIn("(INDETERMINATE):", indeterminate.to_compact_string())
        self.assertNotIn("(FAIL):", indeterminate.to_compact_string())
        self.assertNotIn("(OK):", indeterminate.to_compact_string())
        for index in range(6):
            memory.record_step("ForgeMasterAgent", f"routine_{index}", "routine output")
        self.assertTrue(
            any("Solution Test Suite INDETERMINATE" in trace.summary for trace in memory.episodic)
        )

    def test_actual_fail_traces_remain_fail_and_are_retained(self):
        failure_stats = ForgeTokenCompactor.distill(
            "run_solution_tests",
            "[Core] Running selected tests...\nRESULT: 8 passed, 1 failed\nFAIL case failed\n"
            "Calradia Forge test run failed with exit code 1.",
            save_raw=False,
        )[1]
        self.assertTrue(failure_stats.has_errors)
        self.assertIn("[run_solution_tests] FAIL:", failure_stats.summary_line())

        memory = CoALAAgentMemory(max_episodic_traces=6)
        failure = memory.record_step("ForgeMasterAgent", "run_solution_tests", "one test failed", failure_stats)
        self.assertEqual("FAIL", failure.status)
        self.assertIn("(FAIL):", failure.to_compact_string())
        for index in range(6):
            memory.record_step("ForgeMasterAgent", f"routine_{index}", "routine output")

        self.assertIn(failure, memory.episodic)
        self.assertEqual(6, len(memory.episodic))

    def test_failures_are_not_pruned_when_only_newer_successes_are_available(self):
        failure_stats = ForgeTokenCompactor.distill(
            "run_solution_tests",
            "[Core] Running selected tests...\nRESULT: 8 passed, 1 failed\nFAIL case failed\n"
            "Calradia Forge test run failed with exit code 1.",
            save_raw=False,
        )[1]
        memory = CoALAAgentMemory(max_episodic_traces=6)
        failures = [
            memory.record_step("ForgeMasterAgent", f"failure_{index}", "failed output", failure_stats)
            for index in range(6)
        ]

        memory.record_step("ForgeMasterAgent", "routine", "successful output")

        self.assertEqual(6, len(memory.episodic))
        self.assertTrue(all(trace in memory.episodic for trace in failures))
        self.assertTrue(all("(FAIL):" in trace.to_compact_string() for trace in failures))

    def test_success_phrase_inside_other_text_is_not_a_success_marker(self):
        report, stats = ForgeTokenCompactor.distill(
            "run_solution_tests",
            "Expected output: All selected Calradia Forge test suites passed.",
            save_raw=False,
        )

        self.assertIn("INDETERMINATE", report)
        self.assertEqual("INDETERMINATE", stats.status)
        self.assertTrue(stats.has_errors)

    def test_suite_results_are_bound_to_headers_when_a_suite_is_silent(self):
        raw = (
            "[Core] Running selected tests...\nRESULT: 30 passed, 0 failed\n"
            "[ForgeWeave] Running selected tests...\n"
            "[Desktop] Running selected tests...\nRESULT: 12 passed, 0 failed\n"
            "All selected Calradia Forge test suites passed."
        )

        report, stats = ForgeTokenCompactor.distill("run_solution_tests", raw, save_raw=False)

        self.assertFalse(stats.has_errors)
        self.assertIn("Core Systems: 30 passed (0 failed)", report)
        self.assertNotIn("ForgeWeave:", report)
        self.assertIn("Desktop MVVM: 12 passed (0 failed)", report)


class ContentShowcaseProvenanceTests(unittest.TestCase):
    def test_verify_report_includes_current_native_weapons_source_hash(self):
        generator = (
            Path(__file__).resolve().parents[1]
            / "examples"
            / "CalradiaForge.ContentShowcase"
            / "Generator"
            / "Program.cs"
        ).read_text(encoding="utf-8")
        self.assertIn('"Native weapons.xml SHA-256: "', generator)
        self.assertIn(
            'ComputeSha256(Path.Combine(gameRoot, NativeItemPath.Replace', generator
        )

if __name__ == "__main__":
    unittest.main()
