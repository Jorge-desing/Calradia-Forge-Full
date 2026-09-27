---
name: bannerlord-xml-overrides
description: Guidelines for safely structuring, merging, and overriding XML configurations and assets in Bannerlord mods.
trigger: always_on
---

# XML Overrides and Structure

When injecting new items, troops, cultures, or modifying native assets via XML, enforce these rules:

## 1. Folder Structure Contract
The root folder name inside `Modules/` **MUST** exactly match the `<Id value="..." />` inside the `SubModule.xml` root node. If they do not match, the game launcher will crash or fail to discover the mod.
- C# compiled DLLs go in `bin/Win64_Shipping_Client/`
- Data files go in `ModuleData/`
- UI prefabs go in `GUI/Prefabs/`

## 2. The Merging vs Overwriting Rule
- **Merging (Adding New):** If an `<XmlNode>` registers under an existing ID (e.g., `<XmlName id="Items" />`), the engine *appends* your entities to the global list. 
  - *Best Practice:* Give custom entities an explicit namespace prefix (e.g., `id="custom_mymod_iron_sword"`) to prevent accidental collisions.
- **Overwriting (Patching Native):** To modify a vanilla object (e.g., an Imperial Infantryman), declare the exact same `id="empire_infantry"`. The engine resolves conflicts by **last-loaded wins**. Ensure your mod is ordered below Native in the launcher if you intend to overwrite.

## 3. Gauntlet XML Prefab Overwrites
To overwrite official native user interfaces (e.g., `InventoryScreen.xml`):
1. Recreate the file with the **exact same name** inside your mod's `GUI/Prefabs/` folder.
2. Your mod **MUST** be loaded *before* the module that originally introduced the UI if doing hard overwrites, OR rely on UIExtenderEx if patching dynamically.
