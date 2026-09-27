using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using Sdk = CalradiaForge.Sdk;

namespace CalradiaForge.Core
{
    public static class ReportHtml
    {
        public static string Render(SessionReport report)
        {
            if(report==null)throw new ArgumentNullException(nameof(report));
            var r=ReportWorkspace.Normalize(report);
            var b=new StringBuilder("<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Calradia Forge — Session report</title><style>body{margin:0;background:#172722;color:#f3e6c5;font:16px system-ui}main{max-width:1150px;margin:auto;padding:40px}h1,h2{font-family:Georgia}h1{font-size:38px;border-bottom:2px solid #d8b56c;padding-bottom:18px}h2{margin-top:36px}table{width:100%;border-collapse:collapse;background:#fff7e3;color:#29251c}th,td{text-align:left;padding:10px;border-bottom:1px solid #d6c7a5;vertical-align:top;overflow-wrap:anywhere}th{background:#e7d6ab}td{max-width:400px;white-space:pre-wrap}a{color:#efd393}.scroll{overflow:auto}.meta{line-height:1.8}.empty{color:#dfd4b9}@media print{body{background:white;color:black}main{padding:0}a{color:black}}</style><main><h1>Calradia Forge</h1>");
            b.Append("<p class='meta'>Session: ").Append(E(r.Session)).Append("<br>Captured: ").Append(E(r.Date)).Append("<br>Game: ").Append(E(r.GameVersion)).Append("<br>Suite: ").Append(E(r.SuiteVersion)).Append("</p>");
            b.Append("<nav><a href='#modules'>Modules</a> · <a href='#findings'>Diagnostics</a> · <a href='#forgeweave'>ForgeWeave</a> · <a href='#forgeweave-replay'>Replay Lab</a> · <a href='#patch-preflight'>Patch preflight</a> · <a href='#harmony'>Harmony atlas</a> · <a href='#tests'>Tests</a> · <a href='#snapshots'>Snapshots</a> · <a href='#metrics'>Metrics</a> · <a href='#logs'>Logs</a></nav>");
            Table(b,"modules","Modules",new[]{"ID","Version","Required dependencies"},r.ModuleDiagnostics.Modules.Select(m=>new[]{m.Id,m.Version,string.Join(", ",m.Dependencies??new List<string>())}));
            Table(b,"findings","Diagnostics",new[]{"Severity / code","Module","Evidence","Suggestion"},r.ModuleDiagnostics.Findings.Select(f=>new[]{f.Level+" / "+f.Code,f.Module,f.Message+"\n"+f.File,f.Suggestion}));
            ForgeWeave(b,r.ForgeWeave);
            PatchPreflight(b,r.PatchPreflight);
            Harmony(b,r.Harmony);
            Table(b,"tests","Tests",new[]{"Test","Result","Seed / duration","Steps and errors"},r.Tests.Select(t=>new[]{t.Id,t.Status,t.Seed+" / "+t.Milliseconds.ToString("F2",CultureInfo.InvariantCulture)+" ms",string.Join("\n",t.Steps??new List<string>())+"\n"+t.Error+"\n"+t.CleanupError}));
            Table(b,"snapshots","Snapshots",new[]{"Type / ID","Name","Properties"},r.Snapshots.Select(s=>new[]{s.Type+" / "+s.Id,s.Name,string.Join("\n",(s.Properties??new Dictionary<string,string>()).Select(p=>p.Key+" = "+p.Value))}));
            Table(b,"metrics","Metrics",new[]{"Measurement","Value"},r.Metrics.Select(p=>new[]{p.Key,p.Value.ToString("G6",CultureInfo.InvariantCulture)}));
            Table(b,"logs","Logs",new[]{"Time","Module / level","Message"},r.Logs.Select(l=>new[]{l.Time,l.Module+" / "+l.Level,l.Message}));
            return b.Append("<p>Measurements describe the captured session. They do not prove that a particular mod caused a crash or slowdown. The JSON companion preserves structured data.</p></main></html>").ToString();
        }
        static string E(string value)=>WebUtility.HtmlEncode(value??"—");
        static void Table(StringBuilder b,string id,string title,string[] headers,IEnumerable<string[]> rows)
        {
            var items=rows.ToArray();b.Append("<h2 id='").Append(id).Append("'>").Append(title).Append(" <small>(").Append(items.Length).Append(")</small></h2>");
            if(items.Length==0){b.Append("<p class='empty'>No records captured.</p>");return;}
            b.Append("<div class='scroll'><table><thead><tr>");foreach(var h in headers)b.Append("<th scope='col'>").Append(E(h)).Append("</th>");b.Append("</tr></thead><tbody>");
            foreach(var row in items){b.Append("<tr>");foreach(var cell in row)b.Append("<td>").Append(E(cell)).Append("</td>");b.Append("</tr>");}b.Append("</tbody></table></div>");
        }
        static void Harmony(StringBuilder b,HarmonySnapshot harmony)
        {
            harmony=harmony??new HarmonySnapshot {Status="Not captured."};
            b.Append("<h2 id='harmony'>Harmony patch atlas <small>(").Append(harmony.DisplayedMethodCount).Append(")</small></h2>");
            b.Append("<p class='meta'>").Append(E(harmony.Status)).Append("<br>Captured: ").Append(E(harmony.CapturedAt)).Append(harmony.IsStale?" (stale)":"").Append("<br>Runtime: ").Append(E(harmony.RuntimeAssembly)).Append(" ").Append(E(harmony.RuntimeVersion)).Append("<br>Active targets: ").Append(harmony.ActiveMethodCount).Append(" · No metadata: ").Append(harmony.EmptyMetadataMethodCount).Append(" · Owners: ").Append(harmony.OwnerCount).Append(" · Shared targets: ").Append(harmony.SharedMethodCount).Append("</p>");
            if((harmony.Notes??new List<string>()).Count>0)b.Append("<p class='empty'>").Append(E(string.Join("\n",harmony.Notes))).Append("</p>");
            var rows=(harmony.Methods??new List<HarmonyPatchedMethod>()).SelectMany(method=>(method.Patches??new List<HarmonyPatchObservation>()).DefaultIfEmpty(),(method,patch)=>new[]{
                (method.MetadataStatus??"Unknown")+"\n"+(method.Assembly??"")+" · "+(method.DeclaringType??"")+"."+(method.Method??"")+(method.Signature??""),
                string.Join(", ",method.Owners??new List<string>()),
                patch==null?"No patch metadata":(patch.Kind??"")+" · "+(patch.Owner??"")+" · priority "+(patch.Priority?.ToString()??"unknown"),
                patch==null?"":(patch.PatchAssembly??"")+" · "+(patch.PatchType??"")+"."+(patch.PatchMethod??"")+(patch.PatchSignature??"")+"\nBefore: "+string.Join(", ",patch.Before??new List<string>())+"\nAfter: "+string.Join(", ",patch.After??new List<string>())
            });
            Table(b,"harmony-table","Harmony targets",new[]{"Patched target","Owners","Patch","Patch method / order"},rows);
        }
        static void ForgeWeave(StringBuilder b,ForgeWeaveSnapshot weave)
        {
            weave=weave??new ForgeWeaveSnapshot {Status="Not captured."};
            b.Append("<h2 id='forgeweave'>ForgeWeave extension framework <small>(").Append(weave.HandlerCount).Append(")</small></h2>");
            b.Append("<p class='meta'>").Append(E(weave.Status)).Append("<br>Captured: ").Append(E(weave.CapturedAt)).Append("<br>Ready: ").Append(weave.ReadyHandlerCount).Append(" · Blocked: ").Append(weave.BlockedHandlerCount).Append(" · Quarantined: ").Append(weave.QuarantinedHandlerCount).Append("<br>Dispatches: ").Append(weave.DispatchCount).Append(" · Invocations: ").Append(weave.InvocationCount).Append(" · Failures: ").Append(weave.FailureCount).Append(" · Over budget: ").Append(weave.BudgetExceededCount).Append(" · Mean: ").Append(weave.MeanMilliseconds.ToString("F2",CultureInfo.InvariantCulture)).Append(" ms · Max: ").Append(weave.MaxMilliseconds.ToString("F2",CultureInfo.InvariantCulture)).Append(" ms<br>Replay sources: ").Append(weave.ReplayRecordCount).Append(" · Attempts: ").Append(weave.ReplayAttemptCount).Append(" · Completed: ").Append(weave.ReplaySuccessCount).Append(" · Rejected: ").Append(weave.ReplayRejectedCount).Append("</p>");
            if((weave.Notes??new List<string>()).Count>0)b.Append("<p class='empty'>").Append(E(string.Join("\n",weave.Notes))).Append("</p>");
            var handlers=(weave.Handlers??new List<ForgeWeaveHandlerHealth>()).Select(handler=>new[]{
                (handler.Module??"")+" / "+(handler.Id??"")+"\n"+(handler.Name??""),
                handler.Event+" · "+handler.Context+" · "+handler.Access+(handler.ChangesState?" · state change":"")+"\nReplay: "+handler.ReplayMode+"\nFilter: "+string.Join("; ",(handler.Filter?.RequiredData??new Dictionary<string,string>()).Select(pair=>pair.Key+"="+pair.Value))+"\nBudget: "+(handler.BudgetMilliseconds==0?"off":handler.BudgetMilliseconds+" ms")+" · Overruns: "+handler.BudgetExceededCount,
                (handler.Status??"")+"\n"+(handler.BlockingReason??"")+"\n"+(handler.LastOutcome??"")+"\n"+(handler.LastError??""),
                "Priority: "+handler.Priority+"\nBefore: "+string.Join(", ",handler.Before??new List<string>())+"\nAfter: "+string.Join(", ",handler.After??new List<string>()),
                "Calls: "+handler.InvocationCount+" · Failures: "+handler.FailureCount+" · Consecutive: "+handler.ConsecutiveFailureCount+"\nLast: "+handler.LastInvokedAt+"\nMean: "+handler.MeanMilliseconds.ToString("F2",CultureInfo.InvariantCulture)+" ms · Max: "+handler.MaxMilliseconds.ToString("F2",CultureInfo.InvariantCulture)+" ms"
            });
            Table(b,"forgeweave-handlers","ForgeWeave handlers",new[]{"Extension","Event / access","Health","Declared order","Timing"},handlers);
            var events=(weave.Events??new List<ForgeWeaveEventHealth>()).Select(@event=>new[]{
                @event.Event.ToString(),"Dispatches: "+@event.DispatchCount+" · Calls: "+@event.HandlerInvocationCount+" · Skipped: "+@event.SkippedCount+" · Failures: "+@event.FailureCount,
                "Mean: "+@event.MeanMilliseconds.ToString("F2",CultureInfo.InvariantCulture)+" ms · Max: "+@event.MaxMilliseconds.ToString("F2",CultureInfo.InvariantCulture)+" ms\nLast: "+@event.LastDispatchedAt
            });
            Table(b,"forgeweave-events","Host event health",new[]{"Event","Counts","Timing"},events);
            var journal=(weave.RecentDispatches??new List<ForgeWeaveDispatchRecord>()).Select(entry=>new[]{
                entry.Sequence+" · "+entry.Event+" · "+entry.Context+"\n"+(entry.IsReplay?"Replay of source "+(entry.SourceSequence?.ToString()??"unknown"):"Host dispatch"),(entry.Status??"")+"\n"+(entry.RejectionReason??"")+"\n"+(entry.PropagationStopped?"Propagation stopped: "+entry.StopReason:""),
                "Calls: "+entry.InvokedCount+" · Skipped: "+entry.SkippedCount+" · Failures: "+entry.FailureCount+" · "+entry.Milliseconds.ToString("F2",CultureInfo.InvariantCulture)+" ms\n"+(entry.DispatchedAt??"")+"\n"+Outcomes(entry.HandlerOutcomes)
            });
            Table(b,"forgeweave-journal","Recent ForgeWeave dispatches",new[]{"Sequence / event","Result","Work"},journal);
            ReplayLab(b,weave);
            Table(b,"forgeweave-findings","ForgeWeave findings",new[]{"Severity / code","Module","Evidence","Suggestion"},(weave.Findings??new List<Sdk.Finding>()).Select(f=>new[]{f.Level+" / "+f.Code,f.Module,f.Message,f.Suggestion}));
        }
        static void ReplayLab(StringBuilder b,ForgeWeaveSnapshot weave)
        {
            b.Append("<h2 id='forgeweave-replay'>ForgeWeave Replay Lab <small>(").Append(weave.ReplayRecordCount).Append(")</small></h2>");
            b.Append("<p class='meta'>Replay Lab reuses a retained host event by sequence. It is controlled event verification, not method interception: handlers must opt in, live context must match, and state-changing handlers keep their normal test-mode and campaign-copy gates.</p>");
            var records=(weave.ReplayRecords??new List<Sdk.ForgeReplayRecord>()).Select(record=>new[]{
                record.Sequence+" · "+record.Event+" · "+record.Context+"\n"+(record.RaisedAt??""),
                (record.OriginalStatus??"")+"\nCalls: "+record.OriginalInvokedCount+" · Skipped: "+record.OriginalSkippedCount+" · Failures: "+record.OriginalFailureCount+" · "+record.OriginalMilliseconds.ToString("F2",CultureInfo.InvariantCulture)+" ms"+(record.OriginalPropagationStopped?"\nPropagation stopped: "+record.OriginalStopReason:""),
                Data(record.Data),
                Outcomes(record.OriginalHandlerOutcomes)
            });
            Table(b,"forgeweave-replay-records","Retained replay evidence",new[]{"Source sequence / event","Original dispatch","Copied scalar payload","Original handler outcomes"},records);
            var outcomes=(weave.RecentReplays??new List<Sdk.ForgeReplayResult>()).Select(replay=>new[]{
                "Source: "+replay.SourceSequence+"\nReplay: "+(replay.ReplaySequence?.ToString()??"—")+"\n"+replay.Event+" · "+replay.Context,
                (replay.Status??"")+"\n"+(replay.Replayed?"Replayed":"Rejected")+"\n"+(replay.RejectionReason??"")+(replay.PropagationStopped?"\nPropagation stopped: "+replay.StopReason:""),
                "Calls: "+replay.InvokedCount+" · Skipped: "+replay.SkippedCount+" · Failures: "+replay.FailureCount+" · Quarantined: "+replay.QuarantinedCount+"\n"+replay.Milliseconds.ToString("F2",CultureInfo.InvariantCulture)+" ms",
                Outcomes(replay.HandlerOutcomes)
            });
            Table(b,"forgeweave-replay-outcomes","Recent replay outcomes",new[]{"Source / replay","Outcome","Work","Handler outcomes"},outcomes);
        }
        static string Data(IDictionary<string,string> values)
        {
            if(values==null || values.Count==0)return "No scalar payload retained.";
            return string.Join("\n",values.OrderBy(pair=>pair.Key??"",StringComparer.Ordinal).Select(pair=>(pair.Key??"")+" = "+(pair.Value??"")));
        }
        static string Outcomes(IEnumerable<Sdk.ForgeHandlerOutcome> outcomes)
        {
            var rows=(outcomes??new List<Sdk.ForgeHandlerOutcome>()).Select(outcome=>(outcome.Module??"")+" / "+(outcome.HandlerId??"")+" · "+outcome.ReplayMode+"\n"+(outcome.Status??"")+" · "+(outcome.Reason??"")+" · "+outcome.Milliseconds.ToString("F2",CultureInfo.InvariantCulture)+" ms").ToArray();
            return rows.Length==0?"No handler outcomes captured.":string.Join("\n\n",rows);
        }
        static void PatchPreflight(StringBuilder b,PatchPreflightSnapshot preflight)
        {
            preflight=preflight??new PatchPreflightSnapshot {Status="Not captured."};
            b.Append("<h2 id='patch-preflight'>Patch blueprint preflight <small>(").Append(preflight.Outcomes?.Count??0).Append(")</small></h2>");
            b.Append("<p class='meta'>").Append(E(preflight.Status)).Append("<br>Captured: ").Append(E(preflight.CapturedAt)).Append(preflight.IsStale?" (stale)":"").Append("<br>Providers: ").Append(preflight.ProviderCount).Append(" · Resolved: ").Append(preflight.ResolvedCount).Append(" · Review items: ").Append(preflight.ReviewCount).Append("</p>");
            if((preflight.Notes??new List<string>()).Count>0)b.Append("<p class='empty'>").Append(E(string.Join("\n",preflight.Notes))).Append("</p>");
            var rows=(preflight.Outcomes??new List<PatchPreflightOutcome>()).Select(outcome=>new[]{
                (outcome.Declaration?.Module??"")+" / "+(outcome.Declaration?.Blueprint?.Id??"")+"\n"+(outcome.Declaration?.Blueprint?.Name??"")+"\nHook: "+(outcome.Declaration?.Blueprint?.Hook.ToString()??""),
                Reference(outcome.Declaration?.Blueprint?.Target),
                (outcome.Status??"")+"\n"+(outcome.Resolved?((outcome.ResolvedAssembly??"")+" · "+(outcome.ResolvedType??"")+"."+(outcome.ResolvedMember??"")+(outcome.ResolvedSignature??"")):""),
                string.Join("\n",outcome.Notes??new List<string>())
            });
            Table(b,"patch-preflight-table","Declared patch blueprints",new[]{"Owner / blueprint","Declared target","Resolution","Notes"},rows);
            Table(b,"patch-preflight-findings","Preflight findings",new[]{"Severity / code","Module","Evidence","Suggestion"},(preflight.Findings??new List<Sdk.Finding>()).Select(f=>new[]{f.Level+" / "+f.Code,f.Module,f.Message,f.Suggestion}));
        }
        static string Reference(Sdk.MethodReference reference)
        {
            if(reference==null)return "—";
            var parameters=reference.ParameterTypes??new List<Sdk.TypeReference>();
            var member=reference.MemberKind==Sdk.PatchMemberKind.Constructor?".ctor":reference.MemberName;
            return (reference.AssemblyName??"")+" · "+(reference.DeclaringType??"")+"."+(member??"")+"("+string.Join(", ",parameters.Select(parameter=>parameter?.FullName??"?"))+")";
        }
    }
}
