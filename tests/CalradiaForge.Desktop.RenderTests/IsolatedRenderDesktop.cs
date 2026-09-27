using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

/// <summary>
/// Runs WPF render-test HWNDs on a private Windows desktop so a native test
/// window cannot take foreground from the user's interactive desktop.
/// Attach on the STA entry thread before WPF, HWNDs, or WinEvent hooks exist.
/// The OS releases the process-scoped desktop handle when this test process exits.
/// </summary>
internal static class IsolatedRenderDesktop
{
    const uint DesktopReadObjects = 0x0001;
    const uint DesktopCreateWindow = 0x0002;
    const uint DesktopDestroyWindow = 0x0004;
    const uint DesktopHookControl = 0x0008;
    const uint DesktopEnumerate = 0x0040;
    const uint DesktopWriteObjects = 0x0080;

    static IntPtr desktopHandle;
    static string desktopName;

    internal static string Name => desktopName;
    internal static bool IsCurrentThreadAttached => desktopHandle != IntPtr.Zero &&
        GetThreadDesktop(GetCurrentThreadId()) == desktopHandle;

    internal static void AttachCurrentThread()
    {
        if (desktopHandle != IntPtr.Zero) return;

        desktopName = "CalradiaForge.RenderTests." + Environment.ProcessId + "." + Guid.NewGuid().ToString("N");
        const uint requiredAccess = DesktopReadObjects | DesktopCreateWindow | DesktopDestroyWindow |
                                   DesktopHookControl | DesktopEnumerate | DesktopWriteObjects;
        desktopHandle = CreateDesktop(desktopName, IntPtr.Zero, IntPtr.Zero, 0, requiredAccess, IntPtr.Zero);
        if (desktopHandle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create an isolated desktop for WPF render tests.");

        if (!SetThreadDesktop(desktopHandle))
        {
            var error = Marshal.GetLastWin32Error();
            CloseDesktop(desktopHandle);
            desktopHandle = IntPtr.Zero;
            throw new Win32Exception(error, "The WPF render-test thread already owns a window or hook and cannot be isolated safely.");
        }
    }

    [DllImport("user32.dll", EntryPoint = "CreateDesktopW", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateDesktop(string desktopName, IntPtr device, IntPtr devMode, uint flags,
        uint desiredAccess, IntPtr securityAttributes);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetThreadDesktop(IntPtr desktop);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseDesktop(IntPtr desktop);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr GetThreadDesktop(uint threadId);

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();
}
