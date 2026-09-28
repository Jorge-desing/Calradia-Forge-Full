[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$Prepare,
    [string]$WarTableClothV2Master,
    [string]$RailCartographicFieldV1Master,
    [string]$HeraldicFieldJournalOverlayMaster,
    [string]$AgedBrassPatinaMaster,
    [string]$PineFeltMaster,
    [string]$HeaderSummaryV1Master,
    [string]$HeaderModulesV1Master,
    [string]$HeaderLogsV1Master,
    [string]$HeaderInspectorV1Master,
    [string]$HeaderTestsV1Master,
    [string]$HeaderMetricsV1Master,
    [string]$HeaderFrameworkV1Master,
    [string]$HeaderExtensionsV1Master,
    [string]$HeraldicHeaderV2Master,
    [string]$HeraldicRailV2Master
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($Check -eq $Prepare) {
    throw 'Choose exactly one mode: -Check or -Prepare.'
}

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$masterRoot = Join-Path $repositoryRoot 'assets\gauntlet-imagegen'
$preparedRoot = Join-Path $masterRoot 'prepared'
$archiveRoot = Join-Path $masterRoot 'archive\2026-09-25'
$roundArchiveRoot = Join-Path $masterRoot 'archive\2026-09-28'
$retiredPreparedTextures = @('forge_dark_wood.png', 'forge_inkwash.png', 'forge_war_table_cloth.png')
$retiredPreparedHeaderTextures = @(
    'forge_header_summary_v1.png', 'forge_header_modules_v1.png',
    'forge_header_logs_v1.png', 'forge_header_inspector_v1.png',
    'forge_header_tests_v1.png', 'forge_header_metrics_v1.png',
    'forge_header_framework_v1.png', 'forge_header_extensions_v1.png'
)

function Resolve-InputPath([string]$Value, [string]$DefaultName) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return [IO.Path]::GetFullPath((Join-Path $masterRoot $DefaultName))
    }
    if ([IO.Path]::IsPathRooted($Value)) {
        return [IO.Path]::GetFullPath($Value)
    }
    return [IO.Path]::GetFullPath((Join-Path $repositoryRoot $Value))
}

$textures = @(
    [pscustomobject]@{
        Name = 'forge_war_table_cloth_v2.png'; Master = Resolve-InputPath $WarTableClothV2Master 'forge_war_table_cloth_v3_master.png'
        Width = 1024; Height = 128; MaxAlpha = 24; PreserveArtworkBounds = $false; RequireTransparency = $false
    },
    [pscustomobject]@{
        Name = 'forge_rail_cartographic_field_v1.png'; Master = Resolve-InputPath $RailCartographicFieldV1Master 'forge_rail_cartographic_field_v1.png'
        Width = 256; Height = 256; MaxAlpha = 36; PreserveArtworkBounds = $false; RequireTransparency = $false
    },
    [pscustomobject]@{
        Name = 'forge_heraldic_overlay.png'; Master = Resolve-InputPath $HeraldicFieldJournalOverlayMaster 'heraldic_field_journal_overlay_v1.png'
        Width = 256; Height = 48; MaxAlpha = 112; PreserveArtworkBounds = $true; RequireTransparency = $true
    },
    [pscustomobject]@{
        Name = 'forge_patina_brass.png'; Master = Resolve-InputPath $AgedBrassPatinaMaster 'aged_brass_patina.png'
        Width = 128; Height = 16; MaxAlpha = 88; PreserveArtworkBounds = $false; RequireTransparency = $false
    },
    [pscustomobject]@{
        Name = 'forge_pine_felt.png'; Master = Resolve-InputPath $PineFeltMaster 'pine_felt.png'
        Width = 128; Height = 32; MaxAlpha = 40; PreserveArtworkBounds = $false; RequireTransparency = $false
    },
    [pscustomobject]@{
        Name = 'forge_header_summary_v1.png'; Master = Resolve-InputPath $HeaderSummaryV1Master 'forge_header_summary_v1.png'
        Width = 128; Height = 64; MaxAlpha = 112; PreserveArtworkBounds = $true; RequireTransparency = $true
    },
    [pscustomobject]@{
        Name = 'forge_header_modules_v1.png'; Master = Resolve-InputPath $HeaderModulesV1Master 'forge_header_modules_v1.png'
        Width = 128; Height = 64; MaxAlpha = 112; PreserveArtworkBounds = $true; RequireTransparency = $true
    },
    [pscustomobject]@{
        Name = 'forge_header_logs_v1.png'; Master = Resolve-InputPath $HeaderLogsV1Master 'forge_header_logs_v1.png'
        Width = 128; Height = 64; MaxAlpha = 112; PreserveArtworkBounds = $true; RequireTransparency = $true
    },
    [pscustomobject]@{
        Name = 'forge_header_inspector_v1.png'; Master = Resolve-InputPath $HeaderInspectorV1Master 'forge_header_inspector_v1.png'
        Width = 128; Height = 64; MaxAlpha = 112; PreserveArtworkBounds = $true; RequireTransparency = $true
    },
    [pscustomobject]@{
        Name = 'forge_header_tests_v1.png'; Master = Resolve-InputPath $HeaderTestsV1Master 'forge_header_tests_v1.png'
        Width = 128; Height = 64; MaxAlpha = 112; PreserveArtworkBounds = $true; RequireTransparency = $true
    },
    [pscustomobject]@{
        Name = 'forge_header_metrics_v1.png'; Master = Resolve-InputPath $HeaderMetricsV1Master 'forge_header_metrics_v1.png'
        Width = 128; Height = 64; MaxAlpha = 112; PreserveArtworkBounds = $true; RequireTransparency = $true
    },
    [pscustomobject]@{
        Name = 'forge_header_framework_v1.png'; Master = Resolve-InputPath $HeaderFrameworkV1Master 'forge_header_framework_v1.png'
        Width = 128; Height = 64; MaxAlpha = 112; PreserveArtworkBounds = $true; RequireTransparency = $true
    },
    [pscustomobject]@{
        Name = 'forge_header_extensions_v1.png'; Master = Resolve-InputPath $HeaderExtensionsV1Master 'forge_header_extensions_v1.png'
        Width = 128; Height = 64; MaxAlpha = 112; PreserveArtworkBounds = $true; RequireTransparency = $true
    },
    [pscustomobject]@{
        Name = 'forge_heraldic_header_v2.png'; Master = Resolve-InputPath $HeraldicHeaderV2Master 'forge_heraldic_header_v3_master.png'
        Width = 256; Height = 48; MaxAlpha = 88; PreserveArtworkBounds = $false; RequireTransparency = $true
    },
    [pscustomobject]@{
        Name = 'forge_heraldic_rail_v2.png'; Master = Resolve-InputPath $HeraldicRailV2Master 'forge_heraldic_rail_v4_master.png'
        Width = 128; Height = 256; MaxAlpha = 64; PreserveArtworkBounds = $false; RequireTransparency = $false
    }
)

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

function Get-ResolvedPath([string]$Path) {
    return [IO.Path]::GetFullPath($Path).TrimEnd([char[]]@('\', '/'))
}

function Remove-VerifiedArchivedPreparedTexture([string]$Name, [string]$ArchiveDirectory) {
    $sourcePath = Join-Path $preparedRoot $Name
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        return
    }

    foreach ($path in @($sourcePath, (Join-Path $ArchiveDirectory ('prepared-' + $Name)))) {
        $item = Get-Item -LiteralPath $path -Force -ErrorAction Stop
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Refusing to retire '$Name' through a reparse point: '$path'."
        }
    }
    $archivePath = Join-Path $ArchiveDirectory ('prepared-' + $Name)
    $manifestPath = Join-Path $ArchiveDirectory 'SHA256SUMS.txt'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Refusing to retire '$Name' without its archive integrity manifest."
    }

    $archiveHash = Get-Sha256Hex $archivePath
    $sourceHash = Get-Sha256Hex $sourcePath
    $manifestPattern = '^([0-9A-F]{64})  ' + [regex]::Escape('prepared-' + $Name) +
        '  \[source=assets\\gauntlet-imagegen\\prepared\\' + [regex]::Escape($Name) + '\]$'
    $manifestHashes = @(
        foreach ($line in [IO.File]::ReadAllLines($manifestPath)) {
            if ($line -match $manifestPattern) { $Matches[1] }
        }
    )
    if ($manifestHashes.Count -ne 1 -or $manifestHashes[0] -ne $archiveHash -or
        $sourceHash -ne $archiveHash) {
        throw "Refusing to retire '$Name': prepared source, archived copy and SHA-256 manifest do not match exactly."
    }

    Remove-Item -LiteralPath $sourcePath -Force
    Write-Host "Retired prepared texture after archive verification: $Name ($archiveHash)"
}

function Assert-PathIsWithin([string]$Path, [string]$Parent, [string]$Description) {
    $resolvedPath = Get-ResolvedPath $Path
    $resolvedParent = Get-ResolvedPath $Parent
    $prefix = $resolvedParent + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedPath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing $Description outside the expected directory: '$resolvedPath'"
    }
}

function Assert-Masters([object[]]$TextureSpecs) {
    foreach ($texture in $TextureSpecs) {
        if (-not (Test-Path -LiteralPath $texture.Master -PathType Leaf)) {
            throw "Required ImageGen master is missing: '$($texture.Master)'"
        }
        $item = Get-Item -LiteralPath $texture.Master -Force
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "ImageGen master must not be a reparse point: '$($texture.Master)'"
        }
        if ($item.Length -le 0 -or $item.Length -gt 64MB) {
            throw "ImageGen master size must be between 1 byte and 64 MiB: '$($texture.Master)'"
        }
        $stream = [IO.File]::OpenRead($texture.Master)
        try {
            $signature = New-Object byte[] 8
            if ($stream.Read($signature, 0, $signature.Length) -ne 8 -or
                [BitConverter]::ToString($signature) -ne '89-50-4E-47-0D-0A-1A-0A') {
                throw "ImageGen master is not a PNG: '$($texture.Master)'"
            }
        }
        finally {
            $stream.Dispose()
        }
    }
}

if (-not ('CalradiaForge.ImageGenTextureProcessor' -as [type])) {
    Add-Type -AssemblyName System.Drawing
    $processorSource = @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace CalradiaForge
{
    public sealed class TextureReport
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int MinAlpha { get; set; }
        public int MaxAlpha { get; set; }
        public int VisiblePixels { get; set; }
        public int LeftEdgeVisiblePixels { get; set; }
        public int RightEdgeVisiblePixels { get; set; }
    }

    public static class ImageGenTextureProcessor
    {
        public static TextureReport Prepare(string sourcePath, string outputPath, int targetWidth, int targetHeight, int maxAlpha, bool preserveArtworkBounds)
        {
            if (targetWidth <= 0 || targetHeight <= 0 || maxAlpha < 1 || maxAlpha > 255)
                throw new ArgumentOutOfRangeException("Texture dimensions and alpha cap must be positive and valid.");

            Bitmap source = null;
            Bitmap output = null;
            try
            {
                source = new Bitmap(sourcePath);
                if (source.Width < 1 || source.Height < 1 || source.Width > 8192 || source.Height > 8192)
                    throw new InvalidOperationException("Master dimensions must be between 1 and 8192 pixels per axis.");

                output = new Bitmap(targetWidth, targetHeight, PixelFormat.Format32bppArgb);
                output.SetResolution(96.0f, 96.0f);

                double sourceAspect = (double)source.Width / source.Height;
                double targetAspect = (double)targetWidth / targetHeight;
                int cropWidth = source.Width;
                int cropHeight = source.Height;
                if (sourceAspect > targetAspect)
                {
                    cropWidth = Math.Max(1, (int)Math.Round(source.Height * targetAspect, MidpointRounding.AwayFromZero));
                }
                else if (sourceAspect < targetAspect)
                {
                    cropHeight = Math.Max(1, (int)Math.Round(source.Width / targetAspect, MidpointRounding.AwayFromZero));
                }
                int cropX = (source.Width - cropWidth) / 2;
                int cropY = (source.Height - cropHeight) / 2;
                Rectangle crop = new Rectangle(cropX, cropY, cropWidth, cropHeight);
                Rectangle destination = new Rectangle(0, 0, targetWidth, targetHeight);
                if (preserveArtworkBounds)
                {
                    crop = new Rectangle(0, 0, source.Width, source.Height);
                    if (sourceAspect <= targetAspect)
                    {
                        int fittedWidth = Math.Max(1, Math.Min(targetWidth,
                            (int)Math.Round(targetHeight * sourceAspect, MidpointRounding.AwayFromZero)));
                        destination = new Rectangle((targetWidth - fittedWidth) / 2, 0, fittedWidth, targetHeight);
                    }
                    else
                    {
                        int fittedHeight = Math.Max(1, Math.Min(targetHeight,
                            (int)Math.Round(targetWidth / sourceAspect, MidpointRounding.AwayFromZero)));
                        destination = new Rectangle(0, (targetHeight - fittedHeight) / 2, targetWidth, fittedHeight);
                    }
                }

                using (Graphics graphics = Graphics.FromImage(output))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.SmoothingMode = SmoothingMode.None;
                    graphics.Clear(Color.Transparent);
                    graphics.DrawImage(source, destination, crop, GraphicsUnit.Pixel);
                }

                int minAlpha = 255;
                int observedMaxAlpha = 0;
                int visiblePixels = 0;
                int leftEdgeVisiblePixels = 0;
                int rightEdgeVisiblePixels = 0;
                for (int y = 0; y < targetHeight; y++)
                {
                    for (int x = 0; x < targetWidth; x++)
                    {
                        Color pixel = output.GetPixel(x, y);
                        int alpha = (pixel.A * maxAlpha + 127) / 255;
                        output.SetPixel(x, y, Color.FromArgb(alpha, pixel.R, pixel.G, pixel.B));
                        if (alpha < minAlpha) minAlpha = alpha;
                        if (alpha > observedMaxAlpha) observedMaxAlpha = alpha;
                        if (alpha > 0)
                        {
                            visiblePixels++;
                            if (x < targetWidth / 4) leftEdgeVisiblePixels++;
                            if (x >= targetWidth * 3 / 4) rightEdgeVisiblePixels++;
                        }
                    }
                }
                output.Save(outputPath, ImageFormat.Png);
                return new TextureReport
                {
                    Width = targetWidth,
                    Height = targetHeight,
                    MinAlpha = minAlpha,
                    MaxAlpha = observedMaxAlpha,
                    VisiblePixels = visiblePixels,
                    LeftEdgeVisiblePixels = leftEdgeVisiblePixels,
                    RightEdgeVisiblePixels = rightEdgeVisiblePixels
                };
            }
            finally
            {
                if (output != null) output.Dispose();
                if (source != null) source.Dispose();
            }
        }
    }
}
'@
    Add-Type -TypeDefinition $processorSource -ReferencedAssemblies 'System.Drawing'
}

function New-OutputSet([object[]]$TextureSpecs, [string]$OutputDirectory) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    $reports = @()
    foreach ($texture in $TextureSpecs) {
        $outputPath = Join-Path $OutputDirectory $texture.Name
        $facts = [CalradiaForge.ImageGenTextureProcessor]::Prepare(
            $texture.Master, $outputPath, $texture.Width, $texture.Height, $texture.MaxAlpha,
            $texture.PreserveArtworkBounds)
        if ($facts.Width -ne $texture.Width -or $facts.Height -ne $texture.Height) {
            throw "Generated '$($texture.Name)' has unexpected dimensions $($facts.Width)x$($facts.Height)."
        }
        if ($facts.VisiblePixels -le 0 -or $facts.MaxAlpha -gt $texture.MaxAlpha) {
            throw "Generated '$($texture.Name)' failed alpha/visibility validation."
        }
        if ($texture.RequireTransparency -and $facts.MinAlpha -ne 0) {
            throw "Generated '$($texture.Name)' must retain transparent pixels."
        }
        if (($texture.PreserveArtworkBounds -or $texture.Name -eq 'forge_heraldic_header_v2.png') -and
            ($facts.LeftEdgeVisiblePixels -le 0 -or $facts.RightEdgeVisiblePixels -le 0)) {
            throw "Generated '$($texture.Name)' lost visible art at one or both horizontal ends."
        }
        $reports += [pscustomobject]@{
            Name = $texture.Name
            Path = $outputPath
            Width = $facts.Width
            Height = $facts.Height
            MinAlpha = $facts.MinAlpha
            MaxAlpha = $facts.MaxAlpha
            VisiblePixels = $facts.VisiblePixels
            LeftEdgeVisiblePixels = $facts.LeftEdgeVisiblePixels
            RightEdgeVisiblePixels = $facts.RightEdgeVisiblePixels
            Sha256 = Get-Sha256Hex $outputPath
        }
    }
    return ,$reports
}

function Write-Reports([object[]]$Reports) {
    foreach ($report in $Reports) {
        Write-Host ('{0}: {1}x{2}, alpha {3}-{4}, visible {5}, SHA256 {6}' -f
            $report.Name, $report.Width, $report.Height, $report.MinAlpha, $report.MaxAlpha,
            $report.VisiblePixels, $report.Sha256)
        if ($report.Name -eq 'forge_heraldic_overlay.png' -or $report.Name -eq 'forge_heraldic_header_v2.png') {
            Write-Host ('  visible art in left/right edge bands: {0}/{1}' -f
                $report.LeftEdgeVisiblePixels, $report.RightEdgeVisiblePixels)
        }
    }
}

Assert-Masters $textures
$masterHashes = @{}
foreach ($texture in $textures) { $masterHashes[$texture.Name] = Get-Sha256Hex $texture.Master }

if ($Check) {
    $tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([char[]]@('\', '/'))
    $checkRoot = Join-Path $tempBase ('CalradiaForge-ImageGenTextureCheck-' + [guid]::NewGuid().ToString('N'))
    $resolvedCheckRoot = [IO.Path]::GetFullPath($checkRoot).TrimEnd([char[]]@('\', '/'))
    if ($resolvedCheckRoot.Equals($repositoryRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $repositoryRoot.StartsWith($resolvedCheckRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
        $resolvedCheckRoot.StartsWith($repositoryRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to create check staging under an overlapping repository path: '$resolvedCheckRoot'"
    }
    New-Item -ItemType Directory -Path $checkRoot | Out-Null
    try {
        $first = New-OutputSet $textures (Join-Path $checkRoot 'run-1')
        $second = New-OutputSet $textures (Join-Path $checkRoot 'run-2')
        $differences = @()
        for ($index = 0; $index -lt $first.Count; $index++) {
            if ($first[$index].Name -ne $second[$index].Name -or $first[$index].Sha256 -ne $second[$index].Sha256) {
                $differences += $first[$index].Name
            }
        }
        foreach ($texture in $textures) {
            if ((Get-Sha256Hex $texture.Master) -ne $masterHashes[$texture.Name]) {
                throw "ImageGen master changed while the check was running: '$($texture.Master)'"
            }
        }
        if ($differences.Count -gt 0) {
            throw ('Non-deterministic ImageGen texture output: ' + ($differences -join ', '))
        }
        Write-Reports $first
        Write-Host 'PASS: both isolated preparations produced identical RGBA PNG hashes; prepared outputs were not changed.'
    }
    finally {
        $resolvedParent = [IO.Path]::GetDirectoryName($resolvedCheckRoot)
        $resolvedLeaf = [IO.Path]::GetFileName($resolvedCheckRoot)
        if ($resolvedParent.Equals($tempBase, [StringComparison]::OrdinalIgnoreCase) -and
            $resolvedLeaf -match '^CalradiaForge-ImageGenTextureCheck-[0-9a-f]{32}$') {
            Remove-Item -LiteralPath $resolvedCheckRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    exit 0
}

$stageRoot = Join-Path ([IO.Path]::GetTempPath()) ('CalradiaForge-ImageGenTextureStage-' + [guid]::NewGuid().ToString('N'))
$resolvedStageRoot = [IO.Path]::GetFullPath($stageRoot).TrimEnd([char[]]@('\', '/'))
$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([char[]]@('\', '/'))
if (-not [IO.Path]::GetDirectoryName($resolvedStageRoot).TrimEnd([char[]]@('\', '/')).Equals(
        $tempBase, [StringComparison]::OrdinalIgnoreCase) -or
    [IO.Path]::GetFileName($resolvedStageRoot) -notmatch '^CalradiaForge-ImageGenTextureStage-[0-9a-f]{32}$') {
    throw "Refusing unexpected staging path '$resolvedStageRoot'."
}

$published = @()
$temporaryFiles = @()
New-Item -ItemType Directory -Path $stageRoot | Out-Null
try {
    $stagedReports = New-OutputSet $textures $stageRoot
    foreach ($texture in $textures) {
        if ((Get-Sha256Hex $texture.Master) -ne $masterHashes[$texture.Name]) {
            throw "ImageGen master changed while preparation was running: '$($texture.Master)'"
        }
    }
    New-Item -ItemType Directory -Path $preparedRoot -Force | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'

    foreach ($report in $stagedReports) {
        $destination = Join-Path $preparedRoot $report.Name
        Assert-PathIsWithin $destination $preparedRoot 'prepared texture output'
        $wasPresent = Test-Path -LiteralPath $destination -PathType Leaf
        if (Test-Path -LiteralPath $destination -PathType Container) {
            throw "Prepared output path is a directory: '$destination'"
        }
        if ($wasPresent) {
            $destinationItem = Get-Item -LiteralPath $destination -Force
            if (($destinationItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Prepared output must not be a reparse point: '$destination'"
            }
        }

        $oldHash = if ($wasPresent) { Get-Sha256Hex $destination } else { $null }
        if ($wasPresent -and $oldHash -eq $report.Sha256) {
            Write-Host "Unchanged: $($report.Name) ($($report.Sha256))"
            continue
        }

        $backup = $null
        if ($wasPresent) {
            $backup = $destination + '.bak.' + $stamp + '.' + [guid]::NewGuid().ToString('N').Substring(0, 8)
            [IO.File]::Copy($destination, $backup, $false)
            if ((Get-Sha256Hex $backup) -ne $oldHash) {
                throw "Backup SHA-256 verification failed for '$destination'; original is untouched."
            }
            Write-Host "Backup verified: $backup ($oldHash)"
        }

        $temporary = $destination + '.stage.' + [guid]::NewGuid().ToString('N') + '.tmp'
        $temporaryFiles += $temporary
        [IO.File]::Copy($report.Path, $temporary, $false)
        if ((Get-Sha256Hex $temporary) -ne $report.Sha256) {
            throw "Temporary output SHA-256 verification failed for '$destination'."
        }

        if ($wasPresent -and (Get-Sha256Hex $destination) -ne $oldHash) {
            throw "Prepared output changed concurrently; refusing replacement: '$destination'"
        }
        $record = [pscustomobject]@{
            Destination = $destination
            Backup = $backup
            WasPresent = $wasPresent
            OldHash = $oldHash
            NewHash = $report.Sha256
        }
        $published += $record
        if ($wasPresent) {
            [IO.File]::Replace($temporary, $destination, $null)
        }
        else {
            [IO.File]::Move($temporary, $destination)
        }
        $temporaryFiles = @($temporaryFiles | Where-Object { $_ -ne $temporary })
        if ((Get-Sha256Hex $destination) -ne $report.Sha256) {
            throw "Published output SHA-256 verification failed for '$destination'."
        }
        Write-Host "Prepared: $($report.Name) ($($report.Sha256))"
    }

    Write-Reports $stagedReports
    foreach ($retiredName in $retiredPreparedTextures) {
        Remove-VerifiedArchivedPreparedTexture $retiredName $archiveRoot
    }
    foreach ($retiredName in $retiredPreparedHeaderTextures) {
        Remove-VerifiedArchivedPreparedTexture $retiredName $roundArchiveRoot
    }
    Write-Host "PASS: fixed RGBA sprites are ready for the source generator: $preparedRoot"
}
catch {
    for ($index = $published.Count - 1; $index -ge 0; $index--) {
        $record = $published[$index]
        if ($record.WasPresent -and $record.Backup -and (Test-Path -LiteralPath $record.Backup -PathType Leaf)) {
            [IO.File]::Copy($record.Backup, $record.Destination, $true)
            if ((Get-Sha256Hex $record.Destination) -ne $record.OldHash) {
                Write-Warning "Rollback SHA-256 verification failed for '$($record.Destination)'. Backup retained at '$($record.Backup)'"
            }
            else {
                Write-Warning "Restored previous prepared texture: $($record.Destination)"
            }
        }
        elseif (-not $record.WasPresent -and (Test-Path -LiteralPath $record.Destination -PathType Leaf)) {
            Remove-Item -LiteralPath $record.Destination -Force
            Write-Warning "Removed newly created prepared texture after failure: $($record.Destination)"
        }
    }
    throw
}
finally {
    foreach ($temporary in $temporaryFiles) {
        Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue
    }
    $resolvedParent = [IO.Path]::GetDirectoryName($resolvedStageRoot)
    $resolvedLeaf = [IO.Path]::GetFileName($resolvedStageRoot)
    if ($resolvedParent.Equals($tempBase, [StringComparison]::OrdinalIgnoreCase) -and
        $resolvedLeaf -match '^CalradiaForge-ImageGenTextureStage-[0-9a-f]{32}$') {
        Remove-Item -LiteralPath $resolvedStageRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
