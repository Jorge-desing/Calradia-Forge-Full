using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.IO;
using BannerlordFbxImporter.Automation;

namespace BannerlordFbxImporter;

public sealed record UiWorkerRequest(string Action, string ConfigPath);
public sealed record UiWorkerResponse(bool Succeeded, string Error, EditorInspection? Inspection, string? ActionResult = null);
public sealed record UiWorkerResult<T>(T? Value, bool TimedOut, string Error, string StandardOutput, string StandardError)
{
    public bool Succeeded => !TimedOut && string.IsNullOrEmpty(Error) && Value is not null;
}

/// <summary>Runs UIA in a disposable child process so a hung provider cannot hang the caller.</summary>
public static class UiAutomationWorker
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly TimeSpan RedirectedPipeDrainGrace = TimeSpan.FromSeconds(3);

    public static UiWorkerResult<EditorInspection> Inspect(string configPath, TimeSpan timeout)
    {
        UiWorkerResult<UiWorkerResponse> worker = RunBoundedAction("inspect", configPath, timeout);
        if (!worker.Succeeded || worker.Value?.Inspection is null)
            return new(null, worker.TimedOut, worker.Error.Length == 0 ? "UIA worker returned no inspection." : worker.Error,
                worker.StandardOutput, worker.StandardError);
        return new(worker.Value.Inspection, false, "", worker.StandardOutput, worker.StandardError);
    }

    /// <summary>Invokes only the exact Resource Browser context-menu item and stops before file selection.</summary>
    public static UiWorkerResult<string> OpenImportPicker(string configPath, TimeSpan timeout)
    {
        UiWorkerResult<UiWorkerResponse> worker = RunBoundedAction("open-import-picker", configPath, timeout);
        if (!worker.Succeeded)
            return new(null, worker.TimedOut, worker.Error, worker.StandardOutput, worker.StandardError);
        if (!string.Equals(worker.Value?.ActionResult, "picker-menu-item-invoked", StringComparison.Ordinal))
            return new(null, false, "UIA worker did not return the expected picker-only action result.", worker.StandardOutput, worker.StandardError);
        return new("picker-menu-item-invoked", false, "", worker.StandardOutput, worker.StandardError);
    }

    private static UiWorkerResult<UiWorkerResponse> RunBoundedAction(string action, string configPath, TimeSpan timeout)
    {
        if (timeout < TimeSpan.FromSeconds(5) || timeout > TimeSpan.FromMinutes(15))
            throw new ArgumentOutOfRangeException(nameof(timeout));
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);
        string directory = Path.Combine(Path.GetTempPath(), "CalradiaForge-UIA-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string requestPath = Path.Combine(directory, "request.json");
        string responsePath = Path.Combine(directory, "response.json");
        File.WriteAllText(requestPath, JsonSerializer.Serialize(new UiWorkerRequest(action, Path.GetFullPath(configPath))));

        try
        {
            ProcessStartInfo start = CreateStartInfo(requestPath, responsePath);
            using var process = new Process { StartInfo = start };
            if (!process.Start())
                return new(null, false, "Could not start the isolated UI Automation worker.", "", "");

            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(checked((int)timeout.TotalMilliseconds)))
            {
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
                catch (Win32Exception) { }
                try { process.WaitForExit(3000); } catch (InvalidOperationException) { }
                _ = TryDrainRedirectedPipes(stdout, stderr, RedirectedPipeDrainGrace, () => CloseRedirectedPipes(process));
                return new(null, true, $"UIA worker timed out during '{action}'. The outcome is UNKNOWN; the Editor was not closed, retried, or otherwise controlled.",
                    GetCompletedOutput(stdout), GetCompletedOutput(stderr));
            }

            if (!TryDrainRedirectedPipes(stdout, stderr, RedirectedPipeDrainGrace, () => CloseRedirectedPipes(process)))
                return new(null, true,
                    $"UNKNOWN: UIA worker exited during '{action}', but redirected output pipes did not close within {RedirectedPipeDrainGrace.TotalSeconds:0} seconds. No Editor retry or dialog action was attempted.",
                    GetCompletedOutput(stdout), GetCompletedOutput(stderr));
            if (process.ExitCode != 0)
                return new(null, false, $"UIA worker exited with code {process.ExitCode}.", GetCompletedOutput(stdout), GetCompletedOutput(stderr));
            if (!File.Exists(responsePath))
                return new(null, false, "UIA worker returned no response file.", GetCompletedOutput(stdout), GetCompletedOutput(stderr));

            UiWorkerResponse? response = JsonSerializer.Deserialize<UiWorkerResponse>(File.ReadAllText(responsePath), JsonOptions);
            if (response is null)
                return new(null, false, "UIA worker response was invalid.", GetCompletedOutput(stdout), GetCompletedOutput(stderr));
            if (process.ExitCode != 0 || !response.Succeeded)
                return new(response, false, response.Error.Length == 0 ? $"UIA worker exited with code {process.ExitCode}." : response.Error, GetCompletedOutput(stdout), GetCompletedOutput(stderr));
            return new(response, false, "", GetCompletedOutput(stdout), GetCompletedOutput(stderr));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or JsonException or InvalidOperationException)
        {
            return new(null, false, $"UIA worker failed: {ex.Message}", "", "");
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>
    /// Waits only a bounded time for redirected stream EOF. A child may exit while
    /// a descendant still holds an inherited pipe open, so this must never be an
    /// unbounded Task.WaitAll after Process.WaitForExit has returned.
    /// </summary>
    public static bool TryDrainRedirectedPipes(Task stdout, Task stderr, TimeSpan timeout, Action closePipes)
    {
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);
        ArgumentNullException.ThrowIfNull(closePipes);
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        try
        {
            if (Task.WaitAll([stdout, stderr], timeout)) return true;
        }
        catch (AggregateException)
        {
            // Faulted drain tasks are complete; their content is not trusted below.
            return true;
        }

        try { closePipes(); } catch (Exception) { }
        return false;
    }

    private static void CloseRedirectedPipes(Process process)
    {
        try { process.StandardOutput.Close(); } catch (ObjectDisposedException) { } catch (InvalidOperationException) { }
        try { process.StandardError.Close(); } catch (ObjectDisposedException) { } catch (InvalidOperationException) { }
    }

    private static string GetCompletedOutput(Task<string> stream) =>
        stream.IsCompletedSuccessfully ? stream.Result : "";

    public static int RunWorker(string[] args)
    {
        if (args.Length != 3 || args[0] != "--ui-worker") return 64;
        UiWorkerResponse response;
        try
        {
            string requestPath = Path.GetFullPath(args[1]);
            string responsePath = Path.GetFullPath(args[2]);
            string expectedRoot = Path.GetFullPath(Path.GetDirectoryName(requestPath)!);
            if (!string.Equals(Path.GetDirectoryName(responsePath), expectedRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Worker request and response must share their private temporary directory.");
            if (new FileInfo(requestPath).Length > 64 * 1024)
                throw new InvalidDataException("UIA worker request is too large.");
            UiWorkerRequest request = JsonSerializer.Deserialize<UiWorkerRequest>(File.ReadAllText(requestPath), JsonOptions)
                ?? throw new InvalidDataException("UIA worker request is invalid.");
            if (request.Action is not ("inspect" or "open-import-picker"))
                throw new InvalidOperationException("Only read-only inspect and the picker-only open-import-picker action are implemented in the UIA worker.");
            AppConfig config = AppConfig.Load(request.ConfigPath);
            using WindowsEditorAutomation automation = WindowsEditorAutomation.Attach(config);
            if (string.Equals(request.Action, "inspect", StringComparison.Ordinal))
            {
                response = new(true, "", automation.Inspect());
            }
            else if (string.Equals(request.Action, "open-import-picker", StringComparison.Ordinal))
            {
                automation.OpenImportPicker();
                response = new(true, "", null, "picker-menu-item-invoked");
            }
            else
            {
                throw new InvalidOperationException("Only read-only inspect and the picker-only open-import-picker action are implemented in the UIA worker.");
            }
            File.WriteAllText(responsePath, JsonSerializer.Serialize(response));
            return 0;
        }
        catch (Exception ex)
        {
            try
            {
                string responsePath = Path.GetFullPath(args[2]);
                File.WriteAllText(responsePath, JsonSerializer.Serialize(new UiWorkerResponse(false, ex.Message, null)));
            }
            catch { }
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static ProcessStartInfo CreateStartInfo(string requestPath, string responsePath)
    {
        string processPath = Environment.ProcessPath ?? throw new InvalidOperationException("Current executable path is unavailable.");
        bool isDotnetHost = string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase);
        var start = new ProcessStartInfo
        {
            FileName = processPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = AppContext.BaseDirectory
        };
        if (isDotnetHost)
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("--ui-worker");
        start.ArgumentList.Add(requestPath);
        start.ArgumentList.Add(responsePath);
        return start;
    }
}
