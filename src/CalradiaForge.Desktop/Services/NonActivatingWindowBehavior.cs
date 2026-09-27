using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CalradiaForge.Desktop.Services
{
    /// <summary>Applies no-activate window styles to opt-in inspection/test windows only.</summary>
    internal static class NonActivatingWindowBehavior
    {
        const int ExtendedStyleIndex = -20;
        const long ExtendedStyleNoActivate = 0x08000000L;
        const uint PositionNoSize = 0x0001;
        const uint PositionNoMove = 0x0002;
        const uint PositionNoZOrder = 0x0004;
        const uint PositionNoActivate = 0x0010;
        const uint PositionFrameChanged = 0x0020;

        internal static void Apply(Window window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            window.ShowActivated = false;
            window.SourceInitialized += (_, _) => ApplyToHandle(new WindowInteropHelper(window).Handle);
            // Window configuration (for example, Opacity=0 in the UIA runner)
            // can force WPF to create the source before Apply is called. Ensure
            // the existing or newly-created HWND receives the style before Show.
            ApplyToHandle(new WindowInteropHelper(window).EnsureHandle());
        }

        /// <summary>
        /// Prepares a read-only UI Automation host without showing it over the
        /// user's desktop. The HWND remains available to UIA, but is transparent,
        /// off-screen, absent from the taskbar, and cannot activate.
        /// </summary>
        internal static void ApplyOffscreenInspection(Window window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = -10000;
            window.Top = -10000;
            window.Opacity = 0;
            window.ShowInTaskbar = false;
            Apply(window);
        }

        internal static bool HasNoActivateStyle(Window window)
        {
            if (window == null) return false;
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) return false;
            var style = GetWindowLongPtr(handle, ExtendedStyleIndex).ToInt64();
            return (style & ExtendedStyleNoActivate) != 0;
        }

        static void ApplyToHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return;
            var current = GetWindowLongPtr(handle, ExtendedStyleIndex).ToInt64();
            SetWindowLongPtr(handle, ExtendedStyleIndex, new IntPtr(current | ExtendedStyleNoActivate));
            SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0,
                PositionNoMove | PositionNoSize | PositionNoZOrder | PositionNoActivate | PositionFrameChanged);
        }

        static IntPtr GetWindowLongPtr(IntPtr window, int index) => IntPtr.Size == 8
            ? GetWindowLongPtr64(window, index)
            : new IntPtr(GetWindowLong32(window, index));

        static IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value) => IntPtr.Size == 8
            ? SetWindowLongPtr64(window, index, value)
            : new IntPtr(SetWindowLong32(window, index, value.ToInt32()));

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
        static extern int GetWindowLong32(IntPtr window, int index);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        static extern IntPtr GetWindowLongPtr64(IntPtr window, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
        static extern int SetWindowLong32(IntPtr window, int index, int value);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        static extern IntPtr SetWindowLongPtr64(IntPtr window, int index, IntPtr value);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    }
}
