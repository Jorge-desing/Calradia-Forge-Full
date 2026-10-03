using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using CalradiaForge.Core;
using CalradiaForge.Sdk;

namespace CalradiaForge.ContentShowcase.Generator
{
    internal static class Program
    {
        private const string ModuleId = "CalradiaForgeContentShowcase";
        private const string ItemId = "cfcs_practice_whip";
        private const string TroopId = "cfcs_example_recruit";
        private const string NativeItemPath = "Modules/SandBoxCore/ModuleData/items/weapons.xml";

        private static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0) return PrintUsage();

                string repoRoot = FindRepositoryRoot();
                switch (args[0])
                {
                    case "generate":
                        Generate(repoRoot);
                        Console.WriteLine("Generated the deterministic Calradia Forge content showcase.");
                        return 0;
                    case "verify":
                        Verify(repoRoot, GetOption(args, "--game-root"));
                        return 0;
                    case "contract-tests":
                        RunContractVerifierRegressionTests();
                        return 0;
                    case "benchmark":
                        RunBatchBenchmark();
                        return 0;
                    default:
                        return PrintUsage();
                }
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("Content showcase validation failed: " + error.Message);
                return 1;
            }
        }

        private static int PrintUsage()
        {
            Console.Error.WriteLine("Usage: generate | verify --game-root <Bannerlord root> | contract-tests | benchmark");
            return 2;
        }

        private static void Generate(string repoRoot)
        {
            var files = BuildFiles();
            foreach (KeyValuePair<string, string> file in files)
            {
                string path = Path.Combine(repoRoot, file.Key.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, file.Value, new UTF8Encoding(false));
            }
        }

        private static void Verify(string repoRoot, string gameRoot)
        {
            if (string.IsNullOrWhiteSpace(gameRoot) || !Directory.Exists(gameRoot))
                throw new DirectoryNotFoundException("Pass --game-root with the installed Bannerlord directory.");

            SortedDictionary<string, string> first = BuildFiles();
            SortedDictionary<string, string> second = BuildFiles();
            foreach (KeyValuePair<string, string> file in first)
            {
                if (!second.TryGetValue(file.Key, out string repeated) || !string.Equals(file.Value, repeated, StringComparison.Ordinal))
                    throw new InvalidDataException("Generation is not deterministic for " + file.Key + ".");

                string path = Path.Combine(repoRoot, file.Key.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path)) throw new FileNotFoundException("Generated showcase file is missing.", path);
                byte[] expectedBytes = new UTF8Encoding(false).GetBytes(file.Value);
                byte[] actualBytes = File.ReadAllBytes(path);
                if (!expectedBytes.SequenceEqual(actualBytes))
                    throw new InvalidDataException("Generated file differs from its source generator: " + file.Key + ". Run the BAT generate step.");
            }

            string moduleRoot = Path.Combine(repoRoot, "modules", ModuleId);
            VerifyManifest(moduleRoot);
            VerifyPage(moduleRoot, repoRoot);
            VerifyLocalization(moduleRoot, repoRoot);
            VerifySchema(gameRoot, moduleRoot, "Items.xsd", "ModuleData/calradia_forge_content_showcase_items.xml");
            VerifySchema(gameRoot, moduleRoot, "NPCCharacters.xsd", "ModuleData/calradia_forge_content_showcase_characters.xml");
            VerifyNativeMesh(gameRoot, moduleRoot);
            VerifyEquipmentReferences(moduleRoot);

            Console.WriteLine("Deterministic generation, manifest, UI bindings, EN/ES keys, native references and installed-game XSD validation passed.");
            Console.WriteLine("Items.xsd SHA-256: " + ComputeSha256(Path.Combine(gameRoot, "XmlSchemas", "Items.xsd")));
            Console.WriteLine("NPCCharacters.xsd SHA-256: " + ComputeSha256(Path.Combine(gameRoot, "XmlSchemas", "NPCCharacters.xsd")));
            Console.WriteLine("Native weapons.xml SHA-256: " + ComputeSha256(Path.Combine(gameRoot, NativeItemPath.Replace('/', Path.DirectorySeparatorChar))));
            Console.WriteLine("Source XML validation does not verify engine loading or live rendering.");
        }

        private static SortedDictionary<string, string> BuildFiles()
        {
            var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
            string modulePrefix = "modules/" + ModuleId + "/";
            files.Add(modulePrefix + "SubModule.xml", BuildManifest());
            files.Add(modulePrefix + "ModuleData/calradia_forge_content_showcase_items.xml", BuildItemsXml());
            files.Add(modulePrefix + "ModuleData/calradia_forge_content_showcase_characters.xml", BuildCharactersXml());
            files.Add(modulePrefix + "GUI/Prefabs/ForgeContentShowcase.xml", BuildPrefab());
            AddLanguage(files, modulePrefix, "EN", "English", "en", "Calradia Forge content showcase", "Practice Whip", "Forge Recruit", "Close", "This read-only page demonstrates a module-owned Gauntlet ViewModel and static troop and item XML. It does not run tests or change game state.");
            AddLanguage(files, modulePrefix, "SP", "Spanish", "es", "Muestra de contenido de Calradia Forge", "Látigo de práctica", "Recluta Forge", "Cerrar", "Esta página de solo lectura muestra un ViewModel Gauntlet propio del módulo y XML estático de tropa y objeto. No ejecuta pruebas ni cambia el estado del juego.");
            files.Add("examples/CalradiaForge.ContentShowcase/Module/Generated/ForgeContentShowcasePageViewModel.cs", BuildViewModel());

            // Raw string literals retain source line endings. Normalize every generated
            // file so identical source produces identical bytes across Windows and Unix.
            foreach (string path in files.Keys.ToArray())
                files[path] = NormalizeNewlines(files[path]);
            return files;
        }

        private static string NormalizeNewlines(string value)
        {
            return value.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private static string BuildManifest()
        {
            return """
            <?xml version="1.0" encoding="utf-8"?>
            <Module>
              <Name value="Calradia Forge Content Showcase" />
              <Id value="CalradiaForgeContentShowcase" />
              <Version value="v25.2.0" />
              <SingleplayerModule value="true" />
              <MultiplayerModule value="false" />
              <DependedModules>
                <DependedModule Id="Native" />
                <DependedModule Id="SandBoxCore" />
                <DependedModule Id="CalradiaForge" />
              </DependedModules>
              <SubModules>
                <SubModule>
                  <Name value="Calradia Forge Content Showcase" />
                  <DLLName value="CalradiaForge.ContentShowcase.dll" />
                  <SubModuleClassType value="CalradiaForge.ContentShowcase.SubModule" />
                  <Tags>
                    <Tag key="DedicatedServerType" value="none" />
                    <Tag key="IsNoRenderModeElement" value="false" />
                  </Tags>
                </SubModule>
              </SubModules>
              <Xmls>
                <XmlNode><XmlName id="Items" path="calradia_forge_content_showcase_items" /></XmlNode>
                <XmlNode><XmlName id="NPCCharacters" path="calradia_forge_content_showcase_characters" /></XmlNode>
              </Xmls>
            </Module>
            """;
        }

        private static string BuildItemsXml()
        {
            XElement item = ForgeItemBuilder.Create(ItemId)
                .WithName("{=cfcs_practice_whip}Practice Whip")
                .WithMesh("horse_whip")
                .WithCulture("empire")
                .WithWeight(0.2)
                .WithValue(50)
                .WithType("OneHandedWeapon")
                .AsWeapon("OneHandedSword", 79, 99, 6, "Blunt", 5, "Blunt", "onehanded_block_shield_swing")
                .BuildElement();
            item.SetAttributeValue("body_name", "bo_mace_a");
            item.SetAttributeValue("recalculate_body", "true");
            item.SetAttributeValue("item_holsters", "sword_left_hip");
            return SerializeDocument(new XElement("Items", item));
        }

        private static string BuildCharactersXml()
        {
            XElement troop = ForgeTroopBuilder.Create(TroopId)
                .WithName("{=cfcs_example_recruit}Forge Recruit")
                .WithAge(25)
                .WithLevel(5)
                .WithCulture("empire")
                .WithDefaultGroup("Infantry")
                .WithOccupation("Soldier")
                .AddSkill("OneHanded", 40)
                .AddBattleEquipment("Item0", ItemId)
                .AddCivilianEquipment("Item0", ItemId)
                .BuildElement();
            return SerializeDocument(new XElement("NPCCharacters", troop));
        }

        private static string BuildPrefab()
        {
            return """
            <?xml version="1.0" encoding="utf-8"?>
            <Prefab>
              <Window>
                <Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" Sprite="BlankWhiteSquare_9" Color="#000000CC">
                  <Children>
                    <Widget WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" SuggestedWidth="760" SuggestedHeight="390" HorizontalAlignment="Center" VerticalAlignment="Center" Sprite="BlankWhiteSquare_9" Color="#13231EFF">
                      <Children>
                        <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="64" MarginLeft="28" MarginRight="28" MarginTop="24" Brush="GameTip.Title.Text" Text="@Title" DoNotAcceptEvents="true" />
                        <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="180" MarginLeft="30" MarginRight="30" MarginTop="112" Brush="GameTip.Text" Text="@Body" DoNotAcceptEvents="true" />
                        <ButtonWidget Id="CFCS_Close" WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" SuggestedWidth="180" SuggestedHeight="48" HorizontalAlignment="Right" MarginRight="28" MarginTop="320" Brush="ButtonBrush2" IsFocusable="true" DoNotPassEventsToChildren="true" Command.Click="ExecuteClose">
                          <Children><TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Brush="GameTip.Text" Text="@CloseLabel" DoNotAcceptEvents="true" /></Children>
                        </ButtonWidget>
                      </Children>
                    </Widget>
                  </Children>
                </Widget>
              </Window>
            </Prefab>
            """;
        }

        private static string BuildViewModel()
        {
            return """
            using CalradiaForge.Sdk;
            using TaleWorlds.Library;
            using TaleWorlds.Localization;

            namespace CalradiaForge.ContentShowcase
            {
                [ForgeUiPage("calradiaforge.content.showcase", "ForgeContentShowcase", "cfcs_title", Context = Context.Any)]
                public sealed class ForgeContentShowcasePageViewModel : ViewModel
                {
                    [DataSourceProperty]
                    public string Title => new TextObject("{=cfcs_title}Calradia Forge content showcase").ToString();

                    [DataSourceProperty]
                    public string Body => new TextObject("{=cfcs_body}This read-only page demonstrates a module-owned Gauntlet ViewModel and static troop and item XML. It does not run tests or change game state.").ToString();

                    [DataSourceProperty]
                    public string CloseLabel => new TextObject("{=cfcs_close}Close").ToString();

                    [ForgeUiCommand("close", "ExecuteClose", Context = Context.Any, ChangesState = false)]
                    public void ExecuteClose() => ForgeUI.ClosePage();
                }
            }
            """;
        }

        private static void AddLanguage(SortedDictionary<string, string> files, string modulePrefix,
            string folder, string languageName, string iso, string title, string itemName, string troopName,
            string closeLabel, string body)
        {
            files.Add(modulePrefix + "ModuleData/Languages/" + folder + "/language_data.xml",
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<LanguageData id=\"" + languageName + "\" name=\"" + languageName + "\" supported_iso=\"" + iso + "\" under_development=\"false\"><LanguageFile xml_path=\"" + folder + "/content_showcase_strings.xml\" /></LanguageData>\n");

            var strings = new XElement("base",
                new XAttribute("type", "string"),
                new XElement("tags", new XElement("tag", new XAttribute("language", languageName))),
                new XElement("strings",
                    new XElement("string", new XAttribute("id", "cfcs_title"), new XAttribute("text", title)),
                    new XElement("string", new XAttribute("id", "cfcs_body"), new XAttribute("text", body)),
                    new XElement("string", new XAttribute("id", "cfcs_practice_whip"), new XAttribute("text", itemName)),
                    new XElement("string", new XAttribute("id", "cfcs_example_recruit"), new XAttribute("text", troopName)),
                    new XElement("string", new XAttribute("id", "cfcs_close"), new XAttribute("text", closeLabel))));
            files.Add(modulePrefix + "ModuleData/Languages/" + folder + "/content_showcase_strings.xml",
                SerializeDocument(strings));
        }

        private static string SerializeDocument(XElement root)
        {
            var builder = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(builder, new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace
            }))
            {
                root.WriteTo(writer);
            }
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + builder + "\n";
        }

        private static void VerifyManifest(string moduleRoot)
        {
            var manifest = XDocument.Load(Path.Combine(moduleRoot, "SubModule.xml"));
            if ((string)manifest.Root?.Element("Id")?.Attribute("value") != ModuleId)
                throw new InvalidDataException("Module folder and manifest ID must match.");

            string dll = (string)manifest.Descendants("DLLName").SingleOrDefault()?.Attribute("value");
            string moduleClass = (string)manifest.Descendants("SubModuleClassType").SingleOrDefault()?.Attribute("value");
            if (dll != "CalradiaForge.ContentShowcase.dll" || moduleClass != "CalradiaForge.ContentShowcase.SubModule")
                throw new InvalidDataException("The module manifest must resolve the showcase assembly and SubModule class.");

            string[] dependencies = manifest.Descendants("DependedModule").Select(node => (string)node.Attribute("Id")).ToArray();
            foreach (string expected in new[] { "Native", "SandBoxCore", "CalradiaForge" })
                if (!dependencies.Contains(expected, StringComparer.Ordinal))
                    throw new InvalidDataException("The showcase is missing required module dependency " + expected + ".");

            string[] registrations = manifest.Descendants("XmlName")
                .Select(node => (string)node.Attribute("id") + "|" + (string)node.Attribute("path"))
                .ToArray();
            if (!registrations.Contains("Items|calradia_forge_content_showcase_items", StringComparer.Ordinal) ||
                !registrations.Contains("NPCCharacters|calradia_forge_content_showcase_characters", StringComparer.Ordinal))
                throw new InvalidDataException("The manifest must register the generated Items and NPCCharacters files.");
        }

        private static void VerifyPage(string moduleRoot, string repoRoot)
        {
            XDocument prefab = XDocument.Load(Path.Combine(moduleRoot, "GUI", "Prefabs", "ForgeContentShowcase.xml"));
            string[] bindings = prefab.Descendants("TextWidget").Select(widget => (string)widget.Attribute("Text")).ToArray();
            foreach (string binding in new[] { "@Title", "@Body", "@CloseLabel" })
                if (!bindings.Contains(binding, StringComparer.Ordinal))
                    throw new InvalidDataException("The generated Gauntlet prefab is missing binding " + binding + ".");

            XElement closeButton = prefab.Descendants("ButtonWidget").SingleOrDefault(button => (string)button.Attribute("Id") == "CFCS_Close");
            VerifyCloseButton(closeButton);

            string viewModel = File.ReadAllText(Path.Combine(repoRoot, "examples", "CalradiaForge.ContentShowcase", "Module", "Generated", "ForgeContentShowcasePageViewModel.cs"));
            VerifyPrefabPropertyBindings(prefab, viewModel);
            foreach (string token in new[] { "[ForgeUiPage(", "[DataSourceProperty]", "public string Title", "public string Body", "public string CloseLabel", "ChangesState = false", "ForgeUI.ClosePage()" })
                if (!viewModel.Contains(token, StringComparison.Ordinal))
                    throw new InvalidDataException("Generated read-only ViewModel is missing expected registration or binding " + token + ".");

            string subModule = File.ReadAllText(Path.Combine(repoRoot, "examples", "CalradiaForge.ContentShowcase", "Module", "SubModule.cs"));
            if (!subModule.Contains("RegisterWhenAvailable(RegisterPages)", StringComparison.Ordinal) ||
                !subModule.Contains("UnregisterUiPages(ModuleId)", StringComparison.Ordinal) ||
                subModule.Contains("ForgeUI.OpenPage(", StringComparison.Ordinal))
                throw new InvalidDataException("Showcase page registration must follow module lifecycle without auto-opening the page.");
        }

        private static void VerifyPrefabPropertyBindings(XDocument prefab, string viewModel)
        {
            const string boundPropertyPattern = @"\[DataSourceProperty\]\s*(?:public|protected|internal|private)\s+\S+\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?=\{|=>)";
            var exposedProperties = new HashSet<string>(
                Regex.Matches(viewModel, boundPropertyPattern, RegexOptions.CultureInvariant)
                    .Cast<Match>()
                    .Select(match => match.Groups[1].Value),
                StringComparer.Ordinal);

            var prefabBindings = new HashSet<string>(
                prefab.Descendants().Attributes()
                    .SelectMany(attribute => Regex.Matches(attribute.Value, @"@([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.CultureInvariant)
                        .Cast<Match>()
                        .Select(match => match.Groups[1].Value)),
                StringComparer.Ordinal);

            foreach (string binding in prefabBindings.OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!exposedProperties.Contains(binding))
                    throw new InvalidDataException("Gauntlet prefab binding @" + binding + " has no matching [DataSourceProperty] in the showcase ViewModel.");
            }
        }

        private static void VerifyLocalization(string moduleRoot, string repoRoot)
        {
            string languageRoot = Path.Combine(moduleRoot, "ModuleData", "Languages");
            HashSet<string> english = ReadLocalizationKeys(Path.Combine(languageRoot, "EN", "content_showcase_strings.xml"));
            HashSet<string> spanish = ReadLocalizationKeys(Path.Combine(languageRoot, "SP", "content_showcase_strings.xml"));
            if (!english.SetEquals(spanish) || !english.SetEquals(new[] { "cfcs_title", "cfcs_body", "cfcs_practice_whip", "cfcs_example_recruit", "cfcs_close" }))
                throw new InvalidDataException("English and Spanish showcase resources must contain the same five localized keys.");

            var localizedValues = new List<string>
            {
                File.ReadAllText(Path.Combine(repoRoot, "examples", "CalradiaForge.ContentShowcase", "Module", "Generated", "ForgeContentShowcasePageViewModel.cs"))
            };
            foreach (string relativePath in new[]
            {
                "ModuleData/calradia_forge_content_showcase_items.xml",
                "ModuleData/calradia_forge_content_showcase_characters.xml"
            })
            {
                XDocument content = XDocument.Load(Path.Combine(moduleRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
                localizedValues.AddRange(content.Descendants().Attributes().Select(attribute => attribute.Value));
                localizedValues.AddRange(content.DescendantNodes().OfType<XText>().Select(text => text.Value));
            }
            VerifyLocalizationReferences(CollectLocalizationKeys(localizedValues), english, spanish);

            foreach (string folder in new[] { "EN", "SP" })
            {
                var languageData = XDocument.Load(Path.Combine(languageRoot, folder, "language_data.xml"));
                VerifyLanguageFileRegistration(languageRoot, folder, languageData);
            }
        }

        private static void VerifyLanguageFileRegistration(string languageRoot, string folder, XDocument languageData)
        {
            XElement languageFile = languageData.Root?.Element("LanguageFile");
            string expectedRelativePath = folder + "/content_showcase_strings.xml";
            string referencedRelativePath = (string)languageFile?.Attribute("xml_path");
            if (!string.Equals(referencedRelativePath, expectedRelativePath, StringComparison.Ordinal))
                throw new InvalidDataException(folder + " language_data.xml must reference exactly '" + expectedRelativePath + "'.");

            string languageDirectory = Path.GetFullPath(Path.Combine(languageRoot, folder));
            string languageDirectoryPrefix = languageDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string referencedPath = Path.GetFullPath(Path.Combine(languageRoot, referencedRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            StringComparison pathComparison = Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (!referencedPath.StartsWith(languageDirectoryPrefix, pathComparison) || !File.Exists(referencedPath))
                throw new InvalidDataException(folder + " language_data.xml must reference an existing strings file inside its language directory.");
        }

        private static void VerifyCloseButton(XElement closeButton)
        {
            if (closeButton == null || (string)closeButton.Attribute("Command.Click") != "ExecuteClose")
                throw new InvalidDataException("The prefab close button must bind the non-mutating ExecuteClose command.");

            if (!bool.TryParse((string)closeButton.Attribute("IsFocusable"), out bool isFocusable) || !isFocusable)
                throw new InvalidDataException("The prefab close button must be focusable.");

            if (!closeButton.Descendants("TextWidget").Any(widget => (string)widget.Attribute("Text") == "@CloseLabel"))
                throw new InvalidDataException("The prefab close button must display its localized @CloseLabel binding.");
        }

        private static HashSet<string> CollectLocalizationKeys(IEnumerable<string> values)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string value in values)
            {
                foreach (Match match in Regex.Matches(value ?? string.Empty, @"\{=([A-Za-z0-9_.-]+)\}", RegexOptions.CultureInvariant))
                    keys.Add(match.Groups[1].Value);
            }
            return keys;
        }

        private static void VerifyLocalizationReferences(IEnumerable<string> usedKeys, ISet<string> english, ISet<string> spanish)
        {
            foreach (string key in usedKeys.OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!english.Contains(key))
                    throw new InvalidDataException("Showcase localization key '" + key + "' is missing from the EN catalogue.");
                if (!spanish.Contains(key))
                    throw new InvalidDataException("Showcase localization key '" + key + "' is missing from the SP catalogue.");
            }
        }

        private static void RunContractVerifierRegressionTests()
        {
            XDocument validPrefab = XDocument.Parse("<Prefab><TextWidget Text='@Title' /><ButtonWidget Label='@CloseLabel' /></Prefab>");
            string validViewModel = "[DataSourceProperty] public string Title => \"title\"; [DataSourceProperty] public string CloseLabel => \"close\";";
            VerifyPrefabPropertyBindings(validPrefab, validViewModel);

            XDocument invalidPrefab = XDocument.Parse("<Prefab><TextWidget Text='@Missing' /></Prefab>");
            string unannotatedViewModel = validViewModel + " public string Missing => \"not exposed to Gauntlet\";";
            ExpectContractFailure(
                () => VerifyPrefabPropertyBindings(invalidPrefab, unannotatedViewModel),
                "@Missing");

            XElement validCloseButton = XDocument.Parse(
                "<ButtonWidget Command.Click='ExecuteClose' IsFocusable='true'><Children><TextWidget Text='@CloseLabel' /></Children></ButtonWidget>").Root;
            VerifyCloseButton(validCloseButton);
            XElement unfocusableCloseButton = XDocument.Parse(
                "<ButtonWidget Command.Click='ExecuteClose' IsFocusable='false'><Children><TextWidget Text='@CloseLabel' /></Children></ButtonWidget>").Root;
            ExpectContractFailure(() => VerifyCloseButton(unfocusableCloseButton), "focusable");
            XElement unlabeledCloseButton = XDocument.Parse(
                "<ButtonWidget Command.Click='ExecuteClose' IsFocusable='true'><Children><TextWidget Text='Close' /></Children></ButtonWidget>").Root;
            ExpectContractFailure(() => VerifyCloseButton(unlabeledCloseButton), "@CloseLabel");

            string temporaryRoot = Path.Combine(Path.GetTempPath(), "CalradiaForge.ContentShowcase.Contracts." + Guid.NewGuid().ToString("N"));
            string languageRoot = Path.Combine(temporaryRoot, "Languages");
            try
            {
                string englishDirectory = Path.Combine(languageRoot, "EN");
                Directory.CreateDirectory(englishDirectory);
                File.WriteAllText(Path.Combine(englishDirectory, "content_showcase_strings.xml"), "<base />");
                VerifyLanguageFileRegistration(
                    languageRoot,
                    "EN",
                    XDocument.Parse("<LanguageData><LanguageFile xml_path='EN/content_showcase_strings.xml' /></LanguageData>"));
                ExpectContractFailure(
                    () => VerifyLanguageFileRegistration(languageRoot, "EN", XDocument.Parse("<LanguageData><LanguageFile xml_path='../outside.xml' /></LanguageData>")),
                    "must reference exactly");
                ExpectContractFailure(
                    () => VerifyLanguageFileRegistration(languageRoot, "SP", XDocument.Parse("<LanguageData><LanguageFile xml_path='SP/content_showcase_strings.xml' /></LanguageData>")),
                    "existing strings file");
            }
            finally
            {
                if (Directory.Exists(temporaryRoot))
                    Directory.Delete(temporaryRoot, recursive: true);
            }

            HashSet<string> usedKeys = CollectLocalizationKeys(new[]
            {
                "new TextObject(\"{=vm_title}Title\")",
                "<Item name=\"{=item_name}Practice item\" />",
                "<NPCCharacter name=\"{=troop_name}Recruit\" />"
            });
            var validEnglish = new HashSet<string>(usedKeys, StringComparer.Ordinal);
            var validSpanish = new HashSet<string>(usedKeys, StringComparer.Ordinal);
            VerifyLocalizationReferences(usedKeys, validEnglish, validSpanish);

            var missingEnglish = new HashSet<string>(validEnglish, StringComparer.Ordinal);
            missingEnglish.Remove("item_name");
            ExpectContractFailure(
                () => VerifyLocalizationReferences(usedKeys, missingEnglish, validSpanish),
                "item_name");

            var missingSpanish = new HashSet<string>(validSpanish, StringComparer.Ordinal);
            missingSpanish.Remove("troop_name");
            ExpectContractFailure(
                () => VerifyLocalizationReferences(usedKeys, validEnglish, missingSpanish),
                "troop_name");

            Console.WriteLine("Showcase generator contract regressions passed (prefab bindings, close-button accessibility, and EN/SP localization registrations).");
        }

        private static void ExpectContractFailure(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidDataException error)
            {
                if (!error.Message.Contains(expectedMessage, StringComparison.Ordinal))
                    throw new InvalidDataException("Contract regression produced an unexpected failure: " + error.Message, error);
                return;
            }

            throw new InvalidDataException("Contract regression expected a rejection containing '" + expectedMessage + "'.");
        }

        private static HashSet<string> ReadLocalizationKeys(string path)
        {
            var doc = XDocument.Load(path);
            var keys = doc.Descendants("string").Select(element => (string)element.Attribute("id")).ToArray();
            if (keys.Length != keys.Distinct(StringComparer.Ordinal).Count())
                throw new InvalidDataException("Duplicate localization key in " + path + ".");
            return new HashSet<string>(keys, StringComparer.Ordinal);
        }

        private static void VerifySchema(string gameRoot, string moduleRoot, string schemaName, string xmlRelativePath)
        {
            string schemaPath = Path.Combine(gameRoot, "XmlSchemas", schemaName);
            string xmlPath = Path.Combine(moduleRoot, xmlRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(schemaPath)) throw new FileNotFoundException("Installed game schema is missing.", schemaPath);

            var schemas = new XmlSchemaSet { XmlResolver = null };
            schemas.Add(null, schemaPath);
            schemas.Compile();
            var errors = new List<string>();
            XDocument.Load(xmlPath).Validate(schemas, (_, args) => errors.Add(args.Message));
            if (errors.Count != 0)
                throw new InvalidDataException(schemaName + " rejected " + xmlRelativePath + ": " + string.Join(" | ", errors));
        }

        private static void VerifyNativeMesh(string gameRoot, string moduleRoot)
        {
            string nativePath = Path.Combine(gameRoot, NativeItemPath.Replace('/', Path.DirectorySeparatorChar));
            XDocument nativeItems = XDocument.Load(nativePath);
            XElement nativeItem = nativeItems.Descendants("Item").SingleOrDefault(item => (string)item.Attribute("id") == "horse_whip");
            XElement showcaseItem = XDocument.Load(Path.Combine(moduleRoot, "ModuleData", "calradia_forge_content_showcase_items.xml"))
                .Descendants("Item").Single();
            if (nativeItem == null || (string)nativeItem.Attribute("mesh") != (string)showcaseItem.Attribute("mesh") ||
                (string)nativeItem.Attribute("body_name") != (string)showcaseItem.Attribute("body_name"))
                throw new InvalidDataException("The example item must reuse the installed Native horse_whip mesh and body name.");

            XElement nativeWeapon = nativeItem.Element("ItemComponent")?.Element("Weapon");
            XElement exampleWeapon = showcaseItem.Element("ItemComponent")?.Element("Weapon");
            if (nativeWeapon == null || exampleWeapon == null ||
                (string)nativeWeapon.Attribute("weapon_class") != (string)exampleWeapon.Attribute("weapon_class") ||
                (string)nativeWeapon.Attribute("item_usage") != (string)exampleWeapon.Attribute("item_usage"))
                throw new InvalidDataException("The example weapon must retain the Native mesh's verified weapon class and item usage.");
        }

        private static void VerifyEquipmentReferences(string moduleRoot)
        {
            var declaredItems = new HashSet<string>(
                XDocument.Load(Path.Combine(moduleRoot, "ModuleData", "calradia_forge_content_showcase_items.xml"))
                    .Descendants("Item").Select(item => (string)item.Attribute("id")),
                StringComparer.Ordinal);
            var troop = XDocument.Load(Path.Combine(moduleRoot, "ModuleData", "calradia_forge_content_showcase_characters.xml"))
                .Descendants("NPCCharacter").Single();
            foreach (XElement equipment in troop.Descendants("equipment"))
            {
                string id = (string)equipment.Attribute("id");
                if (!id.StartsWith("Item.", StringComparison.Ordinal) || !declaredItems.Contains(id.Substring("Item.".Length)))
                    throw new InvalidDataException("Showcase troop equipment reference is not declared by the module: " + id + ".");
            }
        }

        private static void RunBatchBenchmark()
        {
            var results = new List<BatchBenchmarkResult>();
            foreach (int itemCount in new[] { 128, 2048, 16384 })
            {
                IReadOnlyList<BenchEntity> entities = Enumerable.Range(0, itemCount)
                    .Select(index => new BenchEntity("showcase_entity_" + index.ToString("D6", CultureInfo.InvariantCulture)))
                    .ToArray();
                Func<BenchEntity, string> idSelector = entity => entity.Id;
                int processed = 0;
                Action<BenchEntity> processor = _ => processed++;
                int repetitions = Math.Max(1, 16384 / itemCount);
                var samples = new double[5];
                long allocatedBytes = 0;
                int expectedPerPass = 0;
                for (int index = 0; index < entities.Count; index++)
                    if (ForgeTimeSlicer.ShouldProcess(entities[index].Id, 7)) expectedPerPass++;

                ForgeTimeSlicer.ProcessBatch(entities, idSelector, processor, 7);
                for (int sample = 0; sample < samples.Length; sample++)
                {
                    processed = 0;
                    var stopwatch = new Stopwatch();
                    long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
                    stopwatch.Start();
                    for (int repetition = 0; repetition < repetitions; repetition++)
                        ForgeTimeSlicer.ProcessBatch(entities, idSelector, processor, 7);
                    stopwatch.Stop();
                    allocatedBytes += GC.GetAllocatedBytesForCurrentThread() - beforeBytes;
                    samples[sample] = stopwatch.Elapsed.TotalMilliseconds;
                    if (processed != expectedPerPass * repetitions)
                        throw new InvalidOperationException("Batch benchmark produced an inconsistent processed count.");
                }

                double[] ordered = samples.OrderBy(value => value).ToArray();
                results.Add(new BatchBenchmarkResult
                {
                    Operation = "ForgeTimeSlicer.ProcessBatch<IReadOnlyList>",
                    EntityCount = itemCount,
                    RepetitionsPerSample = repetitions,
                    SelectedPerPass = expectedPerPass,
                    SamplesMilliseconds = samples,
                    MedianMilliseconds = ordered[ordered.Length / 2],
                    AllocatedBytesAcrossFiveSamples = allocatedBytes
                });
            }

            Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine("Synthetic portable harness measurements only; not a Bannerlord tick or in-game latency result.");
        }

        private static string GetOption(string[] args, string name)
        {
            for (int index = 0; index + 1 < args.Length; index++)
                if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase)) return args[index + 1];
            return null;
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "CalradiaForge.sln"))) return directory.FullName;
                directory = directory.Parent;
            }
            throw new DirectoryNotFoundException("Run the content showcase generator from the repository tree.");
        }

        private static string ComputeSha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private sealed class BenchEntity
        {
            public BenchEntity(string id) { Id = id; }
            public string Id { get; }
        }

        private sealed class BatchBenchmarkResult
        {
            public string Operation { get; set; }
            public int EntityCount { get; set; }
            public int RepetitionsPerSample { get; set; }
            public int SelectedPerPass { get; set; }
            public double[] SamplesMilliseconds { get; set; }
            public double MedianMilliseconds { get; set; }
            public long AllocatedBytesAcrossFiveSamples { get; set; }
        }
    }
}
