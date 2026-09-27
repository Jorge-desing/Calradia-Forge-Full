[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputJson,

    [string]$EditorDirectory = '',

    [ValidateRange(1, 100000)]
    [int]$MaxFiles = 50000,

    [ValidateRange(1, 1099511627776)]
    [long]$MaxCandidateBytes = 5368709120,

    [ValidateRange(1, 250000)]
    [int]$MaxDirectories = 50000,

    [ValidateRange(1, 256)]
    [int]$MaxDirectoryDepth = 64,

    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$Schema = 'calradiaforge.asset-batch-plan/v4'

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

function Get-RelativePath([string]$Root, [string]$FullName) {
    $relative = $FullName.Substring($Root.Length).TrimStart([char[]]@('\', '/'))
    return $relative.Replace('\', '/')
}

function Get-AssetClassification([string]$RelativePath, [string]$Extension) {
    if ($Extension -eq '.xml' -and $RelativePath -match '(?i)^GUI/[^/]*SpriteData\.xml$') {
        return [pscustomobject]@{
            Name = 'sprite-data-metadata'
            Candidate = $false
            Note = 'SpriteData is bounded workflow metadata used to map atlas names to declared categories and sheet IDs; it is not an importable asset.'
        }
    }

    if ($Extension -eq '.fbx' -or $Extension -eq '.psd') {
        return [pscustomobject]@{
            Name = 'documented-source-candidate'
            Candidate = $true
            Note = 'TaleWorlds documents this source format; this plan does not import or compile it.'
        }
    }

    if ($Extension -eq '.png' -and $RelativePath -match '(?i)(^|/)GUI/SpriteParts/ui_[^/]+/') {
        return [pscustomobject]@{
            Name = 'sprite-generator-input'
            Candidate = $true
            Note = 'PNG is located under a GUI/SpriteParts/ui_<category> path; use the official sprite generator, then Resource Browser.'
        }
    }

    if ($Extension -eq '.png' -and $RelativePath -match '(?i)(^|/)AssetSources/GauntletUI/') {
        return [pscustomobject]@{
            Name = 'resource-browser-atlas-candidate'
            Candidate = $true
            Note = 'This resembles a generated Gauntlet atlas source; confirm the category and import it in Resource Browser.'
        }
    }

    if ($Extension -in @('.png', '.tga', '.obj')) {
        return [pscustomobject]@{
            Name = 'unverified-import-candidate'
            Candidate = $true
            Note = 'The extension alone does not establish a Bannerlord import contract; review it in the matching editor workflow.'
        }
    }

    return [pscustomobject]@{
        Name = 'unsupported'
        Candidate = $false
        Note = 'No import support is asserted for this file by the batch planner.'
    }
}

function Get-SpriteDataIndex([object[]]$InventoryFiles, [string]$Root) {
    $metadataFiles = @($InventoryFiles | Where-Object { $_.classification -eq 'sprite-data-metadata' })
    if ($metadataFiles.Count -eq 0) {
        return [pscustomobject]@{ Status = 'missing'; RelativePath = $null; Categories = @(); Finding = 'No GUI/*SpriteData.xml file was found in the inventoried tree.' }
    }
    if ($metadataFiles.Count -gt 1) {
        return [pscustomobject]@{ Status = 'ambiguous'; RelativePath = $null; Categories = @(); Finding = 'More than one SpriteData file was found; choose the intended module metadata before mapping atlases.' }
    }

    $relativePath = $metadataFiles[0].relativePath
    $fullPath = Join-Path $Root ($relativePath.Replace('/', [IO.Path]::DirectorySeparatorChar))
    try {
        $metadataInfo = New-Object IO.FileInfo($fullPath)
        if ($metadataInfo.Length -gt 4194304) {
            return [pscustomobject]@{ Status = 'too-large'; RelativePath = $relativePath; Categories = @(); Finding = 'SpriteData exceeds the 4 MiB parsing limit.' }
        }

        $settings = New-Object System.Xml.XmlReaderSettings
        $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
        $settings.XmlResolver = $null
        $settings.MaxCharactersInDocument = 4194304
        $settings.MaxCharactersFromEntities = 0
        $document = New-Object System.Xml.XmlDocument
        $document.XmlResolver = $null
        $reader = [System.Xml.XmlReader]::Create($fullPath, $settings)
        try { $document.Load($reader) }
        finally { $reader.Dispose() }

        if ($null -eq $document.SelectSingleNode('/SpriteData/SpriteCategories')) {
            return [pscustomobject]@{ Status = 'invalid'; RelativePath = $relativePath; Categories = @(); Finding = 'The document has no /SpriteData/SpriteCategories section.' }
        }

        $categories = New-Object 'System.Collections.Generic.List[object]'
        $categoryNames = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
        foreach ($categoryNode in $document.SelectNodes('/SpriteData/SpriteCategories/SpriteCategory')) {
            $nameNode = $categoryNode.SelectSingleNode('./Name')
            $countNode = $categoryNode.SelectSingleNode('./SpriteSheetCount')
            $categoryName = if ($null -ne $nameNode) { $nameNode.InnerText.Trim() } else { '' }
            $declaredCount = 0
            if ([string]::IsNullOrWhiteSpace($categoryName) -or -not $categoryNames.Add($categoryName) -or
                $null -eq $countNode -or -not [int]::TryParse($countNode.InnerText.Trim(), [ref]$declaredCount) -or
                $declaredCount -lt 1 -or $declaredCount -gt 256) {
                return [pscustomobject]@{ Status = 'invalid'; RelativePath = $relativePath; Categories = @(); Finding = 'SpriteData has a missing, duplicate, or invalid category name or sheet count.' }
            }

            $sheetIds = New-Object 'System.Collections.Generic.List[int]'
            foreach ($sizeNode in $categoryNode.SelectNodes('./SpriteSheetSize')) {
                $sheetId = 0
                $width = 0
                $height = 0
                if (-not [int]::TryParse($sizeNode.GetAttribute('ID'), [ref]$sheetId) -or $sheetId -lt 1 -or
                    -not [int]::TryParse($sizeNode.GetAttribute('Width'), [ref]$width) -or $width -lt 1 -or
                    -not [int]::TryParse($sizeNode.GetAttribute('Height'), [ref]$height) -or $height -lt 1 -or
                    $sheetIds.Contains($sheetId)) {
                    return [pscustomobject]@{ Status = 'invalid'; RelativePath = $relativePath; Categories = @(); Finding = "SpriteData has an invalid or duplicate sheet ID or dimensions for '$categoryName'." }
                }
                $sheetIds.Add($sheetId)
            }
            if ($sheetIds.Count -ne $declaredCount) {
                return [pscustomobject]@{ Status = 'invalid'; RelativePath = $relativePath; Categories = @(); Finding = "SpriteData sheet count does not match its size records for '$categoryName'." }
            }
            $categories.Add([pscustomobject]@{ Name = $categoryName; SheetIds = $sheetIds.ToArray() })
        }

        if ($categories.Count -eq 0) {
            return [pscustomobject]@{ Status = 'invalid'; RelativePath = $relativePath; Categories = @(); Finding = 'SpriteData declares no sprite categories.' }
        }
        return [pscustomobject]@{ Status = 'valid'; RelativePath = $relativePath; Categories = $categories.ToArray(); Finding = $null }
    }
    catch {
        return [pscustomobject]@{ Status = 'invalid'; RelativePath = $relativePath; Categories = @(); Finding = "SpriteData could not be parsed safely: $($_.Exception.Message)" }
    }
}

$sourceItem = Get-Item -LiteralPath $SourceDirectory -ErrorAction Stop
if (-not $sourceItem.PSIsContainer) { throw "SourceDirectory must be a directory: $SourceDirectory" }
if (($sourceItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw 'SourceDirectory cannot itself be a reparse point; choose its real directory path explicitly.'
}
$sourceRoot = [IO.Path]::GetFullPath($sourceItem.FullName).TrimEnd([char[]]@('\', '/'))
$outputPath = [IO.Path]::GetFullPath($OutputJson)
if ([IO.Path]::GetExtension($outputPath) -ine '.json') { throw 'OutputJson must use the .json extension.' }
$sourcePrefix = $sourceRoot + [IO.Path]::DirectorySeparatorChar
if ($outputPath.Equals($sourceRoot, [StringComparison]::OrdinalIgnoreCase) -or
    $outputPath.StartsWith($sourcePrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputJson must be outside SourceDirectory so the plan cannot contaminate its own input.'
}
if ((Test-Path -LiteralPath $outputPath -PathType Leaf) -and -not $Force) {
    throw "Output already exists. Use -Force to replace it with a timestamped backup: $outputPath"
}

$inventoryTimer = [Diagnostics.Stopwatch]::StartNew()
$files = New-Object 'System.Collections.Generic.List[object]'
$warnings = New-Object 'System.Collections.Generic.List[string]'
$pendingDirectories = New-Object 'System.Collections.Generic.Stack[object]'
$pendingDirectories.Push((Get-Item -LiteralPath $sourceRoot))
$discoveredDirectoryCount = 1
$visitedFileCount = 0
$visitedDirectoryCount = 0
$candidateBytesConsidered = [long]0
$candidateBytesHashed = [long]0
$skippedReparsePointCount = 0

while ($pendingDirectories.Count -gt 0) {
    $directory = $pendingDirectories.Pop()
    $visitedDirectoryCount++
    if ($visitedDirectoryCount -gt $MaxDirectories) {
        throw "Source scan exceeded MaxDirectories=$MaxDirectories; no report was written. Increase the explicit bound if this batch is intentional."
    }
    $relativeDirectory = Get-RelativePath $sourceRoot $directory.FullName
    $depth = if ([string]::IsNullOrEmpty($relativeDirectory)) { 0 } else { @($relativeDirectory -split '/').Count }
    if ($depth -gt $MaxDirectoryDepth) {
        $warnings.Add("Skipped directory deeper than $MaxDirectoryDepth levels: $relativeDirectory")
        continue
    }

    try {
      foreach ($entry in $directory.EnumerateFileSystemInfos()) {
        try { $attributes = $entry.Attributes }
        catch {
            $warnings.Add("Could not read attributes for '$((Get-RelativePath $sourceRoot $entry.FullName))': $($_.Exception.Message)")
            continue
        }

        if (($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            $skippedReparsePointCount++
            continue
        }

        if (($attributes -band [IO.FileAttributes]::Directory) -ne 0) {
            $discoveredDirectoryCount++
            if ($discoveredDirectoryCount -gt $MaxDirectories) {
                throw "Source scan exceeded MaxDirectories=$MaxDirectories; no report was written. Increase the explicit bound if this batch is intentional."
            }
            $pendingDirectories.Push($entry)
            continue
        }

        $visitedFileCount++
        if ($visitedFileCount -gt $MaxFiles) {
            throw "Source scan exceeded MaxFiles=$MaxFiles; no report was written. Increase the explicit bound if this batch is intentional."
        }

        $relativePath = Get-RelativePath $sourceRoot $entry.FullName
        $extension = [IO.Path]::GetExtension($entry.Name).ToLowerInvariant()
        $classification = Get-AssetClassification $relativePath $extension
        $fileLength = [long]0
        $sha256 = $null
        $scanState = 'metadata-only'
        try {
            $fileInfo = New-Object IO.FileInfo($entry.FullName)
            $fileLength = [long]$fileInfo.Length
            if ($classification.Candidate) {
                $candidateBytesConsidered += $fileLength
                if ($candidateBytesConsidered -gt $MaxCandidateBytes) {
                    throw "Source scan exceeded MaxCandidateBytes=$MaxCandidateBytes; no report was written. Increase the explicit bound if this batch is intentional."
                }
                $sha256 = Get-Sha256Hex $entry.FullName
                $candidateBytesHashed += $fileLength
                $scanState = 'hashed'
            }
        }
        catch {
            if ($_.Exception.Message -like 'Source scan exceeded MaxCandidateBytes=*') { throw }
            $warnings.Add("Could not read '$relativePath': $($_.Exception.Message)")
            $scanState = 'unreadable'
        }

        $files.Add([ordered]@{
            relativePath = $relativePath
            extension = $extension
            lengthBytes = $fileLength
            sha256 = $sha256
            candidate = [bool]$classification.Candidate
            classification = $classification.Name
            importStatus = 'not-attempted'
            scanState = $scanState
            note = $classification.Note
        })
      }
    }
    catch {
        if ($_.Exception.Message -like 'Source scan exceeded MaxFiles=*' -or
            $_.Exception.Message -like 'Source scan exceeded MaxCandidateBytes=*' -or
            $_.Exception.Message -like 'Source scan exceeded MaxDirectories=*') {
            throw
        }
        $warnings.Add("Directory enumeration stopped for '$relativeDirectory': $($_.Exception.Message)")
    }
}

if ($skippedReparsePointCount -gt 0) {
    $warnings.Add("Skipped $skippedReparsePointCount reparse point(s); their targets were not scanned.")
}

$sortedFiles = @($files | Sort-Object -Property relativePath)
$candidateFiles = @($sortedFiles | Where-Object { $_.candidate -eq $true })
$documentedCount = @($sortedFiles | Where-Object { $_.classification -eq 'documented-source-candidate' }).Count
$spriteInputCount = @($sortedFiles | Where-Object { $_.classification -eq 'sprite-generator-input' }).Count
$atlasCount = @($sortedFiles | Where-Object { $_.classification -eq 'resource-browser-atlas-candidate' }).Count
$unverifiedCount = @($sortedFiles | Where-Object { $_.classification -eq 'unverified-import-candidate' }).Count
$unsupportedCount = @($sortedFiles | Where-Object { $_.classification -eq 'unsupported' }).Count
$spriteDataIndex = Get-SpriteDataIndex $sortedFiles $sourceRoot
$atlasMappings = New-Object 'System.Collections.Generic.List[object]'
foreach ($atlas in @($sortedFiles | Where-Object { $_.classification -eq 'resource-browser-atlas-candidate' })) {
    $relativePath = [string]$atlas.relativePath
    $baseName = [IO.Path]::GetFileNameWithoutExtension($relativePath)
    $matchStatus = 'sprite-data-' + $spriteDataIndex.Status
    $importCategory = $null
    $sheetId = $null
    $expectedTpacPath = $null
    if ($spriteDataIndex.Status -eq 'valid') {
        if ($baseName -match '^(?<category>ui_[A-Za-z0-9_]+)_(?<sheetId>[1-9][0-9]*)$') {
            $importCategory = $Matches.category
            $sheetId = [int]$Matches.sheetId
            $categoryRecord = @($spriteDataIndex.Categories | Where-Object { $_.Name -ceq $importCategory }) | Select-Object -First 1
            if ($null -eq $categoryRecord) {
                $matchStatus = 'category-not-declared'
                $importCategory = $null
                $sheetId = $null
            }
            elseif ($categoryRecord.SheetIds -contains $sheetId) {
                $matchStatus = 'matched'
                $expectedTpacPath = 'Assets/GauntletUI/' + $baseName + '_tex.tpac'
            }
            else {
                $matchStatus = 'sheet-not-declared'
            }
        }
        else {
            $matchStatus = 'unrecognized-atlas-name'
        }
    }
    $atlasMappings.Add([ordered]@{
        sourceAtlas = $relativePath
        mappingStatus = $matchStatus
        importCategory = $importCategory
        sheetId = $sheetId
        expectedRuntimeTpac = $expectedTpacPath
        expectedOutputIsPrediction = $true
        verifiedInEditor = $false
    })
}
$matchedAtlasCount = @($atlasMappings | Where-Object { $_.mappingStatus -eq 'matched' }).Count
$unresolvedAtlasCount = $atlasCount - $matchedAtlasCount
$inventoryTimer.Stop()
$plannerProcessPeakWorkingSetBytes = $null
try {
    $plannerProcessPeakWorkingSetBytes = [long](Get-Process -Id $PID -ErrorAction Stop).PeakWorkingSet64
}
catch { }
$plannerProcessPeakWorkingSetStatus = if ($null -ne $plannerProcessPeakWorkingSetBytes -and $plannerProcessPeakWorkingSetBytes -gt 0) { 'measured' } else { 'unavailable' }

if ([string]::IsNullOrWhiteSpace($EditorDirectory)) {
    if (-not [string]::IsNullOrWhiteSpace($env:BANNERLORD_EDITOR_DIR)) {
        $EditorDirectory = $env:BANNERLORD_EDITOR_DIR
    }
    else {
        $EditorDirectory = Join-Path ${env:ProgramFiles(x86)} 'Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_wEditor'
    }
}
$resolvedEditorDirectory = [IO.Path]::GetFullPath($EditorDirectory)
$spriteGeneratorPath = Join-Path $resolvedEditorDirectory 'TaleWorlds.TwoDimension.SpriteSheetGenerator.exe'
$spriteGeneratorLibraryPath = Join-Path $resolvedEditorDirectory 'TaleWorlds.TwoDimension.SpriteSheetGenerator.Library.dll'
$spriteGeneratorAvailable = (Test-Path -LiteralPath $spriteGeneratorPath -PathType Leaf) -and
    (Test-Path -LiteralPath $spriteGeneratorLibraryPath -PathType Leaf)
$spriteWorkflowStatus = if ($spriteGeneratorAvailable) { 'manual-run-ready' } else { 'tool-unavailable' }
$toolAvailabilityStatus = if ($spriteGeneratorAvailable) { 'available-not-run' } else { 'unavailable' }
$inventoryWorkflowStatus = if ($warnings.Count -eq 0) { 'completed' } else { 'partial' }
$inventoryReportStatus = if ($warnings.Count -eq 0) { 'complete' } else { 'partial' }
$atlasImportStatus = if ($unresolvedAtlasCount -eq 0) { 'manual-required' } else { 'manual-review' }
$nextStepRecommendation = if ($spriteInputCount -gt 0) {
    if ($spriteGeneratorAvailable) {
        'Run the official sprite generator in a visible interactive console, then import its atlas in Resource Browser; verify the TPAC separately.'
    }
    else {
        'UI sprite inputs were found, but the official generator and companion library are unavailable here. Set -EditorDirectory or BANNERLORD_EDITOR_DIR, then run the documented interactive workflow.'
    }
}
elseif ($atlasCount -gt 0) {
    if ($unresolvedAtlasCount -eq 0) {
        'Import each atlas into its SpriteData-declared category in Resource Browser; confirm the predicted TPAC path and verify the package separately.'
    }
    else {
        'Review the SpriteData/atlas mapping findings first; import unresolved atlases only after confirming their categories and sheet IDs in Resource Browser.'
    }
}
else {
    'Review this manifest and use the matching official editor workflow. Verify every result in the editor.'
}

$workflowStages = New-Object 'System.Collections.Generic.List[object]'
$workflowStages.Add([ordered]@{
    id = 'inventory-source'
    status = $inventoryWorkflowStatus
    executed = $true
    evidence = "Scanned $($sortedFiles.Count) files; hashed $candidateBytesHashed candidate bytes."
})
if ($documentedCount -gt 0) {
    $workflowStages.Add([ordered]@{
        id = 'review-model-and-texture-sources'
        status = 'manual-review'
        executed = $false
        fileCount = $documentedCount
        instruction = 'Use the matching documented Bannerlord editor workflow; this plan does not construct or import mesh/texture assets.'
    })
}
if ($spriteInputCount -gt 0) {
    $workflowStages.Add([ordered]@{
        id = 'generate-ui-sprite-atlas'
        status = $spriteWorkflowStatus
        executed = $false
        fileCount = $spriteInputCount
        tool = $spriteGeneratorPath
        companionLibraryAvailable = (Test-Path -LiteralPath $spriteGeneratorLibraryPath -PathType Leaf)
        requiresVisibleInteractiveConsole = $true
        instruction = 'Run the official SpriteSheetGenerator in its supported visible-console workflow; redirected/background execution is not assumed.'
    })
}
if ($atlasCount -gt 0) {
    $workflowStages.Add([ordered]@{
        id = 'import-atlas-in-resource-browser'
        status = $atlasImportStatus
        executed = $false
        fileCount = $atlasCount
        mappings = $atlasMappings.ToArray()
        instruction = 'Use each SpriteData-matched category as an import hint, scan and import in Bannerlord Resource Browser, and confirm the actual output path.'
    })
}
elseif ($spriteInputCount -gt 0) {
    $workflowStages.Add([ordered]@{
        id = 'import-atlas-in-resource-browser'
        status = 'awaiting-generated-atlas'
        executed = $false
        fileCount = 0
        instruction = 'Generate the atlas first; then use Resource Browser to scan and import it.'
    })
}
if ($unverifiedCount -gt 0) {
    $workflowStages.Add([ordered]@{
        id = 'review-unverified-formats'
        status = 'manual-review'
        executed = $false
        fileCount = $unverifiedCount
        instruction = 'A filename extension is not proof of an engine import contract; verify each format in the matching editor workflow.'
    })
}
$workflowStages.Add([ordered]@{
    id = 'verify-runtime-tpac'
    status = 'not-attempted'
    executed = $false
    instruction = 'Compile/import through Resource Browser and verify the resulting TPAC separately; this planner and TpacTool do not compile assets.'
})

$reviewItems = @(
    $candidateFiles |
        Group-Object { [IO.Path]::GetFileNameWithoutExtension($_.relativePath) } |
        Where-Object { $_.Count -gt 1 } |
        ForEach-Object {
            [ordered]@{
                basename = $_.Name
                count = $_.Count
                relativePaths = @($_.Group | ForEach-Object { $_.relativePath })
                finding = 'Repeated basename; review manually. The planner does not infer an asset collision.'
            }
        }
)

$report = [ordered]@{
    schema = $Schema
    planId = [guid]::NewGuid().ToString('D')
    createdUtc = [DateTime]::UtcNow.ToString('o')
    sourceRoot = $sourceRoot
    mode = 'inventory-only'
    status = $inventoryReportStatus
    importExecuted = $false
    compilerInvoked = $false
    toolAvailability = [ordered]@{
        spriteSheetGenerator = $toolAvailabilityStatus
        spriteSheetGeneratorPath = $spriteGeneratorPath
        editorDirectory = $resolvedEditorDirectory
    }
    limits = [ordered]@{
        maxFiles = $MaxFiles
        maxCandidateBytes = $MaxCandidateBytes
        maxDirectories = $MaxDirectories
        maxDirectoryDepth = $MaxDirectoryDepth
        maxSpriteDataBytes = 4194304
        reparsePointsFollowed = $false
    }
    summary = [ordered]@{
        totalFiles = $sortedFiles.Count
        directoriesDiscovered = $discoveredDirectoryCount
        directoriesVisited = $visitedDirectoryCount
        candidateFiles = $candidateFiles.Count
        candidateBytesConsidered = $candidateBytesConsidered
        candidateBytesHashed = $candidateBytesHashed
        documentedSourceCandidates = $documentedCount
        spriteGeneratorInputs = $spriteInputCount
        resourceBrowserAtlasCandidates = $atlasCount
        atlasMappingsMatched = $matchedAtlasCount
        atlasMappingsUnresolved = $unresolvedAtlasCount
        spriteDataStatus = $spriteDataIndex.Status
        spriteDataCategories = $spriteDataIndex.Categories.Count
        unverifiedCandidates = $unverifiedCount
        unsupportedFiles = $unsupportedCount
        repeatedBasenameGroups = $reviewItems.Count
        skippedReparsePoints = $skippedReparsePointCount
        warnings = $warnings.Count
    }
    reviewItems = $reviewItems
    files = $sortedFiles
    warnings = @($warnings)
    spriteData = [ordered]@{
        status = $spriteDataIndex.Status
        relativePath = $spriteDataIndex.RelativePath
        categoryCount = $spriteDataIndex.Categories.Count
        finding = $spriteDataIndex.Finding
        dtdProcessing = 'prohibited'
    }
    atlasMappings = $atlasMappings.ToArray()
    recommendedWorkflow = $workflowStages.ToArray()
    measurements = [ordered]@{
        inventoryDurationMilliseconds = [long]$inventoryTimer.ElapsedMilliseconds
        plannerProcessPeakWorkingSetBytes = $plannerProcessPeakWorkingSetBytes
        plannerProcessPeakWorkingSetStatus = $plannerProcessPeakWorkingSetStatus
        inventoryMeasurementScope = 'directory traversal, candidate hashing, bounded SpriteData parsing, and atlas mapping'
        inventoryCountsAndHashes = 'measured'
        inventoryThreadAllocations = 'not-measured'
        generationDuration = 'not-measured'
        generatorMemoryOrThroughput = 'not-measured'
        crashReduction = 'not-measured'
        note = 'Inventory duration covers the listed planner operations. Peak working set covers the PowerShell host process lifetime and is not attributable to this script alone. Generator and game behavior are not measured.'
    }
    limitsAndClaims = @(
        'File extensions are evidence labels for triage, not proof of successful import or engine compatibility.'
        'No .meta sidecars are generated; no resource files are changed.'
        'No compiler or Resource Browser command is invoked; every importStatus is not-attempted.'
        'SpriteData parsing is capped at 4 MiB with DTD and external entity resolution disabled; atlas/category matches are hints based on filename and declared sheet IDs.'
        'Predicted TPAC output paths must be confirmed after a Resource Browser import.'
        'Planner duration covers inventory and mapping only; peak working set is process-lifetime context, not per-operation allocation or game memory.'
        'FBX and PSD source types, sprite-part folder conventions, and the atlas import path follow the documented Bannerlord workflow.'
        'The infographic claims about .fbs patching, undocumented compiler switches, redirected-stdin automation, and Roslyn injection are not treated as supported interfaces.'
    )
    nextStep = $nextStepRecommendation
}

$outputDirectory = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}
$temporaryPath = "$outputPath.tmp.$([guid]::NewGuid().ToString('N'))"
$backupPath = $null
try {
    $jsonText = ConvertTo-Json -InputObject $report -Depth 8
    [IO.File]::WriteAllText($temporaryPath, $jsonText, (New-Object Text.UTF8Encoding($false)))

    if (Test-Path -LiteralPath $outputPath -PathType Leaf) {
        $backupPath = "$outputPath.bak.$(Get-Date -Format 'yyyyMMdd-HHmmss-fff')"
        if (Test-Path -LiteralPath $backupPath) { throw "Backup path already exists: $backupPath" }
        [IO.File]::Replace($temporaryPath, $outputPath, $backupPath)
        Write-Host "Backup: $backupPath"
    }
    else {
        [IO.File]::Move($temporaryPath, $outputPath)
    }
}
finally {
    if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
        Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Asset batch plan: $outputPath"
Write-Host "Status: $($report.status); files: $($report.summary.totalFiles); candidates: $($report.summary.candidateFiles); review groups: $($report.summary.repeatedBasenameGroups)."
Write-Host 'Inventory only: import not attempted; compiler not invoked; source files unchanged.'
if ($report.status -eq 'partial') {
    # Preserve the CLI's non-zero exit contract while keeping the script
    # composable in the fixture harness without terminating its PowerShell host.
    throw 'Asset batch inventory is partial; inspect the report warnings before using it.'
}
