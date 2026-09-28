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
                    case "gauntlet-page":
                    {
                        return GenerateGauntletPageScaffold(arg);
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
                        return $"Unknown novice tool: '{tool}'. Valid tools: behavior, troop, quest, item, submodule, checklist, events, hint, gauntlet-page, workshop, party, building, combat";
                }
            }
            catch (Exception ex)
            {
                return $"[NoviceScaffoldEngine] Error generating '{tool}': {ex.Message}";
            }
        }

        private static string GenerateGauntletPageScaffold(string argument)
        {
            string title = CreateGauntletPageTitle(argument);
            string pageToken = Sanitize(title);
            if (pageToken.Length > 48) pageToken = pageToken.Substring(0, 48);

            string token = pageToken.ToLowerInvariant();
            string classStem = "Forge" + pageToken + "Page";
            string viewModelName = classStem + "ViewModel";
            string pageId = "my_mod." + token;
            string titleKey = "my_mod_ui_" + token + "_title";
            string statusKey = "my_mod_ui_" + token + "_status";
            string readyKey = "my_mod_ui_" + token + "_ready";
            string escapedTitle = EscapeCSharpString("{=" + titleKey + "}" + title);
            string escapedStatus = EscapeCSharpString("{=" + statusKey + "}Ready");
            string escapedReady = EscapeCSharpString("{=" + readyKey + "}Page ready.");
            var output = new System.Text.StringBuilder();

            output.AppendLine(Header("Gauntlet Page Blueprint", title));
            output.AppendLine("This copyable starter keeps the page in your module. Forge validates its owner folder, prefab, ViewModel, and Command.Click bindings when the page is registered.");
            output.AppendLine("No files are written. Copy each section into the indicated path, replace MyMod with your module folder ID, and compile your module.");
            output.AppendLine();
            output.AppendLine("FILE: <YourModule>/GUI/Prefabs/" + classStem + ".xml");
            output.AppendLine("<Prefab>");
            output.AppendLine("  <Window>");
            output.AppendLine("    <Widget Color=\"#0F1311FF\" HeightSizePolicy=\"Fixed\" HorizontalAlignment=\"Center\" Sprite=\"BlankWhiteSquare_9\" SuggestedHeight=\"420\" SuggestedWidth=\"680\" VerticalAlignment=\"Center\" WidthSizePolicy=\"Fixed\">");
            output.AppendLine("      <Children>");
            output.AppendLine("        <TextWidget Brush=\"GameTip.Title.Text\" HeightSizePolicy=\"Fixed\" MarginLeft=\"24\" MarginTop=\"24\" SuggestedHeight=\"36\" SuggestedWidth=\"620\" Text=\"@TitleText\" WidthSizePolicy=\"Fixed\" />");
            output.AppendLine("        <TextWidget Brush=\"GameTip.Text\" HeightSizePolicy=\"Fixed\" MarginLeft=\"24\" MarginTop=\"84\" SuggestedHeight=\"100\" SuggestedWidth=\"620\" Text=\"@StatusText\" WidthSizePolicy=\"Fixed\" />");
            output.AppendLine("        <ButtonWidget Command.Click=\"ExecuteRefresh\" HeightSizePolicy=\"Fixed\" MarginLeft=\"24\" MarginTop=\"326\" SuggestedHeight=\"42\" SuggestedWidth=\"170\" WidthSizePolicy=\"Fixed\">");
            output.AppendLine("          <Children><TextWidget Brush=\"GameTip.Text\" DoNotAcceptEvents=\"true\" HeightSizePolicy=\"StretchToParent\" HorizontalAlignment=\"Center\" Text=\"{=my_mod_ui_refresh}Refresh\" VerticalAlignment=\"Center\" WidthSizePolicy=\"StretchToParent\" /></Children>");
            output.AppendLine("        </ButtonWidget>");
            output.AppendLine("        <ButtonWidget Command.Click=\"ExecuteClose\" HeightSizePolicy=\"Fixed\" MarginLeft=\"210\" MarginTop=\"326\" SuggestedHeight=\"42\" SuggestedWidth=\"170\" WidthSizePolicy=\"Fixed\">");
            output.AppendLine("          <Children><TextWidget Brush=\"GameTip.Text\" DoNotAcceptEvents=\"true\" HeightSizePolicy=\"StretchToParent\" HorizontalAlignment=\"Center\" Text=\"{=my_mod_ui_close}Close\" VerticalAlignment=\"Center\" WidthSizePolicy=\"StretchToParent\" /></Children>");
            output.AppendLine("        </ButtonWidget>");
            output.AppendLine("      </Children>");
            output.AppendLine("    </Widget>");
            output.AppendLine("  </Window>");
            output.AppendLine("</Prefab>");
            output.AppendLine();
            output.AppendLine("FILE: <YourModule>/src/UI/" + viewModelName + ".cs");
            output.AppendLine("using CalradiaForge.Sdk;");
            output.AppendLine("using TaleWorlds.Library;");
            output.AppendLine("using TaleWorlds.Localization;");
            output.AppendLine();
            output.AppendLine("namespace MyMod.UI");
            output.AppendLine("{");
            output.AppendLine("    [ForgeUiPage(\"" + pageId + "\", \"" + classStem + "\", \"" + titleKey + "\", Context = Context.Any)]");
            output.AppendLine("    public sealed class " + viewModelName + " : ViewModel");
            output.AppendLine("    {");
            output.AppendLine("        private string _statusText = new TextObject(\"" + escapedStatus + "\").ToString();");
            output.AppendLine("        private readonly string _titleText = new TextObject(\"" + escapedTitle + "\").ToString();");
            output.AppendLine("        private readonly string _readyText = new TextObject(\"" + escapedReady + "\").ToString();");
            output.AppendLine();
            output.AppendLine("        [DataSourceProperty]");
            output.AppendLine("        public string TitleText => _titleText;");
            output.AppendLine();
            output.AppendLine("        [DataSourceProperty]");
            output.AppendLine("        public string StatusText");
            output.AppendLine("        {");
            output.AppendLine("            get => _statusText;");
            output.AppendLine("            set");
            output.AppendLine("            {");
            output.AppendLine("                if (_statusText == value) return;");
            output.AppendLine("                _statusText = value;");
            output.AppendLine("                OnPropertyChangedWithValue(value);");
            output.AppendLine("            }");
            output.AppendLine("        }");
            output.AppendLine();
            output.AppendLine("        [ForgeUiCommand(\"refresh\", nameof(ExecuteRefresh), Context = Context.Any, ChangesState = false)]");
            output.AppendLine("        public void ExecuteRefresh() => StatusText = _readyText;");
            output.AppendLine();
            output.AppendLine("        [ForgeUiCommand(\"close\", nameof(ExecuteClose), Context = Context.Any, ChangesState = false)]");
            output.AppendLine("        public void ExecuteClose() => ForgeUI.ClosePage();");
            output.AppendLine("    }");
            output.AppendLine("}");
            output.AppendLine();
            output.AppendLine("ADD TO YOUR EXISTING SUBMODULE: register after Forge becomes available and remove this owner's pages on unload.");
            output.AppendLine("Add using CalradiaForge.Sdk; to the SubModule.cs file.");
            output.AppendLine("protected override void OnSubModuleLoad() => ForgeApi.RegisterWhenAvailable(RegisterForgeUi);");
            output.AppendLine("protected override void OnSubModuleUnloaded()");
            output.AppendLine("{");
            output.AppendLine("    ForgeApi.UnregisterWhenAvailable(RegisterForgeUi);");
            output.AppendLine("    ForgeApi.UnregisterUiPages(\"MyMod\");");
            output.AppendLine("}");
            output.AppendLine("private void RegisterForgeUi(IForgeRegistry registry) => ForgeApi.AutoRegister(typeof(" + viewModelName + ").Assembly, \"MyMod\");");
            output.AppendLine("Call ForgeUI.OpenPage(\"" + pageId + "\") from your mod after the Forge panel is open.");
            output.AppendLine("The module ID passed to AutoRegister/UnregisterUiPages must exactly match the module folder containing the assembly and GUI/Prefabs folder.");
            return output.ToString();
        }

        private static string CreateGauntletPageTitle(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Tools";

            var title = new System.Text.StringBuilder();
            bool pendingSpace = false;
            string trimmed = value.Trim();
            int inputLimit = Math.Min(trimmed.Length, 256);
            for (int i = 0; i < inputLimit && title.Length < 48; i++)
            {
                char character = trimmed[i];
                if (char.IsLetterOrDigit(character))
                {
                    if (pendingSpace && title.Length > 0)
                    {
                        if (title.Length >= 47) break;
                        title.Append(' ');
                    }

                    if (title.Length >= 48) break;
                    title.Append(character);
                    pendingSpace = false;
                }
                else
                {
                    pendingSpace = true;
                }
            }

            return title.Length == 0 ? "Tools" : title.ToString();
        }

        private static string EscapeCSharpString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
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
