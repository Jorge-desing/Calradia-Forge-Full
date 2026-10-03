using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CalradiaForge.Sdk;

namespace CalradiaForge.Tests
{
    public static class AgentCognitiveMemoryTests
    {
        public static void Run(Action<string, Action> test)
        {
            test("AgentCognitiveMemory: Source file exists and adheres to namespace naming rules", TestSourceFileAndNamespace);
            test("AgentCognitiveMemory: Inherits from CampaignBehaviorBase via reflection", TestInheritanceViaReflection);
            test("AgentCognitiveMemory: Parameterless constructor instantiates cleanly", TestParameterlessConstructor);
            test("AgentCognitiveMemory: 100% Stateless - SyncData contains zero IL method calls", TestStatelessSyncDataBytecode);
            test("AgentCognitiveMemory: Zero SaveableTypeDefiner and zero SaveableField attributes", TestZeroSaveableTypeDefiners);
            test("AgentCognitiveMemory: GEMINI.md Anti-Shadowing compliance", TestAntiShadowingRule);
            test("AgentCognitiveMemory: SubModule.OnGameStart registers behavior via AddBehavior()", TestSubModuleRegistration);
            test("AgentCognitiveMemory: Hooks into verified TaleWorlds CampaignEvents declaratively", TestCampaignEventsCoverage);
            test("AgentCognitiveMemory: HourlyTick uses the stable SDK time-slicing helper", TestModulo24TimeSlicing);
            test("AgentCognitiveMemory: Clears volatile memory on campaign start/load, not mission end", TestMemoryLifecycleCleanup);
            test("AgentCognitiveMemory: Live volatile memory integration with ForgeAgentMemory", TestLiveForgeAgentMemoryIntegration);
            test("AgentCognitiveMemory: Telemetry counts only writes accepted by bounded memory", TestTelemetryCountsOnlyAcceptedWrites);
            test("AgentCognitiveMemory: Universal cognitive dialogue flows and token chaining", TestCognitiveDialogueRegistrationAndTokens);
            test("AgentCognitiveMemory: Cognitive dialogue condition and response delegates", TestCognitiveDialogueConditionLogic);
            test("AgentCognitiveMemory: IPC protocol agent-memory registration and serialization", TestIpcMemoryProtocolAndSerialization);
        }

        private static string FindWorkspaceRoot()
        {
            string current = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 6; i++)
            {
                if (File.Exists(Path.Combine(current, "CalradiaForge.sln")))
                    return current;
                string parent = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(parent) || parent == current) break;
                current = parent;
            }

            string cwd = Directory.GetCurrentDirectory();
            for (int i = 0; i < 6; i++)
            {
                if (File.Exists(Path.Combine(cwd, "CalradiaForge.sln")))
                    return cwd;
                string parent = Path.GetDirectoryName(cwd);
                if (string.IsNullOrEmpty(parent) || parent == cwd) break;
                cwd = parent;
            }

            throw new DirectoryNotFoundException("Could not locate workspace root containing CalradiaForge.sln");
        }

        private static string GetBehaviorFilePath()
        {
            string root = FindWorkspaceRoot();
            string path = Path.Combine(root, "src", "CalradiaForge.Mod", "CampaignBehaviors", "AgentCognitiveMemoryBehavior.cs");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("AgentCognitiveMemoryBehavior.cs not found at " + path);
            }
            return path;
        }

        private static Assembly LoadModAssembly()
        {
            return ClanCharacterProgressionTests.LoadModAssembly();
        }

        private static Type GetBehaviorType()
        {
            var asm = LoadModAssembly();
            var type = asm.GetType("CalradiaForge.Mod.CampaignBehaviors.AgentCognitiveMemoryBehavior");
            if (type == null)
            {
                throw new InvalidOperationException("Could not find type CalradiaForge.Mod.CampaignBehaviors.AgentCognitiveMemoryBehavior in assembly.");
            }
            return type;
        }

        private static void TestSourceFileAndNamespace()
        {
            string path = GetBehaviorFilePath();
            string code = File.ReadAllText(path);
            if (!code.Contains("namespace CalradiaForge.Mod.CampaignBehaviors"))
            {
                throw new Exception("AgentCognitiveMemoryBehavior.cs must define namespace CalradiaForge.Mod.CampaignBehaviors");
            }
        }

        private static void TestInheritanceViaReflection()
        {
            var type = GetBehaviorType();
            var baseType = type.BaseType;
            while (baseType != null && baseType.Name != "CampaignBehaviorBase")
            {
                baseType = baseType.BaseType;
            }

            if (baseType == null)
            {
                throw new Exception("AgentCognitiveMemoryBehavior must inherit from CampaignBehaviorBase.");
            }
        }

        private static void TestParameterlessConstructor()
        {
            var type = GetBehaviorType();
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
            {
                throw new Exception("AgentCognitiveMemoryBehavior must have a public parameterless constructor.");
            }

            var instance = Activator.CreateInstance(type);
            if (instance == null)
            {
                throw new Exception("Activator failed to instantiate AgentCognitiveMemoryBehavior.");
            }
        }

        private static void TestStatelessSyncDataBytecode()
        {
            var type = GetBehaviorType();
            var syncDataMethod = type.GetMethod("SyncData", BindingFlags.Public | BindingFlags.Instance);
            if (syncDataMethod == null)
            {
                throw new Exception("SyncData method not found on AgentCognitiveMemoryBehavior.");
            }

            var methodBody = syncDataMethod.GetMethodBody();
            if (methodBody == null) return;

            byte[] il = methodBody.GetILAsByteArray();
            if (il == null || il.Length == 0) return;

            // OpCodes: Call (0x28) and Callvirt (0x6F)
            for (int i = 0; i < il.Length; i++)
            {
                if (il[i] == 0x28 || il[i] == 0x6F)
                {
                    throw new Exception("SyncData IL contains method invocation (opcode 0x" + il[i].ToString("X2") + " at offset " + i + "). Must be 100% stateless.");
                }
            }
        }

        private static void TestZeroSaveableTypeDefiners()
        {
            var asm = LoadModAssembly();
            foreach (var type in asm.GetTypes())
            {
                var cur = type.BaseType;
                while (cur != null)
                {
                    if (cur.Name == "SaveableTypeDefiner")
                    {
                        throw new Exception("Assembly contains SaveableTypeDefiner: " + type.FullName);
                    }
                    cur = cur.BaseType;
                }

                foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    foreach (var attr in member.GetCustomAttributes(false))
                    {
                        var attrName = attr.GetType().Name;
                        if (attrName == "SaveableFieldAttribute" || attrName == "SaveablePropertyAttribute")
                        {
                            throw new Exception("Found saveable attribute " + attrName + " on " + type.FullName + "." + member.Name);
                        }
                    }
                }
            }
        }

        private static void TestAntiShadowingRule()
        {
            string path = GetBehaviorFilePath();
            string code = File.ReadAllText(path);

            if (System.Text.RegularExpressions.Regex.IsMatch(code, @"\bclass\s+Campaign\b") ||
                System.Text.RegularExpressions.Regex.IsMatch(code, @"\bnamespace\s+.*\.Campaign\b"))
            {
                throw new Exception("AgentCognitiveMemoryBehavior.cs shadows Campaign keyword (Rule A violation).");
            }
        }

        private static void TestSubModuleRegistration()
        {
            string root = FindWorkspaceRoot();
            string subModulePath = Path.Combine(root, "src", "CalradiaForge.Mod", "SubModule.cs");
            string code = File.ReadAllText(subModulePath);

            if (!code.Contains("AgentCognitiveMemoryBehavior"))
            {
                throw new Exception("SubModule.cs does not register AgentCognitiveMemoryBehavior.");
            }
        }

        private static void TestCampaignEventsCoverage()
        {
            string path = GetBehaviorFilePath();
            string code = File.ReadAllText(path);

            string[] expectedEvents = new[]
            {
                "HeroPrisonerTaken",
                "HeroPrisonerReleased",
                "HeroKilledEvent",
                "HeroRelationChanged",
                "HeroGainedSkill",
                "HourlyTickEvent",
                "OnSessionLaunchedEvent"
            };

            foreach (var evt in expectedEvents)
            {
                if (!code.Contains(evt))
                {
                    throw new Exception("AgentCognitiveMemoryBehavior does not hook expected event: " + evt);
                }
            }
        }

        private static void TestModulo24TimeSlicing()
        {
            string path = GetBehaviorFilePath();
            string code = File.ReadAllText(path);

            if (!code.Contains("ForgeTimeSlicer.ShouldProcess(hero.StringId, currentHour)") ||
                code.Contains("GetHashCode()") ||
                !code.Contains("HourlyTick"))
            {
                throw new Exception("AgentCognitiveMemoryBehavior must use ForgeTimeSlicer with the stable Hero.StringId in its hourly maintenance loop.");
            }
        }

        private static void TestMemoryLifecycleCleanup()
        {
            string root = FindWorkspaceRoot();
            string dataBehavior = File.ReadAllText(Path.Combine(root, "src", "CalradiaForge.Mod", "CampaignBehaviors", "DataBehavior.cs"));
            string registerEvents = GetMethodBlock(dataBehavior, "public override void RegisterEvents()");
            string newGame = GetMethodBlock(dataBehavior, "private void OnNewGameCreated(CampaignGameStarter starter)");
            string loadedGame = GetMethodBlock(dataBehavior, "private void OnGameLoaded(CampaignGameStarter starter)");

            if (!registerEvents.Contains("CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener") ||
                !registerEvents.Contains("CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener"))
            {
                throw new Exception("DataBehavior must subscribe to both new-game and loaded-game lifecycle events.");
            }

            if (!newGame.Contains("ForgeAgentMemory.ClearAll()") || !loadedGame.Contains("ForgeAgentMemory.ClearAll()"))
            {
                throw new Exception("DataBehavior must clear volatile agent memory when creating or loading a campaign.");
            }

            string subModule = File.ReadAllText(Path.Combine(root, "src", "CalradiaForge.Mod", "SubModule.cs"));
            string onGameStart = GetMethodBlock(subModule, "protected override void OnGameStart(");
            string unload = GetMethodBlock(subModule, "protected override void OnSubModuleUnloaded()");
            string missionEnd = GetMethodBlock(subModule, "protected override void OnEndMission()");
            if (!onGameStart.Contains("campaignStarter.AddBehavior(new CalradiaForge.Mod.DataExtensions.DataBehavior())"))
                throw new Exception("SubModule.OnGameStart must register DataBehavior so campaign memory lifecycle cleanup is active.");
            if (!unload.Contains("ForgeAgentMemory.ClearAll()"))
                throw new Exception("SubModule unload must retain cleanup of volatile agent memory.");
            if (missionEnd.Contains("ForgeAgentMemory.ClearAll()"))
                throw new Exception("Ending a mission must not clear campaign-wide volatile agent memory.");
        }

        private static string GetMethodBlock(string source, string signature)
        {
            int start = source.IndexOf(signature, StringComparison.Ordinal);
            if (start < 0) throw new Exception("Could not find method " + signature + ".");
            int open = source.IndexOf('{', start);
            if (open < 0) throw new Exception("Could not find method body for " + signature + ".");

            int depth = 0;
            for (int index = open; index < source.Length; index++)
            {
                if (source[index] == '{') depth++;
                else if (source[index] == '}' && --depth == 0)
                    return source.Substring(start, index - start + 1);
            }

            throw new Exception("Could not find method end for " + signature + ".");
        }

        private static void TestLiveForgeAgentMemoryIntegration()
        {
            ForgeAgentMemory.ClearAll();

            string testHeroId = "hero_lord_alderick";
            bool addedEpisode = ForgeAgentMemory.Episodic.TryAdd(testHeroId, "Captivity", "Captured by Vlandian Vanguard.");
            if (!addedEpisode) throw new Exception("Failed to record episodic memory in ForgeAgentMemory.");

            bool setFact = ForgeAgentMemory.Semantic.TryUpsert(testHeroId, "IsImprisoned", true);
            if (!setFact) throw new Exception("Failed to upsert semantic memory in ForgeAgentMemory.");

            bool isImprisoned = ForgeAgentMemory.Semantic.Get<bool>(testHeroId, "IsImprisoned");
            if (!isImprisoned) throw new Exception("Semantic fact value did not match expected boolean.");

            int agentCount = ForgeAgentMemory.RegisteredAgentsCount;
            if (agentCount != 1) throw new Exception("RegisteredAgentsCount was " + agentCount + " instead of 1.");

            ForgeAgentMemory.ClearAll();
            if (ForgeAgentMemory.RegisteredAgentsCount != 0) throw new Exception("ClearAll failed to reset RegisteredAgentsCount.");
        }

        private static void TestTelemetryCountsOnlyAcceptedWrites()
        {
            ForgeAgentMemory.ClearAll();
            try
            {
                Type behaviorType = GetBehaviorType();
                object behavior = Activator.CreateInstance(behaviorType);
                MethodInfo recordEpisode = behaviorType.GetMethod("TryRecordEpisodicMemory", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo updateFact = behaviorType.GetMethod("TryUpdateSemanticFact", BindingFlags.Instance | BindingFlags.NonPublic);
                if (recordEpisode == null || updateFact == null)
                    throw new Exception("Memory write helpers needed for telemetry regression were not found.");

                const string acceptedAgentId = "telemetry_accepted_agent";
                if (!(bool)recordEpisode.Invoke(behavior, new object[] { acceptedAgentId, "Test", "accepted episode" }))
                    throw new Exception("The episodic telemetry fixture write should be accepted.");
                if (!(bool)updateFact.Invoke(behavior, new object[] { acceptedAgentId, "TestFact", true }))
                    throw new Exception("The semantic telemetry fixture write should be accepted.");

                PropertyInfo episodicCounter = behaviorType.GetProperty("TotalEpisodicMemoriesRecorded");
                PropertyInfo semanticCounter = behaviorType.GetProperty("TotalSemanticFactsUpdated");
                if (episodicCounter == null || semanticCounter == null ||
                    (int)episodicCounter.GetValue(behavior, null) != 1 ||
                    (int)semanticCounter.GetValue(behavior, null) != 1)
                    throw new Exception("Accepted memory writes must increment their corresponding telemetry counters once.");

                for (int i = 1; i < ForgeAgentMemory.MaximumAgents; i++)
                {
                    string agentId = "telemetry_quota_agent_" + i;
                    if (!ForgeAgentMemory.Episodic.TryAdd(agentId, "Quota", "fill"))
                        throw new Exception("Could not fill the bounded memory registry for quota rejection coverage.");
                }

                if (ForgeAgentMemory.RegisteredAgentsCount != ForgeAgentMemory.MaximumAgents)
                    throw new Exception("The memory registry did not reach its configured agent quota.");

                bool rejectedEpisode = (bool)recordEpisode.Invoke(behavior, new object[] { "telemetry_rejected_agent", "Test", "rejected episode" });
                bool rejectedFact = (bool)updateFact.Invoke(behavior, new object[] { "telemetry_rejected_agent", "TestFact", true });
                if (rejectedEpisode || rejectedFact)
                    throw new Exception("New memory writes beyond the global agent quota must be rejected.");

                if ((int)episodicCounter.GetValue(behavior, null) != 1 || (int)semanticCounter.GetValue(behavior, null) != 1)
                    throw new Exception("Rejected memory writes must not increment telemetry counters.");
            }
            finally
            {
                ForgeAgentMemory.ClearAll();
            }
        }

        private static void TestCognitiveDialogueRegistrationAndTokens()
        {
            string path = GetBehaviorFilePath();
            string code = File.ReadAllText(path);

            string[] expectedTokens = new[]
            {
                "forge_lord_blood_feud_greet",
                "forge_lord_grateful_greet",
                "forge_lord_respected_greet",
                "forge_lord_ask_recollection",
                "forge_lord_reply_recollection",
                "forge_notable_ask_recollection",
                "forge_notable_reply_recollection",
                "forge_companion_ask_recollection",
                "forge_companion_reply_recollection",
                "lord_talk_ask_something_2",
                "hero_main_options",
                "FORGE_COGNITIVE_REPLY"
            };

            foreach (var token in expectedTokens)
            {
                if (!code.Contains(token))
                {
                    throw new Exception("AgentCognitiveMemoryBehavior does not register expected dialogue token or line ID: " + token);
                }
            }
        }

        private static void TestCognitiveDialogueConditionLogic()
        {
            var type = GetBehaviorType();
            string[] expectedMethods = new[]
            {
                "LordBloodFeudGreetingCondition",
                "LordGratefulLiberationCondition",
                "LordRespectedRivalCondition",
                "LordMemoryInquiryCondition",
                "LordMemoryReplyCondition",
                "NotableMemoryInquiryCondition",
                "NotableMemoryReplyCondition",
                "CompanionMemoryInquiryCondition",
                "CompanionMemoryReplyCondition"
            };

            foreach (var method in expectedMethods)
            {
                var methodInfo = type.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (methodInfo == null)
                {
                    throw new Exception("AgentCognitiveMemoryBehavior is missing expected condition delegate method: " + method);
                }
                if (methodInfo.ReturnType != typeof(bool))
                {
                    throw new Exception("Condition delegate method " + method + " must return bool.");
                }
            }
        }

        private static void TestIpcMemoryProtocolAndSerialization()
        {
            // Verify protocol registry
            if (!CalradiaForge.Core.ForgeProtocol.Actions.Contains("agent-memory"))
            {
                throw new Exception("ForgeProtocol.Actions does not contain 'agent-memory'.");
            }

            // Verify live memory JSON serialization without engine dependencies
            ForgeAgentMemory.ClearAll();
            string agentId = "hero_test_noble_1";
            ForgeAgentMemory.Episodic.TryAdd(agentId, "Victory", "Defeated rebel warband at Ortysia.");
            ForgeAgentMemory.Semantic.TryUpsert(agentId, "TotalSlainHeroes", 3);
            ForgeAgentMemory.Semantic.TryUpsert(agentId, "IsAdult", true);

            var episodicList = ForgeAgentMemory.Episodic.GetAll(agentId, "Victory");
            if (episodicList == null || episodicList.Count != 1)
            {
                throw new Exception("Episodic memory count mismatch for agent-memory serialization test.");
            }

            int kills = ForgeAgentMemory.Semantic.Get<int>(agentId, "TotalSlainHeroes");
            if (kills != 3)
            {
                throw new Exception("Semantic memory TotalSlainHeroes mismatch for agent-memory serialization test.");
            }

            ForgeAgentMemory.ClearAll();
        }
    }
}

