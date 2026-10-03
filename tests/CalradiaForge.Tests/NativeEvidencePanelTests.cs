using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace CalradiaForge.Tests
{
    internal static class NativeEvidencePanelTests
    {
        public static void Run(Action<string, Action> test)
        {
            test("Native focus preserves the selected evidence, page and argument", Focus);
            test("Native briefing bindings resolve to attributed compiled properties", Bindings);
            test("Native briefing decorations cannot intercept buttons", Decorations);
            test("Native panel exposes enriched category metadata and curated commands", CategoryEnrichment);
            test("Native panel supports modder role preset cycling and command pinning", ModderRoleCustomization);
            test("Native periodic-work guidance uses stable buckets and localized deferral limits", StableTimeSlicingGuidance);
        }

        static Type PanelType => ClanCharacterProgressionTests.LoadModAssembly().GetType("CalradiaForge.Mod.PanelViewModel", true);
        static XDocument Prefab => XDocument.Load("modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml");
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        static void Focus()
        {
            var type = PanelType;
            // Bypass game session construction. Focus must be a local presentation operation only.
            var panel = FormatterServices.GetUninitializedObject(type);
            foreach (var pair in new[] { new[] { "content", "Hero_1 evidence" }, new[] { "full", "Hero_1 evidence" }, new[] { "argument", "Hero_1" }, new[] { "current", "inspect" } })
                type.GetField(pair[0], BindingFlags.Instance | BindingFlags.NonPublic).SetValue(panel, pair[1]);
            type.GetField("page", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(panel, 2);
            var toggle = type.GetMethod("ExecuteToggleEvidenceFocus");
            var normalHeight = (float)type.GetProperty("EvidenceHeight").GetValue(panel);
            toggle.Invoke(panel, null);
            Check(!(bool)type.GetProperty("ShowCommandDeck").GetValue(panel), "Focus must hide the command deck.");
            Check((float)type.GetProperty("EvidenceHeight").GetValue(panel) > normalHeight, "Focus must expand the evidence viewport.");
            Check((float)type.GetProperty("EvidenceHeight").GetValue(panel) + (float)type.GetProperty("EvidenceTop").GetValue(panel) == 702f, "Focus must keep the lower viewport edge above the action rows.");
            toggle.Invoke(panel, null);
            Check((bool)type.GetProperty("ShowCommandDeck").GetValue(panel), "Tools must return after toggling focus off.");
            Check((string)type.GetProperty("Content").GetValue(panel) == "Hero_1 evidence", "Focus changed evidence.");
            Check((string)type.GetProperty("Argument").GetValue(panel) == "Hero_1", "Focus changed argument.");
            Check((int)type.GetField("page", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel) == 2, "Focus changed page.");
        }
        static void Bindings()
        {
            var type = PanelType;
            foreach (var attribute in Prefab.Descendants().Attributes().Where(a => a.Value.StartsWith("@")))
            {
                var ownerType = ResolveBindingOwner(attribute.Parent, type);
                var property = ownerType.GetProperty(attribute.Value.Substring(1));
                Check(property != null, "Missing property: " + attribute.Value);
                Check(property.GetCustomAttributesData().Any(a => a.AttributeType.Name == "DataSourceProperty"), "Missing native binding attribute: " + property.Name);
            }
            foreach (var command in Prefab.Descendants().Attributes("Command.Click"))
            {
                var ownerType = ResolveBindingOwner(command.Parent, type);
                Check(ownerType.GetMethod(command.Value) != null, "Missing command: " + command.Value + " on " + ownerType.Name);
            }
        }

        static Type ResolveBindingOwner(XElement widget, Type panelType)
        {
            Type contextType = panelType;
            var templates = widget?.AncestorsAndSelf()
                .Where(element => element.Name.LocalName == "ItemTemplate")
                .Reverse();
            if (templates == null) return contextType;

            foreach (var itemTemplate in templates)
            {
                var sourceOwner = itemTemplate.Parent?.Attribute("DataSource") != null
                    ? itemTemplate.Parent
                    : itemTemplate.Ancestors().FirstOrDefault(element => element.Attribute("DataSource") != null);
                var dataSource = (string)sourceOwner?.Attribute("DataSource");
                if (string.IsNullOrWhiteSpace(dataSource) || dataSource.Length < 3 || dataSource[0] != '{' || dataSource[dataSource.Length - 1] != '}')
                    throw new InvalidOperationException("ItemTemplate must be nested under a ListPanel with a bound collection.");

                var collectionProperty = contextType.GetProperty(dataSource.Substring(1, dataSource.Length - 2));
                var itemType = collectionProperty?.PropertyType.GetGenericArguments().FirstOrDefault();
                if (itemType == null)
                    throw new InvalidOperationException("Could not resolve the item ViewModel for " + dataSource + " on " + contextType.Name + ".");
                contextType = itemType;
            }
            return contextType;
        }
        static void Decorations()
        {
            var cards = Prefab.Descendants().Where(e => ((string)e.Attribute("Id") ?? "").StartsWith("Briefing")).ToArray();
            Check(cards.Length == 4, "Expected four tactical briefing cards.");
            foreach (var card in cards)
                Check((string)card.Attribute("DoNotAcceptEvents") == "true" && (string)card.Attribute("DoNotPassEventsToChildren") == "true", "Decorative card intercepts input.");
            var argumentEditors = Prefab.Descendants("EditableTextWidget")
                .Where(widget => (string)widget.Attribute("Text") == "@Argument").ToArray();
            Check(argumentEditors.Length > 0 && argumentEditors.All(widget => (string)widget.Attribute("UpdateTextOnTyping") == "true"),
                "Argument editors must update while typing.");
            Check(argumentEditors.Select(widget => (string)widget.Attribute("Id")).Distinct().Count() == argumentEditors.Length,
                "Each editable argument binding must have a unique widget ID.");
        }

        static void CategoryEnrichment()
        {
            var type = PanelType;
            var panel = FormatterServices.GetUninitializedObject(type);
            var cmdListField = type.GetField("_categorySuggestedCommands", BindingFlags.Instance | BindingFlags.NonPublic);
            var pinnedField = type.GetField("_pinnedCommands", BindingFlags.Instance | BindingFlags.NonPublic);
            var cmdListType = cmdListField.FieldType;
            cmdListField.SetValue(panel, Activator.CreateInstance(cmdListType));
            pinnedField.SetValue(panel, Activator.CreateInstance(cmdListType));

            var categories = new[] { "overview", "inspector", "toolkit", "weave", "simulate", "audit", "novice", "sdk" };
            var rebuildMethod = type.GetMethod("RebuildCategoryCommands", BindingFlags.Instance | BindingFlags.NonPublic);
            var descProp = type.GetProperty("CategoryMissionDescription");
            var rulesProp = type.GetProperty("CategoryEngineRules");
            var cmdsProp = type.GetProperty("CategorySuggestedCommands");

            foreach (var cat in categories)
            {
                type.GetField("currentCategory", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(panel, cat);
                rebuildMethod.Invoke(panel, null);

                var desc = (string)descProp.GetValue(panel);
                var rules = (string)rulesProp.GetValue(panel);
                var cmds = (System.Collections.IList)cmdsProp.GetValue(panel);

                Check(!string.IsNullOrWhiteSpace(desc), "Category description missing for " + cat);
                Check(!string.IsNullOrWhiteSpace(rules), "Category engine rules missing for " + cat);
                Check(cmds != null && cmds.Count >= 3, "Category " + cat + " must have at least 3 curated commands");

                foreach (var cmdItem in cmds)
                {
                    var cmdItemType = cmdItem.GetType();
                    var cmdText = (string)cmdItemType.GetProperty("CommandText").GetValue(cmdItem);
                    var cmdDesc = (string)cmdItemType.GetProperty("Description").GetValue(cmdItem);
                    var cmdHint = (string)cmdItemType.GetProperty("HintText").GetValue(cmdItem);
                    Check(!string.IsNullOrWhiteSpace(cmdText), "CommandText must not be empty in " + cat);
                    Check(!string.IsNullOrWhiteSpace(cmdDesc), "Description must not be empty in " + cat);
                    Check(!string.IsNullOrWhiteSpace(cmdHint), "HintText must not be empty in " + cat);
                }
            }
        }

        static void ModderRoleCustomization()
        {
            var type = PanelType;
            var panel = FormatterServices.GetUninitializedObject(type);
            var cmdListField = type.GetField("_categorySuggestedCommands", BindingFlags.Instance | BindingFlags.NonPublic);
            var pinnedField = type.GetField("_pinnedCommands", BindingFlags.Instance | BindingFlags.NonPublic);
            var cmdListType = cmdListField.FieldType;
            cmdListField.SetValue(panel, Activator.CreateInstance(cmdListType));
            pinnedField.SetValue(panel, Activator.CreateInstance(cmdListType));
            type.GetField("currentCategory", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(panel, "overview");

            var cycleMethod = type.GetMethod("ExecuteCycleModderRole");
            var roleLabelProp = type.GetProperty("ActiveModderRoleLabel");
            var roleHintProp = type.GetProperty("ActiveModderRoleHint");
            var initialLabel = (string)roleLabelProp.GetValue(panel);
            Check(!string.IsNullOrWhiteSpace(initialLabel), "Initial role label is empty");

            var seenLabels = new System.Collections.Generic.HashSet<string>();
            seenLabels.Add(initialLabel);
            for (int i = 0; i < 5; i++)
            {
                cycleMethod.Invoke(panel, null);
                var label = (string)roleLabelProp.GetValue(panel);
                var hint = (string)roleHintProp.GetValue(panel);
                Check(!string.IsNullOrWhiteSpace(label), "Role label is empty after cycling");
                Check(!string.IsNullOrWhiteSpace(hint), "Role hint is empty after cycling");
                seenLabels.Add(label);
            }
            Check(seenLabels.Count == 5, "Expected 5 distinct modder role preset labels, saw: " + seenLabels.Count);

            var rebuildMethod = type.GetMethod("RebuildCategoryCommands", BindingFlags.Instance | BindingFlags.NonPublic);
            rebuildMethod.Invoke(panel, null);
            var cmds = (System.Collections.IList)type.GetProperty("CategorySuggestedCommands").GetValue(panel);
            Check(cmds.Count > 0, "No suggested commands to pin");
            var firstCmd = cmds[0];

            var togglePinMethod = type.GetMethod("TogglePinSuggestedCommand");
            var pinnedList = (System.Collections.IList)type.GetProperty("PinnedCommands").GetValue(panel);
            var hasPinnedProp = type.GetProperty("HasPinnedCommands");

            Check(pinnedList.Count == 0, "Initially pinned list should be empty");
            Check(!(bool)hasPinnedProp.GetValue(panel), "HasPinnedCommands should initially be false");

            togglePinMethod.Invoke(panel, new object[] { firstCmd });
            Check(pinnedList.Count == 1, "Pinned list should contain 1 item after pinning");
            Check((bool)hasPinnedProp.GetValue(panel), "HasPinnedCommands should be true after pinning");

            togglePinMethod.Invoke(panel, new object[] { firstCmd });
            Check(pinnedList.Count == 0, "Pinned list should be empty after unpinning");
            Check(!(bool)hasPinnedProp.GetValue(panel), "HasPinnedCommands should be false after unpinning");
        }

        static void StableTimeSlicingGuidance()
        {
            const string guidance = "2. For optional periodic hero work, use ForgeTimeSlicer.ShouldProcess(hero.StringId, currentHour) for stable buckets. Defer only work that can wait up to 24 in-game hours, and measure before claiming a performance gain.";
            const string resourceId = "forge_b6e557fb90d8";
            string panelSource = File.ReadAllText("src/CalradiaForge.Mod/PanelViewModel.cs");
            Check(panelSource.Contains(guidance), "The simulation guidance must use the stable SDK helper and state when deferral is safe.");
            Check(!panelSource.Contains("hero.Id.GetHashCode() % 24"), "The UI must not recommend process-dependent hero bucketing.");
            Check(File.ReadAllText("tools/regenerate_language_resources.py").Contains(guidance), "The resource generator must classify the new panel key as source-localized.");

            var sourceCatalogs = new[]
            {
                new { Iso = "en", Folder = "EN" }, new { Iso = "es", Folder = "SP" },
                new { Iso = "pt", Folder = "BR" }, new { Iso = "de", Folder = "DE" },
                new { Iso = "fr", Folder = "FR" }, new { Iso = "it", Folder = "IT" },
                new { Iso = "pl", Folder = "PL" }, new { Iso = "ru", Folder = "RU" },
                new { Iso = "tr", Folder = "TR" }, new { Iso = "zh-HANS", Folder = "CNs" },
                new { Iso = "zh-HANT", Folder = "CNt" }, new { Iso = "ja", Folder = "JP" },
                new { Iso = "ko", Folder = "KO" }
            };

            foreach (var locale in sourceCatalogs)
            {
                string sourcePath = Path.Combine("localization", locale.Iso + ".xml");
                var sourceEntry = XDocument.Load(sourcePath).Descendants("string")
                    .SingleOrDefault(entry => (string)entry.Attribute("key") == guidance);
                Check(sourceEntry != null && !string.IsNullOrWhiteSpace((string)sourceEntry.Attribute("value")),
                    "Missing source translation for stable time-slicing guidance: " + locale.Iso);

                string resourcePath = Path.Combine("modules/CalradiaForge/ModuleData/Languages", locale.Folder, "forge_strings.xml");
                var resourceEntry = XDocument.Load(resourcePath).Descendants("string")
                    .SingleOrDefault(entry => (string)entry.Attribute("id") == resourceId);
                Check(resourceEntry != null && (string)resourceEntry.Attribute("text") == (string)sourceEntry.Attribute("value"),
                    "Generated game resource does not match the localized source for " + locale.Iso);
            }

            // This is general native-panel help text, not a native-menu control label.
            // The localized source catalogs and generated game resources are verified above.
        }
    }
}
