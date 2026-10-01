[CmdletBinding()]
param(
    [string]$EditorDirectory = '',
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$sourceModule = Join-Path $repositoryRoot 'modules\CalradiaForge'
$generatorName = 'TaleWorlds.TwoDimension.SpriteSheetGenerator.exe'
$python = Join-Path $repositoryRoot '.venv\Scripts\python.exe'

if ([string]::IsNullOrWhiteSpace($EditorDirectory)) {
    if (-not [string]::IsNullOrWhiteSpace($env:BANNERLORD_EDITOR_DIR)) {
        $EditorDirectory = $env:BANNERLORD_EDITOR_DIR
    }
    else {
        $EditorDirectory = Join-Path ${env:ProgramFiles(x86)} 'Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_wEditor'
    }
}

$EditorDirectory = [IO.Path]::GetFullPath($EditorDirectory)
$generatorPath = Join-Path $EditorDirectory $generatorName
$generatorLibrary = Join-Path $EditorDirectory 'TaleWorlds.TwoDimension.SpriteSheetGenerator.Library.dll'

if (-not (Test-Path -LiteralPath (Join-Path $sourceModule 'SubModule.xml') -PathType Leaf)) {
    throw "Calradia Forge source module was not found: $sourceModule"
}
if (-not (Test-Path -LiteralPath $generatorPath -PathType Leaf) -or -not (Test-Path -LiteralPath $generatorLibrary -PathType Leaf)) {
    throw "Bannerlord SpriteSheetGenerator was not found in '$EditorDirectory'. Set BANNERLORD_EDITOR_DIR to the Win64_Shipping_wEditor folder."
}
if (-not (Test-Path -LiteralPath $python -PathType Leaf)) {
    throw 'The Calradia Forge Python environment is missing. Run tools\Setup-CalradiaForge-Python.bat first.'
}

Push-Location $repositoryRoot
try {
    & $python 'tools\validate_game_icon_assets.py' '--source-prebuild'
    if ($LASTEXITCODE -ne 0) {
        throw 'Source Game-icons and authoring-input validation failed before generation.'
    }
}
finally {
    Pop-Location
}

$stageRoot = Join-Path ([IO.Path]::GetTempPath()) ('CalradiaForge-UiAssetBuild-' + [guid]::NewGuid().ToString('N'))
$modulesRoot = Join-Path $stageRoot 'Modules'
$stagedModule = Join-Path $modulesRoot 'CalradiaForge'
$stageCreated = $false
$published = @()
$temporaryCopies = @()

if ($DryRun) {
    Write-Host 'Dry run: source assets pass validation.'
    Write-Host "Would create an isolated Modules root containing only '$sourceModule'."
    Write-Host "Would run: $generatorPath SourceDirectory=<isolated Modules> CollectionType=AllAvailableModules OutputType=Engine"
    Write-Host 'Would validate the generated atlas and metadata, then back up and hash-check only the source atlas and SpriteData XML.'
    return
}

if ([Console]::IsInputRedirected) {
    throw 'SpriteSheetGenerator waits for a console key after generation. Start tools\Build-CalradiaForge-UiAssets.bat in a visible console; redirected/background execution is unsupported.'
}

try {
    New-Item -ItemType Directory -Path $stagedModule -Force | Out-Null
    $stageCreated = $true
    Copy-Item -LiteralPath (Join-Path $sourceModule 'SubModule.xml') -Destination $stagedModule
    Copy-Item -LiteralPath (Join-Path $sourceModule 'GUI') -Destination $stagedModule -Recurse

    Write-Host 'Running the official sprite generator in an isolated one-module workspace.'
    Write-Host 'This avoids the installed SingleModule CLI path, which exits successfully without generating Engine sprite files.'
    & $generatorPath "SourceDirectory=$modulesRoot" 'CollectionType=AllAvailableModules' 'OutputType=Engine'
    $generatorExitCode = $LASTEXITCODE
    if ($generatorExitCode -ne 0) {
        throw "SpriteSheetGenerator exited with code $generatorExitCode. Staging files are retained at '$stageRoot'."
    }

    $stagedAtlas = Join-Path $stagedModule 'AssetSources\GauntletUI\ui_calradiaforge_1.png'
    $stagedSpriteData = Join-Path $stagedModule 'GUI\CalradiaForgeSpriteData.xml'
    $stagedConfig = Join-Path $stagedModule 'GUI\SpriteParts\Config.xml'
    foreach ($requiredFile in @($stagedAtlas, $stagedSpriteData, $stagedConfig)) {
        if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
            throw "SpriteSheetGenerator reported success but did not create '$requiredFile'. Staging files are retained at '$stageRoot'."
        }
    }

    $spriteDataDocument = New-Object System.Xml.XmlDocument
    $spriteDataDocument.Load($stagedSpriteData)
    $configDocument = New-Object System.Xml.XmlDocument
    $configDocument.Load($stagedConfig)
    foreach ($configuredCategory in $configDocument.SelectNodes('/Config/SpriteCategory')) {
        if ($null -eq $configuredCategory.SelectSingleNode('./AlwaysLoad')) {
            continue
        }

        $categoryName = $configuredCategory.GetAttribute('Name')
        $generatedCategory = $null
        foreach ($candidate in $spriteDataDocument.SelectNodes('/SpriteData/SpriteCategories/SpriteCategory')) {
            if ($candidate.SelectSingleNode('./Name').InnerText -eq $categoryName) {
                $generatedCategory = $candidate
                break
            }
        }
        if ($null -eq $generatedCategory) {
            throw "Configured always-loaded sprite category '$categoryName' is absent from generated SpriteData. Staging files are retained at '$stageRoot'."
        }
        if ($null -eq $generatedCategory.SelectSingleNode('./AlwaysLoad')) {
            $alwaysLoad = $spriteDataDocument.CreateElement('AlwaysLoad')
            $sheetCount = $generatedCategory.SelectSingleNode('./SpriteSheetCount')
            if ($null -eq $sheetCount) {
                [void]$generatedCategory.AppendChild($alwaysLoad)
            }
            else {
                [void]$generatedCategory.InsertBefore($alwaysLoad, $sheetCount)
            }
        }
    }

    $xmlSettings = New-Object System.Xml.XmlWriterSettings
    $xmlSettings.Encoding = New-Object System.Text.UTF8Encoding($false)
    $xmlSettings.Indent = $true
    $xmlSettings.IndentChars = '  '
    $xmlSettings.NewLineChars = "`r`n"
    $xmlSettings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
    $xmlWriter = [System.Xml.XmlWriter]::Create($stagedSpriteData, $xmlSettings)
    try {
        $spriteDataDocument.Save($xmlWriter)
    }
    finally {
        $xmlWriter.Dispose()
    }

    Push-Location $repositoryRoot
    try {
        & $python 'tools\validate_game_icon_assets.py' '--module-path' $stagedModule
        if ($LASTEXITCODE -ne 0) {
            throw "Generated sprite assets failed validation. Staging files are retained at '$stageRoot'."
        }
    }
    finally {
        Pop-Location
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

    $copyItems = @(
        @{ Source = $stagedAtlas; Destination = (Join-Path $sourceModule 'AssetSources\GauntletUI\ui_calradiaforge_1.png'); Relative = 'AssetSources\GauntletUI\ui_calradiaforge_1.png' },
        @{ Source = $stagedSpriteData; Destination = (Join-Path $sourceModule 'GUI\CalradiaForgeSpriteData.xml'); Relative = 'GUI\CalradiaForgeSpriteData.xml' }
    )
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
    foreach ($item in $copyItems) {
        $sourceHash = Get-Sha256Hex $item.Source
        if ((Test-Path -LiteralPath $item.Destination -PathType Leaf) -and (Get-Sha256Hex $item.Destination) -eq $sourceHash) {
            Write-Host "Unchanged: $($item.Relative) ($sourceHash)"
            continue
        }

        $destinationDirectory = Split-Path -Parent $item.Destination
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
        $temporaryPath = $item.Destination + '.stage.' + [guid]::NewGuid().ToString('N') + '.tmp'
        $temporaryCopies += $temporaryPath
        Copy-Item -LiteralPath $item.Source -Destination $temporaryPath
        if ((Get-Sha256Hex $temporaryPath) -ne $sourceHash) {
            throw "Staged copy hash differs for $($item.Relative). Staging files are retained at '$stageRoot'."
        }

        $backupPath = ''
        if (Test-Path -LiteralPath $item.Destination -PathType Leaf) {
            $backupPath = $item.Destination + '.bak.' + $stamp
            Copy-Item -LiteralPath $item.Destination -Destination $backupPath
            Write-Host "Backup: $backupPath"
        }
        $published += @{ Destination = $item.Destination; Backup = $backupPath }
        Copy-Item -LiteralPath $temporaryPath -Destination $item.Destination -Force
        if ((Get-Sha256Hex $item.Destination) -ne $sourceHash) {
            throw "Published copy hash differs for $($item.Relative)."
        }
        Remove-Item -LiteralPath $temporaryPath -Force
        $temporaryCopies = @($temporaryCopies | Where-Object { $_ -ne $temporaryPath })
        Write-Host "Generated: $($item.Relative) ($sourceHash)"
    }

    Push-Location $repositoryRoot
    try {
        & $python 'tools\validate_game_icon_assets.py'
        if ($LASTEXITCODE -ne 0) {
            throw 'The published source assets failed their final validation.'
        }
    }
    finally {
        Pop-Location
    }

    $resolvedTempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([char[]]@('\', '/'))
    $resolvedStageRoot = [IO.Path]::GetFullPath($stageRoot).TrimEnd([char[]]@('\', '/'))
    $stageParent = [IO.Path]::GetDirectoryName($resolvedStageRoot)
    $stageLeaf = [IO.Path]::GetFileName($resolvedStageRoot)
    if (-not $stageParent.Equals($resolvedTempRoot, [StringComparison]::OrdinalIgnoreCase) -or $stageLeaf -notmatch '^CalradiaForge-UiAssetBuild-[0-9a-f]{32}$') {
        throw "Refusing to clean unexpected staging path '$resolvedStageRoot'."
    }
    Remove-Item -LiteralPath $resolvedStageRoot -Recurse -Force
    $stageCreated = $false

    Write-Host ''
    Write-Host 'The official generator produced a validated source atlas and SpriteData XML.'
    Write-Host 'The runtime TPAC still needs the Bannerlord Resource Browser import; SpriteSheetGenerator does not create it.'
    Write-Host 'Next: tools\Prepare-CalradiaForge-ResourceBrowser.bat, then import the staged PNG in Resource Browser.'
}
catch {
    for ($index = $published.Count - 1; $index -ge 0; $index--) {
        $record = $published[$index]
        if ($record.Backup -and (Test-Path -LiteralPath $record.Backup -PathType Leaf)) {
            Copy-Item -LiteralPath $record.Backup -Destination $record.Destination -Force
            Write-Warning "Restored previous file after failure: $($record.Destination)"
        }
        elseif (Test-Path -LiteralPath $record.Destination -PathType Leaf) {
            Remove-Item -LiteralPath $record.Destination -Force
        }
    }
    if ($stageCreated) {
        Write-Warning "Temporary generation workspace retained for inspection: $stageRoot"
    }
    throw
}
finally {
    foreach ($temporaryPath in $temporaryCopies) {
        Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
    }
}
