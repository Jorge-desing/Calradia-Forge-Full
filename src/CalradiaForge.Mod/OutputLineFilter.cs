using System;
using System.Collections.Generic;

namespace CalradiaForge.Mod
{
    /// <summary>
    /// Builds the display rows for the output ledger without changing the source report.
    /// </summary>
    internal static class OutputLineFilter
    {
        internal static List<string> SplitLines(string source)
        {
            if (string.IsNullOrEmpty(source)) return new List<string>();

            string normalized = source.Replace("\r\n", "\n").Replace('\r', '\n');
            return new List<string>(normalized.Split(new[] { '\n' }, StringSplitOptions.None));
        }

        internal static List<string> FilterAndWrap(IList<string> sourceLines, string query, int wrapWidth)
        {
            if (sourceLines == null) throw new ArgumentNullException(nameof(sourceLines));
            if (wrapWidth < 1) throw new ArgumentOutOfRangeException(nameof(wrapWidth));

            bool filterEnabled = !string.IsNullOrWhiteSpace(query);
            var displayLines = new List<string>();
            for (int index = 0; index < sourceLines.Count; index++)
            {
                string line = sourceLines[index] ?? string.Empty;
                if (filterEnabled && line.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (line.Length == 0)
                {
                    displayLines.Add(string.Empty);
                    continue;
                }

                for (int offset = 0; offset < line.Length; offset += wrapWidth)
                    displayLines.Add(line.Substring(offset, Math.Min(wrapWidth, line.Length - offset)));
            }

            return displayLines;
        }
    }
}
