using System.Text.Json;
using System.IO;

namespace BannerlordFbxImporter;

public enum ImportProfileKind
{
    StaticMesh,
    RiggedMesh,
    TextureOnly,
    TextureAssign
}

public static class ImportProfileKinds
{
    public static bool TryParse(string? value, out ImportProfileKind kind)
    {
        kind = value?.Trim().ToLowerInvariant() switch
        {
            "static-mesh" => ImportProfileKind.StaticMesh,
            "rigged-mesh" => ImportProfileKind.RiggedMesh,
            "texture-only" => ImportProfileKind.TextureOnly,
            "texture-assign" => ImportProfileKind.TextureAssign,
            _ => (ImportProfileKind)(-1)
        };
        return Enum.IsDefined(kind);
    }

    public static string ToId(this ImportProfileKind kind) => kind switch
    {
        ImportProfileKind.StaticMesh => "static-mesh",
        ImportProfileKind.RiggedMesh => "rigged-mesh",
        ImportProfileKind.TextureOnly => "texture-only",
        ImportProfileKind.TextureAssign => "texture-assign",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static bool IsMesh(this ImportProfileKind kind) => kind is ImportProfileKind.StaticMesh or ImportProfileKind.RiggedMesh;
    public static bool IsTexture(this ImportProfileKind kind) => kind is ImportProfileKind.TextureOnly or ImportProfileKind.TextureAssign;
}

public enum ImportWorkflowState
{
    Planned,
    TargetReady,
    SettingsReady,
    Submitted,
    Importing,
    OutputObserved,
    Verified,
    VisualReviewed,
    Stopped,
    Unknown
}

public sealed record ImportStateTransition(ImportWorkflowState State, DateTimeOffset AtUtc, string Evidence);

/// <summary>Enforces that no successful state can be skipped or inferred.</summary>
public sealed class ImportStateMachine
{
    private static readonly IReadOnlyDictionary<ImportWorkflowState, ImportWorkflowState[]> Allowed =
        new Dictionary<ImportWorkflowState, ImportWorkflowState[]>
        {
            [ImportWorkflowState.Planned] = [ImportWorkflowState.TargetReady, ImportWorkflowState.Stopped, ImportWorkflowState.Unknown],
            [ImportWorkflowState.TargetReady] = [ImportWorkflowState.SettingsReady, ImportWorkflowState.Stopped, ImportWorkflowState.Unknown],
            [ImportWorkflowState.SettingsReady] = [ImportWorkflowState.Submitted, ImportWorkflowState.Stopped, ImportWorkflowState.Unknown],
            [ImportWorkflowState.Submitted] = [ImportWorkflowState.Importing, ImportWorkflowState.Stopped, ImportWorkflowState.Unknown],
            [ImportWorkflowState.Importing] = [ImportWorkflowState.OutputObserved, ImportWorkflowState.Stopped, ImportWorkflowState.Unknown],
            [ImportWorkflowState.OutputObserved] = [ImportWorkflowState.Verified, ImportWorkflowState.Stopped, ImportWorkflowState.Unknown],
            [ImportWorkflowState.Verified] = [ImportWorkflowState.VisualReviewed, ImportWorkflowState.Stopped, ImportWorkflowState.Unknown],
            [ImportWorkflowState.VisualReviewed] = [],
            [ImportWorkflowState.Stopped] = [],
            [ImportWorkflowState.Unknown] = []
        };

    private readonly List<ImportStateTransition> _history = new();
    public ImportWorkflowState Current { get; private set; } = ImportWorkflowState.Planned;
    public IReadOnlyList<ImportStateTransition> History => _history;

    public ImportStateMachine(string initialEvidence = "Batch scanned and awaiting Editor target.") =>
        _history.Add(new(Current, DateTimeOffset.UtcNow, initialEvidence));

    public void MoveTo(ImportWorkflowState next, string evidence)
    {
        if (string.IsNullOrWhiteSpace(evidence))
            throw new ArgumentException("Every workflow transition needs explicit evidence.", nameof(evidence));
        if (!Allowed[Current].Contains(next))
            throw new InvalidOperationException($"Invalid import state transition: {Current} -> {next}.");
        Current = next;
        _history.Add(new(next, DateTimeOffset.UtcNow, evidence));
    }
}

public sealed record ExpectedResource(string Name, string Type);
public sealed record ResourceInventoryEntry(string Name, string Type);
public sealed record TextureAssignment(string SourceFile, string TextureName, string MaterialName, string Slot);

public sealed record TextureAssignmentMap(IReadOnlyDictionary<string, TextureAssignment> Assignments)
{
    public static TextureAssignmentMap Load(string path, IReadOnlyList<Preflight.AssetFile> files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(files);
        var info = new FileInfo(path);
        if (!info.Exists) throw new FileNotFoundException("Texture assignment map was not found.", path);
        if (info.Length > 1024 * 1024) throw new InvalidDataException("Texture assignment map exceeds 1 MiB.");

        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path), new JsonDocumentOptions { MaxDepth = 12 });
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Texture assignment map must be a JSON array.");

        var entries = new Dictionary<string, TextureAssignment>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement item in document.RootElement.EnumerateArray())
        {
            if (entries.Count >= 100)
                throw new InvalidDataException("Texture assignment map cannot contain more than 100 entries.");
            if (item.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Each texture assignment must be an object.");
            string sourceFile = RequiredString(item, "sourceFile");
            string textureName = RequiredString(item, "textureName");
            string materialName = RequiredString(item, "materialName");
            string slot = RequiredString(item, "slot");
            if (Path.GetFileName(sourceFile) != sourceFile || sourceFile.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidDataException($"Assignment sourceFile must be a single file name: '{sourceFile}'.");
            ValidateResourceValue(textureName, "textureName");
            ValidateResourceValue(materialName, "materialName");
            ValidateResourceValue(slot, "slot");
            if (!entries.TryAdd(sourceFile, new(sourceFile, textureName, materialName, slot)))
                throw new InvalidDataException($"Duplicate assignment for source file '{sourceFile}'.");
        }

        var scanned = files.Select(file => Path.GetFileName(file.FullPath)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        string[] missing = scanned.Where(name => !entries.ContainsKey(name)).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        string[] extra = entries.Keys.Where(name => !scanned.Contains(name)).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        if (missing.Length != 0 || extra.Length != 0)
            throw new InvalidDataException($"Texture map must match the batch exactly. Missing: [{string.Join(", ", missing)}]; not in batch: [{string.Join(", ", extra)}].");

        return new(entries);
    }

    private static string RequiredString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out JsonElement value) || value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()) || value.GetString()!.Trim() != value.GetString())
            throw new InvalidDataException($"Texture assignment property '{property}' must be a non-empty, trimmed string.");
        return value.GetString()!;
    }

    private static void ValidateResourceValue(string value, string property)
    {
        if (value.Length > 256 || value.Any(char.IsControl))
            throw new InvalidDataException($"Texture assignment property '{property}' exceeds 256 characters or contains control characters.");
    }
}

public sealed class AssetProfileConfig
{
    public bool CalibrationReviewed { get; set; }
    public string CalibrationNote { get; set; } = "";
    public List<string> SupportedExtensions { get; set; } = new();
    public bool ResourceInventoryComplete { get; set; }
    public List<ImportSettingConfig> Settings { get; set; } = new();
}

public sealed class ImportSettingConfig
{
    public string Key { get; set; } = "";
    public string Operation { get; set; } = "value";
    public string Value { get; set; } = "";
    public AutomationSelector Selector { get; set; } = new();
}

public enum ImportProfileOperation { ApplySettings, AssignTexture }

public static class ProfileConfiguration
{
    public static IReadOnlyList<string> ValidateAndGetExtensions(AppConfig config, ImportProfileKind kind, bool forSubmit)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.ValidateBasic();
        AssetProfileConfig profile = config.Profiles[kind.ToId()];
        if (forSubmit && !profile.CalibrationReviewed)
            throw new InvalidOperationException($"Profile '{kind.ToId()}' is not calibrated. Inspect the real Editor, manually test and review one sample, then record the observed settings before enabling submission.");
        if (forSubmit && string.IsNullOrWhiteSpace(profile.CalibrationNote))
            throw new InvalidOperationException($"Profile '{kind.ToId()}' needs a calibration note describing the observed Editor version and reviewed sample.");
        if (forSubmit && !profile.ResourceInventoryComplete)
            throw new InvalidOperationException($"Profile '{kind.ToId()}' has no reviewed complete Resource Browser inventory configuration; submission is blocked.");

        if (kind.IsMesh())
        {
            if (profile.SupportedExtensions.Count > 0 &&
                profile.SupportedExtensions.Any(extension => !string.Equals(extension, ".fbx", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Profile '{kind.ToId()}' accepts FBX only.");
            return new[] { ".fbx" };
        }

        string[] textureExtensions = profile.SupportedExtensions
            .Select(NormalizeExtension)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (textureExtensions.Length == 0)
            throw new InvalidOperationException("Texture profiles have no observed file-dialog extensions configured. Run --inspect and record only formats shown by the Editor.");
        return textureExtensions;
    }

    public static void ValidateSettings(AppConfig config, ImportProfileKind kind, bool forSubmit)
    {
        AssetProfileConfig profile = config.Profiles[kind.ToId()];
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ImportSettingConfig setting in profile.Settings)
        {
            setting.Selector ??= new AutomationSelector();
            setting.Key = setting.Key?.Trim() ?? "";
            setting.Operation = setting.Operation?.Trim().ToLowerInvariant() ?? "";
            setting.Value ??= "";
            setting.Selector.Normalize();
            if (setting.Key.Length == 0 || !keys.Add(setting.Key))
                throw new InvalidOperationException($"Profile '{kind.ToId()}' contains an empty or duplicate setting key.");
            if (setting.Operation is not ("value" or "choice" or "toggle"))
                throw new InvalidOperationException($"Setting '{setting.Key}' must use operation value, choice, or toggle.");
            if (setting.Value.Length == 0)
                throw new InvalidOperationException($"Setting '{setting.Key}' has an empty configured value.");
            if (forSubmit) setting.Selector.RequireConfigured($"{kind.ToId()}.{setting.Key}");
        }
        if (forSubmit && profile.Settings.Count == 0)
            throw new InvalidOperationException($"Profile '{kind.ToId()}' has no observed import settings. Submission remains disabled until the settings screen is calibrated.");
    }

    public static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) throw new InvalidOperationException("Observed texture extension is empty.");
        string normalized = extension.Trim().ToLowerInvariant();
        if (normalized.Length is < 2 or > 12 || normalized[0] != '.' ||
            normalized[1..].Any(character => !char.IsAsciiLetterOrDigit(character)))
            throw new InvalidOperationException($"Texture extension '{extension}' is invalid; record only extensions observed in the Resource Browser file filter.");
        return normalized;
    }
}
