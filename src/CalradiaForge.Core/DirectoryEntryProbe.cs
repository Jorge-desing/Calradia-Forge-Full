using System;
using System.IO;
using System.Security;
using System.Threading;

namespace CalradiaForge.Core
{
    internal enum DirectoryEntryProbeResult
    {
        Ready,
        EntryLimitReached,
        Unreadable,
        ReparsePoint
    }

    internal static class DirectoryEntryProbe
    {
        internal static DirectoryEntryProbeResult Inspect(
            string path,
            CancellationToken cancellationToken,
            Func<bool> tryConsumeEntry,
            out FileAttributes attributes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attributes = default(FileAttributes);
            if (!tryConsumeEntry()) return DirectoryEntryProbeResult.EntryLimitReached;

            try
            {
                attributes = File.GetAttributes(path);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is SecurityException)
            {
                return DirectoryEntryProbeResult.Unreadable;
            }

            return (attributes & FileAttributes.ReparsePoint) != 0
                ? DirectoryEntryProbeResult.ReparsePoint
                : DirectoryEntryProbeResult.Ready;
        }
    }
}
