using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CalradiaForge.Core
{
    public sealed partial class ForgeHookService
    {
        const int MaximumCycleIdsInDetail = 8;
        const int MaximumCycleIdCharactersInDetail = 64;
        const int MaximumOrderingFindingsPerHook = 12;
        const int MaximumOrderingFindingCharacters = 768;
        const int MaximumOrderingDiagnosticCharactersPerHook = 2048;

        static Dictionary<string, string> AnalyzeOrdering(Entry[] hooks)
        {
            var findings = new Dictionary<string, HookOrderingFindings>(StringComparer.OrdinalIgnoreCase);
            var byConfigId = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            foreach (Entry hook in hooks)
            {
                findings[hook.Id] = new HookOrderingFindings();
                byConfigId[EffectiveHookId(hook.Id)] = hook;
            }

            foreach (IGrouping<MethodInfo, Entry> targetGroup in hooks.GroupBy(hook => hook.Target))
            {
                var graph = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (Entry hook in targetGroup)
                    graph[EffectiveHookId(hook.Id)] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (Entry hook in targetGroup)
                {
                    string hookId = EffectiveHookId(hook.Id);
                    AddEdges(hook, hook.Before, true, byConfigId, graph, hookId);
                    AddEdges(hook, hook.After, false, byConfigId, graph, hookId);
                }

                foreach (List<string> cycle in FindCycleComponents(graph))
                {
                    string[] displayedNames = cycle.Select(id => byConfigId[id].Id)
                        .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                        .Take(MaximumCycleIdsInDetail)
                        .Select(ShortenCycleId)
                        .ToArray();
                    string names = string.Join(", ", displayedNames);
                    string omitted = cycle.Count > displayedNames.Length
                        ? " (showing " + displayedNames.Length + " of " + cycle.Count + " cycle members)"
                        : string.Empty;
                    foreach (string id in cycle)
                    {
                        Entry hook = byConfigId[id];
                        AddFinding(findings, hook.Id,
                            "Registered ordering declarations contain a cycle among [" + names + "]" + omitted + " for this target. MonoMod documents the resulting relative order as unspecified; this advisory does not report the active backend chain.");
                    }
                }
            }

            // Keep cycle findings ahead of per-reference advisories when the per-hook
            // diagnostic budget is reached; a cycle changes the ordering guarantee.
            foreach (Entry hook in hooks)
            {
                InspectReferences(hook, hook.Before, "Before", byConfigId, findings);
                InspectReferences(hook, hook.After, "After", byConfigId, findings);
            }

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, HookOrderingFindings> pair in findings)
                if (pair.Value.HasFindings) result[pair.Key] = pair.Value.Format();
            return result;
        }

        static string ShortenCycleId(string id) => id.Length <= MaximumCycleIdCharactersInDetail
            ? id
            : id.Substring(0, MaximumCycleIdCharactersInDetail - 3) + "...";

        static void InspectReferences(Entry source, IEnumerable<string> references, string relation,
            Dictionary<string, Entry> byConfigId, Dictionary<string, HookOrderingFindings> findings)
        {
            foreach (string reference in references ?? Enumerable.Empty<string>())
            {
                string effectiveId = EffectiveOrderId(reference);
                if (!byConfigId.TryGetValue(effectiveId, out Entry target))
                {
                    AddFinding(findings, source.Id,
                        relation + " reference '" + reference + "' is not present in the current Forge registry. It may resolve to an external detour with the matching MonoMod ID; its ordering is unverified.");
                    continue;
                }
                if (!source.Target.Equals(target.Target))
                {
                    AddFinding(findings, source.Id,
                        relation + " reference '" + reference + "' resolves to registered hook '" + target.Id + "' on a different target; it cannot order this method's detour chain.");
                }
            }
        }

        static void AddEdges(Entry source, IEnumerable<string> references, bool before,
            Dictionary<string, Entry> byConfigId, Dictionary<string, HashSet<string>> graph, string sourceId)
        {
            foreach (string reference in references ?? Enumerable.Empty<string>())
            {
                string referenceId = EffectiveOrderId(reference);
                if (!byConfigId.TryGetValue(referenceId, out Entry target) || !source.Target.Equals(target.Target)) continue;
                string targetId = EffectiveHookId(target.Id);
                if (before) graph[sourceId].Add(targetId);
                else graph[targetId].Add(sourceId);
            }
        }

        static List<List<string>> FindCycleComponents(Dictionary<string, HashSet<string>> graph)
        {
            // Kosaraju's iterative passes identify strongly connected components
            // without recursion, so large or adversarial registries cannot exhaust
            // the process stack. Separate SCCs keep independent cycles truthful in
            // the per-hook diagnostics, even when a one-way edge joins them.
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var finishOrder = new List<string>(graph.Count);
            foreach (string root in graph.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
            {
                if (visited.Contains(root)) continue;
                var pending = new Stack<OrderVisitFrame>();
                pending.Push(new OrderVisitFrame(root, false));
                while (pending.Count != 0)
                {
                    OrderVisitFrame frame = pending.Pop();
                    if (frame.IsExit)
                    {
                        finishOrder.Add(frame.Id);
                        continue;
                    }

                    if (!visited.Add(frame.Id)) continue;
                    pending.Push(new OrderVisitFrame(frame.Id, true));
                    foreach (string next in graph[frame.Id].OrderByDescending(id => id, StringComparer.OrdinalIgnoreCase))
                        if (!visited.Contains(next)) pending.Push(new OrderVisitFrame(next, false));
                }
            }

            var reverse = graph.Keys.ToDictionary(id => id, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, HashSet<string>> edge in graph)
                foreach (string destination in edge.Value)
                    reverse[destination].Add(edge.Key);

            visited.Clear();
            var cycles = new List<List<string>>();
            for (int index = finishOrder.Count - 1; index >= 0; index--)
            {
                string root = finishOrder[index];
                if (!visited.Add(root)) continue;
                var component = new List<string>();
                var pending = new Stack<string>();
                pending.Push(root);
                while (pending.Count != 0)
                {
                    string current = pending.Pop();
                    component.Add(current);
                    foreach (string prior in reverse[current].OrderByDescending(id => id, StringComparer.OrdinalIgnoreCase))
                        if (visited.Add(prior)) pending.Push(prior);
                }

                if (component.Count > 1 || graph[root].Contains(root))
                    cycles.Add(component);
            }
            return cycles;
        }

        static void AddFinding(Dictionary<string, HookOrderingFindings> findings, string hookId, string detail)
        {
            if (findings.TryGetValue(hookId, out HookOrderingFindings hookFindings)) hookFindings.Add(detail);
        }

        sealed class HookOrderingFindings
        {
            readonly SortedSet<string> details = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            int omitted;

            internal bool HasFindings => details.Count != 0 || omitted != 0;

            internal void Add(string detail)
            {
                string bounded = detail == null || detail.Length <= MaximumOrderingFindingCharacters
                    ? detail ?? string.Empty
                    : detail.Substring(0, MaximumOrderingFindingCharacters - 3) + "...";
                if (details.Contains(bounded)) return;
                if (details.Count >= MaximumOrderingFindingsPerHook)
                {
                    omitted++;
                    return;
                }
                details.Add(bounded);
            }

            internal string Format()
            {
                var visible = new List<string>();
                int visibleCharacters = 0;
                int textOmitted = omitted;
                foreach (string detail in details)
                {
                    int separatorLength = visible.Count == 0 ? 0 : 3;
                    if (visibleCharacters + separatorLength + detail.Length > MaximumOrderingDiagnosticCharactersPerHook - 64)
                    {
                        textOmitted += details.Count - visible.Count;
                        break;
                    }
                    visible.Add(detail);
                    visibleCharacters += separatorLength + detail.Length;
                }

                string text = string.Join(" | ", visible);
                if (textOmitted == 0) return text;
                string suffix = " | " + textOmitted + " additional ordering finding(s) omitted.";
                if (visible.Count == 0) return "Additional ordering findings omitted: " + textOmitted + ".";
                return text + suffix;
            }
        }

        sealed class OrderVisitFrame
        {
            internal OrderVisitFrame(string id, bool isExit) { Id = id; IsExit = isExit; }
            internal string Id { get; }
            internal bool IsExit { get; }
        }
    }
}
