param(
    [string]$Version,
    [string]$TpacToolDirectory = '',
    [switch]$RequireTpacMetadata = $false,
    [switch]$SkipTests = $false,
    [switch]$Quick = $false
)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $workspace 'artifacts'
$pythonPath = Join-Path $workspace '.venv\Scripts\python.exe'
if (-not (Test-Path -LiteralPath $pythonPath -PathType Leaf)) {
    throw "The Calradia Forge Python environment is missing. Run tools\Setup-CalradiaForge-Python.bat first. Expected interpreter: $pythonPath"
}

Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

if (-not ("FastPackageEngine" -as [type])) {
    Add-Type -ReferencedAssemblies 'System.IO.Compression', 'System.IO.Compression.FileSystem' -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public static class FastPackageEngine {
    private static readonly HashSet<string> ExcludedDirNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
        "bin", "obj", "__pycache__", ".venv"
    };

    private static readonly HashSet<string> ExcludedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
        ".cfcrash", ".log", ".backup"
    };

    private static readonly Regex ExcludeBakRegex = new Regex(@"\.bak\.\d{8}-\d{6}-\d+$", RegexOptions.Compiled);

    public struct FileCopyItem {
        public string Source;
        public string Destination;
        public FileCopyItem(string src, string dst) { Source = src; Destination = dst; }
    }

    public static void FastTreeCopy(string[] sourceDirs, string targetDir, string workspaceRoot) {
        var filesToCopy = new List<FileCopyItem>();
        var directoriesToEnsure = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sourceDir in sourceDirs) {
            if (!Directory.Exists(sourceDir)) continue;
            CollectFiles(sourceDir, targetDir, workspaceRoot, filesToCopy, directoriesToEnsure);
        }

        foreach (var dir in directoriesToEnsure) {
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
        }

        Parallel.ForEach(filesToCopy, item => {
            File.Copy(item.Source, item.Destination, true);
        });
    }

    private static void CollectFiles(string currentSource, string targetBase, string workspaceRoot, List<FileCopyItem> files, HashSet<string> dirs) {
        var dirName = Path.GetFileName(currentSource);
        if (ExcludedDirNames.Contains(dirName)) return;
        if (string.Equals(dirName, "api", StringComparison.OrdinalIgnoreCase) && currentSource.IndexOf("docs-site", StringComparison.OrdinalIgnoreCase) >= 0) {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(currentSource)) {
            var ext = Path.GetExtension(file);
            if (ExcludedExtensions.Contains(ext)) continue;
            var name = Path.GetFileName(file);
            if (ExcludeBakRegex.IsMatch(name)) continue;

            string relative = file.Substring(workspaceRoot.Length).TrimStart('\\', '/');
            string target = Path.Combine(targetBase, relative);
            string parent = Path.GetDirectoryName(target);
            if (parent != null) {
                dirs.Add(parent);
            }
            files.Add(new FileCopyItem(file, target));
        }

        foreach (var subDir in Directory.EnumerateDirectories(currentSource)) {
            CollectFiles(subDir, targetBase, workspaceRoot, files, dirs);
        }
    }

    public static void CopyModuleDirectory(string sourceModulePath, string destModulePath) {
        if (!Directory.Exists(sourceModulePath)) return;
        var filesToCopy = new List<FileCopyItem>();
        var directoriesToEnsure = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        CollectModuleFiles(sourceModulePath, destModulePath, sourceModulePath, filesToCopy, directoriesToEnsure);

        foreach (var dir in directoriesToEnsure) {
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
        }

        Parallel.ForEach(filesToCopy, item => {
            File.Copy(item.Source, item.Destination, true);
        });
    }

    private static void CollectModuleFiles(string currentSource, string targetBase, string rootSource, List<FileCopyItem> files, HashSet<string> dirs) {
        var dirName = Path.GetFileName(currentSource);
        if (ExcludedDirNames.Contains(dirName)) return;

        foreach (var file in Directory.EnumerateFiles(currentSource)) {
            var ext = Path.GetExtension(file);
            if (ExcludedExtensions.Contains(ext)) continue;
            if (string.Equals(ext, ".pdb", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase)) continue;
            var name = Path.GetFileName(file);
            if (ExcludeBakRegex.IsMatch(name)) continue;

            string relative = file.Substring(rootSource.Length).TrimStart('\\', '/');
            string target = Path.Combine(targetBase, relative);
            string parent = Path.GetDirectoryName(target);
            if (parent != null) {
                dirs.Add(parent);
            }
            files.Add(new FileCopyItem(file, target));
        }

        foreach (var subDir in Directory.EnumerateDirectories(currentSource)) {
            CollectModuleFiles(subDir, targetBase, rootSource, files, dirs);
        }
    }

    public static void CompressParallel(string[] sourceDirs, string[] destinationZips, CompressionLevel level) {
        if (sourceDirs.Length != destinationZips.Length) {
            throw new ArgumentException("sourceDirs and destinationZips must have equal length");
        }
        var actions = new Action[sourceDirs.Length];
        for (int i = 0; i < sourceDirs.Length; i++) {
            int idx = i;
            actions[idx] = () => {
                var target = destinationZips[idx];
                if (File.Exists(target)) {
                    File.Delete(target);
                }
                ZipFile.CreateFromDirectory(sourceDirs[idx], target, level, false);
            };
        }
        Parallel.Invoke(actions);
    }
}
'@
}

function Copy-TreeWithoutBuildOutput {
    param(
        [string]$Source,
        [string]$Destination,
        [System.Collections.Generic.HashSet[string]]$DirCache = $null
    )
    if (-not [System.IO.Directory]::Exists($Source)) { return }
    [FastPackageEngine]::FastTreeCopy(@($Source), $Destination, $workspace)
}

function New-FastZipArchive {
    param(
        [string]$SourceDirectory,
        [string]$DestinationArchive,
        [System.IO.Compression.CompressionLevel]$CompressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
    )
    if ([System.IO.File]::Exists($DestinationArchive)) {
        [System.IO.File]::Delete($DestinationArchive)
    }
    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $SourceDirectory,
        $DestinationArchive,
        $CompressionLevel,
        $false
    )
}

function Test-ZipEntries {
    param([string]$Path)
    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $names = @($archive.Entries | ForEach-Object { $_.FullName })
        if (($names | Select-Object -Unique).Count -ne $names.Count) { throw "Duplicate archive entries: $Path" }
        foreach ($name in $names) {
            $normal = $name.Replace('\', '/')
            if ($normal.StartsWith('/') -or $normal.Split('/') -contains '..' -or $normal.Contains(':')) { throw "Unsafe archive entry: $name" }
            if ($normal.EndsWith('.sav', [StringComparison]::OrdinalIgnoreCase) -or $normal.IndexOf('save-backup', [StringComparison]::OrdinalIgnoreCase) -ge 0) { throw "Save data included: $name" }
            if ($normal.EndsWith('.cfcrash', [StringComparison]::OrdinalIgnoreCase) -or $normal.EndsWith('.log', [StringComparison]::OrdinalIgnoreCase)) { throw "Debug or crash dump included: $name" }
            if ($normal.EndsWith('.dll', [StringComparison]::OrdinalIgnoreCase) -and [IO.Path]::GetFileName($normal).StartsWith('TaleWorlds', [StringComparison]::OrdinalIgnoreCase)) { throw "Game assembly included: $name" }
        }
    } finally { $archive.Dispose() }
}

Push-Location $workspace
$stages = @()
$completed = $false
try {
    [xml]$props = Get-Content -LiteralPath (Join-Path $workspace 'Directory.Build.props') -Raw
    $release = @($props.Project.PropertyGroup | ForEach-Object { $_.CalradiaForgeVersion } | Where-Object { $_ })[0]
    if ([string]::IsNullOrWhiteSpace($release)) { throw 'Directory.Build.props does not define CalradiaForgeVersion.' }
    if ([string]::IsNullOrWhiteSpace($Version)) { $Version = $release }
    if ($Version -ne $release) { throw "Requested version '$Version' does not match shared release value '$release'." }
    if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Release version must use major.minor.patch.' }

    $zipLevel = if ($Quick) { [System.IO.Compression.CompressionLevel]::Fastest } else { [System.IO.Compression.CompressionLevel]::Optimal }

    dotnet restore CalradiaForge.sln -v:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Release restore failed.' }

    $needAssets = -not ($Quick -and (Test-Path (Join-Path $workspace 'modules/CalradiaForge/Assets/GauntletUI/ui_calradiaforge_1_tex.tpac')))
    if ($needAssets) {
        & $pythonPath tools/generate_game_icons.py
        if ($LASTEXITCODE -ne 0) { throw 'Native and Desktop icon generation failed.' }
        & $pythonPath tools/generate_assets.py
        if ($LASTEXITCODE -ne 0) { throw 'Native UI and manifest generation failed.' }
    }

    dotnet build CalradiaForge.sln -c Release --no-restore -v:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }

    $needDocs = -not ($Quick -and (Test-Path (Join-Path $workspace 'artifacts/docfx-site/index.html')))
    if ($needDocs) {
        & tools/build_docs.ps1
        if ($LASTEXITCODE -ne 0) { throw 'DocFX documentation build failed.' }
        & $pythonPath tools/generate_in_game_help.py
        if ($LASTEXITCODE -ne 0) { throw 'DocFX-backed in-game help generation failed.' }
    }

    & $pythonPath tools/validate_game_icon_assets.py --require-tpac
    if ($LASTEXITCODE -ne 0) { throw 'Native Game-icons source or runtime texture validation failed.' }
    if ([string]::IsNullOrWhiteSpace($TpacToolDirectory)) {
        $TpacToolDirectory = Join-Path $workspace 'TpacTool\bin'
    }
    $readerHost = Get-Command 'pwsh.exe' -ErrorAction SilentlyContinue
    if (-not $readerHost) { $readerHost = Get-Command 'powershell.exe' -ErrorAction SilentlyContinue }
    if (-not $readerHost) { throw 'PowerShell was not found to run the TPAC structural reader.' }
    $tpacArgs = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', 'tools/Inspect-CalradiaForge-Tpac.ps1', '-ModulePath', 'modules/CalradiaForge', '-ToolDirectory', $TpacToolDirectory)
    if ($RequireTpacMetadata) {
        $tpacArgs += '-ValidateOnly'
    }
    else {
        $tpacArgs += @('-ValidateOnly', '-StructuralOnly')
    }
    & $readerHost.Source @tpacArgs
    if ($LASTEXITCODE -ne 0) { throw 'TPAC structural validation failed; release packaging stopped before building archives.' }

    if (-not $SkipTests) {
        & tools/Run-CalradiaForge-Tests.bat --skip-build --no-pause
        if ($LASTEXITCODE -ne 0) { throw 'The .bat test runner reported a failure; release packaging stopped.' }
    }

    $xmlSettings = [System.Xml.XmlReaderSettings]::new()
    $xmlSettings.DtdProcessing = [System.Xml.DtdProcessing]::Ignore
    foreach ($xmlFile in [System.IO.Directory]::EnumerateFiles((Join-Path $workspace 'modules'), '*.xml', [System.IO.SearchOption]::AllDirectories)) {
        $reader = [System.Xml.XmlReader]::Create($xmlFile, $xmlSettings)
        try { while ($reader.Read()) { } }
        catch { throw "Invalid module XML $xmlFile : $($_.Exception.Message)" }
        finally { $reader.Dispose() }
    }

    $moduleStage = Join-Path $artifacts ('staging-' + [Guid]::NewGuid().ToString('N'))
    $sourceStage = Join-Path $artifacts ('source-staging-' + [Guid]::NewGuid().ToString('N'))
    $desktopStage = Join-Path $artifacts ('desktop-staging-' + [Guid]::NewGuid().ToString('N'))
    $stages += $moduleStage, $sourceStage, $desktopStage
    New-Item -ItemType Directory -Force $moduleStage, $sourceStage, $desktopStage | Out-Null

    foreach ($module in @('CalradiaForge', 'CalradiaForgeExamples', 'CalradiaForgePriceProvider', 'CalradiaForgePriceConsumer')) {
        $srcMod = Join-Path $workspace "modules/$module"
        $dstMod = Join-Path $moduleStage $module
        [FastPackageEngine]::CopyModuleDirectory($srcMod, $dstMod)
        $manifest = Join-Path $dstMod "SubModule.xml"
        if (-not (Select-String -LiteralPath $manifest -SimpleMatch "Version value=`"v$Version`"" -Quiet)) { throw "Manifest version mismatch: $module" }
    }
    $forgeBin = Join-Path $moduleStage 'CalradiaForge/bin/Win64_Shipping_Client'
    $examplesBin = Join-Path $moduleStage 'CalradiaForgeExamples/bin/Win64_Shipping_Client'
    $providerBin = Join-Path $moduleStage 'CalradiaForgePriceProvider/bin/Win64_Shipping_Client'
    $consumerBin = Join-Path $moduleStage 'CalradiaForgePriceConsumer/bin/Win64_Shipping_Client'
    New-Item -ItemType Directory -Force $forgeBin, $examplesBin, $providerBin, $consumerBin | Out-Null
    foreach ($assembly in @('CalradiaForge.Sdk', 'CalradiaForge.Core', 'CalradiaForge.Mod')) {
        Copy-Item -LiteralPath "src/$assembly/bin/Release/net472/$assembly.dll" -Destination $forgeBin -Force
    }
    $modBuild = Join-Path $workspace 'src/CalradiaForge.Mod/bin/Release/net472'
    Get-ChildItem -LiteralPath $modBuild -Filter '*.dll' -File |
        Where-Object { $_.Name -match '^(AsmResolver(\.DotNet|\.PE(\.File)?)?|Mono\.Cecil(\.(Mdb|Pdb|Rocks))?|MonoMod\.(Backports|Core|Iced|ILHelpers|RuntimeDetour|Utils)|System\.ValueTuple)\.dll$' } |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $forgeBin -Force }
    Copy-Item -LiteralPath 'THIRD_PARTY_NOTICES.md' -Destination (Join-Path $moduleStage 'CalradiaForge/THIRD_PARTY_NOTICES.md') -Force
    Copy-Item -LiteralPath 'src/CalradiaForge.Desktop/Resources/GameIcons/ATTRIBUTION.json' -Destination (Join-Path $moduleStage 'CalradiaForge/GUI/SpriteParts/ATTRIBUTION.json') -Force
    Copy-Item -LiteralPath 'examples/CalradiaForge.Examples/bin/Release/net472/CalradiaForge.Examples.dll' -Destination $examplesBin -Force
    Copy-Item -LiteralPath 'examples/CalradiaForge.PriceProvider/bin/Release/net472/CalradiaForge.PriceProvider.dll' -Destination $providerBin -Force
    Copy-Item -LiteralPath 'examples/CalradiaForge.PriceConsumer/bin/Release/net472/CalradiaForge.PriceConsumer.dll' -Destination $consumerBin -Force
    Copy-Item -LiteralPath 'examples/CalradiaForge.PriceContracts/bin/Release/net472/CalradiaForge.PriceContracts.dll' -Destination $providerBin -Force

    $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $forgeBin 'CalradiaForge.Mod.dll')).Version.ToString(3)
    if ($assemblyVersion -ne $Version) { throw "Runtime assembly version '$assemblyVersion' does not match '$Version'." }

    $sourceRoot = Join-Path $sourceStage 'Source'
    New-Item -ItemType Directory -Force $sourceRoot | Out-Null
    $sourceFolders = @('src','examples','tests','tools','modules','localization','assets','docs','docs-site','.config') |
        ForEach-Object { Join-Path $workspace $_ }
    [FastPackageEngine]::FastTreeCopy($sourceFolders, $sourceRoot, $workspace)

    $generatedDocs = Join-Path $sourceRoot 'docs-site/generated-site'
    New-Item -ItemType Directory -Force $generatedDocs | Out-Null
    Copy-Item -LiteralPath (Join-Path $artifacts 'docfx-site/*') -Destination $generatedDocs -Recurse -Force
    foreach ($file in @('CalradiaForge.sln','Directory.Build.props','README.md','.gitignore','THIRD_PARTY_NOTICES.md')) {
        Copy-Item -LiteralPath $file -Destination $sourceRoot -Force
    }
    $sdkRoot = Join-Path $sourceStage 'SDK'
    New-Item -ItemType Directory -Force $sdkRoot | Out-Null
    Copy-Item -LiteralPath 'src/CalradiaForge.Sdk/bin/Release/net472/CalradiaForge.Sdk.dll' -Destination $sdkRoot -Force
    Copy-Item -LiteralPath 'src/CalradiaForge.Sdk/bin/Release/net472/CalradiaForge.Sdk.xml' -Destination $sdkRoot -Force
    Copy-Item -LiteralPath 'README.md' -Destination (Join-Path $sourceStage 'README.md') -Force

    $desktopRoot = Join-Path $desktopStage 'Desktop'
    dotnet publish src/CalradiaForge.Desktop/CalradiaForge.Desktop.csproj -c Release --no-build --no-restore --self-contained false -p:UseAppHost=false -o $desktopRoot -v:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Desktop publish failed.' }
    if (Get-ChildItem -LiteralPath $desktopRoot -Filter '*.exe' -Recurse) { throw 'Desktop package must not contain an app-host EXE.' }
    Copy-Item -LiteralPath 'tools/Run-CalradiaForge-Desktop.bat' -Destination (Join-Path $desktopRoot 'Run-CalradiaForge-Desktop.bat') -Force
    Copy-Item -LiteralPath 'THIRD_PARTY_NOTICES.md' -Destination (Join-Path $desktopRoot 'THIRD_PARTY_NOTICES.md') -Force
    Copy-Item -LiteralPath 'src/CalradiaForge.Desktop/Resources/GameIcons/ATTRIBUTION.json' -Destination (Join-Path $desktopRoot 'GameIcons-ATTRIBUTION.json') -Force
    Copy-Item -LiteralPath 'docs/DESKTOP.md' -Destination (Join-Path $desktopStage 'README.md') -Force

    $modulesZip = Join-Path $artifacts "CalradiaForge-Modules-$Version.zip"
    $sourceZip = Join-Path $artifacts "CalradiaForge-Source-SDK-$Version.zip"
    $desktopZip = Join-Path $artifacts "CalradiaForge-Desktop-$Version.zip"

    [FastPackageEngine]::CompressParallel(
        @($moduleStage, $sourceStage, $desktopStage),
        @($modulesZip, $sourceZip, $desktopZip),
        $zipLevel
    )

    foreach ($zip in @($modulesZip, $sourceZip, $desktopZip)) { Test-ZipEntries $zip }
    & $pythonPath tools/audit_package.py --version $Version
    if ($LASTEXITCODE -ne 0) { throw 'Package audit failed.' }
    $hashLines = @(Get-FileHash -LiteralPath $modulesZip, $sourceZip, $desktopZip -Algorithm SHA256 |
        ForEach-Object { "$($_.Hash) *$([IO.Path]::GetFileName($_.Path))" })
    Set-Content -LiteralPath (Join-Path $artifacts "package-sha256-$($Version.Replace('.', '')).txt") -Value $hashLines -Encoding utf8
    Remove-Item -LiteralPath (Join-Path $artifacts "CalradiaForge-$Version.zip") -Force -ErrorAction SilentlyContinue
    $completed = $true
    Write-Output $modulesZip
    Write-Output $sourceZip
    Write-Output $desktopZip
}
finally {
    foreach ($stage in $stages) { if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force } }
    if ($completed) {
        Get-ChildItem -LiteralPath $artifacts -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match '^(staging|source-staging|desktop-staging)-' } |
            ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
    }
    Pop-Location
}
