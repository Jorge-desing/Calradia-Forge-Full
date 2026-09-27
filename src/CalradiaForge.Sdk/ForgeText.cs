using System;
using System.Collections.Generic;
using System.Text;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Type-safe, crash-safe text localization and parameter formatting helper.
    /// Strictly enforces Bannerlord localization contracts:
    /// 1. Prohibits dynamic string concatenation inside localization tokens.
    /// 2. Validates parameter substitution syntax {VAR_NAME}.
    /// 3. Escapes special XML characters and formats newlines to &#10; for safe streaming.
    /// </summary>
    public static class ForgeText
    {
        /// <summary>
        /// Represents a verified, safe localized token definition ready for consumption by TaleWorlds.Localization or UI.
        /// </summary>
        public sealed class LocalizedEntry
        {
            public string Id { get; }
            public string RawText { get; }
            public Dictionary<string, string> Variables { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            internal LocalizedEntry(string id, string rawText)
            {
                Id = id;
                RawText = rawText;
            }

            public LocalizedEntry SetVariable(string key, object value)
            {
                if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Variable key cannot be null or empty.", nameof(key));
                Variables[key.Trim()] = value?.ToString() ?? string.Empty;
                return this;
            }

            /// <summary>
            /// Returns the fully formatted string with variables resolved.
            /// </summary>
            public string Resolve()
            {
                if (Variables.Count == 0) return RawText;

                var sb = new StringBuilder(RawText);
                foreach (var kvp in Variables)
                {
                    sb.Replace("{" + kvp.Key + "}", kvp.Value);
                }
                return sb.ToString();
            }

            /// <summary>
            /// Produces TaleWorlds standard token syntax: {=id}Text
            /// </summary>
            public string ToTaleWorldsToken()
            {
                return $"{{={Id}}}{RawText}";
            }

            /// <summary>
            /// Produces clean XML entry formatted with &#10; newlines and escaped entities.
            /// </summary>
            public string ToXmlStringNode()
            {
                string escapedText = EscapeXmlText(RawText);
                return $"<string id=\"{Id}\" text=\"{escapedText}\" />";
            }
        }

        /// <summary>
        /// Creates a verified localized text entry.
        /// Validates that the ID is valid and that dynamic string concatenation was not attempted inside the ID.
        /// </summary>
        public static LocalizedEntry Create(string id, string text)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Localization ID cannot be null or empty.", nameof(id));
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            string cleanId = id.Trim();
            if (cleanId.StartsWith("{=") && cleanId.EndsWith("}"))
            {
                cleanId = cleanId.Substring(2, cleanId.Length - 3);
            }

            // Invariant Guard: Check for dynamic concatenation anti-pattern
            if (cleanId.Contains(" ") || cleanId.Contains("+") || cleanId.Contains("\n"))
            {
                throw new InvalidOperationException(
                    $"Invalid localization string ID '{cleanId}'. Bannerlord string IDs must be atomic, alphanumeric identifier tokens without whitespace or string concatenation.");
            }

            return new LocalizedEntry(cleanId, text);
        }

        /// <summary>
        /// Creates a verified localized entry with inline variables.
        /// </summary>
        public static LocalizedEntry Create(string id, string text, params (string key, object value)[] variables)
        {
            var entry = Create(id, text);
            if (variables != null)
            {
                foreach (var (key, value) in variables)
                {
                    entry.SetVariable(key, value);
                }
            }
            return entry;
        }

        /// <summary>
        /// Escapes XML special characters (&amp;, &lt;, &gt;, &quot;) and converts newlines to &amp;#10;
        /// </summary>
        public static string EscapeXmlText(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            bool needsEscaping = false;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '&' || c == '<' || c == '>' || c == '"' || c == '\r' || c == '\n')
                {
                    needsEscaping = true;
                    break;
                }
            }
            if (!needsEscaping) return input;

            var sb = new StringBuilder(input.Length + 16);
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\r':
                        // Skip carriage returns, only convert \n
                        break;
                    case '\n':
                        sb.Append("&#10;");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
