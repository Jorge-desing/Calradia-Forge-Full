# Calradia Forge Content Showcase

This installable sample demonstrates static `Items` and `NPCCharacters` XML, the SDK builders that create them, and a small module-owned Gauntlet extension page. It targets the game's `net472` runtime and adds no API, dependency, or behavior to Calradia Forge itself.

Run `tools\Test-CalradiaForge-ContentShowcase.bat` from the repository to regenerate the sample deterministically, validate it against the installed game's `Items.xsd` and `NPCCharacters.xsd`, compile the module, run the bounded time-slicing harness, and execute the Core regression suite through its BAT launcher. Set `BANNERLORD_GAME_PATH` when the game is installed elsewhere.

The output module is `modules/CalradiaForgeContentShowcase`; the directory name matches the manifest ID. The module depends on Native, SandBoxCore, and CalradiaForge. Its troop uses the module's `cfcs_practice_whip` item. That item reuses the installed Native `horse_whip` mesh and body because this example contains no custom art assets. The troop is a static character definition; it is not added to a party template or troop tree.

The page registers through `ForgeApi.RegisterWhenAvailable` and is opened only when selected through Calradia Forge's existing extension-page flow. It contains localized read-only text and a close command; opening it does not run tests, spawn entities, or change game state. Static source and XSD checks do not prove Bannerlord loading, actual gameplay behavior, or live Gauntlet rendering.

See [schema provenance](SCHEMA-PROVENANCE.md) for the installed source files and hashes used during authoring.
