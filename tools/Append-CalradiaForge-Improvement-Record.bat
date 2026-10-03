@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
set "PYTHON=%ROOT%\.venv\Scripts\python.exe"
if "%~1"=="" goto usage
if not "%~2"=="" goto usage
if not exist "%PYTHON%" (
    echo ERROR: Run tools\Setup-CalradiaForge-Python.bat first.
    exit /b 2
)
pushd "%ROOT%"
if errorlevel 1 exit /b 2
if /I "%~1"=="--verify" (
    "%PYTHON%" tools\record_detailed_changelog_integrity.py
) else (
    "%PYTHON%" tools\append_detailed_changelog_revision.py "%~1"
)
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%
:usage
echo Usage: tools\Append-CalradiaForge-Improvement-Record.bat ^<annex.md^|--verify^>
exit /b 2
