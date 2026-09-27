"""Build short in-game localization from the SDK XML API documentation."""
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SUPPORTED = {"en", "es", "pt", "de", "fr", "it", "pl", "ru", "tr", "zh-HANS", "zh-HANT", "ja", "ko"}
help_path = ROOT / "localization" / "in-game-help.json"
docfx_api = ROOT / "docs-site" / "api"
if not docfx_api.is_dir():
    sys.exit("DocFX API output is missing. Build docs-site before extracting in-game help.")

def indent_xml(element, level=0):
    padding = "\n" + "  " * level
    child_padding = "\n" + "  " * (level + 1)
    if len(element):
        if not element.text or not element.text.strip():
            element.text = child_padding
        for child in element:
            indent_xml(child, level + 1)
            if not child.tail or not child.tail.strip():
                child.tail = child_padding
        if not element[-1].tail or not element[-1].tail.strip():
            element[-1].tail = padding

def docfx_summary(uid):
    member_id = uid.split(":", 1)[1] if ":" in uid else uid
    for path in docfx_api.rglob("*.yml"):
        raw = path.read_text(encoding="utf-8")
        for block in re.split(r"(?m)^- uid: ", raw)[1:]:
            block_uid = block.splitlines()[0].strip()
            comment_id = re.search(r"(?m)^  commentId:\s*(.+?)\s*$", block)
            if block_uid not in (uid, member_id) and (comment_id is None or comment_id.group(1) != uid):
                continue
            match = re.search(r"(?ms)^  summary:\s*(.*?)(?=^  [A-Za-z][A-Za-z0-9_-]*:|^- uid:|\Z)", block)
            if not match:
                break
            content = match.group(1).strip()
            content = re.sub(r"^[>|][+-]?\s*", "", content)
            return " ".join(line.strip() for line in content.splitlines() if line.strip())
    # Some DocFX metadata versions omit a summary from the API YAML even though
    # the compiler XML document is present beside the SDK assembly. Read that
    # same authored comment as a fallback so localization generation is stable.
    for xml_path in (
        ROOT / "src" / "CalradiaForge.Sdk" / "bin" / "Release" / "net472" / "CalradiaForge.Sdk.xml",
        ROOT / "src" / "CalradiaForge.Sdk" / "bin" / "Release" / "net8.0" / "CalradiaForge.Sdk.xml",
    ):
        if not xml_path.is_file():
            continue
        try:
            root = ET.parse(xml_path).getroot()
        except ET.ParseError:
            continue
        members = root.find("members")
        member = members.find("member[@name='" + uid.replace("'", "&apos;") + "']") if members is not None else None
        summary = member.find("summary") if member is not None else None
        if summary is not None:
            content = " ".join("".join(summary.itertext()).split())
            if content:
                return content
    return ""
help_data = json.loads(help_path.read_text(encoding="utf-8"))
for key, values in help_data.items():
    source_member = values.get("sourceMember")
    if source_member:
        summary = docfx_summary(source_member)
        if not summary:
            sys.exit(f"DocFX/API summary not found for in-game help key: {key} ({source_member})")
        values["en"] = summary
    for language in SUPPORTED:
        if not values.get(language):
            values[language] = values.get("en", key)
help_path.write_text(json.dumps(help_data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

for language in sorted(SUPPORTED):
    target = ROOT / "localization" / f"{language}.xml"
    root = ET.parse(target).getroot()
    entries = {item.get("key"): item for item in root.findall("string")}
    for key, values in help_data.items():
        value = values.get(language) or values["en"]
        if key in entries:
            entries[key].set("value", value)
        else:
            ET.SubElement(root, "string", {"key": key, "value": value})
    indent_xml(root)
    ET.ElementTree(root).write(target, encoding="utf-8", xml_declaration=True)

print(f"Generated {len(help_data)} DocFX-backed contextual help strings for {len(SUPPORTED)} languages.")
