using BannerlordFbxImporter.Preflight;
using System.IO;

namespace BannerlordFbxImporter;

public sealed record PlannedAsset(AssetFile File, PreflightResult? Evidence, TextureAssignment? TextureAssignment);
public sealed record ImportPlan(ImportProfileKind Profile, AssetBatch Batch, IReadOnlyList<PlannedAsset> Assets, MaterialManifest Manifest);

public static class BatchPlanner
{
    public static ImportPlan Build(string sourceFolder, ImportProfileKind profile, IReadOnlyList<string> supportedExtensions,
        string? manifestPath = null, string? textureAssignmentMapPath = null, AssetScanLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(supportedExtensions);
        if (profile.IsMesh() && !string.IsNullOrWhiteSpace(textureAssignmentMapPath))
            throw new ArgumentException("A texture assignment map is only valid for the texture-assign profile.", nameof(textureAssignmentMapPath));
        if (!profile.IsMesh() && !string.IsNullOrWhiteSpace(manifestPath))
            throw new ArgumentException("A material manifest is only valid for mesh profiles.", nameof(manifestPath));
        if (profile == ImportProfileKind.TextureAssign && string.IsNullOrWhiteSpace(textureAssignmentMapPath))
            throw new ArgumentException("texture-assign requires an explicit --texture-map.", nameof(textureAssignmentMapPath));
        if (profile != ImportProfileKind.TextureAssign && !string.IsNullOrWhiteSpace(textureAssignmentMapPath))
            throw new ArgumentException("--texture-map is only valid for texture-assign.", nameof(textureAssignmentMapPath));

        string[] expectedExtensions = profile.IsMesh() ? [".fbx"] : supportedExtensions.ToArray();
        if (profile.IsMesh() && supportedExtensions.Any(value => !string.Equals(value, ".fbx", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Mesh profiles accept FBX only.", nameof(supportedExtensions));

        AssetBatch batch = AssetBatchScanner.Scan(sourceFolder, limits, expectedExtensions);
        MaterialManifest manifest = profile.IsMesh() ? FbxPreflight.LoadManifest(manifestPath) :
            new(EvidenceAvailability.NotAvailable, new HashSet<string>(StringComparer.OrdinalIgnoreCase), "Not applicable to texture profiles.");
        TextureAssignmentMap? textureMap = profile == ImportProfileKind.TextureAssign
            ? TextureAssignmentMap.Load(textureAssignmentMapPath!, batch.Files)
            : null;
        var assets = batch.Files.Select(file => new PlannedAsset(
            file,
            profile.IsMesh() ? FbxPreflight.Analyze(file.FullPath, manifest) : null,
            textureMap?.Assignments[Path.GetFileName(file.FullPath)])).ToArray();
        return new ImportPlan(profile, batch, assets, manifest);
    }
}
