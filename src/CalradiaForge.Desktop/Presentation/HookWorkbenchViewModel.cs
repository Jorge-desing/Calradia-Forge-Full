using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CalradiaForge.Core;
using CalradiaForge.Desktop.Services;

namespace CalradiaForge.Desktop.Presentation
{
    /// <summary>Desktop projection for the host-registered hook inventory and its two-phase operations.</summary>
    [ForgeUiPage("hook-workbench", "Workbench", releaseOnNavigate: false)]
    internal sealed class HookWorkbenchViewModel : ObservableObject, IDisposable
    {
        static readonly string[] RequiredCapabilities =
        [
            "hook-snapshots", "hook-apply-plan", "hook-apply-confirm", "hook-revert-plan", "hook-revert-confirm", "hook-plan-cancel"
        ];

        readonly DesktopSessionService session;
        readonly Func<string, string, string> localize;
        readonly ObservableCollection<HookWorkbenchRow> hooks = [];
        readonly ObservableCollection<HookWorkbenchRow> plannedHooks = [];
        readonly ObservableCollection<HookWorkbenchRow> filteredHooks = [];
        readonly DesktopReportExportService reportExports = new();
        string ownerFilter = string.Empty;
        string targetFilter = string.Empty;
        string typeFilter = string.Empty;
        string lastResult = string.Empty;
        DateTimeOffset? snapshotsCapturedAtUtc;
        readonly DispatcherTimer expiryTimer;
        HookPlan pendingPlan;
        string sessionId = string.Empty;
        string blockedReason = string.Empty;
        string status = string.Empty;
        bool isEligible;
        bool isBusy;
        bool isConfirmationChecked;
        bool requiresSnapshotRefresh;
        bool snapshotReadSucceeded;
        bool disposed;

        public HookWorkbenchViewModel(DesktopSessionService session, Func<string, string, string> localize = null)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.localize = localize ?? ((_, fallback) => fallback);
            expiryTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
            expiryTimer.Tick += OnExpiryTick;
            RefreshSnapshotsCommand = new(RefreshSnapshotsAsync, CanRefreshSnapshots);
            PlanApplyCommand = new(token => CreatePlanAsync("apply", token), () => CanCreatePlan("apply"));
            PlanRevertCommand = new(token => CreatePlanAsync("revert", token), () => CanCreatePlan("revert"));
            ConfirmPlanCommand = new(ConfirmPlanAsync, CanConfirmPlan);
            CancelPlanCommand = new AsyncRelayCommand(CancelPendingPlanAsync, () => HasPendingPlan && !IsBusy);
            VerifySelectedCommand = new(VerifySelectedAsync, CanVerifySelected);
            ExportSnapshotsCommand = new(ExportSnapshotsAsync, () => !disposed && !IsBusy && snapshotReadSucceeded);
            ExportResultsCommand = new(ExportResultsAsync, () => !disposed && !IsBusy && lastResult.Length > 0);
        }

        public ObservableCollection<HookWorkbenchRow> Hooks => hooks;
        public ObservableCollection<HookWorkbenchRow> FilteredHooks => filteredHooks;
        public bool HasFilteredHooks => filteredHooks.Count > 0;
        public string EmptyInventoryMessage => HasHooks
            ? Text("Ui.HooksNoFilterMatches", "No registered hooks match the current filters.")
            : Text("Ui.HooksNoHooks", "No registered hooks are available in this session.");
        public string OwnerFilter { get => ownerFilter; set { if (Set(ref ownerFilter, value ?? string.Empty)) RefreshFilters(); } }
        public string TargetFilter { get => targetFilter; set { if (Set(ref targetFilter, value ?? string.Empty)) RefreshFilters(); } }
        public string TypeFilter { get => typeFilter; set { if (Set(ref typeFilter, value ?? string.Empty)) RefreshFilters(); } }
        public AsyncRelayCommand VerifySelectedCommand { get; }
        public AsyncRelayCommand ExportSnapshotsCommand { get; }
        public AsyncRelayCommand ExportResultsCommand { get; }
        public ObservableCollection<HookWorkbenchRow> PlannedHooks => plannedHooks;
        public bool HasHooks => hooks.Count > 0;
        public bool HasPendingHooks => plannedHooks.Count > 0;
        public AsyncRelayCommand RefreshSnapshotsCommand { get; }
        public AsyncRelayCommand PlanApplyCommand { get; }
        public AsyncRelayCommand PlanRevertCommand { get; }
        public AsyncRelayCommand ConfirmPlanCommand { get; }
        public AsyncRelayCommand CancelPlanCommand { get; }
        public string SessionId { get => sessionId; private set => Set(ref sessionId, value ?? string.Empty); }
        public bool IsEligible { get => isEligible; private set => Set(ref isEligible, value); }
        public string BlockedReason { get => blockedReason; private set => Set(ref blockedReason, value ?? string.Empty); }
        public string Status { get => status; private set => Set(ref status, value ?? string.Empty); }
        public bool IsBusy { get => isBusy; private set { if (Set(ref isBusy, value)) RefreshCommandStates(); } }
        public bool IsConfirmationChecked { get => isConfirmationChecked; set { if (Set(ref isConfirmationChecked, value)) ConfirmPlanCommand.NotifyCanExecuteChanged(); } }
        public bool HasPendingPlan => pendingPlan != null;
        public bool IsConnected => session.IsConnected;
        public bool SupportsHookProtocol => RequiredCapabilities.All(session.Supports);
        public int SelectedCount => hooks.Count(item => item.IsSelected);
        public int HiddenSelectedCount => hooks.Count(item => item.IsSelected && !filteredHooks.Contains(item));
        public string HooksSummary => string.Format(CultureInfo.CurrentCulture,
            Text("Ui.HooksCountFormat", "{0} registered hooks"), hooks.Count);
        public string PendingOperation => pendingPlan?.Operation ?? string.Empty;
        public bool IsApplyPlan => string.Equals(PendingOperation, "apply", StringComparison.OrdinalIgnoreCase);
        public string PlanWarning => IsApplyPlan
            ? Text("Ui.HooksApplyPartialWarning", "Batch Apply is sequential, not atomic. Earlier hooks can remain applied if a later hook fails.")
            : string.Empty;
        public string PendingExpiryText => pendingPlan == null ? string.Empty : pendingPlan.ExpiresAtUtc.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture);
        public string CapabilityMessage
        {
            get
            {
                if (!IsConnected) return Text("Ui.HooksNeedConnection", "Connect to a Forge session to inspect registered hooks.");
                if (!SupportsHookProtocol) return Text("Ui.HooksUnavailable", "The connected host does not advertise the hook workbench protocol.");
                return string.Empty;
            }
        }
        public string SelectionMessage
        {
            get
            {
                var selected = hooks.Where(item => item.IsSelected).ToArray();
                if (selected.Length == 0) return Text("Ui.HooksNoSelection", "Select registered hooks to plan an operation.");
                var summary = string.Format(CultureInfo.CurrentCulture,
                    Text("Ui.HooksSelectionCountFormat", "{0} selected; {1} hidden by filters. Operations include every selected hook."),
                    selected.Length, HiddenSelectedCount);
                if (selected.Select(item => item.State).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                    return summary + " " + Text("Ui.HooksMixedSelection", "Select hooks in one lifecycle state at a time.");
                return summary;
            }
        }

        public string PlanHeading => string.Equals(PendingOperation, "revert", StringComparison.OrdinalIgnoreCase)
            ? Text("Ui.HooksRevertPlan", "Revert plan")
            : Text("Ui.HooksApplyPlan", "Apply plan");

        public string EligibilityText => IsEligible
            ? Text("Ui.HooksEligible", "The host reports a supported main-menu context.")
            : !string.IsNullOrWhiteSpace(CapabilityMessage) ? CapabilityMessage
            : !string.IsNullOrWhiteSpace(BlockedReason) ? BlockedReason
            : Text("Ui.HooksBlocked", "Hook operations are unavailable in this game context.");

        public bool IsPlanExpired => pendingPlan != null && pendingPlan.ExpiresAtUtc <= DateTimeOffset.UtcNow;

        bool CanRefreshSnapshots() => CanRefreshSnapshots(allowWhileBusy: false);

        bool CanRefreshSnapshots(bool allowWhileBusy) => !disposed && (allowWhileBusy || !IsBusy) &&
            IsConnected && session.Supports("hook-snapshots");

        bool CanCreatePlan(string operation)
        {
            if (disposed || IsBusy || HasPendingPlan || requiresSnapshotRefresh || !IsConnected || !SupportsHookProtocol || !IsEligible) return false;
            var selected = hooks.Where(item => item.IsSelected).ToArray();
            return selected.Length > 0 && selected.Length <= 32 &&
                   selected.All(item => item.IsSelectable && (operation == "apply"
                       ? string.Equals(item.State, "Registered", StringComparison.OrdinalIgnoreCase) || string.Equals(item.State, "Reverted", StringComparison.OrdinalIgnoreCase)
                       : string.Equals(item.State, "Applied", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(item.State, "Conflict", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(item.State, "Failed", StringComparison.OrdinalIgnoreCase))) &&
                   selected.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() == selected.Length;
        }

        bool CanConfirmPlan() => !disposed && !IsBusy && IsConnected && pendingPlan != null &&
            pendingPlan.ExpiresAtUtc > DateTimeOffset.UtcNow && IsConfirmationChecked &&
            string.Equals(pendingPlan.Session, SessionId, StringComparison.Ordinal) &&
            pendingPlan.RequiresConfirmation && pendingPlan.Token.Length > 0;

        bool CanVerifySelected() => !disposed && !IsBusy && !HasPendingPlan && !requiresSnapshotRefresh &&
            IsConnected && IsEligible && session.Supports("hook-verify") && SelectedCount > 0 && SelectedCount <= 32;

        async Task VerifySelectedAsync(CancellationToken cancellation)
        {
            if (!CanVerifySelected()) return;
            var ids = hooks.Where(row => row.IsSelected).Select(row => row.Id).ToArray();
            IsBusy = true;
            try
            {
                var response = await session.SendAsync(new Request { Action = "hook-verify",
                    Argument = Json.Serialize(new HookIpcSelection { HookIds = ids.ToList() }) }, cancellation).ConfigureAwait(true);
                if (response?.Success != true) { Status = response?.Error ?? Text("Ui.HooksVerifyFailed", "Hook verification failed."); return; }
                if (!TryReadCommit(response.Data, "verify", SessionId, ids, out var commit, out var error)) { Status = error; return; }
                if (commit.TokenConsumed) { Status = Text("Ui.HooksVerifyFailed", "Hook verification failed."); return; }
                lastResult = response.Data;
                var outcome = FormatCommitStatus(commit, ids);
                Status = outcome;
                await RefreshSnapshotsAsync(cancellation, allowWhileBusy: true).ConfigureAwait(true);
                if (snapshotReadSucceeded) Status = outcome;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { Status = Text("Ui.HooksCancelStatus", "The operation was cancelled."); }
            catch (Exception error) { Status = error.Message; }
            finally { IsBusy = false; }
        }

        async Task ExportSnapshotsAsync(CancellationToken cancellation)
        {
            IsBusy = true;
            try { ShowExportResult(await reportExports.ExportJsonAsync("hook-snapshots", new { Session = SessionId,
                CapturedAtUtc = snapshotsCapturedAtUtc, Eligible = IsEligible, BlockedReason, Hooks = hooks.Select(row => new {
                    row.Id, row.Owner, row.Target, row.HasPrefix, row.HasPostfix, row.HasFinalizer, row.HasTranspiler,
                    row.Priority, row.Before, row.After, row.State, row.Detail }).ToArray() }, cancellation).ConfigureAwait(true)); }
            finally { IsBusy = false; }
        }

        async Task ExportResultsAsync(CancellationToken cancellation)
        {
            IsBusy = true;
            try
            {
                using var document = JsonDocument.Parse(lastResult);
                ShowExportResult(await reportExports.ExportJsonAsync("hook-results", document.RootElement.Clone(), cancellation).ConfigureAwait(true));
            }
            finally { IsBusy = false; }
        }

        void ShowExportResult(ReportExportResult result) => Status = result.Status switch {
            ReportExportStatus.Succeeded => Text("Ui.ReportExported", "Report exported:") + " " + result.Path,
            ReportExportStatus.Cancelled => Text("Ui.ReportExportCancelled", "Report export was cancelled."),
            _ => Text("Ui.ReportExportFailed", "Report export failed.") };

        void RefreshFilters()
        {
            filteredHooks.Clear();
            foreach (var row in hooks)
                if (row.Owner.Contains(OwnerFilter, StringComparison.OrdinalIgnoreCase) &&
                    row.Target.Contains(TargetFilter, StringComparison.OrdinalIgnoreCase) &&
                    row.HookKinds.Contains(TypeFilter, StringComparison.OrdinalIgnoreCase)) filteredHooks.Add(row);
            Raise(nameof(HasFilteredHooks));
            Raise(nameof(EmptyInventoryMessage));
            Raise(nameof(HiddenSelectedCount));
            Raise(nameof(SelectionMessage));
        }

        Task RefreshSnapshotsAsync(CancellationToken cancellation) => RefreshSnapshotsAsync(cancellation, allowWhileBusy: false);

        async Task RefreshSnapshotsAsync(CancellationToken cancellation, bool allowWhileBusy)
        {
            requiresSnapshotRefresh = true;
            snapshotReadSucceeded = false;
            RefreshCommandStates();
            if (!CanRefreshSnapshots(allowWhileBusy))
            {
                Status = CapabilityMessage;
                return;
            }

            // A refresh invalidates the local preview, so first ask the host to invalidate the
            // exact session/token pair. Keep it visible only if the response is uncertain.
            if (HasPendingPlan && !await CancelPendingPlanCoreAsync(cancellation).ConfigureAwait(true)) return;

            var ownsBusyState = !IsBusy;
            if (ownsBusyState) IsBusy = true;
            Status = Text("Ui.HooksLoading", "Reading registered hook snapshots…");
            try
            {
                var response = await session.SendAsync(new Request { Action = "hook-snapshots" }, cancellation).ConfigureAwait(true);
                if (response?.Success != true)
                {
                    ApplySnapshotFailure(response?.Error ?? Text("Ui.HooksRefreshFailed", "Hook snapshots could not be read."));
                    return;
                }

                if (!TryReadSnapshotEnvelope(response.Data, out var envelope, out var error))
                {
                    ApplySnapshotFailure(error);
                    return;
                }

                SessionId = envelope.Session;
                IsEligible = envelope.Eligible;
                BlockedReason = envelope.BlockedReason;
                ReplaceHooks(envelope.Hooks);
                requiresSnapshotRefresh = false;
                snapshotReadSucceeded = true;
                snapshotsCapturedAtUtc = DateTimeOffset.UtcNow;
                Status = IsEligible
                    ? Text("Ui.HooksLoaded", "Hook inventory refreshed.")
                    : (string.IsNullOrWhiteSpace(BlockedReason) ? Text("Ui.HooksBlocked", "Hook operations are unavailable in this game context.") : BlockedReason);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                requiresSnapshotRefresh = true;
                snapshotReadSucceeded = false;
                Status = Text("Ui.HooksCancelStatus", "The operation was cancelled.");
            }
            catch (Exception error)
            {
                ApplySnapshotFailure(error.Message);
            }
            finally
            {
                if (ownsBusyState) IsBusy = false;
            }
        }

        async Task CreatePlanAsync(string operation, CancellationToken cancellation)
        {
            if (!CanCreatePlan(operation)) return;
            var selected = hooks.Where(item => item.IsSelected).ToArray();
            var selectedIds = selected.Select(item => item.Id).ToArray();
            var action = operation == "apply" ? "hook-apply-plan" : "hook-revert-plan";
            IsBusy = true;
            Status = Text("Ui.HooksPlanning", "Preparing the hook plan…");
            try
            {
                var argument = Json.Serialize(new HookIpcSelection { HookIds = selectedIds.ToList() });
                var response = await session.SendAsync(new Request { Action = action, Argument = argument }, cancellation).ConfigureAwait(true);
                if (response?.Success != true)
                {
                    Status = response?.Error ?? Text("Ui.HooksPlanFailed", "The host did not create a hook plan.");
                    return;
                }

                if (!TryReadPlan(response.Data, operation, SessionId, selectedIds, hooks, out var plan, out var planned, out var error))
                {
                    Status = error;
                    return;
                }

                pendingPlan = plan;
                plannedHooks.Clear();
                foreach (var item in planned) plannedHooks.Add(item);
                IsConfirmationChecked = false;
                Raise(nameof(HasPendingPlan));
                Raise(nameof(PendingOperation));
                Raise(nameof(IsApplyPlan));
                Raise(nameof(PlanWarning));
                Raise(nameof(PendingExpiryText));
                Raise(nameof(PlanHeading));
                Raise(nameof(IsPlanExpired));
                Raise(nameof(HasPendingHooks));
                expiryTimer.Start();
                CancelPlanCommand.NotifyCanExecuteChanged();
                Status = Text("Ui.HooksReviewConfirm", "Review the selected hooks and confirm before continuing.");
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                Status = Text("Ui.HooksCancelStatus", "Plan creation was cancelled; no operation was confirmed.");
            }
            catch (Exception error)
            {
                Status = error.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        async Task ConfirmPlanAsync(CancellationToken cancellation)
        {
            if (!CanConfirmPlan()) return;
            var plan = pendingPlan;
            var action = plan.Operation == "apply" ? "hook-apply-confirm" : "hook-revert-confirm";

            // A confirmation token is single-use. Remove it before the only send attempt;
            // cancellation or a transport error is reported as unknown and never retried.
            ClearPendingPlan();
            requiresSnapshotRefresh = true;
            RefreshCommandStates();
            IsBusy = true;
            Status = Text("Ui.HooksSending", "Sending the confirmed operation…");
            try
            {
                var response = await session.SendAsync(new Request
                {
                    Action = action,
                    Argument = Json.Serialize(new HookIpcConfirmation { Token = plan.Token })
                }, cancellation).ConfigureAwait(true);
                if (response?.Success != true)
                {
                    Status = Text("Ui.HooksUnknownOutcome", "The result is uncertain. Refresh snapshots before planning another operation.") + " " + (response?.Error ?? string.Empty);
                    return;
                }

                if (!TryReadCommit(response.Data, plan.Operation, plan.Session, plan.HookIds, out var commit, out var error))
                {
                    Status = Text("Ui.HooksUnknownOutcome", "The result is uncertain. Refresh snapshots before planning another operation.") + " " + error;
                    return;
                }

                if (!commit.TokenConsumed)
                {
                    Status = Text("Ui.HooksUnknownOutcome", "The result is uncertain. Refresh snapshots before planning another operation.");
                    return;
                }

                var outcome = FormatCommitStatus(commit, plan.HookIds);
                lastResult = response.Data;
                if (commit.RequiresReconciliation)
                    Status = Text("Ui.HooksReconciling", "The host reported an incomplete or uncertain result. Reading fresh hook snapshots before another plan.");
                await RefreshSnapshotsAsync(cancellation, allowWhileBusy: true).ConfigureAwait(true);
                if (snapshotReadSucceeded)
                    Status = outcome;
                else
                    Status = Text("Ui.HooksUnknownOutcome", "The result is uncertain. Refresh snapshots before planning another operation.") + Environment.NewLine + Status;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                Status = Text("Ui.HooksUnknownOutcome", "Confirmation was interrupted. Its outcome is unknown; refresh snapshots manually.");
            }
            catch (Exception error)
            {
                Status = Text("Ui.HooksUnknownOutcome", "The result is uncertain. Refresh snapshots before planning another operation.") + " " + error.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        void ApplySnapshotFailure(string detail)
        {
            requiresSnapshotRefresh = true;
            snapshotReadSucceeded = false;
            IsEligible = false;
            BlockedReason = detail ?? string.Empty;
            Status = BlockedReason.Length == 0 ? Text("Ui.HooksRefreshFailed", "Hook snapshots could not be read.") : BlockedReason;
            ReplaceHooks(Array.Empty<HookWorkbenchRow>());
            SessionId = string.Empty;
        }

        void ReplaceHooks(IEnumerable<HookWorkbenchRow> items)
        {
            foreach (var row in hooks) row.PropertyChanged -= OnHookPropertyChanged;
            hooks.Clear();
            foreach (var row in items ?? Array.Empty<HookWorkbenchRow>())
            {
                row.PropertyChanged += OnHookPropertyChanged;
                hooks.Add(row);
            }
            Raise(nameof(SelectedCount));
            Raise(nameof(SelectionMessage));
            Raise(nameof(HasHooks));
            Raise(nameof(HooksSummary));
            RefreshFilters();
            RefreshCommandStates();
        }

        void OnHookPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName != nameof(HookWorkbenchRow.IsSelected)) return;
            Raise(nameof(SelectedCount));
            Raise(nameof(HiddenSelectedCount));
            Raise(nameof(SelectionMessage));
            RefreshCommandStates();
        }

        void RefreshCommandStates()
        {
            RefreshSnapshotsCommand?.NotifyCanExecuteChanged();
            PlanApplyCommand?.NotifyCanExecuteChanged();
            PlanRevertCommand?.NotifyCanExecuteChanged();
            ConfirmPlanCommand?.NotifyCanExecuteChanged();
            CancelPlanCommand?.NotifyCanExecuteChanged();
            VerifySelectedCommand?.NotifyCanExecuteChanged();
            ExportSnapshotsCommand?.NotifyCanExecuteChanged();
            ExportResultsCommand?.NotifyCanExecuteChanged();
            Raise(nameof(IsConnected));
            Raise(nameof(SupportsHookProtocol));
            Raise(nameof(CapabilityMessage));
            Raise(nameof(EligibilityText));
        }

        public void NotifyConnectionChanged()
        {
            ClearPendingPlan();
            requiresSnapshotRefresh = true;
            snapshotReadSucceeded = false;
            RefreshCommandStates();
            if (!IsConnected) Status = CapabilityMessage;
        }

        async Task CancelPendingPlanAsync(CancellationToken cancellation)
        {
            if (!HasPendingPlan || IsBusy) return;
            await CancelPendingPlanCoreAsync(cancellation).ConfigureAwait(true);
        }

        async Task<bool> CancelPendingPlanCoreAsync(CancellationToken cancellation)
        {
            var plan = pendingPlan;
            if (plan == null) return true;
            if (!IsConnected || !session.Supports("hook-plan-cancel") ||
                !string.Equals(plan.Session, SessionId, StringComparison.Ordinal))
            {
                Status = Text("Ui.HooksCancelUnconfirmed", "Plan cancellation was not confirmed; the pending plan remains available.");
                return false;
            }

            var ownsBusyState = !IsBusy;
            if (ownsBusyState) IsBusy = true;
            Status = Text("Ui.HooksPlanning", "Requesting cancellation of the pending plan…");
            try
            {
                // This is a single idempotent host request. Do not clear the local token until
                // the host confirms the same session and token were invalidated.
                var response = await session.SendAsync(new Request
                {
                    Action = "hook-plan-cancel",
                    Argument = Json.Serialize(new HookIpcCancelPlanRequest { Session = plan.Session, Token = plan.Token })
                }, cancellation).ConfigureAwait(true);
                if (response?.Success != true)
                {
                    Status = Text("Ui.HooksCancelUnconfirmed", "Plan cancellation was not confirmed; the pending plan remains available.") +
                        (string.IsNullOrWhiteSpace(response?.Error) ? string.Empty : " " + response.Error);
                    return false;
                }

                if (!TryReadCancelPlanResult(response.Data, plan.Session, out _, out var error))
                {
                    Status = Text("Ui.HooksCancelUnconfirmed", "Plan cancellation was not confirmed; the pending plan remains available.") +
                        (string.IsNullOrWhiteSpace(error) ? string.Empty : " " + error);
                    return false;
                }

                if (ReferenceEquals(pendingPlan, plan)) ClearPendingPlan();
                // Context changes and another client's newer plan can invalidate this token
                // before refresh. A matching-session `cancelled: false` proves this exact token
                // is no longer pending, so it is safe to discard the local preview and reconcile.
                Status = Text("Ui.HooksCancelStatus", "The operation was cancelled or the pending plan was discarded.");
                return true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                Status = Text("Ui.HooksCancelUnconfirmed", "Plan cancellation was not confirmed; the pending plan remains available.");
                return false;
            }
            catch (Exception error)
            {
                Status = Text("Ui.HooksCancelUnconfirmed", "Plan cancellation was not confirmed; the pending plan remains available.") + " " + error.Message;
                return false;
            }
            finally
            {
                if (ownsBusyState) IsBusy = false;
            }
        }

        void ClearPendingPlan()
        {
            expiryTimer.Stop();
            pendingPlan = null;
            plannedHooks.Clear();
            IsConfirmationChecked = false;
            Raise(nameof(HasPendingPlan));
            Raise(nameof(PendingOperation));
            Raise(nameof(IsApplyPlan));
            Raise(nameof(PlanWarning));
            Raise(nameof(PendingExpiryText));
            Raise(nameof(PlanHeading));
            Raise(nameof(IsPlanExpired));
            Raise(nameof(HasPendingHooks));
            ConfirmPlanCommand?.NotifyCanExecuteChanged();
            CancelPlanCommand?.NotifyCanExecuteChanged();
        }

        void OnExpiryTick(object sender, EventArgs args)
        {
            Raise(nameof(IsPlanExpired));
            Raise(nameof(PendingExpiryText));
            if (IsPlanExpired)
            {
                ClearPendingPlan();
                Status = Text("Ui.HooksPlanExpired", "The plan expired. Refresh snapshots and create a new plan.");
            }
            else ConfirmPlanCommand.NotifyCanExecuteChanged();
        }

        string FormatCommitStatus(HookCommit commit, IReadOnlyList<string> expectedIds)
        {
            var lines = new List<string>
            {
                commit.RequiresReconciliation
                    ? Text("Ui.HooksOperationPartial", "Operation did not fully succeed; review the reconciled state.")
                    : Text("Ui.HooksOperationSucceeded", "Operation completed and was verified.")
            };
            foreach (var result in commit.Results)
            {
                var verification = string.IsNullOrWhiteSpace(result.VerificationDetail) ? string.Empty : " · " + result.VerificationDetail;
                var verificationLabel = Text(result.Verified ? "Ui.HooksVerified" : "Ui.HooksUnverified", result.Verified ? "Verified" : "Unverified");
                lines.Add(result.Id + " — " + result.State + " · " + verificationLabel +
                    (string.IsNullOrWhiteSpace(result.Detail) ? string.Empty : " · " + result.Detail) + verification);
            }
            foreach (var id in commit.NotAttemptedIds)
                lines.Add(id + " — " + Text("Ui.HooksNotAttempted", "Not attempted by the host; state must be reconciled."));
            if (commit.Cancelled)
                lines.Add(Text("Ui.HooksCancelled", "The host reported cancellation; inspect refreshed hook states."));
            if (commit.Partial)
                lines.Add(Text("Ui.HooksPartial", "The host reported a partial operation."));
            if (!string.IsNullOrWhiteSpace(commit.StopReason)) lines.Add(commit.StopReason);
            var accounted = new HashSet<string>(commit.Results.Select(result => result.Id), StringComparer.Ordinal);
            accounted.UnionWith(commit.NotAttemptedIds);
            foreach (var missingId in expectedIds.Where(id => !accounted.Contains(id)))
                lines.Add(missingId + " — " + Text("Ui.HooksNotAttempted", "Not attempted by the host; state must be reconciled."));
            return string.Join(Environment.NewLine, lines);
        }

        string Text(string key, string fallback) => localize(key, fallback) ?? fallback;

        public static bool TryReadSnapshotEnvelope(string json, out HookSnapshotEnvelope envelope, out string error)
        {
            envelope = null;
            error = string.Empty;
            try
            {
                using var document = JsonDocument.Parse(json ?? throw new JsonException("Snapshot payload was empty."));
                var root = document.RootElement;
                if (!root.TryGetPropertyInsensitive("session", out var sessionElement) || sessionElement.ValueKind != JsonValueKind.String ||
                    !root.TryGetPropertyInsensitive("hooks", out var hooksElement) || hooksElement.ValueKind != JsonValueKind.Array)
                    throw new JsonException("Snapshot payload omitted its session or hook list.");

                var serviceAvailable = root.TryGetBooleanInsensitive("serviceAvailable", out var available) ? available : true;
                var eligible = root.TryGetBooleanInsensitive("canManage", out var canManage)
                    ? canManage
                    : root.TryGetBooleanInsensitive("eligible", out var isEligible) && isEligible;
                eligible &= serviceAvailable;
                var blocked = root.TryGetStringInsensitive("blockedReason", out var blockedReason) ? blockedReason : string.Empty;
                var rows = new List<HookWorkbenchRow>();
                foreach (var element in hooksElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.Object || !element.TryGetStringInsensitive("id", out var id) || string.IsNullOrWhiteSpace(id) ||
                        !element.TryGetStringInsensitive("owner", out var owner) ||
                        !(element.TryGetStringInsensitive("targetMethod", out var target) || element.TryGetStringInsensitive("target", out target)) ||
                        !element.TryGetPropertyInsensitive("state", out var stateElement))
                        throw new JsonException("A hook snapshot omitted its stable ID, owner, target, or state.");
                    var state = ReadState(stateElement);
                    var priority = element.TryGetInt32Insensitive("priority", out var parsedPriority) ? parsedPriority : (int?)null;
                    var before = ReadStrings(element, "before");
                    var after = ReadStrings(element, "after");
                    var hasPrefix = element.TryGetBooleanInsensitive("hasPrefix", out var prefix) && prefix;
                    var hasPostfix = element.TryGetBooleanInsensitive("hasPostfix", out var postfix) && postfix;
                    var detail = element.TryGetStringInsensitive("detail", out var note) ? note : string.Empty;
                    var hasFinalizer = element.TryGetBooleanInsensitive("hasFinalizer", out var finalizer) ? finalizer : (bool?)null;
                    var hasTranspiler = element.TryGetBooleanInsensitive("hasTranspiler", out var transpiler) ? transpiler : (bool?)null;
                    rows.Add(new HookWorkbenchRow(id, owner, target, hasPrefix, hasPostfix, priority, before, after, state, detail, hasFinalizer, hasTranspiler));
                }
                if (rows.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != rows.Count)
                    throw new JsonException("Snapshot payload contained duplicate hook IDs.");

                envelope = new HookSnapshotEnvelope(sessionElement.GetString(), eligible, blocked, rows);
                return true;
            }
            catch (Exception exception) when (exception is JsonException || exception is InvalidOperationException || exception is FormatException)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool TryReadPlan(string json, string expectedOperation, string expectedSession, IReadOnlyList<string> expectedIds,
            IReadOnlyList<HookWorkbenchRow> currentSnapshots,
            out HookPlan plan, out IReadOnlyList<HookWorkbenchRow> planned, out string error)
        {
            plan = null;
            planned = Array.Empty<HookWorkbenchRow>();
            error = string.Empty;
            try
            {
                using var document = JsonDocument.Parse(json ?? throw new JsonException("Plan payload was empty."));
                var root = document.RootElement;
                if (!root.TryGetStringInsensitive("operation", out var operation) || !SameOperation(operation, expectedOperation) ||
                    !root.TryGetStringInsensitive("session", out var session) || !string.Equals(session, expectedSession, StringComparison.Ordinal) ||
                    !root.TryGetStringInsensitive("token", out var token) || string.IsNullOrWhiteSpace(token) ||
                    !root.TryGetBooleanInsensitive("requiresConfirmation", out var confirmation) || !confirmation ||
                    !root.TryGetStringInsensitive("expiresAtUtc", out var expiryText) ||
                    !DateTimeOffset.TryParse(expiryText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expiresAt))
                    throw new JsonException("Plan payload did not match the requested operation, current session, or confirmation requirements.");
                if (expiresAt <= DateTimeOffset.UtcNow || expiresAt > DateTimeOffset.UtcNow.AddSeconds(65))
                    throw new JsonException("Plan expiry was invalid or exceeded the 60-second host limit.");
                if (!root.TryGetPropertyInsensitive("selected", out var hooksElement) && !root.TryGetPropertyInsensitive("hooks", out hooksElement))
                    throw new JsonException("Plan payload omitted the exact hook selection.");
                var ids = ReadHookIds(hooksElement);
                if (ids.Count != expectedIds.Count || ids.Distinct(StringComparer.Ordinal).Count() != ids.Count ||
                    !new HashSet<string>(expectedIds, StringComparer.Ordinal).SetEquals(ids))
                    throw new JsonException("Plan payload hook IDs differ from the selected inventory.");

                var plannedRows = ReadHookSnapshots(hooksElement);
                if (plannedRows.Count != ids.Count || plannedRows.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != ids.Count)
                    throw new JsonException("Plan payload did not provide one full snapshot per selected hook.");

                var selectedSnapshots = new List<HookWorkbenchRow>();
                foreach (var id in ids)
                {
                    var source = currentSnapshots?.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal));
                    if (source == null)
                        throw new JsonException("Plan referred to a hook that is not in the current inventory: " + id);
                    var plannedSnapshot = plannedRows.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal));
                    if (plannedSnapshot == null || !SameSnapshot(source, plannedSnapshot))
                        throw new JsonException("Plan hook metadata differs from the latest inventory: " + id);
                    selectedSnapshots.Add(plannedSnapshot);
                }
                plan = new HookPlan(operation: expectedOperation, session, token, expiresAt, confirmation, ids);
                planned = selectedSnapshots;
                return true;
            }
            catch (Exception exception) when (exception is JsonException || exception is InvalidOperationException || exception is FormatException)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool TryReadCommit(string json, string expectedOperation, string expectedSession, IReadOnlyList<string> expectedIds, out HookCommit commit, out string error)
        {
            commit = null;
            error = string.Empty;
            try
            {
                using var document = JsonDocument.Parse(json ?? throw new JsonException("Commit payload was empty."));
                var root = document.RootElement;
                if (!root.TryGetStringInsensitive("operation", out var operation) || !SameOperation(operation, expectedOperation) ||
                    !root.TryGetStringInsensitive("session", out var session) || !string.Equals(session, expectedSession, StringComparison.Ordinal) ||
                    !(root.TryGetBooleanInsensitive("tokenConsumed", out var consumed) || root.TryGetBooleanInsensitive("consumed", out consumed)) ||
                    !root.TryGetBooleanInsensitive("succeeded", out var succeeded) ||
                    !root.TryGetPropertyInsensitive("results", out var resultsElement) || resultsElement.ValueKind != JsonValueKind.Array)
                    throw new JsonException("Commit payload omitted operation status or hook results.");
                var results = new List<HookOperationResult>();
                foreach (var element in resultsElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.Object || !element.TryGetStringInsensitive("id", out var id) || string.IsNullOrWhiteSpace(id) ||
                        !element.TryGetPropertyInsensitive("state", out var stateElement) ||
                        !element.TryGetBooleanInsensitive("succeeded", out var itemSucceeded))
                        throw new JsonException("Commit result omitted an operation ID, state, or success flag.");
                    var detail = element.TryGetStringInsensitive("detail", out var itemDetail) ? itemDetail : string.Empty;
                    var verification = element.TryGetStringInsensitive("verificationDetail", out var verificationDetail) ? verificationDetail : string.Empty;
                    var verified = element.TryGetBooleanInsensitive("verified", out var itemVerified) && itemVerified;
                    results.Add(new HookOperationResult(id, ReadState(stateElement), itemSucceeded, verified, detail, verification));
                }
                var notAttemptedIds = ReadStrings(root, "notAttemptedIds");
                var accountedIds = results.Select(item => item.Id).Concat(notAttemptedIds).ToArray();
                if (accountedIds.Distinct(StringComparer.Ordinal).Count() != accountedIds.Length ||
                    !new HashSet<string>(expectedIds, StringComparer.Ordinal).SetEquals(accountedIds))
                    throw new JsonException("Commit results and not-attempted IDs did not account for the exact one-use plan.");
                var partial = root.TryGetBooleanInsensitive("partial", out var isPartial) && isPartial;
                var cancelled = root.TryGetBooleanInsensitive("cancelled", out var isCancelled) && isCancelled;
                var stopReason = root.TryGetStringInsensitive("stopReason", out var reason) ? reason : string.Empty;
                commit = new HookCommit(operation, consumed, succeeded, partial, cancelled, stopReason, notAttemptedIds, results);
                return true;
            }
            catch (Exception exception) when (exception is JsonException || exception is InvalidOperationException || exception is FormatException)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool TryReadCancelPlanResult(string json, string expectedSession, out bool cancelled, out string error)
        {
            cancelled = false;
            error = string.Empty;
            try
            {
                using var document = JsonDocument.Parse(json ?? throw new JsonException("Plan cancellation payload was empty."));
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object ||
                    !root.TryGetStringInsensitive("session", out var session) ||
                    !string.Equals(session, expectedSession, StringComparison.Ordinal) ||
                    !root.TryGetBooleanInsensitive("cancelled", out cancelled))
                    throw new JsonException("Plan cancellation response did not match the current session or include a cancellation result.");
                return true;
            }
            catch (Exception exception) when (exception is JsonException || exception is InvalidOperationException || exception is FormatException)
            {
                error = exception.Message;
                return false;
            }
        }

        static bool SameOperation(string actual, string expected) =>
            string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(actual, "hook-" + expected, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(actual, "hook-" + expected + "-plan", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(actual, "hook-" + expected + "-confirm", StringComparison.OrdinalIgnoreCase);

        static List<string> ReadHookIds(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array) throw new JsonException("Plan hook selection was not an array.");
            var ids = new List<string>();
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String) ids.Add(item.GetString());
                else if (item.ValueKind == JsonValueKind.Object && item.TryGetStringInsensitive("id", out var id)) ids.Add(id);
                else throw new JsonException("Plan contains an unrecognized hook selection entry.");
            }
            if (ids.Any(string.IsNullOrWhiteSpace)) throw new JsonException("Plan contains an empty hook ID.");
            return ids;
        }

        static List<HookWorkbenchRow> ReadHookSnapshots(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array) throw new JsonException("Plan hook snapshots were not an array.");
            var rows = new List<HookWorkbenchRow>();
            foreach (var hook in element.EnumerateArray())
            {
                if (hook.ValueKind != JsonValueKind.Object || !hook.TryGetStringInsensitive("id", out var id) || string.IsNullOrWhiteSpace(id) ||
                    !hook.TryGetStringInsensitive("owner", out var owner) ||
                    !(hook.TryGetStringInsensitive("targetMethod", out var target) || hook.TryGetStringInsensitive("target", out target)) ||
                    !hook.TryGetPropertyInsensitive("state", out var stateElement))
                    throw new JsonException("A plan hook omitted its registered ID, owner, target, or state.");
                var state = ReadState(stateElement);
                var priority = hook.TryGetInt32Insensitive("priority", out var value) ? value : (int?)null;
                var before = ReadStrings(hook, "before");
                var after = ReadStrings(hook, "after");
                var hasPrefix = hook.TryGetBooleanInsensitive("hasPrefix", out var prefix) && prefix;
                var hasPostfix = hook.TryGetBooleanInsensitive("hasPostfix", out var postfix) && postfix;
                var detail = hook.TryGetStringInsensitive("detail", out var note) ? note : string.Empty;
                var hasFinalizer = hook.TryGetBooleanInsensitive("hasFinalizer", out var finalizer) ? finalizer : (bool?)null;
                var hasTranspiler = hook.TryGetBooleanInsensitive("hasTranspiler", out var transpiler) ? transpiler : (bool?)null;
                rows.Add(new HookWorkbenchRow(id, owner, target, hasPrefix, hasPostfix, priority, before, after, state, detail, hasFinalizer, hasTranspiler));
            }
            return rows;
        }

        static bool SameSnapshot(HookWorkbenchRow left, HookWorkbenchRow right) =>
            string.Equals(left.Owner, right.Owner, StringComparison.Ordinal) &&
            string.Equals(left.Target, right.Target, StringComparison.Ordinal) &&
            string.Equals(left.State, right.State, StringComparison.Ordinal) &&
            string.Equals(left.Detail, right.Detail, StringComparison.Ordinal) &&
            left.HasPrefix == right.HasPrefix && left.HasPostfix == right.HasPostfix && left.Priority == right.Priority &&
            left.HasFinalizer == right.HasFinalizer && left.HasTranspiler == right.HasTranspiler &&
            left.Before.SequenceEqual(right.Before, StringComparer.Ordinal) && left.After.SequenceEqual(right.After, StringComparer.Ordinal);

        static IReadOnlyList<string> ReadStrings(JsonElement element, string name)
        {
            if (!element.TryGetPropertyInsensitive(name, out var values) || values.ValueKind != JsonValueKind.Array) return Array.Empty<string>();
            var result = new List<string>();
            foreach (var value in values.EnumerateArray()) if (value.ValueKind == JsonValueKind.String) result.Add(value.GetString());
            return result;
        }

        static string ReadState(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String) return element.GetString() ?? "Unknown";
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var ordinal))
            {
                var values = new[] { "Registered", "Applied", "Reverted", "Conflict", "Failed", "Unsupported" };
                return ordinal >= 0 && ordinal < values.Length ? values[ordinal] : "Unknown";
            }
            throw new JsonException("Hook state was not a recognized enum value.");
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            expiryTimer.Stop();
            expiryTimer.Tick -= OnExpiryTick;
            ClearPendingPlan();
            foreach (var row in hooks) row.PropertyChanged -= OnHookPropertyChanged;
            hooks.Clear();
            filteredHooks.Clear();
        }
    }

    internal sealed class HookWorkbenchRow : ObservableObject
    {
        bool isSelected;

        public HookWorkbenchRow(string id, string owner, string target, bool hasPrefix, bool hasPostfix,
            int? priority, IReadOnlyList<string> before, IReadOnlyList<string> after, string state, string detail,
            bool? hasFinalizer = null, bool? hasTranspiler = null)
        {
            Id = id ?? string.Empty;
            Owner = owner ?? string.Empty;
            Target = target ?? string.Empty;
            HasPrefix = hasPrefix;
            HasPostfix = hasPostfix;
            HasFinalizer = hasFinalizer;
            HasTranspiler = hasTranspiler;
            Priority = priority;
            Before = before ?? Array.Empty<string>();
            After = after ?? Array.Empty<string>();
            State = state ?? "Unknown";
            Detail = detail ?? string.Empty;
        }

        public string Id { get; }
        public string Owner { get; }
        public string Target { get; }
        public bool HasPrefix { get; }
        public bool HasPostfix { get; }
        public bool? HasFinalizer { get; }
        public bool? HasTranspiler { get; }
        public int? Priority { get; }
        public IReadOnlyList<string> Before { get; }
        public IReadOnlyList<string> After { get; }
        public string State { get; }
        public string Detail { get; }
        public string Order => "Priority " + (Priority?.ToString(CultureInfo.InvariantCulture) ?? "default") +
            (Before.Count == 0 ? string.Empty : " · Before: " + string.Join(", ", Before)) +
            (After.Count == 0 ? string.Empty : " · After: " + string.Join(", ", After));
        public string HookKinds => string.Join(" / ", new[] { HasPrefix ? "Prefix" : null, HasPostfix ? "Postfix" : null,
            HasFinalizer == true ? "Finalizer" : null, HasTranspiler == true ? "Transpiler" : null }.Where(item => item != null));
        public bool IsSelectable => string.Equals(State, "Registered", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(State, "Reverted", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(State, "Applied", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(State, "Conflict", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(State, "Failed", StringComparison.OrdinalIgnoreCase);
        public bool IsSelected { get => isSelected; set { if (Set(ref isSelected, value)) Raise(); } }

        public HookWorkbenchRow Copy() => new(Id, Owner, Target, HasPrefix, HasPostfix, Priority, Before.ToArray(), After.ToArray(), State, Detail, HasFinalizer, HasTranspiler);
    }

    internal sealed class HookSnapshotEnvelope(string session, bool eligible, string blockedReason, IReadOnlyList<HookWorkbenchRow> hooks)
    {
        public string Session { get; } = session ?? string.Empty;
        public bool Eligible { get; } = eligible;
        public string BlockedReason { get; } = blockedReason ?? string.Empty;
        public IReadOnlyList<HookWorkbenchRow> Hooks { get; } = hooks ?? Array.Empty<HookWorkbenchRow>();
    }

    internal sealed class HookPlan(string operation, string session, string token, DateTimeOffset expiresAtUtc, bool requiresConfirmation, IReadOnlyList<string> hookIds)
    {
        public string Operation { get; } = operation ?? string.Empty;
        public string Session { get; } = session ?? string.Empty;
        public string Token { get; } = token ?? string.Empty;
        public DateTimeOffset ExpiresAtUtc { get; } = expiresAtUtc;
        public bool RequiresConfirmation { get; } = requiresConfirmation;
        public IReadOnlyList<string> HookIds { get; } = hookIds ?? Array.Empty<string>();
    }

    internal sealed class HookOperationResult(string id, string state, bool succeeded, bool verified, string detail, string verificationDetail)
    {
        public string Id { get; } = id ?? string.Empty;
        public string State { get; } = state ?? "Unknown";
        public bool Succeeded { get; } = succeeded;
        public bool Verified { get; } = verified;
        public string Detail { get; } = detail ?? string.Empty;
        public string VerificationDetail { get; } = verificationDetail ?? string.Empty;
    }

    internal sealed class HookCommit(string operation, bool tokenConsumed, bool succeeded, bool partial, bool cancelled,
        string stopReason, IReadOnlyList<string> notAttemptedIds, IReadOnlyList<HookOperationResult> results)
    {
        public string Operation { get; } = operation ?? string.Empty;
        public bool TokenConsumed { get; } = tokenConsumed;
        public bool Succeeded { get; } = succeeded;
        public bool Partial { get; } = partial;
        public bool Cancelled { get; } = cancelled;
        public string StopReason { get; } = stopReason ?? string.Empty;
        public IReadOnlyList<string> NotAttemptedIds { get; } = notAttemptedIds ?? Array.Empty<string>();
        public IReadOnlyList<HookOperationResult> Results { get; } = results ?? Array.Empty<HookOperationResult>();
        public bool RequiresReconciliation => Partial || Cancelled || NotAttemptedIds.Count > 0 ||
            !string.IsNullOrWhiteSpace(StopReason) || !Succeeded || Results.Any(result => !result.Succeeded || !result.Verified);
    }

    internal static class HookWorkbenchJsonExtensions
    {
        public static bool TryGetPropertyInsensitive(this JsonElement element, string name, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        public static bool TryGetStringInsensitive(this JsonElement element, string name, out string value)
        {
            if (element.TryGetPropertyInsensitive(name, out var property) && property.ValueKind == JsonValueKind.String)
            {
                value = property.GetString();
                return true;
            }
            value = null;
            return false;
        }

        public static bool TryGetBooleanInsensitive(this JsonElement element, string name, out bool value)
        {
            if (element.TryGetPropertyInsensitive(name, out var property))
            {
                if (property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False)
                {
                    value = property.GetBoolean();
                    return true;
                }
                if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var integer))
                {
                    value = integer != 0;
                    return true;
                }
            }
            value = false;
            return false;
        }

        public static bool TryGetInt32Insensitive(this JsonElement element, string name, out int value)
        {
            if (element.TryGetPropertyInsensitive(name, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value)) return true;
            value = 0;
            return false;
        }
    }
}
