# Pact Broker Integration - Quick Reference

## Pact Broker Location
```
http://puvsfpactserver.tiger01-dev.ba.lab.local:9292
```

---

## Quick Commands

### 1. Test Broker Connectivity
```powershell
Invoke-WebRequest -Uri "http://puvsfpactserver.tiger01-dev.ba.lab.local:9292/diagnostic/status/heartbeat"
```
Expected: HTTP 200 OK

### 2. Run Consumer Tests (Generate Pacts)
```powershell
cd "C:\Users\crf8625\OneDrive - Siemens Healthineers\Documents\Work\Learning\Contract Testing\pact-contract-testing"
dotnet test .\Consumer -v minimal
```

### 3. Publish Pact to Broker
```powershell
$pact = Get-Content -Raw "C:\Users\crf8625\OneDrive - Siemens Healthineers\Documents\Work\Learning\Contract Testing\pact-contract-testing\pacts\ApiClient-ProductService.json"
Invoke-WebRequest -Uri "http://puvsfpactserver.tiger01-dev.ba.lab.local:9292/pacts/provider/ProductService/consumer/ApiClient/version/1.0.0" `
  -Method PUT -Body $pact -ContentType "application/json"
```

### 4. Run Provider Tests (Verify Against Broker)
```powershell
cd "C:\Users\crf8625\OneDrive - Siemens Healthineers\Documents\Work\Learning\Contract Testing\pact-contract-testing"
dotnet test .\Provider -v minimal
```

### 5. View Latest Published Pact
```powershell
$latestPact = Invoke-WebRequest -Uri "http://puvsfpactserver.tiger01-dev.ba.lab.local:9292/pacts/provider/ProductService/consumer/ApiClient/latest" | ConvertFrom-Json
$latestPact | Select-Object -Property "consumer", "provider", "createdAt"
```

### 6. View Broker Dashboard
```powershell
Start-Process "http://puvsfpactserver.tiger01-dev.ba.lab.local:9292"
```

---

## Publishing with Environment Tags

### Publish for Main Branch
```powershell
$pact = Get-Content -Raw "pact-contract-testing\pacts\ApiClient-ProductService.json"
$uri = "http://puvsfpactserver.tiger01-dev.ba.lab.local:9292/pacts/provider/ProductService/consumer/ApiClient/version/1.0.0?tag=main"
Invoke-WebRequest -Uri $uri -Method PUT -Body $pact -ContentType "application/json"
```

### Publish for Development
```powershell
$pact = Get-Content -Raw "pact-contract-testing\pacts\ApiClient-ProductService.json"
$uri = "http://puvsfpactserver.tiger01-dev.ba.lab.local:9292/pacts/provider/ProductService/consumer/ApiClient/version/1.0.0?tag=develop"
Invoke-WebRequest -Uri $uri -Method PUT -Body $pact -ContentType "application/json"
```

### Publish for Production
```powershell
$pact = Get-Content -Raw "pact-contract-testing\pacts\ApiClient-ProductService.json"
$uri = "http://puvsfpactserver.tiger01-dev.ba.lab.local:9292/pacts/provider/ProductService/consumer/ApiClient/version/1.0.0?tag=prod"
Invoke-WebRequest -Uri $uri -Method PUT -Body $pact -ContentType "application/json"
```

---

## Full Workflow Example

### Complete CI/CD-Style Flow

**Step 1: Consumer Pipeline**
```powershell
# Run consumer tests
cd "C:\Users\crf8625\OneDrive - Siemens Healthineers\Documents\Work\Learning\Contract Testing\pact-contract-testing"
dotnet test .\Consumer -v minimal

# Check if pact was created
if (Test-Path "pacts/ApiClient-ProductService.json") {
    Write-Host "✅ Pact generated successfully"
    
    # Publish to broker
    $pact = Get-Content -Raw "pacts/ApiClient-ProductService.json"
    $uri = "http://puvsfpactserver.tiger01-dev.ba.lab.local:9292/pacts/provider/ProductService/consumer/ApiClient/version/1.0.0"
    $response = Invoke-WebRequest -Uri $uri -Method PUT -Body $pact -ContentType "application/json"
    
    if ($response.StatusCode -eq 201) {
        Write-Host "✅ Pact published to broker"
    } else {
        Write-Host "❌ Failed to publish pact: $($response.StatusCode)"
    }
} else {
    Write-Host "❌ Pact file not found"
}
```

**Step 2: Provider Pipeline**
```powershell
# Run provider verification
cd "C:\Users\crf8625\OneDrive - Siemens Healthineers\Documents\Work\Learning\Contract Testing\pact-contract-testing"
$testResult = dotnet test .\Provider -v minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Provider verification passed - safe to deploy"
} else {
    Write-Host "❌ Provider verification failed - do not deploy"
}
```

---

## Troubleshooting

### Issue: Cannot Connect to Broker
```powershell
# Test connectivity
$brokerUrl = "http://puvsfpactserver.tiger01-dev.ba.lab.local:9292"
try {
    $response = Invoke-WebRequest -Uri "$brokerUrl/diagnostic/status/heartbeat" -ErrorAction Stop
    Write-Host "✅ Broker is reachable"
} catch {
    Write-Host "❌ Cannot reach broker: $_"
}
```

### Issue: Pact Publication Failed
```powershell
# Verify pact file is valid JSON
$pactPath = "pact-contract-testing\pacts\ApiClient-ProductService.json"
$pactContent = Get-Content -Path $pactPath | ConvertFrom-Json
Write-Host "Pact is valid JSON: $($pactContent | ConvertTo-Json | Measure-Object -Character)"
```

### Issue: Provider Verification Fails
```powershell
# Check if pact exists in broker
$latestUri = "http://puvsfpactserver.tiger01-dev.ba.lab.local:9292/pacts/provider/ProductService/consumer/ApiClient/latest"
try {
    $pact = Invoke-WebRequest -Uri $latestUri | ConvertFrom-Json
    Write-Host "✅ Latest pact found: $($pact.createdAt)"
} catch {
    Write-Host "❌ No pact found in broker"
}
```

---

## API Endpoints Reference

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/pacts/provider/{provider}/consumer/{consumer}/latest` | GET | Get latest published pact |
| `/pacts/provider/{provider}/consumer/{consumer}/version/{version}` | PUT | Publish specific version |
| `/pacts/provider/{provider}/consumer/{consumer}/version/{version}?tag={tag}` | PUT | Publish with environment tag |
| `/diagnostic/status/heartbeat` | GET | Check broker health |
| `/dashboard` | GET | View broker UI |

---

## Network Requirements

- **Broker Host**: `puvsfpactserver.tiger01-dev.ba.lab.local`
- **Broker Port**: `9292`
- **Protocol**: HTTP
- **Required Ports**: 9292 (TCP)

Verify access with:
```powershell
Test-NetConnection -ComputerName puvsfpactserver.tiger01-dev.ba.lab.local -Port 9292
```

---

## Documentation

- **Full Guide**: See `PACT-BROKER-GUIDE.md` for comprehensive documentation
- **Quick Start**: See `readme.md` in pact-contract-testing folder
- **Integration Summary**: See `PACT-BROKER-INTEGRATION-SUMMARY.md`

---

## Updated Files

1. ✅ **PACT-BROKER-GUIDE.md** - Complete Pact Broker setup and workflow
2. ✅ **Provider/tests/ProductTest.cs** - Provider test configuration updated
3. ✅ **pact-contract-testing/readme.md** - Quick start guide updated
4. ✅ **PACT-BROKER-INTEGRATION-SUMMARY.md** - This integration summary

---

**Last Updated**: 2024
**Status**: ✅ Configuration Complete
**Broker**: puvsfpactserver.tiger01-dev.ba.lab.local:9292
