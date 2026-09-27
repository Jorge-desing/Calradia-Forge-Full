@echo off
setlocal
pushd "%~dp0"
if errorlevel 1 exit /b 2

dotnet build ".\tests\BannerlordFbxImporter.Tests\BannerlordFbxImporter.Tests.csproj" --configuration Release
if errorlevel 1 (
    popd
    exit /b 1
)

dotnet ".\tests\BannerlordFbxImporter.Tests\bin\Release\net8.0-windows\BannerlordFbxImporter.Tests.dll"
set "testExit=%ERRORLEVEL%"
popd
exit /b %testExit%
