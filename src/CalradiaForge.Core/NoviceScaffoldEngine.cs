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
            const string pageIdPrefix = "your_unique_module_page_prefix";
            string pageId = pageIdPrefix + "." + token;
            string localizationPrefix = pageIdPrefix + "_ui_" + token;
            string titleKey = localizationPrefix + "_title";
            string statusKey = localizationPrefix + "_status";
            string readyKey = localizationPrefix + "_ready";
            string refreshKey = localizationPrefix + "_refresh";
            string closeKey = localizationPrefix + "_close";
            string escapedTitle = EscapeCSharpString("{=" + titleKey + "}" + title);
            string escapedStatus = EscapeCSharpString("{=" + statusKey + "}Ready");
            string escapedReady = EscapeCSharpString("{=" + readyKey + "}Page ready.");
            var output = new System.Text.StringBuilder();

            output.AppendLine(Header("Gauntlet Page Blueprint", title));
            output.AppendLine("Copyable starter only: this generator writes no files and changes no module. Copy the sections below into your module, apply the substitutions, then compile it.");
            output.AppendLine("PAGE ID: Replace every '" + pageIdPrefix + "' marker with one short, globally unique lowercase prefix for your module. Use only a-z, 0-9, and underscores; keep the complete page ID at or below 96 characters. The same prefix is used by the attribute, OpenPage call, and localization IDs.");
            output.AppendLine("OWNER ID: Replace every 'YOUR_EXACT_MODULE_FOLDER_ID' marker with the exact module folder ID that contains both the assembly and GUI/Prefabs folder.");
            output.AppendLine("C# NAMESPACE: 'YourMod.UI' is only a valid-identifier example. Rename it to a valid C# namespace if desired; it is independent of the module folder ID and does not need to match it.");
            output.AppendLine();
            output.AppendLine("SECTION 1/4 — XML PREFAB");
            output.AppendLine("FILE: <YOUR_EXACT_MODULE_FOLDER_ID>/GUI/Prefabs/" + classStem + ".xml");
            output.AppendLine("<Prefab>");
            output.AppendLine("  <Window>");
            output.AppendLine("    <Widget Color=\"#0F1311FF\" HeightSizePolicy=\"Fixed\" HorizontalAlignment=\"Center\" Sprite=\"BlankWhiteSquare_9\" SuggestedHeight=\"420\" SuggestedWidth=\"680\" VerticalAlignment=\"Center\" WidthSizePolicy=\"Fixed\">");
            output.AppendLine("      <Children>");
            output.AppendLine("        <TextWidget Brush=\"GameTip.Title.Text\" HeightSizePolicy=\"Fixed\" MarginLeft=\"24\" MarginTop=\"24\" SuggestedHeight=\"36\" SuggestedWidth=\"620\" Text=\"@TitleText\" WidthSizePolicy=\"Fixed\" />");
            output.AppendLine("        <TextWidget Brush=\"GameTip.Text\" HeightSizePolicy=\"Fixed\" MarginLeft=\"24\" MarginTop=\"84\" SuggestedHeight=\"100\" SuggestedWidth=\"620\" Text=\"@StatusText\" WidthSizePolicy=\"Fixed\" />");
            output.AppendLine("        <ButtonWidget Command.Click=\"ExecuteRefresh\" HeightSizePolicy=\"Fixed\" MarginLeft=\"24\" MarginTop=\"326\" SuggestedHeight=\"42\" SuggestedWidth=\"170\" WidthSizePolicy=\"Fixed\">");
            output.AppendLine("          <Children><TextWidget Brush=\"GameTip.Text\" DoNotAcceptEvents=\"true\" HeightSizePolicy=\"StretchToParent\" HorizontalAlignment=\"Center\" Text=\"{=" + refreshKey + "}Refresh\" VerticalAlignment=\"Center\" WidthSizePolicy=\"StretchToParent\" /></Children>");
            output.AppendLine("        </ButtonWidget>");
            output.AppendLine("        <ButtonWidget Command.Click=\"ExecuteClose\" HeightSizePolicy=\"Fixed\" MarginLeft=\"210\" MarginTop=\"326\" SuggestedHeight=\"42\" SuggestedWidth=\"170\" WidthSizePolicy=\"Fixed\">");
            output.AppendLine("          <Children><TextWidget Brush=\"GameTip.Text\" DoNotAcceptEvents=\"true\" HeightSizePolicy=\"StretchToParent\" HorizontalAlignment=\"Center\" Text=\"{=" + closeKey + "}Close\" VerticalAlignment=\"Center\" WidthSizePolicy=\"StretchToParent\" /></Children>");
            output.AppendLine("        </ButtonWidget>");
            output.AppendLine("      </Children>");
            output.AppendLine("    </Widget>");
            output.AppendLine("  </Window>");
            output.AppendLine("</Prefab>");
            output.AppendLine();
            output.AppendLine("SECTION 2/4 — VIEWMODEL");
            output.AppendLine("FILE: <YOUR_MODULE_SOURCE>/UI/" + viewModelName + ".cs");
            output.AppendLine("using CalradiaForge.Sdk;");
            output.AppendLine("using TaleWorlds.Library;");
            output.AppendLine("using TaleWorlds.Localization;");
            output.AppendLine();
            output.AppendLine("namespace YourMod.UI");
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
            output.AppendLine("                OnPropertyChangedWithValue(value, nameof(StatusText));");
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
            output.AppendLine("SECTION 3/4 — LOCALIZATION");
            output.AppendLine("FILE: <YOUR_EXACT_MODULE_FOLDER_ID>/ModuleData/Languages/EN/strings.xml (merge these entries into the existing English strings resource if present).");
            output.AppendLine("<base type=\"string\">");
            output.AppendLine("  <tags><tag language=\"English\" /></tags>");
            output.AppendLine("  <strings>");
            output.AppendLine("    <string id=\"" + titleKey + "\" text=\"" + title + "\" />");
            output.AppendLine("    <string id=\"" + statusKey + "\" text=\"Ready\" />");
            output.AppendLine("    <string id=\"" + readyKey + "\" text=\"Page ready.\" />");
            output.AppendLine("    <string id=\"" + refreshKey + "\" text=\"Refresh\" />");
            output.AppendLine("    <string id=\"" + closeKey + "\" text=\"Close\" />");
            output.AppendLine("  </strings>");
            output.AppendLine("</base>");
            output.AppendLine();
            output.AppendLine("SECTION 4/4 — SUBMODULE INTEGRATION");
            output.AppendLine("Add using CalradiaForge.Sdk; once to the existing SubModule.cs file.");
            output.AppendLine("In the existing OnSubModuleLoad method, ensure there is one availability registration for this assembly/module:");
            output.AppendLine("    ForgeApi.RegisterWhenAvailable(RegisterForgeUi);");
            output.AppendLine("In the existing OnSubModuleUnloaded method, pair it with:");
            output.AppendLine("    ForgeApi.UnregisterWhenAvailable(RegisterForgeUi);");
            output.AppendLine("    ForgeApi.UnregisterUiPages(\"YOUR_EXACT_MODULE_FOLDER_ID\");");
            output.AppendLine("Add this private method once inside the existing SubModule class:");
            output.AppendLine("    private void RegisterForgeUi(IForgeRegistry registry) => ForgeApi.AutoRegister(typeof(" + viewModelName + ").Assembly, \"YOUR_EXACT_MODULE_FOLDER_ID\");");
            output.AppendLine("If either lifecycle override already exists, merge these calls into its body; do not add duplicate overrides. If this assembly is already auto-registered with the same owner ID, keep one AutoRegister call and reuse its availability callback.");
            output.AppendLine("After the Forge panel is open, call ForgeUI.OpenPage(\"" + pageId + "\") to request this page.");
            output.AppendLine("The Refresh button is a demonstration: it resets the sample status text and does not refresh game data. Close requests closure of the active Forge extension page.");
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
