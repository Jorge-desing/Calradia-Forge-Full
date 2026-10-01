using System.Collections.Generic;

namespace CalradiaForge.Core
{
    /// <summary>Request body for a two-phase hook action. Only pre-registered hook IDs are accepted.</summary>
    public sealed class HookIpcSelection
    {
        public List<string> HookIds { get; set; } = new List<string>();
    }

    /// <summary>Request body for the commit phase of a hook action.</summary>
    public sealed class HookIpcConfirmation
    {
        public string Token { get; set; }
    }

    /// <summary>Single-use cancellation request bound to the server session and exact preview token.</summary>
    public sealed class HookIpcCancelPlanRequest
    {
        public string Session { get; set; }
        public string Token { get; set; }
    }

    /// <summary>Result of idempotently invalidating one still-pending preview token.</summary>
    public sealed class HookIpcCancelPlanResult
    {
        public string Session { get; set; }
        public bool Cancelled { get; set; }
    }

    /// <summary>JSON-safe mutable projection of an immutable SDK hook snapshot.</summary>
    public sealed class HookIpcSnapshot
    {
        public string Id { get; set; }
        public string Owner { get; set; }
        public string TargetMethod { get; set; }
        public bool HasPrefix { get; set; }
        public bool HasPostfix { get; set; }
        public bool HasFinalizer { get; set; }
        public bool HasTranspiler { get; set; }
        public int? Priority { get; set; }
        public List<string> Before { get; set; } = new List<string>();
        public List<string> After { get; set; } = new List<string>();
        public string State { get; set; }
        public string Detail { get; set; }
    }

    /// <summary>Read-only hook status and the exact menu/context gate used for management.</summary>
    public sealed class HookIpcStatus
    {
        public string Session { get; set; }
        public bool ServiceAvailable { get; set; }
        public bool CanManage { get; set; }
        public string BlockedReason { get; set; }
        public List<HookIpcSnapshot> Hooks { get; set; } = new List<HookIpcSnapshot>();
    }

    /// <summary>Server-issued preview for an Apply or Revert operation. The token is single-use and short-lived.</summary>
    public sealed class HookIpcPlan
    {
        public string Operation { get; set; }
        public string Session { get; set; }
        public string Token { get; set; }
        public string ExpiresAtUtc { get; set; }
        public bool RequiresConfirmation { get; set; }
        public List<HookIpcSnapshot> Hooks { get; set; } = new List<HookIpcSnapshot>();
    }

    /// <summary>Per-hook result returned after a confirmed operation.</summary>
    public sealed class HookIpcResult
    {
        public string Id { get; set; }
        public string State { get; set; }
        public bool Succeeded { get; set; }
        public bool Verified { get; set; }
        public string Detail { get; set; }
        public string VerificationDetail { get; set; }
    }

    /// <summary>JSON-safe result of consuming a confirmation plan.</summary>
    public sealed class HookIpcCommit
    {
        public string Operation { get; set; }
        public string Session { get; set; }
        public bool TokenConsumed { get; set; }
        public bool Succeeded { get; set; }
        /// <summary>True when the requested batch did not finish or has mixed successful and failed results.</summary>
        public bool Partial { get; set; }
        /// <summary>True when the pipe requested cooperative cancellation between hook operations.</summary>
        public bool Cancelled { get; set; }
        /// <summary>Registered IDs that were in the plan but were not attempted.</summary>
        public List<string> NotAttemptedIds { get; set; } = new List<string>();
        /// <summary>Reason a commit stopped before every planned ID was attempted.</summary>
        public string StopReason { get; set; }
        public List<HookIpcResult> Results { get; set; } = new List<HookIpcResult>();
    }
}
