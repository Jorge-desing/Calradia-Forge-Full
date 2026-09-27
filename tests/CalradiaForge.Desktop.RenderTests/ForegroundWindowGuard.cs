using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// Observes render-test foreground transitions on the renderer's private Windows desktop.
/// This guard is deliberately passive: it never changes foreground or activation state.
/// </summary>
internal sealed class ForegroundWindowGuard : IDisposable
{
    const uint SystemForegroundEvent = 0x0003;
    const uint WinEventOutOfContext = 0x0000;

    readonly uint testProcessId;
    readonly Stopwatch elapsed = Stopwatch.StartNew();
    readonly object observationsLock = new object();
    readonly List<object> foregroundEvents = new List<object>();
    readonly WinEventDelegate winEventCallback;
    IntPtr foregroundHook;
    int testProcessWasForeground;

    ForegroundWindowGuard()
    {
        testProcessId = (uint)Environment.ProcessId;
        winEventCallback = OnForegroundEvent;
        foregroundHook = SetWinEventHook(SystemForegroundEvent, SystemForegroundEvent, IntPtr.Zero,
            winEventCallback, testProcessId, 0, WinEventOutOfContext);
        if (foregroundHook == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not observe foreground transitions on the isolated WPF render-test desktop.");
    }

    internal static ForegroundWindowGuard Capture() => new ForegroundWindowGuard();

    public bool TestProcessWasForegroundOnIsolatedDesktop => Volatile.Read(ref testProcessWasForeground) != 0;

    public object CreateDiagnosticSnapshot()
    {
        var currentWindow = GetForegroundWindow();
        var currentProcessId = GetWindowProcessId(currentWindow);
        return new
        {
            isolatedDesktop = IsolatedRenderDesktop.Name,
            testProcessId,
            testProcessWasForegroundOnIsolatedDesktop = TestProcessWasForegroundOnIsolatedDesktop,
            testWindowCurrentlyForegroundOnIsolatedDesktop = currentWindow != IntPtr.Zero && currentProcessId == testProcessId,
            currentForegroundHwndOnIsolatedDesktop = FormatHandle(currentWindow),
            currentForegroundProcessIdOnIsolatedDesktop = currentProcessId,
            firstForegroundEventOnIsolatedDesktop = FirstForegroundEventOnIsolatedDesktop
        };
    }

    object FirstForegroundEventOnIsolatedDesktop
    {
        get
        {
            lock (observationsLock) return foregroundEvents.Count == 0 ? null : foregroundEvents[0];
        }
    }

    public void Dispose()
    {
        if (foregroundHook == IntPtr.Zero) return;
        UnhookWinEvent(foregroundHook);
        foregroundHook = IntPtr.Zero;
    }

    void OnForegroundEvent(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint eventThread, uint eventTime)
    {
        if (window == IntPtr.Zero || GetWindowProcessId(window) != testProcessId) return;
        Interlocked.Exchange(ref testProcessWasForeground, 1);
        lock (observationsLock)
        {
            if (foregroundEvents.Count != 0) return;
            foregroundEvents.Add(new
            {
                hwnd = FormatHandle(window),
                processId = testProcessId,
                elapsedMilliseconds = elapsed.ElapsedMilliseconds,
                eventTime
            });
        }
    }

    static uint GetWindowProcessId(IntPtr window)
    {
        if (window == IntPtr.Zero) return 0;
        GetWindowThreadProcessId(window, out var processId);
        return processId;
    }

    static string FormatHandle(IntPtr window) => window == IntPtr.Zero ? "0x0" : "0x" + window.ToInt64().ToString("X");

    delegate void WinEventDelegate(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint eventThread, uint eventTime);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventDelegate callback,
        uint processId, uint threadId, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool UnhookWinEvent(IntPtr hook);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
