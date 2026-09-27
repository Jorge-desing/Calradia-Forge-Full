using System.IO;
using BannerlordFbxImporter.Preflight;

namespace BannerlordFbxImporter;

/// <summary>
/// Revalidates a scanned FBX and holds a read-only Windows sharing lease while the
/// Editor consumes its path, preventing ordinary writes or replacement mid-submit.
/// </summary>
public static class AssetFileLease
{
    public static FileStream OpenForSubmission(AssetFile expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        string fullPath = Path.GetFullPath(expected.FullPath);
        if (!string.Equals(fullPath, expected.FullPath, StringComparison.Ordinal))
            throw new InvalidOperationException("The FBX path changed after the batch was reviewed.");

        FileAttributes before = File.GetAttributes(fullPath);
        if ((before & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            throw new InvalidOperationException("The reviewed FBX is no longer a regular file; reparse points and directories are rejected.");

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
        try
        {
            FileAttributes after = File.GetAttributes(fullPath);
            if ((after & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
                throw new InvalidOperationException("The reviewed FBX became a directory or reparse point before submission.");

            var info = new FileInfo(fullPath);
            info.Refresh();
            if (stream.Length != expected.Length || info.Length != expected.Length || info.LastWriteTimeUtc != expected.LastWriteTimeUtc)
                throw new InvalidOperationException("The reviewed FBX changed after the batch was displayed; review the batch again before submitting.");

            return stream;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }
}
