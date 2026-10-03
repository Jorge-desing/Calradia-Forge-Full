@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
set "PYTHON=%ROOT%\.venv\Scripts\python.exe"
if not exist "%PYTHON%" (
    echo ERROR: Run tools\Setup-CalradiaForge-Python.bat first.
    exit /b 2
)
pushd "%ROOT%"
if errorlevel 1 exit /b 2
"%PYTHON%" tools\validate_onboarding_knowledge.py
set "RESULT=%ERRORLEVEL%"
if not "%RESULT%"=="0" goto finish
"%PYTHON%" -m ruff check tools\validate_onboarding_knowledge.py tools\run_developer_onboarding_smoke.py
if errorlevel 1 goto failed
for %%S in (calradia-forge-dotnet calradia-forge-docs calradia-forge-dev-workflow calradia-forge-registro-mejoras bannerlord-dotnet-artisan bannerlord-gauntlet-ui game-ai-behavior-trees discrete-event-simulation performance-hunter) do (
    call tools\Validate-CalradiaForge-Skills.bat .agents\skills\%%S
    if errorlevel 1 goto failed
)
goto finish
:failed
set "RESULT=1"
:finish
popd
exit /b %RESULT%
