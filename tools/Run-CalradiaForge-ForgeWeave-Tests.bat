@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
set "PAUSE_ON_EXIT=1"
for %%A in (%*) do if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"
set "TEST_ASSEMBLY=%ROOT%\tests\CalradiaForge.ForgeWeave.Tests\bin\Release\net8.0\CalradiaForge.ForgeWeave.Tests.dll"

if not exist "%TEST_ASSEMBLY%" (
    echo ERROR: ForgeWeave test output is missing. Build the solution first.
    set "RESULT=2"
    goto finish
)
echo [ForgeWeave] Running bounded event and replay tests via dotnet and this batch launcher...
dotnet "%TEST_ASSEMBLY%"
set "RESULT=%ERRORLEVEL%"
if not "%RESULT%"=="0" echo ERROR: ForgeWeave suite failed with exit code %RESULT%.

:finish
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
