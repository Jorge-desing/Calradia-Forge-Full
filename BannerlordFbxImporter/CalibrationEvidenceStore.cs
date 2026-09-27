using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using BannerlordFbxImporter.Preflight;

namespace BannerlordFbxImporter;

/// <summary>Observed Resource Browser values captured only after an operator verified the imported sample.</summary>
public sealed record CalibrationEvidenceObservation(
    string Profile,
    string ModuleName,
    string AssetsPath,
    string ObservedResourceName,
    string ObservedResourceType,
    IReadOnlyDictionary<string, string> ObservedSettings,
    string WindowTitle,
    string ProcessName,
    DateTimeOffset ObservedAtUtc);

/// <summary>Exact current conditions required before a caller may enable submissions for a calibrated profile.</summary>
public sealed record CalibrationEvidenceMatchRequest(
    string Profile,
    string ModuleName,
    string AssetsPath,
    string SampleFilePath,
    string ObservedResourceName,
    string ObservedResourceType,
    IReadOnlyDictionary<string, string> ObservedSettings,
    string WindowTitle,
    string ProcessName);

/// <summary>Integrity-checked calibration evidence. SHA-256 detects accidental or uncoordinated edits; it is not a signature.</summary>
public sealed record CalibrationEvidence(
    int SchemaVersion,
    string Profile,
    string ModuleName,
    string AssetsPath,
    string SampleFileName,
    string SampleSha256,
    long SampleSizeBytes,
    string ObservedResourceName,
    string ObservedResourceType,
    IReadOnlyDictionary<string, string> ObservedSettings,
    string WindowTitle,
    string ProcessName,
    DateTimeOffset ObservedAtUtc,
    string IntegritySha256);

/// <summary>
/// Persists explicit calibration evidence independently of appsettings booleans. Submission authorization requires
/// a valid record, an unchanged sample PNG, and an exact match to the currently observed profile and Editor context.
/// </summary>
public sealed class CalibrationEvidenceStore
{
    public const int CurrentSchemaVersion = 1;
    public const long MaximumSampleBytes = 256L * 1024 * 1024;
    private const long MaximumStoreBytes = 1024 * 1024;
    private const int MaximumSettings = 128;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true
    };

    private readonly string _path;

    public CalibrationEvidenceStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = System.IO.Path.GetFullPath(path);
    }

    public string Path => _path;

    public static string GetDefaultPath(string profile, string? localAppDataRoot = null)
    {
        if (!ImportProfileKinds.TryParse(profile, out ImportProfileKind parsed) ||
            !string.Equals(profile, parsed.ToId(), StringComparison.Ordinal))
            throw new ArgumentException("Profile must be an exact supported profile identifier.", nameof(profile));
        string root = localAppDataRoot ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root)) throw new InvalidOperationException("Local application data directory is unavailable.");
        return System.IO.Path.GetFullPath(System.IO.Path.Combine(root, "CalradiaForge", "Importer", "Calibration", parsed.ToId() + ".json"));
    }

    /// <summary>
    /// Records an operator-verified sample. Callers must only invoke this after the output name and type were observed
    /// in Resource Browser and explicitly reviewed; this method itself never accesses the Editor or performs imports.
    /// </summary>
    public CalibrationEvidence SaveAfterManualVerification(CalibrationEvidenceObservation observation, string samplePngPath)
    {
        ArgumentNullException.ThrowIfNull(observation);
        CalibrationEvidencePayload payload = CreatePayload(observation, samplePngPath);
        string checksum = ComputeIntegrity(payload);
        var document = new CalibrationEvidenceDocument(payload, checksum);
        WriteAtomically(document);
        return ToEvidence(payload, checksum);
    }

    public bool TryRead(out CalibrationEvidence? evidence, out string reason)
    {
        evidence = null;
        reason = "Calibration evidence is unavailable.";
        try
        {
            if (!File.Exists(_path))
            {
                reason = "Calibration evidence file does not exist.";
                return false;
            }

            var info = new FileInfo(_path);
            info.Refresh();
            if (info.Length <= 0 || info.Length > MaximumStoreBytes)
            {
                reason = $"Calibration evidence size must be between 1 byte and {MaximumStoreBytes} bytes.";
                return false;
            }

            CalibrationEvidenceDocument? document = JsonSerializer.Deserialize<CalibrationEvidenceDocument>(File.ReadAllBytes(_path), JsonOptions);
            if (document?.Evidence is null || string.IsNullOrWhiteSpace(document.IntegritySha256))
            {
                reason = "Calibration evidence document is incomplete.";
                return false;
            }

            CalibrationEvidencePayload payload = NormalizeAndValidate(document.Evidence);
            string calculated = ComputeIntegrity(payload);
            if (!FixedTimeHexEquals(calculated, document.IntegritySha256))
            {
                reason = "Calibration evidence SHA-256 integrity check failed.";
                return false;
            }

            evidence = ToEvidence(payload, calculated);
            reason = "Calibration evidence is valid.";
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or ArgumentException or InvalidOperationException or NotSupportedException or FormatException or OverflowException)
        {
            reason = $"Calibration evidence is invalid: {ex.Message}";
            return false;
        }
    }

    public bool IsSubmissionAuthorized(CalibrationEvidenceMatchRequest request, out string reason)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!TryRead(out CalibrationEvidence? evidence, out reason)) return false;

        try
        {
            CalibrationEvidencePayload expected = CreatePayload(
                new CalibrationEvidenceObservation(
                    request.Profile,
                    request.ModuleName,
                    request.AssetsPath,
                    request.ObservedResourceName,
                    request.ObservedResourceType,
                    request.ObservedSettings,
                    request.WindowTitle,
                    request.ProcessName,
                    evidence!.ObservedAtUtc),
                request.SampleFilePath);

            if (!PayloadMatches(evidence!, expected))
            {
                reason = "Current profile, module, Assets path, sample, observed resource, settings, window, or process does not exactly match the verified calibration.";
                return false;
            }

            reason = "Verified calibration matches the current submission context.";
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or InvalidOperationException or FormatException or OverflowException)
        {
            reason = $"Current submission context does not match a valid calibration: {ex.Message}";
            return false;
        }
    }

    public void RequireSubmissionEvidence(CalibrationEvidenceMatchRequest request)
    {
        if (!IsSubmissionAuthorized(request, out string reason))
            throw new InvalidOperationException("Submission is blocked. " + reason);
    }

    private void WriteAtomically(CalibrationEvidenceDocument document)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(document, JsonOptions);
        if (bytes.Length > MaximumStoreBytes)
            throw new InvalidDataException($"Calibration evidence exceeds the {MaximumStoreBytes}-byte limit.");

        string directory = System.IO.Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        string temporary = System.IO.Path.Combine(directory, $".{System.IO.Path.GetFileName(_path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(_path))
                File.Replace(temporary, _path, destinationBackupFileName: null, ignoreMetadataErrors: true);
            else
                File.Move(temporary, _path);
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
        }
    }

    private static CalibrationEvidencePayload CreatePayload(CalibrationEvidenceObservation observation, string samplePngPath)
    {
        if (!ImportProfileKinds.TryParse(observation.Profile, out ImportProfileKind profile) ||
            !string.Equals(observation.Profile, profile.ToId(), StringComparison.Ordinal))
            throw new InvalidDataException("Calibration profile must be one exact supported profile identifier.");
        string moduleName = RequireText(observation.ModuleName, nameof(observation.ModuleName), 256);
        string assetsPath = AssetBackupService.ValidateAssetsRoot(observation.AssetsPath, moduleName);
        string samplePath = ValidateSamplePath(samplePngPath);
        (long size, string hash) = HashSample(samplePath);
        DateTimeOffset observedAt = observation.ObservedAtUtc.ToUniversalTime();
        if (observation.ObservedAtUtc == default || observedAt > DateTimeOffset.UtcNow.AddMinutes(5))
            throw new InvalidDataException("Calibration timestamp is missing or is unexpectedly in the future.");

        return new CalibrationEvidencePayload(
            CurrentSchemaVersion,
            profile.ToId(),
            moduleName,
            assetsPath,
            System.IO.Path.GetFileName(samplePath),
            hash,
            size,
            RequireText(observation.ObservedResourceName, nameof(observation.ObservedResourceName), 256),
            RequireText(observation.ObservedResourceType, nameof(observation.ObservedResourceType), 128),
            NormalizeSettings(observation.ObservedSettings),
            RequireText(observation.WindowTitle, nameof(observation.WindowTitle), 256),
            ValidateProcessName(observation.ProcessName),
            observedAt);
    }

    private static CalibrationEvidencePayload NormalizeAndValidate(CalibrationEvidencePayload payload)
    {
        if (payload.SchemaVersion != CurrentSchemaVersion)
            throw new InvalidDataException($"Unsupported calibration evidence schema version {payload.SchemaVersion}.");
        if (!ImportProfileKinds.TryParse(payload.Profile, out ImportProfileKind profile) ||
            !string.Equals(payload.Profile, profile.ToId(), StringComparison.Ordinal))
            throw new InvalidDataException("Calibration profile is not a canonical supported identifier.");
        string moduleName = RequireText(payload.ModuleName, nameof(payload.ModuleName), 256);
        string assetsPath = AssetBackupService.ValidateAssetsRoot(payload.AssetsPath, moduleName);
        string sampleName = RequireText(payload.SampleFileName, nameof(payload.SampleFileName), 255);
        if (!string.Equals(System.IO.Path.GetFileName(sampleName), sampleName, StringComparison.Ordinal) ||
            !string.Equals(System.IO.Path.GetExtension(sampleName), ".png", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Calibration sample must be a single PNG file name.");
        if (payload.SampleSizeBytes is < 8 or > MaximumSampleBytes)
            throw new InvalidDataException($"Calibration sample size must be between 8 and {MaximumSampleBytes} bytes.");
        string sampleHash = NormalizeHash(payload.SampleSha256, "sampleSha256");
        DateTimeOffset timestamp = payload.ObservedAtUtc.ToUniversalTime();
        if (payload.ObservedAtUtc == default || timestamp > DateTimeOffset.UtcNow.AddMinutes(5))
            throw new InvalidDataException("Calibration timestamp is missing or is unexpectedly in the future.");

        return new CalibrationEvidencePayload(
            CurrentSchemaVersion,
            profile.ToId(),
            moduleName,
            assetsPath,
            sampleName,
            sampleHash,
            payload.SampleSizeBytes,
            RequireText(payload.ObservedResourceName, nameof(payload.ObservedResourceName), 256),
            RequireText(payload.ObservedResourceType, nameof(payload.ObservedResourceType), 128),
            NormalizeSettings(payload.ObservedSettings),
            RequireText(payload.WindowTitle, nameof(payload.WindowTitle), 256),
            ValidateProcessName(payload.ProcessName),
            timestamp);
    }

    private static string ValidateSamplePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = System.IO.Path.GetFullPath(path);
        if (!string.Equals(System.IO.Path.GetExtension(fullPath), ".png", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Calibration sample must be a PNG file.");
        FileAttributes attributes = File.GetAttributes(fullPath);
        if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            throw new InvalidDataException("Calibration sample must be a regular file, not a directory or reparse point.");
        return fullPath;
    }

    private static (long Size, string Sha256) HashSample(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
        long expectedSize = stream.Length;
        if (expectedSize is < 8 or > MaximumSampleBytes)
            throw new InvalidDataException($"Calibration sample size must be between 8 and {MaximumSampleBytes} bytes.");
        Span<byte> signature = stackalloc byte[8];
        if (stream.Read(signature) != signature.Length || !signature.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            throw new InvalidDataException("Calibration sample does not contain the PNG signature.");
        stream.Position = 0;
        byte[] hash = SHA256.HashData(stream);
        if (stream.Length != expectedSize)
            throw new IOException("Calibration sample changed while its hash was being computed.");
        return (expectedSize, Convert.ToHexString(hash));
    }

    private static IReadOnlyDictionary<string, string> NormalizeSettings(IReadOnlyDictionary<string, string>? settings)
    {
        if (settings is null || settings.Count is < 1 or > MaximumSettings)
            throw new InvalidDataException($"Calibration must contain between 1 and {MaximumSettings} observed settings.");
        var normalized = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach ((string key, string value) in settings)
        {
            string validKey = RequireText(key, "setting key", 128);
            string validValue = RequireText(value, $"setting '{validKey}' value", 1024);
            if (!normalized.TryAdd(validKey, validValue))
                throw new InvalidDataException($"Calibration setting '{validKey}' is duplicated.");
        }
        return normalized;
    }

    private static string RequireText(string? value, string label, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal) || value.Any(char.IsControl))
            throw new InvalidDataException($"{label} must be non-empty, trimmed, control-character free, and at most {maximumLength} characters.");
        return value;
    }

    private static string ValidateProcessName(string? processName)
    {
        string value = RequireText(processName, nameof(processName), 128);
        if (value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || value.Contains('/') || value.Contains('\\'))
            throw new InvalidDataException("Process name must be an executable process name without a path or .exe suffix.");
        return value;
    }

    private static string NormalizeHash(string? hash, string label)
    {
        if (hash is null || hash.Length != 64 || hash.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidDataException($"{label} must be a 64-character SHA-256 hex digest.");
        return hash.ToUpperInvariant();
    }

    private static bool PayloadMatches(CalibrationEvidence evidence, CalibrationEvidencePayload expected)
    {
        return evidence.SchemaVersion == expected.SchemaVersion &&
            string.Equals(evidence.Profile, expected.Profile, StringComparison.Ordinal) &&
            string.Equals(evidence.ModuleName, expected.ModuleName, StringComparison.Ordinal) &&
            string.Equals(evidence.AssetsPath, expected.AssetsPath, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(evidence.SampleFileName, expected.SampleFileName, StringComparison.Ordinal) &&
            evidence.SampleSizeBytes == expected.SampleSizeBytes &&
            FixedTimeHexEquals(evidence.SampleSha256, expected.SampleSha256) &&
            string.Equals(evidence.ObservedResourceName, expected.ObservedResourceName, StringComparison.Ordinal) &&
            string.Equals(evidence.ObservedResourceType, expected.ObservedResourceType, StringComparison.Ordinal) &&
            SettingsEqual(evidence.ObservedSettings, expected.ObservedSettings) &&
            string.Equals(evidence.WindowTitle, expected.WindowTitle, StringComparison.Ordinal) &&
            string.Equals(evidence.ProcessName, expected.ProcessName, StringComparison.Ordinal);
    }

    private static bool SettingsEqual(IReadOnlyDictionary<string, string> left, IReadOnlyDictionary<string, string> right)
    {
        if (left.Count != right.Count) return false;
        foreach ((string key, string value) in left)
            if (!right.TryGetValue(key, out string? other) || !string.Equals(value, other, StringComparison.Ordinal)) return false;
        return true;
    }

    private static string ComputeIntegrity(CalibrationEvidencePayload payload) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions)));

    private static bool FixedTimeHexEquals(string left, string right)
    {
        try
        {
            byte[] leftBytes = Convert.FromHexString(left);
            byte[] rightBytes = Convert.FromHexString(right);
            return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
        catch (FormatException) { return false; }
    }

    private static CalibrationEvidence ToEvidence(CalibrationEvidencePayload payload, string integrity) => new(
        payload.SchemaVersion,
        payload.Profile,
        payload.ModuleName,
        payload.AssetsPath,
        payload.SampleFileName,
        payload.SampleSha256,
        payload.SampleSizeBytes,
        payload.ObservedResourceName,
        payload.ObservedResourceType,
        new SortedDictionary<string, string>(payload.ObservedSettings.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal), StringComparer.Ordinal),
        payload.WindowTitle,
        payload.ProcessName,
        payload.ObservedAtUtc,
        integrity);

    private sealed record CalibrationEvidenceDocument(
        [property: JsonPropertyOrder(0)] CalibrationEvidencePayload Evidence,
        [property: JsonPropertyOrder(1)] string IntegritySha256);

    private sealed record CalibrationEvidencePayload(
        [property: JsonPropertyOrder(0)] int SchemaVersion,
        [property: JsonPropertyOrder(1)] string Profile,
        [property: JsonPropertyOrder(2)] string ModuleName,
        [property: JsonPropertyOrder(3)] string AssetsPath,
        [property: JsonPropertyOrder(4)] string SampleFileName,
        [property: JsonPropertyOrder(5)] string SampleSha256,
        [property: JsonPropertyOrder(6)] long SampleSizeBytes,
        [property: JsonPropertyOrder(7)] string ObservedResourceName,
        [property: JsonPropertyOrder(8)] string ObservedResourceType,
        [property: JsonPropertyOrder(9)] IReadOnlyDictionary<string, string> ObservedSettings,
        [property: JsonPropertyOrder(10)] string WindowTitle,
        [property: JsonPropertyOrder(11)] string ProcessName,
        [property: JsonPropertyOrder(12)] DateTimeOffset ObservedAtUtc);
}
