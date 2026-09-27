using System;
using CalradiaForge.Sdk;

namespace CalradiaForge.Core
{
    /// <summary>
    /// Orchestrates novice modder scaffold generation.
    /// Bridges ForgeNoviceHub (SDK) with the UI layer (Runtime/PanelViewModel).
    /// Validates and formats output with pedagogical headers.
    /// </summary>
    public static class NoviceScaffoldEngine
    {
        /// <summary>
        /// Generates scaffold output for a given novice tool ID and optional argument.
        /// </summary>
        /// <param name="tool">
        /// One of: "behavior", "troop", "quest", "item", "submodule", "checklist", "events"
        /// </param>
        /// <param name="argument">
        /// Optional argument used to customize the generated scaffold (name, id, class, etc.).
        /// </param>
        /// <returns>A formatted string ready for display in the Gauntlet panel.</returns>
        public static string Generate(string tool, string argument)
        {
            if (string.IsNullOrWhiteSpace(tool)) return "No tool specified.";

            // Parse argument as a modId/name/className hint (simple: use as-is)
            var arg = (argument ?? "").Trim();

            try
            {
                switch (tool.ToLowerInvariant())
                {
                    case "behavior":
                    {
                        string className = string.IsNullOrWhiteSpace(arg) ? "MyForgeBehavior" : Sanitize(arg) + "Behavior";
                        string ns = "MyMod.CampaignBehaviors";
                        return Header("Campaign Behavior Scaffold", className) +
                               ForgeNoviceHub.GenerateBehaviorScaffold(className, ns);
                    }
                    case "troop":
                    {
                        string id = string.IsNullOrWhiteSpace(arg) ? "mymod_soldier" : Sanitize(arg).ToLowerInvariant() + "_soldier";
                        string name = string.IsNullOrWhiteSpace(arg) ? "Forge Soldier" : arg + " Soldier";
                        return Header("Troop XML Template", id) +
                               ForgeNoviceHub.GenerateTroopXml(id, name, "Culture.empire", 2);
                    }
                    case "quest":
                    {
                        string questId = string.IsNullOrWhiteSpace(arg) ? "my_custom_quest" : Sanitize(arg).ToLowerInvariant() + "_quest";
                        string className = string.IsNullOrWhiteSpace(arg) ? "MyCustomQuest" : Sanitize(arg) + "Quest";
                        return Header("QuestBase Scaffold", className) +
                               ForgeNoviceHub.GenerateQuestScaffold(questId, className, "MyMod.Quests", 2500001);
                    }
                    case "item":
                    {
                        string id = string.IsNullOrWhiteSpace(arg) ? "mymod_iron_sword" : Sanitize(arg).ToLowerInvariant() + "_sword";
                        string name = string.IsNullOrWhiteSpace(arg) ? "Forge Iron Sword" : arg + " Sword";
                        return Header("Item XML Template", id) +
                               ForgeNoviceHub.GenerateItemXml(id, name, "Culture.empire", "OneHandedWeapon");
                    }
                    case "submodule":
                    {
                        string modId = string.IsNullOrWhiteSpace(arg) ? "MyNewMod" : Sanitize(arg);
                        return Header("SubModule.xml Template", modId) +
                               ForgeNoviceHub.GenerateSubModuleXml(modId, modId, "1.0.0", modId + ".SubModule");
                    }
                    case "checklist":
                    {
                        return Header("Mod Readiness Checklist", arg) +
                               ForgeNoviceHub.GenerateModChecklist(arg);
                    }
                    case "events":
                    {
                        return Header("Campaign Event Explainer", string.IsNullOrWhiteSpace(arg) ? "Full Catalog" : arg) +
                               ForgeNoviceHub.ExplainCampaignEvent(arg);
                    }
                    case "hint":
                    {
                        string title = string.IsNullOrWhiteSpace(arg) ? "Recruit Volunteers" : arg;
                        return Header("Gauntlet Hint & Tooltip Scaffold", title) +
                               ForgeNoviceHub.GenerateHintScaffold("ButtonWidget", title, "Click to execute action with current troop selection.", "Ctrl+Click");
                    }
                    case "workshop":
                    {
                        string type = string.IsNullOrWhiteSpace(arg) ? "apothecary" : Sanitize(arg).ToLowerInvariant();
                        return Header("Workshop & Enterprise Scaffold", type) +
                               ForgeNoviceHub.GenerateWorkshopScaffold(type, "grain", "medicine", "CustomWorkshopBehavior");
                    }
                    case "party":
                    {
                        string clan = string.IsNullOrWhiteSpace(arg) ? "mountain_raiders" : Sanitize(arg).ToLowerInvariant();
                        return Header("Bandit Party & Spawner Scaffold", clan) +
                               ForgeNoviceHub.GenerateBanditPartyScaffold(clan, clan + "_party", "Culture.empire", 15, 35);
                    }
                    case "building":
                    {
                        string bldgId = string.IsNullOrWhiteSpace(arg) ? "custom_granary_vault" : Sanitize(arg).ToLowerInvariant();
                        string bldgName = string.IsNullOrWhiteSpace(arg) ? "Granary Vault" : arg;
                        return Header("Settlement Building Architect", bldgName) +
                               ForgeNoviceHub.GenerateSettlementBuildingScaffold(bldgId, bldgName, "Settlement.CastleAndTown");
                    }
                    case "combat":
                    {
                        string compName = string.IsNullOrWhiteSpace(arg) ? "BattleShoutAgentComponent" : Sanitize(arg) + "AgentComponent";
                        return Header("Combat AI Component Scaffold", compName) +
                               ForgeNoviceHub.GenerateCombatAiComponentScaffold(compName, compName.Replace("AgentComponent", "") + "MissionLogic", "MyMod.Combat");
                    }
                    default:
                        return $"Unknown novice tool: '{tool}'. Valid tools: behavior, troop, quest, item, submodule, checklist, events, hint, workshop, party, building, combat";
                }
            }
            catch (Exception ex)
            {
                return $"[NoviceScaffoldEngine] Error generating '{tool}': {ex.Message}";
            }
        }

        private static string Header(string toolName, string context)
        {
            var line = new string('─', 58);
            return $"{line}\n CALRADIA FORGE NOVICE HUB — {toolName}\n Context: {context}\n{line}\n\n";
        }

        /// <summary>
        /// Sanitizes a user-provided string into a valid C# identifier or file/XML ID.
        /// Removes spaces, dashes, and invalid chars; converts to PascalCase for class names.
        /// </summary>
        private static string Sanitize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "MyMod";
            var parts = input.Split(new[] { ' ', '_', '-', '.', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new System.Text.StringBuilder();
            foreach (var part in parts)
            {
                if (part.Length == 0) continue;
                // Filter to letters/digits only
                var filtered = new System.Text.StringBuilder();
                foreach (char c in part)
                    if (char.IsLetterOrDigit(c)) filtered.Append(c);
                if (filtered.Length == 0) continue;
                // PascalCase
                sb.Append(char.ToUpperInvariant(filtered[0]));
                sb.Append(filtered.ToString().Substring(1));
            }
            var result = sb.ToString();
            return result.Length == 0 ? "MyMod" : result;
        }
    }
}
