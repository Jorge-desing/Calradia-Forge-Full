"""Structural checks for the repository-local autonomous-agent CLI launcher."""

from pathlib import Path
from contextlib import redirect_stdout
from io import StringIO
import sys
import unittest
from unittest.mock import patch

from tools.run_forge_agents import main


REPO_ROOT = Path(__file__).resolve().parents[1]
LAUNCHER_PATH = REPO_ROOT / "tools" / "Run-CalradiaForge-Agents.bat"


class AgentCliLauncherTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.launcher = LAUNCHER_PATH.read_text(encoding="utf-8")

    def test_launcher_requires_the_repository_virtual_environment(self) -> None:
        self.assertIn('set "PYTHON=%ROOT%\\.venv\\Scripts\\python.exe"', self.launcher)
        self.assertIn("if not exist \"%PYTHON%\"", self.launcher)
        self.assertIn("tools\\Setup-CalradiaForge-Python.bat", self.launcher)
        self.assertNotIn("where python", self.launcher.casefold())
        self.assertNotIn("py -3.12", self.launcher.casefold())

    def test_launcher_invokes_the_repository_cli_and_forwards_arguments(self) -> None:
        self.assertIn('set "SCRIPT=%ROOT%\\tools\\run_forge_agents.py"', self.launcher)
        self.assertIn('pushd "%ROOT%"', self.launcher)
        self.assertIn('"%PYTHON%" "%SCRIPT%" %*', self.launcher)

    def test_cli_help_describes_offline_execution_and_variable_tool_count(self) -> None:
        for arguments in (["run_forge_agents", "--help"], ["run_forge_agents", "compact", "--help"]):
            with self.subTest(arguments=arguments):
                output = StringIO()
                with patch.object(sys, "argv", list(arguments)), redirect_stdout(output):
                    with self.assertRaises(SystemExit) as exit_result:
                        main()

                self.assertEqual(exit_result.exception.code, 0)
                help_text = " ".join(output.getvalue().casefold().split())
                self.assertIn("force local offline tool execution", help_text)
                self.assertNotIn("deterministic simulation", help_text)
                if arguments[1] == "--help":
                    self.assertIn("token compaction across registered repository tools", help_text)
                self.assertNotRegex(help_text, r"\ball\s+\d+\s+tools\b")


if __name__ == "__main__":
    unittest.main()
