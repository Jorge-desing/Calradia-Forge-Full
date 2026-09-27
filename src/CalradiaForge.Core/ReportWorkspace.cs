using System;
using System.Collections.Generic;
using System.Linq;
using Sdk = CalradiaForge.Sdk;

namespace CalradiaForge.Core
{
    // A captured/opened report is a distinct workspace, never implicitly overwritten by a connection.
    public sealed class ReportWorkspace
    {
        const int MaximumReplayRecords=64;
        const int MaximumReplayResults=64;
        const int MaximumReplayPayloadEntries=32;
        const int MaximumReplayPayloadKeyLength=64;
        const int MaximumReplayPayloadValueLength=512;
        const int MaximumReplayOutcomes=256;
        public SessionReport Report { get; private set; }=Normalize(new SessionReport());
        public string Source { get; private set; }="New report";
        public void Open(SessionReport report,string source)
        { if(report==null)throw new ArgumentException("The file does not contain a report");Report=Normalize(report);Source=source??"Report"; }
        public static SessionReport Normalize(SessionReport report)
        {
            if(report==null)throw new ArgumentNullException(nameof(report));
            report.Logs=report.Logs??new List<LogEntry>();report.Tests=report.Tests??new List<TestResult>();
            report.Snapshots=report.Snapshots??new List<ObjectSnapshot>();report.Metrics=report.Metrics??new Dictionary<string,double>();
            report.Harmony=report.Harmony??new HarmonySnapshot {Status="Not captured."};
            report.Harmony.Notes=report.Harmony.Notes??new List<string>();
            report.Harmony.Methods=report.Harmony.Methods??new List<HarmonyPatchedMethod>();
            foreach(var method in report.Harmony.Methods.Where(method=>method!=null))
            {
                method.Owners=method.Owners??new List<string>();
                method.Patches=method.Patches??new List<HarmonyPatchObservation>();
                foreach(var patch in method.Patches.Where(patch=>patch!=null)) {patch.Before=patch.Before??new List<string>();patch.After=patch.After??new List<string>();}
                if(string.IsNullOrWhiteSpace(method.MetadataStatus))method.MetadataStatus=method.Owners.Count>0||method.Patches.Count>0?"Active":"No metadata";
            }
            report.PatchPreflight=report.PatchPreflight??new PatchPreflightSnapshot {Status="Not captured."};
            report.PatchPreflight.Notes=report.PatchPreflight.Notes??new List<string>();
            report.PatchPreflight.Findings=report.PatchPreflight.Findings??new List<Sdk.Finding>();
            report.PatchPreflight.Outcomes=report.PatchPreflight.Outcomes??new List<PatchPreflightOutcome>();
            foreach(var outcome in report.PatchPreflight.Outcomes.Where(outcome=>outcome!=null))
            {
                outcome.Declaration=outcome.Declaration??new PatchBlueprintDeclaration();
                outcome.Declaration.Blueprint=outcome.Declaration.Blueprint??new Sdk.PatchBlueprint();
                outcome.Notes=outcome.Notes??new List<string>();
            }
            report.ForgeWeave=report.ForgeWeave??new ForgeWeaveSnapshot {Status="Not captured."};
            report.ForgeWeave.Notes=report.ForgeWeave.Notes??new List<string>();
            report.ForgeWeave.Findings=report.ForgeWeave.Findings??new List<Sdk.Finding>();
            report.ForgeWeave.Handlers=report.ForgeWeave.Handlers??new List<ForgeWeaveHandlerHealth>();
            report.ForgeWeave.Events=report.ForgeWeave.Events??new List<ForgeWeaveEventHealth>();
            report.ForgeWeave.RecentDispatches=report.ForgeWeave.RecentDispatches??new List<ForgeWeaveDispatchRecord>();
            report.ForgeWeave.ReplayRecords=report.ForgeWeave.ReplayRecords??new List<Sdk.ForgeReplayRecord>();
            report.ForgeWeave.RecentReplays=report.ForgeWeave.RecentReplays??new List<Sdk.ForgeReplayResult>();
            if(report.ForgeWeave.RecentDispatches.Any(dispatch=>dispatch==null)||report.ForgeWeave.ReplayRecords.Any(record=>record==null)||report.ForgeWeave.RecentReplays.Any(replay=>replay==null)||report.ForgeWeave.RecentDispatches.Where(dispatch=>dispatch!=null).Any(dispatch=>dispatch.HandlerOutcomes!=null&&dispatch.HandlerOutcomes.Any(outcome=>outcome==null))||report.ForgeWeave.ReplayRecords.Where(record=>record!=null).Any(record=>record.OriginalHandlerOutcomes!=null&&record.OriginalHandlerOutcomes.Any(outcome=>outcome==null))||report.ForgeWeave.RecentReplays.Where(replay=>replay!=null).Any(replay=>replay.HandlerOutcomes!=null&&replay.HandlerOutcomes.Any(outcome=>outcome==null)))
                throw new ArgumentException("Invalid report: record arrays must not contain null entries.");
            foreach(var handler in report.ForgeWeave.Handlers.Where(handler=>handler!=null))
            {
                handler.Before=handler.Before??new List<string>();
                handler.After=handler.After??new List<string>();
            }
            foreach(var dispatch in report.ForgeWeave.RecentDispatches.Where(dispatch=>dispatch!=null))
                dispatch.HandlerOutcomes=NormalizeOutcomes(dispatch.HandlerOutcomes);
            foreach(var record in report.ForgeWeave.ReplayRecords.Where(record=>record!=null))
            {
                record.Data=NormalizePayload(record.Data);
                record.OriginalHandlerOutcomes=NormalizeOutcomes(record.OriginalHandlerOutcomes);
            }
            foreach(var replay in report.ForgeWeave.RecentReplays.Where(replay=>replay!=null))
                replay.HandlerOutcomes=NormalizeOutcomes(replay.HandlerOutcomes);
            if(report.ForgeWeave.ReplayRecords.Count>MaximumReplayRecords)report.ForgeWeave.ReplayRecords=report.ForgeWeave.ReplayRecords.Take(MaximumReplayRecords).ToList();
            if(report.ForgeWeave.RecentReplays.Count>MaximumReplayResults)report.ForgeWeave.RecentReplays=report.ForgeWeave.RecentReplays.Take(MaximumReplayResults).ToList();
            report.ModuleDiagnostics=report.ModuleDiagnostics??new ModuleDiagnostics();
            report.ModuleDiagnostics.Modules=report.ModuleDiagnostics.Modules??new List<Module>();
            report.ModuleDiagnostics.Findings=report.ModuleDiagnostics.Findings??new List<Sdk.Finding>();
            if(report.Logs.Any(x=>x==null)||report.Tests.Any(x=>x==null)||report.Snapshots.Any(x=>x==null)||report.Harmony.Methods.Any(x=>x==null)||report.Harmony.Methods.Any(method=>method.Patches.Any(patch=>patch==null))||report.PatchPreflight.Findings.Any(x=>x==null)||report.PatchPreflight.Outcomes.Any(x=>x==null)||report.ForgeWeave.Findings.Any(x=>x==null)||report.ForgeWeave.Handlers.Any(x=>x==null)||report.ForgeWeave.Events.Any(x=>x==null)||report.ForgeWeave.RecentDispatches.Any(x=>x==null)||report.ForgeWeave.ReplayRecords.Any(x=>x==null)||report.ForgeWeave.RecentReplays.Any(x=>x==null)||report.ModuleDiagnostics.Modules.Any(x=>x==null)||report.ModuleDiagnostics.Findings.Any(x=>x==null))
                throw new ArgumentException("Invalid report: record arrays must not contain null entries.");
            return report;
        }
        public string Section(string action,string filter)
        {
            switch(action)
            {
                case "summary":return Source+"\nGame: "+(Report.GameVersion??"Not captured")+"\nModules: "+Report.ModuleDiagnostics.Modules.Count+"\nFindings: "+Report.ModuleDiagnostics.Findings.Count+"\nForgeWeave handlers: "+Report.ForgeWeave.Handlers.Count+"\nPatch blueprints: "+Report.PatchPreflight.Outcomes.Count+"\nTests: "+Report.Tests.Count+"\nLogs: "+Report.Logs.Count;
                case "modules":return Json.Serialize(Report.ModuleDiagnostics);
                case "logs":return Json.Serialize(Report.Logs.Where(l=>Matches(l.Module+" "+l.Level+" "+l.Message+" "+l.Session,filter)).ToList());
                case "inspect":case "snapshots":return Json.Serialize(Report.Snapshots.Where(o=>Matches(o.Type+"|"+o.Id+" "+o.Name,filter)).ToList());
                case "tests":return Json.Serialize(Report.Tests.Where(t=>Matches(t.Id+" "+t.Status,filter)).ToList());
                case "metrics":return Json.Serialize(Report.Metrics);
                case "harmony":return Json.Serialize(Report.Harmony);
                case "framework":return Json.Serialize(Report.ForgeWeave);
                case "event-journal":return Json.Serialize(Report.ForgeWeave.RecentDispatches);
                case "patch-preflight":return Json.Serialize(Report.PatchPreflight);
                case "dependencies":return Json.Serialize(DependencyPlanner.Create(Report.ModuleDiagnostics.Modules));
                default:return "This operation needs a live game connection.";
            }
        }
        static Dictionary<string,string> NormalizePayload(Dictionary<string,string> values)
        {
            var result=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var pair in values??new Dictionary<string,string>())
            {
                if(result.Count>=MaximumReplayPayloadEntries)break;
                var key=Bound(pair.Key,MaximumReplayPayloadKeyLength);
                if(key.Length==0 || result.ContainsKey(key))continue;
                result.Add(key,Bound(pair.Value,MaximumReplayPayloadValueLength));
            }
            return result;
        }
        static List<Sdk.ForgeHandlerOutcome> NormalizeOutcomes(List<Sdk.ForgeHandlerOutcome> outcomes)
        {
            if(outcomes==null)return new List<Sdk.ForgeHandlerOutcome>();
            foreach(var outcome in outcomes.Where(outcome=>outcome!=null))
            {
                outcome.HandlerId=Bound(outcome.HandlerId,128);
                outcome.Module=Bound(outcome.Module,128);
                outcome.Status=Bound(outcome.Status,512);
                outcome.Reason=Bound(outcome.Reason,512);
                if(double.IsNaN(outcome.Milliseconds)||double.IsInfinity(outcome.Milliseconds)||outcome.Milliseconds<0)outcome.Milliseconds=0;
                else if(outcome.Milliseconds>60000)outcome.Milliseconds=60000;
            }
            return outcomes.Take(MaximumReplayOutcomes).ToList();
        }
        static string Bound(string value,int maximum)
        {
            value=value??"";
            return value.Length<=maximum?value:value.Substring(0,maximum)+"…";
        }
        static bool Matches(string text,string filter)=>string.IsNullOrWhiteSpace(filter)||text.IndexOf(filter,StringComparison.OrdinalIgnoreCase)>=0;
    }
}
