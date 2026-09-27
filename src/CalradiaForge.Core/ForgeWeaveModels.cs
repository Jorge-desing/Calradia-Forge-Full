using System;
using System.Collections.Generic;
using CalradiaForge.Sdk;

namespace CalradiaForge.Core
{
    // These snapshots are copied before they leave the dispatcher. They contain health evidence,
    // never callback data or live game objects supplied by the host.
    public sealed class ForgeWeaveHandlerHealth
    {
        public string Id { get; set; }
        public string Module { get; set; }
        public string Name { get; set; }
        public ForgeEventKind Event { get; set; }
        public ForgeEventAccess Access { get; set; }
        public ForgeReplayMode ReplayMode { get; set; }
        public ForgeEventFilter Filter { get; set; } = new ForgeEventFilter();
        public int BudgetMilliseconds { get; set; }
        public ForgeBudgetPolicy BudgetPolicy { get; set; }
        public int BudgetExceededCount { get; set; }
        public string LastBudgetExceededAt { get; set; }
        public Context Context { get; set; }
        public bool ChangesState { get; set; }
        public int Priority { get; set; }
        public int FailureLimit { get; set; }
        public int MinimumIntervalMilliseconds { get; set; }
        public List<string> Before { get; set; } = new List<string>();
        public List<string> After { get; set; } = new List<string>();
        // Ready, Blocked, or Quarantined describe persistent dispatch eligibility.
        public string Status { get; set; }
        public string BlockingReason { get; set; }
        public string LastOutcome { get; set; }
        public int InvocationCount { get; set; }
        public int FailureCount { get; set; }
        public int ConsecutiveFailureCount { get; set; }
        public double TotalMilliseconds { get; set; }
        public double MeanMilliseconds { get; set; }
        public double MinMilliseconds { get; set; }
        public double MaxMilliseconds { get; set; }
        public double LastMilliseconds { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string CircuitState { get; set; } = "Closed";
        public double P50Milliseconds { get; set; }
        public double P95Milliseconds { get; set; }
        public double P99Milliseconds { get; set; }
        public int BucketUnder1Ms { get; set; }
        public int Bucket1To5Ms { get; set; }
        public int Bucket5To20Ms { get; set; }
        public int BucketOver20Ms { get; set; }
        public string LastError { get; set; }
        public string LastInvokedAt { get; set; }
    }
    public sealed class ForgeWeaveEventHealth
    {
        public ForgeEventKind Event { get; set; }
        public int DispatchCount { get; set; }
        public int HandlerInvocationCount { get; set; }
        public int FailureCount { get; set; }
        public int BudgetExceededCount { get; set; }
        public int SkippedCount { get; set; }
        public double TotalMilliseconds { get; set; }
        public double MeanMilliseconds { get; set; }
        public double MinMilliseconds { get; set; }
        public double MaxMilliseconds { get; set; }
        public string LastDispatchedAt { get; set; }
    }
    public sealed class ForgeWeaveDispatchRecord
    {
        public long Sequence { get; set; }
        public ForgeEventKind Event { get; set; }
        public string Context { get; set; }
        public bool IsReplay { get; set; }
        public long? SourceSequence { get; set; }
        public string Status { get; set; }
        public string RejectionReason { get; set; }
        public int InvokedCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailureCount { get; set; }
        public int BudgetExceededCount { get; set; }
        public bool PropagationStopped { get; set; }
        public string StopReason { get; set; }
        public double Milliseconds { get; set; }
        public string DispatchedAt { get; set; }
        public List<ForgeHandlerOutcome> HandlerOutcomes { get; set; } = new List<ForgeHandlerOutcome>();
    }
    public sealed class ForgeWeaveDispatchResult
    {
        public ForgeEventKind Event { get; set; }
        public long Sequence { get; set; }
        public bool IsReplay { get; set; }
        public long? SourceSequence { get; set; }
        public string Status { get; set; }
        public string RejectionReason { get; set; }
        public int InvokedCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailureCount { get; set; }
        public int BudgetExceededCount { get; set; }
        public int QuarantinedCount { get; set; }
        public bool PropagationStopped { get; set; }
        public string StopReason { get; set; }
        public double Milliseconds { get; set; }
        public List<Finding> Findings { get; set; } = new List<Finding>();
        public List<ForgeHandlerOutcome> HandlerOutcomes { get; set; } = new List<ForgeHandlerOutcome>();
    }
    public sealed class ForgeWeaveSnapshot
    {
        public string Status { get; set; }
        public string CapturedAt { get; set; }
        public int HandlerCount { get; set; }
        public int ReadyHandlerCount { get; set; }
        public int BlockedHandlerCount { get; set; }
        public int QuarantinedHandlerCount { get; set; }
        public int DispatchCount { get; set; }
        public int InvocationCount { get; set; }
        public int FailureCount { get; set; }
        public int ReplayRecordCount { get; set; }
        public int ReplayAttemptCount { get; set; }
        public int ReplaySuccessCount { get; set; }
        public int ReplayRejectedCount { get; set; }
        public int BudgetExceededCount { get; set; }
        public double TotalMilliseconds { get; set; }
        public double MeanMilliseconds { get; set; }
        public double MaxMilliseconds { get; set; }
        public List<string> Notes { get; set; } = new List<string>();
        public List<Finding> Findings { get; set; } = new List<Finding>();
        public List<ForgeWeaveHandlerHealth> Handlers { get; set; } = new List<ForgeWeaveHandlerHealth>();
        public List<ForgeWeaveEventHealth> Events { get; set; } = new List<ForgeWeaveEventHealth>();
        public List<ForgeWeaveDispatchRecord> RecentDispatches { get; set; } = new List<ForgeWeaveDispatchRecord>();
        public List<ForgeReplayRecord> ReplayRecords { get; set; } = new List<ForgeReplayRecord>();
        public List<ForgeReplayResult> RecentReplays { get; set; } = new List<ForgeReplayResult>();
    }
}
