using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

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

    internal static void AllowFixedViewport(Window window)
    {
        if (!IsCurrentThreadAttached)
            throw new InvalidOperationException("Render viewport sizing requires the isolated desktop.");
        window.SourceInitialized += (_, _) =>
        {
            var source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
            source.AddHook(ExpandTrackBounds);
        };
    }

    internal static void VerifySmallDisplayBounds(Window window)
    {
        if (!IsCurrentThreadAttached)
            throw new InvalidOperationException("Render viewport verification requires the isolated desktop.");
        var address = Marshal.AllocHGlobal(Marshal.SizeOf<MinMaxInfo>());
        try
        {
            // Exercise the actual fixture HWND with smaller native limits, without
            // changing the runner display or touching any interactive window.
            var smallDisplay = new MinMaxInfo
            {
                MaxSize = new NativePoint { X = 1024, Y = 768 },
                MaxTrackSize = new NativePoint { X = 1024, Y = 768 }
            };
            Marshal.StructureToPtr(smallDisplay, address, false);
            SendMessage(new WindowInteropHelper(window).Handle, 0x0024, IntPtr.Zero, address);
            var effective = Marshal.PtrToStructure<MinMaxInfo>(address);
            if (effective.MaxTrackSize.X < 1360 || effective.MaxTrackSize.Y < 820)
                throw new InvalidOperationException("The isolated fixture still restricts layout to the runner display size.");
        }
        finally { Marshal.FreeHGlobal(address); }
    }

    static IntPtr ExpandTrackBounds(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WmGetMinMaxInfo = 0x0024;
        if (message != WmGetMinMaxInfo || lParam == IntPtr.Zero) return IntPtr.Zero;
        // WPF caches the native tracking limit for its layout constraints. Hosted runners
        // can have a display smaller than the render matrix. Expand only this fixture's
        // HWND limits, leaving WPF to process the message and enforce its real MinWidth.
        var bounds = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        bounds.MaxTrackSize.X = Math.Max(bounds.MaxTrackSize.X, 16384);
        bounds.MaxTrackSize.Y = Math.Max(bounds.MaxTrackSize.Y, 16384);
        Marshal.StructureToPtr(bounds, lParam, false);
        return IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NativePoint { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

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

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
}
