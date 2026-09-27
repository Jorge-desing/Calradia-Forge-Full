---
name: bannerlord-troop-character
description: Schema rules, equipment loadouts, troop tree branches, XML synthesis for custom troops and NPC characters, and item/crafting XML for weapons, armors, and smithing parts in Mount & Blade II Bannerlord.
---

# Bannerlord Troop, Character & Item Creation

This skill covers defining custom troops, companions, lords, weapons, armors, mounts, and smithing crafting parts — all as static XML assets in `ModuleData/`.

> **Note:** This skill is pure XML data. For C# behaviors or game model decorators, see `bannerlord-shared-patterns`.

---

## 1. Directory Structure & Registration

```
Modules/MyMod/
├── SubModule.xml
└── ModuleData/
    ├── custom_troops.xml        ← NPCCharacters
    ├── custom_items.xml         ← Items (weapons, armor, mounts)
    ├── custom_crafting_pieces.xml
    └── custom_crafting_templates.xml
```

Register all XML files in `SubModule.xml`:

```xml
<Module>
  <Id value="MyMod"/>
  <Name value="My Custom Mod"/>
  <Version value="v10.2.0"/>
  <SubModules/>
  <Xmls>
    <XmlNode>
      <XmlName id="NPCCharacters" path="custom_troops"/>
    </XmlNode>
    <XmlNode>
      <XmlName id="Items" path="custom_items"/>
    </XmlNode>
    <XmlNode>
      <XmlName id="CraftingPieces" path="custom_crafting_pieces"/>
    </XmlNode>
    <XmlNode>
      <XmlName id="CraftingTemplates" path="custom_crafting_templates"/>
    </XmlNode>
  </Xmls>
</Module>
```

---

## 2. Troop XML Template (`custom_troops.xml`)

```xml
<?xml version="1.0" encoding="utf-8"?>
<NPCCharacters>
  <!-- Tier 1 Recruit -->
  <NPCCharacter id="mymod_battanian_skirmisher"
                name="{=mymod_skirmisher}Battanian Woodrunner"
                age="22"
                level="11"
                occupation="Soldier"
                culture="Culture.battania"
                default_group="Ranged"
                is_hero="false">
    <face>
      <face_key_template value="BodyProperty.fighter_battania" />
    </face>
    <skills>
      <skill id="OneHanded" value="60" />
      <skill id="Throwing"  value="80" />
      <skill id="Athletics" value="70" />
    </skills>
    <Equipments>
      <!-- Battle Equipment 1 -->
      <EquipmentSet>
        <equipment slot="Item0" id="Item.battania_axe_1_t2" />
        <equipment slot="Item1" id="Item.battania_throwing_axes_1_t2" />
        <equipment slot="Item2" id="Item.battania_shield_t2" />
        <equipment slot="Head"  id="Item.leather_cap" />
        <equipment slot="Body"  id="Item.rough_bearskin" />
        <equipment slot="Gloves" id="Item.rough_tied_bracers" />
        <equipment slot="Leg"   id="Item.wrapped_shoes" />
      </EquipmentSet>
      <!-- Battle Equipment Variation 2 -->
      <EquipmentSet>
        <equipment slot="Item0" id="Item.battania_axe_1_t2" />
        <equipment slot="Item1" id="Item.battania_javelins_1_t2" />
        <equipment slot="Item2" id="Item.battania_shield_t2" />
        <equipment slot="Head"  id="Item.woodland_hood" />
        <equipment slot="Body"  id="Item.rough_bearskin" />
        <equipment slot="Leg"   id="Item.wrapped_shoes" />
      </EquipmentSet>
      <!-- Civilian Equipment Set (REQUIRED for settlement entry) -->
      <EquipmentSet civilian="true">
        <equipment slot="Item0" id="Item.battania_dagger" />
        <equipment slot="Body"  id="Item.battania_civilian_tunic" />
        <equipment slot="Leg"   id="Item.wrapped_shoes" />
      </EquipmentSet>
    </Equipments>
    <upgrade_targets>
      <upgrade_target id="NPCCharacter.mymod_battanian_harasser" />
      <upgrade_target id="NPCCharacter.mymod_battanian_veteran_skirmisher" />
    </upgrade_targets>
  </NPCCharacter>
</NPCCharacters>
```

### Equipment Slots Reference

| Slot | Usage | Constraints |
|:---|:---|:---|
| `Item0`–`Item3` | Weapons, Shields, Bows, Ammo | Max 4 weapon slots |
| `Head` | Helmets, coifs, hoods | Single item |
| `Cape` | Shoulder armor, cloaks | Single item |
| `Body` | Chest/torso armor | Single item |
| `Gloves` | Bracers, gauntlets | Single item |
| `Leg` | Boots, greaves | Single item |
| `Horse` | Mount animal | Required for Cavalry / HorseArcher |
| `HorseHarness` | Saddle or horse barding | Optional |

### Troop Crash Hazards

1. **Integer Age Rule:** `age="25"` ✅ — `age="25.0"` or `age="25.5"` cause an immediate crash on startup.
2. **Upgrade Target prefix:** Always use `NPCCharacter.` prefix on `<upgrade_target id>`.
3. **Civilian Equipment Sets are required** for any troop that enters settlements.
4. **No inline XML comments inside `<Equipments>`** tags — the parser crashes.

---

## 3. Weapon Item Template (`custom_items.xml`)

```xml
<?xml version="1.0" encoding="utf-8"?>
<Items>
  <Item id="mymod_noble_longsword"
        name="{=mymod_noble_ls}Noble Longsword"
        mesh="mymod_longsword_mesh"
        culture="Culture.vlandia"
        weight="1.4"
        appearance="1"
        Type="OneHandedWeapon">
    <ItemComponent>
      <Weapon weapon_class="OneHandedSword"
              thrust_speed="94"
              speed_rating="95"
              weapon_balance="97"
              thrust_damage="36"
              thrust_damage_type="Pierce"
              swing_damage="78"
              swing_damage_type="Cut"
              item_usage="one_handed_sword" />
    </ItemComponent>
  </Item>
</Items>
```

### Damage Type Enums
Only three values are valid: `Cut`, `Pierce`, `Blunt`. Any other string is silently ignored and the weapon deals 0 damage.

---

## 4. Smithing Recipe Linkage

`custom_crafting_pieces.xml`:
```xml
<CraftingPieces>
  <CraftingPiece id="mymod_blade_broadsword"
                 name="{=mymod_b01}Fine Broadsword Blade"
                 tier="4"
                 piece_type="Blade"
                 mesh="mymod_blade_mesh"
                 length="90"
                 weight="0.9" />
</CraftingPieces>
```

`custom_crafting_templates.xml`:
```xml
<CraftingTemplates>
  <CraftingTemplate id="OneHandedSword">
    <UsablePieces>
      <UsablePiece piece_id="mymod_blade_broadsword" />
    </UsablePieces>
  </CraftingTemplate>
</CraftingTemplates>
```

### Smithy Crash Rules
- Every `piece_id` in a `CraftingTemplate` **must** exist in `CraftingPieces` — missing references crash the smithy screen.
- Masterwork crafting requires all four parts: Blade, Guard, Handle, and Pommel.
- Mount skeleton must be `monster="Monster.horse"` or `monster="Monster.camel"` — mismatch causes invisible rides.
