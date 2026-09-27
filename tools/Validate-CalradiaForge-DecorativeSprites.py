#!/usr/bin/env python3
"""Validate the decorative Gauntlet sprite source assets (Python stdlib only).

This checks source PNG content and metadata, not game rendering. Runtime atlas
and TPAC evidence is reported separately and is never described as pixel-decoded.
"""
from __future__ import annotations

import struct
import sys
import zlib
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Optional, Tuple
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
GUI = ROOT / "modules" / "CalradiaForge" / "GUI"
SPRITES = GUI / "SpriteParts" / "ui_calradiaforge"
SPRITE_DATA = GUI / "CalradiaForgeSpriteData.xml"
CONFIG = GUI / "SpriteParts" / "Config.xml"
PREFAB = GUI / "Prefabs" / "CalradiaForge.xml"
PANEL_VIEW_MODEL = ROOT / "src" / "CalradiaForge.Mod" / "PanelViewModel.cs"
SOURCE_ATLAS = ROOT / "modules" / "CalradiaForge" / "AssetSources" / "GauntletUI" / "ui_calradiaforge_1.png"
TPAC = ROOT / "modules" / "CalradiaForge" / "Assets" / "GauntletUI" / "ui_calradiaforge_1_tex.tpac"
CATEGORY = "ui_calradiaforge"
DECORATION_SIZES = {
    "forge_war_table_cloth_v2": (1024, 128),
    "forge_rail_cartographic_field_v1": (256, 256),
    "forge_heraldic_overlay": (256, 48),
    "forge_patina_brass": (128, 16),
    "forge_pine_felt": (128, 32),
    "forge_header_summary_v1": (128, 64),
    "forge_header_modules_v1": (128, 64),
    "forge_header_logs_v1": (128, 64),
    "forge_header_inspector_v1": (128, 64),
    "forge_header_tests_v1": (128, 64),
    "forge_header_metrics_v1": (128, 64),
    "forge_header_framework_v1": (128, 64),
    "forge_header_extensions_v1": (128, 64),
}
DECORATIONS = tuple(DECORATION_SIZES)
OCCLUDING_PANEL_IDS = {"ForgeKeyHelpPanel", "ForgeSdkCatalogPanel", "NavigationPalettePanel", "ForgePlaybookPanel"}
MAX_ALPHA = {
    "forge_war_table_cloth_v2": 24,
    "forge_rail_cartographic_field_v1": 36,
    "forge_heraldic_overlay": 112,
    "forge_patina_brass": 88,
    "forge_pine_felt": 40,
    "forge_header_summary_v1": 112,
    "forge_header_modules_v1": 112,
    "forge_header_logs_v1": 112,
    "forge_header_inspector_v1": 112,
    "forge_header_tests_v1": 112,
    "forge_header_metrics_v1": 112,
    "forge_header_framework_v1": 112,
    "forge_header_extensions_v1": 112,
}
MIN_VISIBLE_COLORS = {name: 2 for name in DECORATIONS}
TEXT_OVERLAP_ALPHA_MAX = {
    "forge_war_table_cloth_v2": 24,
    "forge_rail_cartographic_field_v1": 36,
}
TRANSPARENT_DECORATIONS = {
    "forge_heraldic_overlay",
    "forge_header_summary_v1",
    "forge_header_modules_v1",
    "forge_header_logs_v1",
    "forge_header_inspector_v1",
    "forge_header_tests_v1",
    "forge_header_metrics_v1",
    "forge_header_framework_v1",
    "forge_header_extensions_v1",
}
EDGE_ART_DECORATIONS = {"forge_heraldic_overlay"}
RETIRED_DECORATIONS = {
    "forge_header_filigree",
    "forge_map_contours",
    "forge_heraldic_corner",
    "forge_woven_border",
    "forge_rosette_mark",
    "forge_stitch_rule",
    "forge_pine_grain",
    "forge_table_grain",
    "forge_brass_rule",
    "forge_dark_wood",
    "forge_inkwash",
    "forge_war_table_cloth",
}
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"
MAX_PNG_BYTES = 32 * 1024 * 1024
MAX_PIXELS = 16_000_000
MAX_DECODED_BYTES = 96 * 1024 * 1024


class ValidationError(Exception):
    pass


@dataclass(frozen=True)
class PngFacts:
    width: int
    height: int
    color_type: int
    bit_depth: int
    alpha_min: int
    alpha_max: int
    visible_pixels: int
    distinct_visible_colors: int
    left_edge_visible_pixels: int
    right_edge_visible_pixels: int


@dataclass(frozen=True)
class Rect:
    x: float
    y: float
    width: float
    height: float

    def intersects(self, other: "Rect") -> bool:
        return (self.x < other.x + other.width and other.x < self.x + self.width
                and self.y < other.y + other.height and other.y < self.y + self.height)

    def contains(self, other: "Rect") -> bool:
        return (self.x <= other.x and self.y <= other.y
                and self.x + self.width >= other.x + other.width
                and self.y + self.height >= other.y + other.height)


def rect_intersection(first: Rect, second: Rect) -> Optional[Rect]:
    left, top = max(first.x, second.x), max(first.y, second.y)
    right = min(first.x + first.width, second.x + second.width)
    bottom = min(first.y + first.height, second.y + second.height)
    if right <= left or bottom <= top:
        return None
    return Rect(left, top, right - left, bottom - top)


# These placements keep the passive ImageGen-derived textures inside known
# surfaces and out of controls, editable fields, and evidence.
EXPECTED_DECORATIVE_PLACEMENTS = {
    # Positions are validated from the responsive prefab at a concrete viewport;
    # keeping fixed design-time rectangles here would reintroduce 1220x880 assumptions.
    "ForgeHeaderCloth": ("forge_war_table_cloth_v2", "ForgeHeader", None),
    "ForgeRailCloth": ("forge_rail_cartographic_field_v1", "ForgeNavigationRail", None),
    "ForgeHeaderHeraldicOverlay": ("forge_heraldic_overlay", "ForgeHeader", None),
    "ForgeTopBrassFrameRule": ("forge_patina_brass", "<shell>", None),
    "ForgeEvidenceActionBrassRule": ("forge_patina_brass", "<shell>", None),
    "ForgeBottomBrassFrameRule": ("forge_patina_brass", "<shell>", None),
    "ForgeRailPineFelt": ("forge_pine_felt", "ForgeNavigationRail", None),
    "ForgeBriefingContextPineFelt": ("forge_pine_felt", "BriefingContextValue", None),
    "ForgeBriefingTestingPineFelt": ("forge_pine_felt", "BriefingTestingValue", None),
    "ForgeBriefingEvidencePineFelt": ("forge_pine_felt", "BriefingEvidenceCountValue", None),
    "ForgeBriefingTestsPineFelt": ("forge_pine_felt", "BriefingTestCountValue", None),
    "ForgeBriefingContextPatinaRule": ("forge_patina_brass", "BriefingContextValue", None),
    "ForgeBriefingTestingPatinaRule": ("forge_patina_brass", "BriefingTestingValue", None),
    "ForgeBriefingEvidencePatinaRule": ("forge_patina_brass", "BriefingEvidenceCountValue", None),
    "ForgeBriefingTestsPatinaRule": ("forge_patina_brass", "BriefingTestCountValue", None),
    "ForgePlaybookBrassRule1": ("forge_patina_brass", "ForgePlaybookFlow", None),
    "ForgePlaybookFeltRule": ("forge_pine_felt", "ForgePlaybookFlow", None),
    "ForgePlaybookBrassRule2": ("forge_patina_brass", "ForgePlaybookFlow", None),
}

PLAYBOOK_FLOW_RULE_IDS = {
    "ForgePlaybookBrassRule1", "ForgePlaybookFeltRule", "ForgePlaybookBrassRule2",
}
PLAYBOOK_FLOW_ORDER = (
    "ForgePlaybookTitle", "ForgePlaybookBrassRule1", "ForgePlaybookStep1",
    "ForgePlaybookStep2", "ForgePlaybookStep3", "ForgePlaybookFeltRule",
    "ForgeTroubleshootingTitle", "ForgeTroubleshootingAdvice", "ForgePlaybookBrassRule2",
    "ForgeRecommendedMacro", "ForgeRunMacro",
)

ROUTE_HEADER_ORNAMENT_BINDINGS = {
    "ForgeHeaderOrnamentSummary": ("forge_header_summary_v1", "@IsSummaryActive"),
    "ForgeHeaderOrnamentModules": ("forge_header_modules_v1", "@IsModulesActive"),
    "ForgeHeaderOrnamentLogs": ("forge_header_logs_v1", "@IsLogsActive"),
    "ForgeHeaderOrnamentInspector": ("forge_header_inspector_v1", "@IsInspectorActive"),
    "ForgeHeaderOrnamentTests": ("forge_header_tests_v1", "@IsTestsActive"),
    "ForgeHeaderOrnamentMetrics": ("forge_header_metrics_v1", "@IsMetricsActive"),
    "ForgeHeaderOrnamentFramework": ("forge_header_framework_v1", "@IsFrameworkActive"),
    "ForgeHeaderOrnamentExtensions": ("forge_header_extensions_v1", "@IsExtensionsActive"),
}
for _element_id, (_sprite_name, _visibility_binding) in ROUTE_HEADER_ORNAMENT_BINDINGS.items():
    EXPECTED_DECORATIVE_PLACEMENTS[_element_id] = (
        _sprite_name, "<shell>", Rect(256, 114, 24, 12))

DECORATIVE_LAYOUT_CONTRACTS = {
    "ForgeHeaderCloth": {
        "HorizontalAlignment": "Center", "WidthSizePolicy": "StretchToParent",
        "HeightSizePolicy": "Fixed", "SuggestedHeight": 76, "MaxWidth": 1172,
    },
    "ForgeRailCloth": {
        "WidthSizePolicy": "Fixed", "HeightSizePolicy": "StretchToParent",
        "SuggestedWidth": 228, "MaxHeight": 256,
        "MarginLeft": 1, "MarginTop": 425, "MarginBottom": 5,
    },
    "ForgePlaybookBrassRule1": {
        "WidthSizePolicy": "StretchToParent", "SuggestedHeight": 3,
        "MarginLeft": 12, "MarginRight": 12, "MarginTop": 6,
    },
    "ForgePlaybookFeltRule": {
        "WidthSizePolicy": "StretchToParent", "SuggestedHeight": 3,
        "MarginLeft": 12, "MarginRight": 12, "MarginTop": 8,
    },
    "ForgePlaybookBrassRule2": {
        "WidthSizePolicy": "StretchToParent", "SuggestedHeight": 3,
        "MarginLeft": 12, "MarginRight": 12, "MarginTop": 8,
    },
}

CARD_TOP_STRIP_IDS = {
    "ForgeBriefingContextPineFelt",
    "ForgeBriefingTestingPineFelt",
    "ForgeBriefingEvidencePineFelt",
    "ForgeBriefingTestsPineFelt",
}
CARD_BOTTOM_RULE_IDS = {
    "ForgeBriefingContextPatinaRule",
    "ForgeBriefingTestingPatinaRule",
    "ForgeBriefingEvidencePatinaRule",
    "ForgeBriefingTestsPatinaRule",
}

def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def child_text(node: ET.Element, name: str) -> str:
    child = next((item for item in node if local_name(item.tag) == name), None)
    return (child.text or "").strip() if child is not None else ""


def int_child(node: ET.Element, name: str) -> Optional[int]:
    try:
        return int(child_text(node, name))
    except ValueError:
        return None


def unfilter(filter_type: int, encoded: bytes, prior: bytes, bpp: int) -> bytes:
    row = bytearray(encoded)
    if filter_type == 0:
        return bytes(row)
    if filter_type not in (1, 2, 3, 4):
        raise ValidationError("PNG uses an unsupported scanline filter")
    for i in range(len(row)):
        left = row[i - bpp] if i >= bpp else 0
        up = prior[i] if prior else 0
        upper_left = prior[i - bpp] if prior and i >= bpp else 0
        if filter_type == 1:
            predictor = left
        elif filter_type == 2:
            predictor = up
        elif filter_type == 3:
            predictor = (left + up) // 2
        else:
            p = left + up - upper_left
            pa, pb, pc = abs(p - left), abs(p - up), abs(p - upper_left)
            predictor = left if pa <= pb and pa <= pc else (up if pb <= pc else upper_left)
        row[i] = (row[i] + predictor) & 255
    return bytes(row)


def sample(row: bytes, index: int, depth: int) -> int:
    if depth == 8:
        return row[index]
    if depth == 16:
        pos = index * 2
        return row[pos] * 256 + row[pos + 1]
    bit = index * depth
    return (row[bit // 8] >> (8 - depth - bit % 8)) & ((1 << depth) - 1)


def scale(value: int, depth: int) -> int:
    if depth == 16:
        return value >> 8
    maximum = (1 << depth) - 1
    return (value * 255 + maximum // 2) // maximum


def inspect_png(path: Path) -> PngFacts:
    if not path.is_file():
        raise ValidationError("source PNG is missing")
    if not (57 <= path.stat().st_size <= MAX_PNG_BYTES):
        raise ValidationError("PNG size is outside the accepted validation bounds")
    data = path.read_bytes()
    if not data.startswith(PNG_SIGNATURE):
        raise ValidationError("invalid PNG signature")
    chunks: List[Tuple[bytes, bytes]] = []
    offset = len(PNG_SIGNATURE)
    saw_end = False
    while offset < len(data):
        if offset + 12 > len(data):
            raise ValidationError("truncated PNG chunk header")
        length = struct.unpack_from(">I", data, offset)[0]
        kind = data[offset + 4:offset + 8]
        end = offset + 12 + length
        if end > len(data):
            raise ValidationError("truncated PNG chunk body")
        body = data[offset + 8:offset + 8 + length]
        stored_crc = struct.unpack_from(">I", data, offset + 8 + length)[0]
        if (zlib.crc32(kind + body) & 0xffffffff) != stored_crc:
            raise ValidationError("PNG chunk CRC mismatch")
        chunks.append((kind, body))
        offset = end
        if kind == b"IEND":
            if length != 0:
                raise ValidationError("invalid IEND chunk")
            saw_end = True
            break
    if not saw_end or offset != len(data):
        raise ValidationError("missing IEND or trailing PNG data")
    if not chunks or chunks[0][0] != b"IHDR" or len(chunks[0][1]) != 13:
        raise ValidationError("PNG does not begin with a valid IHDR")

    width, height, depth, color_type, compression, filter_method, interlace = struct.unpack(">IIBBBBB", chunks[0][1])
    if not width or not height or width > 8192 or height > 8192 or width * height > MAX_PIXELS:
        raise ValidationError("PNG dimensions are empty or exceed validation limits")
    if compression or filter_method or interlace:
        raise ValidationError("PNG compression/filter/interlace mode is unsupported")
    channels = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}.get(color_type)
    depths = {0: (1, 2, 4, 8, 16), 2: (8, 16), 3: (1, 2, 4, 8), 4: (8, 16), 6: (8, 16)}
    if channels is None or depth not in depths[color_type]:
        raise ValidationError("PNG color type/bit depth is unsupported")
    palette = next((body for kind, body in chunks if kind == b"PLTE"), None)
    transparency = next((body for kind, body in chunks if kind == b"tRNS"), None)
    if color_type == 3 and (not palette or len(palette) % 3):
        raise ValidationError("indexed PNG has no valid PLTE")
    if color_type == 3 and transparency is not None and len(transparency) > len(palette) // 3:
        raise ValidationError("indexed PNG transparency exceeds its palette")
    if color_type in (0, 2) and transparency is not None:
        expected = 2 if color_type == 0 else 6
        if len(transparency) != expected:
            raise ValidationError("PNG transparency key has invalid length")
    idat = b"".join(body for kind, body in chunks if kind == b"IDAT")
    if not idat:
        raise ValidationError("PNG has no IDAT image data")
    row_bytes = (width * channels * depth + 7) // 8
    expected_size = (row_bytes + 1) * height
    if expected_size > MAX_DECODED_BYTES:
        raise ValidationError("decoded image would exceed validation memory bounds")
    decoder = zlib.decompressobj()
    decoded = decoder.decompress(idat, expected_size + 1)
    if len(decoded) <= expected_size and not decoder.unconsumed_tail:
        decoded += decoder.flush()
    if len(decoded) != expected_size or not decoder.eof or decoder.unused_data:
        raise ValidationError("PNG image stream is truncated, oversized, or malformed")

    key = None
    if color_type == 0 and transparency is not None:
        key = (struct.unpack(">H", transparency)[0],)
    elif color_type == 2 and transparency is not None:
        key = struct.unpack(">HHH", transparency)
    palette_count = len(palette or b"") // 3
    alpha_min, alpha_max, visible = 255, 0, 0
    left_edge_visible = right_edge_visible = 0
    colors = set()
    previous = b""
    cursor = 0
    bpp = max(1, (channels * depth + 7) // 8)
    for _ in range(height):
        filter_type = decoded[cursor]
        cursor += 1
        row = unfilter(filter_type, decoded[cursor:cursor + row_bytes], previous, bpp)
        cursor += row_bytes
        previous = row
        for x in range(width):
            base = x * channels
            if color_type == 0:
                raw = sample(row, base, depth)
                rgb = (scale(raw, depth),) * 3
                alpha = 0 if key == (raw,) else 255
            elif color_type == 2:
                raw_rgb = tuple(sample(row, base + i, depth) for i in range(3))
                rgb = tuple(scale(v, depth) for v in raw_rgb)
                alpha = 0 if key == raw_rgb else 255
            elif color_type == 3:
                index = sample(row, base, depth)
                if index >= palette_count:
                    raise ValidationError("indexed pixel refers to a missing palette entry")
                rgb = tuple(palette[index * 3:index * 3 + 3])
                alpha = transparency[index] if transparency is not None and index < len(transparency) else 255
            elif color_type == 4:
                gray = sample(row, base, depth)
                rgb = (scale(gray, depth),) * 3
                alpha = scale(sample(row, base + 1, depth), depth)
            else:
                rgb = tuple(scale(sample(row, base + i, depth), depth) for i in range(3))
                alpha = scale(sample(row, base + 3, depth), depth)
            alpha_min, alpha_max = min(alpha_min, alpha), max(alpha_max, alpha)
            if alpha:
                visible += 1
                if len(colors) < 3:
                    colors.add(rgb)
                if x < width // 4:
                    left_edge_visible += 1
                if x >= width * 3 // 4:
                    right_edge_visible += 1
    if not visible:
        raise ValidationError("PNG has no visible pixels")
    return PngFacts(width, height, color_type, depth, alpha_min, alpha_max, visible, len(colors),
                    left_edge_visible, right_edge_visible)


def decode_rgba8(path: Path) -> Tuple[int, int, List[bytes]]:
    """Decode bounded RGBA8 rows after strict PNG/CRC validation."""
    facts = inspect_png(path)
    if facts.color_type != 6 or facts.bit_depth != 8:
        raise ValidationError("atlas/source sprite must use RGBA8 pixels for exact crop verification")
    data = path.read_bytes()
    offset = len(PNG_SIGNATURE)
    idat = bytearray()
    width = height = 0
    while offset < len(data):
        length = struct.unpack_from(">I", data, offset)[0]
        kind = data[offset + 4:offset + 8]
        body_start = offset + 8
        body_end = body_start + length
        if kind == b"IHDR":
            width, height = struct.unpack_from(">II", data, body_start)
        elif kind == b"IDAT":
            idat.extend(data[body_start:body_end])
        offset = body_end + 4
    row_size = width * 4
    expected = (row_size + 1) * height
    decoder = zlib.decompressobj()
    raw = decoder.decompress(bytes(idat), expected + 1)
    if len(raw) <= expected and not decoder.unconsumed_tail:
        raw += decoder.flush()
    if len(raw) != expected or not decoder.eof or decoder.unused_data:
        raise ValidationError("RGBA8 PNG image stream is truncated or oversized")
    rows: List[bytes] = []
    previous = b""
    cursor = 0
    for _ in range(height):
        filter_type = raw[cursor]
        cursor += 1
        row = unfilter(filter_type, raw[cursor:cursor + row_size], previous, 4)
        cursor += row_size
        rows.append(row)
        previous = row
    return width, height, rows


def parse_xml(path: Path, label: str, errors: List[str]) -> Optional[ET.Element]:
    try:
        return ET.parse(path).getroot()
    except (OSError, ET.ParseError) as exc:
        errors.append("{}: {}".format(label, exc))
        return None


def named(root: ET.Element, tag: str) -> List[ET.Element]:
    return [node for node in root.iter() if local_name(node.tag) == tag]


def validate_metadata(errors: List[str]) -> List[str]:
    """Validate registrations when current; report an old atlas as pending.

    SpriteSheetGenerator owns the SpriteData rectangles. A stale file must not
    be mistaken for validation failure of freshly prepared source sprites, nor
    can it provide atlas evidence for them.
    """
    pending: List[str] = []
    root = parse_xml(SPRITE_DATA, "SpriteData", errors)
    if root is None:
        return pending
    categories = [node for node in named(root, "SpriteCategory")
                  if node.get("Name") == CATEGORY or child_text(node, "Name") == CATEGORY]
    if not categories:
        pending.append("SpriteData has not registered the current '{}' category".format(CATEGORY))
    elif not any(any(local_name(child.tag) == "AlwaysLoad" for child in node) for node in categories):
        errors.append("SpriteData category '{}' is not AlwaysLoad".format(CATEGORY))
    parts, generic = named(root, "SpritePart"), named(root, "GenericSprite")
    part_names = [child_text(node, "Name") for node in parts]
    generic_names = [child_text(node, "Name") for node in generic]
    if len([n for n in part_names if n]) != len(set(n for n in part_names if n)):
        errors.append("SpriteData contains duplicate SpritePart names")
    if len([n for n in generic_names if n]) != len(set(n for n in generic_names if n)):
        errors.append("SpriteData contains duplicate GenericSprite names")
    missing = []
    duplicate = []
    for name in DECORATIONS:
        matching_parts = [node for node in parts if child_text(node, "Name") == name]
        matching_generic = [node for node in generic if child_text(node, "Name") == name]
        if len(matching_parts) == 0 or len(matching_generic) == 0:
            missing.append(name)
        if len(matching_parts) > 1 or len(matching_generic) > 1:
            duplicate.append(name)
    retired_parts = sorted({child_text(node, "Name") for node in parts} & RETIRED_DECORATIONS)
    retired_sprites = sorted({child_text(node, "Name") for node in generic} & RETIRED_DECORATIONS)
    if missing or retired_parts or retired_sprites or not categories:
        details = []
        if missing:
            details.append("missing ImageGen registrations: " + ", ".join(missing))
        if retired_parts or retired_sprites:
            details.append("retired or replaced registrations remain: " + ", ".join(
                sorted(set(retired_parts + retired_sprites))))
        pending.append("SpriteData is stale; regenerate the source atlas before claiming sprite registration or runtime evidence" +
                       (" (" + "; ".join(details) + ")" if details else ""))
    if duplicate:
        errors.append("SpriteData contains duplicate ImageGen registration(s): " + ", ".join(duplicate))

    metadata_current = not missing and not retired_parts and not retired_sprites and bool(categories)
    if metadata_current:
        for name in DECORATIONS:
            matching_parts = [node for node in parts if child_text(node, "Name") == name]
            matching_generic = [node for node in generic if child_text(node, "Name") == name]
            if child_text(matching_generic[0], "SpritePartName") != name:
                errors.append("GenericSprite '{}' does not reference its matching SpritePart".format(name))
            part = matching_parts[0]
            if child_text(part, "CategoryName") != CATEGORY:
                errors.append("SpritePart '{}' has the wrong category".format(name))
            expected_width, expected_height = DECORATION_SIZES[name]
            if int_child(part, "Width") != expected_width or int_child(part, "Height") != expected_height:
                errors.append("SpritePart '{}' must register as {}x{}".format(
                    name, expected_width, expected_height))
            png = SPRITES / (name + ".png")
            if png.is_file():
                try:
                    facts = inspect_png(png)
                    if int_child(part, "Width") != facts.width or int_child(part, "Height") != facts.height:
                        errors.append("SpritePart '{}' dimensions differ from its PNG IHDR".format(name))
                except ValidationError:
                    pass  # The PNG validation below reports the content issue.
    config = parse_xml(CONFIG, "SpriteParts Config", errors)
    if config is not None:
        configured = [node for node in named(config, "SpriteCategory") if node.get("Name") == CATEGORY]
        if not configured:
            errors.append("Config.xml is missing SpriteCategory '{}'".format(CATEGORY))
        elif not any(any(local_name(child.tag) == "AlwaysLoad" for child in node) for node in configured):
            errors.append("Config.xml category '{}' is not AlwaysLoad".format(CATEGORY))
    return pending


def number(node: ET.Element, key: str, default: Optional[float] = None) -> Optional[float]:
    raw = node.get(key)
    if raw is None:
        return default
    try:
        return float(raw.strip().replace("px", ""))
    except ValueError:
        return None


def rect_for(node: ET.Element, parent: Rect, stack_x: Optional[float] = None,
             stack_y: Optional[float] = None, workspace_right_margin: float = 24.0,
             primary_action_button_width: float = 164.0) -> Optional[Rect]:
    ml = number(node, "MarginLeft", 0.0)
    raw_mr = node.get("MarginRight", "0")
    mr = workspace_right_margin if raw_mr == "@WorkspaceRightMargin" else number(node, "MarginRight", 0.0)
    mt, mb = number(node, "MarginTop", 0.0), number(node, "MarginBottom", 0.0)
    if None in (ml, mr, mt, mb):
        return None
    raw_width = node.get("SuggestedWidth", "")
    w = primary_action_button_width if raw_width == "@PrimaryActionButtonWidth" else number(node, "SuggestedWidth")
    h = number(node, "SuggestedHeight")
    if node.get("WidthSizePolicy") == "StretchToParent":
        w = max(0.0, parent.width - ml - mr)
    if node.get("HeightSizePolicy") == "StretchToParent":
        h = max(0.0, parent.height - mt - mb)
    max_width, max_height = number(node, "MaxWidth"), number(node, "MaxHeight")
    if w is not None and max_width is not None:
        w = min(w, max_width)
    if h is not None and max_height is not None:
        h = min(h, max_height)
    if w is None or h is None:
        return None
    ha, va = node.get("HorizontalAlignment", "Left"), node.get("VerticalAlignment", "Top")
    if stack_x is not None:
        x = stack_x + ml
    elif ha == "Right":
        x = parent.x + parent.width - mr - w
    elif ha == "Center":
        x = parent.x + ml + (parent.width - ml - mr - w) / 2
    elif ha in ("Left", "Stretch", ""):
        x = parent.x + ml
    else:
        return None
    if stack_y is not None:
        y = stack_y + mt
    elif va == "Bottom":
        y = parent.y + parent.height - mb - h
    elif va == "Center":
        y = parent.y + mt + (parent.height - mt - mb - h) / 2
    elif va in ("Top", "Stretch", ""):
        y = parent.y + mt
    else:
        return None
    return Rect(x, y, w, h)


def is_dynamic_list(node: ET.Element) -> bool:
    if local_name(node.tag) != "ListPanel":
        return False
    if node.get("DataSource") is not None:
        return True
    for child in list(node):
        template_nodes = list(child) if local_name(child.tag) == "Children" else [child]
        if any(local_name(item.tag) == "ItemTemplate" for item in template_nodes):
            return True
    return False


def layout_items(node: ET.Element):
    for child in list(node):
        if local_name(child.tag) in ("Children", "ItemTemplate"):
            yield from layout_items(child)
        else:
            yield child


def layout(parent_node: ET.Element, parent_rect: Rect, path: str = "Shell",
           in_dynamic_list: bool = False, workspace_right_margin: float = 24.0,
           primary_action_button_width: float = 164.0) -> List[Tuple[ET.Element, Optional[Rect], str, bool]]:
    rows: List[Tuple[ET.Element, Optional[Rect], str, bool]] = []
    stack = local_name(parent_node.tag) == "ListPanel" or parent_node.get("StackLayout.LayoutMethod") is not None
    dynamic_list = in_dynamic_list or is_dynamic_list(parent_node)
    method = parent_node.get("StackLayout.LayoutMethod", "")
    vertical_stack = "Vertical" in method
    flow_x = parent_rect.x
    flow_y = parent_rect.y
    for item in layout_items(parent_node):
        current = rect_for(item, parent_rect,
                           flow_x if stack and not vertical_stack else None,
                           flow_y if stack and vertical_stack else None,
                           workspace_right_margin, primary_action_button_width)
        item_path = path + "/{}[{}]".format(local_name(item.tag), item.get("Id", ""))
        rows.append((item, current, item_path, dynamic_list))
        if current is not None:
            rows.extend(layout(item, current, item_path, dynamic_list, workspace_right_margin,
                               primary_action_button_width))
        if stack and current is not None and vertical_stack:
            flow_y = current.y + current.height + (number(item, "MarginBottom", 0.0) or 0.0)
        elif stack and current is not None:
            flow_x = current.x + current.width + (number(item, "MarginRight", 0.0) or 0.0)
    return rows


def direct_widget_parent(root: ET.Element, node: ET.Element) -> Optional[ET.Element]:
    """Return the Gauntlet widget that directly owns ``node`` in Children."""
    for candidate in root.iter():
        children = next((item for item in candidate if local_name(item.tag) == "Children"), None)
        if children is not None and node in list(children):
            return candidate
    return None


def is_descendant(node: ET.Element, ancestor: ET.Element,
                  parent_by_node: Dict[ET.Element, ET.Element]) -> bool:
    current: Optional[ET.Element] = node
    while current is not None:
        if current is ancestor:
            return True
        current = parent_by_node.get(current)
    return False


def later_sibling_in_paint_order(earlier: ET.Element, later: ET.Element,
                                 parent_by_node: Dict[ET.Element, ET.Element]) -> bool:
    earlier_ancestors: List[ET.Element] = []
    current: Optional[ET.Element] = earlier
    while current is not None:
        earlier_ancestors.append(current)
        current = parent_by_node.get(current)
    later_ancestors = set()
    current = later
    while current is not None:
        later_ancestors.add(current)
        current = parent_by_node.get(current)
    common = next((node for node in earlier_ancestors if node in later_ancestors), None)
    if common is None or common is earlier or common is later:
        return False

    def branch_under(node: ET.Element) -> Optional[ET.Element]:
        branch = node
        while parent_by_node.get(branch) is not common:
            parent = parent_by_node.get(branch)
            if parent is None:
                return None
            branch = parent
        return branch

    earlier_branch, later_branch = branch_under(earlier), branch_under(later)
    if earlier_branch is None or later_branch is None or earlier_branch is later_branch:
        return False
    if local_name(common.tag) == "Children":
        siblings = list(common)
    else:
        children = next((item for item in common if local_name(item.tag) == "Children"), None)
        siblings = list(children) if children is not None else list(common)
    try:
        return siblings.index(later_branch) > siblings.index(earlier_branch)
    except ValueError:
        return False


def evidence_rectangles(shell: ET.Element, errors: List[str], workbench_rect: Rect) -> List[Tuple[Rect, bool, bool]]:
    """Resolve the evidence ledger in both VM presentation states.

    The frame's top is bound to PanelViewModel, while its stretched height and
    bottom edge are defined by the prefab margins. EvidenceHeight is not bound
    by the prefab and must not be used to infer the visible background bounds.
    """
    import re

    frames = [node for node in shell.iter()
              if local_name(node.tag) == "Widget" and node.get("Id") == "ForgeEvidenceFrame"]
    if len(frames) != 1:
        errors.append("expected one ForgeEvidenceFrame for decorative overlap checks")
        return []
    frame = frames[0]
    ml = number(frame, "MarginLeft", 0.0)
    raw_mr = frame.get("MarginRight", "0")
    base_mr = number(frame, "MarginRight", 0.0)
    if raw_mr == "@WorkspaceRightMargin":
        base_mr = 24.0
    bottom_margin = number(frame, "MarginBottom")
    width = number(frame, "SuggestedWidth")
    if width is None and frame.get("WidthSizePolicy") == "StretchToParent" and base_mr is not None:
        width = max(0.0, workbench_rect.width - ml - base_mr)
    max_width = number(frame, "MaxWidth")
    if width is not None and max_width is not None:
        width = min(width, max_width)
    if width is None or bottom_margin is None or frame.get("HeightSizePolicy") != "StretchToParent":
        errors.append("ForgeEvidenceFrame must stretch from its VM top binding to a numeric prefab bottom margin")
        return []
    align = frame.get("HorizontalAlignment", "Left")
    if base_mr is None:
        errors.append("ForgeEvidenceFrame MarginRight must be numeric or bind WorkspaceRightMargin")
        return []
    try:
        source = PANEL_VIEW_MODEL.read_text(encoding="utf-8")
    except OSError as exc:
        errors.append("cannot read PanelViewModel evidence bounds: {}".format(exc))
        return []
    top_binding = frame.get("MarginTop", "")
    property_name = top_binding[1:] if top_binding.startswith("@") else ""
    match = re.search(
        r"\b{}\s*=>\s*evidenceFocused\s*\?\s*([0-9]+(?:\.[0-9]+)?)f\s*:\s*([0-9]+(?:\.[0-9]+)?)f".format(re.escape(property_name)),
        source) if property_name else None
    if match is None:
        errors.append("cannot resolve literal states for ForgeEvidenceFrame MarginTop binding")
        return []
    tops = (float(match.group(1)), float(match.group(2)))
    max_height = number(frame, "MaxHeight")
    right_margin_match = re.search(
        r"\bWorkspaceRightMargin\s*=>\s*_isDetailedMode\s*&&\s*!evidenceFocused\s*\?\s*"
        r"([0-9]+(?:\.[0-9]+)?)f\s*:\s*([0-9]+(?:\.[0-9]+)?)f", source)
    if raw_mr == "@WorkspaceRightMargin" and right_margin_match is None:
        errors.append("cannot resolve WorkspaceRightMargin states for the evidence frame")
        return []
    detail_margin = float(right_margin_match.group(1)) if right_margin_match is not None else base_mr
    compact_margin = float(right_margin_match.group(2)) if right_margin_match is not None else base_mr
    rectangles: List[Tuple[Rect, bool, bool]] = []
    for focused, top in ((True, tops[0]), (False, tops[1])):
        for detailed in (False, True):
            mr = detail_margin if detailed and not focused else compact_margin
            if frame.get("WidthSizePolicy") == "StretchToParent":
                current_width = max(0.0, workbench_rect.width - ml - mr)
            else:
                current_width = width
            if current_width is None:
                continue
            if max_width is not None:
                current_width = min(current_width, max_width)
            current_height = max(0.0, workbench_rect.height - top - bottom_margin)
            if max_height is not None:
                current_height = min(current_height, max_height)
            if align == "Center":
                x = workbench_rect.x + ml + (workbench_rect.width - ml - mr - current_width) / 2
            elif align == "Right":
                x = workbench_rect.x + workbench_rect.width - mr - current_width
            else:
                x = workbench_rect.x + ml
            rect = Rect(x, workbench_rect.y + top, current_width, current_height)
            rectangles.append((rect, focused, detailed))
    if any(rect.width <= 0 or rect.height <= 0 or rect.x < workbench_rect.x or rect.y < workbench_rect.y
           or rect.x + rect.width > workbench_rect.x + workbench_rect.width
           or rect.y + rect.height > workbench_rect.y + workbench_rect.height
           for rect, _focused, _detailed in rectangles):
        errors.append("PanelViewModel evidence bounds extend outside the responsive workbench")
        return []
    # Detailed non-focused evidence reserves the playbook width; focused evidence
    # hides the playbook and returns to the wider compact evidence frame.
    return rectangles


def validate_prefab(errors: List[str]) -> None:
    root = parse_xml(PREFAB, "Gauntlet prefab", errors)
    if root is None:
        return
    retired_refs = sorted({
        node.get(attr, "")
        for node in root.iter()
        for attr in ("Sprite", "SpriteName")
        if node.get(attr, "") in RETIRED_DECORATIONS
    })
    if retired_refs:
        errors.append("prefab still references replaced geometric sprites: " + ", ".join(retired_refs))
    refs = {name: [] for name in DECORATIONS}
    for node in root.iter():
        for attr in ("Sprite", "SpriteName"):
            if node.get(attr) in refs:
                refs[node.get(attr)].append(node)
    for name, nodes in refs.items():
        if not nodes:
            errors.append("prefab does not reference '{}'".format(name))
    shells = [node for node in root.iter() if local_name(node.tag) == "Widget"
              and node.get("Id") == "ForgeWorkbenchShell"]
    if len(shells) != 1:
        errors.append("expected one responsive ForgeWorkbenchShell for safe-placement checks")
        return
    shell = shells[0]
    if shell.get("WidthSizePolicy") != "StretchToParent" or shell.get("HeightSizePolicy") != "StretchToParent" \
            or number(shell, "MaxWidth") != 1760 or number(shell, "MaxHeight") != 1024:
        errors.append("ForgeWorkbenchShell must retain its responsive 1760x1024 size caps")
    # Validate the smallest normal profile where header/rail/evidence collisions are most likely.
    viewport_width, viewport_height = 1280, 720
    workbench_rect = Rect(0, 0, min(1760, viewport_width - 48), min(1024, viewport_height - 48))
    rows = layout(shell, workbench_rect)
    placed = {id(node): (rect, path, stacked) for node, rect, path, stacked in rows}

    nodes_by_id: Dict[str, List[ET.Element]] = {}
    for node in shell.iter():
        element_id = node.get("Id")
        if element_id:
            nodes_by_id.setdefault(element_id, []).append(node)
    routes = ("Summary", "Modules", "Logs", "Inspector", "Tests", "Metrics", "Framework", "Extensions")
    for route in routes:
        button_id = "Forge" + route
        active_binding = "@Is" + route + "Active"
        buttons_for_route = nodes_by_id.get(button_id, [])
        if len(buttons_for_route) != 1:
            errors.append("expected one selected navigation button '{}'".format(button_id))
            continue
        route_button = buttons_for_route[0]
        if route_button.get("ButtonType") != "Radio" or route_button.get("IsSelected") != active_binding:
            errors.append("{} must retain radio selection binding {}".format(button_id, active_binding))
        if route_button.get("Brush") != "CalradiaForge.CategoryTab" or route_button.get("IsFocusable") != "true":
            errors.append("{} must use the selected category brush and remain focusable".format(button_id))
        marker_id = button_id + "SelectedMarker"
        markers = nodes_by_id.get(marker_id, [])
        if len(markers) != 1:
            errors.append("expected one visual selection marker '{}'".format(marker_id))
            continue
        marker = markers[0]
        if marker.get("IsVisible") != active_binding or direct_widget_parent(shell, marker) is not route_button:
            errors.append("{} must follow {} inside its navigation button".format(marker_id, active_binding))
        if (marker.get("DoNotAcceptEvents") or "").lower() != "true" or \
                (marker.get("DoNotPassEventsToChildren") or "").lower() != "true":
            errors.append("{} must remain passive".format(marker_id))

    for card_id in ("BriefingContextValue", "BriefingTestingValue",
                    "BriefingEvidenceCountValue", "BriefingTestCountValue"):
        cards = nodes_by_id.get(card_id, [])
        if len(cards) != 1 or cards[0].get("Brush") != "CalradiaForge.BriefingCard":
            errors.append("{} must use the framed briefing-card brush".format(card_id))
        elif cards[0].get("IsVisible") != "@ShowCommandDeck" or \
                (cards[0].get("DoNotAcceptEvents") or "").lower() != "true" or \
                (cards[0].get("DoNotPassEventsToChildren") or "").lower() != "true":
            errors.append("{} must stay passive and follow the command-deck visibility state".format(card_id))

    for element_id, (expected_sprite, expected_parent_id, expected_local_rect) in EXPECTED_DECORATIVE_PLACEMENTS.items():
        matches = nodes_by_id.get(element_id, [])
        if len(matches) != 1:
            errors.append("expected exactly one decorative placement '{}' (found {})".format(element_id, len(matches)))
            continue
        node = matches[0]
        if local_name(node.tag) != "ImageWidget":
            errors.append("{} must be an ImageWidget".format(element_id))
        if node.get("Sprite") != expected_sprite:
            errors.append("{} must reference '{}'".format(element_id, expected_sprite))
        expected_route_ornament = ROUTE_HEADER_ORNAMENT_BINDINGS.get(element_id)
        if expected_route_ornament is not None and node.get("IsVisible") != expected_route_ornament[1]:
            errors.append("{} must be visible only when {} is active".format(
                element_id, expected_route_ornament[1]))
        parent = direct_widget_parent(shell, node)
        parent_matches = parent is shell if expected_parent_id == "<shell>" else (
            parent is not None and parent.get("Id") == expected_parent_id)
        if not parent_matches:
            errors.append("{} must be a direct child of {}".format(element_id, expected_parent_id))
            continue
        if element_id in PLAYBOOK_FLOW_RULE_IDS:
            # A vertical ListPanel deliberately has no fixed height: text rows
            # use CoverChildren and scroll as one content tree. Their rules are
            # still bounded by the flow contract and ordered between complete
            # measured siblings, so static absolute coordinates are inapplicable.
            children = next((item for item in parent if local_name(item.tag) == "Children"), None)
            flow_ids = [item.get("Id", "") for item in list(children)] if children is not None else []
            if flow_ids != list(PLAYBOOK_FLOW_ORDER):
                errors.append("Playbook flow must keep wrapped text, separators, and the macro action in vertical order")
            if node.get("WidthSizePolicy") != "StretchToParent" or node.get("HeightSizePolicy") != "Fixed" \
                    or number(node, "SuggestedHeight") != 3:
                errors.append("{} must remain a fixed-height, width-bounded passive rule in the Playbook flow".format(
                    element_id))
            for attribute, expected_value in DECORATIVE_LAYOUT_CONTRACTS.get(element_id, {}).items():
                numeric_attributes = {
                    "SuggestedHeight", "SuggestedWidth", "MaxWidth", "MaxHeight",
                    "MarginLeft", "MarginRight", "MarginTop", "MarginBottom",
                }
                actual_value = number(node, attribute) if attribute in numeric_attributes else node.get(attribute)
                if actual_value != expected_value:
                    errors.append("{} must keep {}={} (found {})".format(
                        element_id, attribute, expected_value, actual_value))
            if (node.get("DoNotAcceptEvents") or "").lower() != "true" or \
                    (node.get("DoNotPassEventsToChildren") or "").lower() != "true":
                errors.append("{} must remain passive in the Playbook text flow".format(element_id))
            print("PASS structural scroll-flow placement: {} follows complete CoverChildren rows".format(element_id))
            continue
        node_placement = placed.get(id(node))
        parent_placement = placed.get(id(parent)) if parent is not shell else None
        parent_rect = workbench_rect if parent is shell else (
            parent_placement[0] if parent_placement is not None else None)
        if node_placement is None or node_placement[0] is None or parent_rect is None:
            errors.append("{} or its parent has unbounded geometry".format(element_id))
            continue
        rect, _path, in_dynamic_list = node_placement
        actual_local_rect = Rect(rect.x - parent_rect.x, rect.y - parent_rect.y, rect.width, rect.height)
        if expected_local_rect is not None and actual_local_rect != expected_local_rect:
            errors.append("{} local rectangle must be ({},{},{},{}) (found {},{},{},{})".format(
                element_id, expected_local_rect.x, expected_local_rect.y,
                expected_local_rect.width, expected_local_rect.height,
                actual_local_rect.x, actual_local_rect.y,
                actual_local_rect.width, actual_local_rect.height))
        if in_dynamic_list:
            errors.append("{} must remain outside dynamic data-bound lists".format(element_id))
        for attribute, expected_value in DECORATIVE_LAYOUT_CONTRACTS.get(element_id, {}).items():
            numeric_attributes = {
                "SuggestedHeight", "SuggestedWidth", "MaxWidth", "MaxHeight",
                "MarginLeft", "MarginRight", "MarginTop", "MarginBottom",
            }
            actual_value = number(node, attribute) if attribute in numeric_attributes else node.get(attribute)
            if actual_value != expected_value:
                errors.append("{} must keep {}={} (found {})".format(
                    element_id, attribute, expected_value, actual_value))
        if element_id in CARD_TOP_STRIP_IDS:
            children = next((item for item in parent if local_name(item.tag) == "Children"), None)
            if children is None or not list(children) or list(children)[0] is not node:
                errors.append("{} must be the first direct child of its briefing card".format(element_id))
        if element_id in CARD_BOTTOM_RULE_IDS:
            children = next((item for item in parent if local_name(item.tag) == "Children"), None)
            if children is None or not list(children) or list(children)[-1] is not node:
                errors.append("{} must be the last direct child of its briefing card".format(element_id))

    parent_by_node = {child: parent for parent in root.iter() for child in parent}
    for rule_id, card_id in zip(
            ("ForgeBriefingContextPatinaRule", "ForgeBriefingTestingPatinaRule",
             "ForgeBriefingEvidencePatinaRule", "ForgeBriefingTestsPatinaRule"),
            ("BriefingContextValue", "BriefingTestingValue",
             "BriefingEvidenceCountValue", "BriefingTestCountValue")):
        rule = nodes_by_id.get(rule_id, [None])[0]
        card = nodes_by_id.get(card_id, [None])[0]
        rule_rect = placed.get(id(rule), (None, "", False))[0] if rule is not None else None
        if rule is None or card is None or rule_rect is None:
            errors.append("cannot verify that {} stays below its briefing text".format(rule_id))
            continue
        text_bottoms = [candidate_rect.y + candidate_rect.height
                        for candidate, candidate_rect, _path, _dynamic in rows
                        if candidate_rect is not None
                        and local_name(candidate.tag) in ("TextWidget", "RichTextWidget")
                        and is_descendant(candidate, card, parent_by_node)]
        if not text_bottoms or max(text_bottoms) > rule_rect.y:
            errors.append("{} must start at or below the bottom edge of its briefing text".format(rule_id))

    expected_ids_by_sprite = {
        sprite_name: {
            element_id for element_id, (sprite, _parent, _rect) in EXPECTED_DECORATIVE_PLACEMENTS.items()
            if sprite == sprite_name
        }
        for sprite_name in DECORATIONS
    }
    for sprite_name, expected_ids in expected_ids_by_sprite.items():
        actual_nodes = refs.get(sprite_name, [])
        actual_ids = {node.get("Id", "") for node in actual_nodes}
        if len(actual_nodes) != len(expected_ids) or actual_ids != expected_ids:
            errors.append("{} must be referenced only by the expected decorative placements {}".format(
                sprite_name, sorted(expected_ids)))

    allowed_new_overlap_pairs = {
        frozenset(("ForgeHeaderCloth", "ForgeHeaderHeraldicOverlay")),
    }
    route_ornament_ids = tuple(ROUTE_HEADER_ORNAMENT_BINDINGS)
    for index, element_id in enumerate(route_ornament_ids):
        for other_id in route_ornament_ids[index + 1:]:
            allowed_new_overlap_pairs.add(frozenset((element_id, other_id)))
    parent_by_node = {child: parent for parent in root.iter() for child in parent}

    def clipped_visible_rect(node: ET.Element, node_rect: Rect) -> Optional[Rect]:
        """Clip a node's visible bounds to every ClipContents ancestor."""
        visible_rect: Optional[Rect] = node_rect
        current = node
        while visible_rect is not None:
            current = parent_by_node.get(current)
            if current is None:
                break
            if (current.get("ClipContents") or "").lower() == "true":
                clip_rect = placed.get(id(current), (None, "", False))[0]
                if clip_rect is None:
                    return None
                visible_rect = rect_intersection(visible_rect, clip_rect)
        return visible_rect

    decorative_image_rows = [
        (node, node.get("Id", ""), placed.get(id(node), (None, "", False))[0])
        for node in shell.iter()
        if local_name(node.tag) == "ImageWidget" and node.get("Sprite") in DECORATIONS
    ]
    for node, element_id, rect in decorative_image_rows:
        if element_id not in EXPECTED_DECORATIVE_PLACEMENTS or rect is None:
            continue
        visible_rect = clipped_visible_rect(node, rect)
        if visible_rect is None:
            continue
        for other_node, other_id, other_rect in decorative_image_rows:
            if node is other_node or other_rect is None:
                continue
            visible_other_rect = clipped_visible_rect(other_node, other_rect)
            if visible_other_rect is None or not visible_rect.intersects(visible_other_rect):
                continue
            pair = frozenset((element_id, other_id))
            if pair not in allowed_new_overlap_pairs:
                errors.append("new decoration {} overlaps unapproved decoration {}".format(element_id, other_id))

    button_rows = [(node, rect, path) for node, rect, path, _stack in rows
                   if local_name(node.tag) == "ButtonWidget"]
    if any(rect is None for _node, rect, _path in button_rows):
        errors.append("cannot prove decorative sprites avoid every button because a button has unbounded geometry")
    buttons = [(node, rect, path) for node, rect, path in button_rows if rect is not None]
    editable_rows = [(node, rect, path) for node, rect, path, _stack in rows
                     if local_name(node.tag) == "EditableTextWidget"]
    if any(rect is None for _node, rect, _path in editable_rows):
        errors.append("cannot prove decorative sprites avoid editable fields with unbounded geometry")
    editable_fields = [(node, rect, path) for node, rect, path in editable_rows if rect is not None]
    command_input_ids = {"ForgeCommandInputFrame", "ForgeAssemblyPathFrame", "ForgeAssemblyVersionFrame"}
    command_input_rows = [(node, rect, path) for node, rect, path, _stack in rows
                          if node.get("Id") in command_input_ids]
    if any(rect is None for _node, rect, _path in command_input_rows):
        errors.append("cannot prove decorative sprites avoid command input surfaces with unbounded geometry")
    command_inputs = [(node, rect, path) for node, rect, path in command_input_rows if rect is not None]
    text_rows = [(node, rect, path) for node, rect, path, _stack in rows
                 if local_name(node.tag) == "TextWidget"]
    if any(rect is None for _node, rect, _path in text_rows):
        errors.append("cannot prove decorative sprites avoid static text with unbounded geometry")
    text_widgets = [(node, rect, path) for node, rect, path in text_rows if rect is not None]

    overlay_panels = []
    for panel_id in sorted(OCCLUDING_PANEL_IDS):
        matches = nodes_by_id.get(panel_id, [])
        if len(matches) != 1:
            continue
        panel = matches[0]
        color = panel.get("Color", "")
        opaque_color = (len(color) == 9 and color.startswith("#")
                        and all(character in "0123456789abcdefABCDEF" for character in color[1:])
                        and color[-2:].upper() == "FF")
        panel_placement = placed.get(id(panel))
        panel_rect = panel_placement[0] if panel_placement is not None else None
        if panel_id == "NavigationPalettePanel":
            overlay = direct_widget_parent(root, panel)
            shell_parent = direct_widget_parent(root, overlay) if overlay is not None else None
            overlay_placement = placed.get(id(overlay)) if overlay is not None else None
            overlay_rect = overlay_placement[0] if overlay_placement is not None else None
            shell_children = next((child for child in shell_parent
                                   if local_name(child.tag) == "Children"), None) if shell_parent is not None else None
            # The prefab keeps a hidden zero-size sprite warm-up container after
            # the modal overlay. It never paints or accepts input, so it must
            # not make the visible overlay fail the topmost-modal contract.
            shell_siblings = list(shell_children) if shell_children is not None else []
            overlay_index = shell_siblings.index(overlay) if overlay in shell_siblings else -1
            trailing_siblings = shell_siblings[overlay_index + 1:] if overlay_index >= 0 else []
            trailing_non_rendering = all(
                sibling.get("IsVisible") == "false"
                and number(sibling, "SuggestedWidth", 0) == 0
                and number(sibling, "SuggestedHeight", 0) == 0
                and (sibling.get("DoNotAcceptEvents") or "").lower() == "true"
                and (sibling.get("DoNotPassEventsToChildren") or "").lower() == "true"
                for sibling in trailing_siblings
            )
            topmost_modal = overlay_index >= 0 and trailing_non_rendering
            modal_contract_ok = (
                overlay is not None
                and overlay.get("Id") == "NavigationPaletteOverlay"
                and local_name(overlay.tag) == "Widget"
                and overlay.get("IsVisible") == "@IsNavigationPaletteOpen"
                and overlay.get("WidthSizePolicy") == "StretchToParent"
                and overlay.get("HeightSizePolicy") == "StretchToParent"
                and shell_parent is not None
                and shell_parent.get("Id") == "ForgeWorkbenchShell"
                and topmost_modal
                and overlay_rect is not None
                and overlay_rect == workbench_rect
                and panel_rect is not None
                and overlay_rect.contains(panel_rect)
            )
            if not modal_contract_ok:
                errors.append("NavigationPalettePanel occlusion requires an opaque panel inside the final, shell-sized @IsNavigationPaletteOpen overlay")
                continue
            # The overlay dims the shell but is translucent. Only the opaque
            # inner panel masks its descendants from decorations behind it.
            visible_binding = overlay.get("IsVisible", "")
        else:
            visible_binding = panel.get("IsVisible", "")
            if panel_id == "ForgePlaybookPanel":
                if visible_binding != "@IsPlaybookVisible":
                    errors.append("ForgePlaybookPanel occlusion requires the @IsPlaybookVisible state")
                    continue
                if direct_widget_parent(root, panel) is not shell:
                    errors.append("ForgePlaybookPanel must remain a direct child of ForgeWorkbenchShell")
                    continue
        if (local_name(panel.tag) != "Widget" or panel.get("Sprite") != "BlankWhiteSquare_9"
                or not opaque_color or not visible_binding.startswith("@") or panel_rect is None):
            continue
        overlay_panels.append((panel, panel_rect, panel_id, visible_binding))

    def unoccluded_overlaps(decoration_node: ET.Element, decoration_name: str,
                            targets, collision_kind: str, detailed_mode: bool = True,
                            evidence_focused: bool = False):
        uncovered = []
        visible_decoration_rect = clipped_visible_rect(decoration_node, rect)
        if visible_decoration_rect is None:
            return uncovered
        for target_node, target_rect, target_path in targets:
            visible_target_rect = clipped_visible_rect(target_node, target_rect)
            if visible_target_rect is None:
                continue
            overlap = rect_intersection(visible_decoration_rect, visible_target_rect)
            if overlap is None:
                continue
            covering_panel = None
            for panel, panel_rect, panel_id, visible_binding in overlay_panels:
                if panel_id == "ForgePlaybookPanel" and (not detailed_mode or evidence_focused):
                    continue
                target_is_inside = is_descendant(target_node, panel, parent_by_node)
                decoration_is_inside = is_descendant(decoration_node, panel, parent_by_node)
                if target_is_inside and not decoration_is_inside:
                    if not later_sibling_in_paint_order(decoration_node, panel, parent_by_node):
                        continue
                elif decoration_is_inside and not target_is_inside:
                    # ForgePlaybookPanel is an opaque later-painted surface. It
                    # masks both its own children and the background content it
                    # covers; the inner decorations cannot collide visually with
                    # text beneath that surface.
                    if not later_sibling_in_paint_order(target_node, panel, parent_by_node):
                        continue
                else:
                    continue
                if panel_rect.contains(overlap):
                    covering_panel = (panel_id, panel_rect, visible_binding)
                    break
            if covering_panel is None:
                uncovered.append((target_node, target_rect, target_path))
                continue
            panel_id, panel_rect, visible_binding = covering_panel
            print("PASS overlay occlusion: {} ({}) vs {} {}; covered by later sibling {} "
                  "(opaque Sprite=BlankWhiteSquare_9, IsVisible={}, target=descendant, "
                  "collision=({:.1f},{:.1f},{:.1f},{:.1f}), panel=({:.1f},{:.1f},{:.1f},{:.1f}))".format(
                      decoration_name, element_id, collision_kind, target_path, panel_id, visible_binding,
                      overlap.x, overlap.y, overlap.width, overlap.height,
                      panel_rect.x, panel_rect.y, panel_rect.width, panel_rect.height))
        return uncovered

    ledger_states = evidence_rectangles(shell, errors, workbench_rect)
    for name, nodes in refs.items():
        for node in nodes:
            element_id = node.get("Id", "")
            if local_name(node.tag) != "ImageWidget":
                errors.append("'{}' must be used by ImageWidget, not {}".format(name, local_name(node.tag)))
                continue
            if (node.get("DoNotAcceptEvents") or "").lower() != "true":
                errors.append("'{}' ImageWidget must set DoNotAcceptEvents=true".format(name))
            if (node.get("DoNotPassEventsToChildren") or "").lower() != "true":
                errors.append("'{}' ImageWidget must set DoNotPassEventsToChildren=true".format(name))
            placement = placed.get(id(node))
            if placement is None or placement[0] is None or placement[0].width <= 0 or placement[0].height <= 0:
                if element_id in PLAYBOOK_FLOW_RULE_IDS \
                        and direct_widget_parent(shell, node) is not None \
                        and direct_widget_parent(shell, node).get("Id") == "ForgePlaybookFlow":
                    continue
                errors.append("'{}' has no statically bounded decorative placement".format(name))
                continue
            rect, path, in_dynamic_list = placement
            if in_dynamic_list:
                errors.append("'{}' is inside a dynamic data-bound list ({})".format(name, path))
                continue
            if rect.x < workbench_rect.x or rect.y < workbench_rect.y \
                    or rect.x + rect.width > workbench_rect.x + workbench_rect.width \
                    or rect.y + rect.height > workbench_rect.y + workbench_rect.height:
                errors.append("'{}' extends beyond the workbench frame ({})".format(name, path))
            button_overlaps = unoccluded_overlaps(node, name, buttons, "interactive button")
            if button_overlaps:
                errors.append("'{}' overlaps an interactive button ({})".format(
                    name, button_overlaps[0][2]))
            editable_overlaps = unoccluded_overlaps(
                node, name, editable_fields, "editable field")
            if editable_overlaps:
                errors.append("'{}' overlaps an editable field ({})".format(
                    name, editable_overlaps[0][2]))
            input_overlaps = unoccluded_overlaps(
                node, name, command_inputs, "command input surface")
            if input_overlaps:
                errors.append("'{}' overlaps a command input surface ({})".format(
                    name, input_overlaps[0][2]))
            overlapping_text = unoccluded_overlaps(node, name, text_widgets, "static text")
            if overlapping_text:
                allowed_parent = None
                if name in TEXT_OVERLAP_ALPHA_MAX and path.startswith((
                        "Shell/Widget[ForgeHeader]/", "Shell/Widget[ForgeNavigationRail]/")):
                    parent_id = "ForgeHeader" if path.startswith("Shell/Widget[ForgeHeader]/") else "ForgeNavigationRail"
                    parent = next(((candidate_rect, candidate_path) for candidate, candidate_rect, candidate_path, _ in rows
                                   if candidate.get("Id") == parent_id and candidate_rect is not None), None)
                    if parent is not None and parent[0].contains(rect):
                        try:
                            allowed_parent = inspect_png(SPRITES / (name + ".png"))
                        except (OSError, ValidationError):
                            allowed_parent = None
                alpha_limit = TEXT_OVERLAP_ALPHA_MAX.get(name)
                if allowed_parent is None or alpha_limit is None or allowed_parent.alpha_max > alpha_limit:
                    errors.append("'{}' overlaps static text outside its verified low-alpha header/rail exception ({})".format(
                        name, overlapping_text[0][2]))
                else:
                    print("PASS low-alpha text overlap exception: {} in bounded {} (alpha max {}/{})".format(
                        name, parent_id, allowed_parent.alpha_max, alpha_limit))
            parent_widget = direct_widget_parent(shell, node)
            briefing_card = element_id in (CARD_TOP_STRIP_IDS | CARD_BOTTOM_RULE_IDS)
            hidden_while_focused = briefing_card and parent_widget is not None \
                and parent_widget.get("IsVisible") == "@ShowCommandDeck"
            if briefing_card and (parent_widget is None or parent_widget.get("IsVisible") != "@ShowCommandDeck"):
                errors.append("{} must follow the command-deck visibility state before ledger overlap can be waived".format(
                    element_id))
            active_ledger_states = [ledger for ledger, focused, detailed in ledger_states
                                    if not (hidden_while_focused and focused)
                                    and not (element_id.startswith("ForgePlaybook") and (not detailed or focused))]
            if any(rect.intersects(ledger) for ledger in active_ledger_states):
                errors.append("'{}' overlaps the evidence ledger in at least one VM layout state".format(name))


def runtime_evidence(metadata_pending: List[str]) -> Tuple[str, List[str]]:
    reasons = list(metadata_pending)
    observations: List[str] = []
    if metadata_pending:
        return "PENDING", reasons + [
            "atlas pixel crops and TPAC freshness are not evaluated against stale SpriteData"
        ]
    try:
        atlas = SOURCE_ATLAS.stat()
        sprite_data = SPRITE_DATA.stat()
    except OSError:
        return "PENDING", ["source atlas or SpriteData is missing"]
    with SOURCE_ATLAS.open("rb") as stream:
        atlas_signature = stream.read(8)
    if atlas.st_size <= len(PNG_SIGNATURE) or atlas_signature != PNG_SIGNATURE:
        reasons.append("source atlas lacks non-empty PNG signature evidence")
    else:
        try:
            sprite_root = ET.parse(SPRITE_DATA).getroot()
            sheet_parts = [node for node in named(sprite_root, "SpritePart")
                           if child_text(node, "Name") in DECORATIONS]
            atlas_width, atlas_height, atlas_rows = decode_rgba8(SOURCE_ATLAS)
            sheet_ids = {child_text(part, "SheetID") for part in sheet_parts}
            observations.append("source atlas budget: {:,} compressed bytes; {}x{} RGBA uses {:.2f} MiB decoded".format(
                atlas.st_size, atlas_width, atlas_height,
                atlas_width * atlas_height * 4 / (1024 * 1024)))
            if len(sheet_ids) != 1 or not next(iter(sheet_ids), "").isdigit():
                reasons.append("decorative SpriteParts do not resolve to one numeric sheet ID")
            else:
                sheet_id = next(iter(sheet_ids))
                sizes = [node for node in named(sprite_root, "SpriteSheetSize")
                         if node.get("ID") == sheet_id]
                if len(sizes) != 1 or int(sizes[0].get("Width", "-1")) != atlas_width \
                        or int(sizes[0].get("Height", "-1")) != atlas_height:
                    reasons.append("source atlas dimensions do not match SpriteData sheet metadata")
            for part in sheet_parts:
                name = child_text(part, "Name")
                try:
                    x, y = int(child_text(part, "SheetX")), int(child_text(part, "SheetY"))
                    width, height = int(child_text(part, "Width")), int(child_text(part, "Height"))
                    if x < 0 or y < 0 or width <= 0 or height <= 0 \
                            or x + width > atlas_width or y + height > atlas_height:
                        raise ValidationError("SpritePart rectangle exceeds atlas bounds")
                    source_width, source_height, source_rows = decode_rgba8(SPRITES / (name + ".png"))
                    if (source_width, source_height) != (width, height):
                        raise ValidationError("SpritePart dimensions differ from its source PNG")
                    exact = all(
                        atlas_rows[y + row][x * 4:(x + width) * 4] == source_rows[row]
                        for row in range(height)
                    )
                    if not exact:
                        reasons.append("atlas pixel crop differs from source sprite '{}'".format(name))
                except (ValueError, OSError, ValidationError) as exc:
                    reasons.append("cannot verify atlas crop for '{}': {}".format(name, exc))
        except (OSError, ET.ParseError, ValidationError, zlib.error) as exc:
            reasons.append("cannot decode/compare source atlas against SpriteData: {}".format(exc))
    try:
        tpac_stat = TPAC.stat()
        with TPAC.open("rb") as stream:
            tpac_header = stream.read(64)
        if len(tpac_header) < 64 or tpac_header[:4] != b"TPAC" or struct.unpack_from("<I", tpac_header, 4)[0] != 2:
            reasons.append("TPAC v2 header evidence is missing")
        expected = b"AssetSources/GauntletUI/ui_calradiaforge_1.png".lower()
        found_path = False
        overlap = b""
        with TPAC.open("rb") as stream:
            while True:
                chunk = stream.read(1024 * 1024)
                if not chunk:
                    break
                searchable = (overlap + chunk).lower().replace(b"\\", b"/")
                if expected in searchable:
                    found_path = True
                    break
                overlap = searchable[-(len(expected) - 1):]
        if not found_path:
            reasons.append("TPAC does not identify the expected source atlas path")
        if tpac_stat.st_mtime_ns < atlas.st_mtime_ns:
            reasons.append("TPAC predates the source atlas")
    except OSError:
        reasons.append("runtime TPAC is missing")
    if reasons:
        return "PENDING", reasons + observations
    return "EVIDENCE_PRESENT_STRUCTURAL_ONLY", [
        "source atlas dimensions and decorative pixel crops match their SpriteData rectangles",
        "TPAC texture pixels and in-game rendering are not decoded or verified",
    ] + observations


def main() -> int:
    print("Calradia Forge decorative sprite validator — Python standard library only")
    errors: List[str] = []
    for name in DECORATIONS:
        path = SPRITES / (name + ".png")
        try:
            facts = inspect_png(path)
            print("PASS PNG {}: {}x{}, alpha {}..{}, visible pixels {}, visible colors {}".format(
                path.relative_to(ROOT), facts.width, facts.height, facts.alpha_min,
                facts.alpha_max, facts.visible_pixels, facts.distinct_visible_colors))
            if facts.color_type != 6 or facts.bit_depth != 8:
                errors.append("{} must use RGBA8 pixels".format(path.relative_to(ROOT)))
            expected = DECORATION_SIZES[name]
            if (facts.width, facts.height) != expected:
                errors.append("{} must be {}x{} (found {}x{})".format(
                    path.relative_to(ROOT), expected[0], expected[1], facts.width, facts.height))
            if facts.alpha_max == 0:
                errors.append("{} is fully transparent".format(path.relative_to(ROOT)))
            if facts.alpha_max > MAX_ALPHA[name]:
                errors.append("{} exceeds alpha cap {} (found {})".format(
                    path.relative_to(ROOT), MAX_ALPHA[name], facts.alpha_max))
            if facts.distinct_visible_colors < MIN_VISIBLE_COLORS[name]:
                errors.append("{} has insufficient visible color variation (needs at least {})".format(
                    path.relative_to(ROOT), MIN_VISIBLE_COLORS[name]))
            if name in TRANSPARENT_DECORATIONS and (facts.alpha_min != 0 or facts.alpha_min == facts.alpha_max):
                errors.append("{} must keep both fully transparent and visible pixels".format(path.relative_to(ROOT)))
            if name in EDGE_ART_DECORATIONS and \
                    (facts.left_edge_visible_pixels == 0 or facts.right_edge_visible_pixels == 0):
                errors.append("{} must retain visible artwork in both horizontal end bands".format(path.relative_to(ROOT)))
        except (ValidationError, OSError) as exc:
            errors.append("{}: {}".format(path.relative_to(ROOT), exc))
    metadata_pending = validate_metadata(errors)
    validate_prefab(errors)
    print("SpriteData registration: {}".format("PENDING" if metadata_pending else "CURRENT"))
    for note in metadata_pending:
        print("  - {}".format(note))
    status, notes = runtime_evidence(metadata_pending)
    print("Source-atlas/TPAC structural evidence (not a live-render claim): {}".format(status))
    for note in notes:
        print("  - {}".format(note))
    if errors:
        print("\nFAILED: {} issue(s)".format(len(errors)))
        for error in errors:
            print("  - {}".format(error))
        return 1
    print("\nPASS: source PNGs and safe prefab placement are consistent.")
    if metadata_pending:
        print("PENDING: regenerate SpriteData/atlas; no runtime or in-game texture validation is implied.")
    else:
        print("SpriteData registration is current; no TPAC pixels or Bannerlord live rendering are certified.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
