using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CalradiaForge.Desktop.Services;

namespace CalradiaForge.Desktop.Presentation
{
    public enum ModderRolePreset
    {
        All = 0,
        NarrativeDialogues = 1,
        TroopCombatArtisan = 2,
        EconomyWorldArchitect = 3,
        CoreDevPerformanceAuditor = 4
    }

    /// <summary>MVVM composition root for the tactical workbench. It owns state, not WPF controls.</summary>
    internal sealed class DesktopShellViewModel : ObservableObject, IDisposable
    {
        readonly DesktopWorkspaceService workspace;
        readonly DesktopMetricsService metrics;
        readonly DesktopLocalizationService localization;
        readonly DesktopThemeService theme;
        readonly DesktopPreferenceService preferences;
        readonly DesktopReportExportService reportExport;
        readonly Func<string, bool> clipboardWriter;
        readonly Func<ToolDefinition, bool, string> pickInput;
        readonly Dictionary<ToolDefinition, string> toolSearchText;
        readonly BatchObservableCollection<ToolDefinition> visibleTools;
        readonly BatchObservableCollection<ToolGroupViewModel> visibleGroups;
        readonly BatchObservableCollection<OperationalRailEntryViewModel> operationalRailEntries;
        readonly BatchObservableCollection<ToolDefinition> paletteTools;
        readonly List<WorkspaceEvidence> retainedEvidence = [];
        readonly List<ToolDefinition> pinned = [];
        readonly List<ToolDefinition> recent = [];
        ReadOnlyObservableCollection<ToolDefinition> pinnedToolsSnapshot;
        ReadOnlyObservableCollection<ToolDefinition> recentToolsSnapshot;
        readonly Dictionary<string, bool> groupExpansion = new(StringComparer.Ordinal);
        readonly Dictionary<ToolGroupViewModel, OperationalRailGroupEntryViewModel> groupRailEntries = [];
        readonly Dictionary<ToolDefinition, OperationalRailToolEntryViewModel> toolRailEntries = [];
        readonly HashSet<ToolGroupViewModel> observedGroups = [];
        ToolDefinition selectedTool;
        string selectedCategory = string.Empty;
        string filter = string.Empty;
        string paletteFilter = string.Empty;
        string status = "Ready";
        string languageCode = "en";
        string themeId = "war-table";
        string themeMessage = string.Empty;
        int searchFocusRequest;
        ToolPageViewModel currentPage;
        WorkbenchPageViewModel currentWorkbenchPage;
        bool isCommandPaletteOpen;
        bool isContextDossierOpen;
        bool isHookWorkbenchOpen;
        bool decorativeAccentsEnabled = true;
        bool isSplitDeckActive;
        ToolDefinition pinnedTool;
        string pinnedExecutionStatus = "Ready";
        string pinnedRawResult = string.Empty;
        readonly ObservableCollection<WorkspaceEvidence> pinnedEvidence = [];
        bool disposed;
        ModderRolePreset activeModderRole = ModderRolePreset.All;
        bool showFavoritesOnly;

        public DesktopShellViewModel(ToolCatalog catalog, DesktopWorkspaceService workspace, DesktopMetricsService metrics, DesktopLocalizationService localization, DesktopThemeService theme, DesktopPreferenceService preferences, Func<ToolDefinition, bool, string> pickInput = null, DesktopReportExportService reportExport = null, Func<string, bool> clipboardWriter = null)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            this.metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
            this.theme = theme ?? throw new ArgumentNullException(nameof(theme));
            this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
            this.pickInput = pickInput;
            this.reportExport = reportExport ?? new DesktopReportExportService();
            this.clipboardWriter = clipboardWriter ?? (_ => false);
            HookWorkbench = new HookWorkbenchViewModel(workspace.Session, (key, fallback) => localization.GetText(key, fallback));
            using var startupMeasurement = this.metrics.Start(
                "desktop-shell-initialize",
                "Synchronous WPF shell construction only; does not include first paint or system startup.");
            var saved = preferences.Load();
            this.theme.ApplyDefault();
            var applied = this.theme.Apply(saved.State.ThemeId);
            themeId = applied.ThemeId;
            languageCode = DesktopTextCatalog.ResolveCode(saved.State.LanguageCode);
            localization.Apply(languageCode);
            // Build the cached search strings after selecting the language resource dictionary.
            toolSearchText = Catalog.Tools.ToDictionary(
                item => item,
                item => item.Title + " " + item.Id + " " + item.Category);
            if (saved.UsedFallback) status = saved.Message ?? "Desktop preferences were reset.";
            else if (applied.UsedFallback) status = LocalizedThemeMessage(applied);
            Categories = new([ "All areas", ..Catalog.Categories ]);
            visibleTools = [];
            visibleGroups = [];
            operationalRailEntries = [];
            paletteTools = new(Catalog.Tools);
            VisibleTools = visibleTools;
            VisibleGroups = visibleGroups;
            OperationalRailEntries = operationalRailEntries;
            PaletteTools = paletteTools;
            SelectToolCommand = new(SelectTool, item => item is ToolDefinition);
            ToggleFavoriteCommand = new(ToggleFavorite, item => item is ToolDefinition);
            SelectPinnedCommand = new(SelectPinned, item => item is ToolDefinition);
            SelectPinnedSlotCommand = new(SelectPinnedSlot, IsPinnedSlot);
            OpenPaletteCommand = new(() => IsCommandPaletteOpen = true);
            ClosePaletteCommand = new(() => IsCommandPaletteOpen = false);
            SelectFirstPaletteToolCommand = new(SelectFirstPaletteTool, () => paletteTools.Count > 0);
            FocusSearchCommand = new(() => { SearchFocusRequest++; Status = "Search focused — type a title, identifier, or category."; });
            ClearFilterCommand = new(ClearFilter);
            RunPrimaryCommand = new(() => CurrentPage?.RunCommand.Execute(null), () => CurrentPage?.RunCommand.CanExecute(null) == true);
            ExportEvidenceCommand = new(ExportRetainedEvidenceAsync, () => !disposed && retainedEvidence.Count > 0);
            ApplyThemeCommand = new(ApplyTheme, value => value != null);
            ConnectCommand = new(ConnectAsync);
            OpenHookWorkbenchCommand = new(() => IsHookWorkbenchOpen = true);
            CloseHookWorkbenchCommand = new(() => IsHookWorkbenchOpen = false);
            ToggleSplitDeckCommand = new(ToggleSplitDeck);
            PinCurrentToolToSplitDeckCommand = new(PinCurrentToolToSplitDeck, () => CurrentPage != null);
            CloseSplitDeckCommand = new(() => IsSplitDeckActive = false);
            ClearPinnedDeckCommand = new(ClearPinnedDeck);
            CycleModderRoleCommand = new(CycleModderRole);
            ToggleFavoritesOnlyCommand = new(() => ShowFavoritesOnly = !ShowFavoritesOnly);
            ExportMarkdownReportCommand = new(ExportCurrentPageMarkdownAsync, () => CurrentPage != null && !string.IsNullOrWhiteSpace(CurrentPage.RawResult));
            PingConnectionCommand = new(PingConnectionAsync);
            RefreshVisibleTools();
            SelectedTool = VisibleTools.FirstOrDefault();
        }

        public ToolCatalog Catalog { get; }
        /// <summary>Declarative page/command metadata consumed by diagnostics and future templates.</summary>
        public IReadOnlyList<DeclarativeUiDescriptor> UiDescriptors => DeclarativeUiCatalog.Descriptors;
        public ReadOnlyCollection<string> Categories { get; }
        public ObservableCollection<ToolDefinition> VisibleTools { get; }
        public ObservableCollection<ToolGroupViewModel> VisibleGroups { get; }
        public ObservableCollection<OperationalRailEntryViewModel> OperationalRailEntries { get; }
        public ObservableCollection<ToolDefinition> PaletteTools { get; }
        public ReadOnlyObservableCollection<ToolDefinition> PinnedTools => pinnedToolsSnapshot ??= CreateReadOnlySnapshot(pinned);
        public ReadOnlyObservableCollection<ToolDefinition> RecentTools => recentToolsSnapshot ??= CreateReadOnlySnapshot(recent);
        public RelayCommand SelectToolCommand { get; }
        public RelayCommand ToggleFavoriteCommand { get; }
        public RelayCommand SelectPinnedCommand { get; }
        public RelayCommand SelectPinnedSlotCommand { get; }
        public RelayCommand OpenPaletteCommand { get; }
        public RelayCommand ClosePaletteCommand { get; }
        public RelayCommand SelectFirstPaletteToolCommand { get; }
        public RelayCommand FocusSearchCommand { get; }
        public RelayCommand ClearFilterCommand { get; }
        public RelayCommand RunPrimaryCommand { get; }
        public AsyncRelayCommand ExportEvidenceCommand { get; }
        public RelayCommand ApplyThemeCommand { get; }
        public AsyncRelayCommand ConnectCommand { get; }
        public RelayCommand OpenHookWorkbenchCommand { get; }
        public RelayCommand CloseHookWorkbenchCommand { get; }
        public AsyncRelayCommand PingConnectionCommand { get; }
        public RelayCommand ToggleSplitDeckCommand { get; }
        public RelayCommand PinCurrentToolToSplitDeckCommand { get; }
        public RelayCommand CloseSplitDeckCommand { get; }
        public RelayCommand ClearPinnedDeckCommand { get; }
        public RelayCommand ToggleFavoritesOnlyCommand { get; }
        public AsyncRelayCommand ExportMarkdownReportCommand { get; }
        public bool ShowFavoritesOnly
        {
            get => showFavoritesOnly;
            set
            {
                if (!Set(ref showFavoritesOnly, value)) return;
                RefreshVisibleTools();
                EnsureSelectedToolVisible();
                Status = showFavoritesOnly ? "Filtering favorite tools only" : "Showing all tools";
            }
        }
        public string FilteredToolsCountText => $"{VisibleTools.Count} / {Catalog.Tools.Count} TOOLS";
        public bool IsSplitDeckActive { get => isSplitDeckActive; set => Set(ref isSplitDeckActive, value); }
        public ToolDefinition PinnedTool { get => pinnedTool; private set { Set(ref pinnedTool, value); Raise(nameof(HasPinnedDeckContent)); Raise(nameof(PinnedToolTitle)); Raise(nameof(PinnedToolCategory)); } }
        public string PinnedToolTitle => PinnedTool?.Title ?? localization.GetText(
            pinnedEvidence.Count > 0 ? "Ui.PinnedToolOutput" : "Ui.NoToolPinned",
            pinnedEvidence.Count > 0 ? "Pinned Tool Output" : "No Tool Pinned");
        public string PinnedToolCategory => PinnedTool?.Category ?? (pinnedEvidence.Count > 0
            ? localization.GetText("Ui.WorkspaceEvidence", "Workspace Evidence")
            : string.Empty);
        public string PinnedExecutionStatus { get => pinnedExecutionStatus; private set => Set(ref pinnedExecutionStatus, value); }
        public string PinnedRawResult { get => pinnedRawResult; private set => Set(ref pinnedRawResult, value); }
        public ObservableCollection<WorkspaceEvidence> PinnedEvidence => pinnedEvidence;
        public bool HasPinnedDeckContent => PinnedTool != null || pinnedEvidence.Count > 0;
        public ToolPageViewModel CurrentPage { get => currentPage; private set { if (Set(ref currentPage, value)) ExportMarkdownReportCommand?.NotifyCanExecuteChanged(); } }
        public WorkbenchPageViewModel CurrentWorkbenchPage { get => currentWorkbenchPage; private set => Set(ref currentWorkbenchPage, value); }
        public SessionStatusViewModel Session { get; } = new();
        public HookWorkbenchViewModel HookWorkbench { get; }
        public bool IsHookWorkbenchOpen
        {
            get => isHookWorkbenchOpen;
            set
            {
                if (!Set(ref isHookWorkbenchOpen, value)) return;
                if (selectedTool != null && toolRailEntries.TryGetValue(selectedTool, out var selectedEntry))
                    selectedEntry.IsSelected = !value;
                Raise(nameof(ActiveRouteStatus));
                if (value)
                {
                    HookWorkbench.NotifyConnectionChanged();
                    if (HookWorkbench.RefreshSnapshotsCommand.CanExecute(null)) HookWorkbench.RefreshSnapshotsCommand.Execute(null);
                }
                else
                {
                    // A remembered tool may have been filtered out while Hook Workbench
                    // was active. Reconcile against the visible rail before returning to
                    // the regular route so the active page always has a reachable entry.
                    EnsureSelectedToolVisible();
                }
            }
        }
        public string ActiveRouteStatus => IsHookWorkbenchOpen
            ? localization.GetText("Ui.HookWorkbench", "Hook Workbench")
            : Status;
        public ToolDefinition SelectedTool
        {
            get => selectedTool;
            set
            {
                // Selecting the remembered route from the rail is still navigation even
                // when it is already the selected tool beneath the Hook Workbench route.
                if (value != null)
                {
                    IsHookWorkbenchOpen = false;
                    IsCommandPaletteOpen = false;
                }
                var previous = selectedTool;
                if (!Set(ref selectedTool, value)) return;
                if (previous != null && toolRailEntries.TryGetValue(previous, out var previousEntry))
                    previousEntry.IsSelected = false;
                if (value != null && toolRailEntries.TryGetValue(value, out var selectedEntry))
                    selectedEntry.IsSelected = !IsHookWorkbenchOpen;
                if (value == null)
                {
                    ReleaseCurrentPage();
                    return;
                }
                Select(value);
            }
        }
        public string SelectedCategory
        {
            get => string.IsNullOrEmpty(selectedCategory) ? Categories.FirstOrDefault() ?? string.Empty : selectedCategory;
            set
            {
                var allAreas = Categories.FirstOrDefault();
                var next = string.Equals(value, allAreas, StringComparison.Ordinal) ? string.Empty : value ?? string.Empty;
                if (!Set(ref selectedCategory, next)) return;
                RefreshVisibleTools();
                EnsureSelectedToolVisible();
            }
        }
        public string Filter
        {
            get => filter;
            set
            {
                if (!Set(ref filter, value ?? string.Empty)) return;
                RefreshVisibleTools();
                EnsureSelectedToolVisible();
            }
        }
        internal void ClearFilter()
        {
            Filter = string.Empty;
            SearchFocusRequest++;
        }

        void EnsureSelectedToolVisible()
        {
            // Changing rail filters while the dedicated Hook route is selected must not
            // silently navigate away from it or discard the remembered tool route.
            if (IsHookWorkbenchOpen || VisibleTools.Contains(SelectedTool)) return;
            SelectedTool = VisibleTools.FirstOrDefault();
        }
        public string PaletteFilter
        {
            get => paletteFilter;
            set
            {
                if (!Set(ref paletteFilter, value ?? string.Empty)) return;
                var query = paletteFilter.Trim();
                paletteTools.ReplaceAll(FilterPaletteTools(query));
                SelectFirstPaletteToolCommand?.NotifyCanExecuteChanged();
            }
        }
        public bool IsCommandPaletteOpen { get => isCommandPaletteOpen; set => Set(ref isCommandPaletteOpen, value); }
        /// <summary>Transient presentation state for the contextual command and help dossier.</summary>
        public bool IsContextDossierOpen { get => isContextDossierOpen; set => Set(ref isContextDossierOpen, value); }
        public bool DecorativeAccentsEnabled { get => decorativeAccentsEnabled; set => Set(ref decorativeAccentsEnabled, value); }
        public bool IsEvidenceExpanded { get => CurrentWorkbenchPage?.EvidenceLedger.IsExpanded == true; set { if (CurrentWorkbenchPage != null) CurrentWorkbenchPage.EvidenceLedger.IsExpanded = value; Raise(); } }
        public string Status
        {
            get => status;
            private set
            {
                if (Set(ref status, value)) Raise(nameof(ActiveRouteStatus));
            }
        }
        public int SearchFocusRequest { get => searchFocusRequest; private set => Set(ref searchFocusRequest, value); }
        public string LanguageCode
        {
            get => languageCode;
            set
            {
                var next = DesktopTextCatalog.ResolveCode(value);
                if (!Set(ref languageCode, next)) return;
                using (metrics.Start("language:" + next, "Short desktop resource change only; not a game-performance attribution.")) localization.Apply(next);
                Raise(nameof(ActiveRouteStatus));
                HookWorkbench.NotifyLocalizationChanged();
                CurrentPage?.RefreshLocalizedText();
                Raise(nameof(ActiveModderRoleLabel));
                Raise(nameof(ActiveModderRoleHint));
                var apiDeprecationTool = Catalog.Find(ToolDefinition.ApiDeprecationToolId);
                if (apiDeprecationTool != null)
                {
                    apiDeprecationTool.RefreshLocalizedText();
                    toolSearchText[apiDeprecationTool] = apiDeprecationTool.Title + " " + apiDeprecationTool.Id + " " + apiDeprecationTool.Category;
                    RefreshVisibleTools();
                    paletteTools.ReplaceAll(FilterPaletteTools(paletteFilter.Trim()));
                }
                Raise(nameof(PinnedToolTitle));
                Raise(nameof(PinnedToolCategory));
                PersistPreferences();
                Raise(nameof(AvailableThemes));
                Status = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    localization.GetText("Ui.LanguageChanged", "Language changed to {0}"), next);
            }
        }
        public IReadOnlyList<DesktopThemeDescriptor> AvailableThemes => theme.AvailableThemes;
        public string ThemeId
        {
            get => themeId;
            set
            {
                var next = theme.ResolveThemeId(value);
                if (string.Equals(next, themeId, StringComparison.OrdinalIgnoreCase)) return;
                using (metrics.Start("theme:" + next, "Short desktop resource change only; not a game-performance attribution."))
                {
                    var result = theme.Apply(next);
                    if (!result.Succeeded)
                    {
                        ThemeMessage = LocalizedThemeMessage(result);
                        Status = ThemeMessage;
                        return;
                    }
                    Set(ref themeId, result.ThemeId);
                    Raise(nameof(SelectedTheme));
                    ThemeMessage = result.UsedFallback ? LocalizedThemeMessage(result) : string.Empty;
                    PersistPreferences();
                }
                Status = string.IsNullOrWhiteSpace(ThemeMessage) ? "Theme changed to " + ThemeId : ThemeMessage;
            }
        }
        public string ThemeMessage { get => themeMessage; private set => Set(ref themeMessage, value ?? string.Empty); }
        public DesktopThemeDescriptor SelectedTheme => AvailableThemes.FirstOrDefault(item => string.Equals(item.Id, ThemeId, StringComparison.OrdinalIgnoreCase));
        public IReadOnlyList<WorkspaceEvidence> RetainedEvidence => retainedEvidence;
        public IReadOnlyCollection<string> SupportedLanguages => DesktopLocalizationService.SupportedResourceCodes;
        public int EvidenceCount => retainedEvidence.Count;
        public string ConnectionState => workspace.IsConnected ? "Connected" : "Disconnected";
        public string ConnectionLatencyText => workspace.Session?.LastRoundTripLatencyMs.HasValue == true
            ? $"{workspace.Session.LastRoundTripLatencyMs.Value:0.0} ms"
            : "-- ms";
        public bool IsIpcConnected => workspace.IsConnected;
        public string WorkOrderContext => Session.Context;
        public string TestingPermission => Session.TestPermission;
        public string ReportState => Session.Report;
        public string EvidenceSummary => retainedEvidence.Count + " retained";
        public bool HasPinnedTools => pinned.Count > 0;
        public bool HasRecentTools => recent.Count > 0;

        public ModderRolePreset ActiveModderRole
        {
            get => activeModderRole;
            set
            {
                if (Set(ref activeModderRole, value))
                {
                    Raise(nameof(ActiveModderRoleLabel));
                    Raise(nameof(ActiveModderRoleHint));
                    RefreshVisibleTools();
                    var statusTemplate = localization.GetText(
                        "Ui.Role.FilteredStatus",
                        "Workspace filtered for {0} ({1} tools available)");
                    Status = string.Format(statusTemplate, ActiveModderRoleLabel, VisibleTools.Count);
                }
            }
        }

        public string ActiveModderRoleLabel => activeModderRole switch
        {
            ModderRolePreset.NarrativeDialogues => localization.GetText("Ui.Role.NarrativeDialogues", "Narrative & Dialogues"),
            ModderRolePreset.TroopCombatArtisan => localization.GetText("Ui.Role.TroopCombatArtisan", "Troop & Combat Artisan"),
            ModderRolePreset.EconomyWorldArchitect => localization.GetText("Ui.Role.EconomyWorldArchitect", "Economy & World Architect"),
            ModderRolePreset.CoreDevPerformanceAuditor => localization.GetText("Ui.Role.CoreDevPerformanceAuditor", "Core Dev & Performance"),
            _ => localization.GetText("Ui.Role.All", "All Specializations")
        };

        public string ActiveModderRoleHint => activeModderRole switch
        {
            ModderRolePreset.NarrativeDialogues => localization.GetText("Ui.Role.Hint.NarrativeDialogues", "Tools for quests, hero dialogue, agent memory, and encyclopedia."),
            ModderRolePreset.TroopCombatArtisan => localization.GetText("Ui.Role.Hint.TroopCombatArtisan", "Tools for troop trees, combat, equipment, and audio."),
            ModderRolePreset.EconomyWorldArchitect => localization.GetText("Ui.Role.Hint.EconomyWorldArchitect", "Tools for towns, workshops, underworld systems, and diplomacy."),
            ModderRolePreset.CoreDevPerformanceAuditor => localization.GetText("Ui.Role.Hint.CoreDevPerformanceAuditor", "Tools for rules, save safety, memory profiling, and assemblies."),
            _ => localization.GetText("Ui.Role.Hint.All", "Click to cycle through roles and filter tools for that workflow.")
        };

        public RelayCommand CycleModderRoleCommand { get; }

        void CycleModderRole()
        {
            ActiveModderRole = (ModderRolePreset)(((int)activeModderRole + 1) % 5);
        }

        void ApplyTheme(object value) { ThemeId = value?.ToString(); }

        static string LocalizedThemeMessage(ThemeApplyResult result)
        {
            var localized = result?.MessageKey == null ? null : Application.Current?.TryFindResource(result.MessageKey) as string;
            return string.IsNullOrWhiteSpace(localized) ? (result?.Message ?? string.Empty) : localized;
        }

        void PersistPreferences()
        {
            if (!preferences.Save(new DesktopPreferenceState { ThemeId = ThemeId, LanguageCode = LanguageCode }, out var error))
            {
                ThemeMessage = error;
                Status = error;
            }
        }

        void RefreshVisibleTools()
        {
            using var filterMeasurement = metrics.Start(
                "tool-filter-refresh",
                "Synchronous view-model filtering and collection updates only; excludes the following WPF layout/render.");
            var query = filter.Trim();
            var tools = Catalog.Tools;
            var matches = new List<ToolDefinition>(tools.Count);
            var isCategoryEmpty = string.IsNullOrWhiteSpace(selectedCategory);
            for (int i = 0; i < tools.Count; i++)
            {
                var item = tools[i];
                if ((!showFavoritesOnly || pinned.Contains(item)) &&
                    (isCategoryEmpty || item.Category == selectedCategory) &&
                    MatchesSearch(item, query) &&
                    MatchesModderRole(item, activeModderRole))
                {
                    matches.Add(item);
                }
            }

            // Emit one collection Reset rather than a Clear plus one Add per tool.
            // Unchanged matches keep their existing collection items and avoid a refresh.
            visibleTools.ReplaceAll(matches);
            Raise(nameof(FilteredToolsCountText));
            var existingGroups = new Dictionary<string, ToolGroupViewModel>(visibleGroups.Count, StringComparer.Ordinal);
            for (int i = 0; i < visibleGroups.Count; i++)
            {
                var group = visibleGroups[i];
                existingGroups[group.Name] = group;
            }

            var groupedMatches = new Dictionary<string, List<ToolDefinition>>(16, StringComparer.Ordinal);
            for (int i = 0; i < matches.Count; i++)
            {
                var item = matches[i];
                if (!groupedMatches.TryGetValue(item.Group, out var groupList))
                {
                    groupList = new List<ToolDefinition>(16);
                    groupedMatches[item.Group] = groupList;
                }
                groupList.Add(item);
            }

            var sortedGroupKeys = new List<string>(groupedMatches.Count);
            foreach (var key in groupedMatches.Keys) sortedGroupKeys.Add(key);
            sortedGroupKeys.Sort((a, b) =>
            {
                int rankCompare = ToolCatalog.GroupRank(a).CompareTo(ToolCatalog.GroupRank(b));
                return rankCompare != 0 ? rankCompare : string.Compare(a, b, StringComparison.Ordinal);
            });

            var groups = new List<ToolGroupViewModel>(sortedGroupKeys.Count);
            for (int i = 0; i < sortedGroupKeys.Count; i++)
            {
                var groupKey = sortedGroupKeys[i];
                var groupItems = groupedMatches[groupKey];
                if (existingGroups.TryGetValue(groupKey, out var view))
                {
                    // Keep the bound group/container alive during edits inside the same
                    // category, replacing only that group's child items when they changed.
                    view.ReplaceTools(groupItems);
                }
                else
                {
                    view = new ToolGroupViewModel(groupKey, groupItems);
                    if (groupExpansion.TryGetValue(groupKey, out var expanded)) view.IsExpanded = expanded;
                    groupExpansion[groupKey] = view.IsExpanded;
                }
                groups.Add(view);
            }
            // ReplaceAll detects identical group references/order and skips Reset, so a
            // filter edit that stays within the same groups does not rebuild the outer rail.
            visibleGroups.ReplaceAll(groups);
            SynchronizeGroupExpansionSubscriptions(groups);
            RefreshOperationalRailEntries();
        }

        void SynchronizeGroupExpansionSubscriptions(IEnumerable<ToolGroupViewModel> groups)
        {
            var current = new HashSet<ToolGroupViewModel>(groups);
            var toRemove = new List<ToolGroupViewModel>();
            foreach (var group in observedGroups)
            {
                if (!current.Contains(group)) toRemove.Add(group);
            }
            for (int i = 0; i < toRemove.Count; i++)
            {
                var group = toRemove[i];
                group.PropertyChanged -= OnToolGroupPropertyChanged;
                observedGroups.Remove(group);
            }
            foreach (var group in current)
            {
                if (!observedGroups.Add(group)) continue;
                group.PropertyChanged += OnToolGroupPropertyChanged;
            }
        }

        void OnToolGroupPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName != nameof(ToolGroupViewModel.IsExpanded) || !(sender is ToolGroupViewModel group)) return;
            groupExpansion[group.Name] = group.IsExpanded;
            RefreshOperationalRailEntries();
        }

        void RefreshOperationalRailEntries()
        {
            int estimatedCount = visibleGroups.Count + visibleTools.Count + 1;
            var entries = new List<OperationalRailEntryViewModel>(estimatedCount);
            for (int g = 0; g < visibleGroups.Count; g++)
            {
                var group = visibleGroups[g];
                if (!groupRailEntries.TryGetValue(group, out var groupEntry))
                {
                    groupEntry = new OperationalRailGroupEntryViewModel(group);
                    groupRailEntries.Add(group, groupEntry);
                }
                entries.Add(groupEntry);
                if (!group.IsExpanded) continue;
                var tools = group.Tools;
                for (int t = 0; t < tools.Count; t++)
                {
                    var tool = tools[t];
                    if (!toolRailEntries.TryGetValue(tool, out var toolEntry))
                    {
                        toolEntry = new OperationalRailToolEntryViewModel(tool);
                        toolRailEntries.Add(tool, toolEntry);
                    }
                    toolEntry.IsSelected = !isHookWorkbenchOpen && ReferenceEquals(tool, selectedTool);
                    entries.Add(toolEntry);
                }
            }
            entries.Add(OperationalRailFooterEntryViewModel.Instance);
            operationalRailEntries.ReplaceAll(entries);
        }

        bool MatchesSearch(ToolDefinition item, string query) =>
            string.IsNullOrEmpty(query) ||
            (toolSearchText.TryGetValue(item, out var searchable) && searchable.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);

        static bool MatchesModderRole(ToolDefinition tool, ModderRolePreset role)
        {
            if (role == ModderRolePreset.All) return true;
            switch (role)
            {
                case ModderRolePreset.NarrativeDialogues:
                    return tool.Group == "Learning" || tool.Group == "Campaign" || tool.Studio == DesktopStudioKind.AgentMemory ||
                           tool.Id.IndexOf("Quest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Dialogue", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Memory", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Localization", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Encyclopedia", StringComparison.OrdinalIgnoreCase) >= 0;

                case ModderRolePreset.TroopCombatArtisan:
                    return tool.Group == "Combat" || tool.Studio == DesktopStudioKind.TroopTree || tool.Studio == DesktopStudioKind.AudioMixer ||
                           tool.Group == "Assets" ||
                           tool.Id.IndexOf("Troop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Tactics", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Siege", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Armor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Sound", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Audio", StringComparison.OrdinalIgnoreCase) >= 0;

                case ModderRolePreset.EconomyWorldArchitect:
                    return tool.Group == "Economy" || tool.Group == "Politics" || tool.Studio == DesktopStudioKind.Workshop || tool.Studio == DesktopStudioKind.KingdomDiplomacy ||
                           tool.Id.IndexOf("Trade", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Settlement", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Workshop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Crime", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Party", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Diplomacy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Caravan", StringComparison.OrdinalIgnoreCase) >= 0;

                case ModderRolePreset.CoreDevPerformanceAuditor:
                    return tool.Group == "Diagnostics" || tool.Group == "Delivery" || tool.Group == "Forge SDK" || tool.Group == "Gauntlet" ||
                           tool.Studio == DesktopStudioKind.CodeSecurity || tool.Studio == DesktopStudioKind.ModuleHierarchy ||
                           tool.Studio == DesktopStudioKind.LiveSession || tool.Studio == DesktopStudioKind.DeliveryStudio ||
                           tool.Studio == DesktopStudioKind.DiagnosticsStudio ||
                           tool.Id.IndexOf("Audit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Save", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Assembly", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Harmony", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Weave", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           tool.Id.IndexOf("Memory", StringComparison.OrdinalIgnoreCase) >= 0;

                default:
                    return true;
            }
        }

        static ReadOnlyObservableCollection<ToolDefinition> CreateReadOnlySnapshot(IEnumerable<ToolDefinition> items) =>
            new(new ObservableCollection<ToolDefinition>(items));

        async Task ConnectAsync(CancellationToken cancellation)
        {
            Status = "Connecting"; Session.Connection = "Connecting";
            var connected = await workspace.ConnectAsync(cancellation).ConfigureAwait(true);
            Status = connected ? "Connected to Forge session" : (workspace.ConnectionError ?? "Connection was not established.");
            Session.Connection = ConnectionState;
            Raise(nameof(ConnectionState));
            Raise(nameof(IsIpcConnected));
            Raise(nameof(ConnectionLatencyText));
            HookWorkbench.NotifyConnectionChanged();
        }

        async Task PingConnectionAsync(CancellationToken cancellation)
        {
            if (!workspace.IsConnected)
            {
                Status = "Standalone Mode · No active Bannerlord IPC pipe connection.";
                Raise(nameof(ConnectionLatencyText));
                Raise(nameof(IsIpcConnected));
                return;
            }
            Status = "Pinging Bannerlord IPC transport...";
            var latency = await workspace.Session.PingAsync(cancellation).ConfigureAwait(true);
            Raise(nameof(ConnectionLatencyText));
            Raise(nameof(IsIpcConnected));
            Status = latency.HasValue
                ? $"IPC Round-Trip: {latency.Value:0.1} ms · Session Active"
                : "Ping timed out or endpoint unreachable.";
        }

        void SelectTool(object value) { if (value is ToolDefinition tool) { IsCommandPaletteOpen = false; SelectedTool = tool; } }

        void SelectFirstPaletteTool()
        {
            if (paletteTools.Count > 0)
            {
                IsCommandPaletteOpen = false;
                SelectedTool = paletteTools[0];
            }
        }

        IReadOnlyList<ToolDefinition> FilterPaletteTools(string query)
        {
            if (string.IsNullOrEmpty(query)) return Catalog.Tools;
            var tools = Catalog.Tools;
            var results = new List<ToolDefinition>(tools.Count);
            if (query.StartsWith('>') || query.StartsWith(':'))
            {
                var prefixQuery = query.Substring(1).TrimStart();
                var spaceIndex = prefixQuery.IndexOf(' ');
                var prefix = spaceIndex > 0 ? prefixQuery.Substring(0, spaceIndex) : prefixQuery;
                var rest = spaceIndex > 0 ? prefixQuery.Substring(spaceIndex + 1).Trim() : string.Empty;

                Func<ToolDefinition, bool> matchesPrefix;
                if (prefix.StartsWith("live", StringComparison.OrdinalIgnoreCase))
                    matchesPrefix = static item => item.Kind == DesktopToolKind.Live || item.Group.StartsWith("Live", StringComparison.OrdinalIgnoreCase);
                else if (prefix.StartsWith("diag", StringComparison.OrdinalIgnoreCase))
                    matchesPrefix = static item => item.Group.StartsWith("Diagnostics", StringComparison.OrdinalIgnoreCase);
                else if (prefix.StartsWith("asset", StringComparison.OrdinalIgnoreCase))
                    matchesPrefix = static item => item.Group.StartsWith("Assets", StringComparison.OrdinalIgnoreCase);
                else if (prefix.StartsWith("sim", StringComparison.OrdinalIgnoreCase))
                    matchesPrefix = static item => item.Kind == DesktopToolKind.Simulation || item.Group.StartsWith("Simulation", StringComparison.OrdinalIgnoreCase);
                else if (prefix.StartsWith("pol", StringComparison.OrdinalIgnoreCase))
                    matchesPrefix = static item => item.Group.StartsWith("Politics", StringComparison.OrdinalIgnoreCase);
                else if (prefix.StartsWith("camp", StringComparison.OrdinalIgnoreCase))
                    matchesPrefix = static item => item.Group.StartsWith("Campaign", StringComparison.OrdinalIgnoreCase);
                else if (prefix.StartsWith("econ", StringComparison.OrdinalIgnoreCase))
                    matchesPrefix = static item => item.Group.StartsWith("Economy", StringComparison.OrdinalIgnoreCase);
                else if (prefix.StartsWith("comb", StringComparison.OrdinalIgnoreCase))
                    matchesPrefix = static item => item.Group.StartsWith("Combat", StringComparison.OrdinalIgnoreCase);
                else if (prefix.StartsWith("gaunt", StringComparison.OrdinalIgnoreCase))
                    matchesPrefix = static item => item.Group.StartsWith("Gauntlet", StringComparison.OrdinalIgnoreCase);
                else if (prefix.StartsWith("deliv", StringComparison.OrdinalIgnoreCase))
                    matchesPrefix = static item => item.Group.StartsWith("Delivery", StringComparison.OrdinalIgnoreCase);
                else if (prefix.StartsWith("gen", StringComparison.OrdinalIgnoreCase))
                    matchesPrefix = static item => item.Kind == DesktopToolKind.Generator;
                else if (prefix.StartsWith("rep", StringComparison.OrdinalIgnoreCase))
                    matchesPrefix = static item => item.Kind == DesktopToolKind.Report;
                else if (prefix.StartsWith("asm", StringComparison.OrdinalIgnoreCase))
                    matchesPrefix = static item => item.Kind == DesktopToolKind.AssemblyEditor || item.Id.Contains("Assembly");
                else
                    matchesPrefix = item => item.Group.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0 || item.KindLabel.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0;

                for (var i = 0; i < tools.Count; i++)
                {
                    var item = tools[i];
                    if (matchesPrefix(item) && MatchesSearch(item, rest))
                    {
                        results.Add(item);
                    }
                }
                return results;
            }

            for (var i = 0; i < tools.Count; i++)
            {
                var item = tools[i];
                if (MatchesSearch(item, query))
                {
                    results.Add(item);
                }
            }
            return results;
        }

        void ReleaseCurrentPage()
        {
            (CurrentPage as IWorkspacePage)?.Dispose();
            CurrentPage = null;
            CurrentWorkbenchPage = null;
            RunPrimaryCommand.NotifyCanExecuteChanged();
            PinCurrentToolToSplitDeckCommand.NotifyCanExecuteChanged();
            ExportMarkdownReportCommand.NotifyCanExecuteChanged();
        }

        void SelectPinned(object value) { if (value is ToolDefinition tool) { IsCommandPaletteOpen = false; SelectedTool = tool; } }
        void SelectPinnedSlot(object value)
        {
            if (!int.TryParse(value?.ToString(), out var index) || index < 0 || index >= pinned.Count) return;
            IsCommandPaletteOpen = false;
            SelectedTool = pinned[index];
        }
        bool IsPinnedSlot(object value) => int.TryParse(value?.ToString(), out var index) && index >= 0 && index < pinned.Count;
        void ToggleFavorite(object value)
        {
            if (!(value is ToolDefinition tool)) return;
            var changed = false;
            if (pinned.Contains(tool)) changed = pinned.Remove(tool);
            else if (pinned.Count < 9) { pinned.Add(tool); changed = true; }
            if (changed)
            {
                pinnedToolsSnapshot = null;
                Raise(nameof(PinnedTools));
                Raise(nameof(HasPinnedTools));
                if (showFavoritesOnly) RefreshVisibleTools();
            }
            Status = pinned.Contains(tool) ? "Pinned " + tool.Title : "Unpinned " + tool.Title;
        }

        void Select(ToolDefinition tool)
        {
            using (metrics.Start("tool-selection", "Short desktop navigation only; not a game-performance attribution."))
            {
                (CurrentPage as IWorkspacePage)?.Dispose();
                var page = DeclarativeUiCatalog.Create("tool-workbench", () => new WorkbenchPageViewModel(
                    tool, ExecuteAsync, pickInput: pickInput, exportAsync: ExportPageAsync,
                    clipboardWriter: clipboardWriter, localize: key => key switch
                    {
                        "Ui.CopyCommandAccessibleNameFormat" => localization.GetText(key, "Copy console command: {0}"),
                        "Ui.ClipboardCopySucceeded" => localization.GetText(key, "Command copied to the clipboard."),
                        _ => localization.GetText(key, "Could not copy to the clipboard.")
                    }));
                CurrentPage = page;
                CurrentWorkbenchPage = page;
            }
            var previousIndex = recent.IndexOf(tool);
            if (previousIndex != 0)
            {
                if (previousIndex > 0) recent.RemoveAt(previousIndex);
                recent.Insert(0, tool);
                while (recent.Count > 12) recent.RemoveAt(recent.Count - 1);
                recentToolsSnapshot = null;
                Raise(nameof(RecentTools));
                Raise(nameof(HasRecentTools));
            }
            RunPrimaryCommand.NotifyCanExecuteChanged();
            PinCurrentToolToSplitDeckCommand.NotifyCanExecuteChanged();
            ExportMarkdownReportCommand.NotifyCanExecuteChanged();
            Session.Report = "Awaiting result";
            Status = "Selected " + tool.Title;
        }

        async Task<WorkspaceExecutionResult> ExecuteAsync(ToolDefinition tool, string input, CancellationToken cancellation)
        {
            var result = await workspace.ExecuteAsync(tool, input, cancellation).ConfigureAwait(true);
            retainedEvidence.Clear(); retainedEvidence.AddRange(result.Evidence ?? []);
            Raise(nameof(RetainedEvidence)); Raise(nameof(EvidenceCount)); Raise(nameof(EvidenceSummary));
            CurrentWorkbenchPage?.EvidenceLedger.Refresh();
            ExportEvidenceCommand.NotifyCanExecuteChanged();
            ExportMarkdownReportCommand.NotifyCanExecuteChanged();
            Status = result.Status; Session.Report = result.Status;
            return result;
        }

        async Task ExportPageAsync(ToolPageViewModel page, CancellationToken cancellationToken)
        {
            if (page == null) return;
            retainedEvidence.Clear(); retainedEvidence.AddRange(page.Evidence);
            Raise(nameof(EvidenceCount)); Raise(nameof(EvidenceSummary)); ExportEvidenceCommand.NotifyCanExecuteChanged();
            var result = await reportExport.ExportTextAsync("desktop-work-order", page.RawResult, cancellationToken).ConfigureAwait(true);
            ApplyExportResult(result);
        }

        async Task ExportCurrentPageMarkdownAsync(CancellationToken cancellationToken)
        {
            if (CurrentPage == null) return;
            var title = CurrentPage.Tool.Title + " - Forensic Telemetry";
            var mdBody = "### Status: " + CurrentPage.Status + "\n\n```text\n" + CurrentPage.RawResult + "\n```";
            var result = await reportExport.ExportMarkdownAsync("desktop-forensics", title, mdBody, cancellationToken).ConfigureAwait(true);
            ApplyExportResult(result);
        }

        async Task ExportRetainedEvidenceAsync(CancellationToken cancellationToken)
        {
            var snapshot = retainedEvidence.Select(item => item.Source + " | " + item.Status + " | " + item.Detail).ToArray();
            var result = await reportExport.ExportLinesAsync("desktop-evidence", snapshot, cancellationToken).ConfigureAwait(true);
            ApplyExportResult(result);
        }

        void ApplyExportResult(ReportExportResult result)
        {
            if (result?.Status == ReportExportStatus.Succeeded)
            {
                var template = localization.GetText("Ui.ReportExported", "Report exported: {0}");
                Status = string.Format(System.Globalization.CultureInfo.CurrentCulture, template, result.Path);
                Session.Report = localization.GetText("Ui.ReportExportedShort", "Exported");
            }
            else if (result?.Status == ReportExportStatus.Cancelled)
            {
                Status = localization.GetText("Ui.ReportExportCancelled", "Report export cancelled.");
                Session.Report = Status;
            }
            else
            {
                Status = localization.GetText("Ui.ReportExportFailed", "Could not export the report. Check the reports folder and try again.");
                Session.Report = Status;
            }
        }

        void ToggleSplitDeck()
        {
            IsSplitDeckActive = !IsSplitDeckActive;
            if (IsSplitDeckActive && PinnedTool == null && CurrentPage != null)
            {
                PinCurrentToolToSplitDeck();
            }
            Status = IsSplitDeckActive ? "Split Deck active (Ctrl+D)" : "Split Deck closed";
        }

        void PinCurrentToolToSplitDeck()
        {
            if (CurrentPage == null) return;
            PinnedTool = CurrentPage.Tool;
            PinnedExecutionStatus = CurrentPage.Status ?? "Pinned";
            PinnedRawResult = !string.IsNullOrWhiteSpace(CurrentPage.RawResult) ? CurrentPage.RawResult : "(No raw output produced yet. Execute the tool to generate output.)";
            pinnedEvidence.Clear();
            foreach (var ev in CurrentPage.Evidence) pinnedEvidence.Add(ev);
            IsSplitDeckActive = true;
            Status = "Pinned " + CurrentPage.Tool.Title + " to Split Deck";
        }

        void ClearPinnedDeck()
        {
            PinnedTool = null;
            PinnedExecutionStatus = "Ready";
            PinnedRawResult = string.Empty;
            pinnedEvidence.Clear();
            Raise(nameof(PinnedToolTitle));
            Raise(nameof(PinnedToolCategory));
            Status = "Split Deck cleared";
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            ExportEvidenceCommand.Cancel();
            HookWorkbench.Dispose();
            (CurrentPage as IWorkspacePage)?.Dispose();
            workspace.Dispose(); retainedEvidence.Clear(); pinned.Clear(); recent.Clear(); pinnedEvidence.Clear();
        }
    }

    /// <summary>
    /// Observable collection optimized for replacing a filtered view. The backing items are
    /// updated without per-item notifications, then one Reset is raised for WPF bindings.
    /// </summary>
    internal sealed class BatchObservableCollection<T> : ObservableCollection<T>
    {
        public BatchObservableCollection() { }
        public BatchObservableCollection(IEnumerable<T> items) : base(items) { }

        public bool ReplaceAll(IReadOnlyList<T> items)
        {
            items ??= [];
            int count = items.Count;
            if (Count == count)
            {
                var unchanged = true;
                for (var index = 0; index < count; index++)
                {
                    if (!EqualityComparer<T>.Default.Equals(this[index], items[index]))
                    {
                        unchanged = false;
                        break;
                    }
                }
                if (unchanged) return false;
            }

            Items.Clear();
            for (var index = 0; index < count; index++)
            {
                Items.Add(items[index]);
            }
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            return true;
        }

        public bool ReplaceAll(IEnumerable<T> items)
        {
            if (items is IReadOnlyList<T> list) return ReplaceAll(list);
            var replacement = items != null ? items.ToArray() : [];
            return ReplaceAll((IReadOnlyList<T>)replacement);
        }
    }
}
