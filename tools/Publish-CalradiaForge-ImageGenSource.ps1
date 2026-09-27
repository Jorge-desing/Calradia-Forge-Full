[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$StageRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$expectedRoot = 'C:\Users\Alex\Documents\Mod Desarrolladores'
if (-not $repositoryRoot.Equals($expectedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Unexpected repository root '$repositoryRoot'."
}

$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([char[]]@('\', '/'))
$resolvedStageRoot = [IO.Path]::GetFullPath($StageRoot).TrimEnd([char[]]@('\', '/'))
if (-not [IO.Path]::GetDirectoryName($resolvedStageRoot).TrimEnd([char[]]@('\', '/')).Equals(
        $tempRoot, [StringComparison]::OrdinalIgnoreCase) -or
    [IO.Path]::GetFileName($resolvedStageRoot) -notmatch '^CalradiaForge-ImageGenPrefabStage-[0-9a-f]{32}$') {
    throw "Refusing stage outside the validated temp-root naming scheme: '$resolvedStageRoot'."
}

$moduleRoot = Join-Path $repositoryRoot 'modules\CalradiaForge'
$prefabPath = Join-Path $moduleRoot 'GUI\Prefabs\CalradiaForge.xml'
$spriteRoot = Join-Path $moduleRoot 'GUI\SpriteParts\ui_calradiaforge'
$stageModule = Join-Path $resolvedStageRoot 'modules\CalradiaForge'
$stagePrefab = Join-Path $stageModule 'GUI\Prefabs\CalradiaForge.xml'
$stageSpriteData = Join-Path $stageModule 'GUI\CalradiaForgeSpriteData.xml'
$stageSpriteRoot = Join-Path $stageModule 'GUI\SpriteParts\ui_calradiaforge'
$preparedRoot = Join-Path $repositoryRoot 'assets\gauntlet-imagegen\prepared'
$newSpriteNames = @(
    'forge_war_table_cloth_v2.png',
    'forge_rail_cartographic_field_v1.png',
    'forge_heraldic_overlay.png',
    'forge_patina_brass.png',
    'forge_pine_felt.png',
    'forge_header_summary_v1.png',
    'forge_header_modules_v1.png',
    'forge_header_logs_v1.png',
    'forge_header_inspector_v1.png',
    'forge_header_tests_v1.png',
    'forge_header_metrics_v1.png',
    'forge_header_framework_v1.png',
    'forge_header_extensions_v1.png'
)
$retiredSpriteNames = @(
    'forge_war_table_cloth.png',
    'forge_pine_grain.png',
    'forge_table_grain.png',
    'forge_map_contours.png',
    'forge_brass_rule.png',
    'forge_heraldic_corner.png',
    'forge_woven_border.png',
    'forge_rosette_mark.png',
    'forge_stitch_rule.png',
    'forge_header_filigree.png'
)
$retiredHelperNames = @('gauntlet_texture_rosette.py', 'gauntlet_texture_stitch.py')

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

function Assert-File([string]$Path, [string]$Description) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Description is missing: '$Path'."
    }
    $item = Get-Item -LiteralPath $Path -Force
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Description cannot be a reparse point: '$Path'."
    }
}

$temporaryPublishPaths = [System.Collections.Generic.List[string]]::new()

function Copy-ToArchive([string]$Source, [string]$Name, [string]$ArchiveDirectory,
    [System.Collections.Generic.List[string]]$ManifestLines) {
    Assert-File $Source 'Archive source'
    $destination = Join-Path $ArchiveDirectory $Name
    if (Test-Path -LiteralPath $destination) {
        throw "Archive file already exists: '$destination'."
    }
    $sourceHash = Get-Sha256Hex $Source
    [IO.File]::Copy($Source, $destination, $false)
    if ((Get-Sha256Hex $destination) -ne $sourceHash) {
        throw "Archive SHA-256 verification failed: '$destination'."
    }
    $ManifestLines.Add("$sourceHash  $Name")
    return $destination
}

function Publish-StageFile([string]$Source, [string]$Destination, [string]$ArchiveDirectory,
    [string]$ArchiveName, [System.Collections.Generic.List[string]]$ManifestLines,
    [System.Collections.Generic.List[object]]$Changes) {
    Assert-File $Source 'Staged file'
    $sourceHash = Get-Sha256Hex $Source
    if (Test-Path -LiteralPath $Destination -PathType Container) {
        throw "Refusing to replace a directory: '$Destination'."
    }

    $oldHash = $null
    if (Test-Path -LiteralPath $Destination -PathType Leaf) {
        Assert-File $Destination 'Existing destination'
        $oldHash = Get-Sha256Hex $Destination
        if ($oldHash -eq $sourceHash) {
            $ManifestLines.Add("$sourceHash  unchanged/$ArchiveName")
            return
        }
        [void](Copy-ToArchive $Destination $ArchiveName $ArchiveDirectory $ManifestLines)
    }

    $temporaryPath = $Destination + '.stage.' + [guid]::NewGuid().ToString('N') + '.tmp'
    $temporaryPublishPaths.Add($temporaryPath)
    [IO.File]::Copy($Source, $temporaryPath, $false)
    if ((Get-Sha256Hex $temporaryPath) -ne $sourceHash) {
        throw "Temporary publish SHA-256 verification failed: '$Destination'."
    }
    if ($oldHash) {
        if ((Get-Sha256Hex $Destination) -ne $oldHash) {
            throw "Destination changed while staging; refusing replacement: '$Destination'."
        }
        $Changes.Add([pscustomobject]@{
            Destination = $Destination
            OldHash = $oldHash
            SourceHash = $sourceHash
            ArchiveName = $ArchiveName
        })
        Move-Item -LiteralPath $temporaryPath -Destination $Destination -Force
    }
    else {
        $Changes.Add([pscustomobject]@{
            Destination = $Destination
            OldHash = $oldHash
            SourceHash = $sourceHash
            ArchiveName = $ArchiveName
        })
        [IO.File]::Move($temporaryPath, $Destination)
    }
    [void]$temporaryPublishPaths.Remove($temporaryPath)
    if ((Get-Sha256Hex $Destination) -ne $sourceHash) {
        throw "Published SHA-256 verification failed: '$Destination'."
    }
    $ManifestLines.Add("$sourceHash  published/$ArchiveName")
}

if (-not (Test-Path -LiteralPath $stageModule -PathType Container)) {
    throw "Staged CalradiaForge module is missing: '$stageModule'."
}
Assert-File $stagePrefab 'Staged prefab'
$prefabXml = New-Object System.Xml.XmlDocument
$prefabXml.Load($stagePrefab)
foreach ($retiredSprite in @(
    'forge_pine_grain', 'forge_table_grain', 'forge_map_contours', 'forge_brass_rule',
    'forge_heraldic_corner', 'forge_woven_border', 'forge_rosette_mark', 'forge_stitch_rule',
    'forge_header_filigree'
)) {
    if ($null -ne $prefabXml.SelectSingleNode("//*[@Sprite='$retiredSprite']")) {
        throw "Staged prefab retains a retired geometric sprite '$retiredSprite'."
    }
}

# SpriteData is generator-owned and may still describe the previous atlas until
# Build-CalradiaForge-UiAssets.bat runs; all authored UI markup must already be clean.
$retiredClothName = 'forge_war_table_cloth'
$stagedUiRoot = Join-Path $stageModule 'GUI'
foreach ($uiMarkup in Get-ChildItem -LiteralPath $stagedUiRoot -Recurse -File | Where-Object {
        $_.Extension -in @('.xml', '.xaml') -and $_.FullName -ne $stageSpriteData
    }) {
    $document = New-Object System.Xml.XmlDocument
    $document.Load($uiMarkup.FullName)
    foreach ($node in $document.SelectNodes('//*[@Sprite or @SpriteName]')) {
        if ($node.GetAttribute('Sprite') -eq $retiredClothName -or
            $node.GetAttribute('SpriteName') -eq $retiredClothName) {
            throw "Refusing to retire '$retiredClothName': staged UI markup still references it in '$($uiMarkup.FullName)'."
        }
    }
}
foreach ($spriteName in $newSpriteNames) {
    $stageSprite = Join-Path $stageSpriteRoot $spriteName
    $preparedSprite = Join-Path $preparedRoot $spriteName
    Assert-File $stageSprite 'Staged ImageGen sprite'
    Assert-File $preparedSprite 'Prepared ImageGen sprite'
    if ((Get-Sha256Hex $stageSprite) -ne (Get-Sha256Hex $preparedSprite)) {
        throw "Staged sprite differs from deterministic prepared output: '$spriteName'."
    }
}

$retiredRoot = Join-Path $repositoryRoot 'artifacts\retired-ui-sprites'
$archiveDirectory = Join-Path $retiredRoot ('imagegen-replacement-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
$resolvedArchiveDirectory = [IO.Path]::GetFullPath($archiveDirectory)
$resolvedRetiredRoot = [IO.Path]::GetFullPath($retiredRoot).TrimEnd([char[]]@('\', '/'))
if (-not $resolvedArchiveDirectory.StartsWith(
        $resolvedRetiredRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Archive destination escaped the retired-sprites directory: '$resolvedArchiveDirectory'."
}
New-Item -ItemType Directory -Path $resolvedArchiveDirectory -Force | Out-Null
$manifestLines = [System.Collections.Generic.List[string]]::new()
$changes = [System.Collections.Generic.List[object]]::new()
$removedLegacy = [System.Collections.Generic.List[object]]::new()

try {
    foreach ($staleTemp in Get-ChildItem -LiteralPath (Split-Path -Parent $prefabPath) -Filter 'CalradiaForge.xml.stage.*.tmp' -File) {
        $staleHash = Get-Sha256Hex $staleTemp.FullName
        if ($staleHash -ne (Get-Sha256Hex $stagePrefab)) {
            throw "Found an unknown staged prefab temp; leaving it intact: '$($staleTemp.FullName)'."
        }
        $staleArchiveName = 'recovered-' + $staleTemp.Name
        $staleBackup = Copy-ToArchive $staleTemp.FullName $staleArchiveName $resolvedArchiveDirectory $manifestLines
        if ((Get-Sha256Hex $staleBackup) -ne $staleHash) {
            throw "Recovered prefab temp SHA-256 verification failed: '$($staleTemp.FullName)'."
        }
        Remove-Item -LiteralPath $staleTemp.FullName -Force
        $removedLegacy.Add([pscustomobject]@{ Source = $staleTemp.FullName; Backup = $staleBackup; Hash = $staleHash })
    }

    Publish-StageFile $stagePrefab $prefabPath $resolvedArchiveDirectory 'previous-CalradiaForge.xml' $manifestLines $changes
    foreach ($spriteName in $newSpriteNames) {
        Publish-StageFile (Join-Path $stageSpriteRoot $spriteName) (Join-Path $spriteRoot $spriteName) `
            $resolvedArchiveDirectory ('previous-' + $spriteName) $manifestLines $changes
    }

    foreach ($spriteName in $retiredSpriteNames) {
        $legacyPath = Join-Path $spriteRoot $spriteName
        if (-not (Test-Path -LiteralPath $legacyPath -PathType Leaf)) { continue }
        $archiveName = 'retired-' + $spriteName
        $backupPath = Copy-ToArchive $legacyPath $archiveName $resolvedArchiveDirectory $manifestLines
        $legacyHash = Get-Sha256Hex $legacyPath
        if ((Get-Sha256Hex $backupPath) -ne $legacyHash) {
            throw "Legacy sprite backup changed before retirement: '$legacyPath'."
        }
        Remove-Item -LiteralPath $legacyPath -Force
        $removedLegacy.Add([pscustomobject]@{ Source = $legacyPath; Backup = $backupPath; Hash = $legacyHash })
    }

    foreach ($helperName in $retiredHelperNames) {
        $helperPath = Join-Path $repositoryRoot ('tools\' + $helperName)
        if (-not (Test-Path -LiteralPath $helperPath -PathType Leaf)) { continue }
        $backupPath = Copy-ToArchive $helperPath ('retired-' + $helperName) $resolvedArchiveDirectory $manifestLines
        $helperHash = Get-Sha256Hex $helperPath
        if ((Get-Sha256Hex $backupPath) -ne $helperHash) {
            throw "Legacy helper backup changed before retirement: '$helperPath'."
        }
        Remove-Item -LiteralPath $helperPath -Force
        $removedLegacy.Add([pscustomobject]@{ Source = $helperPath; Backup = $backupPath; Hash = $helperHash })
    }

    [IO.File]::WriteAllLines((Join-Path $resolvedArchiveDirectory 'SHA256SUMS.txt'), $manifestLines, [Text.Encoding]::UTF8)
    Write-Host "Published the new prefab and ImageGen sprites. Verified previous UI assets are archived in '$resolvedArchiveDirectory'."
    foreach ($line in $manifestLines) { Write-Host $line }
}
catch {
    for ($index = $removedLegacy.Count - 1; $index -ge 0; $index--) {
        $item = $removedLegacy[$index]
        if (-not (Test-Path -LiteralPath $item.Source -PathType Leaf)) {
            [IO.File]::Copy($item.Backup, $item.Source, $false)
            if ((Get-Sha256Hex $item.Source) -ne $item.Hash) {
                Write-Warning "Could not verify restored legacy asset '$($item.Source)'; backup retained at '$($item.Backup)'."
            }
        }
    }
    for ($index = $changes.Count - 1; $index -ge 0; $index--) {
        $change = $changes[$index]
        if ($change.OldHash) {
            $backup = Join-Path $resolvedArchiveDirectory $change.ArchiveName
            if (Test-Path -LiteralPath $backup -PathType Leaf) {
                [IO.File]::Copy($backup, $change.Destination, $true)
                if ((Get-Sha256Hex $change.Destination) -ne $change.OldHash) {
                    Write-Warning "Could not verify restored destination '$($change.Destination)'."
                }
            }
        }
        elseif (Test-Path -LiteralPath $change.Destination -PathType Leaf) {
            Remove-Item -LiteralPath $change.Destination -Force
        }
    }
    throw
}
finally {
    foreach ($temporaryPath in $temporaryPublishPaths) {
        if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
            Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
        }
    }
}
