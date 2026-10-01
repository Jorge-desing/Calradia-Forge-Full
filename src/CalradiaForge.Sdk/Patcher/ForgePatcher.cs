using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CalradiaForge.Sdk.Patcher
{
    /// <summary>Legacy mutable receipt retained for source compatibility.</summary>
    public class PatchRecord
    {
        public string Id { get; set; }
        public string Owner { get; set; }
        public MethodInfo Original { get; set; }
        public MethodInfo Replacement { get; set; }
        public string SourceModule { get; set; }
        public byte[] OriginalBytes { get; set; }
        // Opaque installation identity; kept internal so the legacy public receipt shape stays stable.
        internal object GenerationToken { get; set; }

        /// <summary>Checks all installed jump bytes against Forge's tracked record.</summary>
        public bool IsIntact()
        {
            if (Original == null || !ForgeDetour.IsTrackedReceipt(Original, Id, GenerationToken, OriginalBytes)) return false;
            string status;
            return ForgeDetour.Verify(Original, out status);
        }
    }

    /// <summary>
    /// Explicit, experimental method replacement for compatibility callers. The complete
    /// declaration batch is inspected before the first write; it is not thread-safe for a
    /// target being executed concurrently and does not provide instruction relocation.
    /// </summary>
    public static class ForgePatcher
    {
        private static readonly object syncLock = new object();
        private static readonly List<PatchRecord> appliedPatches = new List<PatchRecord>();

        public static IReadOnlyList<PatchRecord> GetAppliedPatches()
        {
            lock (syncLock) return appliedPatches.Select(Copy).ToList().AsReadOnly();
        }

        /// <summary>Returns records whose complete installed jump bytes no longer match.</summary>
        public static List<PatchRecord> VerifyIntegrity()
        {
            lock (syncLock) return appliedPatches.Where(record => !record.IsIntact()).Select(Copy).ToList();
        }

        /// <summary>Registers a legacy receipt only when it describes a Forge-tracked detour.</summary>
        public static void Register(PatchRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (record.Original == null || record.Replacement == null || !ForgeDetour.IsTracked(record.Original))
                throw new ArgumentException("Only an active Forge-owned detour can be registered.", nameof(record));
            if (string.IsNullOrWhiteSpace(record.Id))
                record.Id = ForgeDetour.GetTrackedPatchId(record.Original) ?? MakeLegacyId(record.Original, record.Replacement);
            if (record.GenerationToken == null)
            {
                object generationToken;
                if (!ForgeDetour.TryGetTrackedReceipt(record.Original, record.Id, record.OriginalBytes, out generationToken))
                    throw new ArgumentException("The receipt does not match an active Forge detour generation.", nameof(record));
                record.GenerationToken = generationToken;
            }
            if (!ForgeDetour.IsTrackedReceipt(record.Original, record.Id, record.GenerationToken, record.OriginalBytes))
                throw new ArgumentException("The receipt does not match the active Forge detour generation.", nameof(record));
            lock (syncLock)
            {
                if (appliedPatches.Any(existing => existing.Original == record.Original))
                    throw new InvalidOperationException("A patch record already exists for this target method.");
                if (!ForgeDetour.IsTrackedReceipt(record.Original, record.Id, record.GenerationToken, record.OriginalBytes))
                    throw new ArgumentException("The receipt no longer matches the active Forge detour generation.", nameof(record));
                if (appliedPatches.Any(existing => string.Equals(existing.Id, record.Id, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Patch ID already exists: " + record.Id);
                appliedPatches.Add(Copy(record));
            }
        }

        /// <summary>Reverts a tracked record only when its installed bytes still match Forge.</summary>
        public static void Revert(PatchRecord record)
        {
            if (record == null || record.Original == null || record.OriginalBytes == null)
                throw new ArgumentException("Invalid patch record.", nameof(record));
            lock (syncLock)
            {
                PatchRecord registered = appliedPatches.FirstOrDefault(existing => existing.Original == record.Original &&
                    ReferenceEquals(existing.GenerationToken, record.GenerationToken) &&
                    string.Equals(existing.Id, record.Id, StringComparison.OrdinalIgnoreCase));
                if (record.GenerationToken == null || registered == null)
                    throw new InvalidOperationException("The patch receipt is stale or is not registered for this installation generation.");
                if (!ForgeDetour.UnpatchReceipt(record.Original, record.Id, record.GenerationToken, record.OriginalBytes))
                    throw new InvalidOperationException("The patch receipt is stale or target bytes changed; the active Forge detour was not reverted.");
                appliedPatches.RemoveAll(existing => ReferenceEquals(existing.GenerationToken, record.GenerationToken));
            }
        }

        /// <summary>Reverts only records tracked by this legacy registry, in reverse apply order.</summary>
        public static void RevertAll()
        {
            PatchRecord[] snapshot;
            lock (syncLock) snapshot = appliedPatches.Select(Copy).ToArray();
            var failures = new List<Exception>();
            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                try { Revert(snapshot[i]); }
                catch (Exception error) { failures.Add(error); }
            }
            if (failures.Count > 0) throw new AggregateException("One or more legacy Forge patches could not be reverted.", failures);
        }

        /// <summary>Removes receipts only after the shared detour registry verifies original bytes.</summary>
        internal static void RemoveVerifiedRevertedRecords()
        {
            lock (syncLock)
                appliedPatches.RemoveAll(record => record.Original != null && record.OriginalBytes != null &&
                    ForgeDetour.VerifyRestored(record.Original, record.OriginalBytes));
        }

        /// <summary>Reverts legacy records with a matching descriptive owner label.</summary>
        public static int RevertOwner(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("An owner label is required.", nameof(owner));
            PatchRecord[] snapshot;
            lock (syncLock)
                snapshot = appliedPatches.Where(record => string.Equals(record.Owner ?? record.SourceModule, owner,
                    StringComparison.OrdinalIgnoreCase)).Select(Copy).ToArray();
            var failures = new List<Exception>();
            int reverted = 0;
            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                try { Revert(snapshot[i]); reverted++; }
                catch (Exception error) { failures.Add(error); }
            }
            if (failures.Count > 0) throw new AggregateException("One or more owner patches could not be reverted.", failures);
            return reverted;
        }

        /// <summary>
        /// Scans exactly the supplied assembly for [ForgePatch] declarations and applies them
        /// only after every declaration resolves uniquely and passes signature/collision checks.
        /// </summary>
        public static int ApplyAll(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            List<ResolvedPatch> batch = ResolveBatch(assembly);
            if (batch.Count == 0) return 0;

            lock (syncLock)
            {
                var ids = new HashSet<string>(appliedPatches.Select(record => record.Id ?? string.Empty), StringComparer.OrdinalIgnoreCase);
                var targets = new HashSet<MethodInfo>(appliedPatches.Select(record => record.Original));
                foreach (ResolvedPatch patch in batch)
                {
                    if (!ids.Add(patch.Id)) throw new InvalidOperationException("Duplicate patch ID: " + patch.Id);
                    if (!targets.Add(patch.Target) || ForgeDetour.IsTracked(patch.Target))
                        throw new InvalidOperationException("Conflicting patch target: " + Identity(patch.Target));
                }

                string owner = assembly.GetName().Name;
                var records = new PatchRecord[batch.Count];
                var originals = new MethodInfo[batch.Count];
                var replacements = new MethodInfo[batch.Count];
                var patchIds = new string[batch.Count];
                var originalByteCopies = new byte[batch.Count][];
                var generationTokens = new object[batch.Count];
                for (int index = 0; index < batch.Count; index++)
                {
                    ResolvedPatch patch = batch[index];
                    records[index] = new PatchRecord
                    {
                        Id = patch.Id,
                        Owner = owner,
                        Original = patch.Target,
                        Replacement = patch.Replacement,
                        SourceModule = owner
                    };
                    originals[index] = patch.Target;
                    replacements[index] = patch.Replacement;
                    patchIds[index] = patch.Id;
                }

                int requiredCapacity = appliedPatches.Count + batch.Count;
                if (appliedPatches.Capacity < requiredCapacity) appliedPatches.Capacity = requiredCapacity;
                try
                {
                    appliedPatches.AddRange(records);
                    ForgeDetour.PatchBatch(originals, replacements, patchIds, owner, originalByteCopies, generationTokens);
                    for (int index = 0; index < records.Length; index++)
                    {
                        records[index].OriginalBytes = originalByteCopies[index];
                        records[index].GenerationToken = generationTokens[index];
                    }
                    return records.Length;
                }
                catch (Exception applyError)
                {
                    var retained = new List<PatchRecord>();
                    for (int index = 0; index < records.Length; index++)
                    {
                        PatchRecord record = records[index];
                        if (originalByteCopies[index] != null) record.OriginalBytes = originalByteCopies[index];
                        record.GenerationToken = generationTokens[index];
                        if (ForgeDetour.IsTrackedPatch(record.Original, record.Id)) retained.Add(record);
                        else appliedPatches.Remove(record);
                    }
                    if (retained.Count > 0)
                    {
                        throw new AggregateException("Patch batch application failed and one or more uncertain/conflicting records remain; inspect them and do not retry automatically.", applyError);
                    }
                    throw new InvalidOperationException("Patch batch was rejected; already-applied entries were safely reverted.", applyError);
                }
            }
        }

        private sealed class ResolvedPatch
        {
            internal string Id;
            internal MethodInfo Target;
            internal MethodInfo Replacement;
        }

        private static List<ResolvedPatch> ResolveBatch(Assembly assembly)
        {
            if (assembly.IsDynamic) throw new NotSupportedException("Dynamic assemblies are not supported by explicit patch scanning.");
            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException error)
            {
                throw new InvalidOperationException("The complete patch assembly could not be inspected; no patch was applied.", error);
            }

            var declarations = new List<ResolvedPatch>();
            foreach (Type type in types.OrderBy(item => item.FullName, StringComparer.Ordinal))
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly).OrderBy(item => item.Name, StringComparer.Ordinal).ToArray();
                foreach (MethodInfo replacement in methods)
                {
                    IList<CustomAttributeData> attributes = CustomAttributeData.GetCustomAttributes(replacement);
                    int declarationIndex = 0;
                    foreach (CustomAttributeData attribute in attributes.Where(item => item.AttributeType == typeof(ForgePatchAttribute)))
                    {
                        declarationIndex++;
                        if (attribute.ConstructorArguments.Count < 2 || !(attribute.ConstructorArguments[0].Value is Type targetType) ||
                            !(attribute.ConstructorArguments[1].Value is string targetName) || string.IsNullOrWhiteSpace(targetName))
                            throw new InvalidOperationException("Invalid ForgePatch declaration on " + Identity(replacement));
                        if (attribute.ConstructorArguments.Count > 2 && attribute.ConstructorArguments[2].Value != null)
                        {
                            var methodType = (ForgeMethodType)Convert.ToInt32(attribute.ConstructorArguments[2].Value);
                            if (methodType == ForgeMethodType.Getter) targetName = "get_" + targetName;
                            else if (methodType == ForgeMethodType.Setter) targetName = "set_" + targetName;
                        }

                        var candidates = targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                            .Where(target => string.Equals(target.Name, targetName, StringComparison.Ordinal) && ExactSignature(target, replacement))
                            .ToArray();
                        if (candidates.Length != 1)
                            throw new InvalidOperationException(candidates.Length == 0
                                ? "No exact patch target resolves for " + Identity(replacement) + " -> " + targetType.FullName + "." + targetName
                                : "Ambiguous exact patch target for " + Identity(replacement) + " -> " + targetType.FullName + "." + targetName);
                        MethodInfo targetMethod = candidates[0];
                        ForgeDetour.ValidatePair(targetMethod, replacement);
                        declarations.Add(new ResolvedPatch
                        {
                            Id = "assembly:" + ForgeDetour.ShortHash(assembly.GetName().Name + ":" + SignatureIdentity(replacement)) + ":" + declarationIndex,
                            Target = targetMethod,
                            Replacement = replacement
                        });
                    }
                }
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var targets = new HashSet<MethodInfo>();
            foreach (ResolvedPatch declaration in declarations)
            {
                if (!ids.Add(declaration.Id)) throw new InvalidOperationException("Duplicate patch ID in assembly: " + declaration.Id);
                if (!targets.Add(declaration.Target)) throw new InvalidOperationException("Multiple declarations target " + Identity(declaration.Target));
            }
            return declarations;
        }

        private static bool ExactSignature(MethodInfo target, MethodInfo replacement)
        {
            if (target.IsGenericMethod || replacement.IsGenericMethod || target.ContainsGenericParameters || replacement.ContainsGenericParameters)
                return false;
            if (target.IsStatic != replacement.IsStatic || target.CallingConvention != replacement.CallingConvention || target.ReturnType != replacement.ReturnType)
                return false;
            ParameterInfo[] left = target.GetParameters();
            ParameterInfo[] right = replacement.GetParameters();
            if (left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
                if (left[i].ParameterType != right[i].ParameterType || left[i].IsIn != right[i].IsIn || left[i].IsOut != right[i].IsOut) return false;
            return true;
        }

        private static string MakeLegacyId(MethodInfo original, MethodInfo replacement)
        {
            return "legacy:" + ForgeDetour.ShortHash(SignatureIdentity(original) + "->" + SignatureIdentity(replacement));
        }

        private static string SignatureIdentity(MethodInfo method)
        {
            return ForgeDetour.SignatureIdentity(method);
        }

        private static PatchRecord Copy(PatchRecord source)
        {
            return new PatchRecord
            {
                Id = source.Id,
                Owner = source.Owner,
                Original = source.Original,
                Replacement = source.Replacement,
                SourceModule = source.SourceModule,
                OriginalBytes = source.OriginalBytes == null ? null : (byte[])source.OriginalBytes.Clone(),
                GenerationToken = source.GenerationToken
            };
        }

        private static string Identity(MethodInfo method)
        {
            return (method.DeclaringType == null ? "?" : method.DeclaringType.FullName) + "." + method.Name;
        }
    }
}
