using System.Globalization;
using System.IO;
using BannerlordFbxImporter;
using BannerlordFbxImporter.Automation;

if (args.Length > 0 && args[0] == "--ui-worker")
    return UiAutomationWorker.RunWorker(args);

if (args.Length == 0 || args.Any(arg => arg is "--help" or "-h"))
{
    PrintUsage();
    return args.Length == 0 ? 2 : 0;
}

try
{
    CliOptions options = CliOptions.Parse(args);
    if (options.Mode == RunMode.Inspect)
    {
        AppConfig config = AppConfig.Load(options.ConfigPath!);
        UiWorkerResult<EditorInspection> result = UiAutomationWorker.Inspect(options.ConfigPath!, TimeSpan.FromSeconds(config.Timeouts.UiAutomationWorkerSeconds));
        if (result.TimedOut)
        {
            Console.Error.WriteLine($"UNKNOWN: {result.Error}");
            return 3;
        }
        if (!result.Succeeded)
        {
            Console.Error.WriteLine($"INSPECT FAILED: {result.Error}");
            if (!string.IsNullOrWhiteSpace(result.StandardError)) Console.Error.WriteLine(result.StandardError.Trim());
            return 2;
        }
        PrintInspection(result.Value!);
        Console.WriteLine("INSPECT: read-only. No clicks, selections, keystrokes, dialogs, or submissions were sent. UIA ran in a disposable child process.");
        return 0;
    }

    if (options.Mode == RunMode.OpenImportPicker)
    {
        AppConfig config = AppConfig.Load(options.ConfigPath!);
        UiWorkerResult<string> result = UiAutomationWorker.OpenImportPicker(
            options.ConfigPath!, TimeSpan.FromSeconds(config.Timeouts.UiAutomationWorkerSeconds));
        if (result.TimedOut)
        {
            Console.Error.WriteLine($"UNKNOWN: {result.Error}");
            return 3;
        }
        if (!result.Succeeded)
        {
            Console.Error.WriteLine($"PICKER ACTION FAILED: {result.Error}");
            if (!string.IsNullOrWhiteSpace(result.StandardError)) Console.Error.WriteLine(result.StandardError.Trim());
            return result.Error.Contains("UNKNOWN", StringComparison.OrdinalIgnoreCase) ? 3 : 2;
        }
        Console.WriteLine("PICKER_MENU_INVOKED: the unique 'Import new asset' menu item in the configured Resource Browser was invoked.");
        Console.WriteLine("Stopped before file selection. No file was selected and no final Import or Save action was sent. This does not verify that the dialog rendered or that any resource was imported.");
        return 0;
    }

    if (options.Mode == RunMode.RecordCalibration)
        return RecordCalibration(options);

    ImportProfileKind profile = options.Profile!.Value;
    AppConfig dryRunConfig = options.Mode == RunMode.DryRun
        ? string.IsNullOrWhiteSpace(options.ConfigPath) ? new AppConfig() : AppConfig.Load(options.ConfigPath)
        : AppConfig.Load(options.ConfigPath!);
    dryRunConfig.ValidateBasic();
    IReadOnlyList<string> extensions = ProfileConfiguration.ValidateAndGetExtensions(dryRunConfig, profile, forSubmit: false);
    ImportPlan plan = BatchPlanner.Build(options.SourceFolder!, profile, extensions, options.MaterialsManifest, options.TextureAssignmentMap);
    PrintPlan(plan);

    if (options.Mode == RunMode.DryRun)
    {
        Console.WriteLine("DRY-RUN: no UI Automation, clicks, keystrokes, editor access, or writes were performed.");
        return 0;
    }

    Console.WriteLine();
    Console.WriteLine($"Visible module: {options.ModuleName}");
    if (profile != ImportProfileKind.TextureOnly)
    {
        Console.WriteLine($"Submission is LOCKED: profile '{profile.ToId()}' has not been calibrated. The only approved first sample is a manually verified texture-only PNG.");
        Console.WriteLine("No clicks or keyboard input were sent. Assets and AssetSources remain untouched.");
        return 3;
    }

    var evidenceStore = new CalibrationEvidenceStore(CalibrationEvidenceStore.GetDefaultPath(profile.ToId()));
    if (!evidenceStore.TryRead(out CalibrationEvidence? calibration, out string calibrationReason) || calibration is null)
    {
        Console.WriteLine($"Submission is LOCKED: {calibrationReason}");
        Console.WriteLine("No clicks or keyboard input were sent. Assets and AssetSources remain untouched.");
        return 3;
    }

    Dictionary<string, string> observedSettings;
    try
    {
        observedSettings = dryRunConfig.Profiles[profile.ToId()].Settings
            .ToDictionary(setting => setting.Key, setting => setting.Value, StringComparer.Ordinal);
    }
    catch (ArgumentException)
    {
        Console.WriteLine("Submission is LOCKED: configured settings are duplicated and cannot match a verified calibration.");
        return 3;
    }

    var evidenceRequest = new CalibrationEvidenceMatchRequest(
        profile.ToId(),
        options.ModuleName!,
        dryRunConfig.ModuleAssetsDirectory,
        options.CalibrationSamplePath!,
        calibration.ObservedResourceName,
        calibration.ObservedResourceType,
        observedSettings,
        dryRunConfig.ResourceBrowserWindowTitle,
        dryRunConfig.EditorProcessName);
    if (!evidenceStore.IsSubmissionAuthorized(evidenceRequest, out calibrationReason))
    {
        Console.WriteLine($"Submission is LOCKED: {calibrationReason}");
        Console.WriteLine("No clicks or keyboard input were sent. Assets and AssetSources remain untouched.");
        return 3;
    }

    try
    {
        dryRunConfig.ValidateForSubmit(profile, options.ModuleName!);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Submission is LOCKED: {ex.Message}");
        Console.WriteLine("No clicks or keyboard input were sent. Assets and AssetSources remain untouched.");
        return 3;
    }

    Console.WriteLine("Verified sample evidence matches this module, Assets path, PNG hash, observed resource, settings, process, and window.");
    Console.WriteLine("Submission is still LOCKED: the calibrated file-picker/settings/final-Import action sequence and live output verification are not implemented or verified. No UI action was sent.");
    Console.WriteLine("Assets and AssetSources remain untouched.");
    return 3;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERROR: {ex.Message}");
    Console.Error.WriteLine("No import, retry, or dialog dismissal was attempted.");
    return 2;
}

static void PrintPlan(ImportPlan plan)
{
    Console.WriteLine($"Profile: {plan.Profile.ToId()}");
    Console.WriteLine($"Source: {plan.Batch.SourceRoot}");
    Console.WriteLine($"Files: {plan.Assets.Count}; total bytes: {plan.Batch.TotalBytes.ToString("N0", CultureInfo.InvariantCulture)}");
    foreach (string warning in plan.Batch.Warnings) Console.WriteLine($"SCAN WARNING: {warning}");
    if (plan.Profile.IsMesh())
        Console.WriteLine($"Material manifest evidence: {plan.Manifest.Availability} — {plan.Manifest.Detail}");
    else
        Console.WriteLine("Material manifest: not applicable to this texture profile.");

    foreach (PlannedAsset asset in plan.Assets)
    {
        Console.WriteLine($"ASSET: {asset.File.FullPath} ({asset.File.Length.ToString("N0", CultureInfo.InvariantCulture)} bytes)");
        if (asset.Evidence is { } evidence)
        {
            Console.WriteLine($"  FBX text preflight: {evidence.Availability}; material declarations: {(evidence.DeclaredMaterials.Count == 0 ? "(none extracted)" : string.Join(", ", evidence.DeclaredMaterials))}");
            foreach (string warning in evidence.Warnings) Console.WriteLine($"  WARNING: {warning}");
        }
        if (asset.TextureAssignment is { } assignment)
            Console.WriteLine($"  Texture assignment: {assignment.TextureName} -> material '{assignment.MaterialName}', slot '{assignment.Slot}'");
    }
}

static void PrintInspection(EditorInspection inspection)
{
    Console.WriteLine($"Editor process: {inspection.ProcessName} (PID {inspection.ProcessId})");
    Console.WriteLine($"Window: {inspection.WindowName}");
    Console.WriteLine("Visible Assets tree paths:");
    foreach (string path in inspection.AssetsTreePaths) Console.WriteLine($"  {path}");
    Console.WriteLine($"Controls ({inspection.Controls.Count} listed; named and actionable controls shown):");
    foreach (InspectedControl control in inspection.Controls.Where(control =>
        !string.IsNullOrWhiteSpace(control.Name) || control.ControlType is "TreeItem" or "Button" or "Edit" or "ComboBox" or "CheckBox"))
        Console.WriteLine($"  [{control.ControlType}] Name='{control.Name}' AutomationId='{control.AutomationId}' Patterns='{control.Patterns}' Bounds='{control.Bounds}' Path='{control.Path}'");
    foreach (string warning in inspection.Warnings) Console.WriteLine($"WARNING: {warning}");
}

static int RecordCalibration(CliOptions options)
{
    AppConfig config = AppConfig.Load(options.ConfigPath!);
    ImportProfileKind profile = options.Profile!.Value;
    if (profile != ImportProfileKind.TextureOnly)
        throw new InvalidOperationException("Only the approved texture-only PNG sample can be recorded by this workflow.");
    if (!string.Equals(Path.GetFileName(options.CalibrationSamplePath), "calradiaforge_compass.png", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("The initial calibration record is restricted to calradiaforge_compass.png.");
    if (string.IsNullOrWhiteSpace(config.ModuleAssetsDirectory))
        throw new InvalidOperationException("Set moduleAssetsDirectory in appsettings.json to the selected module's direct Assets folder before recording manual verification.");

    string assetsPath = AssetBackupService.ValidateAssetsRoot(config.ModuleAssetsDirectory, options.ModuleName!);
    AssetProfileConfig profileConfig = config.Profiles[profile.ToId()];
    ProfileConfiguration.ValidateSettings(config, profile, forSubmit: false);
    Dictionary<string, string> settings = profileConfig.Settings
        .ToDictionary(setting => setting.Key, setting => setting.Value, StringComparer.Ordinal);
    if (settings.Count == 0)
        throw new InvalidOperationException("The texture-only profile must list the import settings that you personally observed before calibration can be recorded.");

    Console.WriteLine("Manual calibration record (no Editor inspection, UI action, or import will be performed by this command).");
    Console.WriteLine($"Module: {options.ModuleName}");
    Console.WriteLine($"Assets: {assetsPath}");
    Console.WriteLine($"Sample PNG: {options.CalibrationSamplePath}");
    Console.WriteLine($"Observed resource: {options.ObservedResourceName} ({options.ObservedResourceType})");
    Console.WriteLine($"Editor process/window from config: {config.EditorProcessName} / {config.ResourceBrowserWindowTitle}");
    Console.WriteLine("Configured settings to record:");
    foreach ((string key, string value) in settings.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        Console.WriteLine($"  {key} = {value}");
    Console.WriteLine("Only continue if you personally observed this resource name and type in Resource Browser after importing the sample and reviewed every listed setting.");
    Console.Write("Type VERIFIED to record this manual attestation: ");
    if (!CalibrationEvidenceConfirmation.IsAccepted(Console.ReadLine()))
    {
        Console.WriteLine("Calibration evidence was not written.");
        return 3;
    }

    var observation = new CalibrationEvidenceObservation(
        profile.ToId(), options.ModuleName!, assetsPath,
        options.ObservedResourceName!, options.ObservedResourceType!, settings,
        config.ResourceBrowserWindowTitle, config.EditorProcessName, DateTimeOffset.UtcNow);
    CalibrationEvidence evidence = new CalibrationEvidenceStore(CalibrationEvidenceStore.GetDefaultPath(profile.ToId()))
        .SaveAfterManualVerification(observation, options.CalibrationSamplePath!);
    Console.WriteLine($"CALIBRATION_EVIDENCE_RECORDED: {evidence.SampleFileName}; SHA-256 {evidence.SampleSha256}; resource {evidence.ObservedResourceName} ({evidence.ObservedResourceType}).");
    Console.WriteLine("This attestation does not execute, submit, or verify a later import. The automation remains locked until its real UI sequence is calibrated and implemented.");
    return 0;
}

static void PrintUsage()
{
    Console.WriteLine("BannerlordFbxImporter — experimental, process-scoped helper");
    Console.WriteLine("  BannerlordFbxImporter --dry-run --profile <static-mesh|rigged-mesh|texture-only|texture-assign> --source-folder <folder> [--config <json>] [--materials-manifest <json>] [--texture-map <json>]");
    Console.WriteLine("  BannerlordFbxImporter --inspect [--config <json>]");
    Console.WriteLine("  BannerlordFbxImporter --open-import-picker [--config <json>]");
    Console.WriteLine("  BannerlordFbxImporter --record-calibration --profile texture-only --module-name <exact-name> --calibration-sample <calradiaforge_compass.png> --observed-resource-name <name> --observed-resource-type <type> [--config <json>]");
    Console.WriteLine("  BannerlordFbxImporter --submit --profile texture-only --source-folder <folder> --module-name <exact-visible-name> --calibration-sample <verified-png> [--config <json>]");
    Console.WriteLine("--record-calibration only persists a user's explicit manual attestation after the output name/type and settings were inspected. It does not inspect or import assets.");
    Console.WriteLine("--open-import-picker invokes only the unique exact 'Import new asset' context menu item under the configured Resource Browser; it stops before selecting a file.");
    Console.WriteLine("Only file extensions explicitly configured for the selected profile are scanned. --submit requires matching persistent evidence and remains locked until the real UI flow is implemented and verified.");
}

public enum RunMode { DryRun, Inspect, OpenImportPicker, RecordCalibration, Submit }

public sealed class CliOptions
{
    public RunMode Mode { get; private init; }
    public string? SourceFolder { get; private init; }
    public string? ModuleName { get; private init; }
    public ImportProfileKind? Profile { get; private init; }
    public string? MaterialsManifest { get; private init; }
    public string? TextureAssignmentMap { get; private init; }
    public string? CalibrationSamplePath { get; private init; }
    public string? ObservedResourceName { get; private init; }
    public string? ObservedResourceType { get; private init; }
    public string? ConfigPath { get; private init; }

    public static CliOptions Parse(string[] args)
    {
        var modes = new Dictionary<string, RunMode>(StringComparer.OrdinalIgnoreCase)
        {
            ["--dry-run"] = RunMode.DryRun,
            ["--inspect"] = RunMode.Inspect,
            ["--open-import-picker"] = RunMode.OpenImportPicker,
            ["--record-calibration"] = RunMode.RecordCalibration,
            ["--submit"] = RunMode.Submit
        };
        RunMode? mode = null;
        string? source = null, module = null, profileValue = null, manifest = null, textureMap = null, calibrationSample = null,
            observedResourceName = null, observedResourceType = null, config = null;
        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            if (modes.TryGetValue(arg, out RunMode selected))
            {
                if (mode is not null) throw new ArgumentException("Choose exactly one of --dry-run, --inspect, --open-import-picker, --record-calibration, or --submit.");
                mode = selected;
                continue;
            }
            if (arg is "--source-folder" or "--module-name" or "--profile" or "--materials-manifest" or "--texture-map" or "--calibration-sample" or "--observed-resource-name" or "--observed-resource-type" or "--config")
            {
                if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
                    throw new ArgumentException($"Option '{arg}' requires a value.");
                string value = args[index];
                switch (arg)
                {
                    case "--source-folder": source = SetOnce(source, value, arg); break;
                    case "--module-name": module = SetOnce(module, value, arg); break;
                    case "--profile": profileValue = SetOnce(profileValue, value, arg); break;
                    case "--materials-manifest": manifest = SetOnce(manifest, value, arg); break;
                    case "--texture-map": textureMap = SetOnce(textureMap, value, arg); break;
                    case "--calibration-sample": calibrationSample = SetOnce(calibrationSample, value, arg); break;
                    case "--observed-resource-name": observedResourceName = SetOnce(observedResourceName, value, arg); break;
                    case "--observed-resource-type": observedResourceType = SetOnce(observedResourceType, value, arg); break;
                    case "--config": config = SetOnce(config, value, arg); break;
                }
                continue;
            }
            throw new ArgumentException($"Unknown argument '{arg}'. Use --help for usage.");
        }

        if (mode is null) throw new ArgumentException("Choose one explicit mode: --dry-run, --inspect, --open-import-picker, --record-calibration, or --submit.");
        ImportProfileKind? profile = null;
        if (profileValue is not null)
        {
            if (!ImportProfileKinds.TryParse(profileValue, out ImportProfileKind parsed))
                throw new ArgumentException($"Unknown profile '{profileValue}'.");
            profile = parsed;
        }

        if (mode == RunMode.Inspect)
        {
            if (source is not null || module is not null || profile is not null || manifest is not null || textureMap is not null || calibrationSample is not null || observedResourceName is not null || observedResourceType is not null)
                throw new ArgumentException("--inspect accepts only --config.");
        }
        else if (mode == RunMode.OpenImportPicker)
        {
            if (source is not null || module is not null || profile is not null || manifest is not null || textureMap is not null || calibrationSample is not null || observedResourceName is not null || observedResourceType is not null)
                throw new ArgumentException("--open-import-picker accepts only --config; it does not select a file or submit an import.");
        }
        else if (mode == RunMode.RecordCalibration)
        {
            if (source is not null) throw new ArgumentException("--record-calibration does not scan a source folder.");
            if (profile != ImportProfileKind.TextureOnly) throw new ArgumentException("--record-calibration requires --profile texture-only.");
            if (string.IsNullOrWhiteSpace(module)) throw new ArgumentException("--module-name is required for --record-calibration.");
            if (string.IsNullOrWhiteSpace(calibrationSample)) throw new ArgumentException("--calibration-sample is required for --record-calibration.");
            if (string.IsNullOrWhiteSpace(observedResourceName) || string.IsNullOrWhiteSpace(observedResourceType))
                throw new ArgumentException("--observed-resource-name and --observed-resource-type must be copied from the manually verified Resource Browser output.");
            if (manifest is not null || textureMap is not null)
                throw new ArgumentException("Manifest and texture-map options are not valid for --record-calibration.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("--source-folder is required for dry-run and submit modes.");
            if (profile is null) throw new ArgumentException("--profile is required; choose the asset type explicitly.");
            if (mode == RunMode.Submit && string.IsNullOrWhiteSpace(module))
                throw new ArgumentException("--module-name with the exact visible module name is required for --submit.");
            if (mode == RunMode.Submit && string.IsNullOrWhiteSpace(calibrationSample))
                throw new ArgumentException("--calibration-sample is required for --submit; it must be the unchanged PNG that was manually verified in Resource Browser.");
            if (mode == RunMode.Submit && profile != ImportProfileKind.TextureOnly)
                throw new ArgumentException("--submit is currently restricted to the separately calibrated texture-only profile.");
            if (mode != RunMode.Submit && calibrationSample is not null)
                throw new ArgumentException("--calibration-sample is only valid for --submit.");
            if (observedResourceName is not null || observedResourceType is not null)
                throw new ArgumentException("Observed resource identity options are only valid for --record-calibration.");
            if (mode != RunMode.Submit && module is not null)
                throw new ArgumentException("--module-name is only valid with --submit.");
            if (profile.Value.IsMesh() && textureMap is not null)
                throw new ArgumentException("--texture-map is only valid with texture-assign.");
            if (!profile.Value.IsMesh() && manifest is not null)
                throw new ArgumentException("--materials-manifest is only valid with mesh profiles.");
            if (profile.Value == ImportProfileKind.TextureAssign && textureMap is null)
                throw new ArgumentException("--texture-map is required for texture-assign.");
            if (profile.Value != ImportProfileKind.TextureAssign && textureMap is not null)
                throw new ArgumentException("--texture-map is only valid for texture-assign.");
        }

        string? configPath = config is null ? null : Path.GetFullPath(config);
        if (mode == RunMode.Inspect)
            configPath ??= Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (mode is RunMode.OpenImportPicker or RunMode.Submit or RunMode.RecordCalibration)
            configPath ??= Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        return new CliOptions
        {
            Mode = mode.Value,
            SourceFolder = source,
            ModuleName = module,
            Profile = profile,
            MaterialsManifest = manifest,
            TextureAssignmentMap = textureMap,
            CalibrationSamplePath = calibrationSample is null ? null : Path.GetFullPath(calibrationSample),
            ObservedResourceName = observedResourceName,
            ObservedResourceType = observedResourceType,
            ConfigPath = configPath
        };
    }

    private static string SetOnce(string? previous, string value, string option) =>
        previous is null ? value : throw new ArgumentException($"Option '{option}' was provided more than once.");
}
