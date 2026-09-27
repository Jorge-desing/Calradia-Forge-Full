# Bannerlord Items & Crafting Architecture (`Items.xml`, `crafting_pieces.xml`)

When adding custom weapons, armor, mounts, or modular smithing pieces to Mount & Blade II: Bannerlord, follow the engine's XML item schema and crafting pipeline rules.

## 1. Directory Layout & SubModule.xml Registration
- **Item Files Directory:** Place XML data in `Modules/<YourModId>/ModuleData/`.
- **SubModule.xml Manifest:** Declare your custom XML assets under the corresponding engine categories:
  ```xml
  <Xmls>
    <XmlNode>
      <XmlName id="Items" path="custom_items" />
    </XmlNode>
    <XmlNode>
      <XmlName id="CraftingPieces" path="custom_crafting_pieces" />
    </XmlNode>
    <XmlNode>
      <XmlName id="CraftingTemplates" path="custom_crafting_templates" />
    </XmlNode>
  </Xmls>
  ```

## 2. Item Schema & `<ItemComponent>` Contracts
Every item is defined within `<Items><Item id="..." name="..." mesh="..." culture="..." weight="..." Type="...">`:

### A. Weapons (`Type="OneHandedWeapon"`, `"TwoHandedWeapon"`, `"Polearm"`, `"Bow"`, etc.)
```xml
<Item id="custom_vlandic_arming_sword"
      name="{=custom_sword_name}Vlandian Masterwork Arming Sword"
      mesh="custom_vlandian_sword_mesh"
      culture="Culture.vlandia"
      weight="1.3"
      appearance="1"
      Type="OneHandedWeapon">
  <ItemComponent>
    <Weapon weapon_class="OneHandedSword"
            thrust_speed="93"
            speed_rating="96"
            weapon_balance="98"
            thrust_damage="34"
            thrust_damage_type="Pierce"
            swing_damage="72"
            swing_damage_type="Cut"
            item_usage="one_handed_sword" />
  </ItemComponent>
</Item>
```

### B. Armor (`Type="HeadArmor"`, `"BodyArmor"`, `"LegArmor"`, `"HandArmor"`, `"Cape"`)
```xml
<Item id="custom_imperial_scale_hauberk"
      name="{=custom_armor_name}Imperial Scale Hauberk"
      mesh="custom_scale_hauberk_mesh"
      culture="Culture.empire"
      weight="14.5"
      Type="BodyArmor">
  <ItemComponent>
    <Armor body_armor="48" leg_armor="18" arm_armor="16" has_gender_variations="true" />
  </ItemComponent>
</Item>
```

### C. Mounts & Harnesses (`Type="Horse"`, `"Type="HorseHarness"`)
```xml
<!-- Mount Definition -->
<Item id="custom_warhorse_destrier"
      name="{=destrier_name}Imperial Destrier"
      mesh="horse_mesh"
      culture="Culture.empire"
      Type="Horse">
  <ItemComponent>
    <Horse monster="Monster.horse"
           maneuver="68"
           speed="48"
           charge_damage="24"
           extra_health="60"
           is_pack_animal="false"
           is_mountable="true" />
  </ItemComponent>
</Item>

<!-- Horse Barding Definition -->
<Item id="custom_heavy_mail_barding"
      name="{=barding_name}Heavy Mail Barding"
      mesh="barding_mesh"
      culture="Culture.empire"
      Type="HorseHarness">
  <ItemComponent>
    <Armor head_armor="18"
           body_armor="45"
           leg_armor="12"
           maneuver_bonus="-2"
           speed_bonus="-1"
           charge_bonus="10"
           covers_head="true"
           reins_mesh="horse_harness_rein_mesh"
           family_type="1" />
  </ItemComponent>
</Item>
```

## 3. Modular Smithing Pipeline
The smithing system relies on three interconnected XML definitions:
1. **`crafting_pieces.xml`**: Defines modular parts (Blade, Guard, Handle, Pommel).
   ```xml
   <CraftingPiece id="custom_blade_01"
                  name="{=custom_b01}Tempered Broadsword Blade"
                  tier="4"
                  piece_type="Blade"
                  mesh="custom_blade_mesh"
                  length="88"
                  weight="0.9" />
   ```
2. **`crafting_templates.xml`**: The recipe linking templates (e.g. `OneHandedSword`) to allowed pieces.
   ```xml
   <CraftingTemplate id="OneHandedSword">
     <UsablePieces>
       <UsablePiece piece_id="custom_blade_01" />
     </UsablePieces>
   </CraftingTemplate>
   ```

## 4. Critical Engine Constraints & Crash Guards
- **The Smithy Crash Trap:** If a `<UsablePiece piece_id="xxx" />` is declared in `crafting_templates.xml` but the corresponding `<CraftingPiece id="xxx">` is NOT loaded or missing, the game will crash instantly upon opening the Blacksmithing screen.
- **Damage Types Contract:** `thrust_damage_type` and `swing_damage_type` MUST strictly be one of `Cut`, `Pierce`, or `Blunt`. Any other value will cause an unhandled parsing exception.
- **Four-Part Masterwork Rule:** For crafted weapons to qualify for "Masterwork" or "Legendary" quality rolls in campaign mode, they must assemble all 4 modular part categories (Blade, Guard, Handle, Pommel).
- **Armor Gender Variations:** Set `has_gender_variations="false"` if you only have a single unisex mesh to prevent the engine looking for missing female geometry.
