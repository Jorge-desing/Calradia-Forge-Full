---
name: bannerlord-dotnet-artisan
description: "Adapt dotnet-artisan .NET skills, patterns, and utilities to Mount & Blade II: Bannerlord and Calradia Forge. Acts as the engine adaptation bridge for using-dotnet, enforcing TaleWorlds lifecycle, save-safety, threading, and performance rules."
---

# Bannerlord + dotnet-artisan Adaptation Bridge

Start with [calradia-forge-dotnet](../calradia-forge-dotnet/SKILL.md), then use this skill to ground implementation in Bannerlord's engine reality. The protected local using-dotnet file is a backup snapshot, not a required step; an installed general .NET skill may supplement the self-contained project guidance. When generic .NET patterns conflict with engine lifecycles, TaleWorlds architecture always takes precedence.

---

## 1. Adaptation Map

| dotnet-artisan Skill / Pattern | Bannerlord & Calradia Forge Adaptation Rules |
|---|---|
| **`using-dotnet`** | `calradia-forge-dotnet` contains the project-required intent routing, TFM, and KISS rules. The protected local file is retained as a source snapshot; the installed `dotnet-artisan:using-dotnet` plugin may supplement general guidance. |
| **`dotnet-csharp`** | Modern C# language syntax (switch expressions, pattern matching, collection expressions) is enabled via `LangVersion=latest`. However, runtime BCL is strictly .NET Framework 4.7.2 for the game module. Main thread execution is mandatory for all Campaign/Mission entity calls. |
| **`dotnet-ui`** | In-game UI must strictly use Gauntlet XML (`bannerlord-gauntlet-ui`) or Mission HUD (`bannerlord-missionview-hud`). Standalone workbench uses WPF (`calradia-forge-desktop`). Never cross UI paradigms. |
| **`dotnet-api`** | For mod IPC, use lightweight asynchronous Named Pipes or `ForgeWeave` contracts. Do not introduce ASP.NET Core or web hosting into the game module. |
| **`dotnet-testing`** | Map tests to the established test harness: `CalradiaForge.Tests` (.NET Framework 4.7.2), `CalradiaForge.Desktop.Tests` (.NET 8), and `CalradiaForge.Desktop.RenderTests` (synchronous STA layout checks). |
| **`dotnet-tooling`** | Respect solution filters (`CalradiaForge.Desktop.slnf`) and test launchers (`tools\Run-CalradiaForge-Tests.bat`). Distinguish module packaging from standalone desktop distribution. |
| **`dotnet-debugging`** | Use crash dump analyzers, `.cfcrash` JSON logs, and non-blocking diagnostic traps. A stack trace indicates symptom evidence; verify engine call sites. |
| **`dotnet-devops`** | Distribution safety: strip `Zone.Identifier` Mark of the Web, exclude development residue (`.bak.*`), exclude first-party TaleWorlds binaries and `.sav` files, and produce versioned release ZIPs. |

---

## 2. Inviolable Engine Constraints (Grounding Rules)

When translating .NET concepts into Bannerlord C#, you MUST enforce these non-negotiable engine rules:

### A. Tick Cost and Allocation Measurement
- **Tick Allocation Budget:** Measure the full hot path before claiming zero allocations. Prefer reusable buffers and explicit loops; avoid repeated LINQ/materialization in measured frame-critical work. Not every hourly callback has the same budget.
- **Modulo-24 Time Slicing:** Use `ForgeTimeSlicer.ShouldProcess(entity.StringId, currentHour)` for stable identity scheduling. Measure collection traversal separately: filtering one bucket still scans the source. Do not delay event-critical actions merely to fit a cadence.
- **Distance Calculations:** Use `DistanceSquared()` instead of `Math.Sqrt()` or `Distance()` to avoid expensive floating-point square root operations.

### B. Anti-Shadowing Namespace Discipline (`GEMINI.md`)
- **CRITICAL:** Never name a namespace, folder, or class `Campaign` within any mod assembly. Doing so shadows `TaleWorlds.CampaignSystem.Campaign` when `using TaleWorlds.CampaignSystem;` is active, breaking compilation of `Campaign.Current`. Use `CampaignBehaviors` or `CampaignExtensions`.
- **Localization Shadowing:** Never name a class or namespace `Localization` — it shadows `TaleWorlds.Localization`. Use `GameLocalization` or `DataExtensions`.

### C. SubModule & Constructor Safety
- **Zero Entity Access in Constructors:** Behavior constructors must never access `Hero.MainHero`, `Campaign.Current`, or game singletons. Engine managers are uninitialized during module loading.
- **Registration Pipeline:** Register behaviors in `MBSubModuleBase.OnGameStart` by casting `IGameStarter` to `CampaignGameStarter`. Use `[AutoRegisterBehavior]` for automatic discovery.

### D. Save System Safety
- **Never Inherit Engine Types for Saving:** Never inherit from `LogEntry` or other native engine types for serialization; if the mod is removed, saves will corrupt.
- **Stateless Behaviors:** Stateless behaviors (such as progression listeners) must keep `SyncData(IDataStore dataStore)` completely empty (`// Stateless`) and must NEVER inherit `SaveableTypeDefiner`.
- **Stateful Base IDs:** If persistence is mandatory, use `SaveableTypeDefiner` with a unique base ID $\ge 2{,}500{,}000$ to avoid colliding with native game systems.

### E. Single-Threaded Engine Reality
- Bannerlord's simulation state is strictly single-threaded. Never dispatch async `Task.Run()` or background thread pool work that reads or modifies `Hero`, `MobileParty`, `Settlement`, or `Clan` objects. Keep all engine operations on the main thread.

### F. Thread-Safe Static Caching & Concurrency Discipline
- Para cachés estáticas y diccionarios de resolución compartidos entre hilos (como `GameLocalization.cs` o índices de solo lectura del SDK): usar `ConcurrentDictionary<TKey, TValue>` con operaciones atómicas (`GetOrAdd`, `TryGetValue`) en lugar de `Dictionary<TKey, TValue>` mutable o locks manuales pesados.
- Garantizar que las colecciones estáticas no retengan referencias duras a entidades del motor del juego para prevenir memory leaks entre recargas de partidas.

### G. Defensive Validation in Developer Console Commands
- Los comandos expuestos al motor mediante `[CommandLineFunctionality.CommandLineArgumentAttribute]` reciben `args == null` si el usuario no especifica argumentos en la consola de depuración.
- Siempre implementar guardias defensivas explícitas: `if (args == null || args.Count < N) return "Usage: ...";` antes de acceder a cualquier índice de la lista.
- Utilizar `TryParse` para todos los argumentos convertibles (números, booleanos, identificadores) con mensajes de error descriptivos.


---

## 3. Domain Skill Routing

From `bannerlord-dotnet-artisan`, route directly to the specific Bannerlord domain skill:

- **Campaign Systems:** [bannerlord-campaign-behavior](../bannerlord-campaign-behavior/SKILL.md), [bannerlord-character-development](../bannerlord-character-development/SKILL.md), [bannerlord-clan-succession](../bannerlord-clan-succession/SKILL.md)
- **Economy & Diplomacy:** [bannerlord-economy-trade](../bannerlord-economy-trade/SKILL.md), [bannerlord-kingdom-diplomacy](../bannerlord-kingdom-diplomacy/SKILL.md)
- **Combat & Missions:** [bannerlord-combat-ai](../bannerlord-combat-ai/SKILL.md), [bannerlord-siege-mechanics](../bannerlord-siege-mechanics/SKILL.md), [bannerlord-missionview-hud](../bannerlord-missionview-hud/SKILL.md)
- **UI & Presentation:** [bannerlord-gauntlet-ui](../bannerlord-gauntlet-ui/SKILL.md), [calradia-forge-desktop](../calradia-forge-desktop/SKILL.md)
- **Core Patterns:** [bannerlord-shared-patterns](../bannerlord-shared-patterns/SKILL.md), [calradia-forge-submodule-lifecycle](../calradia-forge-submodule-lifecycle/SKILL.md)
