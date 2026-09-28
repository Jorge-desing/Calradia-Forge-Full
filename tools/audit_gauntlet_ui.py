#!/usr/bin/env python3
"""Structural and layout audit for the generated Calradia Forge Gauntlet panel.

This checks the generated prefab against the real brush, sprite and ViewModel
contracts. It deliberately does not claim to replace an in-game render check.
"""

from __future__ import annotations

import argparse
import re
import struct
import sys
import xml.etree.ElementTree as ET
import zlib
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_PREFAB = ROOT / "modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"
DEFAULT_VIEWMODEL = ROOT / "src/CalradiaForge.Mod/PanelViewModel.cs"
DEFAULT_TOOL_ITEM_VIEWMODEL = ROOT / "src/CalradiaForge.Mod/ToolItemVM.cs"
DEFAULT_BRUSHES = ROOT / "modules/CalradiaForge/GUI/Brushes/CalradiaForge.xml"
DEFAULT_SPRITES = ROOT / "modules/CalradiaForge/GUI/CalradiaForgeSpriteData.xml"
SPRITE_PART_ROOT = ROOT / "modules/CalradiaForge/GUI/SpriteParts"
PRIMARY_ROUTE_ICONS = {
    "ForgeSummary": "calradiaforge_open_book",
    "ForgeModules": "calradiaforge_puzzle",
    "ForgeLogs": "calradiaforge_files",
    "ForgeInspector": "calradiaforge_eye_target",
    "ForgeTests": "calradiaforge_test_tubes",
    "ForgeMetrics": "calradiaforge_histogram",
    "ForgeFramework": "calradiaforge_anvil",
    "ForgeExtensions": "calradiaforge_plug",
}

ROUTE_ACTIVE_BINDINGS = {
    "IsSummaryActive": "summary",
    "IsModulesActive": "modules",
    "IsLogsActive": "logs",
    "IsInspectorActive": "inspect",
    "IsTestsActive": "tests",
    "IsMetricsActive": "metrics",
    "IsFrameworkActive": "framework",
    "IsExtensionsActive": "extensions",
}
ROUTE_CANONICAL_ROUTE_BY_BINDING = {binding: route for binding, route in ROUTE_ACTIVE_BINDINGS.items()}
ROUTE_ORNAMENTS = {
    "ForgeHeaderOrnamentSummary": ("forge_header_summary_v1", "IsSummaryActive", "summary"),
    "ForgeHeaderOrnamentModules": ("forge_header_modules_v1", "IsModulesActive", "modules"),
    "ForgeHeaderOrnamentLogs": ("forge_header_logs_v1", "IsLogsActive", "logs"),
    "ForgeHeaderOrnamentInspector": ("forge_header_inspector_v1", "IsInspectorActive", "inspect"),
    "ForgeHeaderOrnamentTests": ("forge_header_tests_v1", "IsTestsActive", "tests"),
    "ForgeHeaderOrnamentMetrics": ("forge_header_metrics_v1", "IsMetricsActive", "metrics"),
    "ForgeHeaderOrnamentFramework": ("forge_header_framework_v1", "IsFrameworkActive", "framework"),
    "ForgeHeaderOrnamentExtensions": ("forge_header_extensions_v1", "IsExtensionsActive", "extensions"),
}

DECORATIVE_IMAGES = {
    "ForgeHeaderCloth": ("forge_war_table_cloth_v2", "ForgeHeader"),
    "ForgeHeaderHeraldicOverlay": ("forge_heraldic_header_v2", "ForgeHeader"),
    "ForgeRailPineFelt": ("forge_pine_felt", "ForgeNavigationRail"),
    "ForgeRailCloth": ("forge_heraldic_rail_v2", "ForgeNavigationRail"),
    "ForgeBriefingContextPineFelt": ("forge_pine_felt", "BriefingContextValue"),
    "ForgeBriefingTestingPineFelt": ("forge_pine_felt", "BriefingTestingValue"),
    "ForgeBriefingEvidencePineFelt": ("forge_pine_felt", "BriefingEvidenceCountValue"),
    "ForgeBriefingTestsPineFelt": ("forge_pine_felt", "BriefingTestCountValue"),
    "ForgeBriefingContextPatinaRule": ("forge_patina_brass", "BriefingContextValue"),
    "ForgeBriefingTestingPatinaRule": ("forge_patina_brass", "BriefingTestingValue"),
    "ForgeBriefingEvidencePatinaRule": ("forge_patina_brass", "BriefingEvidenceCountValue"),
    "ForgeBriefingTestsPatinaRule": ("forge_patina_brass", "BriefingTestCountValue"),
    "ForgeTopBrassFrameRule": ("forge_patina_brass", "<shell>"),
    "ForgeEvidenceActionBrassRule": ("forge_patina_brass", "<shell>"),
    "ForgeBottomBrassFrameRule": ("forge_patina_brass", "<shell>"),
}
DECORATIVE_IMAGES.update(
    {element_id: (sprite, "<shell>") for element_id, (sprite, _, _) in ROUTE_ORNAMENTS.items()}
)


@dataclass(frozen=True)
class Rect:
    left: float
    top: float
    width: float
    height: float

    @property
    def right(self) -> float:
        return self.left + self.width

    @property
    def bottom(self) -> float:
        return self.top + self.height


EXPECTED_LOCAL_RECTS = {
    "ForgeHeaderHeraldicOverlay": Rect(650, 17, 220, 42),
    "ForgeRailPineFelt": Rect(16, 394, 198, 8),
    "ForgeBriefingContextPineFelt": Rect(0, 0, 208, 3),
    "ForgeBriefingTestingPineFelt": Rect(0, 0, 208, 3),
    "ForgeBriefingEvidencePineFelt": Rect(0, 0, 208, 3),
    "ForgeBriefingTestsPineFelt": Rect(0, 0, 208, 3),
    "ForgeBriefingContextPatinaRule": Rect(0, 54, 208, 3),
    "ForgeBriefingTestingPatinaRule": Rect(0, 54, 208, 3),
    "ForgeBriefingEvidencePatinaRule": Rect(0, 54, 208, 3),
    "ForgeBriefingTestsPatinaRule": Rect(0, 54, 208, 3),
}
EXPECTED_LOCAL_RECTS.update(
    {element_id: Rect(256, 114, 24, 12) for element_id in ROUTE_ORNAMENTS}
)

VIEWPORT_PROFILES = ((1220, 880), (1280, 720), (1600, 900), (1920, 1080))
SHELL_MAX_WIDTH = 1760
SHELL_MAX_HEIGHT = 1024
SHELL_MARGIN = 24


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def number(value: str | None) -> float | None:
    if value is None:
        return None
    try:
        return float(value.rstrip("fF"))
    except ValueError:
        return None


def data_source_properties(source: str) -> set[str]:
    """Collect properties explicitly exported to Gauntlet by the ViewModel."""
    pattern = re.compile(
        r"\[DataSourceProperty\]\s*"
        r"(?:(?:public|private|protected|internal|static|virtual|override|new)\s+)*"
        r"[\w.<>,?\[\]]+\s+(?P<name>[A-Za-z_]\w*)\s*(?=\{|=>)",
        re.MULTILINE,
    )
    return {match.group("name") for match in pattern.finditer(source)}


def public_command_methods(source: str) -> set[str]:
    """Collect public methods eligible for Command.Click bindings."""
    pattern = re.compile(
        r"\bpublic\s+(?:(?:async|static|virtual|override|new)\s+)*"
        r"[\w.<>,?\[\]]+\s+(?P<name>Execute[A-Za-z0-9_]*)\s*\(",
        re.MULTILINE,
    )
    return {match.group("name") for match in pattern.finditer(source)}


def item_list_types(source: str) -> dict[str, str]:
    """Map ViewModel list properties to their Gauntlet ItemTemplate types."""
    pattern = re.compile(
        r"\[DataSourceProperty\]\s*"
        r"(?:(?:public|private|protected|internal|static|virtual|override|new)\s+)*"
        r"MBBindingList\s*<\s*(?P<item>[A-Za-z_]\w*)\s*>\s+"
        r"(?P<name>[A-Za-z_]\w*)\s*(?=\{|=>)",
        re.MULTILINE,
    )
    return {match.group("name"): match.group("item") for match in pattern.finditer(source)}


def class_tail(source: str, class_name: str) -> str | None:
    """Return the source from a class declaration, for item-scoped contracts."""
    match = re.search(rf"\bclass\s+{re.escape(class_name)}\b", source)
    return source[match.start():] if match else None


def item_template_for(
    node: ET.Element,
    parents: dict[ET.Element, ET.Element],
) -> ET.Element | None:
    current: ET.Element | None = node
    while current is not None:
        if local_name(current.tag) == "ItemTemplate":
            return current
        current = parents.get(current)
    return None


def conditional_numeric_values(source: str, property_name: str) -> tuple[float, float] | None:
    """Read the true/false numeric arms of a simple ViewModel ternary property."""
    pattern = re.compile(
        rf"\b{re.escape(property_name)}\s*=>\s*[^?;]+\?\s*"
        r"(?P<true>-?\d+(?:\.\d+)?f?)\s*:\s*"
        r"(?P<false>-?\d+(?:\.\d+)?f?)\s*;",
        re.MULTILINE,
    )
    match = pattern.search(source)
    if not match:
        return None
    return number(match.group("true")), number(match.group("false"))  # type: ignore[return-value]


def intersects(a: Rect, b: Rect) -> bool:
    return a.left < b.right and b.left < a.right and a.top < b.bottom and b.top < a.bottom


def descendant_map(root: ET.Element) -> dict[ET.Element, ET.Element]:
    return {child: parent for parent in root.iter() for child in parent}


def png_max_alpha(sprite_name: str) -> int | None:
    """Read the maximum alpha byte from the project's small RGBA8 sprite PNG."""
    path = SPRITE_PART_ROOT / "ui_calradiaforge" / f"{sprite_name}.png"
    try:
        data = path.read_bytes()
    except OSError:
        return None
    signature = b"\x89PNG\r\n\x1a\n"
    if len(data) > 4 * 1024 * 1024 or not data.startswith(signature):
        return None
    offset = len(signature)
    width = height = depth = color_type = -1
    image_data = bytearray()
    try:
        while offset + 12 <= len(data):
            length = struct.unpack_from(">I", data, offset)[0]
            kind = data[offset + 4:offset + 8]
            start, end = offset + 8, offset + 8 + length
            if end + 4 > len(data):
                return None
            body = data[start:end]
            if kind == b"IHDR":
                width, height, depth, color_type = struct.unpack_from(">IIBB", body)
            elif kind == b"IDAT":
                image_data.extend(body)
            offset = end + 4
            if kind == b"IEND":
                break
        if not (0 < width <= 1024 and 0 < height <= 1024 and depth == 8 and color_type == 6):
            return None
        row_size = width * 4
        raw = zlib.decompress(bytes(image_data))
        if len(raw) != (row_size + 1) * height:
            return None
        previous = bytearray(row_size)
        alpha_max = 0
        cursor = 0
        for _ in range(height):
            filter_type = raw[cursor]
            cursor += 1
            row = bytearray(raw[cursor:cursor + row_size])
            cursor += row_size
            if filter_type not in (0, 1, 2, 3, 4):
                return None
            for index in range(row_size):
                left = row[index - 4] if index >= 4 else 0
                up = previous[index]
                upper_left = previous[index - 4] if index >= 4 else 0
                if filter_type == 1:
                    predictor = left
                elif filter_type == 2:
                    predictor = up
                elif filter_type == 3:
                    predictor = (left + up) // 2
                elif filter_type == 4:
                    base = left + up - upper_left
                    pa, pb, pc = abs(base - left), abs(base - up), abs(base - upper_left)
                    predictor = left if pa <= pb and pa <= pc else (up if pb <= pc else upper_left)
                else:
                    predictor = 0
                row[index] = (row[index] + predictor) & 255
            alpha_max = max(alpha_max, max(row[3::4]))
            previous = row
        return alpha_max
    except (ValueError, struct.error, zlib.error):
        return None


def widget_rect(
    node: ET.Element,
    parent_rect: Rect,
    vm_source: str,
    state: bool,
    flow_x: float | None = None,
    flow_y: float | None = None,
    detailed_mode: bool = False,
) -> Rect | None:
    """Resolve one prefab widget into shell coordinates, including stack flow."""
    margins = []
    for key in ("MarginLeft", "MarginRight", "MarginTop", "MarginBottom"):
        raw_margin = node.attrib.get(key, "0")
        margin = number(raw_margin)
        if raw_margin.startswith("@"):
            binding = raw_margin[1:]
            alternatives = conditional_numeric_values(vm_source, binding)
            if alternatives and binding == "WorkspaceRightMargin":
                margin = alternatives[0] if detailed_mode and not state else alternatives[1]
            else:
                margin = alternatives[0 if state else 1] if alternatives else None
        margins.append(margin)
    if any(value is None for value in margins):
        return None
    ml, mr, mt, mb = margins

    def dimension(key: str, policy_key: str, parent_size: float, before: float, after: float) -> float | None:
        raw = node.attrib.get(key)
        value = number(raw)
        if raw and raw.startswith("@"):
            binding = raw[1:]
            if binding == "EvidenceHeight":
                alternatives = conditional_numeric_values(vm_source, binding)
                value = alternatives[0 if state else 1] if alternatives else None
            elif binding == "PrimaryActionButtonWidth":
                alternatives = conditional_numeric_values(vm_source, binding)
                value = alternatives[0] if detailed_mode and not state and alternatives else alternatives[1] if alternatives else None
            else:
                value = None
        if node.attrib.get(policy_key) == "StretchToParent":
            value = parent_size - before - after
        cap = number(node.attrib.get("MaxWidth" if policy_key == "WidthSizePolicy" else "MaxHeight"))
        if value is not None and cap is not None:
            value = min(value, cap)
        return value

    width = dimension("SuggestedWidth", "WidthSizePolicy", parent_rect.width, ml, mr)
    height = dimension("SuggestedHeight", "HeightSizePolicy", parent_rect.height, mt, mb)
    if width is None or height is None or width < 0 or height < 0:
        return None

    horizontal = node.attrib.get("HorizontalAlignment", "Left")
    vertical = node.attrib.get("VerticalAlignment", "Top")
    if flow_x is not None:
        left = flow_x + ml
    elif horizontal == "Right":
        left = parent_rect.left + parent_rect.width - mr - width
    elif horizontal == "Center":
        left = parent_rect.left + ml + (parent_rect.width - ml - mr - width) / 2
    else:
        left = parent_rect.left + ml
    if flow_y is not None:
        top = flow_y + mt
    elif vertical == "Bottom":
        top = parent_rect.top + parent_rect.height - mb - height
    elif vertical == "Center":
        top = parent_rect.top + mt + (parent_rect.height - mt - mb - height) / 2
    else:
        top = parent_rect.top + mt
    return Rect(left, top, width, height)


def effective_shell_rect(viewport: tuple[int, int]) -> Rect:
    """Model the centered 24-DIP inset and 1760x1024 cap for one viewport."""
    width, height = viewport
    return Rect(
        0,
        0,
        min(SHELL_MAX_WIDTH, max(0, width - SHELL_MARGIN * 2)),
        min(SHELL_MAX_HEIGHT, max(0, height - SHELL_MARGIN * 2)),
    )


def shell_layout(
    shell: ET.Element,
    vm_source: str,
    state: bool,
    viewport: tuple[int, int] | None = None,
    detailed_mode: bool = False,
) -> dict[ET.Element, Rect]:
    """Return rectangles for deterministic controls and content at one viewport."""
    if viewport is None:
        shell_rect = Rect(
            0,
            0,
            number(shell.attrib.get("SuggestedWidth")) or 0,
            number(shell.attrib.get("SuggestedHeight")) or 0,
        )
    else:
        shell_rect = effective_shell_rect(viewport)
    result: dict[ET.Element, Rect] = {shell: shell_rect}

    def visit(parent: ET.Element, parent_rect: Rect) -> None:
        children_groups = [child for child in parent if local_name(child.tag) == "Children"]
        if not children_groups:
            return
        method = parent.attrib.get("StackLayout.LayoutMethod", "")
        horizontal_stack = "Horizontal" in method
        vertical_stack = "Vertical" in method
        flow_x, flow_y = parent_rect.left, parent_rect.top
        for children in children_groups:
            for child in children:
                rect = widget_rect(
                    child, parent_rect, vm_source, state,
                    flow_x if horizontal_stack else None,
                    flow_y if vertical_stack else None,
                    detailed_mode,
                )
                if rect is None:
                    continue
                result[child] = rect
                visit(child, rect)
                if horizontal_stack:
                    flow_x = rect.right + (number(child.attrib.get("MarginRight", "0")) or 0)
                elif vertical_stack:
                    flow_y = rect.bottom + (number(child.attrib.get("MarginBottom", "0")) or 0)

    visit(shell, shell_rect)
    return result


def direct_parent(node: ET.Element, parents: dict[ET.Element, ET.Element]) -> ET.Element | None:
    wrapper = parents.get(node)
    if wrapper is None:
        return None
    return parents.get(wrapper) if local_name(wrapper.tag) == "Children" else wrapper


def contained(parent: Rect, child: Rect) -> bool:
    return (parent.left <= child.left and parent.top <= child.top
            and parent.right >= child.right and parent.bottom >= child.bottom)


def visible_in_evidence_state(
    node: ET.Element,
    parents: dict[ET.Element, ET.Element],
    state: bool,
    active_route: str | None = None,
    detailed_mode: bool = False,
) -> bool:
    """Resolve the visibility contract relevant to the normal/focused layout audit.

    ``state`` is the evidence-focused state. The command deck and its briefing
    cards bind visibility to ``ShowCommandDeck``, so those descendants must not
    be treated as painted over the expanded evidence frame while that binding
    is false. Interactive popovers, including the modal navigation palette, are
    audited in their closed resting state; their own controls are separately
    checked in their own layout space.
    """
    current: ET.Element | None = node
    while current is not None:
        visibility = current.attrib.get("IsVisible", "").strip()
        if visibility.casefold() == "false":
            return False
        if visibility == "@ShowCommandDeck" and state:
            return False
        if visibility.startswith("@"):
            route = ROUTE_CANONICAL_ROUTE_BY_BINDING.get(visibility[1:])
            if route is not None and route != active_route:
                return False
        if visibility == "@IsPlaybookVisible":
            if not detailed_mode or state:
                return False
        elif visibility in {
            "@IsSdkCatalogOpen", "@IsHistoryVisible", "@IsKeyHelpOpen",
            "@IsToastVisible", "@IsNavigationPaletteOpen", "@IsCategoryCommandsOpen",
        }:
            return False
        current = parents.get(current)
    return True


class Audit:
    def __init__(self) -> None:
        self.errors: list[str] = []
        self.warnings: list[str] = []

    def error(self, message: str) -> None:
        self.errors.append(message)

    def warning(self, message: str) -> None:
        self.warnings.append(message)


def find_by_id(root: ET.Element, element_id: str) -> ET.Element | None:
    return next((node for node in root.iter() if node.attrib.get("Id") == element_id), None)


def custom_sprite_name(name: str) -> bool:
    return name.startswith("calradiaforge_") or name.startswith("forge_")


EXPECTED_SCROLL_PANELS = {
    "ForgeEvidenceScroll": "ForgeEvidenceScrollBar",
    "ForgeSdkCatalogScroll": "ForgeSdkCatalogScrollBar",
    "ForgeCommandHistoryScroll": "ForgeCommandHistoryScrollBar",
    "ForgeCategoryCommandsScroll": "ForgeCategoryCommandsScrollBar",
    "ForgePlaybookScroll": "ForgePlaybookScrollBar",
    "NavigationPaletteScroll": "NavigationPaletteScrollBar",
}

PLAYBOOK_TEXT_LAYOUT = {
    "CategoryPlaybookTitle": ("GetCategoryPlaybookTitle", 30),
    "CategoryPlaybookStep1": ("GetCategoryPlaybookStep1", 34),
    "CategoryPlaybookStep2": ("GetCategoryPlaybookStep2", 34),
    "CategoryPlaybookStep3": ("GetCategoryPlaybookStep3", 34),
    "CategoryTroubleshootingTitle": ("GetCategoryTroubleshootingTitle", 38),
    "CategoryTroubleshootingAdvice": ("GetCategoryTroubleshootingAdvice", 42),
    "CategoryRecommendedMacro": ("GetCategoryRecommendedMacro", 34),
}
PLAYBOOK_FLOW_ORDER = (
    "ForgePlaybookTitle", "ForgePlaybookBrassRule1", "ForgePlaybookStep1",
    "ForgePlaybookStep2", "ForgePlaybookStep3", "ForgePlaybookFeltRule",
    "ForgeTroubleshootingTitle", "ForgeTroubleshootingAdvice", "ForgePlaybookBrassRule2",
    "ForgeRecommendedMacro", "ForgeRunMacro",
)


def is_scrollbar_widget(node: ET.Element) -> bool:
    # Gauntlet's widget type is case-sensitive. In particular, ScrollBarWidget
    # (capital B) is not the engine's ScrollbarWidget and triggers an assertion.
    return local_name(node.tag) == "ScrollbarWidget"


def validate_scrollbar_contracts(audit: Audit, prefab: ET.Element) -> None:
    """Ensure each supported scrolling surface has a wired sibling scrollbar.

    Gauntlet uses a path relative to the ScrollablePanel for its scrollbar
    reference. The scrollbar itself must sit beside the panel, while its handle
    is an identified child of that ScrollbarWidget. A bar nested in the panel's
    content would scroll with the content and is therefore rejected.
    """
    if not is_scrollbar_widget(ET.Element("ScrollbarWidget")) \
            or is_scrollbar_widget(ET.Element("ScrollBarWidget")):
        audit.error("Scrollbar widget audit must accept only the exact engine tag ScrollbarWidget")

    parents = descendant_map(prefab)
    scroll_panels = [node for node in prefab.iter() if local_name(node.tag) == "ScrollablePanel"]
    panel_ids = {node.attrib.get("Id", "<none>") for node in scroll_panels}
    expected_panel_ids = set(EXPECTED_SCROLL_PANELS)
    if panel_ids != expected_panel_ids:
        missing = sorted(expected_panel_ids - panel_ids)
        unexpected = sorted(panel_ids - expected_panel_ids)
        details = []
        if missing:
            details.append(f"missing {missing}")
        if unexpected:
            details.append(f"unexpected {unexpected}")
        audit.error("ScrollablePanel inventory must match the six supported surfaces: " + "; ".join(details))

    for panel_id, expected_bar_id in EXPECTED_SCROLL_PANELS.items():
        panel = find_by_id(prefab, panel_id)
        if panel is None or local_name(panel.tag) != "ScrollablePanel":
            audit.error(f"Expected ScrollablePanel Id={panel_id}")
            continue

        expected_reference = "..\\" + expected_bar_id
        actual_reference = panel.attrib.get("VerticalScrollbar", "").strip()
        if actual_reference != expected_reference:
            audit.error(
                f"{panel_id} VerticalScrollbar must point to {expected_reference!r}; "
                f"found {actual_reference!r}"
            )

        scrollbar = find_by_id(prefab, expected_bar_id)
        if scrollbar is None or not is_scrollbar_widget(scrollbar):
            audit.error(f"{panel_id} requires sibling ScrollbarWidget Id={expected_bar_id}")
            continue

        panel_wrapper = parents.get(panel)
        scrollbar_wrapper = parents.get(scrollbar)
        if (panel_wrapper is None or local_name(panel_wrapper.tag) != "Children"
                or scrollbar_wrapper is not panel_wrapper):
            audit.error(f"{expected_bar_id} must be a direct sibling of {panel_id}")

        if scrollbar.attrib.get("AlignmentAxis") != "Vertical":
            audit.error(f"{expected_bar_id} must set AlignmentAxis=Vertical")

        handle_id = scrollbar.attrib.get("Handle", "").strip()
        handle = find_by_id(scrollbar, handle_id) if handle_id else None
        if not handle_id or handle is None or handle is scrollbar:
            audit.error(f"{expected_bar_id} Handle must reference an identified child widget")
        else:
            handle_wrapper = parents.get(handle)
            if (handle_wrapper is None or local_name(handle_wrapper.tag) != "Children"
                    or parents.get(handle_wrapper) is not scrollbar):
                audit.error(f"{expected_bar_id} Handle={handle_id} must be a child of the scrollbar")

        nested_bars = [node for node in panel.iter()
                       if node is not panel and is_scrollbar_widget(node)]
        if nested_bars:
            nested_ids = [node.attrib.get("Id", "<none>") for node in nested_bars]
            audit.error(f"{panel_id} must not contain nested ScrollbarWidget controls: {nested_ids}")


def _method_localized_literals(source: str, method_name: str) -> list[str]:
    """Read fixed T(...) or literal examples from one ViewModel playbook selector."""
    start = re.search(rf"^\s*string\s+{re.escape(method_name)}\s*\(", source, re.MULTILINE)
    if start is None:
        return []
    following = re.search(r"^\s*string\s+\w+\s*\(", source[start.end():], re.MULTILINE)
    end = start.end() + following.start() if following is not None else len(source)
    body = source[start.start():end]
    return re.findall(r'return\s+(?:T\()?"([^"\\]*(?:\\.[^"\\]*)*)"\)?\s*;', body)


def _wrap_playbook_sample(text: str, max_line_length: int) -> list[str]:
    """Mirror WrapPlaybookText's word-boundary wrapping for source regressions."""
    text = text.replace(r"\n", " ").replace(r"\r", " ").replace(r"\t", " ").replace(r"\\", "\\")
    words = text.split()
    lines: list[str] = []
    current = ""
    for word in words:
        candidate = word if not current else current + " " + word
        if current and len(candidate) > max_line_length:
            lines.append(current)
            current = word
        else:
            current = candidate
    if current:
        lines.append(current)
    return lines


def validate_playbook_text_contracts(audit: Audit, prefab: ET.Element, vm_source: str) -> None:
    """Require the responsive Playbook text tree to grow and scroll with its content."""
    parents = descendant_map(prefab)
    required_bindings = {
        "ForgePlaybookTitle": "CategoryPlaybookTitle",
        "ForgePlaybookStep1": "CategoryPlaybookStep1",
        "ForgePlaybookStep2": "CategoryPlaybookStep2",
        "ForgePlaybookStep3": "CategoryPlaybookStep3",
        "ForgeTroubleshootingTitle": "CategoryTroubleshootingTitle",
        "ForgeTroubleshootingAdvice": "CategoryTroubleshootingAdvice",
        "ForgeRecommendedMacro": "CategoryRecommendedMacro",
    }
    content = find_by_id(prefab, "ForgePlaybookContent")
    flow = find_by_id(prefab, "ForgePlaybookFlow")
    clip = find_by_id(prefab, "ForgePlaybookClip")
    scroll = find_by_id(prefab, "ForgePlaybookScroll")
    if content is None or local_name(content.tag) != "Widget" \
            or content.attrib.get("WidthSizePolicy") != "StretchToParent" \
            or content.attrib.get("HeightSizePolicy") != "CoverChildren" \
            or content.attrib.get("ClipContents") != "true":
        audit.error("ForgePlaybookContent must grow to cover its children inside the clipped scroll viewport")
    if flow is None or local_name(flow.tag) != "ListPanel" \
            or flow.attrib.get("WidthSizePolicy") != "StretchToParent" \
            or flow.attrib.get("HeightSizePolicy") != "CoverChildren" \
            or flow.attrib.get("StackLayout.LayoutMethod") != "VerticalTopToBottom" \
            or content is None or parents.get(flow) is None \
            or parents.get(parents.get(flow)) is not content:
        audit.error("ForgePlaybookFlow must vertically stack variable-height text inside ForgePlaybookContent")
    flow_children = next((child for child in flow if local_name(child.tag) == "Children"), None) if flow is not None else None
    flow_child_ids = tuple(child.attrib.get("Id", "") for child in list(flow_children)) if flow_children is not None else ()
    if flow_child_ids != PLAYBOOK_FLOW_ORDER:
        audit.error("ForgePlaybookFlow must retain the ordered text, passive separators, and final macro action")
    if clip is None or clip.attrib.get("ClipContents") != "true" or scroll is None \
            or scroll.attrib.get("ClipRect") != "ForgePlaybookClip" \
            or scroll.attrib.get("InnerPanel") != "ForgePlaybookClip\\ForgePlaybookContent":
        audit.error("ForgePlaybookScroll must clip and scroll the complete cover-children Playbook content")
    if scroll is not None and (
        scroll.attrib.get("MarginLeft") != "10"
        or scroll.attrib.get("MarginTop") != "42"
        or scroll.attrib.get("MarginRight") != "18"
        or scroll.attrib.get("MarginBottom") != "10"
    ):
        audit.error("ForgePlaybookScroll must preserve the measured viewport inset and bottom reachability")

    scrollbar = find_by_id(prefab, "ForgePlaybookScrollBar")
    if scrollbar is None or not is_scrollbar_widget(scrollbar) \
            or scrollbar.attrib.get("WidthSizePolicy") != "Fixed" \
            or scrollbar.attrib.get("SuggestedWidth") != "8" \
            or scrollbar.attrib.get("MarginRight") != "10" \
            or scrollbar.attrib.get("MarginTop") != "42" \
            or scrollbar.attrib.get("MarginBottom") != "10":
        audit.error("ForgePlaybookScrollBar must stay adjacent to the viewport and within its top/bottom bounds")

    for element_id, property_name in required_bindings.items():
        node = find_by_id(prefab, element_id)
        if node is None or local_name(node.tag) != "TextWidget":
            audit.error(f"Playbook text {element_id} must remain a TextWidget")
            continue
        expected = "@" + property_name
        if node.attrib.get("Text") != expected:
            audit.error(f"Playbook text {element_id} must retain binding Text={expected}")
        if node.attrib.get("WidthSizePolicy") != "StretchToParent" \
                or node.attrib.get("HeightSizePolicy") != "CoverChildren":
            audit.error(f"Playbook text {element_id} must wrap within the viewport and cover its measured height")
        ancestor = node
        inside_flow = False
        while ancestor is not None:
            if ancestor is flow:
                inside_flow = True
                break
            ancestor = parents.get(ancestor)
        if not inside_flow:
            audit.error(f"Playbook text {element_id} must participate in the vertically scrolling content flow")

    # CoverChildren alone only measures vertical growth; require actual newline
    # generation at word boundaries and keep every source token within the
    # measured viewport's readable line budget (about 266 DIP at 320-DIP width).
    for property_name, (method_name, max_chars) in PLAYBOOK_TEXT_LAYOUT.items():
        getter = re.search(
            rf"\bpublic\s+string\s+{re.escape(property_name)}\s*=>\s*"
            rf"WrapPlaybookText\(\s*{re.escape(method_name)}\(currentCategory\)\s*,\s*{max_chars}\s*\)\s*;",
            vm_source,
        )
        if getter is None:
            audit.error(f"{property_name} must wrap {method_name} at the audited {max_chars}-character line budget")
            continue
        samples = _method_localized_literals(vm_source, method_name)
        if not samples:
            audit.error(f"{method_name} must keep source strings for Playbook wrapping regressions")
            continue
        for sample in samples:
            words = sample.replace(r"\n", " ").replace(r"\\", "\\").split()
            if any(len(word) > max_chars for word in words):
                audit.error(f"{method_name} has a token wider than its {max_chars}-character wrapping budget")
                break
            lines = _wrap_playbook_sample(sample, max_chars)
            if not lines or max(map(len, lines)) > max_chars:
                audit.error(f"{method_name} does not wrap source text within {max_chars} characters per line")
                break
            if len(sample) > max_chars and len(lines) < 2:
                audit.error(f"{method_name} long sample must produce explicit wrapped lines")
                break

    wrapper_match = re.search(r"static\s+string\s+WrapPlaybookText\s*\([^)]*\)\s*\{", vm_source)
    wrapper_body = ""
    if wrapper_match is not None:
        body_start = wrapper_match.end()
        depth = 1
        cursor = body_start
        while cursor < len(vm_source) and depth:
            if vm_source[cursor] == "{":
                depth += 1
            elif vm_source[cursor] == "}":
                depth -= 1
            cursor += 1
        if depth == 0:
            wrapper_body = vm_source[body_start:cursor - 1]
    if not wrapper_body or re.search(r"Append\(\s*'\\n'\s*\)", wrapper_body) is None:
        audit.error("WrapPlaybookText must insert explicit newline characters rather than relying on cover sizing")

    playbook_close = find_by_id(prefab, "ForgePlaybookClose")
    macro_button = find_by_id(prefab, "ForgeRunMacro")
    if playbook_close is None or playbook_close.attrib.get("Command.Click") != "ExecuteToggleDetailMode":
        audit.error("Playbook close action must preserve ExecuteToggleDetailMode")
    if macro_button is None or macro_button.attrib.get("Command.Click") != "ExecuteRunMacro" \
            or macro_button.attrib.get("Hint.HintText") != "@MacroActionHint":
        audit.error("Playbook macro action must preserve ExecuteRunMacro and its hint binding")
    if macro_button is None or parents.get(parents.get(macro_button)) is not flow \
            or macro_button.attrib.get("HeightSizePolicy") != "Fixed" \
            or macro_button.attrib.get("SuggestedHeight") != "36" \
            or macro_button.attrib.get("MarginBottom") != "12":
        audit.error("ForgeRunMacro must remain the final bounded control inside the scrollable Playbook flow")


def validate_contracts(
    audit: Audit,
    prefab: ET.Element,
    viewmodel_source: str,
    tool_item_source: str,
    brush_root: ET.Element,
    sprite_root: ET.Element,
) -> None:
    validate_scrollbar_contracts(audit, prefab)
    validate_playbook_text_contracts(audit, prefab, viewmodel_source)
    props = data_source_properties(viewmodel_source)
    methods = public_command_methods(viewmodel_source)
    list_types = item_list_types(viewmodel_source)
    parents = descendant_map(prefab)
    template_contracts: dict[ET.Element, tuple[set[str], set[str]]] = {}
    for template in (node for node in prefab.iter() if local_name(node.tag) == "ItemTemplate"):
        list_panel = parents.get(template)
        data_source = list_panel.attrib.get("DataSource", "").strip() if list_panel is not None else ""
        list_property = data_source.strip("{}").lstrip("@").strip()
        item_type = list_types.get(list_property)
        item_source = tool_item_source if item_type == "ToolItemVM" else viewmodel_source
        scoped_source = class_tail(item_source, item_type) if item_type else None
        if scoped_source is None:
            list_id = list_panel.attrib.get("Id", "<none>") if list_panel is not None else "<none>"
            audit.error(f"Cannot resolve ItemTemplate data context for ListPanel {list_id}")
            template_contracts[template] = (set(), set())
        else:
            template_contracts[template] = (
                data_source_properties(scoped_source),
                public_command_methods(scoped_source),
            )

    # Gauntlet resolves IDs globally within a prefab; duplicate IDs can silently
    # bind events or focus to the wrong widget, so compare case-insensitively.
    ids: dict[str, list[ET.Element]] = {}
    for node in prefab.iter():
        element_id = node.attrib.get("Id")
        if element_id:
            ids.setdefault(element_id.casefold(), []).append(node)
    for folded, nodes in ids.items():
        if len(nodes) > 1:
            names = ", ".join(node.attrib.get("Id", "") for node in nodes)
            audit.error(f"Duplicate prefab Id (case-insensitive): {names}")

    for node in prefab.iter():
        template = item_template_for(node, parents)
        node_props = template_contracts.get(template, (props, methods))[0] if template is not None else props
        for key, value in node.attrib.items():
            for binding in re.findall(r"@([A-Za-z_]\w*)", value):
                if binding not in node_props:
                    audit.error(
                        f"Unknown Gauntlet binding @{binding} on "
                        f"{node.tag} Id={node.attrib.get('Id', '<none>')} ({key})"
                    )

    command_nodes = [node for node in prefab.iter() if node.attrib.get("Command.Click")]
    for node in command_nodes:
        command = node.attrib["Command.Click"]
        template = item_template_for(node, parents)
        node_methods = template_contracts.get(template, (props, methods))[1] if template is not None else methods
        if command not in node_methods:
            audit.error(
                f"Command.Click target {command} has no public ViewModel method "
                f"(widget Id={node.attrib.get('Id', '<none>')})"
            )

    for template in template_contracts:
        for node in template.iter():
            widget_type = local_name(node.tag)
            if widget_type not in {"ButtonWidget", "EditableTextWidget"}:
                continue
            width_policy = node.attrib.get("WidthSizePolicy", "")
            height_policy = node.attrib.get("HeightSizePolicy", "")
            width = number(node.attrib.get("SuggestedWidth"))
            height = number(node.attrib.get("SuggestedHeight"))
            if width_policy not in {"Fixed", "StretchToParent", "CoverChildren"}:
                audit.error(f"ItemTemplate {widget_type} has unsupported WidthSizePolicy={width_policy}")
            elif width_policy == "Fixed" and (width is None or width <= 0):
                audit.error(f"ItemTemplate {widget_type} with fixed width must have positive SuggestedWidth")
            if height_policy not in {"Fixed", "StretchToParent", "CoverChildren"}:
                audit.error(f"ItemTemplate {widget_type} has unsupported HeightSizePolicy={height_policy}")
            elif height_policy == "Fixed" and (height is None or height <= 0):
                audit.error(f"ItemTemplate {widget_type} with fixed height must have positive SuggestedHeight")

    brushes = {
        node.attrib["Name"]
        for node in brush_root.iter()
        if local_name(node.tag) == "Brush" and node.attrib.get("Name", "").startswith("CalradiaForge.")
    }
    for node in prefab.iter():
        brush = node.attrib.get("Brush", "")
        if brush.startswith("CalradiaForge.") and brush not in brushes:
            audit.error(f"Unregistered custom brush {brush} on Id={node.attrib.get('Id', '<none>')}")

    parts: set[str] = set()
    part_occurrences: dict[str, int] = {}
    generic_occurrences: dict[str, int] = {}
    generic_sprites: dict[str, str] = {}
    part_to_category: dict[str, str] = {}
    always_loaded_categories: set[str] = set()
    for node in sprite_root.iter():
        tag = local_name(node.tag)
        if tag == "SpriteCategory":
            category_name = node.findtext("Name")
            if category_name and node.find("AlwaysLoad") is not None:
                always_loaded_categories.add(category_name)
        elif tag == "SpritePart":
            part_name = node.findtext("Name")
            category = node.findtext("CategoryName")
            if part_name:
                part_occurrences[part_name] = part_occurrences.get(part_name, 0) + 1
                parts.add(part_name)
                if category:
                    part_to_category[part_name] = category
        elif tag == "GenericSprite":
            sprite_name = node.findtext("Name")
            part_name = node.findtext("SpritePartName")
            if sprite_name and part_name:
                generic_occurrences[sprite_name] = generic_occurrences.get(sprite_name, 0) + 1
                generic_sprites[sprite_name] = part_name

    for part_name, count in sorted(part_occurrences.items()):
        if count > 1:
            audit.error(f"SpriteData has duplicate SpritePart name {part_name} ({count} entries)")
    for sprite_name, count in sorted(generic_occurrences.items()):
        if count > 1:
            audit.error(f"SpriteData has duplicate GenericSprite name {sprite_name} ({count} entries)")

    for sprite_name, part_name in generic_sprites.items():
        if part_name not in parts:
            audit.error(f"GenericSprite {sprite_name} points to missing SpritePart {part_name}")

    refs = {
        node.attrib.get("Sprite", "")
        for node in prefab.iter()
        if node.attrib.get("Sprite") and custom_sprite_name(node.attrib["Sprite"])
    }
    for sprite in sorted(refs):
        part = generic_sprites.get(sprite)
        if part is None:
            audit.error(f"Prefab custom sprite {sprite} has no GenericSprite registration")
            continue
        if part not in parts:
            audit.error(f"Prefab custom sprite {sprite} resolves to missing SpritePart {part}")
            continue
        category = part_to_category.get(part)
        if not category:
            audit.error(f"SpritePart {part} for {sprite} has no CategoryName")
        else:
            if category not in always_loaded_categories:
                audit.error(f"Sprite category {category} for {sprite} is not marked AlwaysLoad")
            if not (SPRITE_PART_ROOT / category / f"{part}.png").is_file():
                audit.error(f"SpritePart source PNG is missing: {category}/{part}.png")

    # The eight shell routes are a stable user-facing contract: their visible
    # labels, commands, radio state and active-state bindings must remain paired.
    nav_id = find_by_id(prefab, "ForgeAreaNavigation")
    if nav_id is None or local_name(nav_id.tag) != "ListPanel":
        audit.error("Missing ListPanel Id=ForgeAreaNavigation for the eight primary routes")
        return
    expected_routes = {
        "ForgeSummary": ("ExecuteSummary", "IsSummaryActive"),
        "ForgeModules": ("ExecuteModules", "IsModulesActive"),
        "ForgeLogs": ("ExecuteLogs", "IsLogsActive"),
        "ForgeInspector": ("ExecuteInspector", "IsInspectorActive"),
        "ForgeTests": ("ExecuteTests", "IsTestsActive"),
        "ForgeMetrics": ("ExecuteMetrics", "IsMetricsActive"),
        "ForgeFramework": ("ExecuteFramework", "IsFrameworkActive"),
        "ForgeExtensions": ("ExecuteExtensions", "IsExtensionsActive"),
    }
    route_nodes = {
        node.attrib.get("Id", ""): node
        for node in nav_id.iter()
        if node.attrib.get("Id", "").startswith("Forge")
        and node.attrib.get("Command.Click", "").startswith("Execute")
    }
    if set(route_nodes) != set(expected_routes):
        missing = sorted(set(expected_routes) - set(route_nodes))
        extra = sorted(set(route_nodes) - set(expected_routes))
        audit.error(f"Primary route set mismatch: missing={missing}, extra={extra}")
    for route_id, (command, selected_binding) in expected_routes.items():
        node = route_nodes.get(route_id)
        if node is None:
            continue
        if node.attrib.get("Command.Click") != command:
            audit.error(f"{route_id} must invoke {command}")
        if node.attrib.get("IsSelected") != f"@{selected_binding}":
            audit.error(f"{route_id} must bind radio selection to @{selected_binding}")
        if node.attrib.get("ButtonType") != "Radio":
            audit.error(f"{route_id} must use ButtonType=Radio")
        if node.attrib.get("IsFocusable", "").lower() != "true":
            audit.error(f"{route_id} must be keyboard focusable")
        marker_id = f"{route_id}SelectedMarker"
        marker = find_by_id(node, marker_id)
        if marker is None or local_name(marker.tag) != "Widget":
            audit.error(f"{route_id} must contain passive selection marker {marker_id}")
        else:
            if marker.attrib.get("IsVisible") != f"@{selected_binding}":
                audit.error(f"{marker_id} must follow @{selected_binding}")
            for passive_flag in ("DoNotAcceptEvents", "DoNotPassEventsToChildren"):
                if marker.attrib.get(passive_flag, "").lower() != "true":
                    audit.error(f"{marker_id} must set {passive_flag}=true")
            if marker.attrib.get("IsFocusable", "").lower() == "true":
                audit.error(f"{marker_id} must not be focusable")

    for route_id, expected_sprite in PRIMARY_ROUTE_ICONS.items():
        route_node = route_nodes.get(route_id)
        if route_node is None:
            continue
        icons = [node for node in route_node.iter() if local_name(node.tag) == "ImageWidget"]
        if len(icons) != 1:
            audit.error(f"{route_id} must contain exactly one navigation ImageWidget (found {len(icons)})")
            continue
        icon = icons[0]
        if icon.attrib.get("Sprite") != expected_sprite:
            audit.error(f"{route_id} must use navigation sprite {expected_sprite}")
        if icon.attrib.get("DoNotAcceptEvents", "").lower() != "true":
            audit.error(f"{route_id} navigation icon must set DoNotAcceptEvents=true")
        if icon.attrib.get("IsFocusable", "").lower() == "true":
            audit.error(f"{route_id} navigation icon must not be focusable")

    for card_id in (
        "BriefingContextValue",
        "BriefingTestingValue",
        "BriefingEvidenceCountValue",
        "BriefingTestCountValue",
    ):
        card = find_by_id(prefab, card_id)
        if card is None:
            audit.error(f"Missing briefing card {card_id}")
        elif card.attrib.get("Brush") != "CalradiaForge.BriefingCard":
            audit.error(f"{card_id} must use CalradiaForge.BriefingCard")
        elif card.attrib.get("DoNotAcceptEvents", "").lower() != "true":
            audit.error(f"{card_id} must remain passive with DoNotAcceptEvents=true")

    evidence = find_by_id(prefab, "ForgeEvidenceFrame")
    if evidence is None:
        audit.error("Missing ForgeEvidenceFrame")
        return
    expected_evidence = {
        "ForgeEvidenceHeading": ("Text", "@EvidenceHeading"),
        "ForgeEvidencePage": ("Text", "@PageLabel"),
        "ForgeEmptyEvidence": ("Text", "@ContentPlaceholder"),
        "ForgeEvidenceContent": ("Text", "@Content"),
    }
    for element_id, (attribute, expected_value) in expected_evidence.items():
        node = find_by_id(evidence, element_id)
        if node is None:
            audit.error(f"Evidence ledger is missing {element_id}")
        elif node.attrib.get(attribute) != expected_value:
            audit.error(f"{element_id} must bind {attribute}={expected_value}")
    empty = find_by_id(evidence, "ForgeEmptyEvidence")
    if empty is not None and empty.attrib.get("IsVisible") != "@IsContentEmpty":
        audit.error("ForgeEmptyEvidence must bind visibility to @IsContentEmpty")
    content = find_by_id(evidence, "ForgeEvidenceContent")
    if content is not None and content.attrib.get("ClipContents", "").lower() != "true":
        audit.error("ForgeEvidenceContent must clip long raw evidence to its viewport")
    if any(
        local_name(node.tag) == "ImageWidget" and custom_sprite_name(node.attrib.get("Sprite", ""))
        for node in evidence.iter()
    ):
        audit.error("Decorative custom sprites must not be placed over the evidence ledger")


def fixed_rect(node: ET.Element, vm_source: str, state: bool | None = None) -> Rect | None:
    x = number(node.attrib.get("MarginLeft", "0"))
    y = number(node.attrib.get("MarginTop", "0"))
    width = number(node.attrib.get("SuggestedWidth"))
    height = number(node.attrib.get("SuggestedHeight"))
    if node.attrib.get("SuggestedWidth", "").startswith("@") or node.attrib.get("SuggestedHeight", "").startswith("@"):
        key_width = node.attrib.get("SuggestedWidth", "").lstrip("@")
        key_height = node.attrib.get("SuggestedHeight", "").lstrip("@")
        if key_width == "EvidenceWidth":
            vals = conditional_numeric_values(vm_source, key_width)
            width = vals[0 if state else 1] if vals else None
        if key_height == "EvidenceHeight":
            vals = conditional_numeric_values(vm_source, key_height)
            height = vals[0 if state else 1] if vals else None
    if node.attrib.get("MarginLeft", "").startswith("@") or node.attrib.get("MarginTop", "").startswith("@"):
        key_x = node.attrib.get("MarginLeft", "").lstrip("@")
        key_y = node.attrib.get("MarginTop", "").lstrip("@")
        if key_x == "EvidenceLeft":
            vals = conditional_numeric_values(vm_source, key_x)
            x = vals[0 if state else 1] if vals else None
        if key_y == "EvidenceTop":
            vals = conditional_numeric_values(vm_source, key_y)
            y = vals[0 if state else 1] if vals else None
    if None in (x, y, width, height):
        return None
    return Rect(x, y, width, height)


def validate_decorative_layers(
    audit: Audit,
    prefab: ET.Element,
    shell: ET.Element,
    vm_source: str,
) -> None:
    """Check the approved texture layers and their safe paint/event behavior."""
    parents = descendant_map(prefab)
    nodes_by_id: dict[str, list[ET.Element]] = {}
    for node in prefab.iter():
        if node.attrib.get("Id"):
            nodes_by_id.setdefault(node.attrib["Id"], []).append(node)

    decorations: dict[str, ET.Element] = {}
    for element_id, (sprite_name, expected_parent_id) in DECORATIVE_IMAGES.items():
        matches = nodes_by_id.get(element_id, [])
        if len(matches) != 1:
            audit.error(f"Expected exactly one decorative image Id={element_id} (found {len(matches)})")
            continue
        node = matches[0]
        decorations[element_id] = node
        if local_name(node.tag) != "ImageWidget":
            audit.error(f"{element_id} must be an ImageWidget")
        if node.attrib.get("Sprite") != sprite_name:
            audit.error(f"{element_id} must reference Sprite={sprite_name}")
        if node.attrib.get("DoNotAcceptEvents", "").lower() != "true":
            audit.error(f"{element_id} must set DoNotAcceptEvents=true")
        if node.attrib.get("DoNotPassEventsToChildren", "").lower() != "true":
            audit.error(f"{element_id} must set DoNotPassEventsToChildren=true")

        parent = direct_parent(node, parents)
        actual_parent_id = parent.attrib.get("Id", "") if parent is not None else ""
        if expected_parent_id == "<shell>":
            if parent is not shell:
                audit.error(f"{element_id} must be a direct child of the workbench shell")
        elif actual_parent_id != expected_parent_id:
            audit.error(f"{element_id} must be a direct child of {expected_parent_id}")

        local = fixed_rect(node, vm_source)
        expected = EXPECTED_LOCAL_RECTS.get(element_id)
        if expected is not None and local != expected:
            audit.error(
                f"{element_id} rectangle mismatch: expected "
                f"({expected.left:g},{expected.top:g},{expected.width:g},{expected.height:g}), "
                f"found {None if local is None else (local.left, local.top, local.width, local.height)}"
            )

    def is_bounded_scroll_flow_item(node: ET.Element) -> bool:
        """Allow content-relative bounds only for the validated clipped Playbook flow."""
        flow = find_by_id(prefab, "ForgePlaybookFlow")
        content = find_by_id(prefab, "ForgePlaybookContent")
        clip = find_by_id(prefab, "ForgePlaybookClip")
        scroll = find_by_id(prefab, "ForgePlaybookScroll")
        scrollbar = find_by_id(prefab, "ForgePlaybookScrollBar")
        if None in (flow, content, clip, scroll, scrollbar):
            return False
        flow_children = next((child for child in flow if local_name(child.tag) == "Children"), None)
        flow_order = tuple(child.get("Id", "") for child in list(flow_children)) if flow_children is not None else ()
        expected_order = (
            "ForgePlaybookTitle", "ForgePlaybookBrassRule1", "ForgePlaybookStep1",
            "ForgePlaybookStep2", "ForgePlaybookStep3", "ForgePlaybookFeltRule",
            "ForgeTroubleshootingTitle", "ForgeTroubleshootingAdvice", "ForgePlaybookBrassRule2",
            "ForgeRecommendedMacro", "ForgeRunMacro",
        )
        scroll_parent = direct_parent(scroll, parents)
        if not (
            local_name(scrollbar.tag) == "ScrollbarWidget"
            and direct_parent(scrollbar, parents) is scroll_parent
            and scroll.get("ClipRect") == "ForgePlaybookClip"
            and scroll.get("InnerPanel") == "ForgePlaybookClip\\ForgePlaybookContent"
            and scroll.get("VerticalScrollbar") == "..\\ForgePlaybookScrollBar"
            and (scroll.get("AutoHideScrollBars") or "").lower() == "true"
            and [scroll.get(key) for key in ("MarginLeft", "MarginTop", "MarginRight", "MarginBottom")] == ["10", "42", "18", "10"]
            and [scrollbar.get(key) for key in ("SuggestedWidth", "MarginRight", "MarginTop", "MarginBottom")] == ["8", "10", "42", "10"]
            and clip.get("ClipContents") == "true"
            and content.get("ClipContents") == "true"
            and content.get("WidthSizePolicy") == "StretchToParent"
            and content.get("HeightSizePolicy") == "CoverChildren"
            and flow.get("WidthSizePolicy") == "StretchToParent"
            and flow.get("HeightSizePolicy") == "CoverChildren"
            and flow.get("StackLayout.LayoutMethod") == "VerticalTopToBottom"
            and direct_parent(content, parents) is clip
            and direct_parent(flow, parents) is content
            and direct_parent(node, parents) is flow
            and flow_order == expected_order
        ):
            return False

        if node.get("Id") == "ForgeRunMacro":
            return (local_name(node.tag) == "ButtonWidget"
                    and node.get("Command.Click") == "ExecuteRunMacro"
                    and node.get("WidthSizePolicy") == "StretchToParent"
                    and node.get("HeightSizePolicy") == "Fixed"
                    and node.get("SuggestedHeight") == "36"
                    and node.get("MarginBottom") == "12")
        return (node.get("Id") in {"ForgePlaybookBrassRule1", "ForgePlaybookFeltRule", "ForgePlaybookBrassRule2"}
                and local_name(node.tag) == "ImageWidget"
                and node.get("WidthSizePolicy") == "StretchToParent"
                and node.get("HeightSizePolicy") == "Fixed"
                and number(node, "SuggestedHeight") == 3
                and node.get("DoNotAcceptEvents", "").lower() == "true"
                and node.get("DoNotPassEventsToChildren", "").lower() == "true")

    layout_names = re.search(
        r"LayoutStatePropertyNames\s*=\s*new\[\]\s*\{(?P<items>.*?)\};",
        vm_source,
        re.S,
    )
    for element_id, (sprite_name, binding, canonical_route) in ROUTE_ORNAMENTS.items():
        node = decorations.get(element_id)
        if node is None:
            continue
        if node.attrib.get("IsVisible") != f"@{binding}":
            audit.error(f"{element_id} must be visible only through @{binding}")
        if node.attrib.get("Command.Click") or node.attrib.get("IsFocusable", "").lower() == "true":
            audit.error(f"{element_id} must remain a passive, non-focusable image")
        active_property = re.search(
            rf"\[DataSourceProperty\]\s*public\s+bool\s+{re.escape(binding)}\s*=>\s*current\s*==\s*\"([^\"]+)\"\s*;",
            vm_source,
        )
        if active_property is None or active_property.group(1) != canonical_route:
            audit.error(f"@{binding} must remain tied to the canonical route {canonical_route}")
        if layout_names is None or f"nameof({binding})" not in layout_names.group("items"):
            audit.error(f"@{binding} must remain in LayoutStatePropertyNames for route changes")

    # Illustrated cloth is a quiet surface underlay. It may sit behind text,
    # but its explicit bounds and the collision audit below keep it clear of
    # buttons, editable fields and evidence.
    header = find_by_id(prefab, "ForgeHeader")
    rail = find_by_id(prefab, "ForgeNavigationRail")
    if header is None or rail is None:
        audit.error("Decorative layer parent region ForgeHeader/ForgeNavigationRail is missing")
    else:
        header_children = header.find("Children")
        rail_children = rail.find("Children")
        header_cloth = decorations.get("ForgeHeaderCloth")
        rail_cloth = decorations.get("ForgeRailCloth")
        header_overlay = decorations.get("ForgeHeaderHeraldicOverlay")
        if header_children is None or not list(header_children) or list(header_children)[0] is not header_cloth:
            audit.error("ForgeHeaderCloth must be the first child, behind header content")
        if rail_children is None or rail_cloth is None or rail_cloth not in list(rail_children):
            audit.error("ForgeRailCloth must be a direct rail background child")
        elif find_by_id(rail, "ForgeRailGuidance") is not None:
            if list(rail_children).index(rail_cloth) >= list(rail_children).index(find_by_id(rail, "ForgeRailGuidance")):
                audit.error("ForgeRailCloth must be painted before ForgeRailGuidance text")
        if header_overlay is not None:
            if header_children is None or header_overlay not in list(header_children):
                audit.error("ForgeHeaderHeraldicOverlay must be a direct header child")
            elif header_cloth is not None and list(header_children).index(header_overlay) <= list(header_children).index(header_cloth):
                audit.error("ForgeHeaderHeraldicOverlay must paint above ForgeHeaderCloth")

    overlay = decorations.get("ForgeHeaderHeraldicOverlay")
    if overlay is not None and fixed_rect(overlay, vm_source) != Rect(650, 17, 220, 42):
        audit.error("ForgeHeaderHeraldicOverlay must stay inside the reserved header identity gap")
    header_overlay_alpha = png_max_alpha("forge_heraldic_header_v2")
    if header_overlay_alpha is None:
        audit.error("Cannot verify forge_heraldic_header_v2 alpha; expected an RGBA8 sprite PNG")
    elif header_overlay_alpha > 88:
        audit.error(f"forge_heraldic_header_v2 alpha must be at most 88/255 in the header identity gap (found {header_overlay_alpha})")
    header_cloth = decorations.get("ForgeHeaderCloth")
    if header_cloth is not None:
        expected_header_attrs = {
            "WidthSizePolicy": "StretchToParent",
            "HeightSizePolicy": "Fixed",
            "SuggestedHeight": "76",
            "MaxWidth": "1172",
            "HorizontalAlignment": "Center",
        }
        for key, expected_value in expected_header_attrs.items():
            if header_cloth.attrib.get(key) != expected_value:
                audit.error(f"ForgeHeaderCloth must use {key}={expected_value} for capped centered scaling")
    header_cloth_alpha = png_max_alpha("forge_war_table_cloth_v2")
    if header_cloth_alpha is None:
        audit.error("Cannot verify forge_war_table_cloth_v2 alpha; expected an RGBA8 sprite PNG")
    elif header_cloth_alpha > 24:
        audit.error(f"forge_war_table_cloth_v2 alpha must be at most 24/255 for text-underlay use (found {header_cloth_alpha})")

    rail_cloth = decorations.get("ForgeRailCloth")
    if rail_cloth is not None:
        expected_rail_attrs = {
            "WidthSizePolicy": "Fixed",
            "HeightSizePolicy": "StretchToParent",
            "SuggestedWidth": "128",
            "MaxHeight": "256",
            "HorizontalAlignment": "Center",
            "MarginLeft": "0",
            "MarginTop": "425",
            "MarginBottom": "5",
        }
        for key, expected_value in expected_rail_attrs.items():
            if rail_cloth.attrib.get(key) != expected_value:
                audit.error(f"ForgeRailCloth must use {key}={expected_value} for centered, bounded rail artwork")
    rail_cloth_alpha = png_max_alpha("forge_heraldic_rail_v2")
    if rail_cloth_alpha is None:
        audit.error("Cannot verify forge_heraldic_rail_v2 alpha; expected an RGBA8 sprite PNG")
    elif rail_cloth_alpha > 64:
        audit.error(f"forge_heraldic_rail_v2 alpha must be at most 64/255 for rail guidance underlay (found {rail_cloth_alpha})")
    rail_felt = decorations.get("ForgeRailPineFelt")
    if rail_felt is not None and fixed_rect(rail_felt, vm_source) != Rect(16, 394, 198, 8):
        audit.error("ForgeRailPineFelt must remain in the rail divider gap")

    # The briefing card accents are tiny first-paint-child strips. Keeping them
    # first prevents later card content from being reordered behind decoration.
    card_strips = {
        "ForgeBriefingContextPineFelt": "BriefingContextValue",
        "ForgeBriefingTestingPineFelt": "BriefingTestingValue",
        "ForgeBriefingEvidencePineFelt": "BriefingEvidenceCountValue",
        "ForgeBriefingTestsPineFelt": "BriefingTestCountValue",
    }
    for strip_id, card_id in card_strips.items():
        strip = decorations.get(strip_id)
        card = find_by_id(prefab, card_id)
        children = card.find("Children") if card is not None else None
        if strip is None or children is None or not list(children) or list(children)[0] is not strip:
            audit.error(f"{strip_id} must be the first direct child of {card_id}")

    card_bottom_strips = {
        "ForgeBriefingContextPatinaRule": "BriefingContextValue",
        "ForgeBriefingTestingPatinaRule": "BriefingTestingValue",
        "ForgeBriefingEvidencePatinaRule": "BriefingEvidenceCountValue",
        "ForgeBriefingTestsPatinaRule": "BriefingTestCountValue",
    }
    for strip_id, card_id in card_bottom_strips.items():
        strip = decorations.get(strip_id)
        card = find_by_id(prefab, card_id)
        children = card.find("Children") if card is not None else None
        if strip is None or children is None or not list(children) or list(children)[-1] is not strip:
            audit.error(f"{strip_id} must be the last direct child of {card_id}")

    # The card-edge accents and three shell rules share one source sprite. Keep
    # the complete placement set explicit so accidental texture use elsewhere
    # cannot silently add another painted layer.
    brass_ids = {
        node.attrib.get("Id") for node in prefab.iter()
        if node.attrib.get("Sprite") == "forge_patina_brass"
    }
    expected_brass_ids = {
        "ForgeBriefingContextPatinaRule",
        "ForgeBriefingTestingPatinaRule",
        "ForgeBriefingEvidencePatinaRule",
        "ForgeBriefingTestsPatinaRule",
        "ForgeTopBrassFrameRule",
        "ForgeEvidenceActionBrassRule",
        "ForgeBottomBrassFrameRule",
        "ForgePlaybookBrassRule1",
        "ForgePlaybookBrassRule2",
    }
    if brass_ids != expected_brass_ids:
        audit.error(f"forge_patina_brass placements must be exactly {sorted(expected_brass_ids)}; found {sorted(brass_ids)}")

    # Resolve the complete shell tree at each supported reference viewport and
    # both evidence states. This proves the texture fields stay off hit targets.
    protected_ids = {"ForgeCommandInputFrame", "ForgeAssemblyPathFrame", "ForgeAssemblyVersionFrame"}
    parents = descendant_map(prefab)
    evidence_frame = find_by_id(prefab, "ForgeEvidenceFrame")
    route_states = (*ROUTE_ACTIVE_BINDINGS.values(), None)
    for viewport in VIEWPORT_PROFILES:
        for state in (False, True):
            for active_route in route_states:
                rectangles = shell_layout(shell, vm_source, state, viewport)
                profile_name = (f"{viewport[0]}x{viewport[1]} "
                                f"{'focused' if state else 'normal'} "
                                f"route={active_route or 'auxiliary'}")
                expected_ornaments = {
                    element_id for element_id, (_, _, route) in ROUTE_ORNAMENTS.items()
                    if route == active_route
                }
                visible_ornaments = {
                    element_id for element_id in ROUTE_ORNAMENTS
                    if decorations.get(element_id) is not None
                    and visible_in_evidence_state(decorations[element_id], parents, state, active_route)
                }
                if visible_ornaments != expected_ornaments:
                    audit.error(f"Header ornaments for {profile_name} must be {sorted(expected_ornaments)}; found {sorted(visible_ornaments)}")
                for node in prefab.iter():
                    protected = (local_name(node.tag) in {"ButtonWidget", "EditableTextWidget"}
                                 or node.attrib.get("Id") in protected_ids)
                    if not protected:
                        continue
                    if item_template_for(node, parents) is not None:
                        continue
                    if node not in rectangles:
                        if is_bounded_scroll_flow_item(node):
                            continue
                        audit.error(f"Cannot resolve protected control geometry in {profile_name}: "
                                    f"{node.attrib.get('Id', local_name(node.tag))}")
                    elif rectangles[node].width <= 0 or rectangles[node].height <= 0:
                        audit.error(f"Protected control has empty geometry in {profile_name}: "
                                    f"{node.attrib.get('Id', local_name(node.tag))}")
                if evidence_frame is not None and evidence_frame not in rectangles:
                    audit.error(f"Cannot resolve evidence frame geometry in {profile_name}")
    
                for element_id, node in decorations.items():
                    if node not in rectangles:
                        if is_bounded_scroll_flow_item(node):
                            continue
                        audit.error(f"Cannot resolve {element_id} geometry in {profile_name}")
                        continue
                    deco_rect = rectangles[node]
                    if deco_rect.width <= 0 or deco_rect.height <= 0:
                        audit.error(f"{element_id} has empty geometry in {profile_name}")
                        continue
                    parent = direct_parent(node, parents)
                    parent_rect = rectangles.get(parent) if parent is not None else None
                    if parent_rect is not None and not contained(parent_rect, deco_rect):
                        audit.error(f"{element_id} exceeds its parent in {profile_name}")
                    if not visible_in_evidence_state(node, parents, state, active_route):
                        continue
    
                    for candidate, candidate_rect in rectangles.items():
                        if (candidate is node
                                or not visible_in_evidence_state(candidate, parents, state, active_route)
                                or not intersects(deco_rect, candidate_rect)):
                            continue
                        # Ancestor surfaces define the canvas that contains a child;
                        # they are not competing hit targets.
                        ancestor = parents.get(node)
                        is_ancestor = False
                        while ancestor is not None:
                            if ancestor is candidate:
                                is_ancestor = True
                                break
                            ancestor = parents.get(ancestor)
                        if is_ancestor:
                            continue
                        candidate_id = candidate.attrib.get("Id", "<none>")
                        candidate_tag = local_name(candidate.tag)
    
                        if candidate is evidence_frame:
                            audit.error(f"{element_id} overlaps the evidence ledger in {profile_name}")
                            continue
                        if candidate_tag in {"ButtonWidget", "EditableTextWidget"} or candidate.attrib.get("Id") in protected_ids:
                            audit.error(f"{element_id} overlaps protected control/input {candidate_tag} {candidate_id} in {profile_name}")
                            continue
    
                        other_deco = candidate_id in decorations
                        if other_deco and {element_id, candidate_id} == {"ForgeHeaderCloth", "ForgeHeaderHeraldicOverlay"}:
                            continue
                        if element_id in {"ForgeHeaderCloth", "ForgeRailCloth"}:
                            # Low-alpha textile/cartographic fields may underlay text,
                            # while hit targets and the evidence surface remain protected.
                            continue
                        if candidate_tag in {"TextWidget", "RichTextWidget"}:
                            audit.error(f"{element_id} overlaps static text {candidate_id} in {profile_name}")
                        elif other_deco:
                            audit.error(f"Unapproved decorative overlap: {element_id} with {candidate_id} in {profile_name}")
                        # Other inert parent/child layout frames are handled by their
                        # exact allowed-parent/rectangle checks above.


def validate_navigation_palette_geometry(
    audit: Audit,
    prefab: ET.Element,
    shell: ET.Element,
    vm_source: str,
    viewport: tuple[int, int],
) -> None:
    """Audit the modal palette as a separate layer above the closed shell."""
    viewport_name = f"{viewport[0]}x{viewport[1]}"
    required_ids = (
        "NavigationPaletteOverlay", "NavigationPalettePanel", "NavigationPaletteSurface",
        "NavigationPaletteHeader", "NavigationPaletteContext", "NavigationPaletteSearchFrame",
        "NavigationPaletteScroll", "NavigationPaletteEmptyState", "NavigationPaletteKeyboardHint",
        "NavigationPaletteHeading", "NavigationPaletteClose", "ForgeNavigationPaletteSearch",
        "NavigationPaletteSearchPlaceholder", "NavigationPaletteActiveRoute",
        "NavigationPaletteActiveGroup", "NavigationPaletteEmptyMessage", "NavigationPaletteItems",
    )
    nodes = {element_id: find_by_id(prefab, element_id) for element_id in required_ids}
    missing = [element_id for element_id, node in nodes.items() if node is None]
    if missing:
        audit.error(f"Navigation palette is missing {', '.join(missing)} in {viewport_name}")
        return

    overlay = nodes["NavigationPaletteOverlay"]
    panel = nodes["NavigationPalettePanel"]
    surface = nodes["NavigationPaletteSurface"]
    parents = descendant_map(prefab)
    if overlay.attrib.get("IsVisible") != "@IsNavigationPaletteOpen":
        audit.error("NavigationPaletteOverlay must bind visibility to @IsNavigationPaletteOpen")
    if direct_parent(overlay, parents) is not shell:
        audit.error("NavigationPaletteOverlay must be a direct child of ForgeWorkbenchShell")
    shell_children = shell.find("Children")
    if shell_children is None or not list(shell_children) or list(shell_children)[-1] is not overlay:
        audit.error("NavigationPaletteOverlay must be the topmost ForgeWorkbenchShell child")
    if direct_parent(panel, parents) is not overlay:
        audit.error("NavigationPalettePanel must be a direct child of NavigationPaletteOverlay")
    if direct_parent(surface, parents) is not panel:
        audit.error("NavigationPaletteSurface must be a direct child of NavigationPalettePanel")

    rectangles = shell_layout(shell, vm_source, False, viewport)
    shell_rect = rectangles.get(shell)
    overlay_rect = rectangles.get(overlay)
    panel_rect = rectangles.get(panel)
    surface_rect = rectangles.get(surface)
    if shell_rect is None or overlay_rect is None or panel_rect is None or surface_rect is None:
        audit.error(f"Cannot resolve the navigation palette modal frame in {viewport_name}")
        return
    if overlay_rect != shell_rect:
        audit.error(f"NavigationPaletteOverlay must cover the shell in {viewport_name}")
    if panel_rect.width <= 0 or panel_rect.height <= 0 or not contained(overlay_rect, panel_rect):
        audit.error(f"NavigationPalettePanel exceeds the modal overlay in {viewport_name}")
    if panel_rect.width > 980 or panel_rect.height > 780:
        audit.error(f"NavigationPalettePanel exceeds its 980x780 size cap in {viewport_name}")
    if surface_rect.width <= 0 or surface_rect.height <= 0 or not contained(panel_rect, surface_rect):
        audit.error(f"NavigationPaletteSurface exceeds its panel in {viewport_name}")

    section_ids = (
        "NavigationPaletteHeader", "NavigationPaletteContext", "NavigationPaletteSearchFrame",
        "NavigationPaletteScroll", "NavigationPaletteEmptyState", "NavigationPaletteKeyboardHint",
    )
    section_rects: dict[str, Rect] = {}
    for element_id in section_ids:
        node = nodes[element_id]
        rect = rectangles.get(node)
        if rect is None or rect.width <= 0 or rect.height <= 0:
            audit.error(f"Cannot resolve {element_id} in navigation palette/{viewport_name}")
            continue
        if direct_parent(node, parents) is not surface:
            audit.error(f"{element_id} must be a direct child of NavigationPaletteSurface")
        if not contained(surface_rect, rect):
            audit.error(f"{element_id} exceeds NavigationPaletteSurface in {viewport_name}")
        section_rects[element_id] = rect

    # Results and the empty message deliberately share one content viewport;
    # both states stay inside the surface and the keyboard hint remains clear.
    results_rect = section_rects.get("NavigationPaletteScroll")
    empty_rect = section_rects.get("NavigationPaletteEmptyState")
    if results_rect is not None and empty_rect is not None and results_rect != empty_rect:
        audit.error(f"Navigation palette results and empty state must share a viewport in {viewport_name}")
    visible_sections = (
        "NavigationPaletteHeader", "NavigationPaletteContext", "NavigationPaletteSearchFrame",
        "NavigationPaletteScroll", "NavigationPaletteKeyboardHint",
    )
    for index, first_id in enumerate(visible_sections):
        first = section_rects.get(first_id)
        if first is None:
            continue
        for second_id in visible_sections[index + 1:]:
            second = section_rects.get(second_id)
            if second is not None and intersects(first, second):
                audit.error(f"Navigation palette regions overlap in {viewport_name}: {first_id} and {second_id}")

    # Fixed controls and labels must be contained by the modal surface. Result
    # rows are data templates and are checked structurally below instead.
    content_ids = (
        "NavigationPaletteHeading", "NavigationPaletteClose", "NavigationPaletteActiveRoute",
        "NavigationPaletteActiveGroup", "ForgeNavigationPaletteSearch",
        "NavigationPaletteSearchPlaceholder", "NavigationPaletteEmptyMessage",
    )
    for element_id in content_ids:
        node = nodes[element_id]
        rect = rectangles.get(node)
        if rect is None or rect.width <= 0 or rect.height <= 0:
            audit.error(f"Cannot resolve {element_id} in navigation palette/{viewport_name}")
        elif not contained(surface_rect, rect):
            audit.error(f"{element_id} exceeds NavigationPaletteSurface in {viewport_name}")

    route_list = nodes["NavigationPaletteItems"]
    template = route_list.find("ItemTemplate")
    template_children = template if template is not None else ()
    row = next((child for child in template_children if local_name(child.tag) == "ListPanel"), None)
    row_children = row.find("Children") if row is not None else None
    row_items = row_children if row_children is not None else ()
    buttons = [child for child in row_items if local_name(child.tag) == "ButtonWidget"]
    if route_list.attrib.get("DataSource") != "{NavigationPaletteResults}":
        audit.error("NavigationPaletteItems must bind to {NavigationPaletteResults}")
    if (template is None or row is None
            or "HorizontalLeftToRight" not in row.attrib.get("StackLayout.LayoutMethod", "")
            or len(buttons) != 2):
        audit.error("Navigation palette result rows must have one route button and one favorite button")
    else:
        route_button, favorite_button = buttons
        route_contract = {
            "WidthSizePolicy": "StretchToParent",
            "HeightSizePolicy": "Fixed",
            "SuggestedHeight": "68",
            "Command.Click": "ExecuteSelect",
            "IsFocusable": "true",
            "IsVisible": "@IsVisible",
            "IsSelected": "@IsSelected",
        }
        favorite_contract = {
            "WidthSizePolicy": "Fixed",
            "SuggestedWidth": "48",
            "HeightSizePolicy": "Fixed",
            "SuggestedHeight": "68",
            "Command.Click": "ExecuteToggleFavorite",
            "IsFocusable": "true",
            "IsVisible": "@CanFavorite",
        }
        for key, expected in route_contract.items():
            if route_button.attrib.get(key) != expected:
                audit.error(f"Navigation palette route button must use {key}={expected}")
        for key, expected in favorite_contract.items():
            if favorite_button.attrib.get(key) != expected:
                audit.error(f"Navigation palette favorite button must use {key}={expected}")


def validate_geometry(audit: Audit, prefab: ET.Element, vm_source: str) -> None:
    shell = find_by_id(prefab, "ForgeWorkbenchShell")
    if shell is None or local_name(shell.tag) != "Widget":
        audit.error("Cannot locate Widget Id=ForgeWorkbenchShell")
        return
    shell_contract = {
        "WidthSizePolicy": "StretchToParent",
        "HeightSizePolicy": "StretchToParent",
        "SuggestedWidth": str(SHELL_MAX_WIDTH),
        "SuggestedHeight": str(SHELL_MAX_HEIGHT),
        "MaxWidth": str(SHELL_MAX_WIDTH),
        "MaxHeight": str(SHELL_MAX_HEIGHT),
        "HorizontalAlignment": "Center",
        "VerticalAlignment": "Center",
        "MarginLeft": str(SHELL_MARGIN),
        "MarginRight": str(SHELL_MARGIN),
        "MarginTop": str(SHELL_MARGIN),
        "MarginBottom": str(SHELL_MARGIN),
    }
    for key, expected in shell_contract.items():
        if shell.attrib.get(key) != expected:
            audit.error(f"ForgeWorkbenchShell must use {key}={expected} for adaptive capped layout")
    shell_children = shell.find("Children")
    if shell_children is None:
        audit.error("Workbench shell has no direct child regions")
        return
    top_nodes = {node.attrib.get("Id"): node for node in shell_children if node.attrib.get("Id")}
    stretch_width_ids = {
        "ForgeHeader", "ForgeTopBrassFrameRule", "ForgeInputRow", "ForgePrimaryCommandHost",
        "ForgeEvidenceFrame", "ForgeEvidenceActionBrassRule", "ForgeSecondaryActionDeck",
        "ForgeExtensionActionDeck", "ForgeAssemblyActionDeck", "ForgePaginationAndUtilityDeck",
        "ForgeNavigationFooter", "ForgeBottomBrassFrameRule",
    }
    for element_id in sorted(stretch_width_ids):
        node = top_nodes.get(element_id)
        if node is None:
            audit.error(f"Missing adaptive width region {element_id}")
        elif node.attrib.get("WidthSizePolicy") != "StretchToParent":
            audit.error(f"{element_id} must stretch to the available workbench width")
    bottom_anchors = {
        "ForgeEvidenceActionBrassRule": "158",
        "ForgeSecondaryActionDeck": "99",
        "ForgeExtensionActionDeck": "99",
        "ForgeAssemblyActionDeck": "99",
        "ForgePaginationAndUtilityDeck": "47",
        "ForgeNavigationFooter": "14",
        "ForgeBottomBrassFrameRule": "4",
    }
    for element_id, margin_bottom in bottom_anchors.items():
        node = top_nodes.get(element_id)
        if node is None or node.attrib.get("VerticalAlignment") != "Bottom" or node.attrib.get("MarginBottom") != margin_bottom:
            audit.error(f"{element_id} must be bottom anchored with MarginBottom={margin_bottom}")
    footer = top_nodes.get("ForgeNavigationFooter")
    toast = top_nodes.get("ForgeStatusToast")
    toast_footer_exclusive = False
    if footer is None or toast is None:
        audit.error("The navigation footer and status toast must share the reserved bottom status slot")
    else:
        toast_contract = {
            "WidthSizePolicy": "Fixed",
            "SuggestedWidth": "600",
            "SuggestedHeight": "34",
            "HorizontalAlignment": "Center",
            "VerticalAlignment": "Bottom",
            "MarginLeft": "280",
            "MarginRight": "24",
            "MarginBottom": "12",
            "IsVisible": "@IsToastVisible",
        }
        for key, expected in toast_contract.items():
            if toast.attrib.get(key) != expected:
                audit.error(f"ForgeStatusToast must use {key}={expected}")
        footer_visibility_ok = footer.attrib.get("IsVisible") == "@IsNavigationFooterVisible"
        vm_visibility_ok = re.search(
            r"\bIsNavigationFooterVisible\s*=>\s*!\s*_isToastVisible\b", vm_source
        ) is not None
        if not footer_visibility_ok:
            audit.error("ForgeNavigationFooter must hide while ForgeStatusToast is visible")
        if not vm_visibility_ok:
            audit.error("PanelViewModel must make IsNavigationFooterVisible the inverse of toast visibility")
        toast_footer_exclusive = (
            footer_visibility_ok and vm_visibility_ok
            and toast.attrib.get("IsVisible") == "@IsToastVisible"
        )
    evidence = top_nodes.get("ForgeEvidenceFrame")
    if evidence is not None and (evidence.attrib.get("HeightSizePolicy") != "StretchToParent"
                                 or evidence.attrib.get("MarginTop") != "@EvidenceTop"
                                 or evidence.attrib.get("MarginBottom") != "178"):
        audit.error("ForgeEvidenceFrame must stretch from @EvidenceTop to the anchored bottom action slot")
    rail = top_nodes.get("ForgeNavigationRail")
    if rail is not None and (rail.attrib.get("HeightSizePolicy") != "StretchToParent"
                             or rail.attrib.get("MarginBottom") != "24"):
        audit.error("ForgeNavigationRail must stretch to the bottom safe inset")

    narrow_layout = shell_layout(shell, vm_source, False, VIEWPORT_PROFILES[0])
    wide_layout = shell_layout(shell, vm_source, False, VIEWPORT_PROFILES[-1])
    parents = descendant_map(prefab)
    playbook_flow = find_by_id(prefab, "ForgePlaybookFlow")

    def is_measured_playbook_macro(node: ET.Element) -> bool:
        wrapper = parents.get(node)
        return (
            node.attrib.get("Id") == "ForgeRunMacro"
            and playbook_flow is not None
            and wrapper is not None
            and parents.get(wrapper) is playbook_flow
            and node.attrib.get("WidthSizePolicy") == "StretchToParent"
            and node.attrib.get("HeightSizePolicy") == "Fixed"
            and node.attrib.get("SuggestedHeight") == "36"
            and node.attrib.get("MarginBottom") == "12"
        )

    for element_id in sorted(stretch_width_ids):
        node = top_nodes.get(element_id)
        if node in narrow_layout and node in wide_layout and wide_layout[node].width <= narrow_layout[node].width:
            audit.error(f"{element_id} does not grow between narrow and wide viewport profiles")
    evidence_top = conditional_numeric_values(vm_source, "EvidenceTop")
    if evidence_top is None:
        audit.error("Cannot resolve evidence-focus and normal placement from PanelViewModel")
    if re.search(
        r"\bIsPlaybookVisible\s*=>\s*_isDetailedMode\s*&&\s*!evidenceFocused\s*&&\s*!_isKeyHelpOpen",
        vm_source,
    ) is None:
        audit.error("PanelViewModel must hide the detailed Playbook while the right-column Key Help panel is open")
    playbook_panel = top_nodes.get("ForgePlaybookPanel")
    if playbook_panel is None or playbook_panel.attrib.get("HeightSizePolicy") != "StretchToParent" \
            or playbook_panel.attrib.get("MarginTop") != "281" \
            or playbook_panel.attrib.get("MarginBottom") != "166" \
            or playbook_panel.attrib.get("MaxHeight") != "390":
        audit.error("ForgePlaybookPanel must stretch within the space below inputs and above the bottom action slot")

    # Each viewport derives the shell from the centered inset and maximum size.
    # Normal/focused evidence and all action-deck profiles are checked at each size.
    layout_profiles = {
        "regular": (False, False, ["ForgeHeader", "ForgeTopBrassFrameRule", "ForgeNavigationRail", "ForgeCurrentSection", "ForgeSectionHelp", "ForgeEvidenceToggle", "ForgeBriefingDeck", "ForgeInputRow", "ForgePrimaryCommandHost", "ForgeEvidenceFrame", "ForgeEvidenceActionBrassRule", "ForgeSecondaryActionDeck", "ForgePaginationAndUtilityDeck", "ForgeNavigationFooter", "ForgeStatusToast", "ForgeBottomBrassFrameRule"]),
        "extensions": (False, False, ["ForgeHeader", "ForgeTopBrassFrameRule", "ForgeNavigationRail", "ForgeCurrentSection", "ForgeSectionHelp", "ForgeEvidenceToggle", "ForgeBriefingDeck", "ForgeInputRow", "ForgeEvidenceFrame", "ForgeEvidenceActionBrassRule", "ForgeExtensionActionDeck", "ForgePaginationAndUtilityDeck", "ForgeNavigationFooter", "ForgeStatusToast", "ForgeBottomBrassFrameRule"]),
        "assembly workbench": (False, False, ["ForgeHeader", "ForgeTopBrassFrameRule", "ForgeNavigationRail", "ForgeCurrentSection", "ForgeSectionHelp", "ForgeEvidenceToggle", "ForgeBriefingDeck", "ForgeInputRow", "ForgeEvidenceFrame", "ForgeEvidenceActionBrassRule", "ForgeAssemblyActionDeck", "ForgePaginationAndUtilityDeck", "ForgeNavigationFooter", "ForgeStatusToast", "ForgeBottomBrassFrameRule"]),
        "focused evidence": (True, False, ["ForgeHeader", "ForgeTopBrassFrameRule", "ForgeNavigationRail", "ForgeCurrentSection", "ForgeSectionHelp", "ForgeEvidenceToggle", "ForgeEvidenceFrame", "ForgeEvidenceActionBrassRule", "ForgeSecondaryActionDeck", "ForgePaginationAndUtilityDeck", "ForgeNavigationFooter", "ForgeStatusToast", "ForgeBottomBrassFrameRule"]),
        "detailed": (False, True, ["ForgeHeader", "ForgeTopBrassFrameRule", "ForgeNavigationRail", "ForgeCurrentSection", "ForgeSectionHelp", "ForgeEvidenceToggle", "ForgeBriefingDeck", "ForgeInputRow", "ForgePrimaryCommandHost", "ForgePlaybookPanel", "ForgeEvidenceFrame", "ForgeEvidenceActionBrassRule", "ForgeSecondaryActionDeck", "ForgePaginationAndUtilityDeck", "ForgeNavigationFooter", "ForgeStatusToast", "ForgeBottomBrassFrameRule"]),
    }
    for viewport in VIEWPORT_PROFILES:
        shell_rect = effective_shell_rect(viewport)
        if shell_rect.width <= 0 or shell_rect.height <= 0:
            audit.error(f"Viewport {viewport[0]}x{viewport[1]} leaves no room for the workbench shell")
            continue
        validate_navigation_palette_geometry(audit, prefab, shell, vm_source, viewport)
        if viewport == VIEWPORT_PROFILES[-1] and (shell_rect.width != SHELL_MAX_WIDTH or shell_rect.height != SHELL_MAX_HEIGHT):
            audit.error("The 1920x1080 profile must exercise both centered shell maximums")

        overlay_rectangles = shell_layout(shell, vm_source, False, viewport)
        key_help = find_by_id(prefab, "ForgeKeyHelpPanel")
        sdk_catalog = find_by_id(prefab, "ForgeSdkCatalogPanel")
        key_help_rect = overlay_rectangles.get(key_help) if key_help is not None else None
        sdk_catalog_rect = overlay_rectangles.get(sdk_catalog) if sdk_catalog is not None else None
        if key_help_rect is None or sdk_catalog_rect is None:
            audit.error(f"Cannot resolve SDK and Key Help overlays in {viewport[0]}x{viewport[1]}")
        elif intersects(key_help_rect, sdk_catalog_rect):
            audit.error(f"SDK Catalog and Key Help overlap in {viewport[0]}x{viewport[1]}")

        for profile_name, (state, detailed_mode, region_ids) in layout_profiles.items():
            rectangles = shell_layout(shell, vm_source, state, viewport, detailed_mode)
            shell_bounds = rectangles[shell]
            regions: list[tuple[str, Rect]] = []
            for element_id in region_ids:
                node = top_nodes.get(element_id)
                if node is None:
                    audit.error(f"Geometry profile {profile_name}/{viewport[0]}x{viewport[1]} references missing region {element_id}")
                    continue
                rect = rectangles.get(node)
                if rect is None:
                    audit.error(f"Cannot resolve {element_id} in {profile_name}/{viewport[0]}x{viewport[1]}")
                    continue
                if rect.width <= 0 or rect.height <= 0:
                    audit.error(f"{element_id} has empty geometry in {profile_name}/{viewport[0]}x{viewport[1]}")
                    continue
                if not contained(shell_bounds, rect):
                    audit.error(f"{element_id} exceeds the shell in {profile_name}/{viewport[0]}x{viewport[1]}")
                regions.append((element_id, rect))
            for index, (first_id, first) in enumerate(regions):
                for second_id, second in regions[index + 1:]:
                    if {first_id, second_id} == {"ForgeNavigationFooter", "ForgeStatusToast"} and toast_footer_exclusive:
                        continue
                    if intersects(first, second):
                        audit.error(f"Top-level regions overlap in {profile_name}/{viewport[0]}x{viewport[1]}: {first_id} and {second_id}")

            if detailed_mode:
                playbook = top_nodes.get("ForgePlaybookPanel")
                playbook_rect = rectangles.get(playbook) if playbook is not None else None
                if playbook_rect is None:
                    audit.error(f"Cannot resolve visible ForgePlaybookPanel in detailed/{viewport[0]}x{viewport[1]}")
                else:
                    for control in prefab.iter():
                        if local_name(control.tag) not in {"ButtonWidget", "EditableTextWidget"}:
                            continue
                        if item_template_for(control, parents) is not None:
                            continue
                        ancestor = control
                        inside_playbook = False
                        while ancestor is not None:
                            if ancestor is playbook:
                                inside_playbook = True
                                break
                            ancestor = parents.get(ancestor)
                        if inside_playbook or not visible_in_evidence_state(
                            control, parents, state, detailed_mode=detailed_mode
                        ):
                            continue
                        control_rect = rectangles.get(control)
                        if control_rect is not None and intersects(playbook_rect, control_rect):
                            audit.error(
                                f"ForgePlaybookPanel overlaps actionable control "
                                f"{control.attrib.get('Id', local_name(control.tag))} "
                                f"in detailed/{viewport[0]}x{viewport[1]}"
                            )

            for node in prefab.iter():
                if local_name(node.tag) not in {"ButtonWidget", "EditableTextWidget"}:
                    continue
                if item_template_for(node, parents) is not None:
                    continue
                rect = rectangles.get(node)
                if rect is None:
                    # This is the final child of the fixed-height action row in
                    # the already-audited clipped CoverChildren Playbook flow.
                    # Its vertical position is content-relative and reachable
                    # through the sibling scrollbar, not an absolute shell rect.
                    if is_measured_playbook_macro(node):
                        continue
                    audit.error(f"Cannot resolve actionable geometry for {node.attrib.get('Id', local_name(node.tag))} in {viewport[0]}x{viewport[1]}")
                elif rect.width <= 0 or rect.height <= 0 or not contained(shell_bounds, rect):
                    audit.error(f"Actionable control {node.attrib.get('Id', local_name(node.tag))} is clipped in {viewport[0]}x{viewport[1]}")

            # All eight route buttons remain inside the visible rail at each viewport.
            rail = top_nodes.get("ForgeNavigationRail")
            nav = find_by_id(prefab, "ForgeAreaNavigation")
            rail_rect = rectangles.get(rail) if rail is not None else None
            nav_rect = rectangles.get(nav) if nav is not None else None
            if rail_rect is None or nav_rect is None or not contained(rail_rect, nav_rect):
                audit.error(f"Primary navigation list is clipped in {viewport[0]}x{viewport[1]}")
            elif nav is not None:
                for route in nav.iter():
                    if local_name(route.tag) != "ButtonWidget" or not route.attrib.get("Command.Click", "").startswith("Execute"):
                        continue
                    route_rect = rectangles.get(route)
                    if route_rect is None or not contained(rail_rect, route_rect):
                        audit.error(f"Route {route.attrib.get('Id', '<none>')} is clipped in {viewport[0]}x{viewport[1]}")

            # Dynamic horizontal rows must contain every button after stack flow.
            for row in prefab.iter():
                if local_name(row.tag) != "ListPanel" or "HorizontalLeftToRight" not in row.attrib.get("StackLayout.LayoutMethod", ""):
                    continue
                if item_template_for(row, parents) is not None:
                    continue
                row_rect = rectangles.get(row)
                if row_rect is None:
                    audit.error(f"Cannot resolve horizontal row {row.attrib.get('Id', '<none>')} in {viewport[0]}x{viewport[1]}")
                    continue
                children = row.find("Children")
                if children is None:
                    continue
                for child in children:
                    if local_name(child.tag) != "ButtonWidget":
                        continue
                    child_rect = rectangles.get(child)
                    if child_rect is None or not contained(row_rect, child_rect):
                        audit.error(f"Button {child.attrib.get('Id', '<none>')} exceeds horizontal row {row.attrib.get('Id', '<none>')} in {viewport[0]}x{viewport[1]}")

        # Header/rail descendants and passive decorations use stretch/cap anchors;
        # verify child bounds too so no artwork or label is cropped by a region.
        rectangles = shell_layout(shell, vm_source, False, viewport)
        for parent_id in ("ForgeHeader", "ForgeNavigationRail"):
            parent = find_by_id(prefab, parent_id)
            parent_rect = rectangles.get(parent) if parent is not None else None
            if parent is None or parent_rect is None:
                audit.error(f"Cannot resolve visual region {parent_id} in {viewport[0]}x{viewport[1]}")
                continue
            for child in parent.findall("Children"):
                for actual_child in child:
                    child_rect = rectangles.get(actual_child)
                    if child_rect is not None and not contained(parent_rect, child_rect):
                        audit.error(f"Child {actual_child.attrib.get('Id', local_name(actual_child.tag))} exceeds {parent_id} in {viewport[0]}x{viewport[1]}")
            if parent_id == "ForgeNavigationRail":
                rail_art = find_by_id(prefab, "ForgeRailCloth")
                rail_art_rect = rectangles.get(rail_art) if rail_art is not None else None
                if rail_art_rect is None:
                    audit.error(f"Cannot resolve centered ForgeRailCloth in {viewport[0]}x{viewport[1]}")
                else:
                    expected_width = 128
                    expected_height = min(256, max(0, parent_rect.height - 425 - 5))
                    expected_left = parent_rect.left + (parent_rect.width - expected_width) / 2
                    expected_top = parent_rect.top + 425
                    if expected_height <= 0:
                        audit.error(f"ForgeNavigationRail has no height for the heraldic underlay in {viewport[0]}x{viewport[1]}")
                    elif any((
                        abs(rail_art_rect.left - expected_left) > 0.01,
                        abs(rail_art_rect.top - expected_top) > 0.01,
                        abs(rail_art_rect.width - expected_width) > 0.01,
                        abs(rail_art_rect.height - expected_height) > 0.01,
                    )):
                        audit.error(
                            f"ForgeRailCloth must be centered at native width and capped below rail guidance "
                            f"in {viewport[0]}x{viewport[1]} (expected x={expected_left:g}, y={expected_top:g}, "
                            f"w={expected_width}, h={expected_height}; found "
                            f"x={rail_art_rect.left:g}, y={rail_art_rect.top:g}, "
                            f"w={rail_art_rect.width:g}, h={rail_art_rect.height:g})"
                        )

    validate_decorative_layers(audit, prefab, shell, vm_source)


def audit_prefab(
    prefab_path: Path,
    viewmodel_path: Path = DEFAULT_VIEWMODEL,
    brushes_path: Path = DEFAULT_BRUSHES,
    sprites_path: Path = DEFAULT_SPRITES,
    tool_item_viewmodel_path: Path = DEFAULT_TOOL_ITEM_VIEWMODEL,
) -> Audit:
    audit = Audit()
    inputs = [prefab_path, viewmodel_path, brushes_path, sprites_path, tool_item_viewmodel_path]
    missing = [str(path) for path in inputs if not path.is_file()]
    if missing:
        for path in missing:
            audit.error(f"Required audit input does not exist: {path}")
        return audit

    try:
        prefab_root = ET.parse(prefab_path).getroot()
        brushes_root = ET.parse(brushes_path).getroot()
        sprite_root = ET.parse(sprites_path).getroot()
        vm_source = viewmodel_path.read_text(encoding="utf-8-sig")
        tool_item_source = tool_item_viewmodel_path.read_text(encoding="utf-8-sig")
    except (ET.ParseError, OSError, UnicodeError) as exc:
        audit.error(f"Unable to read audit input: {exc}")
        return audit

    if local_name(prefab_root.tag) != "Prefab":
        audit.error(f"Expected Prefab root element, got {prefab_root.tag}")
    validate_contracts(audit, prefab_root, vm_source, tool_item_source, brushes_root, sprite_root)
    validate_geometry(audit, prefab_root, vm_source)
    return audit


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--prefab", type=Path, default=DEFAULT_PREFAB)
    parser.add_argument("--viewmodel", type=Path, default=DEFAULT_VIEWMODEL)
    parser.add_argument("--toolitemvm", type=Path, default=DEFAULT_TOOL_ITEM_VIEWMODEL)
    parser.add_argument("--brushes", type=Path, default=DEFAULT_BRUSHES)
    parser.add_argument("--sprites", type=Path, default=DEFAULT_SPRITES)
    args = parser.parse_args(argv)

    print("=== CALRADIA FORGE GAUNTLET STRUCTURAL AUDIT ===")
    print(f"Prefab:    {args.prefab}")
    print(f"ViewModel: {args.viewmodel}")
    print(f"Tool item ViewModel: {args.toolitemvm}")
    print(f"Brushes:   {args.brushes}")
    print(f"Sprites:   {args.sprites}")
    audit = audit_prefab(args.prefab, args.viewmodel, args.brushes, args.sprites, args.toolitemvm)
    for message in audit.warnings:
        print(f"WARN: {message}")
    for message in audit.errors:
        print(f"ERROR: {message}")
    print(f"Result: {'FAIL' if audit.errors else 'PASS'} ({len(audit.errors)} errors, {len(audit.warnings)} warnings)")
    if not audit.errors:
        print("Note: structural validation only; live Gauntlet rendering is not verified by this script.")
    return 1 if audit.errors else 0


if __name__ == "__main__":
    sys.exit(main())
