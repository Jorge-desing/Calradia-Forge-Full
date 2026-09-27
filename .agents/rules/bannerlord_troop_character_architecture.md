# Bannerlord Troop & Character Architecture (`NPCCharacters.xml`)

When declaring custom troops, lords, mercenaries, companions, or NPC templates for Mount & Blade II: Bannerlord, you MUST follow the engine's XML schema and equipment rules.

## 1. Directory Layout & SubModule.xml Registration
- **Data Directory:** Store character definitions in `Modules/<YourModId>/ModuleData/` (e.g. `ModuleData/custom_troops.xml`).
- **SubModule.xml Registration:** Character definitions MUST be registered under the engine XML category `NPCCharacters`:
  ```xml
  <XmlNode>
    <XmlName id="NPCCharacters" path="custom_troops" />
  </XmlNode>
  ```
- **Merging & Prefixes:** Like all game XML, declaring new `<NPCCharacter>` elements appends them to the global registry. Always prefix custom troop IDs (e.g. `id="mymod_vlandia_footman"`) to prevent collisions with vanilla troops, unless you intentionally intend to overwrite a native troop.

## 2. Core `<NPCCharacter>` Schema & Attributes
```xml
<NPCCharacters>
  <NPCCharacter id="mymod_vlandia_veteran_sergeant"
                name="{=mymod_vet_sergeant}Veteran Sergeant"
                age="32"
                level="26"
                occupation="Soldier"
                culture="Culture.vlandia"
                default_group="Infantry"
                is_hero="false"
                is_female="false">
    <face>
      <face_key_template value="BodyProperty.fighter_vlandia" />
    </face>
    <skills>
      <skill id="OneHanded" value="130" />
      <skill id="Polearm" value="110" />
      <skill id="Athletics" value="100" />
    </skills>
    <Equipments>
      <EquipmentSet>
        <equipment slot="Item0" id="Item.vlandic_bastard_sword" />
        <equipment slot="Item1" id="Item.vlandic_heavy_heater_shield" />
        <equipment slot="Head" id="Item.spiked_helmet_with_mail" />
        <equipment slot="Cape" id="Item.mail_shoulders" />
        <equipment slot="Body" id="Item.vlandic_coat_of_plates" />
        <equipment slot="Gloves" id="Item.mail_gauntlets" />
        <equipment slot="Leg" id="Item.iron_greaves" />
      </EquipmentSet>
      <EquipmentSet civilian="true">
        <equipment slot="Item0" id="Item.vlandic_dagger" />
        <equipment slot="Body" id="Item.tunic_with_belt" />
        <equipment slot="Leg" id="Item.rough_tied_boots" />
      </EquipmentSet>
    </Equipments>
    <upgrade_targets>
      <upgrade_target id="NPCCharacter.mymod_vlandia_banneret" />
    </upgrade_targets>
  </NPCCharacter>
</NPCCharacters>
```

## 3. Equipment Slots Contract
Every `<EquipmentSet>` child uses `<equipment slot="..." id="Item.item_id" />`.
Valid engine slots are:
- **`Item0`, `Item1`, `Item2`, `Item3`**: Weapons, shields, bows, crossbows, and ammo (maximum 4 slots).
- **`Head`**: Helmets, coifs, hoods.
- **`Cape`**: Shoulders, cloaks, pauldrons.
- **`Body`**: Torso armor, hauberks, coats of plates, robes.
- **`Gloves`**: Bracers, gauntlets, gloves.
- **`Leg`**: Boots, greaves, chausses.
- **`Horse`**: Mount animal (required for `Cavalry` and `HorseArcher`).
- **`HorseHarness`**: Saddle or horse armor.

### Equipment Randomization & Civilian Sets:
- **Multiple Sets:** Multiple `<EquipmentSet>` entries (without `civilian="true"`) are treated by the engine as random equipment variations spawned per individual soldier.
- **Civilian Set:** Always provide one `<EquipmentSet civilian="true">` for any character or troop that can enter towns, keeps, or taverns.

## 4. Upgrade Trees (`<upgrade_targets>`)
- Soldiers upgrade to subsequent tiers via `<upgrade_targets>`.
- Each target is defined as `<upgrade_target id="NPCCharacter.target_troop_id" />`.
- A troop can have up to **two branching upgrade targets** (e.g. Footman upgrading to either Swordsman or Crossbowman).
- Target IDs must include the `NPCCharacter.` namespace prefix inside the attribute value.

## 5. Formation Groups (`default_group`)
Valid formation categories for tactical deployment:
- `Infantry`
- `Ranged`
- `Cavalry`
- `HorseArcher`

## 6. Critical Engine Constraints & Crash Guards
- **The Integer Age Trap:** The `age` attribute MUST be an integer (e.g. `age="25"`). Passing a decimal or float (e.g. `age="25.4"`) will cause TaleWorlds' XML deserializer to throw an unhandled exception and crash to desktop during the loading screen.
- **XML Comment Sensitivity:** Avoid inserting XML comments inside `<Equipments>` or between `<EquipmentSet>` nodes. The native streaming XML reader can silently truncate the equipment array when encountering inline comments in character blocks.
- **Skill Balancing:** Never leave combat troops with 0 in their primary weapon skills; doing so causes AI agents to fail animation blend trees in battle.
