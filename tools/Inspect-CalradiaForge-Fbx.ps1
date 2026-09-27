[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputJson,

    [ValidateRange(1, 10000)]
    [int]$MaxFiles = 1000,

    [ValidateRange(1, 50000)]
    [int]$MaxDirectories = 10000,

    [ValidateRange(1, 64)]
    [int]$MaxDirectoryDepth = 32,

    [ValidateRange(1, 536870912)]
    [long]$MaxFileBytes = 268435456,

    [ValidateRange(1, 4294967296)]
    [long]$MaxTotalBytes = 2147483648,

    [ValidateRange(1, 50000)]
    [int]$MaxObjectRecordsPerFile = 10000,

    [switch]$Recurse,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$Schema = 'calradiaforge.fbx-preflight/v1'
$BinaryMagic = [byte[]](0x4B, 0x61, 0x79, 0x64, 0x61, 0x72, 0x61, 0x20, 0x46, 0x42, 0x58, 0x20, 0x42, 0x69, 0x6E, 0x61, 0x72, 0x79, 0x20, 0x20, 0x00, 0x1A, 0x00)
$ObjectPattern = [regex]::new('^\s*(?<kind>Model|Geometry|Material):\s*(?<id>-?\d+)\s*,\s*"(?<qualified>(?:\\.|[^"])*)"\s*,\s*"(?<subtype>(?:\\.|[^"])*)"', [Text.RegularExpressions.RegexOptions]::CultureInvariant)
$LodPattern = [regex]::new('^(?<base>.+?)(?:\.lod|_lod)(?<level>\d+)$', [Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [Text.RegularExpressions.RegexOptions]::CultureInvariant)

function Get-RelativePath([string]$Root, [string]$FullName) {
    $relative = $FullName.Substring($Root.Length).TrimStart([char[]]@('\', '/'))
    return $relative.Replace('\', '/')
}

function ConvertFrom-FbxName([string]$QualifiedName) {
    $name = $QualifiedName
    $separator = $name.IndexOf('::', [StringComparison]::Ordinal)
    if ($separator -ge 0) { $name = $name.Substring($separator + 2) }
    return $name.Replace('\"', '"').Replace('\\', '\')
}

function New-Finding([string]$Code, [string]$Severity, [string]$Message) {
    return [pscustomobject]@{ code = $Code; severity = $Severity; message = $Message }
}

function Test-BinaryFbx([string]$Path) {
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        if ($stream.Length -lt $BinaryMagic.Length) { return $false }
        $prefix = New-Object byte[] $BinaryMagic.Length
        $read = $stream.Read($prefix, 0, $prefix.Length)
        if ($read -ne $prefix.Length) { return $false }
        for ($index = 0; $index -lt $BinaryMagic.Length; $index++) {
            if ($prefix[$index] -ne $BinaryMagic[$index]) { return $false }
        }
        return $true
    }
    finally { $stream.Dispose() }
}

function Get-LodGroups([string[]]$MeshNames) {
    $groups = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::OrdinalIgnoreCase)
    $findings = New-Object 'System.Collections.Generic.List[object]'
    $duplicateNames = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $seenNames = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)

    foreach ($meshName in $MeshNames) {
        if (-not $seenNames.Add($meshName)) {
            $duplicateNames.Add($meshName) | Out-Null
        }

        $baseName = $meshName
        $level = $null
        $lodMatch = $LodPattern.Match($meshName)
        if ($lodMatch.Success) {
            $baseName = $lodMatch.Groups['base'].Value
            $parsedLevel = 0
            if ([int]::TryParse($lodMatch.Groups['level'].Value, [ref]$parsedLevel)) { $level = $parsedLevel }
        }

        if (-not $groups.ContainsKey($baseName)) {
            $groups.Add($baseName, [pscustomobject]@{
                BaseName = $baseName
                Names = New-Object 'System.Collections.Generic.List[string]'
                Levels = New-Object 'System.Collections.Generic.List[int]'
                HasUnnumberedBase = $false
            })
        }
        $group = $groups[$baseName]
        $group.Names.Add($meshName)
        if ($null -eq $level) { $group.HasUnnumberedBase = $true }
        else { $group.Levels.Add([int]$level) }
    }

    foreach ($duplicateName in $duplicateNames) {
        $findings.Add((New-Finding 'duplicate-mesh-name' 'review' "Repeated FBX mesh declaration '$duplicateName'; confirm whether the repetition is intentional."))
    }

    $reportedGroups = New-Object 'System.Collections.Generic.List[object]'
    foreach ($group in $groups.Values) {
        if ($group.Levels.Count -eq 0) { continue }
        $uniqueLevels = @($group.Levels | Sort-Object -Unique)
        $missingLevels = New-Object 'System.Collections.Generic.List[int]'
        $firstExpected = if ($uniqueLevels -contains 0) { 0 } else { 1 }
        $lastExpected = [int](($uniqueLevels | Measure-Object -Maximum).Maximum)
        for ($expected = $firstExpected; $expected -le $lastExpected; $expected++) {
            if ($uniqueLevels -notcontains $expected) { $missingLevels.Add($expected) }
        }

        if (-not $group.HasUnnumberedBase -and $uniqueLevels -notcontains 0) {
            $findings.Add((New-Finding 'lod-base-not-declared' 'review' "LOD group '$($group.BaseName)' has numbered variants but no unsuffixed base or explicit LOD 0 declaration."))
        }
        if ($missingLevels.Count -gt 0) {
            $findings.Add((New-Finding 'lod-number-gap' 'review' "LOD group '$($group.BaseName)' skips level(s): $($missingLevels -join ', '). Confirm the intended LOD chain in the editor."))
        }

        $reportedGroups.Add([pscustomobject]@{
            baseName = $group.BaseName
            declaredNames = @($group.Names.ToArray())
            levels = @($uniqueLevels)
            hasUnnumberedBase = [bool]$group.HasUnnumberedBase
            missingLevels = @($missingLevels.ToArray())
            engineImportValidated = $false
        })
    }

    return [pscustomobject]@{ Groups = $reportedGroups.ToArray(); Findings = $findings.ToArray() }
}

function Read-AsciiFbxDeclarations([string]$Path, [string]$RelativePath, [long]$FileLength) {
    $result = [ordered]@{
        relativePath = $RelativePath
        lengthBytes = $FileLength
        format = 'unknown'
        analysisStatus = 'unreadable'
        modelNodes = @()
        meshNameSource = 'none'
        meshNames = @()
        materialNames = @()
        sceneAuxiliaryNodes = @()
        lodGroups = @()
        findings = @()
        limitations = @(
            'This tool reads text declarations only; it does not load, execute, import, or modify the FBX.',
            'Camera/light model declarations are review hints only; intent and the full scene graph are not inferred.',
            'It does not validate geometry, transforms, units, axes, UVs, textures, skin weights, or material availability in Bannerlord.',
            'No finding from this preflight proves that Resource Browser will import the file or produce a valid TPAC.'
        )
    }

    if ($FileLength -gt $MaxFileBytes) {
        $result.analysisStatus = 'unsupported-too-large'
        $result.findings = @((New-Finding 'file-size-limit' 'unavailable' "FBX exceeds the configured per-file limit of $MaxFileBytes bytes."))
        return [pscustomobject]$result
    }

    try {
        if (Test-BinaryFbx $Path) {
            $result.format = 'binary'
            $result.analysisStatus = 'unsupported-binary'
            $result.findings = @((New-Finding 'binary-fbx-not-parsed' 'unavailable' 'Binary FBX is detected but not parsed by this text-only preflight.'))
            return [pscustomobject]$result
        }

        $utf8Strict = New-Object Text.UTF8Encoding($false, $true)
        $stream = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        $reader = New-Object IO.StreamReader($stream, $utf8Strict, $true, 4096, $true)
        $modelNodes = New-Object 'System.Collections.Generic.List[object]'
        $modelMeshNames = New-Object 'System.Collections.Generic.List[string]'
        $geometryMeshNames = New-Object 'System.Collections.Generic.List[string]'
        $materialNames = New-Object 'System.Collections.Generic.List[string]'
        $sceneAuxiliaryNodes = New-Object 'System.Collections.Generic.List[object]'
        $sceneAuxiliaryCounts = [ordered]@{ Camera = 0; Light = 0 }
        $findings = New-Object 'System.Collections.Generic.List[object]'
        $headerFound = $false
        $headerLinesChecked = 0
        $objectRecordCount = 0
        try {
            while (($line = $reader.ReadLine()) -ne $null) {
                if (-not $headerFound -and $headerLinesChecked -lt 20) {
                    $headerLinesChecked++
                    if ($line.TrimStart([char]0xFEFF).TrimStart().StartsWith('; FBX ', [StringComparison]::OrdinalIgnoreCase)) {
                        $headerFound = $true
                    }
                }

                $match = $ObjectPattern.Match($line)
                if (-not $match.Success) { continue }
                $objectRecordCount++
                if ($objectRecordCount -gt $MaxObjectRecordsPerFile) {
                    $result.analysisStatus = 'unsupported-too-many-objects'
                    $result.findings = @((New-Finding 'object-record-limit' 'unavailable' "FBX exceeded the configured object-record limit of $MaxObjectRecordsPerFile."))
                    return [pscustomobject]$result
                }

                $kind = $match.Groups['kind'].Value
                $name = ConvertFrom-FbxName $match.Groups['qualified'].Value
                $subtype = ConvertFrom-FbxName $match.Groups['subtype'].Value
                if ([string]::IsNullOrWhiteSpace($name)) { continue }
                if ($kind -eq 'Model') {
                    $modelNodes.Add([pscustomobject]@{ name = $name; subtype = $subtype; objectId = $match.Groups['id'].Value })
                    if ($subtype -ieq 'Mesh') { $modelMeshNames.Add($name) }
                    elseif ($subtype -ieq 'Camera' -or $subtype -ieq 'Light') {
                        if ($subtype -ieq 'Camera') { $sceneAuxiliaryCounts.Camera++ }
                        else { $sceneAuxiliaryCounts.Light++ }
                        if ($sceneAuxiliaryNodes.Count -lt 16) { $sceneAuxiliaryNodes.Add([pscustomobject]@{ name = $name; subtype = $subtype }) }
                    }
                }
                elseif ($kind -eq 'Geometry' -and $subtype -ieq 'Mesh') {
                    $geometryMeshNames.Add($name)
                }
                elseif ($kind -eq 'Material') {
                    $materialNames.Add($name)
                }
            }
        }
        finally {
            $reader.Dispose()
            $stream.Dispose()
        }

        if (-not $headerFound) {
            $result.format = 'text-or-unknown'
            $result.analysisStatus = 'invalid-or-unsupported-text'
            $result.findings = @((New-Finding 'fbx-ascii-header-missing' 'unavailable' 'The file is not recognized as an FBX ASCII project header; no declarations were trusted.'))
            return [pscustomobject]$result
        }

        $result.format = 'ascii'
        $result.modelNodes = @($modelNodes.ToArray())
        $meshNames = if ($geometryMeshNames.Count -gt 0) { @($geometryMeshNames.ToArray()) } else { @($modelMeshNames.ToArray()) }
        $result.meshNameSource = if ($geometryMeshNames.Count -gt 0) { 'geometry-declarations' } elseif ($modelMeshNames.Count -gt 0) { 'model-node-fallback' } else { 'none' }
        $result.meshNames = @($meshNames)
        $result.materialNames = @($materialNames | Sort-Object -Unique)
        $result.sceneAuxiliaryNodes = @($sceneAuxiliaryNodes.ToArray())
        $result.sceneAuxiliaryNodeCounts = $sceneAuxiliaryCounts
        $sceneAuxiliaryTotal = $sceneAuxiliaryCounts.Camera + $sceneAuxiliaryCounts.Light
        $result.sceneAuxiliaryNodesTruncated = $sceneAuxiliaryTotal -gt $sceneAuxiliaryNodes.Count
        if ($sceneAuxiliaryTotal -gt 0) {
            $sceneKinds = @(@('Camera', 'Light') | Where-Object { $sceneAuxiliaryCounts[$_] -gt 0 } | ForEach-Object { "$_=$($sceneAuxiliaryCounts[$_])" })
            $findings.Add((New-Finding 'scene-objects-review' 'review' ("ASCII model declarations include scene objects outside mesh geometry: " + ($sceneKinds -join ', ') + '. Review their intent; if unintended, export only selected objects. No FBX nodes are removed or rewritten.')))
        }
        if ($result.materialNames.Count -gt 0) {
            $findings.Add((New-Finding 'material-availability-not-checked' 'review' 'Material declarations were listed, but their availability and compatibility in the target module were not checked.'))
        }
        if ($result.meshNames.Count -eq 0) {
            $findings.Add((New-Finding 'no-mesh-declarations-found' 'review' 'No FBX geometry/model records with subtype Mesh were found in the parsed text declarations.'))
        }

        $lod = Get-LodGroups $result.meshNames
        $result.lodGroups = @($lod.Groups)
        foreach ($finding in $lod.Findings) { $findings.Add($finding) }
        $result.findings = @($findings.ToArray())
        $result.analysisStatus = if (@($findings | Where-Object { $_.severity -eq 'review' }).Count -gt 0) { 'analyzed-needs-review' } else { 'analyzed-text-declarations' }
        return [pscustomobject]$result
    }
    catch [Text.DecoderFallbackException] {
        $result.format = 'text-or-unknown'
        $result.analysisStatus = 'unsupported-encoding'
        $result.findings = @((New-Finding 'text-encoding-unsupported' 'unavailable' 'The file is not valid UTF-8/ASCII text with an optional recognized byte-order mark.'))
        return [pscustomobject]$result
    }
    catch {
        $result.analysisStatus = 'unreadable'
        $result.findings = @((New-Finding 'file-read-failed' 'unavailable' "Could not read FBX declarations: $($_.Exception.Message)"))
        return [pscustomobject]$result
    }
}

$inputItem = Get-Item -LiteralPath $InputPath -ErrorAction Stop
if (($inputItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw 'InputPath cannot be a reparse point; choose the real file or directory explicitly.'
}
$inputIsDirectory = [bool]$inputItem.PSIsContainer
if (-not $inputIsDirectory -and [IO.Path]::GetExtension($inputItem.Name) -ine '.fbx') {
    throw 'InputPath must be an .fbx file or a directory containing .fbx files.'
}
$inputRoot = [IO.Path]::GetFullPath($inputItem.FullName).TrimEnd([char[]]@('\', '/'))
$outputPath = [IO.Path]::GetFullPath($OutputJson)
if ([IO.Path]::GetExtension($outputPath) -ine '.json') { throw 'OutputJson must use the .json extension.' }
if ($inputIsDirectory) {
    $inputPrefix = $inputRoot + [IO.Path]::DirectorySeparatorChar
    if ($outputPath.Equals($inputRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $outputPath.StartsWith($inputPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'OutputJson must be outside the scanned directory.'
    }
}
elseif ($outputPath.Equals([IO.Path]::GetFullPath($inputItem.FullName), [StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputJson cannot replace the input FBX.'
}
if ((Test-Path -LiteralPath $outputPath -PathType Leaf) -and -not $Force) {
    throw "Output already exists. Use -Force to replace it with a timestamped backup: $outputPath"
}

$discovered = New-Object 'System.Collections.Generic.List[object]'
$visitedFileCount = 0
$visitedDirectoryCount = 0
$discoveredDirectoryCount = 0
$totalInputBytes = [long]0
$skippedReparsePoints = 0
if ($inputIsDirectory) {
    $pending = New-Object 'System.Collections.Generic.Stack[object]'
    $pending.Push((Get-Item -LiteralPath $inputRoot))
    $discoveredDirectoryCount = 1
    while ($pending.Count -gt 0) {
        $directory = $pending.Pop()
        $visitedDirectoryCount++
        if ($visitedDirectoryCount -gt $MaxDirectories) { throw "Input scan exceeded MaxDirectories=$MaxDirectories; no report was written." }
        $relativeDirectory = Get-RelativePath $inputRoot $directory.FullName
        $depth = if ([string]::IsNullOrEmpty($relativeDirectory)) { 0 } else { @($relativeDirectory -split '/').Count }
        if ($depth -gt $MaxDirectoryDepth) { continue }
        foreach ($entry in $directory.EnumerateFileSystemInfos()) {
            if (($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { $skippedReparsePoints++; continue }
            if (($entry.Attributes -band [IO.FileAttributes]::Directory) -ne 0) {
                if ($Recurse -or $depth -eq 0) {
                    $childDepth = $depth + 1
                    if ($childDepth -le $MaxDirectoryDepth) {
                        $discoveredDirectoryCount++
                        if ($discoveredDirectoryCount -gt $MaxDirectories) { throw "Input scan exceeded MaxDirectories=$MaxDirectories; no report was written." }
                        $pending.Push($entry)
                    }
                }
                continue
            }
            $visitedFileCount++
            if ($visitedFileCount -gt $MaxFiles) { throw "Input scan exceeded MaxFiles=$MaxFiles; no report was written." }
            if ([IO.Path]::GetExtension($entry.Name) -ieq '.fbx') {
                $fileInfo = New-Object IO.FileInfo($entry.FullName)
                $totalInputBytes += [long]$fileInfo.Length
                if ($totalInputBytes -gt $MaxTotalBytes) { throw "FBX inputs exceeded MaxTotalBytes=$MaxTotalBytes; no report was written." }
                $discovered.Add([pscustomobject]@{ Path = $entry.FullName; RelativePath = Get-RelativePath $inputRoot $entry.FullName; Length = [long]$fileInfo.Length; LastWriteUtc = $fileInfo.LastWriteTimeUtc })
            }
        }
    }
}
else {
    $totalInputBytes = [long]$inputItem.Length
    if ($totalInputBytes -gt $MaxTotalBytes) { throw "FBX input exceeded MaxTotalBytes=$MaxTotalBytes; no report was written." }
    $discovered.Add([pscustomobject]@{ Path = $inputItem.FullName; RelativePath = $inputItem.Name; Length = [long]$inputItem.Length; LastWriteUtc = $inputItem.LastWriteTimeUtc })
}

$timer = [Diagnostics.Stopwatch]::StartNew()
$fileResults = New-Object 'System.Collections.Generic.List[object]'
foreach ($input in @($discovered | Sort-Object -Property RelativePath)) {
    $result = Read-AsciiFbxDeclarations $input.Path $input.RelativePath $input.Length
    $latestInfo = Get-Item -LiteralPath $input.Path -ErrorAction SilentlyContinue
    if ($null -eq $latestInfo -or [long]$latestInfo.Length -ne $input.Length -or $latestInfo.LastWriteTimeUtc -ne $input.LastWriteUtc) {
        $result.analysisStatus = 'changed-during-analysis'
        $result.modelNodes = @()
        $result.meshNameSource = 'none'
        $result.meshNames = @()
        $result.materialNames = @()
        $result.sceneAuxiliaryNodes = @()
        $result.sceneAuxiliaryNodeCounts = [ordered]@{ Camera = 0; Light = 0 }
        $result.sceneAuxiliaryNodesTruncated = $false
        $result.lodGroups = @()
        $result.findings = @((New-Finding 'input-changed-during-analysis' 'unavailable' 'File size or timestamp changed while it was being inspected; parsed declarations were discarded.'))
    }
    $fileResults.Add($result)
}
$timer.Stop()

$counts = [ordered]@{
    filesDiscovered = $discovered.Count
    filesAnalyzedAscii = @($fileResults | Where-Object { $_.format -eq 'ascii' -and $_.analysisStatus -in @('analyzed-text-declarations', 'analyzed-needs-review') }).Count
    unsupportedBinary = @($fileResults | Where-Object analysisStatus -eq 'unsupported-binary').Count
    unsupportedTooLarge = @($fileResults | Where-Object analysisStatus -eq 'unsupported-too-large').Count
    invalidOrUnsupportedText = @($fileResults | Where-Object analysisStatus -eq 'invalid-or-unsupported-text').Count
    otherUnavailable = @($fileResults | Where-Object { $_.analysisStatus -in @('unsupported-encoding', 'unsupported-too-many-objects', 'unreadable', 'changed-during-analysis') }).Count
    reviewFindings = @($fileResults | ForEach-Object { $_.findings } | Where-Object severity -eq 'review').Count
}
$report = [ordered]@{
    schema = $Schema
    generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    scope = 'Read-only FBX ASCII declaration inspection. No engine, editor, generator, importer, or external process is launched.'
    input = [ordered]@{
        path = [IO.Path]::GetFullPath($inputItem.FullName)
        kind = if ($inputIsDirectory) { 'directory' } else { 'file' }
        recurse = [bool]$Recurse
        totalFbxBytes = $totalInputBytes
    }
    limits = [ordered]@{
        maxFiles = $MaxFiles
        maxDirectories = $MaxDirectories
        maxDirectoryDepth = $MaxDirectoryDepth
        maxFileBytes = $MaxFileBytes
        maxTotalBytes = $MaxTotalBytes
        maxObjectRecordsPerFile = $MaxObjectRecordsPerFile
    }
    summary = $counts
    measurements = [ordered]@{
        analysisDurationMilliseconds = [long]$timer.ElapsedMilliseconds
        scope = 'Bounded directory inventory plus text declaration parsing only; this is not generator, editor, import, or game performance.'
    }
    importExecuted = $false
    compilerInvoked = $false
    files = $fileResults.ToArray()
    skippedReparsePoints = $skippedReparsePoints
    limitations = @(
        'Binary FBX is detected but not parsed. Only ASCII FBX declarations with a recognizable header are analyzed.',
        'LOD groups are naming hints only. Missing levels are review findings, not proof of invalid geometry or failed import.',
        'Material declarations are listed but not resolved against Bannerlord assets or material definitions.',
        'The preflight does not validate units, axis conventions, mesh dimensions, UVs, textures, skin weights, morphs, or TPAC output.',
        'No result is a guarantee of successful Resource Browser import.'
    )
}

$json = ConvertTo-Json -InputObject $report -Depth 12
$outputDirectory = Split-Path -Parent $outputPath
if ([string]::IsNullOrWhiteSpace($outputDirectory)) { $outputDirectory = (Get-Location).Path }
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$temporaryPath = Join-Path $outputDirectory ('.' + [IO.Path]::GetFileName($outputPath) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
try {
    [IO.File]::WriteAllText($temporaryPath, $json, (New-Object Text.UTF8Encoding($false)))
    if (Test-Path -LiteralPath $outputPath -PathType Leaf) {
        if (-not $Force) { throw "Output already exists: $outputPath" }
        $backupPath = $outputPath + '.bak.' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
        Move-Item -LiteralPath $outputPath -Destination $backupPath
        Write-Host "Backup: $backupPath"
    }
    Move-Item -LiteralPath $temporaryPath -Destination $outputPath
}
finally {
    if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) { Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue }
}

Write-Host "FBX preflight report: $outputPath"
Write-Host "Discovered: $($counts.filesDiscovered); ASCII declarations inspected: $($counts.filesAnalyzedAscii); binary unsupported: $($counts.unsupportedBinary); review findings: $($counts.reviewFindings)."
Write-Host 'Read-only result: no import, compilation, or game/editor validation was performed.'
