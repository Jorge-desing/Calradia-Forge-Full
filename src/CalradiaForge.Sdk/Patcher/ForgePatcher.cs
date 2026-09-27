using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CalradiaForge.Sdk.Patcher
{
    public class PatchRecord
    {
        public MethodInfo Original { get; set; }
        public MethodInfo Replacement { get; set; }
        public string SourceModule { get; set; }
        public byte[] OriginalBytes { get; set; }

        /// <summary>
        /// Verifies if the JMP instruction is still in place, meaning no other framework has overwritten it.
        /// </summary>
        public unsafe bool IsIntact()
        {
            if (Original == null) return false;
            byte* ptr = (byte*)Original.MethodHandle.GetFunctionPointer();
            return *ptr == 0x49 && *(ptr + 1) == 0xBB;
        }
    }

    public static class ForgePatcher
    {
        private static readonly List<PatchRecord> appliedPatches = new List<PatchRecord>();

        public static IReadOnlyList<PatchRecord> GetAppliedPatches() => appliedPatches.AsReadOnly();

        /// <summary>
        /// Audits all applied patches to ensure they haven't been overwritten by other frameworks (like Harmony).
        /// Returns a list of broken patches.
        /// </summary>
        public static List<PatchRecord> VerifyIntegrity()
        {
            return appliedPatches.Where(p => !p.IsIntact()).ToList();
        }

        /// <summary>
        /// Manually registers a patch record into the tracker.
        /// </summary>
        public static void Register(PatchRecord record)
        {
            if (record != null && !appliedPatches.Contains(record))
            {
                appliedPatches.Add(record);
            }
        }

        /// <summary>
        /// Reverts a specific patch by restoring its original assembly bytes.
        /// </summary>
        public static void Revert(PatchRecord record)
        {
            if (record == null || record.OriginalBytes == null) throw new ArgumentException("Invalid patch record.");
            MethodSwapper.RestoreMethod(record.Original, record.OriginalBytes);
            appliedPatches.Remove(record);
        }

        /// <summary>
        /// Reverts all active patches applied by ForgePatcher.
        /// </summary>
        public static void RevertAll()
        {
            // Traverse backwards to avoid index issues when removing
            for (int i = appliedPatches.Count - 1; i >= 0; i--)
            {
                Revert(appliedPatches[i]);
            }
        }

        /// <summary>
        /// Scans the provided assembly for methods marked with [ForgePatch]
        /// and applies the replacement detours automatically.
        /// </summary>
        /// <param name="assembly">The assembly to scan</param>
        /// <returns>The number of successful patches applied.</returns>
        public static int ApplyAll(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            int patchesApplied = 0;
            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

            foreach (var type in types)
            {
                MethodInfo[] methods;
                try { methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance); }
                catch { continue; } // Skip types that fail method reflection

                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(ForgePatchAttribute), false);
                    foreach (ForgePatchAttribute attr in attributes)
                    {
                        if (attr.TargetType == null || string.IsNullOrWhiteSpace(attr.TargetMethod))
                            continue;

                        var parameters = method.GetParameters();
                        MethodInfo targetMethod = null;
                        
                        foreach (var tm in attr.TargetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                        {
                            if (tm.Name == attr.TargetMethod)
                            {
                                var targetParams = tm.GetParameters();
                                
                                // Check if it's an exact match (both static or both instance)
                                bool exactMatch = targetParams.Length == parameters.Length;
                                
                                // Check if it's a static method patching an instance method (first param is 'this')
                                bool instanceMatch = !tm.IsStatic && method.IsStatic && parameters.Length == targetParams.Length + 1;

                                if (exactMatch || instanceMatch)
                                {
                                    bool match = true;
                                    int offset = instanceMatch ? 1 : 0;

                                    if (instanceMatch && !attr.TargetType.IsAssignableFrom(parameters[0].ParameterType))
                                    {
                                        match = false;
                                    }
                                    else
                                    {
                                        for (int i = 0; i < targetParams.Length; i++)
                                        {
                                            if (parameters[i + offset].ParameterType != targetParams[i].ParameterType)
                                            {
                                                match = false;
                                                break;
                                            }
                                        }
                                    }

                                    if (match)
                                    {
                                        targetMethod = tm;
                                        break;
                                    }
                                }
                            }
                        }

                        if (targetMethod != null)
                        {
                            byte[] backupBytes = MethodSwapper.DetourMethod(targetMethod, method);
                            appliedPatches.Add(new PatchRecord { Original = targetMethod, Replacement = method, SourceModule = assembly.GetName().Name, OriginalBytes = backupBytes });
                            patchesApplied++;
                        }
                        else
                        {
                            throw new MissingMethodException($"ForgePatcher: Could not find target method {attr.TargetType.Name}.{attr.TargetMethod} matching the signature of {type.Name}.{method.Name}");
                        }
                    }
                }
            }
            return patchesApplied;
        }
    }
}

