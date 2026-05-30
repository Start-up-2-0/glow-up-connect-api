@echo off
setlocal
cd /d "%~dp0.."

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0commit-horarios-atendimento-feature.ps1" %*
set EXITCODE=%ERRORLEVEL%

if %EXITCODE% neq 0 (
    echo.
    echo Script failed with exit code %EXITCODE%.
    pause
)

exit /b %EXITCODE%
