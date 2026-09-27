$ErrorActionPreference = 'Stop'

Write-Host "Enabling Bannerlord UI Debug Mode (Hot Reloading)..." -ForegroundColor Cyan

# The default path to Bannerlord's engine_config.txt
$documentsPath = [System.Environment]::GetFolderPath('MyDocuments')
$configPath = Join-Path $documentsPath "Mount and Blade II Bannerlord\Configs\engine_config.txt"

if (-not (Test-Path $configPath)) {
    Write-Host "Could not find engine_config.txt at $configPath. Please run the game at least once." -ForegroundColor Red
    exit 1
}

$configContent = Get-Content $configPath
$modified = $false

for ($i = 0; $i -lt $configContent.Count; $i++) {
    if ($configContent[$i] -match "DisableGuiMessages") {
        if ($configContent[$i] -ne "DisableGuiMessages = 0") {
            $configContent[$i] = "DisableGuiMessages = 0"
            $modified = $true
        }
    }
}

if ($modified) {
    Set-Content -Path $configPath -Value $configContent
    Write-Host "UI Debug Mode enabled! Use 'ui.toggle_debug_mode' in the console (~) during gameplay." -ForegroundColor Green
} else {
    Write-Host "UI Debug Mode is already enabled. Remember to use 'ui.toggle_debug_mode' in the console (~)." -ForegroundColor Yellow
}
