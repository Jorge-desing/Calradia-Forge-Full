using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using CalradiaForge.Core;
using CalradiaForge.Sdk;
using CalradiaForge.Mod;
using CoreModule = CalradiaForge.Core.Module;

internal static class ReleaseTests
{
    public static void Run(Action<string,Action> test)
    {
        test("English is the default product language",()=>Assert(Localization.DefaultLanguage=="en" && new Settings().Language=="en"));
        test("English text is independent of selected OS culture",()=>{var previous=Thread.CurrentThread.CurrentUICulture;try{Thread.CurrentThread.CurrentUICulture=new System.Globalization.CultureInfo("es-MX");Assert(Localization.Text("Modules",Localization.DefaultLanguage)=="Modules");}finally{Thread.CurrentThread.CurrentUICulture=previous;}});
        test("Spanish is an explicit secondary translation",()=>Assert(Localization.Text("Modules","es")=="Módulos" && Localization.Text("Modules","en")=="Modules"));
        test("Unsupported languages fall back to English",()=>Assert(Localization.Text("Modules","fr")=="Modules" && Localization.Text("Unregistered extension message","es")=="Unregistered extension message"));
        test("Protocol advertises the complete standalone developer surface",()=>{var expected=new[]{"hello","summary","scan","modules","dependencies","diagnostics","logs","inspect","pin","compare","snapshots","unpin","tests","commands","command","test-mode","confirm-copy","run","run-batch","metrics","framework","event-journal","replay","harmony","patch-blueprints","patch-preflight","report","export","panel-open","panel-close","language","agent-memory"};var capabilities=ForgeProtocol.Hello(SuiteInfo.Version,"1.4.8");Assert(capabilities.Contains("protocol:1")&&expected.All(capabilities.Contains)&&ForgeProtocol.Actions.SequenceEqual(expected)&&capabilities.Length==expected.Length+3);});
        test("Extension startup keeps healthy registrations after another callback fails",()=>{
            var healthy=false;Action<IForgeRegistry> broken=registry=>throw new InvalidOperationException("broken extension");Action<IForgeRegistry> ready=registry=>healthy=ReferenceEquals(registry,ForgeApi.Registry);
            ForgeApi.Available+=broken;ForgeApi.Available+=ready;
            try {var result=ExtensionStartup.Connect(new TestEngine());Assert(result.Connected&&healthy&&result.Errors.Count==1&&result.Errors[0].Contains("broken extension"));}
            finally {ForgeApi.Available-=broken;ForgeApi.Available-=ready;ForgeApi.Disconnect();}
        });
        test("Harmony atlas reports absence without requiring Harmony",()=>{var result=HarmonyDiagnostics.Inspect(new Assembly[0]);Assert(!result.Available&&!result.Supported&&result.Status.Contains("not loaded"));});
        test("Harmony atlas reports an incompatible runtime without failing",()=>{var result=HarmonyDiagnostics.Inspect(typeof(UnsupportedHarmony));Assert(result.Available&&!result.Supported&&result.Status.Contains("unavailable"));});
        test("Harmony atlas preserves target identity, owners, kinds and ordering",()=>{
            var result=HarmonyDiagnostics.Inspect(typeof(FakeHarmony),null,20);var target=result.Methods.Single(row=>row.Signature.Contains("System.Int32"));var prefix=target.Patches.Single(patch=>patch.Kind=="Prefix");
            Assert(result.Supported&&result.DiscoveredMethodCount==2&&target.HasMultipleOwners&&target.Owners.SequenceEqual(new[]{"owner.alpha","owner.beta"})&&prefix.Owner=="owner.alpha"&&prefix.Priority==700&&prefix.Before.Single()=="owner.before"&&prefix.After.Single()=="owner.after"&&prefix.PatchMethod=="Prefix");
        });
        test("Harmony atlas filters and explicitly bounds displayed targets",()=>{var filtered=HarmonyDiagnostics.Inspect(typeof(FakeHarmony),"owner.beta",20);var limited=HarmonyDiagnostics.Inspect(typeof(FakeHarmony),null,1);Assert(filtered.Methods.Count==1&&filtered.Methods[0].Owners.Contains("owner.beta")&&limited.Methods.Count==1&&limited.Truncated);});
        test("Harmony atlas tolerates unreadable patch metadata",()=>{var result=HarmonyDiagnostics.Inspect(typeof(BrokenHarmony),null,20);Assert(result.Supported&&result.DiscoveredMethodCount==1&&result.Methods.Count==1);});
        test("Harmony atlas marks empty metadata without calling it active",()=>{var result=HarmonyDiagnostics.Inspect(typeof(EmptyHarmony),null,20);Assert(result.EmptyMetadataMethodCount==1&&result.ActiveMethodCount==0&&result.Methods.Single().MetadataStatus=="No metadata"&&result.Status.Contains("not confirmed"));});
        test("Patch blueprint registry is additive and read-only",()=>{
            var engine=new TestEngine();var provider=new BlueprintProvider("blueprint.provider",Blueprint("blueprint.one",MethodReference.From(typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}))));
            engine.Register(provider);Assert(engine.PatchBlueprintProviders.Single().Id=="blueprint.provider");
            Throws(()=>engine.Register(new StatefulBlueprintProvider()));
            ForgeApi.Connect(engine);try{Assert(ReferenceEquals(ForgeApi.PatchBlueprints,engine));}finally{ForgeApi.Disconnect();}
        });
        test("Patch blueprint capture copies declarations and isolates provider errors",()=>{
            var engine=new TestEngine();var blueprint=Blueprint("blueprint.copy",MethodReference.From(typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)})));
            engine.Register(new BlueprintProvider("blueprint.healthy",blueprint));engine.Register(new BlueprintProvider("blueprint.broken"){Fail=true});
            var capture=engine.CapturePatchBlueprints(new Services(),CancellationToken.None);blueprint.Id="changed";blueprint.Target.MemberName="changed";blueprint.Before.Add("changed");
            Assert(capture.Declarations.Count==1&&capture.Declarations.Single().Blueprint.Id=="blueprint.copy"&&capture.Declarations.Single().Blueprint.Target.MemberName==nameof(PatchTargetFixture.Overload)&&capture.Findings.Any(f=>f.Code=="patch_blueprint_provider_error"));
        });
        test("Patch preflight resolves an exact overload and constructor",()=>{
            var overload=MethodReference.From(typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}));
            var constructor=MethodReference.From(typeof(PatchTargetFixture).GetConstructor(new[]{typeof(int)}));
            var result=PatchPreflightEngine.Inspect(Capture(Blueprint("blueprint.overload",overload),Blueprint("blueprint.constructor",constructor)),new[]{typeof(PatchTargetFixture).Assembly},"Any");
            Assert(result.ResolvedCount==2&&result.Outcomes.All(outcome=>outcome.Resolved)&&result.Outcomes.Single(outcome=>outcome.Declaration.Blueprint.Id=="blueprint.overload").ResolvedSignature.Contains("System.Int32"));
        });
        test("Patch preflight never chooses an undeclared overload",()=>{
            var target=new MethodReference {AssemblyName=typeof(PatchTargetFixture).Assembly.GetName().Name,DeclaringType=typeof(PatchTargetFixture).FullName,MemberName=nameof(PatchTargetFixture.Overload),ParameterTypes=new List<TypeReference>()};
            var result=PatchPreflightEngine.Inspect(Capture(Blueprint("blueprint.missing-signature",target)),new[]{typeof(PatchTargetFixture).Assembly},"Any");
            Assert(result.ResolvedCount==0&&result.Outcomes.Single().Status=="Member not found");
        });
        test("Patch preflight reports invalid and duplicate declarations",()=>{
            var target=MethodReference.From(typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}));
            var invalid=Blueprint(null,target);var first=Blueprint("blueprint.duplicate",target);var duplicate=Blueprint("blueprint.duplicate",target);
            var result=PatchPreflightEngine.Inspect(Capture(invalid,first,duplicate),new[]{typeof(PatchTargetFixture).Assembly},"Any");
            Assert(result.Outcomes.Count==3&&result.Outcomes.Any(outcome=>outcome.Status=="Invalid blueprint")&&result.Outcomes.Any(outcome=>outcome.Status=="Duplicate blueprint ID")&&result.Findings.Count>=2);
        });
        test("Patch preflight remains structurally useful without a patch runtime",()=>{
            var target=MethodReference.From(typeof(string).GetMethod(nameof(string.IsNullOrEmpty),new[]{typeof(string)}));
            var result=PatchPreflightEngine.Inspect(Capture(Blueprint("blueprint.no-runtime",target)),new[]{typeof(string).Assembly},"Any");
            Assert(result.ResolvedCount==1&&result.ReviewCount==0&&result.Outcomes.Single().Resolved&&result.Status.StartsWith("No blocking issues"));
        });
        test("Patch preflight reports self ordering as an independent review item",()=>{
            var target=MethodReference.From(typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}));
            var blueprint=Blueprint("blueprint.order",target);blueprint.Before.Add("blueprint.order");
            var result=PatchPreflightEngine.Inspect(Capture(blueprint),new[]{typeof(PatchTargetFixture).Assembly},"Any");
            Assert(result.ResolvedCount==1&&result.ReviewCount==1&&result.Status.StartsWith("Review required")&&result.Findings.Any(f=>f.Code=="patch_order_self"));
        });
        test("Registration freezes mutation permissions",()=>{var engine=new TestEngine();var item=new MutableTest();engine.Register(item);item.Descriptor.ChangesState=false;Throws(()=>engine.Execute("mutable",new Services(),1,CancellationToken.None));Assert(item.Runs==0);});
        test("Enumerated descriptors cannot change permissions",()=>{var engine=new TestEngine();engine.Register(new MutableTest());engine.Tests.First().ChangesState=false;Throws(()=>engine.Execute("mutable",new Services(),1,CancellationToken.None));});
        test("Registration captures ID once",()=>{var engine=Enabled();var item=new MutableTest();engine.Register(item);item.Descriptor.Id="other";Assert(engine.Execute("mutable",new Services(),1,CancellationToken.None).Id=="mutable");});
        test("Failed log callback preserves test result",()=>{var engine=Enabled();engine.Register(new MutableTest());Assert(engine.Execute("mutable",new Services{ThrowOnLog=true},1,CancellationToken.None).Status=="Passed");});
        test("Batch preflight prevents partial mutation",()=>{var engine=Enabled();var item=new MutableTest();engine.Register(item);Throws(()=>engine.ExecuteBatch(new[]{"mutable","missing"},new Services(),1,CancellationToken.None));Assert(item.Runs==0);});
        test("Batch stops after first failure and cleans",()=>{var engine=Enabled();var first=new MutableTest{Fail=true};var second=new MutableTest();second.Descriptor.Id="second";engine.Register(first);engine.Register(second);var rows=engine.ExecuteBatch(new[]{"mutable","second"},new Services(),12,CancellationToken.None);Assert(rows.Count==1&&first.Cleaned&&second.Runs==0);});
        test("Batch keeps selected seed",()=>{var engine=Enabled();engine.Register(new MutableTest());var rows=engine.ExecuteBatch(new[]{"mutable","mutable"},new Services(),712,CancellationToken.None);Assert(rows.Count==2&&rows.All(r=>r.Seed==712));});
        test("Empty and oversized batches rejected",()=>{var e=Enabled();e.Register(new MutableTest());Throws(()=>e.ExecuteBatch(new string[0],new Services(),1,CancellationToken.None));Throws(()=>e.ExecuteBatch(Enumerable.Repeat("mutable",51),new Services(),1,CancellationToken.None));});
        test("Already cancelled batch performs no mutation",()=>{var e=Enabled();var p=new MutableTest();e.Register(p);Assert(e.ExecuteBatch(new[]{"mutable"},new Services(),1,new CancellationToken(true)).Count==0&&p.Runs==0);});
        test("Dependency ordering places prerequisites first",()=>{var p=DependencyPlanner.Create(new[]{M("C","A","B"),M("B","A"),M("A")});Assert(p.Complete&&string.Join(",",p.Order)=="A,B,C");});
        test("Missing dependency blocks dependents",()=>{var p=DependencyPlanner.Create(new[]{M("A","missing"),M("B","A"),M("C")});Assert(!p.Complete&&p.Blocked.Count==2&&p.Order.Single()=="C");});
        test("Dependency cycle never produces a complete order",()=>Assert(!DependencyPlanner.Create(new[]{M("A","B"),M("B","A")}).Complete));
        test("Duplicate module IDs reject order",()=>Assert(!DependencyPlanner.Create(new[]{M("A"),M("a")}).Complete));
        test("Dependency ID matching is case insensitive",()=>Assert(DependencyPlanner.Create(new[]{M("A"),M("B","a")}).Complete));
        test("Older sparse reports normalize safely",()=>{var w=new ReportWorkspace();w.Open(new SessionReport{Logs=null,Tests=null,Snapshots=null,Metrics=null,Harmony=null,ForgeWeave=null,PatchPreflight=null,ModuleDiagnostics=null},"old");Assert(w.Section("logs","")=="[]"&&w.Section("harmony","").Contains("Not captured")&&w.Section("framework","").Contains("Not captured")&&w.Section("patch-preflight","").Contains("Not captured")&&w.Section("summary","").Contains("old"));});
        test("Malformed report preserves previously opened evidence",()=>{var w=new ReportWorkspace();var previous=new SessionReport{Session="preserve"};w.Open(previous,"baseline");Throws(()=>w.Open(new SessionReport{Logs=new List<LogEntry>{null}},"invalid"));Assert(ReferenceEquals(w.Report,previous)&&w.Source=="baseline");});
        test("HTML rejects null records with an actionable error",()=>{try{ReportHtml.Render(new SessionReport{Snapshots=new List<ObjectSnapshot>{null}});}catch(ArgumentException e){Assert(e.Message.Contains("null entries"));return;}throw new Exception("Expected report validation");});


        test("Harmony report HTML escapes patch metadata",()=>{
            var report=new SessionReport {
                Harmony=new HarmonySnapshot {
                    Status="<script>",
                    Methods=new List<HarmonyPatchedMethod> {
                        new HarmonyPatchedMethod {
                            Owners=new List<string>{"<owner>"},
                            Patches=new List<HarmonyPatchObservation>{new HarmonyPatchObservation{Kind="Prefix",Owner="<patch>"}}
                        }
                    }
                }
            };
            var html=ReportHtml.Render(report);
            Assert(html.Contains("Harmony patch atlas")&&!html.Contains("<script>"));
        });
        test("Patch preflight report HTML escapes author declarations",()=>{
            var report=new SessionReport {PatchPreflight=new PatchPreflightSnapshot {Status="<script>",Outcomes=new List<PatchPreflightOutcome>{new PatchPreflightOutcome {Declaration=new PatchBlueprintDeclaration {Module="<owner>",Blueprint=Blueprint("<blueprint>",new MethodReference {AssemblyName="<assembly>",DeclaringType="<type>",MemberName="<method>"})}}}}};
            var html=ReportHtml.Render(report);Assert(html.Contains("Patch blueprint preflight")&&!html.Contains("<script>"));
        });
        test("ForgeWeave report HTML escapes health, filters and journal evidence",()=>{
            var report=new SessionReport {ForgeWeave=new ForgeWeaveSnapshot {Status="<script>",Handlers=new List<ForgeWeaveHandlerHealth>{new ForgeWeaveHandlerHealth {Id="<handler>",Module="<module>",Name="<name>",Event=ForgeEventKind.ForgeReady,Filter=new ForgeEventFilter {RequiredData=new Dictionary<string,string>{{"<filter>","<value>"}}}}},RecentDispatches=new List<ForgeWeaveDispatchRecord>{new ForgeWeaveDispatchRecord {Event=ForgeEventKind.ForgeReady,Status="<status>"}}}};
            var html=ReportHtml.Render(report);Assert(html.Contains("ForgeWeave extension framework")&&!html.Contains("<script>")&&!html.Contains("<handler>")&&!html.Contains("<filter>"));
        });
        test("ForgeWeave replay report HTML escapes retained evidence and outcomes",()=>{
            var report=new SessionReport {ForgeWeave=new ForgeWeaveSnapshot {
                ReplayRecords=new List<ForgeReplayRecord>{new ForgeReplayRecord {Sequence=7,Event=ForgeEventKind.ForgeReady,Context=Context.Any,OriginalStatus="<status>",Data=new Dictionary<string,string>{{"<key>","<script>"}},OriginalHandlerOutcomes=new List<ForgeHandlerOutcome>{new ForgeHandlerOutcome {HandlerId="<source-handler>",Module="<module>",Status="<source-status>"}}}},
                RecentReplays=new List<ForgeReplayResult>{new ForgeReplayResult {SourceSequence=7,ReplaySequence=8,Event=ForgeEventKind.ForgeReady,Context=Context.Any,Status="<replay-status>",RejectionReason="<reason>",HandlerOutcomes=new List<ForgeHandlerOutcome>{new ForgeHandlerOutcome {HandlerId="<replay-handler>",Status="<outcome>"}}}}
            }};
            var html=ReportHtml.Render(report);Assert(html.Contains("ForgeWeave Replay Lab")&&!html.Contains("<script>")&&!html.Contains("<key>")&&!html.Contains("<replay-handler>"));
        });
        test("Offline framework report exposes copied journal",()=>{
            var w=new ReportWorkspace();w.Open(new SessionReport {ForgeWeave=new ForgeWeaveSnapshot {RecentDispatches=new List<ForgeWeaveDispatchRecord>{new ForgeWeaveDispatchRecord {Sequence=7,Event=ForgeEventKind.ForgeReady,Status="Completed"}}}},"report");
            Assert(Json.Deserialize<List<ForgeWeaveDispatchRecord>>(w.Section("event-journal","")).Single().Sequence==7&&w.Section("framework","").Contains("RecentDispatches"));
        });
        test("Offline framework report preserves retained replay evidence",()=>{
            var w=new ReportWorkspace();w.Open(new SessionReport {ForgeWeave=new ForgeWeaveSnapshot {
                ReplayRecords=new List<ForgeReplayRecord>{new ForgeReplayRecord {Sequence=7,Event=ForgeEventKind.ForgeReady,Context=Context.Any,Data=new Dictionary<string,string>{{"suite","Calradia Forge"}}}},
                RecentReplays=new List<ForgeReplayResult>{new ForgeReplayResult {SourceSequence=7,ReplaySequence=8,Event=ForgeEventKind.ForgeReady,Context=Context.Any,Replayed=true,Status="Completed"}},
                RecentDispatches=new List<ForgeWeaveDispatchRecord>{new ForgeWeaveDispatchRecord {Sequence=8,Event=ForgeEventKind.ForgeReady,Context="Any",IsReplay=true,SourceSequence=7,Status="Completed"}}
            }},"report");
            var snapshot=Json.Deserialize<ForgeWeaveSnapshot>(w.Section("framework",""));var journal=Json.Deserialize<List<ForgeWeaveDispatchRecord>>(w.Section("event-journal",""));
            Assert(snapshot.ReplayRecords.Single().Data["suite"]=="Calradia Forge"&&snapshot.RecentReplays.Single().SourceSequence==7&&journal.Single().IsReplay&&journal.Single().SourceSequence==7);
        });
        test("Sparse replay report normalizes optional nested collections",()=>{
            var w=new ReportWorkspace();w.Open(new SessionReport {ForgeWeave=new ForgeWeaveSnapshot {RecentDispatches=new List<ForgeWeaveDispatchRecord>{new ForgeWeaveDispatchRecord {HandlerOutcomes=null}},ReplayRecords=null,RecentReplays=null}},"sparse");
            Assert(w.Report.ForgeWeave.ReplayRecords.Count==0&&w.Report.ForgeWeave.RecentReplays.Count==0&&w.Report.ForgeWeave.RecentDispatches.Single().HandlerOutcomes.Count==0);
        });
        test("Malformed replay report rejects null evidence before it can be exported",()=>{
            Throws(()=>new ReportWorkspace().Open(new SessionReport {ForgeWeave=new ForgeWeaveSnapshot {ReplayRecords=new List<ForgeReplayRecord>{null}}},"invalid"));
            Throws(()=>new ReportWorkspace().Open(new SessionReport {ForgeWeave=new ForgeWeaveSnapshot {RecentReplays=new List<ForgeReplayResult>{new ForgeReplayResult {HandlerOutcomes=new List<ForgeHandlerOutcome>{null}}}}},"invalid"));
        });
        test("Offline filtering preserves report evidence",()=>{var w=new ReportWorkspace();w.Open(new SessionReport{Logs=new List<LogEntry>{new LogEntry{Message="alpha"},new LogEntry{Message="beta"}}},"imported");Assert(Json.Deserialize<List<LogEntry>>(w.Section("logs","ALPHA")).Count==1&&w.Report.Logs.Count==2&&w.Source=="imported");});
        test("Snapshot filtering uses type and ID",()=>{var w=new ReportWorkspace();w.Open(new SessionReport{Snapshots=new List<ObjectSnapshot>{new ObjectSnapshot{Type="Hero",Id="one"},new ObjectSnapshot{Type="Item",Id="one"}}},"snapshot");Assert(Json.Deserialize<List<ObjectSnapshot>>(w.Section("snapshots","Hero|")).Count==1);});
        test("Timeout owner can cancel after processing completes",()=>{var p=new PendingRequest(CancellationToken.None);p.Release();p.Cancellation.Cancel();Assert(p.Cancellation.IsCancellationRequested);p.Release();});
        test("Queue owner can finish after timeout release",()=>{var p=new PendingRequest(CancellationToken.None);p.Cancellation.Cancel();p.Release();Assert(p.Cancellation.IsCancellationRequested);p.Release();});
    }
    static CoreModule M(string id,params string[] dependencies)=>new CoreModule{Id=id,Dependencies=dependencies.ToList()};
    static TestEngine Enabled()=>new TestEngine{TestingEnabled=true,CampaignCopyConfirmed=true};
    static PatchBlueprintCapture Capture(params PatchBlueprint[] blueprints)=>new PatchBlueprintCapture {ProviderCount=1,Declarations=blueprints.Select(blueprint=>new PatchBlueprintDeclaration {ProviderId="fixture.provider",Module="fixture",ProviderName="Fixture provider",Context=Context.Any,Blueprint=blueprint}).ToList()};
    static PatchBlueprint Blueprint(string id,MethodReference target)=>new PatchBlueprint {Id=id,Name="Fixture blueprint",Hook=PatchHookKind.Prefix,Target=target,Before=new List<string>(),After=new List<string>()};
    static void Assert(bool value){if(!value)throw new Exception("Release regression failed");}
    static void Throws(Action action){try{action();}catch{return;}throw new Exception("Expected rejection");}
    sealed class Services:ITestServices
    {
        public bool ThrowOnLog;public Context CurrentContext=>Context.Campaign;public bool IsCampaignActive=>true;
        public object GetService(Type type)=>null;
        public void Register(string module,string level,string message){if(ThrowOnLog)throw new Exception("Logging unavailable");}
    }
    sealed class MutableTest:ITestCase
    {
        public Descriptor Descriptor{get;}=new Descriptor{Id="mutable",Module="regression",Context=Context.Campaign,ChangesState=true};
        public int Runs;public bool Fail,Cleaned;
        public void Prepare(TestExecution e){}
        public void Execute(TestExecution e){Runs++;if(Fail)throw new Exception("Intentional failure");}
        public void Verify(TestExecution e){}
        public void Cleanup(TestExecution e){Cleaned=true;}
    }
    sealed class BlueprintProvider:IPatchBlueprintProvider
    {
        readonly List<PatchBlueprint> blueprints;
        public Descriptor Descriptor { get; }
        public bool Fail;
        public BlueprintProvider(string id,params PatchBlueprint[] items){Descriptor=new Descriptor {Id=id,Module="fixture",Name="Fixture provider",Context=Context.Any,ChangesState=false};blueprints=items.ToList();}
        public IEnumerable<PatchBlueprint> Describe(PatchBlueprintRequest request){if(Fail)throw new InvalidOperationException("fixture provider failure");return blueprints;}
    }
    sealed class StatefulBlueprintProvider:IPatchBlueprintProvider
    {
        public Descriptor Descriptor { get; }=new Descriptor {Id="blueprint.stateful",Module="fixture",Name="Stateful",Context=Context.Any,ChangesState=true};
        public IEnumerable<PatchBlueprint> Describe(PatchBlueprintRequest request)=>Enumerable.Empty<PatchBlueprint>();
    }
    public sealed class PatchTargetFixture
    {
        public PatchTargetFixture() { }
        public PatchTargetFixture(int value) { }
        public static void Overload(int value) { }
        public static void Overload(string value) { }
    }
    public static class UnsupportedHarmony { }
    public static class FakeOriginals
    {
        public static void Target(int value) { }
        public static void Target(string value) { }
    }
    public static class FakePatches
    {
        public static void Prefix() { }
        public static void Postfix() { }
    }
    public sealed class FakePatch
    {
        public string owner; public int priority; public int index; public string[] before; public string[] after;
        public MethodInfo PatchMethod { get; set; }
    }
    public sealed class FakePatchInfo
    {
        public List<string> Owners { get; set; }=new List<string>();
        public List<FakePatch> Prefixes { get; set; }=new List<FakePatch>();
        public List<FakePatch> Postfixes { get; set; }=new List<FakePatch>();
        public List<FakePatch> Transpilers { get; set; }=new List<FakePatch>();
        public List<FakePatch> Finalizers { get; set; }=new List<FakePatch>();
    }
    public static class FakeHarmony
    {
        static readonly MethodInfo integer=typeof(FakeOriginals).GetMethod("Target",new[]{typeof(int)});
        static readonly MethodInfo text=typeof(FakeOriginals).GetMethod("Target",new[]{typeof(string)});
        public static IEnumerable<MethodBase> GetAllPatchedMethods()=>new MethodBase[]{integer,text};
        public static FakePatchInfo GetPatchInfo(MethodBase target)
        {
            if(target==integer)return new FakePatchInfo {Owners=new List<string>{"owner.beta","owner.alpha"},Prefixes=new List<FakePatch>{new FakePatch{owner="owner.alpha",priority=700,index=2,before=new[]{"owner.before"},after=new[]{"owner.after"},PatchMethod=typeof(FakePatches).GetMethod("Prefix")}},Postfixes=new List<FakePatch>{new FakePatch{owner="owner.beta",priority=200,index=3,PatchMethod=typeof(FakePatches).GetMethod("Postfix")}}};
            return new FakePatchInfo {Owners=new List<string>{"owner.gamma"},Prefixes=new List<FakePatch>{new FakePatch{owner="owner.gamma",priority=100,index=4,PatchMethod=typeof(FakePatches).GetMethod("Prefix")}}};
        }
    }
    public sealed class BrokenPatchInfo
    {
        public IEnumerable<string> Owners { get { throw new InvalidOperationException("metadata getter failed"); } }
        public IEnumerable<FakePatch> Prefixes { get { return new FakePatch[0]; } }
        public IEnumerable<FakePatch> Postfixes { get { return new FakePatch[0]; } }
        public IEnumerable<FakePatch> Transpilers { get { return new FakePatch[0]; } }
        public IEnumerable<FakePatch> Finalizers { get { return new FakePatch[0]; } }
    }
    public static class BrokenHarmony
    {
        public static IEnumerable<MethodBase> GetAllPatchedMethods()=>new MethodBase[]{typeof(FakeOriginals).GetMethod("Target",new[]{typeof(int)})};
        public static BrokenPatchInfo GetPatchInfo(MethodBase target)=>new BrokenPatchInfo();
    }
    public static class EmptyHarmony
    {
        public static IEnumerable<MethodBase> GetAllPatchedMethods()=>new MethodBase[]{typeof(FakeOriginals).GetMethod("Target",new[]{typeof(int)})};
        public static FakePatchInfo GetPatchInfo(MethodBase target)=>null;
    }
}
