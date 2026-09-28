"""Validate source and compiled Gauntlet icon assets for the module."""
import argparse
import json
import struct
from pathlib import Path
from xml.etree import ElementTree

ROOT = Path(__file__).resolve().parents[1]
MODULE = ROOT / "modules" / "CalradiaForge"
EXPECTED = {
    "calradiaforge_archery_target",
    "calradiaforge_compass",
    "calradiaforge_crossed_swords",
    "calradiaforge_open_book",
    "calradiaforge_puzzle",
    "calradiaforge_files",
    "calradiaforge_eye_target",
    "calradiaforge_test_tubes",
    "calradiaforge_histogram",
    "calradiaforge_anvil",
    "calradiaforge_plug",
    "calradiaforge_gear_hammer",
    "calradiaforge_gears",
    "calradiaforge_knight_banner",
    "calradiaforge_magnifying_glass",
    "calradiaforge_scroll_unfurled",
    "calradiaforge_stopwatch",
}
CANONICAL_ATLAS_ICONS = {
    "calradiaforge_archery_target",
    "calradiaforge_compass",
    "calradiaforge_crossed_swords",
    "calradiaforge_open_book",
    "calradiaforge_puzzle",
    "calradiaforge_files",
    "calradiaforge_eye_target",
    "calradiaforge_test_tubes",
    "calradiaforge_histogram",
    "calradiaforge_anvil",
    "calradiaforge_plug",
    "calradiaforge_gears",
    "calradiaforge_gear_hammer",
    "calradiaforge_knight_banner",
    "calradiaforge_magnifying_glass",
    "calradiaforge_scroll_unfurled",
    "calradiaforge_stopwatch",
}
CANONICAL_DECORATION_SIZES = {
    "forge_war_table_cloth_v2": (1024, 128),
    "forge_rail_cartographic_field_v1": (256, 256),
    "forge_heraldic_overlay": (256, 48),
    "forge_heraldic_header_v2": (256, 48),
    "forge_heraldic_rail_v2": (128, 256),
    "forge_patina_brass": (128, 16),
    "forge_pine_felt": (128, 32),
}
DECORATION_SIZES = {
    **CANONICAL_DECORATION_SIZES,
    "forge_header_summary_v1": (128, 64),
    "forge_header_modules_v1": (128, 64),
    "forge_header_logs_v1": (128, 64),
    "forge_header_inspector_v1": (128, 64),
    "forge_header_tests_v1": (128, 64),
    "forge_header_metrics_v1": (128, 64),
    "forge_header_framework_v1": (128, 64),
    "forge_header_extensions_v1": (128, 64),
}
CANONICAL_PREFAB_ICONS = {
    "calradiaforge_archery_target",
    "calradiaforge_compass",
    "calradiaforge_crossed_swords",
    "calradiaforge_knight_banner",
    "calradiaforge_scroll_unfurled",
    "calradiaforge_open_book",
    "calradiaforge_puzzle",
    "calradiaforge_files",
    "calradiaforge_eye_target",
    "calradiaforge_test_tubes",
    "calradiaforge_histogram",
    "calradiaforge_anvil",
    "calradiaforge_plug",
}
ATLAS_SPRITES = CANONICAL_ATLAS_ICONS | set(DECORATION_SIZES)
EXPECTED_ATLAS_SPRITE_COUNT = 32
REQUIRED_SPRITES = ATLAS_SPRITES
EXPECTED_SPRITE_COUNT = EXPECTED_ATLAS_SPRITE_COUNT
EXPECTED_ATLAS_SIZE = (4096, 512)
NAVIGATION_ICONS = {
    "ForgeSummary": "calradiaforge_open_book",
    "ForgeModules": "calradiaforge_puzzle",
    "ForgeLogs": "calradiaforge_files",
    "ForgeInspector": "calradiaforge_eye_target",
    "ForgeTests": "calradiaforge_test_tubes",
    "ForgeMetrics": "calradiaforge_histogram",
    "ForgeFramework": "calradiaforge_anvil",
    "ForgeExtensions": "calradiaforge_plug",
}


def read_png_header(path):
    with Path(path).open("rb") as stream:
        header = stream.read(26)
    if len(header) < 26 or header[:8] != b"\x89PNG\r\n\x1a\n" or header[8:12] != b"\x00\x00\x00\x0d" or header[12:16] != b"IHDR":
        raise ValueError("PNG has no readable signature/IHDR header: " + str(path))
    width, height = struct.unpack(">II", header[16:24])
    if not width or not height or width > 0x7fffffff or height > 0x7fffffff:
        raise ValueError("PNG has invalid IHDR dimensions: " + str(path))
    return width, height, header[25]


def validate_resource_workflow_readme(module_path):
    readme_path = Path(module_path) / "GUI" / "SpriteParts" / "README.md"
    if not readme_path.is_file():
        raise ValueError("Sprite source README is missing: " + str(readme_path))
    readme = readme_path.read_text(encoding="utf-8").casefold()
    if "this module includes the resource browser-generated" in readme:
        raise ValueError("Sprite source README claims TPAC readiness without checking the compiled package.")
    required_guidance = (
        "resource browser imports that atlas",
        "tpactool was removed after frequent reader errors",
        "do not install it or use its legacy parser",
        "file presence, hashes, and header/table checks do not decode texture payloads",
        "preserve and hash the imported steam tpac around code deployment",
        "verify the referenced sprites in the running game",
        "add `--installed` to inspect the steam-installed module",
        "a `.meta` sidecar format for driving resource browser imports",
        "no fbx importer or tpac writer is claimed here",
    )
    if not all(phrase in readme for phrase in required_guidance):
        raise ValueError("Sprite source README must explain Resource Browser compilation and the limits of TPAC validation.")


def validate_source_inputs(module_path):
    """Validate authoring inputs without trusting possibly stale generated metadata."""
    module_path = Path(module_path).resolve()
    validate_resource_workflow_readme(module_path)
    prefab = ElementTree.parse(module_path / "GUI" / "Prefabs" / "CalradiaForge.xml").getroot()
    sprite_names = {
        element.attrib.get("Sprite")
        for element in prefab.iter()
        if element.attrib.get("Sprite")
    }
    if not CANONICAL_PREFAB_ICONS.issubset(sprite_names):
        raise ValueError("The Gauntlet prefab is missing one or more canonical Game-icons references.")
    buttons = {element.attrib.get("Id"): element for element in prefab.iter("ButtonWidget")}
    for button_id, sprite_name in NAVIGATION_ICONS.items():
        button = buttons.get(button_id)
        button_sprites = {child.attrib.get("Sprite") for child in button.iter("ImageWidget")} if button is not None else set()
        if sprite_name not in button_sprites:
            raise ValueError("Navigation button %s must use its semantic icon %s." % (button_id, sprite_name))

    config = ElementTree.parse(module_path / "GUI" / "SpriteParts" / "Config.xml").getroot()
    if config.find("./SpriteCategory[@Name='ui_calradiaforge']/AlwaysLoad") is None:
        raise ValueError("The ui_calradiaforge category is not marked always-load.")

    attribution_path = module_path / "GUI" / "SpriteParts" / "ATTRIBUTION.json"
    attributions = json.loads(attribution_path.read_text(encoding="utf-8"))
    if not isinstance(attributions, list):
        raise ValueError("Per-icon attribution must be a JSON array.")
    attributed = {
        item.get("spriteName")
        for item in attributions
        if isinstance(item, dict)
        and item.get("license") == "CC BY 3.0"
        and item.get("artist")
        and item.get("source")
    }
    if len(attributions) != len(EXPECTED) or attributed != EXPECTED:
        raise ValueError("Per-icon attribution is incomplete or does not match the selected icon set.")
    expected_credits = {
        "archery-target": ("Lorc", "https://game-icons.net/1x1/lorc/archery-target.html", "calradiaforge_archery_target"),
        "compass": ("Lorc", "https://game-icons.net/1x1/lorc/compass.html", "calradiaforge_compass"),
        "crossed-swords": ("Lorc", "https://game-icons.net/1x1/lorc/crossed-swords.html", "calradiaforge_crossed_swords"),
        "open-book": ("Lorc", "https://game-icons.net/1x1/lorc/open-book.html", "calradiaforge_open_book"),
        "puzzle": ("Delapouite", "https://game-icons.net/1x1/delapouite/puzzle.html", "calradiaforge_puzzle"),
        "files": ("Delapouite", "https://game-icons.net/1x1/delapouite/files.html", "calradiaforge_files"),
        "eye-target": ("Delapouite", "https://game-icons.net/1x1/delapouite/eye-target.html", "calradiaforge_eye_target"),
        "test-tubes": ("Lorc", "https://game-icons.net/1x1/lorc/test-tubes.html", "calradiaforge_test_tubes"),
        "histogram": ("Delapouite", "https://game-icons.net/1x1/delapouite/histogram.html", "calradiaforge_histogram"),
        "anvil": ("Lorc", "https://game-icons.net/1x1/lorc/anvil.html", "calradiaforge_anvil"),
        "plug": ("Delapouite", "https://game-icons.net/1x1/delapouite/plug.html", "calradiaforge_plug"),
        "gear-hammer": ("Lorc", "https://game-icons.net/1x1/lorc/gear-hammer.html", "calradiaforge_gear_hammer"),
        "gears": ("Lorc", "https://game-icons.net/1x1/lorc/gears.html", "calradiaforge_gears"),
        "knight-banner": ("Delapouite", "https://game-icons.net/1x1/delapouite/knight-banner.html", "calradiaforge_knight_banner"),
        "magnifying-glass": ("Lorc", "https://game-icons.net/1x1/lorc/magnifying-glass.html", "calradiaforge_magnifying_glass"),
        "scroll-unfurled": ("Lorc", "https://game-icons.net/1x1/lorc/scroll-unfurled.html", "calradiaforge_scroll_unfurled"),
        "stopwatch": ("Lorc", "https://game-icons.net/1x1/lorc/stopwatch.html", "calradiaforge_stopwatch"),
    }
    observed_credits = {
        item.get("icon"): (item.get("artist"), item.get("source"), item.get("spriteName"))
        for item in attributions if isinstance(item, dict)
    }
    if observed_credits != expected_credits or any(item.get("license") != "CC BY 3.0" for item in attributions):
        raise ValueError("Per-icon artist, official source URL, sprite name and CC BY 3.0 license must match the pinned icon manifest.")

    resolved_module = module_path.resolve()
    for item in attributions:
        for field in ("moduleSource", "moduleOutput"):
            relative = item.get(field)
            if not relative:
                raise ValueError("Missing %s in icon attribution." % field)
            relative_path = Path(relative.replace("\\", "/"))
            if relative_path.is_absolute() or ".." in relative_path.parts:
                raise ValueError("Unsafe icon attribution path: " + relative)
            candidate = (module_path / relative_path).resolve()
            try:
                candidate.relative_to(resolved_module)
            except ValueError as error:
                raise ValueError("Icon attribution path escapes the module: " + relative) from error
            if not candidate.is_file():
                raise ValueError("Missing icon attribution asset: " + relative)

        source_path = module_path / Path(item["moduleSource"].replace("\\", "/"))
        try:
            source_root = ElementTree.parse(source_path).getroot()
        except ElementTree.ParseError as error:
            raise ValueError("Game-icons source SVG is malformed: " + str(source_path)) from error
        if not source_root.tag.endswith("svg") or not any(element.tag.endswith("path") for element in source_root.iter()):
            raise ValueError("Game-icons source SVG has no SVG root or path data: " + str(source_path))

        output_path = module_path / Path(item["moduleOutput"].replace("\\", "/"))
        part_width, part_height, color_type = read_png_header(output_path)
        if (part_width, part_height) != (96, 96) or color_type != 6:
            raise ValueError("Icon sprite part must be a transparent 96x96 RGBA PNG: " + item["moduleOutput"])

    desktop_icons = ROOT / "src" / "CalradiaForge.Desktop" / "Resources" / "GameIcons.xaml"
    desktop_geometry_key = "{http://schemas.microsoft.com/winfx/2006/xaml}Key"
    desktop_keys = {
        element.attrib.get(desktop_geometry_key)
        for element in ElementTree.parse(desktop_icons).getroot()
        if element.tag.endswith("Geometry")
    }
    expected_desktop_keys = {"GameIcon." + item["icon"].replace("-", "_") for item in attributions}
    if expected_desktop_keys != desktop_keys:
        raise ValueError("Desktop Game-icons dictionary must contain exactly the attributed icon geometries.")


def validate(require_tpac=False, module_path=MODULE, source_prebuild=False):
    module_path = Path(module_path).resolve()
    validate_source_inputs(module_path)
    if source_prebuild:
        # SpriteData and the atlas are outputs of SpriteSheetGenerator. They can
        # legitimately describe the prior source sprites before this build runs.
        return False, 0, 0

    sprite_data = ElementTree.parse(module_path / "GUI" / "CalradiaForgeSpriteData.xml").getroot()
    category = next(
        (item for item in sprite_data.findall("./SpriteCategories/SpriteCategory")
         if item.findtext("Name") == "ui_calradiaforge"),
        None,
    )
    if category is None or category.find("AlwaysLoad") is None:
        raise ValueError("Generated SpriteData must carry the Config.xml AlwaysLoad setting for ui_calradiaforge.")
    categories = [item for item in sprite_data.findall("./SpriteCategories/SpriteCategory")
                  if item.findtext("Name") == "ui_calradiaforge"]
    if len(categories) != 1:
        raise ValueError("SpriteData must contain exactly one ui_calradiaforge sprite category.")
    try:
        sheet_count = int(categories[0].findtext("SpriteSheetCount"))
        sheet_entries = categories[0].findall("SpriteSheetSize")
        sheet_data = [(int(entry.attrib["ID"]), int(entry.attrib["Width"]), int(entry.attrib["Height"]))
                      for entry in sheet_entries]
    except (KeyError, TypeError, ValueError) as error:
        raise ValueError("ui_calradiaforge SpriteData has malformed sheet metadata.") from error
    if sheet_count != 1 or sheet_data != [(1, 4096, 512)]:
        raise ValueError("ui_calradiaforge must use exactly one sheet, ID 1, sized 4096x512.")

    all_parts = sprite_data.findall("./SpriteParts/SpritePart")
    category_parts = [part for part in all_parts if part.findtext("CategoryName") == "ui_calradiaforge"]
    parts = {part.findtext("Name") for part in category_parts}
    sprites = {sprite.findtext("Name") for sprite in sprite_data.findall("./Sprites/GenericSprite")}
    if len(category_parts) != EXPECTED_SPRITE_COUNT or parts != REQUIRED_SPRITES or \
            len(sprites) != EXPECTED_SPRITE_COUNT or sprites != REQUIRED_SPRITES:
        missing_parts = sorted(REQUIRED_SPRITES.difference(parts))
        extra_parts = sorted(parts.difference(REQUIRED_SPRITES))
        missing_sprites = sorted(REQUIRED_SPRITES.difference(sprites))
        extra_sprites = sorted(sprites.difference(REQUIRED_SPRITES))
        raise ValueError("Generated SpriteData must contain exactly {} expected sprites: parts missing={} extra={} count={}; sprites missing={} extra={} count={}".format(
            EXPECTED_SPRITE_COUNT, missing_parts, extra_parts, len(category_parts), missing_sprites, extra_sprites, len(sprites)))

    sheet_sizes = {}
    for sprite_category in sprite_data.findall("./SpriteCategories/SpriteCategory"):
        category_name = sprite_category.findtext("Name")
        if not category_name:
            raise ValueError("SpriteData contains a category without a name.")
        count_text = sprite_category.findtext("SpriteSheetCount")
        try:
            sheet_count = int(count_text)
        except (TypeError, ValueError) as error:
            raise ValueError("SpriteData has an invalid SpriteSheetCount for " + category_name) from error
        if sheet_count < 1:
            raise ValueError("SpriteData has a non-positive SpriteSheetCount for " + category_name)
        category_sheets = {}
        for entry in sprite_category.findall("SpriteSheetSize"):
            try:
                sheet_id = int(entry.attrib["ID"])
                width = int(entry.attrib["Width"])
                height = int(entry.attrib["Height"])
            except (KeyError, TypeError, ValueError) as error:
                raise ValueError("SpriteData has malformed sheet dimensions for " + category_name) from error
            if sheet_id < 1 or width < 1 or height < 1 or sheet_id in category_sheets:
                raise ValueError("SpriteData has invalid or duplicate sheet metadata for " + category_name)
            category_sheets[sheet_id] = (width, height)
        if len(category_sheets) != sheet_count:
            raise ValueError("SpriteData sheet count and dimensions differ for " + category_name)
        sheet_sizes[category_name] = category_sheets

    seen_parts = set()
    rectangles = []
    for part in sprite_data.findall("./SpriteParts/SpritePart"):
        name = part.findtext("Name")
        category_name = part.findtext("CategoryName")
        if name not in REQUIRED_SPRITES:
            continue
        try:
            sheet_id = int(part.findtext("SheetID"))
            width = int(part.findtext("Width"))
            height = int(part.findtext("Height"))
            x = int(part.findtext("SheetX"))
            y = int(part.findtext("SheetY"))
        except (TypeError, ValueError) as error:
            raise ValueError("SpriteData has a part with invalid size or coordinates.") from error
        if not name or not category_name or any(sep in name or sep in category_name for sep in ("/", "\\", ":")) or name in (".", "..") or category_name in (".", ".."):
            raise ValueError("SpriteData contains an unsafe sprite-part path.")
        part_key = (category_name.casefold(), name.casefold())
        if part_key in seen_parts:
            raise ValueError("SpriteData contains a duplicate sprite part: " + category_name + "/" + name)
        seen_parts.add(part_key)
        if width < 1 or height < 1 or x < 0 or y < 0 or sheet_id < 1:
            raise ValueError("SpriteData has invalid size or coordinates for " + name)
        if category_name != "ui_calradiaforge" or sheet_id != 1:
            raise ValueError("Every required SpritePart must use ui_calradiaforge sheet ID 1: " + name)
        expected_decoration_size = DECORATION_SIZES.get(name)
        if expected_decoration_size is not None and (width, height) != expected_decoration_size:
            raise ValueError("Decorative SpriteData dimensions differ for " + name)
        sheet = sheet_sizes.get(category_name, {}).get(sheet_id)
        if sheet is None:
            raise ValueError("Sprite part references a missing category or sheet: " + category_name + "/" + name)
        if x + width > sheet[0] or y + height > sheet[1]:
            raise ValueError("Sprite part exceeds its declared sheet bounds: " + category_name + "/" + name)
        rectangles.append((name, x, y, width, height))
        part_path = module_path / "GUI" / "SpriteParts" / category_name / (name + ".png")
        if not part_path.is_file():
            raise ValueError("SpriteData references a missing source part: " + str(part_path))
        part_width, part_height, _ = read_png_header(part_path)
        if (part_width, part_height) != (width, height):
            raise ValueError("Sprite-part PNG dimensions differ from SpriteData: " + str(part_path))

    for index, (name, x, y, width, height) in enumerate(rectangles):
        for other_name, other_x, other_y, other_width, other_height in rectangles[index + 1:]:
            if x < other_x + other_width and other_x < x + width and \
                    y < other_y + other_height and other_y < y + height:
                raise ValueError("SpriteData atlas rectangles overlap: %s and %s" % (name, other_name))

    for category_name, category_sheets in sheet_sizes.items():
        for sheet_id, expected_size in category_sheets.items():
            atlas_path = module_path / "AssetSources" / "GauntletUI" / (category_name + "_" + str(sheet_id) + ".png")
            if not atlas_path.is_file():
                raise ValueError("SpriteData references a missing source atlas: " + str(atlas_path))
            atlas_width, atlas_height, _ = read_png_header(atlas_path)
            if (atlas_width, atlas_height) != expected_size:
                raise ValueError("Source atlas dimensions differ from SpriteData: " + str(atlas_path))

    atlas = module_path / "AssetSources" / "GauntletUI" / "ui_calradiaforge_1.png"
    width, height, _ = read_png_header(atlas)
    if (width, height) != EXPECTED_ATLAS_SIZE:
        raise ValueError("Generated atlas must be exactly 4096x512 (found %dx%d)." % (width, height))

    tpac = module_path / "Assets" / "GauntletUI" / "ui_calradiaforge_1_tex.tpac"
    if require_tpac and not tpac.is_file():
        raise ValueError("Runtime TPAC is missing. Import ui_calradiaforge_1.png through the Bannerlord Modding Kit Resource Browser.")
    if tpac.is_file():
        with tpac.open("rb") as stream:
            if stream.read(4) != b"TPAC":
                raise ValueError("Runtime texture package does not have a TPAC header: " + str(tpac))
    return tpac.is_file(), width, height


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--require-tpac", action="store_true", help="Fail unless the Resource Browser compiled runtime texture exists.")
    parser.add_argument("--module-path", type=Path, default=MODULE, help="Validate a module directory other than the workspace source module.")
    parser.add_argument("--source-prebuild", action="store_true", help="Validate authoring inputs only; defer SpriteData/atlas consistency until after SpriteSheetGenerator runs.")
    args = parser.parse_args()
    has_tpac, width, height = validate(args.require_tpac, args.module_path, args.source_prebuild)
    if args.source_prebuild:
        print("Validated source prefab, Config.xml, Game-icons SVG/PNG assets, attribution and Desktop geometries.")
        print("Deferred generated SpriteData/atlas consistency checks until after SpriteSheetGenerator.")
        raise SystemExit(0)
    print("Validated %d Game-icons and %d decorative sprites (%d total) in a %dx%d single-sheet atlas." % (len(CANONICAL_ATLAS_ICONS), len(DECORATION_SIZES), EXPECTED_SPRITE_COUNT, width, height))
    print("Runtime TPAC marker: " + ("present (header-only; internal structure not parsed here)" if has_tpac else "pending Resource Browser import"))
    if has_tpac:
        print("Marker check only: preserve the imported Steam TPAC and verify the referenced sprites in the running game.")
