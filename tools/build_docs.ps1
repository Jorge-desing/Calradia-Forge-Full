param()
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
Push-Location $workspace
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'DocFX tool restore failed.' }
    dotnet docfx docs-site/docfx.json
    if ($LASTEXITCODE -ne 0) { throw 'DocFX build failed.' }
    $site = Join-Path $workspace 'artifacts/docfx-site/index.html'
    if (-not (Test-Path -LiteralPath $site)) { throw 'DocFX did not create the static site index.' }
    Write-Output $site
} finally { Pop-Location }
