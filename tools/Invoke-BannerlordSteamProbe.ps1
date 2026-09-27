param(
    [string]$GamePath = 'C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord',
    [string]$Version = '22.0.0',
    [int]$WaitSeconds = 30
)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$startedAt = [DateTime]::UtcNow
$preflight = & (Join-Path $PSScriptRoot 'Test-BannerlordPreflight.ps1') -GamePath $GamePath -Version $Version
$logRoot = 'C:\ProgramData\Mount and Blade II Bannerlord\logs'

# Steam owns process startup and shutdown. This uses Bannerlord's public Steam application id,
# does not pass launch arguments, and does not rewrite Steam, launcher, BLSE, or save files.
Start-Process 'steam://rungameid/261550'
$deadline = [DateTime]::UtcNow.AddSeconds($WaitSeconds)
$gameProcesses = @()
do {
    Start-Sleep -Seconds 2
    $gameProcesses = @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $_.ProcessName -match '^(Bannerlord|TaleWorlds\.MountAndBlade\.Launcher)$' -and $_.StartTime.ToUniversalTime() -ge $startedAt.AddSeconds(-2)
    } | Select-Object ProcessName, Id, StartTime, Path)
} while ($gameProcesses.Count -eq 0 -and [DateTime]::UtcNow -lt $deadline)

$log = $null
if (Test-Path -LiteralPath $logRoot) {
    $log = Get-ChildItem -LiteralPath $logRoot -Filter 'launcher_log_*.txt' -File -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTimeUtc -ge $startedAt.AddSeconds(-1) } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
}
$launcherLog = if ($null -eq $log) { $null } else { Get-Content -LiteralPath $log.FullName -Raw }
$status = if ($gameProcesses.Count -gt 0) { 'game-process-detected' } elseif ($launcherLog -match '(?im)^ERROR') { 'blocked' } else { 'not-started-within-short-window' }
$result = [ordered]@{
    releaseVersion = $Version
    startedAtUtc = $startedAt.ToString('O')
    launchMethod = 'steam://rungameid/261550'
    waitSeconds = $WaitSeconds
    status = $status
    gameProcesses = $gameProcesses
    firstLauncherError = if ($status -eq 'blocked') { ($launcherLog -replace '\r?\n', ' ').Trim() } else { $null }
    launcherLog = if ($null -eq $log) { $null } else { $log.FullName }
    preflightCanProbe = [bool]$preflight.canProbe
    note = if ($status -eq 'game-process-detected') {
        'Steam started a Bannerlord process. This short process observation does not claim that the main menu or Forge panel was inspected.'
    } elseif ($status -eq 'blocked') {
        'Steam launch did not start a Bannerlord process and produced a concrete official-launcher error. No settings or save data were changed.'
    } else {
        'Steam launch did not start a Bannerlord process during the short observation window. No settings or save data were changed.'
    }
}
$artifact = Join-Path $workspace "artifacts/bannerlord-steam-probe-$($Version.Replace('.', '')).json"
($result | ConvertTo-Json -Depth 5) | Set-Content -LiteralPath $artifact -Encoding utf8
[pscustomobject]$result
if ($status -ne 'game-process-detected') { exit 2 }




