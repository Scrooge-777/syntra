@echo off
title SYNTRA Focus & Study Environment
cd /d "%~dp0"

echo =======================================================
echo   Launching SYNTRA Study Environment...
echo =======================================================
echo.

if exist "Syntra.exe" (
    Syntra.exe
) else if exist "dist\Syntra.App.exe" (
    dist\Syntra.App.exe
) else (
    dotnet run --project src\Syntra.App\Syntra.App.csproj
)

echo.
echo =======================================================
echo   Session finished or paused. Press any key to exit.
echo =======================================================
pause >nul
