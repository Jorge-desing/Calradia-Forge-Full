using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CalradiaForge.Sdk;

namespace CalradiaForge.Core
{
    // ForgeWeave receives official host callbacks and dispatches them to registered extensions.
    // It does not inspect, replace, or patch any game method.
    public sealed partial class ForgeWeaveEngine
    {
        const int MaximumHandlers=256;
        const int MaximumOrderReferences=32;
        const int MaximumJournalEntries=64;
        const int MaximumReplayEntries=64;
        const int MaximumFindings=128;
        readonly object gate=new object();
        readonly Dictionary<string,RegisteredHandler> handlers=new Dictionary<string,RegisteredHandler>(StringComparer.OrdinalIgnoreCase);
        // Non-custom event plans depend only on the registered handler set. Custom plans
        // depend on the caller's topic, so keep them per-dispatch instead of caching topics.
        readonly Dictionary<ForgeEventKind,DispatchPlan> dispatchPlans=new Dictionary<ForgeEventKind,DispatchPlan>();
        readonly Dictionary<ForgeEventKind,EventState> events=new Dictionary<ForgeEventKind,EventState>();
        readonly Queue<ForgeWeaveDispatchRecord> journal=new Queue<ForgeWeaveDispatchRecord>();
        // The host retains original official events separately from the event journal. Replayed
        // dispatches can be audited in the journal but never become another replay source.
        readonly Queue<ForgeReplayRecord> replayRecords=new Queue<ForgeReplayRecord>();
        readonly Queue<ForgeReplayResult> replayJournal=new Queue<ForgeReplayResult>();
        long sequence;
        int dispatchCount,invocationCount,failureCount,budgetExceededCount;
        int replayAttemptCount,replaySuccessCount,replayRejectedCount;
        double totalMilliseconds,maxMilliseconds;

        public int Count { get { lock(gate)return handlers.Count; } }
        public bool HasSubscription(ForgeEventKind eventKind)
        {
            return HasSubscription(eventKind, null);
        }
        public bool HasSubscription(ForgeEventKind eventKind, string topic)
        {
            lock (gate)
            {
                foreach (var handler in handlers.Values)
                {
                    if (handler.Subscription.Event == eventKind)
                    {
                        if (eventKind != ForgeEventKind.Custom || string.IsNullOrEmpty(topic) || MatchesTopic(handler.Subscription.Topic, topic))
                            return true;
                    }
                }
                return false;
            }
        }
        public IEnumerable<ForgeEventSubscription> Subscriptions
        {
            get
            {
                lock (gate)
                {
                    var count = handlers.Count;
                    if (count == 0) return Array.Empty<ForgeEventSubscription>();
                    var array = new ForgeEventSubscription[count];
                    int i = 0;
                    foreach (var handler in handlers.Values)
                    {
                        array[i++] = Copy(handler.Subscription);
                    }
                    Array.Sort(array, (a, b) => string.Compare(a.Descriptor.Id, b.Descriptor.Id, StringComparison.OrdinalIgnoreCase));
                    return array;
                }
            }
        }
        public IReadOnlyList<ForgeReplayRecord> ReplayRecords
        {
            get
            {
                lock(gate)
                {
                    var count = replayRecords.Count;
                    var array = new ForgeReplayRecord[count];
                    int i = 0;
                    foreach (var record in replayRecords)
                    {
                        array[i++] = Copy(record);
                    }
                    return array;
                }
            }
        }

        // TestEngine validates the descriptor and reserves its global ID before calling this.
        internal void Register(IForgeEventHandler handler,Descriptor descriptor)
        {
            if(handler==null)throw new ArgumentNullException(nameof(handler));
            if(descriptor==null)throw new ArgumentNullException(nameof(descriptor));
            var subscription=ValidateAndCopy(handler.Subscription,descriptor);
            lock(gate)
            {
                if(handlers.Count>=MaximumHandlers)throw new InvalidOperationException("ForgeWeave supports at most "+MaximumHandlers+" event handlers per session.");
                if(handlers.ContainsKey(subscription.Descriptor.Id))throw new ArgumentException("Duplicate ForgeWeave subscription ID: "+subscription.Descriptor.Id);
                handlers.Add(subscription.Descriptor.Id,new RegisteredHandler(handler,subscription));
                dispatchPlans.Clear();
            }
        }

        public bool Unregister(string id)
        {
            if(string.IsNullOrWhiteSpace(id))return false;
            lock(gate)
            {
                if(!handlers.Remove(id))return false;
                dispatchPlans.Clear();
                return true;
            }
        }

        public bool Unregister(IForgeEventHandler handler)
        {
            if(handler?.Subscription?.Descriptor?.Id==null)return false;
            return Unregister(handler.Subscription.Descriptor.Id);
        }

        internal ForgeWeaveDispatchResult Dispatch(ForgeEventKind kind,Context context,ITestServices services,double deltaMilliseconds,IDictionary<string,string> data,CancellationToken token,Func<ForgeEventSubscription,ITestServices,Context,string> authorize)
        {
            return Dispatch(kind,context,services,deltaMilliseconds,data,token,authorize,false,null,null);
        }
        internal ForgeWeaveDispatchResult DispatchCustom(string topic,Context context,ITestServices services,double deltaMilliseconds,IDictionary<string,string> data,CancellationToken token,Func<ForgeEventSubscription,ITestServices,Context,string> authorize)
        {
            return Dispatch(ForgeEventKind.Custom,context,services,deltaMilliseconds,data,token,authorize,false,null,topic);
        }
        ForgeWeaveDispatchResult Dispatch(ForgeEventKind kind,Context context,ITestServices services,double deltaMilliseconds,IDictionary<string,string> data,CancellationToken token,Func<ForgeEventSubscription,ITestServices,Context,string> authorize,bool isReplay,long? sourceSequence,string topic = null)
        {
            if(services==null)throw new ArgumentNullException(nameof(services));
            if(!Enum.IsDefined(typeof(ForgeEventKind),kind))throw new ArgumentException("Invalid ForgeWeave event.",nameof(kind));
            if(authorize==null)throw new ArgumentNullException(nameof(authorize));
            var started=Stopwatch.StartNew();
            DispatchPlan plan;
            ForgeEvent invocation;
            lock(gate)
            {
                if(kind==ForgeEventKind.Custom)
                {
                    // Topic sets are open-ended; do not retain a plan per published topic.
                    plan=BuildPlanLocked(kind,topic);
                }
                else if(!dispatchPlans.TryGetValue(kind,out plan))
                {
                    plan=BuildPlanLocked(kind);
                    dispatchPlans.Add(kind,plan);
                }
                invocation=new ForgeEvent(kind,context,++sequence,deltaMilliseconds,data,token,isReplay,sourceSequence,topic);
            }
            var result=new ForgeWeaveDispatchResult {Event=kind,Sequence=invocation.Sequence,IsReplay=isReplay,SourceSequence=sourceSequence};
            foreach(var finding in plan.Findings)AddFinding(result.Findings,Copy(finding));
            result.SkippedCount=plan.Blocked.Count;
            foreach(var blocked in plan.Blocked) {SetLastOutcome(blocked.Key,"Skipped: "+blocked.Value);AddOutcome(result,blocked.Key,"Blocked",blocked.Value,0);}

            var cancelled=token.IsCancellationRequested;
            var ordered=plan.Ordered;
            var visited=0;
            for(var index=0;index<ordered.Count && !cancelled;index++)
            {
                visited=index+1;
                var registered=ordered[index];
                if(invocation.PropagationStopped)
                {
                    result.SkippedCount+=ordered.Count-index;
                    for(var remaining=index;remaining<ordered.Count;remaining++) {SetLastOutcome(ordered[remaining],"Skipped: propagation stopped.");AddOutcome(result,ordered[remaining],"Skipped","Propagation stopped.",0);}
                    break;
                }
                bool isProbe;
                if(IsQuarantinedOrInCooldown(registered,out isProbe))
                {
                    result.SkippedCount++;result.QuarantinedCount++;
                    SetLastOutcome(registered,"Skipped: handler is quarantined (Circuit: "+registered.CircuitState+").");
                    AddOutcome(result,registered,"Quarantined","Handler is quarantined (Circuit: "+registered.CircuitState+").",0);
                    continue;
                }
                var replayRejection=ReplayRejection(invocation,registered.Subscription);
                if(!string.IsNullOrWhiteSpace(replayRejection))
                {
                    result.SkippedCount++;
                    SetLastOutcome(registered,"Skipped: "+Bound(replayRejection,512));
                    AddOutcome(result,registered,"Skipped",replayRejection,0);
                    continue;
                }
                if(!MatchesFilter(registered.Subscription.Filter,invocation.Data))
                {
                    result.SkippedCount++;
                    SetLastOutcome(registered,"Skipped: event filter did not match.");
                    AddOutcome(result,registered,"Filtered","Required event data did not match the handler filter.",0);
                    continue;
                }
                string rejection;
                try {rejection=authorize(registered.Subscription,services,invocation.Context);}
                catch(Exception error) {rejection="Authorization failed: "+Describe(error);}
                if(!string.IsNullOrWhiteSpace(rejection))
                {
                    result.SkippedCount++;
                    SetLastOutcome(registered,"Skipped: "+Bound(rejection,512));
                    AddOutcome(result,registered,"Rejected",rejection,0);
                    continue;
                }
                double remainingMilliseconds;
                if(!TryBeginPulse(registered,out remainingMilliseconds))
                {
                    result.SkippedCount++;
                    SetLastOutcome(registered,"Skipped: Pulse interval; due in "+Math.Ceiling(remainingMilliseconds)+" ms.");
                    AddOutcome(result,registered,"Skipped","Pulse interval; due in "+Math.Ceiling(remainingMilliseconds)+" ms.",0);
                    continue;
                }
                var handlerTimer=Stopwatch.StartNew();
                try
                {
                    token.ThrowIfCancellationRequested();
                    registered.Handler.Handle(invocation);
                    handlerTimer.Stop();
                    var milliseconds=handlerTimer.Elapsed.TotalMilliseconds;
                    var overBudget=IsOverBudget(registered,milliseconds);
                    MarkSucceeded(registered,milliseconds,overBudget,isProbe);
                    AddOutcome(result,registered,overBudget?"OverBudget":"Completed",overBudget?"Execution exceeded the declared budget of "+registered.Subscription.BudgetMilliseconds+" ms.":null,milliseconds,overBudget);
                    if(overBudget)result.BudgetExceededCount++;
                    result.InvokedCount++;
                }
                catch(OperationCanceledException) when(token.IsCancellationRequested)
                {
                    handlerTimer.Stop();
                    MarkCancelled(registered,handlerTimer.Elapsed.TotalMilliseconds);
                    AddOutcome(result,registered,"Cancelled","Cancellation requested.",handlerTimer.Elapsed.TotalMilliseconds);
                    cancelled=true;
                }
                catch(Exception error)
                {
                    handlerTimer.Stop();
                    var quarantined=MarkFailed(registered,handlerTimer.Elapsed.TotalMilliseconds,error,isProbe);
                    result.InvokedCount++;result.FailureCount++;
                    if(quarantined)result.QuarantinedCount++;
                    AddOutcome(result,registered,"Failed",Describe(error),handlerTimer.Elapsed.TotalMilliseconds);
                    AddFinding(result.Findings,new Finding {
                        Level="Warning",Code="forgeweave_handler_error",Module=registered.Subscription.Descriptor.Module,
                        Message="ForgeWeave handler '"+registered.Subscription.Descriptor.Id+"' failed: "+Describe(error),
                        Suggestion=quarantined?"The handler was quarantined after repeated failures. Fix it, then restart the session or reset the host state.":"The failure was isolated. Review the extension error before the next event."
                    });
                    SafeRegister(services,registered.Subscription.Descriptor.Module,"Warning","ForgeWeave handler '"+registered.Subscription.Descriptor.Id+"' failed: "+Describe(error));
                }
            }
            if(cancelled)
            {
                var untouched=Math.Max(0,ordered.Count-visited);
                if(untouched>0)result.SkippedCount+=untouched;
                result.Status="Cancelled";
            }
            else if(ordered.Count==0 && plan.Blocked.Count==0)result.Status="No handlers registered for "+kind+".";
            else if(invocation.PropagationStopped)result.Status="Propagation stopped after "+result.InvokedCount+" handler(s).";
            else if(result.FailureCount>0)result.Status="Completed with "+result.FailureCount+" isolated handler failure(s).";
            else result.Status="Completed";
            result.PropagationStopped=invocation.PropagationStopped;result.StopReason=invocation.StopReason;
            started.Stop();result.Milliseconds=started.Elapsed.TotalMilliseconds;
            RecordDispatch(invocation,result);
            return result;
        }

        // Only a retained original sequence can be replayed. The caller supplies the live host
        // services so the same context and writer gates used for an ordinary event remain in
        // force. The copied record is never accepted as an input object.
        internal ForgeReplayResult Replay(long sourceSequence,ITestServices services,CancellationToken token,Func<ForgeEventSubscription,ITestServices,Context,string> authorize)
        {
            if(services==null)throw new ArgumentNullException(nameof(services));
            if(authorize==null)throw new ArgumentNullException(nameof(authorize));
            ForgeReplayRecord source;
            lock(gate)
            {
                source=replayRecords.FirstOrDefault(record=>record.Sequence==sourceSequence);
                source=source==null?null:Copy(source);
            }
            if(source==null)return RejectReplay(sourceSequence,null,services,"The retained replay sequence is no longer available.");
            if(token.IsCancellationRequested)return CancelReplay(source,services,"Replay was cancelled before dispatch.");
            if(source.Context!=services.CurrentContext)
                return RejectReplay(sourceSequence,source,services,"Replay requires current context "+source.Context+", but the host is in "+services.CurrentContext+".");

            var dispatch=Dispatch(source.Event,source.Context,services,source.DeltaMilliseconds,source.Data,token,authorize,true,source.Sequence);
            var result=new ForgeReplayResult {
                SourceSequence=source.Sequence,ReplaySequence=dispatch.Sequence,Event=source.Event,Context=source.Context,
                Replayed=true,Status=dispatch.Status,InvokedCount=dispatch.InvokedCount,SkippedCount=dispatch.SkippedCount,
                FailureCount=dispatch.FailureCount,BudgetExceededCount=dispatch.BudgetExceededCount,QuarantinedCount=dispatch.QuarantinedCount,PropagationStopped=dispatch.PropagationStopped,
                StopReason=dispatch.StopReason,Milliseconds=dispatch.Milliseconds,HandlerOutcomes=CopyOutcomes(dispatch.HandlerOutcomes)
            };
            RecordReplayAttempt(result);
            return result;
        }

        ForgeReplayResult RejectReplay(long sourceSequence,ForgeReplayRecord source,ITestServices services,string reason)
        {
            var result=new ForgeReplayResult {
                SourceSequence=sourceSequence,Event=source==null?ForgeEventKind.ForgeReady:source.Event,
                Context=source!=null?source.Context:(services!=null?services.CurrentContext:Context.Any),Replayed=false,Status="Rejected",RejectionReason=Bound(reason,512)
            };
            RecordReplayAttempt(result);
            RecordReplayRejection(result);
            return result;
        }

        ForgeReplayResult CancelReplay(ForgeReplayRecord source,ITestServices services,string reason)
        {
            var result=new ForgeReplayResult {
                SourceSequence=source.Sequence,Event=source.Event,Context=source!=null?source.Context:(services!=null?services.CurrentContext:Context.Any),Replayed=false,Status="Cancelled",RejectionReason=Bound(reason,512)
            };
            RecordReplayAttempt(result);
            RecordReplayRejection(result);
            return result;
        }

        public ForgeWeaveSnapshot Snapshot()
        {
            lock(gate)
            {
                var planning=new List<Finding>();
                foreach(var kind in handlers.Values.Select(value=>value.Subscription.Event).Distinct().OrderBy(value=>(int)value))
                {
                    DispatchPlan plan;
                    if(kind==ForgeEventKind.Custom || !dispatchPlans.TryGetValue(kind,out plan))
                    {
                        plan=BuildPlanLocked(kind);
                        if(kind!=ForgeEventKind.Custom)dispatchPlans.Add(kind,plan);
                    }
                    foreach(var finding in plan.Findings)AddFinding(planning,Copy(finding));
                }
                var snapshot=new ForgeWeaveSnapshot {
                    CapturedAt=DateTime.UtcNow.ToString("O"),HandlerCount=handlers.Count,DispatchCount=dispatchCount,
                    InvocationCount=invocationCount,FailureCount=failureCount,TotalMilliseconds=totalMilliseconds,
                    MeanMilliseconds=dispatchCount==0?0:totalMilliseconds/dispatchCount,MaxMilliseconds=maxMilliseconds,BudgetExceededCount=budgetExceededCount,
                    ReplayRecordCount=replayRecords.Count,ReplayAttemptCount=replayAttemptCount,ReplaySuccessCount=replaySuccessCount,ReplayRejectedCount=replayRejectedCount
                };
                foreach(var registered in handlers.Values.OrderBy(value=>value.Subscription.Descriptor.Id,StringComparer.OrdinalIgnoreCase))
                {
                    var health=Health(registered);snapshot.Handlers.Add(health);
                    if(registered.Quarantined)snapshot.QuarantinedHandlerCount++;
                    else if(!string.IsNullOrWhiteSpace(registered.BlockingReason))snapshot.BlockedHandlerCount++;
                    else snapshot.ReadyHandlerCount++;
                }
                foreach(var state in events.OrderBy(pair=>(int)pair.Key).Select(pair=>pair.Value))snapshot.Events.Add(Copy(state));
                foreach(var item in journal)snapshot.RecentDispatches.Add(Copy(item));
                foreach(var item in replayRecords)snapshot.ReplayRecords.Add(Copy(item));
                foreach(var item in replayJournal)snapshot.RecentReplays.Add(Copy(item));
                snapshot.Findings=planning;
                snapshot.Notes.Add("ForgeWeave dispatches only events explicitly raised by the host; it does not patch game methods.");
                snapshot.Notes.Add("Before/After only refine equal-priority handlers. Missing, cross-event, cross-priority, and cyclic declarations are blocked instead of guessed.");
                snapshot.Notes.Add("A handler is quarantined after its declared bounded failure limit; other handlers continue to receive the event.");
                snapshot.Notes.Add("Replay records contain copied scalar host data only. Replays name a retained sequence, require the same current context, and never become replay sources.");
                snapshot.Notes.Add("Replay is disabled per handler by default. ObserveOnly handlers remain read-only; Live handlers still require the host's normal writer authorization.");
                snapshot.Notes.Add("Handlers may declare bounded exact-match scalar filters; a non-matching event is recorded as Filtered and never invokes the handler.");
                snapshot.Notes.Add("Handlers may declare a short execution budget. Warn records an OverBudget outcome without aborting or quarantining user code; zero disables the budget.");
                snapshot.Status=Status(snapshot);
                return snapshot;
            }
        }

        // A host can make a repaired extension eligible again without reconstructing all
        // registrations. The identity is still fixed; only failure health is reset.
        public bool ResetQuarantine(string id)
        {
            if(string.IsNullOrWhiteSpace(id))return false;
            lock(gate)
            {
                RegisteredHandler handler;
                if(!handlers.TryGetValue(id,out handler) || !handler.Quarantined)return false;
                handler.Quarantined=false;
                handler.CircuitState="Closed";
                handler.ConsecutiveFailureCount=0;
                handler.CurrentCooldownSeconds=Math.Max(1,handler.Subscription.CircuitBreakerCooldownSeconds);
                handler.LastStateChangeTimestamp=Stopwatch.GetTimestamp();
                handler.LastError=null;
                handler.LastOutcome="Quarantine reset by host.";
                return true;
            }
        }

        public int ResetAllQuarantines()
        {
            lock(gate)
            {
                int count=0;
                foreach(var handler in handlers.Values)
                {
                    if(handler.Quarantined)
                    {
                        handler.Quarantined=false;
                        handler.CircuitState="Closed";
                        handler.ConsecutiveFailureCount=0;
                        handler.CurrentCooldownSeconds=Math.Max(1,handler.Subscription.CircuitBreakerCooldownSeconds);
                        handler.LastStateChangeTimestamp=Stopwatch.GetTimestamp();
                        handler.LastError=null;
                        handler.LastOutcome="Quarantine reset by host.";
                        count++;
                    }
                }
                return count;
            }
        }

        public int ResetModuleQuarantines(string module)
        {
            if(string.IsNullOrWhiteSpace(module))return 0;
            lock(gate)
            {
                int count=0;
                foreach(var handler in handlers.Values)
                {
                    if(handler.Quarantined && string.Equals(handler.Subscription.Descriptor.Module,module,StringComparison.OrdinalIgnoreCase))
                    {
                        handler.Quarantined=false;
                        handler.CircuitState="Closed";
                        handler.ConsecutiveFailureCount=0;
                        handler.CurrentCooldownSeconds=Math.Max(1,handler.Subscription.CircuitBreakerCooldownSeconds);
                        handler.LastStateChangeTimestamp=Stopwatch.GetTimestamp();
                        handler.LastError=null;
                        handler.LastOutcome="Quarantine reset by host for module "+module+".";
                        count++;
                    }
                }
                return count;
            }
        }

        public ForgeWeaveHandlerHealth GetHandlerHealth(string id)
        {
            if(string.IsNullOrWhiteSpace(id))return null;
            lock(gate)
            {
                RegisteredHandler handler;
                return handlers.TryGetValue(id,out handler)?Health(handler):null;
            }
        }

        public void ClearJournal()
        {
            lock(gate)journal.Clear();
        }

        public void ClearReplays()
        {
            lock(gate)
            {
                replayRecords.Clear();
                replayJournal.Clear();
            }
        }

        internal static bool MatchesTopic(string pattern, string actual)
        {
            if (string.IsNullOrEmpty(pattern) || pattern == "*") return true;
            if (actual == null) return false;
            if (pattern.EndsWith("*"))
            {
                var prefix = pattern.Substring(0, pattern.Length - 1);
                return actual.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }
            return string.Equals(pattern, actual, StringComparison.OrdinalIgnoreCase);
        }

        DispatchPlan BuildPlanLocked(ForgeEventKind kind, string topic = null)
        {
            var plan=new DispatchPlan();
            var matched=handlers.Values
                .Where(value=>value.Subscription.Event==kind && (kind!=ForgeEventKind.Custom || MatchesTopic(value.Subscription.Topic,topic)))
                .OrderBy(value=>value.Subscription.Descriptor.Id,StringComparer.OrdinalIgnoreCase).ToList();
            var byId=matched.ToDictionary(value=>value.Subscription.Descriptor.Id,StringComparer.OrdinalIgnoreCase);
            var edges=new List<OrderEdge>();
            foreach(var handler in matched)
            {
                foreach(var targetId in handler.Subscription.Before)ValidateOrderReference(plan,byId,handler,targetId,true,edges);
                foreach(var targetId in handler.Subscription.After)ValidateOrderReference(plan,byId,handler,targetId,false,edges);
            }
            PropagateBlockedEdges(plan,edges);
            foreach(var group in matched.GroupBy(value=>value.Subscription.Priority).OrderByDescending(group=>group.Key))
            {
                var candidates=group.Where(value=>!plan.Blocked.ContainsKey(value)).ToList();
                var groupEdges=edges.Where(edge=>edge.From.Subscription.Priority==group.Key && edge.To.Subscription.Priority==group.Key && !plan.Blocked.ContainsKey(edge.From) && !plan.Blocked.ContainsKey(edge.To)).ToList();
                var ordered=TopologicalOrder(candidates,groupEdges);
                if(ordered.Count!=candidates.Count)
                {
                    // Keep independently ordered handlers eligible. Only the unresolved cycle
                    // and nodes waiting on it are blocked below.
                    plan.Ordered.AddRange(ordered);
                    var unresolved=candidates.Where(value=>!ordered.Contains(value)).ToList();
                    foreach(var handler in unresolved)Block(plan,handler,"forgeweave_order_cycle","Cyclic ordering declarations prevent a deterministic dispatch order.","Remove a Before/After cycle among equal-priority handler IDs.");
                }
                else plan.Ordered.AddRange(ordered);
            }
            // Nodes downstream of a cycle are just as indeterminate. Block them too rather
            // than moving them ahead of an unresolved prerequisite.
            PropagateBlockedEdges(plan,edges);
            plan.Ordered=plan.Ordered.Where(value=>!plan.Blocked.ContainsKey(value)).ToList();
            foreach(var handler in matched)
            {
                string reason;
                if(plan.Blocked.TryGetValue(handler,out reason))handler.BlockingReason=reason;
                else handler.BlockingReason=null;
            }
            return plan;
        }

        void ValidateOrderReference(DispatchPlan plan,Dictionary<string,RegisteredHandler> byId,RegisteredHandler source,string targetId,bool before,List<OrderEdge> edges)
        {
            if(string.IsNullOrWhiteSpace(targetId))
            {
                Block(plan,source,"forgeweave_order_invalid","A Before/After entry is empty.","Use a registered ForgeWeave handler ID or remove the entry.");
                return;
            }
            RegisteredHandler target;
            if(!byId.TryGetValue(targetId,out target))
            {
                RegisteredHandler any;
                var exists=handlers.TryGetValue(targetId,out any);
                var message=!exists
                    ? "Ordering target '"+targetId+"' is not registered for this event."
                    : "Ordering target '"+targetId+"' is registered for "+any.Subscription.Event+", not "+source.Subscription.Event+".";
                Block(plan,source,"forgeweave_order_missing",message,"Register the target for the same event or remove the ordering declaration.");
                return;
            }
            if(ReferenceEquals(source,target))
            {
                Block(plan,source,"forgeweave_order_self","A handler cannot order itself before or after itself.","Remove the self reference.");
                return;
            }
            if(source.Subscription.Priority!=target.Subscription.Priority)
            {
                Block(plan,source,"forgeweave_order_cross_priority","Ordering target '"+targetId+"' has a different priority.","Use the same priority when Before/After must determine the relative order.");
                return;
            }
            edges.Add(before?new OrderEdge(source,target):new OrderEdge(target,source));
        }

        static void PropagateBlockedEdges(DispatchPlan plan,IEnumerable<OrderEdge> edges)
        {
            var changed=true;
            while(changed)
            {
                changed=false;
                foreach(var edge in edges)
                {
                    if(plan.Blocked.ContainsKey(edge.From) && !plan.Blocked.ContainsKey(edge.To)) {Block(plan,edge.To,"forgeweave_order_dependency_blocked","Ordering dependency '"+edge.From.Subscription.Descriptor.Id+"' is blocked.","Resolve the dependency's ordering declaration, then run the event again.");changed=true;}
                    if(plan.Blocked.ContainsKey(edge.To) && !plan.Blocked.ContainsKey(edge.From)) {Block(plan,edge.From,"forgeweave_order_dependency_blocked","Ordering dependency '"+edge.To.Subscription.Descriptor.Id+"' is blocked.","Resolve the dependency's ordering declaration, then run the event again.");changed=true;}
                }
            }
        }

        static List<RegisteredHandler> TopologicalOrder(List<RegisteredHandler> values,List<OrderEdge> edges)
        {
            var indegree=values.ToDictionary(value=>value,value=>0);
            var outgoing=values.ToDictionary(value=>value,value=>new List<RegisteredHandler>());
            foreach(var edge in edges)
            {
                if(!indegree.ContainsKey(edge.From) || !indegree.ContainsKey(edge.To))continue;
                if(outgoing[edge.From].Contains(edge.To))continue;
                outgoing[edge.From].Add(edge.To);indegree[edge.To]++;
            }
            var ready=new List<RegisteredHandler>(values.Count);
            foreach(var value in values)
                if(indegree[value]==0)PushReady(ready,value);
            var output=new List<RegisteredHandler>();
            while(ready.Count>0)
            {
                var next=PopReady(ready);output.Add(next);
                foreach(var dependent in outgoing[next])
                {
                    indegree[dependent]--;
                    if(indegree[dependent]==0)
                    {
                        PushReady(ready,dependent);
                    }
                }
            }
            return output;
        }

        static void PushReady(List<RegisteredHandler> heap,RegisteredHandler value)
        {
            var index=heap.Count;
            heap.Add(value);
            while(index>0)
            {
                var parent=(index-1)/2;
                if(CompareHandlerIds(heap[parent],value)<=0)break;
                heap[index]=heap[parent];
                index=parent;
            }
            heap[index]=value;
        }

        static RegisteredHandler PopReady(List<RegisteredHandler> heap)
        {
            var first=heap[0];
            var lastIndex=heap.Count-1;
            var last=heap[lastIndex];
            heap.RemoveAt(lastIndex);
            if(heap.Count==0)return first;

            var index=0;
            while(true)
            {
                var left=(index*2)+1;
                if(left>=heap.Count)break;
                var right=left+1;
                var child=right<heap.Count && CompareHandlerIds(heap[right],heap[left])<0?right:left;
                if(CompareHandlerIds(last,heap[child])<=0)break;
                heap[index]=heap[child];
                index=child;
            }
            heap[index]=last;
            return first;
        }

        static int CompareHandlerIds(RegisteredHandler left,RegisteredHandler right)
        {
            return string.Compare(left.Subscription.Descriptor.Id,right.Subscription.Descriptor.Id,StringComparison.OrdinalIgnoreCase);
        }

        static void Block(DispatchPlan plan,RegisteredHandler handler,string code,string message,string suggestion)
        {
            if(plan.Blocked.ContainsKey(handler))return;
            plan.Blocked.Add(handler,message);
            AddFinding(plan.Findings,new Finding {Level="Warning",Code=code,Module=handler.Subscription.Descriptor.Module,Message="Handler '"+handler.Subscription.Descriptor.Id+"': "+message,Suggestion=suggestion});
        }

        static ForgeEventSubscription ValidateAndCopy(ForgeEventSubscription source,Descriptor descriptor)
        {
            if(source==null)throw new ArgumentException("ForgeWeave subscription required.");
            if(!Enum.IsDefined(typeof(ForgeEventKind),source.Event))throw new ArgumentException("Invalid ForgeWeave event.");
            if(!Enum.IsDefined(typeof(ForgeEventAccess),source.Access))throw new ArgumentException("Invalid ForgeWeave access level.");
            if(!Enum.IsDefined(typeof(ForgeReplayMode),source.ReplayMode))throw new ArgumentException("Invalid ForgeWeave replay mode.");
            if(source.Priority<-10000 || source.Priority>10000)throw new ArgumentException("ForgeWeave priority must be between -10000 and 10000.");
            if(source.FailureLimit<1 || source.FailureLimit>5)throw new ArgumentException("ForgeWeave failure limit must be between 1 and 5.");
            if(source.MinimumIntervalMilliseconds<0 || source.MinimumIntervalMilliseconds>60000)throw new ArgumentException("ForgeWeave interval must be between 0 and 60000 milliseconds.");
            if(source.Event==ForgeEventKind.Pulse && source.MinimumIntervalMilliseconds<250)throw new ArgumentException("Pulse handlers require a minimum interval of 250 milliseconds.");
            if(source.Event!=ForgeEventKind.Pulse && source.MinimumIntervalMilliseconds!=0)throw new ArgumentException("Only Pulse handlers can declare a minimum interval.");
            if((source.Access==ForgeEventAccess.Observe || source.Access==ForgeEventAccess.Diagnostics) && descriptor.ChangesState)throw new ArgumentException("Observe and Diagnostics handlers must be read-only.");
            if(source.Access==ForgeEventAccess.CampaignWrite && (!descriptor.ChangesState || descriptor.Context!=Context.Campaign))throw new ArgumentException("CampaignWrite handlers require ChangesState and Campaign context.");
            if(source.Access==ForgeEventAccess.MissionWrite && (!descriptor.ChangesState || descriptor.Context!=Context.Mission))throw new ArgumentException("MissionWrite handlers require ChangesState and Mission context.");
            if(source.ReplayMode==ForgeReplayMode.ObserveOnly && (descriptor.ChangesState || source.Access==ForgeEventAccess.CampaignWrite || source.Access==ForgeEventAccess.MissionWrite))throw new ArgumentException("ObserveOnly replay handlers must be read-only.");
            if(!Enum.IsDefined(typeof(ForgeBudgetPolicy),source.BudgetPolicy))throw new ArgumentException("Invalid ForgeWeave budget policy.");
            if(source.BudgetMilliseconds<0 || source.BudgetMilliseconds>5000)throw new ArgumentException("ForgeWeave execution budgets must be between 0 and 5000 milliseconds.");
            if(!Enum.IsDefined(typeof(ForgeCircuitBreakerPolicy),source.CircuitBreakerPolicy))throw new ArgumentException("Invalid ForgeWeave circuit breaker policy.");
            var cooldown=source.CircuitBreakerCooldownSeconds<=0?5:source.CircuitBreakerCooldownSeconds;
            if(cooldown<1 || cooldown>300)throw new ArgumentException("ForgeWeave circuit breaker cooldown must be between 1 and 300 seconds.");
            var topic=Bound((source.Topic??"").Trim(),128);
            var filter=CopyFilter(source.Filter);
            return new ForgeEventSubscription {
                Descriptor=Copy(descriptor),Event=source.Event,Topic=topic,Priority=source.Priority,Before=CopyIds(source.Before),After=CopyIds(source.After),Access=source.Access,ReplayMode=source.ReplayMode,Filter=filter,BudgetMilliseconds=source.BudgetMilliseconds,BudgetPolicy=source.BudgetPolicy,FailureLimit=source.FailureLimit,CircuitBreakerPolicy=source.CircuitBreakerPolicy,CircuitBreakerCooldownSeconds=cooldown,MinimumIntervalMilliseconds=source.MinimumIntervalMilliseconds
            };
        }

        static ForgeEventFilter CopyFilter(ForgeEventFilter source)
        {
            var filter=source??new ForgeEventFilter();
            var values=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var pair in filter.RequiredData??new Dictionary<string,string>())
            {
                if(values.Count>=8)throw new ArgumentException("ForgeWeave filters support at most 8 required data pairs.");
                var key=(pair.Key??"").Trim();
                if(key.Length==0 || key.Length>64)throw new ArgumentException("ForgeWeave filter keys must contain between 1 and 64 characters.");
                if(pair.Value!=null && pair.Value.Length>512)throw new ArgumentException("ForgeWeave filter values cannot exceed 512 characters.");
                if(values.ContainsKey(key))throw new ArgumentException("ForgeWeave filters cannot repeat a required data key: "+key);
                values.Add(key,pair.Value??"");
            }
            var excluded=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var pair in filter.ExcludedData??new Dictionary<string,string>())
            {
                if(excluded.Count>=8)throw new ArgumentException("ForgeWeave filters support at most 8 excluded data pairs.");
                var key=(pair.Key??"").Trim();
                if(key.Length==0 || key.Length>64)throw new ArgumentException("ForgeWeave filter keys must contain between 1 and 64 characters.");
                if(pair.Value!=null && pair.Value.Length>512)throw new ArgumentException("ForgeWeave filter values cannot exceed 512 characters.");
                if(excluded.ContainsKey(key))throw new ArgumentException("ForgeWeave filters cannot repeat an excluded data key: "+key);
                excluded.Add(key,pair.Value??"");
            }
            return new ForgeEventFilter {RequiredData=values,ExcludedData=excluded};
        }

        static bool MatchesFilter(ForgeEventFilter filter,IReadOnlyDictionary<string,string> data)
        {
            if (filter == null) return true;
            foreach(var pair in filter.RequiredData??new Dictionary<string,string>())
            {
                string actual;
                if(data==null || !data.TryGetValue(pair.Key,out actual) || !string.Equals(actual,pair.Value,StringComparison.Ordinal))return false;
            }
            if (filter.ExcludedData != null && filter.ExcludedData.Count > 0 && data != null)
            {
                foreach(var pair in filter.ExcludedData)
                {
                    string actual;
                    if(data.TryGetValue(pair.Key,out actual) && string.Equals(actual,pair.Value,StringComparison.Ordinal))return false;
                }
            }
            return true;
        }

        static List<string> CopyIds(IEnumerable<string> source)
        {
            var values=new List<string>();
            foreach(var raw in source??Enumerable.Empty<string>())
            {
                if(values.Count>=MaximumOrderReferences)throw new ArgumentException("ForgeWeave supports at most "+MaximumOrderReferences+" Before/After references per handler.");
                var value=(raw??"").Trim();
                if(value.Length==0 || value.Length>128)throw new ArgumentException("ForgeWeave ordering IDs must contain between 1 and 128 characters.");
                if(!values.Contains(value,StringComparer.OrdinalIgnoreCase))values.Add(value);
            }
            return values;
        }

        bool IsQuarantined(RegisteredHandler handler) { lock(gate)return handler.Quarantined; }
        bool IsQuarantinedOrInCooldown(RegisteredHandler handler,out bool isProbe)
        {
            isProbe=false;
            lock(gate)
            {
                if(!handler.Quarantined)
                {
                    handler.CircuitState="Closed";
                    return false;
                }

                if(handler.Subscription.CircuitBreakerPolicy==ForgeCircuitBreakerPolicy.PermanentQuarantine)
                {
                    handler.CircuitState="Open";
                    return true;
                }

                var now=Stopwatch.GetTimestamp();
                var elapsedSeconds=(now-handler.LastStateChangeTimestamp)/(double)Stopwatch.Frequency;
                if(elapsedSeconds>=handler.CurrentCooldownSeconds)
                {
                    handler.CircuitState="HalfOpen";
                    handler.LastStateChangeTimestamp=now;
                    isProbe=true;
                    return false;
                }

                handler.CircuitState="Open";
                return true;
            }
        }
        bool TryBeginPulse(RegisteredHandler handler,out double remainingMilliseconds)
        {
            remainingMilliseconds=0;
            if(handler.Subscription.Event!=ForgeEventKind.Pulse)return true;
            lock(gate)
            {
                var now=Stopwatch.GetTimestamp();
                if(handler.LastPulseTimestamp!=0)
                {
                    var elapsed=(now-handler.LastPulseTimestamp)*1000.0/Stopwatch.Frequency;
                    if(elapsed<handler.Subscription.MinimumIntervalMilliseconds)
                    {
                        remainingMilliseconds=handler.Subscription.MinimumIntervalMilliseconds-elapsed;
                        return false;
                    }
                }
                handler.LastPulseTimestamp=now;
                return true;
            }
        }
        void SetLastOutcome(RegisteredHandler handler,string outcome) { lock(gate)handler.LastOutcome=Bound(outcome,512); }
        bool IsOverBudget(RegisteredHandler handler,double milliseconds)
        {
            return handler.Subscription.BudgetPolicy==ForgeBudgetPolicy.Warn && handler.Subscription.BudgetMilliseconds>0 && milliseconds>handler.Subscription.BudgetMilliseconds;
        }
        void MarkSucceeded(RegisteredHandler handler,double milliseconds,bool overBudget,bool isProbe=false)
        {
            lock(gate)
            {
                handler.InvocationCount++;handler.ConsecutiveFailureCount=0;handler.TotalMilliseconds+=milliseconds;handler.MaxMilliseconds=Math.Max(handler.MaxMilliseconds,milliseconds);
                handler.MinMilliseconds=handler.InvocationCount==1?milliseconds:Math.Min(handler.MinMilliseconds,milliseconds);
                handler.LastMilliseconds=milliseconds;handler.LastError=null;handler.LastInvokedAt=DateTime.UtcNow.ToString("O");handler.LastOutcome=overBudget?"OverBudget":(isProbe?"Probe succeeded; circuit closed.":"Completed");
                if(overBudget){handler.BudgetExceededCount++;handler.LastBudgetExceededAt=DateTime.UtcNow.ToString("O");}
                handler.RecordTiming(milliseconds);
                if(isProbe || handler.CircuitState=="HalfOpen")
                {
                    handler.Quarantined=false;
                    handler.CircuitState="Closed";
                    handler.CurrentCooldownSeconds=Math.Max(1,handler.Subscription.CircuitBreakerCooldownSeconds);
                    handler.LastStateChangeTimestamp=Stopwatch.GetTimestamp();
                }
            }
        }
        void MarkCancelled(RegisteredHandler handler,double milliseconds)
        {
            lock(gate)
            {
                handler.LastMilliseconds=milliseconds;handler.LastInvokedAt=DateTime.UtcNow.ToString("O");handler.LastOutcome="Cancelled";
                handler.RecordTiming(milliseconds);
            }
        }
        bool MarkFailed(RegisteredHandler handler,double milliseconds,Exception error,bool isProbe=false)
        {
            lock(gate)
            {
                handler.InvocationCount++;handler.FailureCount++;handler.ConsecutiveFailureCount++;handler.TotalMilliseconds+=milliseconds;handler.MaxMilliseconds=Math.Max(handler.MaxMilliseconds,milliseconds);
                handler.MinMilliseconds=handler.InvocationCount==1?milliseconds:Math.Min(handler.MinMilliseconds,milliseconds);
                handler.LastMilliseconds=milliseconds;handler.LastError=Describe(error);handler.LastInvokedAt=DateTime.UtcNow.ToString("O");
                handler.RecordTiming(milliseconds);
                if(isProbe || handler.CircuitState=="HalfOpen")
                {
                    handler.Quarantined=true;
                    handler.CircuitState="Open";
                    handler.CurrentCooldownSeconds=Math.Min(60,handler.CurrentCooldownSeconds*2);
                    handler.LastStateChangeTimestamp=Stopwatch.GetTimestamp();
                    handler.LastOutcome="Probe failed; circuit open (cooldown: "+handler.CurrentCooldownSeconds+"s).";
                }
                else if(handler.ConsecutiveFailureCount>=handler.Subscription.FailureLimit)
                {
                    handler.Quarantined=true;
                    handler.CircuitState="Open";
                    handler.CurrentCooldownSeconds=Math.Max(1,handler.Subscription.CircuitBreakerCooldownSeconds);
                    handler.LastStateChangeTimestamp=Stopwatch.GetTimestamp();
                    handler.LastOutcome="Failed; handler quarantined.";
                }
                else
                {
                    handler.LastOutcome="Failed; continuing other handlers.";
                }
                return handler.Quarantined;
            }
        }
        void RecordDispatch(ForgeEvent invocation,ForgeWeaveDispatchResult result)
        {
            lock(gate)
            {
                dispatchCount++;invocationCount+=result.InvokedCount;failureCount+=result.FailureCount;budgetExceededCount+=result.BudgetExceededCount;totalMilliseconds+=result.Milliseconds;maxMilliseconds=Math.Max(maxMilliseconds,result.Milliseconds);
                EventState state;
                if(!events.TryGetValue(invocation.Kind,out state)) {state=new EventState {Event=invocation.Kind};events.Add(invocation.Kind,state);}
                state.DispatchCount++;state.HandlerInvocationCount+=result.InvokedCount;state.FailureCount+=result.FailureCount;state.BudgetExceededCount+=result.BudgetExceededCount;state.SkippedCount+=result.SkippedCount;state.TotalMilliseconds+=result.Milliseconds;state.MaxMilliseconds=Math.Max(state.MaxMilliseconds,result.Milliseconds);
                state.MinMilliseconds=state.DispatchCount==1?result.Milliseconds:Math.Min(state.MinMilliseconds,result.Milliseconds);state.LastDispatchedAt=DateTime.UtcNow.ToString("O");
                var dispatchedAt=DateTime.UtcNow.ToString("O");
                journal.Enqueue(new ForgeWeaveDispatchRecord {
                    Sequence=result.Sequence,Event=invocation.Kind,Context=invocation.Context.ToString(),IsReplay=invocation.IsReplay,SourceSequence=invocation.SourceSequence,
                    Status=Bound(result.Status,512),RejectionReason=Bound(result.RejectionReason,512),InvokedCount=result.InvokedCount,SkippedCount=result.SkippedCount,
                    FailureCount=result.FailureCount,BudgetExceededCount=result.BudgetExceededCount,PropagationStopped=result.PropagationStopped,StopReason=Bound(result.StopReason,256),Milliseconds=result.Milliseconds,
                    DispatchedAt=dispatchedAt,HandlerOutcomes=CopyOutcomes(result.HandlerOutcomes)
                });
                while(journal.Count>MaximumJournalEntries)journal.Dequeue();
                if(!invocation.IsReplay)
                {
                    replayRecords.Enqueue(new ForgeReplayRecord {
                        Sequence=invocation.Sequence,Event=invocation.Kind,Context=invocation.Context,DeltaMilliseconds=invocation.DeltaMilliseconds,
                        RaisedAt=invocation.RaisedAt.ToString("O"),Data=CopyData(invocation.Data),OriginalStatus=Bound(result.Status,512),
                        OriginalInvokedCount=result.InvokedCount,OriginalSkippedCount=result.SkippedCount,OriginalFailureCount=result.FailureCount,
                        OriginalPropagationStopped=result.PropagationStopped,OriginalStopReason=Bound(result.StopReason,256),OriginalMilliseconds=result.Milliseconds,
                        DispatchedAt=dispatchedAt,OriginalHandlerOutcomes=CopyOutcomes(result.HandlerOutcomes)
                    });
                    while(replayRecords.Count>MaximumReplayEntries)replayRecords.Dequeue();
                }
            }
        }
        void RecordReplayAttempt(ForgeReplayResult result)
        {
            lock(gate)
            {
                replayAttemptCount++;
                if(result.Replayed)replaySuccessCount++;
                replayJournal.Enqueue(Copy(result));
                while(replayJournal.Count>MaximumReplayEntries)replayJournal.Dequeue();
            }
        }
        void RecordReplayRejection(ForgeReplayResult result)
        {
            lock(gate)replayRejectedCount++;
        }
        static string ReplayRejection(ForgeEvent invocation,ForgeEventSubscription subscription)
        {
            if(!invocation.IsReplay)return null;
            if(subscription.ReplayMode==ForgeReplayMode.Disabled)return "This handler did not opt in to replay.";
            if(subscription.ReplayMode==ForgeReplayMode.ObserveOnly && (subscription.Descriptor.ChangesState || subscription.Access==ForgeEventAccess.CampaignWrite || subscription.Access==ForgeEventAccess.MissionWrite))return "ObserveOnly replay cannot invoke a state-changing handler.";
            return null;
        }
        static void AddOutcome(ForgeWeaveDispatchResult result,RegisteredHandler handler,string status,string reason,double milliseconds,bool budgetExceeded=false)
        {
            if(result==null || handler==null || result.HandlerOutcomes.Count>=MaximumHandlers)return;
            result.HandlerOutcomes.Add(new ForgeHandlerOutcome {
                HandlerId=handler.Subscription.Descriptor.Id,Module=handler.Subscription.Descriptor.Module,ReplayMode=handler.Subscription.ReplayMode,
                Status=Bound(status,64),Reason=Bound(reason,512),Milliseconds=milliseconds<0?0:milliseconds,BudgetExceeded=budgetExceeded
            });
        }
        static void SafeRegister(ITestServices services,string module,string level,string message)
        {
            try {services.Register(module,level,Bound(message,512));}
            catch { }
        }
        static ForgeWeaveHandlerHealth Health(RegisteredHandler handler)
        {
            var percentiles = handler.CalculatePercentiles();
            return new ForgeWeaveHandlerHealth {
                Id=handler.Subscription.Descriptor.Id,Module=handler.Subscription.Descriptor.Module,Name=handler.Subscription.Descriptor.Name,Event=handler.Subscription.Event,Access=handler.Subscription.Access,
                ReplayMode=handler.Subscription.ReplayMode,Filter=CopyFilter(handler.Subscription.Filter),BudgetMilliseconds=handler.Subscription.BudgetMilliseconds,BudgetPolicy=handler.Subscription.BudgetPolicy,BudgetExceededCount=handler.BudgetExceededCount,LastBudgetExceededAt=handler.LastBudgetExceededAt,Context=handler.Subscription.Descriptor.Context,ChangesState=handler.Subscription.Descriptor.ChangesState,Priority=handler.Subscription.Priority,FailureLimit=handler.Subscription.FailureLimit,MinimumIntervalMilliseconds=handler.Subscription.MinimumIntervalMilliseconds,
                Before=new List<string>(handler.Subscription.Before),After=new List<string>(handler.Subscription.After),Status=handler.Quarantined?"Quarantined":string.IsNullOrWhiteSpace(handler.BlockingReason)?"Ready":"Blocked",
                BlockingReason=handler.BlockingReason,LastOutcome=handler.LastOutcome,InvocationCount=handler.InvocationCount,FailureCount=handler.FailureCount,ConsecutiveFailureCount=handler.ConsecutiveFailureCount,
                TotalMilliseconds=handler.TotalMilliseconds,MeanMilliseconds=handler.InvocationCount==0?0:handler.TotalMilliseconds/handler.InvocationCount,MinMilliseconds=handler.MinMilliseconds,MaxMilliseconds=handler.MaxMilliseconds,LastMilliseconds=handler.LastMilliseconds,LastError=handler.LastError,LastInvokedAt=handler.LastInvokedAt,
                Topic=handler.Subscription.Topic,CircuitState=handler.CircuitState,P50Milliseconds=percentiles.p50,P95Milliseconds=percentiles.p95,P99Milliseconds=percentiles.p99,
                BucketUnder1Ms=handler.BucketUnder1Ms,Bucket1To5Ms=handler.Bucket1To5Ms,Bucket5To20Ms=handler.Bucket5To20Ms,BucketOver20Ms=handler.BucketOver20Ms
            };
        }
        static ForgeEventSubscription Copy(ForgeEventSubscription source)=>new ForgeEventSubscription {
            Descriptor=Copy(source.Descriptor),Event=source.Event,Topic=source.Topic??string.Empty,Priority=source.Priority,
            Before=new List<string>(source.Before??new List<string>()),After=new List<string>(source.After??new List<string>()),
            Access=source.Access,ReplayMode=source.ReplayMode,Filter=CopyFilter(source.Filter),
            BudgetMilliseconds=source.BudgetMilliseconds,BudgetPolicy=source.BudgetPolicy,FailureLimit=source.FailureLimit,
            CircuitBreakerPolicy=source.CircuitBreakerPolicy,CircuitBreakerCooldownSeconds=source.CircuitBreakerCooldownSeconds,
            MinimumIntervalMilliseconds=source.MinimumIntervalMilliseconds
        };
        static Descriptor Copy(Descriptor source)=>new Descriptor {Id=source.Id,Module=source.Module,Name=source.Name,Context=source.Context,ChangesState=source.ChangesState};
        static ForgeWeaveEventHealth Copy(EventState source)=>new ForgeWeaveEventHealth {Event=source.Event,DispatchCount=source.DispatchCount,HandlerInvocationCount=source.HandlerInvocationCount,FailureCount=source.FailureCount,BudgetExceededCount=source.BudgetExceededCount,SkippedCount=source.SkippedCount,TotalMilliseconds=source.TotalMilliseconds,MeanMilliseconds=source.DispatchCount==0?0:source.TotalMilliseconds/source.DispatchCount,MinMilliseconds=source.MinMilliseconds,MaxMilliseconds=source.MaxMilliseconds,LastDispatchedAt=source.LastDispatchedAt};
        static ForgeWeaveDispatchRecord Copy(ForgeWeaveDispatchRecord source)=>new ForgeWeaveDispatchRecord {Sequence=source.Sequence,Event=source.Event,Context=source.Context,IsReplay=source.IsReplay,SourceSequence=source.SourceSequence,Status=source.Status,RejectionReason=source.RejectionReason,InvokedCount=source.InvokedCount,SkippedCount=source.SkippedCount,FailureCount=source.FailureCount,BudgetExceededCount=source.BudgetExceededCount,PropagationStopped=source.PropagationStopped,StopReason=source.StopReason,Milliseconds=source.Milliseconds,DispatchedAt=source.DispatchedAt,HandlerOutcomes=CopyOutcomes(source.HandlerOutcomes)};
        static ForgeReplayRecord Copy(ForgeReplayRecord source)=>new ForgeReplayRecord {Sequence=source.Sequence,Event=source.Event,Context=source.Context,DeltaMilliseconds=source.DeltaMilliseconds,RaisedAt=source.RaisedAt,Data=CopyData(source.Data),OriginalStatus=source.OriginalStatus,OriginalInvokedCount=source.OriginalInvokedCount,OriginalSkippedCount=source.OriginalSkippedCount,OriginalFailureCount=source.OriginalFailureCount,OriginalPropagationStopped=source.OriginalPropagationStopped,OriginalStopReason=source.OriginalStopReason,OriginalMilliseconds=source.OriginalMilliseconds,DispatchedAt=source.DispatchedAt,OriginalHandlerOutcomes=CopyOutcomes(source.OriginalHandlerOutcomes)};
        static ForgeReplayResult Copy(ForgeReplayResult source)=>new ForgeReplayResult {SourceSequence=source.SourceSequence,ReplaySequence=source.ReplaySequence,Event=source.Event,Context=source.Context,Replayed=source.Replayed,Status=source.Status,RejectionReason=source.RejectionReason,InvokedCount=source.InvokedCount,SkippedCount=source.SkippedCount,FailureCount=source.FailureCount,BudgetExceededCount=source.BudgetExceededCount,QuarantinedCount=source.QuarantinedCount,PropagationStopped=source.PropagationStopped,StopReason=source.StopReason,Milliseconds=source.Milliseconds,HandlerOutcomes=CopyOutcomes(source.HandlerOutcomes)};
        static List<ForgeHandlerOutcome> CopyOutcomes(IEnumerable<ForgeHandlerOutcome> source)
        {
            return (source??Enumerable.Empty<ForgeHandlerOutcome>()).Where(outcome=>outcome!=null).Take(MaximumHandlers).Select(outcome=>new ForgeHandlerOutcome {HandlerId=outcome.HandlerId,Module=outcome.Module,ReplayMode=outcome.ReplayMode,Status=outcome.Status,Reason=outcome.Reason,Milliseconds=outcome.Milliseconds,BudgetExceeded=outcome.BudgetExceeded}).ToList();
        }
        static Dictionary<string,string> CopyData(IEnumerable<KeyValuePair<string,string>> source)
        {
            var values=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var pair in source??Enumerable.Empty<KeyValuePair<string,string>>())
            {
                if(values.Count>=32)break;
                var key=Bound(pair.Key,64);
                if(key.Length==0 || values.ContainsKey(key))continue;
                values.Add(key,Bound(pair.Value,512));
            }
            return values;
        }
        static Finding Copy(Finding source)=>new Finding {Level=source.Level,Code=source.Code,Module=source.Module,File=source.File,Message=source.Message,Suggestion=source.Suggestion};
        static string Status(ForgeWeaveSnapshot snapshot)
        {
            if(snapshot.HandlerCount==0)return "No ForgeWeave handlers are registered in this session.";
            if(snapshot.QuarantinedHandlerCount>0 || snapshot.BlockedHandlerCount>0)return "Attention required: "+snapshot.ReadyHandlerCount+" ready, "+snapshot.BlockedHandlerCount+" blocked, "+snapshot.QuarantinedHandlerCount+" quarantined.";
            return "Ready: "+snapshot.ReadyHandlerCount+" handler(s) registered for host-supplied events.";
        }
        static string Describe(Exception error)=>Bound((error==null?"Unknown error":error.GetType().Name+": "+error.Message),512);
        static string Bound(string value,int maximum)
        {
            value=value??"";return value.Length<=maximum?value:value.Substring(0,maximum)+"…";
        }
        static void AddFinding(ICollection<Finding> findings,Finding finding)
        {
            if(findings.Count<MaximumFindings)findings.Add(finding);
        }

    }
}
