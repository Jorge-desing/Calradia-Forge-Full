using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CalradiaForge.Mod
{
    internal enum OutputLineComparisonStatus
    {
        Available,
        TooManyCharacters,
        TooManyLines,
        BaselineTooManyCharacters,
        CurrentTooManyCharacters,
        BaselineTooManyLines,
        CurrentTooManyLines,
        TooManyAlignedRows,
        SearchBudgetExceeded,
        TraceBudgetExceeded
    }

    internal enum OutputLineComparisonKind
    {
        Equal,
        Changed,
        BaselineOnly,
        CurrentOnly
    }

    internal sealed class OutputLineComparisonRow
    {
        internal OutputLineComparisonRow(string baselineText, string currentText, OutputLineComparisonKind kind)
        {
            BaselineText = baselineText ?? string.Empty;
            CurrentText = currentText ?? string.Empty;
            Kind = kind;
        }

        internal string BaselineText { get; }
        internal string CurrentText { get; }
        internal OutputLineComparisonKind Kind { get; }
    }

    internal sealed class OutputLineComparisonResult
    {
        internal OutputLineComparisonResult(OutputLineComparisonStatus status, int pageIndex, int pageCount,
            int totalVisualRows, List<OutputLineComparisonRow> rows)
        {
            Status = status;
            PageIndex = pageIndex;
            PageCount = pageCount;
            TotalVisualRows = totalVisualRows;
            Rows = new ReadOnlyCollection<OutputLineComparisonRow>(rows ?? new List<OutputLineComparisonRow>());
        }

        internal OutputLineComparisonStatus Status { get; }
        internal int PageIndex { get; }
        internal int PageCount { get; }
        internal int TotalVisualRows { get; }
        internal IReadOnlyList<OutputLineComparisonRow> Rows { get; }
    }

    /// <summary>
    /// Produces a bounded, deterministic, paged side-by-side view of two immutable output strings.
    /// Search steps count frontier visits, line equality work, and the worst-case length of
    /// ordinal-ignore-case filter comparisons.
    /// </summary>
    internal static class OutputLineComparison
    {
        internal const int MaxCharacters = 262144;
        internal const int MaxLines = 4096;
        internal const int MaxAlignedRows = 8192;
        internal const int MaxSearchSteps = 500000;
        internal const int MaxTraceCells = 262144;
        internal const int RowsPerPage = 16;

        private enum EditKind
        {
            Equal,
            Delete,
            Insert
        }

        private sealed class EditOperation
        {
            internal EditOperation(EditKind kind, string baseline, string current)
            {
                Kind = kind;
                Baseline = baseline ?? string.Empty;
                Current = current ?? string.Empty;
            }

            internal EditKind Kind { get; }
            internal string Baseline { get; }
            internal string Current { get; }
        }

        private sealed class AlignedLine
        {
            internal AlignedLine(string baseline, string current, OutputLineComparisonKind kind)
            {
                Baseline = baseline ?? string.Empty;
                Current = current ?? string.Empty;
                Kind = kind;
            }

            internal string Baseline { get; }
            internal string Current { get; }
            internal OutputLineComparisonKind Kind { get; }
        }

        internal static OutputLineComparisonStatus ValidateOutput(string output)
        {
            string text = output ?? string.Empty;
            if (text.Length > MaxCharacters) return OutputLineComparisonStatus.TooManyCharacters;
            if (text.Length == 0) return OutputLineComparisonStatus.Available;

            int lineCount = 1;
            for (int index = 0; index < text.Length; index++)
            {
                char value = text[index];
                if (value != '\r' && value != '\n') continue;
                if (value == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                if (++lineCount > MaxLines) return OutputLineComparisonStatus.TooManyLines;
            }

            return OutputLineComparisonStatus.Available;
        }

        internal static OutputLineComparisonResult Compare(string baseline, string current, string filter,
            int baselineWrapWidth, int currentWrapWidth, int pageIndex)
        {
            if (baselineWrapWidth < 1) throw new ArgumentOutOfRangeException(nameof(baselineWrapWidth));
            if (currentWrapWidth < 1) throw new ArgumentOutOfRangeException(nameof(currentWrapWidth));
            if (pageIndex < 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));

            string baselineText = baseline ?? string.Empty;
            string currentText = current ?? string.Empty;
            OutputLineComparisonStatus validation = ValidateOutput(baselineText);
            if (validation == OutputLineComparisonStatus.TooManyCharacters)
                return Unavailable(OutputLineComparisonStatus.BaselineTooManyCharacters);
            if (validation == OutputLineComparisonStatus.TooManyLines)
                return Unavailable(OutputLineComparisonStatus.BaselineTooManyLines);

            validation = ValidateOutput(currentText);
            if (validation == OutputLineComparisonStatus.TooManyCharacters)
                return Unavailable(OutputLineComparisonStatus.CurrentTooManyCharacters);
            if (validation == OutputLineComparisonStatus.TooManyLines)
                return Unavailable(OutputLineComparisonStatus.CurrentTooManyLines);

            List<string> baselineLines = SplitLines(baselineText);
            List<string> currentLines = SplitLines(currentText);
            List<EditOperation> script;
            int searchSteps;
            OutputLineComparisonStatus status = BuildEditScript(baselineLines, currentLines, out script, out searchSteps);
            if (status != OutputLineComparisonStatus.Available) return Unavailable(status);

            List<AlignedLine> aligned;
            status = BuildAlignedLines(script, out aligned);
            if (status != OutputLineComparisonStatus.Available) return Unavailable(status);

            bool filtering = !string.IsNullOrWhiteSpace(filter);
            List<AlignedLine> visibleLines = aligned;
            if (filtering)
            {
                visibleLines = new List<AlignedLine>(aligned.Count);
                foreach (AlignedLine line in aligned)
                {
                    bool matches;
                    if (!Matches(line, filter, ref searchSteps, out matches))
                        return Unavailable(OutputLineComparisonStatus.SearchBudgetExceeded);
                    if (matches) visibleLines.Add(line);
                }
            }

            long visualRowCount = 0;
            foreach (AlignedLine line in visibleLines)
            {
                visualRowCount += Math.Max(FragmentCount(line.Baseline, baselineWrapWidth),
                    FragmentCount(line.Current, currentWrapWidth));
            }

            int totalRows = (int)visualRowCount;
            int pageCount = Math.Max(1, (totalRows + RowsPerPage - 1) / RowsPerPage);
            int selectedPage = Math.Min(pageIndex, pageCount - 1);
            int firstVisualRow = selectedPage * RowsPerPage;
            int afterLastVisualRow = Math.Min(totalRows, firstVisualRow + RowsPerPage);
            var pageRows = new List<OutputLineComparisonRow>(afterLastVisualRow - firstVisualRow);

            int visualRow = 0;
            foreach (AlignedLine line in visibleLines)
            {
                int fragments = Math.Max(FragmentCount(line.Baseline, baselineWrapWidth),
                    FragmentCount(line.Current, currentWrapWidth));
                int lineAfter = visualRow + fragments;
                int fragmentStart = Math.Max(0, firstVisualRow - visualRow);
                int fragmentAfter = Math.Min(fragments, afterLastVisualRow - visualRow);
                for (int fragment = fragmentStart; fragment < fragmentAfter; fragment++)
                {
                    pageRows.Add(new OutputLineComparisonRow(
                        GetFragment(line.Baseline, baselineWrapWidth, fragment),
                        GetFragment(line.Current, currentWrapWidth, fragment), line.Kind));
                }

                visualRow = lineAfter;
            }

            return new OutputLineComparisonResult(OutputLineComparisonStatus.Available, selectedPage, pageCount,
                totalRows, pageRows);
        }

        private static OutputLineComparisonResult Unavailable(OutputLineComparisonStatus status)
        {
            return new OutputLineComparisonResult(status, 0, 0, 0, new List<OutputLineComparisonRow>());
        }

        private static List<string> SplitLines(string source)
        {
            var lines = new List<string>();
            if (source.Length == 0) return lines;

            int start = 0;
            for (int index = 0; index < source.Length; index++)
            {
                char value = source[index];
                if (value != '\r' && value != '\n') continue;

                lines.Add(source.Substring(start, index - start));
                if (value == '\r' && index + 1 < source.Length && source[index + 1] == '\n') index++;
                start = index + 1;
            }

            if (start < source.Length || source[source.Length - 1] == '\r' || source[source.Length - 1] == '\n')
                lines.Add(source.Substring(start));
            return lines;
        }

        private static OutputLineComparisonStatus BuildEditScript(IList<string> baseline, IList<string> current,
            out List<EditOperation> operations, out int searchStepsUsed)
        {
            operations = new List<EditOperation>();
            int maximumDepth = baseline.Count + current.Count;
            var trace = new List<int[]>();
            int traceCells = 0;
            int searchSteps = 0;
            searchStepsUsed = 0;
            int foundDepth = -1;

            for (int depth = 0; depth <= maximumDepth; depth++)
            {
                int cellCount = depth * 2 + 1;
                if (traceCells + cellCount > MaxTraceCells)
                {
                    searchStepsUsed = searchSteps;
                    return OutputLineComparisonStatus.TraceBudgetExceeded;
                }
                traceCells += cellCount;

                var frontier = new int[cellCount];
                int minimumDiagonal = -depth;
                for (int diagonal = minimumDiagonal; diagonal <= depth; diagonal += 2)
                {
                    if (++searchSteps > MaxSearchSteps)
                    {
                        searchStepsUsed = searchSteps;
                        return OutputLineComparisonStatus.SearchBudgetExceeded;
                    }

                    int x;
                    if (depth == 0)
                    {
                        x = 0;
                    }
                    else if (diagonal == -depth ||
                        (diagonal != depth && GetFrontier(trace[depth - 1], depth - 1, diagonal - 1) <
                            GetFrontier(trace[depth - 1], depth - 1, diagonal + 1)))
                    {
                        x = GetFrontier(trace[depth - 1], depth - 1, diagonal + 1);
                    }
                    else
                    {
                        x = GetFrontier(trace[depth - 1], depth - 1, diagonal - 1) + 1;
                    }

                    int y = x - diagonal;
                    while (x < baseline.Count && y < current.Count)
                    {
                        bool equal;
                        if (!LinesEqual(baseline[x], current[y], ref searchSteps, out equal))
                        {
                            searchStepsUsed = searchSteps;
                            return OutputLineComparisonStatus.SearchBudgetExceeded;
                        }
                        if (!equal) break;
                        x++;
                        y++;
                    }

                    frontier[diagonal + depth] = x;
                    if (x >= baseline.Count && y >= current.Count)
                    {
                        foundDepth = depth;
                        break;
                    }
                }

                trace.Add(frontier);
                if (foundDepth >= 0) break;
            }

            searchStepsUsed = searchSteps;
            if (foundDepth < 0) return OutputLineComparisonStatus.SearchBudgetExceeded;

            int backtrackX = baseline.Count;
            int backtrackY = current.Count;
            var reversed = new List<EditOperation>(baseline.Count + current.Count);
            for (int depth = foundDepth; depth > 0; depth--)
            {
                int diagonal = backtrackX - backtrackY;
                int[] previous = trace[depth - 1];
                int previousDiagonal;
                if (diagonal == -depth ||
                    (diagonal != depth && GetFrontier(previous, depth - 1, diagonal - 1) <
                        GetFrontier(previous, depth - 1, diagonal + 1)))
                {
                    previousDiagonal = diagonal + 1;
                }
                else
                {
                    previousDiagonal = diagonal - 1;
                }

                int previousX = GetFrontier(previous, depth - 1, previousDiagonal);
                int previousY = previousX - previousDiagonal;
                while (backtrackX > previousX && backtrackY > previousY)
                {
                    reversed.Add(new EditOperation(EditKind.Equal, baseline[backtrackX - 1], current[backtrackY - 1]));
                    backtrackX--;
                    backtrackY--;
                }

                if (backtrackX == previousX)
                {
                    reversed.Add(new EditOperation(EditKind.Insert, string.Empty, current[backtrackY - 1]));
                    backtrackY--;
                }
                else
                {
                    reversed.Add(new EditOperation(EditKind.Delete, baseline[backtrackX - 1], string.Empty));
                    backtrackX--;
                }
            }

            while (backtrackX > 0 && backtrackY > 0)
            {
                reversed.Add(new EditOperation(EditKind.Equal, baseline[backtrackX - 1], current[backtrackY - 1]));
                backtrackX--;
                backtrackY--;
            }
            while (backtrackX > 0)
            {
                reversed.Add(new EditOperation(EditKind.Delete, baseline[--backtrackX], string.Empty));
            }
            while (backtrackY > 0)
            {
                reversed.Add(new EditOperation(EditKind.Insert, string.Empty, current[--backtrackY]));
            }

            reversed.Reverse();
            operations = reversed;
            return OutputLineComparisonStatus.Available;
        }

        private static int GetFrontier(int[] frontier, int depth, int diagonal)
        {
            return frontier[diagonal + depth];
        }

        private static bool LinesEqual(string left, string right, ref int searchSteps, out bool equal)
        {
            if (left.Length != right.Length)
            {
                if (++searchSteps > MaxSearchSteps)
                {
                    equal = false;
                    return false;
                }

                equal = false;
                return true;
            }

            if (left.Length == 0)
            {
                if (++searchSteps > MaxSearchSteps)
                {
                    equal = false;
                    return false;
                }

                equal = true;
                return true;
            }

            for (int index = 0; index < left.Length; index++)
            {
                if (++searchSteps > MaxSearchSteps)
                {
                    equal = false;
                    return false;
                }

                if (left[index] != right[index])
                {
                    equal = false;
                    return true;
                }
            }

            equal = true;
            return true;
        }

        private static OutputLineComparisonStatus BuildAlignedLines(IList<EditOperation> operations,
            out List<AlignedLine> aligned)
        {
            aligned = new List<AlignedLine>();
            var deleted = new List<string>();
            var inserted = new List<string>();

            for (int index = 0; index <= operations.Count; index++)
            {
                EditOperation operation = index < operations.Count ? operations[index] : null;
                if (operation != null && operation.Kind != EditKind.Equal)
                {
                    if (operation.Kind == EditKind.Delete) deleted.Add(operation.Baseline);
                    else inserted.Add(operation.Current);
                    continue;
                }

                if (deleted.Count > 0 || inserted.Count > 0)
                {
                    int runRows = Math.Max(deleted.Count, inserted.Count);
                    for (int row = 0; row < runRows; row++)
                    {
                        bool hasBaseline = row < deleted.Count;
                        bool hasCurrent = row < inserted.Count;
                        OutputLineComparisonKind kind = hasBaseline && hasCurrent
                            ? OutputLineComparisonKind.Changed
                            : hasBaseline ? OutputLineComparisonKind.BaselineOnly : OutputLineComparisonKind.CurrentOnly;
                        aligned.Add(new AlignedLine(hasBaseline ? deleted[row] : string.Empty,
                            hasCurrent ? inserted[row] : string.Empty, kind));
                        if (aligned.Count > MaxAlignedRows)
                            return OutputLineComparisonStatus.TooManyAlignedRows;
                    }

                    deleted.Clear();
                    inserted.Clear();
                }

                if (operation != null)
                {
                    aligned.Add(new AlignedLine(operation.Baseline, operation.Current, OutputLineComparisonKind.Equal));
                    if (aligned.Count > MaxAlignedRows)
                        return OutputLineComparisonStatus.TooManyAlignedRows;
                }
            }

            return OutputLineComparisonStatus.Available;
        }

        private static bool Matches(AlignedLine line, string filter, ref int searchSteps, out bool matches)
        {
            bool baselineMatch;
            if (!TryContainsOrdinalIgnoreCase(line.Baseline, filter, ref searchSteps, out baselineMatch))
            {
                matches = false;
                return false;
            }

            if (baselineMatch)
            {
                matches = true;
                return true;
            }

            return TryContainsOrdinalIgnoreCase(line.Current, filter, ref searchSteps, out matches);
        }

        private static bool TryContainsOrdinalIgnoreCase(string text, string query, ref int searchSteps, out bool found)
        {
            found = false;
            if (query.Length == 0)
            {
                found = true;
                return true;
            }
            if (query.Length > text.Length) return true;

            int lastStart = text.Length - query.Length;
            for (int start = 0; start <= lastStart; start++)
            {
                // Count the worst-case ordinal comparison length before comparing. This gives
                // the UI-thread filter a firm bound even for highly repetitive input.
                if (searchSteps > MaxSearchSteps - query.Length)
                    return false;
                searchSteps += query.Length;
                if (string.Compare(text, start, query, 0, query.Length, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    found = true;
                    return true;
                }
            }

            return true;
        }

        private static int FragmentCount(string text, int width)
        {
            if (text.Length == 0) return 1;

            int count = 0;
            int start = 0;
            while (start < text.Length)
            {
                start = GetNextFragmentEnd(text, start, width);
                count++;
            }

            return count;
        }

        private static string GetFragment(string text, int width, int fragment)
        {
            if (text.Length == 0) return string.Empty;

            int start = 0;
            for (int index = 0; index < fragment && start < text.Length; index++)
                start = GetNextFragmentEnd(text, start, width);
            if (start >= text.Length) return string.Empty;

            int end = GetNextFragmentEnd(text, start, width);
            return text.Substring(start, end - start);
        }

        private static int GetNextFragmentEnd(string text, int start, int width)
        {
            int remaining = text.Length - start;
            int end = width >= remaining ? text.Length : start + width;
            if (end < text.Length && end > start &&
                char.IsHighSurrogate(text[end - 1]) && char.IsLowSurrogate(text[end]))
            {
                // Keep a valid UTF-16 surrogate pair together. If the requested width is one
                // code unit, allow the pair to occupy that single visual fragment.
                end = end - start > 1 ? end - 1 : end + 1;
            }

            return end;
        }
    }
}
