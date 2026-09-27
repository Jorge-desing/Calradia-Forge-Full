---
name: calradia-forge-gemini-conventions
description: Enforces Calradia Forge GEMINI.md naming rules (Campaign/Localization shadowing) and ModRuleAuditor checks in this repo. Use before adding namespaces, folders, or C# types under src/CalradiaForge.Mod.
---

# Calradia Forge — GEMINI naming (this repo)

## Source of truth

- Root: `GEMINI.md` — **never** use folder, sub-namespace, or **class** named `Campaign` under the mod project; it shadows `TaleWorlds.CampaignSystem.Campaign` when `using TaleWorlds.CampaignSystem;` is active.
- Automated mirror: `CalradiaForge.Core.ModRuleAuditor` rule `GEMINI_CAMPAIGN_SHADOWING` (regex on `namespace …\.Campaign` in any `.cs` under a mod directory).
- Acceptance scripts: `tools/verify_stateless_behavior.ps1` / `.py` scan `src/CalradiaForge.Mod` for a directory named `Campaign` and bad namespace patterns.

## Approved layout (reference implementation)

| Instead of | Use |
|------------|-----|
| `CalradiaForge.Mod.Campaign.*` | `CalradiaForge.Mod.CampaignBehaviors` (see `ClanCharacterProgressionBehavior.cs`) |
| `Campaign/` folder | `CampaignBehaviors/`, `DataExtensions/` |

`PROJECT.md` documents the same contract for the stateless clan behavior milestone.

## Second shadowing rule (Forge-specific)

From `.agents/skills/calradia-forge-modding` (do not duplicate here in depth): never name a namespace or class **`Localization`** — it shadows `TaleWorlds.Localization`. This repo uses `GameLocalization` in `src/CalradiaForge.Mod/GameLocalization.cs`.

## When editing

1. Grep the mod tree before creating paths: `src/CalradiaForge.Mod/**`.
2. Prefer `CampaignBehaviors`, `CampaignExtensions` (in **Core**: `CalradiaForge.Core.CampaignExtensions`), or `DataExtensions`.
3. After changes, run `dotnet build CalradiaForge.sln -c Release` and/or `tools/verify_stateless_behavior.ps1`.
4. For third-party mod folders under audit, call `ModRuleAuditor.Audit(modRoot)` — findings surface as `RuleFinding` with `RuleId`, `Severity`, `FilePath`.

## Test hooks

`tests/CalradiaForge.Tests/ClanCharacterProgressionTests.cs` includes `TestAntiShadowingRule` — extend similar checks if you add new mod assemblies that must comply with GEMINI.

## Do not

- Introduce Harmony or manual patching to “fix” naming collisions.
- Add a type named `Campaign` even as a nested class in `CalradiaForge.Mod`.
