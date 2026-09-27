@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
python "%ROOT%\tools\prepare_desktop_textures.py" %*
exit /b %ERRORLEVEL%
