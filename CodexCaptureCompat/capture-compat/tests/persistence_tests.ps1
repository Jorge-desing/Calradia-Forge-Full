$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$maintainer = Join-Path $project 'persistence.ps1'
$sourceDll = Join-Path $project 'dist\version.dll'
$fixtureRoot = Join-Path $project ('validation\persistence-fixture-' + [Guid]::NewGuid().ToString('N'))
$runtimeRoot = Join-Path $fixtureRoot 'runtimes\cua_node'
$stateRoot = Join-Path $fixtureRoot 'managed-state'
$script:Sha256 = [Security.Cryptography.SHA256]::Create()

function Get-Sha256 {
    param([string]$Path)
    $stream = [IO.File]::OpenRead($Path)
    try { return [BitConverter]::ToString($script:Sha256.ComputeHash($stream)).Replace('-', '') }
    finally { $stream.Dispose() }
}

function New-FixtureRuntime {
    param([string]$RuntimeId)
    $targetDirectory = Join-Path $runtimeRoot "$RuntimeId\bin\node_modules\@oai\sky\bin\windows"
    New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    $helper = Join-Path $targetDirectory 'codex-computer-use.exe'
    Set-Content -LiteralPath $helper -Value "fixture helper $RuntimeId" -Encoding ASCII
    return $targetDirectory
}

function New-FixtureInstallRecord {
    param([string]$HelperPath, [string]$HelperHash, [string]$DllHash)
    return [pscustomobject]@{
        schemaVersion = 1
        ownerMarker = 'CodexCaptureCompat.RuntimeWatcher.v1'
        managedBy = 'CodexCaptureCompat-RuntimeWatcher'
        helperPath = [IO.Path]::GetFullPath($HelperPath)
        helperSha256 = $HelperHash
        dllSha256 = $DllHash
        installedAt = '2026-01-01T00:00:00.0000000Z'
    }
}

function Save-FixtureBackup {
    param([string]$Path, [string]$Destination)
    Copy-Item -LiteralPath $Path -Destination $Destination -Force
    $hash = Get-Sha256 -Path $Destination
    if ($hash -ne (Get-Sha256 -Path $Path)) { throw "Fixture backup hash mismatch: $Destination" }
    Set-Content -LiteralPath ($Destination + '.sha256') -Value $hash -Encoding ASCII
    return [pscustomobject]@{ Path = $Destination; Hash = $hash }
}

function New-RuntimeTransactionJournal {
    param(
        [string]$RuntimeId,
        [string]$RuntimeDirectory,
        [string]$DllHash,
        [string]$RecordHash,
        [bool]$PriorDllExists,
        [object]$PriorDllBackup,
        [bool]$PriorRecordExists,
        [object]$PriorRecordBackup
    )
    $helper = Join-Path $RuntimeDirectory 'codex-computer-use.exe'
    $journal = [pscustomobject]@{
        schemaVersion = 1
        ownerMarker = 'CodexCaptureCompat.RuntimeWatcher.v1'
        managedBy = 'CodexCaptureCompat-RuntimeWatcher'
        state = 'DllInstalled'
        runtimeRoot = [IO.Path]::GetFullPath($runtimeRoot)
        runtimeId = $RuntimeId
        targetPath = [IO.Path]::GetFullPath((Join-Path $RuntimeDirectory 'version.dll'))
        recordPath = [IO.Path]::GetFullPath((Join-Path $RuntimeDirectory 'codex-capture-compat.install.json'))
        helperPath = [IO.Path]::GetFullPath($helper)
        helperSha256 = Get-Sha256 -Path $helper
        newDllSha256 = $DllHash
        newRecordSha256 = $RecordHash
        previousDllExists = $PriorDllExists
        previousDllBackup = $PriorDllBackup
        previousRecordExists = $PriorRecordExists
        previousRecordBackup = $PriorRecordBackup
        createdAtUtc = '2026-01-01T00:00:00.0000000Z'
    }
    $transactionDirectory = Join-Path $stateRoot 'transactions'
    New-Item -ItemType Directory -Path $transactionDirectory -Force | Out-Null
    $journalPath = Join-Path $transactionDirectory "runtime-$RuntimeId-fixture.json"
    $encoding = New-Object -TypeName System.Text.UTF8Encoding -ArgumentList $false
    [IO.File]::WriteAllText($journalPath, ($journal | ConvertTo-Json -Depth 8), $encoding)
    return $journalPath
}

function New-PayloadTransactionJournal {
    param([string]$PayloadStateRoot, [string]$PartialDllPath, [string]$PartialDllHash)
    $entries = @(
        [pscustomobject]@{ path = [IO.Path]::GetFullPath($PartialDllPath); priorExists = $false; priorSha256 = $null; priorBackup = $null; newSha256 = $PartialDllHash },
        [pscustomobject]@{ path = [IO.Path]::GetFullPath((Join-Path $PayloadStateRoot 'persistence.ps1')); priorExists = $false; priorSha256 = $null; priorBackup = $null; newSha256 = ('1' * 64) },
        [pscustomobject]@{ path = [IO.Path]::GetFullPath((Join-Path $PayloadStateRoot 'persistence.json')); priorExists = $false; priorSha256 = $null; priorBackup = $null; newSha256 = ('2' * 64) }
    )
    $transactionDirectory = Join-Path $PayloadStateRoot 'transactions'
    New-Item -ItemType Directory -Path $transactionDirectory -Force | Out-Null
    $journal = [pscustomobject]@{
        schemaVersion = 1
        ownerMarker = 'CodexCaptureCompat.RuntimeWatcher.v1'
        managedBy = 'CodexCaptureCompat-RuntimeWatcher'
        stateRoot = [IO.Path]::GetFullPath($PayloadStateRoot)
        state = 'Prepared'
        entries = $entries
        createdAtUtc = '2026-01-01T00:00:00.0000000Z'
    }
    $journalPath = Join-Path $transactionDirectory 'payload-fixture.json'
    $encoding = New-Object -TypeName System.Text.UTF8Encoding -ArgumentList $false
    [IO.File]::WriteAllText($journalPath, ($journal | ConvertTo-Json -Depth 8), $encoding)
    return $journalPath
}

function Get-FixtureTreeSnapshot {
    param([string[]]$Roots)
    $snapshot = New-Object 'System.Collections.Generic.List[string]'
    foreach ($root in $Roots) {
        if (-not (Test-Path -LiteralPath $root -PathType Container)) { continue }
        $fullRoot = (Resolve-Path -LiteralPath $root).Path.TrimEnd('\')
        foreach ($file in @(Get-ChildItem -LiteralPath $fullRoot -File -Force -Recurse | Sort-Object FullName)) {
            $relative = $file.FullName.Substring($fullRoot.Length).TrimStart('\')
            $snapshot.Add("$fullRoot|$relative|$($file.LastWriteTimeUtc.Ticks)|$(Get-Sha256 -Path $file.FullName)")
        }
    }
    return @($snapshot.ToArray())
}

function Assert-ReparsePointAncestorGuardFixture {
    $testRoot = Join-Path $fixtureRoot 'junction-path-test'
    $targetRoot = Join-Path $testRoot 'safe-target'
    $junctionPath = Join-Path $testRoot 'linked-runtime-root'
    $scanRoot = Join-Path $junctionPath 'cua_node'
    $stateFixture = Join-Path $testRoot 'scan-state'
    $candidateDirectory = Join-Path $targetRoot 'cua_node\runtime-behind-junction\bin\node_modules\@oai\sky\bin\windows'
    New-Item -ItemType Directory -Path $candidateDirectory -Force | Out-Null
    New-Item -ItemType Directory -Path $stateFixture -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $candidateDirectory 'codex-computer-use.exe') -Value 'fixture helper behind a junction' -Encoding ASCII
    Set-Content -LiteralPath (Join-Path $candidateDirectory 'marker.txt') -Value 'must remain unchanged' -Encoding ASCII

    try {
        New-Item -ItemType Junction -Path $junctionPath -Target $targetRoot -ErrorAction Stop | Out-Null
    } catch {
        $script:ReparsePointFixtureStatus = "SKIPPED (junction creation unavailable: $($_.Exception.Message))"
        return
    }

    try {
        $junctionItem = Get-Item -LiteralPath $junctionPath -Force
        if (($junctionItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -eq 0) {
            $script:ReparsePointFixtureStatus = 'SKIPPED (created directory was not reported as a reparse point)'
            return
        }

        $before = Get-FixtureTreeSnapshot -Roots @($targetRoot, $stateFixture)
        $blocked = $false
        try {
            & $maintainer -Action ScanOnce -RuntimeRoot $scanRoot -StateRoot $stateFixture -SourceDll $sourceDll -ErrorAction Stop | Out-Null
        } catch {
            if ($_.Exception.Message -match 'reparse point') { $blocked = $true }
            else { throw }
        }
        if (-not $blocked) { throw 'ScanOnce accepted a runtime-root path descending through a junction.' }

        $after = Get-FixtureTreeSnapshot -Roots @($targetRoot, $stateFixture)
        if (($before -join "`n") -cne ($after -join "`n")) { throw 'Rejected junction scan changed target fixture or state bytes.' }
        if (Test-Path -LiteralPath (Join-Path $candidateDirectory 'version.dll')) { throw 'Rejected junction scan wrote version.dll through the junction.' }
        if (Test-Path -LiteralPath (Join-Path $candidateDirectory 'codex-capture-compat.install.json')) { throw 'Rejected junction scan wrote an ownership record through the junction.' }
        $script:ReparsePointFixtureStatus = 'PASS (descendant path rejected before fixture/state writes)'
    } finally {
        # Delete the reparse point itself without traversing it. Its target is a sibling
        # fixture beneath the already-bounded temporary validation directory.
        if (Test-Path -LiteralPath $junctionPath) {
            try { [IO.Directory]::Delete($junctionPath, $false) } catch { }
        }
    }
}

function Import-TaskOwnershipFunctions {
    $tokens = $null
    $parseErrors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($maintainer, [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count -gt 0) { throw "Could not parse persistence script for task-ownership fixtures: $($parseErrors[0].Message)" }
    $functionAsts = @($ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true))
    $required = @('Get-FullPathForComparison', 'Test-TaskUserMatchesCurrentUser', 'ConvertFrom-ScheduledTaskArguments', 'Test-ManagedScheduledTask')
    $definitions = New-Object 'System.Collections.Generic.List[string]'
    foreach ($name in $required) {
        $definition = $functionAsts | Where-Object { [string]::Equals($_.Name, $name, [StringComparison]::Ordinal) } | Select-Object -First 1
        if (-not $definition) { throw "Persistence task-ownership function not found: $name" }
        $definitions.Add($definition.Extent.Text)
    }
    $script:TaskName = 'CodexCaptureCompat-RuntimeWatcher'
    $script:TaskDescription = 'CodexCaptureCompat managed user-level watcher for the Codex Computer Use runtime. It never edits the Codex app or terminates processes.'
    return [scriptblock]::Create($definitions -join [Environment]::NewLine)
}

function Import-CursorRecoveryFunctions {
    $tokens = $null
    $parseErrors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($maintainer, [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count -gt 0) { throw "Could not parse persistence script for cursor-recovery fixtures: $($parseErrors[0].Message)" }
    $functionAsts = @($ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true))
    $required = @('Test-ManagedCuaProcessActive', 'New-CuaCursorRecoveryState', 'Update-CuaCursorRecoveryState', 'Initialize-CursorRecoveryNativeApi', 'Get-SystemCursorVisibility', 'Restore-SystemCursorScheme')
    $definitions = New-Object 'System.Collections.Generic.List[string]'
    foreach ($name in $required) {
        $definition = $functionAsts | Where-Object { [string]::Equals($_.Name, $name, [StringComparison]::Ordinal) } | Select-Object -First 1
        if (-not $definition) { throw "Persistence cursor-recovery function not found: $name" }
        $definitions.Add($definition.Extent.Text)
    }
    return [scriptblock]::Create($definitions -join [Environment]::NewLine)
}

function Assert-CursorRecoveryFixtures {
    . (Import-CursorRecoveryFunctions)
    Initialize-CursorRecoveryNativeApi
    Initialize-CursorRecoveryNativeApi
    if (-not ('CodexCaptureCompat.CursorRecoveryNative' -as [type])) { throw 'Cursor recovery native API type was not loaded under its expected namespace.' }
    $visible = Get-SystemCursorVisibility
    if ($visible -isnot [bool]) { throw 'GetCursorInfo did not return a boolean visibility result.' }
    $restoreScript = (Get-Command Restore-SystemCursorScheme -CommandType Function).ScriptBlock.ToString()
    if ($restoreScript -notmatch '\[CodexCaptureCompat\.CursorRecoveryNative\]::ReloadSystemCursors') { throw 'System cursor restoration does not resolve the fully-qualified native API type.' }
    $maintainerSource = [IO.File]::ReadAllText($maintainer)
    if (-not $maintainerSource.Contains('Filter "Name = ''codex-computer-use.exe''"') -or $maintainerSource -match 'Filter\s+"[^"]*codex-computer-use-swift\.exe') {
        throw 'Watcher process query must track the session helper only, not the long-lived Swift companion.'
    }
    $helperPath = 'C:\fixture\runtime\codex-computer-use.exe'
    $swiftPath = 'C:\fixture\runtime\swift\x64\codex-computer-use-swift.exe'
    $paths = @($helperPath)
    $helper = [pscustomobject]@{ Name = 'codex-computer-use.exe'; ExecutablePath = $helperPath }
    $swift = [pscustomobject]@{ Name = 'codex-computer-use-swift.exe'; ExecutablePath = $swiftPath }
    $foreign = [pscustomobject]@{ Name = 'codex-computer-use.exe'; ExecutablePath = 'C:\other\codex-computer-use.exe' }
    $unresolved = [pscustomobject]@{ Name = 'codex-computer-use.exe'; ExecutablePath = $null }

    if (-not (Test-ManagedCuaProcessActive -ExecutablePaths $paths -ProcessSnapshot @($helper))) { throw 'An exact owned helper path was not recognized as active.' }
    if ($true -eq (Test-ManagedCuaProcessActive -ExecutablePaths $paths -ProcessSnapshot @($swift))) { throw 'The long-lived Swift companion incorrectly kept a closed Computer Use session active.' }
    if ($null -ne (Test-ManagedCuaProcessActive -ExecutablePaths $paths -ProcessSnapshot @($foreign))) { throw 'A same-name process outside the owned runtime was treated as proof of helper exit.' }
    if ($null -ne (Test-ManagedCuaProcessActive -ExecutablePaths $paths -ProcessSnapshot @($unresolved))) { throw 'An unresolved managed process path was treated as proof of exit.' }
    if (Test-ManagedCuaProcessActive -ExecutablePaths $paths -ProcessSnapshot @()) { throw 'An empty process snapshot was treated as active.' }

    # Watcher starts after a session already hid the cursor: preserve that unknown baseline.
    $lateWatcher = New-CuaCursorRecoveryState
    [void](Update-CuaCursorRecoveryState -State $lateWatcher -HelperActive $true -CursorVisible $false)
    if (Update-CuaCursorRecoveryState -State $lateWatcher -HelperActive $false -CursorVisible $false) { throw 'A session already hidden at watcher startup triggered an unsafe reset.' }

    # Watcher observes the cursor visible, then the owned session hides it and exits.
    $normal = New-CuaCursorRecoveryState
    [void](Update-CuaCursorRecoveryState -State $normal -HelperActive $false -CursorVisible $true)
    [void](Update-CuaCursorRecoveryState -State $normal -HelperActive $true -CursorVisible $false)
    if (-not (Update-CuaCursorRecoveryState -State $normal -HelperActive $false -CursorVisible $false)) { throw 'A visible-to-hidden transition was not recovered after the owned session exited.' }
    if (Update-CuaCursorRecoveryState -State $normal -HelperActive $false -CursorVisible $false) { throw 'Cursor recovery was not one-shot.' }

    # Do not reset while the session still owns the cursor, or if it is visible at exit.
    $stillRunning = New-CuaCursorRecoveryState
    [void](Update-CuaCursorRecoveryState -State $stillRunning -HelperActive $false -CursorVisible $true)
    [void](Update-CuaCursorRecoveryState -State $stillRunning -HelperActive $true -CursorVisible $false)
    if (Update-CuaCursorRecoveryState -State $stillRunning -HelperActive $true -CursorVisible $false) { throw 'Cursor recovery ran while the owned session remained active.' }
    $visibleAtExit = New-CuaCursorRecoveryState
    [void](Update-CuaCursorRecoveryState -State $visibleAtExit -HelperActive $false -CursorVisible $true)
    [void](Update-CuaCursorRecoveryState -State $visibleAtExit -HelperActive $true -CursorVisible $false)
    if (Update-CuaCursorRecoveryState -State $visibleAtExit -HelperActive $false -CursorVisible $true) { throw 'A visible cursor at session exit triggered an unnecessary system reset.' }
}

function New-ScheduledTaskFixture {
    param([string]$Arguments, [string]$UserId, [string]$RunLevel = 'Limited', [string]$Description = $script:TaskDescription, [string]$Execute)
    return [pscustomobject]@{
        Description = $Description
        Actions = @([pscustomobject]@{ Execute = $Execute; Arguments = $Arguments })
        Principal = [pscustomobject]@{ UserId = $UserId; RunLevel = $RunLevel }
    }
}

function Assert-TaskOwnershipFixture {
    $taskOwnershipFunctions = Import-TaskOwnershipFunctions
    . $taskOwnershipFunctions
    $expectedScript = Join-Path $fixtureRoot 'managed task fixture\persistence.ps1'
    $powershell = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $expectedArgs = '-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File "{0}" -Action Watch' -f $expectedScript
    $validTask = New-ScheduledTaskFixture -Arguments $expectedArgs -UserId $identity.Name -Execute $powershell
    if (-not (Test-ManagedScheduledTask -Task $validTask -ExpectedScriptPath $expectedScript)) { throw 'Exact generated task action was not recognized as managed.' }

    $quotedCaseVariant = '"-NoLogo" "-NoProfile" "-NonInteractive" "-ExecutionPolicy" "bypass" "-WindowStyle" "hidden" "-File" "{0}" "-Action" "watch"' -f $expectedScript.ToLowerInvariant()
    $variantTask = New-ScheduledTaskFixture -Arguments $quotedCaseVariant -UserId $identity.Name -Execute $powershell.ToUpperInvariant()
    if (-not (Test-ManagedScheduledTask -Task $variantTask -ExpectedScriptPath $expectedScript)) { throw 'Innocent quoting/case variants of the exact managed task were rejected.' }

    $unownedArguments = @(
        @{ Name = 'extra runtime root'; Value = "$expectedArgs -RuntimeRoot `"C:\alternate-runtime`"" },
        @{ Name = 'extra state root'; Value = "$expectedArgs -StateRoot `"C:\alternate-state`"" },
        @{ Name = 'duplicate action'; Value = "$expectedArgs -Action Watch" },
        @{ Name = 'duplicate file'; Value = "$expectedArgs -File `"$expectedScript`"" },
        @{ Name = 'alternate script path'; Value = ($expectedArgs.Replace($expectedScript, (Join-Path $fixtureRoot 'other task\persistence.ps1'))) },
        @{ Name = 'unbalanced quote'; Value = ($expectedArgs + ' "') }
    )
    foreach ($case in $unownedArguments) {
        $task = New-ScheduledTaskFixture -Arguments $case.Value -UserId $identity.Name -Execute $powershell
        if (Test-ManagedScheduledTask -Task $task -ExpectedScriptPath $expectedScript) { throw "Changed task arguments were accepted ($($case.Name))." }
    }
    $wrongUser = New-ScheduledTaskFixture -Arguments $expectedArgs -UserId 'S-1-5-18' -Execute $powershell
    if (Test-ManagedScheduledTask -Task $wrongUser -ExpectedScriptPath $expectedScript) { throw 'Task running as another user was accepted as managed.' }
    $wrongRunLevel = New-ScheduledTaskFixture -Arguments $expectedArgs -UserId $identity.Name -RunLevel 'HighestAvailable' -Execute $powershell
    if (Test-ManagedScheduledTask -Task $wrongRunLevel -ExpectedScriptPath $expectedScript) { throw 'Task with an elevated run level was accepted as managed.' }
}

try {
    if (-not (Test-Path -LiteralPath $sourceDll -PathType Leaf)) { throw "Build the DLL before running the fixture suite: $sourceDll" }
    $script:ReparsePointFixtureStatus = 'NOT RUN'
    Assert-ReparsePointAncestorGuardFixture
    Assert-TaskOwnershipFixture
    Assert-CursorRecoveryFixtures
    $runtimeOne = New-FixtureRuntime -RuntimeId 'runtime-one'
    & $maintainer -Action Install -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $sourceDll -NoScheduledTask
    $stableDll = Join-Path $stateRoot 'version.dll'
    $stableScript = Join-Path $stateRoot 'persistence.ps1'
    $manifestPath = Join-Path $stateRoot 'persistence.json'
    if (-not (Test-Path $stableDll) -or -not (Test-Path $stableScript) -or -not (Test-Path $manifestPath)) {
        throw 'Install did not stage the managed payload and manifest.'
    }
    $sourceHash = Get-Sha256 $sourceDll
    if ((Get-Sha256 $stableDll) -ne $sourceHash) { throw 'Staged DLL hash mismatch.' }
    $partialStateRoot = Join-Path $fixtureRoot 'payload-partial-state'
    $partialRuntimeRoot = Join-Path $fixtureRoot 'payload-partial-runtimes\cua_node'
    $partialDllPath = Join-Path $partialStateRoot 'version.dll'
    New-Item -ItemType Directory -Path $partialStateRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $partialRuntimeRoot -Force | Out-Null
    Copy-Item -LiteralPath $sourceDll -Destination $partialDllPath
    $payloadJournal = New-PayloadTransactionJournal -PayloadStateRoot $partialStateRoot -PartialDllPath $partialDllPath -PartialDllHash $sourceHash
    & $stableScript -Action Install -RuntimeRoot $partialRuntimeRoot -StateRoot $partialStateRoot -SourceDll $sourceDll -NoScheduledTask
    $partialManifest = Get-Content -LiteralPath (Join-Path $partialStateRoot 'persistence.json') -Raw | ConvertFrom-Json
    if ((Get-Sha256 $partialDllPath) -ne $sourceHash -or (Get-Sha256 (Join-Path $partialStateRoot 'persistence.ps1')) -ne $partialManifest.scriptSha256) { throw 'Interrupted payload staging was not recovered and completed with verified hashes.' }
    if (Test-Path -LiteralPath $payloadJournal) { throw 'Recovered payload transaction journal was not cleared.' }
    $oneDll = Join-Path $runtimeOne 'version.dll'
    $oneRecord = Join-Path $runtimeOne 'codex-capture-compat.install.json'
    if ((Get-Sha256 $oneDll) -ne $sourceHash) { throw 'Initial runtime copy hash mismatch.' }
    $firstRecord = Get-Content -LiteralPath $oneRecord -Raw | ConvertFrom-Json
    if ($firstRecord.schemaVersion -ne 1 -or $firstRecord.ownerMarker -ne 'CodexCaptureCompat.RuntimeWatcher.v1' -or $firstRecord.managedBy -ne 'CodexCaptureCompat-RuntimeWatcher') { throw 'Initial install record lacks strict ownership metadata.' }
    if ($firstRecord.helperSha256 -ne (Get-Sha256 (Join-Path $runtimeOne 'codex-computer-use.exe')) -or $firstRecord.dllSha256 -ne $sourceHash) { throw 'Initial install record helper or DLL hash does not match.' }
    $firstInstallTime = $firstRecord.installedAt

    & $stableScript -Action ScanOnce -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $stableDll
    $secondRecord = Get-Content -LiteralPath $oneRecord -Raw | ConvertFrom-Json
    if ($secondRecord.installedAt -ne $firstInstallTime) { throw 'Idempotent scan rewrote an already verified install record.' }

    $runtimeTwo = New-FixtureRuntime -RuntimeId 'runtime-two'
    & $stableScript -Action ScanOnce -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $stableDll
    if ((Get-Sha256 (Join-Path $runtimeTwo 'version.dll')) -ne $sourceHash) { throw 'Newly discovered runtime was not installed.' }

    $foreignRuntime = New-FixtureRuntime -RuntimeId 'runtime-foreign'
    $foreignDll = Join-Path $foreignRuntime 'version.dll'
    Set-Content -LiteralPath $foreignDll -Value 'foreign version.dll - preserve' -Encoding ASCII
    $foreignHash = Get-Sha256 $foreignDll
    & $stableScript -Action ScanOnce -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $stableDll
    if ((Get-Sha256 $foreignDll) -ne $foreignHash) { throw 'A foreign DLL was overwritten.' }
    if (Test-Path (Join-Path $foreignRuntime 'codex-capture-compat.install.json')) { throw 'A foreign DLL was incorrectly adopted.' }

    $unrelated = Join-Path $runtimeRoot 'unrelated\bin\windows'
    New-Item -ItemType Directory -Path $unrelated -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $unrelated 'marker.txt') -Value 'not a Codex CUA runtime' -Encoding ASCII
    & $stableScript -Action ScanOnce -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $stableDll
    if (Test-Path (Join-Path $unrelated 'version.dll')) { throw 'An unrelated directory received a DLL.' }

    # Matching bytes do not authorize adoption: each ownership field is mandatory.
    $strictCases = @(
        @{ RuntimeId = 'strict-no-schema'; WrongField = 'schemaVersion' },
        @{ RuntimeId = 'strict-owner'; WrongField = 'ownerMarker' },
        @{ RuntimeId = 'strict-managed-by'; WrongField = 'managedBy' },
        @{ RuntimeId = 'strict-helper-path'; WrongField = 'helperPath' },
        @{ RuntimeId = 'strict-helper-hash'; WrongField = 'helperSha256' },
        @{ RuntimeId = 'strict-dll-hash'; WrongField = 'dllSha256' }
    )
    $strictSnapshots = @()
    foreach ($case in $strictCases) {
        $directory = New-FixtureRuntime -RuntimeId $case.RuntimeId
        $helper = Join-Path $directory 'codex-computer-use.exe'
        $dll = Join-Path $directory 'version.dll'
        $recordPath = Join-Path $directory 'codex-capture-compat.install.json'
        Copy-Item -LiteralPath $sourceDll -Destination $dll
        $record = New-FixtureInstallRecord -HelperPath $helper -HelperHash (Get-Sha256 $helper) -DllHash $sourceHash
        switch ($case.WrongField) {
            'schemaVersion' { $record.PSObject.Properties.Remove('schemaVersion') }
            'ownerMarker' { $record.ownerMarker = 'OtherTool.v2' }
            'managedBy' { $record.managedBy = 'Another-Watcher' }
            'helperPath' { $record.helperPath = Join-Path $runtimeRoot 'different-helper.exe' }
            'helperSha256' { $record.helperSha256 = 'not-a-sha256-value' }
            'dllSha256' { $record.dllSha256 = ('0' * 64) }
        }
        $encoding = New-Object -TypeName System.Text.UTF8Encoding -ArgumentList $false
        [IO.File]::WriteAllText($recordPath, ($record | ConvertTo-Json -Depth 6), $encoding)
        $strictSnapshots += [pscustomobject]@{ RuntimeId = $case.RuntimeId; DllHash = (Get-Sha256 $dll); RecordHash = (Get-Sha256 $recordPath) }
    }
    $corruptDirectory = New-FixtureRuntime -RuntimeId 'strict-corrupt-record'
    $corruptDll = Join-Path $corruptDirectory 'version.dll'
    $corruptRecordPath = Join-Path $corruptDirectory 'codex-capture-compat.install.json'
    Set-Content -LiteralPath $corruptDll -Value 'foreign DLL with malformed record' -Encoding ASCII
    Set-Content -LiteralPath $corruptRecordPath -Value '{ definitely not json' -Encoding ASCII
    $corruptDllHash = Get-Sha256 $corruptDll
    $corruptRecordHash = Get-Sha256 $corruptRecordPath
    & $stableScript -Action ScanOnce -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $stableDll
    foreach ($snapshot in $strictSnapshots) {
        $directory = Join-Path $runtimeRoot "$($snapshot.RuntimeId)\bin\node_modules\@oai\sky\bin\windows"
        if ((Get-Sha256 (Join-Path $directory 'version.dll')) -ne $snapshot.DllHash) { throw "Unowned DLL changed despite invalid $($snapshot.RuntimeId) metadata." }
        if ((Get-Sha256 (Join-Path $directory 'codex-capture-compat.install.json')) -ne $snapshot.RecordHash) { throw "Unowned record was adopted or rewritten: $($snapshot.RuntimeId)" }
    }
    if ((Get-Sha256 $corruptDll) -ne $corruptDllHash -or (Get-Sha256 $corruptRecordPath) -ne $corruptRecordHash) { throw 'Corrupt or foreign record/DLL was changed.' }

    # Status must inspect corrupt records without creating or changing any state/log files.
    $healthPath = Join-Path $stateRoot 'watcher-health.json'
    $healthFixture = [pscustomobject]@{ schemaVersion = 1; ownerMarker = 'CodexCaptureCompat.RuntimeWatcher.v1'; managedBy = 'CodexCaptureCompat-RuntimeWatcher'; state = 'Degraded'; runtimeRoot = $runtimeRoot; updatedAtUtc = '2026-01-01T00:00:00.0000000Z'; lastSuccessUtc = '2026-01-01T00:00:00.0000000Z'; lastError = 'fixture watcher scan failure'; lastErrorUtc = '2026-01-01T00:00:01.0000000Z'; lastLogError = $null }
    $encoding = New-Object -TypeName System.Text.UTF8Encoding -ArgumentList $false
    [IO.File]::WriteAllText($healthPath, ($healthFixture | ConvertTo-Json -Depth 6), $encoding)
    $beforeStatus = Get-FixtureTreeSnapshot -Roots @($stateRoot, $runtimeRoot)
    $statusOutput = @(& $stableScript -Action Status -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $stableDll) -join "`n"
    if ($statusOutput -notmatch 'Watcher health: Degraded' -or $statusOutput -notmatch 'fixture watcher scan failure') { throw 'Status did not expose persisted watcher health diagnostics.' }
    $afterStatus = Get-FixtureTreeSnapshot -Roots @($stateRoot, $runtimeRoot)
    if (($beforeStatus -join "`n") -cne ($afterStatus -join "`n")) { throw 'Status wrote logs or changed managed state while inspecting a corrupt record.' }

    # Large logs roll over to bounded numbered files and maintenance continues.
    $maintenanceLog = Join-Path $stateRoot 'maintenance.jsonl'
    [IO.File]::WriteAllText($maintenanceLog, ('x' * 1048577), $encoding)
    & $stableScript -Action ScanOnce -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $stableDll | Out-Null
    if (-not (Test-Path -LiteralPath ($maintenanceLog + '.1') -PathType Leaf)) { throw 'Oversized maintenance log was not rotated.' }
    if ((Get-Item -LiteralPath $maintenanceLog).Length -ge 1048576) { throw 'Rotated maintenance log did not return below the configured size threshold.' }

    # Recovery of an owned update restores verified DLL and record backups before scanning.
    $recoverDirectory = New-FixtureRuntime -RuntimeId 'runtime-recover-owned'
    & $stableScript -Action ScanOnce -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $stableDll
    $recoverDll = Join-Path $recoverDirectory 'version.dll'
    $recoverRecordPath = Join-Path $recoverDirectory 'codex-capture-compat.install.json'
    $priorDllHash = Get-Sha256 $recoverDll
    $priorRecordHash = Get-Sha256 $recoverRecordPath
    $backupDirectory = Join-Path $stateRoot 'backups'
    New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
    $priorDllBackup = Save-FixtureBackup -Path $recoverDll -Destination (Join-Path $backupDirectory 'fixture-prior-version.dll')
    $priorRecordBackup = Save-FixtureBackup -Path $recoverRecordPath -Destination (Join-Path $backupDirectory 'fixture-prior-record.json')
    $interruptedDll = Join-Path $fixtureRoot 'interrupted-version.dll'
    Set-Content -LiteralPath $interruptedDll -Value 'interrupted owned runtime update' -Encoding ASCII
    $interruptedHash = Get-Sha256 $interruptedDll
    $newRecord = New-FixtureInstallRecord -HelperPath (Join-Path $recoverDirectory 'codex-computer-use.exe') -HelperHash (Get-Sha256 (Join-Path $recoverDirectory 'codex-computer-use.exe')) -DllHash $interruptedHash
    $newRecordJson = $newRecord | ConvertTo-Json -Depth 6
    $interruptedRecordHash = [BitConverter]::ToString($script:Sha256.ComputeHash((New-Object -TypeName System.Text.UTF8Encoding -ArgumentList $false).GetBytes($newRecordJson))).Replace('-', '')
    Copy-Item -LiteralPath $interruptedDll -Destination $recoverDll -Force
    $ownedJournal = New-RuntimeTransactionJournal -RuntimeId 'runtime-recover-owned' -RuntimeDirectory $recoverDirectory -DllHash $interruptedHash -RecordHash $interruptedRecordHash -PriorDllExists $true -PriorDllBackup $priorDllBackup -PriorRecordExists $true -PriorRecordBackup $priorRecordBackup
    & $stableScript -Action ScanOnce -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $stableDll
    if ((Get-Sha256 $recoverDll) -ne $priorDllHash -or (Get-Sha256 $recoverRecordPath) -ne $priorRecordHash) { throw 'Interrupted owned update did not restore its verified backups.' }
    if (Test-Path -LiteralPath $ownedJournal) { throw 'Successfully recovered owned transaction journal was not cleared.' }

    # Recovery of a first-time install removes only bytes matching the recorded transaction.
    $freshDirectory = New-FixtureRuntime -RuntimeId 'runtime-recover-fresh'
    $freshDll = Join-Path $freshDirectory 'version.dll'
    $freshHelper = Join-Path $freshDirectory 'codex-computer-use.exe'
    $freshInterruptedSource = Join-Path $fixtureRoot 'fresh-interrupted.dll'
    Set-Content -LiteralPath $freshInterruptedSource -Value 'partial first install' -Encoding ASCII
    $freshInterruptedHash = Get-Sha256 $freshInterruptedSource
    $freshRecord = New-FixtureInstallRecord -HelperPath $freshHelper -HelperHash (Get-Sha256 $freshHelper) -DllHash $freshInterruptedHash
    $freshRecordJson = $freshRecord | ConvertTo-Json -Depth 6
    $freshRecordHash = [BitConverter]::ToString($script:Sha256.ComputeHash((New-Object -TypeName System.Text.UTF8Encoding -ArgumentList $false).GetBytes($freshRecordJson))).Replace('-', '')
    Copy-Item -LiteralPath $freshInterruptedSource -Destination $freshDll
    $freshJournal = New-RuntimeTransactionJournal -RuntimeId 'runtime-recover-fresh' -RuntimeDirectory $freshDirectory -DllHash $freshInterruptedHash -RecordHash $freshRecordHash -PriorDllExists $false -PriorDllBackup $null -PriorRecordExists $false -PriorRecordBackup $null
    & $stableScript -Action ScanOnce -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $stableDll
    if ((Get-Sha256 $freshDll) -ne $sourceHash) { throw 'Recovery left a partial first-install DLL or failed to finish a clean retry.' }
    $freshRecordPath = Join-Path $freshDirectory 'codex-capture-compat.install.json'
    $freshInstalledRecord = Get-Content -LiteralPath $freshRecordPath -Raw | ConvertFrom-Json
    if ($freshInstalledRecord.dllSha256 -ne $sourceHash -or $freshInstalledRecord.helperSha256 -ne (Get-Sha256 $freshHelper)) { throw 'Recovery did not finish a verified first-install record.' }
    if (Test-Path -LiteralPath $freshJournal) { throw 'Successfully recovered first-install journal was not cleared.' }

    $upgradeDll = Join-Path $fixtureRoot 'upgraded-version.dll'
    Set-Content -LiteralPath $upgradeDll -Value 'new fixture DLL version' -Encoding ASCII
    $upgradeHash = Get-Sha256 $upgradeDll
    & $stableScript -Action ScanOnce -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $upgradeDll
    if ((Get-Sha256 $oneDll) -ne $upgradeHash) { throw 'Owned installation did not update to the new source.' }
    if ((Get-Sha256 (Join-Path $runtimeTwo 'version.dll')) -ne $upgradeHash) { throw 'Second owned runtime did not update.' }
    if ((Get-Sha256 $foreignDll) -ne $foreignHash) { throw 'Foreign DLL changed during managed upgrade.' }

    $backups = @(Get-ChildItem -LiteralPath (Join-Path $stateRoot 'backups') -Filter 'runtime-one-*-version.dll' -File)
    if ($backups.Count -lt 1) { throw 'Owned DLL update did not preserve a backup.' }
    $backupHash = Get-Sha256 $backups[0].FullName
    if ($backupHash -ne $sourceHash) { throw 'Verified backup does not match the previous DLL.' }
    if ((Get-Content -LiteralPath ($backups[0].FullName + '.sha256') -Raw).Trim() -ne $sourceHash) { throw 'Backup sidecar SHA-256 mismatch.' }

    # An official updater may replace the helper in place while preserving the runtime ID.
    # Reconcile only when the existing DLL still matches a strict owner record, and keep a
    # verified backup before replacing it with the staged compatibility payload.
    $samePathUpdateDirectory = New-FixtureRuntime -RuntimeId 'same-path-helper-update'
    $samePathUpdateHelper = Join-Path $samePathUpdateDirectory 'codex-computer-use.exe'
    $samePathUpdateDll = Join-Path $samePathUpdateDirectory 'version.dll'
    $samePathUpdateRecordPath = Join-Path $samePathUpdateDirectory 'codex-capture-compat.install.json'
    Copy-Item -LiteralPath $sourceDll -Destination $samePathUpdateDll
    $samePathPriorDllHash = Get-Sha256 $samePathUpdateDll
    $samePathPriorHelperHash = Get-Sha256 $samePathUpdateHelper
    $samePathRecord = New-FixtureInstallRecord -HelperPath $samePathUpdateHelper -HelperHash $samePathPriorHelperHash -DllHash $samePathPriorDllHash
    [IO.File]::WriteAllText($samePathUpdateRecordPath, ($samePathRecord | ConvertTo-Json -Depth 6), $encoding)
    Set-Content -LiteralPath $samePathUpdateHelper -Value 'fixture helper after official in-place update' -Encoding ASCII
    $samePathUpdatedHelperHash = Get-Sha256 $samePathUpdateHelper
    & $stableScript -Action ScanOnce -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $upgradeDll
    $samePathUpdatedRecord = Get-Content -LiteralPath $samePathUpdateRecordPath -Raw | ConvertFrom-Json
    if ((Get-Sha256 $samePathUpdateDll) -ne $upgradeHash) { throw 'A helper update at the same runtime path was not reconciled from its matching owned DLL record.' }
    if ($samePathUpdatedRecord.helperSha256 -ne $samePathUpdatedHelperHash -or $samePathUpdatedRecord.dllSha256 -ne $upgradeHash) { throw 'Same-path helper reconciliation did not refresh the verified install record.' }
    $samePathBackups = @(Get-ChildItem -LiteralPath (Join-Path $stateRoot 'backups') -Filter 'same-path-helper-update-*-version.dll' -File)
    if ($samePathBackups.Count -lt 1) { throw 'Same-path helper reconciliation did not create a prior-DLL backup.' }
    $samePathBackupHash = Get-Sha256 $samePathBackups[0].FullName
    if ($samePathBackupHash -ne $samePathPriorDllHash -or (Get-Content -LiteralPath ($samePathBackups[0].FullName + '.sha256') -Raw).Trim() -ne $samePathPriorDllHash) {
        throw 'Same-path helper reconciliation backup or SHA-256 sidecar does not match the previous owned DLL.'
    }

    # A missing owned DLL can be restored from a strict, same-path ownership record after
    # the helper changes. The missing file itself must not prevent restoration.
    $missingDllDirectory = New-FixtureRuntime -RuntimeId 'same-path-helper-missing-dll'
    $missingDllHelper = Join-Path $missingDllDirectory 'codex-computer-use.exe'
    $missingDllRecordPath = Join-Path $missingDllDirectory 'codex-capture-compat.install.json'
    $missingDllPriorHelperHash = Get-Sha256 $missingDllHelper
    $missingDllRecord = New-FixtureInstallRecord -HelperPath $missingDllHelper -HelperHash $missingDllPriorHelperHash -DllHash $sourceHash
    [IO.File]::WriteAllText($missingDllRecordPath, ($missingDllRecord | ConvertTo-Json -Depth 6), $encoding)
    Set-Content -LiteralPath $missingDllHelper -Value 'fixture helper updated while managed DLL was absent' -Encoding ASCII
    $missingDllUpdatedHelperHash = Get-Sha256 $missingDllHelper
    & $stableScript -Action ScanOnce -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $upgradeDll
    $missingDll = Join-Path $missingDllDirectory 'version.dll'
    $missingDllUpdatedRecord = Get-Content -LiteralPath $missingDllRecordPath -Raw | ConvertFrom-Json
    if ((Get-Sha256 $missingDll) -ne $upgradeHash) { throw 'A missing DLL with a strict same-path ownership record was not restored after helper update.' }
    if ($missingDllUpdatedRecord.helperSha256 -ne $missingDllUpdatedHelperHash -or $missingDllUpdatedRecord.dllSha256 -ne $upgradeHash) { throw 'Restoring a missing owned DLL did not refresh its install record.' }

    # A same-path helper update never authorizes overwriting bytes that disagree with the
    # record's DLL hash. Preserve both the unexpected DLL and its record for diagnosis.
    $unexpectedDllDirectory = New-FixtureRuntime -RuntimeId 'same-path-helper-unexpected-dll'
    $unexpectedDllHelper = Join-Path $unexpectedDllDirectory 'codex-computer-use.exe'
    $unexpectedDll = Join-Path $unexpectedDllDirectory 'version.dll'
    $unexpectedDllRecordPath = Join-Path $unexpectedDllDirectory 'codex-capture-compat.install.json'
    Set-Content -LiteralPath $unexpectedDll -Value 'unexpected replacement bytes must remain untouched' -Encoding ASCII
    $unexpectedDllHash = Get-Sha256 $unexpectedDll
    $unexpectedPriorHelperHash = Get-Sha256 $unexpectedDllHelper
    $unexpectedRecord = New-FixtureInstallRecord -HelperPath $unexpectedDllHelper -HelperHash $unexpectedPriorHelperHash -DllHash $sourceHash
    [IO.File]::WriteAllText($unexpectedDllRecordPath, ($unexpectedRecord | ConvertTo-Json -Depth 6), $encoding)
    $unexpectedRecordHash = Get-Sha256 $unexpectedDllRecordPath
    Set-Content -LiteralPath $unexpectedDllHelper -Value 'fixture helper changed but DLL does not match ownership record' -Encoding ASCII
    & $stableScript -Action ScanOnce -RuntimeRoot $runtimeRoot -StateRoot $stateRoot -SourceDll $upgradeDll
    if ((Get-Sha256 $unexpectedDll) -ne $unexpectedDllHash) { throw 'Unexpected DLL bytes were overwritten during same-path helper reconciliation.' }
    if ((Get-Sha256 $unexpectedDllRecordPath) -ne $unexpectedRecordHash) { throw 'Ownership metadata for a mismatched unexpected DLL was rewritten.' }

    'PASS: exact scheduled-task ownership, cursor recovery lifecycle and path scoping, strict install ownership, corrupt/foreign preservation, read-only Status, payload/runtime transaction recovery, bounded log rotation, idempotency, exact-path scope, owned upgrade, verified backups, same-path helper update recovery, missing-DLL restoration, and unexpected-DLL preservation. Reparse-point ancestor guard: ' + $script:ReparsePointFixtureStatus
} finally {
    $validationRoot = Join-Path $project 'validation'
    if ((Test-Path -LiteralPath $fixtureRoot -PathType Container) -and (Test-Path -LiteralPath $validationRoot -PathType Container)) {
        $resolvedRoot = (Resolve-Path -LiteralPath $validationRoot).Path.TrimEnd('\')
        $resolvedFixture = (Resolve-Path -LiteralPath $fixtureRoot).Path
        if ($resolvedFixture.StartsWith($resolvedRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
        } else {
            throw "Refusing to remove persistence fixture outside validation folder: $resolvedFixture"
        }
    }
    $script:Sha256.Dispose()
}
