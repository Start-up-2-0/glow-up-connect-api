@echo off
setlocal
cd /d "%~dp0.."

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0commit-profissionais-estabelecimento-acesso-feature.ps1" %*
set EXITCODE=%ERRORLEVEL%

if %EXITCODE% neq 0 (
    echo.
    echo Script failed with exit code %EXITCODE%.
    pause
)

exit /b %EXITCODE%
