param(
    [string]$GamePath = 'C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord',
    [string]$PythonPath = 'python',
    [switch]$SkipInstalledScan
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
Push-Location $workspace
try {
    & $PythonPath tools/generate_game_icons.py
    if ($LASTEXITCODE -ne 0) { throw 'Game icon generation failed.' }
    & $PythonPath tools/generate_assets.py
    if ($LASTEXITCODE -ne 0) { throw 'Asset generation failed.' }
    & $PythonPath tools/regenerate_language_resources.py
    if ($LASTEXITCODE -ne 0) { throw 'Native resource generation failed.' }
    & $PythonPath tools/generate_desktop_resources.py
    if ($LASTEXITCODE -ne 0) { throw 'Desktop resource generation failed.' }
    & $PythonPath tools/validate_game_icon_assets.py
    if ($LASTEXITCODE -ne 0) { throw 'Game icon metadata validation failed.' }
    dotnet build CalradiaForge.sln -c Release "-p:GamePath=$GamePath" -v:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    New-Item -ItemType Directory -Force artifacts | Out-Null
    $runner = Join-Path $PSScriptRoot 'Run-CalradiaForge-Tests.bat'
    if (-not $SkipInstalledScan) {
        $env:FORGE_TEST_MODULES = Join-Path $GamePath 'Modules'
    } else {
        Remove-Item Env:\FORGE_TEST_MODULES -ErrorAction SilentlyContinue
    }
    try {
        & $runner --skip-build --no-pause 2>&1 | Tee-Object artifacts/test-results.txt
        if ($LASTEXITCODE -ne 0) { throw "Test runner failed with exit code $LASTEXITCODE." }
    } finally {
        Remove-Item Env:\FORGE_TEST_MODULES -ErrorAction SilentlyContinue
    }
    & $PythonPath tools/audit_localization.py
    if ($LASTEXITCODE -ne 0) { throw 'Localization audit failed.' }
    & $PythonPath tools/desktop_visual_matrix.py
    if ($LASTEXITCODE -ne 0) { throw 'Desktop visual matrix failed.' }
    Write-Output 'Build and automated tests completed. In-game validation is a separate step.'
} finally { Pop-Location }
