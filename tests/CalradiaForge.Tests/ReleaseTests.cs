using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using CalradiaForge.Core;
using CalradiaForge.Sdk;
using CalradiaForge.Mod;
using CalradiaForge.Mod.Commands;
using CoreModule = CalradiaForge.Core.Module;

internal static class ReleaseTests
{
    public static void Run(Action<string,Action> test)
    {
        test("English is the default product language",()=>Assert(Localization.DefaultLanguage=="en" && new Settings().Language=="en"));
        test("English text is independent of selected OS culture",()=>{var previous=Thread.CurrentThread.CurrentUICulture;try{Thread.CurrentThread.CurrentUICulture=new System.Globalization.CultureInfo("es-MX");Assert(Localization.Text("Modules",Localization.DefaultLanguage)=="Modules");}finally{Thread.CurrentThread.CurrentUICulture=previous;}});
        test("Spanish is an explicit secondary translation",()=>Assert(Localization.Text("Modules","es")=="Módulos" && Localization.Text("Modules","en")=="Modules"));
        test("Unsupported languages fall back to English",()=>Assert(Localization.Text("Modules","fr")=="Modules" && Localization.Text("Unregistered extension message","es")=="Unregistered extension message"));
        test("Protocol advertises the complete standalone developer surface",()=>{var expected=new[]{"hello","summary","scan","modules","dependencies","diagnostics","logs","inspect","pin","compare","snapshots","unpin","tests","commands","command","test-mode","confirm-copy","run","run-batch","metrics","framework","event-journal","replay","harmony","patch-blueprints","patch-preflight","hook-snapshots","hook-verify","hook-apply-plan","hook-apply-confirm","hook-revert-plan","hook-revert-confirm","hook-plan-cancel","report","export","panel-open","panel-close","language","agent-memory"};var capabilities=ForgeProtocol.Hello(SuiteInfo.Version,"1.4.8");Assert(capabilities.Contains("protocol:1")&&expected.All(capabilities.Contains)&&ForgeProtocol.Actions.SequenceEqual(expected)&&capabilities.Length==expected.Length+3);});
        test("Gauntlet hook preview rejects stale, changed and unregistered selections",()=>{
            var panelType=typeof(Runtime).Assembly.GetType("CalradiaForge.Mod.PanelViewModel",true);
            var validate=panelType.GetMethod("IsValidHookPlan",BindingFlags.Static|BindingFlags.NonPublic);
            var now=DateTimeOffset.UtcNow;
            var selected=new HookIpcSnapshot {Id="own-hook",Owner="fixture",TargetMethod="Fixture.Target",State="Registered",HasFinalizer=true};
            var status=new HookIpcStatus {Session="own-session",ServiceAvailable=true,CanManage=true,Hooks=new List<HookIpcSnapshot>{selected}};
            var plan=new HookIpcPlan {Session=status.Session,Operation="apply",Token="own-token",RequiresConfirmation=true,ExpiresAtUtc=now.AddSeconds(60).ToString("O"),Hooks=new List<HookIpcSnapshot>{Json.Deserialize<HookIpcSnapshot>(Json.Serialize(selected))}};
            Func<bool> accepted=()=> (bool)validate.Invoke(null,new object[]{plan,status,selected.Id,"apply",now});
            Assert(accepted());
            plan.ExpiresAtUtc=now.ToString("O");Assert(!accepted());plan.ExpiresAtUtc=now.AddSeconds(60).ToString("O");
            plan.Session="another-session";Assert(!accepted());plan.Session=status.Session;
            plan.Hooks[0].HasFinalizer=false;Assert(!accepted());plan.Hooks[0].HasFinalizer=true;
            plan.Hooks[0].Id="unregistered";Assert(!accepted());plan.Hooks[0].Id=selected.Id;
            plan.RequiresConfirmation=false;Assert(!accepted());plan.RequiresConfirmation=true;
            status.CanManage=false;Assert(!accepted());status.CanManage=true;
            status.Hooks.Add(selected);Assert(!accepted());status.Hooks.RemoveAt(1);
            plan.Operation="arbitrary";Assert(!accepted());
        });
        test("Hook IPC DTOs preserve only IDs and single-use confirmation metadata",()=>{
            var selection=Json.Deserialize<HookIpcSelection>(Json.Serialize(new HookIpcSelection {HookIds=new List<string>{"fixture.prefix","fixture.postfix"}}));
            var confirmation=Json.Deserialize<HookIpcConfirmation>(Json.Serialize(new HookIpcConfirmation {Token=new string('a',64)}));
            var cancel=Json.Deserialize<HookIpcCancelPlanRequest>(Json.Serialize(new HookIpcCancelPlanRequest {Session="session",Token=new string('b',64)}));
            var cancelResult=Json.Deserialize<HookIpcCancelPlanResult>(Json.Serialize(new HookIpcCancelPlanResult {Session="session",Cancelled=true}));
            var plan=Json.Deserialize<HookIpcPlan>(Json.Serialize(new HookIpcPlan {Operation="apply",Session="session",Token=confirmation.Token,ExpiresAtUtc="2026-09-29T00:00:00.0000000Z",RequiresConfirmation=true,Hooks=new List<HookIpcSnapshot>{new HookIpcSnapshot {Id="fixture.prefix",Owner="fixture",TargetMethod="Fixture.Target",State="Registered",HasPrefix=true}}}));
            var commit=Json.Deserialize<HookIpcCommit>(Json.Serialize(new HookIpcCommit {Operation="apply",Session="session",TokenConsumed=true,Succeeded=false,Partial=true,Cancelled=true,NotAttemptedIds=new List<string>{"fixture.postfix"},StopReason="fixture stop",Results=new List<HookIpcResult>{new HookIpcResult {Id="fixture.prefix",State="Applied",Succeeded=true,Verified=true}}}));
            Assert(selection.HookIds.Count==2&&selection.HookIds[0]=="fixture.prefix"&&confirmation.Token.Length==64&&plan.RequiresConfirmation&&plan.Hooks.Single().TargetMethod=="Fixture.Target"&&
                cancel.Session=="session"&&cancel.Token.Length==64&&cancelResult.Session=="session"&&cancelResult.Cancelled&&
                commit.TokenConsumed&&commit.Partial&&commit.Cancelled&&commit.NotAttemptedIds.Single()=="fixture.postfix"&&commit.StopReason=="fixture stop"&&commit.Results.Single().Verified);
        });
        test("Hook IPC plans bind to the exact menu epoch and report cancellation boundaries",()=>{
            var source=ReadRuntimeSource();
            var createStart=source.IndexOf("string CreateHookPlan(",StringComparison.Ordinal);
            var commitStart=source.IndexOf("string CommitHookPlan(",StringComparison.Ordinal);
            var coreStart=source.IndexOf("internal static HookIpcCommit CommitHookPlanCore(",commitStart,StringComparison.Ordinal);
            var cancelStart=source.IndexOf("string CancelHookPlan(",coreStart,StringComparison.Ordinal);
            var stopStart=source.IndexOf("static void StopHookCommit(",StringComparison.Ordinal);
            Assert(createStart>=0&&commitStart>createStart&&coreStart>commitStart&&cancelStart>coreStart&&stopStart>cancelStart);
            var create=source.Substring(createStart,commitStart-createStart);
            var wrapper=source.Substring(commitStart,coreStart-commitStart);
            var core=source.Substring(coreStart,cancelStart-coreStart);
            Assert(create.IndexOf("pendingHookPlan = null;",StringComparison.Ordinal)<create.IndexOf("RequireHookManagementContext();",StringComparison.Ordinal)&&
                create.Contains("planningEpoch = hookContextEpoch")&&create.Contains("cancellationToken.ThrowIfCancellationRequested()"));
            Assert(wrapper.Contains("CommitHookPlanCore(")&&wrapper.Contains("() => Log.Id")&&wrapper.Contains("() => ScreenManager.TopScreen")&&
                wrapper.Contains("() => hookContextEpoch")&&wrapper.Contains("() => ForgeApi.Hooks")&&
                wrapper.Contains("service.Apply(id)")&&wrapper.Contains("service.Revert(id)")&&wrapper.Contains("service.Verify(id)"));
            Assert(core.Contains("utcNow() > plan.ExpiresAtUtc")&&core.Contains("currentSession()")&&core.Contains("currentContextEpoch()")&&
                core.Contains("cancellationToken.IsCancellationRequested")&&core.Contains("StopHookCommit")&&core.Contains("validateSnapshots(plan, service)")&&
                core.Contains("commit.NotAttemptedIds.Count > 0"));
            Assert(create.Contains("snapshot.State == ForgeHookState.Conflict")&&create.Contains("snapshot.State == ForgeHookState.Failed")&&
                wrapper.Contains("snapshot.State == ForgeHookState.Conflict")&&wrapper.Contains("snapshot.State == ForgeHookState.Failed"));
        });
        test("Hook commit coordinator enforces single-use expiry, context, cancellation and partial apply",()=>{
            var utcNow=new DateTime(2026,10,1,12,0,0,DateTimeKind.Utc);
            var currentSession="session-a";
            object currentScreen=new object();
            var contextEpoch=4;
            var canManage=true;
            var sessionRead=false;
            var service=new ForgeHookService(()=>true);
            IForgeHookService currentService=service;
            Func<string,ForgeHookOperationResult> applyBehavior=null;
            Action<string> verifyBehavior=null;
            var applyCalls=new List<string>();
            var verifyCalls=new List<string>();
            const string token="fixture-confirmation-token";

            Runtime.PendingHookPlan NewPlan(string planToken,params string[] ids)
            {
                var hooks=ids.Select(id=>new HookIpcSnapshot {Id=id,Owner="fixture",TargetMethod="Fixture.Target",State="Registered"}).ToList();
                return new Runtime.PendingHookPlan("apply",currentSession,planToken,utcNow.AddSeconds(60),currentService,hooks,currentScreen,contextEpoch);
            }
            HookIpcCommit Commit(ref Runtime.PendingHookPlan pending,string confirmationToken,CancellationToken cancellationToken=default(CancellationToken))
            {
                return Runtime.CommitHookPlanCore(ref pending,"apply",confirmationToken,
                    ()=>{sessionRead=true;return currentSession;},()=>utcNow,()=>currentScreen,()=>contextEpoch,()=>canManage,()=>{},()=>currentService,
                    (plan,activeService)=>null,cancellationToken,
                    (activeService,id)=>{applyCalls.Add(id);return applyBehavior==null?new ForgeHookOperationResult(id,ForgeHookState.Applied,true,"applied"):applyBehavior(id);},
                    (activeService,id)=>new ForgeHookOperationResult(id,ForgeHookState.Reverted,true,"reverted"),
                    (activeService,id)=>{verifyCalls.Add(id);verifyBehavior?.Invoke(id);return new ForgeHookOperationResult(id,ForgeHookState.Applied,true,"verified");});
            }
            void ExpectFailure(ref Runtime.PendingHookPlan pending,string confirmationToken,string expected)
            {
                try { Commit(ref pending,confirmationToken); }
                catch(InvalidOperationException error) { Assert(error.Message==expected);return; }
                throw new Exception("Expected hook commit failure: "+expected);
            }

            Runtime.PendingHookPlan pending=NewPlan(token,"single");
            ExpectFailure(ref pending,"wrong-token","Confirmation token is unknown, already used, or for another operation.");
            Assert(pending!=null&&applyCalls.Count==0);
            var single=Commit(ref pending,token);
            Assert(single.Succeeded&&single.TokenConsumed&&single.Results.Single().Id=="single"&&pending==null);
            ExpectFailure(ref pending,token,"Confirmation token is unknown, already used, or for another operation.");
            Assert(applyCalls.SequenceEqual(new[]{"single"}));

            sessionRead=false;
            pending=NewPlan(token,"expired");
            utcNow=pending.ExpiresAtUtc.AddTicks(1);
            ExpectFailure(ref pending,token,"Confirmation token expired; create a new plan.");
            Assert(pending==null&&!sessionRead&&applyCalls.Count==1);
            ExpectFailure(ref pending,token,"Confirmation token is unknown, already used, or for another operation.");
            utcNow=new DateTime(2026,10,1,12,0,0,DateTimeKind.Utc);

            var contextFailures=new[]{
                new { Name="session", Expected="The game session changed after the plan was created." },
                new { Name="screen", Expected="The main-menu screen changed after planning; the single-use plan was discarded." },
                new { Name="epoch", Expected="The main-menu screen changed after planning; the single-use plan was discarded." },
                new { Name="service", Expected="The hook service changed or became unavailable after planning." }
            };
            foreach(var scenario in contextFailures)
            {
                currentSession="session-a";currentScreen=new object();contextEpoch=4;currentService=service;
                pending=NewPlan(token,"guarded");
                if(scenario.Name=="session")currentSession="session-b";
                else if(scenario.Name=="screen")currentScreen=new object();
                else if(scenario.Name=="epoch")contextEpoch++;
                else currentService=new ForgeHookService(()=>true);
                ExpectFailure(ref pending,token,scenario.Expected);
                Assert(pending==null&&applyCalls.Count==1);
            }

            currentSession="session-a";currentScreen=new object();contextEpoch=4;currentService=service;
            pending=NewPlan(token,"first","second","third");
            using(var cancellation=new CancellationTokenSource())
            {
                verifyBehavior=id=>{if(id=="first")cancellation.Cancel();};
                var cancelled=Commit(ref pending,token,cancellation.Token);
                Assert(cancelled.Cancelled&&cancelled.Partial&&!cancelled.Succeeded&&cancelled.Results.Count==1&&
                    cancelled.Results[0].Id=="first"&&cancelled.Results[0].Succeeded&&cancelled.Results[0].Verified&&
                    cancelled.NotAttemptedIds.SequenceEqual(new[]{"second","third"})&&applyCalls.Last()=="first"&&
                    verifyCalls.Last()=="first");
            }
            verifyBehavior=null;

            applyCalls.Clear();verifyCalls.Clear();
            pending=NewPlan(token,"first","failed","remaining");
            applyBehavior=id=>id=="failed"
                ?new ForgeHookOperationResult(id,ForgeHookState.Failed,false,"fixture apply failure")
                :new ForgeHookOperationResult(id,ForgeHookState.Applied,true,"applied");
            var partial=Commit(ref pending,token);
            Assert(!partial.Succeeded&&partial.Partial&&!partial.Cancelled&&partial.Results.Count==2&&
                partial.Results[0].Id=="first"&&partial.Results[0].Succeeded&&partial.Results[1].Id=="failed"&&
                !partial.Results[1].Succeeded&&partial.NotAttemptedIds.SequenceEqual(new[]{"remaining"})&&
                applyCalls.SequenceEqual(new[]{"first","failed"})&&verifyCalls.SequenceEqual(new[]{"first","failed"}));
            applyBehavior=null;
        });
        test("Hook revert coordinator reverses order and reports cancellation and partial failures",()=>{
            var utcNow=new DateTime(2026,10,1,12,0,0,DateTimeKind.Utc);
            var screen=new object();
            const string token="fixture-revert-confirmation-token";
            var service=new ForgeHookService(()=>true);
            var reverted=new List<string>();
            var verified=new List<string>();
            Func<string,ForgeHookOperationResult> revertBehavior=null;
            Action<string> afterVerify=null;

            Runtime.PendingHookPlan NewPlan(params string[] ids)
            {
                var hooks=ids.Select(id=>new HookIpcSnapshot {Id=id,Owner="fixture",TargetMethod="Fixture.Target",State="Applied"}).ToList();
                return new Runtime.PendingHookPlan("revert","session-a",token,utcNow.AddSeconds(60),service,hooks,screen,4);
            }
            HookIpcCommit Commit(ref Runtime.PendingHookPlan pending,CancellationToken cancellationToken=default(CancellationToken))
            {
                return Runtime.CommitHookPlanCore(ref pending,"revert",token,()=>"session-a",()=>utcNow,()=>screen,()=>4,()=>true,()=>{},()=>service,
                    (plan,activeService)=>null,cancellationToken,
                    (activeService,id)=>throw new Exception("A revert plan must never apply a hook."),
                    (activeService,id)=>{reverted.Add(id);return revertBehavior==null?new ForgeHookOperationResult(id,ForgeHookState.Reverted,true,"reverted"):revertBehavior(id);},
                    (activeService,id)=>{verified.Add(id);afterVerify?.Invoke(id);return new ForgeHookOperationResult(id,ForgeHookState.Reverted,id!="middle-failure","verified");});
            }

            Runtime.PendingHookPlan pending=NewPlan("first","middle","last");
            var success=Commit(ref pending);
            Assert(success.Succeeded&&success.TokenConsumed&&pending==null&&success.Results.Select(item=>item.Id).SequenceEqual(new[]{"last","middle","first"})&&
                reverted.SequenceEqual(new[]{"last","middle","first"})&&verified.SequenceEqual(reverted)&&success.NotAttemptedIds.Count==0);

            reverted.Clear();verified.Clear();
            pending=NewPlan("first","middle","last");
            using(var cancellation=new CancellationTokenSource())
            {
                afterVerify=id=>{if(id=="last")cancellation.Cancel();};
                var cancelled=Commit(ref pending,cancellation.Token);
                Assert(cancelled.Cancelled&&!cancelled.Succeeded&&cancelled.Partial&&cancelled.Results.Count==1&&
                    cancelled.Results[0].Id=="last"&&cancelled.Results[0].Succeeded&&cancelled.Results[0].Verified&&
                    cancelled.NotAttemptedIds.SequenceEqual(new[]{"middle","first"})&&reverted.SequenceEqual(new[]{"last"})&&
                    verified.SequenceEqual(new[]{"last"}));
            }
            afterVerify=null;

            reverted.Clear();verified.Clear();
            revertBehavior=id=>id=="middle"
                ?new ForgeHookOperationResult(id,ForgeHookState.Failed,false,"fixture revert failure")
                :new ForgeHookOperationResult(id,ForgeHookState.Reverted,true,"reverted");
            pending=NewPlan("first","middle","last");
            var partial=Commit(ref pending);
            Assert(!partial.Succeeded&&partial.Partial&&!partial.Cancelled&&partial.Results.Count==3&&
                partial.Results[0].Id=="last"&&partial.Results[1].Id=="middle"&&!partial.Results[1].Succeeded&&
                partial.Results[2].Id=="first"&&partial.Results[2].Succeeded&&partial.NotAttemptedIds.Count==0&&
                reverted.SequenceEqual(new[]{"last","middle","first"})&&verified.SequenceEqual(reverted));
        });
        test("A consumer binary compiled against the v12 registry can load on SDK v13",()=>{
            var path=Environment.GetEnvironmentVariable("CALRADIAFORGE_LEGACY_V12_CLIENT_DLL");
            Assert(!string.IsNullOrWhiteSpace(path)&&File.Exists(path));
            var assembly=Assembly.LoadFrom(path);
            var clientType=assembly.GetType("CalradiaForge.LegacySdkV12.LegacyRegistryProvider",true);
            var client=Activator.CreateInstance(clientType);
            Assert(client is IForgeRegistry);
            var sdkReference=assembly.GetReferencedAssemblies().Single(name=>name.Name=="CalradiaForge.Sdk");
            Assert(sdkReference.Version==new Version(25,2,0,0)&&
                (int)clientType.GetMethod("GetCompiledForgeApiVersion").Invoke(client,null)==12&&ForgeApi.Version==13);
            var legacyRegistry=new TestEngine();
            clientType.GetMethod("RegisterLegacyContracts").Invoke(client,new object[]{legacyRegistry});
            Assert(legacyRegistry.TestCount==1&&legacyRegistry.CommandCount==1);
            var legacyServices=(ITestServices)clientType.GetMethod("CreateTestServices").Invoke(client,null);
            Assert(legacyRegistry.Execute("legacy.v12.test",legacyServices,148,CancellationToken.None).Status=="Passed"&&
                legacyRegistry.ExecuteCommand("legacy.v12.command",string.Empty,legacyServices,CancellationToken.None)=="legacy-v12-command"&&
                legacyRegistry.Diagnose(legacyServices).Single().Code=="legacy-v12-provider");
            ForgeApi.Connect((IForgeRegistry)client);
            try
            {
                Assert(ReferenceEquals(ForgeApi.Registry,client)&&ForgeApi.Hooks==null&&ForgeApi.Patches==null);
            }
            finally { ForgeApi.Disconnect(); }
        });
        test("Hook registration rejects ordering cycles among known hooks on the same target",()=>{
            var service=new ForgeHookService(()=>true);
            var target=typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)});
            service.Register(new ForgeHookDefinition {Id="order.a",Owner="fixture",Target=target,Prefix=_=>{},Before=new List<string>{"CalradiaForge.Hook.order.b"},After=new List<string>{"order.c"}});
            service.Register(new ForgeHookDefinition {Id="order.b",Owner="fixture",Target=target,Prefix=_=>{},Before=new List<string>{"order.c"}});
            Exception observed=null;
            try
            {
                service.Register(new ForgeHookDefinition {Id="order.c",Owner="fixture",Target=target,Prefix=_=>{}});
            }
            catch(Exception error) { observed=error; }
            Assert(observed is InvalidOperationException&&observed.Message.Contains("cycle")&&
                service.GetSnapshots().Select(item=>item.Id).OrderBy(id=>id,StringComparer.Ordinal).SequenceEqual(new[]{"order.a","order.b"}));

            var otherTargetService=new ForgeHookService(()=>true);
            var otherTarget=typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(string)});
            otherTargetService.Register(new ForgeHookDefinition {Id="order.cross.a",Owner="fixture",Target=target,Prefix=_=>{},Before=new List<string>{"order.cross.b"}});
            otherTargetService.Register(new ForgeHookDefinition {Id="order.cross.b",Owner="fixture",Target=otherTarget,Prefix=_=>{},After=new List<string>{"order.cross.a"}});
            Assert(otherTargetService.GetSnapshots().Count==2);

        });
        test("Hook console Apply and Revert require the Runtime single-use plan confirmation",()=>{
            var source=ReadForgeCommandsSource();
            var applyStart=source.IndexOf("public static string HookApply(",StringComparison.Ordinal);
            var revertStart=source.IndexOf("public static string HookRevert(",StringComparison.Ordinal);
            var confirmStart=source.IndexOf("public static string HookConfirm(",StringComparison.Ordinal);
            var planStart=source.IndexOf("static string PrepareConsoleHookPlan(",StringComparison.Ordinal);
            var revertAllStart=source.IndexOf("public static string RevertAll(",StringComparison.Ordinal);
            Assert(applyStart>=0&&revertStart>applyStart&&confirmStart>revertStart&&planStart>confirmStart&&revertAllStart>planStart);
            var apply=source.Substring(applyStart,revertStart-applyStart);
            var revert=source.Substring(revertStart,confirmStart-revertStart);
            var confirm=source.Substring(confirmStart,planStart-confirmStart);
            var plan=source.Substring(planStart,revertAllStart-planStart);
            Assert(apply.Contains("PrepareConsoleHookPlan(runtime, \"apply\"")&&!apply.Contains("service.Apply("));
            Assert(revert.Contains("PrepareConsoleHookPlan(runtime, \"revert\"")&&!revert.Contains("service.Revert(")&&!revert.Contains("service.RevertAll("));
            Assert(confirm.Contains("hook-apply-confirm")&&confirm.Contains("hook-revert-confirm")&&
                confirm.Contains("new HookIpcConfirmation { Token = token }")&&confirm.Contains("runtime.Handle"));
            Assert(plan.Contains("hook-apply-plan")&&plan.Contains("hook-revert-plan")&&
                plan.Contains("new HookIpcSelection { HookIds = hookIds }")&&plan.Contains("Expires (UTC):")&&
                plan.Contains("Single-use confirmation token:")&&plan.Contains("target=")&&plan.Contains("plan.Operation"));
            Assert(source.Contains("cf.hook_confirm <apply|revert> <token>"));
        });
        test("Console revert filters inactive records before preparing an explicit plan",()=>{
            var selector=typeof(ForgeCommands).GetMethod("EligibleConsoleHookRevertIds",BindingFlags.NonPublic|BindingFlags.Static);
            Assert(selector!=null);
            Func<string,ForgeHookState,ForgeHookSnapshot> snapshot=(id,state)=>new ForgeHookSnapshot(id,"owner","target",true,false,null,null,null,state,string.Empty);
            var inventory=new[]{snapshot("registered",ForgeHookState.Registered),snapshot("applied",ForgeHookState.Applied),
                snapshot("reverted",ForgeHookState.Reverted),snapshot("conflict",ForgeHookState.Conflict),
                snapshot("failed",ForgeHookState.Failed),snapshot("unsupported",ForgeHookState.Unsupported)};
            var eligible=(List<string>)selector.Invoke(null,new object[]{inventory});
            Assert(eligible.SequenceEqual(new[]{"applied","conflict","failed"}));
            var inactive=(List<string>)selector.Invoke(null,new object[]{new[]{inventory[0],inventory[2],inventory[5]}});
            Assert(inactive.Count==0);
            var single=(List<string>)selector.Invoke(null,new object[]{new[]{inventory[1]}});
            Assert(single.SequenceEqual(new[]{"applied"}));
            var source=ReadForgeCommandsSource();
            var start=source.IndexOf("public static string HookRevert(",StringComparison.Ordinal);
            var end=source.IndexOf("static List<string> EligibleConsoleHookRevertIds(",start,StringComparison.Ordinal);
            var revert=source.Substring(start,end-start);
            Assert(revert.Contains("EligibleConsoleHookRevertIds(snapshots.Where(item => selected.Contains(item.Id)))")&&
                revert.Contains("No selected hooks are eligible for Revert")&&
                revert.IndexOf("selectedIds.Count == 0",StringComparison.Ordinal)<revert.IndexOf("PrepareConsoleHookPlan(runtime, \"revert\"",StringComparison.Ordinal));
        });
        test("Hook dispatch skips callbacks outside the host-approved context",()=>{
            var source=ReadCoreHookSource();
            var start=source.IndexOf("static object Dispatch(",StringComparison.Ordinal);
            var end=source.IndexOf("static object InvokeOriginal(",start,StringComparison.Ordinal);
            Assert(start>=0&&end>start);
            var dispatch=source.Substring(start,end-start);
            Assert(dispatch.Contains("if (!activation.Service.CallbackAllowed(entry)) return InvokeOriginal(originalInvoker, original, instance, callbackArgs)")&&
                dispatch.Contains("if (entry.Prefix != null && !activation.Service.TryInvokeCallback(entry, entry.Prefix, invocation))")&&
                dispatch.Contains("if (!activation.Service.CallbackAllowed(entry)) return InvokeOriginal(originalInvoker, original, instance, originalArgs)")&&
                dispatch.Contains("if (entry.Postfix != null && !activation.Service.TryInvokeCallback(entry, entry.Postfix, invocation))")&&
                dispatch.Contains("if (!activation.Service.CallbackAllowed(entry)) return result;")&&
                dispatch.Contains("if (!entered)")&&dispatch.Contains("InvokeOriginalFallback(original, instance, args, isStatic)")&&
                dispatch.Contains("finally")&&dispatch.Contains("activation.Exit();"));
            var callbackGateStart=source.IndexOf("bool CallbackAllowed(",StringComparison.Ordinal);
            var callbackInvokeStart=source.IndexOf("bool TryInvokeCallback(",callbackGateStart,StringComparison.Ordinal);
            var originalInvokerStart=source.IndexOf("static object InvokeOriginal(",callbackInvokeStart,StringComparison.Ordinal);
            Assert(callbackGateStart>=0&&callbackInvokeStart>callbackGateStart&&originalInvokerStart>callbackInvokeStart&&
                source.Substring(callbackGateStart,callbackInvokeStart-callbackGateStart).Contains("Volatile.Read(ref callbacksEnabled)")&&
                source.Substring(callbackGateStart,callbackInvokeStart-callbackGateStart).Contains("entry.CanInvoke != null && entry.CanInvoke()"));
            var callbackInvoker=source.Substring(callbackInvokeStart,originalInvokerStart-callbackInvokeStart);
            Assert(callbackInvoker.Contains("lock (callbackInvocationGate)")&&callbackInvoker.Contains("if (!CallbackAllowed(entry)) return false;")&&
                callbackInvoker.IndexOf("CallbackAllowed(entry)",StringComparison.Ordinal)<callbackInvoker.IndexOf("callback(invocation)",StringComparison.Ordinal));
        });
        test("Hook plan cancellation is exact-session and exact-token only",()=>{
            var source=ReadRuntimeSource();
            var start=source.IndexOf("string CancelHookPlan(",StringComparison.Ordinal);
            var end=source.IndexOf("static void StopHookCommit(",start,StringComparison.Ordinal);
            Assert(start>=0&&end>start);
            var cancel=source.Substring(start,end-start);
            Assert(cancel.Contains("request.Session, Log.Id, StringComparison.Ordinal")&&
                cancel.Contains("plan.Session, Log.Id, StringComparison.Ordinal")&&
                cancel.Contains("plan.Token, request.Token, StringComparison.Ordinal")&&
                cancel.Contains("if (cancelled) pendingHookPlan = null"));
        });
        test("Hook mutation rejects lookalike main-menu screens",()=>{
            var forged=typeof(ReleaseTests).Assembly.GetType("TaleWorlds.MountAndBlade.GauntletUI.GauntletInitialScreen",false);
            Assert(!Runtime.IsOfficialMainMenuScreenType(null) &&
                !Runtime.IsOfficialMainMenuScreenType(typeof(MainMenuScreen)) &&
                forged!=null&&!Runtime.IsOfficialMainMenuScreenType(forged) &&
                ReadRuntimeSource().Contains("TaleWorlds.MountAndBlade.GauntletUI.GauntletInitialScreen, TaleWorlds.MountAndBlade.GauntletUI") &&
                ReadRuntimeSource().Contains("screenType == officialType"));
        });
        test("Forge hook capability is optional and registration is inert by default",()=>{
            Assert(ForgeApi.Version==13);
            var engine=new TestEngine();
            var callbackCount=0;
            ForgeApi.Connect(engine);
            try
            {
                var capability=ForgeApi.Hooks;
                var handle=capability.Register(new ForgeHookDefinition {Id="fixture.hook.inert",Owner="fixture",Target=typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}),Prefix=_=>callbackCount++});
                var registered=handle.Snapshot;
                var applied=handle.Apply();
                Assert(capability!=null&&ReferenceEquals(capability,(IForgeHookService)engine)&&registered.State==ForgeHookState.Registered&&registered.HasPrefix&&callbackCount==0&&!applied.Succeeded&&applied.State==ForgeHookState.Registered&&callbackCount==0);
            }
            finally { ForgeApi.Disconnect(); }
        });
        test("Hook collective cleanup resolves clean apply failures and retains uncertain conflicts",()=>{
            var ownerService=HookServiceWithState("fixture.clean-failed.owner",ForgeHookState.Failed,false);
            var ownerResults=ownerService.RevertOwner("fixture");
            Assert(ownerResults.Count==1&&ownerResults[0].Succeeded&&ownerResults[0].State==ForgeHookState.Reverted&&
                ownerService.GetSnapshots().Single().State==ForgeHookState.Reverted);

            var allService=HookServiceWithState("fixture.clean-failed.all",ForgeHookState.Failed,false);
            var allResults=allService.RevertAll();
            Assert(allResults.Count==1&&allResults[0].Succeeded&&allResults[0].State==ForgeHookState.Reverted&&
                allService.GetSnapshots().Single().State==ForgeHookState.Reverted);

            var disconnectService=HookServiceWithState("fixture.clean-failed.disconnect",ForgeHookState.Failed,false,false);
            string disconnectReason;
            Assert(disconnectService.CanDisconnect(out disconnectReason)&&string.IsNullOrEmpty(disconnectReason));
            disconnectService.Disconnect();
            Assert(disconnectService.GetSnapshots().Single().State==ForgeHookState.Reverted&&
                !disconnectService.Apply("fixture.clean-failed.disconnect").Succeeded);
            disconnectService.Reconnect();
            Assert(disconnectService.GetSnapshots().Single().State==ForgeHookState.Reverted);

            var conflictService=HookServiceWithState("fixture.uncertain.conflict",ForgeHookState.Conflict,true);
            var conflictResults=conflictService.RevertAll();
            Assert(conflictResults.Count==1&&!conflictResults[0].Succeeded&&conflictResults[0].State==ForgeHookState.Conflict&&
                conflictService.GetSnapshots().Single().State==ForgeHookState.Conflict);
            Throws(conflictService.Disconnect);
            Assert(conflictService.GetSnapshots().Single().State==ForgeHookState.Conflict);
        });
        test("Hook bulk reverts report unresolved records when the main-menu mutation gate is closed",()=>{
            var appliedOwner=HookServiceWithState("fixture.gated.applied.owner",ForgeHookState.Applied,true,false);
            var ownerResults=appliedOwner.RevertOwner("fixture");
            Assert(ownerResults.Count==1&&!ownerResults[0].Succeeded&&ownerResults[0].State==ForgeHookState.Applied&&
                ownerResults[0].Detail.Contains("approved main-menu context")&&appliedOwner.GetSnapshots().Single().State==ForgeHookState.Applied);

            var conflictAll=HookServiceWithState("fixture.gated.conflict.all",ForgeHookState.Conflict,true,false);
            var allResults=conflictAll.RevertAll();
            Assert(allResults.Count==1&&!allResults[0].Succeeded&&allResults[0].State==ForgeHookState.Conflict&&
                allResults[0].Detail.Contains("approved main-menu context")&&allResults[0].Detail.Contains("Seeded post-cleanup lifecycle state")&&
                conflictAll.GetSnapshots().Single().State==ForgeHookState.Conflict);

            var cleanFailedOwner=HookServiceWithState("fixture.gated.clean-failed.owner",ForgeHookState.Failed,false,false);
            var cleanFailedOwnerResults=cleanFailedOwner.RevertOwner("fixture");
            Assert(cleanFailedOwnerResults.Count==1&&cleanFailedOwnerResults[0].Succeeded&&
                cleanFailedOwnerResults[0].State==ForgeHookState.Reverted&&cleanFailedOwner.GetSnapshots().Single().State==ForgeHookState.Reverted);

            var cleanFailedAll=HookServiceWithState("fixture.gated.clean-failed.all",ForgeHookState.Failed,false,false);
            var cleanFailedAllResults=cleanFailedAll.RevertAll();
            Assert(cleanFailedAllResults.Count==1&&cleanFailedAllResults[0].Succeeded&&
                cleanFailedAllResults[0].State==ForgeHookState.Reverted&&cleanFailedAll.GetSnapshots().Single().State==ForgeHookState.Reverted);
        });
        test("ForgeApi retains the published hook service when disconnect throws",()=>{
            var host=new HookLifecycleFixtureHost {ThrowOnDisconnect=true};
            ForgeApi.Connect(host);
            try
            {
                Throws(ForgeApi.Disconnect);
                Assert(ReferenceEquals(ForgeApi.Registry,host)&&ReferenceEquals(ForgeApi.Hooks,host)&&host.GetSnapshots().Single().State==ForgeHookState.Applied);
                host.ThrowOnDisconnect=false;host.ResolveOnDisconnect=true;
                ForgeApi.Disconnect();
                Assert(ForgeApi.Registry==null&&ForgeApi.Hooks==null&&host.DisconnectCount==2);
            }
            finally
            {
                if(ReferenceEquals(ForgeApi.Registry,host))
                {
                    host.ThrowOnDisconnect=false;host.ResolveOnDisconnect=true;
                    ForgeApi.Disconnect();
                }
            }
        });
        test("ForgeApi refuses to unpublish hooks when Disconnect leaves an unresolved record",()=>{
            var host=new HookLifecycleFixtureHost();
            ForgeApi.Connect(host);
            try
            {
                Throws(ForgeApi.Disconnect);
                Assert(ReferenceEquals(ForgeApi.Registry,host)&&ReferenceEquals(ForgeApi.Hooks,host)&&host.GetSnapshots().Single().State==ForgeHookState.Applied);
                host.SetState(ForgeHookState.Reverted);
                ForgeApi.Disconnect();
                Assert(ForgeApi.Registry==null&&ForgeApi.Hooks==null);
            }
            finally
            {
                if(ReferenceEquals(ForgeApi.Registry,host))
                {
                    host.SetState(ForgeHookState.Reverted);
                    ForgeApi.Disconnect();
                }
            }
        });
        test("ForgeApi retains the previous host when replacement hook cleanup fails",()=>{
            var previous=new HookLifecycleFixtureHost {ThrowOnDisconnect=true};
            var incoming=new HookLifecycleFixtureHost();
            incoming.SetState(ForgeHookState.Reverted);
            ForgeApi.Connect(previous);
            try
            {
                Throws(()=>ForgeApi.Connect(incoming));
                Assert(ReferenceEquals(ForgeApi.Registry,previous)&&ReferenceEquals(ForgeApi.Hooks,previous)&&previous.GetSnapshots().Single().State==ForgeHookState.Applied);
                previous.ThrowOnDisconnect=false;previous.ResolveOnDisconnect=true;
                ForgeApi.Connect(incoming);
                Assert(ReferenceEquals(ForgeApi.Registry,incoming)&&ReferenceEquals(ForgeApi.Hooks,incoming));
            }
            finally
            {
                if(ReferenceEquals(ForgeApi.Registry,incoming))ForgeApi.Disconnect();
                else if(ReferenceEquals(ForgeApi.Registry,previous))
                {
                    previous.ThrowOnDisconnect=false;previous.ResolveOnDisconnect=true;
                    ForgeApi.Disconnect();
                }
            }
        });
        test("ForgeApi reopens the previous host when the incoming lifecycle reconnect fails",()=>{
            var previous=new HookLifecycleFixtureHost {ResolveOnDisconnect=true};
            var incoming=new HookLifecycleFixtureHost {ThrowOnReconnect=true};
            incoming.SetState(ForgeHookState.Reverted);
            ForgeApi.Connect(previous);
            try
            {
                Throws(()=>ForgeApi.Connect(incoming));
                Assert(ReferenceEquals(ForgeApi.Registry,previous)&&ReferenceEquals(ForgeApi.Hooks,previous)&&
                    previous.GetSnapshots().Single().State==ForgeHookState.Reverted&&previous.Accepting&&previous.ReconnectCount==2);
                incoming.ThrowOnReconnect=false;
                ForgeApi.Connect(incoming);
                Assert(ReferenceEquals(ForgeApi.Registry,incoming)&&ReferenceEquals(ForgeApi.Hooks,incoming));
            }
            finally
            {
                if(ReferenceEquals(ForgeApi.Registry,incoming))ForgeApi.Disconnect();
                else if(ReferenceEquals(ForgeApi.Registry,previous))
                {
                    previous.ResolveOnDisconnect=true;
                    ForgeApi.Disconnect();
                }
            }
        });
        test("ForgeApi keeps the patch capability published when disconnect retains unresolved patches",()=>{
            var host=new PatchLifecycleFixtureHost();
            ForgeApi.Connect(host);
            try
            {
                Throws(ForgeApi.Disconnect);
                Assert(ReferenceEquals(ForgeApi.Registry,host)&&ReferenceEquals(ForgeApi.Patches,host)&&
                    host.GetSnapshots().Single().State==ForgePatchState.Applied&&host.DisconnectCount==1);
                host.SetState(ForgePatchState.Reverted);
                ForgeApi.Disconnect();
                Assert(ForgeApi.Registry==null&&ForgeApi.Patches==null&&host.DisconnectCount==2);
            }
            finally
            {
                if(ReferenceEquals(ForgeApi.Registry,host))
                {
                    host.SetState(ForgePatchState.Reverted);
                    ForgeApi.Disconnect();
                }
            }
        });
        test("Forge hook registration rejects duplicate IDs and open generic targets while accepting runtime-prefix-shaped IDs",()=>{
            var engine=new TestEngine();
            engine.Register(new MutableTest());
            Throws(()=>engine.Register(new ForgeHookDefinition {Id="mutable",Owner="fixture",Target=typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}),Prefix=_=>{}}));
            var service=new ForgeHookService(()=>true);
            Throws(()=>service.Register(new ForgeHookDefinition {Id="fixture.generic",Owner="fixture",Target=typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Generic)),Prefix=_=>{}}));
            var byRef=typeof(int).GetMethod("TryParse",new[]{typeof(string),typeof(int).MakeByRefType()});
            Throws(()=>service.Register(new ForgeHookDefinition {Id="fixture.byref",Owner="fixture",Target=byRef,Prefix=_=>{}}));
            const string prefixedId="CalradiaForge.Hook.fixture.prefixed";
            service.Register(new ForgeHookDefinition {Id=prefixedId,Owner="fixture",Target=typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}),Prefix=_=>{}});
            Assert(service.GetSnapshots().Single(snapshot=>snapshot.Id==prefixedId).State==ForgeHookState.Registered);
        });
        test("Forge hook ordering rejects prefixed self references, normalized duplicates, and contradictions",()=>{
            var service=new ForgeHookService(()=>true);
            var target=typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)});
            Throws(()=>service.Register(new ForgeHookDefinition {Id="fixture.short-self",Owner="fixture",Target=target,Prefix=_=>{},Before=new List<string>{"fixture.short-self"}}));
            Throws(()=>service.Register(new ForgeHookDefinition {Id="fixture.self",Owner="fixture",Target=target,Prefix=_=>{},Before=new List<string>{"CalradiaForge.Hook.fixture.self"}}));
            Throws(()=>service.Register(new ForgeHookDefinition {Id="fixture.self-case",Owner="fixture",Target=target,Prefix=_=>{},After=new List<string>{"calradiaforge.hook.fixture.self-case"}}));
            Throws(()=>service.Register(new ForgeHookDefinition {Id="fixture.padded-self ",Owner="fixture",Target=target,Prefix=_=>{},Before=new List<string>{"fixture.padded-self"}}));
            Throws(()=>service.Register(new ForgeHookDefinition {Id="fixture.duplicate",Owner="fixture",Target=target,Prefix=_=>{},Before=new List<string>{"fixture.other","CalradiaForge.Hook.fixture.other"}}));
            Throws(()=>service.Register(new ForgeHookDefinition {Id="fixture.contradiction",Owner="fixture",Target=target,Prefix=_=>{},Before=new List<string>{"fixture.other"},After=new List<string>{"CalradiaForge.Hook.fixture.other"}}));
        });
        test("Forge hook self-order validation accepts an opaque ID with the runtime prefix",()=>{
            const string prefixedId="CalradiaForge.Hook.fixture.prefixed-self";
            var service=new ForgeHookService(()=>true);
            var target=typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)});
            Throws(()=>service.Register(new ForgeHookDefinition {Id=prefixedId,Owner="fixture",Target=target,Prefix=_=>{},Before=new List<string>{"CalradiaForge.Hook."+prefixedId}}));
        });
        test("Forge hook graph resolves fully qualified tags for prefix-shaped IDs and rejects cycles",()=>{
            var service=new ForgeHookService(()=>true);
            var target=typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)});
            const string prefixedTargetId="CalradiaForge.Hook.fixture.prefixed-target";
            service.Register(new ForgeHookDefinition {Id="fixture.prefixed-source",Owner="fixture",Target=target,Prefix=_=>{},After=new List<string>{"CalradiaForge.Hook."+prefixedTargetId}});
            Throws(()=>service.Register(new ForgeHookDefinition {Id=prefixedTargetId,Owner="fixture",Target=target,Prefix=_=>{},After=new List<string>{"fixture.prefixed-source"}}));
            Assert(!service.GetSnapshots().Any(snapshot=>snapshot.Id==prefixedTargetId));
        });
        test("Patch console controls are explicit and absent from the read-only IPC action list",()=>{
            var help=ForgeCommands.Help(new List<string>());
            Assert(help.Contains("cf.patch_status [owner]")&&help.Contains("cf.patch_revert <id|owner|all>")&&
                ForgeProtocol.Actions.All(action=>action!="patch_status"&&action!="patch_revert"));
        });
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
            Assert(result.Supported&&result.DiscoveredMethodCount==2&&target.HasMultipleOwners&&target.Owners.SequenceEqual(new[]{"owner.alpha","owner.beta"})&&
                target.Patches.Select(patch=>patch.Kind).SequenceEqual(new[]{"Prefix","Postfix","Transpiler","Finalizer"})&&
                prefix.Owner=="owner.alpha"&&prefix.Priority==700&&prefix.Before.Single()=="owner.before"&&prefix.After.Single()=="owner.after"&&prefix.PatchMethod=="Prefix"&&
                target.Patches.Single(patch=>patch.Kind=="Transpiler").PatchMethod=="Transpiler"&&target.Patches.Single(patch=>patch.Kind=="Finalizer").PatchMethod=="Finalizer");
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
        test("Patch preflight resolves exact callback signatures without invoking callbacks",()=>{
            PatchCallbackFixture.Invoked=false;
            var blueprint=Blueprint("blueprint.callback",MethodReference.From(typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)})));
            blueprint.PatchMethod=MethodReference.From(typeof(PatchCallbackFixture).GetMethod(nameof(PatchCallbackFixture.Callback)));
            var result=PatchPreflightEngine.Inspect(Capture(blueprint),new[]{typeof(PatchTargetFixture).Assembly},"Any");
            Assert(result.ResolvedCount==1&&result.Outcomes.Single().CallbackResolved&&result.Outcomes.Single().ResolvedCallbackSignature=="()"&&!PatchCallbackFixture.Invoked);
        });
        test("Patch preflight blocks missing or non-method callbacks",()=>{
            var target=MethodReference.From(typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}));
            var missing=Blueprint("blueprint.no-callback",target);missing.PatchMethod=null;
            var constructor=Blueprint("blueprint.constructor-callback",target);
            constructor.PatchMethod=MethodReference.From(typeof(PatchCallbackConstructorFixture).GetConstructor(Type.EmptyTypes));
            var result=PatchPreflightEngine.Inspect(Capture(missing,constructor),new[]{typeof(PatchTargetFixture).Assembly},"Any");
            var missingOutcome=result.Outcomes.Single(outcome=>outcome.Declaration.Blueprint.Id=="blueprint.no-callback");
            var constructorOutcome=result.Outcomes.Single(outcome=>outcome.Declaration.Blueprint.Id=="blueprint.constructor-callback");
            if (!(result.ResolvedCount==0&&missingOutcome.Status=="Callback not declared"&&constructorOutcome.Status=="Invalid callback"))
                throw new Exception("Callback preflight mismatch: count="+result.ResolvedCount+", missing="+missingOutcome.Status+", constructor="+constructorOutcome.Status+", outcomes="+result.Outcomes.Count);
        });
        test("Patch preflight validates generic arity for target and callback references",()=>{
            var target=MethodReference.From(typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Generic)));
            var callbackMethod=typeof(PatchCallbackFixture).GetMethod(nameof(PatchCallbackFixture.GenericCallback));
            var valid=Blueprint("blueprint.generic",target);valid.PatchMethod=MethodReference.From(callbackMethod);
            var invalid=Blueprint("blueprint.bad-generic-arity",target);invalid.PatchMethod=MethodReference.From(callbackMethod);invalid.PatchMethod.GenericArity++;
            var result=PatchPreflightEngine.Inspect(Capture(valid,invalid),new[]{typeof(PatchTargetFixture).Assembly},"Any");
            var resolved=result.Outcomes.Single(outcome=>outcome.Declaration.Blueprint.Id=="blueprint.generic");
            var rejected=result.Outcomes.Single(outcome=>outcome.Declaration.Blueprint.Id=="blueprint.bad-generic-arity");
            if (!(result.ResolvedCount==1&&resolved.Resolved&&resolved.CallbackResolved&&resolved.ResolvedCallbackSignature.Contains("T")&&!rejected.Resolved&&rejected.Status=="Callback Member not found"))
                throw new Exception("Generic preflight mismatch: count="+result.ResolvedCount+", valid="+resolved.Status+"/"+resolved.ResolvedCallbackSignature+", invalid="+rejected.Status+"/"+rejected.ResolvedCallbackSignature);
        });
        test("MethodReference.From normalizes closed generic methods and constructed generic declaring types",()=>{
            var closedMethod=typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Generic)).MakeGenericMethod(typeof(string));
            var closedTypeMethod=typeof(GenericParameterScopeFixture<int>).GetMethod(nameof(GenericParameterScopeFixture<int>.Target)).MakeGenericMethod(typeof(string));
            var genericReference=MethodReference.From(closedMethod);
            var closedTypeReference=MethodReference.From(closedTypeMethod);
            var result=PatchPreflightEngine.Inspect(Capture(
                Blueprint("blueprint.closed-generic-method",genericReference),
                Blueprint("blueprint.closed-generic-type",closedTypeReference)),
                new[]{typeof(ReleaseTests).Assembly},"Any");
            Assert(genericReference.ParameterTypes.Single().FullName=="!!0"&&genericReference.ReturnType.FullName=="!!0"&&
                genericReference.GenericArity==1&&genericReference.DeclaringType==typeof(PatchTargetFixture).FullName&&
                closedTypeReference.DeclaringType==typeof(GenericParameterScopeFixture<>).FullName&&
                closedTypeReference.ParameterTypes[0].FullName=="!0"&&closedTypeReference.ParameterTypes[1].FullName=="!!0"&&
                result.ResolvedCount==2&&result.Outcomes.All(outcome=>outcome.Resolved));
        });
        test("Patch preflight requires concrete parameter and return assembly identity despite duplicate full names",()=>{
            var suffix=Guid.NewGuid().ToString("N");
            var payloadA=DefineDuplicateTypeAssembly("PatchPreflight.PayloadA."+suffix,"PatchCollision.Payload");
            var payloadB=DefineDuplicateTypeAssembly("PatchPreflight.PayloadB."+suffix,"PatchCollision.Payload");
            var targetAssembly=AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("PatchPreflight.Target."+suffix),AssemblyBuilderAccess.Run);
            var targetModule=targetAssembly.DefineDynamicModule("PatchPreflight.Target."+suffix);
            var targetBuilder=targetModule.DefineType("PatchCollision.Target",TypeAttributes.Public|TypeAttributes.Abstract|TypeAttributes.Sealed);
            var parameterBuilder=targetBuilder.DefineMethod("Accept",MethodAttributes.Public|MethodAttributes.Static,typeof(void),new[]{payloadA});
            parameterBuilder.GetILGenerator().Emit(OpCodes.Ret);
            var returnBuilder=targetBuilder.DefineMethod("Create",MethodAttributes.Public|MethodAttributes.Static,payloadA,Type.EmptyTypes);
            returnBuilder.GetILGenerator().Emit(OpCodes.Ldnull);
            returnBuilder.GetILGenerator().Emit(OpCodes.Ret);
            var targetType=targetBuilder.CreateType();
            var exactParameter=MethodReference.From(targetType.GetMethod("Accept"));
            var exactReturn=MethodReference.From(targetType.GetMethod("Create"));
            var missingParameterAssembly=MethodReference.From(targetType.GetMethod("Accept"));
            missingParameterAssembly.ParameterTypes[0].AssemblyName=null;
            var missingReturnAssembly=MethodReference.From(targetType.GetMethod("Create"));
            missingReturnAssembly.ReturnType.AssemblyName=" ";
            var result=PatchPreflightEngine.Inspect(Capture(
                Blueprint("blueprint.exact-type-assembly",exactParameter),
                Blueprint("blueprint.omitted-parameter-assembly",missingParameterAssembly),
                Blueprint("blueprint.omitted-return-assembly",missingReturnAssembly),
                Blueprint("blueprint.exact-return-assembly",exactReturn)),
                new[]{targetAssembly,payloadA.Assembly,payloadB.Assembly,typeof(ReleaseTests).Assembly},"Any");
            var parameterRejected=result.Outcomes.Single(outcome=>outcome.Declaration.Blueprint.Id=="blueprint.omitted-parameter-assembly");
            var returnRejected=result.Outcomes.Single(outcome=>outcome.Declaration.Blueprint.Id=="blueprint.omitted-return-assembly");
            Assert(payloadA.FullName==payloadB.FullName&&payloadA.Assembly!=payloadB.Assembly&&
                result.ResolvedCount==2&&result.Outcomes.Single(outcome=>outcome.Declaration.Blueprint.Id=="blueprint.exact-type-assembly").Resolved&&
                result.Outcomes.Single(outcome=>outcome.Declaration.Blueprint.Id=="blueprint.exact-return-assembly").Resolved&&
                !parameterRejected.Resolved&&parameterRejected.Status=="Invalid target"&&
                !returnRejected.Resolved&&returnRejected.Status=="Invalid target");
        });
        test("Patch preflight distinguishes type and method generic parameter positions",()=>{
            var method=typeof(GenericParameterScopeFixture<>).GetMethod(nameof(GenericParameterScopeFixture<object>.Target));
            var exact=MethodReference.From(method);
            var mismatched=MethodReference.From(method);
            mismatched.ParameterTypes[0]=TypeReference.From(method.GetGenericArguments()[0]);
            var wrongAssembly=MethodReference.From(method);
            wrongAssembly.ParameterTypes[0].AssemblyName="Unrelated.Assembly";
            var result=PatchPreflightEngine.Inspect(Capture(
                Blueprint("blueprint.generic-scope",exact),Blueprint("blueprint.generic-scope-mismatch",mismatched),Blueprint("blueprint.generic-scope-assembly-mismatch",wrongAssembly)),
                new[]{typeof(ReleaseTests).Assembly},"Any");
            var resolved=result.Outcomes.Single(outcome=>outcome.Declaration.Blueprint.Id=="blueprint.generic-scope");
            var rejected=result.Outcomes.Single(outcome=>outcome.Declaration.Blueprint.Id=="blueprint.generic-scope-mismatch");
            var assemblyRejected=result.Outcomes.Single(outcome=>outcome.Declaration.Blueprint.Id=="blueprint.generic-scope-assembly-mismatch");
            Assert(exact.ParameterTypes[0].FullName=="!0"&&exact.ParameterTypes[1].FullName=="!!0"&&
                resolved.Resolved&&!rejected.Resolved&&rejected.Status=="Member not found"&&
                !assemblyRejected.Resolved&&assemblyRejected.Status=="Member not found");
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
        test("Patch preflight rejects padded or oversized IDs without truncation aliases",()=>{
            var target=MethodReference.From(typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}));
            var padded=Blueprint(" blueprint.padded ",target);
            var longPrefix=new string('x',512);
            var oversizedA=Blueprint(longPrefix+"a",target);var oversizedB=Blueprint(longPrefix+"b",target);
            var result=PatchPreflightEngine.Inspect(Capture(padded,oversizedA,oversizedB),new[]{typeof(PatchTargetFixture).Assembly},"Any");
            Assert(result.Outcomes.All(outcome=>!outcome.Resolved)&&result.Outcomes.Any(outcome=>outcome.Notes.Any(note=>note.Contains("leading or trailing whitespace")))&&result.Outcomes.Count(outcome=>outcome.Notes.Any(note=>note.Contains("cannot exceed 512 characters")))==2);
        });
        test("Patch preflight remains structurally useful without a patch runtime",()=>{
            var target=MethodReference.From(typeof(string).GetMethod(nameof(string.IsNullOrEmpty),new[]{typeof(string)}));
            var result=PatchPreflightEngine.Inspect(Capture(Blueprint("blueprint.no-runtime",target)),new[]{typeof(string).Assembly,typeof(PatchCallbackFixture).Assembly},"Any");
            Assert(result.ResolvedCount==1&&result.ReviewCount==0&&result.Outcomes.Single().Resolved&&result.Status.StartsWith("No blocking issues"));
        });
        test("Patch preflight reports self ordering as an independent review item",()=>{
            var target=MethodReference.From(typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}));
            var blueprint=Blueprint("blueprint.order",target);blueprint.Before.Add("blueprint.order");
            var result=PatchPreflightEngine.Inspect(Capture(blueprint),new[]{typeof(PatchTargetFixture).Assembly},"Any");
            Assert(result.ResolvedCount==1&&result.ReviewCount==1&&result.Status.StartsWith("Review required")&&result.Findings.Any(f=>f.Code=="patch_order_self"));
        });
        test("Patch preflight reports conflicting targets, unknown references and cycles",()=>{
            var target=MethodReference.From(typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}));
            var first=Blueprint("blueprint.first",target);first.Before.Add("blueprint.second");first.After.Add("blueprint.unknown");
            var second=Blueprint("blueprint.second",target);second.Before.Add("blueprint.first");
            var result=PatchPreflightEngine.Inspect(Capture(first,second),new[]{typeof(PatchTargetFixture).Assembly},"Any");
            Assert(result.ResolvedCount==2&&result.ReviewCount>=5&&result.Findings.Any(f=>f.Code=="patch_target_conflict")&&result.Findings.Any(f=>f.Code=="patch_order_missing")&&result.Findings.Any(f=>f.Code=="patch_order_cycle"));
        });
        test("Patch preflight reports duplicated and ambiguous ordering references",()=>{
            var target=MethodReference.From(typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}));
            var first=Blueprint("blueprint.first",target);first.Before.Add("blueprint.second");first.Before.Add("blueprint.second");
            var second=Blueprint("blueprint.second",target);var duplicate=Blueprint("blueprint.second",target);
            var result=PatchPreflightEngine.Inspect(Capture(first,second,duplicate),new[]{typeof(PatchTargetFixture).Assembly},"Any");
            Assert(result.Findings.Any(f=>f.Code=="patch_order_duplicate")&&result.Findings.Any(f=>f.Code=="patch_order_ambiguous")&&result.Outcomes.Where(outcome=>outcome.Declaration.Blueprint.Id=="blueprint.second").All(outcome=>!outcome.Resolved));
        });
        test("Patch preflight reports blank ordering references",()=>{
            var target=MethodReference.From(typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}));
            var blueprint=Blueprint("blueprint.blank-order",target);blueprint.Before.Add(" ");
            var result=PatchPreflightEngine.Inspect(Capture(blueprint),new[]{typeof(PatchTargetFixture).Assembly},"Any");
            Assert(result.Findings.Any(f=>f.Code=="patch_order_invalid")&&result.ReviewCount==1);
        });
        test("Patch preflight reports ordering references beyond its bounded inspection",()=>{
            var target=MethodReference.From(typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}));
            var blueprint=Blueprint("blueprint.order-limit",target);
            for(var index=0;index<33;index++)blueprint.Before.Add("owner."+index);
            var result=PatchPreflightEngine.Inspect(Capture(blueprint),new[]{typeof(PatchTargetFixture).Assembly},"Any");
            Assert(result.Findings.Any(f=>f.Code=="patch_order_limit"));
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
    static ForgeHookService HookServiceWithState(string id,ForgeHookState state,bool inApplyOrder,bool mayMutate=true)
    {
        var service=new ForgeHookService(()=>mayMutate);
        service.Register(new ForgeHookDefinition {
            Id=id,Owner="fixture",Target=typeof(PatchTargetFixture).GetMethod(nameof(PatchTargetFixture.Overload),new[]{typeof(int)}),Prefix=_=>{}
        });
        var entriesField=typeof(ForgeHookService).GetField("entries",BindingFlags.Instance|BindingFlags.NonPublic);
        var entries=(System.Collections.IDictionary)entriesField.GetValue(service);
        var entry=entries[id];
        entry.GetType().GetProperty("State",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(entry,state,null);
        entry.GetType().GetProperty("Detail",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(entry,"Seeded post-cleanup lifecycle state",null);
        if(inApplyOrder)
        {
            var orderField=typeof(ForgeHookService).GetField("applyOrder",BindingFlags.Instance|BindingFlags.NonPublic);
            ((List<string>)orderField.GetValue(service)).Add(id);
        }
        return service;
    }
    static PatchBlueprintCapture Capture(params PatchBlueprint[] blueprints)=>new PatchBlueprintCapture {ProviderCount=1,Declarations=blueprints.Select(blueprint=>new PatchBlueprintDeclaration {ProviderId="fixture.provider",Module="fixture",ProviderName="Fixture provider",Context=Context.Any,Blueprint=blueprint}).ToList()};
    static PatchBlueprint Blueprint(string id,MethodReference target)=>new PatchBlueprint {Id=id,Name="Fixture blueprint",Hook=PatchHookKind.Prefix,Target=target,PatchMethod=MethodReference.From(typeof(PatchCallbackFixture).GetMethod(nameof(PatchCallbackFixture.Callback))),Before=new List<string>(),After=new List<string>()};
    static Type DefineDuplicateTypeAssembly(string assemblyName,string fullTypeName)
    {
        var assembly=AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(assemblyName),AssemblyBuilderAccess.Run);
        var module=assembly.DefineDynamicModule(assemblyName);
        return module.DefineType(fullTypeName,TypeAttributes.Public|TypeAttributes.Class).CreateType();
    }
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
        public static T Generic<T>(T value) => value;
    }
    static string ReadRuntimeSource()
    {
        var starts=new[]{new DirectoryInfo(Environment.CurrentDirectory),new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory)};
        foreach(var start in starts)
            for(var directory=start;directory!=null;directory=directory.Parent)
            {
                var path=Path.Combine(directory.FullName,"src","CalradiaForge.Mod","Runtime.cs");
                if(File.Exists(path))return File.ReadAllText(path);
            }
        throw new FileNotFoundException("Could not locate the CalradiaForge.Mod Runtime.cs source for hook IPC contract checks.");
    }
    static string ReadForgeCommandsSource()
    {
        var starts=new[]{new DirectoryInfo(Environment.CurrentDirectory),new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory)};
        foreach(var start in starts)
            for(var directory=start;directory!=null;directory=directory.Parent)
            {
                var path=Path.Combine(directory.FullName,"src","CalradiaForge.Mod","Commands","ForgeCommands.cs");
                if(File.Exists(path))return File.ReadAllText(path);
            }
        throw new FileNotFoundException("Could not locate CalradiaForge.Mod ForgeCommands.cs for console hook plan contract checks.");
    }
    static string ReadCoreHookSource()
    {
        var starts=new[]{new DirectoryInfo(Environment.CurrentDirectory),new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory)};
        foreach(var start in starts)
            for(var directory=start;directory!=null;directory=directory.Parent)
            {
                var path=Path.Combine(directory.FullName,"src","CalradiaForge.Core","ForgeHookService.cs");
                if(File.Exists(path))return File.ReadAllText(path);
            }
        throw new FileNotFoundException("Could not locate the CalradiaForge.Core ForgeHookService source for dispatch contract checks.");
    }
    public sealed class GenericParameterScopeFixture<TClass>
    {
        public void Target<TMethod>(TClass classValue,TMethod methodValue) { }
    }
    public static class PatchCallbackFixture
    {
        public static bool Invoked;
        public static void Callback() { Invoked=true; }
        public static void GenericCallback<T>(T value) { Invoked=true; }
    }
    public sealed class PatchCallbackConstructorFixture
    {
        public PatchCallbackConstructorFixture() { }
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
        public static void Transpiler() { }
        public static void Finalizer() { }
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
            if(target==integer)return new FakePatchInfo {Owners=new List<string>{"owner.beta","owner.alpha"},Prefixes=new List<FakePatch>{new FakePatch{owner="owner.alpha",priority=700,index=2,before=new[]{"owner.before"},after=new[]{"owner.after"},PatchMethod=typeof(FakePatches).GetMethod("Prefix")}},Postfixes=new List<FakePatch>{new FakePatch{owner="owner.beta",priority=200,index=3,PatchMethod=typeof(FakePatches).GetMethod("Postfix")}},Transpilers=new List<FakePatch>{new FakePatch{owner="owner.alpha",priority=300,index=4,PatchMethod=typeof(FakePatches).GetMethod("Transpiler")}},Finalizers=new List<FakePatch>{new FakePatch{owner="owner.beta",priority=400,index=5,PatchMethod=typeof(FakePatches).GetMethod("Finalizer")}}};
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
    sealed class HookLifecycleFixtureHost:IForgeRegistry,IForgeHookService,IForgeHookServiceLifecycle
    {
        ForgeHookState state=ForgeHookState.Applied;
        public bool ThrowOnDisconnect;
        public bool ThrowOnReconnect;
        public bool ResolveOnDisconnect;
        public bool Accepting;
        public int DisconnectCount;
        public int ReconnectCount;
        public void Register(ITestCase test) { }
        public void Register(ICommand command) { }
        public void Register(IDiagnosticProvider provider) { }
        public IForgeHookHandle Register(ForgeHookDefinition definition)=>throw new NotSupportedException();
        public IReadOnlyList<ForgeHookSnapshot> GetSnapshots(string owner=null)
        {
            if(!string.IsNullOrEmpty(owner)&&!string.Equals(owner,"fixture",StringComparison.OrdinalIgnoreCase))return Array.Empty<ForgeHookSnapshot>();
            return new[]{new ForgeHookSnapshot("fixture.lifecycle","fixture","Fixture.Target",true,false,null,null,null,state,"fixture state")};
        }
        public ForgeHookOperationResult Apply(string hookId)=>throw new NotSupportedException();
        public ForgeHookOperationResult Verify(string hookId)=>throw new NotSupportedException();
        public ForgeHookOperationResult Revert(string hookId)=>throw new NotSupportedException();
        public IReadOnlyList<ForgeHookOperationResult> RevertOwner(string owner)=>throw new NotSupportedException();
        public IReadOnlyList<ForgeHookOperationResult> RevertAll()=>throw new NotSupportedException();
        public void Disconnect()
        {
            DisconnectCount++;
            if(ThrowOnDisconnect)throw new InvalidOperationException("Fixture disconnect failed before cleanup.");
            if(ResolveOnDisconnect)state=ForgeHookState.Reverted;
            Accepting=false;
        }
        public void Reconnect()
        {
            ReconnectCount++;
            if(ThrowOnReconnect)throw new InvalidOperationException("Fixture reconnect failed before reopening.");
            Accepting=true;
        }
        public void SetState(ForgeHookState value)=>state=value;
    }
    sealed class PatchLifecycleFixtureHost:IForgeRegistry,IForgePatchService
    {
        ForgePatchState state=ForgePatchState.Applied;
        public int DisconnectCount;
        public void Register(ITestCase test) { }
        public void Register(ICommand command) { }
        public void Register(IDiagnosticProvider provider) { }
        public IForgePatchHandle ApplyMethodReplacement(string patchId,string owner,MethodInfo target,MethodInfo replacement)=>throw new NotSupportedException();
        public IReadOnlyList<ForgePatchSnapshot> GetSnapshots(string owner=null)
        {
            if(!string.IsNullOrEmpty(owner)&&!string.Equals(owner,"fixture",StringComparison.OrdinalIgnoreCase))return Array.Empty<ForgePatchSnapshot>();
            return new[]{new ForgePatchSnapshot("fixture.patch.lifecycle","fixture","Fixture.Target","Fixture.Replacement",state,state==ForgePatchState.Reverted)};
        }
        public ForgePatchVerification Verify(string patchId)=>new ForgePatchVerification(patchId,state,state==ForgePatchState.Reverted,"fixture state");
        public ForgePatchRevertResult Revert(string patchId)
        {
            if(state==ForgePatchState.Reverted)return new ForgePatchRevertResult(patchId,state,true,"Already reverted.");
            return new ForgePatchRevertResult(patchId,state,false,"Fixture patch remains unresolved.");
        }
        public IReadOnlyList<ForgePatchRevertResult> RevertOwner(string owner)=>Array.Empty<ForgePatchRevertResult>();
        public IReadOnlyList<ForgePatchRevertResult> RevertAll()=>Array.Empty<ForgePatchRevertResult>();
        public void Disconnect() { DisconnectCount++; }
        public void SetState(ForgePatchState value)=>state=value;
    }
    sealed class MainMenuScreen { }
}

namespace TaleWorlds.MountAndBlade.GauntletUI
{
    // Exact-full-name test double. Runtime must compare the CLR Type object, not names or prefixes.
    internal sealed class GauntletInitialScreen { }
}
