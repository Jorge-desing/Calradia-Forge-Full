---
name: bannerlord-localization
description: Generates and synchronizes Bannerlord localization XML files using SHA-256 hashing. Enforces UTF-8 with BOM and &#10; encoding for newlines.
---

# Bannerlord Localization

> **Note:** This skill covers localization manifests, encoding rules, and sync workflows. For campaign/game model patterns, see `bannerlord-shared-patterns`.

---

## 1. Directory Structure & XML Manifests

Localization data is organized by ISO language code under `ModuleData/Languages/`:

```
Modules/MyMod/
├── SubModule.xml
└── ModuleData/
    └── Languages/
        ├── language_data.xml       ← Main language manifest
        ├── EN/
        │   ├── language_data.xml   ← English language registration
        │   └── forge_strings.xml   ← English string definitions
        ├── ES/
        │   ├── language_data.xml
        │   └── forge_strings.xml
        └── ...
```

### Manifest Schema (`ModuleData/Languages/language_data.xml`):
```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData id="English" subtitle_extension="en">
  <LanguageFiles>
    <LanguageFile xml_path="Languages/EN/forge_strings.xml" />
  </LanguageFiles>
</LanguageData>
```

---

## 2. Localization XML Schema (`forge_strings.xml`)

```xml
<?xml version="1.0" encoding="utf-8"?>
<base xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" type="string">
  <tags>
    <tag language="English" />
  </tags>
  <strings>
    <string id="forge_welcome_msg" text="Welcome to Calradia Forge." />
    <string id="forge_escort_quest_title" text="Escort Caravan to {DESTINATION}" />
    <string id="forge_multiline_notice" text="First instruction paragraph.&#10;&#10;Second paragraph after line break." />
  </strings>
</base>
```

---

## 3. C# Localization Usage Patterns

```csharp
using TaleWorlds.Localization;

namespace CalradiaForge.Localization
{
    public static class TextExamples
    {
        // 1. Static Text with Hash Key
        public static TextObject GetWelcomeText()
        {
            return new TextObject("{=forge_welcome_msg}Welcome to Calradia Forge.");
        }

        // 2. Dynamic Text with Variables (SAFE)
        public static TextObject GetEscortQuestTitle(string settlementName)
        {
            return new TextObject("{=forge_escort_quest_title}Escort Caravan to {DESTINATION}")
                .SetTextVariable("DESTINATION", settlementName);
        }
    }
}
```

---

## 4. Automation & Synchronization Script

The project provides an automated synchronization script to propagate English keys to all 13 supported languages:

```bash
python .agents/skills/bannerlord-localization/scripts/sync_langs.py
```

### Internal Script Mechanics (`sync_langs.py`):
- Reads `EN/forge_strings.xml` as master reference.
- Preserves existing translations in other language directories.
- Automatically creates missing `language_data.xml` files.
- Enforces `utf-8-sig` (BOM) on all outputs.
- Replaces raw newlines (`\n`) with `&#10;` to prevent TaleWorlds' XML streaming parser from stripping whitespace.

---

## 5. ⚠️ Critical Engine Constraints & Crash Hazards

1. **Encoding:** Always save XML files as UTF-8 with BOM (`utf-8-sig`). Plain UTF-8 files will corrupt accented characters or cause parser crashes.
2. **Newline Escaping:** Newlines in XML attribute values (`text="..."`) must strictly be encoded as `&#10;`.
3. **No Dynamic Concatenation in Tokens:** Never concatenate variables inside `new TextObject("{=key}" + variable)`. Always use `.SetTextVariable("VAR", value)`.
4. **XML Entity Safety:** Escape `&` as `&amp;`, `<` as `&lt;`, `>` as `&gt;`, and `"` as `&quot;`.
