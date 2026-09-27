param(
    [string]$GamePath = 'C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord',
    [string]$Version = '22.0.0'
)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot

# This short probe starts Bannerlord through Steam after preflight. It does not inject a module
# list, direct-launch Bannerlord.exe, or modify BLSE, Steam, launcher configuration, or saves.
& (Join-Path $PSScriptRoot 'Invoke-BannerlordSteamProbe.ps1') -GamePath $GamePath -Version $Version




