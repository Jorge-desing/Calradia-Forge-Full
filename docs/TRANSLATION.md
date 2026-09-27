# Translation

English is the canonical source language. `localization/en.xml` contains the English keys and default text; `localization/es.xml` contains the Spanish translation. Filenames, code identifiers, logs, protocol fields and SDK contracts remain English.

## Contribution order

1. Implement and review the feature in English, including filenames, namespaces, classes, methods, variables, authored comments, UI copy, examples and primary documentation.
2. Add its English text to `localization/en.xml`.
3. Add the Latin American Spanish translation to `localization/es.xml`, preserving the English key.
4. Regenerate the game language resources and verify both languages. Spanish documents use a language suffix, such as `README.es.md`; source files are not duplicated or renamed for translation.

The native panel follows BannerlordConfig.Language automatically without a language whitelist. Native labels use TaleWorlds TextObject and the game's current-language lookup. It refreshes on opening and when the game language changes; a previous Forge language setting cannot override the game. The desktop starts in English and its explicit picker exposes the same 13 installed Bannerlord languages: English, Latin American Spanish, Brazilian Portuguese, German, French, Italian, Polish, Russian, Turkish, Simplified Chinese, Traditional Chinese, Japanese and Korean. A missing translation uses the token's English fallback; detecting a custom language does not silently claim a translation that is not present.

All 13 desktop catalogs are embedded in Core. English and Spanish are maintained as source catalogs; the other 11 catalogs are generated from the reviewed rows in `localization/native-menu.json` and retain English for diagnostic phrases that are outside the native menu set. The generator validates complete menu rows, writes the desktop XML catalogs, and creates ModuleData/Languages resources with IDs `forge_` plus the first 12 lowercase SHA-256 hex characters of each UTF-8 English key. Native labels use the game resources, while the desktop uses the embedded catalogs. Voice language and Windows locale do not change the native panel; the desktop picker is explicit.

Translate values, preserving keys and placeholders. The English value is the fallback for unknown keys. Diagnostic payloads and exceptions from third-party modules or the operating system are preserved in their original language; they are evidence, not UI labels.

To add a native language, add its exact game language ID, folder, ISO value and translated menu row to native-menu.json, then regenerate resources. No C# change is required. Community translation modules may alternatively supply the same string IDs through Bannerlord's language resources. Verify layout and fonts in the selected game language. The generated catalogs cover every literal panel label, help text and known operational message declared by `PanelViewModel`; arbitrary SDK output, logs and technical diagnostic payloads remain source evidence. Do not translate game object IDs, API names, command IDs or JSON property names.
