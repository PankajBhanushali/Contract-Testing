# Publish Pact using Docker image pactfoundation/pact-cli
param(
    [string]$BrokerUrl = "$env:BROKER_URL",
    [string]$PactsDir = "pacts",
    [string]$Version = "$env:CONSUMER_VERSION",
    [string]$Tag = "$env:PUBLISH_TAG",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

Write-Host "==> Broker:" $BrokerUrl -ForegroundColor Cyan
Write-Host "==> Pacts dir:" $PactsDir -ForegroundColor Cyan
Write-Host "==> Version:" $Version " Tag:" $Tag -ForegroundColor Cyan

# Ensure Docker available
try {
    $dockerVersion = & docker --version 2>$null
} catch {
    Write-Host "Docker not found. Please install Docker Desktop." -ForegroundColor Red
    exit 1
}

# Optionally run Consumer tests to regenerate pact
if (-not $SkipTests) {
    Write-Host "Running Consumer tests..." -ForegroundColor Green
    dotnet test .\Consumer -v minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Consumer tests failed; aborting publish." -ForegroundColor Red
        exit 1
    }
}

# Validate pacts directory exists
if (-not (Test-Path $PactsDir)) {
    Write-Host "Pacts directory not found: $PactsDir" -ForegroundColor Red
    exit 1
}

# Build Docker publish command (bind mount current workspace)
$workspace = (Get-Location).Path
$containerPath = '/pacts'
$pactPathInContainer = "$containerPath/" + ($PactsDir -replace "^\\|^/","")

$dockerCmd = "docker run --rm --mount type=bind,source=`"$workspace`",target=$containerPath pactfoundation/pact-cli:latest publish `"$pactPathInContainer`" --broker-base-url=`"$BrokerUrl`" --consumer-app-version=`"$Version`""
if ($Tag) { $dockerCmd += " --tag=`"$Tag`"" }

Write-Host "Publishing pact (Docker)..." -ForegroundColor Green
Write-Host $dockerCmd -ForegroundColor DarkGray

Invoke-Expression $dockerCmd

if ($LASTEXITCODE -ne 0) {
    Write-Host "Pact publish failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Pact published successfully to $BrokerUrl" -ForegroundColor Green
