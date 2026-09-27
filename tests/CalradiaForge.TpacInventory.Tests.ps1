[CmdletBinding()]
param(
    [string]$ToolDirectory = '',
    [switch]$RequireReader
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$modulePath = Join-Path $repositoryRoot 'tools\TpacInventory.psm1'
Import-Module $modulePath -Force
$readerDirectory = if ([string]::IsNullOrWhiteSpace($ToolDirectory)) { Join-Path $repositoryRoot 'TpacTool\bin' } else { [IO.Path]::GetFullPath($ToolDirectory) }
$readerPath = Join-Path $readerDirectory 'TpacTool.Lib.dll'
$nativeRoot = if (-not [string]::IsNullOrWhiteSpace($env:BANNERLORD_GAME_DIR)) { $env:BANNERLORD_GAME_DIR } else { Join-Path ${env:ProgramFiles(x86)} 'Steam\steamapps\common\Mount & Blade II Bannerlord' }
$nativePackage = Join-Path $nativeRoot 'Modules\Native\AssetPackages\_shared.tpac'
$forgePackage = Join-Path $nativeRoot 'Modules\CalradiaForge\Assets\GauntletUI\ui_calradiaforge_1_tex.tpac'

function Assert([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Get-FixtureSha256([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-', '') }
    finally { $algorithm.Dispose(); $stream.Dispose() }
}

if (-not (Test-Path -LiteralPath $readerPath -PathType Leaf) -or -not (Test-Path -LiteralPath $nativePackage -PathType Leaf)) {
    $reason = if (-not (Test-Path -LiteralPath $readerPath -PathType Leaf)) { "TpacTool reader is unavailable: $readerPath" } else { "Native TPAC fixture is unavailable: $nativePackage" }
    if ($RequireReader) { throw "Strict TpacTool inventory test requires the reader and Native fixture. $reason" }
    Write-Host "SKIP: $reason"
    exit 0
}

$temp = Join-Path ([IO.Path]::GetTempPath()) ('CalradiaForge-TpacInventory-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp) | Out-Null
try {
    $fixture = Join-Path $temp 'native-shared.tpac'
    Copy-Item -LiteralPath $nativePackage -Destination $fixture
    $sourceHash = Get-FixtureSha256 $fixture
    try {
        $inventory = Read-TpacInventory -InputPackage $fixture -ToolDirectory $readerDirectory
    }
    catch {
        if ($_.Exception.ToString() -match '0x80131515') {
            if ($RequireReader) { throw 'Strict TpacTool inventory test cannot load the downloaded reader because Windows blocked it (0x80131515); use a verified reader copy in an isolated test directory.' }
            Write-Host 'SKIP: Windows blocked loading the downloaded TpacTool.Lib assembly (0x80131515); metadata-reader behavior remains unverified.'
            return
        }
        throw
    }
    Assert ($inventory.schema -eq 'calradiaforge.tpac-inventory/v1') 'Inventory schema identifier is missing.'
    Assert ($inventory.package.formatVersion -eq 2) 'The known Native TPAC version was not retained.'
    Assert ($inventory.assets.Count -eq $inventory.package.declaredAssetCount -and $inventory.assets.Count -gt 0) 'Inventory count differs from the bounded package metadata count.'
    Assert ($inventory.assets[0].name -and $inventory.assets[0].guid -and $inventory.assets[0].typeGuid) 'Inventory asset metadata fields are incomplete.'
    Assert ((Get-FixtureSha256 $fixture) -eq $sourceHash) 'Reading the inventory changed the source TPAC.'

    $output = Join-Path $temp 'native.inventory.json'
    $exported = Export-TpacInventory -InputPackage $fixture -OutputJson $output -ToolDirectory $readerDirectory
    Assert (Test-Path -LiteralPath $output -PathType Leaf) 'Inventory JSON was not created.'
    $json = Get-Content -LiteralPath $output -Raw | ConvertFrom-Json
    Assert ($json.assets.Count -eq $inventory.assets.Count -and $json.source.sha256 -eq $sourceHash) 'Exported JSON does not preserve bounded inventory and source hash.'

    $overwriteRejected = $false
    try { Export-TpacInventory -InputPackage $fixture -OutputJson $output -ToolDirectory $readerDirectory | Out-Null } catch { $overwriteRejected = $true }
    Assert $overwriteRejected 'Existing JSON output was overwritten without explicit -Force.'
    $replacement = Export-TpacInventory -InputPackage $fixture -OutputJson $output -ToolDirectory $readerDirectory -Force
    Assert ($replacement.BackupPath -and (Test-Path -LiteralPath $replacement.BackupPath -PathType Leaf)) 'Explicit replacement did not preserve a timestamped backup.'

    $samePathRejected = $false
    try { Export-TpacInventory -InputPackage $fixture -OutputJson $fixture -ToolDirectory $readerDirectory -Force | Out-Null } catch { $samePathRejected = $true }
    Assert $samePathRejected 'The inventory writer accepted the input TPAC as its output path.'

    if (Test-Path -LiteralPath $forgePackage -PathType Leaf) {
        $forgeCopy = Join-Path $temp 'forge.tpac'
        $forgeOutput = Join-Path $temp 'forge.inventory.json'
        Copy-Item -LiteralPath $forgePackage -Destination $forgeCopy
        $forgeHash = Get-FixtureSha256 $forgeCopy
        $forgeRejected = $false
        try { Export-TpacInventory -InputPackage $forgeCopy -OutputJson $forgeOutput -ToolDirectory $readerDirectory | Out-Null } catch { $forgeRejected = $true }
        Assert $forgeRejected 'The known unparseable Forge package was incorrectly reported as a verified inventory.'
        Assert (-not (Test-Path -LiteralPath $forgeOutput)) 'A failed metadata parse wrote a partial inventory JSON.'
        Assert ((Get-FixtureSha256 $forgeCopy) -eq $forgeHash) 'A failed metadata parse modified the input TPAC.'
    }

    Write-Host 'PASS: Native TPAC metadata inventory, JSON fields and hash, no input mutation, overwrite backup, same-path rejection, and failed-reader no-output behavior.'
}
finally {
    if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
