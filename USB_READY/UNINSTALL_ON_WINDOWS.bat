@echo off
setlocal enabledelayedexpansion
title Cnidocil (Playlist Triage) - Windows PC Uninstaller
echo =================================================================
echo        Cnidocil - Windows PC Uninstaller
echo =================================================================
echo.

set "DIR1=%ProgramData%\Jellyfin\Server\plugins"
set "DIR2=%LocalAppData%\Jellyfin\plugins"
set "DIR3=%ProgramFiles%\Jellyfin\Server\plugins"

for %%D in ("%DIR1%" "%DIR2%" "%DIR3%") do (
    if exist "%%~D" (
        echo Cleaning plugin folders in: %%~D
        for /d %%O in ("%%~D\PlaylistTriage*" "%%~D\Playlist Triage*") do (
            rmdir /s /q "%%O" 2>nul
            echo  [-] Removed: %%O
        )
        if exist "%%~D\configurations\Jellyfin.Plugin.PlaylistTriage.xml" (
            del /f /q "%%~D\configurations\Jellyfin.Plugin.PlaylistTriage.xml" 2>nul
        )
    )
)

:: Clean up web index.html tag if present
set "WEB1=%ProgramFiles%\Jellyfin\Server\jellyfin-web\index.html"
set "WEB2=%ProgramData%\Jellyfin\Server\jellyfin-web\index.html"
for %%W in ("%WEB1%" "%WEB2%") do (
    if exist "%%~W" (
        powershell -Command "(Get-Content '%%~W') -replace '<script src=\"/Plugins/PlaylistTriage/ClientScript\" defer></script>', '' -replace '<script src=\"/Plugins/PlaylistTriage/ClientScript\"></script>', '' | Set-Content '%%~W'"
        echo  [-] Removed injection tag from %%~W
    )
)

echo.
echo =================================================================
echo   [SUCCESS] Cnidocil has been completely uninstalled.
echo   Please restart your Jellyfin Server to finalize changes.
echo =================================================================
echo.
pause
