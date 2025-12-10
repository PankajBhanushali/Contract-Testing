# Verify provider against pacts from Pact Broker and publish results
param(
    [string]$BrokerUrl = "http://puvsfpactserver.tiger01-dev.ba.lab.local:9292",
    [string]$ProviderName = "ProductService",
    [string]$ConsumerName = "ApiClient",
    [string]$ProviderBaseUrl = "http://localhost:5000",
    [string]$PublishVersion = "1.0.0",
    [string]$PublishTag = "main",
    [string]$ApiToken = "",
    [switch]$StartProvider,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

Write-Host "==> Broker:" $BrokerUrl -ForegroundColor Cyan
Write-Host "==> Provider:" $ProviderName " Base:" $ProviderBaseUrl -ForegroundColor Cyan
Write-Host "==> Consumer:" $ConsumerName " Version:" $PublishVersion " Tag:" $PublishTag -ForegroundColor Cyan

# Optionally start provider locally
if ($StartProvider) {
    Write-Host "Starting Provider API..." -ForegroundColor Green
    $providerProj = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "src\provider.csproj"
    if (-not (Test-Path $providerProj)) { Write-Error "Provider project not found: $providerProj"; exit 1 }
    Start-Process powershell -ArgumentList "-NoProfile","-ExecutionPolicy","Bypass","-Command","dotnet run --project `"$providerProj`"" | Out-Null
    Start-Sleep -Seconds 5
}

# Set env for tests to read broker and publish settings
$env:BROKER_URL = $BrokerUrl
$env:PACT_BROKER_BASE_URL = $BrokerUrl
$env:PACT_PROVIDER_NAME = $ProviderName
$env:PACT_CONSUMER_NAME = $ConsumerName
$env:PACT_PROVIDER_BASE_URL = $ProviderBaseUrl
$env:PACT_PROVIDER_VERSION = $PublishVersion
$env:PUBLISH_TAG = $PublishTag
if ($ApiToken) { $env:API_KEY = $ApiToken; $env:PACT_BROKER_TOKEN = $ApiToken }

# Run provider verification tests
if (-not $SkipTests) {
    Write-Host "Running Provider verification tests..." -ForegroundColor Green
    $testsCsproj = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "tests\tests.csproj"
    if (-not (Test-Path $testsCsproj)) { Write-Error "Provider tests project not found: $testsCsproj"; exit 1 }
    dotnet test $testsCsproj -v minimal
    if ($LASTEXITCODE -ne 0) { Write-Error "Provider verification failed"; exit 1 }
}

Write-Host "Provider verification completed successfully." -ForegroundColor Green
