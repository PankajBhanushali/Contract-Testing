# Publish Pact files directly via Pact Broker HTTP API
param(
    [string]$BrokerUrl = "$env:BROKER_URL",
    [string]$PactsDir = "pacts",
    [string]$Version = "$env:CONSUMER_VERSION",
    [string]$Tag = "$env:PUBLISH_TAG",
    [string]$ApiToken = "$env:API_KEY",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

Write-Host "==> Broker:" $BrokerUrl -ForegroundColor Cyan
Write-Host "==> Pacts dir:" $PactsDir -ForegroundColor Cyan
Write-Host "==> Version:" $Version " Tag:" $Tag -ForegroundColor Cyan

if (-not $BrokerUrl) { Write-Host "BrokerUrl is required." -ForegroundColor Red; exit 1 }
if (-not (Test-Path $PactsDir)) { Write-Host "Pacts directory not found: $PactsDir" -ForegroundColor Red; exit 1 }

# Optionally run Consumer tests
if (-not $SkipTests) {
    Write-Host "Running Consumer tests..." -ForegroundColor Green
    dotnet test .\Consumer -v minimal
    if ($LASTEXITCODE -ne 0) { Write-Host "Consumer tests failed; aborting publish." -ForegroundColor Red; exit 1 }
}

$headers = @{ 'Content-Type' = 'application/json' }
if ($ApiToken) { $headers['Authorization'] = "Bearer $ApiToken" }

# Iterate all pact JSON files and publish
$files = Get-ChildItem -Path $PactsDir -Filter *.json -File
if ($files.Count -eq 0) { Write-Host "No pact files found in $PactsDir" -ForegroundColor Yellow; exit 0 }

foreach ($file in $files) {
    Write-Host "Publishing pact: $($file.FullName)" -ForegroundColor Green
    $json = Get-Content -Raw -Path $file.FullName | ConvertFrom-Json

    $consumer = $json.consumer.name
    $provider = $json.provider.name
    if (-not $consumer -or -not $provider) {
        Write-Host "Invalid pact file (missing consumer/provider names): $($file.Name)" -ForegroundColor Red
        continue
    }

    # Construct URL: /pacts/provider/{provider}/consumer/{consumer}/versions/{version}
    $url = "$BrokerUrl/pacts/provider/$provider/consumer/$consumer/versions/$Version"

    try {
        $response = Invoke-RestMethod -Method Put -Uri $url -Headers $headers -Body ($json | ConvertTo-Json -Depth 100)
        Write-Host "Published pact for $consumer -> $provider (version $Version)" -ForegroundColor Green
    } catch {
        Write-Host "Failed to publish $($file.Name): $($_.Exception.Message)" -ForegroundColor Red
        continue
    }

    # Optional: tag the consumer version if tag provided
    if ($Tag) {
        $tagUrl = "$BrokerUrl/pacticipants/$consumer/versions/$Version/tags/$Tag"
        try {
            Invoke-RestMethod -Method Put -Uri $tagUrl -Headers $headers
            Write-Host "Tagged $consumer version $Version with '$Tag'" -ForegroundColor Cyan
        } catch {
            Write-Host "Failed to tag version: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

Write-Host "API publishing completed." -ForegroundColor Green
