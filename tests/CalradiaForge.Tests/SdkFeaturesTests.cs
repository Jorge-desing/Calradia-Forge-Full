using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CalradiaForge.Sdk;
using CalradiaForge.Core;
using CalradiaForge.Examples;
using CalradiaForge.Mod.Commands;

namespace CalradiaForge.Tests
{
    public static class SdkFeaturesTests
    {
        public static void Run(Action<string, Action> test)
        {
            test("ModSettings serialization and caching", TestModSettings);
            test("ModSettings reports safe-save outcomes and preserves committed data on failure", TestModSettingsSafeSave);
            test("CampaignVariableInspector tracking and snapshots", TestCampaignVariableInspector);
            test("ForgeLivePatcher API hooks to ForgeDetour", TestForgeLivePatcher);
            
            test("ForgeApi AutoRegister discovers classes", TestForgeApiAutoRegister);
            test("ForgeApi AutoRegisterWithReport distinguishes unavailable and partial registration", TestForgeApiAutoRegisterWithReport);
            test("ForgeApi Logger routing", TestForgeApiLogger);
            test("ForgeApi Settings registration", TestForgeApiSettings);
            test("ForgeData attachment and clearing", TestForgeDataAttachment);
            test("ForgeData removes its empty outer entity entry safely", TestForgeDataRemovesEmptyEntityEntry);
            test("ForgeModelRegistry evaluates outside locks and owner scopes release only owned generations", TestForgeModelRegistryOwnerScopes);
            test("ForgeModelRegistry category snapshots are immutable, ordered, and stable across mutations", TestForgeModelRegistryCategorySnapshots);
            test("ForgeModelRegistry category queries and evaluation report benchmarks", TestForgeModelRegistryQueryBenchmark);
            test("ForgeModelRegistry evaluation profiles preserve formulas and report benchmarks", TestForgeModelRegistryEvaluationProfiles);
            test("ForgeAgentMemory statistics report bounded aggregate counts and a benchmark", TestForgeAgentMemoryStatisticsBenchmark);
            test("ForgeAgentMemory reads and expiry purge report loaded benchmarks", TestForgeAgentMemoryReadAndPurgeBenchmark);
            test("Unimplemented public engine helpers fail explicitly", TestUnsupportedEngineHelpers);
            test("ForgeAgentMemory cognitive architecture", TestForgeAgentMemory);
            test("ForgeAgentMemory reads expose found, missing, expired, and type-mismatch states", TestForgeAgentMemoryReadResults);
            test("ForgeLocalApi lifecycle and memory leak prevention", TestForgeLocalApi);
            // v5.0.0 new tests (TDD — written before implementation)
            test("ForgeAgentMemory TTL expires stale semantic entry", TestForgeAgentMemoryTTL);
            test("ForgeAgentMemory TTL handles TimeSpan boundaries", TestForgeAgentMemoryTTLBoundaries);
            test("ForgeAgentMemory GetKeys returns tracked fact names", TestForgeAgentMemoryGetKeys);
            test("ForgeAgentMemory Episodic Count returns correct total", TestForgeAgentMemoryEpisodicCount);
            test("ForgeAgentMemory tier quotas, replacement, rejection, and FIFO", TestForgeAgentMemoryTierQuotas);
            test("ForgeAgentMemory shared agent cap and clearing", TestForgeAgentMemoryAgentQuotaAndClearing);
            test("ForgeAgentMemory expired semantic entry releases a global slot", TestForgeAgentMemoryExpiredGlobalSlot);
            test("ForgeLocalApi exposes info and agents endpoints", TestForgeLocalApiEndpoints);
            test("ForgeCampaignEvents logs dispatch errors", TestForgeCampaignEventsErrorLogging);
            test("ForgeApi Version is 10", TestForgeApiVersion);
            test("Forge UI registry rejects duplicate IDs and removes an unloaded owner", TestForgeUiRegistry);
            test("Forge UI discovery validates Gauntlet ViewModel and command binding", TestForgeUiDiscovery);
            test("Forge UI policy enforces context and writer gates", TestForgeUiPolicy);
            test("ModRuleAuditor fails closed on invalid XML, unsafe Pulse intervals, reparse points, and scan limits", TestModRuleAuditorSafety);
            test("ForgeAgentMemory handles null agent and key parameters gracefully", TestForgeAgentMemoryNullSafety);
            test("ForgeTimeSlicer boundaries and zero-allocation processing", TestForgeTimeSlicerBoundaries);
            test("Forge SDK hardening, optimization, and edge case safety", TestSdkHardeningAndOptimizations);
            test("Wave 3 Mod and SDK hardening, optimization, and anti-shadowing", TestWave3ModHardeningAndOptimizations);
            test("ForgeCommands and SDK simulation integration", TestForgeCommandsAndSimulations);
            test("ForgeEncyclopediaExtender in-game codex registry and bookmarks", TestForgeEncyclopediaExtender);
        }

        private static void TestForgeAgentMemory()
        {
            string agentId = "hero_123";
            
            // Semantic Memory
            ForgeAgentMemory.Semantic.Upsert(agentId, "preference_food", "grain");
            var pref = ForgeAgentMemory.Semantic.Get<string>(agentId, "preference_food");
            if (pref != "grain") throw new Exception("Semantic memory failed.");
            
            // Episodic Memory
            ForgeAgentMemory.Episodic.Add(agentId, "battle_won", new { Casualties = 10 });
            var episodes = ForgeAgentMemory.Episodic.GetAll(agentId, "battle_won");
            if (episodes.Count != 1) throw new Exception("Episodic memory failed.");
            
            // Procedural Memory
            ForgeAgentMemory.Procedural.Add(agentId, "siege_tactics", new[] { "build_ram", "attack" });
            var rules = ForgeAgentMemory.Procedural.Get<string[]>(agentId, "siege_tactics");
            if (rules.Length != 2) throw new Exception("Procedural memory failed.");
            
            // Persistence clear
            ForgeAgentMemory.ClearAll();
        }

        private static void TestForgeLocalApi()
        {
            // Just test the lifecycle
            using (var api = new CalradiaForge.Sdk.Api.ForgeLocalApi())
            {
                if (api.IsRunning) throw new Exception("API should not run before Start()");
                api.Start("http://localhost:59999/");
                if (!api.IsRunning) throw new Exception("API should run after Start()");
                api.Stop();
                if (api.IsRunning) throw new Exception("API should stop after Stop()");
            }
        }

        private static void TestModSettings()
        {
            ModSettings.DefaultSerializer = (obj) => "{ \"Name\": \"Test\" }";
            ModSettings.DefaultDeserializer = (json, type) => new DummyConfig { Name = "Test" };
            
            // 1. Initial registration when file might not exist
            var config = ModSettings.Register("test_mod", new DummyConfig { Name = "Init" });
            
            // 2. Force Save to ensure file exists for deserialization
            ModSettings.Save("test_mod", config);
            
            // 3. Clear cache and re-register to test deserialization (from disk)
            typeof(ModSettings).GetField("SettingsCache", BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetValue(null)?.GetType().GetMethod("Clear")?.Invoke(
                typeof(ModSettings).GetField("SettingsCache", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null), null);
                
            var loaded = ModSettings.Register("test_mod", new DummyConfig { Name = "Init" });
            if (loaded.Name != "Test") throw new Exception("ModSettings failed to load/deserialize correctly.");
            
            var cached = ModSettings.Get<DummyConfig>("test_mod");
            if (cached != loaded) throw new Exception("ModSettings cache failed.");
        }

        private static void TestModSettingsSafeSave()
        {
            var previousSerializer=ModSettings.DefaultSerializer;
            var previousDeserializer=ModSettings.DefaultDeserializer;
            var id="cf_safe_save_"+Guid.NewGuid().ToString("N");
            var noSerializerId="cf_no_serializer_"+Guid.NewGuid().ToString("N");
            var blockedId="cf_blocked_save_"+Guid.NewGuid().ToString("N");
            var settingsDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mount and Blade II Bannerlord","Configs","ModSettings");
            var path=Path.Combine(settingsDirectory,id+".json");
            var noSerializerPath=Path.Combine(settingsDirectory,noSerializerId+".json");
            var blockedPath=Path.Combine(settingsDirectory,blockedId+".json");

            try
            {
                ModSettings.DefaultDeserializer=null;
                ModSettings.DefaultSerializer=value=>((DummyConfig)value).Name;
                var originalSettings=ModSettings.Register(id,new DummyConfig{Name="initial"});
                var saved=ModSettings.TrySave(id,originalSettings);
                if(saved.State!=ModSettingsSaveState.Saved || !saved.Succeeded || File.ReadAllText(path)!="initial")
                    throw new Exception("TrySave did not report and commit a successful save.");

                var originalContent=File.ReadAllText(path);
                var nullRejected=false;
                try {ModSettings.TrySave<DummyConfig>(id,null);}
                catch(ArgumentNullException) {nullRejected=true;}
                if(!nullRejected || File.ReadAllText(path)!=originalContent ||
                    !ReferenceEquals(ModSettings.Get<DummyConfig>(id),originalSettings))
                    throw new Exception("TrySave accepted null or changed committed file/cache before rejecting it.");

                ModSettings.DefaultSerializer=_=>throw new InvalidOperationException("private serializer detail");
                var serializationFailure=ModSettings.TrySave(id,new DummyConfig{Name="replacement"});
                if(serializationFailure.State!=ModSettingsSaveState.SerializationFailed ||
                    serializationFailure.FailureReason.Contains("private serializer detail") ||
                    File.ReadAllText(path)!=originalContent || !ReferenceEquals(ModSettings.Get<DummyConfig>(id),originalSettings))
                    throw new Exception("Serialization failure changed committed content/cache or exposed payload details.");

                ModSettings.DefaultSerializer=null;
                var unavailable=ModSettings.TrySave(id,new DummyConfig{Name="replacement"});
                if(unavailable.State!=ModSettingsSaveState.SerializerUnavailable || File.ReadAllText(path)!=originalContent)
                    throw new Exception("Missing serializer was not reported without changing the existing file.");

                ModSettings.Register(noSerializerId,new DummyConfig{Name="defaults"});
                if(File.Exists(noSerializerPath))
                    throw new Exception("Register created a settings file that had no serializer to persist.");

                ModSettings.DefaultSerializer=value=>((DummyConfig)value).Name;
                var caseAlias=id.ToUpperInvariant();
                var aliasSettings=new DummyConfig{Name="case-alias"};
                var aliasSave=ModSettings.TrySave(caseAlias,aliasSettings);
                if(aliasSave.State!=ModSettingsSaveState.Saved ||
                    !ReferenceEquals(ModSettings.Get<DummyConfig>(id),aliasSettings) ||
                    File.ReadAllText(path)!="case-alias")
                    throw new Exception("Case-insensitive mod ID aliases did not share one committed cache/file identity.");

                var aliasSaves=new Task<ModSettingsSaveResult>[16];
                for(var index=0;index<aliasSaves.Length;index++)
                {
                    var capturedIndex=index;
                    aliasSaves[index]=Task.Run(()=>ModSettings.TrySave(
                        capturedIndex%2==0?id:caseAlias,
                        new DummyConfig{Name="parallel-"+capturedIndex}));
                }
                Task.WaitAll(aliasSaves);
                for(var index=0;index<aliasSaves.Length;index++)
                {
                    if(aliasSaves[index].Result.State!=ModSettingsSaveState.Saved)
                        throw new Exception("Concurrent case aliases did not serialize their atomic file commits.");
                }
                var finalAliasSettings=ModSettings.Get<DummyConfig>(id);
                if(finalAliasSettings==null || File.ReadAllText(path)!=finalAliasSettings.Name)
                    throw new Exception("Concurrent alias saves left the cache and committed file out of sync.");

                var reentrantSettings=new DummyConfig{Name="reentrant-latest"};
                ModSettings.DefaultDeserializer=(_,__)=>
                {
                    var nestedSave=ModSettings.TrySave(id,reentrantSettings);
                    if(!nestedSave.Succeeded) throw new Exception("Reentrant serializer save failed.");
                    return new DummyConfig{Name="stale-deserialized-value"};
                };
                typeof(ModSettings).GetField("SettingsCache",BindingFlags.Static|BindingFlags.NonPublic)
                    ?.GetValue(null)?.GetType().GetMethod("Clear")
                    ?.Invoke(typeof(ModSettings).GetField("SettingsCache",BindingFlags.Static|BindingFlags.NonPublic)?.GetValue(null),null);
                var reentrantResult=ModSettings.Register(id,new DummyConfig{Name="fallback"});
                if(!ReferenceEquals(reentrantResult,reentrantSettings) ||
                    !ReferenceEquals(ModSettings.Get<DummyConfig>(id),reentrantSettings) ||
                    File.ReadAllText(path)!="reentrant-latest")
                    throw new Exception("A reentrant deserializer overwrote a newer committed settings save.");

                ModSettings.DefaultDeserializer=null;
                originalContent=File.ReadAllText(path);
                var settingsBeforeStorageFailure=ModSettings.Get<DummyConfig>(id);
                using(var exclusive=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))
                {
                    var storageFailure=ModSettings.TrySave(id,new DummyConfig{Name="replacement"});
                    if(storageFailure.State!=ModSettingsSaveState.StorageFailed)
                        throw new Exception("An exclusive file lock did not produce an explicit storage failure.");
                }
                if(File.ReadAllText(path)!=originalContent || !ReferenceEquals(ModSettings.Get<DummyConfig>(id),settingsBeforeStorageFailure))
                    throw new Exception("A failed atomic replacement changed the original file or cache.");

                Directory.CreateDirectory(blockedPath);
                var blockedSave=ModSettings.TrySave(blockedId,new DummyConfig{Name="blocked"});
                if(blockedSave.State!=ModSettingsSaveState.StorageFailed)
                    throw new Exception("A destination directory collision did not report a storage failure.");
                if(Directory.GetFiles(settingsDirectory,blockedId+".json.*.tmp").Length!=0)
                    throw new Exception("A failed save left a temporary file behind.");
            }
            finally
            {
                ModSettings.DefaultSerializer=previousSerializer;
                ModSettings.DefaultDeserializer=previousDeserializer;
                if(File.Exists(path))File.Delete(path);
                if(File.Exists(noSerializerPath))File.Delete(noSerializerPath);
                if(Directory.Exists(blockedPath))Directory.Delete(blockedPath,true);
                if(File.Exists(blockedPath))File.Delete(blockedPath);
            }
        }

        private static void TestCampaignVariableInspector()
        {
            int testVar = 42;
            CampaignVariableInspector.TrackVariable("test_score", () => testVar, (v) => testVar = (int)v);
            
            var snapshot = CampaignVariableInspector.GenerateSnapshot();
            if (!snapshot.ContainsKey("test_score") || (int)snapshot["test_score"] != 42) 
                throw new Exception("CampaignVariableInspector tracking failed.");
                
            bool setSuccess = CampaignVariableInspector.SetVariable("test_score", 100);
            if (!setSuccess || testVar != 100) throw new Exception("CampaignVariableInspector setter live-tweaking failed.");
            
            CampaignVariableInspector.UntrackVariable("test_score");
            snapshot = CampaignVariableInspector.GenerateSnapshot();
            if (snapshot.ContainsKey("test_score"))
                throw new Exception("CampaignVariableInspector untracking failed.");
        }

        private static void TestForgeLivePatcher()
        {
            // Setup detour
            MethodInfo original = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo replacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic);
            
            string result = TargetMethod();
            if (result != "Original") throw new Exception("Pre-patch state invalid.");
            
            ForgeLivePatcher.ApplyDetour(original, replacement);
            
            string patchedResult = TargetMethod();
            if (patchedResult != "Replacement") throw new Exception("ForgeLivePatcher.ApplyDetour failed! Expected 'Replacement', got: " + patchedResult);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static string TargetMethod() => "Original";

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static string ReplacementMethod() => "Replacement";
        
        private static void TestForgeApiAutoRegister()
        {
            var engine = new TestEngine();
            ForgeApi.Connect(engine);
            try
            {
                ForgeApi.AutoRegister(Assembly.GetExecutingAssembly(), "test_mod");
                if (!engine.Commands.Any(c => c.Id == "dummy_command"))
                    throw new Exception("AutoRegister failed to discover DummyCommand");
            }
            finally
            {
                ForgeApi.Disconnect();
            }
        }
        
        private static void TestForgeApiAutoRegisterWithReport()
        {
            ForgeApi.Disconnect();
            var unavailable=ForgeApi.AutoRegisterWithReport(typeof(SdkFeaturesTests).Assembly,"test_mod");
            if(unavailable.State!=ForgeAutoRegisterState.HostUnavailable || unavailable.FailureCount!=1 || unavailable.RegisteredCount!=0)
                throw new Exception("AutoRegisterWithReport did not distinguish a missing Forge host.");

            var engine=new TestEngine();
            ForgeApi.Connect(engine);
            try
            {
                var report=ForgeApi.AutoRegisterWithReport(typeof(SdkFeaturesTests).Assembly,"test_mod");
                if(report.State!=ForgeAutoRegisterState.CompletedWithErrors || report.CandidateCount<2 || report.RegisteredCount<1 || report.FailureCount<1)
                    throw new Exception("AutoRegisterWithReport did not preserve successful registrations alongside failures.");
                if(!report.Failures.Any(failure=>failure.TypeName.Contains(nameof(AutoRegisterThrowingCommand)) && failure.Contract=="ICommand"))
                    throw new Exception("AutoRegisterWithReport omitted the bounded constructor failure detail.");
                if(!engine.Commands.Any(command=>command.Id=="dummy_command"))
                    throw new Exception("AutoRegisterWithReport failed to register the healthy command.");
            }
            finally
            {
                ForgeApi.Disconnect();
            }
        }

        private static void TestForgeApiLogger()
        {
            var engine = new TestEngine();
            engine.Log = new SessionLog();
            ForgeApi.Connect(engine);
            try
            {
                ForgeApi.Logger.LogInfo("Test", "Hello World");
                var records = engine.Log.Deserialize();
                if (!records.Any(r => r.Module == "Test" && r.Message == "Hello World"))
                    throw new Exception("ForgeLogger failed to route message to SessionLog");
            }
            finally
            {
                ForgeApi.Disconnect();
            }
        }
        
        private static void TestForgeApiSettings()
        {
            var engine = new TestEngine();
            ForgeApi.Connect(engine);
            try
            {
                var cfg = new DummyConfig { Name = "SdkTest" };
                ForgeApi.Settings.Register("test_mod_settings", "Test Mod Settings", cfg);
                
                var retrieved = ForgeApi.Settings.GetSettings<DummyConfig>("test_mod_settings");
                if (retrieved == null || retrieved.Name != "SdkTest")
                    throw new Exception("ForgeSettingsRegistry failed to store/retrieve setting");
            }
            finally
            {
                ForgeApi.Disconnect();
            }
        }
        
        private static void TestForgeDataAttachment()
        {
            var dummyEntity = new object();
            
            // 1. Get auto-creates new
            var data1 = dummyEntity.GetForgeData<DummyConfig>();
            if (data1 == null) throw new Exception("ForgeData failed to create attached data");
            
            // 2. Set stores custom
            var data2 = new DummyConfig { Name = "Attached" };
            dummyEntity.SetForgeData(data2);
            
            // 3. Get retrieves stored
            var retrieved = dummyEntity.GetForgeData<DummyConfig>();
            if (retrieved.Name != "Attached") throw new Exception("ForgeData failed to retrieve attached data");
            
            // 4. ClearAll wipes state
            ForgeData.ClearAll();
            var wiped = dummyEntity.GetForgeData<DummyConfig>();
            if (wiped.Name == "Attached") throw new Exception("ForgeData failed to clear");
        }

        class DummyCommand : ICommand
        {
            public Descriptor Descriptor => new Descriptor { Id = "dummy_command", Module = "test_mod", Name = "Dummy Command" };
            public string Execute(TestExecution execution, string argument) => "OK";
        }

        sealed class AutoRegisterThrowingCommand : ICommand
        {
            public AutoRegisterThrowingCommand() { throw new InvalidOperationException("fixture failure"); }
            public Descriptor Descriptor => new Descriptor { Id = "auto_register_throwing", Module = "test_mod", Name = "Failure fixture" };
            public string Execute(TestExecution execution, string argument) => "unreachable";
        }

        class DummyConfig { public string Name { get; set; } }
        class OtherDummyConfig { public string Name { get; set; } }

        // ─── v5.0.0 test bodies ────────────────────────────────────────────────

        private static void TestForgeAgentMemoryReadResults()
        {
            ForgeAgentMemory.ClearAll();
            var agent="read_result_"+Guid.NewGuid().ToString("N");
            try
            {
                ForgeAgentMemory.Semantic.Upsert(agent,"found","value");
                var found=ForgeAgentMemory.Semantic.GetResult<string>(agent,"found");
                if(found.State!=ForgeMemoryReadState.Found || !found.HasValue || found.Value!="value")
                    throw new Exception("Semantic read did not return the found value and state.");
                if(ForgeAgentMemory.Semantic.GetResult<string>(agent,"missing").State!=ForgeMemoryReadState.Missing)
                    throw new Exception("Semantic read did not distinguish a missing key.");
                ForgeAgentMemory.Semantic.Upsert(agent,"wrong_type",42);
                if(ForgeAgentMemory.Semantic.GetResult<string>(agent,"wrong_type").State!=ForgeMemoryReadState.TypeMismatch)
                    throw new Exception("Semantic read did not distinguish a type mismatch.");
                ForgeAgentMemory.Semantic.Upsert(agent,"stored_null",null);
                var semanticNull=ForgeAgentMemory.Semantic.GetResult<string>(agent,"stored_null");
                if(semanticNull.State!=ForgeMemoryReadState.Found || !semanticNull.HasValue || semanticNull.Value!=null ||
                    ForgeAgentMemory.Semantic.GetResult<int>(agent,"stored_null").State!=ForgeMemoryReadState.TypeMismatch)
                    throw new Exception("Semantic read did not treat a stored null as a found reference value.");
                ForgeAgentMemory.Semantic.Upsert(agent,"expired","old",TimeSpan.Zero);
                if(ForgeAgentMemory.Semantic.GetResult<string>(agent,"expired").State!=ForgeMemoryReadState.Expired)
                    throw new Exception("Semantic read did not distinguish an expired entry.");
                if(ForgeAgentMemory.Semantic.Get<string>(agent,"wrong_type")!=null)
                    throw new Exception("Legacy Semantic.Get changed its default-return behavior.");

                ForgeAgentMemory.Procedural.Add(agent,"found_task","instructions");
                var proceduralFound=ForgeAgentMemory.Procedural.GetResult<string>(agent,"found_task");
                if(proceduralFound.State!=ForgeMemoryReadState.Found || proceduralFound.Value!="instructions")
                    throw new Exception("Procedural read did not return the found value and state.");
                if(ForgeAgentMemory.Procedural.GetResult<string>(agent,"missing_task").State!=ForgeMemoryReadState.Missing)
                    throw new Exception("Procedural read did not distinguish a missing task.");
                ForgeAgentMemory.Procedural.Add(agent,"wrong_task",new object());
                if(ForgeAgentMemory.Procedural.GetResult<string>(agent,"wrong_task").State!=ForgeMemoryReadState.TypeMismatch)
                    throw new Exception("Procedural read did not distinguish a type mismatch.");
                ForgeAgentMemory.Procedural.Add(agent,"null_task",null);
                var proceduralNull=ForgeAgentMemory.Procedural.GetResult<object>(agent,"null_task");
                if(proceduralNull.State!=ForgeMemoryReadState.Found || !proceduralNull.HasValue || proceduralNull.Value!=null)
                    throw new Exception("Procedural read did not treat a stored null as a found reference value.");
            }
            finally
            {
                ForgeAgentMemory.ClearAgent(agent);
            }
        }

        private static void TestForgeAgentMemoryTTL()
        {
            ForgeAgentMemory.ClearAll();
            string agent = "ttl_hero";

            // Upsert with a very short TTL (already-expired)
            ForgeAgentMemory.Semantic.Upsert(agent, "rank", "knight", TimeSpan.FromMilliseconds(-1));
            var val = ForgeAgentMemory.Semantic.Get<string>(agent, "rank");
            if (val != null) throw new Exception("TTL: expired entry should return null.");

            // Upsert with a long TTL — must survive immediately
            ForgeAgentMemory.Semantic.Upsert(agent, "rank", "lord", TimeSpan.FromHours(1));
            val = ForgeAgentMemory.Semantic.Get<string>(agent, "rank");
            if (val != "lord") throw new Exception("TTL: live entry should return value.");

            ForgeAgentMemory.ClearAll();
        }

        private static void TestForgeAgentMemoryGetKeys()
        {
            ForgeAgentMemory.ClearAll();
            string agent = "key_hero";
            ForgeAgentMemory.Semantic.Upsert(agent, "faction", "empire");
            ForgeAgentMemory.Semantic.Upsert(agent, "trait", "brave");

            var keys = ForgeAgentMemory.Semantic.GetKeys(agent);
            if (!keys.Contains("faction")) throw new Exception("GetKeys: missing 'faction'.");
            if (!keys.Contains("trait")) throw new Exception("GetKeys: missing 'trait'.");
            if (keys.Count != 2) throw new Exception("GetKeys: expected exactly 2 keys.");

            ForgeAgentMemory.ClearAll();
        }

        private static void TestForgeAgentMemoryEpisodicCount()
        {
            ForgeAgentMemory.ClearAll();
            string agent = "count_hero";
            ForgeAgentMemory.Episodic.Add(agent, "battle", new { });
            ForgeAgentMemory.Episodic.Add(agent, "battle", new { });
            ForgeAgentMemory.Episodic.Add(agent, "treaty", new { });

            if (ForgeAgentMemory.Episodic.Count(agent, "battle") != 2)
                throw new Exception("Episodic.Count: expected 2 battle events.");
            if (ForgeAgentMemory.Episodic.Count(agent, "treaty") != 1)
                throw new Exception("Episodic.Count: expected 1 treaty event.");
            if (ForgeAgentMemory.Episodic.Count(agent, "siege") != 0)
                throw new Exception("Episodic.Count: expected 0 for unknown type.");

            ForgeAgentMemory.ClearAll();
        }

        private static void TestForgeDataRemovesEmptyEntityEntry()
        {
            ForgeData.ClearAll();
            var entity = new object();
            entity.SetForgeData(new DummyConfig { Name = "temporary" });
            if (ForgeData.EntityData.Count != 1 || !entity.RemoveForgeData<DummyConfig>())
                throw new Exception("The typed ForgeData entry was not removed.");
            if (ForgeData.EntityData.ContainsKey(entity))
                throw new Exception("Removing the last typed ForgeData value should remove its empty outer entity entry.");

            entity.SetForgeData(new DummyConfig { Name = "keep" });
            if (!entity.RemoveForgeData<DummyConfig>() || ForgeData.EntityData.ContainsKey(entity))
                throw new Exception("ForgeData cleanup did not release the final value.");

            // Race a final-value removal against insertion of a different tier. The insertion
            // must not be lost when the remover tries to prune an empty outer key.
            var start = new ManualResetEvent(false);
            Exception workerError = null;
            var remover = new Thread(() =>
            {
                try
                {
                    start.WaitOne();
                    for (int i = 0; i < 2000; i++)
                    {
                        entity.SetForgeData(new DummyConfig { Name = "temporary" });
                        entity.RemoveForgeData<DummyConfig>();
                    }
                }
                catch (Exception error) { workerError = error; }
            });
            var setter = new Thread(() =>
            {
                try
                {
                    start.WaitOne();
                    for (int i = 0; i < 2000; i++)
                        entity.SetForgeData(new OtherDummyConfig { Name = "persistent" });
                }
                catch (Exception error) { workerError = error; }
            });
            remover.Start();
            setter.Start();
            start.Set();
            if (!remover.Join(TimeSpan.FromSeconds(5)) || !setter.Join(TimeSpan.FromSeconds(5)))
                throw new Exception("Concurrent ForgeData cleanup did not finish promptly.");
            start.Dispose();
            if (workerError != null) throw workerError;
            if (!entity.HasForgeData<OtherDummyConfig>())
                throw new Exception("Removing the final typed value detached a concurrently populated entity dictionary.");
            ForgeData.ClearAll();
        }

        private static void TestForgeModelRegistryOwnerScopes()
        {
            var registry = new ForgeModelRegistry();
            var scope = registry.BeginOwnerScope("owner.mod");
            var owned = new ForgeModelModifier("scope.modifier", ForgeGameModelCategory.PartySpeed,
                "owner.mod", "owned", 1f, 0f);
            scope.Register(owned);
            if (registry.GetAllModifiers().Count != 1)
                throw new Exception("An owner-scoped modifier was not registered.");

            try
            {
                scope.Register(new ForgeModelModifier("wrong.owner", ForgeGameModelCategory.PartySpeed,
                    "other.mod", "invalid", 1f, 0f));
                throw new Exception("An owner scope accepted a modifier from another module.");
            }
            catch (ArgumentException) { }

            var replacement = new ForgeModelModifier("scope.modifier", ForgeGameModelCategory.PartySpeed,
                "legacy.mod", "legacy replacement", 2f, 0f);
            registry.Register(replacement);
            scope.Dispose();
            if (registry.GetAllModifiers().Count != 1 || registry.GetAllModifiers()[0] != replacement)
                throw new Exception("Disposing an old owner scope removed a replacement generation it did not own.");

            var secondScope = registry.BeginOwnerScope("second.mod");
            secondScope.Register(new ForgeModelModifier("scope.owned.2", ForgeGameModelCategory.PartySpeed,
                "second.mod", "owned", 1f, 0f));
            secondScope.Dispose();
            if (registry.GetAllModifiers().Count != 1 || registry.GetAllModifiers()[0] != replacement)
                throw new Exception("Disposing an owner scope did not remove its own modifier.");
            try
            {
                secondScope.Register(new ForgeModelModifier("after.dispose", ForgeGameModelCategory.PartySpeed,
                    "second.mod", "invalid", 0f, 0f));
                throw new Exception("A disposed owner scope accepted a new registration.");
            }
            catch (ObjectDisposedException) { }

            registry.Register(new ForgeModelModifier("condition.probe", ForgeGameModelCategory.PartySpeed,
                "probe.mod", "probe", 1f, 0f, _ =>
                {
                    Exception callbackError = null;
                    var worker = new Thread(() =>
                    {
                        try
                        {
                            registry.Register(new ForgeModelModifier("from.callback", ForgeGameModelCategory.PartySpeed,
                                "callback.mod", "callback registration", 1f, 0f));
                        }
                        catch (Exception error) { callbackError = error; }
                    });
                    worker.Start();
                    if (!worker.Join(TimeSpan.FromSeconds(2)))
                        throw new Exception("ForgeModelRegistry held its lock while invoking a condition callback.");
                    if (callbackError != null) throw callbackError;
                    return true;
                }));
            registry.Evaluate(ForgeGameModelCategory.PartySpeed, 1f);
            if (!registry.GetAllModifiers().Any(item => item.Id == "from.callback"))
                throw new Exception("A callback-triggered registration did not complete.");

            registry.Register(new ForgeModelModifier("condition.failure", ForgeGameModelCategory.PartyWage,
                "failure.mod", "throwing predicate", 0f, 0f,
                _ => throw new InvalidOperationException("expected predicate failure")));
            bool propagated = false;
            try { registry.Evaluate(ForgeGameModelCategory.PartyWage, 1f); }
            catch (InvalidOperationException error) when (error.Message == "expected predicate failure") { propagated = true; }
            if (!propagated) throw new Exception("ForgeModelRegistry swallowed a condition failure.");
            registry.Register(new ForgeModelModifier("after.failure", ForgeGameModelCategory.PartyWage,
                "after.mod", "post-failure registration", 0f, 0f));
            if (!registry.GetAllModifiers().Any(item => item.Id == "after.failure"))
                throw new Exception("A failing predicate left the registry locked or unusable.");
        }

        private static void TestForgeModelRegistryQueryBenchmark()
        {
            const int modifierCount = 1024;
            const int queryCount = 128;
            var registry = new ForgeModelRegistry();
            var expected = new List<ForgeModelModifier>();
            for (int index = 0; index < modifierCount; index++)
            {
                var category = (ForgeGameModelCategory)(index % 8);
                var modifier = new ForgeModelModifier("query." + index.ToString(CultureInfo.InvariantCulture),
                    category, "benchmark", "query benchmark", 0f, 0f);
                registry.Register(modifier);
                if (category == ForgeGameModelCategory.PartySpeed) expected.Add(modifier);
            }

            var snapshot = registry.GetModifiers(ForgeGameModelCategory.PartySpeed);
            if (snapshot.Count != expected.Count)
                throw new Exception("GetModifiers returned the wrong category count.");
            if (!ReferenceEquals(snapshot, registry.GetModifiers(ForgeGameModelCategory.PartySpeed)))
                throw new Exception("GetModifiers did not reuse the cached category snapshot.");

            var watch = Stopwatch.StartNew();
            for (int query = 0; query < queryCount; query++)
            {
                var actual = registry.GetModifiers(ForgeGameModelCategory.PartySpeed);
                if (actual.Count != expected.Count)
                    throw new Exception("GetModifiers returned an inconsistent category snapshot.");
            }
            watch.Stop();
            Console.WriteLine("PERF ForgeModelRegistry.GetModifiers modifiers=" + modifierCount +
                " queries=" + queryCount +
                " total_ms=" + watch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture));

            registry.Unregister(expected[0].Id);
            if (snapshot.Count != expected.Count || snapshot[0] != expected[0])
                throw new Exception("A previously returned GetModifiers result was not a stable snapshot.");
            if (registry.GetModifiers(ForgeGameModelCategory.PartySpeed).Count != expected.Count - 1)
                throw new Exception("GetModifiers did not reflect a later unregister operation.");

            const int evaluationCount = 128;
            var evaluationWatch = Stopwatch.StartNew();
            for (int evaluation = 0; evaluation < evaluationCount; evaluation++)
            {
                ForgeModelCalculationResult result = registry.Evaluate(ForgeGameModelCategory.PartySpeed, 100f);
                if (result.AppliedModifiers.Count != expected.Count - 1)
                    throw new Exception("Evaluate included a modifier outside the requested category or missed a registered modifier.");
            }
            evaluationWatch.Stop();
            Console.WriteLine("PERF ForgeModelRegistry.Evaluate modifiers=" + modifierCount +
                " category_modifiers=" + (expected.Count - 1) +
                " evaluations=" + evaluationCount +
                " total_ms=" + evaluationWatch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture));
        }

        private static void TestForgeModelRegistryEvaluationProfiles()
        {
            BenchmarkModelEvaluation("empty", 0, null);
            BenchmarkModelEvaluation("single_true", 1, index => true);
            BenchmarkModelEvaluation("dense_unconditional", 128, null);
            BenchmarkModelEvaluation("dense_true", 128, index => true);
            BenchmarkModelEvaluation("dense_false", 128, index => false);
            BenchmarkModelEvaluation("dense_mixed", 128, index => (index & 1) == 0);
        }

        private static void BenchmarkModelEvaluation(string profile, int modifierCount, Func<int, bool> conditionFactory)
        {
            const int evaluationCount = 128;
            const float baseValue = 100f;
            const float additivePerModifier = 0.25f;
            const float factorPerModifier = 0.001f;
            var registry = new ForgeModelRegistry();
            int expectedApplied = 0;
            float expectedAdds = 0f;
            float expectedFactors = 0f;

            for (int index = 0; index < modifierCount; index++)
            {
                int modifierIndex = index;
                Func<object, bool> condition = conditionFactory == null
                    ? null
                    : new Func<object, bool>(_ => conditionFactory(modifierIndex));
                var modifier = new ForgeModelModifier(
                    "evaluation." + profile + "." + index.ToString(CultureInfo.InvariantCulture),
                    ForgeGameModelCategory.PartySpeed,
                    "benchmark",
                    "evaluation benchmark",
                    additivePerModifier,
                    factorPerModifier,
                    condition);
                registry.Register(modifier);

                if (conditionFactory == null || conditionFactory(index))
                {
                    expectedApplied++;
                    expectedAdds += additivePerModifier;
                    expectedFactors += factorPerModifier;
                }
            }

            ForgeModelCalculationResult first = registry.Evaluate(ForgeGameModelCategory.PartySpeed, baseValue);
            float expectedFinal = (baseValue + expectedAdds) * (1f + expectedFactors);
            if (first.AppliedModifiers.Count != expectedApplied ||
                Math.Abs(first.TotalAdditive - expectedAdds) > 0.001f ||
                Math.Abs(first.TotalFactors - expectedFactors) > 0.001f ||
                Math.Abs(first.FinalValue - expectedFinal) > 0.01f)
            {
                throw new Exception("Evaluate changed its formula or condition filtering for profile " + profile + ".");
            }

            // The public result deliberately exposes a mutable per-call list. Mutating one result
            // must not affect a later evaluation or a cached registry snapshot.
            int originalCount = first.AppliedModifiers.Count;
            first.AppliedModifiers.Add("caller-owned");
            if (first.AppliedModifiers.Count != originalCount + 1 ||
                registry.Evaluate(ForgeGameModelCategory.PartySpeed, baseValue).AppliedModifiers.Count != originalCount)
            {
                throw new Exception("Evaluate result modifier lists stopped being independent and mutable.");
            }

            for (int warmup = 0; warmup < 4; warmup++)
                registry.Evaluate(ForgeGameModelCategory.PartySpeed, baseValue);

            var watch = Stopwatch.StartNew();
            ForgeModelCalculationResult last = null;
            for (int evaluation = 0; evaluation < evaluationCount; evaluation++)
                last = registry.Evaluate(ForgeGameModelCategory.PartySpeed, baseValue);
            watch.Stop();

            if (last == null || last.AppliedModifiers.Count != expectedApplied ||
                Math.Abs(last.FinalValue - expectedFinal) > 0.01f)
            {
                throw new Exception("Evaluate benchmark returned an inconsistent result for profile " + profile + ".");
            }

            Console.WriteLine("PERF ForgeModelRegistry.Evaluate profile=" + profile +
                " modifiers=" + modifierCount +
                " applied=" + expectedApplied +
                " evaluations=" + evaluationCount +
                " total_ms=" + watch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture));
        }

        private static void TestForgeModelRegistryCategorySnapshots()
        {
            var registry = new ForgeModelRegistry();
            var first = new ForgeModelModifier("snapshot.first", ForgeGameModelCategory.PartySpeed,
                "snapshot", "first", 1f, 0f);
            var second = new ForgeModelModifier("snapshot.second", ForgeGameModelCategory.PartySpeed,
                "snapshot", "second", 2f, 0f);
            var otherCategory = new ForgeModelModifier("snapshot.other", ForgeGameModelCategory.PartyWage,
                "snapshot", "other", 3f, 0f);
            registry.Register(first);
            registry.Register(second);
            registry.Register(otherCategory);

            IReadOnlyList<ForgeModelModifier> initial = registry.GetModifiers(ForgeGameModelCategory.PartySpeed);
            if (initial.Count != 2 || initial[0] != first || initial[1] != second)
                throw new Exception("Category snapshots did not preserve registration order or category filtering.");
            if (!ReferenceEquals(initial, registry.GetModifiers(ForgeGameModelCategory.PartySpeed)))
                throw new Exception("Repeated queries did not return the cached immutable snapshot.");
            IReadOnlyList<ForgeModelModifier> invalidCategory = registry.GetModifiers((ForgeGameModelCategory)int.MaxValue);
            if (invalidCategory.Count != 0 ||
                !ReferenceEquals(invalidCategory, registry.GetModifiers((ForgeGameModelCategory)int.MaxValue)))
            {
                throw new Exception("An undefined category was not returned as an uncached shared empty snapshot.");
            }

            var collection = initial as IList<ForgeModelModifier>;
            if (collection == null || !collection.IsReadOnly)
                throw new Exception("The category snapshot is not exposed as a read-only collection.");
            bool rejectedMutation = false;
            try { collection.Add(otherCategory); }
            catch (NotSupportedException) { rejectedMutation = true; }
            if (!rejectedMutation)
                throw new Exception("A caller mutated the registry's cached category snapshot.");

            var third = new ForgeModelModifier("snapshot.third", ForgeGameModelCategory.PartySpeed,
                "snapshot", "third", 4f, 0f);
            registry.Register(third);
            IReadOnlyList<ForgeModelModifier> afterRegister = registry.GetModifiers(ForgeGameModelCategory.PartySpeed);
            if (ReferenceEquals(initial, afterRegister) || afterRegister.Count != 3 || afterRegister[2] != third)
                throw new Exception("Register did not rebuild the category snapshot in registration order.");
            if (initial.Count != 2 || initial[0] != first || initial[1] != second)
                throw new Exception("Register changed a previously returned category snapshot.");

            var moved = new ForgeModelModifier("snapshot.first", ForgeGameModelCategory.PartyWage,
                "snapshot", "replacement in another category", 5f, 0f);
            registry.Register(moved);
            IReadOnlyList<ForgeModelModifier> speedAfterReplace = registry.GetModifiers(ForgeGameModelCategory.PartySpeed);
            IReadOnlyList<ForgeModelModifier> wageAfterReplace = registry.GetModifiers(ForgeGameModelCategory.PartyWage);
            if (speedAfterReplace.Count != 2 || speedAfterReplace[0] != second || speedAfterReplace[1] != third)
                throw new Exception("Replacing a modifier did not remove it from its previous category snapshot.");
            if (wageAfterReplace.Count != 2 || wageAfterReplace[0] != otherCategory || wageAfterReplace[1] != moved)
                throw new Exception("Replacing a modifier did not append it to the new category snapshot.");
        }

        private static void TestForgeAgentMemoryStatisticsBenchmark()
        {
            const int agentCount = 128;
            const int semanticEntriesPerAgent = 16;
            const int episodicEntriesPerAgent = 8;
            const int proceduralEntriesPerAgent = 8;
            const int sampleCount = 32;

            string runId = Guid.NewGuid().ToString("N");
            var agentIds = new List<string>(agentCount);
            try
            {
                for (int agent = 0; agent < agentCount; agent++)
                {
                    string agentId = "stats_benchmark_" + runId + "_" + agent.ToString(CultureInfo.InvariantCulture);
                    agentIds.Add(agentId);
                    for (int entry = 0; entry < semanticEntriesPerAgent; entry++)
                    {
                        ForgeAgentMemory.Semantic.Upsert(agentId,
                            "fact_" + entry.ToString(CultureInfo.InvariantCulture), entry);
                    }
                    for (int entry = 0; entry < episodicEntriesPerAgent; entry++)
                    {
                        ForgeAgentMemory.Episodic.Add(agentId, "event", entry);
                    }
                    for (int entry = 0; entry < proceduralEntriesPerAgent; entry++)
                    {
                        ForgeAgentMemory.Procedural.Add(agentId,
                            "task_" + entry.ToString(CultureInfo.InvariantCulture), entry);
                    }
                }

                var samples = new List<double>(sampleCount);
                for (int sample = 0; sample < sampleCount; sample++)
                {
                    var watch = Stopwatch.StartNew();
                    ForgeAgentMemory.GetStatistics(
                        out int actualAgents,
                        out int semanticEntries,
                        out int episodicEntries,
                        out int proceduralEntries);
                    watch.Stop();
                    if (actualAgents < agentCount ||
                        semanticEntries < agentCount * semanticEntriesPerAgent ||
                        episodicEntries < agentCount * episodicEntriesPerAgent ||
                        proceduralEntries < agentCount * proceduralEntriesPerAgent)
                    {
                        throw new Exception("GetStatistics omitted entries from the representative workload.");
                    }
                    samples.Add(watch.Elapsed.TotalMilliseconds);
                }

                samples.Sort();
                double median = (samples[(sampleCount - 1) / 2] + samples[sampleCount / 2]) / 2.0;
                Console.WriteLine("PERF ForgeAgentMemory.GetStatistics workload_agents=" + agentCount +
                    " workload_semantic=" + (agentCount * semanticEntriesPerAgent) +
                    " workload_episodic=" + (agentCount * episodicEntriesPerAgent) +
                    " workload_procedural=" + (agentCount * proceduralEntriesPerAgent) +
                    " samples=" + sampleCount +
                    " median_ms=" + median.ToString("F3", CultureInfo.InvariantCulture));
            }
            finally
            {
                foreach (string agentId in agentIds) ForgeAgentMemory.ClearAgent(agentId);
            }
        }

        private static void TestForgeAgentMemoryReadAndPurgeBenchmark()
        {
            const int readCount = 8192;
            const int purgeAgentCount = 256;
            const int purgeSamples = 16;
            string runId = Guid.NewGuid().ToString("N");
            string readAgentId = "read_benchmark_" + runId;
            var purgeAgentIds = new List<string>(purgeAgentCount);

            try
            {
                ForgeAgentMemory.Semantic.Upsert(readAgentId, "found", 42);
                ForgeAgentMemory.Procedural.Add(readAgentId, "found", 42);
                if (ForgeAgentMemory.Semantic.Get<int>(readAgentId, "found") != 42 ||
                    ForgeAgentMemory.Procedural.Get<int>(readAgentId, "found") != 42)
                {
                    throw new Exception("Memory read benchmark fixture could not retrieve its seeded values.");
                }

                BenchmarkMemoryRead("Semantic.Get", readCount, () => ForgeAgentMemory.Semantic.Get<int>(readAgentId, "found"), 42);
                BenchmarkMemoryRead("Semantic.GetResult", readCount, () => ForgeAgentMemory.Semantic.GetResult<int>(readAgentId, "found").Value, 42);
                BenchmarkMemoryRead("Semantic.GetMissing", readCount, () => ForgeAgentMemory.Semantic.Get<int>(readAgentId, "missing"), 0);
                BenchmarkMemoryRead("Procedural.Get", readCount, () => ForgeAgentMemory.Procedural.Get<int>(readAgentId, "found"), 42);
                BenchmarkMemoryRead("Procedural.GetResult", readCount, () => ForgeAgentMemory.Procedural.GetResult<int>(readAgentId, "found").Value, 42);

                for (int agent = 0; agent < purgeAgentCount; agent++)
                {
                    string agentId = "purge_benchmark_" + runId + "_" + agent.ToString(CultureInfo.InvariantCulture);
                    purgeAgentIds.Add(agentId);
                    ForgeAgentMemory.Semantic.Upsert(agentId, "stable", agent);
                }

                var purgeTimes = new List<double>(purgeSamples);
                for (int sample = 0; sample < purgeSamples; sample++)
                {
                    for (int agent = 0; agent < purgeAgentIds.Count; agent++)
                        ForgeAgentMemory.Semantic.Upsert(purgeAgentIds[agent], "expired", sample, TimeSpan.Zero);

                    var watch = Stopwatch.StartNew();
                    ForgeAgentMemory.GetStatistics(
                        out int actualAgents,
                        out int semanticEntries,
                        out int episodicEntries,
                        out int proceduralEntries);
                    watch.Stop();

                    if (actualAgents < purgeAgentCount || semanticEntries < purgeAgentCount ||
                        episodicEntries < 0 || proceduralEntries < 0)
                    {
                        throw new Exception("GetStatistics omitted stable entries or returned invalid counts after TTL purge.");
                    }
                    purgeTimes.Add(watch.Elapsed.TotalMilliseconds);
                }

                purgeTimes.Sort();
                double purgeMedian = (purgeTimes[(purgeSamples - 1) / 2] + purgeTimes[purgeSamples / 2]) / 2.0;
                Console.WriteLine("PERF ForgeAgentMemory.PurgeExpired workload_agents=" + purgeAgentCount +
                    " ttl_entries_per_agent=1 samples=" + purgeSamples +
                    " median_ms=" + purgeMedian.ToString("F3", CultureInfo.InvariantCulture));
            }
            finally
            {
                ForgeAgentMemory.ClearAgent(readAgentId);
                foreach (string agentId in purgeAgentIds) ForgeAgentMemory.ClearAgent(agentId);
            }
        }

        private static void BenchmarkMemoryRead(string operation, int readCount, Func<int> read, int expectedValue)
        {
            for (int warmup = 0; warmup < 128; warmup++)
            {
                if (read() != expectedValue) throw new Exception(operation + " returned an unexpected warm-up value.");
            }

            int checksum = 0;
            var watch = Stopwatch.StartNew();
            for (int query = 0; query < readCount; query++) checksum += read();
            watch.Stop();
            if (checksum != expectedValue * readCount)
                throw new Exception(operation + " returned an inconsistent benchmark value.");

            Console.WriteLine("PERF ForgeAgentMemory." + operation +
                " queries=" + readCount +
                " total_ms=" + watch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture));
        }

        private static void TestUnsupportedEngineHelpers()
        {
#if NET472
            var assembly = typeof(ModRuleAuditor).Assembly;
            string[][] methods =
            {
                new[] { "ForgeTask", "RunNextTick" }, new[] { "ForgeCamera", "GetCurrent" },
                new[] { "ForgeParty", "Teleport" }, new[] { "ForgeTime", "FastForward" },
                new[] { "ForgeClan", "CreatePlayerSubClan" }, new[] { "ForgeConversation", "ForceDialog" },
                new[] { "ForgeWorkshop", "ChangeWorkshopType" }, new[] { "ForgeCaravan", "SpawnCaravan" },
                new[] { "ForgeTournament", "StartTournament" }, new[] { "ForgeBattle", "StartEncounter" },
                new[] { "ForgeCrafting", "UnlockPart" }, new[] { "ForgeSiege", "StartSiege" },
                new[] { "ForgeWeather", "ForceRain" }, new[] { "ForgeSound", "PlaySound" },
                new[] { "ForgeParticle", "SpawnParticle" }, new[] { "ForgeMusic", "PlayMusic" },
                new[] { "ForgeTooltip", "ShowTooltip" }, new[] { "ForgeCheat", "ToggleCheats" },
                new[] { "ForgeReligion", "ConvertHero" }, new[] { "ForgeTrait", "SetTraitLevel" },
                new[] { "ForgeBanner", "RandomizeBanner" }, new[] { "ForgeTavern", "AddMercenary" },
                new[] { "ForgeCompanion", "SpawnWanderer" }, new[] { "ForgeHideout", "SpawnHideout" },
                new[] { "ForgeBandit", "SpawnBanditParty" }, new[] { "ForgeMercenary", "HireMercenary" },
                new[] { "ForgeTrade", "ModifyPrice" }, new[] { "ForgeWorkshopProduction", "SetSpeed" },
                new[] { "ForgeSiegeEngine", "SpawnEngine" }, new[] { "ForgeRebellion", "StartRebellion" },
                new[] { "ForgeFormation", "OrderCharge" }, new[] { "ForgeOrder", "OrderRetreat" },
                new[] { "ForgeWoundRate", "SetSurvivalChance" }, new[] { "ForgeHeir", "SetHeir" },
                new[] { "ForgeNotable", "SpawnNotable" }, new[] { "ForgeCaravanGuard", "AddGuards" }
            };

            foreach (string[] entry in methods)
            {
                Type type = assembly.GetType("CalradiaForge.Core.Helpers." + entry[0]);
                MethodInfo method = type?.GetMethod(entry[1], BindingFlags.Public | BindingFlags.Static);
                if (method == null) throw new Exception("Expected helper method is missing: " + entry[0] + "." + entry[1]);
                object[] arguments = method.GetParameters().Select(parameter =>
                    parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null).ToArray();
                try
                {
                    method.Invoke(null, arguments);
                    throw new Exception(entry[0] + "." + entry[1] + " silently returned without performing an action.");
                }
                catch (TargetInvocationException error)
                {
                    if (!(error.InnerException is NotSupportedException) ||
                        !error.InnerException.Message.Contains("no action was performed"))
                        throw new Exception(entry[0] + "." + entry[1] + " did not report unsupported behavior clearly.", error);
                }
            }
#endif
        }

        private static void TestForgeAgentMemoryTTLBoundaries()
        {
            ForgeAgentMemory.ClearAll();
            try
            {
                const string expiredAgent = "ttl_min_value";
                if (!ForgeAgentMemory.Semantic.TryUpsert(
                    expiredAgent, "fact", "expired", TimeSpan.MinValue))
                    throw new Exception("A valid immediately-expired TTL was rejected.");
                if (ForgeAgentMemory.Semantic.Get<string>(expiredAgent, "fact") != null)
                    throw new Exception("TimeSpan.MinValue was not treated as already expired.");

                const string zeroTtlAgent = "ttl_zero_value";
                ForgeAgentMemory.Semantic.Upsert(zeroTtlAgent, "fact", "expired", TimeSpan.Zero);
                if (ForgeAgentMemory.Semantic.Get<string>(zeroTtlAgent, "fact") != null)
                    throw new Exception("A zero TTL should expire the entry immediately.");

                const string longTtlAgent = "ttl_max_value";
                if (!ForgeAgentMemory.Semantic.TryUpsert(
                    longTtlAgent, "fact", "long-lived", TimeSpan.MaxValue))
                    throw new Exception("TimeSpan.MaxValue should be accepted as a long-lived TTL.");
                if (ForgeAgentMemory.Semantic.Get<string>(longTtlAgent, "fact") != "long-lived")
                    throw new Exception("A saturated long-lived TTL was not retained.");
            }
            finally
            {
                ForgeAgentMemory.ClearAll();
            }
        }

        private static void TestForgeAgentMemoryTierQuotas()
        {
            ForgeAgentMemory.ClearAll();
            try
            {
                const string semanticAgent = "quota_semantic";
                for (int i = 0; i < ForgeAgentMemory.MaximumSemanticEntriesPerAgent; i++)
                {
                    if (!ForgeAgentMemory.Semantic.TryUpsert(semanticAgent, "key_" + i, i))
                        throw new Exception("Semantic memory rejected a key before reaching its limit.");
                }

                if (ForgeAgentMemory.Semantic.TryUpsert(semanticAgent, "overflow", 999))
                    throw new Exception("Semantic memory accepted a new key over its per-agent limit.");
                if (!ForgeAgentMemory.Semantic.TryUpsert(semanticAgent, "key_0", "replaced") ||
                    ForgeAgentMemory.Semantic.Get<string>(semanticAgent, "key_0") != "replaced")
                    throw new Exception("Semantic memory did not allow replacing a key at capacity.");

                bool semanticLegacyThrew = false;
                try
                {
                    ForgeAgentMemory.Semantic.Upsert(semanticAgent, "legacy_overflow", 1000);
                }
                catch (InvalidOperationException)
                {
                    semanticLegacyThrew = true;
                }
                if (!semanticLegacyThrew)
                    throw new Exception("Semantic legacy Upsert hid a quota rejection.");

                const string proceduralAgent = "quota_procedural";
                for (int i = 0; i < ForgeAgentMemory.MaximumProceduralEntriesPerAgent; i++)
                {
                    if (!ForgeAgentMemory.Procedural.TryAdd(proceduralAgent, "task_" + i, i))
                        throw new Exception("Procedural memory rejected a task before reaching its limit.");
                }

                if (ForgeAgentMemory.Procedural.TryAdd(proceduralAgent, "overflow", 999))
                    throw new Exception("Procedural memory accepted a new task over its per-agent limit.");
                if (!ForgeAgentMemory.Procedural.TryAdd(proceduralAgent, "task_0", "replaced") ||
                    ForgeAgentMemory.Procedural.Get<string>(proceduralAgent, "task_0") != "replaced")
                    throw new Exception("Procedural memory did not allow replacing a task at capacity.");

                bool proceduralLegacyThrew = false;
                try
                {
                    ForgeAgentMemory.Procedural.Add(proceduralAgent, "legacy_overflow", 1000);
                }
                catch (InvalidOperationException)
                {
                    proceduralLegacyThrew = true;
                }
                if (!proceduralLegacyThrew)
                    throw new Exception("Procedural legacy Add hid a quota rejection.");

                const string typeAgent = "quota_episode_type";
                for (int i = 0; i < ForgeAgentMemory.MaximumEpisodicEntriesPerType; i++)
                {
                    if (!ForgeAgentMemory.Episodic.TryAdd(typeAgent, "battle", i))
                        throw new Exception("Episodic memory rejected an entry at its type boundary.");
                }

                if (!ForgeAgentMemory.Episodic.TryAdd(typeAgent, "battle", 128) ||
                    ForgeAgentMemory.Episodic.Count(typeAgent, "battle") != ForgeAgentMemory.MaximumEpisodicEntriesPerType)
                    throw new Exception("Episodic per-type FIFO limit was not enforced.");
                var typeEpisodes = ForgeAgentMemory.Episodic.GetAll(typeAgent, "battle");
                if ((int)typeEpisodes[0] != 1 || (int)typeEpisodes[typeEpisodes.Count - 1] != 128)
                    throw new Exception("Episodic per-type FIFO did not evict the oldest episode.");

                const string totalAgent = "quota_episode_total";
                for (int type = 0; type < 4; type++)
                {
                    for (int i = 0; i < ForgeAgentMemory.MaximumEpisodicEntriesPerType; i++)
                    {
                        if (!ForgeAgentMemory.Episodic.TryAdd(totalAgent, "type_" + type, i))
                            throw new Exception("Episodic memory rejected an entry before reaching its total limit.");
                    }
                }

                if (ForgeAgentMemory.Episodic.TotalCount(totalAgent) != ForgeAgentMemory.MaximumEpisodicEntriesPerAgent)
                    throw new Exception("Episodic memory did not reach its total per-agent limit.");
                if (!ForgeAgentMemory.Episodic.TryAdd(totalAgent, "new_type", "latest") ||
                    ForgeAgentMemory.Episodic.TotalCount(totalAgent) != ForgeAgentMemory.MaximumEpisodicEntriesPerAgent)
                    throw new Exception("Episodic total FIFO limit was not enforced.");
                var oldestType = ForgeAgentMemory.Episodic.GetAll(totalAgent, "type_0");
                if (oldestType.Count != ForgeAgentMemory.MaximumEpisodicEntriesPerType - 1 ||
                    (int)oldestType[0] != 1)
                    throw new Exception("Episodic total FIFO did not evict the oldest episode across types.");
            }
            finally
            {
                ForgeAgentMemory.ClearAll();
            }
        }

        private static void TestForgeAgentMemoryAgentQuotaAndClearing()
        {
            ForgeAgentMemory.ClearAll();
            try
            {
                for (int i = 0; i < ForgeAgentMemory.MaximumAgents; i++)
                {
                    if (!ForgeAgentMemory.Semantic.TryUpsert("global_" + i, "fact", i))
                        throw new Exception("Global agent quota rejected an ID before the exact limit.");
                }

                var additionalSemantic = new ForgeAgentMemory.SemanticMemory();
                var additionalEpisodic = new ForgeAgentMemory.EpisodicMemory();
                var additionalProcedural = new ForgeAgentMemory.ProceduralMemory();
                if (additionalSemantic.TryUpsert("external_instance_overflow", "fact", "rejected"))
                    throw new Exception("A public memory instance bypassed the process-wide agent quota.");
                if (!additionalSemantic.TryUpsert("global_0", "fact", "updated") ||
                    !additionalEpisodic.TryAdd("global_0", "external", "kept") ||
                    !additionalProcedural.TryAdd("global_0", "external", "kept"))
                    throw new Exception("A public memory instance did not share the bounded agent store.");

                if (!ForgeAgentMemory.Episodic.TryAdd("global_0", "event", "kept") ||
                    !ForgeAgentMemory.Procedural.TryAdd("global_0", "task", "kept"))
                    throw new Exception("The same agent ID was counted more than once across tiers.");
                if (ForgeAgentMemory.Episodic.TryAdd("global_overflow", "event", "rejected"))
                    throw new Exception("Global agent quota accepted an ID over its exact limit.");

                bool episodicLegacyThrew = false;
                try
                {
                    ForgeAgentMemory.Episodic.Add("legacy_global_overflow", "event", "rejected");
                }
                catch (InvalidOperationException)
                {
                    episodicLegacyThrew = true;
                }
                if (!episodicLegacyThrew)
                    throw new Exception("Episodic legacy Add hid a global quota rejection.");

                ForgeAgentMemory.Semantic.ClearAgent("global_0");
                if (ForgeAgentMemory.Procedural.TryAdd("still_full", "task", "rejected"))
                    throw new Exception("Clearing one tier released an agent still present in other tiers.");

                ForgeAgentMemory.ClearAgent("global_0");
                if (ForgeAgentMemory.Semantic.Get<string>("global_0", "fact") != null ||
                    ForgeAgentMemory.Episodic.TotalCount("global_0") != 0 ||
                    ForgeAgentMemory.Procedural.Get<string>("global_0", "task") != null ||
                    additionalSemantic.Get<string>("global_0", "fact") != null ||
                    additionalEpisodic.TotalCount("global_0") != 0 ||
                    additionalProcedural.Get<string>("global_0", "external") != null)
                    throw new Exception("ClearAgent did not clear the agent from all three tiers.");
                if (!ForgeAgentMemory.Procedural.TryAdd("released_slot", "task", "accepted"))
                    throw new Exception("ClearAgent did not release the global agent slot.");

                ForgeAgentMemory.ClearAll();
                if (ForgeAgentMemory.Semantic.GetKeys("global_1").Count != 0 ||
                    additionalSemantic.GetKeys("global_1").Count != 0 ||
                    !ForgeAgentMemory.Semantic.TryUpsert("after_clear_all", "fact", "accepted"))
                    throw new Exception("ClearAll did not clear tier data and the global agent registry.");
            }
            finally
            {
                ForgeAgentMemory.ClearAll();
            }
        }

        private static void TestForgeAgentMemoryExpiredGlobalSlot()
        {
            ForgeAgentMemory.ClearAll();
            try
            {
                for (int i = 0; i < ForgeAgentMemory.MaximumAgents - 1; i++)
                {
                    if (!ForgeAgentMemory.Semantic.TryUpsert("ttl_global_" + i, "fact", i))
                        throw new Exception("Global agent quota rejected an ID before the TTL boundary case.");
                }

                string expiredAgent = "ttl_global_expired";
                if (!ForgeAgentMemory.Semantic.TryUpsert(
                    expiredAgent, "fact", "expired", TimeSpan.FromMilliseconds(-1)))
                    throw new Exception("The expired semantic entry could not be recorded.");

                if (!ForgeAgentMemory.Procedural.TryAdd("ttl_global_reuse", "task", "accepted"))
                    throw new Exception("An expired final semantic fact did not release its global agent slot.");
                if (ForgeAgentMemory.Semantic.GetKeys(expiredAgent).Count != 0)
                    throw new Exception("Expired semantic data remained after global quota cleanup.");
                if (ForgeAgentMemory.Episodic.TryAdd("ttl_global_overflow", "event", "rejected"))
                    throw new Exception("TTL cleanup admitted more than the global agent limit.");
            }
            finally
            {
                ForgeAgentMemory.ClearAll();
            }
        }

        private static void TestForgeLocalApiEndpoints()
        {
            // Use a random high port to avoid conflicts with the main API instance
            string url = "http://localhost:59997/";
            ForgeAgentMemory.ClearAll();
            const string privateAgentId = "private-agent-id-must-not-leak";
            const string privateKey = "private-key-must-not-leak";
            const string privatePayload = "private-payload-must-not-leak";
            ForgeAgentMemory.Semantic.Upsert(privateAgentId, privateKey, privatePayload);
            ForgeAgentMemory.Episodic.Add(privateAgentId, "test-event", privatePayload);
            ForgeAgentMemory.Procedural.Add(privateAgentId, "test-task", privatePayload);
            using (var api = new CalradiaForge.Sdk.Api.ForgeLocalApi())
            {
                try
                {
                    api.Start(url);
                    if (!api.IsRunning) throw new Exception("API did not start.");

                    WaitForApiReady(url);

                    using (var http = new System.Net.WebClient())
                    {
                        http.Headers["Accept"] = "application/json";

                        // /info — verify version string
                        var info = http.DownloadString(url + "info");
                        if (!info.Contains("5.0.0")) throw new Exception("/info did not return version 5.0.0. Got: " + info);

                        // /agents retains the collection field but only exposes aggregate counts.
                        var agents = http.DownloadString(url + "agents");
                        if (!agents.Contains("\"agents\":[]") || !agents.Contains("\"agentCount\":1") ||
                            !agents.Contains("\"semanticEntries\":1") || !agents.Contains("\"episodicEntries\":1") ||
                            !agents.Contains("\"proceduralEntries\":1"))
                            throw new Exception("/agents did not return the expected aggregate memory statistics. Got: " + agents);
                        if (agents.Contains(privateAgentId) || agents.Contains(privateKey) || agents.Contains(privatePayload))
                            throw new Exception("/agents exposed a private identity, memory key, or stored payload.");

                        // CORS header via HttpWebRequest
                        var req = (HttpWebRequest)WebRequest.Create(url + "info");
                        using (var resp = (HttpWebResponse)req.GetResponse())
                        {
                            var cors = resp.Headers["Access-Control-Allow-Origin"];
                            if (cors != "*") throw new Exception("CORS header missing. Got: " + cors);
                        }

                        const int statusRequests = 32;
                        var apiWatch = Stopwatch.StartNew();
                        for (int index = 0; index < statusRequests; index++)
                        {
                            if (http.DownloadString(url + "status") != "{\"status\":\"running\"}")
                                throw new Exception("The status endpoint returned an unexpected response.");
                        }
                        apiWatch.Stop();
                        Console.WriteLine("PERF ForgeLocalApi.Status requests=" + statusRequests +
                            " total_ms=" + apiWatch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture));
                    }

                    api.Stop();
                }
                finally
                {
                    ForgeAgentMemory.ClearAll();
                }
            }
        }

        private static void WaitForApiReady(string url)
        {
            var timeout = Stopwatch.StartNew();
            WebException lastConnectionError = null;
            while (timeout.Elapsed < TimeSpan.FromSeconds(2))
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(url + "status");
                    request.Timeout = 150;
                    request.ReadWriteTimeout = 150;
                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var body = reader.ReadToEnd();
                        if (response.StatusCode != HttpStatusCode.OK || body != "{\"status\":\"running\"}")
                            throw new InvalidOperationException("/status returned an unexpected readiness response: " + body);
                        return;
                    }
                }
                catch (WebException error)
                {
                    if (error.Response != null)
                        throw new InvalidOperationException("ForgeLocalApi returned an HTTP error while checking readiness.", error);
                    lastConnectionError = error;
                    Thread.Sleep(10);
                }
            }

            throw new TimeoutException("ForgeLocalApi did not become reachable within 2 seconds.", lastConnectionError);
        }

        private static void TestForgeCampaignEventsErrorLogging()
        {
            var engine = new TestEngine();
            engine.Log = new SessionLog();
            ForgeApi.Connect(engine);
            try
            {
                // Subscribe a callback that always throws
                var throwingEvent = new ForgeEvent(
                    ForgeEventKind.ForgeReady,
                    Context.Any,          // use Context overload — no ITestServices needed
                    sequence: 99,
                    deltaMilliseconds: 0,
                    data: new System.Collections.Generic.Dictionary<string, string>(),
                    cancellation: System.Threading.CancellationToken.None
                );
                ForgeCampaignEvents.Subscribe(throwingEvent, _ => throw new InvalidOperationException("Intentional test error"));

                // Dispatch via reflection since it is intentionally internal
                var dispatch = typeof(ForgeCampaignEvents).GetMethod(
                    "Dispatch",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public,
                    null,
                    new[] { typeof(ForgeEvent), typeof(object[]) },
                    null);
                if (dispatch == null) throw new Exception("ForgeCampaignEvents.Dispatch not found via reflection.");
                dispatch.Invoke(null, new object[] { throwingEvent, new object[0] });

                // Verify the error was logged
                var records = engine.Log.Deserialize();
                if (!records.Any(r => r.Level == "Error"))
                    throw new Exception("ForgeCampaignEvents did not log dispatch error.");
            }
            finally
            {
                ForgeApi.Disconnect();
            }
        }

        private static void TestForgeApiVersion()
        {
            if (ForgeApi.Version != 10)
                throw new Exception($"ForgeApi.Version should be 10. Got: {ForgeApi.Version}");
        }

        private static void TestForgeUiRegistry()
        {
            var engine = new TestEngine();
            var page = new ForgeUiPageDescriptor { Id = "test.ui.page", Owner = "test_mod", Prefab = "ForgeExamplesPage", TitleKey = "test.title", ViewModelType = typeof(ForgeExamplesPageViewModel), Commands = Array.Empty<ForgeUiCommandDescriptor>() };
            engine.Register(page);
            if (engine.FindPage(page.Id) == null || engine.GetPages().Count != 1) throw new Exception("Registered page was not discoverable.");
            try { engine.Register(page); throw new Exception("Duplicate UI page ID was accepted."); }
            catch (ArgumentException) { }
            if (engine.RemoveOwner("test_mod") != 1 || engine.FindPage(page.Id) != null) throw new Exception("Owner cleanup did not release the registered page.");
        }

        private static void TestForgeUiDiscovery()
        {
            var pageType = typeof(ForgeExamplesPageViewModel);
            var tempRoot = Path.Combine(Path.GetTempPath(), "CalradiaForgeUiDiscovery-" + Guid.NewGuid().ToString("N"));
            var root = Path.Combine(tempRoot, "CalradiaForgeExamples");
            var prefabPath = Path.Combine(root, "GUI", "Prefabs", "ForgeExamplesPage.xml");
            var assemblyPath = Path.Combine(root, "bin", "Win64_Shipping_Client", "CalradiaForge.Examples.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
            Directory.CreateDirectory(Path.GetDirectoryName(assemblyPath));
            File.WriteAllText(prefabPath, "<Prefab><Window><ButtonWidget Command.Click=\"ExecuteClose\" /></Window></Prefab>");
            File.WriteAllBytes(assemblyPath, new byte[] { 0x4D, 0x5A });
            try
            {
                var page = ForgeUiDiscovery.DescribeAt(pageType, "CalradiaForgeExamples", assemblyPath);
                if (page == null || page.Commands.Count != 1 || page.Commands[0].Binding != "ExecuteClose")
                    throw new Exception("Valid Gauntlet page metadata was not discovered.");

                File.WriteAllText(prefabPath, "<Prefab><Window><ButtonWidget Command.Click=\"ExecuteOther\" /></Window></Prefab>");
                try { ForgeUiDiscovery.DescribeAt(pageType, "CalradiaForgeExamples", assemblyPath); throw new Exception("A missing Gauntlet command binding was accepted."); }
                catch (InvalidOperationException) { }
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
        }

        private static void TestForgeUiPolicy()
        {
            var command = new ForgeUiCommandDescriptor { Id = "mutate", Binding = "ExecuteMutate", Context = Context.Campaign, ChangesState = true };
            var page = new ForgeUiPageDescriptor { Id = "test.mutate", Owner = "test_mod", Prefab = "Test", TitleKey = "test.title", Context = Context.Any, ViewModelType = typeof(ForgeExamplesPageViewModel), Commands = new[] { command } };
            if (ForgeUiPolicy.GetUnavailableReason(page, Context.Mission, false, false) == null)
                throw new Exception("Command context mismatch was accepted.");
            if (ForgeUiPolicy.GetUnavailableReason(page, Context.Campaign, false, false) == null)
                throw new Exception("State-changing command was allowed outside test mode.");
            if (ForgeUiPolicy.GetUnavailableReason(page, Context.Campaign, true, false) == null)
                throw new Exception("Campaign mutation was allowed without a copied-campaign confirmation.");
            if (ForgeUiPolicy.GetUnavailableReason(page, Context.Campaign, true, true) != null)
                throw new Exception("A gated campaign command was rejected after all required gates were enabled.");
        }

        private static void TestModRuleAuditorSafety()
        {
            string root = Path.Combine(Path.GetTempPath(), "CF_Audit_Safety_" + Guid.NewGuid().ToString("N"));
            string reparseRoot = root + "_reparse";
            string rootJunction = root + "_root_link";
            string childJunction = Path.Combine(reparseRoot, "linked-content");
            string outsideTarget = root + "_outside";
            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(Path.Combine(root, "invalid.xml"), "<broken>");
                File.WriteAllText(Path.Combine(root, "BelowThreshold.cs"),
                    "class Below { void Register() { var item = new ForgeEventSubscription { Event = ForgeEventKind.Pulse, MinimumIntervalMilliseconds = 49 }; } }");
                File.WriteAllText(Path.Combine(root, "AtThreshold.cs"),
                    "class At { void Register() { var item = new ForgeEventSubscription { Event = ForgeEventKind.Pulse, MinimumIntervalMilliseconds = 50 }; } }");
                File.WriteAllText(Path.Combine(root, "CommentSpoof.cs"),
                    "// MinimumIntervalMilliseconds = 999\nclass Spoof { void Register() { var item = new ForgeEventSubscription { Event = ForgeEventKind.Pulse }; } }");
                File.WriteAllText(Path.Combine(root, "CommentOnly.cs"),
                    "// ForgeEventKind.Pulse new ForgeEventSubscription { MinimumIntervalMilliseconds = 1 }");
                File.WriteAllText(Path.Combine(root, "blocked.xml"), "<root />");

                // Exceed the shared per-file bound with a sparse file so the audit rejects the
                // candidate before reading or allocating its full contents.
                string oversized = Path.Combine(root, "Oversized.cs");
                using (var stream = new FileStream(oversized, FileMode.Create, FileAccess.Write, FileShare.None))
                    stream.SetLength(16L * 1024 * 1024 + 1);

                string current = root;
                for (int i = 0; i < 65; i++)
                {
                    current = Path.Combine(current, "d");
                    Directory.CreateDirectory(current);
                }

                var result = ModRuleAuditor.Audit(root);
                if (result.Passed || !result.Findings.Any(f => f.RuleId == "AUDIT_XML_INVALID" && f.FilePath.EndsWith("invalid.xml")))
                    throw new Exception("Malformed XML did not produce an error that prevents a Passed audit.");
                if (!result.Findings.Any(f => f.RuleId == "AUDIT_INPUT_LIMIT" && f.FilePath.EndsWith("Oversized.cs")))
                    throw new Exception("A source file over the shared analysis size limit was not rejected.");
                if (!result.Findings.Any(f => f.RuleId == "AUDIT_SCAN_INCOMPLETE"))
                    throw new Exception("The depth-bounded tree did not produce an incomplete-scan error.");
                if (!result.Findings.Any(f => f.RuleId == "FORGEWEAVE_UNTHROTTLED_PULSE" && f.FilePath.EndsWith("BelowThreshold.cs")))
                    throw new Exception("A 49 ms Pulse throttle was not rejected.");
                if (!result.Findings.Any(f => f.RuleId == "FORGEWEAVE_UNTHROTTLED_PULSE" && f.FilePath.EndsWith("CommentSpoof.cs")))
                    throw new Exception("A comment did not incorrectly rescue an unthrottled Pulse subscription.");
                if (result.Findings.Any(f => f.FilePath.EndsWith("AtThreshold.cs") && f.RuleId == "FORGEWEAVE_UNTHROTTLED_PULSE"))
                    throw new Exception("The explicit 50 ms Pulse threshold was rejected.");
                if (result.Findings.Any(f => f.FilePath.EndsWith("CommentOnly.cs") && f.RuleId == "FORGEWEAVE_UNTHROTTLED_PULSE"))
                    throw new Exception("Pulse text found only in a comment was treated as executable code.");

                using (var exclusiveReadLock = new FileStream(Path.Combine(root, "blocked.xml"),
                    FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    var unreadableResult = ModRuleAuditor.Audit(root);
                    if (!unreadableResult.Findings.Any(f => f.RuleId == "AUDIT_INPUT_UNREADABLE" && f.FilePath.EndsWith("blocked.xml")))
                        throw new Exception("An XML file that could not be opened was omitted instead of producing an error.");
                }

                var analyzerIds = ForgeAnalysisCatalog.BuiltInAnalyzerIds;
                var analyzerList = analyzerIds as IList<string>;
                if (analyzerList == null || !analyzerList.IsReadOnly)
                    throw new Exception("BuiltInAnalyzerIds is not exposed as an immutable collection.");
                try
                {
                    analyzerList[0] = "mutated";
                    throw new Exception("BuiltInAnalyzerIds accepted a mutation.");
                }
                catch (NotSupportedException) { }
                if (analyzerIds[0] != "module")
                    throw new Exception("A caller changed the built-in analyzer registry.");

                string trustedRepo = root + "_trusted_repo";
                try
                {
                    Directory.CreateDirectory(Path.Combine(trustedRepo, "src", "CalradiaForge.Core"));
                    Directory.CreateDirectory(Path.Combine(trustedRepo, "tools"));
                    Directory.CreateDirectory(Path.Combine(trustedRepo, "modules", "Sample", "tools"));
                    File.WriteAllText(Path.Combine(trustedRepo, "CalradiaForge.sln"), "");
                    string trustedScript = Path.Combine(trustedRepo, "tools", "local-helper.bat");
                    string nestedScript = Path.Combine(trustedRepo, "modules", "Sample", "tools", "bundled-helper.bat");
                    File.WriteAllText(trustedScript, "@echo off");
                    File.WriteAllText(nestedScript, "@echo off");
                    var trustedResult = ModRuleAuditor.Audit(trustedRepo);
                    if (trustedResult.Findings.Any(f => f.RuleId == "DISTRIBUTION_SAFETY" && f.FilePath == trustedScript))
                        throw new Exception("The intended repository-root tools folder was not exempted.");
                    if (!trustedResult.Findings.Any(f => f.RuleId == "DISTRIBUTION_SAFETY" && f.FilePath == nestedScript))
                        throw new Exception("A nested module tools folder incorrectly inherited the repository exemption.");
                }
                finally
                {
                    if (Directory.Exists(trustedRepo))
                    {
                        try { Directory.Delete(trustedRepo, true); } catch { }
                    }
                }

                // On Windows a directory junction can be created without following its target.
                // If the host does not support mklink, fail the regression rather than silently
                // claiming that reparse-point handling was tested.
                Directory.CreateDirectory(outsideTarget);
                File.WriteAllText(Path.Combine(outsideTarget, "ShouldNotBeScanned.cs"), "class Campaign {}");
                Directory.CreateDirectory(reparseRoot);
                CreateJunction(childJunction, outsideTarget);
                var childLinkResult = ModRuleAuditor.Audit(reparseRoot);
                if (!childLinkResult.Findings.Any(f => f.RuleId == "AUDIT_SCAN_INCOMPLETE") ||
                    childLinkResult.Findings.Any(f => f.FilePath.EndsWith("ShouldNotBeScanned.cs")))
                    throw new Exception("An untraversed child reparse point did not prevent a Passed audit.");

                CreateJunction(rootJunction, reparseRoot);
                var rootLinkResult = ModRuleAuditor.Audit(rootJunction);
                if (!rootLinkResult.Findings.Any(f => f.RuleId == "AUDIT_SCAN_INCOMPLETE"))
                    throw new Exception("A selected reparse-point root did not prevent a Passed audit.");
            }
            finally
            {
                TryDeleteJunction(rootJunction);
                TryDeleteJunction(childJunction);
                if (Directory.Exists(root))
                {
                    try { Directory.Delete(root, true); } catch { }
                }
                if (Directory.Exists(reparseRoot))
                {
                    try { Directory.Delete(reparseRoot, true); } catch { }
                }
                if (Directory.Exists(outsideTarget))
                {
                    try { Directory.Delete(outsideTarget, true); } catch { }
                }
            }
        }

        private static void CreateJunction(string junctionPath, string targetPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
                Arguments = "/d /c mklink /J \"" + junctionPath + "\" \"" + targetPath + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException("Could not create the reparse-point regression fixture: " + output + error);
            }
        }

        private static void TryDeleteJunction(string path)
        {
            if (!Directory.Exists(path)) return;
            try { Directory.Delete(path, false); } catch { }
        }

        private static void TestForgeAgentMemoryNullSafety()
        {
            ForgeAgentMemory.ClearAll();

            if (ForgeAgentMemory.Semantic.Get<string>(null, "key") != null) throw new Exception("Semantic.Get with null agentId should return default.");
            if (ForgeAgentMemory.Semantic.Get<string>("hero", null) != null) throw new Exception("Semantic.Get with null key should return default.");
            if (ForgeAgentMemory.Semantic.GetKeys(null).Count != 0) throw new Exception("Semantic.GetKeys with null agentId should return empty list.");

            if (ForgeAgentMemory.Episodic.GetAll(null, "type").Count != 0) throw new Exception("Episodic.GetAll with null agentId should return empty list.");
            if (ForgeAgentMemory.Episodic.GetAll("hero", null).Count != 0) throw new Exception("Episodic.GetAll with null type should return empty list.");
            if (ForgeAgentMemory.Episodic.Count(null, "type") != 0) throw new Exception("Episodic.Count with null agentId should return 0.");
            if (ForgeAgentMemory.Episodic.Count("hero", null) != 0) throw new Exception("Episodic.Count with null type should return 0.");
            if (ForgeAgentMemory.Episodic.TotalCount(null) != 0) throw new Exception("Episodic.TotalCount with null agentId should return 0.");

            if (ForgeAgentMemory.Procedural.Get<string>(null, "task") != null) throw new Exception("Procedural.Get with null agentId should return default.");
            if (ForgeAgentMemory.Procedural.Get<string>("hero", null) != null) throw new Exception("Procedural.Get with null task should return default.");
            if (ForgeAgentMemory.Procedural.GetTaskNames(null).Count != 0) throw new Exception("Procedural.GetTaskNames with null agentId should return empty list.");

            ForgeAgentMemory.ClearAll();
        }

        private static void TestForgeTimeSlicerBoundaries()
        {
            var entities = new List<string> { "e1", "e2", "e3" };

            int processedZero = ForgeTimeSlicer.ProcessBatch(entities, id => id, _ => { }, 5, 0);
            if (processedZero != 3) throw new Exception($"ProcessBatch with 0 buckets should process all entities; got {processedZero}.");

            int processedNeg = ForgeTimeSlicer.ProcessBatch((IEnumerable<string>)entities, id => id, _ => { }, 5, -5);
            if (processedNeg != 3) throw new Exception($"ProcessBatch with negative buckets should process all entities; got {processedNeg}.");

            int processedOne = ForgeTimeSlicer.ProcessBatch(entities, id => id, _ => { }, 5, 1);
            if (processedOne != 3) throw new Exception($"ProcessBatch with 1 bucket should process all entities; got {processedOne}.");

            if (!ForgeTimeSlicer.ShouldProcess("e1", 5, 0)) throw new Exception("ShouldProcess with 0 buckets should return true.");
            if (!ForgeTimeSlicer.ShouldProcess("e1", 5, 1)) throw new Exception("ShouldProcess with 1 bucket should return true.");
            if (ForgeTimeSlicer.GetBucket("e1", 0) != 0) throw new Exception("GetBucket with 0 buckets should return 0.");
            if (ForgeTimeSlicer.GetBucket("e1", 1) != 0) throw new Exception("GetBucket with 1 bucket should return 0.");
        }
        private static void TestSdkHardeningAndOptimizations()
        {
            // 1. ForgeText.EscapeXmlText fast path
            string plainText = "SimpleIdentifier_NoEscaping";
            string plainResult = ForgeText.EscapeXmlText(plainText);
            if (!object.ReferenceEquals(plainText, plainResult))
                throw new Exception("EscapeXmlText should return the original string reference when no XML escaping is required.");

            string xmlText = "Attack <Now> & Win \"Glory\"\nNext";
            string xmlResult = ForgeText.EscapeXmlText(xmlText);
            if (!xmlResult.Contains("&lt;") || !xmlResult.Contains("&gt;") || !xmlResult.Contains("&amp;") || !xmlResult.Contains("&#10;"))
                throw new Exception("EscapeXmlText failed to escape XML entities properly: " + xmlResult);

            // 2. ForgeData hot-path read and cached delegate
            var dummyEntity = new object();
            var d1 = dummyEntity.GetForgeData<DummyConfig>();
            if (d1 == null) throw new Exception("GetForgeData returned null.");
            d1.Name = "Persisted";
            var d2 = dummyEntity.GetForgeData<DummyConfig>();
            if (!object.ReferenceEquals(d1, d2) || d2.Name != "Persisted")
                throw new Exception("GetForgeData fast path did not return the existing attached instance.");

            // 3. ForgeDialogueBuilder standard tokens & duplicate rejection
            var diagBuilder = ForgeDialogueBuilder.Create("test_diag", "hero_main_options");
            diagBuilder.PlayerLine("opt1", "Hello", "npc_reply");
            diagBuilder.NpcReply("reply1", "Greetings", "close_window");
            var diagLines = diagBuilder.Build();
            if (diagLines.Count != 2) throw new Exception("ForgeDialogueBuilder failed to build valid lines.");

            var badDiag = ForgeDialogueBuilder.Create("bad_diag");
            badDiag.PlayerLine("dup_id", "Choice 1", "hero_main_options");
            badDiag.NpcReply("dup_id", "Choice 2", "close_window");
            var diagErrors = badDiag.Validate();
            if (diagErrors.Count == 0 || !diagErrors[0].Contains("Duplicate"))
                throw new Exception("ForgeDialogueBuilder failed to detect duplicate line ID.");

            // 4. ForgeQuestBuilder anti-shadowing guard
            bool caughtCampaignClass = false;
            try { ForgeQuestBuilder.Create("q1", "Campaign"); }
            catch (InvalidOperationException) { caughtCampaignClass = true; }
            if (!caughtCampaignClass) throw new Exception("ForgeQuestBuilder allowed class name 'Campaign' which violates GEMINI.md.");

            bool caughtCampaignNs = false;
            try { ForgeQuestBuilder.Create("q1", "ValidQuest").WithNamespace("MyMod.Campaign.Quests"); }
            catch (InvalidOperationException) { caughtCampaignNs = true; }
            if (!caughtCampaignNs) throw new Exception("ForgeQuestBuilder allowed namespace segment 'Campaign' which violates GEMINI.md.");

            // 5. ForgeAudioBuilder duplicate sound detection
            var audioBuilder = ForgeAudioBuilder.Create();
            audioBuilder.Add2DSound("click", "ui_click.ogg");
            audioBuilder.Add2DSound("click", "ui_click2.ogg");
            var (audioValid, audioErrors) = audioBuilder.Validate();
            if (audioValid || audioErrors.Count == 0 || !audioErrors[0].Contains("Duplicate"))
                throw new Exception("ForgeAudioBuilder failed to detect duplicate sound name.");

            // 6. ForgeAudioInspector duplicate sound detection
            string duplicateXml = "<module_sounds><module_sound name=\"same_sound\" is_2d=\"true\" sound_category=\"ui\" path=\"s1.ogg\" /><module_sound name=\"same_sound\" is_2d=\"true\" sound_category=\"ui\" path=\"s2.ogg\" /></module_sounds>";
            var auditResult = ForgeAudioInspector.AuditSoundManifest(duplicateXml);
            if (auditResult.ErrorCount == 0)
                throw new Exception("ForgeAudioInspector failed to flag duplicate sound name as error.");

            // 7. ForgePartySpawner.EstimateRosterWage null and negative guards
            if (ForgePartySpawner.EstimateRosterWage(null, null) != 0)
                throw new Exception("EstimateRosterWage with null roster should return 0.");
            var testRoster = new Dictionary<string, int> { { "t1", -5 }, { "t2", 10 } };
            int wage = ForgePartySpawner.EstimateRosterWage(testRoster, id => 3);
            if (wage != 10 * Math.Max(2, 3 * 3))
                throw new Exception("EstimateRosterWage did not calculate wages correctly with skipped negative counts: got " + wage);

            // 8. ForgeTradeSimulator workshop economics
            var silverRoi = ForgeTradeSimulator.SimulateWorkshopRoi("Ortysia", "Silversmith", 10000, 10);
            var beerRoi = ForgeTradeSimulator.SimulateWorkshopRoi("Pravend", "Brewery", 10000, 10);
            if (silverRoi.TotalRevenue <= beerRoi.TotalRevenue)
                throw new Exception("ForgeTradeSimulator did not differentiate Silversmith economics from Brewery.");

            // 9. ForgeTradeSystem boundary clamping
            int price = ForgeTradeSystem.CalculatePriceBySupplyDemand(100, -50, 100);
            if (price <= 0 || price > 500)
                throw new Exception("CalculatePriceBySupplyDemand did not clamp negative supply safely: got " + price);
            int net = ForgeTradeSystem.CalculateWorkshopDailyNet(10, 20, -5, 60);
            if (net != -60)
                throw new Exception("CalculateWorkshopDailyNet did not clamp negative volume safely: got " + net);

            // 10. ForgeSettlementSystem boundary clamping
            var (isCrit, danger, status) = ForgeSettlementSystem.EvaluateRebellionRisk(10f, -20, -10);
            if (float.IsNaN(danger) || danger < 0f)
                throw new Exception("EvaluateRebellionRisk produced invalid danger index on negative inputs.");

            // 11. ForgeCombatTactics boundary clamping
            var (breachProb, remainingHp) = ForgeCombatTactics.CalculateSiegeWallBreachProbability(-10, -5, 0);
            if (remainingHp < 10000f || breachProb != 0f)
                throw new Exception("CalculateSiegeWallBreachProbability failed boundary clamping for Tier 0/negative shots.");
            float gateDmg = ForgeCombatTactics.CalculateGateDamage(-5, 0, -2f);
            if (gateDmg != 0f)
                throw new Exception("CalculateGateDamage failed boundary clamping for negative hits.");

            // 12. ForgeItemBuilder boundary clamping & whitespace fallback
            var item = ForgeItemBuilder.Create("test_sword")
                .WithName("  ")
                .WithMesh("")
                .AsWeapon("OneHandedSword", -10, -5, -20, "Cut", -30, "Pierce")
                .BuildElement();
            if ((string)item.Attribute("name") != "test_sword" || (string)item.Attribute("mesh") != "test_sword_mesh")
                throw new Exception("ForgeItemBuilder failed whitespace name/mesh fallback.");

            // 13. ForgeTroopBuilder whitespace guards
            bool caughtBlankItem = false;
            try { ForgeTroopBuilder.Create("t1").AddBattleEquipment("Item0", "  "); }
            catch (ArgumentException) { caughtBlankItem = true; }
            if (!caughtBlankItem) throw new Exception("ForgeTroopBuilder allowed whitespace item ID.");

            // 14. ForgeHudProjector projection & decay boundaries
            var proj = ForgeHudProjector.ProjectWorldToScreen(0, 10, 0, 0, 0, 0, 0, 0, 0f, 0, 0);
            if (float.IsNaN(proj.screenX) || float.IsInfinity(proj.screenX))
                throw new Exception("ProjectWorldToScreen produced NaN or Infinity on edge FOV/screen dimensions.");
            float decay = ForgeHudProjector.CalculateTrackDecay(-1f, -10, -5f);
            if (decay < 0f || float.IsNaN(decay))
                throw new Exception("CalculateTrackDecay produced negative or NaN decay.");

            // 15. ForgeDiplomacy overflow safety & negative vote handling
            int hugeTribute = ForgeDiplomacy.CalculatePeaceTribute(int.MaxValue, 0, 10000, 0);
            if (hugeTribute != 15000)
                throw new Exception("CalculatePeaceTribute did not clamp huge advantage to max tribute (+15000): got " + hugeTribute);

            var votes = new Dictionary<string, int> { { "optA", -100 }, { "optB", 15 } };
            var (winner, winningVotes, share) = ForgeDiplomacy.SimulateElectionOutcome(100, votes);
            if (winner != "optB" || winningVotes != 15 || share != 1.0f)
                throw new Exception("SimulateElectionOutcome failed with negative vote options: winner=" + winner + ", votes=" + winningVotes + ", share=" + share);

            // 16. Localization cached keys & TryGetKeyForText
            var keys1 = Localization.EnglishKeys;
            var keys2 = Localization.EnglishKeys;
            if (!object.ReferenceEquals(keys1, keys2))
                throw new Exception("Localization.EnglishKeys should return the cached static array.");
            if (!Localization.TryGetKeyForText("Módulos", out string esKey) && !Localization.TryGetKeyForText("Modules", out string enKey))
                throw new Exception("Localization.TryGetKeyForText failed to find key.");
        }

        private static void TestWave3ModHardeningAndOptimizations()
        {
            // 1. ForgeTroopBuilder preventing cyclic self-upgrades
            bool caughtSelfUpgrade = false;
            try { ForgeTroopBuilder.Create("empire_recruit").AddUpgradeTarget("empire_recruit"); }
            catch (InvalidOperationException) { caughtSelfUpgrade = true; }
            if (!caughtSelfUpgrade) throw new Exception("ForgeTroopBuilder allowed cyclic self-upgrade.");

            // 2. ForgeMissionLogicBuilder anti-shadowing validation
            bool caughtShadowClass = false;
            try { ForgeMissionLogicBuilder.Create("Campaign"); }
            catch (InvalidOperationException) { caughtShadowClass = true; }
            if (!caughtShadowClass) throw new Exception("ForgeMissionLogicBuilder allowed class named Campaign.");

            bool caughtShadowNs = false;
            try { ForgeMissionLogicBuilder.Create("MyMission").WithNamespace("MyMod.Campaign.Missions"); }
            catch (InvalidOperationException) { caughtShadowNs = true; }
            if (!caughtShadowNs) throw new Exception("ForgeMissionLogicBuilder allowed namespace segment named Campaign.");

            // 3. CampaignVariableInspector null safety
            CampaignVariableInspector.UntrackVariable(null);
            CampaignVariableInspector.UntrackVariable("");
            bool setRes = CampaignVariableInspector.SetVariable(null, "foo");
            if (setRes) throw new Exception("CampaignVariableInspector.SetVariable should return false for null key.");

            // 4. AnalysisContracts.ForgeAnalysisResult.HasErrors zero-allocation check
            var resNoFindings = new ForgeAnalysisResult();
            if (resNoFindings.HasErrors) throw new Exception("HasErrors should be false for empty findings.");
            resNoFindings.Findings.Add(new ForgeDiagnosticFinding { Severity = "Warning" });
            if (resNoFindings.HasErrors) throw new Exception("HasErrors should be false for Warning finding.");
            resNoFindings.Findings.Add(new ForgeDiagnosticFinding { Severity = "Error" });
            if (!resNoFindings.HasErrors) throw new Exception("HasErrors should be true when Error finding is present.");

            // 5. ForgeCasusBelliEngine tribute boundary and ceiling
            var cbAnalysis = ForgeCasusBelliEngine.EvaluateWarJustification("Vlandia", "Battania", 1000000, 100, 5, false, 2, -200);
            if (cbAnalysis.EstimatedTributeDaily > 25000 || cbAnalysis.EstimatedTributeDaily < -25000)
                throw new Exception("ForgeCasusBelliEngine tribute exceeded ceiling 25000: got " + cbAnalysis.EstimatedTributeDaily);

            // 6. ForgeProgressionSystem negative clamping
            float lr = ForgeProgressionSystem.CalculateLearningRate(-5, -2, -10);
            if (float.IsNaN(lr) || lr < 0f) throw new Exception("CalculateLearningRate produced invalid rate for negative parameters.");
            float ds = ForgeProgressionSystem.EvaluateDynasticSuccession(25, -10, -5, -200);
            if (float.IsNaN(ds) || ds < 0f) throw new Exception("EvaluateDynasticSuccession produced negative or NaN score.");

            // 7. ForgeUnderworldSystem negative clamping
            var (gold, crime) = ForgeUnderworldSystem.CalculateAlleyDailyYield(-5, -100, -20f);
            if (gold != 0 || crime != 0f) throw new Exception("CalculateAlleyDailyYield failed negative thugCount guard.");
            var (margin, risk) = ForgeUnderworldSystem.CalculateSmugglingMargin(-10, -20, -0.5f, -5f);
            if (float.IsNaN(risk) || risk < 0f) throw new Exception("CalculateSmugglingMargin produced invalid risk factor.");
            float decayCrime = ForgeUnderworldSystem.CalculateCrimeDecay(-10f, -50, true);
            if (decayCrime != 0f) throw new Exception("CalculateCrimeDecay did not clamp negative crime rating.");

            // 8. ForgeSiegeTactician navmesh capacity and analysis
            var siege = ForgeSiegeTactician.AnalyzeAssaultTactics("Sargot", -100, -50, true, 2, 2);
            if (siege.NavmeshRequirements.Count < 5 || siege.BreakthroughProbability <= 0f)
                throw new Exception("AnalyzeAssaultTactics failed boundary analysis.");

            // 9. ForgePartySpawner.EstimateRosterWage robust handling
            var roster = new Dictionary<string, int> { { "t1", 5 }, { "t2", -3 }, { "t3", 10 } };
            int wage = ForgePartySpawner.EstimateRosterWage(roster, id => id == "t1" ? 1 : throw new Exception("Lookup failure"));
            if (wage <= 0) throw new Exception("EstimateRosterWage failed with throwing lookup.");

            // 10. ForgeSettlementSystem loyalty and security clamping
            float deltaLoyalty = ForgeSettlementSystem.CalculateDailyLoyaltyDelta(true, false, -10, -5, -3);
            if (float.IsNaN(deltaLoyalty)) throw new Exception("CalculateDailyLoyaltyDelta produced NaN.");
            float deltaSec = ForgeSettlementSystem.CalculateDailySecurityDelta(-10, -20, -5, true);
            if (float.IsNaN(deltaSec)) throw new Exception("CalculateDailySecurityDelta produced NaN.");

            // 11. ForgeHintBuilder multi-line text and double newline
            var hint = ForgeHintBuilder.Create("ATTACK")
                .WithDescription("Initiate frontal assault.")
                .WithMeta("Casualties", "Low")
                .WithRequirement("Morale > 50", true)
                .WithWarning("High risk in open terrain")
                .WithShortcut("Space")
                .BuildHintText();
            if (!hint.Contains("ATTACK") || !hint.Contains("[✓]") || !hint.Contains("[!] Warning"))
                throw new Exception("ForgeHintBuilder produced unexpected text.");

            // 12. ModRuleAuditor GEMINI_CAMPAIGN_SHADOWING
            string tempAuditDir = Path.Combine(Path.GetTempPath(), "CF_Audit_Test_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempAuditDir);
                File.WriteAllText(Path.Combine(tempAuditDir, "ValidBehavior.cs"), "namespace MyMod.CampaignBehaviors { public class MyBehavior {} }");
                File.WriteAllText(Path.Combine(tempAuditDir, "ShadowClass.cs"), "namespace MyMod { public class Campaign {} }");

                var auditResult = ModRuleAuditor.Audit(tempAuditDir);
                if (auditResult.Findings.Any(f => f.FilePath.Contains("ValidBehavior") && f.RuleId == "GEMINI_CAMPAIGN_SHADOWING"))
                    throw new Exception("ModRuleAuditor falsely flagged valid CampaignBehaviors namespace.");

                if (!auditResult.Findings.Any(f => f.FilePath.Contains("ShadowClass") && f.RuleId == "GEMINI_CAMPAIGN_SHADOWING"))
                    throw new Exception("ModRuleAuditor failed to detect shadowed Campaign class.");

                // 13. ModRuleAuditor ForgeWeave & Desktop rules
                File.WriteAllText(Path.Combine(tempAuditDir, "UnthrottledPulse.cs"), "public class BadPulse { public void Init() { var s = new ForgeEventSubscription { Event = ForgeEventKind.Pulse }; } }");
                File.WriteAllText(Path.Combine(tempAuditDir, "UnsafeWriter.cs"), "public class BadWriter { public void Init() { var s = new ForgeEventSubscription { Access = ForgeEventAccess.CampaignWrite }; } }");
                File.WriteAllText(Path.Combine(tempAuditDir, "PathShadow.cs"), "public class BadPath { public string Path { get; set; } public string GetP() => Path.Combine(\"a\", \"b\"); }");
                File.WriteAllText(Path.Combine(tempAuditDir, "BadConverter.cs"), "public class BadConv : System.Windows.Data.IMultiValueConverter { public object ConvertBack(object v, System.Type[] t, object p, System.Globalization.CultureInfo c) => null; }");

                var extendedResult = ModRuleAuditor.Audit(tempAuditDir);
                if (!extendedResult.Findings.Any(f => f.FilePath.Contains("UnthrottledPulse") && f.RuleId == "FORGEWEAVE_UNTHROTTLED_PULSE"))
                    throw new Exception("ModRuleAuditor failed to detect FORGEWEAVE_UNTHROTTLED_PULSE.");

                if (!extendedResult.Findings.Any(f => f.FilePath.Contains("UnsafeWriter") && f.RuleId == "FORGEWEAVE_UNSAFE_WRITER"))
                    throw new Exception("ModRuleAuditor failed to detect FORGEWEAVE_UNSAFE_WRITER.");

                if (!extendedResult.Findings.Any(f => f.FilePath.Contains("PathShadow") && f.RuleId == "DESKTOP_PATH_SHADOWING"))
                    throw new Exception("ModRuleAuditor failed to detect DESKTOP_PATH_SHADOWING.");

                if (!extendedResult.Findings.Any(f => f.FilePath.Contains("BadConverter") && f.RuleId == "WPF_CONVERTER_SIGNATURE"))
                    throw new Exception("ModRuleAuditor failed to detect WPF_CONVERTER_SIGNATURE.");
            }
            finally
            {
                if (Directory.Exists(tempAuditDir))
                {
                    try { Directory.Delete(tempAuditDir, true); } catch { }
                }
            }
        }

        private static void TestForgeCommandsAndSimulations()
        {
            // 1. Help command documents all commands
            string help = ForgeCommands.Help(null);
            if (!help.Contains("cf.casusbelli.eval") ||
                !help.Contains("cf.siege.tactics") ||
                !help.Contains("cf.trade.workshop") ||
                !help.Contains("cf.underworld.smuggling") ||
                !help.Contains("cf.character.succession_score") ||
                !help.Contains("cf.character.perk_role") ||
                !help.Contains("cf.settlement.security_delta") ||
                !help.Contains("cf.settlement.loyalty_delta"))
            {
                throw new Exception("ForgeCommands.Help is missing documentation for new SDK commands.");
            }

            // 2. Casus Belli command
            string cbUsage = ForgeCommands.EvalCasusBelli(new List<string>());
            if (!cbUsage.StartsWith("Usage:")) throw new Exception("EvalCasusBelli failed to return usage for empty args.");
            string cbResult = ForgeCommands.EvalCasusBelli(new List<string> { "Vlandia", "Battania", "6000", "3000", "4", "false", "2", "-20" });
            if (!cbResult.Contains("Vlandia") || !cbResult.Contains("Battania") || !cbResult.Contains("Justification Score"))
                throw new Exception("EvalCasusBelli returned invalid output: " + cbResult);

            // 3. Siege Tactics command
            string siegeUsage = ForgeCommands.SiegeTactics(new List<string>());
            if (!siegeUsage.StartsWith("Usage:")) throw new Exception("SiegeTactics failed to return usage for empty args.");
            string siegeResult = ForgeCommands.SiegeTactics(new List<string> { "Chaikand", "600", "200", "true", "2", "1" });
            if (!siegeResult.Contains("Chaikand") || !siegeResult.Contains("Breakthrough") || !siegeResult.Contains("Navmeshes"))
                throw new Exception("SiegeTactics returned invalid output: " + siegeResult);

            // 4. Trade Workshop command
            string wsUsage = ForgeCommands.CalcWorkshop(new List<string>());
            if (!wsUsage.StartsWith("Usage:")) throw new Exception("CalcWorkshop failed to return usage for empty args.");
            string wsResult = ForgeCommands.CalcWorkshop(new List<string> { "10", "45", "6", "75" });
            if (!wsResult.Contains("Daily Net:") || !wsResult.Contains("135"))
                throw new Exception("CalcWorkshop returned invalid output: " + wsResult);

            // 5. Underworld Smuggling command
            string smuggleUsage = ForgeCommands.CalcSmuggling(new List<string>());
            if (!smuggleUsage.StartsWith("Usage:")) throw new Exception("CalcSmuggling failed to return usage for empty args.");
            string smuggleResult = ForgeCommands.CalcSmuggling(new List<string> { "40", "110", "0.15", "15" });
            if (!smuggleResult.Contains("Net Profit:") || !smuggleResult.Contains("Risk Factor:"))
                throw new Exception("CalcSmuggling returned invalid output: " + smuggleResult);

            // 6. Succession Score command
            string succUsage = ForgeCommands.CalcSuccessionScore(new List<string>());
            if (!succUsage.StartsWith("Usage:")) throw new Exception("CalcSuccessionScore failed to return usage for empty args.");
            string succResult = ForgeCommands.CalcSuccessionScore(new List<string> { "28", "90", "140", "40" });
            if (!succResult.Contains("Fitness Score:") || !succResult.Contains("pts"))
                throw new Exception("CalcSuccessionScore returned invalid output: " + succResult);

            // 7. Perk Role command
            string perkUsage = ForgeCommands.CheckPerkRole(new List<string>());
            if (!perkUsage.StartsWith("Usage:")) throw new Exception("CheckPerkRole failed to return usage for empty args.");
            string perkValid = ForgeCommands.CheckPerkRole(new List<string> { "steward_bannerlords", "Quartermaster" });
            if (!perkValid.Contains("Applies: True"))
                throw new Exception("CheckPerkRole failed for valid role: " + perkValid);
            string perkInvalid = ForgeCommands.CheckPerkRole(new List<string> { "steward_bannerlords", "Surgeon" });
            if (!perkInvalid.Contains("Applies: False"))
                throw new Exception("CheckPerkRole failed for invalid role: " + perkInvalid);

            // 8. Settlement Deltas
            string secUsage = ForgeCommands.CalcSecurityDelta(new List<string>());
            if (!secUsage.StartsWith("Usage:")) throw new Exception("CalcSecurityDelta failed to return usage for empty args.");
            string secResult = ForgeCommands.CalcSecurityDelta(new List<string> { "150", "200", "2", "false" });
            if (!secResult.Contains("Security Delta:") || !secResult.Contains("/day"))
                throw new Exception("CalcSecurityDelta returned invalid output: " + secResult);

            string loyUsage = ForgeCommands.CalcLoyaltyDelta(new List<string>());
            if (!loyUsage.StartsWith("Usage:")) throw new Exception("CalcLoyaltyDelta failed to return usage for empty args.");
            string loyResult = ForgeCommands.CalcLoyaltyDelta(new List<string> { "true", "false", "-5", "10", "2" });
            if (!loyResult.Contains("Loyalty Delta:") || !loyResult.Contains("/day"))
                throw new Exception("CalcLoyaltyDelta returned invalid output: " + loyResult);

            // 9. ForgeApi CasusBelli and Siege static contracts
            var apiCb = ForgeApi.CasusBelli.EvaluateWarJustification("Sturgia", "Khuzait", 4000, 4500, 2, false, 1, -10);
            if (string.IsNullOrEmpty(apiCb.AttackerKingdom) || apiCb.CouncilSupportPercentage <= 0)
                throw new Exception("ForgeApi.CasusBelli failed to evaluate war justification.");

            var apiSiege = ForgeApi.Siege.AnalyzeAssaultTactics("Pravend", 700, 300, true, 2, 1);
            if (apiSiege.NavmeshRequirements.Count == 0 || apiSiege.AttackerExpectedCasualtyRate <= 0f)
                throw new Exception("ForgeApi.Siege failed to analyze assault tactics.");

            float apiSucc = ForgeApi.Progression.EvaluateDynasticSuccession(32, 85, 110, 50);
            if (apiSucc <= 0f) throw new Exception("ForgeApi.Progression.EvaluateDynasticSuccession returned non-positive score.");
        }

        private static void TestForgeEncyclopediaExtender()
        {
            CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.Clear();

            var entry1 = new CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.EncyclopediaEntry(
                "hero_derthert", "King Derthert", "Hero", "King of the Vlandians", "CalradiaForge.Mod");
            entry1.Tags.Add("King");
            entry1.Tags.Add("Vlandia");
            entry1.Attributes["Culture"] = "Vlandia";

            var entry2 = new CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.EncyclopediaEntry(
                "settlement_sargot", "Sargot", "Settlement", "Ancient capital of the western realm", "CalradiaForge.Mod");
            entry2.Tags.Add("Town");
            entry2.Tags.Add("Vlandia");

            var entry3 = new CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.EncyclopediaEntry(
                "concept_feudalism", "Feudal Oath", "Concept", "Feudal obligations and oath of fealty", "CalradiaForge.Core");
            entry3.Tags.Add("Lore");

            if (!CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.RegisterEntry(entry1))
                throw new Exception("RegisterEntry failed for entry1");
            if (!CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.RegisterEntry(entry2))
                throw new Exception("RegisterEntry failed for entry2");
            if (!CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.RegisterEntry(entry3))
                throw new Exception("RegisterEntry failed for entry3");

            // Retrieval
            var fetched = CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.GetEntry("hero_derthert");
            if (fetched == null || fetched.Name != "King Derthert")
                throw new Exception("GetEntry returned unexpected result.");

            // Category filtering
            var heroes = CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.GetAllEntries("Hero");
            if (heroes.Count != 1 || heroes[0].Id != "hero_derthert")
                throw new Exception("GetAllEntries with category filter failed.");

            // Search
            var searchResults = CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.Search("Sargot");
            if (searchResults.Count != 1 || searchResults[0].Id != "settlement_sargot")
                throw new Exception("Search by name failed.");

            var searchTagResults = CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.Search("Vlandia");
            if (searchTagResults.Count != 2)
                throw new Exception("Search by tag failed.");

            // Bookmarks
            if (!CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.AddBookmark("hero_derthert"))
                throw new Exception("AddBookmark failed.");
            if (!CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.IsBookmarked("hero_derthert"))
                throw new Exception("IsBookmarked returned false for added bookmark.");
            if (CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.IsBookmarked("settlement_sargot"))
                throw new Exception("IsBookmarked returned true for non-bookmarked entry.");

            var bookmarks = CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.GetBookmarks();
            if (bookmarks.Count != 1 || bookmarks[0] != "hero_derthert")
                throw new Exception("GetBookmarks returned invalid collection.");

            if (!CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.RemoveBookmark("hero_derthert"))
                throw new Exception("RemoveBookmark failed.");
            if (CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.IsBookmarked("hero_derthert"))
                throw new Exception("Bookmark still active after removal.");

            // Custom Filters
            CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.RegisterFilter("Hero", "KingsOnly", e => e.Tags.Contains("King"));
            var filteredKings = CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.Filter("Hero", "KingsOnly");
            if (filteredKings.Count != 1 || filteredKings[0].Id != "hero_derthert")
                throw new Exception("Registered filter KingsOnly failed.");

            // Unregister
            if (!CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.UnregisterEntry("concept_feudalism"))
                throw new Exception("UnregisterEntry failed.");
            if (CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.GetEntry("concept_feudalism") != null)
                throw new Exception("Entry still found after unregistration.");

            CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.Clear();
            if (CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender.GetAllEntries().Count != 0)
                throw new Exception("Clear failed to wipe entries.");
        }
    }
}
