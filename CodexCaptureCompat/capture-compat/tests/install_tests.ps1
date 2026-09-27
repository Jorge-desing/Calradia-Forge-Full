$ErrorActionPreference = 'Stop'
$sha256 = [Security.Cryptography.SHA256]::Create()
function Get-Sha256File {
    param([Parameter(Mandatory)][string]$Path)
    $stream = [IO.File]::OpenRead($Path)
    try { return [BitConverter]::ToString($script:sha256.ComputeHash($stream)).Replace('-', '') }
    finally { $stream.Dispose() }
}
$project = Split-Path -Parent $PSScriptRoot
$fixture = Join-Path $project ('validation\install-fixture-' + [Guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Path $fixture -Force | Out-Null
    $helper = Join-Path $fixture 'codex-computer-use.exe'
    $dll = Join-Path $fixture 'version.dll'
    $record = Join-Path $fixture 'codex-capture-compat.install.json'
    $installer = Join-Path $project 'install.ps1'
    Set-Content -LiteralPath $helper -Value 'Non-executable install test fixture.'
    & $installer -HelperPath $helper -WhatIf
    if ((Test-Path -LiteralPath $dll) -or (Test-Path -LiteralPath $record)) { throw 'WhatIf wrote files.' }
    & $installer -HelperPath $helper
    if (-not (Test-Path -LiteralPath $record)) { throw 'Install record missing.' }
    if ((Get-Sha256File $dll) -ne (Get-Sha256File (Join-Path $project 'dist\version.dll'))) { throw 'Copied DLL differs.' }
    & $installer -HelperPath $helper
    & $installer -HelperPath $helper -Action Uninstall
    if ((Test-Path -LiteralPath $dll) -or (Test-Path -LiteralPath $record)) { throw 'Uninstall left owned files.' }
    Set-Content -LiteralPath $dll -Value 'Foreign DLL fixture - must not be overwritten or removed.'
    $foreignHash = Get-Sha256File $dll
    $rejected = $false
    try { & $installer -HelperPath $helper } catch { $rejected = $true }
    if (-not $rejected -or (Get-Sha256File $dll) -ne $foreignHash) { throw 'Foreign DLL overwrite guard failed.' }
    $rejected = $false
    try { & $installer -HelperPath $helper -Action Uninstall } catch { $rejected = $true }
    if (-not $rejected -or (Get-Sha256File $dll) -ne $foreignHash) { throw 'Unowned DLL removal guard failed.' }
    'PASS: WhatIf, copy hash, idempotency, uninstall, foreign-DLL preservation.'
} finally {
    $validationRoot = Join-Path $project 'validation'
    if ((Test-Path -LiteralPath $fixture -PathType Container) -and (Test-Path -LiteralPath $validationRoot -PathType Container)) {
        $resolvedRoot = (Resolve-Path -LiteralPath $validationRoot).Path.TrimEnd('\')
        $resolvedFixture = (Resolve-Path -LiteralPath $fixture).Path
        if ($resolvedFixture.StartsWith($resolvedRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
        } else {
            throw "Refusing to remove fixture outside validation folder: $resolvedFixture"
        }
    }
    $sha256.Dispose()
}
