using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CalradiaForge.Sdk
{
    /// <summary>Describes how much evidence backs an analysis result.</summary>
    public enum ForgeAnalysisProvenance { Verified, Structural, Template, Unavailable }
    public enum ForgeAnalysisState { Completed, NotRun, Unsupported, Failed, Cancelled }

    /// <summary>Bounded, file-oriented input for an analyzer. Game objects are never exposed.</summary>
    public sealed class ForgeAnalysisRequest
    {
        public string TargetPath { get; set; }
        public string AnalyzerId { get; set; }
        public int MaximumFiles { get; set; } = 1000;
        public int MaximumBytesPerFile { get; set; } = 4 * 1024 * 1024;
        /// <summary>Allows bounded analyzers to stop during traversal and parsing.</summary>
        public CancellationToken CancellationToken { get; set; }
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>A concrete, navigable piece of analysis evidence.</summary>
    public sealed class ForgeDiagnosticFinding
    {
        public string RuleId { get; set; }
        public string Severity { get; set; } = "Info";
        public string SourcePath { get; set; }
        public int? Line { get; set; }
        public int? Column { get; set; }
        public string Evidence { get; set; }
        public string Recommendation { get; set; }
    }

    /// <summary>Detached result returned by a real local analyzer.</summary>
    public sealed class ForgeAnalysisResult
    {
        public string AnalyzerId { get; set; }
        public string AnalyzerName { get; set; }
        public ForgeAnalysisProvenance Provenance { get; set; } = ForgeAnalysisProvenance.Unavailable;
        public ForgeAnalysisState State { get; set; } = ForgeAnalysisState.NotRun;
        public string TargetPath { get; set; }
        public string StartedAt { get; set; } = DateTime.UtcNow.ToString("O");
        public string CompletedAt { get; set; }
        public int FilesExamined { get; set; }
        public bool Truncated { get; set; }
        public string Summary { get; set; }
        public List<ForgeDiagnosticFinding> Findings { get; set; } = new List<ForgeDiagnosticFinding>();

        public bool HasErrors
        {
            get
            {
                if (Findings == null || Findings.Count == 0) return false;
                for (int i = 0; i < Findings.Count; i++)
                {
                    if (string.Equals(Findings[i]?.Severity, "Error", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }
        }
    }

    /// <summary>Read-only extension point for bounded local diagnostics.</summary>
    public interface IForgeAnalyzer
    {
        Descriptor Descriptor { get; }
        ForgeAnalysisResult Analyze(ForgeAnalysisRequest request);
    }

    /// <summary>Available only while the Forge host is connected.</summary>
    public interface IForgeAnalysisRegistry
    {
        void Register(IForgeAnalyzer analyzer);
        IReadOnlyList<Descriptor> Analyzers { get; }
        ForgeAnalysisResult Analyze(string analyzerId, ForgeAnalysisRequest request);
    }

    public enum ForgeRuntimeCapability { Input, SaveSerialization, DebugRendering, AgentInspection }

    /// <summary>Reports host services that are actually implemented in the current context.</summary>
    public interface IForgeRuntimeCapabilities
    {
        bool Supports(ForgeRuntimeCapability capability);
        string DescribeUnavailable(ForgeRuntimeCapability capability);
    }

    public sealed class ForgeCapabilityUnavailableException : InvalidOperationException
    {
        public ForgeRuntimeCapability Capability { get; }
        public ForgeCapabilityUnavailableException(ForgeRuntimeCapability capability, string message)
            : base(message) { Capability = capability; }
    }
}
