param(
    [Parameter(Mandatory = $true)]
    [string] $FixturePath,
    [switch] $IncludeModifierProbe
)

$ErrorActionPreference = 'Stop'

if (-not [Environment]::Is64BitProcess) {
    throw 'Run this regression from 64-bit Windows PowerShell so SendInput uses the x64 INPUT layout.'
}

$resolvedFixture = (Resolve-Path -LiteralPath $FixturePath).Path
$expectedFixture = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\build\capture_test_window.exe')).Path
if ($resolvedFixture -ine $expectedFixture) {
    throw "Refusing any process other than the built fixture at '$expectedFixture'. Requested: '$resolvedFixture'"
}

$nativeInput = @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

public static class CodexFixtureKeyboardInput
{
    private const uint InputKeyboard = 1;
    private const uint KeyUpFlag = 0x0002;
    private static IntPtr ExpectedWindow;
    private static uint ExpectedProcessId;

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KeyboardInput Keyboard;
        [FieldOffset(0)] public MouseInput Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public uint Size;
        public uint Flags;
        public IntPtr ActiveWindow;
        public IntPtr FocusWindow;
        public IntPtr CaptureWindow;
        public IntPtr MenuOwnerWindow;
        public IntPtr MoveSizeWindow;
        public IntPtr CaretWindow;
        public Rect CaretRect;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, [In] Input[] inputs, int inputSize);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    public static extern bool IsWindow(IntPtr window);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDlgItem(IntPtr parent, int controlId);

    public static uint ProcessIdFor(IntPtr window)
    {
        uint processId;
        if (window == IntPtr.Zero || GetWindowThreadProcessId(window, out processId) == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot verify fixture HWND ownership.");
        return processId;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SendMessageTimeoutW(IntPtr window, uint message, UIntPtr wParam,
        StringBuilder lParam, uint flags, uint timeoutMilliseconds, out UIntPtr result);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);

    public static string ReadText(IntPtr window)
    {
        var value = new StringBuilder(2048);
        const uint WmGetText = 0x000D;
        const uint SmtoBlock = 0x0001;
        const uint SmtoAbortIfHung = 0x0002;
        UIntPtr result;
        IntPtr sent = SendMessageTimeoutW(window, WmGetText, new UIntPtr((uint)value.Capacity), value,
            SmtoBlock | SmtoAbortIfHung, 1000, out result);
        if (sent == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "WM_GETTEXT failed or timed out for the exact fixture control.");
        return value.ToString();
    }

    public static IntPtr FocusWindowFor(IntPtr window)
    {
        uint processId;
        uint threadId = GetWindowThreadProcessId(window, out processId);
        if (threadId == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        var info = new GuiThreadInfo();
        info.Size = (uint)Marshal.SizeOf(typeof(GuiThreadInfo));
        if (!GetGUIThreadInfo(threadId, ref info)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return info.FocusWindow;
    }

    public static void SetTargetWindow(IntPtr window, uint processId)
    {
        if (!IsWindow(window) || ProcessIdFor(window) != processId)
            throw new InvalidOperationException("Keyboard target must be a live window owned by the fixture process.");
        ExpectedWindow = window;
        ExpectedProcessId = processId;
    }

    public static void Press(ushort key)
    {
        Send(new[] { MakeKey(key, false), MakeKey(key, true) });
        Thread.Sleep(25);
    }

    public static void Chord(ushort modifier, ushort key)
    {
        Send(new[] { MakeKey(modifier, false), MakeKey(key, false), MakeKey(key, true), MakeKey(modifier, true) });
        Thread.Sleep(40);
    }

    public static void TypeText(string text)
    {
        foreach (char character in text)
        {
            short mapped = VkKeyScanW(character);
            if (mapped == -1) throw new ArgumentException("Fixture regression supports only characters with a virtual-key mapping.");
            ushort key = (ushort)(mapped & 0xff);
            ushort modifiers = (ushort)((mapped >> 8) & 0xff);
            bool shift = (modifiers & 1) != 0;
            if ((modifiers & 6) != 0) throw new ArgumentException("Fixture regression text must not require Ctrl or Alt.");
            if (shift)
                Send(new[] { MakeKey(0x10, false), MakeKey(key, false), MakeKey(key, true), MakeKey(0x10, true) });
            else
                Send(new[] { MakeKey(key, false), MakeKey(key, true) });
            Thread.Sleep(25);
        }
    }

    private static Input MakeKey(ushort key, bool keyUp)
    {
        var item = new Input();
        item.Type = InputKeyboard;
        item.Data.Keyboard = new KeyboardInput();
        item.Data.Keyboard.VirtualKey = key;
        item.Data.Keyboard.Flags = keyUp ? KeyUpFlag : 0;
        return item;
    }

    private static void Send(Input[] items)
    {
        if (ExpectedWindow == IntPtr.Zero || GetForegroundWindow() != ExpectedWindow || ProcessIdFor(ExpectedWindow) != ExpectedProcessId)
            throw new InvalidOperationException("Refusing to inject keys unless the exact fixture process owns the foreground window.");
        uint sent = SendInput((uint)items.Length, items, Marshal.SizeOf(typeof(Input)));
        if (sent != items.Length) throw new Win32Exception(Marshal.GetLastWin32Error(), "SendInput did not inject the complete fixture key sequence.");
        if (GetForegroundWindow() != ExpectedWindow)
            throw new InvalidOperationException("Fixture lost foreground during input; no further keys will be sent.");
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern short VkKeyScanW(char character);
}
'@

Add-Type -TypeDefinition $nativeInput -Language CSharp

function Wait-ForState {
    param(
        [Parameter(Mandatory = $true)] [scriptblock] $Condition,
        [Parameter(Mandatory = $true)] [string] $Description,
        [int] $TimeoutMilliseconds = 2500
    )

    $timer = [Diagnostics.Stopwatch]::StartNew()
    while ($timer.ElapsedMilliseconds -lt $TimeoutMilliseconds) {
        if (& $Condition) { return }
        Start-Sleep -Milliseconds 25
    }
    throw "Timed out waiting for fixture state: $Description"
}

function Wait-ForText {
    param(
        [Parameter(Mandatory = $true)] [IntPtr] $Window,
        [Parameter(Mandatory = $true)] [string] $Expected,
        [Parameter(Mandatory = $true)] [string] $Description,
        [int] $TimeoutMilliseconds = 2500
    )

    $timer = [Diagnostics.Stopwatch]::StartNew()
    $actual = ''
    while ($timer.ElapsedMilliseconds -lt $TimeoutMilliseconds) {
        $actual = [CodexFixtureKeyboardInput]::ReadText($Window)
        if ($actual -ceq $Expected) { return }
        Start-Sleep -Milliseconds 25
    }
    throw "Timed out waiting for $Description; expected '$Expected', got '$actual'"
}

function Assert-FixtureForeground {
    $foreground = [CodexFixtureKeyboardInput]::GetForegroundWindow()
    if ($foreground -ne $script:fixtureWindow) {
        throw 'The fixture lost foreground ownership; refusing to send keys to another window.'
    }
}

$fixtureProcess = $null
try {
    $fixtureProcess = Start-Process -FilePath $resolvedFixture -PassThru -WindowStyle Normal
    try { $null = $fixtureProcess.WaitForInputIdle(10000) } catch { }
    $fixtureProcess.Refresh()
    $fixtureWindow = $fixtureProcess.MainWindowHandle
    Wait-ForState -Description 'the newly launched fixture window' -Condition {
        $fixtureProcess.Refresh()
        $script:fixtureWindow = $fixtureProcess.MainWindowHandle
        $script:fixtureWindow -ne [IntPtr]::Zero
    } -TimeoutMilliseconds 10000

    $windowOwner = [uint32]0
    $null = [CodexFixtureKeyboardInput]::GetWindowThreadProcessId($fixtureWindow, [ref]$windowOwner)
    if ($windowOwner -ne [uint32]$fixtureProcess.Id) {
        throw 'Fixture HWND does not belong to the process started by this test.'
    }
    [CodexFixtureKeyboardInput]::SetTargetWindow($fixtureWindow, [uint32]$fixtureProcess.Id)
    if ([CodexFixtureKeyboardInput]::ReadText($fixtureWindow) -ne 'Codex Computer Use fixture') {
        throw 'The launched window title did not match the isolated fixture.'
    }

    $null = [CodexFixtureKeyboardInput]::ShowWindow($fixtureWindow, 5)
    $null = [CodexFixtureKeyboardInput]::SetForegroundWindow($fixtureWindow)
    $foregroundTimer = [Diagnostics.Stopwatch]::StartNew()
    while ($foregroundTimer.ElapsedMilliseconds -lt 2500 -and
        [CodexFixtureKeyboardInput]::GetForegroundWindow() -ne $script:fixtureWindow) {
        Start-Sleep -Milliseconds 25
    }
    if ([CodexFixtureKeyboardInput]::GetForegroundWindow() -ne $script:fixtureWindow) {
        throw 'SKIP_FOREGROUND: the isolated fixture could not acquire the foreground; no keyboard input was sent.'
    }
    Assert-FixtureForeground

    $edit = [CodexFixtureKeyboardInput]::GetDlgItem($fixtureWindow, 103)
    $button = [CodexFixtureKeyboardInput]::GetDlgItem($fixtureWindow, 102)
    if ($edit -eq [IntPtr]::Zero -or $button -eq [IntPtr]::Zero) {
        throw 'The expected keyboard input or safe fixture control is missing.'
    }
    Wait-ForState -Description 'the fixture edit control to have keyboard focus' -Condition {
        [CodexFixtureKeyboardInput]::FocusWindowFor($fixtureWindow) -eq $edit
    }

    [CodexFixtureKeyboardInput]::TypeText('alpha')
    Wait-ForText -Window $edit -Expected 'alpha' -Description 'ordinary character input'

    if ($IncludeModifierProbe) {
        Write-Host 'DIAGNOSTIC: probing physical Ctrl/Shift/Alt with SendInput; this is not a CUA alias regression.'
        Assert-FixtureForeground
        [CodexFixtureKeyboardInput]::Chord(0xA2, 0x41) # Left Ctrl+A
        [CodexFixtureKeyboardInput]::TypeText('beta')
        Wait-ForText -Window $edit -Expected 'beta' -Description 'Ctrl+A selection and replacement'

        Assert-FixtureForeground
        [CodexFixtureKeyboardInput]::Press(0x24) # Home
        [CodexFixtureKeyboardInput]::Chord(0xA0, 0x23) # Left Shift+End
        [CodexFixtureKeyboardInput]::TypeText('gamma')
        Wait-ForText -Window $edit -Expected 'gamma' -Description 'Shift+End selection and replacement'

        Assert-FixtureForeground
        [CodexFixtureKeyboardInput]::Chord(0xA2, 0x4b) # Left Ctrl+K local accelerator
        $status = [CodexFixtureKeyboardInput]::GetDlgItem($fixtureWindow, 101)
        Wait-ForText -Window $status -Expected 'Observed Ctrl+K.' -Description 'Ctrl+K accelerator'

        Assert-FixtureForeground
        [CodexFixtureKeyboardInput]::Chord(0xA0, 0x75) # Left Shift+F6 local accelerator
        Wait-ForText -Window $status -Expected 'Observed Shift+F6.' -Description 'Shift+F6 accelerator'

        Assert-FixtureForeground
        [CodexFixtureKeyboardInput]::Chord(0xA4, 0x77) # Left Alt+F8 local accelerator
        Wait-ForText -Window $status -Expected 'Observed Alt+F8.' -Description 'Alt+F8 accelerator'
    }

    Assert-FixtureForeground
    [CodexFixtureKeyboardInput]::Press(0x09) # Tab
    Wait-ForState -Description 'Tab navigation to the fixture button' -Condition {
        [CodexFixtureKeyboardInput]::FocusWindowFor($fixtureWindow) -eq $button
    }
    [CodexFixtureKeyboardInput]::Press(0x0d) # Enter
    Wait-ForText -Window $status -Expected 'Safe fixture control activated.' -Description 'Enter activation of the safe fixture control'

    Write-Host 'PASS: exact fixture received text, Tab focus navigation, and Enter activation.'
    if (-not $IncludeModifierProbe) {
        Write-Host 'NOTE: physical modifier tests are optional; symbolic @oai/sky aliases require its exact-window API test.'
    }
    exit 0
}
catch {
    if ($_.Exception.Message.StartsWith('SKIP_FOREGROUND:')) {
        Write-Host $_.Exception.Message
        exit 3
    }
    Write-Error $_
    exit 1
}
finally {
    if ($fixtureProcess) {
        try {
            $fixtureProcess.Refresh()
            if (-not $fixtureProcess.HasExited) {
                $null = $fixtureProcess.CloseMainWindow()
                if (-not $fixtureProcess.WaitForExit(2000)) {
                    Stop-Process -Id $fixtureProcess.Id -Force -ErrorAction SilentlyContinue
                    $fixtureProcess.WaitForExit(2000)
                }
            }
        } catch { }
        $fixtureProcess.Dispose()
    }
}
