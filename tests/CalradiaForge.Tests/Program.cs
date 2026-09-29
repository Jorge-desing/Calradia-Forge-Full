using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalradiaForge.Core;
using CalradiaForge.Sdk;
using CalradiaForge.Mod;
using CalradiaForge.Examples;
using CalradiaForge.Tests;

class Program
{
    static int passed,failed;static string temp;

    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            var name = new System.Reflection.AssemblyName(resolveArgs.Name).Name;
            string gameBin = @"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client";
            string target = Path.Combine(gameBin, name + ".dll");
            if (File.Exists(target))
            {
                try { return System.Reflection.Assembly.LoadFrom(target); } catch { }
            }
            return null;
        };
        temp=Path.Combine(Path.GetTempPath(),"CalradiaForgeTests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
        try {
            ReleaseTests.Run(Test);
            XmlValidationTests.Run(Test);
            SharedLibraryTests.Run(Test);
            ForgeWeaveTests.Run(Test);
            SdkFeaturesTests.Run(Test);
            ModSettingsPathSafetyTests.Run(Test);
            ForgeAnalysisTests.Run(Test);
            AdvancedToolsTests.Run(Test);
            ClanCharacterProgressionTests.Run(Test);
            AgentCognitiveMemoryTests.Run(Test);
            NativeEvidencePanelTests.Run(Test);
            GameThreadActionQueueTests.Run(Test);
            AssemblyWorkbenchTests.Run(Test, temp);
            
            Test("JSON roundtrip with unicode and HTML",()=>{var r=new Request{Argument="Español <script>\n"};Equal(Json.Deserialize<Request>(Json.Serialize(r)).Argument,r.Argument);});
            Test("Atomic settings replacement",()=>{var p=Path.Combine(temp,"settings.json");Json.Save(p,new Settings{Language="en"});Json.Save(p,new Settings{Language="es"});Equal(Json.Deserialize<Settings>(File.ReadAllText(p)).Language,"es");});
            Test("Healthy module",()=>{var d=Folder("healthy");Module(d,"A","");Equal(ModuleValidator.Inspect(d).Findings.Count,0);});
            Test("Dependency version difference is a warning",()=>{var d=Folder("version-difference");Module(d,"A","<DependedModule Id='B' DependentVersion='v2'/>");Module(d,"B","");var finding=ModuleValidator.Inspect(d).Findings.Single();Equal(finding.Code,"dependency_version_difference");Equal(finding.Level,"Warning");True(finding.File.EndsWith("SubModule.xml"));});
            Test("Matching dependency version has no finding",()=>{var d=Folder("version-match");Module(d,"A","<DependedModule Id='B' DependentVersion='v1'/>");Module(d,"B","");Equal(ModuleValidator.Inspect(d).Findings.Count,0);});
            Test("Absent optional versioned dependency is allowed",()=>{var d=Folder("version-optional");Module(d,"A","<DependedModule Id='B' Optional='true' DependentVersion='v2'/>");Equal(ModuleValidator.Inspect(d).Findings.Count,0);});
            Test("Declared incompatible module is reported",()=>{var d=Folder("incompatible");Module(d,"A","");Module(d,"B","");var manifest=Path.Combine(d,"A","SubModule.xml");File.WriteAllText(manifest,File.ReadAllText(manifest).Replace("</Module>","<IncompatibleModules><Module Id='B'/></IncompatibleModules></Module>"));var inspected=ModuleValidator.Inspect(d);True(inspected.Findings.Any(f=>f.Code=="incompatible_module"));True(inspected.Modules.Single(m=>m.Id=="A").Dependencies.Count==0);});
            Test("Module XML traversal reports its depth bound",()=>{var d=Folder("module-depth");Module(d,"A","");var current=Path.Combine(d,"A");for(var i=0;i<70;i++){current=Path.Combine(current,"d");Directory.CreateDirectory(current);}File.WriteAllText(Path.Combine(current,"nested.xml"),"<root/>");var inspected=ModuleValidator.Inspect(d);True(inspected.Findings.Any(f=>f.Code=="analysis_scan_incomplete"));});
            Test("Engine asset XML needs review rather than confirmed failure",()=>{var d=Folder("asset-xml");Module(d,"A","");var asset=Path.Combine(d,"A","SceneObj");Directory.CreateDirectory(asset);File.WriteAllText(Path.Combine(asset,"scene.xml"),"<bad>");var finding=ModuleValidator.Inspect(d).Findings.Single();Equal(finding.Code,"asset_xml_review");Equal(finding.Level,"Warning");});
            Test("Missing dependency",()=>{var d=Folder("missing");Module(d,"A","<DependedModule Id='B'/>");True(ModuleValidator.Inspect(d).Findings.Any(h=>h.Code=="dependency_missing"));});
            Test("Optional dependency",()=>{var d=Folder("optional");Module(d,"A","<DependedModule Id='B' Optional='true'/>");Equal(ModuleValidator.Inspect(d).Findings.Count,0);});
            Test("Dependency cycle",()=>{var d=Folder("cycle");Module(d,"A","<DependedModule Id='B'/>");Module(d,"B","<DependedModule Id='A'/>");True(ModuleValidator.Inspect(d).Findings.Any(h=>h.Code=="dependency_cycle"));});
            Test("Duplicate module ID",()=>{var d=Folder("duplicate");Module(d,"A","");Directory.CreateDirectory(Path.Combine(d,"B"));File.Copy(Path.Combine(d,"A","SubModule.xml"),Path.Combine(d,"B","SubModule.xml"));True(ModuleValidator.Inspect(d).Findings.Any(h=>h.Code=="duplicate_id"));});
            Test("Malformed XML",()=>{var d=Folder("xml");Module(d,"A","");File.WriteAllText(Path.Combine(d,"A","bad.xml"),"<bad>");True(ModuleValidator.Inspect(d).Findings.Any(h=>h.Code=="xml_invalid"));});
            Test("DTD rejected",()=>{var d=Folder("dtd");Module(d,"A","");File.WriteAllText(Path.Combine(d,"A","bad.xml"),"<!DOCTYPE x [<!ENTITY ext SYSTEM 'file:///secret'>]><x>&ext;</x>");True(ModuleValidator.Inspect(d).Findings.Any(h=>h.Code=="xml_invalid"));});
            Test("Missing DLL",()=>{var d=Folder("dll");Module(d,"A","");var f=Path.Combine(d,"A","SubModule.xml");File.WriteAllText(f,File.ReadAllText(f).Replace("</Module>","<SubModules><SubModule><DLLName value='missing.dll'/></SubModule></SubModules></Module>"));True(ModuleValidator.Inspect(d).Findings.Any(h=>h.Code=="dll_missing"));});
            Test("Server-only DLL is not required by Steam client",()=>{var d=Folder("serverdll");Module(d,"A","");var f=Path.Combine(d,"A","SubModule.xml");File.WriteAllText(f,File.ReadAllText(f).Replace("</Module>","<SubModules><SubModule><DLLName value='server.dll'/><Tags><Tag key='DedicatedServerType' value='custom'/></Tags></SubModule></SubModules></Module>"));True(!ModuleValidator.Inspect(d).Findings.Any(h=>h.Code=="dll_missing"));});
            Test("Log bounded under load",()=>{var l=new SessionLog();Parallel.For(0,5000,n=>l.Add("test","Info",n.ToString()));Equal(l.Deserialize().Count,2000);});
            Test("Log filters",()=>{var l=new SessionLog();l.Add("Alpha","Warning","needle");l.Add("Beta","Info","hay");Equal(l.Deserialize("warning").Count,1);Equal(l.Deserialize("ALPHA").Count,1);Equal(l.Deserialize(l.Id).Count,2);});
            Test("Log persistence",()=>{var l=new SessionLog();l.Add("test","Info","ok");var d=Folder("logs");l.Persist(d);True(File.Exists(Path.Combine(d,l.Id+".json")));});
            Test("Log persistence clears a recovered write error",()=>{
                var log=new SessionLog();var folder=Folder("log-recovery");var path=Path.Combine(folder,log.Id+".json");
                log.Add("test","Info","before");log.Persist(folder);var original=File.ReadAllText(path);
                using(var held=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)) {
                    log.Add("test","Info","after");log.Persist(folder);
                    True(!string.IsNullOrEmpty(log.PersistenceError));Equal(File.ReadAllText(path),original);
                }
                log.Persist(folder);
                Equal(Json.Deserialize<List<LogEntry>>(File.ReadAllText(path)).Count,2);
                True(string.IsNullOrEmpty(log.PersistenceError));
                Equal(Directory.GetFiles(folder,"*.tmp").Length,0);
            });
            Test("Concurrent session persistence keeps the latest entries",()=>{
                var log=new SessionLog();var folder=Folder("log-concurrent");
                Parallel.For(0,16,i=>{log.Add("test","Info",i.ToString());log.Persist(folder);});
                var entries=Json.Deserialize<List<LogEntry>>(File.ReadAllText(Path.Combine(folder,log.Id+".json")));
                Equal(entries.Count,16);True(string.IsNullOrEmpty(log.PersistenceError));
            });
            Test("HTML report escapes untrusted text",()=>{var p=Path.Combine(temp,"report");ReportExporter.Export(p,new SessionReport{Session="<script>alert(1)</script>"});True(!File.ReadAllText(p+".html").Contains("<script>"));True(File.Exists(p+".json"));});
            Test("Snapshot differences",()=>{var a=new ObjectSnapshot{Type="Hero",Id="x",Properties=new Dictionary<string,string>{{"Gold","1"}}};var b=Json.Deserialize<ObjectSnapshot>(Json.Serialize(a));b.Properties["Gold"]="2";True(SnapshotComparer.Compare(a,b).Contains("1 → 2"));True(SnapshotComparer.Compare(a,null).Contains("no longer"));});
            Test("Duplicate extension rejected",()=>{var m=new TestEngine();m.Register(new FakeTest());Throws(()=>m.Register(new FakeTest()));});
            Test("Wrong context rejected",()=>{var m=new TestEngine();m.Register(new FakeTest());Throws(()=>m.Execute("fake",new Services{CurrentContext=Context.Any},1,CancellationToken.None));});
            Test("Mutation gate rejected",()=>{var m=new TestEngine();m.Register(new FakeTest());Throws(()=>m.Execute("fake",new Services(),1,CancellationToken.None));});
            Test("Campaign copy required",()=>{var m=new TestEngine{TestingEnabled=true};m.Register(new FakeTest());Throws(()=>m.Execute("fake",new Services(),1,CancellationToken.None));});
            foreach(var stage in new[]{"prepare","execute","assert","cleanup"}) {var captured=stage;Test("Cleanup after "+stage+" failure",()=>{var m=Enabled();var p=new FakeTest{Failure=captured};m.Register(p);var r=m.Execute("fake",new Services(),1,CancellationToken.None);Equal(r.Status,"Failed");True(p.Cleaned);if(captured=="cleanup")True(r.CleanupError!=null);});}
            Test("Cancellation still cleans",()=>{var m=Enabled();var p=new FakeTest();m.Register(p);var r=m.Execute("fake",new Services(),1,new CancellationToken(true));Equal(r.Status,"Cancelled");True(p.Cleaned);});
            Test("Deterministic SDK random seed",()=>{var a=new TestExecution(new Services(),148,CancellationToken.None);var b=new TestExecution(new Services(),148,CancellationToken.None);Equal(a.Random.Next(),b.Random.Next());});
            Test("Inventory example restores state",()=>{var m=Enabled();var s=new Services();m.Register(new InventoryExample());var r=m.Execute("examples.inventory",s,148,CancellationToken.None);Equal(r.Status,"Passed");Equal(s.Lab.Grain,10);});
            Test("Troops example removes generated agent",()=>{var m=Enabled();var s=new Services{CurrentContext=Context.Mission};m.Register(new TroopExample());var r=m.Execute("examples.troops",s,148,CancellationToken.None);Equal(r.Status,"Passed");True(!s.Lab.Agent);});
            Test("Extension diagnostic failure isolated",()=>{var m=new TestEngine();m.Register(new BadProvider());Equal(m.Diagnose(new Services()).Single().Code,"extension_error");});
            Test("English is default and Spanish available",()=>{Equal(Localization.Text("Summary","en"),"Summary");Equal(Localization.Text("Summary","es"),"Resumen");});
            Test("Named pipe roundtrip and reconnect",()=>PipeRoundtrip());
            if(args.Length>0 && Directory.Exists(args[0])) Test("Installed modules can be scanned",()=>{var d=ModuleValidator.Inspect(args[0]);True(d.Modules.Any(m=>m.Id=="Native" && m.Version=="v1.4.8"));Console.WriteLine("Installed modules: "+d.Modules.Count+"; findings: "+d.Findings.Count);Json.Save(Path.Combine("artifacts","installed-module-scan.json"),d);});
        } finally {Directory.Delete(temp,true);}
        Console.WriteLine("RESULT: "+passed+" passed, "+failed+" failed");Environment.ExitCode=failed==0?0:1;
    }
    static void PipeRoundtrip()
    {
        using(var server=new PipeServer()) {server.Start();for(int i=0;i<2;i++){
            var client=Task.Run(()=>{using(var p=new NamedPipeClientStream(".",server.Name,PipeDirection.InOut)){p.Connect(3000);using(var w=new StreamWriter(p,new System.Text.UTF8Encoding(false),4096,true){AutoFlush=true})using(var r=new StreamReader(p,System.Text.Encoding.UTF8,false,4096,true)){var s=new Request{Action="hello"};w.WriteLine(Json.Serialize(s));var result=Json.Deserialize<Response>(r.ReadLine());Equal(result.Id,s.Id);True(result.Success);}}});
            var start=DateTime.UtcNow;while(!client.IsCompleted && (DateTime.UtcNow-start).TotalSeconds<5){server.Process((s,t)=>new Response{Id=s.Id,Success=true,Data="ok"});Thread.Sleep(5);}True(client.Wait(1000));client.GetAwaiter().GetResult();}}
    }
    static string Folder(string n){var d=Path.Combine(temp,n);Directory.CreateDirectory(d);return d;}
    static void Module(string d,string id,string deps){var p=Path.Combine(d,id);Directory.CreateDirectory(p);File.WriteAllText(Path.Combine(p,"SubModule.xml"),"<Module><Id value='"+id+"'/><Version value='v1'/><DependedModules>"+deps+"</DependedModules></Module>");}
    static TestEngine Enabled()=>new TestEngine{TestingEnabled=true,CampaignCopyConfirmed=true};
    // Console.Out writes each line immediately; explicit Flush calls here forced
    // two extra synchronized stream operations for every test case.
    static void Test(string n,Action a){try{Console.WriteLine("RUN  "+n);a();passed++;Console.WriteLine("PASS "+n);}catch(Exception e){failed++;Console.WriteLine("FAIL "+n+": "+e);}}
    static void True(bool b){if(!b)throw new Exception("Assertion failed");}
    static void Equal<T>(T a,T b){if(!Equals(a,b))throw new Exception(a+" != "+b);}
    static void Throws(Action a){try{a();}catch{return;}throw new Exception("Expected exception");}
    sealed class Services:ITestServices {public bool IsCampaignActive=>CurrentContext==Context.Campaign;public Context CurrentContext{get;set;}=Context.Campaign;public FakeLab Lab=new FakeLab();public object GetService(Type t)=>Lab;public void Register(string m,string n,string s){}}
    sealed class FakeLab:IGameLaboratory {public int Grain=10;public bool Agent;public int GetGrainCount()=>Grain;public void ChangeGrain(int d){Grain+=d;}public object SpawnAgent(int s){Agent=true;return this;}public bool AgentExists(object a)=>Agent;public void RemoveAgent(object a){Agent=false;}}
    sealed class FakeTest:ITestCase {public string Failure;public bool Cleaned;public Descriptor Descriptor=>new Descriptor{Id="fake",Module="test",Context=Context.Campaign,ChangesState=true};void Fail(string s){if(Failure==s)throw new Exception(s);}public void Prepare(TestExecution e)=>Fail("prepare");public void Execute(TestExecution e)=>Fail("execute");public void Verify(TestExecution e)=>Fail("assert");public void Cleanup(TestExecution e){Cleaned=true;Fail("cleanup");}}
    sealed class BadProvider:IDiagnosticProvider {public Descriptor Descriptor=>new Descriptor{Id="bad",Module="test"};public IEnumerable<Finding> Inspect(TestExecution e){throw new Exception("bad provider");}}
}

