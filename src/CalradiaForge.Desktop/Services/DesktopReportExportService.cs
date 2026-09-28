using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CalradiaForge.Desktop.Services
{
    internal enum ReportExportStatus
    {
        Succeeded,
        Failed,
        Cancelled
    }

    internal sealed class ReportExportResult
    {
        internal ReportExportResult(ReportExportStatus status, string path = null)
        {
            Status = status;
            Path = path;
        }

        internal ReportExportStatus Status { get; }
        internal string Path { get; }
    }

    /// <summary>Writes reports through a same-directory temporary file and never replaces an existing report.</summary>
    internal sealed class DesktopReportExportService
    {
        readonly string outputDirectory;
        readonly Func<DateTimeOffset> utcNow;

        internal DesktopReportExportService(string outputDirectory = null, Func<DateTimeOffset> utcNow = null)
        {
            this.outputDirectory = outputDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CalradiaForge", "Reports");
            this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        }

        internal Task<ReportExportResult> ExportTextAsync(string prefix, string content, CancellationToken cancellationToken = default) =>
            ExportAsync(prefix, async writer => await writer.WriteAsync((content ?? string.Empty).AsMemory(), cancellationToken).ConfigureAwait(false), cancellationToken);

        internal Task<ReportExportResult> ExportLinesAsync(string prefix, IEnumerable<string> lines, CancellationToken cancellationToken = default) =>
            ExportAsync(prefix, async writer =>
            {
                foreach (var line in lines ?? Array.Empty<string>())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await writer.WriteLineAsync((line ?? string.Empty).AsMemory(), cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);

        internal Task<ReportExportResult> ExportMarkdownAsync(string prefix, string title, string markdownBody, CancellationToken cancellationToken = default) =>
            ExportAsync(prefix, async writer =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await writer.WriteLineAsync(("# " + (title ?? "Calradia Forge Report")).AsMemory(), cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync(("Date: " + utcNow().ToString("u")).AsMemory(), cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync(string.Empty.AsMemory(), cancellationToken).ConfigureAwait(false);
                await writer.WriteAsync((markdownBody ?? string.Empty).AsMemory(), cancellationToken).ConfigureAwait(false);
            }, cancellationToken, ".md");

        internal Task<ReportExportResult> ExportJsonAsync<T>(string prefix, T data, CancellationToken cancellationToken = default) =>
            ExportAsync(prefix, async writer =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await writer.WriteAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
            }, cancellationToken, ".json");

        async Task<ReportExportResult> ExportAsync(string prefix, Func<StreamWriter, Task> write, CancellationToken cancellationToken, string extension = ".txt")
        {
            string temporaryPath = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Directory.CreateDirectory(outputDirectory);
                var safePrefix = SanitizePrefix(prefix);
                var timestamp = utcNow().UtcDateTime.ToString("yyyyMMdd-HHmmss");
                string fileToken = null;
                FileStream stream = null;
                for (var attempt = 0; attempt < 4; attempt++)
                {
                    fileToken = Guid.NewGuid().ToString("N");
                    temporaryPath = Path.Combine(outputDirectory, "." + safePrefix + "-" + fileToken + ".tmp");
                    try
                    {
                        stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096,
                            FileOptions.Asynchronous | FileOptions.SequentialScan);
                        break;
                    }
                    catch (IOException) when (File.Exists(temporaryPath))
                    {
                        temporaryPath = null;
                        fileToken = null;
                    }
                }

                if (stream == null || temporaryPath == null)
                    return new ReportExportResult(ReportExportStatus.Failed);

                using (stream)
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true))
                {
                    await write(writer).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    stream.Flush(true);
                }

                cancellationToken.ThrowIfCancellationRequested();
                var ext = string.IsNullOrWhiteSpace(extension) ? ".txt" : (extension.StartsWith(".") ? extension : "." + extension);
                var finalPath = Path.Combine(outputDirectory, safePrefix + "-" + timestamp + "-" + fileToken + ext);
                File.Move(temporaryPath, finalPath, false);
                temporaryPath = null;
                return new ReportExportResult(ReportExportStatus.Succeeded, finalPath);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new ReportExportResult(ReportExportStatus.Cancelled);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException ||
                error is NotSupportedException || error is SecurityException)
            {
                return new ReportExportResult(ReportExportStatus.Failed);
            }
            finally
            {
                if (temporaryPath != null)
                {
                    try { File.Delete(temporaryPath); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                    catch (SecurityException) { }
                }
            }
        }

        static string SanitizePrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("A report prefix is required.", nameof(prefix));
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(prefix.Length);
            foreach (var character in prefix)
                if (Array.IndexOf(invalid, character) < 0 && !char.IsControl(character)) builder.Append(character);
            if (builder.Length == 0) throw new ArgumentException("The report prefix contains no valid filename characters.", nameof(prefix));
            return builder.ToString();
        }
    }
}
