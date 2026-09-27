<#
.SYNOPSIS
    Manages and verifies the Windows 10 Codex Computer Use capture compatibility layer (CodexCaptureCompat).

.DESCRIPTION
    Provides unified status auditing, test execution, and deployment hooks for the
    local x64 version.dll compatibility layer required by codex-computer-use.exe
    under Windows 10.

.PARAMETER Action
    The action to perform: 'Status' (default), 'Test', 'WhatIf', 'Install', or 'Uninstall'.

.PARAMETER HelperPath
    The absolute or relative path to codex-computer-use.exe. If omitted, attempts
    to auto-detect running instances.
#>
param(
    [ValidateSet('Status', 'Test', 'WhatIf', 'Install', 'Uninstall')]
    [string]$Action = 'Status',

    [string]$HelperPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Get-Item (Join-Path $PSScriptRoot "..")).FullName
$compatDir = Join-Path $repoRoot "CodexCaptureCompat\capture-compat"
$distDir = Join-Path $compatDir "dist"
$versionDll = Join-Path $distDir "version.dll"

Write-Host "=== Calradia Forge · Codex Capture Compat Management ===" -ForegroundColor Cyan
Write-Host "Root: $repoRoot" -ForegroundColor Gray
Write-Host "Action: $Action" -ForegroundColor Yellow

if (-not (Test-Path $compatDir)) {
    Write-Error "CodexCaptureCompat directory not found at $compatDir"
    exit 1
}

# Auto-detect running helper if path is not specified
if (-not $HelperPath) {
    $runningProcesses = Get-Process -Name "codex-computer-use" -ErrorAction SilentlyContinue
    if ($runningProcesses) {
        $firstProc = $runningProcesses | Select-Object -First 1
        try {
            $HelperPath = $firstProc.Path
            Write-Host "Auto-detected running codex-computer-use.exe at: $HelperPath" -ForegroundColor Green
        } catch {
            Write-Host "Detected codex-computer-use process (PID: $($firstProc.Id)), but path is inaccessible." -ForegroundColor Yellow
        }
    }
}

switch ($Action) {
    'Status' {
        Write-Host "`n[Dist Binaries Status]" -ForegroundColor Cyan
        $binaries = @('version.dll', 'compat_probe.exe', 'capture_test_window.exe')
        foreach ($bin in $binaries) {
            $binPath = Join-Path $distDir $bin
            if (Test-Path $binPath) {
                $item = Get-Item $binPath
                $hash = (Get-FileHash -Path $binPath -Algorithm SHA256).Hash
                Write-Host "  [OK] $bin ($($item.Length) bytes, SHA256: $($hash.Substring(0, 16))...)" -ForegroundColor Green
            } else {
                Write-Host "  [MISSING] $bin" -ForegroundColor Red
            }
        }

        Write-Host "`n[Codex Computer Use Process Status]" -ForegroundColor Cyan
        $procs = Get-Process -Name "codex-computer-use" -ErrorAction SilentlyContinue
        if ($procs) {
            Write-Host "  Active processes found: $($procs.Count)" -ForegroundColor Green
            foreach ($p in $procs) {
                Write-Host "    PID: $($p.Id), WorkingSet: $([Math]::Round($p.WorkingSet64 / 1MB, 2)) MB" -ForegroundColor Gray
            }
        } else {
            Write-Host "  No active codex-computer-use processes running." -ForegroundColor Gray
        }
    }

    'Test' {
        Write-Host "`n[Running Install & Verification Tests]" -ForegroundColor Cyan
        $testRunner = Join-Path $compatDir "tests\Test-CodexCaptureCompatInstall.bat"
        if (-not (Test-Path -LiteralPath $testRunner -PathType Leaf)) {
            Write-Error "Batch test runner not found at $testRunner"
            exit 1
        }
        $testProcess = Start-Process -FilePath $testRunner -Wait -PassThru -NoNewWindow
        if ($testProcess.ExitCode -ne 0) {
            Write-Error "Install tests failed with exit code $($testProcess.ExitCode)."
            exit 1
        }
        Write-Host "  [OK] All CodexCaptureCompat verification tests passed successfully." -ForegroundColor Green
    }

    'WhatIf' {
        if (-not $HelperPath) {
            Write-Error "HelperPath is required for WhatIf action when codex-computer-use is not actively running."
            exit 1
        }
        Write-Host "`n[Previewing Installation]" -ForegroundColor Cyan
        $installScript = Join-Path $compatDir "install.ps1"
        & $installScript -HelperPath $HelperPath -WhatIf
    }

    'Install' {
        if (-not $HelperPath) {
            Write-Error "HelperPath is required for Install action when codex-computer-use is not actively running."
            exit 1
        }
        Write-Host "`n[Installing CodexCaptureCompat DLL]" -ForegroundColor Cyan
        $installScript = Join-Path $compatDir "install.ps1"
        & $installScript -HelperPath $HelperPath -Action Install
    }

    'Uninstall' {
        if (-not $HelperPath) {
            Write-Error "HelperPath is required for Uninstall action when codex-computer-use is not actively running."
            exit 1
        }
        Write-Host "`n[Uninstalling CodexCaptureCompat DLL]" -ForegroundColor Cyan
        $installScript = Join-Path $compatDir "install.ps1"
        & $installScript -HelperPath $HelperPath -Action Uninstall
    }
}

Write-Host "`nCodexCaptureCompat management completed successfully." -ForegroundColor Green
