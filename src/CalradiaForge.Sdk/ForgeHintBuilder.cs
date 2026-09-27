using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Fluid builder for creating structured, tactical Gauntlet UI hints and tooltips in Mount &amp; Blade II Bannerlord.
    /// Produces formatted multi-line hints, XML attributes (Hint.HintText), localized strings, and C# ViewModel properties.
    /// </summary>
    public sealed class ForgeHintBuilder
    {
        private string _title;
        private string _description;
        private string _shortcut;
        private string _warning;
        private readonly List<(string Requirement, bool IsMet)> _requirements = new List<(string Requirement, bool IsMet)>();
        private readonly List<(string Label, string Value)> _metadata = new List<(string Label, string Value)>();

        /// <summary>
        /// Creates a new instance of ForgeHintBuilder.
        /// </summary>
        public static ForgeHintBuilder Create() => new ForgeHintBuilder();

        /// <summary>
        /// Creates a new instance of ForgeHintBuilder initialized with a title.
        /// </summary>
        public static ForgeHintBuilder Create(string title) => new ForgeHintBuilder().WithTitle(title);

        /// <summary>
        /// Sets the title header for the hint (rendered in prominent uppercase / brass).
        /// </summary>
        public ForgeHintBuilder WithTitle(string title)
        {
            _title = title?.Trim();
            return this;
        }

        /// <summary>
        /// Sets the core descriptive explanation of the element.
        /// </summary>
        public ForgeHintBuilder WithDescription(string description)
        {
            _description = description?.Trim();
            return this;
        }

        /// <summary>
        /// Sets an associated keyboard or mouse shortcut (e.g. "[Ctrl + Enter]" or "[Shift + Click]").
        /// </summary>
        public ForgeHintBuilder WithShortcut(string shortcut)
        {
            _shortcut = shortcut?.Trim();
            return this;
        }

        /// <summary>
        /// Adds a prerequisite condition or requirement with an interactive met/unmet indicator.
        /// </summary>
        public ForgeHintBuilder WithRequirement(string requirement, bool isMet)
        {
            if (!string.IsNullOrWhiteSpace(requirement))
            {
                _requirements.Add((requirement.Trim(), isMet));
            }
            return this;
        }

        /// <summary>
        /// Adds a warning or caution notice (e.g. "[!] Irreversible action").
        /// </summary>
        public ForgeHintBuilder WithWarning(string warning)
        {
            _warning = warning?.Trim();
            return this;
        }

        /// <summary>
        /// Adds custom metadata key-value pair (e.g. "Cost", "500 Gold").
        /// </summary>
        public ForgeHintBuilder WithMeta(string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(label))
            {
                _metadata.Add((label.Trim(), value?.Trim() ?? ""));
            }
            return this;
        }

        /// <summary>
        /// Builds the formatted multi-line hint string suitable for runtime display in Bannerlord's Gauntlet tooltip engine.
        /// </summary>
        public string BuildHintText()
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(_title))
            {
                sb.AppendLine(_title);
                sb.AppendLine("────────────────────────────");
            }

            if (!string.IsNullOrWhiteSpace(_description))
            {
                sb.AppendLine(_description);
            }

            if (_metadata.Count > 0)
            {
                if (sb.Length > 0 && !EndsWithDoubleNewline(sb)) sb.AppendLine();
                for (int i = 0; i < _metadata.Count; i++)
                {
                    var (label, value) = _metadata[i];
                    sb.AppendLine($"• {label}: {value}");
                }
            }

            if (_requirements.Count > 0)
            {
                if (sb.Length > 0 && !EndsWithDoubleNewline(sb)) sb.AppendLine();
                sb.AppendLine("Requirements:");
                for (int i = 0; i < _requirements.Count; i++)
                {
                    var (req, isMet) = _requirements[i];
                    string mark = isMet ? "[✓]" : "[✗]";
                    sb.AppendLine($"  {mark} {req}");
                }
            }

            if (!string.IsNullOrWhiteSpace(_warning))
            {
                if (sb.Length > 0 && !EndsWithDoubleNewline(sb)) sb.AppendLine();
                sb.AppendLine($"[!] Warning: {_warning}");
            }

            if (!string.IsNullOrWhiteSpace(_shortcut))
            {
                if (sb.Length > 0 && !EndsWithDoubleNewline(sb)) sb.AppendLine();
                sb.AppendLine($"Shortcut: {_shortcut}");
            }

            return sb.ToString().TrimEnd();
        }

        private static bool EndsWithDoubleNewline(StringBuilder sb)
        {
            if (sb.Length < 2) return false;
            if (sb[sb.Length - 1] == '\n' && sb[sb.Length - 2] == '\n') return true;
            return sb.Length >= 4 && sb[sb.Length - 1] == '\n' && sb[sb.Length - 2] == '\r' && sb[sb.Length - 3] == '\n' && sb[sb.Length - 4] == '\r';
        }

        /// <summary>
        /// Generates the Gauntlet XML attribute syntax.
        /// When propertyName is provided, outputs Hint.HintText="@PropertyName".
        /// When directStringId is provided, outputs Hint.HintText="{=string_id}Fallback Text".
        /// </summary>
        public string BuildXmlAttribute(string propertyName = null, string directStringId = null)
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                string prop = propertyName.StartsWith("@") ? propertyName : "@" + propertyName;
                return $"Hint.HintText=\"{prop}\"";
            }

            if (!string.IsNullOrWhiteSpace(directStringId))
            {
                string text = BuildHintText().Replace("\"", "&quot;").Replace("\r\n", "&#10;").Replace("\n", "&#10;");
                return $"Hint.HintText=\"{{={directStringId}}}{text}\"";
            }

            string defaultText = BuildHintText().Replace("\"", "&quot;").Replace("\r\n", "&#10;").Replace("\n", "&#10;");
            return $"Hint.HintText=\"{defaultText}\"";
        }

        /// <summary>
        /// Generates a valid Bannerlord localization XML node (&lt;string id="..." text="..." /&gt;) with encoded newlines.
        /// </summary>
        public string BuildLocalizationXml(string stringId)
        {
            if (string.IsNullOrWhiteSpace(stringId))
            {
                stringId = ComputeHashId(BuildHintText());
            }

            string text = BuildHintText()
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("\r\n", "&#10;")
                .Replace("\n", "&#10;");

            return $"<string id=\"{stringId}\" text=\"{text}\" />";
        }

        /// <summary>
        /// Generates a C# ViewModel property snippet decorated with [DataSourceProperty] for Gauntlet binding.
        /// </summary>
        public string BuildViewModelPropertySnippet(string propertyName, string textProviderMethod = "T")
        {
            if (string.IsNullOrWhiteSpace(propertyName)) propertyName = "ActionHint";
            string hint = BuildHintText().Replace("\"", "\"\"").Replace("\r\n", "\\n").Replace("\n", "\\n");

            var sb = new StringBuilder();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Tactical hint for Gauntlet binding: @{propertyName}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        [DataSourceProperty]");
            sb.AppendLine($"        public string {propertyName} => {textProviderMethod}(@\"{hint}\");");

            return sb.ToString();
        }

        private static string ComputeHashId(string text)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""));
                var hex = new StringBuilder(10);
                for (int i = 0; i < 4; i++)
                {
                    hex.Append(bytes[i].ToString("x2"));
                }
                return "hint_" + hex.ToString();
            }
        }
    }
}
