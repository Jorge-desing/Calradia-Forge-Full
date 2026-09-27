Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$workspace = Split-Path -Parent $PSScriptRoot
$desktopDll = Join-Path $workspace "src\CalradiaForge.Desktop\bin\Release\net8.0-windows\CalradiaForge.Desktop.dll"

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "dotnet"
$psi.Arguments = ('"{0}"' -f $desktopDll)
$psi.UseShellExecute = $false
$psi.WorkingDirectory = $workspace

$p = [System.Diagnostics.Process]::Start($psi)

$deadline = [DateTime]::UtcNow.AddSeconds(10)
$window = $null
while ([DateTime]::UtcNow -lt $deadline) {
    $p.Refresh()
    if ($p.MainWindowHandle -ne [IntPtr]::Zero) {
        $candidate = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
        if ($null -ne $candidate) {
            $candidatePid = [int]$candidate.GetCurrentPropertyValue([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $true)
            if ($candidatePid -eq $p.Id) {
                $window = $candidate
                break
            }
        }
    }
    Start-Sleep -Milliseconds 100
}

if ($null -ne $window) {
    Write-Output "Window: $($window.Current.Name)"
    $children = $window.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    Write-Output "Direct Children Count: $($children.Count)"
    foreach ($child in $children) {
        Write-Output "Child: $($child.Current.ControlType.ProgrammaticName) - $($child.Current.AutomationId) - '$($child.Current.Name)'"
    }

    $descendants = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    Write-Output "Descendants Count: $($descendants.Count)"
    foreach ($d in $descendants) {
        $aid = $d.Current.AutomationId
        if (-not [string]::IsNullOrEmpty($aid)) {
            Write-Output "Descendant: $($d.Current.ControlType.ProgrammaticName) - $aid - '$($d.Current.Name)'"
        }
    }
}

try {
    $p.CloseMainWindow()
    if (-not $p.WaitForExit(1000)) { $p.Kill() }
} catch {
    if (-not $p.HasExited) { $p.Kill() }
}
