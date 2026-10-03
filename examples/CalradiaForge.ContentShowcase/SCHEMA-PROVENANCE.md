# Schema provenance

The showcase's XML was checked against the XSDs from the installed Bannerlord directory on 2026-10-01. The local installation root was `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord`; the verifier accepts another root through `--game-root` and reads the files in place without copying proprietary game data into the repository.

| Installed source | Purpose | SHA-256 captured on 2026-10-01 |
| --- | --- | --- |
| `XmlSchemas/Items.xsd` | Item element and `ItemComponent` structure | `E57D50EA6B0C6FD8356F4A26AAB7593B410242C6F4C457E4F1A4226961B9E266` |
| `XmlSchemas/NPCCharacters.xsd` | Inline `EquipmentRoster` and equipment structure | `912DDCB8BECB1315D9A579C81C109C420FD0391FFCF0D8AB53B49DEFE0B2A687` |
| `Modules/SandBoxCore/ModuleData/items/weapons.xml` | Native `horse_whip` mesh, body, weapon class, and usage | `9C1BF93DB03C47895C010595B76BCADD7D9C398E5CFE8BBA42BDF0C8DAA0F4BC` |

The validator compiles the current installed XSDs and checks the generated files on every run. It also compares the sample item against the installed `horse_whip` record and verifies the troop's `Item.cfcs_practice_whip` references a declared item in this module. The BAT validation report prints the current SHA-256 hashes for both XSDs and `weapons.xml`. The documented hashes identify the source observed during authoring; a different local hash should be reviewed as a game-data update, not treated as corruption.

Schema validation establishes XML structure only. It does not establish that the game accepts every gameplay value, loads this module successfully, places the static troop in a campaign, or renders the Gauntlet page correctly. Those require separate engine-level validation.
