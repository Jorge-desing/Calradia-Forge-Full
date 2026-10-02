using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using CalradiaForge.Core;
using CalradiaForge.Desktop;
using CalradiaForge.Desktop.Services;
using CalradiaForge.Sdk;

internal static class Program
{
    // The source-contract suite reads the same sizeable XAML and C# files from
    // many independent cases. Cache each immutable test input once per process
    // so the assertions stay unchanged without repeatedly hitting the disk.
    static readonly ConcurrentDictionary<string, Lazy<string>> sourceTextCache = new(StringComparer.OrdinalIgnoreCase);
    static readonly ConcurrentDictionary<string, Lazy<XDocument>> sourceXmlCache = new(StringComparer.OrdinalIgnoreCase);
    static readonly ConcurrentDictionary<string, Lazy<string>> desktopFilePathCache = new(StringComparer.OrdinalIgnoreCase);
    static readonly Lazy<string> desktopXamlTreeSource = new(LoadDesktopXamlSourceTree, LazyThreadSafetyMode.ExecutionAndPublication);

    static async Task<int> Main()
    {
        var cases = new (string Name, Func<Task> Run)[] {
            ("Desktop reconnects after server exit on a new process endpoint", Restart),
            ("Desktop bounds newline-delimited IPC responses and honors cancellation", BoundedIpcResponses),
            ("Desktop report exports are asynchronous, unique, and failure-safe", ReportExportSafety),
            ("Desktop dossier copy commands report failures and expose accessible controls", DossierCopyAccessibilitySurface),
            ("Desktop serializes concurrent requests without mixing responses", ConcurrentRequests),
            ("Desktop rejects mismatched response IDs and reconnects", MismatchedId),
            ("Desktop rejects incompatible handshake versions", IncompatibleVersion),
            ("Desktop retains negotiated ForgeWeave, replay, and preflight capabilities", Capabilities),
            ("Desktop sends a selected retained replay sequence without altering it", ReplayRequest),
            ("Disposed desktop client rejects connection and send", DisposedClient),
            ("Desktop request defaults carry protocol version and seed", RequestDefaults),
            ("Desktop request IDs are unique for independent operations", RequestIds),
            ("Desktop preserves a structured server error response", ServerErrorResponse),
            ("Desktop preserves Unicode arguments and selected seeds", RequestFields),
            ("Desktop capability matching is case insensitive", CapabilityMatching),
            ("Desktop clears stale capabilities after disconnect", CapabilityReset),
            ("Desktop PipeClient auto-reconnects to a new endpoint", AutoReconnectAndLastPid),
            ("Desktop session propagates cancellation through connect", SessionConnectCancellation),
            ("Desktop session propagates cancellation through reconnect", SessionReconnectCancellation),
            ("Desktop session propagates send cancellation without masking it as disconnect", SessionSendCancellation),
            ("Desktop PipeClient heartbeat detects disconnection", HeartbeatState),
            ("Desktop uses CommunityToolkit-backed cancelable MVVM commands", MvvmShellSurface),
            ("Desktop integrates pinned MVVM and WPF presentation toolkits", ToolkitIntegrationSurface),
            ("Desktop catalog is explicit and covers every reviewed route", RoutedCatalogSurface),
            ("Desktop does not load tools from a live visual tree", NoVisualTreeCatalog),
            ("Desktop workbench creates transient routed pages", RoutedPageSurface),
            ("Desktop input selection is available through native file and folder pickers", InputSelectionSurface),
            ("Desktop raw result is explicitly read-only and startup errors are recorded", StartupErrorRecovery),
            ("Desktop UIA startup is invisible and non-activating", UiaStartupIsolationSurface),
            ("WPF test launchers keep child consoles hidden and preserve focus evidence", FocusSafeTestLauncherSurface),
            ("Desktop page releases evidence and cancels work on disposal", PageDisposalSurface),
            ("Desktop Split Deck fallback labels exist in all locale catalogs", SplitDeckFallbackCatalogLocalization),
            ("Desktop search and role-filter labels and help are localized in all catalogs", SearchAndRoleFilterLocalization),
            ("Desktop services have no WPF control dependency", ServiceIsolationSurface),
            ("Desktop analyzer results retain provenance and bounded evidence", AnalyzerSurface),
            ("Desktop evidence ledger and export preserve actionable finding context", AnalyzerEvidenceProjection),
            ("Desktop marks malformed ForgeWeave snapshots as unparsed", ForgeWeaveUnparsedSurface),
            ("Desktop FBX route preserves structural provenance in its report", FbxAnalyzerRoute),
            ("Desktop generators state their template limit", TemplateSurface),
            ("Desktop live tools require an advertised session capability", LiveCapabilitySurface),
            ("Desktop hook workbench uses a confirmed one-use plan over registered IDs only", HookWorkbenchSurface),
            ("Desktop hook confirmation reconciles snapshots without clearing its busy state", HookWorkbenchReconciliationRefresh),
            ("Desktop keeps guarded state-changing work unavailable", WriterGateSurface),
            ("Desktop command seal is mathematically centered", MarkGeometry),
            ("Desktop tactical layout has no arbitrary diagonal divider", TacticalLayoutSurface),
            ("Desktop supports bounded responsive dimensions", ResponsiveSurface),
            ("Desktop recent history has a bounded scrolling viewport", RecentHistorySurface),
            ("Desktop resources use the tactical palette dynamically", DynamicThemeSurface),
            ("Desktop exposes three reversible theme dictionaries", ThemeCatalogSurface),
            ("Desktop preferences use atomic fallback persistence", PreferenceSurface),
            ("Desktop resource dictionaries have 13-language key parity", DynamicResourceParity),
            ("Desktop static visualizer labels are localized rather than embedded in XAML", VisualizerStaticLabelLocalization),
            ("Desktop package preserves all in-game language catalogs", UiCatalogCoverage),
            ("Desktop language changes do not traverse visual controls", ResourceOnlyLocalizationSurface),
            ("Desktop session transport measures connect and reconnect", SessionTransportSurface),
            ("Desktop metrics are bounded and scoped", MetricsSurface),
            ("Desktop metrics keep sync allocations and mark async allocations unavailable", MetricsMeasurementSemantics),
            ("Desktop title and documentation share the release value", DesktopVersionBlockSynchronization),
            ("Desktop tactical studios expose documentation, invariants, commands, and shell role presets", TacticalStudioEnrichmentAndRolePresets)
        };
        cases = cases.Concat(DesktopAssemblyServiceTests.Cases).ToArray();
        var failures = 0;
        foreach (var test in cases) {
            try {
                var run = test.Run();
                if (run.IsCompleted) await run;
                else await run.WaitAsync(TimeSpan.FromSeconds(15));
                Console.WriteLine("PASS " + test.Name);
            }
            catch (Exception e) { failures++; Console.WriteLine("FAIL " + test.Name + ": " + e); }
        }
        Console.WriteLine($"RESULT: {cases.Length-failures} passed, {failures} failed");
        return failures == 0 ? 0 : 1;
    }

    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    static string Desktop(string relative) => FindDesktopFile(relative);

    static string ReadSourceText(string path)
    {
        string fullPath = Path.GetFullPath(path);
        return sourceTextCache.GetOrAdd(fullPath, static file => new Lazy<string>(
            () => File.ReadAllText(file), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    // Presentation contracts may live in merged dictionaries or dedicated
    // controls after the shell is decomposed. Search the checked-in Desktop
    // XAML tree rather than requiring those contracts to remain in MainWindow.
    static string DesktopXamlSource => desktopXamlTreeSource.Value;

    static string LoadDesktopXamlSourceTree()
    {
        string sourceRoot = Path.GetDirectoryName(FindDesktopFile("CalradiaForge.Desktop.csproj"))
            ?? throw new DirectoryNotFoundException("Could not resolve the Desktop project directory.");
        if (!Directory.Exists(sourceRoot))
            throw new DirectoryNotFoundException("Could not locate the CalradiaForge.Desktop source tree.");

        var sourceFiles = EnumerateDesktopXamlFiles(sourceRoot)
            .OrderBy(file => Path.GetRelativePath(sourceRoot, file), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (sourceFiles.Length == 0)
            throw new FileNotFoundException("No source XAML files were found under src/CalradiaForge.Desktop.");

        var source = new StringBuilder();
        foreach (string file in sourceFiles)
        {
            source.AppendLine("<!-- Desktop source: " + Path.GetRelativePath(sourceRoot, file) + " -->");
            source.AppendLine(ReadSourceText(file));
        }
        return source.ToString();
    }

    static IEnumerable<string> EnumerateDesktopXamlFiles(string directory)
    {
        foreach (string file in Directory.EnumerateFiles(directory, "*.xaml", SearchOption.TopDirectoryOnly))
            yield return file;

        foreach (string childDirectory in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(childDirectory);
            if (name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                name.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
                name.Equals(".vs", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("artifacts", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (string file in EnumerateDesktopXamlFiles(childDirectory))
                yield return file;
        }
    }

    // XDocuments returned by this helper are inspected only; tests must not
    // mutate them, allowing repeated schema checks to share a single parse.
    static XDocument LoadSourceXml(string path)
    {
        string fullPath = Path.GetFullPath(path);
        return sourceXmlCache.GetOrAdd(fullPath, static file => new Lazy<XDocument>(
            () => XDocument.Load(file), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    static Task RoutedCatalogSurface()
    {
        var catalog = ReadSourceText(Desktop("Presentation/ToolCatalog.cs"));
        Check(catalog.Contains("DesktopToolDefinitions") && !catalog.Contains("RegisterTree"), "Catalog must be explicit and independent of TreeView");
        // One stable route uses the shared ID constant so its localization and
        // unsupported-state behavior cannot drift from the catalog identity.
        var routes = Regex.Matches(catalog, @"^\s*T\(\""", RegexOptions.Multiline).Count +
            Regex.Matches(catalog, @"^\s*T\(ToolDefinition\.ApiDeprecationToolId,", RegexOptions.Multiline).Count;
        Check(routes == 194, "Reviewed route inventory must contain exactly 194 definitions, including sprite readiness, FBX preflight, and the two assembly tools");
        Check(catalog.Contains("ToolDefinition.ApiDeprecationToolId"), "The unavailable API deprecation route must remain addressable by its stable identifier");
        Check(catalog.Contains("Duplicate desktop tool identifier"), "Duplicate route rejection is missing");
        foreach (var category in new[] { "Diagnostics & Safety", "Live Inspection & Memory", "Asset & XML Synthesizers", "Simulation & Balance", "Delivery & Deployment" })
            Check(catalog.Contains(category), "Catalog category is missing: " + category);
        return Task.CompletedTask;
    }

    static Task NoVisualTreeCatalog()
    {
        var catalog = ReadSourceText(Desktop("Presentation/ToolCatalog.cs"));
        var window = ReadSourceText(Desktop("MainWindow.xaml.cs"));
        Check(!catalog.Contains("System.Windows.Controls") && !catalog.Contains("Classify("), "Tool metadata must not be guessed from live visual elements");
        Check(!window.Contains("Visibility") && !window.Contains("SelectedItemChanged"), "Main window must not contain routed-visibility navigation");
        return Task.CompletedTask;
    }

    static Task RoutedPageSurface()
    {
        var xaml = DesktopXamlSource;
        var app = DesktopXamlSource;
        var page = ReadSourceText(Desktop("Presentation/WorkspacePageViewModel.cs"));
        var shell = ReadSourceText(Desktop("Presentation/DesktopShellViewModel.cs"));
        var mainWindow = ReadSourceText(Desktop("MainWindow.xaml.cs"));
        Check(xaml.Contains("<ContentControl") && xaml.Contains("Content=\"{Binding CurrentPage}\""), "Active tool must render through ContentControl");
        Check(page.Contains("ToolPageViewModel") && page.Contains("RunCommand") && page.Contains("ExportCommand"), "Tool page commands are missing");
        Check((shell.Contains("new ToolPageViewModel") || shell.Contains("new WorkbenchPageViewModel")) && shell.Contains("CurrentPage"), "Shell must create a selected page");
        Check(shell.Contains("PinnedTools") && shell.Contains("RecentTools") && shell.Contains("OpenPaletteCommand"), "Workbench shell must expose bounded navigation state");
        Check(shell.Contains("VisibleGroups") && shell.Contains("OperationalRailEntries") && shell.Contains("groupExpansion") && shell.Contains("SelectedTool"), "Workbench shell must retain groups, route selection, and flattened rail entries");
        Check(xaml.Contains("AutomationProperties.AutomationId=\"OperationalRailToolViewport\"") && xaml.Contains("ItemsSource=\"{Binding OperationalRailEntries}\""), "Operational rail must bind the flattened entries to its named viewport");
        Check(xaml.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"") && xaml.Contains("ScrollViewer.HorizontalScrollBarVisibility=\"Disabled\"") && xaml.Contains("ScrollViewer.CanContentScroll=\"True\""), "Operational rail must keep its own vertical scrolling viewport");
        Check(xaml.Contains("VirtualizingPanel.IsVirtualizing=\"True\"") && xaml.Contains("VirtualizingPanel.VirtualizationMode=\"Recycling\"") && xaml.Contains("<VirtualizingStackPanel"), "Operational rail must virtualize rows with recycling");
        Check(app.Contains("DataType=\"{x:Type presentation:OperationalRailGroupEntryViewModel}\"") && app.Contains("DataType=\"{x:Type presentation:OperationalRailToolEntryViewModel}\"") && app.Contains("DataType=\"{x:Type presentation:OperationalRailFooterEntryViewModel}\""), "Operational rail must render typed group, tool, and footer entries");
        Check(app.Contains("Group.ToggleCommand") && app.Contains("DataContext.SelectToolCommand") && app.Contains("DataContext.ToggleFavoriteCommand"), "Rail group, route, and favorite controls must retain their commands");
        var railViewModels = ReadSourceText(Desktop("Presentation/WorkbenchViewModels.cs"));
        Check(railViewModels.Contains("public bool IsSelected") && shell.Contains("previousEntry.IsSelected = false") &&
              shell.Contains("selectedEntry.IsSelected = !IsHookWorkbenchOpen") &&
              shell.Contains("selectedEntry.IsSelected = !value") &&
              shell.Contains("toolEntry.IsSelected = !isHookWorkbenchOpen && ReferenceEquals(tool, selectedTool)") &&
              app.Contains("Binding IsSelected") &&
              app.Contains("ComboSelectedBrush") && app.Contains("ComboSelectedTextBrush") && app.Contains("FocusBrush"),
            "The selected rail route must have observable styling, yield to an active dedicated route, and preserve keyboard focus feedback.");
        Check(xaml.Contains("AutomationProperties.AutomationId=\"OperationalSearchHint\"") &&
              xaml.Contains("{DynamicResource Ui.SearchHint}") && xaml.Contains("IsHitTestVisible=\"False\""),
            "The operational search must provide a localized, passive empty-field hint.");
        var navigation = ReadSourceText(Desktop("Presentation/WorkbenchNavigationControl.xaml"));
        var navigationCodeBehind = ReadSourceText(Desktop("Presentation/WorkbenchNavigationControl.xaml.cs"));
        Check(navigation.Contains("Text=\"{DynamicResource Ui.RoleLabel}\"") &&
              navigation.Contains("Text=\"{Binding ActiveModderRoleLabel}\"") &&
              navigation.Contains("ToolTip=\"{Binding ActiveModderRoleHint}\"") &&
              !navigation.Contains("Text=\"ROLE\""),
            "The role label, active preset, and help must be supplied by localized shell properties/resources.");
        Check(navigation.Contains("Command=\"{Binding ClearFilterCommand}\"") &&
              !navigation.Contains("Click=\"ClearToolFilterClick\"") &&
              !navigationCodeBehind.Contains("ClearToolFilterClick"),
            "The clear-search button must have one activation path so it clears once and requests focus once.");
        Check(app.Contains("ColumnDefinition Width=\"28\"") && app.Contains("Width=\"26\"") && app.Contains("FontFamily=\"Segoe UI Symbol\""), "Operational rail must reserve a centered, visible favorite-action column beside wrapped route labels");
        Check(xaml.Contains("Grid.Column=\"2\"") && xaml.Contains("Stretch=\"Uniform\"") && !xaml.Contains("TitleBarHeraldicFrameDecoration") && !app.Contains("TitleBarHeraldicFrameBrush"), "Tool artwork must stay in its dedicated column and the titlebar must avoid a duplicate centered compass frame");
        Check(xaml.Contains("AutomationProperties.AutomationId=\"CategorySelector\"") && xaml.Contains("ItemsSource=\"{Binding Categories}\"") && xaml.Contains("SelectedItem=\"{Binding SelectedCategory}\""), "Rail category selection must remain bound to the shell");
        Check(xaml.Contains("Text=\"{Binding Filter, UpdateSourceTrigger=PropertyChanged}\""), "Operational rail search must remain bound to the shell filter");
        Check(xaml.Contains("FocusRequestBehavior.Request=\"{Binding SearchFocusRequest}\"") && shell.Contains("SearchFocusRequest"), "Ctrl+F must focus the search field through a dependency-free MVVM behavior");
        Check(xaml.Contains("AutomationProperties.AutomationId=\"ClearToolFilterButton\"") &&
              xaml.Contains("Command=\"{Binding ClearFilterCommand}\"") &&
              xaml.Contains("DataTrigger Binding=\"{Binding Filter}\" Value=\"\"") &&
              shell.Contains("ClearFilterCommand = new(ClearFilter)") && shell.Contains("internal void ClearFilter()") &&
              shell.Contains("SearchFocusRequest++") && mainWindow.Contains("DataContext = shell;"),
            "Clearing a non-empty operational search must use the internal presentation command and return focus through the existing focus request.");
        Check(xaml.Contains("x:Class=\"CalradiaForge.Desktop.Presentation.WorkbenchShellView\"") &&
              xaml.Contains("presentation:WorkbenchHeaderControl") && xaml.Contains("presentation:WorkbenchStatusControl") &&
              xaml.Contains("presentation:WorkbenchNavigationControl") && xaml.Contains("presentation:WorkbenchWorkspaceControl") &&
              xaml.Contains("presentation:CommandPaletteControl") && xaml.Contains("presentation:WorkbenchFooterControl"),
            "MainWindow shell sections must remain independently reusable presentation controls.");
        var pageTemplates = ReadSourceText(Desktop("Resources/Views/ToolPageTemplates.xaml"));
        Check(System.Text.RegularExpressions.Regex.Matches(pageTemplates, "<DataTemplate x:Key=\"[A-Za-z]+DashboardTemplate\"").Count == 9 &&
              pageTemplates.Contains("DataType=\"{x:Type presentation:ToolPageViewModel}\""),
            "The route page and all nine specialist visualizers must remain reusable keyed templates.");
        Check(xaml.Contains("Key=\"D1\" Modifiers=\"Control\"") && xaml.Contains("OpenPaletteCommand"), "Keyboard palette and pinned-tool shortcuts are missing");
        return Task.CompletedTask;
    }

    static Task InputSelectionSurface()
    {
        var xaml = DesktopXamlSource;
        var page = ReadSourceText(Desktop("Presentation/WorkspacePageViewModel.cs"));
        var window = ReadSourceText(Desktop("MainWindow.xaml.cs"));
        var picker = ReadSourceText(Desktop("Services/DesktopInputPickerService.cs"));
        Check(xaml.Contains("{Binding BrowseFileCommand}") && xaml.Contains("{Binding BrowseFolderCommand}"), "Input controls must bind to page commands");
        Check(xaml.Contains("Ui.BrowseFile") && xaml.Contains("Ui.BrowseFolder") && xaml.Contains("Tool.RequiresInput"), "Picker controls must be localized and hidden when input is not required");
        Check(page.Contains("CanSelectInput") && page.Contains("SelectInput(bool folder)"), "File selection must remain MVVM command state");
        Check(picker.Contains("new OpenFileDialog") && picker.Contains("new OpenFolderDialog") && picker.Contains("AssemblyInspector"), "Native pickers must cover files, folders, and assembly-specific filters");
        Check(window.Contains("new DesktopInputPickerService().Pick"), "The composition root must provide the native picker service");
        return Task.CompletedTask;
    }

    static Task StartupErrorRecovery()
    {
        var xaml = DesktopXamlSource;
        var app = ReadSourceText(Desktop("App.xaml.cs"));
        Check(xaml.Contains("Text=\"{Binding RawResult, Mode=OneWay}\""), "RawResult TextBox must never create a TwoWay binding to a read-only value");
        Check(app.Contains("catch (Exception error)") && app.Contains("desktop-startup.log") && app.Contains("MessageBox.Show"), "Startup failures should show the cause and persist a diagnostic log");
        return Task.CompletedTask;
    }

    static Task PageDisposalSurface()
    {
        var page = ReadSourceText(Desktop("Presentation/WorkspacePageViewModel.cs"));
        var shell = ReadSourceText(Desktop("Presentation/DesktopShellViewModel.cs"));
        Check(page.Contains("public void Dispose()") && page.Contains("RunCommand.Cancel()") && page.Contains("Evidence.Clear()"), "Page disposal must cancel work and release evidence");
        Check(shell.Contains("(CurrentPage as IWorkspacePage)?.Dispose()"), "Shell must release prior pages before switching");
        Check(shell.Contains("void ReleaseCurrentPage()") && shell.Contains("CurrentPage = null;") && shell.Contains("CurrentWorkbenchPage = null;"), "Clearing a filtered selection must release and detach the previous page");
        return Task.CompletedTask;
    }

    static Task UiaStartupIsolationSurface()
    {
        var app = ReadSourceText(Desktop("App.xaml.cs"));
        var windowBehavior = ReadSourceText(Desktop("Services/NonActivatingWindowBehavior.cs"));
        var applyIndex = app.IndexOf("ApplyOffscreenInspection(w)", StringComparison.Ordinal);
        var showIndex = app.IndexOf("w.Show()", StringComparison.Ordinal);
        Check(applyIndex >= 0 && showIndex > applyIndex,
            "Read-only UIA startup must configure the main window before WPF shows its handle.");
        Check(windowBehavior.Contains("ShowActivated = false") && windowBehavior.Contains("ShowInTaskbar = false") &&
              windowBehavior.Contains("window.Left = -10000") && windowBehavior.Contains("window.Top = -10000") &&
              windowBehavior.Contains("window.Opacity = 0") && windowBehavior.Contains("PositionNoActivate") &&
              windowBehavior.Contains("EnsureHandle()"),
            "UIA inspection must keep its window invisible, off-screen, absent from the taskbar, and non-activating.");
        Check(app.Contains("if (!readOnlyUia)") && app.Contains("MessageBox.Show"),
            "Startup failures during read-only UIA inspection must be logged without presenting a modal window.");
        string runner = ReadSourceText(Path.Combine(WorkspaceRoot(), "tools/Test-CalradiaForge-Desktop-Uia.ps1"));
        Check(runner.Contains("$desktopPsi.CreateNoWindow = $true") &&
              runner.Contains("$desktopPsi.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden") &&
              runner.Contains("RecentEventSnapshot()") && runner.Contains("ForegroundTransitions"),
            "The UIA child must have no console window and its passive foreground observer must retain transition evidence.");
        return Task.CompletedTask;
    }

    static Task FocusSafeTestLauncherSurface()
    {
        string launcher = ReadSourceText(Path.Combine(WorkspaceRoot(), "tools/Run-CalradiaForge-Desktop-Checks-Hidden.vbs"));
        Check(launcher.Contains("Run-CalradiaForge-Tests.bat") &&
              !launcher.Contains("Test-CalradiaForge-Desktop-Uia.bat") &&
              launcher.Contains("UIA PowerShell smoke check is intentionally not included"),
            "The silent WPF runner must invoke the Desktop/render .bat and keep the separate PowerShell UIA smoke check opt-in.");
        Check(launcher.Contains("shell.Run(command, 0, True)") && launcher.Contains("desktop-checks-") &&
              launcher.Contains(".log") && launcher.Contains(".status.txt"),
            "The WPF test launcher must run without a visible command window and preserve logs and exit status.");
        Check(!launcher.Contains("SetForegroundWindow") && !launcher.Contains("Activate("),
            "The hidden runner must not attempt to move or restore foreground focus.");
        return Task.CompletedTask;
    }

    static Task SplitDeckFallbackCatalogLocalization()
    {
        var expectedKeys = new[] { "Ui.NoToolPinned", "Ui.PinnedToolOutput", "Ui.WorkspaceEvidence" };
        foreach (var language in Localization.SupportedLanguages)
        {
            var path = FindDesktopFile("Resources/Strings." + language.Code + ".xaml");
            var document = LoadSourceXml(path);
            foreach (var key in expectedKeys)
            {
                var localizedValue = document.Descendants().SingleOrDefault(node =>
                    string.Equals(node.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml"))?.Value, key, StringComparison.Ordinal));
                Check(localizedValue != null && !string.IsNullOrWhiteSpace(localizedValue.Value),
                    Path.GetFileName(path) + " must provide a non-empty " + key + " value.");
            }
        }
        return Task.CompletedTask;
    }

    static Task SearchAndRoleFilterLocalization()
    {
        var resourceKeys = new[]
        {
            "Ui.SearchHint", "Ui.LanguageChanged", "Ui.RoleLabel", "Ui.Role.All", "Ui.Role.NarrativeDialogues",
            "Ui.Role.TroopCombatArtisan", "Ui.Role.EconomyWorldArchitect", "Ui.Role.CoreDevPerformanceAuditor",
            "Ui.Role.Hint.All", "Ui.Role.Hint.NarrativeDialogues", "Ui.Role.Hint.TroopCombatArtisan",
            "Ui.Role.Hint.EconomyWorldArchitect", "Ui.Role.Hint.CoreDevPerformanceAuditor", "Ui.Role.FilteredStatus"
        };
        var english = LoadSourceXml(FindDesktopFile("Resources/Strings.en.xaml"));
        string ReadValue(XDocument document, string key)
        {
            var matches = document.Descendants().Where(node =>
                string.Equals(node.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml"))?.Value,
                    key, StringComparison.Ordinal)).ToArray();
            Check(matches.Length == 1 && !string.IsNullOrWhiteSpace(matches[0].Value),
                "Each locale must define exactly one non-empty resource " + key + ".");
            return matches[0].Value;
        }

        var englishValues = resourceKeys.ToDictionary(key => key, key => ReadValue(english, key), StringComparer.Ordinal);
        var checkedLocales = 0;
        foreach (var language in Localization.SupportedLanguages)
        {
            var path = FindDesktopFile("Resources/Strings." + language.Code + ".xaml");
            var document = LoadSourceXml(path);
            foreach (var key in resourceKeys)
            {
                var value = ReadValue(document, key);
                if (!string.Equals(language.Code, "en", StringComparison.OrdinalIgnoreCase))
                    Check(!string.Equals(value, englishValues[key], StringComparison.Ordinal),
                        Path.GetFileName(path) + " still uses the English fallback for " + key + ".");
            }
            checkedLocales++;
        }
        Check(checkedLocales == 13, "Search and role-filter localization must cover all 13 supported languages.");
        return Task.CompletedTask;
    }

    static Task ServiceIsolationSurface()
    {
        var service = ReadSourceText(Desktop("Services/DesktopWorkspaceService.cs"));
        Check(!service.Contains("System.Windows") && !service.Contains("TextBox") && !service.Contains("TreeView"), "Workspace service must not depend on WPF controls");
        Check(service.Contains("DesktopSessionService") && service.Contains("DesktopAnalysisService"), "Workspace service must own transport and analysis boundaries");
        return Task.CompletedTask;
    }

    static Task AnalyzerSurface()
    {
        var service = ReadSourceText(Desktop("Services/DesktopWorkspaceService.cs"));
        Check(service.Contains("analysis.Run(tool.Id, input, cancellation)") && service.Contains("WorkspaceEvidence") && service.Contains("Take(128)"), "Analyzer output must retain bounded evidence and forward cancellation");
        Check(service.Contains("Not run") && service.Contains("Unsupported"), "Unsupported and absent inputs must remain explicit");
        return Task.CompletedTask;
    }

    static Task AnalyzerEvidenceProjection()
    {
        var serviceSource = ReadSourceText(Desktop("Services/DesktopWorkspaceService.cs"));
        var evidenceSource = ReadSourceText(Desktop("Presentation/WorkspacePageViewModel.cs"));
        var xaml = DesktopXamlSource;
        var shell = ReadSourceText(Desktop("Presentation/DesktopShellViewModel.cs"));
        foreach (var field in new[] { "item.RuleId", "item.SourcePath", "item.Line", "item.Column", "item.Evidence", "item.Recommendation" })
            Check(serviceSource.Contains(field), "Analyzer projection must preserve " + field);
        foreach (var field in new[] { "RuleId", "SourcePath", "Line", "Column", "Evidence", "Recommendation", "FormatExportDetail" })
            Check(evidenceSource.Contains(field), "Workspace evidence must retain structured field " + field);
        foreach (var binding in new[] { "{Binding RuleId}", "{Binding Location}", "{Binding Evidence}", "{Binding RecommendationDisplay}" })
            Check(xaml.Contains(binding), "The primary evidence ledger must render " + binding);
        Check(shell.Contains("item.Source + \" | \" + item.Status + \" | \" + item.Detail"),
            "Exported evidence must include the complete detail serialization");

        var report = new DesktopAnalysisService().Format(new ForgeAnalysisResult
        {
            AnalyzerName = "Fixture Analyzer",
            AnalyzerId = "fixture",
            Provenance = ForgeAnalysisProvenance.Structural,
            State = ForgeAnalysisState.Completed,
            Findings =
            [
                new ForgeDiagnosticFinding
                {
                    RuleId = "fixture.material-name",
                    Severity = "Warning",
                    SourcePath = "assets/model.fbx",
                    Line = 17,
                    Column = 5,
                    Evidence = "Unknown material declaration.",
                    Recommendation = "Compare the name with the material inventory."
                }
            ]
        });
        foreach (var expected in new[] { "fixture.material-name", "assets/model.fbx:17:5", "Unknown material declaration.", "Next: Compare the name with the material inventory." })
            Check(report.Contains(expected), "Exported analysis report must retain " + expected);
        return Task.CompletedTask;
    }

    static Task ForgeWeaveUnparsedSurface()
    {
        var source = ReadSourceText(Desktop("Services/DesktopWorkspaceService.cs"));
        var start = source.IndexOf("WorkspaceExecutionResult FormatForgeWeaveLiveReport", StringComparison.Ordinal);
        var end = start < 0 ? -1 : source.IndexOf("static string GetForgeWeaveSnapshotFailure", start, StringComparison.Ordinal);
        Check(start >= 0 && end > start, "ForgeWeave live report parser must be present");
        Check(source.Contains("if (response.Success && tool.PipeAction == \"framework\")") &&
              source.Contains("FormatForgeWeaveLiveReport(response.Data ?? string.Empty)"),
            "Every successful framework response, including a missing or empty payload, must be validated as a snapshot");
        var parser = source.Substring(start, end - start);
        Check(parser.Contains("Status = \"Unparsed\"") &&
              parser.Contains("WorkspaceEvidence(\"ForgeWeave transport\", \"Received\"") &&
              parser.Contains("WorkspaceEvidence(\"ForgeWeave snapshot\", \"Unparsed\", parseFailure)"),
            "A successful transport with an unreadable snapshot must separate Received and Unparsed evidence");
        Check(parser.Contains("RawResult = jsonData"), "The exact received ForgeWeave payload must remain available for diagnosis");
        Check(!parser.Contains("Status = \"Completed\""), "A failed snapshot parse must never be reported as Completed");

        var failureFormatter = source.Substring(end, source.IndexOf("static string FormatHistogramBar", end, StringComparison.Ordinal) - end);
        Check(failureFormatter.Contains("maximumDiagnosticLength = 240") && failureFormatter.Contains("DescribeParseError") &&
              failureFormatter.Contains("CapturedAt") && failureFormatter.Contains("Status"),
            "Snapshot parse diagnostics must be bounded and reject payloads without required shape metadata");
        return Task.CompletedTask;
    }

    static Task FbxAnalyzerRoute()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "CalradiaForgeDesktopFbx-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var path = Path.Combine(tempRoot, "route.fbx");
            File.WriteAllText(path, "; FBX 7.4.0 project file\nGeometry: 1, \"Geometry::route_mesh\", \"Mesh\" { }", new UTF8Encoding(false));
            var service = new DesktopAnalysisService();
            var result = service.Run("FbxAsciiPreflight", path);
            Check(result.AnalyzerId == "fbx" && result.State == ForgeAnalysisState.Completed && result.Provenance == ForgeAnalysisProvenance.Structural,
                "The Desktop FBX route must reach the Core structural analyzer.");
            var report = service.Format(result);
            Check(report.StartsWith("STRUCTURAL ANALYSIS", StringComparison.Ordinal) && report.Contains("route_mesh"),
                "The Desktop raw result must retain structural provenance and FBX declaration evidence.");
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                var cancelled = service.Run("FbxAsciiPreflight", path, cancellation.Token);
                Check(cancelled.State == ForgeAnalysisState.Cancelled,
                    "The Desktop analysis service must forward its cancellation token to the Core analyzer.");
            }
            var verified = service.Format(new ForgeAnalysisResult { AnalyzerName = "Fixture", AnalyzerId = "fixture", Provenance = ForgeAnalysisProvenance.Verified });
            Check(verified.StartsWith("VERIFIED ANALYSIS", StringComparison.Ordinal), "Verified results must retain their explicit provenance label.");
        }
        finally { Directory.Delete(tempRoot, true); }
        return Task.CompletedTask;
    }

    static Task TemplateSurface()
    {
        var service = ReadSourceText(Desktop("Services/DesktopWorkspaceService.cs"));
        Check(service.Contains("Editable Calradia Forge starting point") && service.Contains("it has not been compiled or installed"), "Generator must label editable templates and their limit");
        return Task.CompletedTask;
    }

    static Task LiveCapabilitySurface()
    {
        var service = ReadSourceText(Desktop("Services/DesktopWorkspaceService.cs"));
        Check(service.Contains("session.IsConnected") && service.Contains("session.Supports(tool.PipeAction)") && service.Contains("No request was sent"), "Live tools must negotiate a capability before sending");
        return Task.CompletedTask;
    }

    static Task WriterGateSurface()
    {
        var page = ReadSourceText(Desktop("Presentation/WorkspacePageViewModel.cs"));
        Check(page.Contains("State-changing execution is unavailable from this guarded desktop route."), "State-changing desktop routes need a visible guard reason");
        return Task.CompletedTask;
    }

    static Task TacticalLayoutSurface()
    {
        var xaml = DesktopXamlSource;
        var app = DesktopXamlSource;
        var groups = ReadSourceText(Desktop("Presentation/WorkbenchViewModels.cs"));
        var icons = ReadSourceText(Desktop("Resources/TacticalIcons.xaml"));
        Check(!xaml.Contains("OrnamentalDivider") && !xaml.Contains("TacticalDividerBar"), "The new tactical layout must not use arbitrary dividers");
        Check(app.Contains("Ui.EvidenceLedger") && app.Contains("ItemsSource=\"{Binding Evidence}\""), "The evidence ledger view is missing");
        Check(xaml.Contains("GameIcon.compass") && xaml.Contains("GameIcon.magnifying_glass") && app.Contains("Icon.Recent") && app.Contains("OperationalRailRecentViewport") &&
              groups.Contains("GameIcon.archery_target") && groups.Contains("GameIcon.gears") && groups.Contains("GameIcon.gear_hammer") && groups.Contains("GameIcon.scroll_unfurled") &&
              groups.Contains("GameIcon.crossed_swords") &&
              app.Contains("Icon.Flag") && app.Contains("Tool.IconKey") && app.Contains("GameIcon.scroll_unfurled") &&
              xaml.Contains("AutomationProperties.AutomationId=\"HeaderSealButton\"") && xaml.Contains("Ui.HeaderSeal") &&
              xaml.Contains("AutomationProperties.AutomationId=\"DecorativeAccentsToggle\"") && xaml.Contains("Ui.DecorativeAccents"),
            "Semantic vector marks are missing from the shell or workbench groups");
        Check(!icons.Contains("Icon.Trim") && !icons.Contains("Icon.Corner"), "Arbitrary trim and corner decorations must not return");
        return Task.CompletedTask;
    }

    static Task ResponsiveSurface()
    {
        var xaml = DesktopXamlSource;
        var workspace = ReadSourceText(Desktop("Presentation/WorkbenchWorkspaceControl.xaml"));
        Check(xaml.Contains("Width=\"1360\"") && xaml.Contains("Height=\"820\"") && xaml.Contains("MinWidth=\"980\"") && xaml.Contains("MinHeight=\"680\""), "Workbench needs bounded responsive dimensions for common desktop work areas");
        Check(xaml.Contains("TextWrapping=\"Wrap\"") && xaml.Contains("TextTrimming=\"CharacterEllipsis\""), "Tool content must wrap and header text must trim before overlap");
        Check(xaml.Contains("<WrapPanel Grid.Row=\"0\" MinHeight=\"40\"") && xaml.Contains("<WrapPanel>") &&
              xaml.Contains("ResponsiveWorkbenchPanel") && xaml.Contains("MinimumPresentationWidth") &&
              xaml.Contains("ContextDossierToggleButton") && xaml.Contains("ContextDossierPanel") &&
              xaml.Contains("ResponsiveWorkbenchScrollViewport") && xaml.Contains("MinimumContextWidth") &&
              workspace.IndexOf("ActiveWorkbenchFrame", StringComparison.Ordinal) < workspace.IndexOf("PinnedDeckCard", StringComparison.Ordinal) &&
              workspace.IndexOf("PinnedDeckCard", StringComparison.Ordinal) < workspace.IndexOf("ContextDossierPanel", StringComparison.Ordinal) &&
              workspace.Contains("<presentation:AdaptiveWorkbenchPanel"),
            "The shell must wrap its header, scroll stacked workspace surfaces, and dock its optional contextual dossier without overlaying the route.");
        return Task.CompletedTask;
    }

    static Task RecentHistorySurface()
    {
        var app = DesktopXamlSource;
        var shell = ReadSourceText(Desktop("Presentation/DesktopShellViewModel.cs"));
        Check(app.Contains("AutomationProperties.AutomationId=\"OperationalRailPinnedViewport\"") && app.Contains("AutomationProperties.AutomationId=\"OperationalRailRecentViewport\""), "Pinned and recent rails must retain distinct scroll viewports");
        Check(app.Contains("DataContext.PinnedTools") && app.Contains("DataContext.RecentTools") && app.Contains("Token.SmallHistoryHeight") && app.Contains("Token.HistoryHeight"), "Pinned and recent entries must use bounded resource-driven collections");
        Check(app.Contains("VerticalScrollBarVisibility=\"Auto\"") && app.Contains("Ui.ScrollHistory"), "Pinned and recent history must advertise independent vertical scrolling");
        Check(shell.Contains("while (recent.Count > 12)"), "Recent history must retain a bounded twelve-entry limit");
        return Task.CompletedTask;
    }

    static Task ResourceOnlyLocalizationSurface()
    {
        var resolver = ReadSourceText(Desktop("Services/DesktopTextCatalog.cs"));
        var localizer = ReadSourceText(Desktop("Services/DesktopLocalizationService.cs"));
        Check(!resolver.Contains("Phrases") && !resolver.Contains("TranslateExisting"), "Visible text must not use a manual phrase map or control traversal");
        Check(localizer.Contains("MergedDictionaries") && localizer.Contains("activeDictionary"), "Language switching must replace the active Forge dictionary");
        return Task.CompletedTask;
    }

    static Task SessionTransportSurface()
    {
        var transport = ReadSourceText(Desktop("Services/DesktopSessionService.cs"));
        Check(transport.Contains("ipc:connect") && transport.Contains("ipc:reconnect"), "Transport must record short connect and reconnect operations");
        Check(transport.Contains("CancellationToken") && transport.Contains("PipeClient"), "Transport must be asynchronous and cancellable");
        Check(transport.Contains("pipe.Connect(pid.Value, cancellation)") && transport.Contains("processIdProvider(), 1500, cancellation") && transport.Contains("pipe.Send(request, cancellation)"), "Session cancellation token must reach connect, reconnect, and send operations");
        return Task.CompletedTask;
    }
    static async Task MustFail(Func<Task> operation)
    {
        try { await operation(); }
        catch (Exception e) when (e is IOException || e is ObjectDisposedException) { return; }
        throw new Exception("Expected a connection failure");
    }

    static async Task Restart()
    {
        using var client = new PipeClient();
        await using (var first = new Server()) {
            await client.Connect(first.Id);
            Check((await client.Send(new Request { Action="summary" })).Data == "summary", "Initial response missing");
        }
        await MustFail(() => client.Send(new Request { Action="summary" }));
        Check(!client.Connected, "Connection stayed active after failure");
        await using var second = new Server();
        await client.Connect(second.Id);
        Check(client.Connected, "Reconnect failed");
        Check((await client.Send(new Request { Action="report" })).Data == "report", "New server response missing");
    }

    static async Task ConcurrentRequests()
    {
        await using var server = new Server();
        using var client = new PipeClient();
        await client.Connect(server.Id);
        var requests = Enumerable.Range(0, 20).Select(i => new Request { Action="read-"+i }).ToArray();
        var results = await Task.WhenAll(requests.Select(client.Send));
        for (var i=0;i<requests.Length;i++)
            Check(results[i].Id == requests[i].Id && results[i].Data == requests[i].Action, "Responses crossed requests");
    }

    static async Task MismatchedId()
    {
        using var client = new PipeClient();
        await using (var bad = new Server(request => new Response {
            Id=request.Action == "hello" ? request.Id : "wrong-id", Success=true, Data=request.Action=="hello"?HelloData():request.Action
        })) {
            await client.Connect(bad.Id);
            await MustFail(() => client.Send(new Request { Action="report" }));
            Check(!client.Connected, "Invalid response did not disconnect");
        }
        await using var good = new Server();
        await client.Connect(good.Id);
        Check((await client.Send(new Request { Action="summary" })).Success, "Cannot recover after protocol error");
    }

    static async Task IncompatibleVersion()
    {
        await using var server = new Server(request => new Response { Id=request.Id, Version=99, Success=true });
        using var client = new PipeClient();
        await MustFail(() => client.Connect(server.Id));
        Check(!client.Connected, "Invalid handshake retained connection");
    }

    static async Task SessionConnectCancellation()
    {
        await using var server = new Server(request => request.Action == "hello" ? null : new Response { Id = request.Id, Success = true, Data = request.Action });
        using var client = new PipeClient();
        using var session = new DesktopSessionService(new DesktopMetricsService(), client, () => server.Id);
        using var cancellation = new CancellationTokenSource();

        var connecting = session.ConnectAsync(cancellation.Token);
        await server.WaitForRequest("hello").WaitAsync(TimeSpan.FromSeconds(3));
        cancellation.Cancel();
        var connected = await connecting.WaitAsync(TimeSpan.FromSeconds(3));

        Check(!connected && session.LastError == "Connection was cancelled.", "Connect cancellation must be reported as cancellation, not an unavailable endpoint");
        Check(!client.Connected, "Cancelled handshake left a live pipe behind");
    }

    static async Task SessionReconnectCancellation()
    {
        using var client = new PipeClient();
        await using (var first = new Server()) await client.Connect(first.Id);
        await using var replacement = new Server(request => request.Action == "hello" ? null : new Response { Id = request.Id, Success = true, Data = request.Action });
        using var session = new DesktopSessionService(new DesktopMetricsService(), client, () => replacement.Id);
        using var cancellation = new CancellationTokenSource();

        var reconnecting = session.ReconnectAsync(cancellation.Token);
        await replacement.WaitForRequest("hello").WaitAsync(TimeSpan.FromSeconds(3));
        cancellation.Cancel();
        var connected = await reconnecting.WaitAsync(TimeSpan.FromSeconds(3));

        Check(!connected && session.LastError == "Reconnection was cancelled.", "Reconnect cancellation must remain distinguishable from ordinary reconnect failure");
        Check(!client.Connected, "Cancelled reconnect left a live pipe behind");
    }

    static async Task SessionSendCancellation()
    {
        await using var server = new Server(request => request.Action == "hello"
            ? new Response { Id = request.Id, Success = true, Data = HelloData() }
            : null);
        using var client = new PipeClient();
        using var session = new DesktopSessionService(new DesktopMetricsService(), client, () => server.Id);
        Check(await session.ConnectAsync(CancellationToken.None), "Test session did not connect");
        using var cancellation = new CancellationTokenSource();

        var sending = session.SendAsync(new Request { Action = "hold-open" }, cancellation.Token);
        await server.WaitForRequest("hold-open").WaitAsync(TimeSpan.FromSeconds(3));
        cancellation.Cancel();
        try
        {
            await sending.WaitAsync(TimeSpan.FromSeconds(3));
            throw new Exception("Expected the pending send to be cancelled");
        }
        catch (OperationCanceledException error)
        {
            Check(error.CancellationToken == cancellation.Token, "Send cancellation did not preserve the caller token");
        }
        Check(!client.Connected, "Cancelled request must discard its now-unsynchronized pipe");
        Check(session.LastError == null, "Send cancellation was overwritten with an ambiguous disconnect error");
    }

    static async Task Capabilities()
    {
        await using var server=new Server(request=>new Response {Id=request.Id,Success=true,Data=request.Action=="hello"?Json.Serialize(new[]{"protocol:1","summary","framework","replay","patch-preflight"}):request.Action});
        using var client=new PipeClient();await client.Connect(server.Id);
        Check(client.Supports("framework")&&client.Supports("replay")&&client.Supports("patch-preflight")&&!client.Supports("harmony")&&client.Capabilities.Count==5,"Capability negotiation was not retained");
    }

    static async Task ReplayRequest()
    {
        Request replay=null;
        await using var server=new Server(request=>
        {
            if(request.Action=="replay")replay=request;
            return new Response {Id=request.Id,Success=true,Data=request.Action=="hello"?Json.Serialize(new[]{"protocol:1","summary","framework","replay"}):request.Action};
        });
        using var client=new PipeClient();await client.Connect(server.Id);
        var response=await client.Send(new Request {Action="replay",Argument="42"});
        Check(response.Success&&response.Data=="replay","Replay response was not returned");
        Check(replay!=null&&replay.Action=="replay"&&replay.Argument=="42","Replay sequence changed before it reached the pipe");
    }

    static async Task DisposedClient()
    {
        using var client = new PipeClient();
        client.Dispose();
        await MustFail(() => client.Connect(1));
        await MustFail(() => client.Send(new Request { Action="summary" }));
        Check(!client.Connected, "Disposed client connected");
    }

    static Task UiCatalogCoverage()
    {
        var ui=Path.Combine(AppContext.BaseDirectory,"Ui");
        var view=LoadSourceXml(Path.Combine(ui,"MainWindow.xaml"));
        Check(view.Root != null, "MainWindow.xaml could not be loaded");
        var files=Directory.GetFiles(ui,"*.xml");
        var expected=Localization.SupportedLanguages.Select(item=>item.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actual=files.Select(file=>Path.GetFileNameWithoutExtension(file)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Check(expected.SetEquals(actual),"Desktop catalogs do not match Bannerlord's supported language set");
        Check(Localization.Text("Summary","de")=="Übersicht","German catalog was not loaded");
        Check(Localization.Text("Summary","zh-HANT")=="概覽","Traditional Chinese catalog was not loaded");
        foreach(var code in expected.Where(code=>!code.Equals("en",StringComparison.OrdinalIgnoreCase)))
            Check(Localization.Text("Summary",code)!="Summary",code+" summary is still the English token");
        Check(Localization.Text("Modules","xx")=="Modules","Unsupported catalog did not fall back to English");
        Check(Localization.Text("Summary","")=="Summary","Empty language did not use English fallback");
        return Task.CompletedTask;
    }

    static Task RequestDefaults()
    {
        var request = new Request { Action = "summary" };
        Check(request.Version == 1 && request.Seed == 148, "Request defaults changed");
        Check(!string.IsNullOrWhiteSpace(request.Id), "Request ID default is empty");
        return Task.CompletedTask;
    }

    static Task RequestIds()
    {
        var ids = Enumerable.Range(0, 64).Select(_ => new Request { Action = "summary" }.Id).ToArray();
        Check(ids.Distinct(StringComparer.Ordinal).Count() == ids.Length, "Independent request IDs collided");
        return Task.CompletedTask;
    }

    static async Task ServerErrorResponse()
    {
        await using var server = new Server(request => new Response
        {
            Id = request.Id,
            Success = request.Action == "hello",
            Data = request.Action == "hello" ? HelloData() : null,
            Error = request.Action == "hello" ? null : "short diagnostic error"
        });
        using var client = new PipeClient();
        await client.Connect(server.Id);
        var response = await client.Send(new Request { Action = "diagnostics" });
        Check(!response.Success && response.Error == "short diagnostic error", "Structured server error was lost");
        Check(client.Connected, "A structured error response unexpectedly disconnected the client");
    }

    static async Task RequestFields()
    {
        Request received = null;
        await using var server = new Server(request =>
        {
            received = request;
            return new Response { Id = request.Id, Success = true, Data = request.Action == "hello" ? HelloData() : request.Action };
        });
        using var client = new PipeClient();
        await client.Connect(server.Id);
        await client.Send(new Request { Action = "inspect", Argument = "héroe/中文", Seed = 987 });
        Check(received?.Argument == "héroe/中文" && received.Seed == 987, "Request fields were changed in transit");
    }

    static async Task CapabilityMatching()
    {
        await using var server = new Server(request => new Response
        {
            Id = request.Id,
            Success = true,
            Data = request.Action == "hello" ? Json.Serialize(new[] { "protocol:1", "Framework", "REPLAY" }) : request.Action
        });
        using var client = new PipeClient();
        await client.Connect(server.Id);
        Check(client.Supports("framework") && client.Supports("replay"), "Capability matching became case sensitive");
    }

    static async Task CapabilityReset()
    {
        using var client = new PipeClient();
        await using (var server = new Server())
        {
            await client.Connect(server.Id);
            Check(client.Capabilities.Count > 0, "Initial capabilities were not negotiated");
        }
        await MustFail(() => client.Send(new Request { Action = "summary" }));
        Check(client.Capabilities.Count == 0 && !client.Connected, "Disconnected client retained stale capabilities");
    }

    static Task MarkGeometry()
    {
        var view = DesktopXamlSource;
        Check(view.Contains("Grid Width=\"56\" Height=\"56\"") && view.Contains("M28,10 L28,46 M10,28 L46,28"), "Command seal crosshair must share a 28,28 center");
        Check(view.Contains("Ellipse Width=\"12\" Height=\"12\"") && view.Contains("HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\""), "Command seal centre point is not centered");
        return Task.CompletedTask;
    }

    static Task MvvmShellSurface()
    {
        string primitives = ReadSourceText(FindDesktopFile("Presentation/MvvmPrimitives.cs"));
        string shell = ReadSourceText(FindDesktopFile("Presentation/DesktopShellViewModel.cs"));
        string window = ReadSourceText(FindDesktopFile("MainWindow.xaml.cs"));
        Check(primitives.Contains("CommunityToolkit.Mvvm.ComponentModel.ObservableObject") && primitives.Contains("ToolkitRelayCommand"), "Desktop must delegate MVVM state and commands to CommunityToolkit.Mvvm");
        Check(primitives.Contains("class ObservableObject") && primitives.Contains("class AsyncRelayCommand"), "Compatibility MVVM facade is missing");
        Check(primitives.Contains("ToolkitAsyncRelayCommand") && primitives.Contains("public void Cancel()"), "CommunityToolkit async command must expose cancellation");
        Check(shell.Contains("ToolPageViewModel") && shell.Contains("CurrentPage") && shell.Contains("DesktopWorkspaceService"), "Shell must route work through page view models and services");
        Check(window.Contains("DataContext = shell;") && window.Contains("Window_Closed"), "Window must bind to the shell and retain only lifecycle code");
        return Task.CompletedTask;
    }

    static Task ToolkitIntegrationSurface()
    {
        string project = ReadSourceText(FindDesktopFile("CalradiaForge.Desktop.csproj"));
        string primitives = ReadSourceText(FindDesktopFile("Presentation/MvvmPrimitives.cs"));
        string app = DesktopXamlSource;
        string window = DesktopXamlSource;
        string notices = ReadSourceText(Path.Combine(WorkspaceRoot(), "THIRD_PARTY_NOTICES.md"));
        Check(project.Contains("<Company>Calradia Forge Team</Company>") && project.Contains("<Product>Calradia Forge Desktop</Product>"), "Desktop assembly identity metadata is missing");
        Check(project.Contains("<Description>") && project.Contains("<Copyright>"), "Desktop assembly descriptive metadata is missing");
        Check(project.Contains("<UseAppHost>false</UseAppHost>"), "Desktop must keep app-host executable generation disabled");
        foreach (var package in new[] { "CommunityToolkit.Mvvm\" Version=\"8.4.2", "MaterialDesignThemes\" Version=\"5.3.2", "AsmResolver.DotNet\" Version=\"6.0.1" })
            Check(project.Contains(package), "Pinned Desktop package is missing: " + package);
        Check(primitives.Contains("CommunityToolkit.Mvvm") && app.Contains("MaterialDesign3.Defaults.xaml"), "CommunityToolkit or MaterialDesign resources are not wired");
        Check(!window.Contains("SymbolIcon") && !window.Contains("PackIcon"), "Font-dependent symbol controls must be replaced by vector resources.");
        foreach (var package in new[] { "CommunityToolkit.Mvvm", "MaterialDesignThemes", "AsmResolver" })
            Check(notices.Contains(package), "Third-party notice is missing: " + package);
        return Task.CompletedTask;
    }

    static Task DynamicResourceParity()
    {
        var supportedLanguages = Localization.SupportedLanguages.ToArray();
        Check(supportedLanguages.Length == 13, "Supported language count changed");
        Check(supportedLanguages.Select(item => item.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() == supportedLanguages.Length, "Language codes are not unique");
        Check(supportedLanguages.Select(item => item.DisplayName).Distinct(StringComparer.Ordinal).Count() == supportedLanguages.Length, "Language display names are not unique");
        var expected = supportedLanguages.Select(item => item.Code).OrderBy(code => code).ToArray();
        var files = expected.Select(code => FindDesktopFile("Resources/Strings." + code + ".xaml")).ToArray();
        var english = LoadSourceXml(files.Single(file => file.EndsWith("Strings.en.xaml", StringComparison.OrdinalIgnoreCase)))
            .Descendants().Where(node => node.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")) != null)
            .Select(node => node.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")).Value).ToHashSet(StringComparer.Ordinal);
        Check(english.Count >= 7, "English resource dictionary is incomplete");
        foreach (var file in files)
        {
            var keys = LoadSourceXml(file).Descendants().Where(node => node.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")) != null)
                .Select(node => node.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")).Value).ToHashSet(StringComparer.Ordinal);
            Check(keys.SetEquals(english), Path.GetFileName(file) + " lacks English resource-key parity");
        }
        var requiredTranslations = new[]
        {
            "Ui.CopyCommandAccessibleName", "Ui.CopyCommandAccessibleNameFormat", "Ui.CopyCliAccessibleName",
            "Ui.ClipboardCopyFailed", "Ui.ClipboardCopySucceeded", "Ui.ReportExported",
            "Ui.ReportExportFailed", "Ui.ReportExportCancelled", "Ui.ReportExportedShort", "Ui.HooksCancelUnconfirmed", "Viz.Audio.Title",
            "Viz.Audio.Equalizer", "Viz.Audio.Waveform", "Viz.Operation.Title", "Viz.Operation.ExecutionGuard",
            "Viz.Troop.CountBadge", "Viz.Troop.TierI", "Viz.Troop.TierTwoThree", "Viz.Troop.TierFour",
            "Viz.Troop.NobleLine", "Viz.Troop.CommonLevies", "Viz.Troop.LevelShort", "Viz.Troop.HpLabel",
            "Viz.Troop.WageLabel", "Viz.Troop.CostLabel", "Viz.Troop.EquipmentLabel", "Viz.Troop.ArmorLabel",
            "Viz.Troop.HeadLabel", "Viz.Troop.BodyLabel", "Viz.Troop.LegLabel", "Viz.Troop.Upgrades",
            "Viz.Workshop.ScopeLabel", "Viz.Workshop.OptimalLabel", "Viz.Workshop.ProsperityLabel",
            "Viz.Workshop.CivicLoyaltyLabel", "Viz.Workshop.SecurityScoreLabel", "Viz.Workshop.FoodStorageLabel",
            "Viz.Workshop.GarrisonLabel", "Viz.Workshop.RebellionRiskLabel", "Viz.Workshop.StableLabel",
            "Viz.Workshop.RiskThresholdLabel", "Viz.CodeSecurity.TargetAssemblyLabel", "Viz.CodeSecurity.RuntimeClrLabel",
            "Viz.ModuleHierarchy.IdentifierLabel", "Viz.ModuleHierarchy.ReleaseTagLabel", "Viz.ModuleHierarchy.StageLabel",
            "Viz.ModuleHierarchy.UpstreamDependenciesLabel", "Viz.Diplomacy.RealmLabel", "Viz.Diplomacy.MonarchLabel",
            "Viz.Diplomacy.BorderConflictRiskLabel", "Viz.Component.IdentifierLabel", "Viz.Component.SchemaTargetLabel",
            "Viz.Component.GeneratedMembersLabel"
        };
        var englishValues = LoadSourceXml(files.Single(file => file.EndsWith("Strings.en.xaml", StringComparison.OrdinalIgnoreCase))).Descendants()
            .Where(node => node.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")) != null)
            .ToDictionary(node => node.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")).Value, node => node.Value, StringComparer.Ordinal);
        foreach (var file in files)
        {
            var values = LoadSourceXml(file).Descendants()
                .Where(node => node.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")) != null)
                .ToDictionary(node => node.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")).Value, node => node.Value, StringComparer.Ordinal);
            foreach (var key in requiredTranslations)
                Check(values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value), Path.GetFileName(file) + " has no value for " + key);
            if (!file.EndsWith("Strings.en.xaml", StringComparison.OrdinalIgnoreCase))
                foreach (var key in requiredTranslations)
                    Check(!string.Equals(values[key], englishValues[key], StringComparison.Ordinal), Path.GetFileName(file) + " leaves " + key + " untranslated.");
        }
        string service = ReadSourceText(FindDesktopFile("Services/DesktopLocalizationService.cs"));
        Check(service.Contains("MergedDictionaries") && service.Contains("activeDictionary"), "Language changes must swap only the active Forge dictionary");
        return Task.CompletedTask;
    }

    static Task HookWorkbenchSurface()
    {
        var viewModel = ReadSourceText(Desktop("Presentation/HookWorkbenchViewModel.cs"));
        var xaml = ReadSourceText(Desktop("Presentation/HookWorkbenchControl.xaml"));
        var confirmationStart = viewModel.IndexOf("async Task ConfirmPlanAsync", StringComparison.Ordinal);
        var confirmationEnd = viewModel.IndexOf("void ApplySnapshotFailure", confirmationStart, StringComparison.Ordinal);
        var confirmation = viewModel.Substring(confirmationStart, confirmationEnd - confirmationStart);
        var refresh = viewModel.Substring(viewModel.IndexOf("async Task RefreshSnapshotsAsync", StringComparison.Ordinal));
        refresh = refresh.Substring(0, refresh.IndexOf("async Task CreatePlanAsync", StringComparison.Ordinal));
        var clearToken = confirmation.IndexOf("ClearPendingPlan();", StringComparison.Ordinal);
        var send = confirmation.IndexOf("session.SendAsync", StringComparison.Ordinal);
        Check(xaml.Contains("AutomationProperties.AutomationId=\"HookExplicitConfirmationCheckbox\"") &&
              xaml.Contains("IsChecked=\"{Binding IsConfirmationChecked, Mode=TwoWay}\""),
            "Apply/Revert must require an explicit checkbox in the reviewed plan.");
        Check(viewModel.Contains("pendingPlan.ExpiresAtUtc > DateTimeOffset.UtcNow && IsConfirmationChecked") &&
              viewModel.Contains("pendingPlan.RequiresConfirmation"),
            "The confirmation command must be disabled without a checked box, live plan, and host confirmation requirement.");
        Check(clearToken >= 0 && send > clearToken &&
              Regex.Matches(confirmation, @"session\.SendAsync").Count == 1 &&
              confirmation.Contains("Ui.HooksUnknownOutcome"),
            "The one-use confirmation token must be discarded before exactly one send, and uncertain outcomes must be reported without retry.");
        Check(viewModel.Contains("new HookIpcSelection { HookIds = selectedIds.ToList() }") &&
              Regex.Matches(xaml, "<TextBox").Count == 3 &&
              xaml.Contains("Text=\"{Binding OwnerFilter, UpdateSourceTrigger=PropertyChanged}\"") &&
              xaml.Contains("Text=\"{Binding TargetFilter, UpdateSourceTrigger=PropertyChanged}\"") &&
              xaml.Contains("Text=\"{Binding TypeFilter, UpdateSourceTrigger=PropertyChanged}\"") &&
              !xaml.Contains("TargetMethod=\"{Binding") &&
              xaml.Contains("Text=\"{Binding Target}\""),
            "The UI may select registered IDs and show exact target metadata, but must not accept free-form target/member input.");
        Check(viewModel.Contains("SameSnapshot(source, plannedSnapshot)") &&
              viewModel.Contains("!IsEligible") && xaml.Contains("{Binding EligibilityText}"),
            "A plan must match the exact current snapshot and display the host context gate.");
        Check(viewModel.Contains("string.Equals(State, \"Conflict\", StringComparison.OrdinalIgnoreCase)") &&
              viewModel.Contains("string.Equals(State, \"Failed\", StringComparison.OrdinalIgnoreCase)") &&
              viewModel.Contains("snapshotReadSucceeded") &&
              xaml.Contains("AutomationProperties.AutomationId=\"HookLifetimeWarning\"") &&
              xaml.Contains("AutomationProperties.AutomationId=\"HookApplyPartialWarning\"") &&
              viewModel.Contains("Ui.HooksApplyPartialWarning"),
            "The hook workbench must disclose callback lifetime and sequential partial Apply, and allow recovery planning for uncertain hook states.");
        Check(viewModel.Contains("ReadStrings(root, \"notAttemptedIds\")") &&
              viewModel.Contains("TryGetStringInsensitive(\"stopReason\"") &&
              viewModel.Contains("commit.RequiresReconciliation") && viewModel.Contains("requiresSnapshotRefresh") &&
              viewModel.Contains("Reading fresh hook snapshots before another plan"),
            "Partial, cancelled, stopped, or unattempted host results must be shown as uncertain and reconciled before another plan.");
        var snapshotReset = refresh.IndexOf("snapshotReadSucceeded = false;", StringComparison.Ordinal);
        var canRefresh = refresh.IndexOf("if (!CanRefreshSnapshots(allowWhileBusy))", StringComparison.Ordinal);
        var cancellationCatch = refresh.IndexOf("catch (OperationCanceledException)", StringComparison.Ordinal);
        var genericCatch = refresh.IndexOf("catch (Exception error)", StringComparison.Ordinal);
        var cancellationHandler = cancellationCatch >= 0 && genericCatch > cancellationCatch
            ? refresh.Substring(cancellationCatch, genericCatch - cancellationCatch)
            : string.Empty;
        Check(snapshotReset >= 0 && canRefresh > snapshotReset &&
              cancellationHandler.Contains("requiresSnapshotRefresh = true;") &&
              cancellationHandler.Contains("snapshotReadSucceeded = false;") &&
              confirmation.Contains("if (snapshotReadSucceeded)") &&
              confirmation.Contains("SetLocalizedStatusProvider(() => FormatCommitStatus(commit, plan.HookIds))") &&
              confirmation.Contains("SetLocalizedStatusWithPrevious("),
            "A cancelled reconciliation refresh must clear stale success state, preserve uncertainty, and keep localized status text refreshable.");
        var localizationRefreshStart = viewModel.IndexOf("public void NotifyLocalizationChanged()", StringComparison.Ordinal);
        var localizationRefreshEnd = viewModel.IndexOf("async Task CancelPendingPlanAsync", localizationRefreshStart, StringComparison.Ordinal);
        var localizationRefresh = localizationRefreshStart < 0 || localizationRefreshEnd <= localizationRefreshStart
            ? string.Empty
            : viewModel.Substring(localizationRefreshStart, localizationRefreshEnd - localizationRefreshStart);
        Check(localizationRefresh.Contains("RefreshLocalizedStatus();") &&
              viewModel.Contains("statusProvider = provider;") &&
              viewModel.Contains("Set(ref status, statusProvider() ?? string.Empty, nameof(Status));"),
            "Hook Workbench status messages must resolve again after the selected UI language changes.");
        foreach (var key in new[] { "Ui.HookWorkbench", "Ui.HooksIdentifier", "Ui.HooksCountFormat", "Ui.HooksConfirmPrompt", "Ui.HooksCancelUnconfirmed", "Ui.HooksLifetimeWarning", "Ui.HooksApplyPartialWarning" })
            Check(Directory.GetFiles(Path.GetDirectoryName(Desktop("Resources/Strings.en.xaml"))!, "Strings.*.xaml")
                    .All(file => ReadSourceText(file).Contains("x:Key=\"" + key + "\"")),
                "Hook workbench localization key is missing from a language dictionary: " + key);
        Check(viewModel.Contains("RequiredCapabilities") && viewModel.Contains("\"hook-plan-cancel\"") &&
              viewModel.Contains("Action = \"hook-plan-cancel\"") &&
              viewModel.Contains("HookIpcCancelPlanRequest { Session = plan.Session, Token = plan.Token }") &&
              viewModel.IndexOf("TryReadCancelPlanResult(response.Data, plan.Session", StringComparison.Ordinal) <
              viewModel.IndexOf("if (ReferenceEquals(pendingPlan, plan)) ClearPendingPlan();", StringComparison.Ordinal),
            "Plan cancellation must use one exact host session/token request and retain the local plan until the matching result is verified.");
        Check(refresh.Contains("if (HasPendingPlan && !await CancelPendingPlanCoreAsync(cancellation).ConfigureAwait(true)) return;") &&
              Regex.Matches(refresh, @"session\.SendAsync").Count == 1,
            "Snapshot refresh must stop when the host cannot confirm cancellation of the current plan.");
        var cancelCoreStart = viewModel.IndexOf("async Task<bool> CancelPendingPlanCoreAsync", StringComparison.Ordinal);
        var cancelCoreEnd = viewModel.IndexOf("void ClearPendingPlan()", cancelCoreStart, StringComparison.Ordinal);
        var cancelCore = cancelCoreStart < 0 || cancelCoreEnd <= cancelCoreStart
            ? string.Empty
            : viewModel.Substring(cancelCoreStart, cancelCoreEnd - cancelCoreStart);
        Check(cancelCore.Contains("if (!TryReadCancelPlanResult(response.Data, plan.Session, out _, out var error))") &&
              !cancelCore.Contains("|| !cancelled") &&
              cancelCore.Contains("if (ReferenceEquals(pendingPlan, plan)) ClearPendingPlan();") &&
              cancelCore.Contains("return true;"),
            "A valid same-session response that the old token is no longer pending must clear that stale preview and allow snapshot reconciliation.");
        var cancelParserStart = viewModel.IndexOf("public static bool TryReadCancelPlanResult(", StringComparison.Ordinal);
        var cancelParser = cancelParserStart < 0 ? string.Empty : viewModel.Substring(cancelParserStart);
        Check(cancelParser.Contains("string.Equals(session, expectedSession, StringComparison.Ordinal)") &&
              cancelParser.Contains("TryGetBooleanInsensitive(\"cancelled\", out cancelled)") &&
              cancelParser.Contains("Plan cancellation response did not match the current session"),
            "Cancellation results must require the exact session, a Boolean cancellation result, and reject mismatched or malformed payloads.");
        return Task.CompletedTask;
    }

    static Task HookWorkbenchReconciliationRefresh()
    {
        var viewModel = ReadSourceText(Desktop("Presentation/HookWorkbenchViewModel.cs"));
        var confirm = viewModel.Substring(viewModel.IndexOf("async Task ConfirmPlanAsync", StringComparison.Ordinal));
        var refresh = viewModel.Substring(viewModel.IndexOf("async Task RefreshSnapshotsAsync(CancellationToken cancellation, bool allowWhileBusy)", StringComparison.Ordinal));
        refresh = refresh.Substring(0, refresh.IndexOf("async Task CreatePlanAsync", StringComparison.Ordinal));
        Check(viewModel.Contains("Task RefreshSnapshotsAsync(CancellationToken cancellation) => RefreshSnapshotsAsync(cancellation, allowWhileBusy: false);") &&
              viewModel.Contains("await RefreshSnapshotsAsync(cancellation, allowWhileBusy: true)") &&
              viewModel.Contains("bool CanRefreshSnapshots(bool allowWhileBusy) => !disposed && (allowWhileBusy || !IsBusy)"),
            "Only the internal post-confirm refresh may bypass the manual refresh busy guard.");
        Check(refresh.Contains("if (!CanRefreshSnapshots(allowWhileBusy))") &&
              refresh.Contains("var ownsBusyState = !IsBusy;") &&
              refresh.Contains("if (ownsBusyState) IsBusy = true;") &&
              refresh.Contains("if (ownsBusyState) IsBusy = false;"),
            "The internal refresh must run while confirmation owns busy state and must not release that state early.");
        Check(confirm.IndexOf("await RefreshSnapshotsAsync(cancellation, allowWhileBusy: true)", StringComparison.Ordinal) >
              confirm.IndexOf("if (!commit.TokenConsumed)", StringComparison.Ordinal) &&
              confirm.Contains("if (snapshotReadSucceeded)"),
            "A consumed hook operation must perform reconciliation before accepting the outcome snapshot.");
        return Task.CompletedTask;
    }

    static Task VisualizerStaticLabelLocalization()
    {
        string templates = ReadSourceText(FindDesktopFile("Resources/Views/ToolPageTemplates.xaml"));
        var localizedKeys = new[]
        {
            "Viz.Troop.CountBadge", "Viz.Troop.TierI", "Viz.Troop.TierTwoThree", "Viz.Troop.TierFour",
            "Viz.Troop.NobleLine", "Viz.Troop.CommonLevies", "Viz.Troop.LevelShort", "Viz.Troop.HpLabel",
            "Viz.Troop.WageLabel", "Viz.Troop.CostLabel", "Viz.Troop.EquipmentLabel", "Viz.Troop.ArmorLabel",
            "Viz.Troop.HeadLabel", "Viz.Troop.BodyLabel", "Viz.Troop.LegLabel", "Viz.Troop.Upgrades",
            "Viz.Workshop.ScopeLabel", "Viz.Workshop.OptimalLabel", "Viz.Workshop.ProsperityLabel",
            "Viz.Workshop.CivicLoyaltyLabel", "Viz.Workshop.SecurityScoreLabel", "Viz.Workshop.FoodStorageLabel",
            "Viz.Workshop.GarrisonLabel", "Viz.Workshop.RebellionRiskLabel", "Viz.Workshop.StableLabel",
            "Viz.Workshop.RiskThresholdLabel", "Viz.CodeSecurity.TargetAssemblyLabel", "Viz.CodeSecurity.RuntimeClrLabel",
            "Viz.ModuleHierarchy.IdentifierLabel", "Viz.ModuleHierarchy.ReleaseTagLabel", "Viz.ModuleHierarchy.StageLabel",
            "Viz.ModuleHierarchy.UpstreamDependenciesLabel", "Viz.Diplomacy.RealmLabel", "Viz.Diplomacy.MonarchLabel",
            "Viz.Diplomacy.BorderConflictRiskLabel", "Viz.Component.IdentifierLabel", "Viz.Component.SchemaTargetLabel",
            "Viz.Component.GeneratedMembersLabel"
        };
        foreach (var key in localizedKeys)
            Check(templates.Contains(key, StringComparison.Ordinal), "Visualizer templates do not reference localized label " + key);
        foreach (var literal in new[]
        {
            "Text=\"12 ARCHETYPES · 0 CYCLES\"", "Text=\"TIER I · RECRUITS\"",
            "Text=\"TIER II-III · INFANTRY &amp; ARCHERS\"", "Text=\"TIER IV · ELITE SPECIALISTS\"",
            "Text=\"NOBLE LINE · CATAPHRACTS (T2-T6)\"", "Text=\"Scope: 30-Day Economic Amortization",
            "Text=\"Prosperity: 5,420", "Text=\"Civic Loyalty: 64.0", "Text=\"Security Score: 72.0",
            "Text=\"Food Storage: 184", "Text=\"Garrison: 165", "Text=\"REBELLION RISK: 8.6%\"",
            "Text=\"Stable · Risk threshold: 45%\"", "StringFormat='TARGET ASSEMBLY: {0}'",
            "StringFormat='RUNTIME CLR: {0}'", "StringFormat='MODULE IDENTIFIER: {0}'",
            "StringFormat='RELEASE TAG: {0}'", "StringFormat='Border Conflict Risk: {0}%'",
            "StringFormat='COMPONENT IDENTIFIER: {0}'", "StringFormat='SCHEMA TARGET: {0}'"
        })
            Check(!templates.Contains(literal, StringComparison.Ordinal), "A visualizer label is still embedded in XAML: " + literal);
        return Task.CompletedTask;
    }

    static Task DynamicThemeSurface()
    {
        string palette = ReadSourceText(FindDesktopFile("Resources/TacticalPalette.xaml"));
        string app = ReadSourceText(FindDesktopFile("App.xaml"));
        string service = ReadSourceText(FindDesktopFile("Services/DesktopThemeService.cs"));
        foreach (var token in new[] { "Palette.Coal", "Palette.DeepPine", "Palette.Tempered", "Palette.Brass", "Palette.Verdigris", "Palette.Ember" })
            Check(palette.Contains(token), "Tactical palette is missing " + token);
        var files = new[] { "Resources/TacticalPalette.xaml", "Resources/Themes/TacticalPalette.Parchment.xaml", "Resources/Themes/TacticalPalette.HighContrast.xaml" }
            .Select(FindDesktopFile).ToArray();
        foreach (var brush in new[]
        {
            "CoalBrush", "DeepPineBrush", "PineBrush", "TemperedBrush", "VerdigrisBrush",
            "BrassBrush", "QuietBrassBrush", "EmberBrush", "ParchmentBrush", "TextBrush",
            "MutedTextBrush", "SteelBorderBrush", "LogoTextBrush", "BorderBrush", "FocusBrush"
        })
            Check(files.All(file => ReadSourceText(file).Contains($"x:Key=\"{brush}\"")), "Every dynamic theme must own the " + brush + " resource.");
        Check(app.Contains("DynamicResource MutedTextBrush") && !app.Contains("x:Key=\"MutedTextBrush\""), "Text brushes must resolve from the active theme dictionary rather than a shadowing app-level brush.");
        Check(service.Contains("TacticalPalette.xaml") && service.Contains("MergedDictionaries") && !service.Contains("RefreshBrushResources"), "Theme service must replace the active palette dictionary without mutating global brushes.");
        var keySets = files.Select(file => LoadSourceXml(file).Descendants().Where(node => node.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")) != null)
            .Select(node => node.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")).Value).ToHashSet(StringComparer.Ordinal)).ToArray();
        Check(keySets.All(keys => keys.SetEquals(keySets[0])), "Theme dictionaries must expose identical resource keys");
        return Task.CompletedTask;
    }

    static Task ThemeCatalogSurface()
    {
        string service = ReadSourceText(FindDesktopFile("Services/DesktopThemeService.cs"));
        string shell = ReadSourceText(FindDesktopFile("Presentation/DesktopShellViewModel.cs"));
        string window = DesktopXamlSource;
        foreach (var id in new[] { "war-table", "parchment", "high-contrast" }) Check(service.Contains(id), "Theme catalog is missing " + id);
        Check(service.Contains("AvailableThemes") && service.Contains("ResourceDictionary") && service.Contains("UsedFallback"), "Theme service must expose dynamic dictionaries and fallback state");
        Check(shell.Contains("ApplyThemeCommand") && shell.Contains("PersistPreferences") && shell.Contains("ThemeId"), "Shell must expose theme command and selection state");
        Check(window.Contains("AvailableThemes") && window.Contains("ThemeDisplayConverter"), "Theme selector must be bound to the shell catalog");
        return Task.CompletedTask;
    }

    static Task PreferenceSurface()
    {
        string service = ReadSourceText(FindDesktopFile("Services/DesktopPreferenceService.cs"));
        Check(service.Contains("desktop-preferences.json") && service.Contains("File.Move(temporary, path, true)"), "Preferences must use an atomic replacement file");
        Check(service.Contains("UsedFallback") && service.Contains("JsonSerializer.Deserialize"), "Malformed preferences must fall back safely");
        Check(service.Contains("never writes game saves"), "Preferences must remain outside game save data");
        return Task.CompletedTask;
    }

    static Task MetricsSurface()
    {
        string metrics = ReadSourceText(FindDesktopFile("Services/DesktopMetricsService.cs"));
        string shell = ReadSourceText(FindDesktopFile("Presentation/DesktopShellViewModel.cs"));
        string workspace = ReadSourceText(FindDesktopFile("Services/DesktopWorkspaceService.cs"));
        Check(metrics.Contains("MaximumMeasurements = 64") && metrics.Contains("GC.GetAllocatedBytesForCurrentThread"), "Metrics must be bounded and measure real allocations");
        Check(metrics.Contains("CompleteAsync") && metrics.Contains("AllocatedBytes = null"), "Async metrics must not report a misleading thread-local allocation delta");
        Check(metrics.Contains("not measured (async thread-local counter)"), "Unavailable async allocations must be explicit in the rendered report");
        Check(metrics.Contains("Stopwatch") && metrics.Contains("not a game-wide performance attribution"), "Metrics must report durations with an explicit limit");
        foreach (var operation in new[] { "desktop-shell-initialize", "tool-filter-refresh", "tool-selection", "language:", "tool:" })
            Check(shell.Contains(operation) || workspace.Contains(operation), "Short operation measurement is missing " + operation);
        return Task.CompletedTask;
    }

    static async Task MetricsMeasurementSemantics()
    {
        var metrics = new DesktopMetricsService();
        byte[] allocation;
        using (var scope = metrics.Start("sync-probe", "test-only allocation check"))
        {
            allocation = new byte[4096];
            allocation[0] = 1;
        }

        var sync = metrics.Recent.Single();
        Check(sync.AllocatedBytes.HasValue && sync.AllocatedBytes.Value > 0, "Synchronous scopes must retain their measured allocation count");

        int callerThread = Environment.CurrentManagedThreadId;
        int workerThread = callerThread;
        int result = await metrics.MeasureAsync("async-thread-hop", () =>
            Task.Factory.StartNew(() =>
            {
                workerThread = Environment.CurrentManagedThreadId;
                _ = new byte[8192];
                Thread.Sleep(15);
                return 42;
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default), "test-only async duration check");

        Check(result == 42 && workerThread != callerThread, "Async probe must complete on a different thread");
        var async = metrics.Recent.Last();
        Check(async.ElapsedMilliseconds >= 10, "Async measurement must retain elapsed duration across the thread hop");
        Check(!async.AllocatedBytes.HasValue, "Async allocations must be unavailable when the operation crosses await/thread boundaries");
        Check(metrics.FormatRecent().Contains("not measured (async thread-local counter)"), "Formatted metrics must explain why async allocation bytes are unavailable");
        GC.KeepAlive(allocation);
    }

    static Task TemplateLocalizationSurface()
    {
        string template = ReadSourceText(FindDesktopFile("Services/DesktopTemplateLocalizer.cs"));
        string window = ReadSourceText(FindDesktopFile("MainWindow.xaml.cs"));
        Check(template.Contains("Dictionary<string, string> Headers") && template.Contains("Prefix(string languageCode, string template)"), "Generated templates require a parameterized localized header");
        Check(!template.Contains(".Replace("), "Template localization must not rewrite generated source with replace chains");
        Check(!window.Contains("TranslateScaffoldComments"), "Legacy scaffold replace-chain must be removed");
        return Task.CompletedTask;
    }

    static async Task AutoReconnectAndLastPid()
    {
        using var client = new PipeClient();
        Check(client.LastPid == 0, "Initial LastPid should be 0");
        Check(!client.Connected, "Should start disconnected");

        await using (var server1 = new Server())
        {
            bool connected = await client.TryAutoReconnect(server1.Id, 2000);
            Check(connected, "TryAutoReconnect to server1 failed");
            Check(client.Connected, "Client not marked connected");
            Check(client.LastPid == server1.Id, "LastPid does not match server1.Id");

            var res = await client.Send(new Request { Action = "test-1" });
            Check(res.Data == "test-1", "Data response mismatch on server1");
        }

        // Server 1 disposed.
        Check(!client.CheckHeartbeat() || !client.Connected, "Heartbeat should detect severed pipe or disconnect");

        await using (var server2 = new Server())
        {
            bool reconnected = await client.TryAutoReconnect(server2.Id, 2000);
            Check(reconnected, "TryAutoReconnect to server2 failed");
            Check(client.Connected, "Client not marked connected to server2");
            Check(client.LastPid == server2.Id, "LastPid does not match server2.Id");

            var res2 = await client.Send(new Request { Action = "test-2" });
            Check(res2.Data == "test-2", "Data response mismatch on server2");
        }
    }

    static async Task HeartbeatState()
    {
        using var client = new PipeClient();
        Check(!client.CheckHeartbeat(), "Disconnected client should return false for heartbeat");

        await using (var server = new Server())
        {
            await client.Connect(server.Id);
            Check(client.CheckHeartbeat(), "Connected client should return true for heartbeat");
        }
        // Once server is closed, CheckHeartbeat should detect disconnection
        Check(!client.CheckHeartbeat(), "Severed pipe should return false for heartbeat");
        Check(!client.Connected, "Client should be disconnected after heartbeat failure");
    }

    static string HelloData()=>Json.Serialize(new[]{"protocol:1","summary","report","framework","event-journal","harmony","patch-preflight"});

    // Real named pipes, with a synthetic peer: this exercises the production desktop
    // client without claiming a WPF interaction test or a Bannerlord engine run.
    sealed class Server : IAsyncDisposable
    {
        public int Id { get; } = Random.Shared.Next(100000000, int.MaxValue);
        readonly NamedPipeServerStream pipe;
        readonly CancellationTokenSource stop = new CancellationTokenSource();
        readonly Task task;
        readonly ConcurrentDictionary<string, TaskCompletionSource<Request>> observed = new(StringComparer.Ordinal);
        public Server(Func<Request,Response> respond = null)
        {
            pipe = new NamedPipeServerStream("CalradiaForge-"+Id, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            task = Serve(respond ?? (request => new Response { Id=request.Id, Success=true, Data=request.Action=="hello"?HelloData():request.Action }));
        }
        async Task Serve(Func<Request,Response> respond)
        {
            try {
                await pipe.WaitForConnectionAsync(stop.Token);
                using var reader = new StreamReader(pipe, new UTF8Encoding(false), false, 4096, true);
                using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true) { AutoFlush=true };
                while (!stop.IsCancellationRequested) {
                    var line = await reader.ReadLineAsync(stop.Token);
                    if (line == null) break;
                    var request = Json.Deserialize<Request>(line);
                    observed.GetOrAdd(request.Action, static _ => new TaskCompletionSource<Request>(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult(request);
                    var response = respond(request);
                    if (response != null) await writer.WriteLineAsync(Json.Serialize(response));
                }
            } catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
              catch (ObjectDisposedException) when (stop.IsCancellationRequested) { }
              catch (IOException) { /* A rejected handshake closes the client endpoint. */ }
        }
        public async ValueTask DisposeAsync()
        {
            stop.Cancel(); pipe.Dispose();
            try { await task.WaitAsync(TimeSpan.FromSeconds(3)); }
            finally { stop.Dispose(); }
        }

        public Task<Request> WaitForRequest(string action) => observed.GetOrAdd(action, static _ => new TaskCompletionSource<Request>(TaskCreationOptions.RunContinuationsAsynchronously)).Task;
    }

    static string FindDesktopFile(string relativePath)
    {
        string normalizedRelativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        return desktopFilePathCache.GetOrAdd(normalizedRelativePath, static relative => new Lazy<string>(
            () => ResolveDesktopFile(relative), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    static string ResolveDesktopFile(string relativePath)
    {
        string[] candidates = new[]
        {
            Path.GetFullPath(Path.Combine("src/CalradiaForge.Desktop", relativePath)),
            Path.GetFullPath(Path.Combine("../../../../../src/CalradiaForge.Desktop", relativePath)),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../src/CalradiaForge.Desktop", relativePath)),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/CalradiaForge.Desktop", relativePath)),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../src/CalradiaForge.Desktop", relativePath)),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../src/CalradiaForge.Desktop", relativePath))
        };
        return candidates.FirstOrDefault(File.Exists) ?? throw new FileNotFoundException($"Could not locate {relativePath}");
    }

    static string WorkspaceRoot()
    {
        var project = new DirectoryInfo(Path.GetDirectoryName(FindDesktopFile("CalradiaForge.Desktop.csproj")));
        return project.Parent?.Parent?.FullName ?? Directory.GetCurrentDirectory();
    }

    static async Task DesktopVersionBlockSynchronization()
    {
        string xaml = DesktopXamlSource;
        string code = ReadSourceText(FindDesktopFile("MainWindow.xaml.cs"));
        string props = ReadSourceText(Path.GetFullPath("Directory.Build.props"));
        Check(xaml.Contains("Ui.AppTitle") && code.Contains("DesktopShellViewModel"), "Desktop chrome must bind to the routed workbench");
        Check(props.Contains("<CalradiaForgeVersion>25.2.0</CalradiaForgeVersion>"), "Release version must be centrally defined");
        Check(SuiteInfo.Version == "25.2.0", "SuiteInfo.Version must equal 25.2.0");
        await Task.CompletedTask;
    }

    static async Task BoundedIpcResponses()
    {
        var exact = new BoundedLineReader(new StringReader("1234\r\nnext\nlast\r"), 4, 2);
        Check(await exact.ReadLineAsync(CancellationToken.None) == "1234", "A line exactly at the character cap was rejected.");
        Check(await exact.ReadLineAsync(CancellationToken.None) == "next", "The buffered next frame was lost.");
        Check(await exact.ReadLineAsync(CancellationToken.None) == "last", "A final CR-terminated frame was not returned.");
        Check(await exact.ReadLineAsync(CancellationToken.None) == null, "EOF should return null after the final frame.");

        var overLimitSource = new CountingTextReader("12345\nsecond-frame\n");
        var bounded = new BoundedLineReader(overLimitSource, 4, 2);
        try
        {
            await bounded.ReadLineAsync(CancellationToken.None);
            throw new Exception("An oversized frame was accepted.");
        }
        catch (IOException) { }
        Check(overLimitSource.CharactersRead <= 6, "The bounded reader consumed an unbounded response before rejecting it.");

        using (var cancelled = new CancellationTokenSource())
        {
            cancelled.Cancel();
            try
            {
                await new BoundedLineReader(new StringReader("response\n"), 64, 4).ReadLineAsync(cancelled.Token);
                throw new Exception("A pre-cancelled read completed.");
            }
            catch (OperationCanceledException) { }
        }

        await using var server = new Server(request => new Response
        {
            Id = request.Id,
            Success = true,
            Data = request.Action == "hello" ? HelloData() : new string('x', 8192)
        });
        using var client = new PipeClient(2048);
        await client.Connect(server.Id);
        try
        {
            await client.Send(new Request { Action = "oversized" });
            throw new Exception("The oversized named-pipe response was accepted.");
        }
        catch (IOException) { }
        Check(!client.Connected, "An oversized response must discard the now-unsynchronized connection.");
    }

    static async Task ReportExportSafety()
    {
        var root = Path.Combine(Path.GetTempPath(), "CalradiaForge-Desktop-ExportTests-" + Guid.NewGuid().ToString("N"));
        var timestamp = new DateTimeOffset(2026, 9, 27, 12, 34, 56, TimeSpan.Zero);
        try
        {
            var service = new DesktopReportExportService(root, () => timestamp);
            var reports = await Task.WhenAll(
                service.ExportTextAsync("desktop-evidence", "first report", CancellationToken.None),
                service.ExportTextAsync("desktop-evidence", "second report", CancellationToken.None));
            var first = reports[0];
            var second = reports[1];
            Check(first.Status == ReportExportStatus.Succeeded && second.Status == ReportExportStatus.Succeeded, "Concurrent-timestamp reports did not export successfully.");
            Check(first.Path != second.Path && File.ReadAllText(first.Path) == "first report" && File.ReadAllText(second.Path) == "second report", "Reports collided or an earlier report was overwritten.");

            var preserved = Path.Combine(Path.GetTempPath(), "CalradiaForge-Desktop-ExportSentinel-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(preserved, "keep-existing-file");
            try
            {
                var failing = await new DesktopReportExportService(preserved, () => timestamp)
                    .ExportTextAsync("desktop-evidence", "must not replace", CancellationToken.None);
                Check(failing.Status == ReportExportStatus.Failed, "An invalid output destination was not reported as failure.");
                Check(File.ReadAllText(preserved) == "keep-existing-file", "A pre-existing destination was changed after an export failure.");
            }
            finally { File.Delete(preserved); }

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var cancelled = await service.ExportTextAsync("desktop-evidence", "cancelled", cancellation.Token);
            Check(cancelled.Status == ReportExportStatus.Cancelled, "Export cancellation was not returned explicitly.");
            Check(Directory.GetFiles(root, "*.tmp", SearchOption.TopDirectoryOnly).Length == 0, "An incomplete temporary report was left behind.");

            using var midWriteCancellation = new CancellationTokenSource();
            var cancelledDuringWrite = await service.ExportLinesAsync("desktop-partial", CancelAfterFirstLine(midWriteCancellation), midWriteCancellation.Token);
            Check(cancelledDuringWrite.Status == ReportExportStatus.Cancelled, "Cancellation after a partial write was not returned explicitly.");
            Check(Directory.GetFiles(root, "*.tmp", SearchOption.TopDirectoryOnly).Length == 0, "A cancelled partial report left its temporary file behind.");

            var reportCountBeforeFailure = Directory.GetFiles(root, "*.txt", SearchOption.TopDirectoryOnly).Length;
            var failedDuringWrite = await service.ExportLinesAsync("desktop-partial", FailAfterFirstLine(), CancellationToken.None);
            Check(failedDuringWrite.Status == ReportExportStatus.Failed, "A write failure after partial output was not captured.");
            Check(Directory.GetFiles(root, "*.tmp", SearchOption.TopDirectoryOnly).Length == 0 &&
                  Directory.GetFiles(root, "*.txt", SearchOption.TopDirectoryOnly).Length == reportCountBeforeFailure,
                "A failed partial report was not cleaned up or replaced a prior report.");

            var lines = await service.ExportLinesAsync("desktop-ledger", new[] { "source | verified | evidence" }, CancellationToken.None);
            Check(lines.Status == ReportExportStatus.Succeeded && File.ReadAllText(lines.Path).TrimEnd() == "source | verified | evidence", "Ledger export changed its established line format.");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    static IEnumerable<string> CancelAfterFirstLine(CancellationTokenSource cancellation)
    {
        yield return "partial data";
        cancellation.Cancel();
        yield return "must not be written";
    }

    static IEnumerable<string> FailAfterFirstLine()
    {
        yield return "partial data";
        throw new IOException("synthetic write-stream failure");
    }

    static Task DossierCopyAccessibilitySurface()
    {
        var xaml = ReadSourceText(Desktop("Presentation/ToolDossierControl.xaml"));
        var codeBehind = ReadSourceText(Desktop("Presentation/ToolDossierControl.xaml.cs"));
        var pageSource = ReadSourceText(Desktop("Presentation/WorkspacePageViewModel.cs"));
        Check(xaml.Contains("{Binding AutomationId}") && pageSource.Contains("CopyConsoleCommandButton") && xaml.Contains("CopyCliSyntaxButton") &&
              xaml.Contains("{Binding AccessibleName}") && pageSource.Contains("Ui.CopyCommandAccessibleNameFormat") &&
              xaml.Contains("Ui.CopyCliAccessibleName") &&
              xaml.Contains("ClipboardCopyFeedback") && xaml.Contains("AutomationProperties.LiveSetting=\"Polite\""),
            "Dossier copy controls need unique automation IDs, localized names, and accessible failure feedback.");
        Check(xaml.Contains("DataContext.CopyTextCommand") && xaml.Contains("CommandParameter=\"{Binding Text}\"") &&
              xaml.Contains("CommandParameter=\"{Binding CliSyntax}\"") && !xaml.Contains("Click=\"CopyCommandClick\"") &&
              !codeBehind.Contains("Clipboard.SetText"), "Clipboard behavior must use a page command rather than a silent code-behind click.");
        Check(pageSource.Contains("CopyFeedback") && pageSource.Contains("Ui.ClipboardCopyFailed") &&
              pageSource.Contains("Ui.ClipboardCopySucceeded") && pageSource.Contains("CopyableConsoleCommand") &&
              pageSource.Contains("Ui.CopyCommandAccessibleNameFormat") && pageSource.Contains("AccessibleName = string.Format") &&
              pageSource.Contains("CopyFeedback = copied") && pageSource.Contains("clipboardWriter(text)"),
            "The page ViewModel must expose localized clipboard feedback, failure handling, and command-specific copy identities.");
        return Task.CompletedTask;
    }

    sealed class CountingTextReader : StringReader
    {
        internal CountingTextReader(string value) : base(value) { }
        internal int CharactersRead { get; private set; }
        public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = base.Read(buffer.Span);
            CharactersRead += result;
            return ValueTask.FromResult(result);
        }
    }

    static async Task TacticalStudioEnrichmentAndRolePresets()
    {
        var studios = string.Join("\n", new[]
        {
            "Presentation/DesktopSimulationViewModels.cs",
            "Presentation/DesktopSimulationViewModels.Operations.cs",
            "Presentation/DesktopSimulationViewModels.Security.cs",
            "Presentation/DesktopSimulationViewModels.Workshop.cs"
        }.Select(path => ReadSourceText(Desktop(path))));
        var shell = ReadSourceText(Desktop("Presentation/DesktopShellViewModel.cs"));

        Check(studios.Contains("class StudioConsoleCommand"), "StudioConsoleCommand must be defined");
        Check(studios.Contains("StudioDocumentation") && studios.Contains("ArchitecturalInvariants") && studios.Contains("CuratedConsoleCommands"), "Studio documentation, invariants, and commands must be present");

        var expectedStudios = new[]
        {
            "TroopTreeDashboardViewModel",
            "AudioStudioDashboardViewModel",
            "WorkshopDashboardViewModel",
            "AgentMemoryDashboardViewModel",
            "CodeSecurityDashboardViewModel",
            "ModuleHierarchyDashboardViewModel",
            "KingdomDiplomacyDashboardViewModel",
            "ComponentGeneratorDashboardViewModel"
        };
        foreach (var studio in expectedStudios)
        {
            Check(studios.Contains($"class {studio}"), $"Studio ViewModel missing: {studio}");
        }

        Check(shell.Contains("public enum ModderRolePreset"), "ModderRolePreset enum must be declared in shell");
        Check(shell.Contains("ActiveModderRole") && shell.Contains("CycleModderRoleCommand"), "Shell must expose ActiveModderRole and CycleModderRoleCommand");
        Check(shell.Contains("MatchesModderRole"), "Shell must filter routes using MatchesModderRole");
        foreach (var role in new[] { "All", "NarrativeDialogues", "TroopCombatArtisan", "EconomyWorldArchitect", "CoreDevPerformanceAuditor" })
        {
            Check(shell.Contains(role), $"ModderRolePreset missing role {role}");
        }

        await Task.CompletedTask;
    }


}




