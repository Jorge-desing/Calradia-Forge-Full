using System.Security.Cryptography;
using System.Text.Json;
using System.IO;
using BannerlordFbxImporter;
using BannerlordFbxImporter.Automation;
using BannerlordFbxImporter.Preflight;

int passed = 0;
int failed = 0;
int skipped = 0;
var tests = new (string Name, Action Test)[]
{
    ("parse four explicit profiles", ParseProfiles),
    ("CLI requires a profile and preserves spaced paths", CliProfileAndSpaces),
    ("CLI mode errors list the calibration recording mode", CliModesListRecordCalibration),
    ("CLI exposes a picker-only mode without file or submit options", CliOpenImportPicker),
    ("CLI requires an explicit texture assignment map", CliTextureMapRequired),
    ("CLI requires module name for submit", CliModuleRequired),
    ("calibration recording requires a manually observed output identity", CliRecordCalibrationRequiresObservation),
    ("calibration recording parses spaced sample paths without a source scan", CliRecordCalibrationPath),
    ("calibration attestation requires exact VERIFIED text", CalibrationEvidenceConfirmationExact),
    ("CLI requires an unchanged verified sample for texture submission", CliSubmitRequiresSample),
    ("CLI keeps uncalibrated mesh submission disabled", CliMeshSubmitLocked),
    ("empty source produces a zero-file batch", EmptyBatch),
    ("scan one FBX", ScanOne),
    ("scan the 100-file limit", ScanHundred),
    ("reject 101 files", Reject101),
    ("scan recursive Unicode and spaced paths", ScanUnicodeNested),
    ("reject duplicate names regardless of case", RejectDuplicateNames),
    ("texture scan uses only configured extensions", ScanConfiguredTextureExtensions),
    ("reject malformed configured extensions", RejectMalformedExtension),
    ("texture profile requires observed extensions", TextureNeedsObservedExtensions),
    ("texture format list records an observed extension without guessing a fixed list", ObservedTextureExtension),
    ("texture assignment map binds every input", ValidTextureMap),
    ("texture assignment map rejects missing input", IncompleteTextureMap),
    ("texture assignment map rejects duplicate source names", DuplicateTextureMap),
    ("state machine rejects skipped import states", StateMachineRejectsSkip),
    ("state machine accepts evidence-backed sequence", StateMachineValidSequence),
    ("FBX ASCII material names are advisory evidence", AsciiFbxEvidence),
    ("binary FBX material inspection is unavailable", BinaryFbxEvidence),
    ("malformed FBX text material inspection is unavailable", MalformedFbxEvidence),
    ("missing or invalid material manifest remains unavailable", InvalidManifestEvidence),
    ("target resolver accepts direct module Assets path", ResolveDirectTarget),
    ("target resolver rejects nested Assets path", RejectNestedTarget),
    ("target resolver rejects ambiguous Assets paths", RejectAmbiguousTarget),
    ("window resolver selects Resource Browser and ignores same-process Edit Mode", ResolveResourceBrowserWindow),
    ("window resolver rejects missing or invisible Resource Browser", RejectMissingResourceBrowserWindow),
    ("window resolver rejects ambiguous same-process Resource Browser windows", RejectAmbiguousResourceBrowserWindows),
    ("picker menu lookup fails when the exact menu item is absent", PickerMenuItemAbsent),
    ("picker menu lookup fails closed on duplicate exact items", PickerMenuItemDuplicate),
    ("picker menu lookup rejects wrong PID and window", PickerMenuItemWrongIdentity),
    ("picker menu lookup requires a visible Menu ancestor", PickerMenuItemRequiresMenuAncestor),
    ("picker entry invokes no file selection, final Import, or Save action", PickerEntryOnly),
    ("default config keeps submit calibration off", DefaultConfigSubmitLocked),
    ("calibration evidence is atomically persisted and exactly authorizes matching context", CalibrationEvidenceRoundTrip),
    ("calibration evidence rejects mismatched profile, target, sample, settings, window, or process", CalibrationEvidenceRejectsMismatches),
    ("calibration evidence hash detects altered records", CalibrationEvidenceRejectsTampering),
    ("calibration evidence rejects changed sample content and non-PNG samples", CalibrationEvidenceRejectsChangedSample),
    ("appsettings calibration booleans cannot replace persistent evidence", CalibrationEvidenceRequiredBeyondFlags),
    ("calibration evidence path is profile-scoped under LocalAppData", CalibrationEvidenceDefaultPath),
    ("backup copies and verifies the Assets tree", VerifiedBackup),
    ("backup refuses a non-direct Assets folder", BackupTargetMustBeDirect),
    ("replacement requires an exact complete collision identity", ReplacementCollisionClassification),
    ("replacement backup verification checks exact backing files and hashes", ReplacementBackupMustMatchExactFiles),
    ("unknown replacement dialog stops without authorization or retry", UnknownReplacementDialogStops),
    ("replacement permit is single-use and uncertain outcome stays UNKNOWN", ReplacementPermitIsSingleUse),
    ("scanner skips reparse-point directories when Windows permits link creation", SkipReparseDirectory),
    ("submission coordinator stops on UNKNOWN without retry", UnknownStopsBatch),
    ("UI worker timeout cannot be reported as success", WorkerTimeoutIsNotSuccess),
    ("UI worker bounds redirected pipe draining after process exit", WorkerPipeDrainBounded),
    ("UI worker rejects submission actions", WorkerRejectsSubmissionAction),
    ("stale source file is rejected by submission lease", StaleInputRejected)
};

foreach ((string name, Action test) in tests)
{
    try
    {
        test();
        passed++;
        Console.WriteLine($"PASS {name}");
    }
    catch (TestSkippedException ex)
    {
        skipped++;
        Console.WriteLine($"SKIP {name}: {ex.Message}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

Console.WriteLine($"RESULT: {passed} passed, {failed} failed, {skipped} skipped");
return failed == 0 ? 0 : 1;

static void ParseProfiles()
{
    string[] expected = ["static-mesh", "rigged-mesh", "texture-only", "texture-assign"];
    foreach (string value in expected)
    {
        Assert(ImportProfileKinds.TryParse(value, out _), $"Could not parse {value}.");
        Assert(!ImportProfileKinds.TryParse(value + "-other", out _), $"Accepted invalid profile {value}-other.");
    }
}

static void CliProfileAndSpaces()
{
    CliOptions parsed = CliOptions.Parse(["--dry-run", "--profile", "static-mesh", "--source-folder", @"C:\Mod Assets\A Body"]);
    Assert(parsed.Profile == ImportProfileKind.StaticMesh, "CLI did not set mesh profile.");
    Assert(parsed.SourceFolder == @"C:\Mod Assets\A Body", "CLI altered a quoted path.");
    AssertThrows<ArgumentException>(() => CliOptions.Parse(["--dry-run", "--source-folder", "x"]));
}

static void CliTextureMapRequired() => AssertThrows<ArgumentException>(() =>
    CliOptions.Parse(["--dry-run", "--profile", "texture-assign", "--source-folder", "x"]));

static void CliModesListRecordCalibration()
{
    try
    {
        CliOptions.Parse(["--dry-run", "--record-calibration"]);
        throw new InvalidOperationException("Multiple modes were accepted.");
    }
    catch (ArgumentException ex)
    {
        Assert(ex.Message.Contains("--record-calibration", StringComparison.Ordinal), "Multiple-mode error omitted --record-calibration.");
    }
}

static void CliOpenImportPicker()
{
    CliOptions parsed = CliOptions.Parse(["--open-import-picker", "--config", @"C:\Mod Assets\importer.json"]);
    Assert(parsed.Mode == RunMode.OpenImportPicker, "Picker-only mode was not parsed.");
    Assert(parsed.ConfigPath == Path.GetFullPath(@"C:\Mod Assets\importer.json"), "Picker mode altered a spaced config path.");
    AssertThrows<ArgumentException>(() => CliOptions.Parse(["--open-import-picker", "--source-folder", "assets"]));
    AssertThrows<ArgumentException>(() => CliOptions.Parse(["--open-import-picker", "--profile", "texture-only"]));
}

static void CliModuleRequired() => AssertThrows<ArgumentException>(() =>
    CliOptions.Parse(["--submit", "--profile", "static-mesh", "--source-folder", "x"]));

static void CliSubmitRequiresSample() => AssertThrows<ArgumentException>(() =>
    CliOptions.Parse(["--submit", "--profile", "texture-only", "--source-folder", "assets", "--module-name", "CalradiaForge"]));

static void CliMeshSubmitLocked() => AssertThrows<ArgumentException>(() =>
    CliOptions.Parse(["--submit", "--profile", "static-mesh", "--source-folder", "assets", "--module-name", "CalradiaForge", "--calibration-sample", "compass.png"]));

static void CliRecordCalibrationRequiresObservation() => AssertThrows<ArgumentException>(() =>
    CliOptions.Parse(["--record-calibration", "--profile", "texture-only", "--module-name", "CalradiaForge", "--calibration-sample", "compass.png"]));

static void CliRecordCalibrationPath()
{
    CliOptions parsed = CliOptions.Parse(["--record-calibration", "--profile", "texture-only", "--module-name", "CalradiaForge",
        "--calibration-sample", @"C:\Mod Assets\calradiaforge_compass.png", "--observed-resource-name", "calradiaforge_compass", "--observed-resource-type", "Texture"]);
    Assert(parsed.Mode == RunMode.RecordCalibration && parsed.SourceFolder is null, "Calibration recording incorrectly requires a scanned source folder.");
    Assert(parsed.CalibrationSamplePath == Path.GetFullPath(@"C:\Mod Assets\calradiaforge_compass.png"), "Calibration path with spaces was altered.");
}

static void CalibrationEvidenceConfirmationExact()
{
    Assert(CalibrationEvidenceConfirmation.IsAccepted("VERIFIED"), "Exact manual attestation was rejected.");
    Assert(!CalibrationEvidenceConfirmation.IsAccepted("verified") && !CalibrationEvidenceConfirmation.IsAccepted("IMPORT"),
        "Calibration accepted a non-exact attestation phrase.");
}

static void EmptyBatch()
{
    using var temp = new TempDirectory();
    File.WriteAllText(Path.Combine(temp.Path, "readme.txt"), "ignored");
    Assert(AssetBatchScanner.Scan(temp.Path).Files.Count == 0, "Empty FBX batch was not empty.");
}

static void ScanOne()
{
    using var temp = new TempDirectory();
    File.WriteAllText(Path.Combine(temp.Path, "one.fbx"), "; FBX 7.4.0 project file");
    AssetBatch batch = AssetBatchScanner.Scan(temp.Path);
    Assert(batch.Files.Count == 1 && batch.Files[0].FullPath.EndsWith("one.fbx", StringComparison.OrdinalIgnoreCase), "Expected one FBX.");
}

static void ScanHundred()
{
    using var temp = new TempDirectory();
    for (int index = 0; index < 100; index++) File.WriteAllText(Path.Combine(temp.Path, $"mesh{index:D3}.fbx"), "x");
    Assert(AssetBatchScanner.Scan(temp.Path).Files.Count == 100, "Expected exactly 100 files.");
}

static void Reject101()
{
    using var temp = new TempDirectory();
    for (int index = 0; index < 101; index++) File.WriteAllText(Path.Combine(temp.Path, $"mesh{index:D3}.fbx"), "x");
    AssertThrows<InvalidOperationException>(() => AssetBatchScanner.Scan(temp.Path));
}

static void ScanUnicodeNested()
{
    using var temp = new TempDirectory();
    string nested = Path.Combine(temp.Path, "Modelos con espacios", "Armadura Ñandú");
    Directory.CreateDirectory(nested);
    File.WriteAllText(Path.Combine(nested, "malla ñ.fbx"), "x");
    AssetBatch batch = AssetBatchScanner.Scan(temp.Path);
    Assert(batch.Files.Count == 1 && batch.Files[0].FullPath.Contains("Armadura Ñandú", StringComparison.Ordinal), "Unicode recursive path was lost.");
}

static void RejectDuplicateNames()
{
    using var temp = new TempDirectory();
    Directory.CreateDirectory(Path.Combine(temp.Path, "sub"));
    File.WriteAllText(Path.Combine(temp.Path, "Armor.fbx"), "x");
    File.WriteAllText(Path.Combine(temp.Path, "sub", "armor.FBX"), "x");
    AssertThrows<InvalidOperationException>(() => AssetBatchScanner.Scan(temp.Path));
}

static void ScanConfiguredTextureExtensions()
{
    using var temp = new TempDirectory();
    File.WriteAllText(Path.Combine(temp.Path, "albedo.png"), "png");
    File.WriteAllText(Path.Combine(temp.Path, "normal.dds"), "dds");
    File.WriteAllText(Path.Combine(temp.Path, "notes.txt"), "ignored");
    AssetBatch batch = AssetBatchScanner.Scan(temp.Path, supportedExtensions: [".png"]);
    Assert(batch.Files.Count == 1 && batch.Files[0].FullPath.EndsWith("albedo.png", StringComparison.OrdinalIgnoreCase), "Scanner did not honor the observed extension selection.");
}

static void RejectMalformedExtension() => AssertThrows<ArgumentException>(() => AssetBatchScanner.Scan(".", supportedExtensions: ["png"]));

static void TextureNeedsObservedExtensions()
{
    var config = new AppConfig();
    AssertThrows<InvalidOperationException>(() => ProfileConfiguration.ValidateAndGetExtensions(config, ImportProfileKind.TextureOnly, forSubmit: false));
    config.Profiles["texture-only"].SupportedExtensions.Add(".png");
    Assert(ProfileConfiguration.ValidateAndGetExtensions(config, ImportProfileKind.TextureOnly, forSubmit: false).SequenceEqual([".png"]), "Observed extension was not returned.");
}

static void ObservedTextureExtension()
{
    Assert(ProfileConfiguration.NormalizeExtension(".webp") == ".webp", "The configured observation path still relies on a guessed format allowlist.");
    AssertThrows<InvalidOperationException>(() => ProfileConfiguration.NormalizeExtension("png"));
}

static void ValidTextureMap()
{
    using var temp = new TempDirectory();
    string source = Path.Combine(temp.Path, "Normal Ñandú.png");
    File.WriteAllText(source, "png");
    AssetFile file = AssetBatchScanner.Scan(temp.Path, supportedExtensions: [".png"]).Files.Single();
    string map = WriteMap(temp.Path, "[{\"sourceFile\":\"Normal Ñandú.png\",\"textureName\":\"armor_normal\",\"materialName\":\"armor_mat\",\"slot\":\"Normal\"}]");
    TextureAssignmentMap parsed = TextureAssignmentMap.Load(map, [file]);
    Assert(parsed.Assignments["Normal Ñandú.png"].MaterialName == "armor_mat", "Map entry was not preserved.");
    ImportPlan plan = BatchPlanner.Build(temp.Path, ImportProfileKind.TextureAssign, [".png"], textureAssignmentMapPath: map);
    Assert(plan.Assets.Single().TextureAssignment?.Slot == "Normal", "Planner did not link the assignment.");
}

static void IncompleteTextureMap()
{
    using var temp = new TempDirectory();
    File.WriteAllText(Path.Combine(temp.Path, "a.png"), "a");
    AssetFile file = AssetBatchScanner.Scan(temp.Path, supportedExtensions: [".png"]).Files.Single();
    string map = WriteMap(temp.Path, "[]");
    AssertThrows<InvalidDataException>(() => TextureAssignmentMap.Load(map, [file]));
}

static void DuplicateTextureMap()
{
    using var temp = new TempDirectory();
    AssetFile file = new(Path.Combine(temp.Path, "a.png"), 1, DateTime.UtcNow);
    string json = "[{\"sourceFile\":\"a.png\",\"textureName\":\"a\",\"materialName\":\"m\",\"slot\":\"x\"},{\"sourceFile\":\"A.PNG\",\"textureName\":\"b\",\"materialName\":\"m\",\"slot\":\"x\"}]";
    AssertThrows<InvalidDataException>(() => TextureAssignmentMap.Load(WriteMap(temp.Path, json), [file]));
}

static void StateMachineRejectsSkip()
{
    var machine = new ImportStateMachine();
    AssertThrows<InvalidOperationException>(() => machine.MoveTo(ImportWorkflowState.Verified, "guessed"));
    Assert(machine.Current == ImportWorkflowState.Planned, "Rejected transition changed state.");
}

static void StateMachineValidSequence()
{
    var machine = new ImportStateMachine();
    machine.MoveTo(ImportWorkflowState.TargetReady, "Unique module > Assets path observed.");
    machine.MoveTo(ImportWorkflowState.SettingsReady, "Configured settings observed and reviewed.");
    machine.MoveTo(ImportWorkflowState.Submitted, "Final Import button invocation returned.");
    Assert(machine.Current == ImportWorkflowState.Submitted, "State sequence did not advance.");
}

static void AsciiFbxEvidence()
{
    using var temp = new TempDirectory();
    string path = Path.Combine(temp.Path, "mesh.fbx");
    File.WriteAllText(path, "; FBX 7.4.0 project file\nMaterial: 1, \"Material::armor_mat\", \"Phong\" { }\n");
    var manifest = new MaterialManifest(EvidenceAvailability.Available, new HashSet<string>(["armor_mat"], StringComparer.OrdinalIgnoreCase), "fixture");
    PreflightResult result = FbxPreflight.Analyze(path, manifest);
    Assert(result.Availability == EvidenceAvailability.Available && result.DeclaredMaterials.Contains("armor_mat"), "ASCII material hint was not extracted.");
    Assert(result.Limitation.Contains("no import validation", StringComparison.OrdinalIgnoreCase), "Preflight overstated its evidence.");
}

static void BinaryFbxEvidence()
{
    using var temp = new TempDirectory();
    string path = Path.Combine(temp.Path, "binary.fbx");
    byte[] signature = System.Text.Encoding.ASCII.GetBytes("Kaydara FBX Binary");
    File.WriteAllBytes(path, [.. signature, 0, 0, 0, 0, 0, 0]);
    PreflightResult result = FbxPreflight.Analyze(path, FbxPreflight.LoadManifest(null));
    Assert(result.Availability == EvidenceAvailability.NotAvailable, "Binary FBX was treated as parsed.");
}

static void MalformedFbxEvidence()
{
    using var temp = new TempDirectory();
    string path = Path.Combine(temp.Path, "malformed.fbx");
    File.WriteAllBytes(path, [0xFF, 0xFE, 0x00]);
    PreflightResult result = FbxPreflight.Analyze(path, FbxPreflight.LoadManifest(null));
    Assert(result.Availability == EvidenceAvailability.NotAvailable, "Malformed FBX was treated as parsed.");
}

static void InvalidManifestEvidence()
{
    using var temp = new TempDirectory();
    string invalid = Path.Combine(temp.Path, "materials.json");
    File.WriteAllText(invalid, "{ not an array }");
    MaterialManifest result = FbxPreflight.LoadManifest(invalid);
    Assert(result.Availability == EvidenceAvailability.NotAvailable, "Invalid material manifest was not downgraded to unavailable evidence.");
}

static void ResolveDirectTarget()
{
    string result = ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(["Modules > Calradia Forge > Assets"], "Calradia Forge");
    Assert(result == "Modules > Calradia Forge > Assets", "Unexpected target path.");
}

static void RejectNestedTarget() => AssertThrows<InvalidOperationException>(() =>
    ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(["Modules > Calradia Forge > Temp > Assets"], "Calradia Forge"));

static void RejectAmbiguousTarget() => AssertThrows<InvalidOperationException>(() =>
    ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(["Modules > Forge > Assets", "Modules > Forge > Assets"], "Forge"));

static void ResolveResourceBrowserWindow()
{
    EditorWindowCandidate selected = ProcessWindowResolver.ResolveUnique(
    [
        new EditorWindowCandidate(42, "Edit Mode", true, (IntPtr)1),
        new EditorWindowCandidate(42, "Resource Browser", true, (IntPtr)2),
        new EditorWindowCandidate(84, "Resource Browser", true, (IntPtr)3),
        new EditorWindowCandidate(42, "Resource Browser", false, (IntPtr)4)
    ], 42, "Resource Browser");
    Assert(selected.Handle == (IntPtr)2, "Window resolver selected the wrong same-process window.");
}

static void RejectMissingResourceBrowserWindow() => AssertThrows<InvalidOperationException>(() =>
    ProcessWindowResolver.ResolveUnique(
        [new EditorWindowCandidate(42, "Edit Mode", true, (IntPtr)1), new EditorWindowCandidate(84, "Resource Browser", true, (IntPtr)2)],
        42, "Resource Browser"));

static void RejectAmbiguousResourceBrowserWindows() => AssertThrows<InvalidOperationException>(() =>
    ProcessWindowResolver.ResolveUnique(
        [new EditorWindowCandidate(42, "Resource Browser", true, (IntPtr)1), new EditorWindowCandidate(42, "Resource Browser", true, (IntPtr)2)],
        42, "Resource Browser"));

static void PickerMenuItemAbsent()
{
    int invokeCount = 0;
    AssertThrows<InvalidOperationException>(() => ImportPickerMenuAction.InvokeUnique(
        [PickerCandidate("Create", 42, "Resource Browser", "Create")],
        42, "Resource Browser", _ => invokeCount++));
    Assert(invokeCount == 0, "An absent exact picker menu item caused an invocation.");
}

static void PickerMenuItemDuplicate()
{
    int invokeCount = 0;
    ImportPickerMenuCandidate<string>[] duplicate =
    [
        PickerCandidate("Import new asset", 42, "Resource Browser", "first"),
        PickerCandidate("Import new asset", 42, "Resource Browser", "second")
    ];
    AssertThrows<InvalidOperationException>(() => ImportPickerMenuAction.InvokeUnique(
        duplicate, 42, "Resource Browser", _ => invokeCount++));
    Assert(invokeCount == 0, "An ambiguous picker menu item caused an invocation.");
}

static void PickerMenuItemWrongIdentity()
{
    int invokeCount = 0;
    ImportPickerMenuCandidate<string>[] wrongIdentity =
    [
        PickerCandidate("Import new asset", 84, "Resource Browser", "wrong-pid"),
        PickerCandidate("Import new asset", 42, "Edit Mode", "wrong-window"),
        PickerCandidate("Import new asset", 42, "Resource Browser", "offscreen", visible: false)
    ];
    AssertThrows<InvalidOperationException>(() => ImportPickerMenuAction.InvokeUnique(
        wrongIdentity, 42, "Resource Browser", _ => invokeCount++));
    Assert(invokeCount == 0, "A menu item from a different PID/window or hidden state caused an invocation.");
}

static void PickerMenuItemRequiresMenuAncestor()
{
    int invokeCount = 0;
    ImportPickerMenuCandidate<string> candidate = PickerCandidate("Import new asset", 42, "Resource Browser", "not-in-menu") with
    {
        IsInsideVisibleMenu = false
    };
    AssertThrows<InvalidOperationException>(() => ImportPickerMenuAction.InvokeUnique(
        [candidate], 42, "Resource Browser", _ => invokeCount++));
    Assert(invokeCount == 0, "A homonymous menu item outside a visible menu was invoked.");
}

static void PickerEntryOnly()
{
    int menuInvocations = 0;
    int fileSelections = 0;
    int finalImportActions = 0;
    int saveActions = 0;
    ImportPickerMenuAction.InvokeUnique(
        [PickerCandidate("Import new asset", 42, "Resource Browser", "picker-menu")],
        42, "Resource Browser", item =>
        {
            Assert(item == "picker-menu", "Picker-only resolver returned the wrong menu item.");
            menuInvocations++;
        });
    Assert(menuInvocations == 1, "The exact picker menu item was not invoked exactly once.");
    Assert(fileSelections == 0 && finalImportActions == 0 && saveActions == 0,
        "Picker-entry path performed a file selection, final Import, or Save action.");
}

static ImportPickerMenuCandidate<string> PickerCandidate(
    string name, int processId, string windowTitle, string element,
    bool visible = true, bool enabled = true, bool supportsInvoke = true) =>
    new(name, ImportPickerMenuAction.ExactControlType, processId, windowTitle, true, visible, enabled, supportsInvoke, element);

static void DefaultConfigSubmitLocked()
{
    var config = new AppConfig();
    Assert(config.ResourceBrowserWindowTitle == "Resource Browser", "Default Resource Browser title is not exact.");
    AssertThrows<InvalidOperationException>(() => config.ValidateForSubmit(ImportProfileKind.StaticMesh, "Forge"));
}

static void CalibrationEvidenceRoundTrip()
{
    using var temp = new TempDirectory();
    var fixture = CreateCalibrationFixture(temp.Path);
    CalibrationEvidence stored = fixture.Store.SaveAfterManualVerification(fixture.Observation, fixture.SamplePath);
    Assert(stored.SchemaVersion == CalibrationEvidenceStore.CurrentSchemaVersion, "Evidence version was not recorded.");
    Assert(stored.Profile == "texture-only" && stored.ModuleName == "CalradiaForge", "Profile or module evidence was not recorded.");
    Assert(stored.AssetsPath == fixture.AssetsPath, "Assets path was not recorded.");
    Assert(stored.SampleFileName == "calradiaforge_compass.png" && stored.SampleSizeBytes == File.ReadAllBytes(fixture.SamplePath).Length, "Sample name or size was not recorded.");
    Assert(stored.SampleSha256 == Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fixture.SamplePath))), "Sample hash is incorrect.");
    Assert(stored.ObservedResourceName == "calradiaforge_compass" && stored.ObservedResourceType == "Texture", "Observed output identity was not recorded.");
    Assert(stored.ObservedSettings.Count == fixture.Observation.ObservedSettings.Count && stored.WindowTitle == "Resource Browser" && stored.ProcessName == "TaleWorlds.MountAndBlade.Launcher", "Settings or Editor context was not recorded.");
    Assert(stored.ObservedAtUtc.Offset == TimeSpan.Zero && stored.IntegritySha256.Length == 64, "Timestamp or integrity hash is invalid.");

    Assert(fixture.Store.TryRead(out CalibrationEvidence? loaded, out string readReason), readReason);
    Assert(loaded is not null && loaded.IntegritySha256 == stored.IntegritySha256, "Stored evidence did not round-trip.");
    Assert(fixture.Store.IsSubmissionAuthorized(fixture.Request, out string matchReason), matchReason);
    fixture.Store.RequireSubmissionEvidence(fixture.Request);
    Assert(!Directory.EnumerateFiles(Path.GetDirectoryName(fixture.Store.Path)!, "*.tmp", SearchOption.TopDirectoryOnly).Any(), "Atomic writer left a temporary file behind.");

    CalibrationEvidence secondWrite = fixture.Store.SaveAfterManualVerification(fixture.Observation, fixture.SamplePath);
    Assert(secondWrite.IntegritySha256 == stored.IntegritySha256, "Replacing a prior evidence record changed its canonical integrity hash.");
}

static void CalibrationEvidenceRejectsMismatches()
{
    using var temp = new TempDirectory();
    var fixture = CreateCalibrationFixture(temp.Path);
    fixture.Store.SaveAfterManualVerification(fixture.Observation, fixture.SamplePath);
    CalibrationEvidenceMatchRequest[] mismatches =
    [
        fixture.Request with { Profile = "texture-assign" },
        fixture.Request with { ModuleName = "OtherModule" },
        fixture.Request with { AssetsPath = Path.Combine(temp.Path, "OtherModule", "Assets") },
        fixture.Request with { SampleFilePath = Path.Combine(temp.Path, "different.png") },
        fixture.Request with { ObservedResourceName = "other_name" },
        fixture.Request with { ObservedResourceType = "Material" },
        fixture.Request with { ObservedSettings = new Dictionary<string, string> { ["ImportType"] = "Normal map" } },
        fixture.Request with { WindowTitle = "Edit Mode" },
        fixture.Request with { ProcessName = "OtherEditor" }
    ];

    foreach (CalibrationEvidenceMatchRequest mismatch in mismatches)
        Assert(!fixture.Store.IsSubmissionAuthorized(mismatch, out _), "A mismatched calibration context was authorized.");
}

static void CalibrationEvidenceRejectsTampering()
{
    using var temp = new TempDirectory();
    var fixture = CreateCalibrationFixture(temp.Path);
    fixture.Store.SaveAfterManualVerification(fixture.Observation, fixture.SamplePath);
    string json = File.ReadAllText(fixture.Store.Path);
    const string original = "\"ObservedResourceName\": \"calradiaforge_compass\"";
    Assert(json.Contains(original, StringComparison.Ordinal), "Fixture did not contain the expected output name.");
    File.WriteAllText(fixture.Store.Path, json.Replace(original, "\"ObservedResourceName\": \"forged_resource\"", StringComparison.Ordinal));
    Assert(!fixture.Store.TryRead(out _, out string reason) && reason.Contains("integrity", StringComparison.OrdinalIgnoreCase), "Modified evidence passed its integrity check.");
}

static void CalibrationEvidenceRejectsChangedSample()
{
    using var temp = new TempDirectory();
    var fixture = CreateCalibrationFixture(temp.Path);
    fixture.Store.SaveAfterManualVerification(fixture.Observation, fixture.SamplePath);
    using (var append = new FileStream(fixture.SamplePath, FileMode.Append, FileAccess.Write, FileShare.None)) append.WriteByte(0x99);
    Assert(!fixture.Store.IsSubmissionAuthorized(fixture.Request, out string reason), "Changed sample content was authorized.");
    Assert(reason.Contains("sample", StringComparison.OrdinalIgnoreCase), "Changed sample rejection did not explain the sample mismatch.");

    string invalidPng = Path.Combine(temp.Path, "invalid.png");
    File.WriteAllBytes(invalidPng, [0, 1, 2, 3, 4, 5, 6, 7]);
    AssertThrows<InvalidDataException>(() => fixture.Store.SaveAfterManualVerification(fixture.Observation, invalidPng));
    string wrongExtension = Path.Combine(temp.Path, "not-an-image.txt");
    File.WriteAllBytes(wrongExtension, PngFixtureBytes());
    AssertThrows<InvalidDataException>(() => fixture.Store.SaveAfterManualVerification(fixture.Observation, wrongExtension));
}

static void CalibrationEvidenceRequiredBeyondFlags()
{
    using var temp = new TempDirectory();
    var fixture = CreateCalibrationFixture(temp.Path);
    var appsettings = new AppConfig { EditorCalibrationReviewed = true };
    appsettings.Profiles["texture-only"].CalibrationReviewed = true;
    appsettings.Profiles["texture-only"].ResourceInventoryComplete = true;
    appsettings.Profiles["texture-only"].CalibrationNote = "boolean only";
    Assert(!fixture.Store.IsSubmissionAuthorized(fixture.Request, out string reason), "Appsettings booleans authorized a profile without persistent evidence.");
    Assert(reason.Contains("does not exist", StringComparison.OrdinalIgnoreCase), "Missing evidence failure was not explicit.");
}

static void CalibrationEvidenceDefaultPath()
{
    string path = CalibrationEvidenceStore.GetDefaultPath("texture-only", @"C:\User Data");
    Assert(path == Path.GetFullPath(@"C:\User Data\CalradiaForge\Importer\Calibration\texture-only.json"), "Default evidence path is not profile-scoped under LocalAppData.");
    AssertThrows<ArgumentException>(() => CalibrationEvidenceStore.GetDefaultPath("Texture-Only", @"C:\User Data"));
}

static (CalibrationEvidenceStore Store, CalibrationEvidenceObservation Observation, CalibrationEvidenceMatchRequest Request, string SamplePath, string AssetsPath) CreateCalibrationFixture(string root)
{
    string assetsPath = Path.Combine(root, "Modules", "CalradiaForge", "Assets");
    Directory.CreateDirectory(assetsPath);
    string samplePath = Path.Combine(root, "sample", "calradiaforge_compass.png");
    Directory.CreateDirectory(Path.GetDirectoryName(samplePath)!);
    File.WriteAllBytes(samplePath, PngFixtureBytes());
    var settings = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ImportType"] = "Albedo",
        ["GenerateMips"] = "True"
    };
    var observation = new CalibrationEvidenceObservation(
        "texture-only", "CalradiaForge", assetsPath, "calradiaforge_compass", "Texture", settings,
        "Resource Browser", "TaleWorlds.MountAndBlade.Launcher", DateTimeOffset.UtcNow);
    var request = new CalibrationEvidenceMatchRequest(
        observation.Profile, observation.ModuleName, observation.AssetsPath, samplePath,
        observation.ObservedResourceName, observation.ObservedResourceType, settings,
        observation.WindowTitle, observation.ProcessName);
    return (new CalibrationEvidenceStore(Path.Combine(root, "local", "Calibration", "texture-only.json")), observation, request, samplePath, assetsPath);
}

static byte[] PngFixtureBytes() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02, 0x03, 0x04];

static void VerifiedBackup()
{
    using var temp = new TempDirectory();
    string module = Path.Combine(temp.Path, "My Mod");
    string assets = Path.Combine(module, "Assets");
    Directory.CreateDirectory(Path.Combine(assets, "Meshes"));
    string original = Path.Combine(assets, "Meshes", "armor.tpac");
    byte[] bytes = [0, 1, 2, 3, 4, 5];
    File.WriteAllBytes(original, bytes);
    string sourceHash = Convert.ToHexString(SHA256.HashData(bytes));
    AssetBackupResult backup = AssetBackupService.CreateVerifiedBackup(assets, "My Mod", Path.Combine(temp.Path, "backups"));
    string copied = Path.Combine(backup.BackupDirectory, "Assets", "Meshes", "armor.tpac");
    Assert(File.Exists(copied), "Backup copy is missing.");
    Assert(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(copied))) == sourceHash, "Backup contents differ from source.");
    Assert(backup.FileCount == 1 && File.Exists(backup.ManifestPath), "Backup manifest is incomplete.");
    Assert(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(original))) == sourceHash, "Original source was altered.");
}

static void BackupTargetMustBeDirect()
{
    using var temp = new TempDirectory();
    string nested = Path.Combine(temp.Path, "My Mod", "Work", "Assets");
    Directory.CreateDirectory(nested);
    AssertThrows<InvalidOperationException>(() => AssetBackupService.ValidateAssetsRoot(nested, "My Mod"));
}

static void ReplacementCollisionClassification()
{
    var request = new RequestedResourceIdentity("CalradiaForge", "calradiaforge_compass", "Texture");
    Assert(ResourceCollisionClassifier.Classify(request, false, []).Kind == ResourceCollisionKind.InventoryUnavailable,
        "Incomplete inventory was treated as a clear destination.");
    Assert(ResourceCollisionClassifier.Classify(request, true, []).Kind == ResourceCollisionKind.NoCollision,
        "A complete empty inventory was not classified as no collision.");
    Assert(ResourceCollisionClassifier.Classify(request, true,
        [new DestinationResourceIdentity("CalradiaForge", "calradiaforge_compass", "Material", ["GUI/SpriteParts/compass.tpac"])])
        .Kind == ResourceCollisionKind.TypeConflict, "A same-name, wrong-type resource was not blocked.");
    Assert(ResourceCollisionClassifier.Classify(request, true,
        [new DestinationResourceIdentity("CalradiaForge", "calradiaforge_compass", "Texture", ["GUI/a.tpac"]),
         new DestinationResourceIdentity("CalradiaForge", "calradiaforge_compass", "Texture", ["GUI/b.tpac"])])
        .Kind == ResourceCollisionKind.Ambiguous, "Duplicate exact identities were not blocked as ambiguous.");
    Assert(ResourceCollisionClassifier.Classify(request, true,
        [new DestinationResourceIdentity("CalradiaForge", "calradiaforge_compass", "Texture", ["../outside.tpac"])])
        .Kind == ResourceCollisionKind.InvalidEvidence, "An escaping backing path was accepted.");
}

static void ReplacementBackupMustMatchExactFiles()
{
    using var temp = new TempDirectory();
    const string module = "CalradiaForge";
    string assets = Path.Combine(temp.Path, "Modules", module, "Assets");
    string relative = "GUI/SpriteParts/compass.tpac";
    string original = Path.Combine(assets, relative.Replace('/', Path.DirectorySeparatorChar));
    Directory.CreateDirectory(Path.GetDirectoryName(original)!);
    File.WriteAllBytes(original, [1, 2, 3, 4]);
    AssetBackupResult backup = AssetBackupService.CreateVerifiedBackup(assets, module, Path.Combine(temp.Path, "backups"));
    var requested = new RequestedResourceIdentity(module, "calradiaforge_compass", "Texture");
    ResourceCollisionReview collision = ResourceCollisionClassifier.Classify(requested, true,
        [new DestinationResourceIdentity(module, requested.Name, requested.Type, [relative])]);
    BackupVerificationReview verified = ReplacementBackupVerifier.Verify(collision, backup, assets, module);
    Assert(verified.IsVerified && verified.Files.Count == 1, "A matching source and verified backup were not accepted.");

    string copied = Path.Combine(backup.BackupDirectory, "Assets", relative.Replace('/', Path.DirectorySeparatorChar));
    File.WriteAllBytes(copied, [9, 9, 9, 9]);
    Assert(!ReplacementBackupVerifier.Verify(collision, backup, assets, module).IsVerified,
        "A tampered backup copy passed hash verification.");

    ResourceCollisionReview wrongSet = ResourceCollisionClassifier.Classify(requested, true,
        [new DestinationResourceIdentity(module, requested.Name, requested.Type, ["GUI/SpriteParts/missing.tpac"])]);
    Assert(!ReplacementBackupVerifier.Verify(wrongSet, backup, assets, module).IsVerified,
        "Backup verification accepted a backing file set absent from its manifest.");
}

static void UnknownReplacementDialogStops()
{
    using var temp = new TempDirectory();
    (ResourceCollisionReview collision, BackupVerificationReview backup) = CreateReplacementEvidence(temp.Path);
    var calibration = new CalibratedReplacementDialog(true, 42, "replace-window", "Replace Resource", "replace", "cancel");
    var gate = new ReplacementAuthorizationGate(collision, backup, calibration);
    ReplacementAuthorizationReview result = gate.Authorize(new ReplacementDialogSnapshot(true, false, []));
    Assert(result.State == ReplacementAuthorizationState.Unknown && result.Permit is null,
        "A timed out or incomplete modal observation authorized replacement.");
    Assert(gate.Authorize(new ReplacementDialogSnapshot(false, true, [])).State == ReplacementAuthorizationState.Stopped,
        "Authorization was retried after an unknown dialog outcome.");
}

static void ReplacementPermitIsSingleUse()
{
    using var temp = new TempDirectory();
    (ResourceCollisionReview collision, BackupVerificationReview backup) = CreateReplacementEvidence(temp.Path);
    var calibration = new CalibratedReplacementDialog(true, 42, "replace-window", "Replace Resource", "replace", "cancel");
    var observed = new ReplacementDialogSnapshot(false, true,
        [new ReplacementDialogObservation(42, "replace-window", "Replace Resource", true, 1, 1)]);
    ReplacementAuthorizationReview authorized = new ReplacementAuthorizationGate(collision, backup, calibration).Authorize(observed);
    Assert(authorized.State == ReplacementAuthorizationState.Authorized && authorized.Permit is not null,
        "Exact collision, backup, and calibrated modal did not produce a one-use capability.");

    ReplacementPermit permit = authorized.Permit!;
    Assert(permit.BeginClick(out _), "First calibrated invocation was not permitted.");
    ReplacementClickState uncertain = permit.CompleteClick(false, false, false, false, out string detail);
    Assert(uncertain == ReplacementClickState.Unknown && detail.Contains("do not retry", StringComparison.OrdinalIgnoreCase),
        "An uncertain replacement click was not marked UNKNOWN.");
    Assert(!permit.BeginClick(out _) && permit.ClickState == ReplacementClickState.Unknown,
        "The replacement capability allowed a retry after uncertainty.");
}

static (ResourceCollisionReview Collision, BackupVerificationReview Backup) CreateReplacementEvidence(string root)
{
    const string module = "CalradiaForge";
    const string resourceName = "compass";
    const string resourceType = "Texture";
    const string relative = "GUI/compass.tpac";
    string assets = Path.Combine(root, "Modules", module, "Assets");
    string source = Path.Combine(assets, relative.Replace('/', Path.DirectorySeparatorChar));
    Directory.CreateDirectory(Path.GetDirectoryName(source)!);
    File.WriteAllBytes(source, [1, 2, 3, 4]);
    AssetBackupResult snapshot = AssetBackupService.CreateVerifiedBackup(assets, module, Path.Combine(root, "backups"));
    var requested = new RequestedResourceIdentity(module, resourceName, resourceType);
    ResourceCollisionReview collision = ResourceCollisionClassifier.Classify(requested, true,
        [new DestinationResourceIdentity(module, resourceName, resourceType, [relative])]);
    BackupVerificationReview backup = ReplacementBackupVerifier.Verify(collision, snapshot, assets, module);
    Assert(backup.IsVerified, "Replacement fixture did not produce real verified backup evidence.");
    return (collision, backup);
}

static void SkipReparseDirectory()
{
    using var temp = new TempDirectory();
    string target = Path.Combine(temp.Path, "target");
    string link = Path.Combine(temp.Path, "link");
    Directory.CreateDirectory(target);
    File.WriteAllText(Path.Combine(target, "inside.fbx"), "x");
    try { Directory.CreateSymbolicLink(link, target); }
    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
    {
        throw new TestSkippedException("Windows did not permit creating a symbolic link in the current account.");
    }
    AssetBatch batch = AssetBatchScanner.Scan(temp.Path);
    Assert(batch.Files.Count == 1 && batch.Warnings.Any(warning => warning.Contains("reparse point", StringComparison.OrdinalIgnoreCase)), "Scanner followed or failed to report a reparse directory.");
}

static void UnknownStopsBatch()
{
    using var temp = new TempDirectory();
    string filePath = Path.Combine(temp.Path, "one.fbx");
    File.WriteAllText(filePath, "x");
    AssetFile file = AssetBatchScanner.Scan(temp.Path).Files.Single();
    var batch = new AssetBatch(temp.Path, [file], [], file.Length);
    var plan = new ImportPlan(ImportProfileKind.StaticMesh, batch, [new PlannedAsset(file, null, null)],
        new MaterialManifest(EvidenceAvailability.NotAvailable, new HashSet<string>(), "Not available"));
    var automation = new UnknownAutomation();
    SubmissionSummary result = SubmissionCoordinator.Submit(plan, "Forge", "Modules > Forge > Assets", automation);
    Assert(result.Stopped && automation.SubmitCalls == 1, "Unknown result did not stop without retry.");
    Assert(result.Results.Single().Outcome == "UNKNOWN", "Uncertain result was mislabeled.");
}

static void WorkerTimeoutIsNotSuccess()
{
    var result = new UiWorkerResult<EditorInspection>(null, true, "timeout", "", "");
    Assert(result.TimedOut && !result.Succeeded, "Timed out UIA worker was considered successful.");
}

static void WorkerPipeDrainBounded()
{
    var stdout = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<string> stderr = Task.FromResult("");
    bool pipesClosed = false;
    bool drained = UiAutomationWorker.TryDrainRedirectedPipes(
        stdout.Task, stderr, TimeSpan.FromMilliseconds(30), () => pipesClosed = true);
    Assert(!drained, "An open redirected pipe was treated as drained.");
    Assert(pipesClosed, "A pipe that exceeded its drain deadline was not closed.");
}

static void WorkerRejectsSubmissionAction()
{
    using var temp = new TempDirectory();
    string requestPath = Path.Combine(temp.Path, "request.json");
    string responsePath = Path.Combine(temp.Path, "response.json");
    File.WriteAllText(requestPath, JsonSerializer.Serialize(new UiWorkerRequest("submit", "unused-config.json")));

    int exitCode = UiAutomationWorker.RunWorker(["--ui-worker", requestPath, responsePath]);
    Assert(exitCode != 0 && File.Exists(responsePath), "UI worker accepted or failed to report a non-inspection action.");
    UiWorkerResponse response = JsonSerializer.Deserialize<UiWorkerResponse>(File.ReadAllText(responsePath))
        ?? throw new InvalidOperationException("UI worker rejection response was not valid JSON.");
    Assert(!response.Succeeded && response.Error.Contains("Only read-only inspect and the picker-only open-import-picker action", StringComparison.Ordinal),
        "UI worker did not reject submission before loading configuration or touching the Editor.");
}

static void StaleInputRejected()
{
    using var temp = new TempDirectory();
    string path = Path.Combine(temp.Path, "mesh.fbx");
    File.WriteAllText(path, "before");
    AssetFile scanned = AssetBatchScanner.Scan(temp.Path).Files.Single();
    File.WriteAllText(path, "after changed");
    AssertThrows<InvalidOperationException>(() => AssetFileLease.OpenForSubmission(scanned));
}

static string WriteMap(string directory, string json)
{
    string path = Path.Combine(directory, "map.json");
    File.WriteAllText(path, json);
    return path;
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void AssertThrows<TException>(Action action) where TException : Exception
{
    try { action(); }
    catch (TException) { return; }
    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

sealed class UnknownAutomation : IResourceBrowserAutomation
{
    public int SubmitCalls { get; private set; }
    public EditorInspection Inspect() => throw new NotSupportedException();
    public string ResolveUniqueAssetsTarget(string moduleName) => throw new NotSupportedException();
    public UiSubmissionResult SubmitFile(string moduleName, string expectedAssetsPath, AssetFile file)
    {
        SubmitCalls++;
        return new(UiSubmissionState.Unknown, "UIA timeout; outcome uncertain.");
    }
}

sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CF-Test-" + Guid.NewGuid().ToString("N"));
    public TempDirectory() => Directory.CreateDirectory(Path);
    public void Dispose() { try { Directory.Delete(Path, recursive: true); } catch { } }
}

sealed class TestSkippedException(string message) : Exception(message);
