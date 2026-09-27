# Bannerlord XML Schemas & Merging

When working with Bannerlord XML data files (e.g., in `ModuleData/`), adhere to the following schema and merging principles.

## 1. General XML Overwriting and Merging
- **Merging**: If an XML file registers under an existing `id` in `SubModule.xml` (e.g., `Items`), its content is merged with existing items from other modules.
- **Overwriting**: To overwrite an existing vanilla entity, create an XML node with the exact same `id` as the vanilla entity (e.g., `<Item id="empire_sword_1"...>`). The module loaded last will overwrite the previous definition.

## 2. Items (`Items` Category)
- Items are defined within an `<Items>` root node.
- Key attributes: `id`, `name`, `mesh`, `culture`, `weight`, `Type` (e.g., `OneHandedWeapon`, `Horse`).
- Weapon components are defined in `<ItemComponent>` (e.g., `<Weapon ...>`).

## 3. NPC Characters (`NPCCharacters` Category)
- Defined within an `<NPCCharacters>` root node.
- Sub-nodes include `<NPCCharacter>` with attributes like `id`, `name`, `culture`, `default_group`, `level`, `occupation`.
- Contains nested elements for equipment: `<Equipments>`, `<EquipmentRoster>`, `<skill>`, and `<face>`.

## 4. Scenes (`Scene` Category)
- Scenes dictate physical locations. Often tied to `scene_name`.
- Involves complex folder structures (e.g., `SceneObj/`) containing `scene.xscene` files.
- Scene XMLs define terrain, flora, and physics barriers.
