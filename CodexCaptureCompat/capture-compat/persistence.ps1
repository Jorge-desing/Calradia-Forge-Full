[CmdletBinding()]
param(
    [ValidateSet('Install', 'Status', 'Uninstall', 'Watch', 'ScanOnce')]
    [string]$Action = 'Status',
    [string]$RuntimeRoot = (Join-Path $env:LOCALAPPDATA 'OpenAI\Codex\runtimes\cua_node'),
    [string]$StateRoot = (Join-Path $env:LOCALAPPDATA 'OpenAI\Codex\CodexCaptureCompat'),
    [string]$SourceDll,
    [switch]$NoScheduledTask
)

$ErrorActionPreference = 'Stop'
$script:OwnerMarker = 'CodexCaptureCompat.RuntimeWatcher.v1'
$script:TaskName = 'CodexCaptureCompat-RuntimeWatcher'
$script:TaskDescription = 'CodexCaptureCompat managed user-level watcher for the Codex Computer Use runtime. It never edits the Codex app or terminates processes.'
$script:LogPath = Join-Path $StateRoot 'maintenance.jsonl'
$script:HealthPath = Join-Path $StateRoot 'watcher-health.json'
$script:MaxLogBytes = 1048576
$script:LogRetention = 3
$script:LoggedEntries = New-Object 'System.Collections.Generic.HashSet[string]'

function Get-FileSha256 {
    param([Parameter(Mandatory)][string]$Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
    try { return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
    finally { $stream.Dispose(); $sha.Dispose() }
}

function Get-TextSha256 {
    param([Parameter(Mandatory)][string]$Text)
    $sha = [Security.Cryptography.SHA256]::Create()
    $encoding = New-Object -TypeName System.Text.UTF8Encoding -ArgumentList $false
    try { return [BitConverter]::ToString($sha.ComputeHash($encoding.GetBytes($Text))).Replace('-', '') }
    finally { $sha.Dispose() }
}

function Test-Sha256String {
    param([AllowNull()][string]$Hash)
    return ($Hash -cmatch '^[0-9A-Fa-f]{64}$')
}

function Get-FullPathForComparison {
    param([Parameter(Mandatory)][string]$Path)
    return [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Test-PathWithin {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$Root)
    $fullPath = Get-FullPathForComparison -Path $Path
    $fullRoot = Get-FullPathForComparison -Path $Root
    return $fullPath.StartsWith($fullRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

function Replace-FileAtomically {
    param([Parameter(Mandatory)][string]$Temporary, [Parameter(Mandatory)][string]$Destination)
    $replacementBackup = "$Destination.replace.$([Guid]::NewGuid().ToString('N'))"
    try {
        [IO.File]::Replace($Temporary, $Destination, $replacementBackup)
    } finally {
        if (Test-Path -LiteralPath $replacementBackup -PathType Leaf) {
            Remove-Item -LiteralPath $replacementBackup -Force -ErrorAction SilentlyContinue
        }
    }
}

function Write-MaintenanceLog {
    param([string]$Event, [string]$Path, [string]$Detail)
    $dedupeKey = "$Event`n$Path`n$Detail"
    if ($script:LoggedEntries.Contains($dedupeKey)) { return }
    try {
        if (-not (Test-Path -LiteralPath $StateRoot -PathType Container)) {
            New-Item -ItemType Directory -Path $StateRoot -Force | Out-Null
        }
        if (Test-ReparsePoint -Path $StateRoot) { throw 'Refusing to write maintenance data through a reparse-point state directory.' }
        if (Test-ReparsePoint -Path $script:LogPath) { throw 'Refusing to write maintenance data through a reparse-point log file.' }
        if ((Test-Path -LiteralPath $script:LogPath -PathType Leaf) -and (Get-Item -LiteralPath $script:LogPath -Force).Length -ge $script:MaxLogBytes) {
            for ($index = $script:LogRetention; $index -ge 1; $index--) {
                $source = if ($index -eq 1) { $script:LogPath } else { "$($script:LogPath).$($index - 1)" }
                $destination = "$($script:LogPath).$index"
                if (Test-Path -LiteralPath $destination -PathType Leaf) { Remove-Item -LiteralPath $destination -Force }
                if (Test-Path -LiteralPath $source -PathType Leaf) { Move-Item -LiteralPath $source -Destination $destination -Force }
            }
        }
        $entry = [pscustomobject]@{
            time = (Get-Date).ToUniversalTime().ToString('o')
            event = $Event
            path = $Path
            detail = $Detail
        } | ConvertTo-Json -Compress
        Add-Content -LiteralPath $script:LogPath -Value $entry -Encoding UTF8
        [void]$script:LoggedEntries.Add($dedupeKey)
        if ($script:LoggedEntries.Count -gt 1024) { $script:LoggedEntries.Clear() }
        $script:LastLogError = $null
    } catch {
        # Logging must never terminate the watcher or obscure the original operation.
        $script:LastLogError = $_.Exception.Message
    }
}

function Set-WatcherHealth {
    param(
        [Parameter(Mandatory)][string]$State,
        [Parameter(Mandatory)][string]$Root,
        [AllowNull()][string]$LastSuccessUtc,
        [AllowNull()][string]$LastError,
        [AllowNull()][string]$LastErrorUtc
    )
    try {
        if (-not (Test-Path -LiteralPath $StateRoot -PathType Container)) { New-Item -ItemType Directory -Path $StateRoot -Force | Out-Null }
        if (Test-ReparsePoint -Path $StateRoot) { throw 'Refusing to write watcher health through a reparse-point state directory.' }
        Write-JsonAtomically -Path $script:HealthPath -Value ([pscustomobject]@{
            schemaVersion = 1
            ownerMarker = $script:OwnerMarker
            managedBy = $script:TaskName
            state = $State
            runtimeRoot = [IO.Path]::GetFullPath($Root)
            updatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
            lastSuccessUtc = $LastSuccessUtc
            lastError = $LastError
            lastErrorUtc = $LastErrorUtc
            lastLogError = $script:LastLogError
        })
    } catch { $script:LastHealthError = $_.Exception.Message }
}

function Test-ReparsePoint {
    param([Parameter(Mandatory)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $item = Get-Item -LiteralPath $Path -Force
    return (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)
}

function Test-PathChainHasReparsePoint {
    param([Parameter(Mandatory)][string]$Path)
    $current = [IO.Path]::GetFullPath($Path)
    while ($current) {
        if ((Test-Path -LiteralPath $current) -and (Test-ReparsePoint -Path $current)) { return $true }
        $parent = [IO.Directory]::GetParent($current)
        if (-not $parent -or [string]::Equals($parent.FullName, $current, [StringComparison]::OrdinalIgnoreCase)) { break }
        $current = $parent.FullName
    }
    return $false
}

function Get-RuntimeCandidates {
    param([Parameter(Mandatory)][string]$Root)
    if (-not (Test-Path -LiteralPath $Root -PathType Container)) { return @() }
    if (Test-PathChainHasReparsePoint -Path $Root) {
        throw "Refusing to follow a reparse point in the runtime-root path chain: $Root"
    }
    $rootItem = Get-Item -LiteralPath $Root -Force
    if (($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Refusing to follow a reparse point at the runtime root: $Root"
    }
    $parts = @('bin', 'node_modules', '@oai', 'sky', 'bin', 'windows')
    $candidates = New-Object System.Collections.Generic.List[object]
    foreach ($runtime in @(Get-ChildItem -LiteralPath $rootItem.FullName -Directory -Force -ErrorAction SilentlyContinue)) {
        if (($runtime.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { continue }
        $cursor = $runtime.FullName
        $safe = $true
        foreach ($part in $parts) {
            $cursor = Join-Path $cursor $part
            if (-not (Test-Path -LiteralPath $cursor -PathType Container)) { $safe = $false; break }
            if (Test-ReparsePoint -Path $cursor) { $safe = $false; break }
        }
        if (-not $safe) { continue }
        $helper = Join-Path $cursor 'codex-computer-use.exe'
        if (-not (Test-Path -LiteralPath $helper -PathType Leaf)) { continue }
        if (Test-ReparsePoint -Path $helper) { continue }
        $candidates.Add([pscustomobject]@{
            RuntimeId = $runtime.Name
            Directory = $cursor
            HelperPath = [IO.Path]::GetFullPath($helper)
        })
    }
    return $candidates.ToArray()
}

function Write-JsonAtomically {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)]$Value,
        [string]$JsonText
    )
    if (Test-ReparsePoint -Path $Path) { throw "Refusing to replace a reparse-point JSON file: $Path" }
    $temporary = "$Path.tmp.$([Guid]::NewGuid().ToString('N'))"
    $json = if ($PSBoundParameters.ContainsKey('JsonText')) { $JsonText } else { $Value | ConvertTo-Json -Depth 6 }
    $encoding = New-Object -TypeName System.Text.UTF8Encoding -ArgumentList $false
    try {
        [IO.File]::WriteAllText($temporary, $json, $encoding)
        if (Test-Path -LiteralPath $Path -PathType Leaf) {
            Replace-FileAtomically -Temporary $temporary -Destination $Path
        } else {
            [IO.File]::Move($temporary, $Path)
        }
    } finally {
        if (Test-Path -LiteralPath $temporary -PathType Leaf) { Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue }
    }
}

function Save-VerifiedBackup {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$RuntimeId)
    if (Test-ReparsePoint -Path $StateRoot) { throw "Refusing to write backups through a reparse-point state directory: $StateRoot" }
    $backupDirectory = Join-Path $StateRoot 'backups'
    New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
    if (Test-ReparsePoint -Path $backupDirectory) { throw "Refusing a reparse-point backup directory: $backupDirectory" }
    $stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmmss-fff')
    $backup = Join-Path $backupDirectory "$RuntimeId-$stamp-$([IO.Path]::GetFileName($Path))"
    Copy-Item -LiteralPath $Path -Destination $backup
    $sourceHash = Get-FileSha256 -Path $Path
    $backupHash = Get-FileSha256 -Path $backup
    if ($sourceHash -ne $backupHash) {
        Remove-Item -LiteralPath $backup -Force -ErrorAction SilentlyContinue
        throw "Backup hash mismatch for $Path"
    }
    Set-Content -LiteralPath "$backup.sha256" -Value $backupHash -Encoding ASCII
    return [pscustomobject]@{ Path = $backup; Hash = $backupHash }
}

function Set-VerifiedFile {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination,
        [Parameter(Mandatory)][string]$ExpectedHash
    )
    $temporary = "$Destination.tmp.$([Guid]::NewGuid().ToString('N'))"
    try {
        Copy-Item -LiteralPath $Source -Destination $temporary
        if ((Get-FileSha256 -Path $temporary) -ne $ExpectedHash) { throw "Staged file hash mismatch for $Destination" }
        if (Test-Path -LiteralPath $Destination -PathType Leaf) {
            Replace-FileAtomically -Temporary $temporary -Destination $Destination
        } else {
            [IO.File]::Move($temporary, $Destination)
        }
        if ((Get-FileSha256 -Path $Destination) -ne $ExpectedHash) { throw "Installed file hash mismatch for $Destination" }
    } finally {
        if (Test-Path -LiteralPath $temporary -PathType Leaf) { Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue }
    }
}

function Test-RecordOwnsDll {
    param([object]$Record, [string]$HelperPath, [string]$HelperHash, [string]$DllHash)
    if (-not (Test-RecordMetadata -Record $Record -HelperPath $HelperPath -HelperHash $HelperHash)) { return $false }
    return [string]::Equals([string]$Record.dllSha256, $DllHash, [StringComparison]::OrdinalIgnoreCase)
}

function Test-RecordIdentity {
    param([object]$Record, [string]$HelperPath)
    if (-not $Record) { return $false }
    if ([int]$Record.schemaVersion -ne 1) { return $false }
    if (-not [string]::Equals([string]$Record.ownerMarker, $script:OwnerMarker, [StringComparison]::Ordinal)) { return $false }
    if (-not [string]::Equals([string]$Record.managedBy, $script:TaskName, [StringComparison]::Ordinal)) { return $false }
    try {
        if (-not [string]::Equals((Get-FullPathForComparison -Path ([string]$Record.helperPath)), (Get-FullPathForComparison -Path $HelperPath), [StringComparison]::OrdinalIgnoreCase)) { return $false }
    } catch { return $false }
    if (-not (Test-Sha256String -Hash ([string]$Record.helperSha256))) { return $false }
    if (-not (Test-Sha256String -Hash ([string]$Record.dllSha256))) { return $false }
    return $true
}

function Test-RecordMetadata {
    param([object]$Record, [string]$HelperPath, [string]$HelperHash)
    if (-not (Test-RecordIdentity -Record $Record -HelperPath $HelperPath)) { return $false }
    return [string]::Equals([string]$Record.helperSha256, $HelperHash, [StringComparison]::OrdinalIgnoreCase)
}

function Read-InstallRecord {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    try { return (Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -ErrorAction Stop) }
    catch { return $null }
}

function New-InstallRecord {
    param([string]$HelperPath, [string]$DllHash)
    return [pscustomobject]@{
        schemaVersion = 1
        ownerMarker = $script:OwnerMarker
        managedBy = $script:TaskName
        helperPath = $HelperPath
        helperSha256 = (Get-FileSha256 -Path $HelperPath)
        dllSha256 = $DllHash
        installedAt = (Get-Date).ToUniversalTime().ToString('o')
    }
}

function Get-InstallRecordJson {
    param([Parameter(Mandatory)][string]$HelperPath, [Parameter(Mandatory)][string]$DllHash)
    return (New-InstallRecord -HelperPath $HelperPath -DllHash $DllHash | ConvertTo-Json -Depth 6)
}

function Get-TransactionRoot {
    if (Test-ReparsePoint -Path $StateRoot) { throw "Refusing to write transactions through a reparse-point state directory: $StateRoot" }
    $directory = Join-Path $StateRoot 'transactions'
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
    if (Test-ReparsePoint -Path $directory) { throw "Refusing a reparse-point transaction directory: $directory" }
    return $directory
}

function Test-VerifiedBackup {
    param([object]$Backup, [string]$BackupRoot)
    if (-not $Backup -or -not $Backup.Path -or -not (Test-Sha256String -Hash ([string]$Backup.Hash))) { return $false }
    if (-not (Test-PathWithin -Path ([string]$Backup.Path) -Root $BackupRoot)) { return $false }
    if (-not (Test-Path -LiteralPath ([string]$Backup.Path) -PathType Leaf)) { return $false }
    try { return [string]::Equals((Get-FileSha256 -Path ([string]$Backup.Path)), [string]$Backup.Hash, [StringComparison]::OrdinalIgnoreCase) }
    catch { return $false }
}

function Restore-RuntimeTransaction {
    param([Parameter(Mandatory)][string]$JournalPath, [Parameter(Mandatory)][string]$RuntimeRootPath)
    $journal = Get-Content -LiteralPath $JournalPath -Raw | ConvertFrom-Json -ErrorAction Stop
    if ([int]$journal.schemaVersion -ne 1 -or
        -not [string]::Equals([string]$journal.ownerMarker, $script:OwnerMarker, [StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$journal.managedBy, $script:TaskName, [StringComparison]::Ordinal)) {
        throw "Transaction journal is not owned by this tool: $JournalPath"
    }
    $fullRoot = Get-FullPathForComparison -Path $RuntimeRootPath
    if (-not [string]::Equals((Get-FullPathForComparison -Path ([string]$journal.runtimeRoot)), $fullRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Transaction journal runtime root does not match the requested root: $JournalPath"
    }
    if ([string]$journal.state -notin @('Prepared', 'DllInstalled', 'Committed')) { throw "Transaction journal has an unknown state: $JournalPath" }
    if (-not (Test-Sha256String -Hash ([string]$journal.helperSha256))) { throw "Transaction journal contains a malformed helper hash: $JournalPath" }
    $target = [IO.Path]::GetFullPath([string]$journal.targetPath)
    $recordPath = [IO.Path]::GetFullPath([string]$journal.recordPath)
    $helperPath = [IO.Path]::GetFullPath([string]$journal.helperPath)
    if (-not (Test-PathWithin -Path $target -Root $fullRoot) -or
        -not (Test-PathWithin -Path $recordPath -Root $fullRoot) -or
        -not (Test-PathWithin -Path $helperPath -Root $fullRoot)) {
        throw "Transaction journal contains a path outside its runtime root: $JournalPath"
    }
    $candidate = @(Get-RuntimeCandidates -Root $RuntimeRootPath | Where-Object { [string]::Equals([string]$_.RuntimeId, [string]$journal.runtimeId, [StringComparison]::OrdinalIgnoreCase) }) | Select-Object -First 1
    if (-not $candidate -or
        -not [string]::Equals($target, [IO.Path]::GetFullPath((Join-Path $candidate.Directory 'version.dll')), [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals($recordPath, [IO.Path]::GetFullPath((Join-Path $candidate.Directory 'codex-capture-compat.install.json')), [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals($helperPath, [IO.Path]::GetFullPath($candidate.HelperPath), [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals((Get-FileSha256 -Path $helperPath), [string]$journal.helperSha256, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Transaction journal paths or helper hash do not match a current runtime candidate: $JournalPath"
    }
    if (-not (Test-Sha256String -Hash ([string]$journal.newDllSha256)) -or
        -not (Test-Sha256String -Hash ([string]$journal.newRecordSha256))) {
        throw "Transaction journal contains malformed hashes: $JournalPath"
    }
    if ([string]$journal.state -eq 'Committed') {
        Remove-Item -LiteralPath $JournalPath -Force
        return 'removed-committed'
    }

    $backupRoot = Join-Path $StateRoot 'backups'
    if ([bool]$journal.previousDllExists) {
        if (-not (Test-VerifiedBackup -Backup $journal.previousDllBackup -BackupRoot $backupRoot)) { throw "Prior DLL backup is missing or has the wrong hash: $JournalPath" }
        if (Test-Path -LiteralPath $target -PathType Leaf) {
            $currentTargetHash = Get-FileSha256 -Path $target
            if ($currentTargetHash -ne [string]$journal.newDllSha256 -and $currentTargetHash -ne [string]$journal.previousDllBackup.Hash) {
                throw "Refusing to overwrite a DLL changed outside the interrupted transaction: $target"
            }
        }
        if (-not (Test-Path -LiteralPath $target -PathType Leaf) -or $currentTargetHash -ne [string]$journal.previousDllBackup.Hash) {
            Set-VerifiedFile -Source ([string]$journal.previousDllBackup.Path) -Destination $target -ExpectedHash ([string]$journal.previousDllBackup.Hash)
        }
    } elseif (Test-Path -LiteralPath $target -PathType Leaf) {
        if (-not [string]::Equals((Get-FileSha256 -Path $target), [string]$journal.newDllSha256, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove a DLL changed outside the interrupted transaction: $target"
        }
        Remove-Item -LiteralPath $target -Force
    }

    if ([bool]$journal.previousRecordExists) {
        if (-not (Test-VerifiedBackup -Backup $journal.previousRecordBackup -BackupRoot $backupRoot)) { throw "Prior record backup is missing or has the wrong hash: $JournalPath" }
        if (Test-Path -LiteralPath $recordPath -PathType Leaf) {
            $currentRecordHash = Get-FileSha256 -Path $recordPath
            if ($currentRecordHash -ne [string]$journal.newRecordSha256 -and $currentRecordHash -ne [string]$journal.previousRecordBackup.Hash) {
                throw "Refusing to overwrite a record changed outside the interrupted transaction: $recordPath"
            }
        }
        if (-not (Test-Path -LiteralPath $recordPath -PathType Leaf) -or $currentRecordHash -ne [string]$journal.previousRecordBackup.Hash) {
            Set-VerifiedFile -Source ([string]$journal.previousRecordBackup.Path) -Destination $recordPath -ExpectedHash ([string]$journal.previousRecordBackup.Hash)
        }
    } elseif (Test-Path -LiteralPath $recordPath -PathType Leaf) {
        if (-not [string]::Equals((Get-FileSha256 -Path $recordPath), [string]$journal.newRecordSha256, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove a record changed outside the interrupted transaction: $recordPath"
        }
        Remove-Item -LiteralPath $recordPath -Force
    }
    Remove-Item -LiteralPath $JournalPath -Force
    return 'rolled-back'
}

function Recover-RuntimeTransactions {
    param([Parameter(Mandatory)][string]$RuntimeRootPath)
    $transactionRoot = Get-TransactionRoot
    foreach ($journalPath in @(Get-ChildItem -LiteralPath $transactionRoot -Filter 'runtime-*.json' -File -Force -ErrorAction SilentlyContinue)) {
        try {
            $result = Restore-RuntimeTransaction -JournalPath $journalPath.FullName -RuntimeRootPath $RuntimeRootPath
            Write-MaintenanceLog -Event "transaction-$result" -Path $journalPath.FullName -Detail 'Recovered or finalized an interrupted managed runtime install transaction.'
        } catch {
            Write-MaintenanceLog -Event 'transaction-recovery-error' -Path $journalPath.FullName -Detail $_.Exception.Message
        }
    }
}

function Restore-PayloadTransaction {
    param([Parameter(Mandatory)][string]$JournalPath)
    $journal = Get-Content -LiteralPath $JournalPath -Raw | ConvertFrom-Json -ErrorAction Stop
    if ([int]$journal.schemaVersion -ne 1 -or
        -not [string]::Equals([string]$journal.ownerMarker, $script:OwnerMarker, [StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$journal.managedBy, $script:TaskName, [StringComparison]::Ordinal) -or
        -not [string]::Equals((Get-FullPathForComparison -Path ([string]$journal.stateRoot)), (Get-FullPathForComparison -Path $StateRoot), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Payload transaction journal is not owned by this install: $JournalPath"
    }
    if ([string]$journal.state -eq 'Committed') {
        Remove-Item -LiteralPath $JournalPath -Force
        return 'removed-committed'
    }
    if ([string]$journal.state -ne 'Prepared') { throw "Payload transaction journal has an unknown state: $JournalPath" }
    $backupRoot = Join-Path $StateRoot 'backups'
    $allowed = @(
        [IO.Path]::GetFullPath((Join-Path $StateRoot 'version.dll')),
        [IO.Path]::GetFullPath((Join-Path $StateRoot 'persistence.ps1')),
        [IO.Path]::GetFullPath((Join-Path $StateRoot 'persistence.json'))
    )
    foreach ($entry in @($journal.entries)) {
        $path = [IO.Path]::GetFullPath([string]$entry.path)
        if ($allowed -notcontains $path) { throw "Payload transaction includes an unexpected path: $JournalPath" }
        if (-not (Test-Sha256String -Hash ([string]$entry.newSha256))) { throw "Payload transaction has a malformed new hash: $JournalPath" }
        $exists = Test-Path -LiteralPath $path -PathType Leaf
        if ([bool]$entry.priorExists) {
            if (-not (Test-Sha256String -Hash ([string]$entry.priorSha256))) { throw "Payload transaction has a malformed previous hash: $JournalPath" }
            if ($exists) {
                $actual = Get-FileSha256 -Path $path
                if ($actual -ne [string]$entry.priorSha256 -and $actual -ne [string]$entry.newSha256) {
                    throw "Refusing to overwrite a payload file changed outside the interrupted transaction: $path"
                }
                if ($actual -eq [string]$entry.priorSha256) { continue }
            }
            if (-not (Test-VerifiedBackup -Backup $entry.priorBackup -BackupRoot $backupRoot)) { throw "Verified prior payload backup is missing: $JournalPath" }
            if ([string]$entry.priorBackup.Hash -ne [string]$entry.priorSha256) { throw "Payload backup hash does not match the prior manifest: $JournalPath" }
            Set-VerifiedFile -Source ([string]$entry.priorBackup.Path) -Destination $path -ExpectedHash ([string]$entry.priorSha256)
        } elseif ($exists) {
            if ((Get-FileSha256 -Path $path) -ne [string]$entry.newSha256) { throw "Refusing to remove a payload file changed outside the interrupted transaction: $path" }
            Remove-Item -LiteralPath $path -Force
        }
    }
    Remove-Item -LiteralPath $JournalPath -Force
    return 'rolled-back'
}

function Recover-PayloadTransactions {
    $script:PayloadRecoveryResults = New-Object 'System.Collections.Generic.List[string]'
    $transactionRoot = Get-TransactionRoot
    foreach ($journalPath in @(Get-ChildItem -LiteralPath $transactionRoot -Filter 'payload-*.json' -File -Force -ErrorAction SilentlyContinue)) {
        try {
            $result = Restore-PayloadTransaction -JournalPath $journalPath.FullName
            $script:PayloadRecoveryResults.Add("$result|$($journalPath.FullName)")
        } catch {
            throw
        }
    }
}

function Install-RuntimeCandidateTransaction {
    param(
        [Parameter(Mandatory)]$Candidate,
        [Parameter(Mandatory)][string]$RuntimeRootPath,
        [Parameter(Mandatory)][string]$DllSource,
        [Parameter(Mandatory)][string]$SourceHash,
        [Parameter(Mandatory)][string]$RecordPath
    )
    $target = Join-Path $Candidate.Directory 'version.dll'
    $targetExists = Test-Path -LiteralPath $target -PathType Leaf
    $recordExists = Test-Path -LiteralPath $RecordPath -PathType Leaf
    $dllBackup = if ($targetExists) { Save-VerifiedBackup -Path $target -RuntimeId $Candidate.RuntimeId } else { $null }
    $recordBackup = if ($recordExists) { Save-VerifiedBackup -Path $RecordPath -RuntimeId $Candidate.RuntimeId } else { $null }
    $recordJson = Get-InstallRecordJson -HelperPath $Candidate.HelperPath -DllHash $SourceHash
    $transactionRoot = Get-TransactionRoot
    $journalPath = Join-Path $transactionRoot "runtime-$($Candidate.RuntimeId)-$([Guid]::NewGuid().ToString('N')).json"
    $journal = [pscustomobject]@{
        schemaVersion = 1
        ownerMarker = $script:OwnerMarker
        managedBy = $script:TaskName
        state = 'Prepared'
        runtimeRoot = [IO.Path]::GetFullPath($RuntimeRootPath)
        runtimeId = $Candidate.RuntimeId
        targetPath = [IO.Path]::GetFullPath($target)
        recordPath = [IO.Path]::GetFullPath($RecordPath)
        helperPath = [IO.Path]::GetFullPath($Candidate.HelperPath)
        helperSha256 = Get-FileSha256 -Path $Candidate.HelperPath
        newDllSha256 = $SourceHash
        newRecordSha256 = Get-TextSha256 -Text $recordJson
        previousDllExists = [bool]$targetExists
        previousDllBackup = $dllBackup
        previousRecordExists = [bool]$recordExists
        previousRecordBackup = $recordBackup
        createdAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    }
    Write-JsonAtomically -Path $journalPath -Value $journal
    try {
        Set-VerifiedFile -Source $DllSource -Destination $target -ExpectedHash $SourceHash
        $journal.state = 'DllInstalled'
        Write-JsonAtomically -Path $journalPath -Value $journal
        Write-JsonAtomically -Path $RecordPath -Value ([pscustomobject]@{}) -JsonText $recordJson
        if (-not [string]::Equals((Get-FileSha256 -Path $RecordPath), [string]$journal.newRecordSha256, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Installed ownership record hash mismatch: $RecordPath"
        }
        $journal.state = 'Committed'
        Write-JsonAtomically -Path $journalPath -Value $journal
        Remove-Item -LiteralPath $journalPath -Force
    } catch {
        $installError = $_
        try { [void](Restore-RuntimeTransaction -JournalPath $journalPath -RuntimeRootPath $RuntimeRootPath) }
        catch { Write-MaintenanceLog -Event 'transaction-rollback-error' -Path $journalPath -Detail $_.Exception.Message }
        throw $installError
    }
}

function Test-TaskUserMatchesCurrentUser {
    param([string]$TaskUser)
    if ([string]::IsNullOrWhiteSpace($TaskUser)) { return $false }
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    if ([string]::Equals($TaskUser, $identity.Name, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    if ($TaskUser -match '^S-1-\d+(?:-\d+)+$') { return [string]::Equals($TaskUser, $identity.User.Value, [StringComparison]::OrdinalIgnoreCase) }
    try {
        $taskSid = (New-Object -TypeName Security.Principal.NTAccount -ArgumentList $TaskUser).Translate([Security.Principal.SecurityIdentifier])
        return [string]::Equals($taskSid.Value, $identity.User.Value, [StringComparison]::OrdinalIgnoreCase)
    } catch { return $false }
}

function ConvertFrom-ScheduledTaskArguments {
    param([AllowNull()][string]$Arguments)
    if ($null -eq $Arguments) { return $null }

    $tokens = New-Object 'System.Collections.Generic.List[string]'
    $position = 0
    while ($position -lt $Arguments.Length) {
        while ($position -lt $Arguments.Length -and [char]::IsWhiteSpace($Arguments[$position])) { $position++ }
        if ($position -ge $Arguments.Length) { break }

        if ($Arguments[$position] -eq [char]'"') {
            $position++
            $start = $position
            while ($position -lt $Arguments.Length -and $Arguments[$position] -ne [char]'"') { $position++ }
            if ($position -ge $Arguments.Length) { return $null }
            $token = $Arguments.Substring($start, $position - $start)
            $position++
            if ($position -lt $Arguments.Length -and -not [char]::IsWhiteSpace($Arguments[$position])) { return $null }
        } else {
            $start = $position
            while ($position -lt $Arguments.Length -and -not [char]::IsWhiteSpace($Arguments[$position])) {
                if ($Arguments[$position] -eq [char]'"') { return $null }
                $position++
            }
            $token = $Arguments.Substring($start, $position - $start)
        }
        $tokens.Add($token)
    }
    return $tokens.ToArray()
}

function Test-ManagedScheduledTask {
    param([object]$Task, [Parameter(Mandatory)][string]$ExpectedScriptPath)
    if (-not $Task) { return $false }
    if (-not [string]::Equals([string]$Task.Description, $script:TaskDescription, [StringComparison]::Ordinal)) { return $false }
    $actions = @($Task.Actions)
    if ($actions.Count -ne 1) { return $false }
    $expectedPowerShell = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
    try {
        if (-not [string]::Equals((Get-FullPathForComparison -Path ([string]$actions[0].Execute)), (Get-FullPathForComparison -Path $expectedPowerShell), [StringComparison]::OrdinalIgnoreCase)) { return $false }
    } catch { return $false }
    $arguments = ConvertFrom-ScheduledTaskArguments -Arguments ([string]$actions[0].Arguments)
    if ($null -eq $arguments) { return $false }
    $arguments = [string[]]$arguments
    $expectedArguments = @(
        '-NoLogo', '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass',
        '-WindowStyle', 'Hidden', '-File', [IO.Path]::GetFullPath($ExpectedScriptPath), '-Action', 'Watch'
    )
    if ($arguments.Count -ne $expectedArguments.Count) { return $false }
    try {
        for ($index = 0; $index -lt $expectedArguments.Count; $index++) {
            if ($index -eq 8) {
                if (-not [string]::Equals((Get-FullPathForComparison -Path $arguments[$index]), (Get-FullPathForComparison -Path $ExpectedScriptPath), [StringComparison]::OrdinalIgnoreCase)) { return $false }
            } elseif (-not [string]::Equals($arguments[$index], $expectedArguments[$index], [StringComparison]::OrdinalIgnoreCase)) {
                return $false
            }
        }
    } catch { return $false }
    if (-not (Test-TaskUserMatchesCurrentUser -TaskUser ([string]$Task.Principal.UserId))) { return $false }
    return [string]::Equals([string]$Task.Principal.RunLevel, 'Limited', [StringComparison]::OrdinalIgnoreCase)
}

function Stop-VerifiedManagedTask {
    param([Parameter(Mandatory)][string]$ExpectedScriptPath)
    $task = Get-ScheduledTask -TaskName $script:TaskName -ErrorAction SilentlyContinue
    if (-not $task) { return $false }
    if (-not (Test-ManagedScheduledTask -Task $task -ExpectedScriptPath $ExpectedScriptPath)) {
        throw "Scheduled task does not match this install's description, action, current user, and Limited run level; refusing to stop or replace it: $($task.TaskName)"
    }
    $wasRunning = [string]$task.State -eq 'Running'
    if ($wasRunning) {
        Stop-ScheduledTask -TaskName $script:TaskName -ErrorAction Stop
        $deadline = (Get-Date).AddSeconds(15)
        do {
            Start-Sleep -Milliseconds 250
            $task = Get-ScheduledTask -TaskName $script:TaskName -ErrorAction SilentlyContinue
        } while ($task -and [string]$task.State -eq 'Running' -and (Get-Date) -lt $deadline)
        if ($task -and [string]$task.State -eq 'Running') { throw 'Managed watcher task did not stop within 15 seconds; task registration was not changed.' }
    }
    return $wasRunning
}

function Invoke-RuntimeScan {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$DllSource, [switch]$Quiet)
    if (Test-PathChainHasReparsePoint -Path $Root) { throw "Refusing to scan through a reparse point in the runtime-root path chain: $Root" }
    if (Test-PathChainHasReparsePoint -Path $StateRoot) { throw "Refusing to write through a reparse point in the state-root path chain: $StateRoot" }
    Recover-RuntimeTransactions -RuntimeRootPath $Root
    if (-not (Test-Path -LiteralPath $DllSource -PathType Leaf)) { throw "Source DLL not found: $DllSource" }
    $sourceHash = Get-FileSha256 -Path $DllSource
    $candidates = @(Get-RuntimeCandidates -Root $Root)
    if ($candidates.Count -eq 0) {
        if (-not $Quiet) { Write-Output "No complete Codex Computer Use runtime found under $Root" }
        return
    }
    foreach ($candidate in $candidates) {
        $target = Join-Path $candidate.Directory 'version.dll'
        $recordPath = Join-Path $candidate.Directory 'codex-capture-compat.install.json'
        if ((Test-Path -LiteralPath $target) -and (Test-ReparsePoint -Path $target)) {
            Write-MaintenanceLog -Event 'reparse-target-preserved' -Path $target -Detail 'Existing version.dll is a reparse point; preserved without reading, replacing, or following it.'
            if (-not $Quiet) { Write-Output "Preserved reparse-point version.dll: $target" }
            continue
        }
        if ((Test-Path -LiteralPath $recordPath) -and (Test-ReparsePoint -Path $recordPath)) {
            Write-MaintenanceLog -Event 'reparse-record-preserved' -Path $recordPath -Detail 'Install record is a reparse point; preserved without reading or following it.'
            if (-not $Quiet) { Write-Output "Preserved reparse-point install record: $recordPath" }
            continue
        }
        $record = Read-InstallRecord -Path $recordPath
        $recordExists = Test-Path -LiteralPath $recordPath -PathType Leaf
        $helperHash = Get-FileSha256 -Path $candidate.HelperPath
        $targetExists = Test-Path -LiteralPath $target -PathType Leaf
        $targetHash = if ($targetExists) { Get-FileSha256 -Path $target } else { $null }
        $recordIdentityOwns = Test-RecordIdentity -Record $record -HelperPath $candidate.HelperPath
        $recordMetadataOwns = Test-RecordMetadata -Record $record -HelperPath $candidate.HelperPath -HelperHash $helperHash
        $recordOwnsOrCanReconcile = $recordIdentityOwns -and (-not $targetExists -or [string]::Equals([string]$record.dllSha256, [string]$targetHash, [StringComparison]::OrdinalIgnoreCase))

        if ($targetExists -and -not $recordOwnsOrCanReconcile) {
            $reason = if ($recordExists -and -not $record) { 'Ownership record is malformed' } elseif ($recordExists -and -not $recordIdentityOwns) { 'Ownership record identity, helper path, or hashes are invalid' } else { 'Ownership record DLL hash does not match the installed file' }
            Write-MaintenanceLog -Event 'foreign-dll-preserved' -Path $target -Detail "$reason; existing SHA256 $targetHash was preserved without adoption or overwrite."
            if (-not $Quiet) { Write-Output "Preserved unowned version.dll: $target" }
            continue
        }

        if ($targetExists -and $targetHash -eq $sourceHash -and $recordMetadataOwns) {
            if (-not $Quiet) { Write-Output "Already managed: $target" }
            continue
        }

        if (-not $targetExists -and $recordExists -and -not $recordIdentityOwns) {
            $reason = if (-not $record) { 'Malformed ownership record' } else { 'Ownership record identity or helper path does not match' }
            Write-MaintenanceLog -Event 'record-preserved' -Path $recordPath -Detail "$reason; no DLL was installed and the record was preserved."
            if (-not $Quiet) { Write-Output "Preserved unowned install record: $recordPath" }
            continue
        }

        Install-RuntimeCandidateTransaction -Candidate $candidate -RuntimeRootPath $Root -DllSource $DllSource -SourceHash $sourceHash -RecordPath $recordPath
        if ($targetExists) {
            $event = if ($recordMetadataOwns) { 'updated-owned-install' } else { 'reconciled-owned-helper-update' }
            $detail = if ($recordMetadataOwns) { "Replaced owned DLL in a recoverable transaction. New SHA256 $sourceHash; helper SHA256 $helperHash." } else { "Reconciled an owned runtime after its helper changed at the same path. The previous DLL hash matched its owner record; new DLL SHA256 $sourceHash and helper SHA256 $helperHash were committed transactionally." }
            Write-MaintenanceLog -Event $event -Path $target -Detail $detail
            if (-not $Quiet) { Write-Output "Updated owned installation with verified backup: $target" }
        } else {
            $event = if ($recordIdentityOwns) { 'restored-owned-runtime' } else { 'installed-runtime' }
            $detail = if ($recordIdentityOwns) { "Restored a missing DLL for an owned runtime. The prior owner record matched the exact helper path; new DLL SHA256 $sourceHash and helper SHA256 $helperHash were committed transactionally." } else { "Installed managed DLL SHA256 $sourceHash for helper SHA256 $helperHash." }
            Write-MaintenanceLog -Event $event -Path $target -Detail $detail
            if (-not $Quiet) { Write-Output "Installed in discovered Codex runtime: $target" }
        }
    }
}

function Install-ManagedPayload {
    param([string]$DllSource, [string]$RuntimeRootPath, [switch]$SkipTask)
    if (-not (Test-Path -LiteralPath $DllSource -PathType Leaf)) { throw "Built DLL not found: $DllSource" }
    if (Test-PathChainHasReparsePoint -Path $RuntimeRootPath) { throw "Refusing a reparse point in the runtime-root path chain: $RuntimeRootPath" }
    if (Test-PathChainHasReparsePoint -Path $StateRoot) { throw "Refusing a reparse point in the state-root path chain: $StateRoot" }
    $scriptSource = $PSCommandPath
    if (-not (Test-Path -LiteralPath $scriptSource -PathType Leaf)) { throw 'Persistence script source path is unavailable.' }
    $stagedScript = Join-Path $StateRoot 'persistence.ps1'
    if (-not $SkipTask) {
        $existingTask = Get-ScheduledTask -TaskName $script:TaskName -ErrorAction SilentlyContinue
        if ($existingTask -and -not (Test-ManagedScheduledTask -Task $existingTask -ExpectedScriptPath $stagedScript)) {
            throw "A scheduled task with the managed name does not match this install's description, action, current user, and Limited run level: $script:TaskName"
        }
    }
    if (-not (Test-Path -LiteralPath $StateRoot -PathType Container)) { New-Item -ItemType Directory -Path $StateRoot -Force | Out-Null }
    if (Test-ReparsePoint -Path $StateRoot) { throw "Refusing a reparse-point state directory: $StateRoot" }
    Recover-PayloadTransactions

    $manifestPath = Join-Path $StateRoot 'persistence.json'
    $manifest = $null
    if (Test-Path -LiteralPath $manifestPath -PathType Leaf) {
        try { $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json } catch { throw 'Existing persistence manifest is unreadable; leaving the directory unchanged.' }
        if ([int]$manifest.schemaVersion -ne 1 -or
            -not [string]::Equals([string]$manifest.ownerMarker, $script:OwnerMarker, [StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$manifest.taskName, $script:TaskName, [StringComparison]::Ordinal) -or
            -not (Test-Sha256String -Hash ([string]$manifest.dllSha256)) -or
            -not (Test-Sha256String -Hash ([string]$manifest.scriptSha256)) -or
            -not [string]::Equals([string]$manifest.runtimeRoot, [IO.Path]::GetFullPath($RuntimeRootPath), [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Existing persistence manifest belongs to another owner; refusing to modify it.'
        }
    } elseif (@(Get-ChildItem -LiteralPath $StateRoot -Force | Where-Object { $_.Name -notin @('backups', 'transactions') }).Count -gt 0) {
        throw "State directory already contains unowned files: $StateRoot"
    }

    $stagedDll = Join-Path $StateRoot 'version.dll'
    $expectedDllHash = Get-FileSha256 -Path $DllSource
    $expectedScriptHash = Get-FileSha256 -Path $scriptSource
    $payloadEntries = New-Object 'System.Collections.Generic.List[object]'
    foreach ($entry in @(
        @{ Destination = $stagedDll; Expected = $expectedDllHash; Prior = if ($manifest) { [string]$manifest.dllSha256 } else { $null } },
        @{ Destination = $stagedScript; Expected = $expectedScriptHash; Prior = if ($manifest) { [string]$manifest.scriptSha256 } else { $null } }
    )) {
        $exists = Test-Path -LiteralPath $entry.Destination -PathType Leaf
        $actual = if ($exists) { Get-FileSha256 -Path $entry.Destination } else { $null }
        if ($exists -and $actual -ne $entry.Expected -and (-not $entry.Prior -or $actual -ne $entry.Prior)) {
            throw "Unowned managed payload file; refusing replacement: $($entry.Destination)"
        }
        $backup = if ($exists) { Save-VerifiedBackup -Path $entry.Destination -RuntimeId 'payload' } else { $null }
        $payloadEntries.Add([pscustomobject]@{
            path = [IO.Path]::GetFullPath($entry.Destination)
            priorExists = [bool]$exists
            priorSha256 = $actual
            priorBackup = $backup
            newSha256 = $entry.Expected
        })
    }
    $newManifest = [pscustomobject]@{
        schemaVersion = 1
        ownerMarker = $script:OwnerMarker
        taskName = $script:TaskName
        taskDescription = $script:TaskDescription
        runtimeRoot = [IO.Path]::GetFullPath($RuntimeRootPath)
        dllSha256 = $expectedDllHash
        scriptSha256 = $expectedScriptHash
        installedAt = if ($manifest) { [string]$manifest.installedAt } else { (Get-Date).ToUniversalTime().ToString('o') }
    }
    $newManifestJson = $newManifest | ConvertTo-Json -Depth 6
    $manifestExists = Test-Path -LiteralPath $manifestPath -PathType Leaf
    $priorManifestHash = if ($manifestExists) { Get-FileSha256 -Path $manifestPath } else { $null }
    $manifestBackup = if ($manifestExists) { Save-VerifiedBackup -Path $manifestPath -RuntimeId 'payload' } else { $null }
    $payloadEntries.Add([pscustomobject]@{
        path = [IO.Path]::GetFullPath($manifestPath)
        priorExists = [bool]$manifestExists
        priorSha256 = $priorManifestHash
        priorBackup = $manifestBackup
        newSha256 = Get-TextSha256 -Text $newManifestJson
    })
    $transactionRoot = Get-TransactionRoot
    $payloadJournalPath = Join-Path $transactionRoot "payload-$([Guid]::NewGuid().ToString('N')).json"
    $payloadJournal = [pscustomobject]@{
        schemaVersion = 1
        ownerMarker = $script:OwnerMarker
        managedBy = $script:TaskName
        stateRoot = [IO.Path]::GetFullPath($StateRoot)
        state = 'Prepared'
        entries = $payloadEntries.ToArray()
        createdAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    }
    Write-JsonAtomically -Path $payloadJournalPath -Value $payloadJournal
    try {
        if (-not (Test-Path -LiteralPath $stagedDll -PathType Leaf) -or (Get-FileSha256 -Path $stagedDll) -ne $expectedDllHash) {
            Set-VerifiedFile -Source $DllSource -Destination $stagedDll -ExpectedHash $expectedDllHash
        }
        if (-not (Test-Path -LiteralPath $stagedScript -PathType Leaf) -or (Get-FileSha256 -Path $stagedScript) -ne $expectedScriptHash) {
            Set-VerifiedFile -Source $scriptSource -Destination $stagedScript -ExpectedHash $expectedScriptHash
        }
        if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf) -or (Get-FileSha256 -Path $manifestPath) -ne [string]$payloadEntries[2].newSha256) {
            Write-JsonAtomically -Path $manifestPath -Value $newManifest -JsonText $newManifestJson
        }
        foreach ($entry in $payloadEntries) {
            if (-not (Test-Path -LiteralPath $entry.path -PathType Leaf) -or (Get-FileSha256 -Path $entry.path) -ne [string]$entry.newSha256) {
                throw "Persistence payload failed its final hash check: $($entry.path)"
            }
        }
        $payloadJournal.state = 'Committed'
        Write-JsonAtomically -Path $payloadJournalPath -Value $payloadJournal
        Remove-Item -LiteralPath $payloadJournalPath -Force
    } catch {
        $payloadError = $_
        try { [void](Restore-PayloadTransaction -JournalPath $payloadJournalPath) }
        catch { Write-MaintenanceLog -Event 'payload-rollback-error' -Path $payloadJournalPath -Detail $_.Exception.Message }
        throw $payloadError
    }

    Invoke-RuntimeScan -Root $RuntimeRootPath -DllSource $stagedDll -Quiet:$SkipTask
    foreach ($recovery in @($script:PayloadRecoveryResults.ToArray())) {
        $parts = $recovery -split '\|', 2
        Write-MaintenanceLog -Event "payload-transaction-$($parts[0])" -Path $parts[1] -Detail 'Recovered or finalized an interrupted persistence payload update.'
    }

    if (-not $SkipTask) {
        $existingTask = Get-ScheduledTask -TaskName $script:TaskName -ErrorAction SilentlyContinue
        $taskWasRunning = $false
        if ($existingTask) {
            if (-not (Test-ManagedScheduledTask -Task $existingTask -ExpectedScriptPath $stagedScript)) {
                throw "A scheduled task with the managed name no longer matches this install's expected configuration: $script:TaskName"
            }
            $taskWasRunning = Stop-VerifiedManagedTask -ExpectedScriptPath $stagedScript
        }
        $powerShell = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$stagedScript`" -Action Watch"
        $user = [Security.Principal.WindowsIdentity]::GetCurrent().Name
        $taskAction = New-ScheduledTaskAction -Execute $powerShell -Argument $arguments -WorkingDirectory $StateRoot
        $taskTrigger = New-ScheduledTaskTrigger -AtLogOn -User $user
        $principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
        $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 3 -RestartInterval ([TimeSpan]::FromMinutes(1)) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
        try {
            Register-ScheduledTask -TaskName $script:TaskName -Action $taskAction -Trigger $taskTrigger -Principal $principal -Settings $settings -Description $script:TaskDescription -Force | Out-Null
            Start-ScheduledTask -TaskName $script:TaskName
        } catch {
            $taskFailure = $_
            if ($taskWasRunning) {
                try {
                    $restartTask = Get-ScheduledTask -TaskName $script:TaskName -ErrorAction SilentlyContinue
                    if ($restartTask -and (Test-ManagedScheduledTask -Task $restartTask -ExpectedScriptPath $stagedScript)) {
                        Start-ScheduledTask -TaskName $script:TaskName -ErrorAction Stop
                    }
                } catch { Write-MaintenanceLog -Event 'watcher-restart-error' -Path $script:TaskName -Detail $_.Exception.Message }
            }
            throw $taskFailure
        }
        Write-MaintenanceLog -Event 'watcher-task-installed' -Path $script:TaskName -Detail "User-level logon watcher registered for $RuntimeRootPath. No elevation and no process termination."
        Write-Output "Registered and started user-level runtime watcher: $script:TaskName"
    } else {
        Write-Output 'Staged persistence payload; scheduled task registration skipped for fixture test.'
    }
}

function Get-SourceDllPath {
    if ($SourceDll) { return (Resolve-Path -LiteralPath $SourceDll).Path }
    if ($Action -eq 'Watch' -or $Action -eq 'ScanOnce') {
        $local = Join-Path $PSScriptRoot 'version.dll'
        if (Test-Path -LiteralPath $local -PathType Leaf) { return $local }
    }
    return (Join-Path $PSScriptRoot 'dist\version.dll')
}

function Show-Status {
    $task = Get-ScheduledTask -TaskName $script:TaskName -ErrorAction SilentlyContinue
    if ($task) {
        "Watcher task: $($task.TaskName) [$($task.State)]"
        "Task description: $($task.Description)"
        "Task configuration owned: $(Test-ManagedScheduledTask -Task $task -ExpectedScriptPath (Join-Path $StateRoot 'persistence.ps1'))"
        if ($task.Settings) { "Task failure recovery: restart-count=$($task.Settings.RestartCount); interval=$($task.Settings.RestartInterval)" }
    } else { 'Watcher task: not registered' }
    "Runtime root: $RuntimeRoot"
    $dllSource = Get-SourceDllPath
    if (Test-Path -LiteralPath $dllSource -PathType Leaf) { "Managed source SHA256: $(Get-FileSha256 -Path $dllSource)" }
    $manifestPath = Join-Path $StateRoot 'persistence.json'
    if (Test-Path -LiteralPath $manifestPath -PathType Leaf) {
        $manifestHash = Get-FileSha256 -Path $manifestPath
        try {
            $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -ErrorAction Stop
            $manifestOwned = ([int]$manifest.schemaVersion -eq 1) -and
                [string]::Equals([string]$manifest.ownerMarker, $script:OwnerMarker, [StringComparison]::Ordinal) -and
                [string]::Equals([string]$manifest.taskName, $script:TaskName, [StringComparison]::Ordinal) -and
                (Test-Sha256String -Hash ([string]$manifest.dllSha256)) -and
                (Test-Sha256String -Hash ([string]$manifest.scriptSha256))
            "Persistence manifest: SHA256 $manifestHash; managed-owner=$manifestOwned"
            if ($manifestOwned) {
                $stagedDll = Join-Path $StateRoot 'version.dll'
                $stagedScript = Join-Path $StateRoot 'persistence.ps1'
                $dllMatches = (Test-Path -LiteralPath $stagedDll -PathType Leaf) -and [string]::Equals((Get-FileSha256 -Path $stagedDll), [string]$manifest.dllSha256, [StringComparison]::OrdinalIgnoreCase)
                $scriptMatches = (Test-Path -LiteralPath $stagedScript -PathType Leaf) -and [string]::Equals((Get-FileSha256 -Path $stagedScript), [string]$manifest.scriptSha256, [StringComparison]::OrdinalIgnoreCase)
                "Staged payload hashes: dll=$dllMatches script=$scriptMatches"
            }
        } catch { "Persistence manifest: SHA256 $manifestHash; JSON invalid (preserved)" }
    } else { 'Persistence manifest: not present' }
    if (Test-Path -LiteralPath $script:HealthPath -PathType Leaf) {
        try {
            $health = Get-Content -LiteralPath $script:HealthPath -Raw | ConvertFrom-Json -ErrorAction Stop
            $healthOwned = ([int]$health.schemaVersion -eq 1) -and
                [string]::Equals([string]$health.ownerMarker, $script:OwnerMarker, [StringComparison]::Ordinal) -and
                [string]::Equals([string]$health.managedBy, $script:TaskName, [StringComparison]::Ordinal)
            "Watcher health: $($health.state); last-success=$($health.lastSuccessUtc); last-error=$($health.lastError); owned=$healthOwned"
        } catch { 'Watcher health: invalid or unreadable (preserved)' }
    } else { 'Watcher health: not reported' }
    foreach ($candidate in @(Get-RuntimeCandidates -Root $RuntimeRoot)) {
        $target = Join-Path $candidate.Directory 'version.dll'
        $recordPath = Join-Path $candidate.Directory 'codex-capture-compat.install.json'
        if (Test-Path -LiteralPath $target -PathType Leaf) {
            $hash = Get-FileSha256 -Path $target
            $record = Read-InstallRecord -Path $recordPath
            $helperHash = Get-FileSha256 -Path $candidate.HelperPath
            $owned = Test-RecordOwnsDll -Record $record -HelperPath $candidate.HelperPath -HelperHash $helperHash -DllHash $hash
            "Runtime $($candidate.RuntimeId): version.dll SHA256 $hash; recorded-owner=$owned"
        } else { "Runtime $($candidate.RuntimeId): no local version.dll" }
    }
}

function Get-ManagedCuaExecutablePaths {
    param([Parameter(Mandatory)][string]$Root)
    $paths = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($candidate in @(Get-RuntimeCandidates -Root $Root)) {
        $helperPath = [IO.Path]::GetFullPath([string]$candidate.HelperPath)
        $dllPath = Join-Path $candidate.Directory 'version.dll'
        $recordPath = Join-Path $candidate.Directory 'codex-capture-compat.install.json'
        if ((Test-ReparsePoint -Path $dllPath) -or (Test-ReparsePoint -Path $recordPath)) { continue }
        if (-not (Test-Path -LiteralPath $dllPath -PathType Leaf)) { continue }
        $record = Read-InstallRecord -Path $recordPath
        $helperHash = Get-FileSha256 -Path $helperPath
        $dllHash = Get-FileSha256 -Path $dllPath
        if (-not (Test-RecordOwnsDll -Record $record -HelperPath $helperPath -HelperHash $helperHash -DllHash $dllHash)) { continue }
        [void]$paths.Add($helperPath)
    }
    return $paths
}

function Test-ManagedCuaProcessActive {
    param(
        [Parameter(Mandatory)][string[]]$ExecutablePaths,
        [object[]]$ProcessSnapshot
    )
    if ($ExecutablePaths.Count -eq 0) { return $false }
    if (-not $PSBoundParameters.ContainsKey('ProcessSnapshot')) {
        try {
            # The Swift accessibility companion can remain under Codex for hours after
            # the Computer Use session ends; only the session helper gates restoration.
            $ProcessSnapshot = @(Get-CimInstance -ClassName Win32_Process -Filter "Name = 'codex-computer-use.exe'" -ErrorAction Stop)
        } catch {
            # Unknown process state must never trigger a cursor reset.
            return $null
        }
    }
    $known = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in $ExecutablePaths) {
        try { [void]$known.Add([IO.Path]::GetFullPath([string]$path)) } catch { }
    }
    $unresolvedManagedProcess = $false
    foreach ($process in @($ProcessSnapshot)) {
        $path = [string]$process.ExecutablePath
        if (-not $path) {
            # The CIM query is already restricted to the session helper process name.
            # Missing executable-path access is therefore unknown, not evidence of exit.
            $unresolvedManagedProcess = $true
            continue
        }
        try {
            if ($known.Contains([IO.Path]::GetFullPath($path))) { return $true }
        } catch { }
        # A same-name helper outside the current allowlist may be a newly deployed
        # runtime. Do not interpret it as a closed session until discovery catches up.
        $unresolvedManagedProcess = $true
    }
    if ($unresolvedManagedProcess) { return $null }
    return $false
}

function New-CuaCursorRecoveryState {
    return @{
        HelperActive = $false
        HasObservation = $false
        CursorWasVisibleDuringSession = $false
        PreviousCursorVisible = $false
        HiddenAfterVisible = $false
    }
}

function Update-CuaCursorRecoveryState {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][bool]$HelperActive,
        [Parameter(Mandatory)][bool]$CursorVisible
    )
    $shouldRestore = $false
    if (-not $State.HasObservation) {
        # If the watcher first sees an already-running, already-hidden session,
        # do not infer that Computer Use hid a cursor that may have been hidden before.
        $State.HelperActive = $HelperActive
        $State.HasObservation = $true
        $State.CursorWasVisibleDuringSession = $HelperActive -and $CursorVisible
        $State.PreviousCursorVisible = $CursorVisible
        return $false
    }
    if ($HelperActive) {
        if (-not $State.HelperActive) {
            # A visible sample immediately before launch proves ownership of the transition.
            $State.CursorWasVisibleDuringSession = $State.PreviousCursorVisible
            $State.HiddenAfterVisible = $false
        }
        if ($CursorVisible) {
            $State.CursorWasVisibleDuringSession = $true
        } elseif ($State.CursorWasVisibleDuringSession) {
            $State.HiddenAfterVisible = $true
        }
    } elseif ($State.HelperActive) {
        $shouldRestore = [bool]$State.HiddenAfterVisible -and -not $CursorVisible
        $State.HiddenAfterVisible = $false
        $State.CursorWasVisibleDuringSession = $false
    }
    $State.HelperActive = $HelperActive
    $State.PreviousCursorVisible = $CursorVisible
    return $shouldRestore
}

function Initialize-CursorRecoveryNativeApi {
    if ('CodexCaptureCompat.CursorRecoveryNative' -as [type]) { return }
    $source = @'
using System;
using System.Runtime.InteropServices;

namespace CodexCaptureCompat
{
public static class CursorRecoveryNative
{
    [StructLayout(LayoutKind.Sequential)]
    private struct CursorInfo
    {
        public int Size;
        public int Flags;
        public IntPtr Cursor;
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", EntryPoint = "GetCursorInfo", SetLastError = true)]
    private static extern bool GetCursorInfo(ref CursorInfo info);

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint parameter, IntPtr data, uint flags);

    public static bool TryGetCursorVisibility(out bool visible, out int error)
    {
        var info = new CursorInfo { Size = Marshal.SizeOf(typeof(CursorInfo)) };
        var succeeded = GetCursorInfo(ref info);
        error = Marshal.GetLastWin32Error();
        visible = succeeded && (info.Flags & 1) != 0;
        return succeeded;
    }

    public static bool ReloadSystemCursors(out int error)
    {
        // SPI_SETCURSORS reloads the active Windows cursor scheme (uiParam=0, pvParam=NULL).
        var succeeded = SystemParametersInfo(0x0057, 0, IntPtr.Zero, 0);
        error = Marshal.GetLastWin32Error();
        return succeeded;
    }
}
}
'@
    Add-Type -TypeDefinition $source -ErrorAction Stop | Out-Null
}

function Get-SystemCursorVisibility {
    Initialize-CursorRecoveryNativeApi
    $visible = $false
    $errorCode = 0
    if (-not [CodexCaptureCompat.CursorRecoveryNative]::TryGetCursorVisibility([ref]$visible, [ref]$errorCode)) {
        throw "GetCursorInfo failed with Win32 error $errorCode."
    }
    return $visible
}

function Restore-SystemCursorScheme {
    Initialize-CursorRecoveryNativeApi
    $errorCode = 0
    if (-not [CodexCaptureCompat.CursorRecoveryNative]::ReloadSystemCursors([ref]$errorCode)) {
        throw "SPI_SETCURSORS failed with Win32 error $errorCode."
    }
}

function Invoke-CuaCursorRecoveryCheck {
    param(
        [Parameter(Mandatory)][string[]]$ExecutablePaths,
        [Parameter(Mandatory)][hashtable]$State
    )
    $active = Test-ManagedCuaProcessActive -ExecutablePaths $ExecutablePaths
    if ($null -eq $active) { return }
    $visible = Get-SystemCursorVisibility
    if (-not (Update-CuaCursorRecoveryState -State $State -HelperActive ([bool]$active) -CursorVisible $visible)) { return }

    try {
        Restore-SystemCursorScheme
        Start-Sleep -Milliseconds 100
        $visibleAfterRestore = Get-SystemCursorVisibility
        if ($visibleAfterRestore) {
            Write-MaintenanceLog -Event 'cursor-restored-after-cua' -Path $RuntimeRoot -Detail 'Computer Use exited after hiding the system cursor; SPI_SETCURSORS reloaded the active Windows cursor scheme.'
        } else {
            Write-MaintenanceLog -Event 'cursor-restore-incomplete' -Path $RuntimeRoot -Detail 'SPI_SETCURSORS completed after Computer Use exited, but GetCursorInfo still reports the cursor hidden.'
        }
    } catch {
        Write-MaintenanceLog -Event 'cursor-restore-error' -Path $RuntimeRoot -Detail $_.Exception.Message
    }
}

function Start-RuntimeWatcher {
    if (-not (Test-Path -LiteralPath $RuntimeRoot -PathType Container)) {
        New-Item -ItemType Directory -Path $RuntimeRoot -Force | Out-Null
    }
    if (Test-ReparsePoint -Path $RuntimeRoot) { throw "Refusing to watch a reparse-point runtime root: $RuntimeRoot" }
    if (-not (Test-Path -LiteralPath $StateRoot -PathType Container)) { New-Item -ItemType Directory -Path $StateRoot -Force | Out-Null }
    if (Test-ReparsePoint -Path $StateRoot) { throw "Refusing to use a reparse-point state directory: $StateRoot" }
    $source = Get-SourceDllPath
    $watcher = New-Object IO.FileSystemWatcher
    $watcher.Path = [IO.Path]::GetFullPath($RuntimeRoot)
    $watcher.Filter = '*'
    $watcher.IncludeSubdirectories = $true
    $watcher.NotifyFilter = [IO.NotifyFilters]::DirectoryName -bor [IO.NotifyFilters]::FileName -bor [IO.NotifyFilters]::LastWrite
    $watcher.EnableRaisingEvents = $true
    $ids = @("CodexCompatCreated-$PID", "CodexCompatRenamed-$PID", "CodexCompatChanged-$PID")
    Register-ObjectEvent -InputObject $watcher -EventName Created -SourceIdentifier $ids[0] | Out-Null
    Register-ObjectEvent -InputObject $watcher -EventName Renamed -SourceIdentifier $ids[1] | Out-Null
    Register-ObjectEvent -InputObject $watcher -EventName Changed -SourceIdentifier $ids[2] | Out-Null
    Write-MaintenanceLog -Event 'watcher-started' -Path $RuntimeRoot -Detail 'Watching only the local Codex cua_node runtime tree; scanning every 15 seconds as a fallback.'
    $lastScan = [DateTime]::MinValue
    $lastSuccessUtc = $null
    $lastError = $null
    $lastErrorUtc = $null
    try {
        if (Test-Path -LiteralPath $script:HealthPath -PathType Leaf) {
            $previousHealth = Get-Content -LiteralPath $script:HealthPath -Raw | ConvertFrom-Json -ErrorAction Stop
            if ([string]::Equals([string]$previousHealth.ownerMarker, $script:OwnerMarker, [StringComparison]::Ordinal) -and
                [string]::Equals([string]$previousHealth.managedBy, $script:TaskName, [StringComparison]::Ordinal)) {
                $lastSuccessUtc = [string]$previousHealth.lastSuccessUtc
                $lastError = [string]$previousHealth.lastError
                $lastErrorUtc = [string]$previousHealth.lastErrorUtc
            }
        }
    } catch { }
    $script:lastSuccessUtc = $lastSuccessUtc
    $script:lastError = $lastError
    $script:lastErrorUtc = $lastErrorUtc
    $cursorRecoveryState = New-CuaCursorRecoveryState
    $script:ManagedCursorPaths = @()
    try { $script:ManagedCursorPaths = @(Get-ManagedCuaExecutablePaths -Root $RuntimeRoot) }
    catch { Write-MaintenanceLog -Event 'cursor-runtime-discovery-error' -Path $RuntimeRoot -Detail $_.Exception.Message }
    Set-WatcherHealth -State 'Running' -Root $RuntimeRoot -LastSuccessUtc $lastSuccessUtc -LastError $lastError -LastErrorUtc $lastErrorUtc
    function Invoke-WatcherScan {
        try {
            Invoke-RuntimeScan -Root $RuntimeRoot -DllSource $source -Quiet
            try { $script:ManagedCursorPaths = @(Get-ManagedCuaExecutablePaths -Root $RuntimeRoot) }
            catch { Write-MaintenanceLog -Event 'cursor-runtime-discovery-error' -Path $RuntimeRoot -Detail $_.Exception.Message }
            $script:lastSuccessUtc = (Get-Date).ToUniversalTime().ToString('o')
            $script:lastError = $null
            $script:lastErrorUtc = $null
            Set-WatcherHealth -State 'Running' -Root $RuntimeRoot -LastSuccessUtc $script:lastSuccessUtc -LastError $null -LastErrorUtc $null
        } catch {
            $script:lastError = $_.Exception.Message
            $script:lastErrorUtc = (Get-Date).ToUniversalTime().ToString('o')
            Write-MaintenanceLog -Event 'watcher-scan-error' -Path $RuntimeRoot -Detail $script:lastError
            Set-WatcherHealth -State 'Degraded' -Root $RuntimeRoot -LastSuccessUtc $script:lastSuccessUtc -LastError $script:lastError -LastErrorUtc $script:lastErrorUtc
        }
    }
    try {
        while ($true) {
            $event = Wait-Event -Timeout 2
            if ($event) {
                Remove-Event -EventIdentifier $event.EventIdentifier -ErrorAction SilentlyContinue
                while ($true) {
                    $queued = Get-Event -ErrorAction SilentlyContinue | Select-Object -First 1
                    if (-not $queued) { break }
                    Remove-Event -EventIdentifier $queued.EventIdentifier -ErrorAction SilentlyContinue
                }
                if (((Get-Date) - $lastScan).TotalSeconds -ge 2) {
                    Invoke-WatcherScan
                    $lastScan = Get-Date
                }
            }
            if (((Get-Date) - $lastScan).TotalSeconds -ge 15) {
                Invoke-WatcherScan
                $lastScan = Get-Date
            }
            if ($script:ManagedCursorPaths.Count -gt 0) {
                try { Invoke-CuaCursorRecoveryCheck -ExecutablePaths $script:ManagedCursorPaths -State $cursorRecoveryState }
                catch { Write-MaintenanceLog -Event 'cursor-monitor-error' -Path $RuntimeRoot -Detail $_.Exception.Message }
            }
        }
    } finally {
        Set-WatcherHealth -State 'Stopped' -Root $RuntimeRoot -LastSuccessUtc $script:lastSuccessUtc -LastError $script:lastError -LastErrorUtc $script:lastErrorUtc
        foreach ($id in $ids) { Unregister-Event -SourceIdentifier $id -ErrorAction SilentlyContinue }
        $watcher.EnableRaisingEvents = $false
        $watcher.Dispose()
    }
}

switch ($Action) {
    'Install' {
        $source = if ($SourceDll) { (Resolve-Path -LiteralPath $SourceDll).Path } else { Join-Path $PSScriptRoot 'dist\version.dll' }
        Install-ManagedPayload -DllSource $source -RuntimeRootPath $RuntimeRoot -SkipTask:$NoScheduledTask
    }
    'Status' { Show-Status }
    'Uninstall' {
        $task = Get-ScheduledTask -TaskName $script:TaskName -ErrorAction SilentlyContinue
        if (-not $task) { Write-Output "Watcher task is not registered. Managed backups and payload were preserved at $StateRoot"; break }
        $expectedScript = Join-Path $StateRoot 'persistence.ps1'
        if (-not (Test-ManagedScheduledTask -Task $task -ExpectedScriptPath $expectedScript)) {
            throw "Refusing to stop or remove a task that does not match this install's description, action, current user, and Limited run level: $($task.TaskName)"
        }
        [void](Stop-VerifiedManagedTask -ExpectedScriptPath $expectedScript)
        Unregister-ScheduledTask -TaskName $script:TaskName -Confirm:$false
        Write-MaintenanceLog -Event 'watcher-task-removed' -Path $script:TaskName -Detail 'Task removed; managed payload, install records, DLLs, and backups were retained.'
        Write-Output "Removed watcher task. Payload and backups remain at $StateRoot"
    }
    'Watch' { Start-RuntimeWatcher }
    'ScanOnce' { Invoke-RuntimeScan -Root $RuntimeRoot -DllSource (Get-SourceDllPath) }
}
