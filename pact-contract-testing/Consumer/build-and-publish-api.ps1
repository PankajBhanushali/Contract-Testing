# Build Consumer, set env vars, and publish pacts via API
param(
    [string]$ConsumerPath = ".",
    [string]$BrokerUrl = "http://puvsfpactserver.tiger01-dev.ba.lab.local:9292",
    [string]$ConsumerVersion = "v0.1",
    [string]$PublishTag = "main",
    [string]$ApiToken = "",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

# Resolve paths relative to this script
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $scriptDir
try {
    # 1) Build consumer solution or project
    Write-Host "Building Consumer solution..." -ForegroundColor Green
    $solutionPath = Join-Path $ConsumerPath "Consumer.sln"
    $projectPath  = Join-Path $ConsumerPath "src\consumer.csproj"
    if (Test-Path $solutionPath) {
        dotnet build $solutionPath -c Release
    } elseif (Test-Path $projectPath) {
        dotnet build $projectPath -c Release
    } else {
        Write-Error "Neither Consumer.sln nor src/consumer.csproj found under '$ConsumerPath'"; exit 1
    }
    if ($LASTEXITCODE -ne 0) { Write-Error "Build failed"; exit 1 }

    # 2) Run tests to generate pact files
    Write-Host "Running Consumer tests..." -ForegroundColor Green
    $testsCsproj = Join-Path $ConsumerPath "tests\tests.csproj"
    if (Test-Path $testsCsproj) {
        dotnet test $testsCsproj -v minimal
    } else {
        # Fallback: run tests in tests folder if csproj not found
        $testsFolder = Join-Path $ConsumerPath "tests"
        dotnet test $testsFolder -v minimal
    }
    if ($LASTEXITCODE -ne 0) { Write-Error "Tests failed"; exit 1 }

    # 3) Set environment variables required by publish-with-api.ps1
    $env:BROKER_URL = $BrokerUrl
    $env:CONSUMER_VERSION = $ConsumerVersion
    $env:PUBLISH_TAG = $PublishTag
    if ($ApiToken) { $env:API_KEY = $ApiToken }

    Write-Host "Environment configured:" -ForegroundColor Cyan
    Write-Host "  BROKER_URL=$env:BROKER_URL" -ForegroundColor DarkGray
    Write-Host "  CONSUMER_VERSION=$env:CONSUMER_VERSION" -ForegroundColor DarkGray
    Write-Host "  PUBLISH_TAG=$env:PUBLISH_TAG" -ForegroundColor DarkGray
    if ($ApiToken) { Write-Host "  API_KEY=(set)" -ForegroundColor DarkGray }

    # 4) Publish via API (script lives in Consumer folder)
    Write-Host "Publishing pacts via API..." -ForegroundColor Green
    $publishScript = Join-Path $scriptDir "publish-with-api.ps1"
    if (-not (Test-Path $publishScript)) { Write-Error "publish-with-api.ps1 not found in Consumer folder"; exit 1 }

    # Skip tests during publish script (already executed above)
    $args = @('-SkipTests')

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $publishScript @args
    if ($LASTEXITCODE -ne 0) { Write-Error "API publish failed"; exit $LASTEXITCODE }

    Write-Host "Build and API publish completed successfully." -ForegroundColor Green
} finally {
    Pop-Location
}
