using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CalradiaForge.Desktop.Presentation
{
    /// <summary>Explicit navigation group. It is data, so the rail never has to inspect a WPF tree.</summary>
    internal sealed class ToolGroupViewModel : ObservableObject
    {
        bool expanded = true;
        readonly BatchObservableCollection<ToolDefinition> tools;
        public ToolGroupViewModel(string name, IEnumerable<ToolDefinition> tools)
        {
            Name = name ?? string.Empty;
            this.tools = new BatchObservableCollection<ToolDefinition>(tools ?? []);
            GroupIconKey = IconForGroup(Name);
            ToggleCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
        }
        public string Name { get; }
        public ObservableCollection<ToolDefinition> Tools => tools;
        public int Count => tools.Count;
        internal void ReplaceTools(IEnumerable<ToolDefinition> items)
        {
            if (tools.ReplaceAll(items)) Raise(nameof(Count));
        }
        public string GroupIconKey { get; }
        public static string IconForGroup(string name) => name switch
        {
            "Diagnostics" => "GameIcon.archery_target",
            "Live session" => "GameIcon.compass",
            "Assets" => "GameIcon.gears",
            "Combat" => "GameIcon.crossed_swords",
            "Politics" => "GameIcon.knight_banner",
            "Campaign" => "GameIcon.compass",
            "Simulation" => "GameIcon.archery_target",
            "Delivery" => "GameIcon.knight_banner",
            "Learning" => "GameIcon.scroll_unfurled",
            "Gauntlet" => "GameIcon.gear_hammer",
            "Economy" => "GameIcon.gears",
            "Forge SDK" => "GameIcon.gear_hammer",
            _ => "Icon.Map"
        };
        public RelayCommand ToggleCommand { get; }
        public bool IsExpanded { get => expanded; set { if (Set(ref expanded, value)) Raise(nameof(ExpansionIconKey)); } }
        public string ExpansionIconKey => IsExpanded ? "Icon.ChevronDown" : "Icon.ChevronRight";
    }

    /// <summary>Base row for the flattened, virtualized operational rail.</summary>
    internal abstract class OperationalRailEntryViewModel : ObservableObject
    {
    }

    /// <summary>A collapsible group heading in the operational rail.</summary>
    internal sealed class OperationalRailGroupEntryViewModel(ToolGroupViewModel group) : OperationalRailEntryViewModel
    {
        public ToolGroupViewModel Group { get; } = group ?? throw new ArgumentNullException(nameof(group));
    }

    /// <summary>A single visible tool route in the operational rail.</summary>
    internal sealed class OperationalRailToolEntryViewModel(ToolDefinition tool) : OperationalRailEntryViewModel
    {
        bool isSelected;
        public ToolDefinition Tool { get; } = tool ?? throw new ArgumentNullException(nameof(tool));
        public bool IsSelected { get => isSelected; set => Set(ref isSelected, value); }
    }

    /// <summary>Trailing rail content; pinned and recent retain their own bounded viewports.</summary>
    internal sealed class OperationalRailFooterEntryViewModel : OperationalRailEntryViewModel
    {
        internal static readonly OperationalRailFooterEntryViewModel Instance = new();
        OperationalRailFooterEntryViewModel() { }
    }

    /// <summary>Command deck metadata kept separate from the page and controls.</summary>
    internal sealed class CommandDeckViewModel : ObservableObject
    {
        string executionState = "Ready";
        string disabledReason;
        public CommandDeckViewModel(ToolPageViewModel page)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
            PrimaryCommand = page.RunCommand;
            CancelCommand = page.CancelCommand;
            ExportCommand = page.ExportCommand;
        }
        public ToolPageViewModel Page { get; }
        public ICommand PrimaryCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ExportCommand { get; }
        public string PrimaryLabel => Page.Tool.ChangesState ? "Guarded action" : "Run work order";
        public string ExecutionState { get => executionState; set => Set(ref executionState, value); }
        public string DisabledReason { get => disabledReason ?? Page.DisabledReason; set => Set(ref disabledReason, value); }
    }

    /// <summary>Evidence presentation state; the bounded source remains owned by the page.</summary>
    [ForgeUiPage("evidence-ledger", "Evidence", releaseOnNavigate: false)]
    internal sealed class EvidenceLedgerViewModel(ObservableCollection<WorkspaceEvidence> evidence) : ObservableObject
    {
        bool expanded;
        public ObservableCollection<WorkspaceEvidence> Evidence { get; } = evidence ?? throw new ArgumentNullException(nameof(evidence));
        public int Count => Evidence.Count;
        public bool IsExpanded { get => expanded; set { if (Set(ref expanded, value)) Raise(nameof(ViewLabel)); } }
        public string ViewLabel => IsExpanded ? "INSPECTION VIEW" : "WORK VIEW";
        public void Refresh() => Raise(nameof(Count));
    }

    /// <summary>Connection and report state shown in the order strip and footer.</summary>
    [ForgeUiPage("session-status", "Status", releaseOnNavigate: false)]
    internal sealed class SessionStatusViewModel : ObservableObject
    {
        string connection = "Disconnected";
        string report = "No report selected";
        public string Connection { get => connection; set => Set(ref connection, value); }
        public string Report { get => report; set => Set(ref report, value); }
        public string TestPermission { get; set; } = "Read-only route";
        public string Context { get; set; } = "Desktop / local workspace";
        public string Evidence { get; set; } = "0 retained";
    }

    /// <summary>Routed page implementation. It keeps the legacy RawResult binding surface while exposing the new MVVM workbench services.</summary>
    internal sealed class WorkbenchPageViewModel : ToolPageViewModel
    {
        public WorkbenchPageViewModel(ToolDefinition tool,
            Func<ToolDefinition, string, System.Threading.CancellationToken, System.Threading.Tasks.Task<WorkspaceExecutionResult>> execute,
            Action<ToolPageViewModel> export = null,
            Func<ToolDefinition, bool, string> pickInput = null,
            Func<ToolPageViewModel, System.Threading.CancellationToken, System.Threading.Tasks.Task> exportAsync = null,
            Func<string, bool> clipboardWriter = null,
            Func<string, string> localize = null) : base(tool, execute, export, pickInput, exportAsync, clipboardWriter, localize)
        {
            CommandDeck = new(this);
            EvidenceLedger = new(Evidence);
        }
        public CommandDeckViewModel CommandDeck { get; }
        public EvidenceLedgerViewModel EvidenceLedger { get; }
        public new string RawResult { get => base.RawResult; set => base.RawResult = value; }
        public override void Dispose()
        {
            EvidenceLedger.Refresh();
            base.Dispose();
        }
    }
}
