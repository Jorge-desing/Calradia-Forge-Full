[CmdletBinding()]
param(
    [string]$GamePath = 'C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord',
    [string]$PythonPath = 'python',
    [switch]$DryRun,
    [switch]$LaunchModKit
)

$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$gameRoot = [IO.Path]::GetFullPath($GamePath)
$modulesRoot = Join-Path $gameRoot 'Modules'
$clientGameBin = Join-Path $gameRoot 'bin\Win64_Shipping_Client'
$editorGameBin = Join-Path $gameRoot 'bin\Win64_Shipping_wEditor'
$editorExecutable = Join-Path $editorGameBin 'Bannerlord.exe'
$editorBuildRoot = Join-Path ([IO.Path]::GetTempPath()) ('CalradiaForge-EditorBuild-' + [Guid]::NewGuid().ToString('N'))
$versionProps = Join-Path $workspace 'Directory.Build.props'
$preservedRuntimeTpacRelativePath = 'CalradiaForge\Assets\GauntletUI\ui_calradiaforge_1_tex.tpac'
$moduleIds = @('CalradiaForge', 'CalradiaForgeExamples', 'CalradiaForgePriceProvider', 'CalradiaForgePriceConsumer')
$releaseBuildCompleted = $false
$changes = [Collections.Generic.List[object]]::new()
$script:deploymentOriginals = @{}
$backupRoot = ''
$preservedRuntimeTpacHash = ''
$preservedRuntimeTpacBackupPath = ''

function Get-Sha256([string]$path) {
    $stream = [IO.File]::OpenRead($path)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-', '') }
    finally { $algorithm.Dispose(); $stream.Dispose() }
}

function Copy-WithBackup([string]$source, [string]$destination, [string]$relativePath) {
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Deployment source is missing: $source" }
    $sourceHash = Get-Sha256 $source
    if ((Test-Path -LiteralPath $destination -PathType Leaf) -and (Get-Sha256 $destination) -eq $sourceHash) {
        Write-Host "Current: $relativePath"
        return
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    $backupPath = ''
    $hadDestination = Test-Path -LiteralPath $destination -PathType Leaf
    $resolvedDestination = [IO.Path]::GetFullPath($destination)
    if ($script:deploymentOriginals.ContainsKey($resolvedDestination)) {
        $backupPath = [string]$script:deploymentOriginals[$resolvedDestination]
    }
    else {
        if ($hadDestination) {
            $backupPath = Join-Path $backupRoot $relativePath
            if (Test-Path -LiteralPath $backupPath -PathType Leaf) {
                throw "Deployment backup path already exists; refusing to overwrite it: $backupPath"
            }
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backupPath) | Out-Null
            $originalHash = Get-Sha256 $destination
            Copy-Item -LiteralPath $destination -Destination $backupPath -Force
            if ((Get-Sha256 $backupPath) -ne $originalHash) { throw "Deployment backup hash mismatch: $relativePath" }
        }
        $script:deploymentOriginals[$resolvedDestination] = $backupPath
    }

    $changes.Add([pscustomobject]@{ Destination = $destination; Backup = $backupPath; HadDestination = $hadDestination })
    Copy-Item -LiteralPath $source -Destination $destination -Force
    if ((Get-Sha256 $destination) -ne $sourceHash) { throw "Deployment hash mismatch: $relativePath" }
    if ($backupPath) { Write-Host "Backup: $backupPath" }
    Write-Host "Deployed: $relativePath ($sourceHash)"
}

function Restore-VerifiedBackup([string]$backupPath, [string]$destination, [string]$expectedHash) {
    if ((Get-Sha256 $backupPath) -ne $expectedHash) {
        throw "Backup hash mismatch; refusing to restore $destination from $backupPath."
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    $restoreTempPath = $destination + '.restore-' + [Guid]::NewGuid().ToString('N') + '.tmp'
    try {
        Copy-Item -LiteralPath $backupPath -Destination $restoreTempPath
        if ((Get-Sha256 $restoreTempPath) -ne $expectedHash) {
            throw "Temporary restore copy hash mismatch for $destination."
        }

        if (Test-Path -LiteralPath $destination -PathType Leaf) {
            [IO.File]::Replace($restoreTempPath, $destination, $null)
        }
        else {
            [IO.File]::Move($restoreTempPath, $destination)
        }

        if ((Get-Sha256 $destination) -ne $expectedHash) {
            throw "Restored file hash mismatch for $destination."
        }
    }
    finally {
        if (Test-Path -LiteralPath $restoreTempPath -PathType Leaf) {
            Remove-Item -LiteralPath $restoreTempPath -Force -ErrorAction SilentlyContinue
        }
    }
}

function Get-ManifestVersion([string]$manifestPath) {
    [xml]$manifest = Get-Content -LiteralPath $manifestPath -Raw
    return ([string]$manifest.Module.Version.value).TrimStart([char[]]@('v', 'V'))
}

try {
    if (-not (Test-Path -LiteralPath $modulesRoot -PathType Container)) { throw "Bannerlord Modules directory was not found: $modulesRoot" }
    if (-not (Test-Path -LiteralPath $clientGameBin -PathType Container)) { throw "Client assemblies were not found: $clientGameBin" }
    if (-not (Test-Path -LiteralPath $editorGameBin -PathType Container)) { throw "Modding Kit assemblies were not found: $editorGameBin" }
    if (-not (Test-Path -LiteralPath (Join-Path $editorGameBin 'TaleWorlds.ModuleManager.dll') -PathType Leaf)) { throw "Modding Kit module loader was not found: $editorGameBin" }
    if (Get-Process -Name Bannerlord -ErrorAction SilentlyContinue) { throw 'Close Bannerlord and the Modding Kit before deploying module files.' }

    [xml]$props = Get-Content -LiteralPath $versionProps -Raw
    $releaseVersion = [string]$props.Project.PropertyGroup.CalradiaForgeVersion
    if ([string]::IsNullOrWhiteSpace($releaseVersion)) { throw 'Directory.Build.props does not define CalradiaForgeVersion.' }
    $backupRoot = Join-Path $env:LOCALAPPDATA (Join-Path 'CalradiaForge\deploy-backups' ($releaseVersion + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff')))

    foreach ($moduleId in $moduleIds) {
        $sourceModule = Join-Path $workspace "modules\$moduleId"
        $sourceManifest = Join-Path $sourceModule 'SubModule.xml'
        if (-not (Test-Path -LiteralPath $sourceManifest -PathType Leaf)) { throw "Module source is missing: $sourceManifest" }
        $manifestVersion = Get-ManifestVersion $sourceManifest
        if ($manifestVersion -ne $releaseVersion) { throw "$moduleId manifest version $manifestVersion does not match source release $releaseVersion." }
    }

    $preservedRuntimeTpacPath = Join-Path $modulesRoot $preservedRuntimeTpacRelativePath
    if (-not (Test-Path -LiteralPath $preservedRuntimeTpacPath -PathType Leaf)) {
        throw "The imported Steam runtime TPAC is missing: $preservedRuntimeTpacPath. Complete the Resource Browser import before deploying the Gauntlet UI."
    }
    $preservedRuntimeTpacHash = Get-Sha256 $preservedRuntimeTpacPath
    $preservedRuntimeTpacBackupPath = Join-Path $backupRoot $preservedRuntimeTpacRelativePath
    if ($DryRun) {
        Write-Host "Would back up and preserve imported Steam runtime TPAC: $preservedRuntimeTpacRelativePath ($preservedRuntimeTpacHash)"
    }
    else {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $preservedRuntimeTpacBackupPath) | Out-Null
        Copy-Item -LiteralPath $preservedRuntimeTpacPath -Destination $preservedRuntimeTpacBackupPath -Force
        if ((Get-Sha256 $preservedRuntimeTpacBackupPath) -ne $preservedRuntimeTpacHash) {
            throw "Backup hash mismatch for imported Steam runtime TPAC: $preservedRuntimeTpacRelativePath"
        }
        Write-Host "Backed up and preserving imported Steam runtime TPAC: $preservedRuntimeTpacRelativePath ($preservedRuntimeTpacHash)"
    }

    Push-Location $workspace
    try {
        Write-Host "Building release $releaseVersion against the Client profile..."
        & dotnet build 'CalradiaForge.sln' --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Client-profile Release build failed.' }
        $releaseBuildCompleted = $true

        New-Item -ItemType Directory -Force -Path $editorBuildRoot | Out-Null
        Write-Host 'Building the Forge module against the Modding Kit editor profile...'
        & dotnet build 'src\CalradiaForge.Mod\CalradiaForge.Mod.csproj' --configuration Release --no-restore "-p:GameBin=$editorGameBin" "-p:OutputPath=$editorBuildRoot\"
        if ($LASTEXITCODE -ne 0) { throw 'Modding Kit editor-profile build failed.' }
    }
    finally { Pop-Location }

    $clientAssemblies = @(
        @{ Source = 'src\CalradiaForge.Sdk\bin\Release\net472\CalradiaForge.Sdk.dll'; Target = 'CalradiaForge\bin\Win64_Shipping_Client\CalradiaForge.Sdk.dll' },
        @{ Source = 'src\CalradiaForge.Core\bin\Release\net472\CalradiaForge.Core.dll'; Target = 'CalradiaForge\bin\Win64_Shipping_Client\CalradiaForge.Core.dll' },
        @{ Source = 'src\CalradiaForge.Mod\bin\Release\net472\CalradiaForge.Mod.dll'; Target = 'CalradiaForge\bin\Win64_Shipping_Client\CalradiaForge.Mod.dll' },
        @{ Source = 'examples\CalradiaForge.Examples\bin\Release\net472\CalradiaForge.Examples.dll'; Target = 'CalradiaForgeExamples\bin\Win64_Shipping_Client\CalradiaForge.Examples.dll' },
        @{ Source = 'examples\CalradiaForge.PriceProvider\bin\Release\net472\CalradiaForge.PriceProvider.dll'; Target = 'CalradiaForgePriceProvider\bin\Win64_Shipping_Client\CalradiaForge.PriceProvider.dll' },
        @{ Source = 'examples\CalradiaForge.PriceContracts\bin\Release\net472\CalradiaForge.PriceContracts.dll'; Target = 'CalradiaForgePriceProvider\bin\Win64_Shipping_Client\CalradiaForge.PriceContracts.dll' },
        @{ Source = 'examples\CalradiaForge.PriceConsumer\bin\Release\net472\CalradiaForge.PriceConsumer.dll'; Target = 'CalradiaForgePriceConsumer\bin\Win64_Shipping_Client\CalradiaForge.PriceConsumer.dll' }
    )
    $moduleBuildRoot = Join-Path $workspace 'modules'
    foreach ($moduleId in $moduleIds) {
        $sourceModule = Join-Path $moduleBuildRoot $moduleId
        $targetModule = Join-Path $modulesRoot $moduleId
        $relativeFiles = Get-ChildItem -LiteralPath $sourceModule -File -Recurse |
            Where-Object { $_.Name -notmatch '\.bak(?:\.|$)' }
        $relativeDirectories = Get-ChildItem -LiteralPath $sourceModule -Directory -Recurse
        $allDirectories = @($sourceModule)
        if ($relativeDirectories) { $allDirectories += @($relativeDirectories | ForEach-Object { $_.FullName }) }
        foreach ($directory in $allDirectories) {
            $relativeDirectory = if ($directory -eq $sourceModule) { $moduleId } else { $moduleId + '\' + $directory.Substring($sourceModule.Length).TrimStart('\') }
            $targetDirectory = Join-Path $modulesRoot $relativeDirectory
            if ($DryRun) { Write-Host "Would ensure directory: $relativeDirectory" }
            else { New-Item -ItemType Directory -Force -Path $targetDirectory | Out-Null }
        }
        foreach ($file in $relativeFiles) {
            $relativeSubPath = $file.FullName.Substring($sourceModule.Length).TrimStart('\')
            $targetRelative = $moduleId + '\' + $relativeSubPath
            if ($targetRelative.Equals($preservedRuntimeTpacRelativePath, [StringComparison]::OrdinalIgnoreCase)) {
                if ($DryRun) { Write-Host "Would preserve imported runtime TPAC: $targetRelative" }
                continue
            }
            if ($DryRun) { Write-Host "Would deploy module file: $targetRelative" }
            else { Copy-WithBackup $file.FullName (Join-Path $modulesRoot $targetRelative) $targetRelative }
        }
    }

    foreach ($assembly in $clientAssemblies) {
        $source = Join-Path $workspace $assembly.Source
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Client build output is missing: $source" }
        $relative = $assembly.Target
        if ($DryRun) { Write-Host "Would deploy Client assembly: $relative" }
        else { Copy-WithBackup $source (Join-Path $modulesRoot $relative) $relative }
    }

    $modBuild = Join-Path $workspace 'src\CalradiaForge.Mod\bin\Release\net472'
    foreach ($dependency in @(
        'AsmResolver.dll', 'AsmResolver.DotNet.dll', 'AsmResolver.PE.dll', 'AsmResolver.PE.File.dll',
        'Mono.Cecil.dll', 'Mono.Cecil.Mdb.dll', 'Mono.Cecil.Pdb.dll', 'Mono.Cecil.Rocks.dll',
        'MonoMod.Backports.dll', 'MonoMod.Core.dll', 'MonoMod.Iced.dll', 'MonoMod.ILHelpers.dll',
        'MonoMod.RuntimeDetour.dll', 'MonoMod.Utils.dll', 'System.ValueTuple.dll'
    )) {
        $source = Join-Path $modBuild $dependency
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Runtime dependency build output is missing: $source" }
        $relative = "CalradiaForge\bin\Win64_Shipping_Client\$dependency"
        if ($DryRun) { Write-Host "Would deploy Client dependency: $relative" }
        else { Copy-WithBackup $source (Join-Path $modulesRoot $relative) $relative }
    }

    $expectedAssemblyVersion = [Version]::new([int]($releaseVersion.Split('.')[0]), [int]($releaseVersion.Split('.')[1]), [int]($releaseVersion.Split('.')[2]), 0)
    $editorModuleAssembly = Join-Path $editorBuildRoot 'CalradiaForge.Mod.dll'
    if (-not (Test-Path -LiteralPath $editorModuleAssembly -PathType Leaf)) { throw "Editor-profile build omitted CalradiaForge.Mod.dll: $editorBuildRoot" }
    $builtAssemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($editorModuleAssembly).Version
    if ($builtAssemblyVersion -ne $expectedAssemblyVersion) { throw "Editor assembly version $builtAssemblyVersion does not match release $expectedAssemblyVersion." }
    $editorDlls = @(Get-ChildItem -LiteralPath $editorBuildRoot -Filter '*.dll' -File)
    if (-not ($editorDlls.Name -contains 'CalradiaForge.Core.dll') -or -not ($editorDlls.Name -contains 'CalradiaForge.Sdk.dll')) { throw 'Editor-profile output is missing Forge Core or SDK assemblies.' }
    foreach ($file in $editorDlls) {
        $relative = "CalradiaForge\bin\Win64_Shipping_wEditor\$($file.Name)"
        if ($DryRun) { Write-Host "Would deploy editor assembly: $relative" }
        else { Copy-WithBackup $file.FullName (Join-Path $modulesRoot $relative) $relative }
    }

    if ($DryRun) {
        Write-Host "Dry run complete for Calradia Forge $releaseVersion. No installed module files were changed."
        return
    }

    $installedVersion = Get-ManifestVersion (Join-Path $modulesRoot 'CalradiaForge\SubModule.xml')
    if ($installedVersion -ne $releaseVersion) { throw "Installed module manifest reports $installedVersion after deployment; expected $releaseVersion." }
    if ((Get-Sha256 $preservedRuntimeTpacPath) -ne $preservedRuntimeTpacHash) {
        throw "The imported Steam runtime TPAC changed during deployment: $preservedRuntimeTpacRelativePath"
    }
    foreach ($relative in @('CalradiaForge\bin\Win64_Shipping_Client\CalradiaForge.Mod.dll','CalradiaForge\bin\Win64_Shipping_wEditor\CalradiaForge.Mod.dll')) {
        $path = Join-Path $modulesRoot $relative
        $version = [Reflection.AssemblyName]::GetAssemblyName($path).Version
        if ($version -ne $expectedAssemblyVersion) { throw "Installed assembly version mismatch at ${relative}: $version" }
    }

    Write-Host "Deployed Calradia Forge $releaseVersion to Client and Modding Kit profiles."
    Write-Host "Previous installed files, when replaced, are backed up under: $backupRoot"
    if ($LaunchModKit) {
        if (-not (Test-Path -LiteralPath $editorExecutable -PathType Leaf)) { throw "Modding Kit executable was not found: $editorExecutable" }
        $arguments = '/singleplayer _MODULES_*Native*SandBoxCore*Sandbox*StoryMode*CustomBattle*CalradiaForge*_MODULES_'
        $process = Start-Process -FilePath $editorExecutable -ArgumentList $arguments -WorkingDirectory $editorGameBin -PassThru
        Write-Host "Started Modding Kit PID $($process.Id) with CalradiaForge enabled."
    }
}
catch {
    if (-not $DryRun -and $preservedRuntimeTpacHash) {
        try {
            $runtimeTpacChanged = -not (Test-Path -LiteralPath $preservedRuntimeTpacPath -PathType Leaf)
            if (-not $runtimeTpacChanged) {
                $runtimeTpacChanged = (Get-Sha256 $preservedRuntimeTpacPath) -ne $preservedRuntimeTpacHash
            }

            if ($runtimeTpacChanged) {
                if (-not (Test-Path -LiteralPath $preservedRuntimeTpacBackupPath -PathType Leaf)) {
                    Write-Warning "The imported Steam runtime TPAC changed, but its deployment backup is missing: $preservedRuntimeTpacBackupPath"
                }
                else {
                    Restore-VerifiedBackup $preservedRuntimeTpacBackupPath $preservedRuntimeTpacPath $preservedRuntimeTpacHash
                    Write-Warning "Restored the imported Steam runtime TPAC after its hash changed: $preservedRuntimeTpacRelativePath"
                }
            }
        }
        catch {
            Write-Warning "Could not verify or restore the imported Steam runtime TPAC from $preservedRuntimeTpacBackupPath. $($_.Exception.Message)"
        }
    }
    if (-not $DryRun -and $changes.Count -gt 0) {
        for ($index = $changes.Count - 1; $index -ge 0; $index--) {
            $record = $changes[$index]
            if ($record.HadDestination -and (Test-Path -LiteralPath $record.Backup -PathType Leaf)) {
                Copy-Item -LiteralPath $record.Backup -Destination $record.Destination -Force
                Write-Warning "Restored pre-deployment file: $($record.Destination)"
            }
            elseif (Test-Path -LiteralPath $record.Destination -PathType Leaf) {
                Remove-Item -LiteralPath $record.Destination -Force
                Write-Warning "Removed incomplete new file: $($record.Destination)"
            }
        }
    }
    throw
}
finally {
    if (Test-Path -LiteralPath $editorBuildRoot) {
        $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([char[]]@('\', '/'))
        $resolvedBuildRoot = [IO.Path]::GetFullPath($editorBuildRoot).TrimEnd([char[]]@('\', '/'))
        if ([IO.Path]::GetDirectoryName($resolvedBuildRoot).Equals($tempRoot, [StringComparison]::OrdinalIgnoreCase) -and
            [IO.Path]::GetFileName($resolvedBuildRoot) -match '^CalradiaForge-EditorBuild-[0-9a-f]{32}$') {
            Remove-Item -LiteralPath $resolvedBuildRoot -Recurse -Force
        }
    }
}
