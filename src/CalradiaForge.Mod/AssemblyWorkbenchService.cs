using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using AsmResolver.DotNet;
using CalradiaForge.Core;

namespace CalradiaForge.Mod
{
    /// <summary>Bounded PE metadata reader and opt-in copy-only assembly version transformer.</summary>
    internal sealed class AssemblyWorkbenchService
    {
        const long MaximumBytes = 256L * 1024L * 1024L;
        const int MaximumFiles = 250;
        readonly string outputDirectory;
        readonly Action<string> beforeOutputCommit;

        public AssemblyWorkbenchService(string outputDirectory = null, Action<string> beforeOutputCommit = null)
        { this.outputDirectory = string.IsNullOrWhiteSpace(outputDirectory) ? Path.Combine(Paths.Data, "AssemblyWorkbench") : Path.GetFullPath(outputDirectory); this.beforeOutputCommit = beforeOutputCommit; }

        public IReadOnlyList<string> ListInstalledAssemblies(string modulesRoot, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(modulesRoot) || !Directory.Exists(modulesRoot))
                throw new DirectoryNotFoundException("The Bannerlord Modules directory is unavailable.");
            var files = new List<string>();
            foreach (var module in Directory.GetDirectories(modulesRoot).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                cancellation.ThrowIfCancellationRequested();
                var bin = Path.Combine(module, "bin", "Win64_Shipping_Client");
                if (!Directory.Exists(bin)) continue;
                foreach (var path in Directory.GetFiles(bin, "*.dll", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (Path.GetFileName(path).StartsWith("TaleWorlds", StringComparison.OrdinalIgnoreCase)) continue;
                    files.Add(path);
                    if (files.Count >= MaximumFiles) return files.AsReadOnly();
                }
            }
            return files.AsReadOnly();
        }

        public string Inspect(string path, CancellationToken cancellation)
        {
            var input = ValidateInput(path);
            cancellation.ThrowIfCancellationRequested();
            var inputHash = HashFile(input, cancellation);
            var assembly = AssemblyDefinition.FromFile(input);
            cancellation.ThrowIfCancellationRequested();
            if (!string.Equals(inputHash, HashFile(input, cancellation), StringComparison.Ordinal))
                throw new IOException("The selected assembly changed during inspection; inspect a stable file copy.");
            var module = assembly.ManifestModule ?? throw new InvalidDataException("The managed file has no manifest module.");
            var typeCount = 0;
            var methodCount = 0;
            foreach (var type in module.GetAllTypes().Take(10001))
            {
                cancellation.ThrowIfCancellationRequested();
                typeCount++;
                methodCount += type.Methods.Count;
            }
            var references = module.AssemblyReferences.Take(512).Select(reference =>
                "  " + (reference.Name?.ToString() ?? "<unnamed>") + ", Version=" + (reference.Version?.ToString() ?? "0.0.0.0") + ", Culture=" + (reference.Culture?.ToString() ?? "neutral")).ToArray();
            var info = new FileInfo(input);
            return string.Join(Environment.NewLine, new string[]
            {
                "ASSEMBLY WORKBENCH / STATIC METADATA ONLY",
                "Input: " + input,
                "SHA-256: " + inputHash,
                "Size: " + info.Length.ToString("N0") + " bytes",
                "Assembly: " + (assembly.Name?.ToString() ?? "<unnamed>"),
                "Version: " + (assembly.Version?.ToString() ?? "0.0.0.0"),
                "Module: " + (module.Name?.ToString() ?? "<unnamed>"),
                "Runtime: " + (module.RuntimeVersion?.ToString() ?? "unknown"),
                "IL-only: " + module.IsILOnly.ToString(),
                "Strong-name signed: " + module.IsStrongNameSigned.ToString(),
                "Types examined: " + Math.Min(typeCount, 10000) + (typeCount > 10000 ? " (bounded at 10,000)" : string.Empty),
                "Methods counted: " + methodCount.ToString(),
                "Assembly references (max 512): " + references.Length.ToString(),
                references.Length == 0 ? "  <none>" : string.Join(Environment.NewLine, (IEnumerable<string>)references),
                "Safety: parsed as data on a worker thread; the file was not loaded or executed."
            });
        }

        public string PreviewVersionCopy(string request, bool apply, CancellationToken cancellation)
        {
            var parts = (request ?? string.Empty).Split(new[] { '|' }, 2);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                throw new InvalidDataException("Enter the assembly path, then |, then a four-part version such as 1.2.3.0.");
            var input = ValidateInput(parts[0]);
            if (!Version.TryParse(parts[1].Trim(), out var requested) || requested.Build < 0 || requested.Revision < 0)
                throw new InvalidDataException("Version must have four numeric components, for example 1.2.3.0.");
            cancellation.ThrowIfCancellationRequested();
            var inputHash = HashFile(input, cancellation);
            var source = AssemblyDefinition.FromFile(input);
            cancellation.ThrowIfCancellationRequested();
            if (!string.Equals(inputHash, HashFile(input, cancellation), StringComparison.Ordinal))
                throw new IOException("The source changed while its metadata was being read; no output copy was created.");
            var module = source.ManifestModule ?? throw new InvalidDataException("The managed file has no manifest module.");
            if (module.IsStrongNameSigned) throw new InvalidDataException("Signed assemblies are rejected; this transformation cannot retain a valid strong-name signature.");
            if (!module.IsILOnly) throw new InvalidDataException("Only IL-only managed assemblies can be transformed.");
            var original = source.Version;
            var safeName = Path.GetFileNameWithoutExtension(input);
            var output = Path.Combine(outputDirectory, safeName + ".version-" + requested + ".dll");
            var backup = output + ".source.bak";
            if (File.Exists(output) || File.Exists(backup)) throw new IOException("The output or backup already exists; remove or rename it before applying another copy.");
            if (!apply)
                return string.Join(Environment.NewLine,
                    "VERSION PATCH PREVIEW / NO FILES WRITTEN",
                    "Input: " + input,
                    "Output copy: " + output,
                    "Source backup on apply: " + backup,
                    "Original version: " + original,
                    "Requested version: " + requested,
                    "Input SHA-256: " + inputHash,
                    "Validation: unsigned IL-only managed PE; no output or backup has been created.",
                    "Use Apply version copy only after reviewing this preview.");

            cancellation.ThrowIfCancellationRequested();
            Directory.CreateDirectory(outputDirectory);
            var temp = output + ".forge-tmp-" + Guid.NewGuid().ToString("N");
            var backupTemp = backup + ".forge-tmp-" + Guid.NewGuid().ToString("N");
            var backupCreated = false;
            var backupVerified = false;
            var outputCreated = false;
            string outputHash = null;
            try
            {
                cancellation.ThrowIfCancellationRequested();
                source.Version = requested;
                source.Write(temp);
                cancellation.ThrowIfCancellationRequested();
                var verified = AssemblyDefinition.FromFile(temp);
                if (verified.ManifestModule == null || verified.Version != requested || verified.ManifestModule.IsStrongNameSigned || !verified.ManifestModule.IsILOnly)
                    throw new InvalidDataException("The generated PE failed version or format verification.");
                outputHash = HashFile(temp, cancellation);
                cancellation.ThrowIfCancellationRequested();
                var copiedSourceHash = CopyAndHash(input, backupTemp, cancellation);
                if (!string.Equals(inputHash, copiedSourceHash, StringComparison.Ordinal))
                    throw new IOException("The source changed before the backup completed; no backup or output copy was committed.");
                cancellation.ThrowIfCancellationRequested();
                File.Move(backupTemp, backup);
                backupCreated = true;
                backupVerified = true;
                cancellation.ThrowIfCancellationRequested();
                beforeOutputCommit?.Invoke(output);
                cancellation.ThrowIfCancellationRequested();
                File.Move(temp, output);
                outputCreated = true;
                return string.Join(Environment.NewLine,
                    "VERSION PATCH COMPLETE / SEPARATE COPY",
                    "Input: " + input,
                    "Output: " + output,
                    "Source backup: " + backup,
                    "Original version: " + original,
                    "Output version: " + requested,
                    "Input SHA-256: " + inputHash,
                    "Output SHA-256: " + outputHash,
                    "Verification: output reopened and managed metadata verified.",
                    "The original input was not modified.");
            }
            catch
            {
                TryDelete(temp);
                TryDelete(backupTemp);
                if (outputCreated) TryDeleteIfHashMatches(output, outputHash);
                if (backupCreated && backupVerified) TryDeleteIfHashMatches(backup, inputHash);
                throw;
            }
        }

        static string ValidateInput(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidDataException("Select an installed managed .dll or .exe file.");
            var fullPath = Path.GetFullPath(path.Trim().Trim('"'));
            if (!File.Exists(fullPath)) throw new FileNotFoundException("Assembly input was not found.", fullPath);
            if (!string.Equals(Path.GetExtension(fullPath), ".dll", StringComparison.OrdinalIgnoreCase) && !string.Equals(Path.GetExtension(fullPath), ".exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Only PE .dll and .exe inputs are supported.");
            if (new FileInfo(fullPath).Length > MaximumBytes) throw new InvalidDataException("Assembly exceeds the 256 MiB limit.");
            return fullPath;
        }

        static string HashFile(string path, CancellationToken cancellation)
        {
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.SequentialScan))
            {
                var buffer = new byte[128 * 1024];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancellation.ThrowIfCancellationRequested();
                    hash.AppendData(buffer, 0, read);
                }
                cancellation.ThrowIfCancellationRequested();
                return BitConverter.ToString(hash.GetHashAndReset()).Replace("-", string.Empty);
            }
        }

        static string CopyAndHash(string source, string destination, CancellationToken cancellation)
        {
            var destinationCreated = false;
            try
            {
                using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
                using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.SequentialScan))
                {
                    using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, FileOptions.SequentialScan))
                    {
                        destinationCreated = true;
                        var buffer = new byte[128 * 1024];
                        int read;
                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            cancellation.ThrowIfCancellationRequested();
                            output.Write(buffer, 0, read);
                            hash.AppendData(buffer, 0, read);
                        }
                        cancellation.ThrowIfCancellationRequested();
                        output.Flush(true);
                        return BitConverter.ToString(hash.GetHashAndReset()).Replace("-", string.Empty);
                    }
                }
            }
            catch
            {
                if (destinationCreated) TryDelete(destination);
                throw;
            }
        }

        static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception) { /* Best-effort temporary file cleanup */ }
        }

        static void TryDeleteIfHashMatches(string path, string expectedHash)
        {
            if (string.IsNullOrEmpty(expectedHash)) return;
            try
            {
                if (File.Exists(path) && string.Equals(HashFile(path, CancellationToken.None), expectedHash, StringComparison.Ordinal))
                    File.Delete(path);
            }
            catch (Exception) { /* Best-effort hash cleanup on locked files */ }
        }
    }
}
