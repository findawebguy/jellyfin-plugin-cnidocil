@echo off
setlocal
set "PATH=%PATH%;C:\Program Files\Docker\Docker\resources\bin"

echo =======================================================
echo       Jellyfin Docker Container Reset / Recreate
echo =======================================================
echo.
echo Choose an option:
echo   [1] Fast Recreate (Stops & recreates container; KEEPS your setup, users & media)
echo   [2] Reset Admin Password / Rerun Setup Wizard (Sets IsStartupWizardCompleted to false)
echo   [3] Complete Clean Wipe (Deletes container AND wipes database/config back to brand new)
echo   [4] Cancel
echo.
set /p opt="Enter option [1, 2, 3, or 4]: "

if "%opt%"=="1" (
    echo.
    echo Stopping and recreating container...
    docker compose down
    docker compose up -d --force-recreate
    echo.
    echo Done! Container recreated at http://localhost:8096
) else if "%opt%"=="2" (
    echo.
    echo Resetting startup wizard in system.xml...
    docker compose stop jellyfin
    powershell -Command "(Get-Content 'test_env\config\config\system.xml') -replace '<IsStartupWizardCompleted>true</IsStartupWizardCompleted>', '<IsStartupWizardCompleted>false</IsStartupWizardCompleted>' | Set-Content 'test_env\config\config\system.xml'"
    docker compose start jellyfin
    echo.
    echo Done! Startup Wizard reset. Navigate to http://localhost:8096 to create a new admin or reset password.
) else if "%opt%"=="3" (
    echo.
    echo [WARNING] This will erase test_env/config and test_env/cache!
    echo Stopping container...
    docker compose down -v
    echo Wiping test configuration data...
    rmdir /s /q "test_env\config" 2>nul
    rmdir /s /q "test_env\cache" 2>nul
    mkdir "test_env\config" 2>nul
    mkdir "test_env\cache" 2>nul
    echo Starting clean container...
    docker compose up -d
    echo.
    echo Done! Brand new Jellyfin instance ready at http://localhost:8096
) else (
    echo Cancelled.
)

pause
