# Calradia Forge Skills Taxonomy

This taxonomy groups workspace-local skills by responsibility and gives routing paths for common tasks. Skill folders stay flat under .agents/skills/ so discovery and existing relative links continue to work.

## Protected local copies

The local using-dotnet, docs-generator, and superpowers folders are preserved source snapshots. They protect project data from upstream updates and must remain intact. Their project-specific rules are consolidated into isolated Calradia Forge skills; these snapshots are backup material, not runtime dependencies.

| Upstream source snapshot | Isolated project skill | Consolidated project content |
|---|---|---|
| using-dotnet | [calradia-forge-dotnet](../.agents/skills/calradia-forge-dotnet/SKILL.md) | Dual-TFM boundaries (`net472` vs `net8.0-windows`), KISS, and routing to Bannerlord or Desktop domain skills. |
| docs-generator | [calradia-forge-docs](../.agents/skills/calradia-forge-docs/SKILL.md) | Documentation orchestration, four specialists, bilingual parity, protected append-only DOCX and SHA-256 integrity, DocFX, and release evidence. |
| superpowers | [calradia-forge-dev-workflow](../.agents/skills/calradia-forge-dev-workflow/SKILL.md) | Six-stage lifecycle: planning, TFM gateway, minimal changes, tests, ModRuleAuditor review, documentation, and packaging only when requested. |

The three project skills are self-contained for their repository responsibilities. Installed upstream plugins may add generic guidance, but the project skills do not require them and project rules take precedence.

## 1. Project architecture and engineering gateways
[calradia-forge-modding](../.agents/skills/calradia-forge-modding/SKILL.md) · [calradia-forge-dev-workflow](../.agents/skills/calradia-forge-dev-workflow/SKILL.md) · [calradia-forge-dotnet](../.agents/skills/calradia-forge-dotnet/SKILL.md) · [calradia-forge-gemini-conventions](../.agents/skills/calradia-forge-gemini-conventions/SKILL.md) · [calradia-forge-submodule-lifecycle](../.agents/skills/calradia-forge-submodule-lifecycle/SKILL.md) · [bannerlord-dotnet-artisan](../.agents/skills/bannerlord-dotnet-artisan/SKILL.md) · [bannerlord-shared-patterns](../.agents/skills/bannerlord-shared-patterns/SKILL.md) · [using-dotnet — protected copy](../.agents/skills/using-dotnet/SKILL.md) · [superpowers — protected copy](../.agents/skills/superpowers/SKILL.md)

## 2. Documentation and delivery evidence
[calradia-forge-docs](../.agents/skills/calradia-forge-docs/SKILL.md) · [calradia-forge-codemaps](../.agents/skills/calradia-forge-codemaps/SKILL.md) · [calradia-forge-registro-mejoras](../.agents/skills/calradia-forge-registro-mejoras/SKILL.md) · [calradia-forge-docfx-pipeline](../.agents/skills/calradia-forge-docfx-pipeline/SKILL.md) · [calradia-forge-release-validation](../.agents/skills/calradia-forge-release-validation/SKILL.md) · [docs-generator — protected copy](../.agents/skills/docs-generator/SKILL.md)

## 3. Desktop, Gauntlet, and asset workflows
[calradia-forge-desktop](../.agents/skills/calradia-forge-desktop/SKILL.md) · [calradia-forge-ui-automation](../.agents/skills/calradia-forge-ui-automation/SKILL.md) · [bannerlord-gauntlet-ui](../.agents/skills/bannerlord-gauntlet-ui/SKILL.md) · [bannerlord-resource-browser](../.agents/skills/bannerlord-resource-browser/SKILL.md) · [bannerlord-fbx-importer](../.agents/skills/bannerlord-fbx-importer/SKILL.md) · [bannerlord-localization](../.agents/skills/bannerlord-localization/SKILL.md) · [bannerlord-map-visuals](../.agents/skills/bannerlord-map-visuals/SKILL.md) · [bannerlord-audio-modding](../.agents/skills/bannerlord-audio-modding/SKILL.md) · [bannerlord-missionview-hud](../.agents/skills/bannerlord-missionview-hud/SKILL.md) · [game-ui-design](../.agents/skills/game-ui-design/SKILL.md) · [game-audio](../.agents/skills/game-audio/SKILL.md)

## 4. Campaign and simulation
[bannerlord-campaign-behavior](../.agents/skills/bannerlord-campaign-behavior/SKILL.md) · [bannerlord-gamemodels](../.agents/skills/bannerlord-gamemodels/SKILL.md) · [bannerlord-clan-succession](../.agents/skills/bannerlord-clan-succession/SKILL.md) · [bannerlord-character-development](../.agents/skills/bannerlord-character-development/SKILL.md) · [bannerlord-crime-underworld](../.agents/skills/bannerlord-crime-underworld/SKILL.md) · [bannerlord-economy-trade](../.agents/skills/bannerlord-economy-trade/SKILL.md) · [bannerlord-kingdom-diplomacy](../.agents/skills/bannerlord-kingdom-diplomacy/SKILL.md) · [bannerlord-party-spawner](../.agents/skills/bannerlord-party-spawner/SKILL.md) · [bannerlord-quest-system](../.agents/skills/bannerlord-quest-system/SKILL.md) · [bannerlord-settlement-rebellion](../.agents/skills/bannerlord-settlement-rebellion/SKILL.md) · [bannerlord-troop-character](../.agents/skills/bannerlord-troop-character/SKILL.md) · [bannerlord-inventory-barter](../.agents/skills/bannerlord-inventory-barter/SKILL.md) · [discrete-event-simulation](../.agents/skills/discrete-event-simulation/SKILL.md)

## 5. Combat and mission systems
[bannerlord-combat-ai](../.agents/skills/bannerlord-combat-ai/SKILL.md) · [bannerlord-siege-mechanics](../.agents/skills/bannerlord-siege-mechanics/SKILL.md) · [game-ai-behavior-trees](../.agents/skills/game-ai-behavior-trees/SKILL.md)

## 6. Cross-cutting methods and tools
[agent-memory-systems](../.agents/skills/agent-memory-systems/SKILL.md) · [api-builder](../.agents/skills/api-builder/SKILL.md) · [browser-automation](../.agents/skills/browser-automation/SKILL.md) · [code-reviewer](../.agents/skills/code-reviewer/SKILL.md) · [debugging-master](../.agents/skills/debugging-master/SKILL.md) · [frontend-expert](../.agents/skills/frontend-expert/SKILL.md) · [multi-agent-orchestration](../.agents/skills/multi-agent-orchestration/SKILL.md) · [performance-hunter](../.agents/skills/performance-hunter/SKILL.md) · [ponytail](../.agents/skills/ponytail/SKILL.md) · [test-architect](../.agents/skills/test-architect/SKILL.md) · [uiux-designer](../.agents/skills/uiux-designer/SKILL.md)

## Routing
- Game-module C#: start with calradia-forge-dotnet, then bannerlord-dotnet-artisan and the domain skill.
- Performance optimization & zero-allocation profiling: consult performance-hunter.
- Native crash diagnosis (0xC0000005) & scientific debugging: consult debugging-master.
- Tactical combat AI & behavior trees: consult game-ai-behavior-trees and bannerlord-combat-ai.
- Campaign time-slicing & discrete simulation: consult discrete-event-simulation and bannerlord-campaign-behavior.
- Desktop WPF & Gauntlet UI layout/styling: start with calradia-forge-desktop and game-ui-design.
- Custom engine audio & sound categories: consult game-audio and bannerlord-audio-modding.
- Verification & multi-stage testing architecture: consult test-architect and calradia-forge-dev-workflow.
- Autonomous multi-agent coordination & token compaction: consult multi-agent-orchestration, AGENTS.md, and CODEX.md.
- Docs or release evidence: start with calradia-forge-docs and add its specialist.
- General implementation: start with calradia-forge-dev-workflow; its six project phases include the applicable quality gates.

Before retiring any skill, audit its unique project knowledge, inbound links, triggers, scripts, agent references, and packaging references. This reorganization preserves the protected local copies.
