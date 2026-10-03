"""Short fixture tests for the packaged sprite-asset and archive guards."""
import struct
import sys
import tempfile
import unittest
import importlib.util
from pathlib import Path
from unittest.mock import patch
from xml.etree import ElementTree
from zipfile import ZipFile

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

from tools.audit_package import audit_archive, validate_entry
from tools import validate_game_icon_assets
from tools.validate_game_icon_assets import read_png_header, validate_resource_workflow_readme

_decorative_validator_spec = importlib.util.spec_from_file_location(
    "calradia_forge_decorative_validator",
    ROOT / "tools" / "Validate-CalradiaForge-DecorativeSprites.py",
)
if _decorative_validator_spec is None or _decorative_validator_spec.loader is None:
    raise RuntimeError("Could not load the decorative sprite validator helpers.")
decorative_validator = importlib.util.module_from_spec(_decorative_validator_spec)
sys.modules[_decorative_validator_spec.name] = decorative_validator
_decorative_validator_spec.loader.exec_module(decorative_validator)

SOURCE_TEMPLATE_MANIFEST_PATH = (
    "Source/templates/CalradiaForge.Mod.Template/content/"
    "CalradiaForge.ModTemplate/SubModule.xml"
)
SOURCE_TEMPLATE_MANIFEST = """<Module>
  <Name value="__MODULE_ID__" />
  <Id value="__MODULE_ID__" />
  <Version value="v1.0.0" />
  <DependedModules>
    <DependedModule Id="Native" />
    <DependedModule Id="SandBoxCore" />
    <DependedModule Id="CalradiaForge" />
  </DependedModules>
  <SubModules>
    <SubModule>
      <Name value="__MODULE_ID__" />
      <DLLName value="CalradiaForge.ModTemplate.dll" />
      <SubModuleClassType value="CalradiaForge.ModTemplate.SubModule" />
    </SubModule>
  </SubModules>
</Module>
"""


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

    def test_archive_audit_rejects_python_environments_and_installed_dependencies(self):
        for path in (
            ".venv/Scripts/python.exe",
            "source/tools/venv/Lib/site-packages/PIL/__init__.py",
            "CalradiaForge/tools/dist-packages/yaml/__init__.py",
        ):
            with self.subTest(path=path), self.assertRaisesRegex(ValueError, "Python environment or installed dependencies"):
                validate_entry(path, desktop=False)

    def test_archive_audit_rejects_nested_node_modules_directories(self):
        for path in (
            "Source/tools/vibe-coder-planner-mcp/node_modules/@scope/package/index.js",
            "Desktop/Tools/Nested/NODE_MODULES/package/entry.cmd",
        ):
            with self.subTest(path=path), self.assertRaisesRegex(ValueError, "Node.js dependencies"):
                validate_entry(path, desktop=path.startswith("Desktop/"))

    def test_archive_audit_accepts_python_source_and_requirement_manifests(self):
        validate_entry("tools/validate_game_icon_assets.py", desktop=False)
        validate_entry("requirements-tools.txt", desktop=False)

    def test_archive_audit_rejects_development_and_test_scripts_in_release_archives(self):
        forbidden_entries = (
            "Source/tools/Run-CalradiaForge-Tests.bat",
            "CalradiaForge/tools/Deploy.ps1",
            "Desktop/tools/Test-Desktop.ps1",
            "Desktop/Run-CalradiaForge-Desktop-copy.bat",
        )
        for entry in forbidden_entries:
            with self.subTest(entry=entry), self.assertRaisesRegex(ValueError, "Development or test script"):
                validate_entry(entry, desktop=entry.startswith("Desktop/"))

    def test_archive_audit_allows_only_the_desktop_runtime_launcher(self):
        validate_entry("Desktop/Run-CalradiaForge-Desktop.bat", desktop=True)
        with self.assertRaisesRegex(ValueError, "Development or test script"):
            validate_entry("Desktop/Run-CalradiaForge-Desktop.bat", desktop=False)

    def test_zip_archive_audit_rejects_forbidden_script_entries(self):
        with tempfile.TemporaryDirectory(prefix="CalradiaForge-PackageScripts-") as temporary:
            archive_path = Path(temporary) / "fixture.zip"
            with ZipFile(archive_path, "w") as archive:
                archive.writestr("Source/README.md", "fixture")
                archive.writestr("Source/tests/Run-Tests.bat", "@echo off")

            with self.assertRaisesRegex(ValueError, "Development or test script"):
                audit_archive(
                    archive_path,
                    expected_roots={"Source"},
                    required={"Source/README.md"},
                    version="25.2.0",
                    desktop=False,
                )

    def test_zip_archive_audit_rejects_node_modules_payloads(self):
        with tempfile.TemporaryDirectory(prefix="CalradiaForge-NodeModules-") as temporary:
            archive_path = Path(temporary) / "fixture.zip"
            with ZipFile(archive_path, "w") as archive:
                archive.writestr("Source/README.md", "fixture")
                archive.writestr(
                    "Source/tools/vibe-coder-planner-mcp/node_modules/pkg/index.js",
                    "fixture dependency",
                )

            with self.assertRaisesRegex(ValueError, "Node.js dependencies"):
                audit_archive(
                    archive_path,
                    expected_roots={"Source"},
                    required={"Source/README.md"},
                    version="25.2.0",
                    desktop=False,
                )

    def test_zip_archive_audit_rejects_a_bundled_harmony_runtime(self):
        with tempfile.TemporaryDirectory(prefix="CalradiaForge-HarmonyDependency-") as temporary:
            archive_path = Path(temporary) / "fixture.zip"
            with ZipFile(archive_path, "w") as archive:
                archive.writestr("CalradiaForge/README.md", "The runtime is optional.")
                archive.writestr("CalradiaForge/bin/Win64_Shipping_Client/0Harmony.dll", b"fixture")

            with self.assertRaisesRegex(ValueError, "Harmony runtime must remain optional"):
                audit_archive(
                    archive_path,
                    expected_roots={"CalradiaForge"},
                    required={"CalradiaForge/README.md"},
                    version="25.2.0",
                    desktop=False,
                )

    def test_product_projects_do_not_reference_a_harmony_package_or_assembly(self):
        project_roots = (ROOT / "src", ROOT / "modules", ROOT / "templates")
        projects = [
            path
            for project_root in project_roots
            if project_root.exists()
            for path in project_root.rglob("*.csproj")
            if "bin" not in path.parts and "obj" not in path.parts
        ]
        self.assertTrue(projects, "Expected to inspect maintained product project manifests.")
        for project in projects:
            root = ElementTree.parse(project).getroot()
            for node in root.iter():
                if node.tag.rsplit("}", 1)[-1] not in {"PackageReference", "Reference"}:
                    continue
                identity = node.attrib.get("Include", "") + " " + node.attrib.get("Update", "")
                self.assertNotIn("harmony", identity.casefold(), f"Harmony dependency found in {project}: {identity}")

    def test_source_archive_accepts_starter_template_version_and_current_module_version(self):
        module_manifest_path = "Source/modules/Example/SubModule.xml"
        module_manifest = (
            '<Module><Version value="v25.2.0" /></Module>'
        )
        with tempfile.TemporaryDirectory(prefix="CalradiaForge-TemplateManifest-") as temporary:
            archive_path = Path(temporary) / "source.zip"
            with ZipFile(archive_path, "w") as archive:
                archive.writestr("Source/README.md", "fixture")
                archive.writestr(SOURCE_TEMPLATE_MANIFEST_PATH, SOURCE_TEMPLATE_MANIFEST)
                archive.writestr(module_manifest_path, module_manifest)

            result = audit_archive(
                archive_path,
                expected_roots={"Source"},
                required={"Source/README.md", SOURCE_TEMPLATE_MANIFEST_PATH, module_manifest_path},
                version="25.2.0",
                desktop=False,
            )
            self.assertEqual("25.2.0", result["manifestVersion"])
            self.assertEqual(2, result["xmlEntries"])

    def test_source_template_manifest_requires_its_version_placeholders_and_forge_dependency(self):
        invalid_templates = (
            SOURCE_TEMPLATE_MANIFEST.replace('Version value="v1.0.0"', 'Version value="v25.2.0"'),
            SOURCE_TEMPLATE_MANIFEST.replace('Id value="__MODULE_ID__"', 'Id value="ExampleMod"', 1),
            SOURCE_TEMPLATE_MANIFEST.replace('    <DependedModule Id="CalradiaForge" />\n', ""),
        )
        for template in invalid_templates:
            with self.subTest(template=template), tempfile.TemporaryDirectory(
                prefix="CalradiaForge-TemplateManifest-invalid-"
            ) as temporary:
                archive_path = Path(temporary) / "source.zip"
                with ZipFile(archive_path, "w") as archive:
                    archive.writestr("Source/README.md", "fixture")
                    archive.writestr(SOURCE_TEMPLATE_MANIFEST_PATH, template)

                with self.assertRaisesRegex(ValueError, "Invalid source template manifest"):
                    audit_archive(
                        archive_path,
                        expected_roots={"Source"},
                        required={"Source/README.md", SOURCE_TEMPLATE_MANIFEST_PATH},
                        version="25.2.0",
                        desktop=False,
                    )

    def test_non_template_manifests_still_require_the_current_product_version(self):
        manifest_path = "Source/modules/Example/SubModule.xml"
        with tempfile.TemporaryDirectory(prefix="CalradiaForge-ModuleManifest-version-") as temporary:
            archive_path = Path(temporary) / "source.zip"
            with ZipFile(archive_path, "w") as archive:
                archive.writestr("Source/README.md", "fixture")
                archive.writestr(manifest_path, '<Module><Version value="v1.0.0" /></Module>')

            with self.assertRaisesRegex(ValueError, "Manifest version mismatch"):
                audit_archive(
                    archive_path,
                    expected_roots={"Source"},
                    required={"Source/README.md", manifest_path},
                    version="25.2.0",
                    desktop=False,
                )

    def test_zip_archive_audit_accepts_the_desktop_runtime_launcher(self):
        with tempfile.TemporaryDirectory(prefix="CalradiaForge-DesktopLauncher-") as temporary:
            archive_path = Path(temporary) / "fixture.zip"
            with ZipFile(archive_path, "w") as archive:
                archive.writestr("Desktop/Run-CalradiaForge-Desktop.bat", "@echo off")
                archive.writestr("README.md", "fixture")

            result = audit_archive(
                archive_path,
                expected_roots={"Desktop", "README.md"},
                required={"Desktop/Run-CalradiaForge-Desktop.bat", "README.md"},
                version="25.2.0",
                desktop=True,
            )
            self.assertEqual(2, result["entries"])

    def test_package_source_excludes_repository_python_environment(self):
        package_script = (ROOT / "tools" / "package.ps1").read_text(encoding="utf-8")
        self.assertIn('".venv"', package_script)

    def test_package_staging_filters_scripts_and_preserves_only_the_desktop_launcher(self):
        package_script = (ROOT / "tools" / "package.ps1").read_text(encoding="utf-8")
        self.assertEqual(2, package_script.count('string.Equals(ext, ".bat", StringComparison.OrdinalIgnoreCase)'))
        self.assertEqual(2, package_script.count('string.Equals(ext, ".ps1", StringComparison.OrdinalIgnoreCase)'))
        self.assertIn("Test-ZipEntries $desktopZip -AllowDesktopLauncher", package_script)
        self.assertIn("Desktop/Run-CalradiaForge-Desktop.bat", package_script)

    def test_package_staging_prunes_node_modules_at_any_depth(self):
        package_script = (ROOT / "tools" / "package.ps1").read_text(encoding="utf-8")
        excluded_directory_block = package_script.split("ExcludedDirNames =", 1)[1].split("};", 1)[0]
        self.assertIn('"node_modules"', excluded_directory_block)

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

    def test_decorative_overlap_audit_respects_verified_mutually_exclusive_views(self):
        root = ElementTree.fromstring(
            '<Shell><Widget IsVisible="@ShowCommandDeck"><ImageWidget Id="Decoration" />'
            '</Widget><Widget IsVisible="@IsCampaignRuleBuilderWorkspaceVisible">'
            '<ButtonWidget Id="CampaignRuleAction" /></Widget></Shell>'
        )
        parents = {child: parent for parent in root.iter() for child in parent}
        decoration = next(node for node in root.iter() if node.get("Id") == "Decoration")
        campaign_action = next(node for node in root.iter() if node.get("Id") == "CampaignRuleAction")
        left = decorative_validator.effective_visibility_bindings(decoration, parents)
        right = decorative_validator.effective_visibility_bindings(campaign_action, parents)
        exclusive_pairs = {("ShowCommandDeck", "IsCampaignRuleBuilderWorkspaceVisible")}

        self.assertTrue(decorative_validator.visibility_bindings_are_exclusive(left, right, exclusive_pairs))
        self.assertFalse(decorative_validator.visibility_bindings_are_exclusive(left, left, exclusive_pairs))


if __name__ == "__main__":
    suite = unittest.defaultTestLoader.loadTestsFromTestCase(AssetPipelineTests)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    raise SystemExit(0 if result.wasSuccessful() else 1)
