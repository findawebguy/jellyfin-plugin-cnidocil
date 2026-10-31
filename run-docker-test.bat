@echo off
setlocal
echo =======================================================
echo     Starting Jellyfin Docker Test Environment
echo =======================================================

:: Add Docker CLI to PATH for this session if missing
set "PATH=%PATH%;C:\Program Files\Docker\Docker\resources\bin"

:: Check if Docker CLI is found
where docker >nul 2>nul
if errorlevel 1 (
    echo [ERROR] Docker was not found in "C:\Program Files\Docker\Docker\resources\bin".
    echo Please ensure Docker Desktop is installed.
    pause
    exit /b 1
)

:: Test if Docker daemon is running
echo Checking Docker daemon...
docker info >nul 2>nul
if errorlevel 1 (
    echo.
    echo [WARNING] Docker Desktop daemon is not running!
    echo Please start "Docker Desktop" from your Windows Start Menu,
    echo wait 20-30 seconds until the Docker whale icon in your system tray is active,
    echo and then press any key to try again.
    echo.
    pause
    docker info >nul 2>nul
    if errorlevel 1 (
        echo [ERROR] Still unable to connect to Docker engine.
        echo Please verify Docker Desktop is running and try again.
        pause
        exit /b 1
    )
)

echo Docker daemon is ready!
echo Launching Jellyfin container (image: jellyfin/jellyfin:10.9.11)...
docker compose up -d

echo.
echo =======================================================
echo Jellyfin is running!
echo URL: http://localhost:8096
echo.
echo To stop the test environment later, run:
echo docker compose down
echo =======================================================
pause
