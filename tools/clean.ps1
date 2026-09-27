param(
    [switch]$Force = $false
)

$workspace = Split-Path $PSScriptRoot -Parent
Push-Location $workspace

Write-Host "Cleaning bin/ and obj/ directories..." -ForegroundColor Cyan

Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | ForEach-Object {
    Write-Host "Removing $_"
    Remove-Item -LiteralPath $_.FullName -Force -Recurse
}

Write-Host "Cleaning artifacts directory..." -ForegroundColor Cyan
if (Test-Path "artifacts") {
    Remove-Item -Path "artifacts\*" -Force -Recurse
}

Write-Host "Clean complete!" -ForegroundColor Green
Pop-Location
