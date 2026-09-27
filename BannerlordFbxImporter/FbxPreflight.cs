using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO;

namespace BannerlordFbxImporter.Preflight;

public enum EvidenceAvailability { Available, NotAvailable }

public sealed record MaterialManifest(EvidenceAvailability Availability, IReadOnlySet<string> Names, string Detail);

public sealed record PreflightResult(
    EvidenceAvailability Availability,
    IReadOnlyList<string> DeclaredMaterials,
    IReadOnlyList<string> Warnings,
    string Limitation);

/// <summary>Bounded text-only hints. Findings never block submission or claim import validation.</summary>
public static class FbxPreflight
{
    public const int MaximumTextBytes = 16 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly Regex MaterialRegex = new(
        "Material:\\s*-?\\d+\\s*,\\s*\"Material::(?<name>[^\"]+)\"",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    public static MaterialManifest LoadManifest(string? manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
            return new(EvidenceAvailability.NotAvailable, new HashSet<string>(StringComparer.OrdinalIgnoreCase), "No material manifest was supplied.");
        try
        {
            using var stream = File.OpenRead(manifestPath);
            if (stream.Length > 1024 * 1024)
                return UnavailableManifest("Manifest exceeds the 1 MiB limit.");
            using var document = JsonDocument.Parse(stream, new JsonDocumentOptions { MaxDepth = 8 });
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return UnavailableManifest("Manifest root must be a JSON array of material names.");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in document.RootElement.EnumerateArray())
            {
                if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
                    return UnavailableManifest("Manifest entries must be non-empty strings.");
                names.Add(value.GetString()!);
            }
            return new(EvidenceAvailability.Available, names, $"Loaded {names.Count} name(s) from the optional manifest.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return UnavailableManifest($"Manifest evidence unavailable: {ex.Message}");
        }
    }

    public static PreflightResult Analyze(string fbxPath, MaterialManifest manifest)
    {
        var warnings = new List<string>();
        if (manifest.Availability == EvidenceAvailability.NotAvailable)
            warnings.Add(manifest.Detail);

        byte[] bytes;
        try
        {
            using var stream = new FileStream(fbxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > MaximumTextBytes)
                return NotAvailable(warnings, "FBX text preflight skipped: file exceeds the 16 MiB inspection limit.");
            bytes = new byte[checked((int)stream.Length)];
            int offset = 0;
            while (offset < bytes.Length)
            {
                int read = stream.Read(bytes, offset, bytes.Length - offset);
                if (read == 0)
                    return NotAvailable(warnings, "FBX changed or ended during the bounded read.");
                offset += read;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OutOfMemoryException)
        {
            return NotAvailable(warnings, $"FBX text preflight unavailable: {ex.Message}");
        }

        if (LooksLikeBinary(bytes))
            return NotAvailable(warnings, "Binary FBX is not parsed; no material conclusions were drawn.");

        string text;
        try { text = StrictUtf8.GetString(bytes); }
        catch (DecoderFallbackException)
        {
            return NotAvailable(warnings, "FBX is not valid UTF-8 ASCII text; material evidence is unavailable.");
        }

        if (!text.TrimStart('\uFEFF', ' ', '\t', '\r', '\n').StartsWith("; FBX", StringComparison.Ordinal) &&
            !text.Contains("FBXHeaderExtension:", StringComparison.Ordinal))
            return NotAvailable(warnings, "FBX text header was not recognized; structural evidence is unavailable.");

        var materials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (Match match in MaterialRegex.Matches(text))
                if (!string.IsNullOrWhiteSpace(match.Groups["name"].Value))
                    materials.Add(match.Groups["name"].Value);
        }
        catch (RegexMatchTimeoutException)
        {
            return NotAvailable(warnings, "Material declaration scan exceeded its one-second bound.");
        }

        if (materials.Count == 0)
            warnings.Add("No material declarations were extracted. This is not a validation of mesh assignments.");
        if (manifest.Availability == EvidenceAvailability.Available)
        {
            foreach (string name in materials.Where(name => !manifest.Names.Contains(name)))
                warnings.Add($"Material declaration '{name}' is absent from the optional local manifest; review it in Resource Browser. This does not block submission.");
        }
        warnings.Add("This is bounded text evidence only. It does not validate Editor behavior, assignments, LOD warnings, skin weights, or generated packages.");
        return new(EvidenceAvailability.Available, materials.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(), warnings, "FBX ASCII declarations only; no import validation.");
    }

    private static bool LooksLikeBinary(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> signature = "Kaydara FBX Binary"u8;
        return bytes.StartsWith(signature) || bytes.IndexOf((byte)0) >= 0;
    }

    private static MaterialManifest UnavailableManifest(string detail) =>
        new(EvidenceAvailability.NotAvailable, new HashSet<string>(StringComparer.OrdinalIgnoreCase), detail);

    private static PreflightResult NotAvailable(List<string> warnings, string reason)
    {
        warnings.Add(reason);
        return new(EvidenceAvailability.NotAvailable, Array.Empty<string>(), warnings, "Not available; no blocking conclusion.");
    }
}
