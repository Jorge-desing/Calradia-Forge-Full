$ErrorActionPreference = 'Stop'
$modulePath = Join-Path $PSScriptRoot '..\tools\TpacInventory.psm1'
$cliPath = Join-Path $PSScriptRoot '..\tools\Compare-CalradiaForge-TpacInventory.ps1'
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixtureRoot = Join-Path $tempRoot ('CalradiaForge-TpacInventoryCompare-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($fixtureRoot) | Out-Null
Import-Module $modulePath -Force

function Assert([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Write-JsonFixture([string]$Path, $Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 8
    [IO.File]::WriteAllText($Path, $json, [Text.UTF8Encoding]::new($false))
}

function Get-FixtureSha256([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-', '') }
    finally { $algorithm.Dispose(); $stream.Dispose() }
}

function New-InventoryFixture([string]$SourcePath, [string]$Hash, [string]$PackageGuid, $Assets, [string]$CreatedUtc = '2026-09-22T00:00:00.0000000Z') {
    return [pscustomobject]@{
        schema = 'calradiaforge.tpac-inventory/v1'
        createdUtc = $CreatedUtc
        reader = [pscustomobject]@{ name = 'TpacTool.Lib'; version = '0.4.0.0' }
        source = [pscustomobject]@{ path = $SourcePath; lengthBytes = 4096; sha256 = $Hash }
        package = [pscustomobject]@{ formatVersion = 2; guid = $PackageGuid; declaredAssetCount = @($Assets).Count; tableOfContentsBytes = 1024 }
        assets = @($Assets)
        scope = 'Fixture metadata only.'
        limits = @('Fixture.')
    }
}

function Assert-InventoryFixtureRejected($Fixture, [string]$ExpectedMessage, [string]$Description) {
    Write-JsonFixture $invalidPath $Fixture
    $failureMessage = ''
    try { Compare-TpacInventory -BeforeReport $beforePath -AfterReport $invalidPath | Out-Null }
    catch { $failureMessage = $_.Exception.Message }
    Assert ($failureMessage.Contains($ExpectedMessage)) ("{0}; got: {1}" -f $Description, $failureMessage)
}

function Invoke-ChildPowerShell([string[]]$Arguments) {
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = Join-Path $PSHOME 'powershell.exe'
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Arguments = (($Arguments | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' }) -join ' ')
    $process = [Diagnostics.Process]::Start($startInfo)
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $output = ($stdoutTask.GetAwaiter().GetResult() + [Environment]::NewLine + $stderrTask.GetAwaiter().GetResult()).Trim()
    $exitCode = $process.ExitCode
    $process.Dispose()
    return [pscustomobject]@{ ExitCode = $exitCode; Output = $output }
}

try {
    $beforePath = Join-Path $fixtureRoot 'before.inventory.json'
    $afterPath = Join-Path $fixtureRoot 'after.inventory.json'
    $outputPath = Join-Path $fixtureRoot 'comparison.json'
    $invalidPath = Join-Path $fixtureRoot 'invalid.inventory.json'
    $assetType = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'

    $beforeAssets = @(
        [pscustomobject]@{ name = 'shared_asset'; guid = '11111111-1111-1111-1111-111111111111'; typeGuid = $assetType; version = 1 },
        [pscustomobject]@{ name = 'removed_asset'; guid = '22222222-2222-2222-2222-222222222222'; typeGuid = $assetType; version = 1 },
        [pscustomobject]@{ name = 'changed_asset'; guid = '33333333-3333-3333-3333-333333333333'; typeGuid = $assetType; version = 1 }
    )
    $afterAssets = @(
        [pscustomobject]@{ name = 'shared_asset'; guid = '11111111-1111-1111-1111-111111111111'; typeGuid = $assetType; version = 1 },
        [pscustomobject]@{ name = 'changed_asset'; guid = '33333333-3333-3333-3333-333333333333'; typeGuid = $assetType; version = 2 },
        [pscustomobject]@{ name = 'added_asset'; guid = '44444444-4444-4444-4444-444444444444'; typeGuid = $assetType; version = 1 }
    )
    Write-JsonFixture $beforePath (New-InventoryFixture 'before.tpac' ('A' * 64) 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb' $beforeAssets '2026-09-21T08:30:00.0000000Z')
    Write-JsonFixture $afterPath (New-InventoryFixture 'after.tpac' ('B' * 64) 'cccccccc-cccc-cccc-cccc-cccccccccccc' $afterAssets '2026-09-22T18:45:00.0000000+02:00')

    $beforeHash = Get-FixtureSha256 $beforePath
    $afterHash = Get-FixtureSha256 $afterPath
    $comparison = Compare-TpacInventory -BeforeReport $beforePath -AfterReport $afterPath
    Assert ($comparison.schema -eq 'calradiaforge.tpac-inventory-diff/v1') 'Diff schema identifier is missing.'
    Assert ($comparison.summary.added -eq 1 -and $comparison.summary.removed -eq 1) 'Added/removed counts are incorrect.'
    Assert ($comparison.summary.changed -eq 1 -and $comparison.summary.unchanged -eq 1) 'Changed/unchanged counts are incorrect.'
    Assert ($comparison.summary.compared -eq 4 -and $comparison.items.Count -eq 3) 'Default diff should report the union count and omit unchanged rows.'
    Assert ($comparison.before.capturedUtc -eq '2026-09-21T08:30:00.0000000Z' -and $comparison.after.capturedUtc -eq '2026-09-22T16:45:00.0000000Z') 'Comparison did not preserve each inventory capture timestamp in UTC.'
    $changed = @($comparison.items | Where-Object { $_.status -eq 'changed' })
    Assert ($changed.Count -eq 1 -and $changed[0].changedFields -contains 'version') 'Changed asset details do not identify the version change.'
    $allRows = Compare-TpacInventory -BeforeReport $beforePath -AfterReport $afterPath -IncludeUnchanged
    Assert ($allRows.items.Count -eq 4 -and @($allRows.items | Where-Object status -eq 'unchanged').Count -eq 1) 'IncludeUnchanged did not include the retained asset.'
    Assert ((Get-FixtureSha256 $beforePath) -eq $beforeHash) 'Before inventory was modified.'
    Assert ((Get-FixtureSha256 $afterPath) -eq $afterHash) 'After inventory was modified.'

    $duplicateAssets = @($afterAssets) + @($afterAssets[0])
    Write-JsonFixture $invalidPath (New-InventoryFixture 'duplicate.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $duplicateAssets)
    $duplicateRejected = $false
    try { Compare-TpacInventory -BeforeReport $beforePath -AfterReport $invalidPath | Out-Null } catch { $duplicateRejected = $true }
    Assert $duplicateRejected 'Duplicate asset GUIDs were accepted.'

    Write-JsonFixture $invalidPath (New-InventoryFixture 'invalid-time.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $afterAssets 'not-a-timestamp')
    $timestampRejected = $false
    try { Compare-TpacInventory -BeforeReport $beforePath -AfterReport $invalidPath | Out-Null } catch { $timestampRejected = $_.Exception.Message.Contains('capture timestamp') }
    Assert $timestampRejected 'An inventory with an invalid capture timestamp was accepted.'

    Write-JsonFixture $invalidPath (New-InventoryFixture 'timezone-less.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $afterAssets '2026-09-22T16:45:00.0000000')
    $timezoneRejected = $false
    try { Compare-TpacInventory -BeforeReport $beforePath -AfterReport $invalidPath | Out-Null } catch { $timezoneRejected = $_.Exception.Message.Contains('explicit time zone') }
    Assert $timezoneRejected 'A parseable timestamp without an explicit time zone was assumed to be UTC.'

    foreach ($invalidSuffix in @("`n", "`r`n")) {
        $description = if ($invalidSuffix -eq "`n") { 'LF' } else { 'CRLF' }
        $invalidTimestamp = '2026-09-22T16:45:00.0000000Z' + $invalidSuffix
        $invalidFixture = New-InventoryFixture 'trailing-linebreak.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $afterAssets $invalidTimestamp
        Assert-InventoryFixtureRejected $invalidFixture 'explicit time zone' "A capture timestamp with trailing $description was accepted"
    }
    $unknownOffset = New-InventoryFixture 'unknown-offset.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $afterAssets '2026-09-22T16:45:00-00:00'
    Assert-InventoryFixtureRejected $unknownOffset 'explicit time zone' 'RFC 3339 unknown local offset -00:00 was treated as known UTC'

    Write-JsonFixture $invalidPath ([pscustomobject]@{ schema = 'wrong/v1'; source = @{}; package = @{}; assets = @() })
    $schemaRejected = $false
    try { Compare-TpacInventory -BeforeReport $beforePath -AfterReport $invalidPath | Out-Null } catch { $schemaRejected = $true }
    Assert $schemaRejected 'Unsupported input schema was accepted.'

    $missingSource = New-InventoryFixture 'missing-source.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $afterAssets
    $missingSource.PSObject.Properties.Remove('source') | Out-Null
    Assert-InventoryFixtureRejected $missingSource "missing required field 'source'" 'Missing source object did not produce a controlled diagnostic'
    $nullSource = New-InventoryFixture 'null-source.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $afterAssets
    $nullSource.source = $null
    Assert-InventoryFixtureRejected $nullSource "field 'source' is null" 'Null source object did not produce a controlled diagnostic'
    $missingHash = New-InventoryFixture 'missing-hash.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $afterAssets
    $missingHash.source.PSObject.Properties.Remove('sha256') | Out-Null
    Assert-InventoryFixtureRejected $missingHash "missing required field 'sha256'" 'Missing nested source hash did not produce a controlled diagnostic'
    $invalidFormat = New-InventoryFixture 'invalid-format.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $afterAssets
    $invalidFormat.package.formatVersion = 'not-an-integer'
    Assert-InventoryFixtureRejected $invalidFormat "unsigned 32-bit field 'package.formatVersion'" 'Invalid package format version did not produce a controlled diagnostic'
    $invalidSourceLength = New-InventoryFixture 'invalid-source-length.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $afterAssets
    $invalidSourceLength.source.lengthBytes = 'not-an-integer'
    Assert-InventoryFixtureRejected $invalidSourceLength 'invalid source length' 'Invalid source length did not produce a controlled diagnostic'
    $oversizedSource = New-InventoryFixture 'oversized-source.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $afterAssets
    $oversizedSource.source.lengthBytes = 134217729
    Assert-InventoryFixtureRejected $oversizedSource '128 MiB limit' 'Source length above the metadata reader limit was accepted'
    $missingToc = New-InventoryFixture 'missing-toc.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $afterAssets
    $missingToc.package.PSObject.Properties.Remove('tableOfContentsBytes') | Out-Null
    Assert-InventoryFixtureRejected $missingToc "missing required field 'tableOfContentsBytes'" 'Missing table-of-contents size was accepted'
    $zeroToc = New-InventoryFixture 'zero-toc.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $afterAssets
    $zeroToc.package.tableOfContentsBytes = 0
    Assert-InventoryFixtureRejected $zeroToc 'table-of-contents range' 'Empty table-of-contents range was accepted'
    $outsideToc = New-InventoryFixture 'outside-toc.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $afterAssets
    $outsideToc.package.tableOfContentsBytes = 4061
    Assert-InventoryFixtureRejected $outsideToc 'table-of-contents range' 'Out-of-file table-of-contents range was accepted'
    $nullAssets = New-InventoryFixture 'null-assets.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' $afterAssets
    $nullAssets.assets = $null
    Assert-InventoryFixtureRejected $nullAssets "field 'assets' is null" 'Null asset collection did not produce a controlled diagnostic'
    $scalarAssets = New-InventoryFixture 'scalar-assets.tpac' ('C' * 64) 'dddddddd-dddd-dddd-dddd-dddddddddddd' @($afterAssets[0])
    $scalarAssets.assets = $afterAssets[0]
    Assert-InventoryFixtureRejected $scalarAssets 'assets field is not a JSON array' 'A scalar asset object was accepted in place of an assets array'

    $inventoryTest = Join-Path $PSScriptRoot 'CalradiaForge.TpacInventory.Tests.ps1'
    $missingReaderDirectory = Join-Path $fixtureRoot 'reader-not-present'
    $strictTest = Invoke-ChildPowerShell @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $inventoryTest, '-RequireReader', '-ToolDirectory', $missingReaderDirectory)
    Assert ($strictTest.ExitCode -ne 0 -and $strictTest.Output.Contains('Strict TpacTool inventory test requires')) 'Strict inventory mode incorrectly treated a missing metadata reader as a passing skip.'

    $cliArguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $cliPath, '-BeforeReport', $beforePath, '-AfterReport', $afterPath, '-OutputJson', $outputPath)
    $cli = Invoke-ChildPowerShell $cliArguments
    Assert ($cli.ExitCode -eq 0 -and (Test-Path -LiteralPath $outputPath -PathType Leaf)) ("CLI did not write the requested JSON report. Exit {0}: {1}" -f $cli.ExitCode, $cli.Output)
    $export = Get-Content -LiteralPath $outputPath -Raw | ConvertFrom-Json
    Assert ($export.summary.added -eq 1 -and $export.items.Count -eq 3) 'Exported diff does not preserve comparison results.'
    Assert ($export.before.capturedUtc -eq '2026-09-21T08:30:00.0000000Z' -and $export.after.capturedUtc -eq '2026-09-22T16:45:00.0000000Z') 'Exported diff lost inventory capture timestamps.'
    Assert ($cli.Output.Contains('Before captured UTC: 2026-09-21T08:30:00.0000000Z') -and $cli.Output.Contains('After captured UTC:  2026-09-22T16:45:00.0000000Z')) 'CLI did not display both inventory capture timestamps.'

    $existingHash = Get-FixtureSha256 $outputPath
    $noOverwrite = Invoke-ChildPowerShell $cliArguments
    Assert ($noOverwrite.ExitCode -eq 2) 'CLI overwrote an existing diff report without -Force.'
    Assert ((Get-FixtureSha256 $outputPath) -eq $existingHash) 'Rejected overwrite changed the existing diff report.'

    $forceArguments = $cliArguments + @('-Force')
    $forced = Invoke-ChildPowerShell $forceArguments
    Assert ($forced.ExitCode -eq 0 -and $forced.Output.Contains('Previous JSON backup:')) 'Explicit -Force did not report a backup.'
    $backups = @(Get-ChildItem -LiteralPath $fixtureRoot -Filter 'comparison.json.bak.*' -File)
    Assert ($backups.Count -eq 1) 'Forced replacement did not preserve exactly one backup.'

    $samePathArguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $cliPath, '-BeforeReport', $beforePath, '-AfterReport', $afterPath, '-OutputJson', $beforePath, '-Force')
    $samePath = Invoke-ChildPowerShell $samePathArguments
    Assert ($samePath.ExitCode -eq 2) 'CLI accepted an input report as its output path.'
    Assert ((Get-FixtureSha256 $beforePath) -eq $beforeHash) 'Same-path rejection modified the input report.'

    Write-Host 'PASS: TPAC inventory diff counts, captured UTC timestamps, strict timestamp syntax, structural and numeric report validation, v2 TOC and 128 MiB bounds, JSON array shape, optional unchanged rows, duplicate rejection, read-only inputs, JSON export, no-overwrite, backups, and same-path protection.'
}
finally {
    Remove-Module TpacInventory -ErrorAction SilentlyContinue
    $resolvedRoot = [IO.Path]::GetFullPath($fixtureRoot)
    if ($resolvedRoot.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -and [IO.Directory]::Exists($resolvedRoot)) {
        [IO.Directory]::Delete($resolvedRoot, $true)
    }
}
