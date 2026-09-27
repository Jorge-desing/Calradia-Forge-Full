---
name: bannerlord-audio-system
description: Architectural guidelines and XML schemas for custom sound effects, audio categories, and sound bank integration in Mount & Blade II Bannerlord.
trigger: always_on
---

# Bannerlord Custom Audio & Sound Architecture

When adding custom audio, sound effects, UI feedback, or voice lines to Bannerlord, you MUST follow the engine's audio configuration rules.

## 1. Directory Layout & Registration
- **Audio Files Directory:** Place all `.ogg` and `.wav` audio assets into:
  `Modules/<YourModId>/ModuleSounds/`
- **Definition Manifest:** Declare sounds inside:
  `Modules/<YourModId>/ModuleData/module_sounds.xml`
- **SubModule.xml Entry:** Register the sound definitions under the engine XML category `Sounds` or `ModuleSounds`:
  ```xml
  <XmlNode>
    <XmlName id="Sounds" path="module_sounds" />
  </XmlNode>
  ```

## 2. XML Schema (`module_sounds.xml`)
Every custom sound must be defined within the `<module_sounds>` root element:

```xml
<module_sounds>
  <!-- 2D UI or Interface Audio -->
  <module_sound name="custom_ui_click" is_2d="true" sound_category="ui" path="ui_click.ogg" />
  
  <!-- 2D Campaign Music / Notification -->
  <module_sound name="custom_quest_complete_jingle" is_2d="true" sound_category="ui" path="quest_fanfare.ogg" />

  <!-- 3D Mission Combat Sound -->
  <module_sound name="custom_iron_shield_clash" is_2d="false" sound_category="mission_combat" path="shield_clash.ogg" />
</module_sounds>
```

### Critical Attributes:
- **`name`**: The unique string ID used by C# code (`SoundEvent.PlaySound2D("custom_ui_click")` or `Mission.MakeSound()`).
- **`is_2d`**: Set to `true` for UI clicks, background stings, and HUD notifications. Set to `false` for positional 3D sound emitters in combat or town scenes.
- **`sound_category`**: MUST match an active mixer category (`ui`, `mission_combat`, `ambient`, `voice`). An invalid category will cause the sound to be silently ignored by the engine mixer.
- **`path`**: The filename relative to `ModuleSounds/`.

## 3. C# Audio Playback Patterns
To trigger sounds cleanly from C#:
```csharp
// 2D Interface Feedback
TaleWorlds.Engine.SoundEvent.PlaySound2D("custom_ui_click");

// 3D Positional Combat Feedback
var soundId = TaleWorlds.Engine.SoundEvent.GetEventIdFromString("custom_iron_shield_clash");
TaleWorlds.Engine.MBSoundEvent.PlaySound(soundId, agent.Position);
```

## 4. Performance & Format Guidelines
- **Formats:** Use compressed `.ogg` files (Vorbis) for almost all audio (UI, ambient, voices) to keep module sizes lean. Reserve uncompressed 16-bit 44.1kHz `.wav` only for short combat hit sounds requiring zero-latency decoding.
- **Complex Audio:** Complex audio graphs (pitch variance, distance attenuation curves, dynamic Doppler) must be packaged into custom FMOD `.bank` files using TaleWorlds' official FMOD Studio templates.
