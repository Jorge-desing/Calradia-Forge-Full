$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$prepareScript = Join-Path $repositoryRoot 'tools\Prepare-CalradiaForge-ResourceBrowser.ps1'
$inspectScript = Join-Path $repositoryRoot 'tools\Inspect-CalradiaForge-Tpac.ps1'
$inspectBatch = Join-Path $repositoryRoot 'tools\Inspect-CalradiaForge-Tpac.bat'
$sourceModule = Join-Path $repositoryRoot 'modules\CalradiaForge'
$testRoot = Join-Path $env:TEMP ('CalradiaForge-AssetStage-' + [Guid]::NewGuid().ToString('N'))
$expectedFiles = @(
    'AssetSources\GauntletUI\ui_calradiaforge_1.png',
    'GUI\CalradiaForgeSpriteData.xml',
    'GUI\SpriteParts\Config.xml'
)

function Assert([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
}

function Get-Sha256([string]$path) {
    $stream = [IO.File]::OpenRead($path)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '') }
    finally { $algorithm.Dispose(); $stream.Dispose() }
}

function New-TestGame([string]$root, [string]$moduleId) {
    $moduleDirectory = Join-Path $root 'Modules\CalradiaForge'
    New-Item -ItemType Directory -Force -Path $moduleDirectory | Out-Null
    $manifest = '<Module><Id value="' + $moduleId + '"/><Version value="v15.0.0"/></Module>'
    [IO.File]::WriteAllText((Join-Path $moduleDirectory 'SubModule.xml'), $manifest, [Text.Encoding]::UTF8)
    return $moduleDirectory
}

function Invoke-Prepare([string]$root, [string]$moduleSource, [bool]$dryRun, [bool]$collectTpac) {
    $arguments = @{ GameRoot = $root; SourceModulePath = $moduleSource }
    if ($dryRun) { $arguments.DryRun = $true }
    if ($collectTpac) { $arguments.CollectTpac = $true }
    $captured = New-Object 'System.Collections.Generic.List[object]'
    $exitCode = 0
    try {
        & $prepareScript @arguments *>&1 | ForEach-Object { $captured.Add($_) }
    }
    catch {
        $exitCode = 1
        $captured.Add($_.Exception.Message)
    }
    return New-Object psobject -Property @{ ExitCode = $exitCode; Output = ($captured | Out-String) }
}

function Invoke-Inspect([string]$modulePath) {
    $captured = New-Object 'System.Collections.Generic.List[object]'
    $exitCode = 0
    try {
        & $inspectScript -ModulePath $modulePath -ValidateOnly *>&1 |
            ForEach-Object { $captured.Add($_) }
    }
    catch {
        $exitCode = 1
        $captured.Add($_.Exception.Message)
    }
    return New-Object psobject -Property @{ ExitCode = $exitCode; Output = ($captured | Out-String) }
}

function Invoke-InspectStructural([string]$modulePath) {
    $captured = New-Object 'System.Collections.Generic.List[object]'
    $exitCode = 0
    try {
        & $inspectScript -ModulePath $modulePath -ValidateOnly -StructuralOnly *>&1 |
            ForEach-Object { $captured.Add($_) }
    }
    catch {
        $exitCode = 1
        $captured.Add($_.Exception.Message)
    }
    return New-Object psobject -Property @{ ExitCode = $exitCode; Output = ($captured | Out-String) }
}

function Invoke-InspectInstalledBatch([string]$gameRoot) {
    $previousGameRoot = $env:BANNERLORD_GAME_DIR
    $prevEap = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $env:BANNERLORD_GAME_DIR = $gameRoot
        $batchCommand = '"' + $inspectBatch + '" --installed --validate-only --no-pause'
        $output = & $env:ComSpec /d /c $batchCommand 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $prevEap
        if ($null -eq $previousGameRoot) {
            Remove-Item Env:BANNERLORD_GAME_DIR -ErrorAction SilentlyContinue
        }
        else {
            $env:BANNERLORD_GAME_DIR = $previousGameRoot
        }
    }
    return New-Object psobject -Property @{ ExitCode = $exitCode; Output = ($output | Out-String) }
}

foreach ($scriptPath in @($prepareScript, $inspectScript, (Join-Path $repositoryRoot 'tools\package.ps1'))) {
    $tokens = $null
    $parseErrors = $null
    [void][Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$parseErrors)
    Assert ($parseErrors.Count -eq 0) "PowerShell script has syntax errors: $scriptPath $($parseErrors | Out-String)"
}

try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null
    $matchingGame = Join-Path $testRoot 'matching-version-mismatch'
    $matchingModule = New-TestGame $matchingGame 'CalradiaForge'
    $fixtureSourceModule = Join-Path $testRoot 'source-module'
    New-Item -ItemType Directory -Path $fixtureSourceModule | Out-Null
    Copy-Item -LiteralPath (Join-Path $sourceModule 'SubModule.xml') -Destination (Join-Path $fixtureSourceModule 'SubModule.xml')
    foreach ($relativePath in $expectedFiles) {
        $fixturePath = Join-Path $fixtureSourceModule $relativePath
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $fixturePath) | Out-Null
        Copy-Item -LiteralPath (Join-Path $sourceModule $relativePath) -Destination $fixturePath
    }

    $dryRun = Invoke-Prepare $matchingGame $fixtureSourceModule $true $false
    Assert ($dryRun.ExitCode -eq 0) "Dry-run failed: $($dryRun.Output)"
    Assert ($dryRun.Output.Contains('Would stage:')) 'Dry-run did not report planned asset copies.'
    foreach ($relativePath in $expectedFiles) {
        Assert (-not (Test-Path -LiteralPath (Join-Path $matchingModule $relativePath))) "Dry-run wrote a file: $relativePath"
    }

    $stage = Invoke-Prepare $matchingGame $fixtureSourceModule $false $false
    Assert ($stage.ExitCode -eq 0) "Staging failed: $($stage.Output)"
    foreach ($relativePath in $expectedFiles) {
        $sourcePath = Join-Path $fixtureSourceModule $relativePath
        $destinationPath = Join-Path $matchingModule $relativePath
        Assert (Test-Path -LiteralPath $destinationPath -PathType Leaf) "Expected staged file is missing: $relativePath"
        Assert ((Get-Sha256 $sourcePath) -eq (Get-Sha256 $destinationPath)) "Staged hash differs: $relativePath"
    }

    $atlasPath = Join-Path $matchingModule $expectedFiles[0]
    [IO.File]::WriteAllBytes($atlasPath, [byte[]](1, 2, 3, 4))
    $replace = Invoke-Prepare $matchingGame $fixtureSourceModule $false $false
    Assert ($replace.ExitCode -eq 0) "Replacing a staged file failed: $($replace.Output)"
    Assert ((Get-Sha256 (Join-Path $fixtureSourceModule $expectedFiles[0])) -eq (Get-Sha256 $atlasPath)) 'Replacement did not restore the source atlas.'
    $backups = Get-ChildItem -LiteralPath (Split-Path -Parent $atlasPath) -Filter 'ui_calradiaforge_1.png.bak.*' -File
    Assert ($backups.Count -eq 1) 'Replacing a different installed atlas did not create exactly one backup.'

    $wrongGame = Join-Path $testRoot 'wrong-module-id'
    $wrongModule = New-TestGame $wrongGame 'DifferentModule'
    $wrongId = Invoke-Prepare $wrongGame $fixtureSourceModule $true $false
    Assert ($wrongId.ExitCode -ne 0) 'A mismatched installed module ID was accepted.'
    Assert (-not (Test-Path -LiteralPath (Join-Path $wrongModule $expectedFiles[0]))) 'A mismatched module received staged assets.'

    $missingTpac = Invoke-Prepare $matchingGame $fixtureSourceModule $false $true
    Assert ($missingTpac.ExitCode -ne 0) 'Collection succeeded without an imported TPAC.'
    $installedTpac = Join-Path $matchingModule 'Assets\GauntletUI\ui_calradiaforge_1_tex.tpac'
    $sourceTpac = Join-Path $fixtureSourceModule 'Assets\GauntletUI\ui_calradiaforge_1_tex.tpac'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $installedTpac) | Out-Null
    [IO.File]::WriteAllBytes($installedTpac, [Text.Encoding]::ASCII.GetBytes('TPAC-test-payload-one'))
    $markerOnlyTpac = Invoke-Prepare $matchingGame $fixtureSourceModule $false $true
    Assert ($markerOnlyTpac.ExitCode -ne 0) 'A file with only a TPAC marker was accepted as a structurally valid package.'
    Assert (-not (Test-Path -LiteralPath $sourceTpac)) 'A marker-only TPAC was collected into source.'

    $oversizedCountHeader = New-Object byte[] 37
    [Array]::Copy([Text.Encoding]::ASCII.GetBytes('TPAC'), 0, $oversizedCountHeader, 0, 4)
    [Array]::Copy([BitConverter]::GetBytes([UInt32]2), 0, $oversizedCountHeader, 4, 4)
    [Array]::Copy(([Guid]'6F2A2A0B-6062-46C3-BDB4-3E5E8C8E15FA').ToByteArray(), 0, $oversizedCountHeader, 8, 16)
    [Array]::Copy([BitConverter]::GetBytes([UInt32]100001), 0, $oversizedCountHeader, 24, 4)
    [Array]::Copy([BitConverter]::GetBytes([UInt64]1), 0, $oversizedCountHeader, 28, 8)
    [IO.File]::WriteAllBytes($installedTpac, $oversizedCountHeader)
    $oversizedCountTpac = Invoke-Prepare $matchingGame $fixtureSourceModule $false $true
    Assert ($oversizedCountTpac.ExitCode -ne 0) 'A TPAC declaring over 100,000 assets was not rejected before parsing.'

    [IO.File]::WriteAllBytes($installedTpac, [byte[]]@())
    $emptyTpac = Invoke-Prepare $matchingGame $fixtureSourceModule $false $true
    Assert ($emptyTpac.ExitCode -ne 0) 'An empty imported TPAC was accepted.'
    [IO.File]::WriteAllBytes($installedTpac, [Text.Encoding]::ASCII.GetBytes('BAD!-test-payload'))
    $invalidTpac = Invoke-Prepare $matchingGame $fixtureSourceModule $false $true
    Assert ($invalidTpac.ExitCode -ne 0) 'A file without a TPAC header was accepted as an imported package.'
    Assert (-not (Test-Path -LiteralPath $sourceTpac)) 'An invalid package overwrote source.'

    $readerPath = Join-Path $repositoryRoot 'TpacTool\bin\TpacTool.Lib.dll'
    $nativeRoot = if (-not [string]::IsNullOrWhiteSpace($env:BANNERLORD_GAME_DIR)) { $env:BANNERLORD_GAME_DIR } else { Join-Path ${env:ProgramFiles(x86)} 'Steam\steamapps\common\Mount & Blade II Bannerlord' }
    $nativeTpac = Join-Path $nativeRoot 'Modules\Native\AssetPackages\_shared.tpac'
    $deepValidation = 'SKIPPED: TpacTool.Lib.dll or the local Native fixture is unavailable.'
    if ((Test-Path -LiteralPath $readerPath -PathType Leaf) -and (Test-Path -LiteralPath $nativeTpac -PathType Leaf)) {
        Copy-Item -LiteralPath $nativeTpac -Destination $installedTpac -Force
        $collect = Invoke-Prepare $matchingGame $fixtureSourceModule $false $true
        Assert ($collect.ExitCode -eq 0) "TpacTool rejected a known Native TPAC fixture: $($collect.Output)"
        Assert ($collect.Output.Contains('TpacTool metadata validation passed before collection')) 'Collection did not require explicit TpacTool metadata evidence.'
        Assert ((Get-Sha256 $installedTpac) -eq (Get-Sha256 $sourceTpac)) 'Collected TPAC hash differs from the imported package.'
        $collectedHash = Get-Sha256 $sourceTpac

        [IO.File]::WriteAllBytes($installedTpac, [Text.Encoding]::ASCII.GetBytes('TPAC-marker-only'))
        $rejectedReplacement = Invoke-Prepare $matchingGame $fixtureSourceModule $false $true
        Assert ($rejectedReplacement.ExitCode -ne 0) 'A marker-only replacement overwrote a previously validated package.'
        Assert ((Get-Sha256 $sourceTpac) -eq $collectedHash) 'A rejected replacement changed the source package.'
        $tpacBackups = Get-ChildItem -LiteralPath (Split-Path -Parent $sourceTpac) -Filter 'ui_calradiaforge_1_tex.tpac.bak.*' -File
        Assert ($tpacBackups.Count -eq 0) 'A rejected replacement created a source backup, indicating it was written before validation.'

        $inspection = Invoke-Inspect $fixtureSourceModule
        Assert ($inspection.ExitCode -eq 0) "Read-only TPAC preflight failed on Native fixture: $($inspection.Output)"
        Assert ($inspection.Output.Contains('Header/table preflight: PASS') -and $inspection.Output.Contains('Metadata validation: PASS') -and $inspection.Output.Contains('SHA-256:')) 'TPAC preflight did not report bounded v2 table, parsed metadata and package hash.'
        Copy-Item -LiteralPath $nativeTpac -Destination $installedTpac -Force
        $installedInspection = Invoke-InspectInstalledBatch $matchingGame
        Assert ($installedInspection.ExitCode -eq 0) "The --installed BAT mode failed on a known Native package: $($installedInspection.Output)"
        Assert ($installedInspection.Output.Contains('Header/table preflight: PASS') -and $installedInspection.Output.Contains('Metadata validation: PASS')) 'The --installed BAT mode did not inspect the selected game root.'
        [IO.File]::WriteAllBytes($installedTpac, [Text.Encoding]::ASCII.GetBytes('TPAC-truncated'))
        $invalidInstalledInspection = Invoke-InspectInstalledBatch $matchingGame
        Assert ($invalidInstalledInspection.ExitCode -ne 0) 'Read-only TPAC preflight accepted a marker-only package.'

        $malformedMetadata = New-Object byte[] 52
        [Array]::Copy([Text.Encoding]::ASCII.GetBytes('TPAC'), 0, $malformedMetadata, 0, 4)
        [Array]::Copy([BitConverter]::GetBytes([UInt32]2), 0, $malformedMetadata, 4, 4)
        [Array]::Copy(([Guid]'6F2A2A0B-6062-46C3-BDB4-3E5E8C8E15FA').ToByteArray(), 0, $malformedMetadata, 8, 16)
        [Array]::Copy([BitConverter]::GetBytes([UInt32]1), 0, $malformedMetadata, 24, 4)
        [Array]::Copy([BitConverter]::GetBytes([UInt64]16), 0, $malformedMetadata, 28, 8)
        [IO.File]::WriteAllBytes($installedTpac, $malformedMetadata)
        $metadataRejected = Invoke-Inspect $matchingModule
        Assert ($metadataRejected.ExitCode -ne 0) 'Read-only TPAC preflight accepted a header with missing asset metadata.'
        Assert ($metadataRejected.Output.Contains('Header/table preflight: PASS') -and $metadataRejected.Output.Contains('Metadata validation: FAIL') -and $metadataRejected.Output.Contains('Reason:') -and $metadataRejected.Output.Contains('does not prove the package is malformed')) 'Unparseable TPAC metadata did not distinguish bounded header evidence from parser failure.'
        Assert (-not $metadataRejected.Output.Contains('Inspect-CalradiaForge-Tpac.ps1:') -and -not $metadataRejected.Output.Contains('at System.')) 'Malformed TPAC metadata leaked a PowerShell stack trace instead of the concise diagnostic.'

        $sourceHashBeforeRejectedCollection = Get-Sha256 $sourceTpac
        $collectionRejected = Invoke-Prepare $matchingGame $fixtureSourceModule $false $true
        Assert ($collectionRejected.ExitCode -ne 0) 'Collection accepted a TPAC whose asset metadata could not be read.'
        Assert ($collectionRejected.Output.Contains('Collection stopped:') -and $collectionRejected.Output.Contains('Metadata validation: FAIL')) "Collection failure did not preserve the actionable reader diagnostic: $($collectionRejected.Output)"
        Assert ((Get-Sha256 $sourceTpac) -eq $sourceHashBeforeRejectedCollection) 'A metadata parse failure modified the source TPAC.'
        $deepValidation = 'PASS: valid Native package accepted; malformed metadata rejected with recovery steps; collection preserved source.'
    }
    else {
        $invalidInspection = Invoke-Inspect $fixtureSourceModule
        Assert ($invalidInspection.ExitCode -ne 0) 'TPAC preflight passed without a parser or a package.'
    }

    # Verify -StructuralOnly on the repository module
    $structuralSuccess = Invoke-InspectStructural $sourceModule
    Assert ($structuralSuccess.ExitCode -eq 0) "Structural-only inspection failed on repository module: $($structuralSuccess.Output)"
    Assert ($structuralSuccess.Output.Contains('Structural validation: PASS')) 'Structural-only inspection did not report structural pass.'

    # Verify batch runner forwards --structural-only correctly
    $batchCommand = '"' + $inspectBatch + '" --structural-only --no-pause'
    $structuralBatchOut = & $env:ComSpec /d /c $batchCommand 2>&1
    Assert ($LASTEXITCODE -eq 0) "Inspect batch failed with --structural-only: $($structuralBatchOut | Out-String)"
    Assert (($structuralBatchOut | Out-String).Contains('Structural validation: PASS')) 'Inspect batch with --structural-only did not output structural validation pass.'

    # Verify -StructuralOnly still rejects a truncated package
    $badStructuralTpac = Join-Path $matchingModule 'Assets\GauntletUI\ui_calradiaforge_1_tex.tpac'
    [IO.File]::WriteAllBytes($badStructuralTpac, [Text.Encoding]::ASCII.GetBytes('TPAC-truncated'))
    $structuralFailOutput = Invoke-InspectStructural $matchingModule
    Assert ($structuralFailOutput.ExitCode -ne 0) 'Structural inspection accepted a truncated package.'

    Write-Host "PASS: dry-run, staged hashes, version notice, atlas backup, wrong-module rejection, missing/empty/invalid/over-limit TPAC rejection, and source preservation."
    Write-Host "TPAC deep parser: $deepValidation"
}
finally {
    $tempRoot = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    if ($resolvedTestRoot.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $resolvedTestRoot).StartsWith('CalradiaForge-AssetStage-', [StringComparison]::Ordinal)) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
