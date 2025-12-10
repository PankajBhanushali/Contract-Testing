# Build Provider, set env vars, run tests, and publish verification results via API
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

    # 4) Publish verification results via API
    Write-Host "Publishing provider verification results via API..." -ForegroundColor Green
    $providerName = "ProductService"
    $consumerName = $env:PACT_CONSUMER_NAME
    if (-not $consumerName) { $consumerName = "ApiClient" }
    $latestPactUrl = "$BrokerUrl/pacts/provider/$providerName/consumer/$consumerName/latest"
    $pactResponse = Invoke-WebRequest -Uri $latestPactUrl -UseBasicParsing
    if ($pactResponse.StatusCode -ne 200) { Write-Error "Could not fetch latest pact from broker"; exit 1 }
    # Ensure content is a string before parsing as JSON
    $jsonText = $pactResponse.Content
    if ($jsonText -is [byte[]]) {
        $jsonText = [System.Text.Encoding]::UTF8.GetString($jsonText)
    }
    $pactJson = $jsonText | ConvertFrom-Json
    # Extract pact version from pb:pact-version link with debug output
    $pactVersion = $null
    if ($pactJson._links) {
        if ($pactJson._links.'pb:pact-version' -and $pactJson._links.'pb:pact-version'.name) {
            $pactVersion = $pactJson._links.'pb:pact-version'.name
            Write-Host "Extracted pact version using dot notation: $pactVersion"
        } elseif ($pactJson._links["pb:pact-version"] -and $pactJson._links["pb:pact-version"].name) {
            $pactVersion = $pactJson._links["pb:pact-version"].name
            Write-Host "Extracted pact version using dictionary notation: $pactVersion"
        } elseif ($pactJson._links.'pb:pact-version' -and $pactJson._links.'pb:pact-version'.href -match "/pact-version/([a-f0-9]+)") {
            $pactVersion = $Matches[1]
            Write-Host "Extracted pact version using regex from href: $pactVersion"
        } else {
            Write-Warning "pb:pact-version not found in _links. Dumping _links for debugging:"
            $debugJson = $pactJson._links | ConvertTo-Json -Depth 5
            $debugFile = Join-Path $PSScriptRoot 'debug-links.json'
            Set-Content -Path $debugFile -Value $debugJson -Encoding UTF8
            Write-Host "_links debug JSON written to $debugFile"
        }
    } else {
        Write-Warning "_links property missing in broker response. Dumping broker response for debugging:"
        $debugJson = $pactJson | ConvertTo-Json -Depth 5
        $debugFile = Join-Path $PSScriptRoot 'debug-broker-response.json'
        Set-Content -Path $debugFile -Value $debugJson -Encoding UTF8
        Write-Host "Broker response debug JSON written to $debugFile"
    }
    if (-not $pactVersion) { Write-Error "Could not determine pact version from broker response"; exit 1 }

    # Publish verification result
    $verificationUrl = "$BrokerUrl/pacts/provider/$providerName/consumer/$consumerName/pact-version/$pactVersion/verification-results"
    $body = @{ success = $true; providerApplicationVersion = $ProviderVersion }
    $headers = @{}
    if ($ApiToken) { $headers["Authorization"] = "Bearer $ApiToken" }
    $result = Invoke-RestMethod -Uri $verificationUrl -Method Post -Body ($body | ConvertTo-Json) -ContentType "application/json" -Headers $headers
    Write-Host "Verification result published: $($result._links.self.href)" -ForegroundColor Green

    Write-Host "Provider build, test, and broker verification (with API publish) completed successfully." -ForegroundColor Green
} finally {
    Pop-Location
}
