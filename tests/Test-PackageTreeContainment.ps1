$ErrorActionPreference = 'Stop'

$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$packageScript = Get-Content -LiteralPath (Join-Path $workspace 'tools/package.ps1') -Raw
$pattern = "(?s)Add-Type -ReferencedAssemblies 'System.IO.Compression', 'System.IO.Compression.FileSystem' -TypeDefinition @'\r?\n(?<source>.*?)\r?\n'@"
$match = [regex]::Match($packageScript, $pattern)
if (-not $match.Success) {
    throw 'Could not locate the package engine source for the isolated containment fixture.'
}

Add-Type -ReferencedAssemblies 'System.IO.Compression', 'System.IO.Compression.FileSystem' -TypeDefinition $match.Groups['source'].Value

$fixture = Join-Path $env:TEMP ('calradiaforge-package-tree-' + [Guid]::NewGuid().ToString('N'))
$outside = Join-Path $fixture 'outside'
$source = Join-Path $fixture 'source'
$moduleSource = Join-Path $fixture 'module-source'
$target = Join-Path $fixture 'target'
$moduleTarget = Join-Path $fixture 'module-target'
$directoryJunction = Join-Path $source 'redirected-directory'
$moduleJunction = Join-Path $moduleSource 'redirected-directory'
$reparseFailure = 'Package source path cannot contain a reparse point:'

try {
    New-Item -ItemType Directory -Path $outside, $source, $moduleSource | Out-Null
    Set-Content -LiteralPath (Join-Path $outside 'external-marker.txt') -Value 'outside' -NoNewline
    Set-Content -LiteralPath (Join-Path $source 'owned.txt') -Value 'owned' -NoNewline
    Set-Content -LiteralPath (Join-Path $moduleSource 'owned.txt') -Value 'owned' -NoNewline
    New-Item -ItemType Junction -Path $directoryJunction -Target $outside | Out-Null
    New-Item -ItemType Junction -Path $moduleJunction -Target $outside | Out-Null

    $treeCopyRejected = $false
    try {
        [FastPackageEngine]::FastTreeCopy(@($source), $target, $fixture)
    }
    catch [IO.IOException] {
        if ($_.Exception.Message -notlike "$reparseFailure*") { throw }
        $treeCopyRejected = $true
    }
    if (-not $treeCopyRejected) {
        throw 'FastTreeCopy accepted a source directory junction.'
    }
    if (Test-Path -LiteralPath (Join-Path $target 'source/external-marker.txt')) {
        throw 'FastTreeCopy copied a file reached through a source junction.'
    }

    $moduleCopyRejected = $false
    try {
        [FastPackageEngine]::CopyModuleDirectory($moduleSource, $moduleTarget)
    }
    catch [IO.IOException] {
        if ($_.Exception.Message -notlike "$reparseFailure*") { throw }
        $moduleCopyRejected = $true
    }
    if (-not $moduleCopyRejected) {
        throw 'CopyModuleDirectory accepted a source directory junction.'
    }
    if (Test-Path -LiteralPath (Join-Path $moduleTarget 'external-marker.txt')) {
        throw 'CopyModuleDirectory copied a file reached through a source junction.'
    }

    Write-Output 'Package source-tree reparse-point guards passed.'
}
finally {
    foreach ($junction in @($directoryJunction, $moduleJunction)) {
        if (Test-Path -LiteralPath $junction) {
            [IO.Directory]::Delete($junction)
        }
    }
    if (([IO.Path]::GetFullPath($fixture)).StartsWith(([IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'), [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction SilentlyContinue
    }
}
