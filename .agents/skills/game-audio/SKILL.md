---
name: game-audio
description: Interactive sound design, audio mixer integration, and spatial audio playback architecture for Mount & Blade II: Bannerlord. XML audio manifest declarations, 2D UI stings, 3D combat positional emitters, sound categories, and low-latency OGG/WAV formatting.
risk: safe
source: Calradia Forge Agent Ecosystem (Apache 2.0)
date_added: 2026-09-28
---

# Game Audio: Interactive Sound Design & Spatial Audio Architecture

Audio in Mount & Blade II: Bannerlord creates the visceral impact of battlefield combat: the thunder of cavalry hooves on muddy terrain, the sharp ring of tempered steel against iron shields, the distant roar of siege trebuchets, and immediate auditory feedback in user interfaces. Adding custom audio requires understanding TaleWorlds' multi-tier sound engine: **ModuleSounds directory layouts**, **XML audio manifests**, **engine mixer categories**, and **2D vs 3D spatial playback APIs**.

---

## 1. Core Principles

1. **Strict Audio Manifest Architecture**:
   - Audio files (`.ogg`, `.wav`) must reside inside:
     `Modules/<YourModId>/ModuleSounds/`
   - Custom sounds must be declared in:
     `Modules/<YourModId>/ModuleData/module_sounds.xml`
   - The audio manifest must be registered in `SubModule.xml` under the engine XML ID `Sounds` (omitting `.xml` in the path):
     ```xml
     <XmlNode>
       <XmlName id="Sounds" path="module_sounds" />
     </XmlNode>
     ```
2. **Mixer Category Fidelity**:
   - The `sound_category` attribute routes the audio stream to TaleWorlds' internal mixer buses.
   - Allowed canonical categories:
     - `ui`: 2D interface clicks, notifications, and menu stings.
     - `mission_combat`: 3D weapon clashes, shield blocks, and arrow impacts.
     - `ambient`: Environmental wind, rain, and crowd murmurs.
     - `voice`: Dialogue speech lines and battle warcries.
   - **CRITICAL**: An invalid or misspelled category causes the sound to be silently dropped by the engine mixer.
3. **2D vs 3D Spatial Audio Boundaries**:
   - **2D UI / HUD (`is_2d="true"`)**: Independent of camera and listener orientation. Triggered via `SoundEvent.PlaySound2D("sound_name")`.
   - **3D Positional (`is_2d="false"`)**: Attenuated by distance and panned by listener head orientation. Triggered via `MBSoundEvent.PlaySound(soundId, agent.Position)`.
4. **Format & Latency Strategy**:
   - Use Vorbis `.ogg` for almost all audio (UI stings, voices, music) to minimize memory footprint.
   - Reserve uncompressed 16-bit 44.1kHz `.wav` only for short combat hit sounds requiring zero-latency decoding.

---

## 2. Capabilities & Scope

### Capabilities
- `audio-manifest-authoring`: Creates and validates `module_sounds.xml` schemas.
- `2d-ui-sound-integration`: Triggers menu feedback and notification audio from C#.
- `3d-spatial-combat-audio`: Emits positional sounds at agent coordinates in real-time battles.
- `mixer-category-routing`: Directs audio streams to the appropriate engine master channels.
- `sound-latency-optimization`: Selects appropriate compression formats to prevent frame drops in 1,000-agent battles.

### Scope
- **In Scope**: In-game Bannerlord sound effects, Gauntlet UI audio feedback, battle scene combat sound emitters.
- **Out of Scope**: External FMOD Studio multi-track authoring (complex sound banks requiring FMOD designer licenses).

---

## 3. Concrete Game Audio Patterns

### Pattern 1: Sound Manifest XML Schema (`module_sounds.xml`)
Define 2D and 3D sound definitions inside the `<module_sounds>` root element.

```xml
<?xml version="1.0" encoding="utf-8"?>
<module_sounds>
  <!-- 2D UI Action Click Sound -->
  <module_sound name="forge_ui_tactical_click" is_2d="true" sound_category="ui" path="ui_tactical_click.ogg" />

  <!-- 2D Telemetry Alert Sound -->
  <module_sound name="forge_alert_chime" is_2d="true" sound_category="ui" path="alert_chime.ogg" />

  <!-- 3D Combat Shield Block Sound (Positional) -->
  <module_sound name="forge_iron_shield_clash" is_2d="false" sound_category="mission_combat" path="iron_shield_clash.wav" />

  <!-- 3D Catapult Launch Release Sound -->
  <module_sound name="forge_siege_trebuchet_release" is_2d="false" sound_category="mission_combat" path="trebuchet_release.ogg" />
</module_sounds>
```

### Pattern 2: C# Audio Playback
Trigger custom sound events cleanly from C# code.

```csharp
using TaleWorlds.Engine;
using TaleWorlds.Library;

public static class ForgeSoundPlayer
{
    // 1. Play 2D Interface Audio (e.g. Button click or HUD alert)
    public static void PlayUiClick()
    {
        SoundEvent.PlaySound2D("forge_ui_tactical_click");
    }

    // 2. Play 3D Positional Audio at Agent or World Coordinates
    public static void PlayCombatClashAt(Vec3 position)
    {
        int soundEventId = SoundEvent.GetEventIdFromString("forge_iron_shield_clash");
        if (soundEventId >= 0)
        {
            MBSoundEvent.PlaySound(soundEventId, position);
        }
    }
}
```

---

## 4. Sharp Edges & Anti-Patterns

### Edge 1: Misspelling `sound_category`
- **Severity**: HIGH
- **Symptom**: Custom sound files exist and C# code executes without throwing an exception, but no sound plays in-game.
- **Root Cause**: Setting `sound_category="Combat"` or `sound_category="interface"`. TaleWorlds audio mixer expects exact lowercase categories (`ui`, `mission_combat`, `ambient`, `voice`).
- **Fix**: Always specify exact canonical categories in lowercase.

### Edge 2: Appending `.xml` to Path in `SubModule.xml`
- **Severity**: HIGH
- **Symptom**: Game launcher or startup reports `Failed to load XML file: ModuleData/module_sounds.xml.xml`.
- **Root Cause**: Specifying `path="module_sounds.xml"` instead of `path="module_sounds"`. The engine automatically appends `.xml`.
- **Fix**: Omit the `.xml` extension in the `path` attribute of `<XmlName>`.

### Edge 3: Emitting 3D Positional Audio with `is_2d="true"`
- **Severity**: MEDIUM
- **Symptom**: Weapon impact sounds play at full blast directly inside both ear channels, completely ignoring where the hit occurred on the battlefield.
- **Root Cause**: Forgetting to set `is_2d="false"` for spatial sound events.
- **Fix**: Set `is_2d="false"` for any sound that originates at a specific world coordinate.

---

## 5. Validation Rules & Verification Checklist

1. [ ] **Folder Structure**: Audio files are located in `Modules/<ModId>/ModuleSounds/`.
2. [ ] **Manifest Registration**: `SubModule.xml` registers `module_sounds` under `id="Sounds"` without `.xml` in `path`.
3. [ ] **Valid Categories**: All `sound_category` values are strictly one of `ui`, `mission_combat`, `ambient`, or `voice`.
4. [ ] **Format Hygiene**: Combat impact sounds use short uncompressed `.wav` or optimized `.ogg`.
5. [ ] **Thread Safety**: All `SoundEvent.PlaySound2D` calls execute on the main game thread.
