# Read-only, bounded UI Automation smoke check for the Calradia Forge WPF shell.
# UIA calls run in a child PowerShell process so a blocked provider can be timed out
# without attaching to or terminating an unrelated user process.
[CmdletBinding()]
param(
    [string]$OutputPath = "artifacts/desktop-uia-smoke.json",
    [ValidateRange(5, 180)]
    [int]$TimeoutSeconds = 30,
    [switch]$Worker,
    [int]$TargetProcessId = 0,
    [long]$TargetStartTimeUtcTicks = 0,
    [string]$WorkerResultPathBase64 = ""
)

$ErrorActionPreference = "Stop"
$workspace = Split-Path -Parent $PSScriptRoot
$desktopDll = Join-Path $workspace "src\CalradiaForge.Desktop\bin\Release\net8.0-windows\CalradiaForge.Desktop.dll"

if (-not ("CalradiaForgeUiaForeground" -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
public static class CalradiaForgeUiaForeground {
    [StructLayout(LayoutKind.Sequential)] private struct Message { public IntPtr HWnd; public uint MessageId; public UIntPtr WParam; public IntPtr LParam; public uint Time; public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] private struct WindowRect { public int Left; public int Top; public int Right; public int Bottom; }
    private delegate void WinEventCallback(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint eventThread, uint eventTime);
    private delegate bool EnumWindowsCallback(IntPtr window, IntPtr state);
    private static readonly object Sync = new object();
    private static readonly ManualResetEvent Ready = new ManualResetEvent(false);
    private static readonly List<IntPtr> RecentWindows = new List<IntPtr>();
    private static readonly List<uint> RecentProcessIds = new List<uint>();
    private static readonly List<uint> RecentEventTimes = new List<uint>();
    private static readonly List<uint> RecentEventTypes = new List<uint>();
    private const int MaximumRecentEvents = 256;
    private static WinEventCallback Callback;
    private static Thread Watcher;
    private static IntPtr Hook;
    private static uint WatchPid;
    private static uint WatchThreadId;
    private static volatile bool StopRequested;
    private static IntPtr FirstWindow;
    private static uint FirstProcessId;
    private static uint FirstEventTime;
    private static uint FirstEventType;
    private static volatile bool SawForeground;
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", SetLastError=true)] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] public static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] public static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll", EntryPoint="EnumWindows", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr state);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int maximumLength);
    [DllImport("user32.dll", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr window, out WindowRect rect);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll", EntryPoint="GetWindowLongW", SetLastError=true)] private static extern int GetWindowLong32(IntPtr window, int index);
    [DllImport("user32.dll", EntryPoint="GetWindowLongPtrW", SetLastError=true)] private static extern IntPtr GetWindowLongPtr64(IntPtr window, int index);
    private static long GetExtendedStyle(IntPtr window) { return IntPtr.Size == 8 ? GetWindowLongPtr64(window, -20).ToInt64() : GetWindowLong32(window, -20); }
    [DllImport("user32.dll", SetLastError=true)] private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventCallback callback, uint processId, uint threadId, uint flags);
    [DllImport("user32.dll", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnhookWinEvent(IntPtr hook);
    [DllImport("user32.dll", SetLastError=true)] private static extern int GetMessage(out Message message, IntPtr window, uint min, uint max);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PeekMessage(out Message message, IntPtr window, uint min, uint max, uint remove);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll")] private static extern IntPtr DispatchMessage(ref Message message);
    [DllImport("user32.dll", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PostThreadMessage(uint threadId, uint message, UIntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    public static uint GetProcessId(IntPtr window) { uint processId; GetWindowThreadProcessId(window, out processId); return processId; }
    public static IntPtr FindOwnedMainWindow(uint processId) {
        IntPtr match = IntPtr.Zero;
        EnumWindows(delegate(IntPtr window, IntPtr state) {
            if (!IsWindow(window) || GetProcessId(window) != processId) return true;
            StringBuilder title = new StringBuilder(256);
            GetWindowText(window, title, title.Capacity);
            string value = title.ToString();
            if (!value.StartsWith("Calradia Forge ", StringComparison.Ordinal) && value != "Calradia Forge") return true;
            if (match == IntPtr.Zero) match = window;
            // WPF may expose an auxiliary owned HWND with the same caption
            // before its main inspection HWND; prefer the one carrying the
            // required non-activating style.
            if (HasNoActivateStyle(window)) { match = window; return false; }
            return true;
        }, IntPtr.Zero);
        return match;
    }
    public static string DescribeOwnedWindows(uint processId) {
        List<string> descriptions = new List<string>();
        EnumWindows(delegate(IntPtr window, IntPtr state) {
            if (!IsWindow(window) || GetProcessId(window) != processId) return true;
            StringBuilder title = new StringBuilder(256); GetWindowText(window, title, title.Capacity);
            descriptions.Add("0x" + window.ToInt64().ToString("X") + " [" + title + "] ex=0x" + GetExtendedStyle(window).ToString("X") + " visible=" + IsWindowVisible(window));
            return true;
        }, IntPtr.Zero);
        return String.Join("; ", descriptions.ToArray());
    }
    public static bool IsOutsideVirtualScreen(IntPtr window) {
        WindowRect rect; if (window == IntPtr.Zero || !GetWindowRect(window, out rect)) return false;
        int left = GetSystemMetrics(76), top = GetSystemMetrics(77);
        int right = left + GetSystemMetrics(78), bottom = top + GetSystemMetrics(79);
        return rect.Right <= left || rect.Bottom <= top || rect.Left >= right || rect.Top >= bottom;
    }
    public static string DescribeWindowBounds(IntPtr window) {
        WindowRect rect; if (window == IntPtr.Zero || !GetWindowRect(window, out rect)) return "unavailable";
        return "[" + rect.Left + "," + rect.Top + "," + rect.Right + "," + rect.Bottom + "]";
    }
    public static bool HasNoActivateStyle(IntPtr window) {
        if (window == IntPtr.Zero || !IsWindow(window)) return false;
        long style = IntPtr.Size == 8 ? GetWindowLongPtr64(window, -20).ToInt64() : GetWindowLong32(window, -20);
        return (style & 0x08000000L) != 0;
    }
    public static bool StartWatching() {
        StopWatching();
        lock (Sync) { WatchPid = 0; FirstWindow = IntPtr.Zero; FirstProcessId = 0; FirstEventTime = 0; FirstEventType = 0; SawForeground = false; StopRequested = false; RecentWindows.Clear(); RecentProcessIds.Clear(); RecentEventTimes.Clear(); RecentEventTypes.Clear(); Ready.Reset(); }
        Watcher = new Thread(WatchLoop); Watcher.IsBackground = true; Watcher.Name = "Calradia Forge UIA foreground observer"; Watcher.SetApartmentState(ApartmentState.STA); Watcher.Start();
        if (!Ready.WaitOne(3000)) { StopWatching(); return false; }
        lock (Sync) return Hook != IntPtr.Zero;
    }
    public static bool SetWatchedProcessId(uint processId) {
        if (processId == 0) return false;
        lock (Sync) {
            WatchPid = processId;
            for (int i = 0; i < RecentProcessIds.Count; i++) {
                if (RecentProcessIds[i] != processId) continue;
                if (FirstWindow == IntPtr.Zero) { FirstWindow = RecentWindows[i]; FirstProcessId = processId; FirstEventTime = RecentEventTimes[i]; FirstEventType = RecentEventTypes[i]; }
                SawForeground = true;
                break;
            }
        }
        return true;
    }
    public static void StopWatching() {
        Thread thread = Watcher;
        if (thread == null) return;
        StopRequested = true;
        uint threadId = WatchThreadId;
        if (threadId != 0) PostThreadMessage(threadId, 0x0012, UIntPtr.Zero, IntPtr.Zero);
        if (thread != Thread.CurrentThread) thread.Join(2000);
        lock (Sync) { if (Hook != IntPtr.Zero) { UnhookWinEvent(Hook); Hook = IntPtr.Zero; } Watcher = null; WatchThreadId = 0; }
    }
    public static bool WasWatchedProcessForeground { get { return SawForeground; } }
    public static string FirstForegroundHwnd { get { lock (Sync) return FirstWindow == IntPtr.Zero ? "" : "0x" + FirstWindow.ToInt64().ToString("X"); } }
    public static uint FirstForegroundProcessId { get { lock (Sync) return FirstProcessId; } }
    public static uint FirstForegroundEventTime { get { lock (Sync) return FirstEventTime; } }
    public static uint FirstForegroundEventType { get { lock (Sync) return FirstEventType; } }
    public static bool HookActive { get { lock (Sync) return Hook != IntPtr.Zero; } }
    public static string[] RecentEventSnapshot() {
        lock (Sync) {
            string[] snapshot = new string[RecentWindows.Count];
            for (int i = 0; i < RecentWindows.Count; i++)
                snapshot[i] = "hwnd=0x" + RecentWindows[i].ToInt64().ToString("X") + ";pid=" + RecentProcessIds[i] + ";event=0x" + RecentEventTypes[i].ToString("X") + ";time=" + RecentEventTimes[i];
            return snapshot;
        }
    }
    private static void WatchLoop() {
        WatchThreadId = GetCurrentThreadId(); Message initial; PeekMessage(out initial, IntPtr.Zero, 0, 0, 0);
        Callback = OnForeground;
        // Install before launching the owned WPF process. A PID-filtered hook
        // installed after Process.Start has a startup race that can miss a brief
        // activation before the main HWND receives WS_EX_NOACTIVATE.
        IntPtr hook = SetWinEventHook(0x0003, 0x0003, IntPtr.Zero, Callback, 0, 0, 0);
        lock (Sync) Hook = hook;
        Ready.Set();
        if (hook == IntPtr.Zero) return;
        Message message;
        while (!StopRequested && GetMessage(out message, IntPtr.Zero, 0, 0) > 0) { TranslateMessage(ref message); DispatchMessage(ref message); }
        lock (Sync) { if (Hook != IntPtr.Zero) { UnhookWinEvent(Hook); Hook = IntPtr.Zero; } }
    }
    private static void OnForeground(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint eventThread, uint eventTime) {
        if (window == IntPtr.Zero) return;
        uint processId = GetProcessId(window);
        lock (Sync) {
            if (RecentWindows.Count == MaximumRecentEvents) { RecentWindows.RemoveAt(0); RecentProcessIds.RemoveAt(0); RecentEventTimes.RemoveAt(0); RecentEventTypes.RemoveAt(0); }
            RecentWindows.Add(window); RecentProcessIds.Add(processId); RecentEventTimes.Add(eventTime); RecentEventTypes.Add(eventType);
            if (WatchPid == 0 || processId != WatchPid) return;
            if (FirstWindow == IntPtr.Zero) { FirstWindow = window; FirstProcessId = processId; FirstEventTime = eventTime; FirstEventType = eventType; }
            SawForeground = true;
        }
    }
}
'@
}

function Limit-Text {
    param([AllowNull()][string]$Text, [int]$MaximumLength = 512)
    if ([string]::IsNullOrEmpty($Text)) { return "" }
    if ($Text.Length -le $MaximumLength) { return $Text }
    return $Text.Substring(0, $MaximumLength) + "…"
}

function Resolve-ReportPath {
    param([Parameter(Mandatory = $true)][string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { throw "OutputPath must not be empty." }
    if ([System.IO.Path]::IsPathRooted($Path)) { return [System.IO.Path]::GetFullPath($Path) }
    return [System.IO.Path]::GetFullPath((Join-Path $workspace $Path))
}

function Write-Report {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Report
    )
    $directory = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
    $Report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Stop-OwnedProcess {
    param(
        [AllowNull()][System.Diagnostics.Process]$Process,
        [int]$GraceMilliseconds = 1500
    )
    if ($null -eq $Process) { return }
    try {
        $Process.Refresh()
        if ($Process.HasExited) { return }
        try { [void]$Process.CloseMainWindow() } catch { }
        if ($Process.WaitForExit($GraceMilliseconds)) { return }
        $Process.Kill()
        [void]$Process.WaitForExit(2000)
    } catch {
        Write-Warning ("Could not stop owned process {0}: {1}" -f $Process.Id, (Limit-Text $_.Exception.Message))
    }
}

function Get-ControlType {
    param([Parameter(Mandatory = $true)][string]$ProgrammaticName)
    switch ($ProgrammaticName) {
        "ControlType.Button" { return [System.Windows.Automation.ControlType]::Button }
        "ControlType.ComboBox" { return [System.Windows.Automation.ControlType]::ComboBox }
        "ControlType.CheckBox" { return [System.Windows.Automation.ControlType]::CheckBox }
        "ControlType.Edit" { return [System.Windows.Automation.ControlType]::Edit }
        "ControlType.List" { return [System.Windows.Automation.ControlType]::List }
        "ControlType.Pane" { return [System.Windows.Automation.ControlType]::Pane }
        "ControlType.Tab" { return [System.Windows.Automation.ControlType]::Tab }
        "ControlType.TabItem" { return [System.Windows.Automation.ControlType]::TabItem }
        "ControlType.Text" { return [System.Windows.Automation.ControlType]::Text }
        default { throw "Unsupported expected control type '$ProgrammaticName'." }
    }
}

function Find-UniqueControl {
    param(
        [Parameter(Mandatory = $true)]$Parent,
        [Parameter(Mandatory = $true)][string]$AutomationId,
        [Parameter(Mandatory = $true)][string]$ExpectedType,
        [Parameter(Mandatory = $false)][int]$TargetProcessId = 0
    )

    $expected = Get-ControlType -ProgrammaticName $ExpectedType
    $idCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId
    )
    $typeCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        $expected
    )
    $conditions = [System.Windows.Automation.Condition[]]@($idCondition, $typeCondition)
    $condition = [System.Windows.Automation.AndCondition]::new($conditions)
    $matches = $Parent.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition)

    if ($matches.Count -eq 0) { return [PSCustomObject]@{ State = "Missing"; Element = $null; Count = 0 } }
    if ($matches.Count -ne 1) { return [PSCustomObject]@{ State = "Ambiguous"; Element = $null; Count = $matches.Count } }

    $element = $matches.Item(0)
    $actualProcessId = [int]$element.GetCurrentPropertyValue([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $true)
    if ($TargetProcessId -gt 0 -and $actualProcessId -ne $TargetProcessId) {
        return [PSCustomObject]@{ State = "WrongProcess"; Element = $null; Count = 1 }
    }
    return [PSCustomObject]@{ State = "Found"; Element = $element; Count = 1 }
}

function Get-WorkerInspectionStatus {
    param(
        [Parameter(Mandatory = $true)][string]$ReportPath,
        [Parameter(Mandatory = $true)][int]$ProcessId,
        [Parameter(Mandatory = $true)][long]$ExpectedStartTimeTicks,
        [Parameter(Mandatory = $true)][int]$WaitSeconds
    )

    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $evidence = [System.Collections.Generic.List[object]]::new()
    $status = "Failed"
    $message = "UI Automation inspection did not complete."
    $windowTitle = ""
    $failure = ""

    try {
        try {
            Add-Type -AssemblyName UIAutomationClient -ErrorAction Stop
            Add-Type -AssemblyName UIAutomationTypes -ErrorAction Stop
        } catch {
            $status = "NotRun"
            throw ("Windows UI Automation assemblies are unavailable: {0}" -f (Limit-Text $_.Exception.Message))
        }

        $targetProcess = [System.Diagnostics.Process]::GetProcessById($ProcessId)
        $actualStartTimeTicks = $targetProcess.StartTime.ToUniversalTime().Ticks
        if ($actualStartTimeTicks -ne $ExpectedStartTimeTicks) {
            throw "The process ID no longer identifies the Desktop process started by this check."
        }

        $deadline = [DateTime]::UtcNow.AddSeconds($WaitSeconds)
        $window = $null
        $targetWindowHandle = [CalradiaForgeUiaForeground]::FindOwnedMainWindow([uint32]$ProcessId)
        while ([DateTime]::UtcNow -lt $deadline) {
            $targetProcess.Refresh()
            if ($targetProcess.HasExited) { throw "Desktop process $ProcessId exited before its window became available." }
            $targetWindowHandle = [CalradiaForgeUiaForeground]::FindOwnedMainWindow([uint32]$ProcessId)
            if ($targetWindowHandle -ne [IntPtr]::Zero) {
                try {
                    $candidate = [System.Windows.Automation.AutomationElement]::FromHandle($targetWindowHandle)
                    if ($null -ne $candidate) {
                        $candidatePid = [int]$candidate.GetCurrentPropertyValue([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $true)
                        if ($candidatePid -eq $ProcessId) {
                            $window = $candidate
                            break
                        }
                    }
                } catch { }
            }
            Start-Sleep -Milliseconds 100
        }
        if ($null -eq $window) { throw "Timed out waiting for the main window handle of owned process $ProcessId." }

        $windowInfo = $window.Current
        $windowProcessId = [int]$window.GetCurrentPropertyValue([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $true)
        if ($windowProcessId -ne $ProcessId) { throw "The discovered window does not belong to the owned Desktop process." }
        if (-not [CalradiaForgeUiaForeground]::HasNoActivateStyle($targetWindowHandle)) {
            throw "The inspected Desktop window is missing WS_EX_NOACTIVATE; UIA inspection is stopped."
        }
        if ($windowInfo.ControlType -ne [System.Windows.Automation.ControlType]::Window) {
            throw "The owned main-window handle did not expose ControlType.Window."
        }
        $outsideVirtualScreen = [CalradiaForgeUiaForeground]::IsOutsideVirtualScreen($targetWindowHandle)
        if (-not $outsideVirtualScreen) {
            $bounds = [CalradiaForgeUiaForeground]::DescribeWindowBounds($targetWindowHandle)
            throw "The read-only UIA Desktop window intersects the user's virtual screen at $bounds; UIA inspection is stopped."
        }
        $windowTitle = Limit-Text $windowInfo.Name 256
        $evidence.Add([PSCustomObject]@{
            AutomationId = $windowInfo.AutomationId
            ControlType = $windowInfo.ControlType.ProgrammaticName
            Name = $windowTitle
            IsEnabled = $windowInfo.IsEnabled
            IsOffscreen = $windowInfo.IsOffscreen
            ProcessId = $ProcessId
            NoActivateStyle = $true
            OutsideVirtualScreen = $outsideVirtualScreen
            Test = "owned-main-window"
            Passed = $true
        })

        # Bounded wait for WPF shell controls to be rendered before querying targets
        $shellReadyDeadline = [DateTime]::UtcNow.AddSeconds(10)
        $readyCondition = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
            "WindowMinimizeButton"
        )
        while ([DateTime]::UtcNow -lt $shellReadyDeadline) {
            try {
                $readyMatch = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $readyCondition)
                if ($null -ne $readyMatch) { break }
            } catch { }
            Start-Sleep -Milliseconds 100
        }

        # This smoke check only reads shell metadata. It never changes selection,
        # activates controls, opens dialogs, executes a work order, or saves preferences.
        $targets = @(
            @{ Id = "WindowMinimizeButton"; Type = "ControlType.Button"; Required = $true },
            @{ Id = "WindowCloseButton"; Type = "ControlType.Button"; Required = $true },
            @{ Id = "HeaderSealButton"; Type = "ControlType.Button"; Required = $true },
            @{ Id = "CommandPaletteButton"; Type = "ControlType.Button"; Required = $true },
            @{ Id = "ConnectButton"; Type = "ControlType.Button"; Required = $true },
            @{ Id = "ContextDossierToggleButton"; Type = "ControlType.Button"; Required = $true },
            @{ Id = "SplitDeckToggleButton"; Type = "ControlType.Button"; Required = $true },
            @{ Id = "LanguageSelector"; Type = "ControlType.ComboBox"; Required = $true },
            @{ Id = "ThemeSelector"; Type = "ControlType.ComboBox"; Required = $true },
            @{ Id = "CategorySelector"; Type = "ControlType.ComboBox"; Required = $true },
            @{ Id = "DecorativeAccentsToggle"; Type = "ControlType.CheckBox"; Required = $true },
            @{ Id = "OperationalSearchFilter"; Type = "ControlType.Edit"; Required = $true },
            @{ Id = "OperationalRailToolViewport"; Type = "ControlType.List"; Required = $true },
            @{ Id = "WorkbenchPageScrollViewport"; Type = "ControlType.Pane"; Required = $true },
            @{ Id = "WorkbenchReportTabs"; Type = "ControlType.Tab"; Required = $false },
            @{ Id = "WorkbenchEvidenceTab"; Type = "ControlType.TabItem"; Required = $false },
            @{ Id = "WorkbenchRawResultTab"; Type = "ControlType.TabItem"; Required = $false },
            @{ Id = "EvidenceLedgerItems"; Type = "ControlType.List"; Required = $false },
            @{ Id = "KeyboardShortcutHint"; Type = "ControlType.Text"; Required = $true }
        )

        $failedRequired = 0
        foreach ($target in $targets) {
            $match = Find-UniqueControl -Parent $window -AutomationId $target.Id -ExpectedType $target.Type -TargetProcessId $ProcessId
            $isFound = $match.State -eq "Found"
            $passed = $isFound -or (-not $target.Required -and $match.State -eq "Missing")
            if (-not $passed -and $target.Required) { $failedRequired++ }

            $record = [PSCustomObject]@{
                AutomationId = $target.Id
                ExpectedType = $target.Type
                State = $match.State
                MatchCount = $match.Count
                Required = $target.Required
                Test = "read-only-shell-control"
                Passed = $passed
            }
            if ($isFound) {
                $current = $match.Element.Current
                $record | Add-Member -NotePropertyName ControlType -NotePropertyValue $current.ControlType.ProgrammaticName
                $record | Add-Member -NotePropertyName Name -NotePropertyValue (Limit-Text $current.Name 256)
                $record | Add-Member -NotePropertyName IsEnabled -NotePropertyValue $current.IsEnabled
                $record | Add-Member -NotePropertyName IsOffscreen -NotePropertyValue $current.IsOffscreen
                $record | Add-Member -NotePropertyName ProcessId -NotePropertyValue ([int]$match.Element.GetCurrentPropertyValue([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $true))
            }
            $evidence.Add($record)
        }

        if ($failedRequired -gt 0) {
            $status = "Failed"
            $message = "$failedRequired required shell control(s) were missing, ambiguous, or had a process mismatch. No control was activated."
        } else {
            $status = "Passed"
            $message = "Owned main window and required shell controls were inspected read-only. No picker, work order, or preference was changed."
        }
    } catch {
        if ($status -ne "NotRun") { $status = "Failed" }
        $failure = Limit-Text $_.Exception.Message
        if ($failure -match "(?i)timed out|timeout") { $status = "TimedOut" }
        $message = $failure
    } finally {
        $watch.Stop()
    }

    $report = [PSCustomObject]@{
        Status = $status
        Message = $message
        Failure = $failure
        TotalDurationMs = $watch.ElapsedMilliseconds
        ProcessId = $ProcessId
        WindowTitle = $windowTitle
        VerifiedControls = @($evidence | Where-Object { $_.Passed }).Count
        TotalQueried = $evidence.Count
        Scope = "Read-only UIA inspection of the main window belonging to the process launched by this runner."
        NotExamined = @("File and folder pickers", "Work-order execution", "Preference changes", "Visual approval")
        Evidence = $evidence
    }
    Write-Report -Path $ReportPath -Report $report
    return $status
}

if ($Worker) {
    $workerReportPath = ""
    try {
        if ($TargetProcessId -le 0 -or $TargetStartTimeUtcTicks -le 0 -or [string]::IsNullOrWhiteSpace($WorkerResultPathBase64)) {
            throw "Internal worker arguments are incomplete."
        }
        $workerReportPath = [System.Text.Encoding]::Unicode.GetString([Convert]::FromBase64String($WorkerResultPathBase64))
        $status = Get-WorkerInspectionStatus -ReportPath $workerReportPath -ProcessId $TargetProcessId -ExpectedStartTimeTicks $TargetStartTimeUtcTicks -WaitSeconds $TimeoutSeconds
        if ($status -eq "Passed") { exit 0 }
        exit 1
    } catch {
        if ($workerReportPath) {
            try {
                Write-Report -Path $workerReportPath -Report ([PSCustomObject]@{
                    Status = "Failed"
                    Message = "UIA worker failed before inspection: $(Limit-Text $_.Exception.Message)"
                    Failure = Limit-Text $_.Exception.Message
                    TotalDurationMs = 0
                    ProcessId = $TargetProcessId
                    WindowTitle = ""
                    VerifiedControls = 0
                    TotalQueried = 0
                    Scope = "Read-only UIA inspection of the owned main window."
                    NotExamined = @("File and folder pickers", "Work-order execution", "Preference changes", "Visual approval")
                    Evidence = @()
                })
            } catch { }
        }
        exit 1
    }
}

$startedAt = [DateTime]::UtcNow
$originalForegroundWindow = [CalradiaForgeUiaForeground]::GetForegroundWindow()
$originalForegroundProcessId = [CalradiaForgeUiaForeground]::GetProcessId($originalForegroundWindow)
$foregroundChangedByOwnedProcess = $false
$foregroundHookStarted = $false
$windowNoActivateStyle = $false
$ownedForegroundEventObserved = $false
$firstOwnedForegroundHwnd = ""
$firstOwnedForegroundEventProcessId = 0
$ownedDesktopProcess = $null
$ownedWorkerProcess = $null
$temporaryWorkerReport = Join-Path ([System.IO.Path]::GetTempPath()) ("calradia-forge-uia-{0}.json" -f [Guid]::NewGuid().ToString("N"))
$reportPath = ""
$scriptExitCode = 1
$finalReport = $null

try {
    $reportPath = Resolve-ReportPath -Path $OutputPath
    if (-not (Test-Path -LiteralPath $desktopDll -PathType Leaf)) {
        $finalReport = [PSCustomObject]@{
            Status = "NotRun"; Message = "Desktop build output is missing; build the solution before running this opt-in check."
            Failure = "Missing assembly: $desktopDll"; TotalDurationMs = 0; ProcessId = $null; WindowTitle = ""
            VerifiedControls = 0; TotalQueried = 0; Scope = "No application process was started."
            NotExamined = @("All UI Automation checks"); Evidence = @()
        }
        throw $finalReport.Message
    }

    Write-Host "Starting a dedicated Calradia Forge Desktop process for read-only UI inspection..." -ForegroundColor Cyan
    $desktopPsi = New-Object System.Diagnostics.ProcessStartInfo
    $desktopPsi.FileName = "dotnet"
    $desktopPsi.Arguments = ('"{0}"' -f $desktopDll)
    $desktopPsi.WorkingDirectory = $workspace
    $desktopPsi.UseShellExecute = $false
    # dotnet is a console host even though the target WPF assembly is WinExe.
    # Do not let the child inherit or create a visible console that can steal
    # foreground when this checker is started from Explorer or another app.
    $desktopPsi.CreateNoWindow = $true
    $desktopPsi.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
    $desktopPsi.EnvironmentVariables["CALRADIA_FORGE_UIA_READ_ONLY"] = "1"
    if (-not [CalradiaForgeUiaForeground]::StartWatching()) {
        throw "Could not install the foreground observer before launching the owned Desktop process. UIA inspection was stopped before the app could start."
    }
    $foregroundHookStarted = $true
    $ownedDesktopProcess = [System.Diagnostics.Process]::Start($desktopPsi)
    $ownedDesktopProcess.Refresh()
    $processStartTicks = $ownedDesktopProcess.StartTime.ToUniversalTime().Ticks
    if (-not [CalradiaForgeUiaForeground]::SetWatchedProcessId([uint32]$ownedDesktopProcess.Id)) {
        throw "Could not scope the preinstalled foreground observer to the owned Desktop process. UIA inspection was stopped before inspection."
    }

    $windowDeadline = [DateTime]::UtcNow.AddSeconds([Math]::Min(15, $TimeoutSeconds))
    $ownedWindowHandle = [CalradiaForgeUiaForeground]::FindOwnedMainWindow([uint32]$ownedDesktopProcess.Id)
    while ([DateTime]::UtcNow -lt $windowDeadline) {
        $ownedDesktopProcess.Refresh()
        if ($ownedDesktopProcess.HasExited) { throw "Owned Desktop process $($ownedDesktopProcess.Id) exited before its main window became available." }
        $ownedWindowHandle = [CalradiaForgeUiaForeground]::FindOwnedMainWindow([uint32]$ownedDesktopProcess.Id)
        if ($ownedWindowHandle -ne [IntPtr]::Zero -and [CalradiaForgeUiaForeground]::HasNoActivateStyle($ownedWindowHandle)) { break }
        Start-Sleep -Milliseconds 100
    }
    if ($ownedWindowHandle -eq [IntPtr]::Zero) {
        throw "Timed out waiting for the owned Desktop main window before starting UI Automation."
    }
    $windowNoActivateStyle = [CalradiaForgeUiaForeground]::HasNoActivateStyle($ownedWindowHandle)
    if (-not $windowNoActivateStyle) {
        $windowDiagnostics = [CalradiaForgeUiaForeground]::DescribeOwnedWindows([uint32]$ownedDesktopProcess.Id)
        throw "The owned Desktop window does not have WS_EX_NOACTIVATE. UI Automation was not started. Windows: $windowDiagnostics"
    }

    $encodedReportPath = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($temporaryWorkerReport))
    $workerScript = $PSCommandPath
    $workerArguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$workerScript`" -Worker -TargetProcessId $($ownedDesktopProcess.Id) -TargetStartTimeUtcTicks $processStartTicks -TimeoutSeconds $TimeoutSeconds -WorkerResultPathBase64 $encodedReportPath"
    $ownedWorkerProcess = Start-Process -FilePath (Join-Path $PSHOME "powershell.exe") -ArgumentList $workerArguments -WorkingDirectory $workspace -WindowStyle Hidden -PassThru

    $workerDeadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds + 3)
    while ([DateTime]::UtcNow -lt $workerDeadline) {
        $ownedDesktopProcess.Refresh()
        $ownedWorkerProcess.Refresh()
        if ($ownedDesktopProcess.HasExited) {
            throw "Owned Desktop process $($ownedDesktopProcess.Id) exited before UI Automation completed (exit code $($ownedDesktopProcess.ExitCode))."
        }
        if ($ownedWorkerProcess.HasExited) { break }
        Start-Sleep -Milliseconds 100
    }

    $ownedWorkerProcess.Refresh()
    if (-not $ownedWorkerProcess.HasExited) {
        Stop-OwnedProcess -Process $ownedWorkerProcess -GraceMilliseconds 0
        $finalReport = [PSCustomObject]@{
            Status = "TimedOut"
            Message = "The UI Automation worker exceeded the $TimeoutSeconds second deadline and was terminated; the owned Desktop process will be closed."
            Failure = "A synchronous UI Automation provider call may have blocked. No retry was attempted."
            TotalDurationMs = [int64]([DateTime]::UtcNow - $startedAt).TotalMilliseconds
            ProcessId = $ownedDesktopProcess.Id
            WindowTitle = ""
            VerifiedControls = 0
            TotalQueried = 0
            Scope = "Only the worker and Desktop process launched by this runner were managed."
            NotExamined = @("Remaining controls", "File and folder pickers", "Work-order execution", "Preference changes", "Visual approval")
            Evidence = @()
        }
        Write-Warning $finalReport.Message
    } elseif (Test-Path -LiteralPath $temporaryWorkerReport -PathType Leaf) {
        $finalReport = Get-Content -LiteralPath $temporaryWorkerReport -Raw | ConvertFrom-Json
        if ($finalReport.Status -eq "Passed" -and $ownedWorkerProcess.ExitCode -eq 0) {
            $scriptExitCode = 0
            Write-Host "Read-only UIA inspection passed: $($finalReport.VerifiedControls)/$($finalReport.TotalQueried) observed records." -ForegroundColor Green
        } else {
            $finalReport.Status = if ($finalReport.Status -in @("TimedOut", "NotRun")) { $finalReport.Status } else { "Failed" }
            $finalReport.Message = Limit-Text $finalReport.Message
            Write-Warning ("UIA inspection {0}: {1}" -f $finalReport.Status, $finalReport.Message)
        }
    } else {
        $finalReport = [PSCustomObject]@{
            Status = "Failed"
            Message = "UIA worker exited without writing its report (exit code $($ownedWorkerProcess.ExitCode))."
            Failure = "No worker result file was produced."
            TotalDurationMs = [int64]([DateTime]::UtcNow - $startedAt).TotalMilliseconds
            ProcessId = $ownedDesktopProcess.Id
            WindowTitle = ""
            VerifiedControls = 0
            TotalQueried = 0
            Scope = "Only the worker and Desktop process launched by this runner were managed."
            NotExamined = @("All control checks", "Visual approval")
            Evidence = @()
        }
        Write-Warning $finalReport.Message
    }
} catch {
    if ($null -eq $finalReport) {
        $failureMessage = Limit-Text $_.Exception.Message
        $failureStatus = if ($failureMessage -match "(?i)timed out|timeout|deadline") { "TimedOut" } else { "Failed" }
        $finalReport = [PSCustomObject]@{
            Status = $failureStatus
            Message = $failureMessage
            Failure = $failureMessage
            TotalDurationMs = [int64]([DateTime]::UtcNow - $startedAt).TotalMilliseconds
            ProcessId = if ($null -ne $ownedDesktopProcess) { $ownedDesktopProcess.Id } else { $null }
            WindowTitle = ""
            VerifiedControls = 0
            TotalQueried = 0
            Scope = "Only processes started by this runner may be managed."
            NotExamined = @("Remaining controls", "Visual approval")
            Evidence = @()
        }
    }
    Write-Warning ("Desktop UIA check {0}: {1}" -f $finalReport.Status, $finalReport.Message)
} finally {
    if ($null -ne $ownedDesktopProcess) {
        # Let out-of-context WinEvent callbacks already queued to the dedicated
        # message pump be observed before recording the passive focus result.
        Start-Sleep -Milliseconds 75
        $currentForegroundWindow = [CalradiaForgeUiaForeground]::GetForegroundWindow()
        $currentForegroundProcessId = [CalradiaForgeUiaForeground]::GetProcessId($currentForegroundWindow)
        $ownedForegroundEventObserved = $foregroundHookStarted -and [CalradiaForgeUiaForeground]::WasWatchedProcessForeground
        $firstOwnedForegroundHwnd = if ($foregroundHookStarted) { [CalradiaForgeUiaForeground]::FirstForegroundHwnd } else { "" }
        $firstOwnedForegroundEventProcessId = if ($foregroundHookStarted) { [CalradiaForgeUiaForeground]::FirstForegroundProcessId } else { 0 }
        $foregroundChangedByOwnedProcess = $ownedForegroundEventObserved -or ($currentForegroundProcessId -eq $ownedDesktopProcess.Id -and $currentForegroundWindow -ne $originalForegroundWindow)
        if ($foregroundChangedByOwnedProcess -and $null -ne $finalReport) {
            $finalReport.Status = "Failed"
            $finalReport.Message = "The read-only UIA Desktop process was observed taking foreground; the run fails. No foreground restoration is attempted."
            $finalReport.Failure = "A PID-filtered EVENT_SYSTEM_FOREGROUND hook observed HWND $firstOwnedForegroundHwnd (PID $firstOwnedForegroundEventProcessId)."
            $scriptExitCode = 1
        }
    }
    Stop-OwnedProcess -Process $ownedWorkerProcess -GraceMilliseconds 0
    Stop-OwnedProcess -Process $ownedDesktopProcess -GraceMilliseconds 1500
    if ($foregroundHookStarted) {
        [CalradiaForgeUiaForeground]::StopWatching()
        $ownedForegroundEventObserved = [CalradiaForgeUiaForeground]::WasWatchedProcessForeground
        $firstOwnedForegroundHwnd = [CalradiaForgeUiaForeground]::FirstForegroundHwnd
        $firstOwnedForegroundEventProcessId = [CalradiaForgeUiaForeground]::FirstForegroundProcessId
        if ($ownedForegroundEventObserved) {
            $foregroundChangedByOwnedProcess = $true
            $scriptExitCode = 1
            if ($null -ne $finalReport) {
                $finalReport.Status = "Failed"
                $finalReport.Message = "The read-only UIA Desktop process was observed taking foreground; the run fails. No foreground restoration is attempted."
                $finalReport.Failure = "A PID-filtered EVENT_SYSTEM_FOREGROUND hook observed HWND $firstOwnedForegroundHwnd (PID $firstOwnedForegroundEventProcessId)."
            }
        }
    }
    if (Test-Path -LiteralPath $temporaryWorkerReport -PathType Leaf) {
        Remove-Item -LiteralPath $temporaryWorkerReport -Force -ErrorAction SilentlyContinue
    }
    if ($reportPath) {
        try {
            $finalReport.TotalDurationMs = [int64]([DateTime]::UtcNow - $startedAt).TotalMilliseconds
    $finalForegroundWindow = [CalradiaForgeUiaForeground]::GetForegroundWindow()
    $finalForegroundProcessId = [CalradiaForgeUiaForeground]::GetProcessId($finalForegroundWindow)
            $foregroundTransitions = if ($foregroundHookStarted) { [CalradiaForgeUiaForeground]::RecentEventSnapshot() } else { @() }
            $finalReport | Add-Member -NotePropertyName ForegroundUnchanged -NotePropertyValue ($finalForegroundWindow -eq $originalForegroundWindow) -Force
            $finalReport | Add-Member -NotePropertyName ForegroundChangedByOwnedProcess -NotePropertyValue $foregroundChangedByOwnedProcess -Force
            $finalReport | Add-Member -NotePropertyName ForegroundRestorationAttempted -NotePropertyValue $false -Force
            $finalReport | Add-Member -NotePropertyName ForegroundEventObserved -NotePropertyValue $ownedForegroundEventObserved -Force
            $finalReport | Add-Member -NotePropertyName FirstForegroundEventHwnd -NotePropertyValue $firstOwnedForegroundHwnd -Force
            $finalReport | Add-Member -NotePropertyName ForegroundEventProcessId -NotePropertyValue $firstOwnedForegroundEventProcessId -Force
            $finalReport | Add-Member -NotePropertyName ForegroundEventType -NotePropertyValue $(if ($ownedForegroundEventObserved) { "EVENT_SYSTEM_FOREGROUND" } else { "" }) -Force
            $finalReport | Add-Member -NotePropertyName ForegroundEventTypeId -NotePropertyValue $(if ($foregroundHookStarted) { [CalradiaForgeUiaForeground]::FirstForegroundEventType } else { 0 }) -Force
            $finalReport | Add-Member -NotePropertyName ForegroundEventTime -NotePropertyValue $(if ($foregroundHookStarted) { [CalradiaForgeUiaForeground]::FirstForegroundEventTime } else { 0 }) -Force
            $finalReport | Add-Member -NotePropertyName OriginalForegroundHwnd -NotePropertyValue $(if ($originalForegroundWindow -eq [IntPtr]::Zero) { "0x0" } else { "0x" + $originalForegroundWindow.ToInt64().ToString("X") }) -Force
            $finalReport | Add-Member -NotePropertyName OriginalForegroundProcessId -NotePropertyValue $originalForegroundProcessId -Force
            $finalReport | Add-Member -NotePropertyName FinalForegroundHwnd -NotePropertyValue $(if ($finalForegroundWindow -eq [IntPtr]::Zero) { "0x0" } else { "0x" + $finalForegroundWindow.ToInt64().ToString("X") }) -Force
            $finalReport | Add-Member -NotePropertyName FinalForegroundProcessId -NotePropertyValue $finalForegroundProcessId -Force
            $finalReport | Add-Member -NotePropertyName ForegroundTransitions -NotePropertyValue @($foregroundTransitions) -Force
            $finalReport | Add-Member -NotePropertyName WindowNoActivateStyle -NotePropertyValue $windowNoActivateStyle -Force
            Write-Report -Path $reportPath -Report $finalReport
            Write-Host "Saved UIA report: $reportPath" -ForegroundColor Cyan
        } catch {
            Write-Warning ("Could not write the UIA report to '{0}': {1}" -f $reportPath, (Limit-Text $_.Exception.Message))
            $scriptExitCode = 1
        }
    }
}

exit $scriptExitCode
