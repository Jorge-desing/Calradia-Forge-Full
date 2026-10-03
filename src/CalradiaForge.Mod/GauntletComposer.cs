using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace CalradiaForge.Mod
{
    internal sealed class GauntletComposerDraft
    {
        [JsonProperty("version")]
        public int Version { get; set; } = GauntletComposerPersistence.CurrentVersion;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("components")]
        public List<GauntletComposerBlock> Components { get; set; } = new List<GauntletComposerBlock>();
    }

    internal sealed class GauntletComposerBlock
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("options")]
        public List<string> Options { get; set; } = new List<string>();

        [JsonProperty("progress")]
        public int Progress { get; set; } = 60;

        [JsonProperty("isOn")]
        public bool IsOn { get; set; }
    }

    internal enum GauntletComposerLoadStatus
    {
        Missing,
        Loaded,
        Invalid,
        TooLarge,
        UnknownVersion,
        Unavailable
    }

    internal sealed class GauntletComposerLoadResult
    {
        internal GauntletComposerLoadStatus Status { get; set; }
        internal GauntletComposerDraft Draft { get; set; }
    }

    internal static class GauntletComposerKinds
    {
        internal const int MaximumComponents = 12;
        internal const int MaximumOptions = 8;
        internal const int MaximumTitleLength = 48;
        internal const int MaximumTextLength = 128;
        internal const int MaximumOptionsInputLength = MaximumOptions * (MaximumTextLength + 3);

        internal static readonly string[] All =
        {
            "heading", "text", "field", "button", "metric", "list", "toggle", "progress", "selector"
        };

        internal static bool IsKnown(string value)
        {
            return All.Contains(value, StringComparer.Ordinal);
        }

        internal static bool TryParseOptions(string input, out List<string> options, out bool isComplete, out string error)
        {
            options = new List<string>();
            isComplete = true;
            error = null;
            input = input ?? string.Empty;
            if (input.Length > MaximumOptionsInputLength)
            {
                input = input.Substring(0, MaximumOptionsInputLength);
                isComplete = false;
            }

            string[] parts = input.Split(new[] { '|' }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                string value = parts[i].Trim();
                if (value.Length == 0)
                {
                    isComplete = false;
                    continue;
                }
                if (value.Length > MaximumTextLength)
                {
                    isComplete = false;
                    value = Limit(value, MaximumTextLength);
                }
                options.Add(value);
            }

            if (options.Count < 1 || options.Count > MaximumOptions)
                isComplete = false;
            if (options.Count > MaximumOptions)
            {
                error = "Lists and selectors require between 1 and 8 non-empty options separated by |.";
                return false;
            }
            return true;
        }

        internal static GauntletComposerBlock Create(string kind)
        {
            if (!IsKnown(kind))
                throw new ArgumentException("Unknown Gauntlet component type.", nameof(kind));

            string label;
            switch (kind)
            {
                case "heading": label = "Section heading"; break;
                case "text": label = "Description"; break;
                case "field": label = "Editable field"; break;
                case "button": label = "Action"; break;
                case "metric": label = "Status"; break;
                case "list": label = "Entries"; break;
                case "toggle": label = "Enabled"; break;
                case "progress": label = "Progress"; break;
                default: label = "Selection"; break;
            }

            var block = new GauntletComposerBlock
            {
                Id = "block_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                Kind = kind,
                Label = label,
                Text = kind == "metric" ? "Ready" : kind == "field" ? "Sample value" : "",
                Progress = 60,
                IsOn = false
            };
            if (kind == "list" || kind == "selector")
                block.Options.Add(kind == "list" ? "First item" : "Option 1");
            return block;
        }

        internal static bool TryNormalize(GauntletComposerDraft input, out GauntletComposerDraft normalized, out string error)
        {
            normalized = null;
            error = null;
            if (input == null)
            {
                error = "The draft is empty.";
                return false;
            }
            if (input.Version != GauntletComposerPersistence.CurrentVersion)
            {
                error = "The draft schema version is not supported.";
                return false;
            }

            var blocks = input.Components ?? new List<GauntletComposerBlock>();
            if (blocks.Count > MaximumComponents)
            {
                error = "A draft can contain at most 12 components.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var copy = new GauntletComposerDraft
            {
                Version = GauntletComposerPersistence.CurrentVersion,
                Title = Limit(input.Title, MaximumTitleLength),
                Components = new List<GauntletComposerBlock>(blocks.Count)
            };

            foreach (var source in blocks)
            {
                if (source == null || !IsKnown(source.Kind) || !IsValidId(source.Id) || !ids.Add(source.Id))
                {
                    error = "The draft contains an invalid component type or ID.";
                    return false;
                }

                var options = source.Options ?? new List<string>();
                bool acceptsOptions = source.Kind == "list" || source.Kind == "selector";
                if (acceptsOptions && (options.Count < 1 || options.Count > MaximumOptions || options.Any(string.IsNullOrWhiteSpace)))
                {
                    error = "Lists and selectors require between 1 and 8 options.";
                    return false;
                }
                if (!acceptsOptions && options.Count > 0)
                {
                    error = "Only lists and selectors can contain options.";
                    return false;
                }

                if (options.Count > MaximumOptions)
                {
                    error = "A component can contain at most 8 options.";
                    return false;
                }

                copy.Components.Add(new GauntletComposerBlock
                {
                    Id = source.Id,
                    Kind = source.Kind,
                    Label = Limit(source.Label, MaximumTextLength),
                    Text = Limit(source.Text, MaximumTextLength),
                    Options = options.Select(option => Limit(option, MaximumTextLength)).ToList(),
                    Progress = Math.Max(0, Math.Min(100, source.Progress)),
                    IsOn = source.IsOn
                });
            }

            normalized = copy;
            return true;
        }

        internal static string Limit(string value, int maximum)
        {
            value = value ?? string.Empty;
            return value.Length <= maximum ? value : value.Substring(0, maximum);
        }

        internal static bool IsValidId(string id)
        {
            if (id == null || id.Length != 18 || !id.StartsWith("block_", StringComparison.Ordinal))
                return false;
            for (int i = 6; i < id.Length; i++)
                if (!((id[i] >= '0' && id[i] <= '9') || (id[i] >= 'a' && id[i] <= 'f')))
                    return false;
            return true;
        }
    }

    internal static class GauntletComposerPersistence
    {
        internal const int CurrentVersion = 1;
        internal const int MaximumFileBytes = 64 * 1024;
        private static readonly object SaveGate = new object();

        internal static GauntletComposerLoadResult Load(string path)
        {
            var result = new GauntletComposerLoadResult
            {
                Status = GauntletComposerLoadStatus.Missing,
                Draft = new GauntletComposerDraft()
            };
            if (string.IsNullOrWhiteSpace(path))
            {
                result.Status = GauntletComposerLoadStatus.Unavailable;
                return result;
            }

            try
            {
                if (!File.Exists(path))
                    return result;

                byte[] bytes;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length > MaximumFileBytes)
                    {
                        result.Status = GauntletComposerLoadStatus.TooLarge;
                        return result;
                    }
                    bytes = new byte[(int)stream.Length];
                    int offset = 0;
                    while (offset < bytes.Length)
                    {
                        int read = stream.Read(bytes, offset, bytes.Length - offset);
                        if (read <= 0) throw new EndOfStreamException();
                        offset += read;
                    }
                }

                var json = new UTF8Encoding(false, true).GetString(bytes);
                var loaded = JsonConvert.DeserializeObject<GauntletComposerDraft>(json);
                if (loaded == null)
                {
                    result.Status = GauntletComposerLoadStatus.Invalid;
                    return result;
                }
                if (loaded.Version != CurrentVersion)
                {
                    result.Status = GauntletComposerLoadStatus.UnknownVersion;
                    return result;
                }
                if (!GauntletComposerKinds.TryNormalize(loaded, out var normalized, out _))
                {
                    result.Status = GauntletComposerLoadStatus.Invalid;
                    return result;
                }

                result.Status = GauntletComposerLoadStatus.Loaded;
                result.Draft = normalized;
                return result;
            }
            catch (DecoderFallbackException)
            {
                result.Status = GauntletComposerLoadStatus.Invalid;
                return result;
            }
            catch (JsonException)
            {
                result.Status = GauntletComposerLoadStatus.Invalid;
                return result;
            }
            catch
            {
                result.Status = GauntletComposerLoadStatus.Unavailable;
                return result;
            }
        }

        internal static bool TrySave(string path, GauntletComposerDraft draft, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "The draft path is unavailable.";
                return false;
            }
            if (!GauntletComposerKinds.TryNormalize(draft, out var normalized, out error))
                return false;

            var bytes = new UTF8Encoding(false).GetBytes(JsonConvert.SerializeObject(normalized, Formatting.None));
            if (bytes.Length > MaximumFileBytes)
            {
                error = "The draft exceeds the 64 KiB file limit.";
                return false;
            }

            try
            {
                lock (SaveGate)
                    WriteAtomically(path, bytes);
                return true;
            }
            catch
            {
                error = "The draft could not be saved.";
                return false;
            }
        }

        private static void WriteAtomically(string path, byte[] bytes)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
                throw new IOException("The draft directory is unavailable.");
            Directory.CreateDirectory(directory);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path))
                    File.Replace(temporary, path, null);
                else
                    File.Move(temporary, path);
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch { }
            }
        }
    }

    internal sealed class GauntletComposerPackage
    {
        internal string PrefabXml { get; set; }
        internal string ViewModelCode { get; set; }
        internal string LocalizationXml { get; set; }
        internal string Integration { get; set; }
        internal string FullText { get; set; }
    }

    internal static class GauntletComposerGenerator
    {
        private static readonly Regex BindingPattern = new Regex("@([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
        private static readonly Regex CommandPattern = new Regex("Command\\.Click=\"([A-Za-z_][A-Za-z0-9_]*)\"", RegexOptions.Compiled);
        private static readonly Regex PropertyPattern = new Regex("\\[DataSourceProperty\\]\\s*public\\s+[\\w<>?,\\[\\]]+\\s+(?<name>[A-Za-z_]\\w*)", RegexOptions.Compiled);
        private static readonly Regex CommandMethodPattern = new Regex("public\\s+void\\s+(?<name>Execute[A-Za-z0-9_]*)\\s*\\(", RegexOptions.Compiled);
        private static readonly Regex ListPattern = new Regex("MBBindingList\\s*<\\s*(?<type>[A-Za-z_]\\w*)\\s*>\\s+(?<name>[A-Za-z_]\\w*)", RegexOptions.Compiled);
        private static readonly Regex LocalizedIdPattern = new Regex("Localized\\(\"(?<id>[A-Za-z0-9_]+)\"", RegexOptions.Compiled);

        internal static bool TryGenerate(GauntletComposerDraft draft, out GauntletComposerPackage package, out List<string> errors)
        {
            package = null;
            errors = new List<string>();
            if (!GauntletComposerKinds.TryNormalize(draft, out var normalized, out var normalizeError))
            {
                errors.Add(normalizeError);
                return false;
            }
            if (normalized.Components.Count == 0)
            {
                errors.Add("Add at least one component before generating the package.");
                return false;
            }

            var localization = new List<KeyValuePair<string, string>>();
            string title = string.IsNullOrWhiteSpace(normalized.Title) ? "New Gauntlet Page" : normalized.Title;
            localization.Add(new KeyValuePair<string, string>("forge_composer_page_title", title));
            foreach (var block in normalized.Components)
            {
                localization.Add(new KeyValuePair<string, string>(LocalizationId(block.Id), block.Label));
                if (!string.IsNullOrEmpty(block.Text))
                    localization.Add(new KeyValuePair<string, string>(TextLocalizationId(block.Id), block.Text));
                for (int i = 0; i < block.Options.Count; i++)
                    localization.Add(new KeyValuePair<string, string>(OptionLocalizationId(block.Id, i), block.Options[i]));
            }

            var prefab = BuildPrefab();
            var viewModel = BuildViewModel(normalized, title);
            var localizationXml = BuildLocalization(localization);
            var integration = BuildIntegration();
            errors.AddRange(ValidateContract(prefab, viewModel, localizationXml));
            if (errors.Count > 0)
                return false;

            var full = new StringBuilder();
            full.AppendLine("GAUNTLET PAGE COMPOSER PACKAGE");
            full.AppendLine("Generated text only. No module files were written.");
            full.AppendLine();
            full.AppendLine("=== 1. GUI/Prefabs/GauntletPage.xml ===");
            full.AppendLine(prefab);
            full.AppendLine("=== 2. GauntletPageViewModel.cs ===");
            full.AppendLine(viewModel);
            full.AppendLine("=== 3. Languages/EN/forge_strings.xml entries ===");
            full.AppendLine(localizationXml);
            full.AppendLine("=== 4. SubModule and screen integration ===");
            full.AppendLine(integration);

            package = new GauntletComposerPackage
            {
                PrefabXml = prefab,
                ViewModelCode = viewModel,
                LocalizationXml = localizationXml,
                Integration = integration,
                FullText = full.ToString()
            };
            return true;
        }

        internal static List<string> ValidateContract(string prefabXml, string viewModelCode, string localizationXml)
        {
            return ValidateContractCore(prefabXml, viewModelCode, localizationXml);
        }

        private static string BuildPrefab()
        {
            return @"<Prefab>
  <Window>
    <Widget WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" Sprite=""BlankWhiteSquare_9"">
      <Children>
        <TextWidget Brush=""GameTip.Title.Text"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""Fixed"" SuggestedHeight=""38"" Text=""@PageTitle"" />
        <ScrollablePanel WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginTop=""48"" ClipRect=""PageClip"" InnerPanel=""PageClip\Components"" VerticalScrollbar=""..\PageScrollBar"">
          <Children>
            <Widget Id=""PageClip"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" ClipContents=""true""><Children>
              <ListPanel Id=""Components"" DataSource=""{Components}"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""CoverChildren"" StackLayout.LayoutMethod=""VerticalTopToBottom""><ItemTemplate>
                <ListPanel WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""CoverChildren"" StackLayout.LayoutMethod=""VerticalTopToBottom""><Children>
                  <TextWidget Brush=""GameTip.Title.Text"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""Fixed"" SuggestedHeight=""38"" Text=""@Label"" IsVisible=""@IsHeading"" />
                  <TextWidget Brush=""GameTip.Text"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""CoverChildren"" Text=""@Text"" IsVisible=""@IsText"" />
                  <ListPanel WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""Fixed"" SuggestedHeight=""66"" StackLayout.LayoutMethod=""VerticalTopToBottom"" IsVisible=""@IsField""><Children><TextWidget Brush=""GameTip.Text"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""Fixed"" SuggestedHeight=""22"" Text=""@Label"" /><EditableTextWidget WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""Fixed"" SuggestedHeight=""40"" Text=""@Text"" UpdateTextOnTyping=""true"" /></Children></ListPanel>
                  <ButtonWidget WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""220"" SuggestedHeight=""40"" Command.Click=""ExecuteSampleAction"" IsVisible=""@IsButton""><Children><TextWidget Brush=""GameTip.Text"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" Text=""@SampleButtonLabel"" /></Children></ButtonWidget>
                  <TextWidget Brush=""GameTip.Text"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""Fixed"" SuggestedHeight=""34"" Text=""@ValueText"" IsVisible=""@IsMetric"" />
                  <ListPanel DataSource=""{Options}"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""CoverChildren"" StackLayout.LayoutMethod=""VerticalTopToBottom"" IsVisible=""@IsList""><ItemTemplate><TextWidget Brush=""GameTip.Text"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""Fixed"" SuggestedHeight=""32"" Text=""@Label"" /></ItemTemplate></ListPanel>
                  <ButtonWidget WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""220"" SuggestedHeight=""40"" ButtonType=""Toggle"" IsSelected=""@IsOn"" IsVisible=""@IsToggle""><Children><TextWidget Brush=""GameTip.Text"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" Text=""@Label"" /></Children></ButtonWidget>
                  <FillBarWidget WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""321"" SuggestedHeight=""27"" ContainerWidget=""ContainerWidget"" FillWidget=""FillBarParent\FillWidget"" MaxAmountAsFloat=""1"" InitialAmountAsFloat=""@ProgressAmount"" IsVisible=""@IsProgress""><Children><Widget Id=""FillBarParent"" WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""300"" SuggestedHeight=""14""><Children><Widget Id=""FillWidget"" WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""300"" SuggestedHeight=""14"" Sprite=""BlankWhiteSquare_9"" /></Children></Widget><Widget Id=""ContainerWidget"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" Sprite=""BlankWhiteSquare_9"" /></Children></FillBarWidget>
                  <SelectorWidget WidthSizePolicy=""CoverChildren"" HeightSizePolicy=""CoverChildren"" CurrentSelectedIndex=""@SelectedIndex"" Container=""OptionGrid"" IsVisible=""@IsSelector""><Children><NavigatableGridWidget Id=""OptionGrid"" DataSource=""{Options}"" WidthSizePolicy=""CoverChildren"" HeightSizePolicy=""CoverChildren"" ColumnCount=""4"" DefaultCellHeight=""36"" DefaultCellWidth=""120""><ItemTemplate><ButtonWidget WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""118"" SuggestedHeight=""34"" ButtonType=""Radio"" IsSelected=""@IsSelected"" Command.Click=""ExecuteSelect""><Children><TextWidget Brush=""GameTip.Text"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" Text=""@Label"" /></Children></ButtonWidget></ItemTemplate></NavigatableGridWidget></Children></SelectorWidget>
                </Children></ListPanel>
              </ItemTemplate></ListPanel>
            </Children></Widget>
          </Children>
        </ScrollablePanel>
        <ScrollbarWidget Handle=""PageScrollHandle"" Id=""PageScrollBar"" WidthSizePolicy=""Fixed"" SuggestedWidth=""10"" HeightSizePolicy=""StretchToParent"" HorizontalAlignment=""Right""><Children><ImageWidget Brush=""FaceGen.Scrollbar.Handle"" HeightSizePolicy=""Fixed"" HorizontalAlignment=""Center"" Id=""PageScrollHandle"" SuggestedHeight=""10"" SuggestedWidth=""4"" WidthSizePolicy=""Fixed"" /></Children></ScrollbarWidget>
      </Children>
    </Widget>
  </Window>
</Prefab>";
        }

        private static string BuildViewModel(GauntletComposerDraft draft, string title)
        {
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            builder.AppendLine("using TaleWorlds.Library;");
            builder.AppendLine("using TaleWorlds.Localization;");
            builder.AppendLine();
            builder.AppendLine("namespace MyMod.Gauntlet");
            builder.AppendLine("{");
            builder.AppendLine("    public sealed class GauntletPageViewModel : ViewModel");
            builder.AppendLine("    {");
            builder.AppendLine("        private string _pageTitle;");
            builder.AppendLine("        private readonly MBBindingList<GauntletPageComponentViewModel> _components = new MBBindingList<GauntletPageComponentViewModel>();");
            builder.AppendLine("        [DataSourceProperty] public string PageTitle { get => _pageTitle; set { _pageTitle = value ?? string.Empty; OnPropertyChangedWithValue(_pageTitle, nameof(PageTitle)); } }");
            builder.AppendLine("        [DataSourceProperty] public MBBindingList<GauntletPageComponentViewModel> Components => _components;");
            builder.AppendLine("        public GauntletPageViewModel()");
            builder.AppendLine("        {");
            builder.AppendLine("            PageTitle = Localized(\"forge_composer_page_title\", " + CSharpLiteral(title) + ");");
            foreach (var block in draft.Components)
            {
                string localizedLabel = "Localized(" + CSharpLiteral(LocalizationId(block.Id)) + ", " + CSharpLiteral(block.Label) + ")";
                string localizedText = string.IsNullOrEmpty(block.Text)
                    ? "string.Empty"
                    : "Localized(" + CSharpLiteral(TextLocalizationId(block.Id)) + ", " + CSharpLiteral(block.Text) + ")";
                builder.AppendLine("            var component_" + block.Id.Substring(6) + " = new GauntletPageComponentViewModel(" +
                    CSharpLiteral(block.Id) + ", " + CSharpLiteral(block.Kind) + ", " + localizedLabel + ", " + localizedText + ", " +
                    block.Progress.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + (block.IsOn ? "true" : "false") + ");");
                for (int i = 0; i < block.Options.Count; i++)
                {
                    builder.AppendLine("            component_" + block.Id.Substring(6) + ".Options.Add(new GauntletPageOptionViewModel(Localized(" +
                        CSharpLiteral(OptionLocalizationId(block.Id, i)) + ", " + CSharpLiteral(block.Options[i]) + "), component_" + block.Id.Substring(6) + "));" );
                }
                builder.AppendLine("            _components.Add(component_" + block.Id.Substring(6) + ");");
            }
            builder.AppendLine("        }");
            builder.AppendLine("        private static string Localized(string id, string fallback) => new TextObject(\"{=\" + id + \"}\" + fallback).ToString();");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("    public sealed class GauntletPageComponentViewModel : ViewModel");
            builder.AppendLine("    {");
            builder.AppendLine("        private readonly string _kind;");
            builder.AppendLine("        private string _label, _text, _valueText;");
            builder.AppendLine("        private int _progressValue, _selectedIndex, _sampleActionCount;");
            builder.AppendLine("        private bool _isOn;");
            builder.AppendLine("        private readonly MBBindingList<GauntletPageOptionViewModel> _options = new MBBindingList<GauntletPageOptionViewModel>();");
            builder.AppendLine("        public GauntletPageComponentViewModel(string id, string kind, string label, string text, int progress, bool isOn) { Id = id; _kind = kind; _label = label; _text = text; _progressValue = progress; _isOn = isOn; _valueText = text; }");
            builder.AppendLine("        public string Id { get; }");
            builder.AppendLine("        [DataSourceProperty] public string Label { get => _label; set { _label = value ?? string.Empty; OnPropertyChangedWithValue(_label, nameof(Label)); OnPropertyChanged(nameof(SampleButtonLabel)); } }");
            builder.AppendLine("        [DataSourceProperty] public string SampleButtonLabel => _sampleActionCount == 0 ? Label : Label + \" (\" + _sampleActionCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + \")\";");
            builder.AppendLine("        [DataSourceProperty] public string Text { get => _text; set { _text = value ?? string.Empty; OnPropertyChangedWithValue(_text, nameof(Text)); } }");
            builder.AppendLine("        [DataSourceProperty] public string ValueText { get => _valueText; set { _valueText = value ?? string.Empty; OnPropertyChangedWithValue(_valueText, nameof(ValueText)); } }");
            builder.AppendLine("        [DataSourceProperty] public int ProgressValue { get => _progressValue; set { _progressValue = Math.Max(0, Math.Min(100, value)); OnPropertyChangedWithValue(_progressValue, nameof(ProgressValue)); OnPropertyChanged(nameof(ProgressAmount)); } }");
            builder.AppendLine("        [DataSourceProperty] public float ProgressAmount => ProgressValue / 100f;");
            builder.AppendLine("        [DataSourceProperty] public bool IsOn { get => _isOn; set { _isOn = value; OnPropertyChangedWithValue(_isOn, nameof(IsOn)); } }");
            builder.AppendLine("        [DataSourceProperty] public int SelectedIndex { get => _selectedIndex; set { _selectedIndex = Math.Max(0, Math.Min(Options.Count - 1, value)); OnPropertyChangedWithValue(_selectedIndex, nameof(SelectedIndex)); RefreshOptions(); } }");
            builder.AppendLine("        [DataSourceProperty] public string SelectedOptionLabel => Options.Count == 0 ? string.Empty : Options[SelectedIndex].Label;");
            foreach (string kind in GauntletComposerKinds.All)
                builder.AppendLine("        [DataSourceProperty] public bool Is" + char.ToUpperInvariant(kind[0]) + kind.Substring(1) + " => string.Equals(_kind, " + CSharpLiteral(kind) + ", StringComparison.Ordinal);");
            builder.AppendLine("        [DataSourceProperty] public MBBindingList<GauntletPageOptionViewModel> Options => _options;");
            builder.AppendLine("        public void ExecuteSampleAction() { _sampleActionCount++; OnPropertyChanged(nameof(SampleButtonLabel)); }");
            builder.AppendLine("        public void ExecuteToggle() { IsOn = !IsOn; }");
            builder.AppendLine("        public void ExecuteNextOption() { if (Options.Count > 0) SelectedIndex = (SelectedIndex + 1) % Options.Count; }");
            builder.AppendLine("        private void RefreshOptions() { foreach (var option in Options) option.IsSelected = Options.IndexOf(option) == SelectedIndex; if (Options.Count > 0) ValueText = Options[SelectedIndex].Label; OnPropertyChanged(nameof(ValueText)); OnPropertyChanged(nameof(SelectedOptionLabel)); }");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("    public sealed class GauntletPageOptionViewModel : ViewModel");
            builder.AppendLine("    {");
            builder.AppendLine("        private bool _isSelected;");
            builder.AppendLine("        private readonly GauntletPageComponentViewModel _owner;");
            builder.AppendLine("        public GauntletPageOptionViewModel(string label, GauntletPageComponentViewModel owner) { Label = label; _owner = owner; }");
            builder.AppendLine("        [DataSourceProperty] public string Label { get; }");
            builder.AppendLine("        [DataSourceProperty] public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChangedWithValue(_isSelected, nameof(IsSelected)); } }");
            builder.AppendLine("        public void ExecuteSelect() { if (_owner != null) _owner.SelectedIndex = _owner.Options.IndexOf(this); }");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildLocalization(IEnumerable<KeyValuePair<string, string>> entries)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<strings>");
            foreach (var entry in entries)
                builder.Append("  <string id=\"").Append(EscapeXml(entry.Key)).Append("\" text=\"").Append(EscapeXml(entry.Value)).AppendLine("\" />");
            builder.Append("</strings>");
            return builder.ToString();
        }

        private static string BuildIntegration()
        {
            return "Replace the MyMod namespace and the prefab filename with your own stable identifiers. Add the generated English strings to your module language catalog and register the prefab through your existing module UI setup. In the screen you already own, construct GauntletPageViewModel, call GauntletLayer.LoadMovie with the prefab name, add the layer, and release the movie and layer during that screen's teardown. Keep that screen-opening hook in your existing SubModule lifecycle path; do not add a second SubModule override when one already exists. The generated button, toggle, and selector handlers change only their sample ViewModel values.";
        }

        private sealed class ViewModelContract
        {
            internal readonly HashSet<string> Properties = new HashSet<string>(StringComparer.Ordinal);
            internal readonly HashSet<string> Commands = new HashSet<string>(StringComparer.Ordinal);
            internal readonly Dictionary<string, string> Lists = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        private static List<string> ValidateContractCore(string prefabXml, string viewModelCode, string localizationXml)
        {
            var errors = new List<string>();
            XDocument document;
            try { document = XDocument.Parse(prefabXml); }
            catch { return new List<string> { "Generated prefab XML is not well formed." }; }
            XDocument localization;
            try { localization = XDocument.Parse(localizationXml); }
            catch { return new List<string> { "Generated localization XML is not well formed." }; }

            var contracts = BuildViewModelContracts(viewModelCode);
            if (!contracts.ContainsKey("GauntletPageViewModel"))
            {
                errors.Add("Generated root ViewModel class is missing.");
                return errors;
            }
            ValidatePrefabNode(document.Root, "GauntletPageViewModel", contracts, errors);

            var localizationIds = new HashSet<string>(localization.Descendants("string")
                .Select(element => (string)element.Attribute("id"))
                .Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.Ordinal);
            foreach (Match token in LocalizedIdPattern.Matches(viewModelCode))
                if (!localizationIds.Contains(token.Groups["id"].Value))
                    errors.Add("ViewModel localization ID is missing from the English catalog: " + token.Groups["id"].Value);
            if (!contracts["GauntletPageViewModel"].Properties.Contains("Components")
                || !contracts["GauntletPageViewModel"].Lists.ContainsKey("Components"))
                errors.Add("Generated root ViewModel must expose a data-bound Components list.");
            return errors;
        }

        private static Dictionary<string, ViewModelContract> BuildViewModelContracts(string source)
        {
            var classes = new Dictionary<string, ViewModelContract>(StringComparer.Ordinal);
            var classPattern = new Regex("\\bclass\\s+(?<name>[A-Za-z_]\\w*)[^\\{]*\\{", RegexOptions.Compiled);
            foreach (Match match in classPattern.Matches(source))
            {
                int open = source.IndexOf('{', match.Index);
                int close = FindMatchingBrace(source, open);
                if (open < 0 || close <= open) continue;
                string body = source.Substring(open + 1, close - open - 1);
                var contract = new ViewModelContract();
                foreach (Match property in PropertyPattern.Matches(body)) contract.Properties.Add(property.Groups["name"].Value);
                foreach (Match command in CommandMethodPattern.Matches(body)) contract.Commands.Add(command.Groups["name"].Value);
                foreach (Match list in ListPattern.Matches(body)) contract.Lists[list.Groups["name"].Value] = list.Groups["type"].Value;
                classes[match.Groups["name"].Value] = contract;
            }
            return classes;
        }

        private static int FindMatchingBrace(string source, int open)
        {
            if (open < 0) return -1;
            int depth = 0;
            bool inString = false, inCharacter = false, inLineComment = false, inBlockComment = false;
            for (int i = open; i < source.Length; i++)
            {
                char current = source[i];
                char next = i + 1 < source.Length ? source[i + 1] : '\0';
                if (inLineComment)
                {
                    if (current == '\n') inLineComment = false;
                    continue;
                }
                if (inBlockComment)
                {
                    if (current == '*' && next == '/') { inBlockComment = false; i++; }
                    continue;
                }
                if (inString || inCharacter)
                {
                    if (current == '\\') { i++; continue; }
                    if (inString && current == '"') inString = false;
                    else if (inCharacter && current == '\'') inCharacter = false;
                    continue;
                }
                if (current == '/' && next == '/') { inLineComment = true; i++; continue; }
                if (current == '/' && next == '*') { inBlockComment = true; i++; continue; }
                if (current == '"') { inString = true; continue; }
                if (current == '\'') { inCharacter = true; continue; }
                if (current == '{') depth++;
                else if (current == '}' && --depth == 0) return i;
            }
            return -1;
        }

        private static void ValidatePrefabNode(XElement element, string contextName, IDictionary<string, ViewModelContract> contracts, IList<string> errors)
        {
            if (element == null) return;
            string currentContext = contextName;
            if (element.Name.LocalName == "ItemTemplate")
            {
                var owner = element.Parent;
                string sourceName = GetDataSourceName(owner);
                if (sourceName == null || !contracts.TryGetValue(currentContext, out var ownerContract)
                    || !ownerContract.Lists.TryGetValue(sourceName, out currentContext)
                    || !contracts.ContainsKey(currentContext))
                {
                    errors.Add("ItemTemplate has no matching MBBindingList item context: " + (sourceName ?? "<missing>"));
                    return;
                }
            }

            contracts.TryGetValue(currentContext, out var context);
            foreach (var attribute in element.Attributes())
            {
                if (attribute.Name.LocalName == "DataSource")
                {
                    string sourceName = GetDataSourceName(element);
                    if (sourceName != null && (context == null || !context.Lists.ContainsKey(sourceName)))
                        errors.Add("DataSource does not resolve to an MBBindingList on " + currentContext + ": " + sourceName);
                    if (sourceName != null && !element.Elements().Any(child => child.Name.LocalName == "ItemTemplate"))
                        errors.Add("DataSource has no ItemTemplate: " + sourceName);
                }
                foreach (Match binding in BindingPattern.Matches(attribute.Value))
                    if (context == null || !context.Properties.Contains(binding.Groups[1].Value))
                        errors.Add("Prefab binding is missing from " + currentContext + ": " + binding.Groups[1].Value);
                if (attribute.Name.LocalName == "Command.Click" && attribute.Value.Length > 0
                    && (context == null || !context.Commands.Contains(attribute.Value)))
                    errors.Add("Prefab command is missing from " + currentContext + ": " + attribute.Value);
            }

            foreach (var child in element.Elements())
                ValidatePrefabNode(child, currentContext, contracts, errors);
        }

        private static string GetDataSourceName(XElement element)
        {
            string value = (string)element?.Attribute("DataSource");
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            return value.Length >= 2 && value[0] == '{' && value[value.Length - 1] == '}'
                ? value.Substring(1, value.Length - 2).Trim()
                : null;
        }

        private static string LocalizationId(string blockId) => "forge_composer_" + blockId;
        private static string TextLocalizationId(string blockId) => "forge_composer_" + blockId + "_text";
        private static string OptionLocalizationId(string blockId, int index) => "forge_composer_" + blockId + "_option_" + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static string EscapeXml(string value)
        {
            string escaped = SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;
            return escaped.Replace("\r", "&#13;").Replace("\n", "&#10;").Replace("\t", "&#9;");
        }

        private static string CSharpLiteral(string value)
        {
            var builder = new StringBuilder("\"");
            foreach (char character in value ?? string.Empty)
            {
                switch (character)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 0x20) builder.Append("\\u").Append(((int)character).ToString("x4"));
                        else builder.Append(character);
                        break;
                }
            }
            return builder.Append('"').ToString();
        }
    }
}
