$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$analyzerScript = Join-Path $repositoryRoot 'tools\Inspect-CalradiaForge-Fbx.ps1'
$analyzerBatch = Join-Path $repositoryRoot 'tools\Inspect-CalradiaForge-Fbx.bat'
$powerShell = Get-Command 'powershell.exe' -ErrorAction SilentlyContinue
if (-not $powerShell) { $powerShell = Get-Command 'pwsh.exe' -ErrorAction SilentlyContinue }
if (-not $powerShell) { throw 'PowerShell is required for FBX preflight fixtures.' }
$testRoot = Join-Path $env:TEMP ('CalradiaForge-FbxPreflight-' + [Guid]::NewGuid().ToString('N'))

function Assert([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }

function Get-TestSha256([string]$path) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($path)
    try { return ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-', '') }
    finally { $stream.Dispose(); $algorithm.Dispose() }
}

function Invoke-Analyzer([string]$inputPath, [string]$outputPath, [string[]]$extra = @(), [switch]$ThroughBatch) {
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        if ($ThroughBatch) {
            $command = '"' + $analyzerBatch + '" -InputPath "' + $inputPath + '" -OutputJson "' + $outputPath + '" ' + ($extra -join ' ') + ' <nul'
            $result = & $env:ComSpec /d /c $command 2>&1
            $exitCode = $LASTEXITCODE
        }
        else {
            $arguments = @('-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', $analyzerScript, '-InputPath', $inputPath, '-OutputJson', $outputPath) + $extra
            $result = & $powerShell.Source @arguments 2>&1
            $exitCode = $LASTEXITCODE
        }
    }
    finally { $ErrorActionPreference = $previousPreference }
    return [pscustomobject]@{ ExitCode = $exitCode; Output = ($result | Out-String) }
}

try {
    $inputDirectory = Join-Path $testRoot 'ASCII model fixtures'
    $reports = Join-Path $testRoot 'reports'
    New-Item -ItemType Directory -Path $inputDirectory, $reports -Force | Out-Null
    $validAscii = @'
; FBX 7.4.0 project file
FBXHeaderExtension:  {
    FBXVersion: 7400
}
Objects:  {
    Geometry: 100, "Geometry::armor", "Mesh" {
    }
    Geometry: 101, "Geometry::armor.lod1", "Mesh" {
    }
    Model: 100, "Model::armor", "Mesh" {
    }
    Material: 200, "Material::cloth", "" {
    }
}
Connections:  {
}
'@
    $validPath = Join-Path $inputDirectory 'armor.fbx'
    [IO.File]::WriteAllText($validPath, $validAscii, (New-Object Text.UTF8Encoding($false)))
    $beforeHash = Get-TestSha256 $validPath

    $validOutput = Join-Path $reports 'valid.json'
    $validRun = Invoke-Analyzer $inputDirectory $validOutput @('-Recurse') -ThroughBatch
    Assert ($validRun.ExitCode -eq 0) "The BAT wrapper failed for a valid ASCII FBX: $($validRun.Output)"
    $validReport = Get-Content -LiteralPath $validOutput -Raw | ConvertFrom-Json
    Assert ($validReport.schema -eq 'calradiaforge.fbx-preflight/v1') 'Unexpected FBX preflight schema.'
    Assert ($validReport.importExecuted -eq $false -and $validReport.compilerInvoked -eq $false) 'The report implied an import or compiler run.'
    Assert ($validReport.summary.filesAnalyzedAscii -eq 1) 'ASCII FBX was not counted as inspected text declarations.'
    Assert ($validReport.files[0].meshNames -contains 'armor' -and $validReport.files[0].meshNames -contains 'armor.lod1') 'Mesh geometry names were not extracted.'
    Assert ($validReport.files[0].meshNameSource -eq 'geometry-declarations') 'Geometry declarations were not preferred for mesh-name checks.'
    Assert (@($validReport.files[0].findings | Where-Object code -eq 'duplicate-mesh-name').Count -eq 0) 'The matching Model/Geometry pair was incorrectly reported as duplicate mesh names.'
    Assert ($validReport.files[0].materialNames -contains 'cloth') 'Material declaration name was not extracted.'
    Assert ($validReport.files[0].lodGroups.Count -eq 1 -and $validReport.files[0].lodGroups[0].hasUnnumberedBase -eq $true -and $validReport.files[0].lodGroups[0].levels -contains 1) 'The base/LOD1 naming group was not reported.'
    Assert ((Get-TestSha256 $validPath) -eq $beforeHash) 'The analyzer changed its input FBX.'

    $scenePath = Join-Path $inputDirectory 'scene-extras.fbx'
    $sceneAscii = @'
; FBX 7.4.0 project file
Objects: {
    Model: 1, "Model::camera_main", "Camera" {
    }
    Model: 2, "Model::key_light", "Light" {
    }
    Geometry: 3, "Geometry::shield", "Mesh" {
    }
}
'@
    [IO.File]::WriteAllText($scenePath, $sceneAscii, (New-Object Text.UTF8Encoding($false)))
    $sceneOutput = Join-Path $reports 'scene-extras.json'
    $sceneRun = Invoke-Analyzer $scenePath $sceneOutput
    Assert ($sceneRun.ExitCode -eq 0) "FBX scene declaration hints should not abort analysis: $($sceneRun.Output)"
    $sceneReport = Get-Content -LiteralPath $sceneOutput -Raw | ConvertFrom-Json
    Assert (@($sceneReport.files[0].sceneAuxiliaryNodes | Where-Object subtype -eq 'Camera').Count -eq 1 -and @($sceneReport.files[0].sceneAuxiliaryNodes | Where-Object subtype -eq 'Light').Count -eq 1) 'Camera/light model declarations were not inventoried.'
    Assert ($sceneReport.files[0].sceneAuxiliaryNodeCounts.Camera -eq 1 -and $sceneReport.files[0].sceneAuxiliaryNodeCounts.Light -eq 1 -and $sceneReport.files[0].sceneAuxiliaryNodesTruncated -eq $false) 'Scene-object totals must include exact observed counts and an explicit bounded-list state.'
    Assert (@($sceneReport.files[0].findings | Where-Object code -eq 'scene-objects-review').Count -eq 1) 'Camera/light objects must produce a review hint, not a pass/fail import verdict.'

    $manyScenePath = Join-Path $inputDirectory 'many-cameras.fbx'
    $manySceneLines = New-Object 'System.Collections.Generic.List[string]'
    $manySceneLines.Add('; FBX 7.4.0 project file')
    $manySceneLines.Add('Objects: {')
    for ($cameraIndex = 0; $cameraIndex -lt 20; $cameraIndex++) {
        $manySceneLines.Add(" Model: $($cameraIndex + 1), `"Model::camera_$cameraIndex`", `"Camera`" { }")
    }
    $manySceneLines.Add('}')
    [IO.File]::WriteAllLines($manyScenePath, $manySceneLines, (New-Object Text.UTF8Encoding($false)))
    $manySceneOutput = Join-Path $reports 'many-cameras.json'
    $manySceneRun = Invoke-Analyzer $manyScenePath $manySceneOutput
    Assert ($manySceneRun.ExitCode -eq 0) "Bounded scene-object inventory should complete: $($manySceneRun.Output)"
    $manySceneReport = Get-Content -LiteralPath $manySceneOutput -Raw | ConvertFrom-Json
    Assert (@($manySceneReport.files[0].sceneAuxiliaryNodes).Count -eq 16 -and $manySceneReport.files[0].sceneAuxiliaryNodeCounts.Camera -eq 20 -and $manySceneReport.files[0].sceneAuxiliaryNodesTruncated -eq $true) 'The scene-object name sample must be bounded while preserving the full bounded record count.'

    $gapPath = Join-Path $inputDirectory 'gaps.fbx'
    $gapAscii = '; FBX 7.4.0 project file' + [Environment]::NewLine + 'Objects: {' + [Environment]::NewLine + ' Geometry: 1, "Geometry::wall.lod2", "Mesh" {' + [Environment]::NewLine + ' }' + [Environment]::NewLine + '}'
    [IO.File]::WriteAllText($gapPath, $gapAscii, (New-Object Text.UTF8Encoding($false)))
    $gapOutput = Join-Path $reports 'gap.json'
    $gapRun = Invoke-Analyzer $gapPath $gapOutput
    Assert ($gapRun.ExitCode -eq 0) "LOD review input should produce evidence instead of aborting: $($gapRun.Output)"
    $gapReport = Get-Content -LiteralPath $gapOutput -Raw | ConvertFrom-Json
    Assert ($gapReport.files[0].analysisStatus -eq 'analyzed-needs-review') 'A suspicious LOD group was reported as clean.'
    Assert (@($gapReport.files[0].findings | Where-Object code -eq 'lod-base-not-declared').Count -eq 1) 'Missing base/LOD0 was not surfaced as advisory review evidence.'
    Assert (@($gapReport.files[0].findings | Where-Object code -eq 'lod-number-gap').Count -eq 1) 'Missing LOD levels were not surfaced.'

    $binaryPath = Join-Path $inputDirectory 'binary.fbx'
    $magic = [byte[]](0x4B, 0x61, 0x79, 0x64, 0x61, 0x72, 0x61, 0x20, 0x46, 0x42, 0x58, 0x20, 0x42, 0x69, 0x6E, 0x61, 0x72, 0x79, 0x20, 0x20, 0x00, 0x1A, 0x00)
    [IO.File]::WriteAllBytes($binaryPath, $magic + [byte[]](1, 2, 3, 4))
    $binaryOutput = Join-Path $reports 'binary.json'
    $binaryRun = Invoke-Analyzer $binaryPath $binaryOutput
    Assert ($binaryRun.ExitCode -eq 0) 'Binary FBX should be reported as unsupported without failing the run.'
    $binaryReport = Get-Content -LiteralPath $binaryOutput -Raw | ConvertFrom-Json
    Assert ($binaryReport.files[0].analysisStatus -eq 'unsupported-binary' -and $binaryReport.summary.unsupportedBinary -eq 1) 'Binary FBX was incorrectly parsed or mislabeled.'

    $invalidPath = Join-Path $inputDirectory 'invalid.fbx'
    [IO.File]::WriteAllText($invalidPath, 'not an FBX document', (New-Object Text.UTF8Encoding($false)))
    $invalidOutput = Join-Path $reports 'invalid.json'
    $invalidRun = Invoke-Analyzer $invalidPath $invalidOutput
    Assert ($invalidRun.ExitCode -eq 0) 'Malformed text should be represented as an unavailable finding.'
    $invalidReport = Get-Content -LiteralPath $invalidOutput -Raw | ConvertFrom-Json
    Assert ($invalidReport.files[0].analysisStatus -eq 'invalid-or-unsupported-text') 'Unrecognized text was accepted as FBX.'

    $largeOutput = Join-Path $reports 'bounded.json'
    $largeRun = Invoke-Analyzer $validPath $largeOutput @('-MaxFileBytes', '8')
    Assert ($largeRun.ExitCode -eq 0) 'An oversized file should be reported without reading its declarations.'
    $largeReport = Get-Content -LiteralPath $largeOutput -Raw | ConvertFrom-Json
    Assert ($largeReport.files[0].analysisStatus -eq 'unsupported-too-large') 'Per-file byte bounds were ignored.'

    $insideOutput = Join-Path $inputDirectory 'report.json'
    $insideRun = Invoke-Analyzer $inputDirectory $insideOutput @('-Recurse')
    Assert ($insideRun.ExitCode -ne 0 -and -not (Test-Path -LiteralPath $insideOutput)) 'The analyzer allowed its report to contaminate the scanned input directory.'

    $wideRoot = Join-Path $testRoot 'wide tree'
    New-Item -ItemType Directory -Path (Join-Path $wideRoot 'child') -Force | Out-Null
    $directoryLimitedOutput = Join-Path $reports 'directory-limit.json'
    $directoryLimitedRun = Invoke-Analyzer $wideRoot $directoryLimitedOutput @('-Recurse', '-MaxDirectories', '1')
    Assert ($directoryLimitedRun.ExitCode -ne 0 -and -not (Test-Path -LiteralPath $directoryLimitedOutput)) 'The analyzer queued more directories than MaxDirectories allowed.'

    Write-Host 'PASS: read-only ASCII FBX declarations, LOD/material and camera/light review hints, binary/malformed/oversize handling, early directory bound, input/output boundaries, and BAT wrapper.'
}
finally {
    $tempRootPath = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    if ($resolvedTestRoot.StartsWith($tempRootPath, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $resolvedTestRoot).StartsWith('CalradiaForge-FbxPreflight-', [StringComparison]::Ordinal)) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
