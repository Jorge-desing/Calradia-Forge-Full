using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using CalradiaForge.Sdk;

namespace CalradiaForge.Core
{
    /// <summary>
    /// Local, bounded analyzers used by the desktop workbench and by SDK extensions. They inspect
    /// selected files only; they do not load mod assemblies, invoke user code, or modify a game.
    /// </summary>
    public static class ForgeAnalysisCatalog
    {
        const uint MaximumSpritePackageEntryCount = 100000;
        const int SpritePackageFixedHeaderLength = 36;
        const int MaximumFbxObjectRecordsPerFile = 10000;
        const int MaximumFbxDirectories = 10000;
        const int MaximumFbxDirectoryEntries = 200000;
        const long MaximumFbxTotalBytes = 2147483648L;
        internal const int MaximumAnalysisDirectories = 10000;
        internal const int MaximumAnalysisDirectoryEntries = 200000;
        internal const int MaximumAnalysisDirectoryDepth = 64;
        internal const int MaximumAnalysisFiles = 2000;
        internal const int MaximumAnalysisBytesPerFile = 16 * 1024 * 1024;
        const int MaximumArchiveDirectoryEntries = 10000;
        static readonly byte[] FbxBinaryMagic = Encoding.ASCII.GetBytes("Kaydara FBX Binary  \0\u001a\0");
        static readonly Regex FbxObjectPattern = new Regex(@"^\s*(?<kind>Model|Geometry|Material):\s*(?<id>-?\d+)\s*,\s*""(?<qualified>(?:\\.|[^""])*)""\s*,\s*""(?<subtype>(?:\\.|[^""])*)""", RegexOptions.CultureInvariant);
        static readonly Regex FbxLodPattern = new Regex(@"^(?<base>.+?)(?:\.lod|_lod)(?<level>\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static readonly IReadOnlyList<string> AnalyzerIds = Array.AsReadOnly(new[]
        {
            "module", "xml", "source", "localization", "gauntlet", "assets", "fbx", "watchdog", "crash", "archive"
        });
        static readonly Dictionary<string, string> AnalyzerAliases = CreateAnalyzerAliases();

        public static IReadOnlyList<string> BuiltInAnalyzerIds => AnalyzerIds;

        public static ForgeAnalysisResult Analyze(string analyzerId, ForgeAnalysisRequest request)
        {
            request = request ?? new ForgeAnalysisRequest();
            var id = Normalize(analyzerId ?? request.AnalyzerId);
            request.AnalyzerId = id;
            request.MaximumFiles = Math.Max(1, Math.Min(request.MaximumFiles, MaximumAnalysisFiles));
            request.MaximumBytesPerFile = Math.Max(1024, Math.Min(request.MaximumBytesPerFile,
                id == "fbx" ? 256 * 1024 * 1024 : MaximumAnalysisBytesPerFile));

            var result = Start(id, request.TargetPath);
            if (id == "ApiDeprecationChecker")
                return Finish(result, ForgeAnalysisState.Unsupported, ForgeAnalysisProvenance.Unavailable,
                    "No verified TaleWorlds API deprecation catalog is available; no compatibility claim was made.");

            try
            {
                if (string.IsNullOrWhiteSpace(request.TargetPath))
                    return Finish(result, ForgeAnalysisState.NotRun, ForgeAnalysisProvenance.Unavailable,
                        "Select a file or folder before running this analysis.");
                request.CancellationToken.ThrowIfCancellationRequested();
                if (!File.Exists(request.TargetPath) && !Directory.Exists(request.TargetPath))
                    return Finish(result, ForgeAnalysisState.NotRun, ForgeAnalysisProvenance.Unavailable,
                        "The selected input no longer exists.");

                switch (id)
                {
                    case "module": AnalyzeModules(request, result); break;
                    case "xml": AnalyzeXml(request, result); break;
                    case "source": AnalyzeSource(request, result); break;
                    case "localization": AnalyzeLocalization(request, result); break;
                    case "gauntlet": AnalyzeGauntlet(request, result); break;
                    case "assets": AnalyzeAssets(request, result); break;
                    case "fbx": AnalyzeFbx(request, result); break;
                    case "watchdog": AnalyzeWatchdog(request, result); break;
                    case "crash": AnalyzeCrashMetadata(request, result); break;
                    case "archive": AnalyzeArchive(request, result); break;
                    default: return Finish(result, ForgeAnalysisState.Unsupported, ForgeAnalysisProvenance.Unavailable,
                        "This tool has no verified analyzer mapping.");
                }
                request.CancellationToken.ThrowIfCancellationRequested();
                var provenance = id == "module" || id == "archive" || id == "watchdog" || id == "crash"
                    ? ForgeAnalysisProvenance.Verified : ForgeAnalysisProvenance.Structural;
                return Finish(result, ForgeAnalysisState.Completed, provenance,
                    result.Findings.Count == 0 ? "No findings were produced from the selected input." :
                    result.Findings.Count + " finding(s) were produced from the selected input.");
            }
            catch (OperationCanceledException)
            {
                return Finish(result, ForgeAnalysisState.Cancelled, ForgeAnalysisProvenance.Unavailable, "Analysis was cancelled.");
            }
            catch (Exception error)
            {
                Add(result, "analysis_error", "Error", request.TargetPath, null, null, Bound(error.Message, 512),
                    "Fix the input issue and run the analysis again.");
                return Finish(result, ForgeAnalysisState.Failed, ForgeAnalysisProvenance.Unavailable, "Analysis could not complete.");
            }
        }

        static ForgeAnalysisResult Start(string id, string target)
        {
            return new ForgeAnalysisResult { AnalyzerId = id, AnalyzerName = AnalyzerName(id), TargetPath = target };
        }

        static ForgeAnalysisResult Finish(ForgeAnalysisResult result, ForgeAnalysisState state, ForgeAnalysisProvenance provenance, string summary)
        {
            result.State = state;
            result.Provenance = provenance;
            result.Summary = summary;
            result.CompletedAt = DateTime.UtcNow.ToString("O");
            return result;
        }

        static string Normalize(string analyzerId)
        {
            var value = (analyzerId ?? "").Trim();
            if (AnalyzerAliases.TryGetValue(value, out var canonicalId)) return canonicalId;
            return value.ToLowerInvariant();
        }

        static Dictionary<string, string> CreateAnalyzerAliases()
        {
            var aliases = new Dictionary<string, string>(36, StringComparer.OrdinalIgnoreCase)
            {
                ["SubModuleValidator"] = "module",
                ["ModConflictMatrix"] = "module",
                ["ModIdAuditor"] = "module",
                ["XmlSchemaValidator"] = "xml",
                ["WatchdogParser"] = "watchdog",
                ["CrashAnalyzer"] = "crash",
                ["ApiDeprecationChecker"] = "ApiDeprecationChecker",
                ["PackagingAuditor"] = "archive",
                ["MissionMeshGuard"] = "source",
                ["SaveTypeDefinerAuditor"] = "source",
                ["GauntletEventPassChecker"] = "gauntlet",
                ["CampaignNamespaceGuard"] = "source",
                ["AudioFmodMixerInspector"] = "assets",
                ["SpritePackageAuditor"] = "assets",
                ["FbxAsciiPreflight"] = "fbx",
                ["module manifest audit"] = "module",
                ["source api check"] = "source",
                ["localization scan"] = "localization",
                ["gauntlet audit"] = "gauntlet",
                ["item asset audit"] = "assets",
                ["asset structure audit"] = "assets",
                ["watchdog parser"] = "watchdog",
                ["archive packaging audit"] = "archive"
            };
            foreach (var id in AnalyzerIds) aliases[id] = id;
            return aliases;
        }

        static string AnalyzerName(string id) => id switch
        {
            "module" => "Module and dependency analysis",
            "xml" => "XML safety analysis",
            "source" => "C# structural analysis",
            "localization" => "Localization analysis",
            "gauntlet" => "Gauntlet analysis",
            "assets" => "Game asset analysis",
            "fbx" => "FBX ASCII declaration preflight",
            "watchdog" => "Watchdog log analysis",
            "crash" => "Crash metadata analysis",
            "archive" => "Archive analysis",
            "ApiDeprecationChecker" => "TaleWorlds API deprecation check",
            _ => "Analysis"
        };

        static void AnalyzeModules(ForgeAnalysisRequest request, ForgeAnalysisResult result)
        {
            var root = Directory.Exists(request.TargetPath) ? request.TargetPath : Path.GetDirectoryName(request.TargetPath);
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                Add(result, "module_root", "Error", request.TargetPath, null, null, "A module folder is required.", "Select the Modules folder or a module root.");
                return;
            }
            var modulesRoot = File.Exists(Path.Combine(root, "SubModule.xml")) ? Path.GetDirectoryName(root) : root;
            var inspected = ModuleValidator.Inspect(modulesRoot, request.CancellationToken);
            result.FilesExamined = inspected.Modules.Count;
            if (inspected.Findings.Any(finding => finding.Code == "analysis_scan_incomplete")) result.Truncated = true;
            if (inspected.Findings.Count > 1000) result.Truncated = true;
            foreach (var finding in inspected.Findings.Take(1000))
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                var evidencePath = finding.File;
                if (!string.IsNullOrWhiteSpace(evidencePath) && Directory.Exists(evidencePath))
                {
                    var manifest = Path.Combine(evidencePath, "SubModule.xml");
                    if (File.Exists(manifest)) evidencePath = manifest;
                }
                Add(result, finding.Code, finding.Level, evidencePath, null, null, finding.Message, finding.Suggestion);
            }
            if (inspected.Modules.Count == 0)
                Add(result, "module_none", "Warning", modulesRoot, null, null, "No module manifests were discovered.", "Select a folder containing module directories.");
        }

        static void AnalyzeXml(ForgeAnalysisRequest request, ForgeAnalysisResult result)
        {
            foreach (var file in Files(request, "*.xml", result))
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                result.FilesExamined++;
                try { LoadXml(file, request.MaximumBytesPerFile, request.CancellationToken); }
                catch (XmlException error)
                {
                    Add(result, "xml_invalid", "Error", file, error.LineNumber, error.LinePosition, error.Message,
                        "Correct the XML structure or remove unsupported DTD declarations.");
                }
                catch (InvalidDataException error)
                {
                    Add(result, "xml_limit", "Warning", file, null, null, error.Message, "Select a smaller file or raise the documented analyzer limit.");
                }
            }
            if (result.FilesExamined == 0)
                Add(result, "xml_none", "Warning", request.TargetPath, null, null, "No XML files were found.", "Select an XML file or folder.");
        }

        static void AnalyzeSource(ForgeAnalysisRequest request, ForgeAnalysisResult result)
        {
            foreach (var file in Files(request, "*.cs", result))
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                result.FilesExamined++;
                string text;
                try { text = ReadBounded(file, request.MaximumBytesPerFile, request.CancellationToken); }
                catch (InvalidDataException error)
                {
                    result.Truncated = true;
                    Add(result, "source_file_limit", "Warning", file, null, null, error.Message,
                        "Reduce the source file below the configured analysis limit and rerun the source analysis.");
                    continue;
                }
                catch (Exception error) when (IsAnalysisInputError(error))
                {
                    result.Truncated = true;
                    Add(result, "source_file_unreadable", "Error", file, null, null, Bound(error.Message, 512),
                        "Check file access and rerun the source analysis; this file was not considered valid.");
                    continue;
                }

                // Keep offsets and line breaks stable while excluding comments and literal text
                // from the source-only structural heuristics below.
                request.CancellationToken.ThrowIfCancellationRequested();
                if (!TryMaskCSharpCommentsAndLiterals(text, out var code))
                {
                    AddIncompleteSourceFinding(result, file,
                        "C# source could not be masked completely because a comment or literal was unterminated or exceeded the bounded nesting depth.",
                        "Correct the C# source or simplify nested interpolations, then rerun the source analysis.");
                    continue;
                }
                request.CancellationToken.ThrowIfCancellationRequested();
                if (Regex.IsMatch(code, @"\bnamespace\s+[A-Za-z0-9_\.]*\.Campaign\s*[;\{]"))
                    Add(result, "campaign_namespace_shadowing", "Error", file, LineOf(code, ".Campaign", request.CancellationToken), null,
                        "Namespace ends in .Campaign and can shadow TaleWorlds.CampaignSystem.Campaign.", "Use .CampaignBehaviors or .CampaignMechanics.");
                request.CancellationToken.ThrowIfCancellationRequested();
                if (Regex.IsMatch(code, @"\b(namespace|class)\s+Localization\b"))
                    Add(result, "localization_shadowing", "Error", file, LineOf(code, "Localization", request.CancellationToken), null,
                        "A local Localization symbol can shadow TaleWorlds.Localization.", "Use a distinct namespace or type name.");
                request.CancellationToken.ThrowIfCancellationRequested();
                var typeDefiner = Regex.Match(code, @"SaveableTypeDefiner\s*\(\s*(\d[\d_]*)");
                if (!typeDefiner.Success)
                    typeDefiner = Regex.Match(code, @"SaveableTypeDefiner[\s\S]{0,512}?\bbase\s*\(\s*(\d[\d_]*)");
                if (typeDefiner.Success && int.TryParse(typeDefiner.Groups[1].Value.Replace("_", ""), out var baseId) && baseId < 2500000)
                    Add(result, "save_id_range", "Error", file, LineOf(code, typeDefiner.Value, request.CancellationToken), null,
                        "SaveableTypeDefiner base ID " + baseId + " is below 2,500,000.", "Allocate a unique base ID at or above 2,500,000.");
                if ((code.Contains(": QuestBase") || code.Contains(":QuestBase")) &&
                    (!code.Contains("InitializeQuestOnGameLoad") || Regex.Matches(code, @"\bSetDialogs\s*\(\s*\)").Count < 2))
                    Add(result, "quest_dialog_restore", "Warning", file, LineOf(code, "QuestBase", request.CancellationToken), null,
                        "QuestBase does not show both constructor and load-time SetDialogs calls.", "Restore dialogs when a saved quest loads.");
                if ((code.Contains(": MissionLogic") || code.Contains(":MissionLogic")) && code.Contains("override void OnInit()") &&
                    (code.Contains("Mesh") || code.Contains("Skeleton")) && !code.Contains("_initialized"))
                    Add(result, "mission_mesh_timing", "Warning", file, LineOf(code, "OnInit", request.CancellationToken), null,
                        "Mission mesh or skeleton work appears in OnInit without a deferred initialization guard.", "Defer scene changes to a guarded mission tick.");
            }
            if (result.FilesExamined == 0)
                Add(result, "source_none", "Warning", request.TargetPath, null, null, "No C# files were found.", "Select a source file or directory.");
        }

        static void AnalyzeLocalization(ForgeAnalysisRequest request, ForgeAnalysisResult result)
        {
            var files = new List<string>();
            foreach (var path in Files(request, "*.xml", result))
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                if (Path.GetFileName(path).IndexOf("string", StringComparison.OrdinalIgnoreCase) >= 0)
                    files.Add(path);
            }
            var catalogs = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                result.FilesExamined++;
                try
                {
                    var doc = LoadXml(file, request.MaximumBytesPerFile, request.CancellationToken);
                    var ids = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var item in doc.Descendants("string"))
                    {
                        request.CancellationToken.ThrowIfCancellationRequested();
                        var key = (string)item.Attribute("id") ?? (string)item.Attribute("key");
                        if (!string.IsNullOrWhiteSpace(key)) ids.Add(key);
                    }
                    var language = new DirectoryInfo(Path.GetDirectoryName(file)).Name;
                    catalogs[language] = ids;
                    if (!HasUtf8Bom(file))
                        Add(result, "localization_encoding", "Warning", file, null, null, "The file is not UTF-8 with BOM.", "Save Bannerlord language XML as UTF-8 with BOM.");
                }
                catch (XmlException error) { Add(result, "localization_xml", "Error", file, error.LineNumber, error.LinePosition, error.Message, "Correct the language XML."); }
                catch (InvalidDataException error)
                {
                    result.Truncated = true;
                    Add(result, "localization_file_limit", "Warning", file, null, null, error.Message,
                        "Reduce the language file below the configured analysis limit and rerun localization analysis.");
                }
                catch (Exception error) when (IsAnalysisInputError(error))
                {
                    result.Truncated = true;
                    Add(result, "localization_file_unreadable", "Error", file, null, null, Bound(error.Message, 512),
                        "Check file access and rerun localization analysis; this catalog was not considered valid.");
                }
            }
            var english = catalogs.FirstOrDefault(pair => pair.Key.Equals("EN", StringComparison.OrdinalIgnoreCase) || pair.Key.Equals("en", StringComparison.OrdinalIgnoreCase));
            if (english.Value == null)
            {
                Add(result, "localization_english", "Error", request.TargetPath, null, null, "No English source catalog was found.", "Provide EN or en as the authoritative catalog.");
                return;
            }
            foreach (var catalog in catalogs.Where(pair => !ReferenceEquals(pair.Value, english.Value)))
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                var missing = new List<string>(20);
                foreach (var key in english.Value)
                {
                    request.CancellationToken.ThrowIfCancellationRequested();
                    if (catalog.Value.Contains(key)) continue;
                    missing.Add(key);
                    if (missing.Count == 20) break;
                }
                if (missing.Count > 0)
                    Add(result, "localization_missing_keys", "Warning", catalog.Key, null, null,
                        catalog.Key + " is missing " + missing.Count + "+ English key(s): " + string.Join(", ", missing), "Synchronize keys before release.");
            }
        }

        static void AnalyzeGauntlet(ForgeAnalysisRequest request, ForgeAnalysisResult result)
        {
            foreach (var file in Files(request, "*.xml", result))
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                result.FilesExamined++;
                XDocument doc;
                try { doc = LoadXml(file, request.MaximumBytesPerFile, request.CancellationToken); }
                catch (XmlException error) { Add(result, "gauntlet_xml", "Error", file, error.LineNumber, error.LinePosition, error.Message, "Correct the prefab XML."); continue; }
                foreach (var widget in doc.Descendants())
                {
                    request.CancellationToken.ThrowIfCancellationRequested();
                    if (!widget.Name.LocalName.EndsWith("Widget", StringComparison.Ordinal)) continue;
                    var isDecoration = ((string)widget.Attribute("Brush") ?? "").IndexOf("watermark", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        ((string)widget.Attribute("Text") ?? "").Equals("CF", StringComparison.OrdinalIgnoreCase);
                    if (isDecoration && ((string)widget.Attribute("DoNotAcceptEvents") != "true" || (string)widget.Attribute("DoNotPassEventsToChildren") != "true"))
                        Add(result, "gauntlet_decoration_input", "Error", file, null, null,
                            "A decorative widget can intercept pointer input.", "Set DoNotAcceptEvents and DoNotPassEventsToChildren to true.");
                }
            }
            if (result.FilesExamined == 0)
                Add(result, "gauntlet_none", "Warning", request.TargetPath, null, null, "No prefab XML was found.", "Select a GUI Prefabs folder or prefab XML file.");
        }

        static void AnalyzeFbx(ForgeAnalysisRequest request, ForgeAnalysisResult result)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            const int maximumDepth = 32;
            var files = new List<string>();
            long totalBytes = 0;
            var stopDiscovery = false;
            var selectedFile = File.Exists(request.TargetPath);
            if (selectedFile)
            {
                if (!string.Equals(Path.GetExtension(request.TargetPath), ".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    Add(result, "fbx_input_type", "Warning", request.TargetPath, null, null,
                        "The selected file is not an FBX file.", "Select an .fbx file or a folder containing FBX files.");
                    return;
                }
                files.Add(Path.GetFullPath(request.TargetPath));
            }
            else
            {
                var root = new DirectoryInfo(Path.GetFullPath(request.TargetPath));
                if ((root.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    Add(result, "fbx_reparse_root", "Warning", root.FullName, null, null,
                        "The selected folder is a reparse point and was not traversed.", "Select the actual source folder directly.");
                    return;
                }

                var pending = new Stack<Tuple<DirectoryInfo, int>>();
                pending.Push(Tuple.Create(root, 0));
                var discoveredDirectories = 1;
                var inspectedEntries = 0;
                while (pending.Count > 0 && !stopDiscovery)
                {
                    var current = pending.Pop();
                    try
                    {
                        foreach (var entry in current.Item1.EnumerateFileSystemInfos())
                        {
                            request.CancellationToken.ThrowIfCancellationRequested();
                            inspectedEntries++;
                            if (inspectedEntries > MaximumFbxDirectoryEntries)
                            {
                                result.Truncated = true;
                                Add(result, "fbx_entry_limit", "Warning", current.Item1.FullName, null, null,
                                    "The preflight reached its directory-entry limit of " + MaximumFbxDirectoryEntries + ".", "Select a narrower folder or split the source tree.");
                                stopDiscovery = true;
                                pending.Clear();
                                break;
                            }
                            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                            if ((entry.Attributes & FileAttributes.Directory) != 0)
                            {
                                if (current.Item2 >= maximumDepth)
                                {
                                    if (!result.Truncated)
                                        Add(result, "fbx_depth_limit", "Warning", entry.FullName, null, null,
                                            "A nested folder exceeded the preflight depth limit of " + maximumDepth + ".", "Select that subfolder directly for a separate bounded scan.");
                                    result.Truncated = true;
                                    continue;
                                }
                                discoveredDirectories++;
                                if (discoveredDirectories > MaximumFbxDirectories)
                                {
                                    result.Truncated = true;
                                    Add(result, "fbx_directory_limit", "Warning", entry.FullName, null, null,
                                        "The preflight reached its directory limit of " + MaximumFbxDirectories + ".", "Select a narrower folder or split the source tree.");
                                    stopDiscovery = true;
                                    pending.Clear();
                                    break;
                                }
                                pending.Push(Tuple.Create((DirectoryInfo)entry, current.Item2 + 1));
                                continue;
                            }
                            if (!string.Equals(Path.GetExtension(entry.Name), ".fbx", StringComparison.OrdinalIgnoreCase)) continue;
                            if (files.Count >= request.MaximumFiles)
                            {
                                result.Truncated = true;
                                Add(result, "fbx_file_limit", "Warning", entry.FullName, null, null,
                                    "The preflight reached its file limit of " + request.MaximumFiles + ".", "Select a narrower folder or split the source tree.");
                                stopDiscovery = true;
                                pending.Clear();
                                break;
                            }
                            var length = ((FileInfo)entry).Length;
                            if (length > MaximumFbxTotalBytes - totalBytes)
                            {
                                result.Truncated = true;
                                Add(result, "fbx_total_bytes_limit", "Warning", entry.FullName, null, null,
                                    "The discovered FBX inputs exceed the total byte limit of " + MaximumFbxTotalBytes + ".", "Select fewer or smaller FBX files.");
                                stopDiscovery = true;
                                pending.Clear();
                                break;
                            }
                            totalBytes += length;
                            files.Add(entry.FullName);
                        }
                    }
                    catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                    {
                        Add(result, "fbx_directory_unreadable", "Warning", current.Item1.FullName, null, null,
                            "A folder could not be enumerated: " + Bound(error.Message, 240), "Check access to this folder; other discovered FBX files remain available.");
                    }
                }
            }

            foreach (var file in files)
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                result.FilesExamined++;
                var fileInfo = new FileInfo(file);
                if (fileInfo.Length > request.MaximumBytesPerFile)
                {
                    Add(result, "fbx_file_size_limit", "Warning", file, null, null,
                        "FBX exceeds the configured per-file read limit of " + request.MaximumBytesPerFile + " bytes.", "Use a smaller file or inspect the file with a suitable external FBX tool.");
                    continue;
                }

                if (IsBinaryFbx(file, request.CancellationToken))
                {
                    Add(result, "fbx_binary_unsupported", "Warning", file, null, null,
                        "Binary FBX was detected but is not parsed by this ASCII declaration preflight.", "Export or save an ASCII FBX copy if text declaration inspection is needed.");
                    continue;
                }

                var modelNames = new List<string>();
                var geometryNames = new List<string>();
                var materialNames = new List<string>();
                var sceneAuxiliaryNames = new List<string>();
                var sceneAuxiliaryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var headerFound = false;
                var headerLines = 0;
                var objectRecords = 0;
                var objectLimitReached = false;
                try
                {
                    using (var reader = new StreamReader(file, new UTF8Encoding(false, true), true))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            request.CancellationToken.ThrowIfCancellationRequested();
                            if (!headerFound && headerLines < 20)
                            {
                                headerLines++;
                                if (line.TrimStart('\uFEFF').TrimStart().StartsWith("; FBX ", StringComparison.OrdinalIgnoreCase)) headerFound = true;
                            }
                            var match = FbxObjectPattern.Match(line);
                            if (!match.Success) continue;
                            objectRecords++;
                            if (objectRecords > MaximumFbxObjectRecordsPerFile)
                            {
                                result.Truncated = true;
                                objectLimitReached = true;
                                break;
                            }
                            var name = FbxName(match.Groups["qualified"].Value);
                            var subtype = FbxName(match.Groups["subtype"].Value);
                            if (string.IsNullOrWhiteSpace(name)) continue;
                            var kind = match.Groups["kind"].Value;
                            if (kind.Equals("Model", StringComparison.OrdinalIgnoreCase) && subtype.Equals("Mesh", StringComparison.OrdinalIgnoreCase)) modelNames.Add(name);
                            else if (kind.Equals("Model", StringComparison.OrdinalIgnoreCase) &&
                                (subtype.Equals("Camera", StringComparison.OrdinalIgnoreCase) || subtype.Equals("Light", StringComparison.OrdinalIgnoreCase)))
                            {
                                int count;
                                sceneAuxiliaryCounts.TryGetValue(subtype, out count);
                                sceneAuxiliaryCounts[subtype] = count + 1;
                                if (sceneAuxiliaryNames.Count < 16) sceneAuxiliaryNames.Add(subtype + "::" + name);
                            }
                            else if (kind.Equals("Geometry", StringComparison.OrdinalIgnoreCase) && subtype.Equals("Mesh", StringComparison.OrdinalIgnoreCase)) geometryNames.Add(name);
                            else if (kind.Equals("Material", StringComparison.OrdinalIgnoreCase)) materialNames.Add(name);
                        }
                    }
                }
                catch (DecoderFallbackException)
                {
                    Add(result, "fbx_encoding_unsupported", "Warning", file, null, null,
                        "The file is not valid UTF-8/ASCII text.", "Save an ASCII FBX using a supported text encoding or inspect it in Resource Browser.");
                    continue;
                }
                catch (IOException error)
                {
                    Add(result, "fbx_read_failed", "Warning", file, null, null,
                        "The FBX could not be read: " + Bound(error.Message, 240), "Check file access and try again.");
                    continue;
                }

                if (!headerFound)
                {
                    Add(result, "fbx_ascii_header_missing", "Warning", file, null, null,
                        "A recognizable FBX ASCII header was not found; declarations were not trusted.", "Select a valid FBX file or inspect it in Resource Browser.");
                    continue;
                }
                if (objectLimitReached)
                    Add(result, "fbx_object_record_limit", "Warning", file, null, null,
                        "The preflight stopped after " + MaximumFbxObjectRecordsPerFile + " FBX object declarations.", "Split the input or use a purpose-built FBX inspection tool.");

                var meshNames = geometryNames.Count > 0 ? geometryNames : modelNames;
                var meshSource = geometryNames.Count > 0 ? "Geometry" : modelNames.Count > 0 ? "Model fallback" : "none";
                var distinctMaterials = materialNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                var meshPreview = string.Join(", ", meshNames.Take(16));
                var materialPreview = string.Join(", ", distinctMaterials.Take(12));
                var evidence = "ASCII declarations: Model=" + modelNames.Count + ", Geometry=" + geometryNames.Count + ", Material=" + materialNames.Count + ". Mesh names use " + meshSource + ": " + (meshPreview.Length == 0 ? "none" : meshPreview) + (meshNames.Count > 16 ? ", …" : "") + ". Material names: " + (materialPreview.Length == 0 ? "none" : materialPreview) + (distinctMaterials.Length > 12 ? ", …" : "");
                Add(result, "fbx_declarations", "Info", file, null, null, Bound(evidence, 1800),
                    "Treat names as inspection hints. Verify geometry, material assignments, transforms, and import behavior separately in Resource Browser.");
                if (sceneAuxiliaryCounts.Count > 0)
                {
                    var auxiliaryEvidence = string.Join(", ", sceneAuxiliaryCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(pair => pair.Key + "=" + pair.Value));
                    Add(result, "fbx_scene_objects_review", "Warning", file, null, null,
                        "ASCII model declarations include scene objects outside mesh geometry: " + auxiliaryEvidence +
                        ". Names: " + (sceneAuxiliaryNames.Count == 0 ? "none retained" : string.Join(", ", sceneAuxiliaryNames)) +
                        (sceneAuxiliaryCounts.Values.Sum() > sceneAuxiliaryNames.Count ? ", …" : "") + ".",
                        "Review these objects in the source scene. If they are unintended, re-export only the selected objects; this preflight never deletes or rewrites FBX nodes.");
                }
                if (objectLimitReached) continue;
                if (meshNames.Count == 0)
                    Add(result, "fbx_mesh_declarations_missing", "Warning", file, null, null,
                        "No Model/Geometry records with subtype Mesh were found in the parsed declarations.", "Confirm the FBX contains the expected mesh objects.");
                if (distinctMaterials.Length > 0)
                    Add(result, "fbx_material_resolution_unchecked", "Info", file, null, null,
                        "Material names were listed but were not resolved against module resources or material assignments.", "Inspect assignments and verify the matching resources in the target module.");

                AnalyzeFbxLodNames(file, meshNames, result, request.CancellationToken);
            }

            if (result.FilesExamined == 0)
                Add(result, "fbx_none", "Warning", request.TargetPath, null, null,
                    "No FBX files were discovered in the selected input.", "Select an .fbx file or a folder containing FBX files.");
            if (files.Count > 0)
                Add(result, "fbx_import_not_run", "Info", request.TargetPath, null, null,
                    "This analysis only reads bounded text declarations. No engine import, editor, or compiler was run.",
                    "Use Resource Browser to import assets and verify the resulting resource package separately.");
        }

        static bool IsBinaryFbx(string file, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length < FbxBinaryMagic.Length) return false;
                for (var index = 0; index < FbxBinaryMagic.Length; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (stream.ReadByte() != FbxBinaryMagic[index]) return false;
                }
                return true;
            }
        }

        static string FbxName(string value)
        {
            var separator = value.IndexOf("::", StringComparison.Ordinal);
            if (separator >= 0) value = value.Substring(separator + 2);
            return value.Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        static void AnalyzeFbxLodNames(string file, IList<string> meshNames, ForgeAnalysisResult result, CancellationToken cancellationToken)
        {
            var bases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var groups = new Dictionary<string, SortedSet<int>>(StringComparer.OrdinalIgnoreCase);
            var duplicateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var meshName in meshNames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!seenNames.Add(meshName)) duplicateNames.Add(meshName);
                var match = FbxLodPattern.Match(meshName);
                if (!match.Success || !int.TryParse(match.Groups["level"].Value, out var level))
                {
                    bases.Add(meshName);
                    continue;
                }
                var baseName = match.Groups["base"].Value;
                if (!groups.TryGetValue(baseName, out var levels)) groups[baseName] = levels = new SortedSet<int>();
                levels.Add(level);
            }
            foreach (var name in duplicateNames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Add(result, "fbx_duplicate_geometry_name", "Warning", file, null, null,
                    "Repeated geometry declaration name: " + name + ".", "Confirm whether the duplicate is intentional in the editor.");
            }
            foreach (var pair in groups)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var values = pair.Value.ToArray();
                if (!bases.Contains(pair.Key) && !pair.Value.Contains(0))
                    Add(result, "fbx_lod_base_missing", "Warning", file, null, null,
                        "LOD name group '" + pair.Key + "' has no unsuffixed base or explicit LOD 0.", "Confirm the intended base and LOD chain in Resource Browser.");
                var expected = pair.Value.Contains(0) ? 0 : 1;
                var last = values.Length == 0 ? expected : values[values.Length - 1];
                for (; expected < last; expected++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!pair.Value.Contains(expected))
                        Add(result, "fbx_lod_gap", "Warning", file, null, null,
                            "LOD name group '" + pair.Key + "' skips level " + expected + ".", "Confirm the intended LOD chain in Resource Browser.");
                }
            }
        }

        static void AnalyzeAssets(ForgeAnalysisRequest request, ForgeAnalysisResult result)
        {
            var xmlFiles = Files(request, "*.xml", result).ToArray();
            foreach (var file in xmlFiles)
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                result.FilesExamined++;
                XDocument doc;
                try { doc = LoadXml(file, request.MaximumBytesPerFile, request.CancellationToken); }
                catch (XmlException error) { Add(result, "asset_xml", "Error", file, error.LineNumber, error.LinePosition, error.Message, "Correct the asset XML."); continue; }
                var root = doc.Root == null ? "" : doc.Root.Name.LocalName;
                if (root == "module_sounds")
                {
                    var seenSounds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var sound in doc.Descendants("module_sound"))
                    {
                        request.CancellationToken.ThrowIfCancellationRequested();
                        var name = (string)sound.Attribute("name") ?? "";
                        if (!string.IsNullOrWhiteSpace(name) && !seenSounds.Add(name))
                            Add(result, "audio_duplicate", "Error", file, null, null, "Duplicate sound name: " + name, "Ensure sound names are unique.");
                        var category = (string)sound.Attribute("sound_category");
                        if (!new[] { "ui", "mission_combat", "ambient", "voice" }.Contains(category ?? "", StringComparer.OrdinalIgnoreCase))
                            Add(result, "audio_category", "Error", file, null, null, "Unsupported sound category: " + (category ?? "missing"), "Use a documented mixer category.");
                        var path = (string)sound.Attribute("path") ?? "";
                        if (string.IsNullOrWhiteSpace(path))
                            Add(result, "audio_path_empty", "Error", file, null, null, "Sound definition has empty path attribute.", "Provide relative audio asset path.");
                        else if (!path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                            Add(result, "audio_extension", "Warning", file, null, null, "Sound path has no .ogg or .wav extension.", "Use a supported sound asset extension.");
                    }
                }
                if (root == "NPCCharacters")
                    foreach (var troop in doc.Descendants("NPCCharacter"))
                    {
                        request.CancellationToken.ThrowIfCancellationRequested();
                        var age = (string)troop.Attribute("age");
                        if (!string.IsNullOrWhiteSpace(age) && !int.TryParse(age, out _))
                            Add(result, "troop_age", "Error", file, null, null, "NPCCharacter age is not an integer: " + age, "Use an integer age.");
                    }
                if (root.IndexOf("Item", StringComparison.OrdinalIgnoreCase) >= 0)
                    foreach (var item in doc.Descendants())
                    {
                        request.CancellationToken.ThrowIfCancellationRequested();
                        if (!item.Name.LocalName.Equals("Item", StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.IsNullOrWhiteSpace((string)item.Attribute("id")))
                            Add(result, "item_id", "Error", file, null, null, "An item has no id attribute.", "Assign a unique item ID.");
                    }
            }
            AnalyzeSpritePackages(request, result,
                new HashSet<string>(xmlFiles.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase), request.CancellationToken);
        }

        static void AnalyzeSpritePackages(ForgeAnalysisRequest request, ForgeAnalysisResult result, ISet<string> analyzedXmlFiles, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var selectedDirectory = Directory.Exists(request.TargetPath)
                ? request.TargetPath
                : Path.GetDirectoryName(request.TargetPath);
            var moduleRoot = FindSpriteModuleRoot(selectedDirectory);
            if (moduleRoot == null) return;

            var configPath = Path.Combine(moduleRoot, "GUI", "SpriteParts", "Config.xml");
            if (!analyzedXmlFiles.Contains(configPath))
            {
                if (result.FilesExamined >= request.MaximumFiles)
                {
                    result.Truncated = true;
                    Add(result, "sprite_scan_limit", "Warning", configPath, null, null,
                        "Sprite readiness checks did not run because the analysis file limit was reached before the category configuration could be read.",
                        "Increase the analysis file limit to include the sprite category configuration.");
                    return;
                }
                result.FilesExamined++;
            }
            XDocument config;
            try { config = LoadXml(configPath, request.MaximumBytesPerFile, cancellationToken); }
            catch (XmlException error)
            {
                if (!analyzedXmlFiles.Contains(configPath))
                    Add(result, "asset_xml", "Error", configPath, error.LineNumber, error.LinePosition, error.Message, "Correct the sprite category configuration XML.");
                return;
            }
            catch (InvalidDataException error)
            {
                if (!analyzedXmlFiles.Contains(configPath))
                    Add(result, "asset_xml", "Warning", configPath, null, null, error.Message, "Select a smaller sprite category configuration or raise the documented analyzer limit.");
                return;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Add(result, "sprite_scan_unavailable", "Warning", configPath, null, null,
                    "The sprite category configuration could not be read: " + Bound(error.Message, 256),
                    "Check access to the selected module folder and rerun the asset analysis.");
                return;
            }

            var maximumCategories = Math.Min(request.MaximumFiles, 128);
            var categories = new List<string>();
            var seenCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in config.Descendants("SpriteCategory"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (item.Element("AlwaysLoad") == null) continue;
                var name = (string)item.Attribute("Name");
                if (string.IsNullOrWhiteSpace(name) || !seenCategories.Add(name)) continue;
                categories.Add(name);
                if (categories.Count > maximumCategories) break;
            }
            if (categories.Count > maximumCategories)
            {
                result.Truncated = true;
                categories.RemoveRange(maximumCategories, categories.Count - maximumCategories);
                Add(result, "sprite_scan_limit", "Warning", configPath, null, null,
                    "Sprite readiness checks reached the configured category limit.",
                    "Increase the analysis file limit to inspect every always-loaded sprite category.");
            }
            if (categories.Count == 0) return;

            var spriteSheetMetadata = ReadSpriteSheetCounts(request, result, moduleRoot, analyzedXmlFiles, categories, cancellationToken);

            var sourceDirectory = Path.Combine(moduleRoot, "AssetSources", "GauntletUI");
            if (!Directory.Exists(sourceDirectory))
            {
                Add(result, "sprite_atlas_missing", "Warning", sourceDirectory, null, null,
                    "An always-loaded sprite category has no GauntletUI source directory.",
                    "Run the Bannerlord SpriteSheetGenerator for the selected module before importing it in Resource Browser.");
                return;
            }

            var remainingFiles = request.MaximumFiles - result.FilesExamined;
            if (remainingFiles <= 0)
            {
                result.Truncated = true;
                Add(result, "sprite_scan_limit", "Warning", sourceDirectory, null, null,
                    "Sprite readiness checks did not run because the analysis file limit was reached.",
                    "Increase the analysis file limit to include generated atlases in this scan.");
                return;
            }

            var maximumAtlases = Math.Min(remainingFiles / 2, 512);
            if (maximumAtlases <= 0)
            {
                result.Truncated = true;
                Add(result, "sprite_scan_limit", "Warning", sourceDirectory, null, null,
                    "Sprite readiness checks need room for both an atlas and its runtime-package path, but the analysis file limit was reached.",
                    "Increase the analysis file limit to include sprite atlas/package pairs.");
                return;
            }
            string[] atlases;
            try
            {
                var discoveredAtlases = new List<string>(maximumAtlases + 1);
                foreach (var atlasPath in Directory.EnumerateFiles(sourceDirectory, "*.png", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    discoveredAtlases.Add(atlasPath);
                    if (discoveredAtlases.Count > maximumAtlases) break;
                }
                atlases = discoveredAtlases.ToArray();
                if (atlases.Length > maximumAtlases)
                {
                    atlases = atlases.Take(maximumAtlases).ToArray();
                    result.Truncated = true;
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Add(result, "sprite_scan_unavailable", "Warning", sourceDirectory, null, null,
                    "The sprite source directory could not be enumerated: " + Bound(error.Message, 256),
                    "Check access to the selected module folder and rerun the asset analysis.");
                return;
            }
            result.FilesExamined += atlases.Length;
            var atlasListTruncated = result.Truncated;

            var packagesDirectory = Path.Combine(moduleRoot, "Assets", "GauntletUI");
            foreach (var category in categories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var categoryAtlases = atlases.Where(path => IsSpriteAtlas(path, category)).ToArray();
                SpriteCategoryMetadata categoryMetadata = null;
                if (spriteSheetMetadata != null) spriteSheetMetadata.TryGetValue(category, out categoryMetadata);
                if (!atlasListTruncated && categoryMetadata != null && categoryAtlases.Length != categoryMetadata.SheetCount)
                    Add(result, "sprite_atlas_count_mismatch", "Error", Path.Combine(moduleRoot, "GUI", new DirectoryInfo(moduleRoot).Name + "SpriteData.xml"), null, null,
                        "SpriteData declares " + categoryMetadata.SheetCount.ToString(CultureInfo.InvariantCulture) + " sheet(s) for " + category + " but " + categoryAtlases.Length.ToString(CultureInfo.InvariantCulture) + " matching source atlas file(s) were found.",
                        "Regenerate the sprite sheet with Bannerlord's SpriteSheetGenerator, then reimport its matching texture package with Resource Browser.");
                if (categoryAtlases.Length == 0)
                {
                    Add(result, "sprite_atlas_missing", "Warning", sourceDirectory, null, null,
                        "No generated sprite-sheet PNG was found for always-loaded category " + category + ".",
                        "Run Bannerlord's SpriteSheetGenerator for this module, then rerun the asset analysis.");
                    continue;
                }

                foreach (var atlas in categoryAtlases)
                {
                    result.FilesExamined++;
                    ValidateSpriteAtlas(atlas, category, categoryMetadata, result, cancellationToken);
                    var package = Path.Combine(packagesDirectory, Path.GetFileNameWithoutExtension(atlas) + "_tex.tpac");
                    if (!File.Exists(package))
                    {
                        Add(result, "sprite_tpac_missing", "Warning", atlas, null, null,
                            "Generated source atlas has no compiled runtime package: " + package,
                            "At the Bannerlord main menu, run resource.show_resource_browser, select this module, open Assets/GauntletUI, scan new asset files, and import " + Path.GetFileName(atlas) + ". Verify the non-empty _tex.tpac exists before packaging. For Calradia Forge, run tools/Prepare-CalradiaForge-ResourceBrowser.bat before import and use --collect-tpac afterward when the installed module is separate from the source tree; the helper reports version differences and never copies DLLs or manifests.");
                    }
                    else if (new FileInfo(package).Length == 0)
                    {
                        Add(result, "sprite_tpac_empty", "Error", package, null, null,
                            "The runtime sprite package exists but is empty.",
                            "Reimport the matching source atlas with Bannerlord Resource Browser, collect it back with tools/Prepare-CalradiaForge-ResourceBrowser.bat --collect-tpac when using this repository, and rerun the asset analysis.");
                    }
                    else
                    {
                        try
                        {
                            var packageInfo = new FileInfo(package);
                            using (var stream = new FileStream(package, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                            {
                                var header = new byte[SpritePackageFixedHeaderLength];
                                var headerBytesRead = 0;
                                while (headerBytesRead < header.Length)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    var read = stream.Read(header, headerBytesRead, header.Length - headerBytesRead);
                                    if (read == 0) break;
                                    headerBytesRead += read;
                                }
                                if (headerBytesRead < 4 || header[0] != (byte)'T' || header[1] != (byte)'P' || header[2] != (byte)'A' || header[3] != (byte)'C')
                                {
                                    Add(result, "sprite_tpac_header_invalid", "Error", package, null, null,
                                        "The runtime sprite package is non-empty but does not start with the TPAC marker.",
                                        "Reimport the matching source atlas with Bannerlord Resource Browser, then collect and recheck the generated _tex.tpac file.");
                                }
                                else if (headerBytesRead < header.Length)
                                {
                                    Add(result, "sprite_tpac_header_truncated", "Error", package, null, null,
                                        "The runtime sprite package has the TPAC marker but only " + headerBytesRead.ToString(CultureInfo.InvariantCulture) + " of the 36 fixed header bytes are readable.",
                                        "Reimport the matching source atlas with Bannerlord Resource Browser and verify that the generated _tex.tpac is complete before collecting or packaging it.");
                                }
                                else
                                {
                                    var formatVersion = BitConverter.ToUInt32(header, 4);
                                    if (formatVersion != 2)
                                    {
                                        Add(result, "sprite_tpac_version_unsupported", "Warning", package, null, null,
                                            "The TPAC marker is present, but this analyzer only understands the version 2 header; the package declares version " + formatVersion.ToString(CultureInfo.InvariantCulture) + ". No version-specific fields were interpreted.",
                                            "Use a TPAC reader and validation workflow that explicitly supports version " + formatVersion.ToString(CultureInfo.InvariantCulture) + ". This result does not establish that the package is malformed.");
                                    }
                                    else
                                    {
                                        var entryCount = BitConverter.ToUInt32(header, 24);
                                        var tableOfContentsSize = BitConverter.ToUInt64(header, 28);
                                        var maximumTableSize = (ulong)Math.Max(0, packageInfo.Length - SpritePackageFixedHeaderLength);
                                        if (entryCount == 0 || entryCount > MaximumSpritePackageEntryCount)
                                        {
                                            Add(result, "sprite_tpac_asset_count_invalid", "Error", package, null, null,
                                                "The TPAC v2 fixed header declares " + entryCount.ToString(CultureInfo.InvariantCulture) + " asset metadata entries; the supported bounded range is 1 to " + MaximumSpritePackageEntryCount.ToString(CultureInfo.InvariantCulture) + ".",
                                                "Validate this file with a TPAC v2 metadata reader compatible with the game version before collecting or packaging it. A structural finding alone does not identify how the package was produced.");
                                        }
                                        else if (tableOfContentsSize == 0 || tableOfContentsSize > maximumTableSize)
                                        {
                                            Add(result, "sprite_tpac_toc_size_invalid", "Error", package, null, null,
                                                "The TPAC v2 header declares a table of contents of " + tableOfContentsSize.ToString(CultureInfo.InvariantCulture) + " bytes, which is empty or extends beyond the " + packageInfo.Length.ToString(CultureInfo.InvariantCulture) + " byte file.",
                                                "Validate this file with a TPAC v2 metadata reader compatible with the game version before collecting or packaging it.");
                                        }
                                        else
                                        {
                                            Add(result, "sprite_tpac_header_marker", "Info", package, null, null,
                                                "The 36-byte TPAC v2 header is readable in a " + packageInfo.Length.ToString(CultureInfo.InvariantCulture) + " byte runtime package, declares " + entryCount.ToString(CultureInfo.InvariantCulture) + " asset metadata entr" + (entryCount == 1 ? "y" : "ies") + ", and bounds its table of contents to " + tableOfContentsSize.ToString(CultureInfo.InvariantCulture) + " bytes. This structural preflight does not parse entries or decode texture payloads.",
                                                "Review the Resource Browser import result, then run tools/Inspect-CalradiaForge-Tpac.bat --validate-only with a TPAC v2 reader compatible with the game version. Treat a reader failure as unverified metadata; it does not establish that the package is malformed.");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                        {
                            Add(result, "sprite_tpac_unreadable", "Warning", package, null, null,
                                "The runtime sprite package could not be read: " + Bound(error.Message, 256),
                                "Check file access and rerun the asset analysis.");
                        }
                    }
                }
            }
            if (result.Truncated)
                Add(result, "sprite_scan_limit", "Warning", sourceDirectory, null, null,
                    "The sprite package scan was truncated by its configured bounds.",
                    "Increase the analysis file limit if this module has more generated sprite sheets or categories.");
        }

        sealed class SpriteSheetSize
        {
            public int Width;
            public int Height;
        }

        sealed class SpriteCategoryMetadata
        {
            public int SheetCount;
            public readonly Dictionary<int, SpriteSheetSize> Sheets = new Dictionary<int, SpriteSheetSize>();
        }

        static Dictionary<string, SpriteCategoryMetadata> ReadSpriteSheetCounts(ForgeAnalysisRequest request, ForgeAnalysisResult result, string moduleRoot,
            ISet<string> analyzedXmlFiles, IEnumerable<string> requiredCategories, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var guiDirectory = Path.Combine(moduleRoot, "GUI");
            var metadataPath = Path.Combine(guiDirectory, new DirectoryInfo(moduleRoot).Name + "SpriteData.xml");
            var fullMetadataPath = Path.GetFullPath(metadataPath);
            if (!File.Exists(metadataPath))
            {
                Add(result, "sprite_data_missing", "Warning", metadataPath, null, null,
                    "No generated SpriteData XML was found for the module's sprite categories.",
                    "Run Bannerlord's SpriteSheetGenerator for this module, then rerun the asset analysis.");
                return null;
            }

            if (!analyzedXmlFiles.Contains(fullMetadataPath))
            {
                if (result.FilesExamined >= request.MaximumFiles)
                {
                    result.Truncated = true;
                    Add(result, "sprite_scan_limit", "Warning", metadataPath, null, null,
                        "Sprite metadata checks did not run because the analysis file limit was reached.",
                        "Increase the analysis file limit to include the module SpriteData XML.");
                    return null;
                }
                result.FilesExamined++;
            }

            XDocument spriteData;
            try { spriteData = LoadXml(metadataPath, request.MaximumBytesPerFile, cancellationToken); }
            catch (XmlException error)
            {
                Add(result, "sprite_data_xml", "Error", metadataPath, error.LineNumber, error.LinePosition, error.Message,
                    "Regenerate or correct the module SpriteData XML with Bannerlord's SpriteSheetGenerator.");
                return null;
            }
            catch (InvalidDataException error)
            {
                Add(result, "sprite_data_limit", "Warning", metadataPath, null, null, error.Message,
                    "Select a smaller SpriteData XML or raise the documented analyzer limit.");
                return null;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Add(result, "sprite_data_unavailable", "Warning", metadataPath, null, null, "SpriteData XML could not be read: " + Bound(error.Message, 256),
                    "Check access to the selected module folder and rerun the asset analysis.");
                return null;
            }

            var counts = new Dictionary<string, SpriteCategoryMetadata>(StringComparer.OrdinalIgnoreCase);
            foreach (var category in spriteData.Descendants("SpriteCategory"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = (string)category.Element("Name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                var countText = (string)category.Element("SpriteSheetCount");
                if (!int.TryParse(countText, NumberStyles.None, CultureInfo.InvariantCulture, out var sheetCount) || sheetCount < 1)
                {
                    Add(result, "sprite_sheet_count_invalid", "Error", metadataPath, null, null,
                        "SpriteData has a missing or invalid SpriteSheetCount for " + name + ".",
                        "Regenerate the sprite metadata with Bannerlord's SpriteSheetGenerator.");
                    continue;
                }
                if (counts.ContainsKey(name))
                    Add(result, "sprite_category_duplicate", "Error", metadataPath, null, null,
                        "SpriteData declares category " + name + " more than once.",
                        "Remove duplicate category entries by regenerating SpriteData from the module's SpriteParts configuration.");
                else
                {
                    var info = new SpriteCategoryMetadata { SheetCount = sheetCount };
                    foreach (var size in category.Elements("SpriteSheetSize"))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var idText = (string)size.Attribute("ID");
                        var widthText = (string)size.Attribute("Width");
                        var heightText = (string)size.Attribute("Height");
                        if (!int.TryParse(idText, NumberStyles.None, CultureInfo.InvariantCulture, out var sheetId) || sheetId < 1 ||
                            !int.TryParse(widthText, NumberStyles.None, CultureInfo.InvariantCulture, out var width) || width < 1 ||
                            !int.TryParse(heightText, NumberStyles.None, CultureInfo.InvariantCulture, out var height) || height < 1)
                        {
                            Add(result, "sprite_sheet_size_invalid", "Error", metadataPath, null, null,
                                "SpriteData has an invalid sheet ID or non-positive dimensions for " + name + ".",
                                "Regenerate SpriteData with Bannerlord's SpriteSheetGenerator and verify every sheet size is positive.");
                            continue;
                        }
                        if (info.Sheets.ContainsKey(sheetId))
                        {
                            Add(result, "sprite_sheet_size_duplicate", "Error", metadataPath, null, null,
                                "SpriteData declares sheet " + sheetId.ToString(CultureInfo.InvariantCulture) + " more than once for " + name + ".",
                                "Regenerate SpriteData so each category has one size entry per sheet ID.");
                            continue;
                        }
                        info.Sheets.Add(sheetId, new SpriteSheetSize { Width = width, Height = height });
                    }
                    if (info.Sheets.Count != sheetCount)
                        Add(result, "sprite_sheet_size_missing", "Error", metadataPath, null, null,
                            "SpriteData declares " + sheetCount.ToString(CultureInfo.InvariantCulture) + " sheet(s) for " + name + " but provides dimensions for " + info.Sheets.Count.ToString(CultureInfo.InvariantCulture) + ".",
                            "Regenerate the complete SpriteData metadata with Bannerlord's SpriteSheetGenerator.");
                    counts.Add(name, info);
                }
            }

            ValidateSpriteParts(request, result, moduleRoot, metadataPath, spriteData, counts, cancellationToken);

            foreach (var categoryName in requiredCategories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!counts.ContainsKey(categoryName))
                    Add(result, "sprite_category_metadata_missing", "Error", metadataPath, null, null,
                        "An always-loaded category is missing from SpriteData: " + categoryName + ".",
                        "Regenerate SpriteData after adding the category to GUI/SpriteParts/Config.xml.");
            }
            return counts;
        }

        static void ValidateSpriteParts(ForgeAnalysisRequest request, ForgeAnalysisResult result, string moduleRoot, string metadataPath,
            XDocument spriteData, IDictionary<string, SpriteCategoryMetadata> categories, CancellationToken cancellationToken)
        {
            var maximumParts = Math.Min(request.MaximumFiles, 2000);
            var parts = new List<XElement>(maximumParts + 1);
            foreach (var part in spriteData.Descendants("SpritePart"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                parts.Add(part);
                if (parts.Count > maximumParts) break;
            }
            if (parts.Count > maximumParts)
            {
                result.Truncated = true;
                parts.RemoveRange(maximumParts, parts.Count - maximumParts);
                Add(result, "sprite_scan_limit", "Warning", metadataPath, null, null,
                    "Sprite-part metadata checks reached the configured record limit.",
                    "Increase the asset analysis file limit to inspect all SpritePart entries.");
            }

            var seenParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in parts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = (string)part.Element("Name");
                var categoryName = (string)part.Element("CategoryName");
                var sheetText = (string)part.Element("SheetID");
                var widthText = (string)part.Element("Width");
                var heightText = (string)part.Element("Height");
                var xText = (string)part.Element("SheetX");
                var yText = (string)part.Element("SheetY");
                if (!IsSafeSpriteName(name) || !IsSafeSpriteName(categoryName) ||
                    !int.TryParse(sheetText, NumberStyles.None, CultureInfo.InvariantCulture, out var sheetId) || sheetId < 1 ||
                    !int.TryParse(widthText, NumberStyles.None, CultureInfo.InvariantCulture, out var width) || width < 1 ||
                    !int.TryParse(heightText, NumberStyles.None, CultureInfo.InvariantCulture, out var height) || height < 1 ||
                    !int.TryParse(xText, NumberStyles.None, CultureInfo.InvariantCulture, out var x) || x < 0 ||
                    !int.TryParse(yText, NumberStyles.None, CultureInfo.InvariantCulture, out var y) || y < 0)
                {
                    Add(result, "sprite_part_metadata_invalid", "Error", metadataPath, null, null,
                        "SpriteData contains a part with an unsafe name or invalid sheet, size, or coordinate value.",
                        "Regenerate SpriteData from the module's SpriteParts folder and correct the reported entry.");
                    continue;
                }

                var partKey = categoryName + "/" + name;
                if (!seenParts.Add(partKey))
                    Add(result, "sprite_part_duplicate", "Error", metadataPath, null, null,
                        "SpriteData declares the part " + partKey + " more than once.",
                        "Remove duplicate sprite-part entries and regenerate SpriteData.");

                if (!categories.TryGetValue(categoryName, out var category) || !category.Sheets.TryGetValue(sheetId, out var sheet))
                {
                    Add(result, "sprite_part_sheet_missing", "Error", metadataPath, null, null,
                        "Sprite part " + partKey + " refers to an undeclared category or sheet ID " + sheetId.ToString(CultureInfo.InvariantCulture) + ".",
                        "Regenerate SpriteData so each part references an existing category and sheet.");
                    continue;
                }

                if ((long)x + width > sheet.Width || (long)y + height > sheet.Height)
                    Add(result, "sprite_part_outside_sheet", "Error", metadataPath, null, null,
                        "Sprite part " + partKey + " at (" + x.ToString(CultureInfo.InvariantCulture) + ", " + y.ToString(CultureInfo.InvariantCulture) + ") with size " + width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) + " exceeds sheet " + sheet.Width.ToString(CultureInfo.InvariantCulture) + "x" + sheet.Height.ToString(CultureInfo.InvariantCulture) + ".",
                        "Rebuild the atlas with SpriteSheetGenerator or correct the part rectangle in SpriteData.");

                var partPath = Path.Combine(moduleRoot, "GUI", "SpriteParts", categoryName, name + ".png");
                if (!File.Exists(partPath))
                {
                    Add(result, "sprite_part_missing", "Error", partPath, null, null,
                        "SpriteData references a sprite part whose source PNG is missing.",
                        "Restore the attributed source PNG or remove the stale SpritePart entry, then regenerate the atlas.");
                    continue;
                }
                if (!TryReadPngDimensions(partPath, cancellationToken, out var imageWidth, out var imageHeight))
                {
                    Add(result, "sprite_part_png_header", "Error", partPath, null, null,
                        "The source sprite does not contain a readable PNG signature and IHDR dimension header.",
                        "Regenerate or replace this sprite part with a valid PNG image.");
                    continue;
                }
                if (imageWidth != width || imageHeight != height)
                    Add(result, "sprite_part_png_dimensions", "Error", partPath, null, null,
                        "SpriteData expects " + width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) + " but the PNG IHDR reports " + imageWidth.ToString(CultureInfo.InvariantCulture) + "x" + imageHeight.ToString(CultureInfo.InvariantCulture) + ".",
                        "Regenerate SpriteData from the current source PNG dimensions.");
            }
        }

        static bool IsSafeSpriteName(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value != "." && value != ".." &&
                value.IndexOfAny(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }) < 0 &&
                value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !value.Any(char.IsControl);
        }

        static void ValidateSpriteAtlas(string atlasPath, string categoryName, SpriteCategoryMetadata category,
            ForgeAnalysisResult result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (category == null) return;
            var stem = Path.GetFileNameWithoutExtension(atlasPath);
            var suffix = stem.Substring((categoryName + "_").Length);
            if (!int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var sheetId) ||
                !category.Sheets.TryGetValue(sheetId, out var expected)) return;
            if (!TryReadPngDimensions(atlasPath, cancellationToken, out var width, out var height))
            {
                Add(result, "sprite_atlas_png_header", "Error", atlasPath, null, null,
                    "The generated source atlas does not contain a readable PNG signature and IHDR dimension header.",
                    "Rerun Bannerlord's SpriteSheetGenerator for this module and replace the malformed atlas.");
                return;
            }
            if (width != expected.Width || height != expected.Height)
                Add(result, "sprite_atlas_dimensions_mismatch", "Error", atlasPath, null, null,
                    "SpriteData declares a " + expected.Width.ToString(CultureInfo.InvariantCulture) + "x" + expected.Height.ToString(CultureInfo.InvariantCulture) + " sheet but the atlas PNG IHDR reports " + width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) + ".",
                    "Regenerate SpriteData and the source atlas together with Bannerlord's SpriteSheetGenerator.");
        }

        static bool TryReadPngDimensions(string path, CancellationToken cancellationToken, out int width, out int height)
        {
            cancellationToken.ThrowIfCancellationRequested();
            width = 0;
            height = 0;
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    var header = new byte[24];
                    var read = 0;
                    while (read < header.Length)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var count = stream.Read(header, read, header.Length - read);
                        if (count <= 0) return false;
                        read += count;
                    }
                    if (header[0] != 137 || header[1] != 80 || header[2] != 78 || header[3] != 71 ||
                        header[4] != 13 || header[5] != 10 || header[6] != 26 || header[7] != 10 ||
                        header[8] != 0 || header[9] != 0 || header[10] != 0 || header[11] != 13 ||
                        header[12] != (byte)'I' || header[13] != (byte)'H' || header[14] != (byte)'D' || header[15] != (byte)'R')
                        return false;
                    var rawWidth = ((uint)header[16] << 24) | ((uint)header[17] << 16) | ((uint)header[18] << 8) | header[19];
                    var rawHeight = ((uint)header[20] << 24) | ((uint)header[21] << 16) | ((uint)header[22] << 8) | header[23];
                    if (rawWidth == 0 || rawHeight == 0 || rawWidth > int.MaxValue || rawHeight > int.MaxValue) return false;
                    width = (int)rawWidth;
                    height = (int)rawHeight;
                    return true;
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) { return false; }
        }

        static string FindSpriteModuleRoot(string selectedDirectory)
        {
            if (string.IsNullOrWhiteSpace(selectedDirectory)) return null;
            DirectoryInfo current;
            try { current = new DirectoryInfo(Path.GetFullPath(selectedDirectory)); }
            catch (Exception error) when (error is ArgumentException || error is IOException || error is UnauthorizedAccessException || error is NotSupportedException) { return null; }
            for (var depth = 0; current != null && depth < 10; depth++, current = current.Parent)
                if (File.Exists(Path.Combine(current.FullName, "GUI", "SpriteParts", "Config.xml")))
                    return current.FullName;
            return null;
        }

        static bool IsSpriteAtlas(string path, string category)
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            var prefix = category + "_";
            if (!stem.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            var suffix = stem.Substring(prefix.Length);
            return int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var sheetIndex) && sheetIndex > 0;
        }

        static void AnalyzeWatchdog(ForgeAnalysisRequest request, ForgeAnalysisResult result)
        {
            foreach (var file in Files(request, "*.log", result).Concat(Files(request, "*.txt", result)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                result.FilesExamined++;
                var text = ReadBounded(file, request.MaximumBytesPerFile, request.CancellationToken);
                var lineStart = 0;
                var lineNumber = 1;
                while (lineStart <= text.Length)
                {
                    request.CancellationToken.ThrowIfCancellationRequested();
                    var lineEnd = text.IndexOf('\n', lineStart);
                    if (lineEnd < 0) lineEnd = text.Length;
                    var line = text.Substring(lineStart, lineEnd - lineStart).TrimEnd('\r');
                    if (line.IndexOf("exception", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("fatal", StringComparison.OrdinalIgnoreCase) >= 0)
                        Add(result, "watchdog_event", "Warning", file, lineNumber, null, Bound(line.Trim(), 512), "Inspect the surrounding log lines and module list.");
                    if (lineEnd == text.Length) break;
                    lineStart = lineEnd + 1;
                    lineNumber++;
                }
            }
            if (result.FilesExamined == 0)
                Add(result, "watchdog_none", "Warning", request.TargetPath, null, null, "No .log or .txt watchdog input was found.", "Select a log file or its folder.");
        }

        static void AnalyzeCrashMetadata(ForgeAnalysisRequest request, ForgeAnalysisResult result)
        {
            var files = Files(request, "*.cfcrash", result).Concat(Files(request, "*.dmp", result)).Concat(Files(request, "*.sav", result)).Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                result.FilesExamined++;
                var extension = Path.GetExtension(file).ToLowerInvariant();
                if (extension == ".dmp" || extension == ".sav")
                    Add(result, "binary_metadata_only", "Info", file, null, null,
                        "The file is binary. Only name, size, and timestamp were inspected.", "Use the appropriate game or debugger tool for full binary analysis.");
                else
                {
                    var text = ReadBounded(file, request.MaximumBytesPerFile, request.CancellationToken);
                    if (text.IndexOf("exception", StringComparison.OrdinalIgnoreCase) >= 0)
                        Add(result, "crash_exception", "Warning", file, LineOf(text, "exception", request.CancellationToken), null,
                            "Crash metadata includes an exception marker.", "Export the full report with the module list and stack evidence.");
                    else
                        Add(result, "crash_metadata", "Info", file, null, null, "Crash metadata was readable but contains no exception marker.", "Review the raw payload before assigning a cause.");
                }
            }
            if (result.FilesExamined == 0)
                Add(result, "crash_none", "Warning", request.TargetPath, null, null, "No .cfcrash, .dmp, or .sav input was found.", "Select a crash report or save file.");
        }

        static void AnalyzeArchive(ForgeAnalysisRequest request, ForgeAnalysisResult result)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(request.TargetPath) || !request.TargetPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                Add(result, "archive_input", "Error", request.TargetPath, null, null, "A .zip archive is required.", "Select a release archive.");
                return;
            }
            using (var stream = new FileStream(request.TargetPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > request.MaximumBytesPerFile)
                {
                    result.Truncated = true;
                    Add(result, "archive_size_limit", "Warning", request.TargetPath, null, null,
                        "Archive size " + stream.Length.ToString(CultureInfo.InvariantCulture) + " bytes exceeds the configured inspection limit of " + request.MaximumBytesPerFile.ToString(CultureInfo.InvariantCulture) + " bytes.",
                        "Select an archive within the configured size limit; no ZIP directory entries were inspected.");
                    return;
                }

                ulong archiveEntryCount;
                string directoryError;
                if (!TryReadArchiveEntryCount(stream, out archiveEntryCount, out directoryError))
                {
                    result.Truncated = true;
                    Add(result, "archive_directory_unavailable", "Warning", request.TargetPath, null, null,
                        directoryError,
                        "Use a single-disk ZIP with a readable end-of-central-directory record.");
                    return;
                }
                if (archiveEntryCount > MaximumArchiveDirectoryEntries)
                {
                    result.Truncated = true;
                    Add(result, "archive_entry_limit", "Warning", request.TargetPath, null, null,
                        "Archive directory declares " + archiveEntryCount.ToString(CultureInfo.InvariantCulture) + " entries, exceeding the bounded inspection limit of " + MaximumArchiveDirectoryEntries.ToString(CultureInfo.InvariantCulture) + ".",
                        "Select an archive with no more than " + MaximumArchiveDirectoryEntries.ToString(CultureInfo.InvariantCulture) + " entries; no ZIP entries were materialized.");
                    return;
                }

                stream.Position = 0;
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    var entries = archive.Entries;
                    var entriesToInspect = Math.Min(entries.Count, request.MaximumFiles);
                    for (var index = 0; index < entriesToInspect; index++)
                    {
                        request.CancellationToken.ThrowIfCancellationRequested();
                        var entry = entries[index];
                        result.FilesExamined++;
                        var name = entry.FullName.Replace('\\', '/');
                        if (name.StartsWith("/") || name.Split('/').Any(part => part == ".." || part.Contains(":")))
                            Add(result, "archive_path", "Error", request.TargetPath, null, null, "Unsafe archive entry: " + name, "Rebuild the archive without unsafe paths.");
                        if (name.EndsWith(".sav", StringComparison.OrdinalIgnoreCase) || name.IndexOf("save-backup", StringComparison.OrdinalIgnoreCase) >= 0)
                            Add(result, "archive_save", "Error", request.TargetPath, null, null, "Archive contains save data: " + name, "Remove saves from release archives.");
                        if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && Path.GetFileName(name).StartsWith("TaleWorlds", StringComparison.OrdinalIgnoreCase))
                            Add(result, "archive_game_binary", "Error", request.TargetPath, null, null, "Archive contains a game assembly: " + name, "Do not distribute TaleWorlds assemblies.");
                    }
                    result.Truncated |= entries.Count > result.FilesExamined;
                }
            }
        }

        static bool TryReadArchiveEntryCount(Stream stream, out ulong entryCount, out string error)
        {
            entryCount = 0;
            error = null;
            if (!stream.CanSeek || stream.Length < 22)
            {
                error = "The ZIP end-of-central-directory record could not be inspected; no entries were materialized.";
                return false;
            }

            var originalPosition = stream.Position;
            try
            {
                const int endRecordMinimumLength = 22;
                const int maximumCommentLength = ushort.MaxValue;
                var tailLength = (int)Math.Min(stream.Length, endRecordMinimumLength + maximumCommentLength);
                var tail = new byte[tailLength];
                stream.Position = stream.Length - tailLength;
                if (!ReadExactly(stream, tail))
                {
                    error = "The ZIP end-of-central-directory record was incomplete; no entries were materialized.";
                    return false;
                }

                var endIndex = -1;
                for (var index = tail.Length - endRecordMinimumLength; index >= 0; index--)
                {
                    if (ReadUInt32LittleEndian(tail, index) != 0x06054b50U) continue;
                    if (index + endRecordMinimumLength + ReadUInt16LittleEndian(tail, index + 20) != tail.Length) continue;
                    endIndex = index;
                    break;
                }
                if (endIndex < 0)
                {
                    error = "The ZIP end-of-central-directory record was not found; no entries were materialized.";
                    return false;
                }

                var diskNumber = ReadUInt16LittleEndian(tail, endIndex + 4);
                var centralDirectoryDisk = ReadUInt16LittleEndian(tail, endIndex + 6);
                var entriesOnDisk = ReadUInt16LittleEndian(tail, endIndex + 8);
                var entriesTotal = ReadUInt16LittleEndian(tail, endIndex + 10);
                if (diskNumber != 0 || centralDirectoryDisk != 0 ||
                    (entriesOnDisk != ushort.MaxValue && entriesTotal != ushort.MaxValue && entriesOnDisk != entriesTotal))
                {
                    error = "Multi-disk ZIP archives are not inspected; no entries were materialized.";
                    return false;
                }

                if (entriesTotal != ushort.MaxValue && entriesOnDisk != ushort.MaxValue)
                {
                    if (entriesOnDisk != entriesTotal)
                    {
                        error = "The ZIP entry counts disagree; no entries were materialized.";
                        return false;
                    }
                    entryCount = entriesTotal;
                    return true;
                }

                var absoluteEndRecord = stream.Length - tailLength + endIndex;
                var locator = new byte[20];
                if (absoluteEndRecord < locator.Length || !ReadExactlyAt(stream, absoluteEndRecord - locator.Length, locator) ||
                    ReadUInt32LittleEndian(locator, 0) != 0x07064b50U ||
                    ReadUInt32LittleEndian(locator, 4) != 0 || ReadUInt32LittleEndian(locator, 16) != 1)
                {
                    error = "The ZIP64 entry count could not be validated; no entries were materialized.";
                    return false;
                }

                var zip64EndOffset = ReadUInt64LittleEndian(locator, 8);
                if (zip64EndOffset > long.MaxValue)
                {
                    error = "The ZIP64 directory offset is outside the supported file range; no entries were materialized.";
                    return false;
                }
                var zip64End = new byte[56];
                if (!ReadExactlyAt(stream, (long)zip64EndOffset, zip64End) ||
                    ReadUInt32LittleEndian(zip64End, 0) != 0x06064b50U ||
                    ReadUInt64LittleEndian(zip64End, 4) < 44 ||
                    ReadUInt32LittleEndian(zip64End, 16) != 0 || ReadUInt32LittleEndian(zip64End, 20) != 0)
                {
                    error = "The ZIP64 end-of-central-directory record was invalid; no entries were materialized.";
                    return false;
                }

                var zip64EntriesOnDisk = ReadUInt64LittleEndian(zip64End, 24);
                entryCount = ReadUInt64LittleEndian(zip64End, 32);
                if (zip64EntriesOnDisk != entryCount)
                {
                    error = "The ZIP64 entry counts disagree; no entries were materialized.";
                    return false;
                }
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                error = "The ZIP directory count could not be read (" + exception.Message + "); no entries were materialized.";
                return false;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        static bool ReadExactly(Stream stream, byte[] buffer)
        {
            var offset = 0;
            while (offset < buffer.Length)
            {
                var read = stream.Read(buffer, offset, buffer.Length - offset);
                if (read == 0) return false;
                offset += read;
            }
            return true;
        }

        static bool ReadExactlyAt(Stream stream, long position, byte[] buffer)
        {
            if (position < 0 || position > stream.Length - buffer.Length) return false;
            stream.Position = position;
            return ReadExactly(stream, buffer);
        }

        static ushort ReadUInt16LittleEndian(byte[] bytes, int offset)
        {
            return (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
        }

        static uint ReadUInt32LittleEndian(byte[] bytes, int offset)
        {
            return (uint)(bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24));
        }

        static ulong ReadUInt64LittleEndian(byte[] bytes, int offset)
        {
            return ReadUInt32LittleEndian(bytes, offset) | ((ulong)ReadUInt32LittleEndian(bytes, offset + 4) << 32);
        }

        static IEnumerable<string> Files(ForgeAnalysisRequest request, string pattern, ForgeAnalysisResult result)
        {
            var files = new List<string>();
            var incomplete = false;
            var skippedReparsePoint = false;
            var unreadable = false;

            request.CancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(request.TargetPath))
            {
                try
                {
                    if ((File.GetAttributes(request.TargetPath) & FileAttributes.ReparsePoint) != 0)
                        skippedReparsePoint = true;
                    else if (Path.GetFileName(request.TargetPath).Matches(pattern))
                        files.Add(request.TargetPath);
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is System.Security.SecurityException)
                {
                    unreadable = true;
                }
                AddFileScanNotice(result, request.TargetPath, incomplete, skippedReparsePoint, unreadable);
                return files;
            }

            if (!Directory.Exists(request.TargetPath)) return files;

            var directories = new Stack<Tuple<string, int>>();
            var discoveredDirectories = 1;
            var inspectedEntries = 0;
            Func<bool> tryConsumeEntry = () =>
            {
                if (inspectedEntries >= MaximumAnalysisDirectoryEntries) return false;
                inspectedEntries++;
                return true;
            };
            try
            {
                if ((File.GetAttributes(request.TargetPath) & FileAttributes.ReparsePoint) != 0)
                {
                    skippedReparsePoint = true;
                    AddFileScanNotice(result, request.TargetPath, incomplete, skippedReparsePoint, unreadable);
                    return files;
                }
                directories.Push(Tuple.Create(request.TargetPath, 0));
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is System.Security.SecurityException)
            {
                unreadable = true;
                AddFileScanNotice(result, request.TargetPath, incomplete, skippedReparsePoint, unreadable);
                return files;
            }

            while (directories.Count > 0)
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                var directory = directories.Pop();
                try
                {
                    foreach (var entry in Directory.EnumerateFileSystemEntries(directory.Item1, "*", SearchOption.TopDirectoryOnly))
                    {
                        var probe = DirectoryEntryProbe.Inspect(entry, request.CancellationToken, tryConsumeEntry, out var attributes);
                        if (probe == DirectoryEntryProbeResult.EntryLimitReached)
                        {
                            incomplete = true;
                            break;
                        }
                        if (probe == DirectoryEntryProbeResult.Unreadable)
                        {
                            unreadable = true;
                            continue;
                        }
                        if (probe == DirectoryEntryProbeResult.ReparsePoint)
                        {
                            skippedReparsePoint = true;
                            continue;
                        }

                        if ((attributes & FileAttributes.Directory) != 0)
                        {
                            if (directory.Item2 >= MaximumAnalysisDirectoryDepth || discoveredDirectories >= MaximumAnalysisDirectories)
                            {
                                incomplete = true;
                                continue;
                            }
                            discoveredDirectories++;
                            directories.Push(Tuple.Create(entry, directory.Item2 + 1));
                            continue;
                        }

                        if (!Path.GetFileName(entry).Matches(pattern)) continue;
                        if (files.Count >= request.MaximumFiles)
                        {
                            incomplete = true;
                            break;
                        }
                        files.Add(entry);
                    }
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is System.Security.SecurityException)
                {
                    unreadable = true;
                }

                if ((inspectedEntries >= MaximumAnalysisDirectoryEntries && directories.Count > 0) ||
                    (files.Count >= request.MaximumFiles && incomplete))
                {
                    incomplete = true;
                    break;
                }
            }

            AddFileScanNotice(result, request.TargetPath, incomplete, skippedReparsePoint, unreadable);
            return files;
        }

        static void AddFileScanNotice(ForgeAnalysisResult result, string targetPath, bool incomplete, bool skippedReparsePoint, bool unreadable)
        {
            if (!incomplete && !skippedReparsePoint && !unreadable) return;
            result.Truncated = true;
            if (result.Findings.Any(finding => finding.RuleId == "analysis_scan_incomplete" && string.Equals(finding.SourcePath, targetPath, StringComparison.OrdinalIgnoreCase))) return;

            var reasons = new List<string>();
            if (incomplete) reasons.Add("a directory, depth, entry, or file bound was reached");
            if (skippedReparsePoint) reasons.Add("reparse points were skipped");
            if (unreadable) reasons.Add("some paths could not be read");
            Add(result, "analysis_scan_incomplete", "Warning", targetPath, null, null,
                "The selected tree was only partially inspected because " + string.Join(", ", reasons) + ".",
                "Narrow the selected folder and ensure the desired source files are directly accessible.");
        }

        sealed class CancellationCheckingTextReader : TextReader
        {
            readonly TextReader inner;
            readonly CancellationToken cancellationToken;

            internal CancellationCheckingTextReader(TextReader inner, CancellationToken cancellationToken)
            {
                this.inner = inner;
                this.cancellationToken = cancellationToken;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return inner.Read(buffer, index, count);
            }
            public override int Read()
            {
                cancellationToken.ThrowIfCancellationRequested();
                return inner.Read();
            }
            public override int Peek()
            {
                cancellationToken.ThrowIfCancellationRequested();
                return inner.Peek();
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing) inner.Dispose();
                base.Dispose(disposing);
            }
        }

        static XDocument LoadXml(string file, int maximumBytes, CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();
            using (var stringReader = new StringReader(ReadBounded(file, maximumBytes, cancellation)))
            using (var textReader = new CancellationCheckingTextReader(stringReader, cancellation))
            using (var reader = XmlReader.Create(textReader, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = maximumBytes }))
            {
                var document = XDocument.Load(reader, LoadOptions.None);
                cancellation.ThrowIfCancellationRequested();
                return document;
            }
        }

        internal static string ReadBounded(string file, int maximumBytes, CancellationToken cancellation = default)
        {
            if (maximumBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
            cancellation.ThrowIfCancellationRequested();
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 81920, FileOptions.SequentialScan))
            {
                if (stream.Length > maximumBytes) throw new InvalidDataException("File exceeds the configured analysis limit.");
                using (var bytes = new MemoryStream((int)Math.Min(stream.Length, (long)maximumBytes)))
                {
                    var buffer = new byte[81920];
                    var totalBytes = 0L;
                    while (true)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        var allowed = maximumBytes + 1L - totalBytes;
                        if (allowed <= 0) throw new InvalidDataException("File grew beyond the configured analysis limit while being read.");
                        var read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, allowed));
                        if (read == 0) break;
                        totalBytes += read;
                        if (totalBytes > maximumBytes) throw new InvalidDataException("File grew beyond the configured analysis limit while being read.");
                        bytes.Write(buffer, 0, read);
                    }
                    bytes.Position = 0;
                    using (var reader = new StreamReader(bytes, Encoding.UTF8, true))
                    {
                        var text = reader.ReadToEnd();
                        cancellation.ThrowIfCancellationRequested();
                        return text;
                    }
                }
            }
        }

        static int? LineOf(string text, string token, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var index = text.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;
            var line = 1;
            for (var cursor = 0; cursor < index; cursor++)
            {
                if ((cursor & 4095) == 0) cancellationToken.ThrowIfCancellationRequested();
                if (text[cursor] == '\n') line++;
            }
            return line;
        }

        static bool HasUtf8Bom(string file)
        {
            var prefix = new byte[3];
            var count = 0;
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 3, FileOptions.SequentialScan))
            {
                while (count < prefix.Length)
                {
                    var read = stream.Read(prefix, count, prefix.Length - count);
                    if (read == 0) break;
                    count += read;
                }
            }
            return count == 3 && prefix[0] == 0xEF && prefix[1] == 0xBB && prefix[2] == 0xBF;
        }

        internal static string MaskCSharpCommentsAndLiterals(string source)
        {
            if (!TryMaskCSharpCommentsAndLiterals(source, out var masked))
                throw new InvalidDataException("C# comments and literals could not be masked completely within the bounded nesting depth.");
            return masked;
        }

        internal static bool TryMaskCSharpCommentsAndLiterals(string source, out string masked)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var characters = source.ToCharArray();
            var index = 0;
            while (index < source.Length)
            {
                if (source[index] == '/' && index + 1 < source.Length && source[index + 1] == '/')
                {
                    var start = index;
                    index += 2;
                    while (index < source.Length && source[index] != '\r' && source[index] != '\n') index++;
                    MaskCSharpRange(source, characters, start, index);
                    continue;
                }
                if (source[index] == '/' && index + 1 < source.Length && source[index + 1] == '*')
                {
                    var start = index;
                    index += 2;
                    var closed = false;
                    while (index < source.Length)
                    {
                        if (source[index] == '*' && index + 1 < source.Length && source[index + 1] == '/')
                        {
                            index += 2;
                            closed = true;
                            break;
                        }
                        index++;
                    }
                    if (!closed)
                    {
                        masked = null;
                        return false;
                    }
                    MaskCSharpRange(source, characters, start, index);
                    continue;
                }
                if (source[index] == '"' || source[index] == '\'')
                {
                    var complete = true;
                    var end = FindCSharpLiteralEnd(source, index, 0, ref complete);
                    if (!complete)
                    {
                        masked = null;
                        return false;
                    }
                    MaskCSharpRange(source, characters, index, end);
                    index = end;
                    continue;
                }
                index++;
            }
            masked = new string(characters);
            return true;
        }

        static int FindCSharpLiteralEnd(string source, int quoteIndex, int nestingDepth, ref bool complete)
        {
            if (nestingDepth >= 32)
            {
                complete = false;
                return source.Length;
            }
            if (source[quoteIndex] == '\'') return FindQuotedCharacterEnd(source, quoteIndex, ref complete);

            var quoteCount = 0;
            while (quoteIndex + quoteCount < source.Length && source[quoteIndex + quoteCount] == '"') quoteCount++;
            if (quoteCount >= 3)
            {
                var dollarCount = CountPrecedingCharacters(source, quoteIndex, '$');
                return dollarCount == 0
                    ? FindRawStringEnd(source, quoteIndex, quoteCount, ref complete)
                    : FindRawInterpolatedStringEnd(source, quoteIndex, quoteCount, dollarCount, nestingDepth, ref complete);
            }

            var verbatim = IsVerbatimStringPrefix(source, quoteIndex);
            var interpolated = IsInterpolatedStringPrefix(source, quoteIndex);
            return interpolated
                ? FindInterpolatedStringEnd(source, quoteIndex, verbatim, nestingDepth, ref complete)
                : FindQuotedStringEnd(source, quoteIndex, verbatim, ref complete);
        }

        static int FindQuotedCharacterEnd(string source, int quoteIndex, ref bool complete)
        {
            var index = quoteIndex + 1;
            while (index < source.Length)
            {
                if (source[index] == '\r' || source[index] == '\n')
                {
                    complete = false;
                    return index;
                }
                if (source[index] == '\\')
                {
                    if (index + 1 >= source.Length)
                    {
                        complete = false;
                        return source.Length;
                    }
                    index += 2;
                    continue;
                }
                if (source[index++] == '\'') return index;
            }
            complete = false;
            return source.Length;
        }

        static int FindQuotedStringEnd(string source, int quoteIndex, bool verbatim, ref bool complete)
        {
            var index = quoteIndex + 1;
            while (index < source.Length)
            {
                if (source[index] == '"')
                {
                    if (verbatim && index + 1 < source.Length && source[index + 1] == '"') { index += 2; continue; }
                    return index + 1;
                }
                if (!verbatim && source[index] == '\\' && index + 1 < source.Length) { index += 2; continue; }
                if (!verbatim && (source[index] == '\r' || source[index] == '\n'))
                {
                    complete = false;
                    return index;
                }
                index++;
            }
            complete = false;
            return source.Length;
        }

        static int FindInterpolatedStringEnd(string source, int quoteIndex, bool verbatim, int nestingDepth, ref bool complete)
        {
            var index = quoteIndex + 1;
            while (index < source.Length)
            {
                if (source[index] == '"')
                {
                    if (verbatim && index + 1 < source.Length && source[index + 1] == '"') { index += 2; continue; }
                    return index + 1;
                }
                if (!verbatim && source[index] == '\\' && index + 1 < source.Length) { index += 2; continue; }
                if (source[index] == '{')
                {
                    if (index + 1 < source.Length && source[index + 1] == '{') { index += 2; continue; }
                    index = FindInterpolationExpressionEnd(source, index + 1, 1, nestingDepth + 1, ref complete);
                    if (!complete) return source.Length;
                    continue;
                }
                if (source[index] == '}' && index + 1 < source.Length && source[index + 1] == '}') { index += 2; continue; }
                index++;
            }
            complete = false;
            return source.Length;
        }

        static int FindRawStringEnd(string source, int quoteIndex, int delimiterLength, ref bool complete)
        {
            var index = quoteIndex + delimiterLength;
            while (index < source.Length)
            {
                if (source[index] != '"') { index++; continue; }
                var runLength = CountForwardCharacters(source, index, '"');
                if (runLength >= delimiterLength) return index + runLength;
                index += runLength;
            }
            complete = false;
            return source.Length;
        }

        static int FindRawInterpolatedStringEnd(string source, int quoteIndex, int quoteDelimiterLength, int braceDelimiterLength, int nestingDepth, ref bool complete)
        {
            var index = quoteIndex + quoteDelimiterLength;
            while (index < source.Length)
            {
                if (source[index] == '"')
                {
                    var quoteRun = CountForwardCharacters(source, index, '"');
                    if (quoteRun >= quoteDelimiterLength) return index + quoteRun;
                    index += quoteRun;
                    continue;
                }
                if (source[index] == '{')
                {
                    var braceRun = CountForwardCharacters(source, index, '{');
                    if (braceRun >= braceDelimiterLength)
                    {
                        index = FindInterpolationExpressionEnd(source, index + braceDelimiterLength, braceDelimiterLength, nestingDepth + 1, ref complete);
                        if (!complete) return source.Length;
                        continue;
                    }
                    index += braceRun;
                    continue;
                }
                index++;
            }
            complete = false;
            return source.Length;
        }

        static int FindInterpolationExpressionEnd(string source, int expressionStart, int closeBraceCount, int nestingDepth, ref bool complete)
        {
            if (nestingDepth >= 32)
            {
                complete = false;
                return source.Length;
            }
            var braceDepth = 1;
            var index = expressionStart;
            while (index < source.Length)
            {
                if (source[index] == '/' && index + 1 < source.Length && source[index + 1] == '/')
                {
                    index += 2;
                    while (index < source.Length && source[index] != '\r' && source[index] != '\n') index++;
                    continue;
                }
                if (source[index] == '/' && index + 1 < source.Length && source[index + 1] == '*')
                {
                    index += 2;
                    var closed = false;
                    while (index < source.Length)
                    {
                        if (source[index] == '*' && index + 1 < source.Length && source[index + 1] == '/') { index += 2; closed = true; break; }
                        index++;
                    }
                    if (!closed)
                    {
                        complete = false;
                        return source.Length;
                    }
                    continue;
                }
                if (source[index] == '"' || source[index] == '\'')
                {
                    index = FindCSharpLiteralEnd(source, index, nestingDepth + 1, ref complete);
                    if (!complete) return source.Length;
                    continue;
                }
                if (source[index] == '{') { braceDepth++; index++; continue; }
                if (source[index] == '}')
                {
                    if (--braceDepth == 0)
                    {
                        var closingRun = CountForwardCharacters(source, index, '}');
                        return index + Math.Min(closeBraceCount, closingRun);
                    }
                }
                index++;
            }
            complete = false;
            return source.Length;
        }

        static bool IsVerbatimStringPrefix(string source, int quoteIndex)
        {
            return (quoteIndex > 0 && source[quoteIndex - 1] == '@') ||
                   (quoteIndex > 1 && source[quoteIndex - 1] == '$' && source[quoteIndex - 2] == '@');
        }

        static bool IsInterpolatedStringPrefix(string source, int quoteIndex)
        {
            return CountPrecedingCharacters(source, quoteIndex, '$') > 0 ||
                   (quoteIndex > 1 && source[quoteIndex - 1] == '@' && source[quoteIndex - 2] == '$') ||
                   (quoteIndex > 1 && source[quoteIndex - 1] == '$' && source[quoteIndex - 2] == '@');
        }

        static int CountPrecedingCharacters(string source, int endExclusive, char value)
        {
            var count = 0;
            for (var index = endExclusive - 1; index >= 0 && source[index] == value; index--) count++;
            return count;
        }

        static int CountForwardCharacters(string source, int start, char value)
        {
            var count = 0;
            for (var index = start; index < source.Length && source[index] == value; index++) count++;
            return count;
        }

        static void MaskCSharpRange(string source, char[] characters, int start, int end)
        {
            for (var index = start; index < end; index++)
                if (source[index] != '\r' && source[index] != '\n') characters[index] = ' ';
        }

        static bool IsAnalysisInputError(Exception error)
        {
            return error is IOException || error is UnauthorizedAccessException || error is System.Security.SecurityException ||
                   error is ArgumentException || error is NotSupportedException;
        }

        static string Bound(string text, int maximum) => string.IsNullOrEmpty(text) || text.Length <= maximum ? text : text.Substring(0, maximum) + "…";

        static void Add(ForgeAnalysisResult result, string rule, string severity, string path, int? line, int? column, string evidence, string recommendation)
        {
            if (result.Findings.Count >= 1000) { result.Truncated = true; return; }
            result.Findings.Add(new ForgeDiagnosticFinding { RuleId = rule, Severity = severity, SourcePath = path, Line = line, Column = column, Evidence = evidence, Recommendation = recommendation });
        }

        static void AddIncompleteSourceFinding(ForgeAnalysisResult result, string path, string evidence, string recommendation)
        {
            const string rule = "source_lex_incomplete";
            result.Truncated = true;
            if (result.Findings.Count >= 1000)
            {
                // Keep the diagnostic cap while ensuring a bounded/incomplete parse cannot look clean.
                if (result.Findings.Any(finding => finding != null && string.Equals(finding.RuleId, rule, StringComparison.Ordinal))) return;
                result.Findings[result.Findings.Count - 1] = new ForgeDiagnosticFinding
                {
                    RuleId = rule,
                    Severity = "Error",
                    SourcePath = path,
                    Evidence = evidence,
                    Recommendation = recommendation
                };
                return;
            }
            Add(result, rule, "Error", path, null, null, evidence, recommendation);
        }
    }

    internal static class ForgeAnalysisPathExtensions
    {
        internal static bool Matches(this string name, string pattern)
        {
            if (string.Equals(pattern, "*.xml", StringComparison.OrdinalIgnoreCase)) return name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(pattern, "*.cs", StringComparison.OrdinalIgnoreCase)) return name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(pattern, "*.log", StringComparison.OrdinalIgnoreCase)) return name.EndsWith(".log", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(pattern, "*.txt", StringComparison.OrdinalIgnoreCase)) return name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(pattern, "*.cfcrash", StringComparison.OrdinalIgnoreCase)) return name.EndsWith(".cfcrash", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(pattern, "*.dmp", StringComparison.OrdinalIgnoreCase)) return name.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(pattern, "*.sav", StringComparison.OrdinalIgnoreCase)) return name.EndsWith(".sav", StringComparison.OrdinalIgnoreCase);
            return false;
        }
    }
}
