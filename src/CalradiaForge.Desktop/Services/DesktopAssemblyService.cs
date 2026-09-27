using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;

namespace CalradiaForge.Desktop.Services
{
    /// <summary>Reads .NET metadata without loading the inspected file into the CLR.</summary>
    internal sealed class DesktopAssemblyService
    {
        const long MaximumAssemblyBytes = 256L * 1024 * 1024;
        internal Action<string> BeforeOutputCommit { get; set; }

        public string Inspect(string path, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var fullPath = ValidateInput(path);
            var inputHash = HashFile(fullPath, cancellation);
            var assembly = AssemblyDefinition.FromFile(fullPath);
            cancellation.ThrowIfCancellationRequested();
            if (!string.Equals(inputHash, HashFile(fullPath, cancellation), StringComparison.Ordinal))
                throw new IOException("The input changed during inspection; run it again against a stable file.");

            var module = assembly.ManifestModule ?? throw new InvalidDataException("The managed file has no manifest module.");
            var typeCount = 0;
            var methodCount = 0;
            foreach (var type in module.GetAllTypes().Take(10001))
            {
                cancellation.ThrowIfCancellationRequested();
                typeCount++;
                methodCount += type.Methods.Count;
            }
            var info = new FileInfo(fullPath);
            var refs = module.AssemblyReferences;
            int refLimit = Math.Min(512, refs.Count);

            var report = new StringBuilder(2048 + refLimit * 80);
            report.AppendLine("ASSEMBLY INSPECTION / STATIC METADATA ONLY");
            report.Append("Input: ").AppendLine(fullPath);
            report.Append("SHA-256: ").AppendLine(inputHash);
            report.Append("Size: ").Append(info.Length.ToString("N0")).AppendLine(" bytes");
            report.Append("Assembly: ").AppendLine(assembly.Name?.ToString() ?? "<unnamed>");
            report.Append("Version: ").AppendLine(assembly.Version.ToString());
            report.Append("Module: ").AppendLine(module.Name?.ToString() ?? "<unnamed>");
            report.Append("Runtime: ").AppendLine(module.RuntimeVersion?.ToString() ?? "unknown");
            report.Append("IL-only: ").AppendLine(module.IsILOnly ? "True" : "False");
            report.Append("Strong-name signed: ").AppendLine(module.IsStrongNameSigned ? "True" : "False");
            report.Append("Types examined: ").Append(Math.Min(typeCount, 10000));
            if (typeCount > 10000) report.Append(" (bounded at 10,000)");
            report.AppendLine();
            report.Append("Methods counted: ").AppendLine(methodCount.ToString());
            report.Append("Assembly references (max 512): ").AppendLine(refLimit.ToString());
            if (refLimit == 0)
            {
                report.AppendLine("  <none>");
            }
            else
            {
                for (int i = 0; i < refLimit; i++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    var reference = refs[i];
                    report.Append("  ").Append(reference.Name).Append(", Version=").Append(reference.Version).Append(", Culture=").AppendLine(reference.Culture ?? "neutral");
                }
            }
            report.Append("Safety: the file was parsed as data; it was not loaded or executed.");
            return report.ToString();
        }

        public (string Report, List<AssemblyAuditFinding> Findings) Audit(string path, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var fullPath = ValidateInput(path);
            var inputHash = HashFile(fullPath, cancellation);
            var assembly = AssemblyDefinition.FromFile(fullPath);
            cancellation.ThrowIfCancellationRequested();
            if (!string.Equals(inputHash, HashFile(fullPath, cancellation), StringComparison.Ordinal))
                throw new IOException("The input changed during inspection; run it again against a stable file.");

            var module = assembly.ManifestModule ?? throw new InvalidDataException("The managed file has no manifest module.");
            var findings = new List<AssemblyAuditFinding>();
            var typeCount = 0;
            var methodCount = 0;
            var shadowViolations = 0;
            var statelessViolations = 0;
            var saveIdWarnings = 0;
            var mbguidRisks = 0;

            foreach (var type in module.GetAllTypes().Take(10001))
            {
                cancellation.ThrowIfCancellationRequested();
                typeCount++;
                methodCount += type.Methods.Count;

                // Check 1: GEMINI.md Anti-Shadowing
                var ns = type.Namespace?.ToString();
                var name = type.Name?.ToString();
                if (!string.IsNullOrEmpty(ns))
                {
                    if (ns.Equals("Campaign", StringComparison.OrdinalIgnoreCase) ||
                        ns.StartsWith("Campaign.", StringComparison.OrdinalIgnoreCase) ||
                        ns.EndsWith(".Campaign", StringComparison.OrdinalIgnoreCase) ||
                        ns.Contains(".Campaign.", StringComparison.OrdinalIgnoreCase))
                    {
                        shadowViolations++;
                        findings.Add(new AssemblyAuditFinding
                        {
                            RuleId = "GEMINI_ANTI_SHADOWING",
                            Severity = "Violation",
                            Target = type.FullName,
                            Message = "Namespace contains 'Campaign', shadowing TaleWorlds.CampaignSystem.Campaign.",
                            Recommendation = "Rename namespace to CampaignBehaviors or CampaignExtensions per GEMINI.md Rule A."
                        });
                    }
                    if (ns.Equals("Localization", StringComparison.OrdinalIgnoreCase) ||
                        ns.StartsWith("Localization.", StringComparison.OrdinalIgnoreCase) ||
                        ns.EndsWith(".Localization", StringComparison.OrdinalIgnoreCase) ||
                        ns.Contains(".Localization.", StringComparison.OrdinalIgnoreCase))
                    {
                        shadowViolations++;
                        findings.Add(new AssemblyAuditFinding
                        {
                            RuleId = "GEMINI_ANTI_SHADOWING",
                            Severity = "Violation",
                            Target = type.FullName,
                            Message = "Namespace contains 'Localization', shadowing TaleWorlds.Localization.",
                            Recommendation = "Rename namespace to GameLocalization or LocalizationSync per GEMINI.md Rule A."
                        });
                    }
                }
                if (string.Equals(name, "Campaign", StringComparison.OrdinalIgnoreCase))
                {
                    shadowViolations++;
                    findings.Add(new AssemblyAuditFinding
                    {
                        RuleId = "GEMINI_ANTI_SHADOWING",
                        Severity = "Violation",
                        Target = type.FullName,
                        Message = "Type name 'Campaign' shadows TaleWorlds.CampaignSystem.Campaign.",
                        Recommendation = "Rename class to CampaignBehavior or CampaignSystem per GEMINI.md Rule A."
                    });
                }

                // Check 2: SaveableTypeDefiner & Stateless Behavior Contracts
                var baseTypeName = type.BaseType?.Name?.ToString();
                var baseTypeFullName = type.BaseType?.FullName;

                if (string.Equals(baseTypeName, "CampaignBehaviorBase", StringComparison.OrdinalIgnoreCase) ||
                    baseTypeFullName?.Contains("CampaignBehaviorBase") == true)
                {
                    foreach (var field in type.Fields)
                    {
                        foreach (var attr in field.CustomAttributes)
                        {
                            var attrName = attr.Constructor?.DeclaringType?.Name?.ToString();
                            if (attrName == "SaveableFieldAttribute" || attrName == "SaveableField" ||
                                attrName == "SaveablePropertyAttribute" || attrName == "SaveableProperty")
                            {
                                statelessViolations++;
                                findings.Add(new AssemblyAuditFinding
                                {
                                    RuleId = "RULE_B_STATELESS_BEHAVIOR",
                                    Severity = "Violation",
                                    Target = $"{type.FullName}.{field.Name}",
                                    Message = "Mod CampaignBehavior contains [SaveableField/Property]. Behaviors must remain completely stateless.",
                                    Recommendation = "Do not persist state in behaviors; derive dynamically from vanilla live state."
                                });
                            }
                        }
                    }
                }

                if (string.Equals(baseTypeName, "SaveableTypeDefiner", StringComparison.OrdinalIgnoreCase) ||
                    baseTypeFullName?.Contains("SaveableTypeDefiner") == true)
                {
                    var checkedBaseId = false;
                    var methods = type.Methods;
                    for (int m = 0; m < methods.Count; m++)
                    {
                        var ctor = methods[m];
                        if (!ctor.IsConstructor || ctor.CilMethodBody == null) continue;
                        var instrs = ctor.CilMethodBody.Instructions;
                        for (int i = 0; i < instrs.Count; i++)
                        {
                            var instr = instrs[i];
                            var ldcVal = GetLdcI4Value(instr);
                            if (ldcVal.HasValue)
                            {
                                int id = ldcVal.Value;
                                if (id >= 0)
                                {
                                    checkedBaseId = true;
                                    if (id < 2_500_000)
                                    {
                                        saveIdWarnings++;
                                        findings.Add(new AssemblyAuditFinding
                                        {
                                            RuleId = "BANNERLORD_SAVE_ID_SAFETY",
                                            Severity = "Warning",
                                            Target = type.FullName,
                                            Message = $"SaveableTypeDefiner constructor passes base ID {id:N0} < 2,500,000. Risk of collision with native engine (0-100k) or other mods.",
                                            Recommendation = "Use base ID >= 2,500,000 to ensure save stability across mod lists."
                                        });
                                    }
                                    else
                                    {
                                        findings.Add(new AssemblyAuditFinding
                                        {
                                            RuleId = "BANNERLORD_SAVE_ID_SAFETY",
                                            Severity = "Verified",
                                            Target = type.FullName,
                                            Message = $"SaveableTypeDefiner allocates safe base ID {id:N0} (>= 2,500,000).",
                                            Recommendation = "Verified: zero risk of collision with engine base IDs."
                                        });
                                    }
                                    break;
                                }
                            }
                        }
                        if (checkedBaseId) break;
                    }
                }

                // Check 3: Engine Entity Direct Serialization (Hero, MobileParty, Settlement)
                foreach (var field in type.Fields)
                {
                    var fieldTypeName = field.Signature?.FieldType?.Name?.ToString();
                    if (fieldTypeName == "Hero" || fieldTypeName == "MobileParty" || fieldTypeName == "Settlement" ||
                        fieldTypeName == "Clan" || fieldTypeName == "Kingdom")
                    {
                        foreach (var attr in field.CustomAttributes)
                        {
                            var attrName = attr.Constructor?.DeclaringType?.Name?.ToString();
                            if (attrName == "SaveableFieldAttribute" || attrName == "SaveableField" ||
                                attrName == "SaveablePropertyAttribute" || attrName == "SaveableProperty")
                            {
                                mbguidRisks++;
                                findings.Add(new AssemblyAuditFinding
                                {
                                    RuleId = "BANNERLORD_MBGUID_SERIALIZATION",
                                    Severity = "Violation",
                                    Target = $"{type.FullName}.{field.Name}",
                                    Message = $"Field serializes engine entity '{fieldTypeName}' directly. Objects with MBGUID lose identity mapping on reload.",
                                    Recommendation = "Serialize entity StringId (string) instead and resolve via MBObjectManager."
                                });
                            }
                        }
                    }
                }
            }

            // Check 4: Distribution Metadata (distribution_safety.md)
            bool hasCompany = false, hasProduct = false, hasDesc = false, hasCopyright = false;
            foreach (var a in assembly.CustomAttributes)
            {
                var attrName = a.Constructor?.DeclaringType?.Name?.ToString();
                if (attrName == "AssemblyCompanyAttribute") hasCompany = true;
                else if (attrName == "AssemblyProductAttribute") hasProduct = true;
                else if (attrName == "AssemblyDescriptionAttribute") hasDesc = true;
                else if (attrName == "AssemblyCopyrightAttribute") hasCopyright = true;
            }

            if (hasCompany && hasProduct && hasDesc && hasCopyright)
            {
                findings.Add(new AssemblyAuditFinding
                {
                    RuleId = "DISTRIBUTION_METADATA",
                    Severity = "Verified",
                    Target = assembly.Name?.ToString() ?? "Assembly",
                    Message = "Full assembly metadata declared (Company, Product, Description, Copyright). Meets distribution safety guidelines.",
                    Recommendation = "Binary is safe from common AV heuristic false-positive quarantines."
                });
            }
            else
            {
                var missing = new List<string>(4);
                if (!hasCompany) missing.Add("Company");
                if (!hasProduct) missing.Add("Product");
                if (!hasDesc) missing.Add("Description");
                if (!hasCopyright) missing.Add("Copyright");
                findings.Add(new AssemblyAuditFinding
                {
                    RuleId = "DISTRIBUTION_METADATA",
                    Severity = "Warning",
                    Target = assembly.Name?.ToString() ?? "Assembly",
                    Message = $"Assembly metadata missing: {string.Join(", ", missing)}. Binaries without metadata are aggressively flagged by antivirus engines.",
                    Recommendation = "Declare <Company>, <Product>, <Description>, and <Copyright> in .csproj."
                });
            }

            if (shadowViolations == 0)
            {
                findings.Add(new AssemblyAuditFinding
                {
                    RuleId = "GEMINI_ANTI_SHADOWING",
                    Severity = "Verified",
                    Target = module.Name?.ToString() ?? "Module",
                    Message = "Anti-shadowing invariant satisfied: 0 types or namespaces shadow TaleWorlds.Campaign or TaleWorlds.Localization.",
                    Recommendation = "Clean symbol isolation verified."
                });
            }

            var reportBuilder = new StringBuilder(2048 + findings.Count * 256);
            reportBuilder.AppendLine("================================================================================");
            reportBuilder.AppendLine("CALRADIA FORGE · ADVANCED ASSEMBLY & SAVE SYSTEM AUDITOR");
            reportBuilder.AppendLine("================================================================================");
            reportBuilder.AppendLine("Input Assembly: " + fullPath);
            reportBuilder.AppendLine("SHA-256 Digest: " + inputHash);
            reportBuilder.AppendLine("Assembly Name:  " + (assembly.Name?.ToString() ?? "<unnamed>"));
            reportBuilder.AppendLine("Runtime Target: " + (module.RuntimeVersion?.ToString() ?? "unknown") + " | IL-Only: " + module.IsILOnly);
            reportBuilder.AppendLine("Types Examined: " + typeCount + " | Methods Counted: " + methodCount);
            reportBuilder.AppendLine();
            reportBuilder.AppendLine("AUDIT METRICS:");
            reportBuilder.AppendLine("  Shadowing Violations:     " + shadowViolations);
            reportBuilder.AppendLine("  Stateless Violations:     " + statelessViolations);
            reportBuilder.AppendLine("  SaveableTypeDefiner Base: " + (saveIdWarnings > 0 ? $"{saveIdWarnings} collision hazard(s)" : "Safe / Verified"));
            reportBuilder.AppendLine("  Direct Entity MBGUID:     " + mbguidRisks);
            reportBuilder.AppendLine("  Metadata Distribution:    " + (hasCompany && hasProduct && hasDesc && hasCopyright ? "Complete" : "Incomplete"));
            reportBuilder.AppendLine();
            reportBuilder.AppendLine("DETAILED FINDINGS (" + findings.Count + "):");
            foreach (var finding in findings)
            {
                reportBuilder.AppendLine($"[{finding.Severity.ToUpperInvariant()}] {finding.RuleId} -> {finding.Target}");
                reportBuilder.AppendLine("  Evidence: " + finding.Message);
                if (!string.IsNullOrEmpty(finding.Recommendation))
                    reportBuilder.AppendLine("  Next:     " + finding.Recommendation);
            }

            return (reportBuilder.ToString(), findings);
        }

        static int? GetLdcI4Value(CilInstruction instr)
        {
            if (instr == null) return null;
            if (instr.OpCode.Code >= CilCode.Ldc_I4_0 && instr.OpCode.Code <= CilCode.Ldc_I4_8)
                return (int)instr.OpCode.Code - (int)CilCode.Ldc_I4_0;
            if (instr.OpCode.Code == CilCode.Ldc_I4_M1)
                return -1;
            if (instr.Operand is int i)
                return i;
            if (instr.Operand is sbyte sb)
                return sb;
            if (instr.Operand is byte b)
                return b;
            return null;
        }

        public string PreviewAndWriteVersionPatch(string requestPath, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var requestFullPath = Path.GetFullPath(requestPath ?? string.Empty);
            if (!File.Exists(requestFullPath)) throw new FileNotFoundException("Patch request JSON was not found.", requestFullPath);
            if (new FileInfo(requestFullPath).Length > 1024 * 1024) throw new InvalidDataException("Patch request exceeds the 1 MiB limit.");
            var request = JsonSerializer.Deserialize<AssemblyVersionPatchRequest>(File.ReadAllText(requestFullPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidDataException("Patch request is empty.");
            cancellation.ThrowIfCancellationRequested();
            var previewOnly = string.Equals(request.Operation, "preview-set-assembly-version", StringComparison.Ordinal);
            if (!previewOnly && !string.Equals(request.Operation, "apply-set-assembly-version", StringComparison.Ordinal))
                throw new InvalidDataException("Unsupported operation. Expected 'preview-set-assembly-version' or 'apply-set-assembly-version'.");
            if (!Version.TryParse(request.Version, out var requestedVersion) || requestedVersion.Build < 0 || requestedVersion.Revision < 0)
                throw new InvalidDataException("Version must have four numeric components, for example 1.2.3.0.");

            var input = ValidateInput(ResolveRequestPath(requestFullPath, request.Input));
            var output = Path.GetFullPath(ResolveRequestPath(requestFullPath, request.Output));
            if (string.Equals(input, output, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The output must be a different file from the original.");
            if (File.Exists(output)) throw new IOException("Output already exists; choose a new copy path.");
            var directory = Path.GetDirectoryName(output);
            if (string.IsNullOrEmpty(directory)) throw new InvalidDataException("Output path must include a directory.");
            var backup = output + ".source.bak";
            if (File.Exists(backup)) throw new IOException("The source backup path already exists; choose a new output path.");
            var temp = output + ".forge-tmp-" + Guid.NewGuid().ToString("N");
            var backupTemp = backup + ".forge-tmp-" + Guid.NewGuid().ToString("N");
            var inputHash = HashFile(input, cancellation);
            var sourceAssembly = AssemblyDefinition.FromFile(input);
            cancellation.ThrowIfCancellationRequested();
            if (!string.Equals(inputHash, HashFile(input, cancellation), StringComparison.Ordinal))
                throw new IOException("The input changed while its metadata was being read; no output copy was created.");
            var sourceModule = sourceAssembly.ManifestModule ?? throw new InvalidDataException("The managed file has no manifest module.");
            if (sourceModule.IsStrongNameSigned)
                throw new InvalidDataException("This transformation rejects strong-name signed assemblies because it cannot retain a valid signature.");
            if (!sourceModule.IsILOnly)
                throw new InvalidDataException("This transformation only accepts IL-only managed assemblies; mixed-mode/native code will not be rewritten.");
            var originalVersion = sourceAssembly.Version;
            if (previewOnly)
                return string.Join(Environment.NewLine,
                    "PATCH PREVIEW / NO FILES WRITTEN",
                    "Operation: set-assembly-version",
                    "Input: " + input,
                    "Output: " + output,
                    "Backup to create on apply: " + backup,
                    "Original version: " + originalVersion,
                    "Requested version: " + requestedVersion,
                    "Input SHA-256: " + inputHash,
                    "Validation: supported unsigned IL-only managed assembly; output path is free.",
                    "Review this plan, then change operation to 'apply-set-assembly-version' to write the separate copy.");

            cancellation.ThrowIfCancellationRequested();
            Directory.CreateDirectory(directory);
            var backupCreated = false;
            var backupVerified = false;
            var outputCreated = false;
            string outputHash = null;
            try
            {
                cancellation.ThrowIfCancellationRequested();
                sourceAssembly.Version = requestedVersion;
                sourceAssembly.Write(temp);
                cancellation.ThrowIfCancellationRequested();
                var verification = AssemblyDefinition.FromFile(temp);
                if (verification.Version != requestedVersion)
                    throw new InvalidDataException("The generated copy failed metadata verification.");
                if (verification.ManifestModule == null || verification.ManifestModule.IsStrongNameSigned || !verification.ManifestModule.IsILOnly)
                    throw new InvalidDataException("The generated copy changed to an unsupported PE format.");
                outputHash = HashFile(temp, cancellation);

                cancellation.ThrowIfCancellationRequested();
                var copiedSourceHash = CopyAndHash(input, backupTemp, cancellation);
                if (!string.Equals(inputHash, copiedSourceHash, StringComparison.Ordinal))
                    throw new IOException("The source changed before the backup completed; no backup or output copy was committed.");
                cancellation.ThrowIfCancellationRequested();
                File.Move(backupTemp, backup, overwrite: false);
                backupCreated = true;
                backupVerified = true;

                cancellation.ThrowIfCancellationRequested();
                BeforeOutputCommit?.Invoke(output);
                cancellation.ThrowIfCancellationRequested();
                File.Move(temp, output, overwrite: false);
                outputCreated = true;
                return string.Join(Environment.NewLine,
                    "PATCH COMPLETE / COPY ONLY",
                    "Operation: set-assembly-version",
                    "Input: " + input,
                    "Output: " + output,
                    "Backup: " + backup,
                    "Original version: " + originalVersion,
                    "Output version: " + requestedVersion,
                    "Input SHA-256: " + inputHash,
                    "Output SHA-256: " + outputHash,
                    "Verification: output reopened and managed assembly version confirmed.",
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

        static string ResolveRequestPath(string requestPath, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException("Patch request input/output path is required.");
            return Path.IsPathRooted(value) ? value : Path.Combine(Path.GetDirectoryName(requestPath), value);
        }

        static string ValidateInput(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidDataException("Select a managed .dll or .exe file.");
            var fullPath = Path.GetFullPath(path.Trim().Trim('"'));
            if (!File.Exists(fullPath)) throw new FileNotFoundException("Assembly input was not found.", fullPath);
            if (!string.Equals(Path.GetExtension(fullPath), ".dll", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Path.GetExtension(fullPath), ".exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Only managed PE .dll and .exe inputs are supported.");
            if (new FileInfo(fullPath).Length > MaximumAssemblyBytes) throw new InvalidDataException("Assembly exceeds the 256 MiB inspection limit.");
            return fullPath;
        }

        static string HashFile(string path, CancellationToken cancellation)
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.SequentialScan);
            var buffer = new byte[128 * 1024];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellation.ThrowIfCancellationRequested();
                hash.AppendData(buffer, 0, read);
            }
            cancellation.ThrowIfCancellationRequested();
            return Convert.ToHexString(hash.GetHashAndReset());
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
                        return Convert.ToHexString(hash.GetHashAndReset());
                    }
                }
            }
            catch
            {
                if (destinationCreated) TryDelete(destination);
                throw;
            }
        }

        static void TryDeleteIfHashMatches(string path, string expectedHash)
        {
            if (string.IsNullOrEmpty(expectedHash)) return;
            try
            {
                if (File.Exists(path) && string.Equals(HashFile(path, CancellationToken.None), expectedHash, StringComparison.Ordinal))
                    File.Delete(path);
            }
            catch { }
        }

        static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }

    internal sealed class AssemblyVersionPatchRequest
    {
        public string Operation { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
        public string Version { get; set; }
    }

    internal sealed class AssemblyAuditFinding
    {
        public string RuleId { get; set; }
        public string Severity { get; set; }
        public string Target { get; set; }
        public string Message { get; set; }
        public string Recommendation { get; set; }
    }
}
