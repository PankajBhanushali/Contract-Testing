@echo off
REM Quick Start Script for OpenAPI Contract Testing Demo

echo.
echo ╔════════════════════════════════════════════════════════╗
echo ║  OpenAPI Contract Testing Demo - Quick Start           ║
echo ╚════════════════════════════════════════════════════════╝
echo.

REM Check if provider is already running
echo [1/4] Checking for existing containers...
docker ps --format "{{.Names}}" | findstr "provider-api" >nul
if %ERRORLEVEL% EQU 0 (
    echo ✓ Provider already running
) else (
    echo [2/4] Starting Provider (Docker)...
    docker-compose up -d
    echo ✓ Provider started
    
    echo.
    echo [3/4] Waiting for provider to be healthy...
    timeout /t 5 /nobreak
)

echo.
echo [4/4] Running Consumer Tests...
echo.

REM Run tests
cd consumer
npm install --silent >nul 2>&1
npm test

echo.
echo ╔════════════════════════════════════════════════════════╗
echo ║  Test Run Complete                                     ║
echo ║                                                         ║
echo ║  Provider: http://localhost:5001                       ║
echo ║                                                         ║
echo ║  Commands:                                              ║
echo ║  - npm run test:v1   : SF 17.1 tests only              ║
echo ║  - npm run test:v2   : SF 18.1 tests only              ║
echo ║  - npm run test:watch: Watch mode                      ║
echo ║                                                         ║
echo ║  To stop provider: docker-compose down                 ║
echo ╚════════════════════════════════════════════════════════╝
echo.
