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
        bool hookConfirmationChecked;
        string hookPlanPreview;
        float hookExpiryCheckSeconds;

        [DataSourceProperty] public bool IsHookWorkbenchVisible => IsPatchPreflightActive && ShowCommandDeck;
        [DataSourceProperty] public bool HasHookPlan => hookPlan != null;
        [DataSourceProperty] public bool HasNoHookPlan => hookPlan == null;
        [DataSourceProperty] public bool IsHookSelectionDisabled => !IsHookWorkbenchVisible || hookPlan != null || !HasRegisteredHookSelection(hookStatus, selectedHookId);
        [DataSourceProperty] public bool IsHookConfirmDisabled => !CanConfirmHookPlan();
        [DataSourceProperty] public bool HookConfirmationChecked
        {
            get => hookConfirmationChecked;
            set { hookConfirmationChecked = value; OnPropertyChangedWithValue(value, nameof(HookConfirmationChecked)); OnPropertyChanged(nameof(IsHookConfirmDisabled)); }
        }
        [DataSourceProperty] public string HookStatusLabel => T("Hook inventory");
        [DataSourceProperty] public string HookNextLabel => T("Next hook");
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
                response.Data = T("Selected hook") + ": " + (selectedHookId ?? "-") + "\n" + response.Data;
            }
            ShowHookResponse(response);
        }

        public void ExecuteHookNext()
        {
            if (!IsHookWorkbenchVisible || !CancelHookPlan()) return;
            var response = HookRequest("hook-snapshots");
            if (!TryReadHookStatus(response)) { ShowHookResponse(response); return; }
            var items = hookStatus.Hooks;
            if (items.Count != 0) selectedHookId = items[(items.FindIndex(item => item.Id == selectedHookId) + 1) % items.Count].Id;
            else selectedHookId = null;
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
            if (!response.Success) return false;
            try
            {
                var status = Json.Deserialize<HookIpcStatus>(response.Data);
                if (status == null || string.IsNullOrWhiteSpace(status.Session) || status.Hooks == null ||
                    status.Hooks.Any(item => item == null || string.IsNullOrWhiteSpace(item.Id)) ||
                    status.Hooks.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != status.Hooks.Count)
                    throw new InvalidOperationException("The hook inventory is missing its session or unique registered IDs.");
                hookStatus = status;
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
                    hookPlan = null;
                    hookPlanPreview = null;
                }
                catch (Exception error) { response = new Response { Error = error.Message }; }
            }
            ShowHookResponse(response);
            return hookPlan == null;
        }
    }
}
