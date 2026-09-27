---
name: calradia-forge-dotnet
description: Calradia Forge gateway for C# and MSBuild work. Selects the correct TFM and project constraints, then pairs general dotnet-artisan guidance with Bannerlord or Desktop domain skills.
metadata:
  version: "1.0.0"
  author: "calradia-forge-team"
  category: "bannerlord-foundation"
  hub_skill: "calradia-forge-dotnet"
  related_skills: ["bannerlord-dotnet-artisan", "bannerlord-shared-patterns", "calradia-forge-modding", "calradia-forge-desktop"]
---

# Calradia Forge .NET Gateway & Architecture Discipline

Use this dedicated workspace skill as the **first gateway** whenever modifying, adding, or refactoring C# files, project definitions (`.csproj`), solution filters (`.slnf`), or MSBuild configurations in Calradia Forge.

This isolated project skill contains Calradia Forge's required .NET rules, including Dual-TFM boundaries, KISS, and domain routing. It does not depend on an upstream skill being installed or remaining unchanged. The protected local [`using-dotnet`](../using-dotnet/SKILL.md) snapshot remains as a backup of the source knowledge; installed `dotnet-artisan` guidance may supplement general .NET advice, but project constraints here take precedence.

---

## 1. Intent Detection & Trigger Conditions

Trigger this skill whenever a task involves:
- Adding, editing, or refactoring C# source files (`*.cs`).
- Modifying solution files (`CalradiaForge.sln`, `CalradiaForge.Desktop.slnf`) or project files (`*.csproj`, `Directory.Build.props`).
- Designing new services, behaviors, models, or UI view models in C#.
- Diagnosing C# compiler errors (`CS*`), runtime exceptions, or test failures.

---

## 2. Simplicity First (KISS) & Anti-Over-Engineering

Adhere strictly to KISS principles in all C# development:

1. **Do the direct thing:**
   - If you need a file or asset, create it directly. Do not build speculative runtime generators.
   - If you need data, put it where it belongs. Do not assemble it from embedded strings or complex reflection if direct structures work.
2. **Write readable code over clever code:**
   - Prefer explicit `if`/`else` over deeply nested ternary operators.
   - Prefer clear loops over dense, hard-to-debug LINQ chains (especially in performance-critical code).
   - Leverage modern C# syntax (`[..]`, switch expressions, pattern matching, raw string literals) for conciseness and clarity. Avoid convoluted logic.
3. **Don't add what wasn't asked for:**
   - No speculative config options, no unrequested refactorings, no preemptive error handling for impossible states.
   - Do not add XML documentation to existing unrelated methods.
4. **Earn every abstraction:**
   - Do NOT create `IFooService` if there is only ever one `FooService`.
   - Do NOT extract a helper function for an operation that occurs only once. Extract only when a genuine pattern emerges across 3+ distinct call sites.
5. **Fewer files, fewer layers:**
   - Do not create a Controller + Service + Repository + DTO + Mapper pipeline for a simple 30-line calculation.
   - Scale the architectural pattern to the exact scope of the problem.

---

## 3. Dual-TFM Architecture Awareness

Projects in this repository span two distinct .NET target frameworks. You MUST determine the target framework before recommending or implementing any API:

| Project Area | Target Framework | Language / Runtime Rules |
| :--- | :--- | :--- |
| **Game Module (`CalradiaForge.Mod`)**, **Core (`CalradiaForge.Core`)**, **SDK (`CalradiaForge.Sdk`)** | `net472` (.NET Framework 4.7.2) | `LangVersion` is set to `latest` (modern syntax supported), but BCL APIs are strictly limited to .NET Framework 4.7.2. Do NOT use modern BCL types (e.g. `TimeProvider`, `DateOnly`, newer `System.Text.Json` features unless explicitly referenced). TaleWorlds engine calls MUST remain on the main game thread. |
| **Desktop Workbench (`CalradiaForge.Desktop`)** & **Render Tests** | `net8.0-windows` (.NET 8 WPF) | Full modern .NET 8 BCL available (async named pipes, `System.Text.Json`, `VirtualizingStackPanel`). Runs outside the game process. No TaleWorlds assemblies allowed. |
| **Standalone Helpers (`BannerlordFbxImporter`)** | `net8.0-windows` | Separate experimental tool; keep distinct from game module and Desktop workbench. |

---

## 4. First-Step Routing Discipline

Before generating, planning, or editing any C# code, follow this sequence:

```
[C# / .NET Intent Detected]
          │
          ▼
┌─────────────────────────────────┐
│   calradia-forge-dotnet         │ ── Identify TFM (net472 vs net8.0-windows)
└────────────────┬────────────────┘
                 │
                 ├── Game Module / Core / SDK (net472)
                 │         │
                 │         ▼
                 │   ┌─────────────────────────────────┐
                 │   │    bannerlord-dotnet-artisan    │ ── Single-threaded, zero-allocation, save safety
                 │   └────────────────┬────────────────┘
                 │                    │
                 │                    ▼
                 │   ┌─────────────────────────────────┐
                 │   │   bannerlord-shared-patterns    │ ── Decorator Pattern, CampaignBehaviorBase
                 │   └────────────────┬────────────────┘
                 │                    │
                 │                    ▼
                 │            [Domain Skill] (e.g. bannerlord-clan-succession)
                 │
                 └── Desktop Workbench (net8.0-windows)
                           │
                           ▼
                     ┌─────────────────────────────────┐
                     │     calradia-forge-desktop      │ ── WPF MVVM, themes, localization, IPC
                     └────────────────┬────────────────┘
                                      │
                                      ▼
                     ┌─────────────────────────────────┐
                     │   calradia-forge-ui-automation  │ ── Bounded read-only native-window checks
                           └─────────────────────────────────┘
```

---

## 5. Verification Commands

Always verify changes using the official build and stateless verification pipeline:
```powershell
# 1. Release build
dotnet build CalradiaForge.sln -c Release

# 2. Acceptance & statelessness verification
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\verify_stateless_behavior.ps1

# 3. Core regression tests
tools\Run-CalradiaForge-Core-Tests.bat <nul
```
