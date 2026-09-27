[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BeforeReport,
    [Parameter(Mandatory = $true)][string]$AfterReport,
    [string]$OutputJson = '',
    [switch]$IncludeUnchanged,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'TpacInventory.psm1') -Force

function Write-AtomicJson([string]$Path, [string]$Json, [switch]$AllowReplace) {
    $fullPath = [IO.Path]::GetFullPath($Path)
    $directory = Split-Path -Parent $fullPath
    if (-not [IO.Directory]::Exists($directory)) { [IO.Directory]::CreateDirectory($directory) | Out-Null }
    if ([IO.File]::Exists($fullPath) -and -not $AllowReplace) { throw "Output already exists; pass -Force to replace it with a backup: $fullPath" }

    $tempPath = $fullPath + '.tmp.' + [Guid]::NewGuid().ToString('N')
    $backupPath = $null
    try {
        [IO.File]::WriteAllText($tempPath, $Json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
        if ([IO.File]::Exists($fullPath)) {
            $backupPath = $fullPath + '.bak.' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')
            [IO.File]::Replace($tempPath, $fullPath, $backupPath)
        }
        else { [IO.File]::Move($tempPath, $fullPath) }
        return $backupPath
    }
    catch {
        $sourcePresent = [IO.File]::Exists($tempPath)
        $destinationPresent = [IO.File]::Exists($fullPath)
        if ([IO.File]::Exists($tempPath)) { [IO.File]::Delete($tempPath) }
        throw "Could not atomically write '$fullPath' using temporary file '$tempPath' (source present: $sourcePresent; destination present: $destinationPresent): $($_.Exception.Message)"
    }
}

try {
    $beforePath = [IO.Path]::GetFullPath($BeforeReport)
    $afterPath = [IO.Path]::GetFullPath($AfterReport)
    $comparison = Compare-TpacInventory -BeforeReport $beforePath -AfterReport $afterPath -IncludeUnchanged:$IncludeUnchanged

    Write-Host 'TPAC inventory comparison: COMPLETE'
    Write-Host "Before: $($comparison.before.source.path) ($($comparison.before.source.sha256))"
    Write-Host "Before captured UTC: $($comparison.before.capturedUtc)"
    Write-Host "After:  $($comparison.after.source.path) ($($comparison.after.source.sha256))"
    Write-Host "After captured UTC:  $($comparison.after.capturedUtc)"
    Write-Host "Compared UTC:        $($comparison.createdUtc)"
    Write-Host "Added: $($comparison.summary.added)  Removed: $($comparison.summary.removed)  Changed: $($comparison.summary.changed)  Unchanged: $($comparison.summary.unchanged)"
    if ($comparison.items.Count -eq 0) { Write-Host 'No asset metadata differences were found.' }
    foreach ($item in $comparison.items) {
        $fields = if ($item.changedFields.Count -gt 0) { ' [' + ($item.changedFields -join ', ') + ']' } else { '' }
        Write-Host (('{0,-9} {1}{2}' -f $item.status.ToUpperInvariant(), $item.name, $fields))
    }
    Write-Host $comparison.scope
    Write-Host 'Neither input inventory report nor TPAC package was modified.'

    if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
        $outputPath = [IO.Path]::GetFullPath($OutputJson)
        if ([string]::Equals($outputPath, $beforePath, [StringComparison]::OrdinalIgnoreCase) -or
            [string]::Equals($outputPath, $afterPath, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Output JSON path must not point to either input inventory report.'
        }
        if ([IO.Path]::GetExtension($outputPath) -ine '.json') { throw 'Comparison output must use the .json extension.' }
        $json = ConvertTo-Json -InputObject $comparison -Depth 8
        $backupPath = Write-AtomicJson -Path $outputPath -Json $json -AllowReplace:$Force
        Write-Host "JSON: $outputPath"
        if ($backupPath) { Write-Host "Previous JSON backup: $backupPath" }
    }
}
catch {
    Write-Host 'TPAC inventory comparison: NOT WRITTEN'
    Write-Host "Reason: $($_.Exception.Message)"
    exit 2
}
