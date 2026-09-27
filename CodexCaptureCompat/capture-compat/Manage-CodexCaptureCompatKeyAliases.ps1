[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Status', 'Test', 'Apply', 'Rollback')]
    [string]$Action,
    [Parameter(Mandatory = $true)]
    [string]$RuntimePath,
    [Parameter(Mandatory = $true)]
    [string]$PackagePath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$script:ExpectedRuntimeId = 'b35a688736d56912'
$script:ExpectedNodeVersion = '24.21.0-cua.1'
$script:ExpectedArchiveVersion = '0.0.24/20260924074400-f52ea85e2a98'
$script:ExpectedPackageVersion = '0.7.4'
$script:ExpectedPackageJsonSha256 = '14F88DA4A41E71B878B8045C3FD77D74CE6E9BA212007A407DE98A7D92B2B246'
$script:ExpectedInputSha256 = '617D8E6E18FDDE25F06D4CBA2C84C994E076E05F30A8C55D09E918401C48B171'
$script:ExpectedPatchedSha256 = 'F40CEF88BF48349EA769FE6A10D290D2B25F1623D504C37EE11BA30A10CD4517'
$script:Anchor = 'function b(e){return e}'
$script:Replacement = 'function b(e){const t=({control:"Ctrl_L",ctrl:"Ctrl_L",control_l:"Ctrl_L",control_r:"Ctrl_R",ctrl_l:"Ctrl_L",ctrl_r:"Ctrl_R"})[e.toLowerCase()];return t||e}'
$script:OwnerMarker = 'CodexCaptureCompat.SkyKeyAliases.v1'
$script:StateRoot = Join-Path $env:LOCALAPPDATA 'OpenAI\Codex\CodexCaptureCompat\SkyKeyAliases'
$script:Encoding = New-Object System.Text.UTF8Encoding($false, $true)
$script:WriteMutex = $null
$script:WriteMutexAcquired = $false

function Get-Sha256 {
    param([Parameter(Mandatory = $true)][string]$Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
    try { return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
    finally { $stream.Dispose(); $sha.Dispose() }
}

function Get-BytesSha256 {
    param([Parameter(Mandatory = $true)][byte[]]$Bytes)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($Bytes)).Replace('-', '') }
    finally { $sha.Dispose() }
}

function Get-FullPath {
    param([Parameter(Mandatory = $true)][string]$Path)
    return [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Test-ReparseChain {
    param([Parameter(Mandatory = $true)][string]$Path)
    $current = [IO.Path]::GetFullPath($Path)
    while ($current) {
        if (Test-Path -LiteralPath $current) {
            $item = Get-Item -LiteralPath $current -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Refusing to follow a reparse point in managed path: $current"
            }
        }
        $parent = [IO.Directory]::GetParent($current)
        if (-not $parent -or [string]::Equals($parent.FullName, $current, [StringComparison]::OrdinalIgnoreCase)) { break }
        $current = $parent.FullName
    }
}

function Assert-ExactTarget {
    $runtimeBase = Join-Path $env:LOCALAPPDATA 'OpenAI\Codex\runtimes\cua_node'
    $expectedRuntime = Join-Path $runtimeBase $script:ExpectedRuntimeId
    $expectedPackage = Join-Path $expectedRuntime 'bin\node_modules\@oai\sky'
    $actualRuntime = Get-FullPath $RuntimePath
    $actualPackage = Get-FullPath $PackagePath
    if (-not [string]::Equals($actualRuntime, (Get-FullPath $expectedRuntime), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Runtime path is not the allowlisted runtime $script:ExpectedRuntimeId. Refusing: $actualRuntime"
    }
    if (-not [string]::Equals($actualPackage, (Get-FullPath $expectedPackage), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Package path is not the exact allowlisted @oai/sky package path. Refusing: $actualPackage"
    }
    if (-not (Test-Path -LiteralPath $actualRuntime -PathType Container) -or -not (Test-Path -LiteralPath $actualPackage -PathType Container)) {
        throw 'The exact runtime or package directory does not exist.'
    }
    Test-ReparseChain $actualRuntime
    Test-ReparseChain $actualPackage

    $runtimeManifestPath = Join-Path $actualRuntime 'manifest.json'
    $packageJsonPath = Join-Path $actualPackage 'package.json'
    foreach ($required in @($runtimeManifestPath, $packageJsonPath)) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required version manifest is missing: $required" }
        Test-ReparseChain $required
    }

    $runtimeManifest = Get-Content -LiteralPath $runtimeManifestPath -Raw | ConvertFrom-Json -ErrorAction Stop
    if ([string]$runtimeManifest.platform -cne 'windows' -or [string]$runtimeManifest.arch -cne 'x64' -or
        [string]$runtimeManifest.node_version -cne $script:ExpectedNodeVersion -or
        [string]$runtimeManifest.runtime_archive_version -cne $script:ExpectedArchiveVersion) {
        throw 'Runtime manifest version/platform does not match the allowlisted profile.'
    }
    $packageJsonHash = Get-Sha256 $packageJsonPath
    if ($packageJsonHash -cne $script:ExpectedPackageJsonSha256) { throw "Unknown @oai/sky package.json SHA-256: $packageJsonHash" }
    $packageJson = Get-Content -LiteralPath $packageJsonPath -Raw | ConvertFrom-Json -ErrorAction Stop
    if ([string]$packageJson.name -cne '@oai/sky' -or [string]$packageJson.version -cne $script:ExpectedPackageVersion) {
        throw 'Package identity/version does not match the allowlisted @oai/sky 0.7.4 profile.'
    }

    $target = Join-Path $actualPackage 'dist\project\cua\sky_js\src\targets\windows\internal\computer_use_client_base.js'
    if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { throw "Expected adapter file is missing: $target" }
    Test-ReparseChain $target
    return [pscustomobject]@{ Runtime = $actualRuntime; Package = $actualPackage; Target = $target; RuntimeManifest = $runtimeManifest }
}

function Get-ProspectiveBytes {
    param([Parameter(Mandatory = $true)][byte[]]$InputBytes)
    $anchorBytes = $script:Encoding.GetBytes($script:Anchor)
    $replacementBytes = $script:Encoding.GetBytes($script:Replacement)
    $matches = New-Object 'System.Collections.Generic.List[int]'
    for ($i = 0; $i -le $InputBytes.Length - $anchorBytes.Length; $i++) {
        $same = $true
        for ($j = 0; $j -lt $anchorBytes.Length; $j++) {
            if ($InputBytes[$i + $j] -ne $anchorBytes[$j]) { $same = $false; break }
        }
        if ($same) { $matches.Add($i); $i += ($anchorBytes.Length - 1) }
    }
    if ($matches.Count -ne 1) { throw "Expected exactly one patch anchor '$script:Anchor'; found $($matches.Count)." }
    $start = $matches[0]
    $prefixLength = $start
    $suffixStart = $start + $anchorBytes.Length
    $suffixLength = $InputBytes.Length - $suffixStart
    $output = New-Object byte[] ($prefixLength + $replacementBytes.Length + $suffixLength)
    [Array]::Copy($InputBytes, 0, $output, 0, $prefixLength)
    [Array]::Copy($replacementBytes, 0, $output, $prefixLength, $replacementBytes.Length)
    [Array]::Copy($InputBytes, $suffixStart, $output, $prefixLength + $replacementBytes.Length, $suffixLength)
    return ,$output
}

function Get-ManifestPath { return Join-Path $script:StateRoot 'key-aliases-manifest.json' }
function Get-BackupPath { return Join-Path $script:StateRoot ("computer_use_client_base.js.original.{0}.bak" -f $script:ExpectedInputSha256) }

function Ensure-StateRoot {
    if (-not (Test-Path -LiteralPath $script:StateRoot -PathType Container)) {
        $parent = Split-Path -Parent $script:StateRoot
        Test-ReparseChain $parent
        New-Item -ItemType Directory -Path $script:StateRoot -Force | Out-Null
    }
    Test-ReparseChain $script:StateRoot
}

function Read-Manifest {
    $path = Get-ManifestPath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $null }
    Test-ReparseChain $path
    try { $manifest = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -ErrorAction Stop }
    catch { throw "Managed key-alias manifest is corrupt; preserved at $path. $($_.Exception.Message)" }
    $expectedTarget = Get-FullPath (Join-Path (Get-FullPath $PackagePath) 'dist\project\cua\sky_js\src\targets\windows\internal\computer_use_client_base.js')
    if ([int]$manifest.schemaVersion -ne 1 -or [string]$manifest.ownerMarker -cne $script:OwnerMarker -or
        [string]$manifest.runtimeId -cne $script:ExpectedRuntimeId -or [string]$manifest.packageVersion -cne $script:ExpectedPackageVersion -or
        [string]$manifest.inputSha256 -cne $script:ExpectedInputSha256 -or [string]$manifest.patchedSha256 -cne $script:ExpectedPatchedSha256 -or
        -not [string]::Equals([string]$manifest.runtimePath, (Get-FullPath $RuntimePath), [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([string]$manifest.packagePath, (Get-FullPath $PackagePath), [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([string]$manifest.targetPath, $expectedTarget, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([string]$manifest.backupPath, (Get-FullPath (Get-BackupPath)), [StringComparison]::OrdinalIgnoreCase) -or
        [string]$manifest.backupSha256 -cne $script:ExpectedInputSha256 -or [string]$manifest.state -notin @('Prepared', 'Applied')) {
        throw "Managed key-alias manifest has unknown ownership or identity; preserved at $path."
    }
    return $manifest
}

function Enter-WriteMutex {
    $mutexName = "Local\CodexCaptureCompat.SkyKeyAliases.$script:ExpectedRuntimeId"
    $mutex = [System.Threading.Mutex]::new($false, $mutexName)
    $acquired = $false
    try {
        $acquired = $mutex.WaitOne([TimeSpan]::FromSeconds(3))
    } catch [System.Threading.AbandonedMutexException] {
        # An abandoned mutex is acquired by this thread. The manifest/hash state
        # checks below decide whether an interrupted operation is recoverable.
        $acquired = $true
        Write-Warning 'Recovered the abandoned key-alias manager mutex; validating transaction state before any write.'
    }
    if (-not $acquired) {
        $mutex.Dispose()
        throw 'Another key-alias Apply/Rollback is active; timed out waiting for its mutex after 3 seconds.'
    }
    $script:WriteMutex = $mutex
    $script:WriteMutexAcquired = $true
}

function Exit-WriteMutex {
    if ($script:WriteMutex -is [System.Threading.Mutex]) {
        try {
            if ($script:WriteMutexAcquired) { $script:WriteMutex.ReleaseMutex() }
        } finally {
            $script:WriteMutex.Dispose()
            $script:WriteMutex = $null
            $script:WriteMutexAcquired = $false
        }
    }
}

function Write-ManifestAtomic {
    param([Parameter(Mandatory = $true)][object]$Manifest)
    Ensure-StateRoot
    $path = Get-ManifestPath
    $temp = Join-Path $script:StateRoot ("manifest.{0}.tmp" -f [Guid]::NewGuid().ToString('N'))
    $json = $Manifest | ConvertTo-Json -Depth 5
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    try {
        [IO.File]::WriteAllText($temp, $json, $utf8)
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            $backupTemp = "$path.replace-backup"
            if (Test-Path -LiteralPath $backupTemp) { throw "Unexpected manifest replacement backup exists: $backupTemp" }
            [IO.File]::Replace($temp, $path, $backupTemp)
            if (Test-Path -LiteralPath $backupTemp) { Remove-Item -LiteralPath $backupTemp -Force }
        } else { [IO.File]::Move($temp, $path) }
    } finally {
        if (Test-Path -LiteralPath $temp -PathType Leaf) { Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue }
    }
}

function Ensure-VerifiedBackup {
    param([Parameter(Mandatory = $true)][string]$Target)
    Ensure-StateRoot
    $backup = Get-BackupPath
    if (Test-Path -LiteralPath $backup -PathType Leaf) {
        Test-ReparseChain $backup
        $hash = Get-Sha256 $backup
        if ($hash -cne $script:ExpectedInputSha256) { throw "Existing backup hash is unknown; preserving it: $backup ($hash)" }
        return $backup
    }
    $currentHash = Get-Sha256 $Target
    if ($currentHash -cne $script:ExpectedInputSha256) { throw "Target hash changed before backup; expected $script:ExpectedInputSha256, found $currentHash" }
    [IO.File]::Copy($Target, $backup, $false)
    Test-ReparseChain $backup
    $backupHash = Get-Sha256 $backup
    if ($backupHash -cne $script:ExpectedInputSha256) { throw "Backup failed SHA-256 verification; refusing to patch: $backup ($backupHash)" }
    return $backup
}

function Replace-TargetAtomically {
    param([Parameter(Mandatory = $true)][string]$Target, [Parameter(Mandatory = $true)][byte[]]$Bytes, [Parameter(Mandatory = $true)][string]$ExpectedHash)
    $temp = Join-Path (Split-Path -Parent $Target) ("computer_use_client_base.js.{0}.tmp" -f [Guid]::NewGuid().ToString('N'))
    $replaceBackup = "$temp.replace-backup"
    $replacementVerified = $false
    try {
        [IO.File]::WriteAllBytes($temp, $Bytes)
        if ((Get-Sha256 $temp) -cne $ExpectedHash) { throw 'Temporary replacement bytes failed SHA-256 verification.' }
        $currentTargetHash = Get-Sha256 $Target
        if ($currentTargetHash -cne $script:ExpectedInputSha256 -and $currentTargetHash -cne $script:ExpectedPatchedSha256) {
            throw 'Target changed to an unknown hash immediately before replacement; refusing.'
        }
        [IO.File]::Replace($temp, $Target, $replaceBackup)
        $actualTargetHash = Get-Sha256 $Target
        if ($actualTargetHash -cne $ExpectedHash) { throw "Replacement verification failed for target: $Target; actual SHA-256 $actualTargetHash" }
        $replacementVerified = $true
    } finally {
        if (Test-Path -LiteralPath $temp -PathType Leaf) { Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue }
        if (Test-Path -LiteralPath $replaceBackup -PathType Leaf) {
            if ($replacementVerified) {
                Remove-Item -LiteralPath $replaceBackup -Force -ErrorAction SilentlyContinue
                if (Test-Path -LiteralPath $replaceBackup -PathType Leaf) {
                    Write-Warning "Replacement was verified, but its transient sidecar could not be removed; preserved at $replaceBackup"
                }
            } else {
                Write-Warning "Replacement did not complete and its pre-replacement file is preserved at $replaceBackup"
            }
        }
    }
}

function Show-Status {
    param([Parameter(Mandatory = $true)][object]$TargetInfo)
    $hash = Get-Sha256 $TargetInfo.Target
    "Runtime: $($TargetInfo.Runtime)"
    "Package: @oai/sky $script:ExpectedPackageVersion"
    "Adapter SHA-256: $hash"
    if ($hash -ceq $script:ExpectedInputSha256) {
        $manifest = Read-Manifest
        if ($null -eq $manifest) {
            'State: original allowlisted adapter; no manager transaction manifest exists and no patch is applied.'
            return
        }
        $backup = Get-BackupPath
        Test-ReparseChain $backup
        if (-not (Test-Path -LiteralPath $backup -PathType Leaf)) {
            throw 'Original adapter is present with a manager manifest, but the verified backup is missing; transaction state is inconsistent.'
        }
        $backupHash = Get-Sha256 $backup
        if ($backupHash -cne $script:ExpectedInputSha256) {
            throw "Original adapter is present with a manager manifest, but backup SHA-256 is invalid: $backupHash"
        }
        if ([string]$manifest.state -ceq 'Prepared') {
            'State: interrupted Prepared transaction; original adapter remains intact and the backup SHA-256 is verified. Status made no changes.'
            return
        }
        throw 'Transaction state is inconsistent: manifest says Applied while the original adapter hash is present. Status made no changes.'
    }
    if ($hash -cne $script:ExpectedPatchedSha256) { throw "Unknown adapter SHA-256; refusing to interpret or change it: $hash" }
    $manifest = Read-Manifest
    if ($null -eq $manifest) { throw 'Adapter has the known patched hash but no manager ownership manifest; preserving it.' }
    $backup = Get-BackupPath
    Test-ReparseChain $backup
    if (-not (Test-Path -LiteralPath $backup -PathType Leaf) -or (Get-Sha256 $backup) -cne $script:ExpectedInputSha256) {
        throw 'Known patch is present but its original backup is missing or unverifiable.'
    }
    "State: $($manifest.state); verified original backup SHA-256 $script:ExpectedInputSha256."
}

try {
    $targetInfo = Assert-ExactTarget
    if ($Action -eq 'Apply' -or $Action -eq 'Rollback') { Enter-WriteMutex }
    $observedHash = Get-Sha256 $targetInfo.Target
    if ($Action -eq 'Status') { Show-Status $targetInfo; exit 0 }

    if ($Action -eq 'Test') {
        if ($observedHash -cne $script:ExpectedInputSha256) { throw "Test requires the exact original adapter hash; found $observedHash" }
        $inputBytes = [IO.File]::ReadAllBytes($targetInfo.Target)
        $prospective = Get-ProspectiveBytes -InputBytes $inputBytes
        $prospectiveHash = Get-BytesSha256 -Bytes $prospective
        "Input SHA-256: $observedHash"
        "Prospective output SHA-256: $prospectiveHash"
        "Unique anchor count: 1"
        'Alias map: Control/Ctrl/Control_L/Ctrl_L -> Ctrl_L; Control_R/Ctrl_R -> Ctrl_R; other key names pass through unchanged.'
        if ($script:ExpectedPatchedSha256 -ne '__PATCH_HASH_PENDING__' -and $prospectiveHash -cne $script:ExpectedPatchedSha256) {
            throw "Output hash differs from pinned patch hash $script:ExpectedPatchedSha256"
        }
        exit 0
    }

    if ($Action -eq 'Apply') {
        if ($observedHash -ceq $script:ExpectedPatchedSha256) {
            Show-Status $targetInfo
            exit 0
        }
        if ($observedHash -cne $script:ExpectedInputSha256) { throw "Apply refuses unknown adapter hash: $observedHash" }
        if ($script:ExpectedPatchedSha256 -eq '__PATCH_HASH_PENDING__') { throw 'Patch output hash has not been pinned in this manager build.' }
        $existingManifest = Read-Manifest
        if ($null -ne $existingManifest) { throw 'A prior managed manifest exists while the adapter is original; inspect status and rollback before applying again.' }
        $inputBytes = [IO.File]::ReadAllBytes($targetInfo.Target)
        $outputBytes = Get-ProspectiveBytes -InputBytes $inputBytes
        $outputHash = Get-BytesSha256 -Bytes $outputBytes
        if ($outputHash -cne $script:ExpectedPatchedSha256) { throw "Generated patch hash does not match pinned hash: $outputHash" }
        $backup = Ensure-VerifiedBackup -Target $targetInfo.Target
        $manifest = [pscustomobject]@{
            schemaVersion = 1
            ownerMarker = $script:OwnerMarker
            state = 'Prepared'
            runtimeId = $script:ExpectedRuntimeId
            runtimePath = $targetInfo.Runtime
            packagePath = $targetInfo.Package
            packageVersion = $script:ExpectedPackageVersion
            targetPath = (Get-FullPath $targetInfo.Target)
            inputSha256 = $script:ExpectedInputSha256
            patchedSha256 = $script:ExpectedPatchedSha256
            backupPath = (Get-FullPath $backup)
            backupSha256 = $script:ExpectedInputSha256
            updatedAtUtc = [DateTime]::UtcNow.ToString('o')
        }
        Write-ManifestAtomic $manifest
        Replace-TargetAtomically -Target $targetInfo.Target -Bytes $outputBytes -ExpectedHash $script:ExpectedPatchedSha256
        $manifest.state = 'Applied'
        $manifest.updatedAtUtc = [DateTime]::UtcNow.ToString('o')
        Write-ManifestAtomic $manifest
        Show-Status $targetInfo
        exit 0
    }

    if ($Action -eq 'Rollback') {
        if ($observedHash -ceq $script:ExpectedInputSha256) {
            $prepared = Read-Manifest
            if ($null -ne $prepared) {
                $backup = Get-BackupPath
                Test-ReparseChain $backup
                if ((Get-Sha256 $backup) -cne $script:ExpectedInputSha256) { throw 'Cannot clear a prepared transaction: verified original backup missing or wrong.' }
                Remove-Item -LiteralPath (Get-ManifestPath) -Force
            }
            'State: original adapter already present; no replacement was needed.'
            exit 0
        }
        if ($observedHash -cne $script:ExpectedPatchedSha256) { throw "Rollback refuses unknown adapter hash: $observedHash" }
        $manifest = Read-Manifest
        if ($null -eq $manifest) { throw "Rollback requires this manager's valid ownership manifest." }
        $backup = Get-BackupPath
        Test-ReparseChain $backup
        if (-not (Test-Path -LiteralPath $backup -PathType Leaf)) { throw "Verified original backup is missing: $backup" }
        $backupHash = Get-Sha256 $backup
        if ($backupHash -cne $script:ExpectedInputSha256) { throw "Backup SHA-256 mismatch; preserving target and backup: $backupHash" }
        $backupBytes = [IO.File]::ReadAllBytes($backup)
        Replace-TargetAtomically -Target $targetInfo.Target -Bytes $backupBytes -ExpectedHash $script:ExpectedInputSha256
        if ((Get-Sha256 $targetInfo.Target) -cne $script:ExpectedInputSha256) { throw 'Rollback did not restore the verified original hash.' }
        Remove-Item -LiteralPath (Get-ManifestPath) -Force
        "Rollback complete. Restored SHA-256: $script:ExpectedInputSha256"
        exit 0
    }

    throw "Unhandled action: $Action"
} catch {
    [Console]::Error.WriteLine("Key-alias manager failed closed: $($_.Exception.Message)")
    exit 1
} finally {
    Exit-WriteMutex
}
