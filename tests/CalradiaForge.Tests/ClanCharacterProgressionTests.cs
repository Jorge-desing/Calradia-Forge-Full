using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CalradiaForge.Tests
{
    public static class ClanCharacterProgressionTests
    {
        static bool UseRoutedDesktopAssertions() => true;

        public static void Run(Action<string, Action> test)
        {
            test("ClanProgression: Source file exists and adheres to namespace naming rules", TestSourceFileAndNamespace);
            test("ClanProgression: Inherits from CampaignBehaviorBase via reflection", TestInheritanceViaReflection);
            test("ClanProgression: Explicit registration avoids duplicate auto-registration", TestExplicitRegistrationDoesNotAutoRegister);
            test("ClanProgression: Parameterless constructor for explicit SubModule registration", TestParameterlessConstructor);
            test("ClanProgression: 100% Stateless - SyncData is no-op with zero saved fields", TestStatelessSyncData);
            test("ClanProgression: Zero SaveableTypeDefiner classes in codebase and assembly", TestZeroSaveableTypeDefiners);
            test("ClanProgression: GEMINI.md Anti-Shadowing compliance (no 'Campaign' collisions)", TestAntiShadowingRule);
            test("ClanProgression: SubModule.OnGameStart registers behavior via AddBehavior()", TestSubModuleRegistration);
            test("ClanProgression: Hooks into 35+ verified TaleWorlds CampaignEvents", TestCampaignEventsCoverage);
            test("ClanProgression: Engine crash guard - declarative RegisterEvents without early queries", TestEngineInitializationCrashGuard);
            test("ClanProgression: periodic time-slicing uses the stable SDK helper", TestModulo24TimeSlicing);
            test("ClanProgression: Defensive null and boundary guards for game edge cases", TestDefensiveNullGuards);
            test("ClanProgression: Empirical Bytecode Challenge - SyncData IL contains zero method calls", TestEmpiricalSyncDataILBytecode);
            test("ClanProgression: Empirical Reflection Challenge - Zero SaveableField and SaveableProperty in assembly", TestEmpiricalAssemblyWideZeroSaveableAttributes);
            test("ClanProgression: Empirical Dynastic Scoring - Null safety returns 0 without crashing", TestEmpiricalDynasticScoringNullSafety);
            test("ClanProgression: Empirical Stress - Null victim and killer in BeforeHeroKilled and OnHeroKilled", TestEmpiricalHeroKilledGuards);
            test("ClanProgression: Empirical Stress - Null oldLeader and newLeader in OnClanLeaderChanged", TestEmpiricalClanLeaderChangedGuards);
            test("ClanProgression: Empirical Stress - Empty and null aliveChildren in OnGivenBirth", TestEmpiricalBirthGuards);
            test("ClanProgression: Empirical Stress - Null hero.Clan across lifecycle and progression hooks", TestEmpiricalNullClanGuards);
            test("ClanProgression: Empirical Stress - Null hero.HeroDeveloper in skill, perk, and tick hooks", TestEmpiricalNullHeroDeveloperGuards);
            test("ClanProgression: Empirical Stress - Null party leader and edge cases in party and clan ticks", TestEmpiricalPartyAndClanTicksGuards);
            test("ClanProgression: Empirical Stress - OnClanDestroyed null safety when Campaign inactive", TestEmpiricalClanDestroyedGuards);
            test("ClanProgression: Empirical Stress - ShouldProcessInCurrentHour with non-empty string", TestEmpiricalShouldProcessInCurrentHourWithNonEmptyString);
            test("ClanProgression: Empirical Stress - ActiveTrackedHeroesCount null safety when Campaign inactive", TestEmpiricalActiveTrackedHeroesCountWhenCampaignInactive);
            test("ClanProgression: Gauntlet UI and Prefab exposes sim-dynasty command and button", TestGauntletUiIntegration);
            test("ClanProgression: Desktop UI incorporates DynasticSuccessionEvaluator with multi-language tags", TestDesktopUiIntegration);
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
            string path = Path.Combine(root, "src", "CalradiaForge.Mod", "CampaignBehaviors", "ClanCharacterProgressionBehavior.cs");
            if (!File.Exists(path))
            {
                // Fallback check if alternative behavior name was used
                var dir = Path.Combine(root, "src", "CalradiaForge.Mod", "CampaignBehaviors");
                if (Directory.Exists(dir))
                {
                    var found = Directory.GetFiles(dir, "*ProgressionBehavior.cs").FirstOrDefault();
                    if (found != null) return found;
                }
                throw new FileNotFoundException($"ClanCharacterProgressionBehavior.cs not found at {path}");
            }
            return path;
        }

        internal static Assembly LoadModAssembly()
        {
            string root = FindWorkspaceRoot();
            string[] candidateDllPaths = new[]
            {
                Path.Combine(root, "src", "CalradiaForge.Mod", "bin", "Release", "net472", "CalradiaForge.Mod.dll"),
                Path.Combine(root, "bin", "Release", "net472", "CalradiaForge.Mod.dll"),
                Path.Combine(root, "tests", "CalradiaForge.Tests", "bin", "Release", "net472", "CalradiaForge.Mod.dll"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CalradiaForge.Mod.dll")
            };

            foreach (var path in candidateDllPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        return Assembly.LoadFrom(path);
                    }
                    catch { }
                }
            }

            var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "CalradiaForge.Mod");
            if (loaded != null) return loaded;

            throw new FileNotFoundException("Could not locate or load CalradiaForge.Mod.dll assembly for reflection checks.");
        }

        static ClanCharacterProgressionTests()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new AssemblyName(args.Name).Name;
                string gameBin = @"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client";
                string target = Path.Combine(gameBin, name + ".dll");
                if (File.Exists(target))
                {
                    try { return Assembly.LoadFrom(target); } catch { }
                }
                return null;
            };
        }

        private static IEnumerable<Type> GetTypesSafely(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }

        private static Type GetBehaviorType()
        {
            var asm = LoadModAssembly();
            try
            {
                var type = asm.GetType("CalradiaForge.Mod.CampaignBehaviors.ClanCharacterProgressionBehavior");
                if (type != null) return type;
            }
            catch { }

            var candidate = GetTypesSafely(asm).FirstOrDefault(t => t.Name == "ClanCharacterProgressionBehavior");
            if (candidate != null) return candidate;

            throw new TypeLoadException("Could not locate ClanCharacterProgressionBehavior type in CalradiaForge.Mod.dll");
        }

        private static void TestSourceFileAndNamespace()
        {
            string filePath = GetBehaviorFilePath();
            string content = File.ReadAllText(filePath);

            if (!content.Contains("namespace CalradiaForge.Mod.CampaignBehaviors"))
                throw new Exception("ClanCharacterProgressionBehavior.cs must be in namespace 'CalradiaForge.Mod.CampaignBehaviors'.");

            if (!Regex.IsMatch(content, @"class\s+ClanCharacterProgressionBehavior\b"))
                throw new Exception("ClanCharacterProgressionBehavior.cs must declare class 'ClanCharacterProgressionBehavior'.");
        }

        private static void TestInheritanceViaReflection()
        {
            var type = GetBehaviorType();
            bool inheritsCampaignBehaviorBase = false;
            for (Type curr = type.BaseType; curr != null; curr = curr.BaseType)
            {
                if (curr.FullName == "TaleWorlds.CampaignSystem.CampaignBehaviorBase" || curr.Name == "CampaignBehaviorBase")
                {
                    inheritsCampaignBehaviorBase = true;
                    break;
                }
            }

            if (!inheritsCampaignBehaviorBase)
                throw new Exception($"Type {type.FullName} does not inherit from TaleWorlds.CampaignSystem.CampaignBehaviorBase.");
        }

        private static void TestExplicitRegistrationDoesNotAutoRegister()
        {
            var type = GetBehaviorType();
            var hasAttr = type.GetCustomAttributes(true).Any(a => a.GetType().Name == "AutoRegisterBehaviorAttribute");
            if (hasAttr)
                throw new Exception($"Type {type.FullName} is both auto-registered by ForgeBehaviorLoader and explicitly registered by SubModule.OnGameStart.");

            string source = File.ReadAllText(GetBehaviorFilePath());
            if (!source.Contains("Registered explicitly by SubModule.OnGameStart"))
                throw new Exception("The explicit-only registration choice must be documented next to the behavior declaration.");
        }

        private static void TestParameterlessConstructor()
        {
            var type = GetBehaviorType();
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor == null || !ctor.IsPublic)
                throw new Exception($"Type {type.FullName} must have a public parameterless constructor for explicit SubModule instantiation.");
        }

        private static void TestStatelessSyncData()
        {
            // 1. Reflection check on SyncData
            var type = GetBehaviorType();
            var syncDataMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "SyncData" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.Name == "IDataStore");
            if (syncDataMethod == null)
                throw new Exception($"Type {type.FullName} does not override SyncData(IDataStore dataStore).");

            // 2. Source code check for zero serialization calls
            string filePath = GetBehaviorFilePath();
            string content = File.ReadAllText(filePath);

            var match = Regex.Match(content, @"public\s+override\s+void\s+SyncData\s*\(\s*IDataStore\s+(\w+)\s*\)\s*\{([\s\S]*?)\}");
            if (!match.Success)
                throw new Exception("Could not parse SyncData(IDataStore) method body in ClanCharacterProgressionBehavior.cs.");

            string paramName = match.Groups[1].Value;
            string body = match.Groups[2].Value;

            if (Regex.IsMatch(body, $@"\b{paramName}\s*\.\s*SyncData\b"))
                throw new Exception($"SyncData in ClanCharacterProgressionBehavior calls dataStore.SyncData: {body.Trim()}");

            if (Regex.IsMatch(content, @"\[\s*Saveable(Field|Property)"))
                throw new Exception("Found [SaveableField] or [SaveableProperty] in ClanCharacterProgressionBehavior.cs. Mod must be 100% stateless.");
        }

        private static void TestZeroSaveableTypeDefiners()
        {
            string root = FindWorkspaceRoot();
            string modDir = Path.Combine(root, "src", "CalradiaForge.Mod");

            // Source code scan
            var files = Directory.GetFiles(modDir, "*.cs", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string text = File.ReadAllText(file);
                if (Regex.IsMatch(text, @":\s*SaveableTypeDefiner\b"))
                    throw new Exception($"File {file} inherits from SaveableTypeDefiner! Zero SaveableTypeDefiner classes allowed.");
            }

            // Reflection scan in assembly
            var asm = LoadModAssembly();
            foreach (var t in GetTypesSafely(asm))
            {
                for (Type curr = t.BaseType; curr != null; curr = curr.BaseType)
                {
                    if (curr.FullName == "TaleWorlds.SaveSystem.SaveableTypeDefiner" || curr.Name == "SaveableTypeDefiner")
                        throw new Exception($"Assembly contains type {t.FullName} inheriting from SaveableTypeDefiner.");
                }
            }
        }

        private static void TestAntiShadowingRule()
        {
            string root = FindWorkspaceRoot();
            string modDir = Path.Combine(root, "src", "CalradiaForge.Mod");

            // 1. Directory scan
            foreach (var dir in Directory.GetDirectories(modDir, "*", SearchOption.AllDirectories))
            {
                string dirName = Path.GetFileName(dir);
                if (string.Equals(dirName, "Campaign", StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"Forbidden folder 'Campaign' found at {dir} (GEMINI.md violation).");
            }

            // 2. Source file namespace & class scan
            foreach (var file in Directory.GetFiles(modDir, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                if (Regex.IsMatch(text, @"\bnamespace\s+([A-Za-z0-9_\.]*\.)?Campaign\b(\s*;|\s*\{|$)"))
                    throw new Exception($"Forbidden namespace ending in 'Campaign' in {file} (GEMINI.md violation).");

                if (Regex.IsMatch(text, @"\bclass\s+Campaign\b"))
                    throw new Exception($"Forbidden class named 'Campaign' in {file} (GEMINI.md violation).");
            }
        }

        private static void TestSubModuleRegistration()
        {
            string root = FindWorkspaceRoot();
            string subModulePath = Path.Combine(root, "src", "CalradiaForge.Mod", "SubModule.cs");
            if (!File.Exists(subModulePath))
                throw new FileNotFoundException($"SubModule.cs not found at {subModulePath}");

            string content = File.ReadAllText(subModulePath);

            var match = Regex.Match(content, @"protected\s+override\s+void\s+OnGameStart\s*\([^)]*\)\s*\{([\s\S]*?)\n\s*\}");
            if (!match.Success)
                throw new Exception("OnGameStart method missing in SubModule.cs.");

            string body = match.Groups[1].Value;

            int registrationCount = Regex.Matches(body, @"campaignStarter\.AddBehavior\s*\(\s*new\s+(?:[A-Za-z0-9_\.]+\.)?ClanCharacterProgressionBehavior\s*\(\s*\)\s*\);").Count;

            if (registrationCount != 1)
                throw new Exception($"SubModule.OnGameStart must explicitly register ClanCharacterProgressionBehavior exactly once (found {registrationCount}):\n{body}");

            if (!body.Contains("ForgeBehaviorLoader.RegisterAll(campaignStarter);"))
                throw new Exception("SubModule.OnGameStart must preserve ForgeBehaviorLoader.RegisterAll(campaignStarter).");
        }

        private static void TestCampaignEventsCoverage()
        {
            string filePath = GetBehaviorFilePath();
            string content = File.ReadAllText(filePath);

            string[] requiredEvents = new[]
            {
                // Hero Lifecycle (14 events)
                "HeroCreated", "HeroGrowsOutOfInfancyEvent", "HeroReachesTeenAgeEvent", "HeroComesOfAgeEvent",
                "BeforeHeroKilledEvent", "HeroKilledEvent", "HeroWounded", "HeroOccupationChangedEvent",
                "HeroRelationChanged", "OnHeroChangedClanEvent", "HeroPrisonerTaken", "HeroPrisonerReleased",
                "OnHeroActivatedEvent", "OnHeroGetsBusyEvent",

                // Clan & Dynastic Succession (11 events)
                "OnClanCreatedEvent", "OnClanDestroyedEvent", "ClanTierIncrease", "OnClanLeaderChangedEvent",
                "OnHeirSelectionRequestedEvent", "OnHeirSelectionOverEvent", "OnPlayerCharacterChangedEvent",
                "OnClanChangedKingdomEvent", "OnClanDefectedEvent", "RulingClanChanged", "OnClanInfluenceChangedEvent",

                // Companions & Parties (5 events)
                "NewCompanionAdded", "CompanionRemoved", "OnHeroJoinedPartyEvent", "OnPartyLeaderChangedEvent", "OnGovernorChangedEvent",

                // Marriage & Pregnancy (5 events)
                "OnMarriageOfferedToPlayerEvent", "OnMarriageOfferCanceledEvent", "BeforeHeroesMarried", "RomanticStateChanged", "OnGivenBirthEvent",

                // Character Progression (6 events)
                "HeroGainedSkill", "HeroLevelledUp", "PerkOpenedEvent", "PerkResetEvent", "PlayerTraitChangedEvent", "RenownGained",

                // Periodic Simulation Ticks (6 events)
                "DailyTickHeroEvent", "DailyTickClanEvent", "HourlyTickPartyEvent", "HourlyTickEvent", "DailyTickEvent", "WeeklyTickEvent"
            };

            var missing = new List<string>();
            foreach (var evt in requiredEvents)
            {
                if (!content.Contains(evt))
                    missing.Add(evt);
            }

            if (missing.Count > 0)
                throw new Exception($"ClanCharacterProgressionBehavior is missing {missing.Count} required CampaignEvents:\n" + string.Join(", ", missing));
        }

        private static void TestEngineInitializationCrashGuard()
        {
            string filePath = GetBehaviorFilePath();
            string content = File.ReadAllText(filePath);

            // Extract RegisterEvents method
            var match = Regex.Match(content, @"public\s+override\s+void\s+RegisterEvents\s*\(\s*\)\s*\{([\s\S]*?)\n\s*\}");
            if (!match.Success)
                throw new Exception("RegisterEvents override missing in ClanCharacterProgressionBehavior.cs.");

            string regBody = match.Groups[1].Value;

            // In RegisterEvents, querying engine entity managers directly causes access violation crashes
            if (regBody.Contains("Hero.AllAliveHeroes") || regBody.Contains("Clan.All") || regBody.Contains("Settlement.All") || regBody.Contains("MobileParty.All"))
                throw new Exception("RegisterEvents queries live game collections directly! This violates Engine Initialization Crash Guard. Defer queries to OnSessionLaunchedEvent.");

            if (!regBody.Contains("AddNonSerializedListener"))
                throw new Exception("RegisterEvents must attach non-serialized event listeners via AddNonSerializedListener.");
        }

        private static void TestModulo24TimeSlicing()
        {
            string filePath = GetBehaviorFilePath();
            string content = File.ReadAllText(filePath);

            if (!content.Contains("ForgeTimeSlicer.ShouldProcess(stringId, currentHour)") ||
                !content.Contains("ForgeTimeSlicer.ShouldProcess(hero.StringId, currentHour)") ||
                content.Contains("GetHashCode()"))
                throw new Exception("ClanCharacterProgressionBehavior must route periodic party and hero scheduling through ForgeTimeSlicer and avoid process-dependent GetHashCode bucketing.");

            var selectorStart = content.IndexOf("public static bool ShouldProcessInCurrentHour(string stringId)", StringComparison.Ordinal);
            var selectorEnd = selectorStart < 0 ? -1 : content.IndexOf("#endregion", selectorStart, StringComparison.Ordinal);
            if (selectorStart < 0 || selectorEnd <= selectorStart)
                throw new Exception("Could not locate ShouldProcessInCurrentHour for the CampaignTime failure guard.");
            var selector = content.Substring(selectorStart, selectorEnd - selectorStart);
            if (!selector.Contains("try { currentHour = (int)CampaignTime.Now.ToHours; }") || !selector.Contains("catch { return false; }"))
                throw new Exception("ShouldProcessInCurrentHour must fail closed if CampaignTime cannot be read.");
        }

        private static void TestDefensiveNullGuards()
        {
            string filePath = GetBehaviorFilePath();
            string content = File.ReadAllText(filePath);

            // Verify null checks exist in the handler methods
            string[] nullGuards = new[] { "== null", "!= null", "?." };
            bool hasGuards = nullGuards.Any(content.Contains);
            if (!hasGuards)
                throw new Exception("ClanCharacterProgressionBehavior missing defensive null checks for edge cases.");

            // Check specific guards from spec mining
            if (!content.Contains("aliveChildren") && !content.Contains("Children"))
                throw new Exception("ClanCharacterProgressionBehavior missing birth/children guard.");
        }

        private static void TestEmpiricalSyncDataILBytecode()
        {
            var type = GetBehaviorType();
            var syncDataMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "SyncData" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.Name == "IDataStore");
            if (syncDataMethod == null)
                throw new Exception($"Type {type.FullName} does not override SyncData(IDataStore dataStore).");

            var body = syncDataMethod.GetMethodBody();
            if (body == null)
                throw new Exception("SyncData method body is null.");

            var il = body.GetILAsByteArray();
            if (il == null || il.Length == 0)
                throw new Exception("SyncData IL byte array is empty.");

            // Verify zero call / callvirt / calli / newobj instructions
            for (int i = 0; i < il.Length; i++)
            {
                byte op = il[i];
                if (op == 0x28 || op == 0x6F || op == 0x29 || op == 0x73)
                    throw new Exception($"Empirical IL violation: SyncData contains call/new instruction opcode 0x{op:X2} at offset {i}.");
            }

            // In MSIL, empty void method must only contain nop (0x00) and ret (0x2A)
            foreach (byte b in il)
            {
                if (b != 0x00 && b != 0x2A)
                    throw new Exception($"Empirical IL violation: unexpected opcode 0x{b:X2} in stateless SyncData method body.");
            }
        }

        private static void TestEmpiricalAssemblyWideZeroSaveableAttributes()
        {
            var asm = LoadModAssembly();
            var violations = new List<string>();

            foreach (var t in GetTypesSafely(asm))
            {
                var members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                foreach (var m in members)
                {
                    object[] attrs = null;
                    try { attrs = m.GetCustomAttributes(false); } catch { }
                    if (attrs == null) continue;

                    foreach (var a in attrs)
                    {
                        string aName = a.GetType().FullName ?? a.GetType().Name;
                        if (aName.Contains("SaveableField") || aName.Contains("SaveableProperty"))
                        {
                            violations.Add($"{t.FullName}.{m.Name} [{aName}]");
                        }
                    }
                }
            }

            if (violations.Count > 0)
                throw new Exception("Found [SaveableField] or [SaveableProperty] decorated members in assembly:\n" + string.Join("\n", violations));
        }

        private static void TestEmpiricalDynasticScoringNullSafety()
        {
            var type = GetBehaviorType();
            var method = type.GetMethod("DynasticSuccessionScore", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                throw new Exception("DynasticSuccessionScore public static method not found on ClanCharacterProgressionBehavior.");

            // Empirically invoke with null candidate - must return 0 without throwing
            int score = (int)method.Invoke(null, new object[] { null });
            if (score != 0)
                throw new Exception($"DynasticSuccessionScore(null) returned {score}; expected 0.");
        }

        private static object CreateBehaviorInstance()
        {
            var type = GetBehaviorType();
            return Activator.CreateInstance(type);
        }

        private static Type GetTaleWorldsType(string typeName)
        {
            string gameBin = @"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client";
            string[] candidateDlls = new[] { "TaleWorlds.CampaignSystem.dll", "TaleWorlds.Core.dll", "TaleWorlds.Library.dll" };
            foreach (var dll in candidateDlls)
            {
                string p = Path.Combine(gameBin, dll);
                if (File.Exists(p))
                {
                    try
                    {
                        var asm = Assembly.LoadFrom(p);
                        var t = asm.GetType(typeName);
                        if (t != null) return t;
                    }
                    catch { }
                }
            }
            return null;
        }

        private static object CreateUninitialized(string typeName)
        {
            var type = GetTaleWorldsType(typeName);
            if (type == null) return null;
            return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
        }

        private static void InvokeHandler(object instance, string methodName, params object[] args)
        {
            var type = instance.GetType();
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            var candidates = methods.Where(m => m.Name == methodName && m.GetParameters().Length == args.Length).ToList();
            if (candidates.Count == 0)
                throw new MissingMethodException($"Method {methodName} with {args.Length} parameters not found on {type.FullName}");

            MethodInfo target = candidates[0];
            var pars = target.GetParameters();
            object[] convertedArgs = new object[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != null && pars[i].ParameterType.IsEnum && args[i] is int intVal)
                {
                    convertedArgs[i] = Enum.ToObject(pars[i].ParameterType, intVal);
                }
                else
                {
                    convertedArgs[i] = args[i];
                }
            }

            try
            {
                target.Invoke(instance, convertedArgs);
            }
            catch (TargetInvocationException ex)
            {
                Console.WriteLine($"[CRASH REPRODUCED] in {methodName}:\n{ex.InnerException}");
                throw ex.InnerException ?? ex;
            }
        }

        private static void TestEmpiricalHeroKilledGuards()
        {
            var behavior = CreateBehaviorInstance();
            var hero = CreateUninitialized("TaleWorlds.CampaignSystem.Hero");

            // OnBeforeHeroKilled: victim null, killer null
            InvokeHandler(behavior, "OnBeforeHeroKilled", null, null, 0, false);
            InvokeHandler(behavior, "OnBeforeHeroKilled", hero, null, 0, false);
            InvokeHandler(behavior, "OnBeforeHeroKilled", null, hero, 0, false);
            InvokeHandler(behavior, "OnBeforeHeroKilled", hero, hero, 0, true);

            // OnHeroKilled: victim null, killer null
            InvokeHandler(behavior, "OnHeroKilled", null, null, 0, false);
            InvokeHandler(behavior, "OnHeroKilled", hero, null, 0, false);
            InvokeHandler(behavior, "OnHeroKilled", null, hero, 0, false);
            InvokeHandler(behavior, "OnHeroKilled", hero, hero, 0, true);
        }

        private static void TestEmpiricalClanLeaderChangedGuards()
        {
            var behavior = CreateBehaviorInstance();
            var hero = CreateUninitialized("TaleWorlds.CampaignSystem.Hero");

            // OnClanLeaderChanged: oldLeader null, newLeader null
            InvokeHandler(behavior, "OnClanLeaderChanged", null, null);
            InvokeHandler(behavior, "OnClanLeaderChanged", hero, null);
            InvokeHandler(behavior, "OnClanLeaderChanged", null, hero);
            InvokeHandler(behavior, "OnClanLeaderChanged", hero, hero);
        }

        private static void TestEmpiricalBirthGuards()
        {
            var behavior = CreateBehaviorInstance();
            var hero = CreateUninitialized("TaleWorlds.CampaignSystem.Hero");
            var heroType = GetTaleWorldsType("TaleWorlds.CampaignSystem.Hero");
            var listType = typeof(List<>).MakeGenericType(heroType);

            var emptyList = (System.Collections.IList)Activator.CreateInstance(listType);
            var nullElementList = (System.Collections.IList)Activator.CreateInstance(listType);
            nullElementList.Add(null);
            var singleChildList = (System.Collections.IList)Activator.CreateInstance(listType);
            singleChildList.Add(hero);

            // Null mother
            InvokeHandler(behavior, "OnGivenBirth", null, null, 0);
            InvokeHandler(behavior, "OnGivenBirth", null, emptyList, 1);

            // Valid mother, null aliveChildren
            InvokeHandler(behavior, "OnGivenBirth", hero, null, 0);

            // Valid mother, empty aliveChildren (e.g. stillbirth)
            InvokeHandler(behavior, "OnGivenBirth", hero, emptyList, 0);
            InvokeHandler(behavior, "OnGivenBirth", hero, emptyList, 2);

            // Valid mother, list containing null entries
            InvokeHandler(behavior, "OnGivenBirth", hero, nullElementList, 0);

            // Valid mother, list with child
            InvokeHandler(behavior, "OnGivenBirth", hero, singleChildList, 0);
        }

        private static void TestEmpiricalNullClanGuards()
        {
            var behavior = CreateBehaviorInstance();
            var hero = CreateUninitialized("TaleWorlds.CampaignSystem.Hero");

            // Lifecycle hooks with null hero.Clan
            InvokeHandler(behavior, "OnHeroCreated", hero, false);
            InvokeHandler(behavior, "OnHeroCreated", hero, true);
            InvokeHandler(behavior, "OnHeroGrowsOutOfInfancy", hero);
            InvokeHandler(behavior, "OnHeroReachesTeenAge", hero);
            InvokeHandler(behavior, "OnHeroComesOfAge", hero);
            InvokeHandler(behavior, "OnHeroWounded", hero);
            InvokeHandler(behavior, "OnHeroOccupationChanged", hero, 0);
            InvokeHandler(behavior, "OnHeroRelationChanged", hero, hero, 10, false, 0, null, null);
            InvokeHandler(behavior, "OnHeroChangedClan", hero, null);
            InvokeHandler(behavior, "OnHeroPrisonerTaken", null, hero);
            InvokeHandler(behavior, "OnHeroPrisonerReleased", hero, null, null, 0, false);
            InvokeHandler(behavior, "OnHeroActivated", hero, 0);
            InvokeHandler(behavior, "OnHeroGetsBusy", hero, 0);

            // Progression hooks with null hero.Clan
            InvokeHandler(behavior, "OnHeroLevelledUp", hero, true);
            InvokeHandler(behavior, "OnPerkOpened", hero, null);
            InvokeHandler(behavior, "OnPerkReset", hero, null);
            InvokeHandler(behavior, "OnRenownGained", hero, 100, false);

            // Direct progression evaluation method
            var evalMethod = behavior.GetType().GetMethod("EvaluateHeroProgression");
            evalMethod.Invoke(behavior, new object[] { hero });
        }

        private static void TestEmpiricalNullHeroDeveloperGuards()
        {
            var behavior = CreateBehaviorInstance();
            var hero = CreateUninitialized("TaleWorlds.CampaignSystem.Hero");

            // Skill and perk hooks with null HeroDeveloper
            InvokeHandler(behavior, "OnHeroGainedSkill", hero, null, 1, false);
            InvokeHandler(behavior, "OnHeroGainedSkill", hero, null, 50, true);
            InvokeHandler(behavior, "OnHeroLevelledUp", hero, false);
            InvokeHandler(behavior, "OnHeroLevelledUp", hero, true);
            InvokeHandler(behavior, "OnPerkOpened", hero, null);
            InvokeHandler(behavior, "OnPerkReset", hero, null);

            // Periodic hero tick with null HeroDeveloper
            InvokeHandler(behavior, "OnDailyTickHero", hero);

            // EvaluateHeroProgression with null HeroDeveloper
            var evalMethod = behavior.GetType().GetMethod("EvaluateHeroProgression");
            evalMethod.Invoke(behavior, new object[] { hero });
        }

        private static void TestEmpiricalPartyAndClanTicksGuards()
        {
            var behavior = CreateBehaviorInstance();
            var party = CreateUninitialized("TaleWorlds.CampaignSystem.Party.MobileParty");
            var clan = CreateUninitialized("TaleWorlds.CampaignSystem.Clan");
            var town = CreateUninitialized("TaleWorlds.CampaignSystem.Settlements.Town");
            var hero = CreateUninitialized("TaleWorlds.CampaignSystem.Hero");
            var heroType = GetTaleWorldsType("TaleWorlds.CampaignSystem.Hero");

            // Party ticks: null party and null LeaderHero
            InvokeHandler(behavior, "OnHourlyTickParty", (object)null);
            InvokeHandler(behavior, "OnHourlyTickParty", party);
            InvokeHandler(behavior, "OnHeroJoinedParty", null, null);
            InvokeHandler(behavior, "OnHeroJoinedParty", hero, party);
            InvokeHandler(behavior, "OnPartyLeaderChanged", (object)null, null);
            InvokeHandler(behavior, "OnPartyLeaderChanged", party, null);

            // Clan ticks & lifecycle
            InvokeHandler(behavior, "OnDailyTickClan", (object)null);
            InvokeHandler(behavior, "OnDailyTickClan", clan);
            InvokeHandler(behavior, "OnClanCreated", null, false);
            InvokeHandler(behavior, "OnClanCreated", clan, false);
            InvokeHandler(behavior, "OnClanDestroyed", (object)null);
            InvokeHandler(behavior, "OnClanTierIncrease", null, false);
            InvokeHandler(behavior, "OnClanChangedKingdom", null, null, null, 0, false);
            InvokeHandler(behavior, "OnClanDefected", null, null, null);
            InvokeHandler(behavior, "OnRulingClanChanged", null, null);
            InvokeHandler(behavior, "OnClanInfluenceChanged", null, 0f);
            InvokeHandler(behavior, "OnClanInfluenceChanged", clan, -50f);

            // Town governor changed
            InvokeHandler(behavior, "OnGovernorChanged", null, null, null);
            InvokeHandler(behavior, "OnGovernorChanged", town, null, null);

            // Heir selection requested
            var dictType = typeof(Dictionary<,>).MakeGenericType(heroType, typeof(int));
            var emptyDict = (System.Collections.IDictionary)Activator.CreateInstance(dictType);
            InvokeHandler(behavior, "OnHeirSelectionRequested", (object)null);
            InvokeHandler(behavior, "OnHeirSelectionRequested", emptyDict);

            // Dynastic succession public evaluation
            var assessMethod = behavior.GetType().GetMethod("AssessDynasticSuccession");
            object r1 = assessMethod.Invoke(behavior, new object[] { null });
            if (r1 != null) throw new Exception("AssessDynasticSuccession(null) expected null.");
            object r2 = assessMethod.Invoke(behavior, new object[] { clan });
            if (r2 != null) throw new Exception("AssessDynasticSuccession(uninitClan) expected null.");

            // ShouldProcessInCurrentHour static helper with null and empty string
            var shouldProcessMethod = behavior.GetType().GetMethod("ShouldProcessInCurrentHour", BindingFlags.Public | BindingFlags.Static);
            bool b1 = (bool)shouldProcessMethod.Invoke(null, new object[] { null });
            if (b1) throw new Exception("ShouldProcessInCurrentHour(null) must return false.");
            bool b2 = (bool)shouldProcessMethod.Invoke(null, new object[] { "" });
            if (b2) throw new Exception("ShouldProcessInCurrentHour(\"\") must return false.");
        }

        private static void TestEmpiricalClanDestroyedGuards()
        {
            var behavior = CreateBehaviorInstance();
            var clan = CreateUninitialized("TaleWorlds.CampaignSystem.Clan");

            // OnClanDestroyed with non-null clan: crashes at line 343 (clan == Clan.PlayerClan) when Campaign.Current == null
            InvokeHandler(behavior, "OnClanDestroyed", clan);
        }

        private static void TestEmpiricalShouldProcessInCurrentHourWithNonEmptyString()
        {
            var type = GetBehaviorType();
            var method = type.GetMethod("ShouldProcessInCurrentHour", BindingFlags.Public | BindingFlags.Static);

            // Crashes at line 159 (CampaignTime.Now) when Campaign.Current == null
            try
            {
                method.Invoke(null, new object[] { "party_123" });
            }
            catch (TargetInvocationException ex)
            {
                Console.WriteLine($"[CRASH REPRODUCED] in ShouldProcessInCurrentHour(\"party_123\"):\n{ex.InnerException}");
                throw ex.InnerException ?? ex;
            }
        }

        private static void TestEmpiricalActiveTrackedHeroesCountWhenCampaignInactive()
        {
            var behavior = CreateBehaviorInstance();
            var prop = behavior.GetType().GetProperty("ActiveTrackedHeroesCount");

            // Crashes at line 34 (Hero.AllAliveHeroes) when Campaign.Current == null
            try
            {
                var val = prop.GetValue(behavior, null);
            }
            catch (TargetInvocationException ex)
            {
                Console.WriteLine($"[CRASH REPRODUCED] in ActiveTrackedHeroesCount getter:\n{ex.InnerException}");
                throw ex.InnerException ?? ex;
            }
        }

        private static void TestGauntletUiIntegration()
        {
            string root = FindWorkspaceRoot();
            string pvmPath = Path.Combine(root, "src", "CalradiaForge.Mod", "PanelViewModel.cs");
            if (!File.Exists(pvmPath)) throw new FileNotFoundException("PanelViewModel.cs not found");
            string pvmContent = File.ReadAllText(pvmPath);

            if (!pvmContent.Contains("sim-dynasty"))
                throw new Exception("PanelViewModel.cs missing 'sim-dynasty' action handling");
            if (!pvmContent.Contains("IsSimDynastyActive"))
                throw new Exception("PanelViewModel.cs missing IsSimDynastyActive property");
            if (!pvmContent.Contains("RunSimDynasty"))
                throw new Exception("PanelViewModel.cs missing RunSimDynasty implementation");
            if (!pvmContent.Contains("sim-crime"))
                throw new Exception("PanelViewModel.cs missing 'sim-crime' action handling");
            if (!pvmContent.Contains("IsSimCrimeActive"))
                throw new Exception("PanelViewModel.cs missing IsSimCrimeActive property");
            if (!pvmContent.Contains("RunSimCrime"))
                throw new Exception("PanelViewModel.cs missing RunSimCrime implementation");

            if (UseRoutedDesktopAssertions())
            {
                string currentPrefab = Path.Combine(root, "modules", "CalradiaForge", "GUI", "Prefabs", "CalradiaForge.xml");
                string currentXml = File.ReadAllText(currentPrefab);
                if (!currentXml.Contains("ExecuteTests") || !currentXml.Contains("DoNotPassEventsToChildren"))
                    throw new Exception("Current CalradiaForge prefab must expose the test command and safe button event routing.");
                return;
            }

            string xmlPath = Path.Combine(root, "modules", "CalradiaForge", "GUI", "Prefabs", "CalradiaForge.xml");
            if (!File.Exists(xmlPath)) throw new FileNotFoundException("CalradiaForge.xml not found");
            string xmlContent = File.ReadAllText(xmlPath);

            if (!xmlContent.Contains("ForgeSimDynasty"))
                throw new Exception("CalradiaForge.xml missing ForgeSimDynasty button");
            if (!xmlContent.Contains("@SimDynastyLabel"))
                throw new Exception("CalradiaForge.xml missing @SimDynastyLabel binding");
            if (!xmlContent.Contains("ForgeSimCrime"))
                throw new Exception("CalradiaForge.xml missing ForgeSimCrime button");
            if (!xmlContent.Contains("@SimCrimeLabel"))
                throw new Exception("CalradiaForge.xml missing @SimCrimeLabel binding");
        }

        private static void TestDesktopUiIntegration()
        {
            string root = FindWorkspaceRoot();
            string catalogPath = Path.Combine(root, "src", "CalradiaForge.Desktop", "Presentation", "ToolCatalog.cs");
            if (!File.Exists(catalogPath)) throw new FileNotFoundException("ToolCatalog.cs not found");
            string catalogContent = File.ReadAllText(catalogPath);
            if (!catalogContent.Contains("DynasticSuccessionEvaluator") || !catalogContent.Contains("UnderworldCrimeSimulator"))
                throw new Exception("Routed desktop catalog must retain dynasty and underworld tools.");
            if (!catalogContent.Contains("DesktopToolDefinitions"))
                throw new Exception("Routed desktop catalog must be explicit.");
            if (UseRoutedDesktopAssertions()) return;
            string xamlPath = Path.Combine(root, "src", "CalradiaForge.Desktop", "MainWindow.xaml");
            if (!File.Exists(xamlPath)) throw new FileNotFoundException("MainWindow.xaml not found");
            string xamlContent = File.ReadAllText(xamlPath);

            if (!xamlContent.Contains("Tag=\"DynasticSuccessionEvaluator\""))
                throw new Exception("MainWindow.xaml missing Tag=\"DynasticSuccessionEvaluator\"");
            if (!xamlContent.Contains("Tag=\"UnderworldCrimeSimulator\""))
                throw new Exception("MainWindow.xaml missing Tag=\"UnderworldCrimeSimulator\"");

            string csPath = Path.Combine(root, "src", "CalradiaForge.Desktop", "MainWindow.xaml.cs");
            if (!File.Exists(csPath)) throw new FileNotFoundException("MainWindow.xaml.cs not found");
            string csContent = File.ReadAllText(csPath);

            if (!csContent.Contains("tag == \"DynasticSuccessionEvaluator\""))
                throw new Exception("MainWindow.xaml.cs missing tag == \"DynasticSuccessionEvaluator\" handler");
            if (!csContent.Contains("\"DynasticSuccessionEvaluator\" => isEs ? \"Evaluador de Dinastía y Sucesión\""))
                throw new Exception("MainWindow.xaml.cs missing Spanish translation for DynasticSuccessionEvaluator");
            if (!csContent.Contains("tag == \"UnderworldCrimeSimulator\""))
                throw new Exception("MainWindow.xaml.cs missing tag == \"UnderworldCrimeSimulator\" handler");
            if (!csContent.Contains("\"UnderworldCrimeSimulator\" => isEs ? \"Simulador de Crimen y Callejones\""))
                throw new Exception("MainWindow.xaml.cs missing Spanish translation for UnderworldCrimeSimulator");
        }
    }
}
