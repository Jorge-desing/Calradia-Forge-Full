@echo off
setlocal

for %%I in ("%~dp0..") do set "REPO_ROOT=%%~fI"
cd /d "%REPO_ROOT%"
if errorlevel 1 exit /b 1

if not defined BANNERLORD_GAME_PATH set "BANNERLORD_GAME_PATH=C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord"

dotnet run --project examples\CalradiaForge.ContentShowcase\Generator\CalradiaForge.ContentShowcase.Generator.csproj -c Release -- generate
if errorlevel 1 exit /b 1

dotnet run --project examples\CalradiaForge.ContentShowcase\Generator\CalradiaForge.ContentShowcase.Generator.csproj -c Release -- contract-tests
if errorlevel 1 exit /b 1

dotnet run --project examples\CalradiaForge.ContentShowcase\Generator\CalradiaForge.ContentShowcase.Generator.csproj -c Release -- verify --game-root "%BANNERLORD_GAME_PATH%"
if errorlevel 1 exit /b 1

dotnet build examples\CalradiaForge.ContentShowcase\Module\CalradiaForge.ContentShowcase.csproj -c Release -p:GamePath="%BANNERLORD_GAME_PATH%"
if errorlevel 1 exit /b 1

dotnet run --project examples\CalradiaForge.ContentShowcase\Generator\CalradiaForge.ContentShowcase.Generator.csproj -c Release -- benchmark
if errorlevel 1 exit /b 1

call tools\Run-CalradiaForge-Tests.bat --core-only --no-pause
if errorlevel 1 exit /b 1

echo Content showcase checks passed. Benchmark results describe the synthetic harness only.
endlocal
