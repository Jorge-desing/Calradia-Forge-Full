[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$InputPackage,
    [string]$OutputJson = '',
    [string]$ToolDirectory = '',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'TpacInventory.psm1') -Force
if ([string]::IsNullOrWhiteSpace($OutputJson)) { $OutputJson = [IO.Path]::GetFullPath($InputPackage) + '.inventory.json' }

try {
    $result = Export-TpacInventory -InputPackage $InputPackage -OutputJson $OutputJson -ToolDirectory $ToolDirectory -Force:$Force
    Write-Host 'TPAC metadata inventory: PASS'
    Write-Host "Package: $($result.Inventory.source.path)"
    Write-Host "Assets: $($result.Inventory.assets.Count)"
    Write-Host "SHA-256: $($result.Inventory.source.sha256)"
    Write-Host "JSON: $($result.OutputPath)"
    if ($result.BackupPath) { Write-Host "Previous JSON backup: $($result.BackupPath)" }
    Write-Host 'Scope: metadata only; no TPAC or texture data was modified or decoded.'
}
catch {
    Write-Host 'TPAC metadata inventory: NOT WRITTEN'
    Write-Host "Reason: $($_.Exception.Message)"
    exit 1
}
