param(
    [string]$BrokerUrl,
    [string]$ProviderName = "ProductService",
    [string]$ConsumerName,
    [string]$ProviderBaseUrl,
    [string]$ProviderVersion,
    [string]$BrokerToken
)

if (-not $BrokerUrl) {
    if ($env:BROKER_URL) { $BrokerUrl = $env:BROKER_URL }
    elseif ($env:PACT_BROKER_BASE_URL) { $BrokerUrl = $env:PACT_BROKER_BASE_URL }
}
if (-not $BrokerUrl) { Write-Error "BrokerUrl is required. Set BROKER_URL or PACT_BROKER_BASE_URL."; exit 1 }

if (-not $ConsumerName) { $ConsumerName = if ($env:PACT_CONSUMER_NAME) { $env:PACT_CONSUMER_NAME } else { 'ApiClient' } }
if (-not $ProviderVersion) { $ProviderVersion = if ($env:PACT_PROVIDER_VERSION) { $env:PACT_PROVIDER_VERSION } else { '0.0.0-local' } }
if (-not $BrokerToken) { $BrokerToken = $env:PACT_BROKER_TOKEN }

# Windows Docker Desktop does not support --network host.
# Use host.docker.internal to reach services on the host from the container.
if (-not $ProviderBaseUrl) { $ProviderBaseUrl = "http://host.docker.internal:9001" }

# Verify Docker is available
$dockerVersion = (& docker --version) 2>$null
if (-not $dockerVersion) { Write-Error "Docker is not available on PATH."; exit 1 }

$args = @(
    'run','--rm',
    '--add-host','host.docker.internal:host-gateway'
)

if ($BrokerToken) {
    $args += @('--env',"PACT_BROKER_TOKEN=$BrokerToken")
}

$args += @('--env',"PACT_BROKER_BASE_URL=$BrokerUrl")
$args += @('pactfoundation/pact-cli','pact-provider-verifier')
$args += @('--provider', $ProviderName)
$args += @('--provider-base-url', $ProviderBaseUrl)
$args += @('--publish-verification-results')
$args += @('--provider-app-version', $ProviderVersion)
$args += @('--consumer-name', $ConsumerName)
$args += @('--pact-broker-base-url', $BrokerUrl)

Write-Host "Running: docker $($args -join ' ')"

& docker @args
$exitCode = $LASTEXITCODE
exit $exitCode
