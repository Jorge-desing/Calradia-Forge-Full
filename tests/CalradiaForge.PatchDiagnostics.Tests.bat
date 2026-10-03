@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
set "PAUSE_ON_EXIT=1"
for %%A in (%*) do if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"
pushd "%ROOT%"
if errorlevel 1 (
  echo ERROR: Could not enter the repository root.
  set "RESULT=2"
  goto finish
)
echo [Forge patch diagnostics] Running Forge-owned and optional external-runtime regression fixture through the BAT launcher...
dotnet run --project tests\CalradiaForge.PatchDiagnostics.Tests\CalradiaForge.PatchDiagnostics.Tests.csproj --configuration Release --no-launch-profile
set "RESULT=%ERRORLEVEL%"
if not "%RESULT%"=="0" echo ERROR: Forge patch diagnostics fixture failed with exit code %RESULT%.
popd
:finish
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
