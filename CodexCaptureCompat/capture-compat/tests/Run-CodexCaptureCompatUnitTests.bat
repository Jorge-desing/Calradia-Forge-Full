@echo off
setlocal
set "BUILD_DIR=%~dp0..\build"
if not exist "%BUILD_DIR%\core_tests.exe" (
    echo Missing test executable: "%BUILD_DIR%\core_tests.exe"
    exit /b 1
)
if not exist "%BUILD_DIR%\dispatch_tests.exe" (
    echo Missing test executable: "%BUILD_DIR%\dispatch_tests.exe"
    exit /b 1
)

pushd "%BUILD_DIR%" || exit /b 1
core_tests.exe
if errorlevel 1 (
    popd
    exit /b 1
)
dispatch_tests.exe
set "TEST_EXIT=%ERRORLEVEL%"
popd
exit /b %TEST_EXIT%
