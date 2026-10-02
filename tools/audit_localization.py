"""Validate all generated Calradia Forge native and desktop language catalogs."""
from __future__ import annotations

import hashlib
import json
import re
from pathlib import Path
from xml.etree import ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]
NATIVE = ROOT / "modules" / "CalradiaForge" / "ModuleData" / "Languages"
CORE = ROOT / "localization"
DESKTOP_RESOURCES = ROOT / "src" / "CalradiaForge.Desktop" / "Resources"
LANGUAGES = {"EN": "en", "SP": "es", "BR": "pt", "DE": "de", "FR": "fr", "IT": "it", "PL": "pl", "RU": "ru", "TR": "tr", "CNs": "zh-HANS", "CNt": "zh-HANT", "JP": "ja", "KO": "ko"}
REQUIRED_NAVIGATION_PALETTE_TEXTS = {
    "Quick navigation",
    "Active view",
    "Type to search all routes and SDK tools.",
    "No matching routes.",
    "Search views and SDK tools",
    "↑/↓ Move · Enter Open · Esc Close · Ctrl+P Toggle",
    "Quick actions",
    "Focus search",
    "Focus the current section search or argument field.",
    "Favorites",
    "Recent",
    "Toggle Live Watch",
    "Remove favorite",
    "Add favorite",
    "Use this view's controls to run actions.",
}
REQUIRED_TRANSLATED_NATIVE_IDS = {
    "forge_8f1ea1ac7b38",  # Gauntlet Page Blueprint
    "forge_7bc45a420de0",  # Gauntlet Page Blueprint description
    "forge_02660ffe391c",  # Page title field
}
VERSION = __import__("re").search(r"<CalradiaForgeVersion>([^<]+)</CalradiaForgeVersion>", (ROOT / "Directory.Build.props").read_text(encoding="utf-8")).group(1)


def load_native(folder: str) -> dict[str, str]:
    path = NATIVE / folder / "forge_strings.xml"
    if path.read_bytes()[:3] != b"\xef\xbb\xbf":
        raise ValueError(f"{path} is not UTF-8 with BOM")
    return {item.attrib["id"]: item.attrib.get("text", "") for item in ET.parse(path).findall(".//string")}


def main() -> None:
    english = load_native("EN")
    if not english:
        raise ValueError("English native catalog is empty")
    navigation_palette = json.loads((CORE / "navigation-palette.json").read_text(encoding="utf-8"))
    if set(navigation_palette) != REQUIRED_NAVIGATION_PALETTE_TEXTS:
        raise ValueError("Navigation palette translation source does not contain the required English strings")
    required_locale_order = list(LANGUAGES.values())
    required_isos = set(required_locale_order)
    for source, translations in navigation_palette.items():
        if translations.get("en") != source or set(translations) != required_isos:
            raise ValueError(f"Navigation palette translation locales differ from supported languages: {source!r}")
        if any(not value.strip() for value in translations.values()):
            raise ValueError(f"Navigation palette contains an empty translation: {source!r}")
    composer_source = json.loads((CORE / "gauntlet-composer.json").read_text(encoding="utf-8"))
    composer_languages = composer_source.get("languages", [])
    composer_entries = composer_source.get("entries", [])
    if composer_languages != required_locale_order:
        raise ValueError("Gauntlet Composer translation locales differ from supported languages")
    gauntlet_composer = {}
    for entry in composer_entries:
        source = entry.get("key", "")
        values = entry.get("values", [])
        if not source.strip() or source in gauntlet_composer or len(values) != len(composer_languages):
            raise ValueError(f"Invalid or duplicate Gauntlet Composer translation entry: {source!r}")
        translations = dict(zip(composer_languages, values))
        if translations.get("en") != source or set(translations) != required_isos:
            raise ValueError(f"Gauntlet Composer locale or English key mismatch: {source!r}")
        if any(not value.strip() for value in translations.values()):
            raise ValueError(f"Gauntlet Composer contains an empty translation: {source!r}")
        if source in navigation_palette:
            raise ValueError(f"Gauntlet Composer key duplicates a navigation palette key: {source!r}")
        gauntlet_composer[source] = translations
    if not gauntlet_composer:
        raise ValueError("Gauntlet Composer translation source is empty")
    panel_source = (ROOT / "src" / "CalradiaForge.Mod" / "PanelViewModel.cs").read_text(encoding="utf-8")
    rule_ui_source = (ROOT / "src" / "CalradiaForge.Mod" / "PanelViewModel.CampaignRules.cs").read_text(encoding="utf-8")
    composer_required_texts = set()
    for line in panel_source.splitlines():
        if "GauntletComposer" in line or "OutputHeading" in line:
            composer_required_texts.update(re.findall(r'T\("([^"\n]+)"\)', line))
        if "_gauntletComposerStatusKey =" in line or "SetGauntletComposerStatus(" in line:
            composer_required_texts.update(re.findall(r'"([^"\n]+)"', line))
    composer_required_texts.discard("Search / argument")
    missing_composer_texts = composer_required_texts.difference(gauntlet_composer)
    if missing_composer_texts:
        raise ValueError("Gauntlet Composer UI text lacks source translations: " + ", ".join(sorted(missing_composer_texts)))
    rule_source = json.loads((CORE / "campaign-rule-builder.json").read_text(encoding="utf-8"))
    if rule_source.get("languages") != required_locale_order:
        raise ValueError("Campaign Rule Builder translation locales differ from supported languages")
    campaign_rules = {}
    for entry in rule_source.get("entries", []):
        source = entry.get("key", "")
        values = entry.get("values", [])
        if not source.strip() or source in campaign_rules or len(values) != len(required_locale_order):
            raise ValueError(f"Invalid or duplicate Campaign Rule Builder translation entry: {source!r}")
        translations = dict(zip(required_locale_order, values))
        if translations["en"] != source or any(not value.strip() for value in translations.values()):
            raise ValueError(f"Incomplete Campaign Rule Builder translation: {source!r}")
        if source in navigation_palette or source in gauntlet_composer:
            raise ValueError(f"Campaign Rule Builder duplicates a prior translation key: {source!r}")
        campaign_rules[source] = translations
    if not campaign_rules:
        raise ValueError("Campaign Rule Builder translation source is empty")
    compact_campaign_ui_keys = (
        "Unreadable draft; preserved until save.",
        "Draft over 64 KiB; preserved until save.",
        "Damaged draft; preserved until save.",
        "Unsupported draft version; preserved until save.",
        "Changing the event clears both condition groups.",
    )
    for source in compact_campaign_ui_keys:
        translations = campaign_rules.get(source)
        if translations is None:
            raise ValueError(f"Campaign Rule Builder is missing concise UI text: {source!r}")
        long_values = {language: len(value) for language, value in translations.items() if len(value) > 48}
        if long_values:
            raise ValueError(f"Campaign Rule Builder UI text exceeds the 48-character layout budget for {source!r}: {long_values}")
    rule_required_texts = set()
    for line in (panel_source + "\n" + rule_ui_source).splitlines():
        if "CampaignRuleBuilder" in line or "NoviceCampaignRuleBuilder" in line:
            rule_required_texts.update(re.findall(r'T\("([^"\n]+)"\)', line))
        if "_campaignRuleBuilderStatusKey =" in line or "SetCampaignRuleBuilderStatus(" in line:
            rule_required_texts.update(re.findall(r'"([^"\n]+)"', line))
    rule_required_texts.update(re.findall(r'T\("([^"\n]+)"\)', rule_ui_source))
    rule_core_source = (ROOT / "src" / "CalradiaForge.Mod" / "CampaignRuleBuilder.cs").read_text(encoding="utf-8")
    rule_required_texts.update(re.findall(r'\berror\s*=\s*"([^"\n]+)"', rule_core_source))
    rule_required_texts.update(re.findall(r'\berrors\.Add\("([^"\n]+)"\)', rule_core_source))
    missing_rule_texts = rule_required_texts.difference(campaign_rules).difference(gauntlet_composer).difference(navigation_palette)
    if missing_rule_texts:
        raise ValueError("Campaign Rule Builder UI text lacks source translations: " + ", ".join(sorted(missing_rule_texts)))
    native_results = {}
    for folder, iso in LANGUAGES.items():
        entries = load_native(folder)
        if set(entries) != set(english):
            raise ValueError(f"{folder} does not have English key parity")
        if any(not value.strip() for value in entries.values()):
            raise ValueError(f"{folder} contains an empty translation entry")
        if iso != "en":
            untranslated = [identifier for identifier in REQUIRED_TRANSLATED_NATIVE_IDS if entries[identifier] == english[identifier]]
            if untranslated:
                raise ValueError(f"{folder} leaves required UI strings in English: {', '.join(sorted(untranslated))}")
        for identifier, source in english.items():
            expected = "forge_" + hashlib.sha256(source.encode("utf-8")).hexdigest()[:12]
            if identifier != expected:
                raise ValueError(f"{folder} has a non-deterministic ID {identifier} for English source {source!r}")
        for source, translations in navigation_palette.items():
            identifier = "forge_" + hashlib.sha256(source.encode("utf-8")).hexdigest()[:12]
            if entries.get(identifier) != translations[iso]:
                raise ValueError(f"{folder} is missing the navigation palette translation for {source!r}")
        for source, translations in gauntlet_composer.items():
            identifier = "forge_" + hashlib.sha256(source.encode("utf-8")).hexdigest()[:12]
            if entries.get(identifier) != translations[iso]:
                raise ValueError(f"{folder} is missing the Gauntlet Composer translation for {source!r}")
        for source, translations in campaign_rules.items():
            identifier = "forge_" + hashlib.sha256(source.encode("utf-8")).hexdigest()[:12]
            if entries.get(identifier) != translations[iso]:
                raise ValueError(f"{folder} is missing the Campaign Rule Builder translation for {source!r}")
        metadata = NATIVE / folder / "language_data.xml"
        if metadata.read_bytes()[:3] != b"\xef\xbb\xbf":
            raise ValueError(f"{metadata} is not UTF-8 with BOM")
        language_file = ET.parse(metadata).find("LanguageFile")
        if language_file is None or language_file.attrib.get("xml_path") != folder + "/forge_strings.xml":
            raise ValueError(f"{metadata} does not reference its native strings file")
        native_results[iso] = len(entries)

    core_files = {path.stem for path in CORE.glob("*.xml")}
    required_core = set(LANGUAGES.values())
    if core_files != required_core:
        raise ValueError(f"Desktop catalog set differs from native language set: {sorted(core_files ^ required_core)}")
    english_keys = {item.attrib["key"] for item in ET.parse(CORE / "en.xml").findall("string")}
    for iso in required_core:
        keys = {item.attrib["key"] for item in ET.parse(CORE / f"{iso}.xml").findall("string")}
        if keys != english_keys:
            raise ValueError(f"Desktop catalog {iso} does not have English key parity")

    desktop_resource_keys = None
    for iso in required_core:
        resource = DESKTOP_RESOURCES / f"Strings.{iso}.xaml"
        if not resource.exists():
            raise ValueError(f"Desktop resource dictionary is missing: {resource}")
        document = ET.parse(resource)
        keys = {node.attrib.get("{http://schemas.microsoft.com/winfx/2006/xaml}Key") for node in document.iter() if node.attrib.get("{http://schemas.microsoft.com/winfx/2006/xaml}Key")}
        if desktop_resource_keys is None:
            desktop_resource_keys = keys
        elif keys != desktop_resource_keys:
            raise ValueError(f"Desktop resource dictionary {iso} does not have English key parity")
        if not keys:
            raise ValueError(f"Desktop resource dictionary {iso} is empty")

    evidence = {"nativeLanguages": native_results, "navigationPaletteKeys": len(navigation_palette), "gauntletComposerKeys": len(gauntlet_composer), "campaignRuleBuilderKeys": len(campaign_rules), "desktopLanguages": len(required_core), "desktopKeys": len(english_keys), "desktopResourceKeys": len(desktop_resource_keys), "valid": True}
    output = ROOT / "artifacts" / f"localization-audit-{VERSION.replace('.', '')}.json"
    output.write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(evidence, indent=2))


if __name__ == "__main__":
    main()
