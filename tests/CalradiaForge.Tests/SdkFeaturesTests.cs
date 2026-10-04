using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection.Emit;
using CalradiaForge.Sdk;
using CalradiaForge.Core;
using CalradiaForge.Examples;
using CalradiaForge.Mod.Commands;
using CalradiaForge.Sdk.Patcher;
using CalradiaForge.TestFixtures;

namespace CalradiaForge.Tests
{
    public static class SdkFeaturesTests
    {
        public static void Run(Action<string, Action> test)
        {
            test("ModSettings serialization and caching", TestModSettings);
            test("ModSettings reports safe-save outcomes and preserves committed data on failure", TestModSettingsSafeSave);
            test("Atomic replacement retries only unchanged Win32 1175 outcomes within a bounded budget", TestAtomicReplacementRecovery);
            test("CampaignVariableInspector tracking and snapshots", TestCampaignVariableInspector);
            test("Legacy patch hook API only emits its documented request notification", TestForgeLivePatcher);
            test("ForgeDetour rejects a page-crossing write before changing memory protection", TestForgeDetourPageBoundary);
            test("ForgeDetour public Patch clears its reservation after a page-boundary rejection", TestForgeDetourPatchPageBoundaryCleanup);
            test("ForgePatcher batch clears IDs and targets after a page-boundary rejection", TestPatchBatchPageBoundaryCleanup);
            test("ForgeDetour verifies bytes and refuses foreign-byte rollback with a simulated memory adapter", TestForgeDetourFakeMemory);
            test("ForgeDetour rejects non-x64 architectures before executable memory access", TestForgeDetourRejectsNonX64Architecture);
            test("ForgeDetour rejects unsafe implicit receivers, native entry points, varargs, and custom modifiers before memory access", TestForgeDetourSignatureValidation);
            test("ForgeDetour and patch batches honor shared RuntimeDetour target reservations", TestHookTargetReservationBlocksRawDetours);
            test("Optional Forge patch service reverts on disconnect and rejects stale apply", TestPatchServiceLifecycle);
            test("Forge patch service disconnect preserves unrelated direct detours", TestPatchServiceScope);
            test("Optional Forge patch service retains foreign bytes on disconnect", TestPatchServiceConflictRetention);
            test("ForgeApi connection replacement retires prior patches before publishing the next host", TestPatchServiceConnectionReplacement);
            test("ForgeApi connection replacement preserves conflicted host for manual resolution", TestPatchServiceConnectionReplacementConflict);
            test("ForgeApi disconnect reverts direct and legacy detours through the shared registry", TestPatchServiceLegacyCleanup);
            test("Legacy patch and MethodSwapper receipts cannot revert a later installation generation", TestStaleLegacyReceiptCannotRevertNewGeneration);
            test("ForgePatcher ApplyAll rolls back the complete batch when a later write fails", TestPatchBatchRollback);
            test("Patch console routes use shared IDs, owners, and reverse-all inventory", TestPatchConsoleRoutes);
            
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
            test("ForgeApi Version is 13", TestForgeApiVersion);
            test("Forge UI registry rejects duplicate IDs and removes an unloaded owner", TestForgeUiRegistry);
            test("Forge UI discovery validates Gauntlet ViewModel and command binding", TestForgeUiDiscovery);
            test("Forge UI policy enforces context and writer gates", TestForgeUiPolicy);
            test("ModRuleAuditor fails closed on invalid XML, unsafe Pulse intervals, reparse points, and scan limits", TestModRuleAuditorSafety);
            test("ForgeAgentMemory handles null agent and key parameters gracefully", TestForgeAgentMemoryNullSafety);
            test("ForgeTimeSlicer bucket boundaries and processing selection", TestForgeTimeSlicerBoundaries);
            test("Forge SDK hardening, optimization, and edge case safety", TestSdkHardeningAndOptimizations);
            test("Wave 3 Mod and SDK hardening, optimization, and anti-shadowing", TestWave3ModHardeningAndOptimizations);
            test("ForgeCommands and SDK simulation integration", TestForgeCommandsAndSimulations);
            test("ForgeEncyclopediaExtender in-game codex registry and bookmarks", TestForgeEncyclopediaExtender);
            test("Rev137 Multilayer bug fixes, lifecycle resilience, and boundary safety", TestRev137MultilayerHardeningAndSafety);
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

        private static void TestAtomicReplacementRecovery()
        {
            var directory=Path.Combine(Path.GetTempPath(),"cf_replace_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var source=Path.Combine(directory,"source.tmp");
            var destination=Path.Combine(directory,"destination.json");
            try
            {
                File.WriteAllText(source,"new"); File.WriteAllText(destination,"old");
                int attempts=0; var delays=new List<int>();
                AtomicFileReplacement.Replace(source,destination,(from,to)=>{
                    if(++attempts<=2) throw new IOException("fixture transient refusal",unchecked((int)0x80070497));
                    File.Replace(from,to,null);
                },delays.Add);
                if(attempts!=3 || !delays.SequenceEqual(new[]{20,40}) || File.ReadAllText(destination)!="new" || File.Exists(source))
                    throw new Exception("Transient replacement did not preserve the atomic commit and bounded delays.");

                File.WriteAllText(source,"next"); attempts=0; delays.Clear();
                var persistent=new IOException("fixture persistent refusal",unchecked((int)0x80070497));
                IOException caught=null;
                try { AtomicFileReplacement.Replace(source,destination,(_,__)=>{attempts++;throw persistent;},delays.Add); }
                catch(IOException error) { caught=error; }
                if(!ReferenceEquals(caught,persistent)) throw new Exception("Persistent replacement failure was not propagated unchanged.");
                if(attempts!=4 || !delays.SequenceEqual(new[]{20,40,60}) || File.ReadAllText(source)!="next" || File.ReadAllText(destination)!="new")
                    throw new Exception("Persistent refusal did not retain both files within its retry budget.");

                foreach(var code in new[]{32,5,1176,1177})
                {
                    attempts=0; delays.Clear();
                    var failure=new IOException("fixture non-retryable error",unchecked((int)0x80070000)|code);
                    caught=null;
                    try { AtomicFileReplacement.Replace(source,destination,(_,__)=>{attempts++;throw failure;},delays.Add); }
                    catch(IOException error) { caught=error; }
                    if(!ReferenceEquals(caught,failure)) throw new Exception("Non-retryable replacement failure was not propagated unchanged.");
                    if(attempts!=1 || delays.Count!=0 || File.ReadAllText(destination)!="new")
                        throw new Exception("A non-retryable replacement error was retried or changed the destination.");
                }
                File.Delete(source); attempts=0; delays.Clear();
                caught=null;
                try { AtomicFileReplacement.Replace(source,destination,(_,__)=>{attempts++;throw persistent;},delays.Add); }
                catch(IOException error) { caught=error; }
                if(!ReferenceEquals(caught,persistent)) throw new Exception("Missing-source replacement failure was not propagated unchanged.");
                if(attempts!=1 || delays.Count!=0) throw new Exception("Missing staged output was retried.");
            }
            finally { Directory.Delete(directory,true); }
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
                        throw new Exception("Concurrent case aliases did not serialize their atomic file commits: save " + index +
                            " returned " + aliasSaves[index].Result.State + " (" + aliasSaves[index].Result.FailureReason +
                            "; HRESULT=" + aliasSaves[index].Result.Error?.HResult.ToString("X8") + ").",
                            aliasSaves[index].Result.Error);
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
            MethodInfo original = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo replacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic);
            bool notified = false;
            Action<MethodInfo, MethodInfo, MethodInfo> handler = (target, prefix, postfix) =>
                notified = target == original && prefix == replacement && postfix == null;
            ForgeLivePatcher.OnPatchRequested += handler;
            try { ForgeLivePatcher.ApplyPatch(original, replacement, null); }
            finally { ForgeLivePatcher.OnPatchRequested -= handler; }
            if (!notified) throw new Exception("Legacy hook API should notify subscribers without applying a patch itself.");
            if (ForgeDetour.IsPatched(original)) throw new Exception("Notification-only API must not write target memory.");
        }

        private static void TestForgeDetourFakeMemory()
        {
            var original = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var replacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic);
            EnsureNoTrackedDetours("TestForgeDetourFakeMemory");
            IntPtr[] preparedAddresses = PrepareStableMethodAddresses("TestForgeDetourFakeMemory", original, replacement);
            IntPtr address = preparedAddresses[0];
            if (address == preparedAddresses[1])
                throw new InvalidOperationException("TestForgeDetourFakeMemory: target and replacement resolved to the same prepared address " + Address(address) + ".");
            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            byte[] originalBytes = null;
            Exception testFailure = null;
            try
            {
                ForgeApi.Connect(new TestEngine());
                originalBytes = adapter.Read(address, 13);
                adapter.FailNextProtect = true;
                bool protectionFailed = false;
                try { ForgeDetour.Patch(original, replacement); }
                catch (InvalidOperationException) { protectionFailed = true; }
                if (!protectionFailed || !adapter.Read(address, 13).SequenceEqual(originalBytes))
                    throw new Exception("Protection failure mismatch: failed=" + protectionFailed + ", target=" + Address(address) +
                        ", bytesRestored=" + adapter.Read(address, 13).SequenceEqual(originalBytes) + ", tracked=" + ForgeDetour.IsTracked(original) + ".");

                adapter.FailNextFlush = true;
                bool flushFailed = false;
                try { ForgeDetour.Patch(original, replacement); }
                catch (Exception) { flushFailed = true; }
                if (!flushFailed || ForgeDetour.IsPatched(original) || !adapter.Read(address, 13).SequenceEqual(originalBytes))
                    throw new Exception("Flush rollback mismatch: failed=" + flushFailed + ", target=" + Address(address) +
                        ", failureInjected=" + adapter.FlushFailureTriggered + ", bytesRestored=" + adapter.Read(address, 13).SequenceEqual(originalBytes) +
                        ", tracked=" + ForgeDetour.IsTracked(original) + ", flushCalls=" + adapter.FlushCalls + ".");
                if (adapter.Protection != 0x20)
                    throw new Exception("Flush failure rollback left simulated protection 0x" + adapter.Protection.ToString("X") +
                        " instead of 0x20 at " + Address(address) + ".");

                adapter.FailNextProtectionRestore = true;
                bool protectionRestoreFailed = false;
                try { ForgeDetour.Patch(original, replacement); }
                catch (InvalidOperationException) { protectionRestoreFailed = true; }
                if (!protectionRestoreFailed || ForgeDetour.IsPatched(original) || !adapter.Read(address, 13).SequenceEqual(originalBytes) || adapter.Protection != 0x20)
                    throw new Exception("Protection-restore rollback mismatch: failed=" + protectionRestoreFailed + ", target=" + Address(address) +
                        ", bytesRestored=" + adapter.Read(address, 13).SequenceEqual(originalBytes) + ", protection=0x" + adapter.Protection.ToString("X") +
                        ", tracked=" + ForgeDetour.IsTracked(original) + ".");

                // A failed rollback can leave the exact jump installed while protection or
                // cache state is uncertain. Verify must never promote that retained record.
                adapter.FailNextFlush = true;
                adapter.FailWriteOnCall = adapter.WriteCalls + 2;
                bool uncertainWriteFailed = false;
                try { ForgeDetour.Patch(original, replacement); }
                catch (AggregateException) { uncertainWriteFailed = true; }
                string uncertainStatus;
                bool uncertainVerified = ForgeDetour.Verify(original, out uncertainStatus);
                if (!uncertainWriteFailed || uncertainVerified || uncertainStatus != "write-state-uncertain" || !ForgeDetour.IsTracked(original))
                    throw new Exception("Uncertain write was promoted: failed=" + uncertainWriteFailed + ", verified=" + uncertainVerified +
                        ", status=" + uncertainStatus + ", tracked=" + ForgeDetour.IsTracked(original) + ".");
                adapter.FailWriteOnCall = 0;
                if (!ForgeDetour.Unpatch(original) || ForgeDetour.IsTracked(original))
                    throw new Exception("The isolated fixture could not clean its deliberately uncertain installed image.");

                ForgeDetour.Patch(original, replacement);
                byte[] installedBytes = adapter.Read(address, 13);
                if (!ForgeDetour.IsPatched(original)) throw new Exception("Simulated exact jump should be reported intact.");
                adapter.Tamper(address, 0, 0x90);
                byte[] foreignBytes = adapter.Read(address, 13);
                bool foreignReverted=ForgeDetour.Unpatch(original);
                byte[] afterConflict=adapter.Read(address,13);
                if (foreignReverted || !afterConflict.SequenceEqual(foreignBytes))
                    throw new Exception("Foreign-byte check mismatch at " + Address(address) + ": reverted=" + foreignReverted +
                        ", foreignBytesRetained=" + afterConflict.SequenceEqual(foreignBytes) + ", tracked=" + ForgeDetour.IsTracked(original) +
                        ", firstByteBefore=0x" + foreignBytes[0].ToString("X2") + ", firstByteAfter=0x" + afterConflict[0].ToString("X2") + ".");
                adapter.Replace(address, installedBytes);
                bool restored=ForgeDetour.Unpatch(original);
                bool repeated=ForgeDetour.Unpatch(original);
                if (!restored || repeated)
                    throw new Exception("Restoration check mismatch at " + Address(address) + ": first=" + restored +
                        ", repeated=" + repeated + ", tracked=" + ForgeDetour.IsTracked(original) + ".");
                if (!adapter.Read(address, 13).SequenceEqual(originalBytes)) throw new Exception("Simulated original bytes were not restored.");
            }
            catch (Exception error)
            {
                testFailure = error;
                throw;
            }
            finally
            {
                var cleanupFailures = new List<string>();
                string cleanup = CleanupFakeTarget(adapter, original, address, originalBytes);
                if (cleanup != null) cleanupFailures.Add(cleanup);
                try { if (ForgeApi.Registry != null) ForgeApi.Disconnect(); }
                catch (Exception error) { cleanupFailures.Add("SDK disconnect failed after fake detour cleanup: " + error.Message); }
                ResetFakeMemoryAdapter(cleanupFailures);
                ThrowIfFakeCleanupFailed("TestForgeDetourFakeMemory", testFailure, cleanupFailures);
            }
        }

        private static void TestForgeDetourRejectsNonX64Architecture()
        {
            EnsureNoTrackedDetours("TestForgeDetourRejectsNonX64Architecture");
            var original = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var replacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var adapter = new FakeExecutableMemoryAdapter { ProcessArchitecture = Architecture.Arm64 };
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            Exception testFailure = null;
            try
            {
                ForgeApi.Connect(new TestEngine());
                bool directRejected = false;
                try { ForgeDetour.Patch(original, replacement); }
                catch (PlatformNotSupportedException error)
                {
                    directRejected = error.Message.IndexOf("x64", StringComparison.OrdinalIgnoreCase) >= 0;
                }

                bool batchRejected = false;
                try
                {
                    ForgeDetour.PatchBatch(new[] { original }, new[] { replacement }, new[] { "fixture.arm64.batch" },
                        "fixture.arm64", new byte[1][]);
                }
                catch (PlatformNotSupportedException error)
                {
                    batchRejected = error.Message.IndexOf("x64", StringComparison.OrdinalIgnoreCase) >= 0;
                }

                if (!directRejected || !batchRejected || adapter.ReadCalls != 0 || adapter.WriteCalls != 0 ||
                    adapter.ProtectCalls != 0 || adapter.FlushCalls != 0 || ForgeDetour.GetTrackedSnapshots().Count != 0)
                    throw new Exception("ARM64 must be rejected before any executable-memory read/write/protection/flush or receipt publication; " +
                        "directRejected=" + directRejected + ", batchRejected=" + batchRejected + ", reads=" + adapter.ReadCalls +
                        ", writes=" + adapter.WriteCalls + ", protects=" + adapter.ProtectCalls + ", flushes=" + adapter.FlushCalls +
                        ", tracked=" + ForgeDetour.GetTrackedSnapshots().Count + ".");
            }
            catch (Exception error)
            {
                testFailure = error;
                throw;
            }
            finally
            {
                var cleanupFailures = new List<string>();
                try { if (ForgeApi.Registry != null) ForgeApi.Disconnect(); }
                catch (Exception error) { cleanupFailures.Add("SDK disconnect failed after ARM64 guard fixture: " + error.Message); }
                ResetFakeMemoryAdapter(cleanupFailures);
                ThrowIfFakeCleanupFailed("TestForgeDetourRejectsNonX64Architecture", testFailure, cleanupFailures);
            }
        }

        private static void TestForgeDetourSignatureValidation()
        {
            EnsureNoTrackedDetours("TestForgeDetourSignatureValidation");
            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            Exception testFailure = null;
            try
            {
                MethodInfo receiverTarget = typeof(ReceiverBaseFixture).GetMethod(nameof(ReceiverBaseFixture.Target),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                MethodInfo narrowerReceiver = typeof(ReceiverDerivedFixture).GetMethod(nameof(ReceiverDerivedFixture.Replacement),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                AssertDetourPairRejected(receiverTarget, narrowerReceiver, typeof(ArgumentException),
                    "derived-only replacement receiver", adapter);

                MethodInfo pinvoke = typeof(SdkFeaturesTests).GetMethod(nameof(PInvokeSignatureFixture), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo managedUInt = typeof(SdkFeaturesTests).GetMethod(nameof(ManagedUIntSignatureFixture), BindingFlags.Static | BindingFlags.NonPublic);
                if (pinvoke == null || (pinvoke.Attributes & MethodAttributes.PinvokeImpl) == 0)
                    throw new Exception("The P/Invoke fixture did not emit the metadata flag required to exercise P/Invoke rejection.");
                AssertDetourPairRejected(pinvoke, managedUInt, typeof(NotSupportedException), "P/Invoke target", adapter);
                AssertDetourPairRejected(managedUInt, pinvoke, typeof(NotSupportedException), "P/Invoke replacement", adapter);

                MethodInfo varArgs = typeof(SdkFeaturesTests).GetMethod(nameof(VarArgsSignatureFixture), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo varArgsReplacement = typeof(SdkFeaturesTests).GetMethod(nameof(VarArgsReplacementSignatureFixture), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo fixedArguments = typeof(SdkFeaturesTests).GetMethod(nameof(FixedArgumentsSignatureFixture), BindingFlags.Static | BindingFlags.NonPublic);
                if (varArgs == null || (varArgs.CallingConvention & CallingConventions.VarArgs) == 0)
                    throw new Exception("The varargs fixture did not expose CallingConventions.VarArgs on this runtime.");
                AssertDetourPairRejected(varArgs, varArgsReplacement, typeof(NotSupportedException), "varargs target", adapter);
                AssertDetourPairRejected(varArgs, fixedArguments, typeof(NotSupportedException), "varargs target with fixed replacement", adapter);
                AssertDetourPairRejected(fixedArguments, varArgsReplacement, typeof(NotSupportedException), "varargs replacement", adapter);

                Type modifierFixture = CreateCustomModifierSignatureFixture();
                AssertDetourPairRejected(
                    GetFixtureMethod(modifierFixture, "RequiredParameterTarget"),
                    GetFixtureMethod(modifierFixture, "RequiredParameterReplacement"),
                    typeof(ArgumentException), "parameter modreq mismatch", adapter);
                AssertDetourPairRejected(
                    GetFixtureMethod(modifierFixture, "OptionalParameterTarget"),
                    GetFixtureMethod(modifierFixture, "OptionalParameterReplacement"),
                    typeof(ArgumentException), "parameter modopt mismatch", adapter);
                AssertDetourPairRejected(
                    GetFixtureMethod(modifierFixture, "RequiredReturnTarget"),
                    GetFixtureMethod(modifierFixture, "RequiredReturnReplacement"),
                    typeof(ArgumentException), "return modreq mismatch", adapter);
                AssertDetourPairRejected(
                    GetFixtureMethod(modifierFixture, "OptionalReturnTarget"),
                    GetFixtureMethod(modifierFixture, "OptionalReturnReplacement"),
                    typeof(ArgumentException), "return modopt mismatch", adapter);

                MethodInfo internalCall = GetFixtureMethod(modifierFixture, "InternalCallMethod");
                if ((internalCall.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) == 0)
                    throw new Exception("The emitted internal-call fixture did not retain MethodImplAttributes.InternalCall.");
                AssertDetourPairRejected(internalCall, GetFixtureMethod(modifierFixture, "InternalCallCompatibleReplacement"),
                    typeof(NotSupportedException), "internal-call target", adapter);
                AssertDetourPairRejected(GetFixtureMethod(modifierFixture, "InternalCallCompatibleReplacement"), internalCall,
                    typeof(NotSupportedException), "internal-call replacement", adapter);

                if (adapter.ReadCalls != 0 || adapter.WriteCalls != 0 || adapter.ProtectCalls != 0 || adapter.FlushCalls != 0 ||
                    ForgeDetour.GetTrackedSnapshots().Count != 0)
                    throw new Exception("Signature validation must reject every unsupported pair before reading or writing executable memory.");
            }
            catch (Exception error)
            {
                testFailure = error;
                throw;
            }
            finally
            {
                var cleanupFailures = new List<string>();
                try { if (ForgeApi.Registry != null) ForgeApi.Disconnect(); }
                catch (Exception error) { cleanupFailures.Add("SDK disconnect failed after signature validation: " + error.Message); }
                ResetFakeMemoryAdapter(cleanupFailures);
                ThrowIfFakeCleanupFailed("TestForgeDetourSignatureValidation", testFailure, cleanupFailures);
            }
        }

        private static void AssertDetourPairRejected(MethodInfo target, MethodInfo replacement, Type expectedException,
            string fixtureName, FakeExecutableMemoryAdapter adapter)
        {
            if (target == null || replacement == null)
                throw new Exception("The " + fixtureName + " signature fixture did not resolve both methods.");

            int reads = adapter.ReadCalls;
            int writes = adapter.WriteCalls;
            int protections = adapter.ProtectCalls;
            int flushes = adapter.FlushCalls;
            Exception observed = null;
            try { ForgeDetour.Patch(target, replacement); }
            catch (Exception error) { observed = error; }

            if (observed == null || !expectedException.IsInstanceOfType(observed))
                throw new Exception("The " + fixtureName + " pair should reject with " + expectedException.Name + "; observed " +
                    (observed == null ? "no exception" : observed.GetType().Name + ": " + observed.Message) + ".");
            if (adapter.ReadCalls != reads || adapter.WriteCalls != writes || adapter.ProtectCalls != protections || adapter.FlushCalls != flushes ||
                ForgeDetour.GetTrackedSnapshots().Count != 0)
                throw new Exception("The " + fixtureName + " pair reached executable-memory access or left a patch receipt before rejection.");
        }

        private static Type CreateCustomModifierSignatureFixture()
        {
            var assemblyName = new AssemblyName("CalradiaForge.DetourSignatureFixture");
            AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule(assemblyName.Name);
            TypeBuilder type = module.DefineType("CalradiaForge.DetourSignatureFixture.Methods",
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig;
            Type[] none = Type.EmptyTypes;
            Type[] noParameterModifier = Type.EmptyTypes;
            Type[][] noParameterModifiers = new[] { noParameterModifier };
            Type[] requiredParameterModifier = new[] { typeof(CustomModifierFixtureTag) };
            Type[] optionalParameterModifier = new[] { typeof(CustomModifierFixtureTag) };
            Type[] requiredReturnModifier = new[] { typeof(CustomModifierFixtureTag) };
            Type[] optionalReturnModifier = new[] { typeof(CustomModifierFixtureTag) };

            DefineIdentityIntMethod(type, "RequiredParameterTarget", attributes, none, none, noParameterModifier, noParameterModifier);
            DefineIdentityIntMethod(type, "RequiredParameterReplacement", attributes, none, none, requiredParameterModifier, noParameterModifier);
            DefineIdentityIntMethod(type, "OptionalParameterTarget", attributes, none, none, noParameterModifier, noParameterModifier);
            DefineIdentityIntMethod(type, "OptionalParameterReplacement", attributes, none, none, noParameterModifier, optionalParameterModifier);
            DefineIdentityIntMethod(type, "RequiredReturnTarget", attributes, none, none, noParameterModifier, noParameterModifier);
            DefineIdentityIntMethod(type, "RequiredReturnReplacement", attributes, requiredReturnModifier, none, noParameterModifier, noParameterModifier);
            DefineIdentityIntMethod(type, "OptionalReturnTarget", attributes, none, none, noParameterModifier, noParameterModifier);
            DefineIdentityIntMethod(type, "OptionalReturnReplacement", attributes, none, optionalReturnModifier, noParameterModifier, noParameterModifier);

            MethodBuilder internalCall = type.DefineMethod("InternalCallMethod", attributes, CallingConventions.Standard, typeof(int),
                Type.EmptyTypes, Type.EmptyTypes, new[] { typeof(int) }, noParameterModifiers, noParameterModifiers);
            internalCall.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.InternalCall);
            DefineIdentityIntMethod(type, "InternalCallCompatibleReplacement", attributes, none, none, noParameterModifier, noParameterModifier);
            return type.CreateType();
        }

        private static void DefineIdentityIntMethod(TypeBuilder type, string name, MethodAttributes attributes,
            Type[] returnRequiredModifiers, Type[] returnOptionalModifiers,
            Type[] parameterRequiredModifiers, Type[] parameterOptionalModifiers)
        {
            MethodBuilder method = type.DefineMethod(name, attributes, CallingConventions.Standard, typeof(int),
                returnRequiredModifiers, returnOptionalModifiers, new[] { typeof(int) },
                new[] { parameterRequiredModifiers }, new[] { parameterOptionalModifiers });
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ret);
        }

        private static MethodInfo GetFixtureMethod(Type fixture, string name)
        {
            MethodInfo method = fixture.GetMethod(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (method == null) throw new Exception("The emitted signature fixture method was not found: " + name + ".");
            return method;
        }

        private static void TestHookTargetReservationBlocksRawDetours()
        {
            var target = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var replacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic);
            EnsureNoTrackedDetours("TestHookTargetReservationBlocksRawDetours");
            PrepareStableMethodAddresses("TestHookTargetReservationBlocksRawDetours", target, replacement);
            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            bool registered = false;
            var engine = new TestEngine();
            try
            {
                // Earlier suites may intentionally leave the SDK disconnected. Reconnect
                // so this test reaches the target-reservation guard instead of the global
                // fail-closed application guard.
                ForgeApi.Connect(engine);
                ForgeDetour.RegisterHookTarget(target);
                registered = true;
                bool directRejected = false;
                bool batchRejected = false;
                try { ForgeDetour.Patch(target, replacement, "fixture.raw-vs-hook.direct", "fixture.raw"); }
                catch (InvalidOperationException error) { directRejected = error.Message.Contains("RuntimeDetour"); }
                try
                {
                    ForgeDetour.PatchBatch(new[] { target }, new[] { replacement }, new[] { "fixture.raw-vs-hook.batch" },
                        "fixture.raw", new byte[1][]);
                }
                catch (InvalidOperationException error) { batchRejected = error.Message.Contains("RuntimeDetour"); }

                if (!directRejected || !batchRejected || adapter.ReadCalls != 0 || adapter.WriteCalls != 0 ||
                    adapter.ProtectCalls != 0 || adapter.FlushCalls != 0 || ForgeDetour.IsTracked(target))
                    throw new Exception("Raw detour paths must reject a shared hook reservation before reading or changing executable memory. " +
                        "directRejected=" + directRejected + ", batchRejected=" + batchRejected + ", reads=" + adapter.ReadCalls +
                        ", writes=" + adapter.WriteCalls + ", protects=" + adapter.ProtectCalls + ", flushes=" + adapter.FlushCalls +
                        ", tracked=" + ForgeDetour.IsTracked(target) + ".");
            }
            finally
            {
                if (registered) ForgeDetour.UnregisterHookTarget(target);
                if (ForgeDetour.IsTracked(target)) ForgeDetour.Unpatch(target);
                if (ForgeApi.Registry != null) ForgeApi.Disconnect();
                ForgeDetour.SetMemoryAdapterForTests(null);
            }
        }

        private static void TestForgeDetourPageBoundary()
        {
            EnsureNoTrackedDetours("TestForgeDetourPageBoundary");
            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            Exception testFailure = null;
            try
            {
                const int writeLength = 13;
                long pageSize = Environment.SystemPageSize;
                byte[] original = Enumerable.Range(0, writeLength).Select(index => (byte)(0x20 + index)).ToArray();
                byte[] installed = Enumerable.Range(0, writeLength).Select(index => (byte)(0x80 + index)).ToArray();

                // Ending exactly on the page boundary is a supported one-page range.
                IntPtr exactEndAddress = new IntPtr((pageSize * 2) - writeLength);
                ForgeDetour.WriteExecutableBytesForTests(exactEndAddress, installed, original);
                if (adapter.ProtectCalls != 2 || adapter.WriteCalls != 1 || adapter.FlushCalls != 1 || adapter.Protection != 0x20)
                    throw new Exception("A one-page write ending at the page boundary did not complete and restore protection.");

                int protectsBefore = adapter.ProtectCalls;
                int writesBefore = adapter.WriteCalls;
                int flushesBefore = adapter.FlushCalls;
                int readsBefore = adapter.ReadCalls;
                IntPtr crossingAddress = new IntPtr((pageSize * 3) - (writeLength - 1));
                bool rejected = false;
                try { ForgeDetour.WriteExecutableBytesForTests(crossingAddress, installed, original); }
                catch (InvalidOperationException error)
                {
                    rejected = error.Message.IndexOf("cross a system page boundary", StringComparison.Ordinal) >= 0;
                }

                if (!rejected || adapter.ReadCalls != readsBefore || adapter.ProtectCalls != protectsBefore || adapter.WriteCalls != writesBefore || adapter.FlushCalls != flushesBefore)
                    throw new Exception("A page-crossing write was not rejected before any executable-memory adapter operation.");
            }
            catch (Exception error)
            {
                testFailure = error;
                throw;
            }
            finally
            {
                var cleanupFailures = new List<string>();
                ResetFakeMemoryAdapter(cleanupFailures);
                ThrowIfFakeCleanupFailed("TestForgeDetourPageBoundary", testFailure, cleanupFailures);
            }
        }

        private static void TestForgeDetourPatchPageBoundaryCleanup()
        {
            var original = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var replacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic);
            EnsureNoTrackedDetours("TestForgeDetourPatchPageBoundaryCleanup");
            if (ForgePatcher.GetAppliedPatches().Count != 0)
                throw new InvalidOperationException("TestForgeDetourPatchPageBoundaryCleanup requires an empty legacy receipt registry.");
            IntPtr address = PrepareStableMethodAddresses("TestForgeDetourPatchPageBoundaryCleanup", original, replacement)[0];
            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            byte[] originalBytes = null;
            Exception testFailure = null;
            try
            {
                ForgeApi.Connect(new TestEngine());
                originalBytes = adapter.Read(address, 13);
                int readsBeforeRejection = adapter.ReadCalls;
                ForgeDetour.SetSystemPageSizeForTests(1);
                bool rejected = false;
                try { ForgeDetour.Patch(original, replacement); }
                catch (InvalidOperationException error)
                {
                    rejected = error.Message.IndexOf("cross a system page boundary", StringComparison.Ordinal) >= 0;
                }

                if (!rejected || ForgeDetour.GetTrackedSnapshots().Count != 0 || ForgeDetour.IsTracked(original) ||
                    ForgePatcher.GetAppliedPatches().Count != 0 || adapter.ReadCalls != readsBeforeRejection ||
                    adapter.ProtectCalls != 0 || adapter.WriteCalls != 0 || adapter.FlushCalls != 0)
                    throw new Exception("Public Patch retained a target/ID or touched executable memory after the pre-write page-boundary rejection.");

                // Reusing the same public route proves the rejected reservation did not keep
                // the target or its default patch ID in the registry.
                ForgeDetour.SetSystemPageSizeForTests(ulong.MaxValue);
                ForgeDetour.Patch(original, replacement);
                var applied = ForgeDetour.GetTrackedSnapshots();
                if (applied.Count != 1 || applied[0].State != ForgePatchState.Applied ||
                    !applied[0].PatchId.StartsWith("direct:", StringComparison.Ordinal) || !ForgeDetour.Unpatch(original) ||
                    ForgeDetour.GetTrackedSnapshots().Count != 0)
                    throw new Exception("Public Patch could not reuse and release the target after a rejected pre-write reservation.");
            }
            catch (Exception error)
            {
                testFailure = error;
                throw;
            }
            finally
            {
                var cleanupFailures = new List<string>();
                try { ForgeDetour.SetSystemPageSizeForTests(null); }
                catch (Exception error) { cleanupFailures.Add("Page-size test override reset failed: " + error.Message); }
                string cleanup = CleanupFakeTarget(adapter, original, address, originalBytes);
                if (cleanup != null) cleanupFailures.Add(cleanup);
                try { if (ForgeApi.Registry != null) ForgeApi.Disconnect(); }
                catch (Exception error) { cleanupFailures.Add("SDK disconnect failed after fake direct detour cleanup: " + error.Message); }
                ResetFakeMemoryAdapter(cleanupFailures);
                ThrowIfFakeCleanupFailed("TestForgeDetourPatchPageBoundaryCleanup", testFailure, cleanupFailures);
            }
        }

        private static void TestPatchBatchPageBoundaryCleanup()
        {
            var targetOne = typeof(PatchBatchMethods).GetMethod(nameof(PatchBatchMethods.TargetOne));
            var targetTwo = typeof(PatchBatchMethods).GetMethod(nameof(PatchBatchMethods.TargetTwo));
            var replacementOne = typeof(PatchBatchMethods).GetMethod(nameof(PatchBatchMethods.ReplacementOne));
            var replacementTwo = typeof(PatchBatchMethods).GetMethod(nameof(PatchBatchMethods.ReplacementTwo));
            EnsureNoTrackedDetours("TestPatchBatchPageBoundaryCleanup");
            if (ForgePatcher.GetAppliedPatches().Count != 0)
                throw new InvalidOperationException("TestPatchBatchPageBoundaryCleanup requires an empty legacy receipt registry.");
            IntPtr[] addresses = PrepareStableMethodAddresses("TestPatchBatchPageBoundaryCleanup", targetOne, replacementOne, targetTwo, replacementTwo);
            IntPtr firstAddress = addresses[0];
            IntPtr secondAddress = addresses[2];
            if (addresses.Distinct().Count() != addresses.Length)
                throw new InvalidOperationException("TestPatchBatchPageBoundaryCleanup: fixture methods did not resolve to unique prepared addresses.");

            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            byte[] originalOne = null;
            byte[] originalTwo = null;
            Exception testFailure = null;
            try
            {
                ForgeApi.Connect(new TestEngine());
                originalOne = adapter.Read(firstAddress, 13);
                originalTwo = adapter.Read(secondAddress, 13);
                int readsBeforeRejection = adapter.ReadCalls;
                ForgeDetour.SetSystemPageSizeForTests(1);
                bool rejected = false;
                try { ForgePatcher.ApplyAll(typeof(PatchBatchMethods).Assembly); }
                catch (InvalidOperationException error)
                {
                    rejected = error.Message.IndexOf("Patch batch was rejected", StringComparison.Ordinal) >= 0;
                }

                var rejectedSnapshots = ForgeDetour.GetTrackedSnapshots();
                var rejectedReceipts = ForgePatcher.GetAppliedPatches();
                if (!rejected || rejectedSnapshots.Count != 0 || rejectedReceipts.Count != 0 ||
                    ForgeDetour.IsTracked(targetOne) || ForgeDetour.IsTracked(targetTwo) ||
                    adapter.ReadCalls != readsBeforeRejection || adapter.ProtectCalls != 0 || adapter.WriteCalls != 0 || adapter.FlushCalls != 0)
                    throw new Exception("Batch page-boundary rejection retained an ID/target, recorded failure state, or touched executable memory.");

                // Retry the same declaration IDs under a permissive synthetic page size. A
                // successful retry proves the batch rollback released every reservation.
                ForgeDetour.SetSystemPageSizeForTests(ulong.MaxValue);
                if (ForgePatcher.ApplyAll(typeof(PatchBatchMethods).Assembly) != 2 ||
                    ForgeDetour.GetTrackedSnapshots().Count != 2 || ForgePatcher.GetAppliedPatches().Count != 2)
                    throw new Exception("The complete batch could not reuse its IDs and targets after the page-boundary rejection.");
                ForgePatcher.RevertAll();
                if (ForgeDetour.GetTrackedSnapshots().Count != 0 || ForgePatcher.GetAppliedPatches().Count != 0)
                    throw new Exception("The successful retry did not release all batch targets and receipts.");
            }
            catch (Exception error)
            {
                testFailure = error;
                throw;
            }
            finally
            {
                var cleanupFailures = new List<string>();
                try { ForgeDetour.SetSystemPageSizeForTests(ulong.MaxValue); }
                catch (Exception error) { cleanupFailures.Add("Page-size cleanup override failed: " + error.Message); }
                string firstCleanup = CleanupFakeTarget(adapter, targetOne, firstAddress, originalOne);
                if (firstCleanup != null) cleanupFailures.Add(firstCleanup);
                string secondCleanup = CleanupFakeTarget(adapter, targetTwo, secondAddress, originalTwo);
                if (secondCleanup != null) cleanupFailures.Add(secondCleanup);
                try { if (ForgeApi.Registry != null) ForgeApi.Disconnect(); }
                catch (Exception error) { cleanupFailures.Add("SDK disconnect failed after fake batch cleanup: " + error.Message); }
                try { ForgeDetour.SetSystemPageSizeForTests(null); }
                catch (Exception error) { cleanupFailures.Add("Page-size test override reset failed: " + error.Message); }
                ResetFakeMemoryAdapter(cleanupFailures);
                ThrowIfFakeCleanupFailed("TestPatchBatchPageBoundaryCleanup", testFailure, cleanupFailures);
            }
        }

        private static void TestPatchServiceLifecycle()
        {
            var original = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var replacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            var engine = new TestEngine();
            IForgePatchHandle handle = null;
            try
            {
                ForgeApi.Connect(engine);
                var staleService = ForgeApi.Patches;
                if (staleService == null) throw new Exception("Host should expose the optional patch service.");
                IntPtr targetAddress = original.MethodHandle.GetFunctionPointer();
                byte[] untouched = adapter.Read(targetAddress, 13);
                bool badSignatureRejected = false;
                try { staleService.ApplyMethodReplacement("fixture.bad-signature", "fixture.owner", original, typeof(SdkFeaturesTests).GetMethod(nameof(WrongSignatureMethod), BindingFlags.Static | BindingFlags.NonPublic)); }
                catch (ArgumentException) { badSignatureRejected = true; }
                if (!badSignatureRejected || !adapter.Read(targetAddress, 13).SequenceEqual(untouched))
                    throw new Exception("The explicit service must reject an incompatible signature before writing target bytes.");

                handle = staleService.ApplyMethodReplacement("fixture.patch", "fixture.owner", original, replacement);
                MethodInfo secondTarget = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethodTwo), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo secondReplacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethodTwo), BindingFlags.Static | BindingFlags.NonPublic);
                bool sharedIdRejected = false;
                try { ForgeDetour.Patch(secondTarget, secondReplacement, "fixture.patch", "other.owner"); }
                catch (InvalidOperationException) { sharedIdRejected = true; }
                if (!sharedIdRejected || ForgeDetour.IsTracked(secondTarget))
                    throw new Exception("Patch IDs must be unique across explicit and direct detour paths.");
                bool duplicateIdRejected = false;
                try { staleService.ApplyMethodReplacement("fixture.patch", "other.owner", typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic), original); }
                catch (InvalidOperationException) { duplicateIdRejected = true; }
                bool duplicateTargetRejected = false;
                try { staleService.ApplyMethodReplacement("fixture.other", "other.owner", original, replacement); }
                catch (InvalidOperationException) { duplicateTargetRejected = true; }
                var snapshots = staleService.GetSnapshots("fixture.owner");
                if (!duplicateIdRejected || !duplicateTargetRejected || snapshots.Count != 1 || !(snapshots is IList<ForgePatchSnapshot> snapshotList) || !snapshotList.IsReadOnly)
                    throw new Exception("The explicit service must reject duplicate IDs/targets and return immutable snapshots.");
                if (handle.Verify().State != ForgePatchState.Applied || !handle.Snapshot.IsIntact)
                    throw new Exception("Explicit service did not report the simulated detour as applied.");
                ForgeApi.Disconnect();
                if (ForgeApi.Patches != null || handle.Snapshot.State != ForgePatchState.Reverted || !handle.Revert().IsReverted || !handle.Revert().IsReverted)
                    throw new Exception("Disconnect should revert owned patches and keep the handle idempotent.");
                bool rejected = false;
                try { staleService.ApplyMethodReplacement("fixture.stale", "fixture.owner", original, replacement); }
                catch (InvalidOperationException) { rejected = true; }
                if (!rejected) throw new Exception("A stale service reference must reject post-disconnect applications.");

                bool directRejected = false;
                try { ForgeDetour.Patch(secondTarget, secondReplacement, "fixture.disconnected.direct", "direct.owner"); }
                catch (InvalidOperationException) { directRejected = true; }
                if (!directRejected || ForgeDetour.IsTracked(secondTarget))
                    throw new Exception("Direct and legacy patch adapters must reject applications after the host disconnects.");

                ForgeApi.Connect(engine);
                ForgeApi.Connect(engine);
                var reconnectedService = ForgeApi.Patches;
                if (!ReferenceEquals(staleService, reconnectedService))
                    throw new Exception("Reconnecting the same registry should reuse its optional patch service.");
                var reconnectedHandle = reconnectedService.ApplyMethodReplacement("fixture.reconnected.service", "fixture.owner", original, replacement);
                if (reconnectedHandle.Verify().State != ForgePatchState.Applied || !reconnectedHandle.Revert().IsReverted)
                    throw new Exception("A safely reverted service should resume applications after reconnecting the same registry.");
                ForgeDetour.Patch(secondTarget, secondReplacement, "fixture.reconnected.direct", "direct.owner");
                if (!ForgeDetour.IsTracked(secondTarget) || !ForgeDetour.Unpatch(secondTarget))
                    throw new Exception("A successful host reconnection must reopen direct patch applications and permit exact revert.");
                ForgeApi.Disconnect();
            }
            finally
            {
                if (ForgeApi.Registry != null) ForgeApi.Disconnect();
                ForgeDetour.SetMemoryAdapterForTests(null);
            }
        }

        private static void TestPatchServiceConflictRetention()
        {
            var original = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var replacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            var engine = new TestEngine();
            IForgePatchHandle handle = null;
            byte[] installed = null;
            IntPtr targetAddress = IntPtr.Zero;
            try
            {
                ForgeApi.Connect(engine);
                handle = ForgeApi.Patches.ApplyMethodReplacement("fixture.conflict", "fixture.owner", original, replacement);
                targetAddress = original.MethodHandle.GetFunctionPointer();
                installed = adapter.Read(targetAddress, 13);
                adapter.Tamper(targetAddress, 0, 0x90);
                byte[] foreign = adapter.Read(targetAddress, 13);
                bool disconnectBlocked = false;
                try { ForgeApi.Disconnect(); }
                catch (InvalidOperationException) { disconnectBlocked = true; }
                bool disposeBlocked = false;
                try { handle.Dispose(); }
                catch (InvalidOperationException) { disposeBlocked = true; }
                if (!disconnectBlocked || !disposeBlocked || !ReferenceEquals(ForgeApi.Registry, engine) ||
                    !ReferenceEquals(ForgeApi.Patches, ForgeApi.Registry) || handle.Snapshot.State != ForgePatchState.Conflict ||
                    handle.Revert().State != ForgePatchState.Conflict || !adapter.Read(targetAddress, 13).SequenceEqual(foreign))
                    throw new Exception("Disconnect or handle disposal must preserve the published recovery route and foreign target bytes while reporting Conflict.");
                bool reconnectBlocked = false;
                try { engine.Reconnect(); }
                catch (InvalidOperationException) { reconnectBlocked = true; }
                if (!reconnectBlocked || handle.Snapshot.State != ForgePatchState.Conflict || !adapter.Read(targetAddress, 13).SequenceEqual(foreign))
                    throw new Exception("A disconnected patch service must refuse reconnection while foreign target bytes remain.");
                adapter.Replace(targetAddress, installed);
                if (!handle.Revert().IsReverted || handle.Snapshot.State != ForgePatchState.Reverted)
                    throw new Exception("The retained capability must permit explicit recovery after the recorded patch bytes are restored.");
                ForgeApi.Disconnect();
            }
            finally
            {
                // Remove only this test's synthetic conflict so the process-wide test adapter
                // can be safely returned to native mode after the assertion has observed it.
                if (installed != null && targetAddress != IntPtr.Zero)
                {
                    if (ForgeDetour.IsTracked(original))
                    {
                        adapter.Replace(targetAddress, installed);
                        if (!ForgeDetour.Unpatch(original)) throw new Exception("Test fixture could not clean up its restored synthetic detour.");
                    }
                }
                if (ForgeApi.Registry != null) ForgeApi.Disconnect();
                ForgeDetour.SetMemoryAdapterForTests(null);
            }
        }

        private static void TestPatchServiceScope()
        {
            var ownedTarget = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var ownedReplacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var directTarget = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethodTwo), BindingFlags.Static | BindingFlags.NonPublic);
            var directReplacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethodTwo), BindingFlags.Static | BindingFlags.NonPublic);
            EnsureNoTrackedDetours("TestPatchServiceScope");
            PrepareStableMethodAddresses("TestPatchServiceScope", ownedTarget, ownedReplacement, directTarget, directReplacement);
            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            var engine = new TestEngine();
            IForgePatchService service = null;
            IForgePatchHandle handle = null;
            try
            {
                ForgeApi.Connect(engine);
                service = ForgeApi.Patches;
                if (service == null) throw new Exception("The host did not expose the optional patch capability.");
                handle = service.ApplyMethodReplacement("fixture.service.owned", "service.owner", ownedTarget, ownedReplacement);
                ForgeDetour.Patch(directTarget, directReplacement, "fixture.direct.unrelated", "direct.owner");
                service.Disconnect();
                var remaining = ForgeDetour.GetTrackedSnapshots();
                if (handle.Snapshot.State != ForgePatchState.Reverted || remaining.Count != 1 ||
                    remaining[0].PatchId != "fixture.direct.unrelated" || !ForgeDetour.IsTracked(directTarget))
                    throw new Exception("Service disconnect must revert its own handles while preserving unrelated direct registry entries.");
                if (!ForgeDetour.Unpatch(directTarget) || ForgeDetour.GetTrackedSnapshots().Count != 0)
                    throw new Exception("The isolated direct detour could not be reverted after service-scope verification.");
            }
            finally
            {
                if (ForgeApi.Registry != null) ForgeApi.Disconnect();
                if (ForgeDetour.IsTracked(directTarget)) ForgeDetour.Unpatch(directTarget);
                if (ForgeDetour.IsTracked(ownedTarget)) ForgeDetour.Unpatch(ownedTarget);
                ForgeDetour.SetMemoryAdapterForTests(null);
            }
        }

        private static void TestPatchServiceConnectionReplacement()
        {
            var original = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var replacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            var first = new TestEngine();
            var second = new TestEngine();
            IForgePatchHandle handle = null;
            try
            {
                ForgeApi.Connect(first);
                handle = ForgeApi.Patches.ApplyMethodReplacement("fixture.reconnect", "fixture.owner", original, replacement);
                ForgeApi.Connect(second);
                if (!ReferenceEquals(ForgeApi.Registry, second) || handle.Snapshot.State != ForgePatchState.Reverted ||
                    ForgeDetour.GetTrackedSnapshots().Count != 0)
                    throw new Exception("Replacing a host must disconnect and revert the previous patch service before publication.");
            }
            finally
            {
                if (ForgeApi.Registry != null) ForgeApi.Disconnect();
                ForgeDetour.SetMemoryAdapterForTests(null);
            }
        }

        private static void TestPatchServiceConnectionReplacementConflict()
        {
            var original = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var replacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic);
            EnsureNoTrackedDetours("TestPatchServiceConnectionReplacementConflict");
            IntPtr targetAddress = PrepareStableMethodAddresses("TestPatchServiceConnectionReplacementConflict", original, replacement)[0];
            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            var first = new TestEngine();
            var second = new TestEngine();
            IForgePatchHandle handle = null;
            byte[] installed = null;
            try
            {
                ForgeApi.Connect(first);
                IForgePatchService service = ForgeApi.Patches;
                handle = service.ApplyMethodReplacement("fixture.reconnect.conflict", "fixture.owner", original, replacement);
                installed = ForgeDetour.GetInstalledBytes(original);
                adapter.Tamper(targetAddress, 0, 0x90);

                bool replacementRejected = false;
                try { ForgeApi.Connect(second); }
                catch (InvalidOperationException) { replacementRejected = true; }
                if (!replacementRejected || !ReferenceEquals(ForgeApi.Registry, first) || !ReferenceEquals(ForgeApi.Patches, service) ||
                    handle.Snapshot.State != ForgePatchState.Conflict || ForgeDetour.GetTrackedSnapshots().Count != 1)
                    throw new Exception("A conflicted reconnect must leave the prior host published with its conflict visible for manual resolution.");

                bool applyRejected = false;
                try { service.ApplyMethodReplacement("fixture.reconnect.stale", "fixture.owner", original, replacement); }
                catch (InvalidOperationException) { applyRejected = true; }
                if (!applyRejected)
                    throw new Exception("A prior service must keep rejecting new patch applications after a failed host replacement.");

                adapter.Replace(targetAddress, installed);
                if (!handle.Revert().IsReverted || handle.Snapshot.State != ForgePatchState.Reverted)
                    throw new Exception("The retained conflict handle must allow explicit manual recovery after restoring the recorded bytes.");

                ForgeApi.Connect(second);
                if (!ReferenceEquals(ForgeApi.Registry, second) || ForgeDetour.GetTrackedSnapshots().Count != 0)
                    throw new Exception("Host replacement should succeed after the prior conflicted handle is explicitly resolved.");
            }
            finally
            {
                if (installed != null && ForgeDetour.IsTracked(original)) adapter.Replace(targetAddress, installed);
                if (ForgeApi.Registry != null) ForgeApi.Disconnect();
                if (ForgeDetour.IsTracked(original)) ForgeDetour.Unpatch(original);
                ForgeDetour.SetMemoryAdapterForTests(null);
            }
        }

        private static void TestPatchServiceLegacyCleanup()
        {
            var original = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var replacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var directTarget = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethodTwo), BindingFlags.Static | BindingFlags.NonPublic);
            var directReplacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethodTwo), BindingFlags.Static | BindingFlags.NonPublic);
            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            try
            {
                ForgeApi.Connect(new TestEngine());
                original.DetourWith(replacement);
                ForgeLivePatcher.ApplyDetour(directTarget, directReplacement);
                if (ForgeDetour.GetTrackedSnapshots().Count != 2)
                    throw new Exception("Legacy and direct calls should enter the same shared detour registry.");
                ForgeApi.Disconnect();
                if (ForgeDetour.GetTrackedSnapshots().Count != 0 || ForgePatcher.GetAppliedPatches().Count != 0)
                    throw new Exception("Disconnect must revert and clear verified direct and legacy receipts.");
            }
            finally
            {
                if (ForgeApi.Registry != null) ForgeApi.Disconnect();
                ForgeDetour.SetMemoryAdapterForTests(null);
            }
        }

        private static void TestStaleLegacyReceiptCannotRevertNewGeneration()
        {
            var target = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var replacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic);
            EnsureNoTrackedDetours("TestStaleLegacyReceiptCannotRevertNewGeneration");
            if (ForgePatcher.GetAppliedPatches().Any(record => record.Original == target))
                throw new InvalidOperationException("TestStaleLegacyReceiptCannotRevertNewGeneration requires an empty legacy receipt registry.");

            IntPtr address = PrepareStableMethodAddresses("TestStaleLegacyReceiptCannotRevertNewGeneration", target, replacement)[0];
            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            byte[] originalBytes = null;
            PatchRecord generationA = null;
            PatchRecord generationB = null;
            Exception testFailure = null;
            try
            {
                ForgeApi.Connect(new TestEngine());
                originalBytes = adapter.Read(address, 13);
                try { generationA = target.DetourWith(replacement); }
                catch (Exception error) { throw new InvalidOperationException("Could not apply receipt generation A.", error); }
                byte[] installedA = ForgeDetour.GetInstalledBytes(target);
                string patchIdA = generationA.Id;
                if (!generationA.IsIntact()) throw new Exception("Generation A receipt did not verify immediately after apply.");

                generationA.RevertDetour();
                if (ForgeDetour.IsTracked(target) || !adapter.Read(address, 13).SequenceEqual(originalBytes))
                    throw new Exception("Generation A did not restore the synthetic target before generation B was applied.");

                ForgeApi.Connect(new TestEngine());
                try { generationB = target.DetourWith(replacement); }
                catch (Exception error) { throw new InvalidOperationException("Could not apply receipt generation B.", error); }
                byte[] installedB = ForgeDetour.GetInstalledBytes(target);
                if (!string.Equals(generationB.Id, patchIdA, StringComparison.OrdinalIgnoreCase) ||
                    !generationA.OriginalBytes.SequenceEqual(generationB.OriginalBytes) ||
                    !installedA.SequenceEqual(installedB) || generationA.IsIntact() || !generationB.IsIntact())
                    throw new Exception("Fixture did not reproduce the same-ID, same-original-bytes second installation generation.");

                bool staleRecordRejected = false;
                try { ForgePatcher.Revert(generationA); }
                catch (InvalidOperationException error)
                {
                    staleRecordRejected = error.Message.IndexOf("stale", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                if (!staleRecordRejected || !ForgeDetour.IsTracked(target) || !generationB.IsIntact() ||
                    !adapter.Read(address, installedB.Length).SequenceEqual(installedB))
                    throw new Exception("A stale PatchRecord reverted or altered generation B.");

                bool staleByteReceiptRejected = false;
                try { MethodSwapper.RestoreMethod(target, generationA.OriginalBytes); }
                catch (InvalidOperationException error)
                {
                    staleByteReceiptRejected = error.Message.IndexOf("stale", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                if (!staleByteReceiptRejected || !ForgeDetour.IsTracked(target) || !generationB.IsIntact() ||
                    !adapter.Read(address, installedB.Length).SequenceEqual(installedB))
                    throw new Exception("A stale MethodSwapper byte receipt reverted or altered generation B.");

                generationB.RevertDetour();
                if (ForgeDetour.IsTracked(target) || ForgePatcher.GetAppliedPatches().Any(record => record.Original == target) ||
                    !adapter.Read(address, 13).SequenceEqual(originalBytes))
                    throw new Exception("Generation B did not remain independently revertible after stale-receipt rejection.");

                // A returned MethodSwapper array is an opaque identity token, not the
                // authoritative byte snapshot. Mutating it must not strand its own patch.
                byte[] mutableReceipt = MethodSwapper.DetourMethod(target, replacement);
                byte[] installedC = ForgeDetour.GetInstalledBytes(target);
                string patchIdC = ForgeDetour.GetTrackedPatchId(target);
                object generationTokenC = MethodSwapper.GetGenerationToken(target, mutableReceipt, patchIdC);
                mutableReceipt[0] ^= 0xFF;
                if (!ReferenceEquals(generationTokenC, MethodSwapper.GetGenerationToken(target, mutableReceipt, patchIdC)))
                    throw new Exception("Mutating the public receipt array changed its registered installation generation.");

                bool staleGenerationARejectedAgainstC = false;
                try { MethodSwapper.RestoreMethod(target, generationA.OriginalBytes); }
                catch (InvalidOperationException error)
                {
                    staleGenerationARejectedAgainstC = error.Message.IndexOf("stale", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                if (!staleGenerationARejectedAgainstC || !ForgeDetour.IsTracked(target) ||
                    !adapter.Read(address, installedC.Length).SequenceEqual(installedC))
                    throw new Exception("A stale generation-A receipt reverted or altered the current MethodSwapper generation.");

                MethodSwapper.RestoreMethod(target, mutableReceipt);
                if (ForgeDetour.IsTracked(target) || !adapter.Read(address, originalBytes.Length).SequenceEqual(originalBytes))
                    throw new Exception("A mutated public receipt could not safely restore its own installation generation.");
            }
            catch (Exception error)
            {
                testFailure = error;
                throw;
            }
            finally
            {
                var cleanupFailures = new List<string>();
                if (ForgeDetour.IsTracked(target))
                {
                    try
                    {
                        adapter.Replace(address, ForgeDetour.GetInstalledBytes(target));
                        var receipts = ForgePatcher.GetAppliedPatches().Where(record => record.Original == target).Reverse().ToArray();
                        if (receipts.Length > 0)
                            foreach (PatchRecord receipt in receipts) ForgePatcher.Revert(receipt);
                        else if (!ForgeDetour.Unpatch(target))
                            cleanupFailures.Add("Synthetic detour could not be removed from the target.");
                    }
                    catch (Exception error) { cleanupFailures.Add("Detour cleanup failed: " + error.Message); }
                }
                if (ForgeDetour.IsTracked(target)) cleanupFailures.Add("A detour remained tracked after cleanup.");
                if (originalBytes != null && !adapter.Read(address, originalBytes.Length).SequenceEqual(originalBytes))
                    cleanupFailures.Add("Synthetic target bytes were not restored after cleanup.");
                try { if (ForgeApi.Registry != null) ForgeApi.Disconnect(); }
                catch (Exception error) { cleanupFailures.Add("SDK disconnect failed after stale receipt regression: " + error.Message); }
                try { ForgeDetour.SetMemoryAdapterForTests(null); }
                catch (Exception error) { cleanupFailures.Add("Fake memory adapter reset failed: " + error.Message); }
                if (testFailure != null && cleanupFailures.Count > 0)
                    throw new AggregateException("Stale receipt regression failed and cleanup was incomplete.", cleanupFailures.Select(message => new InvalidOperationException(message)));
                if (cleanupFailures.Count > 0)
                    throw new AggregateException("Stale receipt regression cleanup failed.", cleanupFailures.Select(message => new InvalidOperationException(message)));
            }
        }

        private static void TestPatchBatchRollback()
        {
            var targetOne = typeof(PatchBatchMethods).GetMethod(nameof(PatchBatchMethods.TargetOne));
            var targetTwo = typeof(PatchBatchMethods).GetMethod(nameof(PatchBatchMethods.TargetTwo));
            var replacementOne = typeof(PatchBatchMethods).GetMethod(nameof(PatchBatchMethods.ReplacementOne));
            var replacementTwo = typeof(PatchBatchMethods).GetMethod(nameof(PatchBatchMethods.ReplacementTwo));
            EnsureNoTrackedDetours("TestPatchBatchRollback");
            IntPtr[] preparedAddresses = PrepareStableMethodAddresses("TestPatchBatchRollback", targetOne, replacementOne, targetTwo, replacementTwo);
            IntPtr firstAddress = preparedAddresses[0];
            IntPtr secondAddress = preparedAddresses[2];
            if (preparedAddresses.Distinct().Count() != preparedAddresses.Length)
                throw new InvalidOperationException("TestPatchBatchRollback: fixture methods did not resolve to unique prepared addresses: " +
                    string.Join(", ", preparedAddresses.Select(Address)) + ".");
            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            byte[] originalOne = null;
            byte[] originalTwo = null;
            Exception testFailure = null;
            try
            {
                ForgeApi.Connect(new TestEngine());
                originalOne = adapter.Read(firstAddress, 13);
                originalTwo = adapter.Read(secondAddress, 13);
                adapter.FailFlushForAddress = secondAddress;
                bool failed = false;
                try { ForgePatcher.ApplyAll(typeof(PatchBatchMethods).Assembly); }
                catch (InvalidOperationException) { failed = true; }
                bool firstRestored=adapter.Read(firstAddress, 13).SequenceEqual(originalOne);
                bool secondRestored=adapter.Read(secondAddress, 13).SequenceEqual(originalTwo);
                var tracked=ForgeDetour.GetTrackedSnapshots();
                var receipts=ForgePatcher.GetAppliedPatches();
                if (!failed || !firstRestored || !secondRestored || tracked.Count != 0 || receipts.Count != 0)
                    throw new Exception("Batch rollback mismatch: injectedAddress=" + Address(secondAddress) +
                        ", injectionTriggered=" + adapter.FlushFailureTriggered + ", failed=" + failed +
                        ", first=" + Address(firstAddress) + "/restored=" + firstRestored +
                        ", second=" + Address(secondAddress) + "/restored=" + secondRestored +
                        ", tracked=" + tracked.Count + " [" + string.Join("; ", tracked.Select(item => item.PatchId + ":" + item.State)) +
                        "], receipts=" + receipts.Count + " [" + string.Join("; ", receipts.Select(item => item.Id + "@" + (item.Original == null ? "<null>" : item.Original.Name))) +
                        "], flushCalls=" + adapter.FlushCalls + ", lastFlush=" + Address(adapter.LastFlushAddress) + ".");
            }
            catch (Exception error)
            {
                testFailure = error;
                throw;
            }
            finally
            {
                var cleanupFailures = new List<string>();
                string firstCleanup = CleanupFakeTarget(adapter, targetOne, firstAddress, originalOne);
                if (firstCleanup != null) cleanupFailures.Add(firstCleanup);
                string secondCleanup = CleanupFakeTarget(adapter, targetTwo, secondAddress, originalTwo);
                if (secondCleanup != null) cleanupFailures.Add(secondCleanup);
                try { if (ForgeApi.Registry != null) ForgeApi.Disconnect(); }
                catch (Exception error) { cleanupFailures.Add("SDK disconnect failed after fake batch cleanup: " + error.Message); }
                ResetFakeMemoryAdapter(cleanupFailures);
                ThrowIfFakeCleanupFailed("TestPatchBatchRollback", testFailure, cleanupFailures);
            }
        }

        private static void TestPatchConsoleRoutes()
        {
            var original = typeof(SdkFeaturesTests).GetMethod(nameof(TargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var replacement = typeof(SdkFeaturesTests).GetMethod(nameof(ReplacementMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var adapter = new FakeExecutableMemoryAdapter();
            ForgeDetour.SetMemoryAdapterForTests(adapter);
            try
            {
                ForgeApi.Connect(new TestEngine());
                if (!ForgeCommands.PatchStatus(new List<string> { "console.owner", "ignored" }).StartsWith("Usage: cf.patch_status", StringComparison.Ordinal) ||
                    !ForgeCommands.PatchStatus(new List<string> { " " }).StartsWith("Usage: cf.patch_status", StringComparison.Ordinal) ||
                    !ForgeCommands.HookStatus(new List<string> { "console.owner", "ignored", "prefix", "extra" }).StartsWith("Usage: cf.hook_status", StringComparison.Ordinal) ||
                    !ForgeCommands.HookStatus(new List<string> { " " }).StartsWith("Usage: cf.hook_status", StringComparison.Ordinal))
                    throw new Exception("Optional status commands must reject missing/extra owner arguments instead of silently widening the query.");
                ForgeApi.Hooks.Register(new ForgeHookDefinition { Id = "console.hook.prefix", Owner = "console.owner", Target = original, Prefix = _ => { } });
                ForgeApi.Hooks.Register(new ForgeHookDefinition { Id = "console.hook.finalizer", Owner = "other.owner", Target = original, Finalizer = _ => { } });
                string prefixInventory = ForgeCommands.HookStatus(new List<string> { "console.owner", "TargetMethod", "prefix" });
                string finalizerInventory = ForgeCommands.HookStatus(new List<string> { "-", "TargetMethod", "finalizer" });
                if (!prefixInventory.Contains("console.hook.prefix") || prefixInventory.Contains("console.hook.finalizer") ||
                    !finalizerInventory.Contains("console.hook.finalizer") || finalizerInventory.Contains("console.hook.prefix") ||
                    !ForgeCommands.HookStatus(new List<string> { "-", "-", "arbitrary" }).StartsWith("Usage:", StringComparison.Ordinal) ||
                    !ForgeCommands.HookStatus(new List<string> { "-", "no-such-target", "prefix" }).Contains("No registered"))
                    throw new Exception("Hook inventory must compose owner, target and type filters without applying registered declarations.");
                if (ForgeApi.Hooks.GetSnapshots().Any(item => item.State != ForgeHookState.Registered))
                    throw new Exception("Read-only console inventory must not apply hooks.");
                ForgeDetour.Patch(original, replacement, "console.patch.id", "console.owner");
                string status = ForgeCommands.PatchStatus(new List<string> { "console.owner" });
                if (!status.Contains("console.patch.id") || !status.Contains("Applied"))
                    throw new Exception("patch_status must inventory direct shared-registry records by owner.");
                string byId = ForgeCommands.PatchRevert(new List<string> { "console.patch.id" });
                if (!byId.Contains("1 reverted") || ForgeDetour.GetTrackedSnapshots().Count != 0)
                    throw new Exception("patch_revert by ID did not use the shared registry.");

                ForgeDetour.Patch(original, replacement, "console.patch.owner", "console.owner");
                string byOwner = ForgeCommands.PatchRevert(new List<string> { "console.owner" });
                if (!byOwner.Contains("1 reverted") || ForgeDetour.GetTrackedSnapshots().Count != 0)
                    throw new Exception("patch_revert by owner did not use the shared registry.");

                ForgeDetour.Patch(original, replacement, "same", "same");
                string ambiguous = ForgeCommands.PatchRevert(new List<string> { "same" });
                if (!ambiguous.Contains("both a patch ID and an owner") || ForgeDetour.GetTrackedSnapshots().Count != 1)
                    throw new Exception("patch_revert must reject an ID/owner ambiguity without writing.");
                if (!ForgeDetour.RevertById("same").IsReverted)
                    throw new Exception("The ambiguous-command fixture could not be safely cleaned up by its exact ID.");

                ForgeDetour.Patch(original, replacement, "all", "console.owner");
                string reservedAll = ForgeCommands.PatchRevert(new List<string> { "all" });
                if (!reservedAll.Contains("reserved command") || ForgeDetour.GetTrackedSnapshots().Count != 1)
                    throw new Exception("patch_revert must reject reserved 'all' collisions without writing.");
                if (!ForgeDetour.RevertById("all").IsReverted)
                    throw new Exception("The reserved-command fixture could not be safely cleaned up by its exact ID.");

                ForgeDetour.Patch(original, replacement, "console.patch.owner-all", "all");
                string reservedOwner = ForgeCommands.PatchRevert(new List<string> { "all" });
                if (!reservedOwner.Contains("reserved command") || ForgeDetour.GetTrackedSnapshots().Count != 1)
                    throw new Exception("patch_revert must reject a reserved 'all' owner collision without writing.");
                if (!ForgeDetour.RevertById("console.patch.owner-all").IsReverted)
                    throw new Exception("The reserved-owner fixture could not be safely cleaned up by its exact ID.");

                ForgeDetour.Patch(original, replacement, "all", "all");
                string reservedIdAndOwner = ForgeCommands.PatchRevert(new List<string> { "all" });
                if (!reservedIdAndOwner.Contains("both a patch ID and an owner") || ForgeDetour.GetTrackedSnapshots().Count != 1)
                    throw new Exception("patch_revert must reject a reserved 'all' ID/owner collision without writing.");
                if (!ForgeDetour.RevertById("all").IsReverted)
                    throw new Exception("The reserved ID/owner fixture could not be safely cleaned up by its exact ID.");

                ForgeDetour.Patch(original, replacement, "console.patch.all", "console.owner");
                string all = ForgeCommands.PatchRevert(new List<string> { "all" });
                if (!all.Contains("1 reverted") || ForgeDetour.GetTrackedSnapshots().Count != 0)
                    throw new Exception("patch_revert all did not safely revert the global inventory.");
            }
            finally
            {
                if (ForgeApi.Registry != null) ForgeApi.Disconnect();
                if (ForgeDetour.IsTracked(original)) ForgeDetour.Unpatch(original);
                ForgeDetour.SetMemoryAdapterForTests(null);
            }
        }

        private sealed class FakeExecutableMemoryAdapter : ForgeDetour.IExecutableMemoryAdapter
        {
            private readonly Dictionary<IntPtr, byte[]> memory = new Dictionary<IntPtr, byte[]>();
            public Architecture ProcessArchitecture { get; set; } = Architecture.X64;
            internal bool FailNextProtect;
            internal bool FailNextFlush;
            internal bool FailNextProtectionRestore;
            internal int FailWriteOnCall;
            internal int ReadCalls;
            internal int WriteCalls;
            internal int ProtectCalls;
            internal IntPtr FailFlushForAddress;
            internal bool FlushFailureTriggered;
            internal int FlushCalls;
            internal IntPtr LastFlushAddress;
            internal uint Protection = 0x20;
            public byte[] Read(IntPtr address, int count)
            {
                ReadCalls++;
                byte[] bytes;
                if (!memory.TryGetValue(address, out bytes))
                {
                    bytes = Enumerable.Range(0, count).Select(index => (byte)(0x20 + index)).ToArray();
                    memory[address] = bytes;
                }
                return bytes.Take(count).ToArray();
            }
            public void Write(IntPtr address, byte[] bytes)
            {
                WriteCalls++;
                if (FailWriteOnCall != 0 && WriteCalls == FailWriteOnCall)
                    throw new InvalidOperationException("Injected executable write failure.");
                memory[address] = (byte[])bytes.Clone();
            }
            public bool TryProtect(IntPtr address, int count, uint newProtect, out uint oldProtect, out int error)
            {
                ProtectCalls++;
                if (FailNextProtect && newProtect == 0x40) { FailNextProtect = false; oldProtect = Protection; error = 5; return false; }
                oldProtect = Protection;
                if (FailNextProtectionRestore && newProtect != 0x40)
                {
                    FailNextProtectionRestore = false;
                    error = 5;
                    return false;
                }
                Protection = newProtect;
                error = 0;
                return true;
            }
            public bool Flush(IntPtr address, int count, out int error)
            {
                FlushCalls++;
                LastFlushAddress = address;
                if (FailFlushForAddress != IntPtr.Zero && address == FailFlushForAddress)
                {
                    FailFlushForAddress = IntPtr.Zero;
                    FlushFailureTriggered = true;
                    error = 31;
                    return false;
                }
                if (FailNextFlush) { FailNextFlush = false; FlushFailureTriggered = true; error = 31; return false; }
                error = 0; return true;
            }
            internal void Tamper(IntPtr address, int offset, byte value) { memory[address][offset] = value; }
            internal void Replace(IntPtr address, byte[] bytes) { memory[address] = (byte[])bytes.Clone(); }
        }

        private static IntPtr[] PrepareStableMethodAddresses(string testName, params MethodInfo[] methods)
        {
            if (methods == null || methods.Length == 0)
                throw new ArgumentException(testName + ": at least one method is required for address preparation.", nameof(methods));
            for (int index = 0; index < methods.Length; index++)
            {
                MethodInfo method = methods[index];
                if (method == null)
                    throw new InvalidOperationException(testName + ": method at index " + index + " could not be resolved.");
                if (method.IsAbstract || method.ContainsGenericParameters || method.IsGenericMethod)
                    throw new InvalidOperationException(testName + ": fixture method is not a closed concrete method: " + MethodIdentity(method) + ".");
                System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(method.MethodHandle);
            }

            var addresses = methods.Select(method => method.MethodHandle.GetFunctionPointer()).ToArray();
            for (int index = 0; index < methods.Length; index++)
                System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(methods[index].MethodHandle);
            for (int index = 0; index < methods.Length; index++)
            {
                IntPtr verified = methods[index].MethodHandle.GetFunctionPointer();
                if (addresses[index] == IntPtr.Zero || verified != addresses[index])
                    throw new InvalidOperationException(testName + ": method address changed after preparation for " + MethodIdentity(methods[index]) +
                        " (first=" + Address(addresses[index]) + ", second=" + Address(verified) + ").");
            }
            return addresses;
        }

        private static void EnsureNoTrackedDetours(string testName)
        {
            var tracked = ForgeDetour.GetTrackedSnapshots();
            if (tracked.Count != 0)
                throw new InvalidOperationException(testName + ": fake-memory fixture requires an empty detour registry; found " + tracked.Count +
                    " record(s): " + string.Join("; ", tracked.Select(item => item.PatchId + ":" + item.State + " target=" + item.TargetMethod)) + ".");
        }

        private static string CleanupFakeTarget(FakeExecutableMemoryAdapter adapter, MethodInfo target, IntPtr preparedAddress, byte[] originalBytes)
        {
            var failures = new List<string>();
            try
            {
                var receipts = ForgePatcher.GetAppliedPatches().Where(record => record.Original == target).Reverse().ToArray();
                if (ForgeDetour.IsTracked(target))
                {
                    // The fake adapter is synthetic and owned by this test. Restore only its
                    // recorded jump, then use the ordinary guarded revert to clear the receipt.
                    adapter.Replace(preparedAddress, ForgeDetour.GetInstalledBytes(target));
                    if (receipts.Length > 0)
                    {
                        foreach (PatchRecord receipt in receipts) ForgePatcher.Revert(receipt);
                    }
                    else if (!ForgeDetour.Unpatch(target))
                    {
                        failures.Add("Unpatch returned false for " + MethodIdentity(target) + " at " + Address(preparedAddress) + ".");
                    }
                }
                else if (receipts.Length > 0)
                {
                    failures.Add("Legacy receipt(s) remain without a tracked detour for " + MethodIdentity(target) + ": " +
                        string.Join(", ", receipts.Select(record => record.Id)) + ".");
                }

                if (ForgeDetour.IsTracked(target)) failures.Add("Detour remains tracked for " + MethodIdentity(target) + ".");
                var remainingReceipts = ForgePatcher.GetAppliedPatches().Where(record => record.Original == target).Select(record => record.Id).ToArray();
                if (remainingReceipts.Length > 0)
                    failures.Add("Legacy receipt cleanup incomplete for " + MethodIdentity(target) + ": " + string.Join(", ", remainingReceipts) + ".");
                if (originalBytes != null && !adapter.Read(preparedAddress, originalBytes.Length).SequenceEqual(originalBytes))
                    failures.Add("Synthetic bytes were not restored for " + MethodIdentity(target) + " at " + Address(preparedAddress) + ".");
            }
            catch (Exception error)
            {
                failures.Add(MethodIdentity(target) + " cleanup threw " + error.GetType().Name + ": " + error.Message);
            }
            return failures.Count == 0 ? null : string.Join(" ", failures);
        }

        private static void ResetFakeMemoryAdapter(List<string> failures)
        {
            try
            {
                var remaining = ForgeDetour.GetTrackedSnapshots();
                if (remaining.Count != 0)
                {
                    // The fixture verifies an empty registry before installing the adapter,
                    // so any remaining entry is owned by this test and can get one safe pass.
                    ForgeDetour.UnpatchAllForLifecycle();
                    remaining = ForgeDetour.GetTrackedSnapshots();
                }
                if (remaining.Count != 0)
                    failures.Add("Cannot reset fake memory adapter; tracked detours remain: " +
                        string.Join("; ", remaining.Select(item => item.PatchId + ":" + item.State + " target=" + item.TargetMethod)) + ".");
                else
                    ForgeDetour.SetMemoryAdapterForTests(null);
            }
            catch (Exception error)
            {
                failures.Add("Fake memory adapter reset threw " + error.GetType().Name + ": " + error.Message);
            }
        }

        private static void ThrowIfFakeCleanupFailed(string testName, Exception testFailure, List<string> cleanupFailures)
        {
            if (cleanupFailures == null || cleanupFailures.Count == 0) return;
            string detail = testName + ": fake detour cleanup was incomplete: " + string.Join(" | ", cleanupFailures);
            if (testFailure != null)
                throw new AggregateException(detail + " Original test failure: " + testFailure.Message, testFailure, new InvalidOperationException(detail));
            throw new InvalidOperationException(detail);
        }

        private static string MethodIdentity(MethodInfo method)
        {
            return method == null ? "<null>" : (method.DeclaringType == null ? "?" : method.DeclaringType.FullName) + "." + method.Name;
        }

        private static string Address(IntPtr address)
        {
            return address == IntPtr.Zero ? "<zero>" : "0x" + address.ToInt64().ToString("X");
        }

        [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
        private static extern uint PInvokeSignatureFixture();

        private static uint ManagedUIntSignatureFixture() => 0;

        private static int VarArgsSignatureFixture(int value, __arglist) => value;

        private static int VarArgsReplacementSignatureFixture(int value, __arglist) => value + 1;

        private static int FixedArgumentsSignatureFixture(int value) => value;

        public sealed class CustomModifierFixtureTag { }

        private class ReceiverBaseFixture
        {
            public int Target(int value) => value;
        }

        private sealed class ReceiverDerivedFixture : ReceiverBaseFixture
        {
            public int Replacement(int value) => value + 1;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static string TargetMethod() => "Original";

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static string ReplacementMethod() => "Replacement";

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static string TargetMethodTwo() => "Original two";

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static string ReplacementMethodTwo() => "Replacement two";

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static int WrongSignatureMethod() => 99;
        
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
            // Pick a high ephemeral port rather than sharing a fixed test port with
            // developer tools or concurrent CI jobs.
            int port;
            var reservation = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            try
            {
                reservation.Start();
                port = ((System.Net.IPEndPoint)reservation.LocalEndpoint).Port;
            }
            finally { reservation.Stop(); }
            string url = "http://localhost:" + port.ToString(CultureInfo.InvariantCulture) + "/";
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
            if (ForgeApi.Version != 13)
                throw new Exception($"ForgeApi.Version should be 13. Got: {ForgeApi.Version}");
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
                File.WriteAllText(Path.Combine(root, "BlockCommentOnly.cs"),
                    "/* ForgeEventKind.Pulse new ForgeEventSubscription { Event = ForgeEventKind.Pulse } */ class BlockCommentOnly {}");
                File.WriteAllText(Path.Combine(root, "BlockCommentSpoof.cs"),
                    "/* MinimumIntervalMilliseconds = 999 */ class BlockCommentSpoof { void Register() { var item = new ForgeEventSubscription { Event = ForgeEventKind.Pulse }; } }");

                string rawDelimiter = new string('"', 3);
                File.WriteAllText(Path.Combine(root, "RawStringOnly.cs"),
                    "class RawOnly { void Register() { var text = " + rawDelimiter +
                    "new ForgeEventSubscription { Event = ForgeEventKind.Pulse, MinimumIntervalMilliseconds = 999 }" +
                    rawDelimiter + "; } }");
                File.WriteAllText(Path.Combine(root, "RawStringActual.cs"),
                    "class RawActual { void Register() { var text = " + rawDelimiter +
                    "new ForgeEventSubscription { Event = ForgeEventKind.Pulse, MinimumIntervalMilliseconds = 999 }" +
                    rawDelimiter + "; var item = new ForgeEventSubscription { Event = ForgeEventKind.Pulse }; } }");
                File.WriteAllText(Path.Combine(root, "RawInterpolatedStringOnly.cs"),
                    "class RawInterpolatedOnly { void Register() { var text = $" + rawDelimiter + " {Format(" +
                    rawDelimiter + "new ForgeEventSubscription { Event = ForgeEventKind.Pulse }" +
                    rawDelimiter + ")}" + rawDelimiter + "; } }");
                File.WriteAllText(Path.Combine(root, "InterpolatedStringOnly.cs"),
                    "class InterpolatedOnly { void Register() { var text = $\"{Format(\"new ForgeEventSubscription { Event = ForgeEventKind.Pulse }\")}\"; } }");
                File.WriteAllText(Path.Combine(root, "InterpolatedStringActual.cs"),
                    "class InterpolatedActual { void Register() { var text = $\"{Format(\"new ForgeEventSubscription { Event = ForgeEventKind.Pulse, MinimumIntervalMilliseconds = 999 }\")}\"; var item = new ForgeEventSubscription { Event = ForgeEventKind.Pulse }; } }");
                File.WriteAllText(Path.Combine(root, "InterpolatedCommentOnly.cs"),
                    "class InterpolatedCommentOnly { void Register() { var text = $\"{Format(/* new ForgeEventSubscription { Event = ForgeEventKind.Pulse } */ \"value\")}\"; } }");
                string deeplyNestedInterpolation = "\"leaf\"";
                for (int i = 0; i < 40; i++)
                    deeplyNestedInterpolation = "$\"{" + deeplyNestedInterpolation + "}\"";
                File.WriteAllText(Path.Combine(root, "DeepInterpolatedString.cs"),
                    "class DeepInterpolated { void Register() { var text = " + deeplyNestedInterpolation +
                    "; var item = new ForgeEventSubscription { Event = ForgeEventKind.Pulse }; } }");
                File.WriteAllText(Path.Combine(root, "UnterminatedBlockComment.cs"),
                    "class UnterminatedComment { /* comment never closes; var item = new ForgeEventSubscription { Event = ForgeEventKind.Pulse }; }");
                File.WriteAllText(Path.Combine(root, "UnterminatedString.cs"),
                    "class UnterminatedString { void Register() { var text = \"string never closes; var item = new ForgeEventSubscription { Event = ForgeEventKind.Pulse }; } }");
                File.WriteAllText(Path.Combine(root, "ComputedEvent.cs"),
                    "class ComputedEvent { void Register() { var item = new ForgeEventSubscription { Event = Resolve(ForgeEventKind.Pulse) }; } }");
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
                if (result.Findings.Any(f => f.RuleId == "FORGEWEAVE_UNTHROTTLED_PULSE" &&
                    (f.FilePath.EndsWith("BlockCommentOnly.cs") ||
                    f.FilePath.EndsWith("RawStringOnly.cs") || f.FilePath.EndsWith("InterpolatedStringOnly.cs") ||
                    f.FilePath.EndsWith("RawInterpolatedStringOnly.cs") ||
                    f.FilePath.EndsWith("InterpolatedCommentOnly.cs"))))
                    throw new Exception("Pulse source text found only in a block comment, raw string, or interpolated expression was treated as executable code.");
                if (!result.Findings.Any(f => f.RuleId == "FORGEWEAVE_UNTHROTTLED_PULSE" && f.FilePath.EndsWith("RawStringActual.cs")))
                    throw new Exception("A raw string caused an actual unthrottled Pulse subscription after it to be skipped.");
                if (result.Findings.Count(f => f.RuleId == "FORGEWEAVE_UNTHROTTLED_PULSE" && f.FilePath.EndsWith("InterpolatedStringActual.cs")) != 1)
                    throw new Exception("An interpolated expression either hid an actual unthrottled Pulse subscription or exposed its string decoy.");
                if (!result.Findings.Any(f => f.RuleId == "FORGEWEAVE_UNTHROTTLED_PULSE" && f.FilePath.EndsWith("BlockCommentSpoof.cs")))
                    throw new Exception("A block comment incorrectly rescued an actual unthrottled Pulse subscription.");
                if (!result.Findings.Any(f => f.RuleId == "FORGEWEAVE_UNTHROTTLED_PULSE" && f.FilePath.EndsWith("ComputedEvent.cs")))
                    throw new Exception("A computed Pulse event stopped failing closed after lexer reuse.");
                if (!result.Findings.Any(f => f.RuleId == "AUDIT_CSHARP_LEX_INCOMPLETE" && f.FilePath.EndsWith("DeepInterpolatedString.cs")))
                    throw new Exception("A deeply nested interpolation reached the lexer limit without an explicit incomplete-source error.");
                if (!result.Findings.Any(f => f.RuleId == "AUDIT_CSHARP_LEX_INCOMPLETE" && f.FilePath.EndsWith("UnterminatedBlockComment.cs")) ||
                    !result.Findings.Any(f => f.RuleId == "AUDIT_CSHARP_LEX_INCOMPLETE" && f.FilePath.EndsWith("UnterminatedString.cs")))
                    throw new Exception("An unterminated C# comment or string was masked without an explicit incomplete-source error.");
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
            if (ForgeTimeSlicer.GetBucket(null, 24) != 0 || ForgeTimeSlicer.GetBucket(string.Empty, 24) != 0)
                throw new Exception("Null and empty entity IDs must map to bucket zero.");
            if (!ForgeTimeSlicer.ShouldProcess(null, 0, 24) || ForgeTimeSlicer.ShouldProcess(null, 1, 24)
                || !ForgeTimeSlicer.ShouldProcess(string.Empty, 0, 24) || ForgeTimeSlicer.ShouldProcess(string.Empty, 1, 24))
                throw new Exception("Null and empty entity IDs must be selected only for bucket zero.");

            const int maximumBucketCount = int.MaxValue;
            const string upperBucketId = "time_slice_upper_bound_100";
            int upperBucketHour = ForgeTimeSlicer.GetBucket(upperBucketId, maximumBucketCount);
            if (upperBucketHour <= int.MaxValue / 2)
                throw new Exception("ForgeTimeSlicer upper-bound regression fixture must exercise overflowing positive normalization.");
            if (!ForgeTimeSlicer.ShouldProcess(upperBucketId, upperBucketHour, maximumBucketCount))
                throw new Exception("ShouldProcess must normalize large positive hours without overflowing for the maximum bucket count.");

            var upperBucketEntities = new List<string> { upperBucketId };
            int indexedUpperBucketProcessed = ForgeTimeSlicer.ProcessBatch(
                (IReadOnlyList<string>)upperBucketEntities, id => id, _ => { }, upperBucketHour, maximumBucketCount);
            if (indexedUpperBucketProcessed != 1)
                throw new Exception("The IReadOnlyList ProcessBatch overload must safely normalize large positive hours.");

            int enumerableUpperBucketProcessed = ForgeTimeSlicer.ProcessBatch(
                (IEnumerable<string>)upperBucketEntities, id => id, _ => { }, upperBucketHour, maximumBucketCount);
            if (enumerableUpperBucketProcessed != 1)
                throw new Exception("The IEnumerable ProcessBatch overload must safely normalize large positive hours.");

            foreach (string entityId in new[] { "hero_01", "settlement_02", "party_03", "entity_04" })
            {
                int assignedHours = 0;
                for (int hour = 0; hour < ForgeTimeSlicer.DefaultHourlyBuckets; hour++)
                {
                    if (ForgeTimeSlicer.ShouldProcess(entityId, hour)) assignedHours++;
                    if (ForgeTimeSlicer.ShouldProcess(entityId, hour) !=
                        ForgeTimeSlicer.ShouldProcess(entityId, hour + ForgeTimeSlicer.DefaultHourlyBuckets))
                        throw new Exception("Stable-ID scheduling must repeat for the same hour bucket each 24-hour cycle.");
                }
                if (assignedHours != 1)
                    throw new Exception("Each stable ID must map to one hour bucket per default 24-hour cycle.");
            }

            const int traversalBucketCount = 2;
            int traversalHour = ForgeTimeSlicer.GetBucket(upperBucketId, traversalBucketCount);
            string unselectedId = null;
            for (int candidateIndex = 0; candidateIndex < 32; candidateIndex++)
            {
                string candidate = "time_slice_unselected_" + candidateIndex;
                if (!ForgeTimeSlicer.ShouldProcess(candidate, traversalHour, traversalBucketCount))
                {
                    unselectedId = candidate;
                    break;
                }
            }
            if (unselectedId == null)
                throw new Exception("Traversal regression fixture must include an entity outside the selected bucket.");

            var traversalEntities = new List<string> { upperBucketId, unselectedId };
            int selectorCalls = 0;
            int processorCalls = 0;
            int traversalProcessed = ForgeTimeSlicer.ProcessBatch(
                (IReadOnlyList<string>)traversalEntities,
                id => { selectorCalls++; return id; },
                _ => processorCalls++,
                traversalHour,
                traversalBucketCount);
            if (selectorCalls != traversalEntities.Count || traversalProcessed != 1 || processorCalls != 1)
                throw new Exception("Time-sliced batch selection must inspect all source entries while processing only matching buckets.");
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

        private static void TestRev137MultilayerHardeningAndSafety()
        {
            // 1. ForgeSaveChunker boundary safety
            if (ForgeSaveChunker.NeedsChunking("abc", -1)) throw new Exception("NeedsChunking should return false for negative maxChunkSize.");
            if (ForgeSaveChunker.NeedsChunking("abc", 0)) throw new Exception("NeedsChunking should return false for zero maxChunkSize.");
            if (ForgeSaveChunker.NeedsChunking(null, 10)) throw new Exception("NeedsChunking should return false for null data.");
            if (ForgeSaveChunker.NeedsChunking("", 10)) throw new Exception("NeedsChunking should return false for empty data.");
            if (!ForgeSaveChunker.NeedsChunking("hello world", 5)) throw new Exception("NeedsChunking should return true for length 11 > 5.");

            var chunks = ForgeSaveChunker.Chunk("hello world", 5);
            if (chunks.Length != 3 || chunks[0] != "hello" || chunks[1] != " worl" || chunks[2] != "d")
                throw new Exception("ForgeSaveChunker.Chunk produced unexpected chunks.");
            if (ForgeSaveChunker.Reassemble(chunks) != "hello world")
                throw new Exception("ForgeSaveChunker.Reassemble failed to reconstruct original string.");
            if (ForgeSaveChunker.Reassemble(new string[] { null, "a", null, "b" }) != "ab")
                throw new Exception("ForgeSaveChunker.Reassemble failed null element tolerance.");

            // 2. ForgeMissionLifecycleGuard deferred initializer error resilience
            int attempts = 0;
            var guard = new ForgeMissionLifecycleGuard(() =>
            {
                attempts++;
                if (attempts == 1) throw new InvalidOperationException("Simulation transient error");
            });
            bool caught = false;
            try { guard.OnTick(0.1f); } catch (InvalidOperationException) { caught = true; }
            if (!caught || guard.IsInitialized) throw new Exception("Guard prematurely marked initialized on exception.");
            bool second = guard.OnTick(0.1f);
            if (!second || !guard.IsInitialized || attempts != 2) throw new Exception("Guard failed to initialize on retry.");
            bool third = guard.OnTick(0.1f);
            if (third) throw new Exception("Guard re-executed after successful initialization.");

            // 3. ForgePartyBlueprint StartingFood and troop ID guards
            var bp = ForgePartySpawner.CreateBlueprint("test_party", "Raiders", "vlandia")
                .SetBudget(500, -5f)
                .AddTroop("vlandian_recruit", 10);
            var (isValid, errors) = bp.Validate();
            if (isValid || !errors.Any(e => e.Contains("Starting food cannot be negative")))
                throw new Exception("Blueprint allowed negative starting food.");

            bp.AddTroop("   ", 5);
            bp.AddTroop("valid_troop", 0);
            if (bp.TroopRoster.ContainsKey("   ") || bp.TroopRoster.ContainsKey("valid_troop"))
                throw new Exception("Blueprint accepted invalid troop addition.");

            // 4. ForgeAgentMemory Episodic bounded eviction and GetAll pre-allocation
            string testAgent = "test_agent_rev137";
            for (int i = 0; i < ForgeAgentMemory.MaximumEpisodicEntriesPerType + 10; i++)
            {
                ForgeAgentMemory.Episodic.Add(testAgent, "test_event", i);
            }
            int count = ForgeAgentMemory.Episodic.Count(testAgent, "test_event");
            if (count != ForgeAgentMemory.MaximumEpisodicEntriesPerType)
                throw new Exception("Episodic count exceeded MaximumEpisodicEntriesPerType: " + count);
            var episodes = ForgeAgentMemory.Episodic.GetAll(testAgent, "test_event");
            if (episodes.Count != ForgeAgentMemory.MaximumEpisodicEntriesPerType)
                throw new Exception("GetAll count did not match maximum: " + episodes.Count);
            ForgeAgentMemory.ClearAgent(testAgent);
        }
    }
}
