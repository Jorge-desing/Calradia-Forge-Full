[CmdletBinding()]
param(
    [string]$GameRoot = '',
    [string]$SourceModulePath = '',
    [string]$TpacToolDirectory = '',
    [switch]$DryRun,
    [switch]$LaunchEditor,
    [switch]$CollectTpac
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$python = Join-Path $repositoryRoot '.venv\Scripts\python.exe'
$workspaceModule = Join-Path $repositoryRoot 'modules\CalradiaForge'
if ([string]::IsNullOrWhiteSpace($SourceModulePath)) { $SourceModulePath = $workspaceModule }
$sourceModule = [IO.Path]::GetFullPath($SourceModulePath)
$usingWorkspaceSource = $sourceModule.Equals([IO.Path]::GetFullPath($workspaceModule), [StringComparison]::OrdinalIgnoreCase)
if ($CollectTpac -and $LaunchEditor) {
    throw 'Use either -CollectTpac or -LaunchEditor for one run, not both.'
}

if ([string]::IsNullOrWhiteSpace($GameRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($env:BANNERLORD_GAME_DIR)) {
        $GameRoot = $env:BANNERLORD_GAME_DIR
    }
    else {
        $GameRoot = Join-Path ${env:ProgramFiles(x86)} 'Steam\steamapps\common\Mount & Blade II Bannerlord'
    }
}

$GameRoot = [IO.Path]::GetFullPath($GameRoot)
$installedModule = Join-Path $GameRoot 'Modules\CalradiaForge'
$editorDirectory = Join-Path $GameRoot 'bin\Win64_Shipping_wEditor'
$editorExecutable = Join-Path $editorDirectory 'Bannerlord.exe'
$editorModuleArguments = '/singleplayer _MODULES_*Native*SandBoxCore*Sandbox*StoryMode*CustomBattle*CalradiaForge*_MODULES_'
$inspectScript = Join-Path $PSScriptRoot 'Inspect-CalradiaForge-Tpac.ps1'
if ([string]::IsNullOrWhiteSpace($TpacToolDirectory)) {
    $TpacToolDirectory = Join-Path $repositoryRoot 'TpacTool\bin'
}

function Get-Sha256Hex([string]$path) {
    $stream = [IO.File]::OpenRead($path)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        $digest = $algorithm.ComputeHash($stream)
        return ([BitConverter]::ToString($digest)).Replace('-', '')
    }
    finally {
        $algorithm.Dispose()
        $stream.Dispose()
    }
}

function Read-ModuleIdentity([string]$manifestPath) {
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Module manifest was not found: $manifestPath"
    }

    $manifest = [xml](Get-Content -LiteralPath $manifestPath -Raw)
    $moduleId = [string]$manifest.Module.Id.value
    $moduleVersion = ([string]$manifest.Module.Version.value).TrimStart([char[]]@('v', 'V'))
    if ([string]::IsNullOrWhiteSpace($moduleId) -or [string]::IsNullOrWhiteSpace($moduleVersion)) {
        throw "Module manifest is missing its ID or version: $manifestPath"
    }

    return [pscustomobject]@{ Id = $moduleId; Version = $moduleVersion }
}

$sourceIdentity = Read-ModuleIdentity (Join-Path $sourceModule 'SubModule.xml')
$installedIdentity = Read-ModuleIdentity (Join-Path $installedModule 'SubModule.xml')
if ($sourceIdentity.Id -ne 'CalradiaForge' -or $installedIdentity.Id -ne $sourceIdentity.Id) {
    throw "The source and installed module IDs must both be CalradiaForge. Source='$($sourceIdentity.Id)', installed='$($installedIdentity.Id)'."
}

$files = @(
    'AssetSources\GauntletUI\ui_calradiaforge_1.png',
    'GUI\CalradiaForgeSpriteData.xml',
    'GUI\SpriteParts\Config.xml'
)

foreach ($relativePath in $files) {
    $sourcePath = Join-Path $sourceModule $relativePath
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "Generated UI asset is missing: $sourcePath. Run Build-CalradiaForge-UiAssets.bat first."
    }
}

if ($usingWorkspaceSource -and (Test-Path -LiteralPath $python -PathType Leaf)) {
    Push-Location $repositoryRoot
    try {
        & $python 'tools\validate_game_icon_assets.py'
        if ($LASTEXITCODE -ne 0) {
            throw 'Game-icon source validation failed; no files were staged.'
        }
    }
    finally {
        Pop-Location
    }
}
elseif ($usingWorkspaceSource) {
    throw 'The Calradia Forge Python environment is missing. Run tools\Setup-CalradiaForge-Python.bat first.'
}
else {
    Write-Host 'Using an isolated source-module fixture; repository asset validation is covered by the staging test runner.'
}

if ($sourceIdentity.Version -ne $installedIdentity.Version) {
    if ($CollectTpac) {
        Write-Warning "Source version $($sourceIdentity.Version) differs from installed version $($installedIdentity.Version). This operation collects only the compiled sprite texture; it does not replace the source manifest or DLLs."
    }
    else {
        Write-Warning "Source version $($sourceIdentity.Version) differs from installed version $($installedIdentity.Version). This operation stages only the static sprite atlas and metadata; it does not replace the installed manifest or DLLs."
    }
}

if ($CollectTpac) {
    $tpacRelativePath = 'Assets\GauntletUI\ui_calradiaforge_1_tex.tpac'
    $importedTpac = Join-Path $installedModule $tpacRelativePath
    if (-not (Test-Path -LiteralPath $importedTpac -PathType Leaf)) {
        throw "Imported TPAC was not found: $importedTpac. Complete the Resource Browser import first."
    }
    if ((Get-Item -LiteralPath $importedTpac).Length -le 0) {
        throw "Imported TPAC is empty: $importedTpac"
    }
    $validationOutput = @()
    $validationExitCode = 0
    try {
        $validationOutput = @(& $inspectScript -ModulePath $installedModule -ToolDirectory $TpacToolDirectory -ValidateOnly *>&1)
    }
    catch {
        $validationExitCode = 1
        $validationOutput += $_.Exception.Message
    }
    if ($validationExitCode -ne 0) {
        Write-Host 'Collection stopped: the imported TPAC did not pass metadata validation, so no source file was changed.'
        Write-Host 'Reader output:'
        Write-Host ($validationOutput | Out-String)
        throw 'TPAC metadata validation failed; source collection was stopped.'
    }
    if (-not (($validationOutput | Out-String).Contains('Metadata validation: PASS'))) {
        throw 'TPAC validation did not provide positive metadata evidence; collection was stopped.'
    }
    $copyPlan = @(@{ RelativePath = $tpacRelativePath; SourcePath = $importedTpac; DestinationPath = (Join-Path $sourceModule $tpacRelativePath) })
}
else {
    $copyPlan = @()
    foreach ($relativePath in $files) {
        $copyPlan += @{ RelativePath = $relativePath; SourcePath = (Join-Path $sourceModule $relativePath); DestinationPath = (Join-Path $installedModule $relativePath) }
    }
}

foreach ($planItem in $copyPlan) {
    $relativePath = $planItem.RelativePath
    $sourcePath = $planItem.SourcePath
    $destinationPath = $planItem.DestinationPath
    $sourceHash = Get-Sha256Hex $sourcePath
    $destinationExists = Test-Path -LiteralPath $destinationPath -PathType Leaf
    $destinationHash = if ($destinationExists) { Get-Sha256Hex $destinationPath } else { '' }

    if ($destinationHash -eq $sourceHash) {
        Write-Host "Current: $relativePath"
        continue
    }

    if ($DryRun) {
        Write-Host "Would stage: $relativePath"
        continue
    }

    $destinationDirectory = Split-Path -Parent $destinationPath
    New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
    if ($destinationExists) {
        $backupPath = "$destinationPath.bak.$(Get-Date -Format 'yyyyMMdd-HHmmss-fff')"
        Copy-Item -LiteralPath $destinationPath -Destination $backupPath
        Write-Host "Backup: $backupPath"
    }

    $temporaryPath = "$destinationPath.stage.tmp"
    try {
        Copy-Item -LiteralPath $sourcePath -Destination $temporaryPath -Force
        $temporaryHash = Get-Sha256Hex $temporaryPath
        if ($temporaryHash -ne $sourceHash) {
            throw "Staged copy hash differs for $relativePath"
        }
        Move-Item -LiteralPath $temporaryPath -Destination $destinationPath -Force
        Write-Host "Staged: $relativePath ($sourceHash)"
    }
    catch {
        Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
        throw
    }
}

if ($LaunchEditor -and -not $DryRun) {
    if (-not (Test-Path -LiteralPath $editorExecutable -PathType Leaf)) {
        throw "Modding Kit executable was not found: $editorExecutable"
    }

    $runningGame = Get-Process -Name 'Bannerlord' -ErrorAction SilentlyContinue
    if ($runningGame) {
        Write-Host 'Bannerlord is already running; leaving the current game instance open.'
    }
    else {
        Start-Process -FilePath $editorExecutable -ArgumentList $editorModuleArguments -WorkingDirectory $editorDirectory | Out-Null
        Write-Host "Opened Modding Kit with Calradia Forge enabled: $editorExecutable $editorModuleArguments"
    }
}
elseif ($LaunchEditor -and $DryRun) {
    Write-Host "Would open Modding Kit with Calradia Forge enabled: $editorExecutable $editorModuleArguments"
}

if ($CollectTpac) {
    Write-Host "Collected runtime texture package into the source module: $(Join-Path $sourceModule $tpacRelativePath)"
    Write-Host 'TpacTool metadata validation passed before collection. Texture payload decoding and visual correctness are not certified.'
    Write-Host 'Run tools\Inspect-CalradiaForge-Tpac.bat to review the collected package and open the optional viewer.'
    return
}

Write-Host ''
Write-Host 'Resource Browser steps:'
Write-Host '  1. At the main menu, press Alt+` and enter resource.show_resource_browser.'
Write-Host '  2. Select Calradia Forge, open Assets\GauntletUI, and scan new asset files.'
Write-Host '  3. Import ui_calradiaforge_1.png into category ui_calradiaforge.'
Write-Host '  4. Verify Assets\GauntletUI\ui_calradiaforge_1_tex.tpac before packaging.'
Write-Host 'Use --launch-editor to start the Modding Kit with Native, SandBoxCore, Sandbox, StoryMode, CustomBattle, and CalradiaForge enabled; an already-running Bannerlord process is left untouched.'
Write-Host "Installed module: $installedModule ($($installedIdentity.Version)); source module: $($sourceIdentity.Version)."
if ($DryRun) {
    Write-Host 'Dry run complete; no files were changed and no game process was started.'
}
