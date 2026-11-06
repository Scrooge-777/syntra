@echo off
title SYNTRA Study Environment
cd /d "%~dp0"

echo =======================================================
echo   Launching SYNTRA Study OS...
echo   Mode: Evaluation Mode (Lockdown Disengaged)
echo =======================================================

if exist "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" (
    start "" "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --app="file:///%~dp0ui/index.html" --window-size=1600,1000
) else (
    start "" "%~dp0ui\index.html"
)

exit
