@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
set "PYTHON=%ROOT%\.venv\Scripts\python.exe"
if not exist "%PYTHON%" (
    echo ERROR: Calradia Forge Python environment is missing. Run tools\Setup-CalradiaForge-Python.bat first.
    exit /b 2
)
"%PYTHON%" "%ROOT%\tools\prepare_desktop_textures.py" %*
exit /b %ERRORLEVEL%
