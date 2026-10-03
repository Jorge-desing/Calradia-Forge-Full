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
SOURCE_LOCALIZED_PANEL_KEYS = (
    "Evidence",
    "Generate a CampaignBehaviorBase scaffold with events, SyncData, and stable-ID buckets for optional work; the full collection is still scanned.",
    "Scaffold a CampaignBehaviorBase with stable-ID batch scheduling",
    "Advanced Developer SDK: Surface contracts for 110+ SDK tools, bounded NPC memory inspired by CoALA concepts, and decoupled engine API bridges.",
    "1. Never mutate engine entities during render ticks.\n2. Never store raw Hero, Settlement, or MobileParty pointers in persistent fields; resolve by StringId via MBObjectManager.\n3. Inspection queries must be read-only; measure the complete query path before making performance claims.\n4. ForgeAgentMemory queries bounded NPC memory data; it is not a CoALA language-agent runtime.",
    "2. Query ForgeAgentMemory: Execute cf.agent_memory_query to inspect semantic and episodic memory data.",
    "Query ForgeAgentMemory fact counts and decay status",
    "ForgeWeave: Event recording and sequence replay. Patch diagnostics inspect Forge-owned hooks and patches; a compatible loaded runtime is observed read-only when available. Patch Preflight checks blueprint declarations without applying patches.",
    "2. For optional periodic hero work, use ForgeTimeSlicer.ShouldProcess(hero.StringId, currentHour) for stable buckets. Defer only work that can wait up to 24 in-game hours, and measure before claiming a performance gain.",
    "Filter current output without changing the tool argument.",
    "Filter output lines...",
    "Clear output filter.",
    "No output lines match this filter.",
    "Pin output baseline",
    "Compare outputs",
    "Clear output baseline",
    "Baseline output",
    "Current output",
    "Output baseline pinned.",
    "Pin an output baseline before comparing.",
    "No current output to compare.",
    "No output matches the filter on either side.",
    "Output comparison unavailable because a configured input or work limit was reached.",
    "Output exceeds comparison limits; the baseline was not changed.",
    "Page title",
    "Gauntlet Page Blueprint",
    "Generate a complete, self-contained Gauntlet page with a bound ViewModel, XML prefab, commands, and registration notes.",
    "Show current output",
    "Pin current output as the comparison baseline.",
    "Show the baseline and current outputs side by side.",
    "Return to the current output without removing the baseline.",
    "Clear the pinned output baseline.",
    "Output baseline cleared.",
    "Patch diagnostics",
    "Read-only diagnostics for Forge hooks, Forge patches, and optional loaded patch runtimes.",
    "Optional external runtime",
    "Patch Preflight: Read-only structural check of declared targets and callback references; it never applies patches or invokes callbacks.",
    "ForgeWeave: Event recording and sequence replay. Patch diagnostics inspect Forge-owned hooks and patches; a compatible loaded runtime is observed read-only when available. Patch Preflight checks blueprint declarations without applying patches.",
    "Event Replay & Patch Diagnostics",
    "2. Patch Blueprint Review: Run cf.patch_preflight to review pending declarations. It does not apply patches; applying a Forge method replacement requires a separate explicit opt-in.",
    "1. Forge method replacement requires a separate explicit request; declared hook kinds unsupported by the backend remain metadata. Patch Preflight is read-only and does not invoke callback code. Keep ForgeWeave event listeners non-serialized with AddNonSerializedListener.",
)
OBSOLETE_PANEL_KEYS = {
    "Advanced Developer SDK: Surface contracts for all 110+ Advanced SDK tools, CoALA cognitive memory agents, and decoupled engine API bridges.",
    "1. Never mutate engine entities during render ticks.\n2. Never store raw Hero, Settlement, or MobileParty pointers in persistent fields; resolve by StringId via MBObjectManager.\n3. Inspection queries must be read-only; measure the complete query path before making performance claims.\n4. CoALA memory inspector queries agent episodic and semantic facts without engine thread locks.",
    "2. Query CoALA Memory: Execute cf.agent_memory_query to evaluate semantic beliefs and episodic experience streams.",
    "Query CoALA cognitive memory fact count and decay status",
    "Harmony",
    "Harmony patch atlas",
    "Optional read-only Harmony diagnostics. Shared targets are review candidates, not proven conflicts.",
    "Read-only inventory of active Harmony patches in already-loaded assemblies.",
    "ForgeWeave: Event recording and sequence replay. Harmony Atlas is a separate read-only inventory; Patch Preflight checks blueprint declarations without applying patches.",
    "ForgeWeave Event Pipeline: Deterministic event recording and sequence replay. Harmony diagnostics and Patch Blueprint Preflight are separate tools.",
    "ForgeWeave Event Pipeline: Deterministic event recording and sequence replay. Patch diagnostics inspect Forge-owned hooks and patches; optional external runtime observation is read-only. Patch Blueprint Preflight is a separate tool.",
    "ForgeWeave: Event recording and sequence replay. Patch diagnostics inspect Forge-owned hooks and patches, with optional read-only observation of already-loaded compatible runtimes. Patch Preflight checks blueprint declarations without applying patches.",
}


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
    rule_ui_source = (ROOT / "src" / "CalradiaForge.Mod" / "PanelViewModel.CampaignRules.cs").read_text(encoding="utf-8")
    literal_keys = set(re.findall(r'\bT\("([^"]+)"\)', source))
    original = {folder: parse_strings(LANGUAGE_ROOT / folder / "forge_strings.xml") for folder in LANGUAGES}
    source_catalogs = {
        folder: {entry.attrib["key"]: entry.attrib["value"] for entry in ET.parse(ROOT / "localization" / f"{iso}.xml").getroot()}
        for folder, (iso, _) in LANGUAGES.items()
    }
    navigation_palette = json.loads((ROOT / "localization" / "navigation-palette.json").read_text(encoding="utf-8"))
    required_locale_order = [iso for iso, _ in LANGUAGES.values()]
    required_locales = set(required_locale_order)
    for text, localized in navigation_palette.items():
        if localized.get("en") != text or set(localized) != required_locales or any(not value.strip() for value in localized.values()):
            raise ValueError(f"Incomplete navigation palette translation: {text}")
    composer_source = json.loads((ROOT / "localization" / "gauntlet-composer.json").read_text(encoding="utf-8"))
    composer_languages = composer_source.get("languages", [])
    composer_entries = composer_source.get("entries", [])
    if composer_languages != required_locale_order:
        raise ValueError("Gauntlet Composer translation locales differ from supported languages")
    gauntlet_composer: dict[str, dict[str, str]] = {}
    for entry in composer_entries:
        text = entry.get("key", "")
        values = entry.get("values", [])
        if not text.strip() or text in gauntlet_composer or len(values) != len(composer_languages):
            raise ValueError(f"Invalid or duplicate Gauntlet Composer translation entry: {text}")
        localized = dict(zip(composer_languages, values))
        if localized.get("en") != text or set(localized) != required_locales or any(not value.strip() for value in localized.values()):
            raise ValueError(f"Incomplete Gauntlet Composer translation: {text}")
        if text in navigation_palette:
            raise ValueError(f"Gauntlet Composer translation duplicates navigation palette key: {text}")
        gauntlet_composer[text] = localized
    if not gauntlet_composer:
        raise ValueError("Gauntlet Composer translation source is empty")
    composer_required_texts = set()
    for line in source.splitlines():
        if "GauntletComposer" in line or "OutputHeading" in line:
            composer_required_texts.update(re.findall(r'T\("([^"\n]+)"\)', line))
        if "_gauntletComposerStatusKey =" in line or "SetGauntletComposerStatus(" in line:
            composer_required_texts.update(re.findall(r'"([^"\n]+)"', line))
    composer_required_texts.discard("Search / argument")
    missing_composer_texts = composer_required_texts.difference(gauntlet_composer)
    if missing_composer_texts:
        raise ValueError("Gauntlet Composer UI text lacks source translations: " + ", ".join(sorted(missing_composer_texts)))
    rule_source = json.loads((ROOT / "localization" / "campaign-rule-builder.json").read_text(encoding="utf-8"))
    if rule_source.get("languages") != required_locale_order:
        raise ValueError("Campaign Rule Builder translation locales differ from supported languages")
    campaign_rules: dict[str, dict[str, str]] = {}
    for entry in rule_source.get("entries", []):
        text = entry.get("key", "")
        values = entry.get("values", [])
        if not text.strip() or text in campaign_rules or len(values) != len(required_locale_order):
            raise ValueError(f"Invalid or duplicate Campaign Rule Builder translation entry: {text}")
        localized = dict(zip(required_locale_order, values))
        if localized["en"] != text or any(not value.strip() for value in localized.values()):
            raise ValueError(f"Incomplete Campaign Rule Builder translation: {text}")
        if text in navigation_palette or text in gauntlet_composer:
            raise ValueError(f"Campaign Rule Builder translation duplicates a prior source key: {text}")
        campaign_rules[text] = localized
    if not campaign_rules:
        raise ValueError("Campaign Rule Builder translation source is empty")
    rule_required_texts = set()
    for line in (source + "\n" + rule_ui_source).splitlines():
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
    # The generated EN resource is also an input so existing stable IDs survive. Filter
    # the retired Atlas-only UI strings explicitly; otherwise the generator would keep
    # shipping dead Harmony labels forever even after their source catalogs were cleaned.
    english_by_old_id = {
        old_id: text for old_id, text in original["EN"].items()
        if text not in OBSOLETE_PANEL_KEYS
    }
    english_texts = list(dict.fromkeys(list(english_by_old_id.values()) + sorted(literal_keys) + list(SOURCE_LOCALIZED_PANEL_KEYS) + sorted(navigation_palette) + sorted(gauntlet_composer) + sorted(campaign_rules)))
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
            composer_translation = gauntlet_composer.get(text)
            campaign_translation = campaign_rules.get(text)
            palette_translation = navigation_palette.get(text)
            if campaign_translation is not None:
                localized = campaign_translation[iso]
            elif composer_translation is not None:
                localized = composer_translation[iso]
            elif palette_translation is not None:
                localized = palette_translation[iso]
            elif text in SOURCE_LOCALIZED_PANEL_KEYS:
                localized = source_catalogs[folder].get(text, text)
                if iso == "en" and localized != text:
                    raise ValueError(f"Incomplete in-game Gauntlet label translation for {iso}: {text}")
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
