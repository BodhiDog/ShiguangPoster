@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0package.ps1"
set "EXIT_CODE=%ERRORLEVEL%"
echo.
if "%EXIT_CODE%"=="0" echo 打包完成，文件位于 dist 目录。
if not "%EXIT_CODE%"=="0" echo 打包失败，请查看上面的提示。
pause
exit /b %EXIT_CODE%
