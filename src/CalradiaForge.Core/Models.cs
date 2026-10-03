using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using CalradiaForge.Sdk;

namespace CalradiaForge.Core
{
    public static class Json
    {
        public static string Serialize<T>(T value) { using(var m = new MemoryStream()) { new DataContractJsonSerializer(typeof(T)).WriteObject(m, value); return Encoding.UTF8.GetString(m.ToArray()); } }
        public static T Deserialize<T>(string text) { using(var m = new MemoryStream(Encoding.UTF8.GetBytes(text))) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(m); }
        public static void Save<T>(string path, T value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllText(tmp, Serialize(value), Encoding.UTF8); if(File.Exists(path)) AtomicFileReplacement.Replace(tmp,path); else File.Move(tmp,path); }
            finally { if(File.Exists(tmp)) File.Delete(tmp); }
        }
    }
    public sealed class Request { public int Version { get; set; } = ForgeProtocol.EnvelopeVersion; public string Id { get; set; } = Guid.NewGuid().ToString("N"); public string Action { get; set; } public string Argument { get; set; } public int Seed { get; set; } = 148; }
    public sealed class Response { public int Version { get; set; } = ForgeProtocol.EnvelopeVersion; public string Id { get; set; } public bool Success { get; set; } public string Error { get; set; } public string Data { get; set; } }
    public sealed class LogEntry { public string Time { get; set; } public string Session { get; set; } public string Module { get; set; } public string Level { get; set; } public string Message { get; set; } }
    public sealed class ObjectSnapshot { public string Type { get; set; } public string Id { get; set; } public string Name { get; set; } public Dictionary<string,string> Properties { get; set; } = new Dictionary<string,string>(); }
    public sealed class TestResult { public string Id { get; set; } public string Status { get; set; } public string Error { get; set; } public string CleanupError { get; set; } public int Seed { get; set; } public double Milliseconds { get; set; } public string Context { get; set; } public string StartedAt { get; set; } public List<string> Steps { get; set; } = new List<string>(); }
    public sealed class Module { public string Id { get; set; } public string Version { get; set; } public string Folder { get; set; } public List<string> Dependencies { get; set; } = new List<string>(); }
    public sealed class ModuleDiagnostics { public List<Module> Modules { get; set; } = new List<Module>(); public List<Finding> Findings { get; set; } = new List<Finding>(); }
    public sealed class ForgeOwnedHookRecord
    {
        public string Id { get; set; }
        public string Owner { get; set; }
        public string TargetMethod { get; set; }
        public string State { get; set; }
        public string Detail { get; set; }
        public bool HasPrefix { get; set; }
        public bool HasPostfix { get; set; }
        public bool HasFinalizer { get; set; }
        public bool HasTranspiler { get; set; }
        public int? Priority { get; set; }
        public List<string> Before { get; set; } = new List<string>();
        public List<string> After { get; set; } = new List<string>();
    }
    public sealed class ForgeOwnedPatchRecord
    {
        public string PatchId { get; set; }
        public string Owner { get; set; }
        public string TargetMethod { get; set; }
        public string ReplacementMethod { get; set; }
        public string State { get; set; }
        public bool IsIntact { get; set; }
    }
    public sealed class ExternalPatchObservation
    {
        public string Owner { get; set; }
        public string Kind { get; set; }
        public int? Priority { get; set; }
        public int? Index { get; set; }
        public List<string> Before { get; set; } = new List<string>();
        public List<string> After { get; set; } = new List<string>();
        public string PatchAssembly { get; set; }
        public string PatchType { get; set; }
        public string PatchMethod { get; set; }
        public string PatchSignature { get; set; }
    }
    public sealed class ExternalPatchTarget
    {
        public string Assembly { get; set; }
        public string DeclaringType { get; set; }
        public string Method { get; set; }
        public string Signature { get; set; }
        internal int StableSortToken { get; set; }
        // Active, No metadata, or Unreadable. Empty rows are retained as evidence but never
        // presented as a confirmed active patch target.
        public string MetadataStatus { get; set; } = "Active";
        public List<string> Owners { get; set; } = new List<string>();
        public bool HasMultipleOwners { get; set; }
        public List<ExternalPatchObservation> Patches { get; set; } = new List<ExternalPatchObservation>();
    }
    public sealed class ExternalPatchRuntimeSnapshot
    {
        public string Status { get; set; } = "NotLoaded";
        public string Detail { get; set; }
        public string CapturedAt { get; set; }
        public string RuntimeAssembly { get; set; }
        public string RuntimeVersion { get; set; }
        public int DiscoveredTargetCount { get; set; }
        public int DisplayedTargetCount { get; set; }
        public int ActiveTargetCount { get; set; }
        public int EmptyMetadataTargetCount { get; set; }
        public int OwnerCount { get; set; }
        public int SharedTargetCount { get; set; }
        public int SkippedTargetCount { get; set; }
        public bool Truncated { get; set; }
        public List<string> Notes { get; set; } = new List<string>();
        public List<ExternalPatchTarget> Targets { get; set; } = new List<ExternalPatchTarget>();
    }
    public sealed class ForgePatchDiagnosticsSnapshot
    {
        public string Status { get; set; } = "Not captured.";
        public string CapturedAt { get; set; }
        public bool IsStale { get; set; }
        public int HookCount { get; set; }
        public int PatchCount { get; set; }
        public int ConflictCount { get; set; }
        public int FailedCount { get; set; }
        public bool Truncated { get; set; }
        public List<string> Notes { get; set; } = new List<string>();
        public List<ForgeOwnedHookRecord> Hooks { get; set; } = new List<ForgeOwnedHookRecord>();
        public List<ForgeOwnedPatchRecord> Patches { get; set; } = new List<ForgeOwnedPatchRecord>();
        public ExternalPatchRuntimeSnapshot ExternalRuntime { get; set; } = new ExternalPatchRuntimeSnapshot();
    }
    public sealed class PatchBlueprintDeclaration
    {
        public string ProviderId { get; set; }
        public string Module { get; set; }
        public string ProviderName { get; set; }
        public Context Context { get; set; }
        public PatchBlueprint Blueprint { get; set; } = new PatchBlueprint();
    }
    public sealed class PatchBlueprintCapture
    {
        public int ProviderCount { get; set; }
        public bool Truncated { get; set; }
        public List<PatchBlueprintDeclaration> Declarations { get; set; } = new List<PatchBlueprintDeclaration>();
        public List<Finding> Findings { get; set; } = new List<Finding>();
    }
    public sealed class PatchPreflightOutcome
    {
        public PatchBlueprintDeclaration Declaration { get; set; } = new PatchBlueprintDeclaration();
        public string Status { get; set; }
        public bool Resolved { get; set; }
        public string ResolvedAssembly { get; set; }
        public string ResolvedType { get; set; }
        public string ResolvedMember { get; set; }
        public string ResolvedSignature { get; set; }
        public bool CallbackResolved { get; set; }
        public string ResolvedCallbackAssembly { get; set; }
        public string ResolvedCallbackType { get; set; }
        public string ResolvedCallbackMember { get; set; }
        public string ResolvedCallbackSignature { get; set; }
        public List<string> Notes { get; set; } = new List<string>();
    }
    public sealed class PatchPreflightSnapshot
    {
        public string Status { get; set; }
        public string CapturedAt { get; set; }
        public bool IsStale { get; set; }
        public string Context { get; set; }
        public int ProviderCount { get; set; }
        public int BlueprintCount { get; set; }
        public int ResolvedCount { get; set; }
        public int ReviewCount { get; set; }
        public bool Truncated { get; set; }
        public List<string> Notes { get; set; } = new List<string>();
        public List<Finding> Findings { get; set; } = new List<Finding>();
        public List<PatchPreflightOutcome> Outcomes { get; set; } = new List<PatchPreflightOutcome>();
    }
    public sealed class SessionReport { public string SuiteVersion { get; set; } = SuiteInfo.Version; public string GameVersion { get; set; } public string Session { get; set; } public string Date { get; set; } = DateTime.UtcNow.ToString("O"); public ModuleDiagnostics ModuleDiagnostics { get; set; } public List<LogEntry> Logs { get; set; } = new List<LogEntry>(); public List<ObjectSnapshot> Snapshots { get; set; } = new List<ObjectSnapshot>(); public List<TestResult> Tests { get; set; } = new List<TestResult>(); public Dictionary<string,double> Metrics { get; set; } = new Dictionary<string,double>(); public ForgePatchDiagnosticsSnapshot PatchDiagnostics { get; set; } = new ForgePatchDiagnosticsSnapshot { Status="Not captured." }; public ForgeWeaveSnapshot ForgeWeave { get; set; } = new ForgeWeaveSnapshot {Status="Not captured."}; public PatchPreflightSnapshot PatchPreflight { get; set; } = new PatchPreflightSnapshot {Status="Not captured."}; }
    public sealed class Settings { public string Language { get; set; } = Localization.DefaultLanguage; public string Hotkey { get; set; } = "F10"; public double Scale { get; set; } = 1; }
    public static class Paths
    {
        public static string Data => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CalradiaForge");
        public static string Sessions => Path.Combine(Data,"sessions");
    }
    public static class ForgeProtocol
    {
        public const int EnvelopeVersion = 1;
        public const int Version = 2;
        static readonly string[] actions = new[] { "hello", "summary", "scan", "modules", "dependencies", "diagnostics", "logs", "inspect", "pin", "compare", "snapshots", "unpin", "tests", "commands", "command", "test-mode", "confirm-copy", "run", "run-batch", "metrics", "framework", "event-journal", "replay", "patch-diagnostics", "patch-blueprints", "patch-preflight", "hook-snapshots", "hook-verify", "hook-apply-plan", "hook-apply-confirm", "hook-revert-plan", "hook-revert-confirm", "hook-plan-cancel", "report", "export", "panel-open", "panel-close", "language", "agent-memory" };
        public static IReadOnlyList<string> Actions => actions;
        public static string[] Hello(string suiteVersion,string targetGameVersion)
        {
            var values=new List<string> { "protocol:"+Version, "suite:"+(suiteVersion??""), "target:"+(targetGameVersion??"") };
            values.AddRange(actions);
            return values.ToArray();
        }
    }
}
