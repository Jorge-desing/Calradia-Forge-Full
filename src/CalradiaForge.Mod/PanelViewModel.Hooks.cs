using System;
using System.Linq;
using System.Globalization;
using System.Threading;
using CalradiaForge.Core;
using TaleWorlds.Library;

namespace CalradiaForge.Mod
{
    internal sealed partial class PanelViewModel
    {
        HookIpcStatus hookStatus;
        HookIpcPlan hookPlan;
        string selectedHookId;
        readonly MBBindingList<HookPickerItemVM> hookPickerItems = new MBBindingList<HookPickerItemVM>();
        bool hookPickerOpen;
        bool hookConfirmationChecked;
        string hookPlanPreview;
        float hookExpiryCheckSeconds;

        [DataSourceProperty] public bool IsHookWorkbenchVisible => IsPatchPreflightActive && ShowCommandDeck;
        [DataSourceProperty] public bool HasHookPlan => hookPlan != null;
        [DataSourceProperty] public bool HasNoHookPlan => hookPlan == null;
        [DataSourceProperty] public bool IsHookSelectionDisabled => !IsHookWorkbenchVisible || hookPlan != null || !HasRegisteredHookSelection(hookStatus, selectedHookId);
        [DataSourceProperty] public bool IsHookPickerDisabled => !IsHookWorkbenchVisible || hookPlan != null;
        [DataSourceProperty] public bool IsHookPickerOpen => hookPickerOpen && IsHookWorkbenchVisible;
        [DataSourceProperty] public bool IsHookPickerHasItems => hookPickerItems.Count > 0;
        [DataSourceProperty] public bool IsHookPickerEmpty => hookPickerItems.Count == 0;
        [DataSourceProperty] public MBBindingList<HookPickerItemVM> HookPickerItems => hookPickerItems;
        [DataSourceProperty] public bool IsHookConfirmDisabled => !CanConfirmHookPlan();
        [DataSourceProperty] public bool HookConfirmationChecked
        {
            get => hookConfirmationChecked;
            set { hookConfirmationChecked = value; OnPropertyChangedWithValue(value, nameof(HookConfirmationChecked)); OnPropertyChanged(nameof(IsHookConfirmDisabled)); }
        }
        [DataSourceProperty] public string HookStatusLabel => T("Hook inventory");
        [DataSourceProperty] public string HookNextLabel => T("Next hook");
        [DataSourceProperty] public string HookSelectLabel => T("Choose hook");
        [DataSourceProperty] public string HookPickerTitleLabel => T("Choose a registered hook");
        [DataSourceProperty] public string HookPickerEmptyLabel => T("No registered hooks are available.");
        [DataSourceProperty] public string HookVerifyLabel => T("Verify selected");
        [DataSourceProperty] public string HookApplyLabel => T("Preview apply");
        [DataSourceProperty] public string HookRevertLabel => T("Preview revert");
        [DataSourceProperty] public string HookConfirmLabel => T("Confirm action");
        [DataSourceProperty] public string HookCancelLabel => T("Cancel");
        [DataSourceProperty] public string HookApprovalLabel => T("Approve displayed hook plan");

        Response HookRequest(string action, string data = null)
        {
            try { return runtime.Handle(new Request { Action = action, Argument = data }, CancellationToken.None)
                ?? new Response { Error = "The host did not return a hook response." }; }
            catch (Exception error) { return new Response { Error = error.Message }; }
        }

        void ShowHookResponse(Response response)
        {
            overview = false;
            full = response.Success ? response.Data : T("Error") + ": " + response.Error;
            page = 0;
            Render();
            NotifyHookPlan();
        }

        void NotifyHookPlan()
        {
            foreach (var property in new[] { nameof(HasHookPlan), nameof(HasNoHookPlan), nameof(HookConfirmationChecked), nameof(IsHookSelectionDisabled), nameof(IsHookConfirmDisabled) })
                OnPropertyChanged(property);
        }

        public void ExecuteHookInventory()
        {
            if (!IsHookWorkbenchVisible || !CancelHookPlan()) return;
            var response = HookRequest("hook-snapshots");
            if (TryReadHookStatus(response))
            {
                if (!hookStatus.Hooks.Any(item => item.Id == selectedHookId)) selectedHookId = hookStatus.Hooks.FirstOrDefault()?.Id;
                NotifyHookPickerSelection();
                response.Data = T("Selected hook") + ": " + (selectedHookId ?? "-") + "\n" + response.Data;
            }
            ShowHookResponse(response);
        }

        public void ExecuteHookSelect()
        {
            if (IsHookPickerDisabled) return;
            ExecuteHookInventory();
            if (hookStatus == null || hookPlan != null || !IsHookWorkbenchVisible) return;
            hookPickerOpen = true;
            OnPropertyChangedWithValue(true, nameof(IsHookPickerOpen));
            OnPropertyChanged(nameof(IsHookPickerEmpty));
        }

        public void ExecuteHookClosePicker()
        {
            if (!hookPickerOpen) return;
            hookPickerOpen = false;
            OnPropertyChangedWithValue(false, nameof(IsHookPickerOpen));
        }

        internal void SelectRegisteredHookFromPicker(string id)
        {
            if (!IsHookPickerOpen || !IsHookWorkbenchVisible ||
                !HasRegisteredHookSelection(hookStatus, id) || !CancelHookPlan()) return;

            selectedHookId = id;
            NotifyHookPickerSelection();
            ExecuteHookClosePicker();
            ShowHookResponse(new Response { Success = true, Data = T("Selected hook") + ": " + id });
        }

        internal bool IsHookPickerSelection(string id) =>
            string.Equals(selectedHookId, id, StringComparison.Ordinal);

        void NotifyHookPickerSelection()
        {
            for (int i = 0; i < hookPickerItems.Count; i++)
                hookPickerItems[i].NotifySelectionChanged();
        }

        public void ExecuteHookNext()
        {
            if (!IsHookWorkbenchVisible || !CancelHookPlan()) return;
            var response = HookRequest("hook-snapshots");
            if (!TryReadHookStatus(response)) { ShowHookResponse(response); return; }
            var items = hookStatus.Hooks;
            if (items.Count != 0) selectedHookId = items[(items.FindIndex(item => item.Id == selectedHookId) + 1) % items.Count].Id;
            else selectedHookId = null;
            NotifyHookPickerSelection();
            response.Data = T("Selected hook") + ": " + (selectedHookId ?? "-") + "\n" + response.Data;
            ShowHookResponse(response);
        }

        string HookSelection() => Json.Serialize(new HookIpcSelection { HookIds = new System.Collections.Generic.List<string> { selectedHookId } });
        public void ExecuteHookVerify()
        {
            if (IsHookSelectionDisabled) return;
            ShowHookResponse(HookRequest("hook-verify", HookSelection()));
        }
        public void ExecuteHookApplyPlan() => PrepareHookPlan("apply");
        public void ExecuteHookRevertPlan() => PrepareHookPlan("revert");

        void PrepareHookPlan(string operation)
        {
            if (IsHookSelectionDisabled) return;
            HookConfirmationChecked = false;
            var response = HookRequest("hook-" + operation + "-plan", HookSelection());
            if (response.Success)
            {
                try
                {
                    var plan = Json.Deserialize<HookIpcPlan>(response.Data);
                    if (!IsValidHookPlan(plan, hookStatus, selectedHookId, operation, DateTimeOffset.UtcNow))
                        throw new InvalidOperationException("The host plan does not match the displayed inventory and selected hook.");
                    hookPlan = plan;
                    hookPlanPreview = response.Data;
                }
                catch (Exception error) { response = new Response { Error = error.Message }; }
            }
            ShowHookResponse(response);
        }

        bool CanConfirmHookPlan() => IsHookWorkbenchVisible && HookConfirmationChecked &&
            string.Equals(full, hookPlanPreview, StringComparison.Ordinal) &&
            IsValidHookPlan(hookPlan, hookStatus, selectedHookId, hookPlan?.Operation, DateTimeOffset.UtcNow);

        internal static bool HasRegisteredHookSelection(HookIpcStatus status, string id) => status != null &&
            status.ServiceAvailable && status.CanManage && !string.IsNullOrWhiteSpace(status.Session) &&
            !string.IsNullOrWhiteSpace(id) && status.Hooks != null &&
            status.Hooks.Count(item => item != null && string.Equals(item.Id, id, StringComparison.Ordinal)) == 1;

        internal static bool IsValidHookPlan(HookIpcPlan plan, HookIpcStatus status, string id, string operation, DateTimeOffset now)
        {
            if (plan == null || !HasRegisteredHookSelection(status, id) ||
                (operation != "apply" && operation != "revert") || plan.Operation != operation ||
                !string.Equals(plan.Session, status.Session, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(plan.Token) || !plan.RequiresConfirmation ||
                !DateTimeOffset.TryParse(plan.ExpiresAtUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expiry) ||
                expiry <= now || expiry > now.AddSeconds(65) || plan.Hooks == null || plan.Hooks.Count != 1 ||
                plan.Hooks[0] == null || !string.Equals(plan.Hooks[0].Id, id, StringComparison.Ordinal)) return false;
            return string.Equals(Json.Serialize(plan.Hooks[0]), Json.Serialize(status.Hooks.Single(item => item != null && item.Id == id)), StringComparison.Ordinal);
        }

        bool TryReadHookStatus(Response response)
        {
            hookStatus = null;
            hookPickerItems.Clear();
            OnPropertyChanged(nameof(HookPickerItems));
            OnPropertyChanged(nameof(IsHookPickerHasItems));
            OnPropertyChanged(nameof(IsHookPickerEmpty));
            if (!response.Success) return false;
            try
            {
                var status = Json.Deserialize<HookIpcStatus>(response.Data);
                if (status == null || string.IsNullOrWhiteSpace(status.Session) || status.Hooks == null ||
                    status.Hooks.Any(item => item == null || string.IsNullOrWhiteSpace(item.Id)) ||
                    status.Hooks.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != status.Hooks.Count)
                    throw new InvalidOperationException("The hook inventory is missing its session or unique registered IDs.");
                hookStatus = status;
                hookPickerItems.Clear();
                foreach (var item in status.Hooks)
                    hookPickerItems.Add(new HookPickerItemVM(this, item));
                OnPropertyChanged(nameof(HookPickerItems));
                OnPropertyChanged(nameof(IsHookPickerHasItems));
                OnPropertyChanged(nameof(IsHookPickerEmpty));
                return true;
            }
            catch (Exception error) { response.Success = false; response.Error = error.Message; return false; }
        }

        public void ExecuteHookConfirm()
        {
            var plan = hookPlan;
            if (!CanConfirmHookPlan()) return;
            hookPlan = null;
            hookPlanPreview = null;
            HookConfirmationChecked = false;
            NotifyHookPlan();
            ShowHookResponse(HookRequest("hook-" + plan.Operation + "-confirm", Json.Serialize(new HookIpcConfirmation { Token = plan.Token })));
        }

        public void ExecuteHookCancel()
        {
            CancelHookPlan();
        }

        void RefreshHookConfirmation(float dt)
        {
            if (hookPlan == null || !hookConfirmationChecked) return;
            hookExpiryCheckSeconds += dt;
            if (hookExpiryCheckSeconds < 0.25f) return;
            hookExpiryCheckSeconds = 0;
            if (!CanConfirmHookPlan()) { HookConfirmationChecked = false; NotifyHookPlan(); }
        }

        bool CancelHookPlan()
        {
            var plan = hookPlan;
            HookConfirmationChecked = false;
            if (plan == null) return true;
            var response = HookRequest("hook-plan-cancel", Json.Serialize(new HookIpcCancelPlanRequest { Session = plan.Session, Token = plan.Token }));
            if (response.Success)
            {
                try
                {
                    var raw = Newtonsoft.Json.Linq.JObject.Parse(response.Data);
                    if (raw["Cancelled"]?.Type != Newtonsoft.Json.Linq.JTokenType.Boolean)
                        throw new InvalidOperationException("Hook plan cancellation is missing its Boolean outcome.");
                    var result = Json.Deserialize<HookIpcCancelPlanResult>(response.Data);
                    if (result == null || !string.Equals(result.Session, plan.Session, StringComparison.Ordinal))
                        throw new InvalidOperationException("Hook plan cancellation did not match the preview session.");
                    if (!result.Cancelled &&
                        (!DateTimeOffset.TryParse(plan.ExpiresAtUtc, CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expiry) ||
                         expiry > DateTimeOffset.UtcNow))
                        throw new InvalidOperationException("The host did not confirm cancellation; the displayed hook plan remains locked.");
                    // A false cancellation outcome is safe to clear only after the preview's own
                    // verified expiry. Until then, retain the token and block selection changes.
                    hookPlan = null;
                    hookPlanPreview = null;
                }
                catch (Exception error) { response = new Response { Error = error.Message }; }
            }
            ShowHookResponse(response);
            return hookPlan == null;
        }

        void CloseHookPickerForRouteChange()
        {
            ExecuteHookClosePicker();
            if (hookPlan != null) CancelHookPlan();
        }
    }

    internal sealed class HookPickerItemVM : ViewModel
    {
        readonly PanelViewModel parent;

        internal HookPickerItemVM(PanelViewModel owner, HookIpcSnapshot snapshot)
        {
            parent = owner;
            Id = snapshot?.Id ?? string.Empty;
            var kinds = new System.Collections.Generic.List<string>(4);
            if (snapshot?.HasPrefix == true) kinds.Add("Prefix");
            if (snapshot?.HasPostfix == true) kinds.Add("Postfix");
            if (snapshot?.HasFinalizer == true) kinds.Add("Finalizer");
            if (snapshot?.HasTranspiler == true) kinds.Add("Transpiler");
            Details = string.Join(" · ", new[] { snapshot?.Owner ?? string.Empty, snapshot?.TargetMethod ?? string.Empty,
                string.Join("/", kinds), snapshot?.State ?? string.Empty });
        }

        [DataSourceProperty] public string Id { get; }
        [DataSourceProperty] public string Details { get; }
        [DataSourceProperty] public bool IsSelected => parent != null && parent.IsHookPickerSelection(Id);

        public void ExecuteSelect() => parent?.SelectRegisteredHookFromPicker(Id);

        internal void NotifySelectionChanged() => OnPropertyChanged(nameof(IsSelected));
    }
}
