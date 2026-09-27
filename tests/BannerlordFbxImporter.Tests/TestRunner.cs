using System.Diagnostics;
using System.IO;
using System.Text;
using BannerlordFbxImporter;
using BannerlordFbxImporter.Automation;
using BannerlordFbxImporter.Preflight;

var suite = new ImporterTests();
try
{
    suite.Run();
    Console.WriteLine($"PASS: {suite.Assertions} assertions across bounded scan, preflight, CLI, destination, and UI-stop fixtures.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FAIL after {suite.Assertions} assertions: {ex.Message}");
    return 1;
}

sealed class ImporterTests
{
    public int Assertions { get; private set; }

    public void Run()
    {
        TestScanCountsAndRecursion();
        TestAssetFileLease();
        TestScanRejectsUnsafeBatches();
        TestScanSkipsReparsePoints();
        TestPreflightIsAdvisory();
        TestDestinationResolution();
        TestCliAndConfirmation();
        TestDryRunAndModalStop();
        TestDialogCloseGate();
    }

    private void TestScanCountsAndRecursion()
    {
        using var temp = new TempTree();
        string empty = temp.Directory("empty");
        Equal(0, AssetBatchScanner.Scan(empty).Files.Count, "zero files");

        string one = temp.Directory("one");
        WriteFbx(Path.Combine(one, "a.fbx"));
        Equal(1, AssetBatchScanner.Scan(one).Files.Count, "one file");

        string hundred = temp.Directory("hundred");
        for (int index = 0; index < 100; index++) WriteFbx(Path.Combine(hundred, $"m_{index:D3}.fbx"));
        Equal(100, AssetBatchScanner.Scan(hundred).Files.Count, "100 file limit");

        WriteFbx(Path.Combine(hundred, "m_100.fbx"));
        Throws<InvalidOperationException>(() => AssetBatchScanner.Scan(hundred), "101 file limit");

        string recursive = temp.Directory("recursive");
        string nested = Directory.CreateDirectory(Path.Combine(recursive, "armors", "mail" )).FullName;
        WriteFbx(Path.Combine(nested, "cota_niño ñ.fbx"));
        File.WriteAllText(Path.Combine(nested, "texture.png"), "ignored");
        AssetBatch nestedBatch = AssetBatchScanner.Scan(recursive);
        Equal(1, nestedBatch.Files.Count, "recursive FBX");
        True(nestedBatch.Files[0].FullPath.Contains("ñ", StringComparison.Ordinal), "Unicode path preserved");
        True(!AssetBatchScanner.IsSupportedFbx("x.png"), "PNG is rejected as a candidate type");
        Throws<InvalidOperationException>(() => AssetBatchScanner.Scan(recursive,
            new AssetScanLimits(MaximumFiles: 100, MaximumDepth: 0, MaximumDirectories: 10,
                MaximumFileBytes: 1024, MaximumBatchBytes: 1024)), "depth bound");
        Throws<InvalidOperationException>(() => AssetBatchScanner.Scan(recursive,
            new AssetScanLimits(MaximumFiles: 100, MaximumDepth: 32, MaximumDirectories: 1,
                MaximumFileBytes: 1024, MaximumBatchBytes: 1024)), "directory bound");

        string manyEntries = temp.Directory("many-entries");
        for (int index = 0; index < 6; index++) File.WriteAllText(Path.Combine(manyEntries, $"ignored_{index}.txt"), "x");
        Throws<InvalidOperationException>(() => AssetBatchScanner.Scan(manyEntries,
            new AssetScanLimits(MaximumFiles: 100, MaximumDepth: 32, MaximumDirectories: 10,
                MaximumFileBytes: 1024, MaximumBatchBytes: 1024, MaximumEntries: 5)), "total filesystem entry bound");
    }

    private void TestScanRejectsUnsafeBatches()
    {
        using var temp = new TempTree();
        string missing = Path.Combine(temp.Root, "not-there");
        Throws<DirectoryNotFoundException>(() => AssetBatchScanner.Scan(missing), "missing root");

        string duplicateRoot = temp.Directory("duplicates");
        WriteFbx(Path.Combine(Directory.CreateDirectory(Path.Combine(duplicateRoot, "one")).FullName, "Armor.FBX"));
        WriteFbx(Path.Combine(Directory.CreateDirectory(Path.Combine(duplicateRoot, "two")).FullName, "armor.fbx"));
        Throws<InvalidOperationException>(() => AssetBatchScanner.Scan(duplicateRoot), "case-insensitive duplicate basenames");

        string sizeRoot = temp.Directory("size");
        File.WriteAllBytes(Path.Combine(sizeRoot, "large.fbx"), new byte[8]);
        Throws<InvalidOperationException>(() => AssetBatchScanner.Scan(sizeRoot,
            new AssetScanLimits(MaximumFiles: 100, MaximumDepth: 32, MaximumDirectories: 10,
                MaximumFileBytes: 7, MaximumBatchBytes: 20)), "per-file byte limit");

        string totalRoot = temp.Directory("total");
        File.WriteAllBytes(Path.Combine(totalRoot, "a.fbx"), new byte[4]);
        File.WriteAllBytes(Path.Combine(totalRoot, "b.fbx"), new byte[4]);
        Throws<InvalidOperationException>(() => AssetBatchScanner.Scan(totalRoot,
            new AssetScanLimits(MaximumFiles: 100, MaximumDepth: 32, MaximumDirectories: 10,
                MaximumFileBytes: 8, MaximumBatchBytes: 7)), "batch byte limit");
    }

    private void TestAssetFileLease()
    {
        using var temp = new TempTree();
        string root = temp.Directory("lease");
        string path = Path.Combine(root, "reviewed.fbx");
        WriteFbx(path);
        AssetFile scanned = AssetBatchScanner.Scan(root).Files.Single();

        using (AssetFileLease.OpenForSubmission(scanned))
            Throws<IOException>(() => File.WriteAllText(path, "replacement content"), "open submission lease blocks writes");

        File.WriteAllText(path, "replacement content");
        Throws<InvalidOperationException>(() => AssetFileLease.OpenForSubmission(scanned),
            "changed file is rejected before UI submission");
    }

    private void TestScanSkipsReparsePoints()
    {
        using var temp = new TempTree();
        string outside = temp.Directory("outside");
        WriteFbx(Path.Combine(outside, "must_not_be_followed.fbx"));
        string source = temp.Directory("source");
        string junction = Path.Combine(source, "linked-assets");
        using var cmd = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{junction}\" \"{outside}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });
        if (cmd is null) throw new InvalidOperationException("Could not create reparse-point fixture.");
        cmd.WaitForExit(5000);
        if (cmd.ExitCode != 0)
            throw new InvalidOperationException("Windows junction fixture was not created: " + cmd.StandardError.ReadToEnd());

        try
        {
            AssetBatch result = AssetBatchScanner.Scan(source);
            Equal(0, result.Files.Count, "junction target was not traversed");
            Equal(1, result.Warnings.Count, "junction skip reported");
        }
        finally
        {
            try { Directory.Delete(junction, recursive: false); }
            catch { }
        }
    }

    private void TestPreflightIsAdvisory()
    {
        using var temp = new TempTree();
        string validManifest = Path.Combine(temp.Root, "materials.json");
        File.WriteAllText(validManifest, "[\"armor_iron\"]");
        MaterialManifest manifest = FbxPreflight.LoadManifest(validManifest);
        Equal(EvidenceAvailability.Available, manifest.Availability, "valid material manifest");

        string ascii = Path.Combine(temp.Root, "valid.fbx");
        File.WriteAllText(ascii, "; FBX 7.4.0 project file\nFBXHeaderExtension: {}\nObjects: { Material: 1, \"Material::armor_iron\", \"\" {} Material: 2, \"Material::typo_material\", \"\" {} }", new UTF8Encoding(false));
        PreflightResult asciiResult = FbxPreflight.Analyze(ascii, manifest);
        Equal(EvidenceAvailability.Available, asciiResult.Availability, "ASCII FBX evidence");
        Equal(2, asciiResult.DeclaredMaterials.Count, "material declaration extraction");
        True(asciiResult.Warnings.Any(text => text.Contains("typo_material", StringComparison.Ordinal)), "material mismatch is warning only");
        True(!asciiResult.Warnings.Any(text => text.Contains("blocking", StringComparison.OrdinalIgnoreCase)), "material finding does not block");

        string binary = Path.Combine(temp.Root, "binary.fbx");
        File.WriteAllBytes(binary, Encoding.ASCII.GetBytes("Kaydara FBX Binary  \0\x1a\0"));
        Equal(EvidenceAvailability.NotAvailable, FbxPreflight.Analyze(binary, manifest).Availability, "binary FBX unavailable");

        string malformed = Path.Combine(temp.Root, "malformed.fbx");
        File.WriteAllText(malformed, "not an FBX", new UTF8Encoding(false));
        Equal(EvidenceAvailability.NotAvailable, FbxPreflight.Analyze(malformed, manifest).Availability, "malformed FBX unavailable");

        string invalidManifest = Path.Combine(temp.Root, "invalid.json");
        File.WriteAllText(invalidManifest, "{broken");
        Equal(EvidenceAvailability.NotAvailable, FbxPreflight.LoadManifest(invalidManifest).Availability, "invalid manifest unavailable");
        Equal(EvidenceAvailability.NotAvailable, FbxPreflight.LoadManifest(null).Availability, "missing manifest unavailable");
        PreflightResult noManifest = FbxPreflight.Analyze(ascii, FbxPreflight.LoadManifest(null));
        Equal(EvidenceAvailability.Available, noManifest.Availability, "manifest absence does not block ASCII evidence");
        True(noManifest.Warnings.Any(text => text.Contains("No material manifest", StringComparison.Ordinal)), "missing manifest is explicit");

        string tooLargeForText = Path.Combine(temp.Root, "large-text.fbx");
        using (var stream = File.Create(tooLargeForText)) stream.SetLength(FbxPreflight.MaximumTextBytes + 1L);
        Equal(EvidenceAvailability.NotAvailable, FbxPreflight.Analyze(tooLargeForText, manifest).Availability, "text inspection bound");
    }

    private void TestDestinationResolution()
    {
        Equal("Modules > CalradiaForge > Assets", ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(
            new string?[] { "Modules > Native > Assets", "Modules > CalradiaForge > Assets", "Modules > CalradiaForge > Assets > nested" }, "CalradiaForge"), "exact direct destination");
        Throws<InvalidOperationException>(() => ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(Array.Empty<string>(), "CalradiaForge"), "missing destination");
        Throws<InvalidOperationException>(() => ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(
            new string?[] { "Modules > CalradiaForge > Assets", "Mods > CalradiaForge > Assets" }, "CalradiaForge"), "ambiguous destination");
        Throws<InvalidOperationException>(() => ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(
            new string?[] { "Modules > CalradiaForge > Assets", null }, "CalradiaForge"), "unreadable candidate");
        Throws<InvalidOperationException>(() => ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(
            new string?[] { "Modules > calradiaforge > Assets" }, "CalradiaForge"), "visible module name is exact");
        Throws<ArgumentException>(() => ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(
            new string?[] { "Modules > CalradiaForge > Assets" }, " CalradiaForge"), "module name whitespace rejected");
    }

    private void TestCliAndConfirmation()
    {
        CliOptions dryRun = CliOptions.Parse(new[] { "--dry-run", "--source-folder", @"C:\asset sources\niño" });
        Equal(RunMode.DryRun, dryRun.Mode, "dry-run mode");
        True(dryRun.SourceFolder!.Contains("niño", StringComparison.Ordinal), "CLI Unicode path");
        Throws<ArgumentException>(() => CliOptions.Parse(new[] { "--submit", "--source-folder", "C:\\assets" }), "module name required");
        Throws<ArgumentException>(() => CliOptions.Parse(new[] { "--dry-run", "--source-folder", "C:\\assets", "--config", "appsettings.json" }), "dry-run cannot load UI config");
        Throws<ArgumentException>(() => CliOptions.Parse(new[] { "--inspect", "--dry-run" }), "one explicit mode only");
        True(SubmissionConfirmation.IsAccepted("IMPORT"), "exact confirmation accepted");
        True(!SubmissionConfirmation.IsAccepted("import"), "lowercase confirmation rejected");
        True(!SubmissionConfirmation.IsAccepted("IMPORT "), "confirmation whitespace rejected");
        True(!SubmissionConfirmation.IsAccepted(null), "missing confirmation rejected");
    }

    private void TestDryRunAndModalStop()
    {
        using var temp = new TempTree();
        string source = temp.Directory("dry run ñ");
        WriteFbx(Path.Combine(source, "long sword.fbx"));
        var fake = new FakeAutomation(new UiSubmissionResult(UiSubmissionState.Stopped, "error modal remains open", ModalLeftOpen: true));

        ImportPlan plan = BatchPlanner.Build(source);
        Equal(1, plan.Assets.Count, "dry-run prepares batch");
        Equal(0, fake.InspectCalls + fake.ResolveCalls + fake.SubmitCalls, "dry-run does not access UI abstraction");

        string second = Path.Combine(source, "shield.fbx");
        WriteFbx(second);
        plan = BatchPlanner.Build(source);
        SubmissionSummary summary = SubmissionCoordinator.Submit(plan, "CalradiaForge", "Modules > CalradiaForge > Assets", fake);
        True(summary.Stopped, "UI modal stops batch");
        Equal(1, summary.Results.Count, "files after modal are not sent");
        Equal(1, fake.SubmitCalls, "no retry after modal");
        Equal("STOPPED", summary.Results[0].Outcome, "modal result not reported as success");
        True(summary.Results[0].ModalLeftOpen, "modal remains untouched");

        var failing = new FakeAutomation(new UiSubmissionResult(UiSubmissionState.Stopped, "unknown dialog", ModalLeftOpen: true));
        SubmissionSummary failed = SubmissionCoordinator.Submit(plan, "CalradiaForge", "Modules > CalradiaForge > Assets", failing);
        True(failed.Stopped && failing.SubmitCalls == 1, "uncertain UI state stops after one attempt");
    }

    private void TestDialogCloseGate()
    {
        True(DialogCloseGate.CanReportSubmitted(new(true, false, false, true, false), out _),
            "complete responsive snapshot with no dialogs may report SUBMITTED");
        True(!DialogCloseGate.CanReportSubmitted(new(false, false, false, true, false), out _),
            "incomplete window enumeration cannot report SUBMITTED");
        True(!DialogCloseGate.CanReportSubmitted(new(true, true, false, true, false), out _),
            "existing configured dialog cannot report SUBMITTED");
        True(!DialogCloseGate.CanReportSubmitted(new(true, false, true, true, false), out _),
            "unexpected visible top-level modal blocks SUBMITTED");
        True(!DialogCloseGate.CanReportSubmitted(new(true, false, false, false, false), out _),
            "disabled or unavailable main window blocks SUBMITTED");
        True(!DialogCloseGate.CanReportSubmitted(new(true, false, false, true, true), out _),
            "nested modal blocks SUBMITTED");
    }

    private void WriteFbx(string path) => File.WriteAllText(path, "; FBX 7.4.0 project file\nFBXHeaderExtension: {}", new UTF8Encoding(false));

    private void True(bool value, string label)
    {
        Assertions++;
        if (!value) throw new InvalidOperationException("Assertion failed: " + label);
    }

    private void Equal<T>(T expected, T actual, string label)
    {
        Assertions++;
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Assertion failed: {label}. Expected '{expected}', got '{actual}'.");
    }

    private void Throws<T>(Action action, string label) where T : Exception
    {
        Assertions++;
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Assertion failed: {label}. Expected {typeof(T).Name}.");
    }
}

sealed class FakeAutomation(UiSubmissionResult result) : IResourceBrowserAutomation
{
    public int InspectCalls { get; private set; }
    public int ResolveCalls { get; private set; }
    public int SubmitCalls { get; private set; }

    public EditorInspection Inspect()
    {
        InspectCalls++;
        return new(1, "fake", "fixture", Array.Empty<string>(), Array.Empty<InspectedControl>(), Array.Empty<string>());
    }

    public string ResolveUniqueAssetsTarget(string moduleName)
    {
        ResolveCalls++;
        return "Modules > " + moduleName + " > Assets";
    }

    public UiSubmissionResult SubmitFile(string moduleName, string expectedAssetsPath, AssetFile file)
    {
        SubmitCalls++;
        return result;
    }
}

sealed class TempTree : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "BannerlordFbxImporter.Tests", Guid.NewGuid().ToString("N"));
    public TempTree() => System.IO.Directory.CreateDirectory(Root);
    public string Directory(string name) => System.IO.Directory.CreateDirectory(Path.Combine(Root, name)).FullName;
    public void Dispose()
    {
        try { System.IO.Directory.Delete(Root, recursive: true); }
        catch { }
    }
}
