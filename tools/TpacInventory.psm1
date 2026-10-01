Set-StrictMode -Version Latest

function Get-TpacSha256([string]$Path) {
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

function Read-TpacInventory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$InputPackage,
        [string]$ToolDirectory = ''
    )

    $packagePath = [IO.Path]::GetFullPath($InputPackage)
    if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) { throw "TPAC package was not found: $packagePath" }
    if ([IO.Path]::GetExtension($packagePath) -ine '.tpac') { throw 'Input must be a .tpac file.' }
    $packageInfo = Get-Item -LiteralPath $packagePath
    if ($packageInfo.Length -lt 36) { throw "TPAC v2 fixed header is truncated: $($packageInfo.Length) bytes; expected at least 36." }
    if ($packageInfo.Length -gt 134217728) { throw "TPAC exceeds the bounded 128 MiB inventory limit: $($packageInfo.Length) bytes." }

    $stream = [IO.File]::Open($packagePath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $header = New-Object byte[] 36
        $read = 0
        while ($read -lt $header.Length) {
            $count = $stream.Read($header, $read, $header.Length - $read)
            if ($count -eq 0) { break }
            $read += $count
        }
    }
    finally { $stream.Dispose() }
    if ($read -ne 36) { throw "TPAC v2 fixed header is truncated: read $read of 36 bytes." }
    if ([Text.Encoding]::ASCII.GetString($header, 0, 4) -ne 'TPAC') { throw 'Input does not have the TPAC marker.' }

    $formatVersion = [BitConverter]::ToUInt32($header, 4)
    if ($formatVersion -ne 2) { throw "Unsupported TPAC version $formatVersion; this inventory reader is limited to version 2." }
    $guidBytes = New-Object byte[] 16
    [Array]::Copy($header, 8, $guidBytes, 0, 16)
    $packageGuid = [Guid]::new($guidBytes)
    $declaredCount = [BitConverter]::ToUInt32($header, 24)
    if ($declaredCount -eq 0 -or $declaredCount -gt 100000) { throw "TPAC declares an out-of-range metadata count: $declaredCount." }
    $tocSize = [BitConverter]::ToUInt64($header, 28)
    if ($tocSize -eq 0 -or $tocSize -gt [uint64]($packageInfo.Length - 36)) { throw "TPAC v2 table-of-contents range is empty or outside the $($packageInfo.Length)-byte file." }

    if ([string]::IsNullOrWhiteSpace($ToolDirectory)) { $ToolDirectory = Join-Path (Split-Path -Parent $PSScriptRoot) 'TpacTool\bin' }
    $ToolDirectory = [IO.Path]::GetFullPath($ToolDirectory)
    $readerPath = Join-Path $ToolDirectory 'TpacTool.Lib.dll'
    if (-not (Test-Path -LiteralPath $readerPath -PathType Leaf)) { throw "Optional TpacTool reader was not found: $readerPath. No metadata inventory was written." }

    try {
        $readerAssembly = [Reflection.Assembly]::LoadFrom($readerPath)
        $packageType = $readerAssembly.GetType('TpacTool.Lib.AssetPackage', $true)
        $constructor = $packageType.GetConstructor([Type[]]@([string], [bool], [bool]))
        if (-not $constructor) { throw 'TpacTool does not expose the expected metadata-only AssetPackage constructor.' }
        $arguments = [object[]]::new(3)
        $arguments[0] = $packagePath
        $arguments[1] = $true
        $arguments[2] = $false
        $package = $constructor.Invoke($arguments)
        if (-not $package.HeaderLoaded) { throw 'TpacTool did not read the package header.' }
        if ($package.Invalid) { throw 'TpacTool marked the package invalid.' }
        $items = @($package.Items)
        if ($items.Count -eq 0 -or $items.Count -ne $declaredCount) { throw "TpacTool found $($items.Count) metadata entries; the bounded v2 header declares $declaredCount." }

        $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
        $assets = @(
            foreach ($item in $items) {
                if ($item.Invalid) { throw "TpacTool marked metadata entry '$($item.Name)' invalid." }
                if ([string]::IsNullOrWhiteSpace([string]$item.Name)) { throw 'TpacTool returned an entry with an empty asset name.' }
                $assetGuid = [Guid]$item.Guid
                if ($assetGuid -eq [Guid]::Empty) { throw "Asset '$($item.Name)' has an empty GUID." }
                if (-not $seen.Add($assetGuid.ToString('D'))) { throw "Duplicate asset GUID: $assetGuid" }
                [pscustomobject]@{
                    name = [string]$item.Name
                    guid = $assetGuid.ToString('D')
                    typeGuid = ([Guid]$item.Type).ToString('D')
                    version = [uint32]$item.Version
                }
            }
        ) | Sort-Object name, guid
    }
    catch {
        $message = $_.Exception.Message
        if ($_.Exception.InnerException) { $message = $_.Exception.InnerException.Message }
        throw "TPAC metadata remains unverified: $message"
    }

    [pscustomobject]@{
        schema = 'calradiaforge.tpac-inventory/v1'
        createdUtc = [DateTime]::UtcNow.ToString('o')
        reader = [pscustomobject]@{ name = 'TpacTool.Lib'; version = $readerAssembly.GetName().Version.ToString() }
        source = [pscustomobject]@{ path = $packagePath; lengthBytes = [long]$packageInfo.Length; sha256 = Get-TpacSha256 $packagePath }
        package = [pscustomobject]@{ formatVersion = [uint32]$formatVersion; guid = $packageGuid.ToString('D'); declaredAssetCount = [uint32]$declaredCount; tableOfContentsBytes = [uint64]$tocSize }
        assets = @($assets)
        scope = 'Read-only package header and asset metadata inventory. Texture payloads are not decoded; no import or package writing is performed.'
        limits = @('Reader compatibility with the selected Bannerlord TPAC version must be verified separately.', 'This report does not certify texture payload integrity, visual output, or game loading.')
    }
}

function Export-TpacInventory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$InputPackage,
        [Parameter(Mandatory = $true)][string]$OutputJson,
        [string]$ToolDirectory = '',
        [switch]$Force
    )

    $packagePath = [IO.Path]::GetFullPath($InputPackage)
    $outputPath = [IO.Path]::GetFullPath($OutputJson)
    if ([string]::Equals($packagePath, $outputPath, [StringComparison]::OrdinalIgnoreCase)) { throw 'Output JSON path must not point to the input TPAC.' }
    if ([IO.Path]::GetExtension($outputPath) -ine '.json') { throw 'Inventory output must use the .json extension.' }
    $outputDirectory = Split-Path -Parent $outputPath
    if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) { [IO.Directory]::CreateDirectory($outputDirectory) | Out-Null }

    $inventory = Read-TpacInventory -InputPackage $packagePath -ToolDirectory $ToolDirectory
    if ((Test-Path -LiteralPath $outputPath -PathType Leaf) -and -not $Force) { throw "Output already exists; pass -Force to replace it with a backup: $outputPath" }
    $tempPath = $outputPath + '.tmp.' + [Guid]::NewGuid().ToString('N')
    $backupPath = $null
    try {
        $json = ConvertTo-Json -InputObject $inventory -Depth 8
        [IO.File]::WriteAllText($tempPath, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
        if (Test-Path -LiteralPath $outputPath -PathType Leaf) {
            $backupPath = $outputPath + '.bak.' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')
            [IO.File]::Replace($tempPath, $outputPath, $backupPath)
        }
        else { [IO.File]::Move($tempPath, $outputPath) }
    }
    catch {
        if (Test-Path -LiteralPath $tempPath -PathType Leaf) { Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue }
        throw
    }

    [pscustomobject]@{ Inventory = $inventory; OutputPath = $outputPath; BackupPath = $backupPath }
}

function Get-RequiredInventoryProperty($Object, [string]$PropertyName, [string]$ReportPath) {
    if ($null -eq $Object) { throw "Inventory report field '$PropertyName' is null: $ReportPath" }
    $property = $Object.PSObject.Properties[$PropertyName]
    if ($null -eq $property) { throw "Inventory report is missing required field '$PropertyName': $ReportPath" }
    if ($null -eq $property.Value) { throw "Inventory report field '$PropertyName' is null: $ReportPath" }
    return $property.Value
}

function ConvertTo-InventoryUInt32($Value, [string]$FieldName, [string]$ReportPath) {
    [uint32]$parsed = 0
    if (-not [uint32]::TryParse([string]$Value, [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
        throw "Inventory report has an invalid unsigned 32-bit field '$FieldName': $ReportPath"
    }
    return $parsed
}

function ConvertTo-InventoryUInt64($Value, [string]$FieldName, [string]$ReportPath) {
    [uint64]$parsed = 0
    if (-not [uint64]::TryParse([string]$Value, [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
        throw "Inventory report has an invalid unsigned 64-bit field '$FieldName': $ReportPath"
    }
    return $parsed
}

function Read-ValidatedTpacInventoryReport([string]$Path) {
    $reportPath = [IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) { throw "TPAC inventory report was not found: $reportPath" }
    $file = Get-Item -LiteralPath $reportPath
    if ($file.Length -gt 33554432) { throw "Inventory report exceeds the bounded 32 MiB JSON limit: $reportPath" }

    try { $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json -ErrorAction Stop }
    catch { throw "Inventory report is not valid JSON: $reportPath" }
    $schema = Get-RequiredInventoryProperty $report 'schema' $reportPath
    if ([string]$schema -ne 'calradiaforge.tpac-inventory/v1') { throw "Unsupported TPAC inventory schema in $reportPath" }
    $captureText = [string](Get-RequiredInventoryProperty $report 'createdUtc' $reportPath)
    $capturePattern = '\A\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})\z'
    $capturedAt = [DateTimeOffset]::MinValue
    if ($captureText.EndsWith('-00:00', [StringComparison]::OrdinalIgnoreCase) -or
        $captureText -notmatch $capturePattern -or
        -not [DateTimeOffset]::TryParse($captureText, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AdjustToUniversal, [ref]$capturedAt)) {
        throw "Inventory report has no valid ISO capture timestamp with an explicit time zone: $reportPath"
    }
    $reader = Get-RequiredInventoryProperty $report 'reader' $reportPath
    $readerName = [string](Get-RequiredInventoryProperty $reader 'name' $reportPath)
    $readerVersion = [string](Get-RequiredInventoryProperty $reader 'version' $reportPath)
    $source = Get-RequiredInventoryProperty $report 'source' $reportPath
    $sourceHash = [string](Get-RequiredInventoryProperty $source 'sha256' $reportPath)
    $sourcePath = [string](Get-RequiredInventoryProperty $source 'path' $reportPath)
    $sourceLengthText = [string](Get-RequiredInventoryProperty $source 'lengthBytes' $reportPath)
    $package = Get-RequiredInventoryProperty $report 'package' $reportPath
    $formatVersion = ConvertTo-InventoryUInt32 (Get-RequiredInventoryProperty $package 'formatVersion' $reportPath) 'package.formatVersion' $reportPath
    $packageGuidText = [string](Get-RequiredInventoryProperty $package 'guid' $reportPath)
    $declaredCount = ConvertTo-InventoryUInt32 (Get-RequiredInventoryProperty $package 'declaredAssetCount' $reportPath) 'package.declaredAssetCount' $reportPath
    $tocSize = ConvertTo-InventoryUInt64 (Get-RequiredInventoryProperty $package 'tableOfContentsBytes' $reportPath) 'package.tableOfContentsBytes' $reportPath
    if ($sourceHash -notmatch '\A[A-Fa-f0-9]{64}\z') { throw "Inventory report has no valid source SHA-256: $reportPath" }
    if ([string]::IsNullOrWhiteSpace($sourcePath)) { throw "Inventory report has no source path: $reportPath" }
    [long]$sourceLength = 0
    if (-not [long]::TryParse($sourceLengthText, [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$sourceLength) -or $sourceLength -le 36) {
        throw "Inventory report has an invalid source length: $reportPath"
    }
    if ($sourceLength -gt 134217728) { throw "Inventory report source exceeds the supported 128 MiB limit: $reportPath" }
    if ([string]::IsNullOrWhiteSpace($readerName) -or [string]::IsNullOrWhiteSpace($readerVersion)) {
        throw "Inventory report does not identify its metadata reader: $reportPath"
    }
    if ($formatVersion -ne 2) { throw "Inventory report is not for the supported TPAC v2 format: $reportPath" }
    if ($tocSize -eq 0 -or $tocSize -gt [uint64]($sourceLength - 36)) {
        throw "Inventory report has a table-of-contents range outside the $sourceLength-byte source package: $reportPath"
    }

    $packageGuid = [Guid]::Empty
    if (-not [Guid]::TryParse($packageGuidText, [ref]$packageGuid) -or $packageGuid -eq [Guid]::Empty) {
        throw "Inventory report has an invalid package GUID: $reportPath"
    }

    $assetsValue = Get-RequiredInventoryProperty $report 'assets' $reportPath
    if ($assetsValue -isnot [System.Array]) { throw "Inventory report assets field is not a JSON array: $reportPath" }
    $assets = @($assetsValue)
    if ($assets.Count -eq 0 -or $assets.Count -gt 100000) { throw "Inventory report asset count is outside the supported range 1-100000: $reportPath" }
    if ($declaredCount -ne $assets.Count) { throw "Inventory report asset count does not match its package header: $reportPath" }
    $byGuid = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($asset in $assets) {
        $assetName = [string](Get-RequiredInventoryProperty $asset 'name' $reportPath)
        $assetGuidText = [string](Get-RequiredInventoryProperty $asset 'guid' $reportPath)
        $typeGuidText = [string](Get-RequiredInventoryProperty $asset 'typeGuid' $reportPath)
        $assetVersionText = [string](Get-RequiredInventoryProperty $asset 'version' $reportPath)
        $assetGuid = [Guid]::Empty
        $typeGuid = [Guid]::Empty
        [uint32]$assetVersion = 0
        if (-not [Guid]::TryParse($assetGuidText, [ref]$assetGuid) -or $assetGuid -eq [Guid]::Empty) {
            throw "Inventory report contains an invalid asset GUID: $reportPath"
        }
        if (-not [Guid]::TryParse($typeGuidText, [ref]$typeGuid) -or $typeGuid -eq [Guid]::Empty) {
            throw "Inventory report contains an invalid asset type GUID: $reportPath"
        }
        if (-not [uint32]::TryParse($assetVersionText, [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$assetVersion)) {
            throw "Inventory report contains an invalid asset version: $reportPath"
        }
        if ([string]::IsNullOrWhiteSpace($assetName)) { throw "Inventory report contains an empty asset name: $reportPath" }
        $canonicalGuid = $assetGuid.ToString('D')
        if ($byGuid.ContainsKey($canonicalGuid)) { throw "Inventory report contains duplicate asset GUID '$canonicalGuid': $reportPath" }
        $byGuid.Add($canonicalGuid, [pscustomobject]@{
            name = $assetName
            guid = $canonicalGuid
            typeGuid = $typeGuid.ToString('D')
            version = $assetVersion
        })
    }

    [pscustomobject]@{
        path = $reportPath
        capturedUtc = $capturedAt.UtcDateTime.ToString('o')
        reader = [pscustomobject]@{ name = $readerName; version = $readerVersion }
        source = [pscustomobject]@{
            path = $sourcePath
            lengthBytes = $sourceLength
            sha256 = $sourceHash.ToUpperInvariant()
        }
        package = [pscustomobject]@{
            formatVersion = $formatVersion
            guid = $packageGuid.ToString('D')
            declaredAssetCount = $declaredCount
            tableOfContentsBytes = $tocSize
        }
        assets = $byGuid
    }
}

function Compare-TpacInventory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$BeforeReport,
        [Parameter(Mandatory = $true)][string]$AfterReport,
        [switch]$IncludeUnchanged
    )

    $before = Read-ValidatedTpacInventoryReport $BeforeReport
    $after = Read-ValidatedTpacInventoryReport $AfterReport
    $items = New-Object 'System.Collections.Generic.List[object]'
    $added = 0
    $removed = 0
    $changed = 0
    $unchanged = 0

    foreach ($guid in $before.assets.Keys) {
        $oldAsset = $before.assets[$guid]
        if (-not $after.assets.ContainsKey($guid)) {
            $removed++
            $items.Add([pscustomobject]@{ status = 'removed'; guid = $guid; name = $oldAsset.name; before = $oldAsset; after = $null; changedFields = @() })
            continue
        }

        $newAsset = $after.assets[$guid]
        $changedFields = New-Object 'System.Collections.Generic.List[string]'
        if (-not [string]::Equals($oldAsset.name, $newAsset.name, [StringComparison]::Ordinal)) { $changedFields.Add('name') }
        if (-not [string]::Equals($oldAsset.typeGuid, $newAsset.typeGuid, [StringComparison]::OrdinalIgnoreCase)) { $changedFields.Add('typeGuid') }
        if ($oldAsset.version -ne $newAsset.version) { $changedFields.Add('version') }

        if ($changedFields.Count -gt 0) {
            $changed++
            $items.Add([pscustomobject]@{ status = 'changed'; guid = $guid; name = $newAsset.name; before = $oldAsset; after = $newAsset; changedFields = @($changedFields) })
        }
        else {
            $unchanged++
            if ($IncludeUnchanged) {
                $items.Add([pscustomobject]@{ status = 'unchanged'; guid = $guid; name = $newAsset.name; before = $oldAsset; after = $newAsset; changedFields = @() })
            }
        }
    }

    foreach ($guid in $after.assets.Keys) {
        if (-not $before.assets.ContainsKey($guid)) {
            $newAsset = $after.assets[$guid]
            $added++
            $items.Add([pscustomobject]@{ status = 'added'; guid = $guid; name = $newAsset.name; before = $null; after = $newAsset; changedFields = @() })
        }
    }

    $sortedItems = @($items | Sort-Object status, name, guid)
    [pscustomobject]@{
        schema = 'calradiaforge.tpac-inventory-diff/v1'
        createdUtc = [DateTime]::UtcNow.ToString('o')
        before = $before
        after = $after
        summary = [pscustomobject]@{
            added = $added
            removed = $removed
            changed = $changed
            unchanged = $unchanged
            compared = $added + $removed + $changed + $unchanged
        }
        items = $sortedItems
        scope = 'Metadata comparison by asset GUID. Does not decode textures, validate payloads, prove compatibility, or modify either inventory or TPAC.'
        limits = @('Both input reports must use calradiaforge.tpac-inventory/v1 and contain TpacTool-accepted TPAC v2 metadata.', 'A changed metadata entry does not prove that the corresponding runtime asset behaves differently.')
    }
}

Export-ModuleMember -Function Read-TpacInventory, Export-TpacInventory, Compare-TpacInventory
