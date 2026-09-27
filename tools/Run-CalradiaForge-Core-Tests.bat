@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
set "PAUSE_ON_EXIT=1"
for %%A in (%*) do if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"
set "CORE_TESTS=%ROOT%\tests\CalradiaForge.Tests\bin\Release\net472\CalradiaForge.Tests.dll"

if not exist "%CORE_TESTS%" (
    echo ERROR: Core test library is missing. Build the solution first.
    set "RESULT=2"
    goto finish
)

echo [Core] Running SDK, ForgeWeave, and module tests through the batch launcher...
where powershell.exe >nul 2>nul
if errorlevel 1 (
    echo ERROR: Windows PowerShell is required to host the .NET Framework test assembly.
    set "RESULT=2"
    goto finish
)
if defined FORGE_TEST_MODULES (
    powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0Run-CalradiaForge-Core-Tests.ps1" -TestAssembly "%CORE_TESTS%" -InstalledModulesPath "%FORGE_TEST_MODULES%" <nul
) else (
    powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0Run-CalradiaForge-Core-Tests.ps1" -TestAssembly "%CORE_TESTS%" <nul
)
set "RESULT=%ERRORLEVEL%"
if not "%RESULT%"=="0" echo ERROR: Core suite failed with exit code %RESULT%.

:finish
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
