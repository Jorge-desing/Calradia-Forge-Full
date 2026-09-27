@echo off
setlocal
set "PROJECT_DIR=%~dp0..\CodexCaptureCompat\capture-compat"
pushd "%PROJECT_DIR%" || exit /b 1

call "%PROJECT_DIR%\build.bat" %*
if errorlevel 1 (
    popd
    exit /b 1
)

call "%PROJECT_DIR%\tests\Test-CodexCaptureCompatInstall.bat"
if errorlevel 1 (
    popd
    exit /b 1
)

call "%PROJECT_DIR%\tests\Test-CodexCaptureCompatPersistence.bat"
if errorlevel 1 (
    popd
    exit /b 1
)
popd

call "%~dp0Test-CodexCaptureCompatLatency.bat"
exit /b %ERRORLEVEL%
