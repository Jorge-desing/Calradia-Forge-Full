"""Short fixture tests for the packaged sprite-asset and archive guards."""
import struct
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

from tools.audit_package import validate_entry
from tools import validate_game_icon_assets
from tools.validate_game_icon_assets import read_png_header, validate_resource_workflow_readme


class AssetPipelineTests(unittest.TestCase):
    def test_png_ihdr_dimensions_are_read_from_bounded_header(self):
        header = (
            b"\x89PNG\r\n\x1a\n"
            + struct.pack(">I", 13)
            + b"IHDR"
            + struct.pack(">II", 2048, 256)
            + bytes((8, 6))
        )
        with tempfile.TemporaryDirectory(prefix="CalradiaForge-PngHeader-") as temporary:
            fixture = Path(temporary) / "sprite-header-fixture.png"
            fixture.write_bytes(header)
            self.assertEqual((2048, 256, 6), read_png_header(fixture))

    def test_png_header_rejects_truncated_or_wrong_signature(self):
        with tempfile.TemporaryDirectory(prefix="CalradiaForge-PngHeader-invalid-") as temporary:
            fixture = Path(temporary) / "sprite-header-invalid-fixture.png"
            fixture.write_bytes(b"TPAC" + bytes(30))
            with self.assertRaisesRegex(ValueError, "signature/IHDR"):
                read_png_header(fixture)

    def test_current_icon_validation_parses_prefab_once(self):
        prefab_path = (ROOT / "modules" / "CalradiaForge" / "GUI" / "Prefabs" / "CalradiaForge.xml").resolve()
        original_parse = validate_game_icon_assets.ElementTree.parse
        with patch.object(validate_game_icon_assets.ElementTree, "parse", wraps=original_parse) as parse:
            validate_game_icon_assets.validate()

        prefab_parses = [
            invocation for invocation in parse.call_args_list
            if Path(str(invocation[0][0])).resolve() == prefab_path
        ]
        self.assertEqual(1, len(prefab_parses), "Source validation should reuse its prefab checks instead of reparsing the file.")

    def test_archive_audit_rejects_resource_browser_working_backups(self):
        backup = "CalradiaForge/GUI/CalradiaForgeSpriteData.xml.bak.20260922-182827-921"
        with self.assertRaisesRegex(ValueError, "working backup"):
            validate_entry(backup, desktop=False)

    def test_archive_audit_accepts_release_sprite_assets(self):
        validate_entry("CalradiaForge/GUI/CalradiaForgeSpriteData.xml", desktop=False)
        validate_entry("CalradiaForge/GUI/SpriteParts/ui_calradiaforge/icon.png", desktop=False)

    def test_sprite_workflow_readme_distinguishes_atlas_from_compiled_tpac(self):
        validate_resource_workflow_readme(ROOT / "modules" / "CalradiaForge")

    def test_sprite_workflow_readme_rejects_unverified_tpac_claim(self):
        with tempfile.TemporaryDirectory(prefix="CalradiaForge-README-") as temporary:
            module = Path(temporary)
            readme = module / "GUI" / "SpriteParts" / "README.md"
            readme.parent.mkdir(parents=True)
            readme.write_text(
                "This module includes the Resource Browser-generated runtime TPAC.",
                encoding="utf-8",
            )
            with self.assertRaisesRegex(ValueError, "claims TPAC readiness"):
                validate_resource_workflow_readme(module)

    def test_sprite_workflow_readme_requires_tpactool_and_runtime_rendering_limits(self):
        with tempfile.TemporaryDirectory(prefix="CalradiaForge-README-") as temporary:
            module = Path(temporary)
            readme = module / "GUI" / "SpriteParts" / "README.md"
            readme.parent.mkdir(parents=True)
            source = (ROOT / "modules" / "CalradiaForge" / "GUI" / "SpriteParts" / "README.md").read_text(encoding="utf-8")
            readme.write_text(
                source.replace("TpacTool was removed after frequent reader errors; do not install it or use its legacy parser as an import, deployment, or packaging gate. ", ""),
                encoding="utf-8",
            )
            with self.assertRaisesRegex(ValueError, "Resource Browser compilation and the limits"):
                validate_resource_workflow_readme(module)


if __name__ == "__main__":
    suite = unittest.defaultTestLoader.loadTestsFromTestCase(AssetPipelineTests)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    raise SystemExit(0 if result.wasSuccessful() else 1)
