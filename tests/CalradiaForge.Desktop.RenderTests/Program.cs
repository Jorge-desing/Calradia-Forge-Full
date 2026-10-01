using System;
using System.Collections;
using System.Collections.Generic;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Threading;
using CalradiaForge.Desktop;
using CalradiaForge.Desktop.Presentation;
using CalradiaForge.Desktop.Services;

internal static class Program
{
    static readonly Dictionary<Window, DependencyObject[]> visualTreeSnapshots = new Dictionary<Window, DependencyObject[]>();
    static int renderPassCount;
    static double renderLayoutMilliseconds;
    static readonly List<double> renderLayoutSamples = new List<double>();
    static int visualTreeSnapshotBuilds;
    static int visualTreeSnapshotHits;
    static int visualTreeNodesVisited;
    static ForegroundWindowGuard foregroundWindowGuard;

    [STAThread]
    static int Main(string[] args)
    {
        var exitCode = 1;
        Exception startupFailure = null;
        var renderThread = new Thread(() =>
        {
            try
            {
                IsolatedRenderDesktop.AttachCurrentThread();
                exitCode = RunRenderTests(args);
            }
            catch (Exception error)
            {
                startupFailure = error;
            }
        });
        renderThread.SetApartmentState(ApartmentState.STA);
        renderThread.Start();
        renderThread.Join();
        if (startupFailure != null)
        {
            Console.Error.WriteLine(startupFailure);
            return 1;
        }
        return exitCode;
    }

    static int RunRenderTests(string[] args)
    {
        using var guard = (foregroundWindowGuard = ForegroundWindowGuard.Capture());
        var timer = Stopwatch.StartNew();
        var phaseTimer = Stopwatch.StartNew();
        var phaseTimings = new Dictionary<string, double>(StringComparer.Ordinal);
        var records = new List<object>();
        MainWindow window = null;
        App app = null;
        PreferenceSnapshot preferenceSnapshot = null;
        var exitCode = 1;
        var output = args.FirstOrDefault() ?? "artifacts/desktop-render-tests.json";
        try
        {
        Check(string.Equals(Environment.GetEnvironmentVariable("CALRADIA_FORGE_RENDER_NO_ACTIVATE"), "1", StringComparison.Ordinal),
            "Run the WPF render harness through Run-CalradiaForge-Desktop-Render-Tests.bat so its no-activation contract is enabled.");
        Check(IsolatedRenderDesktop.IsCurrentThreadAttached,
            "WPF render windows must be created on a private Windows desktop, separate from the user's interactive desktop.");
        RunInputPickerTests(records);
        RunAsyncPageDisposalTests(records);
        RunAssemblyWorkbenchTests(records);
            RunDeclarativeCatalogTests(records);
            DesktopSimulationServiceTests.RunAll();
            records.Add(new { test = "desktop-simulation-and-split-deck-suite", passed = true });
            phaseTimings["nonvisual-fixtures-and-contracts"] = phaseTimer.Elapsed.TotalMilliseconds;
            phaseTimer.Restart();
            app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();
            AssertPackagedTextureResources();
            HookWorkbenchUtilityTests.Run();
            records.Add(new { test = "hook-workbench-metadata-filters-verification", passed = true });
            records.Add(new { test = "desktop-illustrated-textures-are-local-packaged-pngs", resources = 15, passed = true });
            var semanticGameIcons = new[] { "GameIcon.gears", "GameIcon.gear_hammer", "GameIcon.scroll_unfurled", "GameIcon.magnifying_glass", "GameIcon.stopwatch" };
            foreach (var iconKey in semanticGameIcons)
                Check(app.TryFindResource(iconKey) is Geometry, "Open-source Game-icon geometry is missing: " + iconKey);
            var semanticGroups = new Dictionary<string, string>
            {
                ["Assets"] = "GameIcon.gears",
                ["Learning"] = "GameIcon.scroll_unfurled",
                ["Gauntlet"] = "GameIcon.gear_hammer"
            };
            foreach (var entry in semanticGroups)
                Check(new ToolGroupViewModel(entry.Key, Array.Empty<ToolDefinition>()).GroupIconKey == entry.Value,
                    "Workbench group icon mapping is not semantic for " + entry.Key);
            records.Add(new { test = "game-icons-resources-and-semantic-group-mapping", passed = true, icons = semanticGameIcons.Length });
            window = new MainWindow { ShowInTaskbar = false, ShowActivated = false, Left = -10000, Top = -10000 };
            IsolatedRenderDesktop.AllowFixedViewport(window);
            NonActivatingWindowBehavior.Apply(window);
            window.Show();
            IsolatedRenderDesktop.VerifySmallDisplayBounds(window);
            PrepareFixedDipViewport(window);
            records.Add(new { test = "isolated-fixed-viewport-survives-small-native-display", passed = true });
            Check(NonActivatingWindowBehavior.HasNoActivateStyle(window),
                "The off-screen render window must carry WS_EX_NOACTIVATE before rendering starts.");
            var shell = window.DataContext;
            var shellType = shell.GetType();
            var preferences = (DesktopPreferenceService)shellType.GetField("preferences", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(shell);
            Check(preferences != null, "Render harness could not locate the Desktop preference store.");
            preferenceSnapshot = PreferenceSnapshot.Capture(preferences.Path);
            SetPresentationForRender(app, shell, "war-table", "en");
            RefreshSelectorBindings(window);
            var languageResources = app.Resources.MergedDictionaries.Where(item => item.Source?.OriginalString.IndexOf("Strings.", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            Check(languageResources.Length == 1 && languageResources[0].Source.OriginalString.EndsWith("Strings.en.xaml", StringComparison.OrdinalIgnoreCase),
                "English startup must reuse one fallback catalog instead of merging duplicate language dictionaries.");
            Render(window);
            AssertHookWorkbenchShellIntegration(app, window, shell, Path.GetDirectoryName(Path.GetFullPath(output)) ?? ".");
            records.Add(new { test = "hook-workbench-shell-visibility-and-responsive-surface", locales = 13, themes = 3, viewport = "980x680 DIP plus 1360x820 DIP", passed = true });
            AssertHeaderActionSizing(window);
            records.Add(new { test = "header-actions-consistent-activation-size", passed = true });
            AssertStatusCardsHaveClearLabelsAndPassiveIcons(window, app);
            records.Add(new { test = "status-ledger-hierarchy-and-passive-icons", passed = true });
            var surface = (FrameworkElement)window.Content;
            var preview = new RenderTargetBitmap((int)Math.Ceiling(surface.ActualWidth), (int)Math.Ceiling(surface.ActualHeight), 96, 96, PixelFormats.Pbgra32);
            preview.Render(surface);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(preview));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            using (var stream = File.Create(Path.ChangeExtension(output, ".png"))) encoder.Save(stream);
            var standardPreviewPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output)) ?? ".", "desktop-render-tests-en-100-current.png");
            var initialPreviewPath = Path.ChangeExtension(output, ".png");
            if (!string.Equals(Path.GetFullPath(initialPreviewPath), Path.GetFullPath(standardPreviewPath), StringComparison.OrdinalIgnoreCase))
                File.Copy(initialPreviewPath, standardPreviewPath, overwrite: true);
            phaseTimings["app-startup-and-first-render"] = phaseTimer.Elapsed.TotalMilliseconds;
            phaseTimer.Restart();
            var catalog = shellType.GetProperty("Catalog").GetValue(shell);
            var tools = (IEnumerable)catalog.GetType().GetProperty("Tools").GetValue(catalog);
            var toolArray = tools.Cast<object>().ToArray();
            Check(toolArray.Length == 194, $"The Desktop route catalog must contain all 194 routes; actual count: {toolArray.Length}.");
            var shellMetrics = (DesktopMetricsService)shellType.GetField("metrics", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(shell);
            Check(shellMetrics != null, "Render harness could not locate Desktop shell performance metrics.");
            var routeSelectionMeasurements = new List<DesktopMeasurement>(toolArray.Length);
            var filterMeasurements = new List<DesktopMeasurement>();
            var pageHost = Descendants(window).OfType<ContentControl>().SingleOrDefault(control =>
                BindingOperations.GetBinding(control, ContentControl.ContentProperty)?.Path?.Path == "CurrentPage");
            Check(pageHost != null, "The rendered workbench host must bind Content to CurrentPage.");
            // Move once before the catalog walk so every one of the 194 catalog entries
            // exercises a real route transition, including the initially selected entry.
            shellType.GetProperty("SelectedTool").SetValue(shell, toolArray[1]);
            foreach (var tool in toolArray)
            {
                var previousPage = shellType.GetProperty("CurrentPage").GetValue(shell);
                shellType.GetProperty("SelectedTool").SetValue(shell, tool);
                window.Dispatcher.Invoke(() => pageHost.GetBindingExpression(ContentControl.ContentProperty)?.UpdateTarget(), DispatcherPriority.DataBind);
                var selectionMeasurement = shellMetrics.Recent.LastOrDefault(item => item.Operation == "tool-selection");
                Check(selectionMeasurement != null, "Tool route selection did not produce its synchronous Desktop metric.");
                routeSelectionMeasurements.Add(selectionMeasurement);
                Check(ReferenceEquals(shellType.GetProperty("SelectedTool").GetValue(shell), tool), "Selecting a catalog route did not retain the selected route.");
                var page = shellType.GetProperty("CurrentPage").GetValue(shell);
                Check(page != null && ReferenceEquals(page.GetType().GetProperty("Tool").GetValue(page), tool),
                    "The selected route did not create a page bound to its ToolDefinition: " + tool.GetType().GetProperty("Id").GetValue(tool));
                Check(ReferenceEquals(pageHost.Content, page), "The CurrentPage ContentControl binding did not follow the selected route.");
                Check(string.Equals((string)previousPage.GetType().GetProperty("DisabledReason").GetValue(previousPage),
                        "This page has been released.", StringComparison.Ordinal),
                    "Navigating to a new route must release the previous page and its transient state.");
                var iconKey = (string)tool.GetType().GetProperty("IconKey").GetValue(tool);
                Check(app.TryFindResource(iconKey) is Geometry, "Tool route maps to a missing Game-icons resource: " + iconKey);
                records.Add(new { test = "render-route", route = tool.GetType().GetProperty("Id").GetValue(tool), passed = true });
            }
            // All routes share the same WorkbenchPageViewModel template. Exercise its real rendered
            // one-way evidence binding once; the route loop above still verifies selection, page
            // binding, and icon coverage for every catalog entry without repeating full layout.
            var renderedPage = shellType.GetProperty("CurrentPage").GetValue(shell);
            var reportTabs = Descendants(window).OfType<TabControl>().Single(control =>
                string.Equals(AutomationProperties.GetAutomationId(control), "WorkbenchReportTabs", StringComparison.Ordinal));
            var evidenceTab = reportTabs.Items.OfType<TabItem>().Single(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "WorkbenchEvidenceTab", StringComparison.Ordinal));
            var rawResultTab = reportTabs.Items.OfType<TabItem>().Single(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "WorkbenchRawResultTab", StringComparison.Ordinal));
            Check(ReferenceEquals(reportTabs.SelectedItem, evidenceTab),
                "The report must open on the Evidence tab by default.");
            Check(evidenceTab.Focusable && evidenceTab.IsTabStop && rawResultTab.Focusable && rawResultTab.IsTabStop &&
                  AutomationProperties.GetName(evidenceTab) == (app.TryFindResource("Ui.Evidence") as string) &&
                  AutomationProperties.GetName(rawResultTab) == (app.TryFindResource("Ui.RawResult") as string),
                "Both report tabs must expose localized, keyboard-focusable accessible names.");
            FocusManager.SetFocusedElement(window, evidenceTab);
            Check(ReferenceEquals(FocusManager.GetFocusedElement(window), evidenceTab),
                "The non-activating synthetic report host must retain logical focus on the Evidence tab.");
            reportTabs.SelectedItem = rawResultTab;
            FocusManager.SetFocusedElement(window, rawResultTab);
            Render(window);
            Check(ReferenceEquals(reportTabs.SelectedItem, rawResultTab) && rawResultTab.IsVisible &&
                  ReferenceEquals(FocusManager.GetFocusedElement(window), rawResultTab),
                "Raw Result tab selection must update the visible view and logical focus without activating the host.");
            var renderedEvidenceProperty = renderedPage.GetType().GetProperty("RawResult");
            const string evidence = "Hero_1 C:\\Mods\\日本語 <raw>";
            renderedEvidenceProperty.SetValue(renderedPage, evidence);
            Render(window);
            var raw = Descendants(window).OfType<TextBox>().Single(control =>
                string.Equals(AutomationProperties.GetAutomationId(control), "RawResultText", StringComparison.Ordinal));
            var rawBinding = BindingOperations.GetBinding(raw, TextBox.TextProperty);
            Check(raw.IsVisible && raw.IsReadOnly && rawBinding?.Path?.Path == "RawResult" && rawBinding.Mode == BindingMode.OneWay,
                "The keyboard-selected Raw Result tab must expose an explicit visible one-way binding.");
            Check(raw.Text == evidence, "Raw result did not refresh after a source notification.");
            const string refreshedEvidence = "Updated evidence after notification";
            renderedEvidenceProperty.SetValue(renderedPage, refreshedEvidence);
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            Check(raw.Text == refreshedEvidence, "Raw result did not refresh after a source notification.");
            raw.SetCurrentValue(TextBox.TextProperty, "local selection test");
            raw.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            Check((string)renderedEvidenceProperty.GetValue(renderedPage) == refreshedEvidence, "Output control wrote back into the result.");
            reportTabs.SelectedItem = evidenceTab;
            FocusManager.SetFocusedElement(window, evidenceTab);
            Render(window);
            var evidenceCard = Descendants(window).OfType<Border>().SingleOrDefault(border =>
                string.Equals(AutomationProperties.GetAutomationId(border), "EvidenceLedgerCard", StringComparison.Ordinal));
            Check(ReferenceEquals(reportTabs.SelectedItem, evidenceTab) && evidenceTab.IsVisible &&
                  evidenceCard?.IsVisible == true && evidenceCard.ActualWidth > 0 && evidenceCard.ActualHeight > 0,
                "Evidence tab selection must return to the visible ledger without activating the host.");
            var technicalEvidenceLabel = app.TryFindResource("Ui.TechnicalEvidence") as string;
            Check(!string.IsNullOrWhiteSpace(technicalEvidenceLabel) && Descendants(window).OfType<TextBlock>().Any(block =>
                    block.IsVisible && block.ActualWidth > 0 && block.ActualHeight > 0 &&
                    string.Equals(block.Text, technicalEvidenceLabel, StringComparison.Ordinal)),
                "The localized Ui.TechnicalEvidence label must render when selection returns to the Evidence tab.");
            records.Add(new { test = "report-tabs-logical-selection-and-lazy-views", evidenceDefault = true, rawResultOneWay = true, keyboardFocusMetadata = true, systemKeyboardInput = "not-simulated-to-preserve-foreground", passed = true });

            // Report ViewModel work separately from full WPF layout/render cost. These observations
            // are diagnostic only; timing varies by host and never gates the suite.
            var originalFilter = (string)shellType.GetProperty("Filter").GetValue(shell);
            var firstRouteId = ((ToolDefinition)toolArray[0]).Id;
            var quickQuery = firstRouteId.Substring(0, Math.Min(3, firstRouteId.Length));
            shellType.GetProperty("Filter").SetValue(shell, quickQuery);
            filterMeasurements.Add(shellMetrics.Recent.Last(item => item.Operation == "tool-filter-refresh"));
            var quickFilterCount = ((IEnumerable)shellType.GetProperty("VisibleTools").GetValue(shell)).Cast<object>().Count();
            Check(quickFilterCount > 0, "A short route-prefix filter must preserve matching tools.");
            shellType.GetProperty("Filter").SetValue(shell, "zz-no-route-match");
            window.Dispatcher.Invoke(() => pageHost.GetBindingExpression(ContentControl.ContentProperty)?.UpdateTarget(), DispatcherPriority.DataBind);
            filterMeasurements.Add(shellMetrics.Recent.Last(item => item.Operation == "tool-filter-refresh"));
            Check(((IEnumerable)shellType.GetProperty("VisibleTools").GetValue(shell)).Cast<object>().Count() == 0,
                "An unmatched quick filter must produce the empty tool state.");
            Check(shellType.GetProperty("SelectedTool").GetValue(shell) == null &&
                  shellType.GetProperty("CurrentPage").GetValue(shell) == null && pageHost.Content == null,
                "An empty filter must clear selection and release the page that was previously visible.");
            shellType.GetProperty("Filter").SetValue(shell, string.Empty);
            window.Dispatcher.Invoke(() => pageHost.GetBindingExpression(ContentControl.ContentProperty)?.UpdateTarget(), DispatcherPriority.DataBind);
            filterMeasurements.Add(shellMetrics.Recent.Last(item => item.Operation == "tool-filter-refresh"));
            Check(((IEnumerable)shellType.GetProperty("VisibleTools").GetValue(shell)).Cast<object>().Count() == toolArray.Length,
                "Clearing the filter must restore all catalog routes.");
            Check(shellType.GetProperty("SelectedTool").GetValue(shell) != null &&
                  shellType.GetProperty("CurrentPage").GetValue(shell) != null && pageHost.Content != null,
                "Clearing the filter must restore a selected workbench page.");
            AssertClearFilterControl(app, window, shell, pageHost, toolArray);
            records.Add(new { test = "localized-clear-filter-restores-catalog-and-focus", passed = true });
            foreach (var query in new[] { "assets", "texture", "module", "ui", "reference" })
            {
                shellType.GetProperty("Filter").SetValue(shell, query);
                var filterMeasurement = shellMetrics.Recent.LastOrDefault(item => item.Operation == "tool-filter-refresh");
                Check(filterMeasurement != null, "Tool filtering did not produce its synchronous Desktop metric.");
                filterMeasurements.Add(filterMeasurement);
            }
            shellType.GetProperty("Filter").SetValue(shell, originalFilter ?? string.Empty);
            shellType.GetProperty("SelectedTool").SetValue(shell, toolArray[0]);
            records.Add(new
            {
                test = "desktop-shell-synchronous-performance-observations",
                thresholdMilliseconds = 16.7,
                filterRefresh = SummarizeShellMetrics(filterMeasurements),
                routeSelection = SummarizeShellMetrics(routeSelectionMeasurements),
                note = "ViewModel synchronous measurements only; WPF layout/render time is reported separately.",
                passed = true
            });
            records.Add(new { test = "quick-and-empty-tool-filtering", quickMatches = quickFilterCount, restoredCount = toolArray.Length, emptyState = true, passed = true });
            phaseTimings["all-route-navigation-and-filter-checks"] = phaseTimer.Elapsed.TotalMilliseconds;
            phaseTimer.Restart();

            // Regression: the recent history is bounded in the ViewModel and has its own viewport.
            foreach (var tool in toolArray.Take(20)) shellType.GetProperty("SelectedTool").SetValue(shell, tool);
            Render(window);
            var recent = (IEnumerable)shellType.GetProperty("RecentTools").GetValue(shell);
            Check(recent.Cast<object>().Count() <= 12, "Recent tools must remain bounded to twelve entries.");
            var railVirtualization = AssertOperationalRailVirtualization(app, window, shell);
            records.Add(new { test = "operational-rail-virtualization", passed = true, details = railVirtualization });
            records.Add(new { test = "recent-history-viewport", recentCount = recent.Cast<object>().Count(), passed = true });
            foreach (var language in new[] { "en", "es", "pt", "de", "fr", "it", "pl", "ru", "tr", "zh-HANS", "zh-HANT", "ja", "ko" })
            {
                SetPresentationForRender(app, shell, "war-table", language);
                RefreshSelectorBindings(window);
                var activeStrings = app.Resources.MergedDictionaries.Where(item => item.Source?.OriginalString.IndexOf("Strings.", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                Check(activeStrings.Length == (language == "en" ? 1 : 2), "Language selection accumulated duplicate Desktop dictionaries: " + language);
                for (var layoutPass = 0; layoutPass < 4; layoutPass++)
                {
                PrepareFixedDipViewport(window);
                var minimumWindowCase = language == "en" && layoutPass == 0;
                if (minimumWindowCase)
                {
                    window.Width = window.MinWidth;
                    window.Height = window.MinHeight;
                }
                shellType.GetProperty("SelectedTool").SetValue(shell, toolArray[0]);
                ClearEvidenceForEmptyRenderCase(shell);
                Render(window);
                if (layoutPass == 0 && (language == "es" || language == "ja"))
                {
                    AssertVisualizerLocalization(app, window, shell, toolArray.Cast<ToolDefinition>().ToArray(), language);
                    records.Add(new { test = "localized-specialist-visualizer-headings", language, routes = 2, passed = true });
                }
                if (minimumWindowCase) AssertApplicationStatusWrapsAtMinimum(window);
                if (language == "en" && layoutPass == 0)
                    SavePreview(window, Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output)) ?? ".", "desktop-render-tests-en-minimum-current.png"), 1.0);
                if (language == "en" && layoutPass == 3)
                    SavePreview(window, Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output)) ?? ".", "desktop-render-tests-en-200-current.png"), 2.0);
                AssertTitleBarAccessibleNames(app, window, language);
                AssertHeaderSealAccess(app, window, shell, language);
                AssertDecorativeAccentsToggleAccess(app, window, shell, language);
                AssertToolPageDecorationAccess(window, language);
                AssertEmptyEvidenceState(app, window, shell, language);
                if (layoutPass == 0)
                    AssertPinnedDeckFallbackLocalization(app, shell, language);
                if (language == "en" && layoutPass == 0)
                {
                    AssertEmptyLedgerFilledTransition(window, shell);
                    AssertEmptyLedgerOpenPaletteAction(window, shell);
                }
                AssertLongLocalizedTextLayout(app, window, language);
                AssertHeaderControlBounds(window, app, language);
                if (minimumWindowCase)
                {
                    Check(Math.Abs(window.ActualWidth - window.MinWidth) < 1 && Math.Abs(window.ActualHeight - window.MinHeight) < 1,
                        "The minimum-window regression must render at the actual 980x680-DIP application viewport.");
                    AssertCompactHeaderAtMinimum(window);
                    AssertFooterFitsShellViewport(window);
                    AssertEmptyLedgerFitsMinimumSurface(window);
                    var minimumSurface = (FrameworkElement)window.Content;
                    var workspaceViewport = Descendants(window).OfType<ScrollViewer>().SingleOrDefault(item =>
                        string.Equals(AutomationProperties.GetAutomationId(item), "ResponsiveWorkbenchScrollViewport", StringComparison.Ordinal));
                    Check(workspaceViewport != null && workspaceViewport.IsVisible && workspaceViewport.ActualWidth > 0 && workspaceViewport.ActualHeight > 0,
                        "The responsive workspace must retain a visible scroll viewport at the minimum window size.");
                    var viewportBounds = Bounds(workspaceViewport, minimumSurface);
                    Check(viewportBounds.Left >= -1 && viewportBounds.Top >= -1 && viewportBounds.Right <= minimumSurface.ActualWidth + 2 && viewportBounds.Bottom <= minimumSurface.ActualHeight + 2,
                        $"The minimum-window workspace viewport must remain inside the window even when its route content scrolls: {viewportBounds} / {minimumSurface.ActualWidth:0.#}x{minimumSurface.ActualHeight:0.#}.");
                    foreach (var automationId in new[] { "TitleBarBorder", "OperationalRailCard" })
                    {
                        var element = Descendants(window).OfType<FrameworkElement>().FirstOrDefault(item =>
                            string.Equals(AutomationProperties.GetAutomationId(item), automationId, StringComparison.Ordinal) ||
                            string.Equals(item.Name, automationId, StringComparison.Ordinal));
                        Check(element != null && element.IsVisible && element.ActualWidth > 0 && element.ActualHeight > 0,
                            "Minimum-window composition lost visible area for " + automationId + ".");
                        var bounds = Bounds(element, minimumSurface);
                        Check(bounds.Left >= -1 && bounds.Top >= -1 && bounds.Right <= minimumSurface.ActualWidth + 2 && bounds.Bottom <= minimumSurface.ActualHeight + 2,
                            $"Minimum-window element {automationId} is clipped outside its surface: {bounds} / {minimumSurface.ActualWidth:0.#}x{minimumSurface.ActualHeight:0.#}.");
                    }
                    foreach (var automationId in new[] { "ActiveWorkbenchFrame", "ActiveWorkbenchContent" })
                    {
                        var element = Descendants(window).OfType<FrameworkElement>().FirstOrDefault(item =>
                            string.Equals(AutomationProperties.GetAutomationId(item), automationId, StringComparison.Ordinal) ||
                            string.Equals(item.Name, automationId, StringComparison.Ordinal));
                        Check(element != null && element.IsVisible && element.ActualWidth > 0 && element.ActualHeight > 0,
                            "Minimum-window composition lost visible scrollable content for " + automationId + ".");
                        Check(Bounds(element, minimumSurface).IntersectsWith(viewportBounds),
                            "The active route must intersect the visible workspace viewport at minimum window size: " + automationId + ".");
                    }
                    AssertMinimumWindowActionsVisible(window, shell, toolArray.Cast<ToolDefinition>());
                    AssertResponsiveEvidenceSummary(window);
                    AssertCategoryMottoBadgeFits(window);
                    AssertSplitDeckFitsMinimumWindow(window, shell);
                    AssertContextDossierResponsive(window, shell);
                    AssertCompactHeaderAcrossLocalesAtMinimum(app, window, shell);
                    records.Add(new { test = "compact-header-single-row-all-locales-at-minimum", locales = 13, passed = true });
                    records.Add(new { test = "context-dossier-responsive-focus-and-transient-state", passed = true });
                    records.Add(new { test = "minimum-window-responsive-layout", width = window.ActualWidth, height = window.ActualHeight, passed = true });
                }
                records.Add(new
                {
                    test = "render-language-fixed-dip-layout-repeat",
                    language,
                    layoutPass = layoutPass + 1,
                    logicalWidthDip = window.Width,
                    logicalHeightDip = window.Height,
                    layoutScaleApplied = false,
                    note = "Repeated responsive-layout assertions at the host's fixed WPF DPI; this pass does not emulate a Windows display-scale setting.",
                    layoutTransform = "identity",
                    windowsSystemDpiChanged = false,
                    emptyEvidence = true,
                    longLabels = true,
                    headerControls = true,
                    passed = true
                });
                }
            }
            phaseTimings["localization-and-fixed-dip-layout-repeats"] = phaseTimer.Elapsed.TotalMilliseconds;
            phaseTimer.Restart();
            // Theme regression: representative routed pages remain renderable in the fixed-DIP host.
            var themeIds = new[] { "war-table", "parchment", "high-contrast" };
            var allDefinitions = toolArray.Cast<ToolDefinition>().ToArray();
            var representatives = allDefinitions.GroupBy(tool => tool.Kind).Select(group => group.First()).ToList();
            var longestTool = allDefinitions.OrderByDescending(definition =>
                (definition.Title?.Length ?? 0) + (definition.Purpose?.Length ?? 0)).FirstOrDefault();
            if (longestTool != null && !representatives.Contains(longestTool)) representatives.Add(longestTool);
            Check(representatives.Select(tool => tool.Kind).Distinct().Count() == allDefinitions.Select(tool => tool.Kind).Distinct().Count(),
                "Theme renders must include at least one representative for every ToolDefinition.Kind.");
            Check(longestTool == null || representatives.Contains(longestTool), "Theme renders must include the longest-label route.");
            var representative = representatives.Cast<object>().ToArray();
            foreach (var themeId in themeIds)
            {
                SetPresentationForRender(app, shell, themeId, "en");
                RefreshSelectorBindings(window);
                for (var layoutPass = 0; layoutPass < 4; layoutPass++)
                {
                    PrepareFixedDipViewport(window);
                    var checkSelectorTheme = layoutPass == 0;
                    RenderTargetBitmap themePreview = null;
                    for (var index = 0; index < representative.Length; index++)
                    {
                        var tool = representative[index];
                        shellType.GetProperty("SelectedTool").SetValue(shell, tool);
                        Render(window);
                        if (index == 0 && layoutPass == 0)
                            themePreview = SavePreview(window, Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output)) ?? ".", Path.GetFileNameWithoutExtension(output) + "-" + themeId + ".png"), 1.0);
                        if (ReferenceEquals(tool, longestTool))
                            AssertLongPageLabels(window, (ToolDefinition)longestTool, themeId);
                        if (index == 0 && checkSelectorTheme)
                        {
                            AssertThemeBrushes(app, shell, themeId);
                            AssertSelectorThemeColors(app, window, themeId, themePreview);
                            AssertTitleBarThemeContrast(app, window, themeId);
                            AssertTitleBarDecorationsStayOutOfCaptionButtons(window, themeId);
                            AssertHeaderSealTheme(app, window, shell, themeId);
                            AssertConnectionIndicatorVisualStates(app, window, shell, themeId);
                            records.Add(new { test = "session-connection-indicator-matches-state", theme = themeId, states = 3, passed = true });
                            AssertDecorativeAccentsTheme(app, window, shell, themeId);
                            AssertStatusIconOpacity(app, window, themeId);
                            records.Add(new { test = "status-icons-remain-legible-by-theme", theme = themeId, passed = true });
                            AssertDisabledEvidenceButton(app, window, themeId);
                            records.Add(new { test = "render-disabled-evidence-button-contrast", theme = themeId, passed = true });
                            if (themeId == "high-contrast")
                                AssertHighContrastHeaderTextFitsAt1360(app, window, shell);
                            if (themeId == "war-table") AssertTitleBarWindowStateContract(window);
                        }
                        if (themeId == "parchment" && layoutPass == 0 && index == 0)
                            AssertHeaderLabelsFitAtNormalWidth(app, window);
                    }
                    if (checkSelectorTheme) AssertComboBoxStateContrast(app, themeId);
                    records.Add(new { test = "render-theme-fixed-dip-layout-repeat", theme = themeId, layoutPass = layoutPass + 1, layoutScaleApplied = false, routes = representative.Length, toolKinds = representatives.Count, longLabels = true, selectorStates = checkSelectorTheme, note = "Representative routes use the host's fixed WPF DPI; this pass does not emulate a Windows display-scale setting.", passed = true });
                }
            }
            phaseTimings["theme-and-fixed-dip-layout-repeats"] = phaseTimer.Elapsed.TotalMilliseconds;
            records.Add(new
            {
                test = "windows-system-dpi-layout-coverage",
                status = "not-simulated",
                appliedWindowsDpiScaleFactors = Array.Empty<double>(),
                previewBitmapRasterFactors = new[] { 1.0, 2.0 },
                note = "RenderTargetBitmap preview density does not change the host window's Windows DPI or WPF layout scale. The 100% and 200% values describe preview rasterization only; 125%, 150%, and 200% Windows-DPI layout remain untested."
            });
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            SetPresentationForRender(app, shell, "war-table", "en");
            PrepareFixedDipViewport(window);
            preferenceSnapshot.AssertUnchanged();
            records.Add(new { test = "render-test-does-not-write-desktop-preferences", passed = true });
            window.Close();
            visualTreeSnapshots.Remove(window);
            window = null;
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Check(IsolatedRenderDesktop.IsCurrentThreadAttached,
                "The WPF render thread must remain on its private Windows desktop through shutdown.");
            records.Add(new { test = "render-windows-isolated-from-interactive-desktop", desktop = IsolatedRenderDesktop.Name, foreground = foregroundWindowGuard.CreateDiagnosticSnapshot(), passed = true });
            Console.WriteLine($"PASS {records.Count} WPF render cases; {timer.ElapsedMilliseconds} ms. No game session or tool execution.");
            Console.WriteLine($"PERF {renderPassCount} render/layout passes, {renderLayoutMilliseconds:0.0} ms in those calls; visual-tree snapshots {visualTreeSnapshotBuilds} builds / {visualTreeSnapshotHits} hits / {visualTreeNodesVisited} visited nodes.");
            Console.WriteLine("PERF-PHASES " + JsonSerializer.Serialize(phaseTimings));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllText(output, JsonSerializer.Serialize(new
            {
                passed = true,
                milliseconds = timer.ElapsedMilliseconds,
                performance = RenderPerformanceSummary(),
                phaseTimings,
                foreground = foregroundWindowGuard.CreateDiagnosticSnapshot(),
                isolatedDesktop = IsolatedRenderDesktop.Name,
                scope = "Actual WPF template instantiation and layout at the isolated host's fixed DPI. Windows system-DPI layout scaling is not simulated; bitmap preview rasterization is reported separately. Not a visual clipping approval or game performance sample.",
                records
            }, new JsonSerializerOptions { WriteIndented = true }));
            exitCode = 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllText(output, JsonSerializer.Serialize(new
            {
                passed = false,
                error = error.ToString(),
                completed = records.Count,
                milliseconds = timer.ElapsedMilliseconds,
                performance = RenderPerformanceSummary(),
                phaseTimings,
                foreground = foregroundWindowGuard.CreateDiagnosticSnapshot(),
                isolatedDesktop = IsolatedRenderDesktop.Name
            }));
            exitCode = 1;
        }
        finally
        {
            if (window != null)
            {
                visualTreeSnapshots.Remove(window);
                window.Close();
            }
            if (app != null)
            {
                var dispatcher = app.Dispatcher;
                app.Shutdown();
                if (!dispatcher.HasShutdownStarted) dispatcher.InvokeShutdown();
            }
            foregroundWindowGuard.Dispose();
            try
            {
                var report = JsonNode.Parse(File.ReadAllText(output)) as JsonObject;
                if (report != null)
                {
                    report["foreground"] = JsonSerializer.SerializeToNode(foregroundWindowGuard.CreateDiagnosticSnapshot());
                    report["isolatedDesktopForegroundObserved"] = foregroundWindowGuard.TestProcessWasForegroundOnIsolatedDesktop;
                    report["interactiveDesktopIsolation"] = IsolatedRenderDesktop.Name;
                    File.WriteAllText(output, report.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch (Exception reportError)
            {
                Console.Error.WriteLine("Could not finalize foreground diagnostics: " + reportError.Message);
                exitCode = 1;
            }
        }
        return exitCode;
    }

    static void SetPresentationForRender(Application app, object shell, string themeId, string languageCode)
    {
        const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        var shellType = shell.GetType();
        var theme = (DesktopThemeService)shellType.GetField("theme", privateInstance)?.GetValue(shell);
        var localization = (DesktopLocalizationService)shellType.GetField("localization", privateInstance)?.GetValue(shell);
        Check(theme != null && localization != null, "Render harness could not access the shell's presentation services.");
        // Language/scale cases often re-enter the same theme. Avoid tearing down and
        // rebuilding its resource dictionary when the effective palette is unchanged.
        if (!string.Equals(theme.SelectedThemeId, themeId, StringComparison.OrdinalIgnoreCase))
        {
            var appliedTheme = theme.Apply(themeId);
            Check(appliedTheme.Succeeded && string.Equals(appliedTheme.ThemeId, themeId, StringComparison.OrdinalIgnoreCase),
                "Render harness could not apply theme resources without changing saved preferences: " + themeId);
        }
        localization.Apply(languageCode);

        // Keep the ViewModel getters in sync for one-way binding refreshes without calling setters that persist preferences.
        shellType.GetField("themeId", privateInstance)?.SetValue(shell, theme.SelectedThemeId);
        shellType.GetField("languageCode", privateInstance)?.SetValue(shell, languageCode);
        var raise = shellType.GetMethod("Raise", privateInstance);
        raise?.Invoke(shell, new object[] { "ThemeId" });
        raise?.Invoke(shell, new object[] { "SelectedTheme" });
        raise?.Invoke(shell, new object[] { "LanguageCode" });
        raise?.Invoke(shell, new object[] { "ActiveModderRoleLabel" });
        raise?.Invoke(shell, new object[] { "ActiveModderRoleHint" });
        Check(string.Equals(theme.SelectedThemeId, themeId, StringComparison.OrdinalIgnoreCase) &&
              string.Equals(localization.CurrentLanguageCode, languageCode, StringComparison.OrdinalIgnoreCase),
            "Render harness presentation resources do not match the requested theme and language.");
    }

    static void RefreshSelectorBindings(Window window)
    {
        foreach (var selector in Descendants(window).OfType<ComboBox>())
        foreach (var property in new[] { ComboBox.ItemsSourceProperty, ComboBox.SelectedItemProperty, ComboBox.SelectedValueProperty })
        {
            BindingOperations.GetBindingExpression(selector, property)?.UpdateTarget();
            if (selector.SelectedIndex < 0 && selector.SelectedItem != null)
                selector.SelectedIndex = selector.Items.IndexOf(selector.SelectedItem);
        }
    }

    static void PrepareFixedDipViewport(Window window)
    {
        // The isolated render host cannot emulate Windows system-DPI changes. Keep layout
        // assertions explicitly at this fixed logical viewport; SavePreview tests bitmap DPI only.
        window.Width = 1360;
        window.Height = 820;
        var surface = (FrameworkElement)window.Content;
        surface.LayoutTransform = Transform.Identity;
        Check(window.MinWidth >= 980 && window.MinHeight >= 680,
            "The render matrix must preserve the Desktop window's 980x680 DIP minimum size.");
        Check(Math.Abs(window.Width - 1360) < 0.1 && Math.Abs(window.Height - 820) < 0.1,
            $"The fixed-DIP render host must preserve its 1360x820 logical viewport; actual {window.Width}x{window.Height}.");
        Check(surface.LayoutTransform == Transform.Identity,
            "The fixed-DIP render host must not claim scaled WPF layout geometry through LayoutTransform.");
    }

    static void AssertEmptyEvidenceState(Application app, Window window, object shell, string languageCode)
    {
        var page = shell.GetType().GetProperty("CurrentPage")?.GetValue(shell);
        var evidence = page?.GetType().GetProperty("Evidence")?.GetValue(page) as IEnumerable;
        Check(evidence != null && !evidence.Cast<object>().Any(), "The empty-evidence render case must start with an empty ledger collection.");

        var activeCatalog = app.Resources.MergedDictionaries.SingleOrDefault(item =>
            item.Source?.OriginalString.EndsWith("Strings." + languageCode + ".xaml", StringComparison.OrdinalIgnoreCase) == true);
        if (string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase))
            activeCatalog ??= app.Resources.MergedDictionaries.SingleOrDefault(item =>
                item.Source?.OriginalString.EndsWith("Strings.en.xaml", StringComparison.OrdinalIgnoreCase) == true);
        Check(activeCatalog?.Contains("Ui.EmptyEvidence") == true,
            "The active " + languageCode + " catalog does not declare Ui.EmptyEvidence.");
        var expectedCopy = activeCatalog["Ui.EmptyEvidence"] as string;
        Check(!string.IsNullOrWhiteSpace(expectedCopy), "Ui.EmptyEvidence is empty in catalog " + languageCode + ".");

        var states = Descendants(window).OfType<FrameworkElement>()
            .Where(element => string.Equals(AutomationProperties.GetAutomationId(element), "EvidenceLedgerEmptyState", StringComparison.Ordinal))
            .ToArray();
        Check(states.Length == 1, "The rendered evidence ledger must expose one EvidenceLedgerEmptyState container.");
        var state = states[0];
        Check(state.Visibility == Visibility.Visible && state.IsVisible && state.ActualWidth > 0 && state.ActualHeight > 0,
            "The localized evidence empty state must have positive visible bounds when the evidence collection is empty.");
        Check(state.IsHitTestVisible,
            "The empty-state card must not disable hit testing for its existing command-palette action.");
        var reportTabs = Descendants(window).OfType<TabControl>().SingleOrDefault(control =>
            string.Equals(AutomationProperties.GetAutomationId(control), "WorkbenchReportTabs", StringComparison.Ordinal));
        var evidenceTab = reportTabs?.Items.OfType<TabItem>().SingleOrDefault(item =>
            string.Equals(AutomationProperties.GetAutomationId(item), "WorkbenchEvidenceTab", StringComparison.Ordinal));
        var rawResultTab = reportTabs?.Items.OfType<TabItem>().SingleOrDefault(item =>
            string.Equals(AutomationProperties.GetAutomationId(item), "WorkbenchRawResultTab", StringComparison.Ordinal));
        var expectedEvidenceHeader = app.TryFindResource("Ui.Evidence") as string;
        var expectedRawHeader = app.TryFindResource("Ui.RawResult") as string;
        Check(reportTabs != null && evidenceTab?.IsVisible == true && rawResultTab?.IsVisible == true &&
              !string.IsNullOrWhiteSpace(expectedEvidenceHeader) && !string.IsNullOrWhiteSpace(expectedRawHeader) &&
              Descendants(evidenceTab).OfType<TextBlock>().Any(block => block.IsVisible && block.ActualWidth > 0 && block.Text == expectedEvidenceHeader) &&
              Descendants(rawResultTab).OfType<TextBlock>().Any(block => block.IsVisible && block.ActualWidth > 0 && block.Text == expectedRawHeader),
            "The Evidence and Raw Result report tabs must have visible, localized labels in the selected theme.");
        var renderedText = Descendants(state).OfType<TextBlock>().ToArray();
        Check(renderedText.Any(block => string.Equals(block.Text, expectedCopy, StringComparison.Ordinal)),
            "The evidence empty state does not render the active localized Ui.EmptyEvidence copy for " + languageCode + ".");
        var searchHint = Descendants(window).OfType<TextBlock>().SingleOrDefault(block =>
            string.Equals(AutomationProperties.GetAutomationId(block), "OperationalSearchHint", StringComparison.Ordinal));
        var expectedSearchHint = app.TryFindResource("Ui.SearchHint") as string;
        Check(searchHint != null && searchHint.IsVisible && !searchHint.IsHitTestVisible &&
              !string.IsNullOrWhiteSpace(expectedSearchHint) &&
              string.Equals(searchHint.Text, expectedSearchHint, StringComparison.Ordinal),
            "The empty operational search must show its localized, passive hint in " + languageCode + ".");
        var cartographicBoard = Descendants(window).OfType<Grid>().SingleOrDefault(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "WorkbenchCartographicBoardDecoration", StringComparison.Ordinal));
        var boardHost = cartographicBoard == null ? null : VisualTreeHelper.GetParent(cartographicBoard) as Grid;
        var compactArtwork = window.ActualWidth < 1120;
        var expectedBoardWidth = compactArtwork ? 92d : 160d;
        var expectedBoardHeight = compactArtwork ? 52d : 90d;
        Check(cartographicBoard != null && boardHost != null && boardHost.ColumnDefinitions.Count >= 3 &&
              Math.Abs(cartographicBoard.ActualWidth - expectedBoardWidth) < 1 && Math.Abs(cartographicBoard.ActualHeight - expectedBoardHeight) < 1 &&
              boardHost.ColumnDefinitions[2].ActualWidth + 1 >= cartographicBoard.ActualWidth + cartographicBoard.Margin.Left + cartographicBoard.Margin.Right,
            $"The cartographic card ornament must adapt proportionally at the minimum width and retain its full illustration at normal width without clipping; expected={expectedBoardWidth:0.#}x{expectedBoardHeight:0.#}, actual={cartographicBoard?.ActualWidth:0.#}x{cartographicBoard?.ActualHeight:0.#}.");
        var ledgerCard = Descendants(window).OfType<Border>().SingleOrDefault(border =>
            string.Equals(AutomationProperties.GetAutomationId(border), "EvidenceLedgerCard", StringComparison.Ordinal));
        Check(ledgerCard != null && ledgerCard.ActualWidth > 0 && ledgerCard.ActualHeight > 0,
            "The evidence ledger card (EvidenceLedgerCard) must have positive visible bounds.");
        var cardBounds = Bounds(ledgerCard, window);
        foreach (var block in renderedText.Where(item => string.Equals(item.Text, expectedCopy, StringComparison.Ordinal)))
        {
            var textBounds = Bounds(block, window);
            Check(textBounds.Left >= cardBounds.Left - 1 && textBounds.Top >= cardBounds.Top - 1 && textBounds.Right <= cardBounds.Right + 2 && textBounds.Bottom <= cardBounds.Bottom + 2,
                $"The localized empty-evidence copy exceeds its ledger card at {languageCode}: text {textBounds}, card {cardBounds}; ancestors {DescribeVisualAncestors(state, window)}.");
        }
        var illustrations = Descendants(state).Where(element => element is Image || element is System.Windows.Shapes.Path).ToArray();
        Check(illustrations.Length > 0, "The evidence empty state must include its passive local illustration.");
        Check(illustrations.All(element => element is UIElement visual && !visual.IsHitTestVisible),
            "The evidence empty-state illustration must not intercept ledger controls.");
        var image = FindImage(state, "evidence-ledger-empty-v1.png");
        Check(image != null && image.Width == 40 && image.Height == 40 && image.IsVisible && !image.IsHitTestVisible && IsLocalTextureUri(image.Source),
            "The evidence empty-state illustration must be a visible, passive, local ImageGen resource in " + languageCode + ".");
        var imageBounds = Bounds(image, window);
        Check(imageBounds.Left >= cardBounds.Left - 1 && imageBounds.Top >= cardBounds.Top - 1 && imageBounds.Right <= cardBounds.Right + 2 && imageBounds.Bottom <= cardBounds.Bottom + 2,
            $"The empty-ledger illustration exceeds its card at {languageCode}: image {imageBounds}, card {cardBounds}.");
        var action = Descendants(state).OfType<Button>().SingleOrDefault(button =>
            string.Equals(AutomationProperties.GetAutomationId(button), "EvidenceLedgerOpenPaletteButton", StringComparison.Ordinal));
        var actionName = app.TryFindResource("Ui.EmptyEvidenceAction") as string;
        var actionBinding = action == null ? null : BindingOperations.GetBinding(action, Button.CommandProperty);
        var paletteCommand = shell.GetType().GetProperty("OpenPaletteCommand")?.GetValue(shell) as ICommand;
        var actionContent = action?.Content as TextBlock;
        Check(action != null && action.IsVisible && action.ActualWidth > 0 && action.ActualHeight >= 32 && action.IsEnabled &&
              action.IsHitTestVisible &&
              !string.IsNullOrWhiteSpace(actionName) &&
              string.Equals(AutomationProperties.GetName(action), actionName, StringComparison.Ordinal) &&
              string.Equals(action.ToolTip as string, actionName, StringComparison.Ordinal) &&
              string.Equals(actionContent?.Text, actionName, StringComparison.Ordinal) &&
              actionBinding?.Path?.Path == "DataContext.OpenPaletteCommand" && paletteCommand != null && ReferenceEquals(action.Command, paletteCommand),
            "The evidence empty-state action must expose its localized name and invoke the existing OpenPaletteCommand in " + languageCode + ".");
        var paletteOverlay = Descendants(window).OfType<FrameworkElement>().SingleOrDefault(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "CommandPaletteOverlay", StringComparison.Ordinal));
        var paletteOpen = (bool)shell.GetType().GetProperty("IsCommandPaletteOpen").GetValue(shell);
        Check(!paletteOpen && paletteOverlay?.Visibility == Visibility.Collapsed && !paletteOverlay.IsVisible,
            $"The command palette must be collapsed while checking the ledger CTA hit target at {languageCode} (state={paletteOpen}, visibility={paletteOverlay?.Visibility}, visible={paletteOverlay?.IsVisible}).");
        // The render host is off-screen, so its full-window OS input target is not a reliable
        // pointer simulation. Verify the button's own WPF hit surface and every visible ancestor;
        // separately require the only full-window command overlay to be collapsed above.
        var actionCenter = new Point(action.ActualWidth / 2, action.ActualHeight / 2);
        var actionHit = VisualTreeHelper.HitTest(action, actionCenter)?.VisualHit;
        var inputAncestorsVisible = true;
        for (DependencyObject ancestor = action; ancestor != null && !ReferenceEquals(ancestor, window); ancestor = VisualTreeHelper.GetParent(ancestor))
        {
            if (ancestor is UIElement inputElement && (!inputElement.IsVisible || !inputElement.IsHitTestVisible))
            {
                inputAncestorsVisible = false;
                break;
            }
        }
        Check(actionHit != null && IsVisualDescendantOrSelf(actionHit, action) && inputAncestorsVisible,
            $"The visible empty-state command-palette button must expose an unobstructed hit surface within its WPF subtree at {languageCode} (hit={actionHit?.GetType().Name ?? "none"}, point={actionCenter}, size={action.ActualWidth:0.#}x{action.ActualHeight:0.#}, ancestors={inputAncestorsVisible}).");
        var actionBounds = Bounds(action, window);
        var surface = (Visual)window.Content;
        var cardInSurface = Bounds(ledgerCard, surface);
        var actionInSurface = Bounds(action, surface);
        var drawnActionLocal = VisualTreeHelper.GetDescendantBounds(action);
        var drawnActionInSurface = action.TransformToAncestor(surface).TransformBounds(drawnActionLocal);
        var composition = PresentationSource.FromVisual(window)?.CompositionTarget;
        var transformToDevice = composition?.TransformToDevice ?? Matrix.Identity;
        var dpiWindow = VisualTreeHelper.GetDpi(window);
        var dpiSurface = VisualTreeHelper.GetDpi(surface);
        var dpiAction = VisualTreeHelper.GetDpi(action);
        Check(actionBounds.Left >= cardBounds.Left - 1 && actionBounds.Top >= cardBounds.Top - 1 && actionBounds.Right <= cardBounds.Right + 2 && actionBounds.Bottom <= cardBounds.Bottom + 2,
            $"The empty-ledger CTA exceeds its card at {languageCode}: action {actionBounds}, card {cardBounds}; " +
            $"window {window.ActualWidth:0.#}x{window.ActualHeight:0.#} dpi=({dpiWindow.DpiScaleX:0.###},{dpiWindow.DpiScaleY:0.###}); " +
            $"surface={((FrameworkElement)surface).ActualWidth:0.#}x{((FrameworkElement)surface).ActualHeight:0.#} dpi=({dpiSurface.DpiScaleX:0.###},{dpiSurface.DpiScaleY:0.###}); " +
            $"actionDpi=({dpiAction.DpiScaleX:0.###},{dpiAction.DpiScaleY:0.###}) deviceMatrix={transformToDevice}; " +
            $"surface-space action={actionInSurface}, card={cardInSurface}, drawnActionLocal={drawnActionLocal}, drawnActionSurface={drawnActionInSurface}; " +
            $"action-tree {DescribeVisualAncestors(action, window)}; card-tree {DescribeVisualAncestors(ledgerCard, window)}.");
    }

    static void ClearEvidenceForEmptyRenderCase(object shell)
    {
        var page = shell.GetType().GetProperty("CurrentPage")?.GetValue(shell);
        var evidence = page?.GetType().GetProperty("Evidence")?.GetValue(page);
        var clear = evidence?.GetType().GetMethod("Clear", Type.EmptyTypes);
        Check(clear != null, "The render harness could not prepare an empty evidence collection.");
        clear.Invoke(evidence, null);
    }

    static void AssertPinnedDeckFallbackLocalization(Application app, object shell, string languageCode)
    {
        var shellType = shell.GetType();
        var pinnedTool = shellType.GetProperty("PinnedTool");
        var pinnedSetter = pinnedTool?.GetSetMethod(nonPublic: true);
        Check(pinnedSetter != null, "The render harness could not prepare an unpinned Split Deck state.");
        pinnedSetter.Invoke(shell, new object[] { null });

        var evidence = shellType.GetProperty("PinnedEvidence")?.GetValue(shell) as IList;
        Check(evidence != null, "The Split Deck must expose its retained evidence collection.");
        evidence.Clear();
        var evidenceType = evidence.GetType().GetGenericArguments().Single();
        var sample = Activator.CreateInstance(evidenceType, new object[]
        {
            "Render test", "Retained", "Temporary pinned-deck evidence.",
            string.Empty, string.Empty, null, null, string.Empty
        });
        evidence.Add(sample);

        var pinnedTitleKey = app.TryFindResource("Ui.PinnedToolOutput") as string;
        var pinnedCategoryKey = app.TryFindResource("Ui.WorkspaceEvidence") as string;
        var emptyTitleKey = app.TryFindResource("Ui.NoToolPinned") as string;
        Check(!string.IsNullOrWhiteSpace(pinnedTitleKey) && !string.IsNullOrWhiteSpace(pinnedCategoryKey) &&
              !string.IsNullOrWhiteSpace(emptyTitleKey), "Split Deck fallback copy is missing from catalog " + languageCode + ".");
        Check((string)shellType.GetProperty("PinnedToolTitle").GetValue(shell) == pinnedTitleKey &&
              (string)shellType.GetProperty("PinnedToolCategory").GetValue(shell) == pinnedCategoryKey,
            "Split Deck evidence labels must resolve from the active localized catalog before clearing in " + languageCode + ".");

        var changedAfterClear = new List<string>();
        PropertyChangedEventHandler handler = (_, args) =>
        {
            if (args.PropertyName == "PinnedToolTitle" || args.PropertyName == "PinnedToolCategory")
            {
                if (evidence.Count == 0) changedAfterClear.Add(args.PropertyName);
            }
        };
        var observable = shell as INotifyPropertyChanged;
        Check(observable != null, "Desktop shell must expose property notifications for fallback labels.");
        observable.PropertyChanged += handler;
        try
        {
            var clear = shellType.GetProperty("ClearPinnedDeckCommand")?.GetValue(shell) as ICommand;
            Check(clear?.CanExecute(null) == true, "Clear Split Deck command must be available for the fallback regression.");
            clear.Execute(null);
            Check(changedAfterClear.Contains("PinnedToolTitle") && changedAfterClear.Contains("PinnedToolCategory") &&
                  (string)shellType.GetProperty("PinnedToolTitle").GetValue(shell) == emptyTitleKey &&
                  string.IsNullOrEmpty((string)shellType.GetProperty("PinnedToolCategory").GetValue(shell)),
                "Clearing Split Deck must notify and show localized empty labels in " + languageCode + ".");
        }
        finally { observable.PropertyChanged -= handler; }
    }

    static void AssertMinimumWindowActionsVisible(Window window, object shell, IEnumerable<ToolDefinition> tools)
    {
        var shellType = shell.GetType();
        var originalTool = shellType.GetProperty("SelectedTool").GetValue(shell);
        var inputTool = tools.FirstOrDefault(tool => tool.RequiresInput);
        Check(inputTool != null, "Minimum-window action coverage needs an input route.");
        try
        {
            shellType.GetProperty("SelectedTool").SetValue(shell, inputTool);
            Render(window);
            var pageViewport = Descendants(window).OfType<ScrollViewer>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "WorkbenchPageScrollViewport", StringComparison.Ordinal));
            var workspaceViewport = Descendants(window).OfType<ScrollViewer>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "ResponsiveWorkbenchScrollViewport", StringComparison.Ordinal));
            Check(pageViewport != null && pageViewport.IsVisible && pageViewport.ActualWidth > 0 && pageViewport.ActualHeight > 0,
                "The active tool page must expose a visible scroll viewport at minimum window size.");
            Check(workspaceViewport != null && workspaceViewport.IsVisible && workspaceViewport.ActualWidth > 0 && workspaceViewport.ActualHeight > 0,
                "The adaptive workspace viewport must be visible while checking minimum-window actions.");
            Check(pageViewport.ActualWidth <= workspaceViewport.ActualWidth + 2,
                $"The page viewport must stay within the responsive workspace width: {pageViewport.ActualWidth:0.#} / {workspaceViewport.ActualWidth:0.#}.");
            pageViewport.ScrollToHome();
            Render(window);

            foreach (var automationId in new[] { "BrowseFileButton", "BrowseFolderButton", "RunWorkOrderButton", "CancelWorkOrderButton", "ExportWorkOrderButton" })
            {
                var button = Descendants(window).OfType<Button>().SingleOrDefault(item =>
                    string.Equals(AutomationProperties.GetAutomationId(item), automationId, StringComparison.Ordinal));
                Check(button != null && button.IsVisible && button.ActualWidth > 0 && button.ActualHeight > 0,
                    "Minimum-window layout must keep the existing work-order action visible: " + automationId + ".");
                Check(button.MinHeight >= 32 && button.ActualHeight >= 32 && button.ActualWidth >= button.MinWidth - 1,
                    $"Work-order action {automationId} must keep a consistent activation size: {button.ActualWidth:0.#}x{button.ActualHeight:0.#} DIP.");
                var bounds = Bounds(button, pageViewport);
                Check(bounds.Left >= -1 && bounds.Top >= -1 && bounds.Right <= pageViewport.ActualWidth + 2 && bounds.Bottom <= pageViewport.ActualHeight + 2,
                    $"Work-order action {automationId} must fit the active page viewport without clipping: {bounds} / {pageViewport.ActualWidth:0.#}x{pageViewport.ActualHeight:0.#}.");
                var workspaceBounds = Bounds(button, workspaceViewport);
                Check(workspaceBounds.Left >= -1 && workspaceBounds.Top >= -1 && workspaceBounds.Right <= workspaceViewport.ActualWidth + 2,
                    $"Work-order action {automationId} must remain horizontally visible in the minimum-window workspace: {workspaceBounds} / {workspaceViewport.ActualWidth:0.#}x{workspaceViewport.ActualHeight:0.#}.");
                var visibleLabels = Descendants(button).OfType<TextBlock>()
                    .Where(block => block.IsVisible && block.ActualWidth > 0 && block.ActualHeight > 0).ToArray();
                Check(visibleLabels.Length > 0,
                    $"Work-order action {automationId} must retain visible button content at minimum width.");
                foreach (var label in visibleLabels)
                {
                    var labelBounds = Bounds(label, button);
                    Check(labelBounds.Left >= -1 && labelBounds.Top >= -1 &&
                          labelBounds.Right <= button.ActualWidth + 1 && labelBounds.Bottom <= button.ActualHeight + 1,
                        $"Work-order action {automationId} has content outside its button bounds: {labelBounds} / {button.ActualWidth:0.#}x{button.ActualHeight:0.#}.");
                }
            }
        }
        finally
        {
            shellType.GetProperty("SelectedTool").SetValue(shell, originalTool);
            Render(window);
        }
    }

    static void AssertResponsiveEvidenceSummary(Window window)
    {
        var card = Descendants(window).OfType<Border>().SingleOrDefault(item =>
            string.Equals(AutomationProperties.GetAutomationId(item), "RetainedEvidenceSummary", StringComparison.Ordinal));
        var summary = Descendants(window).OfType<TextBlock>().SingleOrDefault(item =>
            string.Equals(BindingOperations.GetBinding(item, TextBlock.TextProperty)?.Path?.Path, "EvidenceSummary", StringComparison.Ordinal));
        var button = Descendants(window).OfType<Button>().SingleOrDefault(item =>
            string.Equals(AutomationProperties.GetAutomationId(item), "ExportEvidenceButton", StringComparison.Ordinal));

        Check(card != null && summary != null && button != null && summary.IsVisible && button.IsVisible,
            "The retained-evidence status card must keep its count and export action visible at minimum width.");
        if (card == null || summary == null || button == null) return;

        var summaryBounds = Bounds(summary, card);
        var buttonBounds = Bounds(button, card);
        Check(summary.TextWrapping == TextWrapping.NoWrap && summary.TextTrimming == TextTrimming.CharacterEllipsis &&
              string.Equals(summary.ToolTip as string, summary.Text, StringComparison.Ordinal),
            "The evidence count must stay on one line and expose its full value through a tooltip.");
        Check(summary.ActualHeight <= summary.FontSize * 1.7 && summaryBounds.Right <= card.ActualWidth + 1 &&
              buttonBounds.Right <= card.ActualWidth + 1 && !summaryBounds.IntersectsWith(buttonBounds),
            "The evidence count and localized export action must fit without wrapping or overlapping in the narrow status card.");
        var expectedName = Application.Current.TryFindResource("Ui.ExportEvidence") as string;
        var exportGlyph = Descendants(button).OfType<System.Windows.Shapes.Path>().SingleOrDefault(path => path.IsVisible && path.Data != null);
        var naturalSummary = new TextBlock
        {
            Text = summary.Text,
            FontFamily = summary.FontFamily,
            FontSize = summary.FontSize,
            FontWeight = summary.FontWeight
        };
        naturalSummary.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Check(summary.ActualWidth + 1 >= naturalSummary.DesiredSize.Width,
            $"The evidence count must display without ellipsis at minimum width: actual {summary.ActualWidth:0.#}, required {naturalSummary.DesiredSize.Width:0.#} DIP.");
        Check(button.ActualHeight >= 24 && button.ActualWidth >= 32 && button.ActualWidth <= 40 &&
              exportGlyph != null && string.Equals(AutomationProperties.GetName(button), expectedName, StringComparison.Ordinal) &&
              string.Equals(button.ToolTip as string, expectedName, StringComparison.Ordinal),
            "The compact export icon must remain actionable and expose a localized accessible name and tooltip.");
    }

    static void AssertCompactHeaderAtMinimum(Window window, string locale = "")
    {
        var header = Descendants(window).OfType<Border>().SingleOrDefault(item =>
            string.Equals(AutomationProperties.GetAutomationId(item), "MainCommandHeaderBand", StringComparison.Ordinal));
        var automationIds = new[]
        {
            "HeaderBrandGroup", "CommandPaletteButton", "LanguageSettingGroup", "ThemeSettingGroup",
            "SessionSettingGroup", "DecorativeAccentsToggle", "SplitDeckToggleButton", "ContextDossierToggleButton"
        };
        var controls = automationIds.Select(id => Descendants(window).OfType<FrameworkElement>().SingleOrDefault(item =>
            string.Equals(AutomationProperties.GetAutomationId(item), id, StringComparison.Ordinal))).ToArray();
        Check(header != null && header.IsVisible && controls.All(item => item != null && item.IsVisible && item.ActualWidth > 0),
            "The minimum-width command header must keep every setting and command control visible.");
        if (header == null || controls.Any(item => item == null)) return;

        var tops = controls.Select(item => Bounds(item, header).Top).ToArray();
        var controlLayout = string.Join("; ", controls.Select((item, index) =>
            automationIds[index] + "=" + Bounds(item, header) + ", actual=" + item.ActualWidth.ToString("0.#", CultureInfo.InvariantCulture) + "x" + item.ActualHeight.ToString("0.#", CultureInfo.InvariantCulture)));
        var headerIsCompact = tops.Max() - tops.Min() <= 14 && header.ActualHeight <= 82;
        if (!headerIsCompact)
        {
            var childLayout = string.Join("; ", Descendants(header).OfType<FrameworkElement>()
                .Where(item => item.IsVisible && item.ActualHeight > 16)
                .Take(24)
                .Select(item => item.GetType().Name + "[" + AutomationProperties.GetAutomationId(item) + "]=" + Bounds(item, header) +
                    (item is TextBlock block ? ", text=" + block.Text : string.Empty)));
            Console.WriteLine($"HEADER_LAYOUT_DIAG locale={(string.IsNullOrEmpty(locale) ? "current" : locale)} controls={controlLayout} children={childLayout}");
        }
        Check(headerIsCompact,
            $"The compact command header must remain a single balanced row at 980 DIP instead of wrapping isolated controls; locale={(string.IsNullOrEmpty(locale) ? "current" : locale)}; rows={string.Join(",", tops.Select(top => top.ToString("0.#", CultureInfo.InvariantCulture)))}; height={header.ActualHeight:0.#}; controls={controlLayout}.");

        var dossierToggle = controls[^1] as ToggleButton;
        var localizedDossierName = Application.Current.TryFindResource("Ui.DossierHeading") as string;
        var dossierGlyph = dossierToggle == null ? null : Descendants(dossierToggle).OfType<System.Windows.Shapes.Path>().SingleOrDefault(path => path.IsVisible && path.Data != null);
        Check(dossierToggle != null && dossierToggle.ActualWidth <= 42 && dossierGlyph != null &&
              string.Equals(AutomationProperties.GetName(dossierToggle), localizedDossierName, StringComparison.Ordinal) &&
              string.Equals(dossierToggle.ToolTip as string, localizedDossierName, StringComparison.Ordinal),
            "The compact dossier icon must preserve its localized accessible name and complete tooltip.");
    }

    static void AssertCompactHeaderAcrossLocalesAtMinimum(Application app, Window window, object shell)
    {
        var languages = new[] { "en", "es", "pt", "de", "fr", "it", "pl", "ru", "tr", "zh-HANS", "zh-HANT", "ja", "ko" };
        foreach (var language in languages)
        {
            SetPresentationForRender(app, shell, "war-table", language);
            RefreshSelectorBindings(window);
            Render(window);
            AssertCompactHeaderAtMinimum(window, language);
            AssertFooterFitsShellViewport(window);
        }

        SetPresentationForRender(app, shell, "war-table", "en");
        RefreshSelectorBindings(window);
        Render(window);
    }

    static void AssertEmptyLedgerFitsMinimumSurface(Window window)
    {
        var surface = window.Content as FrameworkElement;
        var viewport = Descendants(window).OfType<ScrollViewer>().SingleOrDefault(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "ResponsiveWorkbenchScrollViewport", StringComparison.Ordinal));
        var state = Descendants(window).OfType<FrameworkElement>().SingleOrDefault(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "EvidenceLedgerEmptyState", StringComparison.Ordinal));
        var action = Descendants(window).OfType<Button>().SingleOrDefault(button =>
            string.Equals(AutomationProperties.GetAutomationId(button), "EvidenceLedgerOpenPaletteButton", StringComparison.Ordinal));
        var title = state == null ? null : Descendants(state).OfType<TextBlock>().FirstOrDefault(block =>
            string.Equals(BindingOperations.GetBinding(block, TextBlock.TextProperty)?.Path?.Path, "Ui.EmptyEvidence", StringComparison.Ordinal) ||
            block.Text == (Application.Current.TryFindResource("Ui.EmptyEvidence") as string));

        Check(surface != null && viewport?.IsVisible == true && state?.IsVisible == true && action?.IsVisible == true && title?.IsVisible == true,
            "The empty report ledger must expose its localized placeholder and action at the minimum window size.");
        if (surface == null || viewport == null || state == null || action == null || title == null) return;

        var viewportBounds = Bounds(viewport, surface);
        var actionBounds = Bounds(action, surface);
        var titleBounds = Bounds(title, surface);
        Check(titleBounds.Left >= viewportBounds.Left - 1 && titleBounds.Top >= viewportBounds.Top - 1 &&
              titleBounds.Right <= viewportBounds.Right + 1 && titleBounds.Bottom <= viewportBounds.Bottom + 1 &&
              actionBounds.Left >= viewportBounds.Left - 1 && actionBounds.Top >= viewportBounds.Top - 1 &&
              actionBounds.Right <= viewportBounds.Right + 1 && actionBounds.Bottom <= viewportBounds.Bottom + 1 &&
              action.ActualHeight >= 32,
            $"The empty-ledger message and action must both fit in the visible workspace viewport at 980x680 DIP: title {titleBounds}, action {actionBounds}, viewport {viewportBounds}.");
    }

    static void AssertCategoryMottoBadgeFits(Window window)
    {
        var badge = Descendants(window).OfType<Border>().SingleOrDefault(item =>
            string.Equals(AutomationProperties.GetAutomationId(item), "ToolCategoryMottoBadge", StringComparison.Ordinal));
        var banner = Descendants(window).OfType<TextBlock>().SingleOrDefault(item =>
            string.Equals(BindingOperations.GetBinding(item, TextBlock.TextProperty)?.Path?.Path, "Tool.CategoryBanner", StringComparison.Ordinal));
        var motto = Descendants(window).OfType<TextBlock>().SingleOrDefault(item =>
            string.Equals(BindingOperations.GetBinding(item, TextBlock.TextProperty)?.Path?.Path, "Tool.CategoryMotto", StringComparison.Ordinal));

        Check(badge != null && banner != null && motto != null && badge.IsVisible && banner.IsVisible && motto.IsVisible,
            "The tool identity badge must retain its category banner and motto at minimum width.");
        if (badge == null || banner == null || motto == null) return;

        Check(banner.TextWrapping == TextWrapping.NoWrap && motto.TextWrapping == TextWrapping.NoWrap &&
              banner.TextTrimming == TextTrimming.CharacterEllipsis && motto.TextTrimming == TextTrimming.CharacterEllipsis &&
              string.Equals(banner.ToolTip as string, banner.Text, StringComparison.Ordinal) &&
              string.Equals(motto.ToolTip as string, motto.Text, StringComparison.Ordinal),
            "Long category identity text must trim visibly and expose the complete motto and banner on hover.");
        Check(badge.ActualWidth <= 541,
            $"The category identity ornament must remain a compact badge instead of spanning the entire summary card: {badge.ActualWidth:0.#} DIP.");
        foreach (var label in new[] { banner, motto })
        {
            var bounds = Bounds(label, badge);
            Check(bounds.Left >= -1 && bounds.Top >= -1 && bounds.Right <= badge.ActualWidth + 1 && bounds.Bottom <= badge.ActualHeight + 1,
                $"The category identity label '{label.Text}' must remain inside its badge: {bounds} / {badge.ActualWidth:0.#}x{badge.ActualHeight:0.#}.");
        }
    }

    static void AssertSplitDeckFitsMinimumWindow(Window window, object shell)
    {
        var shellType = shell.GetType();
        var originalWidth = window.Width;
        var originalHeight = window.Height;
        var originalTool = shellType.GetProperty("SelectedTool").GetValue(shell);
        var wasActive = (bool)shellType.GetProperty("IsSplitDeckActive").GetValue(shell);
        var toggle = shellType.GetProperty("ToggleSplitDeckCommand")?.GetValue(shell) as ICommand;
        var close = shellType.GetProperty("CloseSplitDeckCommand")?.GetValue(shell) as ICommand;
        Check(toggle != null && close != null, "Split Deck must expose its existing open and close commands.");
        if (!wasActive) toggle.Execute(null);
        try
        {
            var catalog = shellType.GetProperty("Catalog").GetValue(shell);
            var toolArray = ((IEnumerable)catalog.GetType().GetProperty("Tools").GetValue(catalog)).Cast<ToolDefinition>().ToArray();
            var dashboardTool = toolArray.FirstOrDefault(tool => tool.HasVisualDashboard);
            var standardTool = toolArray.FirstOrDefault(tool => !tool.HasVisualDashboard);
            Check(dashboardTool != null && standardTool != null,
                "Responsive Split Deck coverage needs both a specialist dashboard and a standard route.");

            window.Width = window.MinWidth;
            window.Height = window.MinHeight;
            shellType.GetProperty("SelectedTool").SetValue(shell, dashboardTool);
            Render(window);
            var frame = Descendants(window).OfType<FrameworkElement>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "ActiveWorkbenchFrame", StringComparison.Ordinal));
            var page = Descendants(window).OfType<ContentControl>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "ActiveWorkbenchContent", StringComparison.Ordinal));
            var deck = Descendants(window).OfType<FrameworkElement>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "PinnedDeckCard", StringComparison.Ordinal));
            Check(frame != null && page != null && deck != null && deck.IsVisible &&
                  page.ActualWidth >= 520 && page.ActualHeight > 0,
                $"Split Deck must preserve a usable workbench at minimum window size; frame={frame?.ActualWidth:0.#}, page={page?.ActualWidth:0.#}x{page?.ActualHeight:0.#}, deck={deck?.ActualWidth:0.#}, viewport={window.ActualWidth:0.#}x{window.ActualHeight:0.#}.");
            var frameBounds = Bounds(frame, window);
            var deckBounds = Bounds(deck, window);
            var pageBounds = Bounds(page, window);
            Check(deckBounds.Left >= frameBounds.Left - 1 && deckBounds.Right <= frameBounds.Right + 2 &&
                  deckBounds.Top >= frameBounds.Bottom - 1 && pageBounds.Left >= frameBounds.Left - 1 &&
                  pageBounds.Right <= frameBounds.Right + 2,
                $"A specialist route that needs more width must stack the Split Deck below the active route at minimum size; frame={frameBounds}, page={pageBounds}, deck={deckBounds}.");

            window.Width = 1360;
            window.Height = 820;
            shellType.GetProperty("SelectedTool").SetValue(shell, standardTool);
            Render(window);
            page = Descendants(window).OfType<ContentControl>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "ActiveWorkbenchContent", StringComparison.Ordinal));
            frameBounds = Bounds(frame, window);
            deckBounds = Bounds(deck, window);
            Check(page.ActualWidth >= 360 && deckBounds.Left >= frameBounds.Right - 1 &&
                  Math.Abs(deckBounds.Top - frameBounds.Top) <= 1,
                $"A standard route should keep the Split Deck beside the active route when both fit; frame={frameBounds}, deck={deckBounds}.");
        }
        finally
        {
            if (!wasActive) close.Execute(null);
            shellType.GetProperty("SelectedTool").SetValue(shell, originalTool);
            window.Width = originalWidth;
            window.Height = originalHeight;
            Render(window);
        }
    }

    static void AssertContextDossierResponsive(Window window, object shell)
    {
        var shellType = shell.GetType();
        var originalWidth = window.Width;
        var originalHeight = window.Height;
        var originalTool = shellType.GetProperty("SelectedTool").GetValue(shell);
        var originalOpen = (bool)shellType.GetProperty("IsContextDossierOpen").GetValue(shell);
        var originalSplit = (bool)shellType.GetProperty("IsSplitDeckActive").GetValue(shell);
        var closeDeck = shellType.GetProperty("CloseSplitDeckCommand")?.GetValue(shell) as ICommand;
        var catalog = shellType.GetProperty("Catalog").GetValue(shell);
        var toolArray = ((IEnumerable)catalog.GetType().GetProperty("Tools").GetValue(catalog)).Cast<ToolDefinition>().ToArray();
        var standardTool = toolArray.FirstOrDefault(tool => !tool.HasVisualDashboard);
        var dashboardTool = toolArray.FirstOrDefault(tool => tool.HasVisualDashboard);
        Check(standardTool != null && dashboardTool != null, "Context dossier coverage needs standard and specialist routes.");

        try
        {
            closeDeck?.Execute(null);
            shellType.GetProperty("IsContextDossierOpen").SetValue(shell, false);
            shellType.GetProperty("SelectedTool").SetValue(shell, standardTool);
            window.Width = 1360;
            window.Height = 820;
            Render(window);
            var toggle = Descendants(window).OfType<ToggleButton>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "ContextDossierToggleButton", StringComparison.Ordinal));
            var dossier = Descendants(window).OfType<FrameworkElement>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "ContextDossierPanel", StringComparison.Ordinal));
            var frame = Descendants(window).OfType<FrameworkElement>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "ActiveWorkbenchFrame", StringComparison.Ordinal));
            var layout = Descendants(window).OfType<AdaptiveWorkbenchPanel>().SingleOrDefault();
            var scroll = Descendants(window).OfType<ScrollViewer>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "ResponsiveWorkbenchScrollViewport", StringComparison.Ordinal));
            Check(toggle != null && dossier != null && frame != null && layout != null && scroll != null,
                "The contextual dossier must expose its accessible toggle, named panel, adaptive layout, and scroll viewport.");
            var localizedName = Application.Current.TryFindResource("Ui.DossierHeading") as string;
            Check(BindingOperations.GetBinding(toggle, ToggleButton.IsCheckedProperty)?.Path?.Path == "IsContextDossierOpen" &&
                  string.Equals(AutomationProperties.GetName(toggle), localizedName, StringComparison.Ordinal),
                "The contextual dossier toggle must have a localized accessible name and bind only to transient presentation state.");
            var toggleGlyph = Descendants(toggle).OfType<System.Windows.Shapes.Path>().SingleOrDefault(path => path.IsVisible && path.Data != null);
            Check(toggleGlyph != null && toggleGlyph.ActualWidth >= 14 && toggleGlyph.ActualHeight >= 10 &&
                  toggle.ActualWidth <= 42 && string.Equals(toggle.ToolTip as string, localizedName as string, StringComparison.Ordinal),
                $"The compact contextual dossier toggle must show its book glyph while preserving the full localized tooltip and accessible name; glyph={toggleGlyph?.ActualWidth:0.#}x{toggleGlyph?.ActualHeight:0.#}, button={toggle.ActualWidth:0.#}x{toggle.ActualHeight:0.#}.");

            FocusManager.SetFocusedElement(window, toggle);
            Check(ReferenceEquals(FocusManager.GetFocusedElement(window), toggle),
                "The dossier toggle must accept logical focus in the non-activating synthetic window.");
            toggle.SetCurrentValue(ToggleButton.IsCheckedProperty, true);
            Render(window);
            Check(dossier.IsVisible && dossier.ActualWidth > 0 && dossier.ActualHeight > 0 &&
                  ReferenceEquals(FocusManager.GetFocusedElement(window), toggle),
                "Opening the contextual dossier must show its side panel without moving logical focus or activating the window.");
            var consoleCopy = Descendants(dossier).OfType<Button>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "CopyConsoleCommandButton0", StringComparison.Ordinal));
            var cliCopy = Descendants(dossier).OfType<Button>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "CopyCliSyntaxButton", StringComparison.Ordinal));
            var copyFeedback = Descendants(dossier).OfType<TextBlock>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), "ClipboardCopyFeedback", StringComparison.Ordinal));
            var page = shellType.GetProperty("CurrentPage").GetValue(shell);
            var commandEntries = ((IEnumerable)page.GetType().GetProperty("ConsoleCommandEntries").GetValue(page)).Cast<object>().ToArray();
            var commandText = commandEntries[0].GetType().GetProperty("Text").GetValue(commandEntries[0]) as string;
            var localizedCommandName = Application.Current.TryFindResource("Ui.CopyCommandAccessibleNameFormat") as string;
            Check(consoleCopy != null && cliCopy != null && copyFeedback != null && commandEntries.Length > 0 &&
                  string.Equals(AutomationProperties.GetAutomationId(consoleCopy), "CopyConsoleCommandButton0", StringComparison.Ordinal) &&
                  string.Equals(AutomationProperties.GetName(consoleCopy), string.Format(CultureInfo.CurrentCulture, localizedCommandName, commandText), StringComparison.Ordinal) &&
                  consoleCopy.Command?.CanExecute(consoleCopy.CommandParameter) == true &&
                  string.Equals(AutomationProperties.GetName(cliCopy), Application.Current.TryFindResource("Ui.CopyCliAccessibleName") as string, StringComparison.Ordinal) &&
                  cliCopy.Command?.CanExecute(cliCopy.CommandParameter) == true &&
                  AutomationProperties.GetLiveSetting(copyFeedback) == AutomationLiveSetting.Polite,
                "The open dossier must expose distinct localized copy names, bound commands, and polite live feedback without invoking a copy action.");
            var frameBounds = Bounds(frame, window);
            var dossierBounds = Bounds(dossier, window);
            Check(!frameBounds.IntersectsWith(dossierBounds) && dossierBounds.Left >= frameBounds.Right - 1 &&
                  dossierBounds.Top >= Bounds(layout, window).Top - 1,
                $"The wide-window contextual dossier must occupy a separate side column, not cover workbench controls; frame={frameBounds}, dossier={dossierBounds}.");

            window.Width = window.MinWidth;
            window.Height = window.MinHeight;
            shellType.GetProperty("SelectedTool").SetValue(shell, dashboardTool);
            Render(window);
            frameBounds = Bounds(frame, window);
            dossierBounds = Bounds(dossier, window);
            var layoutBounds = Bounds(layout, window);
            Check(!frameBounds.IntersectsWith(dossierBounds) &&
                  dossierBounds.Left >= layoutBounds.Left - 1 && dossierBounds.Right <= layoutBounds.Right + 2 &&
                  dossierBounds.Top >= frameBounds.Bottom - 1,
                $"At minimum width, the context panel for a wide visual route must stack below the route in the scrollable work area; frame={frameBounds}, dossier={dossierBounds}, layout={layoutBounds}.");
            toggle.SetCurrentValue(ToggleButton.IsCheckedProperty, false);
            Render(window);
            Check(!dossier.IsVisible && ReferenceEquals(FocusManager.GetFocusedElement(window), toggle) &&
                  !(bool)shellType.GetProperty("IsContextDossierOpen").GetValue(shell),
                "Closing the dossier must hide it and retain logical focus on the same accessible toggle.");
            Check(toggleGlyph.IsVisible && string.Equals(AutomationProperties.GetName(toggle), localizedName as string, StringComparison.Ordinal),
                "The compact dossier glyph and accessible name must remain available at minimum window width after closing the panel.");
        }
        finally
        {
            shellType.GetProperty("IsContextDossierOpen").SetValue(shell, originalOpen);
            if (originalSplit)
            {
                var openDeck = shellType.GetProperty("ToggleSplitDeckCommand")?.GetValue(shell) as ICommand;
                openDeck?.Execute(null);
            }
            shellType.GetProperty("SelectedTool").SetValue(shell, originalTool);
            window.Width = originalWidth;
            window.Height = originalHeight;
            Render(window);
        }
    }

    static void AssertVisualizerLocalization(Application app, Window window, object shell, ToolDefinition[] tools, string languageCode)
    {
        var shellType = shell.GetType();
        var originalTool = shellType.GetProperty("SelectedTool").GetValue(shell);
        var routes = new[]
        {
            (Id: "AudioFmodMixerInspector", Resource: "Viz.Audio.Title"),
            (Id: "AssemblyInspector", Resource: "Viz.CodeSecurity.Title"),
            (Id: "TroopTreeVisualizer", Resource: "Viz.Troop.TierI"),
            (Id: "WorkshopEnterpriseSimulator", Resource: "Viz.Workshop.ProsperityLabel")
        };
        try
        {
            foreach (var route in routes)
            {
                var tool = tools.SingleOrDefault(item => string.Equals(item.Id, route.Id, StringComparison.Ordinal));
                Check(tool != null, "Localized visualizer coverage cannot find route " + route.Id + ".");
                var expected = app.TryFindResource(route.Resource) as string;
                Check(!string.IsNullOrWhiteSpace(expected), "The " + languageCode + " catalog has no visualizer title " + route.Resource + ".");
                shellType.GetProperty("SelectedTool").SetValue(shell, tool);
                Render(window);
                Check(Descendants(window).OfType<TextBlock>().Any(block => block.IsVisible && block.ActualWidth > 0 &&
                        (string.Equals(block.Text, expected, StringComparison.Ordinal) ||
                         block.Inlines.OfType<Run>().Any(run => string.Equals(run.Text, expected, StringComparison.Ordinal)))),
                    "The " + languageCode + " label " + route.Resource + " did not render for " + route.Id + ".");
            }
        }
        finally
        {
            shellType.GetProperty("SelectedTool").SetValue(shell, originalTool);
            Render(window);
        }
    }

    static void AssertEmptyLedgerFilledTransition(Window window, object shell)
    {
        var page = shell.GetType().GetProperty("CurrentPage")?.GetValue(shell);
        var evidence = page?.GetType().GetProperty("Evidence")?.GetValue(page);
        Check(evidence != null, "The synthetic ToolPage must expose its transient evidence collection.");
        var collectionType = evidence.GetType();
        var evidenceType = collectionType.GetGenericArguments().Single();
        var record = Activator.CreateInstance(evidenceType, new object[]
        {
            "Render test", "Available", "Temporary evidence with enough detail to exercise the populated report card and preserve wrapped technical context.",
            "RULE-RENDER-01", @"C:\Mods\CalradiaForge\diagnostics\sample-source.cs", 421, 7,
            "Review the indicated source location and retain the attached evidence for follow-up."
        });
        var secondRecord = Activator.CreateInstance(evidenceType, new object[]
        {
            "Second render test", "Review", "Second synthetic evidence row verifies that repeated cards remain individually represented.",
            "RULE-RENDER-02", @"C:\Mods\CalradiaForge\diagnostics\second-source.cs", 12, 4,
            "   \t"
        });
        Check(record != null && secondRecord != null, "The render harness could not construct its temporary evidence rows.");
        var add = collectionType.GetMethod("Add", new[] { evidenceType });
        var remove = collectionType.GetMethod("Remove", new[] { evidenceType });
        Check(add != null && remove != null, "The transient evidence collection does not expose normal collection transitions.");
        try
        {
            add.Invoke(evidence, new[] { record });
            add.Invoke(evidence, new[] { secondRecord });
            Render(window);
            var state = Descendants(window).OfType<FrameworkElement>().Single(element =>
                string.Equals(AutomationProperties.GetAutomationId(element), "EvidenceLedgerEmptyState", StringComparison.Ordinal));
            var illustration = FindImage(state, "evidence-ledger-empty-v1.png");
            var action = Descendants(state).OfType<Button>().Single(button =>
                string.Equals(AutomationProperties.GetAutomationId(button), "EvidenceLedgerOpenPaletteButton", StringComparison.Ordinal));
            Check(!state.IsVisible && !illustration.IsVisible && !action.IsVisible,
                "The empty-ledger illustration and CTA must collapse when evidence exists.");
            var ledger = Descendants(window).OfType<ListBox>().Single(list =>
                string.Equals(AutomationProperties.GetAutomationId(list), "EvidenceLedgerItems", StringComparison.Ordinal));
            var entryCards = Descendants(ledger).OfType<Border>().Where(border =>
                border.Background is SolidColorBrush && border.CornerRadius.TopLeft > 0 &&
                Descendants(border).OfType<TextBlock>().Any(block =>
                    block.Text.Contains("Temporary evidence with enough detail", StringComparison.Ordinal) ||
                    block.Text.Contains("Second synthetic evidence row", StringComparison.Ordinal))).ToArray();
            var entryCard = entryCards.SingleOrDefault(border =>
                Descendants(border).OfType<TextBlock>().Any(block => block.Text == "RULE-RENDER-01"));
            var secondEntryCard = entryCards.SingleOrDefault(border =>
                Descendants(border).OfType<TextBlock>().Any(block => block.Text == "RULE-RENDER-02"));
            var secondRecommendation = secondEntryCard == null ? null : Descendants(secondEntryCard).OfType<TextBlock>()
                .SingleOrDefault(block => BindingOperations.GetBinding(block, TextBlock.TextProperty)?.Path?.Path == "RecommendationDisplay");
            var recommendationSeparator = secondRecommendation == null ? null : FindAncestor<Border>(secondRecommendation, secondEntryCard);
            var cardText = entryCard == null ? Array.Empty<TextBlock>() : Descendants(entryCard).OfType<TextBlock>().Where(block => block.IsVisible).ToArray();
            Check(entryCards.Length == 2 && entryCards.All(border => string.IsNullOrEmpty(AutomationProperties.GetAutomationId(border))) &&
                  entryCard != null && secondEntryCard != null && entryCard.IsVisible && entryCard.ActualWidth > 0 && entryCard.ActualHeight > 0 &&
                  entryCard.Background is SolidColorBrush && entryCard.CornerRadius.TopLeft > 0 &&
                  secondRecommendation != null && string.IsNullOrEmpty(secondRecommendation.Text) &&
                  recommendationSeparator?.Visibility == Visibility.Collapsed &&
                  cardText.Any(block => block.Text == "RULE-RENDER-01") &&
                  cardText.Any(block => block.Text == @"C:\Mods\CalradiaForge\diagnostics\sample-source.cs:421:7") &&
                  cardText.Any(block => block.Text.Contains("Temporary evidence with enough detail", StringComparison.Ordinal)) &&
                  cardText.Any(block => block.Text.Contains("Review the indicated source location", StringComparison.Ordinal)) &&
                  cardText.Where(block => block.Text.Contains("Temporary evidence with enough detail", StringComparison.Ordinal) ||
                                          block.Text.Contains("Review the indicated source location", StringComparison.Ordinal))
                          .All(block => block.TextWrapping == TextWrapping.Wrap),
                "A populated report entry must present source context, rule, location, evidence, and recommendation on a clean, readable card.");
        }
        finally
        {
            remove.Invoke(evidence, new[] { record });
            remove.Invoke(evidence, new[] { secondRecord });
            Render(window);
        }
        var restored = Descendants(window).OfType<FrameworkElement>().Single(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "EvidenceLedgerEmptyState", StringComparison.Ordinal));
        Check(restored.IsVisible, "Removing the temporary evidence row did not restore the empty-ledger state.");
    }

    static void AssertEmptyLedgerOpenPaletteAction(Window window, object shell)
    {
        var state = Descendants(window).OfType<FrameworkElement>().Single(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "EvidenceLedgerEmptyState", StringComparison.Ordinal));
        var action = Descendants(state).OfType<Button>().Single(button =>
            string.Equals(AutomationProperties.GetAutomationId(button), "EvidenceLedgerOpenPaletteButton", StringComparison.Ordinal));
        var command = action.Command;
        Check(command != null && command.CanExecute(null), "The empty-ledger command-palette action cannot execute in the synthetic host.");
        command.Execute(action.CommandParameter);
        Render(window);
        Check((bool)shell.GetType().GetProperty("IsCommandPaletteOpen").GetValue(shell),
            "Activating the empty-ledger action did not open the existing command palette.");
        var paletteSearch = Descendants(window).OfType<TextBox>().SingleOrDefault(control =>
            string.Equals(AutomationProperties.GetAutomationId(control), "CommandPaletteSearch", StringComparison.Ordinal));
        Check(paletteSearch != null && paletteSearch.IsVisible && paletteSearch.MinHeight >= 42 &&
              paletteSearch.ActualHeight >= 42 && paletteSearch.ActualWidth >= 280,
            $"The command-palette search must use a readable size at the minimum window: {paletteSearch?.ActualWidth:0.#}x{paletteSearch?.ActualHeight:0.#} DIP.");
        var close = shell.GetType().GetProperty("ClosePaletteCommand")?.GetValue(shell) as ICommand;
        Check(close != null && close.CanExecute(null), "The test host cannot close the command palette after its in-memory activation check.");
        close.Execute(null);
        Render(window);
        Check(!(bool)shell.GetType().GetProperty("IsCommandPaletteOpen").GetValue(shell),
            "The command-palette action check did not restore the test host's closed palette state.");
    }

    static void AssertClearFilterControl(Application app, Window window, object shell, ContentControl pageHost, IEnumerable toolArray)
    {
        var shellType = shell.GetType();
        var filterProperty = shellType.GetProperty("Filter");
        var focusRequestProperty = shellType.GetProperty("SearchFocusRequest");
        var visibleToolsProperty = shellType.GetProperty("VisibleTools");
        var searchHint = Descendants(window).OfType<TextBlock>().SingleOrDefault(block =>
            string.Equals(AutomationProperties.GetAutomationId(block), "OperationalSearchHint", StringComparison.Ordinal));
        var searchBox = Descendants(window).OfType<TextBox>().SingleOrDefault(control =>
            string.Equals(AutomationProperties.GetAutomationId(control), "OperationalSearchFilter", StringComparison.Ordinal));
        var clearButton = Descendants(window).OfType<Button>().SingleOrDefault(control =>
            string.Equals(AutomationProperties.GetAutomationId(control), "ClearToolFilterButton", StringComparison.Ordinal));
        Check(filterProperty != null && focusRequestProperty != null &&
              visibleToolsProperty != null && searchBox != null && searchHint != null && clearButton != null,
            "The operational search must expose its filter state and named WPF controls.");
        Check(searchBox.MinHeight >= 42 && searchBox.ActualHeight >= 42 && searchBox.ActualWidth >= 160,
            $"The operational search field must keep a readable, keyboard-friendly size inside the responsive rail: {searchBox.ActualWidth:0.#}x{searchBox.ActualHeight:0.#} DIP (minimum {searchBox.MinHeight:0.#}).");
        Check(BindingOperations.GetBinding(searchBox, TextBox.TextProperty)?.Path?.Path == "Filter" &&
              BindingOperations.GetBinding(searchBox, FocusRequestBehavior.RequestProperty)?.Path?.Path == "SearchFocusRequest" &&
              clearButton.Style.Triggers.OfType<DataTrigger>().Any(trigger =>
                  trigger.Binding is Binding filterBinding && filterBinding.Path?.Path == "Filter" &&
                  Equals(trigger.Value, string.Empty) && trigger.Setters.OfType<Setter>().Any(setter =>
                      setter.Property == UIElement.VisibilityProperty && Equals(setter.Value, Visibility.Collapsed))),
            "The search field must stay bound to the shell, and the clear button must collapse from the existing Filter state.");
        Check(clearButton.Visibility == Visibility.Collapsed && string.IsNullOrEmpty((string)filterProperty.GetValue(shell)),
            "The clear-search action must stay hidden when the filter is empty.");
        Check(searchHint.IsVisible && !searchHint.IsHitTestVisible &&
              string.Equals(searchHint.Text, app.TryFindResource("Ui.SearchHint") as string, StringComparison.Ordinal),
            "An empty search must display a localized, non-interactive hint without intercepting text input.");

        filterProperty.SetValue(shell, "texture");
        window.Dispatcher.Invoke(() => pageHost.GetBindingExpression(ContentControl.ContentProperty)?.UpdateTarget(), DispatcherPriority.DataBind);
        Render(window);
        var localizedLabel = app.TryFindResource("Ui.ClearSearch") as string;
        Check(clearButton.Visibility == Visibility.Visible && clearButton.IsVisible && clearButton.IsEnabled &&
              !string.IsNullOrWhiteSpace(localizedLabel) &&
              string.Equals(AutomationProperties.GetName(clearButton), localizedLabel, StringComparison.Ordinal) &&
              string.Equals(clearButton.ToolTip as string, localizedLabel, StringComparison.Ordinal) &&
              clearButton.Focusable && clearButton.IsTabStop,
            "A non-empty filter must reveal a keyboard-accessible button with the localized name and tooltip.");
        Check(!searchHint.IsVisible,
            "The operational search hint must collapse as soon as the user enters a query.");
        Check(clearButton.ActualWidth >= 32 && clearButton.ActualHeight >= 32,
            "The clear-search target must remain large enough to activate without shifting the search field.");
        var searchGrid = VisualTreeHelper.GetParent(searchBox) as Grid;
        Check(searchGrid != null && ReferenceEquals(VisualTreeHelper.GetParent(clearButton), searchGrid),
            "The clear action must stay inside the search field rather than shifting the rail layout.");
        var buttonBounds = Bounds(clearButton, searchGrid);
        Check(buttonBounds.Left >= -1 && buttonBounds.Top >= -1 &&
              buttonBounds.Right <= searchGrid.ActualWidth + 1 && buttonBounds.Bottom <= searchGrid.ActualHeight + 1,
            "The clear action exceeds the search field's bounds.");

        var previousFocusRequest = (int)focusRequestProperty.GetValue(shell);
        FocusManager.SetFocusedElement(window, clearButton);
        Check(ReferenceEquals(FocusManager.GetFocusedElement(window), clearButton),
            "The clear action must receive logical focus in the non-activating synthetic window.");
        // RaiseEvent(Click) bypasses ButtonBase.OnClick and therefore does not
        // execute the bound command. Invoke the local presentation command directly
        // so this off-screen, non-activating harness never needs native focus input.
        Check(clearButton.Command != null && clearButton.Command.CanExecute(clearButton.CommandParameter),
            "The clear-search command must be executable in the rendered state.");
        clearButton.Command.Execute(clearButton.CommandParameter);
        window.Dispatcher.Invoke(() => pageHost.GetBindingExpression(ContentControl.ContentProperty)?.UpdateTarget(), DispatcherPriority.DataBind);
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        Render(window);
        Check(string.IsNullOrEmpty((string)filterProperty.GetValue(shell)) &&
              (int)focusRequestProperty.GetValue(shell) == previousFocusRequest + 1 &&
              clearButton.Visibility == Visibility.Collapsed && searchBox.Text == string.Empty && searchHint.IsVisible,
            $"Clearing search must reset the filter, hide the action, restore its hint, and issue one focus request. filter='{filterProperty.GetValue(shell)}', focusRequest={focusRequestProperty.GetValue(shell)} (expected {previousFocusRequest + 1}), visibility={clearButton.Visibility}, searchText='{searchBox.Text}', hintVisible={searchHint.IsVisible}.");
        Check(((IEnumerable)visibleToolsProperty.GetValue(shell)).Cast<object>().Count() == 194 &&
              shellType.GetProperty("CurrentPage").GetValue(shell) != null && pageHost.Content != null,
            "Clearing search must restore the complete route catalog and selected page.");
        Check(ReferenceEquals(FocusManager.GetFocusedElement(window), searchBox),
            "The clear-search command must return logical focus to the search field without activating the window.");
    }

    static void AssertHeaderActionSizing(Window window)
    {
        var expected = new Dictionary<string, (double minWidth, double maxWidth)>(StringComparer.Ordinal)
        {
            ["CommandPaletteButton"] = (140, 150),
            ["SplitDeckToggleButton"] = (112, 128),
            ["ContextDossierToggleButton"] = (34, 42)
        };
        foreach (var pair in expected)
        {
            var button = Descendants(window).OfType<ButtonBase>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), pair.Key, StringComparison.Ordinal));
            Check(button != null && button.IsVisible && button.ActualHeight >= 34 &&
                  button.ActualWidth >= pair.Value.minWidth && button.ActualWidth <= pair.Value.maxWidth,
                $"Header action {pair.Key} must use a compact, consistent activation size; actual={button?.ActualWidth:0.#}x{button?.ActualHeight:0.#} DIP.");
            foreach (var label in Descendants(button).OfType<TextBlock>().Where(block => block.IsVisible && block.ActualWidth > 0 && block.ActualHeight > 0))
            {
                var bounds = Bounds(label, button);
                Check(bounds.Left >= -1 && bounds.Top >= -1 &&
                      bounds.Right <= button.ActualWidth + 1 && bounds.Bottom <= button.ActualHeight + 1,
                    $"Header action {pair.Key} clips its visible label: {bounds} / {button.ActualWidth:0.#}x{button.ActualHeight:0.#} DIP.");
            }
        }
    }

    static void AssertStatusCardsHaveClearLabelsAndPassiveIcons(Window window, Application app)
    {
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["WorkOrderContextStatus"] = "Ui.Context",
            ["WorkOrderPermissionStatus"] = "Ui.TestPermission",
            ["WorkOrderReportStatus"] = "Ui.ReportState",
            ["WorkOrderApplicationStatus"] = "Ui.ActiveTool",
            ["RetainedEvidenceSummary"] = "Ui.RetainedEvidence"
        };

        foreach (var entry in expected)
        {
            var card = Descendants(window).OfType<Border>().SingleOrDefault(item =>
                string.Equals(AutomationProperties.GetAutomationId(item), entry.Key, StringComparison.Ordinal));
            var localizedLabel = app.TryFindResource(entry.Value) as string;
            var label = card == null || localizedLabel == null ? null : Descendants(card).OfType<TextBlock>().SingleOrDefault(item =>
                string.Equals(item.Text, localizedLabel, StringComparison.Ordinal));
            Check(card != null && card.IsVisible && label != null && label.IsVisible,
                $"Status card {entry.Key} must expose its localized label {entry.Value}.");
        }

        var statusRoot = Descendants(window).OfType<Border>().SingleOrDefault(item =>
            string.Equals(AutomationProperties.GetAutomationId(item), "WorkOrderCards", StringComparison.Ordinal));
        var decorativePaths = statusRoot == null ? Array.Empty<System.Windows.Shapes.Path>() : Descendants(statusRoot).OfType<System.Windows.Shapes.Path>().ToArray();
        Check(decorativePaths.Length >= 5 && decorativePaths.All(path => !path.IsHitTestVisible && !path.Focusable),
            "Status card iconography must remain passive and must not intercept input or keyboard focus.");

        var commandButton = Descendants(window).OfType<ButtonBase>().SingleOrDefault(item =>
            string.Equals(AutomationProperties.GetAutomationId(item), "CommandPaletteButton", StringComparison.Ordinal));
        Check(commandButton != null && commandButton.IsVisible &&
              string.Equals(AutomationProperties.GetName(commandButton), app.TryFindResource("Ui.CommandPalette") as string, StringComparison.Ordinal) &&
              Descendants(commandButton).OfType<System.Windows.Shapes.Path>().Any(path => !path.IsHitTestVisible && !path.Focusable),
            "The primary command-palette action must retain its localized accessible name and use a passive search glyph.");
    }

    static void AssertStatusIconOpacity(Application app, Window window, string themeId)
    {
        var expectedOpacity = themeId switch
        {
            "high-contrast" => 0.82,
            "parchment" => 0.56,
            _ => 0.48
        };
        var configuredOpacity = app.TryFindResource("Token.StatusIconOpacity") is double value ? value : double.NaN;
        var statusRoot = Descendants(window).OfType<Border>().SingleOrDefault(item =>
            string.Equals(AutomationProperties.GetAutomationId(item), "WorkOrderCards", StringComparison.Ordinal));
        var icons = statusRoot == null ? Array.Empty<System.Windows.Shapes.Path>() : Descendants(statusRoot).OfType<System.Windows.Shapes.Path>()
            .Where(icon => !HasAncestor<Button>(icon, statusRoot)).ToArray();
        Check(Math.Abs(configuredOpacity - expectedOpacity) < 0.001 && icons.Length == 5 &&
              icons.All(icon => Math.Abs(icon.Opacity - expectedOpacity) < 0.001 && !icon.IsHitTestVisible && !icon.Focusable),
            $"Status icons must use the {themeId} legibility token and remain passive; token={configuredOpacity}, icons={icons.Length}.");
    }

    static void AssertApplicationStatusWrapsAtMinimum(Window window)
    {
        var statusCard = Descendants(window).OfType<Border>().SingleOrDefault(border =>
            string.Equals(AutomationProperties.GetAutomationId(border), "WorkOrderApplicationStatus", StringComparison.Ordinal));
        var statusText = statusCard == null ? null : Descendants(statusCard).OfType<TextBlock>().SingleOrDefault(block =>
            BindingOperations.GetBinding(block, TextBlock.TextProperty)?.Path?.Path == "ActiveRouteStatus");
        Check(statusText != null && statusText.IsVisible && statusText.TextWrapping == TextWrapping.Wrap &&
              statusText.TextTrimming == TextTrimming.CharacterEllipsis && statusText.MaxHeight >= 30,
            "The application status card must wrap long route names at the minimum window width and keep the full value available through its tooltip.");
        if (!string.IsNullOrEmpty(statusText?.Text) && statusText.Text.Length >= 20)
            Check(statusText.ActualHeight > statusText.FontSize * 1.25,
                $"The long application status must use more than one line instead of clipping at minimum width; actual height {statusText.ActualHeight:0.#} DIP, text '{statusText.Text}'.");
    }

    static void AssertFooterFitsShellViewport(Window window)
    {
        var shell = Descendants(window).OfType<FrameworkElement>().SingleOrDefault(element =>
            element.GetType().FullName == "CalradiaForge.Desktop.Presentation.WorkbenchShellView");
        var layout = shell == null ? null : Descendants(shell).OfType<Grid>().SingleOrDefault(grid =>
            grid.Margin == new Thickness(14, 8, 14, 12));
        var footer = Descendants(window).OfType<TextBlock>().SingleOrDefault(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "KeyboardShortcutHint", StringComparison.Ordinal));
        Check(shell != null && layout != null && footer != null && footer.IsVisible && footer.ActualWidth > 0 && footer.ActualHeight > 0,
            "The keyboard-shortcut footer and its shell viewport must remain visible at minimum window size.");
        if (layout == null || footer == null) return;

        var bounds = Bounds(footer, layout);
        Check(bounds.Left >= -1 && bounds.Top >= -1 && bounds.Right <= layout.ActualWidth + 1 && bounds.Bottom <= layout.ActualHeight + 1,
            $"The keyboard-shortcut footer must fit inside the shell's content viewport at 980x680 DIP: {bounds} / {layout.ActualWidth:0.#}x{layout.ActualHeight:0.#} DIP.");
        Check(layout.ActualHeight - bounds.Bottom >= 8 && footer.Margin.Bottom >= 2 && footer.MinHeight >= 18,
            $"The localized shortcut footer must preserve its lower inset instead of sitting on the clipped shell edge: inset={layout.ActualHeight - bounds.Bottom:0.#} DIP, margin={footer.Margin.Bottom:0.#}, minHeight={footer.MinHeight:0.#}.");

        var pixelsPerDip = VisualTreeHelper.GetDpi(footer).PixelsPerDip;
        var measuredText = new FormattedText(footer.Text ?? string.Empty, CultureInfo.CurrentUICulture, footer.FlowDirection,
            new Typeface(footer.FontFamily, footer.FontStyle, footer.FontWeight, footer.FontStretch), footer.FontSize,
            footer.Foreground, pixelsPerDip)
        {
            MaxTextWidth = Math.Max(1, footer.ActualWidth)
        };
        Check(measuredText.Height <= footer.ActualHeight + 1,
            $"The complete localized shortcut hint must fit inside its own text bounds at minimum window size: desired {measuredText.Height:0.#}, actual {footer.ActualHeight:0.#} DIP.");
    }

    static void AssertHeaderLabelsFitAtNormalWidth(Application app, Window window)
    {
        var themeSelector = Descendants(window).OfType<ComboBox>().SingleOrDefault(control =>
            string.Equals(AutomationProperties.GetAutomationId(control), "ThemeSelector", StringComparison.Ordinal));
        var themeLabel = themeSelector == null ? null : Descendants(themeSelector).OfType<TextBlock>().SingleOrDefault(block =>
            string.Equals(block.Text, app.TryFindResource("Ui.ThemeParchment") as string, StringComparison.Ordinal));
        var sessionStatus = Descendants(window).OfType<TextBlock>().SingleOrDefault(block =>
            string.Equals(AutomationProperties.GetAutomationId(block), "SessionConnectionStatus", StringComparison.Ordinal));

        static double RequiredTextWidth(TextBlock block)
        {
            var typeface = new Typeface(block.FontFamily, block.FontStyle, block.FontWeight, block.FontStretch);
            var pixelsPerDip = VisualTreeHelper.GetDpi(block).PixelsPerDip;
            return new FormattedText(block.Text ?? string.Empty, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                typeface, block.FontSize, block.Foreground, pixelsPerDip).WidthIncludingTrailingWhitespace;
        }

        Check(themeSelector != null && themeLabel != null && themeLabel.IsVisible && themeSelector.ActualWidth >= 150,
            $"At normal width, the Parchment Light selector must be realized in the widened theme group; selector={themeSelector?.ActualWidth:0.#}, label={themeLabel?.Text ?? "<missing>"}, visible={themeLabel?.IsVisible}.");
        Check(themeLabel.ActualWidth + 1 >= RequiredTextWidth(themeLabel),
            $"The Parchment Light selector label is clipped at normal width: actual {themeLabel.ActualWidth:0.#} DIP, required {RequiredTextWidth(themeLabel):0.#} DIP.");
        Check(sessionStatus != null && sessionStatus.IsVisible && sessionStatus.Text == "Disconnected" &&
              sessionStatus.ActualWidth + 1 >= RequiredTextWidth(sessionStatus),
            $"The disconnected session status is clipped at normal width: actual {sessionStatus?.ActualWidth:0.#} DIP, required {(sessionStatus == null ? 0 : RequiredTextWidth(sessionStatus)):0.#} DIP.");
    }

    static void AssertConnectionIndicatorVisualStates(Application app, Window window, object shell, string themeId)
    {
        var indicators = Descendants(window).OfType<System.Windows.Shapes.Ellipse>()
            .Where(indicator => string.Equals(AutomationProperties.GetAutomationId(indicator), "SessionConnectionIndicator", StringComparison.Ordinal))
            .ToArray();
        Check(indicators.Length == 1,
            $"Theme {themeId} must expose exactly one SessionConnectionIndicator in the header.");

        var indicator = indicators[0];
        var session = shell.GetType().GetProperty("Session")?.GetValue(shell);
        var connection = session?.GetType().GetProperty("Connection");
        Check(session != null && connection?.CanRead == true && connection.CanWrite,
            "The connection indicator test needs the existing session presentation state without changing its contract.");
        Check(!indicator.Focusable && !indicator.IsHitTestVisible && indicator.ActualWidth > 0 && indicator.ActualHeight > 0,
            "The connection status indicator must remain visible and passive.");

        var originalState = connection.GetValue(session) as string;
        var states = new[]
        {
            (Name: "Disconnected", BrushKey: "EmberBrush"),
            (Name: "Connecting", BrushKey: "BrassBrush"),
            (Name: "Connected", BrushKey: "VerdigrisBrush")
        };

        try
        {
            foreach (var state in states)
            {
                connection.SetValue(session, state.Name);
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);

                var expected = app.TryFindResource(state.BrushKey) as SolidColorBrush;
                var actual = indicator.Fill as SolidColorBrush;
                Check(expected != null && actual != null && actual.Color == expected.Color,
                    $"Theme {themeId} must render {state.Name} with {state.BrushKey}; actual fill was {indicator.Fill}.");
            }
        }
        finally
        {
            connection.SetValue(session, originalState);
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
        }
    }

    static bool PumpDispatcherUntil(Dispatcher dispatcher, Func<bool> condition, TimeSpan timeout)
    {
        var elapsed = Stopwatch.StartNew();
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(10)
        };
        timer.Tick += (_, _) =>
        {
            if (condition() || elapsed.Elapsed >= timeout)
            {
                timer.Stop();
                frame.Continue = false;
            }
        };
        timer.Start();
        try
        {
            Dispatcher.PushFrame(frame);
        }
        finally
        {
            timer.Stop();
        }
        return condition();
    }

    static void AssertLongLocalizedTextLayout(Application app, Window window, string languageCode)
    {
        var longKeys = new[] { "Ui.Subtitle", "Ui.TestPermission", "Ui.RetainedEvidence", "Ui.EmptyEvidence" };
        // TechnicalEvidence belongs to the Evidence tab and must not be required in the
        // currently visible tree when the Raw Result tab is selected. Its catalog entry
        // is still required in every locale; the report-tab test checks its rendered view.
        var fixedLabelKeys = new[] { "Ui.Operations" };
        Check(!string.IsNullOrWhiteSpace(app.TryFindResource("Ui.ToolsVisible") as string),
            "The active localized catalog must provide the virtualized footer label Ui.ToolsVisible for " + languageCode + ".");
        Check(!string.IsNullOrWhiteSpace(app.TryFindResource("Ui.EmptyPinned") as string),
            "The active localized catalog must provide the virtualized empty-pinned label Ui.EmptyPinned for " + languageCode + ".");
        Check(!string.IsNullOrWhiteSpace(app.TryFindResource("Ui.ClearSearch") as string),
            "The active localized catalog must provide the clear-search accessible label Ui.ClearSearch for " + languageCode + ".");
        var visibleText = Descendants(window).OfType<TextBlock>()
            .Where(block => block.Visibility == Visibility.Visible && block.IsVisible && block.ActualWidth > 0 && block.ActualHeight > 0)
            .ToArray();
        foreach (var key in fixedLabelKeys)
        {
            var expected = app.TryFindResource(key) as string;
            Check(!string.IsNullOrWhiteSpace(expected) && visibleText.Any(block => string.Equals(block.Text, expected, StringComparison.Ordinal)),
                "Visible label " + key + " did not render from the active localized catalog " + languageCode + ".");
        }
        foreach (var key in new[] { "Ui.TechnicalEvidence", "Ui.ShortcutHint" })
            Check(!string.IsNullOrWhiteSpace(app.TryFindResource(key) as string),
                "The active localized catalog must provide " + key + " for " + languageCode + ".");
        var shortcutHintText = app.TryFindResource("Ui.ShortcutHint") as string;
        var shortcutHint = Descendants(window).OfType<TextBlock>().SingleOrDefault(block =>
            string.Equals(AutomationProperties.GetAutomationId(block), "KeyboardShortcutHint", StringComparison.Ordinal));
        Check(shortcutHint != null && shortcutHint.Visibility == Visibility.Visible && shortcutHint.IsVisible &&
              shortcutHint.ActualWidth > 0 && shortcutHint.ActualHeight > 0 &&
              string.Equals(shortcutHint.Text, shortcutHintText, StringComparison.Ordinal),
            "The localized Ui.ShortcutHint must render visibly in the footer from the active catalog " + languageCode + ".");
        var checkedTexts = 0;
        var compactScript = languageCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(languageCode, "ja", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(languageCode, "ko", StringComparison.OrdinalIgnoreCase);
        foreach (var key in longKeys)
        {
            var value = app.TryFindResource(key) as string;
            Check(!string.IsNullOrWhiteSpace(value), "The active " + languageCode + " catalog has no value for " + key + ".");
            var matching = visibleText.Where(block => string.Equals(block.Text, value, StringComparison.Ordinal)).ToArray();
            // CJK labels use fewer characters for the same visual width; sample them at a lower threshold.
            if (value.Length < (compactScript ? 8 : 20)) continue;
            Check(matching.Length > 0, "Long localized label " + key + " is not present in the rendered layout for " + languageCode + ".");
            foreach (var block in matching)
            {
                AssertTextFitsWrapsOrTrims(block, key, languageCode);
                checkedTexts++;
            }
        }
        Check(checkedTexts > 0, "No long localized labels were measured for " + languageCode + ".");
    }

    static void AssertLongPageLabels(Window window, ToolDefinition tool, string themeId)
    {
        var visibleText = Descendants(window).OfType<TextBlock>()
            .Where(block => block.Visibility == Visibility.Visible && block.IsVisible && block.ActualWidth > 0 && block.ActualHeight > 0)
            .ToArray();
        foreach (var (name, value) in new[] { ("tool title", tool.Title), ("tool purpose", tool.Purpose), ("availability reason", tool.AvailabilityReason) })
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 20) continue;
            var matching = visibleText.Where(block => string.Equals(block.Text, value, StringComparison.Ordinal)).ToArray();
            Check(matching.Length > 0, $"The {name} for {tool.Id} disappeared from the fixed-DIP layout in theme {themeId}.");
            foreach (var block in matching)
                AssertTextFitsWrapsOrTrims(block, name, themeId);
        }
    }

    static void AssertTextFitsWrapsOrTrims(TextBlock block, string label, string context)
    {
        if (block.TextWrapping == TextWrapping.Wrap || block.TextTrimming != TextTrimming.None) return;
        var pixelsPerDip = VisualTreeHelper.GetDpi(block).PixelsPerDip;
        var measured = new FormattedText(block.Text ?? string.Empty, CultureInfo.CurrentCulture, block.FlowDirection,
            new Typeface(block.FontFamily, block.FontStyle, block.FontWeight, block.FontStretch), block.FontSize,
            Brushes.Black, pixelsPerDip);
        Check(measured.WidthIncludingTrailingWhitespace <= block.ActualWidth + 1.0,
            $"Long label '{label}' has no wrapping or trimming and exceeds its {block.ActualWidth:0.#} DIP layout width in {context} at the host's fixed WPF DPI.");
    }

    static void AssertHeaderControlBounds(Window window, Application app, string languageCode)
    {
        var surface = (FrameworkElement)window.Content;
        var bounds = Bounds(surface, window);
        Check(bounds.Left >= -1 && bounds.Top >= -1 && bounds.Right <= window.ActualWidth + 2 && bounds.Bottom <= window.ActualHeight + 2,
            $"The fixed-DIP Desktop surface exceeds its host at language {languageCode}.");

        var titleValue = app.TryFindResource("Ui.AppTitle") as string;
        var title = Descendants(window).OfType<TextBlock>().FirstOrDefault(block => string.Equals(block.Text, titleValue, StringComparison.Ordinal));
        Check(title != null, "The localized application title is missing at language " + languageCode + ".");
        var searchBox = Descendants(window).OfType<TextBox>().SingleOrDefault(control =>
            string.Equals(AutomationProperties.GetAutomationId(control), "OperationalSearchFilter", StringComparison.Ordinal));
        var searchHint = app.TryFindResource("Ui.SearchHint") as string;
        Check(searchBox != null && string.Equals(searchBox.ToolTip as string, searchHint, StringComparison.Ordinal),
            "The operational search must expose its complete localized hint even when the placeholder is ellipsized at " + languageCode + ".");
        var titleBounds = Bounds(title, window);
        var headerSelectors = Descendants(window).OfType<ComboBox>()
            .Where(selector => HasBindingPath(selector, ComboBox.SelectedItemProperty, "LanguageCode") ||
                              HasBindingPath(selector, ComboBox.SelectedValueProperty, "ThemeId"))
            .ToArray();
        Check(headerSelectors.Length >= 2, "The language and theme selectors must remain in the adaptive header.");
        foreach (var selector in headerSelectors)
            Check(!titleBounds.IntersectsWith(Bounds(selector, window)),
                $"The localized title overlaps a header selector at language {languageCode}.");
        var accentsToggle = Descendants(window).OfType<CheckBox>().SingleOrDefault(control =>
            string.Equals(AutomationProperties.GetAutomationId(control), "DecorativeAccentsToggle", StringComparison.Ordinal));
        Check(accentsToggle != null && accentsToggle.IsVisible && accentsToggle.ActualWidth > 0 && accentsToggle.ActualHeight > 0,
            $"The localized decorative accents toggle must retain visible space at language {languageCode}.");
        var toggleBounds = Bounds(accentsToggle, window);
        Check(!titleBounds.IntersectsWith(toggleBounds) && headerSelectors.All(selector => !toggleBounds.IntersectsWith(Bounds(selector, window))),
            $"The localized application title or a header selector overlaps the decorative accents toggle at {languageCode}.");
        Check(toggleBounds.Left >= -1 && toggleBounds.Top >= -1 && toggleBounds.Right <= window.ActualWidth + 2 && toggleBounds.Bottom <= window.ActualHeight + 2,
            $"The decorative accents toggle extends outside the Desktop window at {languageCode}: {toggleBounds}.");

        var subtitleValue = app.TryFindResource("Ui.Subtitle") as string;
        var subtitle = Descendants(window).OfType<TextBlock>().SingleOrDefault(block =>
            string.Equals(block.Text, subtitleValue, StringComparison.Ordinal));
        var brandGroup = Descendants(window).OfType<FrameworkElement>().SingleOrDefault(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "HeaderBrandGroup", StringComparison.Ordinal));
        Check(subtitle != null && brandGroup != null && subtitle.TextWrapping == TextWrapping.NoWrap && subtitle.TextTrimming == TextTrimming.CharacterEllipsis &&
              string.Equals(subtitle.ToolTip as string, subtitleValue, StringComparison.Ordinal),
            $"The localized subtitle must stay on one line with ellipsis and expose its full text as a tooltip at {languageCode}.");
        var subtitleBounds = Bounds(subtitle, brandGroup);
        Check(subtitleBounds.Left >= -1 && subtitleBounds.Top >= -1 &&
              subtitleBounds.Right <= brandGroup.ActualWidth + 1 && subtitleBounds.Bottom <= brandGroup.ActualHeight + 1,
            $"The localized subtitle is clipped outside the brand group at {languageCode}: {subtitleBounds} / {brandGroup.ActualWidth:0.#}x{brandGroup.ActualHeight:0.#}.");

        var categorySelector = Descendants(window).OfType<ComboBox>().SingleOrDefault(control =>
            string.Equals(AutomationProperties.GetAutomationId(control), "CategorySelector", StringComparison.Ordinal));
        var allAreasLabel = categorySelector?.Items.Count > 0 ? categorySelector.Items[0] as string : null;
        Check(categorySelector != null && allAreasLabel != null && categorySelector.SelectedIndex == 0 &&
              string.Equals(allAreasLabel, "All areas", StringComparison.Ordinal) &&
              string.Equals(categorySelector.SelectedItem as string, allAreasLabel, StringComparison.Ordinal) &&
              string.Equals(AutomationProperties.GetName(categorySelector), app.TryFindResource("Ui.Areas") as string, StringComparison.Ordinal) &&
              string.Equals(categorySelector.ToolTip as string, app.TryFindResource("Ui.Areas") as string, StringComparison.Ordinal),
            $"The localized category filter must show its initial all-areas selection and accessible label at {languageCode}.");

        var mainSectionsViewport = Descendants(window).OfType<ListBox>().SingleOrDefault(viewport =>
            string.Equals(AutomationProperties.GetAutomationId(viewport), "OperationalRailToolViewport", StringComparison.Ordinal));
        Check(mainSectionsViewport != null && mainSectionsViewport.ActualWidth > 0 && mainSectionsViewport.ActualHeight > 0,
            $"The operational rail's tool-list viewport (OperationalRailToolViewport) must retain a positive visible area at language {languageCode}; actual rect {mainSectionsViewport?.ActualWidth:0.#}x{mainSectionsViewport?.ActualHeight:0.#}.");

        foreach (var viewport in Descendants(window).OfType<ScrollViewer>().Where(item => item.Visibility == Visibility.Visible && item.IsVisible))
        {
            if (HasAncestor<ScrollViewer>(viewport, surface)) continue;
            var viewportBounds = Bounds(viewport, surface);
            Check(viewportBounds.Left >= -1 && viewportBounds.Top >= -1 && viewportBounds.Right <= surface.ActualWidth + 2 && viewportBounds.Bottom <= surface.ActualHeight + 2,
                $"Visible scroll viewport {DescribeScrollViewer(viewport, surface)} extends outside the Desktop surface at language {languageCode}: {viewportBounds} / {surface.ActualWidth:0.#}x{surface.ActualHeight:0.#}; ancestors {DescribeVisualAncestors(viewport, surface)}.");
        }
        foreach (var block in Descendants(window).OfType<TextBlock>().Where(item => item.Visibility == Visibility.Visible && item.IsVisible))
        {
            if (block.ActualWidth <= 0 || block.ActualHeight <= 0 || HasAncestor<ScrollViewer>(block, surface)) continue;
            var blockBounds = Bounds(block, surface);
            Check(blockBounds.Left >= -1 && blockBounds.Top >= -1 && blockBounds.Right <= surface.ActualWidth + 2 && blockBounds.Bottom <= surface.ActualHeight + 2,
                $"A visible label extends outside the Desktop surface at language {languageCode}: {block.Text}; bounds {blockBounds}, surface {surface.ActualWidth:0.#}x{surface.ActualHeight:0.#}.");
        }
    }

    static void AssertHookWorkbenchShellIntegration(Application app, Window window, object shell, string artifactDirectory)
    {
        var shellType = shell.GetType();
        var hookOpen = shellType.GetProperty("IsHookWorkbenchOpen");
        Check(hookOpen != null && hookOpen.CanWrite, "The isolated render shell must expose the existing hook-workbench view state.");
        var hookViewport = Descendants(window).OfType<ScrollViewer>().SingleOrDefault(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "HookWorkbenchScrollViewport", StringComparison.Ordinal));
        var hookPage = hookViewport?.Content as HookWorkbenchControl;
        var normalViewport = Descendants(window).OfType<ScrollViewer>().SingleOrDefault(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "ResponsiveWorkbenchScrollViewport", StringComparison.Ordinal));
        var navigationButton = Descendants(window).OfType<Button>().SingleOrDefault(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "HookWorkbenchNavigationButton", StringComparison.Ordinal));
        var headerHookButton = Descendants(window).OfType<Button>().FirstOrDefault(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "HookWorkbenchOpenButton", StringComparison.Ordinal));
        Check(hookViewport != null && hookPage != null && normalViewport != null && navigationButton != null && headerHookButton == null,
            "Hook Workbench must be a dedicated navigation route, not a global header overlay; its page must remain a separate scrollable surface.");
        var selectedToolProperty = shellType.GetProperty("SelectedTool");
        var originalSelectedTool = selectedToolProperty?.GetValue(shell);
        var railEntries = shellType.GetProperty("OperationalRailEntries")?.GetValue(shell) as System.Collections.IEnumerable;
        var originalToolEntry = railEntries?.Cast<object>().FirstOrDefault(entry =>
            ReferenceEquals(entry.GetType().GetProperty("Tool")?.GetValue(entry), originalSelectedTool));
        var originalToolEntrySelected = originalToolEntry?.GetType().GetProperty("IsSelected");
        var activeRouteStatusText = Descendants(window).OfType<TextBlock>().SingleOrDefault(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "ActiveRouteStatusText", StringComparison.Ordinal));
        Check(originalToolEntrySelected != null && activeRouteStatusText != null,
            "The shell must expose the selected rail entry and active-route status for route-state verification.");

        var themes = new[] { "war-table", "parchment", "high-contrast" };
        var languages = new[] { "en", "es", "pt", "de", "fr", "it", "pl", "ru", "tr", "zh-HANS", "zh-HANT", "ja", "ko" };
        Directory.CreateDirectory(artifactDirectory);
        var savedOpenPreview = false;

        foreach (var themeId in themes)
        foreach (var languageCode in languages)
        {
            SetPresentationForRender(app, shell, themeId, languageCode);
            window.Width = window.MinWidth;
            window.Height = window.MinHeight;
            RefreshSelectorBindings(window);

            hookOpen.SetValue(shell, false);
            Render(window);
            Check(hookViewport.Visibility == Visibility.Collapsed && !hookViewport.IsVisible && hookViewport.ActualWidth <= 0.1,
                $"The hook workbench must be collapsed and absent from hit testing while closed ({themeId}/{languageCode}).");
            Check(normalViewport.Visibility == Visibility.Visible && normalViewport.IsVisible && normalViewport.ActualWidth > 0,
                $"The regular tool route must remain visible while the hook workbench is closed ({themeId}/{languageCode}).");

            Check(navigationButton.Command != null && navigationButton.Command.CanExecute(navigationButton.CommandParameter),
                $"The dedicated Hook Workbench route must be enabled before navigation ({themeId}/{languageCode}).");
            navigationButton.Command.Execute(navigationButton.CommandParameter);
            Check((bool)hookOpen.GetValue(shell),
                $"The dedicated navigation item must open Hook Workbench ({themeId}/{languageCode}).");
            Render(window);
            Check(hookViewport.Visibility == Visibility.Visible && hookViewport.IsVisible && hookViewport.ActualWidth > 0 && hookViewport.ActualHeight > 0,
                $"The dedicated Hook Workbench navigation route must open in the available shell area ({themeId}/{languageCode}).");
            Check(normalViewport.Visibility == Visibility.Collapsed && !normalViewport.IsVisible && normalViewport.ActualWidth <= 0.1,
                $"Only the selected Hook Workbench route may occupy the workspace; the other route remains available from navigation ({themeId}/{languageCode}).");
            Check(navigationButton.IsVisible && navigationButton.IsEnabled,
                $"Hook Workbench navigation must remain visible while its dedicated route is selected ({themeId}/{languageCode}).");
            Check(Equals(navigationButton.BorderBrush, app.TryFindResource("BrassBrush")),
                $"The selected Hook Workbench route must receive its brass active-state border ({themeId}/{languageCode}).");
            Check(ReferenceEquals(selectedToolProperty?.GetValue(shell), originalSelectedTool),
                $"Opening Hook Workbench must preserve the previously selected tool ({themeId}/{languageCode}).");
            Check(!(bool)originalToolEntrySelected.GetValue(originalToolEntry),
                $"The remembered tool must not appear active at the same time as Hook Workbench ({themeId}/{languageCode}).");
            Check(string.Equals(activeRouteStatusText.Text, app.TryFindResource("Ui.HookWorkbench") as string, StringComparison.Ordinal),
                $"The status strip must identify Hook Workbench as the active route ({themeId}/{languageCode}).");
            var catalog = shellType.GetProperty("Catalog")?.GetValue(shell);
            var catalogTools = catalog?.GetType().GetProperty("Tools")?.GetValue(catalog);
            var catalogCount = (int?)catalogTools?.GetType().GetProperty("Count")?.GetValue(catalogTools);
            Check(catalogCount == 194,
                $"Opening Hook Workbench must preserve all 194 registered routes ({themeId}/{languageCode}).");
            Check(hookPage.IsVisible && hookPage.ActualWidth > 0 && hookPage.ActualHeight > 0 &&
                  hookPage.ActualWidth <= hookViewport.ActualWidth + 1,
                $"The hook-workbench content must remain inside its responsive viewport ({themeId}/{languageCode}); page={hookPage.ActualWidth:0.#}x{hookPage.ActualHeight:0.#}, viewport={hookViewport.ActualWidth:0.#}x{hookViewport.ActualHeight:0.#}.");
            Check(hookViewport.ScrollableWidth <= 1,
                $"The hook-workbench surface must not introduce horizontal overflow ({themeId}/{languageCode}).");

            foreach (var automationId in new[]
            {
                "HookWorkbenchCloseButton", "HookEligibilityMessage", "HookOwnerFilter", "HookTargetFilter",
                "HookTypeFilter", "RegisteredHooksViewport", "HookSelectionStatus", "HookWorkbenchStatus"
            })
            {
                var element = Descendants(hookPage).OfType<FrameworkElement>().SingleOrDefault(candidate =>
                    string.Equals(AutomationProperties.GetAutomationId(candidate), automationId, StringComparison.Ordinal));
                Check(element != null && element.IsVisible && element.ActualWidth > 0 && element.ActualHeight > 0,
                    $"The open hook workbench lost its {automationId} control in {themeId}/{languageCode}.");
                var bounds = Bounds(element, hookPage);
                Check(bounds.Left >= -1 && bounds.Top >= -1 && bounds.Right <= hookPage.ActualWidth + 2 && bounds.Bottom <= hookPage.ActualHeight + 2,
                    $"Hook-workbench control {automationId} exceeds its own content surface in {themeId}/{languageCode}: {bounds}.");
            }

            var emptyInventory = Descendants(hookPage).OfType<TextBlock>().SingleOrDefault(block =>
                BindingOperations.GetBinding(block, TextBlock.TextProperty)?.Path?.Path == "EmptyInventoryMessage");
            Check(emptyInventory != null && emptyInventory.IsVisible && !string.IsNullOrWhiteSpace(emptyInventory.Text),
                $"The hook-workbench empty inventory must remain visible and localized in {themeId}/{languageCode}.");

            var closeButton = Descendants(hookPage).OfType<Button>().Single(element =>
                string.Equals(AutomationProperties.GetAutomationId(element), "HookWorkbenchCloseButton", StringComparison.Ordinal));
            var closeBounds = Bounds(closeButton, window);
            var viewportBounds = Bounds(hookViewport, window);
            var center = new Point(closeBounds.Left + closeBounds.Width / 2, closeBounds.Top + closeBounds.Height / 2);
            Check(viewportBounds.Contains(center) && closeButton.IsHitTestVisible &&
                  HasVisualAncestorOrSelf(VisualTreeHelper.HitTest(window, center)?.VisualHit, closeButton),
                $"The visible hook close control must receive hit testing instead of the hidden tool route ({themeId}/{languageCode}).");

            if (!savedOpenPreview && themeId == "war-table" && languageCode == "en")
            {
                SavePreview(window, Path.Combine(artifactDirectory, "desktop-hook-workbench-open-minimum-current.png"));
                savedOpenPreview = true;
            }
        }

        SetPresentationForRender(app, shell, "war-table", "en");
        window.Width = 1360;
        window.Height = 820;
        RefreshSelectorBindings(window);
        navigationButton.Command?.Execute(null);
        Render(window);
        Check(hookViewport.IsVisible && hookPage.ActualWidth > 0 && hookPage.ActualWidth <= hookViewport.ActualWidth + 1,
            "The hook workbench must also remain responsive at the normal 1360x820-DIP viewport.");
        var closeButtonAtNormalSize = Descendants(hookPage).OfType<Button>().Single(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "HookWorkbenchCloseButton", StringComparison.Ordinal));
        closeButtonAtNormalSize.Command?.Execute(null);
        Render(window);
        Check(normalViewport.IsVisible && !hookViewport.IsVisible,
            "Closing the dedicated Hook Workbench route must restore the previously selected regular route.");
        Check(ReferenceEquals(selectedToolProperty?.GetValue(shell), originalSelectedTool),
            "Opening and closing Hook Workbench must not replace or clear the selected tool route.");
        Check((bool)originalToolEntrySelected.GetValue(originalToolEntry),
            "Closing Hook Workbench must restore the previous tool's active rail marker.");

        var visibleTools = shellType.GetProperty("VisibleTools")?.GetValue(shell) as IEnumerable<ToolDefinition>;
        var alternateTool = visibleTools?.FirstOrDefault(tool => !ReferenceEquals(tool, originalSelectedTool));
        var selectToolCommand = shellType.GetProperty("SelectToolCommand")?.GetValue(shell) as RelayCommand;
        Check(alternateTool != null && selectToolCommand != null,
            "The render shell must expose a second regular route for navigation restoration checks.");
        navigationButton.Command?.Execute(null);
        Check((bool)hookOpen.GetValue(shell), "Hook Workbench must open before switching back to a regular route.");
        shellType.GetProperty("Filter")?.SetValue(shell, "__hook_route_no_match__");
        Render(window);
        Check((bool)hookOpen.GetValue(shell) && ReferenceEquals(selectedToolProperty.GetValue(shell), originalSelectedTool) &&
              hookViewport.IsVisible && !normalViewport.IsVisible,
            "Changing the rail filter, including to an empty result, must not dismiss or replace the dedicated Hook Workbench route.");
        shellType.GetProperty("Filter")?.SetValue(shell, string.Empty);
        Render(window);
        var rememberedToolButton = Descendants(window).OfType<Button>().SingleOrDefault(button =>
            ReferenceEquals(button.Command, selectToolCommand) && ReferenceEquals(button.CommandParameter, originalSelectedTool));
        Check(rememberedToolButton != null,
            "The remembered regular route must remain selectable from its realized rail button.");
        rememberedToolButton.Command.Execute(rememberedToolButton.CommandParameter);
        Render(window);
        Check(!(bool)hookOpen.GetValue(shell) && normalViewport.IsVisible && !hookViewport.IsVisible &&
              ReferenceEquals(selectedToolProperty.GetValue(shell), originalSelectedTool) &&
              (bool)originalToolEntrySelected.GetValue(originalToolEntry),
            "Selecting the already remembered tool from navigation must close Hook Workbench and restore its selected state.");

        navigationButton.Command?.Execute(null);
        Render(window);
        var alternateToolButton = Descendants(window).OfType<Button>().SingleOrDefault(button =>
            ReferenceEquals(button.Command, selectToolCommand) && ReferenceEquals(button.CommandParameter, alternateTool));
        Check(alternateToolButton != null,
            "The alternate regular route must remain available through its realized rail button while Hook Workbench is selected.");
        alternateToolButton.Command.Execute(alternateToolButton.CommandParameter);
        Render(window);
        Check(!(bool)hookOpen.GetValue(shell) && normalViewport.IsVisible && !hookViewport.IsVisible &&
              ReferenceEquals(selectedToolProperty.GetValue(shell), alternateTool),
            "Selecting a regular tool from the navigation rail must leave Hook Workbench and display that tool without deleting either route.");

        navigationButton.Command?.Execute(null);
        Check((bool)hookOpen.GetValue(shell), "Hook Workbench must open before closing it with an unmatched rail filter.");
        shellType.GetProperty("Filter")?.SetValue(shell, "__hook_route_no_match__");
        Render(window);
        Check(((IEnumerable)shellType.GetProperty("VisibleTools").GetValue(shell)).Cast<object>().Count() == 0,
            "The unmatched rail filter must produce no regular routes while Hook Workbench remains selected.");
        var closeAfterEmptyFilter = Descendants(hookPage).OfType<Button>().Single(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "HookWorkbenchCloseButton", StringComparison.Ordinal));
        closeAfterEmptyFilter.Command?.Execute(null);
        Render(window);
        Check(!(bool)hookOpen.GetValue(shell) && normalViewport.IsVisible && !hookViewport.IsVisible &&
              selectedToolProperty.GetValue(shell) == null,
            "Closing Hook Workbench with no visible regular routes must not leave an inaccessible remembered page selected.");
        shellType.GetProperty("Filter")?.SetValue(shell, string.Empty);
        Render(window);
        var restoredSelection = selectedToolProperty.GetValue(shell);
        Check(restoredSelection != null &&
              ((IEnumerable)shellType.GetProperty("VisibleTools").GetValue(shell)).Cast<object>().Contains(restoredSelection) &&
              railEntries.Cast<object>().Any(entry => ReferenceEquals(entry.GetType().GetProperty("Tool")?.GetValue(entry), restoredSelection) &&
                  (bool)entry.GetType().GetProperty("IsSelected").GetValue(entry)),
            "Clearing the unmatched filter after closing Hook Workbench must restore a visible, active regular route.");
    }

    static bool HasVisualAncestorOrSelf(DependencyObject node, DependencyObject expectedAncestor)
    {
        while (node != null)
        {
            if (ReferenceEquals(node, expectedAncestor)) return true;
            node = VisualTreeHelper.GetParent(node);
        }
        return false;
    }

    static object AssertOperationalRailVirtualization(Application app, Window window, object shell)
    {
        var rail = Descendants(window).OfType<ListBox>().SingleOrDefault(view =>
            string.Equals(AutomationProperties.GetAutomationId(view), "OperationalRailToolViewport", StringComparison.Ordinal));
        Check(rail != null, "The operational rail must expose its virtualized ListBox as OperationalRailToolViewport.");

        var sourceBinding = BindingOperations.GetBinding(rail, ItemsControl.ItemsSourceProperty);
        Check(sourceBinding?.Path?.Path == "OperationalRailEntries" && rail.ItemsSource != null,
            "The operational rail ListBox must bind its flattened ItemsSource to OperationalRailEntries.");

        var catalog = shell.GetType().GetProperty("Catalog").GetValue(shell);
        var catalogTools = ((IEnumerable)catalog.GetType().GetProperty("Tools").GetValue(catalog)).Cast<object>().ToArray();
        var expectedToolCount = catalogTools.Length;
        var expectedGroupCount = catalogTools.Select(tool => tool.GetType().GetProperty("Group")?.GetValue(tool))
            .Where(group => group != null).Distinct().Count();
        var entryTypes = rail.Items.Cast<object>().GroupBy(item => item.GetType().Name)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var toolRows = entryTypes.TryGetValue("OperationalRailToolEntryViewModel", out var routeCount) ? routeCount : 0;
        var groupRows = entryTypes.TryGetValue("OperationalRailGroupEntryViewModel", out var groupCount) ? groupCount : 0;
        var footerRows = entryTypes.TryGetValue("OperationalRailFooterEntryViewModel", out var footerCount) ? footerCount : 0;
        Check(toolRows == expectedToolCount && groupRows == expectedGroupCount && footerRows == 1,
            $"The expanded operational rail must expose all {expectedToolCount} tool rows, {expectedGroupCount} group headers, and one footer row; actual rows: tools={toolRows}, groups={groupRows}, footers={footerRows}.");
        var activeSelectedTool = shell.GetType().GetProperty("SelectedTool")?.GetValue(shell);
        var selectedRailEntries = rail.Items.Cast<object>()
            .Where(item => item.GetType().Name == "OperationalRailToolEntryViewModel" &&
                item.GetType().GetProperty("IsSelected")?.GetValue(item) is true)
            .ToArray();
        Check(selectedRailEntries.Length == 1 &&
              ReferenceEquals(selectedRailEntries[0].GetType().GetProperty("Tool")?.GetValue(selectedRailEntries[0]), activeSelectedTool),
            "The rail must expose exactly one selected route matching the active tool.");

        var railVisuals = Descendants(rail).ToArray();
        var panel = railVisuals.OfType<VirtualizingStackPanel>().FirstOrDefault();
        Check(panel != null && VirtualizingPanel.GetIsVirtualizing(rail) &&
              VirtualizingPanel.GetVirtualizationMode(rail) == VirtualizationMode.Recycling,
            "The operational rail must use an active VirtualizingStackPanel with recycling enabled.");
        Check(ScrollViewer.GetCanContentScroll(rail) &&
              ScrollViewer.GetVerticalScrollBarVisibility(rail) == ScrollBarVisibility.Auto,
            "The operational rail must use logical content scrolling with an automatic vertical scrollbar.");

        var mainScrollViewer = railVisuals.OfType<ScrollViewer>().SingleOrDefault(view =>
            FindAncestor<ListBoxItem>(view, rail) == null);
        Check(mainScrollViewer != null && mainScrollViewer.IsVisible && mainScrollViewer.ActualHeight > 0 &&
              mainScrollViewer.ScrollableHeight > 0 &&
              ScrollViewer.GetVerticalScrollBarVisibility(mainScrollViewer) == ScrollBarVisibility.Auto &&
              ScrollViewer.GetCanContentScroll(mainScrollViewer),
            $"The operational rail must own one visible, scrollable logical viewport; actual scrollable height {mainScrollViewer?.ScrollableHeight:0.#} DIP.");

        var initialRealized = CountRealizedRailRows(rail);
        Check(initialRealized > 0 && initialRealized < rail.Items.Count / 2 && initialRealized < expectedToolCount / 2,
            $"The rail must virtualize off-screen rows instead of materializing the full list; realized {initialRealized} of {rail.Items.Count} entries ({expectedToolCount} tool routes).");

        var favoriteLayoutChecks = 0;
        for (var index = 0; index < rail.Items.Count; index++)
        {
            if (!string.Equals(rail.Items[index]?.GetType().Name, "OperationalRailToolEntryViewModel", StringComparison.Ordinal)) continue;
            if (rail.ItemContainerGenerator.ContainerFromIndex(index) is not ListBoxItem row || !row.IsVisible) continue;
            var buttons = Descendants(row).OfType<Button>().ToArray();
            var favorite = buttons.SingleOrDefault(button => button.Content is string glyph && (glyph == "☆" || glyph == "★"));
            var select = buttons.FirstOrDefault(button => !ReferenceEquals(button, favorite));
            Check(favorite != null && select != null,
                "Each realized operational tool row must retain separate selection and favorite controls.");
            if (favorite == null || select == null) continue;

            var rowBounds = Bounds(row, rail);
            var selectionBounds = Bounds(select, rail);
            var favoriteBounds = Bounds(favorite, rail);
            Check(Math.Abs(favorite.ActualWidth - 26) < 1 && favoriteBounds.Left >= rowBounds.Left - 1 &&
                  favoriteBounds.Right <= rowBounds.Right + 1 && favoriteBounds.Top >= rowBounds.Top - 1 &&
                  favoriteBounds.Bottom <= rowBounds.Bottom + 1 && selectionBounds.Right <= favoriteBounds.Left + 1,
                $"The favorite button must stay fully visible in a reserved rail column at every rendered width: row={rowBounds}, select={selectionBounds}, favorite={favoriteBounds}, width={favorite.ActualWidth:0.#} DIP.");
            foreach (var label in Descendants(select).OfType<TextBlock>().Where(block => block.IsVisible && block.ActualWidth > 0 && block.ActualHeight > 0))
            {
                var labelBounds = Bounds(label, rail);
                Check(labelBounds.Left >= selectionBounds.Left - 1 && labelBounds.Right <= selectionBounds.Right + 1 &&
                      labelBounds.Top >= selectionBounds.Top - 1 && labelBounds.Bottom <= selectionBounds.Bottom + 1,
                    $"Operational rail label '{label.Text}' is clipped or overlaps the favorite column: {labelBounds}, select={selectionBounds}.");
            }
            favoriteLayoutChecks++;
        }
        Check(favoriteLayoutChecks > 0, "The operational rail must render at least one visible tool row for favorite-column clipping checks.");

        var savedOffset = mainScrollViewer.VerticalOffset;
        var selectedTool = shell.GetType().GetProperty("SelectedTool").GetValue(shell);
        var endOffset = mainScrollViewer.ScrollableHeight;
        var scrolledRealized = 0;
        var reachedOffset = savedOffset;
        var changedOffset = false;
        try
        {
            if (savedOffset >= endOffset - 1)
            {
                mainScrollViewer.ScrollToVerticalOffset(0);
                window.UpdateLayout();
                changedOffset = Math.Abs(mainScrollViewer.VerticalOffset - savedOffset) > 1;
            }
            mainScrollViewer.ScrollToVerticalOffset(endOffset);
            window.UpdateLayout();
            reachedOffset = mainScrollViewer.VerticalOffset;
            changedOffset |= Math.Abs(mainScrollViewer.VerticalOffset - savedOffset) > 1;
            Check(mainScrollViewer.VerticalOffset >= mainScrollViewer.ScrollableHeight - 1 && changedOffset,
                $"The operational rail did not scroll to its end (offset {savedOffset:0.#} to {mainScrollViewer.VerticalOffset:0.#}, end {mainScrollViewer.ScrollableHeight:0.#}).");
            Check(ReferenceEquals(selectedTool, shell.GetType().GetProperty("SelectedTool").GetValue(shell)),
                "Scrolling the operational rail must not change the selected tool route.");
            scrolledRealized = CountRealizedRailRows(rail);
            Check(scrolledRealized > 0 && scrolledRealized < rail.Items.Count / 2 && scrolledRealized < expectedToolCount / 2,
                $"The rail must keep off-screen rows virtualized after scrolling; realized {scrolledRealized} of {rail.Items.Count} entries.");
            var lastContainer = rail.ItemContainerGenerator.ContainerFromIndex(rail.Items.Count - 1) as ListBoxItem;
            Check(lastContainer != null && lastContainer.IsVisible,
                "Scrolling the operational rail to its end must realize the trailing footer row.");

            var footerVisuals = EnumerateVisualDescendants(rail).ToArray();
            var toolsVisibleText = app.TryFindResource("Ui.ToolsVisible") as string;
            var emptyPinnedText = app.TryFindResource("Ui.EmptyPinned") as string;
            Check(!string.IsNullOrWhiteSpace(toolsVisibleText) &&
                  footerVisuals.OfType<TextBlock>().Any(block => string.Equals(block.Text, toolsVisibleText, StringComparison.Ordinal)),
                "The realized virtualized footer must render its localized Ui.ToolsVisible label.");
            var emptyPinnedLabel = footerVisuals.OfType<TextBlock>().SingleOrDefault(block =>
                string.Equals(block.Text, emptyPinnedText, StringComparison.Ordinal));
            Check(!string.IsNullOrWhiteSpace(emptyPinnedText) && emptyPinnedLabel != null && emptyPinnedLabel.IsVisible &&
                  emptyPinnedLabel.ActualWidth > 0 && emptyPinnedLabel.ActualHeight > 0 && emptyPinnedLabel.TextWrapping == TextWrapping.Wrap,
                "The realized footer must render the wrapping Ui.EmptyPinned label while its pinned collection is empty.");
            var pinnedViewport = footerVisuals.OfType<ScrollViewer>().SingleOrDefault(view =>
                string.Equals(AutomationProperties.GetAutomationId(view), "OperationalRailPinnedViewport", StringComparison.Ordinal));
            var recentViewport = footerVisuals.OfType<ScrollViewer>().SingleOrDefault(view =>
                string.Equals(AutomationProperties.GetAutomationId(view), "OperationalRailRecentViewport", StringComparison.Ordinal));
            var pinnedHeight = app.TryFindResource("Token.SmallHistoryHeight");
            var recentHeight = app.TryFindResource("Token.HistoryHeight");
            Check(pinnedHeight is double expectedPinnedHeight && recentHeight is double expectedRecentHeight &&
                  Math.Abs(expectedRecentHeight - 142) < 0.1 &&
                  pinnedViewport != null && Math.Abs(pinnedViewport.MaxHeight - expectedPinnedHeight) < 0.1 &&
                  pinnedViewport.IsVisible && pinnedViewport.ActualHeight > 0 && pinnedViewport.VerticalScrollBarVisibility == ScrollBarVisibility.Auto &&
                  recentViewport != null && !ReferenceEquals(pinnedViewport, recentViewport) &&
                  Math.Abs(recentViewport.MaxHeight - expectedRecentHeight) < 0.1 && recentViewport.IsVisible && recentViewport.ActualHeight > 0 &&
                  recentViewport.VerticalScrollBarVisibility == ScrollBarVisibility.Auto && recentViewport.ScrollableHeight > 0,
                $"The realized footer must expose separate Auto-scroll Pinned/Recent viewports with their theme tokens; actual IDs/heights: pinned='{(pinnedViewport == null ? "<missing>" : AutomationProperties.GetAutomationId(pinnedViewport))}'/{pinnedViewport?.MaxHeight:0.#}, recent='{(recentViewport == null ? "<missing>" : AutomationProperties.GetAutomationId(recentViewport))}'/{recentViewport?.MaxHeight:0.#}, recent scrollable={recentViewport?.ScrollableHeight:0.#} DIP.");
        }
        finally
        {
            mainScrollViewer.ScrollToVerticalOffset(savedOffset);
            window.UpdateLayout();
            visualTreeSnapshots.Remove(window);
        }
        return new
        {
            routeRows = toolRows,
            groupHeaders = groupRows,
            footerRows,
            itemCount = rail.Items.Count,
            initialRealized,
            favoriteLayoutChecks,
            scrolledRealized,
            scrollableHeight = mainScrollViewer.ScrollableHeight,
            startOffset = savedOffset,
            testedOffset = reachedOffset,
            virtualizationMode = "Recycling",
            realizedFooterHistories = true
        };
    }

    static int CountRealizedRailRows(ListBox rail)
    {
        var realized = 0;
        for (var index = 0; index < rail.Items.Count; index++)
            if (rail.ItemContainerGenerator.ContainerFromIndex(index) is ListBoxItem) realized++;
        return realized;
    }

    static Rect Bounds(FrameworkElement element, Visual ancestor)
    {
        return element.TransformToAncestor(ancestor).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
    }

    static bool HasBindingPath(ComboBox selector, DependencyProperty property, string path)
    {
        return string.Equals(BindingOperations.GetBinding(selector, property)?.Path?.Path, path, StringComparison.Ordinal);
    }

    static bool HasAncestor<T>(DependencyObject element, DependencyObject stopAt) where T : DependencyObject
    {
        for (var current = VisualTreeHelper.GetParent(element); current != null && current != stopAt; current = VisualTreeHelper.GetParent(current))
            if (current is T) return true;
        return false;
    }

    static bool IsVisualDescendantOrSelf(DependencyObject element, DependencyObject ancestor)
    {
        for (var current = element; current != null; current = VisualTreeHelper.GetParent(current))
            if (ReferenceEquals(current, ancestor)) return true;
        return false;
    }

    static string DescribeScrollViewer(ScrollViewer viewport, DependencyObject stopAt)
    {
        var parentGrid = FindAncestor<Grid>(viewport, stopAt);
        var row = parentGrid == null ? -1 : Grid.GetRow(viewport);
        return $"AutomationId='{AutomationProperties.GetAutomationId(viewport)}', ToolTip='{viewport.ToolTip}', Grid.Row={row}, size={viewport.ActualWidth:0.#}x{viewport.ActualHeight:0.#}";
    }

    static T FindAncestor<T>(DependencyObject element, DependencyObject stopAt) where T : DependencyObject
    {
        for (var current = VisualTreeHelper.GetParent(element); current != null && current != stopAt; current = VisualTreeHelper.GetParent(current))
            if (current is T match) return match;
        return null;
    }

    static string DescribeVisualAncestors(DependencyObject element, DependencyObject stopAt)
    {
        var parts = new List<string>();
        for (var current = element; current != null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is FrameworkElement framework)
            {
                var id = AutomationProperties.GetAutomationId(framework);
                var bounds = ReferenceEquals(current, stopAt) ? "root" : Bounds(framework, (Visual)stopAt).ToString();
                parts.Add($"{framework.GetType().Name}[{id}] {framework.ActualWidth:0.#}x{framework.ActualHeight:0.#} at {bounds}");
            }
            if (ReferenceEquals(current, stopAt)) break;
        }
        return string.Join(" <- ", parts);
    }

    static void RunDeclarativeCatalogTests(List<object> records)
    {
        var pages = DeclarativeUiCatalog.Descriptors;
        Check(pages.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count() == pages.Count, "Declarative page keys must be unique.");
        var workbench = pages.Single(item => item.Key == "tool-workbench");
        var expectedWorkbenchCommandIds = new[] { "run", "cancel", "export", "browse-file", "browse-folder", "load-preset", "clear-input" };
        var workbenchCommandIds = workbench.Commands.Select(command => command.Id).ToHashSet(StringComparer.Ordinal);
        Check(workbench.Region == "Workbench" && workbench.ReleaseOnNavigate && workbenchCommandIds.SetEquals(expectedWorkbenchCommandIds), "Workbench descriptor must declare its routed lifecycle, primary actions, input pickers and input utilities.");
        var invalidRegion = new DeclarativeUiDescriptor(typeof(ToolPageViewModel), "bad-region", "Window", true, Array.Empty<DeclarativeUiCommand>());
        var rejectedRegion = false;
        try { DeclarativeUiCatalog.Validate(pages.Concat(new[] { invalidRegion }).ToArray()); }
        catch (InvalidOperationException) { rejectedRegion = true; }
        Check(rejectedRegion, "Unknown declarative UI regions must be rejected.");
        var duplicateCommand = new DeclarativeUiDescriptor(typeof(ToolPageViewModel), "duplicate-command-page", "Workbench", true,
            new[] { new DeclarativeUiCommand("RunCommand", "run", "Primary", cancellable: true) });
        var rejectedCommand = false;
        try { DeclarativeUiCatalog.Validate(pages.Concat(new[] { duplicateCommand }).ToArray()); }
        catch (InvalidOperationException) { rejectedCommand = true; }
        Check(rejectedCommand, "Duplicate declarative command IDs must be rejected.");
        records.Add(new { test = "declarative-page-and-command-contracts", passed = true });
    }

    static void RunAssemblyWorkbenchTests(List<object> records)
    {
        var service = new DesktopAssemblyService();
        var source = typeof(Program).Assembly.Location;
        var inspection = service.Inspect(source, CancellationToken.None);
        Check(inspection.Contains("STATIC METADATA ONLY") && inspection.Contains("SHA-256:") && inspection.Contains("Assembly references"), "Static assembly inspection did not include bounded metadata evidence.");
        records.Add(new { test = "assembly-static-inspection", passed = true, source = Path.GetFileName(source) });

        var temp = Path.Combine(Path.GetTempPath(), "CalradiaForge-AsmResolver-Test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var input = Path.Combine(temp, "render-tests.dll");
            var output = Path.Combine(temp, "output", "render-tests.dll");
            File.Copy(source, input);
            var originalHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input)));
            var requestPath = Path.Combine(temp, "patch.json");
            var request = new AssemblyVersionPatchRequest { Operation = "preview-set-assembly-version", Input = "render-tests.dll", Output = "output/render-tests.dll", Version = "7.6.5.0" };
            File.WriteAllText(requestPath, JsonSerializer.Serialize(request));
            var preview = service.PreviewAndWriteVersionPatch(requestPath, CancellationToken.None);
            Check(preview.Contains("PATCH PREVIEW / NO FILES WRITTEN") && !File.Exists(output) && !File.Exists(output + ".source.bak"), "Patch preview must not write files.");
            request.Operation = "apply-set-assembly-version";
            File.WriteAllText(requestPath, JsonSerializer.Serialize(request));
            var applied = service.PreviewAndWriteVersionPatch(requestPath, CancellationToken.None);
            Check(applied.Contains("PATCH COMPLETE / COPY ONLY") && File.Exists(output) && File.Exists(output + ".source.bak"), "Copy-only patch must create output and source backup.");
            Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))) == originalHash, "Assembly patch changed the original source.");
            Check(service.Inspect(output, CancellationToken.None).Contains("Version: 7.6.5.0"), "Patched output metadata failed verification.");
            records.Add(new { test = "assembly-edit-preview-backup-copy-validation", passed = true });

            var invalid = Path.Combine(temp, "native.dll");
            File.WriteAllBytes(invalid, new byte[] { 0x4D, 0x5A, 0x00, 0x01, 0x02 });
            var rejected = false;
            try { service.Inspect(invalid, CancellationToken.None); } catch (Exception) { rejected = true; }
            Check(rejected, "Malformed or native PE input must not be treated as a managed assembly.");
            records.Add(new { test = "assembly-invalid-input-rejected", passed = true });
        }
        finally { if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true); }
    }

    static void RunInputPickerTests(List<object> records)
    {
        string selectedPath = null;
        bool selectedFolder = false;
        var tool = new ToolCatalog().Find("SpritePackageAuditor");
        var page = new WorkbenchPageViewModel(tool,
            (_, _, _) => System.Threading.Tasks.Task.FromResult(new WorkspaceExecutionResult { Status = "Not run" }),
            _ => { },
            (_, folder) => { selectedFolder = folder; return selectedPath; });

        Check(page.BrowseFileCommand.CanExecute(null) && page.BrowseFolderCommand.CanExecute(null), "Input pickers must be enabled for tools that require a path.");
        selectedPath = @"C:\Mods\CalradiaForge\AssetSources\GauntletUI\ui_calradiaforge_1.png";
        page.BrowseFileCommand.Execute(null);
        Check(!selectedFolder && page.Input == selectedPath, "File selection did not update the page input.");
        selectedPath = @"C:\Mods\CalradiaForge";
        page.BrowseFolderCommand.Execute(null);
        Check(selectedFolder && page.Input == selectedPath, "Folder selection did not update the page input.");
        var retained = page.Input;
        selectedPath = null;
        page.BrowseFileCommand.Execute(null);
        Check(page.Input == retained, "Cancelling a picker must preserve the current input.");
        page.Dispose();
        Check(!page.BrowseFileCommand.CanExecute(null) && !page.BrowseFolderCommand.CanExecute(null), "Disposing a routed page must disable input pickers.");
        records.Add(new { test = "mvvm-native-input-picker-commands", passed = true, preservesInputOnCancel = true });
    }

    static void RunAsyncPageDisposalTests(List<object> records)
    {
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource<WorkspaceExecutionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var observedCancellation = CancellationToken.None;
        var tool = new ToolDefinition("RenderHarness.AsyncDisposal", "Async disposal regression", "Diagnostics", DesktopToolKind.Analyzer, false, false);
        var page = new WorkbenchPageViewModel(tool, async (_, _, cancellation) =>
        {
            observedCancellation = cancellation;
            started.TrySetResult(true);
            return await completion.Task.ConfigureAwait(false);
        }, _ => { });

        var execution = page.RunCommand.ExecuteAsync();
        Check(started.Task.Wait(TimeSpan.FromSeconds(2)), "Async page-disposal fixture did not start its pending operation.");
        var retainedResult = page.RawResult;
        page.Dispose();
        Check(observedCancellation.IsCancellationRequested, "Releasing a page must signal cancellation to its pending operation.");
        completion.SetResult(new WorkspaceExecutionResult
        {
            Status = "Completed",
            RawResult = "late result must not be retained",
            Evidence = new[] { new WorkspaceEvidence("Render harness", "Completed", "late evidence must not be retained") }
        });
        execution.GetAwaiter().GetResult();
        Check(page.Evidence.Count == 0 && page.RawResult == retainedResult,
            "A late async completion must not repopulate result or evidence after its page is released.");
        records.Add(new { test = "async-page-disposal-rejects-late-completion", cancellationRequested = true, lateEvidenceRetained = false, passed = true });
    }

    static object SummarizeShellMetrics(IReadOnlyList<DesktopMeasurement> measurements)
    {
        var elapsed = measurements.Select(item => (double)item.ElapsedMilliseconds).OrderBy(value => value).ToArray();
        var allocations = measurements.Where(item => item.AllocatedBytes.HasValue)
            .Select(item => (double)item.AllocatedBytes.Value).OrderBy(value => value).ToArray();
        return new
        {
            samples = elapsed.Length,
            p50Milliseconds = elapsed.Length == 0 ? 0 : Math.Round(Percentile(elapsed, 0.50), 2),
            p95Milliseconds = elapsed.Length == 0 ? 0 : Math.Round(Percentile(elapsed, 0.95), 2),
            maxMilliseconds = elapsed.Length == 0 ? 0 : Math.Round(elapsed[elapsed.Length - 1], 2),
            atOrAboveFrameThreshold = elapsed.Count(value => value >= 16.7),
            p50AllocatedBytes = allocations.Length == 0 ? (long?)null : (long)Math.Round(Percentile(allocations, 0.50)),
            maxAllocatedBytes = allocations.Length == 0 ? (long?)null : (long)Math.Round(allocations[allocations.Length - 1])
        };
    }

    static double Percentile(double[] sortedValues, double percentile)
    {
        if (sortedValues == null || sortedValues.Length == 0) return 0;
        var index = Math.Max(0, Math.Min(sortedValues.Length - 1, (int)Math.Ceiling(percentile * sortedValues.Length) - 1));
        return sortedValues[index];
    }

    static void Render(Window window)
    {
        visualTreeSnapshots.Remove(window);
        var timer = Stopwatch.StartNew();
        window.UpdateLayout();
        timer.Stop();
        renderPassCount++;
        renderLayoutMilliseconds += timer.Elapsed.TotalMilliseconds;
        renderLayoutSamples.Add(timer.Elapsed.TotalMilliseconds);
    }

    static object RenderPerformanceSummary() => new
    {
        renderPassCount,
        renderLayoutMilliseconds = Math.Round(renderLayoutMilliseconds, 1),
        renderLayoutP50Milliseconds = Math.Round(Percentile(renderLayoutSamples.OrderBy(value => value).ToArray(), 0.50), 2),
        renderLayoutP95Milliseconds = Math.Round(Percentile(renderLayoutSamples.OrderBy(value => value).ToArray(), 0.95), 2),
        renderLayoutMaxMilliseconds = renderLayoutSamples.Count == 0 ? 0 : Math.Round(renderLayoutSamples.Max(), 2),
        visualTreeSnapshotBuilds,
        visualTreeSnapshotHits,
        visualTreeNodesVisited
    };

    static RenderTargetBitmap SavePreview(Window window, string path, double rasterScaleFactor = 1.0)
    {
        var surface = (FrameworkElement)window.Content;
        var dpi = 96 * Math.Clamp(rasterScaleFactor, 1.0, 2.0);
        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(surface.ActualWidth * dpi / 96),
            (int)Math.Ceiling(surface.ActualHeight * dpi / 96),
            dpi, dpi, PixelFormats.Pbgra32);
        bitmap.Render(surface);
        Check(bitmap.PixelWidth >= Math.Ceiling(surface.ActualWidth * rasterScaleFactor - 0.01) &&
              bitmap.PixelHeight >= Math.Ceiling(surface.ActualHeight * rasterScaleFactor - 0.01),
            $"The WPF preview did not rasterize the complete fixed-DIP surface at bitmap factor {rasterScaleFactor:P0}.");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var stream = File.Create(path)) encoder.Save(stream);
        return bitmap;
    }
    static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        if (parent is Window window)
        {
            if (visualTreeSnapshots.TryGetValue(window, out var cached))
            {
                visualTreeSnapshotHits++;
                return cached;
            }

            var snapshot = EnumerateVisualDescendants(parent).ToArray();
            visualTreeSnapshots[window] = snapshot;
            visualTreeSnapshotBuilds++;
            visualTreeNodesVisited += snapshot.Length;
            return snapshot;
        }

        return EnumerateVisualDescendants(parent);
    }

    static IEnumerable<DependencyObject> EnumerateVisualDescendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var descendant in EnumerateVisualDescendants(child)) yield return descendant;
        }
    }
    static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    sealed class PreferenceSnapshot
    {
        readonly string path;
        readonly byte[] hash;
        readonly bool existed;

        PreferenceSnapshot(string path, bool existed, byte[] hash)
        {
            this.path = path;
            this.existed = existed;
            this.hash = hash;
        }

        public static PreferenceSnapshot Capture(string path)
        {
            var exists = File.Exists(path);
            return new PreferenceSnapshot(path, exists, exists ? SHA256.HashData(File.ReadAllBytes(path)) : null);
        }

        public void AssertUnchanged()
        {
            var exists = File.Exists(path);
            Check(exists == existed, "WPF render tests changed whether the user's Desktop preference file exists.");
            if (exists)
                Check(SHA256.HashData(File.ReadAllBytes(path)).SequenceEqual(hash),
                    "WPF render tests changed the user's persisted Desktop preferences.");
        }
    }

    static void AssertThemeBrushes(Application app, object shell, string themeId)
    {
        var expected = themeId switch
        {
            "parchment" => (Muted: Color.FromRgb(0x5B, 0x51, 0x45), Paper: Color.FromRgb(0x2A, 0x21, 0x18), LogoText: Color.FromRgb(0x2A, 0x21, 0x18)),
            "high-contrast" => (Muted: Color.FromRgb(0xD4, 0xE5, 0xD8), Paper: Color.FromRgb(0xFF, 0xFD, 0xF5), LogoText: Color.FromRgb(0xFF, 0xFD, 0xF5)),
            _ => (Muted: Color.FromRgb(0xB6, 0xC7, 0xB9), Paper: Color.FromRgb(0xF1, 0xE6, 0xC8), LogoText: Color.FromRgb(0xF1, 0xE6, 0xC8))
        };
        var muted = app.Resources["MutedTextBrush"] as SolidColorBrush;
        var sources = string.Join(" | ", app.Resources.MergedDictionaries.Select(item => item.Source?.OriginalString + ":" + (item.Contains("Palette.Muted") ? item["Palette.Muted"] : "-")));
        var palettes = app.Resources.MergedDictionaries.Where(item => item.Source?.OriginalString.IndexOf("TacticalPalette", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        Check(palettes.Length == 1, "Theme changes must leave exactly one active palette dictionary.");
        var colors = palettes[0];
        Check(muted != null && muted.Color == expected.Muted,
            $"Theme {themeId} did not apply its muted text brush (actual: {muted?.Color.ToString() ?? "missing"}, palette: {app.TryFindResource("Palette.Muted")}, selected: {shell.GetType().GetProperty("ThemeId").GetValue(shell)}, dictionaries: {sources}; expected: {expected.Muted}).");
        Check(app.Resources["PaperBrush"] is SolidColorBrush paper && paper.Color == expected.Paper,
            $"Theme {themeId} did not apply its primary text brush.");
        Check(app.Resources["LogoTextBrush"] is SolidColorBrush logoText && logoText.Color == expected.LogoText,
            $"Theme {themeId} did not apply its semantic seal-text brush.");
        foreach (var key in new[] { "DisabledButtonSurfaceBrush", "DisabledButtonTextBrush", "DisabledButtonBorderBrush" })
            Check(app.TryFindResource(key) is SolidColorBrush,
                $"Theme {themeId} is missing the themed disabled-button state resource {key}.");
        var disabledSurface = RequiredBrush(app, "DisabledButtonSurfaceBrush").Color;
        var disabledText = RequiredBrush(app, "DisabledButtonTextBrush").Color;
        var disabledBorder = RequiredBrush(app, "DisabledButtonBorderBrush").Color;
        Check(ContrastRatio(disabledText, disabledSurface) >= 4.5 &&
              ContrastRatio(disabledBorder, disabledSurface) >= 3.0 &&
              disabledSurface != (Color)colors["Palette.Tempered"],
            $"Theme {themeId} disabled buttons must remain readable and distinct from enabled buttons.");
        var mutedColor = (Color)colors["Palette.Muted"];
        foreach (var key in new[] { "Palette.Coal", "Palette.DeepPine", "Palette.RailSurface" })
            Check(ContrastRatio(mutedColor, (Color)colors[key]) >= 4.5, $"Theme {themeId} muted text contrast fell below 4.5:1 on {key}.");
        Check(ContrastRatio(expected.LogoText, (Color)colors["Palette.Tempered"]) >= 4.5,
            $"Theme {themeId} seal monogram contrast fell below 4.5:1 on its shield fill.");

        var material = app.TryFindResource("Surface.MaterialTexture");
        var titleBarMaterial = app.TryFindResource("TitleBarMaterialTexture");
        if (string.Equals(themeId, "high-contrast", StringComparison.OrdinalIgnoreCase))
        {
            Check(material is SolidColorBrush && titleBarMaterial is SolidColorBrush &&
                  app.TryFindResource("FrameSurfaceBrush") is SolidColorBrush &&
                  app.TryFindResource("NavigationSurfaceBrush") is SolidColorBrush &&
                  app.TryFindResource("TitleBarSurfaceBrush") is SolidColorBrush,
                "High Contrast must keep themed surfaces solid and exclude image/drawing textures.");
            Check(!colors.Values.OfType<Brush>().Any(brush => brush is ImageBrush || brush is DrawingBrush),
                "High Contrast palette must not register image or drawing brushes.");
        }
        else
        {
            var expectedMaterial = themeId == "parchment" ? "parchment-cartographic-paper.png" : "war-table-illustrated-cloth.png";
            var surfaceOpacityLimit = themeId == "war-table" ? 0.05 : 0.12;
            Check(material is ImageBrush surfaceTexture && IsLocalTextureUri(surfaceTexture.ImageSource) &&
                  surfaceTexture.Opacity <= surfaceOpacityLimit &&
                  surfaceTexture.ImageSource.ToString().IndexOf(expectedMaterial, StringComparison.OrdinalIgnoreCase) >= 0,
                $"Theme {themeId} must use its expected local illustrated material texture within its surface opacity limit ({surfaceOpacityLimit:0.00}).");
            Check(titleBarMaterial is ImageBrush titleTexture && IsLocalTextureUri(titleTexture.ImageSource),
                $"Theme {themeId} title bar material is not a local packaged image texture.");
            Check(app.TryFindResource("FrameSurfaceBrush") is DrawingBrush &&
                  app.TryFindResource("NavigationSurfaceBrush") is DrawingBrush &&
                  app.TryFindResource("TitleBarSurfaceBrush") is DrawingBrush,
                $"Theme {themeId} should compose its illustrated materials into passive surface brushes.");
        }
    }

    static void AssertDisabledEvidenceButton(Application app, Window window, string themeId)
    {
        var button = Descendants(window).OfType<Button>().SingleOrDefault(control =>
            string.Equals(AutomationProperties.GetAutomationId(control), "ExportEvidenceButton", StringComparison.Ordinal));
        Check(button != null && !button.IsEnabled,
            $"Theme {themeId} render requires the disabled empty-ledger export button.");

        var expectedSurface = RequiredBrush(app, "DisabledButtonSurfaceBrush").Color;
        var expectedText = RequiredBrush(app, "DisabledButtonTextBrush").Color;
        var expectedBorder = RequiredBrush(app, "DisabledButtonBorderBrush").Color;
        button.ApplyTemplate();
        var chrome = button.Template.FindName("ActionButtonChrome", button) as Border;
        Check(button.Opacity >= 0.99 && button.Background is SolidColorBrush surface && surface.Color == expectedSurface &&
              button.Foreground is SolidColorBrush text && text.Color == expectedText &&
              button.BorderBrush is SolidColorBrush border && border.Color == expectedBorder &&
              chrome != null && chrome.Opacity >= 0.99,
            $"Theme {themeId} disabled evidence export must use its semantic surface, readable text, and boundary without whole-control dimming.");

        var label = Descendants(button).OfType<TextBlock>().FirstOrDefault(control => control.IsVisible);
        if (label != null && label.Foreground is SolidColorBrush labelBrush)
            Check(ContrastRatio(labelBrush.Color, expectedSurface) >= 4.5,
                $"Theme {themeId} rendered disabled evidence label fell below 4.5:1 contrast.");
    }

    static void AssertSelectorThemeColors(Application app, Window window, string themeId, RenderTargetBitmap surfaceRender)
    {
        var foreground = (app.Resources["PaperBrush"] as SolidColorBrush)?.Color;
        var background = (app.Resources["InputBrush"] as SolidColorBrush)?.Color;
        var rootSurface = (FrameworkElement)window.Content;
        var selectors = Descendants(window).OfType<ComboBox>().ToArray();
        Check(selectors.Length >= 2, "Language and theme selectors must be present in the rendered header.");
        Check(surfaceRender != null && surfaceRender.PixelWidth >= rootSurface.ActualWidth && surfaceRender.PixelHeight >= rootSurface.ActualHeight,
            $"Theme {themeId} selector checks require a fresh full-surface render at the logical viewport size.");
        var pixelStride = surfaceRender.PixelWidth * 4;
        var surfacePixels = new byte[pixelStride * surfaceRender.PixelHeight];
        surfaceRender.CopyPixels(surfacePixels, pixelStride, 0);
        Check(foreground.HasValue && background.HasValue && ContrastRatio(foreground.Value, background.Value) >= 4.5,
            $"Theme {themeId} selector palette does not provide readable text on its input surface.");
        var headerSelectors = selectors.Where(selector =>
        {
            var id = AutomationProperties.GetAutomationId(selector);
            return id is "LanguageSelector" or "ThemeSelector";
        }).ToArray();
        Check(headerSelectors.Length == 2, "The localized language and theme selectors must both be present.");
        foreach (var selector in headerSelectors)
            Check(selector.SelectedIndex >= 0,
                $"Theme {themeId} header selector must reflect the applied presentation state: {DescribeComboBoxSelection(selector)}");
        foreach (var selector in selectors)
        {
            Check(selector.Foreground is SolidColorBrush text && text.Color == foreground.Value,
                $"Theme {themeId} selector {DescribeComboBoxSelection(selector)} is not bound to the semantic text color.");
            Check(selector.Background is SolidColorBrush selectorSurface && selectorSurface.Color == background.Value,
                $"Theme {themeId} selector {DescribeComboBoxSelection(selector)} is not bound to the semantic input color.");
            if (selector.SelectedIndex >= 0)
                AssertRenderedComboBoxContrast(rootSurface, selector, surfacePixels, pixelStride, surfaceRender.PixelWidth, surfaceRender.PixelHeight, foreground.Value, background.Value, themeId);
        }
    }

    static string DescribeComboBoxSelection(ComboBox selector)
    {
        static string DescribeBinding(ComboBox control, DependencyProperty property)
        {
            var expression = BindingOperations.GetBindingExpression(control, property);
            if (expression == null) return "<no binding>";
            var resolvedSource = expression.ResolvedSource;
            var propertyName = expression.ResolvedSourcePropertyName;
            object sourceValue = null;
            if (resolvedSource != null && !string.IsNullOrEmpty(propertyName))
                sourceValue = resolvedSource.GetType().GetProperty(propertyName)?.GetValue(resolvedSource);
            return $"path={expression.ParentBinding.Path?.Path ?? "<none>"}, status={expression.Status}, sourceValue='{sourceValue ?? "<null>"}'";
        }

        return $"AutomationId='{AutomationProperties.GetAutomationId(selector)}', Items.Count={selector.Items.Count}, " +
               $"SelectedIndex={selector.SelectedIndex}, SelectedItem='{selector.SelectedItem ?? "<null>"}', " +
               $"SelectedValue='{selector.SelectedValue ?? "<null>"}', " +
               $"SelectedItemBinding[{DescribeBinding(selector, ComboBox.SelectedItemProperty)}], " +
               $"SelectedValueBinding[{DescribeBinding(selector, ComboBox.SelectedValueProperty)}]";
    }

    static void AssertRenderedComboBoxContrast(FrameworkElement surface, ComboBox selector, byte[] pixels, int stride, int width, int height, Color expectedForeground, Color expectedBackground, string themeId)
    {
        var textVisual = Descendants(selector).OfType<FrameworkElement>()
            .Where(element => element.IsVisible && element.ActualWidth > 0 && element.ActualHeight > 0)
            .FirstOrDefault(element => element is TextBlock || element is AccessText);
        Check(textVisual != null, $"Theme {themeId} ComboBox has no realized selected-text visual to inspect.");

        var selectorBounds = Bounds(selector, surface);
        // Sample the selected-value surface, clear of the trailing arrow hit area.
        // Its hover brush intentionally differs from the input surface.
        // The combo's content presenter can extend over most of the control for long values.
        // A point just inside the left border is outside its padding and reliably samples chrome.
        var surfaceX = Math.Clamp((int)Math.Round(selectorBounds.Left + 3), 1, width - 2);
        var surfaceY = Math.Clamp((int)Math.Round(selectorBounds.Top + selectorBounds.Height * 0.5), 1, height - 2);
        var renderedSurface = ReadPbgraPixel(pixels, stride, surfaceX, surfaceY);
        Check(ColorDistance(renderedSurface, expectedBackground) <= 12,
            $"Theme {themeId} ComboBox {DescribeComboBoxSelection(selector)} visible template surface renders {renderedSurface}, expected InputBrush {expectedBackground}; the template may be ignoring the semantic background.");
        var semanticContrast = ContrastRatio(expectedForeground, expectedBackground);
        Check(semanticContrast >= 4.5,
            $"Theme {themeId} ComboBox semantic PaperBrush/InputBrush contrast is only {semanticContrast:0.00}:1 ({expectedForeground} on {expectedBackground}).");

        var textBounds = Bounds(textVisual, surface);
        var left = Math.Clamp((int)Math.Floor(textBounds.Left), 0, width - 1);
        var top = Math.Clamp((int)Math.Floor(textBounds.Top), 0, height - 1);
        var right = Math.Clamp((int)Math.Ceiling(textBounds.Right), left + 1, width);
        var bottom = Math.Clamp((int)Math.Ceiling(textBounds.Bottom), top + 1, height);
        var maximumRenderedContrast = 0d;
        var maximumContrastPixel = Colors.Transparent;
        var nonSurfacePixels = 0;
        for (var y = top; y < bottom; y++)
        for (var x = left; x < right; x++)
        {
            var pixel = ReadPbgraPixel(pixels, stride, x, y);
            if (ColorDistance(pixel, renderedSurface) <= 4) continue;
            nonSurfacePixels++;
            var contrast = ContrastRatio(pixel, renderedSurface);
            if (contrast > maximumRenderedContrast)
            {
                maximumRenderedContrast = contrast;
                maximumContrastPixel = pixel;
            }
        }
        Check(nonSurfacePixels > 0 && maximumRenderedContrast >= 4.5,
            $"Theme {themeId} ComboBox selected text rendered at only {maximumRenderedContrast:0.00}:1 against surface {renderedSurface}; semantic foreground/background are {expectedForeground}/{expectedBackground}, " +
            $"strongest sampled text pixel is {maximumContrastPixel}, visual '{(textVisual is TextBlock textBlock ? textBlock.Text : ((AccessText)textVisual).Text)}', bounds={textBounds}, selector={AutomationProperties.GetAutomationId(selector)}.");
    }

    static Color ReadPbgraPixel(byte[] pixels, int stride, int x, int y)
    {
        var offset = y * stride + x * 4;
        var alpha = pixels[offset + 3];
        if (alpha == 0) return Colors.Transparent;
        byte Unpremultiply(byte value) => alpha == 255 ? value : (byte)Math.Min(255, Math.Round(value * 255d / alpha));
        return Color.FromArgb(alpha, Unpremultiply(pixels[offset + 2]), Unpremultiply(pixels[offset + 1]), Unpremultiply(pixels[offset]));
    }

    static int ColorDistance(Color left, Color right) =>
        Math.Abs(left.R - right.R) + Math.Abs(left.G - right.G) + Math.Abs(left.B - right.B);

    static void AssertTitleBarAccessibleNames(Application app, Window window, string languageCode)
    {
        var controls = new[]
        {
            (AutomationId: "WindowMinimizeButton", ResourceKey: "Ui.WindowMinimize"),
            (AutomationId: "WindowMaximizeButton", ResourceKey: "Ui.WindowMaximize"),
            (AutomationId: "WindowRestoreButton", ResourceKey: "Ui.WindowRestore"),
            (AutomationId: "WindowCloseButton", ResourceKey: "Ui.WindowClose")
        };
        foreach (var control in controls)
        {
            var button = FindTitleBarButton(window, control.AutomationId);
            Check(button != null, "The custom title bar is missing control " + control.AutomationId + ".");
            var expected = app.TryFindResource(control.ResourceKey) as string;
            Check(!string.IsNullOrWhiteSpace(expected), "The " + languageCode + " catalog has no accessible name for " + control.AutomationId + ".");
            Check(string.Equals(AutomationProperties.GetName(button), expected, StringComparison.Ordinal),
                "The " + languageCode + " title-bar control " + control.AutomationId + " does not expose its localized accessible name.");
            Check(string.Equals(button.ToolTip as string, expected, StringComparison.Ordinal),
                "The " + languageCode + " title-bar control " + control.AutomationId + " tooltip does not match its accessible name.");
            Check(button.Focusable && button.IsTabStop && HasTitleBarFocusAdornment(button.Style),
                "Title-bar control " + control.AutomationId + " is not reachable by keyboard with a visible focus adornment.");
        }
        Check(controls.Select(control => KeyboardNavigation.GetTabIndex(FindTitleBarButton(window, control.AutomationId)))
                .SequenceEqual(new[] { 0, 1, 2, 3 }),
            "The custom title-bar controls do not follow their explicit keyboard tab order.");
    }

    static void AssertDecorativeAccentsToggleAccess(Application app, Window window, object shell, string languageCode)
    {
        var toggle = Descendants(window).OfType<CheckBox>().SingleOrDefault(control =>
            string.Equals(AutomationProperties.GetAutomationId(control), "DecorativeAccentsToggle", StringComparison.Ordinal));
        Check(toggle != null && toggle.IsVisible && toggle.IsEnabled && toggle.Focusable && toggle.IsTabStop,
            "The decorative accents toggle must remain visible, enabled, and keyboard accessible for " + languageCode + ".");
        var expected = app.TryFindResource("Ui.DecorativeAccentsShort") as string;
        var accessibleName = app.TryFindResource("Ui.DecorativeAccents") as string;
        var actualContent = toggle.Content as string ?? (toggle.Content as TextBlock)?.Text;
        var actualName = AutomationProperties.GetName(toggle);
        Check(!string.IsNullOrWhiteSpace(expected) &&
              string.Equals(actualContent, expected, StringComparison.Ordinal) &&
              string.Equals(actualName, accessibleName, StringComparison.Ordinal) &&
              string.Equals(toggle.ToolTip as string, accessibleName, StringComparison.Ordinal),
            $"The decorative accents toggle does not expose its compact localized content and complete accessible name for {languageCode}: expected '{expected}', content '{actualContent}', name '{actualName}'.");
        var binding = BindingOperations.GetBinding(toggle, CheckBox.IsCheckedProperty);
        Check(binding?.Path?.Path == "DecorativeAccentsEnabled" && binding.Mode == BindingMode.TwoWay,
            "The decorative accents control must keep a two-way MVVM binding to DecorativeAccentsEnabled.");
        Check(toggle.IsChecked == (bool)shell.GetType().GetProperty("DecorativeAccentsEnabled").GetValue(shell),
            "The decorative accents checkbox and session ViewModel value are out of sync.");

        var label = Descendants(toggle).FirstOrDefault(element => element is AccessText || element is TextBlock) as FrameworkElement;
        Check(label != null, "The decorative accents checkbox must render its localized label as visible text.");
        var labelBounds = Bounds(label, toggle);
        Check(labelBounds.Left >= -1 && labelBounds.Top >= -1 && labelBounds.Right <= toggle.ActualWidth + 1 && labelBounds.Bottom <= toggle.ActualHeight + 1,
            $"The localized decorative accents label is clipped in {languageCode}: label {labelBounds}, toggle {toggle.ActualWidth:0.#}x{toggle.ActualHeight:0.#}.");
        if (label is TextBlock labelText)
        {
            Check(labelText.TextWrapping == TextWrapping.NoWrap && labelText.TextTrimming == TextTrimming.CharacterEllipsis &&
                  labelText.DesiredSize.Width <= labelText.ActualWidth + 1,
                $"The compact decorative-accent label must remain single-line and fit its measured area in {languageCode}: desired={labelText.DesiredSize.Width:0.#}, actual={labelText.ActualWidth:0.#}.");
        }
    }

    static void AssertToolPageDecorationAccess(Window window, string languageCode)
    {
        var overlay = FindImage(window, "dossier-corner-ornament-hq-rev083.png");
        var compactArtwork = window.ActualWidth < 1120;
        var expectedCornerWidth = compactArtwork ? 88d : 136d;
        var expectedCornerHeight = compactArtwork ? 59d : 90d;
        Check(overlay != null && Math.Abs(overlay.Width - expectedCornerWidth) < 1 && Math.Abs(overlay.Height - expectedCornerHeight) < 1 && Math.Abs(overlay.Opacity - 0.14) < 0.001 &&
              overlay.Stretch == Stretch.Uniform && !overlay.IsHitTestVisible && !overlay.Focusable,
            $"The illustrated ToolPage corner must retain its passive aspect ratio and responsive size at {languageCode}: expected={expectedCornerWidth:0.#}x{expectedCornerHeight:0.#}, actual={overlay?.Width:0.#}x{overlay?.Height:0.#}.");
        Check(overlay?.Source is BitmapSource cardBitmap &&
              cardBitmap.PixelWidth >= overlay.Width * 2 && cardBitmap.PixelHeight >= overlay.Height * 2 &&
              RenderOptions.GetBitmapScalingMode(overlay) == BitmapScalingMode.HighQuality,
            "The ToolPage corner artwork must retain enough source pixels and high-quality sampling for 200% DPI.");
        Check(IsLocalTextureUri(overlay?.Source), "The ToolPage corner texture must load from the local Desktop assembly resource.");
        var parentGrid = VisualTreeHelper.GetParent(overlay) as Grid;
        Check(parentGrid != null && parentGrid.Children.Count > 0 && ReferenceEquals(parentGrid.Children[0], overlay),
            "The ToolPage decoration must be the first passive layer in the title grid.");
        var titleBorder = FindAncestor<Border>(overlay, window);
        Check(titleBorder != null && ReferenceEquals(titleBorder.Child, parentGrid),
            "The ToolPage decoration must stay inside its title card and away from the ledger, results, and controls.");
        Check(parentGrid != null && parentGrid.ColumnDefinitions.Count == 3 && Grid.GetColumn(overlay) == 2 &&
              Grid.GetColumnSpan(overlay) == 1 &&
              overlay.HorizontalAlignment == HorizontalAlignment.Right,
            "The ToolPage corner texture must stay in the dedicated right-hand art column instead of overlaying the title and status columns.");
        if (parentGrid != null && titleBorder != null && overlay.IsVisible && overlay.ActualWidth > 0 && overlay.ActualHeight > 0)
        {
            var gridBounds = Bounds(parentGrid, titleBorder);
            var imageBounds = Bounds(overlay, titleBorder);
            var artColumnStart = parentGrid.ColumnDefinitions.Take(2).Sum(column => column.ActualWidth);
            var artColumnWidth = parentGrid.ColumnDefinitions[2].ActualWidth;
            Check(imageBounds.Left >= gridBounds.Left + artColumnStart - 1 &&
                  imageBounds.Right <= gridBounds.Left + artColumnStart + artColumnWidth + 1 &&
                  imageBounds.Right >= gridBounds.Right - 1 && imageBounds.Left >= gridBounds.Left - 1 &&
                  imageBounds.Top >= gridBounds.Top - 1 && imageBounds.Bottom <= gridBounds.Bottom + 1,
                $"The ToolPage corner texture must remain inside its dedicated right-hand art column and title card in {languageCode}: image {imageBounds}, grid {gridBounds}, art column start={artColumnStart:0.#}, width={artColumnWidth:0.#}.");

            var titleAndStatus = parentGrid.Children.OfType<StackPanel>()
                .FirstOrDefault(panel => Grid.GetColumn(panel) == 0 && Descendants(panel).OfType<TextBlock>().Any());
            Check(titleAndStatus != null,
                "The ToolPage title/status block must remain available for decoration overlap checks.");
            foreach (var text in Descendants(titleAndStatus).OfType<TextBlock>().Where(block => block.IsVisible && block.ActualWidth > 0 && block.ActualHeight > 0))
            {
                var textBounds = Bounds(text, titleBorder);
                Check(!imageBounds.IntersectsWith(textBounds),
                    $"The ToolPage corner texture overlaps title/status text in {languageCode}: image {imageBounds}, text '{text.Text}' {textBounds}.");
            }
        }
        Check(IsLocalTextureUri(overlay.Source),
            "The ToolPage texture for " + languageCode + " is not a packaged local WPF resource.");
    }

    static void AssertTitleBarDecorationsStayOutOfCaptionButtons(Window window, string themeId)
    {
        var titleBar = Descendants(window).OfType<Border>().Single(border => border.Name == "TitleBarBorder");
        var titleGrid = titleBar.Child as Grid;
        var captionButton = FindTitleBarButton(window, "WindowMinimizeButton");
        var captionStack = captionButton == null ? null : FindAncestor<StackPanel>(captionButton, titleGrid);
        var cartographicBand = Descendants(titleBar).OfType<Border>().SingleOrDefault(border =>
            border.Background is ImageBrush brush &&
            brush.ImageSource?.ToString().IndexOf("titlebar-cartographic-panorama-rev072.png", StringComparison.OrdinalIgnoreCase) >= 0);

        var duplicateFrame = Descendants(titleBar).OfType<Border>().Any(border =>
            string.Equals(AutomationProperties.GetAutomationId(border), "TitleBarHeraldicFrameDecoration", StringComparison.Ordinal));
        Check(titleGrid != null && captionStack != null && Grid.GetColumn(captionStack) == 2 && cartographicBand != null && !duplicateFrame,
            $"Theme {themeId} must retain the full cartographic title band without a second centered compass frame and keep caption buttons separate.");
        foreach (var decoration in new[] { cartographicBand })
        {
            Check(Grid.GetColumn(decoration) == 0 && Grid.GetColumnSpan(decoration) == 2 && !decoration.IsHitTestVisible,
                $"Theme {themeId} title-bar texture must span only the two title columns and remain passive.");
            var brush = decoration.Background as ImageBrush;
            Check(brush != null && brush.Viewbox.X == 0 && brush.Viewbox.Y == 0 && brush.Viewbox.Width == 1 && brush.Viewbox.Height == 1 &&
                  brush.Stretch == Stretch.Fill && brush.Opacity <= 0.2 && IsLocalTextureUri(brush.ImageSource),
                $"Theme {themeId} title-bar decoration must show the full pre-cropped local strip at low opacity without a viewport crop.");
            if (decoration.IsVisible && decoration.ActualWidth > 0)
            {
                var decorationBounds = Bounds(decoration, titleGrid);
                var captionBounds = Bounds(captionStack, titleGrid);
                Check(decorationBounds.Right <= captionBounds.Left + 1,
                    $"Theme {themeId} title-bar texture extends beneath the caption buttons: decoration {decorationBounds}, buttons {captionBounds}.");
            }
        }
    }

    static void AssertHighContrastHeaderTextFitsAt1360(Application app, Window window, object shell)
    {
        Check(Math.Abs(window.Width - 1360) < 0.1 && Math.Abs(window.ActualWidth - 1360) < 1,
            "High Contrast header text regression must run at the standard 1360-DIP render width.");

        var themeSelector = Descendants(window).OfType<ComboBox>().SingleOrDefault(control =>
            string.Equals(AutomationProperties.GetAutomationId(control), "ThemeSelector", StringComparison.Ordinal));
        Check(themeSelector != null && string.Equals(themeSelector.SelectedValue as string, "high-contrast", StringComparison.OrdinalIgnoreCase),
            "The High Contrast header regression must inspect the selected High Contrast selector value.");
        themeSelector.ApplyTemplate();
        var selection = themeSelector.Template.FindName("SelectionContent", themeSelector) as ContentPresenter;
        var highContrastName = app.TryFindResource("Ui.ThemeHighContrast") as string;
        var selectedThemeText = selection == null ? null : Descendants(selection).OfType<TextBlock>()
            .FirstOrDefault(block => block.IsVisible && string.Equals(block.Text, highContrastName, StringComparison.Ordinal));
        Check(selectedThemeText != null,
            "The High Contrast ComboBox selection must render its localized selected-theme text.");
        AssertSingleLineTextFits(selectedThemeText, "High Contrast selector at 1360 DIP");

        var accents = Descendants(window).OfType<CheckBox>().SingleOrDefault(control =>
            string.Equals(AutomationProperties.GetAutomationId(control), "DecorativeAccentsToggle", StringComparison.Ordinal));
        var accentsName = app.TryFindResource("Ui.DecorativeAccentsShort") as string;
        var accentsText = accents == null ? null : Descendants(accents).OfType<TextBlock>()
            .FirstOrDefault(block => block.IsVisible && string.Equals(block.Text, accentsName, StringComparison.Ordinal));
        Check(accents != null && accents.IsVisible && accentsText != null,
            "The High Contrast decorative-artwork checkbox must render its visible localized label at 1360 DIP.");
        AssertSingleLineTextFits(accentsText, "Decorative artwork label at 1360 DIP");

        var evidenceSummary = Descendants(window).OfType<Border>().SingleOrDefault(border =>
            string.Equals(AutomationProperties.GetAutomationId(border), "RetainedEvidenceSummary", StringComparison.Ordinal));
        var summaryText = evidenceSummary == null ? null : Descendants(evidenceSummary).OfType<TextBlock>()
            .FirstOrDefault(block => block.IsVisible && string.Equals(block.Text, "0 retained", StringComparison.Ordinal));
        var exportButton = evidenceSummary == null ? null : Descendants(evidenceSummary).OfType<Button>()
            .SingleOrDefault(button => string.Equals(AutomationProperties.GetAutomationId(button), "ExportEvidenceButton", StringComparison.Ordinal));
        Check(summaryText != null && exportButton != null && !exportButton.IsEnabled &&
              (int?)shell.GetType().GetProperty("EvidenceCount")?.GetValue(shell) == 0,
            "The High Contrast retained-evidence summary must show the expected empty count and disabled export control.");
        var disabledSurface = RequiredBrush(app, "DisabledButtonSurfaceBrush");
        var disabledText = RequiredBrush(app, "DisabledButtonTextBrush");
        var disabledBorder = RequiredBrush(app, "DisabledButtonBorderBrush");
        Check(exportButton.Opacity >= 0.99 && exportButton.Background is SolidColorBrush actualDisabledSurface &&
              actualDisabledSurface.Color == disabledSurface.Color && exportButton.Foreground is SolidColorBrush actualDisabledText &&
              actualDisabledText.Color == disabledText.Color && exportButton.BorderBrush is SolidColorBrush actualDisabledBorder &&
              actualDisabledBorder.Color == disabledBorder.Color,
            "High Contrast disabled buttons must use distinct theme colors without whole-control opacity dimming.");
        Check(ContrastRatio(disabledText.Color, disabledSurface.Color) >= 4.5 &&
              ContrastRatio(disabledBorder.Color, disabledSurface.Color) >= 3.0 &&
              disabledSurface.Color != RequiredBrush(app, "TemperedBrush").Color,
            "High Contrast disabled-button text and boundary must remain legible and visually distinguishable from enabled buttons.");
        AssertSingleLineTextFits(summaryText, "0 retained summary at 1360 DIP");
        var summaryBounds = Bounds(summaryText, evidenceSummary);
        var cardBounds = new Rect(0, 0, evidenceSummary.ActualWidth, evidenceSummary.ActualHeight);
        var exportBounds = Bounds(exportButton, evidenceSummary);
        Check(summaryBounds.Left >= cardBounds.Left - 1 && summaryBounds.Top >= cardBounds.Top - 1 &&
              summaryBounds.Right <= cardBounds.Right + 1 && summaryBounds.Bottom <= cardBounds.Bottom + 1 &&
              !summaryBounds.IntersectsWith(exportBounds),
            $"The High Contrast retained-evidence summary must remain inside its card and clear the export button: summary {summaryBounds}, button {exportBounds}, card {cardBounds}.");
    }

    static void AssertSingleLineTextFits(TextBlock text, string context)
    {
        Check(text != null && text.IsVisible && text.ActualWidth > 0 && text.ActualHeight > 0,
            $"{context} must render visible text.");
        var pixelsPerDip = VisualTreeHelper.GetDpi(text).PixelsPerDip;
        var formatted = new FormattedText(text.Text ?? string.Empty, CultureInfo.CurrentCulture, text.FlowDirection,
            new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch), text.FontSize,
            Brushes.Black, pixelsPerDip);
        var maximumSingleLineHeight = Math.Max(formatted.Height + 3.0, formatted.Height * 1.3);
        Check(formatted.WidthIncludingTrailingWhitespace <= text.ActualWidth + 1.0 &&
              text.ActualHeight <= maximumSingleLineHeight,
            $"{context} must fit on one rendered line without clipping (text={text.ActualWidth:0.#}x{text.ActualHeight:0.#} DIP, " +
            $"measured line={formatted.WidthIncludingTrailingWhitespace:0.#}x{formatted.Height:0.#} DIP, wrapping={text.TextWrapping}).");
    }

    static void AssertDecorativeAccentsTheme(Application app, Window window, object shell, string themeId)
    {
        var toggle = Descendants(window).OfType<CheckBox>().Single(control =>
            string.Equals(AutomationProperties.GetAutomationId(control), "DecorativeAccentsToggle", StringComparison.Ordinal));
        var enabledBefore = (bool)shell.GetType().GetProperty("DecorativeAccentsEnabled").GetValue(shell);
        try
        {
            SetDecorativeAccents(toggle, shell, false);
            Render(window);
            AssertDecorativeAccentVisualState(app, window, themeId, false);

            SetDecorativeAccents(toggle, shell, true);
            Render(window);
            AssertDecorativeAccentVisualState(app, window, themeId, true);
        }
        finally
        {
            SetDecorativeAccents(toggle, shell, enabledBefore);
            Render(window);
        }
        Check((bool)shell.GetType().GetProperty("DecorativeAccentsEnabled").GetValue(shell) == enabledBefore,
            "The render suite did not restore the session-only decorative accents setting.");
    }

    static void SetDecorativeAccents(CheckBox toggle, object shell, bool enabled)
    {
        var binding = BindingOperations.GetBindingExpression(toggle, CheckBox.IsCheckedProperty);
        Check(binding != null, "The decorative accents toggle lost its active MVVM binding during state changes.");
        toggle.SetCurrentValue(CheckBox.IsCheckedProperty, enabled);
        binding.UpdateSource();
        Check(toggle.IsChecked == enabled &&
              (bool)shell.GetType().GetProperty("DecorativeAccentsEnabled").GetValue(shell) == enabled,
            "The decorative accents toggle did not update its two-way session property.");
    }

    static void AssertDecorativeAccentVisualState(Application app, Window window, string themeId, bool enabled)
    {
        var highContrast = string.Equals(themeId, "high-contrast", StringComparison.OrdinalIgnoreCase);
        var textureVisibility = app.TryFindResource("DecorativeTextureVisibility") is Visibility.Visible;
        var shouldShowDecorativeOverlays = enabled && textureVisibility;
        var shouldUseIllustratedSurfaces = enabled && !highContrast;
        var titleBar = Descendants(window).OfType<Border>().Single(border => border.Name == "TitleBarBorder");
        var titleCorner = FindImage(window, "titlebar-heraldic-corner.png");
        var journalMark = FindImage(window, "parchment-field-journal-ornament-v1.png");
        var titleGrid = titleBar.Child as Grid;
        var titleArtLayers = Descendants(window).OfType<Border>().Where(border =>
            border.Background is ImageBrush brush &&
            (brush.ImageSource?.ToString().IndexOf("titlebar-cartographic-panorama-rev072.png", StringComparison.OrdinalIgnoreCase) >= 0 ||
             brush.ImageSource?.ToString().IndexOf("titlebar-botanical-band-v1.png", StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
        var cartographicBand = titleArtLayers.SingleOrDefault(border =>
            border.Background is ImageBrush brush && brush.ImageSource?.ToString().IndexOf("titlebar-cartographic-panorama-rev072.png", StringComparison.OrdinalIgnoreCase) >= 0);
        var botanicalRule = titleArtLayers.SingleOrDefault(border =>
            border.Background is ImageBrush brush && brush.ImageSource?.ToString().IndexOf("titlebar-botanical-band-v1.png", StringComparison.OrdinalIgnoreCase) >= 0);
        var cartographicBandDataContext = cartographicBand?.DataContext;
        var cartographicBandAccents = cartographicBandDataContext?.GetType().GetProperty("DecorativeAccentsEnabled")?.GetValue(cartographicBandDataContext);
        var windowDataContext = window.DataContext;
        var windowAccents = windowDataContext?.GetType().GetProperty("DecorativeAccentsEnabled")?.GetValue(windowDataContext);
        var cartographicBandBinding = cartographicBand == null ? null : BindingOperations.GetMultiBindingExpression(cartographicBand, UIElement.VisibilityProperty);
        var seal = Descendants(window).OfType<Button>().Single(button =>
            string.Equals(AutomationProperties.GetAutomationId(button), "HeaderSealButton", StringComparison.Ordinal));
        var sealImages = Descendants(seal).OfType<Image>().ToArray();
        var compass = sealImages.Single(image => image.Source?.ToString().IndexOf("header-heraldic-compass-v2.png", StringComparison.OrdinalIgnoreCase) >= 0);
        var headerCorner = sealImages.Single(image => image.Source?.ToString().IndexOf("header-corner-engraving-v1.png", StringComparison.OrdinalIgnoreCase) >= 0);
        var toolOverlay = FindImage(window, "dossier-corner-ornament-hq-rev083.png");
        var emptyState = Descendants(window).OfType<FrameworkElement>().Single(element =>
            string.Equals(AutomationProperties.GetAutomationId(element), "EvidenceLedgerEmptyState", StringComparison.Ordinal));
        var evidenceImage = FindImage(emptyState, "evidence-ledger-empty-v1.png");
        var evidenceAction = Descendants(emptyState).OfType<Button>().Single(button =>
            string.Equals(AutomationProperties.GetAutomationId(button), "EvidenceLedgerOpenPaletteButton", StringComparison.Ordinal));
        var headerBorder = FindAncestor<Border>(seal, window);
        var toolTitleBorder = FindAncestor<Border>(toolOverlay, window);
        var railViewport = Descendants(window).OfType<ListBox>().Single(view =>
            string.Equals(AutomationProperties.GetAutomationId(view), "OperationalRailToolViewport", StringComparison.Ordinal));
        var railCompass = FindImage(window, "rail-field-compass-v1.png");
        var railBorder = FindAncestor<Border>(railViewport, window);
        var workOrderStrip = Descendants(window).OfType<Border>().Single(border =>
            string.Equals(AutomationProperties.GetAutomationId(border), "WorkOrderCards", StringComparison.Ordinal));
        var workOrderCards = Descendants(workOrderStrip).OfType<Border>()
            .Where(card => AutomationProperties.GetAutomationId(card) is
                "WorkOrderContextStatus" or "WorkOrderPermissionStatus" or "WorkOrderReportStatus" or
                "WorkOrderApplicationStatus" or "RetainedEvidenceSummary")
            .ToArray();
        var workOrderRows = workOrderCards.Select(card => Math.Round(Bounds(card, workOrderStrip).Top, 0)).Distinct().Count();
        var expectedWorkOrderRows = 1;
        var workOrderTextWraps = workOrderCards.SelectMany(card => Descendants(card).OfType<TextBlock>())
            .Where(block => block.IsVisible)
            .All(block => block.TextWrapping == TextWrapping.Wrap ||
                          BindingOperations.GetBinding(block, TextBlock.TextProperty)?.Path?.Path == "EvidenceSummary" &&
                          block.TextWrapping == TextWrapping.NoWrap);
        Check(workOrderStrip.Child is WrapPanel && workOrderCards.Length == 5 &&
              workOrderCards.All(card => Math.Abs(card.Width - 178) < 0.01) && workOrderRows == expectedWorkOrderRows && workOrderTextWraps,
            $"Work-order summaries must keep all five bounded cards visible while labels and long values wrap at narrow and normal widths (width={window.ActualWidth:0.#}, rows={workOrderRows}).");
        var workbenchArtNames = new[]
        {
            "workbench-cartographic-board-hq-rev083.png",
            "workbench-heraldic-rail-portrait-rev064.png",
            "workbench-heraldic-shield-v1.png"
        };
        var workbenchArt = Descendants(window).OfType<Image>()
            .Where(image => workbenchArtNames.Any(name =>
                image.Source?.ToString().IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
            .ToArray();
        Check(workbenchArt.All(image => !image.IsHitTestVisible && !image.Focusable),
            $"New workbench illustrations must remain passive and must not capture input; offending images: {string.Join(", ", workbenchArt.Where(image => image.IsHitTestVisible || image.Focusable).Select(image => image.Source))}.");
        Check(workbenchArt.All(image => IsLocalTextureUri(image.Source)),
            "New workbench illustrations must load from the packaged local Desktop texture resources.");
        Check(workbenchArt.Where(image => image.Source?.ToString().Contains("workbench-heraldic-shield-v1.png", StringComparison.OrdinalIgnoreCase) != true)
                          .All(image => RenderOptions.GetBitmapScalingMode(image) == BitmapScalingMode.HighQuality),
            "Scaled cartographic and rail illustrations must use high-quality bitmap sampling.");
        Check(workbenchArt.Length == workbenchArtNames.Length &&
              workbenchArt.All(image => image.IsVisible == shouldShowDecorativeOverlays),
            $"Workbench art must follow the active theme and decorative-accent setting (theme={themeId}, accents={enabled}).");

        var showHeraldicMark = enabled && app.TryFindResource("TitleBarHeraldicMarkVisibility") is Visibility.Visible;
        var showJournalMark = enabled && app.TryFindResource("TitleBarJournalMarkVisibility") is Visibility.Visible;
        Check(titleCorner != null && titleCorner.IsVisible == showHeraldicMark &&
              !titleCorner.IsHitTestVisible && IsLocalTextureUri(titleCorner.Source) &&
              journalMark != null && AutomationProperties.GetAutomationId(journalMark) == "ParchmentTitleBarJournalMark" &&
              journalMark.Width == 30 && journalMark.Height == 30 && Math.Abs(journalMark.Opacity - 0.86) < 0.001 &&
              journalMark.IsHitTestVisible == false && IsLocalTextureUri(journalMark.Source) && journalMark.IsVisible == showJournalMark &&
              headerCorner.IsVisible == shouldShowDecorativeOverlays && toolOverlay.IsVisible == shouldShowDecorativeOverlays,
             $"Theme {themeId} must show its configured passive title-bar mark and other decorative overlays only when accents are enabled.");
        var titleFrame = Descendants(window).OfType<Border>().SingleOrDefault(border =>
            string.Equals(AutomationProperties.GetAutomationId(border), "TitleBarHeraldicFrameDecoration", StringComparison.Ordinal));
        var railTexture = FindImage(window, "workbench-heraldic-rail-portrait-rev064.png");
        Check(titleFrame == null &&
              railTexture != null && railTexture.IsHitTestVisible == false && !railTexture.Focusable &&
              railTexture.Visibility == (shouldShowDecorativeOverlays ? Visibility.Visible : Visibility.Collapsed) &&
              railTexture.IsVisible == shouldShowDecorativeOverlays && railTexture.Stretch == Stretch.Uniform &&
              RenderOptions.GetBitmapScalingMode(railTexture) == BitmapScalingMode.HighQuality &&
              Math.Abs(railTexture.Opacity - Convert.ToDouble(app.TryFindResource("Token.RailIllustrationOpacity"), CultureInfo.InvariantCulture)) < 0.001 &&
              railTexture.Opacity is >= 0.10 and <= 0.25 && IsLocalTextureUri(railTexture.Source) &&
              AutomationProperties.GetAutomationId(railTexture) == "OperationalRailTextureDecoration",
            $"Theme {themeId} must show only the fitted, passive portrait rail artwork when accents are enabled and avoid a duplicate title-bar compass frame.");
        var cartographicBandBrush = cartographicBand?.Background as ImageBrush;
        var botanicalRuleBrush = botanicalRule?.Background as ImageBrush;
        var expectedCartographicUri = "titlebar-cartographic-panorama-rev072.png";
        var expectedBotanicalRuleUri = "titlebar-botanical-band-v1.png";
        Check(titleArtLayers.Length == 2 && cartographicBand != null && botanicalRule != null &&
              cartographicBand.IsHitTestVisible == false && botanicalRule.IsHitTestVisible == false &&
              RenderOptions.GetBitmapScalingMode(cartographicBand) == BitmapScalingMode.HighQuality &&
              cartographicBand.IsVisible == shouldShowDecorativeOverlays && botanicalRule.IsVisible == shouldShowDecorativeOverlays &&
              cartographicBandBrush != null && IsLocalTextureUri(cartographicBandBrush.ImageSource) &&
              botanicalRuleBrush != null && IsLocalTextureUri(botanicalRuleBrush.ImageSource) &&
              cartographicBandBrush.ImageSource.ToString().IndexOf(expectedCartographicUri, StringComparison.OrdinalIgnoreCase) >= 0 &&
              botanicalRuleBrush.ImageSource.ToString().IndexOf(expectedBotanicalRuleUri, StringComparison.OrdinalIgnoreCase) >= 0 &&
              Math.Abs(cartographicBandBrush.Opacity - 0.18) < 0.001 && Math.Abs(botanicalRuleBrush.Opacity - 0.28) < 0.001 &&
              cartographicBandBrush.Stretch == Stretch.Fill && botanicalRuleBrush.Stretch == Stretch.UniformToFill &&
              Math.Abs(cartographicBandBrush.Viewbox.X) < 0.001 && Math.Abs(cartographicBandBrush.Viewbox.Y) < 0.001 &&
              Math.Abs(cartographicBandBrush.Viewbox.Width - 1.0) < 0.001 && Math.Abs(cartographicBandBrush.Viewbox.Height - 1.0) < 0.001 &&
              Math.Abs(botanicalRuleBrush.Viewbox.Y - 0.74) < 0.001 && Math.Abs(botanicalRuleBrush.Viewbox.Height - 0.259) < 0.001 &&
              titleGrid != null && titleGrid.Children.Count > 0 && ReferenceEquals(titleGrid.Children[0], cartographicBand) &&
              Math.Abs(botanicalRule.Height - 2) < 0.01,
             $"Theme {themeId} cartographic band/botanical rule must be passive, local, low-opacity first/edge layers shown only with accents " +
             $"(layers={titleArtLayers.Length}, bandFound={cartographicBand != null}, ruleFound={botanicalRule != null}, " +
             $"bandVisibility={cartographicBand?.Visibility}/{cartographicBand?.IsVisible}, ruleVisibility={botanicalRule?.Visibility}/{botanicalRule?.IsVisible}, " +
              $"titleBarVisible={titleBar.IsVisible}, accentResource={app.TryFindResource("DecorativeTextureVisibility")}, " +
              $"bandDataContext={cartographicBandDataContext?.GetType().Name}, bandAccents={cartographicBandAccents}, " +
              $"windowAccents={windowAccents}, sameDataContext={ReferenceEquals(cartographicBandDataContext, windowDataContext)}, bandBinding={cartographicBandBinding?.Status}, " +
              $"bandFirst={titleGrid != null && titleGrid.Children.Count > 0 && ReferenceEquals(titleGrid.Children[0], cartographicBand)}, " +
              $"bandParent={cartographicBand?.Parent?.GetType().Name}, ruleParent={botanicalRule?.Parent?.GetType().Name}, " +
              $"bandOpacity={(cartographicBand?.Background as ImageBrush)?.Opacity}, ruleOpacity={(botanicalRule?.Background as ImageBrush)?.Opacity}, " +
              $"bandViewbox={(cartographicBand?.Background as ImageBrush)?.Viewbox}, ruleViewbox={(botanicalRule?.Background as ImageBrush)?.Viewbox}).");
        Check(railCompass != null && AutomationProperties.GetAutomationId(railCompass) == "OperationalRailCompassDecoration" &&
              railCompass.IsHitTestVisible == false && railCompass.IsVisible == shouldShowDecorativeOverlays &&
              railCompass.Width == 18 && railCompass.Height == 18 && Math.Abs(railCompass.Opacity - 0.44) < 0.001 &&
              IsLocalTextureUri(railCompass.Source),
            $"Theme {themeId} must gate the passive local Operations compass with the other decorative accents.");
        var railHeading = Descendants(window).OfType<TextBlock>().SingleOrDefault(block =>
            string.Equals(block.Text, app.TryFindResource("Ui.Operations") as string, StringComparison.Ordinal));
        var railHeadingGrid = railHeading == null ? null : VisualTreeHelper.GetParent(railHeading) as Grid;
        Check(railHeadingGrid != null && ReferenceEquals(VisualTreeHelper.GetParent(railCompass), railHeadingGrid) &&
              Grid.GetColumn(railCompass) == 0 && Grid.GetColumn(railHeading) == 1 &&
              railHeadingGrid.ColumnDefinitions.Count == 2 && Math.Abs(railHeadingGrid.ColumnDefinitions[0].Width.Value - 22) < 0.01,
            "The Operations compass and localized heading must retain separate fixed and flexible columns.");
        if (shouldShowDecorativeOverlays)
        {
            var compassBounds = Bounds(railCompass, railHeadingGrid);
            var headingBounds = Bounds(railHeading, railHeadingGrid);
            Check(compassBounds.Right <= headingBounds.Left + 1,
                $"The Operations compass overlaps its localized heading in theme {themeId}: compass {compassBounds}, heading {headingBounds}.");
        }
        Check(emptyState.IsVisible && evidenceAction.IsVisible && evidenceAction.IsEnabled &&
              evidenceImage != null && evidenceImage.IsVisible == shouldShowDecorativeOverlays && !evidenceImage.IsHitTestVisible,
            $"Theme {themeId} must preserve the empty-state CTA while gating only its passive illustration on accents and texture support.");
        Check(compass.IsVisible == (app.TryFindResource("HeaderImageVisibility") is Visibility.Visible) && seal.IsVisible,
             $"Theme {themeId} header compass visibility must follow its theme resource while the seal remains interactive.");
        Check((app.TryFindResource("HeaderFallbackVisibility") is Visibility.Visible) ==
              Descendants(seal).OfType<TextBlock>().Any(block => block.Visibility == Visibility.Visible && block.IsVisible && block.Text == "CF"),
             $"Theme {themeId} header fallback visibility does not match its configured theme resource.");

        var solidTitle = RequiredBrush(app, "TitleBarSurfaceSolidBrush");
        var solidFrame = RequiredBrush(app, "FrameSurfaceSolidBrush");
        var solidNavigation = RequiredBrush(app, "NavigationSurfaceSolidBrush");
        if (!shouldUseIllustratedSurfaces)
        {
            Check(titleBar.Background is SolidColorBrush titleSolid && titleSolid.Color == solidTitle.Color &&
                  headerBorder.Background is SolidColorBrush headerSolid && headerSolid.Color == solidFrame.Color &&
                  toolTitleBorder.Background is SolidColorBrush toolSolid && toolSolid.Color == solidFrame.Color &&
                  railBorder.Background is SolidColorBrush railSolid && railSolid.Color == solidNavigation.Color &&
                  workOrderCards.Length == 5 && workOrderCards.All(card => card.Background is SolidColorBrush cardSolid && cardSolid.Color == solidFrame.Color),
                 $"Theme {themeId} must render title, header, ToolPage, navigation, and card surfaces as solid theme colors when accents are off or High Contrast is active " +
                 $"(title={titleBar.Background?.GetType().Name}, header={headerBorder.Background?.GetType().Name}, tool={toolTitleBorder.Background?.GetType().Name}, " +
                 $"rail={railBorder.Background?.GetType().Name}, cards={workOrderCards.Length}:{string.Join(",", workOrderCards.Select(card => card.Background?.GetType().Name))}).");
        }
        else
        {
            Check(titleBar.Background is DrawingBrush && headerBorder.Background is DrawingBrush &&
                  toolTitleBorder.Background is DrawingBrush && railBorder.Background is DrawingBrush &&
                  workOrderCards.Length == 5 && workOrderCards.All(card => card.Background is DrawingBrush),
             $"Theme {themeId} must restore illustrated surface brushes only when accents are enabled outside High Contrast.");
        }
    }

    static void AssertPackagedTextureResources()
    {
        var assets = new[]
        {
            "war-table-illustrated-cloth.png",
            "parchment-cartographic-paper.png",
            "titlebar-embroidered-cloth.png",
            "titlebar-botanical-band-v1.png",
            "titlebar-cartographic-panorama-rev072.png",
            "titlebar-heraldic-corner.png",
            "header-heraldic-compass-v2.png",
            "header-corner-engraving-v1.png",
            "evidence-ledger-empty-v1.png",
            "rail-field-compass-v1.png",
            "parchment-field-journal-ornament-v1.png",
            "dossier-corner-ornament-hq-rev083.png",
            "workbench-cartographic-board-hq-rev083.png",
            "workbench-heraldic-rail-portrait-rev064.png",
            "workbench-heraldic-shield-v1.png"
        };
        long totalPackagedBytes = 0;
        long totalDecodedBytes = 0;
        foreach (var asset in assets)
        {
            var uri = new Uri("pack://application:,,,/CalradiaForge.Desktop;component/Resources/Textures/Optimized/" + asset, UriKind.Absolute);
            var info = Application.GetResourceStream(uri);
            Check(info?.Stream != null, "Desktop texture is missing from the compiled WPF resources: " + asset);
            var sourceUri = new Uri("pack://application:,,,/CalradiaForge.Desktop;component/Resources/Textures/" + asset, UriKind.Absolute);
            var hasUnoptimizedSource = false;
            try
            {
                var unoptimizedSource = Application.GetResourceStream(sourceUri);
                hasUnoptimizedSource = unoptimizedSource?.Stream != null;
                unoptimizedSource?.Stream?.Dispose();
            }
            catch (IOException)
            {
                // A missing pack resource is the expected result for full-size authoring masters.
            }
            Check(!hasUnoptimizedSource,
                "Full-size Desktop artwork must remain authoring input and must not be embedded in the runtime assembly: " + asset);
            byte[] bytes;
            using (info.Stream)
            using (var memory = new MemoryStream())
            {
                info.Stream.CopyTo(memory);
                bytes = memory.ToArray();
            }
            Check(bytes.Length >= 33 && bytes.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
                "Packaged Desktop texture is not a complete PNG: " + asset);
            var width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
            var height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
            Check(width > 0 && height > 0, "Packaged Desktop texture has invalid PNG dimensions: " + asset);
            totalPackagedBytes += bytes.Length;
            totalDecodedBytes += (long)width * height * 4;
            using var decoded = new MemoryStream(bytes, writable: false);
            var frame = BitmapFrame.Create(decoded, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            Check(frame.PixelWidth == width && frame.PixelHeight == height,
                "Packaged Desktop texture IHDR dimensions do not match its decoded bitmap: " + asset);
            if (asset == "titlebar-botanical-band-v1.png")
            {
                Check(width == 2172 && height == 98 && bytes[25] == 6,
                    "The title-bar botanical band must be cropped to its visible 2172x98 RGBA strip.");
                Check(Convert.ToHexString(SHA256.HashData(bytes)).Equals("6FACCBB7CCAF9D9687F408AA9C5B3C20F78933D7F4E91C2B20F0AE528B4E9A84", StringComparison.OrdinalIgnoreCase),
                    "Title-bar botanical strip changed from its deterministic output.");
            }
            if (asset == "titlebar-cartographic-panorama-rev072.png")
            {
                Check(width == 2172 && height == 67 && bytes[25] == 2,
                    "The Rev072 illustrated title-bar panorama must preserve its native width and use the declared 2172x67 RGB crop.");
                Check(Convert.ToHexString(SHA256.HashData(bytes)).Equals("084F72940EACB8FFD9FB8639C5EF4E53CA8E598A8BDCD5B59EC96D53D8FE4099", StringComparison.OrdinalIgnoreCase),
                    "The Rev072 illustrated title-bar panorama changed from its deterministic output.");
                Check(bytes.Length < 1_000_000 && (long)width * height * 4 < 1_000_000,
                    "The Rev072 illustrated title-bar panorama must remain below the approved texture budget.");
            }
            if (asset == "rail-field-compass-v1.png")
            {
                Check(width == 96 && height == 96 && bytes[25] == 6,
                    "The rail-field compass must be a compact 96x96 RGBA derivative.");
                Check(Convert.ToHexString(SHA256.HashData(bytes)).Equals("A5E1F4032BAFA603070980BC8A3355A01FA3D5543F5E7157838548D12CCA588B", StringComparison.OrdinalIgnoreCase),
                    "Rail-field compass derivative changed from its deterministic output.");
            }
            if (asset == "parchment-field-journal-ornament-v1.png")
            {
                Check(width == 96 && height == 96 && bytes[25] == 6,
                    "The Parchment title-bar journal mark must be a compact 96x96 RGBA derivative.");
                Check(Convert.ToHexString(SHA256.HashData(bytes)).Equals("57CBC28BA0E895A01CF2B9425911572CDDE582207A78D74EDF3649B35F6A14C1", StringComparison.OrdinalIgnoreCase),
                    "Parchment title-bar journal derivative changed from its deterministic output.");
            }
            if (asset == "titlebar-embroidered-cloth.png")
                Check(width == 2048 && height == 62 && bytes[25] == 2,
                    "The War Table title-bar material must package only its visible RGB band.");
            if (asset == "titlebar-heraldic-corner.png")
                Check(width == 96 && height == 96 && bytes[25] == 6,
                    "The small title-bar heraldic mark must be a compact 96x96 RGBA derivative.");
            if (asset == "header-heraldic-compass-v2.png")
                Check(width == 128 && height == 128 && bytes[25] == 6,
                    "The header command seal must be a compact 128x128 RGBA derivative.");
            if (asset == "header-corner-engraving-v1.png")
                Check(width == 192 && height == 64 && bytes[25] == 6,
                    "The header corner engraving must be a compact 192x64 RGBA derivative.");
            if (asset == "dossier-corner-ornament-hq-rev083.png")
            {
                Check(width == 320 && height == 315 && bytes[25] == 6,
                    "The Rev083 cartographer corner must be a compact transparent 320x315 RGBA illustration.");
                Check(Convert.ToHexString(SHA256.HashData(bytes)).Equals("F66ABA74BE06DCF89796D91F6466D3006201BA0F4022FB30D4340FAF3FB125B5", StringComparison.OrdinalIgnoreCase),
                    "The Rev083 dossier corner changed from its deterministic output.");
            }
            if (asset == "workbench-cartographic-board-hq-rev083.png")
            {
                Check(width == 640 && height == 290 && bytes[25] == 2,
                    "The Rev083 high-resolution workbench cartography must be a 640x290 RGB illustration.");
                Check(Convert.ToHexString(SHA256.HashData(bytes)).Equals("51D1A374F9623D03A2B7C4CE01B1CCC16090848C9F8D5107859935A177AADFF5", StringComparison.OrdinalIgnoreCase),
                    "The Rev083 workbench cartography changed from its deterministic output.");
            }
            if (asset == "workbench-heraldic-rail-portrait-rev064.png")
                Check(width == 320 && height == 640 && bytes[25] == 6,
                    "The portrait workbench rail illustration must be a 320x640 RGBA overlay.");
            if (asset == "workbench-heraldic-shield-v1.png")
                Check(width == 80 && height == 80 && bytes[25] == 6,
                    "The workbench heraldic shield must be an 80x80 RGBA illustration.");
            if (asset is "titlebar-botanical-band-v1.png" or "rail-field-compass-v1.png" or
                "titlebar-heraldic-corner.png" or "header-corner-engraving-v1.png" or
                "evidence-ledger-empty-v1.png" or "parchment-field-journal-ornament-v1.png" or
                "dossier-corner-ornament-hq-rev083.png" or
                "workbench-heraldic-rail-portrait-rev064.png" or
                "workbench-heraldic-shield-v1.png")
            {
                var rgba = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
                var stride = rgba.PixelWidth * 4;
                var rgbaPixels = new byte[stride * rgba.PixelHeight];
                rgba.CopyPixels(rgbaPixels, stride, 0);
                var minimumAlpha = 255;
                var maximumAlpha = 0;
                for (var index = 3; index < rgbaPixels.Length; index += 4)
                {
                    minimumAlpha = Math.Min(minimumAlpha, rgbaPixels[index]);
                    maximumAlpha = Math.Max(maximumAlpha, rgbaPixels[index]);
                }
                Check(minimumAlpha == 0 && maximumAlpha > 0 && maximumAlpha <= 255,
                    "Transparent local decoration must retain both transparent and visible pixels: " + asset);
                if (asset == "workbench-heraldic-shield-v1.png")
                    Check(minimumAlpha == 0 && maximumAlpha == 255,
                        "The workbench heraldic shield must retain fully transparent and fully opaque alpha extrema.");
            }
            if (asset == "evidence-ledger-empty-v1.png")
            {
                Check(width == 96 && height == 96 && bytes[25] == 6,
                    "The evidence-ledger illustration must remain compact and preserve its alpha channel.");
                Check(Convert.ToHexString(SHA256.HashData(bytes)).Equals("1A3A8B6FBB6CCE7CAB2E1EED285AAED2C8481D1AA0D4ADAA61476319DD61DE08", StringComparison.OrdinalIgnoreCase),
                    "The evidence-ledger derivative changed from its deterministic output.");
            }
        }
        foreach (var retired in new[] { "tool-card-top-corners-v1.png", "desktop-titlebar-heraldic-frame-v1.png", "desktop-rail-etched-field-v1.png", "desktop-card-corners-botanical-v1.png", "titlebar-cartographic-engraving-v2.png", "workbench-heraldic-rail-band-v1.png", "titlebar-cartographic-panorama-rev057.png", "dossier-corner-ornament-rev057.png", "workbench-cartographic-board-v1.png", "calradia-heraldic-crest-rev083.png", "parchment-tactical-map-rev083.png" })
        {
            var retiredUri = new Uri("pack://application:,,,/CalradiaForge.Desktop;component/Resources/Textures/Optimized/" + retired, UriKind.Absolute);
            StreamResourceInfo retiredResource = null;
            try { retiredResource = Application.GetResourceStream(retiredUri); }
            catch (IOException) { /* Missing pack resources may throw instead of returning null. */ }
            retiredResource?.Stream?.Dispose();
            Check(retiredResource == null, "Retired texture derivative must not be embedded in the Desktop assembly: " + retired);
        }
        Check(totalPackagedBytes < 8_950_120,
            $"The Desktop texture package exceeds the prior compressed cap plus its approved <1 MB artwork allowance; actual: {totalPackagedBytes} bytes.");
        Check(totalDecodedBytes < 16_810_047,
            $"The Desktop texture package exceeds the prior decoded RGBA cap plus its approved <1 MB artwork allowance; actual: {totalDecodedBytes} bytes.");
    }

    static Image FindImage(DependencyObject root, string assetName) => Descendants(root).OfType<Image>().SingleOrDefault(image =>
        image.Source?.ToString().IndexOf(assetName, StringComparison.OrdinalIgnoreCase) >= 0);

    static bool IsLocalTextureUri(ImageSource source) => source?.ToString().StartsWith(
        "pack://application:,,,/CalradiaForge.Desktop;component/Resources/Textures/Optimized/", StringComparison.OrdinalIgnoreCase) == true;

    static void AssertTitleBarThemeContrast(Application app, Window window, string themeId)
    {
        var controlSurface = RequiredBrush(app, "TitleBarControlBrush");
        var glyph = RequiredBrush(app, "TitleBarGlyphBrush");
        var hoverSurface = RequiredBrush(app, "TitleBarControlHoverBrush");
        var hoverGlyph = RequiredBrush(app, "TitleBarHoverGlyphBrush");
        var closeHoverGlyph = RequiredBrush(app, "TitleBarCloseHoverGlyphBrush");
        var closeHoverSurface = RequiredBrush(app, "TitleBarCloseHoverBrush");
        var focus = RequiredBrush(app, "FocusBrush");
        Check(ContrastRatio(glyph.Color, controlSurface.Color) >= 4.5,
            $"Theme {themeId} title-bar glyph contrast fell below 4.5:1 in the idle state.");
        Check(ContrastRatio(hoverGlyph.Color, hoverSurface.Color) >= 4.5,
            $"Theme {themeId} title-bar glyph contrast fell below 4.5:1 in the hover state.");
        Check(ContrastRatio(closeHoverGlyph.Color, closeHoverSurface.Color) >= 4.5,
            $"Theme {themeId} close-control glyph contrast fell below 4.5:1 in the close-hover state.");
        Check(ContrastRatio(focus.Color, controlSurface.Color) >= 3.0,
            $"Theme {themeId} title-bar keyboard-focus border contrast fell below 3:1.");

        if (string.Equals(themeId, "high-contrast", StringComparison.OrdinalIgnoreCase))
        {
            AssertHighContrastTitleBarArtworkContrast(app, window);
            AssertHighContrastRailArtworkContrast(app, window);
        }

        foreach (var id in new[] { "WindowMinimizeButton", "WindowMaximizeButton", "WindowRestoreButton", "WindowCloseButton" })
        {
            var button = FindTitleBarButton(window, id);
            Check(button != null && button.Background is SolidColorBrush actualSurface && actualSurface.Color == controlSurface.Color,
                $"Theme {themeId} title-bar button {id} does not use its opaque themed hit-area surface.");
            Check(button.Foreground is SolidColorBrush actualGlyph && actualGlyph.Color == glyph.Color,
                $"Theme {themeId} title-bar button {id} does not use its semantic glyph foreground.");
            var glyphShape = button.Content as System.Windows.Shapes.Path ?? Descendants(button).OfType<System.Windows.Shapes.Path>().FirstOrDefault();
            var glyphStroke = glyphShape == null ? null : BindingOperations.GetBinding(glyphShape, System.Windows.Shapes.Shape.StrokeProperty);
            Check(glyphShape != null && glyphStroke?.Path?.Path == "Foreground",
                $"Theme {themeId} title-bar button {id} does not bind its vector glyph to the themed button foreground.");
        }
    }

    static void AssertHighContrastTitleBarArtworkContrast(Application app, Window window)
    {
        var band = Descendants(window).OfType<Border>().SingleOrDefault(border =>
            border.Background is ImageBrush brush &&
            brush.ImageSource?.ToString().IndexOf("titlebar-cartographic-panorama-rev072.png", StringComparison.OrdinalIgnoreCase) >= 0);
        var imageBrush = band?.Background as ImageBrush;
        var title = Descendants(window).OfType<TextBlock>().FirstOrDefault(block =>
            string.Equals(block.Text, app.TryFindResource("Ui.AppTitle") as string, StringComparison.Ordinal));
        var foreground = title?.Foreground as SolidColorBrush;
        var surface = RequiredBrush(app, "TitleBarSurfaceSolidBrush");
        Check(band != null && band.IsVisible && !band.IsHitTestVisible && imageBrush != null && imageBrush.Opacity <= 0.18 &&
              foreground != null && IsLocalTextureUri(imageBrush.ImageSource),
            "High Contrast must show only the passive local cartographic strip at the approved low opacity behind its title.");

        var resourceUri = new Uri("pack://application:,,,/CalradiaForge.Desktop;component/Resources/Textures/Optimized/titlebar-cartographic-panorama-rev072.png", UriKind.Absolute);
        var resource = Application.GetResourceStream(resourceUri);
        Check(resource?.Stream != null, "High Contrast title-art contrast check could not read its packaged cartographic strip.");
        using (resource.Stream)
        {
            var frame = BitmapFrame.Create(resource.Stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var rgba = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            var stride = rgba.PixelWidth * 4;
            var pixels = new byte[stride * rgba.PixelHeight];
            rgba.CopyPixels(pixels, stride, 0);
            static double LinearChannel(byte value)
            {
                var channel = value / 255d;
                return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
            }
            var linearChannels = Enumerable.Range(0, 256).Select(value => LinearChannel((byte)value)).ToArray();
            var maximumSourceLuminance = 0d;
            for (var offset = 0; offset < pixels.Length; offset += 4)
            {
                var sourceLuminance = 0.2126 * linearChannels[pixels[offset + 2]] +
                                      0.7152 * linearChannels[pixels[offset + 1]] +
                                      0.0722 * linearChannels[pixels[offset]];
                maximumSourceLuminance = Math.Max(maximumSourceLuminance, sourceLuminance);
            }
            var surfaceLuminance = 0.2126 * linearChannels[surface.Color.R] +
                                   0.7152 * linearChannels[surface.Color.G] +
                                   0.0722 * linearChannels[surface.Color.B];
            var textLuminance = 0.2126 * linearChannels[foreground.Color.R] +
                                0.7152 * linearChannels[foreground.Color.G] +
                                0.0722 * linearChannels[foreground.Color.B];
            var maximumCompositeLuminance = imageBrush.Opacity * maximumSourceLuminance +
                                            (1 - imageBrush.Opacity) * surfaceLuminance;
            var minimumContrast = (Math.Max(textLuminance, maximumCompositeLuminance) + 0.05) /
                                  (Math.Min(textLuminance, maximumCompositeLuminance) + 0.05);
            Check(minimumContrast >= 4.5,
                $"High Contrast title text over the cartographic texture falls below 4.5:1 after compositing; worst sampled ratio {minimumContrast:0.00}:1.");
        }
    }

    static void AssertHighContrastRailArtworkContrast(Application app, Window window)
    {
        var railTexture = FindImage(window, "workbench-heraldic-rail-portrait-rev064.png");
        var surface = RequiredBrush(app, "NavigationSurfaceBrush");
        var paper = RequiredBrush(app, "PaperBrush");
        var muted = RequiredBrush(app, "MutedTextBrush");
        var expectedOpacity = Convert.ToDouble(app.TryFindResource("Token.RailIllustrationOpacity"), CultureInfo.InvariantCulture);
        Check(railTexture != null && railTexture.IsVisible && !railTexture.IsHitTestVisible && !railTexture.Focusable &&
              surface != null && paper != null && muted != null &&
              Math.Abs(railTexture.Opacity - expectedOpacity) < 0.001 && railTexture.Opacity <= 0.25 &&
              IsLocalTextureUri(railTexture.Source),
            "High Contrast rail artwork must remain a passive local overlay at its bounded theme opacity.");

        var resourceUri = new Uri("pack://application:,,,/CalradiaForge.Desktop;component/Resources/Textures/Optimized/workbench-heraldic-rail-portrait-rev064.png", UriKind.Absolute);
        var resource = Application.GetResourceStream(resourceUri);
        Check(resource?.Stream != null, "High Contrast rail-art contrast check could not read its packaged portrait artwork.");
        using (resource.Stream)
        {
            var frame = BitmapFrame.Create(resource.Stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var rgba = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            var stride = rgba.PixelWidth * 4;
            var pixels = new byte[stride * rgba.PixelHeight];
            rgba.CopyPixels(pixels, stride, 0);
            var maxDecorationContrast = 1d;
            var minPaperTextContrast = double.MaxValue;
            var minMutedTextContrast = double.MaxValue;
            for (var offset = 0; offset < pixels.Length; offset += 4)
            {
                var alpha = pixels[offset + 3] / 255d * railTexture.Opacity;
                if (alpha <= 0)
                    continue;

                var composite = Color.FromRgb(
                    (byte)Math.Round(pixels[offset + 2] * alpha + surface.Color.R * (1 - alpha)),
                    (byte)Math.Round(pixels[offset + 1] * alpha + surface.Color.G * (1 - alpha)),
                    (byte)Math.Round(pixels[offset] * alpha + surface.Color.B * (1 - alpha)));
                maxDecorationContrast = Math.Max(maxDecorationContrast, ContrastRatio(composite, surface.Color));
                minPaperTextContrast = Math.Min(minPaperTextContrast, ContrastRatio(paper.Color, composite));
                minMutedTextContrast = Math.Min(minMutedTextContrast, ContrastRatio(muted.Color, composite));
            }

            Check(maxDecorationContrast >= 1.5,
                $"High Contrast rail ornament is too faint against its solid surface; maximum sampled contrast was {maxDecorationContrast:0.00}:1.");
            Check(minPaperTextContrast >= 4.5 && minMutedTextContrast >= 4.5,
                $"High Contrast rail text over the artwork falls below 4.5:1; minimum paper/muted ratios were {minPaperTextContrast:0.00}:1 and {minMutedTextContrast:0.00}:1.");
        }
    }

    static void AssertHeaderSealAccess(Application app, Window window, object shell, string languageCode)
    {
        var seal = Descendants(window).OfType<Button>().SingleOrDefault(button =>
            string.Equals(AutomationProperties.GetAutomationId(button), "HeaderSealButton", StringComparison.Ordinal));
        Check(seal != null && seal.Visibility == Visibility.Visible && seal.IsVisible,
            "The interactive header seal is missing or hidden in " + languageCode + ".");
        var expectedName = app.TryFindResource("Ui.HeaderSeal") as string;
        Check(!string.IsNullOrWhiteSpace(expectedName) &&
              string.Equals(AutomationProperties.GetName(seal), expectedName, StringComparison.Ordinal) &&
              string.Equals(seal.ToolTip as string, expectedName, StringComparison.Ordinal),
            "The header seal does not expose the localized accessible name and tooltip for " + languageCode + ".");
        Check(seal.Focusable && seal.IsTabStop,
            "The header seal is not reachable by keyboard.");
        var commandBinding = BindingOperations.GetBinding(seal, Button.CommandProperty);
        var expectedCommand = shell.GetType().GetProperty("OpenPaletteCommand")?.GetValue(shell) as ICommand;
        Check(commandBinding?.Path?.Path == "OpenPaletteCommand" && expectedCommand != null && ReferenceEquals(seal.Command, expectedCommand),
            "The header seal is not bound to the shell's existing OpenPaletteCommand.");

        var images = Descendants(seal).OfType<Image>().ToArray();
        Check(images.Length == 2 && images.All(image => !image.IsHitTestVisible),
            "The header seal must contain two passive, non-intercepting local image overlays.");
        foreach (var expectedAsset in new[] { "header-heraldic-compass-v2.png", "header-corner-engraving-v1.png" })
        {
            var image = images.SingleOrDefault(item => item.Source?.ToString().IndexOf(expectedAsset, StringComparison.OrdinalIgnoreCase) >= 0);
            Check(image != null && IsLocalTextureUri(image.Source),
                "The header seal is missing its local image resource " + expectedAsset + ".");
        }
    }

    static void AssertHeaderSealTheme(Application app, Window window, object shell, string themeId)
    {
        var seal = Descendants(window).OfType<Button>().Single(button =>
            string.Equals(AutomationProperties.GetAutomationId(button), "HeaderSealButton", StringComparison.Ordinal));
        var images = Descendants(seal).OfType<Image>().ToArray();
        var mark = images.SingleOrDefault(image => image.Source?.ToString().IndexOf("header-heraldic-compass-v2.png", StringComparison.OrdinalIgnoreCase) >= 0);
        var engraving = images.SingleOrDefault(image => image.Source?.ToString().IndexOf("header-corner-engraving-v1.png", StringComparison.OrdinalIgnoreCase) >= 0);
        var highContrast = string.Equals(themeId, "high-contrast", StringComparison.OrdinalIgnoreCase);
        var accentsEnabled = shell.GetType().GetProperty("DecorativeAccentsEnabled")?.GetValue(shell) is bool enabled && enabled;
        var expectedMarkVisible = app.TryFindResource("HeaderImageVisibility") is Visibility.Visible;
        var expectedEngravingVisible = accentsEnabled && app.TryFindResource("DecorativeTextureVisibility") is Visibility.Visible;
        var expectedFallbackVisible = app.TryFindResource("HeaderFallbackVisibility") is Visibility.Visible;
        var fallback = Descendants(seal).OfType<TextBlock>().SingleOrDefault(block => block.Text == "CF");
        Check(mark != null && engraving != null && images.Length == 2 &&
              images.All(image => !image.IsHitTestVisible && !image.Focusable && IsLocalTextureUri(image.Source)) &&
              mark.IsVisible == expectedMarkVisible && engraving.IsVisible == expectedEngravingVisible &&
              (fallback?.IsVisible == true) == expectedFallbackVisible,
            $"Theme {themeId} seal imagery must follow its theme and accent resource gates, stay local, and remain passive.");
        if (highContrast)
        {
            var titleBarBorder = Descendants(window).OfType<Border>().SingleOrDefault(border => border.Name == "TitleBarBorder");
            Check(app.TryFindResource("TitleBarSurfaceBrush") is SolidColorBrush &&
                  app.TryFindResource("FrameSurfaceBrush") is SolidColorBrush &&
                  app.TryFindResource("NavigationSurfaceBrush") is SolidColorBrush &&
                  titleBarBorder?.Background is SolidColorBrush,
                "High Contrast must retain solid title, frame, and navigation surfaces even when passive seal decoration is enabled.");
            Check(expectedFallbackVisible && fallback?.IsVisible == true,
                "High Contrast must retain the plain CF seal fallback over its solid surface.");
        }
    }

    static void AssertTitleBarWindowStateContract(MainWindow window)
    {
        Check(window.WindowState == WindowState.Normal, "The synthetic Desktop render host must start in Normal state.");
        var minimize = FindTitleBarButton(window, "WindowMinimizeButton");
        var maximize = FindTitleBarButton(window, "WindowMaximizeButton");
        var restore = FindTitleBarButton(window, "WindowRestoreButton");
        var close = FindTitleBarButton(window, "WindowCloseButton");
        Check(minimize?.IsVisible == true && maximize?.IsVisible == true &&
              restore?.IsVisible != true && close?.IsVisible == true,
            "The normal title bar must show minimize, maximize, and close controls, with restore hidden.");

        var maximizeTrigger = maximize?.Style.Triggers.OfType<DataTrigger>().SingleOrDefault(trigger =>
            trigger.Binding is Binding binding && binding.Path?.Path == "WindowState" &&
            string.Equals(Convert.ToString(trigger.Value, CultureInfo.InvariantCulture), WindowState.Maximized.ToString(), StringComparison.Ordinal));
        var restoreBaseVisibility = restore?.Style.Setters.OfType<Setter>().FirstOrDefault(setter =>
            setter.Property == UIElement.VisibilityProperty);
        var restoreTrigger = restore?.Style.Triggers.OfType<DataTrigger>().SingleOrDefault(trigger =>
            trigger.Binding is Binding binding && binding.Path?.Path == "WindowState" &&
            string.Equals(Convert.ToString(trigger.Value, CultureInfo.InvariantCulture), WindowState.Maximized.ToString(), StringComparison.Ordinal));
        Check(maximizeTrigger?.Setters.OfType<Setter>().Any(setter =>
                  setter.Property == UIElement.VisibilityProperty && Equals(setter.Value, Visibility.Collapsed)) == true &&
              restoreBaseVisibility != null && Equals(restoreBaseVisibility.Value, Visibility.Collapsed) &&
              restoreTrigger?.Setters.OfType<Setter>().Any(setter =>
                  setter.Property == UIElement.VisibilityProperty && Equals(setter.Value, Visibility.Visible)) == true,
            "Maximize and Restore visibility must be bound to the WindowState without mutating the native window during render tests.");

        const BindingFlags nonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        Check(typeof(MainWindow).GetMethod("Window_MinimizeClick", nonPublicInstance) != null &&
              typeof(MainWindow).GetMethod("Window_MaximizeClick", nonPublicInstance) != null &&
              typeof(MainWindow).GetMethod("Window_RestoreClick", nonPublicInstance) != null &&
              typeof(MainWindow).GetMethod("Window_CloseClick", nonPublicInstance) != null &&
              minimize != null && maximize != null && restore != null && close != null &&
              window.WindowState == WindowState.Normal,
            "Title-bar action handlers must remain present while the non-activating render test leaves native window state untouched.");
    }

    static Button FindTitleBarButton(Window window, string automationId) => Descendants(window).OfType<Button>()
        .SingleOrDefault(button => string.Equals(AutomationProperties.GetAutomationId(button), automationId, StringComparison.Ordinal));

    static bool IsTitleBarButtonVisible(Window window, string automationId)
    {
        var button = FindTitleBarButton(window, automationId);
        return button != null && button.Visibility == Visibility.Visible && button.IsVisible;
    }

    static SolidColorBrush RequiredBrush(Application app, string key)
    {
        var brush = app.TryFindResource(key) as SolidColorBrush;
        Check(brush != null, "The active theme is missing its solid-color resource " + key + ".");
        return brush;
    }

    static bool HasTitleBarFocusAdornment(Style style)
    {
        for (var current = style; current != null; current = current.BasedOn)
        {
            var trigger = current.Triggers.OfType<Trigger>().FirstOrDefault(item =>
                item.Property == UIElement.IsKeyboardFocusedProperty && Equals(item.Value, true));
            if (trigger != null && trigger.Setters.OfType<Setter>().Any(setter => setter.Property == Control.BorderBrushProperty) &&
                trigger.Setters.OfType<Setter>().Any(setter => setter.Property == Control.BorderThicknessProperty)) return true;
        }
        return false;
    }

    static void AssertComboBoxStateContrast(Application app, string themeId)
    {
        var selectorStyle = app.TryFindResource(typeof(ComboBox)) as Style;
        var itemStyle = app.TryFindResource(typeof(ComboBoxItem)) as Style;
        Check(selectorStyle != null && itemStyle != null, "The implicit ComboBox and ComboBoxItem styles must be available.");

        var selectorTemplate = selectorStyle.Setters.OfType<Setter>()
            .FirstOrDefault(setter => setter.Property == Control.TemplateProperty)?.Value as ControlTemplate;
        var itemTemplate = itemStyle.Setters.OfType<Setter>()
            .FirstOrDefault(setter => setter.Property == Control.TemplateProperty)?.Value as ControlTemplate;
        Check(selectorTemplate != null && itemTemplate != null,
            $"Theme {themeId} ComboBox selectors and popup items must provide explicit templates.");

        var selectorFocus = selectorTemplate.Triggers.OfType<Trigger>().FirstOrDefault(trigger =>
            trigger.Property == UIElement.IsKeyboardFocusWithinProperty && Equals(trigger.Value, true));
        var focusBrushSetter = selectorFocus?.Setters.OfType<Setter>().FirstOrDefault(setter =>
            setter.TargetName == "ComboChrome" && setter.Property == Border.BorderBrushProperty);
        var focusThicknessSetter = selectorFocus?.Setters.OfType<Setter>().FirstOrDefault(setter =>
            setter.TargetName == "ComboChrome" && setter.Property == Border.BorderThicknessProperty);
        Check(UsesDynamicResource(focusBrushSetter, "FocusBrush") && UsesDynamicResource(focusThicknessSetter, "Token.FocusThickness"),
            $"Theme {themeId} ComboBox ControlTemplate focus state must apply FocusBrush and Token.FocusThickness to ComboChrome.");

        var selected = itemTemplate.Triggers.OfType<Trigger>().FirstOrDefault(trigger =>
            trigger.Property == System.Windows.Controls.Primitives.Selector.IsSelectedProperty && Equals(trigger.Value, true));
        var selectedSurfaceSetter = selected?.Setters.OfType<Setter>().FirstOrDefault(setter =>
            setter.TargetName == "ItemChrome" && setter.Property == Border.BackgroundProperty);
        var selectedTextSetter = selected?.Setters.OfType<Setter>().FirstOrDefault(setter =>
            setter.Property == Control.ForegroundProperty);
        Check(UsesDynamicResource(selectedSurfaceSetter, "ComboSelectedBrush") &&
              UsesDynamicResource(selectedTextSetter, "ComboSelectedTextBrush"),
            $"Theme {themeId} ComboBoxItem ControlTemplate must apply the selected text and surface resources.");
        var highlighted = itemTemplate.Triggers.OfType<Trigger>().FirstOrDefault(trigger =>
            trigger.Property == ComboBoxItem.IsHighlightedProperty && Equals(trigger.Value, true));
        var highlightedSurfaceSetter = highlighted?.Setters.OfType<Setter>().FirstOrDefault(setter =>
            setter.TargetName == "ItemChrome" && setter.Property == Border.BackgroundProperty);
        var highlightedTextSetter = highlighted?.Setters.OfType<Setter>().FirstOrDefault(setter =>
            setter.Property == Control.ForegroundProperty);
        Check(UsesDynamicResource(highlightedSurfaceSetter, "ComboHoverBrush") &&
              UsesDynamicResource(highlightedTextSetter, "ComboHoverTextBrush"),
            $"Theme {themeId} ComboBoxItem ControlTemplate must apply the highlighted text and surface resources.");

        var selectedForeground = (app.Resources["ComboSelectedTextBrush"] as SolidColorBrush)?.Color;
        var selectedSurface = (app.Resources["ComboSelectedBrush"] as SolidColorBrush)?.Color;
        var hoverForeground = (app.Resources["ComboHoverTextBrush"] as SolidColorBrush)?.Color;
        var highlightSurface = (app.Resources["ComboHoverBrush"] as SolidColorBrush)?.Color;
        var inputSurface = (app.Resources["InputBrush"] as SolidColorBrush)?.Color;
        var focus = (app.Resources["FocusBrush"] as SolidColorBrush)?.Color;
        Check(selectedForeground.HasValue && selectedSurface.HasValue && ContrastRatio(selectedForeground.Value, selectedSurface.Value) >= 4.5,
            $"Theme {themeId} selected ComboBox text contrast fell below 4.5:1.");
        Check(hoverForeground.HasValue && highlightSurface.HasValue && ContrastRatio(hoverForeground.Value, highlightSurface.Value) >= 4.5,
            $"Theme {themeId} highlighted ComboBox text contrast fell below 4.5:1.");
        Check(focus.HasValue && inputSurface.HasValue && ContrastRatio(focus.Value, inputSurface.Value) >= 3.0,
            $"Theme {themeId} ComboBox focus indicator contrast fell below 3:1.");

        ComboBox probe = null;
        ComboBoxItem selectedItem = null;
        Window host = null;
        try
        {
            probe = new ComboBox { ItemsSource = new[] { "First choice", "Selected choice" }, SelectedIndex = 1, Width = 240, Margin = new Thickness(12) };
            selectedItem = new ComboBoxItem { Content = "Selected choice", IsSelected = true };
            var probeContent = new StackPanel();
            probeContent.Children.Add(probe);
            probeContent.Children.Add(selectedItem);
            host = new Window
            {
                Content = probeContent,
                Width = 290,
                Height = 140,
                MinWidth = 0,
                MinHeight = 0,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                ShowActivated = false,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize
            };
            NonActivatingWindowBehavior.Apply(host);
            host.Show();
            Check(NonActivatingWindowBehavior.HasNoActivateStyle(host),
                "The off-screen ComboBox probe must carry WS_EX_NOACTIVATE before rendering starts.");
            probe.ApplyTemplate();
            selectedItem.ApplyTemplate();
            Render(host);

            Check(probe.SelectedIndex == 1,
                $"Theme {themeId} ComboBox did not preserve its selected value in the non-activating render host.");
            var comboChrome = probe.Template.FindName("ComboChrome", probe) as Border;
            var expectedFocusThickness = app.TryFindResource("Token.FocusThickness") as Thickness?;
            selectorStyle = app.TryFindResource(typeof(ComboBox)) as Style;
            var focusTrigger = selectorStyle?.Setters.OfType<Setter>().FirstOrDefault(setter => setter.Property == Control.TemplateProperty)?.Value as ControlTemplate;
            var focusBorderSetter = focusTrigger?.Triggers.OfType<Trigger>().FirstOrDefault(trigger =>
                trigger.Property == UIElement.IsKeyboardFocusWithinProperty && Equals(trigger.Value, true))?.Setters
                .OfType<Setter>().FirstOrDefault(setter => setter.TargetName == "ComboChrome" && setter.Property == Border.BorderBrushProperty);
            Check(comboChrome != null && focus.HasValue && expectedFocusThickness.HasValue &&
                  UsesDynamicResource(focusBorderSetter, "FocusBrush") && ContrastRatio(focus.Value, inputSurface.Value) >= 3.0,
                $"Theme {themeId} ComboBox must define a legible focus token without requiring system keyboard focus in the render host.");
            var itemForeground = selectedItem.Foreground as SolidColorBrush;
            var selectedItemChrome = selectedItem.Template.FindName("ItemChrome", selectedItem) as Border;
            var itemBackground = selectedItemChrome?.Background as SolidColorBrush;
            Check(itemForeground != null && itemBackground != null && ContrastRatio(itemForeground.Color, itemBackground.Color) >= 4.5 &&
                  selectedForeground.HasValue && itemForeground.Color == selectedForeground.Value &&
                  selectedSurface.HasValue && itemBackground.Color == selectedSurface.Value,
                $"Theme {themeId} realized selected ComboBox item does not meet 4.5:1 text contrast.");
        }
        finally
        {
            host?.Close();
        }
    }

    static bool UsesDynamicResource(Setter setter, object key)
    {
        return setter?.Value is DynamicResourceExtension resource && Equals(resource.ResourceKey, key);
    }

    static double ContrastRatio(Color left, Color right)
    {
        static double Channel(byte value)
        {
            var c = value / 255d;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        static double Luminance(Color color) => 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
        var a = Luminance(left);
        var b = Luminance(right);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }
}
