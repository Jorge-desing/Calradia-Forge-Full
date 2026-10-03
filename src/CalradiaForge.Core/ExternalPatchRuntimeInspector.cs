using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CalradiaForge.Core
{
    // This reflector has no Harmony reference. It observes only the public API when a compatible
    // Harmony runtime is present in the caller-supplied assemblies.
    internal static class ExternalPatchRuntimeInspector
    {
        const int MaximumAssemblyProbes = 512;
        const int MaximumTargetEntries = 4096;
        const int MaximumDiscoveredTargets = 2000;
        const int MaximumDisplayedTargets = 64;
        const int MaximumPatchesPerMethod = 16;
        const int MaximumTotalPatchObservations = 128;
        const int MaximumOwnersPerMethod = 8;
        const int MaximumOrderingIds = 8;
        const int MaximumTotalOrderingIds = 512;
        const int MaximumNotes = 16;
        const int MaximumTextLength = 160;
        const string ExpectedRuntimeAssemblyName = "0Harmony";

        internal static ExternalPatchRuntimeSnapshot Inspect(IEnumerable<Assembly> assemblies, string filter = null, int maximumMethods = 200)
        {
            var result = CreateSnapshot("bounded reflection over caller-supplied assemblies and Harmony's public query API");
            var harmonyType = FindHarmonyType(assemblies, result);
            if (harmonyType == null)
            {
                if (result.Truncated || result.Status == "Incomplete")
                {
                    result.Detail = "Inconclusive: the supplied assembly scan was incomplete; whether the compatible external runtime is loaded could not be determined.";
                    result.Status = "Incomplete";
                }
                else if (result.Status != "Unsupported")
                {
                    result.Detail = "Inconclusive: the expected external runtime was not found in the supplied assemblies; no conclusion can be made about other detours.";
                    result.Status = "NotLoaded";
                }
                AddNote(result, "Inconclusive: this bounded scan does not inspect every loaded assembly, native detours, or other patching backends.");
                return result;
            }

            InspectRuntime(harmonyType, filter, maximumMethods, result);
            return result;
        }

        internal static ExternalPatchRuntimeSnapshot NotRequested()
        {
            return new ExternalPatchRuntimeSnapshot
            {
                Status = "NotRequested",
                CapturedAt = DateTime.UtcNow.ToString("O"),
                Detail = "Optional external-runtime inspection was not requested; no compatibility conclusion is available."
            };
        }

        static ExternalPatchRuntimeSnapshot CreateSnapshot(string origin)
        {
            var result = new ExternalPatchRuntimeSnapshot { CapturedAt = DateTime.UtcNow.ToString("O") };
            AddNote(result, "Origin: " + origin + ".");
            AddNote(result, "Freshness: captured at the UTC timestamp shown; the host may mark this snapshot stale after a context change.");
            AddNote(result, "Safety: output caps do not bound synchronous runtime getters or reflection arrays such as MethodBase.GetParameters; no sandboxing is provided.");
            return result;
        }

        static void InspectRuntime(Type harmonyType, string filter, int maximumMethods, ExternalPatchRuntimeSnapshot result)
        {
            if (harmonyType == null)
            {
                result.Detail = "Inconclusive: no already-loaded external patch runtime type was supplied.";
                result.Status = "NotLoaded";
                AddNote(result, "Inconclusive: provide loaded assemblies for a bounded Harmony API probe.");
                return;
            }

            try
            {
                result.RuntimeAssembly = Text(harmonyType.Assembly.GetName().Name, result);
                result.RuntimeVersion = Text(harmonyType.Assembly.GetName().Version?.ToString(), result);
            }
            catch (Exception error)
            {
                MarkIncomplete(result, "Harmony runtime identity could not be read: " + Describe(error, result));
            }

            MethodInfo methods;
            MethodInfo patchInfo;
            try
            {
                methods = FindMethodsQuery(harmonyType);
                patchInfo = FindPatchInfoQuery(harmonyType);
            }
            catch (Exception error)
            {
                MarkIncomplete(result, "Harmony query API reflection failed: " + Describe(error, result));
                result.Detail = "Inconclusive: Harmony query API could not be inspected.";
                result.Status = "Incomplete";
                return;
            }

            if (methods == null || patchInfo == null)
            {
                result.Detail = "Inconclusive: Harmony is loaded, but its public patch query API is unavailable.";
                result.Status = "Unsupported";
                AddNote(result, "Inconclusive: no conclusion about active patches can be drawn; update Harmony or inspect this runtime with its own tooling. Non-Harmony detours are outside this snapshot.");
                return;
            }

            IEnumerable patched;
            try
            {
                patched = methods.Invoke(null, null) as IEnumerable;
            }
            catch (Exception error)
            {
                result.Detail = "Inconclusive: Harmony patch query failed.";
                MarkIncomplete(result, "Harmony query error: " + Describe(error, result));
                result.Status = "Incomplete";
                return;
            }

            if (patched == null)
            {
                result.Detail = "Inconclusive: Harmony returned no enumerable patch targets; this does not establish the absence of other detours.";
                AddNote(result, "Inconclusive: the query returned null instead of a target collection.");
                MarkIncomplete(result, "The external runtime returned a null target collection.");
                result.Status = "Incomplete";
                return;
            }

            var captured = new List<ExternalPatchTarget>();
            var totalPatchObservations = 0;
            var totalOrderingIds = 0;
            EnumerateTargets(patched, patchInfo, filter, result, captured, ref totalPatchObservations, ref totalOrderingIds);

            result.OwnerCount = captured.SelectMany(row => row.Owners).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            result.SharedTargetCount = captured.Count(row => row.HasMultipleOwners);
            result.ActiveTargetCount = captured.Count(row => row.MetadataStatus == "Active");
            result.EmptyMetadataTargetCount = captured.Count(row => row.MetadataStatus != "Active");

            if (result.ActiveTargetCount > 0)
                AddNote(result, "Observed: Harmony returned active patch metadata for " + result.ActiveTargetCount + " target(s).");
            if (result.SharedTargetCount > 0)
                AddNote(result, "Review: " + result.SharedTargetCount + " target(s) have multiple Harmony owners; shared ownership does not prove a destructive conflict.");
            if (result.EmptyMetadataTargetCount > 0)
                AddNote(result, "Inconclusive: " + result.EmptyMetadataTargetCount + " reported target(s) had absent or unreadable patch metadata.");

            var limit = Math.Max(1, Math.Min(maximumMethods, MaximumDisplayedTargets));
            var ordered = captured
                .OrderByDescending(row => row.HasMultipleOwners)
                .ThenByDescending(row => row.Patches.Count)
                .ThenBy(row => row.Assembly, StringComparer.Ordinal)
                .ThenBy(row => row.DeclaringType, StringComparer.Ordinal)
                .ThenBy(row => row.Method, StringComparer.Ordinal)
                .ThenBy(row => row.Signature, StringComparer.Ordinal)
                .ThenBy(row => row.StableSortToken)
                .ToList();
            if (ordered.Count > limit)
            {
                result.Truncated = true;
                AddNote(result, "Inconclusive: displaying " + limit + " of " + ordered.Count + " matching patch targets.");
                ordered = ordered.Take(limit).ToList();
            }
            result.Targets = ordered;
            result.DisplayedTargetCount = ordered.Count;

            if (result.Truncated)
                AddNote(result, "Inconclusive: one or more output fields or collections were clipped by diagnostic safety bounds.");

            var incomplete = result.Status == "Incomplete";
            if (result.Truncated || incomplete || result.ActiveTargetCount == 0 || ordered.Count == 0)
            {
                result.Detail = BuildInconclusiveStatus(result, filter);
                AddNote(result, "Inconclusive: an empty Harmony result cannot establish the absence of non-Harmony detours or other patching backends.");
                result.Status = result.Truncated || incomplete || result.EmptyMetadataTargetCount > 0 ? "Incomplete" : "Observed";
            }
            else
            {
                result.Detail = "Observed: Harmony reports active patch metadata. Shared targets are review signals, not confirmed conflicts.";
                result.Status = "Observed";
            }

            result.Detail = Text(result.Detail, result);
            if (result.Truncated)
            {
                AddNote(result, "Inconclusive: one or more output fields or collections were clipped by diagnostic safety bounds.");
                result.Status = "Incomplete";
            }
        }

        static string BuildInconclusiveStatus(ExternalPatchRuntimeSnapshot result, string filter)
        {
            if (result.ActiveTargetCount == 0 && result.EmptyMetadataTargetCount > 0)
                return "Inconclusive: Harmony returned target identities without confirmed active patch metadata; active patch state is not confirmed and non-Harmony detours were not inspected.";
            if (result.DisplayedTargetCount == 0 && !string.IsNullOrWhiteSpace(filter) && result.DiscoveredTargetCount > 0)
                return "Inconclusive: no Harmony targets match this filter; this does not establish the absence of other detours.";
            if (result.DiscoveredTargetCount == 0)
                return "Inconclusive: Harmony reported no patched methods; non-Harmony detours were not inspected.";
            return "Inconclusive: the Harmony snapshot is bounded or incomplete.";
        }

        static void EnumerateTargets(IEnumerable patched, MethodInfo patchInfo, string filter, ExternalPatchRuntimeSnapshot result, List<ExternalPatchTarget> captured, ref int totalPatchObservations, ref int totalOrderingIds)
        {
            IEnumerator iterator;
            try { iterator = patched.GetEnumerator(); }
            catch (Exception error)
            {
                MarkIncomplete(result, "Harmony target enumeration could not start: " + Describe(error, result));
                return;
            }
            if (iterator == null)
            {
                MarkIncomplete(result, "Harmony returned a null target enumerator.");
                return;
            }

            var entries = 0;
            try
            {
                while (entries < MaximumTargetEntries && result.DiscoveredTargetCount < MaximumDiscoveredTargets)
                {
                    bool moved;
                    try { moved = iterator.MoveNext(); }
                    catch (Exception error)
                    {
                        MarkIncomplete(result, "Harmony target enumeration stopped: " + Describe(error, result));
                        return;
                    }
                    if (!moved) return;
                    entries++;

                    object value;
                    try { value = iterator.Current; }
                    catch (Exception error)
                    {
                        result.SkippedTargetCount++;
                        MarkIncomplete(result, "A Harmony target entry could not be read: " + Describe(error, result));
                        continue;
                    }

                    var target = value as MethodBase;
                    if (target == null)
                    {
                        result.SkippedTargetCount++;
                        MarkIncomplete(result, value == null
                            ? "Harmony returned a null target entry; it was skipped."
                            : "Harmony returned a target entry that was not a MethodBase; it was skipped.");
                        continue;
                    }

                    result.DiscoveredTargetCount++;
                    try
                    {
                        var row = DescribeTarget(target, patchInfo, result, ref totalPatchObservations, ref totalOrderingIds);
                        if (Matches(row, filter)) captured.Add(row);
                    }
                    catch (Exception error)
                    {
                        result.SkippedTargetCount++;
                        MarkIncomplete(result, "Harmony metadata for " + Text(target.Name, result) + " could not be processed: " + Describe(error, result));
                    }
                }

                if (entries >= MaximumTargetEntries || result.DiscoveredTargetCount >= MaximumDiscoveredTargets)
                {
                    bool hasMore;
                    try { hasMore = iterator.MoveNext(); }
                    catch (Exception error)
                    {
                        MarkIncomplete(result, "Target enumeration could not confirm whether more entries remain: " + Describe(error, result));
                        return;
                    }
                    if (hasMore && entries >= MaximumTargetEntries)
                        MarkTruncated(result, "Harmony target enumeration continued beyond the " + MaximumTargetEntries + " entry safety limit.");
                    else if (hasMore)
                        MarkTruncated(result, "Harmony target discovery continued beyond the " + MaximumDiscoveredTargets + " target safety limit.");
                }
            }
            finally
            {
                try { (iterator as IDisposable)?.Dispose(); }
                catch (Exception error) { MarkIncomplete(result, "Harmony target enumerator cleanup failed: " + Describe(error, result)); }
            }
        }

        static Type FindHarmonyType(IEnumerable<Assembly> assemblies, ExternalPatchRuntimeSnapshot result)
        {
            if (assemblies == null)
            {
                MarkIncomplete(result, "The caller supplied a null assembly collection.");
                return null;
            }

            IEnumerator<Assembly> iterator;
            try { iterator = assemblies.GetEnumerator(); }
            catch (Exception error)
            {
                MarkIncomplete(result, "The supplied assembly collection could not be read: " + Describe(error, result));
                return null;
            }
            if (iterator == null)
            {
                MarkIncomplete(result, "The supplied assembly collection returned a null enumerator.");
                return null;
            }

            var count = 0;
            var expectedAssemblyFound = false;
            try
            {
                while (count < MaximumAssemblyProbes)
                {
                    bool moved;
                    try { moved = iterator.MoveNext(); }
                    catch (Exception error)
                    {
                        MarkIncomplete(result, "Assembly discovery stopped: " + Describe(error, result));
                        return null;
                    }
                    if (!moved) return null;
                    count++;

                    Assembly assembly;
                    try { assembly = iterator.Current; }
                    catch (Exception error)
                    {
                        MarkIncomplete(result, "An assembly entry could not be read: " + Describe(error, result));
                        continue;
                    }
                    if (assembly == null)
                    {
                        MarkIncomplete(result, "A null assembly entry was skipped during Harmony discovery.");
                        continue;
                    }

                    try
                    {
                        var assemblyName = assembly.GetName().Name;
                        if (!string.Equals(assemblyName, ExpectedRuntimeAssemblyName, StringComparison.OrdinalIgnoreCase)) continue;
                        expectedAssemblyFound = true;
                        var harmony = assembly.GetType("HarmonyLib.Harmony", false);
                        if (harmony == null) continue;
                        var typeAssemblyIdentity = harmony.Assembly?.GetName()?.FullName;
                        var candidateAssemblyIdentity = assembly.GetName()?.FullName;
                        if (string.Equals(typeAssemblyIdentity, candidateAssemblyIdentity, StringComparison.OrdinalIgnoreCase)) return harmony;
                        MarkIncomplete(result, "HarmonyLib.Harmony resolved outside the expected 0Harmony assembly identity and was ignored.");
                    }
                    catch (Exception error)
                    {
                        MarkIncomplete(result, "Harmony type lookup failed for an assembly: " + Describe(error, result));
                    }
                }

                bool hasMore;
                try { hasMore = iterator.MoveNext(); }
                catch (Exception error)
                {
                    MarkIncomplete(result, "Assembly discovery could not confirm whether more assemblies remain: " + Describe(error, result));
                    return null;
                }
                if (hasMore) MarkTruncated(result, "Harmony assembly discovery continued beyond the " + MaximumAssemblyProbes + " assembly probe limit.");
                if (expectedAssemblyFound && !result.Truncated && result.Status != "Incomplete")
                {
                    result.Status = "Unsupported";
                    result.Detail = "Inconclusive: the expected 0Harmony assembly is loaded, but it does not expose HarmonyLib.Harmony from the same assembly identity.";
                    AddNote(result, "Inconclusive: the loaded 0Harmony assembly did not provide the expected public Harmony type.");
                }
                return null;
            }
            finally
            {
                try { iterator.Dispose(); }
                catch (Exception error) { MarkIncomplete(result, "Assembly discovery cleanup failed: " + Describe(error, result)); }
            }
        }

        static ExternalPatchTarget DescribeTarget(MethodBase target, MethodInfo getPatchInfo, ExternalPatchRuntimeSnapshot result, ref int totalPatchObservations, ref int totalOrderingIds)
        {
            var row = new ExternalPatchTarget
            {
                Assembly = SafeAssemblyName(target.DeclaringType, result),
                DeclaringType = SafeTypeName(target.DeclaringType, result),
                Method = SafeMethodName(target, result),
                Signature = Signature(target, result),
                StableSortToken = SafeMetadataToken(target, result),
                MetadataStatus = "No metadata"
            };

            object info;
            try { info = getPatchInfo.Invoke(null, new object[] { target }); }
            catch (Exception error)
            {
                row.MetadataStatus = "Unreadable";
                MarkIncomplete(result, "Harmony patch metadata query failed for " + row.DeclaringType + "." + row.Method + ": " + Describe(error, result));
                return row;
            }

            if (info == null)
            {
                AddNote(result, "Inconclusive: Harmony returned no patch metadata for " + row.DeclaringType + "." + row.Method + ".");
                return row;
            }

            var ownersRead = Read(info, result, "Owners");
            if (!ownersRead.Found)
            {
                MarkIncomplete(result, "Harmony patch metadata omitted the Owners collection.");
                row.MetadataStatus = "Unreadable";
            }
            if (ownersRead.Failed || (ownersRead.Found && ownersRead.Value == null)) row.MetadataStatus = "Unreadable";
            if (ownersRead.Found && ownersRead.Value == null)
                MarkIncomplete(result, "Harmony patch metadata returned a null Owners collection.");
            var ownerCollectionFailed = false;
            row.Owners = Strings(ownersRead.Value, MaximumOwnersPerMethod, result, "owner", ref ownerCollectionFailed);
            row.Owners = row.Owners
                .Where(owner => !string.IsNullOrWhiteSpace(owner))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(owner => owner, StringComparer.Ordinal)
                .ToList();
            if (ownerCollectionFailed) row.MetadataStatus = "Unreadable";
            foreach (var kind in new[] { "Prefix", "Postfix", "Transpiler", "Finalizer" })
                if (!AddPatches(row, info, kind, result, ref totalPatchObservations, ref totalOrderingIds)) row.MetadataStatus = "Unreadable";

            if (row.Owners.Count == 0)
                row.Owners = row.Patches.Select(patch => patch.Owner).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.Ordinal).ToList();
            row.HasMultipleOwners = row.Owners.Count > 1;
            if (row.Owners.Count > 0 || row.Patches.Count > 0)
            {
                if (row.MetadataStatus != "Unreadable") row.MetadataStatus = "Active";
            }
            else if (row.MetadataStatus != "Unreadable")
            {
                row.MetadataStatus = "No metadata";
                AddNote(result, "Inconclusive: Harmony returned no active patch metadata for " + row.DeclaringType + "." + row.Method + ".");
            }

            return row;
        }

        static bool AddPatches(ExternalPatchTarget row, object info, string kind, ExternalPatchRuntimeSnapshot result, ref int totalPatchObservations, ref int totalOrderingIds)
        {
            var property = kind == "Prefix" ? "Prefixes"
                : kind == "Postfix" ? "Postfixes"
                : kind == "Transpiler" ? "Transpilers"
                : "Finalizers";
            var read = Read(info, result, property);
            if (!read.Found)
            {
                MarkIncomplete(result, "Harmony patch metadata omitted the " + property + " collection.");
                return false;
            }
            if (read.Failed) return false;
            var values = read.Value as IEnumerable;
            if (read.Value == null) return true;
            if (read.Value is string || values == null)
            {
                MarkIncomplete(result, "Harmony " + kind.ToLowerInvariant() + " metadata had an unsupported shape.");
                return false;
            }
            var remaining = MaximumTotalPatchObservations - totalPatchObservations;
            if (remaining <= 0)
            {
                if (!ProbeHasMore(values, result, kind.ToLowerInvariant() + " metadata at the global patch limit", out var hasMore)) return false;
                if (hasMore)
                {
                    MarkTruncated(result, "External patch observations continued beyond the global " + MaximumTotalPatchObservations + " entry safety limit.");
                    return false;
                }
                return true;
            }
            var patchLimit = Math.Min(MaximumPatchesPerMethod, remaining);

            var failed = false;
            var count = 0;
            var observedTotal = totalPatchObservations;
            var orderingTotal = totalOrderingIds;
            var complete = ForEachBounded(values, patchLimit, result, kind.ToLowerInvariant() + " metadata", value =>
            {
                if (value == null)
                {
                    failed = true;
                    MarkIncomplete(result, "Harmony returned a null " + kind.ToLowerInvariant() + " metadata entry; it was skipped.");
                    return;
                }
                if (count++ >= patchLimit)
                {
                    MarkTruncated(result, "Patch metadata was limited to " + patchLimit + " " + kind.ToLowerInvariant() + " entries for this target.");
                    return;
                }

                var patch = new ExternalPatchObservation { Kind = kind, Owner = "unknown" };
                var owner = Read(value, result, "owner", "Owner");
                var priority = Read(value, result, "priority", "Priority");
                var index = Read(value, result, "index", "Index");
                var before = Read(value, result, "before", "Before");
                var after = Read(value, result, "after", "After");
                var patchMethodRead = Read(value, result, "PatchMethod", "Method", "method");
                if (!owner.Found || !priority.Found || !index.Found || !before.Found || !after.Found || !patchMethodRead.Found)
                {
                    failed = true;
                    MarkIncomplete(result, "Harmony patch metadata omitted one or more owner, ordering, priority, index, or patch-method fields.");
                }
                if (owner.Failed || priority.Failed || index.Failed || before.Failed || after.Failed || patchMethodRead.Failed) failed = true;
                var ownerName = owner.Value as string;
                if (ownerName == null)
                {
                    failed = true;
                    MarkIncomplete(result, "Harmony patch owner metadata was missing or was not a string.");
                }
                patch.Owner = Text(ownerName, result) ?? "unknown";
                if (priority.Value == null || !Integer(priority.Value, out var parsedPriority))
                {
                    failed = true;
                    MarkIncomplete(result, "Harmony patch priority metadata was missing or was not an integer.");
                }
                else patch.Priority = parsedPriority;
                if (index.Value == null || !Integer(index.Value, out var parsedIndex))
                {
                    failed = true;
                    MarkIncomplete(result, "Harmony patch index metadata was missing or was not an integer.");
                }
                else patch.Index = parsedIndex;
                var beforeFailed = false;
                patch.Before = OrderingStrings(before.Value, MaximumOrderingIds, result, "before ordering", ref orderingTotal, ref beforeFailed);
                var afterFailed = false;
                patch.After = OrderingStrings(after.Value, MaximumOrderingIds, result, "after ordering", ref orderingTotal, ref afterFailed);
                if (beforeFailed || afterFailed) failed = true;

                var method = patchMethodRead.Value as MethodBase;
                if (method == null && patchMethodRead.Value != null)
                {
                    var nested = Read(patchMethodRead.Value, result, "method", "Method");
                    if (nested.Failed) failed = true;
                    method = nested.Value as MethodBase;
                }
                if (method != null)
                {
                    patch.PatchAssembly = SafeAssemblyName(method.DeclaringType, result);
                    patch.PatchType = SafeTypeName(method.DeclaringType, result);
                    patch.PatchMethod = SafeMethodName(method, result);
                    patch.PatchSignature = Signature(method, result);
                }
                else
                {
                    failed = true;
                    MarkIncomplete(result, "Harmony patch method metadata was missing or had an unsupported shape.");
                }
                row.Patches.Add(patch);
                observedTotal++;
            });
            totalPatchObservations = observedTotal;
            totalOrderingIds = orderingTotal;
            return !failed && complete;
        }

        static List<string> OrderingStrings(object value, int maximum, ExternalPatchRuntimeSnapshot result, string label, ref int totalOrderingIds, ref bool failed)
        {
            var strings = new List<string>();
            if (value == null) return strings;
            if (value is string)
            {
                failed = true;
                MarkIncomplete(result, "Harmony " + label + " metadata had an unsupported shape.");
                return strings;
            }
            var enumerable = value as IEnumerable;
            if (enumerable == null)
            {
                failed = true;
                MarkIncomplete(result, "Harmony " + label + " metadata was not enumerable.");
                return strings;
            }
            var remaining = MaximumTotalOrderingIds - totalOrderingIds;
            var limit = Math.Min(maximum, remaining);
            if (limit <= 0)
            {
                if (!ProbeHasMore(enumerable, result, label + " at the global ordering-ID limit", out var hasMore)) failed = true;
                else if (hasMore)
                {
                    failed = true;
                    MarkTruncated(result, "External ordering IDs continued beyond the global " + MaximumTotalOrderingIds + " entry safety limit.");
                }
                return strings;
            }
            var orderingTotal = totalOrderingIds;
            var malformedEntry = false;
            var complete = ForEachBounded(enumerable, limit, result, label, item =>
            {
                var text = item as string;
                if (text == null)
                {
                    malformedEntry = true;
                    MarkIncomplete(result, "Harmony " + label + " entry was not a string.");
                    return;
                }
                if (!string.IsNullOrWhiteSpace(text)) strings.Add(Text(text, result));
                orderingTotal++;
            });
            totalOrderingIds = orderingTotal;
            if (!complete || malformedEntry) failed = true;
            return strings;
        }

        static bool ForEachBounded(IEnumerable values, int maximum, ExternalPatchRuntimeSnapshot result, string label, Action<object> action)
        {
            IEnumerator iterator;
            try { iterator = values.GetEnumerator(); }
            catch (Exception error)
            {
                MarkIncomplete(result, label + " could not be enumerated: " + Describe(error, result));
                return false;
            }
            if (iterator == null)
            {
                MarkIncomplete(result, label + " returned a null enumerator.");
                return false;
            }

            var count = 0;
            var complete = true;
            try
            {
                while (true)
                {
                    bool moved;
                    try { moved = iterator.MoveNext(); }
                    catch (Exception error)
                    {
                        MarkIncomplete(result, label + " enumeration stopped: " + Describe(error, result));
                        complete = false;
                        break;
                    }
                    if (!moved) break;
                    if (count >= maximum)
                    {
                        MarkTruncated(result, label + " exceeded the " + maximum + " entry safety limit.");
                        complete = false;
                        break;
                    }
                    count++;
                    object current;
                    try { current = iterator.Current; }
                    catch (Exception error)
                    {
                        MarkIncomplete(result, label + " entry could not be read: " + Describe(error, result));
                        complete = false;
                        continue;
                    }
                    try { action(current); }
                    catch (Exception error) { complete = false; MarkIncomplete(result, label + " entry could not be processed: " + Describe(error, result)); }
                }
            }
            finally
            {
                try { (iterator as IDisposable)?.Dispose(); }
                catch (Exception error) { complete = false; MarkIncomplete(result, label + " enumerator cleanup failed: " + Describe(error, result)); }
            }
            return complete;
        }

        static bool ProbeHasMore(IEnumerable values, ExternalPatchRuntimeSnapshot result, string label, out bool hasMore)
        {
            hasMore = false;
            IEnumerator iterator;
            try { iterator = values.GetEnumerator(); }
            catch (Exception error)
            {
                MarkIncomplete(result, label + " could not be probed: " + Describe(error, result));
                return false;
            }
            if (iterator == null)
            {
                MarkIncomplete(result, label + " returned a null enumerator.");
                return false;
            }
            var complete = true;
            try
            {
                try { hasMore = iterator.MoveNext(); }
                catch (Exception error) { complete = false; MarkIncomplete(result, label + " lookahead failed: " + Describe(error, result)); }
            }
            finally
            {
                try { (iterator as IDisposable)?.Dispose(); }
                catch (Exception error) { complete = false; MarkIncomplete(result, label + " lookahead cleanup failed: " + Describe(error, result)); }
            }
            return complete;
        }

        sealed class ReadValue
        {
            public object Value;
            public bool Found;
            public bool Failed;
        }

        static ReadValue Read(object source, ExternalPatchRuntimeSnapshot result, params string[] names)
        {
            var read = new ReadValue();
            if (source == null) return read;
            foreach (var name in names)
            {
                try
                {
                    var type = source.GetType();
                    var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (property != null)
                    {
                        read.Found = true;
                        var getter = property.GetGetMethod(false);
                        if (getter == null || !getter.IsPublic || getter.IsStatic || getter.GetParameters().Length != 0)
                        {
                            read.Failed = true;
                            MarkIncomplete(result, "Metadata property " + name + " has no public instance getter.");
                            continue;
                        }
                        try { read.Value = property.GetValue(source, null); return read; }
                        catch (Exception error) { read.Failed = true; MarkIncomplete(result, "Metadata getter " + name + " failed: " + Describe(error, result)); continue; }
                    }
                    var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (field != null)
                    {
                        read.Found = true;
                        try { read.Value = field.GetValue(source); return read; }
                        catch (Exception error) { read.Failed = true; MarkIncomplete(result, "Metadata field " + name + " failed: " + Describe(error, result)); }
                    }
                }
                catch (Exception error)
                {
                    read.Failed = true;
                    MarkIncomplete(result, "Metadata reflection for " + name + " failed: " + Describe(error, result));
                }
            }
            return read;
        }

        static List<string> Strings(object value, int maximum, ExternalPatchRuntimeSnapshot result, string label, ref bool failed)
        {
            var strings = new List<string>();
            if (value == null) return strings;
            var single = value as string;
            if (single != null)
            {
                if (!string.IsNullOrWhiteSpace(single)) strings.Add(Text(single, result));
                return strings;
            }
            var enumerable = value as IEnumerable;
            if (enumerable == null)
            {
                failed = true;
                MarkIncomplete(result, "Harmony " + label + " metadata was not enumerable.");
                return strings;
            }

            var malformedEntry = false;
            var complete = ForEachBounded(enumerable, maximum, result, label, item =>
            {
                var text = item as string;
                if (text == null)
                {
                    malformedEntry = true;
                    MarkIncomplete(result, "Harmony " + label + " entry was not a string.");
                    return;
                }
                if (!string.IsNullOrWhiteSpace(text)) strings.Add(Text(text, result));
            });
            if (!complete || malformedEntry) failed = true;
            return strings;
        }

        static bool Matches(ExternalPatchTarget row, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return true;
            var value = filter.Trim();
            if (Contains(row.Assembly, value) || Contains(row.DeclaringType, value) || Contains(row.Method, value) || Contains(row.Signature, value)) return true;
            return row.Owners.Any(owner => Contains(owner, value)) || row.Patches.Any(patch => Contains(patch.Owner, value) || Contains(patch.Kind, value) || Contains(patch.PatchAssembly, value) || Contains(patch.PatchType, value) || Contains(patch.PatchMethod, value));
        }

        static MethodInfo FindMethodsQuery(Type type)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(method =>
            {
                return method.Name == "GetAllPatchedMethods"
                    && method.GetParameters().Length == 0
                    && method.ReturnType != typeof(void)
                    && typeof(IEnumerable).IsAssignableFrom(method.ReturnType);
            });
        }

        static MethodInfo FindPatchInfoQuery(Type type)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(method =>
            {
                var parameters = method.GetParameters();
                return method.Name == "GetPatchInfo"
                    && parameters.Length == 1
                    && parameters[0].ParameterType == typeof(MethodBase)
                    && method.ReturnType != typeof(void);
            });
        }

        static string SafeAssemblyName(Type type, ExternalPatchRuntimeSnapshot result)
        {
            try { return Text(type?.Assembly.GetName().Name, result); }
            catch (Exception error) { MarkIncomplete(result, "A declaring assembly name could not be read: " + Describe(error, result)); return null; }
        }

        static string SafeTypeName(Type type, ExternalPatchRuntimeSnapshot result)
        {
            try { return Text(type?.FullName, result); }
            catch (Exception error) { MarkIncomplete(result, "A declaring type name could not be read: " + Describe(error, result)); return null; }
        }

        static string SafeMethodName(MethodBase method, ExternalPatchRuntimeSnapshot result)
        {
            try { return Text(method.Name, result); }
            catch (Exception error) { MarkIncomplete(result, "A method name could not be read: " + Describe(error, result)); return null; }
        }

        static int SafeMetadataToken(MethodBase method, ExternalPatchRuntimeSnapshot result)
        {
            try { return method.MetadataToken; }
            catch (Exception error)
            {
                MarkIncomplete(result, "A stable method metadata token could not be read: " + Describe(error, result));
                return 0;
            }
        }

        static string Signature(MethodBase method, ExternalPatchRuntimeSnapshot result)
        {
            try
            {
                // Display work is capped below, but GetParameters() returns its complete CLR
                // ParameterInfo array before this observer can limit the copied signature.
                // External reflection therefore remains synchronous, in-process, and not sandboxed.
                const int maximumSignatureParameters = 8;
                var parameters = method.GetParameters();
                var signature = new System.Text.StringBuilder(Math.Min(MaximumTextLength, 96));
                signature.Append('(');
                var parameterCount = Math.Min(parameters.Length, maximumSignatureParameters);
                for (var index = 0; index < parameterCount; index++)
                {
                    if (index > 0) signature.Append(", ");
                    var parameterType = parameters[index].ParameterType;
                    signature.Append(Text(parameterType?.FullName, result) ?? Text(parameterType?.Name, result));
                    if (signature.Length >= MaximumTextLength - 2)
                    {
                        signature.Length = MaximumTextLength - 2;
                        result.Truncated = true;
                        break;
                    }
                }
                if (parameters.Length > maximumSignatureParameters) result.Truncated = true;
                if (result.Truncated && signature.Length >= MaximumTextLength - 2)
                    signature.Append('…');
                else if (parameters.Length > maximumSignatureParameters)
                    signature.Append(parameterCount == 0 ? "…" : ", …");
                signature.Append(')');
                if (signature.Length > MaximumTextLength)
                {
                    signature.Length = MaximumTextLength - 1;
                    signature.Append('…');
                    result.Truncated = true;
                }
                return signature.ToString();
            }
            catch (Exception error) { MarkIncomplete(result, "A method signature could not be read: " + Describe(error, result)); return "(?)"; }
        }

        static bool Integer(object value, out int? result)
        {
            result = null;
            if (value == null) return true;
            var type = value.GetType();
            if (!type.IsPrimitive && !(value is decimal)) return false;
            if (value is bool || value is char) return false;
            try { result = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture); return true; }
            catch { return false; }
        }

        static bool Contains(string source, string value) => source?.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

        static string Text(object value, ExternalPatchRuntimeSnapshot result = null)
        {
            if (value == null) return null;
            var text = value as string;
            if (text == null && value is Version version) text = version.ToString();
            if (text == null && value is AssemblyName assemblyName) text = assemblyName.Name;
            if (text == null && value is Type type) text = type.FullName ?? type.Name;
            if (text == null && (value.GetType().IsPrimitive || value is decimal))
                text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            if (text == null && value.GetType().IsEnum)
                text = Enum.GetName(value.GetType(), value) ?? "[unknown enum value]";
            if (text == null) text = "[unsupported metadata type: " + (value.GetType().FullName ?? "unknown") + "]";
            if (text.Length <= MaximumTextLength) return text;
            if (result != null) result.Truncated = true;
            return text.Substring(0, MaximumTextLength - 1) + "…";
        }

        static string Describe(Exception error, ExternalPatchRuntimeSnapshot result = null)
        {
            while (error is TargetInvocationException && error.InnerException != null) error = error.InnerException;
            return Text((error?.GetType().Name ?? "Error") + ": " + (error?.Message ?? "Unknown error"), result);
        }

        static void MarkIncomplete(ExternalPatchRuntimeSnapshot result, string value)
        {
            result.Status = "Incomplete";
            AddNote(result, "Inconclusive: " + value);
        }

        static void MarkTruncated(ExternalPatchRuntimeSnapshot result, string value)
        {
            result.Truncated = true;
            MarkIncomplete(result, value);
        }

        static void AddNote(ExternalPatchRuntimeSnapshot result, string value)
        {
            var note = Text(value, result) ?? "Inconclusive: an unspecified diagnostic note was omitted.";
            if (result.Notes.Contains(note)) return;
            if (result.Notes.Count >= MaximumNotes)
            {
                result.Truncated = true;
                return;
            }
            result.Notes.Add(note);
        }
    }
}
