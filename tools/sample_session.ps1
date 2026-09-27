param(
    [Parameter(Mandatory=$true)][int]$ProcessId,
    [Parameter(Mandatory=$true)][ValidateSet('Campaign','Mission','Any')][string]$Context,
    [Parameter(Mandatory=$true)][string]$Label,
    [ValidateRange(2,360)][int]$Samples=12,
    [ValidateRange(1,60)][int]$IntervalSeconds=10,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
if(-not ('ForgeSamplingWindow' -as [type])) {
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class ForgeSamplingWindow {
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    public static bool IsForeground(int process) {
        uint current; GetWindowThreadProcessId(GetForegroundWindow(), out current);
        return current == process;
    }
}
'@
}
$outputRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $outputRoot){throw 'Choose a new output directory to preserve earlier evidence.'}
[IO.Directory]::CreateDirectory($outputRoot) | Out-Null
$target=Get-Process -Id $ProcessId
$started=$target.StartTime
$records=[Collections.Generic.List[object]]::new()
$clock=[Diagnostics.Stopwatch]::StartNew()
$summary=& "$PSScriptRoot/inspect_session.ps1" -ProcessId $ProcessId -Action summary
$summary | Set-Content -LiteralPath (Join-Path $outputRoot 'summary-before.txt')
$contextPattern='(?m)^Context: '+[regex]::Escape($Context)+'\r?$'
if($summary -notmatch $contextPattern){throw 'Initial game context does not match the requested context.'}
$sessionMatch=[regex]::Match($summary,'(?m)^Session: (\S+)')
if(-not $sessionMatch.Success){throw 'Session identity is missing.'}
$sessionId=$sessionMatch.Groups[1].Value
try {
    for($index=0;$index -lt $Samples;$index++) {
        $foregroundThroughout=[ForgeSamplingWindow]::IsForeground($ProcessId)
        for($second=0;$second -lt $IntervalSeconds;$second++) {
            Start-Sleep -Seconds 1
            if(-not [ForgeSamplingWindow]::IsForeground($ProcessId)){$foregroundThroughout=$false}
        }
        $target=Get-Process -Id $ProcessId
        if($target.StartTime -ne $started){throw 'Process identity changed; sampling stopped.'}
        $current=& "$PSScriptRoot/inspect_session.ps1" -ProcessId $ProcessId -Action summary
        $current | Set-Content -LiteralPath (Join-Path $outputRoot "summary-$index.txt")
        if($current -notmatch $contextPattern -or $current -notmatch ('(?m)^Session: '+[regex]::Escape($sessionId)+'\r?$')){throw 'Game context or session changed; sampling stopped.'}
        $metrics=& "$PSScriptRoot/inspect_session.ps1" -ProcessId $ProcessId -Action metrics
        $metrics | Set-Content -LiteralPath (Join-Path $outputRoot "metrics-$index.json")
        $records.Add([pscustomobject]@{
            Index=$index;Utc=[DateTime]::UtcNow.ToString('o');ElapsedSeconds=$clock.Elapsed.TotalSeconds
            Label=$Label;RequestedContext=$Context;ProcessId=$ProcessId;ProcessStart=$started
            PrivateBytes=$target.PrivateMemorySize64;WorkingSetBytes=$target.WorkingSet64
            CpuSeconds=$target.TotalProcessorTime.TotalSeconds;Metrics=($metrics | ConvertFrom-Json)
            ForegroundAtEveryCheck=$foregroundThroughout
        })
        $records | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $outputRoot 'samples.json')
        Write-Output "Sample $($index+1)/$Samples retained ($Label)."
    }
} catch {
    $_.ToString() | Set-Content -LiteralPath (Join-Path $outputRoot 'error.txt')
    throw
}
# Labels describe the operator's observed scene; raw summaries must be checked
# against that scene before comparing phases. Metrics are rolling tick windows.
Write-Output "Sampling completed: $outputRoot"
