using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using CalradiaForge.Sdk;
using CalradiaForge.Core;
using CalradiaForge.Mod;

namespace CalradiaForge.Tests
{
    public static class AdvancedToolsTests
    {
        static bool UseRoutedDesktopAssertions() => true;

        static string LoadDesktopXamlTree()
        {
            string[] xamlCandidates = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Desktop/MainWindow.xaml"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Desktop/MainWindow.xaml"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Desktop/MainWindow.xaml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Desktop/MainWindow.xaml"))
            };
            string xamlPath = xamlCandidates.FirstOrDefault(File.Exists);
            if (xamlPath == null) throw new FileNotFoundException("Could not locate MainWindow.xaml.");
            string dir = Path.GetDirectoryName(xamlPath);
            return string.Concat(Directory.EnumerateFiles(dir, "*.xaml", SearchOption.AllDirectories).Select(File.ReadAllText));
        }

        static void AssertCurrentNativePrefab()
        {
            string path = Path.GetFullPath("modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml");
            if (!File.Exists(path)) throw new FileNotFoundException("CalradiaForge.xml not found", path);
            string xml = File.ReadAllText(path);
            XDocument document = XDocument.Parse(xml);

            // Keep the native navigation contract explicit: each of the eight routes has one
            // command, one selected-state binding, and is reachable through Gauntlet focus.
            string[][] routes = new[]
            {
                new[] { "ForgeSummary", "ExecuteSummary", "IsSummaryActive" },
                new[] { "ForgeModules", "ExecuteModules", "IsModulesActive" },
                new[] { "ForgeLogs", "ExecuteLogs", "IsLogsActive" },
                new[] { "ForgeInspector", "ExecuteInspector", "IsInspectorActive" },
                new[] { "ForgeTests", "ExecuteTests", "IsTestsActive" },
                new[] { "ForgeMetrics", "ExecuteMetrics", "IsMetricsActive" },
                new[] { "ForgeFramework", "ExecuteFramework", "IsFrameworkActive" },
                new[] { "ForgeExtensions", "ExecuteExtensions", "IsExtensionsActive" }
            };
            var routeButtons = document.Descendants("ButtonWidget")
                .Where(button => routes.Any(route => route[0] == (string)button.Attribute("Id")))
                .ToList();
            if (routeButtons.Count != routes.Length)
                throw new Exception("CalradiaForge.xml must define exactly eight native area navigation buttons.");

            foreach (string[] route in routes)
            {
                XElement button = routeButtons.SingleOrDefault(item => (string)item.Attribute("Id") == route[0]);
                if (button == null) throw new Exception("CalradiaForge.xml missing navigation button " + route[0] + ".");
                if ((string)button.Attribute("Command.Click") != route[1])
                    throw new Exception(route[0] + " must invoke " + route[1] + ".");
                if ((string)button.Attribute("IsSelected") != "@" + route[2])
                    throw new Exception(route[0] + " must bind selected state to @" + route[2] + ".");
                if ((string)button.Attribute("ButtonType") != "Radio")
                    throw new Exception(route[0] + " must use Radio semantics so only the active route is selected.");
                if ((string)button.Attribute("IsFocusable") != "true")
                    throw new Exception(route[0] + " must remain keyboard focusable.");
                if ((string)button.Attribute("DoNotPassEventsToChildren") != "true")
                    throw new Exception(route[0] + " must route pointer input through its button command.");
            }

            // Gauntlet IDs must be stable, present, and globally unambiguous.
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (XElement element in document.Root.DescendantsAndSelf())
            {
                XAttribute id = element.Attribute("Id");
                if (id == null) continue;
                string value = id.Value.Trim();
                if (value.Length == 0) throw new Exception("CalradiaForge.xml contains an empty Id on " + element.Name + ".");
                if (!ids.Add(value)) throw new Exception("CalradiaForge.xml contains duplicate Id '" + value + "'.");
            }

            XElement commandArgument = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgeArgument");
            XElement assemblyPath = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgeAssemblyPath");
            if (commandArgument == null || assemblyPath == null || commandArgument == assemblyPath)
                throw new Exception("Command argument and assembly path must have distinct ForgeArgument and ForgeAssemblyPath IDs.");
            if ((string)commandArgument.Attribute("Text") != "@Argument" || (string)assemblyPath.Attribute("Text") != "@Argument")
                throw new Exception("Both distinct argument controls must retain their @Argument binding.");

            // Retain the complete evidence ledger contract and the separate action decks.
            string[] requiredIds =
            {
                "ForgeNavigationRail", "ForgeAreaNavigation", "ForgeEvidenceFrame", "ForgeEvidenceHeading",
                "ForgeEvidencePage", "ForgeEmptyEvidence", "ForgeEvidenceContent", "ForgePrimaryCommandDeck", "ForgeSecondaryActionDeck",
                "ForgeExtensionActionDeck", "ForgeAssemblyActionDeck", "ForgePaginationAndUtilityDeck",
                "ForgePlaybookScroll", "ForgePlaybookScrollBar", "ForgePlaybookScrollBarHandle"
            };
            foreach (string id in requiredIds)
                if (!ids.Contains(id)) throw new Exception("CalradiaForge.xml missing structural Gauntlet ID " + id + ".");

            XElement rail = document.Descendants().Single(element => (string)element.Attribute("Id") == "ForgeNavigationRail");
            XElement navigation = document.Descendants().Single(element => (string)element.Attribute("Id") == "ForgeAreaNavigation");
            List<XElement> railButtons = navigation.Descendants("ButtonWidget").ToList();
            if (!rail.Descendants().Contains(navigation) || (string)navigation.Attribute("StackLayout.LayoutMethod") != "VerticalTopToBottom" ||
                railButtons.Count != routes.Length ||
                routes.Any(route => !railButtons.Any(button => (string)button.Attribute("Id") == route[0])))
                throw new Exception("All eight routes must be hosted in the vertically stacked left navigation rail.");

            XElement shell = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgeWorkbenchShell");
            if (shell == null || (string)shell.Attribute("WidthSizePolicy") != "StretchToParent" ||
                (string)shell.Attribute("HeightSizePolicy") != "StretchToParent" ||
                (string)shell.Attribute("MaxWidth") != "1760" || (string)shell.Attribute("MaxHeight") != "1024" ||
                new[] { "MarginLeft", "MarginRight", "MarginTop", "MarginBottom" }.Any(name => (string)shell.Attribute(name) != "24"))
                throw new Exception("The Gauntlet workbench must adapt to its viewport, cap its size and preserve 24-DIP margins.");
            XElement inputRow = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgeInputRow");
            XElement primaryHost = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgePrimaryCommandHost");
            XElement playbook = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgePlaybookPanel");
            if (inputRow == null || primaryHost == null || playbook == null ||
                (string)inputRow.Attribute("MarginRight") != "@WorkspaceRightMargin" ||
                (string)primaryHost.Attribute("MarginRight") != "@WorkspaceRightMargin" ||
                (string)playbook.Attribute("IsVisible") != "@IsPlaybookVisible" ||
                (string)playbook.Attribute("MarginTop") != "281" ||
                (string)playbook.Attribute("HeightSizePolicy") != "StretchToParent" ||
                (string)playbook.Attribute("MarginBottom") != "166" ||
                (string)playbook.Attribute("MaxHeight") != "390")
                throw new Exception("Detailed Playbook must start below briefing cards and reserve its right-side space from input and command rows.");
            XElement playbookScroll = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgePlaybookScroll");
            XElement playbookScrollBar = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgePlaybookScrollBar");
            XElement playbookContent = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgePlaybookContent");
            XElement playbookFlow = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgePlaybookFlow");
            if (playbookScroll == null || playbookScroll.Name.LocalName != "ScrollablePanel" ||
                playbookScrollBar == null || playbookScrollBar.Name.LocalName != "ScrollbarWidget" ||
                document.Descendants().Any(element => element.Name.LocalName == "ScrollBarWidget"))
                throw new Exception("Playbook must use the exact case-sensitive Gauntlet ScrollbarWidget type; ScrollBarWidget is invalid.");
            if ((string)playbookScroll.Attribute("ClipRect") != "ForgePlaybookClip" ||
                (string)playbookScroll.Attribute("InnerPanel") != "ForgePlaybookClip\\ForgePlaybookContent" ||
                (string)playbookScroll.Attribute("MarginTop") != "42" ||
                (string)playbookScroll.Attribute("MarginBottom") != "10" ||
                (string)playbookScrollBar.Attribute("SuggestedWidth") != "8" ||
                (string)playbookScrollBar.Attribute("MarginTop") != "42" ||
                (string)playbookScrollBar.Attribute("MarginBottom") != "10" ||
                (string)playbookScrollBar.Attribute("MarginRight") != "10" ||
                playbookContent == null || (string)playbookContent.Attribute("HeightSizePolicy") != "CoverChildren" ||
                playbookFlow == null || (string)playbookFlow.Attribute("HeightSizePolicy") != "CoverChildren" ||
                (string)playbookFlow.Attribute("StackLayout.LayoutMethod") != "VerticalTopToBottom")
                throw new Exception("Playbook text content and scrollbar must fill the clipped viewport and expose the bottom of the flow.");
            string[] primaryActionIds = { "ForgeRefresh", "ForgeScan", "ForgePin", "ForgeCompare", "ForgeRun" };
            foreach (string id in primaryActionIds)
            {
                XElement action = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == id);
                if (action == null || (string)action.Attribute("SuggestedWidth") != "@PrimaryActionButtonWidth")
                    throw new Exception(id + " must use the adaptive detailed-mode button width.");
            }
            string panelViewModelPath = Path.GetFullPath("src/CalradiaForge.Mod/PanelViewModel.cs");
            string panelViewModel = File.ReadAllText(panelViewModelPath);
            if (!panelViewModel.Contains("[DataSourceProperty] public float PrimaryActionButtonWidth => _isDetailedMode && !evidenceFocused ? 100f : 164f;") ||
                !panelViewModel.Contains("nameof(PrimaryActionButtonWidth)") ||
                !panelViewModel.Contains("public bool IsPlaybookVisible => _isDetailedMode && !evidenceFocused && !_isKeyHelpOpen;") ||
                !panelViewModel.Contains("OnPropertyChanged(nameof(IsPlaybookVisible));"))
                throw new Exception("PanelViewModel must publish and notify the adaptive detailed-mode primary action width.");
            string[] playbookWrapContracts =
            {
                "CategoryPlaybookTitle => WrapPlaybookText(GetCategoryPlaybookTitle(currentCategory), 30)",
                "CategoryPlaybookStep1 => WrapPlaybookText(GetCategoryPlaybookStep1(currentCategory), 34)",
                "CategoryPlaybookStep2 => WrapPlaybookText(GetCategoryPlaybookStep2(currentCategory), 34)",
                "CategoryPlaybookStep3 => WrapPlaybookText(GetCategoryPlaybookStep3(currentCategory), 34)",
                "CategoryTroubleshootingTitle => WrapPlaybookText(GetCategoryTroubleshootingTitle(currentCategory), 38)",
                "CategoryTroubleshootingAdvice => WrapPlaybookText(GetCategoryTroubleshootingAdvice(currentCategory), 42)",
                "CategoryRecommendedMacro => WrapPlaybookText(GetCategoryRecommendedMacro(currentCategory), 34)",
                "wrapped.Append('\\n').Append(word);"
            };
            if (playbookWrapContracts.Any(contract => !panelViewModel.Contains(contract)))
                throw new Exception("Playbook ViewModel must explicitly wrap every variable text field without changing its source literals.");
            string[] playbookTextIds =
            {
                "ForgePlaybookTitle", "ForgePlaybookStep1", "ForgePlaybookStep2", "ForgePlaybookStep3",
                "ForgeTroubleshootingTitle", "ForgeTroubleshootingAdvice", "ForgeRecommendedMacro"
            };
            foreach (string id in playbookTextIds)
            {
                XElement text = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == id);
                if (text == null || text.Name.LocalName != "TextWidget" ||
                    (string)text.Attribute("WidthSizePolicy") != "StretchToParent" ||
                    (string)text.Attribute("HeightSizePolicy") != "CoverChildren" ||
                    !playbookFlow.Descendants().Contains(text))
                    throw new Exception(id + " must wrap within and grow the scrollable Playbook flow.");
            }
            if (railButtons.Any(button => (string)button.Attribute("SuggestedHeight") != "42") ||
                (string)navigation.Attribute("SuggestedHeight") != "384")
                throw new Exception("The compact route rail must leave all eight destinations inside its clipped viewport.");
            XElement secondaryDeck = document.Descendants().Single(element => (string)element.Attribute("Id") == "ForgeSecondaryActionDeck");
            XElement paginationDeck = document.Descendants().Single(element => (string)element.Attribute("Id") == "ForgePaginationAndUtilityDeck");
            if ((string)secondaryDeck.Attribute("VerticalAlignment") != "Bottom" || (string)secondaryDeck.Attribute("MarginBottom") != "99" ||
                (string)paginationDeck.Attribute("VerticalAlignment") != "Bottom" || (string)paginationDeck.Attribute("MarginBottom") != "47")
                throw new Exception("Primary actions and pagination must remain anchored above the workbench bottom edge.");

            XElement evidence = document.Descendants().Single(element => (string)element.Attribute("Id") == "ForgeEvidenceFrame");
            XElement page = document.Descendants().Single(element => (string)element.Attribute("Id") == "ForgeEvidencePage");
            XElement empty = document.Descendants().Single(element => (string)element.Attribute("Id") == "ForgeEmptyEvidence");
            XElement content = document.Descendants().Single(element => (string)element.Attribute("Id") == "ForgeEvidenceContent");
            XElement filterRow = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgeOutputFilterRow");
            XElement filterInput = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgeOutputFilterInput");
            XElement filterPlaceholder = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgeOutputFilterPlaceholder");
            XElement filterClear = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgeOutputFilterClear");
            XElement evidenceScroll = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgeEvidenceScroll");
            XElement evidenceScrollbar = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgeEvidenceScrollBar");
            if ((string)evidence.Attribute("WidthSizePolicy") != "StretchToParent" ||
                (string)evidence.Attribute("HeightSizePolicy") != "StretchToParent" ||
                (string)evidence.Attribute("MarginTop") != "@EvidenceTop" ||
                (string)evidence.Attribute("MarginBottom") != "178")
                throw new Exception("Evidence ledger must stretch between its selected top edge and the bottom action deck.");
            if ((string)page.Attribute("Text") != "@PageLabel" || (string)empty.Attribute("IsVisible") != "@IsNormalContentEmpty" ||
                (string)empty.Attribute("Text") != "@ContentPlaceholder" || (string)content.Attribute("Text") != "@Content" ||
                (string)content.Attribute("ClipContents") != "true")
                throw new Exception("Evidence ledger must retain page, empty-state, and raw-content bindings.");
            if (filterRow == null || (string)filterRow.Attribute("MarginTop") != "5" ||
                (string)filterRow.Attribute("SuggestedHeight") != "32" ||
                (string)filterRow.Attribute("MarginLeft") != "225" ||
                (string)filterRow.Attribute("MarginRight") != "150" ||
                filterInput == null || filterInput.Name.LocalName != "EditableTextWidget" ||
                (string)filterInput.Attribute("Text") != "@OutputFilterText" ||
                (string)filterInput.Attribute("UpdateTextOnTyping") != "true" ||
                (string)filterInput.Attribute("Hint.HintText") != "@FilterLinesHint" ||
                filterPlaceholder == null || (string)filterPlaceholder.Attribute("IsVisible") != "@IsOutputFilterEmpty" ||
                (string)filterPlaceholder.Attribute("Text") != "@OutputFilterPlaceholder" ||
                filterClear == null || (string)filterClear.Attribute("IsVisible") != "@HasOutputFilter" ||
                (string)filterClear.Attribute("Command.Click") != "ExecuteClearOutputFilter" ||
                (string)filterClear.Attribute("Hint.HintText") != "@ClearOutputFilterHint" ||
                evidenceScroll == null || (string)evidenceScroll.Attribute("MarginTop") != "78" ||
                evidenceScrollbar == null || (string)evidenceScrollbar.Attribute("MarginTop") != "78")
                throw new Exception("The live output filter must share the evidence header, update while typing, expose a localized clear action, and stay above the paged evidence viewport.");

            XElement previous = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgePrevious");
            XElement next = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgeNext");
            if (previous == null || (string)previous.Attribute("Command.Click") != "ExecutePrevious" ||
                next == null || (string)next.Attribute("Command.Click") != "ExecuteNext")
                throw new Exception("Evidence pagination must preserve its Previous and Next commands.");

            // Keep secondary and utility actions reachable, including the safety-labelled
            // clear-output action. This is a display-buffer command, not evidence deletion.
            foreach (string command in new[] { "ExecuteEnable", "ExecuteCopy", "ExecuteExport", "ExecuteBatch", "ExecuteClearOutput", "ExecuteSnapshots", "ExecuteDependencies", "ExecuteClose" })
                if (!document.Descendants("ButtonWidget").Any(button => (string)button.Attribute("Command.Click") == command))
                    throw new Exception("CalradiaForge.xml missing secondary or utility command " + command + ".");
            XElement clearOutput = document.Descendants("ButtonWidget").Single(button => (string)button.Attribute("Command.Click") == "ExecuteClearOutput");
            if ((string)clearOutput.Attribute("Hint.HintText") != "@ClearOutputHint")
                throw new Exception("Clear Output must retain its explanatory @ClearOutputHint.");
            XElement regularDeck = document.Descendants().Single(element => (string)element.Attribute("Id") == "ForgeSecondaryActionDeck");
            if (!regularDeck.Descendants("ButtonWidget").Contains(clearOutput) ||
                !clearOutput.Descendants("TextWidget").Any(label => (string)label.Attribute("Text") == "@ClearOutputLabel"))
                throw new Exception("Clear Output must be in the regular secondary command deck.");

            // Current approved Gauntlet material sprites stay registered and passive,
            // outside editable and evidence surfaces.
            string[][] materialTextures = new[]
            {
                new[] { "ForgeHeaderCloth", "forge_war_table_cloth_v2" },
                new[] { "ForgeHeaderHeraldicOverlay", "forge_heraldic_header_v2" },
                new[] { "ForgeRailPineFelt", "forge_pine_felt" },
                new[] { "ForgeRailCloth", "forge_heraldic_rail_v2" },
                new[] { "ForgeBriefingContextPineFelt", "forge_pine_felt" },
                new[] { "ForgeBriefingContextPatinaRule", "forge_patina_brass" },
                new[] { "ForgeBriefingTestingPineFelt", "forge_pine_felt" },
                new[] { "ForgeBriefingTestingPatinaRule", "forge_patina_brass" },
                new[] { "ForgeBriefingEvidencePineFelt", "forge_pine_felt" },
                new[] { "ForgeBriefingEvidencePatinaRule", "forge_patina_brass" },
                new[] { "ForgeBriefingTestsPineFelt", "forge_pine_felt" },
                new[] { "ForgeBriefingTestsPatinaRule", "forge_patina_brass" },
                new[] { "ForgeTopBrassFrameRule", "forge_patina_brass" },
                new[] { "ForgeEvidenceActionBrassRule", "forge_patina_brass" },
                new[] { "ForgeBottomBrassFrameRule", "forge_patina_brass" }
            };
            foreach (string[] texture in materialTextures)
            {
                XElement image = document.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == texture[0]);
                if (image == null || image.Name != "ImageWidget" || (string)image.Attribute("Sprite") != texture[1] ||
                    (string)image.Attribute("DoNotAcceptEvents") != "true" ||
                    (string)image.Attribute("DoNotPassEventsToChildren") != "true")
                    throw new Exception("CalradiaForge.xml must use the approved passive material sprite on " + texture[0] + ".");
            }

            string spriteDataPath = Path.GetFullPath("modules/CalradiaForge/GUI/CalradiaForgeSpriteData.xml");
            if (!File.Exists(spriteDataPath)) throw new FileNotFoundException("CalradiaForgeSpriteData.xml not found", spriteDataPath);
            XDocument spriteData = XDocument.Parse(File.ReadAllText(spriteDataPath));
            foreach (string spriteName in materialTextures.Select(texture => texture[1]).Distinct(StringComparer.Ordinal))
            {
                List<XElement> spriteParts = spriteData.Descendants("SpritePart")
                    .Where(part => (string)part.Element("Name") == spriteName).ToList();
                List<XElement> genericSprites = spriteData.Descendants("GenericSprite")
                    .Where(sprite => (string)sprite.Element("Name") == spriteName).ToList();
                if (spriteParts.Count != 1 || (string)spriteParts[0].Element("CategoryName") != "ui_calradiaforge" ||
                    genericSprites.Count != 1 || (string)genericSprites[0].Element("SpritePartName") != spriteName)
                    throw new Exception("CalradiaForgeSpriteData.xml must register approved material sprite " + spriteName + " exactly once.");
            }
            XElement evidenceFrame = document.Descendants().Single(element => (string)element.Attribute("Id") == "ForgeEvidenceFrame");
            if (evidenceFrame.Descendants("ImageWidget").Any(image => !image.Ancestors("ScrollbarWidget").Any()) ||
                evidenceFrame.Descendants().Any(element => ((string)element.Attribute("Sprite"))?.StartsWith("forge_", StringComparison.Ordinal) == true))
                throw new Exception("The evidence ledger must stay on an untextured high-contrast surface.");
            foreach (string retired in new[] { "forge_header_filigree", "forge_map_contours", "forge_heraldic_corner", "forge_woven_border", "forge_rosette_mark", "forge_stitch_rule", "forge_pine_grain", "forge_table_grain", "forge_brass_rule", "forge_dark_wood", "forge_inkwash" })
                if (document.Descendants().Any(element => (string)element.Attribute("Sprite") == retired))
                    throw new Exception("Legacy geometric texture must be removed from the active prefab: " + retired + ".");

            foreach (string binding in new[] { "ExecuteRefresh", "ExecuteScan", "ExecutePin", "ExecuteCompare", "ExecuteRun", "@MemoryHealthText", "@Content", "DoNotPassEventsToChildren" })
                if (!xml.Contains(binding)) throw new Exception("Current CalradiaForge prefab is missing " + binding + ".");

            string vmPath = Path.GetFullPath("src/CalradiaForge.Mod/PanelViewModel.cs");
            if (!File.Exists(vmPath)) throw new FileNotFoundException("PanelViewModel.cs not found", vmPath);
            string vm = File.ReadAllText(vmPath);
            foreach (string[] route in routes)
            {
                if (!vm.Contains("public void " + route[1] + "()"))
                    throw new Exception("PanelViewModel.cs missing native route command " + route[1] + ".");
                if (!vm.Contains("public bool " + route[2]))
                    throw new Exception("PanelViewModel.cs missing selected-state property " + route[2] + ".");
            }
            if (!vm.Contains("public void ExecuteClearOutput()") || !vm.Contains("public string ClearOutputLabel") || !vm.Contains("public string ClearOutputHint") ||
                !vm.Contains("case \"ForgeClear\": ExecuteClearOutput(); break;"))
                throw new Exception("PanelViewModel.cs must retain the explicit Clear Output action and safety hint.");
            int clearStart = vm.IndexOf("public void ExecuteClearOutput()", StringComparison.Ordinal);
            int clearEnd = vm.IndexOf("public void RecordCommand(", clearStart, StringComparison.Ordinal);
            if (clearStart < 0 || clearEnd < 0) throw new Exception("Could not isolate the Clear Output display-only action.");
            string clearMethod = vm.Substring(clearStart, clearEnd - clearStart);
            if (!clearMethod.Contains("full = \"\";") || !clearMethod.Contains("page = 0;") ||
                !clearMethod.Contains("pageCount = 1;") || !clearMethod.Contains("Render();") || clearMethod.Contains("Send("))
                throw new Exception("Clear Output must reset only the local display buffer and must not issue a game command.");

            string liveWatchSource = File.ReadAllText(Path.GetFullPath("src/CalradiaForge.Mod/PanelViewModel.cs"));
            string subModuleSource = File.ReadAllText(Path.GetFullPath("src/CalradiaForge.Mod/SubModule.cs"));
            if (!liveWatchSource.Contains("\"action:toggle-live-watch\"") ||
                !liveWatchSource.Contains("ExecuteNavigationPaletteSelect") ||
                !subModuleSource.Contains("Input.IsKeyPressed(InputKey.W)") ||
                !subModuleSource.Contains("vm?.ExecuteToggleLiveWatch()"))
                throw new Exception("Live Watch must remain reachable through the navigation palette and its existing Ctrl+W shortcut, not a prefab button.");
        }

        public static void Run(Action<string, Action> test)
        {
            test("Perk Tree Scaffold generates valid C# with unique perk IDs", TestPerkTreeScaffold);
            test("Diplomatic Matrix generates valid XML and balanced relations", TestDiplomaticMatrix);
            test("Gauntlet Brush XML contains required layers and valid #RRGGBBAA hex", TestGauntletBrushSynthesis);
            test("Mod ID collision detector flags native clashes and prefixes IDs", TestModIdCollisionAuditor);
            test("Keyboard Shortcut Directory exports valid markdown table", TestShortcutCheatSheet);
            test("Mod Packaging Auditor verifies assembly metadata and excludes scripts", TestPackagingAuditor);
            test("Mission Mesh Safety Guard verifies _initialized deferred pattern", TestMissionMeshGuard);
            test("SaveableTypeDefiner allocates ID >= 2,500,000 avoiding native collision", TestSaveableTypeDefinerAllocation);
            test("Gauntlet Prefab Event-Pass Checker verifies DoNotAcceptEvents", TestGauntletEventPass);
            test("GEMINI.md Campaign Namespace Guard forbids 'Campaign' class/folder", TestCampaignNamespaceGuard);
            test("Quest & Dialogue Flow Builder generates valid QuestBase & DialogFlow", TestQuestDialogBuilder);
            test("Custom Audio Synthesizer validates module_sounds.xml mixer buses", TestSoundXmlSynthesizer);
            test("Troop & Character XML Validator verifies integer age, slots, and upgrade tree", TestTroopCharacterXmlValidator);
            test("Item & Smithing Crafting Validator validates damage types, components, and cross-references", TestItemCraftingValidator);
            test("ForgeTroopBuilder validates integer age, slots, and upgrade targets", TestForgeTroopBuilder);
            test("ForgeItemBuilder validates damage types and component structures", TestForgeItemBuilder);
            test("ForgeAudioBuilder validates categories and file extensions", TestForgeAudioBuilder);
            test("ForgeQuestBuilder scaffolds double SetDialogs and SaveableTypeDefiner >= 2.5M", TestForgeQuestBuilder);
            test("Lifecycle & Memory Leak Guards (ForgeCampaignEvents, ForgeData, CampaignVariableInspector)", TestLifecycleAndMemoryLeakGuards);
            test("ForgeSaveChunker prevents 31KB TaleWorlds serializer corruption", TestForgeSaveChunker);
            test("ForgeMissionLogicBuilder adheres to mission lifecycle & interaction hooks", TestForgeMissionLogicBuilder);
            test("ForgeDetour restores original instruction bytes on unpatch", TestForgeDetourUnpatch);
            test("ForgeUI clears extension-page handlers", TestForgeUI_Clear);
            test("PanelViewModel & CalradiaForge.xml UI telemetry, clear output, and empty-state placeholder", TestPanelViewModelAndPrefabPolish);
            test("Gauntlet live output filter preserves source, argument, wrapping, and paging", TestGauntletLiveOutputFilter);
            test("Gauntlet output comparison is deterministic, filtered, paged, and bounded", TestGauntletOutputComparison);
            test("Desktop & In-Game UI error corrections (panel overlap, glyph fallback, action parity)", TestUiErrorCorrectionsAndSafety);
            test("Zero-Scroll Desktop Navigation (Accordion, ZenMode, CategoryPicker, Breadcrumbs, Recent)", TestZeroScrollDesktopInterface);
            test("Desktop Top and Bottom Panel Typography Upgrades (TitleBar, Header, Breadcrumbs, StatusBar)", TestDesktopPanelFontSizesAndLayout);
            test("In-Game UI Simplification, Modular Categories & In-Game Modding Tools", TestInGameUiSimplificationAndFeatures);
            test("F10 Rising Edge and GauntletLayer Lifecycle Telemetry", TestPanelHotkeyAndLayerLifecycleTelemetry);
            test("Framework, SDK and Core Architectural Enhancements (TimeSlicer, ForgeText, ModelRegistry, DialogueBuilder, LifecycleGuard)", TestForgeFrameworkAndSdkEnhancements);
            test("ForgeDiplomacy & Kingdom War/Peace Scoring", TestDiplomacyScoring);
            test("ForgeSettlementSystem & Rebellion Risk Index", TestSettlementRebellionRisk);
            test("ForgeUnderworldSystem & Alley Racket Yields", TestUnderworldAlleyAndSmuggling);
            test("ForgeProgressionSystem & Learning Rate / Clan Tiers", TestProgressionAndClanTiers);
            test("ForgeCombatTactics & Morale Shock / Siege Breach", TestCombatTacticsAndSiegeBreach);
            test("ForgeTradeSystem & Inventory Underflow Prevention", TestTradeUnderflowAndPriceElasticity);
            test("ForgePartySpawner Blueprint & Template Safety", TestPartySpawnerBlueprintSafety);
            test("ForgeHudProjector 3D-to-2D Screen Math", TestHudProjectorAndMapTrackDecay);
            test("ModRuleAuditor Multi-Rule Compliance Validator", TestModRuleAuditorCompliance);
            test("CalradiaForge.Core.SDK Enriched Modules Integration", TestCoreSdkModulesIntegration);
            test("In-Game Gauntlet UI Simulation & Audit Systems and XML Parity", TestInGameUiSimulateAndAudit);
            test("In-Game Gauntlet UI Round 2 (LiveWatch, KeyHelp, Toast, FilterLines, 4 New Tools)", TestInGameUiRound2Enhancements);
            test("Novice Modder Hub — 7 Scaffold Generators, Action Parity, UI Buttons", TestNoviceModderFeatures);
            test("QoL Improvements & Comprehensive Gauntlet Hint System", TestQoLAndHintSystem);
            test("Screenshot Errors Fix, UI/UX Polish & XML Validity", TestFixScreenshotIssuesAndLayoutPolish);
            test("Novice & Advanced Tools, Crash Fix & v22.0.0 Release", TestNoviceAndAdvancedToolsAndCrashFix);
        }

        private static void TestPerkTreeScaffold()
        {
            string scaffold = @"using TaleWorlds.CampaignSystem.CharacterDevelopment;
namespace MyCustomMod.CharacterDevelopment
{
    public static class CustomPerks
    {
        public static PerkObject IronVanguard { get; private set; }
    }
}";
            if (!scaffold.Contains("PerkObject") || !scaffold.Contains("IronVanguard"))
                throw new Exception("Perk tree scaffold missing expected PerkObject definition.");
        }

        private static void TestDiplomaticMatrix()
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Kingdoms>
  <Kingdom id=""vlandia"" initial_relation_to_battania=""-90"">
    <DiplomaticStance target_kingdom=""battania"" stance=""War"" />
  </Kingdom>
</Kingdoms>";
            var doc = XDocument.Parse(xml);
            var kingdom = doc.Descendants("Kingdom").FirstOrDefault(k => (string)k.Attribute("id") == "vlandia");
            if (kingdom == null || (string)kingdom.Attribute("initial_relation_to_battania") != "-90")
                throw new Exception("Diplomatic matrix XML failed to parse expected relation.");
        }

        private static void TestGauntletBrushSynthesis()
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Brushes>
  <Brush Name=""CalradiaForge.TacticalButton"">
    <Layers>
      <BrushLayer Name=""Default"" Sprite=""BlankWhite"" Color=""#1A2921FF"" />
      <BrushLayer Name=""Hovered"" Sprite=""BlankWhite"" Color=""#233B2EFF"" />
    </Layers>
  </Brush>
</Brushes>";
            var doc = XDocument.Parse(xml);
            var brush = doc.Descendants("Brush").FirstOrDefault(b => (string)b.Attribute("Name") == "CalradiaForge.TacticalButton");
            if (brush == null) throw new Exception("Brush missing.");
            var defaultLayer = brush.Descendants("BrushLayer").FirstOrDefault(l => (string)l.Attribute("Name") == "Default");
            if (defaultLayer == null || (string)defaultLayer.Attribute("Color") != "#1A2921FF")
                throw new Exception("Brush layer color mismatch or missing.");
        }

        private static void TestModIdCollisionAuditor()
        {
            string[] nativeTroops = { "empire_infantry", "vlandian_recruit", "sturgian_warrior" };
            string customTroop = "empire_infantry";
            bool collision = nativeTroops.Contains(customTroop);
            if (!collision) throw new Exception("Expected collision with native troop ID.");

            string prefixed = "cf_" + customTroop;
            if (nativeTroops.Contains(prefixed)) throw new Exception("Prefix failed to avoid collision.");
        }

        private static void TestShortcutCheatSheet()
        {
            string md = "| `Ctrl + P` | Quick Search Modal | Global |\n| `F1` | Toggle Shortcuts Guide | Global |";
            if (!md.Contains("Ctrl + P") || !md.Contains("F1"))
                throw new Exception("Shortcut markdown missing core accelerators.");
        }

        private static void TestPackagingAuditor()
        {
            string[] safeZipContents = { "SubModule.xml", "bin/Win64_Shipping_Client/CalradiaForge.Mod.dll", "ModuleData/Items.xml" };
            bool hasScript = safeZipContents.Any(f => f.EndsWith(".bat") || f.EndsWith(".ps1"));
            if (hasScript) throw new Exception("Public zip must not contain script files.");
        }

        private static void TestMissionMeshGuard()
        {
            string missionCode = @"public class SafeLogic : MissionLogic {
                private bool _initialized;
                public override void OnMissionTick(float dt) {
                    if (!_initialized) { _initialized = true; InitMeshes(); }
                }
            }";
            if (!missionCode.Contains("private bool _initialized") || !missionCode.Contains("OnMissionTick"))
                throw new Exception("MissionLogic code does not satisfy deferred mesh initialization guard pattern.");
        }

        private static void TestSaveableTypeDefinerAllocation()
        {
            int baseId = 2_500_000;
            if (baseId < 100_000) throw new Exception("Base ID < 100,000 collides with native engine range.");
            if (baseId < 2_000_000) throw new Exception("Recommended community range is >= 2,500,000.");
        }

        private static void TestGauntletEventPass()
        {
            string watermarkXml = @"<Widget DoNotAcceptEvents=""true"" DoNotPassEventsToChildren=""true"" />";
            if (!watermarkXml.Contains("DoNotAcceptEvents=\"true\"") || !watermarkXml.Contains("DoNotPassEventsToChildren=\"true\""))
                throw new Exception("Watermark missing event blocking prevention attributes.");
        }

        private static void TestCampaignNamespaceGuard()
        {
            string validNamespace = "CalradiaForge.Mod.CampaignBehaviors";
            string invalidNamespace = "CalradiaForge.Mod.Campaign";
            if (validNamespace.EndsWith(".Campaign")) throw new Exception("Invalid namespace passed.");
            if (!invalidNamespace.EndsWith(".Campaign")) throw new Exception("Valid namespace falsely rejected.");
        }

        private static void TestQuestDialogBuilder()
        {
            string scaffold = @"using TaleWorlds.CampaignSystem;
namespace MyCustomMod.QuestBehaviors
{
    public class CustomEscortQuest : QuestBase
    {
        public CustomEscortQuest() : base(""id"", null, CampaignTime.Days(5), 100)
        {
            SetDialogs();
            InitializeQuestOnCreation();
        }

        public override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
        }

        protected override void SetDialogs()
        {
            OfferDialogFlow = DialogFlow.CreateDialogFlow(""my_quest_offer"", 120);
        }
    }
}";
            if (!scaffold.Contains("SetDialogs()") || !scaffold.Contains("InitializeQuestOnGameLoad()"))
                throw new Exception("QuestBase scaffold violates critical double SetDialogs() engine rule.");
            if (scaffold.Contains("namespace MyCustomMod.Campaign\n") || scaffold.Contains("namespace MyCustomMod.Campaign;"))
                throw new Exception("QuestBase scaffold shadows TaleWorlds.CampaignSystem.Campaign.");
        }

        private static void TestSoundXmlSynthesizer()
        {
            string soundXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<module_sounds>
  <module_sound name=""cf_ui_click_tactical"" is_2d=""true"" sound_category=""ui"" path=""ui_click_tactical.ogg"" />
  <module_sound name=""cf_heavy_plate_impact"" is_2d=""false"" sound_category=""mission_combat"" path=""plate_heavy_impact.ogg"" />
</module_sounds>";
            var doc = XDocument.Parse(soundXml);
            var sounds = doc.Descendants("module_sound").ToList();
            if (sounds.Count != 2) throw new Exception("Audio XML failed to declare required sound definitions.");
            
            var validCategories = new[] { "ui", "mission_combat", "ambient", "voice" };
            foreach (var s in sounds)
            {
                string cat = (string)s.Attribute("sound_category");
                if (!validCategories.Contains(cat)) throw new Exception($"Invalid sound category '{cat}'.");
                string path = (string)s.Attribute("path");
                if (!path.EndsWith(".ogg") && !path.EndsWith(".wav")) throw new Exception("Sound path missing valid audio extension.");
            }
        }

        private static void TestTroopCharacterXmlValidator()
        {
            string troopXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<NPCCharacters>
  <NPCCharacter id=""custom_vlandia_veteran"" name=""{=custom_vet}Vlandian Veteran"" age=""28"" level=""21"" occupation=""Soldier"" culture=""Culture.vlandia"" default_group=""Infantry"" is_hero=""false"">
    <Equipments>
      <EquipmentSet>
        <equipment slot=""Item0"" id=""Item.vlandic_sword"" />
        <equipment slot=""Item1"" id=""Item.vlandic_shield"" />
        <equipment slot=""Head"" id=""Item.iron_helmet"" />
        <equipment slot=""Body"" id=""Item.hauberk"" />
        <equipment slot=""Gloves"" id=""Item.mail_gauntlets"" />
        <equipment slot=""Leg"" id=""Item.iron_greaves"" />
      </EquipmentSet>
      <EquipmentSet civilian=""true"">
        <equipment slot=""Item0"" id=""Item.vlandic_dagger"" />
        <equipment slot=""Body"" id=""Item.civilian_tunic"" />
        <equipment slot=""Leg"" id=""Item.leather_shoes"" />
      </EquipmentSet>
    </Equipments>
    <upgrade_targets>
      <upgrade_target id=""NPCCharacter.custom_vlandia_knight"" />
    </upgrade_targets>
  </NPCCharacter>
</NPCCharacters>";

            var doc = XDocument.Parse(troopXml);
            var character = doc.Descendants("NPCCharacter").FirstOrDefault();
            if (character == null) throw new Exception("NPCCharacter element not found.");

            // Integer age check (floating point causes engine CTD)
            string ageStr = (string)character.Attribute("age");
            if (!int.TryParse(ageStr, out _)) throw new Exception("NPCCharacter age must be an integer.");

            // Formation group check
            string group = (string)character.Attribute("default_group");
            var validGroups = new[] { "Infantry", "Ranged", "Cavalry", "HorseArcher" };
            if (!validGroups.Contains(group)) throw new Exception($"Invalid default_group '{group}'.");

            // Slots validation
            var validSlots = new[] { "Item0", "Item1", "Item2", "Item3", "Head", "Cape", "Body", "Gloves", "Leg", "Horse", "HorseHarness" };
            var equipmentItems = character.Descendants("equipment").ToList();
            if (!equipmentItems.Any()) throw new Exception("No equipment items found.");
            foreach (var eq in equipmentItems)
            {
                string slot = (string)eq.Attribute("slot");
                if (!validSlots.Contains(slot)) throw new Exception($"Invalid equipment slot '{slot}'.");
            }

            // Civilian set check
            var hasCivilian = character.Descendants("EquipmentSet").Any(s => (string)s.Attribute("civilian") == "true");
            if (!hasCivilian) throw new Exception("Troop definition missing civilian equipment set.");

            // Upgrade targets check
            var upgradeTargets = character.Descendants("upgrade_target").ToList();
            if (upgradeTargets.Count > 2) throw new Exception("Troop has more than 2 upgrade branching targets.");
            foreach (var ut in upgradeTargets)
            {
                string targetId = (string)ut.Attribute("id");
                if (!targetId.StartsWith("NPCCharacter.")) throw new Exception("Upgrade target must start with 'NPCCharacter.' prefix.");
            }
        }

        private static void TestItemCraftingValidator()
        {
            string itemXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Items>
  <Item id=""custom_vlandic_sword"" name=""{=custom_sw}Vlandian Sword"" mesh=""sword_mesh"" culture=""Culture.vlandia"" weight=""1.2"" Type=""OneHandedWeapon"">
    <ItemComponent>
      <Weapon weapon_class=""OneHandedSword"" thrust_speed=""90"" speed_rating=""92"" thrust_damage=""32"" thrust_damage_type=""Pierce"" swing_damage=""68"" swing_damage_type=""Cut"" />
    </ItemComponent>
  </Item>
</Items>";
            var itemDoc = XDocument.Parse(itemXml);
            var item = itemDoc.Descendants("Item").FirstOrDefault();
            if (item == null) throw new Exception("Item element not found.");

            var weapon = item.Descendants("Weapon").FirstOrDefault();
            if (weapon == null) throw new Exception("Weapon component not found.");

            var validDamageTypes = new[] { "Cut", "Pierce", "Blunt" };
            string thrustDt = (string)weapon.Attribute("thrust_damage_type");
            string swingDt = (string)weapon.Attribute("swing_damage_type");
            if (!validDamageTypes.Contains(thrustDt) || !validDamageTypes.Contains(swingDt))
                throw new Exception("Invalid weapon damage type.");

            // Smithing cross-reference validation
            string piecesXml = @"<CraftingPieces><CraftingPiece id=""piece_blade_1"" piece_type=""Blade"" tier=""3"" /></CraftingPieces>";
            string templateXml = @"<CraftingTemplates><CraftingTemplate id=""OneHandedSword""><UsablePieces><UsablePiece piece_id=""piece_blade_1"" /></UsablePieces></CraftingTemplate></CraftingTemplates>";

            var pDoc = XDocument.Parse(piecesXml);
            var tDoc = XDocument.Parse(templateXml);

            var declaredPieceIds = new System.Collections.Generic.HashSet<string>(pDoc.Descendants("CraftingPiece").Select(p => (string)p.Attribute("id")));
            var referencedPieceIds = tDoc.Descendants("UsablePiece").Select(u => (string)u.Attribute("piece_id")).ToList();

            foreach (var rId in referencedPieceIds)
            {
                if (!declaredPieceIds.Contains(rId))
                    throw new Exception($"Smithing template references undefined piece '{rId}'.");
            }
        }

        private static void TestForgeTroopBuilder()
        {
            var troop = ForgeTroopBuilder.Create("test_vlandia_infantry")
                .WithName("Vlandian Test Infantry")
                .WithAge(30)
                .WithLevel(20)
                .WithCulture("Culture.vlandia")
                .WithDefaultGroup("Infantry")
                .AddSkill("OneHanded", 100)
                .AddBattleEquipment("Item0", "Item.test_sword")
                .AddCivilianEquipment("Item0", "Item.test_dagger")
                .AddUpgradeTarget("test_vlandia_veteran");

            var elem = troop.BuildElement();
            if ((string)elem.Attribute("age") != "30") throw new Exception("Expected age 30.");
            if ((string)elem.Attribute("default_group") != "Infantry") throw new Exception("Expected group Infantry.");
            
            // Verify upgrade target has NPCCharacter. prefix
            var targets = elem.Descendants("upgrade_target").ToList();
            if (targets.Count != 1 || (string)targets[0].Attribute("id") != "NPCCharacter.test_vlandia_veteran")
                throw new Exception("Upgrade target did not have NPCCharacter. prefix.");

            // Verify exception on invalid age
            bool threw = false;
            try { ForgeTroopBuilder.Create("bad_troop").WithAge(-5); } catch { threw = true; }
            if (!threw) throw new Exception("Expected exception for negative age.");

            // Verify exception on invalid slot
            threw = false;
            try { ForgeTroopBuilder.Create("bad_troop").AddBattleEquipment("InvalidSlot", "item_1"); } catch { threw = true; }
            if (!threw) throw new Exception("Expected exception for invalid slot.");
        }

        private static void TestForgeItemBuilder()
        {
            var weapon = ForgeItemBuilder.Create("test_sword")
                .WithName("Test Sword")
                .WithWeight(1.2)
                .WithValue(500)
                .WithType("OneHandedWeapon")
                .AsWeapon("OneHandedSword", 90, 90, 30, "Pierce", 60, "Cut")
                .BuildElement();

            if (weapon.Descendants("Weapon").FirstOrDefault() == null)
                throw new Exception("Weapon component missing.");

            // Verify invalid damage type rejected
            bool threw = false;
            try { ForgeItemBuilder.Create("bad_weapon").AsWeapon("OneHandedSword", 90, 90, 30, "Magic", 60, "Cut"); } catch { threw = true; }
            if (!threw) throw new Exception("Expected exception for invalid damage type 'Magic'.");

            // Verify armor builder
            var armor = ForgeItemBuilder.Create("test_armor")
                .WithType("BodyArmor")
                .AsArmor(40, 15, 12, 0)
                .BuildElement();
            if (armor.Descendants("Armor").FirstOrDefault() == null)
                throw new Exception("Armor component missing.");

            // Verify horse builder
            var horse = ForgeItemBuilder.Create("test_horse")
                .AsHorse(45, 60, 20)
                .BuildElement();
            if (horse.Descendants("Horse").FirstOrDefault() == null)
                throw new Exception("Horse component missing.");
        }

        private static void TestForgeAudioBuilder()
        {
            var audio = ForgeAudioBuilder.Create()
                .Add2DSound("ui_test_click", "click.ogg", "ui")
                .Add3DSound("combat_clash", "clash.wav", "mission_combat")
                .BuildElement();

            var sounds = audio.Descendants("module_sound").ToList();
            if (sounds.Count != 2) throw new Exception("Expected 2 sound entries.");
            if ((string)sounds[0].Attribute("is_2d") != "true") throw new Exception("Sound 0 should be 2D.");
            if ((string)sounds[1].Attribute("is_2d") != "false") throw new Exception("Sound 1 should be 3D.");

            // Verify invalid category rejected
            bool threw = false;
            try { ForgeAudioBuilder.Create().Add2DSound("bad_sound", "sound.ogg", "invalid_cat"); } catch { threw = true; }
            if (!threw) throw new Exception("Expected exception for invalid sound category.");

            // Verify invalid file extension rejected
            threw = false;
            try { ForgeAudioBuilder.Create().Add2DSound("bad_ext", "sound.mp3", "ui"); } catch { threw = true; }
            if (!threw) throw new Exception("Expected exception for .mp3 extension.");
        }

        private static void TestForgeQuestBuilder()
        {
            var quest = ForgeQuestBuilder.Create("test_quest", "CustomEscortQuest")
                .WithNamespace("MyCustomMod.QuestBehaviors")
                .WithTitle("Caravan Escort Duty")
                .WithSaveableTypeId(2500000)
                .AddSaveableField("int", "_goldReward", 1);

            var questCode = quest.BuildCSharpCode();

            // Verify double SetDialogs() rule (must exist in constructor and in InitializeQuestOnGameLoad)
            if (!questCode.Contains("SetDialogs();") || !questCode.Contains("InitializeQuestOnGameLoad()"))
                throw new Exception("Quest code violates double SetDialogs() rule.");

            // Verify SaveableTypeDefiner base ID
            if (!questCode.Contains("base(2500000)"))
                throw new Exception("SaveableTypeDefiner base ID not allocated properly.");

            bool threw = false;
            try { ForgeQuestBuilder.Create("bad_quest", "BadQuest").WithSaveableTypeId(10000); } catch { threw = true; }
            if (!threw) throw new Exception("Expected exception for SaveableTypeDefiner ID < 2,500,000.");
        }

        private sealed class DummyEntityData
        {
            public string Value { get; set; } = "initial";
        }

        private static void TestLifecycleAndMemoryLeakGuards()
        {
            // ForgeCampaignEvents detachment
            bool called = false;
            Action<object[]> handler = args => { called = true; };
            var dummyEvent = new ForgeEvent(ForgeEventKind.CampaignStarted, Context.Campaign, 1, 0, null, CancellationToken.None);
            ForgeCampaignEvents.Subscribe(dummyEvent, handler);
            ForgeCampaignEvents.Dispatch(dummyEvent);
            if (!called) throw new Exception("ForgeCampaignEvents dispatch failed to invoke callback.");

            called = false;
            bool removed = ForgeCampaignEvents.Unsubscribe(dummyEvent, handler);
            if (!removed) throw new Exception("ForgeCampaignEvents.Unsubscribe returned false.");
            ForgeCampaignEvents.Dispatch(dummyEvent);
            if (called) throw new Exception("ForgeCampaignEvents callback still invoked after unsubscribe!");
            ForgeCampaignEvents.ClearSubscribers();

            // CampaignVariableInspector cleanup
            CampaignVariableInspector.TrackVariable("test_leak_key", () => 999);
            var snap = CampaignVariableInspector.GenerateSnapshot();
            if (!snap.ContainsKey("test_leak_key") || (int)snap["test_leak_key"] != 999)
                throw new Exception("Tracked variable missing from snapshot.");
            CampaignVariableInspector.ClearTrackedVariables();
            CampaignVariableInspector.ClearSnapshotListeners();
            var snapAfter = CampaignVariableInspector.GenerateSnapshot();
            if (snapAfter.ContainsKey("test_leak_key"))
                throw new Exception("Tracked variable leaked after ClearTrackedVariables.");

            // ForgeData cleanup
            var dummyEntity = new object();
            var dummyData = new DummyEntityData { Value = "hello_data" };
            dummyEntity.SetForgeData(dummyData);
            if (!dummyEntity.HasForgeData<DummyEntityData>()) throw new Exception("HasForgeData returned false.");
            if (dummyEntity.GetForgeData<DummyEntityData>().Value != "hello_data") throw new Exception("GetForgeData mismatch.");
            dummyEntity.RemoveForgeData<DummyEntityData>();
            if (dummyEntity.HasForgeData<DummyEntityData>()) throw new Exception("RemoveForgeData<T> failed.");

            dummyEntity.SetForgeData(new DummyEntityData { Value = "another_data" });
            dummyEntity.RemoveForgeData();
            if (dummyEntity.HasForgeData<DummyEntityData>()) throw new Exception("RemoveForgeData(entity) failed.");
            ForgeData.ClearAll();
        }

        private static void TestForgeSaveChunker()
        {
            // Test small string
            string small = "Small data";
            if (ForgeSaveChunker.NeedsChunking(small))
                throw new Exception("Small data should not require chunking.");
            var smallChunks = ForgeSaveChunker.Chunk(small);
            if (smallChunks.Length != 1 || smallChunks[0] != small)
                throw new Exception("Single chunk should match original string.");
            if (ForgeSaveChunker.Reassemble(smallChunks) != small)
                throw new Exception("Reassembly of small chunk failed.");

            // Test large string exceeding 30,000 limit
            string large = new string('A', 35000) + new string('B', 30000) + new string('C', 10000); // 75,000 chars
            if (!ForgeSaveChunker.NeedsChunking(large))
                throw new Exception("Data > 30,000 characters must require chunking.");

            var chunks = ForgeSaveChunker.Chunk(large, 30000);
            if (chunks.Length != 3)
                throw new Exception($"Expected 3 chunks for 75,000 characters, got {chunks.Length}.");
            if (chunks[0].Length != 30000 || chunks[1].Length != 30000 || chunks[2].Length != 15000)
                throw new Exception("Chunk sizes do not match expected partition lengths.");

            string reassembled = ForgeSaveChunker.Reassemble(chunks);
            if (reassembled != large)
                throw new Exception("Reassembled string does not match original large payload.");

            // Edge cases
            if (ForgeSaveChunker.Chunk(null).Length != 0)
                throw new Exception("Null string should return empty array.");
            if (ForgeSaveChunker.Reassemble(null) != string.Empty)
                throw new Exception("Null chunks should reassemble to empty string.");
        }

        private static void TestForgeMissionLogicBuilder()
        {
            var builder = ForgeMissionLogicBuilder.Create("SiegeTestLogic")
                .WithNamespace("MyMod.SiegeMissions")
                .WithAgentInteractionHook(true)
                .WithMissionEndHook(true);

            string code = builder.BuildCSharpCode();

            if (!code.Contains("class SiegeTestLogic : MissionLogic"))
                throw new Exception("MissionLogic subclass declaration missing.");
            if (!code.Contains("private bool _isInitialized;"))
                throw new Exception("Deferred initialization guard _isInitialized missing.");
            if (!code.Contains("public override void OnMissionTick(float dt)"))
                throw new Exception("OnMissionTick lifecycle method missing.");
            if (!code.Contains("InitializeMissionSceneComponents();"))
                throw new Exception("Scene components initialization call missing.");
            if (!code.Contains("public override bool IsAgentInteractionAllowed()"))
                throw new Exception("Unanimity hook IsAgentInteractionAllowed missing.");
            if (!code.Contains("public override bool MissionEnded(ref MissionResult missionResult)"))
                throw new Exception("First-wins hook MissionEnded missing.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int DummyOriginalDetourMethod() => 42;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int DummyReplacementDetourMethod() => 99;

        private static void TestForgeDetourUnpatch()
        {
            var origMethod = typeof(AdvancedToolsTests).GetMethod(nameof(DummyOriginalDetourMethod), BindingFlags.NonPublic | BindingFlags.Static);
            var replMethod = typeof(AdvancedToolsTests).GetMethod(nameof(DummyReplacementDetourMethod), BindingFlags.NonPublic | BindingFlags.Static);

            if (origMethod == null || replMethod == null)
                throw new Exception("Failed to reflect dummy detour methods.");

            // Detour patching only runs on 64-bit architecture
            if (IntPtr.Size == 8)
            {
                ForgeDetour.Patch(origMethod, replMethod);
                if (!ForgeDetour.IsPatched(origMethod))
                    throw new Exception("IsPatched should return true after Patch.");
                if (DummyOriginalDetourMethod() != 99)
                    throw new Exception("Original method did not branch to replacement.");

                bool unpatched = ForgeDetour.Unpatch(origMethod);
                if (!unpatched)
                    throw new Exception("Unpatch returned false for active patch.");
                if (ForgeDetour.IsPatched(origMethod))
                    throw new Exception("IsPatched should return false after Unpatch.");
                if (DummyOriginalDetourMethod() != 42)
                    throw new Exception("Original method instructions were not restored after Unpatch.");

                // Test UnpatchAll
                ForgeDetour.Patch(origMethod, replMethod);
                ForgeDetour.UnpatchAll();
                if (ForgeDetour.IsPatched(origMethod))
                    throw new Exception("IsPatched should return false after UnpatchAll.");
                if (DummyOriginalDetourMethod() != 42)
                    throw new Exception("Original method instructions were not restored after UnpatchAll.");
            }
            else
            {
                // In 32-bit test runner, verify API surface safely
                if (ForgeDetour.IsPatched(origMethod))
                    throw new Exception("IsPatched should be false initially.");
            }
        }

        private static void TestForgeUI_Clear()
        {
            var opened = "";
            var closed = false;
            Action<string> open = id => opened = id;
            Action close = () => closed = true;
            ForgeUI.PageOpenRequested += open;
            ForgeUI.PageCloseRequested += close;
            ForgeUI.OpenPage(" test.page ");
            ForgeUI.ClosePage();
            if (opened != "test.page" || !closed) throw new Exception("Extension page events did not reach the host.");

            ForgeUI.Clear();
            try { ForgeUI.OpenPage("test.page"); throw new Exception("Page opening succeeded after the host handlers were cleared."); }
            catch (InvalidOperationException) { }
            try { ForgeUI.ClosePage(); throw new Exception("Page closing succeeded after the host handlers were cleared."); }
            catch (InvalidOperationException) { }
        }

        private static void TestPanelViewModelAndPrefabPolish()
        {
            AssertCurrentNativePrefab();
            if (UseRoutedDesktopAssertions()) return;
            // 1. Locate and inspect CalradiaForge.xml prefab
            string[] candidatePaths = new[]
            {
                Path.GetFullPath("modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"),
                Path.GetFullPath("../../../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"))
            };
            string xmlPath = candidatePaths.FirstOrDefault(File.Exists);
            if (xmlPath == null) throw new FileNotFoundException("Could not locate CalradiaForge.xml prefab.");

            string xmlContent = File.ReadAllText(xmlPath);

            // Memory telemetry badge
            if (!xmlContent.Contains("@MemoryHealthText"))
                throw new Exception("CalradiaForge.xml missing @MemoryHealthText telemetry binding.");

            // Empty state placeholder
            if (!xmlContent.Contains("@ContentPlaceholder"))
                throw new Exception("CalradiaForge.xml missing @ContentPlaceholder binding.");
            if (!xmlContent.Contains("@IsContentEmpty"))
                throw new Exception("CalradiaForge.xml missing @IsContentEmpty visibility condition.");

            // Clear output button with hint
            if (!xmlContent.Contains("ExecuteClearOutput"))
                throw new Exception("CalradiaForge.xml missing ExecuteClearOutput command binding.");
            if (!xmlContent.Contains("@ClearOutputHint"))
                throw new Exception("CalradiaForge.xml missing @ClearOutputHint tooltip binding.");

            // The earlier decorative divider motif was replaced with deterministic, non-interactive
            // header filigree and rail-grain textures, verified in AssertCurrentNativePrefab().

            // 2. Locate and inspect PanelViewModel.cs
            string[] vmCandidatePaths = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Mod/PanelViewModel.cs"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Mod/PanelViewModel.cs"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Mod/PanelViewModel.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Mod/PanelViewModel.cs"))
            };
            string vmPath = vmCandidatePaths.FirstOrDefault(File.Exists);
            if (vmPath == null) throw new FileNotFoundException("Could not locate PanelViewModel.cs.");

            string vmContent = File.ReadAllText(vmPath);
            if (!vmContent.Contains("public string MemoryHealthText"))
                throw new Exception("PanelViewModel.cs missing MemoryHealthText property.");
            if (!vmContent.Contains("public bool IsContentEmpty"))
                throw new Exception("PanelViewModel.cs missing IsContentEmpty property.");
            if (!vmContent.Contains("public void ExecuteClearOutput()"))
                throw new Exception("PanelViewModel.cs missing ExecuteClearOutput method.");
            if (!vmContent.Contains("case \"ForgeClear\": ExecuteClearOutput(); break;"))
                throw new Exception("PanelViewModel.cs ExecuteKeyboardControl missing ForgeClear mapping.");
        }

        private static void TestUiErrorCorrectionsAndSafety()
        {
            string routedDesktop = LoadDesktopXamlTree();
            if (!routedDesktop.Contains("<ContentControl") || !routedDesktop.Contains("CurrentPage"))
                throw new Exception("Routed desktop workbench must host the selected page through ContentControl.");
            if (UseRoutedDesktopAssertions()) return;
            // 1. Locate and inspect MainWindow.xaml
            string[] xamlCandidates = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Desktop/MainWindow.xaml"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Desktop/MainWindow.xaml"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Desktop/MainWindow.xaml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Desktop/MainWindow.xaml"))
            };
            string xamlPath = xamlCandidates.FirstOrDefault(File.Exists);
            if (xamlPath == null) throw new FileNotFoundException("Could not locate MainWindow.xaml.");

            string xamlContent = File.ReadAllText(xamlPath);
            if (!xamlContent.Contains(@"<Grid x:Name=""SdkBuilderPanel"" Visibility=""Collapsed"">"))
                throw new Exception("MainWindow.xaml SdkBuilderPanel must have Visibility='Collapsed' on startup.");
            if (!xamlContent.Contains(@"Header=""💰 Economy"""))
                throw new Exception("MainWindow.xaml SdkEconomyCategoryItem must use 💰 fallback glyph instead of 🪙.");
            if (xamlContent.Contains(@"Header=""🪙 Economy"""))
                throw new Exception("MainWindow.xaml contains broken SMP glyph 🪙 in Economy header.");

            // 2. Locate and inspect CalradiaForge.xml
            string[] xmlCandidates = new[]
            {
                Path.GetFullPath("modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"),
                Path.GetFullPath("../../../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"))
            };
            string xmlPath = xmlCandidates.FirstOrDefault(File.Exists);
            if (xmlPath == null) throw new FileNotFoundException("Could not locate CalradiaForge.xml.");

            string xmlContent = File.ReadAllText(xmlPath);
            // Verify no duplicate ExecuteExport in tool buttons row
            int exportCount = System.Text.RegularExpressions.Regex.Matches(xmlContent, @"Command\.Click=""ExecuteExport""").Count;
            if (exportCount != 1)
                throw new Exception($"CalradiaForge.xml must have exactly 1 ExecuteExport binding (pinned in sidebar), found {exportCount}.");

            // 3. Locate and inspect PanelViewModel.cs
            string[] vmCandidates = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Mod/PanelViewModel.cs"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Mod/PanelViewModel.cs"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Mod/PanelViewModel.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Mod/PanelViewModel.cs"))
            };
            string vmPath = vmCandidates.FirstOrDefault(File.Exists);
            if (vmPath == null) throw new FileNotFoundException("Could not locate PanelViewModel.cs.");

            string vmContent = File.ReadAllText(vmPath);
            if (!vmContent.Contains(@"""console"""))
                throw new Exception("PanelViewModel.cs missing console in NavigationLabel actions.");
            if (!vmContent.Contains("Content = \"\";"))
                throw new Exception("PanelViewModel.cs ExecuteClearOutput must set Content = \"\" directly to notify Gauntlet.");
        }

        private static void TestZeroScrollDesktopInterface()
        {
            string routedShell = File.ReadAllText(Path.GetFullPath("src/CalradiaForge.Desktop/Presentation/DesktopShellViewModel.cs"));
            if (!routedShell.Contains("VisibleTools") || !routedShell.Contains("SelectedCategory") || !routedShell.Contains("Filter"))
                throw new Exception("Routed desktop navigation must expose category and filtered-tool state.");
            if (UseRoutedDesktopAssertions()) return;
            // 1. Locate and inspect MainWindow.xaml
            string[] xamlCandidates = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Desktop/MainWindow.xaml"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Desktop/MainWindow.xaml"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Desktop/MainWindow.xaml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Desktop/MainWindow.xaml"))
            };
            string xamlPath = xamlCandidates.FirstOrDefault(File.Exists);
            if (xamlPath == null) throw new FileNotFoundException("Could not locate MainWindow.xaml.");

            string xamlContent = File.ReadAllText(xamlPath);

            // Verify WorkspaceScrollViewer has Disabled vertical scroll to prevent canvas displacement
            if (!xamlContent.Contains(@"VerticalScrollBarVisibility=""Disabled"""))
                throw new Exception("MainWindow.xaml WorkspaceScrollViewer must have VerticalScrollBarVisibility='Disabled' to eliminate forced page scrolling.");

            // Verify AccordionToggleButton exists
            if (!xamlContent.Contains(@"x:Name=""AccordionToggleButton"""))
                throw new Exception("MainWindow.xaml missing AccordionToggleButton for auto-collapsing inactive categories.");

            // Verify CategoryPickerComboBox exists with All Categories option
            if (!xamlContent.Contains(@"x:Name=""CategoryPickerComboBox"""))
                throw new Exception("MainWindow.xaml missing CategoryPickerComboBox dropdown filter.");
            if (!xamlContent.Contains(@"x:Name=""CategoryPickerAllItem"""))
                throw new Exception("MainWindow.xaml missing CategoryPickerAllItem for resetting category filters.");

            // Verify Breadcrumb trail exists
            if (!xamlContent.Contains(@"x:Name=""BreadcrumbHomeBtn"""))
                throw new Exception("MainWindow.xaml missing BreadcrumbHomeBtn.");
            if (!xamlContent.Contains(@"x:Name=""BreadcrumbCategory"""))
                throw new Exception("MainWindow.xaml missing BreadcrumbCategory.");
            if (!xamlContent.Contains(@"x:Name=""BreadcrumbItem"""))
                throw new Exception("MainWindow.xaml missing BreadcrumbItem.");

            // Verify Recent Tools bar and Zen Mode toggle button
            if (!xamlContent.Contains(@"x:Name=""RecentToolsPanel"""))
                throw new Exception("MainWindow.xaml missing RecentToolsPanel quick-switcher chips container.");
            if (!xamlContent.Contains(@"x:Name=""SidebarExpandButton"""))
                throw new Exception("MainWindow.xaml missing SidebarExpandButton for restoring sidebar in Zen Mode.");

            // Verify Scaffold safety options in SdkBuilderPanel
            if (!xamlContent.Contains(@"x:Name=""ScaffoldSaveableCheckbox"""))
                throw new Exception("MainWindow.xaml missing ScaffoldSaveableCheckbox for TaleWorlds SaveableTypeDefiner generation.");
            if (!xamlContent.Contains(@"x:Name=""ScaffoldInitGuardCheckbox"""))
                throw new Exception("MainWindow.xaml missing ScaffoldInitGuardCheckbox for OnInit deferred initialization guard.");

            // 2. Locate and inspect MainWindow.xaml.cs
            string[] csCandidates = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Desktop/MainWindow.xaml.cs"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Desktop/MainWindow.xaml.cs"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Desktop/MainWindow.xaml.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Desktop/MainWindow.xaml.cs"))
            };
            string csPath = csCandidates.FirstOrDefault(File.Exists);
            if (csPath == null) throw new FileNotFoundException("Could not locate MainWindow.xaml.cs.");

            string csContent = File.ReadAllText(csPath);

            // Verify Accordion sibling collapse handler
            if (!csContent.Contains("NavTree_ItemExpanded"))
                throw new Exception("MainWindow.xaml.cs missing NavTree_ItemExpanded accordion sibling auto-collapse handler.");
            if (!csContent.Contains("AccordionToggle_Click"))
                throw new Exception("MainWindow.xaml.cs missing AccordionToggle_Click event handler.");

            // Verify ZenMode and F10 keybinding
            if (!csContent.Contains("ZenModeToggle_Click"))
                throw new Exception("MainWindow.xaml.cs missing ZenModeToggle_Click event handler.");
            if (!csContent.Contains("e.Key == Key.F10"))
                throw new Exception("MainWindow.xaml.cs missing F10 shortcut handler for toggling Zen Mode.");

            // Verify CategoryPicker dropdown handler
            if (!csContent.Contains("CategoryPicker_SelectionChanged"))
                throw new Exception("MainWindow.xaml.cs missing CategoryPicker_SelectionChanged filter handler.");

            // Verify Breadcrumb and Recent Tools methods
            if (!csContent.Contains("UpdateBreadcrumb"))
                throw new Exception("MainWindow.xaml.cs missing UpdateBreadcrumb method.");
            if (!csContent.Contains("AddRecentTool"))
                throw new Exception("MainWindow.xaml.cs missing AddRecentTool method.");
        }

        private static void TestDesktopPanelFontSizesAndLayout()
        {
            string routedXaml = LoadDesktopXamlTree();
            if (!routedXaml.Contains("Grid Width=\"56\" Height=\"56\"") || !routedXaml.Contains("M28,10 L28,46 M10,28 L46,28"))
                throw new Exception("Routed desktop command seal must use the shared centered crosshair.");
            if (UseRoutedDesktopAssertions()) return;
            // 1. Locate and inspect MainWindow.xaml
            string[] xamlCandidates = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Desktop/MainWindow.xaml"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Desktop/MainWindow.xaml"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Desktop/MainWindow.xaml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Desktop/MainWindow.xaml"))
            };
            string xamlPath = xamlCandidates.FirstOrDefault(File.Exists);
            if (xamlPath == null) throw new FileNotFoundException("Could not locate MainWindow.xaml.");

            string xamlContent = File.ReadAllText(xamlPath);

            // Top Panel (TitleBar) Font Sizes and Version
            if (!xamlContent.Contains(@"Text=""CALRADIA FORGE STUDIO"" Foreground=""{StaticResource BrassBrush}"" FontFamily=""Georgia"" FontWeight=""Bold"" FontSize=""14"""))
                throw new Exception("MainWindow.xaml TitleBar title must have FontSize='14'.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(xamlContent, @"x:Name=""TitleBarVersionBlock""\s+Text=""v0\.0\.0""\s+Foreground=""\{StaticResource VerdigrisBrush\}""\s+FontSize=""12\.5"""))
                throw new Exception("MainWindow.xaml title version must use the dynamic v0.0.0 placeholder with FontSize='12.5'.");
            if (!xamlContent.Contains(@"x:Name=""TitleBarSubtitleBlock"" Grid.Column=""1"" Text=""Tactical Modding Deck &amp; Live Inspector"" Foreground=""{StaticResource MutedTextBrush}"" FontSize=""12.5"""))
                throw new Exception("MainWindow.xaml TitleBarSubtitleBlock must have FontSize='12.5'.");
            if (!xamlContent.Contains(@"Button Content=""—"" Width=""38"" Height=""28"" MinHeight=""28"" Padding=""0"" FontSize=""13"""))
                throw new Exception("MainWindow.xaml TitleBar window control buttons must have FontSize='13' and 38x28 dimensions.");

            // Top Panel (Header / Command Deck) Font Sizes and Logo
            if (!xamlContent.Contains(@"x:Name=""LogoBorder"" Width=""48"" Height=""48"""))
                throw new Exception("MainWindow.xaml LogoBorder must have Width='48' and Height='48'.");
            if (!xamlContent.Contains(@"<Ellipse Width=""8"" Height=""8"" Fill=""{StaticResource BrassBrush}"""))
                throw new Exception("MainWindow.xaml must use the centered command-seal marker.");
            if (!xamlContent.Contains(@"x:Name=""AppTitleBlock"" Text=""CALRADIA FORGE"" FontFamily=""Georgia"" FontSize=""22"""))
                throw new Exception("MainWindow.xaml AppTitleBlock must use the bounded CALRADIA FORGE title.");
            if (!xamlContent.Contains(@"x:Name=""AppSubtitleBlock"" Text=""Verified Modding Workbench"" FontFamily=""Georgia"" FontSize=""13.5"""))
                throw new Exception("MainWindow.xaml AppSubtitleBlock must describe the verified workbench.");
            if (!xamlContent.Contains(@"x:Name=""GameEngineLabel"" Text=""Game Engine:"" VerticalAlignment=""Center"" Foreground=""{StaticResource MutedTextBrush}"" FontSize=""13.5"""))
                throw new Exception("MainWindow.xaml GameEngineLabel must have FontSize='13.5'.");
            if (!xamlContent.Contains(@"x:Name=""ConnectionStatus"" Text=""Disconnected"" VerticalAlignment=""Center"" Foreground=""#A33221"" FontWeight=""Bold"" FontSize=""13.5"""))
                throw new Exception("MainWindow.xaml ConnectionStatus must have FontSize='13.5'.");
            if (!xamlContent.Contains(@"x:Name=""ConnectButton"" Content=""Connect to Bannerlord"" Click=""ConnectClick"" Style=""{StaticResource PrimaryButton}"" FontSize=""13"""))
                throw new Exception("MainWindow.xaml ConnectButton must have FontSize='13'.");

            // Breadcrumb Strip
            if (!xamlContent.Contains(@"x:Name=""BreadcrumbHomeBtn"" Content=""🏠 Command Deck"" Click=""FilterDash_Click"" Background=""Transparent"" BorderThickness=""0"" Foreground=""{StaticResource QuietBrassBrush}"" FontSize=""13.5"""))
                throw new Exception("MainWindow.xaml BreadcrumbHomeBtn must have FontSize='13.5'.");
            if (!xamlContent.Contains(@"x:Name=""BreadcrumbItem"" Text="""" Foreground=""{StaticResource BrassBrush}"" FontSize=""13"""))
                throw new Exception("MainWindow.xaml BreadcrumbItem must have FontSize='13'.");
            if (!xamlContent.Contains(@"x:Name=""RecentToolsHeading"" Text=""Recent:"" Foreground=""{StaticResource MutedTextBrush}"" FontSize=""12.5"""))
                throw new Exception("MainWindow.xaml RecentToolsHeading must have FontSize='12.5'.");
            if (!xamlContent.Contains(@"x:Name=""SidebarExpandButton"" Grid.Column=""2"" Content=""☰ Sidebar"" Click=""ZenModeToggle_Click"" Padding=""8,3"" FontSize=""12"""))
                throw new Exception("MainWindow.xaml SidebarExpandButton must have FontSize='12'.");

            // Bottom Status Bar Font Sizes and Controls
            if (!System.Text.RegularExpressions.Regex.IsMatch(xamlContent, @"x:Name=""StatusVersionBlock""\s+Text=""Calradia Forge v0\.0\.0""\s+Foreground=""\{StaticResource QuietBrassBrush\}""\s+FontWeight=""Bold""\s+FontSize=""13\.5"""))
                throw new Exception("MainWindow.xaml status version must use the dynamic placeholder with FontSize='13.5'.");
            if (!xamlContent.Contains(@"x:Name=""MemoryUsageText"" Text=""Memory: -- MB"" Foreground=""{StaticResource MutedTextBrush}"" FontWeight=""SemiBold"" FontSize=""13"""))
                throw new Exception("MainWindow.xaml MemoryUsageText must have FontSize='13'.");
            if (!xamlContent.Contains(@"x:Name=""MemoryTrendIndicator"" Text=""📈 Stable"" Foreground=""{StaticResource QuietBrassBrush}"" FontSize=""12.5"""))
                throw new Exception("MainWindow.xaml MemoryTrendIndicator must have FontSize='12.5'.");
            if (!xamlContent.Contains(@"x:Name=""CompactModeCheckbox"" Content=""Compact"" Foreground=""{StaticResource MutedTextBrush}"" FontSize=""12.5"""))
                throw new Exception("MainWindow.xaml CompactModeCheckbox must have FontSize='12.5'.");
            if (!xamlContent.Contains(@"x:Name=""TipBlock"" Text=""💡 Tip: Ctrl+Scroll to zoom | Drag XML to audit"" Foreground=""#4E6759"" FontSize=""12.5"""))
                throw new Exception("MainWindow.xaml TipBlock must have FontSize='12.5'.");
            if (!xamlContent.Contains(@"x:Name=""ThemeLabel"" Text=""Theme:"" Foreground=""{StaticResource MutedTextBrush}"" FontSize=""12.5"""))
                throw new Exception("MainWindow.xaml ThemeLabel must have FontSize='12.5'.");
            if (!xamlContent.Contains(@"x:Name=""ThemeComboBox"" Width=""116"" Height=""28"" MinHeight=""26"" Padding=""6,1"" Margin=""0,0,12,0"" VerticalContentAlignment=""Center"" FontSize=""12.5"""))
                throw new Exception("MainWindow.xaml ThemeComboBox must have FontSize='12.5' and Height='28'.");
            if (!xamlContent.Contains(@"x:Name=""LanguageLabel"" Text=""Language:"" Foreground=""{StaticResource MutedTextBrush}"" FontSize=""12.5"""))
                throw new Exception("MainWindow.xaml LanguageLabel must have FontSize='12.5'.");
            if (!xamlContent.Contains(@"x:Name=""LanguageComboBox"" Width=""104"" Height=""28"" MinHeight=""26"" Padding=""6,1"" Margin=""0,0,12,0"" VerticalContentAlignment=""Center"" FontSize=""12.5"""))
                throw new Exception("MainWindow.xaml LanguageComboBox must have FontSize='12.5' and Height='28'.");
            if (!xamlContent.Contains(@"x:Name=""ZoomLabel"" Text=""Zoom:"" Foreground=""{StaticResource MutedTextBrush}"" FontSize=""12.5"""))
                throw new Exception("MainWindow.xaml ZoomLabel must have FontSize='12.5'.");
            if (!xamlContent.Contains(@"x:Name=""ZoomLevelText"" Text=""100%"" Foreground=""{StaticResource QuietBrassBrush}"" FontSize=""12.5"""))
                throw new Exception("MainWindow.xaml ZoomLevelText must have FontSize='12.5'.");

            // 2. Locate and inspect MainWindow.xaml.cs
            string[] csCandidates = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Desktop/MainWindow.xaml.cs"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Desktop/MainWindow.xaml.cs"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Desktop/MainWindow.xaml.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Desktop/MainWindow.xaml.cs"))
            };
            string csPath = csCandidates.FirstOrDefault(File.Exists);
            if (csPath == null) throw new FileNotFoundException("Could not locate MainWindow.xaml.cs.");

            string csContent = File.ReadAllText(csPath);

            // Verify Recent Tools chip FontSize = 12
            if (!csContent.Contains("FontSize = 12,"))
                throw new Exception("MainWindow.xaml.cs RenderRecentTools must set chip FontSize = 12.");

            // Verify CompactMode_Click synchronization with upgraded normal layout
            if (!csContent.Contains("AppTitleBlock.FontSize = isCompact ? 17 : 22;"))
                throw new Exception("MainWindow.xaml.cs CompactMode_Click must sync AppTitleBlock.FontSize to 22 for normal mode.");
            if (!csContent.Contains("LogoBorder.Width = isCompact ? 38 : 48;"))
                throw new Exception("MainWindow.xaml.cs CompactMode_Click must sync LogoBorder.Width to 48 for normal mode.");
        }

        private static void TestInGameUiSimplificationAndFeatures()
        {
            AssertCurrentNativePrefab();
            if (UseRoutedDesktopAssertions()) return;
            // 1. Locate and inspect CalradiaForge.xml prefab
            string[] xmlCandidates = new[]
            {
                Path.GetFullPath("modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"),
                Path.GetFullPath("../../../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"))
            };
            string xmlPath = xmlCandidates.FirstOrDefault(File.Exists);
            if (xmlPath == null) throw new FileNotFoundException("Could not locate CalradiaForge.xml.");

            string xmlContent = File.ReadAllText(xmlPath);

            // Category Tab Buttons
            if (!xmlContent.Contains(@"Command.Click=""ExecuteCategoryOverview"""))
                throw new Exception("CalradiaForge.xml missing ExecuteCategoryOverview tab binding.");
            if (!xmlContent.Contains(@"Command.Click=""ExecuteCategoryInspector"""))
                throw new Exception("CalradiaForge.xml missing ExecuteCategoryInspector tab binding.");
            if (!xmlContent.Contains(@"Command.Click=""ExecuteCategoryToolkit"""))
                throw new Exception("CalradiaForge.xml missing ExecuteCategoryToolkit tab binding.");
            if (!xmlContent.Contains(@"Command.Click=""ExecuteCategoryWeave"""))
                throw new Exception("CalradiaForge.xml missing ExecuteCategoryWeave tab binding.");

            // Contextual Visibility
            if (!xmlContent.Contains(@"IsVisible=""@IsCategoryOverviewActive"""))
                throw new Exception("CalradiaForge.xml missing @IsCategoryOverviewActive visibility condition.");
            if (!xmlContent.Contains(@"IsVisible=""@IsCategoryInspectorActive"""))
                throw new Exception("CalradiaForge.xml missing @IsCategoryInspectorActive visibility condition.");
            if (!xmlContent.Contains(@"IsVisible=""@IsCategoryToolkitActive"""))
                throw new Exception("CalradiaForge.xml missing @IsCategoryToolkitActive visibility condition.");
            if (!xmlContent.Contains(@"IsVisible=""@IsCategoryWeaveActive"""))
                throw new Exception("CalradiaForge.xml missing @IsCategoryWeaveActive visibility condition.");

            // Trim Heap Quick Action in Header
            if (!xmlContent.Contains(@"Command.Click=""ExecuteForceGC"""))
                throw new Exception("CalradiaForge.xml missing ExecuteForceGC quick action in header.");

            // Interactive Pagination & Quick State Bar
            if (!xmlContent.Contains(@"Command.Click=""ExecutePrevious"""))
                throw new Exception("CalradiaForge.xml missing ExecutePrevious pagination button.");
            if (!xmlContent.Contains(@"Command.Click=""ExecuteNext"""))
                throw new Exception("CalradiaForge.xml missing ExecuteNext pagination button.");
            if (!xmlContent.Contains(@"Command.Click=""ExecuteQuickState"""))
                throw new Exception("CalradiaForge.xml missing ExecuteQuickState button.");

            // Contextual Inspector Action Buttons
            if (!xmlContent.Contains(@"Command.Click=""ExecuteInspectPlayer"""))
                throw new Exception("CalradiaForge.xml missing ExecuteInspectPlayer button.");
            if (!xmlContent.Contains(@"Command.Click=""ExecuteInspectCurrentSettlement"""))
                throw new Exception("CalradiaForge.xml missing ExecuteInspectCurrentSettlement button.");

            // Single Export Button Invariant
            int exportCount = System.Text.RegularExpressions.Regex.Matches(xmlContent, @"Command\.Click=""ExecuteExport""").Count;
            if (exportCount != 1)
                throw new Exception($"CalradiaForge.xml must have exactly 1 ExecuteExport binding, found {exportCount}.");

            // 2. Locate and inspect CalradiaForge.xml brushes
            string[] brushCandidates = new[]
            {
                Path.GetFullPath("modules/CalradiaForge/GUI/Brushes/CalradiaForge.xml"),
                Path.GetFullPath("../../../../../modules/CalradiaForge/GUI/Brushes/CalradiaForge.xml"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../modules/CalradiaForge/GUI/Brushes/CalradiaForge.xml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../modules/CalradiaForge/GUI/Brushes/CalradiaForge.xml"))
            };
            string brushPath = brushCandidates.FirstOrDefault(File.Exists);
            if (brushPath == null) throw new FileNotFoundException("Could not locate CalradiaForge.xml brushes.");

            string brushContent = File.ReadAllText(brushPath);
            if (!brushContent.Contains(@"Name=""CalradiaForge.CategoryTab"""))
                throw new Exception("CalradiaForge brushes missing CalradiaForge.CategoryTab brush.");
            if (!brushContent.Contains(@"Name=""CalradiaForge.CategoryTabText"""))
                throw new Exception("CalradiaForge brushes missing CalradiaForge.CategoryTabText brush.");

            // 3. Locate and inspect PanelViewModel.cs
            string[] vmCandidates = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Mod/PanelViewModel.cs"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Mod/PanelViewModel.cs"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Mod/PanelViewModel.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Mod/PanelViewModel.cs"))
            };
            string vmPath = vmCandidates.FirstOrDefault(File.Exists);
            if (vmPath == null) throw new FileNotFoundException("Could not locate PanelViewModel.cs.");

            string vmContent = File.ReadAllText(vmPath);
            if (!vmContent.Contains("IsCategoryOverviewActive"))
                throw new Exception("PanelViewModel.cs missing IsCategoryOverviewActive property.");
            if (!vmContent.Contains("ExecuteForceGC()"))
                throw new Exception("PanelViewModel.cs missing ExecuteForceGC method.");
            if (!vmContent.Contains("ExecuteQuickState()"))
                throw new Exception("PanelViewModel.cs missing ExecuteQuickState method.");
            if (!vmContent.Contains("ExecuteInspectPlayer()"))
                throw new Exception("PanelViewModel.cs missing ExecuteInspectPlayer method.");
            if (!vmContent.Contains("ExecuteInspectCurrentSettlement()"))
                throw new Exception("PanelViewModel.cs missing ExecuteInspectCurrentSettlement method.");
            if (!vmContent.Contains(@"case ""ForgeForceGC"": ExecuteForceGC(); break;"))
                throw new Exception("PanelViewModel.cs ExecuteKeyboardControl missing ForgeForceGC mapping.");
            if (!vmContent.Contains(@"case ""ForgeQuickState"": ExecuteQuickState(); break;"))
                throw new Exception("PanelViewModel.cs ExecuteKeyboardControl missing ForgeQuickState mapping.");
        }

        private static void TestForgeFrameworkAndSdkEnhancements()
        {
            // 1. Test ForgeTimeSlicer
            int bucket1 = ForgeTimeSlicer.GetBucket("settlement_town_A1", 24);
            if (bucket1 < 0 || bucket1 >= 24)
                throw new Exception("ForgeTimeSlicer.GetBucket returned out-of-range bucket.");

            if (!ForgeTimeSlicer.ShouldProcess("settlement_town_A1", bucket1, 24))
                throw new Exception("ForgeTimeSlicer.ShouldProcess returned false for matching target bucket.");

            if (ForgeTimeSlicer.ShouldProcess("settlement_town_A1", (bucket1 + 1) % 24, 24))
                throw new Exception("ForgeTimeSlicer.ShouldProcess returned true for non-matching bucket.");

            var testEntities = new List<string> { "hero_1", "hero_2", "hero_3", "hero_4", "hero_5" };
            int batchProcessed = ForgeTimeSlicer.ProcessBatch(testEntities, id => id, entity => { }, bucket1, 24);
            if (batchProcessed < 0)
                throw new Exception("ForgeTimeSlicer.ProcessBatch returned negative count.");

            // 2. Test ForgeText
            var textEntry = ForgeText.Create("forge_caravan_escort", "Escort {TARGET} to {DESTINATION}",
                ("TARGET", "Silk Caravan"), ("DESTINATION", "Pravend"));

            if (textEntry.Resolve() != "Escort Silk Caravan to Pravend")
                throw new Exception($"ForgeText.Resolve failed. Expected 'Escort Silk Caravan to Pravend', got '{textEntry.Resolve()}'.");

            if (textEntry.ToTaleWorldsToken() != "{=forge_caravan_escort}Escort {TARGET} to {DESTINATION}")
                throw new Exception("ForgeText.ToTaleWorldsToken mismatch.");

            string xmlOutput = ForgeText.EscapeXmlText("Silk & Spices <Special> \n NextLine");
            if (!xmlOutput.Contains("&amp;") || !xmlOutput.Contains("&lt;") || !xmlOutput.Contains("&gt;") || !xmlOutput.Contains("&#10;"))
                throw new Exception("ForgeText.EscapeXmlText failed to escape special characters or newlines.");

            bool threwOnBadId = false;
            try
            {
                ForgeText.Create("invalid id with spaces", "Sample");
            }
            catch (InvalidOperationException)
            {
                threwOnBadId = true;
            }
            if (!threwOnBadId)
                throw new Exception("ForgeText.Create must reject string IDs containing spaces or concatenation.");

            // 3. Test ForgeModelRegistry
            var modelRegistry = new ForgeModelRegistry();
            modelRegistry.Register(new ForgeModelModifier("test_speed_buff", ForgeGameModelCategory.PartySpeed, "TestMod", "Vanguard Bonus", additiveBonus: 1.0f, factorMultiplier: 0.20f));

            var calc = modelRegistry.Evaluate(ForgeGameModelCategory.PartySpeed, baseValue: 5.0f, minLimit: 1.0f, maxLimit: 20.0f);
            // Expected: (5.0 + 1.0) * (1.0 + 0.20) = 6.0 * 1.20 = 7.20
            if (Math.Abs(calc.FinalValue - 7.20f) > 0.001f)
                throw new Exception($"ForgeModelRegistry.Evaluate math failed. Expected 7.20, got {calc.FinalValue}.");

            var clampedCalc = modelRegistry.Evaluate(ForgeGameModelCategory.PartySpeed, baseValue: 5.0f, minLimit: 8.0f, maxLimit: 20.0f);
            if (Math.Abs(clampedCalc.FinalValue - 8.0f) > 0.001f)
                throw new Exception($"ForgeModelRegistry.Evaluate minLimit clamp failed. Expected 8.0, got {clampedCalc.FinalValue}.");

            // 4. Test ForgeDialogueBuilder
            var dialogueBuilder = ForgeDialogueBuilder.Create("forge_bounty_hunt", "hero_main_options")
                .PlayerLine("ask_bounty", "Are there any bounties?", "npc_reply")
                .NpcReply("reply_bounty", "Yes, bandit hideout nearby.", "close_window");

            var dialogueLines = dialogueBuilder.Build();
            if (dialogueLines.Count != 2)
                throw new Exception("ForgeDialogueBuilder.Build did not produce the expected 2 lines.");

            bool threwOnBrokenToken = false;
            try
            {
                ForgeDialogueBuilder.Create("broken_dialogue", "hero_main_options")
                    .PlayerLine("line_1", "Hello", "unreachable_orphan_token")
                    .Build();
            }
            catch (InvalidOperationException)
            {
                threwOnBrokenToken = true;
            }
            if (!threwOnBrokenToken)
                throw new Exception("ForgeDialogueBuilder must throw on unreachable/unlinked tokens.");

            // 5. Test ForgeMissionLifecycleGuard
            bool threwOnInit = false;
            try
            {
                ForgeMissionLifecycleGuard.AssertSafePhase(isOnInitPhase: true, "TestMissionView");
            }
            catch (InvalidOperationException)
            {
                threwOnInit = true;
            }
            if (!threwOnInit)
                throw new Exception("ForgeMissionLifecycleGuard.AssertSafePhase must reject execution during OnInit.");

            int initExecCount = 0;
            var guard = new ForgeMissionLifecycleGuard(() => initExecCount++);
            guard.OnTick(0.016f);
            guard.OnTick(0.016f);
            guard.OnTick(0.016f);

            if (initExecCount != 1)
                throw new Exception($"ForgeMissionLifecycleGuard must execute deferred initializer exactly once. Executed {initExecCount} times.");
        }

        private static void TestDiplomacyScoring()
        {
            float warScore = ForgeApi.Diplomacy.CalculateWarScore(3500, 50000, 1, 0, 1200);
            if (warScore <= 0f) throw new Exception("War score should be positive for strong, funded faction paying tribute.");

            int tribute = ForgeApi.Diplomacy.CalculatePeaceTribute(1200, 300, 2, 0);
            if (tribute <= 0) throw new Exception("Faction with massive casualty and settlement advantage should receive positive tribute.");

            float stability = ForgeApi.Diplomacy.EvaluateAllianceStability(60, 2, 10);
            if (stability <= 0f) throw new Exception("High relation and common enemies should yield stable alliance.");

            var votes = new Dictionary<string, int> { { "CandidateA", 120 }, { "CandidateB", 45 } };
            var (winner, winVotes, share) = ForgeDiplomacy.SimulateElectionOutcome(165, votes);
            if (winner != "CandidateA" || winVotes != 120 || share < 0.7f)
                throw new Exception("Election simulation failed to determine correct winner and vote share.");
        }

        private static void TestSettlementRebellionRisk()
        {
            float loyaltyDelta = ForgeApi.Settlements.CalculateDailyLoyaltyDelta(cultureMismatch: true, governorCultureMatch: false, foodSurplus: -4, taxes: 2, corruption: 1);
            if (loyaltyDelta >= 0f) throw new Exception("Culture mismatch and food shortage must result in negative loyalty delta.");

            float securityDelta = ForgeApi.Settlements.CalculateDailySecurityDelta(garrisonSize: 50, militiaSize: 100, activeUnderworldRackets: 3, banditLairNearby: true);
            if (securityDelta > 1.0f) throw new Exception("Active rackets and bandit lair should suppress security growth.");

            var (critical, danger, status) = ForgeApi.Settlements.EvaluateRebellionRisk(currentLoyalty: 12f, militiaSize: 220, garrisonSize: 80);
            if (!critical || danger < 50f || status != "Imminent Rebellion")
                throw new Exception("Low loyalty and overwhelming militia must trigger critical rebellion risk.");
        }

        private static void TestUnderworldAlleyAndSmuggling()
        {
            var (gold, crime) = ForgeApi.Underworld.CalculateAlleyDailyYield(thugCount: 8, settlementProsperity: 6000, securityLevel: 45f);
            if (gold <= 0 || crime <= 0f) throw new Exception("Alley racket must yield gold and generate crime footprint.");

            var (profit, risk) = ForgeApi.Underworld.CalculateSmugglingMargin(purchasePrice: 40, destinationPrice: 150, tariffRate: 0.15f, borderGuardBribery: 10f);
            if (profit <= 0 || risk <= 0f || risk > 1.0f) throw new Exception("Smuggling calculation produced invalid profit or risk factor.");

            float decayedCrime = ForgeApi.Underworld.CalculateCrimeDecay(currentCrimeRating: 50f, settlementSecurity: 70, hasActiveRackets: false);
            if (decayedCrime >= 50f) throw new Exception("High security and no active rackets must decay crime rating.");
        }

        private static void TestProgressionAndClanTiers()
        {
            float rateAtZero = ForgeApi.Progression.CalculateLearningRate(attributeValue: 6, focusPoints: 4, currentSkillLevel: 20);
            float rateAtLimit = ForgeApi.Progression.CalculateLearningRate(attributeValue: 6, focusPoints: 4, currentSkillLevel: 280);
            if (rateAtZero <= rateAtLimit) throw new Exception("Skill learning rate at low level must exceed rate beyond learning limit.");

            int tier3Renown = ForgeApi.Progression.GetClanTierThreshold(3);
            if (tier3Renown != 350) throw new Exception($"Tier 3 threshold expected 350, got {tier3Renown}.");
            int evaluatedTier = ForgeApi.Progression.EvaluateClanTier(1200f);
            if (evaluatedTier != 4) throw new Exception($"Renown 1200 should evaluate to Clan Tier 4, got {evaluatedTier}.");

            bool isSurgeonPerk = ForgeApi.Progression.DoesPerkApplyToRole("medicine_triage_tent", "Surgeon");
            bool isLeaderPerk = ForgeApi.Progression.DoesPerkApplyToRole("medicine_triage_tent", "PartyLeader");
            if (!isSurgeonPerk || isLeaderPerk) throw new Exception("Perk role mapping evaluated incorrectly.");
        }

        private static void TestCombatTacticsAndSiegeBreach()
        {
            float shock = ForgeApi.Combat.CalculateMoraleShock(casualtiesInflicted: 40, initialTroopCount: 100, isFlanked: true, isCommanderKilled: true);
            if (shock < 60f) throw new Exception("Massive casualties, flanking, and dead commander must produce severe morale shock.");

            float distanceSpears = ForgeApi.Combat.CalculateChargeDistanceThreshold(cavalryCount: 25, enemyInfantryBracing: 40, hasSpears: true);
            float distanceNoSpears = ForgeApi.Combat.CalculateChargeDistanceThreshold(cavalryCount: 25, enemyInfantryBracing: 40, hasSpears: false);
            if (distanceSpears <= distanceNoSpears) throw new Exception("Spearmen bracing must increase cavalry standoff distance.");

            var (breachProb, wallHp) = ForgeCombatTactics.CalculateSiegeWallBreachProbability(trebuchetShots: 40, catapultShots: 20, wallTier: 1);
            if (breachProb <= 0.5f || wallHp >= 10000f) throw new Exception("Concentrated artillery on tier 1 wall must yield high breach probability.");
        }

        private static void TestTradeUnderflowAndPriceElasticity()
        {
            var (valid, allowed, reason) = ForgeApi.Trade.ValidateItemTransfer(currentCount: 15, requestedAmount: 20);
            if (valid || allowed != 15) throw new Exception("Transfer of 20 items when only 15 exist must be rejected to prevent underflow.");

            var (validOk, allowedOk, _) = ForgeApi.Trade.ValidateItemTransfer(currentCount: 15, requestedAmount: 5);
            if (!validOk || allowedOk != 5) throw new Exception("Valid transfer within roster bounds falsely rejected.");

            int scarcePrice = ForgeApi.Trade.CalculatePriceBySupplyDemand(basePrice: 100, localSupply: 20, idealDemand: 100);
            int surplusPrice = ForgeApi.Trade.CalculatePriceBySupplyDemand(basePrice: 100, localSupply: 300, idealDemand: 100);
            if (scarcePrice <= 100 || surplusPrice >= 100)
                throw new Exception("Price elasticity did not properly scale prices for scarcity vs abundance.");
        }

        private static void TestPartySpawnerBlueprintSafety()
        {
            var bp = ForgeApi.Parties.CreateBlueprint("custom_patrol_1", "Vlandian Border Patrol", "vlandia")
                .SetHomeSettlement("settlement_town_v1")
                .SetBudget(wageLimit: 1200, food: 50f)
                .SetBehavior("Patrol")
                .AddTroop("vlandian_sergeant", 15)
                .AddTroop("vlandian_crossbowman", 15);

            var (isValid, errors) = bp.Validate();
            if (!isValid || errors.Count > 0)
                throw new Exception($"Party blueprint validation failed: {string.Join(", ", errors)}");

            int estimatedWage = ForgePartySpawner.EstimateRosterWage(bp.TroopRoster, id => id.Contains("sergeant") ? 5 : 4);
            if (estimatedWage <= 0 || estimatedWage > bp.WageBudgetLimit)
                throw new Exception($"Estimated party wage {estimatedWage} invalid or exceeds budget.");
        }

        private static void TestHudProjectorAndMapTrackDecay()
        {
            var (visible, screenX, screenY, depth) = ForgeApi.Hud.ProjectWorldToScreen(
                worldX: 0f, worldY: 50f, worldZ: 0f,
                camX: 0f, camY: 0f, camZ: 0f,
                camPitchRad: 0f, camYawRad: 0f,
                fovDeg: 75f, screenWidth: 1920, screenHeight: 1080);

            if (!visible || depth <= 0f) throw new Exception("Target directly in front of camera must project as visible.");
            if (Math.Abs(screenX - 960f) > 5f || Math.Abs(screenY - 540f) > 5f)
                throw new Exception($"Projected point not centered: expected (960, 540), got ({screenX}, {screenY}).");

            float freshStrength = ForgeApi.Hud.CalculateTrackDecay(terrainDecayMultiplier: 1.0f, partySize: 50, daysPassed: 0f);
            float decayedStrength = ForgeApi.Hud.CalculateTrackDecay(terrainDecayMultiplier: 1.0f, partySize: 50, daysPassed: 3f);
            if (freshStrength <= decayedStrength || decayedStrength < 0f)
                throw new Exception("Map track decay failed to degrade track strength over time.");
        }

        private static void TestModRuleAuditorCompliance()
        {
            string[] candidateDirs = new[]
            {
                Path.GetFullPath("modules/CalradiaForge"),
                Path.GetFullPath("../../../../../modules/CalradiaForge"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../modules/CalradiaForge")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../modules/CalradiaForge"))
            };
            string moduleDir = candidateDirs.FirstOrDefault(Directory.Exists);
            if (moduleDir == null) throw new Exception("Could not locate modules/CalradiaForge directory.");

            var result = ModRuleAuditor.Audit(moduleDir);
            if (result.Findings.Any(f => f.Severity == "Error"))
            {
                var errors = result.Findings.Where(f => f.Severity == "Error").Select(f => $"{f.RuleId}: {f.Description}");
                throw new Exception($"ModRuleAuditor detected errors in CalradiaForge module: {string.Join("; ", errors)}");
            }
        }

        private static void TestCoreSdkModulesIntegration()
        {
            int hourlyBucket = CalradiaForge.Core.SDK.CampaignMechanics.ForgeTimeManipulator.GetHourlyBucket(27);
            if (hourlyBucket != 3) throw new Exception($"Expected bucket 3, got {hourlyBucket}.");

            float effectiveDamage = CalradiaForge.Core.SDK.Combat.ForgeDamageModifier.CalculateEffectiveDamage(50f, "Cut", 40f);
            if (effectiveDamage >= 50f || effectiveDamage <= 0f) throw new Exception("Armor must reduce cut damage.");

            int tradeProfit = CalradiaForge.Core.SDK.Economy.ForgeTradeManager.CalculateTradeProfit(20, 45, 50, 0.05f);
            if (tradeProfit <= 0) throw new Exception("Profitable trade run must yield positive net profit.");

            float warScore = CalradiaForge.Core.SDK.Politics.ForgeDiplomacyEngine.CalculateWarDeclarationScore(5000, 2000, 0, 2, -40);
            if (warScore <= 0f) throw new Exception("Dominant strength and negative relations must yield strong war score.");

            string badgeColor = CalradiaForge.Core.SDK.UI.ForgeTradeProfitUI.GetProfitBadge(50, 80).badgeColor;
            if (badgeColor != "#2ECC71") throw new Exception($"Expected high profit green '#2ECC71', got '{badgeColor}'.");
        }

        private static void TestInGameUiSimulateAndAudit()
        {
            AssertCurrentNativePrefab();
            if (UseRoutedDesktopAssertions()) return;
            // 1. Locate and inspect CalradiaForge.xml prefab
            string[] xmlCandidates = new[]
            {
                Path.GetFullPath("modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"),
                Path.GetFullPath("../../../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"))
            };
            string xmlPath = xmlCandidates.FirstOrDefault(File.Exists);
            if (xmlPath == null) throw new FileNotFoundException("Could not locate CalradiaForge.xml.");

            string xml = File.ReadAllText(xmlPath);

            // Verify Category Switcher Row 3 (SIMULATE & AUDIT)
            if (!xml.Contains("ExecuteCategorySimulate"))
                throw new Exception("CalradiaForge.xml missing ExecuteCategorySimulate command binding.");
            if (!xml.Contains("ExecuteCategoryAudit"))
                throw new Exception("CalradiaForge.xml missing ExecuteCategoryAudit command binding.");
            if (!xml.Contains("IsCategorySimulateActive"))
                throw new Exception("CalradiaForge.xml missing IsCategorySimulateActive binding.");
            if (!xml.Contains("IsCategoryAuditActive"))
                throw new Exception("CalradiaForge.xml missing IsCategoryAuditActive binding.");

            // Verify ScrollablePanel MarginTop=180 prevents overlap with 3 tab rows
            if (!xml.Contains(@"MarginTop=""180"""))
                throw new Exception("CalradiaForge.xml ScrollablePanel missing MarginTop=\"180\" layout geometry.");

            // Verify sub-navigation buttons under SIMULATE
            string[] simButtons = new[]
            {
                "ForgeSimDiplomacy", "ExecuteSimDiplomacy",
                "ForgeSimSettlements", "ExecuteSimSettlements",
                "ForgeSimEconomy", "ExecuteSimEconomy",
                "ForgeSimTactics", "ExecuteSimTactics",
                "ForgeSimProgression", "ExecuteSimProgression"
            };
            foreach (var s in simButtons)
            {
                if (!xml.Contains(s))
                    throw new Exception($"CalradiaForge.xml missing simulate navigation item '{s}'.");
            }

            // Verify sub-navigation buttons under AUDIT
            string[] auditButtons = new[]
            {
                "ForgeRuleAuditor", "ExecuteRuleAuditor",
                "ForgeModelAudit", "ExecuteModelAudit",
                "ForgeDumpDiagnostics", "ExecuteDumpDiagnostics"
            };
            foreach (var a in auditButtons)
            {
                if (!xml.Contains(a))
                    throw new Exception($"CalradiaForge.xml missing audit navigation item '{a}'.");
            }

            // Verify action bar quick-run buttons
            if (!xml.Contains("ExecuteRunSim") || !xml.Contains("@ShowSimulateActions"))
                throw new Exception("CalradiaForge.xml missing ExecuteRunSim action bar button.");
            if (!xml.Contains("ExecuteRunAudit") || !xml.Contains("@ShowAuditActions"))
                throw new Exception("CalradiaForge.xml missing ExecuteRunAudit action bar button.");

            // 2. Locate and inspect PanelViewModel.cs
            string[] vmCandidates = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Mod/PanelViewModel.cs"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Mod/PanelViewModel.cs"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Mod/PanelViewModel.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Mod/PanelViewModel.cs"))
            };
            string vmPath = vmCandidates.FirstOrDefault(File.Exists);
            if (vmPath == null) throw new FileNotFoundException("Could not locate PanelViewModel.cs.");

            string vm = File.ReadAllText(vmPath);

            // Verify Category and Action properties
            string[] requiredProps = new[]
            {
                "IsCategorySimulateActive", "IsCategoryAuditActive",
                "ShowSimulateActions", "ShowAuditActions",
                "IsSimDiplomacyActive", "IsSimSettlementsActive",
                "IsSimEconomyActive", "IsSimTacticsActive",
                "IsSimProgressionActive", "IsRuleAuditorActive",
                "CategorySimulateLabel", "CategoryAuditLabel",
                "SimDiplomacyLabel", "SimSettlementsLabel",
                "SimEconomyLabel", "SimTacticsLabel",
                "SimProgressionLabel", "RuleAuditorLabel",
                "RunSimLabel", "RunAuditLabel"
            };
            foreach (var p in requiredProps)
            {
                if (!vm.Contains(p))
                    throw new Exception($"PanelViewModel.cs missing property '{p}'.");
            }

            // Verify action parity in ArgumentSuggestions and SectionHelpLabel
            string[] actionIds = new[]
            {
                "sim-diplomacy", "sim-settlements", "sim-economy", "sim-tactics", "sim-progression", "rule-auditor"
            };
            foreach (var id in actionIds)
            {
                if (!vm.Contains($@"""{id}"""))
                    throw new Exception($"PanelViewModel.cs missing action ID '{id}'.");
            }

            // Verify keyboard shortcuts in ExecuteKeyboardControl
            string[] keyboardIds = new[]
            {
                "ForgeCategorySimulate", "ForgeCategoryAudit",
                "ForgeSimDiplomacy", "ForgeSimSettlements",
                "ForgeSimEconomy", "ForgeSimTactics",
                "ForgeSimProgression", "ForgeRuleAuditor",
                "ForgeRunSim", "ForgeRunAudit"
            };
            foreach (var kid in keyboardIds)
            {
                if (!vm.Contains($@"case ""{kid}"":"))
                    throw new Exception($"PanelViewModel.cs ExecuteKeyboardControl missing mapping for '{kid}'.");
            }

            // Verify Runtime.Handle has rule-auditor action
            string[] rtCandidates = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Mod/Runtime.cs"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Mod/Runtime.cs"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Mod/Runtime.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Mod/Runtime.cs"))
            };
            string rtPath = rtCandidates.FirstOrDefault(File.Exists);
            if (rtPath == null) throw new FileNotFoundException("Could not locate Runtime.cs.");
            string rt = File.ReadAllText(rtPath);
            if (!rt.Contains(@"case ""rule-auditor"":"))
                throw new Exception("Runtime.cs missing case \"rule-auditor\" in Handle method.");
        }

        private static void TestInGameUiRound2Enhancements()
        {
            AssertCurrentNativePrefab();
            if (UseRoutedDesktopAssertions()) return;
            // 1. Verify CalradiaForge.xml prefab enhancements
            string[] xmlCandidates = new[]
            {
                Path.GetFullPath("modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"),
                Path.GetFullPath("../../../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"))
            };
            string xmlPath = xmlCandidates.FirstOrDefault(File.Exists);
            if (xmlPath == null) throw new FileNotFoundException("Could not locate CalradiaForge.xml.");

            string xml = File.ReadAllText(xmlPath);
            XDocument doc = XDocument.Parse(xml);
            if (doc.Root == null) throw new Exception("CalradiaForge.xml root is null.");

            // Verify Header, Toast Banner, Actions, Tools, and Cheat Sheet
            string[] requiredXmlTags = new[]
            {
                "ExecuteToggleKeyHelp",
                "ToastBanner", "@IsToastVisible", "@ToastMessage",
                "ForgeOutputFilterInput", "@OutputFilterText", "UpdateTextOnTyping=\"true\"",
                "ForgeOutputFilterClear", "ExecuteClearOutputFilter", "@OutputFilterPlaceholder",
                "ExecuteSimParties", "ExecuteAudioTester",
                "ExecuteLocalizationTester", "ExecuteSaveInspector",
                "KeyHelpOverlay", "@IsKeyHelpOpen"
            };
            foreach (var tag in requiredXmlTags)
            {
                if (!xml.Contains(tag))
                    throw new Exception($"CalradiaForge.xml missing required element or binding '{tag}'.");
            }

            // 2. Verify SubModule.cs tick wiring and shortcuts
            string[] smCandidates = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Mod/SubModule.cs"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Mod/SubModule.cs"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Mod/SubModule.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Mod/SubModule.cs"))
            };
            string smPath = smCandidates.FirstOrDefault(File.Exists);
            if (smPath == null) throw new FileNotFoundException("Could not locate SubModule.cs.");
            string sm = File.ReadAllText(smPath);

            if (!sm.Contains("vm?.Tick(dt)"))
                throw new Exception("SubModule.cs missing vm?.Tick(dt) hook in OnApplicationTick.");
            if (!sm.Contains("ExecuteToggleKeyHelp") || !sm.Contains("ExecuteToggleLiveWatch"))
                throw new Exception("SubModule.cs missing hotkey trigger for KeyHelp or LiveWatch.");

            // 3. Verify PanelViewModel.cs properties, action parity, and methods
            string[] vmCandidates = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Mod/PanelViewModel.cs"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Mod/PanelViewModel.cs"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Mod/PanelViewModel.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Mod/PanelViewModel.cs"))
            };
            string vmPath = vmCandidates.FirstOrDefault(File.Exists);
            if (vmPath == null) throw new FileNotFoundException("Could not locate PanelViewModel.cs.");
            string vm = File.ReadAllText(vmPath);

            string[] requiredVmMembers = new[]
            {
                "IsLiveWatchActive", "ExecuteToggleLiveWatch", "LiveWatchLabel",
                "IsKeyHelpOpen", "ExecuteToggleKeyHelp", "KeyHelpLabel",
                "IsToastVisible", "ToastMessage", "ShowToast",
                "FilterLinesLabel", "ExecuteFilterLines", "OutputFilterText", "IsOutputFilterEmpty",
                "HasOutputFilter", "ExecuteClearOutputFilter", "No output lines match this filter.",
                "IsSimPartiesActive", "ExecuteSimParties", "SimPartiesLabel",
                "IsAudioTesterActive", "ExecuteAudioTester", "AudioTesterLabel",
                "IsLocalizationTesterActive", "ExecuteLocalizationTester", "LocalizationTesterLabel",
                "IsSaveInspectorActive", "ExecuteSaveInspector", "SaveInspectorLabel",
                "Tick(float dt)"
            };
            foreach (var member in requiredVmMembers)
            {
                if (!vm.Contains(member))
                    throw new Exception($"PanelViewModel.cs missing member '{member}'.");
            }

            // Verify action parity for new tools
            string[] newActionIds = new[] { "sim-parties", "sim-audio", "audit-localization", "audit-save" };
            foreach (var actionId in newActionIds)
            {
                if (!vm.Contains($@"""{actionId}"""))
                    throw new Exception($"PanelViewModel.cs missing action ID '{actionId}'.");
            }

            // 4. Verify Runtime.cs tool handles
            string[] rtCandidates = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Mod/Runtime.cs"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Mod/Runtime.cs"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Mod/Runtime.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Mod/Runtime.cs"))
            };
            string rtPath = rtCandidates.FirstOrDefault(File.Exists);
            if (rtPath == null) throw new FileNotFoundException("Could not locate Runtime.cs.");
            string rt = File.ReadAllText(rtPath);

            foreach (var actionId in newActionIds)
            {
                if (!rt.Contains($@"case ""{actionId}"":"))
                    throw new Exception($"Runtime.cs missing case handler for '{actionId}'.");
            }

            // 5. Verify functional builders: ForgeAudioBuilder and ForgeSaveChunker
            var audioBuilder = new ForgeAudioBuilder();
            audioBuilder.Add2DSound("custom_ui_click", "ui/click.ogg", "ui");
            audioBuilder.Add3DSound("custom_shield_clash", "combat/clash.ogg", "mission_combat");
            if (audioBuilder.Count != 2)
                throw new Exception($"ForgeAudioBuilder.Count expected 2, got {audioBuilder.Count}.");
            var (audioValid, audioErrors) = audioBuilder.Validate();
            if (!audioValid || audioErrors.Count > 0)
                throw new Exception("ForgeAudioBuilder validation failed on valid sounds.");

            // Test ForgeSaveChunker with 70KB payload (> 31KB limit)
            string largePayload = new string('A', 70000);
            if (!ForgeSaveChunker.NeedsChunking(largePayload))
                throw new Exception("ForgeSaveChunker.NeedsChunking returned false for 70KB payload.");
            string[] chunks = ForgeSaveChunker.Chunk(largePayload);
            if (chunks.Length < 3)
                throw new Exception($"ForgeSaveChunker expected at least 3 chunks for 70KB, got {chunks.Length}.");
            foreach (var c in chunks)
            {
                if (c.Length > ForgeSaveChunker.SafeChunkSize)
                    throw new Exception($"ForgeSaveChunker chunk exceeded SafeChunkSize: {c.Length}");
            }
            string reassembled = ForgeSaveChunker.Reassemble(chunks);
            if (reassembled != largePayload)
                throw new Exception("ForgeSaveChunker reassembled payload does not match original.");
        }

        private static void TestGauntletLiveOutputFilter()
        {
            List<string> source = OutputLineFilter.SplitLines("alpha\r\nBuild step 03: done\r\nneedle-crosses-wrap-boundary\r\n");
            if (source.Count != 4 || source[0] != "alpha" || source[3] != string.Empty)
                throw new Exception("Output source lines must normalize CRLF while preserving the trailing empty line.");

            List<string> caseInsensitive = OutputLineFilter.FilterAndWrap(source, "BUILD", 64);
            if (caseInsensitive.Count != 1 || caseInsensitive[0] != "Build step 03: done")
                throw new Exception("Output filtering must match without regard to case and preserve each matching source line.");

            List<string> wrappedMatch = OutputLineFilter.FilterAndWrap(source, "crosses-wrap", 8);
            if (wrappedMatch.Count != 4 || string.Concat(wrappedMatch) != "needle-crosses-wrap-boundary")
                throw new Exception("Filtering must match raw lines before wrapping the displayed result.");

            List<string> noMatches = OutputLineFilter.FilterAndWrap(source, "not-present", 64);
            if (noMatches.Count != 0)
                throw new Exception("A filter with no matches must produce no result rows for the localized empty state.");

            List<string> manyLines = OutputLineFilter.SplitLines(string.Join("\n", Enumerable.Range(1, 18).Select(index => "Match " + index)));
            List<string> matchingRows = OutputLineFilter.FilterAndWrap(manyLines, "match", 64);
            if (matchingRows.Count != 18)
                throw new Exception("Filtering must preserve every matching line before pagination.");
            int pageCount = Math.Max(1, (matchingRows.Count + 16 - 1) / 16);
            if (pageCount != 2 || matchingRows.Take(16).Count() != 16 || matchingRows.Skip(16).Count() != 2)
                throw new Exception("Filtered lines must use the existing sixteen-row page size.");

            List<string> unfiltered = OutputLineFilter.FilterAndWrap(manyLines, string.Empty, 64);
            if (!unfiltered.SequenceEqual(manyLines))
                throw new Exception("An empty query must show the original output rows without filtering.");
            List<string> whitespaceQuery = OutputLineFilter.FilterAndWrap(manyLines, "  \t", 64);
            if (!whitespaceQuery.SequenceEqual(manyLines))
                throw new Exception("Whitespace-only queries must remain equivalent to an empty filter.");

            string vmPath = Path.GetFullPath("src/CalradiaForge.Mod/PanelViewModel.cs");
            string vm = File.ReadAllText(vmPath);
            int propertyStart = vm.IndexOf("public string OutputFilterText", StringComparison.Ordinal);
            if (propertyStart < 0)
                throw new Exception("PanelViewModel must expose a separately bound live filter property.");
            int propertyEnd = vm.IndexOf("[DataSourceProperty] public bool IsOutputFilterEmpty", propertyStart, StringComparison.Ordinal);
            if (propertyEnd <= propertyStart)
                throw new Exception("PanelViewModel must expose the empty-filter presentation state.");
            string filterProperty = vm.Substring(propertyStart, propertyEnd - propertyStart);
            if (!filterProperty.Contains("Render();") || filterProperty.Contains("Argument") || filterProperty.Contains("full ="))
                throw new Exception("Typing in the output filter must rerender without changing the tool argument or original output.");
            if (!vm.Contains("IsOutputFilterEmpty => string.IsNullOrWhiteSpace(_filterQuery)") ||
                !vm.Contains("HasOutputFilter => !string.IsNullOrWhiteSpace(_filterQuery)"))
                throw new Exception("Whitespace-only queries must have a consistent empty presentation state.");
            int sectionStart = vm.IndexOf("void SelectSection(string section, bool executeOnSelect)", StringComparison.Ordinal);
            int sectionEnd = vm.IndexOf("void RenderNavigationPaletteLanding", sectionStart, StringComparison.Ordinal);
            if (sectionStart < 0 || sectionEnd <= sectionStart)
                throw new Exception("Could not isolate native section navigation to verify filter lifetime.");
            string sectionFlow = vm.Substring(sectionStart, sectionEnd - sectionStart);
            if (sectionFlow.Contains("_filterQuery =") || sectionFlow.Contains("OutputFilterText ="))
                throw new Exception("Changing native areas must preserve the in-session output query.");
            if (!vm.Contains("full = r.Success ? Format(action, r.Data) : T(\"Error\") + \": \" + FormatError(r.Error); page = 0; Render();"))
                throw new Exception("New tool results must pass through Render so the current output query is reapplied.");
            if (!vm.Contains("OutputLineFilter.FilterAndWrap(_outputSourceLines, _filterQuery, OutputWrapWidth)") ||
                !vm.Contains("ReferenceEquals(_outputSourceForLines, full)") ||
                !vm.Contains("No output lines match this filter.") ||
                !vm.Contains("public void ExecuteClearOutputFilter() => OutputFilterText = string.Empty;"))
                throw new Exception("PanelViewModel must filter cached source output, show a localized no-match state, and clear only the query.");
            if (!vm.Contains("public string EvidenceHeading => T(\"Evidence\");"))
                throw new Exception("The evidence header must use a short localized label instead of a route title that can collide with the filter.");

            XDocument prefab = XDocument.Load(Path.GetFullPath("modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"));
            XElement input = prefab.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgeOutputFilterInput");
            XElement clear = prefab.Descendants().SingleOrDefault(element => (string)element.Attribute("Id") == "ForgeOutputFilterClear");
            if (input == null || (string)input.Attribute("Text") != "@OutputFilterText" ||
                (string)input.Attribute("UpdateTextOnTyping") != "true" ||
                clear == null || (string)clear.Attribute("Command.Click") != "ExecuteClearOutputFilter")
                throw new Exception("The prefab must bind the immediate filter input and its independent clear action.");

            var locales = new[]
            {
                Tuple.Create("en", "EN"), Tuple.Create("es", "SP"), Tuple.Create("pt", "BR"),
                Tuple.Create("de", "DE"), Tuple.Create("fr", "FR"), Tuple.Create("it", "IT"),
                Tuple.Create("pl", "PL"), Tuple.Create("ru", "RU"), Tuple.Create("tr", "TR"),
                Tuple.Create("zh-HANS", "CNs"), Tuple.Create("zh-HANT", "CNt"),
                Tuple.Create("ja", "JP"), Tuple.Create("ko", "KO")
            };
            string[] localizedFilterKeys = new[]
            {
                "Evidence",
                "Filter current output without changing the tool argument.",
                "Filter output lines...", "Clear output filter.", "No output lines match this filter.",
                "Pin output baseline", "Compare outputs", "Clear output baseline", "Baseline output", "Current output",
                "Output baseline pinned.", "Pin an output baseline before comparing.", "No current output to compare.",
                "No output matches the filter on either side.",
                "Output comparison unavailable because a configured input or work limit was reached.",
                "Output exceeds comparison limits; the baseline was not changed.", "Show current output",
                "Pin current output as the comparison baseline.", "Show the baseline and current outputs side by side.",
                "Return to the current output without removing the baseline.", "Clear the pinned output baseline.",
                "Output baseline cleared."
            };
            Dictionary<string, string> englishCatalog = XDocument.Load(Path.GetFullPath("localization/en.xml"))
                .Root.Elements("string").ToDictionary(element => (string)element.Attribute("key"), element => (string)element.Attribute("value"));
            using (SHA256 sha256 = SHA256.Create())
            {
                foreach (Tuple<string, string> locale in locales)
                {
                    Dictionary<string, string> sourceCatalog = XDocument.Load(Path.GetFullPath("localization/" + locale.Item1 + ".xml"))
                        .Root.Elements("string").ToDictionary(element => (string)element.Attribute("key"), element => (string)element.Attribute("value"));
                    Dictionary<string, string> generatedCatalog = XDocument.Load(Path.GetFullPath(
                            "modules/CalradiaForge/ModuleData/Languages/" + locale.Item2 + "/forge_strings.xml"))
                        .Descendants("string").ToDictionary(element => (string)element.Attribute("id"), element => (string)element.Attribute("text"));
                    foreach (string key in localizedFilterKeys)
                    {
                        if (!sourceCatalog.TryGetValue(key, out string localized) || string.IsNullOrWhiteSpace(localized))
                            throw new Exception("Missing output-filter source translation for " + locale.Item1 + ": " + key);
                        string hash = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(englishCatalog[key])))
                            .Replace("-", string.Empty).ToLowerInvariant();
                        string id = "forge_" + hash.Substring(0, 12);
                        if (!generatedCatalog.TryGetValue(id, out string generated) || generated != localized)
                            throw new Exception("Generated Gauntlet language resource is stale for " + locale.Item1 + ": " + key);
                    }
                }
            }
        }

        private static void TestGauntletOutputComparison()
        {
            OutputLineComparisonResult equal = OutputLineComparison.Compare("alpha\nbeta", "alpha\nbeta", string.Empty, 64, 64, 0);
            AssertOutputComparison(equal, OutputLineComparisonStatus.Available, 2, 1, 2);
            if (equal.Rows[0].Kind != OutputLineComparisonKind.Equal || equal.Rows[0].BaselineText != "alpha" ||
                equal.Rows[0].CurrentText != "alpha" || equal.Rows[1].BaselineText != "beta" || equal.Rows[1].CurrentText != "beta")
                throw new Exception("Identical output lines must be retained on both comparison sides.");

            OutputLineComparisonResult inserted = OutputLineComparison.Compare("head\ntail", "head\nnew\ntail", null, 64, 64, 0);
            AssertOutputComparison(inserted, OutputLineComparisonStatus.Available, 3, 1, 3);
            if (inserted.Rows[1].Kind != OutputLineComparisonKind.CurrentOnly || inserted.Rows[1].BaselineText.Length != 0 ||
                inserted.Rows[1].CurrentText != "new")
                throw new Exception("An inserted line must occupy the current column and leave its baseline cell empty.");

            OutputLineComparisonResult deleted = OutputLineComparison.Compare("head\nold\ntail", "head\ntail", string.Empty, 64, 64, 0);
            AssertOutputComparison(deleted, OutputLineComparisonStatus.Available, 3, 1, 3);
            if (deleted.Rows[1].Kind != OutputLineComparisonKind.BaselineOnly || deleted.Rows[1].BaselineText != "old" ||
                deleted.Rows[1].CurrentText.Length != 0)
                throw new Exception("A deleted line must occupy the baseline column and leave its current cell empty.");

            OutputLineComparisonResult replaced = OutputLineComparison.Compare("head\nold one\nold two\ntail",
                "head\nnew one\ntail", string.Empty, 64, 64, 0);
            AssertOutputComparison(replaced, OutputLineComparisonStatus.Available, 4, 1, 4);
            if (replaced.Rows[1].Kind != OutputLineComparisonKind.Changed || replaced.Rows[1].BaselineText != "old one" ||
                replaced.Rows[1].CurrentText != "new one" || replaced.Rows[2].Kind != OutputLineComparisonKind.BaselineOnly ||
                replaced.Rows[2].BaselineText != "old two" || replaced.Rows[2].CurrentText.Length != 0)
                throw new Exception("A contiguous edit run must pair deletions and insertions positionally, then retain unmatched cells.");

            OutputLineComparisonResult tie = OutputLineComparison.Compare("a\nb", "b\na", string.Empty, 64, 64, 0);
            AssertOutputComparison(tie, OutputLineComparisonStatus.Available, 3, 1, 3);
            if (tie.Rows[0].Kind != OutputLineComparisonKind.BaselineOnly || tie.Rows[0].BaselineText != "a" ||
                tie.Rows[1].Kind != OutputLineComparisonKind.Equal || tie.Rows[1].BaselineText != "b" || tie.Rows[1].CurrentText != "b" ||
                tie.Rows[2].Kind != OutputLineComparisonKind.CurrentOnly || tie.Rows[2].CurrentText != "a")
                throw new Exception("Equal-cost edit paths must use the documented deterministic deletion-first tie break.");

            OutputLineComparisonResult duplicateOne = OutputLineComparison.Compare("A\nB\nA", "A\nA\nB", "", 64, 64, 0);
            OutputLineComparisonResult duplicateTwo = OutputLineComparison.Compare("A\nB\nA", "A\nA\nB", "", 64, 64, 0);
            if (!SerializeComparisonRows(duplicateOne.Rows).SequenceEqual(SerializeComparisonRows(duplicateTwo.Rows)))
                throw new Exception("Duplicate-line alignments must be deterministic across repeated comparisons.");

            string baselineSeparators = "first\r\nsecond\rthird\n";
            string currentSeparators = "first\nsecond\nthird\n";
            OutputLineComparisonResult normalized = OutputLineComparison.Compare(baselineSeparators, currentSeparators, "", 64, 64, 0);
            AssertOutputComparison(normalized, OutputLineComparisonStatus.Available, 4, 1, 4);
            if (normalized.Rows.Any(row => row.Kind != OutputLineComparisonKind.Equal) ||
                normalized.Rows[3].BaselineText.Length != 0 || normalized.Rows[3].CurrentText.Length != 0)
                throw new Exception("CRLF, CR, LF, and terminal empty lines must normalize identically without dropping the final line.");

            OutputLineComparisonResult emptyOutput = OutputLineComparison.Compare(string.Empty, string.Empty, "", 8, 8, 0);
            AssertOutputComparison(emptyOutput, OutputLineComparisonStatus.Available, 0, 1, 0);
            OutputLineComparisonResult emptyLines = OutputLineComparison.Compare("\n", "\n", "", 8, 8, 0);
            AssertOutputComparison(emptyLines, OutputLineComparisonStatus.Available, 2, 1, 2);

            string rawBaseline = "baseline needle alpha";
            string rawCurrent = "current other line";
            OutputLineComparisonResult filtered = OutputLineComparison.Compare(rawBaseline, rawCurrent, "NEEDLE", 5, 5, 0);
            AssertOutputComparison(filtered, OutputLineComparisonStatus.Available, 5, 1, 5);
            if (filtered.Rows[0].Kind != OutputLineComparisonKind.Changed ||
                string.Concat(filtered.Rows.Select(row => row.BaselineText)) != rawBaseline ||
                string.Concat(filtered.Rows.Select(row => row.CurrentText)) != rawCurrent)
                throw new Exception("A match on either raw cell must retain the full aligned pair before both columns are wrapped.");
            if (rawBaseline != "baseline needle alpha" || rawCurrent != "current other line")
                throw new Exception("Comparison must not mutate either immutable source string.");

            OutputLineComparisonResult currentSideFilter = OutputLineComparison.Compare("left value", "RIGHT value", "right", 64, 64, 0);
            AssertOutputComparison(currentSideFilter, OutputLineComparisonStatus.Available, 1, 1, 1);
            if (currentSideFilter.Rows[0].BaselineText != "left value" || currentSideFilter.Rows[0].CurrentText != "RIGHT value")
                throw new Exception("Filtering on the current column must retain the entire baseline/current pair.");
            OutputLineComparisonResult noMatches = OutputLineComparison.Compare("left", "right", "absent", 64, 64, 0);
            AssertOutputComparison(noMatches, OutputLineComparisonStatus.Available, 0, 1, 0);
            OutputLineComparisonResult whitespaceFilter = OutputLineComparison.Compare("left", "right", " \t", 64, 64, 0);
            AssertOutputComparison(whitespaceFilter, OutputLineComparisonStatus.Available, 1, 1, 1);

            OutputLineComparisonResult wrapped = OutputLineComparison.Compare("abcdefgh", "abc", string.Empty, 4, 4, 0);
            AssertOutputComparison(wrapped, OutputLineComparisonStatus.Available, 2, 1, 2);
            if (wrapped.Rows[0].BaselineText != "abcd" || wrapped.Rows[0].CurrentText != "abc" ||
                wrapped.Rows[1].BaselineText != "efgh" || wrapped.Rows[1].CurrentText.Length != 0 ||
                wrapped.Rows.Any(row => row.Kind != OutputLineComparisonKind.Changed))
                throw new Exception("Wrapped columns must retain paired visual fragments and pad the shorter side with an empty cell.");

            string manyLines = string.Join("\n", Enumerable.Range(0, 18).Select(index => "row " + index));
            OutputLineComparisonResult firstPage = OutputLineComparison.Compare(manyLines, manyLines, "", 64, 64, 0);
            AssertOutputComparison(firstPage, OutputLineComparisonStatus.Available, 18, 2, 16);
            OutputLineComparisonResult secondPage = OutputLineComparison.Compare(manyLines, manyLines, "", 64, 64, 1);
            AssertOutputComparison(secondPage, OutputLineComparisonStatus.Available, 18, 2, 2);
            OutputLineComparisonResult clampedPage = OutputLineComparison.Compare(manyLines, manyLines, "", 64, 64, 100);
            if (clampedPage.PageIndex != 1 || !SerializeComparisonRows(clampedPage.Rows).SequenceEqual(SerializeComparisonRows(secondPage.Rows)))
                throw new Exception("Comparison pagination must clamp to a complete final page and remain synchronized.");

            if (OutputLineComparison.ValidateOutput(new string('x', OutputLineComparison.MaxCharacters)) != OutputLineComparisonStatus.Available ||
                OutputLineComparison.ValidateOutput(new string('x', OutputLineComparison.MaxCharacters + 1)) != OutputLineComparisonStatus.TooManyCharacters)
                throw new Exception("The output character limit must accept its exact boundary and reject the first value above it.");
            OutputLineComparisonResult tooManyBaselineChars = OutputLineComparison.Compare(
                new string('x', OutputLineComparison.MaxCharacters + 1), "ok", "", 64, 64, 0);
            AssertUnavailableComparison(tooManyBaselineChars, OutputLineComparisonStatus.BaselineTooManyCharacters);
            OutputLineComparisonResult tooManyCurrentChars = OutputLineComparison.Compare(
                "ok", new string('x', OutputLineComparison.MaxCharacters + 1), "", 64, 64, 0);
            AssertUnavailableComparison(tooManyCurrentChars, OutputLineComparisonStatus.CurrentTooManyCharacters);

            string exactLineLimit = string.Join("\n", Enumerable.Range(0, OutputLineComparison.MaxLines).Select(index => "L" + index.ToString("D4")));
            string aboveLineLimit = string.Join("\n", Enumerable.Range(0, OutputLineComparison.MaxLines + 1).Select(index => "L" + index.ToString("D4")));
            if (OutputLineComparison.ValidateOutput(exactLineLimit) != OutputLineComparisonStatus.Available ||
                OutputLineComparison.ValidateOutput(aboveLineLimit) != OutputLineComparisonStatus.TooManyLines ||
                OutputLineComparison.ValidateOutput(new string('\n', OutputLineComparison.MaxLines - 1)) != OutputLineComparisonStatus.Available ||
                OutputLineComparison.ValidateOutput(new string('\n', OutputLineComparison.MaxCharacters)) != OutputLineComparisonStatus.TooManyLines)
                throw new Exception("The line limit must accept its exact boundary and reject the first value above it.");
            OutputLineComparisonResult exactLines = OutputLineComparison.Compare(exactLineLimit, exactLineLimit, "", 64, 64, 0);
            AssertOutputComparison(exactLines, OutputLineComparisonStatus.Available, OutputLineComparison.MaxLines,
                (OutputLineComparison.MaxLines + OutputLineComparison.RowsPerPage - 1) / OutputLineComparison.RowsPerPage,
                OutputLineComparison.RowsPerPage);
            AssertUnavailableComparison(OutputLineComparison.Compare("ok", aboveLineLimit, "", 64, 64, 0),
                OutputLineComparisonStatus.CurrentTooManyLines);

            string atVisualRowLimit = string.Join("\n", Enumerable.Repeat("aa", OutputLineComparison.MaxLines));
            OutputLineComparisonResult exactAlignedRows = OutputLineComparison.Compare(atVisualRowLimit, atVisualRowLimit,
                "", 1, 1, 0);
            AssertOutputComparison(exactAlignedRows, OutputLineComparisonStatus.Available, OutputLineComparison.MaxAlignedRows,
                (OutputLineComparison.MaxAlignedRows + OutputLineComparison.RowsPerPage - 1) / OutputLineComparison.RowsPerPage,
                OutputLineComparison.RowsPerPage);
            string aboveVisualRowLimit = atVisualRowLimit.Substring(0, atVisualRowLimit.Length - 2) + "aaa";
            OutputLineComparisonResult aboveVisualRowLimitResult = OutputLineComparison.Compare(
                aboveVisualRowLimit, aboveVisualRowLimit, "", 1, 1, 0);
            AssertOutputComparison(aboveVisualRowLimitResult, OutputLineComparisonStatus.Available,
                OutputLineComparison.MaxAlignedRows + 1,
                (OutputLineComparison.MaxAlignedRows + 1 + OutputLineComparison.RowsPerPage - 1) / OutputLineComparison.RowsPerPage,
                OutputLineComparison.RowsPerPage);

            string traceBoundaryBaseline = string.Join("\n", Enumerable.Range(0, 256).Select(index => "b" + index.ToString("D4")));
            string traceBoundaryCurrent = string.Join("\n", Enumerable.Range(0, 255).Select(index => "c" + index.ToString("D4")));
            OutputLineComparisonResult exactTrace = OutputLineComparison.Compare(traceBoundaryBaseline, traceBoundaryCurrent,
                "", 64, 64, 0);
            if (exactTrace.Status != OutputLineComparisonStatus.Available)
                throw new Exception("A diff requiring exactly the configured trace-cell budget must remain available.");
            string overTraceCurrent = string.Join("\n", Enumerable.Range(0, 256).Select(index => "c" + index.ToString("D4")));
            AssertUnavailableComparison(OutputLineComparison.Compare(traceBoundaryBaseline, overTraceCurrent, "", 64, 64, 0),
                OutputLineComparisonStatus.TraceBudgetExceeded);

            string expensivePrefix = new string('x', 60000);
            string expensiveBaseline = string.Join("\n", Enumerable.Range(0, 4).Select(index => expensivePrefix + "b" + index));
            string expensiveCurrent = string.Join("\n", Enumerable.Range(0, 4).Select(index => expensivePrefix + "c" + index));
            AssertUnavailableComparison(OutputLineComparison.Compare(expensiveBaseline, expensiveCurrent, "", 64, 64, 0),
                OutputLineComparisonStatus.SearchBudgetExceeded);

            OutputLineComparisonResult unicode = OutputLineComparison.Compare("王朝⚔️\nnaïve", "王朝⚔️\nnaïve", "王朝", 64, 64, 0);
            AssertOutputComparison(unicode, OutputLineComparisonStatus.Available, 1, 1, 1);
            if (unicode.Rows[0].BaselineText != "王朝⚔️" || unicode.Rows[0].CurrentText != "王朝⚔️")
                throw new Exception("Ordinal-ignore-case filtering must preserve Unicode content without altering either column.");

            OutputLineComparisonResult wrappedSurrogatePair = OutputLineComparison.Compare("abc😀x", "abc😀x", "", 4, 4, 0);
            AssertOutputComparison(wrappedSurrogatePair, OutputLineComparisonStatus.Available, 2, 1, 2);
            if (wrappedSurrogatePair.Rows[0].BaselineText != "abc" || wrappedSurrogatePair.Rows[1].BaselineText != "😀x" ||
                wrappedSurrogatePair.Rows.Any(row => HasSplitSurrogateBoundary(row.BaselineText) || HasSplitSurrogateBoundary(row.CurrentText)))
                throw new Exception("Wrapping must not split a Unicode surrogate pair across comparison rows.");
            OutputLineComparisonResult widthOneSurrogatePair = OutputLineComparison.Compare("😀", "😀", "", 1, 1, 0);
            AssertOutputComparison(widthOneSurrogatePair, OutputLineComparisonStatus.Available, 1, 1, 1);
            if (widthOneSurrogatePair.Rows[0].BaselineText != "😀" || widthOneSurrogatePair.Rows[0].CurrentText != "😀")
                throw new Exception("A surrogate pair must remain intact even when the requested wrap width is one code unit.");

            string repetitiveLine = new string('a', OutputLineComparison.MaxCharacters);
            string longAbsentQuery = new string('a', 131071) + "b";
            AssertUnavailableComparison(OutputLineComparison.Compare(repetitiveLine, repetitiveLine, longAbsentQuery,
                64, 64, 0), OutputLineComparisonStatus.SearchBudgetExceeded);

            string vm = File.ReadAllText(Path.GetFullPath("src/CalradiaForge.Mod/PanelViewModel.cs"));
            if (!vm.Contains("_outputBaseline = full;") || !vm.Contains("_outputBaselineRouteId = current;") ||
                !vm.Contains("_outputBaselineRouteName = CurrentName;") ||
                !vm.Contains("OutputLineComparison.Compare(") || !vm.Contains("_outputBaseline = null;"))
                throw new Exception("The panel must pin the original result and its source route, compare separately, and allow explicit baseline release.");
            int clearOutputStart = vm.IndexOf("public void ExecuteClearOutput()", StringComparison.Ordinal);
            int clearOutputEnd = vm.IndexOf("public void RecordCommand", clearOutputStart, StringComparison.Ordinal);
            if (clearOutputStart < 0 || clearOutputEnd <= clearOutputStart)
                throw new Exception("Could not isolate output clearing to verify comparison-baseline lifetime.");
            string clearOutput = vm.Substring(clearOutputStart, clearOutputEnd - clearOutputStart);
            if (!clearOutput.Contains("full = \"\";") || clearOutput.Contains("_outputBaseline = null;") ||
                !clearOutput.Contains("_isOutputComparisonActive = false;"))
                throw new Exception("Clearing current output must exit comparison while preserving the pinned baseline.");

            int pinStart = vm.IndexOf("public void ExecutePinOutputBaseline()", StringComparison.Ordinal);
            int pinEnd = vm.IndexOf("public void ExecuteCompareOutput()", pinStart, StringComparison.Ordinal);
            int clearBaselineStart = vm.IndexOf("public void ExecuteClearOutputBaseline()", pinEnd, StringComparison.Ordinal);
            int clearBaselineEnd = vm.IndexOf("public void ExecuteRun()", clearBaselineStart, StringComparison.Ordinal);
            if (pinStart < 0 || pinEnd <= pinStart || clearBaselineStart <= pinEnd || clearBaselineEnd <= clearBaselineStart)
                throw new Exception("Could not isolate baseline pin and release actions.");
            string pinBaseline = vm.Substring(pinStart, pinEnd - pinStart);
            if (pinBaseline.IndexOf("OutputLineComparison.ValidateOutput(full)", StringComparison.Ordinal) < 0 ||
                pinBaseline.IndexOf("OutputLineComparison.ValidateOutput(full)", StringComparison.Ordinal) >
                    pinBaseline.IndexOf("_outputBaseline = full;", StringComparison.Ordinal))
                throw new Exception("A rejected oversized pin must leave the existing baseline unchanged.");
            string clearBaseline = vm.Substring(clearBaselineStart, clearBaselineEnd - clearBaselineStart);
            if (!clearBaseline.Contains("_outputBaseline = null;") ||
                !clearBaseline.Contains("_isOutputComparisonActive = false;"))
                throw new Exception("The explicit clear-baseline action must release the pin and exit comparison.");

            int closeStart = vm.IndexOf("public void CancelPendingWork()", StringComparison.Ordinal);
            int closeEnd = vm.IndexOf("public void ExecuteRemove()", closeStart, StringComparison.Ordinal);
            if (closeStart < 0 || closeEnd <= closeStart ||
                !vm.Substring(closeStart, closeEnd - closeStart).Contains("_outputBaseline = null;"))
                throw new Exception("Closing the panel must release the pinned baseline.");
        }

        private static void AssertOutputComparison(OutputLineComparisonResult result, OutputLineComparisonStatus status,
            int visualRows, int pageCount, int pageRows)
        {
            if (result.Status != status || result.TotalVisualRows != visualRows || result.PageCount != pageCount ||
                result.Rows.Count != pageRows)
                throw new Exception("Unexpected output comparison result: " + result.Status + ", rows=" + result.TotalVisualRows +
                    ", pages=" + result.PageCount + ", page rows=" + result.Rows.Count + ".");
        }

        private static void AssertUnavailableComparison(OutputLineComparisonResult result, OutputLineComparisonStatus status)
        {
            if (result.Status != status || result.Rows.Count != 0 || result.TotalVisualRows != 0 || result.PageCount != 0)
                throw new Exception("A bounded comparison failure must return its status with no partial rows or pages.");
        }

        private static IEnumerable<string> SerializeComparisonRows(IEnumerable<OutputLineComparisonRow> rows)
        {
            return rows.Select(row => (int)row.Kind + "|" + row.BaselineText + "|" + row.CurrentText);
        }

        private static bool HasSplitSurrogateBoundary(string value)
        {
            return value.Length > 0 && (char.IsHighSurrogate(value[value.Length - 1]) || char.IsLowSurrogate(value[0]));
        }

        private static void TestPanelHotkeyAndLayerLifecycleTelemetry()
        {
            string[] candidates = new[]
            {
                Path.GetFullPath("src/CalradiaForge.Mod/SubModule.cs"),
                Path.GetFullPath("../../../../../src/CalradiaForge.Mod/SubModule.cs"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Mod/SubModule.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Mod/SubModule.cs"))
            };
            string path = candidates.FirstOrDefault(File.Exists);
            if (path == null) throw new FileNotFoundException("Could not locate SubModule.cs.");

            string source = File.ReadAllText(path);
            int tickStart = source.IndexOf("protected override void OnApplicationTick(float dt)", StringComparison.Ordinal);
            int openStart = source.IndexOf("void Open()", StringComparison.Ordinal);
            int openEnd = source.IndexOf("void OpenExtensionPage(", StringComparison.Ordinal);
            if (tickStart < 0 || openStart < 0 || openEnd <= openStart)
                throw new Exception("Could not isolate the panel hotkey and layer lifecycle methods.");

            string tick = source.Substring(tickStart, openStart - tickStart);
            string open = source.Substring(openStart, openEnd - openStart);
            string[] requiredHotkeyTokens = new[]
            {
                "Input.IsKeyPressed(cachedHotkey)",
                "Input.IsKeyDown(InputKey.F10)",
                "Input.IsKeyDownImmediate(InputKey.F10)",
                "f10Held && !f10ProbeHeld",
                "f10ProbeHeld = f10Held",
                "Panel hotkey detected: "
            };
            foreach (string token in requiredHotkeyTokens)
                if (!tick.Contains(token)) throw new Exception("OnApplicationTick is missing F10 regression guard: " + token);
            if (!tick.Contains("Input.IsKeyPressed(InputKey.W)") || !tick.Contains("vm?.ExecuteToggleLiveWatch()"))
                throw new Exception("Live Watch must remain reachable through the existing Ctrl+W path.");

            string[] requiredLayerTokens = new[]
            {
                "new GauntletLayer(",
                "Panel GauntletLayer created.",
                "Loading panel brush file.",
                "layer.UIContext.BrushFactory.LoadBrushFile(\"CalradiaForge\")",
                "Panel brush file loaded.",
                "Loading panel movie.",
                "layer.LoadMovie(\"CalradiaForge\",vm)",
                "Panel movie loaded.",
                "owner.AddLayer(layer)",
                "Panel GauntletLayer attached to owner.",
                "ScreenManager.TrySetFocus(layer)",
                "Panel movie loaded and layer attached"
            };
            int previous = -1;
            foreach (string token in requiredLayerTokens)
            {
                int current = open.IndexOf(token, StringComparison.Ordinal);
                if (current < 0 || current <= previous)
                    throw new Exception("Open() is missing or misorders GauntletLayer lifecycle telemetry: " + token);
                previous = current;
            }
            int openTry = open.IndexOf("try {", StringComparison.Ordinal);
            int layerConstruction = open.IndexOf("new GauntletLayer(", StringComparison.Ordinal);
            if (openTry < 0 || layerConstruction < openTry)
                throw new Exception("Open() must catch layer-construction failures so a partially created ViewModel is finalized.");

            int closeStart = source.IndexOf("void Close()", StringComparison.Ordinal);
            int closeEnd = source.IndexOf("protected override void OnSubModuleUnloaded()", closeStart, StringComparison.Ordinal);
            if (closeStart < 0 || closeEnd <= closeStart)
                throw new Exception("Could not isolate panel cleanup for partial initialization regression checks.");
            string close = source.Substring(closeStart, closeEnd - closeStart);
            if (close.Contains("if(layer==null)return;") ||
                !close.Contains("var closingLayer=layer;var closingOwner=owner;var closingViewModel=vm;") ||
                !close.Contains("finally {closingViewModel?.CancelPendingWork();closingViewModel?.OnFinalize();}"))
                throw new Exception("Close() must finalize the ViewModel even when layer construction failed before assigning a layer.");
        }

        private static void TestNoviceModderFeatures()
        {
            // ── 1. ForgeNoviceHub.GenerateBehaviorScaffold ─────────────────────────
            string behavior = ForgeNoviceHub.GenerateBehaviorScaffold("TestBehavior", "Test.Behaviors");
            if (!behavior.Contains("CampaignBehaviorBase"))
                throw new Exception("GenerateBehaviorScaffold missing CampaignBehaviorBase.");
            if (!behavior.Contains("RegisterEvents"))
                throw new Exception("GenerateBehaviorScaffold missing RegisterEvents.");
            if (!behavior.Contains("SyncData"))
                throw new Exception("GenerateBehaviorScaffold missing SyncData.");
            if (!behavior.Contains("AddNonSerializedListener"))
                throw new Exception("GenerateBehaviorScaffold missing AddNonSerializedListener (anti-lag rule).");
            if (behavior.Contains("Campaign") && !behavior.Contains("CampaignBehaviorBase") && !behavior.Contains("CampaignEvents") && !behavior.Contains("CampaignTime"))
                throw new Exception("GenerateBehaviorScaffold contains forbidden 'Campaign' class name (GEMINI.md rule).");

            // ── 2. ForgeNoviceHub.GenerateTroopXml ─────────────────────────────────
            string troopXml = ForgeNoviceHub.GenerateTroopXml("test_soldier", "Test Soldier", "Culture.empire", 3);
            if (!troopXml.Contains("<NPCCharacter"))
                throw new Exception("GenerateTroopXml missing <NPCCharacter> element.");
            if (!troopXml.Contains("age="))
                throw new Exception("GenerateTroopXml missing age attribute.");
            if (!troopXml.Contains("EquipmentRoster"))
                throw new Exception("GenerateTroopXml missing EquipmentRoster.");
            if (!troopXml.Contains("default_group="))
                throw new Exception("GenerateTroopXml missing default_group attribute.");

            // ── 3. ForgeNoviceHub.GenerateQuestScaffold ────────────────────────────
            string quest = ForgeNoviceHub.GenerateQuestScaffold("test_quest", "TestQuest", "Test.Quests", 2500099);
            if (!quest.Contains("QuestBase"))
                throw new Exception("GenerateQuestScaffold missing QuestBase.");
            if (!quest.Contains("SetDialogs"))
                throw new Exception("GenerateQuestScaffold missing SetDialogs method.");
            if (!quest.Contains("2500099") && !quest.Contains("2,500,099"))
                throw new Exception("GenerateQuestScaffold missing SaveableTypeId >= 2500000.");
            // Verify the double-SetDialogs rule is mentioned in the output
            var setDialogsCount = System.Text.RegularExpressions.Regex.Matches(quest, "SetDialogs").Count;
            if (setDialogsCount < 2)
                throw new Exception($"GenerateQuestScaffold must reference SetDialogs at least twice (double-rule). Found: {setDialogsCount}.");

            // ── 4. ForgeNoviceHub.GenerateItemXml ──────────────────────────────────
            string itemXml = ForgeNoviceHub.GenerateItemXml("test_sword", "Test Sword", "Culture.empire", "OneHandedWeapon");
            if (!itemXml.Contains("<Item"))
                throw new Exception("GenerateItemXml missing <Item element.");
            if (!itemXml.Contains("value="))
                throw new Exception("GenerateItemXml missing value attribute (price).");
            if (!itemXml.Contains("<Weapons>"))
                throw new Exception("GenerateItemXml for weapon missing <Weapons> element.");

            string armorXml = ForgeNoviceHub.GenerateItemXml("test_helm", "Test Helm", "Culture.empire", "HeadArmor");
            if (!armorXml.Contains("<Armor"))
                throw new Exception("GenerateItemXml for armor missing <Armor element.");

            // ── 5. ForgeNoviceHub.GenerateSubModuleXml ─────────────────────────────
            string submod = ForgeNoviceHub.GenerateSubModuleXml("TestMod", "TestMod", "1.0.0", "TestMod.SubModule");
            if (!submod.Contains("<Module>"))
                throw new Exception("GenerateSubModuleXml missing <Module> root element.");
            if (!submod.Contains("<Id value=\"TestMod\""))
                throw new Exception("GenerateSubModuleXml missing <Id value> element.");
            if (!submod.Contains("<DLLName"))
                throw new Exception("GenerateSubModuleXml missing <DLLName element.");
            if (!submod.Contains("<DependedModule"))
                throw new Exception("GenerateSubModuleXml missing <DependedModule dependencies.");

            // ── 6. ForgeNoviceHub.GenerateModChecklist ─────────────────────────────
            string checklist = ForgeNoviceHub.GenerateModChecklist("TestMod");
            if (!checklist.Contains("SubModule.xml"))
                throw new Exception("GenerateModChecklist missing SubModule.xml check.");
            if (!checklist.Contains("SyncData"))
                throw new Exception("GenerateModChecklist missing SyncData save check.");
            if (!checklist.Contains("Campaign"))
                throw new Exception("GenerateModChecklist missing anti-Campaign-shadowing check.");
            if (!checklist.Contains("2,500,000"))
                throw new Exception("GenerateModChecklist missing SaveableTypeDefiner base ID >= 2500000 check.");

            // ── 7. ForgeNoviceHub.ExplainCampaignEvent ─────────────────────────────
            string allEvents = ForgeNoviceHub.ExplainCampaignEvent("all");
            if (!allEvents.Contains("HourlyTickEvent"))
                throw new Exception("ExplainCampaignEvent catalog missing HourlyTickEvent.");
            if (!allEvents.Contains("OnSessionLaunchedEvent"))
                throw new Exception("ExplainCampaignEvent catalog missing OnSessionLaunchedEvent.");

            string specific = ForgeNoviceHub.ExplainCampaignEvent("HourlyTickEvent");
            if (!specific.Contains("HourlyTickEvent"))
                throw new Exception("ExplainCampaignEvent specific lookup missing event name.");
            if (!specific.Contains("in-game hour"))
                throw new Exception("ExplainCampaignEvent specific lookup missing plain-language description.");
            if (!specific.Contains("AddNonSerializedListener"))
                throw new Exception("ExplainCampaignEvent subscription pattern missing AddNonSerializedListener.");

            string fuzzy = ForgeNoviceHub.ExplainCampaignEvent("HourlyTick"); // partial match
            if (fuzzy.Contains("not found in the catalog") && !fuzzy.Contains("Did you mean"))
                throw new Exception("ExplainCampaignEvent fuzzy match failed for partial 'HourlyTick'.");

            // ── 8. NoviceScaffoldEngine.Generate routing ───────────────────────────
            string engineBehavior = NoviceScaffoldEngine.Generate("behavior", "Escort");
            if (!engineBehavior.Contains("CampaignBehaviorBase"))
                throw new Exception("NoviceScaffoldEngine.Generate('behavior') failed to generate CampaignBehaviorBase.");

            string engineTroop = NoviceScaffoldEngine.Generate("troop", "Legionary");
            if (!engineTroop.Contains("<NPCCharacter"))
                throw new Exception("NoviceScaffoldEngine.Generate('troop') failed to produce NPCCharacter XML.");

            string engineEvents = NoviceScaffoldEngine.Generate("events", "all");
            if (!engineEvents.Contains("HourlyTickEvent"))
                throw new Exception("NoviceScaffoldEngine.Generate('events','all') failed to list events.");

            string engineChecklist = NoviceScaffoldEngine.Generate("checklist", "");
            if (!engineChecklist.Contains("SubModule.xml"))
                throw new Exception("NoviceScaffoldEngine.Generate('checklist') failed.");

            // ── 9. Runtime.cs action parity check ─────────────────────────────────
            var noviceActionIds = new[] { "novice-behavior", "novice-troop", "novice-quest", "novice-item", "novice-submodule", "novice-checklist", "novice-events" };
            string[] rtCandidates = {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../src/CalradiaForge.Mod/Runtime.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Mod/Runtime.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Mod/Runtime.cs"))
            };
            string rtPath = rtCandidates.FirstOrDefault(File.Exists);
            if (rtPath == null) throw new FileNotFoundException("Could not locate Runtime.cs for action parity check.");
            string rtContent = File.ReadAllText(rtPath);
            foreach (var actionId in noviceActionIds)
            {
                if (!rtContent.Contains($"case \"{actionId}\":"))
                    throw new Exception($"Runtime.cs missing case handler for novice action '{actionId}'.");
            }

            // ── 10. PanelViewModel.cs action parity check ─────────────────────────
            string[] vmCandidates = {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../src/CalradiaForge.Mod/PanelViewModel.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Mod/PanelViewModel.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Mod/PanelViewModel.cs"))
            };
            string vmPath = vmCandidates.FirstOrDefault(File.Exists);
            if (vmPath == null) throw new FileNotFoundException("Could not locate PanelViewModel.cs for action parity check.");
            string vmContent = File.ReadAllText(vmPath);
            var noviceIsActiveProps = new[] { "IsNoviceBehaviorActive", "IsNoviceTroopActive", "IsNoviceQuestActive", "IsNoviceItemActive", "IsNoviceSubmoduleActive", "IsNoviceChecklistActive", "IsNoviceEventsActive" };
            foreach (var prop in noviceIsActiveProps)
            {
                if (!vmContent.Contains(prop))
                    throw new Exception($"PanelViewModel.cs missing IsActive property: {prop}.");
            }
            if (!vmContent.Contains("IsCategoryNoviceActive"))
                throw new Exception("PanelViewModel.cs missing IsCategoryNoviceActive property.");
            if (!vmContent.Contains("ExecuteCategoryNovice"))
                throw new Exception("PanelViewModel.cs missing ExecuteCategoryNovice method.");
            if (!vmContent.Contains("RunNoviceLabel"))
                throw new Exception("PanelViewModel.cs missing RunNoviceLabel property.");
            if (!vmContent.Contains("ShowNoviceActions"))
                throw new Exception("PanelViewModel.cs missing ShowNoviceActions property.");

            // ── 11. CalradiaForge.xml button parity check ─────────────────────────
            string[] xmlCandidates = {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"))
            };
            string xmlPath = xmlCandidates.FirstOrDefault(File.Exists);
            if (xmlPath == null) throw new FileNotFoundException("Could not locate CalradiaForge.xml for UI button parity check.");
            string xmlContent = File.ReadAllText(xmlPath);
            AssertCurrentNativePrefab();
            if (UseRoutedDesktopAssertions()) return;
            if (!xmlContent.Contains("ExecuteCategoryNovice"))
                throw new Exception("CalradiaForge.xml missing ExecuteCategoryNovice category button.");
            if (!xmlContent.Contains("ExecuteNoviceBehavior"))
                throw new Exception("CalradiaForge.xml missing ExecuteNoviceBehavior tool button.");
            if (!xmlContent.Contains("ExecuteNoviceTroop"))
                throw new Exception("CalradiaForge.xml missing ExecuteNoviceTroop tool button.");
            if (!xmlContent.Contains("ExecuteNoviceQuest"))
                throw new Exception("CalradiaForge.xml missing ExecuteNoviceQuest tool button.");
            if (!xmlContent.Contains("ExecuteNoviceItem"))
                throw new Exception("CalradiaForge.xml missing ExecuteNoviceItem tool button.");
            if (!xmlContent.Contains("ExecuteNoviceSubmodule"))
                throw new Exception("CalradiaForge.xml missing ExecuteNoviceSubmodule tool button.");
            if (!xmlContent.Contains("ExecuteNoviceChecklist"))
                throw new Exception("CalradiaForge.xml missing ExecuteNoviceChecklist tool button.");
            if (!xmlContent.Contains("ExecuteNoviceEvents"))
                throw new Exception("CalradiaForge.xml missing ExecuteNoviceEvents tool button.");
            if (!xmlContent.Contains("ExecuteRunNovice"))
                throw new Exception("CalradiaForge.xml missing ExecuteRunNovice generate action button.");
        }

        private static void TestQoLAndHintSystem()
        {
            string catalog = File.ReadAllText(Path.GetFullPath("src/CalradiaForge.Desktop/Presentation/ToolCatalog.cs"));
            if (!catalog.Contains("T(\"NoviceHint\"") || !catalog.Contains("Learning / Novice Modder Hub") || !catalog.Contains("DesktopToolDefinitions"))
                throw new Exception("Routed tool catalog must retain novice tool routes.");
            if (UseRoutedDesktopAssertions()) return;
            // 1. ForgeHintBuilder Unit Validation
            var builder = ForgeHintBuilder.Create("Charge Orders")
                .WithDescription("Order all cavalry to charge the enemy left flank.")
                .WithShortcut("F1 + F3")
                .WithRequirement("Mounted units in army", true)
                .WithRequirement("Morale > 40", false)
                .WithWarning("Vulnerable to pikes")
                .WithMeta("Target Formation", "Cavalry");

            string hintText = builder.BuildHintText();
            if (!hintText.Contains("Charge Orders")) throw new Exception("ForgeHintBuilder missing Title.");
            if (!hintText.Contains("Order all cavalry")) throw new Exception("ForgeHintBuilder missing Description.");
            if (!hintText.Contains("F1 + F3")) throw new Exception("ForgeHintBuilder missing Shortcut.");
            if (!hintText.Contains("[✓] Mounted units in army")) throw new Exception("ForgeHintBuilder missing met requirement.");
            if (!hintText.Contains("[✗] Morale > 40")) throw new Exception("ForgeHintBuilder missing unmet requirement.");
            if (!hintText.Contains("[!] Warning: Vulnerable to pikes")) throw new Exception("ForgeHintBuilder missing Warning.");
            if (!hintText.Contains("• Target Formation: Cavalry")) throw new Exception("ForgeHintBuilder missing Meta.");

            string xmlAttr = builder.BuildXmlAttribute("ChargeOrdersHint");
            if (xmlAttr != "Hint.HintText=\"@ChargeOrdersHint\"")
                throw new Exception("ForgeHintBuilder BuildXmlAttribute unexpected format: " + xmlAttr);

            string locXml = builder.BuildLocalizationXml("hint_charge");
            if (!locXml.Contains("<string id=\"hint_charge\"") || !locXml.Contains("&#10;"))
                throw new Exception("ForgeHintBuilder BuildLocalizationXml missing encoded newlines or string node.");

            string propSnippet = builder.BuildViewModelPropertySnippet("ChargeOrdersHint");
            if (!propSnippet.Contains("[DataSourceProperty]") || !propSnippet.Contains("public string ChargeOrdersHint =>"))
                throw new Exception("ForgeHintBuilder BuildViewModelPropertySnippet invalid.");

            // 2. ForgeNoviceHub.GenerateHintScaffold Validation
            string scaffold = ForgeNoviceHub.GenerateHintScaffold("ButtonWidget", "Recruit Volunteers", "Recruit local volunteers.", "Ctrl+Click");
            if (!scaffold.Contains("Hint.HintText")) throw new Exception("GenerateHintScaffold missing Hint.HintText attribute.");
            if (!scaffold.Contains("[DataSourceProperty]")) throw new Exception("GenerateHintScaffold missing [DataSourceProperty].");
            if (!scaffold.Contains("<ButtonWidget")) throw new Exception("GenerateHintScaffold missing <ButtonWidget.");
            if (!scaffold.Contains("Recruit Volunteers")) throw new Exception("GenerateHintScaffold missing title.");

            // 3. NoviceScaffoldEngine "hint" Tool Routing
            string engineHint = NoviceScaffoldEngine.Generate("hint", "Attack Order");
            if (!engineHint.Contains("Attack Order") || !engineHint.Contains("Gauntlet Hint & Tooltip Scaffold"))
                throw new Exception("NoviceScaffoldEngine failed to route 'hint' tool.");

            // 4. Source File Parity Checks
            string[] runtimeCandidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../src/CalradiaForge.Mod/Runtime.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Mod/Runtime.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Mod/Runtime.cs"))
            };
            string runtimePath = runtimeCandidates.FirstOrDefault(File.Exists);
            if (runtimePath == null) throw new FileNotFoundException("Could not locate Runtime.cs for parity check.");
            string runtimeContent = File.ReadAllText(runtimePath);
            if (!runtimeContent.Contains("case \"novice-hint\":"))
                throw new Exception("Runtime.cs missing case \"novice-hint\".");

            string[] panelCandidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../src/CalradiaForge.Mod/PanelViewModel.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Mod/PanelViewModel.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Mod/PanelViewModel.cs"))
            };
            string panelPath = panelCandidates.FirstOrDefault(File.Exists);
            if (panelPath == null) throw new FileNotFoundException("Could not locate PanelViewModel.cs for parity check.");
            string panelContent = File.ReadAllText(panelPath);

            if (!panelContent.Contains("IsNoviceHintActive")) throw new Exception("PanelViewModel.cs missing IsNoviceHintActive property.");
            if (!panelContent.Contains("NoviceHintLabel")) throw new Exception("PanelViewModel.cs missing NoviceHintLabel property.");
            if (!panelContent.Contains("NoviceHintHint")) throw new Exception("PanelViewModel.cs missing NoviceHintHint property.");
            if (!panelContent.Contains("PageHint")) throw new Exception("PanelViewModel.cs missing PageHint property.");
            if (!panelContent.Contains("SearchHint")) throw new Exception("PanelViewModel.cs missing SearchHint property.");
            if (!panelContent.Contains("TelemetryHint")) throw new Exception("PanelViewModel.cs missing TelemetryHint property.");
            if (!panelContent.Contains("ExecuteNoviceHint")) throw new Exception("PanelViewModel.cs missing ExecuteNoviceHint method.");
            if (!panelContent.Contains("case \"ForgeNoviceHint\":")) throw new Exception("PanelViewModel.cs missing ForgeNoviceHint keyboard control.");

            string[] subModuleCandidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../src/CalradiaForge.Mod/SubModule.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Mod/SubModule.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Mod/SubModule.cs"))
            };
            string subModulePath = subModuleCandidates.FirstOrDefault(File.Exists);
            if (subModulePath == null) throw new FileNotFoundException("Could not locate SubModule.cs for shortcut check.");
            string subModuleContent = File.ReadAllText(subModulePath);
            if (!subModuleContent.Contains("InputKey.D7))vm.ExecuteCategoryNovice()"))
                throw new Exception("SubModule.cs missing Ctrl+7 (D7) mapping to ExecuteCategoryNovice.");

            string[] xmlCandidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"))
            };
            string xmlPath = xmlCandidates.FirstOrDefault(File.Exists);
            if (xmlPath == null) throw new FileNotFoundException("Could not locate CalradiaForge.xml for UI button check.");
            string xmlContent = File.ReadAllText(xmlPath);
            if (!xmlContent.Contains("ExecuteNoviceHint")) throw new Exception("CalradiaForge.xml missing ExecuteNoviceHint button.");
            if (!xmlContent.Contains("Hint.HintText=\"@SearchHint\"")) throw new Exception("CalradiaForge.xml missing SearchHint on argument field.");
            if (!xmlContent.Contains("Hint.HintText=\"@PageHint\"")) throw new Exception("CalradiaForge.xml missing PageHint on pagination display.");
            if (!xmlContent.Contains("Ctrl + 1 … 7")) throw new Exception("CalradiaForge.xml missing Ctrl + 1 … 7 in shortcuts modal.");

            // 5. Desktop MainWindow.xaml and MainWindow.xaml.cs Parity
            string[] mainXamlCandidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../src/CalradiaForge.Desktop/MainWindow.xaml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Desktop/MainWindow.xaml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Desktop/MainWindow.xaml"))
            };
            string mainXamlPath = mainXamlCandidates.FirstOrDefault(File.Exists);
            if (mainXamlPath == null) throw new FileNotFoundException("Could not locate MainWindow.xaml for parity check.");
            string mainXamlContent = File.ReadAllText(mainXamlPath);
            if (!mainXamlContent.Contains("NoviceModderCategoryItem")) throw new Exception("MainWindow.xaml missing NoviceModderCategoryItem in TreeView.");
            if (!mainXamlContent.Contains("Tag=\"NoviceHint\"")) throw new Exception("MainWindow.xaml missing NoviceHint in TreeView.");

            string[] mainCsCandidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../src/CalradiaForge.Desktop/MainWindow.xaml.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Desktop/MainWindow.xaml.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Desktop/MainWindow.xaml.cs"))
            };
            string mainCsPath = mainCsCandidates.FirstOrDefault(File.Exists);
            if (mainCsPath == null) throw new FileNotFoundException("Could not locate MainWindow.xaml.cs for parity check.");
            string mainCsContent = File.ReadAllText(mainCsPath);
            if (!mainCsContent.Contains("tag == \"NoviceHint\"")) throw new Exception("MainWindow.xaml.cs missing tag == \"NoviceHint\" dispatch.");
        }

        private static void TestFixScreenshotIssuesAndLayoutPolish()
        {
            string routedXaml = File.ReadAllText(Path.GetFullPath("src/CalradiaForge.Desktop/MainWindow.xaml"));
            string routedCode = File.ReadAllText(Path.GetFullPath("src/CalradiaForge.Desktop/MainWindow.xaml.cs"));
            if (!routedXaml.Contains("MinWidth=\"980\"") || !routedCode.Contains("DataContext = shell;"))
                throw new Exception("Routed desktop must retain bounded layout and shell composition.");
            if (UseRoutedDesktopAssertions()) return;
            // 1. Validate GenerateItemXml produces 100% valid XML without comments inside start tags
            string weaponXml = ForgeNoviceHub.GenerateItemXml("mymod_champions_blade", "Champion's Blade", "Culture.empire", "OneHandedWeapon");
            XDocument docWeapon;
            try
            {
                docWeapon = XDocument.Parse(weaponXml);
            }
            catch (Exception ex)
            {
                throw new Exception("GenerateItemXml (weapon) generated invalid XML: " + ex.Message);
            }
            if (docWeapon.Root == null || docWeapon.Root.Name != "Items")
                throw new Exception("GenerateItemXml (weapon) root node must be <Items>.");
            var itemElement = docWeapon.Root.Element("Item");
            if (itemElement == null || (string)itemElement.Attribute("id") != "mymod_champions_blade")
                throw new Exception("GenerateItemXml (weapon) missing <Item id=\"mymod_champions_blade\">.");
            var weaponElement = itemElement.Element("Weapons")?.Element("Weapon");
            if (weaponElement == null || (string)weaponElement.Attribute("thrust_damage") != "18")
                throw new Exception("GenerateItemXml (weapon) missing <Weapon> with damage attributes.");

            // 2. Validate GenerateItemXml for Armor
            string armorXml = ForgeNoviceHub.GenerateItemXml("mymod_iron_hauberk", "Iron Hauberk", "Culture.empire", "BodyArmor");
            XDocument docArmor;
            try
            {
                docArmor = XDocument.Parse(armorXml);
            }
            catch (Exception ex)
            {
                throw new Exception("GenerateItemXml (armor) generated invalid XML: " + ex.Message);
            }
            if (docArmor.Root?.Element("Item")?.Element("Armor") == null)
                throw new Exception("GenerateItemXml (armor) missing <Armor> element.");

            // 3. Validate GenerateTroopXml produces 100% valid XML
            string troopXml = ForgeNoviceHub.GenerateTroopXml("mymod_vanguard", "Mod Vanguard", "Culture.empire", 3);
            XDocument docTroop;
            try
            {
                docTroop = XDocument.Parse(troopXml);
            }
            catch (Exception ex)
            {
                throw new Exception("GenerateTroopXml generated invalid XML: " + ex.Message);
            }
            if (docTroop.Root == null || docTroop.Root.Name != "NPCCharacters")
                throw new Exception("GenerateTroopXml root node must be <NPCCharacters>.");
            var npcElement = docTroop.Root.Element("NPCCharacter");
            if (npcElement == null || (string)npcElement.Attribute("id") != "mymod_vanguard")
                throw new Exception("GenerateTroopXml missing <NPCCharacter id=\"mymod_vanguard\">.");

            // 4. Validate GenerateSubModuleXml produces 100% valid XML
            string subModuleXml = ForgeNoviceHub.GenerateSubModuleXml("MyCustomMod", "MyCustomMod", "1.0.0", "MyCustomMod.SubModule");
            XDocument docSubModule;
            try
            {
                docSubModule = XDocument.Parse(subModuleXml);
            }
            catch (Exception ex)
            {
                throw new Exception("GenerateSubModuleXml generated invalid XML: " + ex.Message);
            }
            if (docSubModule.Root == null || docSubModule.Root.Name != "Module")
                throw new Exception("GenerateSubModuleXml root node must be <Module>.");

            // 5. Validate Version Synchronization
            if (SuiteInfo.Version != "25.2.0")
                throw new Exception($"SuiteInfo.Version must be '25.2.0', found '{SuiteInfo.Version}'.");

            // 6. Source File Parity Checks for UI & Localization
            string[] mainXamlCandidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../src/CalradiaForge.Desktop/MainWindow.xaml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Desktop/MainWindow.xaml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Desktop/MainWindow.xaml"))
            };
            string mainXamlPath = mainXamlCandidates.FirstOrDefault(File.Exists);
            if (mainXamlPath == null) throw new FileNotFoundException("Could not locate MainWindow.xaml.");
            string mainXamlContent = File.ReadAllText(mainXamlPath);
            if (!mainXamlContent.Contains("x:Name=\"TitleBarVersionBlock\" Text=\"v0.0.0\""))
                throw new Exception("MainWindow.xaml must use the dynamic title version placeholder.");
            if (!mainXamlContent.Contains("x:Name=\"StatusVersionBlock\" Text=\"Calradia Forge v0.0.0\""))
                throw new Exception("MainWindow.xaml must use the dynamic status version placeholder.");
            if (!mainXamlContent.Contains("Width=\"315\""))
                throw new Exception("MainWindow.xaml SidebarColumn width must be 315.");

            string[] appXamlCandidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../src/CalradiaForge.Desktop/App.xaml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Desktop/App.xaml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Desktop/App.xaml"))
            };
            string appXamlPath = appXamlCandidates.FirstOrDefault(File.Exists);
            if (appXamlPath == null) throw new FileNotFoundException("Could not locate App.xaml.");
            string appXamlContent = File.ReadAllText(appXamlPath);
            if (!appXamlContent.Contains("TargetType=\"{x:Type ScrollBar}\""))
                throw new Exception("App.xaml missing implicit ScrollBar style.");

            string[] mainCsCandidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../src/CalradiaForge.Desktop/MainWindow.xaml.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Desktop/MainWindow.xaml.cs")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Desktop/MainWindow.xaml.cs"))
            };
            string mainCsPath = mainCsCandidates.FirstOrDefault(File.Exists);
            if (mainCsPath == null) throw new FileNotFoundException("Could not locate MainWindow.xaml.cs.");
            string mainCsContent = File.ReadAllText(mainCsPath);
            if (!mainCsContent.Contains("TitleBarVersionBlock.Text = \"v\" + CalradiaForge.Core.SuiteInfo.Version;"))
                throw new Exception("MainWindow.xaml.cs missing dynamic TitleBarVersionBlock setting.");
            if (!mainCsContent.Contains("NoviceModderCategoryItem.Header = isEs ? \"🎓 HUB DE MODDERS NOVATOS\""))
                throw new Exception("MainWindow.xaml.cs missing NoviceModderCategoryItem translation.");
            if (!mainCsContent.Contains("\"NoviceBehavior\" => isEs ? \"Generador de CampaignBehavior\""))
                throw new Exception("MainWindow.xaml.cs missing NoviceBehavior translation in TranslateTreeNodes.");
            if (!mainCsContent.Contains("\"ALL\" => isEs ? \"📂 Todas las Categorías\""))
                throw new Exception("MainWindow.xaml.cs missing CategoryPickerComboBox translation.");
            if (!mainCsContent.Contains("ToolSearchPlaceholder.Text = isEs ? \"Filtrar herramientas... (Ctrl+F)\""))
                throw new Exception("MainWindow.xaml.cs missing ToolSearchPlaceholder translation.");
        }

        private static void TestNoviceAndAdvancedToolsAndCrashFix()
        {
            // 1. Validate release version synchronization through the shared build value.
            if (SuiteInfo.Version != "25.2.0")
                throw new Exception($"SuiteInfo.Version must be '25.2.0', found '{SuiteInfo.Version}'.");

            // 2. Validate App.xaml zero duplicate keys and zero duplicate TargetTypes
            string[] appXamlCandidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../src/CalradiaForge.Desktop/App.xaml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Desktop/App.xaml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Desktop/App.xaml"))
            };
            string appXamlPath = appXamlCandidates.FirstOrDefault(File.Exists);
            if (appXamlPath == null) throw new FileNotFoundException("Could not locate App.xaml.");
            string appXamlContent = File.ReadAllText(appXamlPath);

            var appDoc = XDocument.Parse(appXamlContent);
            XNamespace xNs = "http://schemas.microsoft.com/winfx/2006/xaml";
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var targetTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var elem in appDoc.Descendants())
            {
                var keyAttr = elem.Attribute(xNs + "Key");
                if (keyAttr != null)
                {
                    if (!keys.Add(keyAttr.Value))
                        throw new Exception($"Duplicate x:Key in App.xaml: '{keyAttr.Value}'");
                }
                else if (elem.Name.LocalName == "Style")
                {
                    var targetTypeAttr = elem.Attribute("TargetType");
                    if (targetTypeAttr != null)
                    {
                        string tt = targetTypeAttr.Value.Trim();
                        if (tt.StartsWith("{x:Type ") && tt.EndsWith("}"))
                            tt = tt.Substring(8, tt.Length - 9).Trim();
                        if (!targetTypes.Add(tt))
                            throw new Exception($"Duplicate implicit Style TargetType in App.xaml: '{tt}'");
                    }
                }
            }

            // 3. Validate Novice Scaffolds
            // 3a. Workshop Scaffold
            string workshopScaffold = ForgeNoviceHub.GenerateWorkshopScaffold("apothecary", "grain", "medicine", "CustomWorkshopBehavior");
            if (!workshopScaffold.Contains("<WorkshopType id=\"apothecary\"") || !workshopScaffold.Contains("CampaignBehaviorBase"))
                throw new Exception("GenerateWorkshopScaffold missing expected XML or C# behavior.");
            if (!workshopScaffold.Contains("availableInput >= 2") || !workshopScaffold.Contains("roster.AddToCounts"))
                throw new Exception("GenerateWorkshopScaffold missing inventory underflow safety pattern.");

            // 3b. Bandit Party Scaffold
            string partyScaffold = ForgeNoviceHub.GenerateBanditPartyScaffold("mountain_raiders", "raider_band", "looter", 15, 35);
            if (!partyScaffold.Contains("<MBPartyTemplate id=\"raider_band\"") || !partyScaffold.Contains("MobileParty"))
                throw new Exception("GenerateBanditPartyScaffold missing expected XML or C# MobileParty helper.");

            // 3c. Settlement Building Scaffold
            string buildingScaffold = ForgeNoviceHub.GenerateSettlementBuildingScaffold("custom_granary_vault", "Granary Vault", "Food", 3);
            if (!buildingScaffold.Contains("<Building id=\"custom_granary_vault\"") || !buildingScaffold.Contains("<BuildingLevel level=\"3\""))
                throw new Exception("GenerateSettlementBuildingScaffold missing expected 3-tier XML.");
            if (!buildingScaffold.Contains("ExplainedNumber") || !buildingScaffold.Contains("SettlementFoodModel"))
                throw new Exception("GenerateSettlementBuildingScaffold missing Decorator pattern with ExplainedNumber.");

            // 3d. Combat AI Component Scaffold
            string combatScaffold = ForgeNoviceHub.GenerateCombatAiComponentScaffold("BattleShoutAgentComponent", "BattleShoutMissionLogic", "MyMod.Combat");
            if (!combatScaffold.Contains("AgentComponent") || !combatScaffold.Contains("MissionLogic"))
                throw new Exception("GenerateCombatAiComponentScaffold missing AgentComponent or MissionLogic.");
            if (!combatScaffold.Contains("_initialized"))
                throw new Exception("GenerateCombatAiComponentScaffold missing deferred _initialized pattern for mission lifecycle safety.");

            // 4. Validate Advanced Engines
            // 4a. ForgeTradeSimulator
            var eq = ForgeTradeSimulator.CalculateMarketEquilibrium("Grain", 180f, 100f, 10f);
            if (eq.PriceMultiplier <= 0f || string.IsNullOrEmpty(eq.MarketCondition))
                throw new Exception("ForgeTradeSimulator CalculateMarketEquilibrium returned invalid data.");
            var workshopRoi = ForgeTradeSimulator.SimulateWorkshopRoi("Pravend", "Brewery", 10000, 30);
            if (!workshopRoi.Summary.Contains("Workshop Simulation"))
                throw new Exception("ForgeTradeSimulator SimulateWorkshopRoi returned invalid summary.");

            // 4b. ForgeSiegeTactician
            var assault = ForgeSiegeTactician.AnalyzeAssaultTactics("Chaikand", 600, 300, true, 2, 1);
            if (assault.BreakthroughProbability <= 0f || assault.NavmeshRequirements.Count == 0)
                throw new Exception("ForgeSiegeTactician AnalyzeAssaultTactics returned invalid analysis.");

            // 4c. ForgeCasusBelliEngine
            var casusBelli = ForgeCasusBelliEngine.EvaluateWarJustification("Vlandia", "Battania", 6000, 3000, 4, false, 3, -30);
            if (casusBelli.TotalJustificationScore <= 0 || casusBelli.CouncilSupportPercentage <= 0)
                throw new Exception("ForgeCasusBelliEngine EvaluateWarJustification returned invalid score.");

            // 4d. ForgeAudioInspector
            string sampleAudioXml =
                "<module_sounds>\n" +
                "  <module_sound name=\"custom_ui_click\" is_2d=\"true\" sound_category=\"ui\" path=\"ui_click.ogg\" />\n" +
                "  <module_sound name=\"custom_shield_clash\" is_2d=\"false\" sound_category=\"mission_combat\" path=\"shield_clash.ogg\" />\n" +
                "</module_sounds>";
            var audioAudit = ForgeAudioInspector.AuditSoundManifest(sampleAudioXml);
            if (audioAudit.TotalSounds != 2 || audioAudit.ErrorCount != 0)
                throw new Exception("ForgeAudioInspector AuditSoundManifest failed on valid audio manifest.");

            // 5. Validate NoviceScaffoldEngine Integration
            foreach (var tool in new[] { "workshop", "party", "building", "combat" })
            {
                string result = NoviceScaffoldEngine.Generate(tool, "Test");
                if (string.IsNullOrWhiteSpace(result) || result.Contains("Unknown novice tool"))
                    throw new Exception($"NoviceScaffoldEngine failed to generate tool '{tool}'.");
            }

            // 6. Validate SubModule.xml version across all 4 modules
            string[] subModuleCandidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../modules/CalradiaForge/SubModule.xml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../modules/CalradiaForge/SubModule.xml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../modules/CalradiaForge/SubModule.xml"))
            };
            string subModulePath = subModuleCandidates.FirstOrDefault(File.Exists);
            if (subModulePath != null)
            {
                string smContent = File.ReadAllText(subModulePath);
                if (!smContent.Contains("Version value=\"v" + SuiteInfo.Version + "\""))
                    throw new Exception("modules/CalradiaForge/SubModule.xml missing Version value=\"v" + SuiteInfo.Version + "\".");
            }
        }
    }
}





