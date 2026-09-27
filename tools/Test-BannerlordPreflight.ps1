param(
    [string]$GamePath = 'C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord',
    [string]$Version = '22.0.0'
)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$gameRoot = [IO.Path]::GetFullPath($GamePath)
$binRoot = Join-Path $gameRoot 'bin\Win64_Shipping_Client'
$modulesRoot = Join-Path $gameRoot 'Modules'
$requiredRuntimeFiles = @(
    'Bannerlord.exe',
    'TaleWorlds.MountAndBlade.Launcher.exe',
    'TaleWorlds.MountAndBlade.dll',
    'TaleWorlds.Core.dll',
    'TaleWorlds.Library.dll'
)
$moduleIds = @('CalradiaForge', 'CalradiaForgeExamples', 'CalradiaForgePriceProvider', 'CalradiaForgePriceConsumer')

function Test-Manifest([string]$moduleId) {
    $manifest = Join-Path $modulesRoot "$moduleId\SubModule.xml"
    $state = [ordered]@{ id = $moduleId; manifest = $manifest; present = (Test-Path -LiteralPath $manifest); version = $null; versionMatches = $false }
    if ($state.present) {
        [xml]$xml = Get-Content -LiteralPath $manifest -Raw
        $state.version = $xml.Module.Version.value
        $state.versionMatches = $state.version -eq "v$Version"
    }
    return [pscustomobject]$state
}

function Get-LauncherOrderEvidence {
    $candidates = @(
        (Join-Path $gameRoot 'Configs\LauncherData.xml'),
        (Join-Path $gameRoot 'Configs\LauncherData\LauncherData.xml'),
        (Join-Path $env:USERPROFILE 'Documents\Mount and Blade II Bannerlord\Configs\LauncherData.xml')
    ) | Where-Object { Test-Path -LiteralPath $_ }
    foreach ($candidate in $candidates) {
        try {
            [xml]$launcher = Get-Content -LiteralPath $candidate -Raw
            $selected = @($launcher.UserData.SingleplayerData.ModDatas.UserModData | Where-Object {
                $_.Id -and [string]::Equals([string]$_.IsSelected, 'true', [StringComparison]::OrdinalIgnoreCase)
            })
        }
        catch {
            return [pscustomobject]@{
                path = $candidate
                containsForge = $false
                orderVerified = $false
                forgeLastKnownVersion = $null
                selectedModules = @()
                note = "Launcher order file could not be parsed read-only: $($_.Exception.Message)"
            }
        }

        $selectedSummary = @($selected | ForEach-Object {
            [pscustomobject]@{ id = [string]$_.Id; lastKnownVersion = [string]$_.LastKnownVersion }
        })
        $forgeIndex = -1
        $forgeEntry = $null
        for ($index = 0; $index -lt $selected.Count; $index++) {
            if ([string]::Equals([string]$selected[$index].Id, 'CalradiaForge', [StringComparison]::OrdinalIgnoreCase)) {
                $forgeIndex = $index
                $forgeEntry = $selected[$index]
                break
            }
        }
        $requiredBaseIds = @('Native', 'SandBoxCore', 'Sandbox', 'StoryMode')
        $baseIndexes = @()
        foreach ($baseId in $requiredBaseIds) {
            $baseIndex = -1
            for ($index = 0; $index -lt $selected.Count; $index++) {
                if ([string]::Equals([string]$selected[$index].Id, $baseId, [StringComparison]::OrdinalIgnoreCase)) {
                    $baseIndex = $index
                    break
                }
            }
            $baseIndexes += $baseIndex
        }
        $allBaseBeforeForge = $forgeIndex -ge 0 -and @($baseIndexes | Where-Object { $_ -lt 0 -or $_ -ge $forgeIndex }).Count -eq 0
        return [pscustomobject]@{
            path = $candidate
            containsForge = $forgeIndex -ge 0
            orderVerified = $allBaseBeforeForge
            forgeLastKnownVersion = if ($null -eq $forgeEntry) { $null } else { [string]$forgeEntry.LastKnownVersion }
            selectedModules = $selectedSummary
            note = 'Read-only selected-module order evidence. The launcher cache version is informational and is never changed by the preflight.'
        }
    }
    return [pscustomobject]@{ path = $null; containsForge = $false; orderVerified = $false; forgeLastKnownVersion = $null; selectedModules = @(); note = 'Launcher order file was not available for read-only verification.' }
}

$runtime = foreach ($file in $requiredRuntimeFiles) {
    [pscustomobject]@{ file = $file; path = (Join-Path $binRoot $file); present = (Test-Path -LiteralPath (Join-Path $binRoot $file)) }
}
$modules = foreach ($module in $moduleIds) { Test-Manifest $module }
$blseFiles = @('Bannerlord.BLSE.Launcher.exe', 'Bannerlord.BLSE.LauncherEx.exe', 'Bannerlord.BLSE.Standalone.exe') |
    ForEach-Object { [pscustomobject]@{ file = $_; present = (Test-Path -LiteralPath (Join-Path $binRoot $_)) } }
$launcherOrder = Get-LauncherOrderEvidence
$requiredMissing = @($runtime | Where-Object { -not $_.present })
$forge = $modules | Where-Object { $_.id -eq 'CalradiaForge' } | Select-Object -First 1
$result = [ordered]@{
    releaseVersion = $Version
    targetGameVersion = '1.4.8'
    checkedAtUtc = [DateTime]::UtcNow.ToString('O')
    gamePath = $gameRoot
    officialLauncher = (Join-Path $binRoot 'TaleWorlds.MountAndBlade.Launcher.exe')
    runtimeSurface = $runtime
    deployedModules = $modules
    launcherOrder = $launcherOrder
    blse = [ordered]@{ detected = @($blseFiles | Where-Object present).Count -gt 0; files = $blseFiles; note = 'Detection only; no BLSE, Steam, or launcher settings were modified.' }
    canProbe = ($requiredMissing.Count -eq 0 -and $forge.present -and $forge.versionMatches -and $launcherOrder.orderVerified)
    blockers = @()
}
if ($requiredMissing.Count -gt 0) { $result.blockers += 'Missing game runtime file(s): ' + (($requiredMissing | ForEach-Object file) -join ', ') }
if (-not $forge.present) { $result.blockers += 'CalradiaForge is not deployed in the game Modules directory.' }
elseif (-not $forge.versionMatches) { $result.blockers += "CalradiaForge manifest version '$($forge.version)' does not match v$Version." }
if (-not $launcherOrder.orderVerified) { $result.blockers += 'Launcher order was not verified; review it in the official launcher before the probe.' }
$result.blockers = @($result.blockers)

$artifact = Join-Path $workspace "artifacts/bannerlord-preflight-$($Version.Replace('.', '')).json"
($result | ConvertTo-Json -Depth 6) | Set-Content -LiteralPath $artifact -Encoding utf8
$result
if (-not $result.canProbe) { throw "Bannerlord preflight is blocked. See $artifact" }




