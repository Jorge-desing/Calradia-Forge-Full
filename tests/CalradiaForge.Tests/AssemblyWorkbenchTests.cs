using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using AsmResolver.DotNet;
using CalradiaForge.Mod;

namespace CalradiaForge.Tests
{
    public static class AssemblyWorkbenchTests
    {
        public static void Run(Action<string, Action> test, string tempRoot)
        {
            test("Assembly workbench inspects metadata without loading the selected copy", () => InspectWithoutLoading(tempRoot));
            test("Assembly workbench rejects a truncated managed image", () => RejectTruncated(tempRoot));
            test("Assembly workbench preview does not write output or backup", () => PreviewIsReadOnly(tempRoot));
            test("Assembly workbench version edit preserves original and verifies separate copy", () => ApplyVersionCopy(tempRoot));
            test("Assembly workbench preserves a competing output created before commit", () => PreserveCompetingOutput(tempRoot));
            test("Assembly workbench cancellation before patch writes nothing", () => CancellationBeforePatch(tempRoot));
            test("Assembly workbench cancellation after backup removes staged files", () => CancellationBeforeCommit(tempRoot));
            test("Assembly workbench honors cancellation before inspection", CancellationIsHonored);
        }

        static void InspectWithoutLoading(string tempRoot)
        {
            var folder = CreateCase(tempRoot, "assembly-inspect");
            var input = Path.Combine(folder, "unloaded-copy.exe");
            File.Copy(typeof(AssemblyWorkbenchTests).Assembly.Location, input);
            if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly => !assembly.IsDynamic && SamePath(assembly.Location, input)))
                throw new Exception("Test precondition failed: copied inspection input is already loaded.");
            var result = new AssemblyWorkbenchService(folder).Inspect(input, CancellationToken.None);
            if (!result.Contains("STATIC METADATA ONLY") || !result.Contains("not loaded or executed"))
                throw new Exception("Inspection did not report its static-only behavior.");
            if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly => !assembly.IsDynamic && SamePath(assembly.Location, input)))
                throw new Exception("The inspected PE was loaded into the test process.");
        }

        static void RejectTruncated(string tempRoot)
        {
            var folder = CreateCase(tempRoot, "assembly-invalid");
            var input = Path.Combine(folder, "truncated.dll");
            File.WriteAllBytes(input, new byte[] { 0x4D, 0x5A, 0x00 });
            try { new AssemblyWorkbenchService(folder).Inspect(input, CancellationToken.None); }
            catch { return; }
            throw new Exception("Truncated PE was accepted.");
        }

        static void PreviewIsReadOnly(string tempRoot)
        {
            var folder = CreateCase(tempRoot, "assembly-preview");
            var input = Path.Combine(folder, "source.exe");
            var output = Path.Combine(folder, "workbench-output");
            File.Copy(typeof(AssemblyWorkbenchTests).Assembly.Location, input);
            var sourceHash = Hash(input);
            var result = new AssemblyWorkbenchService(output).PreviewVersionCopy(input + "|1.2.3.4", false, CancellationToken.None);
            if (!result.Contains("NO FILES WRITTEN") || Directory.Exists(output)) throw new Exception("Preview wrote output files.");
            if (Hash(input) != sourceHash) throw new Exception("Preview changed the original input.");
        }

        static void ApplyVersionCopy(string tempRoot)
        {
            var folder = CreateCase(tempRoot, "assembly-apply");
            var input = Path.Combine(folder, "source.exe");
            var outputDirectory = Path.Combine(folder, "workbench-output");
            File.Copy(typeof(AssemblyWorkbenchTests).Assembly.Location, input);
            var sourceHash = Hash(input);
            var result = new AssemblyWorkbenchService(outputDirectory).PreviewVersionCopy(input + "|1.2.3.4", true, CancellationToken.None);
            var output = Path.Combine(outputDirectory, "source.version-1.2.3.4.dll");
            var backup = output + ".source.bak";
            if (!File.Exists(output) || !File.Exists(backup)) throw new Exception("The output copy or source backup is missing.");
            if (!result.Contains("VERSION PATCH COMPLETE") || Hash(input) != sourceHash || Hash(backup) != sourceHash)
                throw new Exception("Original or backup integrity check failed. Input hash=" + Hash(input) + ", expected=" + sourceHash + ", backup hash=" + Hash(backup) + ".");
            if (AssemblyDefinition.FromFile(output).Version.ToString() != "1.2.3.4")
                throw new Exception("The copy did not retain the requested assembly version.");
        }

        static void CancellationIsHonored()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                try { new AssemblyWorkbenchService().Inspect(typeof(AssemblyWorkbenchTests).Assembly.Location, cancellation.Token); }
                catch (OperationCanceledException) { return; }
                throw new Exception("Cancelled inspection was not rejected.");
            }
        }

        static void PreserveCompetingOutput(string tempRoot)
        {
            var folder = CreateCase(tempRoot, "assembly-output-race");
            var input = Path.Combine(folder, "source.exe");
            var outputDirectory = Path.Combine(folder, "workbench-output");
            File.Copy(typeof(AssemblyWorkbenchTests).Assembly.Location, input);
            var expectedSourceHash = Hash(input);
            string racedOutput = null;
            var service = new AssemblyWorkbenchService(outputDirectory, path =>
            {
                racedOutput = path;
                File.WriteAllText(path, "created by another process");
            });
            try
            {
                service.PreviewVersionCopy(input + "|1.2.3.4", true, CancellationToken.None);
                throw new Exception("A destination created during commit was overwritten.");
            }
            catch (IOException) { }

            if (string.IsNullOrEmpty(racedOutput) || !File.Exists(racedOutput) || File.ReadAllText(racedOutput) != "created by another process")
                throw new Exception("The competing output was deleted or changed after the move failed.");
            if (Hash(input) != expectedSourceHash) throw new Exception("A failed output commit changed the original source.");
            if (File.Exists(racedOutput + ".source.bak") || Directory.GetFiles(outputDirectory, "*.forge-tmp-*").Length != 0)
                throw new Exception("The failed transaction left a verified backup or temporary PE behind.");
        }

        static void CancellationBeforePatch(string tempRoot)
        {
            var folder = CreateCase(tempRoot, "assembly-cancel-patch");
            var input = Path.Combine(folder, "source.exe");
            var output = Path.Combine(folder, "workbench-output", "source.version-1.2.3.4.dll");
            File.Copy(typeof(AssemblyWorkbenchTests).Assembly.Location, input);
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                try { new AssemblyWorkbenchService(Path.GetDirectoryName(output)).PreviewVersionCopy(input + "|1.2.3.4", true, cancellation.Token); }
                catch (OperationCanceledException)
                {
                    if (File.Exists(output) || File.Exists(output + ".source.bak") || Directory.Exists(Path.GetDirectoryName(output)))
                        throw new Exception("Cancelled patch created output, backup, or output directory.");
                    return;
                }
                throw new Exception("Cancelled patch was not rejected.");
            }
        }

        static void CancellationBeforeCommit(string tempRoot)
        {
            var folder = CreateCase(tempRoot, "assembly-cancel-commit");
            var input = Path.Combine(folder, "source.exe");
            var outputDirectory = Path.Combine(folder, "workbench-output");
            var output = Path.Combine(outputDirectory, "source.version-1.2.3.4.dll");
            File.Copy(typeof(AssemblyWorkbenchTests).Assembly.Location, input);
            using (var cancellation = new CancellationTokenSource())
            {
                var service = new AssemblyWorkbenchService(outputDirectory, _ => cancellation.Cancel());
                try
                {
                    service.PreviewVersionCopy(input + "|1.2.3.4", true, cancellation.Token);
                    throw new Exception("Cancellation before commit was ignored.");
                }
                catch (OperationCanceledException)
                {
                    if (File.Exists(output) || File.Exists(output + ".source.bak") || Directory.GetFiles(outputDirectory, "*.forge-tmp-*").Length != 0)
                        throw new Exception("Cancellation after staging left output, backup, or temporary files.");
                }
            }
        }

        static string CreateCase(string root, string name)
        { var path = Path.Combine(root, name); Directory.CreateDirectory(path); return path; }
        static string Hash(string path)
        { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty); }
        static bool SamePath(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
            try { return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); }
            catch (ArgumentException) { return false; }
            catch (NotSupportedException) { return false; }
        }
    }
}
