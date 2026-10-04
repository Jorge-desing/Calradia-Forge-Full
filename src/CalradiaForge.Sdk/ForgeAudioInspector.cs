using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Deep validator and manifest generator for Bannerlord custom audio (module_sounds.xml).
    /// Enforces mixer category rules, 2D/3D emitter constraints, and audio encoding standards.
    /// Adheres strictly to bannerlord_audio_system.md.
    /// </summary>
    public static class ForgeAudioInspector
    {
        private static readonly HashSet<string> ValidSoundCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ui",
            "mission_combat",
            "ambient",
            "voice"
        };

        public struct AudioValidationFinding
        {
            public string SoundName;
            public string Severity; // Error, Warning, Pass
            public string Message;
        }

        public struct AudioAuditResult
        {
            public int TotalSounds;
            public int ValidSounds;
            public int ErrorCount;
            public int WarningCount;
            public List<AudioValidationFinding> Findings;
            public string Summary;
        }

        /// <summary>
        /// Audits a module_sounds.xml manifest string for engine mixer compliance.
        /// </summary>
        public static AudioAuditResult AuditSoundManifest(string xmlContent)
        {
            var findings = new List<AudioValidationFinding>();
            int total = 0;
            int valid = 0;
            int errors = 0;
            int warnings = 0;

            if (string.IsNullOrWhiteSpace(xmlContent))
            {
                findings.Add(new AudioValidationFinding
                {
                    SoundName = "N/A",
                    Severity = "Error",
                    Message = "XML manifest content is empty."
                });
                return new AudioAuditResult { TotalSounds = 0, ValidSounds = 0, ErrorCount = 1, WarningCount = 0, Findings = findings, Summary = "Empty XML." };
            }

            try
            {
                var doc = XDocument.Parse(xmlContent);
                var root = doc.Element("module_sounds");
                if (root == null)
                {
                    findings.Add(new AudioValidationFinding
                    {
                        SoundName = "Root",
                        Severity = "Error",
                        Message = "Missing root <module_sounds> element."
                    });
                    errors++;
                }
                else
                {
                    var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var soundElem in root.Elements("module_sound"))
                    {
                        total++;
                        string name = soundElem.Attribute("name")?.Value ?? "";
                        string category = soundElem.Attribute("sound_category")?.Value ?? "";
                        string is2dStr = soundElem.Attribute("is_2d")?.Value ?? "";
                        string path = soundElem.Attribute("path")?.Value ?? "";

                        bool hasError = false;

                        if (string.IsNullOrWhiteSpace(name))
                        {
                            findings.Add(new AudioValidationFinding
                            {
                                SoundName = "Unnamed",
                                Severity = "Error",
                                Message = "Sound definition missing required 'name' attribute."
                            });
                            hasError = true;
                        }
                        else if (!seenNames.Add(name))
                        {
                            findings.Add(new AudioValidationFinding
                            {
                                SoundName = name,
                                Severity = "Error",
                                Message = $"Duplicate sound name '{name}' detected. Engine mixer will silently overwrite earlier sound definitions."
                            });
                            hasError = true;
                        }

                        if (!ValidSoundCategories.Contains(category))
                        {
                            findings.Add(new AudioValidationFinding
                            {
                                SoundName = name,
                                Severity = "Error",
                                Message = $"Invalid sound_category '{category}'. Must be one of: ui, mission_combat, ambient, voice."
                            });
                            hasError = true;
                        }

                        if (bool.TryParse(is2dStr, out bool is2d))
                        {
                            if (string.Equals(category, "ui", StringComparison.OrdinalIgnoreCase) && !is2d)
                            {
                                findings.Add(new AudioValidationFinding
                                {
                                    SoundName = name,
                                    Severity = "Warning",
                                    Message = "UI sounds should set is_2d='true' for non-positional interface feedback."
                                });
                                warnings++;
                            }
                        }
                        else
                        {
                            findings.Add(new AudioValidationFinding
                            {
                                SoundName = name,
                                Severity = "Error",
                                Message = "Attribute 'is_2d' must be 'true' or 'false'."
                            });
                            hasError = true;
                        }

                        if (string.IsNullOrWhiteSpace(path))
                        {
                            findings.Add(new AudioValidationFinding
                            {
                                SoundName = name,
                                Severity = "Error",
                                Message = "Missing 'path' attribute pointing to audio asset in ModuleSounds/."
                            });
                            hasError = true;
                        }
                        else
                        {
                            if (path.Contains(".."))
                            {
                                findings.Add(new AudioValidationFinding
                                {
                                    SoundName = name,
                                    Severity = "Error",
                                    Message = $"Audio path '{path}' contains forbidden directory traversal sequence '..'."
                                });
                                hasError = true;
                            }

                            string ext = "";
                            try
                            {
                                ext = Path.GetExtension(path)?.ToLowerInvariant() ?? "";
                            }
                            catch
                            {
                                ext = "";
                            }

                            if (ext != ".ogg" && ext != ".wav")
                            {
                                findings.Add(new AudioValidationFinding
                                {
                                    SoundName = name,
                                    Severity = "Error",
                                    Message = $"Unsupported file format or invalid path '{ext}'. Must be compressed .ogg or low-latency .wav."
                                });
                                hasError = true;
                            }
                        }

                        if (hasError) errors++;
                        else valid++;
                    }
                }
            }
            catch (Exception ex)
            {
                findings.Add(new AudioValidationFinding
                {
                    SoundName = "XML Parser",
                    Severity = "Error",
                    Message = $"XML Parse Exception: {ex.Message}"
                });
                errors++;
            }

            string summary = $"[Audio Manifest Audit: {total} definitions analyzed]\n" +
                             $"• Valid Sounds: {valid}/{total}\n" +
                             $"• Critical Mixer Errors: {errors}\n" +
                             $"• Warnings: {warnings}\n" +
                             $"• Health Status: {(errors == 0 ? "ALL SOUNDS VALID FOR ENGINE MIXER" : "ERRORS DETECTED — ENGINE WILL SILENTLY DROP SOUNDS")}";

            return new AudioAuditResult
            {
                TotalSounds = total,
                ValidSounds = valid,
                ErrorCount = errors,
                WarningCount = warnings,
                Findings = findings,
                Summary = summary
            };
        }
    }
}
