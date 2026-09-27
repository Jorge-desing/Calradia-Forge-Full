@echo off
setlocal EnableExtensions

set "PROJECT_DIR=%~dp0.."
set "PROBE=%PROJECT_DIR%\dist\compat_probe.exe"
set "SHIM=%PROJECT_DIR%\dist\version.dll"
set "SECONDS=10"

if not exist "%PROBE%" (
    echo Missing probe: "%PROBE%"
    echo Run build.bat first.
    exit /b 1
)
if not exist "%SHIM%" (
    echo Missing compatibility proxy: "%SHIM%"
    echo Run build.bat first.
    exit /b 1
)

pushd "%PROJECT_DIR%\dist" || exit /b 1
compat_probe.exe --expect-shim --sustained-fps "%SECONDS%"
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%
