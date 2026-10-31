@echo off
setlocal enabledelayedexpansion
title Cnidocil (Playlist Triage) - Windows PC Installer
echo =================================================================
echo        Cnidocil - Windows PC Quick Installer
echo =================================================================
echo.

set "SCRIPT_DIR=%~dp0"
set "INSTALLED_COUNT=0"

:: List of standard Jellyfin directories on Windows
set "DIR1=%ProgramData%\Jellyfin\Server"
set "DIR2=%LocalAppData%\Jellyfin"
set "DIR3=%ProgramFiles%\Jellyfin\Server"

for %%D in ("%DIR1%" "%DIR2%" "%DIR3%") do (
    if exist "%%~D" (
        set "PDIR=%%~D\plugins"
        set "CONFIG_DIR=!PDIR!\configurations"
        set "DEST=!PDIR!\PlaylistTriage"
        
        if not exist "!PDIR!" mkdir "!PDIR!"
        if not exist "!CONFIG_DIR!" mkdir "!CONFIG_DIR!"
        
        :: Clean old or duplicate folders
        if exist "!DEST!" rmdir /s /q "!DEST!" 2>nul
        for /d %%O in ("!PDIR!\PlaylistTriage*" "!PDIR!\Playlist Triage*") do (
            rmdir /s /q "%%O" 2>nul
        )
        
        mkdir "!DEST!"
        
        copy /y "%SCRIPT_DIR%PlaylistTriage\Jellyfin.Plugin.PlaylistTriage.dll" "!DEST!\" >nul
        if exist "%SCRIPT_DIR%PlaylistTriage\meta.json" (
            copy /y "%SCRIPT_DIR%PlaylistTriage\meta.json" "!DEST!\" >nul
        )
        if exist "%SCRIPT_DIR%PlaylistTriage\Jellyfin.Plugin.PlaylistTriage.pdb" (
            copy /y "%SCRIPT_DIR%PlaylistTriage\Jellyfin.Plugin.PlaylistTriage.pdb" "!DEST!\" >nul
        )
        
        echo  [+] Installed cleanly to: !DEST!
        set /a INSTALLED_COUNT+=1
    )
)

if %INSTALLED_COUNT% equ 0 (
    echo [!] Standard Jellyfin directories not detected automatically.
    echo Defaulting to: %ProgramData%\Jellyfin\Server\plugins\PlaylistTriage
    set "DEST=%ProgramData%\Jellyfin\Server\plugins\PlaylistTriage"
    if not exist "!DEST!" mkdir "!DEST!"
    copy /y "%SCRIPT_DIR%PlaylistTriage\Jellyfin.Plugin.PlaylistTriage.dll" "!DEST!\" >nul
    if exist "%SCRIPT_DIR%PlaylistTriage\meta.json" copy /y "%SCRIPT_DIR%PlaylistTriage\meta.json" "!DEST!\" >nul
    echo  [+] Created: !DEST!
)

:: Check for web index.html injection fallback
set "WEB1=%ProgramFiles%\Jellyfin\Server\jellyfin-web\index.html"
set "WEB2=%ProgramData%\Jellyfin\Server\jellyfin-web\index.html"
for %%W in ("%WEB1%" "%WEB2%") do (
    if exist "%%~W" (
        findstr /c:"/Plugins/PlaylistTriage/ClientScript" "%%~W" >nul 2>&1
        if errorlevel 1 (
            powershell -Command "(Get-Content '%%~W') -replace '</body>', '<script src=\"/Plugins/PlaylistTriage/ClientScript\" defer></script></body>' | Set-Content '%%~W'"
            echo  [+] Added client script tag to %%~W
        )
    )
)

echo.
echo =================================================================
echo   [SUCCESS] Cnidocil has been installed!
echo   Please restart your Jellyfin Server (via Windows Services or
echo   the Jellyfin tray icon) to load the new plugin.
echo =================================================================
echo.
pause
