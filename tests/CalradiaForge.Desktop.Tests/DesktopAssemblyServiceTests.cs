using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using CalradiaForge.Desktop.Services;

internal static class DesktopAssemblyServiceTests
{
    public static (string Name, Func<Task> Run)[] Cases => new (string Name, Func<Task> Run)[]
    {
        ("Desktop assembly version preview writes no files", () => RunSync(PreviewIsReadOnly)),
        ("Desktop assembly copy verifies backup and output hashes", () => RunSync(ApplyVerifiesCopies)),
        ("Desktop assembly patch preserves a competing output at commit", () => RunSync(PreserveCompetingOutput)),
        ("Desktop assembly cancellation before commit removes staging", () => RunSync(CancellationBeforeCommit)),
        ("Desktop assembly patch honors cancellation before mutation", () => RunSync(CancellationBeforeApply)),
        ("Desktop assembly audit evaluates distribution metadata and rules", () => RunSync(AuditEvaluatesRules))
    };

    static Task RunSync(Action action) { action(); return Task.CompletedTask; }

    static void PreviewIsReadOnly()
    {
        var root = NewRoot("desktop-assembly-preview");
        try
        {
            var source = CopyTestAssembly(root);
            var output = Path.Combine(root, "out", "source.dll");
            var request = WriteRequest(root, "preview-set-assembly-version", source, output);
            var service = new DesktopAssemblyService();
            var result = service.PreviewAndWriteVersionPatch(request, CancellationToken.None);
            Check(result.Contains("NO FILES WRITTEN") && !File.Exists(output) && !File.Exists(output + ".source.bak"),
                "Preview must not write an output or backup.");
        }
        finally { Directory.Delete(root, true); }
    }

    static void ApplyVerifiesCopies()
    {
        var root = NewRoot("desktop-assembly-apply");
        try
        {
            var source = CopyTestAssembly(root);
            var output = Path.Combine(root, "out", "source.dll");
            var request = WriteRequest(root, "apply-set-assembly-version", source, output);
            var sourceHash = Hash(source);
            var result = new DesktopAssemblyService().PreviewAndWriteVersionPatch(request, CancellationToken.None);
            var backup = output + ".source.bak";
            Check(File.Exists(output) && File.Exists(backup), "The output copy and backup must both exist after apply.");
            Check(Hash(source) == sourceHash && Hash(backup) == sourceHash, "The source and verified backup must retain identical bytes.");
            Check(AssemblyDefinition.FromFile(output).Version.ToString() == "1.2.3.4", "The output copy must have the requested metadata version.");
            Check(result.Contains("Input SHA-256: " + sourceHash), "The input hash must retain its uppercase, delimiter-free SHA-256 representation.");
            Check(result.Contains("Output SHA-256: " + Hash(output)), "The report must contain the committed output hash.");
        }
        finally { Directory.Delete(root, true); }
    }

    static void PreserveCompetingOutput()
    {
        var root = NewRoot("desktop-assembly-race");
        try
        {
            var source = CopyTestAssembly(root);
            var output = Path.Combine(root, "out", "source.dll");
            var request = WriteRequest(root, "apply-set-assembly-version", source, output);
            var service = new DesktopAssemblyService();
            var competingContent = "created by another process";
            var callbackRan = false;
            service.BeforeOutputCommit = path =>
            {
                callbackRan = true;
                File.WriteAllText(path, competingContent);
            };

            try
            {
                service.PreviewAndWriteVersionPatch(request, CancellationToken.None);
                throw new Exception("A racing destination was unexpectedly overwritten.");
            }
            catch (IOException) { }

            Check(callbackRan && File.Exists(output) && File.ReadAllText(output) == competingContent,
                "A destination owned by another process must survive the failed atomic move.");
            Check(!File.Exists(output + ".source.bak"), "The transaction should clean up its own verified backup after a failed commit.");
            Check(Directory.GetFiles(Path.GetDirectoryName(output), "*.forge-tmp-*").Length == 0,
                "A failed commit must remove its temporary output.");
        }
        finally { Directory.Delete(root, true); }
    }

    static void CancellationBeforeApply()
    {
        var root = NewRoot("desktop-assembly-cancel");
        try
        {
            var source = CopyTestAssembly(root);
            var output = Path.Combine(root, "out", "source.dll");
            var request = WriteRequest(root, "apply-set-assembly-version", source, output);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            try
            {
                new DesktopAssemblyService().PreviewAndWriteVersionPatch(request, cancellation.Token);
                throw new Exception("A canceled patch unexpectedly completed.");
            }
            catch (OperationCanceledException) { }
            Check(!File.Exists(output) && !File.Exists(output + ".source.bak"), "Cancellation before work must leave no output or backup.");
        }
        finally { Directory.Delete(root, true); }
    }

    static void CancellationBeforeCommit()
    {
        var root = NewRoot("desktop-assembly-cancel-commit");
        try
        {
            var source = CopyTestAssembly(root);
            var output = Path.Combine(root, "out", "source.dll");
            var request = WriteRequest(root, "apply-set-assembly-version", source, output);
            using var cancellation = new CancellationTokenSource();
            var service = new DesktopAssemblyService { BeforeOutputCommit = _ => cancellation.Cancel() };
            try
            {
                service.PreviewAndWriteVersionPatch(request, cancellation.Token);
                throw new Exception("Cancellation before commit was ignored.");
            }
            catch (OperationCanceledException)
            {
                Check(!File.Exists(output) && !File.Exists(output + ".source.bak"), "Cancellation after backup staging must leave no output or backup.");
                Check(Directory.GetFiles(Path.GetDirectoryName(output), "*.forge-tmp-*").Length == 0,
                    "Cancellation after staging must remove temporary files.");
            }
        }
        finally { Directory.Delete(root, true); }
    }

    static string WriteRequest(string root, string operation, string input, string output)
    {
        var requestPath = Path.Combine(root, "patch.json");
        File.WriteAllText(requestPath, JsonSerializer.Serialize(new
        {
            Operation = operation,
            Input = input,
            Output = output,
            Version = "1.2.3.4"
        }));
        return requestPath;
    }

    static string CopyTestAssembly(string root)
    {
        var path = Path.Combine(root, "source.dll");
        File.Copy(typeof(DesktopAssemblyServiceTests).Assembly.Location, path);
        return path;
    }

    static string NewRoot(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), prefix + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    static string Hash(string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(sha.ComputeHash(stream));
    }

    static void AuditEvaluatesRules()
    {
        var root = NewRoot("desktop-assembly-audit");
        try
        {
            var source = CopyTestAssembly(root);
            var service = new DesktopAssemblyService();
            var (report, findings) = service.Audit(source, CancellationToken.None);
            Check(!string.IsNullOrWhiteSpace(report), "Audit must generate a textual report.");
            Check(findings != null && findings.Count > 0, "Audit must extract rule findings.");
            Check(report.Contains("ADVANCED ASSEMBLY & SAVE SYSTEM AUDITOR"), "Report header must be present.");
            Check(findings.Any(f => f.RuleId == "DISTRIBUTION_METADATA"), "Distribution metadata audit must evaluate attributes.");
        }
        finally { Directory.Delete(root, true); }
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
