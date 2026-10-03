using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using CalradiaForge.Core;
using CalradiaForge.Sdk;

namespace CalradiaForge.Tests
{
    /// <summary>Fixture coverage for analyzers that back visible Desktop audit commands.</summary>
    internal static class ForgeAnalysisTests
    {
        public static void Run(Action<string, Action> test)
        {
            test("Analyzer reports NotRun when no input is selected", NoInputIsNotRun);
            test("Unknown analyzer identifiers are not silently routed to XML", UnknownAnalyzerIsUnsupported);
            test("Desktop analyzer IDs use explicit Core mappings", DesktopAnalyzerAliases);
            test("Recursive source scans skip reparse points and stop at a depth bound", FileScanTraversal);
            test("Bounded directory-entry inspection preserves cancellation budget unreadable and reparse states", DirectoryEntryInspectionStates);
            test("Module analyzer distinguishes a valid manifest from a missing dependency", ModuleFixtures);
            test("XML analyzer reports malformed and DTD inputs with line evidence", XmlFixtures);
            test("Source analyzer reports real campaign and save rules", SourceFixtures);
            test("Source heuristics ignore comments and string literals while preserving line evidence", SourceLexerFixtures);
            test("Built-in analyzers stop before inspecting a pre-cancelled request", AnalyzerCancellationIsCooperative);
            test("Source analysis handles cancellation and per-file input failures", SourceCancellationAndInputFailures);
            test("Source tree harness timings are reported for fixed small and medium fixtures", SourceTreeHarnessTimings);
            test("Localization analysis reports per-file input failures and continues", LocalizationInputFailures);
            test("Localization analyzer reports encoding and missing source keys", LocalizationFixtures);
            test("Gauntlet analyzer reports decorations that can intercept input", GauntletFixtures);
            test("Asset analyzer reports audio troop and item structural errors", AssetFixtures);
            test("FBX preflight reports bounded ASCII names without importing or modifying assets", FbxFixtures);
            test("Asset analyzer checks sprite metadata and missing empty invalid and TPAC-marked packages", SpritePackageFixtures);
            test("Watchdog and crash analyzers retain actual metadata evidence", LogAndCrashFixtures);
            test("Archive analyzer rejects saves and TaleWorlds binaries", ArchiveFixtures);
            test("SDK analysis registry accepts read-only analyzers and rejects writer analyzers", AnalyzerRegistry);
            test("Unavailable runtime capabilities throw actionable errors", RuntimeCapabilities);
        }

        static void NoInputIsNotRun()
        {
            var result = ForgeAnalysisCatalog.Analyze("xml", new ForgeAnalysisRequest());
            Require(result.State == ForgeAnalysisState.NotRun, "No input must be NotRun.");
            Require(result.Provenance == ForgeAnalysisProvenance.Unavailable, "No input cannot claim evidence.");
        }

        static void UnknownAnalyzerIsUnsupported()
        {
            WithRoot(root =>
            {
                var input = Path.Combine(root, "input.xml");
                File.WriteAllText(input, "<root />");

                foreach (var analyzerId in new[] { "xml-validatr-typo", "assets-typo", "not-a-module", "prefix-fbx", "not-a-localization-tool" })
                {
                    var result = ForgeAnalysisCatalog.Analyze(analyzerId, Request(input));

                    Require(result.State == ForgeAnalysisState.Unsupported, "An unknown analyzer identifier must not be routed by a keyword embedded in it: " + analyzerId);
                    Require(result.Provenance == ForgeAnalysisProvenance.Unavailable, "An unknown analyzer cannot claim analyzed evidence: " + analyzerId);
                    Require(result.AnalyzerId == analyzerId, "The unsupported result must retain the normalized requested identifier: " + analyzerId);
                    Require(result.FilesExamined == 0 && result.Findings.Count == 0, "An unknown analyzer must not inspect the selected input: " + analyzerId);
                }
            });
        }

        static void DesktopAnalyzerAliases()
        {
            WithRoot(root =>
            {
                var input = Path.Combine(root, "input.xml");
                File.WriteAllText(input, "<root />");
                var aliases = new[]
                {
                    new[] { "SubModuleValidator", "module" },
                    new[] { "ModConflictMatrix", "module" },
                    new[] { "ModIdAuditor", "module" },
                    new[] { "XmlSchemaValidator", "xml" },
                    new[] { "WatchdogParser", "watchdog" },
                    new[] { "CrashAnalyzer", "crash" },
                    new[] { "ApiDeprecationChecker", "ApiDeprecationChecker" },
                    new[] { "PackagingAuditor", "archive" },
                    new[] { "MissionMeshGuard", "source" },
                    new[] { "SaveTypeDefinerAuditor", "source" },
                    new[] { "GauntletEventPassChecker", "gauntlet" },
                    new[] { "CampaignNamespaceGuard", "source" },
                    new[] { "AudioFmodMixerInspector", "assets" },
                    new[] { "SpritePackageAuditor", "assets" },
                    new[] { "FbxAsciiPreflight", "fbx" }
                };

                foreach (var alias in aliases)
                {
                    var result = ForgeAnalysisCatalog.Analyze(alias[0], Request(input));
                    Require(result.AnalyzerId == alias[1], "Desktop analyzer alias must map to its reviewed Core analyzer: " + alias[0]);
                    if (alias[0] == "ApiDeprecationChecker")
                    {
                        Require(result.State == ForgeAnalysisState.Unsupported && result.Provenance == ForgeAnalysisProvenance.Unavailable,
                            "The API deprecation route must remain explicit but unsupported without a verified catalog.");
                        Require(result.FilesExamined == 0, "An unavailable API deprecation check must not report a source scan as its result.");
                    }
                    else
                    {
                        Require(result.State != ForgeAnalysisState.Unsupported, "A reviewed Desktop analyzer ID must not be reported as unsupported: " + alias[0]);
                    }
                }
            });
        }

        static void FileScanTraversal()
        {
            WithRoot(root =>
            {
                var selected = Path.Combine(root, "selected");
                var linkedTarget = Path.Combine(root, "linked-target");
                var ordinary = Path.Combine(selected, "ordinary");
                Directory.CreateDirectory(ordinary);
                Directory.CreateDirectory(linkedTarget);
                File.WriteAllText(Path.Combine(ordinary, "safe.cs"), "internal sealed class SafeInput { }");
                File.WriteAllText(Path.Combine(linkedTarget, "escaped.cs"), "namespace Demo.Campaign { internal class Escaped { } }");

                var junction = Path.Combine(selected, "linked");
                Require(CreateJunction(junction, linkedTarget), "The Windows fixture must create a directory junction to test reparse-point exclusion.");
                try
                {
                    var linkedResult = ForgeAnalysisCatalog.Analyze("source", Request(selected));
                    Require(linkedResult.FilesExamined == 1, "The scan must inspect the ordinary nested source and must not follow the junction.");
                    Require(!linkedResult.Findings.Any(item => item.RuleId == "campaign_namespace_shadowing"), "A source file reached only through a junction must remain outside the selected tree.");
                    Require(linkedResult.Truncated && linkedResult.Findings.Any(item => item.RuleId == "analysis_scan_incomplete"), "Skipping a reparse point must disclose incomplete tree coverage.");
                }
                finally
                {
                    if (Directory.Exists(junction)) Directory.Delete(junction, false);
                }

                var deepRoot = Path.Combine(root, "deep");
                var deepDirectory = deepRoot;
                Directory.CreateDirectory(deepDirectory);
                for (var index = 0; index < 70; index++)
                {
                    deepDirectory = Path.Combine(deepDirectory, "d");
                    Directory.CreateDirectory(deepDirectory);
                }
                File.WriteAllText(Path.Combine(deepDirectory, "too-deep.cs"), "namespace Demo.Campaign { internal class TooDeep { } }");

                var deepResult = ForgeAnalysisCatalog.Analyze("source", Request(deepRoot));
                Require(deepResult.FilesExamined == 0, "The scan must not process files beyond its maximum directory depth.");
                Require(deepResult.Truncated && deepResult.Findings.Any(item => item.RuleId == "analysis_scan_incomplete"), "A depth-limited scan must report that coverage is incomplete.");
            });
        }

        static bool CreateJunction(string linkPath, string targetPath)
        {
            var start = new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
                Arguments = "/c mklink /J \"" + linkPath + "\" \"" + targetPath + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using (var process = Process.Start(start))
            {
                if (process == null) return false;
                if (!process.WaitForExit(5000))
                {
                    process.Kill();
                    if (Directory.Exists(linkPath)) Directory.Delete(linkPath, false);
                    return false;
                }
                if (process.ExitCode != 0 || !Directory.Exists(linkPath))
                {
                    if (Directory.Exists(linkPath)) Directory.Delete(linkPath, false);
                    return false;
                }
                var isReparsePoint = (File.GetAttributes(linkPath) & FileAttributes.ReparsePoint) != 0;
                if (!isReparsePoint) Directory.Delete(linkPath, false);
                return isReparsePoint;
            }
        }

        static void DirectoryEntryInspectionStates()
        {
            var cancellationCalls = 0;
            try
            {
                DirectoryEntryProbe.Inspect(Path.GetTempPath(), new CancellationToken(true), () =>
                {
                    cancellationCalls++;
                    return true;
                }, out _);
                throw new Exception("A cancelled directory-entry inspection must throw.");
            }
            catch (OperationCanceledException)
            {
                Require(cancellationCalls == 0, "Cancellation must be checked before consuming the entry budget.");
            }

            var missingPath = Path.Combine(Path.GetTempPath(), "CalradiaForge-entry-probe-" + Guid.NewGuid().ToString("N"));
            var budgetCalls = 0;
            var exhausted = DirectoryEntryProbe.Inspect(missingPath, CancellationToken.None, () =>
            {
                budgetCalls++;
                return false;
            }, out _);
            Require(exhausted == DirectoryEntryProbeResult.EntryLimitReached && budgetCalls == 1,
                "An exhausted budget must stop before filesystem attribute access.");

            var unreadable = DirectoryEntryProbe.Inspect(missingPath, CancellationToken.None, () => true, out _);
            Require(unreadable == DirectoryEntryProbeResult.Unreadable,
                "A path that disappears or cannot be read must retain the unreadable state.");

            var root = Path.Combine(Path.GetTempPath(), "CalradiaForge-entry-probe-" + Guid.NewGuid().ToString("N"));
            var target = Path.Combine(root, "target");
            var junction = Path.Combine(root, "junction");
            Directory.CreateDirectory(target);
            try
            {
                Require(CreateJunction(junction, target), "The Windows fixture must create a junction to characterize reparse-point inspection.");
                var reparse = DirectoryEntryProbe.Inspect(junction, CancellationToken.None, () => true, out var attributes);
                Require(reparse == DirectoryEntryProbeResult.ReparsePoint && (attributes & FileAttributes.ReparsePoint) != 0,
                    "A reparse point must be identified without being treated as an ordinary directory.");
            }
            finally
            {
                if (Directory.Exists(junction)) Directory.Delete(junction, false);
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        static void ModuleFixtures()
        {
            WithRoot(root =>
            {
                var valid = Path.Combine(root, "valid");
                Directory.CreateDirectory(Path.Combine(valid, "A"));
                File.WriteAllText(Path.Combine(valid, "A", "SubModule.xml"), "<Module><Id value='A'/><Version value='v1'/></Module>");
                var clean = ForgeAnalysisCatalog.Analyze("module manifest audit", Request(valid));
                Require(clean.State == ForgeAnalysisState.Completed && !clean.HasErrors, "Valid module must complete without error evidence.");

                var broken = Path.Combine(root, "broken");
                Directory.CreateDirectory(Path.Combine(broken, "A"));
                File.WriteAllText(Path.Combine(broken, "A", "SubModule.xml"), "<Module><Id value='A'/><Version value='v1'/><DependedModules><DependedModule Id='Missing'/></DependedModules></Module>");
                var finding = ForgeAnalysisCatalog.Analyze("module", Request(broken)).Findings.SingleOrDefault(item => item.RuleId == "dependency_missing");
                Require(finding != null && finding.SourcePath.EndsWith("SubModule.xml"), "Missing dependency must retain manifest evidence.");
            });
        }

        static void XmlFixtures()
        {
            WithRoot(root =>
            {
                var invalid = Path.Combine(root, "bad.xml");
                File.WriteAllText(invalid, "<root>");
                var malformed = ForgeAnalysisCatalog.Analyze("xml", Request(invalid));
                Require(malformed.Findings.Any(item => item.RuleId == "xml_invalid" && item.Line.HasValue), "Malformed XML must provide line evidence.");

                var dtd = Path.Combine(root, "dtd.xml");
                File.WriteAllText(dtd, "<!DOCTYPE root [<!ENTITY e SYSTEM 'file:///blocked'>]><root>&e;</root>");
                Require(ForgeAnalysisCatalog.Analyze("xml", Request(dtd)).Findings.Any(item => item.RuleId == "xml_invalid"), "DTD input must be rejected.");
            });
        }

        static void SourceFixtures()
        {
            WithRoot(root =>
            {
                var source = Path.Combine(root, "Unsafe.cs");
                File.WriteAllText(source, "namespace Demo.Campaign { class X : SaveableTypeDefiner { X() : base(42) {} } }");
                var result = ForgeAnalysisCatalog.Analyze("source API check", Request(source));
                Require(result.Findings.Any(item => item.RuleId == "campaign_namespace_shadowing"), "Campaign shadowing must be structural evidence.");
                Require(result.Findings.Any(item => item.RuleId == "save_id_range"), "Unsafe save range must be structural evidence.");
            });
        }

        static void SourceLexerFixtures()
        {
            WithRoot(root =>
            {
                var source = Path.Combine(root, "LexicalNoise.cs");
                var rawQuotes = new string('"', 3);
                var rawPrefix = new string('$', 2) + rawQuotes;
                var sourceText = string.Join(Environment.NewLine, new[]
                {
                    "// namespace Fake.Campaign; class Localization {} class Dummy : QuestBase { override void OnInit() { Mesh mesh; } }",
                    "/*",
                    "namespace Noise.Campaign; class Localization {}",
                    "*/",
                    "var regular = \"namespace Demo.Campaign; class Localization {} SaveableTypeDefiner(42) : QuestBase InitializeQuestOnGameLoad : MissionLogic override void OnInit() Mesh\";",
                    "var verbatim = @\"namespace Demo.Campaign; \"\"class Localization\"\";\";",
                    "var interpolated = $\"prefix {Format(\"namespace Demo.Campaign;\")} class Localization\";",
                    "var raw = " + rawQuotes + " namespace Demo.Campaign; class Localization {} SaveableTypeDefiner(42) " + rawQuotes + ";",
                    "var rawInterpolated = " + rawPrefix + " {{Format(\"namespace Demo.Campaign;\")}} class Localization " + rawQuotes + ";",
                    "namespace Real.Campaign { }"
                });
                File.WriteAllText(source, sourceText);

                var result = ForgeAnalysisCatalog.Analyze("source", Request(source));
                var campaignFinding = result.Findings.SingleOrDefault(item => item.RuleId == "campaign_namespace_shadowing");
                Require(campaignFinding != null && campaignFinding.Line == 10,
                    "Only the real campaign namespace should be reported, with its original line number after masked multiline text.");
                Require(!result.Findings.Any(item => item.RuleId == "localization_shadowing" || item.RuleId == "save_id_range" ||
                    item.RuleId == "quest_dialog_restore" || item.RuleId == "mission_mesh_timing"),
                    "Comment, regular, verbatim, interpolated, and raw literal text must not trigger source heuristics.");
            });
        }

        static void SourceCancellationAndInputFailures()
        {
            WithRoot(root =>
            {
                var direct = Path.Combine(root, "direct.cs");
                File.WriteAllText(direct, "internal sealed class SafeSource { }");
                using (var cancellation = new CancellationTokenSource())
                {
                    cancellation.Cancel();
                    var cancelledRequest = Request(direct);
                    cancelledRequest.CancellationToken = cancellation.Token;
                    var cancelled = ForgeAnalysisCatalog.Analyze("source", cancelledRequest);
                    Require(cancelled.State == ForgeAnalysisState.Cancelled && cancelled.FilesExamined == 0,
                        "A pre-cancelled request for one selected source file must stop before analysis.");
                }
                File.Delete(direct);

                var oversized = Path.Combine(root, "oversized.cs");
                var locked = Path.Combine(root, "locked.cs");
                var valid = Path.Combine(root, "valid.cs");
                File.WriteAllText(oversized, "// " + new string('x', 2048));
                File.WriteAllText(locked, "internal sealed class LockedSource { }");
                File.WriteAllText(valid, "namespace Demo.Campaign { internal sealed class ValidSource { } }");
                var boundedRequest = Request(root);
                boundedRequest.MaximumBytesPerFile = 1024;
                ForgeAnalysisResult result;
                using (new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None))
                    result = ForgeAnalysisCatalog.Analyze("source", boundedRequest);

                Require(result.State == ForgeAnalysisState.Completed && result.Truncated && result.FilesExamined == 3,
                    "An oversized or locked source file must not abort inspection of the rest of the bounded tree.");
                Require(result.Findings.Any(item => item.RuleId == "source_file_limit" && item.SourcePath == oversized),
                    "Oversized source input must produce a file-local limit finding.");
                Require(result.Findings.Any(item => item.RuleId == "source_file_unreadable" && item.SourcePath == locked),
                    "Unreadable source input must produce a file-local diagnostic.");
                Require(result.Findings.Any(item => item.RuleId == "campaign_namespace_shadowing" && item.SourcePath == valid),
                    "Source analysis must continue and report findings from readable files after local input failures.");
            });
        }

        static void AnalyzerCancellationIsCooperative()
        {
            WithRoot(root =>
            {
                var moduleRoot = Path.Combine(root, "Modules");
                var module = Path.Combine(moduleRoot, "Example");
                Directory.CreateDirectory(module);
                File.WriteAllText(Path.Combine(module, "SubModule.xml"), "<Module><Id value='Example'/></Module>");
                File.WriteAllText(Path.Combine(root, "strings.xml"), "<strings><string id='sample'/></strings>");
                File.WriteAllText(Path.Combine(root, "input.xml"), "<root />");
                File.WriteAllText(Path.Combine(root, "input.cs"), "internal sealed class Sample { }");
                File.WriteAllText(Path.Combine(root, "input.log"), "normal\n");
                File.WriteAllText(Path.Combine(root, "input.cfcrash"), "no exception marker");
                var archive = Path.Combine(root, "input.zip");
                using (ZipFile.Open(archive, ZipArchiveMode.Create)) { }

                var inputs = new[]
                {
                    Tuple.Create("module", moduleRoot),
                    Tuple.Create("xml", root),
                    Tuple.Create("source", root),
                    Tuple.Create("localization", root),
                    Tuple.Create("gauntlet", root),
                    Tuple.Create("assets", root),
                    Tuple.Create("fbx", root),
                    Tuple.Create("watchdog", root),
                    Tuple.Create("crash", root),
                    Tuple.Create("archive", archive)
                };

                using (var cancellation = new CancellationTokenSource())
                {
                    cancellation.Cancel();
                    foreach (var input in inputs)
                    {
                        var request = Request(input.Item2);
                        request.CancellationToken = cancellation.Token;
                        var result = ForgeAnalysisCatalog.Analyze(input.Item1, request);
                        Require(result.State == ForgeAnalysisState.Cancelled && result.Provenance == ForgeAnalysisProvenance.Unavailable,
                            "A pre-cancelled " + input.Item1 + " request must stop without claiming evidence.");
                        Require(result.FilesExamined == 0,
                            "A pre-cancelled " + input.Item1 + " request must not count input as inspected.");
                    }
                }
            });
        }

        static void SourceTreeHarnessTimings()
        {
            WithRoot(root =>
            {
                var smallRoot = Path.Combine(root, "small");
                var mediumRoot = Path.Combine(root, "medium");
                CreateSourceTree(smallRoot, 3, 8);
                CreateSourceTree(mediumRoot, 12, 16);
                var smallMedian = MeasureSourceTree(smallRoot, 24, 5);
                var mediumMedian = MeasureSourceTree(mediumRoot, 192, 5);
                Console.WriteLine("Informational ForgeAnalysis source-tree harness timings (not app latency): small 24 files median " +
                    smallMedian.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " ms; medium 192 files median " +
                    mediumMedian.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " ms; 5 runs each.");
            });
        }

        static void CreateSourceTree(string root, int directoryCount, int filesPerDirectory)
        {
            var body = "// deterministic source-tree fixture\n" + new string('x', 192) + "\n";
            for (var directoryIndex = 0; directoryIndex < directoryCount; directoryIndex++)
            {
                var directory = Path.Combine(root, "Area" + directoryIndex.ToString("D2", System.Globalization.CultureInfo.InvariantCulture));
                Directory.CreateDirectory(directory);
                for (var fileIndex = 0; fileIndex < filesPerDirectory; fileIndex++)
                {
                    var name = "Fixture" + directoryIndex.ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + "_" +
                        fileIndex.ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + ".cs";
                    File.WriteAllText(Path.Combine(directory, name), body + "internal sealed class " + Path.GetFileNameWithoutExtension(name) + " { }\n", new UTF8Encoding(false));
                }
            }
        }

        static double MeasureSourceTree(string root, int expectedFiles, int repetitions)
        {
            var samples = new double[repetitions];
            for (var repetition = 0; repetition < repetitions; repetition++)
            {
                var stopwatch = Stopwatch.StartNew();
                var request = Request(root);
                request.MaximumFiles = expectedFiles + 1;
                var result = ForgeAnalysisCatalog.Analyze("source", request);
                stopwatch.Stop();
                Require(result.State == ForgeAnalysisState.Completed && result.FilesExamined == expectedFiles,
                    "The fixed source-tree timing fixture must inspect all expected files.");
                samples[repetition] = stopwatch.Elapsed.TotalMilliseconds;
            }
            Array.Sort(samples);
            return samples[samples.Length / 2];
        }

        static void LocalizationInputFailures()
        {
            WithRoot(root =>
            {
                var english = Path.Combine(root, "EN");
                var german = Path.Combine(root, "DE");
                Directory.CreateDirectory(english);
                Directory.CreateDirectory(german);
                File.WriteAllText(Path.Combine(english, "strings.xml"), "<strings><string id='one'/><string id='two'/></strings>");
                var oversized = Path.Combine(german, "01_strings_large.xml");
                var locked = Path.Combine(german, "02_strings_locked.xml");
                var valid = Path.Combine(german, "03_strings.xml");
                File.WriteAllText(oversized, "<strings>" + new string('x', 2048) + "</strings>");
                File.WriteAllText(locked, "<strings><string id='locked'/></strings>");
                File.WriteAllText(valid, "<strings><string id='one'/></strings>");

                var boundedRequest = Request(root);
                boundedRequest.MaximumBytesPerFile = 1024;
                ForgeAnalysisResult result;
                using (new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None))
                    result = ForgeAnalysisCatalog.Analyze("localization", boundedRequest);

                Require(result.State == ForgeAnalysisState.Completed && result.Truncated && result.FilesExamined == 4,
                    "A bad language catalog must be isolated so remaining catalogs are analyzed.");
                Require(result.Findings.Any(item => item.RuleId == "localization_file_limit" && item.SourcePath == oversized),
                    "Oversized localization XML must produce a file-local limit finding.");
                Require(result.Findings.Any(item => item.RuleId == "localization_file_unreadable" && item.SourcePath == locked),
                    "Unreadable localization XML must produce a file-local diagnostic.");
                Require(result.Findings.Any(item => item.RuleId == "localization_missing_keys"),
                    "Readable language catalogs must still be compared after another catalog fails to load.");
                Require(!result.Findings.Any(item => item.RuleId == "analysis_error" || item.RuleId == "localization_english"),
                    "A local file failure must not collapse the analysis into a generic failure or hide the readable English source.");
            });
        }

        static void LocalizationFixtures()
        {
            WithRoot(root =>
            {
                var en = Path.Combine(root, "EN");
                var de = Path.Combine(root, "DE");
                Directory.CreateDirectory(en); Directory.CreateDirectory(de);
                File.WriteAllText(Path.Combine(en, "strings.xml"), "<strings><string id='one'/><string id='two'/></strings>", new UTF8Encoding(true));
                File.WriteAllText(Path.Combine(de, "strings.xml"), "<strings><string id='one'/></strings>", new UTF8Encoding(false));
                var result = ForgeAnalysisCatalog.Analyze("localization scan", Request(root));
                Require(result.Findings.Any(item => item.RuleId == "localization_missing_keys"), "Missing translated key must be reported.");
                Require(result.Findings.Any(item => item.RuleId == "localization_encoding"), "Missing BOM must be reported.");
            });
        }

        static void GauntletFixtures()
        {
            WithRoot(root =>
            {
                var prefab = Path.Combine(root, "Forge.xml");
                File.WriteAllText(prefab, "<Prefab><TextWidget Text='CF'/></Prefab>");
                var result = ForgeAnalysisCatalog.Analyze("gauntlet audit", Request(prefab));
                Require(result.Findings.Any(item => item.RuleId == "gauntlet_decoration_input"), "Interactive decoration must be reported.");
            });
        }

        static void AssetFixtures()
        {
            WithRoot(root =>
            {
                var audio = Path.Combine(root, "audio.xml");
                File.WriteAllText(audio, "<module_sounds><module_sound sound_category='unknown' path='broken.mp3'/></module_sounds>");
                var troops = Path.Combine(root, "troops.xml");
                File.WriteAllText(troops, "<NPCCharacters><NPCCharacter age='old'/></NPCCharacters>");
                var items = Path.Combine(root, "items.xml");
                File.WriteAllText(items, "<Items><Item/></Items>");
                var result = ForgeAnalysisCatalog.Analyze("item asset audit", Request(root));
                Require(result.Findings.Any(item => item.RuleId == "audio_category"), "Unsupported audio category must be reported.");
                Require(result.Findings.Any(item => item.RuleId == "troop_age"), "Invalid troop age must be reported.");
                Require(result.Findings.Any(item => item.RuleId == "item_id"), "Missing item id must be reported.");
            });
        }

        static void FbxFixtures()
        {
            WithRoot(root =>
            {
                var ascii = Path.Combine(root, "armor.fbx");
                var contents = @"; FBX 7.4.0 project file
Objects: {
    Model: 100, ""Model::armor"", ""Mesh"" {
    }
    Geometry: 100, ""Geometry::armor"", ""Mesh"" {
    }
    Geometry: 101, ""Geometry::armor.lod2"", ""Mesh"" {
    }
    Material: 200, ""Material::cloth"", """" {
    }
}";
                File.WriteAllText(ascii, contents, new UTF8Encoding(false));
                var before = File.ReadAllBytes(ascii);
                var result = ForgeAnalysisCatalog.Analyze("FbxAsciiPreflight", Request(ascii));
                Require(result.State == ForgeAnalysisState.Completed && result.Provenance == ForgeAnalysisProvenance.Structural, "FBX text declarations must be structural evidence, not a verified import.");
                using (var cancellation = new CancellationTokenSource())
                {
                    cancellation.Cancel();
                    var cancelledRequest = Request(ascii);
                    cancelledRequest.CancellationToken = cancellation.Token;
                    var cancelled = ForgeAnalysisCatalog.Analyze("fbx", cancelledRequest);
                    Require(cancelled.State == ForgeAnalysisState.Cancelled, "The FBX analyzer must honor cancellation inside its bounded scan.");
                }
                var declarations = result.Findings.SingleOrDefault(item => item.RuleId == "fbx_declarations");
                Require(declarations != null && declarations.Evidence.Contains("Geometry=2") && declarations.Evidence.Contains("armor.lod2") && declarations.Evidence.Contains("cloth"), "ASCII mesh and material declaration names must be listed.");
                Require(!result.Findings.Any(item => item.RuleId == "fbx_duplicate_geometry_name"), "The paired Model/Geometry nodes must not create a false duplicate warning.");
                Require(result.Findings.Any(item => item.RuleId == "fbx_lod_gap"), "A missing intermediate LOD naming hint must be surfaced for review.");
                Require(result.Findings.Any(item => item.RuleId == "fbx_material_resolution_unchecked"), "Material names must not imply that module resources were resolved.");
                Require(before.SequenceEqual(File.ReadAllBytes(ascii)), "FBX analysis must not modify the source file.");

                var sceneExtras = Path.Combine(root, "scene-extras.fbx");
                var sceneExtrasContents = @"; FBX 7.4.0 project file
Objects: {
    Model: 1, ""Model::camera_main"", ""Camera"" { }
    Model: 2, ""Model::key_light"", ""Light"" { }
    Geometry: 3, ""Geometry::shield"", ""Mesh"" { }
}";
                File.WriteAllText(sceneExtras, sceneExtrasContents, new UTF8Encoding(false));
                var sceneExtrasResult = ForgeAnalysisCatalog.Analyze("fbx", Request(sceneExtras));
                var sceneReview = sceneExtrasResult.Findings.SingleOrDefault(item => item.RuleId == "fbx_scene_objects_review");
                Require(sceneReview != null && sceneReview.Severity == "Warning" && sceneReview.Evidence.Contains("Camera=1") && sceneReview.Evidence.Contains("Light=1"),
                    "Camera/light declarations must be surfaced as review-only scene evidence.");
                Require(sceneReview.Recommendation.Contains("never deletes or rewrites"), "Scene-object hints must not imply automatic destructive cleanup.");

                var modelFallback = Path.Combine(root, "fallback.fbx");
                File.WriteAllText(modelFallback, "; FBX 7.4.0 project file\nModel: 1, \"Model::shield.lod1\", \"Mesh\" { }", new UTF8Encoding(false));
                var fallback = ForgeAnalysisCatalog.Analyze("fbx", Request(modelFallback));
                Require(fallback.Findings.Any(item => item.RuleId == "fbx_lod_base_missing"), "Model names must provide a fallback when no Geometry Mesh declaration exists.");

                var binary = Path.Combine(root, "binary.fbx");
                var binaryMagic = Encoding.ASCII.GetBytes("Kaydara FBX Binary  \0\u001a\0");
                File.WriteAllBytes(binary, binaryMagic.Concat(new byte[] { 1, 2, 3 }).ToArray());
                var binaryResult = ForgeAnalysisCatalog.Analyze("fbx", Request(binary));
                Require(binaryResult.Findings.Any(item => item.RuleId == "fbx_binary_unsupported"), "Binary FBX must be reported as unsupported, not parsed as ASCII.");

                var malformed = Path.Combine(root, "malformed.fbx");
                File.WriteAllText(malformed, "not an FBX document", new UTF8Encoding(false));
                Require(ForgeAnalysisCatalog.Analyze("fbx", Request(malformed)).Findings.Any(item => item.RuleId == "fbx_ascii_header_missing"), "Text without an FBX header must not be trusted.");

                var oversized = Path.Combine(root, "oversized.fbx");
                File.WriteAllText(oversized, "; FBX 7.4.0 project file\n" + new string('x', 2048), new UTF8Encoding(false));
                var limitedRequest = Request(oversized);
                limitedRequest.MaximumBytesPerFile = 1024;
                Require(ForgeAnalysisCatalog.Analyze("fbx", limitedRequest).Findings.Any(item => item.RuleId == "fbx_file_size_limit"), "Per-file FBX size limits must be enforced before parsing.");

                var batch = Path.Combine(root, "batch");
                Directory.CreateDirectory(batch);
                File.Copy(ascii, Path.Combine(batch, "one.fbx"));
                File.Copy(ascii, Path.Combine(batch, "two.fbx"));
                var batchRequest = Request(batch);
                batchRequest.MaximumFiles = 1;
                var boundedBatch = ForgeAnalysisCatalog.Analyze("fbx", batchRequest);
                Require(boundedBatch.Truncated && boundedBatch.FilesExamined == 1 && boundedBatch.Findings.Any(item => item.RuleId == "fbx_file_limit"), "Folder scans must stop at the selected FBX file limit and state truncation.");
            });
        }

        static void SpritePackageFixtures()
        {
            WithRoot(root =>
            {
                var config = Path.Combine(root, "GUI", "SpriteParts", "Config.xml");
                var spriteData = Path.Combine(root, "GUI", new DirectoryInfo(root).Name + "SpriteData.xml");
                var atlasDirectory = Path.Combine(root, "AssetSources", "GauntletUI");
                var partDirectory = Path.Combine(root, "GUI", "SpriteParts", "ui_fixture");
                var partPng = Path.Combine(partDirectory, "fixture_icon.png");
                var atlas = Path.Combine(atlasDirectory, "ui_fixture_1.png");
                Directory.CreateDirectory(Path.GetDirectoryName(config));
                Directory.CreateDirectory(atlasDirectory);
                Directory.CreateDirectory(partDirectory);
                File.WriteAllText(config, "<Config><SpriteCategory Name='ui_fixture'><AlwaysLoad /></SpriteCategory></Config>");
                File.WriteAllText(spriteData, "<SpriteData><SpriteCategories><SpriteCategory><Name>ui_fixture</Name><AlwaysLoad/><SpriteSheetCount>1</SpriteSheetCount><SpriteSheetSize ID='1' Width='128' Height='128'/></SpriteCategory></SpriteCategories><SpriteParts><SpritePart><SheetID>1</SheetID><Name>fixture_icon</Name><Width>32</Width><Height>24</Height><SheetX>4</SheetX><SheetY>6</SheetY><CategoryName>ui_fixture</CategoryName></SpritePart></SpriteParts></SpriteData>");
                WritePngHeader(atlas, 128, 128);
                WritePngHeader(partPng, 32, 24);

                var boundedRequest = Request(root);
                boundedRequest.MaximumFiles = 1;
                var bounded = ForgeAnalysisCatalog.Analyze("assets", boundedRequest);
                Require(bounded.Truncated && bounded.Findings.Any(item => item.RuleId == "sprite_scan_limit"), "Sprite readiness must explain when the shared file bound prevents the scan.");

                var missing = ForgeAnalysisCatalog.Analyze("asset structure audit", Request(root));
                var missingFinding = missing.Findings.SingleOrDefault(item => item.RuleId == "sprite_tpac_missing");
                Require(missingFinding != null && missingFinding.Severity == "Warning", "Source atlas without a TPAC must be reported as a warning.");
                Require(missingFinding.Recommendation.Contains("resource.show_resource_browser"), "The missing-TPAC finding must point to Resource Browser.");
                Require(missingFinding.Recommendation.Contains("Prepare-CalradiaForge-ResourceBrowser.bat") && missingFinding.Recommendation.Contains("--collect-tpac"), "The missing-TPAC finding must include the source/install handoff for this repository.");
                Require(missingFinding.Recommendation.Contains("never copies DLLs or manifests"), "The missing-TPAC finding must explain the version-safe scope of the helper.");

                var package = Path.Combine(root, "Assets", "GauntletUI", "ui_fixture_1_tex.tpac");
                Directory.CreateDirectory(Path.GetDirectoryName(package));
                File.WriteAllBytes(package, MakeTpacHeader(1));
                var ready = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(!ready.Findings.Any(item => item.RuleId == "sprite_tpac_missing" || item.RuleId == "sprite_tpac_empty" || item.RuleId == "sprite_tpac_header_invalid" || item.RuleId == "sprite_tpac_header_truncated" || item.RuleId == "sprite_tpac_asset_count_invalid"), "A complete TPAC fixed header with a bounded nonzero entry count must clear the header findings.");
                var marker = ready.Findings.SingleOrDefault(item => item.RuleId == "sprite_tpac_header_marker");
                Require(marker != null && marker.Severity == "Info" && marker.Evidence.Contains("36-byte TPAC v2 header") && marker.Evidence.Contains("declares 1 asset metadata entry") && marker.Evidence.Contains("does not parse entries"), "A plausible v2 header must report its entry count and bounded scope instead of claiming full TPAC validation.");
                Require(marker.Recommendation.Contains("Inspect-CalradiaForge-Tpac.bat --validate-only") && marker.Recommendation.Contains("Resource Browser"), "A plausible header must direct authors to the metadata parser and the official import tool.");
                Require(!ready.Findings.Any(item => item.RuleId == "sprite_data_missing" || item.RuleId == "sprite_category_metadata_missing" || item.RuleId == "sprite_atlas_count_mismatch"), "Matching always-loaded category metadata and source atlas counts must produce no parity findings.");
                Require(!ready.Findings.Any(item => item.RuleId.StartsWith("sprite_part_", StringComparison.Ordinal) || item.RuleId.StartsWith("sprite_atlas_png_", StringComparison.Ordinal) || item.RuleId == "sprite_atlas_dimensions_mismatch"), "Matching atlas bounds and PNG IHDR dimensions must produce no structural sprite findings.");

                var validSpriteData = File.ReadAllText(spriteData);
                File.WriteAllText(spriteData, validSpriteData.Replace("<SheetX>4</SheetX>", "<SheetX>100</SheetX>"));
                var outOfBoundsPart = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(outOfBoundsPart.Findings.Any(item => item.RuleId == "sprite_part_outside_sheet"), "Sprite coordinates extending beyond the declared sheet must be rejected.");
                File.WriteAllText(spriteData, validSpriteData);

                WritePngHeader(partPng, 31, 24);
                var partDimensions = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(partDimensions.Findings.Any(item => item.RuleId == "sprite_part_png_dimensions"), "A source PNG whose IHDR dimensions differ from SpriteData must be reported.");
                WritePngHeader(partPng, 32, 24);

                WritePngHeader(atlas, 127, 128);
                var atlasDimensions = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(atlasDimensions.Findings.Any(item => item.RuleId == "sprite_atlas_dimensions_mismatch"), "A source atlas whose IHDR dimensions differ from SpriteData must be reported.");
                WritePngHeader(atlas, 128, 128);

                File.WriteAllBytes(partPng, new byte[] { 1, 2, 3 });
                var invalidPartHeader = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(invalidPartHeader.Findings.Any(item => item.RuleId == "sprite_part_png_header"), "A source part without a readable PNG IHDR must be reported.");
                WritePngHeader(partPng, 32, 24);

                File.WriteAllBytes(atlas, new byte[] { 1, 2, 3, 4 });
                var invalidAtlas = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(invalidAtlas.Findings.Any(item => item.RuleId == "sprite_atlas_png_header"), "A malformed source atlas PNG header must be reported.");
                WritePngHeader(atlas, 128, 128);

                File.Delete(partPng);
                var missingPart = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(missingPart.Findings.Any(item => item.RuleId == "sprite_part_missing"), "A SpriteData entry without its source PNG must be reported.");
                WritePngHeader(partPng, 32, 24);

                File.WriteAllText(spriteData, validSpriteData.Replace("<CategoryName>ui_fixture</CategoryName>", "<CategoryName>../outside</CategoryName>"));
                var unsafePartPath = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(unsafePartPath.Findings.Any(item => item.RuleId == "sprite_part_metadata_invalid"), "SpriteData path traversal in a sprite part category must be rejected before file lookup.");
                File.WriteAllText(spriteData, validSpriteData);

                File.Delete(spriteData);
                var missingMetadata = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(missingMetadata.Findings.Any(item => item.RuleId == "sprite_data_missing"), "Missing SpriteData must be reported for an always-loaded sprite category.");

                File.WriteAllText(spriteData, "<SpriteData><SpriteCategories><SpriteCategory><Name>ui_fixture</Name><AlwaysLoad/><SpriteSheetCount>2</SpriteSheetCount></SpriteCategory></SpriteCategories></SpriteData>");
                var countMismatch = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(countMismatch.Findings.Any(item => item.RuleId == "sprite_atlas_count_mismatch" && item.Severity == "Error"), "A SpriteData sheet count that differs from generated atlas files must be reported.");

                File.WriteAllText(spriteData, "<SpriteData><SpriteCategories><SpriteCategory><Name>ui_other</Name><AlwaysLoad/><SpriteSheetCount>1</SpriteSheetCount></SpriteCategory></SpriteCategories></SpriteData>");
                var categoryMismatch = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(categoryMismatch.Findings.Any(item => item.RuleId == "sprite_category_metadata_missing"), "An always-loaded Config category absent from SpriteData must be reported.");

                File.WriteAllText(spriteData, "<SpriteData>");
                var invalidMetadata = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(invalidMetadata.Findings.Any(item => item.RuleId == "sprite_data_xml" && item.Line.HasValue), "Malformed SpriteData must be reported with line evidence.");
                File.WriteAllText(spriteData, "<SpriteData><SpriteCategories><SpriteCategory><Name>ui_fixture</Name><AlwaysLoad/><SpriteSheetCount>1</SpriteSheetCount></SpriteCategory></SpriteCategories></SpriteData>");

                File.WriteAllBytes(package, Encoding.ASCII.GetBytes("BAD!-test-payload"));
                var invalidHeader = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(invalidHeader.Findings.Any(item => item.RuleId == "sprite_tpac_header_invalid" && item.Severity == "Error"), "A non-empty file without the TPAC marker must not be treated as a runtime package.");

                File.WriteAllBytes(package, new byte[] { (byte)'T', (byte)'P', (byte)'A' });
                var truncatedHeader = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(truncatedHeader.Findings.Any(item => item.RuleId == "sprite_tpac_header_invalid"), "A truncated header must be rejected.");

                File.WriteAllBytes(package, Encoding.ASCII.GetBytes("TPAC-test-payload"));
                var shortFixedHeader = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(shortFixedHeader.Findings.Any(item => item.RuleId == "sprite_tpac_header_truncated" && item.Severity == "Error"), "A TPAC marker without the complete fixed header must be rejected.");

                File.WriteAllBytes(package, MakeTpacHeader(100000));
                var maximumEntryList = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(maximumEntryList.Findings.Any(item => item.RuleId == "sprite_tpac_header_marker" && item.Evidence.Contains("TPAC v2") && item.Evidence.Contains("declares 100000 asset metadata entries") && item.Evidence.Contains("table of contents to 1 bytes")), "A bounded 36-byte TPAC v2 header must accept the documented maximum asset count and table size.");

                File.WriteAllBytes(package, MakeTpacHeader(1, 2, 37));
                var tocOverrun = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(tocOverrun.Findings.Any(item => item.RuleId == "sprite_tpac_toc_size_invalid" && item.Severity == "Error"), "A TPAC table extending past end of file must be rejected.");

                File.WriteAllBytes(package, MakeTpacHeader(1, 2, 37, 3));
                var unsupportedVersion = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                var unsupportedVersionFinding = unsupportedVersion.Findings.SingleOrDefault(item => item.RuleId == "sprite_tpac_version_unsupported");
                Require(unsupportedVersionFinding != null && unsupportedVersionFinding.Severity == "Warning" && unsupportedVersionFinding.Evidence.Contains("version 3") && unsupportedVersionFinding.Evidence.Contains("No version-specific fields were interpreted"), "An unknown TPAC version must be reported as unsupported without parsing its version-specific fields or calling it malformed.");
                Require(!unsupportedVersion.Findings.Any(item => item.RuleId == "sprite_tpac_toc_size_invalid" || item.RuleId == "sprite_tpac_asset_count_invalid"), "An unknown TPAC version must not be judged using v2 offsets.");

                File.WriteAllBytes(package, MakeTpacHeader(0));
                var emptyEntryList = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(emptyEntryList.Findings.Any(item => item.RuleId == "sprite_tpac_asset_count_invalid" && item.Severity == "Error"), "A TPAC fixed header declaring zero assets must be rejected.");

                File.WriteAllBytes(package, MakeTpacHeader(100001));
                var excessiveEntryList = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(excessiveEntryList.Findings.Any(item => item.RuleId == "sprite_tpac_asset_count_invalid" && item.Severity == "Error"), "A TPAC fixed header declaring more than the bounded asset count must be rejected.");

                File.WriteAllBytes(package, new byte[0]);
                var empty = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(empty.Findings.Any(item => item.RuleId == "sprite_tpac_empty" && item.Severity == "Error"), "An empty runtime package must be reported as an error.");

                File.Delete(atlas);
                var missingAtlas = ForgeAnalysisCatalog.Analyze("assets", Request(root));
                Require(missingAtlas.Findings.Any(item => item.RuleId == "sprite_atlas_missing"), "An always-loaded category without generated atlas output must be reported.");

                File.WriteAllText(config, "<Config>");
                var nestedSelection = ForgeAnalysisCatalog.Analyze("assets", Request(atlasDirectory));
                Require(nestedSelection.Findings.Any(item => item.RuleId == "asset_xml" && item.SourcePath.EndsWith("Config.xml")), "An unreadable parent module sprite config must retain file evidence when scanning a nested folder.");
            });
        }

        static byte[] MakeTpacHeader(uint entryCount, ulong tocSize = 1, int totalLength = 37, uint formatVersion = 2)
        {
            var header = new byte[totalLength];
            header[0] = (byte)'T';
            header[1] = (byte)'P';
            header[2] = (byte)'A';
            header[3] = (byte)'C';
            Buffer.BlockCopy(BitConverter.GetBytes(formatVersion), 0, header, 4, sizeof(uint));
            Buffer.BlockCopy(new Guid("6F2A2A0B-6062-46C3-BDB4-3E5E8C8E15FA").ToByteArray(), 0, header, 8, 16);
            Buffer.BlockCopy(BitConverter.GetBytes(entryCount), 0, header, 24, sizeof(uint));
            Buffer.BlockCopy(BitConverter.GetBytes(tocSize), 0, header, 28, sizeof(ulong));
            return header;
        }

        static void LogAndCrashFixtures()
        {
            WithRoot(root =>
            {
                var log = Path.Combine(root, "rgl_log.txt");
                File.WriteAllText(log, "normal\nERROR test failure\n");
                var watchdog = ForgeAnalysisCatalog.Analyze("watchdog parser", Request(log));
                Require(watchdog.Findings.Any(item => item.RuleId == "watchdog_event" && item.Line == 2), "Watchdog finding must retain line evidence.");
                var dump = Path.Combine(root, "crash.dmp");
                File.WriteAllBytes(dump, new byte[] { 0, 1, 2 });
                var crash = ForgeAnalysisCatalog.Analyze("crash", Request(dump));
                Require(crash.Findings.Any(item => item.RuleId == "binary_metadata_only"), "Binary crash input must be metadata-only.");
            });
        }

        static void ArchiveFixtures()
        {
            WithRoot(root =>
            {
                var archive = Path.Combine(root, "unsafe.zip");
                using (var zip = ZipFile.Open(archive, ZipArchiveMode.Create))
                {
                    zip.CreateEntry("save.sav");
                    zip.CreateEntry("bin/TaleWorlds.Core.dll");
                }
                var result = ForgeAnalysisCatalog.Analyze("archive packaging audit", Request(archive));
                Require(result.Provenance == ForgeAnalysisProvenance.Verified, "Archive inspection is verified file evidence.");
                Require(result.Findings.Any(item => item.RuleId == "archive_save"), "Save entry must be rejected.");
                Require(result.Findings.Any(item => item.RuleId == "archive_game_binary"), "Game binary entry must be rejected.");

                var excessiveDirectory = Path.Combine(root, "excessive-directory.zip");
                File.Copy(archive, excessiveDirectory);
                using (var file = new FileStream(excessiveDirectory, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    var tailLength = (int)Math.Min(file.Length, 22L + ushort.MaxValue);
                    var tail = new byte[tailLength];
                    file.Position = file.Length - tailLength;
                    var tailRead = 0;
                    while (tailRead < tail.Length)
                    {
                        var read = file.Read(tail, tailRead, tail.Length - tailRead);
                        if (read == 0) throw new Exception("Could not read the ZIP tail fixture.");
                        tailRead += read;
                    }
                    var endRecord = -1;
                    for (var index = tail.Length - 22; index >= 0; index--)
                    {
                        if (tail[index] == 0x50 && tail[index + 1] == 0x4b && tail[index + 2] == 0x05 && tail[index + 3] == 0x06 &&
                            index + 22 + tail[index + 20] + (tail[index + 21] << 8) == tail.Length)
                        {
                            endRecord = index;
                            break;
                        }
                    }
                    if (endRecord < 0) throw new Exception("The ZIP fixture had no end-of-central-directory record.");
                    const ushort excessiveEntryCount = 10001;
                    tail[endRecord + 8] = (byte)(excessiveEntryCount & 0xff);
                    tail[endRecord + 9] = (byte)(excessiveEntryCount >> 8);
                    tail[endRecord + 10] = (byte)(excessiveEntryCount & 0xff);
                    tail[endRecord + 11] = (byte)(excessiveEntryCount >> 8);
                    file.Position = file.Length - tailLength;
                    file.Write(tail, 0, tail.Length);
                }
                var excessiveRequest = Request(excessiveDirectory);
                var excessive = ForgeAnalysisCatalog.Analyze("archive", excessiveRequest);
                Require(excessive.Findings.Any(item => item.RuleId == "archive_entry_limit"), "A ZIP with an excessive declared central-directory count must be rejected before ZipArchive materializes entries.");
                Require(excessive.Truncated && excessive.FilesExamined == 0 && !excessive.Findings.Any(item => item.RuleId == "archive_save" || item.RuleId == "archive_game_binary"), "An oversized entry directory must not produce entry-level inspection evidence.");

                var boundedRequest = Request(archive);
                boundedRequest.MaximumFiles = 1;
                var bounded = ForgeAnalysisCatalog.Analyze("archive", boundedRequest);
                Require(bounded.FilesExamined == 1 && bounded.Truncated, "A bounded archive scan must report the number of entries actually inspected and mark omitted entries as truncated.");
                Require(!bounded.Findings.Any(item => item.RuleId == "archive_game_binary"), "An archive entry beyond the inspection limit must not be reported as inspected.");

                var oversized = Path.Combine(root, "oversized.zip");
                using (var zip = ZipFile.Open(oversized, ZipArchiveMode.Create))
                {
                    var entry = zip.CreateEntry("payload.bin");
                    var payload = new byte[4096];
                    new Random(173).NextBytes(payload);
                    using (var entryStream = entry.Open()) entryStream.Write(payload, 0, payload.Length);
                }
                var sizeLimitedRequest = Request(oversized);
                sizeLimitedRequest.MaximumBytesPerFile = 1024;
                var sizeLimited = ForgeAnalysisCatalog.Analyze("archive", sizeLimitedRequest);
                Require(sizeLimited.Findings.Any(item => item.RuleId == "archive_size_limit"), "An oversized ZIP must be rejected before its central directory is materialized.");
                Require(sizeLimited.Truncated && sizeLimited.FilesExamined == 0 && !sizeLimited.Findings.Any(item => item.RuleId == "archive_save" || item.RuleId == "archive_game_binary"), "An oversized ZIP must be marked incomplete and must not report entry-level inspection evidence.");
            });
        }

        static void AnalyzerRegistry()
        {
            var engine = new TestEngine();
            engine.Register(new FixtureAnalyzer());
            Require(engine.Analyzers.Count == 1, "Registered analyzer must be discoverable.");
            Require(engine.Analyze("fixture.analysis", new ForgeAnalysisRequest()).State == ForgeAnalysisState.Completed, "Registered analyzer must execute.");
            var writer = new FixtureAnalyzer { ChangesState = true };
            RequireThrows(() => engine.Register(writer), "State-changing analyzer registration must fail.");
        }

        static void RuntimeCapabilities()
        {
            var engine = new TestEngine();
            Require(!engine.Supports(ForgeRuntimeCapability.Input), "Unimplemented input must not be advertised.");
            try { engine.IsKeyDown(ForgeInputKey.Left); }
            catch (ForgeCapabilityUnavailableException error)
            {
                Require(error.Message.IndexOf("Input", StringComparison.OrdinalIgnoreCase) >= 0, "Unavailable error must identify the capability.");
                return;
            }
            throw new Exception("Unavailable input service must throw.");
        }

        static ForgeAnalysisRequest Request(string path) { return new ForgeAnalysisRequest { TargetPath = path, MaximumFiles = 50, MaximumBytesPerFile = 1024 * 1024 }; }
        static void WritePngHeader(string path, int width, int height)
        {
            var bytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 0, 0, 0, 0, 0 };
            bytes[16] = (byte)(width >> 24);
            bytes[17] = (byte)(width >> 16);
            bytes[18] = (byte)(width >> 8);
            bytes[19] = (byte)width;
            bytes[20] = (byte)(height >> 24);
            bytes[21] = (byte)(height >> 16);
            bytes[22] = (byte)(height >> 8);
            bytes[23] = (byte)height;
            File.WriteAllBytes(path, bytes);
        }
        static void WithRoot(Action<string> action)
        {
            var root = Path.Combine(Path.GetTempPath(), "CalradiaForgeAnalysis-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try { action(root); } finally { Directory.Delete(root, true); }
        }
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void RequireThrows(Action action, string message)
        {
            try { action(); } catch { return; }
            throw new Exception(message);
        }

        sealed class FixtureAnalyzer : IForgeAnalyzer
        {
            public bool ChangesState { get; set; }
            public Descriptor Descriptor { get { return new Descriptor { Id = "fixture.analysis", Module = "fixture", Context = Context.Any, ChangesState = ChangesState }; } }
            public ForgeAnalysisResult Analyze(ForgeAnalysisRequest request) { return new ForgeAnalysisResult { AnalyzerId = Descriptor.Id, AnalyzerName = "Fixture", State = ForgeAnalysisState.Completed, Provenance = ForgeAnalysisProvenance.Verified, Summary = "fixture" }; }
        }
    }
}
