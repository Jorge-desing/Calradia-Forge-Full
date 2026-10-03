"""Regression coverage for truthful local-agent route and verdict reporting."""

from __future__ import annotations

import unittest
from unittest.mock import patch

from agents.compactor import ForgeTokenCompactor
from agents.config import ForgeAgentConfig
from agents.orchestrator import ForgeAgentOrchestrator, _classify_tool_report


PASSING_TOOL_OUTPUTS = {
    "inspect_csharp_source": "C# Source Inspection PASSED: checked fixtures.",
    "verify_stateless_behavior": "Stateless Behavior Verification [PASS]: checked fixtures.",
    "audit_desktop_contracts": "Desktop Static Contracts PASSED: checked fixtures.",
    "audit_documentation_parity": (
        "Documentation Parity Audit (docs/):\n"
        "  Missing Spanish translations: 0\n"
        "All English technical documents have synchronized Spanish counterparts!"
    ),
    "audit_ledger_integrity": "Ledger Integrity Verification PASSED: checked fixtures.",
    "audit_code_smells": "Code Smells & Antipatterns Audit PASSED: checked fixtures.",
    "audit_concurrency_hazards": "Concurrency Hazards Audit PASSED: checked fixtures.",
}


class OrchestratorReportStatusTests(unittest.TestCase):
    def run_mocked_audit(self, *, config: ForgeAgentConfig, sdk_available: bool, overrides: dict[str, str] | None = None) -> str:
        outputs = dict(PASSING_TOOL_OUTPUTS)
        outputs.update(overrides or {})
        target = "agents.orchestrator"
        patches = [patch(f"{target}.SDK_AVAILABLE", sdk_available)]
        patches.extend(
            patch(f"{target}.{name}", return_value=output)
            for name, output in outputs.items()
        )
        for item in patches:
            item.start()
        try:
            return ForgeAgentOrchestrator(config).run_task_sync(
                "Audit stateless behavior, documentation parity, Desktop contracts, and code smells"
            )
        finally:
            for item in reversed(patches):
                item.stop()

    def test_forced_offline_route_is_reported_even_with_credentials_and_sdk(self) -> None:
        report = self.run_mocked_audit(
            config=ForgeAgentConfig(api_key="configured-test-key", offline_mode=True),
            sdk_available=True,
        )

        self.assertIn("Execution Mode: Local Offline Tool Execution", report)
        self.assertIn("Execution Route Reason: offline mode was explicitly requested.", report)
        self.assertIn("Tool Execution: repository-tool calls run locally in this process.", report)
        self.assertIn("Cloud-Agent Execution: not used on this route.", report)
        self.assertNotIn("Antigravity workers", report)
        self.assertNotIn("simulation", report.casefold())
        self.assertIn("Validation Status: PASS", report)
        self.assertIn("Status: COMPLETE.", report)

    def test_missing_sdk_uses_offline_route_even_when_credentials_exist(self) -> None:
        report = self.run_mocked_audit(
            config=ForgeAgentConfig(api_key="configured-test-key", offline_mode=False),
            sdk_available=False,
        )

        self.assertIn("Execution Mode: Local Offline Tool Execution", report)
        self.assertIn("Execution Route Reason: the optional Antigravity SDK is unavailable.", report)
        self.assertIn("Tool Execution: repository-tool calls run locally in this process.", report)
        self.assertIn("Cloud-Agent Execution: not used on this route.", report)
        self.assertIn("Execution Status: COMPLETE", report)

    def test_explicit_failure_prevents_complete_or_pass_claim(self) -> None:
        report = self.run_mocked_audit(
            config=ForgeAgentConfig(offline_mode=True),
            sdk_available=False,
            overrides={
                "audit_desktop_contracts": "Desktop Static Contracts FAILED: missing a required token."
            },
        )

        self.assertIn("[FAIL] Desktop static contracts", report)
        self.assertIn("Validation Status: FAIL", report)
        self.assertIn("Status: FAILED.", report)
        self.assertNotIn("Status: COMPLETE.", report)
        self.assertNotIn("All core invariants", report)

    def test_unrecognized_result_is_indeterminate_not_pass(self) -> None:
        report = self.run_mocked_audit(
            config=ForgeAgentConfig(offline_mode=True),
            sdk_available=False,
            overrides={"audit_code_smells": "Audit completed; no machine-readable verdict was returned."},
        )

        self.assertIn("[INDETERMINATE] code smell audit", report)
        self.assertIn("Validation Status: INDETERMINATE", report)
        self.assertIn("Status: INDETERMINATE.", report)
        self.assertNotIn("Status: COMPLETE.", report)

    def test_documentation_count_with_missing_counterparts_is_failure(self) -> None:
        report = self.run_mocked_audit(
            config=ForgeAgentConfig(offline_mode=True),
            sdk_available=False,
            overrides={
                "audit_documentation_parity": (
                    "Documentation Parity Audit (docs/):\n"
                    "  Missing Spanish translations: 1\n"
                    "Files lacking Spanish translation:\n  - docs/Guide.md"
                )
            },
        )

        self.assertIn("[FAIL] documentation parity", report)
        self.assertIn("Validation Status: FAIL", report)

    def test_zero_failed_count_does_not_turn_a_pass_into_failure(self) -> None:
        verdict = _classify_tool_report(
            "C# Source Inspection PASSED: checked fixtures.\nRESULT: 12 passed, 0 failed",
            ("C# Source Inspection PASSED",),
        )

        self.assertEqual("PASS", verdict)

    def test_nonzero_failed_count_overrides_a_success_marker(self) -> None:
        verdict = _classify_tool_report(
            "Code Smells & Antipatterns Audit PASSED: checked fixtures.\nFailed: 1",
            ("Code Smells & Antipatterns Audit PASSED",),
        )

        self.assertEqual("FAIL", verdict)

    def test_tool_error_prefix_is_a_failure(self) -> None:
        verdict = _classify_tool_report("Error: stateless verifier could not start.", ("PASSED",))

        self.assertEqual("FAIL", verdict)

    def test_raw_compaction_bypass_preserves_output_but_marks_status_unclassified(self) -> None:
        raw = "Solution Test Suite Execution [FAILED (exit 1)]:\nfixture output"

        returned, stats = ForgeTokenCompactor.distill(
            "run_solution_tests",
            raw,
            save_raw=False,
            force_raw=True,
        )

        self.assertEqual(raw, returned)
        self.assertEqual("INDETERMINATE", stats.status)
        self.assertTrue(stats.has_errors)  # Retention signal for an unclassified trace.
        self.assertEqual(0, stats.error_count)
        self.assertIn("INDETERMINATE", stats.summary_line())
        self.assertNotIn(" OK:", stats.summary_line())

    def test_ui_automation_distiller_does_not_invent_passes_or_counts(self) -> None:
        unrecognized, unrecognized_stats = ForgeTokenCompactor.distill(
            "run_ui_automation_smoke",
            "UIA runner emitted an unrecognized report.",
            save_raw=False,
        )
        self.assertIn("INDETERMINATE", unrecognized)
        self.assertEqual("INDETERMINATE", unrecognized_stats.status)
        self.assertTrue(unrecognized_stats.has_errors)
        self.assertNotIn("29/29", unrecognized)

        successful_without_counts, _ = ForgeTokenCompactor.distill(
            "run_ui_automation_smoke",
            "Windows UI Automation Smoke Test [PASSED]:\nRunner completed.",
            save_raw=False,
        )
        self.assertIn("PASSED", successful_without_counts)
        self.assertIn("check count was not reported", successful_without_counts)
        self.assertNotIn("29/29", successful_without_counts)

    def test_ui_automation_distiller_uses_reported_observed_counts(self) -> None:
        distilled, stats = ForgeTokenCompactor.distill(
            "run_ui_automation_smoke",
            "Windows UI Automation Smoke Test [PASSED]:\nRead-only UIA inspection passed: 18/18 observed records.",
            save_raw=False,
        )

        self.assertIn("18/18 checks passed", distilled)
        self.assertNotIn("29/29", distilled)
        self.assertIsNone(stats.status)
        self.assertFalse(stats.has_errors)

    def test_offline_build_and_test_intents_run_the_requested_tools(self) -> None:
        with (
            patch("agents.orchestrator.SDK_AVAILABLE", False),
            patch("agents.orchestrator.inspect_csharp_source", return_value=PASSING_TOOL_OUTPUTS["inspect_csharp_source"]),
            patch("agents.orchestrator.audit_desktop_contracts", return_value=PASSING_TOOL_OUTPUTS["audit_desktop_contracts"]),
            patch("agents.orchestrator.run_dotnet_build", return_value="Build SUCCESS for CalradiaForge.sln [Release]: 0 errors, 0 warnings.") as build_tool,
            patch("agents.orchestrator.run_solution_tests", return_value="Solution Test Suite PASSED for the suites selected by the launcher.") as tests_tool,
        ):
            report = ForgeAgentOrchestrator(ForgeAgentConfig(offline_mode=True)).run_task_sync(
                "Build and run tests"
            )

        build_tool.assert_called_once_with(raw=False)
        tests_tool.assert_called_once_with(raw=False)
        self.assertIn("[PASS] solution build", report)
        self.assertIn("[PASS] solution test suites", report)
        self.assertIn("Validation Status: PASS", report)

    def test_offline_uia_intent_invokes_the_read_only_uia_tool(self) -> None:
        with (
            patch("agents.orchestrator.SDK_AVAILABLE", False),
            patch("agents.orchestrator.audit_desktop_contracts", return_value=PASSING_TOOL_OUTPUTS["audit_desktop_contracts"]),
            patch("agents.orchestrator.run_ui_automation_smoke", return_value="Windows UI Automation Smoke PASSED: 18/18 checks passed.") as uia_tool,
        ):
            report = ForgeAgentOrchestrator(ForgeAgentConfig(offline_mode=True)).run_task_sync("uia")

        uia_tool.assert_called_once_with(raw=False)
        self.assertIn("[PASS] Desktop UI Automation", report)
        self.assertIn("Validation Status: PASS", report)

    def test_offline_package_intent_invokes_packaging_but_audit_does_not(self) -> None:
        with (
            patch("agents.orchestrator.SDK_AVAILABLE", False),
            patch("agents.orchestrator.audit_documentation_parity", return_value=PASSING_TOOL_OUTPUTS["audit_documentation_parity"]),
            patch("agents.orchestrator.audit_ledger_integrity", return_value=PASSING_TOOL_OUTPUTS["audit_ledger_integrity"]),
            patch("agents.orchestrator.run_package_workflow", return_value="Package Workflow SUCCESS: Generated 3 distribution archives in artifacts.") as package_tool,
        ):
            report = ForgeAgentOrchestrator(ForgeAgentConfig(offline_mode=True)).run_task_sync("Package the mod")

        package_tool.assert_called_once_with(raw=False)
        self.assertIn("[PASS] release packaging workflow", report)
        self.assertIn("Validation Status: PASS", report)

        with (
            patch("agents.orchestrator.SDK_AVAILABLE", False),
            patch("agents.orchestrator.inspect_csharp_source", return_value=PASSING_TOOL_OUTPUTS["inspect_csharp_source"]),
            patch("agents.orchestrator.verify_stateless_behavior", return_value=PASSING_TOOL_OUTPUTS["verify_stateless_behavior"]),
            patch("agents.orchestrator.audit_desktop_contracts", return_value=PASSING_TOOL_OUTPUTS["audit_desktop_contracts"]),
            patch("agents.orchestrator.audit_documentation_parity", return_value=PASSING_TOOL_OUTPUTS["audit_documentation_parity"]),
            patch("agents.orchestrator.audit_ledger_integrity", return_value=PASSING_TOOL_OUTPUTS["audit_ledger_integrity"]),
            patch("agents.orchestrator.audit_code_smells", return_value=PASSING_TOOL_OUTPUTS["audit_code_smells"]),
            patch("agents.orchestrator.audit_concurrency_hazards", return_value=PASSING_TOOL_OUTPUTS["audit_concurrency_hazards"]),
            patch("agents.orchestrator.run_package_workflow") as forbidden_package_tool,
        ):
            ForgeAgentOrchestrator(ForgeAgentConfig(offline_mode=True)).run_task_sync("Audit the package workflow")

        forbidden_package_tool.assert_not_called()

    def test_tool_exception_marks_execution_incomplete_and_validation_failed(self) -> None:
        output_map = dict(PASSING_TOOL_OUTPUTS)
        target = "agents.orchestrator"
        patches = [patch(f"{target}.SDK_AVAILABLE", False)]
        patches.extend(
            patch(f"{target}.{name}", return_value=output)
            for name, output in output_map.items()
            if name != "audit_desktop_contracts"
        )
        patches.append(patch(f"{target}.audit_desktop_contracts", side_effect=RuntimeError("fixture crash")))
        for item in patches:
            item.start()
        try:
            report = ForgeAgentOrchestrator(ForgeAgentConfig(offline_mode=True)).run_task_sync(
                "Audit stateless behavior, documentation parity, Desktop contracts, and code smells"
            )
        finally:
            for item in reversed(patches):
                item.stop()

        self.assertIn("[FAIL] Desktop static contracts (tool call raised an exception)", report)
        self.assertIn("Execution Status: INCOMPLETE", report)
        self.assertIn("Validation Status: FAIL", report)
        self.assertNotIn("Status: COMPLETE.", report)


if __name__ == "__main__":
    unittest.main()
