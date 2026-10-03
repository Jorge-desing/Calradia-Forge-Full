@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
set "PYTHON=%ROOT%\.venv\Scripts\python.exe"
if not exist "%PYTHON%" (
    echo ERROR: Repository Python environment was not found at:
    echo        "%PYTHON%"
    echo Run tools\Setup-CalradiaForge-Python.bat --no-pause first.
    exit /b 2
)
pushd "%ROOT%" || exit /b 2
"%PYTHON%" tools\regenerate_language_resources.py
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%
