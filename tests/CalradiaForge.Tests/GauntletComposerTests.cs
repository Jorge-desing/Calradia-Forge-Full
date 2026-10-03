using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using CalradiaForge.Mod;

namespace CalradiaForge.Tests
{
    internal static class GauntletComposerTests
    {
        internal static void Run(Action<string, Action> test)
        {
            test("Gauntlet Composer supports nine bounded component kinds and stable IDs", TestKindsLimitsAndStableIds);
            test("Gauntlet Composer generates escaped data-bound files with matching bindings", TestGeneratedPackageContractsAndEscaping);
            test("Gauntlet Composer draft persistence handles valid, damaged, future, and oversized files", TestDraftPersistenceStatuses);
            test("Gauntlet Composer saves drafts atomically and preserves full package copy", TestAtomicSaveAndFullCopyContract);
        }

        private static void TestKindsLimitsAndStableIds()
        {
            Equal(9, GauntletComposerKinds.All.Length);
            Equal(9, GauntletComposerKinds.All.Distinct(StringComparer.Ordinal).Count());
            True(GauntletComposerKinds.TryParseOptions("First | ", out var partial, out var complete, out _));
            Equal(1, partial.Count);
            True(!complete);
            True(GauntletComposerKinds.TryParseOptions("First | Second", out var parsedOptions, out complete, out _));
            Equal(2, parsedOptions.Count);
            True(complete);
            True(!GauntletComposerKinds.TryParseOptions(string.Join(" | ", Enumerable.Range(1, 9)), out _, out _, out _));
            var blocks = GauntletComposerKinds.All.Select(GauntletComposerKinds.Create).ToList();
            True(blocks.All(block => GauntletComposerKinds.IsValidId(block.Id)));
            Equal(9, blocks.Select(block => block.Id).Distinct(StringComparer.Ordinal).Count());

            var originalIds = blocks.ToDictionary(block => block.Kind, block => block.Id, StringComparer.Ordinal);
            blocks.Reverse();
            var reordered = new GauntletComposerDraft { Title = new string('T', 64), Components = blocks };
            True(GauntletComposerKinds.TryNormalize(reordered, out var normalized, out _));
            Equal(48, normalized.Title.Length);
            foreach (var block in normalized.Components)
                Equal(originalIds[block.Kind], block.Id);

            normalized.Components[0].Label = new string('L', 160);
            normalized.Components[0].Text = new string('x', 160);
            normalized.Components[0].Progress = 140;
            True(GauntletComposerKinds.TryNormalize(normalized, out var limited, out _));
            Equal(128, limited.Components[0].Label.Length);
            Equal(128, limited.Components[0].Text.Length);
            Equal(100, limited.Components[0].Progress);

            var twelve = new GauntletComposerDraft
            {
                Components = Enumerable.Range(0, GauntletComposerKinds.MaximumComponents)
                    .Select(_ => GauntletComposerKinds.Create("text")).ToList()
            };
            True(GauntletComposerKinds.TryNormalize(twelve, out _, out _));
            twelve.Components.Add(GauntletComposerKinds.Create("text"));
            True(!GauntletComposerKinds.TryNormalize(twelve, out _, out _));

            foreach (var kind in new[] { "list", "selector" })
            {
                var block = GauntletComposerKinds.Create(kind);
                True(GauntletComposerKinds.TryNormalize(new GauntletComposerDraft { Components = { block } }, out _, out _));
                block.Options = Enumerable.Range(1, GauntletComposerKinds.MaximumOptions).Select(index => "Option " + index).ToList();
                True(GauntletComposerKinds.TryNormalize(new GauntletComposerDraft { Components = { block } }, out _, out _));
                block.Options.Add("Option 9");
                True(!GauntletComposerKinds.TryNormalize(new GauntletComposerDraft { Components = { block } }, out _, out _));
            }

            var invalid = GauntletComposerKinds.Create("heading");
            invalid.Kind = "campaign-action";
            True(!GauntletComposerKinds.TryNormalize(new GauntletComposerDraft { Components = { invalid } }, out _, out _));
        }

        private static void TestGeneratedPackageContractsAndEscaping()
        {
            var escaped = GauntletComposerKinds.Create("text");
            escaped.Label = "A&B <T> \"Ω\"";
            escaped.Text = "line one\nline two \\ end";
            var field = GauntletComposerKinds.Create("field");
            field.Label = "Display name";
            var draft = new GauntletComposerDraft
            {
                Title = "A&B <Page> \"Ω\"",
                Components = { escaped, field }
            };
            True(GauntletComposerGenerator.TryGenerate(draft, out var package, out var errors));
            Equal(0, errors.Count);
            XDocument.Parse(package.PrefabXml);
            XDocument.Parse(package.LocalizationXml);
            True(package.PrefabXml.Contains("DataSource=\"{Components}\""));
            True(package.PrefabXml.Contains("DataSource=\"{Options}\""));
            True(package.PrefabXml.Contains("VerticalScrollbar=\"..\\PageScrollBar\""));
            var generatedPrefab = XDocument.Parse(package.PrefabXml);
            var generatedScrollPanel = generatedPrefab.Descendants("ScrollablePanel").Single();
            var generatedScrollBar = generatedPrefab.Descendants("ScrollbarWidget").Single();
            True(ReferenceEquals(generatedScrollPanel.Parent, generatedScrollBar.Parent));
            True(generatedPrefab.Descendants("TextWidget").Any(widget => (string)widget.Attribute("IsVisible") == "@IsText" && (string)widget.Attribute("Text") == "@Text"));
            True(generatedPrefab.Descendants("TextWidget").Any(widget => (string)widget.Attribute("Text") == "@SampleButtonLabel"));
            var fieldRow = generatedPrefab.Descendants("ListPanel").Single(widget => (string)widget.Attribute("IsVisible") == "@IsField");
            True(fieldRow.Descendants("TextWidget").Any(widget => (string)widget.Attribute("Text") == "@Label"));
            var editableField = fieldRow.Descendants("EditableTextWidget").Single();
            Equal("@Text", (string)editableField.Attribute("Text"));
            Equal("true", (string)editableField.Attribute("UpdateTextOnTyping"));
            True(package.ViewModelCode.Contains("MBBindingList<GauntletPageComponentViewModel> Components"));
            True(package.ViewModelCode.Contains("MBBindingList<GauntletPageOptionViewModel> Options"));
            True(package.ViewModelCode.Contains("SampleButtonLabel"));
            True(package.LocalizationXml.Contains("A&amp;B &lt;T&gt; &quot;Ω&quot;"));
            True(package.LocalizationXml.Contains("line one&#10;line two"));
            True(package.LocalizationXml.Contains("forge_composer_" + escaped.Id + "_text"));
            True(package.ViewModelCode.Contains("A&B <T> \\\"Ω\\\""));
            True(package.ViewModelCode.Contains("line one\\nline two \\\\ end"));
            True(package.FullText.Contains("=== 1. GUI/Prefabs/GauntletPage.xml ==="));
            True(package.FullText.Contains("=== 2. GauntletPageViewModel.cs ==="));
            True(package.FullText.Contains("=== 3. Languages/EN/forge_strings.xml entries ==="));
            True(package.FullText.Contains("=== 4. SubModule and screen integration ==="));
            True(package.FullText.Contains("Generated text only. No module files were written."));

            Equal(0, GauntletComposerGenerator.ValidateContract(package.PrefabXml, package.ViewModelCode, package.LocalizationXml).Count);
            var badList = package.PrefabXml.Replace("DataSource=\"{Options}\"", "DataSource=\"{MissingOptions}\"");
            True(GauntletComposerGenerator.ValidateContract(badList, package.ViewModelCode, package.LocalizationXml).Count > 0);
            var badCommand = package.ViewModelCode.Replace("public void ExecuteSampleAction()", "public void ExecuteRenamedAction()");
            True(GauntletComposerGenerator.ValidateContract(package.PrefabXml, badCommand, package.LocalizationXml).Count > 0);
            var missingLocalization = package.LocalizationXml.Replace("forge_composer_" + escaped.Id, "forge_removed_" + escaped.Id);
            True(GauntletComposerGenerator.ValidateContract(package.PrefabXml, package.ViewModelCode, missingLocalization).Count > 0);
        }

        private static void TestDraftPersistenceStatuses()
        {
            string directory = Path.Combine(Path.GetTempPath(), "CalradiaForgeComposerTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string path = Path.Combine(directory, "gauntlet-composer.json");
                Equal(GauntletComposerLoadStatus.Missing, GauntletComposerPersistence.Load(path).Status);

                var draft = new GauntletComposerDraft
                {
                    Title = "Persisted Ω",
                    Components = { GauntletComposerKinds.Create("selector") }
                };
                True(GauntletComposerPersistence.TrySave(path, draft, out _));
                var loaded = GauntletComposerPersistence.Load(path);
                Equal(GauntletComposerLoadStatus.Loaded, loaded.Status);
                Equal("Persisted Ω", loaded.Draft.Title);
                Equal(draft.Components[0].Id, loaded.Draft.Components[0].Id);

                File.WriteAllText(path, "{");
                Equal(GauntletComposerLoadStatus.Invalid, GauntletComposerPersistence.Load(path).Status);
                File.WriteAllText(path, "{\"version\":99,\"title\":\"future\",\"components\":[]}");
                Equal(GauntletComposerLoadStatus.UnknownVersion, GauntletComposerPersistence.Load(path).Status);
                File.WriteAllBytes(path, new byte[GauntletComposerPersistence.MaximumFileBytes + 1]);
                Equal(GauntletComposerLoadStatus.TooLarge, GauntletComposerPersistence.Load(path).Status);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static void TestAtomicSaveAndFullCopyContract()
        {
            string directory = Path.Combine(Path.GetTempPath(), "CalradiaForgeComposerTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string path = Path.Combine(directory, "gauntlet-composer.json");
                var draft = new GauntletComposerDraft { Title = "First" };
                True(GauntletComposerPersistence.TrySave(path, draft, out _));
                draft.Title = "Second";
                True(GauntletComposerPersistence.TrySave(path, draft, out _));
                Equal("Second", GauntletComposerPersistence.Load(path).Draft.Title);
                Equal(0, Directory.GetFiles(directory, "*.tmp").Length);
            }
            finally
            {
                Directory.Delete(directory, true);
            }

            string panelPath = Path.GetFullPath("src/CalradiaForge.Mod/PanelViewModel.cs");
            string source = File.ReadAllText(panelPath);
            int start = source.IndexOf("public void ExecuteComposerCopyPackage()", StringComparison.Ordinal);
            True(start >= 0);
            int copyEnd = source.IndexOf("public void ExecuteComposerSampleButton()", start, StringComparison.Ordinal);
            True(copyEnd > start);
            string copyMethod = source.Substring(start, copyEnd - start);
            True(copyMethod.Contains("ExecuteClipboard();"));
            True(copyMethod.Contains("full"));
            True(source.Contains("Send(\"clipboard\", full)"));
            True(!copyMethod.Contains("ForgeCopy"));
            int clipboardGuard = source.IndexOf("if (!string.Equals(action, \"clipboard\", StringComparison.Ordinal)", StringComparison.Ordinal);
            True(clipboardGuard >= 0);
            int commandRecord = source.IndexOf("RecordCommand(effectiveArg);", clipboardGuard, StringComparison.Ordinal);
            True(commandRecord > clipboardGuard);
        }

        private static void True(bool value, [CallerLineNumber] int line = 0)
        {
            if (!value) throw new Exception("Gauntlet Composer assertion failed at line " + line + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", found " + actual + ".");
        }
    }
}
