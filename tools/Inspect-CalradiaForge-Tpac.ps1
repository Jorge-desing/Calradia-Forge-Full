[CmdletBinding()]
param(
    [string]$ModulePath = '',
    [string]$ToolPath = '',
    [string]$ToolDirectory = '',
    [switch]$InstalledModule,
    [switch]$ValidateOnly,
    [switch]$StructuralOnly
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot

function Get-Sha256Hex([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-', '')
    }
    finally {
        $algorithm.Dispose()
        $stream.Dispose()
    }
}

if ($InstalledModule) {
    if (-not [string]::IsNullOrWhiteSpace($ModulePath)) {
        throw 'Choose either -InstalledModule or -ModulePath, not both.'
    }
    $gameRoot = $env:BANNERLORD_GAME_DIR
    if ([string]::IsNullOrWhiteSpace($gameRoot)) {
        $gameRoot = Join-Path ${env:ProgramFiles(x86)} 'Steam\steamapps\common\Mount & Blade II Bannerlord'
    }
    $ModulePath = Join-Path $gameRoot 'Modules\CalradiaForge'
}
elseif ([string]::IsNullOrWhiteSpace($ModulePath)) {
    $ModulePath = Join-Path $repositoryRoot 'modules\CalradiaForge'
}
$ModulePath = [IO.Path]::GetFullPath($ModulePath)

$packagePath = Join-Path $ModulePath 'Assets\GauntletUI\ui_calradiaforge_1_tex.tpac'
$atlasPath = Join-Path $ModulePath 'AssetSources\GauntletUI\ui_calradiaforge_1.png'
if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
    throw "Calradia Forge sprite TPAC was not found: $packagePath"
}

$packageInfo = Get-Item -LiteralPath $packagePath
if ($packageInfo.Length -eq 0) {
    throw "The sprite TPAC is empty: $packagePath"
}
if ($packageInfo.Length -gt 134217728) {
    throw "The Calradia Forge sprite TPAC exceeds the 128 MiB inspection limit: $($packageInfo.Length) bytes."
}
if ($packageInfo.Length -lt 36) {
    throw "The sprite TPAC is shorter than the 36-byte TPAC v2 header: $($packageInfo.Length) bytes."
}

$stream = [IO.File]::OpenRead($packagePath)
try {
    $header = New-Object byte[] 36
    $bytesRead = $stream.Read($header, 0, $header.Length)
}
finally {
    $stream.Dispose()
}
if ($bytesRead -ne 36) {
    throw "The sprite TPAC header is truncated: expected 36 bytes, read $bytesRead."
}
if ([Text.Encoding]::ASCII.GetString($header, 0, 4) -ne 'TPAC') {
    throw "The sprite package does not have the TPAC header marker: $packagePath"
}

$formatVersion = [BitConverter]::ToUInt32($header, 4)
$packageGuidBytes = New-Object byte[] 16
[Array]::Copy($header, 8, $packageGuidBytes, 0, 16)
$packageGuid = [Guid]::new($packageGuidBytes)
$declaredAssetCount = [BitConverter]::ToUInt32($header, 24)
$tocSize = [BitConverter]::ToUInt64($header, 28)
if ($formatVersion -ne 2) { throw "Unsupported TPAC format version: $formatVersion. This preflight supports version 2." }
if ($declaredAssetCount -eq 0) { throw 'The TPAC declares no assets.' }
if ($declaredAssetCount -gt 100000) { throw "The TPAC declares too many assets for this bounded inspection: $declaredAssetCount." }
if ($tocSize -eq 0 -or $tocSize -gt [uint64]($packageInfo.Length - 36)) {
    throw "The TPAC v2 table of contents ($tocSize bytes) is empty or extends beyond the $($packageInfo.Length)-byte file."
}
$payloadOffset = [uint64]36 + $tocSize

$hash = Get-Sha256Hex $packagePath
Write-Host 'Calradia Forge sprite package preflight'
Write-Host "Module: $ModulePath"
Write-Host "Source atlas: $(if (Test-Path -LiteralPath $atlasPath -PathType Leaf) { 'present' } else { 'missing' })"
Write-Host "Package: $packagePath"
Write-Host "Size: $($packageInfo.Length) bytes"
Write-Host "Header/table preflight: PASS; TPAC v$formatVersion; package GUID: $packageGuid; table: $tocSize bytes; payload starts at $payloadOffset"
Write-Host "Declared metadata assets: $declaredAssetCount"
Write-Host "SHA-256: $hash"
if ($StructuralOnly) {
    Write-Host 'Structural validation: PASS; package header, table bounds, and asset declaration verified.'
    return
}
if ([string]::IsNullOrWhiteSpace($ToolDirectory)) {
    if (-not [string]::IsNullOrWhiteSpace($ToolPath)) {
        $ToolDirectory = Split-Path -Parent ([IO.Path]::GetFullPath($ToolPath))
    }
    else {
        $ToolDirectory = Join-Path $repositoryRoot 'TpacTool\bin'
    }
}
$ToolDirectory = [IO.Path]::GetFullPath($ToolDirectory)
$readerPath = Join-Path $ToolDirectory 'TpacTool.Lib.dll'
if (-not (Test-Path -LiteralPath $readerPath -PathType Leaf)) {
    throw "TPAC structural validation was not run because the optional TpacTool reader is missing: $readerPath. The four-byte marker alone cannot certify this package."
}

try {
    # TpacTool.Lib parses the header and bounded asset metadata without loading
    # texture payloads into memory. Keep this reader out of the game runtime.
    $readerAssembly = [Reflection.Assembly]::LoadFrom($readerPath)
    $packageType = $readerAssembly.GetType('TpacTool.Lib.AssetPackage', $true)
    $packageConstructor = $packageType.GetConstructor([Type[]]@([string], [bool], [bool]))
    if (-not $packageConstructor) {
        throw 'TpacTool does not expose the expected read-only package constructor.'
    }
    $constructorArguments = [object[]]::new(3)
    $constructorArguments[0] = [string]$packagePath
    $constructorArguments[1] = [bool]$true
    $constructorArguments[2] = [bool]$false
    $package = $packageConstructor.Invoke($constructorArguments)
    if (-not $package.HeaderLoaded) {
        throw 'TpacTool did not finish reading the package header.'
    }
    if ($package.Invalid) {
        throw 'TpacTool marked the package invalid.'
    }
    $items = @($package.Items)
    if ($items.Count -eq 0) {
        throw 'The TPAC contains no readable asset metadata entries.'
    }
    if ($items.Count -ne $declaredAssetCount) {
        throw "The parsed asset count ($($items.Count)) does not match the declared count ($declaredAssetCount)."
    }

    $seenGuids = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($item in $items) {
        if ($item.Invalid) {
            throw "TpacTool marked asset '$($item.Name)' invalid."
        }
        if ([string]::IsNullOrWhiteSpace([string]$item.Name)) {
            throw 'The TPAC contains an asset with an empty name.'
        }
        if ($item.Guid -eq [Guid]::Empty) {
            throw "Asset '$($item.Name)' has an empty GUID."
        }
        if (-not $seenGuids.Add($item.Guid.ToString('D'))) {
            throw "The TPAC contains duplicate asset GUID '$($item.Guid)'."
        }
    }
}
catch {
    $message = $_.Exception.Message
    if ($_.Exception.InnerException) { $message = $_.Exception.InnerException.Message }
    $diagnostic = @(
        'Header/table preflight: PASS; package bounds are plausible, but asset metadata remains unverified.'
        'Metadata validation: FAIL; the installed TpacTool reader could not parse the asset entries.'
        "Reason: $message"
        'Next step: check the Resource Browser import result and validate with a reader known to support TPAC v2 for Bannerlord 1.4.8. This reader error alone does not prove the package is malformed.'
        'The source package was not modified.'
    ) -join [Environment]::NewLine
    # Keep validation failures non-zero when run from BAT/CLI, but avoid
    # terminating a caller that invokes this script in-process for fixtures.
    throw $diagnostic
}

$readerVersion = $readerAssembly.GetName().Version.ToString()
Write-Host "TpacTool.Lib: $readerVersion"
Write-Host "Metadata validation: PASS; readable metadata assets: $($items.Count)"
Write-Host 'Scope: parses package header and asset metadata without decoding texture payloads; visual correctness and full payload integrity are not certified.'

if ($ValidateOnly) {
    return
}

if ([string]::IsNullOrWhiteSpace($ToolPath)) {
    $ToolPath = Join-Path (Split-Path -Parent $ToolDirectory) 'TpacTool.exe'
}
$ToolPath = [IO.Path]::GetFullPath($ToolPath)
if (-not (Test-Path -LiteralPath $ToolPath -PathType Leaf)) {
    throw "Optional TpacTool executable was not found: $ToolPath"
}

$toolVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($ToolPath).ProductVersion
if ([string]::IsNullOrWhiteSpace($toolVersion)) { $toolVersion = 'unknown' }
Write-Host "Optional TpacTool version: $toolVersion"
if ($toolVersion -eq '0.4.0' -or $toolVersion -eq '0.4.0.0') {
    Write-Host 'Compatibility note: upstream TpacTool 0.4.0 targets Bannerlord 1.8.0 beta; Calradia Forge targets 1.4.8, so compatibility is unverified.'
}
else {
    Write-Host "Compatibility note: TpacTool $toolVersion has not been verified against the Bannerlord 1.4.8 target."
}
Write-Host 'TpacTool is a viewer/exporter. It cannot import sprites or create TPAC packages.'
Write-Host 'In TpacTool, choose File > Open AssetPackages Folder and select:'
Write-Host (Join-Path $ModulePath 'Assets\GauntletUI')
Start-Process -FilePath $ToolPath -WorkingDirectory (Split-Path -Parent $ToolPath) | Out-Null
