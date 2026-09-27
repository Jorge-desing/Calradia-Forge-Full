using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Text.Json;

namespace BannerlordFbxImporter;

/// <summary>A resource as observed in the destination module's resource inventory.</summary>
public sealed record DestinationResourceIdentity(
    string ModuleName,
    string Name,
    string Type,
    IReadOnlyList<string> RelativeAssetFiles);

/// <summary>The exact name and type the import is expected to replace.</summary>
public sealed record RequestedResourceIdentity(string ModuleName, string Name, string Type);

public enum ResourceCollisionKind
{
    NoCollision,
    ExactCollision,
    NameConflict,
    TypeConflict,
    Ambiguous,
    InventoryUnavailable,
    InvalidEvidence
}

public sealed class ResourceCollisionReview
{
    internal ResourceCollisionReview(ResourceCollisionKind kind, RequestedResourceIdentity requested,
        DestinationResourceIdentity? exactResource, IReadOnlyList<DestinationResourceIdentity> candidates, string detail)
    {
        Kind = kind;
        Requested = requested;
        ExactResource = exactResource;
        Candidates = candidates;
        Detail = detail;
    }

    public ResourceCollisionKind Kind { get; }
    public RequestedResourceIdentity Requested { get; }
    public DestinationResourceIdentity? ExactResource { get; }
    public IReadOnlyList<DestinationResourceIdentity> Candidates { get; }
    public string Detail { get; }
    public bool IsExactCollision => Kind == ResourceCollisionKind.ExactCollision && ExactResource is not null;
}

/// <summary>
/// Classifies possible destination conflicts conservatively. A resource name match is treated as a
/// conflict even when its type or casing differs; only one exact module/name/type match is replaceable.
/// </summary>
public static class ResourceCollisionClassifier
{
    public static ResourceCollisionReview Classify(
        RequestedResourceIdentity requested,
        bool inventoryComplete,
        IEnumerable<DestinationResourceIdentity>? inventory)
    {
        ArgumentNullException.ThrowIfNull(requested);
        if (string.IsNullOrWhiteSpace(requested.ModuleName) || string.IsNullOrWhiteSpace(requested.Name) || string.IsNullOrWhiteSpace(requested.Type))
            return new(ResourceCollisionKind.InvalidEvidence, requested, null, [], "Requested resource identity is incomplete.");
        if (!inventoryComplete || inventory is null)
            return new(ResourceCollisionKind.InventoryUnavailable, requested, null, [], "Destination inventory is unavailable or incomplete; replacement is blocked.");

        DestinationResourceIdentity[] entries;
        try { entries = inventory.ToArray(); }
        catch (Exception ex)
        {
            return new(ResourceCollisionKind.InvalidEvidence, requested, null, [], $"Destination inventory could not be read: {ex.Message}");
        }

        if (entries.Any(entry => entry is null || string.IsNullOrWhiteSpace(entry.ModuleName) ||
                                 string.IsNullOrWhiteSpace(entry.Name) || string.IsNullOrWhiteSpace(entry.Type) ||
                                 entry.RelativeAssetFiles is null))
            return new(ResourceCollisionKind.InvalidEvidence, requested, null, [], "Destination inventory contains an incomplete resource identity.");

        DestinationResourceIdentity[] candidates = entries
            .Where(entry => string.Equals(entry.ModuleName, requested.ModuleName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(entry.Name, requested.Name, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (candidates.Length == 0)
            return new(ResourceCollisionKind.NoCollision, requested, null, [], "No destination resource with this name was observed.");
        if (candidates.Length > 1)
            return new(ResourceCollisionKind.Ambiguous, requested, null, candidates, "More than one destination resource could match; replacement is blocked.");

        DestinationResourceIdentity candidate = candidates[0];
        if (!string.Equals(candidate.Name, requested.Name, StringComparison.Ordinal) ||
            !string.Equals(candidate.ModuleName, requested.ModuleName, StringComparison.Ordinal))
            return new(ResourceCollisionKind.NameConflict, requested, null, candidates, "A case-insensitive name collision exists, but the observed identity is not an exact match.");
        if (!string.Equals(candidate.Type, requested.Type, StringComparison.Ordinal))
            return new(ResourceCollisionKind.TypeConflict, requested, null, candidates, "The destination name exists with a different resource type.");

        if (candidate.RelativeAssetFiles.Count == 0 || candidate.RelativeAssetFiles.Any(path => !IsSafeRelativeAssetPath(path)) ||
            candidate.RelativeAssetFiles.Distinct(StringComparer.OrdinalIgnoreCase).Count() != candidate.RelativeAssetFiles.Count)
            return new(ResourceCollisionKind.InvalidEvidence, requested, null, candidates, "The exact resource does not have a complete, safe, unambiguous list of backing files.");

        return new(ResourceCollisionKind.ExactCollision, requested, candidate, candidates, "One exact module/name/type identity and its backing asset paths were observed.");
    }

    internal static bool IsSafeRelativeAssetPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path)) return false;
        string normalized = path.Replace('\\', '/');
        string[] segments = normalized.Split('/');
        return segments.Length > 0 && segments.All(segment => segment.Length > 0 && segment != "." && segment != "..") &&
               !normalized.StartsWith('/');
    }
}

public sealed record VerifiedBackupFile(string RelativePath, long Length, string Sha256);

public sealed class BackupVerificationReview
{
    internal BackupVerificationReview(bool isVerified, string detail, string? backupDirectory, IReadOnlyList<VerifiedBackupFile> files)
    {
        IsVerified = isVerified;
        Detail = detail;
        BackupDirectory = backupDirectory;
        Files = files;
    }

    public bool IsVerified { get; }
    public string Detail { get; }
    public string? BackupDirectory { get; }
    public IReadOnlyList<VerifiedBackupFile> Files { get; }
}

/// <summary>
/// Rechecks the backup manifest, copied bytes, and current source bytes for every file belonging to
/// an exactly identified resource. A successful review is evidence of a restorable copy, not of import.
/// </summary>
public static class ReplacementBackupVerifier
{
    private const long MaximumManifestBytes = 32L * 1024 * 1024;

    public static BackupVerificationReview Verify(
        ResourceCollisionReview collision,
        AssetBackupResult? backup,
        string assetsPath,
        string moduleName)
    {
        ArgumentNullException.ThrowIfNull(collision);
        var verifiedFiles = new List<VerifiedBackupFile>();
        if (!collision.IsExactCollision || collision.ExactResource is null)
            return Blocked("An exact resource identity is required before verifying a replacement backup.");
        if (backup is null)
            return Blocked("No backup artifact was supplied.");
        if (!string.Equals(collision.Requested.ModuleName, moduleName, StringComparison.Ordinal) ||
            !string.Equals(collision.ExactResource.ModuleName, moduleName, StringComparison.Ordinal))
            return Blocked("The backup module does not match the exact resource identity.");

        try
        {
            string sourceRoot = AssetBackupService.ValidateAssetsRoot(assetsPath, moduleName);
            string backupDirectory = Path.GetFullPath(backup.BackupDirectory);
            string manifestPath = Path.GetFullPath(backup.ManifestPath);
            if (!IsWithin(manifestPath, backupDirectory) || !string.Equals(Path.GetFileName(manifestPath), "backup-manifest.json", StringComparison.OrdinalIgnoreCase))
                return Blocked("The backup manifest is outside its declared backup directory or has an unexpected name.", backupDirectory);
            if (IsWithin(backupDirectory, sourceRoot))
                return Blocked("The backup must be outside the module Assets tree.", backupDirectory);
            RejectReparsePath(backupDirectory);
            RejectReparsePath(manifestPath);

            var manifestInfo = new FileInfo(manifestPath);
            if (!manifestInfo.Exists || manifestInfo.Length <= 0 || manifestInfo.Length > MaximumManifestBytes)
                return Blocked("The backup manifest is missing, empty, or exceeds the verification limit.", backupDirectory);

            List<AssetBackupEntry>? entries = JsonSerializer.Deserialize<List<AssetBackupEntry>>(File.ReadAllBytes(manifestPath));
            if (entries is null || entries.Count > AssetBackupService.MaximumFiles)
                return Blocked("The backup manifest is invalid or exceeds the file-count limit.", backupDirectory);
            var byPath = new Dictionary<string, AssetBackupEntry>(StringComparer.OrdinalIgnoreCase);
            long manifestTotal = 0;
            foreach (AssetBackupEntry entry in entries)
            {
                if (entry is null || !ResourceCollisionClassifier.IsSafeRelativeAssetPath(entry.RelativePath) ||
                    entry.Length < 0 || !IsSha256(entry.Sha256) || !byPath.TryAdd(Normalize(entry.RelativePath), entry))
                    return Blocked("The backup manifest contains an unsafe, malformed, or duplicate entry.", backupDirectory);
                manifestTotal = checked(manifestTotal + entry.Length);
                if (manifestTotal > AssetBackupService.MaximumBytes)
                    return Blocked("The backup manifest exceeds the verified snapshot size limit.", backupDirectory);
            }

            foreach (string relative in collision.ExactResource.RelativeAssetFiles)
            {
                string key = Normalize(relative);
                if (!byPath.TryGetValue(key, out AssetBackupEntry? entry))
                    return Blocked($"The verified snapshot does not contain the resource file '{relative}'.", backupDirectory);

                string sourceFile = ResolveContainedPath(sourceRoot, relative);
                string backupFile = ResolveContainedPath(Path.Combine(backupDirectory, "Assets"), relative);
                RejectReparsePath(sourceFile);
                RejectReparsePath(backupFile);
                var sourceInfo = new FileInfo(sourceFile);
                var backupInfo = new FileInfo(backupFile);
                if (!sourceInfo.Exists || !backupInfo.Exists || sourceInfo.Length != entry.Length || backupInfo.Length != entry.Length)
                    return Blocked($"The source or backup file is missing or has a different size: '{relative}'.", backupDirectory);

                string expectedHash = entry.Sha256.ToUpperInvariant();
                if (!string.Equals(HashFile(sourceFile), expectedHash, StringComparison.Ordinal) ||
                    !string.Equals(HashFile(backupFile), expectedHash, StringComparison.Ordinal))
                    return Blocked($"SHA-256 verification failed for source or backup file '{relative}'.", backupDirectory);
                verifiedFiles.Add(new(relative, entry.Length, expectedHash));
            }

            return new(true, "Every backing file for the exact resource matches the backup manifest and current source by size and SHA-256.", backupDirectory, verifiedFiles);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException or OverflowException)
        {
            return Blocked($"Backup verification failed closed: {ex.Message}", backup.BackupDirectory);
        }
    }

    private static BackupVerificationReview Blocked(string detail, string? directory = null) => new(false, detail, directory, []);

    private static string ResolveContainedPath(string root, string relative)
    {
        if (!ResourceCollisionClassifier.IsSafeRelativeAssetPath(relative))
            throw new InvalidDataException("An affected asset path is not a safe relative path.");
        string fullRoot = Path.GetFullPath(root);
        string fullPath = Path.GetFullPath(Path.Combine(fullRoot, relative.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));
        if (!IsWithin(fullPath, fullRoot) || string.Equals(fullPath, fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("An affected asset path escaped its declared root.");
        return fullPath;
    }

    private static void RejectReparsePath(string path)
    {
        string full = Path.GetFullPath(path);
        FileSystemInfo? current = File.Exists(full) ? new FileInfo(full) : Directory.Exists(full) ? new DirectoryInfo(full) : null;
        while (current is not null)
        {
            if ((current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"Reparse points are not allowed in replacement evidence: '{current.FullName}'.");
            current = current switch
            {
                FileInfo file => file.Directory,
                DirectoryInfo directory => directory.Parent,
                _ => null
            };
        }
    }

    private static bool IsWithin(string candidate, string root)
    {
        string normalizedCandidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));
        string normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        return string.Equals(normalizedCandidate, normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
               normalizedCandidate.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');
    private static bool IsSha256(string? value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);
    private static string HashFile(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}

/// <summary>Identity of the one replacement modal calibrated by an operator.</summary>
public sealed record CalibratedReplacementDialog(
    bool ReviewedByOperator,
    int EditorProcessId,
    string WindowAutomationId,
    string ExactWindowName,
    string ReplaceButtonAutomationId,
    string CancelButtonAutomationId);

/// <summary>One visible dialog and the exact control multiplicities observed in it.</summary>
public sealed record ReplacementDialogObservation(
    int ProcessId,
    string WindowAutomationId,
    string WindowName,
    bool IsVisible,
    int ReplaceButtonMatches,
    int CancelButtonMatches);

public sealed record ReplacementDialogSnapshot(
    bool TimedOut,
    bool EnumerationComplete,
    IReadOnlyList<ReplacementDialogObservation> VisibleDialogs);

public enum ReplacementDialogState
{
    KnownCalibratedDialog,
    NoDialog,
    UnexpectedDialog,
    Ambiguous,
    TimedOut,
    NotCalibrated,
    InvalidObservation
}

public sealed record ReplacementDialogReview(ReplacementDialogState State, string Detail)
{
    public bool IsKnownCalibratedDialog => State == ReplacementDialogState.KnownCalibratedDialog;
}

/// <summary>Recognizes only a single, exact, previously reviewed replacement dialog; never dismisses it.</summary>
public static class ReplacementDialogClassifier
{
    public static ReplacementDialogReview Classify(CalibratedReplacementDialog? calibration, ReplacementDialogSnapshot? snapshot)
    {
        if (calibration is null || !calibration.ReviewedByOperator || calibration.EditorProcessId <= 0 ||
            string.IsNullOrWhiteSpace(calibration.WindowAutomationId) || string.IsNullOrWhiteSpace(calibration.ExactWindowName) ||
            string.IsNullOrWhiteSpace(calibration.ReplaceButtonAutomationId) || string.IsNullOrWhiteSpace(calibration.CancelButtonAutomationId) ||
            string.Equals(calibration.ReplaceButtonAutomationId, calibration.CancelButtonAutomationId, StringComparison.Ordinal))
            return new(ReplacementDialogState.NotCalibrated, "The replacement dialog does not have a complete operator-reviewed calibration.");
        if (snapshot is null)
            return new(ReplacementDialogState.InvalidObservation, "No dialog observation was supplied.");
        if (snapshot.TimedOut)
            return new(ReplacementDialogState.TimedOut, "UI Automation timed out; the dialog state is unknown and must not be retried.");
        if (!snapshot.EnumerationComplete || snapshot.VisibleDialogs is null)
            return new(ReplacementDialogState.InvalidObservation, "Dialog enumeration is incomplete; the outcome is unknown.");

        ReplacementDialogObservation[] visible = snapshot.VisibleDialogs.Where(dialog => dialog?.IsVisible == true).ToArray();
        if (visible.Length == 0)
            return new(ReplacementDialogState.NoDialog, "No replacement dialog is visible; replacement is not authorized.");
        if (visible.Length != 1)
            return new(ReplacementDialogState.Ambiguous, "Multiple visible dialogs exist; leave them open and stop without retrying.");

        ReplacementDialogObservation observed = visible[0];
        bool exactWindow = observed.ProcessId == calibration.EditorProcessId &&
                           string.Equals(observed.WindowAutomationId, calibration.WindowAutomationId, StringComparison.Ordinal) &&
                           string.Equals(observed.WindowName, calibration.ExactWindowName, StringComparison.Ordinal);
        if (!exactWindow || observed.ReplaceButtonMatches != 1 || observed.CancelButtonMatches != 1)
            return new(ReplacementDialogState.UnexpectedDialog, "The visible dialog or its button multiplicities differ from the reviewed calibration; leave it open and stop.");

        return new(ReplacementDialogState.KnownCalibratedDialog, "Exactly one calibrated replacement dialog is visible. This classification does not click or dismiss it.");
    }
}

public enum ReplacementAuthorizationState { Authorized, Blocked, Stopped, Unknown }
public sealed record ReplacementAuthorizationReview(ReplacementAuthorizationState State, string Detail, ReplacementPermit? Permit);
public enum ReplacementClickState { Ready, Invoking, Applied, Stopped, Unknown }

/// <summary>
/// Single-use capability for an already identified resource with a verified backup and the calibrated
/// replacement dialog visible. Consumers must call BeginClick once and report its outcome; an uncertain
/// outcome permanently consumes the permit and must never be retried.
/// </summary>
public sealed class ReplacementPermit
{
    private int _state; // 0=authorized, 1=invoking, 2=applied, 3=stopped, 4=unknown

    internal ReplacementPermit(string resourceName, string resourceType, string replaceButtonAutomationId, IReadOnlyList<VerifiedBackupFile> files)
    {
        ResourceName = resourceName;
        ResourceType = resourceType;
        ReplaceButtonAutomationId = replaceButtonAutomationId;
        BackedUpFiles = files;
    }

    public string ResourceName { get; }
    public string ResourceType { get; }
    public string ReplaceButtonAutomationId { get; }
    public IReadOnlyList<VerifiedBackupFile> BackedUpFiles { get; }
    public ReplacementClickState ClickState => Volatile.Read(ref _state) switch
    {
        0 => ReplacementClickState.Ready,
        1 => ReplacementClickState.Invoking,
        2 => ReplacementClickState.Applied,
        4 => ReplacementClickState.Unknown,
        _ => ReplacementClickState.Stopped
    };

    public bool BeginClick(out string reason)
    {
        if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
        {
            reason = $"One invocation is permitted for calibrated control '{ReplaceButtonAutomationId}'.";
            return true;
        }
        reason = "The replacement capability was already used or completed; retries are forbidden.";
        return false;
    }

    public ReplacementClickState CompleteClick(bool callReturned, bool expectedDialogClosed, bool editorResponsive, bool unexpectedDialogVisible, out string detail)
    {
        ReplacementClickState next;
        if (!callReturned || !editorResponsive)
            next = ReplacementClickState.Unknown;
        else if (unexpectedDialogVisible || !expectedDialogClosed)
            next = ReplacementClickState.Stopped;
        else
            next = ReplacementClickState.Applied;

        int terminal = next switch
        {
            ReplacementClickState.Applied => 2,
            ReplacementClickState.Unknown => 4,
            _ => 3
        };
        if (Interlocked.CompareExchange(ref _state, terminal, 1) != 1)
        {
            detail = "No active single-use replacement invocation exists; no retry is permitted.";
            return ReplacementClickState.Stopped;
        }

        if (next == ReplacementClickState.Unknown)
        {
            detail = "Replacement click outcome is uncertain; record UNKNOWN and do not retry.";
            return ReplacementClickState.Unknown;
        }
        if (next == ReplacementClickState.Stopped)
        {
            detail = "The calibrated dialog did not close cleanly or another dialog appeared; stop and leave the Editor state for review.";
            return ReplacementClickState.Stopped;
        }

        detail = "The calibrated replacement action returned and its dialog closed. This does not verify the imported resource.";
        return ReplacementClickState.Applied;
    }
}

/// <summary>Combines exact collision, source-matching backup bytes, and exact calibrated dialog evidence.</summary>
public sealed class ReplacementAuthorizationGate
{
    private readonly ResourceCollisionReview _collision;
    private readonly BackupVerificationReview _backup;
    private readonly CalibratedReplacementDialog? _calibration;
    private int _attempted;

    public ReplacementAuthorizationGate(
        ResourceCollisionReview collision,
        BackupVerificationReview backup,
        CalibratedReplacementDialog? calibration)
    {
        _collision = collision ?? throw new ArgumentNullException(nameof(collision));
        _backup = backup ?? throw new ArgumentNullException(nameof(backup));
        _calibration = calibration;
    }

    public ReplacementAuthorizationReview Authorize(ReplacementDialogSnapshot snapshot)
    {
        if (Interlocked.Exchange(ref _attempted, 1) != 0)
            return new(ReplacementAuthorizationState.Stopped, "Authorization was already evaluated; the replacement flow cannot be retried.", null);

        if (!_collision.IsExactCollision || _collision.ExactResource is null)
            return new(ReplacementAuthorizationState.Blocked, "Replacement requires one exact observed module/name/type identity.", null);
        if (!_backup.IsVerified || _backup.Files.Count == 0)
            return new(ReplacementAuthorizationState.Blocked, "Replacement requires a verified, non-empty backup for the exact resource files.", null);
        if (_backup.Files.Select(file => file.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != _backup.Files.Count ||
            _collision.ExactResource.RelativeAssetFiles.Count != _backup.Files.Count ||
            !_collision.ExactResource.RelativeAssetFiles.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(_backup.Files.Select(file => file.RelativePath)))
            return new(ReplacementAuthorizationState.Blocked, "The verified backup file set does not exactly match the resource's observed backing files.", null);

        ReplacementDialogReview dialog = ReplacementDialogClassifier.Classify(_calibration, snapshot);
        if (!dialog.IsKnownCalibratedDialog)
        {
            ReplacementAuthorizationState state = dialog.State == ReplacementDialogState.TimedOut || dialog.State == ReplacementDialogState.InvalidObservation
                ? ReplacementAuthorizationState.Unknown
                : ReplacementAuthorizationState.Stopped;
            return new(state, dialog.Detail, null);
        }

        var permit = new ReplacementPermit(_collision.ExactResource.Name, _collision.ExactResource.Type,
            _calibration!.ReplaceButtonAutomationId, _backup.Files);
        return new(ReplacementAuthorizationState.Authorized, "Exact resource identity, matching verified backup, and calibrated replacement dialog are present.", permit);
    }
}
