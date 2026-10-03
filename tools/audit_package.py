"""Audit the three Calradia Forge release archives without extracting them."""
import argparse
import hashlib
import json
import re
from pathlib import Path
from xml.etree import ElementTree
from zipfile import ZipFile


GAME_ICON_LICENSE_URL = "https://creativecommons.org/licenses/by/3.0/"
SOURCE_TEMPLATE_MANIFEST_PATH = (
    "Source/templates/CalradiaForge.Mod.Template/content/"
    "CalradiaForge.ModTemplate/SubModule.xml"
)
SOURCE_TEMPLATE_VERSION = "v1.0.0"
GAME_ICONS = {
    "archery-target": ("Lorc", "https://game-icons.net/1x1/lorc/archery-target.html"),
    "compass": ("Lorc", "https://game-icons.net/1x1/lorc/compass.html"),
    "crossed-swords": ("Lorc", "https://game-icons.net/1x1/lorc/crossed-swords.html"),
    "gear-hammer": ("Lorc", "https://game-icons.net/1x1/lorc/gear-hammer.html"),
    "gears": ("Lorc", "https://game-icons.net/1x1/lorc/gears.html"),
    "knight-banner": ("Delapouite", "https://game-icons.net/1x1/delapouite/knight-banner.html"),
    "magnifying-glass": ("Lorc", "https://game-icons.net/1x1/lorc/magnifying-glass.html"),
    "scroll-unfurled": ("Lorc", "https://game-icons.net/1x1/lorc/scroll-unfurled.html"),
    "stopwatch": ("Lorc", "https://game-icons.net/1x1/lorc/stopwatch.html"),
    "open-book": ("Lorc", "https://game-icons.net/1x1/lorc/open-book.html"),
    "puzzle": ("Delapouite", "https://game-icons.net/1x1/delapouite/puzzle.html"),
    "files": ("Delapouite", "https://game-icons.net/1x1/delapouite/files.html"),
    "eye-target": ("Delapouite", "https://game-icons.net/1x1/delapouite/eye-target.html"),
    "test-tubes": ("Lorc", "https://game-icons.net/1x1/lorc/test-tubes.html"),
    "histogram": ("Delapouite", "https://game-icons.net/1x1/delapouite/histogram.html"),
    "anvil": ("Lorc", "https://game-icons.net/1x1/lorc/anvil.html"),
    "plug": ("Delapouite", "https://game-icons.net/1x1/delapouite/plug.html"),
}


def digest(data):
    return hashlib.sha256(data).hexdigest().upper()


def validate_entry(name, desktop):
    normal = name.replace("\\", "/")
    parts = [part for part in normal.split("/") if part]
    if normal.startswith("/") or ".." in parts or ":" in normal:
        raise ValueError(f"Unsafe archive path: {name}")
    if any(part.lower() in {".venv", "venv", "site-packages", "dist-packages"} for part in parts):
        raise ValueError(f"Python environment or installed dependencies included: {name}")
    if any(part.lower() == "node_modules" for part in parts):
        raise ValueError(f"Node.js dependencies included: {name}")
    lower = normal.lower()
    if lower.endswith(".sav") or "save-backup" in lower:
        raise ValueError(f"Save data included: {name}")
    if re.search(r"\.bak\.\d{8}-\d{6}-\d+$", lower):
        raise ValueError(f"Timestamped working backup included: {name}")
    if lower.endswith(".dll") and Path(normal).name.lower().startswith("taleworlds"):
        raise ValueError(f"Game assembly included: {name}")
    if lower.endswith(".dll") and Path(normal).name.lower() in {
        "0harmony.dll",
        "harmony.dll",
        "lib.harmony.dll",
        "harmonyx.dll",
    }:
        raise ValueError(f"Harmony runtime must remain optional and must not be bundled: {name}")
    if lower.endswith((".bat", ".ps1")):
        allowed_desktop_launcher = desktop and lower == "desktop/run-calradiaforge-desktop.bat"
        if not allowed_desktop_launcher:
            raise ValueError(f"Development or test script included: {name}")
    assembly_name = Path(normal).name.lower()
    if lower.endswith(".dll") and assembly_name.startswith("asmresolver"):
        allowed_module_path = normal.startswith("CalradiaForge/bin/Win64_Shipping_Client/")
        allowed_desktop_path = desktop and normal.startswith("Desktop/")
        if not (allowed_module_path or allowed_desktop_path):
            raise ValueError(f"AsmResolver dependency is outside the game module or Desktop runtime: {name}")
    if lower.endswith(".exe"):
        raise ValueError(f"App-host executable included: {name}")
    if desktop and parts and parts[0] not in {"Desktop", "README.md"}:
        raise ValueError(f"Unexpected Desktop archive root: {name}")


def validate_xml(archive, names):
    count = 0
    for name in names:
        if name.lower().endswith(".xml"):
            try:
                ElementTree.fromstring(archive.read(name))
            except ElementTree.ParseError as error:
                raise ValueError(f"Invalid packaged XML {name}: {error}") from error
            count += 1
    return count


def validate_source_template_manifest(archive, manifest):
    """Validate the source template's placeholders and its template-owned version."""
    try:
        root = ElementTree.fromstring(archive.read(manifest))
    except (KeyError, ElementTree.ParseError) as error:
        raise ValueError(f"Invalid source template manifest {manifest}: {error}") from error

    expected_values = {
        "./Name": "__MODULE_ID__",
        "./Id": "__MODULE_ID__",
        "./Version": SOURCE_TEMPLATE_VERSION,
        "./SubModules/SubModule/Name": "__MODULE_ID__",
        "./SubModules/SubModule/DLLName": "CalradiaForge.ModTemplate.dll",
        "./SubModules/SubModule/SubModuleClassType": "CalradiaForge.ModTemplate.SubModule",
    }
    if root.tag != "Module":
        raise ValueError(f"Invalid source template manifest root in {manifest}: expected Module")
    for xpath, expected in expected_values.items():
        element = root.find(xpath)
        actual = element.get("value") if element is not None else None
        if actual != expected:
            raise ValueError(
                f"Invalid source template manifest {manifest}: {xpath} must preserve {expected!r}, got {actual!r}"
            )

    dependencies = {
        element.get("Id") for element in root.findall("./DependedModules/DependedModule")
    }
    if "CalradiaForge" not in dependencies:
        raise ValueError(f"Invalid source template manifest {manifest}: CalradiaForge dependency is required")


def validate_game_icon_attribution(archive, attribution_path, notice_path):
    """Require the same complete 17-icon attribution in every release archive."""
    try:
        entries = json.loads(archive.read(attribution_path))
    except (KeyError, ValueError, UnicodeDecodeError) as error:
        raise ValueError(f"Invalid Game-icons attribution in {attribution_path}: {error}") from error
    if not isinstance(entries, list):
        raise ValueError(f"Game-icons attribution must be a JSON array: {attribution_path}")

    by_icon = {}
    for item in entries:
        if not isinstance(item, dict):
            raise ValueError(f"Malformed Game-icons attribution entry in {attribution_path}")
        icon = item.get("icon")
        if not isinstance(icon, str) or not icon or icon in by_icon:
            raise ValueError(f"Missing or duplicate Game-icons icon name in {attribution_path}: {icon!r}")
        by_icon[icon] = item
    if set(by_icon) != set(GAME_ICONS):
        missing = sorted(set(GAME_ICONS) - set(by_icon))
        extra = sorted(set(by_icon) - set(GAME_ICONS))
        raise ValueError(
            f"Game-icons attribution set differs in {attribution_path}: missing={missing}, extra={extra}"
        )

    for icon, (expected_artist, expected_source) in GAME_ICONS.items():
        item = by_icon[icon]
        file_name = f"game-icons-{icon}.svg"
        sprite_name = "calradiaforge_" + icon.replace("-", "_")
        expected_values = {
            "file": file_name,
            "artist": expected_artist,
            "source": expected_source,
            "license": "CC BY 3.0",
            "licenseUrl": GAME_ICON_LICENSE_URL,
            "spriteName": sprite_name,
            "moduleSource": f"GUI/IconSources/GameIcons/{file_name}",
            "moduleOutput": f"GUI/SpriteParts/ui_calradiaforge/{sprite_name}.png",
        }
        for field, expected_value in expected_values.items():
            if item.get(field) != expected_value:
                raise ValueError(
                    f"Game-icons attribution {icon!r} has invalid {field!r} in {attribution_path}"
                )
        if not isinstance(item.get("changes"), str) or not item["changes"].strip():
            raise ValueError(f"Game-icons attribution {icon!r} has no changes description in {attribution_path}")

    try:
        notice = archive.read(notice_path).decode("utf-8-sig")
    except (KeyError, UnicodeDecodeError) as error:
        raise ValueError(f"Invalid third-party notice in {notice_path}: {error}") from error
    notice_rows = {}
    for line in notice.splitlines():
        if not line.startswith("|"):
            continue
        cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
        if len(cells) != 5:
            continue
        icon = cells[0].strip("`")
        if icon in GAME_ICONS:
            if icon in notice_rows:
                raise ValueError(f"Duplicate Game-icons notice row for {icon!r} in {notice_path}")
            notice_rows[icon] = cells
    if set(notice_rows) != set(GAME_ICONS):
        raise ValueError(f"Third-party notice must list all 17 Game-icons sources: {notice_path}")
    for icon, (expected_artist, expected_source) in GAME_ICONS.items():
        _name, artist, source, license_notice, changes = notice_rows[icon]
        if artist != expected_artist or expected_source not in source:
            raise ValueError(f"Third-party notice has wrong author or source for {icon!r}: {notice_path}")
        if "CC BY 3.0" not in license_notice or GAME_ICON_LICENSE_URL not in license_notice:
            raise ValueError(f"Third-party notice is missing the CC BY 3.0 license link for {icon!r}")
        if not changes.strip():
            raise ValueError(f"Third-party notice has no changes description for {icon!r}: {notice_path}")

    normalized = json.dumps(entries, sort_keys=True, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
    return {"count": len(entries), "sha256": digest(normalized)}


def validate_game_icon_geometries(archive, dictionary_path):
    try:
        root = ElementTree.fromstring(archive.read(dictionary_path))
    except (KeyError, ElementTree.ParseError) as error:
        raise ValueError(f"Invalid packaged Game-icons geometry dictionary {dictionary_path}: {error}") from error
    xaml_key = "{http://schemas.microsoft.com/winfx/2006/xaml}Key"
    keys = {node.attrib.get(xaml_key) for node in root.iter() if node.attrib.get(xaml_key)}
    expected = {"GameIcon." + icon.replace("-", "_") for icon in GAME_ICONS}
    if not expected.issubset(keys):
        raise ValueError(f"Desktop Game-icons dictionary is missing geometries: {sorted(expected - keys)}")
    return len(expected)


def audit_archive(path, expected_roots, required, version, desktop, required_files=None,
                  attribution_path=None, notice_path=None, geometry_dictionary_path=None):
    with ZipFile(path) as archive:
        names = [name for name in archive.namelist() if not name.endswith("/")]
        if len(names) != len(set(names)):
            raise ValueError(f"Duplicate entries: {path.name}")
        for name in names:
            validate_entry(name, desktop)
            root = name.replace("\\", "/").split("/", 1)[0]
            if root not in expected_roots:
                raise ValueError(f"Unexpected archive root '{root}' in {path.name}")
        missing = sorted(required.difference(names))
        if missing:
            raise ValueError(f"Missing expected content in {path.name}: {', '.join(missing)}")
        toolkit_missing = sorted(set(required_files or ()).difference(names))
        if toolkit_missing:
            raise ValueError(
                f"Missing Desktop toolkit assemblies in {path.name}: {', '.join(toolkit_missing)}"
            )
        invalid = archive.testzip()
        if invalid:
            raise ValueError(f"Invalid ZIP checksum: {invalid}")
        xml_count = validate_xml(archive, names)
        manifests = [name for name in names if name.endswith("/SubModule.xml")]
        for manifest in manifests:
            normalized_manifest = manifest.replace("\\", "/")
            if normalized_manifest == SOURCE_TEMPLATE_MANIFEST_PATH:
                validate_source_template_manifest(archive, manifest)
            elif f'Version value="v{version}"'.encode() not in archive.read(manifest):
                raise ValueError(f"Manifest version mismatch in {manifest}")
        icon_attribution = None
        if attribution_path or notice_path:
            if not attribution_path or not notice_path:
                raise ValueError(f"Both attribution and notice paths are required for {path.name}")
            icon_attribution = validate_game_icon_attribution(archive, attribution_path, notice_path)
        icon_geometry_count = None
        if geometry_dictionary_path:
            icon_geometry_count = validate_game_icon_geometries(archive, geometry_dictionary_path)
        return {
            "file": path.name,
            "sha256": digest(path.read_bytes()),
            "entries": len(names),
            "xmlEntries": xml_count,
            "manifestVersion": version if manifests else None,
            "gameBinariesAbsent": True,
            "saveDataAbsent": True,
            "appHostExeAbsent": True,
            "requiredFilesPresent": sorted(required_files or ()),
            "gameIconAttribution": icon_attribution,
            "gameIconGeometries": icon_geometry_count,
        }


def audit(root, version):
    artifacts = root / "artifacts"
    modules = artifacts / f"CalradiaForge-Modules-{version}.zip"
    source = artifacts / f"CalradiaForge-Source-SDK-{version}.zip"
    desktop = artifacts / f"CalradiaForge-Desktop-{version}.zip"
    for path in (modules, source, desktop):
        if not path.is_file():
            raise ValueError(f"Missing release archive: {path.name}")
    module_roots = {"CalradiaForge", "CalradiaForgeExamples", "CalradiaForgePriceProvider", "CalradiaForgePriceConsumer", "CalradiaForgeContentShowcase"}
    module_icon_files = {
        "CalradiaForge/GUI/SpriteParts/ui_calradiaforge/calradiaforge_{}.png".format(
            icon.replace("-", "_"))
        for icon in GAME_ICONS
    }
    module_icon_sources = {
        "CalradiaForge/GUI/IconSources/GameIcons/game-icons-{}.svg".format(icon)
        for icon in GAME_ICONS
    }
    source_icon_sources = {
        "Source/modules/CalradiaForge/GUI/IconSources/GameIcons/game-icons-{}.svg".format(icon)
        for icon in GAME_ICONS
    }
    results = [audit_archive(modules, module_roots, {
        "CalradiaForge/SubModule.xml",
        "CalradiaForge/bin/Win64_Shipping_Client/CalradiaForge.Mod.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/CalradiaForge.Core.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/CalradiaForge.Sdk.dll",
        "CalradiaForgeExamples/SubModule.xml",
        "CalradiaForgePriceProvider/SubModule.xml",
        "CalradiaForgePriceConsumer/SubModule.xml",
        "CalradiaForgeContentShowcase/SubModule.xml",
        "CalradiaForgeContentShowcase/bin/Win64_Shipping_Client/CalradiaForge.ContentShowcase.dll",
        "CalradiaForgeContentShowcase/GUI/Prefabs/ForgeContentShowcase.xml",
        "CalradiaForgeContentShowcase/ModuleData/calradia_forge_content_showcase_items.xml",
        "CalradiaForgeContentShowcase/ModuleData/calradia_forge_content_showcase_characters.xml",
        "CalradiaForgeContentShowcase/ModuleData/Languages/EN/content_showcase_strings.xml",
        "CalradiaForgeContentShowcase/ModuleData/Languages/EN/language_data.xml",
        "CalradiaForgeContentShowcase/ModuleData/Languages/SP/content_showcase_strings.xml",
        "CalradiaForgeContentShowcase/ModuleData/Languages/SP/language_data.xml",
        "CalradiaForge/THIRD_PARTY_NOTICES.md",
        "CalradiaForge/GUI/SpriteParts/ATTRIBUTION.json",
        "CalradiaForge/GUI/SpriteParts/Config.xml",
        "CalradiaForge/GUI/CalradiaForgeSpriteData.xml",
        "CalradiaForge/AssetSources/GauntletUI/ui_calradiaforge_1.png",
        "CalradiaForge/Assets/GauntletUI/ui_calradiaforge_1_tex.tpac",
    } | module_icon_files | module_icon_sources, version, False, {
        "CalradiaForge/bin/Win64_Shipping_Client/AsmResolver.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/AsmResolver.DotNet.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/AsmResolver.PE.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/AsmResolver.PE.File.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/Mono.Cecil.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/Mono.Cecil.Mdb.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/Mono.Cecil.Pdb.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/Mono.Cecil.Rocks.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/MonoMod.Backports.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/MonoMod.Core.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/MonoMod.Iced.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/MonoMod.ILHelpers.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/MonoMod.RuntimeDetour.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/MonoMod.Utils.dll",
        "CalradiaForge/bin/Win64_Shipping_Client/System.ValueTuple.dll",
    }, attribution_path="CalradiaForge/GUI/SpriteParts/ATTRIBUTION.json",
       notice_path="CalradiaForge/THIRD_PARTY_NOTICES.md")]
    results.append(audit_archive(source, {"Source", "SDK", "README.md"}, {
        "Source/Directory.Build.props",
        "Source/templates/CalradiaForge.Mod.Template/CalradiaForge.Mod.Template.csproj",
        "Source/templates/CalradiaForge.Mod.Template/content/.template.config/template.json",
        "Source/templates/CalradiaForge.Mod.Template/content/CalradiaForge.ModTemplate/CalradiaForge.ModTemplate.csproj",
        "Source/templates/CalradiaForge.Mod.Template/content/CalradiaForge.ModTemplate/SubModule.cs",
        "Source/templates/CalradiaForge.Mod.Template/content/CalradiaForge.ModTemplate/SubModule.xml",
        "Source/examples/CalradiaForge.ContentShowcase/Generator/Program.cs",
        "Source/src/CalradiaForge.Sdk/AnalysisContracts.cs",
        "Source/docs/README.es.md",
        "Source/docs/ASSEMBLY_WORKBENCH.md",
        "Source/docs-site/docfx.json",
        "Source/.config/dotnet-tools.json",
        "Source/src/CalradiaForge.Desktop/Resources/GameIcons/ATTRIBUTION.json",
        "Source/src/CalradiaForge.Desktop/Resources/GameIcons.xaml",
        "Source/THIRD_PARTY_NOTICES.md",
        "SDK/CalradiaForge.Sdk.dll",
        "SDK/CalradiaForge.Sdk.xml",
        "README.md",
    } | source_icon_sources, version, False,
        attribution_path="Source/src/CalradiaForge.Desktop/Resources/GameIcons/ATTRIBUTION.json",
        notice_path="Source/THIRD_PARTY_NOTICES.md",
        geometry_dictionary_path="Source/src/CalradiaForge.Desktop/Resources/GameIcons.xaml"))
    results.append(audit_archive(desktop, {"Desktop", "README.md"}, {
        "Desktop/CalradiaForge.Desktop.dll",
        "Desktop/CalradiaForge.Desktop.deps.json",
        "Desktop/CalradiaForge.Desktop.runtimeconfig.json",
        "Desktop/Run-CalradiaForge-Desktop.bat",
        "Desktop/THIRD_PARTY_NOTICES.md",
        "Desktop/GameIcons-ATTRIBUTION.json",
        "README.md",
    }, version, True, {
        "Desktop/CommunityToolkit.Mvvm.dll",
        "Desktop/MaterialDesignThemes.Wpf.dll",
        "Desktop/AsmResolver.dll",
        "Desktop/AsmResolver.DotNet.dll",
        "Desktop/AsmResolver.PE.dll",
        "Desktop/AsmResolver.PE.File.dll",
    }, attribution_path="Desktop/GameIcons-ATTRIBUTION.json",
       notice_path="Desktop/THIRD_PARTY_NOTICES.md"))
    attribution_hashes = {item["gameIconAttribution"]["sha256"] for item in results}
    if len(attribution_hashes) != 1:
        raise ValueError("Game-icons attribution differs across Modules, Source-SDK, and Desktop archives")
    return results


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", required=True)
    args = parser.parse_args()
    if args.version.count(".") != 2 or not all(part.isdigit() for part in args.version.split(".")):
        parser.error("Version must use major.minor.patch numeric components")
    workspace = Path(__file__).resolve().parents[1]
    result = audit(workspace, args.version)
    output = workspace / "artifacts" / f"package-audit-{args.version.replace('.', '')}.json"
    output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(f"Verified {len(result)} archives; evidence: {output}")
