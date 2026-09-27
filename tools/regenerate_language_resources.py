"""Regenerate Bannerlord language resources from retained translations and current native text keys."""
from __future__ import annotations

import hashlib
import json
import re
from pathlib import Path
from xml.etree import ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]
LANGUAGE_ROOT = ROOT / "modules" / "CalradiaForge" / "ModuleData" / "Languages"
LANGUAGES = {
    "EN": ("en", "English"), "SP": ("es", "Español (LA)"), "BR": ("pt", "Português (BR)"),
    "DE": ("de", "Deutsch"), "FR": ("fr", "Français"), "IT": ("it", "Italiano"),
    "PL": ("pl", "Polski"), "RU": ("ru", "Русский"), "TR": ("tr", "Türkçe"),
    "CNs": ("zh-HANS", "简体中文"), "CNt": ("zh-HANT", "繁體中文"), "JP": ("ja", "日本語"), "KO": ("ko", "한국어"),
}
VERSION = __import__("re").search(r"<CalradiaForgeVersion>([^<]+)</CalradiaForgeVersion>", (ROOT / "Directory.Build.props").read_text(encoding="utf-8")).group(1)


def stable_id(text: str) -> str:
    return "forge_" + hashlib.sha256(text.encode("utf-8")).hexdigest()[:12]


def parse_strings(path: Path) -> dict[str, str]:
    tree = ET.parse(path)
    return {entry.attrib["id"]: entry.attrib.get("text", "") for entry in tree.findall(".//string")}


def write_xml(path: Path, root: ET.Element) -> None:
    xml = ET.tostring(root, encoding="utf-8")
    path.write_bytes(b"\xef\xbb\xbf<?xml version='1.0' encoding='utf-8'?>\n" + xml)


def main() -> None:
    source = (ROOT / "src" / "CalradiaForge.Mod" / "PanelViewModel.cs").read_text(encoding="utf-8")
    literal_keys = set(re.findall(r'\bT\("([^"]+)"\)', source))
    original = {folder: parse_strings(LANGUAGE_ROOT / folder / "forge_strings.xml") for folder in LANGUAGES}
    navigation_palette = json.loads((ROOT / "localization" / "navigation-palette.json").read_text(encoding="utf-8"))
    required_locales = {iso for iso, _ in LANGUAGES.values()}
    for text, localized in navigation_palette.items():
        if localized.get("en") != text or set(localized) != required_locales or any(not value.strip() for value in localized.values()):
            raise ValueError(f"Incomplete navigation palette translation: {text}")
    english_by_old_id = original["EN"]
    english_texts = list(dict.fromkeys(list(english_by_old_id.values()) + sorted(literal_keys) + sorted(navigation_palette)))
    old_ids_by_text: dict[str, list[str]] = {}
    for old_id, text in english_by_old_id.items():
        old_ids_by_text.setdefault(text, []).append(old_id)

    for folder, (iso, display) in LANGUAGES.items():
        translated = original[folder]
        root = ET.Element("base", {"type": "string"})
        tags = ET.SubElement(root, "tags")
        ET.SubElement(tags, "tag", {"language": display})
        strings = ET.SubElement(root, "strings")
        for text in english_texts:
            palette_translation = navigation_palette.get(text)
            if palette_translation is not None:
                localized = palette_translation[iso]
            else:
                previous_ids = old_ids_by_text.get(text, [])
                localized = next((translated[old] for old in previous_ids if old in translated), text)
            ET.SubElement(strings, "string", {"id": stable_id(text), "text": localized})
        write_xml(LANGUAGE_ROOT / folder / "forge_strings.xml", root)
        metadata = ET.Element("LanguageData", {"id": display, "name": display, "supported_iso": iso, "under_development": "false"})
        ET.SubElement(metadata, "LanguageFile", {"xml_path": folder + "/forge_strings.xml"})
        write_xml(LANGUAGE_ROOT / folder / "language_data.xml", metadata)

    evidence = {
        "languages": len(LANGUAGES),
        "englishKeys": len(english_texts),
        "panelLiteralKeys": len(literal_keys),
        "utf8Bom": True,
        "stableIds": True,
    }
    (ROOT / "artifacts" / f"language-regeneration-{VERSION.replace('.', '')}.json").write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(evidence, indent=2))


if __name__ == "__main__":
    main()
