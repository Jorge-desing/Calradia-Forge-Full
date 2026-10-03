using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace CalradiaForge.Core
{
    public class RuleFinding
    {
        public string RuleId { get; set; }
        public string Severity { get; set; } // "Error", "Warning"
        public string FilePath { get; set; }
        public string Description { get; set; }
        public string Recommendation { get; set; }
    }

    public class RuleAuditResult
    {
        public string TargetDirectory { get; set; }
        public bool Passed => Findings.All(f => f.Severity != "Error");
        public List<RuleFinding> Findings { get; set; } = new List<RuleFinding>();
    }

    public static class ModRuleAuditor
    {
        const string DistributionSafetyRule = "DISTRIBUTION_SAFETY";
        const string ScanIncompleteRule = "AUDIT_SCAN_INCOMPLETE";
        const string InputUnreadableRule = "AUDIT_INPUT_UNREADABLE";
        const string InputLimitRule = "AUDIT_INPUT_LIMIT";
        const string InvalidXmlRule = "AUDIT_XML_INVALID";
        const string CSharpLexIncompleteRule = "AUDIT_CSHARP_LEX_INCOMPLETE";

        sealed class AuditFile
        {
            internal string Path;
            internal string Extension;
        }

        sealed class AuditScan
        {
            internal readonly List<AuditFile> Files = new List<AuditFile>();
            internal readonly List<string> Details = new List<string>();
            internal bool Incomplete;
            internal bool Unreadable;
            internal bool SkippedReparsePoint;
            internal bool FileLimitReached;

            internal void AddDetail(string detail)
            {
                if (Details.Count < 8) Details.Add(detail);
            }
        }

        private static readonly HashSet<string> ValidSoundCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ui", "mission_combat", "ambient", "voice"
        };

        private static readonly HashSet<string> ValidTroopGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Infantry", "Ranged", "Cavalry", "HorseArcher"
        };

        private static readonly HashSet<string> ValidDamageTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Cut", "Pierce", "Blunt"
        };

        public static RuleAuditResult Audit(string modDirectory)
        {
            var result = new RuleAuditResult { TargetDirectory = modDirectory };
            if (string.IsNullOrWhiteSpace(modDirectory) || !Directory.Exists(modDirectory))
            {
                result.Findings.Add(new RuleFinding
                {
                    RuleId = "DIRECTORY_EXISTS",
                    Severity = "Error",
                    FilePath = modDirectory,
                    Description = "Target mod directory does not exist.",
                    Recommendation = "Provide a valid module root directory."
                });
                return result;
            }

            string root;
            try { root = Path.GetFullPath(modDirectory); }
            catch (Exception error) when (IsAuditInputError(error))
            {
                AddFinding(result, "AUDIT_ROOT_UNREADABLE", "Error", modDirectory,
                    "Target mod directory could not be resolved: " + error.Message,
                    "Provide a directly accessible module or development workspace directory.");
                return result;
            }

            var scan = ScanFiles(root);
            var trustedToolsRoot = GetTrustedToolsRoot(root);
            foreach (var file in scan.Files)
            {
                if (IsScriptExtension(file.Extension))
                {
                    if (!IsWithinTrustedToolsRoot(file.Path, trustedToolsRoot))
                    {
                        AddFinding(result, DistributionSafetyRule, "Warning", file.Path,
                            "Found executable script '" + Path.GetFileName(file.Path) + "' in module hierarchy.",
                            "Exclude script files (.ps1, .bat) from user-facing module distribution archives.");
                    }
                }
                else if (string.Equals(file.Extension, ".cs", StringComparison.OrdinalIgnoreCase))
                {
                    AuditCSharpFile(file.Path, result);
                }
                else if (string.Equals(file.Extension, ".xml", StringComparison.OrdinalIgnoreCase))
                {
                    AuditXmlFile(file.Path, result);
                }
            }

            AddScanFinding(result, root, scan);

            return result;
        }

        static AuditScan ScanFiles(string root)
        {
            var result = new AuditScan();
            var pending = new Stack<Tuple<string, int>>();
            var discoveredDirectories = 1;
            var inspectedEntries = 0;
            var inspectedFiles = 0;

            try
            {
                if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
                {
                    result.SkippedReparsePoint = true;
                    result.Incomplete = true;
                    result.AddDetail("The selected root is a reparse point and was not followed.");
                    return result;
                }
                pending.Push(Tuple.Create(root, 0));
            }
            catch (Exception error) when (IsAuditInputError(error))
            {
                result.Unreadable = true;
                result.Incomplete = true;
                result.AddDetail("The selected root could not be inspected: " + error.Message);
                return result;
            }

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                try
                {
                    foreach (var entry in Directory.EnumerateFileSystemEntries(current.Item1, "*", SearchOption.TopDirectoryOnly))
                    {
                        if (inspectedEntries >= ForgeAnalysisCatalog.MaximumAnalysisDirectoryEntries)
                        {
                            result.Incomplete = true;
                            result.AddDetail("The directory-entry limit of " + ForgeAnalysisCatalog.MaximumAnalysisDirectoryEntries + " was reached.");
                            pending.Clear();
                            break;
                        }
                        inspectedEntries++;

                        FileAttributes attributes;
                        try { attributes = File.GetAttributes(entry); }
                        catch (Exception error) when (IsAuditInputError(error))
                        {
                            result.Unreadable = true;
                            result.Incomplete = true;
                            result.AddDetail("An entry could not be inspected: " + entry);
                            continue;
                        }

                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            result.SkippedReparsePoint = true;
                            result.Incomplete = true;
                            result.AddDetail("A reparse point was skipped: " + entry);
                            continue;
                        }

                        if ((attributes & FileAttributes.Directory) != 0)
                        {
                            var depth = current.Item2 + 1;
                            if (depth > ForgeAnalysisCatalog.MaximumAnalysisDirectoryDepth ||
                                discoveredDirectories >= ForgeAnalysisCatalog.MaximumAnalysisDirectories)
                            {
                                result.Incomplete = true;
                                result.AddDetail("The directory depth or directory-count limit was reached at: " + entry);
                                continue;
                            }
                            discoveredDirectories++;
                            pending.Push(Tuple.Create(entry, depth));
                            continue;
                        }

                        var extension = Path.GetExtension(entry);
                        if (!IsAuditFileExtension(extension)) continue;
                        if (inspectedFiles >= ForgeAnalysisCatalog.MaximumAnalysisFiles)
                        {
                            result.Incomplete = true;
                            result.FileLimitReached = true;
                            result.AddDetail("The audit-file limit of " + ForgeAnalysisCatalog.MaximumAnalysisFiles + " was reached.");
                            pending.Clear();
                            break;
                        }

                        inspectedFiles++;
                        result.Files.Add(new AuditFile { Path = entry, Extension = extension });
                    }
                }
                catch (Exception error) when (IsAuditInputError(error))
                {
                    result.Unreadable = true;
                    result.Incomplete = true;
                    result.AddDetail("A directory could not be enumerated: " + current.Item1);
                }
            }

            return result;
        }

        static bool IsAuditFileExtension(string extension)
        {
            return string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase) ||
                   IsScriptExtension(extension);
        }

        static bool IsScriptExtension(string extension)
        {
            return string.Equals(extension, ".ps1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".bat", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".cmd", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".sh", StringComparison.OrdinalIgnoreCase);
        }

        static string GetTrustedToolsRoot(string root)
        {
            // Only the repository's top-level tools folder is trusted. A nested module/tools
            // folder must still be reported as a distribution script location.
            if (!File.Exists(Path.Combine(root, "CalradiaForge.sln")) ||
                !Directory.Exists(Path.Combine(root, "src", "CalradiaForge.Core"))) return null;

            return Path.GetFullPath(Path.Combine(root, "tools")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }

        static bool IsWithinTrustedToolsRoot(string file, string trustedToolsRoot)
        {
            if (string.IsNullOrEmpty(trustedToolsRoot)) return false;
            try
            {
                var fullPath = Path.GetFullPath(file);
                return fullPath.StartsWith(trustedToolsRoot, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception error) when (IsAuditInputError(error)) { return false; }
        }

        static void AddFinding(RuleAuditResult result, string ruleId, string severity, string filePath, string description, string recommendation)
        {
            result.Findings.Add(new RuleFinding
            {
                RuleId = ruleId,
                Severity = severity,
                FilePath = filePath,
                Description = description,
                Recommendation = recommendation
            });
        }

        static void AddScanFinding(RuleAuditResult result, string root, AuditScan scan)
        {
            if (!scan.Incomplete && !scan.Unreadable && !scan.SkippedReparsePoint && !scan.FileLimitReached) return;

            var reasons = new List<string>();
            if (scan.Incomplete) reasons.Add("the bounded scan did not inspect the entire target");
            if (scan.Unreadable) reasons.Add("one or more paths were unreadable");
            if (scan.SkippedReparsePoint) reasons.Add("reparse points were skipped without following them");
            if (scan.FileLimitReached) reasons.Add("the audit-file limit was reached");
            var details = scan.Details.Count == 0 ? string.Empty : " Details: " + string.Join("; ", scan.Details);
            AddFinding(result, ScanIncompleteRule, "Error", root,
                "The audit cannot report Passed because " + string.Join(", ", reasons) + "." + details,
                "Remove inaccessible or reparse-point entries, or select a smaller tree within the Core analysis bounds, then run the audit again.");
        }

        private static void AuditCSharpFile(string csFile, RuleAuditResult result)
        {
            string content;
            try { content = ForgeAnalysisCatalog.ReadBounded(csFile, ForgeAnalysisCatalog.MaximumAnalysisBytesPerFile); }
            catch (InvalidDataException error)
            {
                AddFinding(result, InputLimitRule, "Error", csFile,
                    "C# source could not be audited within the Core per-file limit: " + error.Message,
                    "Reduce the file below " + ForgeAnalysisCatalog.MaximumAnalysisBytesPerFile + " bytes and rerun the audit.");
                return;
            }
            catch (Exception error) when (IsAuditInputError(error))
            {
                AddFinding(result, InputUnreadableRule, "Error", csFile,
                    "C# source could not be read: " + error.Message,
                    "Check file access and rerun the audit; the source was not considered valid.");
                return;
            }

            if (!ForgeAnalysisCatalog.TryMaskCSharpCommentsAndLiterals(content, out var code))
            {
                AddFinding(result, CSharpLexIncompleteRule, "Error", csFile,
                    "C# source could not be masked completely because a comment or literal was unterminated or exceeded the bounded nesting depth.",
                    "Correct the C# source or simplify nested interpolations, then rerun the audit.");
                return;
            }

            // GEMINI.md Campaign namespace and class check
            if (Regex.IsMatch(code, @"\bnamespace\s+[A-Za-z0-9_\.]*\.Campaign[\s;\{]") ||
                Regex.IsMatch(code, @"\bclass\s+Campaign\b"))
            {
                result.Findings.Add(new RuleFinding
                {
                    RuleId = "GEMINI_CAMPAIGN_SHADOWING",
                    Severity = "Error",
                    FilePath = csFile,
                    Description = "Namespace or class shadows TaleWorlds.CampaignSystem.Campaign.",
                    Recommendation = "Rename class or namespace to use CampaignBehaviors, CampaignMechanics, or CampaignExtensions."
                });
            }

            // SaveableTypeDefiner base ID check
            var matchTypeDefiner = Regex.Match(code, @":\s*SaveableTypeDefiner\s*\((\d[\d_]*)\)");
            if (matchTypeDefiner.Success)
            {
                string idStr = matchTypeDefiner.Groups[1].Value.Replace("_", "");
                if (int.TryParse(idStr, out int baseId))
                {
                    if (baseId < 2500000)
                    {
                        result.Findings.Add(new RuleFinding
                        {
                            RuleId = "SAVEABLE_BASE_ID_COLLISION",
                            Severity = "Error",
                            FilePath = csFile,
                            Description = $"SaveableTypeDefiner base ID {baseId} is below 2,500,000, risking native collision.",
                            Recommendation = "Allocate a base ID >= 2,500,000 for custom SaveableTypeDefiner instances."
                        });
                    }
                }
            }

            // QuestBase double SetDialogs() check
            if (code.Contains(": QuestBase") || code.Contains(":QuestBase"))
            {
                bool hasInitOnLoad = code.Contains("InitializeQuestOnGameLoad");
                int setDialogsCount = Regex.Matches(code, @"\bSetDialogs\s*\(\s*\)").Count;
                if (!hasInitOnLoad || setDialogsCount < 2)
                {
                    result.Findings.Add(new RuleFinding
                    {
                        RuleId = "QUEST_DOUBLE_SET_DIALOGS",
                        Severity = "Error",
                        FilePath = csFile,
                        Description = "QuestBase implementation missing double SetDialogs() invocation (required in constructor and InitializeQuestOnGameLoad).",
                        Recommendation = "Call SetDialogs() in both the quest constructor and inside InitializeQuestOnGameLoad()."
                    });
                }
            }

            // MissionLogic Mesh OnInit guard check
            if (code.Contains(": MissionLogic") || code.Contains(":MissionLogic") || code.Contains(": MissionBehavior"))
            {
                if (code.Contains("override void OnInit()") && (code.Contains("Mesh") || code.Contains("Skeleton")))
                {
                    if (!code.Contains("_initialized"))
                    {
                        result.Findings.Add(new RuleFinding
                        {
                            RuleId = "MISSION_MESH_ONINIT_DEFERRED",
                            Severity = "Warning",
                            FilePath = csFile,
                            Description = "Possible mesh/skeleton manipulation in OnInit() without an _initialized deferred OnTick guard.",
                            Recommendation = "Defer all mesh/skeleton modifications to the first OnTick(float dt) frame."
                        });
                    }
                }
            }

            // 5. ForgeWeave Pulse throttle check. Comments and string literals are masked
            // before parsing so documentation cannot make an unthrottled subscription pass.
            AuditPulseSubscriptions(code, csFile, result);

            // 6. ForgeWeave unsafe writer check
            if (code.Contains("ForgeEventAccess.CampaignWrite") || code.Contains("ForgeEventAccess.MissionWrite"))
            {
                bool hasSafetyGate = code.Contains("TestEngine") || code.Contains("IsInTestMode") ||
                                     code.Contains("TestingEnabled") || code.Contains("CampaignCopyConfirmed");
                if (!hasSafetyGate)
                {
                    result.Findings.Add(new RuleFinding
                    {
                        RuleId = "FORGEWEAVE_UNSAFE_WRITER",
                        Severity = "Error",
                        FilePath = csFile,
                        Description = "ForgeWeave write access subscription (CampaignWrite/MissionWrite) declared without test mode gate or simulation safety check.",
                        Recommendation = "Verify IsInTestMode or TestEngine.TestingEnabled before executing state-changing write operations."
                    });
                }
            }

            // 7. Desktop Path member shadowing check
            if (Regex.IsMatch(code, @"\b(public|protected|private|internal)\s+[A-Za-z0-9_<>\[\]]+\s+Path\s*[\{;]") &&
                Regex.IsMatch(code, @"(?<!System\.IO\.)\bPath\.(Combine|GetDirectoryName|GetFileName|GetFullPath|GetExtension|Exists)\b"))
            {
                result.Findings.Add(new RuleFinding
                {
                    RuleId = "DESKTOP_PATH_SHADOWING",
                    Severity = "Error",
                    FilePath = csFile,
                    Description = "Class declares a member named 'Path' while making unqualified calls to Path IO methods, causing CS0236/CS0118 shadowing errors.",
                    Recommendation = "Explicitly qualify path utilities as System.IO.Path.Combine(...) when the enclosing class defines a 'Path' member."
                });
            }

            // 8. WPF IMultiValueConverter ConvertBack signature check
            if (code.Contains("IMultiValueConverter") &&
                Regex.IsMatch(code, @"\bobject\s+ConvertBack\s*\(") &&
                !Regex.IsMatch(code, @"\bobject\[\]\s+ConvertBack\s*\("))
            {
                result.Findings.Add(new RuleFinding
                {
                    RuleId = "WPF_CONVERTER_SIGNATURE",
                    Severity = "Error",
                    FilePath = csFile,
                    Description = "IMultiValueConverter.ConvertBack must return object[], not object, to satisfy the WPF interface contract and avoid CS0738.",
                    Recommendation = "Change the return type of ConvertBack to object[]."
                });
            }
        }

        private static void AuditXmlFile(string xmlFile, RuleAuditResult result)
        {
            XDocument doc;
            try
            {
                var content = ForgeAnalysisCatalog.ReadBounded(xmlFile, ForgeAnalysisCatalog.MaximumAnalysisBytesPerFile);
                using (var textReader = new StringReader(content))
                using (var xmlReader = XmlReader.Create(textReader, new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersInDocument = ForgeAnalysisCatalog.MaximumAnalysisBytesPerFile
                }))
                {
                    doc = XDocument.Load(xmlReader, LoadOptions.None);
                }
            }
            catch (XmlException error)
            {
                AddFinding(result, InvalidXmlRule, "Error", xmlFile,
                    "XML could not be parsed safely: " + error.Message,
                    "Correct the XML and rerun the audit; malformed or DTD-based XML cannot pass validation.");
                return;
            }
            catch (InvalidDataException error)
            {
                AddFinding(result, InputLimitRule, "Error", xmlFile,
                    "XML could not be audited within the Core per-file limit: " + error.Message,
                    "Reduce the file below " + ForgeAnalysisCatalog.MaximumAnalysisBytesPerFile + " bytes and rerun the audit.");
                return;
            }
            catch (Exception error) when (IsAuditInputError(error))
            {
                AddFinding(result, InputUnreadableRule, "Error", xmlFile,
                    "XML could not be read: " + error.Message,
                    "Check file access and rerun the audit; the file was not considered valid.");
                return;
            }

            // Audio XML Check
            if (doc.Root?.Name.LocalName == "module_sounds")
            {
                var seenSoundNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var soundElem in doc.Descendants("module_sound"))
                {
                    string name = (string)soundElem.Attribute("name");
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (!seenSoundNames.Add(name))
                        {
                            result.Findings.Add(new RuleFinding
                            {
                                RuleId = "AUDIO_DUPLICATE_NAME",
                                Severity = "Error",
                                FilePath = xmlFile,
                                Description = $"Duplicate sound name '{name}' detected in module_sounds.xml.",
                                Recommendation = "Ensure each sound entry has a unique name attribute to prevent engine mixer silent overwrites."
                            });
                        }
                    }
                    string cat = (string)soundElem.Attribute("sound_category");
                    if (!string.IsNullOrEmpty(cat) && !ValidSoundCategories.Contains(cat))
                    {
                        result.Findings.Add(new RuleFinding
                        {
                            RuleId = "AUDIO_INVALID_CATEGORY",
                            Severity = "Error",
                            FilePath = xmlFile,
                            Description = $"Sound category '{cat}' is invalid. Valid categories: ui, mission_combat, ambient, voice.",
                            Recommendation = "Update sound_category to match a valid engine mixer category."
                        });
                    }

                    string path = (string)soundElem.Attribute("path");
                    if (!string.IsNullOrEmpty(path))
                    {
                        string ext = Path.GetExtension(path).ToLowerInvariant();
                        if (ext != ".ogg" && ext != ".wav")
                        {
                            result.Findings.Add(new RuleFinding
                            {
                                RuleId = "AUDIO_INVALID_FORMAT",
                                Severity = "Error",
                                FilePath = xmlFile,
                                Description = $"Sound file '{path}' does not use .ogg or .wav extension.",
                                Recommendation = "Convert sound asset to Vorbis .ogg or uncompressed .wav."
                            });
                        }
                    }
                }
            }

            // Troop / NPCCharacter Check
            if (doc.Root?.Name.LocalName == "NPCCharacters")
            {
                foreach (var character in doc.Descendants("NPCCharacter"))
                {
                    string ageStr = (string)character.Attribute("age");
                    if (!string.IsNullOrEmpty(ageStr) && !int.TryParse(ageStr, out _))
                    {
                        result.Findings.Add(new RuleFinding
                        {
                            RuleId = "TROOP_NON_INTEGER_AGE",
                            Severity = "Error",
                            FilePath = xmlFile,
                            Description = $"NPCCharacter '{character.Attribute("id")?.Value}' has fractional age '{ageStr}'. Integer required.",
                            Recommendation = "Ensure NPCCharacter age attribute is an integer to prevent CTD."
                        });
                    }

                    string group = (string)character.Attribute("default_group");
                    if (!string.IsNullOrEmpty(group) && !ValidTroopGroups.Contains(group))
                    {
                        result.Findings.Add(new RuleFinding
                        {
                            RuleId = "TROOP_INVALID_GROUP",
                            Severity = "Warning",
                            FilePath = xmlFile,
                            Description = $"NPCCharacter '{character.Attribute("id")?.Value}' has unrecognized default_group '{group}'.",
                            Recommendation = "Use standard groups: Infantry, Ranged, Cavalry, HorseArcher."
                        });
                    }
                }
            }

            // Gauntlet Prefab Watermark / Decoration Event Pass Check
            if (xmlFile.Replace('\\', '/').Contains("/GUI/Prefabs/"))
            {
                foreach (var textWidget in doc.Descendants("TextWidget"))
                {
                    string textVal = (string)textWidget.Attribute("Text");
                    string brush = (string)textWidget.Attribute("Brush");
                    if (brush != null && brush.Contains("Watermark"))
                    {
                        string accept = (string)textWidget.Attribute("DoNotAcceptEvents");
                        string pass = (string)textWidget.Attribute("DoNotPassEventsToChildren");
                        if (accept != "true" || pass != "true")
                        {
                            result.Findings.Add(new RuleFinding
                            {
                                RuleId = "GAUNTLET_WATERMARK_EVENT_BLOCK",
                                Severity = "Error",
                                FilePath = xmlFile,
                                Description = "Watermark TextWidget missing DoNotAcceptEvents='true' or DoNotPassEventsToChildren='true'.",
                                Recommendation = "Add DoNotAcceptEvents='true' and DoNotPassEventsToChildren='true' so clicks pass through to controls."
                            });
                        }
                    }
                }
            }
        }

        static void AuditPulseSubscriptions(string code, string filePath, RuleAuditResult result)
        {
            if (!code.Contains("ForgeEventKind.Pulse")) return;

            var hasPulseSubscription = false;
            var hasUnresolvedSubscription = false;
            var initializerMatches = Regex.Matches(code, @"\bnew\s+ForgeEventSubscription\s*\{");
            foreach (Match match in initializerMatches)
            {
                var openBrace = code.IndexOf('{', match.Index + match.Length - 1);
                var closeBrace = FindMatchingDelimiter(code, openBrace, '{', '}');
                if (closeBrace < 0) continue;
                var initializer = code.Substring(openBrace + 1, closeBrace - openBrace - 1);
                var eventValue = Regex.Match(initializer, @"\bEvent\s*=\s*(?<value>[^,}]+)");
                if (!eventValue.Success)
                {
                    hasUnresolvedSubscription = true;
                    continue;
                }
                if (!eventValue.Groups["value"].Value.Contains("ForgeEventKind.Pulse"))
                {
                    if (!Regex.IsMatch(eventValue.Groups["value"].Value.Trim(), @"^ForgeEventKind\.[A-Za-z_]\w*$"))
                        hasUnresolvedSubscription = true;
                    continue;
                }

                hasPulseSubscription = true;
                var interval = Regex.Match(initializer,
                    @"\bMinimumIntervalMilliseconds\s*=\s*(?<value>[^,}]+)");
                if (!interval.Success || !IsAtLeastMinimumPulseInterval(interval.Groups["value"].Value))
                    AddPulseThrottleFinding(filePath, result);
            }

            var callMatches = Regex.Matches(code,
                @"\b(?:ForgeCampaignEvents\s*\.\s*)?SubscribeWeave(?:WhenAvailable)?\s*\(");
            foreach (Match match in callMatches)
            {
                var openParenthesis = code.IndexOf('(', match.Index + match.Length - 1);
                var closeParenthesis = FindMatchingDelimiter(code, openParenthesis, '(', ')');
                if (closeParenthesis < 0) continue;

                var arguments = SplitTopLevelArguments(code.Substring(openParenthesis + 1, closeParenthesis - openParenthesis - 1));
                var eventArgument = arguments.FirstOrDefault(argument => Regex.IsMatch(argument, @"\bkind\s*:"));
                if (eventArgument == null && arguments.Count > 1) eventArgument = arguments[1];
                if (eventArgument == null || !eventArgument.Contains("ForgeEventKind.Pulse"))
                {
                    if (eventArgument == null || !Regex.IsMatch(eventArgument.Trim(), @"^ForgeEventKind\.[A-Za-z_]\w*$"))
                        hasUnresolvedSubscription = true;
                    continue;
                }

                hasPulseSubscription = true;
                string intervalExpression = null;
                var namedInterval = arguments.FirstOrDefault(argument => Regex.IsMatch(argument, @"\bminIntervalMs\s*:"));
                if (namedInterval != null)
                {
                    var intervalMatch = Regex.Match(namedInterval, @"\bminIntervalMs\s*:\s*(?<value>[\s\S]*)$");
                    if (intervalMatch.Success) intervalExpression = intervalMatch.Groups["value"].Value;
                }
                else if (arguments.Count > 5)
                {
                    intervalExpression = arguments[5];
                }

                if (!IsAtLeastMinimumPulseInterval(intervalExpression))
                    AddPulseThrottleFinding(filePath, result);
            }

            // A subscription that passes a computed event kind cannot be proven safe by this
            // source-only analyzer. Fail closed for that registration rather than passing it.
            if (hasUnresolvedSubscription && (hasPulseSubscription || initializerMatches.Count > 0 || callMatches.Count > 0))
                AddPulseThrottleFinding(filePath, result);
        }

        static bool IsAtLeastMinimumPulseInterval(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return false;
            var match = Regex.Match(expression.Trim(), @"^\(*\s*(?<value>\d[\d_]*)\s*[uUlL]*\s*\)*$");
            if (!match.Success) return false;
            var value = match.Groups["value"].Value.Replace("_", string.Empty);
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds) && milliseconds >= 50;
        }

        static List<string> SplitTopLevelArguments(string arguments)
        {
            var result = new List<string>();
            var start = 0;
            var parentheses = 0;
            var braces = 0;
            var brackets = 0;
            for (var index = 0; index < arguments.Length; index++)
            {
                switch (arguments[index])
                {
                    case '(': parentheses++; break;
                    case ')': parentheses--; break;
                    case '{': braces++; break;
                    case '}': braces--; break;
                    case '[': brackets++; break;
                    case ']': brackets--; break;
                    case ',':
                        if (parentheses == 0 && braces == 0 && brackets == 0)
                        {
                            result.Add(arguments.Substring(start, index - start).Trim());
                            start = index + 1;
                        }
                        break;
                }
            }
            if (start < arguments.Length) result.Add(arguments.Substring(start).Trim());
            return result;
        }

        static int FindMatchingDelimiter(string source, int start, char open, char close)
        {
            if (start < 0 || start >= source.Length || source[start] != open) return -1;
            var depth = 0;
            for (var index = start; index < source.Length; index++)
            {
                if (source[index] == open) depth++;
                else if (source[index] == close && --depth == 0) return index;
            }
            return -1;
        }

        static void AddPulseThrottleFinding(string filePath, RuleAuditResult result)
        {
            AddFinding(result, "FORGEWEAVE_UNTHROTTLED_PULSE", "Error", filePath,
                "ForgeWeave Pulse subscription has no verifiable numeric MinimumIntervalMilliseconds throttle of at least 50 ms.",
                "Set MinimumIntervalMilliseconds or minIntervalMs to a numeric value >= 50 on each Pulse subscription.");
        }

        static bool IsAuditInputError(Exception error)
        {
            return error is IOException || error is UnauthorizedAccessException || error is SecurityException ||
                   error is InvalidDataException || error is ArgumentException || error is NotSupportedException;
        }
    }
}
