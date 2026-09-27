$ErrorActionPreference = 'Stop'
$toolsDirectory = $PSScriptRoot
$preparePath = Join-Path $toolsDirectory 'Prepare-CalradiaForge-ResourceBrowser.ps1'
$deployPath = Join-Path $toolsDirectory 'deploy_to_game.ps1'
$expected = '/singleplayer _MODULES_*Native*SandBoxCore*Sandbox*StoryMode*CustomBattle*CalradiaForge*_MODULES_'

foreach ($path in @($preparePath, $deployPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required launch script is missing: $path"
    }
}

$prepare = Get-Content -LiteralPath $preparePath -Raw
$deploy = Get-Content -LiteralPath $deployPath -Raw

if ($prepare -notmatch [regex]::Escape("`$editorModuleArguments = '$expected'")) {
    throw 'Resource Browser preparation no longer defines the exact Modding Kit module argument list.'
}
if ($prepare -notmatch [regex]::Escape('Start-Process -FilePath $editorExecutable -ArgumentList $editorModuleArguments -WorkingDirectory $editorDirectory')) {
    throw 'Resource Browser preparation does not pass the module argument list to Start-Process.'
}
if ($deploy -notmatch [regex]::Escape("`$arguments = '$expected'")) {
    throw 'The preparation and deployment scripts have diverged from the established module argument list.'
}

Write-Host 'PASS: Resource Browser launch arguments exactly match deploy_to_game.ps1.'
Write-Host 'PASS: This check is static and did not launch Bannerlord or modify installed files.'
