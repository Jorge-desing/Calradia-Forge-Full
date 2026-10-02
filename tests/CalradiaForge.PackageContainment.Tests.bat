@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
set "PYTHON=%ROOT%\.venv\Scripts\python.exe"
set "PROBE=%TEMP%\calradiaforge-package-containment-%RANDOM%-%RANDOM%"
set "WORKSPACE=%PROBE%\workspace"
set "ROOT_LINK_WORKSPACE=%PROBE%\root-link-workspace"
set "OUTSIDE=%TEMP%\calradiaforge-package-containment-%RANDOM%-%RANDOM%"
set "ROOT_OUTSIDE=%TEMP%\calradiaforge-package-containment-%RANDOM%-%RANDOM%"
set "LOG=%TEMP%\calradiaforge-package-containment-%RANDOM%-%RANDOM%.txt"
set "RESULT=1"

if not exist "%PYTHON%" goto setup_failed
mkdir "%WORKSPACE%\tools"
if errorlevel 1 goto setup_failed
mkdir "%WORKSPACE%\artifacts"
if errorlevel 1 goto setup_failed
mkdir "%OUTSIDE%"
if errorlevel 1 goto setup_failed
copy /Y "%ROOT%\tools\package.ps1" "%WORKSPACE%\tools\package.ps1" >nul
if errorlevel 1 goto setup_failed
mklink /J "%WORKSPACE%\artifacts\redirect" "%OUTSIDE%" >nul
if errorlevel 1 goto setup_failed

echo [Package] Verifying that a junction cannot redirect generated outputs outside artifacts/...
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%WORKSPACE%\tools\package.ps1" -Version 0.0.0 -PackageOutputDirectory "%WORKSPACE%\artifacts\redirect\output" -SkipTests -Quick >"%LOG%" 2>&1
if not errorlevel 1 goto guard_failed
findstr /C:"Package output path cannot contain a reparse point:" "%LOG%" >nul
if errorlevel 1 goto guard_failed
if exist "%OUTSIDE%\output" goto guard_failed

mkdir "%ROOT_LINK_WORKSPACE%\tools"
if errorlevel 1 goto setup_failed
mkdir "%ROOT_OUTSIDE%"
if errorlevel 1 goto setup_failed
copy /Y "%ROOT%\tools\package.ps1" "%ROOT_LINK_WORKSPACE%\tools\package.ps1" >nul
if errorlevel 1 goto setup_failed
mklink /J "%ROOT_LINK_WORKSPACE%\artifacts" "%ROOT_OUTSIDE%" >nul
if errorlevel 1 goto setup_failed

echo [Package] Verifying that an artifacts-root junction is rejected before creating outputs...
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%ROOT_LINK_WORKSPACE%\tools\package.ps1" -Version 0.0.0 -PackageOutputDirectory "%ROOT_LINK_WORKSPACE%\artifacts\output" -SkipTests -Quick >"%LOG%" 2>&1
if not errorlevel 1 goto root_guard_failed
findstr /C:"Package output path cannot contain a reparse point:" "%LOG%" >nul
if errorlevel 1 goto root_guard_failed
if exist "%ROOT_OUTSIDE%\output" goto root_guard_failed

echo [Package] Verifying that source-tree junctions cannot escape into package contents...
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%ROOT%\tests\Test-PackageTreeContainment.ps1" >"%LOG%" 2>&1
if errorlevel 1 goto source_guard_failed
findstr /C:"Package source-tree reparse-point guards passed." "%LOG%" >nul
if errorlevel 1 goto source_guard_failed

"%PYTHON%" "%ROOT%\tools\audit_package.py" --version 25.2.0 --artifacts-dir "%OUTSIDE%" >"%LOG%" 2>&1
if not errorlevel 1 goto audit_guard_failed
findstr /C:"Package audit output must be inside the ignored artifacts directory." "%LOG%" >nul
if errorlevel 1 goto audit_guard_failed
if exist "%OUTSIDE%\package-audit-2520.json" goto audit_guard_failed
echo Package output reparse-point guard passed.
echo Package artifacts-root reparse-point guard passed.
echo Package source-tree reparse-point guard passed.
echo Package audit output boundary passed.
set "RESULT=0"
goto cleanup

:setup_failed
echo ERROR: Could not create the isolated junction fixture.
set "RESULT=2"
goto cleanup

:guard_failed
echo ERROR: Package output reparse-point guard did not stop the redirected path.
if exist "%LOG%" type "%LOG%"
set "RESULT=1"
goto cleanup

:audit_guard_failed
echo ERROR: Package audit output boundary did not reject the external path.
if exist "%LOG%" type "%LOG%"
set "RESULT=1"
goto cleanup

:root_guard_failed
echo ERROR: Package output guard did not reject an artifacts-root reparse point.
if exist "%LOG%" type "%LOG%"
set "RESULT=1"
goto cleanup

:source_guard_failed
echo ERROR: Package source-tree copy followed a reparse point.
if exist "%LOG%" type "%LOG%"
set "RESULT=1"

:cleanup
if exist "%WORKSPACE%\artifacts\redirect" rmdir "%WORKSPACE%\artifacts\redirect" >nul 2>nul
if exist "%ROOT_LINK_WORKSPACE%\artifacts" rmdir "%ROOT_LINK_WORKSPACE%\artifacts" >nul 2>nul
if exist "%WORKSPACE%\tools\package.ps1" del "%WORKSPACE%\tools\package.ps1" >nul 2>nul
if exist "%ROOT_LINK_WORKSPACE%\tools\package.ps1" del "%ROOT_LINK_WORKSPACE%\tools\package.ps1" >nul 2>nul
if exist "%WORKSPACE%\artifacts" rmdir "%WORKSPACE%\artifacts" >nul 2>nul
if exist "%WORKSPACE%\tools" rmdir "%WORKSPACE%\tools" >nul 2>nul
if exist "%WORKSPACE%" rmdir "%WORKSPACE%" >nul 2>nul
if exist "%ROOT_LINK_WORKSPACE%\tools" rmdir "%ROOT_LINK_WORKSPACE%\tools" >nul 2>nul
if exist "%ROOT_LINK_WORKSPACE%" rmdir "%ROOT_LINK_WORKSPACE%" >nul 2>nul
if exist "%PROBE%" rmdir "%PROBE%" >nul 2>nul
if exist "%OUTSIDE%" rmdir "%OUTSIDE%" >nul 2>nul
if exist "%ROOT_OUTSIDE%" rmdir "%ROOT_OUTSIDE%" >nul 2>nul
if exist "%LOG%" del "%LOG%" >nul 2>nul
exit /b %RESULT%
