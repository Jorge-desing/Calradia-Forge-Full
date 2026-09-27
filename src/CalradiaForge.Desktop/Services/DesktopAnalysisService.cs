using System;
using System.Globalization;
using System.Text;
using System.Threading;
using CalradiaForge.Core;
using CalradiaForge.Sdk;

namespace CalradiaForge.Desktop.Services
{
    /// <summary>Formats detached analyzer results for the desktop evidence ledger.</summary>
    internal sealed class DesktopAnalysisService
    {
        public ForgeAnalysisResult Run(string toolLabel, string targetPath, CancellationToken cancellation = default) =>
            ForgeAnalysisCatalog.Analyze(toolLabel, new()
            {
                AnalyzerId = toolLabel,
                TargetPath = targetPath,
                CancellationToken = cancellation,
                MaximumFiles = 1000,
                MaximumBytesPerFile = string.Equals(toolLabel, "FbxAsciiPreflight", StringComparison.Ordinal)
                    ? 256 * 1024 * 1024
                    : 4 * 1024 * 1024
            });

        public string Format(ForgeAnalysisResult result)
        {
            var findings = result.Findings;
            int count = findings.Count;
            StringBuilder output = new(Math.Max(512, count * 256));
            output.Append(result.Provenance.ToString().ToUpperInvariant()).AppendLine(" ANALYSIS");
            output.Append("Tool: ").Append(result.AnalyzerName).Append(" [").Append(result.AnalyzerId).AppendLine("]");
            output.Append("Input: ").AppendLine(string.IsNullOrWhiteSpace(result.TargetPath) ? "Not selected" : result.TargetPath);
            output.Append("State: ").Append(result.State).Append(" | Evidence: ").Append(result.Provenance).Append(" | Files: ").Append(result.FilesExamined);
            if (result.Truncated) output.Append(" (bounded)");
            output.AppendLine();
            output.Append("Summary: ").AppendLine(result.Summary ?? "No summary.");
            output.AppendLine();
            output.Append("EVIDENCE LEDGER (").Append(count).AppendLine(")");
            if (count == 0)
            {
                output.AppendLine("No findings. This means the selected analyzer produced no evidence; it is not a universal compatibility claim.");
                return output.ToString();
            }
            for (int i = 0; i < count; i++)
            {
                var finding = findings[i];
                output.Append('[').Append(finding.Severity ?? "Info").Append("] ").Append(finding.RuleId ?? "finding");
                if (!string.IsNullOrWhiteSpace(finding.SourcePath)) output.Append(" | ").Append(finding.SourcePath);
                if (finding.Line.HasValue) output.Append(':').Append(finding.Line.Value);
                if (finding.Column.HasValue) output.Append(':').Append(finding.Column.Value);
                output.AppendLine();
                output.Append("  Evidence: ").AppendLine(finding.Evidence ?? "No captured evidence.");
                output.Append("  Next: ").AppendLine(finding.Recommendation ?? "Review the selected input.");
            }
            return output.ToString();
        }
    }
}
