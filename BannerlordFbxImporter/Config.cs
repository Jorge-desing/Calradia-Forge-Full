using System.Text.Json;
using System.IO;

namespace BannerlordFbxImporter;

public sealed class AutomationSelector
{
    public string AutomationId { get; set; } = "";
    public string Name { get; set; } = "";
    public string ControlType { get; set; } = "";
    public bool MatchAny { get; set; }
    public bool IsConfigured => !string.IsNullOrWhiteSpace(AutomationId) || !string.IsNullOrWhiteSpace(Name);

    internal void Normalize()
    {
        AutomationId ??= "";
        Name ??= "";
        ControlType ??= "";
    }

    public void RequireConfigured(string label)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(ControlType))
            throw new InvalidOperationException($"Selector '{label}' must specify AutomationId or exact Name, plus ControlType. Use --inspect to discover controls.");
    }
}

public sealed class AutomationSettings
{
    public AutomationSelector ImportButton { get; set; } = new();
    public AutomationSelector OpenFileDialog { get; set; } = new();
    public AutomationSelector FileNameEdit { get; set; } = new();
    public AutomationSelector OpenFileConfirmButton { get; set; } = new();
    public AutomationSelector SettingsDialog { get; set; } = new();
    public AutomationSelector FinalImportButton { get; set; } = new();
    public AutomationSelector ResourceInventoryRoot { get; set; } = new();
    public AutomationSelector ResourceInventoryItem { get; set; } = new();
    public AutomationSelector ResourceTypeLabel { get; set; } = new();
    public AutomationSelector AssignMaterialName { get; set; } = new();
    public AutomationSelector AssignTextureSlot { get; set; } = new();
    public AutomationSelector AssignTextureResource { get; set; } = new();
    public AutomationSelector AssignTextureButton { get; set; } = new();
    public AutomationSelector SaveMaterialButton { get; set; } = new();

    public void Normalize()
    {
        ImportButton ??= new AutomationSelector();
        OpenFileDialog ??= new AutomationSelector();
        FileNameEdit ??= new AutomationSelector();
        OpenFileConfirmButton ??= new AutomationSelector();
        SettingsDialog ??= new AutomationSelector();
        FinalImportButton ??= new AutomationSelector();
        ResourceInventoryRoot ??= new AutomationSelector();
        ResourceInventoryItem ??= new AutomationSelector();
        ResourceTypeLabel ??= new AutomationSelector();
        AssignMaterialName ??= new AutomationSelector();
        AssignTextureSlot ??= new AutomationSelector();
        AssignTextureResource ??= new AutomationSelector();
        AssignTextureButton ??= new AutomationSelector();
        SaveMaterialButton ??= new AutomationSelector();
        ImportButton.Normalize();
        OpenFileDialog.Normalize();
        FileNameEdit.Normalize();
        OpenFileConfirmButton.Normalize();
        SettingsDialog.Normalize();
        FinalImportButton.Normalize();
        ResourceInventoryRoot.Normalize();
        ResourceInventoryItem.Normalize();
        ResourceTypeLabel.Normalize();
        AssignMaterialName.Normalize();
        AssignTextureSlot.Normalize();
        AssignTextureResource.Normalize();
        AssignTextureButton.Normalize();
        SaveMaterialButton.Normalize();
    }

    public void ValidateForSubmit(ImportProfileKind kind)
    {
        Normalize();
        ImportButton.RequireConfigured(nameof(ImportButton));
        OpenFileDialog.RequireConfigured(nameof(OpenFileDialog));
        FileNameEdit.RequireConfigured(nameof(FileNameEdit));
        OpenFileConfirmButton.RequireConfigured(nameof(OpenFileConfirmButton));
        SettingsDialog.RequireConfigured(nameof(SettingsDialog));
        FinalImportButton.RequireConfigured(nameof(FinalImportButton));
        ResourceInventoryRoot.RequireConfigured(nameof(ResourceInventoryRoot));
        ResourceInventoryItem.RequireConfigured(nameof(ResourceInventoryItem));
        ResourceTypeLabel.RequireConfigured(nameof(ResourceTypeLabel));
        if (kind == ImportProfileKind.TextureAssign)
        {
            AssignMaterialName.RequireConfigured(nameof(AssignMaterialName));
            AssignTextureSlot.RequireConfigured(nameof(AssignTextureSlot));
            AssignTextureResource.RequireConfigured(nameof(AssignTextureResource));
            AssignTextureButton.RequireConfigured(nameof(AssignTextureButton));
            SaveMaterialButton.RequireConfigured(nameof(SaveMaterialButton));
        }
    }

    public IEnumerable<AutomationSelector> AllSelectors()
    {
        yield return ImportButton;
        yield return OpenFileDialog;
        yield return FileNameEdit;
        yield return OpenFileConfirmButton;
        yield return SettingsDialog;
        yield return FinalImportButton;
        yield return ResourceInventoryRoot;
        yield return ResourceInventoryItem;
        yield return ResourceTypeLabel;
        yield return AssignMaterialName;
        yield return AssignTextureSlot;
        yield return AssignTextureResource;
        yield return AssignTextureButton;
        yield return SaveMaterialButton;
    }
}

public sealed class Timeouts
{
    public int WindowReadySeconds { get; set; } = 15;
    public int ImportPerFileMaxSeconds { get; set; } = 180;
    public int UiAutomationWorkerSeconds { get; set; } = 240;
    public int PollIntervalMs { get; set; } = 200;

    public void Validate()
    {
        if (WindowReadySeconds is < 1 or > 120)
            throw new InvalidOperationException("timeouts.windowReadySeconds must be between 1 and 120.");
        if (ImportPerFileMaxSeconds is < 1 or > 600)
            throw new InvalidOperationException("timeouts.importPerFileMaxSeconds must be between 1 and 600.");
        if (UiAutomationWorkerSeconds is < 5 or > 900)
            throw new InvalidOperationException("timeouts.uiAutomationWorkerSeconds must be between 5 and 900.");
        if (PollIntervalMs is < 50 or > 2000)
            throw new InvalidOperationException("timeouts.pollIntervalMs must be between 50 and 2000.");
    }
}

public sealed class AppConfig
{
    // Observed on this installation only. This is configurable and never causes
    // a global window search or an automatic Editor launch.
    public string EditorProcessName { get; set; } = "TaleWorlds.MountAndBlade.Launcher";
    public string ResourceBrowserWindowTitle { get; set; } = "Resource Browser";
    public AutomationSettings Automation { get; set; } = new();
    public Timeouts Timeouts { get; set; } = new();
    public Dictionary<string, AssetProfileConfig> Profiles { get; set; } = CreateDefaultProfiles();
    public string ModuleAssetsDirectory { get; set; } = "";
    public bool EditorCalibrationReviewed { get; set; }

    public void ValidateBasic()
    {
        if (string.IsNullOrWhiteSpace(EditorProcessName) ||
            EditorProcessName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            EditorProcessName.Contains(Path.DirectorySeparatorChar) ||
            EditorProcessName.Contains(Path.AltDirectorySeparatorChar) ||
            EditorProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("editorProcessName must be a process name without a path or .exe suffix.");
        if (string.IsNullOrWhiteSpace(ResourceBrowserWindowTitle) ||
            !string.Equals(ResourceBrowserWindowTitle, ResourceBrowserWindowTitle.Trim(), StringComparison.Ordinal) ||
            ResourceBrowserWindowTitle.Any(char.IsControl))
            throw new InvalidOperationException("resourceBrowserWindowTitle must be a non-empty, exact, trimmed window title without control characters.");
        Automation ??= new AutomationSettings();
        Timeouts ??= new Timeouts();
        Profiles ??= CreateDefaultProfiles();
        var normalizedProfiles = new Dictionary<string, AssetProfileConfig>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in Profiles)
        {
            if (!ProfileIds.Contains(pair.Key, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unknown asset profile '{pair.Key}'.");
            if (!normalizedProfiles.TryAdd(pair.Key, pair.Value ?? new AssetProfileConfig()))
                throw new InvalidOperationException($"Duplicate asset profile '{pair.Key}'.");
        }
        Profiles = normalizedProfiles;
        Automation.Normalize();
        foreach (string id in ProfileIds)
            if (!Profiles.ContainsKey(id)) Profiles[id] = new AssetProfileConfig();
        foreach (string id in Profiles.Keys.ToArray())
        {
            AssetProfileConfig profile = Profiles[id] ?? new AssetProfileConfig();
            profile.CalibrationNote ??= "";
            profile.SupportedExtensions ??= new List<string>();
            profile.Settings ??= new List<ImportSettingConfig>();
            Profiles[id] = profile;
        }
        ModuleAssetsDirectory ??= "";
        Timeouts.Validate();
    }

    public void ValidateForSubmit(ImportProfileKind kind, string moduleName)
    {
        ValidateBasic();
        if (!EditorCalibrationReviewed)
            throw new InvalidOperationException("Editor calibration has not been reviewed. A manually verified, integrity-checked sample record is also required; appsettings flags alone never authorize submission.");
        _ = ProfileConfiguration.ValidateAndGetExtensions(this, kind, forSubmit: true);
        ProfileConfiguration.ValidateSettings(this, kind, forSubmit: true);
        Automation.ValidateForSubmit(kind);
        foreach (AutomationSelector selector in Automation.AllSelectors())
        {
            if (selector.IsConfigured || selector.MatchAny)
                _ = WindowsEditorAutomation.ParseConfiguredControlType(selector.ControlType);
        }
        foreach (ImportSettingConfig setting in Profiles[kind.ToId()].Settings)
            _ = WindowsEditorAutomation.ParseConfiguredControlType(setting.Selector.ControlType);
        if (string.IsNullOrWhiteSpace(ModuleAssetsDirectory))
            throw new InvalidOperationException("moduleAssetsDirectory must point to the selected module's on-disk Assets folder so a replacement can be backed up and verified.");
        _ = AssetBackupService.ValidateAssetsRoot(ModuleAssetsDirectory, moduleName);
    }

    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Configuration file was not found. No editor interaction was attempted.", path);
        if (new FileInfo(path).Length > 1024 * 1024)
            throw new InvalidDataException("Configuration exceeds the 1 MiB limit.");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = false,
            ReadCommentHandling = JsonCommentHandling.Disallow
        };
        var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), options)
            ?? throw new InvalidDataException("Configuration JSON did not contain an object.");
        config.Automation ??= new AutomationSettings();
        config.Timeouts ??= new Timeouts();
        config.Profiles ??= CreateDefaultProfiles();
        config.ValidateBasic();
        return config;
    }

    public static string[] ProfileIds => ["static-mesh", "rigged-mesh", "texture-only", "texture-assign"];

    private static Dictionary<string, AssetProfileConfig> CreateDefaultProfiles() =>
        ProfileIds.ToDictionary(id => id, _ => new AssetProfileConfig(), StringComparer.OrdinalIgnoreCase);
}
