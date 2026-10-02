"""Static tool regressions, runnable without optional cloud SDK dependencies."""
import json
import unittest
from pathlib import Path

from agents.tools import _patch_concurrency_issues, audit_concurrency_hazards
from tools.regenerate_language_resources import reject_duplicate_json_keys


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


class LocalizationGeneratorAuditTests(unittest.TestCase):
    def test_duplicate_keys_are_rejected_before_catalog_generation(self):
        duplicate_json = '{"outer":{"label":"first","label":"second"}}'
        with self.assertRaisesRegex(ValueError, "Duplicate localization JSON key: label"):
            json.loads(duplicate_json, object_pairs_hook=reject_duplicate_json_keys)


if __name__ == "__main__":
    unittest.main()
