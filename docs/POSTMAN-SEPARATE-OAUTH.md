# Postman Contract Testing with Separate OAuth Server

## Table of Contents

1. [Architectures](#architectures)
2. [Scenarios & Solutions](#scenarios--solutions)
3. [Multi-Service Setup](#multi-service-setup)
4. [CI/CD Pipelines](#cicd-pipelines)
5. [Docker Compose Setup](#docker-compose-setup)
6. [Network Communication](#network-communication)
7. [Best Practices](#best-practices)

---

## Architectures

### Separate OAuth Server Scenario

When OAuth server is a **completely different application**:

```
┌─────────────────────────────────────────────────────────────┐
│                      Your System                             │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐   │
│  │ OAuth Server │    │   Provider   │    │   Consumer   │   │
│  │ (Separate)   │◀──▶│   API        │◀──▶│  Application │   │
│  │              │    │              │    │              │   │
│  │ Port: 5000   │    │ Port: 5001   │    │ Port: 3000   │   │
│  │ Dotnet Core  │    │ Dotnet Core  │    │ Node.js      │   │
│  └──────────────┘    └──────────────┘    └──────────────┘   │
│         ▲                    ▲                    ▲           │
│         │                    │                    │           │
│  ┌──────┴────────────────────┴────────────────────┴────────┐ │
│  │            Postman Contract Tests (Newman)             │ │
│  │         Testing Provider API + OAuth Calls             │ │
│  └──────────────────────────────────────────────────────────┘ │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Key Differences from Integrated OAuth

| Aspect | Integrated OAuth | Separate OAuth |
|--------|------------------|----------------|
| **Deployment** | Same service | Different service |
| **Startup** | Together | Separate startup |
| **Dependency** | None | Hard dependency |
| **Port** | Same (or random) | Different port |
| **Database** | Shared or separate | Completely separate |
| **Scaling** | Together | Independent |
| **Testing** | Easy | Requires coordination |

---

## Scenarios & Solutions

### Scenario 1: OAuth Server + Provider API (Separate Services)

**Architecture**:
```
OAuth Server (Port 5000)
        │
        └─────────────────────────────┐
                                      │
                            Provider API (Port 5001)
                                      │
                            Postman Contract Tests
```

**Challenge**: Tests must call OAuth first to get token, then call API

**Solution**:

```javascript
// Pre-request script for collection
const getOAuthToken = (callback) => {
  const oauthUrl = pm.environment.get("oauth_url");  // http://localhost:5000
  const clientId = pm.environment.get("client_id");
  const clientSecret = pm.environment.get("client_secret");

  const request = {
    url: oauthUrl + "/oauth/token",
    method: "POST",
    header: {
      "Content-Type": "application/x-www-form-urlencoded"
    },
    body: {
      mode: "urlencoded",
      urlencoded: [
        { key: "grant_type", value: "client_credentials" },
        { key: "client_id", value: clientId },
        { key: "client_secret", value: clientSecret },
        { key: "scope", value: "api" }
      ]
    }
  };

  pm.sendRequest(request, (err, response) => {
    if (err) {
      console.error("OAuth Error:", err);
      callback(null);
    } else {
      const jsonResponse = response.json();
      pm.environment.set("access_token", jsonResponse.access_token);
      pm.environment.set("token_expiry", 
        new Date().getTime() + (jsonResponse.expires_in * 1000)
      );
      console.log("Token acquired from separate OAuth server");
      callback(jsonResponse.access_token);
    }
  });
};

// Check if token needs refresh
const tokenExpiry = pm.environment.get("token_expiry");
if (!pm.environment.get("access_token") || 
    new Date().getTime() > tokenExpiry) {
  getOAuthToken((token) => {
    if (!token) {
      throw new Error("Failed to obtain token from OAuth server");
    }
  });
}
```

**Environment Configuration**:
```json
{
  "id": "separate-oauth-env",
  "name": "Separate OAuth Environment",
  "values": [
    {
      "key": "oauth_url",
      "value": "http://localhost:5000"
    },
    {
      "key": "baseUrl",
      "value": "http://localhost:5001"
    },
    {
      "key": "client_id",
      "value": "postman-client"
    },
    {
      "key": "client_secret",
      "value": "postman-secret"
    },
    {
      "key": "access_token",
      "value": ""
    },
    {
      "key": "token_expiry",
      "value": ""
    }
  ]
}
```

---

### Scenario 2: Third-Party OAuth Provider

**When OAuth is hosted externally** (Auth0, Azure AD, Okta, etc.)

**Architecture**:
```
External OAuth Server (e.g., https://auth.okta.com)
        │
        └───────────────────────────────┐
                                        │
                              Your Provider API (Port 5001)
                                        │
                              Postman Contract Tests
```

**Solution**:

```javascript
// Pre-request script for third-party OAuth
const getThirdPartyToken = (callback) => {
  const tokenUrl = pm.environment.get("third_party_token_url");
  const audience = pm.environment.get("audience");
  const clientId = pm.environment.get("third_party_client_id");
  const clientSecret = pm.environment.get("third_party_client_secret");

  const request = {
    url: tokenUrl,
    method: "POST",
    header: {
      "Content-Type": "application/json"
    },
    body: {
      mode: "raw",
      raw: JSON.stringify({
        client_id: clientId,
        client_secret: clientSecret,
        audience: audience,
        grant_type: "client_credentials"
      })
    }
  };

  pm.sendRequest(request, (err, response) => {
    if (err) {
      console.error("Third-party OAuth Error:", err);
      callback(null);
    } else {
      const jsonResponse = response.json();
      const token = jsonResponse.access_token;
      
      pm.environment.set("access_token", token);
      pm.environment.set("token_expiry",
        new Date().getTime() + (jsonResponse.expires_in * 1000)
      );
      
      console.log("Token acquired from third-party provider");
      callback(token);
    }
  });
};

// Execute token fetch
const shouldRefresh = !pm.environment.get("access_token") || 
  new Date().getTime() > pm.environment.get("token_expiry");

if (shouldRefresh) {
  getThirdPartyToken((token) => {
    if (!token) {
      throw new Error("Failed to get token from third-party OAuth");
    }
  });
}
```

**Environment for Third-Party OAuth (Auth0 Example)**:
```json
{
  "id": "third-party-oauth-env",
  "name": "Third-Party OAuth (Auth0)",
  "values": [
    {
      "key": "third_party_token_url",
      "value": "https://your-tenant.auth0.com/oauth/token"
    },
    {
      "key": "baseUrl",
      "value": "http://localhost:5001"
    },
    {
      "key": "audience",
      "value": "https://your-api.example.com"
    },
    {
      "key": "third_party_client_id",
      "value": "your-auth0-client-id"
    },
    {
      "key": "third_party_client_secret",
      "value": "your-auth0-client-secret"
    },
    {
      "key": "access_token",
      "value": ""
    },
    {
      "key": "token_expiry",
      "value": ""
    }
  ]
}
```

---

## Multi-Service Setup

### Local Development - All Services Running

**Structure**:
```
Project Root/
├── OAuthServer/
│   ├── src/
│   ├── OAuthServer.csproj
│   └── Program.cs
├── ProviderAPI/
│   ├── src/
│   ├── Provider.csproj
│   └── Program.cs
└── postman-contract-testing/
    └── postman-collections/
        ├── Product-API-Contract-Tests.postman_collection.json
        └── Product-API-Environment.postman_environment.json
```

### Startup Script (PowerShell)

**start-services.ps1**:

```powershell
#!/usr/bin/env pwsh

# Script to start OAuth server and Provider API for contract testing

param(
    [switch]$RunTests = $false,
    [switch]$Cleanup = $false
)

# Configuration
$oauthServerPath = ".\OAuthServer\src"
$providerApiPath = ".\ProviderAPI\src"
$oauthPort = 5000
$apiPort = 5001
$healthCheckUrl = "http://localhost:$apiPort/api/products"
$oauthHealthCheckUrl = "http://localhost:$oauthPort/oauth/token"

# Cleanup function
function Stop-Services {
    Write-Host "Stopping services..."
    Get-Process | Where-Object { $_.ProcessName -match "dotnet" } | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Write-Host "Services stopped"
}

# Cleanup if requested
if ($Cleanup) {
    Stop-Services
    exit 0
}

# Start OAuth Server
Write-Host "Starting OAuth Server on port $oauthPort..."
Push-Location $oauthServerPath
Start-Process -NoNewWindow -FilePath "dotnet" -ArgumentList "run", "--urls=http://localhost:$oauthPort" -PassThru | Out-Null
Pop-Location

# Start Provider API
Write-Host "Starting Provider API on port $apiPort..."
Push-Location $providerApiPath
Start-Process -NoNewWindow -FilePath "dotnet" -ArgumentList "run", "--urls=http://localhost:$apiPort" -PassThru | Out-Null
Pop-Location

# Wait for services to be ready
Write-Host "Waiting for services to be ready..."
$retries = 0
$maxRetries = 30

# Check OAuth Server
Write-Host "Checking OAuth Server..."
while ($retries -lt $maxRetries) {
    try {
        $response = Invoke-WebRequest -Uri $oauthHealthCheckUrl -Method Post -ErrorAction Stop
        Write-Host "OAuth Server is ready!"
        break
    }
    catch {
        $retries++
        if ($retries -ge $maxRetries) {
            Write-Host "ERROR: OAuth Server failed to start"
            Stop-Services
            exit 1
        }
        Write-Host "OAuth Server not ready yet... ($retries/$maxRetries)"
        Start-Sleep -Seconds 1
    }
}

# Check API Server
$retries = 0
Write-Host "Checking Provider API..."
while ($retries -lt $maxRetries) {
    try {
        $response = Invoke-WebRequest -Uri $healthCheckUrl -ErrorAction Stop
        Write-Host "Provider API is ready!"
        break
    }
    catch {
        $retries++
        if ($retries -ge $maxRetries) {
            Write-Host "ERROR: Provider API failed to start"
            Stop-Services
            exit 1
        }
        Write-Host "Provider API not ready yet... ($retries/$maxRetries)"
        Start-Sleep -Seconds 1
    }
}

Write-Host "All services are running!"

if ($RunTests) {
    Write-Host "Running Postman contract tests..."
    & newman run `
        "postman-contract-testing/postman-collections/Product-API-Contract-Tests.postman_collection.json" `
        -e "postman-contract-testing/postman-collections/Product-API-Environment.postman_environment.json" `
        --reporters cli,json `
        --reporter-json-export results.json
    
    $testExitCode = $LASTEXITCODE
    Write-Host "Tests completed with exit code: $testExitCode"
    
    Stop-Services
    exit $testExitCode
}
else {
    Write-Host "Services are running. Press Ctrl+C to stop."
    Write-Host "OAuth Server: http://localhost:$oauthPort"
    Write-Host "Provider API: http://localhost:$apiPort"
    Write-Host ""
    Read-Host "Press Enter to stop services"
    Stop-Services
}
```

**Usage**:

```powershell
# Start services and keep running
.\start-services.ps1

# Start services, run tests, and cleanup
.\start-services.ps1 -RunTests

# Stop services
.\start-services.ps1 -Cleanup
```

---

## CI/CD Pipelines

### GitHub Actions - Separate OAuth Server

```yaml
name: Contract Tests - Separate OAuth

on: [push, pull_request]

jobs:
  contract-tests:
    runs-on: ubuntu-latest
    timeout-minutes: 15

    services:
      oauth-server:
        image: mcr.microsoft.com/dotnet/sdk:8.0
        options: >-
          -v ${{ github.workspace }}/OAuthServer:/app
          -w /app/src
          --name oauth-server
        ports:
          - 5000:5000
        env:
          ASPNETCORE_URLS: http://+:5000

      provider-api:
        image: mcr.microsoft.com/dotnet/sdk:8.0
        options: >-
          -v ${{ github.workspace }}/ProviderAPI:/app
          -w /app/src
          --name provider-api
        ports:
          - 5001:5001
        env:
          ASPNETCORE_URLS: http://+:5001

    steps:
      - uses: actions/checkout@v3

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'

      - name: Install Newman
        run: npm install -g newman

      - name: Wait for OAuth Server
        run: |
          for i in {1..30}; do
            if curl -X POST http://localhost:5000/oauth/token \
               -H "Content-Type: application/x-www-form-urlencoded" \
               -d "grant_type=client_credentials&client_id=test&client_secret=test" \
               -f 2>/dev/null; then
              echo "OAuth Server is ready"
              break
            fi
            echo "Waiting for OAuth Server... ($i/30)"
            sleep 1
          done

      - name: Wait for Provider API
        run: |
          for i in {1..30}; do
            curl -f http://localhost:5001/api/products && break
            echo "Waiting for Provider API... ($i/30)"
            sleep 1
          done

      - name: Run Postman Contract Tests
        run: |
          newman run \
            "postman-contract-testing/postman-collections/Product-API-Contract-Tests.postman_collection.json" \
            -e "postman-contract-testing/postman-collections/Product-API-Environment.postman_environment.json" \
            --reporters cli,json \
            --reporter-json-export results.json
        env:
          oauth_url: http://localhost:5000
          baseUrl: http://localhost:5001

      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: test-results
          path: results.json
```

### Using Docker Compose

**docker-compose.yml**:

```yaml
version: '3.8'

services:
  oauth-server:
    build:
      context: ./OAuthServer
      dockerfile: Dockerfile
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_URLS=http://+:5000
      - ASPNETCORE_ENVIRONMENT=Development
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 5s
      timeout: 3s
      retries: 10

  provider-api:
    build:
      context: ./ProviderAPI
      dockerfile: Dockerfile
    ports:
      - "5001:5001"
    environment:
      - ASPNETCORE_URLS=http://+:5001
      - ASPNETCORE_ENVIRONMENT=Development
      - AUTH_URL=http://oauth-server:5000
    depends_on:
      oauth-server:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5001/api/products"]
      interval: 5s
      timeout: 3s
      retries: 10

  newman-tests:
    image: node:18
    volumes:
      - ./postman-contract-testing:/tests
    working_dir: /tests
    command: >
      sh -c "npm install -g newman &&
             sleep 5 &&
             newman run postman-collections/Product-API-Contract-Tests.postman_collection.json
             -e postman-collections/Product-API-Environment.postman_environment.json
             --reporters cli,json
             --reporter-json-export results.json"
    depends_on:
      provider-api:
        condition: service_healthy
    environment:
      - oauth_url=http://oauth-server:5000
      - baseUrl=http://provider-api:5001
```

**Usage**:

```bash
# Start all services
docker-compose up

# Run tests
docker-compose up newman-tests

# Clean up
docker-compose down
```

---

## Docker Compose Setup

### Production-Ready Setup

**docker-compose.prod.yml**:

```yaml
version: '3.8'

services:
  oauth-server:
    image: myrepo/oauth-server:latest
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_URLS=http://+:5000
      - DATABASE_URL=postgres://user:pass@postgres:5432/oauth
      - JWT_SECRET=${JWT_SECRET}
    depends_on:
      - postgres
    networks:
      - app-network
    restart: unless-stopped

  provider-api:
    image: myrepo/provider-api:latest
    ports:
      - "5001:5001"
    environment:
      - ASPNETCORE_URLS=http://+:5001
      - AUTH_URL=http://oauth-server:5000
      - DATABASE_URL=postgres://user:pass@postgres:5432/api
    depends_on:
      - postgres
      - oauth-server
    networks:
      - app-network
    restart: unless-stopped

  postgres:
    image: postgres:15
    environment:
      - POSTGRES_USER=user
      - POSTGRES_PASSWORD=pass
      - POSTGRES_MULTIPLE_DATABASES=oauth,api
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - app-network
    restart: unless-stopped

volumes:
  postgres_data:

networks:
  app-network:
    driver: bridge
```

---

## Network Communication

### Local Development Communication

```
┌─────────────────────────────────────────────────┐
│           Local Network (localhost)              │
├─────────────────────────────────────────────────┤
│                                                 │
│  Postman Tests (localhost)                      │
│         ↓                                       │
│  1. POST http://localhost:5000/oauth/token     │
│     (Get token from OAuth Server)               │
│         ↓                                       │
│  2. GET http://localhost:5001/api/products     │
│     Authorization: Bearer {{access_token}}      │
│     (Call Provider API with token)              │
│         ↓                                       │
│  3. Validate response                           │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Docker Compose Network Communication

```
┌──────────────────────────────────────────────────────┐
│        Docker Compose Network (app-network)          │
├──────────────────────────────────────────────────────┤
│                                                      │
│  Newman Tests Container                             │
│         ↓                                           │
│  1. POST http://oauth-server:5000/oauth/token      │
│     (Internal DNS resolution)                       │
│         ↓                                           │
│  2. GET http://provider-api:5001/api/products      │
│     (Internal DNS resolution)                       │
│         ↓                                           │
│  3. Validate response                              │
│                                                      │
└──────────────────────────────────────────────────────┘
```

### URL Differences

```javascript
// Local Development
const oauth_url = "http://localhost:5000";
const api_url = "http://localhost:5001";

// Docker Compose (internal communication)
const oauth_url = "http://oauth-server:5000";
const api_url = "http://provider-api:5001";

// CI/CD Pipeline (services)
const oauth_url = "http://localhost:5000";  // or Docker network name
const api_url = "http://localhost:5001";    // or Docker network name
```

---

## Best Practices

### ✅ DO

1. **Separate OAuth Server Completely**
   - Different codebase
   - Different Git repository
   - Different deployment pipeline

2. **Use Environment Variables for OAuth URLs**
   ```javascript
   // Environment-based URL selection
   const oauth_url = pm.environment.get("oauth_url");
   // Different per environment: dev, staging, prod
   ```

3. **Implement Proper Startup Order**
   ```yaml
   depends_on:
     oauth-server:
       condition: service_healthy
   ```

4. **Store Credentials Securely**
   ```bash
   # Use GitHub Secrets or similar
   oauth_client_id: ${{ secrets.OAUTH_CLIENT_ID }}
   oauth_client_secret: ${{ secrets.OAUTH_CLIENT_SECRET }}
   ```

5. **Health Checks for Both Services**
   ```yaml
   healthcheck:
     test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
     interval: 5s
     retries: 10
   ```

6. **Multiple Environment Configurations**
   ```
   environments/
   ├── local.json (localhost)
   ├── docker.json (docker-compose)
   ├── staging.json (staging URLs)
   └── production.json (production URLs)
   ```

7. **Circuit Breaker Pattern**
   ```javascript
   // Retry logic for OAuth calls
   const maxRetries = 3;
   let attempts = 0;
   
   while (attempts < maxRetries) {
     try {
       // Get token
       break;
     } catch (error) {
       attempts++;
       if (attempts >= maxRetries) throw error;
       await sleep(1000 * attempts);  // Exponential backoff
     }
   }
   ```

### ❌ DON'T

1. **Don't hardcode OAuth URLs in tests**
   - Use environment variables
   - Different per deployment

2. **Don't assume OAuth is always available**
   - Implement retry logic
   - Handle timeouts gracefully

3. **Don't put OAuth credentials in collection**
   - Use environment variables
   - Use CI/CD secrets

4. **Don't run tests if OAuth fails to start**
   - Implement dependency checks
   - Fail fast with clear errors

5. **Don't forget to stop services**
   - Implement cleanup logic
   - Free up ports for next run

6. **Don't mix OAuth server config with API config**
   - Separate concerns
   - Different environment files

---

## Configuration Examples

### Environment: Local Development

```json
{
  "id": "local-dev",
  "name": "Local Development",
  "values": [
    {
      "key": "oauth_url",
      "value": "http://localhost:5000"
    },
    {
      "key": "baseUrl",
      "value": "http://localhost:5001"
    },
    {
      "key": "client_id",
      "value": "local-client"
    },
    {
      "key": "client_secret",
      "value": "local-secret"
    }
  ]
}
```

### Environment: Docker Compose

```json
{
  "id": "docker-compose",
  "name": "Docker Compose",
  "values": [
    {
      "key": "oauth_url",
      "value": "http://oauth-server:5000"
    },
    {
      "key": "baseUrl",
      "value": "http://provider-api:5001"
    },
    {
      "key": "client_id",
      "value": "docker-client"
    },
    {
      "key": "client_secret",
      "value": "docker-secret"
    }
  ]
}
```

### Environment: CI/CD Pipeline

```json
{
  "id": "ci-cd",
  "name": "CI/CD Pipeline",
  "values": [
    {
      "key": "oauth_url",
      "value": "http://localhost:5000"
    },
    {
      "key": "baseUrl",
      "value": "http://localhost:5001"
    },
    {
      "key": "client_id",
      "value": "{{OAUTH_CLIENT_ID}}"
    },
    {
      "key": "client_secret",
      "value": "{{OAUTH_CLIENT_SECRET}}"
    }
  ]
}
```

---

## Troubleshooting

### Problem: "Connection refused to OAuth Server"

**Cause**: OAuth server hasn't started yet

**Solution**:
```javascript
// Add retry logic
const getTokenWithRetry = (callback, retries = 3) => {
  let attempt = 0;
  
  const attempt_request = () => {
    pm.sendRequest(request, (err, response) => {
      if (err && attempt < retries) {
        attempt++;
        console.log(`Retry ${attempt}/${retries}`);
        setTimeout(attempt_request, 1000 * attempt);
      } else if (err) {
        callback(null);
      } else {
        callback(response.json().access_token);
      }
    });
  };
  
  attempt_request();
};
```

### Problem: "OAuth token expires during long tests"

**Cause**: Token expires between getting it and using it

**Solution**:
```javascript
// Refresh token before each request
const ensureValidToken = (callback) => {
  const expiry = pm.environment.get("token_expiry");
  const now = new Date().getTime();
  
  // Refresh if expires in less than 2 minutes
  if (!expiry || (expiry - now) < 120000) {
    getOAuthToken(callback);
  } else {
    callback(pm.environment.get("access_token"));
  }
};
```

### Problem: "Services can't reach each other in Docker"

**Cause**: Network issues or service names

**Solution**:
```yaml
services:
  service1:
    networks:
      - app-network
  service2:
    networks:
      - app-network

networks:
  app-network:
    driver: bridge
```

---

## Summary

### When OAuth is a Separate Application

1. **Configuration**: Use environment variables for OAuth URL
2. **Startup**: Ensure OAuth starts before Provider API
3. **Tests**: Get token from OAuth, use with Provider API
4. **CI/CD**: Use Docker Compose or service orchestration
5. **Environments**: Different configs per environment

### Files to Create

- ✅ `docker-compose.yml` - Local development
- ✅ `.env` - Environment configuration
- ✅ `start-services.ps1` - Local startup script
- ✅ Multiple environment JSON files
- ✅ CI/CD workflow with service dependencies

### Key Takeaway

**Separate OAuth servers require explicit orchestration** - ensure startup order, health checks, and proper network configuration. Use Docker Compose for consistent local/CI environments.

---

**Last Updated**: November 26, 2025
