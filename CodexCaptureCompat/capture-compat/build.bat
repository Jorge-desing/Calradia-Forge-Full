@echo off
setlocal EnableExtensions
set "PROJECT_DIR=%~dp0"
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
    echo Visual Studio Installer vswhere.exe was not found.
    exit /b 1
)

set "VSINSTALL="
for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "VSINSTALL=%%I"
if not defined VSINSTALL (
    echo MSVC x64 tools are not installed. Install the Visual C++ x64/x86 build tools, then rerun this batch file.
    exit /b 1
)

call "%VSINSTALL%\Common7\Tools\VsDevCmd.bat" -no_logo -arch=x64 -host_arch=x64
if errorlevel 1 exit /b 1
if not defined WindowsSdkDir (
    echo Windows SDK was not loaded by VsDevCmd. Install a Windows 10 or Windows 11 SDK component in Visual Studio Installer.
    exit /b 1
)
set "SDK_HEADER=%WindowsSdkDir%Include\%WindowsSDKVersion%um\Windows.h"
if not exist "%SDK_HEADER%" (
    echo Windows SDK headers were not found: "%SDK_HEADER%"
    echo Install a Windows 10 or Windows 11 SDK component in Visual Studio Installer, then rerun this batch file.
    exit /b 1
)
set "CAPTURE_HEADER=%WindowsSdkDir%Include\%WindowsSDKVersion%winrt\windows.graphics.capture.h"
findstr /c:"IGraphicsCaptureSession3" "%CAPTURE_HEADER%" >nul 2>nul
if errorlevel 1 (
    echo The active Windows SDK %WindowsSDKVersion% does not declare IGraphicsCaptureSession3.
    echo Install a newer Windows SDK whose windows.graphics.capture.h includes that interface, then rerun this batch file.
    exit /b 1
)
if not exist "%PROJECT_DIR%build" mkdir "%PROJECT_DIR%build"
if not exist "%PROJECT_DIR%dist" mkdir "%PROJECT_DIR%dist"
pushd "%PROJECT_DIR%build" || exit /b 1

set "COMMON_FLAGS=/nologo /std:c++17 /EHsc /W4 /O2 /MT /utf-8 /DUNICODE /D_UNICODE /DWIN32_LEAN_AND_MEAN /DNOMINMAX /D_SILENCE_EXPERIMENTAL_COROUTINE_DEPRECATION_WARNINGS /guard:cf"
if /i "%~1"=="-Trace" set "COMMON_FLAGS=%COMMON_FLAGS% /DCAPTURE_COMPAT_TRACE"

cl.exe %COMMON_FLAGS% /c "%PROJECT_DIR%src\compat.cpp" "%PROJECT_DIR%src\proxy.cpp" "%PROJECT_DIR%src\frame_dispatch.cpp"
if errorlevel 1 (echo C++ source compilation failed.& popd& exit /b 1)
ml64.exe /nologo /c "%PROJECT_DIR%src\version.asm"
if errorlevel 1 (echo MASM assembly failed.& popd& exit /b 1)
link.exe /nologo /dll /machine:x64 /dynamicbase /nxcompat /guard:cf /out:version.dll /implib:proxy.lib /def:"%PROJECT_DIR%src\version.def" compat.obj proxy.obj frame_dispatch.obj version.obj windowsapp.lib
if errorlevel 1 (echo Proxy linking failed.& popd& exit /b 1)

cl.exe %COMMON_FLAGS% "%PROJECT_DIR%tests\core_tests.cpp" compat.obj frame_dispatch.obj /Fe:core_tests.exe /link windowsapp.lib /dynamicbase /nxcompat
if errorlevel 1 (echo Core test build failed.& popd& exit /b 1)
cl.exe %COMMON_FLAGS% "%PROJECT_DIR%tests\dispatch_tests.cpp" frame_dispatch.obj /Fe:dispatch_tests.exe /link windowsapp.lib /dynamicbase /nxcompat
if errorlevel 1 (echo Dispatch test build failed.& popd& exit /b 1)
cl.exe %COMMON_FLAGS% "%PROJECT_DIR%tests\probe.cpp" /Fe:compat_probe.exe /link windowsapp.lib d3d11.lib version.lib user32.lib gdi32.lib /dynamicbase /nxcompat
if errorlevel 1 (echo Capture probe build failed.& popd& exit /b 1)
cl.exe %COMMON_FLAGS% "%PROJECT_DIR%tests\test_window.cpp" /Fe:capture_test_window.exe /link user32.lib gdi32.lib /subsystem:windows /dynamicbase /nxcompat
if errorlevel 1 (echo Test window build failed.& popd& exit /b 1)

call "%PROJECT_DIR%tests\Run-CodexCaptureCompatUnitTests.bat"
if errorlevel 1 (echo Native unit tests failed.& popd& exit /b 1)
copy /y version.dll "%PROJECT_DIR%dist\version.dll" >nul
if errorlevel 1 (echo Proxy copy to dist failed.& popd& exit /b 1)
copy /y compat_probe.exe "%PROJECT_DIR%dist\compat_probe.exe" >nul
if errorlevel 1 (echo Probe copy to dist failed.& popd& exit /b 1)
copy /y capture_test_window.exe "%PROJECT_DIR%dist\capture_test_window.exe" >nul
if errorlevel 1 (echo Test-window copy to dist failed.& popd& exit /b 1)
echo Build and unit tests passed.
popd
exit /b 0
