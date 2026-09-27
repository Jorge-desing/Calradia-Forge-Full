@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "SCRIPT=%~dp0Validate-CalradiaForge-DecorativeSprites.py"
set "PAUSE_ON_EXIT=1"
for %%A in (%*) do if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"

where py.exe >nul 2>nul
if not errorlevel 1 (
    py -3 "%SCRIPT%"
    set "RESULT=!ERRORLEVEL!"
) else (
    where python.exe >nul 2>nul
    if errorlevel 1 (
        echo ERROR: Python 3 was not found. Install Python or add py.exe to PATH.
        set "RESULT=9009"
    ) else (
        python "%SCRIPT%"
        set "RESULT=!ERRORLEVEL!"
    )
)
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b !RESULT!
