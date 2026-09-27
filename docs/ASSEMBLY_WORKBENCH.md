# In-game Assembly Workbench

The in-game Assembly Workbench is available from Forge's Extensions section. It lists up to 250 top-level `.dll` files from installed module `bin/Win64_Shipping_Client` folders, excluding TaleWorlds game binaries. Select a row by entering its number; the selected absolute path remains visible in the argument field.

## Inspect

`Inspect metadata` parses a managed PE/.NET file with AsmResolver on a worker thread. It reports identity, version, runtime, IL-only and strong-name status, bounded type/method counts, up to 512 references, file size, and SHA-256. Input is limited to 256 MiB. The file is parsed as data; it is never loaded into the game CLR or executed. Native images, malformed metadata, unsupported formats, and over-limit inputs return an error without blocking the Gauntlet thread.

## Preview and apply a version copy

Enter a four-part version such as `1.2.3.0` in the separate Version field. Run `Preview version` first. Preview creates no files and shows the input, output, backup path, current/requested versions, and input hash. Then use `Apply copy` only when the preview is correct.

Apply writes a separate DLL under `%LocalAppData%\CalradiaForge\AssemblyWorkbench`, creates `<output>.source.bak`, records input and output SHA-256 hashes, writes through a temporary file, then reopens the result to verify its PE metadata and requested version. Existing output/backup paths are not overwritten. The input is not modified. Signed and mixed-mode assemblies are rejected because the transform cannot preserve their signatures or native sections. Cancellation and failed writes remove partial output and temporary files.

This is a narrow assembly-version metadata transform, not an arbitrary IL editor, hot reload system, compatibility verdict, or security scanner. Keep the backup and test any output outside important mod files.

AsmResolver 6.0.1 and its .NET Framework 4.7.2 runtime dependencies are included beside the game module. DocFX is build-time only; the in-game Help action displays short offline summaries derived from SDK XML documentation.

## Desktop Static Assembly & Save System Audit

In the standalone .NET 8 WPF Desktop workbench, AsmResolver also powers deep offline static auditing for compiled `.dll` and `.exe` binaries through the `SaveTypeDefinerAuditor` and `CampaignNamespaceGuard` routes:
- **GEMINI.md Rule A (Anti-Shadowing):** Verifies that neither types nor namespaces shadow `TaleWorlds.CampaignSystem.Campaign` or `TaleWorlds.Localization`.
- **Rule B (Stateless Behavior Contract):** Enforces that classes inheriting from `CampaignBehaviorBase` contain zero `[SaveableField]` or `[SaveableProperty]` attributes.
- **SaveableTypeDefiner Base ID Safety:** Disassembles constructor IL instructions to ensure base IDs are $\ge 2,500,000$, preventing ID collisions with native TaleWorlds types (0–100,000) or third-party mods.
- **Engine Entity MBGUID Direct Serialization:** Detects dangerous direct serialization of engine entities (`Hero`, `MobileParty`, `Settlement`, `Clan`, `Kingdom`) and recommends StringId resolution.
- **Distribution Safety Metadata:** Audits assembly metadata attributes (`Company`, `Product`, `Description`, `Copyright`) to safeguard against false-positive antivirus quarantines.
- **Interactive CLR Security Radar (Rev032):** Visualizes CLR metadata cards (Declared Types, Method Table, Assembly References, StringId Resolution Count), 5 compliance gauge bars, and canonical assembly audit presets cycling via `PresetActionButton`.

