# BRIEFING — 2026-09-20T21:10:55Z

## Mission
Investigate CalradiaForge codebase structure, solution, projects, dependencies, SubModule, CampaignBehaviors, packaging, and provide findings for mod development.

## 🔒 My Identity
- Archetype: explorer
- Roles: read-only investigation, codebase architecture analysis
- Working directory: c:\Users\Alex\Documents\Mod Desarrolladores\.agents\explorer_1
- Original parent: 0e802032-b7a4-434c-94c6-7b8e94b19697
- Milestone: codebase-investigation

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- BANNERLORD ANTI-SHADOWING: Never name a folder, sub-namespace, or class `Campaign`
- Write only to `.agents/explorer_1/`

## Current Parent
- Conversation ID: 0e802032-b7a4-434c-94c6-7b8e94b19697
- Updated: 2026-09-20T21:08:14Z

## Investigation State
- **Explored paths**:
  - `ORIGINAL_REQUEST.md` (authoritative instructions and acceptance criteria)
  - `CalradiaForge.sln` and all project files (`CalradiaForge.Mod.csproj`, `CalradiaForge.Core.csproj`, etc.)
  - `src/CalradiaForge.Mod/SubModule.cs` (lifecycle hooks, `OnGameStart`, `OnCampaignStart`)
  - `src/CalradiaForge.Mod/CampaignBehaviors/DataBehavior.cs`
  - `src/CalradiaForge.Core/CampaignExtensions/ForgeBehaviorLoader.cs` and `AutoRegisterBehaviorAttribute.cs`
  - Native `TaleWorlds.CampaignSystem.CampaignEvents` via `tools/ApiProbe` reflection against installed game binaries
  - Tests (`CalradiaForge.Tests`, `CalradiaForge.Desktop.Tests`)
  - Build and packaging scripts (`tools/build.ps1`, `tools/package.ps1`)
- **Key findings**:
  - `CalradiaForge.Mod.csproj` targets `net472` and references `$(GameBin)\TaleWorlds*.dll`.
  - SubModule registers behaviors in `OnGameStart` via `ForgeBehaviorLoader.RegisterAll` and direct `campaignStarter.AddBehavior(...)`.
  - Anti-shadowing: `CalradiaForge.Mod.CampaignBehaviors` is canonical and safe.
  - Stateless requirement: `SyncData` must not serialize custom data; zero `SaveableTypeDefiner` instances.
  - 30+ specific `CampaignEvents` verified with exact method and delegate signatures.
- **Unexplored areas**: None for codebase investigation scope.

## Key Decisions Made
- Recommending class `DynasticProgressionBehavior : CampaignBehaviorBase` in `src/CalradiaForge.Mod/CampaignBehaviors/DynasticProgressionBehavior.cs`.
- Recommending namespace `CalradiaForge.Mod.CampaignBehaviors` with `[AutoRegisterBehavior]` attribute.
- Recommending registration directly in `SubModule.OnGameStart` via `campaignStarter.AddBehavior(new CalradiaForge.Mod.CampaignBehaviors.DynasticProgressionBehavior());`.
- Verified compilation and test runners (182/182 tests pass).

## Artifact Index
- DISPATCH.md — record of incoming dispatch messages
- BRIEFING.md — persistent working memory
- progress.md — liveness heartbeat
- handoff.md — 5-component final handoff report
