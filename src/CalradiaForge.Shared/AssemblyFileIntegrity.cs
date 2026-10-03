using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace CalradiaForge.Shared
{
    internal static class AssemblyFileIntegrity
    {
        const int BufferSize = 128 * 1024;

        internal static string HashFile(string path, CancellationToken cancellation)
        {
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
            {
                var buffer = new byte[BufferSize];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancellation.ThrowIfCancellationRequested();
                    hash.AppendData(buffer, 0, read);
                }
                cancellation.ThrowIfCancellationRequested();
                return FormatHash(hash.GetHashAndReset());
            }
        }

        internal static string CopyAndHash(string source, string destination, CancellationToken cancellation, Action<string> deleteBestEffort)
        {
            var destinationCreated = false;
            try
            {
                using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
                using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
                using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.SequentialScan))
                {
                    destinationCreated = true;
                    var buffer = new byte[BufferSize];
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        output.Write(buffer, 0, read);
                        hash.AppendData(buffer, 0, read);
                    }
                    cancellation.ThrowIfCancellationRequested();
                    output.Flush(true);
                    return FormatHash(hash.GetHashAndReset());
                }
            }
            catch
            {
                if (destinationCreated) deleteBestEffort(destination);
                throw;
            }
        }

        static string FormatHash(byte[] hash) => BitConverter.ToString(hash).Replace("-", string.Empty);
    }
}
