$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$planScript = Join-Path $repositoryRoot 'tools\Plan-CalradiaForge-AssetBatch.ps1'
$testRoot = Join-Path $env:TEMP ('CalradiaForge-AssetBatchPlan-' + [Guid]::NewGuid().ToString('N'))
$script:editorFixture = Join-Path $testRoot 'editor'

function Assert([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }

function Invoke-Plan([string]$source, [string]$output, [string[]]$extra = @(), [string]$editorDirectory = $script:editorFixture) {
    $arguments = @{ SourceDirectory = $source; OutputJson = $output; EditorDirectory = $editorDirectory }
    for ($index = 0; $index -lt $extra.Count; $index++) {
        $name = $extra[$index].TrimStart('-')
        if ($index + 1 -lt $extra.Count -and -not $extra[$index + 1].StartsWith('-')) {
            $arguments[$name] = $extra[++$index]
        }
        else {
            $arguments[$name] = $true
        }
    }
    $captured = New-Object 'System.Collections.Generic.List[object]'
    $exitCode = 0
    try {
        & $planScript @arguments *>&1 | ForEach-Object { $captured.Add($_) }
    }
    catch {
        $exitCode = 1
        $captured.Add($_.Exception.Message)
    }
    return [pscustomobject]@{ ExitCode = $exitCode; Output = ($captured | Out-String) }
}

try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null
    New-Item -ItemType Directory -Path $script:editorFixture | Out-Null
    Set-Content -LiteralPath (Join-Path $script:editorFixture 'TaleWorlds.TwoDimension.SpriteSheetGenerator.exe') -Value 'test fixture; never executed'
    Set-Content -LiteralPath (Join-Path $script:editorFixture 'TaleWorlds.TwoDimension.SpriteSheetGenerator.Library.dll') -Value 'test fixture; never loaded'
    $source = Join-Path $testRoot 'raw-assets'
    New-Item -ItemType Directory -Path (Join-Path $source 'GUI\SpriteParts\ui_demo') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $source 'AssetSources\Models') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $source 'AssetSources\Textures') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $source 'AssetSources\GauntletUI') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $source 'Loose') -Force | Out-Null
    [IO.File]::WriteAllBytes((Join-Path $source 'GUI\SpriteParts\ui_demo\marker.png'), [byte[]](137, 80, 78, 71, 1, 2))
    [IO.File]::WriteAllBytes((Join-Path $source 'AssetSources\GauntletUI\ui_demo_1.png'), [byte[]](137, 80, 78, 71, 2, 3))
    $spriteDataPath = Join-Path $source 'GUI\DemoSpriteData.xml'
    $validSpriteData = '<?xml version="1.0"?><SpriteData><SpriteCategories><SpriteCategory><Name>ui_demo</Name><SpriteSheetCount>1</SpriteSheetCount><SpriteSheetSize ID="1" Width="128" Height="128" /></SpriteCategory></SpriteCategories></SpriteData>'
    [IO.File]::WriteAllText($spriteDataPath, $validSpriteData, (New-Object Text.UTF8Encoding($false)))
    [IO.File]::WriteAllText((Join-Path $source 'AssetSources\Models\fort.fbx'), 'fbx-fixture')
    [IO.File]::WriteAllText((Join-Path $source 'AssetSources\Textures\cloth.psd'), 'psd-fixture')
    [IO.File]::WriteAllText((Join-Path $source 'Loose\banner.png'), 'png-fixture')
    [IO.File]::WriteAllText((Join-Path $source 'Loose\mesh.obj'), 'obj-fixture')
    [IO.File]::WriteAllText((Join-Path $source 'Loose\texture.tga'), 'tga-fixture')
    [IO.File]::WriteAllText((Join-Path $source 'Loose\notes.txt'), 'text-fixture')
    [IO.File]::WriteAllText((Join-Path $source 'Loose\nested-wall.fbx'), 'duplicate-stem-fixture')
    New-Item -ItemType Directory -Path (Join-Path $source 'Nested') -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $source 'Nested\nested-wall.fbx'), 'duplicate-stem-fixture-2')

    $originalFiles = @(Get-ChildItem -LiteralPath $source -File -Recurse | ForEach-Object { $_.FullName.Substring($source.Length).TrimStart('\') } | Sort-Object)
    $outputPath = Join-Path $testRoot 'reports\asset-batch.json'
    $run = Invoke-Plan $source $outputPath
    Assert ($run.ExitCode -eq 0) "Asset batch planning failed: $($run.Output)"
    Assert (Test-Path -LiteralPath $outputPath -PathType Leaf) 'Asset batch JSON was not created.'
    $report = Get-Content -LiteralPath $outputPath -Raw | ConvertFrom-Json
    Assert ($report.schema -eq 'calradiaforge.asset-batch-plan/v4') 'Unexpected report schema.'
    Assert ($report.mode -eq 'inventory-only' -and $report.importExecuted -eq $false -and $report.compilerInvoked -eq $false) 'The plan does not clearly state that import and compilation were not run.'
    Assert ($report.files.Count -eq 11) 'The recursive inventory did not include every fixture file.'
    Assert ($report.summary.documentedSourceCandidates -eq 4) 'FBX and PSD were not classified as documented source candidates.'
    Assert ($report.summary.spriteGeneratorInputs -eq 1) 'A PNG under GUI/SpriteParts/ui_demo was not recognized as sprite-generator input.'
    Assert ($report.summary.resourceBrowserAtlasCandidates -eq 1) 'A PNG under AssetSources/GauntletUI was not recognized as an atlas candidate.'
    Assert ($report.summary.spriteDataStatus -eq 'valid' -and $report.summary.spriteDataCategories -eq 1) 'A valid SpriteData category was not read.'
    Assert ($report.summary.atlasMappingsMatched -eq 1 -and $report.summary.atlasMappingsUnresolved -eq 0) 'The atlas was not mapped to its declared category and sheet ID.'
    Assert ($report.summary.unverifiedCandidates -eq 3) 'Unverified PNG, OBJ, and TGA candidates were not counted.'
    Assert ($report.summary.unsupportedFiles -eq 1) 'An unsupported extension was not reported.'

    $sprite = $report.files | Where-Object relativePath -eq 'GUI/SpriteParts/ui_demo/marker.png'
    Assert ($sprite.classification -eq 'sprite-generator-input' -and $sprite.importStatus -eq 'not-attempted') 'Sprite input classification or import status is misleading.'
    Assert ($sprite.sha256 -match '^[A-F0-9]{64}$') 'Candidate SHA-256 was not included.'
    $hashedCandidateBytes = [long](($report.files | Where-Object { $_.sha256 } | Measure-Object -Property lengthBytes -Sum).Sum)
    Assert ($report.summary.candidateBytesHashed -eq $hashedCandidateBytes) 'The hashed-byte metric includes files that were not hashed.'
    $mesh = $report.files | Where-Object relativePath -eq 'AssetSources/Models/fort.fbx'
    Assert ($mesh.classification -eq 'documented-source-candidate') 'FBX was not marked with its documented-source classification.'
    $loosePng = $report.files | Where-Object relativePath -eq 'Loose/banner.png'
    Assert ($loosePng.classification -eq 'unverified-import-candidate') 'PNG outside a SpriteParts category was incorrectly treated as a confirmed import input.'
    $text = $report.files | Where-Object relativePath -eq 'Loose/notes.txt'
    Assert ($text.classification -eq 'unsupported' -and [string]::IsNullOrEmpty($text.sha256)) 'Unsupported input was hashed or not reported as unsupported.'
    Assert ($report.reviewItems.Count -eq 1 -and $report.reviewItems[0].basename -eq 'nested-wall') 'Repeated basenames were not surfaced as review items.'
    $spriteMetadata = $report.files | Where-Object relativePath -eq 'GUI/DemoSpriteData.xml'
    Assert ($spriteMetadata.classification -eq 'sprite-data-metadata' -and $spriteMetadata.candidate -eq $false -and [string]::IsNullOrEmpty($spriteMetadata.sha256)) 'SpriteData was misclassified as an importable or hashed asset.'
    Assert ($report.spriteData.status -eq 'valid' -and $report.spriteData.dtdProcessing -eq 'prohibited') 'The bounded SpriteData evidence was not reported.'
    $atlasMapping = $report.atlasMappings | Where-Object sourceAtlas -eq 'AssetSources/GauntletUI/ui_demo_1.png'
    Assert ($atlasMapping.mappingStatus -eq 'matched' -and $atlasMapping.importCategory -ceq 'ui_demo' -and $atlasMapping.sheetId -eq 1) 'The atlas/category mapping did not match SpriteData.'
    Assert ($atlasMapping.expectedRuntimeTpac -eq 'Assets/GauntletUI/ui_demo_1_tex.tpac' -and $atlasMapping.expectedOutputIsPrediction -and -not $atlasMapping.verifiedInEditor) 'The report did not mark the expected TPAC path as a prediction.'
    Assert ($report.toolAvailability.spriteSheetGenerator -eq 'available-not-run') 'The planner did not report the fixture generator as available without running it.'
    $generationStep = $report.recommendedWorkflow | Where-Object id -eq 'generate-ui-sprite-atlas'
    Assert ($generationStep.status -eq 'manual-run-ready' -and $generationStep.executed -eq $false -and $generationStep.requiresVisibleInteractiveConsole -eq $true) 'The workflow overstated sprite generation or omitted its interactive-console requirement.'
    $importStep = $report.recommendedWorkflow | Where-Object id -eq 'import-atlas-in-resource-browser'
    Assert ($importStep.status -eq 'manual-required' -and $importStep.executed -eq $false) 'The workflow claimed or omitted the manual Resource Browser import step.'
    $tpacStep = $report.recommendedWorkflow | Where-Object id -eq 'verify-runtime-tpac'
    Assert ($tpacStep.status -eq 'not-attempted' -and $tpacStep.executed -eq $false) 'The workflow falsely claimed a TPAC compile or validation.'
    Assert ($report.measurements.generationDuration -eq 'not-measured' -and $report.measurements.crashReduction -eq 'not-measured') 'The report adopted infographic performance claims as measurements.'
    Assert ($report.measurements.inventoryDurationMilliseconds -ge 0) 'The planner did not record a valid short inventory duration.'
    Assert ($report.measurements.inventoryMeasurementScope -like '*bounded SpriteData parsing*') 'The inventory measurement scope was not explicit.'
    Assert ($report.measurements.plannerProcessPeakWorkingSetStatus -in @('measured', 'unavailable')) 'The planner process memory observation has an invalid status.'
    if ($report.measurements.plannerProcessPeakWorkingSetStatus -eq 'measured') {
        Assert ($report.measurements.plannerProcessPeakWorkingSetBytes -gt 0) 'A measured process peak working set must be positive.'
    }
    else {
        Assert ($null -eq $report.measurements.plannerProcessPeakWorkingSetBytes) 'An unavailable process memory observation must not fabricate a value.'
    }
    Assert ($report.measurements.inventoryThreadAllocations -eq 'not-measured' -and $report.measurements.generatorMemoryOrThroughput -eq 'not-measured') 'The planner attributed unmeasured allocation or generator metrics.'

    $afterFiles = @(Get-ChildItem -LiteralPath $source -File -Recurse | ForEach-Object { $_.FullName.Substring($source.Length).TrimStart('\') } | Sort-Object)
    Assert (($originalFiles -join '|') -eq ($afterFiles -join '|')) 'The plan wrote into or changed the source tree.'
    Assert (-not (Get-ChildItem -LiteralPath $source -File -Recurse | Where-Object Extension -eq '.meta')) 'The plan created speculative sidecar metadata.'

    [IO.File]::WriteAllText($spriteDataPath, '<SpriteData><SpriteCategories>', (New-Object Text.UTF8Encoding($false)))
    $invalidSpriteDataOutput = Join-Path $testRoot 'reports\invalid-sprite-data.json'
    $invalidSpriteDataRun = Invoke-Plan $source $invalidSpriteDataOutput
    Assert ($invalidSpriteDataRun.ExitCode -eq 0) 'Malformed SpriteData should be reported without aborting the bounded inventory.'
    $invalidSpriteDataReport = Get-Content -LiteralPath $invalidSpriteDataOutput -Raw | ConvertFrom-Json
    Assert ($invalidSpriteDataReport.spriteData.status -eq 'invalid' -and $invalidSpriteDataReport.atlasMappings[0].mappingStatus -eq 'sprite-data-invalid') 'Malformed SpriteData was treated as trusted mapping evidence.'

    $dtdSpriteData = '<!DOCTYPE SpriteData [<!ENTITY category "ui_demo">]><SpriteData><SpriteCategories><SpriteCategory><Name>&category;</Name><SpriteSheetCount>1</SpriteSheetCount><SpriteSheetSize ID="1" Width="128" Height="128" /></SpriteCategory></SpriteCategories></SpriteData>'
    [IO.File]::WriteAllText($spriteDataPath, $dtdSpriteData, (New-Object Text.UTF8Encoding($false)))
    $dtdSpriteDataOutput = Join-Path $testRoot 'reports\dtd-sprite-data.json'
    $dtdSpriteDataRun = Invoke-Plan $source $dtdSpriteDataOutput
    Assert ($dtdSpriteDataRun.ExitCode -eq 0) 'DTD-bearing SpriteData should be rejected as evidence without failing inventory.'
    $dtdSpriteDataReport = Get-Content -LiteralPath $dtdSpriteDataOutput -Raw | ConvertFrom-Json
    Assert ($dtdSpriteDataReport.spriteData.status -eq 'invalid' -and $dtdSpriteDataReport.atlasMappings[0].mappingStatus -eq 'sprite-data-invalid') 'SpriteData DTD processing was not prohibited.'

    $wrongSheetData = $validSpriteData.Replace('ID="1"', 'ID="2"')
    [IO.File]::WriteAllText($spriteDataPath, $wrongSheetData, (New-Object Text.UTF8Encoding($false)))
    $wrongSheetOutput = Join-Path $testRoot 'reports\wrong-sheet.json'
    $wrongSheetRun = Invoke-Plan $source $wrongSheetOutput
    Assert ($wrongSheetRun.ExitCode -eq 0) 'A valid but undeclared sheet should remain a review finding, not abort inventory.'
    $wrongSheetReport = Get-Content -LiteralPath $wrongSheetOutput -Raw | ConvertFrom-Json
    $wrongSheetStep = $wrongSheetReport.recommendedWorkflow | Where-Object id -eq 'import-atlas-in-resource-browser'
    Assert ($wrongSheetReport.atlasMappings[0].mappingStatus -eq 'sheet-not-declared' -and $wrongSheetReport.summary.atlasMappingsUnresolved -eq 1) 'An undeclared atlas sheet was treated as matched.'
    Assert ($wrongSheetStep.status -eq 'manual-review') 'The import workflow did not request review for an undeclared sheet.'

    $wrongCategoryData = $validSpriteData.Replace('<Name>ui_demo</Name>', '<Name>ui_other</Name>')
    [IO.File]::WriteAllText($spriteDataPath, $wrongCategoryData, (New-Object Text.UTF8Encoding($false)))
    $wrongCategoryOutput = Join-Path $testRoot 'reports\wrong-category.json'
    $wrongCategoryRun = Invoke-Plan $source $wrongCategoryOutput
    Assert ($wrongCategoryRun.ExitCode -eq 0) 'A valid but undeclared atlas category should remain a review finding.'
    $wrongCategoryReport = Get-Content -LiteralPath $wrongCategoryOutput -Raw | ConvertFrom-Json
    Assert ($wrongCategoryReport.atlasMappings[0].mappingStatus -eq 'category-not-declared' -and $wrongCategoryReport.summary.atlasMappingsUnresolved -eq 1) 'An undeclared atlas category was treated as matched.'

    [IO.File]::WriteAllText((Join-Path $source 'GUI\OtherSpriteData.xml'), $validSpriteData, (New-Object Text.UTF8Encoding($false)))
    $ambiguousOutput = Join-Path $testRoot 'reports\ambiguous-sprite-data.json'
    $ambiguousRun = Invoke-Plan $source $ambiguousOutput
    Assert ($ambiguousRun.ExitCode -eq 0) 'Multiple SpriteData files should produce a bounded ambiguity finding.'
    $ambiguousReport = Get-Content -LiteralPath $ambiguousOutput -Raw | ConvertFrom-Json
    Assert ($ambiguousReport.spriteData.status -eq 'ambiguous' -and $ambiguousReport.atlasMappings[0].mappingStatus -eq 'sprite-data-ambiguous') 'Multiple SpriteData files were silently selected.'
    Remove-Item -LiteralPath (Join-Path $source 'GUI\OtherSpriteData.xml') -Force

    $oversizedPath = $spriteDataPath
    $oversized = '<SpriteData>' + (' ' * 4194304) + '</SpriteData>'
    [IO.File]::WriteAllText($oversizedPath, $oversized, (New-Object Text.UTF8Encoding($false)))
    $oversizedOutput = Join-Path $testRoot 'reports\oversized-sprite-data.json'
    $oversizedRun = Invoke-Plan $source $oversizedOutput
    Assert ($oversizedRun.ExitCode -eq 0) 'Oversized SpriteData should be rejected without aborting inventory.'
    $oversizedReport = Get-Content -LiteralPath $oversizedOutput -Raw | ConvertFrom-Json
    Assert ($oversizedReport.spriteData.status -eq 'too-large' -and $oversizedReport.atlasMappings[0].mappingStatus -eq 'sprite-data-too-large') 'Oversized SpriteData was not rejected before parsing.'

    [IO.File]::WriteAllText($spriteDataPath, $validSpriteData, (New-Object Text.UTF8Encoding($false)))

    $planBatch = Join-Path $repositoryRoot 'tools\Plan-CalradiaForge-AssetBatch.bat'
    $batchOutput = Join-Path $testRoot 'reports\batch wrapper report.json'
    $batchCommand = '"' + $planBatch + '" -SourceDirectory "' + $source + '" -OutputJson "' + $batchOutput + '" -EditorDirectory "' + $script:editorFixture + '"'
    $batchOutputText = & $env:ComSpec /d /c $batchCommand 2>&1
    $batchExitCode = $LASTEXITCODE
    Assert ($batchExitCode -eq 0 -and (Test-Path -LiteralPath $batchOutput -PathType Leaf)) "BAT wrapper failed with spaced repository/output paths: $($batchOutputText | Out-String)"
    $batchReport = Get-Content -LiteralPath $batchOutput -Raw | ConvertFrom-Json
    Assert ($batchReport.summary.totalFiles -eq 11 -and $batchReport.importExecuted -eq $false) 'The BAT wrapper did not preserve its quoted arguments or inventory-only mode.'

    $partialBatchOutput = Join-Path $testRoot 'reports\partial-batch.json'
    $partialBatchCommand = '"' + $planBatch + '" -SourceDirectory "' + $source + '" -OutputJson "' + $partialBatchOutput + '" -EditorDirectory "' + $script:editorFixture + '" -MaxDirectoryDepth 1'
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $partialBatchText = & $env:ComSpec /d /c $partialBatchCommand 2>&1
        $partialBatchExitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previousPreference }
    Assert ($partialBatchExitCode -ne 0 -and (Test-Path -LiteralPath $partialBatchOutput -PathType Leaf)) "The BAT wrapper did not preserve the non-zero partial-inventory exit contract: $($partialBatchText | Out-String)"

    $insideOutput = Invoke-Plan $source (Join-Path $source 'asset-batch.json')
    Assert ($insideOutput.ExitCode -ne 0 -and -not (Test-Path -LiteralPath (Join-Path $source 'asset-batch.json'))) 'The planner allowed its report to contaminate the scanned source tree.'
    $boundedOutput = Join-Path $testRoot 'reports\bounded.json'
    $bounded = Invoke-Plan $source $boundedOutput @('-MaxFiles', '2')
    Assert ($bounded.ExitCode -ne 0 -and -not (Test-Path -LiteralPath $boundedOutput)) 'The planner ignored the file-count bound or wrote a partial report.'
    $byteBoundOutput = Join-Path $testRoot 'reports\byte-bound.json'
    $byteBound = Invoke-Plan $source $byteBoundOutput @('-MaxCandidateBytes', '2')
    Assert ($byteBound.ExitCode -ne 0 -and -not (Test-Path -LiteralPath $byteBoundOutput)) 'The planner ignored the hashed-byte bound or wrote a partial report.'
    $directoryBoundOutput = Join-Path $testRoot 'reports\directory-bound.json'
    $directoryBound = Invoke-Plan $source $directoryBoundOutput @('-MaxDirectories', '1')
    Assert ($directoryBound.ExitCode -ne 0 -and -not (Test-Path -LiteralPath $directoryBoundOutput)) 'The planner ignored the directory-count bound or wrote a partial report.'
    $depthBoundOutput = Join-Path $testRoot 'reports\depth-bound.json'
    $depthBound = Invoke-Plan $source $depthBoundOutput @('-MaxDirectoryDepth', '1')
    Assert ($depthBound.ExitCode -ne 0 -and (Test-Path -LiteralPath $depthBoundOutput)) 'The planner did not preserve a partial report when it skipped paths beyond the explicit depth limit.'
    $depthReport = Get-Content -LiteralPath $depthBoundOutput -Raw | ConvertFrom-Json
    Assert ($depthReport.status -eq 'partial' -and $depthReport.summary.warnings -gt 0) 'A depth-limited scan was mislabeled complete.'
    $badExtension = Invoke-Plan $source (Join-Path $testRoot 'reports\bad.txt')
    Assert ($badExtension.ExitCode -ne 0) 'The planner accepted a non-JSON report extension.'

    $overwrite = Invoke-Plan $source $outputPath
    Assert ($overwrite.ExitCode -ne 0) 'The planner overwrote an existing report without -Force.'
    $replace = Invoke-Plan $source $outputPath @('-Force')
    Assert ($replace.ExitCode -eq 0) "Explicit report replacement failed: $($replace.Output)"
    $backups = @(Get-ChildItem -LiteralPath (Split-Path -Parent $outputPath) -Filter 'asset-batch.json.bak.*' -File)
    Assert ($backups.Count -eq 1) 'Explicit report replacement did not preserve exactly one backup.'

    $emptySource = Join-Path $testRoot 'empty-assets'
    New-Item -ItemType Directory -Path $emptySource | Out-Null
    $emptyOutput = Join-Path $testRoot 'reports\empty.json'
    $emptyRun = Invoke-Plan $emptySource $emptyOutput
    Assert ($emptyRun.ExitCode -eq 0) "An empty source tree should produce an empty plan: $($emptyRun.Output)"
    $emptyReport = Get-Content -LiteralPath $emptyOutput -Raw | ConvertFrom-Json
    Assert ($emptyReport.files.Count -eq 0 -and $emptyReport.summary.totalFiles -eq 0) 'The empty plan contains phantom files.'
    Assert ($emptyReport.spriteData.status -eq 'missing' -and $emptyReport.summary.atlasMappingsUnresolved -eq 0) 'An empty tree did not report missing SpriteData accurately.'

    $missingGeneratorOutput = Join-Path $testRoot 'reports\no-generator.json'
    $missingGenerator = Invoke-Plan $source $missingGeneratorOutput @() (Join-Path $testRoot 'missing-editor')
    Assert ($missingGenerator.ExitCode -eq 0) "An unavailable optional generator should not prevent inventory: $($missingGenerator.Output)"
    $missingGeneratorReport = Get-Content -LiteralPath $missingGeneratorOutput -Raw | ConvertFrom-Json
    $unavailableStep = $missingGeneratorReport.recommendedWorkflow | Where-Object id -eq 'generate-ui-sprite-atlas'
    Assert ($unavailableStep.status -eq 'tool-unavailable' -and $unavailableStep.executed -eq $false) 'A missing local generator was not reported as unavailable without failing read-only inventory.'

    $partialEditor = Join-Path $testRoot 'editor-missing-library'
    New-Item -ItemType Directory -Path $partialEditor | Out-Null
    Set-Content -LiteralPath (Join-Path $partialEditor 'TaleWorlds.TwoDimension.SpriteSheetGenerator.exe') -Value 'test fixture; never executed'
    $partialGeneratorOutput = Join-Path $testRoot 'reports\missing-library.json'
    $partialGenerator = Invoke-Plan $source $partialGeneratorOutput @() $partialEditor
    Assert ($partialGenerator.ExitCode -eq 0) 'A missing companion library should affect tool availability, not inventory success.'
    $partialGeneratorReport = Get-Content -LiteralPath $partialGeneratorOutput -Raw | ConvertFrom-Json
    $partialGeneratorStep = $partialGeneratorReport.recommendedWorkflow | Where-Object id -eq 'generate-ui-sprite-atlas'
    Assert ($partialGeneratorReport.toolAvailability.spriteSheetGenerator -eq 'unavailable' -and $partialGeneratorStep.status -eq 'tool-unavailable') 'The planner accepted a generator executable without its required companion library.'
    Assert ($partialGeneratorReport.nextStep -like '*-EditorDirectory*') 'The unavailable-generator guidance omitted how to choose an editor installation.'

    Write-Host 'PASS: bounded inventory, secure SpriteData parsing and atlas mappings, duplicate review, truthful tool availability, read-only source, output guards/backups, and empty plans.'
}
finally {
    $tempRootPath = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    if ($resolvedTestRoot.StartsWith($tempRootPath, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $resolvedTestRoot).StartsWith('CalradiaForge-AssetBatchPlan-', [StringComparison]::Ordinal)) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
