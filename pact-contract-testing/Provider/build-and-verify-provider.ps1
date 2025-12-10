# Build Provider, set env vars, and verify against broker
param(
    [string]$ProviderPath = ".",
    [string]$BrokerUrl = "http://puvsfpactserver.tiger01-dev.ba.lab.local:9292",
    [string]$ProviderVersion = "1.0.0",
    [string]$PublishTag = "main",
    [string]$ApiToken = "",
    [string]$ProviderBaseUrl = "http://127.0.0.1:9001",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

# Resolve paths relative to this script
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $scriptDir
try {
    # 1) Build provider solution or project
    Write-Host "Building Provider solution..." -ForegroundColor Green
    $solutionPath = Join-Path $ProviderPath "Provider.sln"
    $projectPath  = Join-Path $ProviderPath "src\provider.csproj"
    if (Test-Path $solutionPath) {
        dotnet build $solutionPath -c Release
    } elseif (Test-Path $projectPath) {
        dotnet build $projectPath -c Release
    } else {
        Write-Error "Neither Provider.sln nor src/provider.csproj found under '$ProviderPath'"; exit 1
    }
    if ($LASTEXITCODE -ne 0) { Write-Error "Build failed"; exit 1 }

    # 2) Optionally run provider tests to verify against broker
    if (-not $SkipTests) {
        Write-Host "Running Provider verification tests..." -ForegroundColor Green
        $testsCsproj = Join-Path $ProviderPath "tests\tests.csproj"
        if (Test-Path $testsCsproj) {
            dotnet test $testsCsproj -v minimal
        } else {
            # Fallback: run tests in tests folder if csproj not found
            $testsFolder = Join-Path $ProviderPath "tests"
            dotnet test $testsFolder -v minimal
        }
        if ($LASTEXITCODE -ne 0) { Write-Error "Tests failed"; exit 1 }
    }

    # 3) Set environment variables required for broker verification
    $env:BROKER_URL = $BrokerUrl
    $env:PACT_BROKER_BASE_URL = $BrokerUrl
    $env:PACT_PROVIDER_VERSION = $ProviderVersion
    $env:PUBLISH_TAG = $PublishTag
    $env:PACT_PROVIDER_BASE_URL = $ProviderBaseUrl
    if ($ApiToken) { $env:API_KEY = $ApiToken; $env:PACT_BROKER_TOKEN = $ApiToken }

    Write-Host "Environment configured:" -ForegroundColor Cyan
    Write-Host "  BROKER_URL=$env:BROKER_URL" -ForegroundColor DarkGray
    Write-Host "  PACT_PROVIDER_VERSION=$env:PACT_PROVIDER_VERSION" -ForegroundColor DarkGray
    Write-Host "  PUBLISH_TAG=$env:PUBLISH_TAG" -ForegroundColor DarkGray
    Write-Host "  PACT_PROVIDER_BASE_URL=$env:PACT_PROVIDER_BASE_URL" -ForegroundColor DarkGray
    if ($ApiToken) { Write-Host "  API_KEY=(set)" -ForegroundColor DarkGray }

    Write-Host "Provider build and broker verification completed successfully." -ForegroundColor Green
} finally {
    Pop-Location
}
