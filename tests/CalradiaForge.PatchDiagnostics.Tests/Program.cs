using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CalradiaForge.Core;
using CalradiaForge.Sdk;
using FakeHarmony = HarmonyLib.Harmony;

static class Program
{
    static int passed;
    static readonly Assembly OfficialRuntimeAssembly = CreateRuntimeAssembly(new Version(0, 0, 0, 1), publicMethods: true, privateMethods: false);

    static int Main()
    {
        Run("missing external runtime remains explicitly not loaded", MissingRuntime);
        Run("not requested is distinct from not loaded", NotRequested);
        Run("reports without external snapshots remain uncaptured", MissingReportSnapshot);
        Run("a same-named type in another assembly is ignored", DecoyAssemblyIsIgnored);
        Run("assembly scan limit distinguishes exact end from additional entries", AssemblyLimitBoundary);
        Run("incomplete assembly scan never claims runtime absence", IncompleteAssemblyScan);
        Run("unsupported loaded runtime is distinguished", UnsupportedRuntime);
        Run("private runtime query methods are unsupported", PrivateRuntimeApi);
        Run("incompatible public query signatures are not invoked", InvalidRuntimeSignatures);
        Run("observed metadata and shared owners are not conflict verdicts", ObservedAndReview);
        Run("empty metadata remains incomplete", EmptyMetadata);
        Run("omitted diagnostic notes explicitly mark output as truncated", NoteCollectionBound);
        Run("throwing getters remain unreadable evidence", ThrowingMetadata);
        Run("missing and malformed metadata remain incomplete", MissingAndMalformedMetadata);
        Run("private metadata members are not reflected", PrivateMetadataMembers);
        Run("wrong-typed target entries prevent a complete claim", WrongTargetEntry);
        Run("Forge registry enumeration failures preserve partial records", ForgeRegistryEnumerationFailure);
        Run("Forge-owned ordering bounds distinguish exact completion from truncation", ForgeOwnedOrderingBounds);
        Run("query and iterator failures remain incomplete", QueryFailures);
        Run("target and displayed target bounds are enforced", TargetAndDisplayBounds);
        Run("owner and patch observations are bounded globally", PatchAndOwnerBounds);
        Run("ordering identifiers have a global bound", OrderingIdentifierBounds);
        Run("inspection only invokes the supplied official assembly", NoDynamicLoading);
        Run("global patch and ordering budgets accept exact-boundary completion", ExactCaptureBounds);
        Run("built-in Forge diagnostics bound snapshots without changing public snapshots", BuiltInRegistryDiagnosticBounds);
        Run("patch diagnostics output remains below the nested IPC budget", SerializedOutputBudget);
        Run("built-in patch diagnostics stop before materializing over-budget records", BuiltInPatchRecordLimit);
        Run("custom registry query seams are explicitly incomplete", CustomRegistryIsBestEffort);
        Run("target ordering is deterministic across same-name assemblies", StableTargetOrdering);
        Run("custom patch query seams are explicitly incomplete", CustomPatchRegistryIsBestEffort);
        Console.WriteLine("Forge patch diagnostics fixture: {0}/30 passed.", passed);
        return passed == 30 ? 0 : 1;
    }

    static void Run(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { Console.Error.WriteLine("FAIL " + name + ": " + error); }
    }

    static ExternalPatchRuntimeSnapshot Inspect(IEnumerable<Assembly> assemblies, int maximumTargets = 200)
        => ForgePatchDiagnostics.Capture(null, null, assemblies, maximumTargets).ExternalRuntime;

    static void MissingRuntime()
    {
        FakeHarmony.Reset(Array.Empty<object>());
        var result = Inspect(Array.Empty<Assembly>());
        Assert(result.Status == "NotLoaded", "A completed scan without the optional runtime should be NotLoaded.");
        Assert(result.Detail.Contains("not found", StringComparison.OrdinalIgnoreCase), "The detail must explain the expected runtime was not found in the supplied scan.");
        Assert(result.Notes.Any(note => note.StartsWith("Origin:", StringComparison.Ordinal)), "Capture provenance is required.");
        Assert(!string.IsNullOrWhiteSpace(result.CapturedAt), "Capture timestamp is required.");
    }

    static void NotRequested()
    {
        var result = ForgePatchDiagnostics.Capture(null, null).ExternalRuntime;
        Assert(result.Status == "NotRequested" && !string.IsNullOrWhiteSpace(result.CapturedAt), "A skipped optional probe must have its own state and timestamp.");
    }

    static void MissingReportSnapshot()
    {
        var report = new SessionReport
        {
            PatchDiagnostics = new ForgePatchDiagnosticsSnapshot { ExternalRuntime = null }
        };
        var external = ReportWorkspace.Normalize(report).PatchDiagnostics.ExternalRuntime;
        Assert(external.Status == "NotCaptured", "A missing snapshot must not claim that a completed scan found no Harmony runtime.");
        Assert(external.Detail.Contains("no external runtime snapshot was captured", StringComparison.OrdinalIgnoreCase), "The fallback must explain that no observation was recorded.");
    }

    static void DecoyAssemblyIsIgnored()
    {
        FakeHarmony.Reset(Array.Empty<object>());
        var result = Inspect(new[] { typeof(HarmonyLib.Harmony).Assembly });
        Assert(result.Status == "NotLoaded", "An unrelated assembly containing HarmonyLib.Harmony must not be treated as Harmony.");
        Assert(FakeHarmony.QueryCalls == 0, "The decoy query method must not execute.");
    }

    static void AssemblyLimitBoundary()
    {
        var decoy = typeof(HarmonyLib.Harmony).Assembly;
        var exact = Inspect(Enumerable.Repeat(decoy, 512));
        Assert(exact.Status == "NotLoaded" && !exact.Truncated, "A full scan ending exactly at the assembly limit must remain complete.");
        var over = Inspect(Enumerable.Repeat(decoy, 513));
        Assert(over.Status == "Incomplete" && over.Truncated, "One lookahead must detect an assembly beyond the scan limit.");
    }

    static void IncompleteAssemblyScan()
    {
        var result = Inspect(new Assembly[] { null, typeof(HarmonyLib.Harmony).Assembly });
        Assert(result.Status == "Incomplete" && !result.Truncated, "A skipped assembly entry must prevent an absence conclusion without claiming output was clipped.");
        Assert(result.Notes.Any(note => note.Contains("null assembly", StringComparison.OrdinalIgnoreCase)), "The skipped assembly must be disclosed.");
    }

    static void UnsupportedRuntime()
    {
        var assembly = CreateUnsupportedRuntimeAssembly();
        var result = Inspect(new[] { assembly });
        Assert(result.Status == "Unsupported", "A loaded type without the public query methods must be unsupported. Actual=" + result.Status + "; assembly=" + assembly.GetName().Name + "; detail=" + result.Detail);
        Assert(result.Detail.Contains("unavailable", StringComparison.OrdinalIgnoreCase), "The unsupported API must be described.");
    }

    static void PrivateRuntimeApi()
    {
        var assembly = CreateRuntimeAssembly(new Version(0, 0, 0, 3), publicMethods: false, privateMethods: true);
        FakeHarmony.Reset(Array.Empty<object>());
        var result = Inspect(new[] { assembly });
        Assert(result.Status == "Unsupported", "Private query methods must not be called as a supported API.");
        Assert(FakeHarmony.QueryCalls == 0, "Private query methods must not be invoked.");
    }

    static void InvalidRuntimeSignatures()
    {
        var assembly = CreateRuntimeAssembly(new Version(0, 0, 0, 4), publicMethods: false, privateMethods: false, invalidMethods: true);
        FakeHarmony.Reset(Array.Empty<object>());
        var result = Inspect(new[] { assembly });
        Assert(result.Status == "Unsupported", "Object-parameter overloads and invalid return types must not be accepted.");
        Assert(FakeHarmony.QueryCalls == 0, "Invalid query signatures must never execute.");
    }

    static void ObservedAndReview()
    {
        var first = Target(nameof(Targets.First));
        var second = Target(nameof(Targets.Second));
        FakeHarmony.Reset(new object[] { first, second });
        FakeHarmony.Metadata[first] = Info(new[] { "owner.alpha" }, Patch("owner.alpha"));
        FakeHarmony.Metadata[second] = Info(new[] { "owner.beta", "owner.gamma" }, Patch("owner.beta"), Patch("owner.gamma"));
        var result = Inspect(new[] { OfficialRuntimeAssembly });
        Assert(result.Status == "Observed" && result.ActiveTargetCount == 2 && result.SharedTargetCount == 1, "Reported metadata should remain an observation. Actual=" + result.Status + "; assembly=" + result.RuntimeAssembly + "; detail=" + result.Detail);
        Assert(result.Notes.Any(note => note.StartsWith("Review:", StringComparison.Ordinal)), "Multiple owners should be categorized as review.");
        Assert(result.Notes.Any(note => note.Contains("does not prove", StringComparison.OrdinalIgnoreCase)), "Shared targets must not be treated as confirmed conflicts.");
    }

    static void EmptyMetadata()
    {
        var target = Target(nameof(Targets.First));
        FakeHarmony.Reset(new object[] { target });
        FakeHarmony.Metadata[target] = null;
        var result = Inspect(new[] { OfficialRuntimeAssembly });
        Assert(result.Status == "Incomplete" && result.EmptyMetadataTargetCount == 1, "Missing metadata must be incomplete, not an empty healthy observation.");
        Assert(result.Targets.Single().MetadataStatus == "No metadata", "The target should remain visible as inconclusive evidence.");
    }

    static void NoteCollectionBound()
    {
        var targets = Enumerable.Range(0, 24).Select(CreateNoteCapTarget).Cast<object>().ToArray();
        FakeHarmony.Reset(targets);

        var result = Inspect(new[] { OfficialRuntimeAssembly });

        Assert(result.Notes.Count == 16, "Diagnostic notes must remain within the existing hard cap.");
        Assert(result.Truncated, "Omitting a unique diagnostic note at the cap must be disclosed as truncation.");
        Assert(result.Status == "Incomplete", "Truncated diagnostics with missing metadata must retain their incomplete status.");
    }

    static void ThrowingMetadata()
    {
        var target = Target(nameof(Targets.First));
        FakeHarmony.Reset(new object[] { target });
        FakeHarmony.Metadata[target] = new ThrowingInfo();
        var result = Inspect(new[] { OfficialRuntimeAssembly });
        Assert(result.Status == "Incomplete" && result.Truncated && result.Targets.Single().MetadataStatus == "Unreadable", "A throwing getter with an oversized diagnostic must remain incomplete and disclose that its output was clipped.");
        Assert(result.Notes.Any(note => note.Contains("…", StringComparison.Ordinal)), "The truncated flag must correspond to an actually clipped diagnostic field.");
        Assert(result.Notes.Count <= 16 && result.Notes.All(note => note == null || note.Length <= 160), "Notes must stay bounded.");
    }

    static void MissingAndMalformedMetadata()
    {
        var target = Target(nameof(Targets.First));
        FakeHarmony.Reset(new object[] { target });
        FakeHarmony.Metadata[target] = new MissingFieldsInfo { Owners = new[] { "owner.alpha" } };
        var result = Inspect(new[] { OfficialRuntimeAssembly });
        Assert(result.Status == "Incomplete" && result.Targets.Single().MetadataStatus == "Unreadable", "A runtime metadata shape missing patch collections must be incomplete.");
        Assert(result.Notes.Any(note => note.Contains("Prefixes", StringComparison.Ordinal)), "The omitted field must be called out.");

        FakeHarmony.Metadata[target] = new FakeInfo { Owners = new[] { "owner.alpha" }, Prefixes = new[] { new FakePatch { owner = "owner.alpha", priority = 1, index = 0, before = Array.Empty<string>(), after = Array.Empty<string>() } }, Postfixes = Array.Empty<FakePatch>(), Transpilers = Array.Empty<FakePatch>(), Finalizers = Array.Empty<FakePatch>() };
        result = Inspect(new[] { OfficialRuntimeAssembly });
        Assert(result.Status == "Incomplete" && result.Targets.Single().MetadataStatus == "Unreadable", "Patch records without an inspectable patch method must be incomplete.");
    }

    static void PrivateMetadataMembers()
    {
        var target = Target(nameof(Targets.First));
        FakeHarmony.Reset(new object[] { target });
        FakeHarmony.Metadata[target] = new PrivateInfo();
        PrivateInfo.GetterCalls = 0;
        var result = Inspect(new[] { OfficialRuntimeAssembly });
        Assert(result.Status == "Incomplete" && result.Targets.Single().MetadataStatus == "Unreadable", "Private metadata members must not be invoked or treated as complete data.");
        Assert(result.Notes.Any(note => note.Contains("omitted the Owners", StringComparison.Ordinal)), "Missing public owner metadata must be disclosed.");
        Assert(PrivateInfo.GetterCalls == 0, "Private metadata getters must not execute.");
    }

    static void WrongTargetEntry()
    {
        FakeHarmony.Reset(new object[] { "not a method" });
        var result = Inspect(new[] { OfficialRuntimeAssembly });
        Assert(result.Status == "Incomplete" && !result.Truncated && result.SkippedTargetCount == 1, "A malformed target must prevent a complete-state claim without claiming output was clipped.");
        Assert(result.Notes.Any(note => note.Contains("not a MethodBase", StringComparison.Ordinal)), "The malformed target shape must be disclosed.");
    }

    static void ForgeRegistryEnumerationFailure()
    {
        var result = ForgePatchDiagnostics.Capture(new ThrowingHookService(), new TestEngine(), Array.Empty<Assembly>());
        Assert(result.Status == "Incomplete" && !result.Truncated && result.HookCount == 1, "A failing registry iterator should preserve partial Forge-owned records without claiming a diagnostic cap was reached.");
        Assert(result.Notes.Any(note => note.Contains("snapshot enumeration stopped", StringComparison.Ordinal)), "The registry enumeration failure must be disclosed.");

        var patches = ForgePatchDiagnostics.Capture(null, new ThrowingPatchService(), Array.Empty<Assembly>());
        Assert(patches.Status == "Incomplete" && !patches.Truncated && patches.PatchCount == 1,
            "A failing patch snapshot iterator should preserve partial records without claiming output was clipped.");
        Assert(patches.Notes.Any(note => note.Contains("snapshot enumeration stopped", StringComparison.Ordinal)),
            "The patch registry enumeration failure must be disclosed.");
    }

    static void ForgeOwnedOrderingBounds()
    {
        var before = Enumerable.Range(0, 16).Select(index => "before." + index).ToArray();
        var after = Enumerable.Range(0, 16).Select(index => "after." + index).ToArray();
        var exact = CreateHooksWithOrderingIds(32, before, after);
        var exactResult = ForgePatchDiagnostics.Capture(exact, null, Array.Empty<Assembly>());
        Assert(exactResult.Hooks.Sum(hook => hook.Before.Count + hook.After.Count) == 1024 && !exactResult.Truncated,
            "Exactly filling the aggregate Forge hook ordering-ID budget must not mark the output truncated.");

        var over = CreateHooksWithOrderingIds(33, before, after);
        var overResult = ForgePatchDiagnostics.Capture(over, null, Array.Empty<Assembly>());
        Assert(overResult.Hooks.Sum(hook => hook.Before.Count + hook.After.Count) == 1024 && overResult.Truncated,
            "Omitting ordering IDs beyond the aggregate Forge hook budget must be reported as truncation.");
    }

    static ForgeHookService CreateHooksWithOrderingIds(int hookCount, string[] before, string[] after)
    {
        var hooks = new ForgeHookService(() => true);
        for (var index = 0; index < hookCount; index++)
        {
            hooks.Register(new ForgeHookDefinition
            {
                Id = "ordering.hook." + index,
                Owner = "fixture",
                Target = (MethodInfo)Target(nameof(Targets.First)),
                Prefix = _ => { },
                Before = before.ToList(),
                After = after.ToList()
            });
        }
        return hooks;
    }

    static void QueryFailures()
    {
        FakeHarmony.Reset(Array.Empty<object>());
        FakeHarmony.ThrowQuery = true;
        var query = Inspect(new[] { OfficialRuntimeAssembly });
        Assert(query.Status == "Incomplete" && !query.Truncated, "A failed query must remain incomplete without claiming output was clipped.");

        FakeHarmony.Reset(new ThrowingEnumerable(Target(nameof(Targets.First))));
        FakeHarmony.Metadata[Target(nameof(Targets.First))] = Info(new[] { "owner.alpha" }, Patch("owner.alpha"));
        var iterator = Inspect(new[] { OfficialRuntimeAssembly });
        Assert(iterator.Status == "Incomplete" && !iterator.Truncated && iterator.DiscoveredTargetCount == 1, "Partial target evidence must survive iterator failure as incomplete without claiming output was clipped.");
    }

    static void TargetAndDisplayBounds()
    {
        var target = Target(nameof(Targets.First));
        FakeHarmony.Reset(Enumerable.Repeat((object)target, 2100).ToArray());
        FakeHarmony.Metadata[target] = Info(new[] { "owner.alpha" }, Patch("owner.alpha"));
        var result = Inspect(new[] { OfficialRuntimeAssembly }, 1);
        Assert(result.DiscoveredTargetCount == 2000 && result.DisplayedTargetCount == 1, "Discovery and display bounds should be distinct.");
        Assert(result.Status == "Incomplete" && result.Truncated, "Bounded output must disclose truncation.");

        FakeHarmony.Reset(Enumerable.Repeat((object)target, 2000).ToArray());
        FakeHarmony.Metadata[target] = Info(new[] { "owner.alpha" }, Patch("owner.alpha"));
        var exact = Inspect(new[] { OfficialRuntimeAssembly }, 500);
        Assert(exact.DiscoveredTargetCount == 2000 && !exact.Notes.Any(note => note.Contains("continued beyond the 2000 target safety limit", StringComparison.Ordinal)), "An exact target-count limit must not claim additional hidden entries.");

        FakeHarmony.Reset(Enumerable.Repeat((object)target, 2001).ToArray());
        FakeHarmony.Metadata[target] = Info(new[] { "owner.alpha" }, Patch("owner.alpha"));
        var over = Inspect(new[] { OfficialRuntimeAssembly }, 500);
        Assert(over.DiscoveredTargetCount == 2000 && over.Notes.Any(note => note.Contains("continued beyond the 2000 target safety limit", StringComparison.Ordinal)), "One lookahead must disclose a target beyond the discovery limit.");
    }

    static void PatchAndOwnerBounds()
    {
        var target = Target(nameof(Targets.First));
        FakeHarmony.Reset(new object[] { target });
        var owners = Enumerable.Range(0, 64).Select(index => "owner." + index).ToArray();
        var patches = Enumerable.Range(0, 160).Select(index => Patch("owner." + index)).ToArray();
        FakeHarmony.Metadata[target] = Info(owners, patches);
        var result = Inspect(new[] { OfficialRuntimeAssembly });
        Assert(result.Targets.Single().Owners.Count == 8, "Owner output must be bounded per target.");
        Assert(result.Targets.Single().Patches.Count == 16, "Patch output must be bounded per target.");
        Assert(result.Status == "Incomplete" && result.Truncated, "Collection truncation must remain explicit.");
    }

    static void OrderingIdentifierBounds()
    {
        var target = Target(nameof(Targets.First));
        var ids = Enumerable.Range(0, 32).Select(index => "owner." + index).ToArray();
        var patch = Patch("owner.alpha");
        patch.before = ids;
        patch.after = ids;
        var repeated = Enumerable.Repeat(patch, 128).ToArray();
        var info = new FakeInfo { Owners = new[] { "owner.alpha" }, Prefixes = repeated, Postfixes = repeated, Transpilers = repeated, Finalizers = repeated };
        FakeHarmony.Reset(Enumerable.Repeat((object)target, 80).ToArray());
        FakeHarmony.Metadata[target] = info;
        var result = Inspect(new[] { OfficialRuntimeAssembly });
        var observations = result.Targets.SelectMany(item => item.Patches).ToArray();
        var orderingIds = observations.Sum(item => item.Before.Count + item.After.Count);
        Assert(observations.Length == 128, "Patch observations must stop at the explicit global limit.");
        Assert(orderingIds == 512, "Ordering IDs must stop at the explicit aggregate limit before materializing more values.");
        Assert(result.Status == "Incomplete" && result.Truncated, "Global limits must downgrade evidence.");
    }

    static void NoDynamicLoading()
    {
        FakeHarmony.Reset(Array.Empty<object>());
        var loaded = new List<Assembly>();
        AssemblyLoadEventHandler handler = (_, eventArgs) => loaded.Add(eventArgs.LoadedAssembly);
        AppDomain.CurrentDomain.AssemblyLoad += handler;
        ExternalPatchRuntimeSnapshot result;
        try { result = Inspect(new[] { OfficialRuntimeAssembly }); }
        finally { AppDomain.CurrentDomain.AssemblyLoad -= handler; }
        Assert(result.Status == "Observed" && FakeHarmony.QueryCalls > 0, "The already-supplied test runtime should be observed through its public query methods.");
        Assert(loaded.Count == 0, "Inspecting the supplied loaded runtime must not load any assemblies.");
    }

    static void ExactCaptureBounds()
    {
        var target = Target(nameof(Targets.First));
        var ids = Enumerable.Range(0, 4).Select(index => "owner." + index).ToArray();
        var patch = Patch("owner.alpha");
        patch.before = ids;
        var repeated = Enumerable.Repeat(patch, 16).ToArray();
        var second = Target(nameof(Targets.Second));
        FakeHarmony.Reset(new object[] { target, second });
        FakeHarmony.Metadata[target] = new FakeInfo { Owners = new[] { "owner.alpha" }, Prefixes = repeated, Postfixes = repeated, Transpilers = repeated, Finalizers = repeated };
        FakeHarmony.Metadata[second] = new FakeInfo { Owners = new[] { "owner.alpha" }, Prefixes = repeated, Postfixes = repeated, Transpilers = repeated, Finalizers = repeated };
        var result = Inspect(new[] { OfficialRuntimeAssembly }, 8);
        var observations = result.Targets.SelectMany(row => row.Patches).ToArray();
        Assert(observations.Length == 128 && observations.Sum(item => item.Before.Count + item.After.Count) == 512, "Both global limits must accept exact completion without truncation.");
        Assert(result.Status == "Observed" && !result.Truncated, "Exact global limits must not be marked incomplete when a bounded lookahead confirms the end.");
    }

    static void BuiltInRegistryDiagnosticBounds()
    {
        var hooks = new ForgeHookService(() => true);
        var before = Enumerable.Range(0, 20).Select(index => "before.dependency." + index).ToList();
        var after = Enumerable.Range(0, 20).Select(index => "after.dependency." + index).ToList();
        for (var index = 0; index < 130; index++)
        {
            hooks.Register(new ForgeHookDefinition
            {
                Id = "diagnostic.hook." + index,
                Owner = "fixture",
                Target = (MethodInfo)Target(nameof(Targets.First)),
                Prefix = _ => { },
                Before = before,
                After = after
            });
        }

        var publicSnapshots = hooks.GetSnapshots();
        Assert(publicSnapshots.Count == 130 && publicSnapshots[0].Before.Count == 20 && publicSnapshots[0].After.Count == 20,
            "The public snapshot API must continue returning complete order metadata.");

        var diagnostics = ForgePatchDiagnostics.Capture(hooks, null, Array.Empty<Assembly>());
        Assert(diagnostics.HookCount == 128 && diagnostics.Status == "Incomplete" && diagnostics.Truncated,
            "The diagnostic path must cap record snapshots and explicitly report omissions.");
        Assert(diagnostics.Hooks.Sum(hook => hook.Before.Count + hook.After.Count) <= 1024,
            "Hook ordering output must respect one aggregate budget across the capture.");
        Assert(diagnostics.Hooks.All(hook => hook.TargetMethod.Length <= 256), "Diagnostic target identities must be bounded before snapshot creation.");

        var identity = typeof(ForgeHookService).GetMethod("DiagnosticIdentity", BindingFlags.Static | BindingFlags.NonPublic);
        var identityArguments = new object[] { Target(nameof(Targets.ManyParameters)), false };
        var signature = identity.Invoke(null, identityArguments) as string;
        Assert(signature.Length <= 256 && signature.Contains("#", StringComparison.Ordinal) && !signature.Contains("(", StringComparison.Ordinal),
            "Diagnostic method identity must avoid materializing a full parameter signature and retain a bounded overload token.");
    }

    static void SerializedOutputBudget()
    {
        var target = Target(nameof(Targets.First));
        var longText = new string('\u0001', 1024);
        var owners = Enumerable.Repeat(longText, 32).ToArray();
        var patch = Patch(longText);
        patch.before = Enumerable.Repeat(longText, 32).ToArray();
        patch.after = Enumerable.Repeat(longText, 32).ToArray();
        var patches = Enumerable.Repeat(patch, 32).ToArray();
        FakeHarmony.Reset(Enumerable.Repeat((object)target, 128).ToArray());
        FakeHarmony.Metadata[target] = new FakeInfo
        {
            Owners = owners,
            Prefixes = patches,
            Postfixes = Array.Empty<FakePatch>(),
            Transpilers = Array.Empty<FakePatch>(),
            Finalizers = Array.Empty<FakePatch>()
        };

        var hooks = new ForgeHookService(() => true);
        var before = Enumerable.Range(0, 20).Select(index => new string('b', 120) + index).ToList();
        var after = Enumerable.Range(0, 20).Select(index => new string('a', 120) + index).ToList();
        for (var index = 0; index < 128; index++)
        {
            hooks.Register(new ForgeHookDefinition
            {
                Id = "serialized.budget." + index,
                Owner = "fixture",
                Target = (MethodInfo)target,
                Prefix = _ => { },
                Before = before,
                After = after
            });
        }

        var captured = ForgePatchDiagnostics.Capture(hooks, null, new[] { OfficialRuntimeAssembly }, 500);
        var external = captured.ExternalRuntime;
        Assert(external.Status == "Incomplete" && external.Truncated, "Clipped external fields and collections must downgrade the observation.");
        Assert(captured.HookCount == 128 && captured.Truncated, "The combined Forge and external capture should retain both explicit safety states.");
        Assert(external.Targets.Count <= 64 && external.Targets.SelectMany(row => row.Patches).Count() <= 128,
            "External target and patch output must obey the tightened hard caps.");
        Assert(external.Targets.All(row => row.Owners.All(owner => owner.Length <= 160)
            && row.Patches.All(item => item.Owner.Length <= 160
                && item.Before.Concat(item.After).All(value => value.Length <= 160))),
            "External owner and ordering strings must be individually bounded.");

        var innerJson = Json.Serialize(captured);
        var nestedIpcJson = Json.Serialize(new Response { Id = "fixture", Success = true, Data = innerJson });
        Assert(nestedIpcJson.Length < 8 * 1024 * 1024,
            "Worst-case nested diagnostic output should remain comfortably below the 32 Mi-character IPC cap. Length=" + nestedIpcJson.Length);
    }

    static void BuiltInPatchRecordLimit()
    {
        var patchServiceType = typeof(ForgePatchDiagnostics).Assembly.GetType("CalradiaForge.Core.ForgePatchService", true);
        var instance = Activator.CreateInstance(patchServiceType, true);
        var entryType = patchServiceType.GetNestedType("PatchEntry", BindingFlags.NonPublic);
        var entry = Activator.CreateInstance(entryType, true);
        entryType.GetField("Id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(entry, "over.budget");
        entryType.GetField("Owner", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(entry, "fixture");
        entryType.GetField("State", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(entry, ForgePatchState.Failed);
        // A null target would make Refresh fail if diagnostics tried to inspect a row beyond
        // a zero-record budget. The capped path should identify omission before snapshotting.
        var entriesField = patchServiceType.GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);
        var entries = (IDictionary)entriesField.GetValue(instance);
        entries.Add("over.budget", entry);
        var order = (IList)patchServiceType.GetField("applyOrder", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
        order.Add("over.budget");

        var bounded = patchServiceType.GetMethod("GetDiagnosticSnapshots", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { 0, 256, false };
        var snapshots = bounded.Invoke(instance, arguments) as IReadOnlyList<ForgePatchSnapshot>;
        Assert(snapshots != null && snapshots.Count == 0 && (bool)arguments[2],
            "The built-in patch diagnostic path must report records beyond the budget without materializing them.");
    }

    static void CustomRegistryIsBestEffort()
    {
        var hooks = new FixedHookService(new ForgeHookSnapshot("custom.one", "fixture", "Fixture.Target", true, false,
            null, null, null, ForgeHookState.Registered, "custom fixture"));
        var result = ForgePatchDiagnostics.Capture(hooks, null, Array.Empty<Assembly>());
        Assert(hooks.SnapshotCalls == 1 && result.HookCount == 1 && result.Status == "Incomplete",
            "A custom registry may still contribute a bounded copy, but must not be presented as a fully bounded observation.");
        Assert(result.Notes.Any(note => note.Contains("custom Forge hook registry", StringComparison.OrdinalIgnoreCase)
            && note.Contains("could not be bounded", StringComparison.OrdinalIgnoreCase)),
            "The synchronous third-party snapshot seam must be named as an explicit limitation.");
    }

    static void CustomPatchRegistryIsBestEffort()
    {
        var patches = new FixedPatchService(new ForgePatchSnapshot("custom.patch", "fixture", "Target.Method#06000001",
            "Replacement.Method#06000002", ForgePatchState.Failed, false));
        var result = ForgePatchDiagnostics.Capture(null, patches, Array.Empty<Assembly>());
        Assert(patches.SnapshotCalls == 1 && result.PatchCount == 1 && result.Status == "Incomplete",
            "A custom patch registry may contribute a bounded copy, but cannot be reported as fully bounded.");
        Assert(result.Notes.Any(note => note.Contains("custom Forge patch registry", StringComparison.OrdinalIgnoreCase)
            && note.Contains("could not be bounded", StringComparison.OrdinalIgnoreCase)),
            "The custom patch snapshot seam must be explicit.");
    }

    static void StableTargetOrdering()
    {
        var zebra = CreateDuplicateTarget("SortFixture.Zebra");
        var alpha = CreateDuplicateTarget("SortFixture.Alpha");
        FakeHarmony.Reset(new object[] { zebra, alpha });
        FakeHarmony.Metadata[zebra] = Info(new[] { "owner.zebra" }, Patch("owner.zebra"));
        FakeHarmony.Metadata[alpha] = Info(new[] { "owner.alpha" }, Patch("owner.alpha"));

        var result = Inspect(new[] { OfficialRuntimeAssembly });
        var assemblyNames = result.Targets.Select(row => row.Assembly).ToArray();
        Assert(assemblyNames.SequenceEqual(assemblyNames.OrderBy(name => name, StringComparer.Ordinal)),
            "Same-name target types from different assemblies must have a deterministic assembly-name tie-breaker.");
        Assert(result.Targets.All(row => row.Signature.Length <= 160), "Stable ordering must not require unbounded display signatures.");
    }

    static Assembly CreateUnsupportedRuntimeAssembly()
    {
        return CreateRuntimeAssembly(new Version(0, 0, 0, 2), publicMethods: false, privateMethods: false);
    }

    static MethodInfo CreateDuplicateTarget(string assemblyName)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule(assemblyName);
        var type = module.DefineType("SortFixture.SharedTarget", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
        var method = type.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
        method.GetILGenerator().Emit(OpCodes.Ret);
        return type.CreateType().GetMethod("Invoke", BindingFlags.Public | BindingFlags.Static);
    }

    static MethodInfo CreateNoteCapTarget(int index)
    {
        var assemblyName = "NoteCapFixture" + index;
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule(assemblyName);
        var type = module.DefineType("NoteCapFixture.Target" + index, TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
        var method = type.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
        method.GetILGenerator().Emit(OpCodes.Ret);
        return type.CreateType().GetMethod("Invoke", BindingFlags.Public | BindingFlags.Static);
    }

    static Assembly CreateRuntimeAssembly(Version version, bool publicMethods, bool privateMethods, bool invalidMethods = false)
    {
        var name = new AssemblyName("0Harmony") { Version = version };
        var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule(name.Name);
        var type = module.DefineType("HarmonyLib.Harmony", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
        var attributes = publicMethods ? MethodAttributes.Public | MethodAttributes.Static : privateMethods ? MethodAttributes.Private | MethodAttributes.Static : (MethodAttributes)0;
        if (publicMethods || privateMethods)
        {
            DefineForwarder(type, "GetAllPatchedMethods", attributes, typeof(IEnumerable), Type.EmptyTypes, typeof(FakeHarmony).GetMethod(nameof(FakeHarmony.GetAllPatchedMethods)));
            DefineForwarder(type, "GetPatchInfo", attributes, typeof(object), new[] { typeof(MethodBase) }, typeof(FakeHarmony).GetMethod(nameof(FakeHarmony.GetPatchInfo)));
        }
        if (invalidMethods)
        {
            DefineStub(type, "GetAllPatchedMethods", MethodAttributes.Public | MethodAttributes.Static, typeof(object), Type.EmptyTypes);
            DefineStub(type, "GetPatchInfo", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] { typeof(object) });
        }
        type.CreateType();
        return assembly;
    }

    static void DefineStub(TypeBuilder type, string name, MethodAttributes attributes, Type returnType, Type[] parameters)
    {
        var method = type.DefineMethod(name, attributes, returnType, parameters);
        var il = method.GetILGenerator();
        if (returnType != typeof(void))
        {
            if (returnType.IsValueType) il.Emit(OpCodes.Initobj, returnType);
            else il.Emit(OpCodes.Ldnull);
        }
        il.Emit(OpCodes.Ret);
    }

    static void DefineForwarder(TypeBuilder type, string name, MethodAttributes attributes, Type returnType, Type[] parameters, MethodInfo target)
    {
        var method = type.DefineMethod(name, attributes, returnType, parameters);
        var il = method.GetILGenerator();
        if (parameters.Length > 0) il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, target);
        il.Emit(OpCodes.Ret);
    }

    static MethodBase Target(string name) => typeof(Targets).GetMethod(name, BindingFlags.Public | BindingFlags.Static);
    static object Info(IEnumerable<string> owners, params FakePatch[] patches) => new FakeInfo { Owners = owners.ToArray(), Prefixes = patches };
    static FakePatch Patch(string owner) => new FakePatch { owner = owner, priority = 400, index = 0, before = Array.Empty<string>(), after = Array.Empty<string>(), PatchMethod = Target(nameof(Targets.Hook)) };

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static class Targets
    {
        public static void First() { }
        public static void Second() { }
        public static void Hook() { }
        public static void ManyParameters(int p0, int p1, int p2, int p3, int p4, int p5, int p6, int p7,
            int p8, int p9, int p10, int p11, int p12, int p13, int p14, int p15) { }
    }

    public sealed class FakeInfo
    {
        public IEnumerable<string> Owners { get; set; }
        public IEnumerable<FakePatch> Prefixes { get; set; }
        public IEnumerable<FakePatch> Postfixes { get; set; }
        public IEnumerable<FakePatch> Transpilers { get; set; }
        public IEnumerable<FakePatch> Finalizers { get; set; }
    }

    public sealed class ThrowingInfo
    {
        public string[] Owners => throw new InvalidOperationException(new string('x', 900));
        public FakePatch[] Prefixes => Array.Empty<FakePatch>();
        public FakePatch[] Postfixes => Array.Empty<FakePatch>();
        public FakePatch[] Transpilers => Array.Empty<FakePatch>();
        public FakePatch[] Finalizers => Array.Empty<FakePatch>();
    }

    public sealed class MissingFieldsInfo
    {
        public string[] Owners { get; set; }
    }

    public sealed class PrivateInfo
    {
        public static int GetterCalls { get; set; }
        private string[] Owners { get { GetterCalls++; return new[] { "private.owner" }; } }
        private FakePatch[] Prefixes { get { GetterCalls++; return Array.Empty<FakePatch>(); } }
        private FakePatch[] Postfixes { get { GetterCalls++; return Array.Empty<FakePatch>(); } }
        private FakePatch[] Transpilers { get { GetterCalls++; return Array.Empty<FakePatch>(); } }
        private FakePatch[] Finalizers { get { GetterCalls++; return Array.Empty<FakePatch>(); } }
    }

    public sealed class FakePatch
    {
        public string owner { get; set; }
        public int priority { get; set; }
        public int index { get; set; }
        public string[] before { get; set; }
        public string[] after { get; set; }
        public MethodBase PatchMethod { get; set; }
    }

    sealed class ThrowingEnumerable : IEnumerable
    {
        readonly object first;
        internal ThrowingEnumerable(object first) => this.first = first;
        public IEnumerator GetEnumerator() => new ThrowingIterator(first);
    }

    sealed class ThrowingIterator : IEnumerator, IDisposable
    {
        readonly object first;
        int state;
        internal ThrowingIterator(object first) => this.first = first;
        public object Current => state == 1 ? first : throw new InvalidOperationException("current unavailable");
        public bool MoveNext() { if (state++ == 0) return true; throw new InvalidOperationException("iterator stopped"); }
        public void Reset() => throw new NotSupportedException();
        public void Dispose() { }
    }

    sealed class ThrowingHookService : IForgeHookService
    {
        public IForgeHookHandle Register(ForgeHookDefinition definition) => throw new NotSupportedException();
        public IReadOnlyList<ForgeHookSnapshot> GetSnapshots(string owner = null) => new ThrowingHookSnapshots();
        public ForgeHookOperationResult Apply(string hookId) => throw new NotSupportedException();
        public ForgeHookOperationResult Verify(string hookId) => throw new NotSupportedException();
        public ForgeHookOperationResult Revert(string hookId) => throw new NotSupportedException();
        public IReadOnlyList<ForgeHookOperationResult> RevertOwner(string owner) => throw new NotSupportedException();
        public IReadOnlyList<ForgeHookOperationResult> RevertAll() => throw new NotSupportedException();
        public void Disconnect() => throw new NotSupportedException();
    }

    sealed class ThrowingPatchService : IForgePatchService
    {
        public IForgePatchHandle ApplyMethodReplacement(string patchId, string owner, MethodInfo target, MethodInfo replacement) => throw new NotSupportedException();
        public IReadOnlyList<ForgePatchSnapshot> GetSnapshots(string owner = null) => new ThrowingPatchSnapshots();
        public ForgePatchVerification Verify(string patchId) => throw new NotSupportedException();
        public ForgePatchRevertResult Revert(string patchId) => throw new NotSupportedException();
        public IReadOnlyList<ForgePatchRevertResult> RevertOwner(string owner) => throw new NotSupportedException();
        public IReadOnlyList<ForgePatchRevertResult> RevertAll() => throw new NotSupportedException();
        public void Disconnect() => throw new NotSupportedException();
    }

    sealed class FixedHookService : IForgeHookService
    {
        readonly IReadOnlyList<ForgeHookSnapshot> snapshots;
        internal int SnapshotCalls { get; private set; }
        internal FixedHookService(params ForgeHookSnapshot[] snapshots) => this.snapshots = snapshots;
        public IForgeHookHandle Register(ForgeHookDefinition definition) => throw new NotSupportedException();
        public IReadOnlyList<ForgeHookSnapshot> GetSnapshots(string owner = null) { SnapshotCalls++; return snapshots; }
        public ForgeHookOperationResult Apply(string hookId) => throw new NotSupportedException();
        public ForgeHookOperationResult Verify(string hookId) => throw new NotSupportedException();
        public ForgeHookOperationResult Revert(string hookId) => throw new NotSupportedException();
        public IReadOnlyList<ForgeHookOperationResult> RevertOwner(string owner) => throw new NotSupportedException();
        public IReadOnlyList<ForgeHookOperationResult> RevertAll() => throw new NotSupportedException();
        public void Disconnect() => throw new NotSupportedException();
    }

    sealed class FixedPatchService : IForgePatchService
    {
        readonly IReadOnlyList<ForgePatchSnapshot> snapshots;
        internal int SnapshotCalls { get; private set; }
        internal FixedPatchService(params ForgePatchSnapshot[] snapshots) => this.snapshots = snapshots;
        public IForgePatchHandle ApplyMethodReplacement(string patchId, string owner, MethodInfo target, MethodInfo replacement) => throw new NotSupportedException();
        public IReadOnlyList<ForgePatchSnapshot> GetSnapshots(string owner = null) { SnapshotCalls++; return snapshots; }
        public ForgePatchVerification Verify(string patchId) => throw new NotSupportedException();
        public ForgePatchRevertResult Revert(string patchId) => throw new NotSupportedException();
        public IReadOnlyList<ForgePatchRevertResult> RevertOwner(string owner) => throw new NotSupportedException();
        public IReadOnlyList<ForgePatchRevertResult> RevertAll() => throw new NotSupportedException();
        public void Disconnect() => throw new NotSupportedException();
    }

    sealed class ThrowingHookSnapshots : IReadOnlyList<ForgeHookSnapshot>
    {
        public int Count => 2;
        public ForgeHookSnapshot this[int index] => throw new NotSupportedException();
        public IEnumerator<ForgeHookSnapshot> GetEnumerator()
        {
            yield return new ForgeHookSnapshot("partial", "fixture", "Fixture.Target", true, false, null, null, null, ForgeHookState.Registered, "captured");
            throw new InvalidOperationException("fixture iterator stopped");
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    sealed class ThrowingPatchSnapshots : IReadOnlyList<ForgePatchSnapshot>
    {
        public int Count => 2;
        public ForgePatchSnapshot this[int index] => throw new NotSupportedException();
        public IEnumerator<ForgePatchSnapshot> GetEnumerator()
        {
            yield return new ForgePatchSnapshot("partial.patch", "fixture", "Fixture.Target", "Fixture.Replacement", ForgePatchState.Applied, true);
            throw new InvalidOperationException("fixture patch iterator stopped");
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

namespace HarmonyLib
{
    public static class Harmony
    {
        public static object Targets { get; set; }
        public static bool ThrowQuery { get; set; }
        public static int QueryCalls { get; private set; }
        public static Dictionary<MethodBase, object> Metadata { get; } = new Dictionary<MethodBase, object>();

        public static IEnumerable GetAllPatchedMethods()
        {
            QueryCalls++;
            if (ThrowQuery) throw new InvalidOperationException("fake query failed");
            return Targets as IEnumerable;
        }

        public static object GetPatchInfo(MethodBase target)
        {
            QueryCalls++;
            if (!Metadata.TryGetValue(target, out var result)) return null;
            if (ThrowQuery) throw new InvalidOperationException("fake metadata failed");
            return result;
        }

        public static void Reset(object targets)
        {
            Targets = targets;
            ThrowQuery = false;
            QueryCalls = 0;
            Metadata.Clear();
        }
    }
}
