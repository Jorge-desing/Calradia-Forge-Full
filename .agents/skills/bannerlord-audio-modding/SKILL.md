---
name: bannerlord-audio-modding
description: Best practices, XML schema, and C# playback integration for custom sound effects, voices, and audio events in Mount & Blade II Bannerlord.
---

# Bannerlord Custom Audio Modding

This skill covers registering, formatting, and triggering custom sound effects and audio events in *Mount & Blade II: Bannerlord*.

> **Note:** This skill is XML/C# audio-only and does not use GameModels or CampaignBehaviors. For those patterns, see `bannerlord-shared-patterns`.

---

## 1. Directory Structure

Place all custom audio assets inside the mod folder:

```
Modules/MyMod/
├── SubModule.xml
├── ModuleSounds/
│   ├── custom_ui_click.ogg
│   ├── custom_victory_fanfare.ogg
│   └── custom_shield_hit.ogg
└── ModuleData/
    └── module_sounds.xml
```

---

## 2. Manifest Registration (`SubModule.xml`)

Add the sound definition file to your `SubModule.xml`:

```xml
<Module>
  <Id value="MyMod"/>
  <Name value="My Custom Mod"/>
  <Version value="v1.0.0"/>
  <SubModules/>
  <Xmls>
    <XmlNode>
      <XmlName id="Sounds" path="module_sounds"/>
    </XmlNode>
  </Xmls>
</Module>
```

---

## 3. Sound Definitions Schema (`ModuleData/module_sounds.xml`)

```xml
<?xml version="1.0" encoding="utf-8"?>
<module_sounds>
  <!-- 2D Interface Audio -->
  <module_sound name="mod_click_action" 
                is_2d="true" 
                sound_category="ui" 
                path="custom_ui_click.ogg" />

  <!-- 2D Campaign Fanfare / Music Sting -->
  <module_sound name="mod_quest_victory" 
                is_2d="true" 
                sound_category="ui" 
                path="custom_victory_fanfare.ogg" />

  <!-- 3D Positional Combat Sound -->
  <module_sound name="mod_shield_impact" 
                is_2d="false" 
                sound_category="mission_combat" 
                path="custom_shield_hit.ogg" />
</module_sounds>
```

### Attribute Reference:
| Attribute | Type | Description |
|---|---|---|
| `name` | `string` | Unique engine identifier used in C# `SoundEvent` calls. |
| `is_2d` | `bool` | `true` for non-positional UI/HUD sounds; `false` for positional 3D sounds in scenes. |
| `sound_category` | `string` | Engine mixer category (`ui`, `mission_combat`, `ambient`, `voice`). |
| `path` | `string` | Filename inside `ModuleSounds/` (supports `.ogg` and `.wav`). |

---

## 4. C# Audio Playback API

### 2D Audio (UI, Menus, Alerts)
```csharp
using TaleWorlds.Engine;

public static class ModAudioManager
{
    public static void PlayUiClick()
    {
        SoundEvent.PlaySound2D("mod_click_action");
    }

    public static void PlayFanfare()
    {
        SoundEvent.PlaySound2D("mod_quest_victory");
    }
}
```

### 3D Positional Audio (Combat & Mission Scenes)
```csharp
using TaleWorlds.Engine;
using TaleWorlds.Library;

public static class MissionAudioHelper
{
    public static void PlayAtPosition(string soundEventName, Vec3 worldPosition)
    {
        int eventId = SoundEvent.GetEventIdFromString(soundEventName);
        if (eventId != -1)
        {
            MBSoundEvent.PlaySound(eventId, worldPosition);
        }
    }
}
```

---

## 5. Golden Rules for Audio
1. **Always use compressed `.ogg` files**: Reduces mod download size by 80% without noticeable fidelity loss.
2. **Never invent arbitrary `sound_category` strings**: Must be one of `ui`, `mission_combat`, `ambient`, or `voice`. Any other category is ignored by the FMOD bus mixer.
3. **Verify matching XML paths**: The `path` attribute in `SubModule.xml` must omit the `.xml` extension (`path="module_sounds"`).
