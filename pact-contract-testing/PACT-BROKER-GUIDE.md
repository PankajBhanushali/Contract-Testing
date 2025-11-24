# Pact Broker in Docker - Complete Setup & Usage Guide

## Overview

This guide explains how to set up a local Pact Broker using Docker, publish consumer pacts to it, and configure provider verification to pull contracts from the broker instead of local files. This enables a centralized contract management workflow for CI/CD pipelines.

## Prerequisites

- Docker Desktop installed and running
- .NET 8.0 SDK
- PowerShell 5.1+
- Your consumer and provider Pact tests already set up and passing

## Part 1: Starting the Pact Broker in Docker

### 1.1 Launch the Broker Container

Start a Pact Broker container with an in-memory SQLite database:

```powershell
docker run -d `
  --name pact-broker `
  -p 9292:9292 `
  -e PACT_BROKER_DATABASE_ADAPTER=sqlite `
  -e PACT_BROKER_DATABASE_NAME=:memory: `
  pactfoundation/pact-broker
```

**Parameters explained:**
- `-d`: Run in detached mode (background)
- `--name pact-broker`: Container name for easy reference
- `-p 9292:9292`: Map container port 9292 to host port 9292
- `-e PACT_BROKER_DATABASE_ADAPTER=sqlite`: Use SQLite adapter (lightweight)
- `-e PACT_BROKER_DATABASE_NAME=:memory:`: Use in-memory database (resets on container restart)
- `pactfoundation/pact-broker`: Official Pact Broker Docker image

### 1.2 Verify Broker is Running

Check container status:

```powershell
docker ps --filter "name=pact-broker"
```

Test connectivity:

```powershell
Invoke-WebRequest -Uri http://localhost:9292/diagnostic/status/heartbeat | Select-Object StatusCode
# Expected output: StatusCode: 200
```

### 1.3 Access the Broker UI

Open in your browser: **http://localhost:9292**

You'll see the Pact Broker dashboard where you can view published pacts and their verification status.

### 1.4 Stop or Restart the Broker

Stop the broker:
```powershell
docker stop pact-broker
```

Restart the broker:
```powershell
docker start pact-broker
```

Remove the broker container (and delete all data):
```powershell
docker rm pact-broker
```

---

## Part 2: Publishing Pacts to the Broker

### 2.1 Generate the Pact File

First, run your consumer tests to generate or update the pact file:

```powershell
cd "C:\path\to\pact-contract-testing"
dotnet test .\Consumer -v minimal
```

This creates/updates `pacts/ApiClient-ProductService.json`.

### 2.2 Publish Using HTTP API

Publish the pact directly to the broker using the HTTP API:

```powershell
$pactPath = "C:\path\to\pact-contract-testing\pacts\ApiClient-ProductService.json"
$pactContent = Get-Content -Path $pactPath -Raw
$uri = "http://localhost:9292/pacts/provider/ProductService/consumer/ApiClient/version/1.0.0"

Invoke-WebRequest -Uri $uri `
  -Method PUT `
  -Body $pactContent `
  -ContentType "application/json" `
  -Verbose
```

**Expected response:**
- Status Code: `201 Created`
- Content-Type: `application/hal+json`

### 2.3 Verify Publication

Check the broker UI or query the API:

```powershell
$latestPact = Invoke-WebRequest -Uri "http://localhost:9292/pacts/provider/ProductService/consumer/ApiClient/latest" | ConvertFrom-Json
$latestPact | Select-Object -Property "consumer", "provider"

# Output:
# consumer : @{name=ApiClient}
# provider : @{name=ProductService}
```

### 2.4 Publishing with Version Tags

To manage multiple deployment environments, publish with tags:

```powershell
$pactPath = "C:\path\to\pact-contract-testing\pacts\ApiClient-ProductService.json"
$pactContent = Get-Content -Path $pactPath -Raw
$uri = "http://localhost:9292/pacts/provider/ProductService/consumer/ApiClient/version/1.0.0"

# Add tag parameter for environment tracking
$uri += "?tag=main"  # or "?tag=prod", "?tag=develop", etc.

Invoke-WebRequest -Uri $uri `
  -Method PUT `
  -Body $pactContent `
  -ContentType "application/json"
```

---

## Part 3: Configuring Provider Verification from Broker

### 3.1 Update Provider Test to Use Broker

Modify `Provider/tests/ProductTest.cs` to fetch pacts from the broker instead of local files:

**Before (local file):**
```csharp
var pactFile = new FileInfo(Path.Join("..", "..", "..", "..", "..", "pacts", "ApiClient-ProductService.json"));
pactVerifier.WithHttpEndpoint(new Uri(_pactServiceUri))
    .WithFileSource(pactFile)
    .WithProviderStateUrl(new Uri($"{_pactServiceUri}/provider-states"))
    .Verify();
```

**After (broker source):**
```csharp
pactVerifier.WithHttpEndpoint(new Uri(_pactServiceUri))
    .WithUriSource(new Uri("http://localhost:9292/pacts/provider/ProductService/consumer/ApiClient/latest"))
    .WithProviderStateUrl(new Uri($"{_pactServiceUri}/provider-states"))
    .Verify();
```

### 3.2 Run Provider Verification Against Broker

Execute provider tests:

```powershell
cd "C:\path\to\pact-contract-testing"
dotnet test .\Provider -v minimal
```

**Expected output:**
- Verifier loads pact from broker (31ms loading time)
- All 5 interactions verified (200-300ms total)
- Test passes with all interactions passing verification

### 3.3 Broker URI Format Reference

Standard endpoints:

| Endpoint | Purpose |
|----------|---------|
| `http://localhost:9292/pacts/provider/ProductService/consumer/ApiClient/latest` | Latest published pact |
| `http://localhost:9292/pacts/provider/ProductService/consumer/ApiClient/version/1.0.0` | Specific version |
| `http://localhost:9292/pacts/provider/ProductService/consumer/ApiClient/latest/main` | Latest with tag `main` |

---

## Part 4: Complete Workflow Example

### 4.1 Full CI/CD-Style Workflow

**Step 1: Consumer Pipeline**
```powershell
# 1. Run consumer tests (generates pact)
cd "C:\path\to\pact-contract-testing"
dotnet test .\Consumer

# 2. Publish pact to broker
$pactPath = "C:\path\to\pact-contract-testing\pacts\ApiClient-ProductService.json"
$pactContent = Get-Content -Path $pactPath -Raw
$uri = "http://localhost:9292/pacts/provider/ProductService/consumer/ApiClient/version/1.0.0?tag=main"

Invoke-WebRequest -Uri $uri `
  -Method PUT `
  -Body $pactContent `
  -ContentType "application/json"

Write-Host "✅ Pact published to broker"
```

**Step 2: Provider Pipeline**
```powershell
# 1. Run provider verification against broker pact
cd "C:\path\to\pact-contract-testing"
dotnet test .\Provider

# 2. If verification passes, provider can be deployed
Write-Host "✅ All pacts verified - safe to deploy"
```

### 4.2 Broker Query Example

Get pact publication details:

```powershell
$pact = Invoke-WebRequest -Uri "http://localhost:9292/pacts/provider/ProductService/consumer/ApiClient/latest" | ConvertFrom-Json

Write-Host "Consumer: $($pact.consumer.name)"
Write-Host "Provider: $($pact.provider.name)"
Write-Host "Created: $($pact.createdAt)"
```

---

## Part 5: Advanced Configuration

### 5.1 Using Persistent Database

For data persistence across container restarts, use PostgreSQL or MySQL:

```powershell
# Example with PostgreSQL (requires separate Postgres container)
docker run -d `
  --name pact-broker `
  -p 9292:9292 `
  -e PACT_BROKER_DATABASE_ADAPTER=postgres `
  -e PACT_BROKER_DATABASE_USERNAME=pact `
  -e PACT_BROKER_DATABASE_PASSWORD=pactbroker `
  -e PACT_BROKER_DATABASE_NAME=pact_broker `
  -e PACT_BROKER_DATABASE_HOST=postgres-host `
  pactfoundation/pact-broker
```

### 5.2 Broker with Custom Port

```powershell
docker run -d `
  --name pact-broker `
  -p 8080:9292 `  # Host port 8080 → container port 9292
  -e PACT_BROKER_DATABASE_ADAPTER=sqlite `
  -e PACT_BROKER_DATABASE_NAME=:memory: `
  pactfoundation/pact-broker
```

Access at: `http://localhost:8080`

### 5.3 Broker with Webhook Notifications

Configure webhooks to trigger provider verification automatically:

```powershell
# Example webhook configuration (would be set via Broker API)
# This triggers provider tests when a new pact is published
$webhookConfig = @{
    description = "Trigger provider verification"
    events = @("pact.published")
    request = @{
        method = "POST"
        url = "http://ci-server/trigger-provider-tests"
    }
}
```

---

## Part 6: Troubleshooting

### Issue: Cannot Reach Broker at localhost:9292

**Solution:**
1. Verify Docker container is running:
   ```powershell
   docker ps --filter "name=pact-broker"
   ```
2. Check container logs:
   ```powershell
   docker logs pact-broker
   ```
3. Test port connectivity:
   ```powershell
   netstat -ano | findstr :9292
   ```

### Issue: Pact Publication Returns 400/500

**Solution:**
1. Verify pact file is valid JSON:
   ```powershell
   Get-Content "pact-contract-testing\pacts\ApiClient-ProductService.json" | ConvertFrom-Json
   ```
2. Check URI format - must match pattern:
   ```
   /pacts/provider/{providerName}/consumer/{consumerName}/version/{version}
   ```
3. Ensure consumer and provider names match pact file metadata

### Issue: Provider Verification Fails to Load Pact from Broker

**Solution:**
1. Test broker connectivity from test machine:
   ```powershell
   Invoke-WebRequest -Uri "http://localhost:9292/pacts/provider/ProductService/consumer/ApiClient/latest"
   ```
2. Verify exact URI in test matches broker API format
3. Ensure pact has been published before running provider tests

### Issue: 401 Unauthorized on Provider Tests

**Solution:**
1. Check `AuthTokenRequestFilter` is active in test startup
2. Verify `ProviderStateMiddleware` is configured correctly
3. Ensure pact includes proper authorization header matchers

---

## Part 7: Best Practices

### 7.1 Version Management

```powershell
# Semantic versioning for pact versions
$version = "1.2.3"  # Major.Minor.Patch

# Publish with environment tag
$uri = "http://localhost:9292/pacts/provider/ProductService/consumer/ApiClient/version/$version?tag=main"
```

### 7.2 CI/CD Integration

**GitHub Actions Example:**
```yaml
- name: Publish Pacts to Broker
  run: |
    $pact = Get-Content -Raw -Path "./pacts/ApiClient-ProductService.json"
    $uri = "http://pact-broker:9292/pacts/provider/ProductService/consumer/ApiClient/version/${{ github.ref_name }}"
    Invoke-WebRequest -Uri $uri -Method PUT -Body $pact -ContentType "application/json"
```

### 7.3 Pact Maintenance

- Regularly review published pacts in broker UI
- Archive old versions by removing old tags
- Monitor can-i-deploy status before deployments
- Keep pact definitions in version control alongside code

### 7.4 Documentation

Document your pact contract expectations:

```csharp
// Consumer/tests/ApiTest.cs
[Fact]
public async Task GetAllProducts()
{
    // PACT: ApiClient expects ProductService to provide a list of products
    // - Endpoint: GET /api/products
    // - Authorization: Bearer token required (timestamp format: yyyy-MM-ddTHHmm:ss.fffZ)
    // - Response: Array of products with id, type, name, version fields
    // - Status: 200 OK
    
    pact.UponReceiving("A valid request for all products")
        .Given("products exist")
        // ... rest of test
}
```

---

## Part 8: Cleanup & Reset

### 8.1 Reset In-Memory Broker

Since we're using an in-memory database, simply restart:

```powershell
docker restart pact-broker
```

### 8.2 Remove All Data

```powershell
docker stop pact-broker
docker rm pact-broker
```

Then restart broker if needed.

### 8.3 View Broker Logs

```powershell
docker logs -f pact-broker  # Follow logs in real-time
docker logs pact-broker | Select-String "ERROR"  # Show only errors
```

---

## Part 9: Reference Commands

### Quick Commands

```powershell
# Start broker
docker run -d --name pact-broker -p 9292:9292 -e PACT_BROKER_DATABASE_ADAPTER=sqlite -e PACT_BROKER_DATABASE_NAME=:memory: pactfoundation/pact-broker

# Test broker
Invoke-WebRequest -Uri http://localhost:9292/diagnostic/status/heartbeat

# Run consumer tests
dotnet test .\pact-contract-testing\Consumer

# Publish pact
$pact = Get-Content -Raw "pact-contract-testing\pacts\ApiClient-ProductService.json"
Invoke-WebRequest -Uri "http://localhost:9292/pacts/provider/ProductService/consumer/ApiClient/version/1.0.0" -Method PUT -Body $pact -ContentType "application/json"

# Run provider tests
dotnet test .\pact-contract-testing\Provider

# View broker
Start-Process "http://localhost:9292"
```

---

## Summary

This workflow provides:

✅ **Centralized pact management** via Pact Broker  
✅ **Decoupled consumer/provider pipelines** - no direct file sharing  
✅ **CI/CD integration** - automate pact publication and verification  
✅ **Version tracking** - manage multiple environments/releases  
✅ **Audit trail** - see publication history in broker UI  

For production deployments, consider using a persistent database backend (PostgreSQL/MySQL) instead of in-memory SQLite.

---

## Additional Resources

- **Pact Broker Docs:** https://docs.pact.foundation/pact_broker
- **Docker Image:** https://hub.docker.com/r/pactfoundation/pact-broker
- **Pact Foundation:** https://pact.foundation
- **Can-I-Deploy:** https://docs.pact.foundation/pact_broker/can_i_deploy
