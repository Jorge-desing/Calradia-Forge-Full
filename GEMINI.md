# Bannerlord C# Naming Conventions

- **CRITICAL**: Never name a folder, sub-namespace, or class `Campaign` within this project. Doing so shadows the `TaleWorlds.CampaignSystem.Campaign` class when `using TaleWorlds.CampaignSystem;` is active, breaking compilation for properties like `Campaign.Current`. Use alternatives like `CampaignBehaviors`, `DataExtensions`, or `CampaignExtensions`.

## Cross-Agent Compatibility (Codex & Multi-Agent)
- For OpenAI Codex CLI and universal multi-agent specifications, see `AGENTS.md` and `CODEX.md`.
- For Calradia Forge Gauntlet changes, F10 hotkey behavior, Resource Browser import, and live game review order, follow `.agents/rules/calradia_forge_ui.md`, `.agents/rules/bannerlord_input_debug_agent.md`, and the relevant Bannerlord UI skills. Close the Modding Kit before launching Bannerlord. Before each source correction, close Bannerlord/Modding Kit and end/reset Ordenador; keep them closed through edits/builds and between live checks.
